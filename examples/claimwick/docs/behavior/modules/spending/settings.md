---
{
  "type": "setting-set",
  "title": "Settings",
  "owner": "claimwick-team",
  "status": "reviewed",
  "records": [
    {
      "type": "setting",
      "id": "setting/spending.receipt-threshold",
      "title": "Receipt Threshold",
      "owner": "claimwick-team",
      "status": "reviewed",
      "valueType": "money",
      "default": "EUR 25",
      "range": "EUR 0 to EUR 500",
      "unit": "EUR",
      "setBy": "decider",
      "atBound": "refuse",
      "fulfills": [
        "requirement/approval.receipt-over-threshold"
      ],
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "86fda04a"
    },
    {
      "type": "setting",
      "id": "setting/spending.approval-reminder-delay",
      "title": "Approval Reminder Delay",
      "owner": "claimwick-team",
      "status": "reviewed",
      "valueType": "duration",
      "default": "48 hours",
      "range": "1-120 hours",
      "unit": "hours",
      "setBy": "operator",
      "key": "Spending:ApprovalReminderDelay",
      "atBound": "refuse",
      "fulfills": [
        "requirement/approval.decided-within-five-days"
      ],
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "14ab7fbc"
    }
  ]
}
---

# Settings

Spending runs within these settings.
