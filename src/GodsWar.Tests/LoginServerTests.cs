using System.Net.Sockets;
using GodsWar.Database.Models;
using GodsWar.Database.Repositories;
using GodsWar.LoginServer;
using GodsWar.Protocol;
using GodsWar.Protocol.Packets;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GodsWar.Tests;

public sealed class LoginServerTests
{
    private sealed class StubAccountRepository : IAccountRepository
    {
        public AccountAuthResult NextResult { get; set; } = new(
            AccountAuthStatus.Success,
            new GameAreaInfo { Gid = 1, GameArea = 1, AccId = 1, PassName = "test", PassPwd = "test" });

        public Task<GameAreaInfo?> GetGameAreaAccountAsync(string passName, CancellationToken cancellationToken = default)
            => Task.FromResult(NextResult.Account);

        public Task<IReadOnlyList<GameAreaInfo>> ListGameAreaAccountsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<GameAreaInfo>>(
                NextResult.Account is not null ? [NextResult.Account] : []);

        public Task<AccountAuthResult> ValidateAccountAsync(
            string passName,
            string password,
            string? clientIp = null,
            CancellationToken cancellationToken = default)
        {
            if (passName == "test" && password == "test")
            {
                return Task.FromResult(NextResult);
            }

            return Task.FromResult(new AccountAuthResult(AccountAuthStatus.InvalidCredentials, null));
        }

        public Task<bool> CreateAccountIfMissingAsync(string passName, string password, CancellationToken cancellationToken = default)
            => Task.FromResult(false);
    }

    private static async Task<(LoginServerListener Listener, CancellationTokenSource Cts, Task RunTask)> StartTestServerAsync(
        IAccountRepository repo,
        LoginServerOptions? options = null)
    {
        options ??= new LoginServerOptions
        {
            BindAddress = "127.0.0.1",
            Port = 0,
            AdvertiseIp = "127.0.0.1",
            GamePort = 6001,
            ServerName = "GodsWar",
            ServerId = 1,
        };

        IOptions<LoginServerOptions> wrapped = Options.Create(options);
        var registry = new GameServerRegistry(wrapped, NullLogger<GameServerRegistry>.Instance);
        var handler = new LoginSessionHandler(repo, registry, wrapped, NullLogger<LoginSessionHandler>.Instance);
        var listener = new LoginServerListener(wrapped, handler, NullLogger<LoginServerListener>.Instance);

        var cts = new CancellationTokenSource();
        Task runTask = listener.StartAsync(cts.Token);

        // Wait briefly for TcpListener to bind an ephemeral port.
        for (int i = 0; i < 50 && listener.BoundPort == 0; i++)
        {
            await Task.Delay(10);
        }

        return (listener, cts, runTask);
    }

    [Fact]
    public async Task FullLoginHandshake_WithRegisteredGameServer_DispatchesValidateCode()
    {
        var repo = new StubAccountRepository();
        (LoginServerListener listener, CancellationTokenSource cts, Task runTask) = await StartTestServerAsync(repo);

        try
        {
            // 1. Connect a simulated GameServer and register it.
            using var gsTcp = new TcpClient();
            await gsTcp.ConnectAsync("127.0.0.1", listener.BoundPort);
            await using NetworkStream gsStream = gsTcp.GetStream();
            await using var gsConn = new PacketConnection(gsStream, encrypted: true);

            var gsInfo = new MsgGameServerInfo("GodsWar", "127.0.0.1", 6001, 1, GameServerState.Normal);
            await gsConn.SendPacketAsync((ushort)LoginOpcode.MsgGameServerInfo, gsInfo.ToServerRegistrationPayload());

            // Give the server a moment to process registration.
            await Task.Delay(50);

            // 2. Connect a 1.0.282 client and log in.
            using var cliTcp = new TcpClient();
            await cliTcp.ConnectAsync("127.0.0.1", listener.BoundPort);
            await using NetworkStream cliStream = cliTcp.GetStream();
            await using var cliConn = new PacketConnection(cliStream, encrypted: true);

            byte[] loginPayload = new MsgLogin("test", "test", 1000270).ToPayload(usClientFormat: true);
            await cliConn.SendPacketAsync((ushort)LoginOpcode.MsgLogin, loginPayload);

            (ushort Opcode, byte[] Payload)? reply1 = await cliConn.ReadPacketAsync();
            Assert.NotNull(reply1);
            Assert.Equal((ushort)LoginOpcode.MsgLoginReturnInfo, reply1.Value.Opcode);
            Assert.Equal(LoginResultCode.Success, MsgLoginReturnInfo.Parse(reply1.Value.Payload).Info);

            (ushort Opcode, byte[] Payload)? reply2 = await cliConn.ReadPacketAsync();
            Assert.NotNull(reply2);
            Assert.Equal((ushort)LoginOpcode.MsgGameServerInfo, reply2.Value.Opcode);
            MsgGameServerInfo pushedServer = MsgGameServerInfo.ParseClientPayload(reply2.Value.Payload);
            Assert.Equal("GodsWar", pushedServer.ServerName);
            Assert.Equal("127.0.0.1", pushedServer.Ip);
            Assert.Equal(6001u, pushedServer.Port);

            // 3. Client requests GameServer (opcode 4 on 1.0.282).
            byte[] reqPayload = new MsgRequestGameServer("test", 1).ToPayload();
            await cliConn.SendPacketAsync((ushort)LoginOpcode.MsgRequestGameServer, reqPayload);

            // 4. GameServer receives MSG_VALIDATE_GAMESERVER (300) and client receives MSG_RESPONSE_GAMESERVER (10001).
            (ushort Opcode, byte[] Payload)? gsPacket = await gsConn.ReadPacketAsync();
            Assert.NotNull(gsPacket);
            Assert.Equal((ushort)LoginOpcode.MsgValidateGameServer, gsPacket.Value.Opcode);
            MsgValidateGameServer validate = MsgValidateGameServer.Parse(gsPacket.Value.Payload);
            Assert.Equal("test", validate.Account);
            Assert.Equal(24, validate.Code.Length);

            (ushort Opcode, byte[] Payload)? cliResponse = await cliConn.ReadPacketAsync();
            Assert.NotNull(cliResponse);
            Assert.Equal((ushort)LoginOpcode.MsgResponseGameServer, cliResponse.Value.Opcode);
            MsgResponseGameServer gsResp = MsgResponseGameServer.Parse(cliResponse.Value.Payload);
            Assert.Equal((byte)0, gsResp.LoginError);
            Assert.Equal("127.0.0.1", gsResp.Ip);
            Assert.Equal(6001u, gsResp.Port);
            Assert.Equal(validate.Code, gsResp.Code);
        }
        finally
        {
            await cts.CancelAsync();
            await listener.StopAsync(CancellationToken.None);
            await runTask;
        }
    }

    [Fact]
    public async Task Login_RejectsBadPassword_VersionMismatch_AndBannedAccount()
    {
        var repo = new StubAccountRepository();
        var options = new LoginServerOptions
        {
            BindAddress = "127.0.0.1",
            Port = 0,
            ExpectedVersion = 1000270,
        };
        (LoginServerListener listener, CancellationTokenSource cts, Task runTask) = await StartTestServerAsync(repo, options);

        try
        {
            using var cliTcp = new TcpClient();
            await cliTcp.ConnectAsync("127.0.0.1", listener.BoundPort);
            await using NetworkStream cliStream = cliTcp.GetStream();
            await using var cliConn = new PacketConnection(cliStream, encrypted: true);

            // Version mismatch -> 0xFF
            await cliConn.SendPacketAsync(
                (ushort)LoginOpcode.MsgLogin,
                new MsgLogin("test", "test", 1000999).ToPayload());
            (ushort Opcode, byte[] Payload)? mismatch = await cliConn.ReadPacketAsync();
            Assert.NotNull(mismatch);
            Assert.Equal(LoginResultCode.VersionMismatch, MsgLoginReturnInfo.Parse(mismatch.Value.Payload).Info);

            // Bad password -> 0x00
            await cliConn.SendPacketAsync(
                (ushort)LoginOpcode.MsgLogin,
                new MsgLogin("test", "wrong", 1000270).ToPayload());
            (ushort Opcode, byte[] Payload)? badPwd = await cliConn.ReadPacketAsync();
            Assert.NotNull(badPwd);
            Assert.Equal(LoginResultCode.InvalidCredentials, MsgLoginReturnInfo.Parse(badPwd.Value.Payload).Info);

            // Account banned -> 0x02
            repo.NextResult = new AccountAuthResult(AccountAuthStatus.AccountBanned, null);
            await cliConn.SendPacketAsync(
                (ushort)LoginOpcode.MsgLogin,
                new MsgLogin("test", "test", 1000270).ToPayload());
            (ushort Opcode, byte[] Payload)? banned = await cliConn.ReadPacketAsync();
            Assert.NotNull(banned);
            Assert.Equal(LoginResultCode.AccountBanned, MsgLoginReturnInfo.Parse(banned.Value.Payload).Info);
        }
        finally
        {
            await cts.CancelAsync();
            await listener.StopAsync(CancellationToken.None);
            await runTask;
        }
    }
}
