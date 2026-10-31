# Decisions

## 2026-10-31: Cut Each Wave After 2 When It Starts

The spec fixes what changes but not which files carry it once earlier waves rename fields, codes and pages. Waves 1 and 2 are cut now; each later wave is cut into slices when it starts, from the tree the earlier waves left. The owner's wave outline (`source/owner-plan.md`) stays the order.

## 2026-10-31: Waves 1 and 2 Run Together

Wave 1 writes only pages and records under `docs/core`, `docs/reference` and `docs/intent`; wave 2 writes only code, tests, the manifest, analyzer docs and the glossary template. No path is shared, so both start at once and commit together.

## 2026-10-31: Workers Check C# with Fuse

`fuse check` works in LiteSpec without `fuse init` (checked on `Checker.Code.cs`). Code workers start with `MUSE_CHECKS=narrow` and run `fuse check` and filtered `fuse test`, so a wave no longer ends with a round of compile errors (lesson from the hierarchy follow-ups).

## 2026-10-31: The Container Is Named Topic

Spec Open Questions left `topic` or `capability` to wave 1 if the owner stated no preference. The owner stated none; `topic` is the spec's recommendation and is used.

## 2026-10-31: Worker Gets No Permission Constants

Worker became a layer (D40). The generator writes permission and role constants for Domain, Application, Infrastructure and WebApi only, and `req.worker-layer` asked whether Worker needs them too. It does not: a Worker job issues commands, and the Application pipeline authorizes them (`litespec/backend/application#enforce-target-authorization-in-the-pipeline`), so no Worker code names a permission. LS5009, LS5010 and LS5012 keep excluding Worker.

## 2026-10-31: NetArchTest Is Replaced by ArchUnitNET

J1 said "NetArchTest replaced or dropped". `w2.analyzer-pin` pinned ArchUnitNET 0.13.5 in the manifest. Its license was checked at the source on 2026-10-31 (GitHub API: Apache-2.0); the worker's prose called it MIT, and the judge corrected it.

## 2026-10-31: setBy Says Decider, Not Founder

C4 lists `setBy` values `founder`, `operator`, `tenant`, and D22 retires "founder" as a role word in favor of "decider". Part 0 wins, so the values are `decider`, `operator`, `tenant`.

## 2026-10-31: Every Audience Becomes Addresses in Wave 3

B2 renames product and notification `audience` to `addresses`, and D1 of Part D extends it to integration events. Wave 3 renames all three, so one field never has two names between waves.

## 2026-10-31: New Codes Keep Their Part 0 Names Until Wave 4

D3, D5 and C9 name `requirement-uncited`, `constraint-uncited` and `parent-changed`; D39 makes every code follow the B8 grammar, whose problem words exclude "uncited" and "changed". Wave 3 lands the checks under the Part 0 names; wave 4 renames every code to B8 at once.

## 2026-10-31: Waves 1 to 3 Commit Together

After the wave 1 and 2 fix round, two book tests still fail because `model.md` already carries the topic parent rule and the `cites` Links rows, which the book code learns in wave 3 (`BookOutlineTests.Parent_Rules_Match_The_Model_Page`, `BookViewsTests.Connection_Sentences_Resolve_From_Lexicon_Phrases`). Patching the book now would be redone in wave 3. Wave 3 starts on the same tree, and waves 1 to 3 commit together when the ladder is green.

## 2026-10-31: Finding Codes Are Renamed by Script Before the Wave

The 85 code renames of contracts.md are exact hyphenated tokens, so `w4.codes` renamed 1,570 occurrences in 143 files by a whole-token script (no letter, digit or hyphen may touch a match), moved the 38 broken-overlay fixture folders named after codes, and left heading-slug test data alone. Two approved Claimwick known gaps named renamed codes; they were re-stamped. The rename was committed alone, so wave 4's workers start from a HEAD where every code already follows B8.

## 2026-10-31: Part D Pages Land with Part I

Part D's page items (D4, D5) edit the Behavior pages that Part I (wave 6) rewrites for automations, sagas, doors and typed inputs. Wave 5 splits `behavior.md` into its five pages (D5) and lands Part D's code and schema items with the mini-language objects (D12); the remaining Part D page items join wave 6, so each Behavior page is rewritten once.

## 2026-10-31: Every Use Case Declares Idempotency, Queries Included

`w5.schema` made `idempotency` required on every use case, following the provision Name the Idempotency of Every Use Case ("Each use case MUST declare its idempotency mode"), which D2 asks the schema to back. A query declares `mode: none` with its reason. The Claimwick example gained the field on every use case, and its commands an audit position, from what its code does.

## 2026-10-31: Pattern Option Names Keep the Pattern Page's Spelling

The B6 rewrite title-cased the option names in the UI pattern catalog template. A catalog option must match its LiteSpec pattern page exactly, and the Title Case rule (B3) covers record titles only, so the names went back to the pattern pages' spelling.

## 2026-10-31: Every Use-Case Input States Its Origin and Classification

The provision Describe Every Use Case Input with Origin and Classification is a MUST, and D2 asks the schema to back every such claim, as `w6.docs-usecases` noted. `useCaseInput` now requires `origin` and `classification` beside `name`, `meaning` and `type`.

## 2026-10-31: A Saga Names Its Drivers in a List

The contract wrote `drivenBy` as one object; the schema kept the list the old `driver` was, so a saga may be driven by an automation and by a person at once. The list stays, and the aggregate template writes one entry.

## 2026-10-31: Templates Show One Concrete Case Where the Schema Branches

The schema's `if` and `not` rules read the record's own values, so a placeholder `kind` or trigger cannot satisfy them. The aggregate template shows a saga (its saga-only fields say to remove them for other kinds), and the automation template a schedule trigger with a concrete `PT1H`; its Summary says how to write an event or manual trigger and which fields to drop.

## 2026-10-31: A Startup Run Is a Schedule, and Jobs Keep Their Service Fields

Wave 6 dropped the job kind `one-off` and the job fields `serviceLevel` and `evidence` without a contract line saying so; the largest consumer has 5 one-off jobs and uses both fields. D27 allows three triggers (event, schedule, manual), and a run once at deployment start is a time-based start, so the schedule object gains `{ "at": "startup" }`. `serviceLevel` and `evidence` return on automations as schedule and manual fields. `onExhausted` stays removed (D5 lists it among undefined fields). Landed after wave 7, whose schema slice owned the file.

## 2026-10-31: An Automation May Listen to Several Events, and Operations Keep Their Retry Prose

The largest consumer has 17 reactions triggered by more than one event (for example, assigning a ticket seat when a seat is sold or a sold seat is released), each with one handler class per event and one shared record. `triggeredBy.event` takes one event id or a list, so one automation states one act over the facts that start it. External-system operations described their retry behavior in a prose `retries` field, which wave 5 removed with the integer form; it returns as prose, its one shape, beside `onTimeout`. Both land in the post-wave-7 patch with the startup schedule.

## 2026-10-31: An Input or Payload Field May Hold Many Values

D31 gives each input and payload field one scalar `type`. The largest consumer has about 70 inputs that are lists (event ids, listing ids, text values), and the type list has no way to say so. A field that holds a list says `"many": true` beside its element `type` (a predicate boolean, D11). Added with the judge's foundation before wave 11.

## 2026-10-31: Mechanical Moves in the Largest Consumer

The largest consumer's migration ran as judge scripts wherever a contract fixes the answer (kept in the consumer's plan folder): the field renames and objects of waves 3 to 5; reactions and jobs moved to automations (a one-off job became `{ "schedule": { "at": "startup" } }`; one automation was renamed where its name met a use case in the same folder); deviations and known gaps moved to exceptions (a deviation's removal condition kept as a sentence in the page body); the frozen phase became a `gap` exception; input types from the C# message records (4,513 of 4,688), `of` from the id type's aggregate (2,092), `binds` from the use cases citing each atomicity rule (43 of 56). A record-level `onFailure` of `retry: none` or `next-run` became `mode: skip-and-log` (the failure is logged and the next delivery or run carries on), and a retry policy became `mode: retry` with its policy and `then: dead-letter`. What needs judgement (rationales, classifications, purposes, payload types, sagas, advisory authorities, the rest) goes to the consumer's module workers.

## 2026-10-31: The Provision Index Reads the ID Line

Wave 8 made the `**ID:**` line a provision's identity in the validator, but the index builder (`Provisions.cs`) still derived IDs from headings, so two provisions whose headings changed from "Business" to "Policy" were listed under IDs no page states. The builder now reads the ID line too, and keeps the heading slug only as the link anchor; those two provisions keep their released IDs, as D42 allows.

## 2026-10-31: Claimwick Drops Two States No Use Case Reaches

J5's rule that every state-changing root method carries a mark found `Policy.Retire` and `Receipt.Remove` in the Claimwick example. No use case or transition reaches them, and nothing calls them; their states (`Retired`, `Removed`) were declared with no transition. The example shows only declared behavior, so the methods, their state types and the two states were removed rather than given invented use cases.
