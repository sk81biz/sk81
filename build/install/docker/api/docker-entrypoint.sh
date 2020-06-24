#!/bin/bash

MYSQLIP=`getent hosts mysql | awk '{ print $1 }'`
KAFKAIP=`getent hosts kafka | awk '{ print $1 }'`

tmp=$(mktemp)
jq ".ConnectionStrings.default.connectionString=\"Server=${MYSQLIP};Port=3306;Database=${MYSQL_DATABASE};User ID=${MYSQL_USER};Password=${MYSQL_PASSWORD};Pooling=true;Character Set=utf8;AutoEnlist=false;SSL Mode=Required\"" /app/onlyoffice/config/appsettings.test.json > "$tmp" && mv "$tmp" /app/onlyoffice/config/appsettings.test.json

tmp=$(mktemp)
jq ".kafka.BootstrapServers=\"${KAFKAIP}\"" /app/onlyoffice/config/kafka.test.json > "$tmp" && mv "$tmp" /app/onlyoffice/config/kafka.test.json

/usr/bin/supervisord --
#cd /var/www/studio/api/
#dotnet ASC.Web.Api.dll --urls=http://0.0.0.0:5000 --pathToConf=/app/onlyoffice/config/ --$STORAGE_ROOT=/app/onlyoffice/data/ --ENVIRONMENT=test
