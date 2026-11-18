using System.Globalization;
using System.Text;
using GodsWar.Database.Models;
using GodsWar.Database.Repositories;
using GodsWar.GameServer.World;
using GodsWar.Protocol;
using GodsWar.Protocol.Packets;

namespace GodsWar.GameServer.Game;

/// <summary>
/// Implements Phase 6 (1.0.282 client systems):
/// Mall &amp; Association Mall (10172–10176), GoldBox (10112/10114/10115),
/// Skill Learn/Upgrade/PointUpgrade/Backup (10046/10047/10159/10164),
/// Player Stalls (10059–10063), Pet System (10230–10243),
/// AFK / Auto-Hunt (10295–10299), Mentor/Apprentice (10227–10229),
/// TargetInfo (10184), and QuickEquip (10054).
/// </summary>
public sealed class ClientFeaturesService
{
    private static long _nextStallItemId = 100_000_000_001L;
    private int _schemaInitialized;

    private readonly WorldManager _world;
    private readonly ICharacterRepository _characters;
    private readonly IWorldSocialRepository _socialRepo;
    private readonly GameplayHandlers _gameplay;

    public ClientFeaturesService(
        WorldManager world,
        ICharacterRepository characters,
        IWorldSocialRepository socialRepo,
        GameplayHandlers gameplay)
    {
        _world = world;
        _characters = characters;
        _socialRepo = socialRepo;
        _gameplay = gameplay;
    }

    public async Task LoadPlayerFeaturesAsync(PlayerSession player, CancellationToken ct)
    {
        if (Interlocked.CompareExchange(ref _schemaInitialized, 1, 0) == 0)
        {
            await _socialRepo.EnsurePhase6SchemaAsync(ct).ConfigureAwait(false);
        }

        UserSkillBackupRecord? backup = await _socialRepo.GetSkillBackupAsync(player.Character.Id, ct).ConfigureAwait(false);
        if (backup is not null)
        {
            player.BackupSkillIds = ParseCsvFixedInts(backup.SkillIds, MsgSkillBackup.BackupCount);
            player.BackupItemIds = ParseCsvFixedInts(backup.ItemIds, MsgSkillBackup.BackupCount);
            player.BackupRSkillId = backup.RSkillId;
            player.BackupRItemId = backup.RItemId;
        }

        IReadOnlyList<UserPetRecord> pets = await _socialRepo.GetUserPetsAsync(player.Character.Id, ct).ConfigureAwait(false);
        if (pets.Count == 0)
        {
            var starterPet = new UserPetRecord
            {
                UserId = player.Character.Id,
                Name = player.Character.Camp == 0 ? "AthenaOwl" : "AresWolf",
                TemplateId = (short)(player.Character.Camp == 0 ? 1 : 2),
                Level = 1,
                Exp = 0,
                Quality = 2,
                Sex = 0,
                Savvy = 15,
                State = 1,
                Loyalty = 100,
                Satiety = 100,
                Rebirth = 0,
                Hp = 250,
                MaxHp = 250,
                Attack = 30,
                Defend = 15,
                MagicAttack = 30,
                MagicDefend = 15,
                Hit = 95,
            };
            UserPetRecord created = await _socialRepo.CreatePetAsync(starterPet, ct).ConfigureAwait(false);
            player.Pets.Clear();
            player.Pets.Add(created);
        }
        else
        {
            player.Pets.Clear();
            player.Pets.AddRange(pets);
        }
    }

    public async Task SendFeaturesOnLoginAsync(PlayerSession player, CancellationToken ct)
    {
        await SendPetListAsync(player, ct).ConfigureAwait(false);
    }

    // =========================================================================
    // 1. Mall & Association Mall (10172 - 10176)
    // =========================================================================

    public async Task HandleMallItemDataAsync(PlayerSession player, CancellationToken ct)
    {
        IReadOnlyList<int> tabListIds = player.Character.Camp == 0
            ? _world.Catalog.MallConfig.AthensMallItemLists
            : _world.Catalog.MallConfig.SpartaMallItemLists;
        IReadOnlyList<byte> tabSells = player.Character.Camp == 0
            ? _world.Catalog.MallConfig.AthensMallSellTypes
            : _world.Catalog.MallConfig.SpartaMallSellTypes;

        if (tabListIds.Count == 0)
        {
            tabListIds = [1000, 1001, 1003, 1004];
            tabSells = [0, 2, 1, 18];
        }

        byte maxTab = (byte)tabListIds.Count;
        for (int t = 0; t < tabListIds.Count; t++)
        {
            byte sellType = t < tabSells.Count ? tabSells[t] : (byte)0;
            List<MallItemEntry> entries = BuildMallEntries(tabListIds[t], sellType);
            await player.SendAsync(
                GameOpcode.MsgMallItemData,
                new MsgMallItemData((byte)t, maxTab, sellType, entries).ToPayload(),
                ct).ConfigureAwait(false);
        }
    }

    public async Task HandleAssociationItemDataAsync(PlayerSession player, CancellationToken ct)
    {
        var discountEntries = _world.Catalog.AssociationDiscounts
            .Select(d => new AssociationDiscountEntry(d.Level, d.StoneDiscount, d.GloryDiscount))
            .ToList();
        if (discountEntries.Count == 0)
        {
            discountEntries.AddRange(
            [
                new AssociationDiscountEntry(1, 95, 95),
                new AssociationDiscountEntry(2, 90, 90),
                new AssociationDiscountEntry(3, 85, 85),
                new AssociationDiscountEntry(4, 80, 80),
                new AssociationDiscountEntry(5, 75, 75),
            ]);
        }

        await player.SendAsync(
            GameOpcode.MsgAssociationDiscount,
            new MsgAssociationDiscount(discountEntries).ToPayload(),
            ct).ConfigureAwait(false);

        IReadOnlyList<int> listIds = player.Character.Camp == 0
            ? _world.Catalog.MallConfig.AthensAssociationItemLists
            : _world.Catalog.MallConfig.SpartaAssociationItemLists;
        int listId = listIds.Count > 0 ? listIds[0] : 1000;

        List<MallItemEntry> allItems = BuildMallEntries(listId, 0, maxCount: 64);
        int pageCount = Math.Max(1, (allItems.Count + 15) / 16);
        for (int p = 0; p < pageCount; p++)
        {
            List<MallItemEntry> slice = allItems.Skip(p * 16).Take(16).ToList();
            await player.SendAsync(
                GameOpcode.MsgMallItemData2,
                new MsgMallItemData((byte)p, (byte)pageCount, 0, slice).ToPayload(),
                ct).ConfigureAwait(false);
        }
    }

    public async Task HandleMallSellAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgMallSell req = MsgMallSell.Parse(payload.Span);
        IReadOnlyList<int> tabListIds = player.Character.Camp == 0
            ? _world.Catalog.MallConfig.AthensMallItemLists
            : _world.Catalog.MallConfig.SpartaMallItemLists;
        if (tabListIds.Count == 0)
        {
            tabListIds = [1000, 1001, 1003, 1004];
        }

        if (req.Index < 0 || req.Index >= tabListIds.Count || req.Count <= 0)
        {
            return;
        }

        int listId = tabListIds[req.Index];
        if (!_world.Catalog.ShopItemLists.TryGetValue(listId, out List<ShopItemDef>? items) ||
            req.Num < 0 || req.Num >= items.Count)
        {
            return;
        }

        ShopItemDef shopItem = items[req.Num];
        if (req.Base > 0 && shopItem.BaseId != req.Base)
        {
            return;
        }

        int discount = _world.Catalog.MallShopItems.TryGetValue(shopItem.BaseId, out MallItemBaseDef? mbase)
            ? mbase.Discount
            : 100;

        if (!TryDeductMallCost(player, shopItem, req.Count, discount))
        {
            return;
        }

        if (!TryGrantMallPurchase(player, shopItem, req.Count, out int bagIdx, out int slotIdx))
        {
            return;
        }

        await GameplayHandlers.SendKitbagPageForSlotAsync(player, bagIdx, slotIdx, ct).ConfigureAwait(false);
        await player.SendAsync(GameOpcode.MsgMallSell, new MsgMallSell(req.Index, req.Num, req.Count, shopItem.BaseId).ToPayload(), ct).ConfigureAwait(false);
        ClientObjectData objData = player.ToClientObjectData();
        await player.SendAsync(GameOpcode.MsgSelfProperty, new MsgSelfProperty(objData).ToPayload(), ct).ConfigureAwait(false);
        await player.SendAsync(GameOpcode.MsgSynGameData, new MsgSynGameData(player.UiId, objData, (uint)Math.Max(0, player.Character.SkillPoint)).ToPayload(), ct).ConfigureAwait(false);

        await _characters.UpdateCharacterStateAsync(player.Character, ct).ConfigureAwait(false);
        await _gameplay.PersistKitbagPublicAsync(player, ct).ConfigureAwait(false);
    }

    public async Task HandleAssociationSellAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgMallSell req = MsgMallSell.Parse(payload.Span);
        IReadOnlyList<int> listIds = player.Character.Camp == 0
            ? _world.Catalog.MallConfig.AthensAssociationItemLists
            : _world.Catalog.MallConfig.SpartaAssociationItemLists;
        int listId = listIds.Count > 0 ? listIds[0] : 1000;

        if (!_world.Catalog.ShopItemLists.TryGetValue(listId, out List<ShopItemDef>? items))
        {
            return;
        }

        int flatIdx = req.Index * 16 + req.Num;
        if (flatIdx < 0 || flatIdx >= items.Count)
        {
            flatIdx = req.Num;
        }
        if (flatIdx < 0 || flatIdx >= items.Count || req.Count <= 0)
        {
            return;
        }

        ShopItemDef shopItem = items[flatIdx];
        if (req.Base > 0 && shopItem.BaseId != req.Base)
        {
            return;
        }

        int discount = _world.Catalog.MallShopItems.TryGetValue(shopItem.BaseId, out MallItemBaseDef? mbase)
            ? mbase.Discount
            : 95;

        if (!TryDeductMallCost(player, shopItem, req.Count, discount))
        {
            return;
        }

        if (!TryGrantMallPurchase(player, shopItem, req.Count, out int bagIdx, out int slotIdx))
        {
            return;
        }

        await GameplayHandlers.SendKitbagPageForSlotAsync(player, bagIdx, slotIdx, ct).ConfigureAwait(false);
        await player.SendAsync(GameOpcode.MsgAssociationSell, new MsgMallSell(req.Index, req.Num, req.Count, shopItem.BaseId).ToPayload(), ct).ConfigureAwait(false);
        ClientObjectData objData = player.ToClientObjectData();
        await player.SendAsync(GameOpcode.MsgSelfProperty, new MsgSelfProperty(objData).ToPayload(), ct).ConfigureAwait(false);
        await player.SendAsync(GameOpcode.MsgSynGameData, new MsgSynGameData(player.UiId, objData, (uint)Math.Max(0, player.Character.SkillPoint)).ToPayload(), ct).ConfigureAwait(false);

        await _characters.UpdateCharacterStateAsync(player.Character, ct).ConfigureAwait(false);
        await _gameplay.PersistKitbagPublicAsync(player, ct).ConfigureAwait(false);
    }

    private List<MallItemEntry> BuildMallEntries(int listId, byte sellType, int maxCount = 16)
    {
        var entries = new List<MallItemEntry>();
        if (!_world.Catalog.ShopItemLists.TryGetValue(listId, out List<ShopItemDef>? items))
        {
            return entries;
        }

        foreach (ShopItemDef si in items.Take(maxCount))
        {
            _world.Catalog.MallShopItems.TryGetValue(si.BaseId, out MallItemBaseDef? mbase);
            int discount = mbase?.Discount ?? 100;
            byte isNew = mbase?.New ?? 0;
            byte isHot = mbase?.Hot ?? 0;

            byte bt0 = si.TradeTypes.Length > 0 ? (byte)si.TradeTypes[0] : (byte)3;
            byte bt1 = si.TradeTypes.Length > 1 ? (byte)si.TradeTypes[1] : (byte)0;
            byte bt2 = si.TradeTypes.Length > 2 ? (byte)si.TradeTypes[2] : (byte)0;
            uint bc0 = si.ConsumeAmounts.Length > 0 ? (uint)Math.Max(0, si.ConsumeAmounts[0]) : 10;
            uint bc1 = si.ConsumeAmounts.Length > 1 ? (uint)Math.Max(0, si.ConsumeAmounts[1]) : 0;
            uint bc2 = si.ConsumeAmounts.Length > 2 ? (uint)Math.Max(0, si.ConsumeAmounts[2]) : 0;
            byte numTypes = (byte)Math.Clamp(si.TradeTypes.Length, 1, 3);

            entries.Add(new MallItemEntry(
                si.BaseId,
                -1, -1, -1, -1, -1,
                si.AppendLevel,
                si.BaseLevel,
                si.Binding,
                si.Overlap,
                sellType,
                bt0, bt1, bt2,
                bc0, bc1, bc2,
                discount,
                numTypes,
                isNew,
                isHot));
        }

        return entries;
    }

    private static bool TryDeductMallCost(PlayerSession player, ShopItemDef item, int count, int discountPct)
    {
        int buyType = item.TradeTypes.Length > 0 ? item.TradeTypes[0] : 3;
        int rawPrice = item.ConsumeAmounts.Length > 0 ? Math.Max(0, item.ConsumeAmounts[0]) : 10;
        int mult = Math.Max(1, (int)item.Overlap) * Math.Max(1, count);

        switch (buyType)
        {
            case 0: // Prestige
            {
                int cost = rawPrice * Math.Max(1, count);
                if (player.Character.Prestige < cost) return false;
                player.Character.Prestige -= cost;
                return true;
            }
            case 2: // Money
            {
                int cost = rawPrice * mult;
                if (player.Character.Money < cost) return false;
                player.Character.Money -= cost;
                return true;
            }
            default: // 3 = Bijou / Stone
            {
                int discountedUnit = Math.Max(1, rawPrice * Math.Clamp(discountPct, 1, 100) / 100);
                int cost = discountedUnit * mult;
                if (player.Character.Stone < cost) return false;
                player.Character.Stone -= cost;
                return true;
            }
        }
    }

    private static bool TryGrantMallPurchase(PlayerSession player, ShopItemDef shopItem, int count, out int bagIdx, out int slotIdx)
    {
        byte overlap = (byte)Math.Clamp(Math.Max(1, (int)shopItem.Overlap) * Math.Max(1, count), 1, 255);
        var slot = new ItemEquipSlot(
            shopItem.BaseId,
            -1, -1, -1, -1, -1,
            shopItem.BaseLevel,
            shopItem.AppendLevel,
            shopItem.Binding,
            overlap);
        return player.TryAddItemToBag(slot, out bagIdx, out slotIdx);
    }

    // =========================================================================
    // 2. GoldBox / Treasure Wheel (10112 / 10114 / 10115)
    // =========================================================================

    public async Task HandleGoldBoxAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgGoldBox req = MsgGoldBox.Parse(payload.Span);
        uint boxId = req.GoldBoxId > 0 ? req.GoldBoxId : (player.PendingGoldBoxId > 0 ? player.PendingGoldBoxId : 4400u);

        if (!_world.Catalog.GoldBoxes.TryGetValue(boxId, out GoldBoxConfig? cfg))
        {
            cfg = _world.Catalog.GoldBoxes.Values.FirstOrDefault()
                ?? new GoldBoxConfig(
                    4400,
                    2,
                    new DialEntryDef(10000, 2900, [-1, -1, -1, -1, -1], 1, 2, 0, 1, false, 1, 100),
                    [new DialEntryDef(10001, 4001, [-1, -1, -1, -1, -1], 1, 1, 0, 5, false, 1, 100)],
                    [new DialEntryDef(10002, 4201, [-1, -1, -1, -1, -1], 1, 1, 0, 1, true, 1, 100)]);
        }

        if (req.Type == 0)
        {
            DialEntryDef[] dial = RollDialItems(cfg);
            player.PendingGoldBoxId = boxId;
            player.PendingGoldBoxIter = req.Iter;
            player.PendingGoldBoxDial = dial;

            uint[] dialIds16 = dial.Select(d => d.ItemId).ToArray();
            uint[] dialIds17 = new uint[17];
            Array.Copy(dialIds16, dialIds17, Math.Min(16, dialIds16.Length));
            dialIds17[16] = cfg.DefaultItem.ItemId;

            await player.SendAsync(
                GameOpcode.MsgGoldBoxInDial,
                new MsgGoldBoxInDial(1, cfg.DefaultItem.ItemId, dialIds16).ToPayload(),
                ct).ConfigureAwait(false);
            await player.SendAsync(
                GameOpcode.MsgGoldBoxReturn,
                new MsgGoldBoxReturn(1, dialIds17).ToPayload(),
                ct).ConfigureAwait(false);
            return;
        }

        DialEntryDef[] activeDial = player.PendingGoldBoxDial ?? RollDialItems(cfg);
        DialEntryDef chosen = req.Type == 2
            ? cfg.DefaultItem
            : activeDial[Random.Shared.Next(activeDial.Length)];

        ConsumeBoxAndKeyFromBag(player, req.Iter, boxId, cfg.KeyBoxId, out int consumedBag, out int consumedSlot);
        if (consumedBag >= 0)
        {
            await GameplayHandlers.SendKitbagPageForSlotAsync(player, consumedBag, consumedSlot, ct).ConfigureAwait(false);
        }

        var rewardSlot = new ItemEquipSlot(
            chosen.BaseId,
            chosen.Appends[0],
            chosen.Appends[1],
            chosen.Appends[2],
            chosen.Appends[3],
            chosen.Appends[4],
            chosen.BaseLevel,
            chosen.AppendLevel,
            chosen.Binding,
            chosen.Overlap);

        if (player.TryAddItemToBag(rewardSlot, out int addBag, out int addSlot))
        {
            await GameplayHandlers.SendKitbagPageForSlotAsync(player, addBag, addSlot, ct).ConfigureAwait(false);
        }

        player.PendingGoldBoxId = 0;
        player.PendingGoldBoxDial = null;

        byte returnInfo = req.Type switch
        {
            1 => 3,
            2 => 5,
            _ => 8,
        };
        uint[] retDial = new uint[17];
        retDial[0] = chosen.ItemId;

        await player.SendAsync(
            GameOpcode.MsgGoldBoxReturn,
            new MsgGoldBoxReturn(returnInfo, retDial).ToPayload(),
            ct).ConfigureAwait(false);

        await _gameplay.PersistKitbagPublicAsync(player, ct).ConfigureAwait(false);
    }

    private static DialEntryDef[] RollDialItems(GoldBoxConfig cfg)
    {
        var dial = new DialEntryDef[16];
        for (int i = 0; i < 16; i++)
        {
            IReadOnlyList<DialEntryDef> pool = i < 13 && cfg.NormalItems.Count > 0
                ? cfg.NormalItems
                : cfg.NonsuchItems.Count > 0
                    ? cfg.NonsuchItems
                    : cfg.NormalItems;

            dial[i] = pool.Count > 0
                ? pool[Random.Shared.Next(pool.Count)]
                : cfg.DefaultItem;
        }
        return dial;
    }

    private void ConsumeBoxAndKeyFromBag(PlayerSession player, byte iter, uint boxId, int keyBoxId, out int changedBag, out int changedSlot)
    {
        changedBag = -1;
        changedSlot = -1;

        int b = iter / 24;
        int s = iter % 24;
        if (b is >= 0 and < 4 && s is >= 0 and < 24 && !player.Kitbags[b][s].IsEmpty && player.Kitbags[b][s].Base == (int)boxId)
        {
            ItemEquipSlot cur = player.Kitbags[b][s];
            player.Kitbags[b][s] = cur.Overlap > 1
                ? cur with { Overlap = (byte)(cur.Overlap - 1) }
                : ItemEquipSlot.Empty;
            changedBag = b;
            changedSlot = s;
        }

        // Also consume 1 matching key if present in bags
        for (int kb = 0; kb < 4; kb++)
        {
            for (int ks = 0; ks < 24; ks++)
            {
                ItemEquipSlot slot = player.Kitbags[kb][ks];
                if (slot.IsEmpty)
                {
                    continue;
                }

                bool isKey = (_world.Catalog.Items.TryGetValue(slot.Base, out ItemBaseDef? def) && def.KeyIdx == keyBoxId)
                    || slot.Base is 4420 or 4421 or 4422;
                if (isKey)
                {
                    player.Kitbags[kb][ks] = slot.Overlap > 1
                        ? slot with { Overlap = (byte)(slot.Overlap - 1) }
                        : ItemEquipSlot.Empty;
                    return;
                }
            }
        }
    }

    // =========================================================================
    // 3. Skill Learn, Passive Skill Upgrade, Point Upgrade & Backup
    //    (10046 / 10047 / 10159 / 10164)
    // =========================================================================

    public async Task HandleLearnSkillAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgLearn req = MsgLearn.Parse(payload.Span);
        await LearnSkillInternalAsync(player, req.SkillType, req.Kinds, ct).ConfigureAwait(false);
    }

    public async Task<bool> TryUseSkillBookAsync(PlayerSession player, ItemBaseDef itemDef, CancellationToken ct)
    {
        int learnId = itemDef.SkillLearnId >= 0 ? itemDef.SkillLearnId : itemDef.SkillId;
        if (!itemDef.IsSkillBook && learnId < 0)
        {
            return false;
        }

        if (learnId < 0)
        {
            learnId = 0;
        }

        byte kinds = _world.Catalog.Magics.ContainsKey(learnId) ? (byte)1 : (byte)0;
        await LearnSkillInternalAsync(player, (byte)Math.Clamp(learnId, 0, 255), kinds, ct, fullId: learnId).ConfigureAwait(false);
        return true;
    }

    private async Task LearnSkillInternalAsync(PlayerSession player, byte skillType, byte kinds, CancellationToken ct, int? fullId = null)
    {
        int id = fullId ?? skillType;
        if (kinds == 0)
        {
            int existingIdx = player.PassiveSkillsList.FindIndex(p => p.SkillId == id);
            if (existingIdx < 0)
            {
                uint nextPt = GameTables.GetPlayerSkillGradePoint(1, id);
                player.PassiveSkillsList.Add(new PassiveSkillEntry(id, 1, 0, nextPt));
            }
            await PersistPassiveSkillsAsync(player, ct).ConfigureAwait(false);
            await player.SendAsync(GameOpcode.MsgPassiveSkillInfo, new MsgPassiveSkillInfo(player.UiId, player.PassiveSkills).ToPayload(), ct).ConfigureAwait(false);
        }
        else
        {
            int existingIdx = player.ActiveSkillsList.FindIndex(a => a.MagicId == id);
            if (existingIdx < 0)
            {
                player.ActiveSkillsList.Add(new ActiveSkillEntry(id, 0));
            }
            await PersistActiveSkillsAsync(player, ct).ConfigureAwait(false);
            await player.SendAsync(GameOpcode.MsgActiveSkillInfo, new MsgActiveSkillInfo(player.UiId, player.ActiveSkills).ToPayload(), ct).ConfigureAwait(false);
        }

        await player.SendAsync(
            GameOpcode.MsgLearn,
            new MsgLearn(player.UiId, skillType, kinds, true).ToPayload(),
            ct).ConfigureAwait(false);
    }

    public async Task HandleSkillUpgradeAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgSkillUpgrade req = MsgSkillUpgrade.Parse(payload.Span);
        int skillId = (int)req.SkillId;
        if (skillId < 0 || skillId > 60000)
        {
            return;
        }

        int idx = player.PassiveSkillsList.FindIndex(s => s.SkillId == skillId);
        ushort currentGrade = idx >= 0 ? player.PassiveSkillsList[idx].Rank : (ushort)0;
        if (currentGrade >= 40)
        {
            return;
        }

        uint reqPoint = Math.Max(1u, GameTables.GetPlayerSkillGradePoint(currentGrade, skillId));
        if (player.Character.SkillPoint < (int)reqPoint)
        {
            return;
        }

        ushort newGrade = (ushort)(currentGrade + 1);
        uint nextGradePoint = GameTables.GetPlayerSkillGradePoint(newGrade, skillId);
        player.Character.SkillPoint -= (int)reqPoint;

        var updatedEntry = new PassiveSkillEntry(skillId, newGrade, 0, nextGradePoint);
        if (idx >= 0)
        {
            player.PassiveSkillsList[idx] = updatedEntry;
        }
        else
        {
            player.PassiveSkillsList.Add(updatedEntry);
        }

        await PersistPassiveSkillsAsync(player, ct).ConfigureAwait(false);
        await _characters.UpdateCharacterStateAsync(player.Character, ct).ConfigureAwait(false);

        uint surplus = (uint)Math.Max(0, player.Character.SkillPoint);
        ClientObjectData objData = player.ToClientObjectData();
        await player.SendAsync(GameOpcode.MsgSynGameData, new MsgSynGameData(player.UiId, objData, surplus).ToPayload(), ct).ConfigureAwait(false);
        await player.SendAsync(
            GameOpcode.MsgSkillUpgrade,
            new MsgSkillUpgrade(player.UiId, (uint)skillId, newGrade, nextGradePoint, surplus).ToPayload(),
            ct).ConfigureAwait(false);
    }

    public async Task AddSkillPointsAsync(PlayerSession player, int addPoints, CancellationToken ct)
    {
        if (addPoints <= 0)
        {
            return;
        }

        player.Character.SkillPoint += addPoints;
        uint total = (uint)Math.Max(0, player.Character.SkillPoint);
        await _characters.UpdateCharacterStateAsync(player.Character, ct).ConfigureAwait(false);
        await player.SendAsync(
            GameOpcode.MsgSkillPointUpgrade,
            new MsgSkillPointUpgrade(player.UiId, total).ToPayload(),
            ct).ConfigureAwait(false);
        await player.SendAsync(
            GameOpcode.MsgSynGameData,
            new MsgSynGameData(player.UiId, player.ToClientObjectData(), total).ToPayload(),
            ct).ConfigureAwait(false);
    }

    public async Task HandleSkillBackupAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgSkillBackup req = MsgSkillBackup.Parse(payload.Span);
        player.BackupSkillIds = req.SkillIds;
        player.BackupItemIds = req.ItemIds;
        player.BackupRSkillId = req.RSkillId;
        player.BackupRItemId = req.RItemId;

        var rec = new UserSkillBackupRecord
        {
            UserId = player.Character.Id,
            SkillIds = string.Join(',', req.SkillIds),
            ItemIds = string.Join(',', req.ItemIds),
            RSkillId = req.RSkillId,
            RItemId = req.RItemId,
        };
        await _socialRepo.SaveSkillBackupAsync(rec, ct).ConfigureAwait(false);
        await player.SendAsync(GameOpcode.MsgSkillBackup, req.ToPayload(), ct).ConfigureAwait(false);
    }

    // =========================================================================
    // 4. Player Stalls (10059 - 10063)
    // =========================================================================

    public async Task HandleStallAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgStall req = MsgStall.Parse(payload.Span);
        player.StallActive = req.State;
        player.StallText = req.State ? req.Text : string.Empty;

        if (!req.State)
        {
            ClearStallGoods(player);
        }

        var outStall = new MsgStall(player.UiId, player.StallActive, player.StallText, player.Character.Name);
        WorldMap? map = _world.GetMap(player.MapId);
        if (map is not null)
        {
            await map.BroadcastToAoiAsync(
                player.CellX,
                player.CellZ,
                GameOpcode.MsgStall,
                outStall.ToPayload(),
                cancellationToken: ct).ConfigureAwait(false);
        }
        else
        {
            await player.SendAsync(GameOpcode.MsgStall, outStall.ToPayload(), ct).ConfigureAwait(false);
        }
    }

    public async Task HandleStallAddItemAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgStallAddItem req = MsgStallAddItem.Parse(payload.Span);
        if (req.Pocket is < 0 or >= 2 || req.Index is < 0 or >= 5 ||
            req.BagIndex is < 0 or >= 4 || req.BagNum is < 0 or >= 24 ||
            req.Money < 0 || req.Bijou < 0)
        {
            return;
        }

        ItemEquipSlot item = player.Kitbags[req.BagIndex][req.BagNum];
        if (item.IsEmpty)
        {
            return;
        }

        long stallId = Interlocked.Increment(ref _nextStallItemId);
        player.StallGoods[req.Pocket, req.Index] = new StallGoodsSlot(
            stallId,
            req.BagIndex,
            req.BagNum,
            req.Money,
            req.Bijou,
            item);

        var reply = req with { StallId = stallId };
        await player.SendAsync(GameOpcode.MsgStallAddItem, reply.ToPayload(), ct).ConfigureAwait(false);
    }

    public async Task HandleStallDelItemAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgStallDelItem req = MsgStallDelItem.Parse(payload.Span);
        if (req.Pocket is < 0 or >= 2 || req.Index is < 0 or >= 5)
        {
            return;
        }

        player.StallGoods[req.Pocket, req.Index] = StallGoodsSlot.Empty;
        await player.SendAsync(GameOpcode.MsgStallDelItem, req.ToPayload(), ct).ConfigureAwait(false);
    }

    public async Task HandleStallItemQueryAsync(PlayerSession viewer, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgStallItem req = MsgStallItem.Parse(payload.Span);
        PlayerSession? seller = _world.GetPlayerByUiId(req.UiId)
            ?? (!string.IsNullOrWhiteSpace(req.SellerName) ? _world.GetPlayerByName(req.SellerName) : null);

        if (seller is null || !seller.StallActive)
        {
            return;
        }

        for (int p = 0; p < 2; p++)
        {
            for (int i = 0; i < 5; i++)
            {
                StallGoodsSlot slot = seller.StallGoods[p, i];
                if (slot.IsEmpty)
                {
                    continue;
                }

                var itemMsg = new MsgStallItem(
                    slot.StallId,
                    seller.Character.Name,
                    seller.UiId,
                    (byte)p,
                    (byte)i,
                    slot.Money,
                    slot.Bijou,
                    slot.Item);
                await viewer.SendAsync(GameOpcode.MsgStallItem, itemMsg.ToPayload(), ct).ConfigureAwait(false);
            }
        }
    }

    public async Task HandleStallBuyItemAsync(PlayerSession buyer, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgStallBuyItem req = MsgStallBuyItem.Parse(payload.Span);
        PlayerSession? seller = _world.GetPlayerByUiId(req.SellUiId)
            ?? (!string.IsNullOrWhiteSpace(req.SellerName) ? _world.GetPlayerByName(req.SellerName) : null);

        if (seller is null || seller.UiId == buyer.UiId || !seller.StallActive)
        {
            return;
        }

        if (req.Pocket is < 0 or >= 2 || req.Index is < 0 or >= 5)
        {
            return;
        }

        StallGoodsSlot goods = seller.StallGoods[req.Pocket, req.Index];
        if (goods.IsEmpty || (req.StallId > 0 && goods.StallId != req.StallId))
        {
            return;
        }

        if (buyer.Character.Money < goods.Money || buyer.Character.Stone < goods.Bijou)
        {
            return;
        }

        ItemEquipSlot sellerBagItem = seller.Kitbags[goods.BagIndex][goods.BagSlot];
        if (sellerBagItem.IsEmpty || sellerBagItem.Base != goods.Item.Base)
        {
            seller.StallGoods[req.Pocket, req.Index] = StallGoodsSlot.Empty;
            return;
        }

        if (!buyer.TryAddItemToBag(sellerBagItem, out int buyerBag, out int buyerSlot))
        {
            return;
        }

        seller.Kitbags[goods.BagIndex][goods.BagSlot] = ItemEquipSlot.Empty;
        seller.StallGoods[req.Pocket, req.Index] = StallGoodsSlot.Empty;

        buyer.Character.Money -= goods.Money;
        seller.Character.Money += goods.Money;
        buyer.Character.Stone -= goods.Bijou;
        seller.Character.Stone += goods.Bijou;

        var confirm = new MsgStallBuyItem(
            seller.UiId,
            buyer.UiId,
            seller.Character.Name,
            buyer.Character.Name,
            goods.StallId,
            req.Pocket,
            req.Index);

        await seller.SendAsync(GameOpcode.MsgStallBuyItem, confirm.ToPayload(), ct).ConfigureAwait(false);
        await buyer.SendAsync(GameOpcode.MsgStallBuyItem, confirm.ToPayload(), ct).ConfigureAwait(false);

        await GameplayHandlers.SendKitbagPageForSlotAsync(seller, goods.BagIndex, goods.BagSlot, ct).ConfigureAwait(false);
        await GameplayHandlers.SendKitbagPageForSlotAsync(buyer, buyerBag, buyerSlot, ct).ConfigureAwait(false);

        await seller.SendAsync(GameOpcode.MsgSelfProperty, new MsgSelfProperty(seller.ToClientObjectData()).ToPayload(), ct).ConfigureAwait(false);
        await buyer.SendAsync(GameOpcode.MsgSelfProperty, new MsgSelfProperty(buyer.ToClientObjectData()).ToPayload(), ct).ConfigureAwait(false);

        await _characters.UpdateCharacterStateAsync(seller.Character, ct).ConfigureAwait(false);
        await _characters.UpdateCharacterStateAsync(buyer.Character, ct).ConfigureAwait(false);
        await _gameplay.PersistKitbagPublicAsync(seller, ct).ConfigureAwait(false);
        await _gameplay.PersistKitbagPublicAsync(buyer, ct).ConfigureAwait(false);
    }

    private static void ClearStallGoods(PlayerSession player)
    {
        for (int p = 0; p < 2; p++)
        {
            for (int i = 0; i < 5; i++)
            {
                player.StallGoods[p, i] = StallGoodsSlot.Empty;
            }
        }
    }

    // =========================================================================
    // 5. Pet System (10230 - 10243)
    // =========================================================================

    public async Task SendPetListAsync(PlayerSession player, CancellationToken ct)
    {
        byte activeState = 0;
        var entries = new List<PetDataEntry>(player.Pets.Count);
        foreach (UserPetRecord p in player.Pets)
        {
            if (p.State > 1 && activeState == 0)
            {
                activeState = (byte)p.State;
            }

            entries.Add(ToPetDataEntry(p));
        }

        await player.SendAsync(
            GameOpcode.MsgPetList,
            new MsgPetList(activeState, entries).ToPayload(),
            ct).ConfigureAwait(false);
    }

    public async Task HandlePetSummonAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgPetCommand cmd = MsgPetCommand.Parse(payload.Span);
        UserPetRecord? pet = FindPet(player, cmd.PetId);
        if (pet is null)
        {
            return;
        }

        foreach (UserPetRecord p in player.Pets)
        {
            p.State = (short)(p.Id == pet.Id ? 2 : 1);
            await _socialRepo.UpdatePetAsync(p, ct).ConfigureAwait(false);
        }

        await player.SendAsync(GameOpcode.MsgPetAction, new MsgPetAction(pet.Id, 1).ToPayload(), ct).ConfigureAwait(false);
        await BroadcastPetViewAsync(player, pet.TemplateId, ct).ConfigureAwait(false);
        await SyncPlayerStatsAfterPetChangeAsync(player, ct).ConfigureAwait(false);
    }

    public async Task HandlePetRecallAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgPetCommand cmd = MsgPetCommand.Parse(payload.Span);
        UserPetRecord? pet = FindPet(player, cmd.PetId);
        if (pet is null)
        {
            return;
        }

        pet.State = 1;
        await _socialRepo.UpdatePetAsync(pet, ct).ConfigureAwait(false);

        await player.SendAsync(GameOpcode.MsgPetAction, new MsgPetAction(pet.Id, 2).ToPayload(), ct).ConfigureAwait(false);
        await BroadcastPetViewAsync(player, -1, ct).ConfigureAwait(false);
        await SyncPlayerStatsAfterPetChangeAsync(player, ct).ConfigureAwait(false);
    }

    public async Task HandlePetReleaseAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgPetCommand cmd = MsgPetCommand.Parse(payload.Span);
        UserPetRecord? pet = FindPet(player, cmd.PetId);
        if (pet is null)
        {
            return;
        }

        if (player.Pets.Count > 1)
        {
            player.Pets.Remove(pet);
            await _socialRepo.DeletePetAsync(pet.Id, player.Character.Id, ct).ConfigureAwait(false);
        }
        else
        {
            pet.State = 1;
            await _socialRepo.UpdatePetAsync(pet, ct).ConfigureAwait(false);
        }

        await player.SendAsync(GameOpcode.MsgPetAction, new MsgPetAction(pet.Id, 3).ToPayload(), ct).ConfigureAwait(false);
        await BroadcastPetViewAsync(player, -1, ct).ConfigureAwait(false);
    }

    public async Task HandlePetFeedAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgPetCommand cmd = MsgPetCommand.Parse(payload.Span);
        UserPetRecord? pet = FindPet(player, cmd.PetId);
        if (pet is null)
        {
            return;
        }

        pet.Satiety = (short)Math.Min(100, pet.Satiety + 20);
        pet.Loyalty = (short)Math.Min(100, pet.Loyalty + 10);
        pet.Hp = Math.Min(pet.MaxHp, pet.Hp + 50);
        pet.Exp += 50;
        if (pet.Exp >= pet.Level * 100 && pet.Level < 120)
        {
            pet.Exp = 0;
            pet.Level++;
            pet.MaxHp += 25;
            pet.Hp = pet.MaxHp;
            pet.Attack += 5;
            pet.Defend += 3;
            pet.MagicAttack += 5;
            pet.MagicDefend += 3;
        }

        await _socialRepo.UpdatePetAsync(pet, ct).ConfigureAwait(false);
        await player.SendAsync(
            GameOpcode.MsgPetState,
            new MsgPetState(pet.Id, (byte)pet.Satiety, (byte)pet.Loyalty, (ushort)pet.Hp).ToPayload(),
            ct).ConfigureAwait(false);
    }

    public async Task HandlePetRenameAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgPetRename req = MsgPetRename.Parse(payload.Span);
        UserPetRecord? pet = FindPet(player, req.PetId);
        if (pet is null || string.IsNullOrWhiteSpace(req.NewName))
        {
            return;
        }

        pet.Name = req.NewName.Trim()[..Math.Min(12, req.NewName.Trim().Length)];
        await _socialRepo.UpdatePetAsync(pet, ct).ConfigureAwait(false);

        await player.SendAsync(
            GameOpcode.MsgPetUpdate,
            new MsgPetUpdate(pet.Id, (byte)pet.Level, (byte)pet.Quality, (byte)pet.Rebirth, pet.Name).ToPayload(),
            ct).ConfigureAwait(false);
    }

    private static PetDataEntry ToPetDataEntry(UserPetRecord p) => new(
        p.Id,
        p.Name,
        (byte)Math.Clamp((int)p.TemplateId, 1, 255),
        (byte)Math.Clamp((int)p.Level, 1, 200),
        (byte)Math.Clamp((int)p.Quality, 1, 5),
        (byte)Math.Clamp((int)p.Sex, 0, 1),
        (byte)Math.Clamp((int)p.Savvy, 1, 100),
        (byte)Math.Clamp((int)p.State, 1, 4),
        (byte)Math.Clamp((int)p.Loyalty, 0, 100),
        (byte)Math.Clamp((int)p.Satiety, 0, 100),
        (byte)Math.Clamp((int)p.Rebirth, 0, 10),
        (ushort)Math.Clamp(p.Hp, 0, ushort.MaxValue),
        (ushort)Math.Clamp(p.MaxHp, 1, ushort.MaxValue),
        p.Exp,
        Math.Max(100, p.Level * 100),
        p.Attack,
        p.Defend,
        p.MagicAttack,
        p.MagicDefend,
        (ushort)Math.Clamp(p.Hit, 1, ushort.MaxValue));

    private static UserPetRecord? FindPet(PlayerSession player, int petId)
        => player.Pets.FirstOrDefault(p => p.Id == petId) ?? player.Pets.FirstOrDefault();

    private async Task BroadcastPetViewAsync(PlayerSession player, int petViewId, CancellationToken ct)
    {
        var viewMsg = new MsgPetView(petViewId, player.UiId);
        WorldMap? map = _world.GetMap(player.MapId);
        if (map is not null)
        {
            await map.BroadcastToAoiAsync(
                player.CellX,
                player.CellZ,
                GameOpcode.MsgPetView,
                viewMsg.ToPayload(),
                cancellationToken: ct).ConfigureAwait(false);
        }
        else
        {
            await player.SendAsync(GameOpcode.MsgPetView, viewMsg.ToPayload(), ct).ConfigureAwait(false);
        }
    }

    private async Task SyncPlayerStatsAfterPetChangeAsync(PlayerSession player, CancellationToken ct)
    {
        ClientObjectData objData = player.ToClientObjectData();
        await player.SendAsync(GameOpcode.MsgSelfProperty, new MsgSelfProperty(objData).ToPayload(), ct).ConfigureAwait(false);
        await player.SendAsync(
            GameOpcode.MsgSynGameData,
            new MsgSynGameData(player.UiId, objData, (uint)Math.Max(0, player.Character.SkillPoint)).ToPayload(),
            ct).ConfigureAwait(false);
    }

    // =========================================================================
    // 6. AFK / Auto-Hunt (10295 - 10299), Mentor (10227), TargetInfo (10184), QuickEquip (10054)
    // =========================================================================

    public async Task HandleAfkRequestAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgAfkRequest req = MsgAfkRequest.Parse(payload.Span);
        player.IsAfk = req.State != 0;
        byte stateByte = player.IsAfk ? (byte)1 : (byte)0;

        await player.SendAsync(GameOpcode.MsgAfkState, new MsgAfkState(stateByte).ToPayload(), ct).ConfigureAwait(false);
        await player.SendAsync(GameOpcode.MsgAfkSync, new MsgAfkState(stateByte).ToPayload(), ct).ConfigureAwait(false);

        WorldMap? map = _world.GetMap(player.MapId);
        if (map is not null)
        {
            await map.BroadcastToAoiAsync(
                player.CellX,
                player.CellZ,
                GameOpcode.MsgAfkBroadcast,
                new MsgAfkBroadcast(player.UiId, stateByte).ToPayload(),
                excludeUiId: player.UiId,
                cancellationToken: ct).ConfigureAwait(false);
        }
    }

    public async Task HandlePrenticeAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgPrentice req = MsgPrentice.Parse(payload.Span);
        if (req.ActionType == 1 &&
            !string.IsNullOrWhiteSpace(req.MasterName) &&
            !string.IsNullOrWhiteSpace(req.PrenticeName))
        {
            await _socialRepo.AddMentorAsync(req.MasterName, req.PrenticeName, ct).ConfigureAwait(false);
        }

        IReadOnlyList<UserMentorRecord> rows = await _socialRepo.GetMentorRecordsAsync(player.Character.Name, ct).ConfigureAwait(false);
        var entries = new List<PrenticeEntry>(rows.Count);
        foreach (UserMentorRecord r in rows)
        {
            string partnerName = string.Equals(r.MasterName, player.Character.Name, StringComparison.OrdinalIgnoreCase)
                ? r.PrenticeName
                : r.MasterName;
            PlayerSession? online = _world.GetPlayerByName(partnerName);
            entries.Add(new PrenticeEntry(
                r.MasterName,
                r.PrenticeName,
                online?.EffectiveLevel ?? 1,
                (byte)(online?.Character.Profession ?? 0),
                online is not null));
        }

        await player.SendAsync(
            GameOpcode.MsgPrenticeList,
            new MsgPrenticeList(entries).ToPayload(),
            ct).ConfigureAwait(false);
    }

    public async Task HandleTargetInfoAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgTargetInfo req = MsgTargetInfo.Parse(payload.Span);
        PlayerSession? target = !string.IsNullOrWhiteSpace(req.AimName)
            ? _world.GetPlayerByName(req.AimName)
            : _world.GetPlayerByUiId(req.ResPlayerId);

        if (target is null)
        {
            return;
        }

        await player.SendAsync(
            GameOpcode.MsgSynGameData,
            new MsgSynGameData(target.UiId, target.ToClientObjectData(), (uint)Math.Max(0, target.Character.SkillPoint)).ToPayload(),
            ct).ConfigureAwait(false);
        await player.SendAsync(
            GameOpcode.MsgTargetInfo,
            new MsgTargetInfo(target.UiId, target.Character.Name).ToPayload(),
            ct).ConfigureAwait(false);
    }

    public Task HandleSetQuickEquipAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
        => player.SendAsync(GameOpcode.MsgSetQuickEquip, payload, ct).AsTask();

    // =========================================================================
    // Helpers
    // =========================================================================

    private Task PersistActiveSkillsAsync(PlayerSession player, CancellationToken ct)
    {
        var sb = new StringBuilder();
        foreach (ActiveSkillEntry a in player.ActiveSkillsList)
        {
            sb.Append(CultureInfo.InvariantCulture, $"[{a.MagicId},{a.Rank}]#");
        }
        return _characters.UpdateUserMagicAsync(player.Character.Id, sb.ToString(), ct);
    }

    private Task PersistPassiveSkillsAsync(PlayerSession player, CancellationToken ct)
    {
        var sb = new StringBuilder();
        foreach (PassiveSkillEntry p in player.PassiveSkillsList)
        {
            sb.Append(CultureInfo.InvariantCulture, $"[{p.SkillId},{p.Rank}]#");
        }
        return _characters.UpdateUserSkillAsync(player.Character.Id, sb.ToString(), ct);
    }

    private static int[] ParseCsvFixedInts(string? raw, int count)
    {
        var arr = new int[count];
        Array.Fill(arr, -1);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return arr;
        }

        string[] parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (int i = 0; i < Math.Min(count, parts.Length); i++)
        {
            if (int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
            {
                arr[i] = v;
            }
        }
        return arr;
    }
}
