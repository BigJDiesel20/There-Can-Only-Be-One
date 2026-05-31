using System;
using UnityEngine;
using Rewired;
using System.Collections.Generic;
using UnityEditor;

/// <summary>
/// Manages the player's attack state machine.
///
/// FRAME-PERFECT DESIGN
/// ─────────────────────
/// • Input  is read in Update  (GetButtonDown only fires once per rendered frame).
///   Presses are queued in a small ring buffer so they are never dropped between
///   FixedUpdate ticks.
/// • Execution runs in FixedUpdate at exactly 60 Hz (set Fixed Timestep to
///   0.016667 in Edit → Project Settings → Time).
/// • Hit detection inside Attack.Execute uses Physics.OverlapBoxNonAlloc —
///   synchronous, same tick, no physics-callback lag.
/// </summary>
[Serializable]
public class AttackController
{
    // ── References ────────────────────────────────────────────────────────
    LocalPlayerManager player;
    PlayerInput        gamePad;
    PlayerEvents       playerEvents;

    // ── Attacks ───────────────────────────────────────────────────────────
    [SerializeField] Attack LightAttack   = new Attack();
    [SerializeField] Attack HeaveyAttack  = new Attack();
    [SerializeField] Attack SpecialAttack = new Attack();
    [SerializeField] Attack LauncherAttack= new Attack();
    [SerializeField] Attack NoAttack      = new Attack();

    [SerializeField] IAttackCommand AttackCommand;

    // ── Combo tracking ────────────────────────────────────────────────────
    [SerializeField] int comboCounter = 0;
    [SerializeField] List<HitBoxTriggerEvents.AttackType> ComboChain =
        new List<HitBoxTriggerEvents.AttackType>();
    [SerializeField] List<List<HitBoxTriggerEvents.AttackType>> ComboList =
        new List<List<HitBoxTriggerEvents.AttackType>>();

    // ── Input buffer ──────────────────────────────────────────────────────
    // Presses captured in Update are held here until the next FixedUpdate tick.
    // Ring capacity = 4 queued inputs (more than enough for any real combo).
    private readonly Queue<HitBoxTriggerEvents.AttackType> _inputBuffer =
        new Queue<HitBoxTriggerEvents.AttackType>(4);

    // ── Attack snap ───────────────────────────────────────────────────────
    public float attackSnapRange          = 8f;
    [SerializeField] float attackSnapAngleThreshold = 60f;
    public           bool  showGizmos               = true;

    Transform  lastSnapTarget = null;
    LayerMask  _playerMask;
    /// <summary>Pre-allocated overlap buffer — shared by snap and hit detection.</summary>
    readonly Collider[] _overlapBuffer = new Collider[16];

    // ── State ─────────────────────────────────────────────────────────────
    bool isInitialized        = false;
    private bool _isHitConfirmPause;

    public bool IsInitialized => isInitialized;

    // ── Update — input capture only ───────────────────────────────────────

    /// <summary>
    /// Called from LocalPlayerManager.Update.
    /// Only reads Rewired button-down events and queues them.
    /// GetButtonDown is edge-triggered per rendered frame so it MUST stay in Update.
    /// </summary>
    void OnUpdate()
    {
        if (!isInitialized || _isHitConfirmPause) return;

        if      (gamePad.GetButtonDown("X")) _inputBuffer.Enqueue(HitBoxTriggerEvents.AttackType.Light);
        else if (gamePad.GetButtonDown("Y")) _inputBuffer.Enqueue(HitBoxTriggerEvents.AttackType.Heavy);
        else if (gamePad.GetButtonDown("B")) _inputBuffer.Enqueue(HitBoxTriggerEvents.AttackType.Special);
        else if (gamePad.GetButtonDown("A")) _inputBuffer.Enqueue(HitBoxTriggerEvents.AttackType.Launcher);
    }

    // ── FixedUpdate — deterministic execution ─────────────────────────────

    /// <summary>
    /// Called from LocalPlayerManager.FixedUpdate at exactly 60 Hz.
    /// Consumes one buffered input per tick, then advances the attack state machine.
    /// </summary>
    void OnFixedUpdate()
    {
        if (!isInitialized) return;

        // If combat is blocked (Dialog, Spectate, Disabled, or one-frame suppression)
        // discard any queued inputs so pre-buffered attacks cannot fire after
        // the context restores (e.g. the button that confirmed a dialog).
        if (!gamePad.IsCombatInputActive)
        {
            _inputBuffer.Clear();
        }
        // Consume the oldest queued input (one per fixed tick — this IS the frame rate)
        else if (!AttackCommand.IsHitConfirmPause && _inputBuffer.Count > 0)
            QueNextAttack(GetAttack(_inputBuffer.Dequeue()));

        // Advance the active attack by one frame
        AttackCommand.Execute(_overlapBuffer, _playerMask);

        // Reset combo counter and snap lock once the cool-down block finishes
        if (AttackCommand.CoolDownProgress >= 1.0)
        {
            if (comboCounter > 0)
            {
                comboCounter   = 0;
                lastSnapTarget = null;
                playerEvents.OnAttackEnd?.Invoke();
            }
        }
    }

    // ── Combo queueing ────────────────────────────────────────────────────

    private void QueNextAttack(Attack nextAttack)
    {
        bool queued     = false;
        bool isNewChain = (comboCounter == 0);  // capture before any increment

        if (IsComboable(nextAttack.Type))
        {
            if (AttackCommand.AnimationProgress >= 1.0)
            {
                comboCounter++;
                AttackCommand.ResetAttack();
                AttackCommand = nextAttack;
                ComboChain.Add(nextAttack.Type);
                queued = true;
            }
        }
        else
        {
            if (AttackCommand.CoolDownProgress >= 1.0)
            {
                comboCounter = 1;
                AttackCommand.ResetAttack();
                AttackCommand = nextAttack;
                ComboChain.Clear();
                ComboChain.Add(nextAttack.Type);
                queued = true;
            }
        }

        if (queued)
        {
            // 1. Snap rotate toward the nearest target — returns the lunge direction.
            // 2. Pass that direction to the new attack so the lunge fires the same way.
            // 3. Notify the state machine (Comboing) that a new chain has started.
            Vector3 lungeDir = SnapToNearestTarget();
            AttackCommand.SetLungeDirection(lungeDir);

            if (isNewChain)
                playerEvents.OnAttackStart?.Invoke();
        }
    }

    // ── Attack snap ───────────────────────────────────────────────────────

    /// <summary>
    /// Rotates the player toward the nearest valid target within the snap arc
    /// and returns the horizontal lunge direction for that same target.
    /// Returns the player's current flat-forward if no target is found so the
    /// lunge always fires in a meaningful direction.
    /// </summary>
    Vector3 SnapToNearestTarget()
    {
        Transform attacker    = player.character.transform;
        Vector3   flatForward = new Vector3(attacker.forward.x, 0f, attacker.forward.z).normalized;

        // Hysteresis: keep the locked target while it stays inside the arc.
        if (lastSnapTarget != null)
        {
            Vector3 toLastTarget = lastSnapTarget.position - attacker.position;
            toLastTarget.y = 0f;

            if (toLastTarget.sqrMagnitude > 0.001f &&
                Vector3.Angle(flatForward, toLastTarget.normalized) <= attackSnapAngleThreshold)
            {
                // Still inside the arc — snap rotate and lunge toward the same target.
                Vector3 dir = toLastTarget.normalized;
                playerEvents.OnAttackRotate?.Invoke(Quaternion.LookRotation(dir), attackSnapAngleThreshold);
                return dir;
            }

            // Target has left the arc — clear it and search for a new one.
            lastSnapTarget = null;
        }

        int       hitCount   = Physics.OverlapSphereNonAlloc(
                                   attacker.position, attackSnapRange, _overlapBuffer, _playerMask);
        float     bestAngle  = float.MaxValue;
        Transform bestTarget = null;

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = _overlapBuffer[i];
            if (col.transform.IsChildOf(attacker) || col.transform == attacker) continue;

            LocalPlayerManager other = col.GetComponentInParent<LocalPlayerManager>();
            if (other == null || other == player) continue;

            Vector3 toTarget = col.transform.position - attacker.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.001f) continue;

            float angle = Vector3.Angle(flatForward, toTarget.normalized);
            if (angle <= attackSnapAngleThreshold && angle < bestAngle)
            {
                bestAngle  = angle;
                bestTarget = col.transform;
            }
        }

        lastSnapTarget = bestTarget;

        if (bestTarget != null)
        {
            Vector3 dir = bestTarget.position - attacker.position;
            dir.y = 0f;
            dir   = dir.normalized;
            playerEvents.OnAttackRotate?.Invoke(Quaternion.LookRotation(dir), attackSnapAngleThreshold);
            return dir;
        }

        // No target in arc — lunge straight forward.
        return flatForward;
    }

    // ── Gizmos ───────────────────────────────────────────────────────────

    public void DrawGizmos()
    {
        if (!showGizmos || player == null || player.character == null) return;

        Transform attacker = player.character.transform;
        Vector3   origin   = attacker.position;
        Vector3   flatFwd  = new Vector3(attacker.forward.x, 0f, attacker.forward.z).normalized;
        if (flatFwd == Vector3.zero) return;

        Gizmos.color = new Color(1f, 1f, 0f, 0.06f);
        Gizmos.DrawSphere(origin, attackSnapRange);
        Gizmos.color = new Color(1f, 1f, 0f, 0.55f);
        Gizmos.DrawWireSphere(origin, attackSnapRange);

        Gizmos.color = new Color(1f, 0.55f, 0f, 0.9f);
        Vector3 leftEdge  = origin + Quaternion.Euler(0, -attackSnapAngleThreshold, 0) * flatFwd * attackSnapRange;
        Vector3 rightEdge = origin + Quaternion.Euler(0,  attackSnapAngleThreshold, 0) * flatFwd * attackSnapRange;
        Gizmos.DrawLine(origin, leftEdge);
        Gizmos.DrawLine(origin, rightEdge);

        const int arcSegments = 32;
        Vector3 prevArc = leftEdge;
        for (int i = 1; i <= arcSegments; i++)
        {
            float   t       = i / (float)arcSegments;
            float   angle   = Mathf.Lerp(-attackSnapAngleThreshold, attackSnapAngleThreshold, t);
            Vector3 nextArc = origin + Quaternion.Euler(0, angle, 0) * flatFwd * attackSnapRange;
            Gizmos.DrawLine(prevArc, nextArc);
            prevArc = nextArc;
        }

        Gizmos.color = new Color(1f, 1f, 0f, 0.9f);
        Vector3 tip = origin + flatFwd * attackSnapRange;
        Gizmos.DrawLine(origin, tip);
        float   arrowSize  = attackSnapRange * 0.08f;
        Gizmos.DrawLine(tip, tip + Quaternion.Euler(0, -145f, 0) * flatFwd * arrowSize);
        Gizmos.DrawLine(tip, tip + Quaternion.Euler(0,  145f, 0) * flatFwd * arrowSize);

        if (lastSnapTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin, lastSnapTarget.position);
            Gizmos.DrawWireSphere(lastSnapTarget.position, 0.4f);
#if UNITY_EDITOR
            Handles.Label(lastSnapTarget.position + Vector3.up * 1.2f,
                          "◉ Snap Target", EditorStyles.boldLabel);
#endif
        }

#if UNITY_EDITOR
        Handles.Label(origin + Vector3.up * (attackSnapRange + 0.4f),
                      $"Range  {attackSnapRange} m   |   Threshold  ±{attackSnapAngleThreshold}°");
#endif
    }

    // ── Initialization ────────────────────────────────────────────────────

    public void Initialize(PlayerInput gamePad, LocalPlayerManager player,
                           Transform character, PlayerEvents playerEvents)
    {
        this.gamePad      = gamePad;
        this.player       = player;
        this.playerEvents = playerEvents;
        _playerMask       = LayerMask.GetMask("Player");

        // Build all attack moves.
        // All time values are in seconds (converted to frames internally at 60 Hz).
        // pushBackDistance : how far the defender travels  (speed = dist / hitStunDuration)
        // lungeDistance    : how far the attacker moves forward during startup  (0 = no lunge)

        NoAttack.Initialize(
            player,
            character,
            hitBoxName:          "No Hit Box",
            hitBoxPosition:      Vector3.zero,
            hitBoxEulerAngle:    Vector3.zero,
            hitBoxScale:         Vector3.zero,
            startupLength:       0f,
            animationLength:     0f,
            attackBlockLength:   0f,
            pushBackDistance:    0f,
            lungeDistance:       0f,
            type:                HitBoxTriggerEvents.AttackType.Light,
            playerEvents:        playerEvents);

        LightAttack.Initialize(
            player,
            character,
            hitBoxName:          "Light Attack Hit Box",
            hitBoxPosition:      new Vector3(0, .18f, .9f),
            hitBoxEulerAngle:    Vector3.zero,
            hitBoxScale:         new Vector3(.5f, .25f, 1),
            startupLength:       0.05f,    //  3f
            animationLength:     0.067f,   //  4f
            attackBlockLength:   0.233f,   // 14f   total = 21f
            pushBackDistance:    1.5f,
            lungeDistance:       5.0f,
            type:                HitBoxTriggerEvents.AttackType.Light,
            playerEvents:        playerEvents);

        HeaveyAttack.Initialize(
            player,
            character,
            hitBoxName:          "Heavy Attack Hit Box",
            hitBoxPosition:      new Vector3(0, .20f, 1),
            hitBoxEulerAngle:    new Vector3(-40, 0, 0),
            hitBoxScale:         new Vector3(.5f, .30f, 2),
            startupLength:       0.133f,   //  8f
            animationLength:     0.1f,     //  6f
            attackBlockLength:   0.367f,   // 22f   total = 36f
            pushBackDistance:    3f,
            lungeDistance:       10.0f,
            type:                HitBoxTriggerEvents.AttackType.Heavy,
            playerEvents:        playerEvents);

        SpecialAttack.Initialize(
            player,
            character,
            hitBoxName:          "Special Attack Hit Box",
            hitBoxPosition:      new Vector3(0, -.19f, 1),
            hitBoxEulerAngle:    Vector3.zero,
            hitBoxScale:         new Vector3(.5f, 1.1f, 1.4f),
            startupLength:       0.2f,     // 12f
            animationLength:     0.133f,   //  8f
            attackBlockLength:   0.567f,   // 34f   total = 54f
            pushBackDistance:    .5f,
            lungeDistance:       2.75f,
            type:                HitBoxTriggerEvents.AttackType.Special,
            playerEvents:        playerEvents);

        LauncherAttack.Initialize(
            player,
            character,
            hitBoxName:          "Launcher Attack Hit Box",
            hitBoxPosition:      new Vector3(0, .30f, .95f),
            hitBoxEulerAngle:    Vector3.zero,
            hitBoxScale:         new Vector3(.5f, 2.5f, 1.11f),
            startupLength:       0.083f,   //  5f
            animationLength:     0.083f,   //  5f
            attackBlockLength:   0.333f,   // 20f   total = 30f
            pushBackDistance:    0f,
            lungeDistance:       0f,
            type:                HitBoxTriggerEvents.AttackType.Launcher,
            playerEvents:        playerEvents);
        AttackCommand = NoAttack;

        // Define combo chains
        ComboList.Add(new List<HitBoxTriggerEvents.AttackType> {
            HitBoxTriggerEvents.AttackType.Light, HitBoxTriggerEvents.AttackType.Light,
            HitBoxTriggerEvents.AttackType.Light, HitBoxTriggerEvents.AttackType.Light });

        ComboList.Add(new List<HitBoxTriggerEvents.AttackType> {
            HitBoxTriggerEvents.AttackType.Light,  HitBoxTriggerEvents.AttackType.Heavy,
            HitBoxTriggerEvents.AttackType.Special,HitBoxTriggerEvents.AttackType.Launcher });

        ComboList.Add(new List<HitBoxTriggerEvents.AttackType> {
            HitBoxTriggerEvents.AttackType.Light, HitBoxTriggerEvents.AttackType.Light,
            HitBoxTriggerEvents.AttackType.Heavy, HitBoxTriggerEvents.AttackType.Heavy });

        ComboList.Add(new List<HitBoxTriggerEvents.AttackType> {
            HitBoxTriggerEvents.AttackType.Light,  HitBoxTriggerEvents.AttackType.Special });

        ComboList.Add(new List<HitBoxTriggerEvents.AttackType> {
            HitBoxTriggerEvents.AttackType.Heavy, HitBoxTriggerEvents.AttackType.Heavy });

        ComboList.Add(new List<HitBoxTriggerEvents.AttackType> {
            HitBoxTriggerEvents.AttackType.Heavy, HitBoxTriggerEvents.AttackType.Launcher });

        // Make each hitbox ignore its owner's own colliders
        Collider[] ownColliders = character.GetComponentsInChildren<Collider>();
        Attack[] allAttacks = { NoAttack, LightAttack, HeaveyAttack, SpecialAttack, LauncherAttack };
        foreach (var atk in allAttacks)
            foreach (var col in ownColliders)
                Physics.IgnoreCollision(atk.Hitbox, col, true);

        // Subscribe to events
        playerEvents.OnUpdate             += OnUpdate;
        playerEvents.OnFixedUpdate        += OnFixedUpdate;
        playerEvents.OnHitConfirm         += OnHitConfirm;
        playerEvents.OnHitConfirmPauseEnd += OnHitConfirmPauseEnd;

        isInitialized = true;
    }

    public void Deactivate()
    {
        NoAttack.Deactivate();        NoAttack       = null;
        LightAttack.Deactivate();     LightAttack    = null;
        HeaveyAttack.Deactivate();    HeaveyAttack   = null;
        SpecialAttack.Deactivate();   SpecialAttack  = null;
        LauncherAttack.Deactivate();  LauncherAttack = null;
        ComboList.Clear();
        _inputBuffer.Clear();

        playerEvents.OnUpdate             -= OnUpdate;
        playerEvents.OnFixedUpdate        -= OnFixedUpdate;
        playerEvents.OnHitConfirm         -= OnHitConfirm;
        playerEvents.OnHitConfirmPauseEnd -= OnHitConfirmPauseEnd;
        playerEvents   = null;
        isInitialized  = false;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private Attack GetAttack(HitBoxTriggerEvents.AttackType type)
    {
        switch (type)
        {
            case HitBoxTriggerEvents.AttackType.Light:    return LightAttack;
            case HitBoxTriggerEvents.AttackType.Heavy:    return HeaveyAttack;
            case HitBoxTriggerEvents.AttackType.Special:  return SpecialAttack;
            case HitBoxTriggerEvents.AttackType.Launcher: return LauncherAttack;
            default:                                       return NoAttack;
        }
    }

    bool IsComboable(HitBoxTriggerEvents.AttackType attackType)
    {
        int  newChainCount  = ComboChain.Count + 1;
        bool doesComboMatch = false;

        for (int i = 0; i < ComboList.Count; i++)
        {
            if (ComboList[i].Count < newChainCount) continue;

            int confirmed = 0;
            for (int j = 0; j < newChainCount; j++)
            {
                HitBoxTriggerEvents.AttackType chainType =
                    j < ComboChain.Count ? ComboChain[j] : attackType;
                if (ComboList[i][j] == chainType) confirmed++;
            }
            if (confirmed == newChainCount) doesComboMatch = true;
        }
        return doesComboMatch;
    }

    public void OnHitConfirm((Collider hitbox, Collider hurtbox) hitInfo)
        => _isHitConfirmPause = true;

    public void OnHitConfirmPauseEnd((Collider hitbox, Collider hurtbox) hitInfo)
        => _isHitConfirmPause = false;
}
