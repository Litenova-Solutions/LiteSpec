---
{
  "type": "research",
  "id": "research/cross-layer-trace-evidence",
  "title": "Cross-Layer Trace Evidence",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "question": "Where does the trace from a goal to a test break in one real consumer, read top down and bottom up?",
  "conclusion": "The chain breaks at the top: only 19 of 925 live requirements trace to a goal and only 18 of 1,251 handlers sit under a use case that does, while the Behavior-to-code-to-test links below are dense and mostly checked.",
  "sources": [
    {
      "path": "docs/core/model.md",
      "read": "2026-10-30"
    },
    {
      "path": "apps/model/src/LiteSpec.Model.Core/RecordTypes.cs",
      "read": "2026-10-30"
    },
    {
      "path": "docs/tools/model.md",
      "read": "2026-10-30"
    }
  ]
}
---

# Cross-Layer Trace Evidence

## Answer

This record distils the cross-layer trace investigation of 2026-10-30 into the claims this release relies on. Counts below were measured over the largest consumer's built model at `D:/Personal-Projects/Nighthall/.model/model.json` on 2026-10-30, holding 21,548 records, of which 20,943 read draft.

Read top down, the chain breaks at the top, not at the bottom. Goal to feature: 12 of 111 features name a goal in `serves`, and all 8 goals are served, but 28 of 40 non-refused features serve no goal. Feature to problem: 40 of 40 non-refused features name `solves`, the one strong Intent hop, already required and checked. Feature to requirement: 23 of 111 features are named by any requirement, and 872 of 928 requirements name no feature, by design. Requirement to Behavior: 763 of 925 live requirements are fulfilled in the checker's sense, 53 are exempt as undelivered, 10 quality requirements are served by a technical decision, and 105 are unfulfilled, all covered by a known gap, so a reader of open findings alone sees no break. Journey to requirement: 112 of 112 journeys carry `fulfills` over 571 distinct requirements, complete but unchecked. Use case to result: 1,272 of 1,272 have a success result. Result to scenario: 3,083 of 5,413 written results have a scenario; 2,330, mostly refusals, have none. Scenario to test: 5,426 of 6,517 scenarios have a confirmed `proves` link, but every link has a null passing state, the model's test run is null, and zero of 1,272 use cases read verified. Use case to code: 1,266 of 1,272 use cases are implemented. Screen to use case: 380 of 380 screens have `offers` equal to computed `calls`.

End to end, only 19 of 925 live requirements trace to a goal through `feature` and then `feature.serves`; per goal the counts are 11, 9, 5, 1, 1, 0, 0, and 0. Of those traced requirements, only 3 reach an implemented use case with a confirmed scenario proof. Only 18 of 1,272 use cases trace to a goal, so only 18 of 1,251 handlers reach one. Every other handler reaches a requirement or a journey step and stops, because the journey above the step names its goal only as a sentence. From a route the climb stops at the same wall: screens, steps, journeys, and requirements all connect, but the journey cannot name its goal record, so a route answers which promise and never which outcome.

The requirement middle is the weak hop. Of 928 requirements, 56 name a `feature` (9 planned, 14 refused targets, confirming the narrow reading), 164 name `basedOn` (139 constraint and 48 research citations with overlap), 209 name either, and 719 name neither. The genuinely broken set is smaller and sharper: 109 live requirements name neither anchor and are also unfulfilled, answering neither why they exist nor what keeps them. The journey hop above them is undefined, not merely empty: a goal record has no outgoing link field at all, `journey.goal` is a sentence in 112 of 112 cases, and there is no journey to goal hop in LiteSpec. The rest of the journey chain is strong: all 1,077 steps carry identity and actor, all are served, and screens serve 817 of them, with 91 known `step-unscreened` findings and none open.

Three inconsistencies were found where specifications disagree. Eleven use cases serve a whole journey instead of a step; the link-type table allows `serves` to name a journey while the schema's `servesArray` allows only a step or a goal, so these records pass one definition and break the other. Written result identifiers drop the module while derived ones keep it, so a reader must learn two containment shapes. Route links name screens as frontend and route text rather than `screen/` identifiers, so joining a call to its screen record needs the computed `screens` array.

Governance divides sharply by home. Three product decisions all carry `decides`; 71 technical decisions govern through `follows` and `basedOn`, of which 56 name a basis, 15 draw advisory `decision-without-basis`, and 8 draw `decision-unfollowed`. No decision in 74 carries `replaces`. The `follows` hop has a tooling break: the model writer exports only `implements`, `calls`, and `proves`, so code to decision traceability is defined and checked but unrecoverable from the built model or the book. Constraints and research are reached unevenly through `basedOn` (24 of 29, 14 of 29), while 25 of 29 constraints cite research as free text in `source`, which no link table validates. Five of 22 deviations name neither `subject` nor `code` and govern nothing a tool can name, and a deviation's subject grammar cannot cover one journey step the way a known gap can.

The investigation argues against mandatory `requirement.feature` as written, because it would invert the field's stated narrow meaning and duplicate the requirement page's own rationale; it supports an anchor set instead, requiring each live requirement to be fulfilled, undelivered through its feature, served by a technical decision when quality, or covered by an unexpired known gap, plus one advisory check for a requirement with no `feature`, no `basedOn`, and no deciding decision. It agrees that every non-refused feature should name its goal beside the existing `solves` rule, agrees the journey needs an identifier-typed goal link, and agrees checks should start advisory with one warning: 105 fulfilled-check results already sit parked in known gaps, invisible in open findings, so a new advisory check needs a named promotion test or it will graduate into the known list instead of into compliance.
