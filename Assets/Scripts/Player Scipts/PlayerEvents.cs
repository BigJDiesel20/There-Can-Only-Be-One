using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class PlayerEvents
{
    public UnityAction OnUpdate;
    public UnityAction<(Collider hitbox, Collider hurtbox)> OnHitConfirm;
    public UnityAction<(Collider hitbox, Collider hurtbox)> OnHitConfirmPauseEnd;
    public UnityAction<Vector3> OnPush;
    public UnityAction OnAnimationComplete;
    public UnityAction OnCoolDownComplete;
    public UnityAction<RaycastHit, bool> TrackTarget;
    public UnityAction<Damage> OnDamageReceived;

    

    public Dictionary<StatEvents.Type, StatEvents> statEventsCoclection = new Dictionary<StatEvents.Type, StatEvents>
    { 
        { StatEvents.Type.Health, new StatEvents() },
        { StatEvents.Type.HealthRegeneration, new StatEvents()},
        { StatEvents.Type.Stamina, new StatEvents()},
        { StatEvents.Type.StaminaRecovery, new StatEvents()},
        { StatEvents.Type.Aura, new StatEvents()},
        { StatEvents.Type.Armor, new StatEvents()},
        { StatEvents.Type.ToughHide, new StatEvents()}
    };

    
}