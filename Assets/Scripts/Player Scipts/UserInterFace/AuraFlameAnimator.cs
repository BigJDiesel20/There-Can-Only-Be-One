using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cycles through a set of Sprites on a UI Image to produce a looping flame animation.
/// Attach to the same GameObject as the Image.
/// The Image's fillAmount continues to be driven externally (aura drain/fill),
/// so the flame visually burns lower as aura depletes — exactly like the old
/// circle icons, just animated.
/// Speed lerps between slowFps (depleted/inactive) and fastFps (full/active)
/// based on the current fillAmount, so active icons flicker faster.
/// </summary>
public class AuraFlameAnimator : MonoBehaviour
{
    [Tooltip("Animation frames in playback order (left-to-right, top-to-bottom from sprite sheet).")]
    public Sprite[] frames;

    [Tooltip("Flicker speed (fps) when the icon is fully depleted / inactive.")]
    public float slowFps = 12f;

    [Tooltip("Flicker speed (fps) when the icon is fully active.")]
    public float fastFps = 24f;

    [Tooltip("Each icon randomly speeds up or slows down by 0–this% on startup. 0.05 = 0–5% variation.")]
    [Range(0f, 0.5f)]
    public float fpsVariance = 0.05f;

    /// <summary>Set true on the icon that is actively losing fill; false on all others.</summary>
    [HideInInspector] public bool isDraining;

    private Image _image;
    private float _timer;
    private int   _frameIndex;
    private float _speedMultiplier;   // per-icon random tweak, baked in Start

    void Start()
    {
        _image = GetComponent<Image>();
        if (_image == null || frames == null || frames.Length == 0) return;

        // Random start frame so icons don't all flicker in sync.
        _frameIndex = Random.Range(0, frames.Length);
        _image.sprite = frames[_frameIndex];

        // Per-icon speed multiplier (0–5% faster or slower), applied every frame.
        _speedMultiplier = 1f + Random.Range(-fpsVariance, fpsVariance);

        // Random phase offset so even same-frame icons drift apart immediately.
        // Uses slowFps as the baseline since most icons start full.
        float startInterval = 1f / Mathf.Max(slowFps * _speedMultiplier, 0.1f);
        _timer = Random.Range(0f, startInterval);
    }

    void Update()
    {
        if (_image == null || frames == null || frames.Length == 0) return;

        // isDraining is set externally by UpdateIcons — fast on the draining icon, slow on all others.
        float targetFps = (isDraining ? fastFps : slowFps) * _speedMultiplier;
        float interval  = 1f / Mathf.Max(targetFps, 0.1f);

        _timer += Time.deltaTime;

        if (_timer >= interval)
        {
            _timer -= interval;
            _frameIndex = (_frameIndex + 1) % frames.Length;
            _image.sprite = frames[_frameIndex];
        }
    }
}
