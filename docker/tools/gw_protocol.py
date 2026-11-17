"""Protocol framing and packet translation between GodsWar 1.0.282 US client and 2007 GameServer."""

from collections import deque
import struct
import time

from gw_npc_map import resolve_npc_name

# Client opcode 10005 triggers CMainModule server-ready flag at 0x4952cb so the
# client sends MSG_CLIENT_READY (10007) and dismisses the "Loading..." modal.
PKT_CLIENT_SERVER_READY = struct.pack("<HH", 4, 10005)

# Server (MsgTypes.h 2007) -> Client (GodsWar.exe 1.0.282) opcode table.
S2C_OP_MAP = {
    10000: 10000,   # MSG_LOGIN_GAMESERVER
    10001: 10001,   # MSG_RESPONSE_GAMESERVER / MSG_GAMESERVER_READY
    10002: 10002,   # MSG_ROLE_INFO
    10005: 10005,   # Ready ack if ever sent directly
    10009: 10019,   # MSG_SELFINFO (struct resized 484 -> 1192)
    10010: 10020,   # MSG_OBJECTINFO (struct reordered)
    10011: 10021,   # MSG_LEAVE
    10012: 10022,   # MSG_COLONY_LEAVE
    10013: 10016,   # MSG_WALK_BEGIN
    10014: 10017,   # MSG_WALK_END
    10015: 10018,   # MSG_SCENE_CHANGE
    10016: 10023,   # MSG_FIGHT
    10017: 10024,   # MSG_ATTACK
    10018: 10025,   # MSG_DEAD
    10020: 10027,   # MSG_DROPS
    10021: 10028,   # MSG_UPGRADE
    10022: 10031,   # MSG_KITBAG (struct resized)
    10023: 10032,   # MSG_STORAGE
    10024: 10033,   # MSG_TALK
    10026: 10037,   # MSG_PARAMATER
    10027: 10038,   # MSG_SKILL
    10028: 10039,   # MSG_ACTIVESKILL_INFO (header pad fixed)
    10029: 10040,   # MSG_PASSIVESKILL_INFO (header pad fixed)
    10030: 10041,   # MSG_SELFPROPERTY (struct resized 116 -> 132)
    10031: 10042,   # MSG_EFFECT
    10032: 10043,   # MSG_MAGIC_DAMAGE
    10033: 10044,   # MSG_MAGIC_PERFORM
    10034: 10045,   # MSG_MAGIC_CLUSTER_DAMAGE
    10035: 10046,   # MSG_LEARN
    10036: 10047,   # MSG_SKILL_UPGRADE
    10037: 10048,   # MSG_PICKUPDROPS
    10038: 10049,   # MSG_USEOREQUIP
    10039: 10050,   # MSG_MOVEITEM
    10040: 10056,   # MSG_BREAK_ITEM
    10041: 10057,   # MSG_STORAGEITEM
    10042: 10058,   # MSG_SELL
    10043: 10059,   # MSG_STALL
    10044: 10060,   # MSG_STALLADDITEM
    10045: 10061,   # MSG_STALLDELITEM
    10046: 10062,   # MSG_STALLITEM
    10047: 10063,   # MSG_STALLBUYITEM
    10048: 10064,   # MSG_TALKNPC
    10049: 10065,   # MSG_NPCDATA
    10050: 10066,   # MSG_SYS_NPC_DATA
    10051: 10067,   # MSG_SYS_FUN_NOTE / LUA_FULL_WIN
    10052: 10068,   # MSG_NPCITEMDATA
    10053: 10069,   # MSG_NPCSTORAGEDATA
    10054: 10070,   # MSG_NPCSELL
    10055: 10072,   # MSG_NPCQUEST
    10056: 10073,   # MSG_NPCNEXTQUEST
    10057: 10074,   # MSG_NPCQUESTS
    10058: 10075,   # MSG_NPCQUESTSAVAILABLE
    10059: 10076,   # MSG_NPCQUESTSUNAVAILABLE
    10060: 10077,   # MSG_NPCQUESTREWARD
    10061: 10078,   # MSG_NPCQUESTVIEW
    10062: 10079,   # MSG_NPCACCEPTQUEST
    10063: 10080,   # MSG_NPCCANCELQUEST
    10064: 10081,   # MSG_NPCCOMPLETEQUEST
    10065: 10082,   # MSG_NPCQUESTFAILD
    10066: 10083,   # MSG_NPCREWARDQUEST
    10067: 10084,   # MSG_NPCQUESTKILLORCAST
    10068: 10085,   # MSG_PLAYER_ACCEPTQUESTS
    10069: 10087,   # MSG_FINDQUESTRESULT
    10070: 10178,   # MSG_NPC_QUESTEXPLORERRESULT
    10071: 10092,   # MSG_RESUNE
    10072: 10093,   # MSG_RELATIONALL (struct resized 28B -> 32B items)
    10073: 10094,   # MSG_RELATION_REQUEST
    10074: 10095,   # MSG_RELATION_RESPONSE
    10075: 10096,   # MSG_RELATION_DELETE
    10076: 10097,   # MSG_RELATION
    10077: 10098,   # MSG_TRADE
    10078: 10099,   # MSG_TRADE_MONEY
    10079: 10100,   # MSG_TRADE_ITEM (struct repacked 108 -> 120)
    10081: 10102,   # MSG_TRADE_REMOVEITEM
    10082: 10103,   # MSG_EQUIPFORGE
    10085: 10106,   # MSG_EQUIPFORGE_EQUIP
    10086: 10107,   # MSG_EQUIPFORGE_MATERIAL
    10088: 10112,   # MSG_GOLD_BOX
    10090: 10114,   # MSG_GOLD_BOX_RETURN
    10091: 10117,   # MSG_TEAM
    10092: 10118,   # MSG_TEAM_ADD
    10093: 10119,   # MSG_TEAMINFO
    10095: 10121,   # MSG_TEAM_DELETE
    10099: 10125,   # MSG_TEAM_TIP
    10101: 10127,   # MSG_TEAM_REFLASH
    10102: 10128,   # MSG_TEAM_DESTROY
    10103: 10129,   # MSG_UPDATE_MP
    10104: 10131,   # MSG_CONSORTIA_CREATE_RESPONSE
    10106: 10132,   # MSG_CONSORTIA_BASE_INFO
    10107: 10133,   # MSG_UPDATE_BASE_INFO
    10108: 10134,   # MSG_UPDATE_BASE_PART_INFO
    10109: 10135,   # MSG_UPDATE_PLACARD_INFO
    10110: 10136,   # MSG_UPDATE_BUILDING_INFO
    10111: 10137,   # MSG_CONSORTIA_MEMBER_LIST
    10112: 10138,   # MSG_CONSORTIA_MEMBER_ONE
    10113: 10139,   # MSG_CONSORTIA_INVITE
    10114: 10140,   # MSG_CONSORTIA_REQUEST
    10115: 10156,   # MSG_ALTAR_INFO
    10116: 10157,   # MSG_ERROR
    10117: 10158,   # MSG_MANAGE_RETURN
    10118: 10159,   # MSG_SKILLPOINT_UPGRADE
    10119: 10160,   # MSG_SYN_GAMEDATA (struct resized 204 -> 236)
    10120: 10161,   # MSG_STATUS
    10121: 10162,   # MSG_LOGIN_QUEUE
    10122: 10163,   # MSG_SERVER_NOTE
    10123: 10164,   # MSG_SKILLBACKUP
    10124: 10165,   # MSG_SKILL_INTERRUPT
    10127: 10143,   # MSG_CONSORTIA_EXIT
    10128: 10144,   # MSG_CONSORTIA_TEXT
    10129: 10145,   # MSG_CONSORTIA_DUTY
    10130: 10146,   # MSG_CONSORTIA_MEMBER_DEL
    10131: 10172,   # MSG_MALL_ITEMDATA
    10132: 10173,   # MSG_MALL_ITEMDATA_2
    10133: 10174,   # MSG_MALL_SELL
    10134: 10175,   # MSG_ASSOCIATION_SELL
    10135: 10176,   # MSG_ASSOCIATIONDISCOUNT
    10138: 10179,   # MSG_SYS_ADD_ITEM
    10139: 10180,   # MSG_SYS_DEL_ITEM
    10140: 10181,   # MSG_SYS_ADD_ITEM_2
    10141: 10184,   # MSG_TARGETINFO
    10142: 10185,   # MSG_DELAY_EXIT
    10143: 10187,   # MSG_WALK (struct resized 28 -> 20)
}

# Client (1.0.282) -> Server (GameServer.exe 0x40c000 MsgEventHandle) opcodes.
C2S_OP_MAP = {
    10000: 10000,   # MSG_LOGIN_GAMESERVER
    10003: 10003,   # MSG_CREATE_ROLE
    10004: 10004,   # MSG_DELETE_ROLE
    10006: 10006,   # MSG_ENTER_GAME
    10007: 10007,   # MSG_CLIENT_READY
    10016: 10013,   # MSG_WALK_BEGIN
    10017: 10014,   # MSG_WALK_END
    10023: 10016,   # MSG_FIGHT
    10024: 10017,   # MSG_ATTACK
    10026: 10019,   # MSG_BACKHOME
    10033: 10024,   # MSG_TALK
    10034: 10025,   # MSG_TALKCHANNEL
    10038: 10027,   # MSG_SKILL
    10046: 10035,   # MSG_LEARN
    10047: 10036,   # MSG_SKILL_UPGRADE
    10048: 10037,   # MSG_PICKUPDROPS
    10049: 10038,   # MSG_USEOREQUIP
    10050: 10039,   # MSG_MOVEITEM
    10056: 10040,   # MSG_BREAK_ITEM
    10057: 10041,   # MSG_STORAGEITEM
    10058: 10042,   # MSG_SELL
    10059: 10043,   # MSG_STALL
    10060: 10044,   # MSG_STALLADDITEM
    10061: 10045,   # MSG_STALLDELITEM
    10062: 10046,   # MSG_STALLITEM
    10063: 10047,   # MSG_STALLBUYITEM
    10064: 10048,   # MSG_TALKNPC
    10066: 10049,   # MSG_NPCDATA / SYS_NPC_DATA (converted 92B -> 24B)
    10070: 10054,   # MSG_NPCSELL
    10079: 10062,   # MSG_NPCACCEPTQUEST
    10080: 10063,   # MSG_NPCCANCELQUEST
    10081: 10064,   # MSG_NPCCOMPLETEQUEST
    10083: 10066,   # MSG_NPCREWARDQUEST
    10086: 10070,   # MSG_NPCQUEST_...
    10087: 10069,   # MSG_FINDQUEST
    10094: 10073,   # MSG_RELATION_REQUEST
    10095: 10074,   # MSG_RELATION_RESPONSE
    10096: 10075,   # MSG_RELATION_DELETE
    10098: 10077,   # MSG_TRADE
    10099: 10078,   # MSG_TRADE_MONEY
    10100: 10079,   # MSG_TRADE_ITEM
    10101: 10080,   # MSG_TRADE_ADDITEM
    10102: 10081,   # MSG_TRADE_REMOVEITEM
    10103: 10082,   # MSG_EQUIPFORGE
    10112: 10088,   # MSG_GOLD_BOX
    10114: 10090,   # MSG_GOLD_BOX_RETURN
    10117: 10091,   # MSG_TEAM_INVITE
    10119: 10093,   # MSG_TEAMINFO (struct repacked 72 -> 70)
    10120: 10094,   # MSG_TEAM_ADD
    10121: 10095,   # MSG_TEAM_DELETE
    10122: 10096,   # MSG_TEAM_REPLACELEADER
    10123: 10097,   # MSG_TEAM_DISSOLVE
    10124: 10098,   # MSG_TEAM_LEAVE
    10126: 10100,   # MSG_TEAM_REJECT
    10164: 10123,   # MSG_SKILLBACKUP
    10184: 10141,   # MSG_TARGETINFO
    10185: 10142,   # MSG_DELAY_EXIT
    10187: 10143,   # MSG_WALK (struct converted 20B -> 28B)
    10258: 10024,   # MSG_TALK alt? Check if chat uses 10033 or 10258
}

# Client-only opcodes never present on 2007 GameServer (drop / reply in shim).
C2S_CLIENT_ONLY = {
    10015,   # Heartbeat/RTT ping (8B) -> echoed back to client
    10054,   # MSG_SETQUICKEQUIP (40B; would collide with GS MSG_NPCSELL 10054!)
    10108,   # Client bag-snapshot delta sync (52B; collides with GS 10108!)
    10111,   # Quickbar/guild state init (4B)
    10227,   # Apprentice/Mentor list query (72B)
    10303,   # _MSG_SERVERTIME (16B) -> answered with current epoch time
    10304,   # Keepalive ping (8B)
}


def pack_client_equip(
    base: int,
    appends: tuple[int, ...],
    base_lv: int,
    append_lv: int,
    binding: int,
    overlap: int,
) -> bytes:
    """Pack item stats into a 72-byte client SEquip record."""
    return struct.pack(
        "<i5iBBBB44x",
        base,
        *appends,
        base_lv & 0xFF,
        append_lv & 0xFF,
        binding & 0xFF,
        min(max(overlap, 0), 255),
    )


def item_32_to_72(src: bytes, offset: int) -> bytes:
    """Convert a 32-byte server ItemData into a 72-byte client SEquip."""
    if offset + 32 > len(src):
        return b"\xff\xff\xff\xff" + b"\x00" * 68
    base = struct.unpack_from("<i", src, offset)[0]
    base_lv = src[offset + 4]
    appends = struct.unpack_from("<5i", src, offset + 8)
    append_lv, binding, overlap = src[offset + 28], src[offset + 29], src[offset + 30]
    return pack_client_equip(base, appends, base_lv, append_lv, binding, overlap)


def object_data_112_to_132(src: bytes) -> bytes:
    """Repack 112-byte server ObjectData into 132-byte (33-dword) client ObjectData."""
    out = bytearray(132)
    # 0x00..0x37 match verbatim (Name[32], Gender, pad, RegionID, X, Y, Z, Speed, Style, Faction)
    out[0:56] = src[0:56]
    prestige, knight = struct.unpack_from("<HB", src, 56)
    union_id, union_duty = struct.unpack_from("<HB", src, 60)
    contrib, store_num, bag_num = struct.unpack_from("<3I", src, 64)
    cls = src[76]
    exp = struct.unpack_from("<I", src, 80)[0]
    rank = src[84]
    hp, mp = struct.unpack_from("<2i", src, 88)
    atk_dist, atk_spd = struct.unpack_from("<2H", src, 96)
    hair, pupil, belief = struct.unpack_from("<BBH", src, 100)
    money, stone = struct.unpack_from("<2I", src, 104)

    struct.pack_into("<I", out, 56, prestige)         # +0x38 (+0x234)
    struct.pack_into("<HB", out, 60, union_id, union_duty)
    out[68] = knight                                  # +0x44 (+0x240)
    struct.pack_into("<I", out, 72, contrib)          # +0x48 (+0x244)
    struct.pack_into("<I", out, 76, store_num)        # +0x4c (+0x248)
    struct.pack_into("<I", out, 80, bag_num)          # +0x50 (+0x24c)
    out[84] = cls                                     # +0x54 (+0x250)
    struct.pack_into("<I", out, 88, exp)              # +0x58 (+0x254)
    out[92] = rank                                    # +0x5c (+0x258 GetRank)
    struct.pack_into("<2i", out, 96, hp, mp)          # +0x60..+0x67 (+0x25c..+0x260)
    struct.pack_into("<2H", out, 104, atk_dist, atk_spd)
    struct.pack_into("<BBH", out, 108, hair, pupil, belief)
    struct.pack_into("<2I", out, 112, money, stone)   # +0x70..+0x77 (+0x26c..+0x270)
    return bytes(out)


class PacketBuffer:
    """Length-prefixed (<HH) TCP packet stream framer."""

    def __init__(self) -> None:
        self._buf = bytearray()

    def feed(self, chunk: bytes) -> list[bytes]:
        self._buf.extend(chunk)
        packets = []
        while len(self._buf) >= 4:
            size = struct.unpack_from("<H", self._buf, 0)[0]
            if size < 4:
                # Corrupt framing; drop buffer to avoid infinite stall.
                self._buf.clear()
                break
            if len(self._buf) < size:
                break
            packets.append(bytes(self._buf[:size]))
            del self._buf[:size]
        return packets


class GameSessionTranslator:
    """Bidirectional packet translator for one client <-> GameServer TCP stream."""

    def __init__(self, log_fn=None) -> None:
        self.log = log_fn or (lambda msg: None)
        self.s2c_buf = PacketBuffer()
        self.c2s_buf = PacketBuffer()
        self.ready_injected = False
        self.player_id = 0
        self.skill_points: dict[int, int] = {}
        self.team_info_modes: deque[int] = deque()

    def server_to_client_chunk(self, chunk: bytes) -> tuple[bytes, list[tuple]]:
        out = bytearray()
        changes = []
        for pkt in self.s2c_buf.feed(chunk):
            translated, pkt_changes = self._translate_s2c_packet(pkt)
            out.extend(translated)
            changes.extend(pkt_changes)
        return bytes(out), changes

    def client_to_server_chunk(self, chunk: bytes) -> tuple[bytes, bytes, list[str]]:
        to_server = bytearray()
        to_client = bytearray()
        notes = []
        for pkt in self.c2s_buf.feed(chunk):
            srv_bytes, cli_bytes, note = self._translate_c2s_packet(pkt)
            if srv_bytes:
                to_server.extend(srv_bytes)
            if cli_bytes:
                to_client.extend(cli_bytes)
            if note:
                notes.append(note)
        return bytes(to_server), bytes(to_client), notes

    def _translate_s2c_packet(self, pkt: bytes) -> tuple[bytes, list[tuple]]:
        size, mtype = struct.unpack_from("<HH", pkt, 0)
        if mtype == 10009:
            translated = self._s2c_selfinfo(pkt)
            changes = [(10009, 10019)]
            if not self.ready_injected:
                self.ready_injected = True
                translated += PKT_CLIENT_SERVER_READY
                changes.append(("inject", 10005))
                self.log("[shim] injected synthetic S2C 10005 (ServerReady) after MSG_SELFINFO")
            return translated, changes
        if mtype == 10022:
            return self._s2c_kitbag(pkt), [(10022, 10031)]
        if mtype == 10030:
            return self._s2c_selfproperty(pkt), [(10030, 10041)]
        if mtype == 10119:
            return self._s2c_syn_gamedata(pkt), [(10119, 10160)]
        if mtype == 10010:
            return self._s2c_objectinfo(pkt), [(10010, 10020)]
        if mtype == 10028:
            return self._s2c_skills(pkt, 10039, 8), [(10028, 10039)]
        if mtype == 10029:
            return self._s2c_skills(pkt, 10040, 12), [(10029, 10040)]
        if mtype == 10072:
            return self._s2c_relationall(pkt), [(10072, 10093)]
        if mtype == 10079:
            return self._s2c_trade_item(pkt), [(10079, 10100)]
        if mtype == 10093:
            return self._s2c_teaminfo(pkt), [(10093, 10119)]
        if mtype == 10143:
            if len(pkt) >= 28 and self.player_id is not None:
                ui_id = struct.unpack_from("<I", pkt, 4)[0]
                if ui_id == self.player_id:
                    # GameServer's CRegion::Walk broadcasts 10143 back to the sender too,
                    # which the 1.0.282 client (0x4970a9) treats as a rollback+stop.
                    return b"", [(10143, "drop_self")]
            return self._s2c_walk(pkt), [(10143, 10187)]

        new_op = S2C_OP_MAP.get(mtype)
        if new_op is not None and new_op != mtype:
            out = bytearray(pkt)
            struct.pack_into("<H", out, 2, new_op)
            return bytes(out), [(mtype, new_op)]
        if new_op is None:
            self.log(f"[shim] UNMAPPED S2C opcode={mtype} size={size} {pkt[:32].hex(' ')}")
        return pkt, []

    def _s2c_selfinfo(self, pkt: bytes) -> bytes:
        # Server 484B -> Client 1192B (0x4a8).
        if len(pkt) < 100:
            return pkt
        equip_mask = struct.unpack_from("<H", pkt, 48)[0] & 0x0FFF
        self.player_id = struct.unpack_from("<I", pkt, 52)[0]
        skill_pt = struct.unpack_from("<I", pkt, 92)[0]
        self.skill_points[self.player_id] = skill_pt

        out = bytearray(1192)
        struct.pack_into("<HH", out, 0, 1192, 10019)
        out[4:100] = pkt[4:100]
        struct.pack_into("<I", out, 100, 1)  # extra != 0 suppresses lv1 newbie stub packet
        src_idx = 0
        for bit in range(12):
            if equip_mask & (1 << bit):
                out[104 + src_idx * 72 : 176 + src_idx * 72] = item_32_to_72(
                    pkt, 100 + src_idx * 32
                )
                src_idx += 1
        return bytes(out)

    def _s2c_kitbag(self, pkt: bytes) -> bytes:
        if len(pkt) < 24:
            return pkt
        ui_id, bag_num, up_step = struct.unpack_from("<3I", pkt, 4)
        bag_idx, start_slot = pkt[16], pkt[17]
        flag = struct.unpack_from("<I", pkt, 20)[0]
        if up_step != 0:
            out = bytearray(pkt)
            struct.pack_into("<H", out, 2, 10031)
            return bytes(out)

        # Unpack non-empty 32B server items into sparse 0..23 slots, then emit fixed 12-slot pages.
        slots = [b"\xff\xff\xff\xff" + b"\x00" * 68 for _ in range(24)]
        item_i = 0
        for bit in range(24):
            if flag & (1 << bit):
                slots[bit] = item_32_to_72(pkt, 24 + item_i * 32)
                item_i += 1

        out = bytearray()
        for half in (0, 12):
            if half == 12 and not (flag >> 12):
                break
            page = bytearray(888)
            struct.pack_into(
                "<HH3IBBBBI",
                page,
                0,
                888,
                10031,
                ui_id,
                bag_num,
                up_step,
                bag_idx,
                start_slot + half,
                0,
                0,
                (flag >> half) & 0x0FFF,
            )
            for j in range(12):
                page[24 + j * 72 : 96 + j * 72] = slots[half + j]
            out.extend(page)
        return bytes(out)

    def _s2c_selfproperty(self, pkt: bytes) -> bytes:
        if len(pkt) < 116:
            return pkt
        obj_client = object_data_112_to_132(pkt[4:116])[:128]
        return struct.pack("<HH", 132, 10041) + obj_client

    def _s2c_syn_gamedata(self, pkt: bytes) -> bytes:
        if len(pkt) < 196:
            return pkt
        ui_id = struct.unpack_from("<I", pkt, 4)[0]
        store_num, bag_num = struct.unpack_from("<2I", pkt, 8 + 68)
        skill_pt = self.skill_points.get(ui_id, 0)

        out = bytearray(236)
        struct.pack_into("<HHI", out, 0, 236, 10160, ui_id)
        out[8:140] = object_data_112_to_132(pkt[8:120])
        out[140:216] = pkt[120:196]  # 19-dword StatusData matches identically
        struct.pack_into("<3IB", out, 216, store_num, bag_num, skill_pt, 0)
        return bytes(out)

    def _s2c_objectinfo(self, pkt: bytes) -> bytes:
        if len(pkt) < 244:
            return pkt
        db_id, ui_id, ui_type = struct.unpack_from("<3I", pkt, 4)
        map_id = struct.unpack_from("<H", pkt, 16)[0]
        x, y, z, fatan = struct.unpack_from("<4f", pkt, 20)
        c_stall = pkt[36]
        stall_text = pkt[38:102]
        name32 = pkt[102:134]
        max_hp, hp = struct.unpack_from("<2I", pkt, 136)
        gender, camp, belief, cls, hair, pupil = pkt[144:150]
        lv = struct.unpack_from("<I", pkt, 152)[0]
        status_dis = pkt[160:240]
        equip_flag = struct.unpack_from("<H", pkt, 240)[0] & 0x0FFF

        count = bin(equip_flag).count("1")
        total = max(628, 344 + count * 72)
        out = bytearray(total)
        struct.pack_into(
            "<HHBBH5I4f",
            out,
            0,
            total,
            10020,
            ui_type & 0xFF,
            camp,
            map_id,
            ui_id,
            lv,
            0,
            hp,
            max_hp,
            x,
            y,
            z,
            fatan,
        )
        if ui_type == 17:
            npc_name = resolve_npc_name(map_id, name32)[:63]
            out[44 : 44 + len(npc_name)] = npc_name
        else:
            out[44:76] = name32
        struct.pack_into("<IBBBBBBB", out, 108, db_id, gender, belief, cls, hair, pupil, c_stall, 0)
        struct.pack_into("<H", out, 126, equip_flag)
        out[132:212] = status_dis
        out[212:276] = stall_text
        for i in range(count):
            out[344 + i * 72 : 416 + i * 72] = item_32_to_72(pkt, 244 + i * 32)
        return bytes(out)

    def _s2c_skills(self, pkt: bytes, target_op: int, stride: int) -> bytes:
        if len(pkt) < 10:
            return pkt
        ui_id, count = struct.unpack_from("<IH", pkt, 4)
        need = 12 + count * stride
        out = bytearray(need)
        struct.pack_into("<HHIHH", out, 0, need, target_op, ui_id, count, 0)
        body = pkt[12:]
        copy_len = min(len(body), count * stride)
        out[12 : 12 + copy_len] = body[:copy_len]
        return bytes(out)

    def _s2c_relationall(self, pkt: bytes) -> bytes:
        if len(pkt) < 10:
            return pkt
        ui_id = struct.unpack_from("<I", pkt, 4)[0]
        rel_type, count = pkt[8], pkt[9]
        total = 12 + count * 32
        out = bytearray(total)
        struct.pack_into("<HHIBB", out, 0, total, 10093, ui_id, rel_type, count)
        for i in range(count):
            src_off = 10 + i * 28
            if src_off + 28 > len(pkt):
                break
            dst_off = 12 + i * 32
            out[dst_off : dst_off + 18] = pkt[src_off : src_off + 18]
            out[dst_off + 22 : dst_off + 30] = pkt[src_off + 18 : src_off + 26]
        return bytes(out)

    def _s2c_trade_item(self, pkt: bytes) -> bytes:
        """Repack 108-byte server MSG_TRADE_ITEM (10079) into 120-byte client 10100."""
        if len(pkt) < 108:
            out = bytearray(pkt)
            struct.pack_into("<H", out, 2, 10100)
            return bytes(out)
        ui_id, index, base_id = struct.unpack_from("<Iii", pkt, 4)
        appends = struct.unpack_from("<5i", pkt, 16)
        append_lv, base_lv = pkt[36], pkt[37]
        overlap = struct.unpack_from("<I", pkt, 40)[0]
        raw_name = pkt[44:108].split(b"\x00", 1)[0][:31]
        equip = pack_client_equip(base_id, appends, base_lv, append_lv, 0, overlap)
        return struct.pack("<HHIii32s", 120, 10100, ui_id, index, base_id, raw_name) + equip

    def _s2c_teaminfo(self, pkt: bytes) -> bytes:
        """Restore client ucMode byte (70-byte server 10093 -> 72-byte client 10119)."""
        uc_mode = self.team_info_modes.popleft() if self.team_info_modes else 1
        if len(pkt) < 70:
            out = bytearray(pkt)
            struct.pack_into("<H", out, 2, 10119)
            return bytes(out)
        return struct.pack("<HHB", 72, 10119, uc_mode) + pkt[4:70] + b"\x00"

    def _c2s_teaminfo(self, pkt: bytes) -> bytes:
        """Strip client ucMode byte (72-byte client 10119 -> 70-byte server 10093)."""
        if len(self.team_info_modes) >= 32:
            self.team_info_modes.popleft()
        self.team_info_modes.append(pkt[4])
        return struct.pack("<HH", 70, 10093) + pkt[5:71]

    def _s2c_walk(self, pkt: bytes) -> bytes:
        if len(pkt) < 28:
            return pkt
        ui_id = struct.unpack_from("<I", pkt, 4)[0]
        x, z, fatan, state = struct.unpack_from("<3fI", pkt, 12)
        return struct.pack("<HHHH3f", 20, 10187, ui_id & 0xFFFF, state & 0xFFFF, x, z, fatan)

    def _translate_c2s_packet(self, pkt: bytes) -> tuple[bytes, bytes, str]:
        size, mtype = struct.unpack_from("<HH", pkt, 0)
        if mtype == 10006:
            self.ready_injected = False
            return pkt, b"", f"C2S {mtype}->{mtype} reset ready"

        if mtype in C2S_CLIENT_ONLY:
            if mtype == 10015 and len(pkt) >= 8:
                return b"", pkt[:8], "C2S 10015 ping -> echoed pong"
            if mtype == 10303:
                now = int(time.time()) & 0xFFFFFFFF
                reply = struct.pack("<HH3I", 16, 10303, now, now, 0)
                return b"", reply, "C2S 10303 time -> replied"
            return b"", b"", f"C2S {mtype} ({size}B) dropped (client-only)"

        if mtype == 10187 and len(pkt) >= 20:
            map_id, state, x, z, fatan = struct.unpack_from("<HH3f", pkt, 4)
            uid = self.player_id or 0
            srv = struct.pack("<HHIHH3fI", 28, 10143, uid, map_id, 0, x, z, fatan, state)
            return srv, b"", f"C2S 10187->10143 walk ({x:.1f},{z:.1f} st={state})"

        if mtype == 10066 and len(pkt) >= 28:
            npc_id, state = struct.unpack_from("<2I", pkt, 4)
            opt0, opt1, opt2 = struct.unpack_from("<3I", pkt, 12)
            srv = struct.pack("<HH5I", 24, 10049, npc_id, state, opt0, opt1, opt2)
            return srv, b"", f"C2S 10066->10049 npc={npc_id}"

        if mtype == 10119 and len(pkt) >= 71:
            return self._c2s_teaminfo(pkt), b"", f"C2S 10119->10093 teaminfo mode={pkt[4]}"

        srv_op = C2S_OP_MAP.get(mtype)
        if srv_op is None:
            self.log(f"[shim] DROPPED unmapped C2S opcode={mtype} size={size} {pkt[:32].hex(' ')}")
            return b"", b"", ""
        if srv_op != mtype:
            out = bytearray(pkt)
            struct.pack_into("<H", out, 2, srv_op)
            return bytes(out), b"", f"C2S {mtype}->{srv_op}"
        return pkt, b"", f"C2S {mtype}"
