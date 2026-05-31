using UnityEngine;

/// <summary>
/// Per-slot data for one player cursor: sprite plus its own colour and glow settings.
/// Adjust each slot independently in the PlayerSymbolLibrary Inspector.
/// </summary>
[System.Serializable]
public class PlayerSymbolEntry
{
    [Tooltip("Symbol sprite shown on this player's cursor.")]
    public Sprite sprite;

    [Tooltip("Tint colour multiplied onto the symbol texture. Alpha controls opacity.")]
    public Color symbolColor = new Color(1f, 0.8f, 0.2f, 1f);

    [Tooltip("HDR glow colour emitted around the symbol. " +
             "Push intensity above 1 in the HDR colour picker to drive bloom.")]
    [ColorUsage(true, true)]
    public Color glowColor = new Color(3f, 2.4f, 0.6f, 1f);

    [Tooltip("Glow brightness fed into the SymbolGlow shader. " +
             "0 = flat symbol, higher values = stronger bloom halo.")]
    [Range(0f, 20f)]
    public float glowIntensity = 3f;

    // ── Placement (world-space cursor) ────────────────────────────────────────
    [Header("Placement")]
    [Tooltip("World-space offset from the target's position. " +
             "Default (0, 2, 0) floats the symbol above the target's head.")]
    public Vector3 positionOffset = new Vector3(0f, 2f, 0f);

    [Tooltip("Uniform scale of the cursor symbol. 1 = original prefab size.")]
    [Range(0.1f, 5f)]
    public float scale = 1f;

    // ── HUD Pie Symbol ─────────────────────────────────────────────────────────
    [Header("HUD Pie Symbol")]
    [Tooltip("How much to scale the symbol inside the pie circle.\n" +
             "1.5 = default (fills circle for sprites with ~25% padding).\n" +
             "Raise if the symbol artwork is small relative to the texture bounds.")]
    [Range(0.5f, 5f)]
    public float hudSymbolScale = 1.5f;

    [Tooltip("Normalized offset that shifts the symbol within the pie circle to " +
             "align the artwork's visual centre with the circle centre.\n" +
             "Each axis is in units of circle diameter: 0.1 = shift 10% of diameter.\n" +
             "Adjust per symbol until the icon looks centred.")]
    public Vector2 hudOffset = Vector2.zero;
}

/// <summary>
/// One-stop shop for all player cursor settings.
/// Each of the 16 slots has its own sprite, tint colour, glow colour and glow intensity.
///
/// Place the asset in Assets/Resources/ named exactly "PlayerSymbolLibrary"
/// so it auto-loads at runtime — no Inspector wiring needed.
///
/// Create via: Assets ▶ Create ▶ Game ▶ Player Symbol Library
/// </summary>
[CreateAssetMenu(fileName = "PlayerSymbolLibrary", menuName = "Game/Player Symbol Library")]
public class PlayerSymbolLibrary : ScriptableObject
{
    [Tooltip("One entry per player slot. Index 0 = Player 1, index 15 = Player 16.")]
    public PlayerSymbolEntry[] symbols = new PlayerSymbolEntry[16];

    // ── Singleton loaded from Resources ───────────────────────────────────────
    private static PlayerSymbolLibrary _instance;
    public static PlayerSymbolLibrary Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<PlayerSymbolLibrary>("PlayerSymbolLibrary");
            return _instance;
        }
    }

    /// <summary>Returns the sprite for the given 0-based player slot, or null if out of range.</summary>
    public Sprite GetSymbol(int playerIndex)
    {
        var entry = GetEntry(playerIndex);
        return entry?.sprite;
    }

    /// <summary>Returns the full entry (sprite + appearance) for the given 0-based slot, or null.</summary>
    public PlayerSymbolEntry GetEntry(int playerIndex)
    {
        if (symbols == null || playerIndex < 0 || playerIndex >= symbols.Length) return null;
        return symbols[playerIndex];
    }
}
