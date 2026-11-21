using ABILanBot.Modules;
using ABILanBot.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace ABILanBot;

class Program
{
	private DiscordSocketClient? _client;
	private IConfiguration? _configuration;
    private InteractionService? _interactionService;
    private IServiceProvider? _serviceProvider;
    private ModuleInfo[] _globalModules = [];
    private ModuleInfo[] _guildModules = [];

    static async Task Main(string[] args)
    {
        if (args.Contains("--validate-config", StringComparer.OrdinalIgnoreCase))
        {
            Environment.ExitCode = ValidateConfigForLauncher();
            return;
        }

        await new Program().MainAsync();
    }

    private static int ValidateConfigForLauncher()
    {
        try
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            StartupConfigurationValidator.Validate(config);
            Console.WriteLine("Configuration valid.");
            Console.WriteLine($"CS2_RCON_CONFIGURED={(!string.IsNullOrWhiteSpace(config["Cs2:Host"])).ToString().ToLowerInvariant()}");
            return 0;
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or ArgumentException)
        {
            Console.Error.WriteLine($"Configuration validation failed: {ex.Message}");
            return 1;
        }
    }

	private async Task MainAsync()
	{
		// Load configuration
		_configuration = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
			.Build();

		// Register all bot dependencies via a single composition root shared with tests.
        _serviceProvider = new ServiceCollection()
            .AddBotServices(_configuration)
            .BuildServiceProvider(validateScopes: true);

		// Resolve Discord client
		_client = _serviceProvider.GetRequiredService<DiscordSocketClient>();

        // Create and configure InteractionService
        _interactionService = new InteractionService(_client);

        // Register modules
        (_globalModules, _guildModules) = await CommandRegistrationPlan.LoadAsync(_interactionService, _serviceProvider);

        // Subscribe to interaction events
        _client.InteractionCreated += async interaction =>
        {
            var ctx = new SocketInteractionContext(_client, interaction);
            var result = await _interactionService.ExecuteCommandAsync(ctx, _serviceProvider);
            if (!result.IsSuccess)
                Console.Error.WriteLine($"Slash command failed: {result.ErrorReason}");
        };

        // Subscribe to events
        _client.Log += LogAsync;
		_client.Ready += ReadyAsync;
		if (BotConfiguration.IsMessageContentEnabled(_configuration))
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

		// Validate deployment configuration before connecting to Discord.
        try
        {
            _ = BotConfiguration.GetGuildId(_configuration);
            _ = BotConfiguration.IsMessageContentEnabled(_configuration);
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"Invalid Discord configuration: {ex.Message}");
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

	private async Task ReadyAsync()
	{
		Console.WriteLine($"{_client?.CurrentUser} is connected and ready!");

        if (_interactionService is null || _configuration is null) return;
        var guildId = BotConfiguration.GetGuildId(_configuration);
        try
        {
            // Publish only the explicitly approved modules in each scope.
            Console.WriteLine($"[Commands] Registering LAN-only commands for guild {guildId}...");
            var guildCommands = await _interactionService.AddModulesToGuildAsync(
                guildId, deleteMissing: true, _guildModules);
            Console.WriteLine($"[Commands] Registered {guildCommands.Count} guild commands.");
            Console.WriteLine("[Commands] Registering DM-safe global commands...");
            var globalCommands = await _interactionService.AddModulesGloballyAsync(
                deleteMissing: true, _globalModules);
            Console.WriteLine($"[Commands] Registered {globalCommands.Count} global commands.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Commands] Registration failed ({ex.GetType().Name}): {ex.Message}");
        }
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
				"**ABI Lan Bot Commands:**\n\n" +
				"**Text Commands:**\n" +
				"• `!ping` - Check if the bot is responsive\n" +
				"• `!help` - Show this help message\n" +
				"• `!info` - Display bot information\n\n" +
				"**Slash Commands:**\n" +
				"• `/dadjoke` - Get a random dad joke\n" +
				"• `/scrimmage` - Split the lobby into random teams\n" +
				"• `/returntolobby` - Move all members from team channels back to the game lobby\n" +
				"• `/cleanupchannels` - Delete empty team voice channels created by the bot\n" +
                    "• `/gamepick` - Randomly pick the next game\n" +
                    "• `/challenge` - Get a LAN party challenge\n" +
                    "• `/lanbingo` - Generate a LAN bingo card\n" +
                    "• `/mat stem` - Stem på hvor vi bestiller middag fra\n" +
                    "• `/spill stem` - Stem på neste spill\n" +
                    "• `/award stem` - Start prisavstemning (arrangør)\n" +
                    "• `/cs2 preset` - Warmup eller competitive (arrangør)"
			);
		}
		// Check for info command
		else if (message.Content.ToLower() == "!info")
		{
			var embed = new EmbedBuilder()
				.WithTitle("ABI Lan Bot")
				.WithDescription("A Discord bot built with Discord.Net")
				.WithColor(Color.Blue)
				.AddField("Version", typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "unknown", inline: true)
				.AddField("Framework", System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription, inline: true)
				.AddField("Library", $"Discord.Net {typeof(DiscordSocketClient).Assembly.GetName().Version?.ToString(3)}", inline: true)
				.WithCurrentTimestamp()
				.Build();

			await message.Channel.SendMessageAsync(embed: embed);
		}
	}
}
