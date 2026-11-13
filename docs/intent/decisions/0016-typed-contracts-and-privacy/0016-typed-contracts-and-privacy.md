# Typed Contracts and Privacy

A contract in LiteSpec is the set of fields that a use case takes as input, or that an integration event sends as its payload. Each field carries a type. Each data class carries a privacy position that says how erasure treats it. This decision sets both. It concerns the people who write use cases, integration events and privacy records, and it matters because a checker can compare records with the generated contract while personal data stays marked as personal.

## Context

Event payloads and use-case inputs carry no type or schema. Trusted payloads still claim that their fields are fit to act on. The position `pseudonymize` sits among the erasure positions, although pseudonymized data stays personal data.

## Options

LiteSpec compares 4 ways to type contracts and state privacy positions.

| Option | Strengths | Drawbacks |
|---|---|---|
| Typed fields with honest privacy positions | Types let the builder compare records with the generated contract, and honest positions keep personal data personal, with a basis and a purpose | Every input, payload, data class and retention row gains types, purposes and ISO 8601 periods in one change |
| Types without the privacy fix | Contracts become checkable while privacy wording waits for legal counsel | Pseudonymized classes keep the wrong position and the wrong count in the meantime |
| Privacy fix without types | Personal data gets an honest position, and contracts stay free | Untyped fields keep the trust claim that no check can see |
| Contracts in code, erasure by pseudonym | The stack already generates and enforces its own contracts | Records restate contracts instead of driving them, and pseudonymized data stays personal data under GDPR Recital 26 |

The chosen option adds types and corrects the privacy positions in one change. Each of the other options fixes only one of these problems.

## Decision

Use-case inputs and integration-event payload fields carry a `type` and a `required` flag. The allowed types are `text`, `integer`, `decimal`, `money`, `boolean`, `date`, `date-time`, `duration`, `email`, `choice` and `id`. An `id` names its record type in `of`. A money value is an object with `amount` and `currency`. An integration event may name a `dataschema`.

`pseudonymize` keeps a data class personal, with its basis. The position `anonymize` is added. Each data class and each retention row states its `purpose`. The field `period` is an ISO 8601 duration. The retention schedule claims only the time limits of a record of processing.

## Consequences

The provisions `litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification` and `litespec/core/edges#describe-every-integration-event-for-its-direction` carry the typed fields. The provisions `litespec/core/privacy#declare-every-data-class-with-its-erasure-position` and `litespec/core/privacy#schedule-every-data-class-for-retention` carry the privacy positions.

## Evidence

`research/lower-layers-practice` holds the outside review and the counts measured over the largest consumer. This decision rests on that research, with high confidence.

## Revisit Conditions

This decision reopens when a contract field appears that no listed type can carry.
