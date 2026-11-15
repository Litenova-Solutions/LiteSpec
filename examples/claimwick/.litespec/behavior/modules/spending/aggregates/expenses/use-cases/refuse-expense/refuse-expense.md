# Refuse Expense

Refuse Expense is the decision by which a manager turns down a submitted expense that falls outside the policy. The employee learns the outcome from it. A refused expense can never be reimbursed, since reimbursement accepts only approved expenses.

## Flow

The manager opens a submitted expense and rejects it with a reason. The records carry the reason in the reason input so the employee sees it. Spending refuses the rejection with `spending.not-submitted` when the expense is already decided.

Otherwise the expense moves to Refused, and `domain-event/expense.refused` is raised with the refusing manager and the decision time. No audit entry is stored. A second rejection of the same expense is refused, because the first rejection already decided it.

## Example

Ava submits her hotel night for the client visit. Ben checks it, finds that it falls outside the policy, rejects it and gives a reason. The expense moves to Refused.

Ben rejects the same expense again, and spending refuses the second rejection with `spending.not-submitted`. Cara later tries to reimburse the expense, and spending refuses that too, because reimbursement needs an Approved expense.
