#!/usr/bin/env bash
# Builds the Hard Mode Patch package with the .NET SDK's C# compiler against the game's own assemblies.
# Usage: tools/build.sh   Output: build/HardModePatch/ (the folder that goes into the game's ModsLocal).
# Needs build/game-dlls from tools/fetch-game-dlls.sh. The game's DLLs and Harmony are never committed.
set -euo pipefail

REPO="$(cd "$(dirname "$0")/.." && pwd)"
GAME_DLLS="${BA_GAME_DLLS:-$REPO/build/game-dlls}"
HARMONY_URL="https://api.nuget.org/v3-flatcontainer/lib.harmony/2.4.2/lib.harmony.2.4.2.nupkg"
HARMONY_SHA256="d64592e53090464559fce48612c9ca7c8dc73113841376b7aa3455f46fc5d579"
HARMONY_DLL_SHA256="7b9e756306fa3d7620e02a857c8927a6ab04973f9bd8a77d3866700a6deac55c"
DEPS="$REPO/build/deps"
OUT="$REPO/build/HardModePatch"

[ -f "$GAME_DLLS/BigAmbitions.dll" ] && [ -f "$GAME_DLLS/mscorlib.dll" ] || { echo "Game DLLs not found in $GAME_DLLS; run tools/fetch-game-dlls.sh" >&2; exit 1; }

# Harmony, checksum-pinned.
mkdir -p "$DEPS"
if [ ! -f "$DEPS/0Harmony.dll" ]; then
  tmp="$(mktemp -d)"
  curl -fsSL "$HARMONY_URL" -o "$tmp/harmony.nupkg"
  echo "$HARMONY_SHA256  $tmp/harmony.nupkg" | shasum -a 256 -c - >/dev/null
  unzip -q -o "$tmp/harmony.nupkg" 'lib/net472/0Harmony.dll' LICENSE -d "$tmp"
  cp "$tmp/lib/net472/0Harmony.dll" "$DEPS/0Harmony.dll"
  cp "$tmp/LICENSE" "$DEPS/Harmony-LICENSE.txt"
  rm -rf "$tmp"
fi
echo "$HARMONY_DLL_SHA256  $DEPS/0Harmony.dll" | shasum -a 256 -c - >/dev/null || { echo "Cached 0Harmony.dll does not match the pinned hash; delete it and rebuild" >&2; exit 1; }

# The newest installed SDK's Roslyn compiler.
SDK_DIR="$(dotnet --list-sdks | tail -1 | sed -E 's/^([^ ]+) \[(.*)\]$/\2\/\1/')"
CSC="$SDK_DIR/Roslyn/bincore/csc.dll"
[ -f "$CSC" ] || { echo "C# compiler not found at $CSC" >&2; exit 1; }

rm -rf "$OUT"
mkdir -p "$OUT/Dependencies" "$OUT/Locales" "$OUT/docs"
REFS=()
for dll in mscorlib System System.Core netstandard BigAmbitions BigAmbitions.Items DayNightCycle BigAmbitions.ModAPI HGExtensions HGPlugins UnityEngine.CoreModule; do
  REFS+=("-reference:$GAME_DLLS/$dll.dll")
done
REFS+=("-reference:$DEPS/0Harmony.dll")
# The game runs on Mono with its own mscorlib, so nothing from the SDK's framework is referenced.
dotnet "$CSC" -nologo -noconfig -nostdlib+ -target:library -langversion:9 -optimize+ -warn:4 -deterministic+ \
  -out:"$OUT/HardModePatch.dll" "${REFS[@]}" "$REPO"/Core/*.cs "$REPO"/src/*.cs

# The package root holds exactly one DLL, the mod's; the loader loads Dependencies\ first.
cp "$DEPS/0Harmony.dll" "$OUT/Dependencies/0Harmony.dll"
cp "$DEPS/Harmony-LICENSE.txt" "$OUT/Dependencies/Harmony-LICENSE.txt"
cp "$REPO"/config/Locales/*.json "$OUT/Locales/"
cp "$REPO/README.md" "$REPO/LICENSE.txt" "$OUT/"
cp "$REPO/docs/hyperreal-map-return.md" "$REPO/docs/factory-diplomas.md" "$OUT/docs/"
shasum -a 256 "$OUT/HardModePatch.dll"
