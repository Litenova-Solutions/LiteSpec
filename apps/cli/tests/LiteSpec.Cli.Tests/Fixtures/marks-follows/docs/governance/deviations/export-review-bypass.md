---
{
  "type": "deviation",
  "id": "deviation/export-review-bypass",
  "title": "Export review bypass",
  "owner": "lending-team",
  "status": "draft",
  "decider": "lending-team",
  "decideBy": "2030-01-31",
  "provision": "litespec/backend/persistence#commit-each-command-pipeline-once",
  "reason": "The nightly export commits per batch, not per pipeline",
  "reviewBy": "2030-06-30",
  "removeWhen": "The export moves to the command pipeline"
}
---

# Export review bypass

## Consequences

The export step commits per batch until it moves to the pipeline.
