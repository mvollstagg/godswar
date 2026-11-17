-- GodsWar Online: PostgreSQL schemas
-- Replaces the two legacy MySQL databases (accounts, godswar) with two schemas
-- inside a single PostgreSQL database for clean cross-schema queries & views.

CREATE SCHEMA IF NOT EXISTS accounts;
CREATE SCHEMA IF NOT EXISTS godswar;
