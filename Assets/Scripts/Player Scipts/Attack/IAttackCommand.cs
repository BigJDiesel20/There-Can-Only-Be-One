using UnityEngine;

public interface IAttackCommand
{
    public double AnimationProgress { get; }
    public double CoolDownProgress  { get; }
    public bool   IsHitConfirmPause { get; }
    public HitBoxTriggerEvents.AttackType Type { get; set; }

    /// <summary>
    /// Advance the attack by one fixed frame.
    /// hitBuffer and playerMask are passed in from AttackController so no
    /// allocation happens per-frame and the layer mask is set once at init.
    /// </summary>
    public void Execute(Collider[] hitBuffer, LayerMask playerMask);
    public void ResetAttack();
    public bool IsComboAble(int ComboIndex, HitBoxTriggerEvents.AttackType attackType);

    /// <summary>
    /// Set the direction the attack will lunge toward during its startup phase.
    /// Called by AttackController after snap rotation so the lunge always fires
    /// toward the correct target.
    /// </summary>
    public void SetLungeDirection(Vector3 direction);
}
