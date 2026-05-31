using JetBrains.Annotations;
using NUnit.Framework;
using UnityEngine;
using System.Collections;
using Rewired;
using System.Collections.Generic;
using System;
using TMPro;

public class GameManager : MonoBehaviour
{
    // ── Game mode ─────────────────────────────────────────────────────────────
    public enum GameMode { Classic }          // extend as new modes are added
    public GameMode currentGameMode = GameMode.Classic;

    // ── Match result ──────────────────────────────────────────────────────────
    /// <summary>Set by Battle when a win condition is met; read by PostGame.</summary>
    public string lastWinnerName = string.Empty;

    [SerializeField] private int playerCount;
    [SerializeField] private int maxPlayerCount;
    
    [SerializeField] private List<GameObject> playerList = new List<GameObject>();
    [TextArea]
    public bool testGamePad;
    public Player playerGamePad;
    public Camera cameraPrefab;
    public List<Camera> cameras = new List<Camera>();
    public GameObject playerPrefab;
    public GameObject displayPrefab;
    public float TestVariableY;



    //public int[] numbers = new int[5];
    [SerializeField] public List<GameObject> characterPrefabs  = new List<GameObject>();

    /// <summary>
    /// One sprite per character, matched by index to characterPrefabs.
    /// Assign in the Inspector after taking screenshots of each model.
    /// </summary>
    [SerializeField] public List<Sprite> characterThumbnails = new List<Sprite>();

    public bool[] isJoinConfirmed;
    public bool[] isCharacterSelect;

    /// <summary>Tracks each player's currently browsed / confirmed colour index.</summary>
    public int[] characterIndex;
    
    [SerializeField]
    public List<GameObject> playerSlot = new List<GameObject>();
    
    
    
    [SerializeField] public Dictionary<string,IGameState> states = new Dictionary<string, IGameState>();
    [SerializeField] public IGameState currentState;


    public Canvas canvasPrefab;

    public GameObject CursorPrefab;

    /// <summary>
    /// The split-screen viewport border. Created by Battle on first load (2+ players),
    /// reused on Replay, and destroyed by PostGame when leaving to CharacterSelect or SplashScreen.
    /// </summary>
    [NonSerialized] public CameraViewportBorder viewportBorder;
    



    public void ChangeState(string state)
    {
        // Let the outgoing state clean up before we hand off.
        currentState?.OnExit();

        IGameState result = null;
        if (states.TryGetValue(state, out result))
        {
            result.OnLoad();
            this.currentState = result;
        }
        Debug.Log(this.currentState.ToString());
    }


    public Viewport[] ScreenQuadrant =
    {
        new Viewport(0.0f, 0.5f, 0.5f, 0.5f),
        new Viewport(0.5f, 0.5f, 0.5f, 0.5f),
        new Viewport(0.0f, 0.0f, 0.5f, 0.5f),
        new Viewport(0.5f, 0.0f, 0.5f, 0.5f),
    };
    //new Viewport(0,0.5,0.5,1);
    // Start is called once before the first execution of Update after the MonoBehaviour is created


    uint qsize = 15;
    Queue myLogQueue = new Queue();

    



    void HandleLog(string logString, string stackTrace, LogType type)
    {
        myLogQueue.Enqueue("{" + type + "] :" + logString);
        myLogQueue.Enqueue(stackTrace);
        while (myLogQueue.Count > qsize)
            myLogQueue.Dequeue();
    }

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;

    }






    private void Awake()
    {
        states.Add("SplashScreen",    new SplashScreen(this));
        states.Add("Menu",            new Menu(this));
        states.Add("Lobby",           new Lobby(this));
        states.Add("CharacterSelect", new CharacterSelect(this));
        states.Add("PreGame",         new PreGame(this));
        states.Add("Battle",          new Battle(this));
        states.Add("PostGame",        new PostGame(this));
    }

    void Start()
    {
        // Only load the first state. All subsequent OnLoad() calls are
        // triggered by ChangeState() as the player moves through the flow.
        playerGamePad = ReInput.players.GetPlayer(0);
        ChangeState("SplashScreen");
    }

    // Update is called once per frame
    void Update()
    {
        currentState.OnUpdate();

        
        //testGamePad = playerGamePad.GetButton("X");

        //if (playerGamePad.GetButtonDown("X"))
        //{
        //    AddNewPlayer();
        //}
        //MeshRenderer mesh = cube.GetComponent<MeshRenderer>();

        //if (playerGamePad.GetButtonDown("A"))
        //{
        //    Debug.Log("Down");
        //    Debug.Log(mesh.enabled = true);


        //}
        //if (playerGamePad.GetButtonUp("A")) 
        //{
        //    Debug.Log("Up");
        //    Debug.Log(mesh.enabled = false);
        //}
    }
    

    
    /// <summary>
    /// Renames every player slot sequentially (_player 1, _player 2, …).
    /// Called by Lobby and CharacterSelect whenever the player list changes.
    /// </summary>
    public void SetPlayerNames()
    {
        for (int i = 0; i < playerSlot.Count; i++)
        {
            string name = $"_player {i + 1}";
            playerSlot[i].GetComponent<LocalPlayerManager>().InitializePlayerName(name);
        }
    }

    public void AddNewPlayer()
    {
        if (playerCount < maxPlayerCount)
        {
            //Debug.Log(playerCount.ToString() + " Before");
            playerCount += 1;
           // Debug.Log(playerCount.ToString() + " After");
            /*GameObject playerObject = GameObject.Instantiate
                (
                playerPrefab,
                new Vector3(Random.Range(0, 10), Random.Range(0, 10), Random.Range(0, 10)),
                Quaternion.identity
                );
            playerList.Add(playerObject);
            LocalPlayer _player = playerObject.GetComponent<LocalPlayer>();
            _player.CreateName(playerCount);
            if (playerCount <= 4)
            {
                _player.myCamera.rect = new Rect
                    (
                    ScreenQuadrant[playerCount - 1].X,
                    ScreenQuadrant[playerCount - 1].Y,
                    ScreenQuadrant[playerCount - 1].Width,
                    ScreenQuadrant[playerCount - 1].Height
                    );

                //_player.myCamera.gameObject.SetActive(false);*/
            cameras.Add(GameObject.Instantiate(cameraPrefab, new Vector3(UnityEngine.Random.Range(0, 20), UnityEngine.Random.Range(0, 20), UnityEngine.Random.Range(0, 20)), Quaternion.identity));

            for (int i = 0; i < playerCount; i++)
            {

                Debug.Log((i).ToString() + " Width = " + (i * .25).ToString());
                cameras[i].rect = new Rect((i * .25f), cameras[i].rect.y, .25f, .25f);
            }

        }
        else
        {

        }


    }

    

}

public class Viewport
{
    public float X { get { return x; } }
    public float Y { get{ return y; } }
    public float Width { get { return width; } }
    public float Height { get { return height; } }

    float x;
    float y;
    float width;
    float height;

    Dictionary<int, float> viewportInfo = new Dictionary<int, float>();


    public Viewport(float x, float y, float width, float height)
    {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;

        // 2, 4, 6, 8, 10, 12, 14, 16

        
    }

    

    
    
}


