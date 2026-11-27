#!/bin/bash
set -e

sed -i'' -e 's|<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="6.0.0" />|<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="6.0.0" />|' src/ECommerce.WebHost/ECommerce.WebHost.csproj
sed -i'' -e 's/ConfigureService();/AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);\nConfigureService();/' src/ECommerce.WebHost/Program.cs
sed -i'' -e 's/UseSqlServer/UseNpgsql/' src/ECommerce.WebHost/Program.cs
sed -i'' -e 's/UseSqlServer/UseNpgsql/' src/ECommerce.WebHost/Extensions/ServiceCollectionExtensions.cs

rm -rf src/ECommerce.WebHost/Migrations/*

dotnet restore && dotnet build

cd src/ECommerce.WebHost \
	&& dotnet ef migrations add initialSchema \
	&& dotnet ef database update
	
echo "The database schema has been created. Please execute the src/Database/StaticData_Postgres.sql to insert static data."
echo "Then type 'dotnet run' in src/ECommerce.WebHost to start the app."
