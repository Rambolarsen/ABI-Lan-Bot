using ABILanBot.Services;
using Discord.Interactions;

namespace ABILanBot.Modules;

public sealed class LanFunModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly LanFunService _fun;

    public LanFunModule(LanFunService fun) => _fun = fun;

    [SlashCommand("gamepick", "Let the LAN bot choose what to play next!")]
    public async Task GamePick(
        [Summary("games", "Optional comma-separated list of games")] string? games = null)
    {
        try
        {
            var selected = _fun.PickGame(games);
            await RespondAsync($"🎲 **Next up:** {selected}!");
        }
        catch (ArgumentException ex)
        {
            await RespondAsync(ex.Message, ephemeral: true);
        }
    }

    [SlashCommand("challenge", "Draw a ridiculous (but friendly) LAN party challenge.")]
    public async Task Challenge(
        [Summary("game", "Select a game, or leave blank for a general challenge")]
        [Choice("General", "general")]
        [Choice("Counter-Strike 2", "cs2")]
        [Choice("Age of Empires II", "aoe2")]
        [Choice("Rocket League", "rocket league")]
        [Choice("Golf With Your Friends", "golf")]
        [Choice("Overcooked", "overcooked")]
        [Choice("Mario Kart", "mario kart")]
        string game = "general")
    {
        try
        {
            var (category, challenge) = _fun.PickChallenge(game);
            await RespondAsync($"⚡ **{category} challenge:** {challenge}");
        }
        catch (ArgumentException ex)
        {
            await RespondAsync(ex.Message, ephemeral: true);
        }
    }

    [SlashCommand("lanbingo", "Generate a fresh 3x3 LAN bingo card.")]
    public async Task LanBingo()
    {
        var card = _fun.CreateBingoCard();
        const int width = 16;
        var lines = new List<string>();

        for (var row = 0; row < 3; row++)
        {
            lines.Add(string.Join(" | ",
                card.Skip(row * 3).Take(3).Select(s => s.PadRight(width))));
            if (row != 2)
                lines.Add(new string('-', width * 3 + 6));
        }

        var board = string.Join("\n", lines);
        var fence = new string((char)96, 3);
        await RespondAsync($"🎯 **LAN BINGO**\n{fence}\n{board}\n{fence}\nFirst to get three in a row wins. Re-run the command for a new card!");
    }
}
