/// <summary>
/// Active when the player has been eliminated and is watching the remaining
/// battle as a spectator.
/// All combat input is blocked. Camera navigation and spectator-specific
/// features can be added here when the spectator system is implemented.
/// </summary>
public class PlayerState_Spectate : IPlayerState
{
    public void OnEnter(LocalPlayerManager player)
    {
        player.playerInput.Context = PlayerInputContext.Spectate;
    }

    public void OnExit(LocalPlayerManager player)
    {
        // The incoming state is responsible for setting its own context.
    }
}
