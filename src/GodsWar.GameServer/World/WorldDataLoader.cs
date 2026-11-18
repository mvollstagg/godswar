using System.Globalization;
using System.Text;

namespace GodsWar.GameServer.World;

public sealed record SceneDefinition(
    ushort MapId,
    string Name,
    ushort AthensRevivalMap,
    float AthensX,
    float AthensZ,
    ushort SpartaRevivalMap,
    float SpartaX,
    float SpartaZ,
    ushort ClientMapId = 0);

public sealed record MonsterTemplate(
    string Name,
    byte Faction,
    float HpMultiplier);

public sealed record NpcSpawnData(
    uint UiId,
    ushort MapId,
    string Name,
    string Script,
    float X,
    float Y,
    float Z,
    float Atan2,
    byte Faction);

public sealed record MonsterSpawnData(
    uint UiId,
    ushort MapId,
    string ObjectName,
    uint Level,
    uint MaxHp,
    float X,
    float Z,
    float Atan2,
    byte Faction,
    int RefreshTimeSeconds,
    byte Mode = 0,
    int MoveTime = 0,
    int DropNum = 0,
    int DropProbability = 0,
    int ConfirmDropNum = 0,
    int ConfirmProbability = 0,
    string ConfirmItemTable = "NULL");

public sealed record PortalZone(
    float MinX,
    float MaxX,
    float MinZ,
    float MaxZ,
    string TargetMapName,
    ushort TargetMapId,
    float TargetX,
    float TargetZ)
{
    public bool Contains(float x, float z)
        => x >= MinX && x <= MaxX && z >= MinZ && z <= MaxZ;
}

public sealed record LoadedWorldData(
    IReadOnlyList<SceneDefinition> Scenes,
    IReadOnlyDictionary<ushort, IReadOnlyList<NpcSpawnData>> NpcsByMap,
    IReadOnlyDictionary<ushort, IReadOnlyList<MonsterSpawnData>> MonstersByMap,
    IReadOnlyDictionary<ushort, IReadOnlyList<PortalZone>> PortalsByMap);

/// <summary>
/// Loads map metadata (<c>SceneDefine.ini</c>), monster templates (<c>MonsterObject.ini</c>),
/// NPC spawns (<c>NPC00.INI</c>), and monster spawns (<c>Monster.ini</c>) from
/// <c>GW 2.0/Server Files (Compiled)/gameserver/Scene</c>.
/// </summary>
public static class WorldDataLoader
{
    public static string ResolveSceneDirectory(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath) && Directory.Exists(configuredPath))
        {
            return configuredPath;
        }

        string? dir = Directory.GetCurrentDirectory();
        for (int i = 0; i < 6 && dir is not null; i++)
        {
            string candidate = Path.Combine(dir, "GW 2.0", "Server Files (Compiled)", "gameserver", "Scene");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new DirectoryNotFoundException(
            "Could not locate 'GW 2.0/Server Files (Compiled)/gameserver/Scene'. Set GameServer:SceneDataPath in appsettings.json.");
    }

    public static LoadedWorldData Load(string sceneRootPath)
    {
        string sceneDefinePath = Path.Combine(sceneRootPath, "SceneDefine.ini");
        string monsterObjectPath = Path.Combine(sceneRootPath, "MonsterObject.ini");

        List<SceneDefinition> scenes = ParseSceneDefine(sceneDefinePath);
        Dictionary<string, MonsterTemplate> templates = File.Exists(monsterObjectPath)
            ? ParseMonsterObjects(monsterObjectPath)
            : new Dictionary<string, MonsterTemplate>(StringComparer.OrdinalIgnoreCase);

        var npcsByMap = new Dictionary<ushort, IReadOnlyList<NpcSpawnData>>();
        var monstersByMap = new Dictionary<ushort, IReadOnlyList<MonsterSpawnData>>();
        var portalsByMap = new Dictionary<ushort, IReadOnlyList<PortalZone>>();
        Dictionary<string, ushort> mapIdByName = scenes.ToDictionary(s => s.Name, s => s.MapId, StringComparer.OrdinalIgnoreCase);

        uint nextNpcId = 5000;
        uint nextMonsterId = 10000;

        foreach (SceneDefinition scene in scenes)
        {
            string sceneDir = Path.Combine(sceneRootPath, scene.Name);
            if (!Directory.Exists(sceneDir))
            {
                npcsByMap[scene.MapId] = [];
                monstersByMap[scene.MapId] = [];
                portalsByMap[scene.MapId] = [];
                continue;
            }

            string npcIni = Path.Combine(sceneDir, "NPC00.INI");
            if (File.Exists(npcIni))
            {
                List<NpcSpawnData> npcs = ParseNpcIni(npcIni, scene.MapId, ref nextNpcId);
                npcsByMap[scene.MapId] = npcs;
            }
            else
            {
                npcsByMap[scene.MapId] = [];
            }

            string monsterIni = Path.Combine(sceneDir, "Monster.ini");
            if (File.Exists(monsterIni))
            {
                List<MonsterSpawnData> monsters = ParseMonsterIni(monsterIni, scene.MapId, templates, ref nextMonsterId);
                monstersByMap[scene.MapId] = monsters;
            }
            else
            {
                monstersByMap[scene.MapId] = [];
            }

            string conveyIni = Path.Combine(sceneDir, "Convey.ini");
            portalsByMap[scene.MapId] = File.Exists(conveyIni)
                ? ParseConveyIni(conveyIni, mapIdByName)
                : [];
        }

        return new LoadedWorldData(scenes, npcsByMap, monstersByMap, portalsByMap);
    }

    private static List<SceneDefinition> ParseSceneDefine(string path)
    {
        var scenes = new List<SceneDefinition>();
        Dictionary<string, Dictionary<string, string>> sections = ReadIniSectionsText(path);

        foreach ((string sectionName, Dictionary<string, string> kv) in sections)
        {
            if (sectionName.Equals("default", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!kv.TryGetValue("id", out string? idStr) || !ushort.TryParse(idStr, out ushort mapId))
            {
                continue;
            }

            if (!kv.TryGetValue("name", out string? name) || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            scenes.Add(new SceneDefinition(
                mapId,
                name.Trim(),
                ParseUShort(kv, "AthensRevivalMap", mapId),
                ParseFloat(kv, "AthensX", 0f),
                ParseFloat(kv, "AthensZ", 0f),
                ParseUShort(kv, "SpartaRevivalMap", mapId),
                ParseFloat(kv, "SpartaX", 0f),
                ParseFloat(kv, "SpartaZ", 0f),
                (ushort)scenes.Count));
        }

        return scenes;
    }

    private static Dictionary<string, MonsterTemplate> ParseMonsterObjects(string path)
    {
        var templates = new Dictionary<string, MonsterTemplate>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, Dictionary<string, string>> sections = ReadIniSectionsText(path);

        foreach ((string sectionName, Dictionary<string, string> kv) in sections)
        {
            byte faction = (byte)Math.Clamp(ParseInt(kv, "Faction", 2), 0, 255);
            float hpMult = ParseFloat(kv, "HP", 1.0f);
            templates[sectionName] = new MonsterTemplate(sectionName, faction, hpMult);
        }

        return templates;
    }

    private static List<NpcSpawnData> ParseNpcIni(string path, ushort mapId, ref uint nextNpcId)
    {
        var npcs = new List<NpcSpawnData>();
        byte[] allBytes = File.ReadAllBytes(path);
        var sections = ReadIniSectionsRaw(allBytes);

        foreach ((string sectionName, Dictionary<string, byte[]> kv) in sections)
        {
            if (!sectionName.StartsWith("NPC", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string script = kv.TryGetValue("Script", out byte[]? rawScript)
                ? Encoding.Latin1.GetString(rawScript).Trim()
                : string.Empty;

            string name = kv.TryGetValue("Name", out byte[]? rawName)
                ? NpcNameResolver.Resolve(mapId, rawName, script)
                : sectionName;

            float x = ParseFloat(kv, "X", 0f);
            float y = ParseFloat(kv, "Y", 0f);
            float z = ParseFloat(kv, "Z", 0f);
            float atan2 = ParseFloat(kv, "Atan2", 0f);
            byte faction = (byte)Math.Clamp(ParseInt(kv, "Faction", 0), 0, 255);

            npcs.Add(new NpcSpawnData(nextNpcId++, mapId, name, script, x, y, z, atan2, faction));
        }

        return npcs;
    }

    private static List<MonsterSpawnData> ParseMonsterIni(
        string path,
        ushort mapId,
        IReadOnlyDictionary<string, MonsterTemplate> templates,
        ref uint nextMonsterId)
    {
        var monsters = new List<MonsterSpawnData>();
        Dictionary<string, Dictionary<string, string>> sections = ReadIniSectionsText(path);

        foreach ((string sectionName, Dictionary<string, string> kv) in sections)
        {
            if (!sectionName.StartsWith("Monster", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!kv.TryGetValue("Object", out string? objectName) || string.IsNullOrWhiteSpace(objectName))
            {
                continue;
            }

            objectName = objectName.Trim();
            uint level = (uint)Math.Max(1, ParseInt(kv, "Level", 1));
            float x = ParseFloat(kv, "X", 0f);
            float z = ParseFloat(kv, "Z", 0f);
            float atan2 = ParseFloat(kv, "Quaternion", 0f);
            int refreshTime = Math.Max(5, ParseInt(kv, "RefreshTime", 60));

            byte faction = 2;
            float hpMult = 1.0f;
            if (templates.TryGetValue(objectName, out MonsterTemplate? tpl))
            {
                faction = tpl.Faction;
                hpMult = tpl.HpMultiplier > 0 ? tpl.HpMultiplier : 1.0f;
            }

            int tableHp = GodsWar.GameServer.Game.GameTables.GetMonsterMaxHp((int)level);
            uint maxHp = (uint)Math.Max(80, (int)MathF.Round(tableHp * hpMult));
            byte mode = byte.TryParse(kv.GetValueOrDefault("Mode"), out byte m) ? m : (byte)0;
            int moveTime = int.TryParse(kv.GetValueOrDefault("MoveTime"), out int mt) ? mt : 0;
            int dropNum = int.TryParse(kv.GetValueOrDefault("DropNum"), out int dn) ? dn : 0;
            int dropProb = int.TryParse(kv.GetValueOrDefault("DropProbability"), out int dp) ? dp : 0;
            int confirmDropNum = int.TryParse(kv.GetValueOrDefault("ConfimDropNum"), out int cdn) ? cdn : 0;
            int confirmProb = int.TryParse(kv.GetValueOrDefault("ConfimProbability"), out int cp) ? cp : 0;
            string confirmTable = kv.GetValueOrDefault("ConfimItemTable", "NULL").Trim();

            monsters.Add(new MonsterSpawnData(
                nextMonsterId++,
                mapId,
                objectName,
                level,
                maxHp,
                x,
                z,
                atan2,
                faction,
                refreshTime,
                mode,
                moveTime,
                dropNum,
                dropProb,
                confirmDropNum,
                confirmProb,
                confirmTable));
        }

        return monsters;
    }

    private static Dictionary<string, Dictionary<string, string>> ReadIniSectionsText(string path)
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string>? current = null;

        foreach (string rawLine in File.ReadLines(path, Encoding.Latin1))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                string sec = line[1..^1].Trim();
                if (!result.TryGetValue(sec, out current))
                {
                    current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    result[sec] = current;
                }
                continue;
            }

            int eq = line.IndexOf('=');
            if (eq > 0 && current is not null)
            {
                string key = line[..eq].Trim();
                string val = line[(eq + 1)..].Trim();
                current[key] = val;
            }
        }

        return result;
    }

    private static List<(string Section, Dictionary<string, byte[]> Values)> ReadIniSectionsRaw(byte[] bytes)
    {
        var result = new List<(string, Dictionary<string, byte[]>)>();
        Dictionary<string, byte[]>? current = null;

        int start = 0;
        while (start < bytes.Length)
        {
            int end = Array.IndexOf(bytes, (byte)'\n', start);
            if (end < 0)
            {
                end = bytes.Length;
            }

            ReadOnlySpan<byte> line = bytes.AsSpan(start, end - start);
            start = end + 1;

            line = TrimAsciiWhitespace(line);
            if (line.Length == 0 || line[0] == (byte)';' || line[0] == (byte)'#')
            {
                continue;
            }

            if (line[0] == (byte)'[' && line[^1] == (byte)']')
            {
                string sec = Encoding.Latin1.GetString(line[1..^1]).Trim();
                current = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
                result.Add((sec, current));
                continue;
            }

            int eq = line.IndexOf((byte)'=');
            if (eq > 0 && current is not null)
            {
                string key = Encoding.Latin1.GetString(line[..eq]).Trim();
                ReadOnlySpan<byte> val = TrimAsciiWhitespace(line[(eq + 1)..]);
                current[key] = val.ToArray();
            }
        }

        return result;
    }

    private static ReadOnlySpan<byte> TrimAsciiWhitespace(ReadOnlySpan<byte> span)
    {
        int s = 0;
        int e = span.Length - 1;
        while (s <= e && (span[s] == (byte)' ' || span[s] == (byte)'\t' || span[s] == (byte)'\r'))
        {
            s++;
        }
        while (e >= s && (span[e] == (byte)' ' || span[e] == (byte)'\t' || span[e] == (byte)'\r'))
        {
            e--;
        }
        return s > e ? ReadOnlySpan<byte>.Empty : span[s..(e + 1)];
    }

    private static ushort ParseUShort(Dictionary<string, string> kv, string key, ushort fallback)
        => kv.TryGetValue(key, out string? v) && ushort.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort parsed)
            ? parsed
            : fallback;

    private static int ParseInt(Dictionary<string, string> kv, string key, int fallback)
        => kv.TryGetValue(key, out string? v) && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
            ? parsed
            : fallback;

    private static int ParseInt(Dictionary<string, byte[]> kv, string key, int fallback)
        => kv.TryGetValue(key, out byte[]? raw)
           && int.TryParse(Encoding.Latin1.GetString(raw).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
            ? parsed
            : fallback;

    private static float ParseFloat(Dictionary<string, string> kv, string key, float fallback)
    {
        if (!kv.TryGetValue(key, out string? v))
        {
            return fallback;
        }

        v = v.Trim().TrimEnd('f', 'F');
        return float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
            ? parsed
            : fallback;
    }

    private static float ParseFloat(Dictionary<string, byte[]> kv, string key, float fallback)
    {
        if (!kv.TryGetValue(key, out byte[]? raw))
        {
            return fallback;
        }

        string v = StripInlineComment(Encoding.Latin1.GetString(raw)).Trim().TrimEnd('f', 'F');
        return float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
            ? parsed
            : fallback;
    }

    private static string StripInlineComment(string raw)
    {
        int semi = raw.IndexOf(';');
        return semi >= 0 ? raw[..semi] : raw;
    }

    private static List<PortalZone> ParseConveyIni(string path, Dictionary<string, ushort> mapIdByName)
    {
        var portals = new List<PortalZone>();
        foreach ((string section, Dictionary<string, byte[]> kv) in ReadIniSectionsRaw(File.ReadAllBytes(path)))
        {
            if (!section.StartsWith("Convey", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!kv.TryGetValue("to", out byte[]? toBytes))
            {
                continue;
            }

            string toMap = StripInlineComment(Encoding.Latin1.GetString(toBytes)).Trim();
            if (!mapIdByName.TryGetValue(toMap, out ushort targetMapId))
            {
                continue;
            }

            float left = ParseFloat(kv, "rect_left", 0f);
            float right = ParseFloat(kv, "rect_right", 0f);
            float top = ParseFloat(kv, "rect_top", 0f);
            float bottom = ParseFloat(kv, "rect_bottom", 0f);
            float toX = ParseFloat(kv, "to_x", 0f);
            float toZ = ParseFloat(kv, "to_z", 0f);

            portals.Add(new PortalZone(
                Math.Min(left, right),
                Math.Max(left, right),
                Math.Min(bottom, top),
                Math.Max(bottom, top),
                toMap,
                targetMapId,
                toX,
                toZ));
        }

        return portals;
    }
}
