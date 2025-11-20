using ABILanBot.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace ABILanBot.Modules
{
	public class TeamsModule : InteractionModuleBase<SocketInteractionContext>
	{
		private readonly TeamService _teams;
		private readonly VoiceChannelService _voice;
		private readonly IConfiguration _config;

		public TeamsModule(TeamService teams, VoiceChannelService voice, IConfiguration config)
		{
			_teams = teams;
			_voice = voice;
			_config = config;
		}

		[SlashCommand("maketeams", "Split people in your voice channel into random teams (and optionally move them).")]
		public async Task MakeTeams(int teamCount = 2, bool moveMembers = true)
		{
			if (teamCount < 2)
			{
				await RespondAsync("You need at least 2 teams 😉", ephemeral: true);
				return;
			}

            var gameLobbyChannelName = _config.GetSection("GameLobbyChannelName").Value;

			// Get all voice channels in the guild that match the name (case-insensitive)
			var voiceChannel = Context.Guild.VoiceChannels
				.FirstOrDefault(vc => string.Equals(vc.Name, gameLobbyChannelName, StringComparison.OrdinalIgnoreCase));

			if (voiceChannel == null)
			{
				await RespondAsync($"You must use the **{gameLobbyChannelName}** channel to split into teams.", ephemeral: true);
				return;
			}

			var members = voiceChannel.Users
				.Where(u => !u.IsBot)
				.ToList();

			//if (members.Count < teamCount)
			//{
			//	await RespondAsync(
			//		$"Not enough people in **{voiceChannel.Name}** for {teamCount} teams (only {members.Count} humans).",
			//		ephemeral: true);
			//	return;
			//}

			var teamsResult = await _teams.CreateRandomTeams(members, teamCount);

			if(!teamsResult.Success || teamsResult.Teams == null)
            {
				await RespondAsync(teamsResult.ErrorMessage ?? "An unknown error occurred while creating teams.", ephemeral: true);
				return;
            }

			var teams = teamsResult.Teams;
            if (moveMembers)
			{
				var vcs = await _voice.EnsureTeamVoiceChannelsExists(Context.Guild, voiceChannel, teamCount);
				await _voice.MoveTeamsToChannelsAsync(teams, vcs);
			}

			var embed = BuildTeamsEmbed(voiceChannel, teams, members.Count, teamCount, moveMembers);
			await RespondAsync(embed: embed);
		}

		[SlashCommand("returntolobby", "Move all members from team channels back to the game lobby.")]
		public async Task ReturnToLobby()
		{
			var gameLobbyChannelName = _config.GetSection("GameLobbyChannelName").Value;

			if (string.IsNullOrEmpty(gameLobbyChannelName))
			{
				await RespondAsync("GameLobbyChannelName is not configured.", ephemeral: true);
				return;
			}

			// Find the lobby voice channel
			var lobbyChannel = Context.Guild.VoiceChannels
				.FirstOrDefault(vc => string.Equals(vc.Name, gameLobbyChannelName, StringComparison.OrdinalIgnoreCase));

			if (lobbyChannel == null)
			{
				await RespondAsync($"Could not find the **{gameLobbyChannelName}** channel.", ephemeral: true);
				return;
			}

			// Find all team channels (channels that start with "{GameLobbyChannelName} - Team")
			var teamChannelPrefix = $"{gameLobbyChannelName} - Team";
			var teamChannels = Context.Guild.VoiceChannels
				.Where(vc => vc.Name.StartsWith(teamChannelPrefix, StringComparison.OrdinalIgnoreCase))
				.ToList();

			if (teamChannels.Count == 0)
			{
				await RespondAsync($"No team channels found matching pattern **{teamChannelPrefix}**.", ephemeral: true);
				return;
			}

			// Collect all users from team channels
			var usersToMove = new List<SocketGuildUser>();
			foreach (var channel in teamChannels)
			{
				usersToMove.AddRange(channel.Users.Where(u => !u.IsBot));
			}

			if (usersToMove.Count == 0)
			{
				await RespondAsync("No users found in team channels to move.", ephemeral: true);
				return;
			}

			// Move all users back to lobby
			int successCount = 0;
			int failCount = 0;
			foreach (var user in usersToMove)
			{
				try
				{
					await user.ModifyAsync(props => props.Channel = lobbyChannel);
					successCount++;
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Failed to move {user.Username}: {ex.Message}");
					failCount++;
				}
			}

			// Build response embed
			var embed = new EmbedBuilder()
				.WithTitle("Return to Lobby")
				.WithDescription(
					$"✅ Successfully moved **{successCount}** user(s) back to **{gameLobbyChannelName}**." +
					(failCount > 0 ? $"\n⚠️ Failed to move **{failCount}** user(s)." : ""))
				.WithColor(failCount > 0 ? Color.Orange : Color.Green)
				.WithCurrentTimestamp()
				.Build();

			await RespondAsync(embed: embed);
		}

		private Embed BuildTeamsEmbed(
			SocketVoiceChannel voiceChannel,
			IReadOnlyList<List<SocketGuildUser>> teams,
			int totalMembers,
			int teamCount,
			bool moved)
		{
			var eb = new EmbedBuilder()
				.WithTitle($"Random teams for {voiceChannel.Name}")
				.WithDescription(
					$"Total players: **{totalMembers}**\n" +
					$"Teams: **{teamCount}**\n" +
					(moved
						? "✅ Members were moved to team voice channels."
						: "❌ Members were **not** moved (showing teams only)."))
				.WithCurrentTimestamp();

			for (int i = 0; i < teams.Count; i++)
			{
				var team = teams[i];
				var name = $"Team {i + 1}";
				var value = team.Count == 0
					? "_(empty)_"
					: string.Join("\n", team.Select(u => u.Mention));

				eb.AddField(name, value, inline: false);
			}

			return eb.Build();
		}
	}
}
