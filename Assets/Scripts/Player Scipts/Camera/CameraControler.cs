using System;
using TMPro.Examples;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.HighDefinition;
using static UnityEngine.GraphicsBuffer;
using UnityEngine.UI;
using System.Text;
using UnityEngine.TextCore.Text;
using UnityEditor;

[Serializable]
public class CameraControler
{
    public Rewired.Player GamePad;
    public float x;
    public float y;
    public float height = 3f;
    public float radius = 7f;
    public float degrees = 0f;
    public float degreeOffset = 90f;

    public float sholderHeight = 1.74f;
    public float sholderDistance = -5.25f;
    public float sholderOffset = 2.19f;
    public GameObject cameraObject;
    public GameObject cameraLocation;
    public Camera camera;
    public GameObject cameraAnchor;
    public GameObject cameraTarget;    
    public Vector3 cameraOffset = Vector3.zero;
    public float targetDistance;
    private RaycastHit hit;
    private bool isHit;
    LayerMask mask; // = LayerMask.GetMask("player");
    public bool isSwitched = false;    
    public GameObject cursor;
    public string PlayerName;
    



    public GameObject TestObject;
    public Vector3 TestVector = new Vector3(0, 0, -3);

    private bool isInitialized = false;
    public bool IsInitialized { get { return isInitialized; } }

    CameraStateWrapper cameraStateWapper = new CameraStateWrapper();
    private bool _isHitConfirmPause;
    private PlayerEvents playerEvents;

    public void OnUpdate()
    {
        
        cameraStateWapper.CurrentState = CameraStateWrapper.CameraState.Orbit;
        if (isInitialized)
        {
            CameraDetection();
            if (GamePad.GetButtonDown("Left Stick Button")) isSwitched = !isSwitched;
            cameraStateWapper.CurrentState = (isSwitched) ? CameraStateWrapper.CameraState.Follow : CameraStateWrapper.CameraState.Orbit;


            if (cameraObject != null)
            {
                switch (cameraStateWapper.CurrentState)
                {
                    case CameraStateWrapper.CameraState.Orbit:
                        Orbit();
                        //Debug.Log($"{"CameraControler"} Orbit");
                        break;
                    case CameraStateWrapper.CameraState.Follow:
                        Follow();
                        //Debug.Log($"{"CameraControler"} Follow");
                        break;
                }
            }

        }
    }


    public void Orbit()
    {
        y = Mathf.Abs(radius) * (float)Math.Sin((degrees - degreeOffset) * Mathf.Deg2Rad);
        x = Mathf.Abs(radius) * (float)Math.Cos((degrees - degreeOffset) * Mathf.Deg2Rad);
        degrees += (Mathf.Abs(GamePad.GetAxis("Right Stick X")) > 0.2f) ? -GamePad.GetAxis("Right Stick X") : 0;
        degrees = degrees % 360;
        cameraLocation.transform.position = new Vector3(x, height, y) + cameraAnchor.transform.position;
        cameraObject.transform.position = cameraLocation.transform.position;
        Vector3 relativeCameraAnchorPosition = camera.transform.InverseTransformPoint(cameraAnchor.transform.localPosition);
        camera.transform.LookAt(cameraAnchor.transform.position, Vector3.up); //= new Vector3(0, MathF.Atan2(relativeCameraAnchorPosition.z, relativeCameraAnchorPosition.x) * Mathf.Rad2Deg, 0);
        cameraLocation.transform.LookAt(cameraAnchor.transform.position, Vector3.up);
        cameraLocation.transform.eulerAngles = new Vector3(0, camera.transform.eulerAngles.y, camera.transform.eulerAngles.z);
        camera.transform.eulerAngles = new Vector3(10, camera.transform.eulerAngles.y, camera.transform.eulerAngles.z);

    }

    public void Follow()
    {
        //float rightStickXInput = 1; //(Mathf.Abs(GamePad.GetAxis("Right Stick X")) > 0.2f) ? Math.Sign(GamePad.GetAxis("Right Stick X")) : 1; 
        //y = -Mathf.Abs(sholderDistance);
        /*x = sholderOffset * rightStickXInput;*/
        cameraObject.transform.position = cameraAnchor.transform.TransformPoint(new Vector3(sholderOffset, sholderHeight, sholderDistance));

        cameraObject.transform.forward = cameraAnchor.transform.forward;








        //cameraObject.transform.position = cameraAnchor.transform.InverseTransformPoint(cameraObject.transform.position);

        //camera.transform.eulerAngles = new Vector3(0,cameraAnchor.transform.eulerAngles.y,0);

        if (TestObject != null) TestObject.transform.position = TestVector;

    }
    public void InitializeCameraControler(GameObject cameraAnchor, Transform parent, Rewired.Player GamePad, GameObject cursor, string cameraCullingMask, PlayerEvents playerEvents)
    {
        GameObject cameraLocation = new GameObject("cameraLocation");
        this.cameraLocation = cameraLocation;
        cameraLocation.transform.SetParent(parent);
        GameObject CameraObject = new GameObject("Camera");
        CameraObject.transform.SetParent(parent);
        this.cameraObject = CameraObject;
        this.camera = CameraObject.AddComponent<Camera>();
        
        this.cursor = cursor;

        this.cameraAnchor = cameraAnchor;
        this.GamePad = GamePad;
        mask = LayerMask.GetMask("Player");
        
        
        for (int i = 0; i < 8; i++)
        {
            int layerIndex = LayerMask.NameToLayer($"P{i + 1}Visible");

            
            
            
            if (layerIndex != -1)
            {
                if (layerIndex != LayerMask.NameToLayer(cameraCullingMask))
                {
                    Debug.LogWarning($"cursor.name: {cursor.name[6]} LayerINdex: {LayerMask.LayerToName(layerIndex)[1]}");
                    camera.cullingMask &= ~(1 << layerIndex);
                }
                
            }
        }
        this.playerEvents = playerEvents;
        this.playerEvents.OnUpdate += OnUpdate;
        this.playerEvents.OnHitConfirm += OnHitConfirm;
        this.playerEvents.OnHitConfirmPauseEnd += OnHitConfirmPauseEnd;
        isInitialized = true;
    }

    public void Deactivate()
    {
        camera = null;

        
        this.playerEvents.OnUpdate -= OnUpdate;
        this.playerEvents.OnHitConfirm -= OnHitConfirm;
        this.playerEvents.OnHitConfirmPauseEnd -= OnHitConfirmPauseEnd;
        this.playerEvents = null;        
        
        if (cursor != null)
        {
            GameObject.Destroy(cursor);
        }
        if (cameraObject != null)
        {
            GameObject.Destroy(cameraObject);
        }
        isInitialized = false;
    }

    public Camera GetCamera()
    {
        return camera;
    }

    
    public Transform GetCameraLocation()
    {
        return cameraLocation.transform;
    }

    void CameraDetection()
    {
        
        if (isHit = Physics.Raycast(camera.transform.position, camera.transform.TransformDirection(Vector3.forward), out hit, Mathf.Infinity, mask))
        {
            targetDistance = hit.distance;
            Debug.DrawRay(camera.transform.position + cameraOffset, camera.transform.TransformDirection(Vector3.forward) * hit.distance, Color.red);
            //Debug.Log("Did Hit");
            cameraTarget = hit.transform.gameObject;
        }
        else
        {
            Debug.DrawRay(camera.transform.position + cameraOffset, camera.transform.TransformDirection(Vector3.forward) * 200, Color.blue);
            //Debug.Log("Did not Hit");
            cameraTarget = null;
        }



        if (isHit)
        {
            
                cursor.gameObject.SetActive(true);
                cursor.transform.position = hit.transform.position + new Vector3(0, 2, 0);
           

        }
        else
        {
            cursor.gameObject.SetActive(false);
        }
       
        playerEvents.TrackTarget?.Invoke(hit, isHit);
    }

    public CameraStateWrapper GetCameraState()
    {
        return cameraStateWapper;
    }

    public RaycastHit GetCameraTarget()
    {
        return hit;
    }

   

    public void SetCameraName(String playerName)
    {
        if (camera != null)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(playerName);
            this.PlayerName = stringBuilder.ToString();
            stringBuilder.Append(" Camera");
            camera.name = stringBuilder.ToString();
            stringBuilder.Replace(" Camera", " Cursor");
            cursor.name = stringBuilder.ToString();
        }
        
    }

    

    public void SetDisplayName(String playerName)
    {

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
