# Naming and Code Style

**Stack:** any
**Concern:** workspace

This page defines how code is named in a LiteSpec codebase: a fixed suffix per architectural role, the aggregate root name at the start of every type an aggregate owns, and one casing map for frontend files, with the exception-naming, boolean, async, and language-version rules beside them. A name states the business intent and architectural role without opening the file. The rules apply to anyone who names code, and especially to agents working across sessions.

## Concepts

### Anchored Names

An aggregate root is the single type through which outside code changes an aggregate. Every type the aggregate owns starts its qualified name with the full name of the root, so the name reveals its owner without folder context. `PostPublishedEvent` and `PostState.Published` both lead with `Post`, while a namespace-level `PublishedState` does not.

### Role Suffixes

Each architectural role carries a fixed suffix: a command handler ends in `CommandHandler`, a result part carries its result's full name, and an endpoint ends in `Endpoint`. The Role Suffixes table lists every role with its pattern and one example:

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
| Domain event | `{Aggregate}{PastFact}Event` | `PostPublishedEvent` |
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

Full suffixes keep coordination apart from projection, and boundary data apart from business objects. Inside a use case folder the suffix also states the layer of the file: a type whose name ends in `Endpoint`, `RequestModel`, or `ResponseModel` is WebApi, and one ending in `Command`, `Query`, `Result`, `Handler`, or `Validator` is Application.

## Standards

### Anchor Aggregate-Owned Types on the Aggregate Root

**ID:** `litespec/workspace/naming#anchor-aggregate-owned-types-on-the-aggregate-root`

**Strength:** required

**Applies to:** `artifact: aggregate`

**Statement:** An aggregate-owned type's qualified name MUST start with its aggregate root's full name.

**Rationale:** Leading with the root name reveals the owner without folder context. The rule covers events, states, child entities, values, and discriminated unions. A type nested inside a declaring type inherits the prefix of that type through its qualified name, so `PostState.Published` satisfies the rule, while a flat `PostStatePublishedState` repeats the anchor twice.

**Example:** `PostPublishedEvent` and `PostState.Published` both lead with `Post`. `PublishedState` declared at namespace level does not.

**Reviewed for:** Does each aggregate-owned type name in `apps/api/src` begin with its aggregate root name, through nesting or prefix? Read the type names beside their aggregates.

### Use Architectural Suffixes

**ID:** `litespec/workspace/naming#use-architectural-suffixes`

**Strength:** required

**Applies to:** artifact: state-record, domain-event, use-case, command-handler, query-handler, validator, reaction, repository, endpoint, job

**Statement:** An architectural type MUST carry the suffix its role assigns in the Role Suffixes table.

**Rationale:** A short name hides the role: `{UseCase}Handler` never says whether it coordinates a command or projects a query, and `{UseCase}Request` never says it is passive boundary data. Full suffixes put the role in the name, so a reader sorts types without opening files.

**Example:**

An event handler names its act and the business fact without the `Event` suffix. `NotifySubscribersOnPostPublishedHandler` answers `PostPublishedEvent` and lives in the folder of the use case it runs (`litespec/workspace/naming#place-each-trigger-with-what-it-runs`). The name carries no technical suffix for the fact, so `NotifySubscribersOnPostPublishedEventHandler` is wrong, while the event type keeps its `Event` suffix (`litespec/workspace/naming#anchor-aggregate-owned-types-on-the-aggregate-root`).

Application types keep their full suffix. The short forms `{UseCase}Result`, `{UseCase}Handler`, and `{UseCase}Validator` drop the command-or-query distinction the full name carries. A result part carries the full name of its result, as `litespec/workspace/naming#name-result-parts-after-their-result` states, not a bare summary name like `PostSummary`. The full role suffix distinguishes command coordination from query projection without opening the file.

HTTP transport types keep their full suffix too. The short forms `{UseCase}Request` and `{UseCase}Response` drop the boundary marking. The `Model` suffix marks the type as passive boundary data rather than an operation or rich business object. Other project-owned, passive HTTP DTOs also name their concrete role and end in `Model`, such as `ListPostsResponseItemModel` or `PaginationModel`.

A polymorphic transport model mirrors a Domain discriminated union (`litespec/backend/api#mirror-each-closed-set-on-the-wire`). Its abstract base names the concept and each sealed case names its case, with both ending in `Model`: `FulfillmentOutcomeModel` and `ShippedFulfillmentOutcomeModel`.

The transport name drops the aggregate prefix of the Domain union, such as `OrderFulfillmentOutcome`, while its discriminator string keeps the stable case code of the Domain union unchanged.

**Reviewed for:** Does each type name and each frontend file name carry the suffix its role assigns in the Role Suffixes table? Read the names beside the table.

### Name Result Parts After Their Result

**ID:** `litespec/workspace/naming#name-result-parts-after-their-result`

**Strength:** required

**Applies to:** artifact: use-case

**Statement:** Each type nested in a use-case result MUST be named after the result it belongs to, as `{UseCase}QueryResult{Part}` or `{UseCase}CommandResult{Part}`.

**Rationale:** A result part named for its own shape instead of its result collides with every other use case shaping the same thing: two summaries named `PostSummary` answer different queries with different fields. Leading with the full result name keeps each part beside the result it belongs to in every listing, and keeps command coordination apart from query projection without opening the file.

**Example:** `ListPostsQueryResultItem` belongs to `ListPostsQueryResult`, and `ReadLoanQueryResultHold` belongs to `ReadLoanQueryResult`.

**Reviewed for:** Does each type nested in a use-case result carry the full result name as its prefix? Read the result types.

### Place Each Trigger with What It Runs

**ID:** `litespec/workspace/naming#place-each-trigger-with-what-it-runs`

**Strength:** required

**Applies to:** artifact: reaction, job

**Statement:** Each event handler and job class MUST live in the `Application/` folder of the use case its record runs, or, when it runs none, in the module `Notifications/` folder named for the notification it sends or the module `ExternalSystems/` folder named for the system it calls, with an event handler named `{Action}On{Fact}Handler`.

**Rationale:** A reaction and a job are triggers, like an endpoint, and a trigger belongs to what it triggers. Placed with its use case, the handler names the command of that use case from inside the folder of the use case, so no command leaves its folder for a caller to reach it, and the folder shows every way the use case starts. A folder named for a fact would gather the commands of several use cases under one name instead, and it would never show every reaction to the fact, because those reactions span modules. The question of what happens when a fact is recorded is answered by the model query on the event and by the handler names, which carry the fact. Settled by [decision/0063-place-each-trigger-with-what-it-runs](decision/0063-place-each-trigger-with-what-it-runs).

**Example:** `Lending/Holds/UseCases/ExpireHold/Application/ExpireHoldOnLoanReturnedHandler` runs the expire-hold use case when a loan is returned, and `Lending/Notifications/LoanDue/SendDueNoticeOnLoanBorrowedHandler` only sends the due notice.

**Checked by:** `litespec model check` reports `code-placement-drift` (level S) for an event handler or job class outside the folder its record's effects name.

### Name Repository Variables After Their Interface

**ID:** `litespec/workspace/naming#name-repository-variables-after-their-interface`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler, validator, reaction, repository, job

**Statement:** Each repository parameter, field or local MUST be named after its interface without the `I` prefix, ending in `Repository`.

**Rationale:** A short plural name hides which aggregate the variable guards. `orders` can be a list, a query, or the repository, and each reader guesses. Naming the variable after its interface keeps one name per repository at every use site, so a reviewer reads the dependency from the name alone.

**Example:** A parameter of type `IExpenseRepository` is named `expenseRepository`, and a field of type `ILoanRepository` is named `loanRepository`.

**Reviewed for:** Does each repository parameter, field, or local carry the name of its interface without the `I` prefix? Read the repository use sites.

### Reveal Asynchrony and Cancellation in Operation Names

**ID:** `litespec/workspace/naming#reveal-asynchrony-and-cancellation-in-operation-names`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler, validator, reaction, port, repository, endpoint, migration, job

**Statement:** An asynchronous operation MUST reveal its asynchrony in its name and accept cancellation explicitly when the operation is cancelable.

**Rationale:** A caller decides from the signature whether to await and what to cancel, so both answers belong in the signature: the name reveals the asynchrony and a cancelable operation accepts cancellation explicitly. An operation is cancelable when it performs I/O or awaits, and anything else has nothing to cancel.

**Reviewed for:** Does each asynchronous operation name reveal asynchrony, with each cancelable operation signature taking cancellation explicitly? Read the public signatures in `apps/api/src`.

### Name Exceptions by Failed Rule

**ID:** `litespec/workspace/naming#name-exceptions-by-failed-rule`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** An exception name MUST state the rule that failed, using `{DomainType}{Reason}Exception` for a Domain rejection.

**Rationale:** An exception name tells the reader which rule failed on which type. The form `{DomainType}{Reason}Exception` puts both in the name, and the `DomainType` is the aggregate root or an anchored owned type, so the name always starts with the aggregate root.

**Reviewed for:** Does each exception name state its failed rule, with Domain rejections in `{DomainType}{Reason}Exception` form leading with the aggregate root? Read the exception names.

### Use Intent-Revealing Boolean Names

**ID:** `litespec/workspace/naming#use-intent-revealing-boolean-names`

**Strength:** advisory

**Applies to:** artifact: aggregate, state-record, value-object, domain-event, use-case, command-handler, query-handler, validator, reaction, port, repository, endpoint, migration, job

**Statement:** A boolean property or method SHOULD use `Is`, `Has`, `Can`, or a precise verb.

**Deviation:** A name fixed by an external contract or an overridden member keeps its fixed wording.

**Rationale:** A boolean answers a yes-or-no question, so its name asks one. The prefixes `Is`, `Has`, `Can`, or a precise verb make the question explicit: `HasLines` and `CanPublish` state a condition. `LinesPresent` and `CheckPublish` state neither a question nor an answer, so the caller reads the body to learn the polarity.

**Reviewed for:** Does each boolean property and method name in `apps/api/src` start with `Is`, `Has`, `Can`, or a precise verb? Read the boolean names.

### Use Current Language Features

**ID:** `litespec/workspace/naming#use-current-language-features`

**Strength:** advisory

**Applies to:** artifact: aggregate, state-record, value-object, domain-event, use-case, command-handler, query-handler, validator, reaction, port, repository, endpoint, migration, job

**Statement:** Production code SHOULD target the pinned language version and use its current construct over an older equivalent.

**Deviation:** Code that targets an older runtime or generated output keeps the construct its target needs.

**Rationale:** A pinned language version gives every writer the same constructs. Using the current construct over its older equivalent keeps one idiom across the codebase instead of one per author. In the dotnet stack that means collection expressions and pattern matching, which express the same intent with less incidental code.

**Reviewed for:** Does production code target the pinned language version and use its current constructs? Read the toolchain pins and the language idioms in `apps/api/src`.

### Avoid Primary Constructors on Classes and Structs

**ID:** `litespec/workspace/naming#avoid-primary-constructors-on-classes-and-structs`

**Strength:** advisory

**Applies to:** artifact: aggregate, state-record, value-object, domain-event, use-case, command-handler, query-handler, validator, reaction, port, repository, endpoint, migration, job

**Statement:** A class or struct SHOULD NOT declare a primary constructor.

**Deviation:** A positional record keeps its positional parameters, which define immutable data rather than captured state.

**Rationale:** A primary constructor parameter is captured, mutable, and visible in the whole class body. Nothing marks it read-only, so a method can reassign it and every later call sees the new value. A private read-only field states the intent the parameter hides. Inside the body, the parameter name reads the same as a local, so the reader loses the cue that something is class state. Copying a parameter into a property while also using the parameter elsewhere stores it twice. Validation, guard clauses, and derived fields have no home: they need either field initializers with side effects or a second constructor that chains to the primary one. Documentation for the parameters moves to the type, where tools show it less well than on a constructor. A type with several dependencies puts them all on the type line, which wraps badly and hides the type's name and role. Positional records look the same but behave differently, with generated read-only properties, which misleads readers who know records. An explicit constructor with private read-only fields avoids every one of these.

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

### Use Predictable Frontend Names

**ID:** `litespec/workspace/naming#use-predictable-frontend-names`

**Strength:** required

**Applies to:** artifact: component, route, form

**Statement:** A frontend folder MUST use kebab-case, a component file and its export PascalCase, and a React hook file `use-{name}`.

**Rationale:** Every file gets one predictable name: kebab-case folders, PascalCase components with matching exports, and `use-{name}` hooks. One mapping lets a reader move between the documentation tree and the source tree without translation, and lets an agent derive the file from the feature name.

**Reviewed for:** Does each frontend folder use kebab-case, each component file and export PascalCase, and each React hook file `use-{name}`? Read the frontend tree.

## Conventions

### Align Business Names Across Layers

**ID:** `litespec/workspace/naming#align-business-names-across-layers`

**Strength:** default

**Applies to:** artifact: record:module

**Default:** Use one business name for a module across Domain, Application, endpoints, features, and documentation.

**Replacement:** A project whose module carries different names per layer records its own per-layer names in its project documentation.

**Rationale:** One business name per module keeps every layer using the same word: the Posts module maps to `Shop/Posts`, `features/posts`, and `.litespec/behavior/modules/posts` without translation. A second name in any layer reads as a second concept that does not exist.

**Reviewed for:** Does one business name for each module span Domain, Application, endpoints, features, and documentation, or does the project documentation name the project's own per-layer names? Read the module names in each layer.

### Keep Namespaces Aligned with Folders

**ID:** `litespec/workspace/naming#keep-namespaces-aligned-with-folders`

**Strength:** default

**Applies to:** artifact: aggregate, state-record, value-object, domain-event, use-case, command-handler, query-handler, validator, reaction, port, repository, endpoint, migration, job

**Default:** Start a namespace with the product name and follow the folders beneath the project root.

**Replacement:** A project whose layout needs another root records its own namespace root in its project documentation.

**Rationale:** A namespace that starts with the product name and follows the folders beneath it puts each type exactly where its namespace says. Including `src` or `apps` couples the type name to a workspace layout decision. The project root is the directory holding the project file, so every folder above it stays outside the namespace.

**Reviewed for:** Does each namespace start with its product name and follow the folders beneath the project root, or does the project documentation name the project's own namespace root? Read the namespaces beside the folders.

### Avoid Generic Type Names

**ID:** `litespec/workspace/naming#avoid-generic-type-names`

**Strength:** advisory

**Applies to:** artifact: aggregate, state-record, value-object, domain-event, use-case, command-handler, query-handler, validator, reaction, port, repository, endpoint, migration, job

**Default:** Avoid `Manager`, `Helper`, `Processor`, `Common`, `Utility`, `BaseService`, `DataService`, `MessageBus`, `Engine`, `Facade`, `Wrapper`, and `Factory` when a narrower name exists.

**Replacement:** A project that keeps a listed name records the name and its reason in its project documentation.

**Rationale:** A generic name invites unrelated responsibility, because nothing in it excludes the next addition. A narrower name states what the type does and, by exclusion, what it does not. `Factory` is generic on a class but exact on a named static aggregate factory method, so the default covers the class and not the method.

**Reviewed for:** Does each type name in `apps/api/src` avoid the listed generic names where a narrower name exists, or does the project documentation name the kept name and its reason? Read the type names.

### Derive Boundary Names from the Ubiquitous Term

**ID:** `litespec/workspace/naming#derive-boundary-names-from-the-ubiquitous-term`

**Strength:** default

**Applies to:** artifact: use-case, endpoint

**Default:** Derive route segments and JSON field names from the current aggregate or ubiquitous term.

**Replacement:** A project with client-facing names that predate its glossary records its own boundary naming in its project documentation.

**Rationale:** Route segments and JSON field names are the contract clients read. Deriving them from the current aggregate or ubiquitous term, the shared Domain word for the concept, keeps the contract in the Domain language. Renaming a Domain concept then renames its boundary names with it, so the contract and the language stay aligned.

**Reviewed for:** Does each route segment and JSON field name derive from its current aggregate or ubiquitous term, or does the project documentation name the project's own boundary naming? Read the routes and contracts beside the glossary.
