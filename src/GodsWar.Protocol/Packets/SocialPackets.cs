using System.Buffers.Binary;

namespace GodsWar.Protocol.Packets;

// ============================================================================
// Relations (10094..10097)
// ============================================================================

/// <summary>
/// C2S / S2C <c>MSG_RELATION_REQUEST</c> (opcode 10094, 72B framed / 68B payload).
/// </summary>
public readonly record struct MsgRelationRequest(string RequestName, string ResponseName, byte Type, byte Error)
{
    public const int PayloadSize = 66;

    public static MsgRelationRequest Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 65)
        {
            throw new InvalidDataException($"MSG_RELATION_REQUEST requires at least 65 payload bytes, got {payload.Length}.");
        }

        return new MsgRelationRequest(
            FixedStringCodec.ReadFixedString(payload.Slice(0, 32)),
            FixedStringCodec.ReadFixedString(payload.Slice(32, 32)),
            payload[64],
            payload.Length >= 66 ? payload[65] : (byte)0);
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        FixedStringCodec.WriteFixedString(buf.AsSpan(0, 32), RequestName);
        FixedStringCodec.WriteFixedString(buf.AsSpan(32, 32), ResponseName);
        buf[64] = Type;
        buf[65] = Error;
        return buf;
    }
}

/// <summary>
/// C2S / S2C <c>MSG_RELATION_RESPONSE</c> (opcode 10095, 38B framed / 34B payload).
/// </summary>
public readonly record struct MsgRelationResponse(string RequestName, bool IsAgree)
{
    public const int PayloadSize = 33;

    public static MsgRelationResponse Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadSize)
        {
            throw new InvalidDataException($"MSG_RELATION_RESPONSE requires {PayloadSize} payload bytes, got {payload.Length}.");
        }

        return new MsgRelationResponse(
            FixedStringCodec.ReadFixedString(payload.Slice(0, 32)),
            payload[32] != 0);
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        FixedStringCodec.WriteFixedString(buf.AsSpan(0, 32), RequestName);
        buf[32] = IsAgree ? (byte)1 : (byte)0;
        return buf;
    }
}

/// <summary>
/// C2S / S2C <c>MSG_RELATION_DELETE</c> (opcode 10096, 37B framed / 33B payload).
/// </summary>
public readonly record struct MsgRelationDelete(string RequestName, byte Style)
{
    public const int PayloadSize = 33;

    public static MsgRelationDelete Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadSize)
        {
            throw new InvalidDataException($"MSG_RELATION_DELETE requires {PayloadSize} payload bytes, got {payload.Length}.");
        }

        return new MsgRelationDelete(
            FixedStringCodec.ReadFixedString(payload.Slice(0, 32)),
            payload[32]);
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        FixedStringCodec.WriteFixedString(buf.AsSpan(0, 32), RequestName);
        buf[32] = Style;
        return buf;
    }
}

// ============================================================================
// Trade (10098..10102)
// ============================================================================

/// <summary>
/// C2S / S2C <c>MSG_TRADE</c> (opcode 10098, 148B framed / 144B payload).
/// State: 0=Request, 1=Open/Start, 2=Lock, 3=Confirm/Complete, 4=Cancel.
/// </summary>
public readonly record struct MsgTrade(
    uint UiId,
    uint ObjectId,
    int State,
    int TraderState,
    string Name,
    string ObjectName)
{
    public const int PayloadSize = 144;

    public static MsgTrade Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadSize)
        {
            throw new InvalidDataException($"MSG_TRADE requires {PayloadSize} payload bytes, got {payload.Length}.");
        }

        return new MsgTrade(
            BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(0, 4)),
            BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(4, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(8, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(12, 4)),
            FixedStringCodec.ReadFixedString(payload.Slice(16, 64)),
            FixedStringCodec.ReadFixedString(payload.Slice(80, 64)));
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), UiId);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(4, 4), ObjectId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(8, 4), State);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(12, 4), TraderState);
        FixedStringCodec.WriteFixedString(buf.AsSpan(16, 64), Name);
        FixedStringCodec.WriteFixedString(buf.AsSpan(80, 64), ObjectName);
        return buf;
    }
}

/// <summary>
/// C2S / S2C <c>MSG_TRADE_MONEY</c> (opcode 10099, 80B framed / 76B payload).
/// </summary>
public readonly record struct MsgTradeMoney(uint UiId, int Money, int Bijou, string Name)
{
    public const int PayloadSize = 76;

    public static MsgTradeMoney Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadSize)
        {
            throw new InvalidDataException($"MSG_TRADE_MONEY requires {PayloadSize} payload bytes, got {payload.Length}.");
        }

        return new MsgTradeMoney(
            BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(0, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(4, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(8, 4)),
            FixedStringCodec.ReadFixedString(payload.Slice(12, 64)));
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), UiId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4, 4), Money);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(8, 4), Bijou);
        FixedStringCodec.WriteFixedString(buf.AsSpan(12, 64), Name);
        return buf;
    }
}

/// <summary>
/// S2C <c>MSG_TRADE_ITEM</c> (opcode 10100, 120B framed / 116B payload in 1.0.282).
/// Ground truth: <c>docker/tools/gw_protocol.py:_s2c_trade_item</c>.
/// </summary>
public readonly record struct MsgTradeItem(uint UiId, int Index, int BaseId, string Name, ItemEquipSlot Item)
{
    public const int PayloadSize = 116;

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), UiId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4, 4), Index);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(8, 4), BaseId);
        FixedStringCodec.WriteFixedString(buf.AsSpan(12, 32), Name);
        Item.WriteTo(buf.AsSpan(44, ItemEquipSlot.WireSize));
        return buf;
    }
}

/// <summary>
/// C2S <c>MSG_TRADE_ADDITEM</c> (opcode 10101, 84B framed / 80B payload).
/// </summary>
public readonly record struct MsgTradeAddItem(uint UiId, int Index, int Bag, int Grid, string Name)
{
    public const int PayloadSize = 80;

    public static MsgTradeAddItem Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadSize)
        {
            throw new InvalidDataException($"MSG_TRADE_ADDITEM requires {PayloadSize} payload bytes, got {payload.Length}.");
        }

        return new MsgTradeAddItem(
            BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(0, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(4, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(8, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(12, 4)),
            FixedStringCodec.ReadFixedString(payload.Slice(16, 64)));
    }
}

/// <summary>
/// C2S / S2C <c>MSG_TRADE_REMOVEITEM</c> (opcode 10102, 76B framed / 72B payload).
/// </summary>
public readonly record struct MsgTradeRemoveItem(uint UiId, int Index, string Name)
{
    public const int PayloadSize = 72;

    public static MsgTradeRemoveItem Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadSize)
        {
            throw new InvalidDataException($"MSG_TRADE_REMOVEITEM requires {PayloadSize} payload bytes, got {payload.Length}.");
        }

        return new MsgTradeRemoveItem(
            BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(0, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(4, 4)),
            FixedStringCodec.ReadFixedString(payload.Slice(8, 64)));
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), UiId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4, 4), Index);
        FixedStringCodec.WriteFixedString(buf.AsSpan(8, 64), Name);
        return buf;
    }
}

// ============================================================================
// Party / Team (10117..10128)
// ============================================================================

/// <summary>
/// C2S / S2C <c>MSG_TEAM</c> (opcode 10117, 136B framed / 132B payload).
/// Used for 10117 (Invite), 10120 (Accept), 10121 (Delete/Kick), 10122 (ReplaceLeader), 10123 (Dissolve), 10124 (Leave), 10126 (Reject).
/// </summary>
public readonly record struct MsgTeam(uint ResPlayerId, string ResPlayerName, string AimPlayerName)
{
    public const int PayloadSize = 132;

    public static MsgTeam Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadSize)
        {
            throw new InvalidDataException($"MSG_TEAM requires {PayloadSize} payload bytes, got {payload.Length}.");
        }

        return new MsgTeam(
            BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(0, 4)),
            FixedStringCodec.ReadFixedString(payload.Slice(4, 64)),
            FixedStringCodec.ReadFixedString(payload.Slice(68, 64)));
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), ResPlayerId);
        FixedStringCodec.WriteFixedString(buf.AsSpan(4, 64), ResPlayerName);
        FixedStringCodec.WriteFixedString(buf.AsSpan(68, 64), AimPlayerName);
        return buf;
    }
}

/// <summary>
/// C2S / S2C <c>MSG_TEAMINFO</c> (opcode 10119, 72B framed / 68B payload in 1.0.282).
/// Ground truth: <c>AUDIT.md</c> Session 5 &amp; <c>gw_protocol.py:_c2s_teaminfo</c>.
/// </summary>
public readonly record struct MsgTeamInfo(byte Mode, string AimName, bool IsAimInTeam, bool IsSelfInTeam)
{
    public const int PayloadSize = 68;

    public static MsgTeamInfo Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 67)
        {
            throw new InvalidDataException($"MSG_TEAMINFO requires at least 67 payload bytes, got {payload.Length}.");
        }

        return new MsgTeamInfo(
            payload[0],
            FixedStringCodec.ReadFixedString(payload.Slice(1, 64)),
            payload[65] != 0,
            payload[66] != 0);
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        buf[0] = Mode;
        FixedStringCodec.WriteFixedString(buf.AsSpan(1, 64), AimName);
        buf[65] = IsAimInTeam ? (byte)1 : (byte)0;
        buf[66] = IsSelfInTeam ? (byte)1 : (byte)0;
        return buf;
    }
}

/// <summary>
/// S2C <c>MSG_TEAM_TIP</c> (opcode 10125, 388B framed / 384B payload).
/// </summary>
public readonly record struct MsgTeamTip(string Name, string Tip)
{
    public const int PayloadSize = 384;

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        FixedStringCodec.WriteUnicodeFixedString(buf.AsSpan(0, 128), Name);
        FixedStringCodec.WriteUnicodeFixedString(buf.AsSpan(128, 256), Tip);
        return buf;
    }
}

public readonly record struct TeamMemberWire(
    int PlayerId,
    uint Hp,
    uint MaxHp,
    uint Level,
    byte Class,
    string Name,
    short MapId,
    float X,
    float Y,
    float Z);

/// <summary>
/// S2C <c>MSG_TEAM_REFLASH</c> (opcode 10127, 488B framed / 484B payload).
/// </summary>
public readonly record struct MsgTeamRefresh(
    TeamMemberWire Leader,
    IReadOnlyList<TeamMemberWire> Teammates,
    bool IsDataUpdate)
{
    public const int PayloadSize = 484; // 5 * 96 + 4

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        // Leader slot: id(4), hp(4), maxHp(4), lv(4), class(1)@16, name[64]@17, pad(1)@81, map(2)@82, x(4)@84, y(4)@88, z(4)@92 -> 96B
        WriteSlot(buf.AsSpan(0, 96), Leader, classBeforeName: true);

        for (int i = 0; i < 4; i++)
        {
            TeamMemberWire m = i < Teammates.Count
                ? Teammates[i]
                : new TeamMemberWire(0, 0, 0, 0, 0, string.Empty, 0, 0f, 0f, 0f);
            // Teammate slot: id(4), hp(4), maxHp(4), lv(4), name[64]@16, class(1)@80, pad(1)@81, map(2)@82, x(4)@84, y(4)@88, z(4)@92 -> 96B
            WriteSlot(buf.AsSpan(96 + i * 96, 96), m, classBeforeName: false);
        }

        buf[480] = IsDataUpdate ? (byte)1 : (byte)0;
        return buf;
    }

    private static void WriteSlot(Span<byte> dest, TeamMemberWire m, bool classBeforeName)
    {
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(0, 4), m.PlayerId);
        BinaryPrimitives.WriteUInt32LittleEndian(dest.Slice(4, 4), m.Hp);
        BinaryPrimitives.WriteUInt32LittleEndian(dest.Slice(8, 4), m.MaxHp);
        BinaryPrimitives.WriteUInt32LittleEndian(dest.Slice(12, 4), m.Level);
        if (classBeforeName)
        {
            dest[16] = m.Class;
            FixedStringCodec.WriteFixedString(dest.Slice(17, 64), m.Name);
        }
        else
        {
            FixedStringCodec.WriteFixedString(dest.Slice(16, 64), m.Name);
            dest[80] = m.Class;
        }

        BinaryPrimitives.WriteInt16LittleEndian(dest.Slice(82, 2), m.MapId);
        BinaryPrimitives.WriteSingleLittleEndian(dest.Slice(84, 4), m.X);
        BinaryPrimitives.WriteSingleLittleEndian(dest.Slice(88, 4), m.Y);
        BinaryPrimitives.WriteSingleLittleEndian(dest.Slice(92, 4), m.Z);
    }
}

// ============================================================================
// Guild / Consortia (10131..10146)
// ============================================================================

/// <summary>
/// S2C <c>MSG_CONSORTIA_CREATE_RESPONSE</c> (opcode 10131).
/// Style: 0=Success, 1=LevelLow, 2=MoneyLow, 3=AlreadyInGuild, 4=NameExists/Error.
/// </summary>
public readonly record struct MsgConsortiaCreateResponse(
    uint PlayerId,
    string GuildName,
    string ChairmanName,
    byte Level,
    byte Style)
{
    public const int PayloadSize = 106;

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), PlayerId);
        FixedStringCodec.WriteFixedString(buf.AsSpan(4, 64), GuildName);
        FixedStringCodec.WriteFixedString(buf.AsSpan(68, 32), ChairmanName);
        buf[100] = Level;
        buf[101] = Style;
        return buf;
    }
}

/// <summary>
/// S2C <c>MSG_CONSORTIA_BASE_INFO</c> (opcode 10132).
/// </summary>
public readonly record struct MsgConsortiaBaseInfo(
    string Name,
    byte Level,
    int Found,
    int Bijou,
    int Credit,
    string Placard,
    int MaxMember)
{
    public const int PayloadSize = 340;

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        FixedStringCodec.WriteFixedString(buf.AsSpan(0, 64), Name);
        buf[64] = Level;
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(68, 4), Found);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(72, 4), Bijou);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(76, 4), Credit);
        FixedStringCodec.WriteFixedString(buf.AsSpan(80, 256), Placard);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(336, 4), MaxMember);
        return buf;
    }
}

public readonly record struct ConsortiaMemberWire(
    string Name,
    sbyte MapId,
    byte Duty,
    byte Level,
    byte Business,
    uint Contribute)
{
    public const int WireSize = 40;

    public void WriteTo(Span<byte> dest)
    {
        dest[..WireSize].Clear();
        FixedStringCodec.WriteFixedString(dest.Slice(0, 32), Name);
        dest[32] = (byte)MapId;
        dest[33] = Duty;
        dest[34] = Level;
        dest[35] = Business;
        BinaryPrimitives.WriteUInt32LittleEndian(dest.Slice(36, 4), Contribute);
    }
}

/// <summary>
/// S2C <c>MSG_CONSORTIA_MEMBER_LIST</c> (opcode 10137).
/// </summary>
public readonly record struct MsgConsortiaMemberList(
    uint ReceivePlayerId,
    IReadOnlyList<ConsortiaMemberWire> Members,
    bool IsAll)
{
    public const int MaxMembers = 24;
    public const int PayloadSize = 8 + MaxMembers * ConsortiaMemberWire.WireSize + 4;

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), ReceivePlayerId);
        int count = Math.Min(MaxMembers, Members.Count);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(4, 4), (uint)count);
        for (int i = 0; i < count; i++)
        {
            Members[i].WriteTo(buf.AsSpan(8 + i * ConsortiaMemberWire.WireSize, ConsortiaMemberWire.WireSize));
        }

        buf[8 + MaxMembers * ConsortiaMemberWire.WireSize] = IsAll ? (byte)1 : (byte)0;
        return buf;
    }
}

/// <summary>
/// C2S / S2C <c>MSG_CONSORTIA_INVITE</c> (opcode 10139).
/// </summary>
public readonly record struct MsgConsortiaInvite(uint InvitePlayerId, string InvitedName, string GuildName)
{
    public const int PayloadSize = 100;

    public static MsgConsortiaInvite Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadSize)
        {
            throw new InvalidDataException($"MSG_CONSORTIA_INVITE requires {PayloadSize} payload bytes, got {payload.Length}.");
        }

        return new MsgConsortiaInvite(
            BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(0, 4)),
            FixedStringCodec.ReadFixedString(payload.Slice(4, 32)),
            FixedStringCodec.ReadFixedString(payload.Slice(36, 64)));
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), InvitePlayerId);
        FixedStringCodec.WriteFixedString(buf.AsSpan(4, 32), InvitedName);
        FixedStringCodec.WriteFixedString(buf.AsSpan(36, 64), GuildName);
        return buf;
    }
}

/// <summary>
/// C2S / S2C <c>MSG_CONSORTIA_RESPONSE</c> (opcode 10140).
/// </summary>
public readonly record struct MsgConsortiaResponse(byte Style, uint SendPlayerId, string ReceiveName)
{
    public const int PayloadSize = 40;

    public static MsgConsortiaResponse Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 36)
        {
            throw new InvalidDataException($"MSG_CONSORTIA_RESPONSE requires at least 36 payload bytes, got {payload.Length}.");
        }

        return new MsgConsortiaResponse(
            payload[0],
            BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(4, 4)),
            FixedStringCodec.ReadFixedString(payload.Slice(8, 32)));
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        buf[0] = Style;
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(4, 4), SendPlayerId);
        FixedStringCodec.WriteFixedString(buf.AsSpan(8, 32), ReceiveName);
        return buf;
    }
}

/// <summary>
/// C2S / S2C <c>MSG_CONSORTIA_EXIT</c> (opcode 10143).
/// </summary>
public readonly record struct MsgConsortiaExit(string Name, string NewDutyName, byte Duty)
{
    public const int PayloadSize = 68;

    public static MsgConsortiaExit Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 65)
        {
            throw new InvalidDataException($"MSG_CONSORTIA_EXIT requires at least 65 payload bytes, got {payload.Length}.");
        }

        return new MsgConsortiaExit(
            FixedStringCodec.ReadFixedString(payload.Slice(0, 32)),
            FixedStringCodec.ReadFixedString(payload.Slice(32, 32)),
            payload[64]);
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        FixedStringCodec.WriteFixedString(buf.AsSpan(0, 32), Name);
        FixedStringCodec.WriteFixedString(buf.AsSpan(32, 32), NewDutyName);
        buf[64] = Duty;
        return buf;
    }
}

/// <summary>
/// C2S / S2C <c>MSG_CONSORTIA_TEXT</c> (opcode 10144).
/// </summary>
public readonly record struct MsgConsortiaText(uint PlayerId, string Placard)
{
    public const int PayloadSize = 132;

    public static MsgConsortiaText Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadSize)
        {
            throw new InvalidDataException($"MSG_CONSORTIA_TEXT requires {PayloadSize} payload bytes, got {payload.Length}.");
        }

        return new MsgConsortiaText(
            BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(0, 4)),
            FixedStringCodec.ReadFixedString(payload.Slice(4, 128)));
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), PlayerId);
        FixedStringCodec.WriteFixedString(buf.AsSpan(4, 128), Placard);
        return buf;
    }
}

/// <summary>
/// C2S / S2C <c>MSG_CONSORTIA_DUTY</c> (opcode 10145).
/// </summary>
public readonly record struct MsgConsortiaDuty(string OldName, byte OldDuty, string NewName, byte NewDuty)
{
    public const int PayloadSize = 68;

    public static MsgConsortiaDuty Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 66)
        {
            throw new InvalidDataException($"MSG_CONSORTIA_DUTY requires at least 66 payload bytes, got {payload.Length}.");
        }

        return new MsgConsortiaDuty(
            FixedStringCodec.ReadFixedString(payload.Slice(0, 32)),
            payload[32],
            FixedStringCodec.ReadFixedString(payload.Slice(33, 32)),
            payload[65]);
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        FixedStringCodec.WriteFixedString(buf.AsSpan(0, 32), OldName);
        buf[32] = OldDuty;
        FixedStringCodec.WriteFixedString(buf.AsSpan(33, 32), NewName);
        buf[65] = NewDuty;
        return buf;
    }
}

/// <summary>
/// C2S / S2C <c>MSG_CONSORTIA_MEMBER_DEL</c> (opcode 10146).
/// </summary>
public readonly record struct MsgConsortiaMemberDel(uint PlayerId, string Name)
{
    public const int PayloadSize = 36;

    public static MsgConsortiaMemberDel Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadSize)
        {
            throw new InvalidDataException($"MSG_CONSORTIA_MEMBER_DEL requires {PayloadSize} payload bytes, got {payload.Length}.");
        }

        return new MsgConsortiaMemberDel(
            BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(0, 4)),
            FixedStringCodec.ReadFixedString(payload.Slice(4, 32)));
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), PlayerId);
        FixedStringCodec.WriteFixedString(buf.AsSpan(4, 32), Name);
        return buf;
    }
}
