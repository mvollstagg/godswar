using System.Buffers.Binary;
using GodsWar.Protocol;

namespace GodsWar.Tests;

public sealed class XorCipherTests
{
    private static byte[] ComputeReferenceKeystream()
    {
        byte[] t1 = new byte[256];
        byte[] t2 = new byte[256];
        byte n1 = 0x50;
        byte n2 = 0x33;

        for (int i = 0; i < 256; i++)
        {
            t1[i] = n1;
            t2[i] = n2;
            n1 = (byte)((0xEE * n1 * n1 + 0x17 * n1 + 0x33) % 256);
            n2 = (byte)((0x82 * n2 * n2 + 0x23 * n2 + 0x61) % 256);
        }

        byte[] ks = new byte[256];
        for (int i = 0; i < 256; i++)
        {
            ks[i] = (byte)(t1[i] ^ t2[i]);
        }

        return ks;
    }

    [Fact]
    public void Keystream_MatchesPythonAndCppReference()
    {
        var cipher = new XorCipher();
        byte[] expected = ComputeReferenceKeystream();

        Assert.Equal(expected, cipher.Keystream.ToArray());
        Assert.Equal(0x63, cipher.Keystream[0]); // 0x50 ^ 0x33
    }

    [Fact]
    public void KnownPacketVector_ClientServerReady_EncryptsAndDecrypts()
    {
        // PKT_CLIENT_SERVER_READY = struct.pack("<HH", 4, 10005) = [0x04, 0x00, 0x15, 0x27]
        byte[] plain = [0x04, 0x00, 0x15, 0x27];
        byte[] expectedCipher = new byte[4];
        byte[] ks = ComputeReferenceKeystream();
        for (int i = 0; i < 4; i++)
        {
            expectedCipher[i] = (byte)(plain[i] ^ ks[i]);
        }

        var server = new XorCipher();
        var client = new XorCipher();

        byte[] buf = [.. plain];
        server.Encrypt(buf);
        Assert.Equal(expectedCipher, buf);
        Assert.Equal(4, server.EncryptPosition);
        Assert.Equal(0, server.DecryptPosition);

        client.Decrypt(buf);
        Assert.Equal(plain, buf);
        Assert.Equal(4, client.DecryptPosition);
        Assert.Equal(0, client.EncryptPosition);
    }

    [Fact]
    public void StreamCipher_WrapsAround256ByteBoundary()
    {
        var sender = new XorCipher();
        var receiver = new XorCipher();

        byte[] original = new byte[700];
        for (int i = 0; i < original.Length; i++)
        {
            original[i] = (byte)(i * 31 + 7);
        }

        byte[] wire = [.. original];

        // Encrypt in irregular chunks
        sender.Encrypt(wire.AsSpan(0, 100));
        sender.Encrypt(wire.AsSpan(100, 256));
        sender.Encrypt(wire.AsSpan(356, 344));

        Assert.Equal(700 % 256, sender.EncryptPosition);
        Assert.NotEqual(original, wire);

        // Decrypt in different irregular chunks
        receiver.Decrypt(wire.AsSpan(0, 17));
        receiver.Decrypt(wire.AsSpan(17, 500));
        receiver.Decrypt(wire.AsSpan(517, 183));

        Assert.Equal(original, wire);
        Assert.Equal(700 % 256, receiver.DecryptPosition);
    }

    [Fact]
    public void ChangeCode_MutatesKeystreamSymmetrically()
    {
        var a = new XorCipher();
        var b = new XorCipher();
        var baseline = new XorCipher();

        a.ChangeCode("12345678");
        b.ChangeCode(12345678u);

        Assert.Equal(a.Keystream.ToArray(), b.Keystream.ToArray());
        Assert.NotEqual(baseline.Keystream.ToArray(), a.Keystream.ToArray());

        byte[] data = "GodsWar-ChangeCode-Test"u8.ToArray();
        byte[] copy = [.. data];

        a.Encrypt(copy);
        b.Decrypt(copy);
        Assert.Equal(data, copy);
    }

    [Fact]
    public void CreateV227_MatchesNetDllHeaderKeystreamAndRoundTrips()
    {
        XorCipher client = XorCipher.CreateV227();
        XorCipher server = XorCipher.CreateV227();

        byte[] header = [0x8C, 0x00, 0x01, 0x00]; // 140-byte MSG_LOGIN
        byte[] wire = [.. header];
        client.Encrypt(wire);

        Assert.Equal([0xAC, 0x9A, 0xBD, 0x26], wire);

        server.Decrypt(wire);
        Assert.Equal(header, wire);
    }
}
