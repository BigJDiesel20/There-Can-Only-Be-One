

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
    private PlayerInput gamePad;
    private LocalPlayerManager player;
    private double messageDuration = 10;
    
    //[SerializeField]  

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
    private LocalPlayerManager _orbitTarget = null;

    public bool IsInitialized { get { return isInitialized; } }


public void OnUpdate()
    {
        LocalPlayerManager targetPlayer = _orbitTarget;

        if (targetPlayer == null) return;

        if (gamePad.GetButtonDown("Right Stick Button"))
        {
            if (team != null)
            {
                if (team.IsCurrentMember(targetPlayer))
                    player.LaunchMessage("Choose Action", () => { QuitTeam(); }, () => { Mutiny(targetPlayer); }, () => { KickMember(targetPlayer); }, () => { }, ("QuitTeam", "Mutiny", "KickMember", "Exit"), 10);
                else
                    player.LaunchMessage("Choose Action", () => { Invite(targetPlayer); }, () => { JoinRequest(targetPlayer); }, () => { }, ("Invite", "JoinRequest", "Exit"), 10);
            }
            else
            {
                player.LaunchMessage("Choose Action", () => { Invite(targetPlayer); }, () => { JoinRequest(targetPlayer); }, () => { }, ("Invite", "JoinRequest", "Exit"), 10);
            }
        }

        if (gamePad.GetButtonDown("Right Trigger"))
        {
            if (team != null && team.IsCurrentMember(targetPlayer))
                KickMember(targetPlayer);
        }

        if (gamePad.GetButtonDown("Left Trigger"))
        {
            if (team != null)
                QuitTeam();
        }
    }

    // ── TeamRules feedback messages ───────────────────────────────────────────

    private string MaxTeamsMsg() =>
        $"Cannot form more teams — max {TeamRules.GetMaxTeams()} allowed " +
        $"(largest team has {TeamRules.GetLargestTeamSize()} members).";

    private string MaxTeamSizeMsg() =>
        $"Cannot join — team is full. " +
        $"Max {TeamRules.GetMaxTeamSize()} members per team with " +
        $"{TeamRules.GetActiveTeamCount()} teams active.";

    // ── JoinRequest ───────────────────────────────────────────────────────────

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
                    if (TeamRules.WouldExceedMaxTeams())
                    { player.LaunchMessage(MaxTeamsMsg(), () => { }, "OK", messageDuration); return; }
                    team = new Team();
                    team.AddMember(player);
                    team.AddMember(otherPlayer);
                    otherPlayer.teamController.team = this.team;
                };
                otherPlayer.LaunchMessage($"Can we team up!", OnOtherPlayerConfirm, "Accept", messageDuration);
                break;

            case (Status.Solo, Status.Leader):
                OnOtherPlayerConfirm = () =>
                {
                    Debug.Log($"Status.Solo, Status.Leader");
                    otherPlayer.teamController.team.RemoveAllMembers();
                    // Dissolution already reduced the count — check is still correct here.
                    if (TeamRules.WouldExceedMaxTeams())
                    { player.LaunchMessage(MaxTeamsMsg(), () => { }, "OK", messageDuration); return; }
                    team = new Team();
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
                        Team leaderTeam = otherPlayer.teamController.team.GetLeader().teamController.team;
                        if (!leaderTeam.AddMember(player))
                        { player.LaunchMessage(MaxTeamSizeMsg(), () => { }, "OK", messageDuration); return; }
                        player.teamController.team = leaderTeam;
                    };
                    otherPlayer.teamController.team.GetLeader().LaunchMessage($"Can {player.name} join our team", LearderConfirm, "Accept", messageDuration);
                };
                otherPlayer.LaunchMessage($"Can I join join your team", OnOtherPlayerConfirm, "Ask", messageDuration);
                break;


            case (Status.Leader, Status.Solo):
                OnOtherPlayerConfirm = () =>
                {
                    team.RemoveAllMembers();
                    // RemoveAllMembers nulls this.team, so create a fresh one.
                    // Count is neutral (dissolved 1, about to create 1) — check still guards edge cases.
                    if (TeamRules.WouldExceedMaxTeams())
                    { player.LaunchMessage(MaxTeamsMsg(), () => { }, "OK", messageDuration); return; }
                    Team newTeam = new Team();
                    newTeam.AddMember(otherPlayer);
                    newTeam.AddMember(player);
                    this.team = newTeam;
                    otherPlayer.teamController.team = newTeam;
                };
                otherPlayer.LaunchMessage($"I'll abandon my team if I can join you!", OnOtherPlayerConfirm, "Accept", messageDuration);
                break;
            case (Status.Leader, Status.Leader):
                OnOtherPlayerConfirm = () =>
                {
                    team.RemoveAllMembers();
                    if (!otherPlayer.teamController.team.AddMember(player))
                    { player.LaunchMessage(MaxTeamSizeMsg(), () => { }, "OK", messageDuration); return; }
                    this.team = otherPlayer.teamController.team;
                };
                otherPlayer.LaunchMessage($"I'll Abandon my team if I can join Yours!", OnOtherPlayerConfirm, "Accept", messageDuration);
                break;
            case (Status.Leader, Status.Follower):
                OnOtherPlayerConfirm = () =>
                {
                    UnityAction OnLeaderConfirm = () =>
                    {
                        Team leaderTeam = otherPlayer.teamController.team.GetLeader().teamController.team;
                        if (!leaderTeam.AddMember(player))
                        { player.LaunchMessage(MaxTeamSizeMsg(), () => { }, "OK", messageDuration); return; }
                        this.team = leaderTeam;
                    };
                    otherPlayer.teamController.team.GetLeader().LaunchMessage($"Can {player.name} Join our team", OnLeaderConfirm, "Accept", messageDuration);
                };
                otherPlayer.LaunchMessage($"Leave your Team and Join My Team!", OnOtherPlayerConfirm, "Ask Leader", messageDuration);
                break;


            case (Status.Follower, Status.Solo):
                OnOtherPlayerConfirm = () =>
                {
                    team.RemoveMember(player);
                    // If the old team dissolved, count decreased — check reflects current state.
                    if (TeamRules.WouldExceedMaxTeams())
                    { player.LaunchMessage(MaxTeamsMsg(), () => { }, "OK", messageDuration); return; }
                    Team newTeam = new Team();
                    newTeam.AddMember(otherPlayer);
                    newTeam.AddMember(player);
                    this.team = newTeam;
                    otherPlayer.teamController.team = newTeam;
                };
                team.GetLeader().LaunchMessage($"Can I join you", OnOtherPlayerConfirm, () => { }, ("Accept", "Reject"), messageDuration);
                break;


            case (Status.Follower, Status.Leader):
                OnOtherPlayerConfirm = () =>
                {
                    team.RemoveMember(player);
                    if (!otherPlayer.teamController.team.AddMember(player))
                    { player.LaunchMessage(MaxTeamSizeMsg(), () => { }, "OK", messageDuration); return; }
                    this.team = otherPlayer.teamController.team;
                };
                team.GetLeader().LaunchMessage($"Can I join your team?", OnOtherPlayerConfirm, () => { }, ("Yes", "No"), messageDuration);
                break;

            case (Status.Follower, Status.Follower):
                OnLeaderConfirm = () =>
                {
                    OnOtherPlayerConfirm = () =>
                    {
                        Team leaderTeam = otherPlayer.teamController.team.GetLeader().teamController.team;
                        team.RemoveMember(player);
                        if (!leaderTeam.AddMember(player))
                        { player.LaunchMessage(MaxTeamSizeMsg(), () => { }, "OK", messageDuration); return; }
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
                    if (TeamRules.WouldExceedMaxTeams())
                    { player.LaunchMessage(MaxTeamsMsg(), () => { }, "OK", messageDuration); return; }
                    team = new Team();
                    team.AddMember(player);
                    team.AddMember(otherPlayer);
                    otherPlayer.teamController.team = this.team;
                };
                otherPlayer.LaunchMessage($"Lets Team up!", OnOtherPlayerConfirm, () => { }, ("Team Up", "Decline"), messageDuration);
                break;
            case (Status.Solo, Status.Leader):
                OnOtherPlayerConfirm = () =>
                {
                    Debug.Log($"Status.Solo, Status.Leader");
                    otherPlayer.teamController.team.RemoveAllMembers();
                    if (TeamRules.WouldExceedMaxTeams())
                    { player.LaunchMessage(MaxTeamsMsg(), () => { }, "OK", messageDuration); return; }
                    team = new Team();
                    team.AddMember(player);
                    team.AddMember(otherPlayer);
                    otherPlayer.teamController.team = this.team;
                };
                otherPlayer.LaunchMessage($"Abandon your Team and Lets Team up!", OnOtherPlayerConfirm, () => { }, ("Team Up", "Decline"), messageDuration);
                break;

            case (Status.Solo, Status.Follower):
                OnOtherPlayerConfirm = () =>
                {
                    otherPlayer.teamController.team.RemoveMember(otherPlayer);
                    if (TeamRules.WouldExceedMaxTeams())
                    { player.LaunchMessage(MaxTeamsMsg(), () => { }, "OK", messageDuration); return; }
                    team = new Team();
                    team.AddMember(player);
                    team.AddMember(otherPlayer);
                    otherPlayer.teamController.team = this.team;
                };
                otherPlayer.LaunchMessage($"Leave your Team and Lets Team up!", OnOtherPlayerConfirm, () => { }, ("Team Up", "Decline"), messageDuration);
                break;


            case (Status.Leader, Status.Solo):
                OnOtherPlayerConfirm = () =>
                {
                    if (!team.AddMember(otherPlayer))
                    { player.LaunchMessage(MaxTeamSizeMsg(), () => { }, "OK", messageDuration); return; }
                    otherPlayer.teamController.team = this.team;
                };
                otherPlayer.LaunchMessage($"Join my Team!", OnOtherPlayerConfirm, () => { }, ("Follow", "Decline"), messageDuration);
                break;
            case (Status.Leader, Status.Leader):
                OnOtherPlayerConfirm = () =>
                {
                    otherPlayer.teamController.team.RemoveAllMembers();
                    if (!team.AddMember(otherPlayer))
                    { player.LaunchMessage(MaxTeamSizeMsg(), () => { }, "OK", messageDuration); return; }
                    otherPlayer.teamController.team = this.team;
                };
                otherPlayer.LaunchMessage($"Abandon your team and Join my Team!", OnOtherPlayerConfirm, () => { }, ("Follow", "Decline"), messageDuration);
                break;
            case (Status.Leader, Status.Follower):
                OnOtherPlayerConfirm = () =>
                {
                    otherPlayer.teamController.team.RemoveMember(otherPlayer);
                    if (!team.AddMember(otherPlayer))
                    { player.LaunchMessage(MaxTeamSizeMsg(), () => { }, "OK", messageDuration); return; }
                    otherPlayer.teamController.team = this.team;
                };
                otherPlayer.LaunchMessage($"Leave your Team and Join My Team!", OnOtherPlayerConfirm, () => { }, ("Follow", "Decline"), messageDuration);
                break;


            case (Status.Follower, Status.Solo):
                
                OnLeaderConfirm = () =>

                {
                    OnOtherPlayerConfirm = () =>
                    {
                        team.GetLeader().teamController.Invite(otherPlayer);
                        otherPlayer.teamController.team = team.GetLeader().teamController.team;
                    };

                    otherPlayer.LaunchMessage($"Our Leader says you can join our team", OnOtherPlayerConfirm, () => { }, ("Follow", "Decline"), messageDuration);
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
                    otherPlayer.LaunchMessage($"Abandon your team and follow our Leader", OnOtherPlayerConfirm, () => { }, ("Follow", "Decline"), messageDuration);
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
                    otherPlayer.LaunchMessage($"Our Leader says you can join our team", OnOtherPlayerConfirm, () => { }, ("Follow", "Decline"), messageDuration);
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
               team.GetLeader().LaunchMessage($"Please Kick {otherPlayer.playerName} from the team", onLeaderConfirm, "Kick", messageDuration); 
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
                    player.LaunchMessage($"Do you want to quit your team?", () => { team.RemoveAllMembers(); }, () => { }, ("Disband", "No"), messageDuration);
                    break;
                case Status.Follower:
                    player.LaunchMessage($"Do you want to quit your team?", () => { team.RemoveMember(player); }, () => { }, ("Leave", "No"), messageDuration);
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
                // RemoveAllMembers nulled this.team — create a fresh team for the mutineer + followers.
                team = new Team();
                team.AddMember(player);

                for (int i = 0; i < followers.Count; i++)
                {
                    team.AddMember(followers[i]);
                }
            }
            hasVoted = 0;
            voteTally = 0;
            monoBehaviour.StopCoroutine(startMunity);
            startMunity = null;
        }
        else
        {
            hasVoted = 0;
            voteTally = 0;
            monoBehaviour.StopCoroutine(startMunity);
            startMunity = null;
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

public void Initialize(PlayerInput gamePad, LocalPlayerManager player, PlayerEvents playerEvents)
    {
        this.gamePad = gamePad;
        this.player = player;
        this.monoBehaviour = player.GetComponent<MonoBehaviour>();
        this.CurrentStatus = Status.Solo;
        this.playerEvents = playerEvents;
        this.playerEvents.OnUpdate += OnUpdate;
        this.playerEvents.OnOrbitTargetChanged += OnOrbitTargetChanged;
        this.playerEvents.OnHitConfirm += OnHitConfirm;
        this.playerEvents.OnHitConfirmPauseEnd += OnHitConfirmPauseEnd;
        this.onStatusChange += OnStatusChanged;
        isInitialized = true;
    }
public void Deactivate()
    {
        this.onStatusChange -= OnStatusChanged;
        this.playerEvents.OnUpdate -= OnUpdate;
        this.playerEvents.OnOrbitTargetChanged -= OnOrbitTargetChanged;
        this.playerEvents.OnHitConfirm -= OnHitConfirm;
        this.playerEvents.OnHitConfirmPauseEnd -= OnHitConfirmPauseEnd;
        this.playerEvents = null;
        isInitialized = false;
    }

    public void OnHitConfirm((Collider hitbox, Collider hurtbox) hitInfo)
    {
        _isHitConfirmPause = true;
    }

    public void OnHitConfirmPauseEnd((Collider hitbox, Collider hurtbox) hitInfo)
    {

        _isHitConfirmPause = false;



    }

    


void OnOrbitTargetChanged(LocalPlayerManager target, bool isTargeting)
    {
        _orbitTarget = target;
    }

    /// <summary>
    /// Called whenever this player's team status changes.
    /// No cursor push needed here — CameraControler polls ActiveSymbol every frame
    /// while orbit-targeting or follow-aim-locking, so symbol changes propagate
    /// automatically the next frame after a status change.
    /// </summary>
    void OnStatusChanged(Status newStatus)
    {
        playerEvents.OnTeamChanged?.Invoke();
    }
}
