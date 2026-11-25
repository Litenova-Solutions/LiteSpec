# Reimburse Expense

Reimburse Expense pays an approved expense back to the employee who submitted it. Money moves here, so a payment made twice reaches the employee twice. The rule `rule/expense.reimbursed-once` guards against that, and the payout bank settles each batch once.

## Flow

1. Finance picks an approved expense, stating the payment fee when the payout carries one.
2. Spending refuses the payment with `spending.not-approved` when the expense is not approved.
3. Spending refuses the payment with `spending.already-reimbursed` when the expense is reimbursed.
4. Spending moves the expense to Reimbursed and raises the reimbursed event.

The payout batch then carries the expense to `external-system/payout-bank`. No audit entry is stored. The first payment moves the expense to Reimbursed, so a second payment finds it already paid and is refused.

## Example

Ben approved Ava's 48.90 EUR train fare earlier in the week, and Cara pays it on Friday. The payout carries no fee. The expense moves to Reimbursed, `domain-event/expense.reimbursed` is raised with the 48.90 EUR amount, and the payout batch carries the payment to the payout bank.

The next Friday, Cara tries to pay the same expense again. Spending refuses with `spending.already-reimbursed`, so Ava receives the 48.90 EUR once.

## Edge Cases

- Spending refuses payment of an unapproved expense with `spending.not-approved`.

The expense stays Submitted, and no payment is made.
