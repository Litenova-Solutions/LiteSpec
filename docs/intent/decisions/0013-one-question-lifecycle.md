# One Question Lifecycle

## Context

Questions carry a self-declared tier that can clear the open-question guard on a tier 1 record, answers have no recorded shape, and the auto-approval wording lets silence approve a whole record.

## Decision

A question carries `question`, `decider`, `decideBy`, `ballot`, `recommendation`, and once answered `answer`, `answeredBy`, `answeredOn`. `ballot` is required, `recommendation` is required when the record carries no `risk`, a question carries no tier of its own and approval follows the record's risk class under the project's approval table, and an unanswered question is open. Only a question auto-approves after its `decideBy` plus the veto window, recorded as `answeredBy: auto (veto window)` on the question; no record carries `approvedBy: auto (veto window)`.

## Evidence with Confidence

`research/naming-grammar-evidence` holds the question-field inventory, and `research/lower-layers-practice` holds the approval review behind the risk-class approval rule, both read from the repository and measured over the largest consumer, high confidence.
