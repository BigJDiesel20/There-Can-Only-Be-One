#define DEBUG

using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Events;

/// <summary>
/// Represents a single attack move.
///
/// FRAME-PERFECT DESIGN
/// ─────────────────────
/// All timing is measured in frames at a fixed 60 Hz game loop (FixedUpdate).
/// • No float timers — progress is tracked with integer frame counters.
/// • Hit detection uses Physics.OverlapBoxNonAlloc called manually each fixed
///   frame during the active window, NOT OnTriggerEnter callbacks.
/// • The hitbox collider stays disabled at all times so it generates zero
///   PhysX trigger overhead.  Its transform is used only as a shape reference
///   for the manual overlap query.
/// • Execute() must be called from FixedUpdate (via AttackController).
/// </summary>
[Serializable]
public class Attack : IAttackCommand
{
    // ── Identity ──────────────────────────────────────────────────────────
    LocalPlayerManager player;
    HitBoxTriggerEvents.AttackType _type;
    List<(int comboIndex, HitBoxTriggerEvents.AttackType attackType)> ComboList;

    // ── Frame counters ────────────────────────────────────────────────────
    // All timing at 60 fps.  Progress properties expose 0..1 doubles so the
    // rest of the codebase needs no changes.
    // Three phases in order: Startup → Active → Recovery
    [SerializeField] int _startupFrames;     // frames before the hitbox becomes live
    [SerializeField] int _startupFrame;      // frames elapsed in startup
    [SerializeField] int _animationFrames;   // total frames for the active (hit) window
    [SerializeField] int _animationFrame;    // frames elapsed in current window
    [SerializeField] int _coolDownFrames;    // total frames for the cool-down block
    [SerializeField] int _coolDownFrame;     // frames elapsed in cool-down

    // Hit-stun pause: 12 frames = 0.2 s at 60 fps
    private const int HitStunFrames = 4;
    [SerializeField] int _hitStunFrame;

    // ── Hitbox / hurtbox ──────────────────────────────────────────────────
    Collider  hitBox;
    [SerializeField] Collider hurtBox;
    [SerializeField] bool isHitConfirm = false;
    Renderer  hitboxRenderer;
    Material  hitboxMaterial;
    [SerializeField] bool isAttackAnimationActive = false;
    Action    onAttack;
    Action    onMiss;

    // ── State ─────────────────────────────────────────────────────────────
    [SerializeField] bool isStartupActive      = false;
    [SerializeField] bool isCoolDownActive     = false;
    [SerializeField] bool _isHitConfirmPause   = false;
    Color tempColor;
    [SerializeField] float _pushBackDistance;  // distance the defender travels
    [SerializeField] float _pushBackSpeed;     // computed: pushBackDistance / 0.2s (fixed travel window)

    PlayerEvents playerEvents;

    // ── Lunge ──────────────────────────────────────────────────────────────
    // A lunge moves the player forward during the startup wind-up phase.
    // Direction is set by AttackController (after snap rotate) so the lunge
    // always fires toward the snap target, not wherever the stick is pointing.
    // A SphereCast each frame stops the lunge early if geometry is in the way.
    Transform  _character;
    Rigidbody  _rb;
    float      _lungeDistance;     // total distance to cover over all startup frames
    float      _lungePerFrame;     // distance applied each startup frame
    Vector3    _lungeDirection;    // set once per attack by SetLungeDirection
    bool       _lungeActive;       // false once obstructed or distance exhausted
    float      _lungeRadius;       // sphere radius for obstruction cast
    LayerMask  _obstructionMask;   // everything except the Player layer
    [SerializeField] float _lungeStopGap = 0.3f;  // extra clearance kept between player and obstruction

    // ── Timestamped animation callbacks ───────────────────────────────────
    public List<(double time, Action action)> onAnimation = new List<(double time, Action action)>();

    // ── Properties ────────────────────────────────────────────────────────

    /// <summary>Animation length in seconds (converts to/from internal frame count).</summary>
    public double AnimationLength
    {
        get => _animationFrames / 60.0;
        set => _animationFrames = Mathf.RoundToInt((float)value * 60f);
    }

    /// <summary>Cool-down length in seconds.</summary>
    public double CoolDownLength
    {
        get => _coolDownFrames / 60.0;
        set => _coolDownFrames = Mathf.RoundToInt((float)value * 60f);
    }

    public Collider Hitbox
    {
        get => hitBox;
        set
        {
            hitBox           = value;
            hitboxRenderer   = hitBox.GetComponent<Renderer>();
            hitboxMaterial   = hitboxRenderer.material;
            hitboxRenderer.enabled = false; // invisible until active
            hitBox.isTrigger = true;
            hitBox.enabled   = false; // never a live trigger — manual overlap only
        }
    }

    public HitBoxTriggerEvents.AttackType Type { get => _type; set => _type = value; }
    public bool   IsAttackActive    => isAttackAnimationActive;
    public bool   IsHitConfirmPause => _isHitConfirmPause;

    /// <summary>0..1 progress through the startup wind-up window.</summary>
    public double StartupProgress =>
        _startupFrames == 0 ? 1.0 : (double)_startupFrame / _startupFrames;

    /// <summary>0..1 progress through the active (hit) window.</summary>
    public double AnimationProgress =>
        _animationFrames == 0 ? 1.0 : (double)_animationFrame / _animationFrames;

    /// <summary>0..1 progress through the cool-down block.</summary>
    public double CoolDownProgress =>
        _coolDownFrames == 0 ? 1.0 : (double)_coolDownFrame / _coolDownFrames;

    // ── Execute — called from FixedUpdate by AttackController ─────────────

    public void Execute(Collider[] hitBuffer, LayerMask playerMask)
    {
        if (_isHitConfirmPause)
        {
            TickHitStunPause();
            return;
        }

        ActivateAttack();

        if (isCoolDownActive && isStartupActive)
        {
            DoWhileStartupIsActive();
            DeactivateStartupOnComplete();
        }
        else if (isCoolDownActive && isAttackAnimationActive)
        {
            DoWhileAnimationIsActive(hitBuffer, playerMask);
            DeactivateAttackAnimationOnComplete();
        }
        else if (isCoolDownActive && !isStartupActive && !isAttackAnimationActive)
        {
            DoWhileAttackBlockIsActive();
            DeactivateAttackBlockOnComplete();
        }
    }

    // ── Hit-stun pause ────────────────────────────────────────────────────

    private void TickHitStunPause()
    {
        hitboxMaterial.color = Color.green;
        _hitStunFrame++;

        if (_hitStunFrame < HitStunFrames) return;

        // Pause over — restore colour and fire end events
        hitboxMaterial.color = tempColor;
        _hitStunFrame        = 0;
        _isHitConfirmPause   = false;
        playerEvents.OnHitConfirmPauseEnd?.Invoke((hitBox, hurtBox));

        if (hurtBox == null) return;

        switch (_type)
        {
            case HitBoxTriggerEvents.AttackType.Launcher:
                hurtBox.GetComponent<PlayerDetection>().PlayerEvents
                       .OnPush?.Invoke(Vector3.up * _pushBackSpeed);
                break;
            default:
                Vector3 dir = (hurtBox.transform.position - hitBox.transform.position).normalized;
                dir.y = 0f;
                hurtBox.GetComponent<PlayerDetection>().PlayerEvents
                       .OnPush?.Invoke(dir * _pushBackSpeed);
                break;
        }
    }

    // ── Attack lifecycle ──────────────────────────────────────────────────

    private void ActivateAttack()
    {
        // Only starts when fully reset (cooldown and active window both at 0 progress)
        if (CoolDownProgress != 0.0 || AnimationProgress != 0.0) return;
        // Guard against re-triggering while startup or active window is still running
        if (isStartupActive || isAttackAnimationActive) return;

        isCoolDownActive = true;
        onAttack?.Invoke();

        // If this attack has a startup wind-up, enter that phase first.
        // Otherwise jump straight to the active (hit) window.
        if (_startupFrames > 0)
        {
            isStartupActive        = true;
            hitboxRenderer.enabled = true;
            hitboxMaterial.color   = Color.blue; // Blue during startup
        }
        else
        {
            isAttackAnimationActive = true;
            hitboxRenderer.enabled  = true;
            hitboxMaterial.color    = Color.red; // Red during active
        }
    }

    /// <summary>Counts up during the startup wind-up and advances the lunge.</summary>
    private void DoWhileStartupIsActive()
    {
        _startupFrame++;
        ApplyLunge();
    }

    /// <summary>
    /// Moves the player forward by one frame's worth of lunge distance.
    /// Stops early if a SphereCast detects geometry in the path.
    /// </summary>
    private void ApplyLunge()
    {
        if (!_lungeActive || _rb == null || _lungePerFrame == 0f) return;

        // Cast a sphere forward by the per-frame step to check for obstruction.
        // Origin is raised to mid-body height so the cast doesn't scrape the floor.
        // SphereCast will not detect colliders that overlap the origin, so the
        // attacker's own capsule is naturally skipped.
        Vector3 origin = _character.position + Vector3.up * (_lungeRadius + 0.05f);

        if (Physics.SphereCast(origin, _lungeRadius, _lungeDirection,
                               out RaycastHit hit, _lungePerFrame, _obstructionMask,
                               QueryTriggerInteraction.Ignore))
        {
            // Stop the player short of the obstruction by _lungeStopGap units.
            // hit.distance is already sphere-center-to-surface, so subtracting the
            // gap keeps the character visibly clear of the obstacle.
            float safeDistance = Mathf.Max(0f, hit.distance - _lungeStopGap);
            if (safeDistance > 0f)
                _rb.MovePosition(_rb.position + _lungeDirection * safeDistance);

            _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
            _lungeActive = false;
            return;
        }
        // If the path is clear, velocity carries the player — nothing else needed.
    }

    /// <summary>Transitions from startup into the active (hit) window once wind-up is done.</summary>
    private void DeactivateStartupOnComplete()
    {
        if (_startupFrame < _startupFrames) return;
        isStartupActive         = false;
        isAttackAnimationActive = true;
        hitboxMaterial.color    = Color.red; // Red — hitbox is now live (renderer already enabled)

        // Lunge finished — kill horizontal velocity so the player stops cleanly
        // at the end of the wind-up rather than sliding into the active window.
        if (_lungeActive && _rb != null)
        {
            _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
            _lungeActive = false;
        }
    }

    private void DoWhileAnimationIsActive(Collider[] hitBuffer, LayerMask playerMask)
    {
        _animationFrame++;

        // Manual hit detection — runs synchronously this frame
        CheckHits(hitBuffer, playerMask);

        // Fire any timestamped animation callbacks
        for (int i = 0; i < onAnimation.Count; i++)
        {
            if (AnimationProgress >= onAnimation[i].time)
                onAnimation[i].action?.Invoke();
        }
    }

    private void DoWhileAttackBlockIsActive()    => _coolDownFrame++;

    private void DeactivateAttackAnimationOnComplete()
    {
        if (_animationFrame < _animationFrames) return;

        playerEvents.OnAnimationComplete?.Invoke();
        hitboxRenderer.enabled  = false; // completely invisible during recovery and idle
        isAttackAnimationActive = false;
    }

    private void DeactivateAttackBlockOnComplete()
    {
        if (_coolDownFrame < _coolDownFrames) return;

        playerEvents.OnCoolDownComplete?.Invoke();
        isCoolDownActive = false;
    }

    // ── Hit detection ─────────────────────────────────────────────────────

    /// <summary>
    /// Manual OverlapBox query each active frame.
    /// The hitbox collider is disabled so there is zero PhysX trigger
    /// overhead — we compute world-space bounds from the transform directly.
    /// </summary>
    private void CheckHits(Collider[] hitBuffer, LayerMask playerMask)
    {
        if (isHitConfirm) return;

        BoxCollider box = hitBox as BoxCollider;
        if (box == null) return;

        // World-space center and half-extents (works even when collider is disabled)
        Vector3 worldCenter  = hitBox.transform.TransformPoint(box.center);
        Vector3 halfExtents  = Vector3.Scale(box.size * 0.5f, hitBox.transform.lossyScale);
        halfExtents = new Vector3(Mathf.Abs(halfExtents.x),
                                  Mathf.Abs(halfExtents.y),
                                  Mathf.Abs(halfExtents.z));

        int hitCount = Physics.OverlapBoxNonAlloc(
            worldCenter, halfExtents, hitBuffer,
            hitBox.transform.rotation, playerMask);

        for (int i = 0; i < hitCount; i++)
        {
            PlayerDetection pd = hitBuffer[i].GetComponent<PlayerDetection>();
            if (pd == null || pd.Player == player) continue;

            // Register the first valid hit and enter hit-stun pause
            hurtBox   = hitBuffer[i];
            tempColor = hitboxMaterial.color;

#if DEBUG
            Debug.Log($"[Attack] Hit {hurtBox.name} — type={_type}  frame={_animationFrame}/{_animationFrames}");
#endif
            isHitConfirm = _isHitConfirmPause = true;
            playerEvents.OnHitConfirm?.Invoke((hitBox, hurtBox));
            pd.PlayerEvents.OnDamageReceived(new Damage(10, Damage.AttackType.Smash));
            break; // one target per frame
        }
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Called by AttackController immediately after snap rotation so the lunge
    /// always fires toward the correct target.
    /// <paramref name="direction"/> should be a normalised horizontal vector;
    /// pass the player's current forward if there is no snap target.
    /// </summary>
    public void SetLungeDirection(Vector3 direction)
    {
        _lungeDirection = direction;
        _lungeActive    = _lungePerFrame > 0f;

        if (_lungeActive && _rb != null)
        {
            // Apply horizontal lunge velocity once. _lungePerFrame * 60 converts
            // the per-fixed-frame distance back to m/s. Y is preserved so gravity
            // continues uninterrupted.
            float speed = _lungePerFrame * 60f;
            _rb.linearVelocity = new Vector3(
                _lungeDirection.x * speed,
                _rb.linearVelocity.y,
                _lungeDirection.z * speed);
        }
    }

    public void ResetAttack()
    {
        _startupFrame = _animationFrame = _coolDownFrame = _hitStunFrame = 0;
        isStartupActive = isCoolDownActive = isAttackAnimationActive = isHitConfirm = _isHitConfirmPause = false;
        hurtBox = null;

        // If a lunge was mid-flight when the attack was reset (e.g. combo chained
        // during startup), kill the horizontal velocity immediately.
        if (_lungeActive && _rb != null)
            _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
        _lungeActive = false;
    }

    public bool IsComboAble(int ComboIndex, HitBoxTriggerEvents.AttackType attackType)
    {
        if (ComboList == null) return false;
        int next = ComboIndex + 1;
        for (int i = 0; i < ComboList.Count; i++)
            if (ComboList[i].comboIndex == next && ComboList[i].attackType == attackType)
                return true;
        return false;
    }

    public void SetCombos(List<(int comboIndex, HitBoxTriggerEvents.AttackType attackType)> combos)
        => ComboList = combos;

    /// <summary>Schedule an action at a normalised progress point (0..1).</summary>
    public void SetOnAnimaiton(Action action, double animationProgress)
        => onAnimation.Add((animationProgress, action));

    /// <summary>Schedule an action at an absolute time (seconds) within the animation.</summary>
    public void SetOnAnimaiton(Action action, float time = 0f)
    {
        double animLength = _animationFrames / 60.0;
        double progress   = animLength == 0 ? 0.0 : Clamp(time, 0, animLength) / animLength;
        onAnimation.Add((progress, action));
    }

    public void AddTimeStampedAction(int frame, Action action)
    {
        // frame expressed as a progress fraction so it lines up with the existing list
        double progress = _animationFrames == 0 ? 0.0 : (double)frame / _animationFrames;
        onAnimation.Add((progress, action));
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────

    public void Initialize(
        LocalPlayerManager player,
        Transform          character,
        string             hitBoxName,
        Vector3            hitBoxPosition,
        Vector3            hitBoxEulerAngle,
        Vector3            hitBoxScale,
        double             startupLength,      // frames before the hitbox is live (wind-up)
        double             animationLength,    // frames the hitbox is active (hit window)
        double             attackBlockLength,  // frames of recovery after the active window
        float              pushBackDistance,   // distance the defender is pushed (speed = dist / 0.2s)
        float              lungeDistance,      // total forward distance during startup (0 = no lunge)
        HitBoxTriggerEvents.AttackType type,
        PlayerEvents       playerEvents)
    {
        this.player           = player;
        this.playerEvents     = playerEvents;
        _pushBackDistance     = pushBackDistance;
        _type                 = type;

        // ── Pushback speed ─────────────────────────────────────────────────
        // Fixed travel window for pushback — independent of HitStunFrames so
        // tuning the freeze duration does not affect how far the defender flies.
        // speed = distance / duration  — same formula as lunge.
        const float pushBackTravelDuration = 0.2f;  // seconds the defender travels after hit-stun
        _pushBackSpeed = pushBackDistance > 0f ? pushBackDistance / pushBackTravelDuration : 0f;

        // ── Lunge setup ────────────────────────────────────────────────────
        _character       = character;
        _lungeDistance   = lungeDistance;
        _rb              = character.GetComponentInParent<Rigidbody>()
                        ?? character.GetComponentInChildren<Rigidbody>();
        _obstructionMask = Physics.DefaultRaycastLayers;  // includes players and geometry

        // Derive sphere radius from the character's CapsuleCollider if available.
        CapsuleCollider cap = character.GetComponentInParent<CapsuleCollider>();
        _lungeRadius = cap != null ? cap.radius * 0.9f : 0.3f;

        // Spawn the hitbox prefab
        GameObject hitboxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Hit Box.prefab");
        hitBox = GameObject.Instantiate(hitboxPrefab).GetComponent<BoxCollider>();

        hitBox.isTrigger = true;
        hitBox.enabled   = false;   // ← never live — manual overlap only, zero trigger cost
        hitBox.transform.SetParent(character);
        hitBox.gameObject.name               = hitBoxName;
        hitBox.transform.localPosition       = hitBoxPosition;
        hitBox.transform.localEulerAngles    = hitBoxEulerAngle;
        hitBox.transform.localScale          = hitBoxScale;

        // Convert seconds → frames (60 Hz fixed loop)
        _startupFrames   = Mathf.RoundToInt((float)startupLength    * 60f);
        _animationFrames = Mathf.RoundToInt((float)animationLength  * 60f);
        _coolDownFrames  = Mathf.RoundToInt((float)attackBlockLength * 60f);

        // Spread the total lunge distance evenly across all startup frames.
        // Zero if there is no startup window or no lunge distance.
        _lungePerFrame = (_startupFrames > 0 && _lungeDistance > 0f)
            ? _lungeDistance / _startupFrames
            : 0f;

        // Renderer setup — disabled until the attack is active (truly invisible, not just alpha=0)
        hitboxRenderer         = hitBox.GetComponent<Renderer>();
        hitboxMaterial         = hitboxRenderer.material;
        hitboxRenderer.enabled = false;
    }

    public void Deactivate()
    {
        this.playerEvents = null;
        if (hitBox != null) GameObject.Destroy(hitBox.gameObject);
        hitBox = null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static double Clamp(double value, double min, double max)
        => value <= min ? min : value >= max ? max : value;
}
