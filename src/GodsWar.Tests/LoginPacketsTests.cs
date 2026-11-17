using System.Buffers.Binary;
using GodsWar.Protocol;
using GodsWar.Protocol.Packets;

namespace GodsWar.Tests;

public sealed class LoginPacketsTests
{
    [Fact]
    public void MsgLogin_ParsesLegacy72ByteAndUs140ByteFormats()
    {
        byte[] legacyPayload = new MsgLogin("test", "secret", 1000270).ToPayload(usClientFormat: false);
        Assert.Equal(68, legacyPayload.Length);
        MsgLogin parsedLegacy = MsgLogin.Parse(legacyPayload);
        Assert.Equal("test", parsedLegacy.Account);
        Assert.Equal("secret", parsedLegacy.Password);
        Assert.Equal(1000270, parsedLegacy.Version);

        byte[] usPayload = new MsgLogin("test2", "123456", 1000282).ToPayload(usClientFormat: true);
        Assert.Equal(136, usPayload.Length);
        MsgLogin parsedUs = MsgLogin.Parse(usPayload);
        Assert.Equal("test2", parsedUs.Account);
        Assert.Equal("123456", parsedUs.Password);
        Assert.Equal(1000282, parsedUs.Version);
    }

    [Fact]
    public void MsgLoginReturnInfo_UsesOpcode3ForUsClientAnd6ByteWireSize()
    {
        Assert.Equal((ushort)3, MsgLoginReturnInfo.GetOpcode(1000270));
        Assert.Equal((ushort)3, MsgLoginReturnInfo.GetOpcode(0));
        Assert.Equal((ushort)2, MsgLoginReturnInfo.GetOpcode(1000145));

        byte[] framed = PacketFramer.FramePacket(
            MsgLoginReturnInfo.GetOpcode(1000270),
            new MsgLoginReturnInfo(LoginResultCode.Success).ToPayload());

        Assert.Equal(6, framed.Length);
        Assert.Equal((ushort)6, BinaryPrimitives.ReadUInt16LittleEndian(framed.AsSpan(0, 2)));
        Assert.Equal((ushort)3, BinaryPrimitives.ReadUInt16LittleEndian(framed.AsSpan(2, 2)));
        Assert.Equal((byte)LoginResultCode.Success, framed[4]);
    }

    [Fact]
    public void MsgGameServerInfo_ClientWireLayoutIsExact78Bytes()
    {
        var info = new MsgGameServerInfo("GodsWar", "127.0.0.1", 6001, 1, GameServerState.Normal);
        byte[] framed = PacketFramer.FramePacket((ushort)LoginOpcode.MsgGameServerInfo, info.ToClientPayload());

        Assert.Equal(78, framed.Length);
        Assert.Equal((ushort)78, BinaryPrimitives.ReadUInt16LittleEndian(framed.AsSpan(0, 2)));
        Assert.Equal((ushort)10008, BinaryPrimitives.ReadUInt16LittleEndian(framed.AsSpan(2, 2)));
        Assert.Equal((uint)6001, BinaryPrimitives.ReadUInt32LittleEndian(framed.AsSpan(68, 4)));
        Assert.Equal((byte)1, framed[76]);
        Assert.Equal((byte)1, framed[77]);

        MsgGameServerInfo roundTrip = MsgGameServerInfo.ParseClientPayload(framed.AsSpan(4));
        Assert.Equal(info, roundTrip);
    }

    [Fact]
    public void MsgGameServerInfo_ParsesServerRegistrationLayout()
    {
        var info = new MsgGameServerInfo("GodsWar", "127.0.0.1", 6001, 2, GameServerState.Busy);
        byte[] regPayload = info.ToServerRegistrationPayload();
        Assert.Equal(72, regPayload.Length);

        MsgGameServerInfo parsed = MsgGameServerInfo.ParseServerRegistration(regPayload);
        Assert.Equal(info, parsed);
    }

    [Fact]
    public void MsgValidateGameServer_WireLayoutIsExact62Bytes()
    {
        var validate = new MsgValidateGameServer("test", "ABCDEFGHIJKLMNOPQRSTUVWX");
        byte[] framed = PacketFramer.FramePacket((ushort)LoginOpcode.MsgValidateGameServer, validate.ToPayload());

        Assert.Equal(62, framed.Length);
        Assert.Equal((ushort)62, BinaryPrimitives.ReadUInt16LittleEndian(framed.AsSpan(0, 2)));
        Assert.Equal((ushort)300, BinaryPrimitives.ReadUInt16LittleEndian(framed.AsSpan(2, 2)));

        MsgValidateGameServer parsed = MsgValidateGameServer.Parse(framed.AsSpan(4));
        Assert.Equal(validate, parsed);
    }

    [Fact]
    public void MsgResponseGameServer_WireLayoutIsExact72Bytes()
    {
        var resp = new MsgResponseGameServer(1, "127.0.0.1", 6001, 0, "ABCDEFGHIJKLMNOPQRSTUVWX");
        byte[] framed = PacketFramer.FramePacket((ushort)LoginOpcode.MsgResponseGameServer, resp.ToPayload());

        Assert.Equal(72, framed.Length);
        Assert.Equal((ushort)72, BinaryPrimitives.ReadUInt16LittleEndian(framed.AsSpan(0, 2)));
        Assert.Equal((ushort)10001, BinaryPrimitives.ReadUInt16LittleEndian(framed.AsSpan(2, 2)));
        Assert.Equal((byte)1, framed[4]);
        Assert.Equal((uint)6001, BinaryPrimitives.ReadUInt32LittleEndian(framed.AsSpan(40, 4)));
        Assert.Equal((byte)0, framed[44]);

        MsgResponseGameServer parsed = MsgResponseGameServer.Parse(framed.AsSpan(4));
        Assert.Equal(resp, parsed);
    }
}
