# Send Approver Reminders on Expense Submitted

This reaction reminds the approver of an expense that waits too long for a decision. It runs when `domain-event/expense.submitted` is raised, so it considers each new expense. It supports `requirement/approval.decided-within-five-days`.

## Trigger and Effect

Each `domain-event/expense.submitted` starts the reaction. The reaction waits the approval reminder delay from `setting/spending.approval-reminder-delay`. If the expense is still undecided then, the reaction sends `notification/spending.approval-reminder` to the approver. A decided expense is skipped, and so is an expense whose approver holds a reminder preference.

A reminder is due only after the wait, so the reaction cannot send it when the expense is submitted.

## Failure Handling

A failed send retries up to five times with a growing delay. A failure past the last retry dead-letters the reminder after one day. Each waiting expense earns one reminder: the reaction recognizes its own repeats by the key `approval-reminder-{expenseId}`, and the notification sends with frequency once.

## Example

Ava submits the 48.90 EUR train fare, and Ben leaves it undecided on the first day. Ben approves the fare on the second day, before the waiting time ends. When the waiting time ends, the reaction finds the fare decided, so it sends nothing.
