---
{
  "type": "automation",
  "id": "automation/lending.send-due-scan",
  "title": "Daily due scan",
  "owner": "lending-team",
  "status": "reviewed",
  "triggeredBy": {
    "schedule": {
      "timezone": "Europe/Amsterdam"
    }
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
  "approvedHash": "0bdf2c99"
}
---

# Daily due scan

Each day the scan marks the loans that passed their due date.
