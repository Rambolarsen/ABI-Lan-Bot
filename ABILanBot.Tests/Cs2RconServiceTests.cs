using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ABILanBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ABILanBot.Tests;

[TestClass]
[DoNotParallelize] // Environment variable used by the production RCON client.
public sealed class Cs2RconServiceTests
{
    [TestMethod]
    public async Task GetStatusAsync_CollectsAllResponsePacketsBeforeReturning()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var previousPassword = Environment.GetEnvironmentVariable("CS2_RCON_PASSWORD");

        try
        {
            Environment.SetEnvironmentVariable("CS2_RCON_PASSWORD", "local-test-secret");
            var serverTask = RespondToStatusAsync(listener);
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Cs2:Host"] = "127.0.0.1",
                    ["Cs2:Port"] = port.ToString()
                })
                .Build();

            var output = await new Cs2RconService(configuration).GetStatusAsync();
            await serverTask;

            Assert.AreEqual("map : de_mirage\nplayers : 2 humans, 0 bots\n", output);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CS2_RCON_PASSWORD", previousPassword);
            listener.Stop();
        }
    }

    private static async Task RespondToStatusAsync(TcpListener listener)
    {
        using var client = await listener.AcceptTcpClientAsync();
        await using var stream = client.GetStream();

        var auth = await ReadPacketAsync(stream);
        Assert.AreEqual(3, auth.Type);
        Assert.AreEqual("local-test-secret", auth.Body);

        // Source RCON sends an empty value packet before its auth result.
        await WritePacketAsync(stream, auth.Id, 0, string.Empty);
        await WritePacketAsync(stream, auth.Id, 2, string.Empty);

        var command = await ReadPacketAsync(stream);
        Assert.AreEqual(2, command.Type);
        Assert.AreEqual("status", command.Body);

        var probe = await ReadPacketAsync(stream);
        Assert.AreEqual(0, probe.Type);
        Assert.AreEqual(string.Empty, probe.Body);
        Assert.AreNotEqual(command.Id, probe.Id);

        // The full response spans two packets; the probe marks its end.
        await WritePacketAsync(stream, command.Id, 0, "map : de_mirage\n");
        await WritePacketAsync(stream, command.Id, 0, "players : 2 humans, 0 bots\n");
        await WritePacketAsync(stream, probe.Id, 0, string.Empty);
    }

    private static async Task<(int Id, int Type, string Body)> ReadPacketAsync(Stream stream)
    {
        var header = new byte[4];
        await stream.ReadExactlyAsync(header.AsMemory());
        var size = BinaryPrimitives.ReadInt32LittleEndian(header);
        Assert.IsTrue(size >= 10, "RCON packets must include ID, type, and terminators.");

        var packet = new byte[size];
        await stream.ReadExactlyAsync(packet.AsMemory());
        return (
            BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(0, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(packet.AsSpan(4, 4)),
            Encoding.UTF8.GetString(packet, 8, size - 10));
    }

    private static async Task WritePacketAsync(Stream stream, int id, int type, string body)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var packet = new byte[14 + bodyBytes.Length];
        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(0, 4), 10 + bodyBytes.Length);
        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(4, 4), id);
        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(8, 4), type);
        bodyBytes.CopyTo(packet, 12);
        await stream.WriteAsync(packet.AsMemory());
    }
}
