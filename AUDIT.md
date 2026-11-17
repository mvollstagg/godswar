# GodsWar 1.0.282 US Client Revival — Audit Log

This file records investigation findings, hypotheses, changes, and test results chronologically.

---

## Session 1 — Initial Investigation
- **Timestamp:** 2026-11-17 15:02:30 +03:00
- **Agent:** Antigravity
- **Model:** Gemini 4 Hydrogen
- **Reasoning / Effort:** XHigh (Extra High)
- **Context / Initial State:**
  - User added a `1.0.262`-numbered (`1.0.282` in-game) US client (`Client/`) on top of a legacy 2007/2008 GodsWar Docker server stack (`LoginServer.exe`, `DBServer.exe`, `GameServer.exe` under Wine + MariaDB 10.11).
  - Previous work modified `docker/tools/login_shim.py`, `docker/server/entrypoint.sh`, `docker/tools/login_probe.py`, `README.md`, and created helper analysis scripts in `docker/tools/` (`build_opcode_map.py`, `opcode_map.py`, `dispatch_map.py`, `find_ready.py`, `msg_strings.py`, `server_enum.py`, `server_values.py`).
  - Current symptom: User can log in, see the server list, select/enter character (`fenasal`), enter `Athens' Suburb (76,-173)` with HP/MP bar and welcome chat banner, **but a modal `"Loading..."` dialog remains stuck in the center of the screen** and character shows level `0` (or incomplete init).

### Initial Observations & Opinions
1. From `docker/tools/login_shim.py` and `game_shim.log`:
   - `login_shim.py` acts as a standalone login front-end on port 5999 and as a transparent/opcode-translating proxy (`--no-inject`) on port 6001 -> 6000 (`GameServer`).
   - Right now, `translate_server_to_client()` only translates `server -> client` opcodes using a partial `OPCODE_MAP` of 12 entries (`10009->10019`, `10022->10031`, `10015->10018`, etc.), **and performs zero translation on `client -> server` opcodes**.
   - In `game_shim.log`, after `MSG_LOGIN_GAMESERVER` (10000) and character select `10006` (`MSG_SELECT_ROLE`?), the server sends `10002` (`MSG_ROLE_INFO`), `10009->10019` (`MSG_SELFINFO`), and `10022->10031` (`MSG_KITBAG`), and then the client starts sending `10303`, `10054`, `10015` (repeatedly!), `10304`, `10227`, `10111`, `10185`.
   - **Hypothesis A (Client -> Server Opcodes):** The client is sending `CLIENT_READY` (or equivalent ready/load-complete / ping / heartbeat packet) using the 1.0.282 US client opcode (likely one of `10015`, `10054`, `10303`, etc.), which the 2007 `GameServer` either ignores or misinterprets because `client -> server` opcodes are NOT translated! Look at `GameServer.exe` vs `Client/GodsWar.exe`! Also, the client's `"Loading..."` popup only closes when the server sends the remaining world-entry packets (or acknowledges `MSG_CLIENT_READY` / sends `MSG_SCENE_CHANGE` / `MSG_SELFPROPERTY` / `MSG_SKILL` / `MSG_ACTIVESKILL` / `MSG_PASSIVESKILL` / `MSG_SYS_DATA` / `MSG_OBJECTINFO` / `MSG_ENTER_GAME` / etc.) — OR `MSG_SELFINFO` / `MSG_KITBAG` payload struct sizes/layouts differ between 2007 server and 1.0.282 client!
   - **Hypothesis B (Server -> Client Opcode & Payload Mismatch):** Look at the screenshot: HP is `1521/1521`, MP is `507/507`, Level is **`0`**! And `10009` (`MSG_SELFINFO`) was 484 bytes, and `10022` (`MSG_KITBAG`) was 88 bytes, and NO `MSG_SELFPROPERTY`, `MSG_PARAMATER`, `MSG_SKILL`, `MSG_SCENE_CHANGE` or `MSG_ENTER` was sent or maybe `GameServer` is waiting for `MSG_CLIENT_READY` from the client! Wait: why does `GameServer` wait? In `find_ready.py` someone was already searching for `0x2717` (`10007 CLIENT_READY`)! And notice the client NEVER sent `10007` — it sent `10303` (16B), `10054` (40B), `10015` (8B)! Wait: what is `0x271f` (`10015`)?! Could `10015` in the US client be `MSG_TIME` (heartbeat) OR `MSG_CLIENT_READY`? And what is `10303`, `10054`, `10227`, `10111`, `10185`?

### Next Steps
- Inspect `MsgTypes.h` and `MsgDefine.h` in `GW 2.0/Server Files (Source)/`.
- Inspect `GameServer` `OnLogin` / `OnSelectRole` / `OnClientReady` flow in C++ source (`GW 2.0/Server Files (Source)/`).
- Inspect `Client/GodsWar.exe` and `Client/GodsWar.map` (symbols!!) to build a COMPLETE bidirectional opcode table AND verify exact packet struct sizes/layouts!

---

## Session 1 (Continued) — Binary Reverse Engineering & Root Cause Discovery
- **Timestamp:** 2026-11-17 15:28:24 +03:00
- **Agent:** Antigravity
- **Model:** Gemini 4 Hydrogen
- **Reasoning / Effort:** XHigh (Extra High)
- **Actions & Findings:**
  1. **Checked `Client/GodsWar.map` vs `Client/GodsWar.exe` & `GW 2.0/Server Files (Source)` vs `/opt/gw/gameserver/GameServer.exe`:**
     - `Client/GodsWar.map` addresses do **not** match `Client/GodsWar.exe` (`MD5 A7A904F43C07CEFCF519280704B40A5E`, identical to `/tmp/GodsWar.exe` in `godswar-server-1`).
     - Similarly, `GW 2.0/Server Files (Source)` is an older C++ source revision than the compiled `/opt/gw/gameserver/GameServer.exe` (e.g., `MSG_OBJECTINFO` is `628` bytes / `0x274` in compiled `GameServer.exe` vs a different size in the C++ header). All opcodes and struct offsets must be verified directly in `GodsWar.exe` and `GameServer.exe`.
  2. **Root Cause #1 — `"Loading..."` Deadlock (`10005` vs `10007`):**
     - Disassembled `CMainModule::ClientReady()` (`0x43e510`) in `Client/GodsWar.exe`: it displays `"Loading"` (`0x7cab14`) whenever `BYTE [0x13e59d9]` (`ServerReady`) is `0` OR `BYTE [0x13e59d8]` (`ClientReady`) is `0`.
     - `0x47301f` sets `[0x13e59d8] = 1`, while the **only** instruction setting `[0x13e59d9] = 1` (aside from constructor/reset zeroing at `0x43c0a4`) is **`0x4952cb` — the client receive handler for opcode `10005` (`0x2715`)**.
     - `GameServer.exe` never sends opcode `10005`; instead `Handle_EnterGame` stops after `MSG_SELFINFO` + `MSG_KITBAG` and `Handle_ClientReady` (`0x40cc80`) waits for client->server `MSG_CLIENT_READY` (`10007`, 32B) before sending `MSG_SELFPROPERTY`, `MSG_SYN_GAMEDATA`, `MSG_OBJECTINFO`, skills, relations, and altars. Both sides wait on each other (classic handshake deadlock).
  3. **Root Cause #2 — Unmapped Bidirectional Opcodes & Differing Dispatcher Tables:**
     - Found 5 client receive dispatchers in `GodsWar.exe` (`0x4914e0` `NetSystemMessage`, `0x497010` movement, `0x48fde0` skill/team/marriage, `0x487470` world/combat/item/NPC/quest, `0x470ce0` repetition) and mapped client->server opcodes via `0x13b7420` send call sites, vs `GameServer.exe`'s exact `MsgTypes.h` table (`10000..10143` at `0x40c000`).
     - Critical client->server collisions currently forwarded un-translated: client `10015` (8B heartbeat/ping) & `10304` (8B) & `10303` (`MSG_SERVERTIME` 16B) & `10054` (`MSG_SETQUICKEQUIP` 40B — collides with server `10054` = `MSG_NPCSELL`!).
  4. **Root Cause #3 — Packet Struct & Item Size Mismatches (`32B` server item vs `72B` client item):**
     - `Item` struct is `32` bytes on server vs `72` bytes (`SEquip`) on `1.0.282` client.
     - `MSG_SELFINFO` (`10009 -> 10019`): `484B` server (100B header + 12*32B equips) vs `1192B` (`0x4a8`) client (100B header + `DWORD extra@100` + `15 * 72B` equips@104 + 2 `DWORD`s@1184,1188).
     - `MSG_KITBAG` (`10022 -> 10031`): `24B` header + `N * 32B` server vs `24B` header + `12 * 72B` client.
     - `MSG_SELFPROPERTY` (`10030 -> 10041`): `116B` (`Head + 28 dwords`) server vs `128B` (`32 dwords` -> `CPlayer+0x1fc`) client.
     - `MSG_SYN_GAMEDATA` (`10119 -> 10160`): `204B` (`0xcc`) server vs client `0x49068c` reading `33 dwords` + `19 dwords` + `[ebx+0xdc..0xe8]`.
     - `MSG_OBJECTINFO` (`10010 -> 10020`): Both `628B` (`0x274`), but internal field offsets are completely reordered (`uiID`, `uiType`, coords, `cName`, `SEquip`).
  5. **Infrastructure Bug in `docker/tools/login_shim.py`:**
     - `translate_server_to_client` operates directly on raw `recv(4096)` chunks without TCP packet framing, and `server_to_client` increments `to_client.pos += len(data)` instead of translated length. Full per-stream `<HH` packet framing is required on both directions, especially when packets resize (`484B -> 1192B`).
     - Verified `docker/server/Dockerfile` copies `docker/tools/login_shim.py` into `/usr/local/bin/login_shim.py`, so `docker compose up -d --build server` is needed to persist changes cleanly.
- **Opinion:** Rewriting `game_shim` with full bidirectional packet framing, `10005` handshake injection after initial `MSG_SELFINFO`+`MSG_KITBAG`, complete C2S/S2C opcode translation, and exact binary-verified struct translation should immediately break the `"Loading..."` deadlock, populate level/stats/equips/inventory/skills/NPCs/monsters, and unlock movement, combat, NPCs, quests, and chat.

---

## Session 2 — Exact Struct Mapping, Translator Implementation & Verification
- **Timestamp:** 2026-11-17 15:28:24 +03:00
- **Agent:** Antigravity
- **Model:** Gemini 4 Hydrogen
- **Reasoning / Effort:** XHigh (Extra High)
- **Plan:**
  1. Disassemble and compare exact binary layouts in `GameServer.exe` vs `GodsWar.exe` for all world-entry and core gameplay packets.
  2. Implement framed bidirectional opcode + struct translation and `10005` injection in `docker/tools/login_shim.py`.
  3. Extend/run automated probe verification and live container build + test.

---

## Session 3 — Monster.ini Crash Fix, Full-World NPC/Data Audit & E2E Verification
- **Timestamp:** 2026-11-17 16:55:00 +03:00
- **Agent:** Antigravity
- **Model:** Gemini 4 Hydrogen
- **Reasoning / Effort:** XHigh (Extra High)
- **Context & Live Symptoms:**
  - User confirmed the `"Loading..."` modal deadlock is resolved (`Umut` enters `Athens' Suburb` with `HP 1521/1521`, `MP 507/507` visible).
  - While walking in-game, client crashed with two chained dialogs:
    1. `section[C_elite_greecewarrior_008] not found in ./Localization/en_us/Monster/Athens_Newbie/Monster.ini!`
    2. `SceneManager::create_entity` (`Create mesh object failed with name monster000001..`).
  - Also noticed in the crash screenshot that the portrait `<Rank>` oval still showed `0` while `HP`/`MP` updated to `1521`/`507`.
- **Investigations & Root Causes Found:**
  1. **Missing `Monster.ini` Sections Across 6 Scenes:**
     - Compared all 24 server `Scene/*/Monster.ini` files (`468` total spawned monster types) against `Client/Localization/en_us/Monster/*/Monster.ini`.
     - `GameServer` (`OBJECTINFO 10010->10020`) sends `C_elite_greecewarrior_008` in `Athens_Newbie`, which was absent from the `1.0.282` US client's `Athens_Newbie/Monster.ini` (`0x646ed1` -> `section[%s] not found in %s!`), leaving the mesh name empty and throwing in `SceneManager::create_entity`.
     - Across all 24 scenes, 18 monster sections were missing across 6 client `Monster.ini` files.
  2. **NPC Truncation & 17-Scene GBK Localization Gap (`NPC.INI` vs `OBJECTINFO` `name32`):**
     - Verified `GameServer.exe` `OBJECTINFO` (`10010`) carries a 32-byte `name32` field (`pkt[102:134]`), whereas `GodsWar.exe` `10020` (`0x492dfb` -> `0x44ce90`) reads a 64-byte `cName` string (`pkt[44:108]`).
     - In `Scene/*/NPC00.INI` (`324` total NPC spawns across `18` scenes), only `Athens_Newbie` had been pre-converted to English; the other 17 scenes still send GBK Chinese `Name=` strings (`307` spawns), which previously relied on `293` raw GBK sections appended to `Client/Localization/en_us/Settings/Sys/NPC.INI` (`862` sections vs `569` original US sections in `NPC.INI.orig`).
     - Worse, 21 client US `NPC.INI` section keys are `32–36` bytes long; `GameServer.exe` truncates them in `name32`, causing live `10020` packets for `Athens_Newbie` NPCs `5113` (`Athens_Newbie_008_AthenianCivili`, missing full `...lian1`) and `5116` (`Athens_Newbie_011_SpartanCivilia`, missing full `...lian1`) not to match `[section]` keys in `NPC.INI`.
     - Confirmed `Script` suffixes map all `324/324` server NPCs uniquely (`0` unresolved, `0` collisions by `(map_id, name32)`) to clean original US sections in `NPC.INI.orig` (max length `36B <= 63B`), and `Handle_TalkNPC` (`10048`) looks up NPCs by `NPCID` (`ui_id`), not name.
  3. **Binary Layout Audit on `10041`, `10160`, `10019`, `10020`, `Skill.ini`, `Status.ini`, `Magic.ini`:**
     - Disassembled `GodsWar.exe` `MSG_SELFPROPERTY` (`0x492652`), `MSG_SYN_GAMEDATA` (`0x49068c`), `MSG_SELFINFO` (`0x4927f0`), `MSG_OBJECTINFO` (`0x492df1`), and HUD update (`0x577930` / `0x577b10`), confirming handlers use `%ebx = pkt - 4` (`cmpw $0x2739, 0x6(%ebx)`): `10041`, `10160`, and `10020` field offsets in `gw_protocol.py` match `GodsWar.exe` verbatim (`rank=1` at `[96]`/`[100]`, `CPlayer::vtable[+0x20] = 0x45d6d0` reading `[CPlayer+0x258]`).
     - Checked `Skill.ini` (`80` server vs `81` client, `0` missing), `Status.ini` (`75` server vs `124` client, `0` missing), and `Magic.ini` (`988` server vs `1084` client, only `[5009..5012]` absent on client).
- **Changes Made:**
  1. **Client `Monster.ini` Patches (with `.orig` backups preserved):**
     - `Client/Localization/en_us/Monster/Athens/Monster.ini` (`13 -> 15`: +`C_elite_greecewarrior_005`, `C_elite_greecewarrior_008`)
     - `Client/Localization/en_us/Monster/Athens_Newbie/Monster.ini` (`22 -> 25`: +`C_elite_greecewarrior_003`, `C_elite_greecewarrior_008`, `C_elite_mage_001`)
     - `Client/Localization/en_us/Monster/Nemea_1/Monster.ini` (`35 -> 37`: +`B_elite_greecewarrior_011`, `B_elite_greecewarrior_012`)
     - `Client/Localization/en_us/Monster/Parnitha_1/Monster.ini` (`36 -> 37`: +`B_elite_greecewarrior_008`)
     - `Client/Localization/en_us/Monster/Sparta/Monster.ini` (`13 -> 15`: +`C_elite_greecewarrior_009`, `C_elite_greecewarrior_012`)
     - `Client/Localization/en_us/Monster/Sparta_Newbie/Monster.ini` (`22 -> 30`: +`A_normal_skeleton_003`..`007`, `C_elite_greecewarrior_004`, `C_elite_greecewarrior_009`, `C_elite_mage_004`)
  2. **NPC Localization & Truncation Fix:**
     - Created `docker/tools/gw_npc_map.py` (`NPC_BY_MAP_NAME` `309` entries, `NPC_UNIQUE_NAME` `283` entries, `resolve_npc_name(map_id, name32)`).
     - Updated `docker/tools/gw_protocol.py` (`_s2c_objectinfo`) to resolve NPC (`ui_type == 17`) names into the full 64-byte `out[44:108]` slot.
     - Updated `docker/server/Dockerfile` to copy `/usr/local/bin/gw_npc_map.py` and rebuilt/restarted `godswar-server-1` (`docker compose up -d --build server`).
- **Verification Results (all run after final edits):**
  - Cross-scene monster check: `checked_scenes=24`, `total_spawned_types=468`, `remaining_missing=0`.
  - Asset reference check across all 49 `Monster.ini` files: `checked_monster_ini_files=49`, `checked_asset_refs=1946`, `bad_asset_refs=0`.
  - Unit check (`gw_npc_map` + `GameSessionTranslator._translate_s2c_packet`): `UNIT_OK by_map=309 uniq=283` (verified every mapped target exists in `NPC.INI.orig`, cross-map GBK disambiguation, and `>31B` expansion).
  - Live E2E probe on rebuilt `godswar-server-1` (`test`/`Umut`): `10006 -> [(10019, 1192), (10005, 4), (10031, 888)]`; `10007 -> 10041(132B, rank=1, hp=1521, mp=507), 10160(236B, rank=1, hp=1521/1521, mp=507/507), 10020(25: 13 NPCs, 12 mobs), 10074(12), 10077(12), 10040(1), 10039(1)`; confirmed `5113=Athens_Newbie_008_AthenianCivilian1` & `5116=Athens_Newbie_011_SpartanCivilian1`; `WALK 10187 -> (10187, 20)`; `TALKNPC 5118 -> (10064, 48)`; clean `10185` exit; `E2E_ALL_CHECKS_PASSED`.
  - Log check on `/opt/gw/logs/game_shim.log` (`L5098..5348`, `251` lines): `error_count=0` (zero tracebacks, `struct.error`, or unmapped S2C opcodes).
- **Engineering Opinion:**
  - The `"section[...] not found"` monster crash is completely resolved across every zone in the game, and full-name US NPC resolution ensures seamless zone transitions (`Athens_Newbie` -> `Athens`, `Sparta_Newbie` -> `Sparta`, etc.) with clean English NPC names and outfits instead of GBK mojibake or 32-byte truncation.
  - Ready for live client playtesting via `Client/play.cmd` (verify walking into elites in `Athens' Suburb`, portal travel to `Athens`, NPC dialogs, combat, and whether `<Rank>` badge updates to `1` once unblocked frames run).

---

## Session 4 — Walk Stuttering Fix & Second Local Account (`test2`)
- **Timestamp:** 2026-11-17 20:05:30 +03:00
- **Agent:** Antigravity
- **Model:** Gemini 4 Hydrogen
- **Reasoning / Effort:** XHigh (Extra High)
- **User Request & Live Symptoms:**
  - User confirmed world entry and exploration work cleanly, but reported character walking stutters ("like its stucking but walking" while animation plays).
  - User also requested a second local account: `test2` / `123456`.
- **Investigations & Root Cause:**
  1. **Live Log Correlation (`/opt/gw/logs/game_shim.log`, `L5348..8385`):**
     - During the user's play session, `GodsWar.exe` sent `341` `C2S 10187 -> 10143` walk packets (`st=2` while walking, `st=0` on stop), and `GameServer.exe` echoed **`343`** `S2C 10143 -> 10187` packets back to the sender (`ui_id = 0`).
  2. **Binary Disassembly (`GameServer.exe` vs `Client/GodsWar.exe`):**
     - In `GameServer.exe`, `Handle_Walk` (`0x40d1f0`, opcode `0x279f = 10143`) calls `CRegion::Walk` (`0x47da00`), which broadcasts `msg_walk` (`0x502554`) via `0x46c500 -> 0x46c440` with **`pExcludePlayer = NULL`** (`xor %ebx, %ebx` at `0x47dab1`), sending every walk packet back to the sender (`self`) too. Only `0x46b57e` (startup init) and `0x47da90` reference `0x502554` in `GameServer.exe`.
     - In `GodsWar.exe`, the movement dispatcher `0x497010` handles `0x27cb` (`10187`) at `0x497040`: for other entities (`ui_id != self.uiID`) it smoothly interpolates (`0x457a50`), **but when `ui_id == g_pPlayer->uiID` (`0x497068: je 0x4970a9`)**, it calls **`0x4502d0(x, z)`** (snapping player coordinates back to the echoed position) followed by **`0x4504a0(0)`** (`SetState(0 = IDLE)`, interrupting the walk state every ~100ms).
  3. **Second Account Setup (`accounts` & `godswar` DB):**
     - `accounts.SP_SELECT_ACCOUNTS_INFO` authenticates against `accounts.gamearea_info`, while `login_shim.py` runs in `--standalone` mode on `5999` and `godswar.character_base` keys characters by `accounts` (`DigerUmu`, `id=3798`, already present under `test2`).
- **Changes Made:**
  1. **Self-Walk Echo Suppression (`docker/tools/gw_protocol.py`):**
     - In `GameSessionTranslator._translate_s2c_packet`, suppress `S2C 10143` (`return b"", [(10143, "drop_self")]`) when `self.player_id is not None` and `ui_id == self.player_id`, while preserving `S2C 10143 -> 10187` (`20B`) for remote players (`ui_id != self.player_id`).
     - In `GameSessionTranslator._translate_c2s_packet` (`mtype == 10187`), guarded `self.player_id or 0` before packing `10143`.
     - Rebuilt and restarted `godswar-server-1` (`docker compose up -d --build server`).
  2. **Account `test2` Created in `godswar-db-1` (`accounts`):**
     - Inserted `test2` / `123456` into `accounts_info` (`id=133`), `gamearea_info` (`GID=8234`), and `accounts_register` (`AccID=127`).
- **Verification Results (run after final edits & rebuild):**
  - **Unit test (`WALK_AND_NPC_UNIT_OK`):** Verified `S2C 10143` with `ui_id == 5` (`self.player_id = 5`) returns `b""` + `[(10143, 'drop_self')]`; `S2C 10143` with `ui_id == 6` returns `20B` `S2C 10187`; `C2S 10187` emits `28B` `10143` with `self.player_id = 5`; `gw_npc_map` targets all exist in `NPC.INI.orig`.
  - **Live E2E probe on rebuilt `godswar-server-1` (`ALL_E2E_WALK_AND_TEST2_PASSED`):**
    - `test` / `123456` (`Umut`, `ui_id=0`): `10006 -> [(10019, 1192), (10005, 4), (10031, 888)]`; `10007 -> 55 pkts, npcs=13, NPC_5113=Athens_Newbie_008_AthenianCivilian1`; sending `10187` (`st=2, st=2, st=0`) produced `WALK_SELF_ECHOES=[]` (`drop_self_count=3` confirmed in `game_shim.log`); `TALKNPC 5118 -> [(10064, 48)]`; clean `10185` logout.
    - `test2` / `123456`: authenticated on `5999` and `GameServer` `10000 -> 10002` (`356B`, `roles_ucNum=1`, `DigerUmu`).
  - **Shim log check (`/opt/gw/logs/game_shim.log` `L8644..8810`, `167` lines):** `error_count=0`, `drop_self_count=3`.
- **Engineering Opinion:**
  - Dropping `S2C 10143` self-echoes eliminates the local `SetPos` + `SetState(0)` rubberband loop without modifying `GameServer.exe`, while leaving server movement tracking and remote player walk broadcasts (`test` <-> `test2`) fully intact.
  - Noted in `L5348..8385` logs that a few optional C2S actions were dropped (`10065` `8B`, `10078` `12B`, `10108` `52B`, `10172`/`10173` `8B`) — likely NPC/Quest/Mall sub-actions to inspect next if you test quests or shops and find any button unresponsive.

---

## Session 5 — Party (`TEAM`) & Trade (`TRADE`) Protocol Fixes
- **Timestamp:** 2026-11-17 21:08:17 +03:00
- **Agent:** Antigravity
- **Model:** Gemini 4 Hydrogen
- **Reasoning / Effort:** XHigh (Extra High)
- **User Request & Live Symptoms:**
  - Confirmed two-client visibility and walking work cleanly (`test`/`Umut` and `test2`/`DigerUmu`), and reported two remaining multiplayer issues:
    1. **Party requests fail:** *"i tried to send party request but didnt worked. i didnt see any party thing."*
    2. **Trade item invisibility & visual desync:** Putting a potion into Trade did not appear in the partner's window; after both accepted, the potion disappeared from the sender's inventory UI without moving to the partner's inventory (relogging revealed the potion was still in the sender's inventory).
- **Investigations & Root Cause (`GameServer.exe`, `Client/GodsWar.exe`, `/opt/gw/logs/game_shim.log`):**
  1. **Party (`TEAM`, server `10091..10102` vs 1.0.282 client `10117..10128`):**
     - **Missing C2S opcode routes (`docker/tools/gw_protocol.py` `C2S_OP_MAP`):** Client `1.0.282` shifts team opcodes `+26`, but `C2S_OP_MAP` only mapped `10117 -> 10091`, `10121 -> 10095`, and `10126 -> 10100`. It omitted `10119 -> 10093` (`_MSG_TEAM_INFO`), `10120 -> 10094` (`_MSG_TEAM_ADD`), `10122 -> 10096` (`_MSG_TEAM_REPLACELEADER`), `10123 -> 10097` (`_MSG_TEAM_DISSOLVE`), and `10124 -> 10098` (`_MSG_TEAM_LEAVE`). In the live capture, the client sent `C2S 10119` (`72B`) to query party state before inviting and `C2S 10120` (`136B`) when the invitee accepted, and the shim dropped both. Disassembly of `GameServer.exe`'s dispatch table (`0x504f98..0x505024`) also confirmed `10100` is `Handle_TeamReject` (`0x40fff0`), matching `10126 -> 10100`.
     - **`MSG_TEAMINFO` (`10119 <-> 10093`) struct mismatch:** Client `1.0.282` (`0x5800cc`, `0x4ca211`, `0x490c7d`) sends/expects **`72B`** with a leading `BYTE ucMode` (`+4`: `1` or `2`): `Head(4) + ucMode(1) + chAimName[64](+5) + bIsAimInTeam(1)(+69) + bIsSelfInTeam(1)(+70) + pad(1)(+71)`. `GameServer.exe` `Handle_TeamInfo` (`0x410220`) reads/writes **`70B`** without `ucMode`: `Head(4) + chAimName[64](+4) + bIsAimInTeam(1)(+68) + bIsSelfInTeam(1)(+69)`.
  2. **Trade (`TRADE`, server `10077..10081` vs 1.0.282 client `10098..10102`):**
     - **Missing C2S add-item mapping:** `C2S 10101 -> 10080` (`_MSG_TRADE_ADDITEM`, `84B`, verified at `GameServer.exe` `0x46b915` and Client `0x5852a8`) was omitted from `C2S_OP_MAP` and dropped by the shim. `GameServer.exe` therefore completed a 0-item trade on Accept while the sender's client UI had already locally removed the item from the bag slot, producing the exact "vanished until relog" symptom.
     - **`MSG_TRADE_ITEM` (`10079 -> 10100`) struct mismatch:** `GameServer.exe` `Trade::SetItem` (`0x46b927`) broadcasts **`108B`** (`Head(4), uiID(4)@4, Index(4)@8, BaseID(4)@12, Append[5](20)@16..35, AppendLevel(1)@36, BaseLevel(1)@37, pad(2), Overlap(4)@40, chName[64]@44..107`), whereas Client `1.0.282` (`0x48ac7e..0x48ad5a`) expects **`120B`** (`Head(4), uiID(4)@4, Index(4)@8, BaseID(4)@12, chName[32]@16..47, SEquip(72B)@48..119`).
     - **`C2S 10108` (`52B`):** Sent by the `1.0.282` client during bag-state delta checks (`0x4f9260..0x4f9353`); on `GameServer.exe`, opcode `10108` (`0x4109a0`) is an unrelated server handler, so adding `10108` to `C2S_CLIENT_ONLY` drops it cleanly without log spam or collision.
  3. **Container Build Fix (`docker/server/Dockerfile`):** Because Git on Windows had `core.autocrlf=true`, rebuilding `godswar-server` copied CRLF line endings into `/usr/local/bin/entrypoint.sh` (`[FATAL tini (7)] exec /usr/local/bin/entrypoint.sh failed: No such file or directory`). Fixed in `Dockerfile` by stripping `\r` during image build.
- **Changes Made:**
  - `docker/tools/gw_protocol.py`:
    - Added `pack_client_equip` helper (reused by both `item_32_to_72` and `_s2c_trade_item`).
    - Added `10101: 10080` (`MSG_TRADE_ADDITEM`) and complete Team mappings (`10119: 10093`, `10120: 10094`, `10122: 10096`, `10123: 10097`, `10124: 10098`, clarified `10126: 10100`) to `C2S_OP_MAP`, and added `10108` to `C2S_CLIENT_ONLY`.
    - Added per-session `team_info_modes` `deque[int]` in `GameSessionTranslator` with `_c2s_teaminfo` (`72B -> 70B`, preserving `ucMode`) and `_s2c_teaminfo` (`70B -> 72B`, restoring `ucMode`).
    - Added `_s2c_trade_item` (`10079` `108B` -> `10100` `120B` with `chName[32]@16` and `72B SEquip@48`).
  - `docker/server/Dockerfile`: Added `sed -i 's/\r$//'` on copied shell/Python files before `chmod +x`.
  - Rebuilt and restarted `godswar-server-1` (`docker compose up -d --build server`); removed temporary `/tmp/gw_client.exe`.
- **Verification Results (run after final edits & rebuild):**
  - **Unit Tests inside `godswar-server-1` (`UNIT_TESTS_PASSED`):** Verified `_c2s_teaminfo`/`_s2c_teaminfo` (`72B <-> 70B` with `ucMode=2`), `_s2c_trade_item` (`108B -> 120B` layout & `SEquip`), all updated `C2S_OP_MAP` team/trade rewrites (`10101->10080`, `10117->10091`, `10120->10094`, `10121->10095`, `10122->10096`, `10123->10097`, `10124->10098`, `10126->10100`), and `C2S 10108` client-only drop.
  - **Live Two-Client TCP E2E on ports `5999`/`6001` (`test`/`Umut` `id=3797` & `test2`/`DigerUmu` `id=3798`):**
    - **Party:** `C2S 10119` (`ucMode=2`) -> `S2C 10119` (`72B`, `mode=2, aimTeam=0, selfTeam=0`); `C2S 10117` invite -> `S2C 10125` (`388B`) + `S2C 10117` (`136B`); `C2S 10120` accept -> `S2C 10127` (`488B` to both, `leader=b'Umut'`, `mate=b'DigerUmu'`); `C2S 10122` leader swap -> `S2C 10127` (`DigerUmu` promoted on both); `C2S 10124` leave -> `S2C 10128` on both; `C2S 10126` reject -> `S2C 10125` tip; `C2S 10123` dissolve -> `S2C 10128` on both.
    - **Trade (`TRADE_ROUNDTRIP_AND_DB_PERSIST_VERIFIED`):** `Umut -> DigerUmu` (`C2S 10098 State=1` -> `S2C 10098 State=1`; `C2S 10101` `Bag=0, Grid=2` -> partner received `S2C 10100` `120B` `ui=0, idx=0, bid=4001, eq_bid=4001, ov=1, who=b'Umut'`; `C2S 10099 Money=1` -> partner received `S2C 10099 money=1`; both locked `State=2` -> `S2C 10098 states=[2, 2, 3]`), verified in MariaDB (`Umut Money=99`, `DigerUmu Money=101`, `4001` in `DigerUmu` `kitbag_1` grid 2), then executed reverse trade `DigerUmu -> Umut` (`S2C 10100` `120B`, `states=[2, 2, 3]`) and relogged both clients confirming `4001` & `Money=100` restored cleanly on `test`/`Umut` and removed from `test2`/`DigerUmu`.
  - **Shim Log & DB State Post-Check:** `tail -n 600 /opt/gw/logs/game_shim.log` reported `ZERO_DROPPED_OR_UNMAPPED_IN_RECENT_LOG`; MariaDB confirmed `3797 Umut Money=100` (`[4001]` back in `kitbag_1` slot 2) and `3798 DigerUmu Money=100` (`kitbag_1` clean).
- **Engineering Opinion:**
  - Both multiplayer regressions stemmed purely from incomplete `1.0.282` <-> `2007` wire translations (`gw_protocol.py`), not server-side bugs in `GameServer.exe`. With `MSG_TEAMINFO` (`72B <-> 70B`), `MSG_TEAM_ADD`/`LEADER`/`DISSOLVE`/`LEAVE`, `MSG_TRADE_ADDITEM`, and `MSG_TRADE_ITEM` (`108B -> 120B`) all translated and verified live end-to-end against `GameServer.exe` and MariaDB, both party formation/management and two-way item+gold trading are ready for live in-game testing with `test` and `test2`.
