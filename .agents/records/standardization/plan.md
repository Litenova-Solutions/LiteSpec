# Standardization

**Created:** 2026-10-31 02:40
**Status:** ready-to-run
**Source:** `source/spec.md` (copied from `.agents/plans/Standardization/`); Part 0 wins over every other part. `source/proposals-lower-layers.md` explains Parts I to L with examples; `source/inputs/` holds the evidence. `source/owner-plan.md` is the owner's wave outline.
**Branch:** LiteSpec `v3`; NightHall `v1` for wave 11.

## Goal

Land spec Parts A to L in LiteSpec 3.0.0 (not tagged), then migrate NightHall to the new names and structures.

## Rules Every Slice Follows

`brief-write.md` carries them to every worker. The ones writers break most:

- Part 0 wins (`source/spec.md:8-70`).
- ASCII, Title Case, one term per concept, provision parts in order (`AGENTS.md`, Writing).
- Rename every citation in the same change; no alias (`litespec/core/maintaining#replace-every-citation-on-a-rename`).
- Never name a consumer project in LiteSpec (`AGENTS.md`, Principles).
- Approval stays loose (D25).
- C# is checked with `fuse check` by the worker before the done file.

## Verify Ladder

`verify.sh <out-dir>`: build, provisions (stage first; it diffs against the index), validate, tests, whitespace, book smoke. `NIGHTHALL=1` adds NightHall's model build, model check, consumer check and book. Every LiteSpec wave must pass all LiteSpec steps; wave 11 must also pass the NightHall steps at the held count its open questions justify.

## Waves

| Wave | Spec parts | Slices | Foundations | What green proves |
|:---|:---|:---|:---|:---|
| 1 | 0, A, B, C (pages), H | 8 docs slices | `contracts.md` (provision IDs) | `provisions` and `validate` pass; every Part H record exists |
| 2 | C1, J1, J2, K1, L1 | 6 code slices | none | Build and tests pass; each fix has a test |
| 3 | A, C (code) | cut at wave start | schema names from spec B2 | Build, tests, `validate` |
| 4 | B | cut at wave start | judge rename script | No old field name; every finding code matches B8 |
| 5 | D | cut at wave start | | Each Behavior finding closed |
| 6 | I | cut at wave start | | No `reaction`, `job` or `process` in the schema |
| 7 | K, L | cut at wave start | | `--release` blocks on an unverified scenario; one exception type |
| 8 | J | cut at wave start | | Every Statement Checked by a real check or Reviewed for |
| 9 | E | cut at wave start | | Cross-layer findings closed |
| 10 | F | cut at wave start | | Book tests pass; Lantern passes `consumer`; decks rebuilt |
| 11 | G | one slice per NightHall module, in a NightHall plan folder | NightHall rename script | NightHall `consumer` and `model check` pass |

Waves 1 and 2 share no path and run at the same time. Each later wave is cut into slices when it starts, because its exact files and names depend on what the waves before it land (`decisions.md`).

## Scope

### Out

- Tagging or publishing 3.0.0 (owner).
- NightHall's open business questions (their deciders).
- Approving decisions 0001 to 0019 (owner; the run writes them as draft with a decider and a date).
- Stricter approval (owner direction, 2026-10-31).

### Irreversible

Pushing to `v3` and `v1` after each green wave; the user asked for it.

## Open Questions

### Blocking

None for the run. Approving decisions 0001 to 0009 blocks the tag only.

### Design

- `topic` or `capability`: `topic` (the spec's recommendation; the owner stated no preference).
- Saga-candidate threshold: 4 aggregates (the recommendation).
- Privacy wording (D33): written as the spec states, marked for counsel in the decision record.

## Budget

Muse and the judge only; no paid model, no Claude subagents.
