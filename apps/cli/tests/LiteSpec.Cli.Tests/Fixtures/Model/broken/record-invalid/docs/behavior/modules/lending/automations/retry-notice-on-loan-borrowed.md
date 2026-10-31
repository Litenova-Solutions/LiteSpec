---
{
  "type": "automation",
  "id": "automation/lending.retry-notice-on-loan-borrowed",
  "title": "Sloppy retry",
  "owner": "lending-team",
  "status": "draft",
  "decider": "lending-team",
  "decideBy": "2035-06-01",
  "triggeredBy": {
    "event": "event/loan.loan-borrowed"
  },
  "runs": "use-case/lending.return-copy",
  "fulfills": [
    "requirement/borrowing.loan-period-days"
  ],
  "idempotency": {
    "mode": "key",
    "key": "notice per loan",
    "scope": "branch-office"
  },
  "onFailure": "Retry a few times, then give up."
}
---

# Sloppy retry

An automation that keeps the retired edge fields, a prose key, an unknown scope, and prose failure handling.
