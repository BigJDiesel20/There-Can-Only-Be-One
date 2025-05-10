using UnityEngine;
using Rewired;
using System;
using UnityEngine.TextCore.Text;



[Serializable]
public class MovementController
{
    public Player gamePad;
    public Rigidbody rb;
    public float signX;
    public float signY;
    public Transform camera;
    public Transform cameraLocation;
    CameraStateWrapper cameraStateWrapper;
    public float pitch = 0;
    public float yaw = 0;
    public GameObject character;

    [SerializeField]
    Vector3 forceDirection = Vector3.zero;
    bool isPushed = false;
    (bool x, bool y, bool z) zeroed = ( true, true,true );


    public RaycastHit hit;
    public Vector3 offset = new Vector3(0, -0.9f, 0);
    [SerializeField]
    float raycastLength = 0.2f;

    private bool isInitialized = false;
    public bool IsInitialized { get { return isInitialized; } }
    [SerializeField] bool _isHitConfirmPause = false;

    //GameObject correctedTransform;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnStart()
    {

        Mathf.Atan2(gamePad.GetAxis("Move Horizontal"), gamePad.GetAxis("Move Vertical"));
    }

    // Update is called once per frame
    public void OnUpdate()
    {
        GroundDetection();

        
            switch (cameraStateWrapper.CurrentState)
            {
                case CameraStateWrapper.CameraState.Orbit:
                    Orbit();
                    //Debug.Log($"{"Movement"} Orbit");
                    break;
                case CameraStateWrapper.CameraState.Follow:
                    Follow();
                    //Debug.Log($"{"Movement"} Follow");
                    break;
            }
        

    }

    public void Orbit()
    {
        signX = 5 * gamePad.GetAxis("Move Horizontal");
        signY = 5 * gamePad.GetAxis("Move Vertical");
        //correctedTransform.transform.eulerAngles = new Vector3(0, camera.eulerAngles.y, camera.eulerAngles.z);
        //correctedTransform.transform.position = camera.position;            
        Vector3 relativeDirection = signY * cameraLocation.forward + signX * cameraLocation.right + 0 * cameraLocation.up;
        // tan(o)* z = y
        // tan()/ y =


        relativeDirection.x += forceDirection.x;
        relativeDirection.z += forceDirection.z;
        if (forceDirection.x > 0) {forceDirection.x -= forceDirection.x * Time.deltaTime; if (forceDirection.x < 0) {forceDirection.x = 0; zeroed.x = true; }}
        if (forceDirection.z > 0) {forceDirection.z -= forceDirection.z * Time.deltaTime; if (forceDirection.z < 0) { forceDirection.z = 0; zeroed.z = true; }}

        if (zeroed.x == true & zeroed.y == true & zeroed.z == true) isPushed = false;

        //if (forceDirection.x > 0) { forceDirection.x -= forceDirection.x * Time.deltaTime; forceDirection.x = (forceDirection.x <= 0) ? 0 : forceDirection.x; }
        //if (forceDirection.y > 0) { forceDirection.z -= forceDirection.z * Time.deltaTime; forceDirection.z = (forceDirection.y <= 0) ? 0 : forceDirection.z; }


        rb.linearVelocity = (!_isHitConfirmPause) ? relativeDirection + Jump() : Vector3.zero;
        if (relativeDirection != Vector3.zero & isPushed == false)
        {
            rb.transform.eulerAngles = new Vector3(0, Mathf.Atan2(relativeDirection.x/*gamePad.GetAxis("Move Horizontal")*/, relativeDirection.z/*gamePad.GetAxis("Move Vertical")*/) * Mathf.Rad2Deg, 0);
        }
        else
        {
            //VIABLE CODE DO NO ERASE
            //rb.transform.forward = camera.forward;
            //rb.transform.eulerAngles = new Vector3(rb.transform.eulerAngles.x - camera.eulerAngles.x, rb.transform.eulerAngles.y, rb.transform.eulerAngles.z);
        }
    }


    public void Follow()
    {
        signX = 5 * gamePad.GetAxis("Move Horizontal");
        signY = 5 * gamePad.GetAxis("Move Vertical");
        yaw += 5 * gamePad.GetAxis("Right Stick X");
        //pitch += gamePad.GetAxis("Right Stick Y");
        //yaw = yaw % 360;
        rb.linearVelocity = rb.transform.TransformDirection(new Vector3(signX, 0, signY)) + Jump();
        rb.transform.eulerAngles = new Vector3(0, yaw, 0);
    }
    public void OnOnOnGUI()
    {
        GUI.TextArea(new Rect(10, 10, 200, 100), rb.linearVelocity.ToString(), 200); ;
    }
    public void GetInput()
    {
        /*"Xaxis: "+ ReInput.players.GetPlayer(1).GetAxis("Left Stick X").ToString() + " Yaxis: "+ ReInput.players.GetPlayer(1).GetAxis("Left Stick Y").ToString()*/

    }

    void MinMax(ref float number, float min, float max)
    {
        number = (number < min) ? min : (number > max) ? max : number;

    }

    public void Initialize(LocalPlayerManager player, Transform camera, Transform cameraLocation, CameraStateWrapper cameraStateWrapper, GameObject charater, ref bool isMovementInitialized)
    {
        gamePad = player.playerGamePad;
        rb = player.character.GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        this.camera = camera;
        this.cameraLocation = cameraLocation;
        this.character = charater;
        this.cameraStateWrapper = cameraStateWrapper;
        //correctedTransform = new GameObject("RelativeTransform");
        //correctedTransform.transform.SetParent(player.gameObject.transform);
        isMovementInitialized = isInitialized = true;

    }
    public void Deactivate()
    {
        gamePad = null;
        rb = null;
        //GameObject.Destroy(correctedTransform.gameObject);
        //correctedTransform = null;

    }
    public float height = 5;
    public float charge = .5f;
    public float yVelocity = 0;
    public bool isGrounded;
    public bool isJumping = false;
    public float gravity = 5f;
    public enum JumpState { Grounded, Jumping, Falling, Launched }
    public JumpState jumpState;
    public Vector3 Jump()
    {
        // when on ground not pressing any thing jump is 0
        // press and hold button to charge jump precentage 0-1
        //if on ground and release button jump. charge precentage * jumpforce
        // jump velocity decay raising. speed up while faling.
        // set to 0 on touch ground.



        switch (jumpState)
        {
            case JumpState.Grounded:
                // charge jump
                if (gamePad.GetButton("A") & isGrounded)
                {
                    charge += Time.deltaTime;
                    charge = (charge > 1f) ? 1 : (charge < 0) ? 0 : charge;
                }


                //jump
                if (gamePad.GetButtonUp("A") & isGrounded)
                {
                    yVelocity = charge * height;
                    jumpState = JumpState.Jumping;
                    ;
                }
                else if (!isGrounded)
                {
                    jumpState = JumpState.Falling;
                }

                if (isGrounded & forceDirection.y > 0)
                {
                    yVelocity = forceDirection.y;                    
                    jumpState = JumpState.Launched;
                }
                break;
            case JumpState.Jumping:
                forceDirection.y = yVelocity -= gravity * Time.deltaTime;
                if (yVelocity < 0)
                {
                    forceDirection.y = 0;
                    zeroed.y = true;
                    jumpState = JumpState.Falling;
                }
                break;

            case JumpState.Launched:
                forceDirection.y -= forceDirection.y;
                yVelocity -= gravity * Time.deltaTime;
                if (yVelocity < 0)
                {
                    jumpState = JumpState.Falling;
                }
                break;
            case JumpState.Falling:
                yVelocity -= 3 * gravity * Time.deltaTime;

                if (yVelocity > 0)
                {
                    jumpState = JumpState.Jumping;
                }
                if (isGrounded)
                {
                    forceDirection.y = 0;
                    zeroed.y = true;
                    yVelocity = 0f;
                    charge = .5f;
                    jumpState = JumpState.Grounded;
                }
                break;
        }

        //if (isGrounded & isInAir) { yVelocity = 0; isInAir = false; }

        return new Vector3(0, yVelocity, 0);
    }
    void GroundDetection()
    {
        //offset.y = -1f * ((character.GetComponent<MeshFilter>().mesh.bounds.size.y));
        isGrounded = Physics.Raycast(character.transform.position + offset, character.transform.TransformDirection(Vector3.down), out hit, raycastLength);

        if (isGrounded)
        {
            Debug.DrawRay(character.transform.position + offset, character.transform.TransformDirection(Vector3.down) * hit.distance, Color.red);
            //Debug.Log("Did Hit");
        }
        else
        {
            Debug.DrawRay(character.transform.position + offset, character.transform.TransformDirection(Vector3.down) * raycastLength, Color.blue);
            //Debug.Log("Did not Hit");
        }
    }

    public void OnHitConfirm((Collider hitbox, Collider hurtbox) hitInfo)
    {
        _isHitConfirmPause = true;


        //bool isAttacked = character.gameObject.GetInstanceID() == hitInfo.hurtbox.gameObject.GetInstanceID();
        //bool isAttacking = character.gameObject.GetInstanceID() == hitInfo.hitBox.transform.parent.gameObject.GetInstanceID();

        //if (isAttacking)
        //{
        //    Debug.Log($"{rb.transform.parent.name} hit {hitInfo.hurtbox.transform.parent.name}");
        //}
        //else
        //{
        //    Debug.Log($"{rb.transform.parent.name} has been hit by {hitInfo.hitBox.transform.parent.parent.name}");
        //}


    }

    public void OnHitConfirmPauseEnd((Collider hitbox, Collider hurtbox) hitInfo)
    {
        //Debug.Log($"_isHitConfirmPause Before: {_isHitConfirmPause}");
        _isHitConfirmPause = false;
        //Debug.Log($"_isHitConfirmPause After: {_isHitConfirmPause}");
        bool isAttacking = character.gameObject.GetInstanceID() == hitInfo.hitbox.transform.parent.gameObject.GetInstanceID();

        if (isAttacking)
        {
            //Debug.Log($"{rb.transform.parent.name} hit {hitInfo.hurtbox.transform.parent.name}");

        }

    }

    public void OnPush(Vector3 direction)
    {
        forceDirection = character.transform.InverseTransformVector(direction);
        Debug.Log($"direction: {direction}");
        Debug.Log($"forceDirection: {forceDirection}");
        isPushed = true;
        if (forceDirection.x > 0) zeroed.x = false;
        if (forceDirection.y > 0) zeroed.y = false;
        if (forceDirection.z > 0) zeroed.z = false;
    }

    
}
