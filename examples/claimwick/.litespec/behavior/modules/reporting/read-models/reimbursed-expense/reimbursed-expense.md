# Reimbursed Expense

The reimbursed expense read model keeps one row for each expense finance paid. Reporting lists these rows, so finance sees who was paid, how much and when. The list reads only what the spending module announced, so Reporting never reads the expense tables.

## Sources

Each field takes its value from one field of an announced event: the employee, the amount, the currency and the reimbursement time come from `module-event/spending.expense-reimbursed`. The employee field is personal, so it names `module-event/spending.expense-personal-data-erased` as the event that erases it. That event clears the employee and keeps the amount and the time.

## Freshness

A reader may see a row up to five seconds after the spending module reimburses the expense. The read model is rebuilt from the announced events, so a row older than five seconds reflects the last event the read model received.

## Example

Ava's 48.90 EUR train fare is reimbursed on 2026-11-12 at 09:30. The spending module announces `module-event/spending.expense-reimbursed` with Ava as the employee, and the read model gains one row with the expense identity, Ava, 48.90, EUR and the reimbursement time.

Later, finance erases the personal data of the fare. The spending module announces `module-event/spending.expense-personal-data-erased`, and the row keeps 48.90 EUR and the time, with no employee.
