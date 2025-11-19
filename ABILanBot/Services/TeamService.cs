using ABILanBot.Entities;
using Discord.WebSocket;

namespace ABILanBot.Services
{
	public class TeamService
	{
		private readonly Random _rng = new();

        public async Task<TeamResult> CreateRandomTeams(
			IReadOnlyList<SocketGuildUser> members,
			int teamCount)
		{
            if (teamCount < 2)
                return new TeamResult { Success = false, ErrorMessage = $"Need at least 2 teams. Current selected teams {teamCount}." };

            //if (members.Count < teamCount)
            //    return new TeamResult { Success = false, ErrorMessage = $"Not enough members for that many teams. Current members is {members.Count}." };


            var shuffled = members.OrderBy(_ => _rng.Next()).ToList();

			var teams = new List<List<SocketGuildUser>>();
			for (int i = 0; i < teamCount; i++)
                teams.Add([]);

			int index = 0;
			foreach (var m in shuffled)
			{
				teams[index].Add(m);
				index = (index + 1) % teamCount;
			}

			return new TeamResult(teams);
		}
	}
}
