---
{
  "type": "decision",
  "id": "decision/0019-loose-approval",
  "title": "Loose Approval",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "options": [
    {
      "name": "Loose approval sized for one-person teams",
      "pros": "Solo projects can approve honestly: an agent reading, a single owner, and an optional security reviewer match how small teams actually work.",
      "cons": "Regulated consumers must add their own separation of duties above the loose default."
    },
    {
      "name": "A human second reviewer on every tier 1 batch",
      "pros": "No tier 1 record ships on self review alone, as separation of duties advises.",
      "cons": "Solo projects cannot staff a second person, and LiteSpec serves one-person teams by standing owner direction."
    },
    {
      "name": "A required security reviewer and a gated queue",
      "pros": "Access and money changes always meet a named reviewer.",
      "cons": "The queue is a list to read, not a gate, and the reviewer stays optional."
    },
    {
      "name": "Keep the single-record path outside the guards",
      "pros": "Quick approvals stay quick.",
      "cons": "Unattributed approvals with no status change are not approvals at all."
    }
  ],
  "choice": "Loose approval sized for one-person teams",
  "reopenedWhen": "A consumer ships a tier 1 record that a second person must countersign.",
  "basedOn": [
    "research/lower-layers-practice"
  ]
}
---

# Loose Approval

## Context

LiteSpec serves one-person teams, but the approval pages demand reviewers and gates solo projects cannot staff, while the single-record path skips every guard and re-approval keeps the old name.

## Decision

Approval stays loose: an agent's reading counts as the tier 1 second reading; a project whose owners list holds one name needs no second reviewer; `review.securityReviewer` is optional and the security queue is a list to read, not a gate; the pages say so. Two defects are fixed: single-record approval promotes the record's status, and re-approval writes the new approver and date.

## Consequences

`litespec/core/release#refuse-self-review-alone-for-tier-1-behavior` states the solo rule, `litespec/core/release#name-the-security-reviewer-in-the-project-file` keeps the reviewer optional, `litespec/core/release#review-the-security-queue-separately-from-reviewed` keeps the queue a list, and `litespec/core/release#advise-without-gating-as-an-agent` counts the agent reading.

## Evidence with Confidence

`research/lower-layers-practice` holds the outside review and the measured counts over the largest consumer behind D25, high confidence.
