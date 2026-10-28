---
{
  "type": "use-case",
  "id": "use-case/lending.borrow-copy",
  "title": "Borrow copy",
  "owner": "lending-team",
  "status": "reviewed",
  "kind": "command",
  "reachedThrough": [
    "entry-point/library-api"
  ],
  "raises": [
    "event/loan.loan-borrowed"
  ],
  "changes": [
    "aggregate/lending.loan"
  ],
  "effects": [
    {
      "issues": "use-case/lending.register-copy"
    }
  ],
  "records": [
    {
      "type": "result",
      "id": "result/borrow-copy.borrowed",
      "title": "Borrowed",
      "owner": "lending-team",
      "outcome": "success",
      "meaning": "The copy is lent."
    },
    {
      "type": "result",
      "id": "result/borrow-copy.copy-unavailable",
      "title": "Copy unavailable",
      "owner": "lending-team",
      "outcome": "refusal",
      "code": "lending.copy-not-available",
      "meaning": "The copy is unavailable."
    },
    {
      "type": "scenario",
      "id": "scenario/borrow-copy.borrowing-an-available-copy-succeeds",
      "title": "Borrowing an available copy succeeds",
      "owner": "lending-team",
      "status": "reviewed",
      "given": "An available copy.",
      "when": "The reader borrows the copy.",
      "then": "The loan is active.",
      "result": "result/borrow-copy.borrowed"
    }
  ]
}
---

# Borrow copy

A reader borrows an available copy.
