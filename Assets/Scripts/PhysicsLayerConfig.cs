using UnityEngine;

/// <summary>
/// Configures physics layer collision rules at game startup.
/// Uses [RuntimeInitializeOnLoadMethod] so it runs automatically before any
/// scene is loaded — no MonoBehaviour or manual wiring required.
///
/// AuraField layer: only interacts with Player layer.
/// All other cross-layer pairs involving AuraField are disabled at the
/// PhysX broadphase, eliminating unnecessary trigger pair processing.
/// </summary>
public static class PhysicsLayerConfig
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Configure()
    {
        int auraLayer   = LayerMask.NameToLayer("AuraField");
        int playerLayer = LayerMask.NameToLayer("Player");

        if (auraLayer < 0)
        {
            Debug.LogWarning("[PhysicsLayerConfig] 'AuraField' layer not found — skipping setup.");
            return;
        }

        if (playerLayer < 0)
        {
            Debug.LogWarning("[PhysicsLayerConfig] 'Player' layer not found — skipping setup.");
            return;
        }

        // Block AuraField from interacting with every layer...
        for (int i = 0; i < 32; i++)
            Physics.IgnoreLayerCollision(auraLayer, i, true);

        // ...except Player (the only layer AuraField triggers need to detect)
        Physics.IgnoreLayerCollision(auraLayer, playerLayer, false);

        Debug.Log($"[PhysicsLayerConfig] AuraField (layer {auraLayer}) " +
                  $"configured to interact with Player (layer {playerLayer}) only.");
    }
}
