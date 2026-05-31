using Rewired;

/// <summary>
/// Thin wrapper around a Rewired Player that gates all input through a
/// <see cref="PlayerInputContext"/> value set by the player state machine.
///
/// Input routing by context:
///   Disabled  — every query returns 0 / false
///   Battle    — full combat input live; UI input blocked
///   Prone     — combat input live (controllers choose which actions to honour);
///               UI input blocked
///   Dialog    — combat input blocked; only <see cref="GetUIButtonDown"/> passes
///   Spectate  — all input blocked (extend here when spectator camera is added)
///
/// <see cref="IsEnabled"/> is kept for backward compatibility and maps to/from
/// Context automatically — existing callers that set <c>IsEnabled = true/false</c>
/// will transition to Battle or Disabled context respectively.
/// </summary>
public class PlayerInput
{
    readonly Rewired.Player _player;

    // ── Context ───────────────────────────────────────────────────────────────

    /// <summary>
    /// The current input context for this player.
    /// Set by the player state machine through <see cref="PlayerStateMachine"/>.
    /// </summary>
    public PlayerInputContext Context { get; set; } = PlayerInputContext.Disabled;

    /// <summary>
    /// Convenience wrapper: true when Context is anything other than Disabled.
    /// Setting true transitions to Battle; setting false transitions to Disabled.
    /// Preserved for backward compatibility with callers that predate the state machine.
    /// </summary>
    public bool IsEnabled
    {
        get => Context != PlayerInputContext.Disabled;
        set => Context = value ? PlayerInputContext.Battle : PlayerInputContext.Disabled;
    }

    /// <summary>The Rewired player ID (used by PlayerSymbolLibrary etc.).</summary>
    public int PlayerId => _player.id;

    public PlayerInput(Rewired.Player player) => _player = player;

    // ── One-frame combat suppression ─────────────────────────────────────────
    // Used to prevent the button that dismissed a dialog from firing a combat
    // action in the same frame. Set by PlayerStateMachine when dialog closes;
    // cleared by PlayerStateMachine.OnUpdate at the start of the next frame
    // (which runs before all other controllers).

    private bool _suppressCombatThisFrame;

    /// <summary>
    /// Block combat input for the rest of the current frame.
    /// Called by the state machine the instant a dialog closes so the confirm/
    /// reject button cannot simultaneously trigger a combat action.
    /// </summary>
    public void SuppressCombatThisFrame() => _suppressCombatThisFrame = true;

    /// <summary>
    /// Re-allow combat input. Called by PlayerStateMachine.OnUpdate at the very
    /// start of each frame, before any combat controller reads input.
    /// </summary>
    public void ClearFrameSuppression() => _suppressCombatThisFrame = false;

    // ── Gating helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// True when combat / movement input should pass through.
    /// Blocked in Disabled, Dialog, and Spectate contexts, and for one frame
    /// after a dialog closes to prevent dismiss-button bleed.
    /// </summary>
    private bool CombatInputActive =>
        !_suppressCombatThisFrame              &&
        Context != PlayerInputContext.Disabled &&
        Context != PlayerInputContext.Dialog   &&
        Context != PlayerInputContext.Spectate;

    // ── Combat input ──────────────────────────────────────────────────────────

    /// <summary>
    /// True when combat input is currently live (not suppressed, not in a
    /// blocking context). Exposed so AttackController can flush its input
    /// buffer in FixedUpdate when the player is in Dialog/Spectate/Disabled.
    /// </summary>
    public bool IsCombatInputActive => CombatInputActive;

    /// <summary>
    /// True when analogue axes (movement, rotation) should pass through.
    /// False in Comboing context so the player cannot steer mid-combo, while
    /// attack buttons remain live for combo buffering.
    /// </summary>
    private bool MovementInputActive =>
        CombatInputActive &&
        Context != PlayerInputContext.Comboing;

    public float GetAxis(string actionName)       => MovementInputActive ? _player.GetAxis(actionName)     : 0f;
    public bool  GetButton(string actionName)     => CombatInputActive && _player.GetButton(actionName);
    public bool  GetButtonDown(string actionName) => CombatInputActive && _player.GetButtonDown(actionName);
    public bool  GetButtonUp(string actionName)   => CombatInputActive && _player.GetButtonUp(actionName);

    // ── UI / dialog input ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns true only while in the Dialog context.
    /// Use this in <see cref="UserInterfaceController"/> so that face buttons
    /// (X/Y/A/B) drive dialog choices without cross-firing combat actions.
    /// </summary>
    public bool GetUIButtonDown(string actionName) =>
        Context == PlayerInputContext.Dialog && _player.GetButtonDown(actionName);
}
