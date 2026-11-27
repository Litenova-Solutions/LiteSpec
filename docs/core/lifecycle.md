# Data Lifecycle

**Stack:** any

**Concern:** application

Every stored record carries a lifetime: how long it stays, what deletes it, and what brings it back. This page is for developers who delete, retain, archive, restore, hold, export, or reuse stored data. A deletion that ignores retention or a legal duty destroys evidence or breaks a promise, so each lifetime is recorded and each operation runs through explicit behavior.

## Concepts

### How Lifetimes Are Recorded

One lifecycle record gathers the choices for a record kind: its classification, retention, deletion trigger, hold, export, restore, and backup. Retention bounds live in setting records beside it.

A lifecycle operation writes its audit record through [Audit Trail](audit.md), so a later dispute answers who deleted what and when. A retirement that removes declared behavior is a breaking change under the `compat` extension.

### How Records Leave and Return

A retained record carries a business reason and an explicit state transition in its aggregate, such as an `Archive` step that runs after the authorization and audit checks succeed. The aggregate exposes delete, archive, and restore as behavior beside its other rules, so each caller runs the same checks. A boolean column that storage alone reads flips with no checks at all, leaving one query showing the record as deleted and another as active.

Authorization runs before the record changes, and normal queries read the active state alone. Privileged reads run on named paths with their own authorization, such as an administration query or a restore query. The specification also defines post-deletion behavior for cross-aggregate references, exports, and unique constraints, giving each one answer.

### How Storage and Jobs Carry It Out

Document implementations state the document state, the filtered queries, the indexes, the purge transformations, and the contract evolution. Relational implementations state the columns, the query filters, the partial indexes, the migrations, and the meaning of each named filter. Tests read through the normal path and the lifecycle path on the selected provider.

A purge or archive job runs resumable, bounded, cancellation-aware, and observable across millions of rows, restarting where it stopped. Each retention run records its progress for the next run. Backups and replicas follow the documented deletion guarantee, since a copy that restores months later falls under the same promise.

A soft-delete timestamp uses `deleted_at_utc`. Archive records sit under the owning module, beside the rules that define them. History kept for display, such as a list of past expenses, lives in a read model beside the record.

## Standards

### Default to Hard Delete

**ID:** `litespec/core/lifecycle#default-to-hard-delete`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A lifecycle implementation MUST use hard delete when no accepted retention or restore requirement applies.

**Rationale:** Removing the row deletes the least and keeps the least: no hidden state, no filtered queries, and no purge job to watch. Rows kept without a duty still answer subject-access exports with data the product promised to delete.

**Example:** A Claimwick expense deleted without a retention duty removes the expense row in the delete transaction.

**Reviewed for:** Does every deletion with no retention or restore requirement remove the row in the delete transaction? Check the delete transaction code.

### Preserve Legal-Hold Data

**ID:** `litespec/core/lifecycle#preserve-legal-hold-data`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A retention job MUST preserve data under legal hold.

**Rationale:** A hold suspends the duty to delete, and the named data stays until the hold releases, even past its retention date. A job that ignores the hold destroys evidence the organization had a duty to keep.

**Example:** Claimwick retention skips receipts under a tax hold and deletes them once the release record resumes their retention.

**Reviewed for:** Does every retention run preserve held data past its retention date? Check the retention run code beside the hold records.

## Conventions

None.
