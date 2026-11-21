using Discord;

namespace ABILanBot.Services;

/// <summary>Builds native Discord polls. Discord owns the voting state and deadline.</summary>
public sealed class LanPollService
{
    private readonly LanFunService _fun;

    public LanPollService(LanFunService fun) => _fun = fun;

    public PollProperties CreateGamePoll(int timer = 1, string? games = null)
    {
        ValidateHours(timer);
        var choices = _fun.GetGameOptions(games);
        return CreatePoll("🎮 Hva spiller vi neste gang?", choices, timer);
    }

    public PollProperties CreateAwardPoll(string category, string nominees, int timer = 1)
    {
        ValidateHours(timer);
        if (string.IsNullOrWhiteSpace(category) || category.Length > 60 ||
            category.Any(c => char.IsControl(c) || c is '@' or '`'))
            throw new ArgumentException("Prisen må ha et navn på maks 60 tegn uten kontrolltegn eller tagging.");

        // Require explicit nominees: Discord's poll API does not support adding candidates later.
        var candidates = ParseNominees(nominees);
        return CreatePoll($"🏆 {category.Trim()} – hvem vinner?", candidates, timer);
    }

    public static IReadOnlyList<string> ParseNominees(string nominees)
    {
        if (string.IsNullOrWhiteSpace(nominees) || nominees.Length > 1000)
            throw new ArgumentException("Oppgi 2–10 nominerte, atskilt med komma (maks 1000 tegn).");

        var entries = nominees.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (entries.Length is < 2 or > 10 ||
            entries.Distinct(StringComparer.OrdinalIgnoreCase).Count() != entries.Length ||
            entries.Any(s => s.Length is < 1 or > 50 ||
                s.Any(c => char.IsControl(c) || c is '@' or '`' or '<' or '>')))
            throw new ArgumentException("Bruk 2–10 unike navn på maks 50 tegn uten tagging eller kontrolltegn.");

        return entries;
    }

    private static void ValidateHours(int timer)
    {
        if (timer is < 1 or > 24)
            throw new ArgumentException("Avstemningen må vare mellom 1 og 24 timer.");
    }

    private static PollProperties CreatePoll(string question, IReadOnlyList<string> answers, int timer) =>
        new()
        {
            Question = new PollMediaProperties { Text = question },
            Answers = answers.Select(s => new PollMediaProperties { Text = s }).ToList(),
            Duration = (uint)timer,
            AllowMultiselect = false,
            LayoutType = PollLayout.Default
        };
}
