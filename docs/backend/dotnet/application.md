# .NET Application Layer

**Stack:** dotnet
**Concern:** application

Application code in a .NET backend sends commands and queries through mediators from LiteBus, the pinned messaging library. Commands change business state, queries read without changing it, and validators report structural input problems as LiteBus validity values. This page defines how the shared coordination rules apply to .NET: the command-handler shape, the validity shape, the query-handler shape, the entry point for each declared effect, the use case folder layout, the pinned mediator methods, and the pinned pipeline bindings. The shared rules are stated on the [Application Layer](../application.md) page.

## Concepts

### Commands, Queries, and Mediators

A command is a message that changes business state, and a query reads from a read model without changing state. The boundary sends the message through a LiteBus mediator: commands travel through `ICommandMediator` and queries through `IQueryMediator`, so the write path and the read path stay visibly separate at every call site. The command pipeline around the handler runs validation, authorization, the handler itself, and then one commit.

A reaction answers a domain event of its own module, a module event, or an inbound integration event in one class that carries one mark naming its reaction record, and it sends at most one command. A scheduled or one-off job selects its items and sends its one command once per item.

Only an endpoint, an event handler, and a job hold the command mediator. A command handler, query handler, reaction, or job holds the query mediator only to send another module's public query with a declared `queries` edge. A validator, step, or domain type sends nothing, no type in a module project publishes an event, and no type outside the platform holds the event mediator.

A project with a `messages` generate target writes each command and query from its use case record: an internal sealed partial record with one positional property per input, the idempotency key, paging members, and tenant key the record implies, and the LiteBus interface over its result. A project with a `results` target writes each result beside its message: one property per output field, a nested record per output group, a row record per multi-field list, and the next cursor when the query pages. Hand-written members live in a sibling partial file.

In Claimwick, approving an expense sends `ApproveExpenseCommand` through `ICommandMediator`: validators check the input, the pipeline authorizes the approver, the handler records the approval, and one commit stores it.

### Validity Findings

A validator checks structural input, such as an empty title or a malformed identifier, before Domain runs. Each finding names the input member, a stable reason code, and a caller-safe message. The pipeline runs every validator for the message and merges the findings, so the caller learns about all input problems in one answer instead of fixing them one round trip at a time. A project with a `validators` generate target writes each validator from its use case record: one rule per constraint with the member, the reason code, and the message, refused through the derived `invalid-input` result, with hand-written checks in the partial hook after the generated rules.

### Message Declarations

Each message states its pipeline positions, authorization, audit, and idempotency, in one generated catalog source per module. A project with a `declarations` generate target declares every command and query through that file: an internal sealed partial class implementing the LiteBus `IMultiMessageDefinition` that calls `catalog.Describe` once per message with the authorization, audit, and idempotency its use case record states. Hand-written definition classes beside the message remain only where no catalog covers the project, or for a message the catalog skips.

Authorization declarations name one policy per message, and the project's guard evaluates the name. The default vocabulary reads the permission id for a permission grant, `signed-in`, `anonymous`, and `system` for each grant, and wraps the policy for a self grant or self scope as `Self(...)`, for a recent sign-in as `RecentSignIn(...,window)`, and for a use case the system also runs as `SystemOr(...)`. A project whose guard speaks another vocabulary names its own forms on the target. The guard parses each name and evaluates it against the caller: a permission check, a self check over the message's subject, a sign-in age against the window, or either a person grant or a system run.

In Claimwick, the spending catalog authorizes submit-expense under `permission/request-expense`, reimburse-expense under `RecentSignIn(permission/post-reimbursement,PT15M)`, and list-expenses under `Self(permission/list-expenses)`; every message trails its exemption reason through `NotAudited`, and submit-expense keys by its expense identity.

### Record Field Types in C#

A contract field maps to exactly one C# type, so a generator writes the same property type from the record on every message, result, event, and read model, and the field-drift check holds each hand-written property to it. The table states the mapping; each row follows the provision owning the C# form.

| Record type | C# type |
|:---|:---|
| `text`, `email` | `string` |
| `integer` | `int` |
| `decimal` | `decimal` |
| `money` | `Money`, or `decimal` with its currency beside it |
| `boolean` | `bool` |
| `date` | `DateOnly` |
| `instant` | `DateTimeOffset` |
| `duration` | `TimeSpan` |
| `choice` | The closed-set record the `set` names |
| `id` | The typed identifier of what `identifies` names, or `Guid` with no `identifies` |
| `list` | `IReadOnlyList<T>` with `T` from the row shape or the named row type |
| `file` | No single type: the endpoint binds the multipart upload, and an event carries the bytes as `byte[]` |

A money field rides as the shared `Money` object, as `litespec/backend/dotnet/domain#hold-every-monetary-amount-in-the-shared-money-object` states. A money field with a fixed `currency` rides as a `decimal` alone, and one with `currencyField` rides as a `decimal` with the sibling carrying the code. An id field identifying an aggregate rides as its typed identifier struct, as `litespec/backend/dotnet/domain#use-strongly-typed-version-7-identifiers` states, and one identifying an actor rides as a `Guid`, as does one with no `identifies`. A choice field rides as the closed-set record hierarchy, as `litespec/backend/dotnet/domain#model-every-closed-set-of-domain-values-without-enums` states. A list row of several fields rides as a nested record, or as the named row type when the field states `rowType`. A carried field naming an aggregate part rides as the part name in PascalCase, and one naming a C# type rides as that type. A file field states its media types and size for the endpoint and the validator; its message form follows the upload flow, so the drift check does not judge it. A file carried in an event payload rides as the bytes in `byte[]`, because the event announces content already stored rather than an upload in flight. The constraints exist so validators enforce them from the record.

In Claimwick, the submit-expense command carries an `ExpenseId`, a `decimal` amount with a `string` currency, and a `DateTimeOffset` handled time from its record.

## Standards

### Bind Command Handlers to Load, Call, and Stage

**ID:** `litespec/backend/dotnet/application#bind-command-handlers-to-load-call-and-stage`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** A command handler MUST load or create one aggregate through its repository, call one domain method on it, stage it, and return the result.

**Rationale:** The handler body has three steps: the load or creation, the one domain call, and the stage. A required load uses the throwing `GetByIdAsync`, and an optional lookup uses the nullable `FindBy...Async`, under `litespec/backend/dotnet/domain#keep-repository-interfaces-in-domain`.

The message carries the pipeline-stamped timestamp as `HandledAt` under `litespec/backend/application#read-time-from-the-scoped-clock` (see `litespec/backend/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis`), so the handler stamps decisions without reading a clock. Validation, authorization, idempotency, audit, holds, and the commit are pipeline stages, not handler code.

Messages, provider calls, and writes to other aggregates run in event handlers after the commit, under `litespec/backend/dotnet/application#dispatch-declared-effects-through-their-entry-points`. The handler takes no `ICommandMediator` or `IEventMediator`; it takes `IQueryMediator` only to send another module's public query with a declared `queries` edge.

A use case whose record declares `consistency: atomic` over several aggregates is the one exception: the handler loads, calls, and stages each aggregate its `writes` names, one method each. This provision binds `litespec/backend/application#keep-command-handlers-to-load-call-and-stage` and `litespec/backend/application#load-a-required-record-or-refuse-it` for .NET, where `GetByIdAsync` and `FindBy...Async` are the forms.

**Example:**

```csharp
internal sealed class ApproveExpenseCommandHandler
    : ICommandHandler<ApproveExpenseCommand, ApproveExpenseCommandResult>
{
    private readonly IExpenseRepository _expenseRepository;

    public ApproveExpenseCommandHandler(IExpenseRepository expenseRepository)
    {
        _expenseRepository = expenseRepository;
    }

    public async Task<ApproveExpenseCommandResult> HandleAsync(ApproveExpenseCommand command, CancellationToken cancellationToken)
    {
        var expense = await _expenseRepository.GetByIdAsync(command.ExpenseId, cancellationToken);
        expense.Approve(command.ApproverId, command.HandledAt);
        _expenseRepository.Store(expense);
        return new ApproveExpenseCommandResult(expense.Id);
    }
}
```

**Checked by:** `dotnet build` reports `LS2012` for a handler that throws a forbidden refusal or takes the authorization guard, `LS2013` for an audit call that reads the message, `LS2014` for a find followed by a create, `LS2015` for a store inside a loop, and `LS2021` for a handler holding a command or event mediator.

### Report Input Problems as Validity

**ID:** `litespec/backend/dotnet/application#report-input-problems-as-validity`

**Strength:** required

**Applies to:** artifact: validator

**Statement:** A validator MUST report its findings as Validity, with each ValidationFailure carrying a member, a code, and a message, and no HTTP status.

**Rationale:** Every validator for the message runs, and the pipeline merges the findings before it refuses the request, so the caller fixes the whole form at once. The member tells the caller which input failed, the stable code lets a client branch on the reason, and the message explains the problem in words safe to show. Validity values come from the pinned mediator's abstractions, named in `litespec/backend/dotnet/application#use-the-pinned-mediator-entry-points`, so every project reports the same shape. This provision binds `litespec/backend/application#report-input-problems-as-validity-findings` for .NET.

**Reviewed for:** Does every validator report through `Validity`, with member, code, and message on each finding and no HTTP status? Check each validator.

### Keep Query Handlers Free of Repositories and Writes

**ID:** `litespec/backend/dotnet/application#keep-query-handlers-free-of-repositories-and-writes`

**Strength:** required

**Applies to:** artifact: query-handler

**Statement:** A query handler MUST take no repository and stage, store, delete, or dispatch nothing.

**Rationale:** A read path acts on no invariant, so a repository in a query handler loads an aggregate the query has no write to justify. The handler projects through the read session under `litespec/backend/dotnet/persistence#query-through-iquerysession`.

A read that writes makes retries and caching unsafe: repeating the query would repeat the write. An audited read is the pipeline's work, not the handler's. `dotnet build` reports a repository in a query handler as `LS2011`. This provision binds `litespec/backend/application#project-queries-directly` and `litespec/backend/application#keep-query-handlers-free-of-writes` for .NET.

**Example:** The list-expenses query projects its rows through `IQuerySession`, taking no repository and staging nothing.

**Reviewed for:** Does every query handler project through `IQuerySession`, with no repository and no stage, store, delete, or dispatch? Check each query handler; `dotnet build` reports LS2011 for a repository in a query handler and LS2012 for a forbidden refusal or the authorization guard.

### Call Shared Logic Instead of Copying It

**ID:** `litespec/backend/dotnet/application#call-shared-logic-instead-of-copying-it`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler, reaction, job

**Statement:** A command handler, query handler, event handler, or job MUST call shared logic from its one home, the aggregate, a domain service, or the knowledge model, instead of carrying a copy of another slice's method body.

**Rationale:** A handler body has three steps: the load, the one domain call, and the stage. A rule, calculation, or projection copied into the handler adds a fourth step that no record owns and no other slice shares, so each fix lands in every copy or misses one. Calling the aggregate method, the domain service, or the knowledge model keeps the handler at its three steps and the logic behind one call. This provision binds `litespec/core/domain-model#keep-one-home-for-each-rule-calculation-and-projection` for .NET.

**Example:** Claimwick's submit-expense handler calls `Expense.Submit` with the receipt threshold it loaded, and the approve-expense handler calls `expense.Approve`: each handler holds its three steps while the expense aggregate holds the rules. A per-diem walk copied into both handlers would report `logic-duplicate`; both call the travel knowledge model instead.

**Checked by:** `litespec model check` reports `logic-duplicate` (level S) for one method or local-function body of at least 6 statements or 80 tokens carried in two or more different folders.

### Dispatch Declared Effects Through Their Entry Points

**ID:** `litespec/backend/dotnet/application#dispatch-declared-effects-through-their-entry-points`

**Strength:** required

**Applies to:** artifact: reaction, job

**Statement:** An event handler or job MUST dispatch each declared `sends` effect through the messenger port, each declared `calls` effect through its external-system port, its one declared `runs` effect through `ICommandMediator`, and each declared `commands` edge through `ICommandMediator`, after the commit that stored the fact it answers, while the pipeline stages each raised domain event, each module event translating it, and each outbound integration event translating it in the same commit.

**Rationale:** A command handler changes its aggregate and returns; it dispatches nothing. The aggregate raises its facts, and the outbox stages them in the same commit through `outbox.StageCollectedEvents()` (see `litespec/backend/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis`). The same commit stages each module event and each outbound integration event whose record translates a raised domain event. The platform collects each one beside its domain event. Each is built by its static `From` method from that domain event alone, so no module code publishes.

After that commit, an event handler answers each fact through its entry point: the messenger port for `sends`, its external-system port for `calls`, and `ICommandMediator` for `runs` and for each declared `commands` edge, sending each command in its own pipeline and commit. A scheduled or one-off job dispatches through the same entry points. Each handler gets its own delivery under `litespec/ext/outbox#give-each-handler-its-own-delivery`, so one failing handler retries alone. An undeclared dispatch fails `effect-undeclared`, whatever its entry point. This provision binds `litespec/backend/application#dispatch-only-declared-effect-edges` for .NET.

**Example:**

```csharp
// The commit step stores the changed aggregate and stages the facts it raised in one transaction.
outbox.StageCollectedEvents();

// Claimwick.Spending.Contracts/Expenses/OnExpenseReimbursed/ExpenseReimbursedModuleEvent.cs: built from its domain event alone.
public sealed record ExpenseReimbursedModuleEvent(ExpenseId ExpenseId, EmployeeId EmployeeId, Money Amount, DateTimeOffset ReimbursedAt)
{
    public static ExpenseReimbursedModuleEvent From(ExpenseReimbursedDomainEvent fact) =>
        new(fact.ExpenseId, fact.EmployeeId, fact.Amount, fact.ReimbursedAt);
}
```

**Checked by:** `litespec model check` reports `effect-undeclared` for a dispatch no record declares, and `dotnet build` reports `LS2021` for a command or event mediator held outside an endpoint, event handler or job and `LS2022` for an event published outside the platform.

### Run Reactions in Dedicated Handlers

**ID:** `litespec/backend/dotnet/application#run-reactions-in-dedicated-handlers`

**Strength:** required

**Applies to:** artifact: reaction

**Statement:** A reaction MUST run in one class carrying one mark naming its reaction record.

**Rationale:** One class per reaction answers the trigger the record names in `triggeredBy`, and dispatches only through its `effects` entries. The class carries the mark `reaction/<module>.<name>` in the declared style, so the stack reader links it without guessing. It sits in the `Application/` folder of the use case it runs, or in the `Application/Notifications/` or `Application/ExternalSystems/` folder of the notification it only sends or the system it only calls, under `litespec/workspace/naming#place-each-trigger-with-what-it-runs`.

It never writes an aggregate directly: a reaction that needs a write sends one command. It honors the record's `reads`, its idempotency, and its `onFailure`. This provision binds `litespec/backend/application#answer-each-reaction-in-one-marked-class` for .NET.

**Example:**

```csharp
// Lending/Notifications/LoanDue/SendDueNoticeOnLoanBorrowedHandler.cs
/// <implements>reaction/lending.send-due-notice-on-loan-borrowed</implements>
internal sealed class SendDueNoticeOnLoanBorrowedHandler
{
    // Answers domain-event/loan.loan-borrowed by sending notification/lending.loan-due.
}
```

**Reviewed for:** Does every reaction run in one handler class with its record's mark, sending at most one command from the folder of what it runs? Check each event handler beside its reaction record.

### Subscribe Each Reaction Handler to Its Declared Triggers

**ID:** `litespec/backend/dotnet/application#subscribe-each-reaction-handler-to-its-declared-triggers`

**Strength:** required

**Applies to:** artifact: reaction

**Statement:** The event handlers marked with a reaction MUST together subscribe to exactly the events the reaction names in `triggeredBy`.

**Rationale:** The reaction record states the facts its handlers answer, and each handler subscribes by implementing its event-handler interface for one event type. A handler subscribed to an event the record does not name answers a fact nobody reviewed, while a named trigger with no subscribed handler is a promise the code does not keep. The check resolves each subscribed type through the event's own mark, so the mark on the event class is the link the reader follows. This provision binds `litespec/backend/application#answer-each-reaction-in-one-marked-class` for .NET, where the subscription is the event type of the handler's event-handler interface.

**Example:** Claimwick's reaction `reaction/spending.send-approver-reminders-on-expense-submitted` names `domain-event/expense.submitted` in `triggeredBy`: `SendApproverRemindersOnExpenseSubmittedHandler` implements `IEventHandler<ExpenseSubmittedDomainEvent>`, and `ExpenseSubmittedDomainEvent` carries the mark `domain-event/expense.submitted`.

**Checked by:** `litespec model check` reports `reaction-subscription-drift` (level S) for a marked handler subscribed to an event outside its reaction's `triggeredBy`, and `reaction-trigger-unhandled` (level S) for a declared trigger with no marked handler subscribed to it.

### Name Every Outbox Subscriber

**ID:** `litespec/backend/dotnet/application#name-every-outbox-subscriber`

**Strength:** required

**Applies to:** artifact: reaction

**Statement:** Every event handler of a module event or domain event MUST carry `[SubscriberName("<module>.<handler-name>")]`.

**Rationale:** Under per-handler delivery, each handler gets its own envelope, so one failing handler retries alone. The subscriber name keeps envelopes reaching the handler after the class is renamed. LiteBus validates the names at composition when per-handler delivery is on.

**Example:** The handler that schedules a payout when spending approves an expense carries `[SubscriberName("payouts.schedule-payout")]`.

**Reviewed for:** Does every event handler of a module event or domain event carry its subscriber name? Check each event handler.

### Report Immediate Dispatch Beside the Backlog

**ID:** `litespec/backend/dotnet/application#report-immediate-dispatch-beside-the-backlog`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A deployment that dispatches projections immediately MUST report the immediate dispatch count with its outcome and the immediate dispatch duration beside the outbox backlog indicators.

**Rationale:** The backlog indicators of `litespec/ext/outbox#publish-backlog-indicators` show whether the outbox processor keeps up. Immediate dispatch adds a path in front of the processor, so its own instruments show how often it runs and how long it takes, and a slow immediate dispatch reads as duration rather than as a backlog. `AddLiteBusOutboxMetrics()` from `LiteBus.Outbox.Extensions.OpenTelemetry` reports the counter `litebus.outbox.immediate_dispatch.count`, tagged `litebus.outbox.immediate_dispatch.outcome` with `published`, `failed`, `skipped` or `timed_out`, and the histogram `litebus.outbox.immediate_dispatch.duration`.

**Example:** Claimwick's reporting projection dispatches immediately. Its dashboard shows the immediate dispatch count by outcome and its duration beside the pending count and oldest pending age, so a run of `timed_out` outcomes explains a rising backlog.

**Reviewed for:** Does the deployment report the immediate dispatch count with its outcome tag and the immediate dispatch duration beside the backlog indicators? Check the exported metrics beside the outbox dashboard.

### Run Scheduled Jobs as Hosted Operations

**ID:** `litespec/backend/dotnet/application#run-scheduled-jobs-as-hosted-operations`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A scheduled or one-off job MUST run as one hosted operation carrying one mark naming its job record.

**Rationale:** The hosted operation sits in the `Application/` folder of the use case it runs, or in the `Application/Notifications/` or `Application/ExternalSystems/` folder of the notification it only sends or the system it only calls. It registers on the declared execution host. It honors its record's selection, batch size, lease, and cursor, so a restart resumes where the last run stopped instead of reprocessing or skipping items. It sends its one command once per selected item, and records its evidence where the record states. This provision binds `litespec/backend/application#run-each-job-within-its-declared-selection` for .NET.

**Example:**

```csharp
/// <implements>job/spending.erase-expired-personal-data</implements>
internal sealed class EraseExpiredPersonalDataHost
{
    // Lives in Spending/Application/Expenses/ErasePersonalData/.
    // Selects the expired expenses and sends one erase command per expense, resuming from its cursor.
}
```

**Reviewed for:** Does every scheduled or one-off job run as one marked hosted operation honoring selection, batch size, lease, and cursor? Check each such job.

### Bind the Pipeline to the Pinned LiteBus APIs

**ID:** `litespec/backend/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An Application MUST compose its command pipeline through the LiteBus APIs this provision names for commit, authorization, audit, idempotency, fault classification, delivery, event staging, immediate dispatch, and the timestamp.

**Rationale:** One pinned binding per position keeps every project's pipeline identical:

- The commit runs through `commands.UseMartenUnitOfWork()`, which registers the LiteBus `IUnitOfWork` at `HandlerPriorities.UnitOfWork`, so the pipeline commits once under `litespec/backend/dotnet/persistence#commit-once-in-the-command-pipeline`.
- Authorization runs through a guard derived from `LiteBus.Messaging.Abstractions.AuthorizationGuard<TMessage>`, which reads the declaration through `DeclarationGuard<TMessage, TValue>`. The guard reads the declaration from the generated module catalog, or from the message's `{Message}AuthorizationDefinition`, which implements `IAuthorizationDefinition<TMessage>` beside the message where no catalog covers the project, under `litespec/backend/application#enforce-target-authorization-in-the-pipeline`. The project's `AuthorizeAsync(policy, message, cancellationToken)` answers whether the caller holds the declared policy; LiteBus carries the policy and never evaluates it.
- Audit runs through `Audited` and `NotAudited` calls in the generated module catalog, or through `AuditDeclaration.Audited(...).WithTargetFrom<TCommand>(...).WithReasonFrom<TCommand>(...)` in the message's `{Message}AuditDefinition`, which implements `IAuditDefinition<TCommand>` from `LiteBus.Messaging.Abstractions.Auditing` where no catalog covers the project, declaring the target and reason beside the message under `litespec/backend/application#declare-the-audit-target-and-reason-beside-the-message`.
- The audit chain runs through `AddAuditing(auditing => auditing.UseMartenStore().UseHashChain(chain => ...))`, with `UseMartenStore` from `LiteBus.Auditing.Storage.Marten`. The chain writes its records in the Marten session of the business unit of work, so a captured record commits or rolls back with the change. `StoreAs<TDocument, TMapper>()` names the stored document and its `IAuditRecordMapper<TDocument>`, `SegmentBy<TSegmenter>()` the `IAuditSegmenter<TDocument>`, `SignCheckpointsWith<TSigner>()` the `ICheckpointSigner`, `CheckpointWhen(...)` the `ICheckpointPolicy`, `PublishCheckpointsTo<TPublisher>()` the `ICheckpointPublisher`, and `VerifyAgainst<TSource>()` the `ICheckpointSource`. The Marten store registers its `IAuditCanonicalizer<TDocument>` as a replaceable default.
- Idempotency runs through the generated module catalog, which keys or exempts each message, or through the message's `{Message}IdempotencyDefinition`, which implements `IIdempotencyDefinition<TMessage>` beside the message where no catalog covers the project. A record of mode `key` keys the message with its key template. A record of mode `key` with `replay: returns-first-result` declares `replayResult: true`, so a repeat returns the first result; with `replay: refuses` or `replay: acts-again` it declares `replayResult: false`, so a repeat brings no stored result. A keyed declaration that omits the flag replays nothing, so `model check` reports `idempotency-replay-drift` when its record states `returns-first-result`. A record of mode `state` or `none` declares an exemption: a state command recognizes the repeat by the state its `recognizedBy` names, and a pipeline key would outlive that state, so a grant, a revoke and a second grant would replay the first answer.
- Fault classification runs through `UseFaultRefusalClassifierInstance(new PostgreSqlConstraintRefusalClassifier(map))`, mapping each store fault to its refusal under `litespec/backend/persistence#map-a-constraint-violation-to-its-refusal`.
- Delivery runs through `outbox.EnablePerHandlerDelivery()`, reaching each handler separately, one envelope per subscribed handler so one failing handler retries alone, under `litespec/ext/outbox#give-each-handler-its-own-delivery`.
- Event staging runs through `outbox.StageCollectedEvents()`, which drains the facts `IDomainEventCollector` holds and stages them in the same commit as the aggregate change. Harvesting moves the domain events an aggregate raised into that collector before the drain, so a handler does not call `Collect` for them:
  - `outbox.HarvestDomainEvents(...)` on the outbox builder names the aggregate root base. Its open generic definition, such as `typeof(AggregateRoot<>)`, covers every aggregate built on the base and gives the accessor that reads and clears its buffered events. An aggregate that implements `IHasDomainEvents` harvests without it.
  - The Marten harvester is registered on the storage builder: `postgres.UseDataSource(dataSource).UseMartenTransactionalOutbox().HarvestDomainEvents()`.
  - Each domain event type takes one decision. `TranslateDomainEvent<TEvent>(...)` stages the module events and integration events it translates, and `IgnoreDomainEvent<TEvent>()` drops it. `RequireDomainEventDecision()` makes staging fail before the commit when a harvested event has no decision. A translation builds each module event by its static `From` method from the domain event alone.
  - Hand collection stays on `IDomainEventCollector` in three cases: a deletion by predicate or by id, which carries no document for the harvester to read; a carrier type that is never stored; and an event raised outside an aggregate.
- In-process dispatch runs through `outbox.UseInProcessDispatch()` with `outbox.EnableOutboxProcessor()`, delivering module events and domain-event reactions inside the process against the product's PostgreSQL database. A Marten project stores outbox rows in its own session and transaction through `LiteBus.Outbox.Storage.Marten` (`outbox.UsePostgreSqlStorage(postgres => postgres.UseDataSource(dataSource).UseMartenTransactionalOutbox().HarvestDomainEvents())`), and a project that persists through Entity Framework Core uses `LiteBus.Outbox.Storage.EntityFrameworkCore`. Envelopes commit with the business work; the processor retries a failed handler with the configured backoff and dead-letters at the bound. No broker is needed for anything inside the bounded context. An outbound integration event uses a transport dispatcher (`LiteBus.Outbox.Dispatch.Amqp` or `LiteBus.Outbox.Dispatch.Kafka`) toward the external system its record names in `through`.
- Immediate dispatch runs through `outbox.EnableImmediateDispatch(...)` with `[DispatchImmediately]` on the handlers the caller reads when the command returns, under `litespec/backend/dotnet/application#dispatch-read-model-projections-immediately`.
- The timestamp runs through `IExecutionContext.HandledAt`, stamping each message once for the handler to read as `HandledAt` under `litespec/backend/application#read-time-from-the-scoped-clock`.

A project with a `declarations` generate target declares every message through its generated module catalog, one partial class per module implementing `IMultiMessageDefinition`. Where no catalog covers the project, each message declares its pipeline positions in definition classes in its use case `Application/` folder: `{Message}AuthorizationDefinition`, `{Message}AuditDefinition`, and `{Message}IdempotencyDefinition`, each in its own file and implementing exactly one definition interface. A message the catalog skips keeps its hand-written definition files. LiteBus verifies at composition through `RequireDeclaration` that every message carries its positions. A definition carries no LiteSpec mark; the handler carries the use case mark. A project declares authorization, audit, and idempotency only through the catalog or definitions, never through attributes or custom guards. Settled by [decision/0103-dotnet-stack-binding](decision/0103-dotnet-stack-binding).

**Example:** A Marten project composes its pipeline as follows:

```csharp
commands.UseMartenUnitOfWork();
outbox.UsePostgreSqlStorage(postgres => postgres
    .UseDataSource(dataSource)
    .UseMartenTransactionalOutbox()
    .HarvestDomainEvents());
outbox.UseInProcessDispatch();
outbox.EnableOutboxProcessor();
outbox.EnablePerHandlerDelivery();
outbox.StageCollectedEvents();
```

Claimwick declares every spending and reporting message through its generated catalogs: `SpendingDeclarations` authorizes reimburse-expense under `RecentSignIn(permission/post-reimbursement,PT15M)` and exempts its audit and idempotency from the record, with no per-message definition files.

**Reviewed for:** Does every pipeline position use its pinned LiteBus API? Check the command pipeline composition.

### Verify Durable Contracts, Event Decisions, and Reference Data at Startup

**ID:** `litespec/backend/dotnet/application#verify-durable-contracts-event-decisions-and-reference-data-at-startup`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The host MUST verify every durable message contract, every staged domain event decision, and the loaded reference data when it composes, failing startup with a message that names the contract or row.

**Rationale:** A durable message contract that fails a round trip, a staged domain event with no handler and no explicit ignore, and a reference row that fails validation each surface at runtime as a failed delivery, a dropped fact, or a wrong answer. Composition-time verification moves each one to startup: (a) every durable message contract survives a serialize-and-deserialize round trip with its identifiers intact; (b) every domain event the outbox may stage has a handler or an explicit ignore; (c) the reference data the product loads validates fully under `litespec/backend/knowledge#validate-reference-data-before-serving` and covers every date in use under `litespec/core/domain-model#cover-every-date-in-use`. The verification runs with the host composition and carries no dedicated tests, because startup itself runs it on every boot. A startup failure names the contract or row, so the breakage points at its fix.

**Example:** In the Claimwick product, composition builds one instance of each durable contract, serializes and deserializes it, and compares the identifiers; it lists every domain event the outbox may stage and confirms each one has a handler or an explicit ignore; and it loads the reference data and validates each row. When the expense-reimbursed envelope loses its expense identifier in the round trip, startup fails with a message naming that contract.

**Reviewed for:** Does every host composition verify its durable contracts, event decisions, and reference data, failing startup with the contract or row it names? Look at the composition code beside the durable contracts, the event decisions, and the reference data load.

### Pass Explicit Mediation Settings to Nested Use Cases

**ID:** `litespec/backend/dotnet/application#pass-explicit-mediation-settings-to-nested-use-cases`

**Strength:** required

**Applies to:** artifact: reaction

**Statement:** An event handler that runs a nested use case MUST pass explicit mediation settings carrying the identity the nested use case runs as.

**Rationale:** A nested command sent with ambient settings runs as whoever the outer pipeline authorized, or as no identity at all. The nested use case then passes an access check meant for another caller or fails one it was meant to pass. Explicit mediation settings state the identity on the send, so the nested pipeline authorizes the identity the handler names.

**Example:** In the library product, the handler that answers `domain-event/loan.loan-borrowed` runs the mark-book-lent use case with explicit settings carrying the system identity:

```csharp
await _commandMediator.SendAsync(
    new MarkBookLentCommand(@event.BookId, @event.LoanId),
    mediationSettings,
    cancellationToken);
```

The settings carry the identity the nested command runs as, so its access check authorizes that identity.

**Checked by:** `dotnet build` reports `LS2034` for an event handler running a nested use case without explicit mediation settings.

## Conventions

### Use the Pinned Mediator Entry Points

**ID:** `litespec/backend/dotnet/application#use-the-pinned-mediator-entry-points`

**Strength:** default

**Applies to:** artifact: reaction, endpoint, job

**Default:** Dispatch a command through `ICommandMediator.SendAsync` only from an endpoint, an event handler, or a job, and a query through `IQueryMediator.QueryAsync` from those or from a handler with a declared `queries` edge, from the LiteBus version the manifest pins.

**Replacement:** A project names its dispatch entry points in its Application documentation.

**Rationale:** With one pinned library, every dispatch site in every project reads the same way, and an agent finds all callers of a message with one search. A project that replaces the default still keeps commands and queries on separate entry points: that separation is required by `litespec/backend/application#dispatch-commands-and-queries-through-separate-entry-points`, and no replacement removes it.

**Reviewed for:** Does every dispatch site use `ICommandMediator.SendAsync` or `IQueryMediator.QueryAsync` from the pinned LiteBus version? Check each dispatch site.

### Dispatch Read-Model Projections Immediately

**ID:** `litespec/backend/dotnet/application#dispatch-read-model-projections-immediately`

**Strength:** default

**Applies to:** artifact: reaction

**Default:** A handler whose effect the caller reads when the command returns, such as a read-model projection the next screen reads, carries `[DispatchImmediately]`, and the outbox registers immediate dispatch with a ten-second timeout and a thirty-second fallback delay.

**Replacement:** A project whose readers tolerate the processor's polling delay leaves immediate dispatch off.

**Rationale:** LiteBus dispatches those envelopes in process after the unit of work commits and before `SendAsync` returns, so the caller reads its own write. A failed or timed-out dispatch is left to the outbox processor's retry, and the command's outcome never changes. Immediate dispatch needs a registered unit of work (`commands.UseUnitOfWork<...>()`), an outbox dispatcher, and a store implementing `IOutboxTargetedLeaseStore`, which the shipped PostgreSQL, Entity Framework Core, and in-memory stores do. The fallback delay stays greater than the timeout.

**Example:**

```csharp
outbox.EnableImmediateDispatch(dispatch =>
{
    dispatch.Timeout = TimeSpan.FromSeconds(10);
    dispatch.FallbackDelay = TimeSpan.FromSeconds(30);
});
```

**Reviewed for:** Does every handler the caller reads when the command returns carry `[DispatchImmediately]` with immediate dispatch registered, and does every other handler leave it off? Check each event handler beside the outbox registration.

## Reference Example

This informative example demonstrates `litespec/backend/dotnet/architecture#keep-implementation-types-internal`, `litespec/backend/application#keep-command-handlers-to-load-call-and-stage`, `litespec/backend/dotnet/application#dispatch-declared-effects-through-their-entry-points`, and `litespec/workspace/naming#place-each-trigger-with-what-it-runs`.

```csharp
public sealed record CreateDraftCommand(
    PostId PostId,
    AuthorId AuthorId,
    string Title,
    DateTimeOffset HandledAt) : ICommand<CreateDraftCommandResult>;

public sealed record CreateDraftCommandResult(PostId PostId);

internal sealed class CreateDraftCommandHandler
    : ICommandHandler<CreateDraftCommand, CreateDraftCommandResult>
{
    private readonly IPostRepository _postRepository;

    public CreateDraftCommandHandler(IPostRepository postRepository)
    {
        _postRepository = postRepository;
    }

    public async Task<CreateDraftCommandResult> HandleAsync(
        CreateDraftCommand command,
        CancellationToken cancellationToken)
    {
        var post = Post.CreateDraft(
            command.PostId,
            command.AuthorId,
            PostTitle.Create(command.Title),
            command.HandledAt);

        _postRepository.Store(post);

        return new CreateDraftCommandResult(post.Id);
    }
}
```

The pinned LiteBus package signatures take precedence over illustrative example signatures.

### Change Another Aggregate from an Event Handler

```csharp
// Lending/Application/Loans/BorrowBook/BorrowBookCommandHandler.cs
internal sealed class BorrowBookCommandHandler
    : ICommandHandler<BorrowBookCommand, BorrowBookCommandResult>
{
    private readonly ILoanRepository _loanRepository;

    public BorrowBookCommandHandler(ILoanRepository loanRepository)
    {
        _loanRepository = loanRepository;
    }

    public async Task<BorrowBookCommandResult> HandleAsync(
        BorrowBookCommand command,
        CancellationToken cancellationToken)
    {
        var loan = Loan.Borrow(
            command.LoanId,
            command.BookId,
            command.ReaderId,
            command.HandledAt);

        _loanRepository.Store(loan);

        return new BorrowBookCommandResult(loan.Id);
    }
}
```

```csharp
// Lending/Application/Books/MarkBookLent/MarkBookLentOnLoanBorrowedHandler.cs
/// <implements>reaction/lending.mark-book-lent-on-loan-borrowed</implements>
internal sealed class MarkBookLentOnLoanBorrowedHandler
    : IEventHandler<LoanBorrowedDomainEvent>
{
    private readonly ICommandMediator _commandMediator;

    public MarkBookLentOnLoanBorrowedHandler(ICommandMediator commandMediator)
    {
        _commandMediator = commandMediator;
    }

    public Task HandleAsync(
        LoanBorrowedDomainEvent @event,
        CancellationToken cancellationToken) =>
        _commandMediator.SendAsync(
            new MarkBookLentCommand(@event.BookId, @event.LoanId),
            cancellationToken);
}
```

The command handler stages only the loan. Borrowing raises `LoanBorrowedDomainEvent`, and after the commit the event handler sends the one command of the mark-book-lent use case. That use case's own handler loads the book, records the lent state, and stages it in its own pipeline and commit. The event handler sits in that use case's `Application/` folder and writes no aggregate itself.
