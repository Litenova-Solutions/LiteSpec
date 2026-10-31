---
{
  "type": "use-case",
  "id": "use-case/spending.reimburse-expense",
  "title": "Reimburse Expense",
  "owner": "claimwick-team",
  "status": "reviewed",
  "kind": "command",
  "access": {
    "auth": [
      "session"
    ],
    "grant": "permission/reimburse-expense",
    "tenant": "own"
  },
  "serves": [
    "journey/submit-and-reimburse#s4"
  ],
  "reachedThrough": [
    "entry-point/claimwick-app"
  ],
  "raises": [
    "event/expense.reimbursed"
  ],
  "fulfills": [
    "requirement/approval.single-reimbursement"
  ],
  "changes": [
    "aggregate/spending.expense"
  ],
  "risk": [
    "money"
  ],
  "records": [
    {
      "type": "result",
      "id": "result/reimburse-expense.reimbursed",
      "title": "Reimbursed",
      "owner": "claimwick-team",
      "outcome": "success",
      "meaning": "The expense is reimbursed.",
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "bab7b447"
    },
    {
      "type": "result",
      "id": "result/reimburse-expense.not-approved",
      "title": "Not Approved",
      "owner": "claimwick-team",
      "outcome": "refusal",
      "code": "spending.not-approved",
      "meaning": "The expense is not approved.",
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "f9b836a8"
    },
    {
      "type": "result",
      "id": "result/reimburse-expense.already-reimbursed",
      "title": "Already Reimbursed",
      "owner": "claimwick-team",
      "outcome": "refusal",
      "code": "spending.already-reimbursed",
      "meaning": "The expense is already reimbursed.",
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "46b8e152"
    },
    {
      "type": "scenario",
      "id": "scenario/reimburse-expense.reimbursing-an-approved-expense-succeeds",
      "title": "Reimbursing an Approved Expense Succeeds",
      "owner": "claimwick-team",
      "status": "reviewed",
      "given": "A manager approved an expense.",
      "when": "Finance reimburses the expense.",
      "result": "result/reimburse-expense.reimbursed",
      "then": "The expense is reimbursed.",
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "46883c3c"
    },
    {
      "type": "scenario",
      "id": "scenario/reimburse-expense.reimbursing-an-unapproved-expense-is-refused",
      "title": "Reimbursing an Unapproved Expense Is Refused",
      "owner": "claimwick-team",
      "status": "reviewed",
      "given": "An employee submitted an expense with its receipt.",
      "when": "Finance reimburses the expense.",
      "result": "result/reimburse-expense.not-approved",
      "then": "The reimbursement is refused because the expense is not approved.",
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "693b4f85"
    },
    {
      "type": "scenario",
      "id": "scenario/reimburse-expense.reimbursing-a-reimbursed-expense-is-refused",
      "title": "Reimbursing a Reimbursed Expense Is Refused",
      "owner": "claimwick-team",
      "status": "reviewed",
      "given": "Finance reimbursed an expense.",
      "when": "Finance reimburses the expense again.",
      "result": "result/reimburse-expense.already-reimbursed",
      "then": "The reimbursement is refused because the expense is already reimbursed.",
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "806626e6"
    }
  ],
  "approvedBy": "claimwick-team",
  "approvedOn": "2026-10-01",
  "approvedHash": "9096a3ce"
}
---

# Reimburse Expense

## Goal

Finance pays an approved expense so the employee gets the money back.

## What Happens

Finance picks an approved expense. Spending refuses the payment when the expense is not approved or is already reimbursed. Otherwise the expense moves to Reimbursed, `event/expense.reimbursed` is raised, and the payout batch carries it to `external-system/payout-bank`.

## Risk

Money moves here: a double payment reaches the employee twice. The `rule/expense.reimbursed-once` rule guards it, and the payout bank settles each batch once.
