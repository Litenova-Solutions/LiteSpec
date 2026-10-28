---
{
  "type": "use-case",
  "id": "use-case/lending.borrow-copy",
  "title": "Borrow copy",
  "owner": "lending-team",
  "status": "reviewed",
  "kind": "command",
  "raises": [
    "event/loan.loan-borrowed"
  ],
  "changes": [
    "aggregate/lending.loan"
  ],
  "records": [
    {
      "type": "result",
      "id": "result/borrow-copy.borrowed",
      "title": "Borrowed",
      "owner": "lending-team",
      "outcome": "success",
      "meaning": "The copy is lent."
    }
  ]
}
---

# Borrow copy

A reader borrows an available copy.
