using System;
using Unity.Mathematics;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.HighDefinition;
using static UnityEngine.Rendering.DebugUI;


[Serializable]
public class StatController
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
    private PlayerEvents playerEvents;
    private bool _isInitialized;
    private bool isHitConfirmPause;

    public bool IsInitialized { get => _isInitialized;}

    public void OnUpdate()
    {
        RegenerateHealth();
        RecoverStamia();

        if (Input.GetKeyDown(KeyCode.Space)) { playerEvents.OnDamageReceived(new Damage(20, Damage.AttackType.Slash)); }
    }



    


    void AddHealth()
    {

    }

    
    public void Initialize(Stat Health, Stat HealthRegeneration, Stat Stamina, Stat StaminaRevovery, Stat Aura, Stat Armor, Stat ToughHide, MonoBehaviour monoBehaviour, PlayerEvents playerEvents)
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

    public void Initialize((float starting,float max ) health, (float starting, float max) healthRegeneration, (float starting, float max) stamina, (float starting, float max) staminaRevovery, (float starting, float max) aura, (float starting, float max) armor, (float starting, float max) toughHide, MonoBehaviour monoBehaviour, PlayerEvents playerEvents)
    {
        
            
        
            this.Health = new Stat();        
            this.Health.Initialize(health.starting, 0, health.max, monoBehaviour, playerEvents.statEventsCoclection[StatEvents.Type.Health]);
            this.HealthRegeneration = new Stat();
            this.HealthRegeneration.Initialize(healthRegeneration.starting, 0, healthRegeneration.max, monoBehaviour, playerEvents.statEventsCoclection[StatEvents.Type.HealthRegeneration]);
            this.Stamina = new Stat();
            this.Stamina.Initialize(stamina.starting, 0, stamina.max, monoBehaviour, playerEvents.statEventsCoclection[StatEvents.Type.Stamina]);
            this.StaminaRevovery = new Stat();
            this.StaminaRevovery.Initialize(staminaRevovery.starting, 0, staminaRevovery.max, monoBehaviour, playerEvents.statEventsCoclection[StatEvents.Type.StaminaRecovery]);
            this.Aura = new Stat();
            this.Aura.Initialize(aura.starting, 0, aura.max, monoBehaviour, playerEvents.statEventsCoclection[StatEvents.Type.Aura]);
            this.Armor = new Stat();
            this.Armor.Initialize(armor.starting, 0, armor.max, monoBehaviour, playerEvents.statEventsCoclection[StatEvents.Type.Armor]);
            this.ToughHide = new Stat();
            this.ToughHide.Initialize(toughHide.starting, 0, toughHide.max, monoBehaviour, playerEvents.statEventsCoclection[StatEvents.Type.ToughHide]);
        this.playerEvents = playerEvents;
        this.playerEvents.OnUpdate += OnUpdate;
        this.playerEvents.OnHitConfirm += OnHitConfirm;
        this.playerEvents.OnHitConfirmPauseEnd += OnHitConfirmPauseEnd;
        this.playerEvents.OnDamageReceived += OnDamageReceived; 
        _isInitialized = true;
    }
public void OnHitConfirm((Collider hitbox, Collider hurtbox) arg0)
    {
        isHitConfirmPause = true;
    }
    public void OnHitConfirmPauseEnd((Collider hitbox, Collider hurtbox) arg0)
    {
        isHitConfirmPause = false;
    }

    

    public void Deactivate()
    {
        this.Health.Deactivate();
        this.Health = null;
        this.HealthRegeneration.Deactivate();
        this.HealthRegeneration = null;
        this.Stamina.Deactivate();
        this.Stamina = null;
        this.StaminaRevovery.Deactivate();
        this.StaminaRevovery = null;
        this.Aura.Deactivate();
        this.Aura = null;
        this.Armor.Deactivate();
        this.Armor = null;
        this.ToughHide.Deactivate();
        this.ToughHide = null;
        this.playerEvents.OnUpdate -= OnUpdate;
        this.playerEvents.OnHitConfirm -= OnHitConfirm;
        this.playerEvents.OnHitConfirmPauseEnd -= OnHitConfirmPauseEnd;
        this.playerEvents = null;
        _isInitialized = false;
    }


    public void OnDamageReceived(Damage Damage)
    {
        float value = Damage.Value;
        if (Damage.Type == Damage.AttackType.Smash)
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
            float RegenerationRatio = HealthRegeneration.Value / 100;
            float maxRegenerationRatePerSecond = 10f * Time.deltaTime;
            float currentRegenerationRatePerSecond = RegenerationRatio * maxRegenerationRatePerSecond;
            Health.Add(currentRegenerationRatePerSecond);
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

            float ratio = StaminaRevovery.Value / 100;
            float ratePerSecond = 10f * Time.deltaTime;
            float recoveryRate = ratio * ratePerSecond;
            Stamina.Add(recoveryRate); 
            return true;
        }
        
        
    }

    
}

