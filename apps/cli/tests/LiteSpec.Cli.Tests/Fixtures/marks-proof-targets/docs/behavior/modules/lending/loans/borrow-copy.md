---
{
  "type": "use-case",
  "id": "use-case/lending.borrow-copy",
  "title": "Borrow copy",
  "owner": "lending-team",
  "status": "draft",
  "decider": "lending-team",
  "decideBy": "2030-01-31",
  "kind": "command",
  "access": {
    "auth": [
      "session"
    ],
    "grant": "permission/borrow-copy",
    "tenant": "own"
  },
  "fulfills": [
    "requirement/borrowing.borrow-latency"
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
      "type": "scenario",
      "id": "scenario/borrow-copy.borrowing-an-available-copy-succeeds",
      "title": "Borrowing an available copy succeeds",
      "owner": "lending-team",
      "status": "draft",
      "decider": "lending-team",
      "decideBy": "2030-01-31",
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
