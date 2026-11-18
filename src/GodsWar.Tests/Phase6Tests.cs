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

public sealed class Phase6Tests
{
    [Fact]
    public void Phase6_Packets_HaveExact10282SizesAndRoundTrip()
    {
        var mallItem = new MallItemEntry(
            4001, -1, -1, -1, -1, -1,
            0, 1, 0, 1, 0,
            1, 0, 0, 50, 0, 0,
            100, 1, 1, 0);
        var mallData = new MsgMallItemData(0, 4, 0, [mallItem]);
        byte[] mallBytes = mallData.ToPayload();
        Assert.Equal(MsgMallItemData.PayloadSize, mallBytes.Length); // 1412B payload (1416B framed)
        MsgMallItemData mallParsed = MsgMallItemData.Parse(mallBytes);
        Assert.Equal((byte)4, mallParsed.MaxTabIndex);
        Assert.Single(mallParsed.Items);
        Assert.Equal(4001, mallParsed.Items[0].BaseId);
        Assert.Equal(50u, mallParsed.Items[0].BuyConsume0);

        var mallSell = new MsgMallSell(0, 0, 2, 4001, 1);
        byte[] mallSellBytes = mallSell.ToPayload();
        Assert.Equal(MsgMallSell.PayloadSize, mallSellBytes.Length); // 20B
        Assert.Equal(4001, MsgMallSell.Parse(mallSellBytes).Base);

        var assocDiscount = new MsgAssociationDiscount([new AssociationDiscountEntry(0, 5, 80)]);
        byte[] discountBytes = assocDiscount.ToPayload();
        Assert.Equal(MsgAssociationDiscount.PayloadSize, discountBytes.Length); // 364B

        var goldBox = new MsgGoldBox(1, 4400, 0);
        byte[] gbBytes = goldBox.ToPayload();
        Assert.Equal(MsgGoldBox.PayloadSize, gbBytes.Length); // 12B
        Assert.Equal(4400u, MsgGoldBox.Parse(gbBytes).GoldBoxId);

        var goldBoxReturn = new MsgGoldBoxReturn(1, Enumerable.Range(100, 17).Select(x => (uint)x).ToArray());
        Assert.Equal(MsgGoldBoxReturn.PayloadSize, goldBoxReturn.ToPayload().Length); // 72B

        var goldBoxInDial = new MsgGoldBoxInDial(1, 4400, Enumerable.Range(100, 16).Select(x => (uint)x).ToArray());
        Assert.Equal(MsgGoldBoxInDial.PayloadSize, goldBoxInDial.ToPayload().Length); // 76B

        var skillUpgrade = new MsgSkillUpgrade(101, 5, 3, 40, 120);
        Assert.Equal(MsgSkillUpgrade.PayloadSize, skillUpgrade.ToPayload().Length); // 20B
        Assert.Equal(5u, MsgSkillUpgrade.Parse(skillUpgrade.ToPayload()).SkillId);

        var skillBackup = new MsgSkillBackup(
            [1, 2, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1],
            [4001, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1],
            5,
            4002);
        byte[] sbBytes = skillBackup.ToPayload();
        Assert.Equal(MsgSkillBackup.PayloadSize, sbBytes.Length); // 136B
        MsgSkillBackup sbParsed = MsgSkillBackup.Parse(sbBytes);
        Assert.Equal(2, sbParsed.SkillIds[1]);
        Assert.Equal(4001, sbParsed.ItemIds[0]);

        var stall = new MsgStall(101, true, "MyShop", "Seller");
        byte[] stallBytes = stall.ToPayload();
        Assert.Equal(MsgStall.PayloadSize, stallBytes.Length); // 104B
        Assert.Equal("MyShop", MsgStall.Parse(stallBytes).Text);

        var stallAdd = new MsgStallAddItem(0, 2, 0, 500, 10, 0, 123456789L);
        byte[] stallAddBytes = stallAdd.ToPayload();
        Assert.Equal(MsgStallAddItem.PayloadSize, stallAddBytes.Length); // 36B
        Assert.Equal(123456789L, MsgStallAddItem.Parse(stallAddBytes).StallId);

        var stallItem = new MsgStallItem(
            987654321L,
            "Seller",
            202,
            0,
            2,
            500,
            10,
            new ItemEquipSlot(4001, -1, -1, -1, -1, -1, 0, 1, 0, 1));
        byte[] stallItemBytes = stallItem.ToPayload();
        Assert.Equal(MsgStallItem.PayloadSize, stallItemBytes.Length); // 132B
        MsgStallItem stallItemParsed = MsgStallItem.Parse(stallItemBytes);
        Assert.Equal(987654321L, stallItemParsed.StallId);
        Assert.Equal(4001, stallItemParsed.Item.Base);

        var stallBuy = new MsgStallBuyItem(101, 202, "Seller", "Buyer", 987654321L, 0, 2);
        byte[] stallBuyBytes = stallBuy.ToPayload();
        Assert.Equal(MsgStallBuyItem.PayloadSize, stallBuyBytes.Length); // 92B
        Assert.Equal(987654321L, MsgStallBuyItem.Parse(stallBuyBytes).StallId);

        var petEntry = new PetDataEntry(
            77, "AthenaOwl", 1, 5, 2, 0, 15, 1, 100, 100, 0,
            250, 250, 100, 1000,
            30, 15, 30, 15, 95);
        var petList = new MsgPetList(1, [petEntry]);
        byte[] petListBytes = petList.ToPayload();
        Assert.Equal(4 + PetDataEntry.Size, petListBytes.Length); // 148B
        MsgPetList petListParsed = MsgPetList.Parse(petListBytes);
        Assert.Single(petListParsed.Pets);
        Assert.Equal("AthenaOwl", petListParsed.Pets[0].Name);

        var petRename = new MsgPetRename(77, "Hootie");
        Assert.Equal(MsgPetRename.PayloadSize, petRename.ToPayload().Length); // 20B
        Assert.Equal("Hootie", MsgPetRename.Parse(petRename.ToPayload()).NewName);

        var afk = new MsgAfkRequest(1);
        Assert.Equal(MsgAfkRequest.PayloadSize, afk.ToPayload().Length); // 2B
        Assert.Equal((byte)1, MsgAfkRequest.Parse(afk.ToPayload()).State);

        var prenticeList = new MsgPrenticeList([new PrenticeEntry("MasterA", "ApprenticeB", 50, 0, true)]);
        Assert.Equal(4 + 72, prenticeList.ToPayload().Length); // 76B
    }

    [Fact]
    public void WorldData_LoadsPhase6Catalogs_Mall_BijouForge_GoldBox_SkillIni()
    {
        string sceneDir = WorldDataLoader.ResolveSceneDirectory(null);
        var world = new WorldManager(Options.Create(new GameServerOptions { SceneDataPath = sceneDir }), NullLogger<WorldManager>.Instance);

        Assert.NotEmpty(world.Catalog.MallShopItems);
        Assert.NotEmpty(world.Catalog.BijouForges);
        Assert.NotEmpty(world.Catalog.GoldBoxes);
        Assert.NotEmpty(world.Catalog.PassiveSkills);
        Assert.True(world.Catalog.MallConfig.AthensMallItemLists.Count > 0 || world.Catalog.MallShopItems.Count > 0);
    }

    [Fact]
    public async Task ClientFeatures_Mall_Skill_Stall_Pet_Afk_Mentor_EndToEnd()
    {
        string sceneDir = WorldDataLoader.ResolveSceneDirectory(null);
        var world = new WorldManager(Options.Create(new GameServerOptions { SceneDataPath = sceneDir }), NullLogger<WorldManager>.Instance);
        var charRepo = new StubCharRepo();
        var socialRepo = new InMemorySocialRepo();
        var gameplay = new GameplayHandlers(world, charRepo, NullLogger<GameplayHandlers>.Instance);
        var features = new ClientFeaturesService(world, charRepo, socialRepo, gameplay);

        (PlayerSession seller, PacketConnection sellerConn) = CreateSession(world, 1001, "Seller", 0, 100f, 100f);
        (PlayerSession buyer, PacketConnection buyerConn) = CreateSession(world, 1002, "Buyer", 0, 102f, 100f);
        seller.Character.Stone = 5000;
        seller.Character.SkillPoint = 500;
        seller.Kitbags[0][0] = new ItemEquipSlot(4001, -1, -1, -1, -1, -1, 0, 1, 0, 5);
        buyer.Character.Money = 2000;

        WorldMap map = world.GetMap(1)!;
        await map.JoinAsync(seller, CancellationToken.None);
        await map.JoinAsync(buyer, CancellationToken.None);

        await features.LoadPlayerFeaturesAsync(seller, CancellationToken.None);
        Assert.NotEmpty(seller.Pets);

        // 1. Mall query (10172)
        await features.HandleMallItemDataAsync(seller, CancellationToken.None);
        (ushort mallOp, byte[] mallPayload) = await ReadUntilOpcodeAsync(sellerConn, (ushort)GameOpcode.MsgMallItemData);
        Assert.Equal((ushort)GameOpcode.MsgMallItemData, mallOp);
        MsgMallItemData mallResp = MsgMallItemData.Parse(mallPayload);
        Assert.NotEmpty(mallResp.Items);

        // 2. Skill upgrade (10047) & Skill backup (10164)
        await features.HandleSkillUpgradeAsync(seller, new MsgSkillUpgrade(seller.UiId, 1, 0, 0, 0).ToPayload(), CancellationToken.None);
        Assert.Contains(seller.PassiveSkillsList, s => s.SkillId == 1 && s.Rank == 1);

        var backupReq = new MsgSkillBackup(
            [1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1],
            [4001, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1],
            5,
            -1);
        await features.HandleSkillBackupAsync(seller, backupReq.ToPayload(), CancellationToken.None);
        Assert.Equal(1, seller.BackupSkillIds[0]);

        // 3. Stall open (10059) -> add item (10060) -> browse (10062) -> buy (10063)
        await features.HandleStallAsync(seller, new MsgStall(seller.UiId, true, "Potions!", seller.Character.Name).ToPayload(), CancellationToken.None);
        Assert.True(seller.StallActive);

        await features.HandleStallAddItemAsync(seller, new MsgStallAddItem(0, 0, 0, 0, 250, 0, 0).ToPayload(), CancellationToken.None);
        StallGoodsSlot placedSlot = seller.StallGoods[0, 0];
        Assert.False(placedSlot.IsEmpty);
        long stallId = placedSlot.StallId;

        await features.HandleStallItemQueryAsync(buyer, new MsgStallItem(0, seller.Character.Name, seller.UiId, 0, 0, 0, 0, ItemEquipSlot.Empty).ToPayload(), CancellationToken.None);
        (ushort browseOp, byte[] browsePayload) = await ReadUntilOpcodeAsync(buyerConn, (ushort)GameOpcode.MsgStallItem);
        Assert.Equal((ushort)GameOpcode.MsgStallItem, browseOp);
        Assert.Equal(4001, MsgStallItem.Parse(browsePayload).Item.Base);

        await features.HandleStallBuyItemAsync(buyer, new MsgStallBuyItem(seller.UiId, buyer.UiId, seller.Character.Name, buyer.Character.Name, stallId, 0, 0).ToPayload(), CancellationToken.None);
        Assert.Equal(250, seller.Character.Money);
        Assert.Equal(1750, buyer.Character.Money);
        Assert.Equal(4001, buyer.Kitbags[0][0].Base);

        // 4. Pet summon (10231), rename (10235), recall (10232)
        int petId = seller.Pets[0].Id;
        await features.HandlePetSummonAsync(seller, new MsgPetCommand(petId).ToPayload(), CancellationToken.None);
        Assert.Equal(2, seller.Pets[0].State);
        await features.HandlePetRenameAsync(seller, new MsgPetRename(petId, "Sparky").ToPayload(), CancellationToken.None);
        Assert.Equal("Sparky", seller.Pets[0].Name);
        await features.HandlePetRecallAsync(seller, new MsgPetCommand(petId).ToPayload(), CancellationToken.None);
        Assert.Equal(1, seller.Pets[0].State);

        // 5. AFK toggle (10295)
        await features.HandleAfkRequestAsync(seller, new MsgAfkRequest(1).ToPayload(), CancellationToken.None);
        Assert.True(seller.IsAfk);

        // 6. Mentor query (10227)
        await features.HandlePrenticeAsync(seller, new MsgPrentice(2, seller.Character.Name, string.Empty).ToPayload(), CancellationToken.None);
        (ushort mentorOp, _) = await ReadUntilOpcodeAsync(sellerConn, (ushort)GameOpcode.MsgPrenticeList);
        Assert.Equal((ushort)GameOpcode.MsgPrenticeList, mentorOp);
    }

    private static (PlayerSession Session, PacketConnection ClientSide) CreateSession(WorldManager world, uint uiId, string name, short gm, float x, float z)
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
            FighterJobLv = 10,
            ScholarJobLv = 1,
            CurHp = 500,
            MaxHp = 500,
            CurMp = 200,
            MaxMp = 200,
            BagNum = 1,
            StoreNum = 10,
            Map = 1,
            PosX = x,
            PosZ = z,
            Money = 0,
        };

        var equips = Enumerable.Repeat(ItemEquipSlot.Empty, 12).ToArray();
        ItemEquipSlot[][] bags =
        [
            Enumerable.Repeat(ItemEquipSlot.Empty, 24).ToArray(),
            Enumerable.Repeat(ItemEquipSlot.Empty, 24).ToArray(),
            Enumerable.Repeat(ItemEquipSlot.Empty, 24).ToArray(),
            Enumerable.Repeat(ItemEquipSlot.Empty, 24).ToArray(),
        ];

        var session = new PlayerSession(uiId, ch.Accounts, ch, serverConn, equips, bags, [], [])
        {
            InWorld = true,
            X = x,
            Z = z,
            MapId = 1,
        };
        world.RegisterPlayer(session);
        return (session, clientConn);
    }

    private static async Task<(ushort Opcode, byte[] Payload)> ReadUntilOpcodeAsync(PacketConnection conn, ushort expectedOpcode)
    {
        using var cts = new CancellationTokenSource(3000);
        while (true)
        {
            (ushort Opcode, byte[] Payload)? pkt = await conn.ReadPacketAsync(cts.Token);
            if (pkt is null)
            {
                throw new EndOfStreamException($"Stream ended before opcode {expectedOpcode}.");
            }
            if (pkt.Value.Opcode == expectedOpcode)
            {
                return pkt.Value;
            }
        }
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

    private sealed class InMemorySocialRepo : IWorldSocialRepository
    {
        private readonly List<UserPetRecord> _pets = [];
        private int _nextPetId = 1;
        private UserSkillBackupRecord? _backup;
        private readonly List<UserMentorRecord> _mentors = [];

        public Task<UserQuestRecord?> GetQuestRecordAsync(int userId, CancellationToken ct = default) => Task.FromResult<UserQuestRecord?>(null);
        public Task SaveQuestRecordAsync(UserQuestRecord record, CancellationToken ct = default) => Task.CompletedTask;
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
        public Task<UserSkillBackupRecord?> GetSkillBackupAsync(int userId, CancellationToken ct = default) => Task.FromResult(_backup);
        public Task SaveSkillBackupAsync(UserSkillBackupRecord record, CancellationToken ct = default) { _backup = record; return Task.CompletedTask; }
        public Task<IReadOnlyList<UserPetRecord>> GetUserPetsAsync(int userId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<UserPetRecord>>(_pets.Where(p => p.UserId == userId).ToList());
        public Task<UserPetRecord> CreatePetAsync(UserPetRecord pet, CancellationToken ct = default)
        {
            var created = new UserPetRecord
            {
                Id = _nextPetId++,
                UserId = pet.UserId,
                Name = pet.Name,
                TemplateId = pet.TemplateId,
                Level = pet.Level,
                Exp = pet.Exp,
                Quality = pet.Quality,
                Sex = pet.Sex,
                Savvy = pet.Savvy,
                State = pet.State,
                Loyalty = pet.Loyalty,
                Satiety = pet.Satiety,
                Rebirth = pet.Rebirth,
                Hp = pet.Hp,
                MaxHp = pet.MaxHp,
                Attack = pet.Attack,
                Defend = pet.Defend,
                MagicAttack = pet.MagicAttack,
                MagicDefend = pet.MagicDefend,
                Hit = pet.Hit,
            };
            _pets.Add(created);
            return Task.FromResult(created);
        }
        public Task UpdatePetAsync(UserPetRecord pet, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeletePetAsync(int userId, int petId, CancellationToken ct = default) { _pets.RemoveAll(p => p.UserId == userId && p.Id == petId); return Task.CompletedTask; }
        public Task<IReadOnlyList<UserMentorRecord>> GetMentorRecordsAsync(string playerName, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<UserMentorRecord>>(_mentors.Where(m => m.MasterName == playerName || m.PrenticeName == playerName).ToList());
        public Task AddMentorAsync(string masterName, string prenticeName, CancellationToken ct = default)
        {
            _mentors.Add(new UserMentorRecord { MasterName = masterName, PrenticeName = prenticeName });
            return Task.CompletedTask;
        }
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
