using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VirtualControllers;
using VirtualControllers.Rewired;

namespace VirtualControllers.Editor
{
    /// <summary>
    /// Editor window to manage up to 16 virtual PS4 controllers while in Play Mode.
    /// Open via: Tools > Virtual Controllers > Controller Manager
    /// </summary>
    public class VirtualControllerManagerWindow : EditorWindow
    {
        // ── Layout constants ──────────────────────────────────────────
        private const int   MaxPlayers   = VirtualControllerRegistry.MaxPlayers;
        private const float HeaderHeight = 26f;
        private const float ColWidth     = 220f;

        // ── State ─────────────────────────────────────────────────────
        private Vector2 _scroll;
        private int     _selectedPlayer  = 0;
        private bool[]  _foldout         = new bool[MaxPlayers];
        private bool    _sequenceRunning = false;

        // ── Patrol ────────────────────────────────────────────────────
        private float   _patrolRadius    = 10f;
        private float   _patrolSpeed     = 0.8f;
        private bool    _patrolAllActive = false;

        // ── Full-flow sequence ─────────────────────────────────────────
        /// <summary>How many virtual players the Full Lobby Flow will spawn.</summary>
        private int _spawnCount = 2;

        // ── Scheduler ─────────────────────────────────────────────────
        // Actions are enqueued with an absolute EditorApplication.timeSinceStartup
        // timestamp and fired on the editor main thread via EditorApplication.update.
        private readonly Queue<(double fireAt, Action action)> _schedule =
            new Queue<(double, Action)>();

        // ── Menu entry ────────────────────────────────────────────────
        [MenuItem("Tools/Virtual Controllers/Controller Manager")]
        public static void Open()
        {
            var win = GetWindow<VirtualControllerManagerWindow>("Virtual Controllers");
            win.minSize = new Vector2(480, 520);
            win.Show();
        }

        // ── Lifecycle ─────────────────────────────────────────────────────
        private void OnEnable()
        {
            // Always start clean — if the window was closed mid-sequence
            // (or an exception left _sequenceRunning stuck), reset everything.
            _sequenceRunning = false;
            _schedule.Clear();
            EditorApplication.update -= TickSchedule; // idempotent unsubscribe
        }

        // ── GUI ───────────────────────────────────────────────────────
        private void OnGUI()
        {
            DrawHeader();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to spawn and control virtual controllers.", MessageType.Info);
                return;
            }

            if (VirtualRewiredBridge.Instance == null)
            {
                EditorGUILayout.HelpBox(
                    "VirtualRewiredBridge is not present in the scene.\n" +
                    "Click below to add it, or add it manually to a persistent GameObject.",
                    MessageType.Warning);
                if (GUILayout.Button("Auto-add VirtualRewiredBridge to scene", GUILayout.Height(30)))
                    AutoAddBridge();
                return;
            }

            DrawToolbar();
            DrawPatrolToolbar();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawPlayerList();
            EditorGUILayout.EndScrollView();

            // Repaint every frame in play mode so sliders/buttons feel live
            if (Application.isPlaying) Repaint();
        }

        // ── Section: Header ───────────────────────────────────────────
        private void DrawHeader()
        {
            var rect = EditorGUILayout.GetControlRect(false, HeaderHeight);
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
            GUI.Label(rect, "  🎮 Virtual PS4 Controller Manager", EditorStyles.whiteLargeLabel);
        }

        // ── Section: Toolbar ──────────────────────────────────────────
        private void DrawToolbar()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("+ Add Player", EditorStyles.toolbarButton, GUILayout.Width(90)))
                AddNextPlayer();

            if (GUILayout.Button("Remove All", EditorStyles.toolbarButton, GUILayout.Width(90)))
            {
                if (EditorUtility.DisplayDialog("Remove All", "Remove all virtual controllers?", "Yes", "Cancel"))
                {
                    for (int i = 0; i < MaxPlayers; i++)
                        VirtualRewiredBridge.Instance.RemoveController(i);
                }
            }

            GUILayout.FlexibleSpace();

            // ── Player-count picker ───────────────────────────────────
            GUILayout.Label("Spawn:", EditorStyles.toolbarButton, GUILayout.Width(44));
            _spawnCount = Mathf.Clamp(
                EditorGUILayout.IntField(_spawnCount, EditorStyles.toolbarTextField, GUILayout.Width(26)),
                1, MaxPlayers);
            GUILayout.Label($"/ {MaxPlayers}", EditorStyles.toolbarButton, GUILayout.Width(32));

            // ── Full-flow button ──────────────────────────────────────
            GUI.enabled = !_sequenceRunning;
            var flowTip = new GUIContent(
                "▶ Full Lobby Flow",
                $"Spawns {_spawnCount} player(s) and simulates the complete pre-game flow:\n\n" +
                "  ✕  Press 1 — Join  (Lobby)\n" +
                "  ✕  Press 2 — Ready Up  (Lobby)\n" +
                "  ⏱  Wait for 3 s countdown → CharacterSelect\n" +
                "  ✕  Press 3 — Confirm Character  (CharacterSelect)");
            if (GUILayout.Button(flowTip, EditorStyles.toolbarButton, GUILayout.Width(120)))
                StartFullLobbyFlowSequence();
            GUI.enabled = true;

            if (_sequenceRunning)
            {
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("Cancel", EditorStyles.toolbarButton, GUILayout.Width(55)))
                    CancelSequence();
                GUI.backgroundColor = prev;
            }

            int active = VirtualRewiredBridge.Instance.ActiveCount;
            GUILayout.Label($"Active: {active}/{MaxPlayers}", EditorStyles.toolbarButton);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);
        }

        // ── Section: Patrol toolbar ───────────────────────────────────
        private void DrawPatrolToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("Radius:", EditorStyles.toolbarButton, GUILayout.Width(48));
            _patrolRadius = EditorGUILayout.Slider(_patrolRadius, 1f, 50f, GUILayout.Width(130));
            GUILayout.Label($"{_patrolRadius:F0}m", EditorStyles.toolbarButton, GUILayout.Width(28));

            GUILayout.Label("Speed:", EditorStyles.toolbarButton, GUILayout.Width(44));
            float newSpeed = EditorGUILayout.Slider(_patrolSpeed, 0.1f, 1f, GUILayout.Width(110));
            if (!Mathf.Approximately(newSpeed, _patrolSpeed))
            {
                _patrolSpeed = newSpeed;
                // Push new global speed to all active patrols immediately
                VirtualControllerPatrol.Instance?.SetSpeedAll(_patrolSpeed);
            }
            GUILayout.Label($"{_patrolSpeed:F2}", EditorStyles.toolbarButton, GUILayout.Width(32));

            GUILayout.FlexibleSpace();

            // Sync global flag with actual patrol state so the button stays honest
            // if individual players were stopped manually.
            if (VirtualControllerPatrol.Instance != null)
            {
                bool anyPatrolling = false;
                for (int i = 0; i < MaxPlayers; i++)
                    if (VirtualControllerPatrol.Instance.IsPatrolling(i)) { anyPatrolling = true; break; }
                if (!anyPatrolling) _patrolAllActive = false;
            }

            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = _patrolAllActive ? new Color(0.4f, 1f, 0.4f) : Color.gray;
            string label = _patrolAllActive ? "⏹  Stop All Patrol" : "▶  Patrol All";
            if (GUILayout.Button(label, EditorStyles.toolbarButton, GUILayout.Width(135)))
            {
                EnsurePatrolComponent();
                if (_patrolAllActive)
                {
                    VirtualControllerPatrol.Instance.StopAll();
                    _patrolAllActive = false;
                }
                else
                {
                    VirtualControllerPatrol.Instance.StartAll(_patrolRadius, _patrolSpeed);
                    _patrolAllActive = true;
                }
            }
            GUI.backgroundColor = prev;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);
        }

        // ── Section: Player list ──────────────────────────────────────
        private void DrawPlayerList()
        {
            for (int i = 0; i < MaxPlayers; i++)
            {
                var state = VirtualControllerRegistry.Instance?.GetState(i);
                bool isActive = state != null;
                DrawPlayerRow(i, isActive, state);
            }
        }

        private void DrawPlayerRow(int playerIndex, bool isActive, VirtualControllerState state)
        {
            // Foldout header row
            Color bgColor = isActive
                ? new Color(0.18f, 0.32f, 0.18f)
                : new Color(0.22f, 0.22f, 0.22f);

            Rect rowRect = EditorGUILayout.GetControlRect(false, 24f);
            EditorGUI.DrawRect(rowRect, bgColor);

            // Foldout (only when active)
            Rect foldRect = new Rect(rowRect.x + 4, rowRect.y + 4, 16, 16);
            if (isActive)
                _foldout[playerIndex] = EditorGUI.Foldout(foldRect, _foldout[playerIndex], GUIContent.none);

            // Label (narrowed to make room for patrol button)
            Rect labelRect = new Rect(rowRect.x + 24, rowRect.y + 3, rowRect.width - 240, 18);
            string statusIcon = isActive ? "🟢" : "⚫";
            GUI.Label(labelRect, $"{statusIcon}  Player {playerIndex + 1}", EditorStyles.whiteLabel);

            // Per-player patrol toggle (only shown when controller is active)
            if (isActive)
            {
                bool patrolling = VirtualControllerPatrol.Instance?.IsPatrolling(playerIndex) ?? false;
                Rect patrolRect = new Rect(rowRect.xMax - 210, rowRect.y + 3, 95, 18);
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = patrolling ? new Color(0.4f, 1f, 0.4f) : new Color(0.7f, 0.7f, 0.7f);
                string patrolLabel = patrolling ? "⏹ Patrol" : "▶ Patrol";
                if (GUI.Button(patrolRect, patrolLabel, EditorStyles.miniButton))
                {
                    EnsurePatrolComponent();
                    if (patrolling)
                        VirtualControllerPatrol.Instance.StopPatrol(playerIndex);
                    else
                        VirtualControllerPatrol.Instance.StartPatrol(playerIndex, _patrolRadius, _patrolSpeed);
                }
                GUI.backgroundColor = prev;
            }

            // Add / Remove button
            Rect btnRect = new Rect(rowRect.xMax - 110, rowRect.y + 3, 105, 18);
            if (isActive)
            {
                if (GUI.Button(btnRect, "Remove", EditorStyles.miniButton))
                {
                    VirtualControllerPatrol.Instance?.StopPatrol(playerIndex);
                    VirtualRewiredBridge.Instance.RemoveController(playerIndex);
                }
            }
            else
            {
                if (GUI.Button(btnRect, "Add Controller", EditorStyles.miniButton))
                    VirtualRewiredBridge.Instance.SpawnController(playerIndex);
            }

            // Detail panel
            if (isActive && _foldout[playerIndex] && state != null)
            {
                EditorGUI.indentLevel++;
                DrawControllerDetail(state);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(2);
            }
        }

        // ── Section: Controller detail (buttons + axes) ───────────────
        private void DrawControllerDetail(VirtualControllerState s)
        {
            EditorGUILayout.Space(2);

            // ── Sticks ────────────────────────────────────────────────
            EditorGUILayout.LabelField("Sticks", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            DrawAxis("L-X",  ref s.leftStickX);
            DrawAxis("L-Y",  ref s.leftStickY);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            DrawAxis("R-X",  ref s.rightStickX);
            DrawAxis("R-Y",  ref s.rightStickY);
            EditorGUILayout.EndHorizontal();

            // ── Triggers ──────────────────────────────────────────────
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Triggers", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            DrawTrigger("L2", ref s.l2);
            DrawTrigger("R2", ref s.r2);
            EditorGUILayout.EndHorizontal();

            // ── Face buttons ──────────────────────────────────────────
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Face Buttons", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            DrawButton("Cross ✕",    ref s.cross);
            DrawButton("Circle ○",   ref s.circle);
            DrawButton("Square □",   ref s.square);
            DrawButton("Triangle △", ref s.triangle);
            EditorGUILayout.EndHorizontal();

            // ── Shoulders ─────────────────────────────────────────────
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Shoulders", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            DrawButton("L1", ref s.l1);
            DrawButton("R1", ref s.r1);
            DrawButton("L3", ref s.l3);
            DrawButton("R3", ref s.r3);
            EditorGUILayout.EndHorizontal();

            // ── D-Pad ─────────────────────────────────────────────────
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("D-Pad", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            DrawButton("↑ Up",   ref s.dpadUp);
            DrawButton("↓ Down", ref s.dpadDown);
            DrawButton("← Left", ref s.dpadLeft);
            DrawButton("→ Right",ref s.dpadRight);
            EditorGUILayout.EndHorizontal();

            // ── System ────────────────────────────────────────────────
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("System", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            DrawButton("Options",  ref s.options);
            DrawButton("Share",    ref s.share);
            DrawButton("Touchpad", ref s.touchpad);
            DrawButton("PS",       ref s.ps);
            EditorGUILayout.EndHorizontal();

            // ── Quick actions ─────────────────────────────────────────
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset All Inputs", GUILayout.Height(20)))
                s.Reset();
            if (GUILayout.Button("Simulate Left Move →", GUILayout.Height(20)))
            {
                s.Reset();
                s.leftStickX = 1f;
            }
            EditorGUILayout.EndHorizontal();

            // ── Per-player patrol speed ───────────────────────────────
            if (VirtualControllerPatrol.Instance != null &&
                VirtualControllerPatrol.Instance.IsActive(s.playerId))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Patrol", EditorStyles.boldLabel);
                float currentSpeed = VirtualControllerPatrol.Instance.GetSpeed(s.playerId);
                float newSpeed = EditorGUILayout.Slider("Speed", currentSpeed, 0.1f, 1f);
                if (!Mathf.Approximately(newSpeed, currentSpeed))
                    VirtualControllerPatrol.Instance.SetSpeed(s.playerId, newSpeed);
            }

            EditorGUILayout.Space(4);
        }

        // ── Helpers ───────────────────────────────────────────────────
        private static void DrawAxis(string label, ref float value)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(ColWidth));
            value = EditorGUILayout.Slider(label, value, -1f, 1f);
            EditorGUILayout.EndVertical();
        }

        private static void DrawTrigger(string label, ref float value)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(ColWidth));
            value = EditorGUILayout.Slider(label, value, 0f, 1f);
            EditorGUILayout.EndVertical();
        }

        private static void DrawButton(string label, ref bool value)
        {
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = value ? Color.green : Color.gray;

            // Allocate the button rect first so we can inspect the mouse event.
            var content  = new GUIContent(label);
            Rect rect    = GUILayoutUtility.GetRect(content, GUI.skin.button, GUILayout.Height(22));

            // Handle the click on MouseDown so the toggle fires on the FIRST click even when
            // this window was not previously focused (bypasses Unity's focus-then-click behavior).
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
            {
                value = !value;
                e.Use();            // consume the event so GUI.Button below doesn't also fire
                GUI.changed = true;
            }

            // Draw-only: pass the event-consumed rect through GUI.Button for visuals + hover state.
            // Because the MouseDown was already Used(), Button() will return false here — that's fine.
            GUI.Button(rect, content);

            GUI.backgroundColor = prev;
        }

        private void AddNextPlayer()
        {
            for (int i = 0; i < MaxPlayers; i++)
            {
                if (VirtualControllerRegistry.Instance.GetState(i) == null)
                {
                    VirtualRewiredBridge.Instance.SpawnController(i);
                    _foldout[i] = true;
                    return;
                }
            }
            Debug.LogWarning("[VirtualControllers] All 16 player slots are already active.");
        }

        /// <summary>
        /// Ensures a VirtualControllerPatrol component exists in the scene.
        /// Piggy-backs on the VirtualRewiredBridge GameObject when available.
        /// </summary>
        private static void EnsurePatrolComponent()
        {
            if (VirtualControllerPatrol.Instance != null) return;

            if (VirtualRewiredBridge.Instance != null)
            {
                VirtualRewiredBridge.Instance.gameObject.AddComponent<VirtualControllerPatrol>();
            }
            else
            {
                var go = new GameObject("[VirtualControllerPatrol]");
                go.AddComponent<VirtualControllerPatrol>();
            }
        }

        private static void AutoAddBridge()
        {
            var go = new GameObject("[VirtualRewiredBridge]");
            go.AddComponent<VirtualRewiredBridge>();
            UnityEditor.Selection.activeGameObject = go;
            Debug.Log("[VirtualControllers] Added VirtualRewiredBridge to scene. Configure it in the Inspector.");
        }

        // ── Full Lobby Flow sequence ──────────────────────────────────────────
        //
        // Simulates the complete pre-game path for _spawnCount virtual players:
        //
        //   Phase 1 — Spawn    : all controllers created simultaneously
        //   Phase 2 — Join     : each player presses ✕ once  → Lobby join
        //   Phase 3 — Ready Up : each player presses ✕ again → Lobby ready-up
        //                        (the last press starts the 3-second countdown)
        //   Phase 4 — Wait     : sit through the lobby countdown + state transition
        //   Phase 5 — Confirm  : each player presses ✕ once  → CharacterSelect confirm
        //
        // Timing uses EditorApplication.timeSinceStartup + EditorApplication.update
        // so all mutations stay on the editor main thread (same thread as OnGUI and
        // the game's Update loop — no threading hazards with PushToRewired).
        // ─────────────────────────────────────────────────────────────────────────
        private void StartFullLobbyFlowSequence()
        {
            if (_sequenceRunning) return;
            _sequenceRunning = true;
            _schedule.Clear();

            int    count   = Mathf.Clamp(_spawnCount, 1, MaxPlayers);
            const double Hold        = 0.15;  // seconds button is held per tap
            const double Gap         = 0.12;  // seconds between individual player taps
            const double Settle      = 0.35;  // pause after a phase before the next
            const double CountdownWait = 3.4; // lobby countdown (3 s) + transition buffer

            double t = EditorApplication.timeSinceStartup;

            // ── Phase 1: Spawn ────────────────────────────────────────────────
            for (int i = 0; i < count; i++)
            {
                int idx = i;
                _schedule.Enqueue((t, () =>
                {
                    if (VirtualControllerRegistry.Instance.GetState(idx) == null)
                    {
                        VirtualRewiredBridge.Instance.SpawnController(idx);
                        _foldout[idx] = true;
                    }
                }));
            }
            t += Settle;

            // ── Phase 2: Join (✕ press 1 per player) ─────────────────────────
            for (int i = 0; i < count; i++)
            {
                int idx      = i;
                double press = t;
                double rel   = t + Hold;
                t = rel + Gap;
                _schedule.Enqueue((press, () => { var s = VirtualControllerRegistry.Instance.GetState(idx); if (s != null) s.cross = true;  }));
                _schedule.Enqueue((rel,   () => { var s = VirtualControllerRegistry.Instance.GetState(idx); if (s != null) s.cross = false; }));
            }
            t += Settle;

            // ── Phase 3: Ready Up (✕ press 2 per player) ─────────────────────
            // The last player's press starts the lobby countdown.
            for (int i = 0; i < count; i++)
            {
                int idx      = i;
                double press = t;
                double rel   = t + Hold;
                t = rel + Gap;
                _schedule.Enqueue((press, () => { var s = VirtualControllerRegistry.Instance.GetState(idx); if (s != null) s.cross = true;  }));
                _schedule.Enqueue((rel,   () => { var s = VirtualControllerRegistry.Instance.GetState(idx); if (s != null) s.cross = false; }));
            }

            // ── Phase 4: Wait for lobby countdown → CharacterSelect ───────────
            t += CountdownWait;

            // ── Phase 5: Confirm Character (✕ press 3 per player) ────────────
            for (int i = 0; i < count; i++)
            {
                int idx      = i;
                double press = t;
                double rel   = t + Hold;
                t = rel + Gap;
                _schedule.Enqueue((press, () => { var s = VirtualControllerRegistry.Instance.GetState(idx); if (s != null) s.cross = true;  }));
                _schedule.Enqueue((rel,   () => { var s = VirtualControllerRegistry.Instance.GetState(idx); if (s != null) s.cross = false; }));
            }

            // ── Done ──────────────────────────────────────────────────────────
            _schedule.Enqueue((t, () =>
            {
                _sequenceRunning = false;
                EditorApplication.update -= TickSchedule;
                Repaint();
            }));

            EditorApplication.update += TickSchedule;
        }

        /// <summary>
        /// Aborts the running sequence immediately and resets all state.
        /// Safe to call at any time — idempotent if no sequence is running.
        /// </summary>
        private void CancelSequence()
        {
            _schedule.Clear();
            _sequenceRunning = false;
            EditorApplication.update -= TickSchedule;
            Repaint();
        }

        /// <summary>
        /// Called every editor update tick while a sequence is running.
        /// Fires all scheduled actions whose timestamp has passed.
        /// Runs entirely on the editor main thread — safe to touch any Unity state.
        /// If any action throws, the sequence is cancelled cleanly instead of
        /// leaving _sequenceRunning stuck as true.
        /// </summary>
        private void TickSchedule()
        {
            try
            {
                double now = EditorApplication.timeSinceStartup;
                while (_schedule.Count > 0 && _schedule.Peek().fireAt <= now)
                    _schedule.Dequeue().action();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VirtualControllers] Sequence aborted due to exception: {ex}");
                CancelSequence();
            }
        }
    }
}
