# Durable Delivery

**Stack:** any

**Concern:** infrastructure

## Intent

Committed changes reach the outside world through durable delivery that survives crashes. This page states how the product stages after-commit effects in the outbox for a Worker to deliver. A team selects it for delivery that survives process failure.

## Activation

Activation scope: `local`.

Applicable specification kinds: `use-case`.

A use case selects this extension when a committed change needs delivery that survives a process failure. That delivery can carry an integration event to another system, a command that advances a journey step, or a side effect on an outside provider. Selecting it also activates `litespec/backend/architecture#declare-the-execution-host-for-work-that-outlives-a-request`: the project declares the execution host that runs the dispatcher.

## Baseline Relationship

This extension replaces `litespec/backend/persistence#collect-events-without-a-public-unit-of-work`.

This extension switches on no optional record type: integration events, settings, and runbooks belong to the core. The project writes an integration-event record for each integration event it delivers, setting records for the delivery bounds, and runbook records for recovery and rollback.

## Standards

### Stage After-Commit Effects in the Outbox

**ID:** `litespec/ext/outbox#stage-after-commit-effects-in-the-outbox`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** A use case declaring `after-commit` `emits` or `runs` effects MUST stage each effect in the outbox in the same transaction as its changes.

**Rationale:** After-commit timing promises the message follows the commit: staged in the same transaction, the change and its message share one fate. An `emits` effect is a domain event the aggregate raises, and persistence stages that event in the outbox in the same commit as the aggregate, as `litespec/backend/application#emit-declared-integration-events-after-commit` requires. An effect sent from memory instead would vanish on a crash between the commit and the send. `litespec/core/use-cases#declare-every-effect-with-one-verb-and-its-timing` states the effect verbs and their timing, and `async` effects dispatch without the transaction.

**Reviewed for:** Which after-commit `emits` or `runs` effects, or domain events, stage outside the transaction of their aggregate changes? Look at each command handler's effects and the outbox writes beside them.

### Declare Published Facts as Integration Events

**ID:** `litespec/ext/outbox#declare-published-facts-as-integration-events`

**Strength:** required

**Applies to:** artifact: record:integration-event

**Statement:** Each `emits` effect MUST name an integration-event record carrying direction, payload, and trust.

**Rationale:** A published fact without a record is a promise nobody versioned: readers depend on fields no page states, and a field change breaks them without a diff. The integration-event record states the direction, the payload with its classifications, and the trust readers place in it. The [edges page](../core/edges.md) states the declaration and the record.

**Reviewed for:** Which `emits` effects name no integration-event record, or one without direction, payload, and trust? Look at each `emits` effect and the integration-event record it names.

### Record Durable Delivery Behavior

**ID:** `litespec/ext/outbox#record-durable-delivery-behavior`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A durable-delivery specification MUST document delivery guarantee, downstream boundary, duplicate behavior, retry horizon, and recovery runbook.

**Rationale:** Delivery crosses a system boundary, so the team agrees up front on the guarantee, the boundary, the duplicate behavior, the retry horizon, and the runbook that recovers when delivery fails. Without that agreement, the writer assumes once-only delivery while the target assumes retries, and both discover the mismatch in production. The agreement lands before the first message does.

**Reviewed for:** Which delivery specifications leave out the guarantee, downstream boundary, duplicate behavior, retry horizon, or recovery runbook? Look at each durable-delivery specification.

### Exclude Manually Repeatable Reactions and Jobs

**ID:** `litespec/ext/outbox#exclude-manually-repeatable-reactions-and-jobs`

**Strength:** required

**Applies to:** artifact: reaction, job

**Statement:** A project MUST NOT activate outbox for a reaction or job whose loss the operators accept and which they can repeat manually.

**Rationale:** Outbox storage, dispatch, retries, and replay exist to prevent loss; a reaction or job whose loss the operators accept and can repeat by hand needs none of that machinery. The reaction's or job's `onFailure` states its handling, and the use case page records the accepted loss.

**Reviewed for:** Which reactions or jobs whose loss the operators accept still carry outbox records? Look at the outbox records and the reactions and jobs they name.

### Emit Excluded Reactions and Jobs After Commit

**ID:** `litespec/ext/outbox#emit-excluded-reactions-and-jobs-after-commit`

**Strength:** required

**Applies to:** artifact: reaction, job

**Statement:** A reaction or job the outbox excludes MUST emit after its commit succeeds without staging an outbox record.

**Rationale:** Without an outbox record, nothing survives a crash between the commit and the emit, so the reaction or job emits immediately after its commit succeeds and accepts the loss. Emitting before the commit risks worse: it fires for a change the database then rolls back.

**Reviewed for:** Which excluded reactions or jobs emit before their commit succeeds, or stage an outbox record? Look at each excluded reaction's and job's emit call.

### Stage Messages with Business Work

**ID:** `litespec/ext/outbox#stage-messages-with-business-work`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A command post-handler MUST stage pending durable messages in the same scoped business session as its aggregate changes.

**Rationale:** The business change and its message share one fate: both persist or neither does. Staging the message in the same scoped business session as the aggregate changes puts both under one commit, so a crash can never leave an accepted change without its message or a message without its change.

**Reviewed for:** Which pending durable messages stage outside the scoped session of their aggregate changes? Look at each command post-handler and its session.

### Serialize Durable Messages

**ID:** `litespec/ext/outbox#serialize-durable-messages`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An outbox writer MUST serialize each pending durable message into an outbox record.

**Rationale:** The command scope ends when the request ends, while dispatch happens later in a Worker that never saw the request. Serializing the message into an outbox record gives that Worker everything it needs after the scope is gone.

**Reviewed for:** Which pending durable messages reach no serialized outbox record? Look at the outbox writer and its record type.

### Store Required Message Fields

**ID:** `litespec/ext/outbox#store-required-message-fields`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An outbox record MUST contain message identity, kind, stable type, schema version, time, payload, tenant, state, attempt, and next-attempt data.

**Rationale:** The dispatcher, the retry logic, and the operator each read the record: identity and type route it, the schema version selects its reader, time and tenant scope it, and state, attempt, and next-attempt data drive the retry. A field stored nowhere forces one of those readers to guess.

**Reviewed for:** Which outbox records lack identity, kind, type, schema version, time, payload, tenant, state, attempt, or next-attempt data? Look at the outbox record type.

### Store Failure and Completion Evidence

**ID:** `litespec/ext/outbox#store-failure-and-completion-evidence`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An outbox record MUST retain its last error summary and completion time.

**Rationale:** When delivery stalls, the operator opens the record first: the last error summary says why the previous attempt failed, and the completion time says which records finished. Without that evidence on the record, each investigation starts from scattered logs instead.

**Reviewed for:** Which outbox records drop their last error summary or completion time? Look at the outbox record type and its update code.

### Classify Message Kinds

**ID:** `litespec/ext/outbox#classify-message-kinds`

**Strength:** required

**Applies to:** artifact: record:integration-event

**Statement:** An outbox record MUST name its message kind as integration event, journey-step command, or provider side effect, with an integration event citing its integration-event record.

**Rationale:** An integration event, a journey-step command, and a provider side effect dispatch through different owners and transports, so the record names which one it holds. The integration event cites its integration-event record, so the staged message traces to the published fact it carries.

**Reviewed for:** Which outbox records name no message kind, or an integration event that does not cite its integration-event record? Look at the outbox record type and the messages staged.

### Reject Independent Outbox Commits

**ID:** `litespec/ext/outbox#reject-independent-outbox-commits`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An outbox writer MUST NOT use another connection or independent commit for required durable delivery.

**Rationale:** Two commits on two connections can split around a crash: the business change persists while the message rolls back, or the message persists for a change that never happened. Sharing one PostgreSQL database does not make separate transactions atomic, so the message joins the business transaction instead.

**Reviewed for:** Which outbox writers use a separate connection or commit? Look at each writer source and its path into the business session.

### Dispatch Claimed Records in Worker

**ID:** `litespec/ext/outbox#dispatch-claimed-records-in-worker`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A Worker MUST claim pending outbox records, dispatch a bounded batch, and record success or retry information.

**Rationale:** Dispatch needs time, retries, and network calls that no request transaction can hold. The Worker claims pending records, dispatches a bounded batch, and records each outcome, so the request commits fast while delivery proceeds on its own schedule.

**Reviewed for:** Which Worker loops dispatch unbounded batches or skip recording an outcome? Look at the dispatcher's claim and batch code.

### Exclude Request-Transaction Dispatch

**ID:** `litespec/ext/outbox#exclude-request-transaction-dispatch`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** WebApi MUST NOT dispatch a durable message inside the request transaction.

**Rationale:** A network call inside the request transaction holds database locks while the outside world answers, and a slow target slows every request behind it. The request commits only to make the message eligible; the Worker performs the delivery later.

**Reviewed for:** Which request paths dispatch a durable message inside the request transaction? Look at the endpoint and command handler paths.

### Define Claim Ownership

**ID:** `litespec/ext/outbox#define-claim-ownership`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An outbox claim MUST use a short transaction with the lease owner, lease expiry, and fencing value that `litespec/ext/jobs#store-execution-leases` defines.

**Rationale:** Several Workers poll the same store, so each dispatch attempt needs one owner: the lease owner, the lease expiry, and the fencing value name it. The short transaction takes the claim without holding locks into the delivery itself.

**Reviewed for:** Which claims take no owner, expiry, or fencing value, or hold them outside a short transaction? Look at the claim code in the outbox store.

### Release Claim Transactions Before Calls

**ID:** `litespec/ext/outbox#release-claim-transactions-before-calls`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A Worker MUST NOT hold an outbox claim transaction open during a network call.

**Rationale:** A network call to a slow target can last seconds or minutes. Holding the claim transaction open across that call pins database locks and connection slots, starving the Workers behind it; releasing the claim first lets the database serve other work while the call runs.

**Reviewed for:** Which claim transactions stay open across a network call? Look at the Worker's dispatch path.

### Restrict Completion to Current Lease Owners

**ID:** `litespec/ext/outbox#restrict-completion-to-current-lease-owners`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A Worker MUST update an outbox record only while it owns the matching lease and fencing value.

**Rationale:** A Worker that lost its lease, through a crash, a pause, or an expiry, can wake up after another Worker claimed the same record. Checking the lease and fencing value before each update stops the stale Worker from overwriting the current owner's result.

**Reviewed for:** Which record updates run without checking the current lease and fencing value? Look at each update statement in the outbox store.

### Requeue Expired Claims

**ID:** `litespec/ext/outbox#requeue-expired-claims`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An outbox store MUST return expired claims to eligible work.

**Rationale:** A Worker can die between claiming a record and dispatching it. Returning expired claims to eligible work lets a live Worker pick them up, so one crash delays a message instead of losing it.

**Reviewed for:** Which expired claims stay out of eligible work? Look at the requeue query in the outbox store and its tests.

### Accept Repeated Delivery

**ID:** `litespec/ext/outbox#accept-repeated-delivery`

**Strength:** required

**Applies to:** artifact: reaction

**Statement:** An event handler that receives an outbox delivery MUST handle the same message more than once.

**Rationale:** The target can accept a message while the Worker crashes before recording completion, and the requeued claim then dispatches it again. Delivery promises at-least-once, so every event handler handles the same message more than once without corrupting its state.

**Reviewed for:** Which event handlers keep wrong state when the same message arrives twice? Look at the handler tests that deliver a message twice.

### Identify Repeated Delivery

**ID:** `litespec/ext/outbox#identify-repeated-delivery`

**Strength:** required

**Applies to:** artifact: reaction

**Statement:** An event handler that receives an outbox delivery MUST use message ID or a business idempotency key at its Command or side-effect boundary.

**Rationale:** Handling a repeat means recognizing it: the message ID or a business idempotency key gives the event handler a stable identity to check at its command or side-effect boundary. Without that key, the handler processes the repeat as new work and pays, notifies, or ships twice.

**Reviewed for:** Which event handlers process a repeated message as new work? Look at each handler's Command or side-effect boundary.

### Deliver Each Event to Each Handler Separately

**ID:** `litespec/ext/outbox#deliver-each-event-to-each-handler-separately`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The outbox MUST deliver an event to each of its handlers as a separate delivery with its own retries and dead letter.

**Rationale:** One delivery for all handlers retries every handler when one fails. Each handler therefore gets its own delivery with its own retries and dead letter, and each delivery still ends in repeat-safe handling under `litespec/ext/outbox#accept-repeated-delivery`.

**Example:** A loan-borrowed event feeds a due-notice handler and a statistics handler. The notice delivery retries on its own schedule while the statistics delivery stays complete.

**Reviewed for:** Which events reach several handlers as one delivery? Look at the outbox delivery records for each handler.

### Await Target Acceptance

**ID:** `litespec/ext/outbox#await-target-acceptance`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A Worker MUST NOT mark an outbox record complete before the target boundary accepts it.

**Rationale:** Marking a record complete before the target accepts it declares a delivery that never happened, and no retry follows because the record looks done. The Worker waits for the target boundary's acceptance before recording completion.

**Reviewed for:** Which outbox records mark completion before the target accepts the message? Look at the Worker's completion code and the target's acceptance response.

### Bound Delivery Retries

**ID:** `litespec/ext/outbox#bound-delivery-retries`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An outbox dispatcher MUST use bounded exponential backoff with jitter.

**Rationale:** A failing target with ten thousand pending messages meets a retry storm when every message retries at once on a fixed interval. Bounded exponential backoff with jitter spreads the retries over time and caps the total, so the target recovers instead of drowning.

**Reviewed for:** Which retry schedules lack a bound, backoff, or jitter? Look at the dispatcher's retry code.

### Classify Permanent Delivery Failure

**ID:** `litespec/ext/outbox#classify-permanent-delivery-failure`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An outbox dispatcher MUST classify permanent failures, stop automatic retry at its retry bound, and retain safe investigation and replay data.

**Rationale:** Some failures never clear on retry: a deleted destination, a rejected payload, a revoked credential. Retrying those forever burns resources and buries the real backlog, so the dispatcher classifies them, stops at its retry bound, and keeps the evidence an operator needs to investigate and replay safely.

**Reviewed for:** Which permanent failures retry past their bound, or lose their investigation and replay data? Look at the dispatcher's failure classification and its tests.

### Distinguish Dependency Outage

**ID:** `litespec/ext/outbox#distinguish-dependency-outage`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A dispatcher MUST distinguish an unavailable outbox store from a failed claimed message.

**Rationale:** A dead outbox store and a failed delivery ask for different recoveries: the first needs a database restored, the second needs one destination investigated. Each carries its own signal. The store's availability is a gauge the readiness endpoint reads, while a claimed message's outcome is a counter carrying the result, following [the OpenTelemetry metrics specification](https://opentelemetry.io/docs/specs/otel/metrics/). A single error counter cannot tell an operator whether to restore a database or investigate one destination.

**Reviewed for:** Which outbox metrics use the wrong instrument type for store availability or claimed-message outcomes? Look at the metrics the outbox publishes.

### Back off Unavailable-Store Polling

**ID:** `litespec/ext/outbox#back-off-unavailable-store-polling`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A Worker MUST apply bounded backoff and rate-limited logging when its outbox store is unavailable.

**Rationale:** A Worker that polls a dead store on a tight loop writes an exception per iteration and fills disks and log quotas before anyone reads the first one. Bounded backoff with rate-limited logging keeps the Worker alive and quiet until the store returns.

**Reviewed for:** Which Worker loops poll an unavailable store without backoff or rate-limited logging? Look at the Worker's poll loop and its outage tests.

### Restrict Message Retry to Claimed Records

**ID:** `litespec/ext/outbox#restrict-message-retry-to-claimed-records`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A Worker MUST apply message retry and poison handling only to claimed outbox records.

**Rationale:** Retry and poison handling act on a claimed message; when the store itself is down, no claim exists and no message waits for delivery. Running retry logic against the outage double-counts attempts and can dead-letter messages whose only fault was an unreachable store.

**Reviewed for:** Which retry or poison-handling paths act on records that hold no claim? Look at the dispatcher's retry and poison code.

### Gate Dispatch on Readiness

**ID:** `litespec/ext/outbox#gate-dispatch-on-readiness`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A Worker host exposing readiness MUST gate dispatch and stop polling stores that cannot serve requests.

**Rationale:** A host that reports unready still serves traffic it cannot finish when its Workers keep polling dead stores. Gating dispatch on readiness stops the polling, so the host fails fast and recovers instead of churning against storage that cannot answer.

**Reviewed for:** Which unready hosts keep dispatching or polling a store that cannot serve requests? Look at the readiness check and the Worker's dispatch loop.

### Version Message Types

**ID:** `litespec/ext/outbox#version-message-types`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An outbox message MUST use a stable type name and explicit schema version.

**Rationale:** An outbox record outlives the deployment that wrote it, so the dispatcher meets records written by several versions at once. A stable type name with an explicit schema version tells the dispatcher which reader parses each record; without the pair, one shape change breaks every record written before it.

**Reviewed for:** Which outbox messages lack a stable type name or an explicit schema version? Look at each outbox message type.

### Support Coexisting Message Producers

**ID:** `litespec/ext/outbox#support-coexisting-message-producers`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A deployed dispatcher MUST process records from every application version that can coexist during rollout or rollback.

**Rationale:** During rollout and rollback, several application versions share one store and write different valid shapes into it. The dispatcher reads from every version that can coexist, or the deployment itself causes delivery failures.

Each message record therefore carries its own shape version, and the dispatcher selects a reader from that value. Adding an optional member with a defined absent-value behavior needs no new version. Removing a member, renaming one, narrowing a type, or changing a discriminator needs a new version, and the reader for each version stays until no record carries that version.

**Reviewed for:** Which application versions that can coexist have no reader in the dispatcher? Look at the dispatcher's version selection and its tests.

### Publish Backlog Indicators

**ID:** `litespec/ext/outbox#publish-backlog-indicators`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An outbox deployment MUST publish pending count, failed count, oldest pending age, attempts, dispatch duration, and success rate.

**Rationale:** Pending count, failed count, oldest pending age, attempts, dispatch duration, and success rate together show whether delivery keeps up: growth in the backlog or age reveals a stall, while duration and success rate reveal a struggling target. Without these indicators, the first sign of failure is a customer asking where their message went.

**Reviewed for:** Which of the six backlog indicators does the deployment leave unpublished? Look at the metrics the Worker exports.

### Set Delivery Alerts

**ID:** `litespec/ext/outbox#set-delivery-alerts`

**Strength:** required

**Applies to:** artifact: record:setting

**Statement:** An outbox deployment MUST set alert thresholds as setting records from its use-case delivery target.

**Rationale:** A shipment notice delayed an hour and a receipt delayed a week carry different business consequences, so each use case sets alert thresholds from its own delivery target. Setting records hold those thresholds, so the alert fires when delay passes what the business accepted.

**Reviewed for:** Which alert thresholds sit outside a setting record derived from a use-case delivery target? Look at the setting records beside each use case.

### Model Outbox Lifecycle States

**ID:** `litespec/ext/outbox#model-outbox-lifecycle-states`

**Strength:** required

**Applies to:** artifact: state-record

**Statement:** An outbox record MUST model its lifecycle as an abstract state base with one sealed record for each of pending, processing, dispatched, and dead-letter.

**Rationale:** `pending` becomes `processing`, and `processing` becomes `dispatched`, `pending`, or `dead-letter`, while audited replay moves `dead-letter` back to `pending`. Three of the four states carry data the others do not: `processing` holds a lease expiry, `dispatched` holds a dispatch time, and `dead-letter` holds a failure reason and an attempt count. A four-value label forces those onto nullable columns that mean something in one state each, which is the shape `litespec/backend/domain#model-every-closed-set-of-domain-values-as-a-closed-type-hierarchy` removes everywhere else.

**Example:** `OutboxState.Processing` carries one unexpired lease before a dispatcher records `OutboxState.Dispatched` or a retry state.

**Reviewed for:** Which outbox states lack a sealed record under the abstract base, or a documented transition? Look at the outbox state types and their transitions.

### Preserve Message Identity During Retry

**ID:** `litespec/ext/outbox#preserve-message-identity-during-retry`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An outbox dispatcher MUST preserve message ID across retry and replay.

**Rationale:** The target suppresses a repeat by recognizing its identity, which works only when every retry and replay carries the same message ID. A dispatcher that mints a new ID per attempt turns each retry into a new message the target processes again.

**Reviewed for:** Which retry or replay paths mint a new message ID? Look at the dispatcher's retry and replay code.

### Audit Outbox Replay

**ID:** `litespec/ext/outbox#audit-outbox-replay`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An outbox replay MUST record actor, reason, time, and failure without editing the payload: an audit record when audit is selected, a standalone record otherwise.

**Rationale:** A replay dispatches a message the system already handled or dead-lettered, so the team records who replayed it, why, when, and which failure it answers. Without that record, a duplicate delivery looks like a dispatcher bug instead of an operator's decision, and nobody can reconstruct the choice.

**Reviewed for:** Which replays omit the actor, reason, time, or failure, or edit the payload? Look at the replay code and its audit records.

### Protect Active Worker Compatibility

**ID:** `litespec/ext/outbox#protect-active-worker-compatibility`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** New WebApi code MUST emit a message only after every active Worker can read its type and schema version.

**Rationale:** WebApi writers and Worker dispatchers deploy independently, so a writer can run ahead of the oldest active Worker. When the writer emits a shape that Worker cannot read, those messages dead-letter on arrival through no fault of their own.

**Reviewed for:** Which new messages carry a type or schema version that an active Worker cannot read? Look at the Worker's readers and the messages the WebApi emits.

### Deploy Readers Before Writers

**ID:** `litespec/ext/outbox#deploy-readers-before-writers`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release MUST deploy compatible readers before writers for additive event versions.

**Rationale:** In a mixed-version rollout, old and new records sit in one store. Deploying compatible readers before the writers that emit new shapes keeps every record dispatchable at every step; the reverse order strands new records until the readers catch up.

**Reviewed for:** Which release plans deploy an additive writer before its readers? Look at the deployment order in the release runbook.

### Plan Undispatched Rollback Records

**ID:** `litespec/ext/outbox#plan-undispatched-rollback-records`

**Strength:** required

**Applies to:** artifact: record:runbook

**Statement:** A rollback Runbook MUST include records created by the new writer but not yet dispatched.

**Rationale:** Rolling back the deployment does not roll back the store: records the new writer staged but nobody dispatched stay behind. The rollback Runbook names how the previous version handles them, or they sit undispatched with no owner and no reader.

**Reviewed for:** Which rollback runbooks omit records the new writer staged but nobody dispatched? Look at each rollback runbook record.

## Conventions

### Group Outbox Infrastructure

**ID:** `litespec/ext/outbox#group-outbox-infrastructure`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep outbox record, storage, claim, and dispatch infrastructure under `Infrastructure/Messaging/Outbox/`.

**Replacement:** A project names another home for outbox infrastructure in its documentation.

**Rationale:** The outbox record, storage, claim, and dispatch code form one mechanism with one owner. Keeping it under `Infrastructure/Messaging/Outbox/` puts every persistent dispatch question in one folder instead of scattered across the project.

**Reviewed for:** Which outbox infrastructure sits outside `Infrastructure/Messaging/Outbox/` with no other home named in the documentation? Look at the outbox code folders and the project's documentation.

### Isolate the Worker Host

**ID:** `litespec/ext/outbox#isolate-the-worker-host`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep the outbox Worker host in its own project.

**Replacement:** A project hosts outbox dispatch inside its request host.

**Rationale:** The Worker runs as its own process with its own lifetime, scaling, and configuration, apart from the request host. Its own project keeps that independence visible and stops request concerns leaking into dispatch configuration.

**Reviewed for:** Which outbox Worker hosts share a project with the request host, with no documented choice? Look at the solution's projects.

### Keep Provider Mapping with Integration

**ID:** `litespec/ext/outbox#keep-provider-mapping-with-integration`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep Integration Event-to-provider mapping in the integration that owns the side effect.

**Replacement:** A project keeps provider mapping in the shared dispatch path.

**Rationale:** Each provider speaks its own transport with its own mapping, retries, and quirks. The integration that owns the side effect owns that mapping, so a provider change touches one integration instead of the shared dispatch path.

**Reviewed for:** Which provider mappings sit in the shared dispatch path with no documented choice? Look at the dispatch code and the integration projects.

### Dispatch Journey-Step Commands in a New Scope

**ID:** `litespec/ext/outbox#dispatch-journey-step-commands-in-a-new-scope`

**Strength:** default

**Applies to:** artifact: any

**Default:** Dispatch a journey-step command through `ICommandMediator` in a fresh Worker scope.

**Replacement:** A project runs journey-step commands inline in the dispatcher scope.

**Rationale:** A journey-step command is a full command with validation, authorization, and its own transaction, not a subroutine of the dispatcher. Sending it through `ICommandMediator` in a fresh Worker scope runs the whole application pipeline for it.

**Reviewed for:** Which journey-step commands run inline in the dispatcher scope with no documented choice? Look at the dispatcher's journey-step code.

### Avoid Automatic Durable-Bus Adoption

**ID:** `litespec/ext/outbox#avoid-automatic-durable-bus-adoption`

**Strength:** default

**Applies to:** artifact: any

**Default:** Use project-owned Marten records and Worker dispatch unless a manifest-pinned durable package passes a shared-transaction integration test.

**Replacement:** A project names a durable bus package as its dispatch transport in its documentation.

**Rationale:** A durable bus package earns its place only when an integration test proves it commits the message in the same transaction as the business work. Until that evidence exists, project-owned Marten records with Worker dispatch carry delivery without depending on behavior nobody verified.

**Reviewed for:** Which dispatchers run on a durable bus package with no pinned manifest entry or documented choice? Look at the manifest and the dispatch transport.

## Dependencies

Packages: None.

Related extensions: `integrations`, `jobs`, `realtime`.

Guides: None.
