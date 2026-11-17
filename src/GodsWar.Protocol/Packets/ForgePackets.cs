using System.Buffers.Binary;

namespace GodsWar.Protocol.Packets;

/// <summary>
/// C2S / S2C <c>MSG_EQUIPFORGE_EQUIP</c> (opcode 10106, 16B framed / 12B payload).
/// </summary>
public readonly record struct MsgEquipForgeEquip(int Index, int Num, int Type)
{
    public const int PayloadSize = 12;

    public static MsgEquipForgeEquip Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadSize)
        {
            throw new InvalidDataException($"MSG_EQUIPFORGE_EQUIP requires {PayloadSize} payload bytes, got {payload.Length}.");
        }

        return new MsgEquipForgeEquip(
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(0, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(4, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(8, 4)));
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(0, 4), Index);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4, 4), Num);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(8, 4), Type);
        return buf;
    }
}

/// <summary>
/// C2S / S2C <c>MSG_EQUIPFORGE_MATERIAL</c> (opcode 10107, 16B framed / 12B payload).
/// </summary>
public readonly record struct MsgEquipForgeMaterial(int Index, int Num, int Type)
{
    public const int PayloadSize = 12;

    public static MsgEquipForgeMaterial Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < PayloadSize)
        {
            throw new InvalidDataException($"MSG_EQUIPFORGE_MATERIAL requires {PayloadSize} payload bytes, got {payload.Length}.");
        }

        return new MsgEquipForgeMaterial(
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(0, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(4, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(8, 4)));
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(0, 4), Index);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4, 4), Num);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(8, 4), Type);
        return buf;
    }
}

/// <summary>
/// C2S / S2C <c>MSG_EQUIPFORGE</c> (opcode 10103, 40B framed / 36B payload).
/// </summary>
public readonly record struct MsgEquipForge(
    bool Success,
    int Type,
    int BaseLevel,
    int AppendLevel,
    int[] AppendIds)
{
    public const int PayloadSize = 36;

    public static MsgEquipForge Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 4)
        {
            throw new InvalidDataException($"MSG_EQUIPFORGE requires at least 4 payload bytes, got {payload.Length}.");
        }

        bool success = payload[0] != 0;
        int type = payload.Length >= 8 ? BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(4, 4)) : 0;
        int baseLv = payload.Length >= 12 ? BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(8, 4)) : 0;
        int appLv = payload.Length >= 16 ? BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(12, 4)) : 0;
        int[] appIds = new int[5];
        for (int i = 0; i < 5 && 16 + i * 4 + 4 <= payload.Length; i++)
        {
            appIds[i] = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(16 + i * 4, 4));
        }

        return new MsgEquipForge(success, type, baseLv, appLv, appIds);
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        buf[0] = Success ? (byte)1 : (byte)0;
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4, 4), Type);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(8, 4), BaseLevel);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(12, 4), AppendLevel);
        for (int i = 0; i < Math.Min(5, AppendIds.Length); i++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(16 + i * 4, 4), AppendIds[i]);
        }

        return buf;
    }
}
