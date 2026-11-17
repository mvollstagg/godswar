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

### Phase 1 â€” Solution Scaffolding, Crypto, Packet Framing & PostgreSQL Schema
- **Goal:** Working .NET solution with tested networking primitives and a clean PostgreSQL database ready for the server.
- **Tasks:**
  1. Initialize `src/GodsWar.Server.sln` and the 5 projects (`Protocol`, `Database`, `LoginServer`, `GameServer`, `Tests`).
  2. Port the 256-byte XOR stream cipher and `<HH` (length + opcode) TCP packet framing using `System.IO.Pipelines` / `Span<byte>` / `BinaryPrimitives`.
  3. Inspect the legacy SQL dumps (`GW 2.0/Server Files (Compiled)/-- RESTORE DATABASES/`) and convert the `accounts` and `godswar` schemas into clean **PostgreSQL** DDL (proper UTF-8, `IDENTITY`, `TIMESTAMPTZ`, `JSONB` where helpful) + seed data (`test`/`test` account, `gamearea_info`, default character/world data).
  4. Set up `Npgsql` + Dapper/EF Core in `GodsWar.Database` and add a PostgreSQL Docker Compose file/profile (e.g. `compose.dotnet.yaml` with `postgres:17`, separate from the legacy `compose.yaml` oracle).
  5. Write unit tests in `GodsWar.Tests` verifying XOR keystream encryption/decryption and packet framing against test vectors from `docker/tools/gw_protocol.py`.
- **Done when:** `dotnet test` passes 100%, Postgres starts cleanly with the migrated schema + seed data, and a smoke test reads `accounts.gamearea_info` from PostgreSQL.

### Phase 2 â€” `LoginServer` (Direct `1.0.282` Auth & Server List)
- **Goal:** Replace `LoginServer.exe` + `login_shim.py` on port `5999` with `GodsWar.LoginServer`.
- **Tasks:**
  1. Port `GW 2.0/Server Files (Source)/LoginServer/` auth flow and `MSG_LOGIN` / `MSG_LOGIN_RETURN_INFO` (`ucInfo = 1` / error codes) + `MSG_REQUEST_GAMESERVER`.
  2. Query PostgreSQL (`accounts` schema) for account verification and `gamearea_info` server list (using `ADVERTISE_IP`).
  3. Support clean session handoff / token validation with `GameServer`.
- **Done when:** `python docker/tools/login_probe.py --test --account test --password test --version 1000270` succeeds against `GodsWar.LoginServer`, and `Client/GodsWar.exe` reaches the server list and hands off to `GameServer`.

### Phase 3 â€” `GameServer` Core: Character Select, World Entry & Movement
- **Goal:** Replace `GameServer.exe` for login-to-world spawn, NPC/mob visibility, and walking around.
- **Tasks:**
  1. Port `GameServer` TCP listener (port `6001`), session management, and character list / create / delete (`10002`, `10006`, etc.) backed by PostgreSQL.
  2. Load scene/map metadata, NPC spawns (`NPC00.INI` + `gw_npc_map.py` US name resolution), and Monster spawns (`Monster.ini`) from `GW 2.0/Server Files (Compiled)/Scene/`.
  3. Implement exact `1.0.282` world-entry packets (`10019 MSG_SELFINFO`, `10005` ready handshake, `10031 MSG_KITBAG`, `10041 MSG_SELFPROPERTY`, `10160 MSG_SYN_GAMEDATA`, `10020 MSG_OBJECTINFO`, `10074/10077` skills, `10040/10039` altars/relations).
  4. Implement spatial grid / AOI broadcasting, player movement (`10187`), and heartbeat (`10015`, `10303`).
- **Done when:** `Client/play.cmd` logs in with `test / test`, enters `Athens' Suburb`, renders NPCs/monsters with English names and proper stats/level, and walks smoothly with no `"Loading..."` hang.

### Phase 4 â€” Combat, Monster AI, Stats, Drops & Inventory
- **Goal:** Playable combat loop, monster AI, leveling, loot, and equipment.
- **Tasks:**
  1. Port `Combat.cpp`, `Buff.cpp`, `GameObject.cpp`, `DropItemMgr.cpp`, `Bag.cpp`, and `Item.cpp` from `GW 2.0/Server Files (Source)/GameServer/GameCore/`.
  2. Implement monster AI loop (idle, patrol, aggro, chase, attack, death, respawn timer).
  3. Implement damage calculation, XP/level-up, skill casting & cooldowns, buff/debuff ticks, ground loot drops, pickup, equip/unequip, and DB persistence.
- **Done when:** A player can fight monsters, take/deal damage, cast skills, gain XP/level up, pick up/equip items, and persist state across relogs.

### Phase 5 â€” NPCs, Quests, Shops, Forging, Portals & Social Systems
- **Goal:** Full core RPG world systems.
- **Tasks:**
  1. Port `Dial.cpp` (NPC dialogs), shops (buy/sell), `EquipForge.cpp`, `ChangeRegion.cpp` (portals/map transitions), `Altar.cpp`, `Country.cpp`, and `ChatCommand.cpp` (local/world/whisper/GM commands).
  2. Port or bridge `GameScript.cpp` (~745 quest/NPC scripts in `GW 2.0/Server Files (Compiled)/Script/`).
  3. Port Parties and Guilds + DB persistence.
- **Done when:** Players can talk to NPCs, accept/complete quests, buy/sell/forge items, and travel between maps (e.g. `Athens' Suburb` -> `Athens`).

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
