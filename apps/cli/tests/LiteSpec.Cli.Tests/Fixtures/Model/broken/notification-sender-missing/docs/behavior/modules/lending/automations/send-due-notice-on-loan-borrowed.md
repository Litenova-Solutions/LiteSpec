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
  "fulfills": [
    "requirement/borrowing.max-active-loans"
  ],
  "approvedBy": "lending-team",
  "approvedOn": "2026-10-31",
  "approvedHash": "ce3675ec",
  "effects": [
    {
      "runs": "use-case/lending.return-copy",
      "timing": "async"
    }
  ]
}
---

# Send Due Notice on Loan Borrowed

Every borrowed loan earns one due notice for its reader.
