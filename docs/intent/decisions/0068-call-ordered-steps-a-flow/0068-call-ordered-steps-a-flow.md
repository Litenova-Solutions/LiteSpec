# Call Ordered Steps a Flow

Authors and readers meet one record. The record holds a trigger, ordered steps, alternative paths, step failures, and an end state. This decision names that record a flow, so the type uses the standard engineering word for what it holds.

## Context

The name came from customer journey maps, which hold touchpoints, emotions, and channels. The record holds none of those. It holds a trigger, ordered steps, alternative paths, step failures, and an end state. In software terms that is a flow. Every other LiteSpec type used a standard engineering name. Prose used flow only loosely, never as a type.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| flow | The standard user-flow term, fitting any actor and matching steps and branches. | Every record, identifier, folder, provision, and citation moves. |
| workflow | Known outside software. | Suggests automation engines. |
| process | Standard in operations. | Too formal for one actor's steps. |
| Keep journey | Nothing moves. | The map's touchpoints and emotions stay in the name. |

## Decision

The type is `flow`. It is the standard user-flow term, it fits any actor, and it matches the fields. `steps` are the main flow and `branches` the alternative flows.

Each rejected name fails on its own meaning. `use-case` and `scenario` name other records. `task` clashes with backlog items. `story` means a small slice. `workflow` suggests automation engines and `process` a managed operation. The record holds one actor's ordered steps.

The rename follows the flow reason and the step citations, so it moves the final shape once.

## Consequences

Identifiers read `flow/<name>` and `flow/<name>#s1`. Flows live in `intent/flows/`.

The sixteen provisions naming the old type take the slugs their new headings derive, with every citation replaced in the same change.

The book, the model builder, `litespec new`, and `litespec rename` read and write the new shape. Claimwick's three flows move to `intent/flows/`.

## Evidence

[research/naming-grammar-evidence](research/naming-grammar-evidence) counted 248 field names with 18 genuine collisions behind one grammar with no exceptions. [research/field-naming-evidence](research/field-naming-evidence) found one concept carrying two names already producing misleading consumer data. The record's fields were read from the schema with high confidence.

## Revisit Conditions

This decision reopens when a consumer shows that flow misleads its readers about what the record holds.
