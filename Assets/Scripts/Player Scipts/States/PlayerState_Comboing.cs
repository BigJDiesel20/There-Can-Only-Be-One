/// <summary>
/// Active while the player is executing an attack or mid-combo chain.
///
/// Input behaviour in this context:
///   • Attack buttons (X / Y / B / A) remain live so combo inputs can be
///     buffered into the ring buffer for the next fixed tick.
///   • All analogue axes (left stick movement, right stick rotation) return
///     zero — the player is committed to the combo and cannot steer.
///
/// Transitions (managed by PlayerStateMachine):
///   Battle / Prone  ──► Comboing   (PlayerEvents.OnAttackStart)
///   Comboing        ──► Battle     (PlayerEvents.OnAttackEnd via ReturnToPreviousState)
///   Comboing        ──► Prone      (PlayerEvents.OnProneActive  — interrupts combo)
///   Comboing        ──► Dialog     (PlayerEvents.OnDialogStateChanged)
/// </summary>
public class PlayerState_Comboing : IPlayerState
{
    public void OnEnter(LocalPlayerManager player)
    {
        player.playerInput.Context = PlayerInputContext.Comboing;
    }

    public void OnExit(LocalPlayerManager player) { }
}
