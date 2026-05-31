using UnityEngine;
using Rewired;
using System;

/// <summary>
/// State 2 — CharacterSelect
/// All joined players browse colours (D-Pad) and lock in with A.
/// Pressing B while confirmed deconfirms. Pressing B while browsing unjoins
/// the player; if the last player leaves, the state returns to Lobby.
/// When every joined player has confirmed, advances to PreGame.
/// </summary>
[Serializable]
public class CharacterSelect : IGameState
{
    public GameManager gameManager { get; set; }

    CharacterSelectUI _ui;

    public CharacterSelect(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public void OnLoad()
    {
        if (gameManager.characterPrefabs.Count == 0) return;

        int total = gameManager.characterPrefabs.Count;

        // Nudge every joined player's starting index off any already-confirmed slot.
        for (int i = 0; i < ReInput.players.playerCount; i++)
        {
            if (!gameManager.isJoinConfirmed[i]) continue;
            for (int step = 0; step < total; step++)
            {
                if (!IsColorTaken(gameManager.characterIndex[i], i)) break;
                gameManager.characterIndex[i] = (gameManager.characterIndex[i] + 1) % total;
            }
        }

        // Spin up the UI overlay.
        _ui = new CharacterSelectUI();
        _ui.Initialize(ReInput.players.playerCount);
        _ui.Refresh(gameManager.isJoinConfirmed, gameManager.isCharacterSelect,
                    gameManager.characterIndex,  gameManager.characterPrefabs,
                    gameManager.characterThumbnails);
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

            // ── A (not yet joined) : Late join ───────────────────────────────
            // Allows a player to join directly in CharacterSelect if they
            // missed the Lobby phase.
            if (gamePad.GetButtonDown("A") && !gameManager.isJoinConfirmed[i])
            {
                GameObject playerObject = GameObject.Instantiate(gameManager.playerPrefab);
                playerObject.SetActive(false);
                gameManager.playerSlot.Add(playerObject);

                LocalPlayerManager localPlayer = playerObject.GetComponent<LocalPlayerManager>()
                    ?? playerObject.AddComponent<LocalPlayerManager>();

                localPlayer.InitializePlayer(gamePad);
                gameManager.isJoinConfirmed[i] = true;
                gameManager.SetPlayerNames();

                // Nudge starting index off any already-confirmed colour.
                if (gameManager.characterPrefabs.Count > 0)
                {
                    int total = gameManager.characterPrefabs.Count;
                    for (int step = 0; step < total; step++)
                    {
                        if (!IsColorTaken(gameManager.characterIndex[i], i)) break;
                        gameManager.characterIndex[i] = (gameManager.characterIndex[i] + 1) % total;
                    }
                }

                Debug.Log($"[CharacterSelect] Player {gamePad.id} late-joined.");
                continue; // Skip the rest of this player's input this frame.
            }

            if (!gameManager.isJoinConfirmed[i]) continue;

            // ── A : Confirm colour ───────────────────────────────────────────
            if (gamePad.GetButtonDown("A") && !gameManager.isCharacterSelect[i])
            {
                // Auto-resolve: if the hovered colour is already confirmed by
                // another player, silently advance to the nearest free slot so
                // the press always succeeds and the game can continue.
                if (IsColorTaken(gameManager.characterIndex[i], i))
                {
                    int total = gameManager.characterPrefabs.Count;
                    for (int step = 0; step < total; step++)
                    {
                        gameManager.characterIndex[i] = (gameManager.characterIndex[i] + 1) % total;
                        if (!IsColorTaken(gameManager.characterIndex[i], i)) break;
                    }
                    Debug.Log($"[CharacterSelect] Player {i} auto-resolved to colour index {gameManager.characterIndex[i]}.");
                }

                for (int j = 0; j < gameManager.playerSlot.Count; j++)
                {
                    LocalPlayerManager localPlayer = gameManager.playerSlot[j].GetComponent<LocalPlayerManager>();
                    if (localPlayer.playerGamePad.id != gamePad.id) continue;

                    GameObject character = GameObject.Instantiate(
                        gameManager.characterPrefabs[gameManager.characterIndex[i]],
                        Vector3.zero, Quaternion.identity,
                        gameManager.playerSlot[j].transform);

                    character.name = $"Player{j + 1} {character.name}";
                    character.tag  = "Player";

                    GameObject displayObject = GameObject.Instantiate(gameManager.displayPrefab);
                    Canvas     canvas        = GameObject.Instantiate(gameManager.canvasPrefab).GetComponent<Canvas>();
                    GameObject cursor        = GameObject.Instantiate(gameManager.CursorPrefab);

                    int layerIndex = LayerMask.NameToLayer($"P{j + 1}Visible");
                    cursor.layer   = (layerIndex == -1) ? cursor.layer : layerIndex;

                    localPlayer.StageCharacter(character, displayObject, canvas, cursor, $"P{j + 1}Visible");
                    gameManager.SetPlayerNames();
                    break;
                }

                gameManager.isCharacterSelect[i] = true;
                Debug.Log($"[CharacterSelect] Player {i} confirmed colour index {gameManager.characterIndex[i]}.");

                // Bump any other browsing player sitting on the now-locked index.
                BumpConflictingPlayers(i);

                CheckAllConfirmed();
            }

            // ── B : Deconfirm or unjoin ──────────────────────────────────────
            if (gamePad.GetButtonDown("B"))
            {
                if (gameManager.isCharacterSelect[i])
                {
                    // Tear down the character — player goes back to browsing.
                    for (int j = 0; j < gameManager.playerSlot.Count; j++)
                    {
                        LocalPlayerManager lp = gameManager.playerSlot[j].GetComponent<LocalPlayerManager>();
                        if (lp.playerGamePad.id != gamePad.id) continue;
                        lp.DeactivatePlayerCharacter();
                        break;
                    }
                    gameManager.isCharacterSelect[i] = false;
                    Debug.Log($"[CharacterSelect] Player {i} deconfirmed.");
                }
                else
                {
                    // Remove the player entirely.
                    for (int j = 0; j < gameManager.playerSlot.Count; j++)
                    {
                        LocalPlayerManager lp = gameManager.playerSlot[j].GetComponent<LocalPlayerManager>();
                        if (lp.playerGamePad.id != gamePad.id) continue;
                        lp.DeactivatePlayer(lp.playerGamePad);
                        GameObject.Destroy(gameManager.playerSlot[j]);
                        gameManager.playerSlot.RemoveAt(j);
                        break;
                    }
                    gameManager.isJoinConfirmed[i] = false;
                    gameManager.SetPlayerNames();
                    Debug.Log($"[CharacterSelect] Player {i} left.");

                    // If the lobby is now empty return to Lobby.
                    if (gameManager.playerSlot.Count == 0)
                    {
                        ChangeState("Lobby");
                        return;
                    }
                }
            }

            // ── D-Pad Left : Browse left ─────────────────────────────────────
            if (gamePad.GetButtonDown("D-Pad Left") && !gameManager.isCharacterSelect[i])
            {
                int total = gameManager.characterPrefabs.Count;
                int steps = 0;
                do
                {
                    gameManager.characterIndex[i]--;
                    if (gameManager.characterIndex[i] < 0) gameManager.characterIndex[i] = total - 1;
                    steps++;
                }
                while (IsColorTaken(gameManager.characterIndex[i], i) && steps < total);
                Debug.Log($"[CharacterSelect] Player {i} browsing: {gameManager.characterPrefabs[gameManager.characterIndex[i]].name}");
            }

            // ── D-Pad Right : Browse right ───────────────────────────────────
            if (gamePad.GetButtonDown("D-Pad Right") && !gameManager.isCharacterSelect[i])
            {
                int total = gameManager.characterPrefabs.Count;
                int steps = 0;
                do
                {
                    gameManager.characterIndex[i]++;
                    if (gameManager.characterIndex[i] >= total) gameManager.characterIndex[i] = 0;
                    steps++;
                }
                while (IsColorTaken(gameManager.characterIndex[i], i) && steps < total);
                Debug.Log($"[CharacterSelect] Player {i} browsing: {gameManager.characterPrefabs[gameManager.characterIndex[i]].name}");
            }
        }

        // Refresh the overlay every frame so swatches and statuses stay current.
        _ui?.Refresh(gameManager.isJoinConfirmed, gameManager.isCharacterSelect,
                     gameManager.characterIndex,  gameManager.characterPrefabs,
                     gameManager.characterThumbnails);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Advance to PreGame if every joined player has confirmed a colour.</summary>
    void CheckAllConfirmed()
    {
        bool anyJoined = false;
        for (int j = 0; j < gameManager.isJoinConfirmed.Length; j++)
        {
            if (!gameManager.isJoinConfirmed[j]) continue;
            anyJoined = true;
            if (!gameManager.isCharacterSelect[j]) return; // Someone still browsing.
        }
        if (anyJoined) ChangeState("PreGame");
    }

    /// <summary>
    /// When player <paramref name="confirmedId"/> locks in, move every other
    /// browsing player that shares the same index forward to the next free slot.
    /// </summary>
    void BumpConflictingPlayers(int confirmedId)
    {
        int total = gameManager.characterPrefabs.Count;
        for (int j = 0; j < ReInput.players.playerCount; j++)
        {
            if (j == confirmedId) continue;
            if (!gameManager.isJoinConfirmed[j] || gameManager.isCharacterSelect[j]) continue;
            if (gameManager.characterIndex[j] != gameManager.characterIndex[confirmedId]) continue;

            for (int step = 0; step < total; step++)
            {
                gameManager.characterIndex[j] = (gameManager.characterIndex[j] + 1) % total;
                if (!IsColorTaken(gameManager.characterIndex[j], j)) break;
            }
        }
    }

    /// <summary>
    /// Returns true if a joined-and-confirmed player other than
    /// <paramref name="ownPlayerId"/> has already locked <paramref name="index"/>.
    /// </summary>
    bool IsColorTaken(int index, int ownPlayerId)
    {
        for (int j = 0; j < ReInput.players.playerCount; j++)
        {
            if (j != ownPlayerId && gameManager.isCharacterSelect[j]
                                 && gameManager.characterIndex[j] == index)
                return true;
        }
        return false;
    }

    public void ChangeState(string state) => gameManager.ChangeState(state);
}
