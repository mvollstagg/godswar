using System.Collections.Concurrent;
using GodsWar.Protocol;
using GodsWar.Protocol.Packets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GodsWar.LoginServer;

public sealed class RegisteredGameServer
{
    public required MsgGameServerInfo Info { get; set; }
    public required PacketConnection Connection { get; init; }
    public DateTimeOffset LastUpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed record GameServerLookupResult(
    bool Found,
    bool Unavailable,
    bool SentValidateToGameServer,
    MsgGameServerInfo ServerInfo);

public sealed class GameServerRegistry
{
    private readonly ConcurrentDictionary<byte, RegisteredGameServer> _servers = new();
    private readonly LoginServerOptions _options;
    private readonly ILogger<GameServerRegistry> _logger;

    public GameServerRegistry(
        IOptions<LoginServerOptions> options,
        ILogger<GameServerRegistry> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public void RegisterOrUpdate(MsgGameServerInfo info, PacketConnection connection)
    {
        string effectiveName = string.IsNullOrWhiteSpace(info.ServerName) ? _options.ServerName : info.ServerName;
        string effectiveIp = string.IsNullOrWhiteSpace(info.Ip) || info.Ip == "0.0.0.0"
            ? _options.AdvertiseIp
            : info.Ip;
        uint effectivePort = info.Port == 0 ? (uint)_options.GamePort : info.Port;

        var normalized = new MsgGameServerInfo(effectiveName, effectiveIp, effectivePort, info.ServerId, info.State);

        _servers.AddOrUpdate(
            info.ServerId,
            _ => new RegisteredGameServer { Info = normalized, Connection = connection },
            (_, existing) =>
            {
                if (!ReferenceEquals(existing.Connection, connection))
                {
                    return new RegisteredGameServer { Info = normalized, Connection = connection };
                }

                existing.Info = normalized;
                existing.LastUpdatedUtc = DateTimeOffset.UtcNow;
                return existing;
            });

        _logger.LogInformation(
            "GameServer registered/updated: Id={ServerId} Name={Name} Endpoint={Ip}:{Port} State={State}",
            normalized.ServerId,
            normalized.ServerName,
            normalized.Ip,
            normalized.Port,
            normalized.State);
    }

    public void UnregisterConnection(PacketConnection connection)
    {
        foreach (KeyValuePair<byte, RegisteredGameServer> kvp in _servers)
        {
            if (ReferenceEquals(kvp.Value.Connection, connection) && _servers.TryRemove(kvp.Key, out RegisteredGameServer? removed))
            {
                _logger.LogInformation(
                    "GameServer disconnected and unregistered: Id={ServerId} Name={Name}",
                    removed.Info.ServerId,
                    removed.Info.ServerName);
            }
        }
    }

    public IReadOnlyList<MsgGameServerInfo> GetServerListForClient()
    {
        if (_servers.IsEmpty)
        {
            return
            [
                new MsgGameServerInfo(
                    _options.ServerName,
                    _options.AdvertiseIp,
                    (uint)_options.GamePort,
                    _options.ServerId,
                    GameServerState.Normal)
            ];
        }

        return _servers.Values
            .OrderBy(s => s.Info.ServerId)
            .Select(s => s.Info with
            {
                Ip = string.IsNullOrWhiteSpace(_options.AdvertiseIp) ? s.Info.Ip : _options.AdvertiseIp
            })
            .ToArray();
    }

    public async Task<GameServerLookupResult> DispatchValidationAsync(
        byte requestedServerId,
        string account,
        string validateCode,
        CancellationToken cancellationToken)
    {
        RegisteredGameServer? target = null;

        if (!_servers.IsEmpty)
        {
            if (requestedServerId == 0)
            {
                target = _servers.Values.OrderBy(s => s.Info.ServerId).FirstOrDefault();
            }
            else if (!_servers.TryGetValue(requestedServerId, out target) && _servers.Count == 1)
            {
                // 1.0.282 client may send a non-matching cServerId (e.g. 51/'3') when selecting
                // the sole server from MSG_GAMESERVER_INFO; route to the only online GameServer.
                target = _servers.Values.FirstOrDefault();
            }
        }

        if (target is null)
        {
            if (!_servers.IsEmpty)
            {
                return new GameServerLookupResult(false, true, false, default);
            }

            // Standalone fallback when no GameServer has registered over TCP yet.
            var fallback = new MsgGameServerInfo(
                _options.ServerName,
                _options.AdvertiseIp,
                (uint)_options.GamePort,
                _options.ServerId,
                GameServerState.Normal);
            return new GameServerLookupResult(true, false, false, fallback);
        }

        MsgGameServerInfo clientInfo = target.Info with
        {
            Ip = string.IsNullOrWhiteSpace(_options.AdvertiseIp) ? target.Info.Ip : _options.AdvertiseIp
        };

        if (clientInfo.State is GameServerState.Full or GameServerState.Offline)
        {
            return new GameServerLookupResult(true, true, false, clientInfo);
        }

        byte[] validatePayload = new MsgValidateGameServer(account, validateCode).ToPayload();
        try
        {
            await target.Connection.SendPacketAsync(
                (ushort)LoginOpcode.MsgValidateGameServer,
                validatePayload,
                cancellationToken).ConfigureAwait(false);
            return new GameServerLookupResult(true, false, true, clientInfo);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to send MSG_VALIDATE_GAMESERVER to GameServer Id={ServerId} for Account={Account}",
                clientInfo.ServerId,
                account);
            UnregisterConnection(target.Connection);
            return new GameServerLookupResult(false, true, false, clientInfo);
        }
    }
}
