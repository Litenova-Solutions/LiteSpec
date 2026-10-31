---
{
  "type": "job",
  "id": "job/lending.send-due-scan",
  "title": "Daily due scan",
  "owner": "lending-team",
  "status": "reviewed",
  "kind": "scheduled",
  "schedule": {
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
  "approvedHash": "78787369"
}
---

# Daily due scan

Each day the scan marks the loans that passed their due date.
