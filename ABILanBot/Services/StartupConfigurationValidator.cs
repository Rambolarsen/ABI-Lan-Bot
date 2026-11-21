using Microsoft.Extensions.Configuration;

namespace ABILanBot.Services;

/// <summary>Validate local settings without exposing secrets or connecting to Discord.</summary>
public static class StartupConfigurationValidator
{
    public static void Validate(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (string.IsNullOrWhiteSpace(configuration["Discord:Token"]) ||
            configuration["Discord:Token"] is "YOUR_BOT_TOKEN_HERE" or "PASTE_YOUR_TOKEN_HERE")
            throw new InvalidOperationException("Discord:Token must contain a real bot token.");

        _ = BotConfiguration.GetGuildId(configuration);
        _ = BotConfiguration.IsMessageContentEnabled(configuration);

        var cs2Host = configuration["Cs2:Host"];
        var cs2Port = configuration["Cs2:Port"];
        // CS2 is optional, but incomplete RCON endpoints should fail fast.
        if (!string.IsNullOrWhiteSpace(cs2Host) || !string.IsNullOrWhiteSpace(cs2Port))
        {
            if (string.IsNullOrWhiteSpace(cs2Host) ||
                cs2Host.Any(c => char.IsWhiteSpace(c) || char.IsControl(c) || c is ';') ||
                !int.TryParse(cs2Port, out var port) || port is < 1 or > 65535)
                throw new InvalidOperationException("Cs2:Host and Cs2:Port must be a valid host and TCP port.");
        }
    }
}
