/// <summary>
/// Contract for every per-player state.
/// Each state receives the owning <see cref="LocalPlayerManager"/> on enter and
/// exit so it can set context flags, subscribe/unsubscribe events, or adjust
/// any controller without needing its own field references.
/// </summary>
public interface IPlayerState
{
    /// <summary>Called when this state becomes the active state.</summary>
    void OnEnter(LocalPlayerManager player);

    /// <summary>Called just before this state is replaced by the next state.</summary>
    void OnExit(LocalPlayerManager player);
}
