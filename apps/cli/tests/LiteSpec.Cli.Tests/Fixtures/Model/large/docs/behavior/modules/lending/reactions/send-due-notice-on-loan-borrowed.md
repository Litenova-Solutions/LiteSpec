---
{
  "type": "reaction",
  "id": "reaction/lending.send-due-notice-on-loan-borrowed",
  "title": "Send loan due notice",
  "owner": "lending-team",
  "status": "reviewed",
  "on": [
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
  "approvedOn": "2026-10-01",
  "approvedHash": "a551aff9"
}
---

# Send Due Notice on Loan Borrowed

Every borrowed loan earns one due notice for its reader.
