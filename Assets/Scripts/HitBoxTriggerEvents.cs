using UnityEngine;
using UnityEngine.Events;


public class HitBoxTriggerEvents
{
    public enum AttackType { None, Light, Heavy, Special, Launcher }
    public UnityAction<Collider> BroadCastOnTriggerEnter;
    public UnityAction<Collider> BroadCastOnTriggerExit;
}

