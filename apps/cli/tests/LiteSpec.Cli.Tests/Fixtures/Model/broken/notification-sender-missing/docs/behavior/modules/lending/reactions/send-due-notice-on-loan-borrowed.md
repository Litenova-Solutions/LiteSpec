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
  "fulfills": [
    "requirement/borrowing.max-active-loans"
  ],
  "approvedBy": "lending-team",
  "approvedOn": "2026-10-01",
  "approvedHash": "7337e44f",
  "effects": [
    {
      "issues": "use-case/lending.return-copy",
      "timing": "async"
    }
  ]
}
---

# Send Due Notice on Loan Borrowed

Every borrowed loan earns one due notice for its reader.
