using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dynamic team constraints — both limits are inversely linked and recalculate
/// live from <see cref="LocalPlayerManager.ActivePlayers"/> on every query.
///
///   MaxTeamSize = Ceil( PlayerCount / ActiveTeamCount )
///     Enforced in Team.AddMember.
///     Always divides by at least 2 so one team can never absorb all players.
///     Ceil gives the one +1 remainder slot when PlayerCount % TeamCount != 0.
///
///   MaxTeams = Floor( PlayerCount / LargestCurrentTeamSize )
///     Enforced in TeamController before any new Team object is created.
///     Kicks in once any team reaches 2+ members.
///
/// Example (16 players):
///   • One team of 8 forms  → MaxTeams = Floor(16/8) = 2
///   • Eight teams of 2     → MaxTeamSize = Ceil(16/8) = 2
///   • Four teams of 4      → both limits = 4
/// </summary>
public static class TeamRules
{
    // ── Active-team snapshot ──────────────────────────────────────────────────

    /// <summary>
    /// Returns all distinct Team objects that currently have 2 or more members.
    /// </summary>
    public static List<Team> GetActiveTeams()
    {
        var seen   = new HashSet<Team>();
        var result = new List<Team>();

        foreach (LocalPlayerManager p in LocalPlayerManager.ActivePlayers)
        {
            Team t = p.teamController?.team;
            if (t != null && t.GetAllMembers().Count >= 2 && !seen.Contains(t))
            {
                seen.Add(t);
                result.Add(t);
            }
        }

        return result;
    }

    /// <summary>Number of distinct active teams (each with 2+ members).</summary>
    public static int GetActiveTeamCount() => GetActiveTeams().Count;

    /// <summary>
    /// Member count of the largest currently active team.
    /// Returns 0 if no team of 2+ members exists.
    /// </summary>
    public static int GetLargestTeamSize()
    {
        int largest = 0;

        foreach (LocalPlayerManager p in LocalPlayerManager.ActivePlayers)
        {
            Team t = p.teamController?.team;
            if (t == null) continue;
            int size = t.GetAllMembers().Count;
            if (size > largest) largest = size;
        }

        return largest;
    }

    // ── Dynamic limits ────────────────────────────────────────────────────────

    /// <summary>
    /// Maximum number of teams currently allowed.
    /// Formula: Floor( PlayerCount / LargestTeamSize ).
    /// Returns <see cref="int.MaxValue"/> while no team of 2+ members exists — no cap yet.
    /// </summary>
    public static int GetMaxTeams()
    {
        int playerCount = LocalPlayerManager.ActivePlayers.Count;
        if (playerCount == 0) return 0;

        int largest = GetLargestTeamSize();
        if (largest < 2) return int.MaxValue;   // first team still forming — no cap

        return Mathf.FloorToInt((float)playerCount / largest);
    }

    /// <summary>
    /// Maximum members allowed on any single team right now.
    /// Formula: Ceil( PlayerCount / max(TeamCount, 2) ).
    /// Always divides by at least 2 so even the first team forming is capped at half
    /// the player count — leaving room for at least one opposing team.
    /// Ceil gives the one +1 remainder slot when PlayerCount % TeamCount != 0.
    /// Returns <see cref="int.MaxValue"/> when fewer than 2 players are active.
    /// </summary>
    public static int GetMaxTeamSize()
    {
        int playerCount = LocalPlayerManager.ActivePlayers.Count;
        if (playerCount < 2) return int.MaxValue;   // 0 or 1 active player — no constraint possible

        int teamCount = GetActiveTeamCount();

        // Always divide by at least 2 — even when only one team is forming there must be
        // room left for at least one opposing team, so the cap is Ceil(players / 2) minimum.
        int effectiveTeamCount = Mathf.Max(teamCount, 2);
        return Mathf.CeilToInt((float)playerCount / effectiveTeamCount);
    }

    // ── Guard helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// True if forming one additional team (without dissolving any existing one) would
    /// push the active team count past <see cref="GetMaxTeams"/>.
    /// </summary>
    public static bool WouldExceedMaxTeams()
    {
        int max = GetMaxTeams();
        if (max == int.MaxValue) return false;
        return (GetActiveTeamCount() + 1) > max;
    }

    /// <summary>
    /// True if adding one more member to <paramref name="team"/> would push its member
    /// count past <see cref="GetMaxTeamSize"/>.
    /// </summary>
    public static bool WouldExceedMaxTeamSize(Team team)
    {
        int max = GetMaxTeamSize();
        if (max == int.MaxValue) return false;
        return (team.GetAllMembers().Count + 1) > max;
    }
}
