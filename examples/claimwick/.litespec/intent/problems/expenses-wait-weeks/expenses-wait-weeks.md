# Expenses Wait Weeks

Employees who submit a work expense wait weeks for the decision that pays it back. While they wait, they carry the amount from their own account, and they chase the approver by email. This problem describes that wait, what it costs employees and what would end it.

## Affected Users

Employees who submit expenses are affected by this problem. The problem record names `actor/employee` as the actor it affects.

The affected employee submitted an expense and waits for a decision. During that wait, the employee carries the amount from their own account.

## Current Workaround

Employees email the approver to ask for a faster decision. The approver answers from memory, and no record shows how long the expense has waited.

## Cost

The cost is recorded in weeks of waiting, not in money. Employees carry the amount from their own account for the length of the wait, and the evidence puts the average wait at 3 weeks.

No total of carried money is recorded for this problem.

## Evidence

The evidence is one reported claim: employees wait 3 weeks on average for reimbursement. The claim cites `research/approval-time-benchmarks` as its source, and it was read on 2026-11-01.

The record marks the claim as reported, not measured. The research gives the comparison point: small companies decide expenses within 1 week.

## Resolution

The problem ends when each submitted expense is decided within 5 business days, and the employee sees the decision without asking.

Those two conditions remove both the wait and the chase by email. The 5-day rule is set in `requirement/approval.decided-within-five-days`.
