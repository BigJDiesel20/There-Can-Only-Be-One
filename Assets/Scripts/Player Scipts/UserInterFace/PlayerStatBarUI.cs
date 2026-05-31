using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Per-player HUD rendered inside camera.pixelRect on a ScreenSpaceOverlay canvas.
///
/// LEFT SIDE  — local player stats (always visible)
/// RIGHT SIDE — targeted player stats (shown only while a target is locked;
///              mirrors the left layout horizontally)
///
/// Three-layer stack on every element:
///   black outline  →  red depleted background  →  coloured fill on top
///
/// Build is deferred to the first Update tick (after SetCameraRect has run)
/// and automatically rebuilds if the viewport changes (player count changes).
/// </summary>
[Serializable]
public class PlayerStatBarUI
{
    // ── Layout constants (reference sizes at s = 1.0, 540 px viewport height) ──
    private const float PadLeft   = 10f;
    private const float PadBottom = 10f;
    private const float BarHeight = 16f;
    private const float BarGap    = 8f;
    private const float PieSize   = 72f;
    private const float IconW     = 20f;
    private const float IconH     = 28f;
    private const float IconGap   = 5f;
    private const int   MaxIcons  = 10;

    // ── Colors ─────────────────────────────────────────────────────────────────
    private static readonly Color OutlineColor = new Color(0.00f, 0.00f, 0.00f, 1.00f);
    private static readonly Color BgColor      = new Color(0.85f, 0.08f, 0.08f, 1.00f);
    private static readonly Color HealthColor  = new Color(0.04f, 0.40f, 0.04f, 1.00f);
    private static readonly Color StaminaColor = new Color(0.04f, 0.20f, 0.55f, 1.00f);
    private static readonly Color AuraColor    = new Color(0.53f, 0.81f, 0.98f, 1.00f);

    // ── Local player UI ────────────────────────────────────────────────────────
    private TextMeshProUGUI _nameText;
    private Image           _healthFill;
    private Image           _staminaFill;
    private Image           _auraPie;
    private Transform[]         _auraIconRoots  = new Transform[MaxIcons];
    private Image[]             _auraIconFills  = new Image[MaxIcons];
    private AuraFlameAnimator[] _auraAnimators  = new AuraFlameAnimator[MaxIcons];

    // ── Target player UI (right side, hidden when no target) ──────────────────
    private GameObject      _targetRoot;        // parent for all right-side elements
    private TextMeshProUGUI _targetNameText;
    private Image           _targetHealthFill;
    private Image           _targetStaminaFill;
    private Image           _targetAuraPie;
    private Transform[]         _targetIconRoots = new Transform[MaxIcons];
    private Image[]             _targetIconFills = new Image[MaxIcons];
    private AuraFlameAnimator[] _targetAnimators = new AuraFlameAnimator[MaxIcons];
    private LocalPlayerManager _currentTarget;

    // ── Canvas / camera ────────────────────────────────────────────────────────
    private Canvas _canvas;
    private Camera _camera;
    private Rect   _lastPixelRect;
    private bool   _needsBuild = true;

    // ── Cached sprites / materials ─────────────────────────────────────────────
    private static Sprite   _squareSprite;
    private static Sprite   _circleSprite;
    private static Sprite   _glowSprite;            // Radial-gradient circle for the pie halo glow
    private static Sprite[] _flameFrames;           // AuraFlame sprite sheet frames (loaded once)
    private static Material _heatDistortMat;        // Heat-distortion UI material (loaded once)
    private static Sprite   _humanFigureSprite;     // Silhouette icon for role display
    private static Sprite[] _crownSprites;          // Randomised crown icons for leader role

    // ── Cached stat values (events may fire before the HUD is built) ──────────
    // Seeded to 1f / 0f so first-frame display is correct before any event fires.
    private float _lastAuraPct   = 1f;   // from OnPercentageChange
    private float _lastAuraValue = 0f;   // from OnValueChange

    // ── Stored name ────────────────────────────────────────────────────────────
    private string _playerName = string.Empty;

    // ── Owner reference (for ActiveSymbol polling) ─────────────────────────────
    private LocalPlayerManager _owner;
    private PlayerSymbolEntry  _lastLocalSymbol;
    private PlayerSymbolEntry  _lastTargetSymbol;
    private Image              _localSymbolImage;
    private RectTransform      _localSymbolRT;
    private Image              _targetSymbolImage;
    private RectTransform      _targetSymbolRT;
    private float              _symbolInnerDiam;   // cached inner circle size for ApplySymbol

    // ── Team list ───────────────────────────────────────────────────────────────
    private GameObject          _teamListRoot;         // container parented to the stat-bar canvas
    private List<GameObject>    _teamRows = new List<GameObject>(); // one row per OTHER team member
    private Vector2             _nameLabelCenter;      // stored from BuildLocalHUD for row placement
    private Vector2             _nameLabelSize;
    private Team                _subscribedTeam;       // team we're currently listening to

    // ── Owner name-label role icon refs (updated without rebuilding the label) ──
    private Image               _localFigureImg;
    private Image               _localCrownImg;

    // ── Role icon type ──────────────────────────────────────────────────────────
    private enum RoleIcon { None, Follower, Leader }

    // ── Events ─────────────────────────────────────────────────────────────────
    private PlayerEvents _playerEvents;

    // ═══════════════════════════════════════════════════════════════════════════
    // Public API
    // ═══════════════════════════════════════════════════════════════════════════

    public void Initialize(Camera camera, PlayerEvents playerEvents)
    {
        _playerEvents = playerEvents;
        _camera       = camera;
        EnsureSprites();

        GameObject canvasGO = new GameObject("Stat Bar Canvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 5;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();

        _needsBuild = true;

        // Local stat events
        StatEvents health  = playerEvents.statEventsCoclection[StatEvents.Type.Health];
        StatEvents stamina = playerEvents.statEventsCoclection[StatEvents.Type.Stamina];
        StatEvents aura    = playerEvents.statEventsCoclection[StatEvents.Type.Aura];

        health.OnPercentageChange  += OnHealthChanged;
        stamina.OnPercentageChange += OnStaminaChanged;
        aura.OnPercentageChange    += OnAuraPieChanged;
        aura.OnValueChange         += OnAuraIconsChanged;

        // Targeting
        playerEvents.OnOrbitTargetChanged += OnOrbitTargetChanged;
        playerEvents.OnUpdate             += OnUpdate;
    }

    public void Deactivate()
    {
        HideTargetHUD();   // unsubscribe from target before clearing
        UnsubscribeFromTeam(_subscribedTeam);

        if (_owner != null)
        {
            _owner.playerEvents.OnTeamChanged -= OnOwnerTeamChanged;
        }

        if (_playerEvents != null)
        {
            StatEvents health  = _playerEvents.statEventsCoclection[StatEvents.Type.Health];
            StatEvents stamina = _playerEvents.statEventsCoclection[StatEvents.Type.Stamina];
            StatEvents aura    = _playerEvents.statEventsCoclection[StatEvents.Type.Aura];

            health.OnPercentageChange  -= OnHealthChanged;
            stamina.OnPercentageChange -= OnStaminaChanged;
            aura.OnPercentageChange    -= OnAuraPieChanged;
            aura.OnValueChange         -= OnAuraIconsChanged;

            _playerEvents.OnOrbitTargetChanged -= OnOrbitTargetChanged;
            _playerEvents.OnUpdate             -= OnUpdate;
            _playerEvents = null;
        }

        if (_canvas != null)
        {
            UnityEngine.Object.Destroy(_canvas.gameObject);
            _canvas = null;
        }

        NullLocalRefs();
        NullTargetRefs();
    }

    public void SetPlayerName(string name)
    {
        _playerName = name ?? string.Empty;
        UpdateOwnNameLabel();
    }

    /// <summary>
    /// Provide the owning LocalPlayerManager so we can receive team change events
    /// and keep the local symbol up to date.
    /// Call this once after Initialize (e.g. right after InitializePlayerCharacter).
    /// </summary>
    public void SetOwner(LocalPlayerManager owner)
    {
        if (_owner != null)
        {
            _owner.playerEvents.OnTeamChanged -= RefreshLocalSymbol;
            _owner.playerEvents.OnTeamChanged -= OnOwnerTeamChanged;
        }
        UnsubscribeFromTeam(_subscribedTeam);

        _owner = owner;

        if (_owner != null)
        {
            _owner.playerEvents.OnTeamChanged += RefreshLocalSymbol;
            _owner.playerEvents.OnTeamChanged += OnOwnerTeamChanged;
        }

        ResubscribeTeam();
        RefreshLocalSymbol();
        RefreshTeamList();

        // Seed cached stat values from the owner's current stats so the first
        // OnUpdate build tick gets the right _lastAuraValue (and not the 0f default).
        // Stat.Initialize fires OnValueChange before PlayerStatBarUI subscribes, so
        // without this call _lastAuraValue stays 0f → icons all show empty at battle start.
        if (_owner?.statManager != null)
            _owner.statManager.BroadcastCurrentValues();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Targeting
    // ═══════════════════════════════════════════════════════════════════════════

    private void OnOrbitTargetChanged(LocalPlayerManager target, bool acquired)
    {
        if (acquired && target != null)
            ShowTargetHUD(target);
        else
            HideTargetHUD();
    }

    private void ShowTargetHUD(LocalPlayerManager target)
    {
        // Unsubscribe from the previous target first
        if (_currentTarget != null) UnsubscribeTarget(_currentTarget);

        _currentTarget = target;
        SubscribeTarget(target);

        // Ping the target's own stat events so all subscribed callbacks fire
        // immediately with the current values — no data needs to be read here.
        target.statManager.BroadcastCurrentValues();

        if (_targetRoot     != null) _targetRoot.SetActive(true);
        if (_targetNameText != null) _targetNameText.text = target.playerName;

        // Push the target's active symbol (or their leader's) onto the target pie overlay
        _lastTargetSymbol = target.ActiveSymbol;
        ApplySymbol(_targetSymbolImage, _targetSymbolRT, _symbolInnerDiam, _lastTargetSymbol);
    }

    private void HideTargetHUD()
    {
        if (_currentTarget != null)
        {
            UnsubscribeTarget(_currentTarget);
            _currentTarget = null;
        }
        _lastTargetSymbol = null;
        if (_targetRoot        != null) _targetRoot.SetActive(false);
        if (_targetSymbolImage != null) _targetSymbolImage.color = Color.clear;
    }

    private void SubscribeTarget(LocalPlayerManager target)
    {
        StatEvents h = target.playerEvents.statEventsCoclection[StatEvents.Type.Health];
        StatEvents s = target.playerEvents.statEventsCoclection[StatEvents.Type.Stamina];
        StatEvents a = target.playerEvents.statEventsCoclection[StatEvents.Type.Aura];
        h.OnPercentageChange      += OnTargetHealthChanged;
        s.OnPercentageChange      += OnTargetStaminaChanged;
        a.OnPercentageChange      += OnTargetAuraPieChanged;
        a.OnValueChange           += OnTargetIconsChanged;
        target.playerEvents.OnTeamChanged += OnTargetTeamChanged;
    }

    private void UnsubscribeTarget(LocalPlayerManager target)
    {
        StatEvents h = target.playerEvents.statEventsCoclection[StatEvents.Type.Health];
        StatEvents s = target.playerEvents.statEventsCoclection[StatEvents.Type.Stamina];
        StatEvents a = target.playerEvents.statEventsCoclection[StatEvents.Type.Aura];
        h.OnPercentageChange      -= OnTargetHealthChanged;
        s.OnPercentageChange      -= OnTargetStaminaChanged;
        a.OnPercentageChange      -= OnTargetAuraPieChanged;
        a.OnValueChange           -= OnTargetIconsChanged;
        target.playerEvents.OnTeamChanged -= OnTargetTeamChanged;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Local stat callbacks
    // ═══════════════════════════════════════════════════════════════════════════

    private void OnHealthChanged(float pct)
    {
        if (_healthFill  != null) _healthFill.fillAmount  = Mathf.Clamp01(pct);
    }
    private void OnStaminaChanged(float pct)
    {
        if (_staminaFill != null) _staminaFill.fillAmount = Mathf.Clamp01(pct);
    }
    private void OnAuraPieChanged(float pct)
    {
        _lastAuraPct = pct;
        if (_auraPie != null) _auraPie.fillAmount = Mathf.Clamp01(pct);
    }
    private void OnAuraIconsChanged(float value)
    {
        _lastAuraValue = value;
        UpdateIcons(_auraIconRoots, _auraIconFills, value, _auraAnimators);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Target stat callbacks
    // ═══════════════════════════════════════════════════════════════════════════

    private void OnTargetHealthChanged(float pct)
    {
        if (_targetHealthFill  != null) _targetHealthFill.fillAmount  = Mathf.Clamp01(pct);
    }
    private void OnTargetStaminaChanged(float pct)
    {
        if (_targetStaminaFill != null) _targetStaminaFill.fillAmount = Mathf.Clamp01(pct);
    }
    private void OnTargetAuraPieChanged(float pct)
    {
        if (_targetAuraPie     != null) _targetAuraPie.fillAmount     = Mathf.Clamp01(pct);
    }
    private void OnTargetIconsChanged(float value) => UpdateIcons(_targetIconRoots, _targetIconFills, value, _targetAnimators);
    private void OnTargetTeamChanged()
    {
        if (_currentTarget == null || _targetSymbolImage == null) return;
        _lastTargetSymbol = _currentTarget.ActiveSymbol;
        ApplySymbol(_targetSymbolImage, _targetSymbolRT, _symbolInnerDiam, _lastTargetSymbol);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Shared icon update logic
    // ═══════════════════════════════════════════════════════════════════════════

    private static void UpdateIcons(Transform[] roots, Image[] fills, float value,
                                     AuraFlameAnimator[] animators = null)
    {
        if (roots == null) return;

        // All 10 flames represent a rolling 100-aura band.
        // Each flame = 10 aura within that band.
        // Aura = 0 → all empty (no loop).  Exact multiples of 100 (non-zero) → all full (no loop).
        float bandValue = value % 100f;
        if (value > 0f && bandValue < 0.001f)
            bandValue = 100f;

        int drainingIndex = -1;
        for (int i = 0; i < MaxIcons; i++)
        {
            if (roots[i] == null) continue;
            // Flame i covers the aura slice [i*10, (i+1)*10] within the band
            float fill = Mathf.Clamp01((bandValue - i * 10f) / 10f);
            roots[i].gameObject.SetActive(fill > 0f);
            if (fill > 0f && fills[i] != null)
                fills[i].fillAmount = fill;
            // Track the rightmost icon that is partially filled (actively draining)
            if (fill > 0.01f && fill < 0.99f) drainingIndex = i;
        }
        // Tell each animator whether it is the one currently draining
        if (animators == null) return;
        for (int i = 0; i < MaxIcons; i++)
        {
            if (animators[i] != null)
                animators[i].isDraining = (i == drainingIndex);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Deferred / reactive build
    // ═══════════════════════════════════════════════════════════════════════════

    private void OnUpdate()
    {
        if (_canvas == null) return;

        Rect current = _camera.pixelRect;
        if (_needsBuild || current != _lastPixelRect)
        {
            _needsBuild    = false;
            _lastPixelRect = current;

            ClearUI();
            BuildUI(_canvas, current);
            ResubscribeTeam();
            RefreshTeamList();

            // Seed the pie and icons with cached values — these events may have
            // fired in PreGame before the HUD was built, so we must replay them.
            if (_auraPie != null) _auraPie.fillAmount = Mathf.Clamp01(_lastAuraPct);
            UpdateIcons(_auraIconRoots, _auraIconFills, _lastAuraValue, _auraAnimators);

            // Restore target HUD if a target is still locked
            if (_currentTarget != null)
            {
                if (_targetRoot     != null) _targetRoot.SetActive(true);
                if (_targetNameText != null) _targetNameText.text = _currentTarget.playerName;
                ApplySymbol(_targetSymbolImage, _targetSymbolRT, _symbolInnerDiam, _currentTarget.ActiveSymbol);

                // Re-seed all target fills after the rebuild reset them to 1f
                _currentTarget.statManager.BroadcastCurrentValues();
            }
        }

    }

    private void ClearUI()
    {
        NullLocalRefs();
        NullTargetRefs();
        Transform root = _canvas.transform;
        for (int i = root.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(root.GetChild(i).gameObject);
    }

    private void NullLocalRefs()
    {
        _nameText         = null;
        _healthFill       = null;
        _staminaFill      = null;
        _auraPie          = null;
        _localSymbolImage = null;
        _localSymbolRT    = null;
        _lastLocalSymbol  = null;
        _auraIconRoots    = new Transform[MaxIcons];
        _auraIconFills    = new Image[MaxIcons];
        _auraAnimators    = new AuraFlameAnimator[MaxIcons];
        _teamListRoot     = null;
        _teamRows.Clear();
        _localFigureImg   = null;
        _localCrownImg    = null;
    }

    private void NullTargetRefs()
    {
        _targetRoot         = null;
        _targetNameText     = null;
        _targetHealthFill   = null;
        _targetStaminaFill  = null;
        _targetAuraPie      = null;
        _targetSymbolImage  = null;
        _targetSymbolRT     = null;
        _targetIconRoots    = new Transform[MaxIcons];
        _targetIconFills    = new Image[MaxIcons];
        _targetAnimators    = new AuraFlameAnimator[MaxIcons];
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UI construction
    // ═══════════════════════════════════════════════════════════════════════════

    private void BuildUI(Canvas canvas, Rect vp)
    {
        Transform root = canvas.transform;
        canvas.gameObject.SetActive(true);

        // Scale factor normalised to a 540 px tall reference viewport
        float s  = Mathf.Clamp(vp.height / 540f, 0.1f, 1.5f);
        float ol = Mathf.Max(1f, 2f * s);

        float barW = vp.width  * 0.45f;
        float barH = Mathf.Max(3f, BarHeight * s);
        float pieS = Mathf.Max(8f, PieSize   * s);
        float iW   = Mathf.Max(4f, IconW     * s);
        float iH   = Mathf.Max(6f, IconH     * s);
        float gap  = Mathf.Max(1f, BarGap    * s);
        float iGap = Mathf.Max(1f, IconGap   * s);
        float padL = Mathf.Max(2f, PadLeft   * s);
        float padB = Mathf.Max(2f, PadBottom * s);

        // ── Left side (local player) ────────────────────────────────────────────
        float lx = vp.x + padL;
        float oy = vp.y + padB;
        BuildLocalHUD(root, lx, oy, barW, barH, pieS, iW, iH, gap, iGap, ol, s);

        // ── Right side (target player) — hidden until target is locked ──────────
        float rx = vp.x + vp.width - padL;
        BuildTargetHUD(root, rx, oy, barW, barH, pieS, iW, iH, gap, iGap, ol, s);
    }

    // ── Left HUD ───────────────────────────────────────────────────────────────

    private void BuildLocalHUD(Transform root,
                                float ox, float oy,
                                float barW, float barH,
                                float pieS, float iW, float iH,
                                float gap,  float iGap, float ol, float s)
    {
        // ── Vertical layout (bottom → top) ─────────────────────────────────────
        // Pie sits at the very bottom of the HUD row.
        // Stamina touches the pie top directly (no gap, same height as health bar).
        // Health bar touches the stamina top directly (no gap).
        float staminaH = barH;
        float staminaY = oy + pieS;                    // stamina bottom = pie top
        float healthY  = staminaY + staminaH;          // health bottom  = stamina top

        // Name label sits above the health bar
        float labelH   = Mathf.Max(10f, barH * 2f);
        float labelGap = Mathf.Max(1f, 2f * s);
        Vector2 nameCenter = new Vector2(ox + barW * 0.5f, healthY + barH + labelGap + labelH * 0.5f);
        Vector2 nameSize   = new Vector2(barW, labelH);

        BuildNameLabel(root, "Player Name", nameCenter, nameSize,
                       _playerName, TextAlignmentOptions.BottomLeft,
                       barH, out _nameText, out _localFigureImg, out _localCrownImg);

        // ── Team list ─────────────────────────────────────────────────────────
        // Store the name label's position so RefreshTeamList can stack rows above it.
        _nameLabelCenter = nameCenter;
        _nameLabelSize   = nameSize;

        _teamListRoot = new GameObject("Team List", typeof(RectTransform));
        _teamListRoot.transform.SetParent(root, false);
        var tlRT = _teamListRoot.GetComponent<RectTransform>();
        tlRT.anchorMin = tlRT.anchorMax = Vector2.zero;
        tlRT.pivot     = new Vector2(0.5f, 0.5f);
        tlRT.anchoredPosition = Vector2.zero;
        tlRT.sizeDelta        = Vector2.zero;

        // Health bar — outlined (black border) + embossed
        CreateBar(root, "Health Bar",
                  new Vector2(ox + barW * 0.5f, healthY + barH * 0.5f),
                  new Vector2(barW, barH), ol, HealthColor,
                  Image.OriginHorizontal.Left, true, out _healthFill, emboss: true);

        // Stamina bar — outlined + embossed, same height, flush below health
        CreateBar(root, "Stamina Bar",
                  new Vector2(ox + barW * 0.5f, staminaY + staminaH * 0.5f),
                  new Vector2(barW, staminaH), ol, StaminaColor,
                  Image.OriginHorizontal.Left, true, out _staminaFill, emboss: true);

        // Aura pie (left side)
        Vector2 pieCenterL = new Vector2(ox + pieS * 0.5f, oy + pieS * 0.5f);
        CreatePie(root, "Aura Pie", pieCenterL, pieS, ol, out _auraPie, out float pieInnerDiamL);

        _symbolInnerDiam = Mathf.Max(4f, pieInnerDiamL);
        CreateSymbolInCircle(root, "Local Symbol", pieCenterL, _symbolInnerDiam,
                             _owner?.ActiveSymbol,
                             out _localSymbolImage, out _localSymbolRT);
        _lastLocalSymbol = _owner?.ActiveSymbol;

        // ── Flame icons — fill the space between pie right edge and bar right edge ──
        // Each icon is as tall as the pie and evenly divides the available width.
        float iconSpan   = barW - pieS;                         // total horizontal space for icons
        float flameW     = Mathf.Max(4f, iconSpan / MaxIcons); // per-icon width (no gaps)
        float flameH     = pieS;                                // full pie height
        float iconStartX = ox + pieS;                           // start immediately right of pie
        float iconCenterY = oy + pieS * 0.5f;
        BuildIcons(root, iconStartX, iconCenterY, flameW, flameH, 0f, ol,
                   1f, _auraIconRoots, _auraIconFills, _auraAnimators);
    }

    // ── Right HUD (mirrored) ───────────────────────────────────────────────────

    private void BuildTargetHUD(Transform root,
                                 float rx, float oy,
                                 float barW, float barH,
                                 float pieS, float iW, float iH,
                                 float gap,  float iGap, float ol, float s)
    {
        // ── Vertical layout (mirrors local HUD) ────────────────────────────────
        float staminaH = barH;
        float staminaY = oy + pieS;
        float healthY  = staminaY + staminaH;

        // Wrapper — toggling this shows/hides the whole right HUD
        _targetRoot = new GameObject("Target HUD", typeof(RectTransform));
        _targetRoot.transform.SetParent(root, false);
        RectTransform wrapRT = _targetRoot.GetComponent<RectTransform>();
        wrapRT.anchorMin = Vector2.zero;
        wrapRT.anchorMax = Vector2.one;
        wrapRT.offsetMin = Vector2.zero;
        wrapRT.offsetMax = Vector2.zero;
        _targetRoot.SetActive(false);

        Transform wrap = _targetRoot.transform;

        // Name label (right-aligned)
        float labelH   = Mathf.Max(10f, barH * 2f);
        float labelGap = Mathf.Max(1f, 2f * s);
        BuildNameLabel(wrap, "Target Name",
                       new Vector2(rx - barW * 0.5f, healthY + barH + labelGap + labelH * 0.5f),
                       new Vector2(barW, labelH),
                       string.Empty, TextAlignmentOptions.BottomRight,
                       barH, out _targetNameText);

        // Health bar — outlined + embossed
        CreateBar(wrap, "Target Health Bar",
                  new Vector2(rx - barW * 0.5f, healthY + barH * 0.5f),
                  new Vector2(barW, barH), ol, HealthColor,
                  Image.OriginHorizontal.Right, true, out _targetHealthFill, emboss: true);

        // Stamina bar — outlined + embossed
        CreateBar(wrap, "Target Stamina Bar",
                  new Vector2(rx - barW * 0.5f, staminaY + staminaH * 0.5f),
                  new Vector2(barW, staminaH), ol, StaminaColor,
                  Image.OriginHorizontal.Right, true, out _targetStaminaFill, emboss: true);

        // Aura pie (right side)
        Vector2 pieCenterR = new Vector2(rx - pieS * 0.5f, oy + pieS * 0.5f);
        CreatePie(wrap, "Target Aura Pie", pieCenterR, pieS, ol, out _targetAuraPie, out float pieInnerDiamR);

        float innerDiamR = Mathf.Max(4f, pieInnerDiamR);
        CreateSymbolInCircle(wrap, "Target Symbol", pieCenterR, innerDiamR,
                             null,
                             out _targetSymbolImage, out _targetSymbolRT);

        // ── Flame icons — fill the space between bar left edge and pie left edge ──
        float iconSpan    = barW - pieS;
        float flameW      = Mathf.Max(4f, iconSpan / MaxIcons);
        float flameH      = pieS;
        float iconStartX  = rx - barW;                  // start at bar left edge
        float iconCenterY = oy + pieS * 0.5f;
        BuildIcons(wrap, iconStartX, iconCenterY, flameW, flameH, 0f, ol,
                   1f, _targetIconRoots, _targetIconFills, _targetAnimators);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Element builders
    // ═══════════════════════════════════════════════════════════════════════════

    // ── No-icon overload — used for the target HUD name label ─────────────────
    private static void BuildNameLabel(Transform parent, string goName,
                                       Vector2 center, Vector2 size,
                                       string text, TextAlignmentOptions alignment,
                                       float barH, out TextMeshProUGUI tmp)
    {
        Image dummy1, dummy2;
        BuildNameLabel(parent, goName, center, size, text, alignment, barH,
                       out tmp, out dummy1, out dummy2, withIconSlot: false);
    }

    // ── Icon-slot overload — used for the local player name label ──────────────
    private static void BuildNameLabel(Transform parent, string goName,
                                       Vector2 center, Vector2 size,
                                       string text, TextAlignmentOptions alignment,
                                       float barH, out TextMeshProUGUI tmp,
                                       out Image figureImg, out Image crownImg)
    {
        BuildNameLabel(parent, goName, center, size, text, alignment, barH,
                       out tmp, out figureImg, out crownImg, withIconSlot: true);
    }

    /// <summary>
    /// Core name-label builder.
    /// withIconSlot = true  → HorizontalLayoutGroup lays out [icon | text]; role images are
    ///                        returned via figureImg/crownImg for later show/hide by ApplyRoleIconImages.
    /// withIconSlot = false → plain label (target HUD); no icon slot, original CSF + stretch layout.
    /// </summary>
    private static void BuildNameLabel(Transform parent, string goName,
                                       Vector2 center, Vector2 size,
                                       string text, TextAlignmentOptions alignment,
                                       float barH, out TextMeshProUGUI tmp,
                                       out Image figureImg, out Image crownImg,
                                       bool withIconSlot)
    {
        figureImg = null;
        crownImg  = null;

        bool leftAligned = alignment == TextAlignmentOptions.BottomLeft
                        || alignment == TextAlignmentOptions.Left
                        || alignment == TextAlignmentOptions.TopLeft;

        float edgeX   = leftAligned ? center.x - size.x * 0.5f   // left edge
                                    : center.x + size.x * 0.5f;  // right edge
        Vector2 pivot = leftAligned ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f);

        // ── Container ─────────────────────────────────────────────────────────
        GameObject container = new GameObject(goName + " Label", typeof(RectTransform));
        container.transform.SetParent(parent, false);
        RectTransform cRT    = container.GetComponent<RectTransform>();
        cRT.anchorMin        = Vector2.zero;
        cRT.anchorMax        = Vector2.zero;
        cRT.pivot            = pivot;
        cRT.anchoredPosition = new Vector2(edgeX, center.y);
        cRT.sizeDelta        = new Vector2(0f, size.y);

        if (withIconSlot)
        {
            // HorizontalLayoutGroup drives icon + text side by side
            HorizontalLayoutGroup hlg  = container.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment         = leftAligned ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
            hlg.spacing                = 0f;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth      = true;
            hlg.childControlHeight     = true;
            hlg.padding                = new RectOffset(2, 2, 0, 0);
        }

        ContentSizeFitter csf = container.AddComponent<ContentSizeFitter>();
        csf.horizontalFit     = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit       = ContentSizeFitter.FitMode.Unconstrained;

        // ── Dark background — always fills container via anchors ──────────────
        GameObject bgGO = new GameObject("Bg", typeof(RectTransform));
        bgGO.transform.SetParent(container.transform, false);
        RectTransform bgRT   = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin       = Vector2.zero;
        bgRT.anchorMax       = Vector2.one;
        bgRT.offsetMin       = Vector2.zero;
        bgRT.offsetMax       = Vector2.zero;
        Image bgImg          = bgGO.AddComponent<Image>();
        bgImg.sprite         = _squareSprite;
        bgImg.color          = new Color(0f, 0f, 0f, 0.55f);
        bgImg.type           = Image.Type.Simple;
        bgImg.raycastTarget  = false;
        if (withIconSlot)
        {
            // Must not participate in HLG layout; anchors will still fill the container
            LayoutElement bgLE = bgGO.AddComponent<LayoutElement>();
            bgLE.ignoreLayout  = true;
        }

        // ── Icon slot (square, height = label height) ─────────────────────────
        if (withIconSlot)
        {
            float iconSz = size.y;

            GameObject slotGO = new GameObject("IconSlot", typeof(RectTransform));
            slotGO.transform.SetParent(container.transform, false);
            LayoutElement slotLE   = slotGO.AddComponent<LayoutElement>();
            slotLE.preferredWidth  = iconSz;
            slotLE.preferredHeight = iconSz;
            slotLE.flexibleWidth   = 0f;

            // Human figure — fills the slot; colour set to clear until role is applied
            GameObject figGO    = new GameObject("Figure", typeof(RectTransform));
            figGO.transform.SetParent(slotGO.transform, false);
            RectTransform figRT = figGO.GetComponent<RectTransform>();
            figRT.anchorMin     = Vector2.zero;
            figRT.anchorMax     = Vector2.one;
            figRT.offsetMin     = Vector2.zero;
            figRT.offsetMax     = Vector2.zero;
            figGO.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            figureImg                = figGO.AddComponent<Image>();
            figureImg.sprite         = _humanFigureSprite;
            figureImg.color          = Color.clear;      // hidden until ApplyRoleIconImages is called
            figureImg.preserveAspect = true;
            figureImg.raycastTarget  = false;

            // Crown — anchored to sit on the figure's head.
            // Scale 0.5 keeps it tight; offsets (-2.1 / 2.3) nudge it onto the head centre.
            // Inspector mapping: Left=offsetMin.x, Right=-offsetMax.x, Top=-offsetMax.y, Bottom=offsetMin.y
            GameObject crownGO    = new GameObject("Crown", typeof(RectTransform));
            crownGO.transform.SetParent(slotGO.transform, false);
            RectTransform crownRT = crownGO.GetComponent<RectTransform>();
            crownRT.anchorMin     = new Vector2(0.05f, 0.65f);
            crownRT.anchorMax     = new Vector2(0.95f, 1.10f);
            crownRT.offsetMin     = new Vector2(-2.1f,  2.3f);   // Left=-2.1, Bottom=2.3
            crownRT.offsetMax     = new Vector2(-2.1f,  2.3f);   // Right=2.1 (-(-2.1)), Top=-2.3 (-(2.3))
            crownGO.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            crownImg              = crownGO.AddComponent<Image>();
            crownImg.sprite       = (_crownSprites != null && _crownSprites.Length > 0)
                                    ? _crownSprites[0] : null;
            crownImg.color        = Color.clear;         // hidden until ApplyRoleIconImages is called
            crownImg.preserveAspect = true;
            crownImg.raycastTarget  = false;
        }

        // ── Text ──────────────────────────────────────────────────────────────
        GameObject go = new GameObject(goName, typeof(RectTransform));
        go.transform.SetParent(container.transform, false);
        RectTransform goRT = go.GetComponent<RectTransform>();

        if (withIconSlot)
        {
            // HLG positions it; TMP's preferred width drives ContentSizeFitter
            LayoutElement textLE = go.AddComponent<LayoutElement>();
            textLE.flexibleWidth = 0f;
        }
        else
        {
            // Original: stretches to fill container, its preferred width drives CSF
            goRT.anchorMin = Vector2.zero;
            goRT.anchorMax = Vector2.one;
            goRT.offsetMin = Vector2.zero;
            goRT.offsetMax = Vector2.zero;
        }

        tmp                  = go.AddComponent<TextMeshProUGUI>();
        tmp.text             = text;
        tmp.color            = Color.white;
        tmp.enableAutoSizing = false;
        tmp.fontSize         = Mathf.Max(10f, barH * 1.5f);
        tmp.alignment        = alignment;
        tmp.fontStyle        = FontStyles.Bold;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;

        // Force layout so the background is the correct width on frame 1
        LayoutRebuilder.ForceRebuildLayoutImmediate(cRT);
    }

    /// <summary>Three-layer bar: black outline → red background → coloured fill, with optional emboss.</summary>
    private static void CreateBar(Transform parent, string name,
                                   Vector2 center, Vector2 size,
                                   float ol, Color fillColor,
                                   Image.OriginHorizontal fillOrigin,
                                   bool showOutline,
                                   out Image fillImage,
                                   bool emboss = false)
    {
        // ── Outline wrapper ──────────────────────────────────────────────────────
        GameObject trackGO = new GameObject(name + " Track", typeof(RectTransform));
        trackGO.transform.SetParent(parent, false);
        SetBL(trackGO.GetComponent<RectTransform>(), center, size);

        if (showOutline)
        {
            Image trackImg  = trackGO.AddComponent<Image>();
            trackImg.sprite = _squareSprite;
            trackImg.color  = OutlineColor;
            trackImg.type   = Image.Type.Simple;
        }

        Transform barParent = trackGO.transform;
        float inset = showOutline ? ol : 0f;

        // ── Red depleted background ──────────────────────────────────────────────
        GameObject bgGO = new GameObject(name + " Bg", typeof(RectTransform));
        bgGO.transform.SetParent(barParent, false);
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = new Vector2(inset, inset); bgRT.offsetMax = new Vector2(-inset, -inset);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.sprite = _squareSprite; bgImg.color = BgColor; bgImg.type = Image.Type.Simple;

        // ── Coloured fill ────────────────────────────────────────────────────────
        GameObject fillGO = new GameObject(name + " Fill", typeof(RectTransform));
        fillGO.transform.SetParent(barParent, false);
        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(inset, inset); fillRT.offsetMax = new Vector2(-inset, -inset);

        fillImage            = fillGO.AddComponent<Image>();
        fillImage.sprite     = _squareSprite;
        fillImage.color      = fillColor;
        fillImage.type       = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)fillOrigin;
        fillImage.fillAmount = 1f;

        if (emboss)
        {
            float eh = Mathf.Max(1f, ol * 0.6f);

            GameObject hlGO = new GameObject(name + " Highlight", typeof(RectTransform));
            hlGO.transform.SetParent(barParent, false);
            RectTransform hlRT = hlGO.GetComponent<RectTransform>();
            hlRT.anchorMin = new Vector2(0f, 1f); hlRT.anchorMax = new Vector2(1f, 1f);
            hlRT.offsetMin = new Vector2(inset, -inset - eh);
            hlRT.offsetMax = new Vector2(-inset, -inset);
            Image hlImg = hlGO.AddComponent<Image>();
            hlImg.sprite = _squareSprite; hlImg.color = new Color(1f, 1f, 1f, 0.35f);
            hlImg.type = Image.Type.Simple; hlImg.raycastTarget = false;

            GameObject shGO = new GameObject(name + " Shadow", typeof(RectTransform));
            shGO.transform.SetParent(barParent, false);
            RectTransform shRT = shGO.GetComponent<RectTransform>();
            shRT.anchorMin = new Vector2(0f, 0f); shRT.anchorMax = new Vector2(1f, 0f);
            shRT.offsetMin = new Vector2(inset, inset);
            shRT.offsetMax = new Vector2(-inset, inset + eh);
            Image shImg = shGO.AddComponent<Image>();
            shImg.sprite = _squareSprite; shImg.color = new Color(0f, 0f, 0f, 0.35f);
            shImg.type = Image.Type.Simple; shImg.raycastTarget = false;
        }
    }

    // ── Aura pie glow colours ──────────────────────────────────────────────────
    // Low  = base blue (same family as AuraColor, slightly cooler)
    // High = bright near-white blue at the pulse peak
    private static readonly Color PulseColorLow  = new Color(0.40f, 0.73f, 0.97f, 1.00f);
    private static readonly Color PulseColorHigh = new Color(0.82f, 0.96f, 1.00f, 1.00f);
    // Halo tint — more saturated blue for the outer-glow spread
    private static readonly Color HaloColorRGB   = new Color(0.18f, 0.50f, 1.00f, 0.00f);

    /// <summary>
    /// Three-layer radial pie: outline → red background → coloured fill.
    /// Also injects a soft glow halo behind the pie and attaches an
    /// AuraPiePulse MonoBehaviour to animate brightness each frame.
    /// </summary>
    // Fraction of the outer pie diameter used for the inner circle.
    // Raising this gives a thinner ring; lowering it gives a wider ring.
    private const float InnerCircleRatio = 0.75f;

    /// <param name="innerDiam">
    /// Usable interior diameter of the inner circle (after its outline inset).
    /// Pass this directly to CreateSymbolInCircle so the symbol fills the hole.
    /// </param>
    private static void CreatePie(Transform parent, string name,
                                   Vector2 center, float pieS, float ol,
                                   out Image fillImage, out float innerDiam)
    {
        // ── 0 — Halo (rendered before/behind the pie outline) ──────────────────
        // Oversized soft-gradient circle; alpha driven by AuraPiePulse each frame.
        float haloSize = pieS + ol * 6f;
        GameObject haloGO = new GameObject(name + " Halo", typeof(RectTransform));
        haloGO.transform.SetParent(parent, false);
        SetBL(haloGO.GetComponent<RectTransform>(), center, new Vector2(haloSize, haloSize));
        Image haloImg         = haloGO.AddComponent<Image>();
        haloImg.sprite        = _glowSprite ?? _circleSprite;
        haloImg.color         = new Color(HaloColorRGB.r, HaloColorRGB.g, HaloColorRGB.b, 0f);
        haloImg.type          = Image.Type.Simple;
        haloImg.raycastTarget = false;

        // ── 1 — Black outline ──────────────────────────────────────────────────
        GameObject pieGO = new GameObject(name, typeof(RectTransform));
        pieGO.transform.SetParent(parent, false);
        SetBL(pieGO.GetComponent<RectTransform>(), center, new Vector2(pieS, pieS));
        Image outline = pieGO.AddComponent<Image>();
        outline.sprite = _circleSprite; outline.color = OutlineColor; outline.type = Image.Type.Simple;

        // ── 2 — Red background ─────────────────────────────────────────────────
        GameObject bgGO = new GameObject(name + " Bg", typeof(RectTransform));
        bgGO.transform.SetParent(pieGO.transform, false);
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = new Vector2(ol, ol); bgRT.offsetMax = new Vector2(-ol, -ol);
        Image bg = bgGO.AddComponent<Image>();
        bg.sprite = _circleSprite; bg.color = BgColor; bg.type = Image.Type.Simple;

        // ── 3 — Coloured fill (radial) ─────────────────────────────────────────
        GameObject fillGO = new GameObject(name + " Fill", typeof(RectTransform));
        fillGO.transform.SetParent(pieGO.transform, false);
        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(ol, ol); fillRT.offsetMax = new Vector2(-ol, -ol);

        fillImage               = fillGO.AddComponent<Image>();
        fillImage.sprite        = _circleSprite;
        fillImage.color         = PulseColorLow;
        fillImage.type          = Image.Type.Filled;
        fillImage.fillMethod    = Image.FillMethod.Radial360;
        fillImage.fillOrigin    = (int)Image.Origin360.Top;
        fillImage.fillClockwise = true;
        fillImage.fillAmount    = 1f;

        // ── 4 — Inner circle (converts the disc into a ring / progress bar) ────
        // Sits on top of the fill so only the ring between the two circles shows.
        float innerInset = pieS * (1f - InnerCircleRatio) * 0.5f;

        // 4a — Inner black outline ring
        GameObject innerGO = new GameObject(name + " Inner", typeof(RectTransform));
        innerGO.transform.SetParent(pieGO.transform, false);
        RectTransform innerRT = innerGO.GetComponent<RectTransform>();
        innerRT.anchorMin = Vector2.zero; innerRT.anchorMax = Vector2.one;
        innerRT.offsetMin = new Vector2(innerInset, innerInset);
        innerRT.offsetMax = new Vector2(-innerInset, -innerInset);
        Image innerOutline = innerGO.AddComponent<Image>();
        innerOutline.sprite = _circleSprite;
        innerOutline.color  = OutlineColor;
        innerOutline.type   = Image.Type.Simple;

        // 4b — Inner dark fill (the solid centre that hides the radial fill inside)
        GameObject innerFillGO = new GameObject(name + " Inner Fill", typeof(RectTransform));
        innerFillGO.transform.SetParent(innerGO.transform, false);
        RectTransform innerFillRT = innerFillGO.GetComponent<RectTransform>();
        innerFillRT.anchorMin = Vector2.zero; innerFillRT.anchorMax = Vector2.one;
        innerFillRT.offsetMin = new Vector2(ol, ol); innerFillRT.offsetMax = new Vector2(-ol, -ol);
        Image innerFill = innerFillGO.AddComponent<Image>();
        innerFill.sprite = _circleSprite;
        innerFill.color  = new Color(0.06f, 0.06f, 0.10f, 1f); // dark centre matching HUD tone
        innerFill.type   = Image.Type.Simple;

        // Expose the usable interior diameter for the symbol overlay
        innerDiam = pieS * InnerCircleRatio - 2f * ol;

        // ── 5 — Attach pulse animator ──────────────────────────────────────────
        AuraPiePulse pulse   = fillGO.AddComponent<AuraPiePulse>();
        pulse.fill           = fillImage;
        pulse.halo           = haloImg;
        pulse.colorLow       = PulseColorLow;
        pulse.colorHigh      = PulseColorHigh;
        pulse.haloColorRGB   = HaloColorRGB;
        pulse.haloAlphaMin   = 0.00f;
        pulse.haloAlphaMax   = 0.50f;
        pulse.frequency      = 1.5f;
    }

    /// <summary>
    /// Builds MaxIcons aura icons starting at (iconStartX, iconCenterY).
    /// Icons grow left-to-right; icon 0 = lowest aura segment.
    ///
    /// When the AuraFlame sprite sheet is loaded the fill layer uses an animated
    /// flame (AuraFlameAnimator cycles the frames) with a vertical fill mask so
    /// the flame "burns lower" as aura depletes.  Falls back to a plain coloured
    /// circle if the sheet is missing.
    /// </summary>
    private static void BuildIcons(Transform parent,
                                    float iconStartX, float iconCenterY,
                                    float iW, float iH, float iGap, float ol,
                                    float initialFill,
                                    Transform[] roots, Image[] fills,
                                    AuraFlameAnimator[] animators = null)
    {
        bool useFlame = _flameFrames != null && _flameFrames.Length > 0;

        for (int i = 0; i < MaxIcons; i++)
        {
            float cx = iconStartX + i * (iW + iGap) + iW * 0.5f;

            if (useFlame)
            {
                // ── Flame icon: just the animated sprite, no outline or background ──
                GameObject iconGO = new GameObject($"Icon {i}", typeof(RectTransform));
                iconGO.transform.SetParent(parent, false);
                SetBL(iconGO.GetComponent<RectTransform>(), new Vector2(cx, iconCenterY), new Vector2(iW, iH));

                Image fillImg      = iconGO.AddComponent<Image>();
                fillImg.sprite     = _flameFrames[0];
                fillImg.color      = AuraColor;
                fillImg.type       = Image.Type.Filled;
                fillImg.fillMethod = Image.FillMethod.Vertical;
                fillImg.fillOrigin = (int)Image.OriginVertical.Bottom;
                fillImg.fillAmount = initialFill;
                fillImg.preserveAspect = false;
                if (_heatDistortMat != null) fillImg.material = _heatDistortMat;

                AuraFlameAnimator anim = iconGO.AddComponent<AuraFlameAnimator>();
                anim.frames   = _flameFrames;
                anim.slowFps  = 12f;
                anim.fastFps  = 24f;

                roots[i] = iconGO.transform;
                fills[i] = fillImg;
                if (animators != null) animators[i] = anim;
            }
            else
            {
                // ── Fallback: three-layer circle icon (outline → red bg → coloured fill) ──
                GameObject iconGO = new GameObject($"Icon {i}", typeof(RectTransform));
                iconGO.transform.SetParent(parent, false);
                SetBL(iconGO.GetComponent<RectTransform>(), new Vector2(cx, iconCenterY), new Vector2(iW, iH));
                Image outImg = iconGO.AddComponent<Image>();
                outImg.sprite = _circleSprite; outImg.color = OutlineColor; outImg.type = Image.Type.Simple;

                GameObject bgGO = new GameObject("Bg", typeof(RectTransform));
                bgGO.transform.SetParent(iconGO.transform, false);
                RectTransform bgRT = bgGO.GetComponent<RectTransform>();
                bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
                bgRT.offsetMin = new Vector2(ol, ol); bgRT.offsetMax = new Vector2(-ol, -ol);
                Image bgImg = bgGO.AddComponent<Image>();
                bgImg.sprite = _circleSprite; bgImg.color = BgColor; bgImg.type = Image.Type.Simple;

                GameObject fillGO = new GameObject("Fill", typeof(RectTransform));
                fillGO.transform.SetParent(iconGO.transform, false);
                RectTransform fillRT = fillGO.GetComponent<RectTransform>();
                fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
                fillRT.offsetMin = new Vector2(ol, ol); fillRT.offsetMax = new Vector2(-ol, -ol);
                Image fillImg      = fillGO.AddComponent<Image>();
                fillImg.sprite     = _circleSprite;
                fillImg.color      = AuraColor;
                fillImg.type       = Image.Type.Filled;
                fillImg.fillMethod = Image.FillMethod.Vertical;
                fillImg.fillOrigin = (int)Image.OriginVertical.Bottom;
                fillImg.fillAmount = initialFill;

                roots[i] = iconGO.transform;
                fills[i] = fillImg;
            }
        }
    }

    // ── Symbol overlay ─────────────────────────────────────────────────────────

    /// <summary>
    /// Places a player symbol centred on the pie circle.
    ///
    /// A circular Mask (using the same circle sprite) clips everything to the
    /// inner-circle boundary.  The symbol Image is sized at 1.5× the mask so
    /// that the visible sprite content fills the full circle even when the source
    /// sprite has transparent padding around the artwork.
    ///
    /// All symbols end up at the same pixel area (preserveAspect = false), so
    /// every player's icon appears the same size regardless of sprite dimensions.
    /// </summary>
    private static void CreateSymbolInCircle(Transform parent, string name,
                                              Vector2 center, float innerDiam,
                                              PlayerSymbolEntry entry,
                                              out Image img, out RectTransform symRT)
    {
        // ── Circular mask container ──────────────────────────────────────────────
        GameObject maskGO = new GameObject(name + " Mask", typeof(RectTransform));
        maskGO.transform.SetParent(parent, false);
        SetBL(maskGO.GetComponent<RectTransform>(), center, new Vector2(innerDiam, innerDiam));

        Image maskImg         = maskGO.AddComponent<Image>();
        maskImg.sprite        = _circleSprite;
        maskImg.color         = Color.white;
        maskImg.type          = Image.Type.Simple;
        maskImg.raycastTarget = false;

        Mask maskComp = maskGO.AddComponent<Mask>();
        maskComp.showMaskGraphic = false;

        // ── Symbol image (child of mask) ─────────────────────────────────────────
        GameObject symGO = new GameObject(name, typeof(RectTransform));
        symGO.transform.SetParent(maskGO.transform, false);
        symRT = symGO.GetComponent<RectTransform>();

        img                = symGO.AddComponent<Image>();
        img.type           = Image.Type.Simple;
        img.preserveAspect = false;
        img.raycastTarget  = false;

        // Apply entry-driven scale and offset (or sensible defaults if no entry yet)
        ApplySymbol(img, symRT, innerDiam, entry);
    }

    /// <summary>
    /// Sets the symbol sprite/colour and applies hudSymbolScale + hudOffset from the entry.
    /// hudSymbolScale drives how much the image overflows the mask (fills circle).
    /// hudOffset shifts the image within the mask to align the artwork's visual centre.
    /// Both are set per-symbol in the PlayerSymbolLibrary Inspector.
    /// </summary>
    private static void ApplySymbol(Image img, RectTransform symRT, float innerDiam, PlayerSymbolEntry entry)
    {
        if (img == null) return;

        if (entry != null && entry.sprite != null)
        {
            img.sprite = entry.sprite;
            img.color  = entry.symbolColor;

            if (symRT != null)
            {
                // Overflow anchors: ext = (scale-1)/2 on each side so total size = scale × mask
                float ext = (entry.hudSymbolScale - 1f) * 0.5f;
                symRT.anchorMin        = new Vector2(-ext, -ext);
                symRT.anchorMax        = new Vector2(1f + ext, 1f + ext);
                symRT.offsetMin        = Vector2.zero;
                symRT.offsetMax        = Vector2.zero;
                // hudOffset is in normalised circle-diameter units; convert to pixels
                symRT.anchoredPosition = entry.hudOffset * innerDiam;
            }
        }
        else
        {
            img.color = Color.clear;
            if (symRT != null)
            {
                // Reset to neutral (1× = no oversize, centred)
                symRT.anchorMin        = Vector2.zero;
                symRT.anchorMax        = Vector2.one;
                symRT.offsetMin        = Vector2.zero;
                symRT.offsetMax        = Vector2.zero;
                symRT.anchoredPosition = Vector2.zero;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Team list
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called when the owner's team status changes (join, leave, promotion, etc.)
    /// Re-subscribes to the new Team object and rebuilds the visual list.
    /// </summary>
    private void OnOwnerTeamChanged()
    {
        ResubscribeTeam();
        RefreshTeamList();
    }

    /// <summary>
    /// Switches the membership-changed subscription to the owner's current team.
    /// Called on SetOwner, on status change, and after every UI rebuild.
    /// </summary>
    private void ResubscribeTeam()
    {
        Team newTeam = _owner?.teamController?.team;
        if (newTeam == _subscribedTeam) return;

        UnsubscribeFromTeam(_subscribedTeam);
        _subscribedTeam = newTeam;

        if (_subscribedTeam != null)
            _subscribedTeam.OnMembershipChanged += RefreshTeamList;
    }

    private void UnsubscribeFromTeam(Team team)
    {
        if (team != null) team.OnMembershipChanged -= RefreshTeamList;
        _subscribedTeam = null;
    }

    /// <summary>
    /// Rebuilds the team list rows above the name label.
    /// Own name always stays at the bottom; teammates stack upward.
    /// When solo (no team), only the plain name is shown.
    /// </summary>
    private void RefreshTeamList()
    {
        // Destroy existing rows
        foreach (var go in _teamRows)
            if (go != null) UnityEngine.Object.Destroy(go);
        _teamRows.Clear();

        // Always update own name label (adds/removes status icon)
        UpdateOwnNameLabel();

        if (_teamListRoot == null || _owner == null)
        {
            Debug.Log($"[TeamList] Early exit — _teamListRoot={_teamListRoot != null}, _owner={_owner != null}");
            return;
        }

        Team team = _owner.teamController?.team;
        if (team == null)
        {
            Debug.Log($"[TeamList] {_owner.playerName} is Solo — no rows to build.");
            return;
        }

        // Build one row per other team member, growing upward from the name label
        List<LocalPlayerManager> all = team.GetAllMembers();
        Debug.Log($"[TeamList] {_owner.playerName} rebuilding — {all.Count} member(s), nameCenter={_nameLabelCenter}, nameSize={_nameLabelSize}");
        int rowIndex = 1;
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i] == _owner) continue;

            float rowCenterY = _nameLabelCenter.y + _nameLabelSize.y * rowIndex;
            rowIndex++;
            Debug.Log($"[TeamList] Row for {all[i].playerName} at Y={rowCenterY}");

            // Row container — left-edge anchored, HLG lays out [icon | text], CSF sizes width
            float rowLeftX = _nameLabelCenter.x - _nameLabelSize.x * 0.5f;
            var go         = new GameObject($"TeamRow{i}", typeof(RectTransform));
            go.transform.SetParent(_teamListRoot.transform, false);

            var rt               = go.GetComponent<RectTransform>();
            rt.anchorMin         = Vector2.zero;
            rt.anchorMax         = Vector2.zero;
            rt.pivot             = new Vector2(0f, 0.5f);
            rt.anchoredPosition  = new Vector2(rowLeftX, rowCenterY);
            rt.sizeDelta         = new Vector2(0f, _nameLabelSize.y);

            var hlg                   = go.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment        = TextAnchor.MiddleLeft;
            hlg.spacing               = 0f;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth      = true;
            hlg.childControlHeight     = true;
            hlg.padding                = new RectOffset(2, 2, 0, 0);

            var csf              = go.AddComponent<ContentSizeFitter>();
            csf.horizontalFit    = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit      = ContentSizeFitter.FitMode.Unconstrained;

            // Dark background — ignores HLG layout, fills container via anchors
            var bgGO             = new GameObject("Bg", typeof(RectTransform));
            bgGO.transform.SetParent(go.transform, false);
            var bgRT             = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin       = Vector2.zero;  bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin       = Vector2.zero;  bgRT.offsetMax = Vector2.zero;
            var bgImg            = bgGO.AddComponent<Image>();
            bgImg.sprite         = _squareSprite;
            bgImg.color          = new Color(0f, 0f, 0f, 0.55f);
            bgImg.type           = Image.Type.Simple;
            bgImg.raycastTarget  = false;
            var bgLE             = bgGO.AddComponent<LayoutElement>();
            bgLE.ignoreLayout    = true;

            // Icon slot — square, same height as row
            float iconSz     = _nameLabelSize.y;
            var slotGO       = new GameObject("IconSlot", typeof(RectTransform));
            slotGO.transform.SetParent(go.transform, false);
            var slotLE       = slotGO.AddComponent<LayoutElement>();
            slotLE.preferredWidth  = iconSz;
            slotLE.preferredHeight = iconSz;
            slotLE.flexibleWidth   = 0f;

            // Human figure — fills the slot at 75 % scale for visual comfort
            var figGO        = new GameObject("Figure", typeof(RectTransform));
            figGO.transform.SetParent(slotGO.transform, false);
            var figRT        = figGO.GetComponent<RectTransform>();
            figRT.anchorMin  = Vector2.zero;  figRT.anchorMax = Vector2.one;
            figRT.offsetMin  = figRT.offsetMax = Vector2.zero;
            figGO.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            var figImg       = figGO.AddComponent<Image>();
            figImg.sprite    = _humanFigureSprite;
            figImg.preserveAspect = true;
            figImg.raycastTarget  = false;

            RoleIcon memberRole = GetRoleIcon(all[i]);
            figImg.color        = (memberRole != RoleIcon.None) ? Color.white : Color.clear;

            // Crown — leader only, random sprite, scaled to 50 % with head-alignment offsets
            if (memberRole == RoleIcon.Leader && _crownSprites != null && _crownSprites.Length > 0)
            {
                var crownGO      = new GameObject("Crown", typeof(RectTransform));
                crownGO.transform.SetParent(slotGO.transform, false);
                var crownRT      = crownGO.GetComponent<RectTransform>();
                crownRT.anchorMin = new Vector2(0.05f, 0.65f);
                crownRT.anchorMax = new Vector2(0.95f, 1.10f);
                crownRT.offsetMin = new Vector2(-2.1f,  2.3f);
                crownRT.offsetMax = new Vector2(-2.1f,  2.3f);
                crownGO.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
                var crownImg     = crownGO.AddComponent<Image>();
                crownImg.sprite  = _crownSprites[UnityEngine.Random.Range(0, _crownSprites.Length)];
                crownImg.color   = Color.white;
                crownImg.preserveAspect = true;
                crownImg.raycastTarget  = false;
            }

            // Text — TMP preferred width drives HLG / ContentSizeFitter
            var textGO           = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            var textLE           = textGO.AddComponent<LayoutElement>();
            textLE.flexibleWidth = 0f;
            var tmp              = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text             = all[i].playerName;
            tmp.color            = new Color(0.90f, 0.90f, 0.90f);
            tmp.fontSize         = Mathf.Max(8f, _nameLabelSize.y * 0.55f);
            tmp.alignment        = TextAlignmentOptions.BottomLeft;
            tmp.fontStyle        = FontStyles.Normal;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;

            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            _teamRows.Add(go);
        }
    }

    /// <summary>
    /// Updates the owner's name label text and role icon without rebuilding the full label.
    /// </summary>
    private void UpdateOwnNameLabel()
    {
        if (_nameText == null) return;
        _nameText.text = (_owner != null) ? _owner.playerName : _playerName;
        ApplyRoleIconImages(_localFigureImg, _localCrownImg, GetRoleIcon(_owner));
    }

    /// <summary>Returns the RoleIcon for a player based on their current team status.</summary>
    private static RoleIcon GetRoleIcon(LocalPlayerManager player)
    {
        if (player?.teamController == null) return RoleIcon.None;
        return player.teamController.CurrentStatus switch
        {
            TeamController.Status.Leader   => RoleIcon.Leader,
            TeamController.Status.Follower => RoleIcon.Follower,
            _                              => RoleIcon.None
        };
    }

    /// <summary>
    /// Shows/hides the human-figure and crown images in a name label icon slot.
    /// Leader   → figure visible + random crown visible.
    /// Follower → figure visible, crown hidden.
    /// None     → both hidden.
    /// </summary>
    private static void ApplyRoleIconImages(Image figImg, Image crownImg, RoleIcon role)
    {
        if (figImg == null) return;
        figImg.color = (role != RoleIcon.None) ? Color.white : Color.clear;

        if (crownImg == null) return;
        if (role == RoleIcon.Leader && _crownSprites != null && _crownSprites.Length > 0)
        {
            crownImg.sprite = _crownSprites[UnityEngine.Random.Range(0, _crownSprites.Length)];
            crownImg.color  = Color.white;
        }
        else
        {
            crownImg.color = Color.clear;
        }
    }

    // Name is shown plain; the icon slot provides the visual role indicator
    private static string FormatMemberLabel(LocalPlayerManager member) => member.playerName;

    private void RefreshLocalSymbol()
    {
        if (_owner == null || _localSymbolImage == null) return;
        _lastLocalSymbol = _owner.ActiveSymbol;
        ApplySymbol(_localSymbolImage, _localSymbolRT, _symbolInnerDiam, _lastLocalSymbol);
    }

    // ── RectTransform helper — bottom-left anchor ──────────────────────────────

    private static void SetBL(RectTransform rt, Vector2 center, Vector2 size)
    {
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.zero;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = center;
        rt.sizeDelta        = size;
    }

    // ── Sprite generation ──────────────────────────────────────────────────────

    private static void EnsureSprites()
    {
        if (_squareSprite    == null) _squareSprite    = MakeSquareSprite();
        if (_circleSprite    == null) _circleSprite    = MakeCircleSprite(64);
        if (_glowSprite      == null) _glowSprite      = MakeGlowSprite(64);
        // Load the 9-frame flame sprite sheet from Resources/UI/AuraFlame
        if (_flameFrames == null || _flameFrames.Length == 0)
        {
            _flameFrames = Resources.LoadAll<Sprite>("UI/AuraFlame");
            if (_flameFrames == null || _flameFrames.Length == 0)
                Debug.LogWarning("[PlayerStatBarUI] AuraFlame sprite sheet not found in Resources/UI/. " +
                                 "Falling back to circle icons.");
        }

        // Load the heat-distortion material (created via Tools ▶ Create Heat Distortion Flame Material)
        if (_heatDistortMat == null)
            _heatDistortMat = Resources.Load<Material>("UI/HeatDistortionFlame");

        // Role icons
        if (_humanFigureSprite == null)
        {
            _humanFigureSprite = Resources.Load<Sprite>("Sprites/HumanFigure");
            if (_humanFigureSprite == null)
                Debug.LogWarning("[PlayerStatBarUI] HumanFigure sprite not found in Resources/Sprites/.");
        }
        if (_crownSprites == null || _crownSprites.Length == 0)
        {
            _crownSprites = Resources.LoadAll<Sprite>("Sprites/Crowns");
            if (_crownSprites == null || _crownSprites.Length == 0)
                Debug.LogWarning("[PlayerStatBarUI] No crown sprites found in Resources/Sprites/Crowns/.");
        }
    }

    private static Sprite MakeSquareSprite()
    {
        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
    }

    private static Sprite MakeCircleSprite(int res)
    {
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float r = res * 0.5f, cx = r - 0.5f, cy = r - 0.5f;
        Color[] px = new Color[res * res];
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float dx = x - cx, dy = y - cy;
                px[y * res + x] = new Color(1f, 1f, 1f,
                    Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy)));
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// Generates a soft radial-gradient circle (bright centre, smooth quadratic falloff
    /// to transparent at the edge) used as the pie halo glow layer.
    /// </summary>
    private static Sprite MakeGlowSprite(int res)
    {
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float r = res * 0.5f, cx = r - 0.5f, cy = r - 0.5f;
        Color[] px = new Color[res * res];
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float dist  = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float t     = Mathf.Clamp01(dist / r);
                // Quadratic falloff: 1 at centre → 0 at edge (softer than linear)
                float alpha = 1f - t * t;
                px[y * res + x] = new Color(1f, 1f, 1f, alpha);
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
    }

}

