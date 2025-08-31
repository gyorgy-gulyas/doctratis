-- Keyspace (RF: 2 in 'docratis' DC - adjust as needed)
CREATE KEYSPACE IF NOT EXISTS __KEYSPACE__
WITH replication = {'class': 'NetworkTopologyStrategy', '__DC__': __RF__}
AND durable_writes = true;

-- Application user
CREATE ROLE IF NOT EXISTS __APP_USER__ WITH PASSWORD='__APP_PASS__' AND LOGIN=true;

-- Permissions
GRANT ALL PERMISSIONS ON KEYSPACE __KEYSPACE__ TO __APP_USER__;

-- (optional) create own admin and disable default cassandra user
-- CREATE ROLE IF NOT EXISTS scylla_admin WITH PASSWORD='VeryStrong#Passw0rd' AND SUPERUSER=true AND LOGIN=true;
-- ALTER ROLE cassandra WITH LOGIN=false;
