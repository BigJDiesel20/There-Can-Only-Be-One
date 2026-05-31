using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space overlay UI for the Lobby state.
/// Cards are arranged in a GridLayoutGroup (max 4 columns, wraps to more rows).
///
/// Card states:
///   Empty   — dark grey  | "Press [A] to Join"
///   Joined  — amber      | "Player N  ·  Press [A] to Ready"
///   Ready   — green      | "Player N  ·  ✓ Ready!"
///
/// When every joined player is ready a countdown panel appears.
/// </summary>
public class LobbyUI
{
    GameObject          _root;
    GridLayoutGroup     _glg;
    RectTransform       _gridRT;
    int                 _lastJoinedCount = -1;
    GameObject[]        _cards;
    Image[]             _slotBgs;
    TextMeshProUGUI[]   _slotStatus;
    GameObject          _countdownPanel;
    TextMeshProUGUI     _countdownNumber;
    TextMeshProUGUI     _footer;

    static readonly Color ColBg      = new Color(0.05f, 0.05f, 0.05f, 0.88f);
    static readonly Color ColEmpty   = new Color(0.15f, 0.15f, 0.15f, 1.00f);
    static readonly Color ColJoined  = new Color(0.55f, 0.38f, 0.04f, 1.00f);   // amber — joined, not ready
    static readonly Color ColReady   = new Color(0.08f, 0.48f, 0.12f, 1.00f);   // green  — ready

    const string FooterDefault   = "A: Join  |  A again: Ready Up  |  B: Leave / Cancel";
    const string FooterCountdown = "All players ready — get set!";

    // ── Public API ────────────────────────────────────────────────────────────

    public void Initialize(int slotCount)
    {
        _root = new GameObject("LobbyUI");

        var canvas          = _root.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler          = _root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight   = 0.5f;
        _root.AddComponent<GraphicRaycaster>();

        // Full-screen dark background
        MakePanel(_root.transform, "Bg", ColBg,
                  Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Title
        MakeTMP(_root.transform, "Title", "LOBBY",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -30f), new Vector2(0f, 100f),
                72f, FontStyles.Bold, Color.white);

        // ── Grid of slot cards ────────────────────────────────────────────────
        const float cellW = 165f, cellH = 115f, gap = 14f;
        int cols = Mathf.Min(slotCount, 4);

        var gridGO = new GameObject("SlotGrid", typeof(RectTransform));
        gridGO.transform.SetParent(_root.transform, false);
        _gridRT                 = gridGO.GetComponent<RectTransform>();
        _gridRT.anchorMin       = new Vector2(0.5f, 0.5f);
        _gridRT.anchorMax       = new Vector2(0.5f, 0.5f);
        _gridRT.pivot           = new Vector2(0.5f, 0.5f);
        _gridRT.anchoredPosition = new Vector2(0f, -10f);   // slight downward offset from centre
        _glg                = gridGO.AddComponent<GridLayoutGroup>();
        _glg.cellSize        = new Vector2(cellW, cellH);
        _glg.spacing         = new Vector2(gap, gap);
        _glg.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        _glg.startAxis       = GridLayoutGroup.Axis.Horizontal;
        _glg.childAlignment  = TextAnchor.MiddleCenter;
        _glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        _glg.constraintCount = cols;

        // Auto-size the container to fit the grid content
        var csf = gridGO.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        _cards      = new GameObject[slotCount];
        _slotBgs    = new Image[slotCount];
        _slotStatus = new TextMeshProUGUI[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            // Card background — direct child of the grid so GridLayoutGroup sizes it
            var card = new GameObject($"Slot{i + 1}", typeof(RectTransform));
            card.transform.SetParent(gridGO.transform, false);
            card.SetActive(false);   // hidden until this player joins
            _cards[i] = card;

            var bg = card.AddComponent<Image>();
            bg.color   = ColJoined;
            _slotBgs[i] = bg;

            // "P1" / "P2" … label at top of card
            MakeTMP(card.transform, "PNum", $"P{i + 1}",
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -6f), new Vector2(0f, 26f),
                    18f, FontStyles.Bold, new Color(0.80f, 0.80f, 0.80f));

            // Status text
            _slotStatus[i] = MakeTMP(card.transform, "Status", "Press [A]\nto Join",
                    new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, -10f), new Vector2(-8f, -32f),
                    13f, FontStyles.Normal, new Color(0.55f, 0.55f, 0.55f));
        }

        // ── Countdown panel (hidden until all players are ready) ───────────────
        _countdownPanel = MakePanel(_root.transform, "CountdownPanel",
                                     new Color(0f, 0f, 0f, 0.70f),
                                     new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                     new Vector2(0f, -165f), new Vector2(280f, 130f));

        MakeTMP(_countdownPanel.transform, "Label", "STARTING IN",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -14f), new Vector2(0f, 32f),
                20f, FontStyles.Normal, new Color(0.85f, 0.85f, 0.85f));

        _countdownNumber = MakeTMP(_countdownPanel.transform, "Number", "3",
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -10f), new Vector2(0f, -48f),
                78f, FontStyles.Bold, Color.white);

        _countdownPanel.SetActive(false);

        // ── Footer ────────────────────────────────────────────────────────────
        _footer = MakeTMP(_root.transform, "Footer", FooterDefault,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 18f), new Vector2(0f, 38f),
                17f, FontStyles.Normal, new Color(0.75f, 0.75f, 0.75f));
    }

    /// <summary>Call every frame from Lobby.OnUpdate().</summary>
    public void Refresh(bool[] isJoinConfirmed, bool[] lobbyConfirmed,
                        float countdown, bool countdownActive)
    {
        if (_root == null) return;

        // ── Show/hide cards and update column count ───────────────────────────
        int joinedCount = 0;
        for (int i = 0; i < isJoinConfirmed.Length; i++)
            if (isJoinConfirmed[i]) joinedCount++;

        if (joinedCount != _lastJoinedCount)
        {
            // Show only joined players — unjoined cards are removed from the grid flow
            for (int i = 0; i < _cards.Length && i < isJoinConfirmed.Length; i++)
                _cards[i].SetActive(isJoinConfirmed[i]);

            // Grow the grid: 1 col → 2 cols → 3 cols → 4 cols as players join
            _glg.constraintCount = Mathf.Clamp(joinedCount, 1, 4);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_gridRT);
            _lastJoinedCount = joinedCount;
        }

        // ── Update card content for joined players ────────────────────────────
        for (int i = 0; i < _slotBgs.Length && i < isJoinConfirmed.Length; i++)
        {
            if (!isJoinConfirmed[i]) continue;

            bool ready = lobbyConfirmed[i];

            if (!ready)
            {
                _slotBgs[i].color    = ColJoined;
                _slotStatus[i].text  = $"Player {i + 1}\nPress [A] to Ready";
                _slotStatus[i].color = Color.white;
            }
            else
            {
                _slotBgs[i].color    = ColReady;
                _slotStatus[i].text  = $"Player {i + 1}\n✓  Ready!";
                _slotStatus[i].color = Color.white;
            }
        }

        _countdownPanel.SetActive(countdownActive);
        if (countdownActive)
            _countdownNumber.text = Mathf.CeilToInt(countdown).ToString();

        _footer.text = countdownActive ? FooterCountdown : FooterDefault;
    }

    /// <summary>Tears down the canvas. Call from Lobby.OnExit().</summary>
    public void Destroy()
    {
        if (_root != null) Object.Destroy(_root);
        _root = null; _glg = null; _gridRT = null;
        _cards = null; _slotBgs = null; _slotStatus = null;
        _countdownPanel = null; _countdownNumber = null; _footer = null;
        _lastJoinedCount = -1;
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
