using UnityEngine;

public class CameraStateWrapper
{
    public enum CameraState { Orbit, Follow }


    public CameraState CurrentState = CameraState.Orbit;
}
