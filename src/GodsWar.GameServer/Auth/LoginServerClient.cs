using System.Net.Sockets;
using GodsWar.Protocol;
using GodsWar.Protocol.Packets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GodsWar.GameServer.Auth;

/// <summary>
/// Maintains the inter-server TCP link to <c>GodsWar.LoginServer</c> (port 5999),
/// registers this GameServer via <c>MSG_GAMESERVER_INFO</c> (10008),
/// and receives <c>MSG_VALIDATE_GAMESERVER</c> (300) session tokens.
/// </summary>
public sealed class LoginServerClient : BackgroundService
{
    private readonly GameServerOptions _options;
    private readonly ValidationTokenStore _tokenStore;
    private readonly ILogger<LoginServerClient> _logger;

    public LoginServerClient(
        IOptions<GameServerOptions> options,
        ValidationTokenStore tokenStore,
        ILogger<LoginServerClient> logger)
    {
        _options = options.Value;
        _tokenStore = tokenStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunConnectionLoopAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "LoginServer link ({Host}:{Port}) disconnected or failed: {Message}. Retrying in 3s...",
                    _options.LoginServerHost,
                    _options.LoginServerPort,
                    ex.Message);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunConnectionLoopAsync(CancellationToken stoppingToken)
    {
        using var tcp = new TcpClient { NoDelay = true };
        await tcp.ConnectAsync(_options.LoginServerHost, _options.LoginServerPort, stoppingToken).ConfigureAwait(false);

        await using NetworkStream stream = tcp.GetStream();
        await using var connection = new PacketConnection(stream, encrypted: true);

        var info = new MsgGameServerInfo(
            _options.ServerName,
            _options.AdvertiseIp,
            (uint)_options.Port,
            _options.ServerId,
            GameServerState.Normal);

        await connection.SendPacketAsync(
            (ushort)LoginOpcode.MsgGameServerInfo,
            info.ToServerRegistrationPayload(),
            stoppingToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Registered with LoginServer at {Host}:{Port} as ServerId={ServerId} ({Name} -> {Ip}:{GamePort})",
            _options.LoginServerHost,
            _options.LoginServerPort,
            _options.ServerId,
            _options.ServerName,
            _options.AdvertiseIp,
            _options.Port);

        while (!stoppingToken.IsCancellationRequested)
        {
            (ushort Opcode, byte[] Payload)? packet = await connection.ReadPacketAsync(stoppingToken).ConfigureAwait(false);
            if (packet is null)
            {
                break;
            }

            (ushort opcode, byte[] payload) = packet.Value;
            if (opcode == (ushort)LoginOpcode.MsgValidateGameServer)
            {
                MsgValidateGameServer msg = MsgValidateGameServer.Parse(payload);
                _tokenStore.Store(msg.Account, msg.Code);
                _logger.LogDebug("Stored validation token for Account={Account}", msg.Account);
            }
            else
            {
                _logger.LogDebug("Received unhandled LoginServer opcode {Opcode} ({Length}B)", opcode, payload.Length);
            }
        }
    }
}
