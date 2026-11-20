using System.Text.RegularExpressions;

namespace ABILanBot.Services;

/// <summary>
/// Extracts only the public map and human-player count from CS2 status text.
/// The raw response can include player IDs/IP addresses and must not be posted
/// to Discord or written to logs.
/// </summary>
public static class Cs2StatusParser
{
    private static readonly Regex MapLine = new(
        @"^[ \t]*map[ \t]*:[ \t]*([a-z0-9_]+)",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    // CS2 often omits "map :" and places the active map in the first root
    // spawngroup. Require both group identifiers to be 1: subsequent groups
    // may contain skybox or other prefab names instead of the active map.
    private static readonly Regex RootSpawngroup = new(
        @"^[ \t]*loaded[ \t]+spawngroup\([ \t]*1[ \t]*\)[ \t]*:[ \t]*SV:[ \t]*\[[ \t]*1:[ \t]*([a-z0-9_]+)[ \t]*\|",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex PlayersLine = new(
        @"^[ \t]*players[ \t]*:[ \t]*(\d+)[ \t]+humans\b",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static (string? Map, int? HumanPlayers) Parse(string status)
    {
        ArgumentNullException.ThrowIfNull(status);

        var map = MapLine.Match(status);
        if (!map.Success)
            map = RootSpawngroup.Match(status);

        var players = PlayersLine.Match(status);
        int? humanPlayers = players.Success &&
                            int.TryParse(players.Groups[1].Value, out var count)
            ? count
            : null;

        return (map.Success ? map.Groups[1].Value : null, humanPlayers);
    }
}
