using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;




public class ObjectTriggerDetection : MonoBehaviour
{
    [SerializeField] Collider other;
    [SerializeField] ObjectTriggerEvents _triggerEvents;

    public ObjectTriggerEvents TriggerEvents { get => _triggerEvents; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }



    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        this.other = other;
        _triggerEvents.BroadCastOnTriggerEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (this.other == other) this.other = null;
        _triggerEvents.BroadCastOnTriggerExit(other);
    }

    public void Initialize(ObjectTriggerEvents triggerEvents)
    {

        _triggerEvents = triggerEvents;
    }

    public void Deactivate()
    {
        _triggerEvents = null;
        other = null;
    }
}