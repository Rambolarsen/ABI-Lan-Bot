using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace WorkLanBot.Services
{
	public class VoiceChannelService
	{
        //TODO: I want to be able to keep a local cache of created channels so i can move users back to lobby from them.
        private readonly Dictionary<SocketVoiceChannel, SocketVoiceChannel> _teamChannelCache = new();

        public async Task<IReadOnlyList<SocketVoiceChannel>> EnsureTeamVoiceChannelsExists(
			SocketGuild guild,
			SocketVoiceChannel baseChannel,
			int teamCount)
		{
			var result = new List<SocketVoiceChannel>();

			for (int i = 0; i < teamCount; i++)
			{
				var channelName = $"{baseChannel.Name} - Team {i + 1}";

                //need to check if the channel already exists in the cache

				if( _teamChannelCache.TryGetValue(baseChannel, out var cachedChannel))
				{
					result.Add(cachedChannel);
					continue;
                }

                var existing = guild.VoiceChannels
					.FirstOrDefault(c => string.Equals(c.Name, channelName, StringComparison.OrdinalIgnoreCase));

				SocketVoiceChannel vc;
				if (existing != null)
				{
					vc = existing;
				}
				else
				{
					vc = await guild.CreateVoiceChannelAsync(channelName, props =>
					{
						props.CategoryId = baseChannel.CategoryId;
						props.Bitrate = baseChannel.Bitrate;
						props.UserLimit = baseChannel.UserLimit;
					});
				}

				result.Add(vc);
			}

			return result;
		}

		public async Task MoveTeamsToChannelsAsync(
			IReadOnlyList<List<SocketGuildUser>> teams,
			IReadOnlyList<SocketVoiceChannel> channels)
		{
			for (int i = 0; i < teams.Count; i++)
			{
				var team = teams[i];
				var target = channels[i];

				foreach (var member in team)
				{
					try
					{
						await member.ModifyAsync(props => props.Channel = target);
					}
					catch (Exception ex)
					{
						await RespondAsync($"Failed to move {member.Username}");
						Console.WriteLine($"Failed to move {member.Username}: {ex.Message}");
					}
				}
			}
		}
	}
}
