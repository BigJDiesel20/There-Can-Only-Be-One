using UnityEngine;
using Rewired;
using System;
[Serializable]
public class GamePlay : IGameState
{ 


    public GamePlay(GameManager gameManager)
    {
        this.gameManager = gameManager;
        GamePad = gameManager.playerGamePad;
    }

    public GameManager gameManager { get; set; }
    Rewired.Player GamePad;

    public void OnLoad()
    {
        SetVeiwPort();
        SpawnCharacters();
    }

    public void OnUpdate()
    {
        //use confirmed player count to esabish camera layout.
        //spawn chosen character from player slots characters.
        //Set sawn characters camera to the apropriate rect position.


        // Game won when only one player left

    }
    public void OnOnOnGUI() { }
    public void ChangeState(string state) { }

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



