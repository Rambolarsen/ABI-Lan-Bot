using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace ABILanBot.Services;

/// <summary>
/// Public game join details. These are independent of the private RCON
/// administration password, which must never appear in Discord.
/// </summary>
public static class Cs2ConnectInfo
{
    private static readonly Regex HostPattern = new(
        @"\A[a-z0-9](?:[a-z0-9.-]{0,251}[a-z0-9])?\z",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex PasswordPattern = new(
        @"\A[a-zA-Z0-9_-]{1,64}\z",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private const string DefaultJoinPassword = "bouvet";

    public static (string Address, string Password) GetJoinInfo(IConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        var host = config["Cs2:JoinHost"] ?? config["Cs2:Host"];
        var portText = config["Cs2:JoinPort"] ?? config["Cs2:Port"];
        // Bouvet-LAN requires a player password. Existing local appsettings.json
        // files may predate JoinPassword, so preserve that requirement by default.
        var password = config["Cs2:JoinPassword"] ?? DefaultJoinPassword;

        // Do not allow Discord Markdown, mentions, newline, or console-command
        // separators from configuration into the public response.
        if (string.IsNullOrWhiteSpace(host) || !HostPattern.IsMatch(host) ||
            !ushort.TryParse(portText, out var port) || port == 0 ||
            !PasswordPattern.IsMatch(password))
            throw new InvalidOperationException("Invalid CS2 join configuration.");

        return ($"{host}:{port}", password);
    }
}
