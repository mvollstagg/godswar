using System.Buffers.Binary;
using System.Text;

namespace GodsWar.Protocol.Packets;

/// <summary>
/// Result codes for <see cref="MsgLoginReturnInfo"/> (<c>ucInfo</c>).
/// Matches <c>GW 2.0/Server Files (Source)/LoginServer/Src/Session.cpp</c> and <c>LoginLogic.cpp</c>.
/// </summary>
public enum LoginResultCode : byte
{
    InvalidCredentials = 0x00,
    Success = 0x01,
    AccountBanned = 0x02,
    IpBanned = 0x03,
    VersionMismatch = 0xFF,
}

/// <summary>
/// Server load state for <see cref="MsgGameServerInfo"/>.
/// </summary>
public enum GameServerState : byte
{
    Offline = 0,
    Normal = 1,
    Busy = 2,
    Full = 3,
}

public static class ProtocolConstants
{
    public const int MaxNameLength = 32;
    public const int ValidateCodeLength = 25;
}

/// <summary>
/// C2S <c>MSG_LOGIN</c> (opcode 1).
/// Supports both legacy 72-byte packets (<c>fVersion</c> at payload offset 64)
/// and 1.0.282 US client 140-byte packets (<c>fVersion</c> in the last 4 bytes).
/// </summary>
public readonly record struct MsgLogin(string Account, string Password, int Version)
{
    public const int MinPayloadLength = 68; // 72 - 4
    public const int UsClientPayloadLength = 136; // 140 - 4

    public static MsgLogin Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < MinPayloadLength)
        {
            throw new InvalidDataException($"MSG_LOGIN payload too short: {payload.Length} bytes (expected >= {MinPayloadLength}).");
        }

        var reader = new PacketReader(payload);
        string account = reader.ReadFixedString(ProtocolConstants.MaxNameLength);
        string password = reader.ReadFixedString(ProtocolConstants.MaxNameLength);

        int version = payload.Length >= UsClientPayloadLength
            ? BinaryPrimitives.ReadInt32LittleEndian(payload[^4..])
            : reader.ReadInt32();

        return new MsgLogin(account, password, version);
    }

    public byte[] ToPayload(bool usClientFormat = true)
    {
        int length = usClientFormat ? UsClientPayloadLength : MinPayloadLength;
        byte[] payload = new byte[length];
        WriteFixedAscii(Account, payload.AsSpan(0, ProtocolConstants.MaxNameLength));
        WriteFixedAscii(Password, payload.AsSpan(ProtocolConstants.MaxNameLength, ProtocolConstants.MaxNameLength));
        int versionOffset = usClientFormat ? length - 4 : ProtocolConstants.MaxNameLength * 2;
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(versionOffset, 4), Version);
        return payload;
    }

    internal static void WriteFixedAscii(string? value, Span<byte> destination)
    {
        destination.Clear();
        if (!string.IsNullOrEmpty(value) && destination.Length > 1)
        {
            int maxChars = Math.Min(value.Length, destination.Length - 1);
            Encoding.Latin1.GetBytes(value.AsSpan(0, maxChars), destination);
        }
    }
}

/// <summary>
/// S2C <c>MSG_LOGIN_RETURN_INFO</c> (opcode 3 for 1.0.282 US client, opcode 2 for legacy &lt; 1000200).
/// Wire size: 6 bytes (4-byte header + 2-byte payload).
/// </summary>
public readonly record struct MsgLoginReturnInfo(LoginResultCode Info)
{
    public const int PayloadLength = 2;

    public static ushort GetOpcode(int clientVersion) =>
        clientVersion > 0 && clientVersion < 1_000_200
            ? (ushort)LoginOpcode.MsgLoginReturnInfoLegacy
            : (ushort)LoginOpcode.MsgLoginReturnInfo;

    public byte[] ToPayload() => [(byte)Info, 0];

    public static MsgLoginReturnInfo Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 1)
        {
            throw new InvalidDataException($"MSG_LOGIN_RETURN_INFO payload too short: {payload.Length} bytes.");
        }

        return new MsgLoginReturnInfo((LoginResultCode)payload[0]);
    }
}

/// <summary>
/// <c>MSG_GAMESERVER_INFO</c> (opcode 10008).
/// Note that the 2007 GameServer-&gt;LoginServer registration layout (76B) differs from
/// the 1.0.282 LoginServer-&gt;Client server-list layout (78B).
/// </summary>
public readonly record struct MsgGameServerInfo(
    string ServerName,
    string Ip,
    uint Port,
    byte ServerId,
    GameServerState State)
{
    public const int ClientPayloadLength = 74; // 78 - 4
    public const int ServerRegistrationMinPayloadLength = 72; // 76 - 4

    /// <summary>
    /// Serializes the 74-byte payload (78 bytes framed) expected by the 1.0.282 US client.
    /// Ground truth: <c>docker/tools/login_shim.py:server_list_packet</c>.
    /// </summary>
    public byte[] ToClientPayload()
    {
        byte[] payload = new byte[ClientPayloadLength];
        MsgLogin.WriteFixedAscii(ServerName, payload.AsSpan(0, ProtocolConstants.MaxNameLength));
        MsgLogin.WriteFixedAscii(Ip, payload.AsSpan(32, ProtocolConstants.MaxNameLength));
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(64, 4), Port);
        // payload[68..72] is 4 zero bytes (wire 72..76)
        payload[72] = ServerId;
        payload[73] = (byte)State;
        return payload;
    }

    /// <summary>
    /// Parses a 1.0.282 client-bound server-list payload (74 bytes).
    /// </summary>
    public static MsgGameServerInfo ParseClientPayload(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < ClientPayloadLength)
        {
            throw new InvalidDataException($"Client MSG_GAMESERVER_INFO payload too short: {payload.Length} bytes.");
        }

        var reader = new PacketReader(payload);
        string serverName = reader.ReadFixedString(ProtocolConstants.MaxNameLength);
        string ip = reader.ReadFixedString(ProtocolConstants.MaxNameLength);
        uint port = reader.ReadUInt32();
        reader.Skip(4);
        byte serverId = reader.ReadByte();
        var state = (GameServerState)reader.ReadByte();
        return new MsgGameServerInfo(serverName, ip, port, serverId, state);
    }

    /// <summary>
    /// Parses a GameServer-&gt;LoginServer registration payload (&gt;= 72 bytes, matching <c>Common/MsgDefine.h</c>).
    /// </summary>
    public static MsgGameServerInfo ParseServerRegistration(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < ServerRegistrationMinPayloadLength)
        {
            throw new InvalidDataException($"Server registration MSG_GAMESERVER_INFO payload too short: {payload.Length} bytes.");
        }

        var reader = new PacketReader(payload);
        string ip = reader.ReadFixedString(ProtocolConstants.MaxNameLength);
        uint port = reader.ReadUInt32();
        byte serverId = reader.ReadByte();
        string serverName = reader.ReadFixedString(ProtocolConstants.MaxNameLength);
        var state = (GameServerState)reader.ReadByte();
        return new MsgGameServerInfo(serverName, ip, port, serverId, state);
    }

    /// <summary>
    /// Serializes a GameServer-&gt;LoginServer registration payload (72 bytes, 76 bytes framed).
    /// </summary>
    public byte[] ToServerRegistrationPayload()
    {
        byte[] payload = new byte[ServerRegistrationMinPayloadLength];
        MsgLogin.WriteFixedAscii(Ip, payload.AsSpan(0, ProtocolConstants.MaxNameLength));
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(32, 4), Port);
        payload[36] = ServerId;
        MsgLogin.WriteFixedAscii(ServerName, payload.AsSpan(37, ProtocolConstants.MaxNameLength));
        payload[69] = (byte)State;
        return payload;
    }
}

/// <summary>
/// C2S <c>MSG_REQUEST_GAMESERVER</c> (opcode 4 on 1.0.282 US client, opcode 3 on legacy / <c>login_probe.py</c>).
/// </summary>
public readonly record struct MsgRequestGameServer(string Account, byte ServerId)
{
    public const int MinPayloadLength = 32;

    public static MsgRequestGameServer Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < MinPayloadLength)
        {
            throw new InvalidDataException($"MSG_REQUEST_GAMESERVER payload too short: {payload.Length} bytes.");
        }

        var reader = new PacketReader(payload);
        string account = reader.ReadFixedString(ProtocolConstants.MaxNameLength);
        byte serverId = payload.Length > ProtocolConstants.MaxNameLength ? reader.ReadByte() : (byte)0;
        return new MsgRequestGameServer(account, serverId);
    }

    public byte[] ToPayload()
    {
        byte[] payload = new byte[ProtocolConstants.MaxNameLength + 1];
        MsgLogin.WriteFixedAscii(Account, payload.AsSpan(0, ProtocolConstants.MaxNameLength));
        payload[ProtocolConstants.MaxNameLength] = ServerId;
        return payload;
    }
}

/// <summary>
/// LoginServer-&gt;GameServer <c>MSG_VALIDATE_GAMESERVER</c> (opcode 300).
/// Wire size: 62 bytes (4-byte header + 58-byte payload).
/// </summary>
public readonly record struct MsgValidateGameServer(string Account, string Code)
{
    public const int PayloadLength = 58; // 32 + 25 + 1 pad

    public byte[] ToPayload()
    {
        byte[] payload = new byte[PayloadLength];
        MsgLogin.WriteFixedAscii(Account, payload.AsSpan(0, ProtocolConstants.MaxNameLength));
        MsgLogin.WriteFixedAscii(Code, payload.AsSpan(ProtocolConstants.MaxNameLength, ProtocolConstants.ValidateCodeLength));
        return payload;
    }

    public static MsgValidateGameServer Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < ProtocolConstants.MaxNameLength + ProtocolConstants.ValidateCodeLength)
        {
            throw new InvalidDataException($"MSG_VALIDATE_GAMESERVER payload too short: {payload.Length} bytes.");
        }

        var reader = new PacketReader(payload);
        string account = reader.ReadFixedString(ProtocolConstants.MaxNameLength);
        string code = reader.ReadFixedString(ProtocolConstants.ValidateCodeLength);
        return new MsgValidateGameServer(account, code);
    }
}

/// <summary>
/// S2C <c>MSG_RESPONSE_GAMESERVER</c> (opcode 10001).
/// Wire size: 72 bytes (4-byte header + 68-byte payload).
/// Ground truth: <c>docker/tools/login_shim.py:response_packet</c>.
/// </summary>
public readonly record struct MsgResponseGameServer(
    byte ServerId,
    string Ip,
    uint Port,
    byte LoginError,
    string Code)
{
    public const int PayloadLength = 68; // 72 - 4

    public byte[] ToPayload()
    {
        byte[] payload = new byte[PayloadLength];
        payload[0] = ServerId;
        MsgLogin.WriteFixedAscii(Ip, payload.AsSpan(1, ProtocolConstants.MaxNameLength));
        // payload[33..36] = 3 bytes alignment padding
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(36, 4), Port);
        payload[40] = LoginError;
        MsgLogin.WriteFixedAscii(Code, payload.AsSpan(41, ProtocolConstants.ValidateCodeLength));
        // payload[66..68] = 2 bytes trailing struct padding
        return payload;
    }

    public static MsgResponseGameServer Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 66)
        {
            throw new InvalidDataException($"MSG_RESPONSE_GAMESERVER payload too short: {payload.Length} bytes.");
        }

        byte serverId = payload[0];
        var ipReader = new PacketReader(payload.Slice(1, ProtocolConstants.MaxNameLength));
        string ip = ipReader.ReadFixedString(ProtocolConstants.MaxNameLength);
        uint port = BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(36, 4));
        byte loginError = payload[40];
        var codeReader = new PacketReader(payload.Slice(41, ProtocolConstants.ValidateCodeLength));
        string code = codeReader.ReadFixedString(ProtocolConstants.ValidateCodeLength);
        return new MsgResponseGameServer(serverId, ip, port, loginError, code);
    }
}
