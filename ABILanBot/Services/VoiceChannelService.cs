using Discord.WebSocket;

namespace ABILanBot.Services
{
    public class VoiceChannelService
    {
        //TODO: I want to be able to keep a local cache of created channels so i can move users back to lobby from them.
        private readonly Dictionary<string, SocketVoiceChannel> _teamChannelCache = new();

        public async Task<IReadOnlyList<SocketVoiceChannel>> EnsureTeamVoiceChannelsExists(
            SocketGuild guild,
            SocketVoiceChannel baseChannel,
            int teamCount)
        {
            var result = new List<SocketVoiceChannel>();

            for (int i = 0; i < teamCount; i++)
            {
                var channelName = $"Game room {i + 1}";

                //need to check if the channel already exists in the cache

                if( _teamChannelCache.TryGetValue(channelName, out var cachedChannel))
                {
                    result.Add(cachedChannel);
                    continue;
                }

                var existing = guild.VoiceChannels
                    .FirstOrDefault(c => string.Equals(c.Name, channelName, StringComparison.OrdinalIgnoreCase));

                if(existing == default)
                {
                    var voiceChannel = await guild.CreateVoiceChannelAsync(channelName, props =>
                    {
                        props.CategoryId = baseChannel.CategoryId;
                        props.Bitrate = baseChannel.Bitrate;
                        props.UserLimit = baseChannel.UserLimit;
                    });
                    existing = guild.VoiceChannels.FirstOrDefault(c => c.Id == voiceChannel.Id);
                }
                if(existing != default)
                {
                    result.Add(existing);
                    _teamChannelCache.TryAdd(channelName, existing);
                }
                else
                {
                    //this should never happen but just in case
                    Console.WriteLine($"Failed to create or find voice channel: {channelName}");
                    continue;
                }
            }

            return result;
        }

        public IReadOnlyList<SocketVoiceChannel> GetTeamChannelsFromCache(string baseChannelName)
        {
            var result = new List<SocketVoiceChannel>();
            var teamChannelPrefix = $"Game room";

            foreach (var kvp in _teamChannelCache)
            {
                if (kvp.Key.StartsWith(teamChannelPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(kvp.Value);
                }
            }

            return result;
        }

        public async Task<(int deleted, int skipped)> CleanupTeamChannelsAsync(bool forceDelete = false)
        {
            var channelsToDelete = new List<string>();
            int deletedCount = 0;
            int skippedCount = 0;

            foreach (var kvp in _teamChannelCache.ToList())
            {
                var channel = kvp.Value;
                
                // Check if channel still exists
                if (channel == null)
                {
                    _teamChannelCache.Remove(kvp.Key);
                    continue;
                }

                // Only delete if channel is empty or force delete is enabled
                if (forceDelete || channel.ConnectedUsers.Count == 0)
                {
                    try
                    {
                        await channel.DeleteAsync();
                        channelsToDelete.Add(kvp.Key);
                        deletedCount++;
                    }
                    catch (Discord.Net.HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.NotFound)
                    {
                        // Channel was already deleted externally, just remove from cache
                        channelsToDelete.Add(kvp.Key);
                        Console.WriteLine($"Channel {kvp.Key} was already deleted externally.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to delete channel {kvp.Key}: {ex.Message}");
                        skippedCount++;
                    }
                }
                else
                {
                    skippedCount++;
                }
            }

            // Remove deleted channels from cache
            foreach (var channelName in channelsToDelete)
            {
                _teamChannelCache.Remove(channelName);
            }

            return (deletedCount, skippedCount);
        }
    }
}
