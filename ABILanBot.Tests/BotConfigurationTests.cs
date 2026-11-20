using ABILanBot.Modules;
using ABILanBot.Services;
using Discord;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ABILanBot.Tests;

[TestClass]
public sealed class BotConfigurationTests
{
    private static IConfiguration Config(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(
            values.ToDictionary(x => x.Key, x => x.Value)).Build();

    [TestMethod]
    public void ExistingBotsRequestMessageContentByDefault()
    {
        var config = Config();
        Assert.IsTrue(BotConfiguration.IsMessageContentEnabled(config));
        Assert.IsTrue(BotServiceRegistration.CreateSocketConfig(config)
            .GatewayIntents.HasFlag(GatewayIntents.MessageContent));
    }

    [TestMethod]
    public void SlashOnlyModeOmitsPrivilegedIntent()
    {
        var config = Config(("Discord:EnableMessageContentIntent", "false"));
        Assert.IsFalse(BotConfiguration.IsMessageContentEnabled(config));
        Assert.IsFalse(BotServiceRegistration.CreateSocketConfig(config)
            .GatewayIntents.HasFlag(GatewayIntents.MessageContent));
    }

    [TestMethod]
    public void InvalidIntentSettingIsRejected()
    {
        var config = Config(("Discord:EnableMessageContentIntent", "maybe"));
        Assert.ThrowsExactly<InvalidOperationException>(
            () => BotConfiguration.IsMessageContentEnabled(config));
    }

    [TestMethod]
    public void GuildIdMustBeConfigured()
    {
        Assert.AreEqual(1439887410107519087UL,
            BotConfiguration.GetGuildId(Config(("Discord:GuildId", "1439887410107519087"))));
        Assert.ThrowsExactly<InvalidOperationException>(
            () => BotConfiguration.GetGuildId(Config()));
        Assert.ThrowsExactly<InvalidOperationException>(
            () => BotConfiguration.GetGuildId(Config(("Discord:GuildId", "not-a-number"))));
    }

    [TestMethod]
    public void SlashModuleDependenciesResolveFromProductionRegistration()
    {
        using var provider = new ServiceCollection()
            .AddBotServices(Config(("Discord:EnableMessageContentIntent", "false")))
            .BuildServiceProvider(validateScopes: true);

        Assert.IsNotNull(provider.GetRequiredService<LanFunService>());
        Assert.IsNotNull(ActivatorUtilities.CreateInstance<LanFunModule>(provider));
    }
}
