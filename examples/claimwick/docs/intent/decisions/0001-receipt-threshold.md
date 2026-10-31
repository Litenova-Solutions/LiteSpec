---
{
  "type": "decision",
  "id": "decision/0001-receipt-threshold",
  "title": "Receipt Threshold",
  "owner": "claimwick-team",
  "status": "approved",
  "options": [
    {
      "name": "0 EUR",
      "pros": "Every expense carries proof.",
      "cons": "Tiny expenses cost more to file than they pay."
    },
    {
      "name": "25 EUR",
      "pros": "Small expenses stay light to submit.",
      "cons": "Some fraud slips under the line."
    },
    {
      "name": "75 EUR",
      "pros": "Almost nothing needs a receipt.",
      "cons": "Finance cannot defend the tax file."
    }
  ],
  "choice": "25 EUR",
  "reopenedWhen": "Finance sees abuse or friction.",
  "decides": [
    "requirement/approval.receipt-over-threshold"
  ],
  "basedOn": [],
  "approvedBy": "claimwick-team",
  "approvedOn": "2026-10-01",
  "approvedHash": "21fa87c2"
}
---

# Receipt Threshold

## Context

Small expenses need no proof, large ones do.

## Decision

Expenses above 25 EUR need a receipt.

## Consequences

Small expenses stay light to submit.

## Evidence with Confidence

Finance reviewed the filing cost against the fraud risk in February 2026 and stands behind 25 EUR.
