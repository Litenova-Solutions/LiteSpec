# Expenses Wait Weeks

Employees cannot get a work expense paid back quickly, because nothing shows the approver how long an expense has waited. As a result, employees carry the amount from their own account for weeks and chase the approver by email. This problem describes that wait, what it costs employees and what would end it.

## Affected Users

The wait falls on employees who submit expenses. The problem record names them as `actor/employee`.

The affected employee submitted an expense and waits for a decision on it. For the length of that wait, the employee carries the amount from their own account.

## Failure Cases

- An expense waits without a decision while the approver sees no waiting time and the employee carries the amount.
- The employee learns the decision only by asking and chases a decision already made.

Ava submits the 48.90 EUR train fare on Monday. Ben sees the fare with no waiting time, so the fare waits while Ava carries the amount. When Ben decides it on Tuesday, Ava still sees it as open and asks Ben before she learns the decision.

## Example

Ava submits the 48.90 EUR train fare for the client visit and waits for Ben. The fare shows no waiting time, so Ben leaves it undecided while Ava carries the amount. Ava emails Ben, and Ben answers from memory that he has not decided it yet.

## Current Workaround

Employees email the approver to ask for a faster decision. The approver answers from memory with no record of the wait. Each employee repeats the email for each waiting expense.

## Cost

Employees carry the amount from their own account for the length of the wait. The average wait is three weeks as reported, and the money cost is unknown.

## Evidence

The evidence is one reported claim: employees wait 3 weeks on average for reimbursement. The claim cites `research/approval-time-benchmarks`, and it was read on 2026-11-01.

The record marks the claim as reported, not measured. The research sets the comparison point: small companies decide expenses within 1 week.

## Resolution Criteria

- Each submitted expense is decided within five business days.
- The employee sees the decision without asking the approver.

The 5-day rule is set in `requirement/approval.decided-within-five-days`.
