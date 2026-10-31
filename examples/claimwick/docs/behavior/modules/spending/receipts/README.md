---
{
  "type": "aggregate",
  "id": "aggregate/spending.receipt",
  "title": "Receipt",
  "owner": "claimwick-team",
  "status": "reviewed",
  "summary": "A receipt is proof of payment attached to an expense.",
  "kind": "entity",
  "states": [
    {
      "name": "Attached",
      "meaning": "The receipt sits on the expense."
    },
    {
      "name": "Removed",
      "meaning": "The receipt left the expense."
    }
  ],
  "records": [
    {
      "type": "rule",
      "id": "rule/receipt.attaches-before-decision",
      "title": "Attaches Before Decision",
      "owner": "claimwick-team",
      "status": "reviewed",
      "kind": "precondition",
      "statement": "A receipt attaches before the expense is decided.",
      "governs": [
        "use-case/spending.attach-receipt"
      ],
      "refusal": "result/attach-receipt.expense-decided",
      "basedOn": [],
      "fulfills": [
        "requirement/approval.receipt-over-threshold"
      ],
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "98084cc5"
    },
    {
      "type": "event",
      "id": "event/receipt.attached",
      "title": "Receipt Attached",
      "owner": "claimwick-team",
      "status": "reviewed",
      "meaning": "A receipt was attached.",
      "carries": [
        "receiptId",
        "expenseId"
      ],
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "904eea77"
    }
  ],
  "approvedBy": "claimwick-team",
  "approvedOn": "2026-10-31",
  "approvedHash": "55f94df7"
}
---

# Receipt

A receipt is proof of payment attached to an expense.

## Transitions

None.

## Data

None.
