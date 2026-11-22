# Reimbursed Once

Finance pays each expense at most once. The rule protects the company from paying the same expense twice and keeps each spender's reimbursement equal to what was approved.

## Rationale

A payment that has left the company is hard to take back, so a double payment costs more to undo than to prevent. The requirement `requirement/approval.expense-reimbursed-at-most-once` commits to one reimbursement per approved expense. The first payment moves the expense to Reimbursed, and `use-case/spending.reimburse-expense` refuses any later payment with `refusal/spending.already-reimbursed`.

## Example

Cara pays Ava's approved 48.90 EUR fare on Friday, and the expense moves to Reimbursed. The next Friday, Cara tries to pay the same fare again. Spending refuses with `spending.already-reimbursed`, so Ava receives 48.90 EUR once.

## Limits

The rule counts payments, not approvals. `rule/expense.approved-before-reimbursement` makes sure finance pays only an expense a manager approved.
