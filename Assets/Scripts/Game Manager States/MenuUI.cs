using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space overlay UI for the Menu state.
///
/// Two panels share the canvas:
///
///   Top-level panel  — Online / Offline / Quit
///   Offline submenu  — Game Mode ◄► / Start / Back
///
/// Only one panel is visible at a time.  Call ShowSubmenu / HideSubmenu to
/// transition between them.  RefreshTopLevel and RefreshSubmenu update the
/// highlighted row within their respective panels.
/// </summary>
public class MenuUI
{
    // ── Canvas root ───────────────────────────────────────────────────────────
    GameObject _root;

    // ── Top-level panel ───────────────────────────────────────────────────────
    GameObject          _topPanel;
    GameObject[]        _topRowBgs;
    TextMeshProUGUI[]   _topRowLabels;
    TextMeshProUGUI     _topFooter;

    // ── Offline submenu panel ─────────────────────────────────────────────────
    GameObject          _subPanel;
    GameObject[]        _subRowBgs;
    TextMeshProUGUI[]   _subRowLabels;
    TextMeshProUGUI     _subModeValue;   // right-hand value on the Game Mode row
    TextMeshProUGUI     _subFooter;

    // ── Colours ───────────────────────────────────────────────────────────────
    static readonly Color ColBg          = new Color(0.05f, 0.05f, 0.05f, 0.88f);
    static readonly Color ColRowNormal   = new Color(0.12f, 0.12f, 0.12f, 1.00f);
    static readonly Color ColRowSelected = new Color(0.55f, 0.38f, 0.04f, 1.00f);   // amber
    static readonly Color ColRowDisabled = new Color(0.10f, 0.10f, 0.10f, 1.00f);
    static readonly Color ColLabelNormal = new Color(0.88f, 0.88f, 0.88f, 1.00f);
    static readonly Color ColLabelSel    = new Color(1.00f, 1.00f, 1.00f, 1.00f);
    static readonly Color ColLabelDim    = new Color(0.45f, 0.45f, 0.45f, 1.00f);
    static readonly Color ColValue       = new Color(0.80f, 0.80f, 0.80f, 1.00f);
    static readonly Color ColSubBg       = new Color(0.08f, 0.08f, 0.12f, 0.96f);

    // ── Top-level rows ────────────────────────────────────────────────────────
    const int TopRowOnline  = 0;
    const int TopRowOffline = 1;
    const int TopRowQuit    = 2;
    const int TopRowCount   = 3;
    static readonly string[] TopLabels = { "Online", "Offline", "Quit" };

    // ── Submenu rows ──────────────────────────────────────────────────────────
    const int SubRowGameMode = 0;
    const int SubRowStart    = 1;
    const int SubRowBack     = 2;
    const int SubRowCount    = 3;
    static readonly string[] SubLabels = { "Game Mode", "Start", "Back" };

    // ── Layout constants ──────────────────────────────────────────────────────
    const float RowW   = 520f;
    const float RowH   = 72f;
    const float RowGap = 12f;

    // ── Public API ────────────────────────────────────────────────────────────

    public void Initialize()
    {
        _root = new GameObject("MenuUI");

        var canvas          = _root.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler          = _root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;
        _root.AddComponent<GraphicRaycaster>();

        // Full-screen background
        MakePanel(_root.transform, "Bg", ColBg,
                  Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // ── Shared title ──────────────────────────────────────────────────────
        MakeTMP(_root.transform, "Title", "THERE CAN ONLY BE ONE",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -36f), new Vector2(0f, 90f),
                54f, FontStyles.Bold, Color.white);

        // ── Build panels ──────────────────────────────────────────────────────
        BuildTopLevelPanel();
        BuildSubmenuPanel();

        // Submenu starts hidden
        _subPanel.SetActive(false);
    }

    // ── Top-level panel ───────────────────────────────────────────────────────

    void BuildTopLevelPanel()
    {
        _topPanel = new GameObject("TopPanel", typeof(RectTransform));
        _topPanel.transform.SetParent(_root.transform, false);
        var rt        = _topPanel.GetComponent<RectTransform>();
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;

        _topRowBgs    = new GameObject[TopRowCount];
        _topRowLabels = new TextMeshProUGUI[TopRowCount];

        float totalH  = TopRowCount * RowH + (TopRowCount - 1) * RowGap;
        float startY  = totalH * 0.5f;

        for (int i = 0; i < TopRowCount; i++)
        {
            float rowCentreY = startY - i * (RowH + RowGap) - RowH * 0.5f;

            _topRowBgs[i] = MakePanel(_topPanel.transform, $"Row{i}", ColRowNormal,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, rowCentreY), new Vector2(RowW, RowH));

            _topRowLabels[i] = MakeTMP(_topRowBgs[i].transform, "Label", TopLabels[i],
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                    Vector2.zero, Vector2.zero,
                    26f, FontStyles.Normal, ColLabelNormal);
        }

        _topFooter = MakeTMP(_topPanel.transform, "Footer",
                "↑↓ Navigate  |  A: Confirm",
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 18f), new Vector2(0f, 38f),
                17f, FontStyles.Normal, new Color(0.75f, 0.75f, 0.75f));
    }

    // ── Offline submenu panel ─────────────────────────────────────────────────

    void BuildSubmenuPanel()
    {
        // Slightly inset card so it feels like it sits over the main menu
        _subPanel = MakePanel(_root.transform, "SubmenuPanel", ColSubBg,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 0f), new Vector2(RowW + 60f, SubRowCount * (RowH + RowGap) + 140f));

        // Submenu title
        MakeTMP(_subPanel.transform, "SubTitle", "OFFLINE",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -20f), new Vector2(0f, 58f),
                34f, FontStyles.Bold, Color.white);

        _subRowBgs    = new GameObject[SubRowCount];
        _subRowLabels = new TextMeshProUGUI[SubRowCount];

        float totalH = SubRowCount * RowH + (SubRowCount - 1) * RowGap;
        float startY = totalH * 0.5f - 10f;   // slight downward shift inside card

        for (int i = 0; i < SubRowCount; i++)
        {
            float rowCentreY = startY - i * (RowH + RowGap) - RowH * 0.5f;

            _subRowBgs[i] = MakePanel(_subPanel.transform, $"SubRow{i}", ColRowNormal,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, rowCentreY), new Vector2(RowW, RowH));

            if (i == SubRowGameMode)
            {
                // Left label + right value for the Game Mode row
                var lbl = MakeTMP(_subRowBgs[i].transform, "Label", SubLabels[i],
                        new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 0.5f),
                        new Vector2(20f, 0f), new Vector2(0f, 0f),
                        24f, FontStyles.Normal, ColLabelNormal);
                lbl.alignment = TextAlignmentOptions.MidlineLeft;
                _subRowLabels[i] = lbl;

                _subModeValue = MakeTMP(_subRowBgs[i].transform, "Value", "",
                        new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f),
                        new Vector2(-20f, 0f), new Vector2(0f, 0f),
                        22f, FontStyles.Normal, ColValue);
                _subModeValue.alignment = TextAlignmentOptions.MidlineRight;
            }
            else
            {
                _subRowLabels[i] = MakeTMP(_subRowBgs[i].transform, "Label", SubLabels[i],
                        Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                        Vector2.zero, Vector2.zero,
                        26f, FontStyles.Normal, ColLabelNormal);
            }
        }

        // Submenu footer
        _subFooter = MakeTMP(_subPanel.transform, "SubFooter",
                "↑↓ Navigate  |  ◄► Change Mode  |  A: Confirm  |  B: Back",
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 12f), new Vector2(0f, 34f),
                15f, FontStyles.Normal, new Color(0.75f, 0.75f, 0.75f));
    }

    // ── Refresh helpers ───────────────────────────────────────────────────────

    /// <summary>Updates the top-level row highlight. Call from Menu whenever selection changes.</summary>
    public void RefreshTopLevel(int selectedRow)
    {
        if (_root == null) return;

        for (int i = 0; i < TopRowCount; i++)
        {
            bool selected = (i == selectedRow);
            bool disabled = (i == TopRowOnline);

            _topRowBgs[i].GetComponent<Image>().color =
                selected ? ColRowSelected :
                disabled ? ColRowDisabled :
                           ColRowNormal;

            _topRowLabels[i].color =
                selected ? ColLabelSel  :
                disabled ? ColLabelDim  :
                           ColLabelNormal;
        }
    }

    /// <summary>Shows the submenu panel and sets initial state.</summary>
    public void ShowSubmenu(string modeName, bool hasMultipleModes, int selectedSubRow)
    {
        if (_root == null) return;
        _subPanel.SetActive(true);
        RefreshSubmenu(modeName, hasMultipleModes, selectedSubRow);
    }

    /// <summary>Updates the submenu row highlight and Game Mode value.</summary>
    public void RefreshSubmenu(string modeName, bool hasMultipleModes, int selectedSubRow)
    {
        if (_root == null) return;

        for (int i = 0; i < SubRowCount; i++)
        {
            bool selected = (i == selectedSubRow);

            _subRowBgs[i].GetComponent<Image>().color = selected ? ColRowSelected : ColRowNormal;
            _subRowLabels[i].color                    = selected ? ColLabelSel    : ColLabelNormal;

            if (i == SubRowGameMode && _subModeValue != null)
            {
                _subModeValue.text  = hasMultipleModes ? $"◄  {modeName}  ►" : modeName;
                _subModeValue.color = selected ? ColLabelSel : ColValue;
            }
        }
    }

    /// <summary>Hides the submenu and restores the top-level highlight.</summary>
    public void HideSubmenu(int topSelectedRow)
    {
        if (_root == null) return;
        _subPanel.SetActive(false);
        RefreshTopLevel(topSelectedRow);
    }

    /// <summary>Tears down the canvas. Call from Menu.OnExit().</summary>
    public void Destroy()
    {
        if (_root != null) Object.Destroy(_root);
        _root = null;
        _topPanel = null; _topRowBgs = null; _topRowLabels = null; _topFooter = null;
        _subPanel = null; _subRowBgs = null; _subRowLabels = null;
        _subModeValue = null; _subFooter = null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static GameObject MakePanel(Transform parent, string name, Color color,
                                 Vector2 anchorMin, Vector2 anchorMax,
                                 Vector2 pos, Vector2 size)
    {
        var go              = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt              = go.GetComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        go.AddComponent<Image>().color = color;
        return go;
    }

    static TextMeshProUGUI MakeTMP(Transform parent, string name, string text,
                                    Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
                                    Vector2 pos, Vector2 size,
                                    float fontSize, FontStyles style, Color color)
    {
        var go              = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt              = go.GetComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        var tmp             = go.AddComponent<TextMeshProUGUI>();
        tmp.text            = text;
        tmp.fontSize        = fontSize;
        tmp.fontStyle       = style;
        tmp.color           = color;
        tmp.alignment       = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }
}
