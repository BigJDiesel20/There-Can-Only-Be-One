using UnityEngine;
using Rewired;
using System;

/// <summary>
/// State 3 — PreGame
/// One-time setup that runs immediately before the battle starts.
/// Calculates and assigns each player's split-screen camera viewport,
/// then transitions straight to Battle.
/// Future home of: countdown timer, "Get Ready" screen, match settings.
/// </summary>
[Serializable]
public class PreGame : IGameState
{
    public GameManager gameManager { get; set; }

    public PreGame(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public void OnLoad()
    {
        BuildAllCharacters();   // initialises controllers + subscribes stat events
        SetAuraMaximum();       // fires OnPercentageChange with combined max — UI already subscribed
        SetViewPort();          // needs cameraControler, so must run after BuildAllCharacters
        gameManager.ChangeState("Battle");
    }

    public void OnUpdate() { } // Transient state — execution goes straight to Battle.

    // ── Character build ───────────────────────────────────────────────────────

    /// <summary>
    /// Calls BuildCharacter on every player slot, completing the initialisation
    /// that was deferred from CharacterSelect. Must run before SetAuraMaximum so
    /// that stat event subscriptions are live when the combined max is applied.
    /// </summary>
    void BuildAllCharacters()
    {
        int playerCount = gameManager.playerSlot.Count;
        for (int i = 0; i < playerCount; i++)
        {
            LocalPlayerManager player = gameManager.playerSlot[i].GetComponent<LocalPlayerManager>();
            player.BuildCharacter();
        }
        Debug.Log($"[PreGame] Built {playerCount} character(s).");
    }

    // ── Viewport layout ───────────────────────────────────────────────────────

    /// <summary>
    /// Divides the screen into an equal grid and assigns a camera Rect to
    /// each player based on total player count.
    ///
    /// Layout map:
    ///   1  player  → 1×1  (full screen)
    ///   2  players → 2×1
    ///   3–4        → 2×2
    ///   5–9        → 3×3
    ///   10–12      → 3×4
    ///   13–16      → 4×4
    /// </summary>
    void SetViewPort()
    {
        int playerCount = gameManager.playerSlot.Count;
        float rows = 1f, cols = 1f;

        if      (playerCount == 1)                        { rows = 1; cols = 1; }
        else if (playerCount <= 2)                        { rows = 2; cols = 1; }
        else if (playerCount > 2  && playerCount <= 4)    { rows = 2; cols = 2; }
        else if (playerCount > 4  && playerCount <= 9)    { rows = 3; cols = 3; }
        else if (playerCount > 9  && playerCount <= 12)   { rows = 3; cols = 4; }
        else if (playerCount > 12 && playerCount <= 16)   { rows = 4; cols = 4; }

        Debug.Log($"[PreGame] Viewport grid: {rows}×{cols} for {playerCount} player(s).");

        int index = 0;
        for (int i = 0; i < rows && index < playerCount; i++)
        {
            for (int j = 0; j < cols && index < playerCount; j++)
            {
                LocalPlayerManager player = gameManager.playerSlot[index].GetComponent<LocalPlayerManager>();
                Rect viewport = new Rect(
                    i * (1f / rows),
                    (1f - (1f / cols)) - (j * (1f / cols)),
                    1f / rows,
                    1f / cols);

                player.SetCameraRect(viewport);
                Debug.Log($"[PreGame] Player {index + 1} viewport: {viewport}");
                index++;
            }
        }
    }

    // ── Aura maximum scaling ──────────────────────────────────────────────────

    /// <summary>
    /// Sets every player's aura maximum to (playerCount × each player's base aura max).
    /// Called once before Battle so the pie fill reads currentAura / combinedMax.
    /// </summary>
    void SetAuraMaximum()
    {
        int playerCount = gameManager.playerSlot.Count;
        if (playerCount == 0) return;

        LocalPlayerManager firstPlayer = gameManager.playerSlot[0].GetComponent<LocalPlayerManager>();
        float baseAuraMax  = firstPlayer.statManager.GetAuraMax();
        float combinedMax  = playerCount * baseAuraMax;

        Debug.Log($"[PreGame] Setting aura max to {combinedMax} ({playerCount} players × {baseAuraMax} each).");

        for (int i = 0; i < playerCount; i++)
        {
            LocalPlayerManager player = gameManager.playerSlot[i].GetComponent<LocalPlayerManager>();
            player.statManager.AdjustAuraMaximum(combinedMax);
        }
    }

    public void ChangeState(string state) => gameManager.ChangeState(state);
}
