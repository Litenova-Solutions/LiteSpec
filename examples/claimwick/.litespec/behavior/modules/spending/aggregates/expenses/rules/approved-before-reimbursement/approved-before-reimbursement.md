# Approved Before Reimbursement

This rule keeps unapproved costs out of the payout batch, so finance pays only expenses a manager has agreed to. It protects the company from money leaving for a cost nobody approved, and it tells finance when to stop.

## Rationale

`rule/expense.approved-before-reimbursement` is a precondition on `use-case/spending.reimburse-expense`. Money moves in that use case, so a payment made too early sends an out-of-policy cost to the employee. The requirement `requirement/approval.expense-reimbursed-at-most-once` says an approved expense is reimbursed at most once. Its rationale notes that a double payment is hard to take back. The refusal `refusal/spending.not-approved` names the state that stops the payment: the expense is not approved.

## Example

Ava submits her 48.90 EUR train fare for the client visit, and Ben approves it. Cara then reimburses the fare on Friday. Without Ben's approval, Spending refuses Cara's payment with `spending.not-approved`. Ava is not paid from an undecided expense.

## Limits

This rule checks approval only. It does not count payments on an expense already reimbursed; the rule `rule/expense.reimbursed-once` covers that case.
