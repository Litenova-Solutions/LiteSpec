---
{
  "type": "decision",
  "id": "decision/0016-typed-contracts-and-privacy",
  "title": "Typed Contracts and Privacy",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "options": [
    {
      "name": "Typed fields with honest privacy positions",
      "pros": "Types let the builder compare records with the generated contract, and honest positions keep personal data personal with a basis and a purpose.",
      "cons": "Every input, payload, data class and retention row gains types, purposes and ISO periods in one change."
    },
    {
      "name": "Types without the privacy fix",
      "pros": "Contracts become checkable while privacy wording waits for counsel.",
      "cons": "Pseudonymized classes keep the wrong position and the wrong count meanwhile."
    },
    {
      "name": "Privacy fix without types",
      "pros": "Personal data is honestly positioned while contracts stay free.",
      "cons": "Untyped fields keep the trust claim no check can see."
    },
    {
      "name": "Contracts in code, erasure by pseudonym",
      "pros": "The stack already generates and enforces its own contracts.",
      "cons": "Records then restate contracts instead of driving them, and pseudonymized data stays personal data under GDPR Recital 26."
    }
  ],
  "choice": "Typed fields with honest privacy positions",
  "reopenedWhen": "A contract field appears that no listed type can carry.",
  "basedOn": [
    "research/lower-layers-practice"
  ]
}
---

# Typed Contracts and Privacy

## Context

Event payloads and use-case inputs carry no type or schema while trusted payloads claim the fields are fit to act on, and `pseudonymize` sits as an erasure position although pseudonymized data stays personal data.

## Decision

Use-case inputs and integration-event payload fields carry `type` (`text`, `integer`, `decimal`, `money`, `boolean`, `date`, `date-time`, `duration`, `email`, `choice`, `id` with `of`) and `required`; an integration event may name `dataschema`; money values are `{ amount, currency }`. `pseudonymize` keeps the class personal, with its basis, and counts in `risk-acceptance-missing`; `anonymize` is added; each data class and retention row states `purpose`; `period` is an ISO 8601 duration; the retention schedule claims only the time limits of a record of processing.

## Consequences

`litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification` and `litespec/core/edges#describe-every-integration-event-for-its-direction` carry the typed fields, and `litespec/core/privacy#declare-every-data-class-with-its-erasure-position` with `litespec/core/privacy#schedule-every-data-class-for-retention` carry the privacy positions.

## Evidence with Confidence

`research/lower-layers-practice` holds the outside review and the measured counts over the largest consumer behind D31 and D33, high confidence.
