---
{
  "type": "reaction",
  "id": "reaction/lending.send-due-notice-on-loan-borrowed",
  "title": "Send loan due notice",
  "owner": "lending-team",
  "status": "reviewed",
  "triggeredBy": [
    "event/loan.loan-borrowed"
  ],
  "effects": [
    {
      "sends": "notification/lending.loan-due"
    }
  ],
  "fulfills": [
    "requirement/borrowing.max-active-loans"
  ],
  "approvedBy": "lending-team",
  "approvedOn": "2026-10-31",
  "approvedHash": "d64bdd8b"
}
---

# Send Due Notice on Loan Borrowed

Every borrowed loan earns one due notice for its reader.
