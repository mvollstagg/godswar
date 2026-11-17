using System.Buffers;
using System.IO.Pipelines;
using GodsWar.Protocol;

namespace GodsWar.Tests;

public sealed class PacketFramingTests
{
    [Fact]
    public void TryReadPacket_HandlesPartialHeaderAndPayload()
    {
        byte[] pkt = PacketFramer.FramePacket((ushort)GameOpcode.MsgClientServerReady, ReadOnlySpan<byte>.Empty);
        Assert.Equal([0x04, 0x00, 0x15, 0x27], pkt);

        // 2 bytes -> incomplete header
        var partial = new ReadOnlySequence<byte>(pkt.AsMemory(0, 2));
        Assert.False(PacketFramer.TryReadPacket(ref partial, out _, out _));
        Assert.Equal(2, partial.Length);

        // Full 4 bytes -> complete
        var full = new ReadOnlySequence<byte>(pkt);
        Assert.True(PacketFramer.TryReadPacket(ref full, out ushort opcode, out ReadOnlySequence<byte> payload));
        Assert.Equal((ushort)GameOpcode.MsgClientServerReady, opcode);
        Assert.Equal(0, payload.Length);
        Assert.Equal(0, full.Length);
    }

    [Fact]
    public void TryReadPacket_HandlesCoalescedAndSegmentedBuffers()
    {
        var w1 = new PacketWriter((ushort)GameOpcode.MsgServerTime);
        w1.WriteUInt32(1700000000u);
        byte[] p1 = w1.ToArray();

        var w2 = new PacketWriter((ushort)GameOpcode.MsgTalk);
        w2.WriteFixedString("Umut", 32);
        byte[] p2 = w2.ToArray();

        // Build a two-segment ReadOnlySequence splitting p1 in the middle
        byte[] combined = [.. p1, .. p2];
        var seg1 = new TestSegment(combined.AsMemory(0, 5), 0);
        var seg2 = seg1.Append(combined.AsMemory(5));
        var seq = new ReadOnlySequence<byte>(seg1, 0, seg2, seg2.Memory.Length);

        Assert.True(PacketFramer.TryReadPacket(ref seq, out ushort op1, out ReadOnlySequence<byte> pay1));
        Assert.Equal((ushort)GameOpcode.MsgServerTime, op1);
        var r1 = new PacketReader(pay1.ToArray());
        Assert.Equal(1700000000u, r1.ReadUInt32());

        Assert.True(PacketFramer.TryReadPacket(ref seq, out ushort op2, out ReadOnlySequence<byte> pay2));
        Assert.Equal((ushort)GameOpcode.MsgTalk, op2);
        var r2 = new PacketReader(pay2.ToArray());
        Assert.Equal("Umut", r2.ReadFixedString(32));

        Assert.Equal(0, seq.Length);
    }

    [Fact]
    public void TryReadPacket_ThrowsOnInvalidLengthBelowHeaderSize()
    {
        byte[] invalid = [0x03, 0x00, 0x01, 0x00];
        var seq = new ReadOnlySequence<byte>(invalid);

        Assert.Throws<InvalidDataException>(() => PacketFramer.TryReadPacket(ref seq, out _, out _));
    }

    [Fact]
    public void PacketWriterAndReader_RoundtripAllTypes()
    {
        var writer = new PacketWriter((ushort)GameOpcode.MsgWalk, 64);
        writer.WriteUInt32(3797u);
        writer.WriteSingle(76.5f);
        writer.WriteSingle(-173.25f);
        writer.WriteByte(2);
        writer.WriteFixedString("Athens_Newbie", 32);
        writer.WriteInt16(-42);

        byte[] raw = writer.ToArray();
        Assert.Equal(4 + 4 + 4 + 4 + 1 + 32 + 2, raw.Length);

        var seq = new ReadOnlySequence<byte>(raw);
        Assert.True(PacketFramer.TryReadPacket(ref seq, out ushort opcode, out ReadOnlySequence<byte> payload));
        Assert.Equal((ushort)GameOpcode.MsgWalk, opcode);

        var reader = new PacketReader(payload.ToArray());
        Assert.Equal(3797u, reader.ReadUInt32());
        Assert.Equal(76.5f, reader.ReadSingle());
        Assert.Equal(-173.25f, reader.ReadSingle());
        Assert.Equal((byte)2, reader.ReadByte());
        Assert.Equal("Athens_Newbie", reader.ReadFixedString(32));
        Assert.Equal((short)-42, reader.ReadInt16());
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public async Task PacketConnection_EncryptedDuplexRoundtrip()
    {
        var clientToServer = new Pipe();
        var serverToClient = new Pipe();

        await using var clientStream = new DuplexPipeStream(serverToClient.Reader, clientToServer.Writer);
        await using var serverStream = new DuplexPipeStream(clientToServer.Reader, serverToClient.Writer);

        await using var client = new PacketConnection(clientStream, encrypted: true);
        await using var server = new PacketConnection(serverStream, encrypted: true);

        var reqWriter = new PacketWriter((ushort)LoginOpcode.MsgLogin);
        reqWriter.WriteFixedString("test", 32);
        reqWriter.WriteFixedString("test", 32);
        await client.SendPacketAsync(reqWriter.Opcode, reqWriter.ToArray().AsMemory(PacketHeader.Size));

        var received = await server.ReadPacketAsync();
        Assert.NotNull(received);
        Assert.Equal((ushort)LoginOpcode.MsgLogin, received.Value.Opcode);

        var reqReader = new PacketReader(received.Value.Payload);
        Assert.Equal("test", reqReader.ReadFixedString(32));
        Assert.Equal("test", reqReader.ReadFixedString(32));

        await server.SendPacketAsync((ushort)GameOpcode.MsgClientServerReady, ReadOnlyMemory<byte>.Empty);
        var reply = await client.ReadPacketAsync();
        Assert.NotNull(reply);
        Assert.Equal((ushort)GameOpcode.MsgClientServerReady, reply.Value.Opcode);
        Assert.Empty(reply.Value.Payload);
    }

    [Fact]
    public void Protocol10282Opcodes_MatchGroundTruth()
    {
        Assert.Equal((ushort)3, (ushort)LoginOpcode.MsgLoginReturnInfo);
        Assert.Equal((ushort)4, (ushort)LoginOpcode.MsgRequestGameServer);
        Assert.Equal((ushort)10005, (ushort)GameOpcode.MsgClientServerReady);
        Assert.Equal((ushort)10019, (ushort)GameOpcode.MsgSelfInfo);
        Assert.Equal((ushort)10020, (ushort)GameOpcode.MsgObjectInfo);
        Assert.Equal((ushort)10031, (ushort)GameOpcode.MsgKitbag);
        Assert.Equal((ushort)10041, (ushort)GameOpcode.MsgSelfProperty);
        Assert.Equal((ushort)10160, (ushort)GameOpcode.MsgSynGameData);
        Assert.Equal((ushort)10187, (ushort)GameOpcode.MsgWalk);
        Assert.Equal((ushort)10303, (ushort)GameOpcode.MsgServerTime);
    }

    private sealed class TestSegment : ReadOnlySequenceSegment<byte>
    {
        public TestSegment(ReadOnlyMemory<byte> memory, long runningIndex)
        {
            Memory = memory;
            RunningIndex = runningIndex;
        }

        public TestSegment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new TestSegment(memory, RunningIndex + Memory.Length);
            Next = next;
            return next;
        }
    }

    private sealed class DuplexPipeStream(PipeReader reader, PipeWriter writer) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush() { }
        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ReadResult result = await reader.ReadAsync(cancellationToken);
            if (result.Buffer.IsEmpty && result.IsCompleted)
            {
                return 0;
            }

            int toCopy = (int)Math.Min(buffer.Length, result.Buffer.Length);
            result.Buffer.Slice(0, toCopy).CopyTo(buffer.Span);
            reader.AdvanceTo(result.Buffer.GetPosition(toCopy));
            return toCopy;
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await writer.WriteAsync(buffer, cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
        public override void Write(byte[] buffer, int offset, int count) => WriteAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
