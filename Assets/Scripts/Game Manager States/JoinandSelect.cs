using UnityEngine;
using Rewired;
using NUnit.Framework;
using System.Collections.Generic;
using System.Text;
using System;
using Unity.VisualScripting;
using UnityEngine.TextCore.Text;
using static UnityEngine.Rendering.DebugUI.Table;
using TMPro;
using System.Security.Cryptography;
using System.Threading.Tasks;

[Serializable]
public class JoinandSelect : IGameState
{

    public GameManager gameManager { get; set; }
    int[] characterIndex = new int[ReInput.players.playerCount];

    Player GamePad;
    public JoinandSelect(GameManager gameManager)
    {
        this.gameManager = gameManager;
        gameManager.isJoinConfirmed = new bool[ReInput.players.playerCount];
        gameManager.isCharacterSelect = new bool[ReInput.players.playerCount];
    }

    int activePlayers = 0;
    int[] playerPostions = new int[ReInput.players.playerCount];
    (bool isTrue, bool isFalse) check;

   
    public void OnLoad()
    {
        for (int i = 0; i < characterIndex.Length; i++)
        {
            
           
               
                characterIndex[i] = UnityEngine.Random.Range(0, gameManager.characterPrefabs.Count);

            

             
        }
    }
   
    public void OnUpdate()
    {

        (bool isTrue, bool isFalse) check = (false, false);

        //Debug.Log("J&S Contoller Count " + ReInput.players.playerCount.ToString());
        //Debug.Log("J&S isJoinConfired " + gameManager.isJoinConfirmed.Length);


        for (int i = 0; i < ReInput.players.playerCount; i++)
        {
            Rewired.Player GamePad = ReInput.players.GetPlayer(i); // Click button to join
            //Debug.Log("GamePad: " + GamePad.id.ToString());

            // Press button to join game. Setup player
            if (GamePad.GetButtonDown("A") & gameManager.isJoinConfirmed[i] == false & gameManager.isCharacterSelect[i] == false)
            {
                activePlayers++;

                //Debug.Log(GamePad.id.ToString() + " " + GamePad.name);

                GameObject playerObject = GameObject.Instantiate(gameManager.playerPrefab);
                playerObject.SetActive(false);
                gameManager.playerSlot.Add(playerObject);
                LocalPlayerManager localPlayer;
                if (playerObject.GetComponent<LocalPlayerManager>() != null)
                {
                    localPlayer = playerObject.GetComponent<LocalPlayerManager>();
                }
                else
                {
                    localPlayer = playerObject.AddComponent<LocalPlayerManager>();

                }

                Debug.Log($"Check {localPlayer}");
                localPlayer.InitializePlayer(GamePad);
                //localPlayer.playerGamePad = GamePad;


                SetPlayerNames();
                Debug.Log("id: " + GamePad.id);
                gameManager.isJoinConfirmed[i] = true;


            }
            // Select Character
            else if (GamePad.GetButtonDown("A") & gameManager.isJoinConfirmed[i] == true & gameManager.isCharacterSelect[i] == false)
            {
                //gameManager.characterPrefabs[characterIndex]

                for (int j = 0; j < gameManager.playerSlot.Count; j++)
                {
                    if (gameManager.playerSlot[j].GetComponent<LocalPlayerManager>().playerGamePad.id == GamePad.id)
                    {
                        LocalPlayerManager localPlayer = gameManager.playerSlot[j].GetComponent<LocalPlayerManager>();
                        GameObject character = GameObject.Instantiate(gameManager.characterPrefabs[characterIndex[i]], new Vector3(), Quaternion.identity, gameManager.playerSlot[j].transform);
                        character.name = $"Player{j + 1} {character.name}";
                        character.tag = "Player";                        
                        GameObject displayObject = GameObject.Instantiate(gameManager.displayPrefab);
                        Canvas canvas = GameObject.Instantiate(gameManager.canvasPrefab).GetComponent<Canvas>();
                        GameObject cursor = GameObject.Instantiate(gameManager.CursorPrefab);
                        int layerIndex = LayerMask.NameToLayer($"P{j + 1}Visible");                        
                        Debug.LogError(LayerMask.LayerToName(layerIndex));
                        cursor.layer = (layerIndex == -1)? cursor.layer: layerIndex;
                        string cameraCullingMask = $"P{j + 1}Visible";

                        localPlayer.InitializePlayerCharacter(character, displayObject, canvas, cursor, cameraCullingMask);
                        SetPlayerNames();

                        // gameManager.playerSlot[j].GetComponent<LocalPlayer>().character = character;
                        //GameObject cameraObject = new GameObject("Camera");
                        //cameraObject.transform.parent = character.transform;
                        //cameraObject.transform.position = new Vector3(0, 1.49f, -5.07f);
                        //Camera camera = cameraObject.AddComponent<Camera>();                        
                        //gameManager.playerSlot[j].GetComponent<LocalPlayer>().myCamera = camera;

                        //displayObject.transform.position = new Vector3(0, 2, 0);
                        //gameManager.playerSlot[j].GetComponent<LocalPlayer>().movementController = new Movement(gameManager.playerSlot[j].GetComponent<LocalPlayer>());
                    }
                }

                gameManager.isCharacterSelect[i] = true;
            }
            // If all joined players have selected there characters start game.
            else if (GamePad.GetButtonDown("A") & gameManager.isJoinConfirmed[i] == true & gameManager.isCharacterSelect[i] == true)
            {
                for (int j = 0; j < gameManager.isJoinConfirmed.Length; j++)
                {
                    if (gameManager.isJoinConfirmed[j] == true)
                    {
                        if (gameManager.isCharacterSelect[j] == true)
                        {
                            check.isTrue = true;
                        }
                        else
                        {
                            check.isFalse = true;
                        }
                    }
                }
                if (check.isTrue == true & check.isFalse == false)
                {
                    gameManager.compltedSelections = true;
                }
                else
                {
                    gameManager.compltedSelections = false;
                }
                if (gameManager.compltedSelections == true)
                {
                    //SetVeiwPort();
                    //SpawnCharacters();
                    ChangeState("GamePlay");
                }
            }
            // Delete player object and remove player slot in list.
            if (GamePad.GetButtonDown("B") & gameManager.isJoinConfirmed[i] == true & gameManager.isCharacterSelect[i] == false)
            {
                int index = 0;
                bool isfound = false;
                for (int j = 0; j < gameManager.playerSlot.Count; j++)
                {
                    if (gameManager.playerSlot[j].GetComponent<LocalPlayerManager>().playerGamePad.id == GamePad.id)
                    {
                        LocalPlayerManager player = gameManager.playerSlot[j].GetComponent<LocalPlayerManager>();
                        player.DeactivatePlayer(player.playerGamePad);
                        //player.playerGamePad = null;
                        //player.playerName = null;
                        index = j;
                        isfound = true;
                    }
                }
                if (isfound == true)
                {

                    GameObject playerObject = gameManager.playerSlot[index];
                    gameManager.playerSlot.Remove(playerObject);
                    GameObject.Destroy(playerObject);
                    SetPlayerNames();
                }
                gameManager.isJoinConfirmed[i] = false;
            }
            // Delete Selected Character
            else if (GamePad.GetButtonDown("B") & gameManager.isJoinConfirmed[i] == true & gameManager.isCharacterSelect[i] == true)
            {


                for (int j = 0; j < gameManager.playerSlot.Count; j++)
                {
                    if (gameManager.playerSlot[j].GetComponent<LocalPlayerManager>().playerGamePad.id == GamePad.id)
                    {
                        LocalPlayerManager localPlayer = gameManager.playerSlot[j].GetComponent<LocalPlayerManager>();
                        localPlayer.DeactivatePlayerCharacter();
                        //GameObject.Destroy(gameManager.playerSlot[j].GetComponent<LocalPlayer>().character);
                        //GameObject.Destroy(gameManager.playerSlot[j].GetComponent<LocalPlayer>().displayName);
                    }
                }

                gameManager.isCharacterSelect[i] = false;
            }
            // View Character deincrement
            if (GamePad.GetButtonDown("D-Pad Left") & gameManager.isJoinConfirmed[i] == true & gameManager.isCharacterSelect[i] == false)
            {
                characterIndex[i]--;
                characterIndex[i] = (characterIndex[i] < 0) ? gameManager.characterPrefabs.Count - 1 : (characterIndex[i] >= gameManager.characterPrefabs.Count) ? 0 : characterIndex[i];
                Debug.Log("Character: " + gameManager.characterPrefabs[characterIndex[i]].ToString());

            }
            // View Character increment
            if (GamePad.GetButtonDown("D-Pad Right") & gameManager.isJoinConfirmed[i] == true & gameManager.isCharacterSelect[i] == false)
            {
                characterIndex[i]--;
                characterIndex[i] = (characterIndex[i] < 0) ? gameManager.characterPrefabs.Count - 1 : (characterIndex[i] >= gameManager.characterPrefabs.Count) ? 0 : characterIndex[i];
                Debug.Log("Character: " + gameManager.characterPrefabs[characterIndex[i]].ToString());
            }





        }

        activePlayers = gameManager.playerSlot.Count;
    }

    Rewired.Player GamePadtest;


    public void ChangeState(string state)
    {
        gameManager.ChangeState(state);
    }


    public void SetPlayerNames()
    {
        for (int i = 0; i < gameManager.playerSlot.Count; i++)
        {
            StringBuilder name = new StringBuilder();
            name.Append("player ");
            name.Append(i + 1);
            gameManager.playerSlot[i].GetComponent<LocalPlayerManager>().InitializePlayerName(name.ToString());
            Debug.Log(i + 1);
        }
    }
    /// <summary>
    ///  line 145
    /// </summary>
    void SetVeiwPort()
    {
        int playerCount = gameManager.playerSlot.Count;
        float rows = 0f;
        float colums = 0f;
        float x = 0f;
        float y = 0f;
        float width = 1f;
        float height = 1f;

        //Debug.Log("player Count: " + gameManager.playerSlot.Count);
        if (playerCount == 1) { rows = 1; colums = 1; width = 1; height = 1; x = 0; y = 0; Debug.Log("playerCount == 1"); }
        else if (playerCount <= 2) { rows = 2; colums = 1; width = .5f/*1 / rows*/; height = .5f /*(1 / colums) / 2*/; x = width / 2; y = height; Debug.Log("playerCount <= 2"); } //if number of players are <= 2 (rows = 2 colums = 1 )
        else if (playerCount > 2 & playerCount <= 4) { rows = 2; colums = 2; width = 1 / rows; height = 1 / colums; x = width / 2; y = .5f; Debug.Log("playerCount > 2 & playerCount <= 4"); } //if number of players are > 2 & < 4 (rows = 2 colums = 2 )
        else if (playerCount > 4 & playerCount <= 9) { rows = 3; colums = 3; width = 1 / rows; height = 1 / colums; x = width; y = height; Debug.Log("playerCount > 4 & playerCount <= 9"); }// if number of players are > 4 & < 9 (rows = 3 colums = 3)
        else if (playerCount > 9 & playerCount <= 12) { rows = 3; colums = 4; width = 1 / rows; height = 1 / colums; x = width; y = height; Debug.Log("playerCount > 9 & playerCount <= 12"); }// if number of players are > 9 & < 12 (rows = 3 colums = 4)
        else if (playerCount > 12 & playerCount <= 16) { rows = 4; colums = 4; width = 1 / rows; height = 1 / colums; x = width; y = height; Debug.Log("playerCount > 12 & playerCount <= 16"); }// if number of players are > 12 & < 16 (rows = 4 colum = 4)

        Debug.Log("Row: " + rows.ToString() + " " + (1 / rows).ToString() + " Colums: " + colums.ToString() + " " + (1 / colums).ToString());


        //Debug.Log("Width: " + width.ToString() + " Height: " + height.ToString());
        int index = 0;
        if (playerCount >= 1)
        {
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < colums; j++)
                {

                    //int index = ((i + 1) * (j + 1)) - 1;
                    if (index < playerCount)
                    {
                        LocalPlayerManager player = gameManager.playerSlot[index].GetComponent<LocalPlayerManager>();
                        x = i * x;
                        y = 1 - (j * y) - height;
                        Rect cameraViewport = new Rect(i * (1 / rows), (1 - (1 / colums)) - (j * (1 / colums)), (1 / rows), (1 / colums));
                        Debug.Log(cameraViewport);
                        player.SetCameraRect(cameraViewport);
                        //Debug.Log("player: "+ player.gameObject.name + " Index: " + index.ToString());                        
                        index++;

                    }
                }
            }
        }

        // Get number of players
        // if 1 player 1 rows x 1 colums x = 0 y = 0 width = 1 height = 1
        // if 2 player 2 rows x 1 colums x = (1 - wdith)/2 y = .5f width = .5f height = .5f
        // if 4 players 2 rows x 2 colums x = (for(i < row) i * width) y = 1 - (for(i < colum) i * height) - height  width = 1/row height = 1/colums;
        // if 9 players 3 rows x 3 colums x = (for(i < row) i * width) y = 1 - (for(i < colum) i * height) - height  width = 1/row height = 1/colums;
        // if 16 players 4 rows x 4 colums x = (for(i < row) i * width) y = 1 - (for(i < colum) i * height) - height  width = 1/row height = 1/colums;
        // if number of players 
        // if number of players are > 2 & < 4 (rows = 2 colums = 2 )
        // if number of players are > 4 & < 9 (rows = 3 colums = 3)
        // if number of players are > 9 & < 12 (rows = 3 colums = 4)
        // if number of players are > 12 & < 16 (rows = 4 colum = 4)
        // for(int i = 0, i < row, i++)
        // {
        // if (i == row -1) colums = colums - cameraCount
        // for (int j = 0, j < colums,j++)
        // {
        // if
        // }
        // if number of players is > 4 and < 9 3x3
        // The divide the _max grid layout by number of rows
    }
    //line 145
    void SpawnCharacters()
    {
        for (int i = 0; i < gameManager.playerSlot.Count; i++)
        {
            Vector3 randomposition = new Vector3(UnityEngine.Random.Range(0, 20), 0, UnityEngine.Random.Range(0, 20)); 
            var colliders = Physics.OverlapBox(randomposition, Vector3.one, Quaternion.identity, LayerMask.GetMask("Ground"));
            int breakLimit = 0;
            while (colliders.Length != 0)
            {
                randomposition = new Vector3(UnityEngine.Random.Range(0, 10), UnityEngine.Random.Range(0, 10), UnityEngine.Random.Range(0, 10));
                colliders = Physics.OverlapBox(randomposition, Vector3.one, Quaternion.identity, LayerMask.GetMask("Ground"));
                breakLimit++;
                Debug.Log($"Break Limite: {breakLimit}");
                if (breakLimit > 999999)
                {
                    Debug.Log($"Break Limite: {breakLimit}");
                    break;
                }
            }

            gameManager.playerSlot[i].GetComponent<LocalPlayerManager>().character.transform.position = randomposition;
            gameManager.playerSlot[i].gameObject.SetActive(true);



        }
            
           

            
        
    }
}
