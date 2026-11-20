using Microsoft.Extensions.Configuration;

namespace ABILanBot.Services;

public static class BotConfiguration
{
    /// <summary>True by default to preserve existing text commands.</summary>
    public static bool IsMessageContentEnabled(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var setting = configuration["Discord:EnableMessageContentIntent"];
        if (string.IsNullOrWhiteSpace(setting))
            return true;
        if (bool.TryParse(setting, out var enabled))
            return enabled;
        throw new InvalidOperationException(
            "Discord:EnableMessageContentIntent must be true or false.");
    }

    public static ulong GetGuildId(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (ulong.TryParse(configuration["Discord:GuildId"], out var guildId) && guildId > 0)
            return guildId;
        throw new InvalidOperationException(
            "Discord:GuildId must contain a valid Discord server ID.");
    }
}
