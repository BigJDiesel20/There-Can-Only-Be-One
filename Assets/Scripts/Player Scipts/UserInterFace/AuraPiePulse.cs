using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the bright pulsating blue glow on the aura pie.
///
/// Two simultaneous effects:
///   1. Fill colour — oscillates between colorLow and colorHigh each cycle.
///   2. Halo alpha  — the oversized soft circle behind the pie pulses from
///                    haloAlphaMin to haloAlphaMax in sync with the fill.
///
/// Wired up by PlayerStatBarUI.CreatePie() at build time; no Inspector setup needed.
/// </summary>
public class AuraPiePulse : MonoBehaviour
{
    // ── Set by CreatePie ───────────────────────────────────────────────────────
    [HideInInspector] public Image fill;            // the radial fill Image
    [HideInInspector] public Image halo;            // oversized soft-glow circle behind pie

    [HideInInspector] public Color colorLow;        // fill colour at pulse trough
    [HideInInspector] public Color colorHigh;       // fill colour at pulse peak

    [HideInInspector] public Color haloColorRGB;    // halo tint (alpha driven by pulse)
    [HideInInspector] public float haloAlphaMin;
    [HideInInspector] public float haloAlphaMax;

    [HideInInspector] public float frequency = 1.5f; // full cycles per second

    // ── Runtime ────────────────────────────────────────────────────────────────

    private void Update()
    {
        // Smooth 0 → 1 → 0 sinusoid
        float t = (Mathf.Sin(Time.time * frequency * Mathf.PI * 2f) + 1f) * 0.5f;

        // 1 — Pulse fill brightness
        if (fill != null)
            fill.color = Color.Lerp(colorLow, colorHigh, t);

        // 2 — Pulse halo alpha
        if (halo != null)
        {
            Color c = haloColorRGB;
            c.a     = Mathf.Lerp(haloAlphaMin, haloAlphaMax, t);
            halo.color = c;
        }
    }
}
