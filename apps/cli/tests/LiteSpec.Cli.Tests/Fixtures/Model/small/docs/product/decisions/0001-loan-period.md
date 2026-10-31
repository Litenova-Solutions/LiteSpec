---
{
  "type": "decision",
  "id": "decision/0001-loan-period",
  "title": "Loan period",
  "owner": "lending-team",
  "status": "approved",
  "options": [
    {
      "name": "14 days",
      "pros": "Copies turn over fast for holds.",
      "cons": "Readers rush through long books."
    },
    {
      "name": "21 days",
      "pros": "Most readers finish in three weeks.",
      "cons": "Holds wait a week longer."
    },
    {
      "name": "28 days",
      "pros": "Slow readers keep up.",
      "cons": "Popular copies sit idle."
    }
  ],
  "choice": "21 days",
  "reopenedWhen": "Reader demand shifts.",
  "decides": [
    "requirement/borrowing.max-active-loans",
    "requirement/borrowing.loan-period-days",
    "requirement/borrowing.hold-pickup-window",
    "requirement/borrowing.copy-shelf-home"
  ],
  "basedOn": [],
  "approvedBy": "lending-team",
  "approvedOn": "2026-10-01",
  "approvedHash": "1c363de5"
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
