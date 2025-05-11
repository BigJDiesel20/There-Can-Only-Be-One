

using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;
using System.Collections;
using UnityEngine.Rendering;
using Rewired;
using UnityEngine.Events;
using TMPro.Examples;

[Serializable]
public class TeamController
{
    private MonoBehaviour monoBehaviour;
    private Player gamePad;
    private LocalPlayerManager player;
    LocalPlayerManager Taeget;
    [SerializeField]
    private GameObject cameraTarget;
    [SerializeField]
    private Status targetStaus; 
    [SerializeField]
    private RaycastHit hitTarget;
    private bool isHit;
    private double messageDuration = 10;
    
    [SerializeField]
    
    public enum Status { Solo, Leader, Follower }
    [SerializeField]
    private Status _currentStatus;
    public Status CurrentStatus { get { return _currentStatus; } set { _currentStatus = value; onStatusChange?.Invoke(_currentStatus); } }

    private PlayerEvents playerEvents;
    public UnityAction<Status> onStatusChange;

    [SerializeField]
    public Team team;

    
    

    
    
    private int voteTally = 0; // The number of team members that voting to remove the leader
    private int hasVoted = 0; // How many team memebers have voted in total.
    private Coroutine startMunity = null;
    public int testvar = 5000;
    private bool IsVoteCompleted()
    {
        if (hasVoted == team.GetFollowers().Count) return true; else return false;
    }

    private bool isInitialized = false;
    private bool _isHitConfirmPause;

    public bool IsInitialized { get { return isInitialized; } }


    public void OnUpdate()
    {
        if (isHit)
        {
            if (gamePad.GetButtonDown("Right Stick Button"))
            {
                


                LocalPlayerManager otherPlayer = hitTarget.transform.GetComponentInParent<LocalPlayerManager>();
                Debug.Log(hitTarget.transform.gameObject);

                if (otherPlayer != null)
                {
                    
                    if (team != null)
                    {
                        // If I am teamed and i taget a teammember
                        if (team.IsCurrentMember(otherPlayer))
                        {
                            
                            player.LaunchMessage("Choose Action", ()=> { QuitTeam();}, ()=> { Mutiny(otherPlayer); },() => { KickMember(otherPlayer); }, () => { } ,("QuitTeam","Mutiny" ,"KickMember","Exit"  ), 10);
                        }
                        else // If im teamed and I target a non teammember
                        {
                            player.LaunchMessage("Choose Action", () => { Invite(otherPlayer); }, () => { JoinRequest(otherPlayer); }, () => { }, ("Invite", "JoinRequest", "Exit"), 10);
                        }
                        
                    }
                    else
                    {
                        player.LaunchMessage("Choose Action", () => { Invite(otherPlayer); }, () => { JoinRequest(otherPlayer); }, () => { }, ("Invite", "JoinRequest", "Exit"), 10);
                        //invite other player doesn't have a team                                        
                    }
                    // = (otherPlayer.teamController.team.CurrentStatus //  == Team.Status.Leader);

                    //UnityAction invite = () => { AddMember(otherPlayer); Debug.Log($"{otherPlayer.name} acepts invatation to {player.name} team"); };
                    //UnityAction JoinRequest = () => { otherPlayer.teamController.AddMember(player); Debug.Log($"{otherPlayer.name} acepts {player.name} request to join there team"); };
                    //UnityAction Quite = () => { otherPlayer.teamController.RemoveMember(player);Debug.Log($"{player} quites {otherPlayer} team") };
                    //UnityAction Kick = () => { RemoveMember(otherPlayer); Debug.Log($"Kick {otherPlayer} off of {player} team ") };
                    //UnityAction Muntiny = () => {V };

                    //if (otherPlayer.teamController.Leader == null or)
                    //{
                    //    otherPlayer.LaunchMessage("Do you want to join my team?", invite);
                    //}
                    //otherPlayer.LaunchMessaage("May I Join your team");
                }


                //CharacterController characterController;

            }


            if (gamePad.GetButtonDown("Right Trigger"))
            {
                if (team != null)
                {
                    LocalPlayerManager otherPlayer = hitTarget.transform.GetComponentInParent<LocalPlayerManager>();
                    if (team.IsCurrentMember(otherPlayer))
                    {
                        KickMember(otherPlayer);
                    }
                }
                
            }

            if (gamePad.GetButtonDown("Left Trigger"))
            {
                if (team != null)
                {

                    QuitTeam();
                    
                    
                }
            }

        }
    }

    private void JoinRequest(LocalPlayerManager otherPlayer)
    {

        UnityAction OnOtherPlayerConfirm;
        UnityAction OnLeaderConfirm;
        Debug.Log($"{player.name}: {player.CurrentTeamStatus} {otherPlayer.name}: {otherPlayer.CurrentTeamStatus}");
        switch (player.CurrentTeamStatus, otherPlayer.CurrentTeamStatus)
        {

            case (Status.Solo, Status.Solo):

                OnOtherPlayerConfirm = () =>
                {                    
                    otherPlayer.teamController.team.AddMember(otherPlayer);
                    otherPlayer.teamController.team.AddMember(player);
                    otherPlayer.teamController.team = this.team;
                };
                team = new Team();
                otherPlayer.LaunchMessage($"Can we team up!", OnOtherPlayerConfirm, "Accept", messageDuration);

                break; 

            case (Status.Solo, Status.Leader):
                OnOtherPlayerConfirm = () =>
                {
                    Debug.Log($"Status.Solo, Status.Leader");
                    otherPlayer.teamController.team.RemoveAllMembers();
                    team.AddMember(player);
                    team.AddMember(otherPlayer);
                    otherPlayer.teamController.team = this.team;
                };
                otherPlayer.LaunchMessage($"Allow me to join your Team!", OnOtherPlayerConfirm, "Accept", messageDuration);
                break;

            case (Status.Solo, Status.Follower):
                OnOtherPlayerConfirm = () =>
                {
                    UnityAction LearderConfirm = () =>
                    {
                        otherPlayer.teamController.team.GetLeader().teamController.team.AddMember(player);
                        player.teamController.team = otherPlayer.teamController.team.GetLeader().teamController.team;
                    };
                    otherPlayer.teamController.team.GetLeader().LaunchMessage($"Can {player.name} join our team",LearderConfirm, "Accept", messageDuration);
                };

                
                otherPlayer.LaunchMessage($"Can I join join your team", OnOtherPlayerConfirm, "Ask", messageDuration);
                break;


            case (Status.Leader, Status.Solo):
                OnOtherPlayerConfirm = () =>
                {

                    team.RemoveAllMembers();
                    otherPlayer.teamController.team.AddMember(otherPlayer);
                    otherPlayer.teamController.team.AddMember(player);
                    player.teamController.team = otherPlayer.teamController.team;
                };
                otherPlayer.LaunchMessage($"I'll abandon my team if I can join you!", OnOtherPlayerConfirm, "Accept", messageDuration);
                break;
            case (Status.Leader, Status.Leader):
                OnOtherPlayerConfirm = () =>
                {
                    team.RemoveAllMembers();
                    otherPlayer.teamController.team.AddMember(player);
                     this.team= otherPlayer.teamController.team;
                };
                otherPlayer.LaunchMessage($"I'll Abandon my team if I can join Yours!", OnOtherPlayerConfirm, "Accept", messageDuration);
                break;
            case (Status.Leader, Status.Follower):
                OnOtherPlayerConfirm = () =>
                {
                    UnityAction OnLeaderConfirm = () =>
                    {
                        otherPlayer.teamController.team.GetLeader().teamController.team.AddMember(player);
                        this.team = otherPlayer.teamController.team.GetLeader().teamController.team;
                    };
                    otherPlayer.teamController.team.GetLeader().LaunchMessage($"Can {player.name} Join our team", OnLeaderConfirm, "Accept",messageDuration);
                };
                otherPlayer.LaunchMessage($"Leave your Team and Join My Team!", OnOtherPlayerConfirm, "Ask Leader", messageDuration);
                break;


            case (Status.Follower, Status.Solo):

                
                    OnOtherPlayerConfirm = () =>
                    {
                        team.RemoveMember(player);
                        otherPlayer.teamController.team.AddMember(otherPlayer);
                        otherPlayer.teamController.team.AddMember(player);
                        this.team = otherPlayer.teamController.team;
                    };

                   
                

                team.GetLeader().LaunchMessage($"Can I join you", OnOtherPlayerConfirm, () => { }, ("Accept", "Reject"), messageDuration);
                break;


            case (Status.Follower, Status.Leader):

                
                    OnOtherPlayerConfirm = () =>
                    {
                        team.RemoveMember(player);
                        otherPlayer.teamController.team.AddMember(player);
                        this.team = otherPlayer.teamController.team;
                    };
                   
                

                team.GetLeader().LaunchMessage($"Can I join your team?", OnOtherPlayerConfirm, () =>{ }, ("Yes", "No"), messageDuration);
                break;

            case (Status.Follower, Status.Follower):
                OnLeaderConfirm = () =>
                {
                    OnOtherPlayerConfirm = () =>
                    {

                        team.RemoveMember(player);
                        otherPlayer.teamController.team.GetLeader().teamController.team.AddMember(player);
                        this.team = otherPlayer.teamController.team;
                    };
                    otherPlayer.LaunchMessage($"Can {player.name} join our team", OnOtherPlayerConfirm, "Confirm", messageDuration);
                };

                team.GetLeader().LaunchMessage($"Can I join your team?", OnLeaderConfirm, () => { }, ("Yes", "No"), messageDuration);
                break;

        }






       
    }

    public void Invite(LocalPlayerManager otherPlayer)
    {
        UnityAction OnOtherPlayerConfirm;
        UnityAction OnLeaderConfirm;
        Debug.Log($"{player.name}: {player.CurrentTeamStatus} {otherPlayer.name}: {otherPlayer.CurrentTeamStatus}" );
        switch (player.CurrentTeamStatus, otherPlayer.CurrentTeamStatus)
        {
            case (Status.Solo, Status.Solo):

                OnOtherPlayerConfirm = () =>
                {
                    team.AddMember(player);
                    team.AddMember(otherPlayer);
                    otherPlayer.teamController.team = this.team;
                };
                team = new Team();
                otherPlayer.LaunchMessage($"Lets Team up!", OnOtherPlayerConfirm, "Team Up", messageDuration);

                break;
            case (Status.Solo, Status.Leader):
                OnOtherPlayerConfirm = () =>
                {
                    Debug.Log($"Status.Solo, Status.Leader");
                    otherPlayer.teamController.team.RemoveAllMembers();
                    team.AddMember(player);
                    team.AddMember(otherPlayer);
                    otherPlayer.teamController.team = this.team;
                };
                otherPlayer.LaunchMessage($"Abandon your Team and Lets Team up!", OnOtherPlayerConfirm, "Team Up", messageDuration);
                break;

            case (Status.Solo, Status.Follower):
                OnOtherPlayerConfirm = () =>
                {
                    otherPlayer.teamController.team.RemoveMember(otherPlayer);
                    team.AddMember(player);
                    team.AddMember(otherPlayer);
                    otherPlayer.teamController.team = this.team;
                };
                otherPlayer.LaunchMessage($"Leave your Team and Lets Team up!", OnOtherPlayerConfirm, "Team Up", messageDuration);
                break;


            case (Status.Leader, Status.Solo):
                OnOtherPlayerConfirm = () =>
                {

                    team.AddMember(otherPlayer);
                    otherPlayer.teamController.team = this.team;
                };
                otherPlayer.LaunchMessage($"Join my Team!", OnOtherPlayerConfirm, "Follow", messageDuration);
                break;
            case (Status.Leader, Status.Leader):
                OnOtherPlayerConfirm = () =>
                {
                    otherPlayer.teamController.team.RemoveAllMembers();
                    team.AddMember(otherPlayer);
                    otherPlayer.teamController.team = this.team;
                };
                otherPlayer.LaunchMessage($"Abandon your team and Join my Team!", OnOtherPlayerConfirm, "Follow", messageDuration);
                break;
            case (Status.Leader, Status.Follower):
                OnOtherPlayerConfirm = () =>
                {
                    otherPlayer.teamController.team.RemoveMember(otherPlayer);
                    team.AddMember(otherPlayer);
                    otherPlayer.teamController.team = this.team;
                };
                otherPlayer.LaunchMessage($"Leave your Team and Join My Team!", OnOtherPlayerConfirm, "Follow", messageDuration);
                break;


            case (Status.Follower, Status.Solo):
                
                OnLeaderConfirm = () =>

                {
                    OnOtherPlayerConfirm = () =>
                    {
                        team.GetLeader().teamController.Invite(otherPlayer);
                        otherPlayer.teamController.team = team.GetLeader().teamController.team;
                    };

                    otherPlayer.LaunchMessage($"Our Leader says you can join our team", OnOtherPlayerConfirm, "Follow", messageDuration);
                };
                
                team.GetLeader().LaunchMessage($"Can I invite {otherPlayer.playerName} to join our team?", OnLeaderConfirm, () => { }, ("Yes", "No"), messageDuration);
                break;
        

            case (Status.Follower, Status.Leader):

                OnLeaderConfirm = () =>
                {
                    OnOtherPlayerConfirm = () => 
                    { 
                        otherPlayer.teamController.team.RemoveAllMembers();                        
                        team.GetLeader().teamController.Invite(otherPlayer); 
                        otherPlayer.teamController.team = team.GetLeader().teamController.team; 
                    };
                    otherPlayer.LaunchMessage($"Abandon your team and follow our Leader", OnOtherPlayerConfirm,"Follow", messageDuration);
                };
                
                team.GetLeader().LaunchMessage($"Can I invite {otherPlayer.playerName} to join our team?", OnLeaderConfirm, () => { },("Yes", "No") , messageDuration);
                break;
                
            case (Status.Follower, Status.Follower):
                OnLeaderConfirm = () =>
                {
                    OnOtherPlayerConfirm = () =>
                    {
                        otherPlayer.teamController.team.RemoveMember(otherPlayer);
                        team.GetLeader().teamController.Invite(otherPlayer);
                        otherPlayer.teamController.team = team.GetLeader().teamController.team;
                    };
                    otherPlayer.LaunchMessage($"Our Leader says you can join our team", OnOtherPlayerConfirm,"Follow", messageDuration);
                };
                
                team.GetLeader().LaunchMessage($"Can I to invite {otherPlayer.playerName} to join our team?", OnLeaderConfirm, () => { },("Yes", "No"), messageDuration);
                break;

        }


    }

    void KickMember(LocalPlayerManager otherPlayer)
    {
        switch(player.CurrentTeamStatus)
        {
            case Status.Leader:
                Debug.Log("Called");
                team.RemoveMember(otherPlayer);                
                break;
            case Status.Follower:
                UnityAction onLeaderConfirm = () => 
                { 
                    team.RemoveMember(otherPlayer);
                };
               team.GetLeader().LaunchMessage($"Please Kick {otherPlayer} from the team", onLeaderConfirm, "Kick", messageDuration); 
                break;
            case Status.Solo:                
                break;
        }

        
        //if (Members.ContainsKey(otherPlayer.playerName))
        //{
        //    Members.Remove(otherPlayer.playerName);
        //    if (Members.Count <= 0)
        //    {
        //        Leader = null;
        //    }
        //    if (Members.Count != 0) DebugLogs();
        //}
    }

    void QuitTeam()
    {
        if (team != null)
        {
           

            
            switch (player.CurrentTeamStatus)
            {
                case Status.Leader:
                    Debug.Log("Called");                    
                    player.LaunchMessage($"Do you want to quit your team?", () => { team.RemoveAllMembers(); }, () => { }, ("KickMember", "No"), messageDuration);
                    break;
                case Status.Follower:                    
                    player.LaunchMessage($"Do you want to quit your team?", () => { team.RemoveMember(player); }, () => { }, ("KickMember", "No"), messageDuration);
                    break;
                case Status.Solo:                    
                    break;

            }
        }
    }

    void DebugLogs()
    {
        foreach (var members in team.GetFollowers())
        {
            Debug.Log(members.ToString());
        }
    }
    void Mutiny(LocalPlayerManager leader)
    {
        if (team != null)
        {
            switch (player.CurrentTeamStatus)
            {
                case Status.Leader:                    
                    break;
                case Status.Follower:

                    if (team.IsCurrentMember(leader)& leader.CurrentTeamStatus == Status.Leader)
                    {
                        if (startMunity == null)
                        {
                            startMunity = monoBehaviour.StartCoroutine(VoteQue(leader));
                            UnityAction onConfirm = () => { };
                            leader.LaunchMessage($"{player.name} seeks to depose you!", onConfirm, () => { },("Revenge","Forgive"), 10);
                            List<LocalPlayerManager> followers = team.GetFollowers();
                            for (int i = 0; i < followers.Count; i++)
                            {
                                UnityAction onMemberConfirm = () => { VoteYes(); };
                                UnityAction onMemberReject = () => { VoteNo(); };
                                followers[i].LaunchMessage($"Depose the Leader Yay or Nay", onMemberConfirm, onMemberReject,("Yay","Nay"), 10);
                            }
                        }
                    }
                    break;
                case Status.Solo:
                    break;

            }
        }
        

    }


    IEnumerator VoteQue(LocalPlayerManager leader)
    {
        Debug.Log("VoteQue Activated");
        // Send Message Munity vote Message Box to all Temembers
        yield return new WaitUntil(IsVoteCompleted);
        Debug.Log("Vote Completed");
        if (voteTally >= team.GetFollowers().Count)
        {
            List<LocalPlayerManager> followers = leader.teamController.team.GetFollowers();
            followers.Remove(player);


            leader.teamController.team.RemoveAllMembers();

            if (followers.Count >= 1)
            {
                team.AddMember(player);



                for (int i = 0; i < followers.Count; i++)
                {

                    team.AddMember(followers[i]);
                }
            }
            hasVoted = 0;
            voteTally = 0;
            monoBehaviour.StopCoroutine(startMunity);
        }
        else
        {
            hasVoted = 0;
            voteTally = 0;
            monoBehaviour.StopCoroutine(startMunity);
        }




    }

    void VoteYes()
    {
        voteTally++;
        hasVoted++;
    }

    void VoteNo()
    {
        hasVoted++;
    }

    public void Initialize(Player gamePad, LocalPlayerManager player, ref GameObject cameraTarget, PlayerEvents playerEvents)
    {
        this.gamePad = gamePad;
        this.player = player;
        this.monoBehaviour = player.GetComponent<MonoBehaviour>();
        this.cameraTarget = cameraTarget;
        this.CurrentStatus = Status.Solo;
        this.playerEvents = playerEvents;
        this.playerEvents.OnUpdate += OnUpdate;
        this.playerEvents.TrackTarget += SetTarget;
        this.playerEvents.OnHitConfirm += OnHitConfirm;
        this.playerEvents.OnHitConfirmPauseEnd += OnHitConfirmPauseEnd;
        isInitialized = true;
    }
    public void Deactivate()
    {
        this.playerEvents.OnUpdate -= OnUpdate;
        this.playerEvents.TrackTarget -= SetTarget;
        this.playerEvents.OnHitConfirm -= OnHitConfirm;
        this.playerEvents.OnHitConfirmPauseEnd -= OnHitConfirmPauseEnd;
        isInitialized = false;
    }

    public void SetTarget(RaycastHit hitTarget, bool isHit)
    {
        this.isHit = isHit;
        this.hitTarget = hitTarget;

        if (isHit)
        {
            this.cameraTarget = hitTarget.transform.gameObject;
            this.targetStaus = hitTarget.transform.GetComponentInParent<LocalPlayerManager>().CurrentTeamStatus;
        }
        else
        {
            this.cameraTarget = null;
        }
        
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
