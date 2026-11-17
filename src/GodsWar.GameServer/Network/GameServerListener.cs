using System.Net;
using System.Net.Sockets;
using GodsWar.Protocol;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GodsWar.GameServer.Network;

public sealed class GameServerListener : BackgroundService
{
    private readonly GameServerOptions _options;
    private readonly GameSessionHandler _sessionHandler;
    private readonly ILogger<GameServerListener> _logger;
    private TcpListener? _listener;

    public GameServerListener(
        IOptions<GameServerOptions> options,
        GameSessionHandler sessionHandler,
        ILogger<GameServerListener> logger)
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
            "GodsWar.GameServer listening on {BindIp}:{Port} (plaintext 1.0.282 protocol)",
            bindIp,
            BoundPort);

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
        _logger.LogDebug("Accepted GameServer connection from {RemoteEndpoint}", client.Client.RemoteEndPoint);

        using (client)
        {
            client.NoDelay = true;
            await using NetworkStream stream = client.GetStream();
            // 1.0.282 US client <-> GameServer link is plaintext (no XOR cipher).
            await using var connection = new PacketConnection(stream, encrypted: false);
            await _sessionHandler.HandleConnectionAsync(connection, remoteIp, stoppingToken).ConfigureAwait(false);
        }
    }
}
