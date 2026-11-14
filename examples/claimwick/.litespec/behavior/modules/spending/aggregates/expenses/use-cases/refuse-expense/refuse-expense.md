# Refuse Expense

Refuse Expense is the decision a manager makes to turn down a submitted expense that falls outside the policy. The employee learns the outcome from it. It matters because a refused expense can never be reimbursed, since reimbursement accepts only approved expenses.

## Flow

The manager opens a submitted expense and rejects it. The records state that the manager gives a reason, but they do not record a field for it. Spending refuses the rejection when the expense is already decided, with `spending.not-submitted`.

Otherwise the expense moves to Refused, and `event/expense.refused` is raised with the refusing manager and the decision time. No audit entry is stored. A second rejection of the same expense is refused, because the first rejection already decided it.

## Example

Ava submits her hotel night for the client visit. Ben checks it and finds that it falls outside the policy. He rejects it and gives a reason. The expense moves to Refused.

Ben rejects the same expense again. Spending refuses the second rejection with `spending.not-submitted`. Cara later tries to reimburse the expense, and spending refuses that too, because reimbursement needs an Approved expense.
