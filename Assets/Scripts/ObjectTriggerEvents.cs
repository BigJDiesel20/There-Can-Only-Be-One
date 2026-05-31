using UnityEngine;
using UnityEngine.Events;
public class ObjectTriggerEvents
{
    public enum Type { AuraField }
    public UnityAction<Collider> BroadCastOnTriggerEnter;
    public UnityAction<Collider> BroadCastOnTriggerExit;
}