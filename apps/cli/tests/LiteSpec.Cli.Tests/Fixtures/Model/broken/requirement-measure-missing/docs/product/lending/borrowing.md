---
{
  "type": "topic",
  "id": "topic/borrowing",
  "title": "Borrowing",
  "owner": "lending-team",
  "status": "approved",
  "reviewBy": "2027-04-01",
  "approvedBy": "lending-team",
  "approvedOn": "2026-10-01",
  "approvedHash": "4984bc41",
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
      "cites": [
        "research/loan-period-benchmarks",
        "constraint/library-privacy-law"
      ],
      "approvedBy": "lending-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "96150778"
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
      "cites": [
        "research/loan-period-benchmarks"
      ],
      "approvedBy": "lending-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "bad36586"
    },
    {
      "type": "requirement",
      "id": "requirement/borrowing.hold-pickup-window",
      "title": "Hold pickup window",
      "owner": "lending-team",
      "status": "approved",
      "kind": "functional",
      "statement": "A ready hold waits seven days.",
      "rationale": "A week gives readers time to reach the desk before the hold lapses.",
      "cites": [
        "research/loan-period-benchmarks"
      ],
      "approvedBy": "lending-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "2ee75d8b"
    },
    {
      "type": "requirement",
      "id": "requirement/borrowing.copy-shelf-home",
      "title": "Copy shelf home",
      "owner": "lending-team",
      "status": "approved",
      "kind": "functional",
      "statement": "A copy lives on one home shelf.",
      "rationale": "One home shelf keeps every copy findable.",
      "cites": [
        "research/loan-period-benchmarks"
      ],
      "approvedBy": "lending-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "607a26c2"
    },
    {
      "type": "requirement",
      "id": "requirement/borrowing.list-loans-latency",
      "title": "List loans latency",
      "owner": "lending-team",
      "status": "approved",
      "kind": "quality",
      "statement": "Listing loans answers quickly.",
      "rationale": "Slow listings push readers back to the desk.",
      "stimulus": "Two hundred readers list their loans in the first hour of the term.",
      "environment": "Normal operation, the search index warm.",
      "response": "Each listing answers from the index.",
      "measure": "p95 latency",
      "dueBy": "under 200 ms",
      "cites": [
        "research/loan-period-benchmarks"
      ],
      "approvedBy": "lending-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "b40412ce"
    }
  ]
}
---

# Borrowing

Borrowing covers loans and holds.
