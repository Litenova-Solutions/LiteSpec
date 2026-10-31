---
{
  "type": "notification",
  "id": "notification/spending.approval-reminder",
  "title": "Approval Reminder",
  "owner": "claimwick-team",
  "status": "reviewed",
  "template": "approval-reminder-manager",
  "addresses": [
    "actor/manager"
  ],
  "channel": [
    "email"
  ],
  "consent": "Managers accept reminders with the approver role.",
  "frequency": "once",
  "dedupKey": "approval-reminder-{expenseId}",
  "params": [
    "submitterName"
  ],
  "locale": "English only; the template ships one language.",
  "fulfills": [
    "requirement/approval.decided-within-five-days"
  ],
  "approvedBy": "claimwick-team",
  "approvedOn": "2026-10-31",
  "approvedHash": "e0cc7502"
}
---

# Approval Reminder

The reminder names the expense and its waiting time. Each waiting expense earns one reminder.
