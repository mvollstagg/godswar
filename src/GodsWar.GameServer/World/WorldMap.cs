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

        foreach (MonsterSpawnData monster in monsters)
        {
            (int cx, int cz) = ToCellCoords(monster.X, monster.Z);
            _cells[cx, cz].Monsters[monster.UiId] = monster;
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

                foreach (MonsterSpawnData monster in cell.Monsters.Values)
                {
                    toJoiningPlayer.Add(ToMonsterObjectInfo(monster).ToPayload());
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

                    foreach (MonsterSpawnData monster in cell.Monsters.Values)
                    {
                        enterObjectsForSelf.Add(ToMonsterObjectInfo(monster).ToPayload());
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

                    foreach (MonsterSpawnData monster in cell.Monsters.Values)
                    {
                        leaveObjectsForSelf.Add(new MsgLeave(monster.UiId).ToPayload());
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
        public Dictionary<uint, MonsterSpawnData> Monsters { get; } = [];
    }
}
