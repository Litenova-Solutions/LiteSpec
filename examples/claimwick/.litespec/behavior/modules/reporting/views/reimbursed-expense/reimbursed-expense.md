# Reimbursed Expense

The reimbursed expense view keeps one row for each expense that finance paid. Reporting lists these rows, so finance sees who was paid, how much and when. It matters because the list reads only what the spending module announced, so Reporting never reads the expense tables.

## Sources

Each field takes its value from one field of an announced event. The employee, the amount, the currency and the reimbursement time come from `integration-event/expense-reimbursed`. The employee field is personal, so it names `integration-event/expense-personal-data-erased` as the event that erases it. That event clears the employee and keeps the amount and the time.

## Freshness

A reader may see a row up to five seconds after the spending module reimburses the expense. The view is rebuilt from the announced events, so a row that is older than five seconds reflects the last event the view received.

## Example

Ava's 48.90 EUR train fare is reimbursed on 2026-11-12 at 09:30. The spending module announces `integration-event/expense-reimbursed` with Ava as the employee. The view gets one row with the expense identity, Ava, 48.90, EUR and the reimbursement time.

Later, finance erases the personal data of the fare. The spending module announces `integration-event/expense-personal-data-erased`, and the row keeps 48.90 EUR and the time, with no employee.
