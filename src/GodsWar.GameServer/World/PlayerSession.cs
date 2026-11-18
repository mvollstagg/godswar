using GodsWar.Database.Models;
using GodsWar.GameServer.Game;
using GodsWar.Protocol;
using GodsWar.Protocol.Packets;

namespace GodsWar.GameServer.World;

public readonly record struct PlayerCombatStats(
    int Attack,
    int Defend,
    int MagicAttack,
    int MagicDefend,
    int Hit,
    int Dodge,
    int CritAppend,
    int CritDefend,
    int MaxHpBonus,
    int MaxMpBonus);

public readonly record struct StallGoodsSlot(
    long StallId,
    int BagIndex,
    int BagSlot,
    int Money,
    int Bijou,
    ItemEquipSlot Item)
{
    public static readonly StallGoodsSlot Empty = new(0, -1, -1, 0, 0, ItemEquipSlot.Empty);
    public bool IsEmpty => StallId <= 0 || Item.IsEmpty;
}

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
        ActiveSkillsList = activeSkills.ToList();
        PassiveSkillsList = passiveSkills.ToList();

        MapId = (ushort)Math.Max(0, (int)character.Map);
        ClientMapId = MapId;
        X = character.PosX;
        Y = 0f;
        Z = character.PosZ;
    }

    public uint UiId { get; }
    public string Account { get; }
    public CharacterBase Character { get; }
    public PacketConnection Connection { get; }
    public ItemEquipSlot[] Equips { get; }
    public ushort EquipMask => ComputeEquipMask();
    public ItemEquipSlot[][] Kitbags { get; }
    public List<ActiveSkillEntry> ActiveSkillsList { get; }
    public List<PassiveSkillEntry> PassiveSkillsList { get; }
    public IReadOnlyList<ActiveSkillEntry> ActiveSkills => ActiveSkillsList;
    public IReadOnlyList<PassiveSkillEntry> PassiveSkills => PassiveSkillsList;

    public ushort MapId { get; set; }
    public ushort ClientMapId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Atan2 { get; set; }
    public int CellX { get; set; } = -1;
    public int CellZ { get; set; } = -1;
    public bool InWorld { get; set; }

    public bool InCombat { get; set; }
    public uint? TargetUiId { get; set; }
    public DateTime NextAttackAtUtc { get; set; }
    public DateTime LastCombatAtUtc { get; set; }
    public DateTime LastRegenAtUtc { get; set; } = DateTime.UtcNow;
    public Dictionary<int, DateTime> SkillCooldowns { get; } = [];
    public List<ActiveStatus> ActiveStatuses { get; } = [];

    // Phase 5 state
    public ItemEquipSlot[] Storage { get; set; } = Enumerable.Repeat(ItemEquipSlot.Empty, 24).ToArray();
    public Dictionary<int, ActiveQuestState> ActiveQuests { get; } = [];
    public HashSet<int> CompletedQuests { get; } = [];
    public HashSet<string> Friends { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> Blacklist { get; } = new(StringComparer.OrdinalIgnoreCase);
    public int[] Altars { get; } = new int[12];
    public int? TeamId { get; set; }
    public uint TalkingNpcId { get; set; }
    public byte ActiveChatChannel { get; set; } = (byte)ChatChannel.Local;
    public (int Bag, int Slot)? ForgeEquipSlot { get; set; }
    public (int Bag, int Slot)? ForgeMaterialSlot { get; set; }

    // Phase 6 state (Stalls, GoldBox, SkillBackup, Pets, AFK)
    public bool StallActive { get; set; }
    public string StallText { get; set; } = string.Empty;
    public StallGoodsSlot[,] StallGoods { get; } = new StallGoodsSlot[2, 5];
    public uint PendingGoldBoxId { get; set; }
    public byte PendingGoldBoxIter { get; set; }
    public DialEntryDef[]? PendingGoldBoxDial { get; set; }
    public int[] BackupSkillIds { get; set; } = Enumerable.Repeat(-1, 16).ToArray();
    public int[] BackupItemIds { get; set; } = Enumerable.Repeat(-1, 16).ToArray();
    public int BackupRSkillId { get; set; } = -1;
    public int BackupRItemId { get; set; } = -1;
    public List<UserPetRecord> Pets { get; } = [];
    public bool IsAfk { get; set; }
    public DateTime LastAfkHuntAtUtc { get; set; }
    public DateTime LastPetActionAtUtc { get; set; }

    public bool IsDead => Character.CurHp <= 0;

    public bool FactionEquals(byte monsterFaction)
    {
        // MonsterObject.ini: 0 = Sparta guard, 1 = Athens guard, 2 = Wild monster
        // Character.Camp:    0 = Sparta, 1 = Athens
        if (monsterFaction >= 2)
        {
            return false;
        }

        return (byte)Character.Camp == monsterFaction;
    }

    public byte GenderByte => string.Equals(Character.Gender, "male", StringComparison.OrdinalIgnoreCase)
        ? (byte)1
        : (byte)0;

    public byte EffectiveLevel => (byte)Math.Clamp(
        Math.Max((int)Character.FighterJobLv, (int)Character.ScholarJobLv),
        1,
        255);

    public uint EffectiveMaxHp => (uint)Math.Max(1, Character.MaxHp > 0 ? Character.MaxHp : GameTables.GetPlayerMaxHp(EffectiveLevel, Character.Profession));
    public uint EffectiveMaxMp => (uint)Math.Max(1, Character.MaxMp > 0 ? Character.MaxMp : GameTables.GetPlayerMaxMp(EffectiveLevel, Character.Profession));
    public uint EffectiveCurHp => (uint)Math.Clamp(Character.CurHp, 0, (int)EffectiveMaxHp);
    public uint EffectiveCurMp => (uint)Math.Clamp(Character.CurMp, 0, (int)EffectiveMaxMp);

    private ushort ComputeEquipMask()
    {
        ushort mask = 0;
        for (int i = 0; i < Math.Min(12, Equips.Length); i++)
        {
            if (!Equips[i].IsEmpty)
            {
                mask |= (ushort)(1 << i);
            }
        }
        return mask;
    }

    public PlayerCombatStats ComputeCombatStats(GameDataCatalog catalog)
    {
        int lv = EffectiveLevel;
        int atk = 18 + lv * 4;
        int def = 8 + lv * 3;
        int matk = 18 + lv * 4;
        int mdef = 8 + lv * 3;
        int hit = 85 + lv;
        int dodge = 10 + lv / 2;
        int critAdd = 5;
        int critDef = 0;
        int hpBonus = 0;
        int mpBonus = 0;

        foreach (ItemEquipSlot eq in Equips)
        {
            if (eq.IsEmpty || !catalog.Items.TryGetValue(eq.Base, out ItemBaseDef? defItem))
            {
                continue;
            }

            int blvIdx = Math.Clamp(eq.BaseLevel - 1, 0, 4);
            if (defItem.AttackByLevel.Length > blvIdx) atk += defItem.AttackByLevel[blvIdx];
            if (defItem.DefendByLevel.Length > blvIdx) def += defItem.DefendByLevel[blvIdx];
            if (defItem.MagicAttackByLevel.Length > blvIdx) matk += defItem.MagicAttackByLevel[blvIdx];
            if (defItem.MagicDefendByLevel.Length > blvIdx) mdef += defItem.MagicDefendByLevel[blvIdx];
            if (defItem.MaxHpByLevel.Length > blvIdx) hpBonus += defItem.MaxHpByLevel[blvIdx];
            if (defItem.MaxMpByLevel.Length > blvIdx) mpBonus += defItem.MaxMpByLevel[blvIdx];

            int alvIdx = Math.Clamp(eq.AppendLevel - 1, 0, 11);
            int[] appIds = [eq.Append0, eq.Append1, eq.Append2, eq.Append3, eq.Append4];
            foreach (int appId in appIds)
            {
                if (appId < 0 || !catalog.Appends.TryGetValue(appId, out ItemAppendDef? app))
                {
                    continue;
                }

                float val = app.ValuesByLevel[alvIdx];
                switch (app.Type)
                {
                    case 0: atk += (int)val; break;
                    case 1: def += (int)val; break;
                    case 2: matk += (int)val; break;
                    case 3: mdef += (int)val; break;
                    case 4: hit += (int)val; break;
                    case 5: dodge += (int)val; break;
                    case 6: critAdd += (int)val; break;
                    case 7: critDef += (int)val; break;
                    case 13: hpBonus += (int)val; break;
                    case 14: mpBonus += (int)val; break;
                }
            }
        }

        float atkPct = 0f, defPct = 0f, matkPct = 0f, mdefPct = 0f;
        foreach (ActiveStatus st in ActiveStatuses)
        {
            if (catalog.Statuses.TryGetValue(st.StatusId, out StatusDef? sdef))
            {
                atkPct += sdef.AttackPct;
                defPct += sdef.DefendPct;
                matkPct += sdef.MagicAttackPct;
                mdefPct += sdef.MagicDefendPct;
            }
        }

        atk = Math.Max(1, (int)(atk * (1f + atkPct)));
        def = Math.Max(0, (int)(def * (1f + defPct)));
        matk = Math.Max(1, (int)(matk * (1f + matkPct)));
        mdef = Math.Max(0, (int)(mdef * (1f + mdefPct)));

        UserPetRecord? activePet = Pets.FirstOrDefault(p => p.State is 2 or 3);
        if (activePet is not null)
        {
            atk += activePet.Attack / 2;
            def += activePet.Defend / 2;
            matk += activePet.MagicAttack / 2;
            mdef += activePet.MagicDefend / 2;
        }

        return new PlayerCombatStats(atk, def, matk, mdef, hit, dodge, critAdd, critDef, hpBonus, mpBonus);
    }

    public bool AddExp(uint gainedExp, GameDataCatalog catalog, out bool leveledUp)
    {
        leveledUp = false;
        if (EffectiveLevel >= GameTables.PlayerMaxLevel || gainedExp == 0)
        {
            return false;
        }

        long exp = (long)Math.Max(0, Character.FighterJobExp) + gainedExp;
        int level = EffectiveLevel;

        while (level < GameTables.PlayerMaxLevel)
        {
            uint nextExp = GameTables.GetPlayerNextGradeExp(level);
            if (exp < nextExp)
            {
                break;
            }

            exp -= nextExp;
            level++;
            leveledUp = true;
        }

        Character.FighterJobExp = (int)Math.Min(int.MaxValue, exp);
        Character.ScholarJobExp = Character.FighterJobExp;
        Character.FighterJobLv = (short)level;
        Character.ScholarJobLv = (short)level;

        if (leveledUp)
        {
            PlayerCombatStats stats = ComputeCombatStats(catalog);
            Character.MaxHp = GameTables.GetPlayerMaxHp(level, Character.Profession) + stats.MaxHpBonus;
            Character.MaxMp = GameTables.GetPlayerMaxMp(level, Character.Profession) + stats.MaxMpBonus;
            Character.CurHp = Character.MaxHp;
            Character.CurMp = Character.MaxMp;
            Character.SkillPoint += 1;
        }

        return leveledUp;
    }

    public bool TryAddItemToBag(ItemEquipSlot item, out int bagIndex, out int slotIndex)
    {
        int bagCount = Math.Clamp(Character.BagNum, 1, 4);
        for (int b = 0; b < bagCount; b++)
        {
            for (int s = 0; s < 24; s++)
            {
                if (Kitbags[b][s].IsEmpty)
                {
                    Kitbags[b][s] = item;
                    bagIndex = b;
                    slotIndex = s;
                    return true;
                }
            }
        }

        bagIndex = -1;
        slotIndex = -1;
        return false;
    }

    public ValueTask SendAsync(GameOpcode opcode, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        ushort wireOpcode = Connection.Cipher is not null
            ? V227OpcodeMap.ToWire(opcode)
            : (ushort)opcode;
        return Connection.SendPacketAsync(wireOpcode, payload, cancellationToken);
    }

    public MsgSelfInfo ToSelfInfo() => new(
        Character.Id,
        Character.Name,
        GenderByte,
        (byte)Character.Camp,
        (byte)Character.Belief,
        (byte)Character.Profession,
        (byte)Character.HairStyle,
        (byte)(Character.FaceShap ?? 0),
        ClientMapId,
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
        GameTables.GetPlayerNextGradeExp(EffectiveLevel),
        (uint)Math.Max(0, Character.SkillPoint),
        (uint)Math.Max(0, Character.SkillExp),
        Equips);

    public ClientObjectData ToClientObjectData() => new(
        Character.Name,
        GenderByte,
        ClientMapId,
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

    public ClientBaseStats ToClientBaseStats(GameDataCatalog catalog)
    {
        PlayerCombatStats stats = ComputeCombatStats(catalog);
        return new ClientBaseStats(
            MaxHp: (int)EffectiveMaxHp + stats.MaxHpBonus,
            MaxMp: (int)EffectiveMaxMp + stats.MaxMpBonus,
            ResumeHp: 2,
            ResumeMp: 2,
            Attack: stats.Attack,
            Defend: stats.Defend,
            MagicAttack: stats.MagicAttack,
            MagicDefend: stats.MagicDefend,
            Hit: stats.Hit,
            Dodge: stats.Dodge,
            CritAppend: stats.CritAppend,
            CritDefend: stats.CritDefend,
            PhyDamageAppend: 0f,
            MagicDamageAppend: 0f,
            DamageSorb: 0,
            BeCure: 0f,
            Cure: 0f,
            StatusHit: 0,
            StatusDodge: 0);
    }

    public MsgSynGameData ToSynGameData(GameDataCatalog catalog, uint? skillPointOverride = null) => new(
        UiId,
        ToClientObjectData(),
        skillPointOverride ?? (uint)Math.Max(0, Character.SkillPoint),
        ToClientBaseStats(catalog));

    public MsgObjectInfo ToObjectInfo() => new(
        WorldObjectType.Player,
        (byte)Character.Camp,
        ClientMapId,
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
        (byte)(StallActive ? 1 : 0),
        EquipMask,
        Equips);
}
