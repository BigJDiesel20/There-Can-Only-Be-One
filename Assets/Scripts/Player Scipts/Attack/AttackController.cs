using System;
using UnityEngine;
using Rewired;
using System.Collections;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine.TextCore.Text;
using System.Windows.Input;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEditor.PackageManager;
using System.Linq;
using static AttackController;

[Serializable]
public class AttackController
{
    LocalPlayerManager player;
    MonoBehaviour monoBehaviour;
    Rewired.Player gamePad;
    [SerializeField]
    Attack LightAttack = new Attack();
    [SerializeField]
    Attack HeaveyAttack = new Attack();
    [SerializeField]
    Attack SpecialAttack = new Attack();
    [SerializeField]
    Attack LauncherAttack = new Attack();
    bool isComplete = false;
    [SerializeField]
    public List<IAttackCommand> AttackQue = new List<IAttackCommand>();
    public enum AttackType { None, Light, Heavy, Special, Launcher }

    [SerializeField]
    Attack NoAttack = new Attack();
    [SerializeField]
    IAttackCommand AttackCommand;
    [SerializeField]
    int comboCounter = 0;
    bool isCounted = false;
    bool isComboCountReset = false;
    [SerializeField]
    List<AttackType> ComboChain = new List<AttackType>();
    [SerializeField]
    List<List<AttackType>> ComboList = new List<List<AttackType>>();

    EventManager eventManager;



    bool IsInitialized = false;
    private bool _isHitConfirmPause;

    public void OnUpdate()
    {

        //if (gamePad.GetButtonDown("X"))
        //{
        //    _isHitConfirmPause = true;
        //}

        if (IsInitialized)
        {
            if (!AttackCommand.IsHitConfirmPause)
            {
                if (gamePad.GetButtonDown("X")) { QueNextAttack(LightAttack); }
                else if (gamePad.GetButtonDown("Y")) { QueNextAttack(HeaveyAttack); }
                else if (gamePad.GetButtonDown("B")) { QueNextAttack(SpecialAttack); }
                else if (gamePad.GetButtonDown("A")) { QueNextAttack(LauncherAttack); }
            }



            AttackCommand.Execute();

            if (AttackCommand.CoolDownProgress >= 1)
            {
                comboCounter = 0;
            }


        }


    }

    private void QueNextAttack(Attack NextAttack)
    {
        if (IsComboable(NextAttack.Type)/*AttackCommand.IsComboAble(comboCounter, NextAttack.Type)*/)
        {
            if (AttackCommand.AnimationProgress >= 1)
            {
                comboCounter++;
                AttackCommand.ResetAttack();
                AttackCommand = NextAttack;
                ComboChain.Add(NextAttack.Type);
            }
        }
        else
        {
            if (AttackCommand.CoolDownProgress >= 1)
            {

                comboCounter = 0;
                comboCounter++;
                AttackCommand.ResetAttack();
                AttackCommand = NextAttack;
                ComboChain.Clear();
                ComboChain.Add(NextAttack.Type);

            }
        }


        //void CountCombo()
        //{
        //    //if (!isCounted)
        //    //{

        //        //isCounted = true;
        //    //}
        //}

        //void ClearComboCounter()
        //{
        //    if (!isComboCountReset)
        //    {

        //        isComboCountReset = true;
        //    }
        //}

        //void ResetCounterFunctions()
        //{
        //    isCounted = false;
        //    isComboCountReset = false;
        //}

    }







    public void Initialize(Rewired.Player gamePad, LocalPlayerManager player, Transform character, EventManager eventManager)
    {
        this.gamePad = gamePad;
        monoBehaviour = player.GetComponent<MonoBehaviour>();


       


        NoAttack.Initialize(player, character, "No Hit Box", Vector3.zero, Vector3.zero, Vector3.zero, 0, 0,0, AttackType.Light, eventManager);
        LightAttack.Initialize(player, character, "Light Attack Hit Box", new Vector3(0, .18f, .9f), new Vector3(0, 0, 0), new Vector3(.5f, .25f, 1), .1f, 1.2f, 1,AttackType.Light, eventManager);
        HeaveyAttack.Initialize(player, character, "Heavy Attack Hit Box", new Vector3(0, .20f, 1), new Vector3(-40, 0, 0), new Vector3(.5f, .30f, 2), .25f, 1.2f, 2,AttackType.Heavy, eventManager);
        SpecialAttack.Initialize(player, character, "Special Attack Hit Box", new Vector3(0, -.19f, 1), new Vector3(0, 0, 0), new Vector3(.5f, 1.1f, 1.4f), .75f, 2.25f,3 ,AttackType.Special, eventManager);
        LauncherAttack.Initialize(player, character, "Launcher Attack Hit Box", new Vector3(0, .30f, .95f), new Vector3(0, 0, 0), new Vector3(.5f, 2.5f, 1.11f), .1f, 1.2f,5 ,AttackType.Launcher, eventManager);

        

        AttackCommand = NoAttack;
        

       

        List<AttackType> breadAndButter = new List<AttackType>() { AttackType.Light, AttackType.Light, AttackType.Light, AttackType.Light };
        List<AttackType> combo1 = new List<AttackType>() { AttackType.Light, AttackType.Light, AttackType.Heavy, AttackType.Heavy };


        List<AttackType> magicSeries = new List<AttackType>() { AttackType.Light, AttackType.Heavy, AttackType.Special, AttackType.Launcher };

        List<AttackType> combo2 = new List<AttackType>() { AttackType.Light, AttackType.Special };
        List<AttackType> combo3 = new List<AttackType>() { AttackType.Heavy, AttackType.Heavy };
        List<AttackType> combo4 = new List<AttackType>() { AttackType.Heavy, AttackType.Launcher };

        ComboList.Add(breadAndButter);
        ComboList.Add(magicSeries);
        ComboList.Add(combo1);
        ComboList.Add(combo2);
        ComboList.Add(combo3);
        ComboList.Add(combo4);


        this.eventManager = eventManager;

        IsInitialized = true;

        //NoAttack.SetOnAnimaiton(() => { }, 0.5);







    }

    bool IsComboable(AttackType attackType)
    {
        List<AttackType> NewChain = ComboChain.ToList();
        NewChain.Add(attackType);

        bool doesComboMatch = false;
        int CountConfirm = 0;
        for (int i = 0; i < ComboList.Count; i++)
        {
            CountConfirm = 0;
            if (ComboList[i].Count >= NewChain.Count)
            {
                for (int j = 0; j < NewChain.Count; j++)
                {
                    if (ComboList[i][j] == NewChain[j])
                    {
                        CountConfirm++;
                    }
                    if (j + 1 == NewChain.Count && CountConfirm == NewChain.Count)
                    {
                        doesComboMatch = true;
                        //Debug.Log($"ComboMatch: {doesComboMatch} index: {i}");
                    }
                }
            }

        }

        return doesComboMatch;

    }
    public void OnHitConfirm((Collider hitbox, Collider hurtbox) hitInfo)
    {
        _isHitConfirmPause = true;
    }
    public void OnHitConfirmPauseEnd((Collider hitbox, Collider hurtbox) hitInfo)
    {

        _isHitConfirmPause = false;
    }
}
