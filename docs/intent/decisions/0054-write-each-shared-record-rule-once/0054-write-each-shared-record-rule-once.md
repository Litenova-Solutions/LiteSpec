# Write Each Shared Record Rule Once

Several record types repeat the same faults, and each fault has one cause in LiteSpec's guidance. This decision states each shared rule once, as a provision that applies to every record type it concerns, instead of writing a copy for each type.

## Context

Four faults appeared in more than one record type. Records stated rules that other records own: accounting rules in journeys, product effects in constraints, sign-in rules in actors and product consequences in research. A shorter form dropped the conditions of the claim it shortened, in constraint summaries, research conclusions and actor openings.

The Example prompts of eight templates asked for numbers "worked through", which invited figures that rest on rules no record holds. Constraints and research both needed a rule for when to split a record. Several records stated release phases and future plans, which the product as it stands does not support.

Writing a separate rule for each type would have written the same rule several times in slightly different words, and each copy would have drifted.

## Options

LiteSpec compared 3 options for where a shared rule is written. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| One provision per shared rule | A rule is stated and read once. | Each provision names the record types it covers. |
| A rule per record type in each workstream | Each page holds all of its type's rules. | The same rule is written several times and each copy drifts. |
| Template wording only | No provision is added. | No check or review reads the rule. |

## Decision

A rule that more than one record type shares is one provision. The provision applies to every record type it concerns, and each record type's page and template cite it instead of stating a copy.

The shared rules cover what an explanation file describes, keeping the conditions when a claim is shortened, drawing each Example detail from the cast or a cited record, splitting a record whose parts change separately, and keeping release phases and plans out of records.

## Consequences

The existing rule about behavior in explanation files applies to every Intent explanation file under the same identifier. The Example prompts of the eight templates share one sentence form, and the journey, aggregate and job prompts add the detail their type needs. No template still asks for numbers worked through or opens a prompt with walk.

Each provision is required and reviewed for, since no check can judge whether a summary keeps its condition.

## Revisit Conditions

This decision holds until a shared provision is cited by record types it does not fit. It also holds until an Example prompt needs a rule for one type that the shared sentence cannot carry. Either report reopens the choice.
