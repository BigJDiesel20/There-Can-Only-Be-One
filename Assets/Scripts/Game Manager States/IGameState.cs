using System;
using UnityEngine;


public interface IGameState
{
    public abstract GameManager gameManager { get; set; }

    
    public void OnLoad() { }

    public void OnUpdate() { }
   
    public void ChangeState(string state) { }

}