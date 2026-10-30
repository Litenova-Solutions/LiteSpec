---
{
  "type": "screen",
  "id": "screen/claimwick-app.expense-list",
  "title": "Expense List",
  "owner": "claimwick-team",
  "status": "reviewed",
  "entryPoint": "entry-point/claimwick-app",
  "address": "/expenses",
  "offers": [
    "use-case/spending.list-expenses"
  ],
  "serves": [
    "journey/submit-and-reimburse#s5"
  ],
  "floorplan": "list-page",
  "approvedBy": "claimwick-team",
  "approvedOn": "2026-10-30",
  "approvedHash": "2d7ac4f9"
}
---
# Expense List

## Summary

An employee sees their own expenses, newest first, with the amount and the latest state of each.
