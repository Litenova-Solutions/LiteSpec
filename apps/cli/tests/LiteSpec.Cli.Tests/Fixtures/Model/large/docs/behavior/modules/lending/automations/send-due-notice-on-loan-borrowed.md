---
{
  "type": "automation",
  "id": "automation/lending.send-due-notice-on-loan-borrowed",
  "title": "Send loan due notice",
  "owner": "lending-team",
  "status": "reviewed",
  "triggeredBy": {
    "event": "event/loan.loan-borrowed"
  },
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
  "approvedHash": "350c2dcf"
}
---

# Send Due Notice on Loan Borrowed

Every borrowed loan earns one due notice for its reader.
