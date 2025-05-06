using static AttackController;

public interface IAttackCommand
{
    
    public double AnimationProgress { get; }
    public double CoolDownProgress { get; }
    public bool IsHitConfirmPause { get; }
    public AttackController.AttackType Type { get; set; }
    public void Execute();
    public void ResetAttack();
    public bool IsComboAble(int ComboIndex, AttackType attackType);
    
}