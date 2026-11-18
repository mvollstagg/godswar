# GodsWar Online — .NET 11 + PostgreSQL Server

A clean-room **C# (.NET 11) + PostgreSQL 17** server implementation for **GodsWar Online** (`LoginServer` + `GameServer` + built-in Web Admin UI), wire-compatible with the **1.0.282 US client**.

## Repository Structure

```text
src/
  GodsWar.Database/                 Npgsql + PostgreSQL repositories & schema models
  GodsWar.Protocol/                 1.0.282 wire framing, XOR cipher & binary packet structs
  GodsWar.LoginServer/              LoginServer (5999), account auth, server list & GameServer IPC
  GodsWar.GameServer/               GameServer (6001), world AOI, combat, quests, social, mall, pets, stalls
    Admin/AdminWebService.cs        Built-in Web Admin UI & JSON API (port 8080)
  GodsWar.Tests/                    xUnit protocol, gameplay & integration tests
docker/postgres/init/               PostgreSQL 17 schemas & seed SQL (01-03)
GW 2.0/Server Files (Compiled)/gameserver/
                                    Game data files (Drop/, Item/, Obstacle/, Scene/, Script/, Skill/)
compose.dotnet.yaml                 PostgreSQL 17 + LoginServer + GameServer Docker stack
compose.postgres.yaml               Standalone PostgreSQL 17 container for local IDE debugging
```

---

## Quick Start (Docker)

### Requirements
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Linux containers)
- A **1.0.282 US GodsWar Online client** (Windows)

### 1. Start PostgreSQL, LoginServer & GameServer
```bash
docker compose -f compose.dotnet.yaml up -d --build
docker compose -f compose.dotnet.yaml logs -f loginserver gameserver
```

Services started:
- **`postgres`** (`postgres:17-alpine`, port `5432`) — auto-initializes `accounts` and `godswar` schemas from `docker/postgres/init/`
- **`loginserver`** (`GodsWar.LoginServer`, port `5999`)
- **`gameserver`** (`GodsWar.GameServer`, port `6001` game TCP, port `8080` Web Admin UI; mounts `./GW 2.0/Server Files (Compiled)/gameserver` read-only)

Verify containers:
```bash
docker compose -f compose.dotnet.yaml ps
docker compose -f compose.dotnet.yaml exec postgres psql -U godswar -d godswar -c "SELECT COUNT(*) FROM godswar.character_base;"
```

### 2. Connect with the 1.0.282 Client
1. In your 1.0.282 client's `config.ini`, set:
   ```ini
   [SERVER]
   PORT=5999
   IP=127.0.0.1

   [SYSTEM]
   Region=11
   ISFB=0
   ```
2. Launch `GodsWar.exe` directly (**do not** run `Launch.exe` or `Patcher.exe`, which overwrite `config.ini` with defunct retail/Facebook settings).
3. Default test account: **`test` / `test`** (auto-creation of new accounts is enabled by default via `LoginServer__AutoCreateAccount=true`).

For **LAN play**, set `ADVERTISE_IP=<your-lan-ip>` when starting Compose and point `config.ini` on other PCs to that IP:
```bash
ADVERTISE_IP=192.168.1.50 docker compose -f compose.dotnet.yaml up -d
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
- **Auth & Character Flow:** 1.0.282 login cipher, server list, character creation/deletion, Sparta (`Camp 0`) & Athens (`Camp 1`) spawns, starter gear/skills, server time sync.
- **World & Combat:** AOI player/monster/NPC visibility, movement, physical/magic combat, skill/talent upgrades, XP & level-ups (100x default), monster loot drops & pickup, death/revive.
- **Economy & Features:** NPC shops, potions/consumables, right-click equip, bag management, GM commands, and the Web Admin UI (`:8080`).

### Known Work-in-Progress
- **Equipment UI:** Right-click unequip and drag-to-equip UI sync still have client/server state edge cases.
- **Pet System:** Pet list & HUD display work, but pet world spawning (`MsgPetView`) and pet action button states (`Summon` / `Recall` / `Abandon`) need further packet alignment.
