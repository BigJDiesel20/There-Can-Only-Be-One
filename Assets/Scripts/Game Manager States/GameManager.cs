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
    [SerializeField] public List<GameObject> characterPrefabs = new List<GameObject>();
    public bool[] isJoinConfirmed;
    public bool[] isCharacterSelect;
    public bool compltedSelections = false;
    
    [SerializeField]
    public List<GameObject> playerSlot = new List<GameObject>();
    
    
    
    [SerializeField] public Dictionary<string,IGameState> states = new Dictionary<string, IGameState>();
    [SerializeField] public IGameState currentState;


    public Canvas canvasPrefab;

    public GameObject CursorPrefab;
    



    public void ChangeState(string state)
    {
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

    private void Awake()
    {
        
        
        
        states.Add("PlayerJoin", new JoinandSelect(this));
        states.Add("CharacterSelect", new CharacterSelect(this));
        states.Add("CharacterUI", new CharacterUI(this));
        states.Add("GamePlay", new GamePlay(this));

    }
    void Start()
    {

        foreach(var state in states)
        {
            state.Value.OnLoad();
        }
        ChangeState("PlayerJoin");        
        //mesh.GetComponent<MeshRenderer>();
        playerGamePad = ReInput.players.GetPlayer(0);
        //MeshRenderer mesh = cube.GetComponent<MeshRenderer>();
        //Debug.Log(mesh.enabled = false);
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
            LocalPlayer player = playerObject.GetComponent<LocalPlayer>();
            player.CreateName(playerCount);
            if (playerCount <= 4)
            {
                player.myCamera.rect = new Rect
                    (
                    ScreenQuadrant[playerCount - 1].X,
                    ScreenQuadrant[playerCount - 1].Y,
                    ScreenQuadrant[playerCount - 1].Width,
                    ScreenQuadrant[playerCount - 1].Height
                    );

                //player.myCamera.gameObject.SetActive(false);*/
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
