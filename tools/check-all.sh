#!/usr/bin/env bash
# Builds, then runs the core tests and every offline check. Run before a release.
# Usage: tools/check-all.sh <target DLL folder> <unreviewed RealLifeOverhaul.dll> <DynamicRivals.dll> <HARTRivalBalance.dll>
#   The target folder holds the reviewed RealLifeOverhaul.dll and whichever of Bakery, PolyModsHyperreal, InternetCafe,
#   MeaningfulAmbience, HardcoreAmbitions, VendingMachines and EmpireBank are available (HookCheck skips the rest).
#   The unreviewed RLO is a reviewed release with one byte appended: same shapes, unknown hash.
set -euo pipefail
REPO="$(cd "$(dirname "$0")/.." && pwd)"
[ $# -eq 4 ] || { sed -n 3,6p "$0" >&2; exit 2; }
TARGETS="$1"; UNREVIEWED="$2"; DR="$3"; RB="$4"
G="$REPO/build/game-dlls"; D="$REPO/build/HardModePatch/HardModePatch.dll"; R="$TARGETS/RealLifeOverhaul.dll"
"$REPO/tools/build.sh"
dotnet test "$REPO/tests/HardModePatch.Core.Tests.csproj" | tail -1
"$REPO/tools/check-hooks.sh" "$TARGETS"
for check in ScenarioCheck FactoryCheck CostCheck; do dotnet run --project "$REPO/tools/$check" -- "$G" "$D" "$R" | tail -1; done
dotnet run --project "$REPO/tools/CompatibilityCheck" -- "$G" "$D" "$TARGETS" | tail -1
dotnet run --project "$REPO/tools/RloGateCheck" -- "$G" "$D" "$UNREVIEWED" | tail -1
dotnet run --project "$REPO/tools/SettingsCheck" -- "$G" "$D" "$R" "$DR" "$RB" "$REPO/tools/export-series-settings.ps1" | tail -1
