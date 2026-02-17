#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION="${1:-Debug}"
PUBLISH="${2:-false}"
RID="${3:-linux-x64}"
SINGLE_FILE="${4:-false}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet not found. Install .NET SDK 8 first." >&2
  exit 1
fi

DOTNET_REAL_PATH="$(readlink -f "$(command -v dotnet)" || true)"
if [ "$PUBLISH" = "true" ] && [ "${CBETA_ALLOW_SNAP_DOTNET:-0}" != "1" ] && [[ "$DOTNET_REAL_PATH" == /usr/bin/snap || "$DOTNET_REAL_PATH" == *"/snap/"* ]]; then
  echo "Refusing self-contained publish with snap dotnet." >&2
  echo "Snap-built Linux apphosts can embed /snap/core20 loader paths and fail at runtime (GLIBC mismatch)." >&2
  echo "Install non-snap .NET SDK and retry, or override with CBETA_ALLOW_SNAP_DOTNET=1." >&2
  exit 2
fi

echo "=== Configuration: $CONFIGURATION ==="
echo "=== Runtime Identifier: $RID ==="
echo "=== Publish: $PUBLISH ==="
echo "=== Single File: $SINGLE_FILE ==="

if [ "$PUBLISH" = "true" ]; then
  echo "=== Restore ==="
  dotnet restore ./CbetaTranslator.App.sln

  echo "=== Publish (self-contained) ==="
  dotnet publish ./CbetaTranslator.App.csproj \
    -c "$CONFIGURATION" \
    -r "$RID" \
    --self-contained true \
    -p:PublishSingleFile="$SINGLE_FILE" \
    -p:IncludeNativeLibrariesForSelfExtract="$SINGLE_FILE" \
    -p:EnableCompressionInSingleFile=false \
    -o "./publish/$RID"

  echo "=== Published to: ./publish/$RID ==="
  echo "=== Run: ./run-cbeta-selfcontained.sh $RID ==="
else
  echo "=== Restore ==="
  dotnet restore ./CbetaTranslator.App.sln

  echo "=== Build ($CONFIGURATION) ==="
  dotnet build ./CbetaTranslator.App.sln -c "$CONFIGURATION" --no-restore

  echo "=== Done ==="
  echo "=== Run app: dotnet run --project ./CbetaTranslator.App.csproj -c $CONFIGURATION ==="
fi
