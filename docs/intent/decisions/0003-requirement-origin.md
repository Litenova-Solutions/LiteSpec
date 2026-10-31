---
{
  "type": "decision",
  "id": "decision/0003-requirement-origin",
  "title": "Requirement Origin",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "options": [
    {
      "name": "Origin in feature, basedOn, or decides",
      "pros": "Established practice treats a requirement without a stated origin as incomplete, and the three sources answer different questions: which offer carries it, which fact grounds it, and which choice settles it.",
      "cons": "Authors must anchor most existing requirements, and list order becomes meaningful because the first entry is the parent."
    },
    {
      "name": "Feature only",
      "pros": "One uniform rule covers every requirement with no precedence to learn.",
      "cons": "It falsifies constraint-born requirements, contradicts the field meaning the Links table states, and the evidence rejects a feature parent specifically."
    },
    {
      "name": "Keep origin optional",
      "pros": "No author writes anything new and no approved record needs re-approval.",
      "cons": "Most requirements stay unanchored, both standards call that orphaned, and the chain from a goal to a test keeps breaking at the top."
    },
    {
      "name": "Group under the requirement page",
      "pros": "Every requirement already sits on a page, so placement needs no new field.",
      "cons": "A page style is not a record type, and file grouping hides the origin gap completely because every requirement looks parented."
    }
  ],
  "choice": "Origin in feature, basedOn, or decides",
  "reopenedWhen": "A requirement kind appears that no offer carries, no fact grounds, and no choice settles.",
  "basedOn": [
    "research/intent-hierarchy-evidence",
    "research/cross-layer-trace-evidence",
    "research/established-traceability-practice"
  ]
}
---

# Requirement Origin

## Context

Most requirements name neither a carrying feature nor a basis, while every enforced Intent check passes.

## Decision

Every requirement names its parent: a carrying `feature` of any lifecycle including shipped, or a constraint or research in `basedOn`, or a decision that names it in `decides`, in that precedence.

## Consequences

`litespec/core/intent#identify-every-requirement` promotes the origin from rationale to Statement. The limit to planned, later, or refused carriers is lifted, so shipped features carry requirements too.

## Evidence with Confidence

`research/intent-hierarchy-evidence` and `research/cross-layer-trace-evidence` are measured over the largest consumer on 2026-10-30, high confidence. `research/established-traceability-practice` carries the stated vendor-restatement limit on the regulated regimes.
