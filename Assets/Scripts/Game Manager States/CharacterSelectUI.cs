using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space overlay UI for the CharacterSelect state.
///
/// Cards are arranged in a GridLayoutGroup (max 4 columns).
/// Only joined player slots are shown; the grid re-flows automatically
/// when players join or leave.
///
/// Each card shows:
///   • A sprite thumbnail from GameManager.characterThumbnails (falls back to a
///     grey placeholder if that index has no sprite assigned yet).
///   • The character's name (from the prefab asset name).
///   • The player's slot label.
///   • Browse / Confirmed status.
///
/// To set up thumbnails:
///   1. Take a screenshot of each character model in the Unity Editor.
///   2. Import them as Sprite assets.
///   3. Assign them to GameManager → Character Thumbnails in the Inspector,
///      in the same order as Character Prefabs.
/// </summary>
public class CharacterSelectUI
{
    // ── Canvas / Grid ─────────────────────────────────────────────────────────
    GameObject          _root;
    RectTransform       _gridRT;         // needed to force layout rebuild
    GameObject[]        _cards;
    Image[]             _thumbnails;     // sprite display per slot
    TextMeshProUGUI[]   _charNames;
    TextMeshProUGUI[]   _playerLabels;
    TextMeshProUGUI[]   _statuses;

    int _lastVisibleCount = -1;

    static readonly Color ColBg          = new Color(0.05f, 0.05f, 0.05f, 0.88f);
    static readonly Color ColCard        = new Color(0.14f, 0.14f, 0.14f, 1.00f);
    static readonly Color ColThumbEmpty  = new Color(0.22f, 0.22f, 0.22f, 1.00f);  // placeholder
    static readonly Color ColConfirmed   = new Color(0.10f, 0.80f, 0.22f, 1.00f);
    static readonly Color ColBrowse      = new Color(0.90f, 0.90f, 0.90f, 1.00f);

    // Card dimensions (pixels, in reference resolution 1920×1080)
    const float CardW      = 200f;
    const float CardH      = 270f;
    const float ThumbH     = 150f;  // height of the thumbnail image area
    const float GridGap    = 18f;
    const int   MaxCols    = 4;

    // ── Public API ────────────────────────────────────────────────────────────

    public void Initialize(int slotCount)
    {
        _root = new GameObject("CharacterSelectUI");

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
        MakeTMP(_root.transform, "Title", "SELECT YOUR CHARACTER",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -30f), new Vector2(0f, 90f),
                52f, FontStyles.Bold, Color.white);

        // ── Grid container ────────────────────────────────────────────────────
        var gridGO = new GameObject("CardGrid", typeof(RectTransform));
        gridGO.transform.SetParent(_root.transform, false);
        _gridRT             = gridGO.GetComponent<RectTransform>();
        _gridRT.anchorMin   = new Vector2(0.5f, 0.5f);
        _gridRT.anchorMax   = new Vector2(0.5f, 0.5f);
        _gridRT.pivot       = new Vector2(0.5f, 0.5f);
        _gridRT.anchoredPosition = new Vector2(0f, -30f);

        var glg = gridGO.AddComponent<GridLayoutGroup>();
        glg.cellSize        = new Vector2(CardW, CardH);
        glg.spacing         = new Vector2(GridGap, GridGap);
        glg.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis       = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment  = TextAnchor.MiddleCenter;
        glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = Mathf.Min(slotCount, MaxCols);

        // Auto-size the container around the visible cards
        var csf = gridGO.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        // ── Cards (one per controller slot, hidden until joined) ───────────────
        _cards        = new GameObject[slotCount];
        _thumbnails   = new Image[slotCount];
        _charNames    = new TextMeshProUGUI[slotCount];
        _playerLabels = new TextMeshProUGUI[slotCount];
        _statuses     = new TextMeshProUGUI[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            var card = new GameObject($"Card{i + 1}", typeof(RectTransform));
            card.transform.SetParent(gridGO.transform, false);
            card.AddComponent<Image>().color = ColCard;
            card.SetActive(false);
            _cards[i] = card;

            // ── Character thumbnail (sprite) ──────────────────────────────────
            var thumbGO = new GameObject("Thumbnail", typeof(RectTransform));
            thumbGO.transform.SetParent(card.transform, false);
            var thumbRT             = thumbGO.GetComponent<RectTransform>();
            thumbRT.anchorMin       = new Vector2(0f, 1f);
            thumbRT.anchorMax       = new Vector2(1f, 1f);
            thumbRT.pivot           = new Vector2(0.5f, 1f);
            thumbRT.anchoredPosition = new Vector2(0f, -8f);
            thumbRT.sizeDelta       = new Vector2(-16f, ThumbH);

            var img             = thumbGO.AddComponent<Image>();
            img.color           = ColThumbEmpty;   // grey until sprite assigned
            img.preserveAspect  = true;
            _thumbnails[i]      = img;

            // ── Player label ("Player 1" …) ───────────────────────────────────
            float below = 8f + ThumbH + 6f;
            _playerLabels[i] = MakeTMP(card.transform, "PlayerLabel", $"Player {i + 1}",
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -below), new Vector2(-8f, 24f),
                    15f, FontStyles.Bold, new Color(0.72f, 0.72f, 0.72f));

            // ── Character name ────────────────────────────────────────────────
            below += 24f + 4f;
            _charNames[i] = MakeTMP(card.transform, "CharName", "???",
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -below), new Vector2(-8f, 32f),
                    21f, FontStyles.Bold, Color.white);

            // ── Status (browse / confirmed) — pinned to card bottom ───────────
            _statuses[i] = MakeTMP(card.transform, "Status", "◄ Browse ►",
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 8f), new Vector2(-8f, 28f),
                    14f, FontStyles.Normal, ColBrowse);
        }

        // ── Footer ────────────────────────────────────────────────────────────
        MakeTMP(_root.transform, "Footer",
                "◄ ► Browse  |  A: Confirm  |  B: Back",
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 18f), new Vector2(0f, 38f),
                17f, FontStyles.Normal, new Color(0.75f, 0.75f, 0.75f));
    }

    /// <summary>
    /// Refreshes all card content. Call every frame from CharacterSelect.OnUpdate().
    /// </summary>
    public void Refresh(bool[]           isJoinConfirmed,
                        bool[]           isCharacterSelect,
                        int[]            characterIndex,
                        List<GameObject> characterPrefabs,
                        List<Sprite>     thumbnails)
    {
        if (_root == null) return;

        // ── Show/hide cards; force grid rebuild when count changes ────────────
        int visible = 0;
        for (int i = 0; i < isJoinConfirmed.Length; i++)
            if (isJoinConfirmed[i]) visible++;

        if (visible != _lastVisibleCount)
        {
            for (int i = 0; i < _cards.Length && i < isJoinConfirmed.Length; i++)
                _cards[i].SetActive(isJoinConfirmed[i]);

            LayoutRebuilder.ForceRebuildLayoutImmediate(_gridRT);
            _lastVisibleCount = visible;
        }

        // ── Update visible card content ───────────────────────────────────────
        for (int i = 0; i < _cards.Length && i < isJoinConfirmed.Length; i++)
        {
            if (!isJoinConfirmed[i]) continue;

            int  idx       = characterIndex[i];
            bool confirmed = isCharacterSelect[i];

            // Thumbnail — use supplied sprite; grey placeholder if missing
            bool hasSprite        = thumbnails != null
                                    && idx >= 0
                                    && idx < thumbnails.Count
                                    && thumbnails[idx] != null;
            _thumbnails[i].sprite = hasSprite ? thumbnails[idx] : null;
            _thumbnails[i].color  = hasSprite ? Color.white : ColThumbEmpty;

            // Character name from prefab asset name
            _charNames[i].text = (idx >= 0 && idx < characterPrefabs.Count)
                                  ? characterPrefabs[idx].name
                                  : "???";

            // Status
            _statuses[i].text  = confirmed ? "✓  CONFIRMED" : "◄ Browse ►";
            _statuses[i].color = confirmed ? ColConfirmed   : ColBrowse;
        }
    }

    /// <summary>Tears down the canvas. Call from CharacterSelect.OnExit().</summary>
    public void Destroy()
    {
        if (_root != null) Object.Destroy(_root);
        _root = null; _gridRT = null;
        _cards = null; _thumbnails = null;
        _charNames = null; _playerLabels = null; _statuses = null;
        _lastVisibleCount = -1;
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
