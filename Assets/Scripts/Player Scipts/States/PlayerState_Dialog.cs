/// <summary>
/// Active while an in-world dialog / message box is open.
/// Combat input (movement, attacks) is blocked so the player cannot act while
/// reading a prompt. UI button input is exclusively available via
/// <see cref="PlayerInput.GetUIButtonDown"/> so the same face buttons that
/// control combat can confirm, reject, or choose dialog options without
/// cross-firing into combat systems.
/// </summary>
public class PlayerState_Dialog : IPlayerState
{
    public void OnEnter(LocalPlayerManager player)
    {
        player.playerInput.Context = PlayerInputContext.Dialog;
    }

    public void OnExit(LocalPlayerManager player)
    {
        // The incoming state is responsible for setting its own context.
    }
}
