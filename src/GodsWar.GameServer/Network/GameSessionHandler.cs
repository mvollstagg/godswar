using GodsWar.Database.Models;
using GodsWar.Database.Repositories;
using GodsWar.GameServer.Auth;
using GodsWar.GameServer.World;
using GodsWar.Protocol;
using GodsWar.Protocol.Packets;
using Microsoft.Extensions.Logging;

namespace GodsWar.GameServer.Network;

public sealed class GameSessionHandler
{
    private readonly ICharacterRepository _characterRepository;
    private readonly ValidationTokenStore _tokenStore;
    private readonly WorldManager _worldManager;
    private readonly ILogger<GameSessionHandler> _logger;

    public GameSessionHandler(
        ICharacterRepository characterRepository,
        ValidationTokenStore tokenStore,
        WorldManager worldManager,
        ILogger<GameSessionHandler> logger)
    {
        _characterRepository = characterRepository;
        _tokenStore = tokenStore;
        _worldManager = worldManager;
        _logger = logger;
    }

    public async Task HandleConnectionAsync(
        PacketConnection connection,
        string? remoteIp,
        CancellationToken cancellationToken)
    {
        string? account = null;
        PlayerSession? player = null;

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
                switch ((GameOpcode)opcode)
                {
                    case GameOpcode.MsgLoginGameServer:
                        account = await HandleLoginAsync(connection, payload, remoteIp, cancellationToken).ConfigureAwait(false);
                        if (account is null)
                        {
                            return;
                        }
                        break;

                    case GameOpcode.MsgCreateRole:
                        if (account is not null)
                        {
                            await HandleCreateRoleAsync(connection, account, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgDeleteRole:
                        if (account is not null)
                        {
                            await HandleDeleteRoleAsync(connection, account, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgEnterGame:
                        if (account is not null)
                        {
                            player = await HandleEnterGameAsync(connection, account, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgClientReady:
                        if (player is not null)
                        {
                            await HandleClientReadyAsync(player, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgWalk:
                        if (player is not null && player.InWorld)
                        {
                            MsgWalk walk = MsgWalk.Parse(payload);
                            WorldMap? map = _worldManager.GetMap(player.MapId);
                            if (map is not null)
                            {
                                await map.MoveAsync(player, walk.X, walk.Z, walk.Atan2, walk.State, cancellationToken).ConfigureAwait(false);
                            }
                        }
                        break;

                    case GameOpcode.MsgHeartbeatPing:
                        await connection.SendPacketAsync((ushort)GameOpcode.MsgHeartbeatPing, payload, cancellationToken).ConfigureAwait(false);
                        break;

                    case GameOpcode.MsgServerTime:
                        uint now = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        await connection.SendPacketAsync(
                            (ushort)GameOpcode.MsgServerTime,
                            new MsgServerTime(now).ToPayload(),
                            cancellationToken).ConfigureAwait(false);
                        break;

                    case GameOpcode.MsgDelayExit:
                        _logger.LogInformation("Player {Name} ({Account}) requested clean exit (10185)", player?.Character.Name ?? "<none>", account ?? "<none>");
                        return;

                    case GameOpcode.MsgKeepalivePing:
                    case GameOpcode.MsgQuickbarInit:
                    case GameOpcode.MsgBagSnapshotSync:
                    case GameOpcode.MsgMentorListQuery:
                    case GameOpcode.MsgSetQuickEquip:
                        break;

                    default:
                        _logger.LogDebug("Unhandled GameServer opcode {Opcode} ({Length}B) from Account={Account}", opcode, payload.Length, account ?? "<unauthenticated>");
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
            _logger.LogDebug("Client {RemoteIp} closed: {Message}", remoteIp, ex.Message);
        }
        finally
        {
            if (player is not null)
            {
                await CleanupPlayerAsync(player).ConfigureAwait(false);
            }
        }
    }

    private async Task<string?> HandleLoginAsync(
        PacketConnection connection,
        byte[] payload,
        string? remoteIp,
        CancellationToken cancellationToken)
    {
        MsgLoginGameServer msg = MsgLoginGameServer.Parse(payload);
        if (!_tokenStore.Validate(msg.Account, msg.ValidateCode))
        {
            _logger.LogWarning(
                "GameServer login rejected (invalid/expired token) for Account={Account} RemoteIp={RemoteIp}",
                msg.Account,
                remoteIp);
            return null;
        }

        _logger.LogInformation("Account={Account} authenticated to GameServer from {RemoteIp}", msg.Account, remoteIp);
        await SendRoleListAsync(connection, msg.Account, cancellationToken).ConfigureAwait(false);
        return msg.Account;
    }

    private async Task SendRoleListAsync(
        PacketConnection connection,
        string account,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<CharacterBase> characters = await _characterRepository.GetCharactersByAccountAsync(account, cancellationToken).ConfigureAwait(false);
        var summaries = new List<RoleSummary>(characters.Count);

        foreach (CharacterBase ch in characters.Take(MsgRoleInfo.MaxRoles))
        {
            CharacterKitbag? kitbag = await _characterRepository.GetKitbagAsync(ch.Id, cancellationToken).ConfigureAwait(false);
            ItemEquipSlot[] equips = ItemEquipSlot.ParseDbKitbagString(kitbag?.Equip, MsgRoleInfo.EquipCount);
            int[] equipIds = equips.Select(e => e.Base > 0 ? e.Base : -1).ToArray();

            byte gender = string.Equals(ch.Gender, "male", StringComparison.OrdinalIgnoreCase) ? (byte)1 : (byte)0;
            byte level = (byte)Math.Clamp(Math.Max((int)ch.FighterJobLv, (int)ch.ScholarJobLv), 1, 255);

            summaries.Add(new RoleSummary(
                ch.Name,
                (byte)ch.Camp,
                (byte)ch.Profession,
                level,
                gender,
                (byte)ch.HairStyle,
                (byte)(ch.FaceShap ?? 0),
                equipIds));
        }

        var roleInfo = new MsgRoleInfo(summaries);
        await connection.SendPacketAsync((ushort)GameOpcode.MsgRoleInfo, roleInfo.ToPayload(), cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleCreateRoleAsync(
        PacketConnection connection,
        string account,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        MsgCreateRole msg = MsgCreateRole.Parse(payload);
        if (string.IsNullOrWhiteSpace(msg.Name) || msg.Name.Length > 31)
        {
            await connection.SendPacketAsync(
                (ushort)GameOpcode.MsgManageReturn,
                new MsgManageReturn((ushort)GameOpcode.MsgCreateRole, ManageReturnCode.NameTooLong).ToPayload(),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        if (msg.Camp > 1 || msg.Gender > 1 || msg.Profession > 3 || msg.Belief > 1 || msg.HairStyle > 74 || msg.FaceShap > 4)
        {
            await connection.SendPacketAsync(
                (ushort)GameOpcode.MsgManageReturn,
                new MsgManageReturn((ushort)GameOpcode.MsgCreateRole, ManageReturnCode.IllegalCharacters).ToPayload(),
                cancellationToken).ConfigureAwait(false);
            return;
        }

        (ushort startMap, float startX, float startZ) = _worldManager.GetStartingPosition(msg.Camp);
        (int maxHp, int maxMp) = msg.Profession switch
        {
            0 => (200, 60),  // Warrior
            1 => (180, 80),  // Champion
            2 => (150, 120), // Priest
            _ => (140, 150), // Mage
        };

        var createParams = new CreateCharacterParams(
            account,
            msg.Name.Trim(),
            msg.Gender == 1 ? "male" : "female",
            msg.Camp,
            msg.Profession,
            msg.Belief,
            msg.HairStyle,
            msg.FaceShap,
            (short)startMap,
            startX,
            startZ,
            maxHp,
            maxMp);

        CreateCharacterResult result = await _characterRepository.CreateCharacterAsync(createParams, cancellationToken).ConfigureAwait(false);
        var returnCode = (ManageReturnCode)(int)result.Status;

        await connection.SendPacketAsync(
            (ushort)GameOpcode.MsgManageReturn,
            new MsgManageReturn((ushort)GameOpcode.MsgCreateRole, returnCode).ToPayload(),
            cancellationToken).ConfigureAwait(false);

        if (result.Status == CreateCharacterStatus.Success)
        {
            _logger.LogInformation("Created character {Name} (Camp={Camp}, Map={Map}) for Account={Account}", msg.Name, msg.Camp, startMap, account);
            await SendRoleListAsync(connection, account, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task HandleDeleteRoleAsync(
        PacketConnection connection,
        string account,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        MsgDeleteRole msg = MsgDeleteRole.Parse(payload);
        bool deleted = await _characterRepository.DeleteCharacterAsync(account, msg.Name, cancellationToken).ConfigureAwait(false);

        await connection.SendPacketAsync(
            (ushort)GameOpcode.MsgManageReturn,
            new MsgManageReturn(
                (ushort)GameOpcode.MsgDeleteRole,
                deleted ? ManageReturnCode.Success : ManageReturnCode.IllegalCharacters).ToPayload(),
            cancellationToken).ConfigureAwait(false);

        if (deleted)
        {
            _logger.LogInformation("Deleted character {Name} for Account={Account}", msg.Name, account);
            await SendRoleListAsync(connection, account, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<PlayerSession?> HandleEnterGameAsync(
        PacketConnection connection,
        string account,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        MsgEnterGame msg = MsgEnterGame.Parse(payload);
        CharacterBase? ch = await _characterRepository.GetCharacterByNameAsync(msg.Name, cancellationToken).ConfigureAwait(false);
        if (ch is null || !string.Equals(ch.Accounts, account, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Account={Account} attempted to enter with invalid character {Name}", account, msg.Name);
            return null;
        }

        CharacterKitbag? kitbag = await _characterRepository.GetKitbagAsync(ch.Id, cancellationToken).ConfigureAwait(false);
        ItemEquipSlot[] equips = ItemEquipSlot.ParseDbKitbagString(kitbag?.Equip, 12);
        ItemEquipSlot[][] kitbags =
        [
            ItemEquipSlot.ParseDbKitbagString(kitbag?.Kitbag1, 24),
            ItemEquipSlot.ParseDbKitbagString(kitbag?.Kitbag2, 24),
            ItemEquipSlot.ParseDbKitbagString(kitbag?.Kitbag3, 24),
            ItemEquipSlot.ParseDbKitbagString(kitbag?.Kitbag4, 24),
        ];

        string? magicStr = await _characterRepository.GetUserMagicAsync(ch.Id, cancellationToken).ConfigureAwait(false);
        string? skillStr = await _characterRepository.GetUserSkillAsync(ch.Id, cancellationToken).ConfigureAwait(false);

        List<ActiveSkillEntry> activeSkills = ParseActiveSkills(magicStr);
        List<PassiveSkillEntry> passiveSkills = ParsePassiveSkills(skillStr);

        uint uiId = _worldManager.AllocatePlayerUiId();
        var session = new PlayerSession(uiId, account, ch, connection, equips, kitbags, activeSkills, passiveSkills);
        _worldManager.RegisterPlayer(session);

        // 1.0.282 world-entry sequence: MSG_SELFINFO (10019) -> MSG_CLIENTSERVERREADY (10005) -> MSG_KITBAG (10031)
        await session.SendAsync(GameOpcode.MsgSelfInfo, session.ToSelfInfo().ToPayload(), cancellationToken).ConfigureAwait(false);
        await session.SendAsync(GameOpcode.MsgClientServerReady, ReadOnlyMemory<byte>.Empty, cancellationToken).ConfigureAwait(false);

        int bagNum = Math.Clamp(ch.BagNum, 1, 4);
        for (int b = 0; b < bagNum; b++)
        {
            uint flag24 = 0;
            for (int s = 0; s < 24; s++)
            {
                if (!kitbags[b][s].IsEmpty)
                {
                    flag24 |= 1u << s;
                }
            }

            if (b == 0 || (flag24 & 0x0FFF) != 0)
            {
                var page0 = new MsgKitbagPage(uiId, (uint)bagNum, (byte)b, 0, flag24 & 0x0FFF, kitbags[b][..12]);
                await session.SendAsync(GameOpcode.MsgKitbag, page0.ToPayload(), cancellationToken).ConfigureAwait(false);
            }

            if ((flag24 >> 12) != 0)
            {
                var page1 = new MsgKitbagPage(uiId, (uint)bagNum, (byte)b, 12, (flag24 >> 12) & 0x0FFF, kitbags[b][12..24]);
                await session.SendAsync(GameOpcode.MsgKitbag, page1.ToPayload(), cancellationToken).ConfigureAwait(false);
            }
        }

        _logger.LogInformation(
            "Player {Name} (Id={DbId}, UiId={UiId}) entering Map={Map} at ({X:F1}, {Z:F1})",
            ch.Name,
            ch.Id,
            uiId,
            session.MapId,
            session.X,
            session.Z);

        return session;
    }

    private async Task HandleClientReadyAsync(PlayerSession player, CancellationToken cancellationToken)
    {
        ClientObjectData objData = player.ToClientObjectData();
        await player.SendAsync(GameOpcode.MsgSelfProperty, new MsgSelfProperty(objData).ToPayload(), cancellationToken).ConfigureAwait(false);
        await player.SendAsync(
            GameOpcode.MsgSynGameData,
            new MsgSynGameData(player.UiId, objData, (uint)Math.Max(0, player.Character.SkillPoint)).ToPayload(),
            cancellationToken).ConfigureAwait(false);

        WorldMap? map = _worldManager.GetMap(player.MapId) ?? _worldManager.GetMap(1);
        if (map is not null)
        {
            player.MapId = map.Scene.MapId;
            await map.JoinAsync(player, cancellationToken).ConfigureAwait(false);
        }

        await player.SendAsync(
            GameOpcode.MsgPassiveSkillInfo,
            new MsgPassiveSkillInfo(player.UiId, player.PassiveSkills).ToPayload(),
            cancellationToken).ConfigureAwait(false);
        await player.SendAsync(
            GameOpcode.MsgActiveSkillInfo,
            new MsgActiveSkillInfo(player.UiId, player.ActiveSkills).ToPayload(),
            cancellationToken).ConfigureAwait(false);

        await player.SendAsync(GameOpcode.MsgRelationAll, new MsgRelationAll(player.UiId, 0).ToPayload(), cancellationToken).ConfigureAwait(false);
        await player.SendAsync(GameOpcode.MsgAltarInfo, new MsgAltarInfo(0).ToPayload(), cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Player {Name} (UiId={UiId}) ready in Map={Map}", player.Character.Name, player.UiId, player.MapId);
    }

    private async Task CleanupPlayerAsync(PlayerSession player)
    {
        try
        {
            if (player.InWorld)
            {
                WorldMap? map = _worldManager.GetMap(player.MapId);
                if (map is not null)
                {
                    await map.LeaveAsync(player).ConfigureAwait(false);
                }

                await _characterRepository.UpdateCharacterPositionAsync(
                    player.Character.Id,
                    (short)player.MapId,
                    player.X,
                    player.Z).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error saving/cleaning up player {Name}", player.Character.Name);
        }
        finally
        {
            _worldManager.UnregisterPlayer(player);
        }
    }

    private static List<ActiveSkillEntry> ParseActiveSkills(string? magicInfo)
    {
        var list = new List<ActiveSkillEntry>();
        if (string.IsNullOrWhiteSpace(magicInfo))
        {
            return list;
        }

        foreach (string token in magicInfo.Split('#', StringSplitOptions.RemoveEmptyEntries))
        {
            string trimmed = token.Trim();
            if (!trimmed.StartsWith('[') || !trimmed.EndsWith(']') || trimmed.Length <= 2)
            {
                continue;
            }

            string[] parts = trimmed[1..^1].Split(',');
            if (parts.Length >= 1 && int.TryParse(parts[0], out int magicId))
            {
                ushort rank = parts.Length >= 2 && ushort.TryParse(parts[1], out ushort r) ? r : (ushort)0;
                list.Add(new ActiveSkillEntry(magicId, rank));
            }
        }

        return list;
    }

    private static List<PassiveSkillEntry> ParsePassiveSkills(string? skillInfo)
    {
        var list = new List<PassiveSkillEntry>();
        if (string.IsNullOrWhiteSpace(skillInfo))
        {
            return list;
        }

        foreach (string token in skillInfo.Split('#', StringSplitOptions.RemoveEmptyEntries))
        {
            string trimmed = token.Trim();
            if (!trimmed.StartsWith('[') || !trimmed.EndsWith(']') || trimmed.Length <= 2)
            {
                continue;
            }

            string[] parts = trimmed[1..^1].Split(',');
            if (parts.Length >= 1 && int.TryParse(parts[0], out int skillId))
            {
                ushort rank = parts.Length >= 2 && ushort.TryParse(parts[1], out ushort r) ? r : (ushort)0;
                list.Add(new PassiveSkillEntry(skillId, rank, 0, 0));
            }
        }

        return list;
    }
}
