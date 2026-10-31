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
