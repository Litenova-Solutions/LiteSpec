# One Audit Chain Segment per Category and Year

Audit retention deletion and hash chaining pull in opposite directions: deletion removes rows while chaining forbids it. This decision gives each audited category and calendar year its own chain segment, deleted whole by an operator account when retention ends.

## Context

The audit extension requires three things at once: the deployment deletes each record after its category's retention ends, the writing account holds neither `UPDATE` nor `DELETE`, and each record chains to its predecessor. Deleting one category's expired records from a shared chain breaks every later link, so verification fails from the first expiry and never recovers. Nothing names the account that deletes.

Claimwick keeps sign-in denials for two years and expense records for seven, in one chain per workspace. In year three the first denial expires, and deleting it breaks the chain for every record after it.

## Options

LiteSpec compared 2 options for audit retention deletion. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| One chain segment per audited category and calendar year, deleted whole by an operator account, with each deletion recorded in the following segment | A closed segment is deleted as a whole, so live links never break and the remaining segments still verify from their own anchors. The operator account holds `DELETE` while the application account keeps only `INSERT` and `SELECT`. | Each category and year pair is its own chain to checkpoint and verify, so a deployment with many categories runs more segments. |
| One shared chain with links rewritten around deleted records | One chain keeps one checkpoint and one verification pass. | Rewriting links edits retained evidence, so a reviewer cannot tell deletion from tampering. Each expiry rewrites history instead of closing it. |

## Decision

An audit store holds one hash chain segment per audited category and calendar year, each with its own genesis record and checkpoints.

A segment is closed with a final checkpoint and deleted as a whole after its category's retention ends.

Retention deletion and tenant data erasure run through a separate audited operator process under its own account, while the application account keeps only `INSERT` and `SELECT`.

Each deleted segment is recorded in the following segment of the same category, which is still live because it expires a year later.

## Consequences

An audit store holds one hash chain segment per audited category and calendar year, each with its own genesis record and checkpoints. A segment is closed with a final checkpoint and deleted as a whole after its category's retention ends.

Retention deletion and tenant data erasure run through a separate audited operator process under its own account. Each deleted segment is recorded in the following segment of the same category.

The new provisions are `litespec/ext/audit#delete-expired-records-through-a-separate-operator-account`, `litespec/ext/audit#segment-the-audit-chain-by-category-and-calendar-year`, and `litespec/ext/audit#record-each-segment-deletion-in-the-following-segment`.

## Revisit Conditions

This decision reopens when a deployment needs a chain shape this segmentation cannot express, or when per-year segments prove too coarse or too fine for retention schedules in use.

Either case means one segment per category and year no longer fits the deployments built on it. The team then decides again how chains are segmented.
