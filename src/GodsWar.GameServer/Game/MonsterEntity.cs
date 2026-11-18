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

    public MsgObjectInfo ToObjectInfo(ushort? clientMapId = null) => new(
        WorldObjectType.Monster,
        Faction,
        clientMapId ?? MapId,
        UiId,
        (byte)Math.Clamp(Level, 1, 255),
        Hp,
        MaxHp,
        X,
        0f,
        Z,
        Atan2,
        ObjectName,
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
