#!/bin/bash
set -e

if psql -h simpldb --username postgres -lqt | cut -d \| -f 1 | grep -qw ECommerce; then
    echo "ECommerce database existed"
else
    echo "create new database ECommerce"
	psql -h simpldb --username postgres -c "CREATE DATABASE ECommerce WITH ENCODING 'UTF8'"
	psql -h simpldb --username postgres -d ECommerce -a -f /app/dbscript.sql
fi

cd /app && dotnet ECommerce.WebHost.dll 
