# Durable and Live Delivery

**Stack:** any
**Layer:** infrastructure

## Intent

Committed changes reach the outside world through durable delivery that survives crashes and through live updates that reach open screens in seconds. This page states how the product stages after-commit effects in the outbox for a Worker to deliver and when it replaces polling with realtime transport. A team selects it for delivery that survives process failure or for updates a measured target requires live.

## Activation

Activation scope: `local`.

Applicable specification kinds: `use-case`, `journey`.

A use case selects this extension when a committed change needs delivery that survives a process failure. That delivery can carry an integration event to another system, a command that advances a journey step, or a side effect on an outside provider. Selecting it also activates `litespec/backend/architecture#declare-the-execution-host-for-work-that-outlives-a-request`: the project declares the execution host that runs the dispatcher.

A use case or journey selects this extension for live updates after the team measures polling against the update target: the polling interval, the visible delay, the request cost, and the required update speed. The selection also names the accepted cost of disconnects and missed messages: how the client recovers when the connection drops or a notification never arrives.

## Baseline Relationship

This extension replaces `litespec/backend/persistence#collect-events-without-a-public-unit-of-work`.

This extension switches on no optional record type: integration events, settings, and runbooks belong to the core. The project writes an integration-event record for each integration event it delivers, setting records for the delivery bounds, and runbook records for recovery and rollback. Each pushed update is a notification record, and each resource bound lives in a setting record.

One-way notifications travel over server-sent events, which need no added package because the platform ships them.

## Standards

### Stage After-Commit Effects in the Outbox

**ID:** `litespec/ext/outbox#stage-after-commit-effects-in-the-outbox`

**Strength:** rule

**Applies to:** artifact: command-handler

**Statement:** A use case declaring `after-commit` `emits` or `runs` effects MUST stage each effect in the outbox in the same transaction as its changes.

**Rationale:** After-commit timing promises the message follows the commit: staged in the same transaction, the change and its message share one fate. An `emits` effect is a domain event the aggregate raises, and persistence stages that event in the outbox in the same commit as the aggregate, as `litespec/backend/application#emit-declared-integration-events-after-commit` requires. An effect sent from memory instead would vanish on a crash between the commit and the send. `litespec/core/use-cases#declare-every-effect-with-one-verb-and-its-timing` states the effect verbs and their timing, and `async` effects dispatch without the transaction.

**Reviewed for:** Each after-commit emits and runs effect stages in the outbox in the same transaction as its changes, with each domain event staged in the same commit as its aggregate.

### Declare Published Facts as Integration Events

**ID:** `litespec/ext/outbox#declare-published-facts-as-integration-events`

**Strength:** rule

**Applies to:** artifact: record:integration-event

**Statement:** Each `emits` effect MUST name an integration-event record carrying direction, payload, and trust.

**Rationale:** A published fact without a record is a promise nobody versioned: readers depend on fields no page states, and a field change breaks them without a diff. The integration-event record states the direction, the payload with its classifications, and the trust readers place in it. The [edges page](../core/edges.md) states the declaration and the record.

**Reviewed for:** Each emits effect names an integration-event record carrying direction, payload, and trust.

### Record Durable Delivery Behavior

**ID:** `litespec/ext/outbox#record-durable-delivery-behavior`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A durable-delivery specification MUST document delivery guarantee, downstream boundary, duplicate behavior, retry horizon, and recovery runbook.

**Rationale:** Delivery crosses a system boundary, so the team agrees up front on the guarantee, the boundary, the duplicate behavior, the retry horizon, and the runbook that recovers when delivery fails. Without that agreement, the writer assumes once-only delivery while the target assumes retries, and both discover the mismatch in production. The agreement lands before the first message does.

**Reviewed for:** Each delivery specification documents guarantee, downstream boundary, duplicate behavior, retry horizon, and recovery runbook.

### Exclude Manually Repeatable Reactions and Jobs

**ID:** `litespec/ext/outbox#exclude-manually-repeatable-reactions-and-jobs`

**Strength:** rule

**Applies to:** artifact: reaction, job

**Statement:** A project MUST NOT activate outbox for a reaction or job whose loss the operators accept and which they can repeat manually.

**Rationale:** Outbox storage, dispatch, retries, and replay exist to prevent loss; a reaction or job whose loss the operators accept and can repeat by hand needs none of that machinery. The reaction's or job's `onFailure` states its handling, and the use case page records the accepted gap.

**Reviewed for:** Reactions and jobs whose loss the operators accept and can repeat manually carry no outbox records.

### Emit Excluded Reactions and Jobs After Commit

**ID:** `litespec/ext/outbox#emit-excluded-reactions-and-jobs-after-commit`

**Strength:** rule

**Applies to:** artifact: reaction, job

**Statement:** A reaction or job the outbox excludes MUST emit after its commit succeeds without staging an outbox record.

**Rationale:** Without an outbox record, nothing survives a crash between the commit and the emit, so the reaction or job emits immediately after its commit succeeds and accepts the gap. Emitting before the commit risks worse: it fires for a change the database then rolls back.

**Reviewed for:** Excluded reactions and jobs emit after commit success with no staged outbox record.

### Stage Messages with Business Work

**ID:** `litespec/ext/outbox#stage-messages-with-business-work`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A command post-handler MUST stage pending durable messages in the same scoped business session as its aggregate changes.

**Rationale:** The business change and its message share one fate: both persist or neither does. Staging the message in the same scoped business session as the aggregate changes puts both under one commit, so a crash can never leave an accepted change without its message or a message without its change.

**Reviewed for:** Pending durable messages stage in the same scoped session as their aggregate changes.

### Serialize Durable Messages

**ID:** `litespec/ext/outbox#serialize-durable-messages`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** An outbox writer MUST serialize each pending durable message into an outbox record.

**Rationale:** The command scope ends when the request ends, while dispatch happens later in a Worker that never saw the request. Serializing the message into an outbox record gives that Worker everything it needs after the scope is gone.

**Reviewed for:** Each pending durable message lands serialized in an outbox record.

### Store Required Message Fields

**ID:** `litespec/ext/outbox#store-required-message-fields`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** An outbox record MUST contain message identity, kind, stable type, schema version, time, payload, tenant, state, attempt, and next-attempt data.

**Rationale:** The dispatcher, the retry logic, and the operator each read the record: identity and type route it, the schema version selects its reader, time and tenant scope it, and state, attempt, and next-attempt data drive the retry. A field stored nowhere forces one of those readers to guess.

**Reviewed for:** Each outbox record carries identity, kind, type, schema version, time, payload, tenant, state, attempt, and next-attempt data.

### Store Failure and Completion Evidence

**ID:** `litespec/ext/outbox#store-failure-and-completion-evidence`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** An outbox record MUST retain its last error summary and completion time.

**Rationale:** When delivery stalls, the operator opens the record first: the last error summary says why the previous attempt failed, and the completion time says which records finished. Without that evidence on the record, each investigation starts from scattered logs instead.

**Reviewed for:** Each outbox record retains its last error summary and completion time.

### Classify Message Kinds

**ID:** `litespec/ext/outbox#classify-message-kinds`

**Strength:** rule

**Applies to:** artifact: record:integration-event

**Statement:** An outbox record MUST name its message kind as integration event, journey-step command, or provider side effect, with an integration event citing its integration-event record.

**Rationale:** An integration event, a journey-step command, and a provider side effect dispatch through different owners and transports, so the record names which one it holds. The integration event cites its integration-event record, so the staged message traces to the published fact it carries.

**Reviewed for:** Each outbox record names integration event, journey-step command, or provider side effect, with integration events citing their integration-event record.

### Reject Independent Outbox Commits

**ID:** `litespec/ext/outbox#reject-independent-outbox-commits`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** An outbox writer MUST NOT use another connection or independent commit for required durable delivery.

**Rationale:** Two commits on two connections can split around a crash: the business change persists while the message rolls back, or the message persists for a change that never happened. Sharing one PostgreSQL database does not make separate transactions atomic, so the message joins the business transaction instead.

**Reviewed for:** Writer sources stage through the business session with no separate connection or commit.

### Dispatch Claimed Records in Worker

**ID:** `litespec/ext/outbox#dispatch-claimed-records-in-worker`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A Worker MUST claim pending outbox records, dispatch a bounded batch, and record success or retry information.

**Rationale:** Dispatch needs time, retries, and network calls that no request transaction can hold. The Worker claims pending records, dispatches a bounded batch, and records each outcome, so the request commits fast while delivery proceeds on its own schedule.

**Reviewed for:** The Worker claims pending records, dispatches bounded batches, and records each outcome.

### Exclude Request-Transaction Dispatch

**ID:** `litespec/ext/outbox#exclude-request-transaction-dispatch`

**Strength:** rule

**Applies to:** artifact: endpoint

**Statement:** WebApi MUST NOT dispatch a durable message inside the request transaction.

**Rationale:** A network call inside the request transaction holds database locks while the outside world answers, and a slow target slows every request behind it. The request commits only to make the message eligible; the Worker performs the delivery later.

**Reviewed for:** Request paths commit eligibility only, with dispatch living in the Worker.

### Define Claim Ownership

**ID:** `litespec/ext/outbox#define-claim-ownership`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** An outbox claim MUST use a short transaction with the lease owner, lease expiry, and fencing value that `litespec/ext/jobs#store-execution-leases` defines.

**Rationale:** Several Workers poll the same store, so each dispatch attempt needs one owner: the lease owner, the lease expiry, and the fencing value name it. The short transaction takes the claim without holding locks into the delivery itself.

**Reviewed for:** Each claim takes owner, expiry, and fencing value in a short transaction.

### Release Claim Transactions Before Calls

**ID:** `litespec/ext/outbox#release-claim-transactions-before-calls`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A Worker MUST NOT hold an outbox claim transaction open during a network call.

**Rationale:** A network call to a slow target can last seconds or minutes. Holding the claim transaction open across that call pins database locks and connection slots, starving the Workers behind it; releasing the claim first lets the database serve other work while the call runs.

**Reviewed for:** Claim transactions close before the Worker makes network calls.

### Restrict Completion to Current Lease Owners

**ID:** `litespec/ext/outbox#restrict-completion-to-current-lease-owners`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A Worker MUST update an outbox record only while it owns the matching lease and fencing value.

**Rationale:** A Worker that lost its lease, through a crash, a pause, or an expiry, can wake up after another Worker claimed the same record. Checking the lease and fencing value before each update stops the stale Worker from overwriting the current owner's result.

**Reviewed for:** Record updates succeed only under the current lease and fencing value.

### Requeue Expired Claims

**ID:** `litespec/ext/outbox#requeue-expired-claims`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** An outbox store MUST return expired claims to eligible work.

**Rationale:** A Worker can die between claiming a record and dispatching it. Returning expired claims to eligible work lets a live Worker pick them up, so one crash delays a message instead of losing it.

**Reviewed for:** Expired claims return to eligible work for a live Worker.

### Accept Repeated Delivery

**ID:** `litespec/ext/outbox#accept-repeated-delivery`

**Strength:** rule

**Applies to:** artifact: reaction

**Statement:** An event handler that receives an outbox delivery MUST handle the same message more than once.

**Rationale:** The target can accept a message while the Worker crashes before recording completion, and the requeued claim then dispatches it again. Delivery promises at-least-once, so every event handler handles the same message more than once without corrupting its state.

**Reviewed for:** Each event handler keeps correct state when the same message arrives twice.

### Identify Repeated Delivery

**ID:** `litespec/ext/outbox#identify-repeated-delivery`

**Strength:** rule

**Applies to:** artifact: reaction

**Statement:** An event handler that receives an outbox delivery MUST use message ID or a business idempotency key at its Command or side-effect boundary.

**Rationale:** Handling a repeat means recognizing it: the message ID or a business idempotency key gives the event handler a stable identity to check at its command or side-effect boundary. Without that key, the handler processes the repeat as new work and pays, notifies, or ships twice.

**Reviewed for:** Each event handler checks message ID or business key at its Command or side-effect boundary.

### Deliver Each Event to Each Handler Separately

**ID:** `litespec/ext/outbox#deliver-each-event-to-each-handler-separately`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** The outbox MUST deliver an event to each of its handlers as a separate delivery with its own retries and dead letter.

**Rationale:** One delivery for all handlers retries every handler when one fails. Each handler therefore gets its own delivery with its own retries and dead letter, and each delivery still ends in repeat-safe handling under `litespec/ext/outbox#accept-repeated-delivery`.

**Example:** A loan-borrowed event feeds a due-notice handler and a statistics handler. The notice delivery retries on its own schedule while the statistics delivery stays complete.

**Reviewed for:** Each event reaches each of its handlers as a separate delivery with its own retries and dead letter.

### Await Target Acceptance

**ID:** `litespec/ext/outbox#await-target-acceptance`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A Worker MUST NOT mark an outbox record complete before the target boundary accepts it.

**Rationale:** Marking a record complete before the target accepts it declares a delivery that never happened, and no retry follows because the record looks done. The Worker waits for the target boundary's acceptance before recording completion.

**Reviewed for:** Completion records follow target acceptance, with no early completion.

### Bound Delivery Retries

**ID:** `litespec/ext/outbox#bound-delivery-retries`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** An outbox dispatcher MUST use bounded exponential backoff with jitter.

**Rationale:** A failing target with ten thousand pending messages meets a retry storm when every message retries at once on a fixed interval. Bounded exponential backoff with jitter spreads the retries over time and caps the total, so the target recovers instead of drowning.

**Reviewed for:** Retries run bounded exponential backoff with jitter.

### Classify Permanent Delivery Failure

**ID:** `litespec/ext/outbox#classify-permanent-delivery-failure`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** An outbox dispatcher MUST classify permanent failures, stop automatic retry at its retry bound, and retain safe investigation and replay data.

**Rationale:** Some failures never clear on retry: a deleted destination, a rejected payload, a revoked credential. Retrying those forever burns resources and buries the real backlog, so the dispatcher classifies them, stops at its retry bound, and keeps the evidence an operator needs to investigate and replay safely.

**Reviewed for:** Permanent failures stop at the retry bound with safe investigation and replay data retained.

### Distinguish Dependency Outage

**ID:** `litespec/ext/outbox#distinguish-dependency-outage`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A dispatcher MUST distinguish an unavailable outbox store from a failed claimed message.

**Rationale:** A dead outbox store and a failed delivery ask for different recoveries: the first needs a database restored, the second needs one destination investigated. Each carries its own signal. The store's availability is a gauge the readiness endpoint reads, while a claimed message's outcome is a counter carrying the result, following [the OpenTelemetry metrics specification](https://opentelemetry.io/docs/specs/otel/metrics/). A single error counter cannot tell an operator whether to restore a database or investigate one destination.

**Reviewed for:** Store availability reads as a gauge while claimed-message outcomes read as a counter.

### Back off Unavailable-Store Polling

**ID:** `litespec/ext/outbox#back-off-unavailable-store-polling`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A Worker MUST apply bounded backoff and rate-limited logging when its outbox store is unavailable.

**Rationale:** A Worker that polls a dead store on a tight loop writes an exception per iteration and fills disks and log quotas before anyone reads the first one. Bounded backoff with rate-limited logging keeps the Worker alive and quiet until the store returns.

**Reviewed for:** Polling during a store outage backs off with rate-limited logging.

### Restrict Message Retry to Claimed Records

**ID:** `litespec/ext/outbox#restrict-message-retry-to-claimed-records`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A Worker MUST apply message retry and poison handling only to claimed outbox records.

**Rationale:** Retry and poison handling act on a claimed message; when the store itself is down, no claim exists and no message waits for delivery. Running retry logic against the outage double-counts attempts and can dead-letter messages whose only fault was an unreachable store.

**Reviewed for:** Retry and poison handling touch claimed records only.

### Gate Dispatch on Readiness

**ID:** `litespec/ext/outbox#gate-dispatch-on-readiness`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A Worker host exposing readiness MUST gate dispatch and stop polling stores that cannot serve requests.

**Rationale:** A host that reports unready still serves traffic it cannot finish when its Workers keep polling dead stores. Gating dispatch on readiness stops the polling, so the host fails fast and recovers instead of churning against storage that cannot answer.

**Reviewed for:** Unready hosts stop dispatch and dead-store polling.

### Version Message Types

**ID:** `litespec/ext/outbox#version-message-types`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** An outbox message MUST use a stable type name and explicit schema version.

**Rationale:** An outbox record outlives the deployment that wrote it, so the dispatcher meets records written by several versions at once. A stable type name with an explicit schema version tells the dispatcher which reader parses each record; without the pair, one shape change breaks every record written before it.

**Reviewed for:** Each message carries a stable type name with an explicit schema version.

### Support Coexisting Message Producers

**ID:** `litespec/ext/outbox#support-coexisting-message-producers`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A deployed dispatcher MUST process records from every application version that can coexist during rollout or rollback.

**Rationale:** During rollout and rollback, several application versions share one store and write different valid shapes into it. The dispatcher reads from every version that can coexist, or the deployment itself causes delivery failures.

Each message record therefore carries its own shape version, and the dispatcher selects a reader from that value. Adding an optional member with a defined absent-value behavior needs no new version. Removing a member, renaming one, narrowing a type, or changing a discriminator needs a new version, and the reader for each version stays until no record carries that version.

**Reviewed for:** The dispatcher reads records from every coexisting application version.

### Publish Backlog Indicators

**ID:** `litespec/ext/outbox#publish-backlog-indicators`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** An outbox deployment MUST publish pending count, failed count, oldest pending age, attempts, dispatch duration, and success rate.

**Rationale:** Pending count, failed count, oldest pending age, attempts, dispatch duration, and success rate together show whether delivery keeps up: growth in the backlog or age reveals a stall, while duration and success rate reveal a struggling target. Without these indicators, the first sign of failure is a customer asking where their message went.

**Reviewed for:** The deployment publishes pending count, failed count, oldest pending age, attempts, dispatch duration, and success rate.

### Set Delivery Alerts

**ID:** `litespec/ext/outbox#set-delivery-alerts`

**Strength:** rule

**Applies to:** artifact: record:setting

**Statement:** An outbox deployment MUST set alert thresholds as setting records from its use-case delivery target.

**Rationale:** A shipment notice delayed an hour and a receipt delayed a week carry different business consequences, so each use case sets alert thresholds from its own delivery target. Setting records hold those thresholds, so the alert fires when delay passes what the business accepted.

**Reviewed for:** Alert thresholds sit in setting records derived from each use-case delivery target.

### Model Outbox Lifecycle States

**ID:** `litespec/ext/outbox#model-outbox-lifecycle-states`

**Strength:** rule

**Applies to:** artifact: state-record

**Statement:** An outbox record MUST model its lifecycle as an abstract state base with one sealed record for each of pending, processing, dispatched, and dead-letter.

**Rationale:** `pending` becomes `processing`, and `processing` becomes `dispatched`, `pending`, or `dead-letter`, while audited replay moves `dead-letter` back to `pending`. Three of the four states carry data the others do not: `processing` holds a lease expiry, `dispatched` holds a dispatch time, and `dead-letter` holds a failure reason and an attempt count. A four-value label forces those onto nullable columns that mean something in one state each, which is the shape `litespec/backend/domain#model-every-closed-set-of-domain-values-as-a-closed-type-hierarchy` removes everywhere else.

**Example:** `OutboxState.Processing` carries one unexpired lease before a dispatcher records `OutboxState.Dispatched` or a retry state.

**Reviewed for:** Outbox lifecycle states form sealed records over an abstract base with documented transitions.

### Preserve Message Identity During Retry

**ID:** `litespec/ext/outbox#preserve-message-identity-during-retry`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** An outbox dispatcher MUST preserve message ID across retry and replay.

**Rationale:** The target suppresses a repeat by recognizing its identity, which works only when every retry and replay carries the same message ID. A dispatcher that mints a new ID per attempt turns each retry into a new message the target processes again.

**Reviewed for:** Message ID stays identical across retry and replay.

### Audit Outbox Replay

**ID:** `litespec/ext/outbox#audit-outbox-replay`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** An outbox replay MUST record actor, reason, time, and failure without editing the payload: an audit record when audit is selected, a standalone record otherwise.

**Rationale:** A replay dispatches a message the system already handled or dead-lettered, so the team records who replayed it, why, when, and which failure it answers. Without that record, a duplicate delivery looks like a dispatcher bug instead of an operator's decision, and nobody can reconstruct the choice.

**Reviewed for:** Each replay records actor, reason, time, and failure without editing the payload.

### Protect Active Worker Compatibility

**ID:** `litespec/ext/outbox#protect-active-worker-compatibility`

**Strength:** rule

**Applies to:** artifact: endpoint

**Statement:** New WebApi code MUST emit a message only after every active Worker can read its type and schema version.

**Rationale:** WebApi writers and Worker dispatchers deploy independently, so a writer can run ahead of the oldest active Worker. When the writer emits a shape that Worker cannot read, those messages dead-letter on arrival through no fault of their own.

**Reviewed for:** Every active Worker reads the type and schema version of new messages.

### Deploy Readers Before Writers

**ID:** `litespec/ext/outbox#deploy-readers-before-writers`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A release MUST deploy compatible readers before writers for additive event versions.

**Rationale:** In a mixed-version rollout, old and new records sit in one store. Deploying compatible readers before the writers that emit new shapes keeps every record dispatchable at every step; the reverse order strands new records until the readers catch up.

**Reviewed for:** Releases deploy compatible readers before additive writers.

### Plan Undispatched Rollback Records

**ID:** `litespec/ext/outbox#plan-undispatched-rollback-records`

**Strength:** rule

**Applies to:** artifact: record:runbook

**Statement:** A rollback Runbook MUST include records created by the new writer but not yet dispatched.

**Rationale:** Rolling back the deployment does not roll back the store: records the new writer staged but nobody dispatched stay behind. The rollback Runbook names how the previous version handles them, or they sit undispatched with no owner and no reader.

**Reviewed for:** The rollback Runbook covers records the new writer staged but nobody dispatched.

### Record Realtime Need

**ID:** `litespec/ext/outbox#record-realtime-need`

**Strength:** rule

**Applies to:** artifact: record:notification

**Statement:** A realtime use case MUST document polling behavior, measured cost, update latency, expected connections, and recovery behavior.

**Rationale:** Open connections cost memory, CPU, and operating attention on every host, so the team writes down what polling costs, what the update target requires, and how many connections to expect before choosing transport. The use case page carries that record, so a later reader sees which measurements justified the choice.

**Reviewed for:** The use case page holds the polling record, measured cost, update target, expected connections, and recovery behavior.

### Reject Cosmetic Realtime Adoption

**ID:** `litespec/ext/outbox#reject-cosmetic-realtime-adoption`

**Strength:** rule

**Applies to:** artifact: record:notification

**Statement:** A product MUST NOT activate realtime only to make an interface feel immediate.

**Rationale:** Realtime transport that serves only a feeling of immediacy spends connection capacity and operating attention without moving any target. The extension asks for a measured update requirement first, so polling stays where polling suffices.

**Reviewed for:** The realtime decision cites a measured update requirement.

### Use Server-Sent Events for Notifications

**ID:** `litespec/ext/outbox#use-server-sent-events-for-notifications`

**Strength:** rule

**Applies to:** artifact: record:notification

**Statement:** A one-way server notification use case MUST use server-sent events.

**Rationale:** A notification that flows one way, from server to browser, needs no bidirectional channel with groups and fallback. Server-sent events carry it over plain HTTP, which passes through proxies and firewalls without extra negotiation.

**Reviewed for:** Each one-way notification travels over the server-sent events boundary in code.

### Use SignalR for Interactive Messaging

**ID:** `litespec/ext/outbox#use-signalr-for-interactive-messaging`

**Strength:** rule

**Applies to:** artifact: record:notification

**Statement:** A use case requiring bidirectional messages, server groups, or transport fallback MUST use SignalR.

**Rationale:** Two-way messages, server-managed groups, and transport fallback need a channel built for interaction, which one-way events cannot provide. SignalR supplies all three, so the use case that needs them takes SignalR instead of stretching events past their shape.

**Reviewed for:** Each bidirectional use case runs on SignalR groups, messages, or fallback in code.

### Record Transport Support

**ID:** `litespec/ext/outbox#record-transport-support`

**Strength:** rule

**Applies to:** artifact: record:notification

**Statement:** A realtime use case MUST record its transport choice and expected hosted-platform support.

**Rationale:** Hosted platforms limit connection duration, idle timeouts, and transport upgrades in ways that differ per host. Recording the transport choice with its expected platform support surfaces the mismatch before deployment instead of during the first long-lived connection.

**Reviewed for:** The use case page names its transport choice with the expected hosted-platform support.

### Declare Realtime Freshness

**ID:** `litespec/ext/outbox#declare-realtime-freshness`

**Strength:** rule

**Applies to:** artifact: record:notification

**Statement:** A query served over realtime MUST declare a `freshness` object with `mode` of `immediate` or `advisory` on its output.

**Rationale:** A realtime channel delivers an update seconds after the change, so the served query promises `{"mode": "immediate"}`; where the pushed value still awaits a later decision, it promises `{"mode": "advisory", "authority": "<record id>"}` naming the record that holds the authoritative value. Without the declared freshness, a reader cannot tell whether the live value is final or provisional.

**Reviewed for:** Which realtime-served queries lack an immediate or advisory freshness object on their output? Look at the served query beside the realtime use case.

### Authenticate and Authorize Subscriptions

**ID:** `litespec/ext/outbox#authenticate-and-authorize-subscriptions`

**Strength:** rule

**Applies to:** artifact: record:notification

**Statement:** A realtime server MUST authenticate each connection and authorize every user-, tenant-, or resource-scoped subscription.

**Rationale:** Connecting proves who the caller is; it says nothing about which user's, tenant's, or resource's updates the caller may hear. Each user-, tenant-, or resource-scoped subscription checks its own authorization, so one valid connection cannot listen in on another tenant's data.

**Reviewed for:** The connection tests show authenticated connections only, with each scoped subscription authorized.

### Revalidate Long-Lived Access

**ID:** `litespec/ext/outbox#revalidate-long-lived-access`

**Strength:** rule

**Applies to:** artifact: record:notification

**Statement:** A realtime server MUST revalidate long-lived access according to token and session lifetime.

**Rationale:** A connection can outlive the grant that opened it: tokens expire, roles change, sessions end. Revalidating access on token and session lifetime closes the connection whose grant died instead of streaming updates to a caller who no longer qualifies.

**Reviewed for:** A connection whose token or session expired loses access in the revalidation tests.

### Reconnect and Refresh Authoritative State

**ID:** `litespec/ext/outbox#reconnect-and-refresh-authoritative-state`

**Strength:** rule

**Applies to:** artifact: record:notification

**Statement:** A realtime client MUST reconnect with bounded backoff and refresh authoritative state after a connection gap.

**Rationale:** Connections drop, and a notification sent during the gap never arrives. Bounded backoff reconnects without hammering the server, and refreshing authoritative state after the gap replaces whatever the missed notifications would have changed.

**Reviewed for:** The client reconnects with bounded backoff and refreshes authoritative state after a gap in the recovery tests.

### Tolerate Notification Ordering Differences

**ID:** `litespec/ext/outbox#tolerate-notification-ordering-differences`

**Strength:** rule

**Applies to:** artifact: record:notification

**Statement:** A realtime client MUST safely handle duplicate and out-of-order notifications.

**Rationale:** The transport can deliver a notification twice or deliver two of them out of order. A client that applies each notification as an absolute update corrupts its state on the first duplicate; a client built for repeats and reordering converges on the truth either way.

**Reviewed for:** Duplicate and reordered notifications leave client state correct in the client tests.

### Bound Realtime Resources

**ID:** `litespec/ext/outbox#bound-realtime-resources`

**Strength:** rule

**Applies to:** artifact: record:notification

**Statement:** A realtime deployment MUST set four setting-record bounds: concurrent connections per host, receive message size, parallel invocations per connection, and client timeout.

**Rationale:** Each open connection holds memory, each message costs CPU, and a flood of either starves the host. The deployment sets four bounds as setting records - concurrent connections per host, receive message size, parallel invocations per connection, and client timeout - with each value drawn from the deployment's own connection count and message size and the observation that produced it. [The platform's realtime security guidance](https://learn.microsoft.com/en-us/aspnet/core/signalr/security) names the same settings.

**Reviewed for:** The four deployment bounds live as setting records with their values beside the deployment.

### Reject Unbounded Slow-Client Buffering

**ID:** `litespec/ext/outbox#reject-unbounded-slow-client-buffering`

**Strength:** rule

**Applies to:** artifact: record:notification

**Statement:** A realtime server MUST NOT buffer an unbounded stream for a slow client.

**Rationale:** A client that reads slower than the server writes grows its buffer without end, consuming host memory meant for every subscriber. Bounded buffering with backpressure disconnects or slows the laggard instead of letting one slow reader starve the rest.

**Reviewed for:** Slow-client buffering in the server code is bounded with backpressure.

## Conventions

### Group Outbox Infrastructure

**ID:** `litespec/ext/outbox#group-outbox-infrastructure`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep outbox record, storage, claim, and dispatch infrastructure under `Infrastructure/Messaging/Outbox/`.

**Replacement:** A project names another home for outbox infrastructure in its documentation.

**Rationale:** The outbox record, storage, claim, and dispatch code form one mechanism with one owner. Keeping it under `Infrastructure/Messaging/Outbox/` puts every persistent dispatch question in one folder instead of scattered across the project.

**Reviewed for:** Outbox infrastructure sits under `Infrastructure/Messaging/Outbox/`, or the project names another home in its documentation.

### Isolate the Worker Host

**ID:** `litespec/ext/outbox#isolate-the-worker-host`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep the outbox Worker host in its own project.

**Replacement:** A project hosts outbox dispatch inside its request host.

**Rationale:** The Worker runs as its own process with its own lifetime, scaling, and configuration, apart from the request host. Its own project keeps that independence visible and stops request concerns leaking into dispatch configuration.

**Reviewed for:** The outbox Worker host lives in its own project, or the project hosts dispatch inside its request host.

### Keep Provider Mapping with Integration

**ID:** `litespec/ext/outbox#keep-provider-mapping-with-integration`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep Integration Event-to-provider mapping in the integration that owns the side effect.

**Replacement:** A project keeps provider mapping in the shared dispatch path.

**Rationale:** Each provider speaks its own transport with its own mapping, retries, and quirks. The integration that owns the side effect owns that mapping, so a provider change touches one integration instead of the shared dispatch path.

**Reviewed for:** Provider mapping sits in the owning integration, or the project keeps it in the shared dispatch path.

### Dispatch Journey-Step Commands in a New Scope

**ID:** `litespec/ext/outbox#dispatch-journey-step-commands-in-a-new-scope`

**Strength:** default

**Applies to:** artifact: any

**Default:** Dispatch a journey-step command through `ICommandMediator` in a fresh Worker scope.

**Replacement:** A project runs journey-step commands inline in the dispatcher scope.

**Rationale:** A journey-step command is a full command with validation, authorization, and its own transaction, not a subroutine of the dispatcher. Sending it through `ICommandMediator` in a fresh Worker scope runs the whole application pipeline for it.

**Reviewed for:** Journey-step commands run through `ICommandMediator` in a fresh Worker scope, or the project runs them inline in the dispatcher scope.

### Avoid Automatic Durable-Bus Adoption

**ID:** `litespec/ext/outbox#avoid-automatic-durable-bus-adoption`

**Strength:** default

**Applies to:** artifact: any

**Default:** Use project-owned Marten records and Worker dispatch unless a manifest-pinned durable package passes a shared-transaction integration test.

**Replacement:** A project names a durable bus package as its dispatch transport in its documentation.

**Rationale:** A durable bus package earns its place only when an integration test proves it commits the message in the same transaction as the business work. Until that evidence exists, project-owned Marten records with Worker dispatch carry delivery without depending on behavior nobody verified.

**Reviewed for:** Dispatch runs on project-owned Marten records, or the project names a pinned durable bus package as its transport in its documentation.

### Place Realtime Code at Outer Boundaries

**ID:** `litespec/ext/outbox#place-realtime-code-at-outer-boundaries`

**Strength:** default

**Applies to:** artifact: record:notification

**Default:** Keep hubs or endpoints in WebApi, publication adapters in Infrastructure, and browser connections under `lib/realtime/`.

**Replacement:** A consumer records a realtime-placement convention naming where its transport code lives.

**Rationale:** Hubs, endpoints, publication adapters, and browser connections deal in transport and provider detail, not business rules. Keeping them at the outer boundaries leaves business features owning only the narrow functions they call.

**Reviewed for:** Realtime transport code sits at the documented boundaries, or the realtime-placement convention names where it lives.

### Subscribe Through Narrow Functions

**ID:** `litespec/ext/outbox#subscribe-through-narrow-functions`

**Strength:** default

**Applies to:** artifact: record:notification

**Default:** Let feature modules subscribe through narrow use-case-specific functions.

**Replacement:** A consumer records a realtime-subscription convention naming how its features subscribe.

**Rationale:** A feature that touches the raw connection spreads subscription strings and payload shapes through business code. Narrow use-case-specific functions show each subscription's inputs and effects beside the feature that owns them.

**Reviewed for:** Features subscribe through use-case-specific functions, or the realtime-subscription convention names how they subscribe.

## Dependencies

Packages: Selecting SignalR adds `@microsoft/signalr`. The project records a decision for the package and pins it in the manifest.

Related extensions: `integrations`, `jobs`.

Guides: None.
