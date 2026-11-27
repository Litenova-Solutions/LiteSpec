# Audit Trail

**Stack:** any

**Concern:** application

An audit trail proves who attempted what inside data one party owns while another party acts in it. It covers attempts the system refused as well as attempts that completed, because refusals carry the signal a reviewer looks for. This page states what each record stores, which attempts the trail covers, how the store resists tampering, and how a reviewer reads the trail. It is for developers recording auditable actions and for reviewers judging the evidence.

## Concepts

### How This Page Builds on the Baseline

The baseline states the fields of one security audit event, and this page owns the rest: the producer writing the records, the transaction boundary of their commit, the policy selecting the audited attempts, the store holding them, and the read path a reviewer uses. Every command declares its audit position in its `audit` field. The audit trail guide shows the pipeline step by step in [the audit trail guide](../guide/audit-trail.md), and the obligations map lists the external obligation behind each rule in [the obligations map](../reference/audit-obligations.md).

### What the Trail Proves

The product already keeps diagnostic logs and a domain event stream, and neither answers who is accountable. The table below shows the three records side by side, each with its own producer, its own reader, and its own retention. Using one in place of another removes evidence.

| Record | Holds | Read by |
|:---|:---|:---|
| Diagnostic log | What the software did | Engineers during an incident |
| Domain event stream | What became true | The system itself |
| Audit trail | Who is accountable for an attempt | Reviewers, customers, regulators |

A domain event carries domain identity rather than use-case identity, and one event type comes from more than one use case. It holds the fields the domain needs, so an actor the domain ignores is absent from it. A refused attempt changes nothing and raises no event, even though refusal is the case a reviewer asks about.

### Which Commands the Trail Covers

The adoption decision declares the audited categories with the threshold, the access model, the integrity method, and the records entry. Each command declares its audit position in its `audit` field, with a level of `required`, `optional`, or `none`, its category, and its target, or it declares an exemption with its justification. A level of `none` marks a command outside the trail, and each declared exemption turns an omission into a visible statement.

The category vocabulary lists eight values: authentication, authorization-denial, grant-change, monetary-action, tenant-configuration, personal-data-read, bulk-export, and background-process-change. Cross-boundary personal-data queries write a record, and above-threshold queries write one inside the boundary as well, carrying the subject count instead of each subject. Exports write a record naming the requested scope, and reads of the trail write a record too.

A recorded selection keeps the trail small enough to read. A trail recording every command buries the sensitive action among routine traffic. The build gate turns away a command with neither a position nor an exemption, with a composition refusal at startup satisfying the gate.

### How Records Reach the Store

The producer runs in the mediation stages dispatching the command. Those stages observe the whole attempt, including attempts the handler stays out of and refusals the pipeline makes. Work running after its request is recorded at request time, under the identity of the caller asking for it.

The handler contributes the values it alone determines, such as a generated target identity or a reason value composed from loaded state. The declaration beside the message fixes the target and the reason value, so the pipeline records both on each outcome including denial. A success record commits with the business change in one transaction, staged before the commit stage, while an unsuccessful record travels on a connection outside the failing transaction.

The deployment publishes the write failure count of the unsuccessful path. Steady loss is thus visible apart from rare loss. The detection copy leaves through the outbox, with the local store authoritative for accountability.

### What Each Record Carries

Each record carries actor, action, target, outcome, business time, record time, and tenant. The actor carries its kind beside its identifier, and the target carries its own identity apart from the actor. Business time marks the action and record time marks the writing, so a delayed write shows as a delay. Records from inbound requests carry the origin address, the client description, and the request identifier, plus the trace identifier joining the row to its diagnostic detail.

The action value is the identity of the invoked use case, joining record and specification with no mapping between them. Outcomes are three: `succeeded` for a committed change, `denied` for refused authorization, and `failed` for a permitted attempt ending without completion. Each denied or failed record carries the refusal code its attempt produced, and each record carries an explicit schema version.

The actor vocabulary is closed: `person`, `company`, `provider`, `system`, or `guest`. A device maps to the person whose account authorized it, with the device as the client description, and a named process maps to `system`. Actions under an administrative, support, or delegation grant record the grant used, putting the acting party on the record beside the delegator.

Secrets and payment instrument values stay out of the trail. Personal data stays out except for the pseudonymous identifiers of actor and target, and state copies stay out with the change left to the event stream. Erasure removes the mapping from identifier to person and retains each record, so the record survives its subject.

### How the Store Shows Tampering

The store is append-only through granted privileges. The application account holds `INSERT` and `SELECT` alone, and the operator account alone holds the deletion privilege. Each record chains to its predecessor in its segment with a SHA-256 hash over a canonical serialization of both.

Each segment verifies from its own genesis record. The store publishes a periodic checkpoint of each live segment head and record count to a destination outside operator rewrite, and verification recomputes each live segment on a schedule with an alert on a break. Records past the latest checkpoint stay uncovered until the next one, so the interval bounds the window of unseen rewrites.

Segments divide by audited category and calendar year. Deletion removes a whole closed segment after a final checkpoint, with a deletion record landing in the following segment of the same category. Retention deletion and tenant data erasure run through the separate audited operator process under its own account.

### How Reviewers Read the Trail

The trail read is a use case with its own access grant. Filters on tenant, actor, action, target, and outcome keep each reader inside the records the reader is allowed to see. Tenant actions carry the tenant identity, while authentication events live in the global trail apart from tenant activity.

## Standards

### Record the Audit Adoption Decision

**ID:** `litespec/core/audit#record-the-audit-adoption-decision`

**Strength:** required

**Applies to:** artifact: record:decision; when: audit-adoption

**Statement:** An audit adoption decision MUST record its audited categories, threshold, access model, integrity method, and records entry.

**Rationale:** Policy choices belong in one declared place. A reviewer reads the declared policy and then reads the implementation against it, so two reviewers start from the same statement. The records entry fixes where the evidence lives, including the split between the tenant trail and the global trail.

**Reviewed for:** Does the adoption decision behind the audited Commands name its categories, threshold, access model, integrity method, and records entry? Check that decision record.

### Declare Which Commands Are Audited

**ID:** `litespec/core/audit#declare-which-commands-are-audited`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Every command use case MUST declare its audit position in its `audit` field, or declare an exemption with its justification.

**Rationale:** A selection living in code stays invisible. The `audit` field turns each omission into a visible statement, so a reader tells an unaudited command from an audited one with a missing call. The target ties the record to the data the command changes.

**Example:** A Claimwick command listing public help pages carries an exemption with its justification. Reading public text is outside the sensitive set, and the exemption says so on the record.

**Checked by:** `litespec model check` reports `audit-missing` for a command with neither an audit position nor an exemption, `record-invalid` for an audited command without its category, `audit-target-missing` for an audited command changing a record with no target, `audit-target-unknown` for a target naming no input, an input with no `identifies`, or no output field, and `audit-target-kind-drift` for a target kind differing from what a single target input identifies.

### Audit Only Selected Commands

**ID:** `litespec/core/audit#audit-only-selected-commands`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A project MUST NOT audit every Command without a recorded selection.

**Rationale:** Volume defeats review. A trail recording every command buries the sensitive action among routine traffic, and the reviewer stops reading. A recorded selection keeps the trail small enough to read.

**Reviewed for:** Does every audited category carry a recorded selection reason? Check the declared selection.

### Record Every Attempt Outcome

**ID:** `litespec/core/audit#record-every-attempt-outcome`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An audit record MUST carry an outcome of `succeeded`, `denied`, or `failed`.

**Rationale:** Successes alone show a change history. Refusals carry the security signal and failures carry the operating signal, and three outcomes hold both beside the successes. A validation refusal records `failed` with its refusal code, while `denied` stays for refused authorization.

**Example:** In Claimwick, an expense refused for a missing receipt records `failed` with the receipt refusal code. An approval by someone lacking the permission records `denied` with the forbidden code.

**Reviewed for:** Does every record carry a `succeeded`, `denied`, or `failed` outcome? Check the outcome column.

### Record Refused Authorization

**ID:** `litespec/core/audit#record-refused-authorization`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An authorization decision that refuses an audited action MUST produce an audit record.

**Rationale:** Repeated refusal is the first indicator of a compromised or probing account. Nothing else holds it: the change went nowhere, so nothing else was written. Each refusal writes its own record, so the trail shows an attacker who failed.

**Reviewed for:** Does every refused authorization produce a denied record? Check the refusal paths beside the trail.

### Keep Secrets and Payment Data from the Trail

**ID:** `litespec/core/audit#keep-secrets-and-payment-data-from-the-trail`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An audit record MUST NOT contain a secret, as `litespec/quality/security#keep-secrets-out-of-tracked-and-observable-data` states, or a payment instrument value.

**Rationale:** The trail is retained, widely readable, and append-only. That combination extends exposure and removes repair: a credential written once stays readable for the whole retention period. Secrets and payment values travel in the systems built to hold them.

**Reviewed for:** Does any stored record contain a secret or a payment instrument value? Check the stored audit rows.

### Commit a Success Record with Its Business Change

**ID:** `litespec/core/audit#commit-a-success-record-with-its-business-change`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An audit record with a `succeeded` outcome MUST commit in the same database transaction as the change it describes.

**Rationale:** One transaction makes the action and its evidence inseparable. The change commits with its record or neither commits. Separate writes admit both an unrecorded action and a record of a rolled-back action.

**Reviewed for:** Does every succeeded record commit inside its business transaction? Check the success write path.

### Write an Unsuccessful Record Outside the Failed Transaction

**ID:** `litespec/core/audit#write-an-unsuccessful-record-outside-the-failed-transaction`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An audit record with a `denied` or `failed` outcome MUST be written on a connection that the failing transaction does not roll back.

**Rationale:** A denied or failed attempt ends with its transaction rolling back. A record inside that transaction rolls back with it and vanishes. The separate connection keeps the evidence of a refusal past the rollback.

**Reviewed for:** Does any denied or failed record roll back with its transaction? Check the unsuccessful write path.

### Scope Every Record to Its Tenant

**ID:** `litespec/core/audit#scope-every-record-to-its-tenant`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every audit record MUST carry the scope it belongs to: an action inside tenant-owned data carries the identity of that tenant, and an authentication event that belongs to no tenant lives in the global trail apart from tenant activity.

**Rationale:** The tenant is the filter for each read of the trail. Scoping each record at write time keeps that filter exact. Authentication events belong to no tenant, so the global trail holds sign-in evidence apart from tenant data.

**Reviewed for:** Is every record scoped to the right trail, with tenant data carrying its tenant identity and authentication events in the global trail? Check the tenant column and the global trail.

### Enforce Append-Only Through Storage Privilege

**ID:** `litespec/core/audit#enforce-append-only-through-storage-privilege`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An audit store MUST refuse update and delete through the privileges granted to every account except the operator account that deletes expired records.

**Rationale:** Absent code paths stay absent until someone writes them. Revoked privileges refuse the write no matter what the code tries. A reviewer verifies a privilege in the database instead of auditing each line for a write that is absent.

**Example:** The application account holds `INSERT` and `SELECT` on the audit table, with neither `UPDATE` nor `DELETE`. The operator account holds `INSERT` and `DELETE` on the same table and writes through the deletion job alone.

**Reviewed for:** Does any account except the operator deletion account hold update or delete on the audit store? Check the granted store privileges.

### Correct Mistakes with a New Record

**ID:** `litespec/core/audit#correct-mistakes-with-a-new-record`

**Strength:** required

**Applies to:** artifact: command-handler; when: audited

**Statement:** A correction to the trail MUST be a new record that references the earlier one.

**Rationale:** An edited history carries no evidential value. A reviewer tells a correction from a cover-up only when both stay visible. The new record keeps the mistake and the fix side by side.

**Reviewed for:** Is every correction a new record that points at the earlier one? Check the correction records.

### Delete Expired Records Through a Separate Operator Account

**ID:** `litespec/core/audit#delete-expired-records-through-a-separate-operator-account`

**Strength:** required

**Applies to:** artifact: job

**Statement:** An audit deployment MUST run retention deletion and tenant data erasure through a separate audited operator process under its own account.

**Rationale:** The application account holds no deletion privilege, so retention deletion needs a second account. The operator process holds `DELETE` under its own account and writes one deletion record per deleted segment. Tenant data erasure runs through the same process, so no application path deletes audit data. This follows decision/0097-one-audit-chain-segment-per-category-and-year.

**Example:** Claimwick keeps sign-in denials for two years and expense records for seven. A nightly operator job deletes the closed segments past retention and writes one deletion record per deleted segment, while the application account keeps `INSERT` and `SELECT` alone.

**Reviewed for:** Does all retention deletion and tenant data erasure run through the operator process under its own account? Check the deletion job and the granted store privileges.

### Segment the Audit Chain by Category and Calendar Year

**ID:** `litespec/core/audit#segment-the-audit-chain-by-category-and-calendar-year`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An audit store MUST hold one hash chain segment per audited category and calendar year, each with its own genesis record and checkpoints.

**Rationale:** Deleting one category from a shared chain breaks each later link. One segment per category and year lets the deployment delete a whole closed segment while the rest still verify from their own anchors. The segment closes with a final checkpoint before deletion. This follows decision/0097-one-audit-chain-segment-per-category-and-year.

**Example:** Claimwick holds its 2026 sign-in denials in one segment. When the two years end, the deployment deletes that segment whole, and the expense segments verify from their own genesis records.

**Reviewed for:** Does every audit record sit in the segment of its category and year, with its own genesis and checkpoints? Check the stored chain values.

### Record Each Segment Deletion in the Following Segment

**ID:** `litespec/core/audit#record-each-segment-deletion-in-the-following-segment`

**Strength:** required

**Applies to:** artifact: job

**Statement:** An audit deployment MUST record each deleted chain segment as an audit record in the following segment of the same category.

**Rationale:** A deleted segment leaves no rows. The deletion record in the following segment states which segment went with its count and final checkpoint. The following year segment is still live, so it holds the record. This follows decision/0097-one-audit-chain-segment-per-category-and-year.

**Example:** Deleting the 2026 sign-in denial segment writes one record into the 2027 denial segment. The record names the deleted segment with its count and final checkpoint.

**Reviewed for:** Does every deleted segment have its deletion record in the following segment? Check the deletion records against the retention schedule.

## Conventions

None.
