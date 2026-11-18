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

            // Peek first 4 bytes to auto-detect plaintext 1.0.282 client (8C 00 01 00 / 48 00 01 00)
            // vs XOR-encrypted client/GameServer IPC.
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

            await using var prefixedStream = new PrefixedReadStream(prefix, stream);
            await using var connection = new PacketConnection(prefixedStream, encrypted: !isPlaintextLogin);
            await _sessionHandler.HandleConnectionAsync(connection, remoteIp, stoppingToken, caesarDecodeLogin: isPlaintextLogin).ConfigureAwait(false);
        }
    }

    private sealed class PrefixedReadStream(byte[] prefix, Stream inner) : Stream
    {
        private int _pos;
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => inner.Flush();
        public override Task FlushAsync(CancellationToken ct) => inner.FlushAsync(ct);
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_pos < prefix.Length)
            {
                int n = Math.Min(count, prefix.Length - _pos);
                prefix.AsSpan(_pos, n).CopyTo(buffer.AsSpan(offset, n));
                _pos += n;
                return n;
            }
            return inner.Read(buffer, offset, count);
        }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            if (_pos < prefix.Length)
            {
                int n = Math.Min(buffer.Length, prefix.Length - _pos);
                prefix.AsSpan(_pos, n).CopyTo(buffer.Span[..n]);
                _pos += n;
                return n;
            }
            return await inner.ReadAsync(buffer, ct).ConfigureAwait(false);
        }
        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default) => inner.WriteAsync(buffer, ct);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
