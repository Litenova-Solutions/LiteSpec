# Remove Shapes Nothing Needs

A schema holding modes no field can fill and kinds no record uses teaches authors shapes that have no use. This decision removes each such shape, so every remaining value has a use and a check.

## Context

The review behind this release read every value against live records. The `compensated` mode had an undo no field holds. The `manual` job kind duplicated a command with its guard. The `default` rule kind duplicated a setting default or an initial state. The `cross-aggregate` kind duplicated a home with `binds`. The `not-pending` mode had no definition. `builtAsynchronously` duplicated `timing`. Three prose fields on external systems duplicated their typed operations. `writes` on reactions and jobs had exactly the use the automation provision forbids. The `floorplan` link pointed at a contract owned elsewhere. Nothing read two container definitions in the schema.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| Remove each unused or unheld shape | Every value has a use and a check. | Records using the shapes move. |
| Keep the shapes, stop writing them | Nothing breaks. | Authors keep using dead shapes. |
| Remove values, keep fields | Smaller move. | Empty fields stay everywhere. |

## Decision

Each shape above is removed. A manual run is a command whose entry point and access name who starts it. A default is a setting's default or an aggregate's initial state. A rule's scope comes from its home, with `binds` naming the aggregates of a module-level rule. The `floorplan` link lives in the frontend design contract where floorplans are defined.

The `compensated` mode stays removed. Its undo had no field. The `compensates` link on the reaction is that field. A reaction that undoes a committed step of a multi-step process names the step's use case in `compensates`.

The `reservation` and `frozen-value` rule kinds, the erasure positions, and the job scheduling fields stay despite low use. Two products are thin evidence for a standard.

The `store` aggregate kind is finished rather than removed. It gains its `owner` field and a Claimwick instance holding the suppression list.

## Consequences

Consistency keeps `atomic` and `per-record`. Idempotency keeps `key`, `state`, and `none`.

The automation provision forbidding `writes` on reactions and jobs states a shape the schema refuses.

A reaction that undoes a committed step names it in `compensates`, as `litespec/core/automation#name-the-step-a-compensating-reaction-undoes` states.

Templates drop the removed fields, so no new record starts from them.

## Evidence

[research/check-coverage-evidence](research/check-coverage-evidence) mapped which values the checker reads. The removed values are the ones no check reads. [research/field-naming-evidence](research/field-naming-evidence) found one name carrying two concepts, which the removals fix. Each removed shape was counted against live records with high confidence.

## Revisit Conditions

This decision reopens when a removed shape proves to be the only shape carrying a behavior a product needs.
