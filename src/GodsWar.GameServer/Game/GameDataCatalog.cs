using System.Globalization;
using System.Text;
using System.Xml.Linq;
using GodsWar.Protocol.Packets;

namespace GodsWar.GameServer.Game;

public enum EquipmentSlotType
{
    Unknown = -1,
    Head = 0,
    Amulet = 1,
    Glove = 2,
    Armor = 3,
    Cuff = 4,
    Girdle = 5,
    Shoes = 6,
    Leggins = 7,
    Ring = 8,
    Weapon = 10,
    Shield = 11,
    Consume = 12,
    Cloth = 13,
}

public sealed record ItemBaseDef(
    int Id,
    string Name,
    EquipmentSlotType SlotType,
    bool IsEquip,
    bool IsConsumable,
    byte Hand,
    int RequiredClassMask,
    int RequiredLevel,
    int Faction,
    byte MaxOverlap,
    int Money,
    int SkillId,
    int[] AttackByLevel,
    int[] DefendByLevel,
    int[] MagicAttackByLevel,
    int[] MagicDefendByLevel,
    int[] MaxHpByLevel,
    int[] MaxMpByLevel,
    bool IsSkillBook = false,
    int PrevSkillId = -1,
    int SkillLearnId = -1,
    int GoldBoxIdx = -1,
    int KeyIdx = -1);

public sealed record PassiveSkillDef(
    int Id,
    string Name,
    int PrefixId,
    int PrefixRank,
    int TotalRank);

public sealed record ItemAppendDef(
    int Id,
    int Type,
    bool IsPercent,
    float[] ValuesByLevel);

public sealed record MagicDef(
    int Id,
    string Name,
    byte Property, // 0 = physical, 1 = magic, 2 = support/other
    int MpCost,
    float CoolingSeconds,
    float Distance,
    float Range,
    float Power1,
    float Power2,
    int StatusId,
    int StatusOdds);

public sealed record StatusDef(
    int Id,
    string Name,
    int DurationSeconds,
    int IntervalSeconds,
    int AddHp,
    int DecHp,
    int AddMp,
    int DecMp,
    float AttackPct,
    float DefendPct,
    float MagicAttackPct,
    float MagicDefendPct);

public sealed record DropEntryDef(
    int BaseId,
    int Probability,
    int[] Appends,
    byte AppendLevel,
    byte BaseLevel);

/// <summary>
/// Loads item, append, skill/magic, status, and drop definitions from
/// <c>GW 2.0/Server Files (Compiled)/gameserver/</c>.
/// </summary>
public sealed class GameDataCatalog
{
    public IReadOnlyDictionary<int, ItemBaseDef> Items { get; }
    public IReadOnlyDictionary<int, ItemAppendDef> Appends { get; }
    public IReadOnlyDictionary<int, MagicDef> Magics { get; }
    public IReadOnlyDictionary<int, StatusDef> Statuses { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<DropEntryDef>> DropTables { get; }
    public IReadOnlyDictionary<int, EquipForgeDef> EquipForges { get; }
    public IReadOnlyDictionary<int, List<ShopItemDef>> ShopItemLists { get; }
    public IReadOnlyDictionary<int, QuestDef> Quests { get; }
    public IReadOnlyDictionary<string, NpcScriptDef> NpcScripts { get; }
    public IReadOnlyDictionary<int, PassiveSkillDef> PassiveSkills { get; }
    public IReadOnlyDictionary<int, MallItemBaseDef> MallShopItems { get; }
    public IReadOnlyList<AssociationDiscountDef> AssociationDiscounts { get; }
    public IReadOnlyDictionary<int, BijouForgeDef> BijouForges { get; }
    public MallScriptConfig MallConfig { get; }
    public IReadOnlyDictionary<uint, GoldBoxConfig> GoldBoxes { get; }

    private GameDataCatalog(
        Dictionary<int, ItemBaseDef> items,
        Dictionary<int, ItemAppendDef> appends,
        Dictionary<int, MagicDef> magics,
        Dictionary<int, StatusDef> statuses,
        Dictionary<string, IReadOnlyList<DropEntryDef>> dropTables,
        Dictionary<int, EquipForgeDef> equipForges,
        Dictionary<int, List<ShopItemDef>> shopItemLists,
        Dictionary<int, QuestDef> quests,
        Dictionary<string, NpcScriptDef> npcScripts,
        Dictionary<int, PassiveSkillDef> passiveSkills,
        Dictionary<int, MallItemBaseDef> mallShopItems,
        List<AssociationDiscountDef> associationDiscounts,
        Dictionary<int, BijouForgeDef> bijouForges,
        MallScriptConfig mallConfig,
        Dictionary<uint, GoldBoxConfig> goldBoxes)
    {
        Items = items;
        Appends = appends;
        Magics = magics;
        Statuses = statuses;
        DropTables = dropTables;
        EquipForges = equipForges;
        ShopItemLists = shopItemLists;
        Quests = quests;
        NpcScripts = npcScripts;
        PassiveSkills = passiveSkills;
        MallShopItems = mallShopItems;
        AssociationDiscounts = associationDiscounts;
        BijouForges = bijouForges;
        MallConfig = mallConfig;
        GoldBoxes = goldBoxes;
    }

    public static GameDataCatalog LoadFromSceneParent(string sceneDir)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        string gameServerRoot = Directory.GetParent(sceneDir)?.FullName ?? sceneDir;
        var items = LoadItems(Path.Combine(gameServerRoot, "Item", "ItemBaseAttribute.xml"));
        var appends = LoadAppends(Path.Combine(gameServerRoot, "Item", "ItemAppendAttribute.xml"));
        var magics = LoadMagics(Path.Combine(gameServerRoot, "Skill", "Magic.ini"));
        var statuses = LoadStatuses(Path.Combine(gameServerRoot, "Skill", "Status.ini"));
        var passiveSkills = LoadPassiveSkills(Path.Combine(gameServerRoot, "Skill", "Skill.ini"));
        var drops = LoadDropTables(Path.Combine(gameServerRoot, "Drop"));
        var forges = ScriptDataLoader.LoadEquipForges(Path.Combine(gameServerRoot, "Item", "EquipForge.xml"));
        var bijouForges = ScriptDataLoader.LoadBijouForges(Path.Combine(gameServerRoot, "Item", "BijouForge.xml"));
        (var mallShopItems, var assocDiscounts) = ScriptDataLoader.LoadItemShop(Path.Combine(gameServerRoot, "Item", "ItemShop.xml"));
        var shops = ScriptDataLoader.LoadShopItemLists(Path.Combine(gameServerRoot, "Script", "Item.py"));
        var quests = ScriptDataLoader.LoadQuests(Path.Combine(gameServerRoot, "Script", "Quest.py"));
        var npcScripts = ScriptDataLoader.LoadNpcScripts(Path.Combine(gameServerRoot, "Script"));
        var mallConfig = ScriptDataLoader.LoadMallScripts(Path.Combine(gameServerRoot, "Script"));
        var goldBoxes = ScriptDataLoader.LoadGoldBoxes(Path.Combine(gameServerRoot, "Drop"));
        return new GameDataCatalog(
            items, appends, magics, statuses, drops, forges, shops, quests, npcScripts,
            passiveSkills, mallShopItems, assocDiscounts, bijouForges, mallConfig, goldBoxes);
    }

    public ItemEquipSlot CreateDropItem(string tableKey, Random? rng = null)
    {
        rng ??= Random.Shared;
        if (!DropTables.TryGetValue(tableKey, out IReadOnlyList<DropEntryDef>? entries) || entries.Count == 0)
        {
            return ItemEquipSlot.Empty;
        }

        int roll = rng.Next(100);
        foreach (DropEntryDef entry in entries)
        {
            if (entry.Probability >= 100 - roll && entry.BaseId > 0)
            {
                return new ItemEquipSlot(
                    entry.BaseId,
                    entry.Appends.Length > 0 ? entry.Appends[0] : -1,
                    entry.Appends.Length > 1 ? entry.Appends[1] : -1,
                    entry.Appends.Length > 2 ? entry.Appends[2] : -1,
                    entry.Appends.Length > 3 ? entry.Appends[3] : -1,
                    entry.Appends.Length > 4 ? entry.Appends[4] : -1,
                    Math.Max((byte)1, entry.BaseLevel),
                    entry.AppendLevel,
                    0,
                    1);
            }
        }

        return ItemEquipSlot.Empty;
    }

    private static Dictionary<int, ItemBaseDef> LoadItems(string path)
    {
        var map = new Dictionary<int, ItemBaseDef>();
        if (!File.Exists(path))
        {
            return map;
        }

        XDocument doc = XDocument.Load(path);
        foreach (XElement el in doc.Descendants())
        {
            XAttribute? idAttr = el.Attribute("ID");
            if (idAttr is null || !int.TryParse(idAttr.Value, out int id))
            {
                continue;
            }

            string typeStr = el.Attribute("Type")?.Value ?? string.Empty;
            EquipmentSlotType slotType = typeStr.ToLowerInvariant() switch
            {
                "weapon" => EquipmentSlotType.Weapon,
                "head" => EquipmentSlotType.Head,
                "shield" => EquipmentSlotType.Shield,
                "amulet" => EquipmentSlotType.Amulet,
                "glove" => EquipmentSlotType.Glove,
                "armor" => EquipmentSlotType.Armor,
                "cloth" => EquipmentSlotType.Cloth,
                "cuff" => EquipmentSlotType.Cuff,
                "girdle" => EquipmentSlotType.Girdle,
                "shoes" => EquipmentSlotType.Shoes,
                "leggins" => EquipmentSlotType.Leggins,
                "ring" => EquipmentSlotType.Ring,
                "consume item" => EquipmentSlotType.Consume,
                _ => EquipmentSlotType.Unknown,
            };

            bool isEquip = ParseInt(el.Attribute("Equip")?.Value) == 1;
            bool isConsume = ParseInt(el.Attribute("Use")?.Value) == 1 || slotType == EquipmentSlotType.Consume;
            byte hand = (byte)Math.Clamp(ParseInt(el.Attribute("Hand")?.Value), 0, 255);
            int reqClass = ParseClassMask(el.Attribute("Class")?.Value);
            int reqLv = ParseInt(el.Attribute("PlayLv")?.Value, 1);
            int faction = ParseInt(el.Attribute("Faction")?.Value, -1);
            byte maxOverlap = (byte)Math.Clamp(ParseInt(el.Attribute("Overlap")?.Value, 1), 1, 99);
            int money = ParseInt(el.Attribute("Money")?.Value);
            int skillId = ParseInt(el.Attribute("Skill")?.Value, -1);
            int prevSkillId = ParseInt(el.Attribute("PrevSkillID")?.Value, -1);
            int skillLearnId = ParseInt(el.Attribute("SkillID")?.Value, -1);
            bool isSkillBook = prevSkillId >= 0 || skillLearnId >= 0 || el.Name.LocalName.StartsWith("SkillBook", StringComparison.OrdinalIgnoreCase);
            int goldBoxIdx = ParseInt(el.Attribute("GoldBoxIdx")?.Value, -1);
            int keyIdx = ParseInt(el.Attribute("KEY")?.Value, -1);

            map[id] = new ItemBaseDef(
                id,
                el.Name.LocalName,
                slotType,
                isEquip,
                isConsume,
                hand,
                reqClass,
                reqLv,
                faction,
                maxOverlap,
                money,
                skillId,
                ParseIntArray(el.Attribute("Attack")?.Value),
                ParseIntArray(el.Attribute("Defence")?.Value),
                ParseIntArray(el.Attribute("MagicAttack")?.Value),
                ParseIntArray(el.Attribute("MagicDefend")?.Value),
                ParseIntArray(el.Attribute("MaxHP")?.Value),
                ParseIntArray(el.Attribute("MaxMP")?.Value),
                isSkillBook,
                prevSkillId,
                skillLearnId,
                goldBoxIdx,
                keyIdx);
        }

        return map;
    }

    private static Dictionary<int, PassiveSkillDef> LoadPassiveSkills(string path)
    {
        var map = new Dictionary<int, PassiveSkillDef>();
        if (!File.Exists(path))
        {
            return map;
        }

        foreach ((string section, Dictionary<string, string> kv) in ReadIniSections(path))
        {
            if (!int.TryParse(section, out int skillId))
            {
                continue;
            }

            map[skillId] = new PassiveSkillDef(
                skillId,
                kv.GetValueOrDefault("Name", $"Skill_{skillId}"),
                ParseInt(kv.GetValueOrDefault("PrefixID"), skillId),
                ParseInt(kv.GetValueOrDefault("RrefixRank"), 0),
                ParseInt(kv.GetValueOrDefault("TotalRank"), 0));
        }

        return map;
    }

    private static Dictionary<int, ItemAppendDef> LoadAppends(string path)
    {
        var map = new Dictionary<int, ItemAppendDef>();
        if (!File.Exists(path))
        {
            return map;
        }

        XDocument doc = XDocument.Load(path);
        foreach (XElement el in doc.Descendants())
        {
            if (!int.TryParse(el.Attribute("ID")?.Value, out int id))
            {
                continue;
            }

            int type = ParseInt(el.Attribute("Type")?.Value);
            bool isPercent = ParseInt(el.Attribute("Flag")?.Value) == 1;
            var values = new float[12];
            for (int lv = 1; lv <= 12; lv++)
            {
                string? raw = el.Attribute($"L{lv}")?.Value;
                if (!string.IsNullOrWhiteSpace(raw) &&
                    float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float val))
                {
                    values[lv - 1] = val;
                }
            }

            map[id] = new ItemAppendDef(id, type, isPercent, values);
        }

        return map;
    }

    private static Dictionary<int, MagicDef> LoadMagics(string path)
    {
        var map = new Dictionary<int, MagicDef>();
        if (!File.Exists(path))
        {
            return map;
        }

        foreach ((string section, Dictionary<string, string> kv) in ReadIniSections(path))
        {
            if (!int.TryParse(section, out int magicId))
            {
                continue;
            }

            map[magicId] = new MagicDef(
                magicId,
                kv.GetValueOrDefault("Name", $"Magic_{magicId}"),
                (byte)Math.Clamp(ParseInt(kv.GetValueOrDefault("Property")), 0, 255),
                ParseInt(kv.GetValueOrDefault("MP")),
                ParseFloat(kv.GetValueOrDefault("CoolingTime"), 1.0f),
                ParseFloat(kv.GetValueOrDefault("Distance"), 3.0f),
                ParseFloat(kv.GetValueOrDefault("Range")),
                ParseFloat(kv.GetValueOrDefault("Power1")),
                ParseFloat(kv.GetValueOrDefault("Power2")),
                ParseInt(kv.GetValueOrDefault("Status"), -1),
                ParseInt(kv.GetValueOrDefault("StatusOdds")));
        }

        return map;
    }

    private static Dictionary<int, StatusDef> LoadStatuses(string path)
    {
        var map = new Dictionary<int, StatusDef>();
        if (!File.Exists(path))
        {
            return map;
        }

        foreach ((string section, Dictionary<string, string> kv) in ReadIniSections(path))
        {
            if (!int.TryParse(section, out int statusId))
            {
                continue;
            }

            map[statusId] = new StatusDef(
                statusId,
                kv.GetValueOrDefault("Name", $"Status_{statusId}"),
                ParseInt(kv.GetValueOrDefault("Time"), 10),
                Math.Max(1, ParseInt(kv.GetValueOrDefault("Interval"), 2)),
                ParseInt(kv.GetValueOrDefault("AddHP")),
                ParseInt(kv.GetValueOrDefault("DecHP")),
                ParseInt(kv.GetValueOrDefault("AddMP")),
                ParseInt(kv.GetValueOrDefault("DecMP")),
                ParseFloat(kv.GetValueOrDefault("Attack")),
                ParseFloat(kv.GetValueOrDefault("Defend")),
                ParseFloat(kv.GetValueOrDefault("MagicAttack")),
                ParseFloat(kv.GetValueOrDefault("MagicDefend")));
        }

        return map;
    }

    private static Dictionary<string, IReadOnlyList<DropEntryDef>> LoadDropTables(string dropDir)
    {
        var tables = new Dictionary<string, IReadOnlyList<DropEntryDef>>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(dropDir))
        {
            return tables;
        }

        foreach (string file in Directory.EnumerateFiles(dropDir, "Drop*.ini"))
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            string key = fileName.StartsWith("Drop", StringComparison.OrdinalIgnoreCase)
                ? fileName[4..]
                : fileName;

            Dictionary<string, Dictionary<string, string>> sections = ReadIniSections(file);
            if (!sections.TryGetValue("sys", out Dictionary<string, string>? sys) ||
                !int.TryParse(sys.GetValueOrDefault("Count"), out int count))
            {
                continue;
            }

            var list = new List<DropEntryDef>(count);
            for (int i = 0; i < count; i++)
            {
                if (!sections.TryGetValue(i.ToString(CultureInfo.InvariantCulture), out Dictionary<string, string>? sec))
                {
                    continue;
                }

                int baseId = ParseInt(sec.GetValueOrDefault("BaseID"));
                int prob = ParseInt(sec.GetValueOrDefault("Probability"));
                int[] appends = ParseIntArray(sec.GetValueOrDefault("Append"));
                byte appLv = (byte)Math.Clamp(ParseInt(sec.GetValueOrDefault("AppLV")), 0, 12);
                byte baseLv = (byte)Math.Clamp(ParseInt(sec.GetValueOrDefault("BaseLV"), 1), 1, 5);
                if (baseId > 0)
                {
                    list.Add(new DropEntryDef(baseId, prob, appends, appLv, baseLv));
                }
            }

            tables[key] = list;
        }

        return tables;
    }

    private static Dictionary<string, Dictionary<string, string>> ReadIniSections(string path)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        string current = string.Empty;

        foreach (string rawLine in File.ReadLines(path, Encoding.GetEncoding(936)))
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith(';') || line.StartsWith("//"))
            {
                continue;
            }

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                current = line[1..^1].Trim();
                if (!sections.ContainsKey(current))
                {
                    sections[current] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                continue;
            }

            int eq = line.IndexOf('=');
            if (eq > 0 && !string.IsNullOrEmpty(current))
            {
                string key = line[..eq].Trim();
                string val = line[(eq + 1)..].Trim().TrimEnd(';');
                sections[current][key] = val;
            }
        }

        return sections;
    }

    private static int ParseClassMask(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return 0b1111;
        }

        int mask = 0;
        foreach (string part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(part.Trim(), out int cls) && cls is >= 0 and <= 3)
            {
                mask |= 1 << cls;
            }
        }
        return mask == 0 ? 0b1111 : mask;
    }

    private static int[] ParseIntArray(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        string[] parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var arr = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            int.TryParse(parts[i].Trim(), out arr[i]);
        }
        return arr;
    }

    private static int ParseInt(string? raw, int fallback = 0)
        => int.TryParse(raw?.Trim(), out int val) ? val : fallback;

    private static float ParseFloat(string? raw, float fallback = 0f)
        => float.TryParse(raw?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float val) ? val : fallback;
}
