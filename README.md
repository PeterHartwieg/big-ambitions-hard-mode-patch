# HART. Mode Patch

Formerly Hard Mode Patch. The DLL, the mod folder and the option IDs keep the old `HardModePatch` name, so installed settings and saves carry over.

A Big Ambitions mod for the HART. Group series pack: the series settings in one button, an optional indebted starting scenario, our own balance rules on Real Life Overhaul, and runtime patches that make other Workshop mods fit together. Every change has an option under Settings > Mods. The scenario and our own rules are off by default; with them off and no target mod installed, the only change is the custom game screen opening with the series values, and that has its own switch.

**Status:** 0.5.0, the first Workshop release, paired with City Performance 0.5.0. Checked offline only; nothing in 0.5.0 has run in the game yet. See [CHANGELOG.md](CHANGELOG.md).

HART. Mode Patch owns mod compatibility and the pack's intended mechanics: the series settings, prices, the scenario, rent, happiness, running costs, factory diplomas and the Hyperreal map return. City Performance owns every performance change; Bakery's Help discovery and Hyperreal's lamp search moved there in 0.4.2 ([the handoff](docs/performance-ownership.md)).

## HART series settings

Under Settings > Mods > HART. Mode Patch > HART series settings:

- **Apply HART series settings** (a button) sets the values the series plays with: this mod's rules on, HART Rival Balance on Normal, Dynamic Rivals at 7 wholesale and 8 import rivals, Vanilla occupancy and Balanced events, and Real Life Overhaul with its own camera and Flare off, fewer interruptions and notices, and its core systems on. It finds each option by the mod that registered it and the option's ID, writes it the way the game's Mods page does, and logs every change in `Player.log`. A mod or option it can't find is skipped and listed. It works at the main menu only; restart the game afterwards. It runs only when you press it, and it leaves every option it doesn't list alone.
- **Prefill the custom game with the series settings** (on) opens New game > Custom with the series values: 60 days a year, $0 starting money, rival attacks 150%, age 18, tax 51%, salaries, public prices and bank interest 130%, customer promotion 50%, urgent fees 200%, export 10%, resale value 50%, every Disable and Unlock box off. Change any of them before you start, or switch the toggle off for the game's own presets.

[The full list of values, their sources and what is left out](docs/series-settings.md). `tools/export-series-settings.ps1` dumps a Windows PC's current values to JSON, to compare a PC with the series profile.

## Real Life Overhaul versions

Real Life Overhaul updates through the Workshop on its own schedule. Factory diplomas, running costs, owner job pay and the hotel rule run only on RLO versions this mod has reviewed: 0.45.0 and 0.49.0. On any other version they switch off until this mod is updated, with one line in `Player.log` and, if you use one of them, one notice when a save loads; RLO itself runs as its author published it. Your save's factory diploma courses and results are kept meanwhile and come back with the update; a course under way carries on where it stood, at most six days later than the time it was paused. [How the check works and the 0.49.0 review](docs/rlo-compatibility.md).

## Hyperreal map-return workaround (awaiting playtest)

**Reset Hyperreal exposure history after returning from the city map** (`hyperreal_map_return_fix`, on by default) requests one HDRP postprocessing-history reset after the game's camera return and Hyperreal's closing fade both finish. It acts only outdoors when Hyperreal has resumed Automatic Histogram exposure, on the camera Hyperreal supplied that frame. Fixed exposure and indoor returns are left alone. This compatibility feature is independent of City Performance and does not require any lamp-discovery hook.

This addresses the leading hypothesis for Peter's 19 October report of darkness after leaving the map. The supplied log cannot identify the black frame, so symptom resolution is **unverified**. The historical lamp cache did not change exposure or drop disabled lamps. See [the diagnosis and test procedure](docs/hyperreal-map-return.md) for ranked evidence and remaining alternatives.

The workaround retains its exact reviewed Hyperreal/game/Unity/HDRP binary requirements. Unknown binaries leave Hyperreal unpatched. `Player.log` records `Hyperreal map return: reset exposure history...` once when it acts. Loading another save restores the map-close subscription; scene changes, game-event initialization, Hyperreal reset and option changes cancel pending returns.

## Rent rebalance

With **Flats at twice and shops at three times the market rent** on (Settings > Mods, off by default), every lease you sign costs twice the market rent for a flat and three times for any other building. Deposits keep the game's usual number of days, so they rise with the rent; leases already signed keep their rent. Rivals' estimated income keeps the game's rent, so rival survival and the Rivals leaderboard don't change.

## Happiness rebalance

With **Home activities bank up to four days…** on (Settings > Mods, off by default), home activities give a smaller boost that banks up to four days, so home tops out around 60. Outside activities (cinema, theatre, golf, tennis, casino, swimming, clubs, the pier, walks) give their boost for 4 to 7 days from one visit, so reaching 100 takes a mix of them. "No home" counts −15 instead of −50 while Real Life Overhaul has a hotel room booked for you. Other mods' happiness effects are unchanged.

## Running costs (awaiting playtest)

Two options under **Running costs**, both off by default, for Real Life Overhaul's utilities. They bring back the two Economy Expansion pressures RLO doesn't have. Without RLO, on an RLO version this mod hasn't reviewed, or when a patched RLO member is renamed or its signature changes, nothing is patched and RLO's own bills stand.

**Appliances and opening hours** (`equipment_utilities`). RLO bills a business's power and water by floor area and business kind, the same for a 2-hour and a 24-hour day. With this on, that floor figure follows the hours open: 12 hours keep RLO's figure, 24 hours cost 1.75 times as much, and a closed day keeps RLO's quarter. Cooking, machines and IT add their own draw while open: a fryer averages 5 kW, a grill 4 kW, a pizza oven 6 kW, factory machines 3 to 40 kW. RLO's closed-day quarter is its allowance for fridges and alarms, so placed fridges, freezers and alarms replace it when they draw more, open or not. RLO's water rate is the kind's basins, so placed sinks, showers, dishwashers and toilets replace its open-hours water when they use more. At RLO's start prices (15¢ a kWh), a 100 m² burger bar with two fryers, two grills, two fridges, a freezer and a drinks fridge pays about $37 a day more for power when open 12 hours, and about $79 more when open 24. Homes keep RLO's bill. The bill, its due day, late fee and cut stay RLO's.

**Office IT** (`office_it`). RLO requires a connection only from graphic designers, web developers, headquarters and its own tech firms. With this on, every office type needs one to open: law firms, marketing, recruitment, event planning, travel, interior installation and import/export too. Each desk with a computer (the game's computer, desktop, gaming PC and laptop) adds $150 a game month of software, cloud and support to the office's internet bill, charged on the days RLO charges the connection. In a 60-day year a month is 5 days, so that is $30 a desk a day; a 20-desk office pays $600 a day. An unpaid bill cuts the connection and the office closes, as RLO already does. The Utilities app still shows the plan's own price; the bill includes the desks. An office that needs a connection only because this option was switched on mid-save gets no grace period. RLO's own tech firms pay RLO's software charge by the seat instead, not this one.

Appliance figures are average draw with typical duty cycles, in `Core/RunningCosts.cs`; other mods' items fall back on the game's flags (factory machine 10 kW, computer 150 W, sink, toilet). Offline checks pass against released RLO 0.45.0 and 0.49.0 (`tools/CostCheck`); RLO's nightly books, the Utilities app and real saves still need an in-game check.

## Advanced factory diplomas (awaiting playtest)

With **Require trade diplomas to buy sewing, kiln and polishing machines** on (off by default), the Factory Supply Depot requires the corresponding garment making, electronics technician or jewelry and goldsmithing diploma. HMP adds these as native RLO school courses: eight four-hour classes, three a week, four homework pieces and an exam; base tuition $3,600, $3,900 and $3,800. Their full-time schedules do not overlap. Already-owned machines keep working; the Laser Cutting Machine and plain consumer-goods workstation have no new restriction.

Requires reviewed RLO 0.45.0 or 0.49.0 with careers and study enabled. Missing/incompatible RLO or unavailable study leaves purchases unrestricted. Under an RLO build HMP hasn't reviewed, HMP keeps a copy of the save's diploma records and gives them back once a reviewed build is loaded ([RLO compatibility](docs/rlo-compatibility.md)). Turning HMP's option off keeps diploma progress/results and opens sales; removing HMP makes unmodified RLO safely ignore the extra course records and discard them on its next save. Ordinary courses, skills and student debt remain readable. See [design, hooks, save behavior and playtest](docs/factory-diplomas.md). Offline checks pass; Campus UI, physical school activities, live purchases and actual save/load still need an in-game check.

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
- **Not patched (not reviewed yet):** Real Life Overhaul only. Its features here reach into its save data and bills, so a new RLO version is never patched before it has been reviewed ([RLO versions](#real-life-overhaul-versions)).

The state of each mod shows on the options page and in `Player.log` on lines starting `[HART. Mode Patch]`.

## Planned, not written

- **Alcware Seasons follows RLO's calendar** (Peter, 17 October): Alcware's season picture and demand would follow RLO's seasons. The design is in [docs/plan.md](docs/plan.md).

## Install

Subscribe on the Workshop (not published yet; it is part of the HART. Mod Pack), or unzip `build/HardModePatch-0.5.0.zip` into `%USERPROFILE%\AppData\LocalLow\Hovgaard Games\Big Ambitions\ModsLocal`.

Mods load for every save. Back up any save you care about before loading it with this or the mods it patches.

## Building

Needs the .NET SDK (8 or newer to build; 10 for the check projects) and the game's managed DLLs, which belong to Hovgaard Games and are never committed.

```bash
tools/fetch-game-dlls.sh <ssh host of the Windows game PC>
tools/build.sh
tools/package.sh   # build/HardModePatch-<version>.zip and its SHA-256
tools/check-all.sh <target DLL folder> <unreviewed RealLifeOverhaul.dll> <DynamicRivals.dll> <HARTRivalBalance.dll>
dotnet test tests/HardModePatch.Core.Tests.csproj
tools/check-hooks.sh <folder holding the target mods' DLLs>
dotnet run --project tools/ScenarioCheck -- build/game-dlls build/HardModePatch/HardModePatch.dll <RealLifeOverhaul.dll>
dotnet run --project tools/CompatibilityCheck -- build/game-dlls build/HardModePatch/HardModePatch.dll <folder containing Bakery.dll and PolyModsHyperreal.dll>
dotnet run --project tools/FactoryCheck -- build/game-dlls build/HardModePatch/HardModePatch.dll <RealLifeOverhaul.dll>
dotnet run --project tools/CostCheck -- build/game-dlls build/HardModePatch/HardModePatch.dll <RealLifeOverhaul.dll>
dotnet run --project tools/RloGateCheck -- build/game-dlls build/HardModePatch/HardModePatch.dll <unreviewed RealLifeOverhaul.dll>
dotnet run --project tools/SettingsCheck -- build/game-dlls build/HardModePatch/HardModePatch.dll <RealLifeOverhaul.dll> <DynamicRivals.dll> <HARTRivalBalance.dll> tools/export-series-settings.ps1
```

`tools/check-hooks.sh` loads the built patch, the game's assemblies and the target mods' DLLs in .NET, resolves every hook and applies every patch with Harmony, then removes them. It catches renamed methods, changed signatures and mismatched patch parameters without starting the game. It needs the Unity modules in `build/game-dlls` as well; copy every `Unity*.dll` from the game's `Managed` folder there.

`ScenarioCheck` exercises the standalone scenario against native data containers and optionally the selected RLO DLL's game-free loan tracking/holds. It does not launch Unity, read a save or execute nightly game settlement. Its bank settings are illustrative, not extracted real-bank assets. Omit the final DLL argument for scenario-only checks.

`CompatibilityCheck` verifies price-only Bakery behavior and original-price restoration, exact map-return hooks, camera/fade completion, exposure eligibility, scene/reset/reload cleanup and unreadable binary refusal. It also asserts that HMP no longer owns Help, Scan or lamp lifecycle hooks. City Performance's `OwnershipCheck` covers the moved Help behavior and actual old/new HMP coexistence in both load orders. Engine objects are inert test data; no native rendering/UI or real saves are exercised.

`FactoryCheck` exercises the advanced factory diplomas against released RLO 0.45.0 or 0.49.0: native course definitions, study/save logic, purchase refusals, fail-safe states and held records given back, without Unity or a live save.

`RloGateCheck` takes an RLO DLL whose hash isn't reviewed (a release with one byte appended works) and checks that every RLO feature stays off, the refusal is one message and one notice, and a save's diploma records are held.

`SettingsCheck` checks that every option ID the series button writes is in its mod's DLL, runs the button's lookup on the real options pages of Dynamic Rivals, Rival Balance and this mod, and resolves the custom game prefill against the game's screen. RLO's options page needs Unity to build, so RLO's lookup isn't exercised offline.

`CostCheck` applies the running-cost patches to the selected RLO DLL's own billing methods and runs them on fixture accounts and registrations: hours, appliances, homes, office desks, outages and the connection list. It needs `UnityEngine.CoreModule.dll` in the game folder.

`tools/build.sh` compiles with the SDK's C# compiler against the game's own `mscorlib`, so the package runs on the game's Mono runtime. Harmony 2.4.2 is downloaded and checked against a pinned hash.

Rules and target versions without game code live in `Core/` and are covered by the tests. Game-facing code is in `src/`, one file per patched mod.

## Licence

MIT, see [LICENSE.txt](LICENSE.txt). Harmony is MIT, and its licence ships in `Dependencies\`. The patched mods belong to their authors; this repository contains none of their code.
