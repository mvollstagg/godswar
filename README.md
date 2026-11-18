# GodsWar Online — .NET 11 + PostgreSQL Server (`v2.26.002` / `v2.27`)

A clean-room **C# (.NET 11) + PostgreSQL 17** server implementation for **GodsWar Online** (`LoginServer` + `GameServer` + built-in Web Admin UI), supporting the **v2.26.002 (`v2.27`) client** (while preserving `1.0.282` protocol compatibility).

## Repository Structure

```text
src/
  GodsWar.Database/                 Npgsql + PostgreSQL repositories & schema models
  GodsWar.Protocol/                 1.0.282 & v2.27 wire framing, XOR ciphers, opcode map & binary packets
  GodsWar.LoginServer/              LoginServer (5999), account auth, server list & GameServer IPC
  GodsWar.GameServer/               GameServer (6001), world AOI, combat, quests, social, mall, pets, stalls
    Admin/AdminWebService.cs        Built-in Web Admin UI & JSON API (port 8080)
  GodsWar.Tests/                    xUnit protocol, gameplay & integration tests
docker/postgres/init/               PostgreSQL 17 schemas & seed SQL (01-03)
GW 2.0/Server Files (Compiled)/gameserver/
                                    Game data files (Drop/, Item/, Obstacle/, Scene/, Script/, Skill/)
Client/                             v2.26.002 (v2.27) game client (Windows, run Client/GodsWar.exe)
compose.v227.yaml                   Isolated godswar-v227 Docker stack (uses gw-pg-data-v227)
compose.dotnet.yaml                 Legacy 1.0.282 Docker stack (uses gw-pg-data)
compose.postgres.yaml               Standalone PostgreSQL 17 container for local IDE debugging
```

---

## Quick Start (Docker)

### Requirements
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Linux containers)
- The included **v2.26.002 (`v2.27`) GodsWar Online client** in `Client/` (Windows)

### 1. Start PostgreSQL, LoginServer & GameServer (`godswar-v227`)
```bash
docker compose -f compose.v227.yaml up -d --build
docker compose -f compose.v227.yaml logs -f loginserver gameserver
```

Services started:
- **`postgres`** (`postgres:17-alpine`, port `5432`) — uses isolated `gw-pg-data-v227` volume and auto-initializes `accounts` and `godswar` schemas from `docker/postgres/init/`
- **`loginserver`** (`GodsWar.LoginServer`, port `5999`) — auto-detects `v2.27` `Net.dll` XOR cipher (`TEncryptClient<0x20, 0xFD, 0x07, 0x1F, 0x7A, 0xCF, 0xE5, 0x3F>`) + Caesar-encoded login accounts
- **`gameserver`** (`GodsWar.GameServer`, port `6001` game TCP, port `8080` Web Admin UI; mounts `./GW 2.0/Server Files (Compiled)/gameserver` read-only)

Verify containers:
```bash
docker compose -f compose.v227.yaml ps
docker compose -f compose.v227.yaml exec postgres psql -U godswar -d godswar -c "SELECT COUNT(*) FROM godswar.character_base;"
```

### 2. Connect with the Included v2.27 Client
Run `Client/GodsWar.exe` directly (**do not** run `Launch.exe` or `Patcher.exe`, which overwrite `Client/config.ini` with defunct retail settings).
- Auto-creation of new accounts is enabled by default via `LoginServer__AutoCreateAccount=true`.
- Preconfigured `Client/config.ini`: `IP=127.0.0.1`, `PORT=5999`, `Region=11`, `ISFB=0`.

For **LAN play**, set `ADVERTISE_IP=<your-lan-ip>` when starting Compose and point `config.ini` on other PCs to that IP:
```bash
ADVERTISE_IP=192.168.1.50 docker compose -f compose.v227.yaml up -d
```

---

## Web Admin UI (`http://localhost:8080`)

`GodsWar.GameServer` includes a built-in management dashboard at **`http://localhost:8080`**:
- **XP Rate (%):** Adjust server XP rate live (starts at `10000%` / `100x` by default, with `1x / 10x / 100x / 500x` quick presets).
- **Welcome Message:** Customize the system message sent to players upon entering the world.
- **Server Announcements:** Broadcast System or World messages to all online players.
- **Online Players & GM Actions:** View connected players and trigger `Heal`, `+1 Lv`, `+100k` gold, or open the **Searchable Item Picker** modal (searches loaded `ItemBaseAttribute.xml` items by name or ID).

---

## In-Game GM Commands

Characters with `gm > 0` in `godswar.character_base` can run commands in chat:
- `/help` — List available commands
- `/xprate <percent>` — Set server XP rate (e.g. `/xprate 10000` for 100x)
- `/exp <amount>` — Grant experience
- `/level <lv>` — Set character level
- `/money <amount>` — Add gold
- `/item <id> [count]` — Spawn an item into your bag
- `/heal` — Restore full HP and MP
- `/tp <mapId|name> [x z]` — Teleport to a map/coordinates
- `/guild create <name>` — Create a guild

To grant GM privileges in PostgreSQL:
```bash
docker compose -f compose.dotnet.yaml exec postgres psql -U godswar -d godswar -c "UPDATE godswar.character_base SET gm = 1 WHERE name = 'YourCharName';"
```

---

## Local Development & Testing (.NET 11 SDK)

You can run PostgreSQL in Docker and run/debug `LoginServer` and `GameServer` natively:
```bash
# Start PostgreSQL only
docker compose -f compose.dotnet.yaml up -d postgres

# Build & run tests
dotnet build src/GodsWar.Server.sln
dotnet test src/GodsWar.Server.sln

# Run servers locally
dotnet run --project src/GodsWar.LoginServer
dotnet run --project src/GodsWar.GameServer -- --GameServer:SceneDataPath="GW 2.0/Server Files (Compiled)/gameserver/Scene"
```

---

## Implementation Status

### Working
- **Auth & Character Flow (`v2.27` & `1.0.282`):** `v2.27` `Net.dll` XOR cipher + Caesar-decoded `MSG_LOGIN`, `44`-byte `MSG_GAMESERVER_INFO`, `496`-byte `MSG_ROLE_INFO`, character creation/deletion, Sparta (`Camp 0`) & Athens (`Camp 1`) spawns, starter gear/skills, server time sync.
- **World, Portals & Combat:** `V227OpcodeMap` for shifted movement (`10192`), NPC talk (`10065`), quest view (`648`-byte `10079`), `v2.27` monster/NPC fallback tables across all 24 maps, `v2.27` portal overrides (`Athens` ↔ `Athens_Newbie`, `Sparta` ↔ `Sparta_Newbie`, `Marathon_All` ↔ `Parnitha_1`), AOI visibility, physical/magic combat, skill/talent upgrades, XP & level-ups, loot drops.
- **Economy & Features:** NPC shops, potions/consumables, bag management, GM commands (including `/level` talent points), and the Web Admin UI (`:8080`).

### Known Work-in-Progress
- **Equipment UI & Pets:** Right-click unequip/drag-to-equip UI sync and pet world actions (`Summon` / `Recall` / `Abandon` / attack / collect) still need further `v2.27` packet alignment.
- **Chat & Quest Flow:** Local chat channel (`Ch=16`) and quest acceptance/completion dialogs need additional `v2.27` payload inspection.
