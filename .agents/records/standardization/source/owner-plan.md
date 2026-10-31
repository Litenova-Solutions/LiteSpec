# Standardization

**Created:** 2026-10-31
**Status:** draft, ready for owner review
**Source:** `spec.md` in this folder. Part 0 holds the owner's direction and the decisions it settles (D1 to D46); it wins over every other part. `proposals-lower-layers.md` explains the Behavior, Implementation, Proof and Governance changes with examples. The evidence is in `inputs/`:

- `session-findings.tsv`: 115 findings from the 2026-10-30 review of the Intent layer, the Behavior layer and the seams.
- `findings-verified.tsv`: those findings and 9 new ones rechecked against `v3` head `c9b2725` on 2026-10-31 (107 open, 8 changed, 9 new).
- `outside-review-intent-hierarchy.md`: the Intent hierarchy against GitHub Spec Kit, OpenSpec, Kiro, the opportunity solution tree, impact mapping, ISO/IEC/IEEE 29148 and Volere.
- `outside-review-lower-layers.md`: 81 findings on Behavior, Implementation, Proof and Governance against outside practice.
- `deck-sources/`: the sources of the teaching decks and the Lantern example.

**Branch:** LiteSpec `v3`. The release being prepared is 3.0.0 (manifest `version`, `CHANGELOG.md` `## v3.0.0`); `v2.0.0` is the highest tag, so 3.0.0 is not yet published and every rename lands in it with no compatibility work. NightHall (`D:/Personal-Projects/Nighthall`, branch `v1`) migrates in the last wave.

## Goal

Make LiteSpec plainer and consistent in every layer, starting with Intent:

- **Intent:** the topic page holds its requirements, a requirement names its reasons in one `cites` list, only Behavior fulfils a requirement, a journey walks one actor, and every record type says why it exists and when to skip it.
- **Language:** one standard for every name and value a project writes and every message a tool prints: eight word classes for field names, one tense and voice for each kind of text, a name form for every record type, one grammar for finding codes, on one page, `docs/core/language.md`.
- **Behavior:** fewer, clearer concepts: automations instead of reactions and jobs, sagas for orchestration, entry points as doors, typed inputs, privacy positions that match the law, and atomic groups bound by their rule.
- **Implementation:** checks that run where the pages say they run, mark styles per language, stable provision ids, and rules sized to the project.
- **Proof:** "verified" that means verified, proof state that CI can see, loose checks daily and strict ones at release.
- **Governance:** approval stays loose for one-person teams; one exception record replaces four ways to relax a check.
- **Teaching and consumers:** the decks in `D:/docs`, the Lantern example and NightHall move to the new names and structures.

## Rules Every Slice Follows

- Part 0 of `spec.md` wins.
- AGENTS.md: ASCII only, Title Case headings, one term per concept, provision parts in order, capitalized modals only in a Statement.
- The language standard (spec Part B) from the moment wave 1 writes it: every new field, value, identifier, finding code and message follows it.
- Rename every citation in the same change; no aliases or compatibility shims (`litespec/core/maintaining#replace-every-citation-on-a-rename`).
- Links are written on the record that points; inverses are computed.
- Never name a consumer project in LiteSpec's `docs/`.
- Each fact once: a rule stated on two pages keeps one home and the other page cites it.
- Every Checked by line names a check that exists; every finding code has a test.
- Approval stays loose (D25): no slice adds a reviewer, a person-only approver or a review gate.

## Waves

| Wave | Spec parts | What it changes | What green proves |
|:---|:---|:---|:---|
| 1 | 0, A, B, C (pages), H | Research and decision records; `docs/core/language.md`; rewritten provisions on `intent.md`, `model.md`, `layers.md`; the questions table; glossary entries | `provisions` and `validate` pass; every decision has its record |
| 2 | C1, J1, J2, K1, L1 | Correctness fixes that make today's checks trustworthy: glossary term deciders, journey fulfilment, browser proofs, blocking by own status, unknown codes, approval bugs, analyzer pin, fail loud | Build and tests pass; each fix has a test that failed before it |
| 3 | A, C (code) | Intent schema, templates, checker and builder: topic, `cites`, coverage checks, question lifecycle, Intent renames | Build, tests, `validate`; the Lantern example passes `consumer` with the expected findings |
| 4 | B | The field grammar and renames across all layers; mini-languages to objects; finding codes to B8; value-style and identifier checks | No `FIELD_NAME_SHAPE`; every finding code matches B8; templates pass the value-style checks |
| 5 | D | Behavior gaps and inconsistencies | Each confirmed Behavior finding closed |
| 6 | I | Behavior design: automations, sagas, entry points, typed inputs, privacy, atomic groups | Build, tests, `validate`; the schema has no `reaction`, `job` or `process` |
| 7 | K, L | Proof and governance model: tracked proof state, `--release`, `--since`, proof words, book as renderer, exception record, loose approval stated | `--release` blocks on an unverified scenario in a fixture; one exception type remains |
| 8 | J | Implementation: marks per language, folder and symbol checks, stable provision ids, size-aware Defaults, honest enforcement, the code page | Build, tests, `validate`; every Statement Checked by a real check or Reviewed for |
| 9 | E | Seams: direction rules, one home per provision, docs that match checks, words with two meanings | Same, for the cross-layer findings |
| 10 | F | Book labels, the Lantern example, the three decks | Book tests pass; Lantern passes `consumer`; decks rebuilt with no overflow |
| 11 | G | NightHall migrated, one slice per module | NightHall `consumer` and `model check` pass at the held count its open questions justify |

Verify ladder for LiteSpec waves (AGENTS.md, Verification):

```bash
dotnet build LiteSpec.slnx
dotnet test --solution LiteSpec.slnx
dotnet run --project apps/maintain/src/LiteSpec.Maintain -- provisions
dotnet run --project apps/maintain/src/LiteSpec.Maintain -- validate
git diff --check
```

## Scope

### In

Everything in `spec.md` Parts A to L.

### Out

- Tagging or publishing the 3.0.0 release.
- Answering NightHall's open business questions; they stay with their deciders.
- Stricter approval: a distinct-person second reviewer, owner versus approver identities, approval fields inside the hash, a sampled-review gate, an emergency-change process (owner direction, 2026-10-31).
- New record types beyond `topic`, `automation`, `saga` (a kind) and `exception`.

## Open Questions

### Blocking

1. **Approve decisions 0001 to 0009** in `docs/intent/decisions/`, still draft while the pages cite them as settled (finding I3). Owner action, before the tag.

### Not blocking

- The record type name for the requirement container: `topic` is recommended, `capability` the alternative. Decided in wave 1 if the owner states no preference.
- The threshold for the saga-candidate finding (D30): 4 aggregates is recommended; the largest consumer has 9 atomic use cases above it, the largest writing 31.
- The legal wording of the privacy positions (D33): counsel confirms before wave 6 lands.

### Settled

- D1 to D24: owner direction of 2026-10-31 ("I like your simplification and suggestions, including all gaps and inconsistencies, let's standardize naming and field names as well").
- D25 to D46: owner direction of 2026-10-31 ("overall your suggestion about other layers sounds good"), with approval kept loose.

## Next Step

Run `plan-compile` on this folder to cut the waves into a slice ledger, then `plan-execute`.
