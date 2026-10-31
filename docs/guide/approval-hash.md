# Read the Approval Hash

## Purpose

An approved or reviewed record carries `approvedHash`, the builder hash over its semantic fields, so a later change inside those fields reopens the approval while a change outside them does not. This guide states the one rule for which fields feed the hash, states how the explanation file of an article type feeds it, states what reopens an approval, and shows how `model approve --dry-run` previews the stamp. Each step names the standard it satisfies, and the guide adds no rule of its own.

## Prerequisites

Start with:

- A consumer from [Start a Consumer Repository](../tutorial/getting-started.md), with at least one approved or reviewed record.
- The model command at the adopted release, run from the consumer root.

Read [Project Model](../core/model.md) before this guide: it states the approval data, and this guide shows which fields the hash covers.

## Procedure

### Know Which Fields Stay Outside

The hash covers every written record field except identity, workflow, and approval data. These fields stay outside on every record type: `type`, `id`, `title`, `owner`, `status`, `decider`, `decideBy`, `approvedBy`, `approvedOn`, `approvedHash`, and `provenance`. A term's `labels` stay outside too, so adding a language leaves the approved meaning alone. The entries a page nests under `records` carry their own hash and stay outside their page's, so promoting a result or a scenario leaves its use case's approval alone. Every other written record field feeds the hash, so a field a record type gains joins the hash by default. That covers the prose fields a record holds: a reworded `purpose`, `whatHappens`, `summary`, or `betweenSteps` reopens the approval the same way a moved statement does. Rendered consequences summaries never feed the hash. The builder canonicalizes the remaining fields and hashes them to eight hex characters. This satisfies `litespec/core/model#record-approval-as-data`.

### Know How Article Explanations Feed the Hash

Research, decision, and topic are article types: each carries a required explanation file beside its record file, and the hash covers that file with the record. Editing the Answer of a research article, the Context of a decision, or any of the six required sections of a topic reopens the approval of its record. For every other type the hash covers the record file alone, so fixing a typo in Notes or an Example reopens nothing. This satisfies `litespec/core/model#record-approval-as-data`.

### Know What Reopens an Approval

Any change inside the hashed fields reopens the approval: a moved statement, a new result, a renamed output, a new rule, a changed step, a reworded `purpose` or `whatHappens` field, or an edited article explanation. `litespec model check` reports `approval-reopened` for a record whose approval hash differs from its `approvedHash`. A change outside the hash, such as a retitled record, a new owner, or a reworded explanation beside a non-article record, reopens nothing. This satisfies `litespec/core/model#record-approval-as-data`.

### Preview Approval with a Dry Run

Run the dry run before stamping, to see exactly which records the stamp touches:

```bash
litespec model approve --all --dry-run
```

The dry run lists exactly the records `litespec model approve --all` stamps: each approved or reviewed record with no `approvedHash` prints as one `would be stamped (no approvedHash)` line. The last line counts them. The command writes nothing and exits 1 only when a named id matches no written record. This satisfies `litespec/core/model#record-approval-as-data`.

## Check

- `litespec model approve --all --dry-run` exits `0` and lists exactly the approved or reviewed records with no `approvedHash` that the next `approve --all` stamp touches.
- `litespec model check` reports no `approval-reopened` finding after approval.
