using System.Text.RegularExpressions;
using ABILanBot.Services;
using Discord;
using Discord.Interactions;

namespace ABILanBot.Modules;

[Group("cs2", "Status og administrasjon av Bouvet-LAN CS2-serveren")]
[CommandContextType(InteractionContextType.Guild)]
public sealed class Cs2Module : InteractionModuleBase<SocketInteractionContext>
{
    private readonly Cs2RconService _rcon;

    public Cs2Module(Cs2RconService rcon) => _rcon = rcon;

    [SlashCommand("status", "Se om CS2-serveren svarer, med kart og antall spillere")]
    public async Task StatusAsync()
    {
        await DeferAsync();
        try
        {
            var status = await _rcon.GetStatusAsync();

            // Avoid posting the full status output: it can contain player IDs
            // and connection information. Show only non-sensitive fields.
            var map = Regex.Match(status, @"(?im)^\s*map\s*:\s*([a-z0-9_]+)");
            var players = Regex.Match(status, @"(?im)^\s*players\s*:\s*(\d+)\s+humans");
            var lines = new List<string>
            {
                $"✅ **CS2-serveren svarer på RCON**",
                $"Adresse: `{_rcon.ServerAddress}`",
                $"Kart: **{(map.Success ? map.Groups[1].Value : "ukjent")}**",
                $"Spillere: **{(players.Success ? players.Groups[1].Value : "ukjent")}**"
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
