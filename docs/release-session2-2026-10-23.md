# Release session 2, 23 October 2026

The HART series preset explicitly sets `rent_rebalance=0`, `equipment_utilities=1` and `office_it=1`. Turning rent off uses the existing unmodified market-rent path and also overrides an older preset value of 1. A hidden 1x mode would misrepresent the existing “twice/three times” option. The optional standalone rent rule and signed lease contracts are unchanged. The running-cost implementation was already on main; this release activates it in place of flat rent increases and updates the preset exporter, tests and copy.

Search covered all local and remote-tracking branches in the Big Ambitions repositories (excluding the forbidden Food Trucks repository) and HART Group. No additional EE implementation was found. Relevant history: HART Group's `docs/research/modpack-review-2026-10-07/economy.md`, `docs/research/real-life-overhaul-modpack-audit-2026-10-14.md`, `docs/hard-mode-patch-plan.md` and `mods/modpack/modpack.json`. Older video/research plans are historical. EE itself remains excluded; RLO owns billing. Current manifest and series plan now match the new decision.

Full `tools/check-all.sh` passed with RLO 0.49.0: 158 Core tests, available-target hook checks, 38 scenario, 76 factory, 21 cost, 30 compatibility, 17 unreviewed-RLO gate and 26 settings checks (all 74 rows). Empire Bank, Hardcore Ambitions, Internet Cafe, Meaningful Ambience and Vending Machines DLLs were unavailable and were explicitly skipped by HookCheck. This run did not repeat CostCheck against 0.45.0; the prior documented 0.45.0 result remains historical. Logs: `build/release-session2-checks.log`, `build/release-session2-package.log`.

Pending: actual RLO nightly bills and Utilities/BizMan UI for food shop, office, 24-hour opening and factory; office closure without internet; balance calibration and native save/reload. `04-house-rules.png` still has old rent copy and must be redrawn by Opus before gallery use. Only its HTML text changed.

## Package verification

- Archive: `build/HardModePatch-0.5.0.zip`.
- SHA-256: `26e8e6d00995e16e7b69a1a1af320b42a3df15f1318965a8683ff8417bfbb7b7`; matching `.zip.sha256` sidecar.
- DLL SHA-256: `638983edb460aec8ad4b0aba8b15d27b135aa6084f58862eec2ea8f4179b82da`.
- 13 files: ZIP CRC, exact file inventory, every decompressed hash, package manifest and approved thumbnail equality passed. Exactly one root DLL and only `preview.png` as the root image; no preview.jpg.
- Full description: `workshop/DESCRIPTION.bbcode.txt` (4727 characters, below 8,000), ends with the PayPal Support section.
- Change notes: `workshop/CHANGENOTE-0.5.0.txt`, `.bbcode.txt` and `.md`.

## Rig availability and limits

Read-only availability checks and retries during this session found both rigs occupied. At 13:55 CEST, Windows agent1 PID 34936 was reserved by `persistent-city-neighborhood-finance-20261023`; Mac agentmac PID 18917 was reserved by `street-food-cooler-release-20261023`. No reservation was taken over; no game, mod, profile, save, preference or manifest was changed. New native integration and running-cost acceptance remain pending. These are verified build candidates, not a claim that the missing in-game checks passed.

No Workshop upload, force-push or history rewrite. Food Trucks repository/worktrees/files and Campaign Objectives files were untouched. Artwork images were not redesigned or re-rendered. The required gallery redraws are in the shared `DESIGN-HANDOFF-MODS.md`.
