# List Reimbursed Expenses

List Reimbursed Expenses gives finance a list of the expenses that were paid, with the employee each one was paid to. Finance checks the payouts against the employees with it. It matters because the list reads the reimbursed expense read model, so it shows what the spending module announced and changes nothing.

## Flow

Finance asks for the reimbursed expenses. Reporting reads `read-model/reporting.reimbursed-expense` and returns one row for each reimbursed expense, newest first. A row holds the expense identity, the employee, the amount, the currency and the reimbursement time.

The rows may lag the spending module by up to five seconds, which the read model's freshness allows. The list pages at 25 rows by default and 100 at most.

## Example

Cara, in finance, opens the list on 2026-11-12. The list shows Ava's 48.90 EUR train fare, reimbursed at 09:30, with Ava as the employee. Cara checks the row against the bank settlement report.
