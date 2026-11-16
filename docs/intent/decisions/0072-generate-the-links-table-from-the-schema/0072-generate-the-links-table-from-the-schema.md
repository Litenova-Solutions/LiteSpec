# Generate the Links Table from the Schema

Readers meet every link in the docs table and the book. Every link in a record reads two ways: from its record and back. This decision generates those phrases from the schema, so the table and the labels state what the schema states.

## Context

The Links table was hand-written beside the schema it claimed to state. It held a dozen to twenty missing rows, rows for non-links, and a wrong `erasure` row. The book invented its own connection labels, such as Illustrates, which no record holds.

Verbs had also diverged. `serves` carried three target sets, `source` five meanings, and four fields shared names with other concepts. The sentence Records never point down misdescribes the links it claims to govern.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| Schema annotations generate table and labels | One meaning per link; docs and book cannot drift. | Every link property gains two annotations. |
| Keep the hand-written table | No tooling changes. | Missing and wrong rows reappear with every edit. |
| Derive labels in the book only | The book reads consistently. | The docs table still drifts. |

## Decision

Each link property in the record schema carries its reads-as and read-back phrases as annotations. The maintainer writes the Links table into a generated region of the model page. The book takes every connection label from the same annotations. A link with no phrases fails `validate`.

The direction rule states what the links do. Intent links point up a layer. Effect and structure links follow the effect graph within Behavior. The only down-links are `decides`, `allows`, and `holds`.

One meaning per verb holds, as [decision/0004-features-advance-goals](decision/0004-features-advance-goals) chose for features. Screen `serves` stays explicit, since it names flow steps as a use case's `serves` does.

The shared names split. Notification `addresses` becomes `audience`. Integration-event `addresses` becomes `requires`. Consistency `cites` becomes `boundBy`. Output-group `cites` becomes `widenedBy`. `triggeredBy` reads triggers and is triggered by instead of answers, the flow's word.

`fulfills` leaves read models and entry points, which the completion stages never counted.

## Consequences

The model page holds the generated region. One sentence says the table is generated from the schema annotations.

The book drops every invented label.

The query that reads a read model fulfills the requirement the read model never fulfilled.

## Evidence

[research/field-naming-evidence](research/field-naming-evidence) found the five collisions this decision splits, including `serves` with three target sets. [research/naming-grammar-evidence](research/naming-grammar-evidence) counted the 248-field inventory behind one grammar with no exceptions. The missing rows were counted against the schema with high confidence.

## Revisit Conditions

This decision reopens when a link needs a meaning its two phrases cannot carry. It also reopens when generation blocks a shape the schema must add.
