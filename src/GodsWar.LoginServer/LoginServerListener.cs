using System.Net;
using System.Net.Sockets;
using GodsWar.Protocol;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GodsWar.LoginServer;

public sealed class LoginServerListener : BackgroundService
{
    private readonly LoginServerOptions _options;
    private readonly LoginSessionHandler _sessionHandler;
    private readonly ILogger<LoginServerListener> _logger;
    private TcpListener? _listener;

    public LoginServerListener(
        IOptions<LoginServerOptions> options,
        LoginSessionHandler sessionHandler,
        ILogger<LoginServerListener> logger)
    {
        _options = options.Value;
        _sessionHandler = sessionHandler;
        _logger = logger;
    }

    public int BoundPort => _listener is { LocalEndpoint: IPEndPoint ep } ? ep.Port : _options.Port;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IPAddress bindIp = IPAddress.TryParse(_options.BindAddress, out IPAddress? parsed)
            ? parsed
            : IPAddress.Any;

        _listener = new TcpListener(bindIp, _options.Port);
        _listener.Start();

        _logger.LogInformation(
            "GodsWar.LoginServer listening on {BindIp}:{Port} (Advertise={AdvertiseIp}:{GamePort}, ServerId={ServerId}, Name={ServerName})",
            bindIp,
            BoundPort,
            _options.AdvertiseIp,
            _options.GamePort,
            _options.ServerId,
            _options.ServerName);

        var activeTasks = new List<Task>();
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                Task clientTask = RunClientAsync(client, stoppingToken);
                activeTasks.RemoveAll(static t => t.IsCompleted);
                activeTasks.Add(clientTask);
            }
        }
        finally
        {
            _listener.Stop();
            await Task.WhenAll(activeTasks).ConfigureAwait(false);
        }
    }

    private async Task RunClientAsync(TcpClient client, CancellationToken stoppingToken)
    {
        string? remoteIp = (client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString();
        _logger.LogDebug("Accepted connection from {RemoteEndpoint}", client.Client.RemoteEndPoint);

        using (client)
        {
            client.NoDelay = true;
            await using NetworkStream stream = client.GetStream();

            // Peek first 4 bytes to auto-detect:
            // - plaintext 1.0.282 client (8C 00 01 00 / 48 00 01 00)
            // - v2.27 encrypted client (AC 9A BD 26 / 68 9A BD 26)
            // - 1.0.282 XOR-encrypted client/GameServer IPC
            byte[] prefix = new byte[4];
            int read = 0;
            while (read < 4)
            {
                int n = await stream.ReadAsync(prefix.AsMemory(read, 4 - read), stoppingToken).ConfigureAwait(false);
                if (n == 0)
                {
                    return;
                }
                read += n;
            }

            bool isPlaintextLogin = (prefix[0] == 0x8C || prefix[0] == 0x48)
                && prefix[1] == 0x00
                && prefix[2] == 0x01
                && prefix[3] == 0x00;

            bool isV227Login = (prefix[0] == 0xAC || prefix[0] == 0x68)
                && prefix[1] == 0x9A
                && prefix[2] == 0xBD
                && prefix[3] == 0x26;

            XorCipher? cipher = isPlaintextLogin
                ? null
                : isV227Login
                    ? XorCipher.CreateV227()
                    : new XorCipher();

            await using var prefixedStream = new PrefixedStream(prefix, stream);
            await using var connection = new PacketConnection(prefixedStream, cipher);
            await _sessionHandler.HandleConnectionAsync(
                connection,
                remoteIp,
                stoppingToken,
                caesarDecodeLogin: isPlaintextLogin || isV227Login,
                isV227: isV227Login).ConfigureAwait(false);
        }
    }
}
