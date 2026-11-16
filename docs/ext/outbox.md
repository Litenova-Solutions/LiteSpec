# Durable Delivery

**Stack:** any

**Concern:** infrastructure

Durable delivery carries committed changes to their readers through the outbox: the product stages each after-commit effect in the same transaction as its changes, and a Worker delivers it later. Facts between modules of one product travel in process through the outbox and need no broker, while facts for outside systems leave through a transport. These rules apply to delivery that must survive a process failure.

## Activation

Activation scope: `local`.

Applicable specification kinds: `use-case`.

Select this extension on a use case whose committed change needs delivery that survives a process failure. That delivery can carry a module event to another module, an integration event to another system, a command that advances a flow step, or a side effect on an outside provider. Selecting it also activates `litespec/backend/architecture#name-the-host-for-work-that-outlives-a-request`, so the project declares the execution host that runs the dispatcher.

## Baseline Relationship

This extension replaces `litespec/backend/persistence#collect-events-without-a-public-unit-of-work`.

It switches on no optional record type: integration events, module events, settings, and runbooks belong to the core. The project writes an integration-event record for each integration event it delivers and a module-event record for each module event it delivers, setting records for the delivery bounds, and runbook records for recovery and rollback.

## Standards

### Stage After-Commit Effects in the Outbox

**ID:** `litespec/ext/outbox#stage-after-commit-effects-in-the-outbox`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** A use case MUST stage each domain event its act raises, each module event translating one of them, and each integration event translating one of them, in the outbox in the same transaction as its changes.

**Rationale:** A raised fact means what follows it runs: staged in the same transaction, the change and its message commit or roll back together. One global pipeline step stages each raised domain event for the reactions of its module, each module event whose record translates it, and each integration event whose record translates it, each built from the domain event alone, as `litespec/backend/application#stage-declared-module-and-integration-events-in-the-commit` requires. No user code stages or publishes, so this step is the only code that stages. A message sent from memory instead would be lost on a crash between the commit and the send, and every follow-on act runs from a reaction to a staged fact.

**Reviewed for:** Does every raised domain event and translated module and integration event stage in the transaction of its aggregate changes, through the one global step? Check the pipeline's staging step and the outbox writes beside it.

### Deliver Module Facts in Process Without a Broker

**ID:** `litespec/ext/outbox#deliver-module-facts-in-process-without-a-broker`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Facts between modules of one product MUST travel in process through the outbox, with no broker.

**Rationale:** A module event is a contract between modules of one product, delivered from the same database that holds the business work. A broker between modules adds transport, operations, and failure modes for a hop that never leaves the process. An integration event still crosses the bounded context through the transport its record names in `through`.

**Reviewed for:** Does any delivery between modules of one product leave the process or name a broker? Check the dispatch transports beside the module events.

### Dispatch Projections Immediately After Commit

**ID:** `litespec/ext/outbox#dispatch-projections-immediately-after-commit`

**Strength:** required

**Applies to:** artifact: reaction

**Statement:** A projection a caller reads when the command returns MUST dispatch in process after the commit, with the outbox processor retrying whatever the immediate dispatch misses.

**Rationale:** A read-model projection the next screen reads is visible when the command returns, but a dispatch without retry loses the projection on a crash. An immediate in-process dispatch after the commit gives read-your-writes for projections, and the outbox processor stays the fallback, so retry is never given up. The command's outcome never changes on a failed immediate dispatch.

**Reviewed for:** Is every projection the caller reads dispatched in process after the commit with the processor as fallback? Check each such projection beside the dispatch registration.

### Declare Published Facts as Integration Events

**ID:** `litespec/ext/outbox#declare-published-facts-as-integration-events`

**Strength:** required

**Applies to:** artifact: record:integration-event

**Statement:** Each integration event the outbox delivers MUST have an integration-event record carrying direction, payload, trust and, for an outbound event, the domain event it translates.

**Rationale:** A published fact without a record leaves readers depending on fields no page states, and a field change breaks them without a diff. The integration-event record states the direction, the payload with its classifications, and the trust readers place in it. The [edges page](../core/edges.md) states the declaration and the record.

**Reviewed for:** Does every delivered integration event have a record carrying direction, payload, trust, and, for an outbound event, `translates`? Check each event type beside its record.

### Keep Published Events Available for Read Model Rebuilds

**ID:** `litespec/ext/outbox#keep-published-events-available-for-read-model-rebuilds`

**Strength:** required

**Applies to:** artifact: record:read-model, record:integration-event, record:module-event

**Statement:** A project MUST keep every module or integration event a read model names in `from`, `erasedBy` or `removedBy` apart from the outbox's delivery records for as long as a read model may need rebuilding from it, with erased personal fields removed or the event key shredded.

**Rationale:** A new read model, or a changed one, fills from the events the owner already published. An outbox that deletes delivered records after a few days leaves a new read model nothing to replay, and every new read model then needs a hand-written backfill from the owner's tables, which is the coupling the read model exists to remove. Keeping the published events in a log apart from delivery lets a projection replay history.

The log holds personal fields, so the erasing event a read model names in `erasedBy` also erases the field in the log. A retained event keeps its shape with the erased personal fields removed, or with its key shredded, so no rebuild restores them. The read model keeps an erasure tombstone for the erased key and applies it on rebuild, under `litespec/core/automation#erase-a-read-model-personal-field-with-its-owner`. Settled by [decision/0065-read-another-module-through-a-read-model](decision/0065-read-another-module-through-a-read-model).

**Example:** In Claimwick, the reporting module rebuilds its reimbursed-expense read model by replaying the kept expense-reimbursed module events, while the outbox deletes its delivery records after seven days. A retained event for an erased expense replays without the erased personal fields.

**Reviewed for:** Is every module or integration event a read model names kept apart from delivery for as long as a rebuild may need it, with erased fields removed or the key shredded? Check the event log's retention beside the read models that read it.

### Document Delivery Guarantees Before Sending

**ID:** `litespec/ext/outbox#document-delivery-guarantees-before-sending`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A durable-delivery specification MUST document delivery guarantee, downstream boundary, duplicate behavior, retry horizon, and recovery runbook.

**Rationale:** Delivery crosses a system boundary, so the team agrees before sending on the guarantee, the boundary, the duplicate behavior, the retry horizon, and the runbook that recovers when delivery fails. Without that agreement, the writer assumes once-only delivery while the target assumes retries, and both discover the mismatch in production. The agreement comes before the first message.

**Reviewed for:** Does every durable-delivery specification document its guarantee, downstream boundary, duplicate behavior, retry horizon, and recovery runbook? Check each specification.

### Skip the Outbox for Manually Repeatable Work

**ID:** `litespec/ext/outbox#skip-the-outbox-for-manually-repeatable-work`

**Strength:** required

**Applies to:** artifact: reaction, job

**Statement:** A project MUST NOT activate outbox for a reaction or job whose loss the operators accept and which they can repeat manually.

**Rationale:** Outbox storage, dispatch, retries, and replay exist to prevent loss. A reaction or job whose loss the operators accept, and which they can repeat by hand, needs none of that machinery. Its `onFailure` states how failures are handled, and the use case page records that the loss is accepted.

**Reviewed for:** Does any reaction or job whose loss the operators accept still carry outbox records? Check the outbox records and the reactions and jobs they name.

### Emit Excluded Reactions and Jobs After Commit

**ID:** `litespec/ext/outbox#emit-excluded-reactions-and-jobs-after-commit`

**Strength:** required

**Applies to:** artifact: reaction, job

**Statement:** The facts raised by the use case a reaction or job the outbox excludes runs MUST be published after that use case's commit succeeds, without staging an outbox record.

**Rationale:** Without an outbox record, nothing survives a crash between the commit and the publish, so the pipeline publishes the raised facts right after the commit succeeds and accepts the loss. Publishing before the commit would be worse: it would fire for a change the database then rolls back. The reaction or job itself publishes nothing; the pipeline's staging step does.

**Reviewed for:** Does every fact raised under an excluded reaction or job publish after the commit succeeds, with no outbox record staged? Check the pipeline's publish step for the use cases those reactions and jobs run.

### Stage Messages with Business Work

**ID:** `litespec/ext/outbox#stage-messages-with-business-work`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A command post-handler MUST stage pending durable messages in the same scoped business session as its aggregate changes.

**Rationale:** The business change and its message commit together: both persist or neither does. Staging the message in the same scoped business session as the aggregate changes puts both under one commit, so a crash can never leave an accepted change without its message or a message without its change.

**Reviewed for:** Does every pending durable message stage inside the scoped session of its aggregate changes? Check each command post-handler and its session.

### Serialize Durable Messages

**ID:** `litespec/ext/outbox#serialize-durable-messages`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An outbox writer MUST serialize each pending durable message into an outbox record.

**Rationale:** The command scope ends when the request ends. Dispatch happens later, in a Worker that runs after the request ends. Serializing the message into an outbox record gives that Worker everything it needs once the scope is gone.

**Reviewed for:** Does every pending durable message reach a serialized outbox record? Check the outbox writer and its record type.

### Store Required Message Fields

**ID:** `litespec/ext/outbox#store-required-message-fields`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An outbox record MUST contain message identity, kind, stable type, schema version, time, payload, tenant, state, attempt, and next-attempt data.

**Rationale:** Three readers share the record: the dispatcher, the retry logic, and the operator. Identity and type route the message, the schema version picks its reader, time and tenant scope it, and state, attempt, and next-attempt data drive the retry. A field stored nowhere forces one of those readers to guess.

**Reviewed for:** Does every outbox record carry identity, kind, type, schema version, time, payload, tenant, state, attempt, and next-attempt data? Check the outbox record type.

### Store Failure and Completion Evidence

**ID:** `litespec/ext/outbox#store-failure-and-completion-evidence`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An outbox record MUST retain its last error summary and completion time.

**Rationale:** When delivery stalls, the operator opens the record first. The last error summary says why the previous attempt failed, and the completion time says which records finished. Without that evidence on the record, each investigation starts from scattered logs instead.

**Reviewed for:** Does every outbox record keep its last error summary and completion time? Check the record type and its update code.

### Classify Message Kinds

**ID:** `litespec/ext/outbox#classify-message-kinds`

**Strength:** required

**Applies to:** artifact: record:integration-event, record:module-event

**Statement:** An outbox record MUST name its message kind as module event, integration event, flow-step command, or provider side effect, with a module event citing its module-event record and an integration event citing its integration-event record.

**Rationale:** Each kind dispatches through its own owner: a module event in process to its subscribed handlers, an integration event through its transport, a flow-step command through the mediator, and a provider side effect through its integration, so the record names which one it holds. The module event cites its module-event record and the integration event cites its integration-event record, so the staged message traces to the published fact it carries.

**Reviewed for:** Does every outbox record name its message kind, with each module and integration event citing its record? Check the record type and the messages staged.

### Reject Independent Outbox Commits

**ID:** `litespec/ext/outbox#reject-independent-outbox-commits`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An outbox writer MUST NOT use another connection or independent commit for required durable delivery.

**Rationale:** Two commits on two connections can split around a crash: the business change persists while the message rolls back, or the message persists for a change that never happened. Sharing one PostgreSQL database does not make separate transactions atomic, so the message joins the business transaction instead.

**Reviewed for:** Does any outbox writer use a separate connection or commit? Check each writer source and its path into the business session.

### Dispatch Claimed Records in Worker

**ID:** `litespec/ext/outbox#dispatch-claimed-records-in-worker`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A Worker MUST claim pending outbox records, dispatch a bounded batch, and record success or retry information.

**Rationale:** Dispatch needs time, retries, and network calls that no request transaction can hold. The Worker claims pending records, dispatches a bounded batch, and records each outcome. The request then commits fast while delivery proceeds on its own schedule.

**Reviewed for:** Does every Worker loop dispatch a bounded batch and record each outcome? Check the dispatcher's claim and batch code.

### Exclude Request-Transaction Dispatch

**ID:** `litespec/ext/outbox#exclude-request-transaction-dispatch`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** WebApi MUST NOT dispatch a durable message inside the request transaction.

**Rationale:** A network call inside the request transaction holds database locks while it waits for the target, and a slow target slows every request behind it. The request commits only to make the message eligible. The Worker performs the delivery later.

**Reviewed for:** Does any request path dispatch a durable message inside the request transaction? Check the endpoint and command handler paths.

### Define Claim Ownership

**ID:** `litespec/ext/outbox#define-claim-ownership`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An outbox claim MUST use a short transaction with the lease owner, lease expiry, and fencing value that `litespec/ext/jobs#store-execution-leases` defines.

**Rationale:** Several Workers poll the same store, so each dispatch attempt needs one owner. The lease owner, the lease expiry, and the fencing value name that owner. The short transaction takes the claim without holding locks into the delivery itself.

**Reviewed for:** Does every claim take an owner, expiry, and fencing value inside a short transaction? Check the claim code in the outbox store.

### Release Claim Transactions Before Calls

**ID:** `litespec/ext/outbox#release-claim-transactions-before-calls`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A Worker MUST NOT hold an outbox claim transaction open during a network call.

**Rationale:** A network call to a slow target can last seconds or minutes. Holding the claim transaction open across that call pins database locks and connection slots, starving the Workers behind it. Releasing the claim first lets the database serve other work while the call runs.

**Reviewed for:** Does any claim transaction stay open across a network call? Check the Worker's dispatch path.

### Restrict Completion to Current Lease Owners

**ID:** `litespec/ext/outbox#restrict-completion-to-current-lease-owners`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A Worker MUST update an outbox record only while it owns the matching lease and fencing value.

**Rationale:** A Worker that lost its lease through a crash, a pause, or an expiry can wake up after another Worker claimed the same record. Checking the lease and fencing value before each update stops the stale Worker from overwriting the current owner's result.

**Reviewed for:** Does every record update check the current lease and fencing value? Check each update statement in the outbox store.

### Requeue Expired Claims

**ID:** `litespec/ext/outbox#requeue-expired-claims`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An outbox store MUST return expired claims to eligible work.

**Rationale:** A Worker can die between claiming a record and dispatching it. Returning expired claims to eligible work lets a live Worker pick them up. One crash then delays a message instead of losing it.

**Reviewed for:** Does every expired claim return to eligible work? Check the requeue query in the outbox store and its tests.

### Accept Repeated Delivery

**ID:** `litespec/ext/outbox#accept-repeated-delivery`

**Strength:** required

**Applies to:** artifact: reaction

**Statement:** An event handler that receives an outbox delivery MUST handle the same message more than once.

**Rationale:** The target can accept a message while the Worker crashes before recording completion, and the requeued claim then dispatches it again. Delivery is at-least-once, so every event handler handles the same message more than once without corrupting its state.

**Reviewed for:** Does every event handler keep correct state when the same message arrives twice? Check the handler tests that deliver a message twice.

### Identify Repeated Delivery

**ID:** `litespec/ext/outbox#identify-repeated-delivery`

**Strength:** required

**Applies to:** artifact: reaction

**Statement:** An event handler that receives an outbox delivery MUST use message ID or a business idempotency key at its Command or side-effect boundary.

**Rationale:** Handling a repeat means recognizing it: the message ID or a business idempotency key gives the event handler a stable identity to check at its command or side-effect boundary. Without that key, the handler processes the repeat as new work and pays, notifies, or ships twice.

**Reviewed for:** Does every event handler check the message ID or business key at its Command or side-effect boundary? Check each handler's boundary.

### Give Each Handler Its Own Delivery

**ID:** `litespec/ext/outbox#give-each-handler-its-own-delivery`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The outbox MUST deliver an event to each of its handlers as a separate delivery with its own retries and dead letter.

**Rationale:** One delivery shared by all handlers retries every handler when one fails. Each handler therefore gets its own delivery with its own retries and dead letter. Each of those deliveries still ends in repeat-safe handling, under `litespec/ext/outbox#accept-repeated-delivery`.

**Example:** In the library product, a loan-borrowed event feeds a due-notice handler and a statistics handler. The notice delivery retries on its own schedule while the statistics delivery stays complete.

**Reviewed for:** Does every handler receive its own delivery with its own retries and dead letter? Check the outbox delivery records for each handler.

### Wait for Target Acceptance Before Completing

**ID:** `litespec/ext/outbox#wait-for-target-acceptance-before-completing`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A Worker MUST NOT mark an outbox record complete before the target boundary accepts it.

**Rationale:** Marking a record complete before the target accepts it declares a delivery that never happened. No retry follows, because the record looks done. The Worker therefore waits for the target boundary's acceptance before recording completion.

**Reviewed for:** Does any outbox record mark completion before the target accepts the message? Check the Worker's completion code and the target's acceptance response.

### Retry Delivery with Bounded Backoff and Jitter

**ID:** `litespec/ext/outbox#retry-delivery-with-bounded-backoff-and-jitter`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An outbox dispatcher MUST use bounded exponential backoff with jitter.

**Rationale:** A failing target with ten thousand pending messages meets a retry storm when every message retries at once on a fixed interval. Bounded exponential backoff with jitter spreads those retries over time and caps the total, so the target recovers instead of failing under the load.

**Reviewed for:** Does every retry schedule use a bound, backoff, and jitter? Check the dispatcher's retry code.

### Stop Retrying Permanent Failures at the Bound

**ID:** `litespec/ext/outbox#stop-retrying-permanent-failures-at-the-bound`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An outbox dispatcher MUST classify permanent failures, stop automatic retry at its retry bound, and retain safe investigation and replay data.

**Rationale:** Some failures never clear on retry: a deleted destination, a rejected payload, or a revoked credential. Retrying those forever wastes resources and hides the real backlog. The dispatcher classifies them, stops at its retry bound, and keeps the evidence an operator needs to investigate and replay safely.

**Reviewed for:** Does every permanent failure stop at its retry bound with its investigation and replay data kept? Check the dispatcher's failure classification and its tests.

### Report Store Outages Separately from Delivery Failures

**ID:** `litespec/ext/outbox#report-store-outages-separately-from-delivery-failures`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A dispatcher MUST distinguish an unavailable outbox store from a failed claimed message.

**Rationale:** A dead outbox store and a failed delivery need different recoveries: the first needs a database restored, the second needs one destination investigated. Each carries its own signal. The store's availability is a gauge the readiness endpoint reads, while a claimed message's outcome is a counter carrying the result, following [the OpenTelemetry metrics specification](https://opentelemetry.io/docs/specs/otel/metrics/). A single error counter cannot tell an operator whether to restore a database or investigate one destination.

**Reviewed for:** Does every outbox metric use the right instrument type for store availability and claimed-message outcomes? Check the metrics the outbox publishes.

### Reduce Polling When the Store Is Unavailable

**ID:** `litespec/ext/outbox#reduce-polling-when-the-store-is-unavailable`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A Worker MUST apply bounded backoff and rate-limited logging when its outbox store is unavailable.

**Rationale:** A Worker that polls a dead store on a tight loop writes an exception per iteration, filling disks and log quotas before anyone reads the first one. Bounded backoff with rate-limited logging keeps the Worker alive and quiet until the store returns.

**Reviewed for:** Does every Worker loop back off and rate-limit its logging when the store is unavailable? Check the Worker's poll loop and its outage tests.

### Restrict Message Retry to Claimed Records

**ID:** `litespec/ext/outbox#restrict-message-retry-to-claimed-records`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A Worker MUST apply message retry and poison handling only to claimed outbox records.

**Rationale:** Retry and poison handling act on a claimed message. When the store itself is down, no claim exists and no message waits for delivery. Running retry logic against the outage double-counts attempts and can dead-letter messages whose only fault was an unreachable store.

**Reviewed for:** Does any retry or poison-handling path act on a record that holds no claim? Check the dispatcher's retry and poison code.

### Stop Dispatch When the Host Is Not Ready

**ID:** `litespec/ext/outbox#stop-dispatch-when-the-host-is-not-ready`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A Worker host exposing readiness MUST gate dispatch and stop polling stores that cannot serve requests.

**Rationale:** A host that reports unready still serves traffic it cannot finish when its Workers keep polling dead stores. Gating dispatch on readiness stops that polling, so the host fails fast and recovers instead of polling storage that cannot serve requests.

**Reviewed for:** Does every unready host stop dispatching and polling stores that cannot serve requests? Check the readiness check and the Worker's dispatch loop.

### Version Message Types

**ID:** `litespec/ext/outbox#version-message-types`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An outbox message MUST use a stable type name and explicit schema version.

**Rationale:** An outbox record outlives the deployment that wrote it, so the dispatcher meets records written by several versions at once. A stable type name with an explicit schema version tells the dispatcher which reader parses each record. Without that pair, one shape change breaks every record written before it.

**Reviewed for:** Does every outbox message carry a stable type name and an explicit schema version? Check each outbox message type.

### Read Records from Every Coexisting Version

**ID:** `litespec/ext/outbox#read-records-from-every-coexisting-version`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A deployed dispatcher MUST process records from every application version that can coexist during rollout or rollback.

**Rationale:** During rollout and rollback, several application versions share one store and write different valid shapes into it. The dispatcher reads from every version that can coexist, or the deployment itself causes delivery failures.

Each message record therefore carries its own shape version, and the dispatcher picks a reader from that value. Adding an optional member with a defined absent-value behavior needs no new version. Removing a member, renaming one, narrowing a type, or changing a discriminator needs a new version, and the reader for each version stays until no record carries that version.

**Reviewed for:** Does the dispatcher hold a reader for every application version that can coexist? Check the dispatcher's version selection and its tests.

### Publish Backlog Indicators

**ID:** `litespec/ext/outbox#publish-backlog-indicators`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An outbox deployment MUST publish pending count, failed count, oldest pending age, attempts, dispatch duration, and success rate.

**Rationale:** Pending count, failed count, oldest pending age, attempts, dispatch duration, and success rate together show whether delivery keeps up. Growth in the backlog or its age reveals a stall, while duration and success rate reveal a struggling target. Without these indicators, the first sign of failure is a customer asking where their message went.

**Reviewed for:** Does the deployment publish all six backlog indicators? Check the metrics the Worker exports.

### Set Delivery Alerts

**ID:** `litespec/ext/outbox#set-delivery-alerts`

**Strength:** required

**Applies to:** artifact: record:setting

**Statement:** An outbox deployment MUST set alert thresholds as setting records from its use-case delivery target.

**Rationale:** A shipment notice delayed an hour and a receipt delayed a week carry different business consequences. Each use case therefore sets alert thresholds from its own delivery target. Setting records hold those thresholds, so the alert fires when a delay passes what the business accepted.

**Reviewed for:** Does every alert threshold sit in a setting record derived from a use-case delivery target? Check the setting records beside each use case.

### Model Outbox Lifecycle States

**ID:** `litespec/ext/outbox#model-outbox-lifecycle-states`

**Strength:** required

**Applies to:** artifact: state-record

**Statement:** An outbox record MUST model its lifecycle as an abstract state base with one sealed record for each of pending, processing, dispatched, and dead-letter.

**Rationale:** `pending` becomes `processing`, and `processing` becomes `dispatched`, `pending`, or `dead-letter`, while audited replay moves `dead-letter` back to `pending`. Three of the four states carry data the others do not: `processing` holds a lease expiry, `dispatched` holds a dispatch time, and `dead-letter` holds a failure reason and an attempt count. A four-value label forces those onto nullable columns that mean something in one state each. That is the shape `litespec/backend/domain#model-every-closed-set-as-a-type-hierarchy` removes everywhere else.

**Example:** `OutboxState.Processing` carries one unexpired lease before a dispatcher records `OutboxState.Dispatched` or a retry state.

**Reviewed for:** Does every outbox state have a sealed record under the abstract base with a documented transition? Check the outbox state types and their transitions.

### Preserve Message Identity During Retry

**ID:** `litespec/ext/outbox#preserve-message-identity-during-retry`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An outbox dispatcher MUST preserve message ID across retry and replay.

**Rationale:** The target suppresses a repeat by recognizing its identity. That works only when every retry and replay carries the same message ID. A dispatcher that mints a new ID per attempt turns each retry into a new message the target processes again.

**Reviewed for:** Does any retry or replay path mint a new message ID? Check the dispatcher's retry and replay code.

### Audit Outbox Replay

**ID:** `litespec/ext/outbox#audit-outbox-replay`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An outbox replay MUST record actor, reason, time, and failure without editing the payload: an audit record when audit is selected, a standalone record otherwise.

**Rationale:** A replay dispatches a message the system already handled or dead-lettered. The team records who replayed it, why, when, and which failure it addresses. Without that record, a duplicate delivery looks like a dispatcher bug rather than an operator's decision, and no one can reconstruct the choice.

**Reviewed for:** Does every replay record its actor, reason, time, and failure without editing the payload? Check the replay code and its audit records.

### Protect Active Worker Compatibility

**ID:** `litespec/ext/outbox#protect-active-worker-compatibility`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** New WebApi code MUST emit a message only after every active Worker can read its type and schema version.

**Rationale:** WebApi writers and Worker dispatchers deploy independently, so a writer can run ahead of the oldest active Worker. When the writer emits a shape that Worker cannot read, those messages dead-letter on arrival even though their shape is valid.

**Reviewed for:** Can every active Worker read the type and schema version of each new message the WebApi emits? Check the Worker's readers against those messages.

### Deploy Readers Before Writers

**ID:** `litespec/ext/outbox#deploy-readers-before-writers`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release MUST deploy compatible readers before writers for additive event versions.

**Rationale:** In a mixed-version rollout, old and new records sit in one store. Deploying compatible readers before the writers that emit new shapes keeps every record dispatchable at each step. The reverse order leaves new records without a reader until the readers catch up.

**Reviewed for:** Does every release plan deploy compatible readers before its additive writers? Check the deployment order in the release runbook.

### Cover Undispatched Records in the Rollback Runbook

**ID:** `litespec/ext/outbox#cover-undispatched-records-in-the-rollback-runbook`

**Strength:** required

**Applies to:** artifact: record:runbook

**Statement:** A rollback Runbook MUST include records created by the new writer but not yet dispatched.

**Rationale:** Rolling back the deployment does not roll back the store. Records the new writer staged but no one dispatched stay behind. The rollback Runbook names how the previous version handles them. Without that plan, they sit undispatched with no owner and no reader.

**Reviewed for:** Does every rollback runbook cover the records the new writer staged but no one dispatched? Check each rollback runbook record.

## Conventions

### Group Outbox Infrastructure

**ID:** `litespec/ext/outbox#group-outbox-infrastructure`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep outbox record, storage, claim, and dispatch infrastructure under `Infrastructure/Messaging/Outbox/`.

**Replacement:** A project names another home for outbox infrastructure in its documentation.

**Rationale:** The outbox record, storage, claim, and dispatch code form one mechanism with one owner. Keeping it under `Infrastructure/Messaging/Outbox/` keeps every persistent dispatch file in one folder instead of scattered across the project.

**Reviewed for:** Does every outbox infrastructure file sit under `Infrastructure/Messaging/Outbox/` or another home named in the documentation? Check the outbox code folders and the project's documentation.

### Isolate the Worker Host

**ID:** `litespec/ext/outbox#isolate-the-worker-host`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep the outbox Worker host in its own project.

**Replacement:** A project hosts outbox dispatch inside its request host.

**Rationale:** The Worker runs as its own process with its own lifetime, scaling, and configuration, apart from the request host. Its own project keeps that independence visible and stops request concerns from leaking into dispatch configuration.

**Reviewed for:** Does the outbox Worker host live in its own project, or is the shared hosting a documented choice? Check the solution's projects.

### Keep Provider Mapping with Integration

**ID:** `litespec/ext/outbox#keep-provider-mapping-with-integration`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep Integration Event-to-provider mapping in the integration that owns the side effect.

**Replacement:** A project keeps provider mapping in the shared dispatch path.

**Rationale:** Each provider uses its own transport with its own mapping, retries, and quirks. The integration that owns the side effect owns that mapping, so a provider change touches one integration instead of the shared dispatch path.

**Reviewed for:** Does every provider mapping live in the integration that owns its side effect, or is shared-path hosting a documented choice? Check the dispatch code and the integration projects.

### Dispatch Flow-Step Commands in a New Scope

**ID:** `litespec/ext/outbox#dispatch-flow-step-commands-in-a-new-scope`

**Strength:** default

**Applies to:** artifact: any

**Default:** Dispatch a flow-step command through `ICommandMediator` in a fresh Worker scope.

**Replacement:** A project runs flow-step commands inline in the dispatcher scope.

**Rationale:** A flow-step command is a full command with validation, authorization, and its own transaction, not a subroutine of the dispatcher. Sending it through `ICommandMediator` in a fresh Worker scope runs the whole application pipeline for it.

**Reviewed for:** Does every flow-step command dispatch through `ICommandMediator` in a fresh Worker scope, or is inline running a documented choice? Check the dispatcher's flow-step code.

### Avoid Automatic Durable-Bus Adoption

**ID:** `litespec/ext/outbox#avoid-automatic-durable-bus-adoption`

**Strength:** default

**Applies to:** artifact: any

**Default:** Use project-owned Marten records and Worker dispatch unless a manifest-pinned durable package passes a shared-transaction integration test.

**Replacement:** A project names a durable bus package as its dispatch transport in its documentation.

**Rationale:** A durable bus package is adopted only when an integration test proves it commits the message in the same transaction as the business work. Until that evidence exists, project-owned Marten records with Worker dispatch handle delivery without depending on behavior no one verified.

**Reviewed for:** Does every dispatcher run on project-owned records, or on a durable bus with a pinned manifest entry and a documented choice? Check the manifest and the dispatch transport.

## Dependencies

Packages: None.

Related extensions: `integrations`, `jobs`, `realtime`.

Guides: None.
