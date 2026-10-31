# Decisions

## 2026-10-30: Take the Optional Partial-Batch Change (F4)

The NightHall approval left the `admin/screen` and `control-panel/screen` batches held as a whole because a few of their screens serve steps of `journey/choose-a-plan`, which rests on an open pricing question; every other screen in those batches passed its guards. The approval manifests already carry a `held` action per record, so holding only the blocked records fits the data the tool writes. A module-type batch now approves what passes and holds the rest by name; use-case, decision and tier 1 batches stay all-or-nothing because their records are reviewed as one unit.

## 2026-10-30: One NightHall Slice per Module

F1's page fixes and F2's code fixes both edit `docs/behavior/modules/<module>/`, so cutting them by finding category would put two writers on one page. Each module is one slice carrying both, with its code folders (`apps/api/src/*/<Module>/`), and the cross-module F1 work (intent pages, the UI pattern catalog, integration events, external systems, runbooks, settings, screens) gets slices of its own.

## 2026-10-30: Terms Carry the Common Fields, and Labels Any Language

NightHall's consumer check reported 2,032 failures on `docs/glossary.json`: all 508 terms carry `approvedBy`, `approvedOn` and `approvedHash` (stamped 2026-10-23), and each carries a French label. The published 3.0.0 tool never read the glossary ("no glossary found"), so the failures surfaced only now. A term is a record, and `litespec/core/model#state-the-common-fields-on-every-record` gives every record its owner, status, decider and approval fields, so the term schema now carries them; `labels` takes any two-letter language code instead of only `nl` and `de`, because the interface languages are the project's to choose. `docs/core/behavior.md` states both.

## 2026-10-30: A Held Result Takes No Decider

Holding records after approval wrote `decider` and `decideBy` onto result entries, which the result schema does not allow (a result is decided with its use case), and nine NightHall pages failed the schema. The entries are fixed in NightHall, and `Approver.KeepInheritedStatus` no longer copies those two fields onto a result.

## 2026-10-30: The Partial-Batch Rule Is Its Own Provision

The partial-batch change first extended the Statement of `litespec/core/release#approve-drafts-in-bounded-batches` with a second sentence and three modals, which the page contract refuses (one sentence, one obligation). The hold rule is now `litespec/core/release#hold-only-the-blocked-records-of-a-type-batch`, and the partial-batch tests prove it.

## 2026-10-30: The Validator Skips Nested Checkouts

A locked Claude worktree (`.claude/worktrees/layer-presentations`, branch `worktree-layer-presentations`, made by another session at 20:54) sits inside the LiteSpec folder, and `litespec-maintain validate` walked its pages and reported 1,311 errors that are not this repository's. The page walk and the source walk now skip any folder holding its own `.git`. The worktree itself belongs to that session and was left alone.

## 2026-10-30: The Refused Reading of `solves` Lives in the Row's Description

The conformance fix put "(refused: declines)" inside the `solves` row's phrase cells, which the book reads as one phrase each (contracts Section 1), and the book test caught it. The phrase cells read `solves` and `is solved by`; the row's Points at text states that a refused feature names the problem it declines. Rendering "declines" for a refused feature would need a second phrase pair keyed by lifecycle, which the Links table does not model; that is recorded as a follow-up, not invented in the client.

## 2026-10-31: Two Code Checks Follow the Aggregate Kinds

Eight NightHall slices asked the same two questions independently. Should a `kind: store` aggregate need an aggregate root? Should a command that only issues other commands call an act? LiteSpec defines a store as state no aggregate invariant guards, with a named owner, so it has no root; and an act is the method that changes a guarded aggregate. `aggregate-unimplemented` now skips store aggregates, and `use-case-act-uncalled` applies only to a command whose use case changes an entity or process aggregate. Both follow the definitions LiteSpec already states; neither weakens a check on an aggregate that has a root.

## 2026-10-31: A Repository Serves the Aggregate Its Implementation Marks

After the NightHall requests wave, `write-undeclared` blocked one approved use case, `use-case/events.suspend-event-sales`. Its handler takes `IEventReviewRepository`, and its record changes `aggregate/platform.event-moderation`. The check derived the aggregate from the interface name (`event-review`), while the repository class `EventReviewRepository` carries `implements aggregate/platform.event-moderation`, as the Marks rule asks of a repository (`docs/core/model.md`, line 300). Renaming the aggregate would also rename the persisted event classifications `event/event-moderation.*`, a data change outside this run. The check now reads the implementing class's `implements` mark first and falls back to the interface name only where no implementation names an aggregate. The dotnet stack page states it, and `CodeCheckTests.Repository_Serves_The_Aggregate_Its_Implementation_Marks` proves it. The naming drift between the record and the code stays a NightHall follow-up for its owner.
