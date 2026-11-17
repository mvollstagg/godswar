using GodsWar.Database.Models;
using GodsWar.Protocol;
using GodsWar.Protocol.Packets;

namespace GodsWar.GameServer.World;

/// <summary>
/// Live in-world player entity and its network session.
/// </summary>
public sealed class PlayerSession
{
    public PlayerSession(
        uint uiId,
        string account,
        CharacterBase character,
        PacketConnection connection,
        ItemEquipSlot[] equips,
        ItemEquipSlot[][] kitbags,
        IReadOnlyList<ActiveSkillEntry> activeSkills,
        IReadOnlyList<PassiveSkillEntry> passiveSkills)
    {
        UiId = uiId;
        Account = account;
        Character = character;
        Connection = connection;
        Equips = equips;
        Kitbags = kitbags;
        ActiveSkills = activeSkills;
        PassiveSkills = passiveSkills;

        ushort mask = 0;
        for (int i = 0; i < Math.Min(12, equips.Length); i++)
        {
            if (!equips[i].IsEmpty)
            {
                mask |= (ushort)(1 << i);
            }
        }
        EquipMask = mask;

        MapId = (ushort)Math.Max(0, (int)character.Map);
        X = character.PosX;
        Y = 0f;
        Z = character.PosZ;
    }

    public uint UiId { get; }
    public string Account { get; }
    public CharacterBase Character { get; }
    public PacketConnection Connection { get; }
    public ItemEquipSlot[] Equips { get; }
    public ushort EquipMask { get; }
    public ItemEquipSlot[][] Kitbags { get; }
    public IReadOnlyList<ActiveSkillEntry> ActiveSkills { get; }
    public IReadOnlyList<PassiveSkillEntry> PassiveSkills { get; }

    public ushort MapId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Atan2 { get; set; }
    public int CellX { get; set; } = -1;
    public int CellZ { get; set; } = -1;
    public bool InWorld { get; set; }

    public byte GenderByte => string.Equals(Character.Gender, "male", StringComparison.OrdinalIgnoreCase)
        ? (byte)1
        : (byte)0;

    public byte EffectiveLevel => (byte)Math.Clamp(
        Math.Max((int)Character.FighterJobLv, (int)Character.ScholarJobLv),
        1,
        255);

    public uint EffectiveMaxHp => (uint)Math.Max(1, Character.MaxHp > 0 ? Character.MaxHp : Character.CurHp);
    public uint EffectiveMaxMp => (uint)Math.Max(1, Character.MaxMp > 0 ? Character.MaxMp : Character.CurMp);
    public uint EffectiveCurHp => (uint)Math.Clamp(Character.CurHp > 0 ? Character.CurHp : (int)EffectiveMaxHp, 1, (int)EffectiveMaxHp);
    public uint EffectiveCurMp => (uint)Math.Clamp(Character.CurMp, 0, (int)EffectiveMaxMp);

    public ValueTask SendAsync(GameOpcode opcode, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
        => Connection.SendPacketAsync((ushort)opcode, payload, cancellationToken);

    public MsgSelfInfo ToSelfInfo() => new(
        Character.Id,
        Character.Name,
        GenderByte,
        (byte)Character.Camp,
        (byte)Character.Belief,
        (byte)Character.Profession,
        (byte)Character.HairStyle,
        (byte)(Character.FaceShap ?? 0),
        MapId,
        EquipMask,
        UiId,
        X,
        Y,
        Z,
        EffectiveMaxHp,
        EffectiveMaxMp,
        EffectiveCurHp,
        EffectiveCurMp,
        (uint)Math.Max(0, Character.FighterJobExp),
        1000u,
        (uint)Math.Max(0, Character.SkillPoint),
        (uint)Math.Max(0, Character.SkillExp),
        Equips);

    public ClientObjectData ToClientObjectData() => new(
        Character.Name,
        GenderByte,
        MapId,
        X,
        Y,
        Z,
        Speed: 4.5f,
        Style: 0,
        Faction: (byte)Character.Camp,
        Prestige: (uint)Math.Max(0, Character.Prestige),
        UnionId: (ushort)Math.Clamp(Character.Consortia, 0, ushort.MaxValue),
        UnionDuty: (byte)Math.Clamp((int)Character.ConsortiaJob, 0, (int)byte.MaxValue),
        Knight: (byte)Math.Clamp((int)Character.EarlRank, 0, (int)byte.MaxValue),
        Contribute: (uint)Math.Max(0, Character.ConsortiaContribute),
        StoreNum: (uint)Math.Max(10, Character.StoreNum),
        BagNum: (uint)Math.Max(1, Character.BagNum),
        Profession: (byte)Character.Profession,
        Exp: (uint)Math.Max(0, Character.FighterJobExp),
        Rank: EffectiveLevel,
        Hp: (int)EffectiveCurHp,
        Mp: (int)EffectiveCurMp,
        AttackDistance: 2,
        AttackSpeed: 1000,
        HairStyle: (byte)Character.HairStyle,
        FaceShap: (byte)(Character.FaceShap ?? 0),
        Belief: (ushort)Math.Max(0, (int)Character.Belief),
        Money: (uint)Math.Max(0, Character.Money),
        Stone: (uint)Math.Max(0, Character.Stone));

    public MsgObjectInfo ToObjectInfo() => new(
        WorldObjectType.Player,
        (byte)Character.Camp,
        MapId,
        UiId,
        EffectiveLevel,
        EffectiveCurHp,
        EffectiveMaxHp,
        X,
        Y,
        Z,
        Atan2,
        Character.Name,
        (uint)Character.Id,
        GenderByte,
        (byte)Character.Belief,
        (byte)Character.Profession,
        (byte)Character.HairStyle,
        (byte)(Character.FaceShap ?? 0),
        0,
        EquipMask,
        Equips);
}
