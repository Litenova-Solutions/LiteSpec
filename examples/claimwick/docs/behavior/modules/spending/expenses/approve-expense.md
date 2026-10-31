---
{
  "type": "use-case",
  "id": "use-case/spending.approve-expense",
  "title": "Approve Expense",
  "owner": "claimwick-team",
  "status": "reviewed",
  "kind": "command",
  "access": {
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
  "idempotency": {
    "mode": "none",
    "reason": "the first approval moves the expense out of the submitted state, so the aggregate refuses a second approval"
  },
  "audit": {
    "level": "none",
    "target": "no audit trail; the example stores no audit entry"
  },
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
      "title": "Own Expense",
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
      "title": "Not Submitted",
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
      "title": "Approving a Submitted Expense Succeeds",
      "owner": "claimwick-team",
      "status": "reviewed",
      "given": "An employee submitted an expense with its receipt.",
      "when": "The manager approves the expense.",
      "result": "result/approve-expense.approved",
      "then": "The expense is approved.",
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "ed71684f"
    },
    {
      "type": "scenario",
      "id": "scenario/approve-expense.approving-an-own-expense-is-refused",
      "title": "Approving an Own Expense Is Refused",
      "owner": "claimwick-team",
      "status": "reviewed",
      "given": "A manager submitted an expense with its receipt.",
      "when": "The manager approves the expense.",
      "result": "result/approve-expense.own-expense",
      "then": "The approval is refused because the approver submitted the expense.",
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "421eb8f9"
    },
    {
      "type": "scenario",
      "id": "scenario/approve-expense.approving-a-decided-expense-is-refused",
      "title": "Approving a Decided Expense Is Refused",
      "owner": "claimwick-team",
      "status": "reviewed",
      "given": "A manager approved an expense.",
      "when": "The manager approves the expense again.",
      "result": "result/approve-expense.not-submitted",
      "then": "The approval is refused because the expense is already decided.",
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "f21919e8"
    }
  ],
  "approvedBy": "claimwick-team",
  "approvedOn": "2026-10-31",
  "approvedHash": "3593159b"
}
---

# Approve Expense

## Goal

A manager decides a submitted expense so the employee learns the outcome.

## What Happens

The manager opens a submitted expense. Spending refuses the decision when the approver submitted the expense or the expense is already decided. Otherwise the expense moves to Approved and `event/expense.approved` is raised.
