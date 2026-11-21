using ABILanBot.Modules;
using Discord.Interactions;

namespace ABILanBot.Services;

/// <summary>Explicit publication allowlist: new modules are not global by default.</summary>
public static class CommandRegistrationPlan
{
    public static IReadOnlyList<Type> GlobalModuleTypes { get; } =
        [typeof(LanFunModule), typeof(DadJokeModule)];
    public static IReadOnlyList<Type> GuildModuleTypes { get; } =
        [typeof(TeamsModule), typeof(Cs2Module), typeof(FoodModule), typeof(GameVoteModule), typeof(AwardModule)];

    public static async Task<(ModuleInfo[] Global, ModuleInfo[] Guild)> LoadAsync(
        InteractionService interactions, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(interactions);
        ArgumentNullException.ThrowIfNull(services);
        var global = new List<ModuleInfo>();
        var guild = new List<ModuleInfo>();
        foreach (var type in GlobalModuleTypes)
            global.Add(await interactions.AddModuleAsync(type, services));
        foreach (var type in GuildModuleTypes)
            guild.Add(await interactions.AddModuleAsync(type, services));
        return (global.ToArray(), guild.ToArray());
    }
}
