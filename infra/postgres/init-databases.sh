#!/bin/bash
set -e

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "postgres" <<-EOSQL
    CREATE DATABASE meals_db;
    CREATE DATABASE planner_db;
    CREATE DATABASE auth_db;
EOSQL
