# Use Cases Read Their Settings

A setting is a value that the product reads from its configuration. A use case is one action that the product performs. This decision sets where the link between them lives. The link lives on the use case, which names the settings it reads. A reader who opens a use case sees its settings, and a setting does not need to list its readers.

## Context

The setting's `readBy` field reads passively, against the verb grammar. It also restates on the setting what the use case already knows, which is the settings that the use case reads.

## Options

LiteSpec compares 4 places for the reader link.

| Option | Strengths | Drawbacks |
|---|---|---|
| Name readers on the use case and compute the inverse | The link lives on the record that points, reads as an active verb, and has one written side | The consumer project that already uses `readBy` moves its settings in one change |
| Keep `readBy` on the setting | No consumer migrates, and the adopted name stays | The name reads passively against the verb grammar, and the setting restates what the use case knows |
| Write both directions | Each side reads without following the inverse | Two written sides drift apart, and neither is the source of truth |
| Drop the reader link entirely | No link, so no dispute about direction | Whether a setting is read at all becomes unknowable in the model again |

The chosen option keeps one written side and computes the other. Only the use case states the link.

## Decision

A use case names the settings it reads in `reads`. The setting's `readBy` field is removed. The model computes the readers of a setting as the inverse of `reads`, and no author writes that inverse.

## Consequences

The provision `litespec/core/model#write-links-on-the-record-that-points` places the link on the use case. The provision `litespec/core/language#name-each-field-by-its-word-class` reads `reads` as a link verb. The provision `litespec/core/use-cases#name-the-settings-each-use-case-reads` is restated for `reads`.

## Evidence

`research/naming-grammar-evidence` holds the field inventory behind the link-verb reading. It is read from the schemas and measured over the largest consumer, with high confidence.

## Revisit Conditions

This decision reopens when a setting appears whose readers no use case can name.
