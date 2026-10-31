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
      "statement": "A reader holds at most five active loans.",
      "rationale": "Readers borrow enough for a term without starving holds.",
      "cites": []
    },
    {
      "type": "requirement",
      "id": "requirement/borrowing.loan-period-days",
      "title": "Loan period days",
      "owner": "lending-team",
      "status": "approved",
      "kind": "functional",
      "statement": "A loan lasts 21 days.",
      "rationale": "Three weeks lets most readers finish while copies keep moving.",
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
      "measure": "p95 latency",
      "by": "under 200 ms",
      "cites": []
    },
    {
      "type": "requirement",
      "id": "requirement/borrowing.quiet-hours",
      "title": "Quiet hours",
      "owner": "lending-team",
      "status": "draft",
      "decider": "lending-team",
      "decideBy": "2027-01-01",
      "kind": "functional",
      "statement": "The library keeps quiet hours.",
      "rationale": "Readers need guaranteed quiet time to study.",
      "cites": []
    }
  ]
}
---

# Borrowing

Borrowing covers loans and holds.
