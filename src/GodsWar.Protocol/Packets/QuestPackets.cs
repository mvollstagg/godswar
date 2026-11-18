using System.Buffers.Binary;

namespace GodsWar.Protocol.Packets;

public static class QuestWireConstants
{
    public const int ObjectivesCount = 4;
    public const int RewardsCount = 4;
    public const int ExplorerCount = 4;
    public const int NpcTakeCount = 10;
    public const int FindCount = 32;
    public const int ItemData32Size = 32;

    public static void WriteItemData32(Span<byte> dest, ItemEquipSlot item)
    {
        dest[..ItemData32Size].Clear();
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(0, 4), item.Base);
        dest[4] = item.BaseLevel;
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(8, 4), item.Append0);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(12, 4), item.Append1);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(16, 4), item.Append2);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(20, 4), item.Append3);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(24, 4), item.Append4);
        dest[28] = item.AppendLevel;
        dest[29] = item.Binding;
        dest[30] = item.Overlap;
    }
}

public readonly record struct NpcQuestEntry(int QuestId, bool Available);

public readonly record struct PlayerQuestWire(
    int QuestId,
    int NpcId,
    int RewardNpcId,
    int QuestSort,
    int Money,
    int Bijou,
    int RewardMoney,
    int RewardBijou,
    short[] Items,
    short[] Creatures,
    short[] ItemCounts,
    short[] CreatureCounts,
    bool[] ExplorerStates,
    byte SpecialFlags,
    byte Status,
    byte Completed,
    short[] ItemCompletedCounts,
    short[] CreatureCompletedCounts,
    ItemEquipSlot[] RewardItems)
{
    // 8*4 (32) + 4*2*4 (32) + 4 (explorer) + 3 (flags/status/completed) + 1(pad) + 2*2*4 (16) + 4*32 (128) = 216B
    public const int WireSize = 216;

    public void WriteTo(Span<byte> dest)
    {
        dest[..WireSize].Clear();
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(0, 4), QuestId);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(4, 4), NpcId);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(8, 4), RewardNpcId);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(12, 4), QuestSort);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(16, 4), Money);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(20, 4), Bijou);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(24, 4), RewardMoney);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(28, 4), RewardBijou);

        for (int i = 0; i < QuestWireConstants.ObjectivesCount; i++)
        {
            BinaryPrimitives.WriteInt16LittleEndian(dest.Slice(32 + i * 2, 2), i < Items.Length ? Items[i] : (short)-1);
            BinaryPrimitives.WriteInt16LittleEndian(dest.Slice(40 + i * 2, 2), i < Creatures.Length ? Creatures[i] : (short)-1);
            BinaryPrimitives.WriteInt16LittleEndian(dest.Slice(48 + i * 2, 2), i < ItemCounts.Length ? ItemCounts[i] : (short)0);
            BinaryPrimitives.WriteInt16LittleEndian(dest.Slice(56 + i * 2, 2), i < CreatureCounts.Length ? CreatureCounts[i] : (short)0);
            dest[64 + i] = (i < ExplorerStates.Length && ExplorerStates[i]) ? (byte)1 : (byte)0;
        }

        dest[68] = SpecialFlags;
        dest[69] = Status;
        dest[70] = Completed;

        for (int i = 0; i < QuestWireConstants.ObjectivesCount; i++)
        {
            BinaryPrimitives.WriteInt16LittleEndian(dest.Slice(72 + i * 2, 2), i < ItemCompletedCounts.Length ? ItemCompletedCounts[i] : (short)0);
            BinaryPrimitives.WriteInt16LittleEndian(dest.Slice(80 + i * 2, 2), i < CreatureCompletedCounts.Length ? CreatureCompletedCounts[i] : (short)0);
        }

        for (int i = 0; i < QuestWireConstants.RewardsCount; i++)
        {
            ItemEquipSlot rw = i < RewardItems.Length ? RewardItems[i] : ItemEquipSlot.Empty;
            QuestWireConstants.WriteItemData32(dest.Slice(88 + i * 32, 32), rw);
        }
    }
}

/// <summary>
/// S2C <c>MSG_NPC_QUEST</c> (opcode 10072, 16B framed / 12B payload).
/// </summary>
public readonly record struct MsgNpcQuest(uint NpcId, int QuestId, bool Available)
{
    public const int PayloadSize = 12;

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), NpcId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4, 4), QuestId);
        buf[8] = Available ? (byte)1 : (byte)0;
        return buf;
    }
}

/// <summary>
/// S2C <c>MSG_NPC_QUESTS</c> (opcode 10074).
/// </summary>
public readonly record struct MsgNpcQuests(uint NpcId, IReadOnlyList<NpcQuestEntry> Quests)
{
    public const int PayloadSize = 8 + QuestWireConstants.NpcTakeCount * 8;

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), NpcId);
        int count = Math.Min(QuestWireConstants.NpcTakeCount, Quests.Count);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4, 4), count);
        for (int i = 0; i < count; i++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(8 + i * 8, 4), Quests[i].QuestId);
            buf[8 + i * 8 + 4] = Quests[i].Available ? (byte)1 : (byte)0;
        }

        return buf;
    }
}

/// <summary>
/// S2C <c>MSG_NPC_QUESTREWARD</c> (opcode 10077).
/// </summary>
public readonly record struct MsgNpcQuestReward(uint NpcId, IReadOnlyList<int> QuestIds)
{
    public const int PayloadSize = 8 + QuestWireConstants.NpcTakeCount * 4;

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), NpcId);
        int count = Math.Min(QuestWireConstants.NpcTakeCount, QuestIds.Count);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4, 4), count);
        for (int i = 0; i < count; i++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(8 + i * 4, 4), QuestIds[i]);
        }

        return buf;
    }
}

/// <summary>
/// C2S / S2C <c>MSG_NPC_ACCEPTQUEST</c> (opcode 10079).
/// </summary>
public readonly record struct MsgNpcAcceptQuest(
    int NpcId,
    int RewardNpcId,
    int QuestId,
    int QuestSort,
    int SpecialFlags,
    short[] Items,
    short[] Creatures,
    short[] ItemCounts,
    short[] CreatureCounts,
    int Money,
    int Bijou,
    int RewardMoney,
    int RewardBijou,
    ItemEquipSlot[] RewardItems)
{
    public const int PayloadSize = 20 + 32 + 16 + 4 * 32; // 196B
    public const int V227PayloadSize = 72 + 8 * ItemEquipSlot.WireSize; // 648B (652B framed)

    public static MsgNpcAcceptQuest Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 12)
        {
            throw new InvalidDataException($"MSG_NPC_ACCEPTQUEST requires at least 12 payload bytes, got {payload.Length}.");
        }

        int npcId = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(0, 4));
        int rewardNpcId = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(4, 4));
        int questId = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(8, 4));
        int sort = payload.Length >= 16 ? BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(12, 4)) : 0;
        int flags = payload.Length >= 20 ? BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(16, 4)) : 0;

        return new MsgNpcAcceptQuest(
            npcId,
            rewardNpcId,
            questId,
            sort,
            flags,
            [],
            [],
            [],
            [],
            0,
            0,
            0,
            0,
            []);
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(0, 4), NpcId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4, 4), RewardNpcId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(8, 4), QuestId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(12, 4), QuestSort);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(16, 4), SpecialFlags);

        for (int i = 0; i < QuestWireConstants.ObjectivesCount; i++)
        {
            BinaryPrimitives.WriteInt16LittleEndian(buf.AsSpan(20 + i * 2, 2), i < Items.Length ? Items[i] : (short)-1);
            BinaryPrimitives.WriteInt16LittleEndian(buf.AsSpan(28 + i * 2, 2), i < Creatures.Length ? Creatures[i] : (short)-1);
            BinaryPrimitives.WriteInt16LittleEndian(buf.AsSpan(36 + i * 2, 2), i < ItemCounts.Length ? ItemCounts[i] : (short)0);
            BinaryPrimitives.WriteInt16LittleEndian(buf.AsSpan(44 + i * 2, 2), i < CreatureCounts.Length ? CreatureCounts[i] : (short)0);
        }

        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(52, 4), Money);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(56, 4), Bijou);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(60, 4), RewardMoney);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(64, 4), RewardBijou);

        for (int i = 0; i < QuestWireConstants.RewardsCount; i++)
        {
            ItemEquipSlot rw = i < RewardItems.Length ? RewardItems[i] : ItemEquipSlot.Empty;
            QuestWireConstants.WriteItemData32(buf.AsSpan(68 + i * 32, 32), rw);
        }

        return buf;
    }

    /// <summary>
    /// Serializes the 648-byte payload (652 bytes framed) expected by the v2.26.002 (v2.27)
    /// <c>MSG_NPC_QUESTVIEW</c> / <c>MSG_NPC_ACCEPTQUEST</c> handler (<c>0x4a379c</c>),
    /// which uses 72-byte <c>SEquip</c> reward slots starting at payload offset 72.
    /// </summary>
    public byte[] ToV227Payload()
    {
        byte[] buf = new byte[V227PayloadSize];
        ToPayload().AsSpan(0, 68).CopyTo(buf);

        for (int i = 0; i < QuestWireConstants.RewardsCount; i++)
        {
            ItemEquipSlot rw = i < RewardItems.Length ? RewardItems[i] : ItemEquipSlot.Empty;
            rw.WriteTo(buf.AsSpan(72 + i * ItemEquipSlot.WireSize, ItemEquipSlot.WireSize));
            ItemEquipSlot.Empty.WriteTo(buf.AsSpan(72 + (4 + i) * ItemEquipSlot.WireSize, ItemEquipSlot.WireSize));
        }

        return buf;
    }
}

/// <summary>
/// C2S / S2C <c>MSG_NPC_CANCELQUEST</c> (opcode 10080, 20B framed / 16B payload).
/// </summary>
public readonly record struct MsgNpcCancelQuest(int NpcId, int RewardNpcId, int QuestId, bool Available)
{
    public const int PayloadSize = 16;

    public static MsgNpcCancelQuest Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 12)
        {
            throw new InvalidDataException($"MSG_NPC_CANCELQUEST requires at least 12 payload bytes, got {payload.Length}.");
        }

        return new MsgNpcCancelQuest(
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(0, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(4, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(8, 4)),
            payload.Length >= 13 && payload[12] != 0);
    }

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(0, 4), NpcId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4, 4), RewardNpcId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(8, 4), QuestId);
        buf[12] = Available ? (byte)1 : (byte)0;
        return buf;
    }
}

/// <summary>
/// C2S <c>MSG_NPC_COMPLETEQUEST</c> (opcode 10081, 16B framed / 12B payload).
/// </summary>
public readonly record struct MsgNpcCompleteQuest(int RewardNpcId, int QuestId, int Choice)
{
    public const int PayloadSize = 12;

    public static MsgNpcCompleteQuest Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 8)
        {
            throw new InvalidDataException($"MSG_NPC_COMPLETEQUEST requires at least 8 payload bytes, got {payload.Length}.");
        }

        return new MsgNpcCompleteQuest(
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(0, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(4, 4)),
            payload.Length >= 12 ? BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(8, 4)) : 0);
    }
}

/// <summary>
/// S2C <c>MSG_NPC_REWARDQUEST</c> (opcode 10083, 76B framed / 72B payload).
/// </summary>
public readonly record struct MsgNpcRewardQuest(
    int NpcId,
    int RewardNpcId,
    int QuestId,
    int Choice,
    int Credit,
    int Contribute,
    int Exp,
    int SkillPoint,
    int SpecialFlags,
    int SpecialCount,
    ItemEquipSlot Item)
{
    public const int PayloadSize = 40 + 32;

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(0, 4), NpcId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4, 4), RewardNpcId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(8, 4), QuestId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(12, 4), Choice);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(16, 4), Credit);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(20, 4), Contribute);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(24, 4), Exp);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(28, 4), SkillPoint);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(32, 4), SpecialFlags);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(36, 4), SpecialCount);
        QuestWireConstants.WriteItemData32(buf.AsSpan(40, 32), Item);
        return buf;
    }
}

/// <summary>
/// S2C <c>MSG_NPC_QUESTKILLORCAST</c> (opcode 10084, 20B framed / 16B payload).
/// </summary>
public readonly record struct MsgNpcQuestKillOrCast(int QuestId, int NpcId, bool Kill, int KillOrCastId)
{
    public const int PayloadSize = 16;

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(0, 4), QuestId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4, 4), NpcId);
        buf[8] = Kill ? (byte)1 : (byte)0;
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(12, 4), KillOrCastId);
        return buf;
    }
}

/// <summary>
/// S2C <c>MSG_PLAYER_ACCEPTQUESTS</c> (opcode 10085).
/// </summary>
public readonly record struct MsgPlayerAcceptQuests(IReadOnlyList<PlayerQuestWire> Quests)
{
    public const int MaxPerPacket = 3;
    public const int PayloadSize = 4 + MaxPerPacket * PlayerQuestWire.WireSize;

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        int count = Math.Min(MaxPerPacket, Quests.Count);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(0, 4), count);
        for (int i = 0; i < count; i++)
        {
            Quests[i].WriteTo(buf.AsSpan(4 + i * PlayerQuestWire.WireSize, PlayerQuestWire.WireSize));
        }

        return buf;
    }
}

/// <summary>
/// S2C <c>MSG_FINDQUESTRESULT</c> (opcode 10087).
/// </summary>
public readonly record struct MsgFindQuestResult(IReadOnlyList<short> QuestIds)
{
    public const int PayloadSize = 4 + QuestWireConstants.FindCount * 2;

    public byte[] ToPayload()
    {
        byte[] buf = new byte[PayloadSize];
        int count = Math.Min(QuestWireConstants.FindCount, QuestIds.Count);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(0, 4), count);
        for (int i = 0; i < count; i++)
        {
            BinaryPrimitives.WriteInt16LittleEndian(buf.AsSpan(4 + i * 2, 2), QuestIds[i]);
        }

        return buf;
    }
}
