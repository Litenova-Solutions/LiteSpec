---
{
  "type": "aggregate",
  "id": "aggregate/spending.expense",
  "title": "Expense",
  "owner": "claimwick-team",
  "status": "reviewed",
  "summary": "An expense is one work cost an employee submitted.",
  "kind": "entity",
  "states": [
    {
      "name": "Submitted",
      "meaning": "The expense waits for a decision."
    },
    {
      "name": "Approved",
      "meaning": "A manager approved the expense."
    },
    {
      "name": "Refused",
      "meaning": "A manager refused the expense."
    },
    {
      "name": "Reimbursed",
      "meaning": "The reimbursed state of an expense whose payout reached the submitter."
    }
  ],
  "dataClasses": [
    {
      "name": "submitter-contact",
      "classification": "personal",
      "fields": [
        "employee"
      ],
      "onErasure": "pseudonymize",
      "basis": "business",
      "period": "P7Y",
      "enforcedBy": "job/spending.erase-expired-personal-data"
    },
    {
      "name": "expense-ledger",
      "classification": "internal",
      "fields": [
        "amount",
        "receipt",
        "state"
      ],
      "onErasure": "retain",
      "basis": "statute",
      "period": "P7Y",
      "enforcedBy": "record-expiry"
    }
  ],
  "records": [
    {
      "type": "rule",
      "id": "rule/expense.receipt-required-above-threshold",
      "title": "Receipt Required Above Threshold",
      "owner": "claimwick-team",
      "status": "reviewed",
      "kind": "precondition",
      "statement": "An expense above the policy threshold needs a receipt.",
      "governs": [
        "use-case/spending.submit-expense"
      ],
      "refusal": "result/submit-expense.receipt-missing",
      "basedOn": [],
      "fulfills": [
        "requirement/approval.receipt-over-threshold"
      ],
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "240b4d9a"
    },
    {
      "type": "rule",
      "id": "rule/expense.approver-is-not-submitter",
      "title": "Approver Is Not Submitter",
      "owner": "claimwick-team",
      "status": "reviewed",
      "kind": "precondition",
      "statement": "An approver is not the submitter.",
      "governs": [
        "use-case/spending.approve-expense"
      ],
      "refusal": "result/approve-expense.own-expense",
      "basedOn": [],
      "fulfills": [
        "requirement/approval.no-self-approval"
      ],
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "8c4454b9"
    },
    {
      "type": "rule",
      "id": "rule/expense.reimbursed-once",
      "title": "Reimbursed Once",
      "owner": "claimwick-team",
      "status": "reviewed",
      "kind": "invariant",
      "statement": "An expense is reimbursed at most once.",
      "governs": [
        "use-case/spending.reimburse-expense"
      ],
      "refusal": "result/reimburse-expense.already-reimbursed",
      "basedOn": [],
      "fulfills": [
        "requirement/approval.single-reimbursement"
      ],
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "0de76b31"
    },
    {
      "type": "rule",
      "id": "rule/expense.approved-before-reimbursement",
      "title": "Approved Before Reimbursement",
      "owner": "claimwick-team",
      "status": "reviewed",
      "kind": "precondition",
      "statement": "An expense is reimbursed only after approval.",
      "governs": [
        "use-case/spending.reimburse-expense"
      ],
      "refusal": "result/reimburse-expense.not-approved",
      "basedOn": [],
      "fulfills": [
        "requirement/approval.single-reimbursement"
      ],
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "dafc1317"
    },
    {
      "type": "rule",
      "id": "rule/expense.decided-from-submitted",
      "title": "Decided from Submitted",
      "owner": "claimwick-team",
      "status": "reviewed",
      "kind": "precondition",
      "statement": "An expense is decided only from Submitted.",
      "governs": [
        "use-case/spending.approve-expense",
        "use-case/spending.refuse-expense"
      ],
      "refusal": "result/approve-expense.not-submitted",
      "basedOn": [],
      "fulfills": [
        "requirement/approval.decided-within-five-days"
      ],
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "d9e6bb11"
    },
    {
      "type": "rule",
      "id": "rule/expense.export-carries-approved",
      "title": "Export Carries Approved",
      "owner": "claimwick-team",
      "status": "reviewed",
      "kind": "precondition",
      "statement": "An export carries an approved expense.",
      "governs": [
        "use-case/spending.export-expenses"
      ],
      "refusal": "result/export-expenses.nothing-approved",
      "basedOn": [],
      "fulfills": [
        "requirement/approval.decided-within-five-days"
      ],
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "d3875025"
    },
    {
      "type": "event",
      "id": "event/expense.submitted",
      "title": "Expense Submitted",
      "owner": "claimwick-team",
      "status": "reviewed",
      "meaning": "An expense was submitted.",
      "carries": [
        "expenseId",
        "amount",
        "currency",
        "employeeId"
      ],
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "1a85c98d"
    },
    {
      "type": "event",
      "id": "event/expense.approved",
      "title": "Expense Approved",
      "owner": "claimwick-team",
      "status": "reviewed",
      "meaning": "An expense was approved.",
      "carries": [
        "expenseId",
        "approvedBy",
        "decidedAt"
      ],
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "4d4e789b"
    },
    {
      "type": "event",
      "id": "event/expense.refused",
      "title": "Expense Refused",
      "owner": "claimwick-team",
      "status": "reviewed",
      "meaning": "An expense was refused.",
      "carries": [
        "expenseId",
        "refusedBy",
        "decidedAt"
      ],
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "5f15425f"
    },
    {
      "type": "event",
      "id": "event/expense.reimbursed",
      "title": "Expense Reimbursed",
      "owner": "claimwick-team",
      "status": "reviewed",
      "meaning": "An expense was reimbursed.",
      "carries": [
        "expenseId",
        "amount",
        "currency",
        "reimbursedAt"
      ],
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "f2df7198"
    }
  ],
  "approvedBy": "claimwick-team",
  "approvedOn": "2026-10-31",
  "approvedHash": "9f19cfe1"
}
---

# Expense

An expense is one work cost an employee submitted.

## Transitions

| From | To | Triggered By |
|:--|:--|:--|
| Submitted | Approved | `use-case/spending.approve-expense` |
| Submitted | Refused | `use-case/spending.refuse-expense` |
| Approved | Reimbursed | `use-case/spending.reimburse-expense` |

## Data

| Name | Meaning | Classification | Data Class |
|:--|:--|:--|:--|
| employee | The employee who submitted the expense. | personal | submitter-contact |
| amount | The exact amount with its currency. | internal | expense-ledger |
| receipt | The proof of payment, when one is attached. | internal | expense-ledger |
| state | The lifecycle state of the expense. | internal | expense-ledger |
