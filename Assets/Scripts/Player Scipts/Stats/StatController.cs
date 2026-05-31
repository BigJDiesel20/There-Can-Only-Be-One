using NUnit.Framework;
using Rewired;
using System;
using System.Collections.Generic;
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
    Stat Stamina;// = new Stat(100,0,100);    
    [SerializeField]
    Stat Aura;// = new Stat(10000,0, 10000);



    [SerializeField]
    Stat HealthRegeneration;
    [SerializeField]
    Stat StaminaRevovery;
    [SerializeField]
    Stat Armor;
    [SerializeField]
    Stat ToughHide;
    [SerializeField]
    List<LocalPlayerManager> targets = new List<LocalPlayerManager>();

    MonoBehaviour monoBehaviour;
    private PlayerEvents playerEvents;
    private bool _isInitialized;
    private bool isHitConfirmPause;
    [SerializeField]
    bool isInvulnerabilityActive = false;
    [SerializeField]
    private bool isProne = false;
    [SerializeField]
    private bool isHealthReset = true;
    [SerializeField]
    float threshold = 0;
    [SerializeField]
    float proneTimelimit = 0;
    
    PlayerInput gamePad;
    public bool debugLogs = false;

    public bool IsInitialized { get => _isInitialized; }

    // ── Aura max scaling (called once from PreGame) ───────────────────────────

    /// <summary>Returns the aura maximum currently set on this player.</summary>
    public float GetAuraMax() => Aura.Max;

    /// <summary>
    /// Re-fires OnValueChange and OnPercentageChange on Health, Stamina, and Aura
    /// with their current values. Call this immediately after subscribing a new
    /// listener (e.g. target HUD on lock-on) so it receives the current state
    /// without waiting for the next stat change event.
    /// </summary>
    public void BroadcastCurrentValues()
    {
        Health.Refresh();
        Stamina.Refresh();
        Aura.Refresh();
    }

    /// <summary>
    /// Resets Health, Stamina and Aura current values back to their cached
    /// starting values (set at Initialize time). The aura maximum is left
    /// unchanged so the combined playerCount scaling survives a Replay.
    /// Fires OnValueChange and OnPercentageChange on each stat so the HUD
    /// updates immediately without any extra wiring.
    /// </summary>
    public void ResetStats()
    {
        Health.Reset();
        Stamina.Reset();
        Aura.Reset();
    }

    /// <summary>
    /// Scales the aura maximum to <paramref name="newMax"/> and fires
    /// the OnPercentageChange event so the HUD pie updates immediately.
    /// </summary>
    public void AdjustAuraMaximum(float newMax) => Aura.AdjustMaximum(newMax);

    public void OnUpdate()
    {


        if (Input.GetKeyDown(KeyCode.Space)) { playerEvents.OnDamageReceived(new Damage(20, Damage.AttackType.Slash)); }



        if (Aura.Value != 0)
        {
            if (!isProne & isHealthReset)
            {
                Recover(Health, HealthRegeneration, 1, .1f, true);
                Recover(Stamina, StaminaRevovery, 10, 1, true);              

            }
            else if (isProne & !isHealthReset)
            {
                proneTimelimit = Mathf.Clamp((proneTimelimit += Time.deltaTime), 0, 20);
                if (proneTimelimit >= 20)
                {
                    proneTimelimit = 0;
                    playerEvents.OnProneActive(false);
                    
                }
            }
            else if (!isProne & !isHealthReset)
            {
                
                Recover(Health, HealthRegeneration, Health.Max/3, 0, false);
                if (debugLogs) Debug.Log($"isProne:{isProne} isHealthReset:{isHealthReset} Running");
                if (Health.Value == Health.Max)
                {
                    isHealthReset = true;
                    playerEvents.OnInvulnerabilityActive?.Invoke(false);
                }

            }

            if (gamePad.GetButton("B"))
            {


                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i].statManager.isProne)
                    {
                        targets[i].playerEvents.OnAuraDrain?.Invoke();
                        playerEvents.OnAuraReplenish?.Invoke();
                    }

                }
            }
            else
            {
                threshold = 0;
            }
        }
        else
        {
            if (debugLogs) Debug.Log($"Player Dead");
        }
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

    public void Initialize((float starting, float max) health, (float starting, float max) healthRegeneration, (float starting, float max) stamina, (float starting, float max) staminaRevovery, (float starting, float max) aura, (float starting, float max) armor, (float starting, float max) toughHide, MonoBehaviour monoBehaviour, PlayerEvents playerEvents, PlayerInput gamePad)
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
        this.playerEvents.OnAuraDrain += OnAuraDrain;
        this.playerEvents.OnAuraReplenish += OnAuraReplenish;
        this.playerEvents.OnInvulnerabilityActive += OnInvulnerabilityActive;
        this.playerEvents.OnProneActive += OnProneActive;
        this.playerEvents.statEventsCoclection[StatEvents.Type.Health].OnValueZero += OnHealthValueZero;
        this.playerEvents.objectTriggerEventCollection[ObjectTriggerEvents.Type.AuraField].BroadCastOnTriggerEnter += BroadCastOnTriggerEnter;
        this.playerEvents.objectTriggerEventCollection[ObjectTriggerEvents.Type.AuraField].BroadCastOnTriggerExit += BroadCastOnTriggerExit;
        
        this.gamePad = gamePad;
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
        this.playerEvents.OnAuraDrain -= OnAuraDrain;
        this.playerEvents.OnAuraReplenish -= OnAuraReplenish;
        this.playerEvents.OnInvulnerabilityActive -= OnInvulnerabilityActive;
        this.playerEvents.OnProneActive -= OnProneActive;
        this.playerEvents.statEventsCoclection[StatEvents.Type.Health].OnValueZero -= OnHealthValueZero;
        this.playerEvents.objectTriggerEventCollection[ObjectTriggerEvents.Type.AuraField].BroadCastOnTriggerEnter -= BroadCastOnTriggerEnter;
        this.playerEvents.objectTriggerEventCollection[ObjectTriggerEvents.Type.AuraField].BroadCastOnTriggerExit -= BroadCastOnTriggerExit;

        this.playerEvents = null;
        _isInitialized = false;
    }


    public void OnDamageReceived(Damage Damage)
    {
        if (!isInvulnerabilityActive)
        {
            float value = Damage.Value;
            if (Damage.Type == Damage.AttackType.Smash)
            {
                float percentage = (ToughHide.Value / 100);
                value = value - (percentage * value);
            }
            Health.Subtract(Damage.Value);
        }
        Damage.Reset();
    }

    public void OnAuraDrain()
    {
        Aura.Subtract(5 * Time.deltaTime);
        if (debugLogs) Debug.Log("Being Drained");
    }
    public void OnAuraReplenish()
    {
        Aura.Add(5 * Time.deltaTime);
    }

    /// <summary>
    /// Recovers stat over time 
    /// </summary>
    /// <param name="stat"> Stat to be recovered</param>
    /// <param name="statModifier"> Multiplyer to the base rate bonus value.</param>
    /// <param name="baseRatePerSecond">Base stat per second to be recovered</param>
    /// <param name="baseRateBonusPerModifierValue">Base stat per Modifier value to be added to the base rate per second </param>
    /// <param name="minMaxPauseActive">Pauses recovery if Minimum or Maximum stat value is reached.</param>
    /// <returns></returns>
    bool Recover(Stat stat, Stat statModifier, float baseRatePerSecond, float baseRateBonusPerModifierValue, bool minMaxPauseActive)
    {
        if ((stat.Value <= stat.Min | stat.Value >= stat.Max) & minMaxPauseActive == true)
        {
            if (debugLogs) Debug.Log($"Min or Max is true:{(stat.Value <= stat.Min | stat.Value >= stat.Max)} & PauseActive: {minMaxPauseActive}");
            return false;
        }
        else
        {
            float rateBonusPerSecond = statModifier.Value * baseRateBonusPerModifierValue;
            float ratePerSecond = baseRatePerSecond + rateBonusPerSecond; ;
            float ratePerDletaTime = ratePerSecond * Time.deltaTime;            
            stat.Add(ratePerDletaTime);
            return true;
        }

    }


    public void Heal()
    {

    }

    // ── Debug helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Debug: instantly sets health to zero (triggers prone / knock-out events)
    /// and aura to zero so the player can no longer be drained or replenished.
    /// </summary>
    public void DebugForceKnockOut()
    {
        Health.SetValue(0);   // fires OnValueZero → OnHealthValueZero → prone
        Aura.SetValue(0);     // fires OnValueZero / OnValueMinimum
    }

    /// <summary>
    /// Debug: instantly sets aura to its current maximum, which fires
    /// OnValueMaximum and triggers Battle's win-condition handler.
    /// </summary>
    public void DebugForceWin()
    {
        Aura.SetValue(Aura.Max);
    }

    // Pre-allocated — updated in place every OnGUI call instead of allocating a new array
    private readonly (string name, float value, float max)[] _debugStats = new (string, float, float)[3];

    public (string name, float value, float max)[] GetDebugStats()
    {
        _debugStats[0] = ("Health",  Health.Value,  Health.Max);
        _debugStats[1] = ("Stamina", Stamina.Value, Stamina.Max);
        _debugStats[2] = ("Aura",    Aura.Value,    Aura.Max);
        return _debugStats;
    }

    //bool RecoverStamia()
    //{
    //    if (Stamina.Value <= 0 | Stamina.Value >= Stamina.Max)
    //    {
    //        return false;
    //    }
    //    else
    //    {

    //        float ratio = StaminaRevovery.Value / 100;
    //        float ratePerDeltaTime = 10f * Time.deltaTime;
    //        float recoveryRate = ratio * ratePerDeltaTime;
    //        Stamina.Add(recoveryRate);
    //        return true;
    //    }


    //}



    public void BroadCastOnTriggerEnter(Collider otherPlayer)
    {
        if (otherPlayer.tag.Contains("Player"))
        {
            if (!targets.Contains(otherPlayer.GetComponentInParent<LocalPlayerManager>()))
            {
                targets.Add(otherPlayer.GetComponentInParent<LocalPlayerManager>());
            }
        }
    }
    public void BroadCastOnTriggerExit(Collider otherPlayer)
    {
        if (otherPlayer.tag.Contains("Player"))
        {
            if (targets.Contains(otherPlayer.GetComponentInParent<LocalPlayerManager>()))
            {
                targets.Remove(otherPlayer.GetComponentInParent<LocalPlayerManager>());
            }
        }
    }

    public void OnHealthValueZero()
    {
        if (debugLogs) Debug.Log("Health Zero");
        playerEvents.OnProneActive(true);
        isHealthReset = false;
        playerEvents.OnInvulnerabilityActive?.Invoke(true);
    }


    void OnInvulnerabilityActive(bool isActive)
    {
        isInvulnerabilityActive = isActive;
    }
    void OnProneActive(bool isActive)
    {
        isProne = isActive;
    }

}

