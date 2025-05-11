using UnityEngine.Events;

public class StatEvents
{
    public enum Type { Health, HealthRegeneration, Stamina, StaminaRecovery, Aura, Armor, ToughHide };
    public UnityAction<float> OnValueChange;
    public UnityAction<float> OnMinimumChange;
    public UnityAction<float> OnMaximumChange;
    public UnityAction<float> OnPercentageChange;

    // Detect Death
    public UnityAction OnValueZero;
    public UnityAction OnValueMinimum;
    public UnityAction OnValueMaximum;
}

