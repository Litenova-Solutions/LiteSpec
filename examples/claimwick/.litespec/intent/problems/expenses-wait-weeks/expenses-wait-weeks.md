# Expenses Wait Weeks

Employees cannot get a work expense paid back quickly, because nothing shows the approver how long an expense has waited. As a result, employees carry the amount from their own account for weeks and chase the approver by email. This problem describes that wait, what it costs employees and what would end it.

## Affected Users

The wait falls on employees who submit expenses. The problem record names them as `actor/employee`.

The affected employee submitted an expense and waits for a decision on it. For the length of that wait, the employee carries the amount from their own account.

## Failure Cases

- **An expense waits without a decision.** Nothing shows the approver how long an expense has waited, so the expense waits until the employee chases it. Until then, the employee carries the amount from their own account.
- **The employee cannot see the decision.** Nothing tells the employee when the approver decides, so the employee learns the outcome only by asking. A decided expense still looks open, and the employee chases a decision the approver already made.

## Current Workaround

Employees email the approver to ask for a faster decision. The approver answers from memory, with no record of how long the expense has waited.

Each employee repeats that email for each waiting expense, and keeps track of which expenses are still open unaided.

## Cost

The cost is counted in weeks of waiting, not in money. Employees carry the amount from their own account for the length of the wait, and the evidence puts the average wait at 3 weeks. Employees reported that figure; nobody measured it.

No total of carried money is recorded for this problem, so the money cost is unknown.

## Evidence

The evidence is one reported claim: employees wait 3 weeks on average for reimbursement. The claim cites `research/approval-time-benchmarks`, and it was read on 2026-11-01.

The record marks the claim as reported, not measured. The research sets the comparison point: small companies decide expenses within 1 week.

## Resolution Criteria

- Each submitted expense is decided within 5 business days.
- The employee sees the decision without asking the approver.

The 5-day rule is set in `requirement/approval.decided-within-five-days`.
