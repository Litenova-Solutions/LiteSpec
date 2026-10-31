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
  ],
  "fulfills": [
    "requirement/borrowing.loan-period-days"
  ],
  "approvedBy": "lending-team",
  "approvedOn": "2026-10-31",
  "approvedHash": "a0da3cc4"
}
---

# Nightly Due Scan

Each night the scan marks the loans that passed their due date.
