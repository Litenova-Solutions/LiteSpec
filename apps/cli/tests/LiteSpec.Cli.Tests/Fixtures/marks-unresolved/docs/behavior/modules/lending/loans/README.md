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
      ]
    }
  ]
}
---

# Loan

A loan lends one copy to one reader.
