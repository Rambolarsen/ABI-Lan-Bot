using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace ABILanBot.Services;

public enum Cs2GameMode
{
    ArmsRace,
    Deathmatch,
    Casual,
    Competitive
}

/// <summary>
/// Minimal Source RCON client for the CS2 server. Each command uses a fresh TCP
/// connection, so map changes cannot leave a shared connection in a bad state.
/// RCON itself is not encrypted: only configure trusted networks and keep the
/// password secret.
/// </summary>
public sealed class Cs2RconService
{
    private const int MaxPacketSize = 1024 * 1024;
    private readonly IConfiguration _config;
    private readonly SemaphoreSlim _adminLock = new(1, 1);

    public Cs2RconService(IConfiguration config) => _config = config;

    public string ServerAddress => $"{_config["Cs2:Host"]}:{_config["Cs2:Port"]}";

    public Task<string> GetStatusAsync() => ExecuteAsync("status");

    public async Task SetModeAsync(Cs2GameMode mode)
    {
        var (gameType, gameMode, map) = mode switch
        {
            Cs2GameMode.ArmsRace => (1, 0, "ar_pool_day"),
            Cs2GameMode.Deathmatch => (1, 2, "de_dust2"),
            Cs2GameMode.Casual => (0, 0, "de_mirage"),
            Cs2GameMode.Competitive => (0, 1, "de_mirage"),
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };

        await _adminLock.WaitAsync();
        try
        {
            // CS2 applies game_type/game_mode when it loads the next map.
            await ExecuteAsync($"game_type {gameType}");
            await ExecuteAsync($"game_mode {gameMode}");
            await ExecuteAsync($"changelevel {map}", allowDisconnect: true);
        }
        finally
        {
            _adminLock.Release();
        }
    }

    public async Task ChangeMapAsync(string map)
    {
        // Prevent a map argument from becoming an arbitrary console command.
        if (!Regex.IsMatch(map, @"^[a-z][a-z0-9_]{1,63}$", RegexOptions.CultureInvariant))
            throw new ArgumentException("Kartnavnet kan bare inneholde små bokstaver, tall og understrek.", nameof(map));

        await _adminLock.WaitAsync();
        try
        {
            await ExecuteAsync($"changelevel {map}", allowDisconnect: true);
        }
        finally
        {
            _adminLock.Release();
        }
    }

    public async Task RestartMatchAsync()
    {
        await _adminLock.WaitAsync();
        try
        {
            await ExecuteAsync("mp_restartgame 1");
        }
        finally
        {
            _adminLock.Release();
        }
    }

    private async Task<string> ExecuteAsync(string command, bool allowDisconnect = false)
    {
        var host = _config["Cs2:Host"];
        var password = Environment.GetEnvironmentVariable("CS2_RCON_PASSWORD");
        if (string.IsNullOrWhiteSpace(host) ||
            !int.TryParse(_config["Cs2:Port"], out var port) ||
            port is < 1 or > 65535)
            throw new InvalidOperationException("Cs2:Host og Cs2:Port må konfigureres.");

        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("CS2_RCON_PASSWORD må settes i miljøet til botten.");

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        using var client = new TcpClient();
        await client.ConnectAsync(host, port, timeout.Token);
        await using var stream = client.GetStream();

        var authId = Random.Shared.Next(1, int.MaxValue - 2);
        await WritePacketAsync(stream, authId, 3, password, timeout.Token);

        // Source RCON can send an empty SERVERDATA_RESPONSE_VALUE before the
        // SERVERDATA_AUTH_RESPONSE; only type 2 confirms authentication.
        var authenticated = false;
        for (var i = 0; i < 5; i++)
        {
            var packet = await ReadPacketAsync(stream, timeout.Token);
            if (packet.Type != 2)
                continue;

            if (packet.Id == -1)
                throw new UnauthorizedAccessException("RCON avviste passordet.");

            if (packet.Id == authId)
            {
                authenticated = true;
                break;
            }
        }

        if (!authenticated)
            throw new IOException("Mottok ikke bekreftet RCON-autentisering.");

        var commandId = authId + 1;
        await WritePacketAsync(stream, commandId, 2, command, timeout.Token);

        // The command can close the connection when it changes maps.
        // A disconnected response means the request was sent, NOT that the
        // server has finished loading the new map.
        try
        {
            using var replyTimeout = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
            replyTimeout.CancelAfter(TimeSpan.FromSeconds(3));
            for (var i = 0; i < 8; i++)
            {
                var reply = await ReadPacketAsync(stream, replyTimeout.Token);
                if (reply.Id == commandId)
                    return reply.Body;
            }
            throw new IOException("Mottok ikke svar på RCON-kommandoen.");
        }
        catch (EndOfStreamException) when (allowDisconnect)
        {
            return string.Empty;
        }
        catch (IOException) when (allowDisconnect && !client.Connected)
        {
            return string.Empty;
        }
    }

    private static async Task WritePacketAsync(
        Stream stream, int id, int type, string body, CancellationToken token)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        var packet = new byte[14 + bytes.Length]; // length + id + type + text + two NULs
        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(0, 4), 10 + bytes.Length);
        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(4, 4), id);
        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(8, 4), type);
        bytes.CopyTo(packet, 12);
        await stream.WriteAsync(packet, token);
    }

    private static async Task<RconPacket> ReadPacketAsync(Stream stream, CancellationToken token)
    {
        var lengthBytes = new byte[4];
        await stream.ReadExactlyAsync(lengthBytes, token);
        var length = BinaryPrimitives.ReadInt32LittleEndian(lengthBytes);

        if (length is < 10 or > MaxPacketSize)
            throw new IOException("Serveren svarte med en ugyldig RCON-pakkestørrelse.");

        var data = new byte[length];
        await stream.ReadExactlyAsync(data, token);
        var id = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(0, 4));
        var type = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(4, 4));
        var text = Encoding.UTF8.GetString(data, 8, length - 10);
        return new RconPacket(id, type, text);
    }

    private sealed record RconPacket(int Id, int Type, string Body);
}
