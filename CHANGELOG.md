# HART. Mode Patch changelog

## 0.5.0 (22 October 2026), first Workshop release

- New name: HART. Mode Patch, formerly Hard Mode Patch. The DLL, the mod folder and the option IDs keep the old `HardModePatch` name, so settings and saves carry over.
- New button: **Apply HART series settings**. It sets the HART series values in this mod, HART Rival Balance, Dynamic Rivals and Real Life Overhaul, writes each change to `Player.log`, skips and lists anything it can't find, and asks for a restart. It runs only when you press it, and only at the main menu. See `docs/series-settings.md` for every value.
- New toggle, on by default: **Prefill the custom game with the series settings**. The custom game screen opens with the series values (60 days a year, $0 starting money, tax 51%, rival attacks 150% and the rest); you can change any of them before starting.
- Real Life Overhaul 0.49.0 is reviewed and supported, and 0.45.0 stays supported. 0.41.1 is no longer accepted.
- Every Real Life Overhaul feature (factory diplomas, running costs, owner job pay, the hotel rule for "no home") now runs only on an RLO version this mod has reviewed. On any other version they switch off, with one line in `Player.log` and, if you use one of them or a save has diplomas to keep, one notice when a save loads; RLO runs as published. Running costs and owner job pay used to run on any RLO whose method names still matched.
- Under an RLO version it hasn't reviewed, the mod keeps a copy of your save's factory diploma courses and results and gives them back once a reviewed RLO is loaded; a course under way carries on where it stood, a week or less later. Before, RLO dropped them on its next save.
- Bakery's Help menu discovery and Hyperreal's street-lamp search moved to City Performance, which now owns every performance change. Their two options are gone from this mod; Bakery prices and the Hyperreal map-return fix stay here. Pair this version with City Performance 0.5.0.

Defaults: the indebted start, rent, happiness, owner job pay, running costs and factory diplomas stay off until you turn them on or press the button. The patches for other mods stay on, and only act when those mods are installed.

Checked offline only: 158 core tests and the hook, scenario, factory, cost, compatibility, RLO gate and settings checks against the game's and the mods' DLLs. Nothing in this version has run in the game yet.

## Earlier versions

0.1.0 to 0.4.2 were local test builds for the HART series pack and were never published.

- 0.4.2: Bakery Help and Hyperreal lamp discovery handed to City Performance.
- 0.4.1 and the unreleased work after it: the standalone indebted start, rent, happiness and owner job pay rules, factory diplomas for RLO, running costs on RLO's utilities, the Hyperreal map-return fix and the hotel booking watch.
- 0.1.0 to 0.3.0: patches for Hardcore Ambitions, Meaningful Ambience, Bakery, Internet Cafe, Vending Machines and Empire Bank.
