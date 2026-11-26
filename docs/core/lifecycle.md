# Data Lifecycle

**Stack:** any

**Concern:** application

Stored data lives for a defined time: how long it stays, when it is deleted, and when it can return. This page states where those terms live and how code carries them out: hard delete where nothing needs the data later, explicit archive and restore states where a use case needs them, and authorized, audited operations throughout. A use case is one command or query the product offers.

These rules cover each use case or rule that deletes, retains, archives, restores, holds, exports, or reuses data after deletion, so no data lingers past its retention and no deletion breaks a legal or business duty.

## Concepts

### When These Rules Apply

These rules cover a use case or rule that deletes a record, keeps one past its active life, archives or restores one, holds one under a legal duty, exports one person's data on request, or reuses a value after deletion. For example, a spending rule that keeps receipts for 7 years under the tax receipt retention constraint falls under these rules, while an expense use case that deletes nothing does not.

### How This Page Builds on the Baseline

No baseline provision changes under these rules.

The setting record type holds the retention bounds the lifecycle rules name.

A lifecycle record retires, and a retirement that removes declared behavior is a breaking change under the `compat` extension.

### Related Reading

A lifecycle operation writes its audit record through [Audit Trail](audit.md). Retiring declared behavior follows [API Compatibility](../ext/compat.md).

## Standards

### Record Lifecycle Scope

**ID:** `litespec/core/lifecycle#record-lifecycle-scope`

**Strength:** required

**Applies to:** artifact: record:rule

**Statement:** A lifecycle record MUST name classification, retention, deletion trigger, hold, export, restore, and backup.

**Rationale:** A record's lifetime raises one set of choices: what kind of data it is, how long it stays, what triggers deletion, and how holds, exports, restores, and backups behave. Naming them all in one lifecycle record keeps them reviewable together. Without that record, the choices scatter across tickets and memory, and a deletion runs before anyone checks the retention bound. Retention bounds live in setting records.

**Reviewed for:** Does every lifecycle record name classification, retention, deletion trigger, hold, export, restore, and backup? Check each lifecycle record.

### Default to Hard Delete

**ID:** `litespec/core/lifecycle#default-to-hard-delete`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A lifecycle implementation MUST use hard delete when no accepted retention or restore requirement applies.

**Rationale:** When no use case needs the data back, removing the row deletes the least and keeps the least: no hidden state, no filtered queries, and no purge job to write and watch. A team that keeps deleted rows just in case pays for retention without a requirement and answers subject-access exports with data it promised to delete.

**Example:** A Claimwick expense deleted without a retention duty removes the expense row in the delete transaction.

**Reviewed for:** Does every deletion with no retention or restore requirement remove the row in the delete transaction? Check the delete transaction code.

### Model Retained Lifecycle States

**ID:** `litespec/core/lifecycle#model-retained-lifecycle-states`

**Strength:** required

**Applies to:** artifact: state-record

**Statement:** A soft-deleted or archived record MUST have a business reason and explicit state transition.

**Rationale:** A record that stays after deletion or moves to an archive still affects the product: it appears in some reads, skips others, and can return. A named state with a named transition shows that behavior in the aggregate itself, so a reader sees which transitions exist and which checks each one runs. Without the transition, the retained data is a row that code handles with special cases in scattered places.

**Example:** `Archive` changes aggregate state after the authorization and audit checks succeed.

**Reviewed for:** Does every retained record carry a business reason and a named state transition? Check each retained record beside its state transitions.

### Reject Hidden Lifecycle Flags

**ID:** `litespec/core/lifecycle#reject-hidden-lifecycle-flags`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A lifecycle implementation MUST NOT use a hidden boolean as its sole lifecycle behavior.

**Rationale:** A boolean column that only the storage layer reads can flip without running the business checks: no authorization, no audit entry, and no named transition a reviewer can find. The record then looks deleted in one query and active in another, depending on which filter each query applies. An explicit lifecycle operation keeps the flag, where one exists, as an effect of the transition rather than the transition itself.

**Reviewed for:** Does every lifecycle transition run through explicit behavior instead of a storage-only flag? Check the transition code.

### Model Lifecycle Operations in Domain

**ID:** `litespec/core/lifecycle#model-lifecycle-operations-in-domain`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** A Domain aggregate MUST expose delete, archive, and restore as explicit behavior when those operations apply.

**Rationale:** Delete, archive, and restore change what the business promises about a record. The aggregate owns them the way it owns any rule: one method per operation, with the checks beside the change. When the operation lives anywhere else, two callers implement it two ways and the checks drift apart.

**Reviewed for:** Does every aggregate expose delete, archive, and restore methods where those operations apply? Check each aggregate's methods.

### Authorize Lifecycle Changes

**ID:** `litespec/core/lifecycle#authorize-lifecycle-changes`

**Strength:** required

**Applies to:** artifact: command-handler, job; when: erasure

**Statement:** A lifecycle operation MUST apply its documented authorization.

**Rationale:** Deleting or restoring the wrong record harms a customer or breaks a legal duty. The authorization check runs before the record changes, because a check that runs after can only report harm it already allowed.

**Reviewed for:** Does every lifecycle operation check its documented authorization before changing the record? Check each operation's authorization check.

### Audit Lifecycle Changes

**ID:** `litespec/core/lifecycle#audit-lifecycle-changes`

**Strength:** required

**Applies to:** artifact: command-handler, job; when: erasure

**Statement:** A lifecycle operation MUST record its audit as an audit record when audit is selected and a standalone record otherwise.

**Rationale:** A subject-access or legal-hold dispute asks first who deleted what and when. The audit entry lets a reviewer answer from the record.

**Reviewed for:** Does every lifecycle operation write an audit record, or a standalone record where audit is not selected? Check each operation's writes.

### Exclude Persistence-Only Lifecycle Behavior

**ID:** `litespec/core/lifecycle#exclude-persistence-only-lifecycle-behavior`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A lifecycle rule MUST NOT rely only on persistence filters or property setters.

**Rationale:** A query filter or a property setter runs without the business checks. It filters rows in storage while the aggregate never learns the transition happened. The product then enforces lifecycle in two places that disagree, and a new query that skips the filter exposes data the lifecycle retired.

**Reviewed for:** Does every lifecycle transition run through behavior beyond persistence filters or property setters alone? Check the transition code and the persistence filters.

### Exclude Inactive Records from Normal Reads

**ID:** `litespec/core/lifecycle#exclude-inactive-records-from-normal-reads`

**Strength:** required

**Applies to:** artifact: query-handler

**Statement:** A normal query MUST exclude deleted or archived records.

**Rationale:** An employee browsing expenses expects live data. Deleted and archived records in that list confuse the employee and leak data the lifecycle retired. Normal queries therefore read only the active state, while the named privileged paths serve the records outside it.

**Reviewed for:** Is every normal query free of deleted or archived records? Check the query filters beside the active state.

### Authorize Lifecycle Access Paths

**ID:** `litespec/core/lifecycle#authorize-lifecycle-access-paths`

**Strength:** required

**Applies to:** artifact: query-handler

**Statement:** An administrative or restore query MUST use a named path with explicit authorization.

**Rationale:** Restore and administration queries reach records normal queries cannot see, so they need a path of their own with checks of their own. Without the named path, a privileged read hides inside normal query code and inherits normal authorization, which grants too much to too many callers.

**Reviewed for:** Does every administrative or restore query run on a named path with explicit authorization? Check the query paths and their authorization.

### Define Post-Deletion References

**ID:** `litespec/core/lifecycle#define-post-deletion-references`

**Strength:** required

**Applies to:** artifact: record:rule

**Statement:** A lifecycle specification MUST define post-deletion behavior for cross-aggregate references, exports, and unique constraints.

**Rationale:** Other records can point at the deleted one, exports can carry its values, and a unique value such as an expense reference number can become available again. When the specification does not define the behavior, each developer guesses: one nulls the reference, another keeps it, and a duplicate value slips through or a report breaks. Writing the behavior down gives every reference, export, and constraint one answer.

**Reviewed for:** Does every specification define post-deletion behavior for references, exports, and unique constraints? Check each specification's deletion section.

### Define Document Lifecycle Behavior

**ID:** `litespec/core/lifecycle#define-document-lifecycle-behavior`

**Strength:** required

**Applies to:** artifact: repository

**Statement:** A Marten lifecycle implementation MUST define document state, filtered queries, indexes, purge transformations, and contract evolution.

**Rationale:** A document store keeps whatever the aggregate writes. The team states how each lifecycle state lands in the document: which state the document carries, which queries filter it, which indexes serve those queries, how a purge rewrites the document, and how the contract changes when the shape does. Without those choices, the business transition lands in storage by accident and each query invents its own filter.

**Reviewed for:** Does every document implementation define document state, filtered queries, indexes, purge transformations, and contract evolution? Check each document implementation.

### Define Relational Lifecycle Behavior

**ID:** `litespec/core/lifecycle#define-relational-lifecycle-behavior`

**Strength:** required

**Applies to:** artifact: migration, repository

**Statement:** A relational lifecycle implementation MUST define columns, query filters, partial indexes, migrations, and named filter behavior.

**Rationale:** A relational table keeps retained rows beside active ones. The team states which columns mark the state, which query filters hide retained rows, which partial indexes keep active values unique, which migrations add all of it, and what each named filter means. Without those choices, a unique constraint blocks a legitimate reuse or a query returns rows the lifecycle retired.

**Reviewed for:** Does every relational implementation define columns, query filters, partial indexes, migrations, and named filter behavior? Check each relational implementation.

### Test Provider Lifecycle Paths

**ID:** `litespec/core/lifecycle#test-provider-lifecycle-paths`

**Strength:** required

**Applies to:** artifact: test

**Statement:** A selected persistence provider MUST test normal and lifecycle query paths.

**Rationale:** Filters, indexes, and migrations behave differently per provider: a filter that works on documents can fail on tables. Tests that read through both the normal path and the lifecycle path on the selected provider catch the difference before a customer does.

**Reviewed for:** Does the suite test the normal and lifecycle query paths on the selected provider? Check the lifecycle tests.

### Keep Purge and Archive Work Bounded and Observable

**ID:** `litespec/core/lifecycle#keep-purge-and-archive-work-bounded-and-observable`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A purge or archive operation MUST be resumable, bounded, cancellation-aware, and observable.

**Rationale:** A purge or archive job can run for hours across millions of rows, and a deployment, a failure, or an operator stops it midway. Resumable, bounded, cancellation-aware work restarts where it stopped instead of from the beginning, and observable progress tells the operator whether it moves or stalls.

**Reviewed for:** Does every purge and archive run resume after interruption within declared bounds and show progress? Check the purge and archive tests.

### Preserve Legal-Hold Data

**ID:** `litespec/core/lifecycle#preserve-legal-hold-data`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A retention job MUST preserve data under legal hold.

**Rationale:** A legal hold suspends the duty to delete. The named data stays until the hold releases, even when its retention date passes. A retention job that ignores the hold destroys evidence the organization had a duty to keep.

**Reviewed for:** Does every retention run preserve held data past its retention date? Check the retention run code beside the hold records.

### Record Retention Progress

**ID:** `litespec/core/lifecycle#record-retention-progress`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A retention job MUST record its progress.

**Rationale:** A retention run can stop midway through millions of rows. Recorded progress tells the next run where to resume, and observable progress tells the operator whether the job moves or stalls.

**Reviewed for:** Does every retention run persist progress for the next run to resume from? Check the retention run code.

### Record the Release of a Legal Hold

**ID:** `litespec/core/lifecycle#record-the-release-of-a-legal-hold`

**Strength:** required

**Applies to:** artifact: command-handler; when: erasure

**Statement:** A legal-hold release MUST record actor, data covered, reason, and resumed retention: an audit record when audit is selected, a standalone record otherwise.

**Rationale:** A hold suspends the duty to erase, and releasing it resumes that duty from a date the team can name. Without a release record, nobody can state when the suspension ended. A subject [erasure request](https://gdpr-info.eu/art-17-gdpr/) then has no answer date, and the next retention run cannot tell which data is deletable again. The record also tells the retention job exactly which data resumed which retention date.

**Example:** A release record names the hold, the release date, and the retention date the covered data carries.

**Reviewed for:** Does every legal-hold release record the actor, data covered, reason, and resumed retention? Check the release records.

### Align Backup Deletion Behavior

**ID:** `litespec/core/lifecycle#align-backup-deletion-behavior`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Backup and replica operations MUST follow the documented deletion guarantee.

**Rationale:** Every copy the product keeps falls under the deletion promise, including backups and replicas that restore for months after the live row disappears. When backup behavior stays undocumented, the team promises erasure it cannot deliver and learns the gap during an audit or a restore.

**Reviewed for:** Does every backup and replica operation follow the documented deletion guarantee? Check the backup and replica procedures.

## Conventions

### Name Soft-Delete Timestamps

**ID:** `litespec/core/lifecycle#name-soft-delete-timestamps`

**Strength:** default

**Applies to:** artifact: any

**Default:** Use `deleted_at_utc` when a soft-delete timestamp is required.

**Replacement:** A project names another column for the soft-delete timestamp in its documentation.

**Rationale:** One name for the deletion timestamp lets every reader and query recognize it on sight. The name states the event and the unit, removing two guesses a reviewer otherwise makes.

**Reviewed for:** Does every soft-delete timestamp use `deleted_at_utc`, or another column the documentation names? Check the schema columns.

### Keep Archive Records Module-Owned

**ID:** `litespec/core/lifecycle#keep-archive-records-module-owned`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep archive records under the owning module.

**Replacement:** A project keeps archive records in one shared archive area.

**Rationale:** The module that defines what a record means also defines what its archive means and who may read it. An archive kept elsewhere sits under two modules, and its access checks drift from its home module.

**Reviewed for:** Does every archive record sit under its owning module, or in the shared archive area the project keeps? Check the module folders.

### Separate Query-Shaped History

**ID:** `litespec/core/lifecycle#separate-query-shaped-history`

**Strength:** default

**Applies to:** artifact: any

**Default:** Use a separate read model when archived history is query-shaped instead of an aggregate invariant.

**Replacement:** A project keeps query-shaped history on the aggregate.

**Rationale:** History kept for display, such as a list of past expenses, constrains nothing the aggregate protects. It costs less as a read model beside the record. Folding it into the record widens the transaction boundary for data no invariant needs.

**Reviewed for:** Does every query-shaped history live in a read model instead of on its aggregate, unless the project keeps it on the aggregate? Check the aggregate code beside the read models.
