#!/usr/bin/env bash
set -euo pipefail

export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/root}"
export NUGET_PACKAGES="${NUGET_PACKAGES:-/root/.nuget/packages}"
export NUGET_HTTP_CACHE_PATH="${NUGET_HTTP_CACHE_PATH:-/root/.nuget/http-cache}"
mkdir -p "${NUGET_PACKAGES}" "${NUGET_HTTP_CACHE_PATH}"

RESTORE_HASH_FILE="${NUGET_PACKAGES}/.ism_restore_hash"
CURRENT_RESTORE_HASH="$({
  sha256sum ISM.sln
  find . -type f \( -name '*.csproj' -o -name 'packages.lock.json' \) -print0 \
    | sort -z | xargs -0 -r sha256sum
} | sha256sum | awk '{print $1}')"

echo "Waiting for MySQL to accept TCP connections..."
until nc -z -w 2 mysql 3306 >/dev/null 2>&1; do
  sleep 2
done

if [ ! -f "${RESTORE_HASH_FILE}" ] || [ "$(cat "${RESTORE_HASH_FILE}")" != "${CURRENT_RESTORE_HASH}" ]; then
  echo "Backend dependency manifest changed. Restoring dependencies..."
  dotnet restore ISM.sln --packages "${NUGET_PACKAGES}"
  printf '%s' "${CURRENT_RESTORE_HASH}" > "${RESTORE_HASH_FILE}"
else
  echo "Backend dependencies are up to date. Skipping restore."
fi

echo "Starting ASP.NET Core with hot reload..."
exec dotnet watch --project src/ISM.API run --no-restore --no-launch-profile --urls http://0.0.0.0:8080
