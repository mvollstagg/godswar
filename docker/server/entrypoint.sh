#!/bin/bash
# GodsWar server entrypoint.
#
# Renders the three legacy config.ini files from environment variables, then
# starts LoginServer -> DBServer -> GameServer under supervision.
#
# Why the socat relays: the servers take ONE ip from config.ini and use it both
# for bind() and for the address they hand to the client. Under Docker NAT the
# container address is not reachable from outside, so the servers advertise a
# client-reachable address (ADVERTISE_IP, 127.0.0.1 for local play) and we relay
# from the container's own eth0 address on the same port. If a daemon happens to
# bind 0.0.0.0 instead, published ports work directly and the relay is skipped.
set -uo pipefail

GW_ROOT="${GW_ROOT:-/opt/gw}"
LOG_DIR="$GW_ROOT/logs"

# Debug switches (see docker/DEBUGGING.md):
#   GW_DEBUG=1           run the daemons attached to the terminal instead of
#                        redirecting them to log files, so stdio is line-buffered
#                        and a crash is visible at the exact point it happens.
#   GW_WINEDBG_ENABLE=1  keep Wine's crash debugger enabled (it prints a
#                        backtrace, then waits for input).
#   GW_WINEDEBUG=+seh    Wine debug channels, e.g. +seh,+loaddll.
GW_DEBUG="${GW_DEBUG:-0}"
GW_WINEDBG_ENABLE="${GW_WINEDBG_ENABLE:-0}"
GW_WINEDEBUG="${GW_WINEDEBUG:--all}"

DB_HOST="${DB_HOST:-db}"
DB_USER="${DB_USER:-root}"
DB_PASSWORD="${DB_PASSWORD:-godswar}"
LOGIN_PORT="${LOGIN_PORT:-5999}"
# The LoginServer listens on a private port; login_shim.py owns the public one
# and answers the login packet, which this build of the LoginServer never does.
LOGIN_BACKEND_PORT="${LOGIN_BACKEND_PORT:-5998}"
GAME_PORT="${GAME_PORT:-6001}"
# Same split for the game port: the daemon listens on a private port, the shim
# owns the public one (and logs the client's packets).
GAME_BACKEND_PORT="${GAME_BACKEND_PORT:-6000}"
DB_PORT="${DB_PORT:-5000}"
CLIENT_VERSION="${CLIENT_VERSION:-10267}"
SERVER_NAME="${SERVER_NAME:-GodsWar}"
MAX_ONLINE="${MAX_ONLINE:-200}"
# Address written into the server configs (used for bind + advertised to clients).
ADVERTISE_IP="${ADVERTISE_IP:-127.0.0.1}"

mkdir -p "$LOG_DIR"
log() { printf '[gw] %s\n' "$*"; }

# ---------------------------------------------------------------------------
# Networking setup
# ---------------------------------------------------------------------------
# If we advertise an address that is not loopback (LAN play), put it on lo so
# the daemons can actually bind it inside the container.
if [ "$ADVERTISE_IP" != "127.0.0.1" ]; then
  ip addr add "$ADVERTISE_IP/32" dev lo 2>/dev/null || true
  log "advertising $ADVERTISE_IP (added to lo)"
fi

CONTAINER_IP="$(hostname -I 2>/dev/null | awk '{print $1}')"
[ -z "$CONTAINER_IP" ] && CONTAINER_IP="$(ip -4 -o addr show eth0 | awk '{print $4}' | cut -d/ -f1)"
log "container address: ${CONTAINER_IP:-unknown}"

# ---------------------------------------------------------------------------
# Render configs (CRLF: these parsers expect Windows line endings)
# ---------------------------------------------------------------------------
render() { # render <path> ; reads heredoc on stdin
  local path="$1"
  sed 's/$/\r/' > "$path"
  log "wrote ${path#"$GW_ROOT"/}"
}

render "$GW_ROOT/loginserver/config.ini" <<EOF
[System]
IP=$ADVERTISE_IP
ListenPort=$LOGIN_BACKEND_PORT
MaxOnline=$MAX_ONLINE
Version=$CLIENT_VERSION

[Database]
DBServer=$DB_HOST
LoginName=$DB_USER
Password=$DB_PASSWORD
DBName=accounts

[CONSOLE]
debug = true
EOF

render "$GW_ROOT/dbserver/config.ini" <<EOF
[System]
IP=$ADVERTISE_IP
ListenPort=$DB_PORT
MaxOnline=5

[Database]
DBServer=$DB_HOST
LoginName=$DB_USER
Password=$DB_PASSWORD
DBName=godswar
EOF

# GameServer registers with the LoginServer through the shim (login_shim.py),
# not directly: that link is how the shim hands a validate code to the
# GameServer when a client asks for the game server.
render "$GW_ROOT/gameserver/config.ini" <<EOF
[LoginServer]
Port=$LOGIN_PORT
IP=$ADVERTISE_IP

[System]
ID=0
IP=$ADVERTISE_IP
ListenPort=$GAME_BACKEND_PORT
ServerName=$SERVER_NAME
MaxOnline=$MAX_ONLINE

[Database]
DBServer=$DB_HOST
LoginName=$DB_USER
Password=$DB_PASSWORD
DBName=godswar

[DBServer]
IP=$ADVERTISE_IP
Port=$DB_PORT
EOF

# ---------------------------------------------------------------------------
# X (Wine may pop message boxes on fatal errors; never let that block us)
# ---------------------------------------------------------------------------
Xvfb :99 -screen 0 1024x768x16 >/dev/null 2>&1 &
XVFB_PID=$!
export DISPLAY=:99

export WINEDEBUG="$GW_WINEDEBUG"
if [ "$GW_WINEDBG_ENABLE" = "1" ]; then
  # Drop the winedbg.exe=d override so a fault dumps a backtrace.
  export WINEDLLOVERRIDES="mscoree,mshtml="
  log "winedbg enabled (crash debugger will attach)"
fi
[ "$GW_DEBUG" = "1" ] && log "debug mode: daemon output attached to terminal"

cleanup() {
  log "shutting down"
  trap - TERM INT
  for pid in "${PIDS[@]:-}"; do kill "$pid" 2>/dev/null; done
  wineserver -k >/dev/null 2>&1 || true
  kill "$XVFB_PID" 2>/dev/null
  exit 0
}
trap cleanup TERM INT

# ---------------------------------------------------------------------------
# Daemon helpers
# ---------------------------------------------------------------------------
declare -a PIDS=()
declare -A DAEMONS=()     # name -> "dir|exe"

wait_port() { # wait_port <port> <timeout-seconds> [label]
  local port="$1" timeout="${2:-120}" label="${3:-port $1}"
  local deadline=$((SECONDS + timeout))
  while [ $SECONDS -lt $deadline ]; do
    if nc -z 127.0.0.1 "$port" 2>/dev/null; then
      log "$label is listening on $port"
      return 0
    fi
    sleep 2
  done
  log "WARNING: $label did not open $port within ${timeout}s"
  return 1
}

launch() { # launch <name> <dir> <exe>
  local name="$1" dir="$2" exe="$3"
  DAEMONS["$name"]="$dir|$exe"
  log "starting $name"
  if [ "$GW_DEBUG" = "1" ]; then
    ( cd "$dir" && exec wine "$exe" ) 2>&1 &
  else
    ( cd "$dir" && exec wine "$exe" ) >>"$LOG_DIR/$name.log" 2>&1 &
  fi
  PIDS+=("$!")
  eval "PID_$name=$!"
}

ensure_relay() { # ensure_relay <public-port> <target-ip>
  local port="$1" target="$2"
  [ -z "$CONTAINER_IP" ] && return 0
  # Wildcard bind? Then the published port already reaches the daemon.
  if ss -ltn 2>/dev/null | awk '{print $4}' | grep -qE "(^|:)${port}$" && \
     ss -ltn 2>/dev/null | awk '{print $4}' | grep -q "0.0.0.0:${port}$"; then
    log "port $port already bound to all interfaces; no relay needed"
    return 0
  fi
  log "relaying ${CONTAINER_IP}:${port} -> ${target}:${port}"
  ( exec socat "TCP-LISTEN:${port},bind=${CONTAINER_IP},reuseaddr,fork,backlog=64" \
      "TCP:${target}:${port}" ) >>"$LOG_DIR/relay-$port.log" 2>&1 &
  sleep 1
}

# ---------------------------------------------------------------------------
# Startup sequence
# ---------------------------------------------------------------------------
launch LoginServer "$GW_ROOT/loginserver" LoginServer.exe
wait_port "$LOGIN_BACKEND_PORT" 90 "LoginServer"

# Front end that answers the client's login packet (see docker/tools/login_shim.py).
# It binds every interface, so the published port reaches it without a relay.
start_login_shim() {
  log "starting login shim on $LOGIN_PORT -> $LOGIN_BACKEND_PORT"
  # PYTHONHOME points at the Windows Python 2.5 that GameServer.exe embeds;
  # it must not leak into the container's own python3.
  ( exec env -u PYTHONHOME -u PYTHONPATH \
      python3 -u /usr/local/bin/login_shim.py \
      --listen "0.0.0.0:$LOGIN_PORT" \
      --upstream "127.0.0.1:$LOGIN_BACKEND_PORT" \
      --standalone \
      --advertise-port "$GAME_PORT" ) \
    >>"$LOG_DIR/login_shim.log" 2>&1 &
  SHIM_PID=$!
  PIDS+=("$SHIM_PID")
}
start_login_shim
wait_port "$LOGIN_PORT" 30 "login shim"
ensure_relay "$LOGIN_PORT" "$ADVERTISE_IP"

launch DBServer "$GW_ROOT/dbserver" DBServer.exe
wait_port "$DB_PORT" 60 "DBServer"

log "starting GameServer (loading maps and data, this takes a few minutes)"
launch GameServer "$GW_ROOT/gameserver" GameServer.exe
wait_port "$GAME_BACKEND_PORT" 900 "GameServer"

# Capture/proxy for the game port: logs every client packet (types and sizes)
# while the daemon stays on the private port.
start_game_shim() {
  log "starting game shim on $GAME_PORT -> $GAME_BACKEND_PORT"
  ( exec env -u PYTHONHOME -u PYTHONPATH \
      python3 -u /usr/local/bin/login_shim.py \
      --listen "0.0.0.0:$GAME_PORT" \
      --upstream "127.0.0.1:$GAME_BACKEND_PORT" \
      --no-inject ) >>"$LOG_DIR/game_shim.log" 2>&1 &
  GAME_SHIM_PID=$!
  PIDS+=("$GAME_SHIM_PID")
}
start_game_shim
wait_port "$GAME_PORT" 60 "game shim"
ensure_relay "$GAME_PORT" "$ADVERTISE_IP"

# Mirror daemon logs to the container's stdout so `docker compose logs` works.
if [ "$GW_DEBUG" != "1" ]; then
  tail -n +1 -F "$LOG_DIR"/LoginServer.log "$LOG_DIR"/login_shim.log \
    "$LOG_DIR"/DBServer.log "$LOG_DIR"/GameServer.log \
    "$LOG_DIR"/game_shim.log 2>/dev/null &
  TAIL_PID=$!
fi

# ---------------------------------------------------------------------------
# Supervisor
# ---------------------------------------------------------------------------
log "supervising: ${!DAEMONS[*]}"
while :; do
  sleep 5
  if [ -n "${SHIM_PID:-}" ] && ! kill -0 "$SHIM_PID" 2>/dev/null; then
    log "login shim exited; restarting in 2s"
    sleep 2
    start_login_shim
  fi
  if [ -n "${GAME_SHIM_PID:-}" ] && ! kill -0 "$GAME_SHIM_PID" 2>/dev/null; then
    log "game shim exited; restarting in 2s"
    sleep 2
    start_game_shim
  fi
  for name in "${!DAEMONS[@]}"; do
    var="PID_$name"
    pid="${!var:-}"
    [ -z "$pid" ] && continue
    if ! kill -0 "$pid" 2>/dev/null; then
      IFS='|' read -r dir exe <<<"${DAEMONS[$name]}"
      log "$name exited; restarting in 5s"
      sleep 5
      ( cd "$dir" && exec wine "$exe" ) >>"$LOG_DIR/$name.log" 2>&1 &
      eval "PID_$name=$!"
    fi
  done
done
