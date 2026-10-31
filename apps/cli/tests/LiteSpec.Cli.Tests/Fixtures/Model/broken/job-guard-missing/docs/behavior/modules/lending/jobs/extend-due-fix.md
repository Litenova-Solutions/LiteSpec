---
{
  "type": "job",
  "id": "job/lending.extend-due-fix",
  "title": "Manual due fix",
  "owner": "lending-team",
  "status": "reviewed",
  "kind": "manual",
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
  "approvedHash": "52ea8e50"
}
---

# Manual due fix

A librarian fixes a due date by hand.
