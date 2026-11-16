# Audit Trail

**Stack:** any

**Concern:** application

An audit trail proves who attempted what inside data one party owns while another party acts in it. It covers attempts the system refused as well as attempts that completed, because refusals carry the signal a reviewer looks for.

This extension states what each record stores, which attempts the trail covers, where the producer runs, how the store resists tampering, and how a reviewer reads the trail.

## Activation

Activation scope: `project`.

Applicable specification kinds: None.

Enable `audit` when a person or a machine acts inside data that another party owns and a later reviewer has to establish who is accountable. A regulatory obligation, a customer contract, payment scope, and platform-side support access each meet this condition.

[Audit obligations](../reference/audit-obligations.md) maps each provision on this page to the external obligation it satisfies.

## Baseline Relationship

This extension replaces `litespec/quality/security#record-security-audit-events`. The baseline states the required record fields for one security audit event. This extension owns the rest: the producer that writes records, the transaction boundary for their commit, the policy that selects which attempts are audited, the store that holds them, and the read path a reviewer uses to read them.

This extension switches on no optional record type.

Diagnostic logs and domain events already exist in every system, but neither answers who is accountable. An audited system keeps three records with different producers, readers, and retention. Using one in place of another removes evidence.

| Record | Holds | Read by |
|:---|:---|:---|
| Diagnostic log | What the software did | Engineers during an incident |
| Domain event stream | What became true | The system itself |
| Audit trail | Who is accountable for an attempt | Reviewers, customers, regulators |

A domain event cannot substitute for an audit record. It carries domain identity rather than use-case identity, and one event type can come from more than one use case. It holds only the fields the domain needs, so an actor the domain ignores is absent. A refused attempt changes no state and raises no event, even though refusal is the case a reviewer asks about.

## Standards

### Record the Audit Adoption Decision

**ID:** `litespec/ext/audit#record-the-audit-adoption-decision`

**Strength:** required

**Applies to:** artifact: record:decision; when: audit-adoption

**Statement:** An audit adoption decision MUST record its audited categories, threshold, access model, integrity method, and records entry.

**Rationale:** Which actions to audit, how long to keep them, and how to protect them are policy choices, so the project declares them once in the adoption decision. A reviewer reads the declared policy before reading the implementation instead of reconstructing it from the code, which would give two reviewers two different policies. The records entry declares where audit records live: the store that holds them and, where tenants exist, that authentication events live in the global trail apart from tenant activity.

**Reviewed for:** Does the adoption decision behind the audited Commands name its categories, threshold, access model, integrity method, and records entry? Check that decision record.

### Declare Which Commands Are Audited

**ID:** `litespec/ext/audit#declare-which-commands-are-audited`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Every command use case MUST declare its audit position in its `audit` field, or declare an exemption with its justification.

**Rationale:** An audit selection that lives only in code is invisible: a reader cannot tell an unaudited command from an audited one whose call is missing. The `audit` field states a level of `required`, `optional`, or `none` with its target, or an exemption with its justification, so each omission becomes a visible statement. A level of `none` means the command is not audited. `litespec/core/use-cases#state-the-audit-position-of-every-command` states the field, and review reads the selection instead of searching for missing calls.

**Example:** A Claimwick command that lists public help pages declares an exemption, because reading public text is not a sensitive action.

**Reviewed for:** Does every command use case state its audit position or an exemption with its justification in its `audit` field? Check each use case page.

### Audit Async Work at Request Time

**ID:** `litespec/ext/audit#audit-async-work-at-request-time`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** Work that runs after its request MUST write its audit record at request time.

**Rationale:** The actor, the origin, and the grant exist when the request arrives; the later run carries worker identity under system auth, which says nothing about who asked. Writing the record at request time ties the attempt to the caller who made it. A record written when the background run starts would attribute every scheduled and deferred act to the worker instead of the requester.

**Reviewed for:** Does every deferred run write its audit record when requested rather than when it runs? Check the audit rows beside the requesting calls.

### Audit Every Required Category

**ID:** `litespec/ext/audit#audit-every-required-category`

**Strength:** required

**Applies to:** artifact: record:decision

**Statement:** An audit selection MUST include authentication, authorization denial, grant change, monetary action, tenant configuration, personal-data read, bulk export, and background-process change.

**Rationale:** Each listed category answers a question a security review, a customer, or a supervisory authority asks after an incident: who signed in, who was refused, who changed a grant, who moved money, and the rest. A trail that omits a category has no answer when that question arrives. The list sets the minimum so no project discovers the gap during its first review.

**Reviewed for:** Does the declared selection cover all eight required categories? Check it against the eight categories this provision names.

### Audit Only Selected Commands

**ID:** `litespec/ext/audit#audit-only-selected-commands`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A project MUST NOT audit every Command without a recorded selection.

**Rationale:** A trail that records every Command hides the sensitive action among routine traffic, and the reviewer who has to read it stops reading. Volume then defeats the review the trail exists to support. A recorded selection keeps the trail small enough to read.

**Reviewed for:** Does every audited category carry a recorded selection reason? Check the declared selection.

### Write Audit Records in the Mediation Pipeline

**ID:** `litespec/ext/audit#write-audit-records-in-the-mediation-pipeline`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An audit producer MUST run in the mediation pipeline stages that dispatch the Command.

**Rationale:** Only the pipeline stages that dispatch the Command observe the whole attempt, including attempts the handler never reaches. An audit call inside the handler misses every attempt that throws before it and every refusal the pipeline makes. The pipeline is therefore the one place that records all three outcomes.

**Example:** A pre-handler opens the audit scope, a post-handler closes it as `succeeded`, and an error handler closes it as `denied` or `failed`.

**Reviewed for:** Does every audit write run in the mediation pipeline rather than inside a handler? Check the producer stages and the handler code.

### Restrict Handler Contribution to Determined Values

**ID:** `litespec/ext/audit#restrict-handler-contribution-to-determined-values`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** A handler MUST contribute only the audit values that the handler alone determines.

**Rationale:** The declaration beside the message already fixes the audit target and the reason value the message carries, as `litespec/backend/application#declare-the-audit-target-and-reason-beside-the-message` requires, so the pipeline records both on every outcome, including a denial. Only the handler knows the values it learns while running, such as a generated target identity or a reason value composed from loaded state. Letting the handler write the rest invites a second copy of each field that drifts from the first.

**Example:** A Claimwick handler that creates an expense contributes the expense identity it generated, and nothing else: no actor, action code, timestamp, or value the message already carries.

**Reviewed for:** Does any handler contribution repeat the declared target, the reason value, the actor, the action code, or the timestamp? Check the handler audit calls.

### Exclude Derived Audit Trails

**ID:** `litespec/ext/audit#exclude-derived-audit-trails`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An audit trail MUST NOT be derived from domain events, database triggers, or change data capture.

**Rationale:** Domain events, triggers, and change data capture describe committed state change. They carry no refused attempt, no use-case identity, and no request origin, which are the fields a reviewer reads. A trail derived from them answers what changed and never who attempted it.

**Reviewed for:** Does any trail write derive from events, triggers, or change capture? Check the producer source.

### Record the Required Audit Fields

**ID:** `litespec/ext/audit#record-the-required-audit-fields`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An audit record MUST contain actor, action, target, outcome, business time, record time, and tenant, fixed where the tenancy extension is absent.

**Rationale:** Actor, action, target, outcome, the two times, and the tenant together answer who attempted what, against whose data, with what result, and when. Business time states when the action happened and record time states when the evidence was written, so a delayed write stays visible as a delay. The actor carries its kind beside its identifier, and the target carries its own identity apart from the actor, keeping who acted and what was acted on in separate fields. A record missing any of them leaves the accountability question half answered.

**Reviewed for:** Does every stored record carry actor, action, target, outcome, business time, record time, and tenant? Check the stored audit rows.

### Name Each Action After Its Use Case

**ID:** `litespec/ext/audit#name-each-action-after-its-use-case`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An audit `action` MUST use the identity of the use case that the actor invoked.

**Rationale:** A reviewer who reads an action value opens the specification of that use case to learn the intended behavior. A separate action vocabulary needs a mapping to those specifications, and the mapping drifts as both sides change. Reusing the use-case identity keeps record and specification joined with no mapping to maintain.

**Example:** `catalog.add-product` is both the use case the actor ran and the value stored in the audit `action` field.

**Reviewed for:** Does every action value name the use case the actor invoked? Check the action column against the use case pages.

### Record the Refusal Code on an Unsuccessful Attempt

**ID:** `litespec/ext/audit#record-the-refusal-code-on-an-unsuccessful-attempt`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An audit record with a `denied` or `failed` outcome MUST carry the refusal `code` that the attempt produced.

**Rationale:** A refused permission and an exhausted precondition need different responses, but both look like failure without a code. The refusal `code` tells the reviewer which one the attempt produced. It also lets trend queries count one kind of refusal across the whole trail. The operation, the outcome, and the refusal code stay separate fields, so a reviewer reads a validation refusal as a failed attempt, not as a denied access.

**Reviewed for:** Does every denied or failed record carry its refusal code? Check the unsuccessful audit rows.

### Version the Audit Record Shape

**ID:** `litespec/ext/audit#version-the-audit-record-shape`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An audit record MUST carry an explicit schema version.

**Rationale:** A retained record outlives several shapes of the application that wrote it. An explicit version lets a later reader parse each record in the shape it was written in. Without it, every shape change forces a rewrite of retained evidence.

**Reviewed for:** Does every stored record carry an explicit schema version? Check the stored audit rows.

### Use a Closed Actor Set

**ID:** `litespec/ext/audit#use-a-closed-actor-set`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An audit actor MUST be one of `person`, `company`, `provider`, `system`, or `guest`.

**Rationale:** An absent actor collapses three answers into one: a scheduled process acted, a device acted, or no one recorded the actor, and only the third is a defect. The five kinds separate them: a device maps to the person whose account authorized it, with the device as the client description, and a named process maps to `system`. A free-text actor would let each writer spell the same actor differently and break every actor filter.

**Example:** A library self-checkout records the person actor whose account authorized it, while the origin record names the kiosk.

**Reviewed for:** Does every actor value use one of the five kinds, with devices and processes mapped? Check the actor column.

### Mark Actions Taken Under a Support Grant

**ID:** `litespec/ext/audit#mark-actions-taken-under-a-support-grant`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An action that an actor takes under an administrative or support grant MUST record that the grant was used.

**Rationale:** Support staff act inside customer data under a grant, and without a marker their actions read exactly like customer actions. The marker makes a no-silent-access commitment demonstrable: every supplier-side action says so on its own record. A reviewer who cannot separate the two cannot verify the commitment.

**Reviewed for:** Does every action taken under an administrative or support grant record that the grant was used? Check those records.

### Record Every Attempt Outcome

**ID:** `litespec/ext/audit#record-every-attempt-outcome`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An audit record MUST carry an outcome of `succeeded`, `denied`, or `failed`.

**Rationale:** A trail of successes is a change history, not evidence. The refused attempt carries the security signal, and the failed attempt carries the operating signal. Three outcomes keep both signals in the same trail as the successes they qualify. A validation refusal records `failed` with its refusal code; `denied` stays for a refused authorization, so recording a validation refusal as denied would accuse the actor of an access refusal they did not meet.

**Example:** `succeeded` names a committed change, `denied` names a refused authorization, and `failed` names a permitted attempt that did not complete. In Claimwick, an expense refused for a missing receipt records `failed` with the receipt refusal code, while an approval by an actor without the permission records `denied` with the forbidden code.

**Reviewed for:** Does every record carry a `succeeded`, `denied`, or `failed` outcome? Check the outcome column.

### Record Refused Authorization

**ID:** `litespec/ext/audit#record-refused-authorization`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An authorization decision that refuses an audited action MUST produce an audit record.

**Rationale:** Repeated refusal is the first indicator of a compromised or probing account, and no other record holds it: nothing changed, so nothing else was written. Each refusal therefore writes its own record. A trail without refusals shows an attacker who never failed.

**Reviewed for:** Does every refused authorization produce a denied record? Check the refusal paths beside the trail.

### Record Failed Audited Attempts

**ID:** `litespec/ext/audit#record-failed-audited-attempts`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An audited action that the actor was permitted to take and that did not complete MUST produce an audit record.

**Rationale:** A permitted attempt that throws is neither a success nor a refusal, and without a record it vanishes entirely. Writing it separates an operating defect from an access problem: failures cluster around broken behavior, denials cluster around missing permission. Support reads that split before opening the logs.

**Reviewed for:** Does every permitted attempt that threw produce a failed record? Check the error paths beside the trail.

### Record the Request Origin

**ID:** `litespec/ext/audit#record-the-request-origin`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An audit record produced from an inbound request MUST carry the request origin address, the client description, and the request identifier.

**Rationale:** Two records naming one account state nothing about location. The same account acting from two countries within one minute states that the account is compromised, but only when each record carries origin, client, and request identifier. The request identifier also joins the record to its exact request in the diagnostic log.

**Reviewed for:** Does every record produced from an inbound request carry origin, client, and request identifier? Check the inbound-request audit rows.

### Link Each Record to Its Operation Trace

**ID:** `litespec/ext/audit#link-each-record-to-its-operation-trace`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An audit record MUST carry the trace identifier of the operation that produced it.

**Rationale:** The audit record states what was attempted, and the diagnostic log states what the software did while attempting it. The trace identifier joins the two, so an investigation moves from evidence to detail in one step. Without it, the investigation stops at the audit record and reconstructs the rest by time.

**Reviewed for:** Does every stored record carry the trace identifier of its operation? Check the stored audit rows.

### Keep Secrets and Payment Data from the Trail

**ID:** `litespec/ext/audit#keep-secrets-and-payment-data-from-the-trail`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An audit record MUST NOT contain a secret, as `litespec/quality/security#keep-secrets-out-of-tracked-and-observable-data` states, or a payment instrument value.

**Rationale:** The trail is retained, widely readable, and append-only, which is the worst combination for a secret: retention extends the exposure and append-only removes the repair. A credential or payment value written once stays readable for the whole retention period. Secrets and payment values therefore travel in the systems built to hold them, never in the trail.

**Reviewed for:** Does any stored record contain a secret or a payment instrument value? Check the stored audit rows.

### Limit Personal Data to Actor and Target Identifiers

**ID:** `litespec/ext/audit#limit-personal-data-to-actor-and-target-identifiers`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An audit record MUST NOT contain personal data other than the pseudonymous identifier of the actor and the target.

**Rationale:** A record holding a name, an address, or a message body turns retention into an erasure conflict: the law asks for deletion and the trail asks for keeping. Pseudonymous identifiers keep the record meaningful while the mapping lives, and erasable once the mapping goes. The record then survives erasure instead of conflicting with it.

A pseudonymous identifier is still personal data where the holder can reverse it, as [the EDPB states](https://edpb.europa.eu/our-work-tools/our-documents/guidelines/guidelines-012020-processing-personal-data-context-art-6-1). The identifier is therefore safe inside the tenant that owns the mapping, and it becomes a disclosure when it leaves that tenant.

**Example:** An audit export crossing a tenant boundary replaces each identifier with one derived for that export, so the receiving party cannot join the records back to a subject.

**Reviewed for:** Does any stored record hold personal data beyond the pseudonymous actor and target identifiers? Check the stored audit rows.

### Keep State Copies from Audit Records

**ID:** `litespec/ext/audit#keep-state-copies-from-audit-records`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An audit record MUST NOT contain a copy of the state before or after the audited change.

**Rationale:** The domain event stream already holds each change under its own retention. Copying before and after state into the trail duplicates that storage and widens every record with data the reviewer did not ask for. The trail adds the actor and the reason value, which nothing else holds, and leaves the change itself to the stream.

**Reviewed for:** Does any stored record contain a before or after state copy? Check the stored audit rows.

### Commit a Success Record with Its Business Change

**ID:** `litespec/ext/audit#commit-a-success-record-with-its-business-change`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An audit record with a `succeeded` outcome MUST commit in the same database transaction as the change it describes.

**Rationale:** One transaction makes the action and its evidence inseparable: the change commits with its record or neither commits. Separate writes admit both an unrecorded action and a record of a rolled-back action. Either one leaves the trail disagreeing with the data it describes.

**Reviewed for:** Does every succeeded record commit inside its business transaction? Check the success write path.

### Write an Unsuccessful Record Outside the Failed Transaction

**ID:** `litespec/ext/audit#write-an-unsuccessful-record-outside-the-failed-transaction`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An audit record with a `denied` or `failed` outcome MUST be written on a connection that the failing transaction does not roll back.

**Rationale:** A denied or failed attempt ends with its transaction rolling back. A record written inside that transaction would roll back with it and vanish. The record therefore travels on a connection the rollback does not touch, so the evidence of a refusal survives the rollback.

**Reviewed for:** Does any denied or failed record roll back with its transaction? Check the unsuccessful write path.

### Stage the Audit Write Before the Business Commit

**ID:** `litespec/ext/audit#stage-the-audit-write-before-the-business-commit`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An audit producer that stages a success record MUST run before the pipeline stage that commits the business transaction.

**Rationale:** A success record staged after the commit reaches a transaction that is already closed, so the write cannot commit. The producer therefore runs before the pipeline stage that commits. Pipeline priority states that order outright instead of leaving it to registration order.

**Reviewed for:** Does any audit stage run after the business commit stage? Check the pipeline stage order.

### Publish Unsuccessful Write Failures

**ID:** `litespec/ext/audit#publish-unsuccessful-write-failures`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An audit deployment MUST publish the write failure count of its unsuccessful-outcome path.

**Rationale:** The separate connection for unsuccessful records can lose a record when the process dies between the refusal and the write. Steady loss is a defect, and only a published count distinguishes it from a rare loss. The deployment publishes that count so monitoring can tell the two apart.

**Reviewed for:** Does the deployment publish a write failure count for the unsuccessful path? Check the published metrics.

### Scope Every Record to Its Tenant

**ID:** `litespec/ext/audit#scope-every-record-to-its-tenant`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every audit record MUST carry the scope it belongs to: an action inside tenant-owned data carries the identity of that tenant, and an authentication event that belongs to no tenant lives in the global trail apart from tenant activity.

**Rationale:** The tenant is the filter for every read of the trail: each customer reads its own records and no one else's. A record without a tenant identity serves an internal reviewer and no customer. Scoping every record at write time keeps that filter exact. Authentication events belong to no tenant: a sign-in attempt names an account, not a tenant, so those events live in the global trail apart from tenant activity, where a reviewer reads sign-in evidence without opening any tenant's data.

**Reviewed for:** Is every record scoped to the right trail, with tenant data carrying its tenant identity and authentication events in the global trail? Check the tenant column and the global trail.

### Enforce Append-Only Through Storage Privilege

**ID:** `litespec/ext/audit#enforce-append-only-through-storage-privilege`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An audit store MUST refuse update and delete through the privileges granted to the writing account.

**Rationale:** An audit store that relies on absent code paths stays append-only until someone writes the missing path. Revoked privileges refuse update and delete no matter what the code tries. A reviewer verifies a privilege in the database instead of auditing every line for a write that should not exist.

**Example:** The application account holds `INSERT` and `SELECT` on the audit table, and it holds neither `UPDATE` nor `DELETE`.

**Reviewed for:** Does any writing account hold update or delete on the audit store? Check the granted store privileges.

### Chain Records to Expose Tampering

**ID:** `litespec/ext/audit#chain-records-to-expose-tampering`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An audit store MUST chain each record to its predecessor with a SHA-256 hash over a canonical serialization of both.

**Rationale:** The hash chain detects an edit and a removal, because a changed or missing record breaks every value after it. Detection is the obligation: it lets a reviewer prove the trail is intact. Prevention at a higher cost is not required.

The algorithm is named because two implementations that each choose their own cannot verify each other. A verifier written later cannot recompute a chain whose algorithm no one recorded. Canonical serialization is named for the same reason. A hash over a serialization that reorders fields verifies nothing.

**Example:** The chain value is `SHA-256(previousHash || canonical(record))`, where `canonical` sorts object members and encodes every timestamp and number one fixed way.

**Reviewed for:** Does every stored record carry the chained SHA-256 hash over the canonical serialization? Check the stored chain values.

### Anchor the Audit Chain Outside Its Own Store

**ID:** `litespec/ext/audit#anchor-the-audit-chain-outside-its-own-store`

**Strength:** advisory

**Applies to:** artifact: job

**Statement:** An audit store SHOULD publish a periodic checkpoint of its chain head to a destination that the store's own operators cannot rewrite.

**Deviation:** A store whose recorded threat model excludes its own operators publishes no checkpoint.

**Rationale:** A chain proves that records agree with each other, but anyone holding the store can recompute the whole chain and keep it agreeing. A checkpoint held somewhere the operators cannot rewrite fixes the chain as it stood at that moment. A later recomputation disagrees with the checkpoint, which exposes it.

**Example:** The checkpoint is the chain head and its record count, published at a stated interval. A separate append-only destination, a timestamping authority, and a transparency log each qualify.

**Reviewed for:** Does the store publish a checkpoint where its own operators cannot rewrite it? Check the checkpoint destination and the threat model.

### Verify the Integrity Chain on a Schedule

**ID:** `litespec/ext/audit#verify-the-integrity-chain-on-a-schedule`

**Strength:** required

**Applies to:** artifact: job

**Statement:** An audit deployment MUST verify its integrity chain on a recurring schedule and raise an alert on a break.

**Rationale:** Tamper evidence that no one checks proves nothing: a break sits in the store until someone looks. Scheduled verification turns the chain into a standing claim the deployment re-proves on each run. Verification recomputes the chain and checks its head against the external checkpoint, so it detects a changed or truncated tail since the last checkpoint. The alert carries the break to an operator instead of waiting for the next review.

**Reviewed for:** Does the deployment verify its chain on a schedule and alert on a break? Check the integrity job and its alerts.

### Correct Mistakes with a New Record

**ID:** `litespec/ext/audit#correct-mistakes-with-a-new-record`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** A correction to the trail MUST be a new record that references the earlier one.

**Rationale:** An edited history carries no evidential value, because the reviewer cannot tell a correction from a cover-up. A new record that references the earlier one keeps both visible: the mistake and its fix. The trail then shows its own repairs instead of hiding them.

**Reviewed for:** Is every correction a new record that points at the earlier one? Check the correction records.

### Delete Records When Their Retention Ends

**ID:** `litespec/ext/audit#delete-records-when-their-retention-ends`

**Strength:** required

**Applies to:** artifact: job

**Statement:** An audit deployment MUST delete a record after the retention period declared for its category.

**Rationale:** Each category declares its retention in the adoption decision, and the deployment deletes records when that period ends. Deleting on schedule keeps retention defensible: everything kept has a declared reason, and nothing kept lacks one. An indefinite period is the hardest retention to defend, and the easiest to fix while the design is still open.

**Reviewed for:** Does any stored record outlive its category's declared retention? Check the stored rows against the adoption decision.

### Remove the Identity Mapping on Erasure

**ID:** `litespec/ext/audit#remove-the-identity-mapping-on-erasure`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** An erasure request MUST remove the mapping from actor identifier to person and retain every audit record.

**Rationale:** Erasure asks that the person stop being identifiable, while accountability asks that the record stay. Removing the mapping from identifier to person satisfies both: the record keeps its evidential value and no longer points at anyone. This works only because the record carries no other personal data.

**Reviewed for:** Does every erasure remove the actor mapping while keeping each audit record? Check the erasure handling and the retained rows.

### Provide a Use Case That Reads the Trail

**ID:** `litespec/ext/audit#provide-a-use-case-that-reads-the-trail`

**Strength:** required

**Applies to:** artifact: query-handler

**Statement:** An audited system MUST provide a trail read as a use case with its access grant, filtered by tenant, actor, action, target, and outcome.

**Rationale:** A trail no use case reads is storage, not a control. The read path is a use case with its own access grant: the actor holds a role whose scope reaches the records, the role allows the permission, and the grant permits the read. Filters on tenant, actor, action, target, and outcome keep each reader inside the records they may see.

**Reviewed for:** Does the trail read carry its access grant and its declared filters? Check the trail read use case.

### Audit Reads of the Trail

**ID:** `litespec/ext/audit#audit-reads-of-the-trail`

**Strength:** required

**Applies to:** artifact: query-handler

**Statement:** A read of the audit trail MUST itself produce an audit record.

**Rationale:** Reading the evidence is itself a sensitive action: the trail shows who did what across the whole product. A read that writes no record leaves the reviewer outside the trail. Recording each read puts the reviewer inside the same accountability as everyone else.

**Reviewed for:** Does every completed trail read produce an audit record? Check the trail read path.

### Record Personal-Data Reads

**ID:** `litespec/ext/audit#record-personal-data-reads`

**Strength:** required

**Applies to:** artifact: query-handler

**Statement:** A query returning personal data across a boundary MUST produce an audit record, defining the account boundary in the adoption decision where tenancy is absent.

**Rationale:** A read changes no state and therefore leaves no other trace, so a disclosure without a record cannot be investigated. The audit record on a cross-boundary personal-data query states who read whose data and when. Where tenancy is absent, the adoption decision defines the account boundary this rule watches.

**Reviewed for:** Does every cross-boundary personal-data query produce a record? Check the query paths beside the trail.

### Audit Bulk Reads Inside One Boundary

**ID:** `litespec/ext/audit#audit-bulk-reads-inside-one-boundary`

**Strength:** required

**Applies to:** artifact: query-handler

**Statement:** A query returning personal data for more subjects than its declared threshold MUST produce an audit record, inside one tenant or account boundary as well.

**Rationale:** The cross-boundary rule catches a disclosure to the wrong party, but it misses an export of every record a party may read one at a time, which is the shape a copied dataset has. A query above the declared threshold therefore writes a record even inside one boundary. [Article 30 of the GDPR](https://gdpr-info.eu/art-30-gdpr/) asks what categories of data were processed and by whom, and an unrecorded bulk read answers neither.

**Example:** Claimwick records the threshold in its adoption decision. A list endpoint whose page size can exceed it audits the read with the subject count rather than naming each subject.

**Reviewed for:** Does every above-threshold query produce a record with the subject count? Check the bulk query paths.

### Audit Every Bulk Export

**ID:** `litespec/ext/audit#audit-every-bulk-export`

**Strength:** required

**Applies to:** artifact: query-handler

**Statement:** An export that returns a set of records to a caller MUST produce an audit record naming the requested scope.

**Rationale:** Bulk extraction is the highest-consequence read in most products: one call moves a dataset across the boundary. Naming the requested scope states what left the system. Without it, the project learns the shape of a leak from the party that received it.

**Reviewed for:** Does every export produce a record naming the requested scope? Check the export paths.

### Fail the Build for Commands Without an Audit Declaration

**ID:** `litespec/ext/audit#fail-the-build-for-commands-without-an-audit-declaration`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An automated check MUST fail when a Command declares neither an audit position nor an exemption, with a composition refusal at startup satisfying this rule.

**Rationale:** The next Command is written by someone who did not read this page. Only an automated gate catches it: the build or startup fails while the declaration is missing. A selection that depends on every author remembering has gaps.

**Reviewed for:** Does any Command build or start without an audit position or exemption? Check the build or startup gate.

## Conventions

### Keep Audit Infrastructure in One Location

**ID:** `litespec/ext/audit#keep-audit-infrastructure-in-one-location`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep the audit record, its store, the producer, and the integrity job under `Infrastructure/Auditing/`.

**Replacement:** A consumer records an audit-layout convention naming where its audit parts live.

**Rationale:** The record, the store, the producer, and the integrity job form one evidence mechanism. One Infrastructure location keeps that mechanism reviewable as a whole instead of scattered across the codebase.

**Reviewed for:** Does every audit part live in the documented location or the recorded local convention? Check under `Infrastructure/Auditing/`.

### Declare Audit Details on the Command Type

**ID:** `litespec/ext/audit#declare-audit-details-on-the-command-type`

**Strength:** default

**Applies to:** artifact: command-handler

**Default:** Declare the action, target kind, category, and reason selector as an attribute on the Command type, beside the use-case `audit` field.

**Replacement:** A consumer records a selection-declaration convention naming where each Command declares its audit selection.

**Rationale:** The declaration sits with the code it describes, so a review reads both at once. The use-case page carries the audit position or exemption, while the attribute carries the action, target kind, and category the producer writes. The reason selector names the message field holding the reason value, so the pipeline reads the declared reason without handler help. A selection kept in a separate file drifts from the Commands it names.

**Reviewed for:** Does every Command carry the declaration attribute with its action, target kind, category, and reason selector, or a recorded local convention? Check the Command types.

### Use a Fixed Category Vocabulary

**ID:** `litespec/ext/audit#use-a-fixed-category-vocabulary`

**Strength:** default

**Applies to:** artifact: command-handler

**Default:** Classify each audited action as `authentication`, `authorization-denial`, `grant-change`, `monetary-action`, `tenant-configuration`, `personal-data-read`, `bulk-export`, or `background-process-change`.

**Replacement:** A consumer records a category convention naming the audit categories it uses.

**Rationale:** The category drives retention and the review query. A free-text value defeats both, because each writer spells the same category differently.

**Reviewed for:** Does every category value use the eight-value vocabulary or the recorded local convention? Check the declared selection.

### Store the Trail Beside the Business Data

**ID:** `litespec/ext/audit#store-the-trail-beside-the-business-data`

**Strength:** default

**Applies to:** artifact: any

**Default:** Store audit records in the business database, under a separate account holding `INSERT` and `SELECT` privileges.

**Replacement:** A consumer records a trail-store convention naming where its audit records live.

**Rationale:** One database provides the atomic success write the transaction rules depend on. A separate account holding only `INSERT` and `SELECT` provides the append-only guarantee.

**Reviewed for:** Does the trail live in the business database or the recorded local convention? Check the audit store and its privileges.

### Publish a Copy to the Security Platform

**ID:** `litespec/ext/audit#publish-a-copy-to-the-security-platform`

**Strength:** default

**Applies to:** artifact: any

**Default:** Publish a copy of each record to an external security platform through the outbox, and keep the local store authoritative.

**Replacement:** A consumer records a copy-delivery convention naming how audit copies leave the local store.

**Rationale:** Delivery is at-least-once, while evidence needs one durable source. The local store stays authoritative for accountability and the published copy serves detection, so a lost copy never corrupts the evidence.

**Reviewed for:** Does the local store stay authoritative, with each copy leaving through the recorded convention? Check the delivery path.

## Dependencies

Packages: None.

Related extensions: `tenancy` for tenant resolution, `outbox` for copy delivery.

Guides: [Build an audit trail](../guide/audit-trail.md).
