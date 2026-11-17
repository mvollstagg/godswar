using System.IO.Pipelines;
using GodsWar.Database.Models;
using GodsWar.Database.Repositories;
using GodsWar.GameServer;
using GodsWar.GameServer.Auth;
using GodsWar.GameServer.Network;
using GodsWar.GameServer.World;
using GodsWar.Protocol;
using GodsWar.Protocol.Packets;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GodsWar.Tests;

public class GameServerTests
{
    [Fact]
    public void WorldDataLoader_LoadsScenesNpcsAndMonstersWithUsNames()
    {
        string sceneDir = WorldDataLoader.ResolveSceneDirectory(null);
        LoadedWorldData data = WorldDataLoader.Load(sceneDir);

        Assert.Equal(24, data.Scenes.Count);
        Assert.True(data.NpcsByMap.TryGetValue(1, out IReadOnlyList<NpcSpawnData>? athensNewbieNpcs));
        Assert.Equal(17, athensNewbieNpcs.Count);
        Assert.Contains(athensNewbieNpcs, n => n.Name == "Athens_Newbie_008_AthenianCivilian1");

        // Verify GBK Chinese NPC names in Sparta (map 50) resolve to US 1.0.282 keys
        Assert.True(data.NpcsByMap.TryGetValue(50, out IReadOnlyList<NpcSpawnData>? spartaNpcs));
        Assert.True(spartaNpcs.Count > 0);
        Assert.All(spartaNpcs, n => Assert.StartsWith("Sparta_", n.Name));

        int totalMonsters = data.MonstersByMap.Values.Sum(m => m.Count);
        Assert.Equal(15449, totalMonsters);
    }

    [Fact]
    public async Task WorldMap_SuppressesSelfWalkEcho_AndBroadcastsToAoiNeighbors()
    {
        var scene = new SceneDefinition(1, "Athens_Newbie", 1, 98f, -178f, 51, -108f, -122f);
        var npc = new NpcSpawnData(5001, 1, "Athens_Newbie_001_Male30", "Script", 100f, 0f, -172f, 1.7f, 2);
        var map = new WorldMap(scene, [npc], []);

        await using var link1 = DuplexPipeStream.CreatePair();
        await using var link2 = DuplexPipeStream.CreatePair();

        await using var conn1 = new PacketConnection(link1.ServerStream, encrypted: false);
        await using var client1 = new PacketConnection(link1.ClientStream, encrypted: false);
        await using var conn2 = new PacketConnection(link2.ServerStream, encrypted: false);
        await using var client2 = new PacketConnection(link2.ClientStream, encrypted: false);

        var p1 = CreatePlayerSession(1, "test1", "HeroOne", 98f, -178f, conn1);
        var p2 = CreatePlayerSession(2, "test2", "HeroTwo", 100f, -176f, conn2);

        await map.JoinAsync(p1);
        // p1 receives NPC 5001
        var p1First = await client1.ReadPacketAsync();
        Assert.NotNull(p1First);
        Assert.Equal((ushort)GameOpcode.MsgObjectInfo, p1First.Value.Opcode);

        await map.JoinAsync(p2);
        // p2 receives p1 + NPC 5001; p1 receives p2
        var p2PktA = await client2.ReadPacketAsync();
        var p2PktB = await client2.ReadPacketAsync();
        var p1SeesP2 = await client1.ReadPacketAsync();
        Assert.NotNull(p2PktA);
        Assert.NotNull(p2PktB);
        Assert.NotNull(p1SeesP2);
        Assert.Equal((ushort)GameOpcode.MsgObjectInfo, p1SeesP2.Value.Opcode);

        // p1 walks: p2 receives MsgWalk (10187) with UiId=1; p1 receives nothing (no self-echo)
        await map.MoveAsync(p1, 102f, -175f, 1.5f, 2);

        var p2Walk = await client2.ReadPacketAsync();
        Assert.NotNull(p2Walk);
        Assert.Equal((ushort)GameOpcode.MsgWalk, p2Walk.Value.Opcode);
        MsgWalk walk = MsgWalk.Parse(p2Walk.Value.Payload);
        Assert.Equal((ushort)1, walk.IdOrMap);
        Assert.Equal(102f, walk.X);

        using var timeoutCts = new CancellationTokenSource(100);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client1.ReadPacketAsync(timeoutCts.Token).AsTask());
    }

    [Fact]
    public async Task GameSessionHandler_FullCharacterSelectWorldEntryAndWalkFlow()
    {
        var repo = new InMemoryCharacterRepository();
        var tokenStore = new ValidationTokenStore();
        const string validateCode = "VALIDTOKEN12345678901234";
        tokenStore.Store("test", validateCode);

        var options = Options.Create(new GameServerOptions());
        var worldManager = new WorldManager(options, NullLogger<WorldManager>.Instance);
        var handler = new GameSessionHandler(repo, tokenStore, worldManager, NullLogger<GameSessionHandler>.Instance);

        await using var link = DuplexPipeStream.CreatePair();
        await using var serverConn = new PacketConnection(link.ServerStream, encrypted: false);
        await using var clientConn = new PacketConnection(link.ClientStream, encrypted: false);

        using var testCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        CancellationToken ct = testCts.Token;

        Task sessionTask = handler.HandleConnectionAsync(serverConn, "127.0.0.1", ct);

        // 1. Login to GameServer (10000) -> MSG_ROLE_INFO (10002)
        await clientConn.SendPacketAsync(
            (ushort)GameOpcode.MsgLoginGameServer,
            new MsgLoginGameServer("test", validateCode).ToPayload(), ct);

        var rolePkt = await clientConn.ReadPacketAsync(ct);
        Assert.NotNull(rolePkt);
        Assert.Equal((ushort)GameOpcode.MsgRoleInfo, rolePkt.Value.Opcode);
        Assert.Equal(352, rolePkt.Value.Payload.Length);

        // 2. Create Role (10003) -> MSG_MANAGE_RETURN (10158) + refreshed MSG_ROLE_INFO (10002)
        await clientConn.SendPacketAsync(
            (ushort)GameOpcode.MsgCreateRole,
            new MsgCreateRole("AthensHero", Gender: 1, Camp: 0, Profession: 0, Belief: 0, HairStyle: 5, FaceShap: 1).ToPayload(), ct);

        var createRet = await clientConn.ReadPacketAsync(ct);
        Assert.NotNull(createRet);
        Assert.Equal((ushort)GameOpcode.MsgManageReturn, createRet.Value.Opcode);
        Assert.Equal(ManageReturnCode.Success, MsgManageReturn.Parse(createRet.Value.Payload).Disc);

        var refreshedRoles = await clientConn.ReadPacketAsync(ct);
        Assert.NotNull(refreshedRoles);
        Assert.Equal("AthensHero", MsgRoleInfo.Parse(refreshedRoles.Value.Payload).Roles[0].Name);

        // 3. Enter Game (10006) -> 10019 (1192B), 10005 (4B), 10031 (888B)
        await clientConn.SendPacketAsync(
            (ushort)GameOpcode.MsgEnterGame,
            new MsgEnterGame("AthensHero", validateCode).ToPayload(), ct);

        var selfInfo = await clientConn.ReadPacketAsync(ct);
        var serverReady = await clientConn.ReadPacketAsync(ct);
        var kitbag = await clientConn.ReadPacketAsync(ct);
        Assert.NotNull(selfInfo);
        Assert.NotNull(serverReady);
        Assert.NotNull(kitbag);
        Assert.Equal((ushort)GameOpcode.MsgSelfInfo, selfInfo.Value.Opcode);
        Assert.Equal(1188, selfInfo.Value.Payload.Length);
        Assert.Equal((ushort)GameOpcode.MsgClientServerReady, serverReady.Value.Opcode);
        Assert.Empty(serverReady.Value.Payload);
        Assert.Equal((ushort)GameOpcode.MsgKitbag, kitbag.Value.Opcode);
        Assert.Equal(884, kitbag.Value.Payload.Length);

        // 4. Client Ready (10007) -> 10041, 10160, 10020 (NPCs/Monsters), 10040, 10039, 10093, 10156
        await clientConn.SendPacketAsync(
            (ushort)GameOpcode.MsgClientReady,
            new MsgClientReady(1, validateCode).ToPayload(), ct);

        var receivedOpcodes = new List<ushort>();
        while (true)
        {
            var pkt = await clientConn.ReadPacketAsync(ct);
            Assert.NotNull(pkt);
            receivedOpcodes.Add(pkt.Value.Opcode);
            if (pkt.Value.Opcode == (ushort)GameOpcode.MsgAltarInfo)
            {
                break;
            }
        }

        Assert.Contains((ushort)GameOpcode.MsgSelfProperty, receivedOpcodes);
        Assert.Contains((ushort)GameOpcode.MsgSynGameData, receivedOpcodes);
        Assert.Contains((ushort)GameOpcode.MsgObjectInfo, receivedOpcodes);
        Assert.Contains((ushort)GameOpcode.MsgPassiveSkillInfo, receivedOpcodes);
        Assert.Contains((ushort)GameOpcode.MsgActiveSkillInfo, receivedOpcodes);
        Assert.Contains((ushort)GameOpcode.MsgRelationAll, receivedOpcodes);
        Assert.Contains((ushort)GameOpcode.MsgAltarInfo, receivedOpcodes);

        // 5. Heartbeat ping (10015) and ServerTime (10303)
        await clientConn.SendPacketAsync((ushort)GameOpcode.MsgHeartbeatPing, new byte[] { 1, 2, 3, 4 }, ct);
        var pong = await clientConn.ReadPacketAsync(ct);
        Assert.NotNull(pong);
        Assert.Equal((ushort)GameOpcode.MsgHeartbeatPing, pong.Value.Opcode);

        await clientConn.SendPacketAsync((ushort)GameOpcode.MsgServerTime, new byte[12], ct);
        var timeReply = await clientConn.ReadPacketAsync(ct);
        Assert.NotNull(timeReply);
        Assert.Equal((ushort)GameOpcode.MsgServerTime, timeReply.Value.Opcode);
        Assert.Equal(12, timeReply.Value.Payload.Length);

        // 6. Clean exit (10185)
        await clientConn.SendPacketAsync((ushort)GameOpcode.MsgDelayExit, new byte[4], ct);
        await sessionTask.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(1, repo.PositionSaveCount);
    }

    private static PlayerSession CreatePlayerSession(
        uint uiId,
        string account,
        string name,
        float x,
        float z,
        PacketConnection connection)
    {
        var ch = new CharacterBase
        {
            Id = (int)uiId + 100,
            Accounts = account,
            Name = name,
            Gender = "male",
            Camp = 0,
            Profession = 0,
            FighterJobLv = 1,
            ScholarJobLv = 1,
            CurHp = 200,
            MaxHp = 200,
            CurMp = 60,
            MaxMp = 60,
            Map = 1,
            PosX = x,
            PosZ = z,
            BagNum = 1,
            StoreNum = 10,
        };
        return new PlayerSession(
            uiId,
            account,
            ch,
            connection,
            Enumerable.Repeat(ItemEquipSlot.Empty, 12).ToArray(),
            [
                Enumerable.Repeat(ItemEquipSlot.Empty, 24).ToArray(),
                Enumerable.Repeat(ItemEquipSlot.Empty, 24).ToArray(),
                Enumerable.Repeat(ItemEquipSlot.Empty, 24).ToArray(),
                Enumerable.Repeat(ItemEquipSlot.Empty, 24).ToArray(),
            ],
            [],
            []);
    }

    private sealed class InMemoryCharacterRepository : ICharacterRepository
    {
        private readonly List<CharacterBase> _chars = [];
        private int _nextId = 1000;
        public int PositionSaveCount { get; private set; }

        public Task<IReadOnlyList<CharacterBase>> GetCharactersByAccountAsync(string account, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CharacterBase>>(_chars.Where(c => c.Accounts == account).ToList());

        public Task<CharacterBase?> GetCharacterByIdAsync(int id, CancellationToken ct = default)
            => Task.FromResult(_chars.FirstOrDefault(c => c.Id == id));

        public Task<CharacterBase?> GetCharacterByNameAsync(string name, CancellationToken ct = default)
            => Task.FromResult(_chars.FirstOrDefault(c => c.Name == name));

        public Task<CharacterKitbag?> GetKitbagAsync(int userId, CancellationToken ct = default)
            => Task.FromResult<CharacterKitbag?>(new CharacterKitbag { UserId = userId });

        public Task<string?> GetUserMagicAsync(int userId, CancellationToken ct = default)
            => Task.FromResult<string?>("[250,0]#");

        public Task<string?> GetUserSkillAsync(int userId, CancellationToken ct = default)
            => Task.FromResult<string?>("[50,1]#");

        public Task<CreateCharacterResult> CreateCharacterAsync(CreateCharacterParams request, CancellationToken ct = default)
        {
            if (_chars.Any(c => c.Name == request.Name))
            {
                return Task.FromResult(new CreateCharacterResult(CreateCharacterStatus.DuplicateName, null));
            }

            var ch = new CharacterBase
            {
                Id = _nextId++,
                Accounts = request.Account,
                Name = request.Name,
                Gender = request.Gender,
                Camp = request.Camp,
                Profession = request.Profession,
                Belief = request.Belief,
                HairStyle = request.HairStyle,
                FaceShap = request.FaceShap,
                Map = request.Map,
                PosX = request.PosX,
                PosZ = request.PosZ,
                MaxHp = request.MaxHp,
                CurHp = request.MaxHp,
                MaxMp = request.MaxMp,
                CurMp = request.MaxMp,
                FighterJobLv = 1,
                ScholarJobLv = 1,
                BagNum = 1,
                StoreNum = 10,
            };
            _chars.Add(ch);
            return Task.FromResult(new CreateCharacterResult(CreateCharacterStatus.Success, ch));
        }

        public Task<bool> DeleteCharacterAsync(string account, string name, CancellationToken ct = default)
            => Task.FromResult(_chars.RemoveAll(c => c.Accounts == account && c.Name == name) > 0);

        public Task UpdateCharacterPositionAsync(int id, short map, float posX, float posZ, CancellationToken ct = default)
        {
            PositionSaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class DuplexPipeStream : Stream, IAsyncDisposable
    {
        private readonly PipeReader _reader;
        private readonly PipeWriter _writer;

        private DuplexPipeStream(PipeReader reader, PipeWriter writer)
        {
            _reader = reader;
            _writer = writer;
        }

        public Stream ServerStream => this;
        public Stream ClientStream => _peer!;
        private DuplexPipeStream? _peer;

        public static DuplexPipeStream CreatePair()
        {
            var aToB = new Pipe();
            var bToA = new Pipe();
            var server = new DuplexPipeStream(aToB.Reader, bToA.Writer);
            var client = new DuplexPipeStream(bToA.Reader, aToB.Writer);
            server._peer = client;
            return server;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
        public override void Write(byte[] buffer, int offset, int count) => WriteAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ReadResult result = await _reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            if (result.Buffer.IsEmpty && result.IsCompleted)
            {
                return 0;
            }

            int toCopy = (int)Math.Min(buffer.Length, result.Buffer.Length);
            System.Buffers.BuffersExtensions.CopyTo(result.Buffer.Slice(0, toCopy), buffer.Span);
            _reader.AdvanceTo(result.Buffer.GetPosition(toCopy));
            return toCopy;
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await _writer.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
            await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        public override async ValueTask DisposeAsync()
        {
            await _writer.CompleteAsync().ConfigureAwait(false);
            await _reader.CompleteAsync().ConfigureAwait(false);
            if (_peer is not null)
            {
                await _peer.DisposeAsync().ConfigureAwait(false);
            }
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}
