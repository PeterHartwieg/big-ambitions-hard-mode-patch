# Series settings

HART. Mode Patch carries the settings the HART series plays with, so a viewer can set up the same game. Two parts, both under Settings > Mods > HART. Mode Patch > HART series settings. The values are in one table, `Core/SeriesSettings.cs`, covered by `tests/SeriesSettingsTests.cs` and `tools/SettingsCheck`.

## Apply HART series settings (button)

It runs only when pressed. For each row below it finds the option the game has registered, by the assembly of the mod that registered it and the option ID. It never uses a mod ID: that is the Workshop item ID for a Workshop copy and the folder path for a local one. It then does what the game's own option controls do: writes an int to PlayerPrefs under `m:<modId>:<optionId>` (toggles 0 or 1, sliders and dropdowns their value or index; no game option type stores a float) and calls the option's `OnValueChanged`. A value that is already set is left alone and its callback isn't called.

Every change goes to `Player.log` as `Series settings: <mod> <option> <before> -> <after> (<why>)`. A missing mod, a missing option, an option of another type, a value outside its range or an ID the mod registers twice is skipped and logged with the reason. Then `PlayerPrefs.Save()`, and each changed mod's options page is registered again with the same options, so an open Mods page shows the new values. One notice asks for a restart; if anything was skipped, the notice says so and points at the log.

Undecided rows of the RLO triage keep RLO's default, and so does every option not listed here: City Performance, Food Trucks, Driven Rides, Campaign Objectives and the recording tools are not touched.

| Mod | Option ID | Writes | Default | Why |
| --- | --- | ---: | --- | --- |
| HART. Mode Patch | `scenario_indebted_start` | 1 | 0 | series start: $551.37 cash and the $75,454.35 loan |
| HART. Mode Patch | `rent_rebalance` | 1 | 0 | series rule |
| HART. Mode Patch | `happiness_rebalance` | 1 | 0 | series rule |
| HART. Mode Patch | `owner_job_full_pay` | 1 | 0 | series rule |
| HART. Mode Patch | `factory_diplomas` | 1 | 0 | series rule |
| HART. Mode Patch | `equipment_utilities` | 1 | 0 | series rule |
| HART. Mode Patch | `office_it` | 1 | 0 | series rule |
| HART Rival Balance | `rival_preset` | 1 | 1 | preset Normal (0 Easy, 1 Normal, 2 Hard) |
| Dynamic Rivals | `wholesale_rival_count` | 0 | 0 | 7 (choices 7, 8, 10, 15, ...) |
| Dynamic Rivals | `import_rival_count` | 1 | 1 | 8 |
| Dynamic Rivals | `retail_occupancy` | 0 | 0 | Vanilla |
| Dynamic Rivals | `office_occupancy` | 0 | 0 | Vanilla |
| Dynamic Rivals | `warehouse_occupancy` | 0 | 0 | Vanilla |
| Dynamic Rivals | `residential_occupancy` | 0 | 0 | Vanilla |
| Dynamic Rivals | `event_frequency` | 2 | 2 | Balanced (Very Frequent, Frequent, Balanced, Rare, Extremely Rare) |
| Real Life Overhaul | `camera_enabled` | 0 | 1 | RLO camera off for the series |
| Real Life Overhaul | `camera_mouselook` | 0 | 1 | RLO camera off for the series |
| Real Life Overhaul | `car_camera` | 0 | 1 | RLO camera off for the series |
| Real Life Overhaul | `social_media` | 0 | 1 | Flare off while Persistent City is parked (21 Oct) |
| Real Life Overhaul | `social_texts_max` | 1 | 3 (1 to 6) | triage: the slider's minimum |
| Real Life Overhaul | `life_events` | 0 | 1 | triage |
| Real Life Overhaul | `accident_frequency` | 1 | 2 (1 to 10) | triage: the minimum |
| Real Life Overhaul | `crime_frequency` | 1 | 2 (1 to 10) | triage: the minimum |
| Real Life Overhaul | `roadside_checks` | 0 | 1 | triage |
| Real Life Overhaul | `player_offences` | 0 | 1 | triage |
| Real Life Overhaul | `street_event_frequency` | 1 | 4 (1 to 10) | triage: one day in twelve |
| Real Life Overhaul | `fire_frequency` | 1 | 2 (1 to 10) | triage: the minimum |
| Real Life Overhaul | `social_media_notices` | 0 | 1 | triage |
| Real Life Overhaul | `accident_marker` | 0 | 1 | triage |
| Real Life Overhaul | `accident_notices` | 0 | 1 | triage |
| Real Life Overhaul | `ledger_online_notes` | 0 | 1 | triage: system not in use yet |
| Real Life Overhaul | `markets_dividend_notes` | 0 | 1 | triage: system not in use yet |
| Real Life Overhaul | `careers_agency_note` | 0 | 1 | triage: system not in use yet |
| Real Life Overhaul | `business_frequency` | 2 | 4 (1 to 10) | triage |
| Real Life Overhaul | `social_relationships` | 1 | 1 | triage decision: stays on |
| Real Life Overhaul | `clothes_must_be_bought` | 1 | 1 | triage decision: on for the series save (kept per save) |
| Real Life Overhaul | `driving_licence` | 1 | 1 | triage decision |
| Real Life Overhaul | `driving_school` | 1 | 1 | triage decision |
| Real Life Overhaul | `economy` | 1 | 1 | audit profile |
| Real Life Overhaul | `economy_city` | 1 | 1 | audit profile |
| Real Life Overhaul | `economy_company` | 1 | 1 | audit profile |
| Real Life Overhaul | `utilities` | 1 | 1 | audit profile |
| Real Life Overhaul | `economy_company_staff` | 1 | 1 | audit profile |
| Real Life Overhaul | `economy_company_cover` | 1 | 1 | audit profile |
| Real Life Overhaul | `economy_company_marketing` | 1 | 1 | audit profile |
| Real Life Overhaul | `economy_company_tax` | 1 | 1 | audit profile |
| Real Life Overhaul | `economy_company_licences` | 1 | 1 | audit profile |
| Real Life Overhaul | `economy_rota` | 1 | 1 | audit profile |
| Real Life Overhaul | `interiors` | 1 | 1 | audit profile |
| Real Life Overhaul | `business_pressure` | 1 | 1 | audit profile |
| Real Life Overhaul | `family` | 1 | 1 | audit profile |
| Real Life Overhaul | `home_life` | 1 | 1 | audit profile |
| Real Life Overhaul | `social` | 1 | 1 | audit profile |
| Real Life Overhaul | `leisure` | 1 | 1 | audit profile |
| Real Life Overhaul | `civic` | 1 | 1 | audit profile |
| Real Life Overhaul | `civic_elections` | 1 | 1 | audit profile |
| Real Life Overhaul | `civic_office` | 1 | 1 | audit profile |
| Real Life Overhaul | `city_life` | 1 | 1 | audit profile |
| Real Life Overhaul | `ambient_sound` | 1 | 1 | audit profile |
| Real Life Overhaul | `night_life` | 1 | 1 | audit profile |
| Real Life Overhaul | `transit_buses` | 1 | 1 | audit profile |
| Real Life Overhaul | `transit_subway` | 1 | 1 | audit profile |
| Real Life Overhaul | `transit_taxi_call` | 1 | 1 | audit profile |
| Real Life Overhaul | `nav_minimap` | 1 | 1 | audit profile |
| Real Life Overhaul | `nav_gps` | 1 | 1 | audit profile |
| Real Life Overhaul | `planner` | 1 | 1 | audit profile |
| Real Life Overhaul | `jobs` | 1 | 1 | audit profile |
| Real Life Overhaul | `careers` | 1 | 1 | audit profile; factory diplomas need it |
| Real Life Overhaul | `careers_study` | 1 | 1 | audit profile; factory diplomas need it |
| Real Life Overhaul | `careers_jobs` | 1 | 1 | audit profile |
| Real Life Overhaul | `careers_life` | 1 | 1 | audit profile |
| Real Life Overhaul | `careers_degrees` | 1 | 1 | audit profile |
| Real Life Overhaul | `economy_products` | 1 | 1 | audit profile |
| Real Life Overhaul | `economy_consequences` | 1 | 1 | audit profile |

"Default" is the mod's own default (RLO 0.49.0 source, Dynamic Rivals' DLL of 19 October, Rival Balance 0.2.0). Every RLO row that writes 1 is already RLO's default; the button writes it so a viewer who switched one off gets the series value back. Every RLO, Dynamic Rivals and Rival Balance option ID above is a string in that mod's DLL (`SettingsCheck`).

Sources: the series-2 release plan (RLO camera, Flare, Rival Balance, Dynamic Rivals), the RLO options triage as applied on homepc on 20 October and Peter's decisions that day, and the RLO-led profile rows of the 14 October modpack audit that the series still uses.

Left out on purpose:

- The audit's "0 in baseline, staged later" rows (`economy_estate`, `economy_bank`, `economy_online`, `economy_brands`, `economy_dealer`, the markets and ventures rows, the tech sector, `careers_own`, `careers_family`, `car_ownership`, `car_tuning`, `garage`, `economy_shark`, `markets_margin`). The later triage says late-game systems stay on and dormant, and Episode 1 ran with them at RLO's defaults. Peter to confirm.
- `visuals_enabled` and `character_high_detail`: the triage leaves presentation to the series look, so they are undecided.
- `dev_diagnostics`: not an option in RLO 0.49.0.
- `permadeath`: off by default, and RLO turns it off at the main menu itself. Writing it while a save is loaded would change that save.
- City Performance options: City Performance owns them.

## Prefill the custom game with the series settings (toggle, on)

A postfix on `UI.MainMenu.CustomGameOptionsHandler.Start` takes the preset the game just set (its first built-in preset), copies it, puts the series values on top and calls the screen's public `SetPreset(GameVariables)`. The sliders then show the series values, and the player can change any of them before starting. It does nothing when the screen's `preventAutoSetOnStart` is set. Every name is looked up at start; if the screen, `SetPreset`, `GetPreset` or any field below is missing or of another type, nothing is patched, the options page says so and the game's own preset stands.

| `GameVariables` field | Series value |
| --- | --- |
| `daysPerYear` | 60 |
| `startingMoney` | 0 (the indebted start then gives $551.37 and the $75,454.35 loan) |
| `rivalsDifficultyMultiplier` | 1.5 (rival attacks 150%) |
| `startingAge` | 18 |
| `disableAging`, `disableEnergy`, `disableHappiness`, `allCoursesUnlocked`, `allContactsUnlocked`, `disableVehicleDamage`, `disableVehicleFuel`, `disableWholesaleAndImportLimits`, `allProductsAvailableFromImporters` | false |
| `tutorialEnabled` | false (`SetPreset` sets it off anyway) |
| `taxPercentage` | 51 |
| `employeeHourlySalaryMultiplier` | 1.3 |
| `marketPriceMultiplier` | 1.3 (public prices) |
| `bankInterestMultiplier` | 1.3 |
| `baseCustomerPromotionMultiplier` | 0.5 |
| `wholesaleUrgentFeeMultiplier`, `importerUrgentFeeMultiplier` | 2.0 |
| `exportMultiplier` | 0.1 |
| `sellingMultiplier` | 0.5 (resale value) |

Source: the series-2 campaign's recommended settings (Peter, 20 October), as the release plan lists them. The slider limits live in the game's scene, not in code, so whether 2.0 urgent fees and 0.1 export fit the sliders is unverified; Episode 1's settings screen showed the urgent fees at 200%.

## Comparing with the series PC

`tools/export-series-settings.ps1` runs on Windows with the game closed. It reads `HKCU\Software\Hovgaard Games\Big Ambitions` (Unity's PlayerPrefs; each value name is the key, `_h` and a djb2-xor hash of the key, which the script checks), matches every row of the table above by option ID, and writes JSON with the stored value per mod ID and whether it matches. `-All` adds every other mod option key. It changes nothing. Run:

```powershell
powershell -ExecutionPolicy Bypass -File export-series-settings.ps1 -Out "$env:USERPROFILE\Desktop\hart-series-settings.json" -All
```

The script was parsed and run with PowerShell 7 on macOS against a stand-in registry; it hasn't run on Windows.

## Checked and not

Checked offline: the table (core tests), every option ID in its mod's DLL, the button's lookup on Dynamic Rivals', Rival Balance's and HMP's real options pages, the prefill hooks and preset against the game's assemblies (build 25482473), and the export script's list against the table. Not checked: the button in the game, RLO's live options page (RLO registers it during its own start, which needs Unity), PlayerPrefs writes, the restart notice at the main menu, the custom game screen and the export script on Windows.
