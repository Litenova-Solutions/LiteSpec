---
{
  "type": "decision",
  "id": "decision/0002-store-loans-as-documents",
  "title": "Store loans as documents",
  "owner": "lending-team",
  "status": "draft",
  "decider": "lending-team",
  "decideBy": "2030-01-31",
  "options": [
    {
      "name": "Documents",
      "pros": "Reads stay simple",
      "cons": "Queries need an index"
    }
  ],
  "reopenedWhen": "Queries pass one hundred a day",
  "basedOn": [
    "requirement/borrowing.borrow-latency"
  ]
}
---

# Store loans as documents

## Context

Loans read as one aggregate with no joins.

## Decision

Store each loan as one document.

## Consequences

The document store owns loan persistence.
