using System.Buffers.Binary;

namespace GodsWar.Protocol;

/// <summary>
/// 256-byte quadratic-congruential XOR stream cipher matching C++
/// <c>cnet::TEncryptClient&lt;0xEE, 0x17, 0x33, 0x50, 0x82, 0x23, 0x61, 0x33&gt;</c>
/// in <c>Common/Include/cNet/Encryptor.h</c> and <c>docker/tools/login_shim.py</c>.
/// </summary>
public sealed class XorCipher
{
    public const int TableSize = 256;

    // Coefficients from Common/Include/cNet/Encryptor.h (1.0.282)
    private const byte A1 = 0xEE, B1 = 0x17, C1 = 0x33, First1 = 0x50;
    private const byte A2 = 0x82, B2 = 0x23, C2 = 0x61, First2 = 0x33;

    // Coefficients from v2.26.002 (v2.27) Net.dll TEncryptClient<0x20, 0xFD, 0x07, 0x1F, 0x7A, 0xCF, 0xE5, 0x3F>
    private const byte V227A1 = 0x20, V227B1 = 0xFD, V227C1 = 0x07, V227First1 = 0x1F;
    private const byte V227A2 = 0x7A, V227B2 = 0xCF, V227C2 = 0xE5, V227First2 = 0x3F;

    private readonly byte[] _table1 = new byte[TableSize];
    private readonly byte[] _table2 = new byte[TableSize];
    private readonly byte[] _keystream = new byte[TableSize];

    private int _encryptPos;
    private int _decryptPos;

    public XorCipher()
        : this(A1, B1, C1, First1, A2, B2, C2, First2)
    {
    }

    public XorCipher(byte a1, byte b1, byte c1, byte first1, byte a2, byte b2, byte c2, byte first2)
    {
        InitTables(a1, b1, c1, first1, a2, b2, c2, first2);
    }

    /// <summary>
    /// Creates an <see cref="XorCipher"/> configured with the v2.26.002 (v2.27) <c>Net.dll</c>
    /// <c>TEncryptClient</c> coefficients.
    /// </summary>
    public static XorCipher CreateV227() =>
        new(V227A1, V227B1, V227C1, V227First1, V227A2, V227B2, V227C2, V227First2);

    /// <summary>Current byte offset in the encryption keystream (0..255).</summary>
    public int EncryptPosition => _encryptPos;

    /// <summary>Current byte offset in the decryption keystream (0..255).</summary>
    public int DecryptPosition => _decryptPos;

    /// <summary>Combined 256-byte XOR keystream (<c>table1[i] ^ table2[i]</c>).</summary>
    public ReadOnlySpan<byte> Keystream => _keystream;

    private void InitTables(byte a1, byte b1, byte c1, byte first1, byte a2, byte b2, byte c2, byte first2)
    {
        byte n1 = first1;
        byte n2 = first2;
        for (int i = 0; i < TableSize; i++)
        {
            _table1[i] = n1;
            _table2[i] = n2;
            _keystream[i] = (byte)(n1 ^ n2);

            n1 = (byte)((a1 * n1 * n1 + b1 * n1 + c1) & 0xFF);
            n2 = (byte)((a2 * n2 * n2 + b2 * n2 + c2) & 0xFF);
        }

        _encryptPos = 0;
        _decryptPos = 0;
    }

    /// <summary>Encrypts <paramref name="buffer"/> in-place and advances <see cref="EncryptPosition"/>.</summary>
    public void Encrypt(Span<byte> buffer)
    {
        int pos = _encryptPos;
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] ^= _keystream[pos];
            pos = (pos + 1) & 0xFF;
        }
        _encryptPos = pos;
    }

    /// <summary>Decrypts <paramref name="buffer"/> in-place and advances <see cref="DecryptPosition"/>.</summary>
    public void Decrypt(Span<byte> buffer)
    {
        int pos = _decryptPos;
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] ^= _keystream[pos];
            pos = (pos + 1) & 0xFF;
        }
        _decryptPos = pos;
    }

    /// <summary>
    /// Mutates both tables using a 32-bit code (mirrors <c>TEncryptClient::ChangeCode(DWORD)</c>).
    /// </summary>
    public void ChangeCode(uint code)
    {
        uint code2 = unchecked(code * code);
        for (int i = 0; i < TableSize; i += 4)
        {
            uint v1 = BinaryPrimitives.ReadUInt32LittleEndian(_table1.AsSpan(i, 4)) ^ code;
            uint v2 = BinaryPrimitives.ReadUInt32LittleEndian(_table2.AsSpan(i, 4)) ^ code2;
            BinaryPrimitives.WriteUInt32LittleEndian(_table1.AsSpan(i, 4), v1);
            BinaryPrimitives.WriteUInt32LittleEndian(_table2.AsSpan(i, 4), v2);
        }

        for (int i = 0; i < TableSize; i++)
        {
            _keystream[i] = (byte)(_table1[i] ^ _table2[i]);
        }
    }

    /// <summary>
    /// Mutates both tables using an ASCII numeric key (mirrors <c>TEncryptClient::ChangeCode(const char*)</c>).
    /// </summary>
    public void ChangeCode(string? key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        uint code = uint.TryParse(key, out uint parsed) ? parsed : 0;
        ChangeCode(code);
    }

    /// <summary>
    /// Statelessly XORs <paramref name="source"/> into <paramref name="destination"/>
    /// starting at <paramref name="startPosition"/> using the default keystream.
    /// </summary>
    public static void TransformAt(ReadOnlySpan<byte> source, Span<byte> destination, int startPosition, ReadOnlySpan<byte> keystream)
    {
        if (destination.Length < source.Length)
        {
            throw new ArgumentException("Destination span is smaller than source span.", nameof(destination));
        }

        int pos = startPosition & 0xFF;
        for (int i = 0; i < source.Length; i++)
        {
            destination[i] = (byte)(source[i] ^ keystream[pos]);
            pos = (pos + 1) & 0xFF;
        }
    }
}
