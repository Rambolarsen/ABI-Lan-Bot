using System.Net.Sockets;
using ABILanBot.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ABILanBot.Tests;

[TestClass]
public sealed class Cs2RconErrorMessagesTests
{
    [DataTestMethod]
    [DataRow("bad-password", "avviste passordet")]
    [DataRow("no-password", "mangler")]
    [DataRow("bad-config", "ufullstendig")]
    [DataRow("timeout", "tidsavbrudd")]
    [DataRow("network", "Kunne ikke koble til")]
    [DataRow("protocol", "RCON-svar")]
    public void ReportsSpecificSafeFailure(string kind, string expected)
    {
        Exception error = kind switch
        {
            "bad-password" => new UnauthorizedAccessException("my-secret"),
            "no-password" => new InvalidOperationException("CS2_RCON_PASSWORD må settes."),
            "bad-config" => new InvalidOperationException("Cs2:Host missing"),
            "timeout" => new OperationCanceledException(),
            "network" => new SocketException((int)SocketError.ConnectionRefused),
            "protocol" => new IOException("raw address or password my-secret"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        var userMessage = Cs2RconErrorMessages.ToUserMessage(error);
        StringAssert.Contains(userMessage, expected);
        Assert.IsFalse(userMessage.Contains("my-secret", StringComparison.Ordinal));
        Assert.IsFalse(userMessage.Contains("Cs2:Host missing", StringComparison.Ordinal));
    }

    [TestMethod]
    public void UnexpectedErrorIsNotExposedToDiscord()
    {
        var message = Cs2RconErrorMessages.ToUserMessage(new Exception("token=secret"));
        Assert.IsFalse(message.Contains("token=secret", StringComparison.Ordinal));
    }
}
