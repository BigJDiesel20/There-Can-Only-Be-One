using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Rewired;
using System;

/// <summary>
/// State 4 — Battle
/// The active game state. Spawns characters at random non-overlapping
/// positions on load, then runs game logic every frame.
///
/// Win condition (Classic mode): the first player whose aura reaches the
/// combined maximum wins the match. Battle subscribes to each player's
/// <c>OnValueMaximum</c> aura event and transitions to PostGame on trigger.
/// </summary>
[Serializable]
public class Battle : IGameState
{
    public GameManager gameManager { get; set; }

    // ── Win-condition state ───────────────────────────────────────────────────
    bool _gameOver;

    // Stores (StatEvents, handler) pairs so we can cleanly unsubscribe on exit.
    readonly List<(StatEvents events, UnityAction handler)> _auraHandlers
        = new List<(StatEvents, UnityAction)>();

    // ── Debug UI ──────────────────────────────────────────────────────────────
    BattleDebugUI _debugUI;

    public Battle(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public void OnLoad()
    {
        _gameOver = false;

        SpawnCharacters();
        SubscribeWinCondition();

        // Transition every active player into the Battle state.
        // PlayerStateMachine.EnterBattle sets the input context to Battle so all
        // combat controllers start receiving real input values.
        foreach (LocalPlayerManager player in LocalPlayerManager.ActivePlayers)
            player.stateMachine.EnterBattle();

        _debugUI = BattleDebugUI.Create(DebugForceWinForPlayer);

        // Create the border once on the first load (2+ players). On Replay it
        // already exists on GameManager and is reused without recreation.
        if (gameManager.viewportBorder == null && LocalPlayerManager.ActivePlayers.Count >= 2)
            gameManager.viewportBorder = CameraViewportBorder.Create(color: Color.black);
    }

    public void OnExit()
    {
        _debugUI?.DestroyUI();
        _debugUI = null;

        // Transition every player back to a null (Disabled) state.
        // PlayerStateMachine.ChangeState(null) sets Context = Disabled so all
        // input queries return 0 / false immediately without any Rigidbody changes.
        foreach (LocalPlayerManager player in LocalPlayerManager.ActivePlayers)
        {
            player.stateMachine.ChangeState(null);

            // Zero residual Rigidbody velocity so the character doesn't coast on
            // the zero-friction physics material into the next game state.
            if (player.movementController?.rb != null)
                player.movementController.rb.linearVelocity = Vector3.zero;
        }

        UnsubscribeWinCondition();
    }

    public void OnUpdate()
    {
        // Win condition is event-driven (see OnAuraMaxReached).
    }

    /// <summary>
    /// Forces player <paramref name="winnerNumber"/> (1-based) to win.
    /// Every other active player is knocked out and their aura zeroed;
    /// the winner's aura is set to maximum, firing the normal win-condition chain.
    /// </summary>
    void DebugForceWinForPlayer(int winnerNumber)
    {
        if (_gameOver) return;

        var players = LocalPlayerManager.ActivePlayers;
        if (winnerNumber < 1 || winnerNumber > players.Count)
        {
            Debug.LogWarning($"[Battle] DEBUG: player {winnerNumber} out of range " +
                             $"(1–{players.Count} active).");
            return;
        }

        LocalPlayerManager winner = players[winnerNumber - 1];

        // Knock out every other player first so the HUD and prone state update
        // before the win event fires and the state transitions to PostGame.
        foreach (LocalPlayerManager player in players)
        {
            if (player != winner)
            {
                player.statManager.DebugForceKnockOut();
                Debug.Log($"[Battle] DEBUG: knocked out {player.playerName}.");
            }
        }

        // Fill the winner's aura to trigger OnValueMaximum → OnAuraMaxReached.
        Debug.Log($"[Battle] DEBUG: forcing win for {winner.playerName}.");
        winner.statManager.DebugForceWin();
    }

    // ── Win condition ─────────────────────────────────────────────────────────

    /// <summary>
    /// Subscribes to every active player's aura OnValueMaximum event so the
    /// win condition fires the instant any player's aura hits the combined max.
    /// A closure captures the specific player reference for each subscription.
    /// </summary>
    void SubscribeWinCondition()
    {
        _auraHandlers.Clear();

        for (int i = 0; i < gameManager.playerSlot.Count; i++)
        {
            LocalPlayerManager player =
                gameManager.playerSlot[i].GetComponent<LocalPlayerManager>();

            StatEvents aura =
                player.playerEvents.statEventsCoclection[StatEvents.Type.Aura];

            // Capture player reference in closure so the handler knows who won.
            LocalPlayerManager captured = player;
            UnityAction handler = () => OnAuraMaxReached(captured);

            aura.OnValueMaximum += handler;
            _auraHandlers.Add((aura, handler));
        }
    }

    void UnsubscribeWinCondition()
    {
        foreach (var (events, handler) in _auraHandlers)
            events.OnValueMaximum -= handler;

        _auraHandlers.Clear();
    }

    void OnAuraMaxReached(LocalPlayerManager winner)
    {
        // Guard: only the first player to hit max aura wins.
        if (_gameOver) return;
        _gameOver = true;

        gameManager.lastWinnerName = winner.playerName;
        Debug.Log($"[Battle] {winner.playerName} reached max aura — WINNER! Transitioning to PostGame.");
        ChangeState("PostGame");
    }

    // ── Spawn ─────────────────────────────────────────────────────────────────

    // Minimum world-unit gap between any two players' spawn points.
    const float MinSpacing  = 3f;
    // XZ half-range of the random spawn area (centred on origin).
    const float SpawnRange  = 20f;
    // Max random attempts per player before falling back to circle placement.
    const int   MaxAttempts = 500;

    /// <summary>
    /// Places each player at a random world position that:
    ///   • does not overlap the Ground layer, and
    ///   • is at least MinSpacing units from every previously placed player.
    /// Falls back to an evenly-spaced circle if no valid position is found
    /// within MaxAttempts tries.
    /// </summary>
    void SpawnCharacters()
    {
        int count = gameManager.playerSlot.Count;
        var placed = new List<Vector3>(count);

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = Vector3.zero;
            bool    found    = false;

            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                Vector3 candidate = new Vector3(
                    UnityEngine.Random.Range(-SpawnRange, SpawnRange),
                    0f,
                    UnityEngine.Random.Range(-SpawnRange, SpawnRange));

                if (Physics.OverlapBox(candidate, Vector3.one, Quaternion.identity,
                                       LayerMask.GetMask("Ground")).Length > 0)
                    continue;

                bool tooClose = false;
                foreach (Vector3 p in placed)
                {
                    if (Vector3.Distance(candidate, p) < MinSpacing)
                    { tooClose = true; break; }
                }
                if (tooClose) continue;

                spawnPos = candidate;
                found    = true;
                break;
            }

            if (!found)
            {
                float angle = i * (360f / count) * Mathf.Deg2Rad;
                spawnPos = new Vector3(
                    Mathf.Cos(angle) * MinSpacing * 2f,
                    0f,
                    Mathf.Sin(angle) * MinSpacing * 2f);
                Debug.LogWarning($"[Battle] Player {i + 1} used circle fallback spawn at {spawnPos}.");
            }

            placed.Add(spawnPos);

            LocalPlayerManager player =
                gameManager.playerSlot[i].GetComponent<LocalPlayerManager>();
            player.character.transform.position = spawnPos;
            gameManager.playerSlot[i].gameObject.SetActive(true);
            Debug.Log($"[Battle] Player {i + 1} spawned at {spawnPos}.");
        }
    }

    public void ChangeState(string state) => gameManager.ChangeState(state);
}
