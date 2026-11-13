# Send Approver Reminders on Expense Submitted

This reaction sends a reminder to the approver of an expense that waits too long for a decision. It runs when `event/expense.submitted` is raised, so it considers each new expense. It supports the requirement `requirement/approval.decided-within-five-days`.

## Trigger and Effect

Each `event/expense.submitted` starts the reaction. If the expense is still undecided after the waiting time, the reaction sends `notification/spending.approval-reminder` to the approver. A decided expense is skipped.

The records state the waiting time in two places, and they do not agree. The reaction says three business days. The reminder delay setting, `setting/spending.approval-reminder-delay`, defaults to 48 hours. The records do not say which one applies.

A reminder is due only after the wait, so the reaction cannot send it when the expense is submitted.

## Failure Handling

The records do not describe retries, duplicates or dead letters for this reaction. The notification states that each waiting expense earns one reminder, with frequency once and a dedup key of `approval-reminder-{expenseId}`. The records do not say how the dedup key is checked.

## Example

Ava submits the 48.90 EUR train fare, and Ben does not decide it on the first day. Ben approves the fare on the second day, before the waiting time ends. When the waiting time ends, the reaction finds the fare decided, so it sends nothing.
