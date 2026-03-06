#!/usr/bin/env zsh

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"
API_PROJECT="$ROOT_DIR/GobTrackerApi/GobTrackerApi.csproj"
MAUI_PROJECT="$ROOT_DIR/MauiApp1/MauiApp1.csproj"
API_SETTINGS="$ROOT_DIR/GobTrackerApi/appsettings.Development.json"
API_HEALTH_URL="http://127.0.0.1:5117/api/health"

cd "$ROOT_DIR"

if ! az account show >/dev/null 2>&1; then
  echo "Azure login is required for Active Directory auth. Run: az login"
  exit 1
fi

echo "Starting local API..."
ASPNETCORE_ENVIRONMENT="Development" ASPNETCORE_URLS="http://127.0.0.1:5117" dotnet run --project "$API_PROJECT" >/tmp/gobtracker-api.log 2>&1 &
API_PID=$!

cleanup() {
  if ps -p "$API_PID" >/dev/null 2>&1; then
    echo "Stopping local API..."
    kill "$API_PID" >/dev/null 2>&1 || true
  fi
}

trap cleanup EXIT INT TERM

echo "Waiting for API to become healthy..."
for i in {1..30}; do
  if curl -fsS "$API_HEALTH_URL" >/dev/null 2>&1; then
    echo "API is ready."
    break
  fi
  sleep 1
done

if ! curl -fsS "$API_HEALTH_URL" >/dev/null 2>&1; then
  echo "API failed to start. Check logs: /tmp/gobtracker-api.log"
  exit 1
fi

echo "Starting MAUI app..."
dotnet run --project "$MAUI_PROJECT" -f net10.0-maccatalyst

echo "MAUI launched. API is still running on http://127.0.0.1:5117"
echo "Press Ctrl+C in this terminal to stop the API."
while ps -p "$API_PID" >/dev/null 2>&1; do
  sleep 1
done