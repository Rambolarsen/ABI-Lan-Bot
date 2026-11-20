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
    }
}
