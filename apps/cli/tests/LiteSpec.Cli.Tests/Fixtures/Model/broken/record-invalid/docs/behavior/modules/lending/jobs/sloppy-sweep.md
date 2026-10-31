---
{
  "type": "job",
  "id": "job/lending.sloppy-sweep",
  "title": "Sloppy sweep",
  "owner": "lending-team",
  "status": "draft",
  "decider": "lending-team",
  "decideBy": "2035-06-01",
  "kind": "one-off",
  "issues": [
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

A job that keeps the retired edge field and states its delays in prose instead of ISO 8601 durations.
