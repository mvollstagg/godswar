using System.Buffers;
using System.Buffers.Binary;
using GodsWar.Database.Models;
using GodsWar.GameServer.Game;
using GodsWar.GameServer.World;
using GodsWar.Protocol.Packets;

namespace GodsWar.Tests;

public sealed class Phase4Tests
{
    [Fact]
    public void CombatAndInventoryPackets_MatchExpected10282SizesAndRoundtrips()
    {
        var fight = new MsgFight(10, 10005, true);
        Assert.Equal(12, fight.ToPayload().Length);
        Assert.Equal(fight, MsgFight.Parse(fight.ToPayload()));

        var atk = new MsgAttack(10, 1.5f, 0f, -2.5f, 10005, 42, 1, (byte)AttackResultType.Hit);
        Assert.Equal(28, atk.ToPayload().Length);
        Assert.Equal(atk, MsgAttack.Parse(atk.ToPayload()));

        var dead = new MsgDead(10005, [1, -1, -1, -1, -1], [50, 0, 0, 0, 0], [100, 0, 0, 0, 0], [0, 0, 0, 0, 0], [0, 0, 0, 0, 0], 10, 0);
        Assert.Equal(112, dead.ToPayload().Length);

        var backHome = new MsgBackHome(10, 1);
        Assert.Equal(8, backHome.ToPayload().Length);
        Assert.Equal(backHome, MsgBackHome.Parse(backHome.ToPayload()));

        var upgrade = new MsgUpgrade(10, 2, 500, 20, 250, 250, 80, 80);
        Assert.Equal(32, upgrade.ToPayload().Length);

        Assert.Equal(220, MsgParamater.CreateDefaultPayload().Length);

        var skill = new MsgSkill(10, 250, 1, 10005, 12, 10f, 20f, 12f, 22f);
        Assert.Equal(36, skill.ToPayload().Length);
        Assert.Equal(skill, MsgSkill.Parse(skill.ToPayload()));

        Assert.Equal(40, new MsgEffect(10, 1f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 1f).ToPayload().Length);
        Assert.Equal(32, new MsgMagicDamage(10, 10005, 1, 0, 0, 85, 250, 12f, 22f).ToPayload().Length);
        Assert.Equal(24, new MsgMagicPerform(10, 10005, 250, 12f, 22f).ToPayload().Length);
        Assert.Equal(33, new MsgMagicClusterDamage(10, 250, 0, [new MagicClusterEntry(10005, 1, 0, 50), new MagicClusterEntry(10006, 0, 0, 75)]).ToPayload().Length);
        Assert.Equal(8, new MsgUpdateMp(10, 55).ToPayload().Length);
        Assert.Equal(276, new MsgStatus(10, [(10u, (ushort)8)]).ToPayload().Length);

        var drops = new MsgDrops(10005, [new ItemEquipSlot(4000, -1, -1, -1, -1, -1, 1, 0, 0, 1)]);
        Assert.Equal(40, drops.ToPayload().Length);

        var pickup = new MsgPickupDrops(10, 10005, 0);
        Assert.Equal(12, pickup.ToPayload().Length);
        Assert.Equal(pickup, MsgPickupDrops.Parse(pickup.ToPayload()));

        var useEquip = new MsgUseOrEquip(10, 0, 2, 0, 4000, -1, -1, -1, -1, -1, 0, 1, false);
        Assert.Equal(40, useEquip.ToPayload().Length);
        Assert.Equal(useEquip, MsgUseOrEquip.Parse(useEquip.ToPayload()));

        var move = new MsgMoveItem(10, 0, 1, 0, 5, 1000, 1, -1, -1, -1, -1, 1, 1);
        Assert.Equal(40, move.ToPayload().Length);
        Assert.Equal(move, MsgMoveItem.Parse(move.ToPayload()));

        var brk = new MsgBreakItem(2, 0, 0, 0, 1, 5);
        Assert.Equal(12, brk.ToPayload().Length);
        Assert.Equal(brk, MsgBreakItem.Parse(brk.ToPayload()));
    }

    [Fact]
    public void ItemEquipSlot_FormatAndParseDbKitbagString_Roundtrips()
    {
        ItemEquipSlot[] slots =
        [
            new ItemEquipSlot(1000, 1, 10, -1, -1, -1, 2, 3, 1, 1),
            ItemEquipSlot.Empty,
            new ItemEquipSlot(4000, -1, -1, -1, -1, -1, 1, 0, 0, 15),
        ];

        string db = ItemEquipSlot.FormatDbKitbagString(slots);
        ItemEquipSlot[] parsed = ItemEquipSlot.ParseDbKitbagString(db, 3);
        Assert.Equal(slots, parsed);
    }

    [Fact]
    public void GameTables_And_CombatCalculator_MatchCppFormulas()
    {
        Assert.True(GameTables.GetPlayerMaxHp(1, 0) > 0);
        Assert.True(GameTables.GetPlayerMaxMp(1, 3) > 0);
        Assert.True(GameTables.GetPlayerNextGradeExp(1) > 0);
        Assert.True(GameTables.GetMonsterExp(1) > 0);
        Assert.True(GameTables.GetMonsterMaxHp(1) > 0);

        int normalDmg = CombatCalculator.CalculatePhysicalDamage(attack: 100, defend: 40, isCrit: false);
        int critDmg = CombatCalculator.CalculatePhysicalDamage(attack: 100, defend: 40, isCrit: true);
        Assert.Equal(60, normalDmg);
        Assert.Equal(90, critDmg);

        int magicDmg = CombatCalculator.CalculateMagicDamage(magicAttack: 120, magicDefend: 20, power1: 0.5f, power2: 30);
        Assert.Equal(180, magicDmg);

        uint soloExp = CombatCalculator.CalculateSoloExp(monsterExp: 100, playerLevel: 1, monsterLevel: 1);
        Assert.Equal(100u, soloExp);

        uint teamExp = CombatCalculator.CalculateTeamExp(monsterExp: 100, monsterLevel: 5, memberLevel: 5, teamLevelSum: 10, memberCount: 2);
        Assert.True(teamExp > 50);
    }

    [Fact]
    public void GameDataCatalog_LoadsItemsMagicsStatusesAndDropTables()
    {
        string sceneDir = WorldDataLoader.ResolveSceneDirectory(null);
        var catalog = GameDataCatalog.LoadFromSceneParent(sceneDir);

        Assert.True(catalog.Items.ContainsKey(1000)); // ShortSword
        Assert.True(catalog.Items.ContainsKey(4000)); // HPPotion_a
        Assert.True(catalog.Appends.ContainsKey(0));    // AttackA
        Assert.True(catalog.Magics.ContainsKey(0));     // Light Chop 1
        Assert.True(catalog.Statuses.ContainsKey(10));  // Bleeding
        Assert.True(catalog.DropTables.ContainsKey("1"));
    }

    [Fact]
    public void MonsterEntity_Lifecycle_DieDropsAndRespawn()
    {
        string sceneDir = WorldDataLoader.ResolveSceneDirectory(null);
        var catalog = GameDataCatalog.LoadFromSceneParent(sceneDir);

        var spawn = new MonsterSpawnData(
            10001, 1, "TestMob", 1, 100, 10f, 10f, 0f, 1, 10,
            Mode: 0, MoveTime: 0, DropNum: 1, DropProbability: 100,
            ConfirmDropNum: 1, ConfirmProbability: 100, ConfirmItemTable: "1");
        var monster = new MonsterEntity(spawn);

        monster.AddEnmity(1, 25);
        Assert.Equal(MonsterAiState.Fight, monster.State);
        Assert.Equal(1u, monster.TargetPlayerUiId);

        monster.Die(1, catalog);
        Assert.True(monster.IsDead);
        Assert.Equal(MonsterAiState.Dead, monster.State);
        Assert.NotEmpty(monster.GroundDrops);
        Assert.Equal(1u, monster.DropOwnerUiId);

        monster.Respawn();
        Assert.False(monster.IsDead);
        Assert.Equal(spawn.MaxHp, monster.Hp);
        Assert.Equal(MonsterAiState.Idle, monster.State);
        Assert.Empty(monster.GroundDrops);
    }

    [Fact]
    public async Task GameplayHandlers_CombatSkillPickupEquipUseAndSplitFlow()
    {
        var worldManager = new WorldManager(
            Microsoft.Extensions.Options.Options.Create(new GodsWar.GameServer.GameServerOptions()),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<WorldManager>.Instance);

        var repo = new StubCharRepo();
        var gameplay = new GameplayHandlers(
            worldManager,
            repo,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<GameplayHandlers>.Instance);

        // Create a test player with a potion stack and a sword in kitbag[0]
        var (s1, s2) = DuplexStreamPair.Create();
        var serverConn = new GodsWar.Protocol.PacketConnection(s1);
        var clientConn = new GodsWar.Protocol.PacketConnection(s2);

        var ch = new CharacterBase
        {
            Id = 999,
            Accounts = "test",
            Name = "Hero",
            Gender = "male",
            Camp = 0,
            Profession = 0,
            FighterJobLv = 1,
            ScholarJobLv = 1,
            MaxHp = 200,
            CurHp = 150,
            MaxMp = 60,
            CurMp = 60,
            BagNum = 1,
            Map = 1,
            PosX = 98f,
            PosZ = -178f,
        };

        ItemEquipSlot[] equips = Enumerable.Repeat(ItemEquipSlot.Empty, 12).ToArray();
        ItemEquipSlot[][] kitbags =
        [
            Enumerable.Repeat(ItemEquipSlot.Empty, 24).ToArray(),
            Enumerable.Repeat(ItemEquipSlot.Empty, 24).ToArray(),
            Enumerable.Repeat(ItemEquipSlot.Empty, 24).ToArray(),
            Enumerable.Repeat(ItemEquipSlot.Empty, 24).ToArray(),
        ];
        kitbags[0][0] = new ItemEquipSlot(4000, -1, -1, -1, -1, -1, 1, 0, 0, 5); // 5x HPPotion_a
        kitbags[0][1] = new ItemEquipSlot(1000, -1, -1, -1, -1, -1, 1, 0, 0, 1); // ShortSword

        var player = new PlayerSession(1, "test", ch, serverConn, equips, kitbags, [], []);
        WorldMap map = worldManager.GetMap(1)!;
        await map.JoinAsync(player);

        // Drain join packets on clientConn
        while (true)
        {
            using var cts = new CancellationTokenSource(20);
            try { await clientConn.ReadPacketAsync(cts.Token); } catch { break; }
        }

        // 1. Use potion at (0, 0) -> HP increases, overlap becomes 4
        await gameplay.HandleUseOrEquipAsync(player, new MsgUseOrEquip(1, 0, 0, 0, 4000, -1, -1, -1, -1, -1, 0, 1, false).ToPayload(), CancellationToken.None);
        Assert.Equal(200, player.Character.CurHp);
        Assert.Equal((byte)4, player.Kitbags[0][0].Overlap);

        // 2. Split 2 potions from (0, 0) to (0, 2)
        await gameplay.HandleBreakItemAsync(player, new MsgBreakItem(2, 0, 0, 0, 2, 2).ToPayload(), CancellationToken.None);
        Assert.Equal((byte)2, player.Kitbags[0][0].Overlap);
        Assert.Equal((byte)2, player.Kitbags[0][2].Overlap);

        // 3. Equip ShortSword from (0, 1)
        await gameplay.HandleUseOrEquipAsync(player, new MsgUseOrEquip(1, 0, 1, 0, 1000, -1, -1, -1, -1, -1, 0, 1, false).ToPayload(), CancellationToken.None);
        Assert.Equal(1000, player.Equips[(int)EquipmentSlotType.Weapon].Base);
        Assert.True(player.Kitbags[0][1].IsEmpty);
        Assert.True(repo.KitbagSavedCount >= 3);

        // 4. Move split potion from (0, 2) to (0, 3)
        await gameplay.HandleMoveItemAsync(player, new MsgMoveItem(1, 0, 2, 0, 3, 4000, -1, -1, -1, -1, -1, 0, 1).ToPayload(), CancellationToken.None);
        Assert.True(player.Kitbags[0][2].IsEmpty);
        Assert.Equal((byte)2, player.Kitbags[0][3].Overlap);

        await map.LeaveAsync(player);
    }

    private sealed class StubCharRepo : GodsWar.Database.Repositories.ICharacterRepository
    {
        public int KitbagSavedCount { get; private set; }
        public int StateSavedCount { get; private set; }

        public Task<IReadOnlyList<CharacterBase>> GetCharactersByAccountAsync(string account, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<CharacterBase>>([]);
        public Task<CharacterBase?> GetCharacterByIdAsync(int id, CancellationToken ct = default) => Task.FromResult<CharacterBase?>(null);
        public Task<CharacterBase?> GetCharacterByNameAsync(string name, CancellationToken ct = default) => Task.FromResult<CharacterBase?>(null);
        public Task<CharacterKitbag?> GetKitbagAsync(int userId, CancellationToken ct = default) => Task.FromResult<CharacterKitbag?>(null);
        public Task<string?> GetUserMagicAsync(int userId, CancellationToken ct = default) => Task.FromResult<string?>(null);
        public Task<string?> GetUserSkillAsync(int userId, CancellationToken ct = default) => Task.FromResult<string?>(null);
        public Task<CreateCharacterResult> CreateCharacterAsync(CreateCharacterParams request, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> DeleteCharacterAsync(string account, string name, CancellationToken ct = default) => Task.FromResult(false);
        public Task UpdateCharacterPositionAsync(int id, short map, float posX, float posZ, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateCharacterStateAsync(CharacterBase character, CancellationToken ct = default) { StateSavedCount++; return Task.CompletedTask; }
        public Task UpdateKitbagAsync(int userId, string k1, string? k2, string? k3, string? k4, string eq, CancellationToken ct = default) { KitbagSavedCount++; return Task.CompletedTask; }
        public Task UpdateStorageAsync(int userId, string storage, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateUserMagicAsync(int userId, string magicInfo, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateUserSkillAsync(int userId, string skillInfo, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class DuplexStreamPair : Stream
    {
        private readonly System.IO.Pipelines.PipeReader _reader;
        private readonly System.IO.Pipelines.PipeWriter _writer;
        private DuplexStreamPair(System.IO.Pipelines.PipeReader r, System.IO.Pipelines.PipeWriter w) { _reader = r; _writer = w; }
        public static (Stream A, Stream B) Create()
        {
            var p1 = new System.IO.Pipelines.Pipe();
            var p2 = new System.IO.Pipelines.Pipe();
            return (new DuplexStreamPair(p1.Reader, p2.Writer), new DuplexStreamPair(p2.Reader, p1.Writer));
        }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            var res = await _reader.ReadAsync(ct);
            if (res.Buffer.IsEmpty && res.IsCompleted) return 0;
            int n = (int)Math.Min(buffer.Length, res.Buffer.Length);
            res.Buffer.Slice(0, n).CopyTo(buffer.Span);
            _reader.AdvanceTo(res.Buffer.GetPosition(n));
            return n;
        }
        public override void Write(byte[] buffer, int offset, int count) => WriteAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
        {
            await _writer.WriteAsync(buffer, ct);
            await _writer.FlushAsync(ct);
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
