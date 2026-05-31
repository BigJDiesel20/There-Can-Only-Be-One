using UnityEngine;
using Rewired;
using System;
using UnityEngine.TextCore.Text;
using Unity.VisualScripting;



[Serializable]
public class MovementController
{
    public PlayerInput gamePad;
    public Rigidbody rb;
    public float signX;
    public float signY;
    public Transform camera;
    public Transform cameraLocation;
    CameraStateWrapper cameraStateWrapper;
    public float pitch = 0;
    public float yaw = 0;
    public GameObject character;

    [SerializeField]
    Vector3 currentForceDirection = Vector3.zero;
    [SerializeField]
    Vector3 forceDirection = Vector3.zero;
    [SerializeField]
    bool isPushed = false;
    [SerializeField]
    bool isLaunched = false;
    [SerializeField]
    (bool x, bool z)  isZero = (true, true);
    [SerializeField]
    int isPushCompleted = 0;
    Vector3 currentRoation = Vector3.zero;
    Vector3 fallPosition = Vector3.zero;
    float startTime = 0;
    float fallStartTime = 0;
    Vector3 fallStartRotation = Vector3.zero;
    float fallDuration = 0.5f;
    float standUpDuration = 1f;


    public RaycastHit hit;
    public Vector3 offset = new Vector3(0, -0.9f, 0);
    [SerializeField] float groundSphereRadius = 0.3f;  // match ~half character width
    [SerializeField] float groundCheckDist    = 0.2f;  // distance swept below the sphere

    private bool isInitialized = false;
    public bool IsInitialized { get { return isInitialized; } }
    [SerializeField] bool _isHitConfirmPause = false;
    private PlayerEvents playerEvents;
    public bool debugLogs = false;

    // Movement speeds per camera mode
    public float orbitMoveSpeed     = 20f;
    public float followMoveSpeed    =  5f;
    public float sideViewMoveSpeed  =  5f;

    // Attack snap-to-target
    private bool       isAttackSnapping  = false;
    private Quaternion attackSnapTarget  = Quaternion.identity;
    private float      _snapThreshold    = 60f;   // arc degrees — mirrored from AttackController via event
    public  float      attackSnapSpeed   = 720f;  // degrees per second
    //GameObject correctedTransform;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnStart()
    {

        Mathf.Atan2(gamePad.GetAxis("Move Horizontal"), gamePad.GetAxis("Move Vertical"));
    }

    // Update is called once per frame
    /// <summary>
    /// Runs in Update (render loop). Only reads edge-triggered input into
    /// sticky flags — no physics here. Uses |= so rapid presses between
    /// FixedUpdate ticks are never silently dropped.
    /// </summary>
    void OnInputUpdate()
    {
        _jumpPressed  |= gamePad.GetButtonDown("A");
        _jumpReleased |= gamePad.GetButtonUp("A");
    }

public void OnUpdate()
    {
        GroundDetection();

        switch (cameraStateWrapper.CurrentState)
        {
            case CameraStateWrapper.CameraState.Orbit:
                Orbit();
                break;
            case CameraStateWrapper.CameraState.Follow:
                Follow();
                break;
            case CameraStateWrapper.CameraState.FightingSide:
                FightingSideMove();
                break;
        }

        // Attack snap-to-target: runs after movement so it always wins the rotation.
        // Stays active (holding the player facing the target) until OnAttackEnd clears it.
        if (isAttackSnapping)
        {
            Quaternion next = Quaternion.RotateTowards(rb.rotation, attackSnapTarget, attackSnapSpeed * Time.deltaTime);
            rb.MoveRotation(next);
        }
    }

    public void Orbit()
    {
        if (!isProne)
        {
            if (!_isHitConfirmPause)
            {
                signX = orbitMoveSpeed * gamePad.GetAxis("Move Horizontal");
                signY = orbitMoveSpeed * gamePad.GetAxis("Move Vertical");
                //correctedTransform.transform.eulerAngles = new Vector3(0, myCamera.eulerAngles.y, myCamera.eulerAngles.z);
                //correctedTransform.transform.position = myCamera.position;            
                Vector3 relativeDirection = (signY * cameraLocation.forward + signX * cameraLocation.right + 0 * cameraLocation.up);
                // tan(o)* z = y
                // tan()/ y =

                if (isZero.x == false)
                {
                    currentForceDirection.x = Mathf.MoveTowards(currentForceDirection.x, 0, Mathf.Abs(forceDirection.x) * 2 * Time.deltaTime);
                    if (currentForceDirection.x == 0)
                    {
                        isZero.x = true;
                    }
                }

                if (isZero.z == false)
                {
                    currentForceDirection.z = Mathf.MoveTowards(currentForceDirection.z, 0, Mathf.Abs(forceDirection.z) * 2 * Time.deltaTime);
                    if (currentForceDirection.z == 0)
                    {
                        isZero.z = true;
                    }
                }

                if (debugLogs) Debug.Log($"isPushCompleted >= 2: {isPushCompleted >= 2} isPushed: {isPushed}");
                if (isZero == (true, true))
                {
                    isPushed = false;
                }



                if (!isPushed)
                {
                    float standUpRecoveryTime = Mathf.Clamp01((Time.time - startTime) / standUpDuration);

                    if (standUpRecoveryTime < 1f)
                    {
                        float targetY = (relativeDirection != Vector3.zero)
                            ? Mathf.Atan2(relativeDirection.x, relativeDirection.z) * Mathf.Rad2Deg
                            : currentRoation.y;
                        rb.MoveRotation(Quaternion.Euler(
                            Mathf.SmoothStep(fallPosition.x, 0, standUpRecoveryTime),
                            Mathf.SmoothStep(fallPosition.y, targetY, standUpRecoveryTime),
                            Mathf.SmoothStep(fallPosition.z, 0, standUpRecoveryTime)
                        ));
                        rb.linearVelocity = new Vector3(0, Jump(), 0);
                    }
                    else
                    {
                        if (relativeDirection != Vector3.zero)
                        {
                            // If snap-locked, check whether the stick direction exceeds the arc
                            // threshold relative to the locked forward. If so, break the lock.
                            if (isAttackSnapping)
                            {
                                Vector3 snapForward = attackSnapTarget * Vector3.forward;
                                snapForward.y = 0f;
                                float stickAngle = Vector3.Angle(relativeDirection.normalized, snapForward.normalized);
                                if (stickAngle > _snapThreshold)
                                    isAttackSnapping = false;
                            }

                            if (!isAttackSnapping)
                            {
                                //VIABLE CODE DO NO ERASE
                                //rb.transform.forward = myCamera.forward;
                                //rb.transform.eulerAngles = new Vector3(rb.transform.eulerAngles.x - myCamera.eulerAngles.x, rb.transform.eulerAngles.y, rb.transform.eulerAngles.z);
                                rb.MoveRotation(Quaternion.Euler(0,
                                    Mathf.Atan2(relativeDirection.x, relativeDirection.z) * Mathf.Rad2Deg, 0));
                            }
                        }
                        // Break snap lock the moment the player leaves the ground —
                        // jumping always restores full movement.
                        if (isAttackSnapping && jumpState != JumpState.Grounded)
                            isAttackSnapping = false;

                        rb.linearVelocity = isAttackSnapping
                            ? new Vector3(0, Jump(), 0)
                            : relativeDirection + new Vector3(0, Jump(), 0);
                    }
                }
                else
                {
                    rb.MoveRotation(Quaternion.Euler(currentRoation));
                    rb.linearVelocity = new Vector3(currentForceDirection.x, Jump(), currentForceDirection.z);
                }





            }
            else
            {
                rb.MoveRotation(Quaternion.Euler(currentRoation));
                rb.linearVelocity = Vector3.zero;
            }
        }
        else
        {
            float fallProgress = Mathf.Clamp01((Time.time - fallStartTime) / fallDuration);
            rb.MoveRotation(Quaternion.Euler(
                Mathf.SmoothStep(fallStartRotation.x, -90, fallProgress),
                currentRoation.y,
                Mathf.SmoothStep(fallStartRotation.z, 0, fallProgress)
            ));
            fallPosition = rb.transform.eulerAngles;
            if (fallPosition.x > 180f) fallPosition.x -= 360f;
            rb.linearVelocity = new Vector3(0, Jump(), 0);
        }
    }


public void Follow()
    {
        if (!isProne)
        {
            if (!_isHitConfirmPause)
            {
                signX = followMoveSpeed * gamePad.GetAxis("Move Horizontal");
                signY = followMoveSpeed * gamePad.GetAxis("Move Vertical");

                // Suppress right-stick X character rotation while Follow aim-lock is active
                if (!cameraStateWrapper.IsFollowAimLock)
                    yaw += 200f * gamePad.GetAxis("Right Stick X") * Time.fixedDeltaTime;

                if (!isPushed)
                {
                    rb.linearVelocity = rb.transform.TransformDirection(new Vector3(signX, Jump(), signY));
                    rb.MoveRotation(Quaternion.Euler(0, yaw, 0));
                }
                else
                {
                    rb.linearVelocity = new Vector3(currentForceDirection.x, Jump(), currentForceDirection.z);
                    rb.MoveRotation(Quaternion.Euler(currentRoation));
                }
            }
            else
            {
                rb.MoveRotation(Quaternion.Euler(currentRoation));
                rb.linearVelocity = Vector3.zero;
            }
        }
        else
        {
        }
    }

void FightingSideMove()
    {
        if (!isProne)
        {
            if (!_isHitConfirmPause)
            {
                Vector3 fightAxis = cameraStateWrapper.FightAxis;
                if (fightAxis.sqrMagnitude > 0.01f)
                    yaw = Mathf.Atan2(fightAxis.x, fightAxis.z) * Mathf.Rad2Deg;

                signX = sideViewMoveSpeed * gamePad.GetAxis("Move Horizontal");
                signY = sideViewMoveSpeed * gamePad.GetAxis("Move Vertical");

                if (!isPushed)
                {
                    rb.linearVelocity = rb.transform.TransformDirection(new Vector3(-signY, Jump(), signX));
                    rb.MoveRotation(Quaternion.Euler(0, yaw, 0));
                }
                else
                {
                    rb.linearVelocity = new Vector3(currentForceDirection.x, Jump(), currentForceDirection.z);
                    rb.MoveRotation(Quaternion.Euler(currentRoation));
                }
            }
            else
            {
                rb.MoveRotation(Quaternion.Euler(currentRoation));
                rb.linearVelocity = Vector3.zero;
            }
        }
    }


    
    public void OnOnOnGUI()
    {
        GUI.TextArea(new Rect(10, 10, 200, 100), rb.linearVelocity.ToString(), 200); ;
    }
    public void GetInput()
    {
        /*"Xaxis: "+ ReInput.players.GetPlayer(1).GetAxis("Left Stick X").ToString() + " Yaxis: "+ ReInput.players.GetPlayer(1).GetAxis("Left Stick Y").ToString()*/

    }

    void MinMax(ref float number, float min, float max)
    {
        number = (number < min) ? min : (number > max) ? max : number;

    }

    public void Initialize(LocalPlayerManager player, Transform camera, Transform cameraLocation, CameraStateWrapper cameraStateWrapper, GameObject charater, ref bool isMovementInitialized, PlayerEvents playerEvents)
    {
        gamePad = player.playerInput;
        rb = player.character.GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        this.camera = camera;
        this.cameraLocation = cameraLocation;
        this.character = charater;
        this.cameraStateWrapper = cameraStateWrapper;
        //correctedTransform = new GameObject("RelativeTransform");
        //correctedTransform.transform.SetParent(_player.gameObject.transform);
        isMovementInitialized = isInitialized = true;

        this.playerEvents = playerEvents;
        this.playerEvents.OnUpdate      += OnInputUpdate; // edge-triggered input (Update rate)
        this.playerEvents.OnFixedUpdate += OnUpdate;      // physics (FixedUpdate rate)
        this.playerEvents.OnHitConfirm += OnHitConfirm;
        this.playerEvents.OnHitConfirmPauseEnd += OnHitConfirmPauseEnd;
        this.playerEvents.OnPush += OnPush;
        this.playerEvents.OnInvulnerabilityActive += OnInvulnerabilityActive;
        this.playerEvents.OnProneActive           += OnProneActive;
        this.playerEvents.OnAttackRotate          += OnAttackRotate;
        this.playerEvents.OnAttackEnd             += OnAttackEnd;

    }

    

    /// <summary>
    /// Immediately freezes the character by making the Rigidbody kinematic.
    /// Kinematic removes it from the physics simulation entirely — no forces,
    /// no collider pushing, no settling — so the character stays exactly where
    /// it is regardless of what else is happening in the scene.
    /// Also clears all internal force/push/jump state so resuming is clean.
    /// Call this on Battle exit; pair with Resume() on Battle enter.
    /// </summary>
    public void Halt()
    {
        if (rb == null) return;

        // Freeze physics first — kinematic ignores ALL external forces/contacts.
        rb.isKinematic     = true;
        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        currentForceDirection = Vector3.zero;
        forceDirection        = Vector3.zero;
        isPushed              = false;
        isLaunched            = false;
        isZero                = (true, true);
        _jumpPressed          = false;
        _jumpReleased         = false;
        charge                = 0f;
        _jumpBufferFrame      = 0;
        _coyoteFrame          = 0;
        jumpState             = JumpState.Grounded;
        isAttackSnapping      = false;
    }

    /// <summary>
    /// Re-enables full physics simulation after a Halt().
    /// Call this at the start of Battle.OnLoad() before spawning/repositioning
    /// characters so the Rigidbody is dynamic again when gameplay begins.
    /// </summary>
    public void Resume()
    {
        if (rb == null) return;
        rb.isKinematic     = false;
        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void Deactivate()
    {
        gamePad = null;
        rb = null;
        this.playerEvents.OnUpdate      -= OnInputUpdate;
        this.playerEvents.OnFixedUpdate -= OnUpdate;
        this.playerEvents.OnHitConfirm -= OnHitConfirm;
        this.playerEvents.OnHitConfirmPauseEnd -= OnHitConfirmPauseEnd;
        this.playerEvents.OnPush -= OnPush;
        this.playerEvents.OnInvulnerabilityActive -= OnInvulnerabilityActive;
        this.playerEvents.OnProneActive           -= OnProneActive;
        this.playerEvents.OnAttackRotate          -= OnAttackRotate;
        this.playerEvents.OnAttackEnd             -= OnAttackEnd;
        this.playerEvents = null;
        //GameObject.Destroy(correctedTransform.gameObject);
        //correctedTransform = null;

    }
    public float height     = 8f;
    public float chargeRate = 3f;  // multiplier — full charge in (1 / chargeRate) seconds
    public float charge     = 0f;
    public float launchForce = 0;
    public bool isGrounded;
    public bool isJumping = false;
    public float gravity = 14f;
    public enum JumpState { Grounded, Jumping, Falling, Launched }
    public JumpState jumpState;
    private bool isInvulnerabilityActive;
    private bool isProne;

    // ── Jump game-feel ────────────────────────────────────────────────
    private const int   CoyoteFrames        = 6;    // frames after walking off a ledge where jump still fires
    private const int   JumpBufferFrames    = 6;    // frames before landing where a jump press is remembered
    private const float MinJumpVelocity      = 3f;   // minimum launch speed — quick tap = snappy small hop
    private const float ApexThreshold        = 1.0f; // |Y velocity| below this triggers apex hang
    private const float ApexGravityScale     = 0.65f;// gravity multiplier during apex hang (subtle, not floaty)
    private const float PostApexGravityScale = 4f;   // faster fall after the apex hang ends
    private int  _coyoteFrame     = 0;
    private int  _jumpBufferFrame = 0;
    private bool _hadApexHang     = false;  // true if apex hang fired this jump

    // ── Buffered jump input ───────────────────────────────────────────
    // GetButtonDown/Up are only true for one Update frame. Reading them
    // directly in FixedUpdate risks missing the input between ticks.
    // These flags are set in OnInputUpdate (Update) and cleared after
    // Jump() consumes them in OnUpdate (FixedUpdate).
    private bool _jumpPressed  = false;
    private bool _jumpReleased = false;

    public float Jump()
    {
        bool jumpHeld = gamePad.GetButton("A"); // held-state is safe to poll in FixedUpdate
        bool jumpDown = _jumpPressed;           // set by OnInputUpdate, consumed below
        bool jumpUp   = _jumpReleased;          // set by OnInputUpdate, consumed below

        // ── Jump buffer ───────────────────────────────────────────────
        // Remember a press for JumpBufferFrames ticks so a press just
        // before landing still fires a jump on touch-down.
        if (jumpDown)                  _jumpBufferFrame = JumpBufferFrames;
        else if (_jumpBufferFrame > 0) _jumpBufferFrame--;

        switch (jumpState)
        {
            // ── Grounded ─────────────────────────────────────────────
            case JumpState.Grounded:
                if (!isProne)
                {
                    // Top up the coyote window every tick we are on solid ground
                    if (isGrounded) _coyoteFrame = CoyoteFrames;

                    // Charge while holding button on the ground
                    if (isGrounded && jumpHeld)
                    {
                        charge += Time.fixedDeltaTime * chargeRate;
                        charge  = Mathf.Clamp01(charge);
                    }

                    if (!isGrounded && isLaunched)
                    {
                        // Knocked airborne — hand off to Launched state
                        currentForceDirection.y = launchForce;
                        jumpState = JumpState.Launched;
                    }
                    else if (jumpUp && isGrounded && !isLaunched)
                    {
                        // Charge-jump: uncharged = 1× height, full charge = 2× height
                        ExecuteJump();
                    }
                    else if (jumpDown && !isGrounded && _coyoteFrame > 0 && !isLaunched)
                    {
                        // Coyote jump: pressed during the grace window after walking off a ledge
                        ExecuteJump();
                    }
                    else if (!isGrounded && !isLaunched)
                    {
                        // Walked off a ledge — burn the coyote window then fall
                        if (_coyoteFrame > 0) _coyoteFrame--;
                        else                  jumpState = JumpState.Falling;
                    }
                }
                else
                {
                    if (!isGrounded) jumpState = JumpState.Falling;
                }
                break;

            // ── Jumping (rising arc) ──────────────────────────────────
            case JumpState.Jumping:
                // Variable jump height: release early to cut the arc short
                if (jumpUp && currentForceDirection.y > MinJumpVelocity)
                    currentForceDirection.y = MinJumpVelocity;

                // Apex hang: soften gravity near the peak when button is held
                bool inApex = jumpHeld && Mathf.Abs(currentForceDirection.y) < ApexThreshold;
                if (inApex) _hadApexHang = true;
                float applyGravity = inApex ? gravity * ApexGravityScale : gravity;

                currentForceDirection.y -= applyGravity * Time.fixedDeltaTime;

                if (currentForceDirection.y < 0) jumpState = JumpState.Falling;
                break;

            // ── Launched (hit-launched upward arc) ────────────────────
            case JumpState.Launched:
                currentForceDirection.y -= gravity * Time.fixedDeltaTime;
                if (currentForceDirection.y < 0)
                {
                    currentForceDirection.y = 0;
                    isLaunched = false;
                    jumpState = JumpState.Falling;
                }
                break;

            // ── Falling ───────────────────────────────────────────────
            case JumpState.Falling:
                float fallScale = _hadApexHang ? PostApexGravityScale : 3f;
                currentForceDirection.y -= fallScale * gravity * Time.fixedDeltaTime;

                if (currentForceDirection.y > 0) jumpState = JumpState.Launched;

                if (isGrounded)
                {
                    // Touch-down — reset vertical state
                    currentForceDirection.y = 0f;
                    charge    = 0f;
                    jumpState = JumpState.Grounded;

                    // Consume jump buffer: pressed just before landing → jump immediately
                    if (_jumpBufferFrame > 0) ExecuteJump();
                }
                break;
        }

        // Clear flags — consumed for this tick
        _jumpPressed  = false;
        _jumpReleased = false;

        return currentForceDirection.y;
    }

    /// <summary>
    /// Shared launch logic used by normal jump, coyote jump, and jump buffer.
    /// charge 0 = 1× height (same as old max-charge jump).
    /// charge 1 = 2× height (fully charged).
    /// Always at least MinJumpVelocity so an instant release still hops.
    /// </summary>
    private void ExecuteJump()
    {
        currentForceDirection.y = Mathf.Max((1f + charge) * height, MinJumpVelocity);
        charge           = 0f;
        _jumpBufferFrame = 0;
        _coyoteFrame     = 0;
        _hadApexHang     = false;
        jumpState        = JumpState.Jumping;
    }
    void GroundDetection()
    {
        // Sphere origin: feet position shifted up by the radius so the bottom
        // of the sphere sits exactly at the feet. Cast straight down in world space.
        Vector3 origin = character.transform.position + offset + Vector3.up * groundSphereRadius;

        // QueryTriggerInteraction.Ignore excludes AuraFields, hitboxes, and other
        // triggers — every solid collider (ground, other players, props) counts.
        isGrounded = Physics.SphereCast(
            origin, groundSphereRadius, Vector3.down,
            out hit, groundCheckDist,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        if (debugLogs) Debug.Log($"Ground check origin: {origin}  grounded: {isGrounded}  surface: {(isGrounded ? hit.collider.name : "none")}");

        // Debug visualisation
        if (isGrounded)
            Debug.DrawRay(origin, Vector3.down * (hit.distance + groundSphereRadius), Color.red);
        else
            Debug.DrawRay(origin, Vector3.down * (groundCheckDist + groundSphereRadius), Color.blue);
    }

    public void OnHitConfirm((Collider hitbox, Collider hurtbox) hitInfo)
    {
        if (isProne)
        {
            _isHitConfirmPause = true;        
            currentRoation.y = rb.transform.eulerAngles.y;
        }

        //bool isAttacked = character.gameObject.GetInstanceID() == hitInfo.hurtbox.gameObject.GetInstanceID();
        //bool isAttacking = character.gameObject.GetInstanceID() == hitInfo.hitBox.transform.parent.gameObject.GetInstanceID();

        //if (isAttacking)
        //{
        //    Debug.Log($"{rb.transform.parent.name} hit {hitInfo.hurtbox.transform.parent.name}");
        //}
        //else
        //{
        //    Debug.Log($"{rb.transform.parent.name} has been hit by {hitInfo.hitBox.transform.parent.parent.name}");
        //}


    }

    public void OnHitConfirmPauseEnd((Collider hitbox, Collider hurtbox) hitInfo)
    {
        //Debug.Log($"_isHitConfirmPause Before: {_isHitConfirmPause}");
        _isHitConfirmPause = false;
        //Debug.Log($"_isHitConfirmPause After: {_isHitConfirmPause}");
        bool isAttacking = character.gameObject.GetInstanceID() == hitInfo.hitbox.transform.parent.gameObject.GetInstanceID();

        if (isAttacking)
        {
            //Debug.Log($"{rb.transform.parent.name} hit {hitInfo.hurtbox.transform.parent.name}");

        }

    }

    public void OnPush(Vector3 direction)
    {
        if (!isInvulnerabilityActive)
        {
            forceDirection.x = currentForceDirection.x = direction.x;
            forceDirection.z = currentForceDirection.z = direction.z;
            launchForce = direction.y;
            Debug.Log($"direction: {direction}");
            Debug.Log($"currentForceDirection: {currentForceDirection}");
            isPushed = true;
            isLaunched = true;
            isZero = (false, false);
        }


    }
    void OnInvulnerabilityActive(bool isActive)
    {
        isInvulnerabilityActive = isActive;
    }

    private void OnProneActive(bool isActive)
    {
        isProne = isActive;
        if (isProne)
        {
            fallStartTime = Time.time;
            fallStartRotation = rb.transform.eulerAngles;
            currentRoation.y = rb.transform.eulerAngles.y;
        }
        else
        {
            currentRoation.y = rb.transform.eulerAngles.y;
            startTime = Time.time;
        }
    }

    void OnAttackRotate(Quaternion targetRotation, float arcThreshold)
    {
        attackSnapTarget = targetRotation;
        _snapThreshold   = arcThreshold;
        isAttackSnapping = true;
    }

    void OnAttackEnd()
    {
        isAttackSnapping = false;
    }

    public void DrawGizmos()
    {
        if (rb == null || !isAttackSnapping) return;

        Vector3 origin     = rb.position + Vector3.up * 0.5f;
        Vector3 currentFwd = rb.rotation      * Vector3.forward;
        Vector3 targetFwd  = attackSnapTarget * Vector3.forward;
        const float len = 2.5f;

        // Current facing — white
        Gizmos.color = new Color(1f, 1f, 1f, 0.7f);
        Gizmos.DrawRay(origin, currentFwd * len);

        // Target facing — green with arrowhead
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.9f);
        Gizmos.DrawRay(origin, targetFwd * len);
        Vector3 tip        = origin + targetFwd * len;
        Vector3 arrowLeft  = Quaternion.Euler(0, -145f, 0) * targetFwd;
        Vector3 arrowRight = Quaternion.Euler(0,  145f, 0) * targetFwd;
        float   arrowSize  = 0.25f;
        Gizmos.DrawLine(tip, tip + arrowLeft  * arrowSize);
        Gizmos.DrawLine(tip, tip + arrowRight * arrowSize);

        // Angle delta label
#if UNITY_EDITOR
        float delta = Quaternion.Angle(rb.rotation, attackSnapTarget);
        UnityEditor.Handles.Label(origin + Vector3.up * 0.4f,
            $"Snapping  {delta:F1}° remaining");
#endif
    }
}
