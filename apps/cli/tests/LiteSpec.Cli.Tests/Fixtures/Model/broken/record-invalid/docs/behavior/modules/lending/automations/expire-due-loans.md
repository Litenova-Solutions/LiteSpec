---
{
  "type": "automation",
  "id": "automation/lending.expire-due-loans",
  "title": "Sloppy sweep",
  "owner": "lending-team",
  "status": "draft",
  "decider": "lending-team",
  "decideBy": "2035-06-01",
  "triggeredBy": {
    "schedule": {
      "every": "P1D"
    }
  },
  "runs": [
    "use-case/lending.return-copy"
  ],
  "fulfills": [
    "requirement/borrowing.loan-period-days"
  ],
  "onFailure": {
    "mode": "retry",
    "retry": {
      "maxAttempts": 10,
      "backoff": "5 seconds"
    },
    "then": "dead-letter"
  }
}
---

# Sloppy sweep

An automation that keeps the retired edge field and states its delays in prose instead of ISO 8601 durations.
