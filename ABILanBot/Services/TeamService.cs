using System;
using System.Collections.Generic;
using System.Linq;
using Discord.WebSocket;

namespace WorkLanBot.Services
{
	public class TeamService
	{
		private readonly Random _rng = new();

		public async IReadOnlyList<List<SocketGuildUser>> CreateRandomTeams(
			IReadOnlyList<SocketGuildUser> members,
			int teamCount)
		{
			if (teamCount < 2)
			{
				await RespondAsync($"Need at least 2 teams. Current selected teams {teamCount}."); return;
			}

			if (members.Count < teamCount)
			{
				await RespondAsync($"Not enough members for that many teams. Current members is {members.Count}.");
			}

			var shuffled = members.OrderBy(_ => _rng.Next()).ToList();

			var teams = new List<List<SocketGuildUser>>();
			for (int i = 0; i < teamCount; i++)
				teams.Add(new List<SocketGuildUser>());

			int index = 0;
			foreach (var m in shuffled)
			{
				teams[index].Add(m);
				index = (index + 1) % teamCount;
			}

			return teams;
		}
	}
}
