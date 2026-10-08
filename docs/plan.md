# Plan

A public Workshop mod that rebalances six Workshop mods so they fit a hard, $0-ish, no-new-loans game. Written 8 October 2026 against build 3682. The reviewed mod versions are the hashes in `Core/Targets.cs`. The code reviews behind each patch live in the HART repository (`docs/research/modpack-compatibility-2026-10-07.md` and `docs/research/modpack-review-2026-10-07/`). Nothing here has run in the game yet.

## Working assumptions

- **Name:** working title "Hard Mode Patch". This repository, MIT. Built with the .NET SDK's C# compiler against the game's own assemblies (`tools/build.sh`), Harmony 2.4.2 in `Dependencies\`.
- **One patch per target mod, found at runtime.** The patch never references the target DLLs at compile time; it finds their types by name with `AccessTools.TypeByName`, so a viewer missing a target mod loses only that patch.
- **Two levels of trust.** A target whose DLL hash matches a reviewed version is *verified*. A new hash whose hooked methods all still exist with the same signatures is *unverified*: the patch applies, logs a warning and shows the mod as unverified on the options page. A missing method or changed signature skips that target and says so. Target authors update on their own schedule and the Workshop can't pin versions, so this keeps viewers balanced while a new version is reviewed.
- **Every rule has an option** on a native Mod Options page, defaulting to the hard values below, so viewers can turn single rules off.
- **No files written outside the mod folder, no network.**

## Patches

| Target | Change | Hook (decompiled names) |
| --- | --- | --- |
| Hardcore Ambitions | Keep the $75,454 student loan, $551 cash and the vehicle and boat prices. Skip the 23 `GameVariables` writes and the loan-rate recalculation, so the save keeps its custom difficulty. Skip the three investment funds. | Prefix `HardcoreAmbitionsPrototypeMod.EnforceHardcoreGameVariables` → skip. Prefix `EnsureStartingReserve` → set cash to $551.37, set its `StartingReserveInitialized` flag, return true. |
| Hardcore Ambitions | Vehicle prices stop leaking into other saves in the same session. | Prefix `ApplyCatalogPriceTargets`: snapshot catalogue prices before the first write. On loading a save without `HardcoreAmbitions.SaveEnabled`, restore the snapshot. |
| Meaningful Ambience | Decor adds a share of the customers the game sends, not a share of building capacity. Preset 10% means +10% at most, so the save's 0.1 customer promotion, marketing and opening-hour factors carry through. | Postfix `GameBridge.ProjectRetail`: `AmbienceRaw = VanillaRaw × score × bonus`, then recompute `CombinedRaw`, `FinalTarget`, `AddedAfterCapacity` and the blocked counts the same way the method does. Offices already scale with game clients. |
| Bakery | Prices at vanilla bakery margins. | Postfix `ItemsGetter.RegisterModItem` for `bakeryxx:` products: set `wholesalePrice` and `defaultMarketPrice`. Defaults below. |
| Internet Cafe | Off screen, at most 2 customers per PC per hour (30-minute sessions). | Postfix `RetailBusinessSimulator.GetMaxCustomerCapacityThisHour` when the simulator is `InternetCafe.CafeBusinessSimulator`: cap at 2 × the PCs placed in the building. |
| Vending Machines | Placing a machine in a rival building no longer lets you pick up the rival's furniture. | Prefix `ItemController.SecondaryInteract` and `TryToGrabItem`: while Vending's `RivalPlacement._raised` and the game's `isBuildingTemporarilyEditable` are both on in a building the player doesn't rent, allow only `vendingmachines:` items. The developer console's EditBusiness still works on its own. |
| Empire Bank | Withdrawals are taxed at the save's rate when taken. | Prefix `BigAmbitionsPlayerEconomyGateway.TryChangePlayerMoney`: positive amounts whose reason mentions a withdrawal × (1 − tax %). The licence-upgrade rollback refund stays untaxed. The game taxes business income from registrations, not transactions, so re-labelling the transaction would not tax it. |
| Empire Bank | No emergency central-bank borrowing. | Prefix `EmpireBankEmergencyCard.OnRequest` → skip, with a notice. |
| Empire Bank | No 850 minimum-credit trick. | Transpiler on `LoanApplicationService.Create` and `CreditCardApplicationService.CreateApplication`: the `Math.Max(product.MinimumCreditScore, BellCredit(rng))` call becomes a call that keeps the drawn score. |
| Empire Bank | Loan APR ≤ 15%, fee ≤ 3%, term ≥ 6 months; card APR ≤ 30%. | Prefixes on `EmpireBankRuntime.UpdateLoanProduct` and `UpdateProduct` refuse edits beyond the limits with a message; postfix on `AdjustProductRate` clamps the rate. |
| Empire Bank | No contact with the author's servers. | Prefixes skip `EmpireTelemetryClient.FetchStatsIfStale` and `EmpireBankSupportersView.StartRemoteRefresh`. The opt-in heartbeat stays the author's. |

### Bakery prices

Margins per unit close to the vanilla bakery and coffee items (croissant $4.29, cupcake $3.10, donut $2.28, coffee $3.00):

| Item | Wholesale | Market | Margin | Was |
| --- | ---: | ---: | ---: | --- |
| Bread | 3.00 | 7.50 | 4.50 | 12.00 / 50.00 |
| Bun | 2.00 | 5.00 | 3.00 | 8.00 / 28.00 |
| Chocolate cookie | 1.50 | 3.80 | 2.30 | 5.00 / 20.00 |
| Latte | 1.10 | 4.50 | 3.40 | 2.00 / 8.20 |
| Americano | 0.90 | 3.90 | 3.00 | 3.50 / 9.00 |

`ProductMarketEntry` stores only a price index, not prices, so the market reads the patched defaults. A business that already set its own selling prices keeps them (they're per business in the save).

## Not patched

- **Hardcore Ambitions' student loan terms** stay the mod's fixed $16 a day interest and $158 a day payment. The save's bank interest setting doesn't reach it.
- **Bakery's logo copy** into the game install stays. It is the game's intended folder for modded logos; the README tells viewers how to remove the files.
- **Vending Machines' own-building rule** (no fee or share) stays. Machines in your own shop are furniture you paid for.

## Order

1. Scaffold: entry class, target gate, options page, build and install scripts, tests for the pure logic. Done 8 October 2026.
2. Patches in order of risk to the series: Hardcore Ambitions, Meaningful Ambience, Internet Cafe, Bakery, Vending Machines, Empire Bank. All written 8 October 2026; `tools/check-hooks.sh` resolves and applies every hook and checks every body fingerprint against the reviewed DLLs. Not yet run in the game.
3. Review loop with three reviewers until they agree, as for the food truck mod. Round 1 (8 October 2026): all three said fix first; fixed in `c36db9c`. Round 2: one ship, two fix first; fixed in `1f59deb` (accepted negotiations, the sign action, body contracts for every Empire Bank hook and the student-loan helpers, exact member and parameter types, the product-limits toggle, pending applications). Round 3: all three said ship to in-game test. Two follow-ups from it are in the next commit: compiler-generated ordinals are masked before fingerprinting, so an unrelated method added to a target class doesn't switch its patch off, and the product-limits toggle does nothing at the main menu. Accepted limitation: products normalised while a save loads aren't marked as a bank change; they are normalised again on every load.
4. Build on homepc, install, and run the test save (HART repository, `docs/research/modpack-compatibility-2026-10-07.md`, "Test save before Episode 1") with the patch: Hardcore start (loan, $551, no funds, custom values intact in the save), an unstaffed one-PC cafe, a decorated shop's ambience ledger, Bakery prices in the wholesale app, a rival-building vending placement, and a second save loaded in the same session showing vanilla car prices.
5. Message each target mod's author before publishing: what the patch changes and why, and which changes they could make themselves (Empire Bank's consent gate, a Bakery price option, Ambience scaling with demand). Each upstream fix retires a patch. Check the Workshop's and Hovgaard's rules on mods that patch other mods.
6. Publish on the Workshop next to Rival Balance.

## Sharing

- Nothing is pushed to other authors' mods. Viewers subscribe to a Steam collection that links the original mods, Rival Balance, Campaign Objectives and this patch; Steam fetches each from its author, and the patch changes their methods in memory.
- Settings stored per PC (Economy Expansion, Dynamic Rivals, Meaningful Ambience presets) go in the collection description and in a Campaign Objectives campaign that lists the recommended values.
- When a target updates: diff its decompiled code against the reviewed version, then add the new hash to the verified list or fix the patch, and publish a patch update.
