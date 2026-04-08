#!/usr/bin/env bash
set -euo pipefail

# Get the directory where this script is located
DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

cd "$DIR"
echo "Starting Read Zen (development build)..."
exec dotnet run --project ./ReadZen.App.csproj -c Release

