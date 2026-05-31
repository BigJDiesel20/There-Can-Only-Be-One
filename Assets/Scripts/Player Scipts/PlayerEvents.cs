using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class PlayerEvents
{
    public UnityAction OnUpdate;
    public UnityAction OnFixedUpdate;
    public UnityAction OnLateUpdate;
    public UnityAction<Quaternion, float> OnAttackRotate;  // rotation, arcThreshold
    /// <summary>
    /// Fired by <see cref="AttackController"/> the moment the first attack of a
    /// new chain is committed (comboCounter 0 → 1). Subscribed by
    /// <see cref="PlayerStateMachine"/> to enter the Comboing state.
    /// </summary>
    public UnityAction OnAttackStart;
    public UnityAction OnAttackEnd;
    public UnityAction<(Collider hitbox, Collider hurtbox)> OnHitConfirm;
    public UnityAction<(Collider hitbox, Collider hurtbox)> OnHitConfirmPauseEnd;
    public UnityAction<Vector3> OnPush;
    public UnityAction OnAnimationComplete;
    public UnityAction OnCoolDownComplete;
    public UnityAction<LocalPlayerManager, bool> OnOrbitTargetChanged;
    public UnityAction<Damage> OnDamageReceived;
    public UnityAction OnAuraDrain;
    public UnityAction OnAuraReplenish;
    public UnityAction OnTeamChanged;
    public UnityAction<bool> OnInvulnerabilityActive;
    public UnityAction<bool> OnProneActive;

    /// <summary>
    /// Fired by <see cref="UserInterfaceController"/> when a dialog / message box
    /// opens (true) or closes (false). Subscribed by <see cref="PlayerStateMachine"/>
    /// to transition the player into and out of the Dialog input context.
    /// </summary>
    public UnityAction<bool> OnDialogStateChanged;

    public Dictionary<HitBoxTriggerEvents.AttackType, HitBoxTriggerEvents> hitboxTriggerEventCollection = new Dictionary<HitBoxTriggerEvents.AttackType, HitBoxTriggerEvents>
    {
        {HitBoxTriggerEvents.AttackType.None,new HitBoxTriggerEvents() },
        {HitBoxTriggerEvents.AttackType.Light,new HitBoxTriggerEvents() },
        {HitBoxTriggerEvents.AttackType.Heavy,new HitBoxTriggerEvents() },
        {HitBoxTriggerEvents.AttackType.Special,new HitBoxTriggerEvents() },
        {HitBoxTriggerEvents.AttackType.Launcher,new HitBoxTriggerEvents() }
    };

    public Dictionary<ObjectTriggerEvents.Type, ObjectTriggerEvents> objectTriggerEventCollection = new Dictionary<ObjectTriggerEvents.Type, ObjectTriggerEvents>
    {
        {ObjectTriggerEvents.Type.AuraField, new ObjectTriggerEvents() }    
    };

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