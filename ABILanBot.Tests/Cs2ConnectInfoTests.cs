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
    public void PasswordIsOptionalForPublicServers()
    {
        var config = Config(("Cs2:Host", "server.example.com"), ("Cs2:Port", "27015"));
        var join = Cs2ConnectInfo.GetJoinInfo(config);
        Assert.AreEqual("server.example.com:27015", join.Address);
        Assert.IsNull(join.Password);
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
    public void RejectsMissingAddress()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => Cs2ConnectInfo.GetJoinInfo(Config()));
    }
}
