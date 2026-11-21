using ABILanBot.Services;
using Discord;
using Discord.Interactions;

namespace ABILanBot.Modules;

[Group("award", "LAN-priser og kåringer")]
[CommandContextType(InteractionContextType.Guild)]
public sealed class AwardModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly LanPollService _polls;

    public AwardModule(LanPollService polls) => _polls = polls;

    [SlashCommand("stem", "Start avstemning om LAN-pris blant nominerte")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task VoteAsync(
        [Summary("pris", "For eksempel MVP, Biggest Throw eller LAN Clown")] string pris,
        [Summary("nominerte", "2–10 nominerte, atskilt med komma")] string nominerte,
        [Summary("timer", "Varighet i timer, 1–24")] int timer = 1)
    {
        try
        {
            var poll = _polls.CreateAwardPoll(pris, nominerte, timer);
            await RespondAsync("🏆 **LAN Awards!** Stem på én nominert. Resultatet vises i Discord når avstemningen slutter.",
                poll: poll, allowedMentions: AllowedMentions.None);
        }
        catch (ArgumentException ex)
        {
            await RespondAsync(ex.Message, ephemeral: true);
        }
    }
}
