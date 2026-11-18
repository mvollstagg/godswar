using System.Security.Cryptography;
using GodsWar.Database.Models;
using GodsWar.Database.Repositories;
using GodsWar.Protocol;
using GodsWar.Protocol.Packets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GodsWar.LoginServer;

public sealed class LoginSessionHandler
{
    private const string ValidateCodeAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    private readonly IAccountRepository _accountRepository;
    private readonly GameServerRegistry _gameServerRegistry;
    private readonly LoginServerOptions _options;
    private readonly ILogger<LoginSessionHandler> _logger;

    public LoginSessionHandler(
        IAccountRepository accountRepository,
        GameServerRegistry gameServerRegistry,
        IOptions<LoginServerOptions> options,
        ILogger<LoginSessionHandler> logger)
    {
        _accountRepository = accountRepository;
        _gameServerRegistry = gameServerRegistry;
        _options = options.Value;
        _logger = logger;
    }

    public async Task HandleConnectionAsync(
        PacketConnection connection,
        string? remoteIp,
        CancellationToken cancellationToken,
        bool caesarDecodeLogin = false)
    {
        string? authenticatedAccount = null;
        int clientVersion = 0;
        bool gameServerRequested = false;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                (ushort Opcode, byte[] Payload)? packet = await connection.ReadPacketAsync(cancellationToken).ConfigureAwait(false);
                if (packet is null)
                {
                    break;
                }

                (ushort opcode, byte[] payload) = packet.Value;
                switch (opcode)
                {
                    case (ushort)LoginOpcode.MsgLogin:
                        (authenticatedAccount, clientVersion) = await HandleLoginAsync(
                            connection,
                            payload,
                            remoteIp,
                            cancellationToken,
                            caesarDecodeLogin).ConfigureAwait(false);
                        gameServerRequested = false;
                        break;

                    case (ushort)LoginOpcode.MsgGameServerInfo:
                        MsgGameServerInfo gsInfo = MsgGameServerInfo.ParseServerRegistration(payload);
                        _gameServerRegistry.RegisterOrUpdate(gsInfo, connection);
                        break;

                    case (ushort)LoginOpcode.MsgRequestGameServerLegacy:
                    case (ushort)LoginOpcode.MsgRequestGameServer:
                        if (string.IsNullOrEmpty(authenticatedAccount) || gameServerRequested)
                        {
                            _logger.LogWarning(
                                "Ignoring MSG_REQUEST_GAMESERVER ({Opcode}) from {RemoteIp}: authenticated={Authenticated}, alreadyRequested={AlreadyRequested}",
                                opcode,
                                remoteIp,
                                authenticatedAccount is not null,
                                gameServerRequested);
                            break;
                        }

                        gameServerRequested = await HandleRequestGameServerAsync(
                            connection,
                            payload,
                            authenticatedAccount,
                            cancellationToken).ConfigureAwait(false);
                        break;

                    default:
                        _logger.LogDebug("Unhandled LoginServer opcode {Opcode} ({Length}B payload) from {RemoteIp}", opcode, payload.Length, remoteIp);
                        break;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Graceful shutdown
        }
        catch (EndOfStreamException ex)
        {
            _logger.LogDebug("Client {RemoteIp} disconnected mid-packet: {Message}", remoteIp, ex.Message);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException)
        {
            _logger.LogDebug("Connection {RemoteIp} closed: {Message}", remoteIp, ex.Message);
        }
        finally
        {
            _gameServerRegistry.UnregisterConnection(connection);
        }
    }

    private async Task<(string? AuthenticatedAccount, int ClientVersion)> HandleLoginAsync(
        PacketConnection connection,
        byte[] payload,
        string? remoteIp,
        CancellationToken cancellationToken,
        bool caesarDecodeLogin = false)
    {
        MsgLogin msg = MsgLogin.Parse(payload);
        if (caesarDecodeLogin)
        {
            msg = new MsgLogin(
                DecodeCaesarPlus2(msg.Account),
                msg.Password,
                msg.Version);
        }
        ushort replyOpcode = MsgLoginReturnInfo.GetOpcode(msg.Version);

        if (_options.ExpectedVersion > 0 && msg.Version != _options.ExpectedVersion)
        {
            _logger.LogWarning(
                "Login rejected (version mismatch) for Account={Account} RemoteIp={RemoteIp} ClientVersion={Version} Expected={Expected}",
                msg.Account,
                remoteIp,
                msg.Version,
                _options.ExpectedVersion);

            await connection.SendPacketAsync(
                replyOpcode,
                new MsgLoginReturnInfo(LoginResultCode.VersionMismatch).ToPayload(),
                cancellationToken).ConfigureAwait(false);
            return (null, msg.Version);
        }

        if (!IsValidCredentialsShape(msg.Account, msg.Password))
        {
            _logger.LogWarning("Login rejected (invalid characters) for Account={Account} RemoteIp={RemoteIp}", msg.Account, remoteIp);
            await connection.SendPacketAsync(
                replyOpcode,
                new MsgLoginReturnInfo(LoginResultCode.InvalidCredentials).ToPayload(),
                cancellationToken).ConfigureAwait(false);
            return (null, msg.Version);
        }

        AccountAuthResult auth = await _accountRepository.ValidateAccountAsync(
            msg.Account,
            msg.Password,
            remoteIp,
            cancellationToken).ConfigureAwait(false);

        if (auth.Status == AccountAuthStatus.InvalidCredentials && _options.AutoCreateAccount)
        {
            bool created = await _accountRepository.CreateAccountIfMissingAsync(
                msg.Account,
                msg.Password,
                cancellationToken).ConfigureAwait(false);
            if (created)
            {
                _logger.LogInformation("Auto-registered new Account={Account} from {RemoteIp}", msg.Account, remoteIp);
                auth = await _accountRepository.ValidateAccountAsync(
                    msg.Account,
                    msg.Password,
                    remoteIp,
                    cancellationToken).ConfigureAwait(false);
            }
        }

        LoginResultCode resultCode = MapAuthStatus(auth.Status);
        await connection.SendPacketAsync(
            replyOpcode,
            new MsgLoginReturnInfo(resultCode).ToPayload(),
            cancellationToken).ConfigureAwait(false);

        if (resultCode != LoginResultCode.Success)
        {
            _logger.LogInformation(
                "Login denied for Account={Account} RemoteIp={RemoteIp} Version={Version} Status={Status} (0x{Code:X2})",
                msg.Account,
                remoteIp,
                msg.Version,
                auth.Status,
                (byte)resultCode);
            return (null, msg.Version);
        }

        IReadOnlyList<MsgGameServerInfo> servers = _gameServerRegistry.GetServerListForClient();
        foreach (MsgGameServerInfo server in servers)
        {
            await connection.SendPacketAsync(
                (ushort)LoginOpcode.MsgGameServerInfo,
                server.ToClientPayload(),
                cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation(
            "Login succeeded for Account={Account} RemoteIp={RemoteIp} Version={Version}; pushed {ServerCount} server(s)",
            msg.Account,
            remoteIp,
            msg.Version,
            servers.Count);

        return (msg.Account, msg.Version);
    }

    private async Task<bool> HandleRequestGameServerAsync(
        PacketConnection connection,
        byte[] payload,
        string authenticatedAccount,
        CancellationToken cancellationToken)
    {
        MsgRequestGameServer request = MsgRequestGameServer.Parse(payload);
        _logger.LogDebug("MSG_REQUEST_GAMESERVER ({Len}B): {Hex}", payload.Length, Convert.ToHexString(payload));
        if (!string.Equals(request.Account, authenticatedAccount, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Account mismatch in MSG_REQUEST_GAMESERVER: requested={Requested} authenticated={Authenticated}",
                request.Account,
                authenticatedAccount);
            return false;
        }

        string validateCode = GenerateValidateCode();
        GameServerLookupResult lookup = await _gameServerRegistry.DispatchValidationAsync(
            request.ServerId,
            authenticatedAccount,
            validateCode,
            cancellationToken).ConfigureAwait(false);

        if (!lookup.Found || lookup.Unavailable)
        {
            var errResponse = new MsgResponseGameServer(0, string.Empty, 0, LoginError: 1, string.Empty);
            await connection.SendPacketAsync(
                (ushort)LoginOpcode.MsgResponseGameServer,
                errResponse.ToPayload(),
                cancellationToken).ConfigureAwait(false);

            _logger.LogWarning(
                "GameServer request denied for Account={Account} ServerId={ServerId} (Found={Found}, Unavailable={Unavailable})",
                authenticatedAccount,
                request.ServerId,
                lookup.Found,
                lookup.Unavailable);
            return false;
        }

        var response = new MsgResponseGameServer(
            lookup.ServerInfo.ServerId,
            lookup.ServerInfo.Ip,
            lookup.ServerInfo.Port,
            LoginError: 0,
            validateCode);

        await connection.SendPacketAsync(
            (ushort)LoginOpcode.MsgResponseGameServer,
            response.ToPayload(),
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Handed off Account={Account} to GameServer Id={ServerId} ({Ip}:{Port}) ValidateSentToGS={SentToGS}",
            authenticatedAccount,
            lookup.ServerInfo.ServerId,
            lookup.ServerInfo.Ip,
            lookup.ServerInfo.Port,
            lookup.SentValidateToGameServer);
        return true;
    }

    public static bool IsValidCredentialsShape(string account, string password)
    {
        if (string.IsNullOrEmpty(account) || account.Length >= ProtocolConstants.MaxNameLength)
        {
            return false;
        }

        if (password.Length > ProtocolConstants.MaxNameLength)
        {
            return false;
        }

        foreach (char c in account)
        {
            if (!(char.IsAsciiLetterOrDigit(c) || c == '_'))
            {
                return false;
            }
        }

        return true;
    }

    public static LoginResultCode MapAuthStatus(AccountAuthStatus status) => status switch
    {
        AccountAuthStatus.Success => LoginResultCode.Success,
        AccountAuthStatus.AccountBanned => LoginResultCode.AccountBanned,
        AccountAuthStatus.IpBanned => LoginResultCode.IpBanned,
        _ => LoginResultCode.InvalidCredentials,
    };

    public static string GenerateValidateCode() =>
        RandomNumberGenerator.GetString(ValidateCodeAlphabet, ProtocolConstants.ValidateCodeLength - 1);

    public static string DecodeCaesarPlus2(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return string.Create(value.Length, value, static (span, src) =>
        {
            for (int i = 0; i < src.Length; i++)
            {
                span[i] = (char)(src[i] + 2);
            }
        });
    }
}
