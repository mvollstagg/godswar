using GodsWar.Database.Models;
using GodsWar.Database.Repositories;
using GodsWar.GameServer.Auth;
using GodsWar.GameServer.Game;
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
    private readonly GameplayHandlers _gameplay;
    private readonly QuestService? _quests;
    private readonly NpcShopAndForgeService? _npcShopAndForge;
    private readonly ChatAndPortalService? _chatAndPortal;
    private readonly SocialService? _social;
    private readonly ClientFeaturesService? _features;
    private readonly ILogger<GameSessionHandler> _logger;

    public GameSessionHandler(
        ICharacterRepository characterRepository,
        ValidationTokenStore tokenStore,
        WorldManager worldManager,
        GameplayHandlers gameplay,
        ILogger<GameSessionHandler> logger,
        QuestService? quests = null,
        NpcShopAndForgeService? npcShopAndForge = null,
        ChatAndPortalService? chatAndPortal = null,
        SocialService? social = null,
        ClientFeaturesService? features = null)
    {
        _characterRepository = characterRepository;
        _tokenStore = tokenStore;
        _worldManager = worldManager;
        _gameplay = gameplay;
        _logger = logger;
        _quests = quests;
        _npcShopAndForge = npcShopAndForge;
        _chatAndPortal = chatAndPortal;
        _social = social;
        _features = features;
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
                        if (player is not null && player.InWorld && !player.IsDead)
                        {
                            MsgWalk walk = MsgWalk.Parse(payload);
                            WorldMap? map = _worldManager.GetMap(player.MapId);
                            if (map is not null)
                            {
                                await map.MoveAsync(player, walk.X, walk.Z, walk.Atan2, walk.State, cancellationToken).ConfigureAwait(false);
                                PortalZone? portal = map.FindPortalAt(walk.X, walk.Z);
                                if (portal is not null && _chatAndPortal is not null)
                                {
                                    await _chatAndPortal.TransitionMapAsync(player, portal.TargetMapId, portal.TargetX, portal.TargetZ, cancellationToken).ConfigureAwait(false);
                                }
                            }
                        }
                        break;

                    case GameOpcode.MsgFight:
                        if (player is not null && player.InWorld)
                        {
                            await _gameplay.HandleFightAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgAttack:
                        if (player is not null && player.InWorld)
                        {
                            await _gameplay.HandleAttackAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgSkill:
                        if (player is not null && player.InWorld)
                        {
                            await _gameplay.HandleSkillAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgBackHome:
                        if (player is not null && player.InWorld)
                        {
                            await _gameplay.HandleBackHomeAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgPickupDrops:
                        if (player is not null && player.InWorld)
                        {
                            await _gameplay.HandlePickupDropsAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgUseOrEquip:
                        if (player is not null && player.InWorld)
                        {
                            await _gameplay.HandleUseOrEquipAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgMoveItem:
                        if (player is not null && player.InWorld)
                        {
                            await _gameplay.HandleMoveItemAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgBreakItem:
                        if (player is not null && player.InWorld)
                        {
                            await _gameplay.HandleBreakItemAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgTalk:
                        if (player is not null && player.InWorld && _chatAndPortal is not null)
                        {
                            await _chatAndPortal.HandleTalkAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgTalkChannel:
                        if (player is not null && player.InWorld && _chatAndPortal is not null)
                        {
                            await _chatAndPortal.HandleTalkChannelAsync(player, payload).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgTalkNpc:
                        if (player is not null && player.InWorld && _npcShopAndForge is not null)
                        {
                            await _npcShopAndForge.HandleTalkNpcAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgSysNpcData:
                        if (player is not null && player.InWorld && _npcShopAndForge is not null && _chatAndPortal is not null)
                        {
                            await _npcShopAndForge.HandleSysNpcDataAsync(player, payload, _chatAndPortal, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgNpcSell:
                        if (player is not null && player.InWorld && _npcShopAndForge is not null)
                        {
                            await _npcShopAndForge.HandleNpcBuyAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgSell:
                        if (player is not null && player.InWorld && _npcShopAndForge is not null)
                        {
                            await _npcShopAndForge.HandleSellToNpcAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgStorageItem:
                        if (player is not null && player.InWorld && _npcShopAndForge is not null)
                        {
                            await _npcShopAndForge.HandleStorageItemAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgNpcAcceptQuest:
                        if (player is not null && player.InWorld && _quests is not null)
                        {
                            await _quests.HandleAcceptQuestAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgNpcCancelQuest:
                        if (player is not null && player.InWorld && _quests is not null)
                        {
                            await _quests.HandleCancelQuestAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgNpcCompleteQuest:
                        if (player is not null && player.InWorld && _quests is not null)
                        {
                            await _quests.HandleCompleteQuestAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgFindQuest:
                        if (player is not null && player.InWorld && _quests is not null)
                        {
                            await _quests.HandleFindQuestAsync(player, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgEquipForgeEquip:
                        if (player is not null && player.InWorld && _npcShopAndForge is not null)
                        {
                            await _npcShopAndForge.HandleForgeEquipSelectAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgEquipForgeMaterial:
                        if (player is not null && player.InWorld && _npcShopAndForge is not null)
                        {
                            await _npcShopAndForge.HandleForgeMaterialSelectAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgEquipForge:
                        if (player is not null && player.InWorld && _npcShopAndForge is not null)
                        {
                            await _npcShopAndForge.HandleEquipForgeAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgRelationRequest:
                        if (player is not null && player.InWorld && _social is not null)
                        {
                            await _social.HandleRelationRequestAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgRelationResponse:
                        if (player is not null && player.InWorld && _social is not null)
                        {
                            await _social.HandleRelationResponseAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgRelationDelete:
                        if (player is not null && player.InWorld && _social is not null)
                        {
                            await _social.HandleRelationDeleteAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgTrade:
                        if (player is not null && player.InWorld && _social is not null)
                        {
                            await _social.HandleTradeAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgTradeMoney:
                        if (player is not null && player.InWorld && _social is not null)
                        {
                            await _social.HandleTradeMoneyAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgTradeAddItem:
                        if (player is not null && player.InWorld && _social is not null)
                        {
                            await _social.HandleTradeAddItemAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgTradeRemoveItem:
                        if (player is not null && player.InWorld && _social is not null)
                        {
                            await _social.HandleTradeRemoveItemAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgTeamInfo:
                        if (player is not null && player.InWorld && _social is not null)
                        {
                            await _social.HandleTeamInfoAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgTeam:
                        if (player is not null && player.InWorld && _social is not null)
                        {
                            await _social.HandleTeamInviteAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgTeamAccept:
                        if (player is not null && player.InWorld && _social is not null)
                        {
                            await _social.HandleTeamAcceptAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgTeamReject:
                        if (player is not null && player.InWorld && _social is not null)
                        {
                            await _social.HandleTeamRejectAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgTeamLeave:
                        if (player is not null && player.InWorld && _social is not null)
                        {
                            await _social.HandleTeamLeaveAsync(player, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgTeamDelete:
                        if (player is not null && player.InWorld && _social is not null)
                        {
                            await _social.HandleTeamDeleteAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgTeamReplaceLeader:
                        if (player is not null && player.InWorld && _social is not null)
                        {
                            await _social.HandleTeamReplaceLeaderAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgTeamDissolve:
                        if (player is not null && player.InWorld && _social is not null)
                        {
                            await _social.HandleTeamDissolveAsync(player, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgConsortiaInvite:
                        if (player is not null && player.InWorld && _social is not null)
                        {
                            await _social.HandleConsortiaInviteAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgConsortiaRequest:
                        if (player is not null && player.InWorld && _social is not null)
                        {
                            await _social.HandleConsortiaResponseAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgConsortiaExit:
                        if (player is not null && player.InWorld && _social is not null)
                        {
                            await _social.HandleConsortiaExitAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgConsortiaText:
                        if (player is not null && player.InWorld && _social is not null)
                        {
                            await _social.HandleConsortiaTextAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgConsortiaDuty:
                        if (player is not null && player.InWorld && _social is not null)
                        {
                            await _social.HandleConsortiaDutyAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgConsortiaMemberDel:
                        if (player is not null && player.InWorld && _social is not null)
                        {
                            await _social.HandleConsortiaMemberDelAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgMallItemData:
                        if (player is not null && player.InWorld && _features is not null)
                        {
                            await _features.HandleMallItemDataAsync(player, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgMallItemData2:
                        if (player is not null && player.InWorld && _features is not null)
                        {
                            await _features.HandleAssociationItemDataAsync(player, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgMallSell:
                        if (player is not null && player.InWorld && _features is not null)
                        {
                            await _features.HandleMallSellAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgAssociationSell:
                        if (player is not null && player.InWorld && _features is not null)
                        {
                            await _features.HandleAssociationSellAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgGoldBox:
                        if (player is not null && player.InWorld && _features is not null)
                        {
                            await _features.HandleGoldBoxAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgLearn:
                        if (player is not null && player.InWorld && _features is not null)
                        {
                            await _features.HandleLearnSkillAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgSkillUpgrade:
                        if (player is not null && player.InWorld && _features is not null)
                        {
                            await _features.HandleSkillUpgradeAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgSkillBackup:
                        if (player is not null && player.InWorld && _features is not null)
                        {
                            await _features.HandleSkillBackupAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgStall:
                        if (player is not null && player.InWorld && _features is not null)
                        {
                            await _features.HandleStallAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgStallAddItem:
                        if (player is not null && player.InWorld && _features is not null)
                        {
                            await _features.HandleStallAddItemAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgStallDelItem:
                        if (player is not null && player.InWorld && _features is not null)
                        {
                            await _features.HandleStallDelItemAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgStallItem:
                        if (player is not null && player.InWorld && _features is not null)
                        {
                            await _features.HandleStallItemQueryAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgStallBuyItem:
                        if (player is not null && player.InWorld && _features is not null)
                        {
                            await _features.HandleStallBuyItemAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgPetSummon:
                        if (player is not null && player.InWorld && _features is not null)
                        {
                            await _features.HandlePetSummonAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgPetRecall:
                        if (player is not null && player.InWorld && _features is not null)
                        {
                            await _features.HandlePetRecallAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgPetRelease:
                        if (player is not null && player.InWorld && _features is not null)
                        {
                            await _features.HandlePetReleaseAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgPetFeed:
                        if (player is not null && player.InWorld && _features is not null)
                        {
                            await _features.HandlePetFeedAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgPetRename:
                        if (player is not null && player.InWorld && _features is not null)
                        {
                            await _features.HandlePetRenameAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgAfkRequest:
                        if (player is not null && player.InWorld && _features is not null)
                        {
                            await _features.HandleAfkRequestAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgPrentice:
                        if (player is not null && player.InWorld && _features is not null)
                        {
                            await _features.HandlePrenticeAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgTargetInfo:
                        if (player is not null && player.InWorld && _features is not null)
                        {
                            await _features.HandleTargetInfoAsync(player, payload, cancellationToken).ConfigureAwait(false);
                        }
                        break;

                    case GameOpcode.MsgSetQuickEquip:
                        if (player is not null && player.InWorld && _features is not null)
                        {
                            await _features.HandleSetQuickEquipAsync(player, payload, cancellationToken).ConfigureAwait(false);
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
        if (ch.CurHp <= 0)
        {
            ch.CurHp = Math.Max(1, ch.MaxHp);
        }
        var session = new PlayerSession(uiId, account, ch, connection, equips, kitbags, activeSkills, passiveSkills)
        {
            Storage = ItemEquipSlot.ParseDbKitbagString(kitbag?.Storage, 24),
        };
        if (_quests is not null) await _quests.LoadPlayerQuestsAsync(session, cancellationToken).ConfigureAwait(false);
        if (_social is not null) await _social.LoadRelationsAsync(session, cancellationToken).ConfigureAwait(false);
        if (_npcShopAndForge is not null) await _npcShopAndForge.LoadAltarsAsync(session, cancellationToken).ConfigureAwait(false);
        if (_features is not null) await _features.LoadPlayerFeaturesAsync(session, cancellationToken).ConfigureAwait(false);
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

        if (_social is not null)
        {
            await _social.SendRelationsOnLoginAsync(player, cancellationToken).ConfigureAwait(false);
            await _social.SendGuildOnLoginAsync(player, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await player.SendAsync(GameOpcode.MsgRelationAll, new MsgRelationAll(player.UiId, 0).ToPayload(), cancellationToken).ConfigureAwait(false);
        }

        if (_quests is not null)
        {
            await _quests.SendActiveQuestsOnLoginAsync(player, cancellationToken).ConfigureAwait(false);
        }

        if (_features is not null)
        {
            await _features.SendFeaturesOnLoginAsync(player, cancellationToken).ConfigureAwait(false);
        }

        await player.SendAsync(GameOpcode.MsgAltarInfo, new MsgAltarInfo(0).ToPayload(), cancellationToken).ConfigureAwait(false);
        await player.SendAsync(GameOpcode.MsgParamater, MsgParamater.CreateDefaultPayload(), cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Player {Name} (UiId={UiId}) ready in Map={Map}", player.Character.Name, player.UiId, player.MapId);
    }

    private async Task CleanupPlayerAsync(PlayerSession player)
    {
        try
        {
            if (_social is not null)
            {
                await _social.CancelTradeAsync(player, CancellationToken.None).ConfigureAwait(false);
                await _social.HandleTeamLeaveAsync(player, CancellationToken.None).ConfigureAwait(false);
            }

            if (player.InWorld)
            {
                WorldMap? map = _worldManager.GetMap(player.MapId);
                if (map is not null)
                {
                    await map.LeaveAsync(player).ConfigureAwait(false);
                }

                player.Character.Map = (short)player.MapId;
                player.Character.PosX = player.X;
                player.Character.PosZ = player.Z;
                await _characterRepository.UpdateCharacterStateAsync(player.Character).ConfigureAwait(false);
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
