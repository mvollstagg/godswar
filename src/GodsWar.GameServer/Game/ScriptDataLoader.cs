using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using GodsWar.Protocol.Packets;

namespace GodsWar.GameServer.Game;

public sealed record EquipForgeDef(
    int Id,
    int NextId,
    int BadId,
    int Probability,
    int[] BaseProbAdd,
    int[] AppendProbAdd,
    int AMoney,
    int[] BMoney,
    int[] CMoney);

public sealed record ShopItemDef(
    int BaseId,
    byte BaseLevel,
    byte AppendLevel,
    byte Overlap,
    byte Binding,
    int[] TradeTypes,
    int[] ConsumeAmounts);

public sealed class QuestDef
{
    public int QuestId { get; set; }
    public int QuestSort { get; set; }
    public int PrevQuestId { get; set; } = -1;
    public int NextQuestId { get; set; } = -1;
    public int MinLevel { get; set; } = 1;
    public int MaxLevel { get; set; } = 200;
    public int RequiredClass { get; set; } = -1;
    public int RequiredFaction { get; set; } = -1;
    public int SpecialFlags { get; set; }
    public short[] Items { get; } = [-1, -1, -1, -1];
    public short[] Creatures { get; } = [-1, -1, -1, -1];
    public short[] ItemCounts { get; } = [0, 0, 0, 0];
    public short[] CreatureCounts { get; } = [0, 0, 0, 0];
    public int Money { get; set; }
    public int Bijou { get; set; }
    public int Repeat { get; set; } = 1;
    public int RewardMoney { get; set; }
    public int RewardBijou { get; set; }
    public ItemEquipSlot[] RewardItems { get; } =
    [
        ItemEquipSlot.Empty, ItemEquipSlot.Empty, ItemEquipSlot.Empty, ItemEquipSlot.Empty
    ];

    // QuestReward fields (QR<id>)
    public int Credit { get; set; }
    public int Contribute { get; set; }
    public int Exp { get; set; }
    public int SkillPoint { get; set; }
    public int RewardSpecialFlags { get; set; }
    public int RewardSpecialCount { get; set; }
    public int UnionContribute { get; set; }
}

public sealed record NpcScriptDef(
    string ScriptKey,
    int NpcFlag,
    int SysFlag,
    IReadOnlyList<int> ItemListIds,
    IReadOnlyList<int> QuestIds,
    IReadOnlyList<int> RewardQuestIds,
    string TalkKey);

public sealed record MallItemBaseDef(int Id, int Discount, byte New, byte Hot);

public sealed record AssociationDiscountDef(byte Level, int StoneDiscount, int GloryDiscount);

public sealed record BijouForgeDef(int Id, int NextId, int MaterialType, int MaterialProbAdd);

public sealed record MallScriptConfig(
    IReadOnlyList<int> AthensMallItemLists,
    IReadOnlyList<byte> AthensMallSellTypes,
    IReadOnlyList<int> SpartaMallItemLists,
    IReadOnlyList<byte> SpartaMallSellTypes,
    IReadOnlyList<int> AthensAssociationItemLists,
    IReadOnlyList<int> SpartaAssociationItemLists);

public sealed record DialEntryDef(
    uint ItemId,
    int BaseId,
    int[] Appends,
    byte AppendLevel,
    byte BaseLevel,
    byte Binding,
    byte Overlap,
    bool IsNonsuch,
    int RandInDial,
    int Probability);

public sealed record GoldBoxConfig(
    uint GoldBoxId,
    int KeyBoxId,
    DialEntryDef DefaultItem,
    IReadOnlyList<DialEntryDef> NormalItems,
    IReadOnlyList<DialEntryDef> NonsuchItems);

public static partial class ScriptDataLoader
{
    private static readonly Dictionary<string, int> NpcFlagConstants = new(StringComparer.Ordinal)
    {
        ["NPC_FLAG_NONE"] = 0,
        ["NPC_FLAG_QUESTGIVER"] = 1,
        ["NPC_FLAG_QUESTRESPONDER"] = 2,
        ["NPC_FLAG_VENDOR"] = 4,
        ["NPC_FLAG_TRAINER"] = 8,
        ["NPC_FLAG_GUARD"] = 16,
        ["NPC_FLAG_BANKER"] = 32,
        ["NPC_FLAG_AUCTIONEER"] = 64,
        ["NPC_FLAG_CONSORTIA"] = 128,
        ["NPC_FLAG_CONSORTIA_LEVELUP"] = 256,
        ["NPC_FLAG_SYS"] = 512,
        ["NPC_FLAG_SYS_TRANMIT"] = 1,
        ["NPC_FLAG_SYS_CREDIT_EXCHANGE"] = 2,
        ["NPC_FLAG_SYS_GRANT_OFFICAL"] = 4,
        ["NPC_FLAG_SYS_OFFICAL_SALARY"] = 8,
        ["NPC_FLAG_SYS_ALTAR"] = 16,
    };

    private static readonly Dictionary<string, int> QuestFlagConstants = new(StringComparer.Ordinal)
    {
        ["QUEST_SPECIAL_FLAGS_NONE"] = 0,
        ["QUEST_SPECIAL_FLAGS_DELIVER"] = 1,
        ["QUEST_SPECIAL_FLAGS_MAP_EXPLORER"] = 2,
        ["QUEST_SPECIAL_FLAGS_SPEAKTO"] = 4,
        ["QUEST_SPECIAL_FLAGS_KILL_OR_CAST"] = 8,
        ["QUEST_SPECIAL_FLAGS_TIMED"] = 16,
        ["QUEST_SPECIAL_FLAGS_REPEATABLE"] = 32,
        ["QUEST_SPECIAL_FLAGS_REPUTATION"] = 64,
        ["QUEST_SPECIAL_FLAGS_LOOP"] = 128,
        ["QUEST_FACTION_NONE"] = -1,
        ["QUEST_FACTION_Q_Athens"] = 0,
        ["QUEST_FACTION_Q_Sparta"] = 1,
        ["QUEST_CLASS_NONE"] = -1,
        ["QUEST_CLASS_FIGHTER1"] = 0,
        ["QUEST_CLASS_FIGHTER2"] = 1,
        ["QUEST_CLASS_SCHOLAR1"] = 2,
        ["QUEST_CLASS_SCHOLAR2"] = 3,
        ["QUEST_CLASS_FIGHTER"] = 4,
        ["QUEST_CLASS_SCHOLAR"] = 5,
    };

    public static Dictionary<int, EquipForgeDef> LoadEquipForges(string xmlPath)
    {
        var result = new Dictionary<int, EquipForgeDef>();
        if (!File.Exists(xmlPath))
        {
            return result;
        }

        XDocument doc = XDocument.Load(xmlPath);
        if (doc.Root is null)
        {
            return result;
        }

        foreach (XElement el in doc.Root.Elements())
        {
            int id = ParseAttrInt(el, "ID");
            if (id <= 0)
            {
                continue;
            }

            result[id] = new EquipForgeDef(
                id,
                ParseAttrInt(el, "NextID"),
                ParseAttrInt(el, "BadID"),
                ParseAttrInt(el, "Probability"),
                ParseCsvIntArray(el.Attribute("BaseProyAdd")?.Value),
                ParseCsvIntArray(el.Attribute("AppendProyAdd")?.Value),
                ParseAttrInt(el, "Amoney"),
                ParseCsvIntArray(el.Attribute("Bmoney")?.Value),
                ParseCsvIntArray(el.Attribute("Cmoney")?.Value));
        }

        return result;
    }

    public static Dictionary<int, List<ShopItemDef>> LoadShopItemLists(string itemPyPath)
    {
        var result = new Dictionary<int, List<ShopItemDef>>();
        if (!File.Exists(itemPyPath))
        {
            return result;
        }

        var working = new Dictionary<int, List<ShopItemBuilder>>();
        foreach (string rawLine in File.ReadLines(itemPyPath))
        {
            string line = StripComment(rawLine).Trim();
            if (line.Length == 0)
            {
                continue;
            }

            Match decl = ItemListDeclRegex().Match(line);
            if (decl.Success && int.TryParse(decl.Groups[1].Value, out int listId))
            {
                int count = Regex.Matches(decl.Groups[2].Value, @"ItemData\s*\(\s*\)").Count;
                var list = new List<ShopItemBuilder>(count);
                for (int i = 0; i < count; i++)
                {
                    list.Add(new ShopItemBuilder());
                }
                working[listId] = list;
                continue;
            }

            Match assign = ItemListAssignRegex().Match(line);
            if (!assign.Success)
            {
                continue;
            }

            int lid = int.Parse(assign.Groups[1].Value, CultureInfo.InvariantCulture);
            int idx = int.Parse(assign.Groups[2].Value, CultureInfo.InvariantCulture);
            string prop = assign.Groups[3].Value;
            string val = assign.Groups[4].Value.Trim();

            if (!working.TryGetValue(lid, out List<ShopItemBuilder>? builders) || idx < 0 || idx >= builders.Count)
            {
                continue;
            }

            ShopItemBuilder b = builders[idx];
            switch (prop)
            {
                case "Base": b.BaseId = ParseIntSafe(val, -1); break;
                case "BaseLv": b.BaseLevel = (byte)Math.Clamp(ParseIntSafe(val, 1), 1, 255); break;
                case "AppendLv": b.AppendLevel = (byte)Math.Clamp(ParseIntSafe(val, 1), 1, 255); break;
                case "Overlap": b.Overlap = (byte)Math.Clamp(ParseIntSafe(val, 1), 1, 255); break;
                case "Binding": b.Binding = (byte)Math.Clamp(ParseIntSafe(val, 0), 0, 255); break;
                case "Type": b.TradeTypes = ParseIntListLiteral(val); break;
                case "Consume": b.ConsumeAmounts = ParseIntListLiteral(val); break;
            }
        }

        foreach ((int listId, List<ShopItemBuilder> builders) in working)
        {
            var items = new List<ShopItemDef>(builders.Count);
            foreach (ShopItemBuilder b in builders)
            {
                if (b.BaseId > 0)
                {
                    items.Add(new ShopItemDef(
                        b.BaseId,
                        b.BaseLevel,
                        b.AppendLevel,
                        b.Overlap,
                        b.Binding,
                        b.TradeTypes,
                        b.ConsumeAmounts));
                }
            }

            if (items.Count > 0)
            {
                result[listId] = items;
            }
        }

        return result;
    }

    public static Dictionary<int, QuestDef> LoadQuests(string questPyPath)
    {
        var quests = new Dictionary<int, QuestDef>();
        if (!File.Exists(questPyPath))
        {
            return quests;
        }

        QuestDef GetOrCreate(int id)
        {
            if (!quests.TryGetValue(id, out QuestDef? q))
            {
                q = new QuestDef { QuestId = id };
                quests[id] = q;
            }
            return q;
        }

        foreach (string rawLine in File.ReadLines(questPyPath))
        {
            string line = StripComment(rawLine).Trim();
            if (line.Length == 0)
            {
                continue;
            }

            Match rewardItemMatch = QuestRewardItemRegex().Match(line);
            if (rewardItemMatch.Success)
            {
                int qid = int.Parse(rewardItemMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                int slot = int.Parse(rewardItemMatch.Groups[2].Value, CultureInfo.InvariantCulture);
                string prop = rewardItemMatch.Groups[3].Value;
                string val = rewardItemMatch.Groups[4].Value.Trim();

                if (slot is >= 0 and < 4)
                {
                    QuestDef q = GetOrCreate(qid);
                    ItemEquipSlot cur = q.RewardItems[slot];
                    q.RewardItems[slot] = prop switch
                    {
                        "Base" => cur with { Base = ParseIntSafe(val, -1) },
                        "BaseLv" => cur with { BaseLevel = (byte)Math.Clamp(ParseIntSafe(val, 1), 1, 255) },
                        "AppendLv" => cur with { AppendLevel = (byte)Math.Clamp(ParseIntSafe(val, 1), 1, 255) },
                        "Overlap" => cur with { Overlap = (byte)Math.Clamp(ParseIntSafe(val, 1), 1, 255) },
                        "Binding" => cur with { Binding = (byte)Math.Clamp(ParseIntSafe(val, 0), 0, 255) },
                        "Append" => ApplyAppends(cur, ParseIntListLiteral(val)),
                        _ => cur
                    };
                }
                continue;
            }

            Match propMatch = QuestAssignRegex().Match(line);
            if (!propMatch.Success)
            {
                continue;
            }

            string prefix = propMatch.Groups[1].Value;
            int id = int.Parse(propMatch.Groups[2].Value, CultureInfo.InvariantCulture);
            string field = propMatch.Groups[3].Value;
            string rhs = propMatch.Groups[4].Value.Trim();
            QuestDef quest = GetOrCreate(id);

            if (prefix == "QI")
            {
                switch (field)
                {
                    case "QuestID": quest.QuestId = ParseIntSafe(rhs, id); break;
                    case "QuestSort": quest.QuestSort = ParseIntSafe(rhs, 0); break;
                    case "PrevQuestId": quest.PrevQuestId = ParseIntSafe(rhs, -1); break;
                    case "NextQuestId": quest.NextQuestId = ParseIntSafe(rhs, -1); break;
                    case "MinLevel": quest.MinLevel = ParseIntSafe(rhs, 1); break;
                    case "MaxLevel": quest.MaxLevel = ParseIntSafe(rhs, 200); break;
                    case "RequiredClass": quest.RequiredClass = EvaluateQuestExpr(rhs, -1); break;
                    case "RequiredFaction": quest.RequiredFaction = EvaluateQuestExpr(rhs, -1); break;
                    case "SpecialFlags": quest.SpecialFlags = EvaluateQuestExpr(rhs, 0); break;
                    case "Money": quest.Money = ParseIntSafe(rhs, 0); break;
                    case "Bijou": quest.Bijou = ParseIntSafe(rhs, 0); break;
                    case "Repeat": quest.Repeat = ParseIntSafe(rhs, 1); break;
                    case "RewardMoney": quest.RewardMoney = ParseIntSafe(rhs, 0); break;
                    case "RewardBijou": quest.RewardBijou = ParseIntSafe(rhs, 0); break;
                    case "Item": FillShortArray(quest.Items, ParseIntListLiteral(rhs)); break;
                    case "Creature": FillShortArray(quest.Creatures, ParseIntListLiteral(rhs)); break;
                    case "ItemCount": FillShortArray(quest.ItemCounts, ParseIntListLiteral(rhs)); break;
                    case "CreatureCount": FillShortArray(quest.CreatureCounts, ParseIntListLiteral(rhs)); break;
                }
            }
            else if (prefix == "QR")
            {
                switch (field)
                {
                    case "Credit": quest.Credit = ParseIntSafe(rhs, 0); break;
                    case "Contribute": quest.Contribute = ParseIntSafe(rhs, 0); break;
                    case "EXP": quest.Exp = ParseIntSafe(rhs, 0); break;
                    case "SkillPoint": quest.SkillPoint = ParseIntSafe(rhs, 0); break;
                    case "SpecialFlags": quest.RewardSpecialFlags = EvaluateQuestExpr(rhs, 0); break;
                    case "SpecialCount": quest.RewardSpecialCount = ParseIntSafe(rhs, 0); break;
                    case "UionContribute": quest.UnionContribute = ParseIntSafe(rhs, 0); break;
                }
            }
        }

        return quests;
    }

    public static Dictionary<string, NpcScriptDef> LoadNpcScripts(string scriptRoot)
    {
        var scripts = new Dictionary<string, NpcScriptDef>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(scriptRoot))
        {
            return scripts;
        }

        foreach (string dir in Directory.EnumerateDirectories(scriptRoot))
        {
            string sceneFolder = Path.GetFileName(dir);
            foreach (string file in Directory.EnumerateFiles(dir, "*.py"))
            {
                string fileStem = Path.GetFileNameWithoutExtension(file);
                if (fileStem.Equals("__init__", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string scriptKey = $"Script.{sceneFolder}.{fileStem}";
                string text = File.ReadAllText(file);

                int npcFlag = ExtractFlagSum(text, "GetType", NpcFlagConstants);
                int sysFlag = ExtractFlagSum(text, "GetSysType", NpcFlagConstants);
                List<int> itemLists = ExtractIntMatches(text, "GetItemList", @"ItemList(\d+)");
                List<int> questIds = ExtractIntMatches(text, "GetQuest", @"\bQI(\d+)\b");
                List<int> rewardQuestIds = ExtractIntMatches(text, "GetQuestReward", @"\bQR(\d+)\b");
                string talkKey = ExtractTalkKey(text) ?? fileStem;

                scripts[scriptKey] = new NpcScriptDef(
                    scriptKey,
                    npcFlag,
                    sysFlag,
                    itemLists,
                    questIds,
                    rewardQuestIds,
                    talkKey);
            }
        }

        return scripts;
    }

    public static (Dictionary<int, MallItemBaseDef> ShopItems, List<AssociationDiscountDef> Discounts) LoadItemShop(string xmlPath)
    {
        var shopItems = new Dictionary<int, MallItemBaseDef>();
        var discounts = new List<AssociationDiscountDef>();
        if (!File.Exists(xmlPath))
        {
            return (shopItems, discounts);
        }

        XDocument doc = XDocument.Load(xmlPath);
        if (doc.Root is null)
        {
            return (shopItems, discounts);
        }

        XElement? shopEl = doc.Root.Element("Shop");
        if (shopEl is not null)
        {
            foreach (XElement el in shopEl.Elements())
            {
                int id = ParseAttrInt(el, "ID");
                if (id <= 0)
                {
                    continue;
                }

                int discount = ParseIntSafe(el.Attribute("Discount")?.Value, 100);
                byte isNew = (byte)Math.Clamp(ParseAttrInt(el, "New"), 0, 1);
                byte isHot = (byte)Math.Clamp(ParseAttrInt(el, "Hot"), 0, 1);
                shopItems[id] = new MallItemBaseDef(id, discount, isNew, isHot);
            }
        }

        XElement? guildEl = doc.Root.Element("Guild");
        if (guildEl is not null)
        {
            foreach (XElement el in guildEl.Elements())
            {
                int glevel = ParseAttrInt(el, "Glevel");
                if (glevel <= 0)
                {
                    continue;
                }

                int discA = ParseIntSafe(el.Attribute("DiscountA")?.Value, 100);
                int discB = ParseIntSafe(el.Attribute("DiscountB")?.Value, 100);
                discounts.Add(new AssociationDiscountDef((byte)glevel, discA, discB));
            }
        }

        return (shopItems, discounts);
    }

    public static Dictionary<int, BijouForgeDef> LoadBijouForges(string xmlPath)
    {
        var result = new Dictionary<int, BijouForgeDef>();
        if (!File.Exists(xmlPath))
        {
            return result;
        }

        XDocument doc = XDocument.Load(xmlPath);
        if (doc.Root is null)
        {
            return result;
        }

        foreach (XElement el in doc.Root.Elements())
        {
            int id = ParseAttrInt(el, "ID");
            if (id <= 0)
            {
                continue;
            }

            result[id] = new BijouForgeDef(
                id,
                ParseAttrInt(el, "NextID"),
                ParseAttrInt(el, "MaterialType"),
                ParseAttrInt(el, "MaterialProyAdd"));
        }

        return result;
    }

    public static MallScriptConfig LoadMallScripts(string scriptRoot)
    {
        (List<int> athensLists, List<byte> athensSell) = LoadSingleMallScript(Path.Combine(scriptRoot, "Athens_GameMall.py"));
        (List<int> spartaLists, List<byte> spartaSell) = LoadSingleMallScript(Path.Combine(scriptRoot, "Sparta_GameMall.py"));
        (List<int> athensAssoc, _) = LoadSingleMallScript(Path.Combine(scriptRoot, "Athens_AssociationMall.py"));
        (List<int> spartaAssoc, _) = LoadSingleMallScript(Path.Combine(scriptRoot, "Sparta_AssociationMall.py"));

        return new MallScriptConfig(athensLists, athensSell, spartaLists, spartaSell, athensAssoc, spartaAssoc);
    }

    public static Dictionary<uint, GoldBoxConfig> LoadGoldBoxes(string dropDir)
    {
        var result = new Dictionary<uint, GoldBoxConfig>();
        string dialIndex = Path.Combine(dropDir, "dial.ini");
        if (!File.Exists(dialIndex))
        {
            return result;
        }

        foreach (string line in File.ReadLines(dialIndex))
        {
            string trimmed = line.Trim();
            int eq = trimmed.IndexOf('=');
            if (eq <= 0 || !trimmed[..eq].StartsWith("ID", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!uint.TryParse(trimmed[(eq + 1)..].Trim(), out uint boxId))
            {
                continue;
            }

            string boxFile = Path.Combine(dropDir, $"dial{boxId}.ini");
            if (!File.Exists(boxFile))
            {
                continue;
            }

            GoldBoxConfig? cfg = ParseSingleGoldBoxFile(boxId, boxFile);
            if (cfg is not null)
            {
                result[boxId] = cfg;
            }
        }

        return result;
    }

    private static GoldBoxConfig? ParseSingleGoldBoxFile(uint boxId, string path)
    {
        var sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        string current = string.Empty;
        foreach (string rawLine in File.ReadLines(path))
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith(';') || line.StartsWith("//"))
            {
                continue;
            }

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                current = line[1..^1].Trim();
                sections[current] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                continue;
            }

            int eq = line.IndexOf('=');
            if (eq > 0 && !string.IsNullOrEmpty(current))
            {
                sections[current][line[..eq].Trim()] = line[(eq + 1)..].Trim();
            }
        }

        int keyBoxId = sections.TryGetValue("ItemCount", out Dictionary<string, string>? ic)
            ? ParseIntSafe(ic.GetValueOrDefault("BoxID"), 2)
            : 2;
        int count = ic is not null ? ParseIntSafe(ic.GetValueOrDefault("Count"), 0) : 0;

        DialEntryDef defaultItem = sections.TryGetValue("DefaultItem", out Dictionary<string, string>? defSec)
            ? ParseDialEntry(defSec, 10000)
            : new DialEntryDef(10000, 2900, [-1, -1, -1, -1, -1], 1, 1, 0, 1, false, 1, 100);

        var normal = new List<DialEntryDef>();
        var nonsuch = new List<DialEntryDef>();
        for (int i = 0; i < count; i++)
        {
            if (!sections.TryGetValue($"DialItem{i}", out Dictionary<string, string>? dsec))
            {
                continue;
            }

            DialEntryDef entry = ParseDialEntry(dsec, (uint)(10001 + i));
            if (entry.BaseId <= 0)
            {
                continue;
            }

            if (entry.IsNonsuch)
            {
                nonsuch.Add(entry);
            }
            else
            {
                normal.Add(entry);
            }
        }

        return new GoldBoxConfig(boxId, keyBoxId, defaultItem, normal, nonsuch);
    }

    private static DialEntryDef ParseDialEntry(Dictionary<string, string> sec, uint fallbackItemId)
    {
        uint itemId = uint.TryParse(sec.GetValueOrDefault("ItemID"), out uint iid) ? iid : fallbackItemId;
        int baseId = ParseIntSafe(sec.GetValueOrDefault("BaseID"), -1);
        int[] rawApp = ParseCsvIntArray(sec.GetValueOrDefault("Append"));
        int[] appends =
        [
            rawApp.Length > 0 ? rawApp[0] : -1,
            rawApp.Length > 1 ? rawApp[1] : -1,
            rawApp.Length > 2 ? rawApp[2] : -1,
            rawApp.Length > 3 ? rawApp[3] : -1,
            rawApp.Length > 4 ? rawApp[4] : -1,
        ];
        byte appLv = (byte)Math.Clamp(ParseIntSafe(sec.GetValueOrDefault("AppLV"), 1), 1, 12);
        byte baseLv = (byte)Math.Clamp(ParseIntSafe(sec.GetValueOrDefault("BaseLV"), 1), 1, 5);
        byte binding = (byte)Math.Clamp(ParseIntSafe(sec.GetValueOrDefault("Binding"), 0), 0, 1);
        byte overlap = (byte)Math.Clamp(ParseIntSafe(sec.GetValueOrDefault("OverLap"), 1), 1, 99);
        bool nonsuch = ParseIntSafe(sec.GetValueOrDefault("Nonsuch"), 0) != 0;
        int randInDial = Math.Max(1, ParseIntSafe(sec.GetValueOrDefault("RandInDial"), 1));
        int prob = Math.Max(1, ParseIntSafe(sec.GetValueOrDefault("Probability"), 100));

        return new DialEntryDef(itemId, baseId, appends, appLv, baseLv, binding, overlap, nonsuch, randInDial, prob);
    }

    private static (List<int> ItemLists, List<byte> SellTypes) LoadSingleMallScript(string path)
    {
        if (!File.Exists(path))
        {
            return ([], []);
        }

        string text = File.ReadAllText(path);
        List<int> itemLists = ExtractIntMatches(text, "GetItemList", @"ItemList(\d+)");
        List<int> sellTypeInts = ExtractIntMatches(text, "GetItemListType", @"(\d+)");
        List<byte> sellTypes = sellTypeInts.Select(v => (byte)Math.Clamp(v, 0, 255)).ToList();
        while (sellTypes.Count < itemLists.Count)
        {
            sellTypes.Add(0);
        }

        return (itemLists, sellTypes);
    }

    private static int ExtractFlagSum(string text, string funcName, Dictionary<string, int> constants)
    {
        Match m = Regex.Match(text, $@"def\s+{funcName}\s*\([^)]*\)\s*:\s*([\s\S]*?)(?=\ndef\s|\z)");
        if (!m.Success)
        {
            return 0;
        }

        string body = m.Groups[1].Value;
        int sum = 0;
        foreach (Match tok in Regex.Matches(body, @"NPC_FLAG_[A-Z0-9_]+"))
        {
            if (constants.TryGetValue(tok.Value, out int val))
            {
                sum += val;
            }
        }

        return sum;
    }

    private static List<int> ExtractIntMatches(string text, string funcName, string itemPattern)
    {
        var list = new List<int>();
        Match m = Regex.Match(text, $@"def\s+{funcName}\s*\([^)]*\)\s*:\s*([\s\S]*?)(?=\ndef\s|\z)");
        if (!m.Success)
        {
            return list;
        }

        foreach (Match im in Regex.Matches(m.Groups[1].Value, itemPattern))
        {
            if (int.TryParse(im.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
            {
                list.Add(id);
            }
        }

        return list;
    }

    private static string? ExtractTalkKey(string text)
    {
        Match m = Regex.Match(text, @"def\s+Talk\s*\([^)]*\)\s*:[\s\S]*?return\s+[""']([^""']+)[""']");
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string StripComment(string line)
    {
        int hash = line.IndexOf('#');
        return hash >= 0 ? line[..hash] : line;
    }

    private static int EvaluateQuestExpr(string expr, int fallback)
    {
        if (int.TryParse(expr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int direct))
        {
            return direct;
        }

        int sum = 0;
        bool matched = false;
        foreach (string part in expr.Split(['+', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (QuestFlagConstants.TryGetValue(part, out int cval))
            {
                sum += cval;
                matched = true;
            }
            else if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out int nval))
            {
                sum += nval;
                matched = true;
            }
        }

        return matched ? sum : fallback;
    }

    private static ItemEquipSlot ApplyAppends(ItemEquipSlot slot, int[] appends)
        => slot with
        {
            Append0 = appends.Length > 0 ? appends[0] : -1,
            Append1 = appends.Length > 1 ? appends[1] : -1,
            Append2 = appends.Length > 2 ? appends[2] : -1,
            Append3 = appends.Length > 3 ? appends[3] : -1,
            Append4 = appends.Length > 4 ? appends[4] : -1,
        };

    private static void FillShortArray(short[] target, int[] values)
    {
        for (int i = 0; i < Math.Min(target.Length, values.Length); i++)
        {
            target[i] = (short)values[i];
        }
    }

    private static int[] ParseIntListLiteral(string rhs)
    {
        string trimmed = rhs.Trim().Trim('[', ']');
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return [];
        }

        string[] parts = trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var values = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            values[i] = ParseIntSafe(parts[i], 0);
        }

        return values;
    }

    private static int[] ParseCsvIntArray(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        string[] parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var values = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            values[i] = ParseIntSafe(parts[i], 0);
        }

        return values;
    }

    private static int ParseAttrInt(XElement el, string attrName)
        => ParseIntSafe(el.Attribute(attrName)?.Value, 0);

    private static int ParseIntSafe(string? raw, int fallback)
        => int.TryParse(raw?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : fallback;

    private sealed class ShopItemBuilder
    {
        public int BaseId { get; set; } = -1;
        public byte BaseLevel { get; set; } = 1;
        public byte AppendLevel { get; set; } = 1;
        public byte Overlap { get; set; } = 1;
        public byte Binding { get; set; }
        public int[] TradeTypes { get; set; } = [2];
        public int[] ConsumeAmounts { get; set; } = [-1];
    }

    [GeneratedRegex(@"^ItemList(\d+)\s*=\s*\[(.*)\]$")]
    private static partial Regex ItemListDeclRegex();

    [GeneratedRegex(@"^ItemList(\d+)\[(\d+)\]\.(\w+)\s*=\s*(.+)$")]
    private static partial Regex ItemListAssignRegex();

    [GeneratedRegex(@"^(QI|QR)(\d+)\.(\w+)\s*=\s*(.+)$")]
    private static partial Regex QuestAssignRegex();

    [GeneratedRegex(@"^QI(\d+)\.RewardItem\[(\d+)\]\.(\w+)\s*=\s*(.+)$")]
    private static partial Regex QuestRewardItemRegex();
}
