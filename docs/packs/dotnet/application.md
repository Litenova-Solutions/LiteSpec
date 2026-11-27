# .NET Application Layer

**Stack:** dotnet
**Concern:** application

Application code in a .NET backend sends commands and queries through mediators from LiteBus, the pinned messaging library. It is for developers who write command handlers, query handlers, reactions, and jobs. A handler that reaches past its three steps takes over work the pipeline owns, so each handler loads, calls, and stages, and the pipeline does the rest.

## Concepts

### Commands, Queries, and Mediators

A command is a message that changes business state, and a query reads from a read model without changing state. Commands travel through `ICommandMediator` and queries through `IQueryMediator`, so the write path and the read path stay visibly separate at every call site. The command pipeline around the handler runs validation, authorization, the handler itself, and then one commit. The LiteBus version comes from the manifest, and commands and queries keep their separate entry points in every project.

An endpoint, an event handler, and a job hold the command mediator. A command handler, query handler, reaction, or job holds the query mediator for one purpose: sending another module public query with a declared `queries` edge. A validator, step, or domain type sends nothing, no type in a module project publishes an event, and no type outside the platform holds the event mediator.

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

In Claimwick, approving an expense sends `ApproveExpenseCommand` through `ICommandMediator`: validators check the input, the pipeline authorizes the approver, the handler records the approval, and one commit stores it. The message carries the pipeline-stamped timestamp as `HandledAt`, so the handler stamps decisions without reading a clock.

A validator checks structural input, such as an empty title or a malformed identifier, before Domain runs. Each finding names the input member, a stable reason code, and a caller-safe message, with no HTTP status. The pipeline runs every validator for the message and merges the findings, so the caller learns about all input problems in one answer instead of fixing them one round trip at a time. Validity values come from the pinned mediator abstractions, so every project reports the same shape.

A query handler projects through the read session, `IQuerySession`, applying filter, scope, ordering, and limit before materialization. It takes no repository and stages, stores, deletes, or dispatches nothing. A read that writes makes retries and caching unsafe: repeating the query would repeat the write. An audited read is the pipeline work, not the handler. The list-expenses query projects its rows through `IQuerySession`, taking no repository and staging nothing.

### Reactions and Their Entry Points

A reaction answers a domain event of its own module, a module event, or an inbound integration event in one class that carries one mark naming its reaction record, and it sends at most one command. The class sits in the `Application/` folder of the use case it runs, or in the `Application/Notifications/` or `Application/ExternalSystems/` folder of the notification it sends or the system it calls. It honors the record `reads`, its idempotency, and its `onFailure`, and it writes no aggregate directly: a reaction that needs a write sends one command.

Each handler subscribes by implementing its event-handler interface for one event type. The reaction record states the facts its handlers answer in `triggeredBy`, so a handler subscribed to an event the record omits answers a fact nobody reviewed, while a named trigger with no subscribed handler is a promise the code does not keep. The check resolves each subscribed type through the event own mark, so the mark on the event class is the link the reader follows. Under per-handler delivery, each handler gets its own envelope, so one failing handler retries alone, and the subscriber name keeps envelopes reaching the handler after the class is renamed. LiteBus validates the names at composition.

```csharp
// Lending/Notifications/LoanDue/SendDueNoticeOnLoanBorrowedHandler.cs
/// <implements>reaction/lending.send-due-notice-on-loan-borrowed</implements>
internal sealed class SendDueNoticeOnLoanBorrowedHandler
{
    // Answers domain-event/loan.loan-borrowed by sending notification/lending.loan-due.
}
```

A command handler changes its aggregate and returns; it dispatches nothing. The aggregate raises its facts, and the outbox stages them in the same commit through `outbox.StageCollectedEvents()`. The platform collects each module event and each outbound integration event beside its domain event, built by its static `From` method from that domain event alone, so no module code publishes. After that commit, an event handler answers each fact through its entry point: the messenger port for `sends`, its external-system port for `calls`, and `ICommandMediator` for `runs` and for each declared `commands` edge, sending each command in its own pipeline and commit. An undeclared dispatch fails `effect-undeclared`, whatever its entry point.

```csharp
// Claimwick.Spending.Contracts/Expenses/OnExpenseReimbursed/ExpenseReimbursedModuleEvent.cs
public sealed record ExpenseReimbursedModuleEvent(ExpenseId ExpenseId, EmployeeId EmployeeId, Money Amount, DateTimeOffset ReimbursedAt)
{
    public static ExpenseReimbursedModuleEvent From(ExpenseReimbursedDomainEvent fact) =>
        new(fact.ExpenseId, fact.EmployeeId, fact.Amount, fact.ReimbursedAt);
}
```

A handler whose effect the caller reads when the command returns, such as a read-model projection the next screen reads, carries `[DispatchImmediately]`, and the outbox registers immediate dispatch with a ten-second timeout and a thirty-second fallback delay. LiteBus dispatches those envelopes in process after the unit of work commits and before `SendAsync` returns, so the caller reads its own write. A failed or timed-out dispatch stays with the outbox processor retry, and the command outcome stays unchanged. The deployment reports the immediate dispatch count with its outcome tag and the immediate dispatch duration beside the outbox backlog indicators, through the counter `litebus.outbox.immediate_dispatch.count` and the histogram `litebus.outbox.immediate_dispatch.duration`.

### Jobs That Resume

A scheduled or one-off job runs as one hosted operation carrying one mark naming its job record. It sits in the `Application/` folder of the use case it runs, or in the `Application/Notifications/` or `Application/ExternalSystems/` folder of the notification it sends or the system it calls. It registers on the declared execution host and honors its record selection, batch size, lease, and cursor, so a restart resumes where the last run stopped instead of reprocessing or skipping items. It sends its one command once per selected item, and records its evidence where the record states.

```csharp
/// <implements>job/spending.erase-expired-personal-data</implements>
internal sealed class EraseExpiredPersonalDataHost
{
    // Lives in Spending/Application/Expenses/ErasePersonalData/.
    // Selects the expired expenses and sends one erase command per expense, resuming from its cursor.
}
```

The host verifies its durable surface when it composes. Every durable message contract survives a serialize-and-deserialize round trip with its identifiers intact; every domain event the outbox stages has a handler or an explicit ignore; and the reference data the product loads validates fully and covers every date in use. A startup failure names the contract or row, so the breakage points at its fix, and composition itself runs the verification on every boot, so no dedicated tests carry it. In Claimwick, composition builds one instance of each durable contract, serializes and deserializes it, and compares the identifiers; when the expense-reimbursed envelope loses its expense identifier in the round trip, startup fails with a message naming that contract.

### Idempotent Resumable Jobs and Time Zones

A job selects items, sends one command per item with an idempotency key derived from the occurrence, and keeps a cursor so a restart resumes. The key ties the command to the occurrence it serves: the same occurrence selected again replays or turns away instead of running twice. The cursor records the last processed item, and each batch stores its cursor before reading the next one.

Business-local times schedule in the business time zone while instants store in UTC. The job converts the stored UTC instant to the business zone to decide what is due, and the command carries the UTC instant the handler stamps. A reminder due at nine in the morning in Berlin fires at nine in Berlin in winter and in summer alike, while the stored `HandledAt` stays a UTC instant the audit trail compares directly.

```csharp
internal sealed class SendApproverReminderHost
{
    private static readonly TimeZoneInfo Berlin =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

    // Selects one batch of submitted expenses past the reminder window,
    // sends one command per expense, and stores the cursor before the next batch.
    public async Task RunBatchAsync(
        ICommandMediator commands,
        IExpenseReminderSelection selection,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var todayLocal = TimeZoneInfo.ConvertTime(nowUtc, Berlin).Date;

        foreach (var item in selection.NextBatch())
        {
            var key = $"reminder:{item.ExpenseId.Value}:{todayLocal:yyyy-MM-dd}";
            await commands.SendAsync(
                new SendApproverReminderCommand(item.ExpenseId, nowUtc, key),
                cancellationToken);
        }

        selection.StoreCursor();
    }
}
```

## Standards

### Bind Command Handlers to Load, Call, and Stage

**ID:** `litespec/packs/dotnet/application#bind-command-handlers-to-load-call-and-stage`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** A command handler MUST load or create one aggregate through its repository, call one domain method on it, stage it, and return the result.

**Rationale:** The handler body has three steps: the load or creation, the one domain call, and the stage. The throwing `GetByIdAsync` serves loads behind a command that continues, and the nullable `FindBy...Async` serves lookups where absence is normal. Messages, provider calls, and writes to other aggregates run in event handlers after the commit, and the commit itself stays a pipeline stage with validation, authorization, idempotency, audit, and holds. The handler takes the query mediator for one purpose: sending another module public query with a declared `queries` edge.

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

A use case whose record declares `consistency: atomic` over several aggregates is the one exception: the handler loads, calls, and stages each aggregate its `writes` names, one method each.

**Checked by:** `dotnet build` reports `LS2012` for a handler that throws a forbidden refusal or takes the authorization guard, `LS2013` for an audit call that reads the message, `LS2014` for a find followed by a create, `LS2015` for a store inside a loop, and `LS2021` for a handler holding a command or event mediator.

### Call Shared Logic Instead of Copying It

**ID:** `litespec/packs/dotnet/application#call-shared-logic-instead-of-copying-it`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler, reaction, job

**Statement:** A command handler, query handler, event handler, or job MUST call shared logic from its one home, the aggregate, a domain service, or the knowledge model, instead of carrying a copy of another slice's method body.

**Rationale:** A handler body has three steps: the load, the one domain call, and the stage. A rule, calculation, or projection copied into the handler adds a fourth step that no record owns and no other slice shares, so each fix lands in every copy or misses one. Calling the aggregate method, the domain service, or the knowledge model keeps the handler at its three steps and the logic behind one call. This provision binds `litespec/core/domain-model#keep-one-home-for-each-rule-calculation-and-projection` for .NET.

**Example:** Claimwick submit-expense handler calls `Expense.Submit` with the receipt threshold it loaded, and the approve-expense handler calls `expense.Approve`: each handler holds its three steps while the expense aggregate holds the rules. A per-diem walk copied into both handlers would report `logic-duplicate`; both call the travel knowledge model instead.

**Checked by:** `litespec model check` reports `logic-duplicate` (level S) for one method or local-function body of at least 6 statements or 80 tokens carried in two or more different folders.

### Dispatch Declared Effects Through Their Entry Points

**ID:** `litespec/packs/dotnet/application#dispatch-declared-effects-through-their-entry-points`

**Strength:** required

**Applies to:** artifact: reaction, job

**Statement:** An event handler or job MUST dispatch each declared `sends` effect through the messenger port, each declared `calls` effect through its external-system port, its one declared `runs` effect through `ICommandMediator`, and each declared `commands` edge through `ICommandMediator`, after the commit that stored the fact it answers, while the pipeline stages each raised domain event, each module event translating it, and each outbound integration event translating it in the same commit.

**Rationale:** A command handler changes its aggregate and returns; it dispatches nothing. The aggregate raises its facts, and the outbox stages them in the same commit through `outbox.StageCollectedEvents()`. The same commit stages each module event and each outbound integration event whose record translates a raised domain event, with the platform collecting each one beside its domain event. Each is built by its static `From` method from that domain event alone, so no module code publishes. After that commit, an event handler answers each fact through its entry point, sending each command in its own pipeline and commit. Each handler gets its own delivery under [Outbox](../../core/outbox.md), so one failing handler retries alone. This provision binds `litespec/core/backend-application#dispatch-only-declared-effect-edges` for .NET.

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

### Subscribe Each Reaction Handler to Its Declared Triggers

**ID:** `litespec/packs/dotnet/application#subscribe-each-reaction-handler-to-its-declared-triggers`

**Strength:** required

**Applies to:** artifact: reaction

**Statement:** The event handlers marked with a reaction MUST together subscribe to exactly the events the reaction names in `triggeredBy`.

**Rationale:** The reaction record states the facts its handlers answer, and each handler subscribes by implementing its event-handler interface for one event type. A handler subscribed to an event the record omits answers a fact nobody reviewed, while a named trigger with no subscribed handler is a promise the code does not keep. The check resolves each subscribed type through the event own mark, so the mark on the event class is the link the reader follows. This provision binds [Backend Application](../../core/backend-application.md) for .NET, where the subscription is the event type of the handler event-handler interface.

**Example:** Claimwick reaction `reaction/spending.send-approver-reminders-on-expense-submitted` names `domain-event/expense.submitted` in `triggeredBy`: `SendApproverRemindersOnExpenseSubmittedHandler` implements `IEventHandler<ExpenseSubmittedDomainEvent>`, and `ExpenseSubmittedDomainEvent` carries the mark `domain-event/expense.submitted`.

**Checked by:** `litespec model check` reports `reaction-subscription-drift` (level S) for a marked handler subscribed to an event outside its reaction's `triggeredBy`, and `reaction-trigger-unhandled` (level S) for a declared trigger with no marked handler subscribed to it.

### Bind the Pipeline to the Pinned LiteBus APIs

**ID:** `litespec/packs/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An Application MUST compose its command pipeline through the LiteBus APIs this provision names for commit, authorization, audit, idempotency, fault classification, delivery, event staging, immediate dispatch, and the timestamp.

**Rationale:** One pinned binding per position keeps every project pipeline identical. The commit runs through `commands.UseMartenUnitOfWork()`, registered at `HandlerPriorities.UnitOfWork`, so the pipeline commits once. Authorization runs through a guard derived from `LiteBus.Messaging.Abstractions.AuthorizationGuard<TMessage>`, reading the declaration through `DeclarationGuard<TMessage, TValue>` from the message `{Message}AuthorizationDefinition` beside the message. The project answers `AuthorizeAsync(policy, message, cancellationToken)` for the declared policy; LiteBus carries the policy without evaluating it.

Audit runs through `AuditDeclaration.Audited(...).WithTargetFrom<TCommand>(...).WithReasonFrom<TCommand>(...)` in the message `{Message}AuditDefinition` from `LiteBus.Messaging.Abstractions.Auditing`. The chain runs through `AddAuditing(auditing => auditing.UseMartenStore().UseHashChain(chain => ...))`, writing its records in the Marten session of the business unit of work, so a captured record commits or rolls back with the change. `StoreAs<TDocument, TMapper>()` names the stored document and its `IAuditRecordMapper<TDocument>`, with `SegmentBy`, `SignCheckpointsWith`, `CheckpointWhen`, `PublishCheckpointsTo`, and `VerifyAgainst` filling the remaining positions, and the Marten store registers its `IAuditCanonicalizer<TDocument>` as a replaceable default.

Idempotency runs through the message `{Message}IdempotencyDefinition`, declaring the message idempotency beside the message. A record of mode `key` keys the message with its key template: `replay: returns-first-result` declares `replayResult: true`, while `replay: refuses` and `replay: acts-again` declare `false`. A record of mode `state` or `none` declares an exemption instead. Fault classification runs through `UseFaultRefusalClassifierInstance(new PostgreSqlConstraintRefusalClassifier(map))`, mapping each store fault to its refusal. Delivery runs through `outbox.EnablePerHandlerDelivery()`, reaching each handler separately.

Event staging runs through `outbox.StageCollectedEvents()`, draining the facts `IDomainEventCollector` holds into the same commit as the aggregate change. Harvesting moves the raised domain events into that collector: `outbox.HarvestDomainEvents(...)` on the outbox builder names the aggregate root base, and the Marten harvester registers on the storage builder through `postgres.UseDataSource(dataSource).UseMartenTransactionalOutbox().HarvestDomainEvents()`. Each domain event type takes one decision: `TranslateDomainEvent<TEvent>(...)` stages the module and integration events it translates, `IgnoreDomainEvent<TEvent>()` drops it, and `RequireDomainEventDecision()` fails staging before the commit when a harvested event has no decision. Each translation builds its module event by its static `From` method from the domain event alone.

In-process dispatch runs through `outbox.UseInProcessDispatch()` with `outbox.EnableOutboxProcessor()`, against the product PostgreSQL database, with transport dispatchers toward the external system the record names in `through`. Immediate dispatch runs through `outbox.EnableImmediateDispatch(...)`, and the timestamp through `IExecutionContext.HandledAt`.

Each message declares its pipeline positions in definition classes in its use case `Application/` folder: `{Message}AuthorizationDefinition`, `{Message}AuditDefinition`, and `{Message}IdempotencyDefinition`, each in its own file and implementing exactly one definition interface. LiteBus verifies at composition through `RequireDeclaration` that every message carries them, and the project declares authorization, audit, and idempotency through definitions alone.

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

Claimwick reimburse-expense command carries `ReimburseExpenseCommandAuthorizationDefinition`, `ReimburseExpenseCommandAuditDefinition`, and `ReimburseExpenseCommandIdempotencyDefinition` in its use case `Application/` folder, each implementing its one definition interface.

**Reviewed for:** Does every pipeline position use its pinned LiteBus API? Check the command pipeline composition.

### Pass Explicit Mediation Settings to Nested Use Cases

**ID:** `litespec/packs/dotnet/application#pass-explicit-mediation-settings-to-nested-use-cases`

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

None.