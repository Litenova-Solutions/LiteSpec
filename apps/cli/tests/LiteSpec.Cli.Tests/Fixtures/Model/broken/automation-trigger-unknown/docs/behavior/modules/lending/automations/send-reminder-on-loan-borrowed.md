---
{
  "type": "automation",
  "id": "automation/lending.send-reminder-on-loan-borrowed",
  "title": "Remind overdue",
  "owner": "lending-team",
  "status": "reviewed",
  "triggeredBy": {
    "event": "event/loan.no-such-event"
  },
  "effects": [
    {
      "runs": "use-case/lending.return-copy",
      "timing": "async"
    }
  ],
  "fulfills": [
    "requirement/borrowing.loan-period-days"
  ],
  "approvedBy": "lending-team",
  "approvedOn": "2026-10-31",
  "approvedHash": "f84fbb9f"
}
---

# Remind overdue

Readers with overdue loans get a reminder.
