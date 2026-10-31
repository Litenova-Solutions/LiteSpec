---
{
  "type": "reaction",
  "id": "reaction/spending.send-approver-reminders-on-expense-submitted",
  "title": "Send Approver Reminders on Expense Submitted",
  "owner": "claimwick-team",
  "status": "reviewed",
  "triggeredBy": [
    "event/expense.submitted"
  ],
  "skippedWhen": "The expense waits past three business days with no decision.",
  "effects": [
    {
      "sends": "notification/spending.approval-reminder"
    }
  ],
  "fulfills": [
    "requirement/approval.decided-within-five-days"
  ],
  "approvedBy": "claimwick-team",
  "approvedOn": "2026-10-31",
  "approvedHash": "d21003d7"
}
---

# Send Approver Reminders on Expense Submitted

A submitted expense that waits past three business days without a decision earns one reminder to its approver. Decided expenses are left alone.
