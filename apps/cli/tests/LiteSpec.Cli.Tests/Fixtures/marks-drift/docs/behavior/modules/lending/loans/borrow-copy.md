---
{
  "type": "use-case",
  "id": "use-case/lending.borrow-copy",
  "title": "Borrow copy",
  "owner": "lending-team",
  "status": "reviewed",
  "kind": "command",
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
    }
  ]
}
---

# Borrow copy

A reader borrows an available copy.
