using ABILanBot.Services;
using Discord;
using Discord.Interactions;

namespace ABILanBot.Modules;

[Group("cs2", "Status og administrasjon av Bouvet-LAN CS2-serveren")]
[CommandContextType(InteractionContextType.Guild)]
public sealed class Cs2Module : InteractionModuleBase<SocketInteractionContext>
{
    private readonly Cs2RconService _rcon;

    private readonly Microsoft.Extensions.Configuration.IConfiguration _config;

    public Cs2Module(Cs2RconService rcon, Microsoft.Extensions.Configuration.IConfiguration config)
    {
        _rcon = rcon;
        _config = config;
    }


    [SlashCommand("connect", "Vis CS2-serveradresse og passord for å bli med")]
    public async Task ConnectAsync()
    {
        try
        {
            // Read-only join information. Do not connect to RCON or expose its
            // separate CS2_RCON_PASSWORD environment secret.
            var join = Cs2ConnectInfo.GetJoinInfo(_config);
            var command = $"password {join.Password}; connect {join.Address}";
            var passwordLine = $"Passord: `{join.Password}`";

            await RespondAsync(
                $"🎮 **Bli med på Bouvet-LAN CS2**\n" +
                $"Server: `{join.Address}`\n" +
                $"{passwordLine}\n" +
                "Åpne utviklerkonsollen i CS2 og lim inn:\n" +
                $"```\n{command}\n```");
        }
        catch (InvalidOperationException)
        {
            await RespondAsync("❌ CS2-tilkoblingsinformasjonen er ikke konfigurert riktig.", ephemeral: true);
        }
    }

    [SlashCommand("status", "Se om CS2-serveren svarer, med kart og antall spillere")]
    public async Task StatusAsync()
    {
        await DeferAsync();
        try
        {
            var status = await _rcon.GetStatusAsync();

            // Avoid posting the full status output: it can contain player IDs
            // and connection information. Show only non-sensitive fields.
            var parsed = Cs2StatusParser.Parse(status);
            var lines = new List<string>
            {
                $"✅ **CS2-serveren svarer på RCON**",
                $"Adresse: `{_rcon.ServerAddress}`",
                $"Kart: **{(parsed.Map ?? "ukjent")}**",
                $"Spillere: **{(parsed.HumanPlayers?.ToString() ?? "ukjent")}**"
            };
            await FollowupAsync(string.Join("\n", lines));
        }
        catch (Exception ex)
        {
            await ReportErrorAsync(ex);
        }
    }

    [SlashCommand("mode", "Bytt spillmodus (laster et nytt kart)")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task ModeAsync(Cs2GameMode mode)
    {
        await DeferAsync(ephemeral: true);
        try
        {
            await _rcon.SetModeAsync(mode);
            await FollowupAsync(
                $"🎮 Kommandoer for **{mode}** sendt. Serveren laster et kart på nytt. " +
                "Kontroller med `/cs2 status` før dere spiller.",
                ephemeral: true);
        }
        catch (Exception ex)
        {
            await ReportErrorAsync(ex);
        }
    }

    [SlashCommand("preset", "Bruk en forhåndsdefinert CS2-oppsett (krever bekreftelse)")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task PresetAsync(
        [Summary("preset", "Warmup eller Competitive")] Cs2MatchPreset preset,
        [Summary("bekreft", "Bekreft at dette kan påvirke den aktive kampen")] bool bekreft = false)
    {
        if (!bekreft)
        {
            await RespondAsync(
                "⚠️ Dette kan påvirke en aktiv kamp. Kjør kommandoen på nytt med `bekreft: true`.",
                ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);
        try
        {
            await _rcon.ApplyPresetAsync(preset);
            await FollowupAsync(
                preset == Cs2MatchPreset.Warmup
                    ? "🔥 Warmup-kommando sendt. Gjeldende kart beholdes."
                    : "🏆 Competitive-oppsett sendt. Kartet lastes på nytt; sjekk `/cs2 status` etterpå.",
                ephemeral: true);
        }
        catch (Exception ex)
        {
            await ReportErrorAsync(ex);
        }
    }

    [SlashCommand("map", "Bytt til et kart installert på serveren")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task MapAsync(string map)
    {
        await DeferAsync(ephemeral: true);
        try
        {
            await _rcon.ChangeMapAsync(map);
            await FollowupAsync(
                $"🗺️ Kartbytte til **{map}** sendt. Sjekk `/cs2 status` etter innlasting.",
                ephemeral: true);
        }
        catch (Exception ex)
        {
            await ReportErrorAsync(ex);
        }
    }

    [SlashCommand("restartmatch", "Restart gjeldende kamp, ikke hele spillserveren")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task RestartMatchAsync()
    {
        await DeferAsync(ephemeral: true);
        try
        {
            await _rcon.RestartMatchAsync();
            await FollowupAsync("🔁 Kamprestart sendt (`mp_restartgame 1`).", ephemeral: true);
        }
        catch (Exception ex)
        {
            await ReportErrorAsync(ex);
        }
    }

    private async Task ReportErrorAsync(Exception exception)
    {
        // Do not publish raw exception details to Discord.
        Console.Error.WriteLine($"CS2 RCON failure: {exception.GetType().Name}");
        await FollowupAsync(Cs2RconErrorMessages.ToUserMessage(exception), ephemeral: true);
    }
}
