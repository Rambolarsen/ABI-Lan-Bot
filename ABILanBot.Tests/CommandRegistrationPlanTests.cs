using System.Net.Sockets;
using System.Reflection;
using ABILanBot.Modules;
using ABILanBot.Services;
using Discord;
using Discord.Interactions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ABILanBot.Tests;

[TestClass]
public sealed class CommandRegistrationPlanTests
{
    [TestMethod]
    public void OnlyFunModulesAreGlobal()
    {
        CollectionAssert.AreEquivalent(
            new[] { typeof(LanFunModule), typeof(DadJokeModule) },
            CommandRegistrationPlan.GlobalModuleTypes.ToArray());

        CollectionAssert.AreEquivalent(
            new[] { typeof(TeamsModule), typeof(Cs2Module), typeof(FoodModule), typeof(GameVoteModule), typeof(AwardModule) },
            CommandRegistrationPlan.GuildModuleTypes.ToArray());
    }

    [TestMethod]
    public void EverySlashModuleMustBeExplicitlyClassified()
    {
        var loaded = typeof(LanFunModule).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract &&
                typeof(InteractionModuleBase<SocketInteractionContext>).IsAssignableFrom(type))
            .ToArray();

        var classified = CommandRegistrationPlan.GlobalModuleTypes
            .Concat(CommandRegistrationPlan.GuildModuleTypes).ToArray();
        Assert.AreEqual(classified.Length, classified.Distinct().Count());
        CollectionAssert.AreEquivalent(loaded, classified);
    }

    [TestMethod]
    public void GlobalModulesExplicitlyAllowGuildAndBotDmContexts()
    {
        foreach (var type in CommandRegistrationPlan.GlobalModuleTypes)
        {
            var attributes = type.GetCustomAttributes(inherit: true);
            Assert.IsTrue(attributes.Any(a => a is CommandContextTypeAttribute),
                $"{type.Name} needs explicit command contexts");
            Assert.IsTrue(attributes.Any(a => a is IntegrationTypeAttribute),
                $"{type.Name} needs explicit GuildInstall integration type");
        }
    }
}
