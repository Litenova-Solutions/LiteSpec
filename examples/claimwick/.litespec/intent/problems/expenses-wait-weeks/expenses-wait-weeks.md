# Expenses Wait Weeks

Employees cannot get a work expense paid back quickly, because nothing shows the approver how long an expense has waited. As a result, employees carry the amount from their own account for weeks and chase the approver by email. This problem describes that wait, what it costs employees and what would end it.

## Affected Users

Employees who submit expenses are affected by this problem. The problem record names `actor/employee` as the actor it affects.

The affected employee submitted an expense and waits for a decision. During that wait, the employee carries the amount from their own account.

## Failure Cases

- **An expense waits without a decision.** Nothing shows the approver how long an expense has waited, so it waits until the employee chases it. Until then, the employee carries the amount from their own account.
- **The employee cannot see the decision.** Nothing tells the employee when the approver decides, so the employee learns the outcome only by asking. A decided expense still looks open, and the employee chases a decision that was already made.

## Current Workaround

Employees email the approver to ask for a faster decision. The approver answers from memory, and no record shows how long the expense has waited.

Each employee repeats the email for each expense that waits, and has to remember which expenses are still open.

## Cost

The cost is recorded in weeks of waiting, not in money. Employees carry the amount from their own account for the length of the wait, and the evidence puts the average wait at 3 weeks. That figure is reported by employees, not measured.

No total of carried money is recorded for this problem, so the money cost is unknown.

## Evidence

The evidence is one reported claim: employees wait 3 weeks on average for reimbursement. The claim cites `research/approval-time-benchmarks` as its source, and it was read on 2026-11-01.

The record marks the claim as reported, not measured. The research gives the comparison point: small companies decide expenses within 1 week.

## Resolution Criteria

- Each submitted expense is decided within 5 business days.
- The employee sees the decision without asking the approver.

The 5-day rule is set in `requirement/approval.decided-within-five-days`.
