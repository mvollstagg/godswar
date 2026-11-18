using System.Buffers.Binary;

namespace GodsWar.Protocol.Packets;

/// <summary>
/// C2S / S2C <c>MSG_TALKNPC</c> (opcode 10064, 48B framed / 44B payload).
/// </summary>
public readonly record struct MsgTalkNpc(uint NpcId, int NpcFlag, int SysFlag, string Talk)
{
    public const int PayloadSize = 44;

    public static MsgTalkNpc Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 4)
        {
            throw new InvalidDataException($"MSG_TALKNPC requires at least 4 payload bytes, got {payload.Length}.");
        }

        uint npcId = BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(0, 4));
        int npcFlag = payload.Length >= 8 ? BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(4, 4)) : 0;
        int sysFlag = payload.Length >= 12 ? BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(8, 4)) : 0;
        string talk = payload.Length >= PayloadSize
            ? FixedStringCodec.ReadFixedString(payload.Slice(12, 32))
            : string.Empty;

        return new MsgTalkNpc(npcId, npcFlag, sysFlag, talk);
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), NpcId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4, 4), NpcFlag);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(8, 4), SysFlag);
        FixedStringCodec.WriteFixedString(buf.AsSpan(12, 32), Talk);
        return buf;
    }
}

/// <summary>
/// C2S <c>MSG_NPC_DATA</c> (opcode 10065, 8B framed / 4B payload).
/// </summary>
public readonly record struct MsgNpcData(uint NpcId)
{
    public const int PayloadSize = 4;

    public static MsgNpcData Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadSize)
        {
            throw new InvalidDataException($"MSG_NPC_DATA requires {PayloadSize} payload bytes, got {payload.Length}.");
        }

        return new MsgNpcData(BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(0, 4)));
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), NpcId);
        return buf;
    }
}

/// <summary>
/// C2S / S2C <c>MSG_SYS_NPC_DATA</c> (opcode 10066).
/// Client 1.0.282 sends 92B (88B payload) or 28B (24B payload): <c>NpcId(4) + NpcType(4) + State[8]</c>.
/// </summary>
public readonly record struct MsgSysNpcData(uint NpcId, int NpcType, int[] States)
{
    public const int PayloadSize = 40;

    public static MsgSysNpcData Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 8)
        {
            throw new InvalidDataException($"MSG_SYS_NPC_DATA requires at least 8 payload bytes, got {payload.Length}.");
        }

        uint npcId = BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(0, 4));
        int npcType = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(4, 4));
        int count = Math.Min(8, (payload.Length - 8) / 4);
        int[] states = new int[8];
        for (int i = 0; i < count; i++)
        {
            states[i] = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(8 + i * 4, 4));
        }

        return new MsgSysNpcData(npcId, npcType, states);
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), NpcId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4, 4), NpcType);
        for (int i = 0; i < Math.Min(8, States.Length); i++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(8 + i * 4, 4), States[i]);
        }

        return buf;
    }
}

public readonly record struct NpcShopItemEntry(
    int Base,
    byte AppendLevel,
    byte BaseLevel,
    byte Binding,
    byte Overlap,
    byte Sell,
    byte BuyType0,
    byte BuyType1,
    byte BuyType2,
    uint BuyConsume0,
    uint BuyConsume1,
    uint BuyConsume2,
    byte BuyTypeNum)
{
    public const int WireSize = 88; // 0x58 on 1.0.282 client (0x44cb55)

    public void WriteTo(Span<byte> dest)
    {
        dest[..WireSize].Clear();
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(0, 4), Base);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(4, 4), -1);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(8, 4), -1);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(12, 4), -1);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(16, 4), -1);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(20, 4), -1);
        dest[24] = BaseLevel;
        dest[25] = AppendLevel;
        dest[26] = Binding;
        dest[27] = Overlap;
        dest[64] = Sell;
        dest[65] = BuyType0;
        dest[66] = BuyType1;
        dest[67] = BuyType2;
        BinaryPrimitives.WriteUInt32LittleEndian(dest.Slice(68, 4), BuyConsume0);
        BinaryPrimitives.WriteUInt32LittleEndian(dest.Slice(72, 4), BuyConsume1);
        BinaryPrimitives.WriteUInt32LittleEndian(dest.Slice(76, 4), BuyConsume2);
        dest[80] = BuyTypeNum;
    }
}

/// <summary>
/// S2C <c>MSG_NPC_ITEMDATA</c> (opcode 10068).
/// </summary>
public readonly record struct MsgNpcItemData(
    uint NpcId,
    byte TabIndex,
    byte MaxTabIndex,
    IReadOnlyList<NpcShopItemEntry> Items)
{
    public const int MaxItems = 32;
    public const int PayloadSize = 12 + MaxItems * NpcShopItemEntry.WireSize;

    public byte[] ToPayload()
    {
        int count = Math.Min(MaxItems, Items.Count);
        byte[] buf = new byte[12 + count * NpcShopItemEntry.WireSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), NpcId);
        buf[4] = TabIndex;
        buf[6] = (byte)count;
        buf[7] = MaxTabIndex;
        for (int i = 0; i < count; i++)
        {
            Items[i].WriteTo(buf.AsSpan(12 + i * NpcShopItemEntry.WireSize, NpcShopItemEntry.WireSize));
        }

        return buf;
    }
}

/// <summary>
/// S2C <c>MSG_NPC_STORAGEDATA</c> (opcode 10069, 12B framed / 8B payload).
/// </summary>
public readonly record struct MsgNpcStorageData(uint NpcId, int StorageSlots)
{
    public const int PayloadSize = 8;

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), NpcId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4, 4), StorageSlots);
        return buf;
    }
}

/// <summary>
/// C2S / S2C <c>MSG_NPC_SELL</c> (opcode 10070, 24B framed / 20B payload).
/// </summary>
public readonly record struct MsgNpcSell(uint NpcId, int Index, int Num, int Count, int Base)
{
    public const int PayloadSize = 20;

    public static MsgNpcSell Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadSize)
        {
            throw new InvalidDataException($"MSG_NPC_SELL requires {PayloadSize} payload bytes, got {payload.Length}.");
        }

        return new MsgNpcSell(
            BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(0, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(4, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(8, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(12, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(16, 4)));
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), NpcId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4, 4), Index);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(8, 4), Num);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(12, 4), Count);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(16, 4), Base);
        return buf;
    }
}

/// <summary>
/// C2S / S2C <c>MSG_SELL</c> (opcode 10058, 8B framed / 4B payload).
/// </summary>
public readonly record struct MsgSell(ushort Index, ushort Num)
{
    public const int PayloadSize = 4;

    public static MsgSell Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadSize)
        {
            throw new InvalidDataException($"MSG_SELL requires {PayloadSize} payload bytes, got {payload.Length}.");
        }

        return new MsgSell(
            BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(0, 2)),
            BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(2, 2)));
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(0, 2), Index);
        BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(2, 2), Num);
        return buf;
    }
}

/// <summary>
/// C2S / S2C <c>MSG_STORAGEITEM</c> (opcode 10057, 16B framed / 12B payload).
/// </summary>
public readonly record struct MsgStorageItem(short Index, short IndexAim, short NumAim, int Money, bool ToStorage)
{
    public const int PayloadSize = 12;

    public static MsgStorageItem Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadSize)
        {
            throw new InvalidDataException($"MSG_STORAGEITEM requires {PayloadSize} payload bytes, got {payload.Length}.");
        }

        return new MsgStorageItem(
            BinaryPrimitives.ReadInt16LittleEndian(payload.Slice(0, 2)),
            BinaryPrimitives.ReadInt16LittleEndian(payload.Slice(2, 2)),
            BinaryPrimitives.ReadInt16LittleEndian(payload.Slice(4, 2)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(6, 4)),
            payload[10] != 0);
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteInt16LittleEndian(buf.AsSpan(0, 2), Index);
        BinaryPrimitives.WriteInt16LittleEndian(buf.AsSpan(2, 2), IndexAim);
        BinaryPrimitives.WriteInt16LittleEndian(buf.AsSpan(4, 2), NumAim);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(6, 4), Money);
        buf[10] = ToStorage ? (byte)1 : (byte)0;
        return buf;
    }
}
