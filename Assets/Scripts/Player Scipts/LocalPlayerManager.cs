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
    public MovementController movement;
    private bool isMovementInitialize = false;


    [SerializeField]
    public UserInterfaceController uiController;
    private bool isUIInitialized = false;

    [SerializeField]
    public AttackController attackController;


public UnityAction OnUpdate;
    public bool test = false;

    UnityAction<(LocalPlayerManager hitBoxOwner, LocalPlayerManager hurtBoxOwner)> LocalOnHitConfirm;
    public UnityAction<(LocalPlayerManager hitBoxOwner, LocalPlayerManager hurtBoxOwner)> RemoteOnHitConfirm;

    public UnityAction<Vector3> OnHitPauseEnd;




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
            OnUpdate();
            //if (isMovementInitialize) movement.OnUpdate();
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
       uiController.SetMessage(message, confirmX, confirmXButtonText, messageDuration);
    }
    public void LaunchMessage(string message, UnityAction confirmX, UnityAction reject, (string confirmX, string rejectB) buttonText, double messageDuration)
    {
        uiController.SetMessage(message, confirmX, reject, buttonText, messageDuration);
    }

    public void LaunchMessage(string message, UnityAction confirmX, UnityAction confirmY, UnityAction reject, (string confirmX, string confirmY, string rejectB) buttonText, double messageDuration)
    {
        uiController.SetMessage(message, confirmX, confirmY, reject, buttonText, messageDuration);
    }
    public void LaunchMessage(string message, UnityAction confirmX, UnityAction confirmY, UnityAction confirmA, UnityAction reject, (string confirmX, string confirmY, string confirmA, string rejectB) buttonText, double messageDuration)
    {
        uiController.SetMessage(message, confirmX, confirmY, confirmA, reject, buttonText, messageDuration);
    }

    public void InitializePlayerName(string playerName)
    {
        this.name = this.playerName = playerName;

        if (this.displayName != null) this.displayName.text = playerName; 
        if (this.cameraControler != null) cameraControler.SetCameraName(playerName);
        if (this.uiController != null) uiController.SetCanvasName(playerName);

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
        OnUpdate += cameraControler.OnUpdate;


        movement = new MovementController();
        movement.Initialize(this, cameraControler.GetCamera().transform, cameraControler.GetCameraLocation(), cameraControler.GetCameraState(), character, ref isMovementInitialize);
        OnUpdate += movement.OnUpdate;
        teamController = new TeamController();
        teamController.InitializeTeam(playerGamePad, this, ref cameraControler.cameraTarget, ref isTeamInitialized);
        OnUpdate += teamController.OnUpdate;
        cameraControler.TrackTarget += teamController.SetTarget;


        displayName = displayNameObject.GetComponent<TextMeshPro>();
        displayName.color = Color.black;
        displayNameObject.transform.transform.SetParent(character.transform, false);
        displayName.transform.SetLocalPositionAndRotation(new Vector3(0, 2, 0), Quaternion.identity);

        uiController = new UserInterfaceController();
        uiController.InstatiateMessageBox(cameraControler.GetCamera(), canvas, playerGamePad);
        OnUpdate += uiController.OnUpdate;

        attackController = new AttackController();       

        LocalOnHitConfirm += movement.OnHitConfirm;
        LocalOnHitConfirm += cameraControler.OnHitConfirm;
        LocalOnHitConfirm += teamController.OnHitConfirm;
        LocalOnHitConfirm += uiController.OnHitConfirm;
        LocalOnHitConfirm += attackController.OnHitConfirm;



        RemoteOnHitConfirm += movement.OnHitConfirm;
        RemoteOnHitConfirm += cameraControler.OnHitConfirm;
        RemoteOnHitConfirm += teamController.OnHitConfirm;
        RemoteOnHitConfirm += uiController.OnHitConfirm;
        RemoteOnHitConfirm += attackController.OnHitConfirm;


        OnHitPauseEnd += movement.OnHitPauseEnd;
        OnHitPauseEnd += cameraControler.OnHitPauseEnd;
        OnHitPauseEnd += teamController.OnHitPauseEnd;
        OnHitPauseEnd += uiController.OnHitPauseEnd;
        OnHitPauseEnd += attackController.OnHitPauseEnd;

        attackController.Initialize(playerGamePad, this, character.transform, LocalOnHitConfirm, OnHitPauseEnd);
        OnUpdate += attackController.OnUpdate;

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
        OnUpdate -= cameraControler.OnUpdate;
        OnUpdate -= movement.OnUpdate;
        OnUpdate -= teamController.OnUpdate;
        OnUpdate -= attackController.OnUpdate;
        cameraControler.DeactivateCameraControler(ref isCameraControlerInitialized);
        cameraControler = null;
        movement.Deactivate();
        movement = null;
        if (this.character != null)
        {
            GameObject.Destroy(this.character);
        }
        teamController = null;



    }

    

   
}







