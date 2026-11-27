# Approver Is Not Submitter

This rule keeps the approval check independent of the person who submitted the expense. It protects the company by making a second person review each cost before payment, and it protects employees from a manager deciding their own claim.

## Rationale

`rule/expense.approver-is-not-submitter` governs `use-case/spending.approve-expense`, where a wrong approval sends an out-of-policy expense toward payment. The requirement `requirement/approval.nobody-approves-own-expense` says nobody approves their own expense. Its rationale states the reason: a second person checks each expense before it is paid. The refusal `refusal/spending.own-expense` names the failure case, where the approver submitted the expense.

## Example

Ava submits her 48.90 EUR train fare, and Ben, her manager, approves it. The approval succeeds because Ben did not submit the fare. Ben also submits an expense of his own. When he tries to approve it, Spending refuses with `spending.own-expense`.
