using UnityEngine;

public class HitBoxTriggerDetection : MonoBehaviour
{
    [SerializeField] Collider other;
    [SerializeField] HitBoxTriggerEvents _triggerEvents;

    public HitBoxTriggerEvents TriggerEvents { get => _triggerEvents;}

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
        if (other.CompareTag("Player"))
        {
            this.other = other;
        }
        _triggerEvents.BroadCastOnTriggerEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (this.other == other)
            {
                this.other = null;
            }
        }
        _triggerEvents.BroadCastOnTriggerExit(other);
    }

    public void Initialize(HitBoxTriggerEvents triggerEvents)
    {
        
       _triggerEvents = triggerEvents;
    }

    public void Deactivate()
    {
        _triggerEvents = null;
        other = null;
    }
}
