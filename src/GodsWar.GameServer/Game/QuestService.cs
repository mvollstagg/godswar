using System.Globalization;
using System.Text;
using GodsWar.Database.Models;
using GodsWar.Database.Repositories;
using GodsWar.GameServer.World;
using GodsWar.Protocol;
using GodsWar.Protocol.Packets;

namespace GodsWar.GameServer.Game;

public sealed class ActiveQuestState
{
    public int QuestId { get; init; }
    public int NpcId { get; init; }
    public int RewardNpcId { get; init; }
    public short[] CreatureCompletedCounts { get; } = new short[4];
    public short[] ItemCompletedCounts { get; } = new short[4];
    public bool[] ExplorerStates { get; } = new bool[4];
    public bool Completed { get; set; }
}

public sealed class QuestService
{
    private readonly WorldManager _world;
    private readonly ICharacterRepository _characters;
    private readonly IWorldSocialRepository _socialRepo;

    public QuestService(
        WorldManager world,
        ICharacterRepository characters,
        IWorldSocialRepository socialRepo)
    {
        _world = world;
        _characters = characters;
        _socialRepo = socialRepo;
    }

    public async Task LoadPlayerQuestsAsync(PlayerSession player, CancellationToken ct)
    {
        UserQuestRecord? rec = await _socialRepo.GetQuestRecordAsync(player.Character.Id, ct).ConfigureAwait(false);
        if (rec is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(rec.AllQuestStatus))
        {
            foreach (string tok in rec.AllQuestStatus.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (int.TryParse(tok, NumberStyles.Integer, CultureInfo.InvariantCulture, out int qid))
                {
                    player.CompletedQuests.Add(qid);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(rec.ActiveStatus))
        {
            foreach (string entry in rec.ActiveStatus.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                string[] parts = entry.Split(':');
                if (parts.Length < 3
                    || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int qid)
                    || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int npcId)
                    || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int rewardNpcId))
                {
                    continue;
                }

                var state = new ActiveQuestState
                {
                    QuestId = qid,
                    NpcId = npcId,
                    RewardNpcId = rewardNpcId,
                };

                if (parts.Length >= 4)
                {
                    string[] cCounts = parts[3].Split(',');
                    for (int i = 0; i < Math.Min(4, cCounts.Length); i++)
                    {
                        if (short.TryParse(cCounts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out short v))
                        {
                            state.CreatureCompletedCounts[i] = v;
                        }
                    }
                }

                if (parts.Length >= 5)
                {
                    string[] iCounts = parts[4].Split(',');
                    for (int i = 0; i < Math.Min(4, iCounts.Length); i++)
                    {
                        if (short.TryParse(iCounts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out short v))
                        {
                            state.ItemCompletedCounts[i] = v;
                        }
                    }
                }

                if (_world.Catalog.Quests.TryGetValue(qid, out QuestDef? def))
                {
                    state.Completed = IsQuestRequirementsMet(player, def, state);
                }

                player.ActiveQuests[qid] = state;
            }
        }
    }

    public async Task SavePlayerQuestsAsync(PlayerSession player, CancellationToken ct)
    {
        string allCompleted = string.Join(',', player.CompletedQuests.OrderBy(x => x));
        var sb = new StringBuilder();
        foreach (ActiveQuestState q in player.ActiveQuests.Values)
        {
            if (sb.Length > 0) sb.Append(';');
            sb.Append(q.QuestId).Append(':')
              .Append(q.NpcId).Append(':')
              .Append(q.RewardNpcId).Append(':')
              .Append(string.Join(',', q.CreatureCompletedCounts)).Append(':')
              .Append(string.Join(',', q.ItemCompletedCounts));
        }

        await _socialRepo.SaveQuestRecordAsync(
            new UserQuestRecord
            {
                Id = player.Character.Id,
                AllQuestStatus = allCompleted,
                ActiveStatus = sb.ToString(),
                TimeQuest = string.Empty,
            },
            ct).ConfigureAwait(false);
    }

    public async Task SendActiveQuestsOnLoginAsync(PlayerSession player, CancellationToken ct)
    {
        if (player.ActiveQuests.Count == 0)
        {
            return;
        }

        var wires = new List<PlayerQuestWire>(3);
        foreach (ActiveQuestState st in player.ActiveQuests.Values)
        {
            if (!_world.Catalog.Quests.TryGetValue(st.QuestId, out QuestDef? def))
            {
                continue;
            }

            wires.Add(ToWire(def, st));
            if (wires.Count == MsgPlayerAcceptQuests.MaxPerPacket)
            {
                await player.SendAsync(GameOpcode.MsgPlayerAcceptQuests, new MsgPlayerAcceptQuests(wires).ToPayload(), ct).ConfigureAwait(false);
                wires.Clear();
            }
        }

        if (wires.Count > 0)
        {
            await player.SendAsync(GameOpcode.MsgPlayerAcceptQuests, new MsgPlayerAcceptQuests(wires).ToPayload(), ct).ConfigureAwait(false);
        }
    }

    public async Task SendNpcQuestsAsync(PlayerSession player, uint npcId, NpcScriptDef script, CancellationToken ct)
    {
        var available = new List<NpcQuestEntry>();
        foreach (int qid in script.QuestIds)
        {
            if (!_world.Catalog.Quests.TryGetValue(qid, out QuestDef? def))
            {
                continue;
            }

            bool canAccept = CanAcceptQuest(player, def);
            available.Add(new NpcQuestEntry(qid, canAccept));
        }

        if (available.Count > 0)
        {
            await player.SendAsync(GameOpcode.MsgNpcQuests, new MsgNpcQuests(npcId, available).ToPayload(), ct).ConfigureAwait(false);
        }

        var completable = new List<int>();
        foreach (int qid in script.RewardQuestIds)
        {
            if (player.ActiveQuests.TryGetValue(qid, out ActiveQuestState? st)
                && _world.Catalog.Quests.TryGetValue(qid, out QuestDef? def)
                && IsQuestRequirementsMet(player, def, st))
            {
                completable.Add(qid);
            }
        }

        if (completable.Count > 0)
        {
            await player.SendAsync(GameOpcode.MsgNpcQuestReward, new MsgNpcQuestReward(npcId, completable).ToPayload(), ct).ConfigureAwait(false);
        }
    }

    public async Task HandleAcceptQuestAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgNpcAcceptQuest req = MsgNpcAcceptQuest.Parse(payload.Span);
        if (!_world.Catalog.Quests.TryGetValue(req.QuestId, out QuestDef? def))
        {
            return;
        }

        if (!CanAcceptQuest(player, def) || player.ActiveQuests.Count >= 12)
        {
            return;
        }

        int npcId = req.NpcId > 0 ? req.NpcId : (int)player.TalkingNpcId;
        int rewardNpcId = req.RewardNpcId > 0 ? req.RewardNpcId : npcId;
        var state = new ActiveQuestState
        {
            QuestId = def.QuestId,
            NpcId = npcId,
            RewardNpcId = rewardNpcId,
            Completed = IsQuestRequirementsMet(player, def, null),
        };
        player.ActiveQuests[def.QuestId] = state;

        var reply = new MsgNpcAcceptQuest(
            npcId,
            rewardNpcId,
            def.QuestId,
            def.QuestSort,
            def.SpecialFlags,
            def.Items,
            def.Creatures,
            def.ItemCounts,
            def.CreatureCounts,
            def.Money,
            def.Bijou,
            def.RewardMoney,
            def.RewardBijou,
            def.RewardItems);

        await player.SendAsync(GameOpcode.MsgNpcAcceptQuest, reply.ToPayload(), ct).ConfigureAwait(false);
        await SavePlayerQuestsAsync(player, ct).ConfigureAwait(false);
    }

    public async Task HandleCancelQuestAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgNpcCancelQuest req = MsgNpcCancelQuest.Parse(payload.Span);
        if (!player.ActiveQuests.Remove(req.QuestId))
        {
            return;
        }

        await player.SendAsync(
            GameOpcode.MsgNpcCancelQuest,
            new MsgNpcCancelQuest(req.NpcId, req.RewardNpcId, req.QuestId, true).ToPayload(),
            ct).ConfigureAwait(false);
        await SavePlayerQuestsAsync(player, ct).ConfigureAwait(false);
    }

    public async Task HandleCompleteQuestAsync(PlayerSession player, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        MsgNpcCompleteQuest req = MsgNpcCompleteQuest.Parse(payload.Span);
        if (!player.ActiveQuests.TryGetValue(req.QuestId, out ActiveQuestState? st)
            || !_world.Catalog.Quests.TryGetValue(req.QuestId, out QuestDef? def))
        {
            return;
        }

        if (!IsQuestRequirementsMet(player, def, st))
        {
            return;
        }

        // Remove required items if any
        for (int i = 0; i < 4; i++)
        {
            if (def.Items[i] > 0 && def.ItemCounts[i] > 0)
            {
                ConsumeItemFromBags(player, def.Items[i], def.ItemCounts[i]);
            }
        }

        player.ActiveQuests.Remove(req.QuestId);
        player.CompletedQuests.Add(req.QuestId);

        // Grant rewards
        player.Character.Money += def.RewardMoney;
        player.Character.Stone += def.RewardBijou;
        player.Character.Prestige += def.Credit;
        player.Character.SkillPoint += def.SkillPoint;

        bool leveledUp = false;
        if (def.Exp > 0)
        {
            player.AddExp((uint)def.Exp, _world.Catalog, out leveledUp);
        }

        ItemEquipSlot chosenReward = ItemEquipSlot.Empty;
        int choiceIdx = Math.Clamp(req.Choice, 0, 3);
        if (!def.RewardItems[choiceIdx].IsEmpty)
        {
            chosenReward = def.RewardItems[choiceIdx];
        }
        else
        {
            chosenReward = def.RewardItems.FirstOrDefault(r => !r.IsEmpty);
        }

        if (!chosenReward.IsEmpty && player.TryAddItemToBag(chosenReward, out int bagIdx, out int slotIdx))
        {
            await GameplayHandlers.SendKitbagPageForSlotAsync(player, bagIdx, slotIdx, ct).ConfigureAwait(false);
        }

        var rewardMsg = new MsgNpcRewardQuest(
            st.NpcId,
            req.RewardNpcId > 0 ? req.RewardNpcId : st.RewardNpcId,
            def.QuestId,
            req.Choice,
            def.Credit,
            def.Contribute,
            def.Exp,
            def.SkillPoint,
            def.RewardSpecialFlags,
            def.RewardSpecialCount,
            chosenReward);

        await player.SendAsync(GameOpcode.MsgNpcRewardQuest, rewardMsg.ToPayload(), ct).ConfigureAwait(false);
        await player.SendAsync(GameOpcode.MsgSelfProperty, new MsgSelfProperty(player.ToClientObjectData()).ToPayload(), ct).ConfigureAwait(false);

        if (leveledUp)
        {
            await player.SendAsync(GameOpcode.MsgUpgrade, GameplayHandlers.BuildUpgradePacket(player).ToPayload(), ct).ConfigureAwait(false);
        }

        await SavePlayerQuestsAsync(player, ct).ConfigureAwait(false);
        await _characters.UpdateCharacterStateAsync(player.Character, ct).ConfigureAwait(false);
        await PersistKitbagsAsync(player, ct).ConfigureAwait(false);
    }

    public async Task HandleFindQuestAsync(PlayerSession player, CancellationToken ct)
    {
        var available = new List<short>(QuestWireConstants.FindCount);
        foreach (QuestDef def in _world.Catalog.Quests.Values)
        {
            if (CanAcceptQuest(player, def))
            {
                available.Add((short)def.QuestId);
                if (available.Count >= QuestWireConstants.FindCount)
                {
                    break;
                }
            }
        }

        await player.SendAsync(GameOpcode.MsgFindQuest, new MsgFindQuestResult(available).ToPayload(), ct).ConfigureAwait(false);
    }

    public async Task OnMonsterKilledAsync(PlayerSession player, MonsterEntity monster, CancellationToken ct)
    {
        bool anyUpdated = false;
        foreach (ActiveQuestState st in player.ActiveQuests.Values)
        {
            if (!_world.Catalog.Quests.TryGetValue(st.QuestId, out QuestDef? def))
            {
                continue;
            }

            for (int i = 0; i < 4; i++)
            {
                if (def.Creatures[i] > 0 && st.CreatureCompletedCounts[i] < def.CreatureCounts[i])
                {
                    // Match creature ID or wild monster kill progress
                    if (def.Creatures[i] == (short)(monster.UiId % 10000) || def.Creatures[i] > 0)
                    {
                        st.CreatureCompletedCounts[i]++;
                        anyUpdated = true;
                        await player.SendAsync(
                            GameOpcode.MsgNpcQuestKillOrCast,
                            new MsgNpcQuestKillOrCast(def.QuestId, st.NpcId, true, def.Creatures[i]).ToPayload(),
                            ct).ConfigureAwait(false);
                    }
                }
            }

            st.Completed = IsQuestRequirementsMet(player, def, st);
        }

        if (anyUpdated)
        {
            await SavePlayerQuestsAsync(player, ct).ConfigureAwait(false);
        }
    }

    public bool CanAcceptQuest(PlayerSession player, QuestDef def)
    {
        if (player.ActiveQuests.ContainsKey(def.QuestId))
        {
            return false;
        }

        if (def.Repeat <= 1 && player.CompletedQuests.Contains(def.QuestId))
        {
            return false;
        }

        if (def.PrevQuestId > 0 && !player.CompletedQuests.Contains(def.PrevQuestId))
        {
            return false;
        }

        int lv = player.EffectiveLevel;
        if (lv < def.MinLevel || lv > def.MaxLevel)
        {
            return false;
        }

        if (def.RequiredFaction >= 0 && def.RequiredFaction != player.Character.Camp)
        {
            return false;
        }

        return true;
    }

    private static bool IsQuestRequirementsMet(PlayerSession player, QuestDef def, ActiveQuestState? st)
    {
        for (int i = 0; i < 4; i++)
        {
            if (def.CreatureCounts[i] > 0)
            {
                short done = st?.CreatureCompletedCounts[i] ?? 0;
                if (done < def.CreatureCounts[i])
                {
                    return false;
                }
            }

            if (def.Items[i] > 0 && def.ItemCounts[i] > 0)
            {
                if (CountItemInBags(player, def.Items[i]) < def.ItemCounts[i])
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static int CountItemInBags(PlayerSession player, int baseId)
    {
        int total = 0;
        int bags = Math.Clamp(player.Character.BagNum, 1, 4);
        for (int b = 0; b < bags; b++)
        {
            for (int s = 0; s < 24; s++)
            {
                ItemEquipSlot item = player.Kitbags[b][s];
                if (!item.IsEmpty && item.Base == baseId)
                {
                    total += Math.Max(1, (int)item.Overlap);
                }
            }
        }

        return total;
    }

    private static void ConsumeItemFromBags(PlayerSession player, int baseId, int count)
    {
        int remaining = count;
        int bags = Math.Clamp(player.Character.BagNum, 1, 4);
        for (int b = 0; b < bags && remaining > 0; b++)
        {
            for (int s = 0; s < 24 && remaining > 0; s++)
            {
                ItemEquipSlot item = player.Kitbags[b][s];
                if (item.IsEmpty || item.Base != baseId)
                {
                    continue;
                }

                int ov = Math.Max(1, (int)item.Overlap);
                if (ov <= remaining)
                {
                    remaining -= ov;
                    player.Kitbags[b][s] = ItemEquipSlot.Empty;
                }
                else
                {
                    player.Kitbags[b][s] = item with { Overlap = (byte)(ov - remaining) };
                    remaining = 0;
                }
            }
        }
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

    private static PlayerQuestWire ToWire(QuestDef def, ActiveQuestState st) => new(
        def.QuestId,
        st.NpcId,
        st.RewardNpcId,
        def.QuestSort,
        def.Money,
        def.Bijou,
        def.RewardMoney,
        def.RewardBijou,
        def.Items,
        def.Creatures,
        def.ItemCounts,
        def.CreatureCounts,
        st.ExplorerStates,
        (byte)def.SpecialFlags,
        0,
        st.Completed ? (byte)1 : (byte)0,
        st.ItemCompletedCounts,
        st.CreatureCompletedCounts,
        def.RewardItems);
}
