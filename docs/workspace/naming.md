# Naming and Code Style

**Stack:** any
**Concern:** workspace

This page defines how code gets named in a LiteSpec codebase: a fixed suffix per architectural role, the aggregate root name at the start of each type an aggregate owns, and one casing map for frontend files. A name states the business intent and architectural role without opening the file. The rules apply to anyone who names code, and especially to agents working across sessions.

## Concepts

### Anchored Names

An aggregate root is the single type through which outside code changes an aggregate. Each type the aggregate owns starts its qualified name with the full name of the root, so the name reveals its owner without folder context. `PostPublishedDomainEvent` and `PostState.Published` both lead with `Post`, while a namespace-level `PublishedState` leaves the owner out.

A type nested inside a declaring type inherits the prefix of that type through its qualified name. `PostState.Published` satisfies the shape, while a flat `PostStatePublishedState` repeats the anchor twice. The shape covers events, states, child entities, values, and discriminated unions.

### Role Suffixes

Each architectural role carries a fixed suffix: a command handler ends in `CommandHandler`, a result part carries its result full name, and an endpoint ends in `Endpoint`. The Role Suffixes table lists each role with its pattern and one example:

| Role | Pattern | Example |
|:---|:---|:---|
| Command message | `{UseCase}Command` | `CreateDraftCommand` |
| Command result | `{UseCase}CommandResult` | `CreateDraftCommandResult` |
| Command handler | `{UseCase}CommandHandler` | `CreateDraftCommandHandler` |
| Command validator | `{UseCase}CommandValidator` | `CreateDraftCommandValidator` |
| Query message | `{UseCase}Query` | `GetPostQuery` |
| Query result | `{UseCase}QueryResult` | `GetPostQueryResult` |
| Query result part | `{UseCase}QueryResult{Part}` | `ListPostsQueryResultItem` |
| Command result part | `{UseCase}CommandResult{Part}` | `SubmitExpenseCommandResultLine` |
| Query handler | `{UseCase}QueryHandler` | `GetPostQueryHandler` |
| Query validator | `{UseCase}QueryValidator` | `GetPostQueryValidator` |
| Aggregate state base | `{Aggregate}State` | `PostState` |
| Aggregate state case, flat | `{Aggregate}{State}State` | `PostPublishedState` |
| Aggregate state case, nested | `{Aggregate}State.{State}` | `PostState.Published` |
| Domain event | `{Aggregate}{PastFact}DomainEvent` | `PostPublishedDomainEvent` |
| Module event | `{Fact}ModuleEvent` | `ExpenseReimbursedModuleEvent` |
| Integration event | `{Fact}IntegrationEvent` | `ExpenseSubmittedIntegrationEvent` |
| Event handler | `{Action}On{Fact}Handler` | `NotifySubscribersOnPostPublishedHandler` |
| Read model projection | `{ReadModel}On{Fact}Projection` | `ReimbursedExpenseOnExpenseReimbursedProjection` |
| Repository implementation | `{Aggregate}Repository` | `PostRepository` |
| Endpoint | `{UseCase}Endpoint` | `CreateDraftEndpoint` |
| HTTP request model | `{UseCase}RequestModel` | `CreateDraftRequestModel` |
| HTTP response model | `{UseCase}ResponseModel` | `CreateDraftResponseModel` |
| HTTP response item model | `{UseCase}ResponseItemModel` | `ListPostsResponseItemModel` |
| HTTP pagination model | `PaginationModel` | `PaginationModel` |
| Polymorphic transport model base | `{Concept}Model` | `FulfillmentOutcomeModel` |
| Polymorphic transport model case | `{Case}{Concept}Model` | `ShippedFulfillmentOutcomeModel` |
| Persistence configuration | `{PersistedType}Configuration` | `PostConfiguration` |
| Registration class | `{Layer}ServiceRegistration` | `InfrastructureServiceRegistration` |

Full suffixes keep coordination apart from projection, and boundary data apart from business objects. The top folder states the layer of the file, and inside a layer folder the suffix states its role: a type whose name ends in `Endpoint`, `RequestModel`, or `ResponseModel` is a WebApi file, and one ending in `Command`, `Query`, `Result`, `Handler`, or `Validator` is an Application file.

### Exceptions, Booleans, and Language Shape

An exception name states the rule that failed on which type. The Domain form puts both in the name: `{DomainType}{Reason}Exception`, with the `DomainType` as the aggregate root or an anchored owned type, so the name starts with the aggregate root.

A boolean answers a yes-or-no question, so its name asks one. The prefixes `Is`, `Has`, `Can`, or a precise verb make the question explicit: `HasLines` and `CanPublish` state a condition. A name fixed by an outside contract or an overridden member keeps its fixed wording.

Production code targets the pinned language version and uses its current construct over an older equivalent. In the dotnet stack that means collection expressions and pattern matching, which express the same intent with less incidental code. A generic name invites unrelated responsibility, because nothing in it excludes the next addition.

The listed names stay out of class names where a narrower name exists: `Manager`, `Helper`, `Processor`, `Common`, `Utility`, `BaseService`, `DataService`, `MessageBus`, `Engine`, `Facade`, `Wrapper`, and `Factory`. `Factory` stays exact on a named static aggregate factory method, so the default covers the class and not the method.

### Frontend and Boundary Names

Each file gets one predictable name: kebab-case folders, PascalCase components with matching exports, and `use-{name}` hooks. One mapping lets you move between the documentation tree and the source tree without translation, and lets an agent derive the file from the feature name.

One business name per module spans Domain, Application, endpoints, features, and documentation. The Posts module maps to `Shop/Posts`, `features/posts`, and `.litespec/behavior/modules/posts` without translation. A second name in any layer reads as a second concept that exists nowhere.

A namespace starts with the product name and follows the folders beneath the project root. `src` and `apps` stay out, since they couple the type name to a workspace layout decision. The project root is the directory holding the project file, so each folder above it stays outside the namespace.

Route segments and JSON field names derive from the current aggregate or ubiquitous term, the shared Domain word for the concept. Renaming a Domain concept then renames its boundary names with it, so the contract and the language stay aligned. A project with client-facing names that predate its glossary records its own boundary naming in its project documentation.

## Standards

### Use Architectural Suffixes

**ID:** `litespec/workspace/naming#use-architectural-suffixes`

**Strength:** required

**Applies to:** artifact: state-record, domain-event, use-case, command-handler, query-handler, validator, reaction, repository, endpoint, job

**Statement:** An architectural type MUST carry the suffix its role assigns in the Role Suffixes table.

**Rationale:** A short name hides the role: `{UseCase}Handler` says nothing about coordination versus projection, and `{UseCase}Request` says nothing about passive boundary data. Full suffixes put the role in the name, so you sort types without opening files.

**Example:** An event handler names its act and the business fact without the kind suffix. `NotifySubscribersOnPostPublishedHandler` answers `PostPublishedDomainEvent` and lives in the folder of the use case it runs. The name carries no technical suffix for the fact, so `NotifySubscribersOnPostPublishedDomainEventHandler` is wrong, while the event type keeps its kind suffix.

Application types keep their full suffix. The short forms `{UseCase}Result`, `{UseCase}Handler`, and `{UseCase}Validator` drop the command-or-query distinction the full name carries. A result part carries the full name of its result, not a bare summary name like `PostSummary`.

HTTP transport types keep their full suffix too. The short forms `{UseCase}Request` and `{UseCase}Response` drop the boundary marking. The `Model` suffix marks the type as passive boundary data instead of an operation or rich business object.

A polymorphic transport model mirrors a Domain discriminated union. Its abstract base names the concept and each sealed case names its case, with both ending in `Model`: `FulfillmentOutcomeModel` and `ShippedFulfillmentOutcomeModel`. The transport name drops the aggregate prefix of the Domain union, such as `OrderFulfillmentOutcome`, while its discriminator string keeps the stable case code of the Domain union unchanged.

**Reviewed for:** Does each type name and each frontend file name carry the suffix its role assigns in the Role Suffixes table? Read the names beside the table.

### Name Result Parts After Their Result

**ID:** `litespec/workspace/naming#name-result-parts-after-their-result`

**Strength:** required

**Applies to:** artifact: use-case

**Statement:** Each type nested in a use-case result MUST be named after the result it belongs to, as `{UseCase}QueryResult{Part}` or `{UseCase}CommandResult{Part}`.

**Rationale:** A result part named for its own shape instead of its result collides with each other use case shaping the same thing: two summaries named `PostSummary` answer different queries with different fields. Leading with the full result name keeps each part beside the result it belongs to in each listing, and keeps command coordination apart from query projection without opening the file.

**Example:** `ListPostsQueryResultItem` belongs to `ListPostsQueryResult`, and `ReadLoanQueryResultHold` belongs to `ReadLoanQueryResult`. Both read beside their results in each listing.

**Reviewed for:** Does each type nested in a use-case result carry the full result name as its prefix? Read the result types.

### Place Each Trigger with What It Runs

**ID:** `litespec/workspace/naming#place-each-trigger-with-what-it-runs`

**Strength:** required

**Applies to:** artifact: reaction, job

**Statement:** Each event handler and job class MUST live in the `Application/` folder of the use case its record runs, or, when it runs none, in the `Application/Notifications/` folder named for the notification it sends or the `Application/ExternalSystems/` folder named for the system it calls, with an event handler named `{Action}On{Fact}Handler`.

**Rationale:** A reaction and a job are triggers, like an endpoint, and a trigger belongs to what it triggers. Placed with its use case, the handler names the command of that use case from inside the folder of the use case, so no command leaves its folder for a caller to reach it, and the folder shows each way the use case starts. A folder named for a fact gathers the commands of several use cases under one name instead, and those reactions span modules, so the question of what happens when a fact gets recorded gets answered by the model query on the event and by the handler names, which carry the fact.

**Example:** `Lending/Application/Holds/ExpireHold/ExpireHoldOnLoanReturnedHandler` runs the expire-hold use case when a loan gets returned, and `Lending/Application/Notifications/LoanDue/SendDueNoticeOnLoanBorrowedHandler` only sends the due notice. The first lives in the folder of the use case it runs; the second lives in the folder named for the notification it sends.

**Checked by:** `litespec model check` reports `code-placement-drift` (level S) for an event handler or job class outside the folder its record's effects name.

### Name Repository Variables After Their Interface

**ID:** `litespec/workspace/naming#name-repository-variables-after-their-interface`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler, validator, reaction, repository, job

**Statement:** Each repository parameter, field or local MUST be named after its interface without the `I` prefix, ending in `Repository`.

**Rationale:** A short plural name hides which aggregate the variable guards. `orders` reads as a list, a query, or the repository, and each reader guesses. Naming the variable after its interface keeps one name per repository at each use site, so you read the dependency from the name alone.

**Example:** A parameter of type `IExpenseRepository` is named `expenseRepository`, and a field of type `ILoanRepository` is named `loanRepository`. Each use site then names the same dependency the same way.

**Reviewed for:** Does each repository parameter, field, or local carry the name of its interface without the `I` prefix? Read the repository use sites.

### Reveal Asynchrony and Cancellation in Operation Names

**ID:** `litespec/workspace/naming#reveal-asynchrony-and-cancellation-in-operation-names`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler, validator, reaction, port, repository, endpoint, migration, job

**Statement:** An asynchronous operation MUST reveal its asynchrony in its name and accept cancellation explicitly when the operation is cancelable.

**Rationale:** A caller decides from the signature whether to await and what to cancel, so both answers belong in the signature: the name reveals the asynchrony and a cancelable operation accepts cancellation explicitly. An operation is cancelable when it performs I/O or awaits, and anything else has nothing to cancel.

**Reviewed for:** Does each asynchronous operation name reveal asynchrony, with each cancelable operation signature taking cancellation explicitly? Read the public signatures in `apps/api/src`.

### Avoid Primary Constructors on Classes and Structs

**ID:** `litespec/workspace/naming#avoid-primary-constructors-on-classes-and-structs`

**Strength:** advisory

**Applies to:** artifact: aggregate, state-record, value-object, domain-event, use-case, command-handler, query-handler, validator, reaction, port, repository, endpoint, migration, job

**Statement:** A class or struct SHOULD NOT declare a primary constructor.

**Deviation:** A positional record keeps its positional parameters, which define immutable data instead of captured state.

**Rationale:** A primary constructor parameter is captured, mutable, and visible in the whole class body. Nothing marks it read-only, so a method reassigns it and each later call sees the new value. Inside the body, the parameter name reads the same as a local, so you lose the cue that something is class state.

Copying a parameter into a property while using the parameter elsewhere stores it twice. Validation, guard clauses, and derived fields have no home: they need either field initializers with side effects or a second constructor that chains to the primary one. Documentation for the parameters moves to the type, where tools show it less well than on a constructor.

A type with several dependencies puts them all on the type line, which wraps badly and hides the type name and role. Positional records look the same but behave differently, with generated read-only properties, which misleads readers who know records. An explicit constructor with private read-only fields avoids each one of these. Settled by decision/0106-state-class-design-rules.

**Example:**

```csharp
internal sealed class ApproveExpenseCommandHandler : ICommandHandler<ApproveExpenseCommand, ApproveExpenseCommandResult>
{
    private readonly IExpenseRepository _expenseRepository;

    public ApproveExpenseCommandHandler(IExpenseRepository expenseRepository)
    {
        _expenseRepository = expenseRepository;
    }
}
```

Positional records keep their positional form.

**Reviewed for:** Does any class or struct declare a primary constructor? Look at the type declarations in `apps/api/src`.

## Conventions

None.
