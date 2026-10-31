---
{
  "type": "topic",
  "id": "topic/borrowing",
  "title": "Borrowing performance",
  "owner": "lending-team",
  "status": "draft",
  "decider": "lending-team",
  "decideBy": "2030-01-31",
  "reviewBy": "2030-01-31",
  "records": [
    {
      "type": "requirement",
      "id": "requirement/borrowing.borrow-latency",
      "title": "Borrow latency",
      "kind": "quality",
      "statement": "Borrowing completes within two hundred milliseconds at p95.",
      "rationale": "Borrowing must feel instant at the desk.",
      "stimulus": "Readers borrow during the first hour of term.",
      "environment": "Normal operation with a warm catalog index.",
      "response": "Each borrow is recorded and confirmed.",
      "measure": "p95 latency of borrowing",
      "by": "2026-12-01"
    },
    {
      "type": "requirement",
      "id": "requirement/borrowing.borrow-form",
      "title": "Borrow form",
      "kind": "functional",
      "statement": "A reader borrows an available copy through the app.",
      "rationale": "Members borrow from the app without visiting the desk.",
      "cites": []
    }
  ]
}
---

# Borrowing performance

Borrowing stays fast and simple.
