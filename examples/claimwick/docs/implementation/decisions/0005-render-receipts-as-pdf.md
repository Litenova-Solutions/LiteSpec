---
{
  "type": "decision",
  "id": "decision/0005-render-receipts-as-pdf",
  "title": "Render Receipts as PDF",
  "owner": "claimwick-team",
  "status": "approved",
  "options": [
    {
      "name": "PDF library",
      "pros": "Receipts download as files finance keeps.",
      "cons": "A new package to admit and update."
    },
    {
      "name": "HTML to PDF browser",
      "pros": "No new package lands.",
      "cons": "Rendering differs per browser and prints poorly."
    }
  ],
  "choice": "PDF library",
  "reopenedWhen": "Receipts stop needing a downloadable file, or the library stops fitting.",
  "basedOn": [
    "constraint/tax-receipt-retention"
  ],
  "admits": [
    {
      "package": "nuget:PdfSharp",
      "layer": "Infrastructure",
      "licence": "MIT",
      "location": "in-process",
      "accountOwner": "none",
      "failureBehavior": "the receipt download fails and the expense stays readable",
      "operatingCost": "none"
    }
  ],
  "approvedBy": "claimwick-team",
  "approvedOn": "2026-10-01",
  "approvedHash": "f179da6a"
}
---

# Render Receipts as PDF

## Context

Finance keeps each receipt as a file next to the expense.

## Decision

Receipts render to PDF through the admitted library.

## Consequences

Each receipt downloads as a file finance keeps.

## Evidence with Confidence

Finance confirmed in September 2026 that a PDF file per receipt satisfies the tax file.
