/// <summary>
/// Per-player state machine that lives on <see cref="LocalPlayerManager"/>.
///
/// Responsibilities:
///   • Owns pre-allocated instances of every player state so there is zero
///     allocation during state transitions at runtime.
///   • Drives transitions in response to game events (OnProneActive,
///     OnDialogStateChanged) by subscribing to <see cref="PlayerEvents"/>.
///   • Tracks the previous state so Dialog and other interruptive states can
///     return the player to exactly where they were before.
///   • Exposes convenience entry points (EnterBattle, EnterProne, etc.) for
///     external callers such as <see cref="Battle"/> and <see cref="PostGame"/>.
///
/// Passing null to ChangeState disables input (equivalent to Disabled context)
/// without allocating a dedicated Disabled state object.
///
/// Same-frame input bleed prevention:
///   When a dialog closes, the context is restored to Battle/Prone immediately
///   so _previousState is never corrupted. However, the button that dismissed
///   the dialog must not fire a combat action on that same frame. This is solved
///   by calling PlayerInput.SuppressCombatThisFrame() at the moment of closure.
///   PlayerStateMachine.OnUpdate clears the suppression flag at the very start
///   of the next frame (before any combat controller polls input), so exactly
///   one frame is suppressed and normal play resumes the frame after.
/// </summary>
public class PlayerStateMachine
{
    // ── Pre-allocated state instances ─────────────────────────────────────────

    public readonly PlayerState_Battle   Battle   = new PlayerState_Battle();
    public readonly PlayerState_Prone    Prone    = new PlayerState_Prone();
    public readonly PlayerState_Comboing Comboing = new PlayerState_Comboing();
    public readonly PlayerState_Dialog   Dialog   = new PlayerState_Dialog();
    public readonly PlayerState_Spectate Spectate = new PlayerState_Spectate();

    // ── Internal state ────────────────────────────────────────────────────────

    private IPlayerState       _currentState;
    private IPlayerState       _previousState;
    private LocalPlayerManager _player;

    /// <summary>The state currently running on this player.</summary>
    public IPlayerState CurrentState => _currentState;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Wire up event subscriptions. Must be called after <see cref="PlayerEvents"/>
    /// is ready on the player (i.e. after InitializePlayer).
    /// StateMachine subscribes to OnUpdate first so the suppression flag is
    /// cleared before any combat controller polls input each frame.
    /// </summary>
    public void Initialize(LocalPlayerManager player)
    {
        _player = player;
        player.playerEvents.OnUpdate             += OnUpdate;
        player.playerEvents.OnAttackStart        += OnAttackStart;
        player.playerEvents.OnAttackEnd          += OnAttackEnd;
        player.playerEvents.OnProneActive        += OnProneActive;
        player.playerEvents.OnDialogStateChanged += OnDialogStateChanged;
    }

    /// <summary>
    /// Unsubscribes all event listeners and clears references.
    /// Call from DeactivatePlayerCharacter before the player is destroyed.
    /// </summary>
    public void Deactivate()
    {
        if (_player == null) return;

        _currentState?.OnExit(_player);
        _currentState  = null;
        _previousState = null;

        _player.playerEvents.OnUpdate             -= OnUpdate;
        _player.playerEvents.OnAttackStart        -= OnAttackStart;
        _player.playerEvents.OnAttackEnd          -= OnAttackEnd;
        _player.playerEvents.OnProneActive        -= OnProneActive;
        _player.playerEvents.OnDialogStateChanged -= OnDialogStateChanged;
        _player = null;
    }

    // ── Per-frame tick ────────────────────────────────────────────────────────

    /// <summary>
    /// Clears the one-frame combat suppression that was set when a dialog closed.
    /// Subscribed before all other controllers so it fires first, ensuring
    /// combat is unblocked before movement and attack controllers read input.
    /// </summary>
    private void OnUpdate()
    {
        _player.playerInput.ClearFrameSuppression();
    }

    // ── State transitions ─────────────────────────────────────────────────────

    /// <summary>
    /// Transitions to <paramref name="newState"/>.
    /// Pass <c>null</c> to disable all input without entering a concrete state.
    /// </summary>
    public void ChangeState(IPlayerState newState)
    {
        _previousState = _currentState;
        _currentState?.OnExit(_player);
        _currentState = newState;

        if (_currentState != null)
            _currentState.OnEnter(_player);
        else
            _player.playerInput.Context = PlayerInputContext.Disabled;
    }

    /// <summary>
    /// Returns to the state that was active before the current one.
    /// Useful for interruptive states (Dialog) that need to restore the
    /// player's prior context when they close.
    /// </summary>
    public void ReturnToPreviousState() => ChangeState(_previousState);

    // ── Convenience entry points ──────────────────────────────────────────────

    public void EnterBattle()    => ChangeState(Battle);
    public void EnterProne()     => ChangeState(Prone);
    public void EnterComboing()  => ChangeState(Comboing);
    public void EnterDialog()    => ChangeState(Dialog);
    public void EnterSpectate()  => ChangeState(Spectate);

    // ── Event-driven transitions ──────────────────────────────────────────────

    /// <summary>
    /// Fired by <see cref="AttackController"/> when the first attack of a new
    /// chain is committed (comboCounter 0 → 1).
    /// Only enters Comboing from Battle or Prone — ignores Dialog/Spectate/Disabled.
    /// </summary>
    private void OnAttackStart()
    {
        if (_currentState == Battle || _currentState == Prone)
            ChangeState(Comboing);
    }

    /// <summary>
    /// Fired by <see cref="AttackController"/> once the full cooldown block
    /// finishes. Restores the state that was active before the combo started
    /// (Battle normally, Prone if knocked mid-combo).
    /// </summary>
    private void OnAttackEnd()
    {
        if (_currentState == Comboing)
            ReturnToPreviousState();
    }

    /// <summary>
    /// Fired by prone/knockdown systems when the player goes down or recovers.
    /// </summary>
    private void OnProneActive(bool isProne)
    {
        if (isProne) ChangeState(Prone);
        else         ReturnToPreviousState();
    }

    /// <summary>
    /// Fired by <see cref="UserInterfaceController"/> when a dialog opens or closes.
    /// Both transitions are immediate. On close, SuppressCombatThisFrame() ensures
    /// the button that dismissed the dialog cannot fire a combat action on the
    /// same frame — it is cleared automatically at the start of the next frame.
    /// </summary>
    private void OnDialogStateChanged(bool isOpen)
    {
        if (isOpen)
        {
            ChangeState(Dialog);
        }
        else
        {
            ReturnToPreviousState();
            _player.playerInput.SuppressCombatThisFrame();  // prevent dismiss-button bleed
        }
    }
}
