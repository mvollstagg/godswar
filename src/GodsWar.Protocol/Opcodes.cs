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
    MsgTalkNpcV227 = 10065,
    MsgNpcData = 10065,
    MsgSysNpcData = 10066,
    MsgSysFunNote = 10067,
    MsgNpcItemData = 10068,
    MsgNpcStorageData = 10069,
    MsgNpcSell = 10070,
    MsgLearnSucceed = 10071,
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
    MsgGoldBoxInDial = 10115,

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
    MsgManageReturnV227 = 10162,
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
    MsgWalkV227 = 10192,
    MsgMentorListQuery = 10227,
    MsgPrentice = 10227,
    MsgPrenticeList = 10228,
    MsgPrenticeTip = 10229,

    // 1.0.282 Pet System (10230..10243)
    MsgPetList = 10230,
    MsgPetSummon = 10231,
    MsgPetRecall = 10232,
    MsgPetRelease = 10233,
    MsgPetFeed = 10234,
    MsgPetRename = 10235,
    MsgPetMerge = 10236,
    MsgPetAction = 10237,
    MsgPetState = 10238,
    MsgPetEquip = 10239,
    MsgPetUpdate = 10240,
    MsgPetView = 10241,
    MsgPetTip = 10242,
    MsgPetAdd = 10243,

    // 1.0.282 AFK / Auto-Hunt System (10295..10299)
    MsgAfkRequest = 10295,
    MsgAfkState = 10296,
    MsgAfkSync = 10297,
    MsgAfkTime = 10298,
    MsgAfkBroadcast = 10299,

    MsgServerTime = 10303,
    MsgKeepalivePing = 10304,
}

/// <summary>
/// Bidirectional opcode mapping between canonical <see cref="GameOpcode"/> (1.0.282)
/// and the v2.26.002 (<c>v2.27</c>) client's shifted opcode table.
/// </summary>
public static class V227OpcodeMap
{
    private static readonly Dictionary<GameOpcode, ushort> CanonicalToWire = new()
    {
        [GameOpcode.MsgTalkNpc] = 10065,
        [GameOpcode.MsgNpcData] = 10066,
        [GameOpcode.MsgSysNpcData] = 10067,
        [GameOpcode.MsgSysFunNote] = 10068,
        [GameOpcode.MsgNpcItemData] = 10069,
        [GameOpcode.MsgNpcStorageData] = 10070,
        [GameOpcode.MsgNpcSell] = 10071,
        [GameOpcode.MsgLearnSucceed] = 10072,
        [GameOpcode.MsgNpcQuest] = 10073,
        [GameOpcode.MsgNpcNextQuest] = 10074,
        [GameOpcode.MsgNpcQuests] = 10075,
        [GameOpcode.MsgNpcQuestsAvailable] = 10076,
        [GameOpcode.MsgNpcQuestsUnavailable] = 10077,
        [GameOpcode.MsgNpcQuestReward] = 10078,
        [GameOpcode.MsgNpcQuestView] = 10079,
        [GameOpcode.MsgNpcAcceptQuest] = 10080,
        [GameOpcode.MsgNpcCancelQuest] = 10081,
        [GameOpcode.MsgNpcCompleteQuest] = 10082,
        [GameOpcode.MsgNpcQuestFailed] = 10083,
        [GameOpcode.MsgNpcRewardQuest] = 10084,
        [GameOpcode.MsgNpcQuestKillOrCast] = 10085,
        [GameOpcode.MsgPlayerAcceptQuests] = 10088,
        [GameOpcode.MsgFindQuest] = 10090,
        [GameOpcode.MsgAltarInfo] = 10160,
        [GameOpcode.MsgManageReturn] = 10162,
        [GameOpcode.MsgWalk] = 10192,
        [GameOpcode.MsgPetList] = 10235,
        [GameOpcode.MsgPetSummon] = 10236,
        [GameOpcode.MsgPetRecall] = 10237,
        [GameOpcode.MsgPetRelease] = 10238,
        [GameOpcode.MsgPetFeed] = 10239,
        [GameOpcode.MsgPetRename] = 10240,
        [GameOpcode.MsgPetMerge] = 10241,
        [GameOpcode.MsgPetAction] = 10242,
        [GameOpcode.MsgPetState] = 10243,
        [GameOpcode.MsgPetEquip] = 10244,
        [GameOpcode.MsgPetUpdate] = 10245,
        [GameOpcode.MsgPetView] = 10246,
        [GameOpcode.MsgPetTip] = 10247,
        [GameOpcode.MsgPetAdd] = 10248,
        [GameOpcode.MsgServerTime] = 10309,
    };

    private static readonly Dictionary<ushort, GameOpcode> WireToCanonical = new()
    {
        [10065] = GameOpcode.MsgTalkNpc,
        [10066] = GameOpcode.MsgNpcData,
        [10067] = GameOpcode.MsgSysNpcData,
        [10071] = GameOpcode.MsgNpcSell,
        [10073] = GameOpcode.MsgNpcQuest,
        [10074] = GameOpcode.MsgNpcNextQuest,
        [10075] = GameOpcode.MsgNpcQuests,
        [10079] = GameOpcode.MsgNpcQuestView,
        [10080] = GameOpcode.MsgNpcAcceptQuest,
        [10081] = GameOpcode.MsgNpcCancelQuest,
        [10082] = GameOpcode.MsgNpcCompleteQuest,
        [10084] = GameOpcode.MsgNpcRewardQuest,
        [10088] = GameOpcode.MsgPlayerAcceptQuests,
        [10089] = GameOpcode.MsgFindQuest,
        [10190] = GameOpcode.MsgDelayExit,
        [10192] = GameOpcode.MsgWalk,
        [10235] = GameOpcode.MsgPetList,
        [10236] = GameOpcode.MsgPetSummon,
        [10237] = GameOpcode.MsgPetRecall,
        [10238] = GameOpcode.MsgPetRelease,
        [10239] = GameOpcode.MsgPetFeed,
        [10240] = GameOpcode.MsgPetRename,
        [10241] = GameOpcode.MsgPetMerge,
        [10242] = GameOpcode.MsgPetAction,
        [10243] = GameOpcode.MsgPetState,
        [10244] = GameOpcode.MsgPetEquip,
        [10245] = GameOpcode.MsgPetUpdate,
        [10246] = GameOpcode.MsgPetView,
        [10247] = GameOpcode.MsgPetTip,
        [10248] = GameOpcode.MsgPetAdd,
        [10309] = GameOpcode.MsgServerTime,
    };

    public static ushort ToWire(GameOpcode opcode) =>
        CanonicalToWire.TryGetValue(opcode, out ushort wire) ? wire : (ushort)opcode;

    public static GameOpcode FromWire(ushort wireOpcode) =>
        WireToCanonical.TryGetValue(wireOpcode, out GameOpcode canonical) ? canonical : (GameOpcode)wireOpcode;
}
