using System.Buffers;
using System.Buffers.Binary;

namespace GodsWar.Protocol;

/// <summary>
/// 4-byte little-endian <c>&lt;HH</c> packet header (Length + Opcode).
/// Length includes the 4-byte header itself (minimum valid length is 4).
/// </summary>
public readonly record struct PacketHeader(ushort Length, ushort Opcode)
{
    public const int Size = 4;
    public const int MaxPacketSize = ushort.MaxValue;

    public int PayloadLength => Length - Size;

    public static bool TryRead(ReadOnlySpan<byte> source, out PacketHeader header)
    {
        if (source.Length < Size)
        {
            header = default;
            return false;
        }

        ushort length = BinaryPrimitives.ReadUInt16LittleEndian(source[..2]);
        ushort opcode = BinaryPrimitives.ReadUInt16LittleEndian(source[2..4]);
        header = new PacketHeader(length, opcode);
        return true;
    }

    public void Write(Span<byte> destination)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(destination[..2], Length);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[2..4], Opcode);
    }
}

/// <summary>
/// Reads <c>&lt;HH</c>-framed packets from a <see cref="ReadOnlySequence{T}"/> (e.g. a <c>System.IO.Pipelines.PipeReader</c>).
/// </summary>
public static class PacketFramer
{
    /// <summary>
    /// Attempts to extract one complete packet from <paramref name="buffer"/>.
    /// Returns <see langword="false"/> if more data is needed.
    /// Throws <see cref="InvalidDataException"/> if <c>Length &lt; 4</c>.
    /// </summary>
    public static bool TryReadPacket(
        ref ReadOnlySequence<byte> buffer,
        out ushort opcode,
        out ReadOnlySequence<byte> payload)
    {
        if (buffer.Length < PacketHeader.Size)
        {
            opcode = 0;
            payload = default;
            return false;
        }

        Span<byte> headerBytes = stackalloc byte[PacketHeader.Size];
        buffer.Slice(0, PacketHeader.Size).CopyTo(headerBytes);
        PacketHeader.TryRead(headerBytes, out PacketHeader header);

        if (header.Length < PacketHeader.Size)
        {
            throw new InvalidDataException($"Invalid packet length {header.Length} (must be >= {PacketHeader.Size}) for opcode {header.Opcode}.");
        }

        if (buffer.Length < header.Length)
        {
            opcode = 0;
            payload = default;
            return false;
        }

        opcode = header.Opcode;
        payload = buffer.Slice(PacketHeader.Size, header.PayloadLength);
        buffer = buffer.Slice(header.Length);
        return true;
    }

    /// <summary>
    /// Serializes a packet (<c>&lt;HH</c> header + payload) into a new byte array,
    /// optionally encrypting it in-place with <paramref name="cipher"/>.
    /// </summary>
    public static byte[] FramePacket(ushort opcode, ReadOnlySpan<byte> payload, XorCipher? cipher = null)
    {
        int totalLength = PacketHeader.Size + payload.Length;
        if (totalLength > PacketHeader.MaxPacketSize)
        {
            throw new ArgumentOutOfRangeException(nameof(payload), $"Packet size {totalLength} exceeds {PacketHeader.MaxPacketSize}.");
        }

        byte[] packet = new byte[totalLength];
        new PacketHeader((ushort)totalLength, opcode).Write(packet);
        payload.CopyTo(packet.AsSpan(PacketHeader.Size));
        cipher?.Encrypt(packet);
        return packet;
    }
}
