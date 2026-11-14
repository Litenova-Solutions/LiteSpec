# Copy Each Template from Claimwick

A template in LiteSpec does three jobs at once: it is the file `new` copies, it is a worked example and it is a field reference. This decision makes each YAML template a copy of one Claimwick record, so the example stays coherent and no copied value passes as a decision.

## Context

Templates cited records that did not exist, and some of their links proved nothing. The requirement template stated that the product decides every expense within five working days, when managers decide. The notification, reaction and job templates cited that requirement for claims it does not hold.

Sample values such as a retention period of seven years or three retry attempts looked like decisions to an agent that copied them. A prompt assumed one kind per type: the aggregate prompt assumed a lifecycle, and the job prompt assumed a schedule, though some kinds have neither.

The idempotency key of one use-case template was built from the amount, so two different expenses of the same amount would collide.

## Options

LiteSpec compared 3 options for the source of a template. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Copy each template from a Claimwick record and report unchanged values | Links resolve in one product, and a copied value is reported. | A template follows its record, and Claimwick must carry each record the templates need. |
| Three file sets for scaffold, example and reference | Each file does one job. | Three files per type drift apart. |
| Placeholder values reported by model check | A scaffold is plainly unfinished. | The no-placeholders principle refuses this, and placeholders are useless as examples. |

## Decision

Each YAML template is a copy of one Claimwick record. `litespec-maintain validate` reports a template that differs from its source, and every link in a template resolves in Claimwick.

A scaffolded record that still carries a template value unchanged reports `template-value-unchanged`. The example values stay readable as examples, and none passes as a decision.

The explanation prompts follow the kind a record takes, so a prompt asks only for what the record's kind can hold.

## Consequences

Claimwick gains the reminder requirement, the retention requirement and the query use case that the templates need. The command and query use cases each have their own template, and the command template's idempotency key comes from a request identifier that the input declares.

The guides quote Claimwick's finished records, so each example in a guide passes `model check`.

## Revisit Conditions

This decision holds until a template copied from Claimwick keeps a value a reader takes for a rule. It also holds until Claimwick must carry records that a product of another kind would never need. Either report reopens the choice.
