using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class Team
{
    /// <summary>
    /// Fired whenever any member is added or removed.
    /// All current members subscribe so their team list UIs stay in sync
    /// even when their own status hasn't changed.
    /// </summary>
    public Action OnMembershipChanged;

    [SerializeField]
    private List<LocalPlayerManager> Members = new List<LocalPlayerManager>();


    public LocalPlayerManager GetLeader()
    {
        for (int i = 0; i < Members.Count; i++)
        {
            if (Members[i].CurrentTeamStatus == TeamController.Status.Leader)
                return Members[i];
        }
        return null;
    }
    public List<LocalPlayerManager> GetFollowers()
    {
        List<LocalPlayerManager> Followers = new List<LocalPlayerManager>();
        for (int i = 0; i < Members.Count; i++)
        {
            if (Members[i].CurrentTeamStatus != TeamController.Status.Leader)
            {
                Followers.Add(Members[i]);
            }
        }
        return Followers;
    }
    public List<LocalPlayerManager> GetAllMembers()
    {
        return Members;
    }


    public LocalPlayerManager GetMembersByName(string name)
    {
        LocalPlayerManager member = null;

        for (int i = 0; i < Members.Count; i++)
        {
            if (Members[i].playerName == name)
                member = Members[i];
        }

        return member;
    }

    public bool IsCurrentMember(LocalPlayerManager player)
    {
        bool isMember = false;
        for (int i = 0; i < Members.Count; i++)
        {
            if (Members[i].playerName == player.name)
            {
                isMember = true;
            }
        }
        return isMember;

    }

    /// <summary>
    /// Adds <paramref name="member"/> to this team.
    /// Returns <c>false</c> (and does nothing) if the player is already a member,
    /// or if adding them would exceed the dynamic <see cref="TeamRules.GetMaxTeamSize"/> cap.
    /// </summary>
    public bool AddMember(LocalPlayerManager member)
    {
        if (IsCurrentMember(member)) return false;

        // Enforce dynamic team-size cap (active once 2+ teams exist).
        if (TeamRules.WouldExceedMaxTeamSize(this))
        {
            Debug.Log($"[Team] Cannot add {member.playerName}: " +
                      $"team full ({Members.Count}/{TeamRules.GetMaxTeamSize()}).");
            return false;
        }

        // Wire the team reference BEFORE setting CurrentTeamStatus.
        // The status setter fires RefreshCursorSymbol(), which reads
        // teamController.team to resolve the leader's symbol — so the
        // reference must already be in place when that fires.
        member.teamController.team = this;

        if (Members.Count == 0)
            member.CurrentTeamStatus = TeamController.Status.Leader;
        else
            member.CurrentTeamStatus = TeamController.Status.Follower;

        Members.Add(member);
        OnMembershipChanged?.Invoke(); // notify all subscribers (e.g. existing members' HUDs)
        return true;
    }

    public void RemoveMember(LocalPlayerManager member)
    {
        if (!IsCurrentMember(member)) return;

        bool wasLeader = member.CurrentTeamStatus == TeamController.Status.Leader;

        // Always clean up the removed member
        member.CurrentTeamStatus    = TeamController.Status.Solo;
        member.teamController.team  = null;
        Members.Remove(member);

        if (Members.Count == 1)
        {
            // Team dissolves — one person is not a team
            Members[0].CurrentTeamStatus   = TeamController.Status.Solo;
            Members[0].teamController.team = null;
            Members.Clear();
        }
        else if (wasLeader && Members.Count >= 1)
        {
            // Promote the first remaining follower to leader
            Members[0].CurrentTeamStatus = TeamController.Status.Leader;
        }

        OnMembershipChanged?.Invoke();
    }

    public void RemoveAllMembers()
    {
        List<LocalPlayerManager> memberList = GetAllMembers();

        for (int i = 0; i < Members.Count; i++)
        {
            Members[i].CurrentTeamStatus = TeamController.Status.Solo;
        }

        Members.Clear();

        for (int i = 0; i < memberList.Count; i++)
        {
            memberList[i].teamController.team = null;
        }

        OnMembershipChanged?.Invoke();
    }


}
