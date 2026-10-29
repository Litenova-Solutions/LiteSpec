---
{
  "type": "use-case",
  "id": "use-case/lending.extend-loan",
  "title": "Extend loan",
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
    "requirement/borrowing.borrow-form"
  ],
  "records": [
    {
      "type": "result",
      "id": "result/extend-loan.extended",
      "title": "Extended",
      "owner": "lending-team",
      "outcome": "success",
      "meaning": "The loan runs longer."
    },
    {
      "type": "scenario",
      "id": "scenario/extend-loan.extending-an-active-loan-succeeds",
      "title": "Extending an active loan succeeds",
      "owner": "lending-team",
      "status": "draft",
      "decider": "lending-team",
      "decideBy": "2030-01-31",
      "given": "An active loan.",
      "when": "The reader extends the loan.",
      "then": "The due date moves out.",
      "result": "result/extend-loan.extended"
    }
  ]
}
---

# Extend loan

A reader extends an active loan.
