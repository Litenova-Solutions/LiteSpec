---
{
  "type": "decision",
  "id": "decision/0005-journeys-serve-a-goal",
  "title": "Journeys Serve a Goal",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "options": [
    {
      "name": "A journey serves one goal record",
      "for": "The journey is the only Intent record that means goal with prose, and both story mapping and impact mapping make the spine a link the book can follow.",
      "against": "Every journey sentence must be mapped by hand to a goal record, because no mechanical mapping exists."
    },
    {
      "name": "Keep the goal a sentence only",
      "for": "A founder approves a journey chapter with no field names and no other page to open.",
      "against": "The book cannot roll steps up to a goal at all, so the top-down edge stays missing by design."
    },
    {
      "name": "Replace the sentence with the link",
      "for": "One field holds the whole goal fact with nothing to keep in sync.",
      "against": "It demotes the sentence the founder approves, and a goal link carries less information than the sentences it would replace."
    },
    {
      "name": "Allow several goals per journey",
      "for": "A walk can serve a staff outcome and a system promise at once.",
      "against": "Both spine methods say one spine, and the singular shape states the rule instead of hiding it in a one-element list."
    }
  ],
  "chosen": "A journey serves one goal record",
  "changeWhen": "A journey kind appears that walks toward no goal record.",
  "basedOn": [
    "research/intent-hierarchy-evidence",
    "research/cross-layer-trace-evidence",
    "research/established-traceability-practice"
  ]
}
---

# Journeys Serve a Goal

## Context

Every other Intent record that means goal links to a goal record, while the journey holds a sentence.

## Decision

The journey sentence field `goal` is renamed `aim`. Every journey names its goal record in a new required single link `serves: goal/<name>`, which is its parent. `finishedWhen` and the steps are unchanged.

## Consequences

`litespec/core/intent#order-each-journey-toward-one-goal` states the sentence beside the link. Use cases no longer serve goals directly; they serve journey steps.

## Evidence with Confidence

`research/intent-hierarchy-evidence` and `research/cross-layer-trace-evidence` are measured over the largest consumer on 2026-10-30, high confidence, including the finding that no goal sentence matches a goal title. `research/established-traceability-practice` carries the stated vendor-restatement limit on the regulated regimes.
