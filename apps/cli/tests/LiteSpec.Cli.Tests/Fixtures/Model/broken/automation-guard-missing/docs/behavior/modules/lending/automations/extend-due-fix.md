---
{
  "type": "automation",
  "id": "automation/lending.extend-due-fix",
  "title": "Manual due fix",
  "owner": "lending-team",
  "status": "reviewed",
  "triggeredBy": {
    "manual": true
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
  "approvedHash": "df6cf477"
}
---

# Manual due fix

A librarian fixes a due date by hand.
