# Check Coverage Evidence

LiteSpec's checker reads a built model and reports findings. Each finding names a check that a record broke. This research record counts which links and parents the checker enforces, which it only reports, and which no check reads at all. It uses the largest consumer, a project that uses LiteSpec, as the source of the counts. It matters because a clean model report can mean that a rule is satisfied, waived or never run, and the report alone does not show which.

## Findings

The checker is dense below a requirement and almost absent above it. No check reads a feature's goal, a requirement's origin, a rule's `fulfills` link or a journey's goal. Those 4 unchecked links hold the largest measured gaps in the largest consumer.

On 30 October 2026, the consumer's built model held 2,711 open findings and 328 known findings. This record counts a check when its predicate tests one of these: the existence, resolution, target type or reachability of a written link; containment by identifier or folder; the reverse orphan direction of such a link; or a computed trace. The record gives no confidence for this claim.

Below a requirement, resolution is fully covered. Dangling record citations, body Markdown links, link target types, identifier homes, and step and operation fragments each have a tier G code, and all of them read clean. Requirement and journey checks cover fulfillment (105 known, none open), measurement, reach, state reachability and transitions. The record gives no confidence for this claim.

Containment checks cover success results, scenario results, calculation tables and reaction triggers, and all read clean. Orphan checks cover the 5 Behavior types that have neither parent, unserved steps, screened steps (91 known), screen serve mismatches and effect loops. Data and governance checks cover consistency rules, widening justification (6 open), erasure, retention, setting defaults, reservation releases, notification senders, exception fallbacks and quality alerts. The record gives no confidence for this claim.

The code, screen, store and operations traces are also dense. They cover marks and their resolution, followed decisions (8 open and unfollowed, 15 advisory without a basis), and unimplemented aggregates (21 open). They cover reactions, jobs and external systems, unexposed use cases (60 open) and unimplemented use cases (8 open). They also cover declared effects that no dispatch takes (412 advisory), uncalled aggregate methods (165 open) and unreached use cases (182 advisory). Advisory findings are reported and not blocking. The record gives no confidence for this claim.

The same traces cover unshown refusals (911 advisory), unconfirmed and duplicated proofs (90, with 50 known and 237 advisory), unthrown refusals, contract mismatches, alerts, settings, templates and dependencies. The record gives no confidence for this claim.

A clean result can have 4 causes, and only the first is good news. Satisfied checks hold complete data. No in-scope record lacks both parents, no step is unserved, and every reaction trigger resolves. The sender, fallback, reservation and serve checks agree with independent counts. The record gives no confidence for this claim.

Covered checks emit findings, and named waivers hold those findings back. A waiver blocks nothing only while it stays unexpired. The covered checks are the unfulfilled requirements, the unscreened steps, the unmeasured requirements, and the undeclared and unconfirmed families. The record gives no confidence for this claim.

Gated or scoped checks never run their predicate. Journey reach exempts background-only use cases. Of the checks, 2 run only at large size. The transition check skips stores that have no transitions. Decision codes are exempt by folder in opposite directions. `record-link-missing` covers only 5 Behavior types, and screen checks skip screens without a route. Dependency and template checks return silently when they are not configured, so their clean tallies cannot tell satisfied from skipped. The record gives no confidence for this claim.

Some checks are missing from the checker. `partial-missing` is emitted only by the analyzer, so a waiver that names it would itself draw `gap-finding-unknown`. One working check is also weaker than its standard. `state-unreached` passes a state that is a transition source or target. The standard means a state reached by no transition, so the check hides 9 states that have no incoming transition. The record gives no confidence for these claims.

Above a requirement, almost nothing is checked. No check reads a feature's goal link, and 28 of 40 features name no goal. No check reads a requirement's origin, and 719 of 925 live requirements name neither `feature` nor `basedOn`. No check reads a rule's `fulfills` link. Of 2,295 rules, 2,052 are empty on direct links, and 79 stay empty even when transitive fulfillment through governed use cases and aggregates counts. No check reads a journey's goal, and all 112 goals are prose sentences. The record gives no confidence for this claim.

Other unchecked links each have a measured size. Journey `fulfills` is a checker gap, because all 112 journeys carry it. The reverse orphan of `solves` covers 1 unsolved problem. Product decision `basedOn` has 2 of 3 decisions without it. Bare-code rule refusals match no result code in 206 of 1,631 cases. One event is unraised, and one permission is allowed but never granted. The record gives no confidence for these counts.

More unchecked links are also measured. The reverse of `scenario.result` covers 2,330 written results, of which 62 are success results and 2,268 are refusals. Of 49 outbound operations, 9 are never called. Of the outbound integration events, 8 translate nothing, and 2 inbound events are consumed by nothing. Of the notification claims, 33 sit on 28 notifications without a requirement. Of 73 jobs, 15 have no runbook. The reverse orphan of `basedOn` covers 4 constraints and 15 of 29 research records. The record gives no confidence for these counts.

Containment by identifier is otherwise complete. The one empty parent, `module/hosting`, follows a recorded exception rather than drift. There are 3 populations that look orphaned and need no check. They are orchestration use cases with no aggregate, store aggregates that only code reads, and uncited glossary terms, whose home is the glossary itself. The record gives no confidence for this claim.

The investigation proposes new finding codes in the existing naming pattern. Each code starts advisory. The codes are `feature-goal-missing` (28), `requirement-without-basis` (719), `rule-fulfills-nothing` (2,052 direct and 79 transitive), `journey-goal-unlinked` (112, after the field change) and `problem-unsolved` (1). They also include `result-scenario-missing` (2,330, with the 62 success results as the first slice), `rule-refusal-unresolved` (206) and `event-unraised` (1). The record states no confidence for these proposals.

The remaining proposed codes are `permission-orphaned` (1), `operation-uncalled` (9), `integration-event-untranslated` (8), `integration-event-unconsumed` (2), `claim-unlinked` (33 entries) and `job-without-runbook` (15). The record also proposes that `record-link-missing` extend to journeys, which covers none today. It proposes that `decision-basis-missing` extend to product decisions (2 more), and that `state-unreached` become incoming-only, with advice first (9). The record states no confidence for these proposals.

The recommendation has 2 sharpenings. The cheap measurable form of requirement origin is the union of `feature` and `basedOn`, because `feature` alone is deliberately narrow. The journey goal link is a LiteSpec field change before it is a check, because no field exists today for a checker to read. The record states no confidence for these sharpenings.

## Source Notes

The record names 4 sources, all read on 30 October 2026. They are the tools model page (`docs/tools/model.md`), the checker code (`apps/cli/src/LiteSpec.Cli/Model/Checker.cs`), the finding code (`apps/model/src/LiteSpec.Model/Findings/Finding.cs`) and the suppression code (`apps/model/src/LiteSpec.Model/Findings/Suppression.cs`). The record does not say which claim each source supports.

The record names no disagreement between its sources. It also names no claim that it could not confirm. Its counts come from one consumer's built model on one day.
