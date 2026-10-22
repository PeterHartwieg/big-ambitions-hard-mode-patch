# Real Life Overhaul compatibility

Viewers get each Real Life Overhaul release from the Workshop the day it ships, and HART. Mode Patch can't hold them back. So everything HMP does to or with RLO runs only on an RLO build whose code was reviewed for it, and any other build turns those features off.

## Reviewed builds

| RLO | DLL SHA-256 | Source tag | Reviewed |
| --- | --- | --- | --- |
| 0.41.1 | `f1b93c176b33f79d…` | v0.41.1 | 20 October 2026, factory diplomas |
| 0.45.0 | `2f360a365cf7915f…` | v0.45.0 | 21 October 2026, factory diplomas and running costs |
| 0.49.0 | `4f82b4f88f9191782d315f97ac0c33f1bda57e8022c072ce281857c762a8191b` | v0.49.0 (`fd5827c`) | 22 October 2026, all RLO features |

The prefixes are in `Core/Targets.cs`. A new build needs a review and a new prefix there.

## What an unreviewed build does

`src/RloGate.cs` hashes the RLO assembly once when it loads. For a build that isn't in the list:

- One `Player.log` warning, for example: `[HART. Mode Patch] Real Life Overhaul 0.50.0 (1234abcd5678ef90) is a version HART. Mode Patch hasn't reviewed. Factory diplomas, running costs, owner job pay and the hotel 'no home' rule are off until HART. Mode Patch is updated; RLO runs as its author published it. A save's factory diploma records are kept for that update.`
- No Harmony patch on RLO or on the game's machine purchase methods, and HMP's three courses are not added to RLO's catalogue. Machine sales are unrestricted, RLO's own utility bills and owner pay cut apply, and "no home" keeps the game's value. Rent, happiness, the indebted start and the other target patches are unaffected.
- The options page shows Real Life Overhaul as "Not patched: this version hasn't been reviewed yet", and the running costs and owner job pay sections show their "unavailable" line.
- One in-game notice when a save's city has loaded (`hardmode_rlo_unreviewed`), at most once per session.

Such an RLO doesn't know HMP's course ids 18, 19 and 34. Its careers reader drops their records, and its next save would lose a player's diploma enrolments and results. Before RLO first reads the save (RLO reads it from a timer after game-loaded, HMP acts in a game-loaded prefix), HMP copies those records to its own save line, `hardmodepatch:factory_diplomas_held`, and the notice says so instead (`hardmode_rlo_unreviewed_held`). RLO's own line is never changed under an unreviewed build. Ordinary courses, skills and student debt stay RLO's. Tuition paid for a held course isn't refunded.

When a reviewed RLO and HMP's courses are loaded again, the same prefix adds each held record whose course RLO's line no longer has, and RLO's own reader then checks it like any other record. The hold stays until a later load finds every held course in RLO's line, which means RLO has read and saved it. Held records are copied as they were, so a course under way resumes where it stood when it was held. Restoring needs RLO's careers line format version to match the hold's (version 1 in 0.41.1 to 0.49.0).

Limits: the hold works only while HMP is installed. Removing HMP still loses the records on RLO's next save, as before. A save played under an unreviewed RLO that is then saved without HMP loaded loses them too.

## Review of 0.49.0 (22 October 2026)

Release `v0.49.0` from github.com/AdultCudi/real-life-overhaul: `RealLifeOverhaul-0.49.0.zip`, SHA-256 `8855279ebb94057850adf0a6c9463ed3f07d90cd98509299c010ab673712f697`, matching the digest GitHub publishes for the asset. The DLL inside is the one hashed above. The source was read at tag `v0.49.0`, diffed against `v0.45.0` (983 files changed); the release DLL was not rebuilt from source.

Every RLO member HMP touches, and what changed:

| Feature | RLO members | 0.45.0 to 0.49.0 |
| --- | --- | --- |
| Factory diplomas | `Careers.StudyCatalogue` (`All`, `Find`, `IdsMost`, `Skills`), `CourseDef` constructor, `StudyFile.Holds`, `StudyRules`, `CampusRules._courses`/`Courses`, `CareersStore` (save codec), `Tech.TechCodec` | No change. Ids 18, 19, 34 are still free. |
| | `CareersDirector.Offered`, `Instance`, `Study`, `_broken`; `Settings.Careers`, `CareersStudy` | Unchanged members. The director now builds the Campus app only once the phone exists and adds an interview visit; neither touches the catalogue or study record. |
| | `Tech.TalentCatalogue` constants | One new role constant (`FinancialAnalyst = 29`); the skill and `Diploma` constants HMP reads are unchanged. |
| Running costs | `Utilities.UtilitiesRules.AddDay`, `InternetRules.AddDay`, `Account` fields, `InternetCatalogue.Needs`, `Prices.Level`, `UtilitiesGame.Place` | Code unchanged apart from comments and line wrapping. `InternetRules.Down` now reports a closed plan as a closed account rather than "no plan"; HMP doesn't use it. |
| | `UtilitiesDirector.NeedsNet` and the nightly billing loop | Moved to the partial file `UtilitiesDirector.Internet.cs`; same body, same single read of `Needs`. The loop still calls `UtilitiesRules.AddDay` once per place and service a day and `InternetRules.AddDay` once per connection. |
| Owner job pay | `CareerCatalogue.OwnerFactor(int)` | No change. |
| Happiness | `TravelState.HasRoom` | No change. |
| Indebted start | none patched; RLO's loan tracking is read by `ScenarioCheck` | Finance apps changed only their text box call. |

Offline checks against the 0.49.0 DLL, all passing: HookCheck 10/10 RLO members and both purchase contracts, ScenarioCheck 38, FactoryCheck 63 (including the hold given back through RLO's own reader), CostCheck 21. RloGateCheck 16 against the same DLL with one byte appended, so every shape matches and only the review is missing. None of this ran in the game.

## Upstream

The data loss on an unknown course id is RLO's reader dropping records it can't place. A PR description for RLO to keep them is in [rlo-upstream-unknown-course-records.md](rlo-upstream-unknown-course-records.md); it isn't opened.
