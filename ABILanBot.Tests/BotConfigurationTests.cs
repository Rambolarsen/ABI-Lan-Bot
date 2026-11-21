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
    public void StartupValidationChecksTokenAndGuild()
    {
        Assert.ThrowsExactly<InvalidOperationException>(
            () => StartupConfigurationValidator.Validate(Config(("Discord:GuildId", "123"))));
        StartupConfigurationValidator.Validate(Config(
            ("Discord:GuildId", "123"),
            ("Discord:Token", "ci-test-token-not-real")));
    }

    [TestMethod]
    public void StartupValidationAcceptsJsonComments()
    {
        const string json = """
            {
              // A valid JSON comment supported by the .NET configuration provider.
              "Discord": {
                "Token": "ci-test-token-not-real",
                "GuildId": "123"
              }
            }
            """;
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        var config = new ConfigurationBuilder().AddJsonStream(stream).Build();
        StartupConfigurationValidator.Validate(config);
    }

    [TestMethod]
    public void StartupValidationRejectsIncompleteCs2Endpoint()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            StartupConfigurationValidator.Validate(Config(
                ("Discord:Token", "ci-test-token-not-real"),
                ("Discord:GuildId", "123"), ("Cs2:Host", "oslo21.spillvert.no"))));
        StartupConfigurationValidator.Validate(Config(
            ("Discord:Token", "ci-test-token-not-real"),
            ("Discord:GuildId", "123"), ("Cs2:Host", "oslo21.spillvert.no"),
            ("Cs2:Port", "27020")));
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
        Assert.IsNotNull(provider.GetRequiredService<Cs2RconService>());
        Assert.IsNotNull(ActivatorUtilities.CreateInstance<Cs2Module>(provider));
    }
}
