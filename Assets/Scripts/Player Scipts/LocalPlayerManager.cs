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
    private LocalPlayerManager playerTarget;
    private StringBuilder sb = new StringBuilder("player");
    private bool isMuntiy;


    public Player playerGamePad;
    public string playerName;


    public TextMeshPro displayName;

    public GameObject character;
    public TeamController teamController;
    private bool isTeamInitialized = false;
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

    

    public UnityAction<Vector3> OnHitPauseEnd;
    public EventManager eventManager = new EventManager(); 



    //public string stringToEdit;

    void OnGUI()
    {
        if (isCameraControlerInitialized)
        {
            //stringToEdit = (teamController.Members.Count != 0) ? teamController.Members.ToString() : "Solo";
            //// Make a multiline text area that modifies stringToEdit.
            ////cameraControler.GetCamera().rect
            //stringToEdit = GUI.TextArea(cameraControler.GetCamera().rect/*new Rect(10, 10, 200, 100)*/, stringToEdit, 200);
        }
    }




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
            eventManager.OnUpdate?.Invoke();
            //if (isMovementInitialize) movementController.OnUpdate();
            //if (isCameraControlerInitialized) cameraControler.OnUpdate();
            //if (isTeamInitialized) GetTarget(); team.OnUpdate();
        }
    }
    //private void OnDrawGizmosSelected()
    //{

    //    if (isCameraControlerInitialized)
    //    {
    //        Camera camera = cameraControler.GetCamera();
    //        Gizmos.color = Color.red;
    //        Gizmos.DrawSphere(camera.transform.position, 1f);
    //        Gizmos.DrawLine(camera.transform.position, character.transform.position);
    //        //Debug.Log(camera.ToString());
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
    public void InitializePlayer(Player playerGamePad)
    {
        this.playerGamePad = playerGamePad;

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
        character.AddComponent<HitDetectionManager>().player = this;
        character.transform.SetParent(this.character.transform, false);
        PhysicsMaterial material = new PhysicsMaterial();
        material.staticFriction = 0;
        material.dynamicFriction = 0;
        material.frictionCombine = PhysicsMaterialCombine.Minimum;
        character.GetComponent<Collider>().material = material;
        character.layer = LayerMask.NameToLayer("player");






        cameraControler = new CameraControler();
        cameraControler.InitializeCameraControler(character, this.transform, playerGamePad, ref isCameraControlerInitialized, cursor, cameraCullingMask);
        eventManager.OnUpdate += cameraControler.OnUpdate;


        movementController = new MovementController();
        movementController.Initialize(this, cameraControler.GetCamera().transform, cameraControler.GetCameraLocation(), cameraControler.GetCameraState(), character, ref isMovementInitialize);
        eventManager.OnUpdate += movementController.OnUpdate;
        teamController = new TeamController();
        teamController.InitializeTeam(playerGamePad, this, ref cameraControler.cameraTarget, ref isTeamInitialized);
        eventManager.OnUpdate += teamController.OnUpdate;
        cameraControler.TrackTarget += teamController.SetTarget;


        displayName = displayNameObject.GetComponent<TextMeshPro>();
        displayName.color = Color.black;
        displayNameObject.transform.transform.SetParent(character.transform, false);
        displayName.transform.SetLocalPositionAndRotation(new Vector3(0, 2, 0), Quaternion.identity);

        userInterfaceController = new UserInterfaceController();
        userInterfaceController.InstatiateMessageBox(cameraControler.GetCamera(), canvas, playerGamePad);
        eventManager.OnUpdate += userInterfaceController.OnUpdate;

        attackController = new AttackController();

        eventManager.OnHitConfirm += movementController.OnHitConfirm;
        eventManager.OnHitConfirm += cameraControler.OnHitConfirm;
        eventManager.OnHitConfirm += teamController.OnHitConfirm;
        eventManager.OnHitConfirm += userInterfaceController.OnHitConfirm;
        eventManager.OnHitConfirm += attackController.OnHitConfirm;      


        eventManager.OnHitConfirmPauseEnd += movementController.OnHitConfirmPauseEnd;
        eventManager.OnHitConfirmPauseEnd += cameraControler.OnHitConfirmPauseEnd;
        eventManager.OnHitConfirmPauseEnd += teamController.OnHitConfirmPauseEnd;
        eventManager.OnHitConfirmPauseEnd += userInterfaceController.OnHitConfirmPauseEnd;
        eventManager.OnHitConfirmPauseEnd += attackController.OnHitConfirmPauseEnd;

        eventManager.OnPush += movementController.OnPush;

        attackController.Initialize(playerGamePad, this, character.transform, eventManager);
        eventManager.OnUpdate += attackController.OnUpdate;

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
        eventManager.OnUpdate -= cameraControler.OnUpdate;
        eventManager.OnUpdate -= movementController.OnUpdate;
        eventManager.OnUpdate -= teamController.OnUpdate;
        eventManager.OnUpdate -= attackController.OnUpdate;
        cameraControler.DeactivateCameraControler(ref isCameraControlerInitialized);
        cameraControler = null;
        movementController.Deactivate();
        movementController = null;
        if (this.character != null)
        {
            GameObject.Destroy(this.character);
        }
        teamController = null;



    }

    

   
}







