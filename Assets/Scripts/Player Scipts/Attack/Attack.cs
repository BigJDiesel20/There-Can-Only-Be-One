#define DEBUG

using System;
using UnityEngine;
using System.Collections.Generic;
using System.Windows.Input;
using UnityEditor.PackageManager.Requests;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.TextCore.Text;
using static AttackController;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Events;
using UnityEditor.PackageManager;

[Serializable]
public class Attack : IAttackCommand
{
    LocalPlayerManager player;
    AttackController.AttackType _type;
    List<(int comboIndex, AttackType attackType)> ComboList;// = new List<(int comboIndex, AttackType attackType)> { (2, AttackType.Light), (3, AttackType.Heavy), };
    [SerializeField]
    double animationTimer = 0;
    [SerializeField]
    double _animationLength;
    [SerializeField]
    double _animationProgress = 0;
    [SerializeField]
    double coolDownTimer = 0;
    [SerializeField]
    double _coolDownLength;
    [SerializeField]
    double _coolDownProgress = 0;
    Collider hitBox;
    [SerializeField] Collider hurtBox;
    [SerializeField] bool isHitConfirm = false;
    Material hitboxMaterial;
    [SerializeField]
    bool isAttackAnimationActive = false;
    float startAlbedo;
    Action onAttack;
   
    //UnityAction<Vector3> OnHitPauseEnd;
    Action onMiss;
    
    [SerializeField]
    bool isCoolDownActive = false;
    [SerializeField] bool _isHitConfirmPause = false;
    [SerializeField] double hitStunTimer;
    [SerializeField] double hitStunLength = .2f;
    Color tempColor;
    [SerializeField] float pushBack = 1;

    EventManager eventManager;
    
    






    public List<(double time, Action action)> onAnimation = new List<(double time, Action action)>();

    public double AnimationLength { get => _animationLength; set => _animationLength = value; }
    public double CoolDownLength { get => _coolDownLength; set => _coolDownLength = value; }
    public Collider Hitbox
    {
        get
        {
            return hitBox;
        }
        set
        {
            hitBox = value;
            hitboxMaterial = hitBox.GetComponent<Renderer>().material;
            startAlbedo = hitboxMaterial.color.a;
            hitboxMaterial.color = new Color(hitboxMaterial.color.r, hitboxMaterial.color.g, hitboxMaterial.color.b, 0);
            hitBox.isTrigger = true;
        }
    }

    public AttackController.AttackType Type { get => _type; set => _type = value; }
    public bool IsAttackActive { get => isAttackAnimationActive; }
    public double AnimationProgress { get => _animationProgress; }
    public double CoolDownProgress { get => _coolDownProgress; }
    public bool IsHitConfirmPause { get => _isHitConfirmPause; }

    public void Execute()
    {
        if (_isHitConfirmPause)
        {
            hitboxMaterial.color = Color.blue;
            //Debug.Log($"_isHitConfirmPause is True /{_isHitConfirmPause}");
            hitStunLength = Clamp(hitStunLength, 0.1, hitStunLength);
            if (((hitStunTimer += Time.deltaTime) / hitStunLength) >= 1)
            {
                hitboxMaterial.color = tempColor;
                hitStunTimer = 0;
                _isHitConfirmPause = false;                
                
                //OnHitPauseEnd?.Invoke();
                eventManager.OnHitConfirmPauseEnd?.Invoke((hitBox,hurtBox));
                switch(_type)
                {     
                    case AttackController.AttackType.Launcher:

                        eventManager.OnPush?.Invoke(Vector3.up * pushBack);
                        break;
                    default:
                        Vector3 direction = hurtBox.transform.position - hitBox.transform.position;
                        direction.Normalize();
                        eventManager.OnPush?.Invoke(direction * pushBack);
                        break;
                }                
            }

        }
        else
        {
            ActivateAttack();

            if (isCoolDownActive & isAttackAnimationActive)
            {
                //Debug.Log($"_isHitConfirmPause is False/{_isHitConfirmPause}");
                DoWhileAnimationIsActive();
                DeactivateAttackAnimationOnComplete();

            }
            else if (isCoolDownActive & !isAttackAnimationActive)
            {
                DoWhileAttackBlockIsActive();
                DeactivateAttackBlockOnComPlete();
            }
            else if (!isCoolDownActive & !isAttackAnimationActive)
            {

            }
        }
        
           
            
            
        

    }



    private void DeactivateAttackBlockOnComPlete()
    {
        if (_coolDownProgress >= 1)
        {
            eventManager.OnCoolDownComplete?.Invoke();
            isCoolDownActive = false;
        }
    }



    private void DeactivateAttackAnimationOnComplete()
    {
        if (_animationProgress >= 1)
        {
            eventManager.OnAnimationComplete?.Invoke();
            hitboxMaterial.color = new Color(hitboxMaterial.color.r, hitboxMaterial.color.g, hitboxMaterial.color.b, 0);
            //Debug.Log(isAttackAnimationActive);
            isAttackAnimationActive = false;
        }
    }
    private void DoWhileAttackBlockIsActive()
    {
        _coolDownProgress = TrackProgress(ref coolDownTimer, _coolDownLength);
    }
    private void DoWhileAnimationIsActive()
    {
        //_coolDownProgress = TrackProgress(ref coolDownTimer, _coolDownLength);
        _animationProgress = TrackProgress(ref animationTimer, _animationLength);

        if (hurtBox != null & isHitConfirm == false)
        {
            tempColor = hitboxMaterial.color;
            Debug.Log($"Hit {hurtBox.name}");
            _isHitConfirmPause = isHitConfirm = true;
            eventManager.OnHitConfirm?.Invoke((hitBox,hurtBox));
            
        }


        for (int i = 0; i < onAnimation.Count; i++)
        {
            if (_animationProgress >= onAnimation[i].time) onAnimation[i].action?.Invoke();
        }


    }

    private void ActivateAttack()
    {
        if (_coolDownProgress == 0 & _animationProgress == 0)
        {
            isCoolDownActive = isAttackAnimationActive = true;
            onAttack?.Invoke();
            hitboxMaterial.color = new Color(hitboxMaterial.color.r, hitboxMaterial.color.g, hitboxMaterial.color.b, startAlbedo);
#if DEBUG
            //Debug.Log("StartPunch");
#endif
        }
    }

    private double TrackProgress(ref double timer, double Lenght)
    {
        if (Lenght != 0)
        {
            return Clamp((timer += Time.deltaTime) / Lenght,0,1);
            //Debug.Log($"_coolDownProgress: {_coolDownProgress}");
        }
        else
        {
            return 1;
            //Debug.Log($"_coolDownProgress: {_coolDownProgress}");
        }


        
    }
    double Clamp(double value,double min,double max)
        {
            return (value <= min) ? min : (value >= max) ? max : value;
        }
    public void ResetAttack()
    {
        _coolDownProgress = coolDownTimer = _animationProgress = animationTimer = 0;
        isCoolDownActive = isAttackAnimationActive = isHitConfirm = false;
    }


    public void AddTimeStapededAction(int timeStame, Action action)
    {
        onAnimation.Add((timeStame, action));
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            LocalPlayerManager otherPlayer = other.GetComponent<HitDetectionManager>().player;
            eventManager.OnHitConfirm += otherPlayer.eventManager.OnHitConfirm;
            eventManager.OnHitConfirmPauseEnd += otherPlayer.eventManager.OnHitConfirmPauseEnd;
            eventManager.OnPush = otherPlayer.eventManager.OnPush;
            hurtBox = other;

        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            LocalPlayerManager otherPlayer = other.GetComponent<HitDetectionManager>().player;
            eventManager.OnHitConfirm -= otherPlayer.eventManager.OnHitConfirm;
            if (_isHitConfirmPause == true) 
            {
                Vector3 direction = hurtBox.transform.position - hitBox.transform.position;
                direction.Normalize();
                eventManager.OnHitConfirmPauseEnd?.Invoke((hitBox, hurtBox));
                
                eventManager.OnPush?.Invoke(direction * pushBack);
            }
            eventManager.OnHitConfirmPauseEnd -= otherPlayer.eventManager.OnHitConfirmPauseEnd;


            if (hurtBox == other)
            {
                hurtBox = null;
            }
        }
    }
    public bool IsComboAble(int ComboIndex, AttackType attackType)
    {
        bool isComboAble = false;

        if (ComboList != null)
        {

            for (int i = 0; i < ComboList.Count; i++)
            {
                int NextComboIndex = ComboIndex + 1;
                if (ComboList[i].comboIndex == NextComboIndex & ComboList[i].attackType == attackType)
                {
                    isComboAble = true;
                }
#if DEBUG == true
                //Debug.Log($"{attackType.ToString()} is {((isComboAble) ? "ComboAble" : "ComboAble")} at index {NextComboIndex}");
#endif
            }
        }
        return isComboAble;
    }
    public void SetCombos(List<(int comboIndex, AttackType attackType)> combos)
    {
        ComboList = combos;
    }
    

    

    

    /// <summary>
    /// Set a Action to be exicuted after a specified percentage of the animation is played.
    /// </summary>
    /// <param name="action"> The method you want to trigger.</param>
    /// <param name="animationProgress">The Precentage point that you want you method to exicute. Number must be between 0 and 1.</param>
    public void SetOnAnimaiton(Action action, double animationProgress)
    {
        onAnimation.Add((animationProgress, action));
    }

    /// <summary>
    /// Set a Action to be exicuted at a spesific point in time during the animation.
    /// </summary>
    /// <param name="action">The method you want to trigger.</param>
    /// <param name="time">The Time stamp that you want you method to play. Must add (f) to specifiy float or cast as float.</param>
    public void SetOnAnimaiton(Action action, float time = 0f)
    {
        double progress = Clamp(time, 0, _animationLength) / _animationLength;
        onAnimation.Add((progress, action));
    }
    public void Initialize(LocalPlayerManager player,Transform character, string hitBoxName, Vector3 hitBoxPosition, Vector3 hitBoxEulerAngle, Vector3 hitBoxScale, double animationLength, double attackBlockLength,float pushBack, AttackType Type,EventManager eventManager)
    {
        this.player = player;
        GameObject hitboxPrefab = AssetDatabase.LoadAssetAtPath("Assets/Prefabs/Hit Box.prefab", typeof(GameObject)) as GameObject;
        hitBox = GameObject.Instantiate(hitboxPrefab).GetComponent<BoxCollider>();
        hitBox.isTrigger = true;
        hitBox.transform.SetParent(character);
        hitBox.gameObject.name = hitBoxName;
        hitBox.transform.localPosition = hitBoxPosition;
        hitBox.transform.localEulerAngles = hitBoxEulerAngle;
        hitBox.transform.localScale = hitBoxScale;
        TriggerDetectionManager triggerDetection = hitBox.gameObject.AddComponent<TriggerDetectionManager>() as TriggerDetectionManager;
        triggerDetection.BroadCastOnTriggerEnter += OnTriggerEnter;
        triggerDetection.BroadCastOnTriggerExit += OnTriggerExit;        
        _animationLength = animationLength;
        _coolDownLength = attackBlockLength;

        _type = Type;
        this.pushBack = pushBack;

        hitboxMaterial = hitBox.GetComponent<Renderer>().material;
        startAlbedo = hitboxMaterial.color.a;
        hitboxMaterial.color = new Color(hitboxMaterial.color.r, hitboxMaterial.color.g, hitboxMaterial.color.b, 0);
        hitBox.isTrigger = true;

        //if (hitBoxScale == Vector3.zero)
        //{
        //    hitBox.gameObject.SetActive(false);
        //}

        this.eventManager = eventManager;


    }

    
}