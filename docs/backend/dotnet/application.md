# .NET Application Layer

**Stack:** dotnet
**Concern:** application

## Intent

Application code in a .NET backend sends commands and queries through mediators from LiteBus, the pinned messaging library: commands change business state, queries read without changing it, and validators report structural input problems as LiteBus validity values. This page binds that dispatch for .NET: the command-handler shape, the validity shape, the query-handler shape, the entry point for each declared effect, the use case folder layout, the pinned mediator methods, and the pinned pipeline bindings. The shared coordination rules live on [Application Layer](../application.md); this page holds only their .NET bindings.

## Concepts

### Commands, Queries, and Mediators

A command is a message that changes business state; a query reads a read model without changing state. The boundary sends the message through a LiteBus mediator, and the command pipeline around the handler runs validation, authorization, the handler itself, and then one commit. Commands travel through `ICommandMediator` and queries through `IQueryMediator`, so the write path and the read path stay visibly separate at every call site.

A reaction answers a domain event or an integration event in one class carrying one mark naming its reaction record, and sends at most one command. A scheduled or one-off job selects its items and sends its one command once per item. Only an endpoint, an event handler and a job hold a mediator: a command handler, query handler, validator, step or domain type sends nothing, and no type in the modules project publishes an event.

### Validity Findings

A validator checks structural input, such as an empty title or a malformed identifier, before Domain runs. Each finding names the input member, a stable reason code, and a caller-safe message. The pipeline runs every validator for the message and merges the findings, so the caller learns about all input problems in one answer instead of fixing them one round trip at a time.

## Standards

### Bind Command Handlers to Load, Call and Stage

**ID:** `litespec/backend/dotnet/application#bind-command-handlers-to-load-call-and-stage`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** A command handler MUST load or create one aggregate through its repository, call one domain method on it, stage it, and return the result.

**Rationale:** The handler body has three steps: the load or creation, the one domain call, and the stage. A required load uses the throwing `GetByIdAsync` and an optional lookup uses the nullable `FindBy...Async` under `litespec/backend/dotnet/domain#keep-repository-interfaces-in-domain`. The message carries the pipeline-stamped timestamp as `HandledAt` under `litespec/backend/application#take-one-timestamp-per-message` (see `litespec/backend/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis`), so the handler stamps decisions without reading a clock. Validation, authorization, idempotency, audit, holds, and the commit are pipeline stages; messages, provider calls, and writes to other aggregates run in event handlers after the commit under `litespec/backend/dotnet/application#dispatch-declared-effects-through-their-entry-points`. The handler takes no `ICommandMediator`, `IQueryMediator` or `IEventMediator`. A use case whose record declares `consistency: atomic` over several aggregates is the one exception: the handler loads, calls, and stages each aggregate its `writes` names, one method each. This provision binds `litespec/backend/application#keep-command-handlers-to-load-call-and-stage` and `litespec/backend/application#load-a-required-record-or-refuse-it` for .NET, where `GetByIdAsync` and `FindBy...Async` are the forms.

**Example:**

```csharp
internal sealed class ApproveExpenseCommandHandler(IExpenseRepository expenseRepository)
    : ICommandHandler<ApproveExpenseCommand, ApproveExpenseCommandResult>
{
    public async Task<ApproveExpenseCommandResult> HandleAsync(ApproveExpenseCommand command, CancellationToken cancellationToken)
    {
        var expense = await expenseRepository.GetByIdAsync(command.ExpenseId, cancellationToken);
        expense.Approve(command.ApproverId, command.HandledAt);
        expenseRepository.Store(expense);
        return new ApproveExpenseCommandResult(expense.Id);
    }
}
```

**Checked by:** `dotnet build` reports `LS2012` for a handler that throws a forbidden refusal or takes the authorization guard, `LS2013` for an audit call that reads the message, `LS2014` for a find followed by a create, `LS2015` for a store inside a loop, and `LS2021` for a handler holding a mediator.

### Report Input Problems as Validity

**ID:** `litespec/backend/dotnet/application#report-input-problems-as-validity`

**Strength:** required

**Applies to:** artifact: validator

**Statement:** A validator MUST report its findings as Validity, with each ValidationFailure carrying a member, a code, and a message, and no HTTP status.

**Rationale:** Every validator for the message runs, and the pipeline merges the findings before it refuses the request, so the caller fixes the whole form at once. The member tells the caller which input failed, the stable code lets a client branch on the reason, and the message explains the problem in words safe to show. Validity values come from the pinned mediator's abstractions, named in `litespec/backend/dotnet/application#use-the-pinned-mediator-entry-points`, so every project reports the same shape. This provision binds `litespec/backend/application#report-input-problems-as-validity-findings` for .NET.

**Reviewed for:** Which validators report outside `Validity`, or skip member, code, or message, or leak HTTP status? Look at each validator.

### Keep Query Handlers Free of Repositories

**ID:** `litespec/backend/dotnet/application#keep-query-handlers-free-of-repositories`

**Strength:** required

**Applies to:** artifact: query-handler

**Statement:** A query handler MUST take no repository and stage, store, delete or dispatch nothing.

**Rationale:** A read path acts on no invariant, so a repository in a query handler loads an aggregate the query has no write to justify. The handler projects through the read session under `litespec/backend/dotnet/persistence#query-through-iquerysession`. A read that writes makes retries and caching unsafe; an audited read is the pipeline's work, not the handler's. `dotnet build` reports a repository in a query handler as `LS2011`. This provision binds `litespec/backend/application#project-queries-directly` and `litespec/backend/application#keep-query-handlers-free-of-writes` for .NET.

**Example:** The list-expenses query projects its rows through `IQuerySession`, taking no repository and staging nothing.

**Reviewed for:** Which query handlers take a repository, skip `IQuerySession`, or stage, store, delete, or dispatch? Look at each query handler; `dotnet build` reports LS2011 for a repository in a query handler and LS2012 for a forbidden refusal or the authorization guard.

### Dispatch Declared Effects Through Their Entry Points

**ID:** `litespec/backend/dotnet/application#dispatch-declared-effects-through-their-entry-points`

**Strength:** required

**Applies to:** artifact: reaction, job

**Statement:** An event handler or job MUST dispatch each declared `sends` effect through the messenger port, each declared `calls` effect through its external-system port, and its one declared `runs` effect through `ICommandMediator`, after the commit that stored the fact it answers, while the pipeline stages each raised domain event and each integration event translating it in the same commit.

**Rationale:** A command handler changes its aggregate and returns; it dispatches nothing. The aggregate raises its facts, and the outbox stages them in the same commit through `outbox.StageCollectedEvents()` (see `litespec/backend/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis`). The same commit stages each integration event whose record translates a raised domain event: the shared kernel collects it beside its domain event, built by the integration event's static `From` method from that domain event alone, so no module code publishes. After that commit, an event handler answers each fact through its entry point: the messenger port for `sends`, its external-system port for `calls`, and `ICommandMediator` for `runs`, sending one command in its own pipeline and commit. A scheduled or one-off job dispatches through the same entry points. Each handler gets its own delivery under `litespec/ext/outbox#deliver-each-event-to-each-handler-separately`, so one failing handler retries alone. An undeclared dispatch fails `effect-undeclared`, whatever its entry point. This provision binds `litespec/backend/application#dispatch-only-declared-effect-edges` for .NET.

**Example:**

```csharp
// The commit step stores the changed aggregate and stages the facts it raised in one transaction.
outbox.StageCollectedEvents();

// Spending/Contracts/IntegrationEvents/ExpenseReimbursed.cs: built from its domain event alone.
public sealed record ExpenseReimbursed(ExpenseId ExpenseId, EmployeeId EmployeeId, Money Amount, DateTimeOffset ReimbursedAt)
{
    public static ExpenseReimbursed From(ExpenseReimbursedEvent fact) =>
        new(fact.ExpenseId, fact.EmployeeId, fact.Amount, fact.ReimbursedAt);
}
```

**Checked by:** `litespec model check` reports `effect-undeclared` for a dispatch no record declares, and `dotnet build` reports `LS2021` for a mediator held outside an endpoint, event handler or job and `LS2022` for an event published outside the shared kernel.

### Run Reactions in Dedicated Handlers

**ID:** `litespec/backend/dotnet/application#run-reactions-in-dedicated-handlers`

**Strength:** required

**Applies to:** artifact: reaction

**Statement:** A reaction MUST run in one class carrying one mark naming its reaction record.

**Rationale:** One class per reaction answers the trigger the record names in `triggeredBy` and dispatches only through its `effects` entries. The class carries the mark `reaction/<module>.<name>` in the declared style, so the stack reader links it without guessing. It lives in the `Application/` folder of the use case it runs, or in the module `Notifications/` or `ExternalSystems/` folder of what it only sends or calls, under `litespec/workspace/naming#place-each-trigger-with-what-it-runs`. It never writes an aggregate directly: a reaction that needs a write sends one command. It honors the record's `reads`, its idempotency, and its `onFailure`. This provision binds `litespec/backend/application#keep-reaction-implementations-explicit` for .NET.

**Example:**

```csharp
// Lending/Notifications/LoanDue/SendDueNoticeOnLoanBorrowedHandler.cs
/// <implements>reaction/lending.send-due-notice-on-loan-borrowed</implements>
internal sealed class SendDueNoticeOnLoanBorrowedHandler(...)
{
    // Answers domain-event/loan.loan-borrowed by sending notification/lending.loan-due.
}
```

**Reviewed for:** Which reactions escape one handler class with their record's mark, send more than one command, or sit outside the folder of what they run? Look at each event handler beside its reaction record.

### Run Scheduled Jobs as Hosted Operations

**ID:** `litespec/backend/dotnet/application#run-scheduled-jobs-as-hosted-operations`

**Strength:** required

**Applies to:** artifact: job

**Statement:** A scheduled or one-off job MUST run as one hosted operation carrying one mark naming its job record.

**Rationale:** The hosted operation lives in the `Application/` folder of the use case it runs, or in the module `Notifications/` or `ExternalSystems/` folder of what it only sends or calls, and registers on the declared execution host. It honors its record's selection, batch size, lease, and cursor, sends its one command once per selected item, and records its evidence where the record states. This provision binds `litespec/backend/application#run-each-job-within-its-declared-selection` for .NET.

**Example:**

```csharp
/// <implements>job/spending.erase-expired-personal-data</implements>
internal sealed class EraseExpiredPersonalDataHost(...)
{
    // Lives in Spending/Expenses/UseCases/ErasePersonalData/Application/.
    // Selects the expired expenses and sends one erase command per expense, resuming from its cursor.
}
```

**Reviewed for:** Which scheduled or one-off jobs escape one marked hosted operation honoring selection, batch size, lease, and cursor? Look at each such job.

### Bind the Pipeline to the Pinned LiteBus APIs

**ID:** `litespec/backend/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An Application MUST compose its command pipeline through the LiteBus APIs this provision names for commit, authorization, audit, fault classification, delivery, event staging, and the timestamp.

**Rationale:** One pinned binding per position keeps every project's pipeline identical. The commit runs through `commands.UseMartenUnitOfWork()`, which registers the LiteBus `IUnitOfWork` at `HandlerPriorities.UnitOfWork`, so the pipeline commits once under `litespec/backend/dotnet/persistence#commit-once-in-the-command-pipeline`. Authorization runs through a guard derived from `LiteBus.Messaging.Abstractions.DeclarationGuard<TMessage, TValue>`, reading the declaration from the message definition under `litespec/backend/application#enforce-target-authorization-in-the-pipeline`. Audit runs through `AuditDeclaration.Audited(...).WithTargetFrom<TCommand>(...).WithReasonFrom<TCommand>(...)` in an `IAuditDefinition<TCommand>`, declaring the target and reason beside the message under `litespec/backend/application#declare-audit-values-beside-the-message`. Fault classification runs through `UseFaultRefusalClassifierInstance(new PostgreSqlConstraintRefusalClassifier(map))`, mapping each store fault to its refusal under `litespec/backend/persistence#map-a-constraint-violation-to-its-refusal`. Delivery runs through `outbox.EnablePerHandlerDelivery()`, reaching each handler separately under `litespec/ext/outbox#deliver-each-event-to-each-handler-separately`. Event staging runs through `outbox.StageCollectedEvents()`, staging the facts `IDomainEventCollector` collected in the same commit as the aggregate change. The timestamp runs through `IExecutionContext.HandledAt`, stamping each message once for the handler to read as `HandledAt` under `litespec/backend/application#take-one-timestamp-per-message`.

**Reviewed for:** Which pipeline positions lack their pinned LiteBus API? Look at the command pipeline composition.

## Conventions

### Use the Use Case Folder Layout

**ID:** `litespec/backend/dotnet/application#use-the-use-case-folder-layout`

**Strength:** default

**Applies to:** artifact: any

**Default:** Place each use case in a folder named for it inside the `UseCases/` folder of its module and aggregate, with the files this provision names in its `Application/` and `WebApi/` folders.

**Replacement:** A project names its use case layout in its Application documentation.

**Rationale:** The folder path matches the specification path, so a reader locates a use case from its use-case identifier without searching. Type names derive from the use-case identifier with the architectural suffix, so `use-case/spending.submit-expense` owns `SubmitExpenseCommand` under `litespec/workspace/naming#use-architectural-suffixes`. Only a use case gets a folder below its `UseCases/` folder: its own models stay in it under `litespec/backend/architecture#give-each-use-case-its-own-models`, any other type sits flat in the layer folder that owns it under `litespec/backend/architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers`, and the event handlers and job that run it sit in its `Application/` folder under `litespec/workspace/naming#place-each-trigger-with-what-it-runs`. Each result type carries one case per result, and each refusal carries the refusal `code` from its result record. A query result type carries one member per declared `include` group, and each result part is named after its result under `litespec/workspace/naming#name-result-parts-after-their-result`.

**Example:** The use case folder layout is:

```text
{Product}.SharedKernel/
  Application/                      product plumbing: pipeline positions, clock, failures, validation
    Pipeline/                       the pipeline own stages, and nothing else
      ApplicationPipeline.cs
      AuthorizationGuard.cs
    Clock/
      IClock.cs
    Failures/
      ResourceNotFoundException.cs
      UseCaseForbiddenException.cs
      UseCaseConflictException.cs
    Validation/
      ValidationErrors.cs
{Product}.Modules/
  Posts/                            single aggregate whose name matches the module: use cases sit in the module UseCases folder
    Application/
      IPostArchive.cs               a port several Posts use cases name
    UseCases/
      CreateDraft/
        Application/
          CreateDraftCommand.cs
          CreateDraftCommandResult.cs
          CreateDraftCommandValidator.cs
          CreateDraftCommandHandler.cs
        WebApi/
          CreateDraftEndpoint.cs
          CreateDraftRequestModel.cs
          CreateDraftResponseModel.cs
      GetPost/
        Application/
          GetPostQuery.cs
          GetPostQueryResult.cs
          GetPostQueryValidator.cs
          GetPostQueryHandler.cs
        WebApi/
          GetPostEndpoint.cs
      ListPosts/
        Application/
          ListPostsQuery.cs
          ListPostsQueryResult.cs
          ListPostsQueryResultItem.cs
          ListPostsQueryValidator.cs
          ListPostsQueryHandler.cs
        WebApi/
          ListPostsEndpoint.cs
      ArchivePost/
        Application/                no WebApi folder: only its reaction runs it
          ArchivePostCommand.cs
          ArchivePostCommandHandler.cs
          ArchivePostOnAuthorBannedHandler.cs   the reaction that runs this use case
    Notifications/
      NewPost/
        NotifySubscribersOnPostPublishedHandler.cs  a reaction that only sends the new-post notification
        IPostPublicationNotifier.cs a port only this notification names
  Customers/                        two aggregates: use cases nest under each aggregate UseCases folder
    CustomerAccounts/
      Application/
        CustomerAccountScopeLookup.cs aggregate-owned, so it sits with the aggregate
      UseCases/
        RestrictAccount/
          Application/
            RestrictAccountCommand.cs
            RestrictAccountCommandHandler.cs
    Consents/
      UseCases/
        GrantConsent/
          Application/
            GrantConsentCommand.cs
            GrantConsentCommandHandler.cs
```

Module and aggregate order, including the single-aggregate flat form, follow `litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case`. A use case file sits in the layer folder its role suffix names under `litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level`.

**Reviewed for:** Which use case folders miss message, result, validator, or handler files named for the use case, hold a file in the wrong layer folder, or which event handlers and jobs escape the folder of what they run? Look at each use case folder.

### Use the Pinned Mediator Entry Points

**ID:** `litespec/backend/dotnet/application#use-the-pinned-mediator-entry-points`

**Strength:** default

**Applies to:** artifact: reaction, endpoint, job

**Default:** Dispatch a command through `ICommandMediator.SendAsync` and a query through `IQueryMediator.QueryAsync`, from the LiteBus version the manifest pins, only from an endpoint, an event handler or a job.

**Replacement:** A project names its dispatch entry points in its Application documentation.

**Rationale:** One pinned library means every dispatch site in every project reads the same way, and an agent finds all callers of a message with one search. A project that replaces the default still keeps commands and queries on separate entry points, because that separation is required by `litespec/backend/application#dispatch-commands-and-queries-through-separate-entry-points` and no replacement removes it.

**Reviewed for:** Which dispatch sites bypass `ICommandMediator.SendAsync` or `IQueryMediator.QueryAsync`, or use an unpinned LiteBus version? Look at each dispatch site.

## Reference Example

This informative example demonstrates `litespec/backend/dotnet/architecture#keep-implementation-types-internal`, `litespec/backend/application#keep-command-handlers-to-load-call-and-stage`, `litespec/backend/dotnet/application#dispatch-declared-effects-through-their-entry-points`, and `litespec/workspace/naming#place-each-trigger-with-what-it-runs`.

```csharp
public sealed record CreateDraftCommand(
    PostId PostId,
    AuthorId AuthorId,
    string Title,
    DateTimeOffset HandledAt) : ICommand<CreateDraftCommandResult>;

public sealed record CreateDraftCommandResult(PostId PostId);

internal sealed class CreateDraftCommandHandler(
    IPostRepository postRepository) : ICommandHandler<CreateDraftCommand, CreateDraftCommandResult>
{
    public async Task<CreateDraftCommandResult> HandleAsync(
        CreateDraftCommand command,
        CancellationToken cancellationToken)
    {
        var post = Post.CreateDraft(
            command.PostId,
            command.AuthorId,
            PostTitle.Create(command.Title),
            command.HandledAt);

        postRepository.Store(post);

        return new CreateDraftCommandResult(post.Id);
    }
}
```

The pinned LiteBus package signatures take precedence over illustrative example signatures.

### Change Another Aggregate from an Event Handler

```csharp
// Lending/Loans/UseCases/BorrowBook/Application/BorrowBookCommandHandler.cs
internal sealed class BorrowBookCommandHandler(
    ILoanRepository loanRepository)
    : ICommandHandler<BorrowBookCommand, BorrowBookCommandResult>
{
    public async Task<BorrowBookCommandResult> HandleAsync(
        BorrowBookCommand command,
        CancellationToken cancellationToken)
    {
        var loan = Loan.Borrow(
            command.LoanId,
            command.BookId,
            command.ReaderId,
            command.HandledAt);

        loanRepository.Store(loan);

        return new BorrowBookCommandResult(loan.Id);
    }
}
```

```csharp
// Lending/Books/UseCases/MarkBookLent/Application/MarkBookLentOnLoanBorrowedHandler.cs
/// <implements>reaction/lending.mark-book-lent-on-loan-borrowed</implements>
internal sealed class MarkBookLentOnLoanBorrowedHandler(ICommandMediator commandMediator)
    : IEventHandler<LoanBorrowedEvent>
{
    public Task HandleAsync(
        LoanBorrowedEvent @event,
        CancellationToken cancellationToken) =>
        commandMediator.SendAsync(
            new MarkBookLentCommand(@event.BookId, @event.LoanId),
            cancellationToken);
}
```

The command handler stages only the loan; borrowing raises `LoanBorrowedEvent`, and after the commit the event handler sends the one command of the mark-book-lent use case, whose own handler loads the book, records the lent state and stages it in its own pipeline and commit. The event handler sits in that use case's `Application/` folder and writes no aggregate itself.
