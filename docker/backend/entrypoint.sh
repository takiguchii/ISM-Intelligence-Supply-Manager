#!/usr/bin/env bash
set -euo pipefail

echo "Waiting for MySQL to accept TCP connections..."
until (echo > /dev/tcp/mysql/3306) >/dev/null 2>&1; do
  sleep 2
done

echo "Restoring backend dependencies..."
dotnet restore ISM.sln

echo "Applying Entity Framework migrations..."
dotnet ef database update \
  --project src/ISM.Infrastructure \
  --startup-project src/ISM.API

echo "Starting ASP.NET Core with hot reload..."
exec dotnet watch --project src/ISM.API run --no-launch-profile --urls http://0.0.0.0:8080
