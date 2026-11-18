using System.Buffers;
using System.IO.Pipelines;

namespace GodsWar.Protocol;

/// <summary>
/// Full-duplex <c>&lt;HH</c>-framed packet connection over a <see cref="Stream"/>
/// using <see cref="System.IO.Pipelines"/> and <see cref="XorCipher"/>.
/// </summary>
public sealed class PacketConnection : IAsyncDisposable
{
    private readonly Stream _stream;
    private readonly XorCipher? _cipher;
    private readonly Pipe _pipe = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly CancellationTokenSource _disposeCts = new();
    private readonly Task _fillTask;
    private bool _disposed;

    public PacketConnection(Stream stream, bool encrypted = true)
        : this(stream, encrypted ? new XorCipher() : null)
    {
    }

    public PacketConnection(Stream stream, XorCipher? cipher)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _cipher = cipher;
        _fillTask = FillPipeAsync(_disposeCts.Token);
    }

    public XorCipher? Cipher => _cipher;

    /// <summary>
    /// Reads the next complete packet from the stream, or returns <see langword="null"/> on EOF.
    /// </summary>
    public async ValueTask<(ushort Opcode, byte[] Payload)?> ReadPacketAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            ReadResult result = await _pipe.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            ReadOnlySequence<byte> buffer = result.Buffer;

            if (PacketFramer.TryReadPacket(ref buffer, out ushort opcode, out ReadOnlySequence<byte> payloadSeq))
            {
                byte[] payload = payloadSeq.ToArray();
                _pipe.Reader.AdvanceTo(buffer.Start);
                return (opcode, payload);
            }

            _pipe.Reader.AdvanceTo(buffer.Start, buffer.End);

            if (result.IsCompleted)
            {
                if (buffer.Length > 0)
                {
                    byte[] preview = buffer.Slice(0, Math.Min(16, buffer.Length)).ToArray();
                    throw new EndOfStreamException($"Stream closed with {buffer.Length} trailing incomplete packet bytes (head={Convert.ToHexString(preview)}).");
                }

                return null;
            }
        }
    }

    /// <summary>
    /// Frames, encrypts (if enabled), and writes a packet atomically.
    /// </summary>
    public async ValueTask SendPacketAsync(ushort opcode, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            byte[] framed = PacketFramer.FramePacket(opcode, payload.Span, _cipher);
            await _stream.WriteAsync(framed, cancellationToken).ConfigureAwait(false);
            await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task FillPipeAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                Memory<byte> memory = _pipe.Writer.GetMemory(4096);
                int bytesRead = await _stream.ReadAsync(memory, cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    break;
                }

                // Decrypt each incoming chunk once as it enters the pipe so PacketFramer
                // can inspect headers without ever double-advancing the keystream cursor.
                _cipher?.Decrypt(memory.Span[..bytesRead]);
                _pipe.Writer.Advance(bytesRead);

                FlushResult flush = await _pipe.Writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                if (flush.IsCompleted)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            await _pipe.Writer.CompleteAsync(ex).ConfigureAwait(false);
            return;
        }

        await _pipe.Writer.CompleteAsync().ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _disposeCts.Cancel();
        await _pipe.Reader.CompleteAsync().ConfigureAwait(false);

        try
        {
            await _fillTask.ConfigureAwait(false);
        }
        catch
        {
            // Ignore background read errors during disposal
        }

        _sendLock.Dispose();
        _disposeCts.Dispose();
    }
}

/// <summary>
/// Wraps a <see cref="Stream"/> with a small already-read prefix buffer for protocol auto-detection.
/// </summary>
public sealed class PrefixedStream(byte[] prefix, Stream inner) : Stream
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
