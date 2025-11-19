using Discord.WebSocket;

namespace ABILanBot.Entities
{
    public class TeamResult
    {
        public TeamResult() { }

        public TeamResult(List<List<SocketGuildUser>> teams)
        {
            Teams = teams;
            Success = true;
        }

        public bool Success { get; set;  }
        public string? ErrorMessage { get; set; }
        public List<List<SocketGuildUser>>? Teams { get; set; }
    }
}
