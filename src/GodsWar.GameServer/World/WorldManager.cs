using System.Collections.Concurrent;
using GodsWar.GameServer.Game;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GodsWar.GameServer.World;

/// <summary>
/// Manages all loaded <see cref="WorldMap"/> instances, <see cref="GameDataCatalog"/>, and active <see cref="PlayerSession"/>s.
/// </summary>
public sealed class WorldManager
{
    private readonly Dictionary<ushort, WorldMap> _maps = [];
    private readonly Dictionary<string, WorldMap> _mapsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<uint, PlayerSession> _playersByUiId = new();
    private readonly ConcurrentDictionary<string, PlayerSession> _playersByAccount = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, PlayerSession> _playersByName = new(StringComparer.OrdinalIgnoreCase);
    private int _nextPlayerUiId;

    public WorldManager(IOptions<GameServerOptions> options, ILogger<WorldManager> logger)
    {
        string sceneDir = WorldDataLoader.ResolveSceneDirectory(options.Value.SceneDataPath);
        LoadedWorldData data = WorldDataLoader.Load(sceneDir);
        Catalog = GameDataCatalog.LoadFromSceneParent(sceneDir);

        int totalNpcs = 0;
        int totalMonsters = 0;
        int totalPortals = 0;
        foreach (SceneDefinition scene in data.Scenes)
        {
            IReadOnlyList<NpcSpawnData> npcs = data.NpcsByMap.GetValueOrDefault(scene.MapId) ?? [];
            IReadOnlyList<MonsterSpawnData> monsters = data.MonstersByMap.GetValueOrDefault(scene.MapId) ?? [];
            IReadOnlyList<PortalZone> portals = data.PortalsByMap.GetValueOrDefault(scene.MapId) ?? [];
            var map = new WorldMap(scene, npcs, monsters, portals);
            _maps[scene.MapId] = map;
            _mapsByName[scene.Name] = map;
            totalNpcs += npcs.Count;
            totalMonsters += monsters.Count;
            totalPortals += portals.Count;
        }

        logger.LogInformation(
            "Loaded {MapCount} maps, {NpcCount} NPCs, {MonsterCount} monsters, {PortalCount} portals, {ItemCount} items, {MagicCount} magics, {QuestCount} quests, and {ForgeCount} forge recipes from {SceneDir}",
            _maps.Count,
            totalNpcs,
            totalMonsters,
            totalPortals,
            Catalog.Items.Count,
            Catalog.Magics.Count,
            Catalog.Quests.Count,
            Catalog.EquipForges.Count,
            sceneDir);
    }

    public GameDataCatalog Catalog { get; }
    public IReadOnlyDictionary<ushort, WorldMap> Maps => _maps;

    public PlayerSession? GetPlayerByUiId(uint uiId) => _playersByUiId.GetValueOrDefault(uiId);

    public PlayerSession? GetPlayerByName(string name) => _playersByName.GetValueOrDefault(name);

    public IEnumerable<PlayerSession> GetAllPlayers() => _playersByUiId.Values;

    public WorldMap? GetMap(ushort mapId) => _maps.GetValueOrDefault(mapId);

    public WorldMap? GetMapByName(string name) => _mapsByName.GetValueOrDefault(name);

    public NpcSpawnData? FindNpc(uint npcId)
    {
        foreach (WorldMap map in _maps.Values)
        {
            NpcSpawnData? npc = map.GetNpc(npcId);
            if (npc is not null)
            {
                return npc;
            }
        }

        return null;
    }

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
        _playersByName[session.Character.Name] = session;
    }

    public void UnregisterPlayer(PlayerSession session)
    {
        _playersByUiId.TryRemove(new KeyValuePair<uint, PlayerSession>(session.UiId, session));
        _playersByAccount.TryRemove(new KeyValuePair<string, PlayerSession>(session.Account, session));
        _playersByName.TryRemove(new KeyValuePair<string, PlayerSession>(session.Character.Name, session));
    }

    public PlayerSession? GetPlayerByAccount(string account)
        => _playersByAccount.GetValueOrDefault(account);

    public ushort GetClientMapId(ushort serverMapId)
        => _maps.TryGetValue(serverMapId, out WorldMap? map) ? map.Scene.ClientMapId : serverMapId;

    public int XpRatePercent { get; set; } = 10000;

    public string WelcomeMessage { get; set; } = "Welcome to GodsWar Online! (100x XP Event Active)";

    public IReadOnlyCollection<PlayerSession> OnlinePlayers => (IReadOnlyCollection<PlayerSession>)_playersByUiId.Values;

    public (ushort MapId, float X, float Z) GetStartingPosition(byte camp)
    {
        // Camp 0 = Sparta (Sparta_Newbie = server MapId 51, ClientMapId 4),
        // Camp 1 = Athens (Athens_Newbie = server MapId 1, ClientMapId 2).
        string newbieName = camp == 0 ? "Sparta_Newbie" : "Athens_Newbie";
        if (_mapsByName.TryGetValue(newbieName, out WorldMap? map))
        {
            return camp == 0
                ? (map.Scene.MapId, map.Scene.SpartaX, map.Scene.SpartaZ)
                : (map.Scene.MapId, map.Scene.AthensX, map.Scene.AthensZ);
        }

        return camp == 0
            ? ((ushort)51, -108f, -122f)
            : ((ushort)1, 98f, -178f);
    }
}
