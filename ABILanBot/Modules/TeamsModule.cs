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
		private readonly MemberService _members;
		private readonly IConfiguration _config;
		private const string GameLobbyChannelName = "GameLobbyChannelName";

        public TeamsModule(TeamService teams, VoiceChannelService voice, MemberService members, IConfiguration config)
		{
			_teams = teams;
			_voice = voice;
			_members = members;
			_config = config;
		}

		[SlashCommand("scrimmage", "Split people in your voice channel into random teams (and optionally move them).")]
		public async Task MakeTeams(int teamCount = 2, bool moveMembers = true)
		{
			if (teamCount < 2)
			{
				await RespondAsync("You need at least 2 teams 😉", ephemeral: true);
				return;
			}

            var gameLobbyChannelName = _config.GetSection(GameLobbyChannelName).Value;

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
				await _members.MoveTeamsToChannelsAsync(teams, vcs);
			}

			var embed = BuildTeamsEmbed(voiceChannel, teams, members.Count, teamCount, moveMembers);
			await RespondAsync(embed: embed);
		}

		[SlashCommand("returntolobby", "Move all members from team channels back to the game lobby.")]
		public async Task ReturnToLobby()
		{
			var gameLobbyChannelName = _config.GetSection(GameLobbyChannelName).Value;

			if (string.IsNullOrEmpty(gameLobbyChannelName))
			{
				await RespondAsync($"{GameLobbyChannelName} is not configured.", ephemeral: true);
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

			// Get team channels from the cache
			var teamChannels = _voice.GetTeamChannelsFromCache(gameLobbyChannelName);

			if (teamChannels.Count == 0)
			{
				await RespondAsync($"No team channels found in cache. Use `/maketeams` first to create team channels.", ephemeral: true);
				return;
			}

			// Collect all users from team channels
			var usersToMove = new List<SocketGuildUser>();
			foreach (var channel in teamChannels)
			{
				usersToMove.AddRange(channel.ConnectedUsers.Where(u => !u.IsBot));
			}

			if (usersToMove.Count == 0)
			{
				await RespondAsync("No users found in team channels to move.", ephemeral: true);
				return;
			}

			// Move all users back to lobby using MemberService
			await _members.MoveMembersToChannelAsync(usersToMove, lobbyChannel);

			// Build response embed
			var embed = new EmbedBuilder()
				.WithTitle("Return to Lobby")
				.WithDescription($"✅ Successfully moved **{usersToMove.Count}** user(s) back to **{gameLobbyChannelName}**.")
				.WithColor(Color.Green)
				.WithCurrentTimestamp()
				.Build();

			await RespondAsync(embed: embed);
		}

		[SlashCommand("cleanupchannels", "Delete empty team voice channels created by the bot.")]
		public async Task CleanupChannels(
			[Summary("force-delete", "If true, deletes channels even if users are connected")] bool forceDelete = false)
		{
			// Get team channels from the cache
			var teamChannels = _voice.GetTeamChannelsFromCache(string.Empty);

			if (teamChannels.Count == 0)
			{
				await RespondAsync("No team channels found to clean up.", ephemeral: true);
				return;
			}

			// Perform cleanup
			var (deleted, skipped) = await _voice.CleanupTeamChannelsAsync(forceDelete);

			// Build response embed
			var description = forceDelete
				? $"✅ Deleted **{deleted}** team channel(s)."
				: $"✅ Deleted **{deleted}** empty team channel(s).\n" +
				  (skipped > 0 ? $"⚠️ Skipped **{skipped}** channel(s) with users still connected." : "");

			var embed = new EmbedBuilder()
				.WithTitle("Channel Cleanup")
				.WithDescription(description)
				.WithColor(deleted > 0 ? Color.Green : Color.Orange)
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
