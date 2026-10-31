---
{
  "type": "job",
  "id": "job/lending.nightly-due-scan",
  "title": "Nightly due scan",
  "owner": "lending-team",
  "status": "reviewed",
  "kind": "scheduled",
  "schedule": {
    "every": "P1D",
    "timezone": "Europe/Amsterdam"
  },
  "effects": [
    {
      "sends": "notification/lending.loan-due"
    }
  ]
}
---

# Nightly Due Scan

Each night the scan marks the loans that passed their due date.
