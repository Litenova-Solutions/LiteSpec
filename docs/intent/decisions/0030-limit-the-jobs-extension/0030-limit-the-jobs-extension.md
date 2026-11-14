# Jobs Extension Limited to Scheduled and Manual Work

The jobs extension is the part of LiteSpec's standard that covers jobs, the work that runs on a schedule or when a person starts it. This decision keeps the rules for scheduled and manual work, and it drops the rules for an ingest pipeline and a workflow engine. A project that runs those keeps their rules in its own practice.

## Context

The jobs extension holds 73 provisions across 6 subjects. Two of the subjects, the ingest pipeline and the workflow engine, serve products that a small-team project does not run. No check can prove the engine steps, because the record schema has no kind for a multi-step workflow.

## Options

LiteSpec compared 3 options for the jobs extension. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Keep scheduled and manual jobs, occurrences, time rules, queues and reports | The extension covers the work that a small team runs, and no provision describes a check that nothing reads. | A team that runs an ingest pipeline or a workflow engine keeps those rules in its own practice. |
| Keep all 6 subjects | No provision leaves the extension. | The ingest pipeline serves a data-ingestion product, and the engine provisions describe steps that no record kind can state. |
| Remove the extension | No extension to maintain. | Teams that run scheduled work lose the lease, cursor and misfire rules they still need. |

## Decision

The extension keeps scheduled and manual jobs, occurrences, time rules, human-cleared queues and reports. It drops the ingest pipeline and the workflow-engine subjects.

Coordination across aggregates is stated on each use case, as `decision/0015-automations-and-sagas` records.

## Consequences

The extension keeps 59 provisions, down from 73. The ingest pipeline and workflow-engine subjects leave with their 14 provisions.

The record schema has no saga kind. A saga is a sequence of steps that spans several aggregates. No check reads engine steps. The removed IDs appear in the 5.0.0 provision diff.

## Revisit Conditions

This decision reopens when a team must prove a multi-step workflow that no record can state, or when a product must prove an ingest pipeline.

Each of those cases needs a record kind or a subject that the extension no longer holds. Either one would bring back the subject it needs.
