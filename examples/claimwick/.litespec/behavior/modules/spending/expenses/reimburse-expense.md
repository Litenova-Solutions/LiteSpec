# Reimburse Expense

## What Happens

Finance picks an approved expense. Spending refuses the payment when the expense is not approved or is already reimbursed. Otherwise the expense moves to Reimbursed, `event/expense.reimbursed` is raised, and the payout batch carries it to `external-system/payout-bank`.
