using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class PlayerEvents
{
    public UnityAction OnUpdate;
    public UnityAction<(Collider hitbox,Collider hurtbox)> OnHitConfirm;
    public UnityAction<(Collider hitbox, Collider hurtbox)> OnHitConfirmPauseEnd;
    public UnityAction<Vector3> OnPush;
    public UnityAction OnAnimationComplete;
    public UnityAction OnCoolDownComplete;
    public UnityAction<RaycastHit, bool> TrackTarget;
    
}