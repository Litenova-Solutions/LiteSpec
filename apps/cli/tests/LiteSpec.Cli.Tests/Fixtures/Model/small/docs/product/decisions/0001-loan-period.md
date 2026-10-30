---
{
  "type": "decision",
  "id": "decision/0001-loan-period",
  "title": "Loan period",
  "owner": "lending-team",
  "status": "approved",
  "decidedOn": "2026-02-01",
  "options": [
    {
      "name": "14 days",
      "for": "Copies turn over fast for holds.",
      "against": "Readers rush through long books."
    },
    {
      "name": "21 days",
      "for": "Most readers finish in three weeks.",
      "against": "Holds wait a week longer."
    },
    {
      "name": "28 days",
      "for": "Slow readers keep up.",
      "against": "Popular copies sit idle."
    }
  ],
  "chosen": "21 days",
  "changeWhen": "Reader demand shifts.",
  "decides": [
    "requirement/borrowing.max-active-loans",
    "requirement/borrowing.loan-period-days",
    "requirement/borrowing.hold-pickup-window",
    "requirement/borrowing.copy-shelf-home"
  ],
  "basedOn": [],
  "approvedBy": "lending-team",
  "approvedOn": "2026-10-01",
  "approvedHash": "8218157b"
}
---

# Loan period

## Context

Readers asked for a longer loan.

## Options

Fourteen, 21, or 28 days.

## Decision

The loan lasts 21 days.

## Consequences

Holds wait longer.

## Evidence

Reader survey, January 2026.
