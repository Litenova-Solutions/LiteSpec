# One Field Naming Grammar

Authors who write records and tools that check them need every field name to mean one concept. A name that means two things produces wrong data, and a checker cannot tell which meaning an author intended. This decision renames the colliding fields and states the rule that every field name follows.

## Context

One field name carries two concepts, and one concept carries two names, across the record types. The 5 collisions already produce invalid or misleading data.

## Options

LiteSpec compares 4 ways to handle the collisions.

| Option | Strengths | Drawbacks |
|---|---|---|
| Rename and state the grammar | The 5 collisions already produce invalid or misleading data, and the validator checks the mechanical half on every run | Every renamed field changes the schema, templates, provisions, checks, and consumer records in one change |
| Define without renaming | No record migrates, and no citation sweep is needed | Definitions guide readers but not search or tools; invalid values keep the colliding names |
| Rename fields but skip the reads-as columns | A smaller change, and the Links table keeps its shape | The 3 `serves` rows stay ambiguous, and book connection labels stay invented |
| Adopt external names wholesale | One outside standard would settle every naming dispute | No outside standard covers record fields; external names stay only where a standard fixes them, such as the published-fact fields |

The chosen option renames the fields and states the grammar. Each of the other options leaves one of these problems in place.

## Decision

This decision renames each colliding field to one concept, one name, and one shape. Constraint `source` becomes `authority`. Constraint `category` becomes `kind`.

The Links table states how each link reads in both directions. The book takes its connection labels from those phrases.

## Consequences

The provision `litespec/core/language#name-each-field-by-its-word-class` states the grammar and its checked mechanical half. The provision `litespec/core/model#write-links-on-the-record-that-points` carries the reads-as columns. The provision `litespec/core/model#name-book-labels-with-litespec-terms` takes book labels from those columns.

## Evidence

`research/field-naming-evidence` comes from the schemas and from measurements over the largest consumer on 2026-10-30, with high confidence. `research/cross-layer-trace-evidence` confirms the split of `serves` targets that the grammar settles, also with high confidence.

## Revisit Conditions

This decision reopens when a new field appears that the grammar cannot name.
