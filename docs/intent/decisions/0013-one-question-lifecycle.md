---
{
  "type": "decision",
  "id": "decision/0013-one-question-lifecycle",
  "title": "One Question Lifecycle",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "options": [
    {
      "name": "One lifecycle on the record; questions alone auto-approve",
      "pros": "One shape answers every question the same way, the tier follows the record's risk instead of a self-declared field, and silence can settle a question but never a record.",
      "cons": "Every question entry moves to the new fields, and every page citing the old lifecycle is restated, in one change."
    },
    {
      "name": "Keep the question tier",
      "pros": "No question entry migrates.",
      "cons": "A self-declared tier lets a low tier clear the open-question guard on a tier 1 record, and money figures auto-approve."
    },
    {
      "name": "Auto-approve whole records by silence",
      "pros": "Stale records clear themselves without a reader.",
      "cons": "Approval without reading is not approval, and the hash then seals what nobody reviewed."
    },
    {
      "name": "Refuse every auto-approval",
      "pros": "Every answer carries a named decider.",
      "cons": "Tier 3 queues stall on trivia, which is what the veto window exists to keep moving."
    }
  ],
  "choice": "One lifecycle on the record; questions alone auto-approve",
  "reopenedWhen": "A question appears whose answer needs a shape these fields cannot record.",
  "basedOn": [
    "research/naming-grammar-evidence",
    "research/lower-layers-practice"
  ]
}
---

# One Question Lifecycle

## Context

Questions carry a self-declared tier that can clear the open-question guard on a tier 1 record, answers have no recorded shape, and the auto-approval wording lets silence approve a whole record.

## Decision

A question carries `question`, `decider`, `decideBy`, `ballot`, `recommendation`, and once answered `answer`, `answeredBy`, `answeredOn`. `ballot` is required, `recommendation` is required when the record carries no `risk`, the question's `tier` is removed and derived from the record's `risk`, and an unanswered question is open. Only a question auto-approves after its `decideBy` plus the veto window, recorded as `answeredBy: auto (veto window)` on the question; no record carries `approvedBy: auto (veto window)`.

## Consequences

`litespec/core/model#answer-each-question-on-its-record` is the one home of the lifecycle, and `litespec/core/intent#group-questions-into-ballots` keeps the ballot rule and cites it.

## Evidence with Confidence

`research/naming-grammar-evidence` holds the question-field inventory, and `research/lower-layers-practice` holds the approval review behind the tier derivation, both read from the repository and measured over the largest consumer, high confidence.
