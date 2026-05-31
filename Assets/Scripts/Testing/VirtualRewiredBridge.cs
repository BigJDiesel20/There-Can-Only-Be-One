using System.Collections.Generic;
using System.Linq;
using Rewired;
using UnityEngine;
using VirtualControllers;

namespace VirtualControllers.Rewired
{
    /// <summary>
    /// MonoBehaviour driver that wires virtual controllers into Rewired at runtime.
    ///
    /// QUICK START
    /// ───────────
    /// 1. Place this component on a persistent GameObject in your test scene
    ///    (or your game bootstrap object).
    /// 2. Set `autoSpawnCount` to how many virtual players you want at startup (0 = none).
    /// 3. In your Rewired Input Manager asset:
    ///    • Add a "Custom Controller" definition named "VirtualPS4".
    ///    • Add 6 axes (LeftStickX, LeftStickY, RightStickX, RightStickY, L2, R2).
    ///    • Add 16 buttons in the order defined by VirtualRewiredController.ButtonId.
    ///    • Set `customControllerTemplateId` on this component to match that template's ID
    ///      (shown in the Rewired editor — default is 0 if it's your first custom controller).
    /// 4. Your existing Action maps work unchanged; just map Actions → VirtualPS4 buttons/axes
    ///    in the Rewired Input Manager like you would for any real controller.
    ///
    /// RUNTIME API
    /// ───────────
    ///   VirtualRewiredBridge.Instance.SpawnController(playerIndex);
    ///   VirtualRewiredBridge.Instance.RemoveController(playerIndex);
    ///   VirtualRewiredBridge.Instance.GetState(playerIndex).cross = true; // press button
    /// </summary>
    public class VirtualRewiredBridge : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────
        public static VirtualRewiredBridge Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────
        [Header("Rewired")]
        [Tooltip("The Rewired Custom Controller template ID (check your Rewired asset).")]
        public int customControllerTemplateId = 0;

        [Header("Auto-spawn")]
        [Range(0, 16)]
        [Tooltip("Number of virtual controllers to spawn automatically on Start().")]
        public int autoSpawnCount = 1;

        // ── Private state ─────────────────────────────────────────────
        private readonly Dictionary<int, VirtualRewiredController> _bridges =
            new Dictionary<int, VirtualRewiredController>();

        // ── Lifecycle ─────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Keep the game loop running when the Game View loses focus in the editor.
            // Without this, clicking the Virtual Controller Manager window pauses Update()
            // and PushToRewired() stops firing until the Game View is clicked again.
            Application.runInBackground = true;
        }

        private void Start()
        {
            for (int i = 0; i < autoSpawnCount; i++)
                SpawnController(i);
        }

        private void Update()
        {
            foreach (var bridge in _bridges.Values)
                bridge.PushToRewired();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ────────────────────────────────────────────────

        /// <summary>
        /// Creates a virtual controller for playerIndex and assigns it to the matching
        /// Rewired Player. Does nothing if one already exists for that index.
        /// </summary>
        public VirtualRewiredController SpawnController(int playerIndex)
        {
            if (_bridges.TryGetValue(playerIndex, out var existing))
            {
                Debug.LogWarning($"[VirtualRewiredBridge] Controller for player {playerIndex + 1} already exists.");
                return existing;
            }

            // 1. Register state in the registry
            var state = VirtualControllerRegistry.Instance.AddController(playerIndex);

            // 2. Create a Rewired Custom Controller
            CustomController cc = ReInput.controllers.CreateCustomController(customControllerTemplateId);
            if (cc == null)
            {
                Debug.LogError($"[VirtualRewiredBridge] Failed to create Rewired CustomController " +
                               $"(template id {customControllerTemplateId}). " +
                               "Check your Rewired Input Manager setup.");
                return null;
            }

            // 3. Assign controller to the Rewired Player
            Player rewiredPlayer = ReInput.players.GetPlayer(playerIndex);
            if (rewiredPlayer == null)
            {
                Debug.LogError($"[VirtualRewiredBridge] No Rewired Player found for index {playerIndex}. " +
                               "Make sure your Rewired Input Manager has enough players defined.");
                ReInput.controllers.DestroyCustomController(cc);
                return null;
            }
            rewiredPlayer.controllers.AddController(cc, removeFromOtherPlayers: false);

            // 3b. Explicitly load default maps — Rewired does NOT auto-load maps for
            //     dynamically created custom controllers, so we force it here.
            rewiredPlayer.controllers.maps.LoadDefaultMaps(ControllerType.Custom);

            // 3c. Debug: report how many maps are now active for this player
            int mapCount = 0;
            foreach (var m in rewiredPlayer.controllers.maps.GetMaps(ControllerType.Custom, cc.id))
            {
                mapCount++;
                Debug.Log($"[VirtualRewiredBridge] Player {playerIndex + 1} — map loaded: " +
                          $"id={m.id} cat={m.categoryId} layout={m.layoutId} enabled={m.enabled} " +
                          $"actionMaps={m.AllMaps.Count()}");
            }
            if (mapCount == 0)
                Debug.LogWarning($"[VirtualRewiredBridge] Player {playerIndex + 1}: no custom controller maps " +
                                 "were loaded! Check 'customControllerMaps' in the Rewired InputManager.");

            // 4. Build bridge
            var bridge = new VirtualRewiredController(state, cc);
            _bridges[playerIndex] = bridge;

            Debug.Log($"[VirtualRewiredBridge] Virtual PS4 controller spawned for player {playerIndex + 1}.");
            return bridge;
        }

        /// <summary>Removes the virtual controller for the given player index.</summary>
        public void RemoveController(int playerIndex)
        {
            if (!_bridges.TryGetValue(playerIndex, out var bridge)) return;

            Player rewiredPlayer = ReInput.players.GetPlayer(playerIndex);
            rewiredPlayer?.controllers.RemoveController(bridge.RewiredController);
            ReInput.controllers.DestroyCustomController(bridge.RewiredController);

            VirtualControllerRegistry.Instance.RemoveController(playerIndex);
            _bridges.Remove(playerIndex);

            Debug.Log($"[VirtualRewiredBridge] Virtual controller removed for player {playerIndex + 1}.");
        }

        /// <summary>Returns the state object you can mutate to simulate input.</summary>
        public VirtualControllerState GetState(int playerIndex)
            => VirtualControllerRegistry.Instance.GetState(playerIndex);

        /// <summary>Returns the VirtualRewiredController bridge (null if not spawned).</summary>
        public VirtualRewiredController GetBridge(int playerIndex)
            => _bridges.TryGetValue(playerIndex, out var b) ? b : null;

        public int ActiveCount => _bridges.Count;
    }
}
