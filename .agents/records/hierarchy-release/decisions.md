# Decisions

## 2026-10-30: Re-cut Waves 1 to 3 into One Parallel Wave

The ledger ran the contract as one judge slice, then checks, then the book core, one wave after the other. The user asked for maximum parallelism with Muse. The contract work splits cleanly by file: the model page, the intent and behavior pages, the sweep of every other page, the naming check and the record checks each own disjoint paths, and every field name they write is fixed by Part 0 and Part B. The judge fixed the shapes that crossed slices (the Links table columns, the Parents table, the checker foundation API and the book shard contract) in `contracts.md` before launch, so 15 Muse slices run at once while the judge writes the schema, templates and checker foundations. The original ledger is kept as `ledger.original.tsv`.

## 2026-10-30: NightHall Runs Beside LiteSpec

NightHall's mechanical migration (N2 to N4) and its judgement work depend only on field names Part 0 fixes, not on the LiteSpec code. The judge ran N2 to N4 by script (counts equal Part F's measurements: 111 features, 112 journeys, 56 requirement feature links, 33 requirement and 8 goal deadlines, 29 constraints, 1 decision, 8 sentBy and 33 writes dropped, 564 derived references), committed it on `v1`, and started NightHall's judgement wave (9 Muse slices) from a plan folder inside NightHall, so its workers watch NightHall's own git state. Verification of NightHall waits for the LiteSpec CLI to be green.

## 2026-10-30: Free Tier Skipped

The free-tier probe returned no working model. Every worker slice runs on Muse (`WAVE_SKIP_TIERS=free`), with the memory reserve lowered to 3 GB and 0.4 GB per worker, because workers run no builds.

## 2026-10-30: Approval Hash Field List

`Approver.SemanticHash` hashes every field except the metadata list in `Checker.Graph.cs`, so `conclusion`, `advances`, `readBy` and the renamed fields enter the hash with no code change; only the documentation comment listing the fields changed.

## 2026-10-30: `result-without-scenario` Runs on Large Projects Only

Scenarios are a large-size type (`RecordTypes.LargeAdded`), and `use-case-without-scenario` already runs only on large projects. Without the same gate every small project's success results would block at tier G for a type it does not write. The check now returns early unless the project size is large; the provision's Checked by line is unchanged because the small size carries no scenarios to show.
