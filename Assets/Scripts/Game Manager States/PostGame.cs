using UnityEngine;
using Rewired;
using System;

/// <summary>
/// State 5 — PostGame
/// Entered when Battle detects a winner. Displays the result and offers four
/// options navigated with D-Pad Up / Down, confirmed with A.
///
///   0 — Replay            Same players, same characters → PreGame
///   1 — Choose Characters Same players, new characters  → CharacterSelect
///   2 — Leave             Full teardown                 → SplashScreen
///   3 — Quit              Application.Quit()
///
/// Characters remain in the scene while the menu is open; teardown is
/// handled per-option so the correct state is left for the destination.
/// </summary>
[Serializable]
public class PostGame : IGameState
{
    public GameManager gameManager { get; set; }

    // ── Row indices ───────────────────────────────────────────────────────────
    const int RowReplay     = 0;
    const int RowNewChars   = 1;
    const int RowLeave      = 2;
    const int RowQuit       = 3;
    const int RowCount      = 4;

    int         _selectedRow;
    PostGameUI  _ui;

    public PostGame(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public void OnLoad()
    {
        _selectedRow = 0;

        string winner = gameManager.lastWinnerName.Length > 0
            ? gameManager.lastWinnerName
            : "Unknown";

        Debug.Log($"[PostGame] Winner: {winner}. Choose an option.");
        LogMenu();

        _ui = new PostGameUI();
        _ui.Initialize(winner);
        _ui.Refresh(_selectedRow);
    }

    public void OnExit()
    {
        gameManager.lastWinnerName = string.Empty;

        _ui?.Destroy();
        _ui = null;
    }

    public void OnUpdate()
    {
        // Any joined player can navigate — use player 0 as the primary driver.
        Player pad = ReInput.players.GetPlayer(0);

        bool dirty = false;

        if (pad.GetButtonDown("D-Pad Up"))
        {
            _selectedRow = (_selectedRow - 1 + RowCount) % RowCount;
            LogMenu();
            dirty = true;
        }
        if (pad.GetButtonDown("D-Pad Down"))
        {
            _selectedRow = (_selectedRow + 1) % RowCount;
            LogMenu();
            dirty = true;
        }

        if (dirty)
            _ui?.Refresh(_selectedRow);

        if (pad.GetButtonDown("A"))
            Confirm();
    }

    // ── Option handlers ───────────────────────────────────────────────────────

    void Confirm()
    {
        Debug.Log($"[PostGame] Selected: {RowLabel(_selectedRow)}");
        switch (_selectedRow)
        {
            case RowReplay:     Replay();           break;
            case RowNewChars:   ChooseCharacters(); break;
            case RowLeave:      Leave();            break;
            case RowQuit:       Quit();             break;
        }
    }

    /// <summary>
    /// Resets every player's stats to their cached starting values (health,
    /// stamina, aura current value). The combined aura max is left untouched.
    /// Battle's SpawnCharacters then repositions everyone — no teardown or
    /// reinstantiation needed.
    /// </summary>
    void Replay()
    {
        for (int i = 0; i < gameManager.playerSlot.Count; i++)
        {
            LocalPlayerManager player =
                gameManager.playerSlot[i].GetComponent<LocalPlayerManager>();
            player.statManager.ResetStats();
        }

        Debug.Log("[PostGame] Stats reset — replaying match.");
        ChangeState("Battle");
    }

    /// <summary>
    /// Deactivates every character but keeps all player slots and join flags
    /// intact, then returns to CharacterSelect so players can pick new colours.
    /// </summary>
    void ChooseCharacters()
    {
        DeactivateAllCharacters();

        // Set every slot inactive so it matches the state Lobby leaves them in.
        // Battle's SpawnCharacters() will re-activate each slot at the next match
        // start. Without this, any character staged during CharacterSelect would
        // appear immediately in the 3D scene (as a child of an active slot)
        // before PreGame has a chance to initialise it properly.
        for (int i = 0; i < gameManager.playerSlot.Count; i++)
            gameManager.playerSlot[i].SetActive(false);

        // Clear confirmed-character flags so CharacterSelect treats everyone
        // as browsing again.
        for (int i = 0; i < gameManager.isCharacterSelect.Length; i++)
            gameManager.isCharacterSelect[i] = false;

        DestroyViewportBorder();
        ChangeState("CharacterSelect");
    }

    /// <summary>
    /// Fully tears down every player and slot, resets all flags, then returns
    /// to SplashScreen as if the application had just launched.
    /// </summary>
    void Leave()
    {
        DeactivateAllCharacters();
        DestroyAllSlots();
        DestroyViewportBorder();
        ChangeState("SplashScreen");
    }

    void Quit()
    {
        Debug.Log("[PostGame] Quitting application.");
        Application.Quit();
    }

    // ── Teardown helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Destroys the split-screen viewport border when leaving to a non-Battle state.
    /// Not called on Replay so the border is reused without flickering.
    /// </summary>
    void DestroyViewportBorder()
    {
        gameManager.viewportBorder?.DestroyBorder();
        gameManager.viewportBorder = null;
    }

    /// <summary>Calls DeactivatePlayerCharacter on every slot that has one.</summary>
    void DeactivateAllCharacters()
    {
        for (int i = 0; i < gameManager.playerSlot.Count; i++)
        {
            LocalPlayerManager lp =
                gameManager.playerSlot[i].GetComponent<LocalPlayerManager>();
            if (lp != null) lp.DeactivatePlayerCharacter();
        }
    }

    /// <summary>
    /// Destroys all player slot GameObjects and clears the list and join flags.
    /// Only called when leaving the game entirely (Leave option).
    /// </summary>
    void DestroyAllSlots()
    {
        for (int i = gameManager.playerSlot.Count - 1; i >= 0; i--)
            GameObject.Destroy(gameManager.playerSlot[i]);

        gameManager.playerSlot.Clear();

        for (int i = 0; i < gameManager.isJoinConfirmed.Length; i++)
        {
            gameManager.isJoinConfirmed[i]   = false;
            gameManager.isCharacterSelect[i] = false;
        }

        Debug.Log("[PostGame] All players torn down.");
    }

    // ── UI helper ─────────────────────────────────────────────────────────────

    void LogMenu()
    {
        string c(int row) => _selectedRow == row ? "►" : " ";
        Debug.Log(
            $"[PostGame]\n" +
            $"  {c(RowReplay)}   Replay\n" +
            $"  {c(RowNewChars)} Choose Characters\n" +
            $"  {c(RowLeave)}   Leave\n" +
            $"  {c(RowQuit)}    Quit");
    }

    static string RowLabel(int row) => row switch
    {
        RowReplay   => "Replay",
        RowNewChars => "Choose Characters",
        RowLeave    => "Leave",
        RowQuit     => "Quit",
        _           => "Unknown"
    };

    public void ChangeState(string state) => gameManager.ChangeState(state);
}
