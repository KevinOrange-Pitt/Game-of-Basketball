#!/usr/bin/env zsh

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"
API_PROJECT="$ROOT_DIR/GobTrackerApi/GobTrackerApi.csproj"
MAUI_PROJECT="$ROOT_DIR/MauiApp1/MauiApp1.csproj"
API_HEALTH_URL="http://127.0.0.1:5117/api/health"
API_LOG_FILE="/tmp/gobtracker-api.log"
API_PID=""
STARTED_API="0"
REUSE_API="${REUSE_API:-0}"

if [[ $# -gt 0 ]]; then
  MAUI_FRAMEWORK="$1"
else
  case "$(uname -s)" in
    Darwin)
      MAUI_FRAMEWORK="net10.0-maccatalyst"
      ;;
    Linux)
      MAUI_FRAMEWORK="net10.0-android"
      ;;
    *)
      MAUI_FRAMEWORK="net10.0-maccatalyst"
      ;;
  esac
fi

require_command() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Missing required command: $1"
    exit 1
  fi
}

print_api_log_tail() {
  if [[ -f "$API_LOG_FILE" ]]; then
    echo "--- API log tail ($API_LOG_FILE) ---"
    tail -n 40 "$API_LOG_FILE"
    echo "--- end log ---"
  fi
}

is_api_healthy() {
  curl -fsS "$API_HEALTH_URL" >/dev/null 2>&1
}

is_pid_alive() {
  local pid="$1"
  [[ -n "$pid" ]] && ps -p "$pid" >/dev/null 2>&1
}

cd "$ROOT_DIR"

require_command dotnet
require_command curl

cleanup() {
  if [[ "$STARTED_API" == "1" ]] && is_pid_alive "$API_PID"; then
    echo "Stopping local API..."
    kill "$API_PID" >/dev/null 2>&1 || true
  fi
}

trap cleanup EXIT INT TERM

if is_api_healthy; then
  if [[ "$REUSE_API" == "1" ]]; then
    echo "API already running on $API_HEALTH_URL. Reusing existing API process (REUSE_API=1)."
  else
    echo "API already running on $API_HEALTH_URL. Restarting to pick up latest code..."
    pkill -f "dotnet run --project $API_PROJECT" >/dev/null 2>&1 || true
    sleep 1
    ASPNETCORE_ENVIRONMENT="Development" ASPNETCORE_URLS="http://127.0.0.1:5117" dotnet run --project "$API_PROJECT" >"$API_LOG_FILE" 2>&1 &
    API_PID=$!
    STARTED_API="1"
  fi
else
  echo "Starting local API..."
  ASPNETCORE_ENVIRONMENT="Development" ASPNETCORE_URLS="http://127.0.0.1:5117" dotnet run --project "$API_PROJECT" >"$API_LOG_FILE" 2>&1 &
  API_PID=$!
  STARTED_API="1"
fi

echo "Waiting for API to become healthy..."
for _ in {1..30}; do
  if is_api_healthy; then
    echo "API is ready."
    break
  fi

  if [[ "$STARTED_API" == "1" ]] && ! is_pid_alive "$API_PID"; then
    echo "API process exited before becoming healthy."
    print_api_log_tail
    exit 1
  fi

  sleep 1
done

if ! is_api_healthy; then
  echo "API failed to start on $API_HEALTH_URL"
  print_api_log_tail
  exit 1
fi

echo "Launching MAUI app for framework: $MAUI_FRAMEWORK"
if ! dotnet run --project "$MAUI_PROJECT" -f "$MAUI_FRAMEWORK"; then
  echo "MAUI launch failed for framework '$MAUI_FRAMEWORK'."
  echo "Tip: try './run-dev.sh net10.0-maccatalyst' on macOS."
  exit 1
fi

echo "MAUI launched. API is still running on http://127.0.0.1:5117"
echo "Press Ctrl+C in this terminal to stop the API."
while [[ "$STARTED_API" == "1" ]] && is_pid_alive "$API_PID"; do
  sleep 1
done