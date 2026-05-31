/// <summary>
/// Active while the player is knocked down / prone.
/// Combat input is still routed through the Prone context so controllers can
/// selectively gate specific actions (e.g. only allow a get-up input) while
/// blocking attacks and movement.
/// </summary>
public class PlayerState_Prone : IPlayerState
{
    public void OnEnter(LocalPlayerManager player)
    {
        player.playerInput.Context = PlayerInputContext.Prone;
    }

    public void OnExit(LocalPlayerManager player)
    {
        // The incoming state is responsible for setting its own context.
    }
}
