#!/bin/bash
# GodsWar: create the two schemas and import the legacy dumps.
# The original setup guide requires godswar to be imported before accounts
# (several stored procedures in accounts/godswar reference across schemas).
set -euo pipefail

MYSQL=(mysql -uroot -p"${MYSQL_ROOT_PASSWORD}" --protocol=socket)

echo "[gw-db] creating schemas"
"${MYSQL[@]}" <<'SQL'
CREATE DATABASE IF NOT EXISTS godswar DEFAULT CHARACTER SET utf8 COLLATE utf8_unicode_ci;
CREATE DATABASE IF NOT EXISTS accounts DEFAULT CHARACTER SET utf8 COLLATE utf8_unicode_ci;
SQL

for db in godswar accounts; do
  dump="/restore/${db}.sql"
  if [ ! -f "$dump" ]; then
    echo "[gw-db] ERROR: missing $dump" >&2
    exit 1
  fi
  echo "[gw-db] importing ${db}.sql"
  "${MYSQL[@]}" --default-character-set=utf8 "$db" < "$dump"
done

SEED="/docker-entrypoint-initdb.d/seed/10-seed.sql"
if [ -f "$SEED" ]; then
  echo "[gw-db] applying seed data"
  "${MYSQL[@]}" --default-character-set=utf8mb4 accounts < "$SEED"
fi

echo "[gw-db] import finished"
