# Put Each Feature Inside the Problem It Answers

A feature is an offer that answers one pain. This decision places each feature in the folder of that pain, so a reader of a problem sees every answer to it, and a feature answers exactly one problem.

## Context

A feature named its problems in a `solves` list. The provision that governed sibling problems accepted one feature that answered two, so the same offer could be filed under two pains, and each pain could be written twice.

The problem file itself held no link back to its features. A reader of a problem had to search the feature files to find what answered it.

In the reference consumer, each of the 40 features solved exactly one problem. The parent of a requirement already sits in its folder, so the same rule was applied to features.

## Options

LiteSpec compared 3 options for where a feature's problem is recorded. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Feature inside the problem's folder | A problem's folder shows its features, and the folder names the parent. | Every feature moves and every path changes, while identifiers stay. |
| Problem as a field, features flat | One feature list, and a move edits one field. | A problem file cannot show its features, and the field must agree with other links. |
| Keep `solves` as a list | No feature moves. | One feature can answer two problems, and a problem's features show only from the features. |

## Decision

A feature sits in the folder of the one problem it answers. Two pains that one offer answers are one problem. An offer that answers two pains is two features. A feature whose requirements answer a second problem is split, or the two problems are merged.

Cross-cutting qualities do not live in a features folder. A baseline quality the standard already requires gets no record. A quality of one offer is a requirement inside that feature. A product-wide quality the product commits to beyond the standard is a product-level requirement.

## Consequences

`solves` is removed, and the folder names the problem. `record-invalid` reports a feature that still carries it, and `record-placement-invalid` reports a feature outside a problem's features folder. `litespec new feature` takes the problem and writes the feature into it.

`litespec/core/intent#answer-one-problem-with-each-feature` is required, and it retires the sibling-problem provision. Identifiers carry no problem, so a move changes no citation. The Links table reads answers and is answered by for the folder parent, and the book shows each problem with its features.

## Revisit Conditions

This decision holds until consumers report that features with no outside pain are forced onto weak problems by the folder. It also holds until a feature's requirements answer a second problem often enough that splitting costs more than sharing. Either report reopens the choice.
