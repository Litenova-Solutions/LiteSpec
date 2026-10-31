---
{
  "type": "use-case",
  "id": "use-case/spending.refuse-expense",
  "title": "Refuse Expense",
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
    "event/expense.refused"
  ],
  "fulfills": [
    "requirement/approval.decided-within-five-days"
  ],
  "changes": [
    "aggregate/spending.expense"
  ],
  "idempotency": {
    "mode": "none",
    "reason": "the first refusal moves the expense out of the submitted state, so the aggregate refuses a second refusal"
  },
  "audit": {
    "level": "none",
    "target": "no audit trail; the example stores no audit entry"
  },
  "records": [
    {
      "type": "result",
      "id": "result/refuse-expense.rejected",
      "title": "Rejected",
      "owner": "claimwick-team",
      "outcome": "success",
      "meaning": "The expense is rejected.",
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "8a09db39"
    },
    {
      "type": "result",
      "id": "result/refuse-expense.not-submitted",
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
      "id": "scenario/refuse-expense.rejecting-a-submitted-expense-succeeds",
      "title": "Rejecting a Submitted Expense Succeeds",
      "owner": "claimwick-team",
      "status": "reviewed",
      "given": "An employee submitted an expense outside the policy.",
      "when": "The manager rejects the expense with a reason.",
      "result": "result/refuse-expense.rejected",
      "then": "The expense is rejected.",
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "3cec10d6"
    },
    {
      "type": "scenario",
      "id": "scenario/refuse-expense.rejecting-a-decided-expense-is-refused",
      "title": "Rejecting a Decided Expense Is Refused",
      "owner": "claimwick-team",
      "status": "reviewed",
      "given": "A manager rejected an expense.",
      "when": "The manager rejects the expense again.",
      "result": "result/refuse-expense.not-submitted",
      "then": "The rejection is refused because the expense is already decided.",
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "a127b14b"
    }
  ],
  "approvedBy": "claimwick-team",
  "approvedOn": "2026-10-31",
  "approvedHash": "b7970625"
}
---

# Refuse Expense

## Goal

A manager turns down a submitted expense that falls outside the policy.

## What Happens

The manager opens a submitted expense and gives a reason. Spending refuses the decision when the expense is already decided. Otherwise the expense moves to Refused and `event/expense.refused` is raised.
