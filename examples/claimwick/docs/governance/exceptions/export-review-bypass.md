---
{
  "type": "exception",
  "id": "exception/export-review-bypass",
  "title": "Export Review Bypass",
  "owner": "claimwick-team",
  "status": "approved",
  "kind": "departure",
  "covers": "litespec/backend/application#enforce-target-authorization-in-the-pipeline",
  "subject": [
    "use-case/spending.export-expenses"
  ],
  "reason": "The weekend export runs with no reviewer on shift.",
  "expiresOn": "2027-01-01",
  "approvedBy": "claimwick-team",
  "approvedOn": "2026-10-01",
  "approvedHash": "6b3665a6"
}
---

# Export Review Bypass

## Consequences

The weekend export skips the daily review. Finance runs the export alone, and a Monday review covers each weekend batch after the fact. The bypass ends when a reviewer covers weekends.
