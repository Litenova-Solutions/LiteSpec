---
{
  "type": "exception",
  "id": "exception/export-review-bypass",
  "title": "Export review bypass",
  "owner": "lending-team",
  "status": "draft",
  "decider": "lending-team",
  "decideBy": "2030-01-31",
  "kind": "departure",
  "covers": "litespec/backend/persistence#commit-each-command-pipeline-once",
  "subject": [
    "use-case/lending.borrow-copy"
  ],
  "reason": "The nightly export commits per batch, not per pipeline",
  "expiresOn": "2030-06-30"
}
---

# Export review bypass

## Consequences

The export step commits per batch until it moves to the pipeline.
