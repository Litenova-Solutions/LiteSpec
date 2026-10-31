---
{
  "type": "decision",
  "id": "decision/0001-keep-release-reasoning-as-records",
  "title": "Keep Release Reasoning as Records",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "options": [
    {
      "name": "Consumer homes under docs/intent",
      "pros": "Comparable projects keep rationale in numbered records beside the normative text while the specification stays the only normative surface, and the consumer research and decision shapes already exist with an exercised example.",
      "cons": "The maintainer validator and the site each need a record pass before the records publish and check."
    },
    {
      "name": "A root records folder",
      "pros": "Reasoning stays visibly separate from the documented contract.",
      "cons": "No site ingestion and no authoring precedent cover a root folder, and the maintainer validator would still walk its pages as stray Markdown."
    },
    {
      "name": "Stay in .agents",
      "pros": "No contract, validator, or site change is needed.",
      "cons": "Working folders are ignored by git, so the reasoning is neither tracked nor referenced, and no later change can cite it."
    },
    {
      "name": "docs/reference",
      "pros": "The reasoning sits beside the other exact material readers consult.",
      "cons": "The reference style states exact values and owns no records, so record pages would break the area contract."
    }
  ],
  "choice": "Consumer homes under docs/intent",
  "reopenedWhen": "LiteSpec stops publishing provisions as its specification.",
  "basedOn": [
    "research/comparable-rationale-practice",
    "research/established-traceability-practice"
  ]
}
---

# Keep Release Reasoning as Records

## Context

Provisions state obligations, but they hold no question, no dated sources, and no conclusion a later change can cite.

## Decision

LiteSpec keeps its own research and decisions as consumer-shaped records under `docs/intent/research/` and `docs/intent/decisions/`, checked by `litespec-maintain validate` and shown by the site as a non-applied area.

## Consequences

`litespec/core/maintaining#record-the-reasoning-behind-each-release` states the obligation, and its Checked by line names the five `RECORD_*` codes. Research and decisions inform and explain; the provision remains the only obligation.

## Evidence with Confidence

`research/comparable-rationale-practice` is read in full from five project templates, high confidence. `research/established-traceability-practice` rests on vendor restatements for the regulated regimes and a second-hand glossary quotation, medium confidence on those claims.
