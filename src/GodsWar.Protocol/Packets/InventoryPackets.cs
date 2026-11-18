using System.Buffers.Binary;

namespace GodsWar.Protocol.Packets;

/// <summary>
/// S2C <c>MSG_DROPS</c> (opcode 10027, 12B + DropNum * 32B framed / 8B + DropNum * 32B payload).
/// </summary>
public sealed record MsgDrops(uint MonsterUiId, IReadOnlyList<ItemEquipSlot> Drops)
{
    public byte[] ToPayload()
    {
        int count = Math.Min(Drops.Count, 8);
        byte[] buf = new byte[8 + count * ItemEquipSlot.WireSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), MonsterUiId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4, 4), count);
        for (int i = 0; i < count; i++)
        {
            Drops[i].WriteTo(buf.AsSpan(8 + i * ItemEquipSlot.WireSize, ItemEquipSlot.WireSize));
        }
        return buf;
    }
}

/// <summary>
/// C2S / S2C <c>MSG_PICKUP_DROPS</c> (opcode 10048, 16B framed / 12B payload).
/// </summary>
public readonly record struct MsgPickupDrops(uint UiId, uint MonsterId, int PickupIndex)
{
    public const int PayloadSize = 12;

    public static MsgPickupDrops Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadSize)
        {
            throw new InvalidDataException($"MSG_PICKUP_DROPS requires {PayloadSize} payload bytes, got {payload.Length}.");
        }

        return new MsgPickupDrops(
            BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(0, 4)),
            BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(4, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(8, 4)));
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), UiId);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(4, 4), MonsterId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(8, 4), PickupIndex);
        return buf;
    }
}

/// <summary>
/// C2S / S2C <c>MSG_USEOREQUIP_ITEM</c> (opcode 10049, 44B framed / 40B payload).
/// </summary>
public readonly record struct MsgUseOrEquip(
    uint UiId,
    ushort Index,
    ushort Num,
    int ChangeF,
    int Base,
    int Append0,
    int Append1,
    int Append2,
    int Append3,
    int Append4,
    byte AppendLevel,
    byte BaseLevel,
    bool False)
{
    public const int PayloadSize = 80; // 84B framed (0x54) on 1.0.282 client

    public static MsgUseOrEquip Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 40)
        {
            throw new InvalidDataException($"MSG_USEOREQUIP_ITEM requires >= 40 payload bytes, got {payload.Length}.");
        }

        return new MsgUseOrEquip(
            BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(0, 4)),
            BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(8, 2)),
            BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(10, 2)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(12, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(44, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(48, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(52, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(56, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(60, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(64, 4)),
            payload[68],
            payload[69],
            payload[42] != 0);
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), UiId);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(4, 4), UiId);
        BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(8, 2), Index);
        BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(10, 2), Num);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(12, 4), ChangeF);
        buf[42] = False ? (byte)1 : (byte)0;
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(44, 4), Base);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(48, 4), Append0);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(52, 4), Append1);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(56, 4), Append2);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(60, 4), Append3);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(64, 4), Append4);
        buf[68] = AppendLevel;
        buf[69] = BaseLevel;
        return buf;
    }
}

/// <summary>
/// C2S / S2C <c>MSG_MOVE_ITEM</c> (opcode 10050, 44B framed / 40B payload).
/// </summary>
public readonly record struct MsgMoveItem(
    uint UiId,
    short Index,
    short Num,
    short IndexAim,
    short NumAim,
    int Base,
    int Append0,
    int Append1,
    int Append2,
    int Append3,
    int Append4,
    byte AppendLevel,
    byte BaseLevel)
{
    public const int PayloadSize = 76; // 80B framed (0x50) C2S on 1.0.282 client
    public const int ClientPayloadSize = 80; // S2C 0x4885e1 reads UiId@4, Index@8, Num@10, IndexAim@12, NumAim@14, Append@44 (9 dwords)

    public static MsgMoveItem Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 40)
        {
            throw new InvalidDataException($"MSG_MOVE_ITEM requires >= 40 payload bytes, got {payload.Length}.");
        }

        return new MsgMoveItem(
            BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(0, 4)),
            BinaryPrimitives.ReadInt16LittleEndian(payload.Slice(4, 2)),
            BinaryPrimitives.ReadInt16LittleEndian(payload.Slice(6, 2)),
            BinaryPrimitives.ReadInt16LittleEndian(payload.Slice(8, 2)),
            BinaryPrimitives.ReadInt16LittleEndian(payload.Slice(10, 2)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(12, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(16, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(20, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(24, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(28, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(32, 4)),
            payload[36],
            payload[37]);
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), UiId);
        BinaryPrimitives.WriteInt16LittleEndian(buf.AsSpan(4, 2), Index);
        BinaryPrimitives.WriteInt16LittleEndian(buf.AsSpan(6, 2), Num);
        BinaryPrimitives.WriteInt16LittleEndian(buf.AsSpan(8, 2), IndexAim);
        BinaryPrimitives.WriteInt16LittleEndian(buf.AsSpan(10, 2), NumAim);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(12, 4), Base);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(16, 4), Append0);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(20, 4), Append1);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(24, 4), Append2);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(28, 4), Append3);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(32, 4), Append4);
        buf[36] = AppendLevel;
        buf[37] = BaseLevel;
        return buf;
    }

    /// <summary>
    /// Serializes the 1.0.282 S2C <c>MSG_MOVE_ITEM</c> layout (<c>UiId@4, Index@8, Num@10, IndexAim@12, NumAim@14, Append@44</c>).
    /// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x4885e1</c>.
    /// </summary>
    public byte[] ToClientPayload()
    {
        byte[] buf = new byte[ClientPayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(4, 4), UiId);
        BinaryPrimitives.WriteInt16LittleEndian(buf.AsSpan(8, 2), Index);
        BinaryPrimitives.WriteInt16LittleEndian(buf.AsSpan(10, 2), Num);
        BinaryPrimitives.WriteInt16LittleEndian(buf.AsSpan(12, 2), IndexAim);
        BinaryPrimitives.WriteInt16LittleEndian(buf.AsSpan(14, 2), NumAim);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(44, 4), Base);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(48, 4), Append0);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(52, 4), Append1);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(56, 4), Append2);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(60, 4), Append3);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(64, 4), Append4);
        buf[68] = AppendLevel;
        buf[69] = BaseLevel;
        return buf;
    }
}

/// <summary>
/// C2S / S2C <c>MSG_BREAK_ITEM</c> (opcode 10056, 16B framed / 12B payload).
/// </summary>
public readonly record struct MsgBreakItem(
    short Flags,
    short Index,
    short Num,
    short IndexAim,
    short NumAim,
    short Overlap)
{
    public const int PayloadSize = 12;

    public static MsgBreakItem Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadSize)
        {
            throw new InvalidDataException($"MSG_BREAK_ITEM requires {PayloadSize} payload bytes, got {payload.Length}.");
        }

        return new MsgBreakItem(
            BinaryPrimitives.ReadInt16LittleEndian(payload.Slice(0, 2)),
            BinaryPrimitives.ReadInt16LittleEndian(payload.Slice(2, 2)),
            BinaryPrimitives.ReadInt16LittleEndian(payload.Slice(4, 2)),
            BinaryPrimitives.ReadInt16LittleEndian(payload.Slice(6, 2)),
            BinaryPrimitives.ReadInt16LittleEndian(payload.Slice(8, 2)),
            BinaryPrimitives.ReadInt16LittleEndian(payload.Slice(10, 2)));
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteInt16LittleEndian(buf.AsSpan(0, 2), Flags);
        BinaryPrimitives.WriteInt16LittleEndian(buf.AsSpan(2, 2), Index);
        BinaryPrimitives.WriteInt16LittleEndian(buf.AsSpan(4, 2), Num);
        BinaryPrimitives.WriteInt16LittleEndian(buf.AsSpan(6, 2), IndexAim);
        BinaryPrimitives.WriteInt16LittleEndian(buf.AsSpan(8, 2), NumAim);
        BinaryPrimitives.WriteInt16LittleEndian(buf.AsSpan(10, 2), Overlap);
        return buf;
    }
}
