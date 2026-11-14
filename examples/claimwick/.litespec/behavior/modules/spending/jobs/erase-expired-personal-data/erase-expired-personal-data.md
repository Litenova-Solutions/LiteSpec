# Erase Expired Personal Data

Erase Expired Personal Data is a scheduled job that erases personal data once its retention period ends. It works on decided expenses, and it keeps their business facts for the tax file. It matters because personal data should not be kept longer than its retention period allows.

## Invocation

The job runs every day, in the Europe/Amsterdam time zone. The records set no clock time, so the time of day of each run is not recorded. No reason for the daily interval is recorded. The records do not say whether a run waits for an earlier run still in progress, or where a run resumes after it stops.

Each run selects the decided expenses past their retention. The job runs `use-case/spending.erase-personal-data` asynchronously, so the job does not wait for each erasure to finish.

## Failure Handling

The records do not describe what happens when a run fails. They name the runbook `runbook/clear-stuck-reimbursements`, but they do not say how that runbook applies to a failed erasure run.

The erasure use case finds nothing left to erase on a repeat call. A rerun of the job should therefore leave expenses that were already erased unchanged. The records do not test a rerun, so this is an inference.

## Example

Ava's 48.90 EUR train fare was reimbursed, and its retention is 7 years. Once the 7 years have passed, the job selects the fare on its next daily run. The job then runs the erasure, which pseudonymizes Ava's name on the expense. The 48.90 EUR amount stays on file. The records do not say from which date the 7 years count, so this example gives no run date.
