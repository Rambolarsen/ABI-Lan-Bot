using ABILanBot.Services;
using Discord;
using Discord.Interactions;

namespace ABILanBot.Modules;

[Group("award", "LAN-priser og kåringer")]
[CommandContextType(InteractionContextType.Guild)]
public sealed class AwardModule : InteractionModuleBase<SocketInteractionContext>
{
    public const string AwardName = "Årets gamer";
    private readonly LanPollService _polls;
    private readonly AwardNominationService _nominations;

    public AwardModule(LanPollService polls, AwardNominationService nominations)
    {
        _polls = polls;
        _nominations = nominations;
    }

    [SlashCommand("nominer", "Nominer en deltaker til Årets gamer")]
    public async Task NominateAsync(
        [Summary("deltaker", "Navnet på deltakeren")] string deltaker)
    {
        try
        {
            await _nominations.NominateAsync(Context.Guild.Id, Context.User.Id, AwardName, deltaker);
            await RespondAsync($"🏆 Nominasjonen til **{AwardName}** er registrert. Du kan endre den ved å nominere på nytt.",
                ephemeral: true, allowedMentions: AllowedMentions.None);
        }
        catch (ArgumentException ex)
        {
            await RespondAsync(ex.Message, ephemeral: true);
        }
        catch (IOException)
        {
            await RespondAsync("Kunne ikke lagre nominasjonen lokalt. Kontakt arrangøren.", ephemeral: true);
        }
    }


    [SlashCommand("stem", "Start avstemning om Årets gamer blant nominerte")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task VoteAsync(
        [Summary("nominerte", "Valgfri overstyring: 2–10 navn atskilt med komma")] string? nominerte = null,
        [Summary("timer", "Varighet i timer, 1–24")] int timer = 1)
    {
        try
        {
            var names = nominerte;
            if (string.IsNullOrWhiteSpace(names))
                names = string.Join(", ", await _nominations.GetNomineesAsync(Context.Guild.Id, AwardName));
            var poll = _polls.CreateAwardPoll(AwardName, names, timer);
            await RespondAsync("🏆 **Årets gamer!** Stem på én nominert. Resultatet vises i Discord når avstemningen slutter.",
                poll: poll, allowedMentions: AllowedMentions.None);
        }
        catch (ArgumentException ex)
        {
            await RespondAsync(ex.Message, ephemeral: true);
        }
    }
}
