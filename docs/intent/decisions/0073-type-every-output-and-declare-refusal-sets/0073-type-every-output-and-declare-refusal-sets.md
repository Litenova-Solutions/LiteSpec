# Type Every Output and Declare Refusal Sets

A use case ends by returning values or by refusing with a named reason. This decision types every output field and declares each module's refusal codes once, so callers and readers meet the same shapes everywhere.

## Context

Outputs carried no value types, so a file input stayed a text field with prose beside it and a list of rows stayed undescribed. Refusal codes scattered across results: each result wrote its own code with its own meaning, and generic refusals were written by hand beside the ones the model derives.

Rules had the same scatter. One rule's condition could be refused two ways in two use cases, and a rule over several aggregates named its scope with a kind instead of naming the aggregates.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| Typed outputs with files and rows, one refusal set per module | Every output has a type; each code is declared once. | Outputs gain types; each module writes its set. |
| Keep untyped outputs and per-result refusal prose | Nothing moves. | Files stay in prose; codes scatter. |
| A product-wide refusal list | One list holds every code. | Modules share codes they never raise. |

## Decision

A `file` value type carries `contentTypes` and `maxSize`. Every output field takes a value type. A list of rows takes `of` a shape: typed fields, `required` per field, `minItems`, and `unique` keys. The choice list moves from `of` to `choices`, so `of` keeps one meaning. Values the system derives leave the input list and become outputs or calculations.

Each module declares its refusal codes once in its `refusals.yaml`, with each code's meaning and the problem type it maps to. Results and rule `refusal` links cite the set. The product-wide generic refusals derive and are never written.

One rule states one condition with its refusal code from the module's set. Every use case that can break the rule lists the refusal result citing that code. A module-level rule over several aggregates names them in `binds`, so the rule's scope comes from its home instead of a kind.

## Consequences

Claimwick carries a refusal set per module that refuses, a file input, and typed outputs on every command, so each shape has one instance the docs cite. A hand-written generic refusal is reported, and a result code missing from its module's set is reported.

## Evidence

[research/lower-layers-practice](research/lower-layers-practice) confirmed uneven contracts below Intent with design wording that overclaims what the records hold. [research/field-naming-evidence](research/field-naming-evidence) found one field name carrying two concepts, which the split of `of` and `choices` answers. The untyped outputs were counted against the schema with high confidence.

## Revisit Conditions

This decision reopens when a refusal needs a code no module set can declare. It also reopens when a value needs a shape the value types cannot carry.
