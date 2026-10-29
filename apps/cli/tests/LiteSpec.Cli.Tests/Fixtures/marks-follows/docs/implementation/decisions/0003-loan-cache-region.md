---
{
  "type": "decision",
  "id": "decision/0003-loan-cache-region",
  "title": "Loan cache region",
  "owner": "lending-team",
  "status": "draft",
  "decider": "lending-team",
  "decideBy": "2030-01-31",
  "options": [
    {
      "name": "One region",
      "for": "Eviction stays simple",
      "against": "Hot loans evict cold ones"
    }
  ],
  "changeWhen": "Cache misses pass one percent"
}
---

# Loan cache region

## Context

Loan reads need a cache before the document store grows.

## Decision

Cache loans in one region.

## Consequences

No code follows this decision yet.
