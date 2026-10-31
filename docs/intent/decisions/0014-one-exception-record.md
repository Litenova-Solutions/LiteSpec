---
{
  "type": "decision",
  "id": "decision/0014-one-exception-record",
  "title": "One Exception Record",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "options": [
    {
      "name": "One exception record with approval, scope and expiry",
      "pros": "One shape carries every waiver with its reason, owner, scope and end date, and covers only what it names.",
      "cons": "Deviations, known gaps, deferrals and the frozen phase all migrate to the one record in one change."
    },
    {
      "name": "Keep four mechanisms and tighten each",
      "pros": "Each mechanism keeps its readers and its history.",
      "cons": "Four approval shapes and four expiry rules keep the uneven coverage that caused the finding."
    },
    {
      "name": "Exceptions as project settings only",
      "pros": "The project file holds every waiver in one place.",
      "cons": "Settings carry no owner, no reason and no per-item expiry, so waivers never return for review."
    },
    {
      "name": "Every finding blocks",
      "pros": "No waiver, no waiver abuse.",
      "cons": "One-person teams stall on advisory findings, against the standing direction that approval stays loose."
    }
  ],
  "choice": "One exception record with approval, scope and expiry",
  "reopenedWhen": "A waiver appears that covers neither a provision nor named finding codes.",
  "basedOn": [
    "research/lower-layers-practice"
  ]
}
---

# One Exception Record

## Context

Exceptions are uneven and too wide: a draft known gap nobody approved silences a check level G finding, a deviation silences every finding on its subject instead of its provision, and deferrals with the frozen phase switch checks off with no owner or expiry.

## Decision

One `exception` record replaces deviation, known gap, `deferredProvisions` and `check.phase: frozen`, with `kind` (`departure` or `gap`), `covers` (a provision or finding codes), `subject` or `paths`, `reason`, `owner`, `status` and `expiresOn`. It covers only what it names, applies once approved, where one person may approve, and `exception-expired` reports it after its date.

## Consequences

`litespec/core/model#record-accepted-gaps-as-known-gaps`, `litespec/core/model#record-a-project-wide-lowering-as-a-deviation` and `litespec/core/release#record-accepted-debt-as-a-known-gap` state the mechanisms this decision unifies; the Governance pages replace them with the one exception record.

## Evidence with Confidence

`research/lower-layers-practice` holds the outside review and the measured counts over the largest consumer behind D26, high confidence.
