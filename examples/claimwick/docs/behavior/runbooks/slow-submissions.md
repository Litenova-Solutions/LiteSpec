---
{
  "type": "runbook",
  "id": "runbook/slow-submissions",
  "title": "Slow Submissions",
  "owner": "claimwick-team",
  "status": "draft",
  "decider": "claimwick-team",
  "decideBy": "2027-02-01",
  "trigger": "The submission-acknowledgement-slow alert fires.",
  "severity": "high"
}
---

# Slow Submissions

## Impact

Submissions queue past the promised two seconds, and month-end filers wait on acknowledgements.

## Prerequisites

Access to the submission dashboard and the acknowledgement queue.

## Procedure

List the unacknowledged submissions. Restart the stuck acknowledgement worker. Notify finance of the batch that still stands.

## Verification

New submissions acknowledge within two seconds again.

## Rollback or Recovery

A submission acknowledged twice stays single: the submission id deduplicates the retry.

## Escalation

When the queue still stands after one retry round, the on-call engineer pages the backend owner and freezes new deploys.
