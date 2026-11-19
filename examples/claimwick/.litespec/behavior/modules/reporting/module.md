# Reporting

Reporting is the Claimwick module that lists what the business paid out. It keeps its own copy of each reimbursed expense, built from the events the spending module announces. Finance depends on this module to check each payout against the employee it was paid to.

## Covers

- Reporting keeps one row for each reimbursed expense.
- Reporting reads reimbursement and erasure events from the spending module.
- Reporting confirms each listed expense still exists in the spending module.
- Reporting lists the rows for finance.

## Boundaries

- Reporting reads the spending module only through announced events and the public read-expense query.
- Reporting does not read the expense tables of the spending module.
- Reporting changes no expense.

## Example

Cara, in finance, lists the reimbursed expenses of the week. Ava's 48.90 EUR train fare appears with Ava as the employee, because the spending module announced the reimbursement with her identifier. After the fare's personal data is erased, the row keeps the amount and the time, and the employee is empty.
