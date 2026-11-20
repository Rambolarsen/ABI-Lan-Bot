using ABILanBot.Services;
using Discord;
using Discord.Interactions;

namespace ABILanBot.Modules;

[Group("food", "Vote on what to eat during the LAN.")]
[CommandContextType(InteractionContextType.Guild)]
public sealed class FoodModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly FoodPollService _polls;

    public FoodModule(FoodPollService polls) => _polls = polls;

    [SlashCommand("vote", "Start a food vote in this channel.")]
    public async Task VoteAsync(
        [Summary("options", "Comma-separated food choices (default: Pizza, Burger, Kebab, Sushi)")]
        string? options = null,
        [Summary("meal", "What are we choosing food for?")]
        [Choice("Lunch", "lunch")]
        [Choice("Dinner", "dinner")]
        [Choice("Snacks", "snacks")]
        string meal = "dinner",
        [Summary("hours", "How many hours voting remains open (1-24; default 2)")]
        int hours = 2)
    {
        try
        {
            var poll = _polls.CreatePoll(options, meal, hours);
            await RespondAsync(
                "🍽️ **LAN food vote!** One choice per person. Change your vote before the poll closes.",
                poll: poll,
                allowedMentions: AllowedMentions.None);
        }
        catch (ArgumentException ex)
        {
            await RespondAsync(ex.Message, ephemeral: true);
        }
    }
}
