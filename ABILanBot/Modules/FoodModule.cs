using ABILanBot.Services;
using Discord;
using Discord.Interactions;

namespace ABILanBot.Modules;

[Group("mat", "Stem over hvor vi skal bestille middag til LAN-et.")]
[CommandContextType(InteractionContextType.Guild)]
public sealed class FoodModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly FoodPollService _polls;

    public FoodModule(FoodPollService polls) => _polls = polls;

    [SlashCommand("stem", "Start en middagsavstemning med restauranter som tilbyr levering i Sandvika.")]
    public async Task StemAsync(
        [Summary("timer", "Hvor mange timer kan folk stemme? (1–24, standard 2)")]
        int timer = 2)
    {
        try
        {
            var poll = _polls.CreatePoll(timer);
            await RespondAsync(
                "🍕 **Middagsavstemning for LAN-et!** Velg én restaurant. " +
                "Du kan endre stemmen din før fristen.\n" +
                "Menyer og bestilling:\n" + _polls.GetOrderingLinks() + "\n" +
                "_Sjekk at restauranten leverer til vår adresse når vi skal bestille._",
                poll: poll,
                allowedMentions: AllowedMentions.None);
        }
        catch (ArgumentException ex)
        {
            await RespondAsync(ex.Message, ephemeral: true);
        }
    }
}
