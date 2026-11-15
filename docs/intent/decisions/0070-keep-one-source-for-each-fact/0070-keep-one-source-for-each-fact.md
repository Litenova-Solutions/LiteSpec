# Keep One Source for Each Fact

A Behavior fact such as a retention period or a use case's reach should read the same wherever it appears. This decision moves each such fact to its one source and checks each stated copy, so the trace from Intent to code holds on facts that agree.

## Context

One fact sat in two to four places. Retention basis, period, enforcement, and purpose sat on both the data class and the retention schedule page. Exposure, classification, and membership each had a stated form and a derived form with no check between them.

In one consumer, 8 of 23 retention rows disagreed with their data class, and 19 field classifications differed from their class. The model check reported no blocking finding either way.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| Move each fact to its source, check each copy | One source per fact, with every copy checked. | Records move fields in one release. |
| Derive every copy, forbid stating it | No copy drifts. | Isolated files stop reading whole. |
| Leave each fact where written | Nothing moves. | Facts stay on two pages and disagree. |

## Decision

Retention moves onto the data class, and the retention schedule becomes computed. An aggregate data class holds `period`, `periodStartsAt`, `basis`, `purpose`, `enforcedBy`, and `exceptions`. Product-wide facts move to their content: `processors` becomes `processes` on the external system that holds the data, and conflicts become data-class `exceptions`. The book renders the schedule from the data classes.

Classification and membership stay explicit and are checked. Each field keeps its `classification` and each data class keeps its `fields` list, with a check for a field differing from its class and for a list differing from the fields naming it. `governs` stays on rules under a check against every link naming the rule. `exposure` stays on use cases with a fixed meaning: in plain words, who reaches the use case, through which entry points, under which permission, and what is at stake if it goes wrong.

`background` keeps one meaning: the Given shared by every scenario of the use case, with at least two scenarios where it appears. Explanation files interpret and never restate a field value, and a rule adds checkable precision over its requirement instead of rephrasing it. Failure and idempotency are required on every reaction and job. Queries carry no idempotency and no consistency, since a read is idempotent by kind; `consistency` is required only on a command writing more than one aggregate, since a single-aggregate command is atomic by definition.

## Consequences

Claimwick fills `exposure` on all 10 use cases, so every field family has one instance the docs cite. The retention schedule record type is removed. Company narrative moves out of `background` into the explanation file.

## Evidence

[research/check-coverage-evidence](research/check-coverage-evidence) found no check reading the links above a requirement, where the largest measured gaps sit. [research/behavior-containment-evidence](research/behavior-containment-evidence) found containment by identifier sound and complete, which the moved links keep. The retention disagreements were measured over live records with high confidence.

## Revisit Conditions

This decision reopens when a fact with one source proves unreadable where its reader needs it. It also reopens when a required copy costs more than its check returns.
