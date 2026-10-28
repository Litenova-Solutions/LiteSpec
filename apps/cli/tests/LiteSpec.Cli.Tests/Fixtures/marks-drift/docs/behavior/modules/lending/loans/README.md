---
{
  "type": "aggregate",
  "id": "aggregate/lending.loan",
  "title": "Loan",
  "owner": "lending-team",
  "status": "reviewed",
  "states": [
    {
      "name": "Active",
      "meaning": "The loan is open."
    }
  ],
  "records": [
    {
      "type": "rule",
      "id": "rule/loan.copy-available",
      "title": "Copy available",
      "owner": "lending-team",
      "status": "reviewed",
      "kind": "precondition",
      "statement": "A borrowed copy is available.",
      "governs": [
        "use-case/lending.borrow-copy"
      ],
      "refusal": "result/borrow-copy.copy-unavailable"
    },
    {
      "type": "event",
      "id": "event/loan.loan-borrowed",
      "title": "Loan borrowed",
      "owner": "lending-team",
      "status": "reviewed",
      "meaning": "A copy was lent.",
      "carries": [
        "loanId"
      ]
    }
  ]
}
---

# Loan

A loan lends one copy to one reader.
