using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace GodsWar.Protocol.Packets;

/// <summary>
/// Entity type constants for <see cref="MsgObjectInfo.UiType"/> (<c>Common/MsgTypes.h</c>).
/// </summary>
public enum WorldObjectType : byte
{
    Player = 0x10,  // 16
    Npc = 0x11,     // 17
    Monster = 0x12, // 18
    Boss = 0x13,    // 19
}

/// <summary>
/// Error codes for <see cref="MsgManageReturn"/> (<c>GameServer/ErrorMessage.h</c>).
/// </summary>
public enum ManageReturnCode
{
    Success = 1,
    NameTooLong = 2,
    IllegalCharacters = 3,
    RoleLimitReached = 4,
    DuplicateRoleName = 5,
    GuildLeaderCannotDelete = 6,
    DeleteFailed = 7,
    DeleteSuccess = 8,
}

/// <summary>
/// 72-byte client <c>SEquip</c> item slot (<c>docker/tools/gw_protocol.py:pack_client_equip</c>).
/// </summary>
public readonly record struct ItemEquipSlot(
    int Base,
    int Append0,
    int Append1,
    int Append2,
    int Append3,
    int Append4,
    byte BaseLevel,
    byte AppendLevel,
    byte Binding,
    byte Overlap)
{
    public const int WireSize = 72;

    public static ItemEquipSlot Empty => new(-1, -1, -1, -1, -1, -1, 0, 0, 0, 0);

    public bool IsEmpty => Base <= 0;

    public void WriteTo(Span<byte> dest)
    {
        if (dest.Length < WireSize)
        {
            throw new ArgumentException($"Destination must be at least {WireSize} bytes.", nameof(dest));
        }

        dest[..WireSize].Clear();
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(0, 4), Base);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(4, 4), Append0);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(8, 4), Append1);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(12, 4), Append2);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(16, 4), Append3);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(20, 4), Append4);
        dest[24] = BaseLevel;
        dest[25] = AppendLevel;
        dest[26] = Binding;
        dest[27] = Overlap;
    }

    public static ItemEquipSlot ReadFrom(ReadOnlySpan<byte> src)
    {
        if (src.Length < WireSize)
        {
            throw new InvalidDataException($"SEquip requires {WireSize} bytes, got {src.Length}.");
        }

        return new ItemEquipSlot(
            BinaryPrimitives.ReadInt32LittleEndian(src.Slice(0, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(src.Slice(4, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(src.Slice(8, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(src.Slice(12, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(src.Slice(16, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(src.Slice(20, 4)),
            src[24],
            src[25],
            src[26],
            src[27]);
    }

    public void WriteServerItem32(Span<byte> dest)
    {
        if (dest.Length < 32)
        {
            throw new ArgumentException("Destination must be at least 32 bytes.", nameof(dest));
        }

        dest[..32].Clear();
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(0, 4), Base);
        dest[4] = BaseLevel;
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(8, 4), Append0);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(12, 4), Append1);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(16, 4), Append2);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(20, 4), Append3);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(24, 4), Append4);
        dest[28] = AppendLevel;
        dest[29] = Binding;
        dest[30] = Overlap;
    }

    public static string FormatDbKitbagString(IReadOnlyList<ItemEquipSlot> slots)
    {
        var sb = new System.Text.StringBuilder(slots.Count * 16);
        foreach (ItemEquipSlot slot in slots)
        {
            if (slot.IsEmpty)
            {
                sb.Append("[]#");
            }
            else
            {
                string a0 = slot.Append0 > 0 ? slot.Append0.ToString() : string.Empty;
                string a1 = slot.Append1 > 0 ? slot.Append1.ToString() : string.Empty;
                string a2 = slot.Append2 > 0 ? slot.Append2.ToString() : string.Empty;
                string a3 = slot.Append3 > 0 ? slot.Append3.ToString() : string.Empty;
                string a4 = slot.Append4 > 0 ? slot.Append4.ToString() : string.Empty;
                sb.Append($"[{slot.Base},{a0},{a1},{a2},{a3},{a4},{slot.Binding},{slot.Overlap},{slot.BaseLevel},{slot.AppendLevel}]#");
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Parses a legacy <c>character_kitbag</c> string (<c>[4000,,,,,,0,20,1,1,0]#[]#...</c>)
    /// into fixed-length item slots.
    /// </summary>
    public static ItemEquipSlot[] ParseDbKitbagString(string? dbString, int slotCount)
    {
        var slots = new ItemEquipSlot[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            slots[i] = Empty;
        }

        if (string.IsNullOrWhiteSpace(dbString) || string.Equals(dbString, "NULL", StringComparison.OrdinalIgnoreCase))
        {
            return slots;
        }

        string[] tokens = dbString.Split('#', StringSplitOptions.RemoveEmptyEntries);
        int count = Math.Min(tokens.Length, slotCount);
        for (int i = 0; i < count; i++)
        {
            string token = tokens[i].Trim();
            if (!token.StartsWith('[') || !token.EndsWith(']') || token.Length <= 2)
            {
                continue;
            }

            string inner = token[1..^1];
            string[] parts = inner.Split(',');
            if (parts.Length < 2 || !int.TryParse(parts[0], out int baseId) || baseId <= 0)
            {
                continue;
            }

            int[] appends = [-1, -1, -1, -1, -1];
            for (int a = 0; a < 5; a++)
            {
                int idx = a + 1;
                if (idx < parts.Length && int.TryParse(parts[idx], out int val))
                {
                    appends[a] = val;
                }
            }

            byte binding = parts.Length > 6 && int.TryParse(parts[6], out int b) ? (byte)Math.Clamp(b, 0, 255) : (byte)0;
            byte overlap = parts.Length > 7 && int.TryParse(parts[7], out int o) ? (byte)Math.Clamp(o, 0, 255) : (byte)1;
            byte baseLv = parts.Length > 8 && int.TryParse(parts[8], out int blv) ? (byte)Math.Clamp(blv, 0, 255) : (byte)1;
            byte appendLv = parts.Length > 9 && int.TryParse(parts[9], out int alv) ? (byte)Math.Clamp(alv, 0, 255) : (byte)0;

            slots[i] = new ItemEquipSlot(
                baseId,
                appends[0], appends[1], appends[2], appends[3], appends[4],
                baseLv,
                appendLv,
                binding,
                overlap);
        }

        return slots;
    }
}

/// <summary>
/// C2S <c>MSG_LOGIN_GAMESERVER</c> (opcode 10000, 62B framed / 58B payload).
/// </summary>
public readonly record struct MsgLoginGameServer(string Account, string ValidateCode)
{
    public const int PayloadLength = 58;

    public static MsgLoginGameServer Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < ProtocolConstants.MaxNameLength + ProtocolConstants.ValidateCodeLength)
        {
            throw new InvalidDataException($"MSG_LOGIN_GAMESERVER payload too short: {payload.Length} bytes.");
        }

        var reader = new PacketReader(payload);
        string account = reader.ReadFixedString(ProtocolConstants.MaxNameLength);
        string code = reader.ReadFixedString(ProtocolConstants.ValidateCodeLength);
        return new MsgLoginGameServer(account, code);
    }

    public byte[] ToPayload()
    {
        byte[] payload = new byte[PayloadLength];
        MsgLogin.WriteFixedAscii(Account, payload.AsSpan(0, ProtocolConstants.MaxNameLength));
        MsgLogin.WriteFixedAscii(ValidateCode, payload.AsSpan(ProtocolConstants.MaxNameLength, ProtocolConstants.ValidateCodeLength));
        return payload;
    }
}

public readonly record struct RoleSummary(
    string Name,
    byte Camp,
    byte Profession,
    byte Level,
    byte Gender,
    byte HairStyle,
    byte FaceShap,
    int[] EquipBaseIds);

/// <summary>
/// S2C <c>MSG_ROLE_INFO</c> (opcode 10002, 356B framed / 352B payload).
/// </summary>
public sealed class MsgRoleInfo
{
    public const int PayloadLength = 352;
    public const int V227PayloadLength = 496; // 500 - 4
    public const int V227EquipCount = 21;
    public const int MaxRoles = 4;
    public const int EquipCount = 12;

    public IReadOnlyList<RoleSummary> Roles { get; }

    public MsgRoleInfo(IReadOnlyList<RoleSummary> roles)
    {
        Roles = roles;
    }

    public byte[] ToPayload()
    {
        byte[] payload = new byte[PayloadLength];
        int count = Math.Min(Roles.Count, MaxRoles);
        payload[0] = (byte)count;

        for (int i = 0; i < count; i++)
        {
            RoleSummary role = Roles[i];
            int nameOffset = i == 0 ? 1 : 88 * i;
            MsgLogin.WriteFixedAscii(role.Name, payload.AsSpan(nameOffset, ProtocolConstants.MaxNameLength));

            int metaOffset = nameOffset + ProtocolConstants.MaxNameLength;
            payload[metaOffset + 0] = role.Camp;
            payload[metaOffset + 1] = role.Profession;
            payload[metaOffset + 2] = role.Level;
            payload[metaOffset + 3] = role.Gender;
            payload[metaOffset + 4] = role.HairStyle;
            payload[metaOffset + 5] = role.FaceShap;

            // MSVC natural alignment puts iEquip[12] at +40 relative to slot start (or +40 in slot 0 after ucNum+Name+6B+1B pad).
            int equipOffset = i == 0 ? 40 : 88 * i + 40;
            for (int e = 0; e < EquipCount; e++)
            {
                int equipId = e < role.EquipBaseIds.Length ? role.EquipBaseIds[e] : -1;
                BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(equipOffset + e * 4, 4), equipId);
            }
        }

        return payload;
    }

    /// <summary>
    /// Serializes the 496-byte payload (500 bytes framed) expected by the v2.26.002 (v2.27) client.
    /// </summary>
    public byte[] ToV227Payload()
    {
        byte[] payload = new byte[V227PayloadLength];
        int count = Math.Min(Roles.Count, MaxRoles);
        payload[0] = (byte)count;

        for (int i = 0; i < count; i++)
        {
            RoleSummary role = Roles[i];
            int slotStart = i == 0 ? 1 : 124 * i;
            MsgLogin.WriteFixedAscii(role.Name, payload.AsSpan(slotStart, ProtocolConstants.MaxNameLength));

            int metaOffset = slotStart + ProtocolConstants.MaxNameLength;
            payload[metaOffset + 0] = role.Camp;
            payload[metaOffset + 1] = role.Profession;
            payload[metaOffset + 2] = role.Level;
            payload[metaOffset + 3] = role.Gender;
            payload[metaOffset + 4] = role.HairStyle;
            payload[metaOffset + 5] = role.FaceShap;

            int equipOffset = i == 0 ? 40 : 124 * i + 40;
            for (int e = 0; e < V227EquipCount; e++)
            {
                int equipId = e < role.EquipBaseIds.Length ? role.EquipBaseIds[e] : -1;
                BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(equipOffset + e * 4, 4), equipId);
            }
        }

        return payload;
    }

    public static MsgRoleInfo Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadLength)
        {
            throw new InvalidDataException($"MSG_ROLE_INFO payload too short: {payload.Length} bytes.");
        }

        int count = Math.Min((int)payload[0], MaxRoles);
        var roles = new List<RoleSummary>(count);
        for (int i = 0; i < count; i++)
        {
            int nameOffset = i == 0 ? 1 : 88 * i;
            var reader = new PacketReader(payload.Slice(nameOffset, ProtocolConstants.MaxNameLength));
            string name = reader.ReadFixedString(ProtocolConstants.MaxNameLength);
            int metaOffset = nameOffset + ProtocolConstants.MaxNameLength;
            int equipOffset = i == 0 ? 40 : 88 * i + 40;
            int[] equips = new int[EquipCount];
            for (int e = 0; e < EquipCount; e++)
            {
                equips[e] = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(equipOffset + e * 4, 4));
            }

            roles.Add(new RoleSummary(
                name,
                payload[metaOffset + 0],
                payload[metaOffset + 1],
                payload[metaOffset + 2],
                payload[metaOffset + 3],
                payload[metaOffset + 4],
                payload[metaOffset + 5],
                equips));
        }

        return new MsgRoleInfo(roles);
    }
}

/// <summary>
/// C2S <c>MSG_CREATE_ROLE</c> (opcode 10003, 42B framed / 38B payload).
/// </summary>
public readonly record struct MsgCreateRole(
    string Name,
    byte Gender,
    byte Camp,
    byte Profession,
    byte Belief,
    byte HairStyle,
    byte FaceShap)
{
    public const int PayloadLength = 38;

    public static MsgCreateRole Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadLength)
        {
            throw new InvalidDataException($"MSG_CREATE_ROLE payload too short: {payload.Length} bytes.");
        }

        var reader = new PacketReader(payload);
        string name = reader.ReadFixedString(ProtocolConstants.MaxNameLength);
        byte gender = reader.ReadByte();
        byte camp = reader.ReadByte();
        byte profession = reader.ReadByte();
        byte belief = reader.ReadByte();
        byte hairStyle = reader.ReadByte();
        byte faceShap = reader.ReadByte();
        return new MsgCreateRole(name, gender, camp, profession, belief, hairStyle, faceShap);
    }

    public byte[] ToPayload()
    {
        byte[] payload = new byte[PayloadLength];
        MsgLogin.WriteFixedAscii(Name, payload.AsSpan(0, ProtocolConstants.MaxNameLength));
        payload[32] = Gender;
        payload[33] = Camp;
        payload[34] = Profession;
        payload[35] = Belief;
        payload[36] = HairStyle;
        payload[37] = FaceShap;
        return payload;
    }
}

/// <summary>
/// C2S <c>MSG_DELETE_ROLE</c> (opcode 10004, 68B framed / 64B payload).
/// </summary>
public readonly record struct MsgDeleteRole(string Account, string Name)
{
    public const int PayloadLength = 64;

    public static MsgDeleteRole Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadLength)
        {
            throw new InvalidDataException($"MSG_DELETE_ROLE payload too short: {payload.Length} bytes.");
        }

        var reader = new PacketReader(payload);
        string account = reader.ReadFixedString(ProtocolConstants.MaxNameLength);
        string name = reader.ReadFixedString(ProtocolConstants.MaxNameLength);
        return new MsgDeleteRole(account, name);
    }

    public byte[] ToPayload()
    {
        byte[] payload = new byte[PayloadLength];
        MsgLogin.WriteFixedAscii(Account, payload.AsSpan(0, ProtocolConstants.MaxNameLength));
        MsgLogin.WriteFixedAscii(Name, payload.AsSpan(ProtocolConstants.MaxNameLength, ProtocolConstants.MaxNameLength));
        return payload;
    }
}

/// <summary>
/// S2C <c>MSG_MANAGE_RETURN</c> (opcode 10158, 12B framed / 8B payload).
/// </summary>
public readonly record struct MsgManageReturn(ushort ErrorType, ManageReturnCode Disc)
{
    public const int PayloadLength = 8;

    public byte[] ToPayload()
    {
        byte[] payload = new byte[PayloadLength];
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(0, 2), ErrorType);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(4, 4), (int)Disc);
        return payload;
    }

    public static MsgManageReturn Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadLength)
        {
            throw new InvalidDataException($"MSG_MANAGE_RETURN payload too short: {payload.Length} bytes.");
        }

        ushort errorType = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(0, 2));
        var disc = (ManageReturnCode)BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(4, 4));
        return new MsgManageReturn(errorType, disc);
    }
}

/// <summary>
/// C2S <c>MSG_ENTER_GAME</c> (opcode 10006, 62B framed / 58B payload).
/// </summary>
public readonly record struct MsgEnterGame(string Name, string ValidateCode)
{
    public const int PayloadLength = 58;

    public static MsgEnterGame Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < ProtocolConstants.MaxNameLength)
        {
            throw new InvalidDataException($"MSG_ENTER_GAME payload too short: {payload.Length} bytes.");
        }

        var reader = new PacketReader(payload);
        string name = reader.ReadFixedString(ProtocolConstants.MaxNameLength);
        string code = payload.Length >= ProtocolConstants.MaxNameLength + ProtocolConstants.ValidateCodeLength
            ? reader.ReadFixedString(ProtocolConstants.ValidateCodeLength)
            : string.Empty;
        return new MsgEnterGame(name, code);
    }

    public byte[] ToPayload()
    {
        byte[] payload = new byte[PayloadLength];
        MsgLogin.WriteFixedAscii(Name, payload.AsSpan(0, ProtocolConstants.MaxNameLength));
        MsgLogin.WriteFixedAscii(ValidateCode, payload.AsSpan(ProtocolConstants.MaxNameLength, ProtocolConstants.ValidateCodeLength));
        return payload;
    }
}

/// <summary>
/// C2S <c>MSG_CLIENT_READY</c> (opcode 10007, 32B framed / 28B payload).
/// </summary>
public readonly record struct MsgClientReady(ushort UserId, string ValidateCode)
{
    public const int PayloadLength = 28;

    public static MsgClientReady Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 2)
        {
            throw new InvalidDataException($"MSG_CLIENT_READY payload too short: {payload.Length} bytes.");
        }

        ushort userId = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(0, 2));
        string code = payload.Length >= 2 + ProtocolConstants.ValidateCodeLength
            ? new PacketReader(payload[2..]).ReadFixedString(ProtocolConstants.ValidateCodeLength)
            : string.Empty;
        return new MsgClientReady(userId, code);
    }

    public byte[] ToPayload()
    {
        byte[] payload = new byte[PayloadLength];
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(0, 2), UserId);
        MsgLogin.WriteFixedAscii(ValidateCode, payload.AsSpan(2, ProtocolConstants.ValidateCodeLength));
        return payload;
    }
}

/// <summary>
/// S2C <c>MSG_SELFINFO</c> (opcode 10019, 1192B framed / 1188B payload).
/// </summary>
public sealed record MsgSelfInfo(
    int DbId,
    string Name,
    byte Gender,
    byte Camp,
    byte Belief,
    byte Profession,
    byte HairStyle,
    byte FaceShap,
    ushort MapId,
    ushort EquipMask,
    uint UserId,
    float X,
    float Y,
    float Z,
    uint MaxHp,
    uint MaxMp,
    uint CurHp,
    uint CurMp,
    uint Exp,
    uint MaxExp,
    uint SkillPoint,
    uint SkillExp,
    IReadOnlyList<ItemEquipSlot> Equips)
{
    public const int PayloadLength = 1188; // 1192 - 4

    public byte[] ToPayload()
    {
        byte[] payload = new byte[PayloadLength];
        Span<byte> span = payload;

        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(0, 4), DbId);
        MsgLogin.WriteFixedAscii(Name, span.Slice(4, ProtocolConstants.MaxNameLength));
        span[36] = Gender;
        span[37] = Camp;
        span[38] = Belief;
        span[39] = Profession;
        span[40] = HairStyle;
        span[41] = FaceShap;
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(42, 2), MapId);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(44, 2), (ushort)(EquipMask & 0x0FFF));
        // span[46..48] = 2-byte alignment pad
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(48, 4), UserId);
        BinaryPrimitives.WriteSingleLittleEndian(span.Slice(52, 4), X);
        BinaryPrimitives.WriteSingleLittleEndian(span.Slice(56, 4), Y);
        BinaryPrimitives.WriteSingleLittleEndian(span.Slice(60, 4), Z);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(64, 4), MaxHp);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(68, 4), MaxMp);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(72, 4), CurHp);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(76, 4), CurMp);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(80, 4), Exp);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(84, 4), MaxExp);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(88, 4), SkillPoint);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(92, 4), SkillExp);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(96, 4), 1); // extra != 0 suppresses lv1 newbie stub

        int packedIndex = 0;
        for (int bit = 0; bit < 12; bit++)
        {
            if ((EquipMask & (1 << bit)) != 0 && bit < Equips.Count && packedIndex < 15)
            {
                Equips[bit].WriteTo(span.Slice(100 + packedIndex * ItemEquipSlot.WireSize, ItemEquipSlot.WireSize));
                packedIndex++;
            }
        }

        return payload;
    }
}

/// <summary>
/// S2C <c>MSG_KITBAG</c> (opcode 10031, 888B framed / 884B payload per 12-slot page).
/// </summary>
public sealed record MsgKitbagPage(
    uint UserId,
    uint BagNum,
    byte BagIndex,
    byte StartSlot,
    uint Flag12,
    IReadOnlyList<ItemEquipSlot> Slots)
{
    public const int PayloadLength = 884; // 888 - 4

    public byte[] ToPayload()
    {
        byte[] payload = new byte[PayloadLength];
        Span<byte> span = payload;

        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(0, 4), UserId);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(4, 4), BagNum);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(8, 4), 0); // up_step
        span[12] = BagIndex;
        span[13] = StartSlot;
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(16, 4), Flag12 & 0x0FFF);

        for (int i = 0; i < 12; i++)
        {
            ItemEquipSlot slot = i < Slots.Count ? Slots[i] : ItemEquipSlot.Empty;
            slot.WriteTo(span.Slice(20 + i * ItemEquipSlot.WireSize, ItemEquipSlot.WireSize));
        }

        return payload;
    }
}

/// <summary>
/// 132-byte client <c>ObjectData</c> block shared by <see cref="MsgSelfProperty"/> and <see cref="MsgSynGameData"/>.
/// Ground truth: <c>docker/tools/gw_protocol.py:object_data_112_to_132</c>.
/// </summary>
public sealed record ClientObjectData(
    string Name,
    byte Gender,
    ushort RegionId,
    float X,
    float Y,
    float Z,
    float Speed,
    ushort Style,
    byte Faction,
    uint Prestige,
    ushort UnionId,
    byte UnionDuty,
    byte Knight,
    uint Contribute,
    uint StoreNum,
    uint BagNum,
    byte Profession,
    uint Exp,
    byte Rank,
    int Hp,
    int Mp,
    ushort AttackDistance,
    ushort AttackSpeed,
    byte HairStyle,
    byte FaceShap,
    ushort Belief,
    uint Money,
    uint Stone)
{
    public const int WireSize = 132;

    public void WriteTo(Span<byte> dest)
    {
        if (dest.Length < WireSize)
        {
            throw new ArgumentException($"Destination must be at least {WireSize} bytes.", nameof(dest));
        }

        dest[..WireSize].Clear();
        MsgLogin.WriteFixedAscii(Name, dest.Slice(0, ProtocolConstants.MaxNameLength));
        dest[32] = Gender;
        BinaryPrimitives.WriteUInt16LittleEndian(dest.Slice(34, 2), RegionId);
        BinaryPrimitives.WriteSingleLittleEndian(dest.Slice(36, 4), X);
        BinaryPrimitives.WriteSingleLittleEndian(dest.Slice(40, 4), Y);
        BinaryPrimitives.WriteSingleLittleEndian(dest.Slice(44, 4), Z);
        BinaryPrimitives.WriteSingleLittleEndian(dest.Slice(48, 4), Speed);
        BinaryPrimitives.WriteUInt16LittleEndian(dest.Slice(52, 2), Style);
        dest[54] = Faction;
        BinaryPrimitives.WriteUInt32LittleEndian(dest.Slice(56, 4), Prestige);
        BinaryPrimitives.WriteUInt16LittleEndian(dest.Slice(60, 2), UnionId);
        dest[62] = UnionDuty;
        dest[68] = Knight;
        BinaryPrimitives.WriteUInt32LittleEndian(dest.Slice(72, 4), Contribute);
        BinaryPrimitives.WriteUInt32LittleEndian(dest.Slice(76, 4), StoreNum);
        BinaryPrimitives.WriteUInt32LittleEndian(dest.Slice(80, 4), BagNum);
        dest[84] = Profession;
        BinaryPrimitives.WriteUInt32LittleEndian(dest.Slice(88, 4), Exp);
        dest[92] = Rank;
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(96, 4), Hp);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(100, 4), Mp);
        BinaryPrimitives.WriteUInt16LittleEndian(dest.Slice(104, 2), AttackDistance);
        BinaryPrimitives.WriteUInt16LittleEndian(dest.Slice(106, 2), AttackSpeed);
        dest[108] = HairStyle;
        dest[109] = FaceShap;
        BinaryPrimitives.WriteUInt16LittleEndian(dest.Slice(110, 2), Belief);
        BinaryPrimitives.WriteUInt32LittleEndian(dest.Slice(112, 4), Money);
        BinaryPrimitives.WriteUInt32LittleEndian(dest.Slice(116, 4), Stone);
    }
}

/// <summary>
/// S2C <c>MSG_SELFPROPERTY</c> (opcode 10041, 132B framed / 128B payload).
/// </summary>
public readonly record struct MsgSelfProperty(ClientObjectData ObjectData)
{
    public const int PayloadLength = 128; // 132 - 4

    public byte[] ToPayload()
    {
        byte[] full = new byte[ClientObjectData.WireSize];
        ObjectData.WriteTo(full);
        return full[..PayloadLength];
    }
}

/// <summary>
/// 76-byte (19 dwords) <c>sBaseData</c> block in <see cref="MsgSynGameData"/>.
/// Ground truth: <c>GW 2.0/Server Files (Source)/GameServer/GameCore/Player.cpp:609-641</c>.
/// </summary>
public readonly record struct ClientBaseStats(
    int MaxHp,
    int MaxMp,
    int ResumeHp,
    int ResumeMp,
    int Attack,
    int Defend,
    int MagicAttack,
    int MagicDefend,
    int Hit,
    int Dodge,
    int CritAppend,
    int CritDefend,
    float PhyDamageAppend,
    float MagicDamageAppend,
    int DamageSorb,
    float BeCure,
    float Cure,
    int StatusHit,
    int StatusDodge)
{
    public const int WireSize = 76;

    public void WriteTo(Span<byte> dest)
    {
        if (dest.Length < WireSize)
        {
            throw new ArgumentException($"Destination must be at least {WireSize} bytes.", nameof(dest));
        }

        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(0, 4), MaxHp);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(4, 4), MaxMp);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(8, 4), ResumeHp);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(12, 4), ResumeMp);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(16, 4), Attack);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(20, 4), Defend);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(24, 4), MagicAttack);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(28, 4), MagicDefend);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(32, 4), Hit);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(36, 4), Dodge);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(40, 4), CritAppend);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(44, 4), CritDefend);
        BinaryPrimitives.WriteSingleLittleEndian(dest.Slice(48, 4), PhyDamageAppend);
        BinaryPrimitives.WriteSingleLittleEndian(dest.Slice(52, 4), MagicDamageAppend);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(56, 4), DamageSorb);
        BinaryPrimitives.WriteSingleLittleEndian(dest.Slice(60, 4), BeCure);
        BinaryPrimitives.WriteSingleLittleEndian(dest.Slice(64, 4), Cure);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(68, 4), StatusHit);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(72, 4), StatusDodge);
    }
}

/// <summary>
/// S2C <c>MSG_SYN_GAMEDATA</c> (opcode 10160, 236B framed / 232B payload).
/// </summary>
public sealed record MsgSynGameData(
    uint UserId,
    ClientObjectData ObjectData,
    uint SkillPoint,
    ClientBaseStats? BaseStats = null)
{
    public const int PayloadLength = 232; // 236 - 4

    public byte[] ToPayload()
    {
        byte[] payload = new byte[PayloadLength];
        Span<byte> span = payload;

        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(0, 4), UserId);
        ObjectData.WriteTo(span.Slice(4, ClientObjectData.WireSize));
        if (BaseStats is { } stats)
        {
            stats.WriteTo(span.Slice(136, ClientBaseStats.WireSize));
        }
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(212, 4), ObjectData.StoreNum);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(216, 4), ObjectData.BagNum);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(220, 4), SkillPoint);
        // span[224..231] = trailing pad
        return payload;
    }
}

/// <summary>
/// S2C <c>MSG_OBJECTINFO</c> (opcode 10020, 628B+ framed).
/// </summary>
public sealed record MsgObjectInfo(
    WorldObjectType UiType,
    byte Camp,
    ushort MapId,
    uint UiId,
    uint Level,
    uint Hp,
    uint MaxHp,
    float X,
    float Y,
    float Z,
    float Atan2,
    string Name,
    uint DbId = 0,
    byte Gender = 0,
    byte Belief = 0,
    byte Profession = 0,
    byte HairStyle = 0,
    byte FaceShap = 0,
    byte Stall = 0,
    ushort EquipFlag = 0,
    IReadOnlyList<ItemEquipSlot>? Equips = null)
{
    public const int MinFramedSize = 628;

    public byte[] ToPayload()
    {
        int equipCount = BitOperations.PopCount((uint)(EquipFlag & 0x0FFF));
        int totalFramed = Math.Max(MinFramedSize, 344 + equipCount * ItemEquipSlot.WireSize);
        byte[] payload = new byte[totalFramed - PacketHeader.Size];
        Span<byte> span = payload;

        // Wire offsets shifted by -4 for payload
        span[0] = (byte)UiType;
        span[1] = Camp;
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(2, 2), MapId);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(4, 4), UiId);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(8, 4), Level);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(12, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(16, 4), Hp);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(20, 4), MaxHp);
        BinaryPrimitives.WriteSingleLittleEndian(span.Slice(24, 4), X);
        BinaryPrimitives.WriteSingleLittleEndian(span.Slice(28, 4), Y);
        BinaryPrimitives.WriteSingleLittleEndian(span.Slice(32, 4), Z);
        BinaryPrimitives.WriteSingleLittleEndian(span.Slice(36, 4), Atan2);

        MsgLogin.WriteFixedAscii(Name, span.Slice(40, 64)); // wire 44..107
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(104, 4), DbId);
        span[108] = Gender;
        span[109] = Belief;
        span[110] = Profession;
        span[111] = HairStyle;
        span[112] = FaceShap;
        span[113] = Stall;
        span[114] = 0;

        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(122, 2), (ushort)(EquipFlag & 0x0FFF)); // wire 126

        if (Equips is not null)
        {
            int packed = 0;
            for (int bit = 0; bit < 12 && packed < equipCount; bit++)
            {
                if ((EquipFlag & (1 << bit)) != 0 && bit < Equips.Count)
                {
                    Equips[bit].WriteTo(span.Slice(340 + packed * ItemEquipSlot.WireSize, ItemEquipSlot.WireSize));
                    packed++;
                }
            }
        }

        return payload;
    }
}

/// <summary>
/// S2C <c>MSG_LEAVE</c> (opcode 10021, 8B framed / 4B payload).
/// </summary>
public readonly record struct MsgLeave(uint UiId)
{
    public const int PayloadLength = 4;

    public byte[] ToPayload()
    {
        byte[] payload = new byte[PayloadLength];
        BinaryPrimitives.WriteUInt32LittleEndian(payload, UiId);
        return payload;
    }
}

public readonly record struct ActiveSkillEntry(int MagicId, ushort Rank);

/// <summary>
/// S2C <c>MSG_ACTIVESKILL_INFO</c> (opcode 10039, <c>12 + count * 8</c> framed).
/// </summary>
public sealed record MsgActiveSkillInfo(uint UiId, IReadOnlyList<ActiveSkillEntry> Skills)
{
    public byte[] ToPayload()
    {
        byte[] payload = new byte[8 + Skills.Count * 8];
        Span<byte> span = payload;
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(0, 4), UiId);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(4, 2), (ushort)Skills.Count);
        for (int i = 0; i < Skills.Count; i++)
        {
            int off = 8 + i * 8;
            BinaryPrimitives.WriteInt32LittleEndian(span.Slice(off, 4), Skills[i].MagicId);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(off + 4, 2), Skills[i].Rank);
        }

        return payload;
    }
}

public readonly record struct PassiveSkillEntry(int SkillId, ushort Rank, ushort EquipLevel, uint NextGradePoint);

/// <summary>
/// S2C <c>MSG_PASSIVESKILL_INFO</c> (opcode 10040, <c>12 + count * 12</c> framed).
/// </summary>
public sealed record MsgPassiveSkillInfo(uint UiId, IReadOnlyList<PassiveSkillEntry> Skills)
{
    public byte[] ToPayload()
    {
        byte[] payload = new byte[8 + Skills.Count * 12];
        Span<byte> span = payload;
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(0, 4), UiId);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(4, 2), (ushort)Skills.Count);
        for (int i = 0; i < Skills.Count; i++)
        {
            int off = 8 + i * 12;
            BinaryPrimitives.WriteInt32LittleEndian(span.Slice(off, 4), Skills[i].SkillId);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(off + 4, 2), Skills[i].Rank);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(off + 6, 2), Skills[i].EquipLevel);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(off + 8, 4), Skills[i].NextGradePoint);
        }

        return payload;
    }
}

/// <summary>
/// C2S/S2C <c>MSG_WALK</c> (opcode 10187, 20B framed / 16B payload).
/// </summary>
public readonly record struct MsgWalk(ushort IdOrMap, ushort State, float X, float Z, float Atan2)
{
    public const int PayloadLength = 16;

    public static MsgWalk Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadLength)
        {
            throw new InvalidDataException($"MSG_WALK payload too short: {payload.Length} bytes.");
        }

        ushort idOrMap = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(0, 2));
        ushort state = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(2, 2));
        float x = BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(4, 4));
        float z = BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(8, 4));
        float atan2 = BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(12, 4));
        return new MsgWalk(idOrMap, state, x, z, atan2);
    }

    public byte[] ToPayload()
    {
        byte[] payload = new byte[PayloadLength];
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(0, 2), IdOrMap);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(2, 2), State);
        BinaryPrimitives.WriteSingleLittleEndian(payload.AsSpan(4, 4), X);
        BinaryPrimitives.WriteSingleLittleEndian(payload.AsSpan(8, 4), Z);
        BinaryPrimitives.WriteSingleLittleEndian(payload.AsSpan(12, 4), Atan2);
        return payload;
    }
}

public readonly record struct RelationEntry(string Name, byte Camp, byte Profession, byte Level, byte Online);

/// <summary>
/// S2C <c>MSG_RELATIONALL</c> (opcode 10093, 12B framed when empty, +32B per entry).
/// </summary>
public readonly record struct MsgRelationAll(
    uint UiId,
    byte RelationType,
    byte Count = 0,
    IReadOnlyList<RelationEntry>? Entries = null)
{
    public byte[] ToPayload()
    {
        int count = Entries?.Count ?? Count;
        byte[] payload = new byte[8 + count * 32];
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(0, 4), UiId);
        payload[4] = RelationType;
        payload[5] = (byte)count;
        if (Entries is not null)
        {
            for (int i = 0; i < count; i++)
            {
                Span<byte> entry = payload.AsSpan(8 + i * 32, 32);
                FixedStringCodec.WriteFixedString(entry[..18], Entries[i].Name);
                entry[22] = Entries[i].Camp;
                entry[23] = Entries[i].Profession;
                entry[24] = Entries[i].Level;
                entry[25] = Entries[i].Online;
            }
        }
        return payload;
    }
}

/// <summary>
/// S2C <c>MSG_ALTAR_INFO</c> (opcode 10156, 8B framed when empty).
/// </summary>
public readonly record struct MsgAltarInfo(int AltarCount = 0)
{
    public byte[] ToPayload()
    {
        byte[] payload = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(payload, AltarCount);
        return payload;
    }
}

/// <summary>
/// S2C <c>MSG_SERVERTIME</c> (opcode 10303, 16B framed / 12B payload).
/// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x48be3e..0x48be60</c>
/// (<c>int32 TimeZoneHours</c> at +0, <c>int64 __time64_t EpochSeconds</c> at +4).
/// </summary>
public readonly record struct MsgServerTime(long EpochSeconds, int TimeZoneHours = 0)
{
    public byte[] ToPayload()
    {
        byte[] payload = new byte[12];
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(0, 4), TimeZoneHours);
        BinaryPrimitives.WriteInt64LittleEndian(payload.AsSpan(4, 8), EpochSeconds);
        return payload;
    }
}
