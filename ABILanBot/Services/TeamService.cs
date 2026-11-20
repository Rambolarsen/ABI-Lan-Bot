using ABILanBot.Entities;
using Discord.WebSocket;

namespace ABILanBot.Services;

public class TeamService
{
    public Task<TeamResult> CreateRandomTeams(
        IReadOnlyList<SocketGuildUser> members,
        int teamCount)
    {
        if (teamCount < 2)
            return Task.FromResult(new TeamResult
            {
                Success = false,
                ErrorMessage = $"Need at least 2 teams. Current selected teams {teamCount}."
            });

        var teams = TeamAllocator.Split(members, teamCount);
        return Task.FromResult(new TeamResult(teams));
    }
}
