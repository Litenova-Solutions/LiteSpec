# Reporting

Reporting is the Claimwick module that lists what the business has paid out. It keeps its own copy of each reimbursed expense, built from the events that the spending module announces. Finance depends on this module to check each payout against the employee it was paid to.

## Responsibilities

The module keeps one row for each reimbursed expense. It reads the reimbursement and the erasure events from the spending module, and it lists the rows for finance.

## Boundaries

The module reads the spending module only through the events that spending announces. It does not read the expense tables of the spending module, and it changes no expense.

## Example

Cara, in finance, lists the reimbursed expenses of the week. Ava's 48.90 EUR train fare appears with Ava as the employee, because the spending module announced the reimbursement with her identifier. After the erasure of the fare's personal data, the row keeps the amount and the time, and the employee is empty.
