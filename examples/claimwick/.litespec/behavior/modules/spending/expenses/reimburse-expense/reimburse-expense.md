# Reimburse Expense

Reimburse Expense pays an approved expense back to the employee who submitted it. Money moves in this use case, so a payment made twice reaches the employee twice. The rule `rule/expense.reimbursed-once` guards against that, and the payout bank settles each batch once.

## Flow

Finance picks an approved expense. Spending refuses the payment with `spending.not-approved` when the expense is not approved. It refuses the payment with `spending.already-reimbursed` when the expense is already reimbursed.

Otherwise the expense moves to Reimbursed, and `event/expense.reimbursed` is raised with the amount, the currency and the payment time. The payout batch then carries the expense to `external-system/payout-bank`. No audit entry is stored. The first payment moves the expense to Reimbursed, so a second payment finds it already paid and is refused.

## Example

Ben approved Ava's 48.90 EUR train fare earlier in the week. Cara pays it on Friday. The expense moves to Reimbursed, and `event/expense.reimbursed` is raised with the 48.90 EUR amount. The payout batch carries the payment to the payout bank.

The next Friday, Cara tries to pay the same expense again. Spending refuses the payment with `spending.already-reimbursed`, so Ava receives the 48.90 EUR once.

## Edge Cases

No payment goes out for an expense that is not approved. When Cara tries to pay Ava's fare before Ben has approved it, spending refuses with `spending.not-approved`. The expense stays Submitted, and no payment is made.
