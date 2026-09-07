#!/bin/sh
set -eu

LOCK_HASH_FILE="/workspace/frontend/node_modules/.lock_hash"
CURRENT_HASH=""

if [ -f "/workspace/frontend/package-lock.json" ]; then
  CURRENT_HASH=$(sha256sum /workspace/frontend/package-lock.json | awk '{print $1}')
elif [ -f "/workspace/frontend/package.json" ]; then
  CURRENT_HASH=$(sha256sum /workspace/frontend/package.json | awk '{print $1}')
fi

if [ ! -d "/workspace/frontend/node_modules" ] || [ ! -f "$LOCK_HASH_FILE" ] || [ "$(cat "$LOCK_HASH_FILE" 2>/dev/null)" != "$CURRENT_HASH" ]; then
  echo "Dependencies missing or changed. Installing frontend dependencies..."
  npm install --prefer-offline --no-audit
  mkdir -p /workspace/frontend/node_modules
  echo "$CURRENT_HASH" > "$LOCK_HASH_FILE"
else
  echo "Frontend dependencies are up to date. Skipping npm install."
fi

echo "Starting Nuxt development server..."
exec npm run dev
