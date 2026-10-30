---
{
  "type": "research",
  "id": "research/check-coverage-evidence",
  "title": "Check Coverage Evidence",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-10-30",
  "question": "Which links and parents does the checker enforce, which does it only report, and which does no check read at all?",
  "conclusion": "The checker is dense below a requirement and almost absent above it: no check reads a feature's goal, a requirement's origin, a rule's fulfillment, or a journey's goal, and those four unchecked links hold the largest measured gaps in the largest consumer.",
  "sources": [
    {
      "path": "docs/tools/model.md",
      "read": "2026-10-30"
    },
    {
      "path": "apps/cli/src/LiteSpec.Cli/Model/Checker.cs",
      "read": "2026-10-30"
    },
    {
      "path": "apps/model/src/LiteSpec.Model.Core/Finding.cs",
      "read": "2026-10-30"
    },
    {
      "path": "apps/model/src/LiteSpec.Model.Core/Suppression.cs",
      "read": "2026-10-30"
    }
  ]
}
---

# Check Coverage Evidence

## Answer

This record distils the check coverage investigation of 2026-10-30 into the claims this release relies on. Counts below were read from the `findings` and `known` arrays of the largest consumer's built model at `D:/Personal-Projects/Nighthall/.model/model.json` on 2026-10-30 (2,711 open, 328 known), and gap counts were measured over the same file. A code counts here when its predicate is the existence, resolution, target type, or reachability of a written link, containment by identifier or folder, the reverse orphan direction of such a link, or a computed trace.

Below a requirement the checker is dense. Resolution is fully covered: dangling record citations, body Markdown links, link target types, identifier homes, and step and operation fragments each have a tier G code, and all read clean. Requirement and journey checks cover fulfillment (105 known, none open), measurement, reach, state reachability, and transitions. Containment checks cover success results, scenario results, calculation tables, and reaction triggers, all clean. Orphan checks cover the five Behavior types with neither parent, unserved steps, screened steps (91 known), screen serve mismatches, and effect loops, all clean at open. Data and governance checks cover consistency rules, widening justification (6 open), erasure, retention, risk acceptance, setting defaults, reservation releases, notification senders, exception fallbacks, transcribed review, gap registry names, and quality alerts. The code, screen, store, and operations traces are equally dense: marks and their resolution, followed decisions (8 open unfollowed, 15 advisory without basis), unimplemented aggregates (21 open), reactions, jobs, and external systems, unexposed use cases (60 open), unimplemented use cases (8 open), declared effects taken by no dispatch (412 advisory), undeclared writes and effects and events, uncalled aggregate methods (165 open), screen routes and calls, unreached use cases (182 advisory), unshown refusals (911 advisory), unconfirmed and duplicated proofs (90 with 50 known, 237 advisory), unthrown refusals, contract mismatches, alerts, settings, templates, and dependencies.

Silence in the model has four causes, and only the first is good news. Satisfied checks hold genuinely complete data: no in-scope record lacks both parents, no step is unserved, every reaction trigger resolves, and sender, fallback, reservation, and serve checks agree with independent counts. Moved-to-known checks emit and are then covered by named gaps, blocking nothing only while the gaps stay unexpired: unfulfilled requirements, unscreened steps, unmeasured requirements, and the undeclared and unconfirmed families. Gated or scoped checks never run their predicate: journey reach exempts background-only use cases, two checks run only at large size, lifecycle skips transition-less stores, decision codes exempt by folder in opposite directions, `record-orphaned` covers only five Behavior types, screen checks skip routeless screens, dependency and template checks return silently when unconfigured (so their clean tallies cannot distinguish satisfied from skipped), and the transcribed check waits on an adoption-ending approval while 2,882 transcribed records stand. Checks not in the checker at all cannot appear in model output: `partial-missing` is emitted only by the analyzer, so a known gap naming it would itself draw `gap-unknown-check`. One working check is weaker than its standard: `state-unreachable` passes a state that is a transition source or target, while the standard means reached by no transition, hiding 9 states with no incoming transition.

Above a requirement almost nothing is checked, and the unchecked remainder concentrates at Intent altitude. No check reads a feature's `serves` (28 of 40 non-refused features serve no goal), a requirement's origin (719 of 925 live requirements name neither `feature` nor `basedOn`), a rule's `fulfills` (2,052 of 2,295 direct-empty, 79 empty even counting transitive fulfillment through governed use cases and aggregates), or a journey's goal (112 of 112 prose sentences). Further unchecked links, each with its measured size: journey `fulfills` (a checker gap, not a data gap: 112 of 112 present); the reverse orphan of `solves` (1 unsolved problem); product decision `basedOn` (2 of 3); bare-code rule refusals matching no result code (206 of 1,631); the reverse orphan of `raises` (1 unraised event); the permission middle between role `allows` and use-case grants (1 allowed but never granted); the reverse of `scenario.result` over written results (2,330: 62 success, 2,268 refusal); outbound operations never called (9 of 49); outbound integration events translating nothing (8) and inbound events consumed by nothing (2); notification claims without a requirement (33 entries on 28 notifications); jobs without a runbook (15 of 73); deviations without a scope (5 of 22, inert in suppression); the reverse orphan of `basedOn` (4 constraints, 15 of 29 research records); and process exits (1 of 6 aggregates). Containment by identifier is otherwise complete, and the one empty parent, `module/hosting`, follows a recorded deviation rather than drift. Three populations that look orphaned need no check: aggregate-less orchestration use cases, store aggregates read only by code, and uncited glossary terms, whose home is the glossary itself.

The investigation proposes new finding codes in the existing naming stock, each starting advisory: `feature-without-goal` (28), `requirement-without-basis` (719), `rule-fulfills-nothing` (2,052 direct, 79 transitive-empty), `record-orphaned` extended to journeys (0 today), `journey-goal-unlinked` (after the field change, 112), `problem-unsolved` (1), `result-without-scenario` (2,330, with success results the defensible first slice at 62), `rule-refusal-unresolved` (206), `event-unraised` (1), `permission-orphaned` (1), `operation-uncalled` (9), `integration-event-untranslated` (8, with a none-with-reason exemption defined first), `integration-event-unconsumed` (2), `claim-unlinked` (33 entries), `job-without-runbook` (15), `deviation-without-scope` (5), `decision-without-basis` extended to product decisions (2 more), and `state-unreachable` amended to incoming-only with advice first (9). Two sharpenings follow for the owner's recommendation: the cheap measurable form of requirement origin is the union of `feature` and `basedOn`, because `feature` alone is deliberately narrow, and the journey goal link is a LiteSpec field change before it is a check, because today there is nothing for a checker to read.
