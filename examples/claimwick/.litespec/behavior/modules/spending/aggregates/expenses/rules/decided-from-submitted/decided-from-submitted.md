# Decided from Submitted

This rule keeps each decision tied to an expense that still waits for one. It protects managers from deciding twice and employees from receiving two different outcomes on the same expense.

## Rationale

`rule/expense.decided-from-submitted` governs both `use-case/spending.approve-expense` and `use-case/spending.refuse-expense`. Each command moves the expense out of Submitted on its first run, and the aggregate refuses a second decision. The refusal `refusal/spending.not-submitted` names that case: the expense is already decided. The requirement `requirement/approval.decided-within-five-days` asks Spending to decide each submitted expense. Once the expense is decided, it no longer waits in Submitted.

## Example

Ava's 48.90 EUR fare waits in Submitted. Ben approves it, and the expense moves to Approved. When Ben tries to approve it again, Spending refuses with `spending.not-submitted`. The expense keeps its Approved outcome.
