using GodsWar.Database.Models;
using GodsWar.Database.Repositories;
using GodsWar.GameServer.World;
using GodsWar.Protocol;
using GodsWar.Protocol.Packets;

namespace GodsWar.GameServer.Game;

public sealed class NpcShopAndForgeService
{
    private readonly WorldManager _world;
    private readonly ICharacterRepository _characters;
    private readonly IWorldSocialRepository _socialRepo;
    private readonly QuestService _quests;

    public NpcShopAndForgeService(
        WorldManager world,
        ICharacterRepository characters,
        IWorldSocialRepository socialRepo,
        QuestService quests)
    {
        _world = world;
        _characters = characters;
        _socialRepo = socialRepo;
        _quests = quests;
    }

    public async Task LoadAltarsAsync(PlayerSession player, CancellationToken ct)
    {
        UserAltarRecord? rec = await _socialRepo.GetAltarsAsync(player.Character.Id, ct).ConfigureAwait(false);
        if (rec is not null)
        {
            int[] arr = rec.ToArray();
            Array.Copy(arr, player.Altars, Math.Min(arr.Length, player.Altars.Length));
        }
    }

    public async Task HandleTalkNpcAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgTalkNpc req = MsgTalkNpc.Parse(payload.Span);
        NpcSpawnData? npc = _world.GetMap(player.MapId)?.GetNpc(req.NpcId) ?? _world.FindNpc(req.NpcId);
        if (npc is null)
        {
            return;
        }

        player.TalkingNpcId = req.NpcId;
        _world.Catalog.NpcScripts.TryGetValue(npc.Script, out NpcScriptDef? script);

        int npcFlag = script?.NpcFlag ?? 0;
        int sysFlag = script?.SysFlag ?? 0;
        string talkKey = script?.TalkKey ?? npc.Name;

        await player.SendAsync(
            GameOpcode.MsgTalkNpc,
            new MsgTalkNpc(req.NpcId, npcFlag, sysFlag, talkKey).ToPayload(),
            ct).ConfigureAwait(false);

        if (script is null)
        {
            return;
        }

        if ((npcFlag & (1 | 2)) != 0)
        {
            await _quests.SendNpcQuestsAsync(player, req.NpcId, script, ct).ConfigureAwait(false);
        }

        if ((npcFlag & 4) != 0 && script.ItemListIds.Count > 0)
        {
            byte maxTab = (byte)Math.Max(0, script.ItemListIds.Count - 1);
            for (int t = 0; t < script.ItemListIds.Count; t++)
            {
                int listId = script.ItemListIds[t];
                if (!_world.Catalog.ShopItemLists.TryGetValue(listId, out List<ShopItemDef>? shopItems))
                {
                    continue;
                }

                var entries = new List<NpcShopItemEntry>(shopItems.Count);
                foreach (ShopItemDef si in shopItems)
                {
                    byte buyType0 = si.TradeTypes.Length > 0 ? (byte)si.TradeTypes[0] : (byte)2;
                    byte buyType1 = si.TradeTypes.Length > 1 ? (byte)si.TradeTypes[1] : (byte)0;
                    byte buyType2 = si.TradeTypes.Length > 2 ? (byte)si.TradeTypes[2] : (byte)0;
                    uint c0 = si.ConsumeAmounts.Length > 0 ? (uint)Math.Max(0, si.ConsumeAmounts[0]) : 0;
                    uint c1 = si.ConsumeAmounts.Length > 1 ? (uint)Math.Max(0, si.ConsumeAmounts[1]) : 0;
                    uint c2 = si.ConsumeAmounts.Length > 2 ? (uint)Math.Max(0, si.ConsumeAmounts[2]) : 0;
                    byte numTypes = (byte)Math.Clamp(si.TradeTypes.Length, 1, 3);

                    entries.Add(new NpcShopItemEntry(
                        si.BaseId,
                        si.AppendLevel,
                        si.BaseLevel,
                        si.Binding,
                        si.Overlap,
                        1,
                        buyType0,
                        buyType1,
                        buyType2,
                        c0,
                        c1,
                        c2,
                        numTypes));
                }

                await player.SendAsync(
                    GameOpcode.MsgNpcItemData,
                    new MsgNpcItemData(req.NpcId, (byte)t, maxTab, entries).ToPayload(),
                    ct).ConfigureAwait(false);
            }
        }

        if ((npcFlag & 32) != 0)
        {
            await player.SendAsync(
                GameOpcode.MsgNpcStorageData,
                new MsgNpcStorageData(req.NpcId, Math.Max(10, player.Character.StoreNum)).ToPayload(),
                ct).ConfigureAwait(false);
        }
    }

    public async Task HandleSysNpcDataAsync(
        PlayerSession player,
        ReadOnlyMemory<byte> payload,
        ChatAndPortalService portalService,
        CancellationToken ct)
    {
        MsgSysNpcData req = MsgSysNpcData.Parse(payload.Span);
        // NpcType: 0=PrestigeExchange, 1=OfficialRank, 2=Salary, 3=Transmit/Portal, 4=Altar
        switch (req.NpcType)
        {
            case 3 when req.States.Length > 0 && req.States[0] > 0:
            {
                ushort targetMap = (ushort)req.States[0];
                WorldMap? dest = _world.GetMap(targetMap);
                if (dest is not null)
                {
                    float tx = player.Character.Camp == 0 ? dest.Scene.AthensX : dest.Scene.SpartaX;
                    float tz = player.Character.Camp == 0 ? dest.Scene.AthensZ : dest.Scene.SpartaZ;
                    await portalService.TransitionMapAsync(player, targetMap, tx, tz, ct).ConfigureAwait(false);
                }
                break;
            }
            case 4 when req.States.Length > 0:
            {
                int altarIdx = Math.Clamp(req.States[0], 0, 11);
                int cost = (player.Altars[altarIdx] + 1) * 100;
                if (player.Character.Money >= cost && player.Altars[altarIdx] < 10)
                {
                    player.Character.Money -= cost;
                    player.Altars[altarIdx]++;
                    var rec = new UserAltarRecord { UserId = player.Character.Id };
                    rec.FromArray(player.Altars);
                    await _socialRepo.SaveAltarsAsync(rec, ct).ConfigureAwait(false);
                    await _characters.UpdateCharacterStateAsync(player.Character, ct).ConfigureAwait(false);
                    await player.SendAsync(GameOpcode.MsgSelfProperty, new MsgSelfProperty(player.ToClientObjectData()).ToPayload(), ct).ConfigureAwait(false);
                }
                break;
            }
        }

        await player.SendAsync(GameOpcode.MsgSysNpcData, req.ToPayload(), ct).ConfigureAwait(false);
    }

    public async Task HandleNpcBuyAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgNpcSell req = MsgNpcSell.Parse(payload.Span);
        NpcSpawnData? npc = _world.GetMap(player.MapId)?.GetNpc(req.NpcId) ?? _world.FindNpc(req.NpcId);
        if (npc is null || !_world.Catalog.NpcScripts.TryGetValue(npc.Script, out NpcScriptDef? script))
        {
            return;
        }

        if (req.Index < 0 || req.Index >= script.ItemListIds.Count)
        {
            return;
        }

        int listId = script.ItemListIds[req.Index];
        if (!_world.Catalog.ShopItemLists.TryGetValue(listId, out List<ShopItemDef>? shopItems))
        {
            return;
        }

        if (req.Num < 0 || req.Num >= shopItems.Count)
        {
            return;
        }

        ShopItemDef shopItem = shopItems[req.Num];
        if (req.Base > 0 && shopItem.BaseId != req.Base)
        {
            return;
        }

        int buyCount = Math.Clamp(req.Count, 1, 99);
        int unitPrice = shopItem.ConsumeAmounts.Length > 0 && shopItem.ConsumeAmounts[0] > 0
            ? shopItem.ConsumeAmounts[0]
            : (_world.Catalog.Items.TryGetValue(shopItem.BaseId, out ItemBaseDef? bdef) ? Math.Max(1, bdef.Money) : 10);
        int totalCost = unitPrice * buyCount;
        int currencyType = shopItem.TradeTypes.Length > 0 ? shopItem.TradeTypes[0] : 2;

        if (currencyType == 3)
        {
            if (player.Character.Stone < totalCost) return;
            player.Character.Stone -= totalCost;
        }
        else if (currencyType == 0)
        {
            if (player.Character.Prestige < totalCost) return;
            player.Character.Prestige -= totalCost;
        }
        else
        {
            if (player.Character.Money < totalCost) return;
            player.Character.Money -= totalCost;
        }

        byte overlap = (byte)Math.Clamp(buyCount * Math.Max(1, (int)shopItem.Overlap), 1, 255);
        var newItem = new ItemEquipSlot(
            shopItem.BaseId,
            -1, -1, -1, -1, -1,
            shopItem.BaseLevel,
            shopItem.AppendLevel,
            shopItem.Binding,
            overlap);

        if (!player.TryAddItemToBag(newItem, out int bagIdx, out int slotIdx))
        {
            // Refund if bag full
            if (currencyType == 3) player.Character.Stone += totalCost;
            else if (currencyType == 0) player.Character.Prestige += totalCost;
            else player.Character.Money += totalCost;
            return;
        }

        await GameplayHandlers.SendKitbagPageForSlotAsync(player, bagIdx, slotIdx, ct).ConfigureAwait(false);
        await player.SendAsync(GameOpcode.MsgNpcSell, req.ToPayload(), ct).ConfigureAwait(false);
        await player.SendAsync(GameOpcode.MsgSelfProperty, new MsgSelfProperty(player.ToClientObjectData()).ToPayload(), ct).ConfigureAwait(false);

        await _characters.UpdateCharacterStateAsync(player.Character, ct).ConfigureAwait(false);
        await PersistKitbagsAsync(player, ct).ConfigureAwait(false);
    }

    public async Task HandleSellToNpcAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgSell req = MsgSell.Parse(payload.Span);
        int bagIdx = req.Index;
        int slotIdx = req.Num;
        if (bagIdx < 0 || bagIdx >= 4 || slotIdx < 0 || slotIdx >= 24)
        {
            return;
        }

        ItemEquipSlot item = player.Kitbags[bagIdx][slotIdx];
        if (item.IsEmpty)
        {
            return;
        }

        int sellPrice = _world.Catalog.Items.TryGetValue(item.Base, out ItemBaseDef? def)
            ? Math.Max(1, def.Money / 2)
            : 1;
        int totalGain = sellPrice * Math.Max(1, (int)item.Overlap);

        player.Kitbags[bagIdx][slotIdx] = ItemEquipSlot.Empty;
        player.Character.Money += totalGain;

        await GameplayHandlers.SendKitbagPageForSlotAsync(player, bagIdx, slotIdx, ct).ConfigureAwait(false);
        await player.SendAsync(GameOpcode.MsgSell, req.ToPayload(), ct).ConfigureAwait(false);
        await player.SendAsync(GameOpcode.MsgSelfProperty, new MsgSelfProperty(player.ToClientObjectData()).ToPayload(), ct).ConfigureAwait(false);

        await _characters.UpdateCharacterStateAsync(player.Character, ct).ConfigureAwait(false);
        await PersistKitbagsAsync(player, ct).ConfigureAwait(false);
    }

    public async Task HandleStorageItemAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgStorageItem req = MsgStorageItem.Parse(payload.Span);
        int bagIdx = req.IndexAim;
        int bagSlot = req.NumAim;
        int storeSlot = req.Index;
        if (bagIdx < 0 || bagIdx >= 4 || bagSlot < 0 || bagSlot >= 24 || storeSlot < 0 || storeSlot >= player.Storage.Length)
        {
            return;
        }

        ItemEquipSlot bagItem = player.Kitbags[bagIdx][bagSlot];
        ItemEquipSlot storeItem = player.Storage[storeSlot];

        if (req.ToStorage && bagItem.IsEmpty)
        {
            return;
        }
        if (!req.ToStorage && storeItem.IsEmpty)
        {
            return;
        }

        player.Kitbags[bagIdx][bagSlot] = storeItem;
        player.Storage[storeSlot] = bagItem;

        await GameplayHandlers.SendKitbagPageForSlotAsync(player, bagIdx, bagSlot, ct).ConfigureAwait(false);
        await player.SendAsync(GameOpcode.MsgStorageItem, req.ToPayload(), ct).ConfigureAwait(false);

        await PersistKitbagsAsync(player, ct).ConfigureAwait(false);
        await _characters.UpdateStorageAsync(player.Character.Id, ItemEquipSlot.FormatDbKitbagString(player.Storage), ct).ConfigureAwait(false);
    }

    public Task HandleForgeEquipSelectAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgEquipForgeEquip req = MsgEquipForgeEquip.Parse(payload.Span);
        if (req.Num is >= 0 and < 4 && req.Index is >= 0 and < 24)
        {
            player.ForgeEquipSlot = (req.Num, req.Index);
        }
        return player.SendAsync(GameOpcode.MsgEquipForgeEquip, req.ToPayload(), ct).AsTask();
    }

    public Task HandleForgeMaterialSelectAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgEquipForgeMaterial req = MsgEquipForgeMaterial.Parse(payload.Span);
        if (req.Index is >= 0 and < 4 && req.Num is >= 0 and < 24)
        {
            player.ForgeMaterialSlot = (req.Index, req.Num);
        }
        return player.SendAsync(GameOpcode.MsgEquipForgeMaterial, req.ToPayload(), ct).AsTask();
    }

    public async Task HandleEquipForgeAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgEquipForge req = MsgEquipForge.Parse(payload.Span);
        if (player.ForgeEquipSlot is not (int eBag, int eSlot))
        {
            return;
        }

        ItemEquipSlot equip = player.Kitbags[eBag][eSlot];
        if (equip.IsEmpty)
        {
            return;
        }

        int cost = 500;
        int successRate = 85;
        int nextBaseId = equip.Base;

        if (_world.Catalog.EquipForges.TryGetValue(equip.Base, out EquipForgeDef? forgeDef))
        {
            cost = forgeDef.AMoney > 0 ? forgeDef.AMoney : 500;
            if (req.Type == 0 && forgeDef.NextId > 0)
            {
                nextBaseId = forgeDef.NextId;
                successRate = Math.Clamp(forgeDef.Probability + 50, 25, 100);
            }
            else if (forgeDef.BaseProbAdd.Length > 0)
            {
                int idx = Math.Clamp(equip.BaseLevel - 1, 0, forgeDef.BaseProbAdd.Length - 1);
                successRate = Math.Clamp(60 + forgeDef.BaseProbAdd[idx], 20, 100);
            }
        }

        if (player.Character.Money < cost)
        {
            await player.SendAsync(
                GameOpcode.MsgEquipForge,
                new MsgEquipForge(false, req.Type, equip.BaseLevel, equip.AppendLevel, [equip.Append0, equip.Append1, equip.Append2, equip.Append3, equip.Append4]).ToPayload(),
                ct).ConfigureAwait(false);
            return;
        }

        player.Character.Money -= cost;

        // Consume material if selected
        if (player.ForgeMaterialSlot is (int mBag, int mSlot))
        {
            ItemEquipSlot mat = player.Kitbags[mBag][mSlot];
            if (!mat.IsEmpty)
            {
                player.Kitbags[mBag][mSlot] = mat.Overlap > 1
                    ? mat with { Overlap = (byte)(mat.Overlap - 1) }
                    : ItemEquipSlot.Empty;

                await GameplayHandlers.SendKitbagPageForSlotAsync(player, mBag, mSlot, ct).ConfigureAwait(false);
            }
        }

        bool success = Random.Shared.Next(100) < successRate;
        if (success)
        {
            byte newBaseLv = (byte)Math.Min(5, equip.BaseLevel + 1);
            byte newAppLv = (byte)Math.Min(12, equip.AppendLevel + (req.Type == 2 ? 1 : 0));
            equip = equip with
            {
                Base = req.Type == 0 ? nextBaseId : equip.Base,
                BaseLevel = newBaseLv,
                AppendLevel = newAppLv,
            };
            player.Kitbags[eBag][eSlot] = equip;

            await GameplayHandlers.SendKitbagPageForSlotAsync(player, eBag, eSlot, ct).ConfigureAwait(false);
        }

        await player.SendAsync(
            GameOpcode.MsgEquipForge,
            new MsgEquipForge(success, req.Type, equip.BaseLevel, equip.AppendLevel, [equip.Append0, equip.Append1, equip.Append2, equip.Append3, equip.Append4]).ToPayload(),
            ct).ConfigureAwait(false);
        await player.SendAsync(GameOpcode.MsgSelfProperty, new MsgSelfProperty(player.ToClientObjectData()).ToPayload(), ct).ConfigureAwait(false);

        await _characters.UpdateCharacterStateAsync(player.Character, ct).ConfigureAwait(false);
        await PersistKitbagsAsync(player, ct).ConfigureAwait(false);
    }

    private async Task PersistKitbagsAsync(PlayerSession player, CancellationToken ct)
    {
        await _characters.UpdateKitbagAsync(
            player.Character.Id,
            ItemEquipSlot.FormatDbKitbagString(player.Kitbags[0]),
            ItemEquipSlot.FormatDbKitbagString(player.Kitbags[1]),
            ItemEquipSlot.FormatDbKitbagString(player.Kitbags[2]),
            ItemEquipSlot.FormatDbKitbagString(player.Kitbags[3]),
            ItemEquipSlot.FormatDbKitbagString(player.Equips),
            ct).ConfigureAwait(false);
    }
}
