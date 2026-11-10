# Jobs Extension Limited to Scheduled and Manual Work

## Context

The jobs extension held 73 provisions across six subjects. Two of them, the ingest pipeline and the workflow engine, serve products that a small-team project does not run. No check could prove the engine steps, because the record schema has no kind for a multi-step workflow.

## Decision

The extension keeps scheduled and manual jobs, occurrences, time rules, human-cleared queues and reports. It drops the ingest pipeline and the workflow-engine subjects. Coordination across aggregates is stated on each use case, as `decision/0015-automations-and-sagas` records.
