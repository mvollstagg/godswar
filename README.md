# GodsWar Online — local revival

Legacy GodsWar Online (众神之战) server from 2007/2008, running in Docker.
The three server daemons are 32-bit Windows binaries, so they run under a
32-bit Wine prefix inside a Linux container; MariaDB serves the legacy schema.

```
Client/                             game client (Windows, run play.cmd)
GW 2.0/Server Files (Compiled)/     LoginServer / DBServer / GameServer + content
GW 2.0/Server Files (Source)/       C++ source (VS2008 era), reference only
GodsWar-Private-Server-master/      abandoned C# emu, not used
docker/                             container images, DB bootstrap, tools
compose.yaml                        db + server
```

## Requirements

- Docker Desktop (Linux containers)
- Windows for the client

## Start the server

```bash
cp docker/.env.example .env       # then edit DB_PASSWORD / ADVERTISE_IP
docker compose up -d --build
docker compose logs -f server
```

First start imports both databases, then LoginServer (5999), DBServer (5000)
and GameServer (6001). The GameServer loads maps, scenes and ~745 Python
scripts, so it needs a few minutes before it accepts connections; the
container healthcheck allows 5 minutes for that.

Check status:

```bash
docker compose ps
docker compose exec db mariadb -uroot -p"$DB_PASSWORD" -e "SELECT COUNT(*) FROM accounts.gamearea_info;"
```

## Play

Run `Client/play.cmd` (it launches `GodsWar.exe` directly — `Launch.exe` and
`Patcher.exe` point at a patch server that no longer exists).

**Do not run `Launch.exe`.** It is the retail launcher: it found the real
server list baked into its binary (`67.208.220.189/.251`, `67.208.221.34`) and
rewrites `Client/config.ini` with the retail address, `Region=13` and
`ISFB=1`. That last flag switches the client into Facebook login mode, so the
next start opens a browser instead of the login box and every connection times
out. Restore the three values (`IP=127.0.0.1`, `Region=11`, `ISFB=0`) if it
happens — `play.cmd` never touches them.

Default account: **test / test**. The client connects to `127.0.0.1:5999`
(`Client/config.ini`).

## Accounts, GM, verification

```bash
# add an account: edit the two values at the top, then
docker compose exec -T db mariadb -uroot -p"$DB_PASSWORD" accounts < docker/tools/new-account.sql

# grant / revoke GM for a character
docker/tools/gm.sh MyCharacter
docker/tools/gm.sh MyCharacter --off

# headless check of the whole login chain (no client GUI needed)
python docker/tools/login_probe.py --test --account test --password test --version 1000270
```

### Client version gate

`CLIENT_VERSION` in `.env` must equal the build number the client sends as
`fVersion`. The client sends the build as `1_000_000 + build`, so 1.0.145 is
`1000145` and 1.0.270 is `1000270`. That is the value the bundled 1.0.270
client needs; the `Version=10267` line the repack shipped with was wrong (a
1.0.x client never sends a 5-digit number).

On a mismatch the LoginServer answers with `ucInfo = 0xff` and drops the
connection — the client shows a version error and nothing else. It does log
what it received, so the value can be read straight out of the log after a
failed attempt:

```bash
docker compose exec -T server sh -lc 'iconv -f gbk -t utf-8 /opt/gw/logs/LoginServer.log | tail -5'
# LoginServer> 登陆消息...
# 客户端版本:1000270          <- put this in CLIENT_VERSION
```

Alternatively, capture the packet itself:

```bash
docker compose stop server
python docker/tools/login_probe.py --listen      # then start the client once
```

It prints `fVersion=...`; put that number in `.env`, then
`docker compose up -d`.

## Login reply shim (`docker/tools/login_shim.py`)

This repack's `LoginServer.exe` never answers `MSG_LOGIN` at all. Its startup
banner says `去版本验证` ("version verification removed") and the binary shows
why: the check compares the same field twice, so it always jumps over the only
block that sends `MSG_LOGIN_RETURN_INFO`, into a login path that sends nothing.
The 1.0.145 client never waited for that reply, which is why it worked; the
1.0.270 client waits and shows `verifying...` followed by a timeout, whatever
`CLIENT_VERSION` is set to.

So the public login port is no longer the daemon's. `LoginServer` listens on
`5998` inside the container and a small Python front end owns `5999`,
decrypts a copy of the client→server stream, answers `MSG_LOGIN` itself with
`ucInfo = 1` (login success, per `Common/MsgDefine.h`) and passes everything
else through untouched — including the `MSG_REQUEST_GAMESERVER` exchange the
LoginServer does implement, and the `GameServer` registration (that one goes
directly to 5998).

Both directions of this protocol use the same 256-byte XOR keystream from
position 0, so pass-through is byte-exact; the shim only re-encrypts the
server→client direction to account for the packet it injected. It logs every
packet to `logs/login_shim.log`, which doubles as a protocol capture:

```bash
docker compose exec -T server cat /opt/gw/logs/login_shim.log
```

### The 1.0.282 US client needs three things the 1.0.145 one did not

The US build shifted the login-server opcodes and changed how the server picker
is filled. All three were confirmed against the shipped binary (`GodsWar.map`
symbols plus disassembly of `NetSystemMessage`) and against the emulator's
captured retail packets in `GodsWar-Private-Server-master`:

1. **The login reply is opcode 3, not 2.** The dispatcher compares `[msg+6]`
   against `0x2710`, then `3`, then falls through to the default handler.
   Sending type 2 makes the client drop the packet and sit on `Verifying...`
   until it times out.
2. **The server list is pushed, not requested.** This client never sends
   `MSG_REQUEST_GAMESERVER`; it opens the server-select screen and waits for the
   list to arrive. The shim sends it immediately after the login reply.
3. **The list is `MSG_GAMESERVER_INFO` (10008) entries, not
   `MSG_RESPONSE_GAMESERVER` (10001).** Each entry is **78 bytes**, not the
   48-byte shape in the emulator's retail capture. The client's 10008 handler
   holds the packet at object offset 4, so its reads map to wire offsets:
   name at 4 (32 bytes), IP at 36 (32 bytes), port at 68 (dword), server id at
   76, state at 77. Feeding the 48-byte variant renders the name as garbage and
   the UI reports "no server available right now".

The client→gameserver direction is **plaintext** in this build; only the login
link is XOR-encrypted. `login_probe.py` defaults to plaintext for that leg
(`--gs-cipher plain`): sending an encrypted `MSG_LOGIN_GAMESERVER` makes the
GameServer read a garbage header and drop the packet silently, which looks
exactly like a rejected validate code.

Env vars: `LOGIN_PORT` (public, 5999) and `LOGIN_BACKEND_PORT` (5998). The
version gate only decides whether the daemon logs the packet now, so
`CLIENT_VERSION` no longer matters for playing.

## LAN play

Set `ADVERTISE_IP` in `.env` to the host's LAN IPv4 (e.g. `192.168.68.54`),
keep ports 5999/6001 published, and point `Client/config.ini` on the other PCs
at that address. The container adds the address to `lo` (hence `NET_ADMIN`) so
the daemons can bind it, and relays to it from the container interface.

## Data and development

Content lives in the server tree and needs no recompilation:

| What | Where |
|---|---|
| Items, forge, shops | `GW 2.0/Server Files (Compiled)/gameserver/Item/*.xml` |
| Skills / buffs | `.../gameserver/Skill/{Magic,Skill,Status}.ini` |
| NPCs, spawns, portals | `.../gameserver/Scene/<map>/` |
| Drops | `.../gameserver/Drop/*.ini` |
| Quests / AI / events | `.../gameserver/Script/**/*.py` (Python 2.5) |
| Stats, formulas, exp | `.../gameserver/Data*`, `Settings` in the client |

Rebuild the image after editing: `docker compose up -d --build server`.
Keep a copy of `gw-db-data` before schema-heavy changes
(`docker run --rm -v godswar_gw-db-data:/data -v "$PWD":/backup alpine tar czf /backup/gw-db.tgz /data`).

Code changes to the daemons need the VS2008 (v90) toolchain: they link
prebuilt VC9 static libraries (`Base.lib`, `SNet.lib`, `DB.lib`) and `Net.dll`
has no source. Note the C++ tree is a slightly older revision than the shipped
binaries (e.g. `SP_INSERT_ROLE_INFO` has 14 parameters in source, 15 in the
binary and in the database), so rebuilds must be reconciled first.

## GameServer under Wine (fixed crash)

The GameServer used to die 2-4 s into startup, right after the
`Initialize ChatHandler` progress bar:

```
wine: Unhandled page fault on read access to 00000004 at address 1E0903F5
```

That address is `PyModule_AddObject` inside `python25.dll`, i.e. the embedded
Python 2.5 script engine. `InitGameScript()` (`GameServer/GameScript.cpp`) does
`PyModule_AddObject(ImportModule("Script.Altar"), …)` with no NULL check, and
`Script/Altar.py` starts with `import math`. On the original Windows host that
came from a system-wide `C:\Python25`; without a standard library the import
returns NULL and the unchecked call dereferences it (`[NULL+4]` = `ob_type`).

The image now ships Python 2.5.4 (`docker/python25/runtime` -> `C:\Python25`,
`PYTHONHOME=C:\Python25`), which the GameServer needs for `Script/*.py`.
Without it every restart reproduces the same fault.

Debugging switches for the entrypoint (see `docker/server/entrypoint.sh`):

| Variable | Effect |
|---|---|
| `GW_DEBUG=1` | daemons attached to the terminal (line-buffered, no log redirect) |
| `GW_WINEDBG_ENABLE=1` | keep Wine's crash debugger so a fault prints a backtrace |
| `GW_WINEDEBUG=+seh,+loaddll` | Wine debug channels (module bases, exception records) |

Example: `docker compose run --rm -e GW_DEBUG=1 -e GW_WINEDEBUG=+loaddll server`.
`winedbg.exe=d` is set in the image so a fault exits instead of leaving an
interactive `winedbg` that the supervisor cannot see dying.

## Client data patch (Athens_Newbie NPCs)

The client shipped in `Client/` is **1.0.270** (US data, `GodsWar.exe` file
version 1, 0, 270, 0; data under `Localization/en_us/`, with `zh_cn/`
alongside). It already contains all 17 `Athens_Newbie_*` ids, but the server
names the older city NPCs in Chinese (`Name=墨勒提欧_雅典男文官1` in
`Scene/Athens/NPC00.INI`) while the English data keys `NPC.INI` by English id
(`[Athens_001_AthenianCivilian1]`). The client resolves the section by the
server's `Name=`, so 293 city NPCs stopped with `section[...] not found`.

The patcher also handles that: for each missing name it takes the NPC id from
the scene's `Script=Script.Athens.Athens_001` and clones the client's own
`Athens_001_*` section, so the appearance is the real one. Run on the bundled
client: `missing: 293` → 248 matched by script id, 45 generic (NPCs the US
client never had: warehouse keepers, some merchants), `NPC.INI` 569 → 862
sections, and a re-run reports `missing: 0`.

The same tool serves the older 1.0.145 client in `Client_1.0.145/`, which
predates the `Athens_Newbie` scene data: the server spawns 17 NPCs with English
ids (`Athens_Newbie_013_MaleHero2`) while that client's `Settings/Sys/NPC.INI`
only knew the older Chinese section names, so entering the world stopped with
`section[Athens_Newbie_013_MaleHero2] not found in ./Settings/Sys/NPC.ini`.

`docker/tools/patch-client-npc.py` compares every `Name=` in
`gameserver/Scene/*/NPC*.INI` against the client's NPC.ini and appends the
missing sections, cloning avatar values from an existing entry of the same sex
(so every avatar part id is one the client already ships). It is idempotent and
keeps a one-time `NPC.INI.orig` backup:

```
python docker/tools/patch-client-npc.py          # or: --dry-run to preview
```

Run it after any server scene edit that adds NPCs, then restart the client (it
reads `Settings` at startup). Sections whose script id has no counterpart in
the client fall back to a same-sex template, so those few look generic; the
other 248 keep the appearance the US client already ships for that NPC.

## Known quirks

- `SP_UPDATE_CONSORTIA_INFO` builds SQL in a fixed buffer; very long guild
  names can truncate the query (`DBError.txt`). Keep guild names short.
- `CreateItem() Lv error 0,0` / `!pItem->GetItemBaseAttribute()` in
  `gameserver/Log/SYS*.log` are data warnings for a few item entries, not fatal.
- The GM pipe server (`GMMgr`) is optional and not required for gameplay.
- The 1.0.145 client in `Client_1.0.145/` is older than the server data
  (missing `Athens_Newbie` NPC ids, 56 client-only item IDs); it needs
  `docker/tools/patch-client-npc.py`. Use `Client/` (1.0.270) instead.
- The 1.0.270 client ships content the server has no scenes for
  (`Atlantis_Entrance`, `Alcatraz_Island`, `Medusa_Island`, `Labyrinth` …).
  Harmless: the server simply never sends those maps.
