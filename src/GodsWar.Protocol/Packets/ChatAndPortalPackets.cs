using System.Buffers.Binary;
using System.Text;

namespace GodsWar.Protocol.Packets;

public enum ChatChannel : byte
{
    Whisper = 0,
    Team = 1,
    Guild = 2,
    Camp = 3,
    Local = 4,
    World = 5,
    System = 6,
    Gm = 7,
}

/// <summary>
/// C2S / S2C <c>MSG_TALK</c> (opcode 10033).
/// Payload: <c>uiUserID(4) + ucTextSize(4) + ucType(1) + ucResNameSize(1) + ucAimNameSize(1) + ucBubble(1) + wcContent(UTF-16LE)</c>.
/// </summary>
public readonly record struct MsgTalk(
    uint UserId,
    ChatChannel Channel,
    byte ResPlayerNameSize,
    byte AimPlayerNameSize,
    byte ChatBubbleStyle,
    string Content,
    string SenderName = "",
    string AimName = "")
{
    public const int HeaderPayloadSize = 12;
    public const int MaxContentBytes = 512;

    public static MsgTalk Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < HeaderPayloadSize)
        {
            throw new InvalidDataException($"MSG_TALK requires at least {HeaderPayloadSize} payload bytes, got {payload.Length}.");
        }

        uint userId = BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(0, 4));
        int textSize = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(4, 4));
        var channel = (ChatChannel)payload[8];
        byte resSize = payload[9];
        byte aimSize = payload[10];
        byte bubble = payload[11];

        ReadOnlySpan<byte> raw = payload.Slice(HeaderPayloadSize);
        int maxLen = Math.Min(raw.Length, textSize > 0 ? textSize : raw.Length);
        if (maxLen > 0)
        {
            raw = raw[..maxLen];
        }

        // UTF-16LE content; strip trailing null code units
        int byteLen = raw.Length & ~1;
        string fullText = byteLen > 0 ? Encoding.Unicode.GetString(raw[..byteLen]).TrimEnd('\0') : string.Empty;

        int senderChars = resSize / 2;
        int aimChars = aimSize / 2;
        string sender = senderChars > 0 && fullText.Length >= senderChars ? fullText[..senderChars].TrimEnd('\0') : string.Empty;
        string aim = aimChars > 0 && fullText.Length >= senderChars + aimChars
            ? fullText.Substring(senderChars, aimChars).TrimEnd('\0')
            : string.Empty;
        string body = fullText.Length >= senderChars + aimChars
            ? fullText[(senderChars + aimChars)..].TrimEnd('\0')
            : fullText;

        return new MsgTalk(userId, channel, resSize, aimSize, bubble, body, sender, aim);
    }

    public byte[] ToPayload()
    {
        string combined = string.Concat(SenderName, AimName, Content);
        byte[] textBytes = Encoding.Unicode.GetBytes(combined);
        int copyLen = Math.Min(textBytes.Length, MaxContentBytes - 2);
        byte resBytes = (byte)Math.Min(byte.MaxValue, Encoding.Unicode.GetByteCount(SenderName));
        byte aimBytes = (byte)Math.Min(byte.MaxValue, Encoding.Unicode.GetByteCount(AimName));

        byte[] buf = new byte[HeaderPayloadSize + MaxContentBytes];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), UserId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4, 4), copyLen + 2);
        buf[8] = (byte)Channel;
        buf[9] = resBytes;
        buf[10] = aimBytes;
        buf[11] = ChatBubbleStyle;
        textBytes.AsSpan(0, copyLen).CopyTo(buf.AsSpan(HeaderPayloadSize));
        return buf;
    }
}

/// <summary>
/// C2S <c>MSG_TALKCHANNEL</c> (opcode 10034, 5B framed / 1B payload).
/// </summary>
public readonly record struct MsgTalkChannel(byte Channel)
{
    public const int PayloadSize = 1;

    public static MsgTalkChannel Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadSize)
        {
            throw new InvalidDataException($"MSG_TALKCHANNEL requires {PayloadSize} payload byte, got {payload.Length}.");
        }

        return new MsgTalkChannel(payload[0]);
    }

    public byte[] ToPayload() => [Channel];
}

/// <summary>
/// S2C <c>MSG_SCENE_CHANGE</c> (opcode 10018, 24B framed / 20B payload).
/// </summary>
public readonly record struct MsgSceneChange(
    uint UserId,
    float X,
    float Y,
    float Z,
    ushort CurrentMapId,
    ushort NextMapId)
{
    public const int PayloadSize = 20;

    public static MsgSceneChange Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadSize)
        {
            throw new InvalidDataException($"MSG_SCENE_CHANGE requires {PayloadSize} payload bytes, got {payload.Length}.");
        }

        return new MsgSceneChange(
            BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(0, 4)),
            BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(4, 4)),
            BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(8, 4)),
            BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(12, 4)),
            BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(16, 2)),
            BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(18, 2)));
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), UserId);
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(4, 4), X);
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(8, 4), Y);
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(12, 4), Z);
        BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(16, 2), CurrentMapId);
        BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(18, 2), NextMapId);
        return buf;
    }
}
