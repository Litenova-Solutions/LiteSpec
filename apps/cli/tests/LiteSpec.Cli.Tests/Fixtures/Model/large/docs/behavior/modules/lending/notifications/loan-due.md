---
{
  "type": "notification",
  "id": "notification/lending.loan-due",
  "title": "Loan due",
  "owner": "lending-team",
  "status": "reviewed",
  "template": "loan-due-reader",
  "audience": [
    "actor/reader"
  ],
  "channel": [
    "email"
  ],
  "sender": "The library desk",
  "consent": "Readers accept notices at signup.",
  "frequency": "once",
  "fulfills": [
    "requirement/borrowing.max-active-loans"
  ],
  "approvedBy": "lending-team",
  "approvedOn": "2026-10-01",
  "approvedHash": "2ab61ded"
}
---

# Loan Due

The reader learns when the loan ends.
