# Use the Standard Term for Each Concept

Authors and readers meet each concept in the record, the folder, the code, the book, and the finding code. One concept carries one word in all five. This decision renames each name that broke that rule to its standard term. It records which names stay.

## Context

`view` collided with UI and database views while naming a CQRS read model. `event` appeared beside `integration-event` while the prose already said domain event. The checks used writes while the field used `changes`. `transitions.by` violated the word-class rule the Links table already stated as moved by. `runsWhere` held a condition in a Where form. `then` in `onFailure` reused a scenario word. `exempt` held a reason in a flag form. `commercial-data` obscured a frozen value. Two page styles used list beside `rule-set` and `setting-set`. Finding codes used orphaned for unused and group for atomicity. Prose used constituent for the `parts` field, door for an entry point, and systems home for external systems.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| Rename every listed name, keep the rest | One standard term per concept everywhere. | Every renamed term moves in every file. |
| Rename only the record types | Smaller move. | Fields, codes, and prose keep the old words. |
| Keep every name | Nothing moves. | The collisions stay in the language. |

## Decision

Each name above moves to its standard term. The terms are `read-model`, `domain-event`, `writes`, `movedBy`, `runsWhen`, `ifExhausted`, `exemptBecause`, `frozen-value`, `permission-set`, `entry-point-set`, `role-unused`, `permission-unused`, `atomicity-*`, `part`, `notification-effect-missing`, and the provision part `Waiver`.

A frozen value names what the rule does. It is a value copied onto a record at the act and never recomputed.

Some names stay, each for its reason. `reaction` stays, because the record holds the trigger, failure policy, idempotency, and edge beside the handler. `job`, `scenario`, `result`, `rule`, `refusal`, and `entry point` stay, naming their concepts plainly. `reachedThrough` stays, with the word-class rule restated to allow a participle with `By`, `Through`, `In`, or `From`. `effects` stays, with its reads-as phrase corrected.

## Consequences

The glossary gains Read Model, Rule Set, Setting Set, Permission Set, Refusal Set, and Act. It merges Event into Domain Event.

Record folders, code folders, finding codes, and provisions move with the names in the same change.

The book labels follow the generated Links table, so they cannot keep the old words.

## Evidence

[research/naming-grammar-evidence](research/naming-grammar-evidence) counted 248 field names with 18 genuine collisions behind one grammar with no exceptions. [research/field-naming-evidence](research/field-naming-evidence) found one concept carrying two names already producing misleading consumer data. Each collision above was read from the schema and the code with high confidence.

## Revisit Conditions

This decision reopens when a renamed term collides with another concept. It also reopens when practice settles on a better standard term.
