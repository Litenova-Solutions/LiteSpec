---
{
  "type": "topic",
  "id": "topic/borrowing",
  "title": "Borrowing",
  "owner": "lending-team",
  "status": "approved",
  "reviewBy": "2027-04-01",
  "records": [
    {
      "type": "requirement",
      "id": "requirement/borrowing.max-active-loans",
      "title": "Max active loans",
      "owner": "lending-team",
      "status": "approved",
      "kind": "functional",
      "statement": "A reader holds at most three active loans.",
      "rationale": "Readers borrow enough for a term without starving holds.",
      "cites": []
    },
    {
      "type": "requirement",
      "id": "requirement/borrowing.search-speed",
      "title": "Search speed",
      "owner": "lending-team",
      "status": "approved",
      "kind": "quality",
      "statement": "Search answers quickly.",
      "rationale": "Slow search sends readers to the desk queue.",
      "measure": "p99 latency",
      "by": "under 200 ms",
      "cites": []
    }
  ]
}
---

# Borrowing

Borrowing covers loans and holds.
