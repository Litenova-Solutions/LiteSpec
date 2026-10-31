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
  "fulfills": [
    "requirement/borrowing.loan-period-days"
  ],
  "approvedBy": "lending-team",
  "approvedOn": "2026-10-31",
  "approvedHash": "68cdc656",
  "effects": [
    {
      "runs": "use-case/lending.return-copy",
      "timing": "async"
    }
  ]
}
---

# Nightly Due Scan

Each night the scan marks the loans that passed their due date.
