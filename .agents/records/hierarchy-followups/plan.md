# Hierarchy Release Follow-Ups

**Created:** 2026-10-30
**Status:** ready-to-run
**Source:** `.agents/records/hierarchy-release/handover.md` (F1 to F8), with `.agents/records/hierarchy-release/spec.md` Part 0 binding.
**Branches:** LiteSpec `v3`; NightHall `v1`, whose slices live in `D:/Personal-Projects/Nighthall/.agents/records/hierarchy-followups/`. Commit and push each green wave to its own repository.

## Goal

Close every follow-up the hierarchy release left: NightHall's consumer validation (F1), its held records with open code findings (F2), the end of transcription and decision 0084 (F3), the batch-approval improvement (F4), the book's visual and test coverage (F5), exact research sources (F6), release hygiene (F7), and a conformance review of the whole release with its fixes (F8).

## Waves

| Repository | Wave | Slices | What green proves |
|:---|:---|:---|:---|
| LiteSpec | 1 | 5 Muse writers, 7 Muse conformance reviewers (explore) | Book matches the mockup, Part E tests complete, exact sources, changelog, partial batches; conformance findings listed |
| LiteSpec | 2 | Conformance fixes, cut from the reviewers' findings | Every confirmed conformance finding is fixed |
| NightHall | 1 | One slice per module (F1 page fixes and F2 code fixes together), plus cross-module F1 slices | `litespec consumer` and `model check` pass; held findings closed |
| NightHall | 2 | F3: transcribed marks cleared by module; decision 0084 | `transcribed-after-adoption` would report nothing |
| NightHall | 3 | Judge: approval passes with agent second reviews for changed batches | `model check` exits 0 with the held count down to what the open questions justify |

## Scope

### Out

- **The three open questions** (F4): owned by the tax adviser and counsel; never answered here.
- **Tagging or publishing a LiteSpec release.**

## Open Questions

### Blocking

None.
