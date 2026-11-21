using ABILanBot.Services;
using Discord;
using Discord.Interactions;

namespace ABILanBot.Modules;

[Group("spill", "Avstemning om neste spill på LAN-et")]
[CommandContextType(InteractionContextType.Guild)]
public sealed class GameVoteModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly LanPollService _polls;

    public GameVoteModule(LanPollService polls) => _polls = polls;

    [SlashCommand("stem", "La deltakerne velge neste spill")]
    public async Task VoteAsync(
        [Summary("timer", "Varighet i timer, 1–24")] int timer = 1,
        [Summary("spill", "Valgfri kommaseparert liste med 2–20 spill")] string? spill = null)
    {
        try
        {
            var poll = _polls.CreateGamePoll(timer, spill);
            await RespondAsync("🎮 **Hva skal vi spille?** Én stemme hver – dere kan endre den frem til fristen.",
                poll: poll, allowedMentions: AllowedMentions.None);
        }
        catch (ArgumentException ex)
        {
            await RespondAsync(ex.Message, ephemeral: true);
        }
    }
}
