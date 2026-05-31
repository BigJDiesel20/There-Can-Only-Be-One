using UnityEngine;
using System;

/// <summary>
/// IMGUI debug overlay shown only during Battle.
/// Renders a collapsible panel in the top-left corner.
///   Expanded — full panel with player-number input, Force Win button,
///              and a ▲ compress button in the title bar.
///   Collapsed — small black box with a ▼ expand button.
///
/// Usage: BattleDebugUI.Create(callback) on Battle load;
///        instance.DestroyUI()           on Battle exit.
/// </summary>
public class BattleDebugUI : MonoBehaviour
{
    // ── Layout ────────────────────────────────────────────────────────────────
    const float PanelW    = 230f;
    const float PanelH    = 120f;
    const float Padding   = 12f;
    const float RowH      = 28f;
    const float IconBtnSz = 22f;   // square size of the collapse / expand button
    const float CollapsedSz = 28f; // size of the collapsed black box

    // ── State ─────────────────────────────────────────────────────────────────
    string      _input      = "1";
    Action<int> _onForceWin;
    bool        _collapsed  = false;

    // ── IMGUI styles (built once in OnGUI) ────────────────────────────────────
    GUIStyle _styleBox;
    GUIStyle _styleLabel;
    GUIStyle _styleField;
    GUIStyle _styleButton;
    GUIStyle _styleIconBtn;
    GUIStyle _styleCollapsedBox;
    bool     _stylesBuilt;

    // ── Factory ───────────────────────────────────────────────────────────────

    public static BattleDebugUI Create(Action<int> onForceWin)
    {
        var go       = new GameObject("BattleDebugUI");
        var instance = go.AddComponent<BattleDebugUI>();
        instance._onForceWin = onForceWin;
        return instance;
    }

    public void DestroyUI() => Destroy(gameObject);

    // ── IMGUI ─────────────────────────────────────────────────────────────────

    void OnGUI()
    {
        BuildStyles();

        if (_collapsed)
            DrawCollapsed();
        else
            DrawExpanded();
    }

    // ── Collapsed view — tiny black box with ▼ expand button ─────────────────

    void DrawCollapsed()
    {
        float x = Padding;
        float y = Padding;

        // Black backing box
        GUI.Box(new Rect(x, y, CollapsedSz, CollapsedSz), GUIContent.none, _styleCollapsedBox);

        // ▼ expand button fills the box
        if (GUI.Button(new Rect(x, y, CollapsedSz, CollapsedSz), "▼", _styleIconBtn))
            _collapsed = false;
    }

    // ── Expanded view — full panel with ▲ compress button in title bar ────────

    void DrawExpanded()
    {
        float x = Padding;
        float y = Padding;

        // Panel background
        GUI.Box(new Rect(x, y, PanelW, PanelH), GUIContent.none, _styleBox);

        // Title text (left-aligned inside panel)
        GUI.Label(
            new Rect(x + Padding, y + 6f, PanelW - Padding * 2f - IconBtnSz - 4f, 22f),
            "  DEBUG  |  Force Win",
            _styleBox);

        // ▲ compress button — top-right corner of the panel
        float btnX = x + PanelW - IconBtnSz - 6f;
        float btnY = y + 5f;
        if (GUI.Button(new Rect(btnX, btnY, IconBtnSz, IconBtnSz), "▲", _styleIconBtn))
            _collapsed = true;

        // ── Content rows ──────────────────────────────────────────────────────
        float cx = x + Padding;
        float cy = y + 34f;

        // Row 1: label + number input
        GUI.Label(new Rect(cx, cy, 90f, RowH), "Player #", _styleLabel);

        _input = GUI.TextField(
            new Rect(cx + 94f, cy + 1f, 64f, RowH - 2f),
            _input, maxLength: 2,
            style: _styleField);

        cy += RowH + 8f;

        // Row 2: Force Win button
        if (GUI.Button(new Rect(cx, cy, PanelW - Padding * 2f, 32f),
                       "FORCE WIN", _styleButton))
        {
            if (int.TryParse(_input.Trim(), out int playerNum))
                _onForceWin?.Invoke(playerNum);
            else
                Debug.LogWarning("[BattleDebugUI] Enter a valid player number.");
        }
    }

    // ── Style builder ─────────────────────────────────────────────────────────

    void BuildStyles()
    {
        if (_stylesBuilt) return;
        _stylesBuilt = true;

        // Box / panel
        _styleBox = new GUIStyle(GUI.skin.box);
        _styleBox.normal.background = MakeTex(new Color(0.06f, 0.06f, 0.06f, 0.92f));
        _styleBox.normal.textColor  = new Color(1.00f, 0.60f, 0.10f);
        _styleBox.fontStyle         = FontStyle.Bold;
        _styleBox.fontSize          = 13;
        _styleBox.alignment         = TextAnchor.UpperLeft;
        _styleBox.padding           = new RectOffset(10, 8, 7, 6);

        // Label
        _styleLabel = new GUIStyle(GUI.skin.label);
        _styleLabel.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
        _styleLabel.fontSize         = 14;
        _styleLabel.alignment        = TextAnchor.MiddleLeft;

        // Text field
        _styleField = new GUIStyle(GUI.skin.textField);
        _styleField.normal.background  = MakeTex(new Color(0.18f, 0.18f, 0.18f));
        _styleField.normal.textColor   = Color.white;
        _styleField.focused.background = MakeTex(new Color(0.25f, 0.25f, 0.28f));
        _styleField.focused.textColor  = Color.white;
        _styleField.hover.background   = MakeTex(new Color(0.22f, 0.22f, 0.22f));
        _styleField.hover.textColor    = Color.white;
        _styleField.fontSize           = 15;
        _styleField.alignment          = TextAnchor.MiddleCenter;
        _styleField.padding            = new RectOffset(4, 4, 2, 2);

        // Force Win button
        _styleButton = new GUIStyle(GUI.skin.button);
        _styleButton.normal.background  = MakeTex(new Color(0.70f, 0.12f, 0.08f));
        _styleButton.normal.textColor   = Color.white;
        _styleButton.hover.background   = MakeTex(new Color(0.85f, 0.18f, 0.10f));
        _styleButton.hover.textColor    = Color.white;
        _styleButton.active.background  = MakeTex(new Color(0.50f, 0.08f, 0.05f));
        _styleButton.active.textColor   = Color.white;
        _styleButton.fontSize           = 14;
        _styleButton.fontStyle          = FontStyle.Bold;
        _styleButton.alignment          = TextAnchor.MiddleCenter;

        // ▲ / ▼ icon button (shared by both states)
        _styleIconBtn = new GUIStyle(GUI.skin.button);
        _styleIconBtn.normal.background  = MakeTex(new Color(0.20f, 0.20f, 0.20f, 0.95f));
        _styleIconBtn.normal.textColor   = new Color(1.00f, 0.60f, 0.10f);
        _styleIconBtn.hover.background   = MakeTex(new Color(0.30f, 0.30f, 0.30f, 0.95f));
        _styleIconBtn.hover.textColor    = Color.white;
        _styleIconBtn.active.background  = MakeTex(new Color(0.12f, 0.12f, 0.12f, 0.95f));
        _styleIconBtn.active.textColor   = Color.white;
        _styleIconBtn.fontSize           = 13;
        _styleIconBtn.fontStyle          = FontStyle.Bold;
        _styleIconBtn.alignment          = TextAnchor.MiddleCenter;
        _styleIconBtn.padding            = new RectOffset(0, 0, 0, 0);

        // Collapsed box background
        _styleCollapsedBox = new GUIStyle(GUI.skin.box);
        _styleCollapsedBox.normal.background = MakeTex(new Color(0.06f, 0.06f, 0.06f, 0.92f));
        _styleCollapsedBox.padding           = new RectOffset(0, 0, 0, 0);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static Texture2D MakeTex(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }
}
