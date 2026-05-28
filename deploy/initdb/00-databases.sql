-- Creates the two application databases on first PostgreSQL boot.
-- Executed by docker-entrypoint-initdb.d when the data volume is empty.
-- The POSTGRES_USER from the env file is the superuser that runs this.

CREATE DATABASE appdb;
CREATE DATABASE orleansdb;
