using System;
using System.Collections.Generic;
using TMPro.Examples;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.HighDefinition;
using static UnityEngine.GraphicsBuffer;
using UnityEngine.UI;
using System.Text;
using UnityEngine.TextCore.Text;
using UnityEditor;

[Serializable]
public class CameraControler
{
    // ── References ────────────────────────────────────────────────────────────
    [Header("References")]
    [Tooltip("The gated input wrapper assigned to this camera.")]
    public PlayerInput GamePad;

    [Tooltip("The Camera component attached to the generated camera GameObject.")]
    public Camera camera;

    [Tooltip("The cursor GameObject shown over a locked-on target.")]
    public GameObject cursor;

    [Tooltip("The display name used to label this camera in the hierarchy.")]
    public string PlayerName;

    // ── Runtime Objects (set at runtime, do not assign manually) ──────────────
    [Header("Runtime Objects")]
    [Tooltip("The GameObject that carries the Camera component. Created automatically on Initialize.")]
    public GameObject cameraObject;

    [Tooltip("A helper transform used to drive the camera's horizontal facing direction. Created automatically on Initialize.")]
    public GameObject cameraLocation;

    [Tooltip("The orbit/follow anchor that the camera orbits around or snaps to. Created automatically on Initialize.")]
    public GameObject cameraAnchor;

    [Tooltip("The last enemy/player GameObject hit by the camera's targeting raycast.")]
    public GameObject cameraTarget;

    // ── Orbit Camera — Orbit Shape ─────────────────────────────────────────────
    [Header("Orbit Camera — Orbit Shape")]
    [Tooltip("Current horizontal position component of the camera (calculated, do not set manually).")]
    public float x;

    [Tooltip("Current depth position component of the camera (calculated, do not set manually).")]
    public float y;

    [Tooltip("Unused legacy height field — orbit vertical position is driven by pitch instead.")]
    public float height = 3f;

    [Tooltip("Distance from the orbit anchor to the camera. Increase to zoom out.")]
    public float radius = 7f;

    [Tooltip("Current horizontal orbit angle in degrees (calculated at runtime).")]
    public float degrees = 0f;

    [Tooltip("Rotational offset applied to the orbit angle so 0° places the camera behind the player.")]
    public float degreeOffset = 90f;

    // ── Orbit Camera — Pitch (Vertical Tilt) ──────────────────────────────────
    [Header("Orbit Camera — Pitch")]
    [Tooltip("Current vertical angle of the orbit camera in degrees. 0 = horizon level, positive = looking from above.")]
    public float pitch = 20f;

    [Tooltip("Minimum pitch angle. Negative values allow the camera to dip below the horizon.")]
    public float minPitch = -5f;

    [Tooltip("Maximum pitch angle. Prevents the camera from flipping over the top of the player.")]
    public float maxPitch = 75f;

    // ── Orbit Camera — Input Speeds ────────────────────────────────────────────
    [Header("Orbit Camera — Input Speeds")]
    [Tooltip("Degrees per frame the orbit angle moves when the right stick is pushed horizontally.")]
    public float horizontalOrbitSpeed = 5f;

    [Tooltip("Degrees per frame the pitch moves when the right stick is pushed vertically.")]
    public float verticalOrbitSpeed = 2f;

    // ── Orbit Camera — Anchor Follow ───────────────────────────────────────────
    [Header("Orbit Camera — Anchor Follow")]
    [Tooltip("How far the player can move in front of the anchor before it starts catching up (world units).")]
    public float frontThreshold = 3f;

    [Tooltip("How far the player can move behind the anchor before it starts catching up (world units).")]
    public float backThreshold = 3f;

    [Tooltip("Distance at which the anchor stops lerping and snaps to the player position.")]
    public float deadzone = 0.05f;

    [Tooltip("Speed at which the orbit anchor lerps toward the player when outside the front/back threshold.")]
    public float anchorLerpSpeed = 3.5f;

    // ── Orbit Camera — Directional Auto-Follow ─────────────────────────────────
    [Header("Orbit Camera — Directional Auto-Follow")]
    [Tooltip("Degrees per second the orbit auto-rotates to sit behind the player when the left stick is pushed horizontally and the right stick is idle.")]
    public float directionalFollowSpeed = 90f;

    // ── Orbit Camera — Look Offset ─────────────────────────────────────────────
    [Header("Orbit Camera — Look Offset")]
    [Tooltip("Shifts the orbit look target left/right (camera-relative). Use to frame the player off-centre.")]
    public float lookOffsetX = 0f;

    [Tooltip("Shifts the orbit look target up/down (camera-relative). Use to raise or lower the focal point.")]
    public float lookOffsetY = 0f;

    // ── Follow Camera — Position ───────────────────────────────────────────────
    [Header("Follow Camera — Position")]
    [Tooltip("Vertical height of the follow camera above the player's origin.")]
    public float sholderHeight = 1.74f;

    [Tooltip("Distance behind the player the follow camera sits. Negative = behind.")]
    public float sholderDistance = -5.25f;

    [Tooltip("Horizontal side offset of the follow camera from the player's centre. Positive = right shoulder.")]
    public float sholderOffset = 2.19f;

    // ── Follow Camera — Vertical Tilt ─────────────────────────────────────────
    [Header("Follow Camera — Vertical Tilt")]
    [Tooltip("Current vertical tilt of the follow camera in degrees. Positive = looking up, negative = looking down. Returns to 0 when the stick is released.")]
    public float followPitch = 0f;

    [Tooltip("Maximum downward tilt for the follow camera (degrees, use a negative value).")]
    public float followMinPitch = -30f;

    [Tooltip("Maximum upward tilt for the follow camera (degrees).")]
    public float followMaxPitch = 45f;

    [Tooltip("Degrees per second the follow camera tilts while the right stick Y is held.")]
    public float followTiltSpeed = 60f;

    [Tooltip("Degrees per second the follow camera returns to centre after the right stick Y is released.")]
    public float followTiltReturnSpeed = 120f;

    // ── Follow Camera — Aim Lock ───────────────────────────────────────────────
    [Header("Follow Camera — Aim Lock")]
    [Tooltip("Maximum horizontal rotation (degrees) the camera can pan left/right from the locked forward direction while R1 is held.")]
    public float aimHorizontalLimit = 60f;

    [Tooltip("Maximum vertical rotation (degrees) the camera can tilt up/down from the locked pitch while R1 is held.")]
    public float aimVerticalLimit = 30f;

    [Tooltip("Degrees per second the right stick rotates the camera during aim lock.")]
    public float aimRotateSpeed = 90f;

    [Tooltip("Half-width of the viewport centre zone (0–0.5). Players whose viewport X is within 0.5 ± this value are considered targeted.")]
    public float aimCenterX = 0.07f;

    [Tooltip("Half-height of the viewport centre zone (0–0.5). Players whose viewport Y is within 0.5 ± this value are considered targeted.")]
    public float aimCenterY = 0.15f;

    // ── Targeting — Orbit Mode ─────────────────────────────────────────────────
    [Header("Targeting — Orbit Mode")]
    [Tooltip("Maximum world-unit distance from the owner within which players can be locked on. Players beyond this radius are ignored entirely.")]
    public float targetingRange = 20f;

    [Tooltip("Degrees per second the orbit camera auto-rotates to frame both the owner and the locked target.")]
    public float targetingOrbitSpeed = 120f;

    [Tooltip("Fraction of the viewport (0–0.5) inset from each edge where the off-screen cursor appears.")]
    public float offScreenEdgePadding = 0.05f;

    [Tooltip("Maximum seconds between two R1 taps to register as a double-tap cancel.")]
    public float doubleTapInterval = 0.35f;

    [Tooltip("Right-stick X dead-zone used when cycling through targets.")]
    public float cycleDzone = 0.3f;

    [Tooltip("Seconds of unbroken line-of-sight obstruction before the lock is automatically cancelled. Set to 0 to disable.")]
    public float losTimeout = 2f;

    // ── Targeting — Combat Framing ─────────────────────────────────────────────
    [Header("Targeting — Combat Framing")]
    [Tooltip("Degrees added to the right of the normal targeting orbit angle when inside attack range.")]
    public float attackViewAngleOffset = 60f;

    [Tooltip("Degrees per second the combat framing offset lerps in and out.")]
    public float attackViewLerpSpeed = 90f;

    // ── State ──────────────────────────────────────────────────────────────────
    [Header("State")]
    [Tooltip("When true the camera is in Follow (over-the-shoulder) mode. When false it is in Orbit mode. Toggled at runtime by pressing the Left Stick Button.")]
    public bool isSwitched = false;

    // ── Side View Camera ───────────────────────────────────────────────────────
    [Header("Side View Camera")]
    [Tooltip("Height above the players' midpoint the camera sits.")]
    public float sideViewHeight = 2f;

    [Tooltip("Camera pull-back multiplier relative to the separation between the two players.")]
    public float sideViewDistanceMultiplier = 0.8f;

    [Tooltip("Minimum camera distance from the midpoint regardless of player separation.")]
    public float sideViewMinDistance = 5f;

    [Tooltip("Maximum camera distance from the midpoint regardless of player separation.")]
    public float sideViewMaxDistance = 22f;

    [Tooltip("How quickly the camera position smooths toward its target each frame.")]
    public float sideViewSmoothing = 6f;

    [Tooltip("Field of view used in side-view mode.")]
    public float sideViewFOV = 55f;

    [Tooltip("Viewport Y fraction (0–1, from bottom) the camera targets for the lower player's feet. " +
             "Must clear the name label, which tops out at ~0.274 of viewport height " +
             "(padB+pieS+2×barH+labelGap+labelH = 148s at s=vp.height/540). " +
             "Default 0.32 puts the feet ~5% above the label for a clean gap. " +
             "Raise if players still overlap the label; lower to allow them to sit closer to it.")]
    public float sideViewHUDClearance = 0.32f;

    [Tooltip("How quickly the vertical framing correction smooths toward the target each frame. " +
             "Higher = snappier; lower = more gradual.")]
    public float sideViewFramingSpeed = 4f;

    // ── Debug ──────────────────────────────────────────────────────────────────
    [Header("Debug")]
    [Tooltip("When enabled, draws a live stat readout in the top-left corner of this player's viewport.")]
    public bool showDebugStats = true;

    [Tooltip("When enabled, draws the aim-lock centre detection box in the game view.")]
    public bool showAimCenterBox = true;

    [Tooltip("Optional test object repositioned every frame to TestVector (for prototype / debugging use only).")]
    public GameObject TestObject;

    [Tooltip("World position TestObject is moved to each frame when assigned.")]
    public Vector3 TestVector = new Vector3(0, 0, -3);

    // ── Private state ──────────────────────────────────────────────────────────
    private Transform playerTransform;
    private bool isAnchorLerping = false;
    private bool isHit;

    // Cursor symbol — uses the existing MeshRenderer with the SymbolGlow shader
    // (SpriteRenderer is incompatible with HDRP and renders invisible)
    private MeshRenderer          _cursorMeshRenderer;
    private MaterialPropertyBlock _cursorMpb;

    // Cached shader property IDs — resolved once, avoids per-frame string hashing
    private static readonly int _ID_BaseColorMap  = Shader.PropertyToID("_BaseColorMap");
    private static readonly int _ID_BaseColor     = Shader.PropertyToID("_BaseColor");
    private static readonly int _ID_EmissiveColor = Shader.PropertyToID("_EmissiveColor");
    private static readonly int _ID_GlowIntensity = Shader.PropertyToID("_GlowIntensity");

    // Cached GUI styles — built once on first DrawDebugStats call, reused every frame
    private GUIStyle _guiHeader;
    private GUIStyle _guiDivider;
    private GUIStyle _guiName;
    private GUIStyle _guiValue;

    // Cached 1×1 texture used to draw the aim-centre-box overlay
    private Texture2D _aimBoxTex;

    private bool isInitialized = false;
    public bool IsInitialized { get { return isInitialized; } }

    CameraStateWrapper cameraStateWapper = new CameraStateWrapper();
    private bool _isHitConfirmPause;
    private PlayerEvents playerEvents;

    // ── Side-view private state ───────────────────────────────────────────────
    private bool    _isSideView           = false;
    private int     _sideViewSign         = 1;        // which side of the fight axis the camera is on
    private Vector3 _sideViewSmoothPos;               // lerped camera position
    private Vector3 _lastFightAxis        = Vector3.right; // previous frame fight axis for side-swap detection
    private float   _defaultFOV          = 60f;
    private float   _sideViewLookOffsetY  = 0f;       // smoothed look-target Y offset for HUD framing

    // ── Follow aim-lock private state ─────────────────────────────────────────
    private bool               _isFollowAimLock    = false;
    private float              _aimLockYaw         = 0f;   // world-space yaw saved when aim entered
    private float              _aimLockPitch       = 0f;   // followPitch value saved when aim entered
    private float              _aimYawOffset       = 0f;   // right-stick horizontal deviation, ±aimHorizontalLimit
    private float              _aimPitchOffset     = 0f;   // right-stick vertical deviation,   ±aimVerticalLimit
    private LocalPlayerManager _followAimTarget    = null; // player currently in the centre zone
    private Renderer[]         _followAimRenderers = null;

    // ── Orbit targeting private state ─────────────────────────────────────────
    private LocalPlayerManager              _owner           = null;
    private bool                            _isTargeting     = false;
    private bool                            _isR1Held        = false;   // R1 currently held down
    private LocalPlayerManager              _currentTarget   = null;
    private readonly List<LocalPlayerManager> _sortedTargets = new List<LocalPlayerManager>();
    private int                             _targetIndex     = 0;
    private float                           _lastR1TapTime   = -999f;  // time of most recent R1 press
    private bool                            _stickCycleReady = true;
    private Renderer[]                      _targetRenderers = null;
    private MaterialPropertyBlock           _mpb;             // created in Initialize — not a field initializer
    private float                           _losTimer        = 0f;     // accumulates while LOS is blocked
    private readonly RaycastHit[]           _losHitBuffer    = new RaycastHit[32]; // pre-alloc, avoids GC
    private float                           _attackViewOffset = 0f;   // current combat-framing degree offset

public void OnUpdate()
    {
        cameraStateWapper.CurrentState = CameraStateWrapper.CameraState.Orbit;
        if (isInitialized)
        {
            if (GamePad.GetButtonDown("D-Pad Down"))
            {
                isSwitched = !isSwitched;
                if (!isSwitched && _isFollowAimLock) ExitFollowAimLock();
                if ( isSwitched && _isTargeting)     ExitTargeting();
                if (_isSideView)                     ExitSideView();
            }

            // D-Pad Right — toggle FightingSide view (requires a target)
            if (GamePad.GetButtonDown("D-Pad Right"))
            {
                if (_isSideView) ExitSideView();
                else             EnterSideView(); // no-op if not targeting
            }

            // FightingSide overrides the normal Orbit/Follow state
            if (_isSideView)
            {
                cameraStateWapper.CurrentState = CameraStateWrapper.CameraState.FightingSide;
                // Auto-exit if target is gone
                if (_currentTarget == null || _currentTarget.character == null)
                    ExitSideView();
            }
            else
            {
                cameraStateWapper.CurrentState = isSwitched
                    ? CameraStateWrapper.CameraState.Follow
                    : CameraStateWrapper.CameraState.Orbit;
            }

            // Targeting state machine only runs in Orbit mode
            if (cameraStateWapper.CurrentState == CameraStateWrapper.CameraState.Orbit)
                UpdateTargeting();

            if (cameraObject != null)
            {
                switch (cameraStateWapper.CurrentState)
                {
                    case CameraStateWrapper.CameraState.Orbit:
                        if (_isTargeting)
                            UpdateOffScreenIndicator();
                        else if (cursor != null)
                            cursor.SetActive(false);
                        Orbit();
                        break;

                    case CameraStateWrapper.CameraState.Follow:
                        if (_isFollowAimLock)
                            UpdateFollowAimCursor();
                        else if (cursor != null)
                            cursor.SetActive(false);
                        Follow();
                        break;

                    case CameraStateWrapper.CameraState.FightingSide:
                        if (cursor != null) cursor.SetActive(false);
                        FightingSide();
                        break;
                }
            }
        }
    }


    public void Orbit()
    {
        // Anchor always follows the owner player — no conflict because
        // UpdateTargetingOrbit only modifies 'degrees', not the anchor.
        UpdateAnchor();

        if (_isTargeting && _isR1Held)
        {
            // Auto-rotate the orbit angle so the camera sits behind the owner
            // with the target visible ahead. Right-stick X cycles targets.
            UpdateTargetingOrbit();
        }
        else if (_isTargeting)
        {
            // Locked but R1 released: full manual orbit with both axes,
            // same as normal orbit. Right-stick X cycling is only active while R1 is held.
            UpdateDirectionalFollow();
            degrees += (Mathf.Abs(GamePad.GetAxis("Right Stick X")) > 0.2f)
                ? -GamePad.GetAxis("Right Stick X") * horizontalOrbitSpeed : 0;
        }
        else
        {
            // Normal orbit: directional auto-follow + manual right-stick X rotation.
            UpdateDirectionalFollow();
            degrees += (Mathf.Abs(GamePad.GetAxis("Right Stick X")) > 0.2f)
                ? -GamePad.GetAxis("Right Stick X") * horizontalOrbitSpeed : 0;
        }

        degrees = degrees % 360;

        pitch += (Mathf.Abs(GamePad.GetAxis("Right Stick Y")) > 0.2f) ? -GamePad.GetAxis("Right Stick Y") * verticalOrbitSpeed : 0;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        float pitchRad = pitch * Mathf.Deg2Rad;
        float horizontalRadius = Mathf.Abs(radius) * Mathf.Cos(pitchRad);
        float verticalOffset = Mathf.Abs(radius) * Mathf.Sin(pitchRad);

        y = horizontalRadius * (float)Math.Sin((degrees - degreeOffset) * Mathf.Deg2Rad);
        x = horizontalRadius * (float)Math.Cos((degrees - degreeOffset) * Mathf.Deg2Rad);

        cameraLocation.transform.position = new Vector3(x, verticalOffset, y) + cameraAnchor.transform.position;
        cameraObject.transform.position = cameraLocation.transform.position;
        Vector3 relativeCameraAnchorPosition = camera.transform.InverseTransformPoint(cameraAnchor.transform.localPosition);
        Vector3 lookTarget = cameraAnchor.transform.position
            + camera.transform.right * lookOffsetX
            + camera.transform.up    * lookOffsetY;
        camera.transform.LookAt(lookTarget, Vector3.up);
        cameraLocation.transform.LookAt(lookTarget, Vector3.up);
        cameraLocation.transform.eulerAngles = new Vector3(0, camera.transform.eulerAngles.y, camera.transform.eulerAngles.z);
    }

public void Follow()
    {
        // Aim-lock: enter on R1 press, exit on R1 release
        if (GamePad.GetButtonDown("Right Shoulder"))    EnterFollowAimLock();
        else if (GamePad.GetButtonUp("Right Shoulder")) ExitFollowAimLock();

        cameraAnchor.transform.position = playerTransform.position;

        if (_isFollowAimLock)
        {
            // Right stick X rotates camera view left/right within ±aimHorizontalLimit
            float stickX = GamePad.GetAxis("Right Stick X");
            if (Mathf.Abs(stickX) > deadzone)
            {
                _aimYawOffset += stickX * aimRotateSpeed * Time.deltaTime;
                _aimYawOffset  = Mathf.Clamp(_aimYawOffset, -aimHorizontalLimit, aimHorizontalLimit);
            }

            // Right stick Y tilts camera view up/down within ±aimVerticalLimit
            float stickY = GamePad.GetAxis("Right Stick Y");
            if (Mathf.Abs(stickY) > deadzone)
            {
                _aimPitchOffset += stickY * aimRotateSpeed * Time.deltaTime;
                _aimPitchOffset  = Mathf.Clamp(_aimPitchOffset, -aimVerticalLimit, aimVerticalLimit);
            }

            float finalYaw   = _aimLockYaw  + _aimYawOffset;
            float finalPitch = _aimLockPitch + _aimPitchOffset;

            // Anchor stays locked to the player's forward — camera position never orbits.
            // Only the camera rotation changes, giving a pan/tilt feel.
            cameraAnchor.transform.forward  = playerTransform.forward;
            cameraObject.transform.position = cameraAnchor.transform.TransformPoint(
                new Vector3(sholderOffset, sholderHeight, sholderDistance));
            cameraObject.transform.rotation = Quaternion.Euler(-finalPitch, finalYaw, 0f);

            UpdateFollowAimTarget();
        }
        else
        {
            // Normal follow branch
            cameraAnchor.transform.forward = playerTransform.forward;
            cameraObject.transform.position = cameraAnchor.transform.TransformPoint(
                new Vector3(sholderOffset, sholderHeight, sholderDistance));

            float rightStickY = GamePad.GetAxis("Right Stick Y");
            if (Mathf.Abs(rightStickY) > deadzone)
            {
                followPitch += rightStickY * followTiltSpeed * Time.deltaTime;
                followPitch  = Mathf.Clamp(followPitch, followMinPitch, followMaxPitch);
            }
            else
            {
                followPitch = Mathf.MoveTowards(followPitch, 0f, followTiltReturnSpeed * Time.deltaTime);
            }

            cameraObject.transform.rotation = Quaternion.Euler(-followPitch, cameraAnchor.transform.eulerAngles.y, 0f);
        }

        if (TestObject != null) TestObject.transform.position = TestVector;
    }

void EnterFollowAimLock()
    {
        if (_isFollowAimLock) return;
        _isFollowAimLock    = true;
        cameraStateWapper.IsFollowAimLock = true;
        _aimLockYaw         = playerTransform.eulerAngles.y;
        _aimLockPitch       = followPitch;
        _aimYawOffset       = 0f;
        _aimPitchOffset     = 0f;
        _followAimTarget    = null;
        _followAimRenderers = null;
    }

void ExitFollowAimLock()
    {
        if (!_isFollowAimLock) return;
        ClearFollowAimHighlight();
        _isFollowAimLock    = false;
        cameraStateWapper.IsFollowAimLock = false;
        _followAimTarget    = null;
        _followAimRenderers = null;
        followPitch         = Mathf.Clamp(_aimLockPitch + _aimPitchOffset, followMinPitch, followMaxPitch);
        _aimYawOffset       = 0f;
        _aimPitchOffset     = 0f;
        // Clear shared targeting state so TeamController knows aim-lock ended
        cameraTarget = null;
        isHit        = false;
        playerEvents.OnOrbitTargetChanged?.Invoke(null, false);
    }

void UpdateFollowAimTarget()
    {
        LocalPlayerManager newTarget = null;
        float closestDist = float.MaxValue;
        foreach (LocalPlayerManager p in LocalPlayerManager.ActivePlayers)
        {
            if (p == _owner || p.character == null) continue;
            Vector3 vp = camera.WorldToViewportPoint(p.character.transform.position + Vector3.up * 1f);
            if (vp.z <= 0f) continue;
            if (Mathf.Abs(vp.x - 0.5f) > aimCenterX) continue;
            if (Mathf.Abs(vp.y - 0.5f) > aimCenterY) continue;
            float dist = vp.z;
            if (dist < closestDist)
            {
                closestDist = dist;
                newTarget   = p;
            }
        }
        if (newTarget != _followAimTarget)
        {
            ClearFollowAimHighlight();
            _followAimTarget = newTarget;
            if (_followAimTarget != null)
            {
                _followAimRenderers = _followAimTarget.character.GetComponentsInChildren<Renderer>();
                ApplyFollowAimHighlight();
            }
            // Keep cameraTarget, isHit, and TeamController in sync with follow aim-lock target
            cameraTarget = _followAimTarget?.character;
            isHit        = _followAimTarget != null;
            playerEvents.OnOrbitTargetChanged?.Invoke(_followAimTarget, _followAimTarget != null);
            // Show the new target's symbol (or their leader's if they're a follower)
            if (_followAimTarget != null)
                SetCursorSymbol(_followAimTarget.ActiveSymbol);
        }
    }

void UpdateFollowAimCursor()
    {
        if (_followAimTarget == null || _followAimTarget.character == null)
        {
            if (cursor != null) cursor.SetActive(false);
            return;
        }
        if (cursor != null)
        {
            // Poll every frame to catch team-status changes mid-aim
            SetCursorSymbol(_followAimTarget.ActiveSymbol);
            cursor.SetActive(true);
            cursor.transform.position   = _followAimTarget.character.transform.position + CursorOffset;
            cursor.transform.localScale = Vector3.one * CursorScale;
            // Billboard: face the symbol toward this player's camera
            cursor.transform.rotation = Quaternion.LookRotation(cameraObject.transform.forward, Vector3.up);
        }
    }

void ApplyFollowAimHighlight()
    {
        if (_followAimRenderers == null) return;
        Color glow = Color.cyan * 3f;
        foreach (Renderer r in _followAimRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor("_EmissionColor", glow);
            r.SetPropertyBlock(_mpb);
        }
    }

void ClearFollowAimHighlight()
    {
        if (_followAimRenderers == null) return;
        foreach (Renderer r in _followAimRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor("_EmissionColor", Color.black);
            r.SetPropertyBlock(_mpb);
        }
        _followAimRenderers = null;
    }






    // ══════════════════════════════════════════════════════════════════════════
    //  Targeting System — Orbit Mode
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Main targeting state machine — called every LateUpdate while in Orbit mode.
    ///
    /// • Press R1          → enter targeting (lock on nearest player).
    /// • Hold R1           → camera auto-orbits to frame both players.
    /// • Release R1        → auto-orbit stops; lock (glow + cursor) stays active.
    /// • Double-tap R1     → cancel targeting entirely (two presses within doubleTapInterval).
    /// </summary>
    void UpdateTargeting()
    {
        bool r1Down    = GamePad.GetButtonDown("Right Shoulder");
        bool r1Up      = GamePad.GetButtonUp("Right Shoulder");
        _isR1Held      = GamePad.GetButton("Right Shoulder");

        // When R1 is released, reset the cycle gate so the next hold starts fresh
        // regardless of where the stick is sitting.
        if (r1Up) _stickCycleReady = false;

        if (r1Down)
        {
            float elapsed = Time.time - _lastR1TapTime;
            _lastR1TapTime = Time.time;

            if (_isTargeting && elapsed <= doubleTapInterval)
            {
                // ── Double-tap while locked on → hard cancel ──────────────────
                ExitTargeting();
                return;
            }

            if (!_isTargeting)
            {
                // ── First press → enter targeting ─────────────────────────────
                EnterTargeting();
            }
        }

        if (_isTargeting)
        {
            // Once locked, range no longer matters — the lock holds at any distance.
            // Rebuild only if the list became empty because a player left the match.
            if (_sortedTargets.Count == 0) BuildSortedTargets();

            CheckLineOfSight();

            // Right stick X cycles targets only while R1 is held.
            // When R1 is released the stick is returned to normal orbit control.
            if (_isR1Held) UpdateTargetCycling();
        }
    }

    /// <summary>
    /// Checks line-of-sight from the owner to the current target each frame.
    /// Casts against all non-trigger colliders and skips any hit that belongs
    /// to the owner's or target's own character hierarchy, so internal colliders
    /// (hitboxes, bones, aura fields) never cause a false positive.
    /// If something genuinely blocks the path for losTimeout seconds the lock cancels.
    /// Timer resets the moment LOS is restored.
    /// </summary>
    void CheckLineOfSight()
    {
        if (losTimeout <= 0f || _currentTarget == null || _currentTarget.character == null)
        {
            _losTimer = 0f;
            return;
        }

        Vector3 from = playerTransform.position          + Vector3.up * 1.2f;
        Vector3 to   = _currentTarget.character.transform.position + Vector3.up * 1.2f;
        Vector3 dir  = to - from;
        float   dist = dir.magnitude;

        if (dist < 0.01f) { _losTimer = 0f; return; }

        // Cast against every layer, ignore triggers, collect all hits into the buffer.
        int hitCount = Physics.RaycastNonAlloc(
            from, dir / dist, _losHitBuffer, dist,
            Physics.AllLayers, QueryTriggerInteraction.Ignore);

        // A hit only counts as a block if it isn't part of the owner or target hierarchy.
        Transform ownerRoot  = playerTransform;
        Transform targetRoot = _currentTarget.character.transform;

        bool blocked = false;
        for (int i = 0; i < hitCount; i++)
        {
            Transform t = _losHitBuffer[i].transform;
            if (t.IsChildOf(ownerRoot)  || t == ownerRoot)  continue;
            if (t.IsChildOf(targetRoot) || t == targetRoot) continue;
            blocked = true;
            break;
        }

        if (blocked)
        {
            _losTimer += Time.deltaTime;
            if (_losTimer >= losTimeout)
                ExitTargeting();
        }
        else
        {
            _losTimer = 0f;
        }
    }

    void EnterTargeting()
    {
        BuildSortedTargets();

        // Only enter targeting if at least one player is within range.
        // Pressing R1 with nobody in range does nothing.
        if (_sortedTargets.Count == 0) return;

        _isTargeting     = true;
        _targetIndex     = 0;
        _stickCycleReady = true;
        SetCurrentTarget(_sortedTargets[0]);
    }

void ExitTargeting()
    {
        _isTargeting      = false;
        _losTimer         = 0f;
        _attackViewOffset = 0f;
        ClearTargetHighlight();
        _currentTarget = null;
        _sortedTargets.Clear();
        // Clear shared targeting state so all consumers know the lock ended
        cameraTarget = null;
        isHit        = false;
        playerEvents.OnOrbitTargetChanged?.Invoke(null, false);
    }

public void EnterSideView()
    {
        if (_isSideView || !_isTargeting || _currentTarget == null) return;
        _isSideView   = true;
        _sideViewSign = -1;

        // Seed last fight axis from the real current direction so the first
        // frame of FightingSide() never triggers a spurious side-swap.
        Vector3 playerPos = playerTransform.position;
        Vector3 targetPos = _currentTarget.character.transform.position;
        Vector3 toTarget  = new Vector3(targetPos.x - playerPos.x, 0f, targetPos.z - playerPos.z);
        _lastFightAxis = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : Vector3.forward;

        if (camera != null) { _defaultFOV = camera.fieldOfView; camera.fieldOfView = sideViewFOV; }
        if (cameraObject != null) _sideViewSmoothPos = cameraObject.transform.position;
    }

public void ExitSideView()
    {
        if (!_isSideView) return;
        _isSideView          = false;
        _sideViewLookOffsetY = 0f;
        if (camera != null) camera.fieldOfView = _defaultFOV;
    }

void FightingSide()
    {
        if (_currentTarget == null || _currentTarget.character == null) { ExitSideView(); return; }

        Vector3 playerPos = playerTransform.position;
        Vector3 targetPos = _currentTarget.character.transform.position;

        // Flat fight axis: direction from owner toward opponent
        Vector3 toTarget  = new Vector3(targetPos.x - playerPos.x, 0f, targetPos.z - playerPos.z);
        if (toTarget.sqrMagnitude < 0.01f) return;

        Vector3 fightAxis  = toTarget.normalized;
        float   separation = toTarget.magnitude;

        // Side-swap: if fight axis reversed (players crossed), flip camera side
        if (Vector3.Dot(fightAxis, _lastFightAxis) < 0f)
            _sideViewSign *= -1;
        _lastFightAxis = fightAxis;

        // Perpendicular direction — the side the camera sits on
        Vector3 sideDir = new Vector3(-fightAxis.z, 0f, fightAxis.x) * _sideViewSign;

        // Midpoint between players, averaging Y for vertical tracking
        Vector3 midpoint = new Vector3(
            (playerPos.x + targetPos.x) * 0.5f,
            (playerPos.y + targetPos.y) * 0.5f,
            (playerPos.z + targetPos.z) * 0.5f);

        // Distance scales with separation to keep both players in frame
        float camDist = Mathf.Clamp(separation * sideViewDistanceMultiplier,
                                    sideViewMinDistance, sideViewMaxDistance);

        Vector3 targetCamPos = midpoint + sideDir * camDist + Vector3.up * sideViewHeight;

        // Smooth camera movement
        _sideViewSmoothPos = Vector3.Lerp(_sideViewSmoothPos, targetCamPos,
                                          sideViewSmoothing * Time.deltaTime);
        cameraObject.transform.position = _sideViewSmoothPos;

        // ── Vertical HUD framing ───────────────────────────────────────────────
        // Step 1 — provisional LookAt so WorldToViewportPoint gives a valid result
        Vector3 baseLookTarget = midpoint + Vector3.up * 1.5f;
        cameraObject.transform.LookAt(baseLookTarget, Vector3.up);

        // Step 2 — measure where the lower player's feet sit in the viewport (0=bottom, 1=top).
        // Use the actual world position of the lower player so the depth calculation is
        // accurate — avoids the error of mixing midpoint X/Z with a different player's Y.
        Vector3 lowerFeetRef  = playerPos.y <= targetPos.y ? playerPos : targetPos;
        float   currentFeetVP = camera.WorldToViewportPoint(lowerFeetRef).y;

        // Step 3 — convert the viewport-Y error to a world-space look-target Y offset.
        // Moving the look target up tilts the camera up, which shifts the feet downward
        // in the frame toward the HUD line.  Formula derived from perspective projection:
        //   Δviewport ≈ –ΔlookY / (2 × dist × tan(vFOV/2))
        // => ΔlookY = (currentFeetVP – desiredFeetVP) × 2 × dist × tan(vFOV/2)
        float dist         = Vector3.Distance(_sideViewSmoothPos, baseLookTarget);
        float halfFrustumH = Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float targetLookOffsetY = (currentFeetVP - sideViewHUDClearance) * 2f * dist * halfFrustumH;

        // Step 4 — smooth the offset so corrections feel like natural camera adjustment
        _sideViewLookOffsetY = Mathf.Lerp(_sideViewLookOffsetY, targetLookOffsetY,
                                           sideViewFramingSpeed * Time.deltaTime);

        // Step 5 — apply corrected LookAt
        cameraObject.transform.LookAt(baseLookTarget + Vector3.up * _sideViewLookOffsetY, Vector3.up);

        // Keep cameraLocation in sync so movement uses the correct reference frame
        cameraLocation.transform.position    = cameraObject.transform.position;
        cameraLocation.transform.eulerAngles = new Vector3(0f, cameraObject.transform.eulerAngles.y, 0f);

        // Publish fight axis so MovementController locks the player's facing to the opponent
        cameraStateWapper.FightAxis = fightAxis;
    }




    /// <summary>
    /// Rebuilds _sortedTargets with players within targetingRange, sorted nearest → farthest.
    /// Players outside the range are excluded entirely.
    /// </summary>
    void BuildSortedTargets()
    {
        _sortedTargets.Clear();
        if (_owner == null) return;

        Vector3 ownerPos    = playerTransform.position;
        float   rangeSqr    = targetingRange * targetingRange;

        foreach (LocalPlayerManager p in LocalPlayerManager.ActivePlayers)
        {
            if (p == _owner || p.character == null) continue;

            float distSqr = Vector3.SqrMagnitude(p.character.transform.position - ownerPos);
            if (distSqr <= rangeSqr)
                _sortedTargets.Add(p);
        }

        _sortedTargets.Sort((a, b) =>
        {
            float dA = Vector3.SqrMagnitude(a.character.transform.position - ownerPos);
            float dB = Vector3.SqrMagnitude(b.character.transform.position - ownerPos);
            return dA.CompareTo(dB);
        });
    }

void SetCurrentTarget(LocalPlayerManager target)
    {
        ClearTargetHighlight();
        _currentTarget = target;
        if (_currentTarget != null && _currentTarget.character != null)
        {
            _targetRenderers = _currentTarget.character.GetComponentsInChildren<Renderer>();
            ApplyTargetHighlight();
        }
        // Keep cameraTarget and isHit in sync so all downstream consumers
        // (TeamController, AttackController, etc.) see the same target
        cameraTarget = _currentTarget?.character;
        isHit        = _currentTarget != null;
        playerEvents.OnOrbitTargetChanged?.Invoke(_currentTarget, _currentTarget != null);
        // Show the target's own symbol (or their leader's if they're a follower)
        if (_currentTarget != null)
            SetCursorSymbol(_currentTarget.ActiveSymbol);
    }

    /// <summary>Adds a bright emission glow to all renderers on the current target.</summary>
    void ApplyTargetHighlight()
    {
        if (_targetRenderers == null) return;
        // Uses _EmissionColor — ensure the target material has emission enabled.
        // Multiplied by 3 for HDRP HDR intensity.
        Color glow = Color.yellow * 3f;
        foreach (Renderer r in _targetRenderers)
        {
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor("_EmissionColor", glow);
            r.SetPropertyBlock(_mpb);
        }
    }

    /// <summary>Removes the emission glow from all cached target renderers.</summary>
    void ClearTargetHighlight()
    {
        if (_targetRenderers == null) return;
        foreach (Renderer r in _targetRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor("_EmissionColor", Color.black);
            r.SetPropertyBlock(_mpb);
        }
        _targetRenderers = null;
    }

    /// <summary>
    /// Cycles the current target with right-stick X.
    /// Stick right → next (closer first). Stick left → previous (farther first).
    /// Requires stick to return to center before cycling again (prevents rapid spin).
    /// </summary>
    void UpdateTargetCycling()
    {
        if (_sortedTargets.Count <= 1) return;

        float stickX = GamePad.GetAxis("Right Stick X");

        if (Mathf.Abs(stickX) > cycleDzone)
        {
            if (_stickCycleReady)
            {
                _stickCycleReady = false;
                int count = _sortedTargets.Count;

                if (stickX > 0f)
                    _targetIndex = (_targetIndex + 1) % count;          // toward farthest
                else
                    _targetIndex = (_targetIndex - 1 + count) % count;  // toward closest

                // Bounds-check in case list shrank
                _targetIndex = Mathf.Clamp(_targetIndex, 0, _sortedTargets.Count - 1);
                SetCurrentTarget(_sortedTargets[_targetIndex]);
            }
        }
        else
        {
            _stickCycleReady = true;
        }
    }

    /// <summary>
    /// Rotates the orbit angle so the camera sits behind the owner player relative
    /// to the target — both players end up in view with the owner in the foreground
    /// and the target visible ahead of them.
    /// Does not touch the anchor; UpdateAnchor() handles that normally.
    /// </summary>
void UpdateTargetingOrbit()
    {
        if (_currentTarget == null || _currentTarget.character == null) return;

        Vector3 ownerPos  = playerTransform.position;
        Vector3 targetPos = _currentTarget.character.transform.position;

        Vector3 camDir = ownerPos - targetPos;
        camDir.y = 0f;
        if (camDir.sqrMagnitude < 0.001f) return;
        camDir.Normalize();

        float targetDegrees = Mathf.Atan2(camDir.z, camDir.x) * Mathf.Rad2Deg + degreeOffset;

        // Combat framing: use the same attack snap range defined in AttackController.
        float snapRange   = (_owner != null && _owner.attackController != null)
                            ? _owner.attackController.attackSnapRange : 0f;
        float dist        = Vector3.Distance(ownerPos, targetPos);
        float targetOffset = dist <= snapRange ? attackViewAngleOffset : 0f;
        _attackViewOffset = Mathf.MoveTowards(_attackViewOffset, targetOffset, attackViewLerpSpeed * Time.deltaTime);
        targetDegrees += _attackViewOffset;

        degrees = Mathf.MoveTowardsAngle(degrees, targetDegrees, targetingOrbitSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Shows the cursor at the viewport edge pointing toward the target when it
    /// moves off-screen. Hides the cursor when the target is visible on-screen.
    /// </summary>
    /// <summary>
    /// Manages the orbit-mode lock-on cursor in both cases:
    ///   • Target ON-screen  → cursor floats above the target (same as Follow-mode aim cursor).
    ///   • Target OFF-screen → cursor appears at the viewport edge as a directional arrow.
    /// </summary>
    void UpdateOffScreenIndicator()
    {
        if (_currentTarget == null || _currentTarget.character == null || cursor == null)
        {
            if (cursor != null) cursor.SetActive(false);
            return;
        }

        // Poll the target's ActiveSymbol every frame — catches team changes mid-lock
        // (e.g. target joins a team and should now show their leader's symbol)
        SetCursorSymbol(_currentTarget.ActiveSymbol);

        Vector3 targetWorldPos = _currentTarget.character.transform.position + CursorOffset;
        Vector3 screenPos      = camera.WorldToViewportPoint(targetWorldPos);

        bool onScreen = screenPos.z > 0f
                     && screenPos.x >= 0f && screenPos.x <= 1f
                     && screenPos.y >= 0f && screenPos.y <= 1f;

        cursor.SetActive(true);

        if (onScreen)
        {
            // ── Target visible: float symbol cursor above their head ──────────
            cursor.transform.position   = targetWorldPos;
            cursor.transform.localScale = Vector3.one * CursorScale;
            // Billboard: always face this player's camera
            cursor.transform.rotation = Quaternion.LookRotation(cameraObject.transform.forward, Vector3.up);
            return;
        }

        // ── Target off-screen: show symbol at viewport edge pointing toward them ──
        Vector2 dir = new Vector2(screenPos.x - 0.5f, screenPos.y - 0.5f);
        if (screenPos.z < 0f) dir = -dir;   // flip when target is behind camera

        float maxComp = Mathf.Max(Mathf.Abs(dir.x), Mathf.Abs(dir.y));
        if (maxComp < 0.0001f) { cursor.SetActive(false); return; }

        Vector2 edgeVP = dir / maxComp;                             // touches ±0.5 edge
        edgeVP *= (0.5f - offScreenEdgePadding);                    // inset by padding
        edgeVP += new Vector2(0.5f, 0.5f);                          // back to 0–1 range
        edgeVP.x = Mathf.Clamp01(edgeVP.x);
        edgeVP.y = Mathf.Clamp01(edgeVP.y);

        Vector3 edgeWorldPos = camera.ViewportToWorldPoint(new Vector3(edgeVP.x, edgeVP.y, 4f));
        cursor.transform.position   = edgeWorldPos;
        cursor.transform.localScale = Vector3.one * CursorScale;

        // Rotate so the cursor symbol faces the camera and points toward the target
        Vector3 toTarget = targetWorldPos - edgeWorldPos;
        if (toTarget.sqrMagnitude > 0.001f)
            cursor.transform.rotation = Quaternion.LookRotation(toTarget.normalized, camera.transform.up);
    }

    // ══════════════════════════════════════════════════════════════════════════

    public void Initialize(GameObject cameraAnchor, Transform parent, PlayerInput GamePad, GameObject cursor, string cameraCullingMask, PlayerEvents playerEvents)
    {
        GameObject cameraLocation = new GameObject("cameraLocation");
        this.cameraLocation = cameraLocation;
        cameraLocation.transform.SetParent(parent);
        GameObject CameraObject = new GameObject("myCamera");
        CameraObject.transform.SetParent(parent);
        this.cameraObject = CameraObject;
        this.camera = CameraObject.AddComponent<Camera>();
        
        this.cursor = cursor;
        // Use the cursor's existing MeshRenderer — SpriteRenderer is invisible in HDRP.
        // Swap its material for an HDRP/Unlit transparent one so the symbol texture shows
        // correctly with alpha transparency. Use HDMaterial API so all internal HDRP
        // keywords, blend state, and render queue are set consistently in one call.
        _cursorMeshRenderer = cursor.GetComponent<MeshRenderer>();
        if (_cursorMeshRenderer != null)
        {
            _cursorMeshRenderer.enabled = true;
            var glowShader = Shader.Find("Custom/SymbolGlow");
            if (glowShader != null)
            {
                _cursorMeshRenderer.material = new Material(glowShader);
            }
            else
            {
                Debug.LogWarning("[CameraControler] Custom/SymbolGlow shader not found — cursor will be invisible. " +
                                 "Ensure Assets/Shaders/SymbolGlow.shader is in the project.");
            }
            _cursorMpb = new MaterialPropertyBlock();
        }

        this.playerTransform = cameraAnchor.transform;
        GameObject orbitAnchor = new GameObject("CameraOrbitAnchor");
        orbitAnchor.transform.position = cameraAnchor.transform.position;
        this.cameraAnchor = orbitAnchor;
        this.GamePad = GamePad;

        for (int i = 0; i < 8; i++)
        {
            int layerIndex = LayerMask.NameToLayer($"P{i + 1}Visible");

            
            
            
            if (layerIndex != -1)
            {
                if (layerIndex != LayerMask.NameToLayer(cameraCullingMask))
                {
                    Debug.LogWarning($"cursor.name: {cursor.name[6]} LayerINdex: {LayerMask.LayerToName(layerIndex)[1]}");
                    camera.cullingMask &= ~(1 << layerIndex);
                }
                
            }
        }
        // Safe to create Unity-native objects here (not during serialization)
        _mpb   = new MaterialPropertyBlock();

        // Cache the owning LocalPlayerManager so targeting can find "self"
        _owner = parent.GetComponent<LocalPlayerManager>();

        this.playerEvents = playerEvents;
        this.playerEvents.OnLateUpdate += OnUpdate;
        this.playerEvents.OnHitConfirm += OnHitConfirm;
        this.playerEvents.OnHitConfirmPauseEnd += OnHitConfirmPauseEnd;
        isInitialized = true;
    }

public void Deactivate()
    {
        // Clean up any active state before destroying objects
        if (_isFollowAimLock) ExitFollowAimLock();
        if (_isTargeting)     ExitTargeting();
        _owner = null;
        _sortedTargets.Clear();

        camera = null;

        if (cameraAnchor != null)
        {
            GameObject.Destroy(cameraAnchor);
        }

        this.playerEvents.OnLateUpdate -= OnUpdate;
        this.playerEvents.OnHitConfirm -= OnHitConfirm;
        this.playerEvents.OnHitConfirmPauseEnd -= OnHitConfirmPauseEnd;
        this.playerEvents = null;

        if (cursor != null)
        {
            GameObject.Destroy(cursor);
        }
        if (cameraObject != null)
        {
            GameObject.Destroy(cameraObject);
        }
        isInitialized = false;
    }

    public Camera GetCamera()
    {
        return camera;
    }

    
    public Transform GetCameraLocation()
    {
        return cameraLocation.transform;
    }

    void UpdateAnchor()
    {
        Vector3 flatForward = new Vector3(cameraObject.transform.forward.x, 0, cameraObject.transform.forward.z).normalized;
        Vector3 anchorToPlayer = playerTransform.position - cameraAnchor.transform.position;
        float forwardOffset = Vector3.Dot(anchorToPlayer, flatForward);

        bool beyondFront = forwardOffset > frontThreshold;
        bool beyondBack  = forwardOffset < -backThreshold;

        if (beyondFront || beyondBack)
            isAnchorLerping = true;

        if (isAnchorLerping)
        {
            float distanceToPlayer = Vector3.Distance(cameraAnchor.transform.position, playerTransform.position);
            if (distanceToPlayer <= deadzone)
            {
                isAnchorLerping = false;
            }
            else
            {
                cameraAnchor.transform.position = Vector3.Lerp(cameraAnchor.transform.position, playerTransform.position, anchorLerpSpeed * Time.deltaTime);
            }
        }

        DrawThresholdDebug(flatForward, forwardOffset);
    }

    void UpdateDirectionalFollow()
    {
        bool rightStickActive = Mathf.Abs(GamePad.GetAxis("Right Stick X")) > 0.2f
                             || Mathf.Abs(GamePad.GetAxis("Right Stick Y")) > 0.2f;

        float horizontalInput = GamePad.GetAxis("Move Horizontal");

        if (rightStickActive || Mathf.Abs(horizontalInput) <= 0.2f) return;

        // Derive target angle from the camera's own right vector + stick direction.
        // Avoids any dependency on playerTransform rotation being correct.
        Vector3 flatCamRight = new Vector3(cameraObject.transform.right.x, 0f, cameraObject.transform.right.z).normalized;
        Vector3 moveDir      = flatCamRight * Mathf.Sign(horizontalInput);

        // Camera should sit opposite the movement direction (behind the player)
        float targetDegrees = Mathf.Atan2(-moveDir.z, -moveDir.x) * Mathf.Rad2Deg + degreeOffset;
        degrees = Mathf.MoveTowardsAngle(degrees, targetDegrees, directionalFollowSpeed * Time.deltaTime);
    }

    void DrawThresholdDebug(Vector3 flatForward, float forwardOffset)
    {
        Vector3 anchorPos  = cameraAnchor.transform.position;
        Vector3 playerPos  = playerTransform.position;
        Vector3 right      = Vector3.Cross(Vector3.up, flatForward);
        float   lineHalf   = 2f;

        // Threshold lines relative to anchor — red
        Vector3 frontThresholdPos = anchorPos + flatForward * frontThreshold;
        Vector3 backThresholdPos  = anchorPos - flatForward * backThreshold;
        Debug.DrawLine(frontThresholdPos - right * lineHalf, frontThresholdPos + right * lineHalf, Color.red);
        Debug.DrawLine(backThresholdPos  - right * lineHalf, backThresholdPos  + right * lineHalf, Color.red);

        // Camera flat forward axis between back and front threshold — yellow
        Debug.DrawLine(backThresholdPos, frontThresholdPos, Color.yellow);

        // Deadzone circle around the player — green
        int segments = 16;
        for (int i = 0; i < segments; i++)
        {
            float a0 = (i / (float)segments) * Mathf.PI * 2f;
            float a1 = ((i + 1) / (float)segments) * Mathf.PI * 2f;
            Vector3 p0 = playerPos + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * deadzone;
            Vector3 p1 = playerPos + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * deadzone;
            Debug.DrawLine(p0, p1, Color.green);
        }

        // Anchor position marker — white cross
        Debug.DrawLine(anchorPos - right * 0.3f,     anchorPos + right * 0.3f,     Color.white);
        Debug.DrawLine(anchorPos - Vector3.up * 0.3f, anchorPos + Vector3.up * 0.3f, Color.white);

        // Line from anchor to player — cyan, brightens to magenta while lerping
        Color lerpColor = isAnchorLerping ? Color.magenta : Color.cyan;
        Debug.DrawLine(anchorPos, playerPos, lerpColor);
    }

    public CameraStateWrapper GetCameraState()
    {
        return cameraStateWapper;
    }

public void DrawDebugStats(string playerName, (string name, float value, float max)[] stats, TeamController teamController)
    {
        if (!showDebugStats || camera == null) return;

        // Camera.rect uses a bottom-left origin; GUI uses top-left, so flip Y.
        Rect  vp      = camera.rect;
        float screenX = vp.x * Screen.width;
        float screenY = (1f - vp.y - vp.height) * Screen.height;
        float screenW = vp.width  * Screen.width;
        float screenH = vp.height * Screen.height;

        const float panelW  = 250f;
        const float padding =   8f;
        const float nameCol = 110f;

        // Panel height grows with team size: base covers header + stats + targeting + team status,
        // plus 20px per team member row.
        int   memberCount = teamController?.team != null ? teamController.team.GetAllMembers().Count : 0;
        float panelH      = 200f + memberCount * 20f;

        Rect area = new Rect(
            screenX + padding,
            screenY + padding,
            Mathf.Min(panelW, screenW - padding * 2f),
            Mathf.Min(panelH, screenH - padding * 2f));

        // ── Styles — built once, reused every frame ───────────────────────────
        if (_guiHeader == null)
        {
            _guiHeader = new GUIStyle(GUI.skin.label)
                { fontStyle = FontStyle.Bold, fontSize = 12, alignment = TextAnchor.MiddleCenter };
            _guiHeader.normal.textColor = Color.white;

            _guiDivider = new GUIStyle(GUI.skin.label)
                { fontSize = 10, alignment = TextAnchor.MiddleCenter };
            _guiDivider.normal.textColor = new Color(1f, 1f, 1f, 0.35f);

            _guiName = new GUIStyle(GUI.skin.label)
                { fontSize = 11, richText = true, alignment = TextAnchor.MiddleLeft };
            _guiName.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

            _guiValue = new GUIStyle(GUI.skin.label)
                { fontSize = 11, richText = true, alignment = TextAnchor.MiddleRight };
        }

        // ── Draw panel ────────────────────────────────────────────────────────
        GUI.Box(area, GUIContent.none);
        GUILayout.BeginArea(area);

        GUILayout.Space(3f);
        GUILayout.Label(playerName.ToUpper(), _guiHeader);
        GUILayout.Label("────────────────────", _guiDivider);

        foreach (var (name, value, max) in stats)
        {
            float t   = max > 0f ? Mathf.Clamp01(value / max) : 0f;
            Color col = Color.HSVToRGB(t * 0.33f, 0.9f, 1f);
            string hex  = UnityEngine.ColorUtility.ToHtmlStringRGB(col);
            string label = $"<color=#{hex}>{value,6:F0}  /  {max:F0}</color>";

            GUILayout.BeginHorizontal();
            GUILayout.Label(name,  _guiName,  GUILayout.Width(nameCol));
            GUILayout.Label(label, _guiValue);
            GUILayout.EndHorizontal();
        }

        // ── Targeting section ─────────────────────────────────────────────────
        GUILayout.Label("────────────────────", _guiDivider);

        string targetLabel;
        if (_isFollowAimLock && _followAimTarget != null)
        {
            // Cyan = aim-lock target in Follow mode
            string targetName = string.IsNullOrEmpty(_followAimTarget.playerName)
                ? "Unknown" : _followAimTarget.playerName;
            targetLabel = $"<color=#00FFFF>{targetName}</color>  <color=#888888>[Aim]</color>";
        }
        else if (_isTargeting && _currentTarget != null)
        {
            // Yellow = locked on in Orbit mode
            string targetName = string.IsNullOrEmpty(_currentTarget.playerName)
                ? "Unknown" : _currentTarget.playerName;

            if (_losTimer > 0f)
            {
                float losRemain = Mathf.Max(0f, losTimeout - _losTimer);
                targetLabel = $"<color=#FFD700>{targetName}</color>  "
                            + $"<color=#FF6644>[LOS {losRemain:F1}s]</color>";
            }
            else
            {
                targetLabel = $"<color=#FFD700>{targetName}</color>";
            }
        }
        else
        {
            targetLabel = "<color=#666666>None</color>";
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("Target", _guiName, GUILayout.Width(nameCol));
        GUILayout.Label(targetLabel, _guiValue);
        GUILayout.EndHorizontal();

        // ── Team section ──────────────────────────────────────────────────────
        if (teamController != null)
        {
            GUILayout.Label("────────────────────", _guiDivider);

            // Status row — gold = Leader, cyan = Follower, gray = Solo
            string statusHex = teamController.CurrentStatus switch
            {
                TeamController.Status.Leader   => "FFD700",
                TeamController.Status.Follower => "88CCFF",
                _                              => "888888",
            };
            GUILayout.BeginHorizontal();
            GUILayout.Label("Status", _guiName, GUILayout.Width(nameCol));
            GUILayout.Label($"<color=#{statusHex}>{teamController.CurrentStatus}</color>", _guiValue);
            GUILayout.EndHorizontal();

            // Member rows
            if (teamController.team != null)
            {
                var members = teamController.team.GetAllMembers();
                for (int i = 0; i < members.Count; i++)
                {
                    LocalPlayerManager m = members[i];
                    string mHex = m.CurrentTeamStatus switch
                    {
                        TeamController.Status.Leader   => "FFD700",
                        TeamController.Status.Follower => "88CCFF",
                        _                              => "888888",
                    };
                    string mName  = string.IsNullOrEmpty(m.playerName) ? "?" : m.playerName;
                    string mLabel = $"<color=#{mHex}>[{m.CurrentTeamStatus}]</color>";

                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"  {mName}", _guiName, GUILayout.Width(nameCol));
                    GUILayout.Label(mLabel, _guiValue);
                    GUILayout.EndHorizontal();
                }
            }
        }

        GUILayout.Space(3f);
        GUILayout.EndArea();
    }

/// <summary>
    /// Draws the aim-lock centre detection rectangle in the game view.
    /// Cyan when aim-lock is active, white when idle. Toggle with showAimCenterBox.
    /// Call this from OnGUI alongside DrawDebugStats.
    /// </summary>
public void DrawAimCenterBox()
    {
        if (!showAimCenterBox || camera == null) return;
        if (!_isFollowAimLock) return;

        if (_aimBoxTex == null)
        {
            _aimBoxTex = new Texture2D(1, 1);
            _aimBoxTex.SetPixel(0, 0, Color.white);
            _aimBoxTex.Apply();
        }

        float camX = camera.rect.x;
        float camY = camera.rect.y;
        float camW = camera.rect.width;
        float camH = camera.rect.height;

        float boxLeft   = (camX + (0.5f - aimCenterX) * camW) * Screen.width;
        float boxRight  = (camX + (0.5f + aimCenterX) * camW) * Screen.width;
        float boxTop    = (1f - camY - (0.5f + aimCenterY) * camH) * Screen.height;
        float boxBottom = (1f - camY - (0.5f - aimCenterY) * camH) * Screen.height;
        float boxW      = boxRight  - boxLeft;
        float boxH      = boxBottom - boxTop;

        const float T = 2f;

        Color col = _followAimTarget != null
            ? new Color(1f, 0f, 0f, 0.9f)   // bright red — target acquired
            : new Color(0f, 0.4f, 1f, 0.6f); // blue — no target

        GUI.color = col;

        GUI.DrawTexture(new Rect(boxLeft,      boxTop,        boxW, T), _aimBoxTex);
        GUI.DrawTexture(new Rect(boxLeft,      boxBottom - T, boxW, T), _aimBoxTex);
        GUI.DrawTexture(new Rect(boxLeft,      boxTop,        T, boxH), _aimBoxTex);
        GUI.DrawTexture(new Rect(boxRight - T, boxTop,        T, boxH), _aimBoxTex);

        float cx = (boxLeft + boxRight)  * 0.5f;
        float cy = (boxTop  + boxBottom) * 0.5f;
        const float CH = 8f;
        GUI.DrawTexture(new Rect(cx - CH,      cy - T * 0.5f, CH * 2f, T),        _aimBoxTex);
        GUI.DrawTexture(new Rect(cx - T * 0.5f, cy - CH,      T,        CH * 2f), _aimBoxTex);

        GUI.color = Color.white;
    }

public void DrawSideViewDebugButton()
    {
        if (!isInitialized || camera == null) return;
        if (!_isTargeting && !_isSideView) return;

        Rect  vp      = camera.rect;
        float screenX = vp.x * Screen.width;
        float screenY = (1f - vp.y - vp.height) * Screen.height;
        float screenW = vp.width  * Screen.width;
        float screenH = vp.height * Screen.height;

        const float btnW = 180f;
        const float btnH = 28f;
        const float pad  = 10f;

        Rect btnRect = new Rect(
            screenX + (screenW - btnW) * 0.5f,
            screenY + screenH - btnH - pad,
            btnW, btnH);

        string label = _isSideView ? "[Debug] Exit Side View" : "[Debug] Enter Side View";
        Color  prev  = GUI.color;
        GUI.color = _isSideView
            ? new Color(1f, 0.4f, 0.2f, 0.9f)   // orange-red when active
            : new Color(0.2f, 0.9f, 1f, 0.9f);   // cyan when available

        if (GUI.Button(btnRect, label))
        {
            if (_isSideView) ExitSideView();
            else             EnterSideView();
        }

        GUI.color = prev;
    }

// Cached entry so RefreshCursorAppearance can re-apply without needing the sprite again
    private PlayerSymbolEntry _currentSymbolEntry;

    // Convenience reads — fall back to sensible defaults when no entry is loaded yet
    Vector3 CursorOffset => _currentSymbolEntry != null ? _currentSymbolEntry.positionOffset : new Vector3(0f, 2f, 0f);
    float   CursorScale  => _currentSymbolEntry != null ? _currentSymbolEntry.scale          : 1f;

    /// <summary>
    /// Applies the given symbol entry (sprite + colour + glow) to the cursor renderer.
    /// Each player's cursor is independent via MaterialPropertyBlock — no shared-material mutation.
    /// </summary>
    public void SetCursorSymbol(PlayerSymbolEntry entry)
    {
        if (_cursorMeshRenderer == null || entry == null || entry.sprite == null) return;
        _currentSymbolEntry = entry;
        _cursorMeshRenderer.GetPropertyBlock(_cursorMpb);
        _cursorMpb.SetTexture(_ID_BaseColorMap,  entry.sprite.texture);
        _cursorMpb.SetColor  (_ID_BaseColor,     entry.symbolColor);
        _cursorMpb.SetColor  (_ID_EmissiveColor, entry.glowColor);
        _cursorMpb.SetFloat  (_ID_GlowIntensity, entry.glowIntensity);
        _cursorMeshRenderer.SetPropertyBlock(_cursorMpb);
    }

    /// <summary>
    /// Re-applies colour and glow from the current symbol entry without changing the texture.
    /// Call this at runtime if you edit entry values and need an immediate visual update.
    /// </summary>
    public void RefreshCursorAppearance()
    {
        if (_cursorMeshRenderer == null || _cursorMpb == null || _currentSymbolEntry == null) return;
        _cursorMeshRenderer.GetPropertyBlock(_cursorMpb);
        _cursorMpb.SetColor (_ID_BaseColor,     _currentSymbolEntry.symbolColor);
        _cursorMpb.SetColor (_ID_EmissiveColor, _currentSymbolEntry.glowColor);
        _cursorMpb.SetFloat (_ID_GlowIntensity, _currentSymbolEntry.glowIntensity);
        _cursorMeshRenderer.SetPropertyBlock(_cursorMpb);
    }




   

    public void DrawGizmos()
    {
        if (!isInitialized || playerTransform == null) return;

        Vector3 origin = playerTransform.position;

        // ── Targeting range sphere ─────────────────────────────────────────────
        Gizmos.color = _isTargeting
            ? new Color(1f, 0.8f, 0f, 0.08f)   // gold fill when active
            : new Color(0f, 0.8f, 1f, 0.04f);  // cyan fill when idle
        Gizmos.DrawSphere(origin, targetingRange);

        Gizmos.color = _isTargeting
            ? new Color(1f, 0.8f, 0f, 0.7f)
            : new Color(0f, 0.8f, 1f, 0.35f);
        Gizmos.DrawWireSphere(origin, targetingRange);

        // ── Line to current target ─────────────────────────────────────────────
        if (_isTargeting && _currentTarget != null && _currentTarget.character != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 targetPos = _currentTarget.character.transform.position;
            Gizmos.DrawLine(origin, targetPos);
            Gizmos.DrawWireSphere(targetPos, 0.5f);
#if UNITY_EDITOR
            Handles.Label(targetPos + Vector3.up * 1.8f,
                          $"◉ {_currentTarget.playerName}", EditorStyles.boldLabel);
#endif
        }

#if UNITY_EDITOR
        Handles.Label(origin + Vector3.up * (targetingRange + 0.5f),
                      $"Target Range  {targetingRange} m");
#endif
    }

    public void SetCameraName(String playerName)
    {
        if (camera != null)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(playerName);
            this.PlayerName = stringBuilder.ToString();
            stringBuilder.Append(" myCamera");
            camera.name = stringBuilder.ToString();
            stringBuilder.Replace(" myCamera", " Cursor");
            cursor.name = stringBuilder.ToString();
        }
        
    }

    

    public void SetDisplayName(String playerName)
    {

    }

    public void OnHitConfirm((Collider hitbox, Collider hurtbox) hitInfo)
    {
        _isHitConfirmPause = true;
    }

    public void OnHitConfirmPauseEnd((Collider hitbox, Collider hurtbox) hitInfo)
    {

        _isHitConfirmPause = false;



    }
}
