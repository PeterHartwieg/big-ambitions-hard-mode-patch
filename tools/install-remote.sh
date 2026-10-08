#!/usr/bin/env bash
# Copies build/HardModePatch into ModsLocal on the Windows game PC, replacing an earlier copy.
# Usage: tools/install-remote.sh [ssh host]   (or set BA_GAME_HOST). Run tools/build.sh first.
# A Workshop copy of this mod beats a local one; disable it before testing a local build.
set -euo pipefail
REPO="$(cd "$(dirname "$0")/.." && pwd)"
HOST="${1:-${BA_GAME_HOST:?Pass the ssh host of the Windows game PC, or set BA_GAME_HOST}}"
PKG="$REPO/build/HardModePatch"
[ -f "$PKG/HardModePatch.dll" ] || { echo "No build at $PKG; run tools/build.sh" >&2; exit 1; }
# Inline PowerShell over ssh mangles quoting, so the swap runs from a script file.
tmp="$(mktemp -d)"
cat > "$tmp/install-hardmodepatch.ps1" <<'PS1'
$ErrorActionPreference = 'Stop'
$mods = Join-Path $env:USERPROFILE 'AppData\LocalLow\Hovgaard Games\Big Ambitions\ModsLocal'
$staged = Join-Path $env:USERPROFILE 'HardModePatch-staged'
$target = Join-Path $mods 'HardModePatch'
if (Test-Path $target) { Remove-Item $target -Recurse -Force }
Move-Item $staged $target
(Get-FileHash (Join-Path $target 'HardModePatch.dll') -Algorithm SHA256).Hash.Substring(0, 16).ToLower()
PS1
ssh "$HOST" 'powershell -NoProfile -Command "Remove-Item -Recurse -Force $env:USERPROFILE\HardModePatch-staged -ErrorAction SilentlyContinue"' < /dev/null
scp -q -r "$PKG" "$HOST:HardModePatch-staged" < /dev/null
scp -q "$tmp/install-hardmodepatch.ps1" "$HOST:install-hardmodepatch.ps1" < /dev/null
echo "Installed on $HOST, DLL hash $(ssh "$HOST" 'powershell -NoProfile -ExecutionPolicy Bypass -File install-hardmodepatch.ps1' < /dev/null | tr -d '\r')"
rm -rf "$tmp"
