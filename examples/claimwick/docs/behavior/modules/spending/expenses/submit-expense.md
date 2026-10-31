---
{
  "type": "use-case",
  "id": "use-case/spending.submit-expense",
  "title": "Submit Expense",
  "owner": "claimwick-team",
  "status": "reviewed",
  "kind": "command",
  "access": {
    "auth": [
      "session"
    ],
    "grant": "permission/submit-expense",
    "tenant": "own"
  },
  "serves": [
    "journey/submit-and-reimburse#s1"
  ],
  "reachedThrough": [
    "entry-point/claimwick-app"
  ],
  "raises": [
    "event/expense.submitted"
  ],
  "fulfills": [
    "requirement/approval.receipt-over-threshold"
  ],
  "input": [
    {
      "name": "amount",
      "meaning": "The exact amount of the expense."
    },
    {
      "name": "currency",
      "meaning": "The ISO 4217 currency code of the amount."
    },
    {
      "name": "receipt",
      "meaning": "The proof of payment, required above the threshold."
    }
  ],
  "changes": [
    "aggregate/spending.expense"
  ],
  "reads": [
    "aggregate/spending.policy"
  ],
  "idempotency": {
    "mode": "none",
    "reason": "the endpoint mints a fresh expense identity for each request, so each call submits its own expense"
  },
  "audit": {
    "level": "none",
    "target": "no audit trail; the example stores no audit entry"
  },
  "records": [
    {
      "type": "result",
      "id": "result/submit-expense.submitted",
      "title": "Submitted",
      "owner": "claimwick-team",
      "outcome": "success",
      "meaning": "The expense is submitted.",
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "95ade09a"
    },
    {
      "type": "result",
      "id": "result/submit-expense.receipt-missing",
      "title": "Receipt Missing",
      "owner": "claimwick-team",
      "outcome": "refusal",
      "code": "spending.receipt-missing",
      "meaning": "The expense is above the receipt threshold and carries no receipt.",
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "91419801"
    },
    {
      "type": "scenario",
      "id": "scenario/submit-expense.submitting-with-a-receipt-succeeds",
      "title": "Submitting with a Receipt Succeeds",
      "owner": "claimwick-team",
      "status": "reviewed",
      "given": "An employee paid for a train ticket and kept the receipt.",
      "when": "The employee submits the expense with the receipt.",
      "result": "result/submit-expense.submitted",
      "then": "The expense is submitted.",
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "c583910f"
    },
    {
      "type": "scenario",
      "id": "scenario/submit-expense.submitting-without-a-receipt-is-refused",
      "title": "Submitting Without a Receipt Is Refused",
      "owner": "claimwick-team",
      "status": "reviewed",
      "given": "An employee paid for a train ticket and lost the receipt.",
      "when": "The employee submits the expense with no receipt.",
      "result": "result/submit-expense.receipt-missing",
      "then": "The submission is refused because the expense is above the receipt threshold and carries no receipt.",
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "07e36afb"
    }
  ],
  "approvedBy": "claimwick-team",
  "approvedOn": "2026-10-31",
  "approvedHash": "7fa3b83e"
}
---

# Submit Expense

## Goal

An employee files a work expense so it can be decided and reimbursed.

## What Happens

The employee sends the amount with its currency and the receipt, if one is attached. Spending checks the receipt rule against the policy threshold. An expense above the threshold with no receipt is refused with `spending.receipt-missing`. Otherwise the expense is stored as Submitted and `event/expense.submitted` is raised.

## Example

Ava takes the train to a client visit and keeps the ticket receipt. Back at the office she submits the 48.90 EUR fare with the receipt attached. The expense waits for Ben to decide.
