using UnityEngine;

public class CameraStateWrapper
{
    public enum CameraState { Orbit, Follow, FightingSide }

    public CameraState CurrentState = CameraState.Orbit;

    /// <summary>True while Follow camera aim-lock (R1) is active.</summary>
    public bool IsFollowAimLock = false;

    /// <summary>
    /// Flat unit vector from the owner toward the opponent in FightingSide mode.
    /// MovementController uses this to lock the player's facing direction.
    /// </summary>
    public Vector3 FightAxis = Vector3.forward;
}
