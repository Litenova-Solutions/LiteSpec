# .NET Application Layer

## Intent

Application code in a .NET backend sends commands and queries through mediators from LiteBus, the pinned messaging library: commands change business state, queries read without changing it, and validators report structural input problems as LiteBus validity values. This page states the .NET form of that dispatch: the command-handler shape, the validity shape, the query-handler shape, the entry point for each declared effect, the operation layout, the pinned mediator methods, and the pinned pipeline bindings. The shared coordination rules live on [Application Layer](../application.md); this page holds only their .NET form.

## Concepts

### Commands, Queries, and Mediators

A command is a message that changes business state; a query reads a read model without changing state. The boundary sends the message through a LiteBus mediator, and the command pipeline around the handler runs validation, authorization, the handler itself, and then one commit. Commands travel through `ICommandMediator` and queries through `IQueryMediator`, so the write path and the read path stay visibly separate at every call site.

A reaction answers a domain event or an integration event in one class carrying one mark naming its reaction record. A scheduled or manual job runs its declared commands once per item in its selection.

### Validity Findings

A validator checks structural input, such as an empty title or a malformed identifier, before Domain runs. Each finding names the input member, a stable reason code, and a caller-safe message. The pipeline runs every validator for the message and merges the findings, so the caller learns about all input problems in one answer instead of fixing them one round trip at a time.

## Standards

### Keep Command Handlers to Load, Call and Stage

**ID:** `litespec/backend/dotnet/application#keep-command-handlers-to-load-call-and-stage`

**Statement:** A command handler MUST load or create one aggregate through its repository, call one domain method on it, stage it, and return the result.

**Rationale:** The handler body has three steps: the load or creation, the one domain call, and the stage. A required load uses the throwing `GetByIdAsync` and an optional lookup uses the nullable `FindBy...Async` under `litespec/backend/dotnet/domain#keep-repository-interfaces-in-domain`. The message carries the pipeline-stamped timestamp as `HandledAt` under `litespec/backend/application#take-one-timestamp-per-message` (see `litespec/backend/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis`), so the handler stamps decisions without reading a clock. Validation, authorization, idempotency, audit, holds, and the commit are pipeline stages; messages, provider calls, and writes to other aggregates run in event handlers after the commit under `litespec/backend/dotnet/application#dispatch-declared-effects-through-their-entry-points`. A use case whose record declares `consistency: atomic` over several aggregates is the one exception: the handler loads, calls, and stages each aggregate its `changes` names, one method each. This provision states the .NET form of `litespec/backend/application#keep-command-handlers-to-load-call-and-stage`.

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

**Reviewed for:** Which command handlers do more than load or create one aggregate, call one domain method, stage it, and return? Look at each command handler body; `dotnet build` reports find-or-create as LS2014 and looped stores as LS2015.

### Report Input Problems as Validity

**ID:** `litespec/backend/dotnet/application#report-input-problems-as-validity`

**Statement:** A validator MUST report its findings as Validity, with each ValidationFailure carrying a member, a code, and a message, and no HTTP status.

**Rationale:** Every validator for the message runs, and the pipeline merges the findings before it refuses the request, so the caller fixes the whole form at once. The member tells the caller which input failed, the stable code lets a client branch on the reason, and the message explains the problem in words safe to show. Validity values come from the pinned mediator's abstractions, named in `litespec/backend/dotnet/application#use-the-pinned-mediator-entry-points`, so every project reports the same shape. This provision states the .NET form of `litespec/backend/application#report-input-problems-as-validity-findings`.

**Reviewed for:** Which validators report outside `Validity`, or skip member, code, or message, or leak HTTP status? Look at each validator.

### Keep Query Handlers Free of Writes

**ID:** `litespec/backend/dotnet/application#keep-query-handlers-free-of-writes`

**Statement:** A query handler MUST take no repository and stage, store, delete or dispatch nothing.

**Rationale:** A read path acts on no invariant, so a repository in a query handler loads an aggregate the query has no write to justify. The handler projects through the read session under `litespec/backend/dotnet/persistence#query-through-iquerysession`. A read that writes makes retries and caching unsafe; an audited read is the pipeline's work, not the handler's. `dotnet build` reports a repository in a query handler as `LS2011`. This provision states the .NET form of `litespec/backend/application#keep-query-handlers-free-of-writes` and of the repository rule in `litespec/backend/application#project-queries-directly`.

**Example:** The list-expenses query projects its rows through `IQuerySession`, taking no repository and staging nothing.

**Reviewed for:** Which query handlers take a repository, skip `IQuerySession`, or stage, store, delete, or dispatch? Look at each query handler; `dotnet build` reports a repository in a query handler as LS2011.

### Dispatch Declared Effects Through Their Entry Points

**ID:** `litespec/backend/dotnet/application#dispatch-declared-effects-through-their-entry-points`

**Statement:** An event handler MUST dispatch each declared `sends` effect through the messenger port, each declared `calls` effect through its external-system port, and each declared `runs` effect through `ICommandMediator`, after the commit that stored the fact it answers, while the aggregate raises each declared `emits` effect as a domain event the outbox stages in the same commit.

**Rationale:** A command handler changes its aggregate and returns; it dispatches nothing. The aggregate raises its facts, and the outbox stages them in the same commit through `outbox.StageCollectedEvents()` (see `litespec/backend/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis`). After that commit, an event handler answers each fact through its entry point: the messenger port for `sends`, its external-system port for `calls`, and `ICommandMediator` for `runs`. An `inline` `runs` entry states whether it shares the caller's transaction. A scheduled or manual job dispatches through the same entry points. Each handler gets its own delivery under `litespec/ext/outbox#deliver-each-event-to-each-handler-separately`, so one failing handler retries alone. An undeclared dispatch fails `effect-undeclared`, whatever its entry point. This provision states the .NET form of `litespec/backend/application#dispatch-only-declared-effect-edges`.

**Example:**

```csharp
// The commit step stores the changed aggregate and stages the facts it raised in one transaction.
outbox.StageCollectedEvents();
```

**Checked by:** `litespec model check` reports `effect-undeclared` for a dispatch no record declares; each event handler dispatches sends through the messenger port, calls through its external-system port, and runs through `ICommandMediator`, after the commit.

### Run Reactions in Dedicated Handlers

**ID:** `litespec/backend/dotnet/application#run-reactions-in-dedicated-handlers`

**Statement:** A reaction MUST run in one class carrying one mark naming its reaction record.

**Rationale:** One class per reaction answers the trigger the record names in `triggeredBy` and dispatches only through its `effects` entries. The class carries the mark `reaction/<module>.<name>` in the declared style, so the stack reader links it without guessing. It lives in the `On{Fact}` folder under its subscriber aggregate folder under `litespec/workspace/naming#group-event-handlers-by-the-fact-they-handle`, beside every other handler of the same fact. It never writes an aggregate directly: a reaction that needs a write runs a command. It honors the record's `reads`, its idempotency, and its `onFailure`. This provision states the .NET form of `litespec/backend/application#keep-reaction-implementations-explicit`.

**Example:**

```csharp
// Loans/Loans/OnLoanBorrowed/SendDueNoticeOnLoanBorrowedHandler.cs
/// <implements>reaction/lending.send-due-notice-on-loan-borrowed</implements>
internal sealed class SendDueNoticeOnLoanBorrowedHandler(...)
{
    // Answers event/loan.loan-borrowed by sending notification/lending.loan-due.
}
```

**Reviewed for:** Which reactions escape one handler class with their record's mark in the `On{Fact}` folder? Look at each reaction in Application.

### Run Scheduled Jobs as Hosted Operations

**ID:** `litespec/backend/dotnet/application#run-scheduled-jobs-as-hosted-operations`

**Statement:** A scheduled or manual job MUST run as one hosted operation carrying one mark naming its job record.

**Rationale:** The hosted operation lives in Infrastructure with the scheduling concerns and registers on the declared execution host. It honors its record's selection, batch size, lease, and cursor, runs its commands once per item, and records its evidence where the record states. A `manual` job states its `runsWhere` and its guard and touches no aggregate. This provision states the .NET form of `litespec/backend/application#run-each-job-within-its-declared-selection`.

**Example:**

```csharp
/// <implements>job/spending.erase-expired-personal-data</implements>
internal sealed class EraseExpiredPersonalDataHost(...)
{
    // Runs one erase command per expired expense, resuming from its cursor.
}
```

**Reviewed for:** Which scheduled or manual jobs escape one marked hosted operation honoring selection, batch size, lease, and cursor? Look at each such job.

### Bind the Pipeline to the Pinned LiteBus APIs

**ID:** `litespec/backend/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis`

**Statement:** An Application MUST compose its command pipeline through the LiteBus APIs this provision names for commit, authorization, audit, fault classification, delivery, event staging, and the timestamp.

**Rationale:** One pinned binding per position keeps every project's pipeline identical. The commit runs through `commands.UseMartenUnitOfWork()`, which registers the LiteBus `IUnitOfWork` at `HandlerPriorities.UnitOfWork`, so the pipeline commits once under `litespec/backend/dotnet/persistence#commit-once-in-the-command-pipeline`. Authorization runs through a guard derived from `LiteBus.Messaging.Abstractions.DeclarationGuard<TMessage, TValue>`, reading the declaration from the message definition under `litespec/backend/application#enforce-target-authorization-in-the-pipeline`. Audit runs through `AuditDeclaration.Audited(...).WithTargetFrom<TCommand>(...).WithReasonFrom<TCommand>(...)` in an `IAuditDefinition<TCommand>`, declaring the target and reason beside the message under `litespec/backend/application#declare-audit-values-beside-the-message`. Fault classification runs through `UseFaultRefusalClassifierInstance(new PostgreSqlConstraintRefusalClassifier(map))`, mapping each store fault to its refusal under `litespec/backend/persistence#map-a-constraint-violation-to-its-refusal`. Delivery runs through `outbox.EnablePerHandlerDelivery()`, reaching each handler separately under `litespec/ext/outbox#deliver-each-event-to-each-handler-separately`. Event staging runs through `outbox.StageCollectedEvents()`, staging the facts `IDomainEventCollector` collected in the same commit as the aggregate change. The timestamp runs through `IExecutionContext.HandledAt`, stamping each message once for the handler to read as `HandledAt` under `litespec/backend/application#take-one-timestamp-per-message`.

**Reviewed for:** Which pipeline positions lack their pinned LiteBus API? Look at the command pipeline composition.

## Conventions

### Use the Operation Folder Layout

**ID:** `litespec/backend/dotnet/application#use-the-operation-folder-layout`

**Default:** Place each operation in a folder named for its use case under its module and aggregate, with the files this provision names.

**Replacement:** A project names its operation layout in its Application documentation.

**Rationale:** The folder path matches the specification path, so a reader locates an operation from its use-case identifier without searching. Type names derive from the use-case identifier with the architectural suffix, so `use-case/spending.submit-expense` owns `SubmitExpenseCommand` under `litespec/workspace/naming#use-architectural-suffixes`. Only a use case or a handled fact gets a folder: any other type sits flat in the folder that owns it under `litespec/backend/architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers`, and a handled fact's folder takes the `On{Fact}` name with its marked handler classes under `litespec/workspace/naming#group-event-handlers-by-the-fact-they-handle`. Each result type carries one case per result, and each refusal carries the refusal `code` from its result record. A query result type carries one member per declared `include` group, and each result part is named after its result under `litespec/workspace/naming#name-result-parts-after-their-result`.

**Example:** The operation layout is:

```text
{ProjectName}.Application.Abstractions/     contracts more than one module names
  Clock/
    IClock.cs
  Failures/
    ResourceNotFoundException.cs
    UseCaseForbiddenException.cs
    UseCaseConflictException.cs
  Validation/
    ValidationErrors.cs
{ProjectName}.Application/
  Pipeline/                         the pipeline's own stages, and nothing else
    ApplicationPipeline.cs
    AuthorizationGuard.cs
  Posts/                            single aggregate whose name matches the module: operations sit directly under the module
    IPostArchive.cs                 a port several Posts use cases name
    CreateDraft/
      CreateDraftCommand.cs
      CreateDraftCommandResult.cs
      CreateDraftCommandValidator.cs
      CreateDraftCommandHandler.cs
    GetPost/
      GetPostQuery.cs
      GetPostQueryResult.cs
      GetPostQueryValidator.cs
      GetPostQueryHandler.cs
    ListPosts/
      ListPostsQuery.cs
      ListPostsQueryResult.cs
      ListPostsQueryResultItem.cs
      ListPostsQueryValidator.cs
      ListPostsQueryHandler.cs
    OnPostPublished/   event handlers of one fact: one marked class per reaction
      NotifySubscribersOnPostPublishedHandler.cs
      IPostPublicationNotifier.cs   a port one event handler names
  Customers/                        two aggregates: operations nest under each aggregate
    CustomerAccounts/
      CustomerAccountScopeLookup.cs aggregate-owned, so it sits with the aggregate
      RestrictAccount/
        RestrictAccountCommand.cs
        RestrictAccountCommandHandler.cs
    Consents/
      GrantConsent/
        GrantConsentCommand.cs
        GrantConsentCommandHandler.cs
```

Module and aggregate order, including the single-aggregate flat form, follow `litespec/backend/architecture#organize-every-layer-by-module-and-use-case`.

**Reviewed for:** Which operation folders miss message, result, validator, or handler files named for the use case, or which event handlers escape their `On{Fact}` folder? Look at each operation folder.

### Use the Pinned Mediator Entry Points

**ID:** `litespec/backend/dotnet/application#use-the-pinned-mediator-entry-points`

**Default:** Dispatch a command through `ICommandMediator.SendAsync` and a query through `IQueryMediator.QueryAsync`, from the LiteBus version the manifest pins.

**Replacement:** A project names its dispatch entry points in its Application documentation.

**Rationale:** One pinned library means every dispatch site in every project reads the same way, and an agent finds all callers of a message with one search. A project that replaces the default still keeps commands and queries on separate entry points, because that separation is required by `litespec/backend/application#dispatch-commands-and-queries-through-separate-entry-points` and no replacement removes it.

**Reviewed for:** Which dispatch sites bypass `ICommandMediator.SendAsync` or `IQueryMediator.QueryAsync`, or use an unpinned LiteBus version? Look at each dispatch site.

## Reference Example

This informative example demonstrates `litespec/backend/dotnet/architecture#keep-implementation-types-internal`, `litespec/backend/application#keep-command-handlers-to-load-call-and-stage`, `litespec/backend/dotnet/application#dispatch-declared-effects-through-their-entry-points`, and `litespec/workspace/naming#group-event-handlers-by-the-fact-they-handle`.

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
// Loans/Loans/BorrowBook/BorrowBookCommandHandler.cs
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
// Loans/Books/OnLoanBorrowed/MarkBookLentOnLoanBorrowedHandler.cs
/// <implements>reaction/lending.mark-book-lent-on-loan-borrowed</implements>
internal sealed class MarkBookLentOnLoanBorrowedHandler(
    IBookRepository bookRepository)
    : IEventHandler<LoanBorrowedEvent>
{
    public async Task HandleAsync(
        LoanBorrowedEvent @event,
        CancellationToken cancellationToken)
    {
        var book = await bookRepository.GetByIdAsync(@event.BookId, cancellationToken);

        book.MarkLent(@event.LoanId, @event.BorrowedAt);

        bookRepository.Store(book);
    }
}
```

The command handler stages only the loan; borrowing raises `LoanBorrowedEvent`, and the event handler records the lent state on the book after the commit. It commits nothing: the post-handler commits once, and an event handler that needs a write runs a command through `ICommandMediator`.
