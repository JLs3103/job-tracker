#!/usr/bin/env bash
set -Eeuo pipefail

ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$ROOT/backend"
FRONTEND_DIR="$ROOT/frontend"
BACKEND_DLL="$BACKEND_DIR/bin/Debug/net10.0/backend.dll"
VITE_CLI="$FRONTEND_DIR/node_modules/vite/bin/vite.js"

for command_name in dotnet node npm; do
  if ! command -v "$command_name" >/dev/null 2>&1; then
    printf "Required command '%s' was not found. Install the .NET 10 SDK and Node.js.\n" "$command_name" >&2
    exit 1
  fi
done

if ! dotnet --list-sdks | grep -Eq '^10\.'; then
  echo "The .NET 10 SDK is required. Install it and run this script again." >&2
  exit 1
fi

if [[ ! -f "$VITE_CLI" ]]; then
  (cd "$FRONTEND_DIR" && npm ci)
fi

if ! dotnet build "$BACKEND_DIR/backend.csproj" --configuration Debug; then
  if [[ ! -f "$BACKEND_DIR/obj/project.assets.json" ]]; then
    exit 1
  fi
  echo "Restore failed. Retrying with the existing NuGet restore assets..."
  dotnet build "$BACKEND_DIR/backend.csproj" --configuration Debug --no-restore
fi

backend_pid=''
frontend_pid=''
cleanup() {
  trap - EXIT INT TERM
  if [[ -n "$frontend_pid" ]] && kill -0 "$frontend_pid" 2>/dev/null; then
    kill "$frontend_pid"
  fi
  if [[ -n "$backend_pid" ]] && kill -0 "$backend_pid" 2>/dev/null; then
    kill "$backend_pid"
  fi
  [[ -z "$frontend_pid" ]] || wait "$frontend_pid" 2>/dev/null || true
  [[ -z "$backend_pid" ]] || wait "$backend_pid" 2>/dev/null || true
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

(
  cd "$BACKEND_DIR"
  ASPNETCORE_ENVIRONMENT=Development \
    ASPNETCORE_URLS=http://localhost:5227 \
    dotnet "$BACKEND_DLL"
) &
backend_pid=$!

(
  cd "$FRONTEND_DIR"
  VITE_USE_MOCK_API=false \
    VITE_API_BASE_URL=http://localhost:5227 \
    node "$VITE_CLI" --host localhost
) &
frontend_pid=$!

printf '\nJob Tracker is starting:\n'
printf '  App:     http://localhost:5173\n'
printf '  API:     http://localhost:5227\n'
printf '  OpenAPI: http://localhost:5227/openapi/v1.json\n'
printf 'Press Ctrl+C to stop both services.\n\n'

while true; do
  running_jobs="$(jobs -pr)"
  if ! grep -Fxq "$backend_pid" <<<"$running_jobs"; then
    wait "$backend_pid"
    break
  fi
  if ! grep -Fxq "$frontend_pid" <<<"$running_jobs"; then
    wait "$frontend_pid"
    break
  fi
  sleep 1
done
