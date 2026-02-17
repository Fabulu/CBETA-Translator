#!/usr/bin/env bash
set -euo pipefail

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RID="${1:-linux-x64}"
APP_DIR="$DIR/publish/$RID"
APP="$APP_DIR/CbetaTranslator.App"
SYSTEM_LOADER="/lib64/ld-linux-x86-64.so.2"

if [ ! -x "$APP" ]; then
  echo "Self-contained app not found: $APP"
  echo "Build it first with: ./eng/build-linux.sh Release true $RID"
  exit 1
fi

# Ensure native libs (SkiaSharp, etc.) resolve from the publish directory first.
export LD_LIBRARY_PATH="$APP_DIR${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}"

# Snap-built apphosts embed a snap loader path that can break on newer distros.
# If detected, launch through the system loader instead.
if grep -aq '/snap/core20/current/lib64/ld-linux-x86-64.so.2' "$APP" && [ -x "$SYSTEM_LOADER" ]; then
  exec "$SYSTEM_LOADER" "$APP"
fi

exec "$APP"
