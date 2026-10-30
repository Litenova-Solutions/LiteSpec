---
{
  "type": "decision",
  "id": "decision/0004-features-advance-goals",
  "title": "Features Advance Goals",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "options": [
    {
      "name": "Advances for goals, solves for problems",
      "for": "One verb per meaning ends the three-target split of serves, every live feature already solves exactly one problem, and the goal link is the thin edge the trace needs.",
      "against": "Features, provisions, checks, and the book must all learn the new verb in one change."
    },
    {
      "name": "Keep serves for features",
      "for": "No field renames and no citation sweep are needed.",
      "against": "Serves keeps three target sets and two meanings, and invalid values already exist where authors must guess which targets a carrier allows."
    },
    {
      "name": "Goals optional for features",
      "for": "With few goals, forcing one goal per feature risks vague pasted claims that carry less information than the Offering.",
      "against": "The reader starting at most planned features reaches no goal, and only a handful of live requirements trace to any goal at all."
    },
    {
      "name": "Refused features carry full links",
      "for": "Every feature follows one uniform link rule with no exemption to state.",
      "against": "Requiring links on a refusal inverts its meaning; a refusal records why in refusedBecause instead."
    }
  ],
  "chosen": "Advances for goals, solves for problems",
  "changeWhen": "A feature kind appears that moves no goal and declines no problem.",
  "basedOn": [
    "research/intent-hierarchy-evidence",
    "research/cross-layer-trace-evidence",
    "research/established-traceability-practice"
  ]
}
---

# Features Advance Goals

## Context

A feature contributes to a goal and carries a walker through nothing, while every other serves carrier does the reverse.

## Decision

A feature's link to a goal is renamed from `serves` to `advances`. Every feature names at least one problem in `solves`, and its parent is the first. Every non-refused feature names at least one goal in `advances`. A refused feature names `refusedBecause` and the problem it declines in `solves`, and carries no `advances`.

## Consequences

`litespec/core/intent#state-each-feature-with-its-lifecycle` states the renamed fields and the refused exemption. Serves keeps one meaning per carrier: a use case, reaction, job, or screen serves journey steps, and a journey serves one goal.

## Evidence with Confidence

`research/intent-hierarchy-evidence` and `research/cross-layer-trace-evidence` are measured over the largest consumer on 2026-10-30, high confidence. `research/established-traceability-practice` carries the stated vendor-restatement limit on the regulated regimes.
