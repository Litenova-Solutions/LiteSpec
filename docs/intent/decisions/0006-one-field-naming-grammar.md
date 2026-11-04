# One Field Naming Grammar

## Context

One field name carries two concepts, and one concept carries two names, across the record types.

## Decision

Each colliding field is renamed to one concept, one name, and one shape: constraint `source` to `authority`, and constraint `category` to `kind`. The Links table states how every link reads in both directions, and the book takes its connection labels from those phrases.

## Evidence with Confidence

`research/field-naming-evidence` is read from the schemas and measured over the largest consumer on 2026-10-30, high confidence. `research/cross-layer-trace-evidence` confirms the serves target split the grammar must settle, high confidence.
