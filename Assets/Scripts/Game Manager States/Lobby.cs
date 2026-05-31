using UnityEngine;
using Rewired;
using System;
using System.Collections.Generic;

/// <summary>
/// State 1 — Lobby
///
/// A (not joined)        → join
/// A (joined, not ready) → ready up
/// B (ready)             → un-ready
/// B (joined, not ready) → unjoin
///
/// When every joined player is readied up, a 3-second countdown starts.
/// Any new join or un-ready resets the countdown.
/// Countdown reaching zero advances to CharacterSelect.
/// </summary>
[Serializable]
public class Lobby : IGameState
{
    public GameManager gameManager { get; set; }

    LobbyUI _ui;

    /// <summary>Which lobby slots have pressed A twice and are ready to go.</summary>
    bool[] _lobbyConfirmed;

    float _countdown;
    const float CountdownDuration = 3f;

    public Lobby(GameManager gameManager)
    {
        this.gameManager = gameManager;

        // Allocate shared arrays sized to the maximum controller count.
        // Values are initialised in OnLoad so re-entering Lobby resets them.
        int count = ReInput.players.playerCount;
        gameManager.isJoinConfirmed   = new bool[count];
        gameManager.isCharacterSelect = new bool[count];
        gameManager.characterIndex    = new int[count];
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public void OnLoad()
    {
        int count = ReInput.players.playerCount;

        for (int i = 0; i < gameManager.isJoinConfirmed.Length; i++)
        {
            gameManager.isJoinConfirmed[i]   = false;
            gameManager.isCharacterSelect[i] = false;
        }

        _lobbyConfirmed = new bool[count];
        _countdown      = CountdownDuration;

        // Randomise each slot's starting colour index.
        int prefabCount = Mathf.Max(1, gameManager.characterPrefabs.Count);
        for (int i = 0; i < gameManager.characterIndex.Length; i++)
            gameManager.characterIndex[i] = UnityEngine.Random.Range(0, prefabCount);

        _ui = new LobbyUI();
        _ui.Initialize(count);
        _ui.Refresh(gameManager.isJoinConfirmed, _lobbyConfirmed, _countdown, false);
    }

    public void OnExit()
    {
        _ui?.Destroy();
        _ui = null;
    }

    public void OnUpdate()
    {
        for (int i = 0; i < ReInput.players.playerCount; i++)
        {
            Player gamePad = ReInput.players.GetPlayer(i);

            // ── A (not joined) : Join ────────────────────────────────────────
            if (gamePad.GetButtonDown("A") && !gameManager.isJoinConfirmed[i])
            {
                GameObject playerObject = GameObject.Instantiate(gameManager.playerPrefab);
                playerObject.SetActive(false);
                gameManager.playerSlot.Add(playerObject);

                LocalPlayerManager localPlayer = playerObject.GetComponent<LocalPlayerManager>()
                    ?? playerObject.AddComponent<LocalPlayerManager>();

                localPlayer.InitializePlayer(gamePad);
                gameManager.isJoinConfirmed[i] = true;
                _lobbyConfirmed[i]             = false;
                gameManager.SetPlayerNames();
                Debug.Log($"[Lobby] Player {gamePad.id} joined.");
                continue; // skip further input this frame
            }

            if (!gameManager.isJoinConfirmed[i]) continue;

            // ── B : Un-ready or unjoin ───────────────────────────────────────
            if (gamePad.GetButtonDown("B"))
            {
                if (_lobbyConfirmed[i])
                {
                    // Step back to "joined but not ready".
                    _lobbyConfirmed[i] = false;
                    Debug.Log($"[Lobby] Player {i} un-readied.");
                }
                else
                {
                    // Leave the lobby entirely.
                    for (int j = 0; j < gameManager.playerSlot.Count; j++)
                    {
                        LocalPlayerManager lp = gameManager.playerSlot[j].GetComponent<LocalPlayerManager>();
                        if (lp.playerGamePad.id == gamePad.id)
                        {
                            lp.DeactivatePlayer(lp.playerGamePad);
                            GameObject.Destroy(gameManager.playerSlot[j]);
                            gameManager.playerSlot.RemoveAt(j);
                            break;
                        }
                    }
                    gameManager.isJoinConfirmed[i] = false;
                    _lobbyConfirmed[i]             = false;
                    gameManager.SetPlayerNames();
                    Debug.Log($"[Lobby] Player {gamePad.id} left.");
                }
            }

            // ── A (joined, not ready) : Ready up ────────────────────────────
            if (gamePad.GetButtonDown("A") && !_lobbyConfirmed[i])
            {
                _lobbyConfirmed[i] = true;
                Debug.Log($"[Lobby] Player {i} readied up.");
            }
        }

        // ── Countdown ─────────────────────────────────────────────────────────
        bool allReady = CheckAllReady();

        if (allReady)
        {
            _countdown -= Time.deltaTime;
            if (_countdown <= 0f)
            {
                ChangeState("CharacterSelect");
                return;
            }
        }
        else
        {
            _countdown = CountdownDuration; // reset whenever conditions aren't met
        }

        _ui?.Refresh(gameManager.isJoinConfirmed, _lobbyConfirmed, _countdown, allReady);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// True only when at least one player is joined and every joined player
    /// has pressed A twice (readied up).
    /// </summary>
    bool CheckAllReady()
    {
        bool anyJoined = false;
        for (int i = 0; i < gameManager.isJoinConfirmed.Length; i++)
        {
            if (!gameManager.isJoinConfirmed[i]) continue;
            anyJoined = true;
            if (!_lobbyConfirmed[i]) return false;
        }
        return anyJoined;
    }

    public void ChangeState(string state) => gameManager.ChangeState(state);
}
