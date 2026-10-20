# Hard Mode Patch

A Big Ambitions mod with an optional indebted starting scenario and runtime balance patches for other Workshop mods. Every change has an option under Settings > Mods. The scenario is off by default; with it off and no target mod installed, this changes nothing.

**Status:** version 0.4.1, built against game build 3682. Includes Bakery and Hyperreal discovery optimizations, the standalone starting scenario, rent and happiness rebalances. 94 core tests and 32 focused offline performance checks pass; all seven target patches resolve/apply against the locally reviewed DLLs. The 38 scenario/rebalance checks also pass against the currently installed RLO 0.38.0. The performance changes passed Astra and Sol code review after fixes. Their in-game effect still needs an A/B trace; offline checks do not validate live Help or lighting.

## Recurring hitch fixes

Two independent switches are on by default under Settings > Mods > Hard Mode Patch:

- **Avoid repeated Help searches:** Bakery's retries use the game's registered Help instance instead of scanning all loaded objects once a second. Bakery adds its own pages before the game builds Help, including opening it after Bakery's retry timeout and after a language change. Enabling the option also populates an already initialized Help menu. Turning it off restores Bakery's original discovery; entries already added remain until Help reloads.
- **Cache street-light discovery:** Hyperreal seeds its light list once per scene state, then uses light enable/destroy callbacks to update it. Its eight-second scan reuses this list, including while Hyperreal's enhancements are off. Disabled lights remain tracked; newly created inactive lights enter when first enabled. Scene changes and option toggles clear the cache. Hyperreal retains its lighting values, restoration and foliage classification.

These address the repeating ~1-second and ~8-second hitches correlated with Peter's 18 October trace. Improvement is not measured yet. Initial scene discovery and foliage checks when new street lamps appear can still hitch; this does not eliminate every source of stutter.

Performance hooks require the reviewed Bakery `6d68edab0f5600f5` / Hyperreal `7d1bd589947558e2` binaries and their audited game/Unity dependencies. Unknown or unreadable binaries retain original discovery. Bakery's existing price patch remains independently available. Check `[Hard Mode Patch]` lines in `Player.log` for activation, cached lookups, global searches, scene invalidations and Hyperreal scan timings. Logs are aggregated (roughly once a minute while discovery is running, plus lifecycle/toggle messages); use City Performance's exported trace for actual frame times.

For the next playtest, compare the same route with each switch on/off, then check Bakery Help after waiting over two minutes, toggling the option and changing language, plus street lights at night and after reloading a save. In-game validation is pending.

## Rent rebalance

With **Flats at twice and shops at three times the market rent** on (Settings > Mods, off by default), every lease you sign costs twice the market rent for a flat and three times for any other building. Deposits keep the game's usual number of days, so they rise with the rent; leases already signed keep their rent. Rivals' estimated income keeps the game's rent, so rival survival and the Rivals leaderboard don't change.

## Happiness rebalance

With **Home activities bank up to four days…** on (Settings > Mods, off by default), home activities give a smaller boost that banks up to four days, so home tops out around 60. Outside activities (cinema, theatre, golf, tennis, casino, swimming, clubs, the pier, walks) give their boost for 4 to 7 days from one visit, so reaching 100 takes a mix of them. "No home" counts −15 instead of −50 while Real Life Overhaul has a hotel room booked for you. Other mods' happiness effects are unchanged.

## Indebted starting scenario

With Hardcore Ambitions disabled, open Settings > Mods and enable **Start a new game with $551 cash and a $75,454 loan**, then create a new game. This is the starting scenario for the RLO-led HART pack. It is an explicit new-game choice and does not convert an existing save, including an existing Day 1 save. Toggling it later affects future new games only.

The scenario gives $551.37 cash and one native $75,454.35 loan at a real city bank, preferring Vantander at 6 Second Avenue. Interest and principal payments come from that bank's native formulas and the chosen calendar/interest difficulty. It leaves your difficulty unchanged and buys no investment funds. The debt represents an existing student loan: it has no new-loan cash payout or origination event. It does not carry over Hardcore Ambitions' vehicle-price changes.

The game services the native loan; RLO reads it into its credit file and handles its penalties/arrangements. This mod keeps no parallel debt account and does not force the former $16/$158 terms back onto it. The native contract uses the game's original-principal interest formula; RLO's recognition of it is not proof of fully realistic amortisation or balanced lending. Those are the next debt-balance review.

A saved completion marker prevents resetting cash or recreating the debt after reload, repayment or refinancing. The choice is captured in `SaveGameManager.New`; setup runs before `GlobalEvents.InvokeOnGameLoaded` subscribers, after city assets load. Missing bank data, conflicting Hardcore Ambitions or unexpected existing debt leaves the scenario unapplied with an error notice and a failed marker; it never silently resets that save on a later reload. Disable Hardcore Ambitions and restart before selecting this scenario.

**Checked offline, 15 October:** native scenario data, actual game-formula calculations with illustrative bank settings, hook resolution/application, opt-in/conflict guards, repeat callbacks, existing Day 1 saves, reload, payoff, and RLO loan tracking/penalty/arrangement/save restoration. Release 0.24.0 was extracted into a temporary folder from the checksum-verified upstream ZIP; reviewed DLL SHA-256 `9c380a71a819c79a20c3a82aebc45164fcbae575a2245f90b9a0a2f269b2e4a0`. Local Workshop RLO 0.20.0 passed the same checks. **Pending in game:** actual lifecycle/bank availability and asset terms, the options page/notice, first midnight repayment, RLO's live Finance app and save/reload settlement. No existing saves or installed mods were changed by these checks.

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

If you use the optional **Hardcore Ambitions patch** instead of this mod's standalone scenario, its student loan still keeps Hardcore Ambitions' fixed $16 interest/$158 payment. The new scenario does not set Hardcore's save flags and is not subject to that restoration. The RLO-led pack drops Hardcore Ambitions.

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

Needs the .NET SDK (8 or newer to build; 10 for the check projects) and the game's managed DLLs, which belong to Hovgaard Games and are never committed.

```bash
tools/fetch-game-dlls.sh <ssh host of the Windows game PC>
tools/build.sh
dotnet test tests/HardModePatch.Core.Tests.csproj
tools/check-hooks.sh <folder holding the target mods' DLLs>
dotnet run --project tools/ScenarioCheck -- build/game-dlls build/HardModePatch/HardModePatch.dll <RealLifeOverhaul.dll>
dotnet run --project tools/PerformanceCheck -- build/game-dlls build/HardModePatch/HardModePatch.dll <folder containing Bakery.dll and PolyModsHyperreal.dll>
```

`tools/check-hooks.sh` loads the built patch, the game's assemblies and the target mods' DLLs in .NET, resolves every hook and applies every patch with Harmony, then removes them. It catches renamed methods, changed signatures and mismatched patch parameters without starting the game. It needs the Unity modules in `build/game-dlls` as well; copy every `Unity*.dll` from the game's `Managed` folder there.

`ScenarioCheck` exercises the standalone scenario against native data containers and optionally the selected RLO DLL's game-free loan tracking/holds. It does not launch Unity, read a save or execute nightly game settlement. Its bank settings are illustrative, not extracted real-bank assets. Omit the final DLL argument for scenario-only checks.

`PerformanceCheck` verifies exact hook application, the single-call transpiler substitutions, original-body preservation, pre-patch delegates, Help refresh scope and enabling on an existing singleton, typed light cache updates, scene/toggle invalidation and unverified-Bakery price fallback. Engine objects are inert test data; no native rendering/UI or real saves are exercised.

`tools/build.sh` compiles with the SDK's C# compiler against the game's own `mscorlib`, so the package runs on the game's Mono runtime. Harmony 2.4.2 is downloaded and checked against a pinned hash.

Rules and target versions without game code live in `Core/` and are covered by the tests. Game-facing code is in `src/`, one file per patched mod.

## Licence

MIT, see [LICENSE.txt](LICENSE.txt). Harmony is MIT, and its licence ships in `Dependencies\`. The patched mods belong to their authors; this repository contains none of their code.
