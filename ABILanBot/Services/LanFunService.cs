namespace ABILanBot.Services;

/// <summary>Stateless, in-memory party activities; no external APIs or storage needed.</summary>
public sealed class LanFunService
{
    public static IReadOnlyList<string> DefaultGames { get; } =
    [
        "Counter-Strike 2",
        "Age of Empires II",
        "Rocket League",
        "Golf With Your Friends",
        "Overcooked",
        "Mario Kart"
    ];

    private static readonly Dictionary<string, string[]> Challenges = new(StringComparer.OrdinalIgnoreCase)
    {
        ["General"] =
        [
            "The losing team picks the next game.",
            "Swap one teammate next round (if everyone agrees).",
            "Pick a character, role, or loadout you rarely use.",
            "The winner of the next round must compliment the other team.",
            "For one round, the captain can only give three-word callouts.",
            "Everyone predicts the winning team before the next match."
        ],
        ["CS2"] =
        [
            "Pistol party: your team uses pistols for one casual round.",
            "Use a weapon you almost never buy for one casual round.",
            "Play a round with a new in-game leader calling the shots.",
            "Agree on a ridiculous but harmless strategy for one casual round."
        ],
        ["AoE2"] =
        [
            "Everyone selects a civilization they have barely played.",
            "Use random civilizations in the next friendly match.",
            "Try an unusual opening strategy in your next friendly match.",
            "Predict who reaches Castle Age first in the next friendly match."
        ],
        ["Rocket League"] =
        [
            "Rotate who takes kickoff in the next casual match.",
            "Try to score a goal from a teammate's assist.",
            "Use a car preset you rarely pick for one match.",
            "Everyone predicts the final score before kickoff."
        ],
        ["Golf"] =
        [
            "Everyone attempts a bank shot on the next hole.",
            "The player in last place picks everyone's next ball cosmetic.",
            "Everyone predicts their own stroke count on the next hole.",
            "No advice until everyone has finished the next hole."
        ],
        ["Overcooked"] =
        [
            "Swap kitchen roles after each completed order.",
            "Appoint a chef to call the next three orders.",
            "Everyone announces what they're carrying for one level.",
            "Swap your usual kitchen positions next level."
        ],
        ["Mario Kart"] =
        [
            "Everyone selects a random character for the next race.",
            "Try a kart setup you've never used before.",
            "Last place picks the next track.",
            "Everyone predicts the winner before the next race."
        ]
    };

    private static readonly string[] BingoPrompts =
    [
        "Blames lag", "Muted mic", "Wrong server", "One more!", "Game crashes",
        "Alt-tab fail", "Pizza time", "AFK for food", "Teamkill", "Invite lost",
        "Wrong screen", "Who's host?", "Patch needed", "Rage quits", "Pad dies",
        "Not installed", "Discord dies", "No sound", "What map?", "Slow loading",
        "Team damage", "Disconnect", "Steam update", "Wrong lobby", "Bathroom AFK",
        "Snack break", "Own goal", "Forgot rules", "Hot mic", "Wrong game"
    ];

    public IReadOnlyList<string> GetGameOptions(string? commaSeparatedGames)
    {
        if (string.IsNullOrWhiteSpace(commaSeparatedGames))
            return DefaultGames;

        if (commaSeparatedGames.Length > 1000)
            throw new ArgumentException("The game list is too long (maximum 1000 characters).");

        var games = commaSeparatedGames
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (games.Length is < 2 or > 20)
            throw new ArgumentException("Provide 2 to 20 different games, separated by commas.");

        if (games.Any(game => game.Length > 50 ||
            game.Any(c => !(char.IsLetterOrDigit(c) || c == ' ' ||
                c is '-' or '\'' or '&' or ':' or '+' or '.' or '(' or ')'))))
        {
            throw new ArgumentException("Game names must be at most 50 characters, using only letters, numbers, spaces or common punctuation.");
        }

        return games;
    }

    public string PickGame(string? commaSeparatedGames = null)
    {
        var games = GetGameOptions(commaSeparatedGames);
        return games[Random.Shared.Next(games.Count)];
    }

    public (string Category, string Text) PickChallenge(string? game = null)
    {
        var category = NormalizeGame(game);
        var options = Challenges[category];
        return (category, options[Random.Shared.Next(options.Length)]);
    }

    /// <summary>Creates a randomized 3x3 card with eight unique events and a free center.</summary>
    public IReadOnlyList<string> CreateBingoCard()
    {
        var shuffled = BingoPrompts.ToArray();
        for (var i = shuffled.Length - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        return
        [
            shuffled[0], shuffled[1], shuffled[2],
            shuffled[3], "FREE SPACE", shuffled[4],
            shuffled[5], shuffled[6], shuffled[7]
        ];
    }

    private static string NormalizeGame(string? game)
    {
        return game?.Trim().ToLowerInvariant() switch
        {
            null or "" or "general" => "General",
            "cs2" or "cs" or "counter-strike 2" => "CS2",
            "aoe2" or "aoe" or "age of empires ii" => "AoE2",
            "rl" or "rocket league" => "Rocket League",
            "golf" or "golf with your friends" => "Golf",
            "overcooked" => "Overcooked",
            "mario kart" or "mariokart" => "Mario Kart",
            _ => throw new ArgumentException("Unknown game category. Choose one of the suggested options.")
        };
    }
}
