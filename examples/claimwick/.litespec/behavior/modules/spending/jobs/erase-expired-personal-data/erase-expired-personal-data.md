# Erase Expired Personal Data

Erase Expired Personal Data is a scheduled job that erases personal data once its retention period ends. It works on decided expenses, and it keeps their business facts for the tax file. It matters because personal data should not be kept longer than its retention period allows.

## Invocation

The job runs every day, in the Europe/Amsterdam time zone. The records set no clock time, so the time of day of each run is not recorded. No reason for the daily interval is recorded. The records do not say whether a run waits for an earlier run still in progress, or where a run resumes after it stops.

Each run selects the decided expenses past their retention. The job sends `use-case/spending.erase-personal-data` once for each selected expense, asynchronously, so the job does not wait for each erasure to finish.

## Failure Handling

A failed run retries up to three times with a growing delay, and a failure past the last retry dead-letters the selection for one day. The next nightly run then selects the same expenses again.

The erasure use case finds nothing left to erase on a repeat call, and an erased expense shows the Erased state. A rerun of the job therefore leaves expenses that were already erased unchanged: it recognizes them by state.

## Example

Ava's 48.90 EUR train fare was reimbursed on 2026-11-12, and its retention is 7 years counted from the reimbursement. The retention ends on 2033-11-12, so the job selects the fare on its first daily run past that date. The job then runs the erasure, which pseudonymizes Ava's name on the expense. The 48.90 EUR amount stays on file.
