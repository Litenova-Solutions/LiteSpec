---
{
  "type": "decision",
  "id": "decision/0001-loan-threshold",
  "title": "Loan threshold",
  "owner": "lending-team",
  "status": "draft",
  "decider": "lending-team",
  "decideBy": "2030-01-31",
  "options": [
    {
      "name": "Five loans",
      "pros": "Readers borrow enough for a term",
      "cons": "Popular copies wait longer"
    }
  ],
  "reopenedWhen": "Holds pass one hundred a day",
  "decides": [
    "requirement/borrowing.borrow-form"
  ]
}
---

# Loan threshold

## Context

Readers ask how many loans one card holds.

## Decision

One card holds five active loans.

## Consequences

The borrow form refuses a sixth loan.
