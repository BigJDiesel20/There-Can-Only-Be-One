using UnityEngine;

public class PlayerDetection : MonoBehaviour
{
    private LocalPlayerManager _player;
    private PlayerEvents playerEvents;

    public LocalPlayerManager Player { get => _player;}
    public PlayerEvents PlayerEvents { get => playerEvents;}

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void Initialize(LocalPlayerManager localPlayerManager, PlayerEvents playerEvents)
    {
        this._player = localPlayerManager;
        this.playerEvents = playerEvents;
    
    }
    public void Deactivate()
    {
        this._player = null;
        this.playerEvents = null;
    }
}
