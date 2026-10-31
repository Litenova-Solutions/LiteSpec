---
{
  "type": "automation",
  "id": "automation/lending.nightly-due-scan",
  "title": "Nightly due scan",
  "owner": "lending-team",
  "status": "reviewed",
  "triggeredBy": {
    "schedule": {
      "every": "P1D",
      "timezone": "Europe/Amsterdam"
    }
  },
  "fulfills": [
    "requirement/borrowing.loan-period-days"
  ],
  "approvedBy": "lending-team",
  "approvedOn": "2026-10-31",
  "approvedHash": "013f6b6c",
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
