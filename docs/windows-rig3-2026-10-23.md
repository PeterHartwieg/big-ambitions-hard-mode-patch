# Windows rig 3, 23 October 2026

**Release ready: no.** The specific BizMan utility closure message is fixed and passed native Windows checks. The requested staffed-business, several-month balance comparison is **not completed**. No fair/unfair verdict or economy tuning is supported by this session.

## Series start and balance boundary

Used the existing isolated agent1 rig, including its approved modified game assembly (`055700fd26a7a6430bbb6a9d79379781909fbc518af760453396947887db5063`). Peter's game was absent at the initial process check. The compiled `BridgeControl.LeaseStore` was unheld, then acquired as `windows-rig3-economy-20261023`; every launch asserted ownership and absence of any Big Ambitions process. No other process was stopped.

The native HART series action was invoked at the main menu, followed by a restart. New game used the native Custom screen's saved series preset: 60 days/year, prices/wages/interest 130%, promotion 50%, tax 51%, all Disable and Unlock boxes off. The scenario supplied **$551.37 cash and one $75,454.35 loan**, initially $196/day interest plus $314/day principal. No injected money, free business layouts, unlocked courses, additional loans, inventory or employees were supplied to this series save. The final package's main-menu action reported **74 already set, 0 skipped**; after restart, `equipment_utilities=1`, `office_it=1`, `rent_rebalance=0` persisted.

A native El Gato cashier job advertised $25/hour, 08:00–16:00 with Friday off. A partial day-1 shift, started around 11:56, settled **$102** through the game's salary transaction at 16:00. Cash became **$653.37**, preserved at day 1 16:01 in `FT Windows Release Rig3 Earned` and verified after restarting with the final release DLL. The bridge maintained player needs during that accelerated work interval; this is not a survival or startup viability benchmark.

No staffed profitable retail or office was established. The meaning of the no-extra-capital constraint for normal in-game bank borrowing was queried; no answer had arrived when the session stopped, and no new borrowing was taken. This does **not** establish that an earned-cash route is impossible or that the series is unplayable. The safe checkpoint is retained for continued legitimate progression. It would be misleading to reuse the earlier $250,000 fixture as the requested economic result.

| Intended business | Game month | Revenue | Wages | Rent | Utilities | Office IT | Profit |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Retail, 12h | 1 | N/A | N/A | N/A | N/A | N/A | N/A |
| Retail, 12h | 2 | N/A | N/A | N/A | N/A | N/A | N/A |
| Retail, 12h | 3 | N/A | N/A | N/A | N/A | N/A | N/A |
| Matched retail, 24h | 1 | N/A | N/A | N/A | N/A | N/A | N/A |
| Matched retail, 24h | 2 | N/A | N/A | N/A | N/A | N/A | N/A |
| Matched retail, 24h | 3 | N/A | N/A | N/A | N/A | N/A | N/A |
| Staffed office with computers | 1 | N/A | N/A | N/A | N/A | N/A | N/A |
| Staffed office with computers | 2 | N/A | N/A | N/A | N/A | N/A | N/A |
| Staffed office with computers | 3 | N/A | N/A | N/A | N/A | N/A | N/A |

N/A means not observed, not zero. **Fairness verdict: unassessed. Tuning recommendation: retain the existing coefficients pending a valid revenue-relative test.** No running-cost coefficient, series value, rent rule or starting debt changed.

## BizMan fix

Default-on `utility_closure_reasons` reads reviewed RLO 0.49.0's current utility-owned closure and recorded reason, and replaces only the game's generic `BusinessTemporarilyClosed` description. It preserves the business-name prefix and another mod's specific description. Disabled, failed or stale directors, another save, manual/non-utility closures, expired closures and unknown reasons keep the original message. The adapter does not alter accounts, tasks, closures or saves. RLO 0.45.0 retains the original message because these new internal fields have only been reviewed for 0.49.0. After a reload the generic text can remain until RLO's next hourly refresh records a reason.

Native UI test used a **separate copy of the earlier supplied-capital `FT Windows Release Billing` fixture**, initially day 2, $245,652, unstaffed law office and kitchen. This fixture is excluded from the balance table. At 1920×1080 the office's alert visibly reads **“Temporarily closed: internet installation pending”**, wrapped cleanly on two lines. Turning the display rule off returns the generic description; turning it on restores the specific key with identical cash and closure records. By day 5 the connection is active, the office is open and its closure records/reasons/tasks are empty. Exhaustion and native hospital time-skips occurred in this old fixture, so the last observation is not a precise reopening-time measurement. No money was injected during this UI test.

Source: `Core/UtilityClosureReason.cs`, `src/UtilityClosureReasonPatch.cs`; option and English locale, native-data regression coverage, compatibility notes, README, change notes and Workshop descriptions updated. The test observer remains ignored and never ships.

## Validation and package

`tools/check-all.sh` passed: **170 Core**, available-target hook checks, **38 scenario**, **76 factory**, **44 cost/presentation**, **30 compatibility**, **19 unknown-RLO gate**, **26 settings**. Absent target DLLs were explicitly skipped by HookCheck; no broader compatibility claim is made. CostCheck covers native field shapes, Harmony installation/removal, the default reason, off, other-mod text, issue ownership, expiry, stale saves, failed/disabled directors, reopening and unknown reasons. RloGateCheck confirms the unknown hash leaves the description unpatched.

Mod Pack integration: native full-stack loading/version scan sees Patch 0.5.0, RLO 0.49.0, Rivals 0.2.0, City Performance 0.5.0 and Mod Pack 1.0.0; Campaign Objectives 1.2.0 remains the rig's installed snapshot. The native series action, restart, earned-save reload, cash/debt/settings and empty closure state passed. Mod Pack's 29 Core tests and 79/79 game API references on Windows and Mac passed. Steam enable/subscription handling remains deliberately disconnected and untested. City Performance's stock-hash guard remains withdrawn on this modified rig; this is not a new CP acceptance claim. No other mod's source or release package changed.

Final ZIP: `build/HardModePatch-0.5.0.zip`
SHA-256: `8924a0e70b1b3ad8cb497412ff1a3f42628e9cfc7046ff54a1441ca2dcf37127`

13 files; CRC, exact package inventory, all decompressed SHA-256 values and the manifest, current source documents/locale, assembly `0.5.0.0`, informational version `0.5.0`, one root DLL and **only `preview.png` as the thumbnail/image** verified. Its DLL SHA-256 `44374fae1608f9a1f87674b202e67afe0972815d3b17d0d4581e45d0f273c2ab` matches the native tested DLL. Final archive also installed and restarted successfully. Mod Pack ZIP was not rebuilt.

## Evidence and restoration

Ignored local evidence: `build/windows-rig3-20261023/`. Remote profile backups, retained test profile and full logs: `C:\Users\Peter\HART-Diagnostics\Performance\windows-rig3-20261023`.

Key files: `backup.json`, `game-ready.json`, `after-work.json`, `final-earned-game.json`, `final-series-menu-lines.txt`, `final-options-restarted.json`, `closure-before.json`, `closure-current.json`, `closure-current-screen.png`, `closure-off.json`, `closure-on.json`, `closure-after-connection.json`, `connected-economy.json`, `candidate-diagnostic.json`, `final-installed.json`, `check-all.log`, `modpack-core.log`, `modpack-api.log`, `package-verification.json`, `dll-versions.tsv`, `restore-receipt.json`, `final-rig-status.json`.

Restored all **4,290 original profile files** by inventory and SHA-256; isolated preferences, installation manifest and **105 referenced files** match the backup. At **2026-10-23T14:49:44.1793472Z**, no game process remained, the lease was unheld and pending operations were zero. No normal profile, ModsLocal, saves, preferences or subscriptions were changed. No Food Trucks repository/worktree was accessed or modified; the installed bridge snapshot was used as before. No Workshop upload, SteamCMD, force-push or history rewrite.
