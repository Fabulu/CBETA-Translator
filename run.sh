#!/usr/bin/env bash
set -euo pipefail

# Get the directory where this script is located
DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Set library path to include native libraries
export LD_LIBRARY_PATH="$DIR/bin/Debug/net8.0/runtimes/linux-x64/native:$LD_LIBRARY_PATH"

# Run the application
echo "Starting CBETA Translator..."
echo "Library path: $LD_LIBRARY_PATH"
dotnet run --project ./CbetaTranslator.App.csproj -c Debug
