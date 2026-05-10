#!/usr/bin/env bash
set -euo pipefail

curl --fail --silent http://localhost:8080/api/system/status >/dev/null
curl --fail --silent http://localhost:8080/swagger/index.html >/dev/null
curl --fail --silent http://localhost:3000 >/dev/null

echo "ISM smoke check completed successfully."
