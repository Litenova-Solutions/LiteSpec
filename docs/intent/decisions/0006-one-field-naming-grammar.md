---
{
  "type": "decision",
  "id": "decision/0006-one-field-naming-grammar",
  "title": "One Field Naming Grammar",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "options": [
    {
      "name": "Rename and state the grammar",
      "for": "The five collisions already produce invalid or misleading data, and a stated grammar lets the validator check the mechanical half on every run.",
      "against": "Every renamed field moves schema, templates, provisions, checks, and consumer records in one change."
    },
    {
      "name": "Define without renaming",
      "for": "No record migrates and no citation sweep is needed.",
      "against": "Definitions fix readers but not search or tooling, and the invalid values keep arriving under the colliding names."
    },
    {
      "name": "Rename fields but skip the reads-as columns",
      "for": "The change stays smaller and the Links table keeps its current shape.",
      "against": "Carriers sharing one verb, such as the three serves rows, stay ambiguous in prose, and book connection labels stay invented."
    },
    {
      "name": "Adopt external names wholesale",
      "for": "One outside standard would settle every naming dispute at once.",
      "against": "No outside standard covers record fields, and external names are kept only where a standard fixes them, such as the published-fact fields."
    }
  ],
  "chosen": "Rename and state the grammar",
  "changeWhen": "A new field appears that the grammar cannot name.",
  "basedOn": [
    "research/field-naming-evidence",
    "research/cross-layer-trace-evidence"
  ]
}
---

# One Field Naming Grammar

## Context

One field name carries two concepts, and one concept carries two names, across the record types.

## Decision

Each colliding field is renamed to one concept, one name, and one shape: feature `state` to `lifecycle`, goal and requirement `by` to `dueBy`, feature `by` to `plannedFor`, decision `decided` to `decidedOn`, constraint `source` to `authority`, and constraint `category` to `kind`. The Links table states how every link reads in both directions, and the book takes its connection labels from those phrases.

## Consequences

`litespec/core/model#name-each-field-for-one-concept` states the grammar with its checked mechanical half. `litespec/core/model#write-links-on-the-record-that-points` carries the reads-as columns, and `litespec/core/model#name-book-labels-with-litespec-terms` takes book labels from them.

## Evidence with Confidence

`research/field-naming-evidence` is read from the schemas and measured over the largest consumer on 2026-10-30, high confidence. `research/cross-layer-trace-evidence` confirms the serves target split the grammar must settle, high confidence.
