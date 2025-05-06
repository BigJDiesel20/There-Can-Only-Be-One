using UnityEngine;
using Rewired;
public class CharacterUI : IGameState
{
    public CharacterUI(GameManager gameManager)
    {
        this.gameManager = gameManager;
        GamePad = gameManager.playerGamePad;
    }

    public GameManager gameManager { get; set; }
    Rewired.Player GamePad;

    public void OnLoad() { }

    public void OnUpdate() { }
    public void OnOnOnGUI() { }
    public void ChangeState(string state) { }
}
