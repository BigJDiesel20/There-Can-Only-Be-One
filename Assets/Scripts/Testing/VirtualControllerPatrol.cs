using System.Collections.Generic;
using UnityEngine;
using VirtualControllers;

/// <summary>
/// Runtime MonoBehaviour that drives virtual controllers to patrol random waypoints
/// by writing to their VirtualControllerState left-stick axes each frame.
///
/// Each patrolling player wanders to random positions within a fixed radius of their
/// spawn position (captured when patrol starts).  When patrol is stopped the player
/// steers back to that origin; sticks are zeroed once they arrive.
///
/// The stick direction is projected into camera-relative space so the movement
/// matches however the game maps left-stick axes to world movement.
///
/// This component is auto-added to the VirtualRewiredBridge GameObject by
/// VirtualControllerManagerWindow — you do not need to place it manually.
/// </summary>
public class VirtualControllerPatrol : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────
    public static VirtualControllerPatrol Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────
    [Tooltip("Fraction of full stick deflection used while patrolling (0.1 – 1.0).")]
    [Range(0.1f, 1f)]
    public float stickMagnitude = 0.8f;

    [Tooltip("XZ distance (metres) at which a waypoint is considered reached.")]
    public float arrivalThreshold = 1.5f;

    // ── Per-player data ───────────────────────────────────────────────────
    private class PatrolData
    {
        public bool               isPatrolling;   // true = wander; false = returning to origin
        public Vector3            origin;          // world-space position captured when patrol began
        public Vector3            target;          // current waypoint
        public float              radius;          // wander radius around origin
        public float              speed;           // stick magnitude 0.1–1.0 for this player
        public LocalPlayerManager lpm;             // cached — avoids per-frame scene queries
    }

    private readonly Dictionary<int, PatrolData> _patrol = new Dictionary<int, PatrolData>();
    private GameManager _gm;

    // Throttle: only push new stick values every N frames.
    // Cameras don't need to react to stick changes at 60 fps — every 3 frames is plenty.
    private const int TickInterval = 3;
    private int _tickCounter = 0;

    // ── Unity lifecycle ───────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (_patrol.Count == 0) return;

        // Cache registry reference once per frame rather than inside every TickPlayer call
        _registry = VirtualControllerRegistry.Instance;
        if (_registry == null) return;

        // Throttle: only update stick axes every TickInterval frames.
        // Arrival checks (distance) still run every frame so we don't overshoot waypoints.
        _tickCounter = (_tickCounter + 1) % TickInterval;
        bool pushSticks = _tickCounter == 0;

        var done = new List<int>();

        foreach (var kvp in _patrol)
        {
            bool stillActive = TickPlayer(kvp.Key, kvp.Value, pushSticks);
            if (!stillActive) done.Add(kvp.Key);
        }

        foreach (var idx in done)
            _patrol.Remove(idx);
    }
    private VirtualControllerRegistry _registry;

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>Begin patrol for one virtual player.</summary>
    public void StartPatrol(int playerIndex, float radius, float speed = -1f)
    {
        var lpm = FindLocalPlayer(playerIndex);
        if (lpm == null || lpm.character == null)
        {
            Debug.LogWarning($"[Patrol] Player {playerIndex + 1} has no spawned character — cannot start patrol.");
            return;
        }

        if (!_patrol.TryGetValue(playerIndex, out var data))
        {
            data = new PatrolData();
            _patrol[playerIndex] = data;
        }

        data.lpm          = lpm;                                    // cache — no per-frame lookup
        data.radius       = radius;
        data.speed        = speed < 0f ? stickMagnitude : Mathf.Clamp01(speed);
        data.origin       = lpm.character.transform.position;
        data.target       = RandomWaypoint(data.origin, radius);
        data.isPatrolling = true;

        Debug.Log($"[Patrol] Player {playerIndex + 1} patrol started — origin={data.origin} radius={radius}m");
    }

    /// <summary>Stop wandering; player steers back to origin then idles.</summary>
    public void StopPatrol(int playerIndex)
    {
        if (!_patrol.TryGetValue(playerIndex, out var data)) return;
        data.isPatrolling = false;
        data.target       = data.origin;
        Debug.Log($"[Patrol] Player {playerIndex + 1} returning to origin.");
    }

    /// <summary>Start patrol on every player that has a spawned character.</summary>
    public void StartAll(float radius, float speed = -1f)
    {
        if (_gm == null) _gm = FindFirstObjectByType<GameManager>();
        if (_gm == null) { Debug.LogWarning("[Patrol] GameManager not found."); return; }

        foreach (var slot in _gm.playerSlot)
        {
            var lpm = slot?.GetComponent<LocalPlayerManager>();
            if (lpm != null && lpm.playerGamePad != null)
                StartPatrol(lpm.playerGamePad.id, radius, speed);
        }
    }

    /// <summary>Stop all currently patrolling players (they will return to origin).</summary>
    public void StopAll()
    {
        foreach (var kvp in new Dictionary<int, PatrolData>(_patrol))
            if (kvp.Value.isPatrolling)
                StopPatrol(kvp.Key);
    }

    /// <summary>Change the stick speed for one player while patrol is running.</summary>
    public void SetSpeed(int playerIndex, float speed)
    {
        if (_patrol.TryGetValue(playerIndex, out var data))
            data.speed = Mathf.Clamp(speed, 0.1f, 1f);
    }

    /// <summary>Returns the current stick speed for this player, or the global default if not patrolling.</summary>
    public float GetSpeed(int playerIndex)
        => _patrol.TryGetValue(playerIndex, out var data) ? data.speed : stickMagnitude;

    /// <summary>Apply a new speed to every currently active patrol.</summary>
    public void SetSpeedAll(float speed)
    {
        stickMagnitude = Mathf.Clamp(speed, 0.1f, 1f);
        foreach (var data in _patrol.Values)
            data.speed = stickMagnitude;
    }

    /// <summary>True while this player is actively wandering (not returning or idle).</summary>
    public bool IsPatrolling(int playerIndex)
        => _patrol.TryGetValue(playerIndex, out var d) && d.isPatrolling;

    /// <summary>True while the player is either wandering or returning to origin.</summary>
    public bool IsActive(int playerIndex)
        => _patrol.ContainsKey(playerIndex);

    // ── Internal tick ─────────────────────────────────────────────────────

    /// <returns>false when the player has finished returning to origin (remove from dict).</returns>
    private bool TickPlayer(int playerIndex, PatrolData data, bool pushSticks)
    {
        // Use cached lpm — no scene queries in the hot path
        var lpm = data.lpm;
        if (lpm == null || lpm.character == null) return false;

        var state = _registry.GetState(playerIndex);
        if (state == null) return false;

        Vector3 charPos  = lpm.character.transform.position;
        Vector3 toTarget = data.target - charPos;
        toTarget.y       = 0f;
        float   dist     = toTarget.magnitude;

        if (dist < arrivalThreshold)
        {
            if (!data.isPatrolling)
            {
                // Arrived back at origin — zero sticks and finish
                state.leftStickX = 0f;
                state.leftStickY = 0f;
                Debug.Log($"[Patrol] Player {playerIndex + 1} returned to origin.");
                return false;
            }

            // Arrived at waypoint — pick the next one
            data.target = RandomWaypoint(data.origin, data.radius);
            return true;
        }

        // Only push new stick values on throttled frames to reduce camera system load
        if (pushSticks)
            SetStickToward(state, lpm, toTarget / dist, data.speed);

        return true;
    }

    /// <summary>
    /// Converts a world-space direction into camera-relative left-stick values and
    /// writes them to the virtual controller state.
    /// </summary>
    private static void SetStickToward(VirtualControllerState state, LocalPlayerManager lpm,
                                       Vector3 worldDir, float speed)
    {
        // Get the player's camera so we project correctly regardless of camera orbit angle.
        Camera cam = lpm.cameraControler?.GetCamera();

        Vector3 fwd, right;
        if (cam != null)
        {
            fwd   = cam.transform.forward; fwd.y   = 0f;
            right = cam.transform.right;   right.y = 0f;
            if (fwd.sqrMagnitude   > 0.001f) fwd.Normalize();   else fwd   = Vector3.forward;
            if (right.sqrMagnitude > 0.001f) right.Normalize(); else right = Vector3.right;
        }
        else
        {
            fwd   = Vector3.forward;
            right = Vector3.right;
        }

        state.leftStickX = Mathf.Clamp(Vector3.Dot(worldDir, right), -1f, 1f) * speed;
        state.leftStickY = Mathf.Clamp(Vector3.Dot(worldDir, fwd),   -1f, 1f) * speed;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private LocalPlayerManager FindLocalPlayer(int playerIndex)
    {
        if (_gm == null) _gm = FindFirstObjectByType<GameManager>();
        if (_gm == null) return null;

        foreach (var slot in _gm.playerSlot)
        {
            var lpm = slot?.GetComponent<LocalPlayerManager>();
            if (lpm != null && lpm.playerGamePad != null && lpm.playerGamePad.id == playerIndex)
                return lpm;
        }
        return null;
    }

    /// <summary>
    /// Returns a random world-space point within [radius*0.35, radius] of origin
    /// on the XZ plane.  The minimum inner radius avoids all players clustering at
    /// the exact origin when the radius is small.
    /// </summary>
    private static Vector3 RandomWaypoint(Vector3 origin, float radius)
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float dist  = Random.Range(radius * 0.35f, radius);
        return origin + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
    }
}
