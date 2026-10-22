# HART. Mode Patch: Workshop upload

Paste-ready fields for the game's Mod Creator (Main menu > Mods > Mod Creator). This folder is not part of the package, except `preview.jpg` once it exists. Nothing here has been uploaded. Nothing in 0.5.0 has run in the game yet: the release plan's integration run on `agentmac` and Peter's first-day check on a clean Windows profile come before or right after the upload.

## Fields

**Mod folder:** `HardModePatch`. The folder keeps the old name so installed settings and saves carry over. Unzip `build/HardModePatch-0.5.0.zip` into `ModsLocal` on the PC that uploads; the Mod Creator lists the folders in `ModsLocal`.

**Title:** `HART. Mode Patch`

**Short description:** `The HART. Group series settings in one button, plus the balance rules and mod fixes the HART. Mod Pack plays with.`

**Target build:** `3682`, the game build the hooks were read against (decompile of 22 September 2026; the managed DLLs used for the offline checks are Steam build 25482473).

**Version:** `0.5.0`. Package: `build/HardModePatch/`; archive: `build/HardModePatch-0.5.0.zip`, SHA-256 in `build/HardModePatch-0.5.0.zip.sha256`; file hashes: `build/HardModePatch.sha256`. `tools/package.sh` rebuilds all three.

**Thumbnail:** none yet. The Mod Creator takes the first image in the mod folder's root, so put a `preview.jpg` under 1 MB in `workshop/` and rerun `tools/package.sh`; it copies the file into the package root.

**Required items (set on the Workshop page after upload):** none. Every part that needs another mod switches itself on only when that mod is loaded. HART. Mod Pack lists it as a member.

**Bug reports:** open. There is no HART. Mode Patch Discord channel yet; the description below has a placeholder.

**Donations:** the PayPal hosted button `Q8KVURCRBFLQN` covers all of Peter's Big Ambitions projects.

**Licence:** MIT for this mod's code (`LICENSE.txt`). Harmony's own licence ships in `Dependencies/`. The patched mods belong to their authors; the package contains none of their code.

**Description** (Steam formatting):

```
The settings and rules the HART. Group series plays with, in one mod. It is part of the HART. Mod Pack and also works on its own: every part that needs another mod waits for that mod to be loaded.

[h2]Series settings[/h2]
Under Settings > Mods > HART. Mode Patch:
[list]
[*][b]Apply HART series settings[/b]: one button sets the series values in this mod, HART Rival Balance (Normal), Dynamic Rivals (7 wholesale and 8 import rivals, Vanilla occupancy, Balanced events) and Real Life Overhaul (its own camera and Flare off, fewer interruptions and notices, its core systems on). Every change goes to Player.log, anything it can't find is skipped and listed, and it asks you to restart. It only runs when you press it and leaves every other option alone.
[*][b]Prefill the custom game with the series settings[/b] (on): New game > Custom opens with the series values. 60 days a year, $0 starting money, rival attacks 150%, age 18, tax 51%, salaries, public prices and bank interest 130%, customer promotion 50%, urgent fees 200%, export 10%, resale value 50%, every Disable and Unlock box off. Change anything before you start, or switch it off.
[/list]

[h2]Our own rules[/h2]
All off until you turn them on or press the button:
[list]
[*][b]Indebted start[/b]: a new game starts with $551 cash and a $75,454 student loan at a city bank. Your difficulty settings stay as you chose them.
[*][b]Rent[/b]: leases you sign cost twice the market rent for a flat and three times for any other building. Rivals keep the game's rent.
[*][b]Happiness[/b]: home tops out around 60; cinema, golf, the beach and other outside activities carry you for days, so a mix of them beats a routine.
[*][b]Real Life Overhaul[/b]: jobs pay in full when you run businesses, power and water follow your appliances and opening hours, offices need an internet connection and pay for their computers, and the sewing, kiln and polishing machines need a trade diploma from RLO's school.
[/list]

[h2]Fixes for other mods[/h2]
On by default, each only when that mod is installed:
[list]
[*][b]Bakery[/b]: prices at the game's own bakery margins.
[*][b]POLYMODS HYPERREAL[/b]: resets exposure history after you close the city map, aimed at the dark evening screen some players see then.
[*][b]Internet Cafe[/b]: at most two customers per PC per hour while you're away.
[*][b]Meaningful Ambience[/b]: decor adds a share of the customers the game sends, not of the building's capacity.
[*][b]Vending Machines[/b]: a rival's furniture stays locked while you place a machine in their building.
[*][b]Empire Bank[/b]: withdrawals are taxed, no emergency central bank borrowing, honest credit scores, capped product terms, no requests to the author's servers.
[*][b]Hardcore Ambitions[/b]: keeps your difficulty settings and drops the investment funds.
[/list]

[h2]Mod updates[/h2]
Other mods update on their own schedule. If one changes the code a fix relies on, that fix switches itself off and the mod runs as its author published it. Real Life Overhaul's features run only on RLO versions this mod has been checked against (0.49.0 at release). When RLO updates first, they pause with one notice and a line in Player.log, and your save's diploma progress is kept until this mod catches up.

[h2]Good to know[/h2]
[list]
[*]Performance changes live in City Performance, not here.
[*]Back up a save you care about before loading it with new mods.
[*]Lines starting [HART. Mode Patch] in Player.log (%USERPROFILE%\AppData\LocalLow\Hovgaard Games\Big Ambitions) show what the mod did and why.
[/list]

[h2]Bug reports[/h2]
Post bugs in [url=DISCORD-LINK]#hart-mode-patch-bugs on my Discord[/url], one post per problem. Include the mod version, your other mods and what happened, plus the [HART. Mode Patch] lines from Player.log.

[h2]Support[/h2]
The mod is free and stays free. If you'd like to say thanks, you can [url=https://www.paypal.com/donate/?hosted_button_id=Q8KVURCRBFLQN]support my Big Ambitions projects via PayPal[/url].

Made for the HART. Group series. The full README ships with the mod.
```

**Change note** (first upload):

```
0.5.0: first Workshop release

- New: Apply HART series settings, one button for the series values in this mod, HART Rival Balance, Dynamic Rivals and Real Life Overhaul.
- New: the custom game screen opens with the series values (switch under Settings > Mods).
- Real Life Overhaul 0.49.0 supported. On an RLO version this mod hasn't been checked against, its RLO features pause and your diploma progress is kept.
- Performance changes moved to City Performance 0.5.0.

Restart the game after updating.
```
