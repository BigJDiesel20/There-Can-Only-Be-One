using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space overlay UI for the PostGame state.
///
/// Layout (top → bottom, centred):
///   • "MATCH OVER" header
///   • Winner announcement panel (name + crown emoji)
///   • Four navigable option rows: Replay / Choose Characters / Leave / Quit
///   • Selected row highlighted in amber; others dim
///   • Footer hint bar at the bottom
/// </summary>
public class PostGameUI
{
    GameObject          _root;
    TextMeshProUGUI     _winnerLabel;
    GameObject[]        _rowBgs;
    TextMeshProUGUI[]   _rowLabels;

    static readonly Color ColBg          = new Color(0.05f, 0.05f, 0.05f, 0.92f);
    static readonly Color ColWinnerPanel = new Color(0.10f, 0.10f, 0.10f, 1.00f);
    static readonly Color ColRowNormal   = new Color(0.12f, 0.12f, 0.12f, 1.00f);
    static readonly Color ColRowSelected = new Color(0.55f, 0.38f, 0.04f, 1.00f);   // amber
    static readonly Color ColLabelNormal = new Color(0.88f, 0.88f, 0.88f, 1.00f);
    static readonly Color ColLabelSel    = new Color(1.00f, 1.00f, 1.00f, 1.00f);
    static readonly Color ColWinnerName  = new Color(1.00f, 0.85f, 0.20f, 1.00f);   // gold

    // Row indices — must match PostGame.cs constants
    const int RowReplay   = 0;
    const int RowNewChars = 1;
    const int RowLeave    = 2;
    const int RowQuit     = 3;
    const int RowCount    = 4;

    static readonly string[] RowLabels =
    {
        "Replay",
        "Choose Characters",
        "Leave",
        "Quit"
    };

    const float RowW   = 500f;
    const float RowH   = 68f;
    const float RowGap = 10f;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the canvas. Call from PostGame.OnLoad().
    /// <paramref name="winnerName"/> is displayed in the winner banner.
    /// </summary>
    public void Initialize(string winnerName)
    {
        _root = new GameObject("PostGameUI");

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

        // ── "MATCH OVER" header ───────────────────────────────────────────────
        MakeTMP(_root.transform, "Header", "MATCH OVER",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -36f), new Vector2(0f, 90f),
                58f, FontStyles.Bold, Color.white);

        // ── Winner banner ─────────────────────────────────────────────────────
        var winnerPanel = MakePanel(_root.transform, "WinnerPanel", ColWinnerPanel,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 120f), new Vector2(640f, 130f));

        MakeTMP(winnerPanel.transform, "Crown", "👑",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                new Vector2(0f, 28f), Vector2.zero,
                36f, FontStyles.Normal, Color.white);

        _winnerLabel = MakeTMP(winnerPanel.transform, "WinnerName",
                string.IsNullOrEmpty(winnerName) ? "WINNER!" : winnerName.ToUpper() + "  WINS!",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                new Vector2(0f, -18f), Vector2.zero,
                38f, FontStyles.Bold, ColWinnerName);

        // ── Option rows (vertical stack, centred below banner) ────────────────
        _rowBgs    = new GameObject[RowCount];
        _rowLabels = new TextMeshProUGUI[RowCount];

        float totalH = RowCount * RowH + (RowCount - 1) * RowGap;
        float startY = -60f;   // top of first row relative to canvas centre

        for (int i = 0; i < RowCount; i++)
        {
            float rowCentreY = startY - i * (RowH + RowGap) - RowH * 0.5f;

            _rowBgs[i] = MakePanel(_root.transform, $"Row{i}", ColRowNormal,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, rowCentreY), new Vector2(RowW, RowH));

            _rowLabels[i] = MakeTMP(_rowBgs[i].transform, "Label", RowLabels[i],
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                    Vector2.zero, Vector2.zero,
                    24f, FontStyles.Normal, ColLabelNormal);
        }

        // ── Footer ────────────────────────────────────────────────────────────
        MakeTMP(_root.transform, "Footer",
                "↑↓ Navigate  |  A: Confirm",
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 18f), new Vector2(0f, 38f),
                17f, FontStyles.Normal, new Color(0.75f, 0.75f, 0.75f));
    }

    /// <summary>
    /// Updates the highlighted row. Call every frame from PostGame.OnUpdate().
    /// </summary>
    public void Refresh(int selectedRow)
    {
        if (_root == null) return;

        for (int i = 0; i < RowCount; i++)
        {
            bool selected = (i == selectedRow);
            _rowBgs[i].GetComponent<Image>().color = selected ? ColRowSelected : ColRowNormal;
            _rowLabels[i].color                    = selected ? ColLabelSel    : ColLabelNormal;
        }
    }

    /// <summary>Tears down the canvas. Call from PostGame.OnExit().</summary>
    public void Destroy()
    {
        if (_root != null) Object.Destroy(_root);
        _root = null; _winnerLabel = null; _rowBgs = null; _rowLabels = null;
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
