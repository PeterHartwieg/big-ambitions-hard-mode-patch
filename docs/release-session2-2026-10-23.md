# Release session 2, 23 October 2026

The HART series preset explicitly sets `rent_rebalance=0`, `equipment_utilities=1` and `office_it=1`. Turning rent off uses the existing unmodified market-rent path and also overrides an older preset value of 1. A hidden 1x mode would misrepresent the existing “twice/three times” option. The optional standalone rent rule and signed lease contracts are unchanged. The running-cost implementation was already on main; this release activates it in place of flat rent increases and updates the preset exporter, tests and copy.

Search covered all local and remote-tracking branches in the Big Ambitions repositories (excluding the forbidden Food Trucks repository) and HART Group. No additional EE implementation was found. Relevant history: HART Group's `docs/research/modpack-review-2026-10-07/economy.md`, `docs/research/real-life-overhaul-modpack-audit-2026-10-14.md`, `docs/hard-mode-patch-plan.md` and `mods/modpack/modpack.json`. Older video/research plans are historical. EE itself remains excluded; RLO owns billing. Current manifest and series plan now match the new decision.

Full `tools/check-all.sh` passed with RLO 0.49.0: 158 Core tests, available-target hook checks, 38 scenario, 76 factory, 21 cost, 30 compatibility, 17 unreviewed-RLO gate and 26 settings checks (all 74 rows). Empire Bank, Hardcore Ambitions, Internet Cafe, Meaningful Ambience and Vending Machines DLLs were unavailable and were explicitly skipped by HookCheck. This run did not repeat CostCheck against 0.45.0; the prior documented 0.45.0 result remains historical. Logs: `build/release-session2-checks.log`, `build/release-session2-package.log`.

Pending: actual RLO nightly bills and Utilities/BizMan UI for food shop, office, 24-hour opening and factory; office closure without internet; balance calibration and native save/reload. `04-house-rules.png` still has old rent copy and must be redrawn by Opus before gallery use. Only its HTML text changed.

## Package verification

- Archive: `build/HardModePatch-0.5.0.zip`.
- SHA-256: `aa5b327150269bf57402cca2becd0bc7d7903ade7c1a73f0a01579219132201a`; matching `.zip.sha256` sidecar.
- DLL SHA-256: `638983edb460aec8ad4b0aba8b15d27b135aa6084f58862eec2ea8f4179b82da`.
- 13 files: ZIP CRC, exact file inventory, every decompressed hash, package manifest and approved thumbnail equality passed. Exactly one root DLL and only `preview.png` as the root image; no preview.jpg.
- Full description: `workshop/DESCRIPTION.bbcode.txt` (4727 characters, below 8,000), ends with the PayPal Support section.
- Change notes: `workshop/CHANGENOTE-0.5.0.txt`, `.bbcode.txt` and `.md`.

## Native Mac integration and limits

Both rigs were initially occupied and were left alone during offline work. At 14:01 CEST the Mac process had exited and its lock was released. Acquired a new reservation (`mods-release-session2-20261023`), verified/backed up all 3,144 profile files, preferences and installation manifest, and installed the four release candidates plus unchanged Campaign Objectives 1.2.1 and RLO 0.49.0. The existing Food Trucks 0.10.2 bridge snapshot was untouched. Private client shims used the existing compiled lock controller, so no Food Trucks repository/tool files were accessed or changed.

PASS: actual series button changed a deliberately seeded rent value from 1 to 0, enabled equipment utilities/office IT, and stored all 74 expected options. All persisted after restart. City Performance's 13 toggles matched the fresh defaults (nine behavior switches on, four diagnostics/trace switches off). The real Custom menu prefilled all 24 world fields. New Day 1 had $551.37 and one $75,454.35 loan. The world reached day 2 at 00:05, with $41.37 and $75,140.35 principal; save/quit/restart/reload preserved day, cash, debt, all world settings and the complete-v1 scenario marker. Main session `dc5f0d43`; reload session `a2ed72bc`.

PASS: 12 bounded running-cost fixtures executed inside native Mono using actual catalogue items and the patched RLO billing methods: 12h and 24h fryer/fridge use, closed-day refrigeration, sink allowance, unchanged home bills, factory equipment, office plan-only versus three-computer IT charges, outage credit and law-firm internet requirement. Fixtures used temporary registrations while paused, were removed synchronously without saving, and verified unchanged world money/day/registrations. Subsequent reads also matched cash, debt, settings and marker. This is native billing-method coverage, not actual player leases, nightly/monthly business settlement, office closure presentation or Utilities/BizMan UI acceptance.

Restored the complete original profile, all 3,144 hashes, preferences and install manifest; stopped the game and released ownership. Disposable test profile and structured evidence remain under Mod Pack's ignored `build/release-session2-20261023/`. The two game logs were archived and SHA-256 verified at `C:\Users\Peter\HART-Diagnostics\Performance\release-session2-20261023`; temporary Mac log copies were removed. Normal profiles were untouched. Windows agent1 remained reserved by the Persistent City session throughout; no Windows gameplay claim is made.

The Mac's unaudited game hash disables most City Performance behavior even when its toggles are on. Known Windows-only Bakery/Hyperreal bundle errors remain. RLO reports FAIL in minimap alignment, transit network and transit-pole diagnostics; these are outside this scoped release check and are not cleared by it. Native Windows all-on behavior, rendered UI/performance, actual business bills and the Steam install path remain manual checks. Campaign Objectives 1.2.1 was already pinned after the earlier passing integration; this fresh pass confirms the updated combination without changing Campaign Objectives or adding an untested pin.

No Workshop upload, force-push or history rewrite. Food Trucks repository/worktrees/files and Campaign Objectives files were untouched. Artwork images were not redesigned or re-rendered. The required gallery redraws are in the shared `DESIGN-HANDOFF-MODS.md`.
