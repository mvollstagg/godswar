namespace GodsWar.Protocol;

/// <summary>
/// LoginServer (port 5999) and inter-server handshake opcodes for GodsWar 1.0.282 US client.
/// Ground truth: <c>docker/tools/login_shim.py</c> and <c>Client/GodsWar.exe</c>.
/// </summary>
public enum LoginOpcode : ushort
{
    MsgLogin = 1,
    MsgLoginReturnInfoLegacy = 2,
    MsgLoginReturnInfo = 3,          // S2C: 1.0.282 US client reply
    MsgRequestGameServerLegacy = 3,  // C2S: 1.0.145 / login_probe.py request
    MsgRequestGameServer = 4,        // C2S: 1.0.282 US client request
    MsgValidateGameServer = 300,
    MsgLoginGameServer = 10000,
    MsgResponseGameServer = 10001,
    MsgGameServerInfo = 10008,
}

/// <summary>
/// GameServer (port 6001) opcodes for the 1.0.282 US client (<c>Client/GodsWar.exe</c>).
/// Ground truth: <c>docker/tools/gw_protocol.py</c>.
/// </summary>
public enum GameOpcode : ushort
{
    // Handshake, Character Select & World Entry
    MsgLoginGameServer = 10000,
    MsgGameServerReady = 10001,
    MsgRoleInfo = 10002,
    MsgCreateRole = 10003,
    MsgDeleteRole = 10004,
    MsgClientServerReady = 10005, // S2C ready flag (0x4952cb) that dismisses "Loading..."
    MsgEnterGame = 10006,
    MsgClientReady = 10007,

    // Client-only keepalive / pings / UI sync (never forwarded to legacy server)
    MsgHeartbeatPing = 10015,
    MsgWalkBegin = 10016,
    MsgWalkEnd = 10017,
    MsgSceneChange = 10018,

    // Entity & Player Info
    MsgSelfInfo = 10019,
    MsgObjectInfo = 10020,
    MsgLeave = 10021,
    MsgColonyLeave = 10022,

    // Combat & Chat
    MsgFight = 10023,
    MsgAttack = 10024,
    MsgDead = 10025,
    MsgBackHome = 10026,
    MsgDrops = 10027,
    MsgUpgrade = 10028,
    MsgKitbag = 10031,
    MsgStorage = 10032,
    MsgTalk = 10033,
    MsgTalkChannel = 10034,

    // Stats & Skills
    MsgParamater = 10037,
    MsgSkill = 10038,
    MsgActiveSkillInfo = 10039,
    MsgPassiveSkillInfo = 10040,
    MsgSelfProperty = 10041,
    MsgEffect = 10042,
    MsgMagicDamage = 10043,
    MsgMagicPerform = 10044,
    MsgMagicClusterDamage = 10045,
    MsgLearn = 10046,
    MsgSkillUpgrade = 10047,

    // Items, Inventory & Stall
    MsgPickupDrops = 10048,
    MsgUseOrEquip = 10049,
    MsgMoveItem = 10050,
    MsgSetQuickEquip = 10054,
    MsgBreakItem = 10056,
    MsgStorageItem = 10057,
    MsgSell = 10058,
    MsgStall = 10059,
    MsgStallAddItem = 10060,
    MsgStallDelItem = 10061,
    MsgStallItem = 10062,
    MsgStallBuyItem = 10063,

    // NPC & Quests
    MsgTalkNpc = 10064,
    MsgNpcData = 10065,
    MsgSysNpcData = 10066,
    MsgSysFunNote = 10067,
    MsgNpcItemData = 10068,
    MsgNpcStorageData = 10069,
    MsgNpcSell = 10070,
    MsgNpcQuest = 10072,
    MsgNpcNextQuest = 10073,
    MsgNpcQuests = 10074,
    MsgNpcQuestsAvailable = 10075,
    MsgNpcQuestsUnavailable = 10076,
    MsgNpcQuestReward = 10077,
    MsgNpcQuestView = 10078,
    MsgNpcAcceptQuest = 10079,
    MsgNpcCancelQuest = 10080,
    MsgNpcCompleteQuest = 10081,
    MsgNpcQuestFailed = 10082,
    MsgNpcRewardQuest = 10083,
    MsgNpcQuestKillOrCast = 10084,
    MsgPlayerAcceptQuests = 10085,
    MsgFindQuest = 10087,

    // Relations & Trade
    MsgResume = 10092,
    MsgRelationAll = 10093,
    MsgRelationRequest = 10094,
    MsgRelationResponse = 10095,
    MsgRelationDelete = 10096,
    MsgRelation = 10097,
    MsgTrade = 10098,
    MsgTradeMoney = 10099,
    MsgTradeItem = 10100,
    MsgTradeAddItem = 10101,
    MsgTradeRemoveItem = 10102,
    MsgEquipForge = 10103,
    MsgEquipForgeEquip = 10106,
    MsgEquipForgeMaterial = 10107,
    MsgBagSnapshotSync = 10108,
    MsgQuickbarInit = 10111,
    MsgGoldBox = 10112,
    MsgGoldBoxReturn = 10114,

    // Party / Team
    MsgTeam = 10117,
    MsgTeamAdd = 10118,
    MsgTeamInfo = 10119,
    MsgTeamAccept = 10120,
    MsgTeamDelete = 10121,
    MsgTeamReplaceLeader = 10122,
    MsgTeamDissolve = 10123,
    MsgTeamLeave = 10124,
    MsgTeamTip = 10125,
    MsgTeamReject = 10126,
    MsgTeamRefresh = 10127,
    MsgTeamDestroy = 10128,
    MsgUpdateMp = 10129,

    // Guild / Consortia
    MsgConsortiaCreateResponse = 10131,
    MsgConsortiaBaseInfo = 10132,
    MsgUpdateBaseInfo = 10133,
    MsgUpdateBasePartInfo = 10134,
    MsgUpdatePlacardInfo = 10135,
    MsgUpdateBuildingInfo = 10136,
    MsgConsortiaMemberList = 10137,
    MsgConsortiaMemberOne = 10138,
    MsgConsortiaInvite = 10139,
    MsgConsortiaRequest = 10140,
    MsgConsortiaExit = 10143,
    MsgConsortiaText = 10144,
    MsgConsortiaDuty = 10145,
    MsgConsortiaMemberDel = 10146,

    // World Data, Status, Mall, Movement
    MsgAltarInfo = 10156,
    MsgError = 10157,
    MsgManageReturn = 10158,
    MsgSkillPointUpgrade = 10159,
    MsgSynGameData = 10160,
    MsgStatus = 10161,
    MsgLoginQueue = 10162,
    MsgServerNote = 10163,
    MsgSkillBackup = 10164,
    MsgSkillInterrupt = 10165,
    MsgMallItemData = 10172,
    MsgMallItemData2 = 10173,
    MsgMallSell = 10174,
    MsgAssociationSell = 10175,
    MsgAssociationDiscount = 10176,
    MsgNpcQuestExplorerResult = 10178,
    MsgSysAddItem = 10179,
    MsgSysDelItem = 10180,
    MsgSysAddItem2 = 10181,
    MsgTargetInfo = 10184,
    MsgDelayExit = 10185,
    MsgWalk = 10187,
    MsgMentorListQuery = 10227,
    MsgServerTime = 10303,
    MsgKeepalivePing = 10304,
}
