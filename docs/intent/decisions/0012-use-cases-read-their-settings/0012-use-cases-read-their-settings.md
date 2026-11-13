# Use Cases Read Their Settings

## Context

`readBy` on the setting reads passively against the verb grammar and restates on the setting what the use case already knows: which settings it reads.

## Decision

A use case names the settings it reads in `reads`. The setting's `readBy` is removed and computed as an inverse, never written.

## Evidence with Confidence

`research/naming-grammar-evidence` holds the field inventory behind the link-verb reading, read from the schemas and measured over the largest consumer, high confidence.
