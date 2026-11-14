# Retention Schedule

The retention schedule sets how long Claimwick keeps the personal data of each expense, and why. A reader who has read nothing else should see that expense data is kept for seven years because the tax law requires it, and that an erasure request waits until then.

## Basis

The expense row keeps its data for seven years under a statute: national tax law requires a company to keep its business receipts for seven years, as `constraint/tax-receipt-retention` records. No business or engineering basis applies to a row in this schedule.

## Conflicts

An erasure request for an expense inside its statutory period waits until the period ends, because the tax law sets the period.

## Deletion

The job `job/spending.erase-expired-personal-data` runs every day and erases the personal data of each decided expense past its seven years. The job names `runbook/clear-stuck-reimbursements` as its runbook.
