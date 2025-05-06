using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class Team
{

    [SerializeField]
    private List<LocalPlayerManager> Members = new List<LocalPlayerManager>();


    public LocalPlayerManager GetLeader()
    {
        LocalPlayerManager Leader = null;

        for (int i = 0; i < Members.Count; i++)
        {

            if (Members[i].CurrentTeamStatus == TeamController.Status.Leader)
            {
                Leader = Members[i];

            }
            else
            {
                Leader = null;
            }
        }
        return Leader;
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

    public void AddMember(LocalPlayerManager member)
    {
        if (!IsCurrentMember(member))
        {
            if (Members.Count == 0)
            {
                member.CurrentTeamStatus = TeamController.Status.Leader;
            }
            else
            {
                member.CurrentTeamStatus = TeamController.Status.Follower;
            }
            Members.Add(member);
        }
    }

    public void RemoveMember(LocalPlayerManager member)
    {
        if (IsCurrentMember(member))
        {
            member.CurrentTeamStatus = TeamController.Status.Solo;
            Members.Remove(member);

            if (Members.Count == 1)
            {
                Members[0].CurrentTeamStatus = TeamController.Status.Solo;
                Members.Clear();
                member.teamController.team = null;
            }


        }




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




    }


}
