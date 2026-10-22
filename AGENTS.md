# HART. Mode Patch

A public Big Ambitions mod that patches other authors' Workshop mods at runtime with Harmony. Read `docs/plan.md` before changing what a patch does.

Mod ownership (Peter, 22 October 2026): City Performance owns performance changes and diagnostics. HART. Mode Patch owns cross-mod compatibility and the modpack's intended gameplay, economy and balance. Bakery Help discovery/build scheduling and Hyperreal lamp discovery belong only to City Performance; Bakery prices and Hyperreal map-return compatibility belong to HART. Mode Patch. The HMP 0.4.2 / CP 0.4.4 candidate pair implements this split. Preserve legacy conflict refusal so older HMP discovery hooks cannot run alongside CP's replacement. Incidental caches needed for gameplay rules stay with those rules.

Performance contribution routing (Peter, 18 October 2026): performance fixes requiring Harmony ship through City Performance in the HART modpack. Improvements to another project's own code that do not require Harmony should go upstream as PRs to its owning repository. A source change or candidate build does not establish installation or runtime validation.

- Never commit game DLLs, Harmony, other authors' mod DLLs or their decompiled code. Reference their methods by name only.
- One file per target mod in `src/`, implementing `ITargetPatch`. `ResolveHooks` must list every method the patch touches, so a changed target is caught before anything is patched.
- Pure logic (rules, numbers, decisions) goes in `Core/` with a test. `Core/` has no game or Unity references.
- When a target mod updates: diff its decompiled code against the reviewed version, then add the new hash prefix to `Core/Targets.cs` or fix the patch.
- Every behaviour change gets a rule on the options page, defaulting to the hard value, with an English label in `config/Locales/en.json`.
- Keep implemented and planned patches apart in the README. Don't describe in-game behaviour as tested until it has been checked in a save.
- Build with `tools/build.sh`; test with `dotnet test tests/HardModePatch.Core.Tests.csproj`.
