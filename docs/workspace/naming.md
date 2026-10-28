# Naming and Code Style

## Intent

Names are the first documentation a reader meets: a name states the business intent and architectural role without opening the file. In a LiteSpec codebase every architectural role carries a fixed suffix, every type an aggregate owns starts with its root's name, and every frontend file follows one casing map. This page states those suffixes, anchors, and casings, with the exception-naming, boolean, async, and language-version rules beside them, for anyone who names code, and especially for agents working across sessions.

## Concepts

### Anchored Names

An aggregate root is the single type through which outside code changes an aggregate. Every type the aggregate owns starts its qualified name with the root's full name, so the name reveals its owner without folder context. `PostPublishedEvent` and `PostState.Published` both lead with `Post`; a namespace-level `PublishedState` does not.

### Role Suffixes

Each architectural role carries a fixed suffix: a command handler ends in `CommandHandler`, a result part carries its result name, an endpoint in `Endpoint`. The suffix table in the standards below lists every role with its pattern and one example. Full suffixes keep coordination apart from projection and boundary data apart from business objects.

## Standards

### Anchor Aggregate-Owned Types on the Aggregate Root

**ID:** `litespec/workspace/naming#anchor-aggregate-owned-types-on-the-aggregate-root`

**Statement:** An aggregate-owned type's qualified name MUST start with its aggregate root's full name.

**Rationale:** Leading with the root name reveals the owner without folder context. The rule covers events, states, child entities, values, and discriminated unions. A type nested inside a declaring type inherits that type's prefix through its qualified name, so `PostState.Published` satisfies the rule while a flat `PostStatePublishedState` repeats the anchor twice.

**Example:** `PostPublishedEvent` and `PostState.Published` both lead with `Post`. `PublishedState` declared at namespace level does not.

**Reviewed for:** Each aggregate-owned type name in `apps/api/src` begins with its aggregate root name, through nesting or prefix.

### Use Architectural Suffixes

**ID:** `litespec/workspace/naming#use-architectural-suffixes`

**Statement:** An architectural type MUST carry the suffix its role assigns in the table in this section.

**Example:**

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
| Repository interface | `I{Aggregate}Repository` | `IPostRepository` |
| Repository implementation | `{Aggregate}Repository` | `PostRepository` |
| Endpoint | `{UseCase}Endpoint` | `CreateDraftEndpoint` |
| HTTP request model | `{UseCase}RequestModel` | `CreateDraftRequestModel` |
| HTTP response model | `{UseCase}ResponseModel` | `CreateDraftResponseModel` |
| HTTP response item model | `{UseCase}ResponseItemModel` | `ListPostsResponseItemModel` |
| HTTP pagination model | `PaginationModel` | `PaginationModel` |
| Polymorphic transport model base | `{Concept}Model` | `FulfillmentOutcomeModel` |
| Polymorphic transport model case | `{Case}{Concept}Model` | `ShippedFulfillmentOutcomeModel` |
| API mapping class | `{UseCase}ApiMappings` | `CreateDraftApiMappings` |
| Options class | `{ConfigurationPurpose}Options` | `EmailOptions` |
| Persistence configuration | `{PersistedType}Configuration` | `PostConfiguration` |
| Registration class | `{Layer}ServiceRegistration` | `InfrastructureServiceRegistration` |
| Assembly marker | `{Layer}AssemblyMarker` | `ApplicationAssemblyMarker` |

In the dotnet stack, the repository interface carries the `I` prefix, options classes carry the `Options` suffix, and each scanned project carries an assembly marker.

An event handler folder starts with `On` and names the business fact without the `Event` suffix: `OnPostPublished` holds `NotifySubscribersOnPostPublishedHandler` for `PostPublishedEvent` (`litespec/workspace/naming#group-event-handlers-by-the-fact-they-handle`). The `On` prefix already marks the handler, and folder names carry no technical suffix, so `OnPostPublishedEvent` is wrong. The event type keeps its `Event` suffix (`litespec/workspace/naming#anchor-aggregate-owned-types-on-the-aggregate-root`).

Application types keep their full suffix: `{UseCase}Result`, `{UseCase}Handler`, and `{UseCase}Validator` shorten away the command-or-query distinction the full name carries. A result part carries the full name of its result, as `litespec/workspace/naming#name-result-parts-after-their-result` states, not a bare summary name like `PostSummary`. The full role suffix distinguishes command coordination from query projection without opening the file.

HTTP transport types keep their full suffix too: `{UseCase}Request` and `{UseCase}Response` shorten away the boundary marking. `Model` marks the type as passive boundary data rather than an operation or rich business object. Other project-owned, passive HTTP DTOs also name their concrete role and end in `Model`, such as `ListPostsResponseItemModel` or `PaginationModel`. Mapping classes use `ApiMappings` instead of the context-dependent `Mappings` suffix.

A polymorphic transport model mirrors a Domain discriminated union (`litespec/backend/api#mirror-a-domain-closed-set-as-a-transport-model-of-the-same-shape`). Its abstract base names the concept and each sealed case names its case, with both ending in `Model`: `FulfillmentOutcomeModel` and `ShippedFulfillmentOutcomeModel`.

The transport name drops the Domain union's aggregate prefix, such as `OrderFulfillmentOutcome`, while its discriminator string keeps the Domain union's stable case code unchanged.

**Reviewed for:** Each type name in `apps/api/src` and each frontend file name carries the suffix its role assigns in the table; `dotnet build` reports a missing handler, endpoint, or reaction suffix as `LS2005`, `LS2006`, or `LS2007`.

### Name Result Parts After Their Result

**ID:** `litespec/workspace/naming#name-result-parts-after-their-result`

**Statement:** Each type nested in a use-case result MUST be named after the result it belongs to, as `{UseCase}QueryResult{Part}` or `{UseCase}CommandResult{Part}`.

**Rationale:** A result part named for its own shape instead of its result collides with every other use case shaping the same thing: two summaries named `PostSummary` answer different queries with different fields. Leading with the full result name keeps each part beside the result it belongs to in every listing, and keeps command coordination apart from query projection without opening the file.

**Example:** `ListPostsQueryResultItem` belongs to `ListPostsQueryResult`, and `ReadLoanQueryResultHold` belongs to `ReadLoanQueryResult`.

**Reviewed for:** Each type nested in a use-case result in `apps/api/src` leads with its result name; `dotnet build` reports a result part not named after its result as `LS2009`.

### Group Event Handlers by the Fact They Handle

**ID:** `litespec/workspace/naming#group-event-handlers-by-the-fact-they-handle`

**Statement:** Each event handler MUST live in a folder named `On{Fact}`, the event name without its `Event` suffix, inside the aggregate folder of the subscriber, and be named `{Action}On{Fact}Handler`.

**Rationale:** The folder answers "what happens when this fact is recorded?" with one listing: one folder holds every handler of that fact in that module. The subscriber is the module whose records the handler changes, or for a handler that changes nothing, the module that owns the fact. A folder named for the handler instead scatters one fact across as many folders as handlers.

**Example:** `Loans/Loans/OnLoanBorrowed/SendDueNoticeOnLoanBorrowedHandler` sends the due notice when a loan is borrowed.

**Reviewed for:** Each event handler in `apps/api/src` lives in its `On{Fact}` folder and carries `{Action}On{Fact}Handler`; `dotnet build` reports a handler outside its fact folder as `LS2008`.

### Name Repository Variables After Their Interface

**ID:** `litespec/workspace/naming#name-repository-variables-after-their-interface`

**Statement:** Each repository parameter, field or local MUST be named after its interface without the `I` prefix, ending in `Repository`.

**Rationale:** A short plural name hides which aggregate the variable guards: `orders` could be a list, a query, or the repository, and each reader guesses. Naming the variable after its interface keeps one name per repository at every use site, so a reviewer reads the dependency from the name alone.

**Example:** A parameter of type `IExpenseRepository` is named `expenseRepository`, and a field of type `ILoanRepository` is named `loanRepository`.

**Reviewed for:** Each repository parameter, field, and local in `apps/api/src` carries its interface name without the `I` prefix; `dotnet build` reports a mismatch as `LS2010`.

### Name Asynchronous Operations Completely

**ID:** `litespec/workspace/naming#name-asynchronous-operations-completely`

**Statement:** An asynchronous operation MUST reveal its asynchrony in its name and accept cancellation explicitly when the operation is cancelable.

**Rationale:** A caller decides from the signature whether to await and what to cancel. Naming the asynchrony and accepting cancellation explicitly put both answers in the signature. An operation is cancelable when it performs I/O or awaits; anything else has nothing to cancel.

**Reviewed for:** Each asynchronous operation name reveals asynchrony, and each cancelable operation signature takes cancellation explicitly; read the public signatures in `apps/api/src`.

### Name Exceptions by Failed Rule

**ID:** `litespec/workspace/naming#name-exceptions-by-failed-rule`

**Statement:** An exception name MUST state the rule that failed, using `{DomainType}{Reason}Exception` for a Domain rejection.

**Rationale:** An exception name is the first diagnosis: it states which rule failed on which type. `{DomainType}{Reason}Exception` puts both in the name. `DomainType` is the aggregate root or an anchored owned type, so the name always starts with the aggregate root.

**Reviewed for:** Each exception name states its failed rule, with Domain rejections in `{DomainType}{Reason}Exception` form leading with the aggregate root.

### Use Intent-Revealing Boolean Names

**ID:** `litespec/workspace/naming#use-intent-revealing-boolean-names`

**Statement:** A boolean property or method MUST use `Is`, `Has`, `Can`, or a precise verb.

**Rationale:** A boolean answers a yes-or-no question, so its name asks one. `Is`, `Has`, `Can`, or a precise verb make the question explicit: `HasLines` and `CanPublish` state a condition. `LinesPresent` and `CheckPublish` state neither a question nor an answer, so the caller reads the body to learn the polarity.

**Reviewed for:** Each boolean property and method name in `apps/api/src` starts with `Is`, `Has`, `Can`, or a precise verb.

### Use Current Language Features

**ID:** `litespec/workspace/naming#use-current-language-features`

**Statement:** Production code MUST target the pinned language version and use its current construct over an older equivalent.

**Rationale:** A pinned language version gives every writer the same constructs. Using the current construct over its older equivalent keeps one idiom across the codebase instead of one per author. In the dotnet stack, collection expressions, pattern matching, and primary constructors express the same intent with less incidental code.

**Reviewed for:** Production code targets the pinned language version and uses its current constructs; read the toolchain pins and the language idioms in `apps/api/src`.

### Use Predictable Frontend Names

**ID:** `litespec/workspace/naming#use-predictable-frontend-names`

**Statement:** A frontend folder MUST use kebab-case, a component file and its export PascalCase, and a React hook file `use-{name}`.

**Rationale:** Kebab-case folders, PascalCase components with matching exports, and `use-{name}` hooks give every file one predictable name. One mapping lets a reader move between the documentation tree and the source tree without translation, and lets an agent derive the file from the feature name.

**Reviewed for:** Each frontend folder uses kebab-case, each component file and export uses PascalCase, and each React hook file uses `use-{name}`.

## Conventions

### Align Business Names Across Layers

**ID:** `litespec/workspace/naming#align-business-names-across-layers`

**Default:** Use one business name for a module across Domain, Application, endpoints, features, and documentation.

**Replacement:** A project whose module carries different names per layer records its own per-layer names in its project documentation.

**Rationale:** One business name per module keeps every layer pointing at the same word. The Posts module maps to `Domain/Posts`, `features/posts`, and `docs/domain/modules/posts` without translation. A second name in any layer reads as a second concept that does not exist.

**Reviewed for:** One business name for each module spans Domain, Application, endpoints, features, and documentation; where replaced, the project documentation names the project's own per-layer names.

### Keep Namespaces Aligned with Folders

**ID:** `litespec/workspace/naming#keep-namespaces-aligned-with-folders`

**Default:** Start a namespace with the project name and follow the folders beneath the project root.

**Replacement:** A project whose layout needs another root records its own namespace root in its project documentation.

**Rationale:** Starting the namespace with the project name and following the folders beneath it puts each type exactly where its namespace says. Including `src` or `apps` couples the type name to a workspace layout decision. The project root is the directory holding the project file, so every folder above it stays outside the namespace.

**Reviewed for:** Each namespace starts with its project name and follows the folders beneath the project root; where replaced, the project documentation names the project's own namespace root.

### Avoid Generic Type Names

**ID:** `litespec/workspace/naming#avoid-generic-type-names`

**Default:** Avoid `Manager`, `Helper`, `Processor`, `Common`, `Utility`, `BaseService`, `DataService`, `MessageBus`, `Engine`, `Facade`, `Wrapper`, and `Factory` when a narrower name exists.

**Replacement:** A project that keeps a listed name records the name and its reason in its project documentation.

**Rationale:** A generic name invites unrelated responsibility, because nothing in it excludes the next addition. A narrower name states what the type does and, by exclusion, what it does not. `Factory` is generic on a class and exact on a named static aggregate factory method, so the default names the class and not the method.

**Reviewed for:** Each type name in `apps/api/src` avoids the listed generic names where a narrower name exists; where replaced, the project documentation names the kept name and its reason.

### Derive Boundary Names from the Ubiquitous Term

**ID:** `litespec/workspace/naming#derive-boundary-names-from-the-ubiquitous-term`

**Default:** Derive route segments and JSON field names from the current aggregate or ubiquitous term.

**Replacement:** A project with client-facing names that predate its glossary records its own boundary naming in its project documentation.

**Rationale:** Route segments and JSON field names are the contract clients read. Deriving them from the current aggregate or ubiquitous term, the Domain's shared word for the concept, keeps the contract speaking the Domain language. Renaming a Domain concept then renames its boundary names, so the contract and the language stay aligned.

**Reviewed for:** Each route segment and JSON field name derives from its current aggregate or ubiquitous term; where replaced, the project documentation names the project's own boundary naming.
