---
{
  "type": "exception",
  "id": "exception/export-expenses-unexposed",
  "title": "Export Expenses Unexposed",
  "owner": "claimwick-team",
  "status": "approved",
  "kind": "gap",
  "covers": [
    "use-case-endpoint-missing"
  ],
  "subject": [
    "use-case/spending.export-expenses"
  ],
  "reason": "Export has no endpoint until it has a handler, which waits for the finance file format.",
  "expiresOn": "2027-06-01",
  "approvedBy": "claimwick-team",
  "approvedOn": "2026-10-31",
  "approvedHash": "47d378df"
}
---

# Export Expenses Unexposed

## Exit

Exporting expenses has no endpoint, because it has no handler yet. The gap closes when the export use case gains its handler and its endpoint.
