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
                var channelName = $"{baseChannel.Name} - Team {i + 1}";

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
            var teamChannelPrefix = $"{baseChannelName} - Team";

            foreach (var kvp in _teamChannelCache)
            {
                if (kvp.Key.StartsWith(teamChannelPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(kvp.Value);
                }
            }

            return result;
        }
    }
}
