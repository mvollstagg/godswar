using System.Buffers.Binary;

namespace GodsWar.Protocol.Packets;

/// <summary>
/// C2S / S2C <c>MSG_FIGHT</c> (opcode 10023, 16B framed / 12B payload).
/// </summary>
public readonly record struct MsgFight(uint UiId, uint TargetId, bool Fight)
{
    public const int PayloadSize = 12;

    public static MsgFight Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadSize)
        {
            throw new InvalidDataException($"MSG_FIGHT requires {PayloadSize} payload bytes, got {payload.Length}.");
        }

        return new MsgFight(
            BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(0, 4)),
            BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(4, 4)),
            payload[8] != 0);
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), UiId);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(4, 4), TargetId);
        buf[8] = Fight ? (byte)1 : (byte)0;
        return buf;
    }
}

/// <summary>
/// C2S / S2C <c>MSG_ATTACK</c> (opcode 10024, 32B framed / 28B payload).
/// AttackType: 0 = Crit (Force), 1 = Hit, 2 = Miss.
/// </summary>
public readonly record struct MsgAttack(
    uint UiId,
    float X,
    float Y,
    float Z,
    uint ObjectId,
    int Damage,
    byte WeaponType,
    byte AttackType)
{
    public const int PayloadSize = 28;

    public static MsgAttack Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadSize)
        {
            throw new InvalidDataException($"MSG_ATTACK requires {PayloadSize} payload bytes, got {payload.Length}.");
        }

        return new MsgAttack(
            BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(0, 4)),
            BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(4, 4)),
            BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(8, 4)),
            BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(12, 4)),
            BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(16, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(20, 4)),
            payload[24],
            payload[25]);
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), UiId);
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(4, 4), X);
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(8, 4), Y);
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(12, 4), Z);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(16, 4), ObjectId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(20, 4), Damage);
        buf[24] = WeaponType;
        buf[25] = AttackType;
        return buf;
    }
}

/// <summary>
/// S2C <c>MSG_DEAD</c> (opcode 10025, 116B framed / 112B payload).
/// </summary>
public sealed record MsgDead(
    uint UiId,
    int[] PlayerIds,
    uint[] PlayerMoney,
    uint[] Exp,
    uint[] SkillExp,
    uint[] SkillPoint,
    uint KillerId,
    uint KillerPrestige)
{
    public const int MaxTeam = 5;
    public const int PayloadSize = 112;

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), UiId);
        for (int i = 0; i < MaxTeam; i++)
        {
            int pid = i < PlayerIds.Length ? PlayerIds[i] : -1;
            uint money = i < PlayerMoney.Length ? PlayerMoney[i] : 0u;
            uint exp = i < Exp.Length ? Exp[i] : 0u;
            uint skExp = i < SkillExp.Length ? SkillExp[i] : 0u;
            uint skPt = i < SkillPoint.Length ? SkillPoint[i] : 0u;

            BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4 + i * 4, 4), pid);
            BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(24 + i * 4, 4), money);
            BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(44 + i * 4, 4), exp);
            BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(64 + i * 4, 4), skExp);
            BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(84 + i * 4, 4), skPt);
        }

        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(104, 4), KillerId);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(108, 4), KillerPrestige);
        return buf;
    }
}

/// <summary>
/// C2S / S2C <c>MSG_BACKHOME</c> (opcode 10026, 12B framed / 8B payload).
/// </summary>
public readonly record struct MsgBackHome(uint UiId, int ReviveType)
{
    public const int PayloadSize = 8;

    public static MsgBackHome Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadSize)
        {
            throw new InvalidDataException($"MSG_BACKHOME requires {PayloadSize} payload bytes, got {payload.Length}.");
        }

        return new MsgBackHome(
            BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(0, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(4, 4)));
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), UiId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4, 4), ReviveType);
        return buf;
    }
}

/// <summary>
/// S2C <c>MSG_UPGRADE</c> (opcode 10028, 36B framed / 32B payload).
/// </summary>
public readonly record struct MsgUpgrade(
    uint Id,
    uint Rank,
    uint MaxExp,
    uint CurExp,
    uint MaxHp,
    uint CurHp,
    uint MaxMp,
    uint CurMp)
{
    public const int PayloadSize = 32;

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), Id);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(4, 4), Rank);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(8, 4), MaxExp);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(12, 4), CurExp);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(16, 4), MaxHp);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(20, 4), CurHp);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(24, 4), MaxMp);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(28, 4), CurMp);
        return buf;
    }
}

/// <summary>
/// S2C <c>MSG_PARAMATER</c> (opcode 10037, 224B framed / 220B payload).
/// </summary>
public sealed class MsgParamater
{
    public const int PayloadSize = 220;

    public static byte[] CreateDefaultPayload()
    {
        byte[] buf = new byte[PayloadSize];
        // Populate sensible defaults matching DefaultValue[] in Mat.cpp
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(0, 4), 20);     // Fighter_HP_A
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(4, 4), 12f);   // Fighter_HP_B
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(16, 4), 10);    // Fighter_MP_A
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(20, 4), 4f);   // Fighter_MP_B
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(28, 4), 5);     // Fighter_Attack_A
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(32, 4), 2f);   // Fighter_Attack_B
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(44, 4), 3);     // Fighter_Defend_A
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(48, 4), 1.5f); // Fighter_Defend_B

        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(80, 4), 15);    // Scholar_HP_A
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(84, 4), 9f);   // Scholar_HP_B
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(96, 4), 20);    // Scholar_MP_A
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(100, 4), 8f);  // Scholar_MP_B
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(108, 4), 6);    // Scholar_Attack_A
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(112, 4), 2.5f);// Scholar_Attack_B

        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(160, 4), 85);   // Default_Hit
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(164, 4), 10);   // Default_Dodge
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(168, 4), 10);   // Default_MagicExempt
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(188, 4), 2.0f);// Default_Resume_HP
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(192, 4), 2.0f);// Default_Resume_MP
        return buf;
    }
}

/// <summary>
/// C2S / S2C <c>MSG_SKILL</c> (opcode 10038, 40B framed / 36B payload).
/// </summary>
public readonly record struct MsgSkill(
    uint UiId,
    int MagicId,
    byte Step,
    int ObjectId,
    byte MpCost,
    float X,
    float Z,
    float TargetX,
    float TargetZ)
{
    public const int PayloadSize = 36;

    public static MsgSkill Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadSize)
        {
            throw new InvalidDataException($"MSG_SKILL requires {PayloadSize} payload bytes, got {payload.Length}.");
        }

        return new MsgSkill(
            BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(0, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(4, 4)),
            payload[8],
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(12, 4)),
            payload[16],
            BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(20, 4)),
            BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(24, 4)),
            BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(28, 4)),
            BinaryPrimitives.ReadSingleLittleEndian(payload.Slice(32, 4)));
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), UiId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4, 4), MagicId);
        buf[8] = Step;
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(12, 4), ObjectId);
        buf[16] = MpCost;
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(20, 4), X);
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(24, 4), Z);
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(28, 4), TargetX);
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(32, 4), TargetZ);
        return buf;
    }
}

/// <summary>
/// S2C <c>MSG_EFFECT</c> (opcode 10042, 44B framed / 40B payload).
/// </summary>
public readonly record struct MsgEffect(
    uint UiId,
    float Color1R,
    float Color1G,
    float Color1B,
    float Color2R,
    float Color2G,
    float Color2B,
    float Color3R,
    float Color3G,
    float Color3B)
{
    public const int PayloadSize = 40;

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), UiId);
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(4, 4), Color1R);
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(8, 4), Color1G);
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(12, 4), Color1B);
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(16, 4), Color2R);
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(20, 4), Color2G);
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(24, 4), Color2B);
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(28, 4), Color3R);
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(32, 4), Color3G);
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(36, 4), Color3B);
        return buf;
    }
}

/// <summary>
/// S2C <c>MSG_MAGIC_DAMAGE</c> (opcode 10043, 36B framed / 32B payload).
/// </summary>
public readonly record struct MsgMagicDamage(
    uint UiId,
    int ObjectId,
    byte AttackType,
    byte StatusType,
    byte TargetType,
    int Power,
    int MagicId,
    float TargetX,
    float TargetZ)
{
    public const int PayloadSize = 32;

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), UiId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4, 4), ObjectId);
        buf[8] = AttackType;
        buf[9] = StatusType;
        buf[10] = TargetType;
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(12, 4), Power);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(16, 4), MagicId);
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(20, 4), TargetX);
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(24, 4), TargetZ);
        return buf;
    }
}

/// <summary>
/// S2C <c>MSG_MAGIC_PERFORM</c> (opcode 10044, 28B framed / 24B payload).
/// </summary>
public readonly record struct MsgMagicPerform(
    uint UiId,
    int ObjectId,
    uint MagicId,
    float X,
    float Z)
{
    public const int PayloadSize = 24;

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), UiId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4, 4), ObjectId);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(8, 4), MagicId);
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(12, 4), X);
        BinaryPrimitives.WriteSingleLittleEndian(buf.AsSpan(16, 4), Z);
        return buf;
    }
}

public readonly record struct MagicClusterEntry(uint ObjectId, byte AttackType, byte TargetType, int Power);

/// <summary>
/// S2C <c>MSG_MAGIC_CLUSTER_DAMAGE</c> (opcode 10045, packed: 13B header + 10B * Count payload).
/// </summary>
public sealed record MsgMagicClusterDamage(
    uint UiId,
    uint MagicId,
    byte HitStatus,
    IReadOnlyList<MagicClusterEntry> Entries)
{
    public byte[] ToPayload()
    {
        int count = Math.Min(Entries.Count, 100);
        byte[] buf = new byte[13 + count * 10];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), UiId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4, 4), count);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(8, 4), MagicId);
        buf[12] = HitStatus;

        for (int i = 0; i < count; i++)
        {
            MagicClusterEntry e = Entries[i];
            int off = 13 + i * 10;
            BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(off, 4), e.ObjectId);
            buf[off + 4] = e.AttackType;
            buf[off + 5] = e.TargetType;
            BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(off + 6, 4), e.Power);
        }

        return buf;
    }
}

/// <summary>
/// S2C <c>MSG_UPDATE_MP</c> (opcode 10129, 12B framed / 8B payload).
/// </summary>
public readonly record struct MsgUpdateMp(uint PlayerId, int Mp)
{
    public const int PayloadSize = 8;

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), PlayerId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4, 4), Mp);
        return buf;
    }
}

/// <summary>
/// S2C <c>MSG_STATUS</c> (opcode 10161, 280B framed / 276B payload).
/// </summary>
public sealed record MsgStatus(
    uint UiId,
    IReadOnlyList<(uint StatusId, ushort SurplusSeconds)> Statuses)
{
    public const int MaxStatuses = 20;
    public const int PayloadSize = 276;

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        int count = Math.Min(Statuses.Count, MaxStatuses);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), UiId);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(4, 4), (uint)count);

        for (int i = 0; i < count; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(8 + i * 4, 4), Statuses[i].StatusId);
            BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(88 + i * 2, 2), Statuses[i].SurplusSeconds);
        }

        // Remaining 148 bytes (offset 128..276) are StatusData (zeroed unless deltas applied)
        return buf;
    }
}
