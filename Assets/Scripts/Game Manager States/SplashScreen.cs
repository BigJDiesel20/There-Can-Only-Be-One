using UnityEngine;
using Rewired;
using System;

/// <summary>
/// State 0 — SplashScreen
/// Shown once when the game launches. Displays a logo / title card and then
/// advances to the main Menu either automatically after <see cref="DisplayDuration"/>
/// seconds or immediately when any player presses any button.
///
/// Future home of: animated logo, studio ident, legal text.
/// </summary>
[Serializable]
public class SplashScreen : IGameState
{
    public GameManager gameManager { get; set; }

    // How long the splash is shown before auto-advancing (seconds).
    const float DisplayDuration = 5f;

    float           _timer;
    SplashScreenUI  _ui;

    public SplashScreen(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public void OnLoad()
    {
        _timer = DisplayDuration;
        Debug.Log("[SplashScreen] Loaded — press any button or wait to continue.");

        _ui = new SplashScreenUI();
        _ui.Initialize();
        _ui.Refresh(1f, 0f);   // start with bar full, no pulse offset
    }

    public void OnExit()
    {
        _ui?.Destroy();
        _ui = null;
    }

    public void OnUpdate()
    {
        _timer -= Time.deltaTime;

        // Refresh UI every frame (timer bar + pulse animation)
        _ui?.Refresh(_timer / DisplayDuration, Time.deltaTime);

        // Any player pressing any button skips the splash immediately.
        for (int i = 0; i < ReInput.players.playerCount; i++)
        {
            Player pad = ReInput.players.GetPlayer(i);
            if (pad.GetAnyButtonDown())
            {
                ChangeState("Menu");
                return;
            }
        }

        if (_timer <= 0f)
        {
            ChangeState("Menu");
        }
    }

    public void ChangeState(string state) => gameManager.ChangeState(state);
}
