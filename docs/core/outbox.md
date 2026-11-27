# Durable Delivery

**Stack:** any

**Concern:** infrastructure

Durable delivery carries committed changes to their readers through the outbox: the product stages each after-commit effect in the same transaction as its changes, and a Worker delivers it later. This page states how a project stages, claims, dispatches, and replays those effects. It is for developers who send facts across modules or systems and operators who replay them. A message sent from memory dies with the process, so the change and its message commit together.

## Concepts

### How Effects Enter the Outbox

A use case stages each raised domain event, each module event translating one of them, and each integration event translating one of them, in the outbox in the same transaction as its changes. One global pipeline step does the staging, and user code stages nothing. The business change and its message commit together, so a crash leaves neither an accepted change without its message nor a message without its change.

```csharp
// One pipeline step stages every raised fact with the aggregate changes.
pipeline.StageOutbox(raisedDomainEvents, moduleTranslations, integrationTranslations);
```

Each domain event reaches one decided outcome: a reaction or a translating event consumes it, or its record states `ignored: true` with matching code registration. Claimwick's `domain-event/expense.submitted` feeds the approver-reminder reaction and the expense-submitted integration event, while `domain-event/expense.approved` states `ignored: true`. A staged event with no handler fails the raising command at commit time, and an event the code drops although the records consume it skips its reaction.

Module facts travel in process through the outbox with no broker, since a module event is a contract between modules of one product delivered from the same database that holds the business work. Integration events leave through the transport their record names in `through`, and the integration-event record states the direction, the payload with its classifications, and the trust readers place in it. A projection the caller reads dispatches in process right after the commit for read-your-writes, with the outbox processor staying the fallback that retries whatever the immediate dispatch misses.

A reaction or job whose loss the operators accept and repeat by hand carries no outbox records, and its `onFailure` states how failures are handled. The facts raised by the use case such an excluded reaction or job runs publish after that use case's commit succeeds, with no outbox record staged. Publishing before the commit fires for a change the database then rolls back.

### How the Worker Delivers

The Worker claims pending records in a short transaction naming the lease owner, expiry, and fencing value, then releases the claim before any network call. It dispatches a bounded batch and records each outcome, while WebApi commits only to make the message eligible. Holding the claim across the call pins locks and connection slots the database needs for other work.

Each outbox record carries identity, kind, type, schema version, time, payload, tenant, state, attempt, and next-attempt data, with the last error summary and the completion time for the operator. Each message carries a stable type name with an explicit schema version, so the dispatcher tells which reader parses each record written by an older deployment. The writer serializes each pending message into an outbox record, giving the later Worker everything it needs once the request scope is gone.

Each record names its kind: a module event with its module-event record, an integration event with its integration-event record, a flow-step command, or a provider side effect. Each handler gets its own delivery with its own retries and dead letter, so one failing handler retries on its own schedule while the others stay complete. In the library product, a loan-borrowed event feeds a due-notice delivery and a statistics delivery that complete independently.

Delivery is at-least-once: the target accepts a message while the Worker crashes before recording completion, and the requeued claim dispatches it again. Each event handler absorbs the repeat with the message ID or a business idempotency key at its command or side-effect boundary, keeping correct state when the same message arrives twice. The record moves through pending, processing, dispatched, and dead-letter, and audited replay moves dead-letter back to pending.

New writers wait until every active Worker reads their type and schema version, and each release deploys compatible readers before the writers that emit new shapes. During rollout and rollback, old and new records share one store, so the dispatcher keeps a reader for every application version that coexists. Adding an optional member with defined absent-value behavior needs no new version, while removing or renaming a member, narrowing a type, or changing a discriminator needs one.

### How Operators See Delivery

Pending count, failed count, oldest pending age, attempts, dispatch duration, and success rate show whether delivery keeps up. Growth in the backlog or its age reveals a stall, while duration and success rate reveal a struggling target. Each use case sets alert thresholds from its own delivery target, held in setting records.

A dead store and a failed delivery carry separate signals: store availability reads as a gauge the readiness endpoint serves, while each claimed message outcome reads as a counter. A single error counter leaves the operator guessing whether to restore a database or investigate one destination. A Worker facing a dead store backs off with rate-limited logging instead of filling disks with one exception per iteration.

Retry and poison handling touch claimed records only, since a message with no claim waits for no delivery and double-counts attempts under an outage. A host that reports unready stops dispatch and stops polling stores that serve no requests. Each replay records its actor, reason, time, and failure without editing the payload, so a duplicate delivery reads as an operator decision instead of a dispatcher bug.

Published events stay available apart from the delivery records for as long as a read model needs them for a rebuild, with erased personal fields removed or the key shredded. Claimwick rebuilds its reimbursed-expense read model by replaying the kept expense-reimbursed module events while the outbox deletes its delivery records after seven days. A retained event keeps its shape with the erased fields removed, and the read model carries an erasure tombstone for the erased key.

The outbox record, storage, claim, and dispatch code live together under `Infrastructure/Messaging/Outbox/`, and the Worker host runs as its own project with its own lifetime, scaling, and configuration. Provider mapping lives in the integration that owns the side effect, so a provider change touches one integration instead of the shared dispatch path. A flow-step command travels through `ICommandMediator` in a fresh Worker scope, running the whole application pipeline for it. A durable bus package arrives only with a manifest pin and an integration test proving it commits the message in the same transaction as the business work.

Each record update checks the current lease and fencing value, so a Worker that lost its lease through a crash, a pause, or an expiry leaves the current owner's result intact. A claim whose lease expired returns to eligible work for a live Worker, so one crash delays a message instead of losing it.

Module facts consumed in process are scheduled through [Scheduled Jobs and Reports](jobs.md). Provider calls behind durable effects are isolated per the integrations design. Live push updates beyond polling belong to the `realtime` extension.

## Standards

### Decide Every Domain Event Outcome

**ID:** `litespec/core/outbox#decide-every-domain-event-outcome`

**Strength:** required

**Applies to:** artifact: domain-event

**Statement:** Each domain event MUST reach exactly one decided outcome in both the records and the code's outbox decision: a reaction or a translating module or integration event consumes it, or its record states `ignored: true`.

**Rationale:** A staged event with no handler fails each raising command at commit time. An event the code drops although the records consume it skips its reaction. Deciding each event once in the records, with matching code registration, keeps staging and handling one fact.

**Example:** Claimwick's `domain-event/expense.submitted` is answered by the approver-reminder reaction and translated by the expense-submitted integration event, so it states no `ignored`; `domain-event/expense.approved` states `ignored: true` because nothing answers it. The outbox registration translates the submitted event and ignores the approved event.

**Checked by:** `litespec model check` reports `domain-event-outcome-missing` (level G) for a domain event no reaction or translating event consumes that states no `ignored`, `domain-event-outcome-drift` (level G) for a domain event stating `ignored` that a reaction or translation consumes, and `outbox-decision-drift` (level S) for a code decision contradicting the records and `outbox-decision-missing` (level S) for a domain event type with no outbox decision.

### Keep Published Events Available for Read Model Rebuilds

**ID:** `litespec/core/outbox#keep-published-events-available-for-read-model-rebuilds`

**Strength:** required

**Applies to:** artifact: record:read-model, record:integration-event, record:module-event

**Statement:** A project MUST keep every module or integration event a read model names in `from`, `erasedBy` or `removedBy` apart from the outbox's delivery records for as long as a read model may need rebuilding from it, with erased personal fields removed or the event key shredded.

**Rationale:** A new or changed read model fills from the events the owner already published. An outbox that deletes delivered records after days leaves a new read model nothing to replay, forcing a hand-written backfill from the owner's tables.

**Example:** In Claimwick, the reporting module rebuilds its reimbursed-expense read model by replaying the kept expense-reimbursed module events, while the outbox deletes its delivery records after seven days. A retained event for an erased expense replays without the erased personal fields.

**Reviewed for:** Is every module or integration event a read model names kept apart from delivery for as long as a rebuild may need it, with erased fields removed or the key shredded? Check the event log's retention beside the read models that read it.

### Stage Messages with Business Work

**ID:** `litespec/core/outbox#stage-messages-with-business-work`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A command post-handler MUST stage pending durable messages in the same scoped business session as its aggregate changes.

**Rationale:** The business change and its message persist together or not at all. Staging the message in the same scoped session as the aggregate changes puts both under one commit.

**Reviewed for:** Does every pending durable message stage inside the scoped session of its aggregate changes? Check each command post-handler and its session.

### Reject Independent Outbox Commits

**ID:** `litespec/core/outbox#reject-independent-outbox-commits`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An outbox writer MUST NOT use another connection or independent commit for required durable delivery.

**Rationale:** Two commits on two connections split around a crash: the change persists without its message, or the message persists for a change that rolled back. Sharing one PostgreSQL database leaves separate transactions non-atomic, so the message joins the business transaction.

**Reviewed for:** Does any outbox writer use a separate connection or commit? Check each writer source and its path into the business session.

### Release Claim Transactions Before Calls

**ID:** `litespec/core/outbox#release-claim-transactions-before-calls`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A Worker MUST NOT hold an outbox claim transaction open during a network call.

**Rationale:** A network call to a slow target lasts seconds or minutes. An open claim across that call pins locks and connection slots, starving the Workers behind it. Releasing the claim first lets the database serve other work while the call runs.

**Reviewed for:** Does any claim transaction stay open across a network call? Check the Worker's dispatch path.

### Accept Repeated Delivery

**ID:** `litespec/core/outbox#accept-repeated-delivery`

**Strength:** required

**Applies to:** artifact: reaction

**Statement:** An event handler that receives an outbox delivery MUST handle the same message more than once.

**Rationale:** The target accepts a message while the Worker crashes before recording completion, and the requeued claim dispatches it again. Each handler absorbs the repeat without corrupting its state, since work assuming exactly-once delivery breaks on the first uncertain failure.

**Reviewed for:** Does every event handler keep correct state when the same message arrives twice? Check the handler tests that deliver a message twice.

### Wait for Target Acceptance Before Completing

**ID:** `litespec/core/outbox#wait-for-target-acceptance-before-completing`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A Worker MUST NOT mark an outbox record complete before the target boundary accepts it.

**Rationale:** A completion marked before acceptance declares a delivery with no acceptance behind it, and no retry follows a complete-looking record. The Worker records completion after the target boundary accepts.

**Reviewed for:** Does any outbox record mark completion before the target accepts the message? Check the Worker's completion code and the target's acceptance response.

### Retry Delivery with Bounded Backoff and Jitter

**ID:** `litespec/core/outbox#retry-delivery-with-bounded-backoff-and-jitter`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An outbox dispatcher MUST use bounded exponential backoff with jitter.

**Rationale:** A failing target with ten thousand pending messages meets a retry storm when every message retries at once on a fixed interval. Bounded backoff with jitter spreads those retries over time and caps the total, so the target recovers instead of failing under the load.

**Reviewed for:** Does every retry schedule use a bound, backoff, and jitter? Check the dispatcher's retry code.

### Deploy Readers Before Writers

**ID:** `litespec/core/outbox#deploy-readers-before-writers`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A release MUST deploy compatible readers before writers for additive event versions.

**Rationale:** Old and new records share one store during rollout. Compatible readers deployed before the writers that emit new shapes keep every record dispatchable at each step, while the reverse order leaves new records without a reader until the readers catch up.

**Reviewed for:** Does every release plan deploy compatible readers before its additive writers? Check the deployment order in the release runbook.

## Conventions

None.
