/// <summary>
/// Active during normal gameplay.
/// Enables the full combat input layer so movement, attacks, and camera all
/// respond to player input.
/// </summary>
public class PlayerState_Battle : IPlayerState
{
    public void OnEnter(LocalPlayerManager player)
    {
        player.playerInput.Context = PlayerInputContext.Battle;
    }

    public void OnExit(LocalPlayerManager player)
    {
        // The incoming state is responsible for setting its own context.
    }
}
