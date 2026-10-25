---
{
  "type": "reaction",
  "id": "reaction/spending.send-approver-reminders-on-expense-submitted",
  "title": "Send Approver Reminders on Expense Submitted",
  "owner": "claimwick-team",
  "status": "reviewed",
  "on": [
    "event/expense.submitted"
  ],
  "if": "The expense waits past three business days with no decision.",
  "effects": [
    {
      "sends": "notification/spending.approval-reminder"
    }
  ],
  "fulfills": [
    "requirement/approval.decided-within-five-days"
  ],
  "approvedBy": "claimwick-team",
  "approvedOn": "2026-10-01",
  "approvedHash": "21b6075a"
}
---

# Send Approver Reminders on Expense Submitted

A submitted expense that waits past three business days without a decision earns one reminder to its approver. Decided expenses are left alone.
