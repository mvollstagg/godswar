using GodsWar.GameServer.Game;
using GodsWar.Protocol;
using GodsWar.Protocol.Packets;

namespace GodsWar.GameServer.World;

/// <summary>
/// 16x16 spatial-grid map (<c>SIZE_OF_REGION = 256f</c>, <c>SIZE_OF_GRID = 32f</c>)
/// matching <c>GW 2.0/Server Files (Source)/GameServer/GameCore/Map.h</c> &amp; <c>Region.cpp</c>.
/// </summary>
public sealed class WorldMap
{
    public const float SizeOfRegion = 256.0f;
    public const float SizeOfGrid = 32.0f;
    public const int GridDimension = 16;

    private readonly object _sync = new();
    private readonly GridCell[,] _cells = new GridCell[GridDimension, GridDimension];
    private readonly Dictionary<uint, MonsterEntity> _monstersById = [];

    public WorldMap(SceneDefinition scene, IReadOnlyList<NpcSpawnData> npcs, IReadOnlyList<MonsterSpawnData> monsters)
    {
        Scene = scene;
        for (int x = 0; x < GridDimension; x++)
        {
            for (int z = 0; z < GridDimension; z++)
            {
                _cells[x, z] = new GridCell(x, z);
            }
        }

        foreach (NpcSpawnData npc in npcs)
        {
            (int cx, int cz) = ToCellCoords(npc.X, npc.Z);
            _cells[cx, cz].Npcs[npc.UiId] = npc;
        }

        foreach (MonsterSpawnData spawn in monsters)
        {
            var entity = new MonsterEntity(spawn);
            _cells[entity.CellX, entity.CellZ].Monsters[entity.UiId] = entity;
            _monstersById[entity.UiId] = entity;
        }

        NpcCount = npcs.Count;
        MonsterCount = monsters.Count;
    }

    public SceneDefinition Scene { get; }
    public int NpcCount { get; }
    public int MonsterCount { get; }

    public static (int CellX, int CellZ) ToCellCoords(float x, float z)
    {
        int cx = Math.Clamp((int)((x + SizeOfRegion) / SizeOfGrid), 0, GridDimension - 1);
        int cz = Math.Clamp((int)((z + SizeOfRegion) / SizeOfGrid), 0, GridDimension - 1);
        return (cx, cz);
    }

    public int PlayerCount
    {
        get
        {
            lock (_sync)
            {
                int count = 0;
                for (int x = 0; x < GridDimension; x++)
                {
                    for (int z = 0; z < GridDimension; z++)
                    {
                        count += _cells[x, z].Players.Count;
                    }
                }
                return count;
            }
        }
    }

    public async Task JoinAsync(PlayerSession player, CancellationToken cancellationToken = default)
    {
        byte[] selfObjectPayload = player.ToObjectInfo().ToPayload();
        var toJoiningPlayer = new List<byte[]>();
        var notifyOthers = new List<PlayerSession>();

        lock (_sync)
        {
            (int cx, int cz) = ToCellCoords(player.X, player.Z);
            player.CellX = cx;
            player.CellZ = cz;
            player.InWorld = true;
            _cells[cx, cz].Players[player.UiId] = player;

            foreach ((int ax, int az) in GetAoiCells(cx, cz))
            {
                GridCell cell = _cells[ax, az];
                foreach (PlayerSession other in cell.Players.Values)
                {
                    if (other.UiId == player.UiId)
                    {
                        continue;
                    }

                    toJoiningPlayer.Add(other.ToObjectInfo().ToPayload());
                    notifyOthers.Add(other);
                }

                foreach (NpcSpawnData npc in cell.Npcs.Values)
                {
                    toJoiningPlayer.Add(ToNpcObjectInfo(npc).ToPayload());
                }

                foreach (MonsterEntity monster in cell.Monsters.Values)
                {
                    if (monster.IsVisible)
                    {
                        toJoiningPlayer.Add(monster.ToObjectInfo().ToPayload());
                    }
                }
            }
        }

        foreach (byte[] payload in toJoiningPlayer)
        {
            await player.SendAsync(GameOpcode.MsgObjectInfo, payload, cancellationToken).ConfigureAwait(false);
        }

        foreach (PlayerSession other in notifyOthers)
        {
            await SendBestEffortAsync(other, GameOpcode.MsgObjectInfo, selfObjectPayload, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task MoveAsync(
        PlayerSession player,
        float x,
        float z,
        float atan2,
        ushort state,
        CancellationToken cancellationToken = default)
    {
        byte[] walkPayload = new MsgWalk((ushort)(player.UiId & 0xFFFF), state, x, z, atan2).ToPayload();
        byte[]? selfObjectPayload = null;
        byte[]? selfLeavePayload = null;

        var enterObjectsForSelf = new List<byte[]>();
        var enterNotifyOthers = new List<PlayerSession>();
        var leaveObjectsForSelf = new List<byte[]>();
        var leaveNotifyOthers = new List<PlayerSession>();
        var walkRecipients = new List<PlayerSession>();

        lock (_sync)
        {
            int oldX = player.CellX;
            int oldZ = player.CellZ;
            (int newX, int newZ) = ToCellCoords(x, z);

            player.X = x;
            player.Z = z;
            player.Atan2 = atan2;
            player.Character.PosX = x;
            player.Character.PosZ = z;

            if (oldX != newX || oldZ != newZ)
            {
                if (IsValidCell(oldX, oldZ))
                {
                    _cells[oldX, oldZ].Players.Remove(player.UiId);
                }

                _cells[newX, newZ].Players[player.UiId] = player;
                player.CellX = newX;
                player.CellZ = newZ;

                var oldArea = IsValidCell(oldX, oldZ)
                    ? GetAoiCells(oldX, oldZ).ToHashSet()
                    : new HashSet<(int, int)>();
                var newArea = GetAoiCells(newX, newZ).ToHashSet();

                selfObjectPayload = player.ToObjectInfo().ToPayload();
                selfLeavePayload = new MsgLeave(player.UiId).ToPayload();

                foreach ((int ax, int az) in newArea)
                {
                    if (oldArea.Contains((ax, az)))
                    {
                        continue;
                    }

                    GridCell cell = _cells[ax, az];
                    foreach (PlayerSession other in cell.Players.Values)
                    {
                        if (other.UiId != player.UiId)
                        {
                            enterObjectsForSelf.Add(other.ToObjectInfo().ToPayload());
                            enterNotifyOthers.Add(other);
                        }
                    }

                    foreach (NpcSpawnData npc in cell.Npcs.Values)
                    {
                        enterObjectsForSelf.Add(ToNpcObjectInfo(npc).ToPayload());
                    }

                    foreach (MonsterEntity monster in cell.Monsters.Values)
                    {
                        if (monster.IsVisible)
                        {
                            enterObjectsForSelf.Add(monster.ToObjectInfo().ToPayload());
                        }
                    }
                }

                foreach ((int ax, int az) in oldArea)
                {
                    if (newArea.Contains((ax, az)))
                    {
                        continue;
                    }

                    GridCell cell = _cells[ax, az];
                    foreach (PlayerSession other in cell.Players.Values)
                    {
                        if (other.UiId != player.UiId)
                        {
                            leaveObjectsForSelf.Add(new MsgLeave(other.UiId).ToPayload());
                            leaveNotifyOthers.Add(other);
                        }
                    }

                    foreach (NpcSpawnData npc in cell.Npcs.Values)
                    {
                        leaveObjectsForSelf.Add(new MsgLeave(npc.UiId).ToPayload());
                    }

                    foreach (MonsterEntity monster in cell.Monsters.Values)
                    {
                        if (monster.IsVisible)
                        {
                            leaveObjectsForSelf.Add(new MsgLeave(monster.UiId).ToPayload());
                        }
                    }
                }
            }

            // Broadcast MSG_WALK to all other players in current 3x3 AOI, excluding self
            // (self-echo causes SetPos + SetState(0) rubberbanding in 1.0.282 client).
            foreach ((int ax, int az) in GetAoiCells(player.CellX, player.CellZ))
            {
                foreach (PlayerSession other in _cells[ax, az].Players.Values)
                {
                    if (other.UiId != player.UiId)
                    {
                        walkRecipients.Add(other);
                    }
                }
            }
        }

        foreach (byte[] payload in enterObjectsForSelf)
        {
            await player.SendAsync(GameOpcode.MsgObjectInfo, payload, cancellationToken).ConfigureAwait(false);
        }

        foreach (byte[] payload in leaveObjectsForSelf)
        {
            await player.SendAsync(GameOpcode.MsgLeave, payload, cancellationToken).ConfigureAwait(false);
        }

        if (selfObjectPayload is not null)
        {
            foreach (PlayerSession other in enterNotifyOthers)
            {
                await SendBestEffortAsync(other, GameOpcode.MsgObjectInfo, selfObjectPayload, cancellationToken).ConfigureAwait(false);
            }
        }

        if (selfLeavePayload is not null)
        {
            foreach (PlayerSession other in leaveNotifyOthers)
            {
                await SendBestEffortAsync(other, GameOpcode.MsgLeave, selfLeavePayload, cancellationToken).ConfigureAwait(false);
            }
        }

        foreach (PlayerSession other in walkRecipients)
        {
            await SendBestEffortAsync(other, GameOpcode.MsgWalk, walkPayload, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task LeaveAsync(PlayerSession player, CancellationToken cancellationToken = default)
    {
        byte[] leavePayload = new MsgLeave(player.UiId).ToPayload();
        var notifyOthers = new List<PlayerSession>();

        lock (_sync)
        {
            if (IsValidCell(player.CellX, player.CellZ))
            {
                _cells[player.CellX, player.CellZ].Players.Remove(player.UiId);
                foreach ((int ax, int az) in GetAoiCells(player.CellX, player.CellZ))
                {
                    foreach (PlayerSession other in _cells[ax, az].Players.Values)
                    {
                        if (other.UiId != player.UiId)
                        {
                            notifyOthers.Add(other);
                        }
                    }
                }
            }

            player.InWorld = false;
            player.CellX = -1;
            player.CellZ = -1;
        }

        foreach (PlayerSession other in notifyOthers)
        {
            await SendBestEffortAsync(other, GameOpcode.MsgLeave, leavePayload, cancellationToken).ConfigureAwait(false);
        }
    }

    public MonsterEntity? GetMonster(uint uiId)
    {
        lock (_sync)
        {
            return _monstersById.GetValueOrDefault(uiId);
        }
    }

    public List<MonsterEntity> GetMonstersInRange(float x, float z, float radius)
    {
        var result = new List<MonsterEntity>();
        float r2 = radius * radius;
        (int cx, int cz) = ToCellCoords(x, z);
        lock (_sync)
        {
            foreach ((int ax, int az) in GetAoiCells(cx, cz))
            {
                foreach (MonsterEntity monster in _cells[ax, az].Monsters.Values)
                {
                    if (monster.IsDead)
                    {
                        continue;
                    }

                    float dx = monster.X - x;
                    float dz = monster.Z - z;
                    if (dx * dx + dz * dz <= r2)
                    {
                        result.Add(monster);
                    }
                }
            }
        }
        return result;
    }

    public async Task BroadcastToAoiAsync(
        int cellX,
        int cellZ,
        GameOpcode opcode,
        byte[] payload,
        uint? excludeUiId = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidCell(cellX, cellZ))
        {
            return;
        }

        var recipients = new List<PlayerSession>();
        lock (_sync)
        {
            foreach ((int ax, int az) in GetAoiCells(cellX, cellZ))
            {
                foreach (PlayerSession other in _cells[ax, az].Players.Values)
                {
                    if (excludeUiId is null || other.UiId != excludeUiId.Value)
                    {
                        recipients.Add(other);
                    }
                }
            }
        }

        foreach (PlayerSession target in recipients)
        {
            await SendBestEffortAsync(target, opcode, payload, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Advances monster AI (idle patrol, chase, attack, leave-fight, corpse despawn, respawn),
    /// status/buff ticks, and out-of-combat player HP/MP regeneration.
    /// </summary>
    public async Task TickAsync(DateTime nowUtc, GameDataCatalog catalog, CancellationToken cancellationToken = default)
    {
        var events = new List<(int CellX, int CellZ, GameOpcode Opcode, byte[] Payload, uint? TargetPlayerUiId)>();
        var allPlayers = new List<PlayerSession>();

        lock (_sync)
        {
            for (int cx = 0; cx < GridDimension; cx++)
            {
                for (int cz = 0; cz < GridDimension; cz++)
                {
                    allPlayers.AddRange(_cells[cx, cz].Players.Values);
                }
            }

            // Player status ticks & out-of-combat regen
            foreach (PlayerSession player in allPlayers)
            {
                if (!player.InWorld || player.IsDead)
                {
                    continue;
                }

                bool statusChanged = false;
                for (int i = player.ActiveStatuses.Count - 1; i >= 0; i--)
                {
                    ActiveStatus st = player.ActiveStatuses[i];
                    if (nowUtc >= st.ExpiresAtUtc)
                    {
                        player.ActiveStatuses.RemoveAt(i);
                        statusChanged = true;
                        continue;
                    }

                    if (nowUtc >= st.NextTickAtUtc && catalog.Statuses.TryGetValue(st.StatusId, out StatusDef? sdef))
                    {
                        player.ActiveStatuses[i] = st with { NextTickAtUtc = nowUtc.AddSeconds(sdef.IntervalSeconds) };
                        int hpDelta = sdef.AddHp - sdef.DecHp;
                        int mpDelta = sdef.AddMp - sdef.DecMp;
                        if (hpDelta != 0)
                        {
                            player.Character.CurHp = Math.Clamp(player.Character.CurHp + hpDelta, 1, (int)player.EffectiveMaxHp);
                        }
                        if (mpDelta != 0)
                        {
                            player.Character.CurMp = Math.Clamp(player.Character.CurMp + mpDelta, 0, (int)player.EffectiveMaxMp);
                            events.Add((player.CellX, player.CellZ, GameOpcode.MsgUpdateMp, new MsgUpdateMp(player.UiId, player.Character.CurMp).ToPayload(), player.UiId));
                        }
                    }
                }

                if (statusChanged)
                {
                    var pairs = player.ActiveStatuses.Select(s => ((uint)s.StatusId, (ushort)Math.Max(0, (s.ExpiresAtUtc - nowUtc).TotalSeconds))).ToList();
                    events.Add((player.CellX, player.CellZ, GameOpcode.MsgStatus, new MsgStatus(player.UiId, pairs).ToPayload(), null));
                }

                if (player.InCombat && (nowUtc - player.LastCombatAtUtc).TotalSeconds >= 8)
                {
                    player.InCombat = false;
                    events.Add((player.CellX, player.CellZ, GameOpcode.MsgFight, new MsgFight(player.UiId, 0, false).ToPayload(), null));
                }

                if (!player.InCombat && (nowUtc - player.LastRegenAtUtc).TotalSeconds >= 3)
                {
                    player.LastRegenAtUtc = nowUtc;
                    bool changed = false;
                    if (player.Character.CurHp < (int)player.EffectiveMaxHp)
                    {
                        player.Character.CurHp = Math.Min((int)player.EffectiveMaxHp, player.Character.CurHp + Math.Max(2, (int)(player.EffectiveMaxHp * 0.03f)));
                        changed = true;
                    }
                    if (player.Character.CurMp < (int)player.EffectiveMaxMp)
                    {
                        player.Character.CurMp = Math.Min((int)player.EffectiveMaxMp, player.Character.CurMp + Math.Max(2, (int)(player.EffectiveMaxMp * 0.03f)));
                        changed = true;
                    }
                    if (changed)
                    {
                        events.Add((player.CellX, player.CellZ, GameOpcode.MsgUpdateMp, new MsgUpdateMp(player.UiId, player.Character.CurMp).ToPayload(), player.UiId));
                        events.Add((player.CellX, player.CellZ, GameOpcode.MsgSynGameData, new MsgSynGameData(player.UiId, player.ToClientObjectData(), (uint)Math.Max(0, player.Character.SkillPoint)).ToPayload(), player.UiId));
                    }
                }
            }

            if (allPlayers.Count == 0 && _monstersById.Values.All(m => m.State is MonsterAiState.Idle or MonsterAiState.Walk))
            {
                return;
            }

            var playerLookup = allPlayers.ToDictionary(p => p.UiId);

            // Monster AI loop
            foreach (MonsterEntity monster in _monstersById.Values)
            {
                switch (monster.State)
                {
                    case MonsterAiState.Dead:
                        if (nowUtc >= monster.CorpseDespawnAtUtc)
                        {
                            monster.State = MonsterAiState.Lost;
                            events.Add((monster.CellX, monster.CellZ, GameOpcode.MsgLeave, new MsgLeave(monster.UiId).ToPayload(), null));
                        }
                        break;

                    case MonsterAiState.Lost:
                        if (nowUtc >= monster.RespawnAtUtc)
                        {
                            int oldX = monster.CellX;
                            int oldZ = monster.CellZ;
                            monster.Respawn();
                            MoveMonsterCell(monster, oldX, oldZ);
                            events.Add((monster.CellX, monster.CellZ, GameOpcode.MsgObjectInfo, monster.ToObjectInfo().ToPayload(), null));
                        }
                        break;

                    case MonsterAiState.LeaveFight:
                        {
                            float dx = monster.Spawn.X - monster.X;
                            float dz = monster.Spawn.Z - monster.Z;
                            float dist = MathF.Sqrt(dx * dx + dz * dz);
                            if (dist <= 1.0f)
                            {
                                monster.X = monster.Spawn.X;
                                monster.Z = monster.Spawn.Z;
                                monster.Hp = monster.MaxHp;
                                monster.State = MonsterAiState.Idle;
                                monster.ClearCombat();
                            }
                            else
                            {
                                float step = Math.Min(dist, 1.5f);
                                int oldX = monster.CellX;
                                int oldZ = monster.CellZ;
                                monster.X += dx / dist * step;
                                monster.Z += dz / dist * step;
                                monster.Atan2 = MathF.Atan2(dx, dz);
                                MoveMonsterCell(monster, oldX, oldZ);
                                events.Add((monster.CellX, monster.CellZ, GameOpcode.MsgWalk, new MsgWalk((ushort)(monster.UiId & 0xFFFF), 2, monster.X, monster.Z, monster.Atan2).ToPayload(), null));
                            }
                        }
                        break;

                    case MonsterAiState.Fight:
                        {
                            PlayerSession? target = monster.TargetPlayerUiId.HasValue
                                ? playerLookup.GetValueOrDefault(monster.TargetPlayerUiId.Value)
                                : null;

                            float homeDx = monster.X - monster.Spawn.X;
                            float homeDz = monster.Z - monster.Spawn.Z;
                            float homeDist = MathF.Sqrt(homeDx * homeDx + homeDz * homeDz);

                            if (target is null || !target.InWorld || target.IsDead || target.MapId != Scene.MapId ||
                                (homeDist > 18.0f && (nowUtc - monster.LastCombatAtUtc).TotalSeconds > 4))
                            {
                                monster.State = MonsterAiState.LeaveFight;
                                monster.ClearCombat();
                                events.Add((monster.CellX, monster.CellZ, GameOpcode.MsgFight, new MsgFight(monster.UiId, 0, false).ToPayload(), null));
                                break;
                            }

                            float dx = target.X - monster.X;
                            float dz = target.Z - monster.Z;
                            float dist = MathF.Sqrt(dx * dx + dz * dz);
                            if (dist > 2.4f)
                            {
                                float step = Math.Min(dist - 1.8f, 1.2f);
                                if (step > 0.1f)
                                {
                                    int oldX = monster.CellX;
                                    int oldZ = monster.CellZ;
                                    monster.X += dx / dist * step;
                                    monster.Z += dz / dist * step;
                                    monster.Atan2 = MathF.Atan2(dx, dz);
                                    MoveMonsterCell(monster, oldX, oldZ);
                                    events.Add((monster.CellX, monster.CellZ, GameOpcode.MsgWalk, new MsgWalk((ushort)(monster.UiId & 0xFFFF), 2, monster.X, monster.Z, monster.Atan2).ToPayload(), null));
                                }
                            }
                            else if (nowUtc >= monster.NextAttackAtUtc)
                            {
                                monster.NextAttackAtUtc = nowUtc.AddMilliseconds(1500);
                                monster.LastCombatAtUtc = nowUtc;
                                target.InCombat = true;
                                target.LastCombatAtUtc = nowUtc;

                                int mLv = (int)monster.Level;
                                PlayerCombatStats pStats = target.ComputeCombatStats(catalog);
                                AttackResultType hitType = CombatCalculator.JudgeAttackStyle(
                                    GameTables.GetMonsterHit(mLv),
                                    pStats.Dodge,
                                    5,
                                    pStats.CritDefend);

                                int dmg = hitType == AttackResultType.Miss
                                    ? -1
                                    : CombatCalculator.CalculatePhysicalDamage(
                                        GameTables.GetMonsterAttack(mLv),
                                        pStats.Defend,
                                        isCrit: hitType == AttackResultType.Crit);

                                if (dmg > 0)
                                {
                                    target.Character.CurHp = Math.Max(0, target.Character.CurHp - dmg);
                                }

                                events.Add((
                                    monster.CellX,
                                    monster.CellZ,
                                    GameOpcode.MsgAttack,
                                    new MsgAttack(monster.UiId, target.X, 0f, target.Z, target.UiId, dmg, 0, (byte)hitType).ToPayload(),
                                    null));

                                events.Add((
                                    target.CellX,
                                    target.CellZ,
                                    GameOpcode.MsgSynGameData,
                                    new MsgSynGameData(target.UiId, target.ToClientObjectData(), (uint)Math.Max(0, target.Character.SkillPoint)).ToPayload(),
                                    target.UiId));

                                if (target.IsDead)
                                {
                                    var deadMsg = new MsgDead(
                                        target.UiId,
                                        [-1, -1, -1, -1, -1],
                                        new uint[5],
                                        new uint[5],
                                        new uint[5],
                                        new uint[5],
                                        monster.UiId,
                                        0);
                                    events.Add((target.CellX, target.CellZ, GameOpcode.MsgDead, deadMsg.ToPayload(), null));
                                    monster.State = MonsterAiState.LeaveFight;
                                    monster.ClearCombat();
                                }
                            }
                        }
                        break;

                    case MonsterAiState.Idle:
                    case MonsterAiState.Walk:
                        // Aggressive monsters (Mode == 2) aggro nearby alive players within 6 units
                        if (monster.Spawn.Mode == 2 && allPlayers.Count > 0)
                        {
                            foreach (PlayerSession candidate in allPlayers)
                            {
                                if (!candidate.InWorld || candidate.IsDead || candidate.FactionEquals(monster.Faction))
                                {
                                    continue;
                                }

                                float dx = candidate.X - monster.X;
                                float dz = candidate.Z - monster.Z;
                                if (dx * dx + dz * dz <= 36f)
                                {
                                    monster.AddEnmity(candidate.UiId, 1);
                                    events.Add((monster.CellX, monster.CellZ, GameOpcode.MsgFight, new MsgFight(monster.UiId, candidate.UiId, true).ToPayload(), null));
                                    break;
                                }
                            }
                        }

                        if (monster.State != MonsterAiState.Fight && monster.Spawn.MoveTime > 0 && nowUtc >= monster.NextMoveAtUtc)
                        {
                            monster.NextMoveAtUtc = nowUtc.AddSeconds(Random.Shared.Next(5, 12));
                            float targetX = monster.State == MonsterAiState.Idle
                                ? monster.Spawn.X + (float)(Random.Shared.NextDouble() * 6.0 - 3.0)
                                : monster.Spawn.X;
                            float targetZ = monster.State == MonsterAiState.Idle
                                ? monster.Spawn.Z + (float)(Random.Shared.NextDouble() * 6.0 - 3.0)
                                : monster.Spawn.Z;
                            monster.State = monster.State == MonsterAiState.Idle ? MonsterAiState.Walk : MonsterAiState.Idle;

                            int oldX = monster.CellX;
                            int oldZ = monster.CellZ;
                            monster.Atan2 = MathF.Atan2(targetX - monster.X, targetZ - monster.Z);
                            monster.X = targetX;
                            monster.Z = targetZ;
                            MoveMonsterCell(monster, oldX, oldZ);
                            events.Add((monster.CellX, monster.CellZ, GameOpcode.MsgWalk, new MsgWalk((ushort)(monster.UiId & 0xFFFF), 2, monster.X, monster.Z, monster.Atan2).ToPayload(), null));
                        }
                        break;
                }
            }
        }

        foreach ((int cx, int cz, GameOpcode opcode, byte[] payload, uint? targetPlayerUiId) in events)
        {
            if (targetPlayerUiId.HasValue)
            {
                PlayerSession? target = allPlayers.FirstOrDefault(p => p.UiId == targetPlayerUiId.Value);
                if (target is not null)
                {
                    await SendBestEffortAsync(target, opcode, payload, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                await BroadcastToAoiAsync(cx, cz, opcode, payload, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private void MoveMonsterCell(MonsterEntity monster, int oldX, int oldZ)
    {
        (int newX, int newZ) = ToCellCoords(monster.X, monster.Z);
        if (newX == oldX && newZ == oldZ)
        {
            return;
        }

        if (IsValidCell(oldX, oldZ))
        {
            _cells[oldX, oldZ].Monsters.Remove(monster.UiId);
        }

        _cells[newX, newZ].Monsters[monster.UiId] = monster;
        monster.CellX = newX;
        monster.CellZ = newZ;
    }

    private static bool IsValidCell(int cx, int cz)
        => cx >= 0 && cx < GridDimension && cz >= 0 && cz < GridDimension;

    private static IEnumerable<(int X, int Z)> GetAoiCells(int cx, int cz)
    {
        for (int dz = -1; dz <= 1; dz++)
        {
            int nz = cz + dz;
            if (nz < 0 || nz >= GridDimension)
            {
                continue;
            }

            for (int dx = -1; dx <= 1; dx++)
            {
                int nx = cx + dx;
                if (nx >= 0 && nx < GridDimension)
                {
                    yield return (nx, nz);
                }
            }
        }
    }

    private static MsgObjectInfo ToNpcObjectInfo(NpcSpawnData npc) => new(
        WorldObjectType.Npc,
        npc.Faction,
        npc.MapId,
        npc.UiId,
        Level: 1,
        Hp: 1000,
        MaxHp: 1000,
        npc.X,
        npc.Y,
        npc.Z,
        npc.Atan2,
        npc.Name);

    private static MsgObjectInfo ToMonsterObjectInfo(MonsterSpawnData monster) => new(
        WorldObjectType.Monster,
        monster.Faction,
        monster.MapId,
        monster.UiId,
        monster.Level,
        monster.MaxHp,
        monster.MaxHp,
        monster.X,
        0f,
        monster.Z,
        monster.Atan2,
        monster.ObjectName);

    private static async Task SendBestEffortAsync(
        PlayerSession target,
        GameOpcode opcode,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        try
        {
            await target.SendAsync(opcode, payload, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Target socket may be closing concurrently.
        }
    }

    private sealed class GridCell(int x, int z)
    {
        public int X { get; } = x;
        public int Z { get; } = z;
        public Dictionary<uint, PlayerSession> Players { get; } = [];
        public Dictionary<uint, NpcSpawnData> Npcs { get; } = [];
        public Dictionary<uint, MonsterEntity> Monsters { get; } = [];
    }
}
