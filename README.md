# Hard Mode Patch

A Big Ambitions mod that rebalances other Workshop mods so they fit a hard game. It changes nothing on its own. When one of the mods below is installed, it patches that mod while the game runs, and every change can be switched off under Settings > Mods.

**Status:** version 0.1.0, built against game build 3682. It has not been run in the game yet. Only the Hardcore Ambitions patch exists so far; the rest are planned in [docs/plan.md](docs/plan.md).

## What it changes

| Mod | Change | Option |
| --- | --- | --- |
| [Hardcore Ambitions](https://steamcommunity.com/sharedfiles/filedetails/?id=3809287926) | You still start with $551 and the $75,454 student loan, but the game keeps the difficulty settings you chose instead of the mod's 23 preset values. | Keep my difficulty settings |
| | No starting investment funds. | Start without the investment funds |
| | The mod's higher vehicle and boat prices stay in Hardcore saves and are put back to normal when you load any other save in the same session. | Hardcore vehicle prices only in Hardcore saves |

Planned next: Meaningful Ambience, Internet Cafe, Bakery, Vending Machines and Empire Bank.

The student loan keeps Hardcore Ambitions' own terms: $16 interest and a $158 payment a day.

Hardcore Ambitions sets up only the first save you load after starting the game. Restart the game before loading a different save. The log says so when it happens.

## How it decides what to patch

Each patched mod updates on its own schedule, and this patch can't control that. When a mod loads, the patch checks it:

- **Patched (reviewed version):** the DLL matches a version whose code was read for this patch.
- **Patched (new version, not yet reviewed):** the DLL is new, but every method the patch hooks is still there with the same signature. It is patched, and `Player.log` carries a warning.
- **Not patched:** a hooked method is gone or changed. That mod runs exactly as its author published it until this patch is updated.

The state of each mod shows on the options page and in `Player.log` on lines starting `[Hard Mode Patch]`.

## Install

Subscribe on the Workshop (not published yet), or copy `build/HardModePatch` into `%USERPROFILE%\AppData\LocalLow\Hovgaard Games\Big Ambitions\ModsLocal`.

Mods load for every save. Back up any save you care about before loading it with this or the mods it patches.

## Building

Needs the .NET SDK (8 or newer) and the game's managed DLLs, which belong to Hovgaard Games and are never committed.

```bash
tools/fetch-game-dlls.sh <ssh host of the Windows game PC>
tools/build.sh
dotnet test tests/HardModePatch.Core.Tests.csproj
```

`tools/build.sh` compiles with the SDK's C# compiler against the game's own `mscorlib`, so the package runs on the game's Mono runtime. Harmony 2.4.2 is downloaded and checked against a pinned hash.

Rules and target versions without game code live in `Core/` and are covered by the tests. Game-facing code is in `src/`, one file per patched mod.

## Licence

MIT, see [LICENSE.txt](LICENSE.txt). Harmony is MIT, and its licence ships in `Dependencies\`. The patched mods belong to their authors; this repository contains none of their code.
