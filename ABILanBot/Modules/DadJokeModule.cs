using ABILanBot.Services;
using Discord;
using Discord.Interactions;

namespace ABILanBot.Modules
{
	[CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm)]
	[IntegrationType(ApplicationIntegrationType.GuildInstall)]
	public class DadJokeModule : InteractionModuleBase<SocketInteractionContext>
	{
		private readonly DadJokeService _dadJokeService;

		public DadJokeModule(DadJokeService dadJokeService)
		{
			_dadJokeService = dadJokeService;
		}

		[SlashCommand("dadjoke", "Get a random dad joke")]
		public async Task DadJoke()
		{
			try
			{
				var joke = await _dadJokeService.GetRandomDadJokeAsync();
				await RespondAsync($"😄 {joke}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error handling dadjoke command: {ex.Message}");
				await RespondAsync("Dad joke service is not available right now! 😅", ephemeral: true);
			}
		}
	}
}
