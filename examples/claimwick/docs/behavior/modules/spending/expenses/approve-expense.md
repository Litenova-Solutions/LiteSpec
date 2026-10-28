---
{
  "type": "use-case",
  "id": "use-case/spending.approve-expense",
  "title": "Approve Expense",
  "owner": "claimwick-team",
  "status": "reviewed",
  "kind": "command",
  "access": {
    "auth": [
      "session"
    ],
    "grant": "permission/approve-expense",
    "tenant": "own"
  },
  "serves": [
    "journey/submit-and-reimburse#s3"
  ],
  "reachedThrough": [
    "entry-point/claimwick-app"
  ],
  "raises": [
    "event/expense.approved"
  ],
  "fulfills": [
    "requirement/approval.no-self-approval"
  ],
  "changes": [
    "aggregate/spending.expense"
  ],
  "records": [
    {
      "type": "result",
      "id": "result/approve-expense.approved",
      "title": "Approved",
      "owner": "claimwick-team",
      "outcome": "success",
      "meaning": "The expense is approved.",
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "28c796e6"
    },
    {
      "type": "result",
      "id": "result/approve-expense.own-expense",
      "title": "Own expense",
      "owner": "claimwick-team",
      "outcome": "refusal",
      "code": "spending.own-expense",
      "meaning": "The approver submitted the expense.",
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "a201dcbc"
    },
    {
      "type": "result",
      "id": "result/approve-expense.not-submitted",
      "title": "Not submitted",
      "owner": "claimwick-team",
      "outcome": "refusal",
      "code": "spending.not-submitted",
      "meaning": "The expense is already decided.",
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "b2983ba6"
    },
    {
      "type": "scenario",
      "id": "scenario/approve-expense.approving-a-submitted-expense-succeeds",
      "title": "Approving a submitted expense succeeds",
      "owner": "claimwick-team",
      "status": "reviewed",
      "given": "An employee submitted an expense with its receipt.",
      "when": "The manager approves the expense.",
      "result": "result/approve-expense.approved",
      "then": "The expense is approved.",
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "ed71684f"
    }
  ],
  "approvedBy": "claimwick-team",
  "approvedOn": "2026-10-01",
  "approvedHash": "50f1f977"
}
---

# Approve Expense

## Goal

A manager decides a submitted expense so the employee learns the outcome.

## What Happens

The manager opens a submitted expense. Spending refuses the decision when the approver submitted the expense or the expense is already decided. Otherwise the expense moves to Approved and `event/expense.approved` is raised.
