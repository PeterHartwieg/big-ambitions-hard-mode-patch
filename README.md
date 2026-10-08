# Hard Mode Patch

A Big Ambitions mod that rebalances other Workshop mods so they fit a hard game. It changes nothing on its own. When one of the mods below is installed, it patches that mod while the game runs, and every change can be switched off under Settings > Mods.

**Status:** version 0.1.0, built against game build 3682. All six patches are written, and an offline check resolves and applies every hook against the reviewed versions of the six mods. Three review rounds with three reviewers are done; all three cleared it for in-game testing. Nothing has been run in the game yet.

## What it changes

| Mod | Change | Option |
| --- | --- | --- |
| [Hardcore Ambitions](https://steamcommunity.com/sharedfiles/filedetails/?id=3809287926) | You still start with $551 and the $75,454 student loan, but the game keeps the difficulty settings you chose instead of the mod's 23 preset values. | Keep my difficulty settings |
| | No starting investment funds. | Start without the investment funds |
| | The mod's higher car prices stay in Hardcore saves and go back to normal when you load any other save in the same session. (Its boat prices never take effect on game build 3682: boats aren't the kind of game object it edits.) | Hardcore vehicle prices only in Hardcore saves |
| [Meaningful Ambience](https://steamcommunity.com/sharedfiles/filedetails/?id=3810641423) | Decor adds a share of the customers the game sends (up to the preset, for example 10%), instead of a share of the building's capacity every open hour. Low customer promotion, marketing and quiet hours now carry through. | Decor adds a share of the customers the game sends |
| [Bakery](https://steamcommunity.com/sharedfiles/filedetails/?id=3810927743) | Prices at vanilla bakery margins: bread $3.00 wholesale / $7.50 market, bun $2.00 / $5.00, chocolate cookie $1.50 / $3.80, latte $1.10 / $4.50, americano $0.90 / $3.90. Businesses that already set their own selling prices keep them. | Vanilla bakery margins (needs a restart) |
| [Internet Cafe](https://steamcommunity.com/sharedfiles/filedetails/?id=3796860957) | While you're away, a cafe serves at most two customers per station per hour, matching its 30-minute sessions. A station is a PC with its own chair on the same desk or table, the way the cafe seats customers when you're there. | At most two customers per PC per hour |
| [Vending Machines](https://steamcommunity.com/sharedfiles/filedetails/?id=3812054455) | While you place a machine in a rival's building, only vending items can be picked up, a sign's "change text" does nothing, and the Interior Designer doesn't open. The rival's furniture stays locked. | Rival furniture stays locked while placing a machine |
| [Empire Bank](https://steamcommunity.com/sharedfiles/filedetails/?id=3781110629) | Interest withdrawals are taxed at your save's tax rate when you take them out. | Tax withdrawals |
| | Neither emergency funding button (Risk Center or the dashboard's liquidity warning) borrows from the central bank. | No emergency central bank borrowing |
| | Applicants keep the credit score they were given; a product's minimum no longer raises everyone to it. | Honest credit scores |
| | Loan APR at most 15%, origination fee at most 3%, loan term at least 6 months, card APR at most 30%. Applies to product edits, products and pending applications already in a save, approvals, corporate facility offers and accepted offers not yet finalised. Switching the option on mid-game applies it to the open bank at once. Contracts already signed keep their terms. | Product limits |
| | No requests to the author's statistics and supporters servers. The opt-in heartbeat in Empire Bank's own settings is unchanged. | No requests to the author's servers |

The student loan keeps Hardcore Ambitions' own terms: $16 interest and a $158 payment a day, also after you change the interest setting mid-game.

Hardcore Ambitions sets up only the first save you load after starting the game. Restart the game before loading a different save. The log says so when it happens.

## How it decides what to patch

Each patched mod updates on its own schedule, and this patch can't control that. When a mod loads, the patch checks it:

- **Patched (reviewed version):** the DLL matches a version whose code was read for this patch.
- **Patched (new version, not yet reviewed):** the DLL is new, but every method the patch hooks is still there with the same signature, and every method body the patch skips or replaces is unchanged instruction for instruction. It is patched, and `Player.log` carries a warning.
- **Not patched:** a hooked method is gone or changed, or a body the patch replaces was rewritten. That mod runs exactly as its author published it until this patch is updated.

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
tools/check-hooks.sh <folder holding the target mods' DLLs>
```

`tools/check-hooks.sh` loads the built patch, the game's assemblies and the target mods' DLLs in .NET, resolves every hook and applies every patch with Harmony, then removes them. It catches renamed methods, changed signatures and mismatched patch parameters without starting the game. It needs the Unity modules in `build/game-dlls` as well; copy every `Unity*.dll` from the game's `Managed` folder there.

`tools/build.sh` compiles with the SDK's C# compiler against the game's own `mscorlib`, so the package runs on the game's Mono runtime. Harmony 2.4.2 is downloaded and checked against a pinned hash.

Rules and target versions without game code live in `Core/` and are covered by the tests. Game-facing code is in `src/`, one file per patched mod.

## Licence

MIT, see [LICENSE.txt](LICENSE.txt). Harmony is MIT, and its licence ships in `Dependencies\`. The patched mods belong to their authors; this repository contains none of their code.
