# Keep Shape Rules in the Schema

LiteSpec checks each record twice: a JSON schema lets an editor check a YAML file as it is written, and the model check reads the whole set of records. This decision divides the rules between the two, so the shape of a record is stated in the schema and its links are checked by the model check.

## Context

The schema and the model check disagreed on some local requirements. An editor validating against the schema accepted a role without `allows`, which the model check required. Decision `choice`, empty research sources, module `dependsOn` and empty policy values had the same disagreement.

The schema also used `uniqueItems` on lists of objects, such as research sources and journey steps. That rule compares whole objects, so two entries that share one key both passed.

## Options

LiteSpec compared 3 options for where each rule lives. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Shape rules in the schema, graph rules in the model check, and a parity test | An editor and the check agree on shape, and the test shows it. | Each rule has one of two homes, and the test runs in continuous integration. |
| Every rule in the model check, with the schema for editors only | One validator holds every rule. | An editor accepts records the check rejects. |
| Every rule in both, with no test | Each validator enforces the full set. | Nothing shows that the copies agree, and they drift. |

## Decision

The schema holds the rules a single record can be checked against: required properties, minimum counts, conditional requirements and allowed values. The model check holds the rules that need the record graph: reference resolution, ownership, scope, reach and consistency across records.

A test runs both validators over a set of invalid records and asserts that they agree on every rule that both carry.

## Consequences

The five named disagreements are settled and written into the schema. Duplicate keys inside a record are reported by the model check, because `uniqueItems` cannot catch them. Each duplicate-key case has its own test.

The change touches the schema, the schema checks and the parity test. No record changes shape because of it, except where a disagreement is settled in favor of the stricter rule.

## Revisit Conditions

This decision holds until the schema and the model check disagree on a rule that the parity test does not cover. It also holds until an editor's schema support loses a rule the schema can no longer carry. Either report reopens the choice.
