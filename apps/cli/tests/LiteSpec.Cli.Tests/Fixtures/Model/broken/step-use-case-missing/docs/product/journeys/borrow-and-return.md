---
{
  "type": "journey",
  "id": "journey/borrow-and-return",
  "title": "Borrow and return",
  "owner": "lending-team",
  "status": "approved",
  "actor": "actor/reader",
  "serves": [
    "goal/faster-pickup"
  ],
  "finishedWhen": "The reader holds the copy, then the copy sits back on its shelf.",
  "steps": [
    {
      "id": "s1",
      "statement": "The librarian registers the copy on its home shelf.",
      "actor": "actor/librarian"
    },
    {
      "id": "s2",
      "statement": "The reader borrows the copy from the shelf.",
      "actor": "actor/reader",
      "failure": "The copy is lent out, so the reader places a hold instead."
    },
    {
      "id": "s3",
      "statement": "The reader renews the loan before it ends.",
      "actor": "actor/reader",
      "failure": "The loan already ended, so the reader returns the copy late."
    },
    {
      "id": "s4",
      "statement": "The reader reviews their loans and due dates.",
      "actor": "actor/reader"
    },
    {
      "id": "s5",
      "statement": "The reader returns the copy to the desk.",
      "actor": "actor/reader",
      "failure": "The copy is damaged, so the librarian retires it."
    },
    {
      "id": "s6",
      "statement": "The reader places a hold on a lent copy.",
      "actor": "actor/reader"
    },
    {
      "id": "s7",
      "statement": "The reader cancels a hold they no longer need.",
      "actor": "actor/reader"
    },
    {
      "id": "s8",
      "statement": "The librarian retires a worn copy.",
      "actor": "actor/librarian"
    },
    {
      "id": "s9",
      "statement": "The reader donates a book to the desk."
    }
  ],
  "cites": [
    "requirement/borrowing.max-active-loans"
  ],
  "approvedBy": "lending-team",
  "approvedOn": "2026-10-01",
  "approvedHash": "ee1317d7"
}
---

# Borrow and return

A reader borrows a copy and returns it.
