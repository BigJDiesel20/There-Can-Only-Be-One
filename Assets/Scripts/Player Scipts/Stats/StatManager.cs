using System;
using Unity.Mathematics;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.HighDefinition;
using static UnityEngine.Rendering.DebugUI;


[Serializable]
public class StatManager
{
    [SerializeField]
    Stat Health;// = new Stat(1000,0,1000);
    [SerializeField]
    Stat HealthRegeneration;
    [SerializeField]
    Stat Stamina;// = new Stat(100,0,100);
    [SerializeField]
    Stat StaminaRevovery;
    [SerializeField]
    Stat Aura;// = new Stat(10000,0, 10000);

    
    
    
    [SerializeField]
    Stat Armor;
    [SerializeField]
    Stat ToughHide;

    MonoBehaviour monoBehaviour;
    public void OnUpdate()
    {
        RegenerateHealth();
        RecoverStamia();
    }



    public void OnHitConfirm(bool ishitConfirm)
    {

    }


    void AddHealth()
    {

    }

    
    public void Initialize(Stat Health, Stat HealthRegeneration, Stat Stamina, Stat StaminaRevovery, Stat Aura, Stat Armor, Stat ToughHide)
    {
        if (Health.IsInitialized & HealthRegeneration.IsInitialized & Stamina.IsInitialized & StaminaRevovery.IsInitialized & Aura.IsInitialized & Armor.IsInitialized & ToughHide.IsInitialized)
        {
            this.Health = Health;
            this.HealthRegeneration = HealthRegeneration;
            this.Stamina = Stamina;
            this.StaminaRevovery = StaminaRevovery;
            this.Aura = Aura;
            this.Armor = Armor;
            this.ToughHide = ToughHide;
        }
        else
        {
            throw new Exception("Not all stats are initialized");
        }
    }

    

    
    public void DealDamage(Damage Damage)
    {
        float value = Damage.Value;
        if (Damage.Type == Damage.AttackDamage.Smash)
        {
            float percentage = (ToughHide.Value / 100);
            value = value - (percentage * value);
        }
        Health.Subtract(Damage.Value);
        Damage.Reset();
    }

    bool RegenerateHealth()
    {
        if (Health.Value <= Health.Min | Health.Value >= Health.Max)
         {
            return false;
        }
        else
        {
            Health.Add(HealthRegeneration.Value * .5f * Time.deltaTime);
            return true;
        }      
        
    }

    public void Heal()
    {

    }

    bool RecoverStamia()
    {
        if (Stamina.Value <= 0 | Stamina.Value >= Stamina.Max)
        {
            return false;
        }
        else
        {
            Stamina.Add(StaminaRevovery.Value * .5f * Time.deltaTime);
            return true;
        }
        
        
    }

    
}

