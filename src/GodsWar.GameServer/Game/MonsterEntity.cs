using GodsWar.GameServer.World;
using GodsWar.Protocol.Packets;

namespace GodsWar.GameServer.Game;

public enum MonsterAiState
{
    Idle = 0,
    Walk = 1,
    Fight = 2,
    LeaveFight = 3,
    Dead = 4,
    Lost = 5,
}

public sealed record ActiveStatus(
    int StatusId,
    DateTime ExpiresAtUtc,
    DateTime NextTickAtUtc);

/// <summary>
/// Live mutable monster instance in a <see cref="WorldMap"/>.
/// State machine ported from <c>Monster.cpp</c> (<c>AI</c>, <c>Dead</c>, <c>Refresh</c>).
/// </summary>
public sealed class MonsterEntity
{
    private readonly Dictionary<uint, int> _enmity = [];

    public MonsterEntity(MonsterSpawnData spawn)
    {
        Spawn = spawn;
        Hp = spawn.MaxHp;
        MaxHp = spawn.MaxHp;
        X = spawn.X;
        Z = spawn.Z;
        Atan2 = spawn.Atan2;
        (CellX, CellZ) = WorldMap.ToCellCoords(X, Z);
        State = MonsterAiState.Idle;
        NextMoveAtUtc = DateTime.UtcNow.AddSeconds(Random.Shared.Next(3, 10));
    }

    public MonsterSpawnData Spawn { get; }
    public uint UiId => Spawn.UiId;
    public ushort MapId => Spawn.MapId;
    public string ObjectName => Spawn.ObjectName;
    public uint Level => Spawn.Level;
    public byte Faction => Spawn.Faction;

    public uint Hp { get; set; }
    public uint MaxHp { get; }
    public float X { get; set; }
    public float Z { get; set; }
    public float Atan2 { get; set; }
    public int CellX { get; set; }
    public int CellZ { get; set; }

    public MonsterAiState State { get; set; }
    public uint? TargetPlayerUiId { get; set; }
    public DateTime NextAttackAtUtc { get; set; }
    public DateTime NextMoveAtUtc { get; set; }
    public DateTime CorpseDespawnAtUtc { get; set; }
    public DateTime RespawnAtUtc { get; set; }
    public DateTime LastCombatAtUtc { get; set; }

    public List<ItemEquipSlot> GroundDrops { get; } = [];
    public uint? DropOwnerUiId { get; set; }
    public List<ActiveStatus> ActiveStatuses { get; } = [];

    public bool IsDead => State is MonsterAiState.Dead or MonsterAiState.Lost;
    public bool IsVisible => State != MonsterAiState.Lost;

    public void AddEnmity(uint playerUiId, int amount)
    {
        _enmity[playerUiId] = _enmity.GetValueOrDefault(playerUiId) + Math.Max(1, amount);
        LastCombatAtUtc = DateTime.UtcNow;
        if (State != MonsterAiState.Dead && State != MonsterAiState.Lost && State != MonsterAiState.LeaveFight)
        {
            State = MonsterAiState.Fight;
            TargetPlayerUiId = GetMaxEnmityPlayer();
        }
    }

    public uint? GetMaxEnmityPlayer()
    {
        uint? bestId = null;
        int bestVal = -1;
        foreach ((uint pid, int val) in _enmity)
        {
            if (val > bestVal)
            {
                bestVal = val;
                bestId = pid;
            }
        }
        return bestId;
    }

    public void RemoveEnmity(uint playerUiId)
    {
        _enmity.Remove(playerUiId);
        if (TargetPlayerUiId == playerUiId)
        {
            TargetPlayerUiId = GetMaxEnmityPlayer();
        }
    }

    public void ClearCombat()
    {
        _enmity.Clear();
        TargetPlayerUiId = null;
    }

    public void Die(uint killerUiId, GameDataCatalog catalog)
    {
        Hp = 0;
        State = MonsterAiState.Dead;
        ClearCombat();
        GroundDrops.Clear();
        DropOwnerUiId = killerUiId;
        ActiveStatuses.Clear();

        DateTime now = DateTime.UtcNow;
        CorpseDespawnAtUtc = now.AddSeconds(15);
        RespawnAtUtc = now.AddSeconds(Math.Max(5, Spawn.RefreshTimeSeconds));

        // Roll drops from ConfirmItemTable (Drop*.ini)
        if (!string.Equals(Spawn.ConfirmItemTable, "NULL", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(Spawn.ConfirmItemTable))
        {
            for (int i = 0; i < Spawn.ConfirmDropNum; i++)
            {
                if (Random.Shared.Next(100) < Spawn.ConfirmProbability)
                {
                    ItemEquipSlot drop = catalog.CreateDropItem(Spawn.ConfirmItemTable);
                    if (!drop.IsEmpty)
                    {
                        GroundDrops.Add(drop);
                    }
                }
            }
        }

        // Roll fallback potion/starter drop if DropNum > 0 and ConfirmItemTable didn't yield anything
        if (GroundDrops.Count == 0 && Spawn.DropNum > 0 && Random.Shared.Next(100) < Spawn.DropProbability)
        {
            int potionId = Level switch
            {
                < 15 => 4000, // HPPotion_a
                < 35 => 4001, // HPPotion_b
                < 60 => 4002, // HPPotion_c
                _ => 4003,    // HPPotion_d
            };
            GroundDrops.Add(new ItemEquipSlot(potionId, -1, -1, -1, -1, -1, 1, 0, 0, 1));
        }
    }

    public void Respawn()
    {
        Hp = MaxHp;
        X = Spawn.X;
        Z = Spawn.Z;
        Atan2 = Spawn.Atan2;
        (CellX, CellZ) = WorldMap.ToCellCoords(X, Z);
        State = MonsterAiState.Idle;
        ClearCombat();
        GroundDrops.Clear();
        DropOwnerUiId = null;
        ActiveStatuses.Clear();
        NextMoveAtUtc = DateTime.UtcNow.AddSeconds(Random.Shared.Next(3, 10));
    }

    private static readonly Dictionary<(ushort MapId, string Name), string> V227MonsterFallbacks = new()
    {
        // Map 0: Athens
        [(0, "C_elite_greecewarrior_005")] = "C_elite_greecewarrior_003",
        [(0, "C_elite_greecewarrior_008")] = "C_elite_greecewarrior_003",
        // Map 1: Athens_Newbie
        [(1, "A_elite_snake_001")] = "A_elite_bull_006",
        [(1, "A_elite_stub_002")] = "A_elite_bull_006",
        [(1, "A_elite_wasp_002")] = "A_elite_bull_006",
        [(1, "A_normal_deer_001")] = "A_normal_ark_003",
        [(1, "A_normal_snake_001")] = "A_normal_ark_003",
        [(1, "A_normal_stub_002")] = "A_normal_ark_003",
        [(1, "A_normal_wasp_002")] = "A_normal_ark_003",
        [(1, "A_normal_wasp_008")] = "A_normal_ark_003",
        [(1, "C_elite_greecewarrior_003")] = "C_elite_greecewarrior_006",
        [(1, "C_elite_greecewarrior_008")] = "C_elite_greecewarrior_006",
        [(1, "C_elite_mage_001")] = "C_elite_greecewarrior_006",
        // Map 3: Parnitha_1
        [(3, "B_elite_greecewarrior_008")] = "A_boss_boar_001",
        // Map 4: Parnitha_2
        [(4, "B_boss_turtle_001")] = "B_bossB_octopus_001",
        [(4, "B_elite_crocodilian_002")] = "B_eliteB_froggy_003",
        [(4, "B_elite_mage_011")] = "B_eliteB_froggy_003",
        [(4, "B_elite_mage_017")] = "B_eliteB_froggy_003",
        [(4, "B_elite_persiawarrior_003")] = "B_eliteB_froggy_003",
        [(4, "B_elite_skeleton_008")] = "B_eliteB_froggy_003",
        [(4, "B_normal_crocodilian_002")] = "B_normalB_centaur_008",
        [(4, "B_normal_mage_011")] = "B_normalB_centaur_008",
        [(4, "B_normal_mage_017")] = "B_normalB_centaur_008",
        [(4, "B_normal_persiawarrior_003")] = "B_normalB_centaur_008",
        [(4, "B_normal_persiawarrior_004")] = "B_normalB_centaur_008",
        [(4, "B_normal_skeleton_008")] = "B_normalB_centaur_008",
        [(4, "B_normal_skeleton_014")] = "B_normalB_centaur_008",
        [(4, "B_normal_turtle_003")] = "B_normalB_centaur_008",
        // Map 50: Sparta
        [(50, "C_elite_greecewarrior_009")] = "C_elite_greecewarrior_004",
        [(50, "C_elite_greecewarrior_012")] = "C_elite_greecewarrior_004",
        // Map 51: Sparta_Newbie
        [(51, "A_elite_ark_001")] = "A_elite_skeleton_002",
        [(51, "A_elite_snake_006")] = "A_elite_skeleton_002",
        [(51, "A_elite_stub_001")] = "A_elite_skeleton_002",
        [(51, "A_normal_ark_001")] = "A_normal_ark_002",
        [(51, "A_normal_skeleton_003")] = "A_normal_ark_002",
        [(51, "A_normal_skeleton_004")] = "A_normal_ark_002",
        [(51, "A_normal_skeleton_005")] = "A_normal_ark_002",
        [(51, "A_normal_skeleton_006")] = "A_normal_ark_002",
        [(51, "A_normal_skeleton_007")] = "A_normal_ark_002",
        [(51, "A_normal_snake_002")] = "A_normal_ark_002",
        [(51, "A_normal_snake_006")] = "A_normal_ark_002",
        [(51, "A_normal_stub_001")] = "A_normal_ark_002",
        [(51, "A_normal_stub_003")] = "A_normal_ark_002",
        [(51, "C_elite_greecewarrior_004")] = "C_elite_greecewarrior_011",
        [(51, "C_elite_greecewarrior_009")] = "C_elite_greecewarrior_011",
        [(51, "C_elite_mage_004")] = "C_elite_greecewarrior_011",
        // Map 53: Nemea_1
        [(53, "B_elite_greecewarrior_011")] = "A_boss_wolf_005",
        [(53, "B_elite_greecewarrior_012")] = "A_boss_wolf_005",
        // Map 54: Nemea_2
        [(54, "B_boss_lion_001")] = "B_bossB_spriggan_001",
        [(54, "B_elite_dryad_007")] = "B_eliteB_dryad_001_000",
        [(54, "B_elite_harpies_004")] = "B_eliteB_dryad_001_000",
        [(54, "B_elite_wasp_005")] = "B_eliteB_dryad_001_000",
        [(54, "B_elite_wolf_004")] = "B_eliteB_dryad_001_000",
        [(54, "B_elite_wraith_001")] = "B_eliteB_dryad_001_000",
        [(54, "B_normal_dryad_001")] = "B_normalB_dryad_002",
        [(54, "B_normal_dryad_002")] = "B_normalB_dryad_002",
        [(54, "B_normal_dryad_007")] = "B_normalB_dryad_002",
        [(54, "B_normal_harpies_004")] = "B_normalB_dryad_002",
        [(54, "B_normal_lion_002")] = "B_normalB_dryad_002",
        [(54, "B_normal_wasp_005")] = "B_normalB_dryad_002",
        [(54, "B_normal_wolf_004")] = "B_normalB_dryad_002",
        [(54, "B_normal_wraith_001")] = "B_normalB_dryad_002",
        // Map 100: Derveni_All
        [(100, "A_elite_snake_008")] = "A_normal_greecewarrior_009",
        [(100, "B_normal_centaur_003")] = "B_normal_centaur_004",
        // Map 152: Thermopylae_All
        [(152, "B_elite_boar_002")] = "B_eliteB_amazon_004",
        [(152, "B_elite_froggy_001")] = "B_eliteB_amazon_004",
        [(152, "B_elite_spider_007")] = "B_eliteB_amazon_004",
        [(152, "B_elite_stymphalianbird_002")] = "B_eliteB_amazon_004",
        [(152, "B_elite_turtle_004")] = "B_eliteB_amazon_004",
        [(152, "B_elite_wraith_004")] = "B_eliteB_amazon_004",
        [(152, "B_normal_boar_002")] = "B_normalB_amazon_001",
        [(152, "B_normal_froggy_001")] = "B_normalB_amazon_001",
        [(152, "B_normal_greecewarrior_005")] = "B_normalB_amazon_001",
        [(152, "B_normal_greecewarrior_008")] = "B_normalB_amazon_001",
        [(152, "B_normal_persiawarrior_002")] = "B_normalB_amazon_001",
        [(152, "B_normal_spider_007")] = "B_normalB_amazon_001",
        [(152, "B_normal_stymphalianbird_002")] = "B_normalB_amazon_001",
        [(152, "B_normal_turtle_004")] = "B_normalB_amazon_001",
        [(152, "B_normal_wraith_004")] = "B_normalB_amazon_001",
        [(152, "C_boss_persiawarrior_001")] = "C_boss_greecewarrior_001",
        [(152, "C_elite_mage_001")] = "C_elite_greecewarrior_003",
        [(152, "C_elite_mage_011")] = "C_elite_greecewarrior_003",
        [(152, "C_normal_mage_011")] = "C_boss_greecewarrior_001",
    };

    public MsgObjectInfo ToObjectInfo(ushort? clientMapId = null, bool isV227 = false)
    {
        ushort mapId = clientMapId ?? MapId;
        string name = isV227 && V227MonsterFallbacks.TryGetValue((MapId, ObjectName), out string? fallback)
            ? fallback
            : ObjectName;

        return new(
            WorldObjectType.Monster,
            Faction,
            mapId,
            UiId,
            (byte)Math.Clamp(Level, 1, 255),
            Hp,
            MaxHp,
            X,
            0f,
            Z,
            Atan2,
            name,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            []);
    }
}
