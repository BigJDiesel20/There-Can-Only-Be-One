using UnityEngine;
using UnityEngine.Events;

public class TriggerDetectionManager : MonoBehaviour
{
    [SerializeField] Collider other;
    public UnityAction<Collider> BroadCastOnTriggerEnter;
    public UnityAction<Collider> BroadCastOnTriggerExit;
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
        BroadCastOnTriggerEnter(other);
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
        BroadCastOnTriggerExit(other);
    }
}
