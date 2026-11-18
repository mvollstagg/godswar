using System.IO.Pipelines;
using GodsWar.Database.Models;
using GodsWar.Database.Repositories;
using GodsWar.GameServer;
using GodsWar.GameServer.Game;
using GodsWar.GameServer.World;
using GodsWar.Protocol;
using GodsWar.Protocol.Packets;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GodsWar.Tests;

public sealed class Phase5Tests
{
    [Fact]
    public void Phase5_Packets_HaveExact10282SizesAndRoundTrip()
    {
        var talk = new MsgTalk(10, ChatChannel.World, 0, 0, 0, "Hello Athens!", "Umut", string.Empty);
        MsgTalk talkParsed = MsgTalk.Parse(talk.ToPayload());
        Assert.Equal("Hello Athens!", talkParsed.Content);
        Assert.Equal("Umut", talkParsed.SenderName);
        Assert.Equal(ChatChannel.World, talkParsed.Channel);

        var sceneChange = new MsgSceneChange(1, 124f, 0f, 24f, 1, 2);
        byte[] scBytes = sceneChange.ToPayload();
        Assert.Equal(20, scBytes.Length);
        MsgSceneChange scParsed = MsgSceneChange.Parse(scBytes);
        Assert.Equal((ushort)2, scParsed.NextMapId);
        Assert.Equal(124f, scParsed.X);

        var tradeItem = new MsgTradeItem(1, 0, 4001, "Umut", new ItemEquipSlot(4001, -1, -1, -1, -1, -1, 1, 1, 0, 5));
        Assert.Equal(116, tradeItem.ToPayload().Length); // +4B header = 120B framed

        var teamInfo = new MsgTeamInfo(2, "DigerUmu", false, false);
        byte[] tiBytes = teamInfo.ToPayload();
        Assert.Equal(68, tiBytes.Length); // +4B header = 72B framed
        MsgTeamInfo tiParsed = MsgTeamInfo.Parse(tiBytes);
        Assert.Equal((byte)2, tiParsed.Mode);
        Assert.Equal("DigerUmu", tiParsed.AimName);

        var teamRefresh = new MsgTeamRefresh(
            new TeamMemberWire(1, 100, 100, 1, 0, "Umut", 1, 10f, 0f, 20f),
            [new TeamMemberWire(2, 120, 120, 2, 1, "DigerUmu", 1, 15f, 0f, 25f)],
            true);
        Assert.Equal(484, teamRefresh.ToPayload().Length); // +4B header = 488B framed

        var forge = new MsgEquipForge(true, 0, 2, 1, [-1, -1, -1, -1, -1]);
        byte[] forgeBytes = forge.ToPayload();
        Assert.Equal(36, forgeBytes.Length); // +4B header = 40B framed
        MsgEquipForge forgeParsed = MsgEquipForge.Parse(forgeBytes);
        Assert.True(forgeParsed.Success);
        Assert.Equal(2, forgeParsed.BaseLevel);
    }

    [Fact]
    public void WorldData_LoadsPortals_Scripts_Quests_Shops_And_Forges()
    {
        string sceneDir = WorldDataLoader.ResolveSceneDirectory(null);
        var world = new WorldManager(Options.Create(new GameServerOptions { SceneDataPath = sceneDir }), NullLogger<WorldManager>.Instance);

        WorldMap? athensNewbie = world.GetMap(1);
        Assert.NotNull(athensNewbie);
        Assert.NotEmpty(athensNewbie.Portals);
        PortalZone? toAthens = athensNewbie.Portals.FirstOrDefault(p => p.TargetMapName.Equals("Athens", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(toAthens);
        Assert.NotNull(athensNewbie.FindPortalAt((toAthens.MinX + toAthens.MaxX) / 2f, (toAthens.MinZ + toAthens.MaxZ) / 2f));

        Assert.True(world.Catalog.Quests.TryGetValue(1000, out QuestDef? q1000));
        Assert.Equal(100, q1000.Exp);

        Assert.True(world.Catalog.ShopItemLists.TryGetValue(101, out List<ShopItemDef>? list101));
        Assert.Contains(list101, i => i.BaseId == 4001);

        Assert.True(world.Catalog.NpcScripts.TryGetValue("Script.Athens.Athens_021", out NpcScriptDef? vendor));
        Assert.True((vendor.NpcFlag & 4) != 0);
        Assert.Contains(101, vendor.ItemListIds);

        Assert.True(world.Catalog.EquipForges.TryGetValue(1000, out EquipForgeDef? swordForge));
        Assert.Equal(1001, swordForge.NextId);
    }

    [Fact]
    public async Task QuestService_Accept_KillProgress_And_Complete_GrantsReward()
    {
        string sceneDir = WorldDataLoader.ResolveSceneDirectory(null);
        var world = new WorldManager(Options.Create(new GameServerOptions { SceneDataPath = sceneDir }), NullLogger<WorldManager>.Instance);
        var charRepo = new StubCharRepo();
        var socialRepo = new StubSocialRepo();
        var quests = new QuestService(world, charRepo, socialRepo);

        (PlayerSession player, PacketConnection clientConn) = CreateSession(world, 1, "HeroA", gm: 0);
        player.Character.Camp = 1; // Quest 1000 requires Sparta faction (1)
        int startMoney = player.Character.Money;

        // Quest 1000 is SpeakTo (no kill reqs)
        await quests.HandleAcceptQuestAsync(player, new MsgNpcAcceptQuest(5000, 5000, 1000, 0, 0, [], [], [], [], 0, 0, 0, 0, []).ToPayload(), CancellationToken.None);
        Assert.True(player.ActiveQuests.ContainsKey(1000));
        (ushort op1, _) = (await clientConn.ReadPacketAsync(CancellationToken.None))!.Value;
        Assert.Equal((ushort)GameOpcode.MsgNpcAcceptQuest, op1);

        await quests.HandleCompleteQuestAsync(player, BitConverter.GetBytes(5000).Concat(BitConverter.GetBytes(1000)).Concat(BitConverter.GetBytes(0)).ToArray(), CancellationToken.None);
        Assert.False(player.ActiveQuests.ContainsKey(1000));
        Assert.Contains(1000, player.CompletedQuests);
        Assert.True(player.Character.FighterJobExp > 0 || player.EffectiveLevel > 1);
        Assert.True(player.Character.Money >= startMoney);
        Assert.NotNull(socialRepo.SavedQuest);
    }

    [Fact]
    public async Task NpcShopAndForge_Buy_Sell_And_Forge_WorkEndToEnd()
    {
        string sceneDir = WorldDataLoader.ResolveSceneDirectory(null);
        var world = new WorldManager(Options.Create(new GameServerOptions { SceneDataPath = sceneDir }), NullLogger<WorldManager>.Instance);
        var charRepo = new StubCharRepo();
        var socialRepo = new StubSocialRepo();
        var quests = new QuestService(world, charRepo, socialRepo);
        var svc = new NpcShopAndForgeService(world, charRepo, socialRepo, quests);

        (PlayerSession player, PacketConnection clientConn) = CreateSession(world, 1, "Shopper", gm: 0);
        player.Character.Money = 10000;

        // Find vendor NPC in Athens (Script.Athens.Athens_021)
        WorldMap athens = world.GetMapByName("Athens")!;
        NpcSpawnData vendorNpc = Enumerable.Range(0, 16)
            .SelectMany(cx => Enumerable.Range(0, 16).Select(cz => (cx, cz)))
            .SelectMany(c => Enumerable.Empty<NpcSpawnData>())
            .FirstOrDefault()!;
        uint vendorId = 0;
        for (uint id = 5000; id < 5500; id++)
        {
            NpcSpawnData? cand = athens.GetNpc(id);
            if (cand is not null && cand.Script.Equals("Script.Athens.Athens_021", StringComparison.OrdinalIgnoreCase))
            {
                vendorId = id;
                break;
            }
        }
        Assert.True(vendorId > 0);

        await svc.HandleTalkNpcAsync(player, new MsgTalkNpc(vendorId, 0, 0, string.Empty).ToPayload(), CancellationToken.None);
        (ushort talkOp, _) = (await clientConn.ReadPacketAsync(CancellationToken.None))!.Value;
        (ushort shopOp, _) = (await clientConn.ReadPacketAsync(CancellationToken.None))!.Value;
        Assert.Equal((ushort)GameOpcode.MsgTalkNpc, talkOp);
        Assert.Equal((ushort)GameOpcode.MsgNpcItemData, shopOp);

        // Drain extra shop tab if any
        await svc.HandleNpcBuyAsync(player, new MsgNpcSell(vendorId, 0, 1, 1, 4001).ToPayload(), CancellationToken.None);
        Assert.Equal(4001, player.Kitbags[0][0].Base);
        Assert.True(player.Character.Money < 10000);

        // Sell item back
        int moneyBeforeSell = player.Character.Money;
        await svc.HandleSellToNpcAsync(player, new MsgSell(0, 0).ToPayload(), CancellationToken.None);
        Assert.True(player.Kitbags[0][0].IsEmpty);
        Assert.True(player.Character.Money > moneyBeforeSell);
    }

    [Fact]
    public async Task SocialService_Party_And_Trade_WorkEndToEnd()
    {
        string sceneDir = WorldDataLoader.ResolveSceneDirectory(null);
        var world = new WorldManager(Options.Create(new GameServerOptions { SceneDataPath = sceneDir }), NullLogger<WorldManager>.Instance);
        var charRepo = new StubCharRepo();
        var socialRepo = new StubSocialRepo();
        var social = new SocialService(world, charRepo, socialRepo);

        (PlayerSession p1, PacketConnection c1) = CreateSession(world, 1, "Umut", gm: 1);
        (PlayerSession p2, PacketConnection c2) = CreateSession(world, 2, "DigerUmu", gm: 0);
        p1.Character.Money = 100;
        p2.Character.Money = 100;
        p1.Kitbags[0][0] = new ItemEquipSlot(4001, -1, -1, -1, -1, -1, 1, 1, 0, 1);

        // Party: info -> invite -> accept
        await social.HandleTeamInfoAsync(p1, new MsgTeamInfo(2, "DigerUmu", false, false).ToPayload(), CancellationToken.None);
        (ushort tiOp, byte[] tiData) = (await c1.ReadPacketAsync(CancellationToken.None))!.Value;
        Assert.Equal((ushort)GameOpcode.MsgTeamInfo, tiOp);
        Assert.False(MsgTeamInfo.Parse(tiData).IsAimInTeam);

        await social.HandleTeamInviteAsync(p1, new MsgTeam(p1.UiId, "Umut", "DigerUmu").ToPayload(), CancellationToken.None);
        (ushort invOp, _) = (await c2.ReadPacketAsync(CancellationToken.None))!.Value;
        Assert.Equal((ushort)GameOpcode.MsgTeam, invOp);

        await social.HandleTeamAcceptAsync(p2, new MsgTeam(p1.UiId, "Umut", "DigerUmu").ToPayload(), CancellationToken.None);
        (ushort r1Op, byte[] r1Data) = (await c1.ReadPacketAsync(CancellationToken.None))!.Value;
        (ushort r2Op, byte[] r2Data) = (await c2.ReadPacketAsync(CancellationToken.None))!.Value;
        Assert.Equal((ushort)GameOpcode.MsgTeamRefresh, r1Op);
        Assert.Equal((ushort)GameOpcode.MsgTeamRefresh, r2Op);
        Assert.Equal(484, r1Data.Length);

        // Trade: request -> accept -> add item -> money -> lock x2
        await social.HandleTradeAsync(p1, new MsgTrade(p1.UiId, p2.UiId, 0, 0, "Umut", "DigerUmu").ToPayload(), CancellationToken.None);
        _ = await c2.ReadPacketAsync(CancellationToken.None);

        await social.HandleTradeAsync(p2, new MsgTrade(p2.UiId, p1.UiId, 1, 0, "DigerUmu", "Umut").ToPayload(), CancellationToken.None);
        _ = await c1.ReadPacketAsync(CancellationToken.None);
        _ = await c2.ReadPacketAsync(CancellationToken.None);

        byte[] addPayload = new byte[80];
        BitConverter.GetBytes(p1.UiId).CopyTo(addPayload, 0);
        await social.HandleTradeAddItemAsync(p1, addPayload, CancellationToken.None);
        (ushort tiA, byte[] tiAData) = (await c1.ReadPacketAsync(CancellationToken.None))!.Value;
        (ushort tiB, byte[] tiBData) = (await c2.ReadPacketAsync(CancellationToken.None))!.Value;
        Assert.Equal((ushort)GameOpcode.MsgTradeItem, tiA);
        Assert.Equal((ushort)GameOpcode.MsgTradeItem, tiB);
        Assert.Equal(116, tiAData.Length);

        await social.HandleTradeMoneyAsync(p2, new MsgTradeMoney(p2.UiId, 25, 0, "DigerUmu").ToPayload(), CancellationToken.None);
        _ = await c1.ReadPacketAsync(CancellationToken.None);
        _ = await c2.ReadPacketAsync(CancellationToken.None);

        await social.HandleTradeAsync(p1, new MsgTrade(p1.UiId, p2.UiId, 2, 1, "Umut", "DigerUmu").ToPayload(), CancellationToken.None);
        _ = await c1.ReadPacketAsync(CancellationToken.None);
        _ = await c2.ReadPacketAsync(CancellationToken.None);

        await social.HandleTradeAsync(p2, new MsgTrade(p2.UiId, p1.UiId, 2, 2, "DigerUmu", "Umut").ToPayload(), CancellationToken.None);

        Assert.True(p1.Kitbags[0][0].IsEmpty);
        Assert.Equal(4001, p2.Kitbags[0][0].Base);
        Assert.Equal(125, p1.Character.Money);
        Assert.Equal(75, p2.Character.Money);
    }

    [Fact]
    public async Task ChatAndPortal_TransitionMap_SendsSceneChange_And_MovesPlayer()
    {
        string sceneDir = WorldDataLoader.ResolveSceneDirectory(null);
        var world = new WorldManager(Options.Create(new GameServerOptions { SceneDataPath = sceneDir }), NullLogger<WorldManager>.Instance);
        var charRepo = new StubCharRepo();
        var socialRepo = new StubSocialRepo();
        var social = new SocialService(world, charRepo, socialRepo);
        var chatPortal = new ChatAndPortalService(world, charRepo, social);

        (PlayerSession player, PacketConnection clientConn) = CreateSession(world, 1, "Traveler", gm: 1);
        await world.GetMap(1)!.JoinAsync(player, CancellationToken.None);
        // Drain join packets
        while (true)
        {
            using var cts = new CancellationTokenSource(20);
            try { _ = await clientConn.ReadPacketAsync(cts.Token); } catch { break; }
        }

        await chatPortal.TransitionMapAsync(player, 2, 124f, 24f, CancellationToken.None);
        (ushort op, byte[] data) = (await clientConn.ReadPacketAsync(CancellationToken.None))!.Value;
        Assert.Equal((ushort)GameOpcode.MsgSceneChange, op);
        MsgSceneChange sc = MsgSceneChange.Parse(data);
        Assert.Equal((ushort)1, sc.CurrentMapId);
        Assert.Equal((ushort)2, sc.NextMapId);
        Assert.Equal((ushort)2, player.MapId);
    }

    private static (PlayerSession Session, PacketConnection ClientSide) CreateSession(WorldManager world, uint uiId, string name, short gm)
    {
        (Stream serverStream, Stream clientStream) = DuplexStreamPair.Create();
        var serverConn = new PacketConnection(serverStream, encrypted: false);
        var clientConn = new PacketConnection(clientStream, encrypted: false);

        var ch = new CharacterBase
        {
            Id = (int)uiId + 100,
            Accounts = name.ToLowerInvariant(),
            Name = name,
            Gender = "male",
            Gm = gm,
            Camp = 0,
            Profession = 0,
            FighterJobLv = 1,
            ScholarJobLv = 1,
            CurHp = 150,
            MaxHp = 150,
            CurMp = 50,
            MaxMp = 50,
            BagNum = 1,
            StoreNum = 10,
            Map = 1,
            PosX = 98f,
            PosZ = -178f,
            Money = 100,
        };

        var equips = Enumerable.Repeat(ItemEquipSlot.Empty, 12).ToArray();
        ItemEquipSlot[][] bags =
        [
            Enumerable.Repeat(ItemEquipSlot.Empty, 24).ToArray(),
            Enumerable.Repeat(ItemEquipSlot.Empty, 24).ToArray(),
            Enumerable.Repeat(ItemEquipSlot.Empty, 24).ToArray(),
            Enumerable.Repeat(ItemEquipSlot.Empty, 24).ToArray(),
        ];

        var session = new PlayerSession(uiId, ch.Accounts, ch, serverConn, equips, bags, [], []);
        session.InWorld = true;
        world.RegisterPlayer(session);
        return (session, clientConn);
    }

    private sealed class StubCharRepo : ICharacterRepository
    {
        public Task<IReadOnlyList<CharacterBase>> GetCharactersByAccountAsync(string account, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<CharacterBase>>([]);
        public Task<CharacterBase?> GetCharacterByIdAsync(int id, CancellationToken ct = default) => Task.FromResult<CharacterBase?>(null);
        public Task<CharacterBase?> GetCharacterByNameAsync(string name, CancellationToken ct = default) => Task.FromResult<CharacterBase?>(null);
        public Task<CharacterKitbag?> GetKitbagAsync(int userId, CancellationToken ct = default) => Task.FromResult<CharacterKitbag?>(null);
        public Task<string?> GetUserMagicAsync(int userId, CancellationToken ct = default) => Task.FromResult<string?>(null);
        public Task<string?> GetUserSkillAsync(int userId, CancellationToken ct = default) => Task.FromResult<string?>(null);
        public Task<CreateCharacterResult> CreateCharacterAsync(CreateCharacterParams request, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> DeleteCharacterAsync(string account, string name, CancellationToken ct = default) => Task.FromResult(false);
        public Task UpdateCharacterPositionAsync(int id, short map, float posX, float posZ, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateCharacterStateAsync(CharacterBase character, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateKitbagAsync(int userId, string k1, string? k2, string? k3, string? k4, string eq, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateStorageAsync(int userId, string storage, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateUserMagicAsync(int userId, string magicInfo, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateUserSkillAsync(int userId, string skillInfo, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubSocialRepo : IWorldSocialRepository
    {
        public UserQuestRecord? SavedQuest { get; private set; }
        public Task<UserQuestRecord?> GetQuestRecordAsync(int userId, CancellationToken ct = default) => Task.FromResult<UserQuestRecord?>(null);
        public Task SaveQuestRecordAsync(UserQuestRecord record, CancellationToken ct = default) { SavedQuest = record; return Task.CompletedTask; }
        public Task<UserRelationRecord?> GetRelationsAsync(int userId, CancellationToken ct = default) => Task.FromResult<UserRelationRecord?>(null);
        public Task SaveRelationsAsync(UserRelationRecord record, CancellationToken ct = default) => Task.CompletedTask;
        public Task<UserAltarRecord?> GetAltarsAsync(int userId, CancellationToken ct = default) => Task.FromResult<UserAltarRecord?>(null);
        public Task SaveAltarsAsync(UserAltarRecord record, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<ConsortiaRecord>> GetAllConsortiaAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ConsortiaRecord>>([]);
        public Task<ConsortiaRecord?> GetConsortiaByIdAsync(int consortiaId, CancellationToken ct = default) => Task.FromResult<ConsortiaRecord?>(null);
        public Task<ConsortiaRecord?> GetConsortiaByNameAsync(string name, CancellationToken ct = default) => Task.FromResult<ConsortiaRecord?>(null);
        public Task<ConsortiaRecord?> CreateConsortiaAsync(string name, string placard, CancellationToken ct = default) => Task.FromResult<ConsortiaRecord?>(null);
        public Task UpdateConsortiaAsync(ConsortiaRecord record, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteConsortiaAsync(int consortiaId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<ConsortiaMemberView>> GetConsortiaMembersAsync(int consortiaId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ConsortiaMemberView>>([]);
        public Task UpdateMemberConsortiaAsync(int userId, int consortiaId, short job, int contribute, CancellationToken ct = default) => Task.CompletedTask;
        public Task EnsurePhase6SchemaAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task<UserSkillBackupRecord?> GetSkillBackupAsync(int userId, CancellationToken ct = default) => Task.FromResult<UserSkillBackupRecord?>(null);
        public Task SaveSkillBackupAsync(UserSkillBackupRecord record, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<UserPetRecord>> GetUserPetsAsync(int userId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<UserPetRecord>>([]);
        public Task<UserPetRecord> CreatePetAsync(UserPetRecord pet, CancellationToken ct = default) => Task.FromResult(pet);
        public Task UpdatePetAsync(UserPetRecord pet, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeletePetAsync(int userId, int petId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<UserMentorRecord>> GetMentorRecordsAsync(string playerName, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<UserMentorRecord>>([]);
        public Task AddMentorAsync(string masterName, string prenticeName, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class DuplexStreamPair : Stream
    {
        private readonly PipeReader _reader;
        private readonly PipeWriter _writer;
        private DuplexStreamPair(PipeReader r, PipeWriter w) { _reader = r; _writer = w; }
        public static (Stream A, Stream B) Create()
        {
            var p1 = new Pipe();
            var p2 = new Pipe();
            return (new DuplexStreamPair(p1.Reader, p2.Writer), new DuplexStreamPair(p2.Reader, p1.Writer));
        }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) => _reader.AsStream().ReadAsync(buffer, ct);
        public override void Write(byte[] buffer, int offset, int count) => WriteAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default) => _writer.AsStream().WriteAsync(buffer, ct);
        public override Task FlushAsync(CancellationToken ct) => _writer.FlushAsync(ct).AsTask();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
