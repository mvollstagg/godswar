using System.Buffers.Binary;
using System.Text;

namespace GodsWar.Protocol;

/// <summary>
/// Zero-allocation little-endian packet payload reader over <see cref="ReadOnlySpan{T}"/>.
/// </summary>
public ref struct PacketReader
{
    private readonly ReadOnlySpan<byte> _buffer;
    private int _offset;

    public PacketReader(ReadOnlySpan<byte> buffer)
    {
        _buffer = buffer;
        _offset = 0;
    }

    public readonly int Position => _offset;
    public readonly int Remaining => _buffer.Length - _offset;

    public byte ReadByte()
    {
        EnsureAvailable(1);
        return _buffer[_offset++];
    }

    public sbyte ReadSByte() => (sbyte)ReadByte();

    public short ReadInt16()
    {
        EnsureAvailable(2);
        short value = BinaryPrimitives.ReadInt16LittleEndian(_buffer.Slice(_offset, 2));
        _offset += 2;
        return value;
    }

    public ushort ReadUInt16()
    {
        EnsureAvailable(2);
        ushort value = BinaryPrimitives.ReadUInt16LittleEndian(_buffer.Slice(_offset, 2));
        _offset += 2;
        return value;
    }

    public int ReadInt32()
    {
        EnsureAvailable(4);
        int value = BinaryPrimitives.ReadInt32LittleEndian(_buffer.Slice(_offset, 4));
        _offset += 4;
        return value;
    }

    public uint ReadUInt32()
    {
        EnsureAvailable(4);
        uint value = BinaryPrimitives.ReadUInt32LittleEndian(_buffer.Slice(_offset, 4));
        _offset += 4;
        return value;
    }

    public float ReadSingle()
    {
        EnsureAvailable(4);
        float value = BinaryPrimitives.ReadSingleLittleEndian(_buffer.Slice(_offset, 4));
        _offset += 4;
        return value;
    }

    public ReadOnlySpan<byte> ReadBytes(int count)
    {
        EnsureAvailable(count);
        ReadOnlySpan<byte> slice = _buffer.Slice(_offset, count);
        _offset += count;
        return slice;
    }

    /// <summary>
    /// Reads a fixed-width null-terminated string using Latin-1 (matching <c>login_shim.py</c>).
    /// </summary>
    public string ReadFixedString(int fieldLength)
    {
        ReadOnlySpan<byte> raw = ReadBytes(fieldLength);
        int nul = raw.IndexOf((byte)0);
        if (nul >= 0)
        {
            raw = raw[..nul];
        }

        return Encoding.Latin1.GetString(raw);
    }

    public void Skip(int count)
    {
        EnsureAvailable(count);
        _offset += count;
    }

    private readonly void EnsureAvailable(int count)
    {
        if (count < 0 || _offset + count > _buffer.Length)
        {
            throw new InvalidDataException($"Packet underflow: need {count} bytes at offset {_offset}, remaining {Remaining}.");
        }
    }
}

/// <summary>
/// Little-endian packet builder that writes a <c>&lt;HH</c> header and payload.
/// </summary>
public sealed class PacketWriter
{
    private readonly byte[] _buffer;
    private int _offset;

    public PacketWriter(ushort opcode, int capacity = 256)
    {
        if (capacity < PacketHeader.Size || capacity > PacketHeader.MaxPacketSize)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        _buffer = new byte[capacity];
        _offset = PacketHeader.Size;
        Opcode = opcode;
    }

    public ushort Opcode { get; }
    public int Length => _offset;

    public void WriteByte(byte value)
    {
        EnsureCapacity(1);
        _buffer[_offset++] = value;
    }

    public void WriteInt16(short value)
    {
        EnsureCapacity(2);
        BinaryPrimitives.WriteInt16LittleEndian(_buffer.AsSpan(_offset, 2), value);
        _offset += 2;
    }

    public void WriteUInt16(ushort value)
    {
        EnsureCapacity(2);
        BinaryPrimitives.WriteUInt16LittleEndian(_buffer.AsSpan(_offset, 2), value);
        _offset += 2;
    }

    public void WriteInt32(int value)
    {
        EnsureCapacity(4);
        BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(_offset, 4), value);
        _offset += 4;
    }

    public void WriteUInt32(uint value)
    {
        EnsureCapacity(4);
        BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan(_offset, 4), value);
        _offset += 4;
    }

    public void WriteSingle(float value)
    {
        EnsureCapacity(4);
        BinaryPrimitives.WriteSingleLittleEndian(_buffer.AsSpan(_offset, 4), value);
        _offset += 4;
    }

    public void WriteZeros(int count)
    {
        EnsureCapacity(count);
        _buffer.AsSpan(_offset, count).Clear();
        _offset += count;
    }

    /// <summary>
    /// Writes a fixed-width Latin-1 string, truncating to <c>fieldLength - 1</c> and null-padding.
    /// </summary>
    public void WriteFixedString(string? value, int fieldLength)
    {
        EnsureCapacity(fieldLength);
        Span<byte> dest = _buffer.AsSpan(_offset, fieldLength);
        dest.Clear();

        if (!string.IsNullOrEmpty(value) && fieldLength > 1)
        {
            int maxChars = Math.Min(value.Length, fieldLength - 1);
            Encoding.Latin1.GetBytes(value.AsSpan(0, maxChars), dest);
        }

        _offset += fieldLength;
    }

    public void WriteBytes(ReadOnlySpan<byte> data)
    {
        EnsureCapacity(data.Length);
        data.CopyTo(_buffer.AsSpan(_offset, data.Length));
        _offset += data.Length;
    }

    /// <summary>
    /// Finalizes the <c>&lt;HH</c> header and returns the complete packet bytes,
    /// optionally encrypting them in-place with <paramref name="cipher"/>.
    /// </summary>
    public byte[] ToArray(XorCipher? cipher = null)
    {
        new PacketHeader((ushort)_offset, Opcode).Write(_buffer.AsSpan(0, PacketHeader.Size));
        byte[] result = _buffer.AsSpan(0, _offset).ToArray();
        cipher?.Encrypt(result);
        return result;
    }

    private void EnsureCapacity(int additional)
    {
        if (_offset + additional > _buffer.Length)
        {
            throw new InvalidOperationException($"PacketWriter overflow: {_offset + additional} > {_buffer.Length}.");
        }
    }
}
