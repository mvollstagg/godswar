#!/bin/bash
# Toggle the in-game GM flag for a character.
#
# Usage:
#   docker/tools/gm.sh <character-name>          # grant GM
#   docker/tools/gm.sh <character-name> --off    # revoke GM
#
# Run from the repo root. GM commands are typed in normal chat (see
# GW 2.0/Server Files (Source)/GameServer/ChatCommand.cpp).
set -euo pipefail

NAME="${1:?usage: gm.sh <character-name> [--off]}"
FLAG=1
[ "${2:-}" = "--off" ] && FLAG=0

# shellcheck disable=SC1091
if [ -f .env ]; then set -a; . ./.env; set +a; fi
if [ -f docker/.env ]; then set -a; . ./docker/.env; set +a; fi
DB_PASSWORD="${DB_PASSWORD:-godswar}"

docker compose exec -T db mariadb -uroot -p"$DB_PASSWORD" godswar -e \
  "UPDATE character_base SET GM=$FLAG WHERE name='${NAME//\'/\'\'}'; SELECT id,name,GM FROM character_base WHERE name='${NAME//\'/\'\'}';"
