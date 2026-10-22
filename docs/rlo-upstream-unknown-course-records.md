# Upstream PR draft: keep course records RLO doesn't know

Not opened. A description for a pull request to github.com/AdultCudi/real-life-overhaul from Peter's fork, written 22 October 2026 against v0.49.0. The maintainer asks for anything bigger than a small fix to be discussed first, so this may start as an issue. Follow CONTRIBUTING.md: 0 warnings, a "Fixed:" line in `reallife_changes_next` in `en`, `pt` and `zh-cn`, an offline check that fails on the old code, no version change, no tool attribution.

Why HART. Mode Patch wants it: HMP adds three school diplomas (ids 18, 19, 34) to `StudyCatalogue.All` at runtime. When a player runs a build of RLO that HMP hasn't reviewed, or removes HMP, RLO drops those enrolments and results on its next save. HMP now holds a copy in its own save line (see [RLO compatibility](rlo-compatibility.md)), but the loss itself happens in RLO and affects any mod that adds courses.

---

**Title:** Careers: keep course records whose course isn't in the catalogue

**What happens**

`CareersStore.Read` turns each `c` and `r` record into an `Enrolment`. `Course(Fields)` returns null when `StudyCatalogue.Find(id)` has no definition, and the record is skipped. `CareersStore.Write` then writes only the enrolments it kept, so the next save loses the record for good. That happens when a course was added by another mod that is missing this session, or a course id is retired in a later RLO.

**Change**

- `StudyFile` gets a list of the raw records it couldn't place, per part (`c` or `r`), kept as the text RLO read.
- `CareersStore.Read` puts a record whose course id isn't in the catalogue there instead of dropping it. Records that fail for another reason are handled as today.
- `CareersStore.Write` writes those records back at the end of their part, unchanged.
- Nothing else reads the list: no fee, loan, mask, Campus row or result is computed from an unknown course.

**Checked offline**

A new check in the careers checker: a line with an ordinary course, an unknown-id course under way and an unknown-id result, read and written twice. Before the change the two unknown records are gone after the first write; after it both are present, byte for byte, and the ordinary course, skills and student debt read as before. The existing checks pass. Not tried in the game.

**Not in this PR**

A supported way for a mod to add courses (for example a `StudyCatalogue.Register` called before the careers director starts) would let mods stop writing the catalogue array themselves. Happy to open that separately if it's wanted.
