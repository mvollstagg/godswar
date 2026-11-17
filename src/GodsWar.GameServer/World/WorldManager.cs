using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GodsWar.GameServer.World;

/// <summary>
/// Manages all loaded <see cref="WorldMap"/> instances and active <see cref="PlayerSession"/>s.
/// </summary>
public sealed class WorldManager
{
    private readonly Dictionary<ushort, WorldMap> _maps = [];
    private readonly ConcurrentDictionary<uint, PlayerSession> _playersByUiId = new();
    private readonly ConcurrentDictionary<string, PlayerSession> _playersByAccount = new(StringComparer.OrdinalIgnoreCase);
    private int _nextPlayerUiId;

    public WorldManager(IOptions<GameServerOptions> options, ILogger<WorldManager> logger)
    {
        string sceneDir = WorldDataLoader.ResolveSceneDirectory(options.Value.SceneDataPath);
        LoadedWorldData data = WorldDataLoader.Load(sceneDir);

        int totalNpcs = 0;
        int totalMonsters = 0;
        foreach (SceneDefinition scene in data.Scenes)
        {
            IReadOnlyList<NpcSpawnData> npcs = data.NpcsByMap.GetValueOrDefault(scene.MapId) ?? [];
            IReadOnlyList<MonsterSpawnData> monsters = data.MonstersByMap.GetValueOrDefault(scene.MapId) ?? [];
            _maps[scene.MapId] = new WorldMap(scene, npcs, monsters);
            totalNpcs += npcs.Count;
            totalMonsters += monsters.Count;
        }

        logger.LogInformation(
            "Loaded {MapCount} maps, {NpcCount} NPCs, and {MonsterCount} monster spawns from {SceneDir}",
            _maps.Count,
            totalNpcs,
            totalMonsters,
            sceneDir);
    }

    public IReadOnlyDictionary<ushort, WorldMap> Maps => _maps;

    public WorldMap? GetMap(ushort mapId) => _maps.GetValueOrDefault(mapId);

    public uint AllocatePlayerUiId()
    {
        while (true)
        {
            uint candidate = (uint)((Interlocked.Increment(ref _nextPlayerUiId) - 1) % 4999) + 1;
            if (!_playersByUiId.ContainsKey(candidate))
            {
                return candidate;
            }
        }
    }

    public void RegisterPlayer(PlayerSession session)
    {
        _playersByUiId[session.UiId] = session;
        _playersByAccount[session.Account] = session;
    }

    public void UnregisterPlayer(PlayerSession session)
    {
        _playersByUiId.TryRemove(new KeyValuePair<uint, PlayerSession>(session.UiId, session));
        _playersByAccount.TryRemove(new KeyValuePair<string, PlayerSession>(session.Account, session));
    }

    public PlayerSession? GetPlayerByAccount(string account)
        => _playersByAccount.GetValueOrDefault(account);

    public (ushort MapId, float X, float Z) GetStartingPosition(byte camp)
    {
        // Camp 0 = Athens (Athens_Newbie = 1), Camp 1 = Sparta (Sparta_Newbie = 51)
        ushort preferredMap = camp == 0 ? (ushort)1 : (ushort)51;
        if (_maps.TryGetValue(preferredMap, out WorldMap? map))
        {
            return camp == 0
                ? (map.Scene.AthensRevivalMap, map.Scene.AthensX, map.Scene.AthensZ)
                : (map.Scene.SpartaRevivalMap, map.Scene.SpartaX, map.Scene.SpartaZ);
        }

        return camp == 0
            ? ((ushort)1, 98f, -178f)
            : ((ushort)51, -108f, -122f);
    }
}
