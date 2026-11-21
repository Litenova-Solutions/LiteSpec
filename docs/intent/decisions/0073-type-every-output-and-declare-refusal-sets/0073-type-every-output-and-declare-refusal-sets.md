# Type Every Output and Declare Refusal Sets

A use case ends by returning values or by refusing with a named reason. This decision types every output field and declares each aggregate's refusals once, so callers and readers meet the same shapes everywhere.

## Context

Outputs carried no value types. A file input stayed a text field with prose beside it. A list of rows stayed undescribed. Refusal codes spread across results. Each result wrote its own code with its own meaning. Generic refusals were written by hand beside the ones the model derives.

Rules spread the same way. One rule's condition could be refused two ways in two use cases. A rule over several aggregates named its scope with a kind instead of naming the aggregates.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| Typed outputs with files and rows, one refusal set per aggregate | Every output has a type; each refusal is declared once. | Outputs gain types; each aggregate writes its set. |
| Keep untyped outputs and per-result refusal prose | Nothing moves. | Files stay in prose; codes spread. |
| A product-wide refusal list | One list holds every code. | Modules share codes they never raise. |

## Decision

A `file` value type carries `contentTypes` and `maxSize`. Every output field takes a value type.

A list of rows takes `of` a shape. The shape holds typed fields, `required` per field, `minItems`, and `unique` keys. The choice list moves from `of` to `choices`, so `of` keeps one meaning.

Values the system derives leave the input list and become outputs or calculations.

Each aggregate declares the refusals its rules raise once in its own `refusals.yaml`, mirroring `Domain/{Aggregates}/Exceptions/`. Each entry carries its `id` and `title`; the code is the id's name. The module's `refusals.yaml` keeps only the refusals no aggregate raises.

A result's `refusal` names a refusal from its aggregate's set, or from the module's set for a refusal no aggregate raises, and a rule's `refusal` names the same refusal.

The product-wide generic refusals derive and are never written.

One rule states one condition with its refusal from its aggregate's set. Every use case that can break the rule lists the refusal result citing that refusal. A module-level rule over several aggregates names them in `binds`, so the rule's scope comes from its home instead of a kind.

## Consequences

Claimwick carries a refusal set per aggregate that refuses, with the module's set keeping only the refusals no aggregate raises, a file input, and typed outputs on every command. Each shape has one instance the docs cite.

A hand-written generic refusal is reported. A result refusal missing from its aggregate's set is reported.

## Evidence

[research/lower-layers-practice](research/lower-layers-practice) confirmed uneven contracts below Intent with design wording that overclaims what the records hold. [research/field-naming-evidence](research/field-naming-evidence) found one field name carrying two concepts, which the split of `of` and `choices` fixes. The untyped outputs were counted against the schema with high confidence.

## Revisit Conditions

This decision reopens when a refusal needs a code no module set can declare. It also reopens when a value needs a shape the value types cannot carry.
