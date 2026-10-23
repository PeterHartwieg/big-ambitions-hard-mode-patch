# HART. Mod Pack Patch: Workshop upload

Paste-ready fields for the game's Mod Creator (Main menu > Mods > Mod Creator). This folder is not part of the package, except `preview.png`. Nothing here has been uploaded. Earlier Mac integration covered series settings, a new game, midnight and save/reload. Current release validation and remaining native checks are recorded in the release session 2 handoff.

## Fields

**Mod folder:** `HardModePatch`. The folder keeps the old name so installed settings and saves carry over. Unzip `build/HardModePatch-0.5.0.zip` into `ModsLocal` on the PC that uploads; the Mod Creator lists the folders in `ModsLocal`.

**Title:** `HART. Mod Pack Patch`

Called HART. Mode Patch until 23 October 2026. The name now says what the mod is: the HART. Mod Pack's companion. Only player-facing text changed. The folder, DLL, option IDs and the `[HART. Mode Patch]` prefix in Player.log keep their old names.

**Short description:** `The HART. Mod Pack's companion mod: the HART. Group series settings in one button, plus the house rules and mod fixes the pack plays with.`

**Target build:** `3682`, the game build the hooks were read against (decompile of 22 September 2026; the managed DLLs used for the offline checks are Steam build 25482473).

**Version:** `0.5.0`. Package: `build/HardModePatch/`; archive: `build/HardModePatch-0.5.0.zip`, SHA-256 in `build/HardModePatch-0.5.0.zip.sha256`; file hashes: `build/HardModePatch.sha256`. `tools/package.sh` rebuilds all three.

**Thumbnail:** `workshop/preview.png`, the only image in the package root. Approved artwork; the build script copies it automatically. Gallery cards stay outside the runtime package.

**Required items (set on the Workshop page after upload):** none. Every part that needs another mod switches itself on only when that mod is loaded. HART. Mod Pack lists it as a member.

**Bug reports:** use the Workshop comments or discussion for the relevant item; no Discord invitation is configured here.

**Donations:** the PayPal hosted button `Q8KVURCRBFLQN` covers all of Peter's Big Ambitions projects.

**Licence:** MIT for this mod's code (`LICENSE.txt`). Harmony's own licence ships in `Dependencies/`. The patched mods belong to their authors; the package contains none of their code.

**Description** (Steam formatting):

```
The HART. Mod Pack's companion mod. It carries the settings and rules the HART. Group series plays with, and the fixes that let the pack's mods run together. The pack lists it as a required item. It also works on its own: every part that needs another mod waits for that mod to be loaded.

[h2]Series settings[/h2]
Under Settings > Mods > HART. Mod Pack Patch:
[list]
[*][b]Apply HART series settings[/b]: works at the main menu. One button sets the series values in this mod, HART. Thriving Rivals (Normal), Dynamic Rivals (7 wholesale and 8 import rivals, Vanilla occupancy, Balanced events) and Real Life Overhaul (its own camera and Flare off, fewer interruptions and notices, its core systems on). Every change goes to Player.log, anything it can't find is skipped and listed, and it asks you to restart. It only runs when you press it and leaves every other option alone.
[*][b]Prefill the custom game with the series settings[/b] (on): New game > Custom opens with the series values. 60 days a year, $0 starting money, rival attacks 150%, age 18, tax 51%, salaries, public prices and bank interest 130%, customer promotion 50%, urgent fees 200%, export 10%, resale value 50%, every Disable and Unlock box off. Change anything before you start, or switch it off.
[/list]

[h2]Our own rules[/h2]
Standalone house rules start off. The series button enables the following rules and explicitly turns the separate rent multiplier off:
[list]
[*][b]Indebted start[/b]: a new game starts with $551 cash and a $75,454 student loan at a city bank. Your difficulty settings stay as you chose them.
[*][b]Economy Expansion running costs[/b]: on RLO, equipment and opening hours shape utility bills. Every office needs internet and pays $150 per computer each game month, adjusted by RLO's price level. RLO handles bills, late fees and cuts. The series button disables the former flat 2x/3x rent increase; existing signed leases keep their stored rent. Economy Expansion itself is not required.
[*][b]Happiness[/b]: home tops out around 60; cinema, golf, the beach and other outside activities carry you for days, so a mix of them beats a routine.
[*][b]Real Life Overhaul[/b]: jobs pay in full when you run businesses, power and water follow your appliances and opening hours, offices need an internet connection and pay for their computers, and the sewing, kiln and polishing machines need a trade diploma from RLO's school.
[/list]

[h2]Fixes for other mods[/h2]
On by default, each only when that mod is installed:
[list]
[*][b]RLO utility closure reasons[/b]: BizMan explains pending internet installation, outages and utility supply problems when reviewed RLO 0.49.0 owns the closure. Display only; manual closures and unknown reasons keep the game's message.
[*][b]Bakery[/b]: prices at the game's own bakery margins.
[*][b]POLYMODS HYPERREAL[/b]: resets exposure history after you close the city map, aimed at the dark evening screen some players see then.
[*][b]Internet Cafe[/b]: at most two customers per PC per hour while you're away.
[*][b]Meaningful Ambience[/b]: decor adds a share of the customers the game sends, not of the building's capacity.
[*][b]Vending Machines[/b]: a rival's furniture stays locked while you place a machine in their building.
[*][b]Empire Bank[/b]: withdrawals are taxed, no emergency central bank borrowing, honest credit scores, capped product terms, and no statistics or supporters requests to the author's servers (its own opt-in heartbeat stays).
[*][b]Hardcore Ambitions[/b]: keeps your difficulty settings and drops the investment funds.
[/list]

[h2]Mod updates[/h2]
Other mods update on their own schedule. If one changes the code a fix relies on, that fix switches itself off and the mod runs as its author published it. Real Life Overhaul's features run only on RLO versions this mod has been checked against (0.49.0 at release). When RLO updates first, they pause with one notice and a line in Player.log, and your save's diploma progress is kept until this mod catches up.

[h2]Good to know[/h2]
[list]
[*]First Workshop release. Earlier Mac integration checked settings, the indebted start, midnight and save/reload. Windows checks cover utility billing and the specific BizMan closure alert. Staffed-business balance at series settings and Workshop installation still need acceptance.
[*]Performance changes live in City Performance, not here.
[*]Back up a save you care about before loading it with new mods.
[*]Lines starting [HART. Mode Patch] (the old name) in Player.log (%USERPROFILE%\AppData\LocalLow\Hovgaard Games\Big Ambitions) show what the mod did and why.
[/list]

[h2]Bug reports[/h2]
Post bugs in this item's Workshop comments or discussions, one post per problem. Include the mod version, your other mods and what happened, plus the [HART. Mode Patch] lines from Player.log.

[h2]Support[/h2]
The mod is free and stays free. If you'd like to say thanks, you can [url=https://www.paypal.com/donate/?hosted_button_id=Q8KVURCRBFLQN]support my Big Ambitions projects via PayPal[/url].
```

**Change note** (first upload):

```
[b]HART. Mod Pack Patch 0.5.0[/b]

[list]
[*]First Workshop release as HART. Mod Pack Patch, the companion to HART. Mod Pack.
[*]Apply HART series settings enables Economy Expansion-inspired running costs on Real Life Overhaul: equipment and opening hours affect power and water; offices need internet and pay $150 per computer each game month.
[*]The series button explicitly switches off the former flat 2x/3x rent increase. Existing signed leases keep their stored rent; the rent multiplier remains a separate optional rule.
[*]One button applies the series options; New game > Custom opens with the series values.
[*]Supports reviewed RLO 0.49.0. Unreviewed RLO versions pause the RLO features and preserve diploma progress.
[*]Performance changes belong to City Performance 0.5.0. Restart after updating.
[/list]
```
