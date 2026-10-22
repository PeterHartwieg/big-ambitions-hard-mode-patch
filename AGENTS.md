# HART. Mode Patch

A public Big Ambitions mod that patches other authors' Workshop mods at runtime with Harmony. Read `docs/plan.md` before changing what a patch does.

Performance contribution routing (Peter, 18 October 2026): new performance fixes requiring Harmony belong in the sibling City Performance mod for the HART modpack. Improvements to another project's code that do not require Harmony should go upstream as PRs to its owning repository. The existing Bakery/Hyperreal performance patches are already deployed here; any migration must coordinate removal and activation to prevent duplicate behavioral patches. This routing does not move the gameplay/balance patches out of HART. Mode Patch.

- Never commit game DLLs, Harmony, other authors' mod DLLs or their decompiled code. Reference their methods by name only.
- One file per target mod in `src/`, implementing `ITargetPatch`. `ResolveHooks` must list every method the patch touches, so a changed target is caught before anything is patched.
- Pure logic (rules, numbers, decisions) goes in `Core/` with a test. `Core/` has no game or Unity references.
- When a target mod updates: diff its decompiled code against the reviewed version, then add the new hash prefix to `Core/Targets.cs` or fix the patch.
- Every behaviour change gets a rule on the options page, defaulting to the hard value, with an English label in `config/Locales/en.json`.
- Keep implemented and planned patches apart in the README. Don't describe in-game behaviour as tested until it has been checked in a save.
- Build with `tools/build.sh`; test with `dotnet test tests/HardModePatch.Core.Tests.csproj`.
