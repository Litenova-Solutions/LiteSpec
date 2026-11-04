# Typed Contracts and Privacy

## Context

Event payloads and use-case inputs carry no type or schema while trusted payloads claim the fields are fit to act on, and `pseudonymize` sits as an erasure position although pseudonymized data stays personal data.

## Decision

Use-case inputs and integration-event payload fields carry `type` (`text`, `integer`, `decimal`, `money`, `boolean`, `date`, `date-time`, `duration`, `email`, `choice`, `id` with `of`) and `required`; an integration event may name `dataschema`; money values are `{ amount, currency }`. `pseudonymize` keeps the class personal, with its basis; `anonymize` is added; each data class and retention row states `purpose`; `period` is an ISO 8601 duration; the retention schedule claims only the time limits of a record of processing.

## Evidence with Confidence

`research/lower-layers-practice` holds the outside review and the measured counts over the largest consumer behind D31 and D33, high confidence.
