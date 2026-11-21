using ABILanBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ABILanBot.Tests;

[TestClass]
public sealed class Cs2ConnectInfoTests
{
    private static IConfiguration Config(params (string Key, string? Value)[] entries) =>
        new ConfigurationBuilder().AddInMemoryCollection(
            entries.ToDictionary(x => x.Key, x => x.Value)).Build();

    [TestMethod]
    public void ReturnsGameAddressAndPublicJoinPassword()
    {
        var config = Config(
            ("Cs2:Host", "oslo21.spillvert.no"),
            ("Cs2:Port", "27020"),
            ("Cs2:JoinPassword", "bouvet"));
        var join = Cs2ConnectInfo.GetJoinInfo(config);
        Assert.AreEqual("oslo21.spillvert.no:27020", join.Address);
        Assert.AreEqual("bouvet", join.Password);
    }

    [TestMethod]
    public void SupportsDistinctJoinAndRconEndpoints()
    {
        var config = Config(
            ("Cs2:Host", "rcon.internal"),
            ("Cs2:Port", "27020"),
            ("Cs2:JoinHost", "91.189.178.190"),
            ("Cs2:JoinPort", "27021"),
            ("Cs2:JoinPassword", "bouvet"));
        var join = Cs2ConnectInfo.GetJoinInfo(config);
        Assert.AreEqual("91.189.178.190:27021", join.Address);
        Assert.AreEqual("bouvet", join.Password);
    }

    [TestMethod]
    public void MissingPasswordUsesBouvetLanDefault()
    {
        // This is the shape of a previously deployed appsettings.json.
        var config = Config(("Cs2:Host", "oslo21.spillvert.no"), ("Cs2:Port", "27020"));
        var join = Cs2ConnectInfo.GetJoinInfo(config);
        Assert.AreEqual("oslo21.spillvert.no:27020", join.Address);
        Assert.AreEqual("bouvet", join.Password);
    }

    [TestMethod]
    public void ExplicitlyBlankPasswordDoesNotMisrepresentPasswordProtectedServer()
    {
        var config = Config(
            ("Cs2:Host", "oslo21.spillvert.no"),
            ("Cs2:Port", "27020"),
            ("Cs2:JoinPassword", ""));
        Assert.ThrowsExactly<InvalidOperationException>(() => Cs2ConnectInfo.GetJoinInfo(config));
    }

    [TestMethod]
    public void ExplicitJoinPasswordCanBeChanged()
    {
        var config = Config(
            ("Cs2:Host", "oslo21.spillvert.no"),
            ("Cs2:Port", "27020"),
            ("Cs2:JoinPassword", "new_join_password"));
        Assert.AreEqual("new_join_password", Cs2ConnectInfo.GetJoinInfo(config).Password);
    }

    [DataTestMethod]
    [DataRow("server.example.com;quit", "27020", "bouvet")]
    [DataRow("@everyone", "27020", "bouvet")]
    [DataRow("server.example.com", "0", "bouvet")]
    [DataRow("server.example.com", "65536", "bouvet")]
    [DataRow("server.example.com", "abc", "bouvet")]
    [DataRow("server.example.com", "27020", "bouvet;quit")]
    [DataRow("server.example.com", "27020", "bouvet\n@everyone")]
    public void RejectsUnsafeOrInvalidJoinDetails(string host, string port, string password)
    {
        var config = Config(
            ("Cs2:JoinHost", host),
            ("Cs2:JoinPort", port),
            ("Cs2:JoinPassword", password));
        Assert.ThrowsExactly<InvalidOperationException>(() => Cs2ConnectInfo.GetJoinInfo(config));
    }

    [TestMethod]
    public void DiscordHowToExplainsConsoleAndIncludesRequiredPassword()
    {
        var config = Config(
            ("Cs2:Host", "oslo21.spillvert.no"),
            ("Cs2:Port", "27020"));
        var instructions = Cs2ConnectInfo.BuildDiscordHowTo(config);

        StringAssert.Contains(instructions, "Slik kobler du til");
        StringAssert.Contains(instructions, "Aktiver utviklerkonsoll");
        StringAssert.Contains(instructions, "Start **Counter-Strike 2**");
        StringAssert.Contains(instructions, "password bouvet; connect oslo21.spillvert.no:27020");
        StringAssert.Contains(instructions, "Spillpassord: `bouvet`");
        Assert.IsFalse(instructions.Contains("CS2_RCON_PASSWORD", StringComparison.Ordinal));
        Assert.IsFalse(instructions.Contains("Ikke nødvendig", StringComparison.Ordinal));
    }

    [TestMethod]
    public void DiscordHowToRespectsConfiguredJoinEndpointAndPassword()
    {
        var config = Config(
            ("Cs2:JoinHost", "cs2.example.org"),
            ("Cs2:JoinPort", "27015"),
            ("Cs2:JoinPassword", "friends"));
        var instructions = Cs2ConnectInfo.BuildDiscordHowTo(config);

        StringAssert.Contains(instructions, "password friends; connect cs2.example.org:27015");
    }

    [TestMethod]
    public void RejectsMissingAddress()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => Cs2ConnectInfo.GetJoinInfo(Config()));
    }
}
