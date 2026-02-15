#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION="${1:-Debug}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet not found. Install .NET SDK 8 first." >&2
  exit 1
fi

echo "== Restore =="
dotnet restore ./CbetaTranslator.App.sln

echo "== Build ($CONFIGURATION) =="
dotnet build ./CbetaTranslator.App.sln -c "$CONFIGURATION" --no-restore

echo "== Done =="
echo "Run app: dotnet run --project ./CbetaTranslator.App.csproj -c $CONFIGURATION"
