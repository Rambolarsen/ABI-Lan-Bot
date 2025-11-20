using Discord.WebSocket;

namespace ABILanBot.Services
{
    public class MemberService
    {
        public async Task MoveMembersToChannelAsync(
            IReadOnlyList<SocketGuildUser> members,
            SocketVoiceChannel targetChannel)
        {
            foreach (var member in members)
            {
                try
                {
                    await member.ModifyAsync(props => props.Channel = targetChannel);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to move {member.Username}: {ex.Message}");
                }
            }
        }

        public async Task MoveTeamsToChannelsAsync(
            IReadOnlyList<List<SocketGuildUser>> teams,
            IReadOnlyList<SocketVoiceChannel> channels)
        {
            if (teams.Count != channels.Count)
            {
                throw new ArgumentException("Teams and channels collections must have the same count.");
            }
            for (int i = 0; i < teams.Count; i++)
            {
                var team = teams[i];
                var target = channels[i];
                await MoveMembersToChannelAsync(team, target);
            }
        }
    }
}
