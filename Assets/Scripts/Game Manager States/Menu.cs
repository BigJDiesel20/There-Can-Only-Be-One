using UnityEngine;
using Rewired;
using System;

/// <summary>
/// State 1 — Menu
/// Main menu driven by Player 0's controller.
///
/// Top-level layout
///   ──────────────────────────────
///   ► Online                  (A → not yet implemented)
///     Offline                 (A → enters Offline submenu)
///     Quit                    (A → Application.Quit)
///   ──────────────────────────────
///
/// Offline submenu (entered when Offline is confirmed)
///   ──────────────────────────────
///   Game Mode : Classic       (◄/► to cycle)
///   Start                     (A → Lobby)
///   Back                      (A or B → return to top-level menu)
///   ──────────────────────────────
///
/// D-Pad Up / Down — move cursor between rows.
/// D-Pad Left / Right — cycle Game Mode (submenu only).
/// A — confirm.  B — back (submenu only).
///
/// Future home of: animated background, music, Settings row.
/// </summary>
[Serializable]
public class Menu : IGameState
{
    public GameManager gameManager { get; set; }

    // ── Top-level row indices ─────────────────────────────────────────────────
    const int RowOnline  = 0;
    const int RowOffline = 1;
    const int RowQuit    = 2;
    const int RowCount   = 3;

    // ── Offline submenu row indices ───────────────────────────────────────────
    const int SubRowGameMode = 0;
    const int SubRowStart    = 1;
    const int SubRowBack     = 2;
    const int SubRowCount    = 3;

    // ── Game mode options (extend this array when new modes are added) ────────
    static readonly GameManager.GameMode[] GameModes =
    {
        GameManager.GameMode.Classic,
        // GameManager.GameMode.Elimination,  // example future entry
    };

    int     _selectedRow;
    int     _selectedSubRow;
    int     _gameModeIndex;
    bool    _inSubmenu;
    MenuUI  _ui;

    public Menu(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public void OnLoad()
    {
        _selectedRow    = 0;
        _selectedSubRow = 0;
        _inSubmenu      = false;
        _gameModeIndex  = Array.IndexOf(GameModes, gameManager.currentGameMode);
        if (_gameModeIndex < 0) _gameModeIndex = 0;

        Debug.Log("[Menu] Loaded.");
        LogMenu();

        _ui = new MenuUI();
        _ui.Initialize();
        _ui.RefreshTopLevel(_selectedRow);
    }

    public void OnExit()
    {
        _ui?.Destroy();
        _ui = null;
    }

    public void OnUpdate()
    {
        Player pad = ReInput.players.GetPlayer(0);

        if (_inSubmenu)
            UpdateSubmenu(pad);
        else
            UpdateTopLevel(pad);
    }

    // ── Top-level navigation ──────────────────────────────────────────────────

    void UpdateTopLevel(Player pad)
    {
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
            _ui?.RefreshTopLevel(_selectedRow);

        if (pad.GetButtonDown("A"))
            ConfirmTopLevel();
    }

    void ConfirmTopLevel()
    {
        switch (_selectedRow)
        {
            case RowOnline:
                Debug.Log("[Menu] Online mode is not yet available.");
                break;

            case RowOffline:
                Debug.Log("[Menu] Entering offline submenu.");
                _inSubmenu      = true;
                _selectedSubRow = 0;
                LogSubmenu();
                _ui?.ShowSubmenu(
                    gameManager.currentGameMode.ToString(),
                    GameModes.Length > 1,
                    _selectedSubRow);
                break;

            case RowQuit:
                Debug.Log("[Menu] Quitting application.");
                Application.Quit();
                break;
        }
    }

    // ── Offline submenu navigation ────────────────────────────────────────────

    void UpdateSubmenu(Player pad)
    {
        bool dirty = false;

        if (pad.GetButtonDown("D-Pad Up"))
        {
            _selectedSubRow = (_selectedSubRow - 1 + SubRowCount) % SubRowCount;
            LogSubmenu();
            dirty = true;
        }
        if (pad.GetButtonDown("D-Pad Down"))
        {
            _selectedSubRow = (_selectedSubRow + 1) % SubRowCount;
            LogSubmenu();
            dirty = true;
        }

        // ◄► cycles game mode when cursor is on the Game Mode row
        if (_selectedSubRow == SubRowGameMode)
        {
            if (pad.GetButtonDown("D-Pad Left"))
            {
                _gameModeIndex = (_gameModeIndex - 1 + GameModes.Length) % GameModes.Length;
                gameManager.currentGameMode = GameModes[_gameModeIndex];
                LogSubmenu();
                dirty = true;
            }
            if (pad.GetButtonDown("D-Pad Right"))
            {
                _gameModeIndex = (_gameModeIndex + 1) % GameModes.Length;
                gameManager.currentGameMode = GameModes[_gameModeIndex];
                LogSubmenu();
                dirty = true;
            }
        }

        if (dirty)
            _ui?.RefreshSubmenu(
                gameManager.currentGameMode.ToString(),
                GameModes.Length > 1,
                _selectedSubRow);

        if (pad.GetButtonDown("A"))
            ConfirmSubmenu();

        // B always backs out to the top-level menu
        if (pad.GetButtonDown("B"))
            ExitSubmenu();
    }

    void ConfirmSubmenu()
    {
        switch (_selectedSubRow)
        {
            case SubRowGameMode:
                // ◄► already set the mode; A on this row re-confirms (no-op, stays in submenu)
                Debug.Log($"[Menu] Game Mode set to {gameManager.currentGameMode}.");
                break;

            case SubRowStart:
                Debug.Log($"[Menu] Starting offline game — mode: {gameManager.currentGameMode}.");
                ChangeState("Lobby");
                break;

            case SubRowBack:
                ExitSubmenu();
                break;
        }
    }

    void ExitSubmenu()
    {
        Debug.Log("[Menu] Returning to main menu.");
        _inSubmenu = false;
        LogMenu();
        _ui?.HideSubmenu(_selectedRow);
    }

    // ── Debug helpers ─────────────────────────────────────────────────────────

    void LogMenu()
    {
        string c(int row) => _selectedRow == row ? "►" : " ";
        Debug.Log(
            $"[Menu]\n" +
            $"  {c(RowOnline)}  Online\n" +
            $"  {c(RowOffline)}  Offline\n" +
            $"  {c(RowQuit)}  Quit");
    }

    void LogSubmenu()
    {
        string c(int row) => _selectedSubRow == row ? "►" : " ";
        Debug.Log(
            $"[Menu > Offline]\n" +
            $"  {c(SubRowGameMode)}  Game Mode : {gameManager.currentGameMode}" +
                (GameModes.Length > 1 ? " ◄►" : "") + "\n" +
            $"  {c(SubRowStart)}  Start\n" +
            $"  {c(SubRowBack)}  Back");
    }

    public void ChangeState(string state) => gameManager.ChangeState(state);
}
