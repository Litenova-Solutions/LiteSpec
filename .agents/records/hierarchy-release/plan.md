# Hierarchy, Naming and Book Release

**Created:** 2026-10-30
**Status:** ready-to-run
**Source:** `.agents/records/hierarchy-release/spec.md`. Part 0 (lines 7 to 35) holds the owner's binding decisions and the judge's corrections and wins over Parts A to F. The evidence behind it is in `.agents/delegate/hier/done/` (seven investigation reports) and `.agents/delegate/plan/done/` (the six planning sections the specification was assembled from).
**Branch:** LiteSpec `v3` for waves 1 to 5; NightHall `v1` (`D:/Personal-Projects/Nighthall`) for waves 6 to 9. Both already exist and are pushed; commit and push each green wave to its repository's branch only.

## Goal

Make every LiteSpec record sit in one complete structure and make the book show it:

- No record without a parent, an error even on drafts, with drafts allowed as parents so authors can write top down or bottom up.
- Every requirement names its origin; features advance goals and solve problems; journeys serve a goal record; results and scenarios read their parent from their identifier.
- One concept, one field name, one shape, with the mechanical half checked by `litespec-maintain validate`.
- The Links table reads both ways, and the book takes its labels from it.
- `litespec book` is rebuilt to the approved mockup (`.agents/mockups/book-v6.html`).
- LiteSpec's own research and decisions live in its tree as records; research records state a conclusion.
- LiteSpec's approval tool writes correct stamps (the four defects in NightHall decision J39 fixed).
- NightHall is migrated: the agents research and decide every gap, record each judgement, and approve NightHall with the fixed tool (owner's decision 14, 2026-10-30).

## Rules Every Slice Follows

The writer brief header (`brief-write.md`) carries these to every worker:

- Part 0 of the specification wins.
- ASCII only, Title Case headings, one term per concept (`litespec/core/authoring#keep-authored-prose-ascii-safe`).
- Provision parts in order, heading verb from the validator's list (AGENTS.md, Writing).
- Rename every citation in the same change; no aliases or compatibility shims (`litespec/core/maintaining#replace-every-citation-on-a-rename`).
- Never name a consumer project in LiteSpec.
- Links are written on the record that points; inverses are computed (`litespec/core/model#write-links-on-the-record-that-points`).
- The book shows only LiteSpec terms, LiteSpec's own explanations and computed facts (the six book standards in `docs/core/model.md`).
- NightHall judgement items are researched and decided by the agent, each with its evidence recorded; NightHall tests change only by mechanical call-site replacement.

## Verify Ladder

`bash .agents/records/hierarchy-release/verify.sh <out-dir>` runs every step and reports each:

| Step | Proves | Waves that must pass it |
|:---|:---|:---|
| build | `dotnet build LiteSpec.slnx` compiles every project | all |
| provisions | the provision index regenerates with no diff | all |
| validate | `litespec-maintain validate`: schemas, provisions, page contracts, links, and (from wave 1) the naming rule | all |
| tests | `dotnet test --solution LiteSpec.slnx` | all |
| whitespace | `git diff --check` | all |
| book-smoke | `litespec book` renders the fixture model | 3 to 8 (the old book until wave 4, the new one after) |
| nh-build, nh-check, nh-consumer, nh-book | NightHall's model builds and checks against this LiteSpec, and its book renders; run with `NIGHTHALL=1` | 6 to 9 |

NightHall's own tests are not run by this plan.

## Waves

| Wave | Theme | Slices | Foundations | What green proves |
|:---|:---|:---|:---|:---|
| 1 | LiteSpec contract | 1 | `contract.records-and-links` | The provisions, Links table, schema, templates, glossary and naming rule describe the new structure, and validate passes |
| 2 | Check foundations; LiteSpec's own records | 2 | `checks.foundations` | The builder extracts the new links, derives the new result form, exports `follows`; this release's research and decisions are records |
| 3 | New checks; book data core | 6 | none | `record-without-parent`, `feature-without-goal`, `result-without-scenario` report on fixtures; the book's lexicon, outline and enrichment are tested |
| 4 | Book shell and client; renames in existing checks | 3 | `checks.renames` | The new book renders from shards with the mockup's design; existing checks read the renamed fields; the finding table lists the new codes |
| 5 | Retire the old book; scale; docs; approval tool fixes | 4 | `book.retire-old` | The old pipeline is gone, a 21,500-record model renders within the shard caps, `docs/tools/book.md` describes the new command, and the approval tool stamps screens, reaction scenarios and decisions correctly |
| 6 | NightHall mechanical migration | 1 | `nighthall.baseline-and-renames` | NightHall uses the new field names and the derived result form; its model builds with the new LiteSpec |
| 7 | NightHall gaps, researched and decided; link fields | 6 | none | Every journey, feature and requirement names its parent, chosen from evidence and recorded; `readBy` adopted, `sentBy` and `writes` gone |
| 8 | NightHall close-out | 1 | `nighthall.close-out` | `record-without-parent` reports zero in NightHall; every judgement group has a decision-log entry |
| 9 | Approving NightHall | 1 | `nighthall.approval-waves` | Agent-reviewed batches are approved with the fixed tool; the model check shows no finding the approvals introduced; J39 is closed |

Totals: 25 slices; 7 `judge`, 18 `muse`, 0 `free`. Waves are small because nearly every change in this release edits shared contract files; the ledger keeps the slices of a wave on separate paths.

## Scope

### Out

- **Tagging or publishing a LiteSpec release.** Not part of this plan (owner's decision 13).

### Irreversible

None. Every change, the NightHall approval stamps included, is a commit on `v3` or `v1` that a later commit can revert; nothing is published, tagged, deployed or sent outside the two repositories.

## Open Questions

### Blocking

None. Every choice is settled in Part 0 of the specification.

### Design

Settled in the specification and not to be reopened:
- the product parents every root type, with no `product` field (C1);
- written result and scenario identifiers keep their form (C2);
- one `record-without-parent` code (C3);
- `solves` stays a list whose first entry is the parent (C4);
- the book is one offline shell with sharded data and no Mermaid (Part E, "Risks the Executor Should Not Reopen").

Choices left to the executor, recorded in `decisions.md`:
- the exact wording of a Statement where a section gives only its intent;
- the order of items inside a slice;
- the fixture shapes for new tests.

## Budget

| Budget | Value |
|:---|:---|
| Wave size | 1 to 6 slices, as the ledger sets |
| Fix rounds per slice | 2 by workers, then the judge |
| Concurrency | auto; Muse Wonder up to 10 at once, Muse Spark as memory allows |
| Worker effort | max |
| Time | none |
