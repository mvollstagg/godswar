using System.Buffers.Binary;
using System.Text;

namespace GodsWar.Protocol.Packets;

/// <summary>
/// 88-byte (<c>0x58</c>) Mall item entry for <c>MSG_MALL_ITEMDATA</c> (10172) and
/// <c>MSG_MALL_ITEMDATA_2</c> (10173).
/// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x50bea0</c> and <c>0x50d720</c>.
/// </summary>
public readonly record struct MallItemEntry(
    int BaseId,
    int Append0,
    int Append1,
    int Append2,
    int Append3,
    int Append4,
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
    int Discount,
    byte BuyTypeNum,
    byte New,
    byte Hot)
{
    public const int Size = 88; // 0x58

    public void WriteTo(Span<byte> dest)
    {
        dest[..Size].Clear();
        BinaryPrimitives.WriteInt32LittleEndian(dest[0..4], BaseId);
        BinaryPrimitives.WriteInt32LittleEndian(dest[4..8], Append0);
        BinaryPrimitives.WriteInt32LittleEndian(dest[8..12], Append1);
        BinaryPrimitives.WriteInt32LittleEndian(dest[12..16], Append2);
        BinaryPrimitives.WriteInt32LittleEndian(dest[16..20], Append3);
        BinaryPrimitives.WriteInt32LittleEndian(dest[20..24], Append4);
        dest[24] = AppendLevel;
        dest[25] = BaseLevel;
        dest[26] = Binding;
        dest[27] = Overlap;

        // Offset +0x40 (64) = ebx + 0x28 (where ebx = dest + 0x18)
        dest[64] = Sell;
        dest[65] = BuyType0;
        dest[66] = BuyType1;
        dest[67] = BuyType2;
        BinaryPrimitives.WriteUInt32LittleEndian(dest[68..72], BuyConsume0);
        BinaryPrimitives.WriteUInt32LittleEndian(dest[72..76], BuyConsume1);
        BinaryPrimitives.WriteUInt32LittleEndian(dest[76..80], BuyConsume2);
        BinaryPrimitives.WriteInt32LittleEndian(dest[80..84], Discount);
        dest[84] = BuyTypeNum;
        dest[85] = New;
        dest[86] = Hot;
        dest[87] = 0;
    }

    public static MallItemEntry ReadFrom(ReadOnlySpan<byte> src)
    {
        if (src.Length < Size)
        {
            return new MallItemEntry(-1, -1, -1, -1, -1, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 100, 0, 0, 0);
        }

        return new MallItemEntry(
            BinaryPrimitives.ReadInt32LittleEndian(src[0..4]),
            BinaryPrimitives.ReadInt32LittleEndian(src[4..8]),
            BinaryPrimitives.ReadInt32LittleEndian(src[8..12]),
            BinaryPrimitives.ReadInt32LittleEndian(src[12..16]),
            BinaryPrimitives.ReadInt32LittleEndian(src[16..20]),
            BinaryPrimitives.ReadInt32LittleEndian(src[20..24]),
            src[24],
            src[25],
            src[26],
            src[27],
            src[64],
            src[65],
            src[66],
            src[67],
            BinaryPrimitives.ReadUInt32LittleEndian(src[68..72]),
            BinaryPrimitives.ReadUInt32LittleEndian(src[72..76]),
            BinaryPrimitives.ReadUInt32LittleEndian(src[76..80]),
            BinaryPrimitives.ReadInt32LittleEndian(src[80..84]),
            src[84],
            src[85],
            src[86]);
    }
}

/// <summary>
/// <c>MSG_MALL_ITEMDATA</c> (10172) and <c>MSG_MALL_ITEMDATA_2</c> (10173).
/// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x4896bb</c> and <c>0x4896f2</c>.
/// </summary>
public readonly record struct MsgMallItemData(
    byte TabIndex,
    byte MaxTabIndex,
    byte Sell,
    IReadOnlyList<MallItemEntry> Items)
{
    public const int MaxItems = 16;
    public const int PayloadSize = 4 + MaxItems * MallItemEntry.Size; // 1412B payload (1416B packet)

    public byte[] ToPayload()
    {
        int count = Math.Min(MaxItems, Items?.Count ?? 0);
        var buf = new byte[PayloadSize];
        buf[0] = TabIndex;
        buf[1] = MaxTabIndex;
        buf[2] = (byte)count;
        buf[3] = Sell;

        for (int i = 0; i < MaxItems; i++)
        {
            MallItemEntry entry = i < count
                ? Items![i]
                : new MallItemEntry(-1, -1, -1, -1, -1, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 100, 0, 0, 0);
            entry.WriteTo(buf.AsSpan(4 + i * MallItemEntry.Size, MallItemEntry.Size));
        }

        return buf;
    }

    public static MsgMallItemData Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 4)
        {
            return new MsgMallItemData(0, 0, 0, []);
        }

        byte tab = payload[0];
        byte maxTab = payload[1];
        byte count = Math.Min((byte)MaxItems, payload[2]);
        byte sell = payload[3];
        var list = new List<MallItemEntry>(count);
        for (int i = 0; i < count; i++)
        {
            int off = 4 + i * MallItemEntry.Size;
            if (off + MallItemEntry.Size > payload.Length)
            {
                break;
            }
            list.Add(MallItemEntry.ReadFrom(payload.Slice(off, MallItemEntry.Size)));
        }

        return new MsgMallItemData(tab, maxTab, sell, list);
    }
}

/// <summary>
/// <c>MSG_MALL_SELL</c> (10174) and <c>MSG_ASSOCIATION_SELL</c> (10175).
/// C2S is 20B payload (24B packet); S2C is 16B payload (20B packet).
/// </summary>
public readonly record struct MsgMallSell(int Index, int Num, int Count, int Base, byte BuyType = 3)
{
    public const int PayloadSize = 16;

    public static MsgMallSell Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 16)
        {
            return default;
        }

        byte buyType = payload.Length >= 17 ? payload[16] : (byte)3;
        return new MsgMallSell(
            BinaryPrimitives.ReadInt32LittleEndian(payload[0..4]),
            BinaryPrimitives.ReadInt32LittleEndian(payload[4..8]),
            BinaryPrimitives.ReadInt32LittleEndian(payload[8..12]),
            BinaryPrimitives.ReadInt32LittleEndian(payload[12..16]),
            buyType);
    }

    public byte[] ToPayload()
    {
        var buf = new byte[PayloadSize];
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(0, 4), Index);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4, 4), Num);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(8, 4), Count);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(12, 4), Base);
        return buf;
    }
}

public readonly record struct AssociationDiscountEntry(byte Level, int StoneDiscount, int GloryDiscount);

/// <summary>
/// <c>MSG_ASSOCIATIONDISCOUNT</c> (10176): 364B payload (368B packet).
/// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x4897a6</c> and <c>0x50d628</c>.
/// </summary>
public readonly record struct MsgAssociationDiscount(IReadOnlyList<AssociationDiscountEntry> Entries)
{
    public const int MaxEntries = 30;
    public const int EntryStride = 12;
    public const int PayloadSize = 4 + MaxEntries * EntryStride; // 364B payload

    public byte[] ToPayload()
    {
        var buf = new byte[PayloadSize];
        int count = Math.Min(MaxEntries, Entries?.Count ?? 0);
        buf[0] = (byte)count;
        for (int i = 0; i < count; i++)
        {
            int off = 4 + i * EntryStride;
            buf[off] = Entries![i].Level;
            BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(off + 4, 4), Entries[i].StoneDiscount);
            BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(off + 8, 4), Entries[i].GloryDiscount);
        }
        return buf;
    }

    public static MsgAssociationDiscount Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 4)
        {
            return new MsgAssociationDiscount([]);
        }

        int count = Math.Clamp((int)payload[0], 0, MaxEntries);
        var entries = new List<AssociationDiscountEntry>(count);
        for (int i = 0; i < count; i++)
        {
            int off = 4 + i * EntryStride;
            if (off + EntryStride > payload.Length)
            {
                break;
            }
            entries.Add(new AssociationDiscountEntry(
                payload[off],
                BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(off + 4, 4)),
                BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(off + 8, 4))));
        }

        return new MsgAssociationDiscount(entries);
    }
}

/// <summary>
/// <c>MSG_GOLD_BOX</c> (10112): 12B payload (16B packet).
/// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x4dc760</c> and <c>MsgDefine.h</c>.
/// </summary>
public readonly record struct MsgGoldBox(byte Type, uint GoldBoxId, byte Iter)
{
    public const int PayloadSize = 12;

    public static MsgGoldBox Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 9)
        {
            return default;
        }

        return new MsgGoldBox(
            payload[0],
            BinaryPrimitives.ReadUInt32LittleEndian(payload[4..8]),
            payload[8]);
    }

    public byte[] ToPayload()
    {
        var buf = new byte[PayloadSize];
        buf[0] = Type;
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(4, 4), GoldBoxId);
        buf[8] = Iter;
        return buf;
    }
}

/// <summary>
/// <c>MSG_GOLD_BOX_RETURN</c> (10114): 72B payload (76B packet).
/// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x487b95</c> and <c>MsgDefine.h</c>.
/// </summary>
public readonly record struct MsgGoldBoxReturn(byte Info, uint[] DialItems)
{
    public const int DialItemCount = 17; // 16 dial items + 1 default item
    public const int PayloadSize = 4 + DialItemCount * 4; // 72B

    public byte[] ToPayload()
    {
        var buf = new byte[PayloadSize];
        buf[0] = Info;
        if (DialItems is not null)
        {
            int count = Math.Min(DialItemCount, DialItems.Length);
            for (int i = 0; i < count; i++)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(4 + i * 4, 4), DialItems[i]);
            }
        }
        return buf;
    }

    public static MsgGoldBoxReturn Parse(ReadOnlySpan<byte> payload)
    {
        var items = new uint[DialItemCount];
        if (payload.Length < 4)
        {
            return new MsgGoldBoxReturn(0, items);
        }

        byte info = payload[0];
        for (int i = 0; i < DialItemCount && 4 + (i + 1) * 4 <= payload.Length; i++)
        {
            items[i] = BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(4 + i * 4, 4));
        }

        return new MsgGoldBoxReturn(info, items);
    }
}

/// <summary>
/// <c>MSG_GOLD_BOX_INDIAL</c> (10115): 76B payload (80B packet).
/// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x487c12</c>.
/// </summary>
public readonly record struct MsgGoldBoxInDial(byte Info, uint DefaultItemId, uint[] DialItems)
{
    public const int DialCount = 16;
    public const int PayloadSize = 12 + DialCount * 4; // 76B

    public byte[] ToPayload()
    {
        var buf = new byte[PayloadSize];
        buf[0] = Info;
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(4, 4), DefaultItemId);
        if (DialItems is not null)
        {
            int count = Math.Min(DialCount, DialItems.Length);
            for (int i = 0; i < count; i++)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(12 + i * 4, 4), DialItems[i]);
            }
        }
        return buf;
    }
}

/// <summary>
/// <c>MSG_LEARN</c> (10046): 12B payload (16B packet).
/// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x490474</c>.
/// </summary>
public readonly record struct MsgLearn(uint UiId, byte SkillType, byte Kinds, bool IsSucceed)
{
    public const int PayloadSize = 12;

    public static MsgLearn Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 6)
        {
            return default;
        }

        bool succeed = payload.Length >= 12 && BinaryPrimitives.ReadInt32LittleEndian(payload[8..12]) != 0;
        return new MsgLearn(
            BinaryPrimitives.ReadUInt32LittleEndian(payload[0..4]),
            payload[4],
            payload[5],
            succeed);
    }

    public byte[] ToPayload()
    {
        var buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), UiId);
        buf[4] = SkillType;
        buf[5] = Kinds;
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(8, 4), IsSucceed ? 1 : 0);
        return buf;
    }
}

/// <summary>
/// <c>MSG_SKILL_UPGRADE</c> (10047): 20B payload (24B packet).
/// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x4908d6</c> and <c>0x5694e0</c>.
/// </summary>
public readonly record struct MsgSkillUpgrade(
    uint PlayerUiId,
    uint SkillId,
    uint CurrentSkillGrade,
    uint NextSkillGradePoint,
    uint SurplusPoint)
{
    public const int PayloadSize = 20;

    public static MsgSkillUpgrade Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 8)
        {
            return default;
        }

        return new MsgSkillUpgrade(
            BinaryPrimitives.ReadUInt32LittleEndian(payload[0..4]),
            BinaryPrimitives.ReadUInt32LittleEndian(payload[4..8]),
            payload.Length >= 12 ? BinaryPrimitives.ReadUInt32LittleEndian(payload[8..12]) : 0,
            payload.Length >= 16 ? BinaryPrimitives.ReadUInt32LittleEndian(payload[12..16]) : 0,
            payload.Length >= 20 ? BinaryPrimitives.ReadUInt32LittleEndian(payload[16..20]) : 0);
    }

    public byte[] ToPayload()
    {
        var buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), PlayerUiId);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(4, 4), SkillId);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(8, 4), CurrentSkillGrade);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(12, 4), NextSkillGradePoint);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(16, 4), SurplusPoint);
        return buf;
    }
}

/// <summary>
/// <c>MSG_SKILLPOINT_UPGRADE</c> (10159): 8B payload (12B packet).
/// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x490634</c>.
/// </summary>
public readonly record struct MsgSkillPointUpgrade(uint PlayerUiId, uint SkillPoint)
{
    public const int PayloadSize = 8;

    public byte[] ToPayload()
    {
        var buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), PlayerUiId);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(4, 4), SkillPoint);
        return buf;
    }

    public static MsgSkillPointUpgrade Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 8)
        {
            return default;
        }

        return new MsgSkillPointUpgrade(
            BinaryPrimitives.ReadUInt32LittleEndian(payload[0..4]),
            BinaryPrimitives.ReadUInt32LittleEndian(payload[4..8]));
    }
}

/// <summary>
/// <c>MSG_SKILL_BACKUP</c> (10164): 136B payload (140B packet).
/// Ground truth: <c>MsgDefine.h</c> (<c>SKILLBACKUPCOUNT = 16</c>).
/// </summary>
public readonly record struct MsgSkillBackup(
    int[] SkillIds,
    int[] ItemIds,
    int RSkillId,
    int RItemId)
{
    public const int BackupCount = 16;
    public const int PayloadSize = BackupCount * 4 * 2 + 8; // 136B

    public static MsgSkillBackup Parse(ReadOnlySpan<byte> payload)
    {
        var skillIds = new int[BackupCount];
        var itemIds = new int[BackupCount];
        Array.Fill(skillIds, -1);
        Array.Fill(itemIds, -1);

        if (payload.Length < PayloadSize)
        {
            return new MsgSkillBackup(skillIds, itemIds, -1, -1);
        }

        for (int i = 0; i < BackupCount; i++)
        {
            skillIds[i] = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(i * 4, 4));
            itemIds[i] = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(64 + i * 4, 4));
        }

        int rSkill = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(128, 4));
        int rItem = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(132, 4));
        return new MsgSkillBackup(skillIds, itemIds, rSkill, rItem);
    }

    public byte[] ToPayload()
    {
        var buf = new byte[PayloadSize];
        for (int i = 0; i < BackupCount; i++)
        {
            int s = SkillIds is not null && i < SkillIds.Length ? SkillIds[i] : -1;
            int it = ItemIds is not null && i < ItemIds.Length ? ItemIds[i] : -1;
            BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(i * 4, 4), s);
            BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(64 + i * 4, 4), it);
        }
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(128, 4), RSkillId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(132, 4), RItemId);
        return buf;
    }
}

/// <summary>
/// <c>MSG_STALL</c> (10059): 104B payload (108B packet).
/// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x4892fe</c>, <c>0x460b25</c>, <c>0x5766a0</c>.
/// </summary>
public readonly record struct MsgStall(uint UiId, bool State, string Text, string SellerName)
{
    public const int PayloadSize = 104; // 108B packet (0x6c)

    public static MsgStall Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 6)
        {
            return new MsgStall(0, false, string.Empty, string.Empty);
        }

        uint uiId = BinaryPrimitives.ReadUInt32LittleEndian(payload[0..4]);
        bool state = payload[4] != 0;
        string text = string.Empty;
        if (payload.Length >= 70)
        {
            ReadOnlySpan<byte> textSpan = payload.Slice(6, 64);
            int nullOff = 0;
            while (nullOff + 1 < textSpan.Length && (textSpan[nullOff] != 0 || textSpan[nullOff + 1] != 0))
            {
                nullOff += 2;
            }
            text = Encoding.Unicode.GetString(textSpan[..nullOff]);
        }

        string seller = payload.Length >= 102
            ? FixedStringCodec.ReadFixedString(payload.Slice(70, 32))
            : string.Empty;

        return new MsgStall(uiId, state, text, seller);
    }

    public byte[] ToPayload()
    {
        var buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), UiId);
        buf[4] = State ? (byte)1 : (byte)0;
        if (!string.IsNullOrEmpty(Text))
        {
            byte[] wbytes = Encoding.Unicode.GetBytes(Text);
            int copyLen = Math.Min(62, wbytes.Length);
            wbytes.AsSpan(0, copyLen).CopyTo(buf.AsSpan(6, 64));
        }
        FixedStringCodec.WriteFixedString(buf.AsSpan(70, 32), SellerName ?? string.Empty);
        return buf;
    }
}

/// <summary>
/// <c>MSG_STALLADDITEM</c> (10060): 36B payload (40B packet).
/// Note 4B alignment padding at payload[24..27] (<c>pkt[28..31]</c>) before <c>__int64 StallID</c> at payload[28..35] (<c>pkt[32..39]</c>).
/// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x489412</c> and <c>0x575bb0</c>.
/// </summary>
public readonly record struct MsgStallAddItem(
    int Index,
    int Pocket,
    int BagIndex,
    int BagNum,
    int Money,
    int Bijou,
    long StallId)
{
    public const int PayloadSize = 36; // 40B packet (0x28)

    public static MsgStallAddItem Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 24)
        {
            return default;
        }

        long stallId = payload.Length >= 36
            ? BinaryPrimitives.ReadInt64LittleEndian(payload.Slice(28, 8))
            : payload.Length >= 32
                ? BinaryPrimitives.ReadInt64LittleEndian(payload.Slice(24, 8))
                : 0L;

        return new MsgStallAddItem(
            BinaryPrimitives.ReadInt32LittleEndian(payload[0..4]),
            BinaryPrimitives.ReadInt32LittleEndian(payload[4..8]),
            BinaryPrimitives.ReadInt32LittleEndian(payload[8..12]),
            BinaryPrimitives.ReadInt32LittleEndian(payload[12..16]),
            BinaryPrimitives.ReadInt32LittleEndian(payload[16..20]),
            BinaryPrimitives.ReadInt32LittleEndian(payload[20..24]),
            stallId);
    }

    public byte[] ToPayload()
    {
        var buf = new byte[PayloadSize];
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(0, 4), Index);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4, 4), Pocket);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(8, 4), BagIndex);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(12, 4), BagNum);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(16, 4), Money);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(20, 4), Bijou);
        BinaryPrimitives.WriteInt64LittleEndian(buf.AsSpan(28, 8), StallId);
        return buf;
    }
}

/// <summary>
/// <c>MSG_STALLDELITEM</c> (10061): 8B payload (12B packet).
/// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x489462</c> and <c>0x576d10</c>.
/// </summary>
public readonly record struct MsgStallDelItem(int Index, int Pocket)
{
    public const int PayloadSize = 8;

    public static MsgStallDelItem Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 8)
        {
            return default;
        }

        return new MsgStallDelItem(
            BinaryPrimitives.ReadInt32LittleEndian(payload[0..4]),
            BinaryPrimitives.ReadInt32LittleEndian(payload[4..8]));
    }

    public byte[] ToPayload()
    {
        var buf = new byte[PayloadSize];
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(0, 4), Index);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(4, 4), Pocket);
        return buf;
    }
}

/// <summary>
/// <c>MSG_STALLITEM</c> (10062): 132B payload (136B / <c>0x88</c> packet).
/// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x4893ae</c> (S2C) and <c>0x444270</c> (C2S).
/// </summary>
public readonly record struct MsgStallItem(
    long StallId,
    string SellerName,
    uint UiId,
    byte Pocket,
    byte Index,
    int Money,
    int Bijou,
    ItemEquipSlot Item)
{
    public const int PayloadSize = 132; // 136B packet (0x88)

    public static MsgStallItem Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 48)
        {
            return new MsgStallItem(0, string.Empty, 0, 0, 0, 0, 0, ItemEquipSlot.Empty);
        }

        long stallId = BinaryPrimitives.ReadInt64LittleEndian(payload.Slice(4, 8));
        string seller = FixedStringCodec.ReadFixedString(payload.Slice(12, 32));
        uint uiId = BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(44, 4));
        byte pocket = payload.Length >= 49 ? payload[48] : (byte)0;
        byte index = payload.Length >= 50 ? payload[49] : (byte)0;
        int money = payload.Length >= 56 ? BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(52, 4)) : 0;
        int bijou = payload.Length >= 60 ? BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(56, 4)) : 0;
        ItemEquipSlot item = payload.Length >= 132
            ? ItemEquipSlot.ReadFrom(payload.Slice(60, ItemEquipSlot.WireSize))
            : ItemEquipSlot.Empty;

        return new MsgStallItem(stallId, seller, uiId, pocket, index, money, bijou, item);
    }

    public byte[] ToPayload()
    {
        var buf = new byte[PayloadSize];
        BinaryPrimitives.WriteInt64LittleEndian(buf.AsSpan(4, 8), StallId);
        FixedStringCodec.WriteFixedString(buf.AsSpan(12, 32), SellerName ?? string.Empty);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(44, 4), UiId);
        buf[48] = Pocket;
        buf[49] = Index;
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(52, 4), Money);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(56, 4), Bijou);
        Item.WriteTo(buf.AsSpan(60, ItemEquipSlot.WireSize));
        return buf;
    }
}

/// <summary>
/// <c>MSG_STALLBUYITEM</c> (10063): 92B payload (96B / <c>0x60</c> packet).
/// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x4894a4</c> and <c>0x5750b0</c>.
/// </summary>
public readonly record struct MsgStallBuyItem(
    uint SellUiId,
    uint BuyUiId,
    string SellerName,
    string CustomName,
    long StallId,
    int Pocket,
    int Index)
{
    public const int PayloadSize = 92; // 96B packet (0x60)

    public static MsgStallBuyItem Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 88)
        {
            return new MsgStallBuyItem(0, 0, string.Empty, string.Empty, 0, 0, 0);
        }

        uint sellId = BinaryPrimitives.ReadUInt32LittleEndian(payload[0..4]);
        uint buyId = BinaryPrimitives.ReadUInt32LittleEndian(payload[4..8]);
        string seller = FixedStringCodec.ReadFixedString(payload.Slice(8, 32));
        string custom = FixedStringCodec.ReadFixedString(payload.Slice(40, 32));
        long stallId = payload.Length >= 92
            ? BinaryPrimitives.ReadInt64LittleEndian(payload.Slice(76, 8))
            : BinaryPrimitives.ReadInt64LittleEndian(payload.Slice(72, 8));
        int pocket = payload.Length >= 92
            ? BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(84, 4))
            : BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(80, 4));
        int index = payload.Length >= 92
            ? BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(88, 4))
            : BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(84, 4));

        return new MsgStallBuyItem(sellId, buyId, seller, custom, stallId, pocket, index);
    }

    public byte[] ToPayload()
    {
        var buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), SellUiId);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(4, 4), BuyUiId);
        FixedStringCodec.WriteFixedString(buf.AsSpan(8, 32), SellerName ?? string.Empty);
        FixedStringCodec.WriteFixedString(buf.AsSpan(40, 32), CustomName ?? string.Empty);
        BinaryPrimitives.WriteInt64LittleEndian(buf.AsSpan(76, 8), StallId);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(84, 4), Pocket);
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(88, 4), Index);
        return buf;
    }
}

/// <summary>
/// 144-byte (<c>0x90</c>) Pet entry for <c>MSG_PET_LIST</c> (10230) and <c>MSG_PET_ADD</c> (10243).
/// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x5c5020</c>.
/// </summary>
public readonly record struct PetDataEntry(
    int PetId,
    string Name,
    byte TemplateId,
    byte Level,
    byte Quality,
    byte Sex,
    byte Savvy,
    byte State,
    byte Loyalty,
    byte Satiety,
    byte Rebirth,
    ushort Stamina,
    ushort MaxStamina,
    int CurExp,
    int MaxExp,
    int Attack,
    int Defend,
    int MagicAttack,
    int MagicDefend,
    ushort Hit)
{
    public const int Size = 144; // 0x90

    public void WriteTo(Span<byte> dest)
    {
        dest[..Size].Clear();
        BinaryPrimitives.WriteInt32LittleEndian(dest[0..4], PetId);
        FixedStringCodec.WriteFixedString(dest.Slice(4, 32), Name ?? "ZodiacPet");
        dest[36] = TemplateId;
        dest[37] = Level;
        dest[38] = Quality;
        dest[39] = Sex;
        dest[40] = Savvy;
        dest[46] = State;
        dest[47] = Loyalty;
        dest[48] = Satiety;
        dest[49] = Rebirth;
        BinaryPrimitives.WriteUInt16LittleEndian(dest.Slice(62, 2), Stamina);
        BinaryPrimitives.WriteUInt16LittleEndian(dest.Slice(64, 2), MaxStamina);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(68, 4), CurExp);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(72, 4), MaxExp);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(76, 4), Attack);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(80, 4), Defend);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(84, 4), MagicAttack);
        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(88, 4), MagicDefend);
        BinaryPrimitives.WriteUInt16LittleEndian(dest.Slice(92, 2), Hit);
        BinaryPrimitives.WriteUInt16LittleEndian(dest.Slice(132, 2), 100); // Growth
    }

    public static PetDataEntry ReadFrom(ReadOnlySpan<byte> src)
    {
        if (src.Length < Size)
        {
            return default;
        }

        return new PetDataEntry(
            BinaryPrimitives.ReadInt32LittleEndian(src[0..4]),
            FixedStringCodec.ReadFixedString(src.Slice(4, 32)),
            src[36],
            src[37],
            src[38],
            src[39],
            src[40],
            src[46],
            src[47],
            src[48],
            src[49],
            BinaryPrimitives.ReadUInt16LittleEndian(src.Slice(62, 2)),
            BinaryPrimitives.ReadUInt16LittleEndian(src.Slice(64, 2)),
            BinaryPrimitives.ReadInt32LittleEndian(src.Slice(68, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(src.Slice(72, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(src.Slice(76, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(src.Slice(80, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(src.Slice(84, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(src.Slice(88, 4)),
            BinaryPrimitives.ReadUInt16LittleEndian(src.Slice(92, 2)));
    }
}

/// <summary>
/// <c>MSG_PET_LIST</c> (10230): 4B header + <c>Count * 144B</c>.
/// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x5bcca0</c>.
/// </summary>
public readonly record struct MsgPetList(byte ActiveState, IReadOnlyList<PetDataEntry> Pets)
{
    public byte[] ToPayload()
    {
        int count = Math.Min(16, Pets?.Count ?? 0);
        var buf = new byte[4 + count * PetDataEntry.Size];
        buf[0] = ActiveState;
        buf[1] = (byte)count;
        for (int i = 0; i < count; i++)
        {
            Pets![i].WriteTo(buf.AsSpan(4 + i * PetDataEntry.Size, PetDataEntry.Size));
        }
        return buf;
    }

    public static MsgPetList Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 4)
        {
            return new MsgPetList(0, []);
        }

        byte activeState = payload[0];
        int count = payload[1];
        var pets = new List<PetDataEntry>(count);
        for (int i = 0; i < count; i++)
        {
            int off = 4 + i * PetDataEntry.Size;
            if (off + PetDataEntry.Size > payload.Length)
            {
                break;
            }
            pets.Add(PetDataEntry.ReadFrom(payload.Slice(off, PetDataEntry.Size)));
        }

        return new MsgPetList(activeState, pets);
    }
}

/// <summary>
/// C2S Pet command (10231 Summon, 10232 Recall, 10233 Release, 10234 Feed): 4B payload (8B packet).
/// </summary>
public readonly record struct MsgPetCommand(int PetId)
{
    public const int PayloadSize = 4;

    public static MsgPetCommand Parse(ReadOnlySpan<byte> payload)
        => payload.Length >= 4 ? new MsgPetCommand(BinaryPrimitives.ReadInt32LittleEndian(payload[0..4])) : default;

    public byte[] ToPayload()
    {
        var buf = new byte[PayloadSize];
        BinaryPrimitives.WriteInt32LittleEndian(buf, PetId);
        return buf;
    }
}

/// <summary>
/// C2S <c>MSG_PET_RENAME</c> (10235): 20B payload (24B packet).
/// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x5bcac7</c>.
/// </summary>
public readonly record struct MsgPetRename(int PetId, string NewName)
{
    public const int PayloadSize = 20;

    public static MsgPetRename Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 5)
        {
            return new MsgPetRename(0, string.Empty);
        }

        int petId = BinaryPrimitives.ReadInt32LittleEndian(payload[0..4]);
        string name = FixedStringCodec.ReadFixedString(payload[4..Math.Min(20, payload.Length)]);
        return new MsgPetRename(petId, name);
    }

    public byte[] ToPayload()
    {
        var buf = new byte[PayloadSize];
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(0, 4), PetId);
        FixedStringCodec.WriteFixedString(buf.AsSpan(4, 16), NewName ?? string.Empty);
        return buf;
    }
}

/// <summary>
/// S2C <c>MSG_PET_ACTION</c> (10237): 8B payload (12B packet).
/// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x5bcf80</c> (<c>Action: 1=Summon, 2=Recall, 3=Release</c>).
/// </summary>
public readonly record struct MsgPetAction(int PetId, byte Action)
{
    public const int PayloadSize = 8;

    public byte[] ToPayload()
    {
        var buf = new byte[PayloadSize];
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(0, 4), PetId);
        buf[4] = Action;
        return buf;
    }

    public static MsgPetAction Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 5)
        {
            return default;
        }
        return new MsgPetAction(BinaryPrimitives.ReadInt32LittleEndian(payload[0..4]), payload[4]);
    }
}

/// <summary>
/// S2C <c>MSG_PET_STATE</c> (10238): 12B payload (16B packet).
/// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x5bd020</c>.
/// </summary>
public readonly record struct MsgPetState(int PetId, byte Satiety, byte Loyalty, ushort Stamina)
{
    public const int PayloadSize = 12;

    public byte[] ToPayload()
    {
        var buf = new byte[PayloadSize];
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(0, 4), PetId);
        buf[5] = Satiety;
        buf[7] = Loyalty;
        BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(10, 2), Stamina);
        return buf;
    }

    public static MsgPetState Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 12)
        {
            return default;
        }
        return new MsgPetState(
            BinaryPrimitives.ReadInt32LittleEndian(payload[0..4]),
            payload[5],
            payload[7],
            BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(10, 2)));
    }
}

/// <summary>
/// S2C <c>MSG_PET_UPDATE</c> (10240): 20B payload (24B packet).
/// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x5bd7e0</c>.
/// </summary>
public readonly record struct MsgPetUpdate(int PetId, byte Level, byte Quality, byte Rebirth, string Name)
{
    public const int PayloadSize = 20;

    public byte[] ToPayload()
    {
        var buf = new byte[PayloadSize];
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(0, 4), PetId);
        buf[4] = Level;
        buf[5] = Quality;
        buf[6] = Rebirth;
        FixedStringCodec.WriteFixedString(buf.AsSpan(8, 12), Name ?? string.Empty);
        return buf;
    }

    public static MsgPetUpdate Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 20)
        {
            return new MsgPetUpdate(0, 0, 0, 0, string.Empty);
        }
        return new MsgPetUpdate(
            BinaryPrimitives.ReadInt32LittleEndian(payload[0..4]),
            payload[4],
            payload[5],
            payload[6],
            FixedStringCodec.ReadFixedString(payload.Slice(8, 12)));
    }
}

/// <summary>
/// S2C <c>MSG_PET_VIEW</c> (10241): 8B payload (12B packet).
/// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x5bdba0</c>.
/// </summary>
public readonly record struct MsgPetView(int PetId, uint OwnerUiId)
{
    public const int PayloadSize = 8;

    public byte[] ToPayload()
    {
        var buf = new byte[PayloadSize];
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(0, 4), PetId);
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(4, 4), OwnerUiId);
        return buf;
    }

    public static MsgPetView Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 8)
        {
            return default;
        }
        return new MsgPetView(
            BinaryPrimitives.ReadInt32LittleEndian(payload[0..4]),
            BinaryPrimitives.ReadUInt32LittleEndian(payload[4..8]));
    }
}

/// <summary>
/// C2S <c>MSG_AFK_REQUEST</c> (10295): 2B payload (6B packet).
/// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x45d190</c>.
/// </summary>
public readonly record struct MsgAfkRequest(byte State)
{
    public const int PayloadSize = 2;

    public static MsgAfkRequest Parse(ReadOnlySpan<byte> payload)
        => payload.Length >= 1 ? new MsgAfkRequest(payload[0]) : default;

    public byte[] ToPayload() => [State, 0];
}

/// <summary>
/// S2C <c>MSG_AFK_STATE</c> (10296 / 10297 / 10298): 4B payload (8B packet).
/// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x45d020</c>.
/// </summary>
public readonly record struct MsgAfkState(byte State)
{
    public const int PayloadSize = 4;

    public byte[] ToPayload() => [State, 0, 0, 0];

    public static MsgAfkState Parse(ReadOnlySpan<byte> payload)
        => payload.Length >= 1 ? new MsgAfkState(payload[0]) : default;
}

/// <summary>
/// S2C <c>MSG_AFK_BROADCAST</c> (10299): 8B payload (12B packet).
/// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x45d131</c>.
/// </summary>
public readonly record struct MsgAfkBroadcast(uint PlayerUiId, byte State)
{
    public const int PayloadSize = 8;

    public byte[] ToPayload()
    {
        var buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), PlayerUiId);
        buf[4] = State;
        return buf;
    }

    public static MsgAfkBroadcast Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 5)
        {
            return default;
        }
        return new MsgAfkBroadcast(BinaryPrimitives.ReadUInt32LittleEndian(payload[0..4]), payload[4]);
    }
}

/// <summary>
/// <c>MSG_PRENTICE</c> (10227): 68B payload (72B / <c>0x48</c> packet).
/// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x53ebf0</c> and <c>0x53f990</c>.
/// </summary>
public readonly record struct MsgPrentice(int ActionType, string MasterName, string PrenticeName)
{
    public const int PayloadSize = 68;

    public static MsgPrentice Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 4)
        {
            return new MsgPrentice(0, string.Empty, string.Empty);
        }

        int action = BinaryPrimitives.ReadInt32LittleEndian(payload[0..4]);
        string master = payload.Length >= 36 ? FixedStringCodec.ReadFixedString(payload.Slice(4, 32)) : string.Empty;
        string prentice = payload.Length >= 68 ? FixedStringCodec.ReadFixedString(payload.Slice(36, 32)) : string.Empty;
        return new MsgPrentice(action, master, prentice);
    }

    public byte[] ToPayload()
    {
        var buf = new byte[PayloadSize];
        BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(0, 4), ActionType);
        FixedStringCodec.WriteFixedString(buf.AsSpan(4, 32), MasterName ?? string.Empty);
        FixedStringCodec.WriteFixedString(buf.AsSpan(36, 32), PrenticeName ?? string.Empty);
        return buf;
    }
}

public readonly record struct PrenticeEntry(string MasterName, string PrenticeName, byte Level, byte Profession, bool IsOnline);

/// <summary>
/// S2C <c>MSG_PRENTICELIST</c> (10228): 4B header + <c>Count * 72B</c>.
/// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x53ec70</c>.
/// </summary>
public readonly record struct MsgPrenticeList(IReadOnlyList<PrenticeEntry> Entries)
{
    public const int EntryStride = 72;

    public byte[] ToPayload()
    {
        int count = Math.Min(10, Entries?.Count ?? 0);
        var buf = new byte[4 + count * EntryStride];
        buf[0] = (byte)count;
        for (int i = 0; i < count; i++)
        {
            int off = 4 + i * EntryStride;
            FixedStringCodec.WriteFixedString(buf.AsSpan(off, 32), Entries![i].MasterName ?? string.Empty);
            FixedStringCodec.WriteFixedString(buf.AsSpan(off + 32, 32), Entries[i].PrenticeName ?? string.Empty);
            buf[off + 64] = Entries[i].Level;
            buf[off + 65] = Entries[i].Profession;
            buf[off + 66] = Entries[i].IsOnline ? (byte)1 : (byte)0;
        }
        return buf;
    }

    public static MsgPrenticeList Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 4)
        {
            return new MsgPrenticeList([]);
        }

        int count = payload[0];
        var entries = new List<PrenticeEntry>(count);
        for (int i = 0; i < count; i++)
        {
            int off = 4 + i * EntryStride;
            if (off + EntryStride > payload.Length)
            {
                break;
            }
            entries.Add(new PrenticeEntry(
                FixedStringCodec.ReadFixedString(payload.Slice(off, 32)),
                FixedStringCodec.ReadFixedString(payload.Slice(off + 32, 32)),
                payload[off + 64],
                payload[off + 65],
                payload[off + 66] != 0));
        }

        return new MsgPrenticeList(entries);
    }
}

/// <summary>
/// <c>MSG_TARGETINFO</c> (10184): 68B payload (72B packet).
/// Ground truth: <c>Client/GodsWar.exe</c> at <c>0x580330</c> and <c>MsgDefine.h</c>.
/// </summary>
public readonly record struct MsgTargetInfo(uint ResPlayerId, string AimName)
{
    public const int PayloadSize = 68;

    public static MsgTargetInfo Parse(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 8)
        {
            return new MsgTargetInfo(0, string.Empty);
        }

        uint resId = BinaryPrimitives.ReadUInt32LittleEndian(payload[0..4]);
        string aim = FixedStringCodec.ReadFixedString(payload[4..Math.Min(68, payload.Length)]);
        return new MsgTargetInfo(resId, aim);
    }

    public byte[] ToPayload()
    {
        var buf = new byte[PayloadSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(0, 4), ResPlayerId);
        FixedStringCodec.WriteFixedString(buf.AsSpan(4, 64), AimName ?? string.Empty);
        return buf;
    }
}
