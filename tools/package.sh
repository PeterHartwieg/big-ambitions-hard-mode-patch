#!/usr/bin/env bash
# Builds the package and zips it for the Workshop: build/HardModePatch-<version>.zip, its SHA-256 in
# build/HardModePatch-<version>.zip.sha256 and each file's SHA-256 in build/HardModePatch.sha256.
# Refuses when the source version, the assembly version and the changelog's top entry disagree.
set -euo pipefail
REPO="$(cd "$(dirname "$0")/.." && pwd)"
VERSION="$(sed -nE 's/.*public const string Version = "([^"]+)";.*/\1/p' "$REPO/src/HardModePatchMod.cs")"
ASSEMBLY="$(sed -nE 's/.*AssemblyInformationalVersion\("([^"]+)"\).*/\1/p' "$REPO/src/HardModePatchMod.cs")"
CHANGELOG="$(sed -nE 's/^## ([0-9][^ ]*) .*/\1/p' "$REPO/CHANGELOG.md" | head -1)"
[ -n "$VERSION" ] && [ "$VERSION" = "$ASSEMBLY" ] && [ "$VERSION" = "$CHANGELOG" ] \
  || { echo "Version mismatch: source $VERSION, assembly $ASSEMBLY, changelog $CHANGELOG" >&2; exit 1; }
"$REPO/tools/build.sh" >/dev/null
OUT="$REPO/build/HardModePatch"
# The Mod Creator uses the first image in the mod folder's root as the Workshop thumbnail.
[ -f "$REPO/workshop/preview.jpg" ] && cp "$REPO/workshop/preview.jpg" "$OUT/preview.jpg"
cd "$REPO/build"
(cd HardModePatch && find . -type f | sed 's|^\./||' | LC_ALL=C sort | xargs shasum -a 256) > HardModePatch.sha256
ZIP="HardModePatch-$VERSION.zip"
rm -f "$ZIP"
# Fixed timestamps and sorted entries, so the same files make the same zip.
find HardModePatch -exec touch -t 202610220000 {} +
find HardModePatch -type f | LC_ALL=C sort | TZ=UTC zip -q -X -D "$ZIP" -@
shasum -a 256 "$ZIP" > "$ZIP.sha256"
cat "$ZIP.sha256"
