using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space overlay UI for the SplashScreen state.
///
/// Layout:
///   • Full-screen dark background
///   • Large game title centred on screen
///   • Subtitle / studio name below the title
///   • "Press any button to continue" footer that pulses opacity
///   • Thin progress bar at the bottom that drains as the auto-advance timer counts down
/// </summary>
public class SplashScreenUI
{
    GameObject          _root;
    TextMeshProUGUI     _pressAny;
    Image               _timerBar;

    // Pulsing alpha for the footer text
    float _pulseT;

    static readonly Color ColBg       = new Color(0.03f, 0.03f, 0.05f, 1.00f);
    static readonly Color ColTitle    = new Color(1.00f, 1.00f, 1.00f, 1.00f);
    static readonly Color ColSubtitle = new Color(0.65f, 0.65f, 0.70f, 1.00f);
    static readonly Color ColFooter   = new Color(0.80f, 0.80f, 0.80f, 1.00f);
    static readonly Color ColBar      = new Color(0.55f, 0.38f, 0.04f, 1.00f);   // amber
    static readonly Color ColBarBg    = new Color(0.15f, 0.15f, 0.15f, 1.00f);

    // ── Public API ────────────────────────────────────────────────────────────

    public void Initialize()
    {
        _root = new GameObject("SplashScreenUI");

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

        // ── Title ─────────────────────────────────────────────────────────────
        MakeTMP(_root.transform, "Title", "THERE CAN\nONLY BE ONE",
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 60f), new Vector2(0f, 300f),
                96f, FontStyles.Bold, ColTitle);

        // ── Subtitle ─────────────────────────────────────────────────────────
        MakeTMP(_root.transform, "Subtitle", "A LOCAL MULTIPLAYER BRAWLER",
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -70f), new Vector2(0f, 48f),
                28f, FontStyles.Normal, ColSubtitle);

        // ── "Press any button" footer ─────────────────────────────────────────
        _pressAny = MakeTMP(_root.transform, "PressAny", "PRESS ANY BUTTON TO CONTINUE",
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 72f), new Vector2(0f, 44f),
                22f, FontStyles.Normal, ColFooter);

        // ── Timer bar background ──────────────────────────────────────────────
        var barBg = MakePanel(_root.transform, "TimerBarBg", ColBarBg,
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 20f), new Vector2(0f, 10f));

        // ── Timer bar fill (anchored left, width driven by Refresh) ──────────
        var fillGO = new GameObject("TimerBarFill", typeof(RectTransform));
        fillGO.transform.SetParent(barBg.transform, false);
        var fillRT         = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin   = Vector2.zero;
        fillRT.anchorMax   = new Vector2(1f, 1f);   // starts full width
        fillRT.offsetMin   = Vector2.zero;
        fillRT.offsetMax   = Vector2.zero;
        _timerBar          = fillGO.AddComponent<Image>();
        _timerBar.color    = ColBar;

        _pulseT = 0f;
    }

    /// <summary>
    /// Call every frame from SplashScreen.OnUpdate().
    /// <paramref name="normalizedTimeRemaining"/> is 1→0 as the auto-advance timer counts down.
    /// <paramref name="deltaTime"/> is Time.deltaTime, used to drive the pulse animation.
    /// </summary>
    public void Refresh(float normalizedTimeRemaining, float deltaTime)
    {
        if (_root == null) return;

        // Shrink timer bar from full to empty
        if (_timerBar != null)
        {
            var rt       = _timerBar.GetComponent<RectTransform>();
            rt.anchorMax = new Vector2(Mathf.Clamp01(normalizedTimeRemaining), 1f);
        }

        // Pulse "press any button" opacity
        _pulseT += deltaTime * 2f;
        float alpha = Mathf.Lerp(0.35f, 1f, (Mathf.Sin(_pulseT) + 1f) * 0.5f);
        if (_pressAny != null)
        {
            var c   = _pressAny.color;
            c.a     = alpha;
            _pressAny.color = c;
        }
    }

    /// <summary>Tears down the canvas. Call from SplashScreen.OnExit().</summary>
    public void Destroy()
    {
        if (_root != null) Object.Destroy(_root);
        _root = null; _pressAny = null; _timerBar = null;
        _pulseT = 0f;
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
