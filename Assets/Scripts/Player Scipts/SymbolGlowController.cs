using UnityEngine;

/// <summary>
/// Drives the Custom/SymbolGlow shader properties on a SpriteRenderer
/// without creating extra material instances — uses MaterialPropertyBlock.
///
/// Drop this on any GameObject that has a SpriteRenderer using the
/// SymbolGlow material.  Tweak SymbolColor and GlowIntensity in the
/// Inspector, or call SetColor / SetGlow from code at runtime.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SymbolGlowController : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────
    [Header("Symbol")]
    [Tooltip("Tint applied to the sprite texture.")]
    [SerializeField] private Color _symbolColor = new Color(1f, 0.8f, 0.2f, 1f);

    [Header("Glow")]
    [Tooltip("HDR colour of the glow emission.  Use values > 1 for stronger bloom.")]
    [SerializeField] private Color _glowColor = new Color(1f, 0.8f, 0.2f, 1f);

    [Tooltip("Multiplier on top of the glow colour.  0 = no glow.")]
    [SerializeField, Range(0f, 20f)] private float _glowIntensity = 0f;

    // ── Shader property IDs (cached for performance) ──────────────────────
    static readonly int ID_BaseColor      = Shader.PropertyToID("_BaseColor");
    static readonly int ID_EmissiveColor  = Shader.PropertyToID("_EmissiveColor");
    static readonly int ID_GlowIntensity  = Shader.PropertyToID("_GlowIntensity");

    // ── Private ───────────────────────────────────────────────────────────
    SpriteRenderer      _renderer;
    MaterialPropertyBlock _block;

    // ── Unity callbacks ───────────────────────────────────────────────────
    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _block    = new MaterialPropertyBlock();
        Apply();
    }

    void OnValidate()          // live-update while tweaking in the Inspector
    {
        if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
        if (_block    == null) _block    = new MaterialPropertyBlock();
        Apply();
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>Change the symbol tint colour at runtime.</summary>
    public void SetColor(Color color)
    {
        _symbolColor = color;
        Apply();
    }

    /// <summary>Change both the glow colour and its intensity at runtime.</summary>
    public void SetGlow(Color color, float intensity)
    {
        _glowColor     = color;
        _glowIntensity = Mathf.Max(0f, intensity);
        Apply();
    }

    /// <summary>Set only the glow intensity, keeping the current glow colour.</summary>
    public void SetGlowIntensity(float intensity)
    {
        _glowIntensity = Mathf.Max(0f, intensity);
        Apply();
    }

    /// <summary>Convenience: pulse the glow using a 0-1 normalised value.</summary>
    public void SetGlowNormalized(float t, float maxIntensity = 10f)
    {
        _glowIntensity = Mathf.Lerp(0f, maxIntensity, Mathf.Clamp01(t));
        Apply();
    }

    // ── Internal ──────────────────────────────────────────────────────────
    void Apply()
    {
        _renderer.GetPropertyBlock(_block);
        _block.SetColor(ID_BaseColor,     _symbolColor);
        _block.SetColor(ID_EmissiveColor, _glowColor);
        _block.SetFloat(ID_GlowIntensity, _glowIntensity);
        _renderer.SetPropertyBlock(_block);
    }

    // ── Properties (for external code that prefers property syntax) ───────
    public Color SymbolColor
    {
        get => _symbolColor;
        set { _symbolColor = value; Apply(); }
    }

    public Color GlowColor
    {
        get => _glowColor;
        set { _glowColor = value; Apply(); }
    }

    public float GlowIntensity
    {
        get => _glowIntensity;
        set { _glowIntensity = Mathf.Max(0f, value); Apply(); }
    }
}
