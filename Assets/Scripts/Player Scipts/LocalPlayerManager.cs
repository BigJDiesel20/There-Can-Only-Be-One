using NUnit.Framework;
using UnityEngine;
using System.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using Rewired;
using TMPro;
using TMPro.Examples;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.PackageManager;

[Serializable]
public class LocalPlayerManager : MonoBehaviour
{
    /// <summary>All currently active (character-spawned) players. Used by targeting systems.</summary>
    public static List<LocalPlayerManager> ActivePlayers = new List<LocalPlayerManager>();

    private LocalPlayerManager playerTarget;
    private StringBuilder sb = new StringBuilder("_player");
    private bool isMuntiy;


    public Player playerGamePad;

    /// <summary>
    /// Gated input wrapper for this player's Rewired gamepad.
    /// All controllers hold a reference to this instead of Rewired.Player directly.
    /// The player state machine sets Context to control which input layer is active.
    /// </summary>
    public PlayerInput playerInput;

    /// <summary>
    /// Per-player state machine. Manages input context and transitions between
    /// Battle, Prone, Dialog, and Spectate states in response to game events.
    /// Initialized in InitializePlayer alongside playerInput.
    /// </summary>
    public PlayerStateMachine stateMachine = new PlayerStateMachine();

    public string playerName;


    public TextMeshPro displayName;

    public GameObject character;
    public TeamController teamController;
    private bool isTeamInitialized = false;
    GameObject auraField;

    public StatController statManager;
    public TeamController.Status CurrentTeamStatus { get { return teamController.CurrentStatus; } set { teamController.CurrentStatus = value; } }

    public CameraControler cameraControler;
    private bool isCameraControlerInitialized = false;
    

    [SerializeField]
    public MovementController movementController;
    private bool isMovementInitialize = false;


    [SerializeField]
    public UserInterfaceController userInterfaceController;
    private bool isUIInitialized = false;

    [SerializeField]
    public AttackController attackController;

    

    public bool test = false;

    // ── Symbol ────────────────────────────────────────────────────────────────
    /// <summary>This player's own permanent symbol entry (sprite + appearance), assigned from PlayerSymbolLibrary at spawn.</summary>
    public PlayerSymbolEntry personalSymbol;

    /// <summary>
    /// The symbol entry currently shown as this player's cursor:
    /// • Solo / Leader  → personalSymbol
    /// • Follower       → the team leader's personalSymbol
    /// </summary>
    public PlayerSymbolEntry ActiveSymbol
    {
        get
        {
            if (teamController == null || teamController.CurrentStatus != TeamController.Status.Follower)
                return personalSymbol;
            LocalPlayerManager leader = teamController.team?.GetLeader();
            return leader != null ? leader.personalSymbol : personalSymbol;
        }
    }

    /// <summary>
    /// No-op: cursor symbol is now target-driven, not owner-driven.
    /// CameraControler polls the targeted player's ActiveSymbol every frame, so
    /// no explicit push is required when team status changes.
    /// </summary>
    public void RefreshCursorSymbol() { }

    public PlayerEvents playerEvents = new PlayerEvents();



    




    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //playerGamePad = ReInput.players.GetPlayer(0);
        

    }



    // Update is called once per frame
    void Update()
    {
        if (!test)
        {
            playerEvents.OnUpdate?.Invoke();
        }
    }

    void FixedUpdate()
    {
        if (!test)
        {
            playerEvents.OnFixedUpdate?.Invoke();
        }
    }

    void LateUpdate()
    {
        if (!test)
        {
            playerEvents.OnLateUpdate?.Invoke();
        }
    }

    void OnDrawGizmos()
    {
        if (attackController  != null && attackController.IsInitialized)
            attackController.DrawGizmos();

        if (movementController != null && movementController.IsInitialized)
            movementController.DrawGizmos();

        if (cameraControler != null && cameraControler.IsInitialized)
            cameraControler.DrawGizmos();
    }
    //private void OnDrawGizmosSelected()
    //{

    //    if (isCameraControlerInitialized)
    //    {
    //        myCamera myCamera = cameraControler.GetCamera();
    //        Gizmos.color = Color.red;
    //        Gizmos.DrawSphere(myCamera.transform.position, 1f);
    //        Gizmos.DrawLine(myCamera.transform.position, character.transform.position);
    //        //Debug.Log(myCamera.ToString());
    //    }
    //}
    //public void CreateName(int playerCount)
    //{
    //    playerName = sb.Append(playerCount).ToString();
    //    this.name = playerName;
    //}
    public void SetCameraRect(Rect cameraViewport)
    {
        cameraControler.GetCamera().rect = cameraViewport;
    }
    // ── Staged character refs (set in CharacterSelect, consumed in PreGame) ──────
    private GameObject _pendingCharacter;
    private GameObject _pendingDisplayObject;
    private Canvas     _pendingCanvas;
    private GameObject _pendingCursor;
    private string     _pendingCullingMask;

    /// <summary>
    /// Stores the instantiated GameObjects chosen during CharacterSelect without
    /// initialising any controllers. Call BuildCharacter() from PreGame to finish setup.
    /// </summary>
    public void StageCharacter(GameObject character, GameObject displayObject,
                                Canvas canvas, GameObject cursor, string cullingMask)
    {
        _pendingCharacter     = character;
        _pendingDisplayObject = displayObject;
        _pendingCanvas        = canvas;
        _pendingCursor        = cursor;
        _pendingCullingMask   = cullingMask;
    }

    /// <summary>
    /// Completes character initialisation using the refs stored by StageCharacter.
    /// Called by PreGame after SetAuraMaximum so stat events fire with the correct
    /// combined max before the HUD subscribes.
    /// </summary>
    public void BuildCharacter()
    {
        if (_pendingCharacter == null)
        {
            Debug.LogWarning($"[LocalPlayerManager] BuildCharacter called but no character is staged for {playerName}.");
            return;
        }

        InitializePlayerCharacter(_pendingCharacter, _pendingDisplayObject,
                                   _pendingCanvas, _pendingCursor, _pendingCullingMask);

        _pendingCharacter     = null;
        _pendingDisplayObject = null;
        _pendingCanvas        = null;
        _pendingCursor        = null;
        _pendingCullingMask   = null;
    }

    public void InitializePlayer(Player playerGamePad)
    {
        this.playerGamePad = playerGamePad;
        this.playerInput   = new PlayerInput(playerGamePad);

        // Wire the state machine now that playerInput and playerEvents are ready.
        // The machine stays dormant (Disabled context) until Battle calls EnterBattle().
        stateMachine.Initialize(this);
    }
   

    public void Invite(LocalPlayerManager otherPlayer)
    {
        teamController.Invite(otherPlayer);    
    }
    public void JoinRequest()
    {
        
    }

    public void Muntiny()
    {

    }

    public void QuitTeam()
    {

    }

    public void LaunchMessage(string message, UnityAction confirmX, string confirmXButtonText, double messageDuration)
    {
       userInterfaceController.SetMessage(message, confirmX, confirmXButtonText, messageDuration);
    }
    public void LaunchMessage(string message, UnityAction confirmX, UnityAction reject, (string confirmX, string rejectB) buttonText, double messageDuration)
    {
        userInterfaceController.SetMessage(message, confirmX, reject, buttonText, messageDuration);
    }

    public void LaunchMessage(string message, UnityAction confirmX, UnityAction confirmY, UnityAction reject, (string confirmX, string confirmY, string rejectB) buttonText, double messageDuration)
    {
        userInterfaceController.SetMessage(message, confirmX, confirmY, reject, buttonText, messageDuration);
    }
    public void LaunchMessage(string message, UnityAction confirmX, UnityAction confirmY, UnityAction confirmA, UnityAction reject, (string confirmX, string confirmY, string confirmA, string rejectB) buttonText, double messageDuration)
    {
        userInterfaceController.SetMessage(message, confirmX, confirmY, confirmA, reject, buttonText, messageDuration);
    }

    public void InitializePlayerName(string playerName)
    {
        this.name = this.playerName = playerName;

        if (this.displayName != null) this.displayName.text = playerName; 
        if (this.cameraControler != null) cameraControler.SetCameraName(playerName);
        if (this.userInterfaceController != null) userInterfaceController.SetCanvasName(playerName);

    }
    public void InitializePlayerCharacter(GameObject character, GameObject displayNameObject, Canvas canvas, GameObject cursor, string cameraCullingMask)
    {
        this.character = character;
        character.AddComponent<PlayerDetection>().Initialize(this, playerEvents);
        character.transform.SetParent(this.character.transform, false);
        PhysicsMaterial material = new PhysicsMaterial();
        material.staticFriction = 0;
        material.dynamicFriction = 0;
        material.frictionCombine = PhysicsMaterialCombine.Minimum;
        character.GetComponent<Collider>().material = material;
        character.layer = LayerMask.NameToLayer("Player");
        GameObject auraPrefab = AssetDatabase.LoadAssetAtPath("Assets/Prefabs/Aura Field.prefab", typeof(GameObject)) as GameObject;
        auraField = Instantiate(auraPrefab);
        auraField.transform.SetParent(this.character.transform);
        auraField.name = $"{playerName}{auraField.name}";
        auraField.layer = LayerMask.NameToLayer("AuraField"); // only interacts with Player layer
        Collider auraFieldCollider = auraField.GetComponent<Collider>();
        auraFieldCollider.isTrigger = true;
        auraField.AddComponent<ObjectTriggerDetection>().Initialize(playerEvents.objectTriggerEventCollection[ObjectTriggerEvents.Type.AuraField]);
        



        statManager = new StatController();
        statManager.Initialize((100,100), (1, 100), (100, 100), (1, 100), (1000, 1000), (1, 100), (1, 100), this, playerEvents, playerInput);






        // Assign this player's permanent symbol from the library (keyed by Rewired player ID).
        personalSymbol = PlayerSymbolLibrary.Instance?.GetEntry(playerGamePad.id);

        cameraControler = new CameraControler();
        cameraControler.Initialize(character, this.transform, playerInput, cursor, cameraCullingMask, playerEvents);

        // Push the initial symbol to the cursor (player is Solo at this point).
        RefreshCursorSymbol();

        movementController = new MovementController();
        movementController.Initialize(this, cameraControler.GetCamera().transform, cameraControler.GetCameraLocation(), cameraControler.GetCameraState(), character, ref isMovementInitialize, playerEvents);
        


        teamController = new TeamController();
        teamController.Initialize(playerInput, this, playerEvents);
        

        displayName = displayNameObject.GetComponent<TextMeshPro>();
        displayName.color = Color.black;
        displayNameObject.transform.transform.SetParent(character.transform, false);
        displayName.transform.SetLocalPositionAndRotation(new Vector3(0, 1.25f, 0), Quaternion.identity);

        userInterfaceController = new UserInterfaceController();
        userInterfaceController.Initialize(cameraControler.GetCamera(), canvas, playerInput, playerEvents);
        userInterfaceController.SetOwner(this);
        



        attackController = new AttackController();
        attackController.Initialize(playerInput, this, character.transform, playerEvents);

        // Register in the global active-player list so targeting systems can find us.
        if (!ActivePlayers.Contains(this))
            ActivePlayers.Add(this);

        Collider[] colliders = character.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Physics.IgnoreCollision(auraFieldCollider, colliders[i],true);
        }

        // Re-apply the player name now that displayName, cameraControler, and
        // userInterfaceController are all initialised — InitializePlayerName was
        // called during SetPlayerNames (Lobby/CharacterSelect) when they were still null.
        InitializePlayerName(playerName);
    }
    
    public void DeactivatePlayer(Player playerGamePad)
    {
        this.playerGamePad = null;

    }

    public void DeactivatePlayerName()
    {
        this.name = this.playerName = string.Empty;

        if (displayName != null)
        {
            this.displayName.text = string.Empty;
        }
    }
    public void DeactivatePlayerCharacter()
    {
        // If the character was staged but BuildCharacter hasn't run yet, just
        // destroy the pending GameObjects and clear the staging refs.
        if (statManager == null)
        {
            if (_pendingCharacter     != null) GameObject.Destroy(_pendingCharacter);
            if (_pendingDisplayObject != null) GameObject.Destroy(_pendingDisplayObject);
            if (_pendingCanvas        != null) GameObject.Destroy(_pendingCanvas.gameObject);
            if (_pendingCursor        != null) GameObject.Destroy(_pendingCursor);
            _pendingCharacter     = null;
            _pendingDisplayObject = null;
            _pendingCanvas        = null;
            _pendingCursor        = null;
            _pendingCullingMask   = null;
            return;
        }

        // Remove from the global active-player list before tearing anything else down.
        ActivePlayers.Remove(this);

        Collider auraFieldCollider = auraField.GetComponent<Collider>();
        Collider[] colliders = character.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Physics.IgnoreCollision(auraFieldCollider, colliders[i], false);
        }

        // Deactivate state machine first — it may fire OnExit on the current state
        // (setting Context = Disabled) which is harmless, and it unsubscribes all
        // event listeners before the controllers that own those events are torn down.
        stateMachine.Deactivate();

        cameraControler.Deactivate();
        cameraControler = null;

        movementController.Deactivate();
        movementController = null;

        teamController.Deactivate();
        teamController = null;

        userInterfaceController.Deactivate();
        userInterfaceController = null;

        attackController.Deactivate();
        attackController = null;

        statManager.Deactivate();
        statManager = null;

        if (auraField != null)
        {
            var otd = auraField.GetComponent<ObjectTriggerDetection>();
            if (otd != null) otd.Deactivate();
            GameObject.Destroy(auraField);
        }
        auraField = null;

        if (character != null)
        {
            var pd = character.GetComponent<PlayerDetection>();
            if (pd != null) pd.Deactivate();
            GameObject.Destroy(character);
        }
        character = null;
        
    }


    
}







