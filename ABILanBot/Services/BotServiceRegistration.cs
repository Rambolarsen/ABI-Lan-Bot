using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ABILanBot.Services;

/// <summary>The single composition root for production and tests.</summary>
public static class BotServiceRegistration
{
    public static IServiceCollection AddBotServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton(configuration);
        services.AddSingleton<DiscordSocketClient>(_ =>
            new DiscordSocketClient(CreateSocketConfig(configuration)));
        services.AddSingleton<TeamService>();
        services.AddSingleton<VoiceChannelService>();
        services.AddSingleton<MemberService>();
        services.AddSingleton<LanFunService>();
        services.AddSingleton<FoodPollService>();
        services.AddSingleton<LanPollService>();
        services.AddSingleton<AwardNominationService>();
        services.AddSingleton<Cs2RconService>();
        services.AddHttpClient<DadJokeService>();
        return services;
    }

    public static DiscordSocketConfig CreateSocketConfig(IConfiguration configuration)
    {
        var intents = GatewayIntents.AllUnprivileged;
        if (BotConfiguration.IsMessageContentEnabled(configuration))
            intents |= GatewayIntents.MessageContent;

        return new DiscordSocketConfig { GatewayIntents = intents };
    }
}
