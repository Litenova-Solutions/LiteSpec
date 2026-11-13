# Render Receipts as PDF

Claimwick renders each receipt as a PDF file that finance keeps. Finance keeps each receipt as a file next to its expense, so the tax file holds the proof it needs. This decision is part of the Spending module's implementation. It chose a PDF library over rendering receipts in the browser, and it admits one package for the job.

## Context

Finance keeps each receipt as a file next to its expense. The decision rests on `constraint/tax-receipt-retention`, the constraint behind this choice.

## Options

Two ways of producing the PDF were weighed: a PDF library, and an HTML to PDF browser.

| Option | Advantage | Drawback |
|:---|:---|:---|
| PDF library | Receipts download as files finance keeps. | A new package to admit and update. |
| HTML to PDF browser | No new package lands. | Rendering differs per browser and prints poorly. |

The PDF library adds a package to admit and update. The browser route avoids a package, but its rendering differs from one browser to another and prints poorly.

## Decision

Receipts render to PDF through the admitted library, `nuget:PdfSharp`.

The package is under the MIT license. It runs in the same process as Claimwick, in the Infrastructure layer.

## Consequences

Each receipt downloads as a file that finance keeps. The team must admit and update the package.

If the library fails, the receipt download fails and the expense stays readable.

## Evidence

Finance confirmed in September 2026 that one PDF file per receipt satisfies the tax file.

The record gives no separate confidence level for this confirmation.

## Revisit Conditions

The choice reopens when receipts stop needing a downloadable file, or when the library stops fitting. Either sign removes the reason for a PDF file or for this package.
