using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class EventManager
{
    public UnityAction OnUpdate;
    public UnityAction<(Collider hitbox,Collider hurtbox)> OnHitConfirm;
    public UnityAction<(Collider hitbox, Collider hurtbox)> OnHitConfirmPauseEnd;
    public UnityAction<Vector3> OnPush;
    public UnityAction OnAnimationComplete;
    public UnityAction OnCoolDownComplete;

}