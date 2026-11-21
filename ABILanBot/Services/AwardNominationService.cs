using System.Text.Json;

namespace ABILanBot.Services;

/// <summary>
/// Stores one nomination per Discord user and award in a local JSON file.
/// The file is written atomically so a process restart preserves nominations.
/// </summary>
public sealed class AwardNominationService
{
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Dictionary<string, Dictionary<string, string>> _nominations;

    public AwardNominationService() : this(Path.Combine(Directory.GetCurrentDirectory(), "data", "award-nominations.json")) { }

    public AwardNominationService(string path)
    {
        _path = path;
        _nominations = File.Exists(path)
            ? JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(path))
                ?? new(StringComparer.Ordinal)
            : new(StringComparer.Ordinal);
    }

    public async Task NominateAsync(ulong guildId, ulong userId, string award, string nominee)
    {
        Validate(award, nominee);
        var key = Key(guildId, award);

        await _gate.WaitAsync();
        try
        {
            // Build a candidate snapshot without modifying the published state.
            // If persistence fails, no rejected nomination can leak into a later save.
            var updated = new Dictionary<string, Dictionary<string, string>>(_nominations, StringComparer.Ordinal);
            var byUser = _nominations.TryGetValue(key, out var current)
                ? new Dictionary<string, string>(current, StringComparer.Ordinal)
                : new Dictionary<string, string>(StringComparer.Ordinal);

            byUser[userId.ToString()] = nominee.Trim();
            if (byUser.Values.Distinct(StringComparer.OrdinalIgnoreCase).Count() > 10)
                throw new ArgumentException("Maks 10 forskjellige nominerte per pris (Discord-begrensning).");

            updated[key] = byUser;
            await SaveAsync(updated);
            _nominations = updated;
        }
        finally { _gate.Release(); }
    }

    public async Task<IReadOnlyList<string>> GetNomineesAsync(ulong guildId, string award)
    {
        Validate(award, "placeholder");
        await _gate.WaitAsync();
        try
        {
            return _nominations.TryGetValue(Key(guildId, award), out var byUser)
                ? byUser.Values.Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToArray()
                : [];
        }
        finally { _gate.Release(); }
    }

    private async Task SaveAsync(Dictionary<string, Dictionary<string, string>> snapshot)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
        var tmp = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(tmp, JsonSerializer.Serialize(snapshot));
            File.Move(tmp, _path, overwrite: true);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }

    private static string Key(ulong guildId, string award) => guildId + ":" + award.Trim().ToLowerInvariant();

    public static void Validate(string award, string nominee)
    {
        if (string.IsNullOrWhiteSpace(award) || award.Length > 60 ||
            award.Any(c => char.IsControl(c) || c is '@' or '<' or '>' or '`'))
            throw new ArgumentException("Prisnavn må være 1–60 tegn uten tagging.");
        if (string.IsNullOrWhiteSpace(nominee) || nominee.Length > 50 ||
            nominee.Any(c => char.IsControl(c) || c is '@' or '<' or '>' or '`' or ','))
            throw new ArgumentException("Nominert må være 1–50 tegn uten tagging.");
    }
}
