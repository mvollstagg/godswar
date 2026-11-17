using GodsWar.Database.Repositories;
using GodsWar.GameServer.World;
using GodsWar.Protocol;
using GodsWar.Protocol.Packets;
using Microsoft.Extensions.Logging;

namespace GodsWar.GameServer.Game;

/// <summary>
/// Handles Phase 4 gameplay packets: combat, skills, death/revive, drops/pickup,
/// item use/equip/move/split, and PostgreSQL persistence.
/// </summary>
public sealed class GameplayHandlers
{
    private readonly WorldManager _worldManager;
    private readonly ICharacterRepository _characterRepository;
    private readonly ILogger<GameplayHandlers> _logger;

    public GameplayHandlers(
        WorldManager worldManager,
        ICharacterRepository characterRepository,
        ILogger<GameplayHandlers> logger)
    {
        _worldManager = worldManager;
        _characterRepository = characterRepository;
        _logger = logger;
    }

    public async Task HandleFightAsync(PlayerSession player, byte[] payload, CancellationToken ct)
    {
        MsgFight msg = MsgFight.Parse(payload);
        player.InCombat = msg.Fight;
        player.TargetUiId = msg.Fight ? msg.TargetId : null;
        player.LastCombatAtUtc = DateTime.UtcNow;

        WorldMap? map = _worldManager.GetMap(player.MapId);
        if (map is not null)
        {
            await map.BroadcastToAoiAsync(
                player.CellX,
                player.CellZ,
                GameOpcode.MsgFight,
                new MsgFight(player.UiId, msg.TargetId, msg.Fight).ToPayload(),
                cancellationToken: ct).ConfigureAwait(false);
        }
    }

    public async Task HandleAttackAsync(PlayerSession player, byte[] payload, CancellationToken ct)
    {
        if (player.IsDead || !player.InWorld)
        {
            return;
        }

        DateTime now = DateTime.UtcNow;
        if (now < player.NextAttackAtUtc)
        {
            return;
        }

        MsgAttack msg = MsgAttack.Parse(payload);
        WorldMap? map = _worldManager.GetMap(player.MapId);
        MonsterEntity? monster = map?.GetMonster(msg.ObjectId);
        if (map is null || monster is null || monster.IsDead || player.FactionEquals(monster.Faction))
        {
            return;
        }

        float dx = player.X - monster.X;
        float dz = player.Z - monster.Z;
        if (dx * dx + dz * dz > 16.0f) // 4.0f tolerance
        {
            return;
        }

        player.NextAttackAtUtc = now.AddMilliseconds(800);
        player.InCombat = true;
        player.TargetUiId = monster.UiId;
        player.LastCombatAtUtc = now;

        PlayerCombatStats stats = player.ComputeCombatStats(_worldManager.Catalog);
        int mLv = (int)monster.Level;
        AttackResultType hitType = CombatCalculator.JudgeAttackStyle(
            stats.Hit,
            GameTables.GetMonsterDodge(mLv),
            stats.CritAppend,
            0);

        int damage = hitType == AttackResultType.Miss
            ? -1
            : CombatCalculator.CalculatePhysicalDamage(
                stats.Attack,
                GameTables.GetMonsterPhyDefend(mLv),
                isCrit: hitType == AttackResultType.Crit);

        if (damage > 0)
        {
            monster.Hp = monster.Hp > (uint)damage ? monster.Hp - (uint)damage : 0;
            monster.AddEnmity(player.UiId, damage);
        }
        else
        {
            monster.AddEnmity(player.UiId, 1);
        }

        var outAttack = new MsgAttack(
            player.UiId,
            player.X,
            player.Y,
            player.Z,
            monster.UiId,
            damage,
            0,
            (byte)hitType);

        await map.BroadcastToAoiAsync(
            player.CellX,
            player.CellZ,
            GameOpcode.MsgAttack,
            outAttack.ToPayload(),
            cancellationToken: ct).ConfigureAwait(false);

        if (monster.Hp == 0)
        {
            await HandleMonsterKilledAsync(player, map, monster, ct).ConfigureAwait(false);
        }
    }

    public async Task HandleSkillAsync(PlayerSession player, byte[] payload, CancellationToken ct)
    {
        if (player.IsDead || !player.InWorld)
        {
            return;
        }

        MsgSkill msg = MsgSkill.Parse(payload);
        WorldMap? map = _worldManager.GetMap(player.MapId);
        if (map is null)
        {
            return;
        }

        _worldManager.Catalog.Magics.TryGetValue(msg.MagicId, out MagicDef? magic);
        int mpCost = magic?.MpCost ?? 10;
        float cooldownSec = Math.Max(0.5f, magic?.CoolingSeconds ?? 1.0f);
        float power1 = magic?.Power1 ?? 0.2f;
        float power2 = magic?.Power2 ?? 25f;
        float range = magic?.Range ?? 0f;
        byte property = magic?.Property ?? 0;

        DateTime now = DateTime.UtcNow;
        if (player.SkillCooldowns.TryGetValue(msg.MagicId, out DateTime readyAt) && now < readyAt)
        {
            return;
        }

        if (player.Character.CurMp < mpCost)
        {
            return;
        }

        if (msg.Step == 0)
        {
            await map.BroadcastToAoiAsync(
                player.CellX,
                player.CellZ,
                GameOpcode.MsgMagicPerform,
                new MsgMagicPerform(player.UiId, msg.ObjectId, (uint)msg.MagicId, msg.TargetX, msg.TargetZ).ToPayload(),
                cancellationToken: ct).ConfigureAwait(false);
            return;
        }

        player.Character.CurMp = Math.Max(0, player.Character.CurMp - mpCost);
        player.SkillCooldowns[msg.MagicId] = now.AddSeconds(cooldownSec);
        player.InCombat = true;
        player.LastCombatAtUtc = now;

        await player.SendAsync(
            GameOpcode.MsgUpdateMp,
            new MsgUpdateMp(player.UiId, player.Character.CurMp).ToPayload(),
            ct).ConfigureAwait(false);

        PlayerCombatStats stats = player.ComputeCombatStats(_worldManager.Catalog);

        if (range > 0.5f)
        {
            List<MonsterEntity> targets = map.GetMonstersInRange(msg.TargetX, msg.TargetZ, range);
            var entries = new List<MagicClusterEntry>(targets.Count);
            var killed = new List<MonsterEntity>();

            foreach (MonsterEntity m in targets)
            {
                if (player.FactionEquals(m.Faction))
                {
                    continue;
                }

                int mLv = (int)m.Level;
                AttackResultType hitType = CombatCalculator.JudgeAttackStyle(stats.Hit, GameTables.GetMonsterDodge(mLv), stats.CritAppend, 0);
                int dmg = hitType == AttackResultType.Miss
                    ? 0
                    : property == 1
                        ? CombatCalculator.CalculateMagicDamage(stats.MagicAttack, GameTables.GetMonsterMagicDefend(mLv), 0f, power1, power2, isCrit: hitType == AttackResultType.Crit)
                        : CombatCalculator.CalculatePhysicalDamage(stats.Attack, GameTables.GetMonsterPhyDefend(mLv), 0f, power1, (int)power2, isCrit: hitType == AttackResultType.Crit);

                if (dmg > 0)
                {
                    m.Hp = m.Hp > (uint)dmg ? m.Hp - (uint)dmg : 0;
                    m.AddEnmity(player.UiId, dmg);
                    if (m.Hp == 0)
                    {
                        killed.Add(m);
                    }
                }

                entries.Add(new MagicClusterEntry(m.UiId, (byte)hitType, 0, dmg));
            }

            await map.BroadcastToAoiAsync(
                player.CellX,
                player.CellZ,
                GameOpcode.MsgMagicClusterDamage,
                new MsgMagicClusterDamage(player.UiId, (uint)msg.MagicId, 0, entries).ToPayload(),
                cancellationToken: ct).ConfigureAwait(false);

            foreach (MonsterEntity m in killed)
            {
                await HandleMonsterKilledAsync(player, map, m, ct).ConfigureAwait(false);
            }
            return;
        }

        MonsterEntity? monster = map.GetMonster((uint)msg.ObjectId);
        if (monster is null || monster.IsDead || player.FactionEquals(monster.Faction))
        {
            return;
        }

        int monLv = (int)monster.Level;
        AttackResultType singleHit = CombatCalculator.JudgeAttackStyle(stats.Hit, GameTables.GetMonsterDodge(monLv), stats.CritAppend, 0);
        int singleDmg = singleHit == AttackResultType.Miss
            ? 0
            : property == 1
                ? CombatCalculator.CalculateMagicDamage(stats.MagicAttack, GameTables.GetMonsterMagicDefend(monLv), 0f, power1, power2, isCrit: singleHit == AttackResultType.Crit)
                : CombatCalculator.CalculatePhysicalDamage(stats.Attack, GameTables.GetMonsterPhyDefend(monLv), 0f, power1, (int)power2, isCrit: singleHit == AttackResultType.Crit);

        if (singleDmg > 0)
        {
            monster.Hp = monster.Hp > (uint)singleDmg ? monster.Hp - (uint)singleDmg : 0;
            monster.AddEnmity(player.UiId, singleDmg);
        }

        await map.BroadcastToAoiAsync(
            player.CellX,
            player.CellZ,
            GameOpcode.MsgMagicDamage,
            new MsgMagicDamage(player.UiId, (int)monster.UiId, (byte)singleHit, 0, 0, singleDmg, msg.MagicId, monster.X, monster.Z).ToPayload(),
            cancellationToken: ct).ConfigureAwait(false);

        if (monster.Hp == 0)
        {
            await HandleMonsterKilledAsync(player, map, monster, ct).ConfigureAwait(false);
        }
    }

    public async Task HandleBackHomeAsync(PlayerSession player, byte[] payload, CancellationToken ct)
    {
        MsgBackHome msg = MsgBackHome.Parse(payload);
        (ushort revMap, float revX, float revZ) = _worldManager.GetStartingPosition((byte)player.Character.Camp);

        player.Character.CurHp = (int)player.EffectiveMaxHp;
        player.Character.CurMp = (int)player.EffectiveMaxMp;
        player.InCombat = false;
        player.TargetUiId = null;
        player.ActiveStatuses.Clear();

        WorldMap? oldMap = _worldManager.GetMap(player.MapId);
        if (oldMap is not null)
        {
            await oldMap.MoveAsync(player, revX, revZ, player.Atan2, 0, ct).ConfigureAwait(false);
        }
        else
        {
            player.MapId = revMap;
            player.X = revX;
            player.Z = revZ;
        }

        await player.SendAsync(GameOpcode.MsgBackHome, new MsgBackHome(player.UiId, msg.ReviveType).ToPayload(), ct).ConfigureAwait(false);
        ClientObjectData objData = player.ToClientObjectData();
        await player.SendAsync(GameOpcode.MsgSelfProperty, new MsgSelfProperty(objData).ToPayload(), ct).ConfigureAwait(false);
        await player.SendAsync(GameOpcode.MsgSynGameData, new MsgSynGameData(player.UiId, objData, (uint)Math.Max(0, player.Character.SkillPoint)).ToPayload(), ct).ConfigureAwait(false);
        await _characterRepository.UpdateCharacterStateAsync(player.Character, ct).ConfigureAwait(false);
    }

    public async Task HandlePickupDropsAsync(PlayerSession player, byte[] payload, CancellationToken ct)
    {
        if (player.IsDead || !player.InWorld)
        {
            return;
        }

        MsgPickupDrops msg = MsgPickupDrops.Parse(payload);
        WorldMap? map = _worldManager.GetMap(player.MapId);
        MonsterEntity? monster = map?.GetMonster(msg.MonsterId);
        if (monster is null || monster.GroundDrops.Count == 0)
        {
            return;
        }

        if (monster.DropOwnerUiId.HasValue && monster.DropOwnerUiId.Value != player.UiId)
        {
            return;
        }

        int idx = msg.PickupIndex >= 0 && msg.PickupIndex < monster.GroundDrops.Count ? msg.PickupIndex : 0;
        ItemEquipSlot item = monster.GroundDrops[idx];
        if (!player.TryAddItemToBag(item, out int bagIdx, out int slotIdx))
        {
            return;
        }

        monster.GroundDrops.RemoveAt(idx);
        await player.SendAsync(
            GameOpcode.MsgPickupDrops,
            new MsgPickupDrops(player.UiId, monster.UiId, idx).ToPayload(),
            ct).ConfigureAwait(false);

        await SendKitbagPageForSlotAsync(player, bagIdx, slotIdx, ct).ConfigureAwait(false);
        await PersistKitbagAsync(player, ct).ConfigureAwait(false);
    }

    public async Task HandleUseOrEquipAsync(PlayerSession player, byte[] payload, CancellationToken ct)
    {
        if (player.IsDead || !player.InWorld)
        {
            return;
        }

        MsgUseOrEquip msg = MsgUseOrEquip.Parse(payload);
        if (msg.Index >= 4 || msg.Num >= 24)
        {
            return;
        }

        ItemEquipSlot slot = player.Kitbags[msg.Index][msg.Num];
        if (slot.IsEmpty)
        {
            return;
        }

        _worldManager.Catalog.Items.TryGetValue(slot.Base, out ItemBaseDef? itemDef);

        // Consumable / Potion handling
        if (itemDef is null || itemDef.IsConsumable || itemDef.SlotType == EquipmentSlotType.Consume)
        {
            int hpHeal = slot.Base switch
            {
                4000 => 150,
                4001 => 350,
                4002 => 700,
                4003 => 1500,
                4010 or 4011 => 500,
                _ => 0,
            };
            int mpHeal = slot.Base switch
            {
                4030 => 100,
                4031 => 250,
                4032 => 500,
                4033 => 1000,
                4040 or 4041 => 400,
                _ => 0,
            };

            if (hpHeal == 0 && mpHeal == 0)
            {
                hpHeal = 100;
            }

            player.Character.CurHp = Math.Min((int)player.EffectiveMaxHp, player.Character.CurHp + hpHeal);
            player.Character.CurMp = Math.Min((int)player.EffectiveMaxMp, player.Character.CurMp + mpHeal);

            byte newOverlap = slot.Overlap > 1 ? (byte)(slot.Overlap - 1) : (byte)0;
            player.Kitbags[msg.Index][msg.Num] = newOverlap > 0
                ? slot with { Overlap = newOverlap }
                : ItemEquipSlot.Empty;

            await player.SendAsync(
                GameOpcode.MsgUseOrEquip,
                new MsgUseOrEquip(player.UiId, msg.Index, msg.Num, msg.ChangeF, slot.Base, slot.Append0, slot.Append1, slot.Append2, slot.Append3, slot.Append4, slot.AppendLevel, slot.BaseLevel, false).ToPayload(),
                ct).ConfigureAwait(false);
            await player.SendAsync(GameOpcode.MsgUpdateMp, new MsgUpdateMp(player.UiId, player.Character.CurMp).ToPayload(), ct).ConfigureAwait(false);
            await player.SendAsync(GameOpcode.MsgSynGameData, new MsgSynGameData(player.UiId, player.ToClientObjectData(), (uint)Math.Max(0, player.Character.SkillPoint)).ToPayload(), ct).ConfigureAwait(false);
            await SendKitbagPageForSlotAsync(player, msg.Index, msg.Num, ct).ConfigureAwait(false);
            await PersistKitbagAsync(player, ct).ConfigureAwait(false);
            await _characterRepository.UpdateCharacterStateAsync(player.Character, ct).ConfigureAwait(false);
            return;
        }

        // Equipment handling
        if (!itemDef.IsEquip || itemDef.SlotType is < EquipmentSlotType.Weapon or > EquipmentSlotType.Ring)
        {
            return;
        }

        if (itemDef.RequiredLevel > player.EffectiveLevel)
        {
            return;
        }

        int equipIdx = (int)itemDef.SlotType;
        ItemEquipSlot previousEquip = player.Equips[equipIdx];
        player.Equips[equipIdx] = slot with { Binding = 1 };
        player.Kitbags[msg.Index][msg.Num] = previousEquip;

        PlayerCombatStats stats = player.ComputeCombatStats(_worldManager.Catalog);
        player.Character.MaxHp = GameTables.GetPlayerMaxHp(player.EffectiveLevel, player.Character.Profession) + stats.MaxHpBonus;
        player.Character.MaxMp = GameTables.GetPlayerMaxMp(player.EffectiveLevel, player.Character.Profession) + stats.MaxMpBonus;
        player.Character.CurHp = Math.Min(player.Character.CurHp, player.Character.MaxHp);
        player.Character.CurMp = Math.Min(player.Character.CurMp, player.Character.MaxMp);

        await player.SendAsync(
            GameOpcode.MsgUseOrEquip,
            new MsgUseOrEquip(player.UiId, msg.Index, msg.Num, msg.ChangeF, slot.Base, slot.Append0, slot.Append1, slot.Append2, slot.Append3, slot.Append4, slot.AppendLevel, slot.BaseLevel, false).ToPayload(),
            ct).ConfigureAwait(false);

        ClientObjectData objData = player.ToClientObjectData();
        await player.SendAsync(GameOpcode.MsgSelfProperty, new MsgSelfProperty(objData).ToPayload(), ct).ConfigureAwait(false);
        await player.SendAsync(GameOpcode.MsgSynGameData, new MsgSynGameData(player.UiId, objData, (uint)Math.Max(0, player.Character.SkillPoint)).ToPayload(), ct).ConfigureAwait(false);
        await SendKitbagPageForSlotAsync(player, msg.Index, msg.Num, ct).ConfigureAwait(false);

        WorldMap? map = _worldManager.GetMap(player.MapId);
        if (map is not null)
        {
            await map.BroadcastToAoiAsync(
                player.CellX,
                player.CellZ,
                GameOpcode.MsgObjectInfo,
                player.ToObjectInfo().ToPayload(),
                excludeUiId: player.UiId,
                cancellationToken: ct).ConfigureAwait(false);
        }

        await PersistKitbagAsync(player, ct).ConfigureAwait(false);
        await _characterRepository.UpdateCharacterStateAsync(player.Character, ct).ConfigureAwait(false);
    }

    public async Task HandleMoveItemAsync(PlayerSession player, byte[] payload, CancellationToken ct)
    {
        MsgMoveItem msg = MsgMoveItem.Parse(payload);

        // Unequip from equipment slot (Num < 0) to bag
        if (msg.Num < 0)
        {
            if (msg.Index < 0 || msg.Index >= 12 || msg.IndexAim < 0 || msg.IndexAim >= 4 || msg.NumAim < 0 || msg.NumAim >= 24)
            {
                return;
            }

            if (player.Equips[msg.Index].IsEmpty || !player.Kitbags[msg.IndexAim][msg.NumAim].IsEmpty)
            {
                return;
            }

            player.Kitbags[msg.IndexAim][msg.NumAim] = player.Equips[msg.Index];
            player.Equips[msg.Index] = ItemEquipSlot.Empty;

            await player.SendAsync(GameOpcode.MsgMoveItem, msg with { UiId = player.UiId } is var outMsg ? outMsg.ToPayload() : payload, ct).ConfigureAwait(false);
            ClientObjectData objData = player.ToClientObjectData();
            await player.SendAsync(GameOpcode.MsgSelfProperty, new MsgSelfProperty(objData).ToPayload(), ct).ConfigureAwait(false);
            await player.SendAsync(GameOpcode.MsgSynGameData, new MsgSynGameData(player.UiId, objData, (uint)Math.Max(0, player.Character.SkillPoint)).ToPayload(), ct).ConfigureAwait(false);
            await PersistKitbagAsync(player, ct).ConfigureAwait(false);
            return;
        }

        if (msg.Index < 0 || msg.Index >= 4 || msg.Num >= 24)
        {
            return;
        }

        // Drop / delete item from bag
        if (msg.IndexAim < 0 || msg.NumAim < 0)
        {
            player.Kitbags[msg.Index][msg.Num] = ItemEquipSlot.Empty;
            await player.SendAsync(GameOpcode.MsgMoveItem, (msg with { UiId = player.UiId }).ToPayload(), ct).ConfigureAwait(false);
            await PersistKitbagAsync(player, ct).ConfigureAwait(false);
            return;
        }

        if (msg.IndexAim >= 4 || msg.NumAim >= 24)
        {
            return;
        }

        ItemEquipSlot src = player.Kitbags[msg.Index][msg.Num];
        ItemEquipSlot dst = player.Kitbags[msg.IndexAim][msg.NumAim];
        if (src.IsEmpty)
        {
            return;
        }

        // Stack if same base and stackable
        byte maxStack = _worldManager.Catalog.Items.TryGetValue(src.Base, out ItemBaseDef? def) ? def.MaxOverlap : (byte)1;
        if (!dst.IsEmpty && dst.Base == src.Base && maxStack > 1 && dst.Overlap < maxStack)
        {
            int total = dst.Overlap + src.Overlap;
            if (total <= maxStack)
            {
                player.Kitbags[msg.IndexAim][msg.NumAim] = dst with { Overlap = (byte)total };
                player.Kitbags[msg.Index][msg.Num] = ItemEquipSlot.Empty;
            }
            else
            {
                player.Kitbags[msg.IndexAim][msg.NumAim] = dst with { Overlap = maxStack };
                player.Kitbags[msg.Index][msg.Num] = src with { Overlap = (byte)(total - maxStack) };
            }
        }
        else
        {
            player.Kitbags[msg.IndexAim][msg.NumAim] = src;
            player.Kitbags[msg.Index][msg.Num] = dst;
        }

        await player.SendAsync(GameOpcode.MsgMoveItem, (msg with { UiId = player.UiId }).ToPayload(), ct).ConfigureAwait(false);
        await PersistKitbagAsync(player, ct).ConfigureAwait(false);
    }

    public async Task HandleBreakItemAsync(PlayerSession player, byte[] payload, CancellationToken ct)
    {
        MsgBreakItem msg = MsgBreakItem.Parse(payload);
        if (msg.Index < 0 || msg.Index >= 4 || msg.Num < 0 || msg.Num >= 24 ||
            msg.IndexAim < 0 || msg.IndexAim >= 4 || msg.NumAim < 0 || msg.NumAim >= 24 ||
            msg.Overlap <= 0)
        {
            return;
        }

        ItemEquipSlot src = player.Kitbags[msg.Index][msg.Num];
        ItemEquipSlot dst = player.Kitbags[msg.IndexAim][msg.NumAim];
        if (src.IsEmpty || !dst.IsEmpty || src.Overlap <= msg.Overlap)
        {
            return;
        }

        player.Kitbags[msg.Index][msg.Num] = src with { Overlap = (byte)(src.Overlap - msg.Overlap) };
        player.Kitbags[msg.IndexAim][msg.NumAim] = src with { Overlap = (byte)msg.Overlap };

        await player.SendAsync(
            GameOpcode.MsgBreakItem,
            new MsgBreakItem(2, msg.Index, msg.Num, msg.IndexAim, msg.NumAim, msg.Overlap).ToPayload(),
            ct).ConfigureAwait(false);
        await PersistKitbagAsync(player, ct).ConfigureAwait(false);
    }

    private async Task HandleMonsterKilledAsync(PlayerSession killer, WorldMap map, MonsterEntity monster, CancellationToken ct)
    {
        int baseExp = GameTables.GetMonsterExp((int)monster.Level);
        uint gainedExp = CombatCalculator.CalculateSoloExp(baseExp, killer.EffectiveLevel, (int)monster.Level);
        int moneyGain = Math.Max(1, (int)monster.Level * 3);
        killer.Character.Money += moneyGain;

        killer.AddExp(gainedExp, _worldManager.Catalog, out bool leveledUp);
        monster.Die(killer.UiId, _worldManager.Catalog);

        var deadMsg = new MsgDead(
            monster.UiId,
            [killer.Character.Id, -1, -1, -1, -1],
            [(uint)killer.Character.Money, 0, 0, 0, 0],
            [(uint)killer.Character.FighterJobExp, 0, 0, 0, 0],
            [(uint)killer.Character.SkillExp, 0, 0, 0, 0],
            [(uint)killer.Character.SkillPoint, 0, 0, 0, 0],
            killer.UiId,
            0);

        await map.BroadcastToAoiAsync(
            monster.CellX,
            monster.CellZ,
            GameOpcode.MsgDead,
            deadMsg.ToPayload(),
            cancellationToken: ct).ConfigureAwait(false);

        if (monster.GroundDrops.Count > 0)
        {
            await killer.SendAsync(
                GameOpcode.MsgDrops,
                new MsgDrops(monster.UiId, monster.GroundDrops).ToPayload(),
                ct).ConfigureAwait(false);
        }

        if (leveledUp)
        {
            var upgrade = new MsgUpgrade(
                killer.UiId,
                killer.EffectiveLevel,
                GameTables.GetPlayerNextGradeExp(killer.EffectiveLevel),
                (uint)killer.Character.FighterJobExp,
                killer.EffectiveMaxHp,
                killer.EffectiveCurHp,
                killer.EffectiveMaxMp,
                killer.EffectiveCurMp);

            await map.BroadcastToAoiAsync(
                killer.CellX,
                killer.CellZ,
                GameOpcode.MsgUpgrade,
                upgrade.ToPayload(),
                cancellationToken: ct).ConfigureAwait(false);
        }

        ClientObjectData objData = killer.ToClientObjectData();
        await killer.SendAsync(GameOpcode.MsgSelfProperty, new MsgSelfProperty(objData).ToPayload(), ct).ConfigureAwait(false);
        await killer.SendAsync(GameOpcode.MsgSynGameData, new MsgSynGameData(killer.UiId, objData, (uint)Math.Max(0, killer.Character.SkillPoint)).ToPayload(), ct).ConfigureAwait(false);
        await _characterRepository.UpdateCharacterStateAsync(killer.Character, ct).ConfigureAwait(false);

        _logger.LogDebug(
            "Player {Name} killed Monster {Monster} (UiId={MonsterId}), gained {Exp} XP (Lv={Level}, LeveledUp={LeveledUp}, Drops={Drops})",
            killer.Character.Name,
            monster.ObjectName,
            monster.UiId,
            gainedExp,
            killer.EffectiveLevel,
            leveledUp,
            monster.GroundDrops.Count);
    }

    private static async Task SendKitbagPageForSlotAsync(PlayerSession player, int bagIdx, int slotIdx, CancellationToken ct)
    {
        int bagNum = Math.Clamp(player.Character.BagNum, 1, 4);
        int half = slotIdx < 12 ? 0 : 12;
        uint flag12 = 0;
        for (int i = 0; i < 12; i++)
        {
            if (!player.Kitbags[bagIdx][half + i].IsEmpty)
            {
                flag12 |= 1u << i;
            }
        }

        var page = new MsgKitbagPage(
            player.UiId,
            (uint)bagNum,
            (byte)bagIdx,
            (byte)half,
            flag12,
            player.Kitbags[bagIdx][half..(half + 12)]);
        await player.SendAsync(GameOpcode.MsgKitbag, page.ToPayload(), ct).ConfigureAwait(false);
    }

    private Task PersistKitbagAsync(PlayerSession player, CancellationToken ct)
    {
        int bagNum = Math.Clamp(player.Character.BagNum, 1, 4);
        string k1 = ItemEquipSlot.FormatDbKitbagString(player.Kitbags[0]);
        string? k2 = bagNum >= 2 ? ItemEquipSlot.FormatDbKitbagString(player.Kitbags[1]) : null;
        string? k3 = bagNum >= 3 ? ItemEquipSlot.FormatDbKitbagString(player.Kitbags[2]) : null;
        string? k4 = bagNum >= 4 ? ItemEquipSlot.FormatDbKitbagString(player.Kitbags[3]) : null;
        string eq = ItemEquipSlot.FormatDbKitbagString(player.Equips);
        return _characterRepository.UpdateKitbagAsync(player.Character.Id, k1, k2, k3, k4, eq, ct);
    }
}
