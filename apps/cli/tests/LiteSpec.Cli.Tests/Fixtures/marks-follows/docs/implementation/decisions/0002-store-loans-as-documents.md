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
      "pros": "Loans read as one document",
      "cons": "Cross-loan queries need an index"
    }
  ],
  "reopenedWhen": "Cross-loan queries pass one hundred a day"
}
---

# Store loans as documents

## Context

Loans read as one aggregate with no joins.

## Decision

Store each loan as one document.

## Consequences

The document store owns loan persistence.
