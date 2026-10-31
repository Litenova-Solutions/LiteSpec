---
{
  "type": "decision",
  "id": "decision/0012-use-cases-read-their-settings",
  "title": "Use Cases Read Their Settings",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "options": [
    {
      "name": "Name readers on the use case and compute the inverse",
      "pros": "The link lives on the record that points, reads as an active verb, and has one written side with the inverse computed.",
      "cons": "The consumer already using readBy moves its settings in one change."
    },
    {
      "name": "Keep readBy on the setting",
      "pros": "No consumer migrates and the adopted name stays.",
      "cons": "The name reads passively against the verb grammar, and the setting restates what the use case already knows."
    },
    {
      "name": "Write both directions",
      "pros": "Each side reads without following the inverse.",
      "cons": "Two written sides drift apart, and neither is the source of truth."
    },
    {
      "name": "Drop the reader link entirely",
      "pros": "No link, no direction dispute.",
      "cons": "Whether a setting is read at all becomes unknowable in the model again."
    }
  ],
  "choice": "Name readers on the use case and compute the inverse",
  "reopenedWhen": "A setting appears whose readers no use case can name.",
  "basedOn": [
    "research/naming-grammar-evidence"
  ],
  "replaces": "decision/0007-adopt-setting-readby"
}
---

# Use Cases Read Their Settings

## Context

`readBy` on the setting reads passively against the verb grammar and restates on the setting what the use case already knows: which settings it reads.

## Decision

A use case names the settings it reads in `reads`. The setting's `readBy` is removed and computed as an inverse, never written.

## Consequences

`litespec/core/model#write-links-on-the-record-that-points` puts the link on the use case, and `litespec/core/language#name-each-field-by-its-word-class` reads `reads` as a link verb. `litespec/core/use-cases#name-the-settings-each-use-case-reads` is restated for `reads`.

## Evidence with Confidence

`research/naming-grammar-evidence` holds the field inventory behind the link-verb reading, read from the schemas and measured over the largest consumer, high confidence.
