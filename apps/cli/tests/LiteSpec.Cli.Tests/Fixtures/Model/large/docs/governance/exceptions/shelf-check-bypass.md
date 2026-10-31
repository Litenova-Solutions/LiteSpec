---
{
  "type": "exception",
  "id": "exception/shelf-check-bypass",
  "title": "Shelf check bypass",
  "owner": "lending-team",
  "status": "approved",
  "kind": "departure",
  "covers": "litespec/backend/application#enforce-target-authorization-in-the-pipeline",
  "subject": [
    "use-case/lending.register-copy"
  ],
  "reason": "The import runs before opening.",
  "expiresOn": "2027-01-01",
  "approvedBy": "lending-team",
  "approvedOn": "2026-10-01",
  "approvedHash": "d1d5b05b"
}
---

# Shelf check bypass

## Consequences

The morning import skips the quiet check. The bypass ends when the import moves after opening.
