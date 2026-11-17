# GodsWar Online â€” C++ to C# (.NET) Server Porting Plan

This document is the master blueprint and session-by-session execution prompt for porting the legacy C++ GodsWar server (`GW 2.0/Server Files (Source)/`) into a clean, debuggable, cross-platform **C# (.NET 10+)** server targeting the **`1.0.282` US client (`Client/GodsWar.exe`)** directlyâ€”eliminating Wine, 32-bit Windows binaries, and the Python protocol translation shim.

---

## Reference Sources (Ground Truth)

| Need | Source of Truth | Notes |
|---|---|---|
| **Game logic, formulas, AI, DB schema** | `GW 2.0/Server Files (Source)/` | 270 C++ files (`LoginServer`, `DBServer`, `GameServer`, `Common`). Slightly older revision than the compiled `GameServer.exe`. |
| **Maps, NPCs, Monsters, Scripts, SQL** | `GW 2.0/Server Files (Compiled)/` | `Scene/*`, `Script/*`, `Monster.ini`, `NPC00.INI`, `-- RESTORE DATABASES/`. |
| **`1.0.282` exact opcodes & structs** | `docker/tools/gw_protocol.py`, `docker/tools/gw_npc_map.py`, `docker/tools/login_shim.py`, `AUDIT.md`, `Client/GodsWar.exe` | Always trust `gw_protocol.py` + `Client/GodsWar.exe` over C++ headers when packet sizes/offsets differ. |
| **C# project style inspiration** | `GodsWar-Private-Server-master/` | Early-stage networking/crypto reference only; contains no real game logic. |
| **Live reference oracle** | `compose.yaml` (`docker compose up -d`) | Keep untouched (MariaDB + Wine C++) so packet captures and behavior can be verified against the real binary. |

---

## Target Solution Layout

Create the new server under `src/` (using `.NET 10` / `net10.0` or the installed SDK, e.g. `net11.0`) backed by **PostgreSQL** from Day 1:

```text
src/
  GodsWar.Server.sln
  GodsWar.Protocol/       # 256-byte XOR cipher, <HH framing, 1.0.282 opcodes & packet structs
  GodsWar.Database/       # Npgsql + Dapper/EF Core repositories + PostgreSQL schema/migrations
  GodsWar.LoginServer/    # Standalone LoginServer (port 5999)
  GodsWar.GameServer/     # World, maps, AOI, movement, combat, NPCs, items, scripts (port 6001)
  GodsWar.Tests/          # xUnit tests (crypto, packet serialization, protocol vectors, combat math)
```

### Engineering Rules for Every Phase
1. **Binary-exact `1.0.282` packets:** Verify every packet opcode, byte length, and field offset against `docker/tools/gw_protocol.py` and `AUDIT.md` (and `Client/GodsWar.exe` when adding new ones).
2. **Debuggable & contributor-friendly:** Clean separation between networking/serialization (`GodsWar.Protocol`), persistence (`GodsWar.Database`), and game simulation (`GodsWar.GameServer`). Use structured logging (`ILogger`) and unit tests for every packet and core formula.
3. **Verify before finishing a phase:** Run `dotnet build`, `dotnet test`, and live verification (either `docker/tools/login_probe.py` or `Client/play.cmd`). Update `AUDIT.md` or a phase checklist with what was implemented and verified.

---

## Phased Execution Plan

### Phase 1 — Solution Scaffolding, Crypto, Packet Framing & PostgreSQL Schema ✅
- **Goal:** Working .NET solution with tested networking primitives and a clean PostgreSQL database ready for the server.
- **Tasks:**
  1. Initialize `src/GodsWar.Server.sln` and the 5 projects (`Protocol`, `Database`, `LoginServer`, `GameServer`, `Tests`).
  2. Port the 256-byte XOR stream cipher and `<HH` (length + opcode) TCP packet framing using `System.IO.Pipelines` / `Span<byte>` / `BinaryPrimitives`.
  3. Inspect the legacy SQL dumps (`GW 2.0/Server Files (Compiled)/-- RESTORE DATABASES/`) and convert the `accounts` and `godswar` schemas into clean **PostgreSQL** DDL (proper UTF-8, `IDENTITY`, `TIMESTAMPTZ`, `JSONB` where helpful) + seed data (`test`/`test` account, `gamearea_info`, default character/world data).
  4. Set up `Npgsql` + Dapper/EF Core in `GodsWar.Database` and add a PostgreSQL Docker Compose file/profile (e.g. `compose.dotnet.yaml` with `postgres:17`, separate from the legacy `compose.yaml` oracle).
  5. Write unit tests in `GodsWar.Tests` verifying XOR keystream encryption/decryption and packet framing against test vectors from `docker/tools/gw_protocol.py`.
- **Done when:** `dotnet test` passes 100%, Postgres starts cleanly with the migrated schema + seed data, and a smoke test reads `accounts.gamearea_info` from PostgreSQL.

### Phase 2 — `LoginServer` (Direct `1.0.282` Auth & Server List) ✅
- **Goal:** Replace `LoginServer.exe` + `login_shim.py` on port `5999` with `GodsWar.LoginServer`.
- **Tasks:**
  1. Port `GW 2.0/Server Files (Source)/LoginServer/` auth flow and `MSG_LOGIN` / `MSG_LOGIN_RETURN_INFO` (`ucInfo = 1` / error codes) + `MSG_REQUEST_GAMESERVER`.
  2. Query PostgreSQL (`accounts` schema) for account verification and `gamearea_info` server list (using `ADVERTISE_IP`).
  3. Support clean session handoff / token validation with `GameServer`.
- **Verified:** `GodsWar.Protocol/Packets/LoginPackets.cs` (`MSG_LOGIN` 72B/140B, `MSG_LOGIN_RETURN_INFO` opcode `3`/`2`, `MSG_GAMESERVER_INFO` 78B/76B, `MSG_REQUEST_GAMESERVER` `3`/`4`, `MSG_VALIDATE_GAMESERVER` `300`, `MSG_RESPONSE_GAMESERVER` `10001`), `GodsWar.LoginServer` (`LoginServerListener`, `LoginSessionHandler`, `GameServerRegistry`), `dotnet build` + `dotnet test` (21/21 passing), and live `login_probe.py --test --account test --password test --version 1000270` (plus `test2`/`123456` and invalid password `0x00` check) against `GodsWar.LoginServer` + PostgreSQL + `6001` validation stub.

### Phase 3 — `GameServer` Core: Character Select, World Entry & Movement ✅
- **Goal:** Replace `GameServer.exe` for login-to-world spawn, NPC/mob visibility, and walking around.
- **Tasks:**
  1. Port `GameServer` TCP listener (port `6001`), session management, and character list / create / delete (`10002`, `10006`, etc.) backed by PostgreSQL.
  2. Load scene/map metadata, NPC spawns (`NPC00.INI` + `gw_npc_map.py` US name resolution), and Monster spawns (`Monster.ini`) from `GW 2.0/Server Files (Compiled)/Scene/`.
  3. Implement exact `1.0.282` world-entry packets (`10019 MSG_SELFINFO`, `10005` ready handshake, `10031 MSG_KITBAG`, `10041 MSG_SELFPROPERTY`, `10160 MSG_SYN_GAMEDATA`, `10020 MSG_OBJECTINFO`, `10074/10077` skills, `10040/10039` altars/relations).
  4. Implement spatial grid / AOI broadcasting, player movement (`10187`), and heartbeat (`10015`, `10303`).
- **Verified:** `GodsWar.Protocol/Packets/GamePackets.cs` (`10000`, `10002` 356B, `10003`/`10004`/`10158`, `10006`/`10005`/`10007`, `10019` 1192B, `10031` 888B, `10041` 132B, `10160` 236B, `10020` 628B+, `10021`, `10039`/`10040`, `10093`/`10156`, `10187` 20B, `10303` 16B), `GodsWar.Database` character create/delete/position/skills repositories, `GodsWar.GameServer` (`LoginServerClient`, `ValidationTokenStore`, `WorldDataLoader`, `NpcNameResolver`, `WorldMap` 16x16 AOI grid with walk self-echo suppression, `GameSessionHandler`, `GameServerListener`), `dotnet build` + `dotnet test` (31/31 passing), and live `login_probe.py --test --account test --password test --version 1000270` + E2E character create/enter/ready/walk/delete probe (`npcs=13`, `monsters=12`, `Athens_Newbie_013_MaleHero2`, zero walk self-echo) against `GodsWar.LoginServer` + `GodsWar.GameServer` + PostgreSQL.

### Phase 4 — Combat, Monster AI, Stats, Drops & Inventory ✅
- **Goal:** Playable combat loop, monster AI, leveling, loot, and equipment.
- **Tasks:**
  1. Port `Combat.cpp`, `Buff.cpp`, `GameObject.cpp`, `DropItemMgr.cpp`, `Bag.cpp`, and `Item.cpp` from `GW 2.0/Server Files (Source)/GameServer/GameCore/`.
  2. Implement monster AI loop (idle, patrol, aggro, chase, attack, death, respawn timer).
  3. Implement damage calculation, XP/level-up, skill casting & cooldowns, buff/debuff ticks, ground loot drops, pickup, equip/unequip, and DB persistence.
- **Verified:** `GodsWar.Protocol/Packets/CombatPackets.cs` & `InventoryPackets.cs` (`10023`–`10028`, `10037`–`10038`, `10042`–`10045`, `10048`–`10050`, `10056`, `10129`, `10161`), `GodsWar.Database` (`UpdateCharacterStateAsync`, `UpdateKitbagAsync`), `GodsWar.GameServer/Game/` (`GameTables` 200-level tables from `Mat.cpp`, `GameDataCatalog` loading `ItemBaseAttribute.xml`/`ItemAppendAttribute.xml`/`Magic.ini`/`Status.ini`/`Drop*.ini`, `CombatCalculator`, `MonsterEntity`, `GameplayHandlers`, `WorldSimulationService` 200ms AI/buff/regen loop), `dotnet build src/GodsWar.Server.sln` + `dotnet test src/GodsWar.Server.sln` (37/37 passing), and live E2E probe against `GodsWar.LoginServer` + `GodsWar.GameServer` + PostgreSQL (skill cast `10038`->`10044`/`10129`/`10043`, combat kill `10024`->`10025`, drop `10027` + pickup `10048`, and XP/money persistence across relog).

### Phase 5 — NPCs, Quests, Shops, Forging, Portals & Social Systems ✅
- **Goal:** Full core RPG world systems.
- **Tasks:**
  1. Port `Dial.cpp` (NPC dialogs), shops (buy/sell), `EquipForge.cpp`, `ChangeRegion.cpp` (portals/map transitions), `Altar.cpp`, `Country.cpp`, and `ChatCommand.cpp` (local/world/whisper/GM commands).
  2. Port or bridge `GameScript.cpp` (~745 quest/NPC scripts in `GW 2.0/Server Files (Compiled)/Script/`).
  3. Port Parties and Guilds + DB persistence.
- **Verified:** `GodsWar.Protocol/Packets/` (`ChatAndPortalPackets.cs`, `NpcShopPackets.cs`, `QuestPackets.cs`, `ForgePackets.cs`, `SocialPackets.cs` covering `10018`, `10033`–`10034`, `10057`–`10058`, `10064`–`10070`, `10072`–`10087`, `10093`–`10107`, `10117`–`10128`, `10131`–`10146`), `GodsWar.Database` (`WorldSocialRepository` for `user_quest`, `user_relation`, `user_altar`, `consortia`, and `character_kitbag.storage`), `GodsWar.GameServer/Game/` (`ScriptDataLoader` loading `Convey.ini`, `EquipForge.xml`, `Script/Item.py`, `Script/Quest.py`, and `Script/*/*.py`, plus `QuestService`, `NpcShopAndForgeService`, `ChatAndPortalService`, and `SocialService`), `dotnet build src/GodsWar.Server.sln` + `dotnet test src/GodsWar.Server.sln` (43/43 passing), and live two-client E2E probe against `GodsWar.LoginServer` + `GodsWar.GameServer` + PostgreSQL (chat `10033`, NPC talk `10064`->`10074`, quest find `10087` + accept `10079` + complete `10081`->`10083`, party `10119`->`10117`->`10120`->`10127`, trade `10098`/`10099`->state `3`, guild create `10131`/`10132`/`10137` + exit `10143`, and bidirectional portal transitions `10187`->`10018` `Athens_Newbie (1) <-> Athens (0)`).

### Phase 6 â€” `1.0.282`-Exclusive Features (Pet System, AFK System & Polish)
- **Goal:** Implement features present in `Client/GodsWar.exe` (`1.0.282`) that were missing or incomplete in the 2007/2008 C++ server.
- **Tasks:**
  1. Capture and/or disassemble `Client/GodsWar.exe` opcodes for the **Pet system**, **AFK / auto-hunt system**, and any remaining unmapped UI packets.
  2. Design PostgreSQL tables / `JSONB` columns as needed and implement full server-side handlers in `GodsWar.GameServer`.
  3. Finalize Dockerfiles / `compose.dotnet.yaml` for running the pure .NET server alongside PostgreSQL, plus contributor & debugging docs.
- **Done when:** Pet and AFK systems work end-to-end in `Client/GodsWar.exe` with zero Wine/legacy binary dependencies.

---

## Copy-Paste Prompt for Future Sessions

Use this prompt when starting a new session to execute a specific phase:

```text
Read PORTING_PLAN.md and AUDIT.md first.
We are executing **Phase <N>** of the C++ to C# (.NET) server port.

Rules:
- Use `GW 2.0/Server Files (Source)/` for game logic, formulas, and DB schema.
- Use `docker/tools/gw_protocol.py`, `docker/tools/gw_npc_map.py`, and `Client/GodsWar.exe` as the ground truth for `1.0.282` packet opcodes and struct layouts.
- Keep code clean, modular, debuggable, and well-tested (`GodsWar.Tests`).
- After implementing Phase <N>, run `dotnet build`, `dotnet test`, and any relevant live/probe checks, then report exact results and update `PORTING_PLAN.md` progress.

Start Phase <N> now.
```
