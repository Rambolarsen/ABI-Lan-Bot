using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace ABILanBot;

class Program
{
    private DiscordSocketClient? _client;
    private IConfiguration? _configuration;

    static Task Main(string[] args) => new Program().MainAsync();

    private async Task MainAsync()
    {
        // Load configuration
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Configure Discord client
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
        };

        _client = new DiscordSocketClient(config);

        // Subscribe to events
        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _client.MessageReceived += MessageReceivedAsync;

        // Get token from configuration
        var token = _configuration["Discord:Token"];
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("ERROR: Discord token not found in configuration!");
            Console.WriteLine("Please create an appsettings.json file with your bot token.");
            Console.WriteLine("Use appsettings.example.json as a template.");
            return;
        }

        // Login and start
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        // Block this task until the program is closed
        await Task.Delay(-1);
    }

    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }

    private Task ReadyAsync()
    {
        Console.WriteLine($"{_client?.CurrentUser} is connected and ready!");
        return Task.CompletedTask;
    }

    private async Task MessageReceivedAsync(SocketMessage message)
    {
        // Don't respond to bot messages
        if (message.Author.IsBot)
            return;

        // Check for ping command
        if (message.Content.ToLower() == "!ping")
        {
            await message.Channel.SendMessageAsync("Pong! 🏓");
        }
        // Check for help command
        else if (message.Content.ToLower() == "!help")
        {
            await message.Channel.SendMessageAsync(
                "**ABI Lan Bot Commands:**\n" +
                "• `!ping` - Check if the bot is responsive\n" +
                "• `!help` - Show this help message\n" +
                "• `!info` - Display bot information"
            );
        }
        // Check for info command
        else if (message.Content.ToLower() == "!info")
        {
            var embed = new EmbedBuilder()
                .WithTitle("ABI Lan Bot")
                .WithDescription("A Discord bot built with Discord.Net")
                .WithColor(Color.Blue)
                .AddField("Version", "1.0.0", inline: true)
                .AddField("Framework", ".NET 10.0", inline: true)
                .AddField("Library", "Discord.Net 3.18.0", inline: true)
                .WithCurrentTimestamp()
                .Build();

            await message.Channel.SendMessageAsync(embed: embed);
        }
    }
}
