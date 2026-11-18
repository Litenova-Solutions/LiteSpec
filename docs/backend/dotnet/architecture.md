# .NET Architecture

**Stack:** dotnet
**Concern:** application

Business code in a .NET backend lives in the Kernel, the Platform, and one project per module, with hosts that compose and serve them. This page defines how the shared architecture rules apply to .NET: which projects exist, how modules, aggregates, and use cases fill one namespace tree, which folders each level allows, and where host-only endpoints live, so every type keeps one home and project references, folders, and marks hold the boundaries. The shared rules are stated on the [Backend Architecture](../architecture.md) page.

## Concepts

### The Kernel, the Platform, the Modules, and Their Hosts

A module is a business capability, and it is complete: its domain, its use cases, its own infrastructure, and its entry points live inside it. Nothing about a module lives outside it except one registration call in the hosts. One technical platform serves every module, and one tiny business kernel holds the values every module speaks. The compiler enforces the boundaries between them; analyzers enforce the conventions inside them.

```text
apps/api/src/
  {Product}.Kernel/                    business values, no dependencies
    Money.cs  TypedId.cs  AggregateRoot.cs  DomainException.cs  BusinessDate.cs ...

  {Product}.Platform/                  technical capabilities, no business words
    Persistence/                       store setup, serializer, Money and typed id converters,
                                       the discriminator registry modules contribute to
    Messaging/                         mediators, outbox, in-process dispatch, recurring jobs
    Access/                            sessions, authorization guards, composable policies
                                       (Permission, RecentSignIn, Self, System)
    Auditing/                          audit sink, pseudonym lookup port
    Http/                              endpoint discovery, problem details, refusal mapping, JSON
    CountryPacks/                      pack loader and dated lookups
    Modules/                           IModule contract and AddModules(...)

  Modules/
    {Product}.Spending/                one project per module
      SpendingModule.cs                the only public entry point: registers everything below
      Contracts/                       public: identifiers, module events, ports others call
      Domain/
        Expense/                       Expense.cs, Expense.Approve.cs ..., lines, events, refusals
        Policy/  ...
      UseCases/
        SubmitExpense/
          Application/                 SubmitExpenseCommand.cs
                                       SubmitExpenseCommandHandler.cs
                                       SubmitExpenseCommandValidator.cs
                                       SubmitExpenseCommandAuthorizationDefinition.cs
                                       SubmitExpenseCommandAuditDefinition.cs
                                       SubmitExpenseCommandIdempotencyDefinition.cs
                                       SubmitExpenseResult.cs
          WebApi/                      SubmitExpenseEndpoint.cs
                                       SubmitExpenseRequest.cs
                                       SubmitExpenseResponse.cs
        ExportExpenses/
          Application/                 query, handler, definitions, result,
                                       IExpensesArchiveWriter.cs (the slice's port)
          Infrastructure/              ExpensesArchiveWriter.cs, one writer per section
          WebApi/                      ExportExpensesEndpoint.cs
      Infrastructure/                  the module's shared adapters
        Persistence/                   repositories, store contributions, read models
        ExternalSystems/               payout provider, document reader, bank formats ...
    {Product}.Reporting/  ...

  Hosts/
    {Product}.WebApi/Program.cs        AddPlatform(); AddModules(Spending, Reporting, ...); map endpoints
    {Product}.Worker/Program.cs        the same modules, jobs and outbox only
```

`{Product}.Kernel` holds the business values every module speaks and references only the base class library. `{Product}.Platform` holds the technical capabilities with no business words and references the Kernel and its libraries, never a module. Each module project references the Kernel, the Platform, and other modules' `Contracts/`, and only its `Contracts/` folder and its `{Module}Module` entry type are public, so the compiler refuses any other cross-module reference. Each project takes its own name as its root namespace. Each module's `Contracts/` folder compiles as its own `{Product}.{Module}.Contracts` project, so module projects reference contracts projects and never each other, and two modules may depend on each other's contracts without a project cycle. A project that keeps one modules project holding every module is an allowed deviation; `LS2020` then enforces the `Contracts/` boundary that project references otherwise enforce.

The WebApi host holds `Program.cs` composition, and the Worker host exists only when background work needs its own process. Each `Program.cs` decides which modules run in that host and in what order, and nothing else: it calls `AddPlatform()`, then `AddModules(...)` with the modules of that host, then maps the endpoints the platform discovered. Two hosts share wiring through the platform's `AddModules`, never through a folder beside the modules.

The host also keeps the endpoints that have no use case folder to sit in: protocol endpoints such as sign-out and device callbacks, static assets such as scripts it serves, and development-only tooling. Each such endpoint still carries its mark.

### The Module Tree and Its Layers

Every node of the tree allows a closed set of folder names, so any other folder is refused. Inside one of those folders, subfolders stay free except where the closed set table states Files only: the folder already states the layer, and the lowest-folder rule already placed the type. The layer of a file is its folder.

A module holds its entry type at its root, then `Contracts/`, `Domain/`, `UseCases/`, `Infrastructure/`, `Notifications/`, `ExternalSystems/`, and `ReadModels/`. `Domain/` holds one folder per declared aggregate in the singular; a module whose single aggregate has the module's name stays flat instead of nesting, with the aggregate's types directly in `Domain/`. Aggregates live under `Domain/{Aggregate}/`, and no sibling namespace contains the aggregate name, so a file beside the aggregate resolves the aggregate type without an alias. `UseCases/` holds one folder per declared use case with its layer folders. `Infrastructure/` holds `Persistence/` with the repositories, store contributions, and read models, and `ExternalSystems/` with the adapters for systems the module calls.

| The type is | It goes in |
|---|---|
| A business value every module uses | `{Product}.Kernel` |
| A technical capability with no business words | `{Product}.Platform` |
| An aggregate, its entities, values, events and refusals | `Modules/{Product}.{Module}/Domain/{Aggregate}/` |
| A use case's message, handler and definitions | `Modules/{Product}.{Module}/UseCases/{UseCase}/Application/` |
| A use case's endpoint and HTTP models | `Modules/{Product}.{Module}/UseCases/{UseCase}/WebApi/` |
| An adapter only one use case uses | `Modules/{Product}.{Module}/UseCases/{UseCase}/Infrastructure/` |
| A repository, store contribution or read model | `Modules/{Product}.{Module}/Infrastructure/Persistence/` |
| An adapter for an external system the module calls | `Modules/{Product}.{Module}/Infrastructure/ExternalSystems/` |
| A type other modules use | `Modules/{Product}.{Module}/Contracts/` |
| Wiring that decides which modules run | `Hosts/{Host}/Program.cs` |

A reaction's event handler and a job class sit in the `Application/` folder of the use case they run. One that runs no use case sits in the module `Notifications/` folder of the notification it sends, or in the `ExternalSystems/` folder of the system it calls. A module keeps each read model in its `ReadModels/` folder, and its public surface sorted by kind in `Contracts/`: `Identifiers/`, `IntegrationEvents/`, `ModuleEvents/`, and `Ports/`, so a reader sees at a glance what another module may reference.

A use case file takes its layer from its layer folder: Endpoint, RequestModel, and ResponseModel sit in `WebApi/`, the message, result, handler, validator, definitions, ports, reactions, and job sit in `Application/`, and adapters implementing the slice's own ports sit in `Infrastructure/`. A typed identifier that another module references lives in its owner's `Contracts/Identifiers/` folder, and shared values live in the Kernel project. The platform `Http/` folder holds what endpoints of two or more modules compile against, such as the endpoint contract and the current actor.

The record tree keeps use cases under aggregates while code holds every use case directly under its module. A use case record at `modules/{module}/aggregates/{aggregate}/use-cases/{use-case}` or at `modules/{module}/use-cases/{use-case}` maps to code at `Modules/{Product}.{Module}/UseCases/{UseCase}/`, and an aggregate record at `modules/{module}/aggregates/{aggregate}` maps to code at `Modules/{Product}.{Module}/Domain/{Aggregate}/`.

Models stay separate and flow one way: request model, command, aggregate, result, response model. The endpoint maps the request in and the result out, and the handler returns a result, never the aggregate.

### Visibility and Composition

A type stays visible only to the assemblies that need it. Only `Contracts/` and the `{Module}Module` entry type are public; everything else in a module project is internal. Implementations stay `internal sealed`, endpoints stay internal while the platform discovers them, and a message, result, or port turns public only inside `Contracts/`.

Service registration sits in the hosts: each module registers its own types through its entry type, and the host calls `AddPlatform()` and `AddModules(...)` in one visible order.

## Standards

### Mirror Modules, Aggregates, and Use Cases in One Namespace Tree

**ID:** `litespec/backend/dotnet/architecture#mirror-modules-aggregates-and-use-cases-in-one-namespace-tree`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every type in the Kernel, Platform, and module projects MUST live in the namespace tree that starts with the product name and follows the folders below the project root.

**Rationale:** When the namespace matches the folder path, `Claimwick.Spending.UseCases.SubmitExpense.Application` states the product, module, use case, and layer without a map. A reader moves from a Domain aggregate to its handler, its repository, and its endpoint by name alone, following folders under `litespec/workspace/naming#keep-namespaces-aligned-with-folders`. Each project takes its own name as its root namespace, so every namespace still starts with the product name. This provision binds `litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case` for .NET.

**Example:** The Expense aggregate of Claimwick's Spending module fills one branch:

```text
Modules/Claimwick.Spending/Domain/Expense/Expense.cs
Modules/Claimwick.Spending/UseCases/SubmitExpense/Application/SubmitExpenseCommandHandler.cs
```

The handler in the tree above has namespace `Claimwick.Spending.UseCases.SubmitExpense.Application`.

**Reviewed for:** Does every type in the Kernel, Platform, and module projects carry the namespace its folder path implies? Compare the namespaces with the folders.

### Keep Closed Folder Sets at Every Level

**ID:** `litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each level of the Kernel, Platform, and module projects MUST hold only the folders the closed set table in this section allows.

**Rationale:** Only the folders the table lists are allowed, so a catch-all folder has no home and review refuses it. Product-wide values sit in the Kernel, product-wide technical capabilities sit in the Platform, and cross-module contracts sit in each module Contracts folder.

`litespec/backend/architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers` decides between a use case folder and the module and product folders. Module folders mirror record homes: use cases sit in `UseCases/` folders with their layer folders, each reaction and job sits with the use case, notification, or external system it runs, sends, or calls, and each read model sits in the module `ReadModels/` folder. Settled by [decision/0063-place-each-trigger-with-what-it-runs](decision/0063-place-each-trigger-with-what-it-runs).

A use case record at `modules/{module}/aggregates/{aggregate}/use-cases/{use-case}` or at `modules/{module}/use-cases/{use-case}` maps to code at `Modules/{Product}.{Module}/UseCases/{UseCase}/`, and an aggregate record at `modules/{module}/aggregates/{aggregate}` maps to code at `Modules/{Product}.{Module}/Domain/{Aggregate}/`. A project that keeps one modules project holding every module maps each module to one folder of that project instead.

**Example:**

| Level | Allowed folders |
|:---|:---|
| Kernel project root | Files only |
| Platform project root | `Persistence/`, `Messaging/`, `Access/`, `Auditing/`, `Http/`, `CountryPacks/`, `Modules/` |
| `Modules/` folder | one project folder per declared module |
| Module project root | `Contracts/`, `Domain/`, `UseCases/`, `Infrastructure/`, `Notifications/`, `ExternalSystems/`, `ReadModels/` |
| One modules project root, when the project keeps it | one folder per declared module |
| Module `Contracts/` | `Identifiers/`, `IntegrationEvents/`, `ModuleEvents/`, `Ports/` |
| Module `Domain/` | one folder per declared aggregate, in the singular |
| Module `Domain/{Aggregate}/` | `Events/`, `Exceptions/` |
| Module `UseCases/` | one per declared use case |
| Use case | `Application/`, and `WebApi/` when the use case has an endpoint, and `Infrastructure/` when the use case has its own port |
| Module `Infrastructure/` | `Persistence/`, `ExternalSystems/` |
| Module `Notifications/` | one per notification a reaction or job of the module only sends |
| Module `ExternalSystems/` | one per external system a reaction or job of the module only calls |
| Module `ReadModels/` | one per declared read model of the module, in the plural |
| Use case `Application/`, `WebApi/`, and `Infrastructure/`, and each folder under `Notifications/`, `ExternalSystems/` and `ReadModels/` | Files only |

```text
{Product}.Kernel/
  Money.cs  TypedId.cs  AggregateRoot.cs  DomainException.cs  BusinessDate.cs
{Product}.Platform/
  Persistence/  Messaging/  Access/  Auditing/  Http/  CountryPacks/  Modules/
Modules/{Product}.Organizations/
  OrganizationsModule.cs
  Contracts/
    Identifiers/  IntegrationEvents/  ModuleEvents/  Ports/
  Domain/
    Team/              Team.cs  TeamState.cs  ITeamRepository.cs  Events/  Exceptions/
  UseCases/
    CreateTeam/
      Application/ CreateTeamCommand.cs  CreateTeamCommandResult.cs  CreateTeamCommandHandler.cs  CreateTeamCommandValidator.cs  CreateTeamCommandAuthorizationDefinition.cs  CreateTeamCommandAuditDefinition.cs  CreateTeamCommandIdempotencyDefinition.cs
      WebApi/      CreateTeamEndpoint.cs  CreateTeamRequestModel.cs  CreateTeamResponseModel.cs
    RemoveMember/
      Application/ RemoveMemberCommand.cs  RemoveMemberCommandHandler.cs  RemoveMemberOnAccountClosedHandler.cs
    ReadTeamDirectory/        a use case no single aggregate owns
  Infrastructure/
    Persistence/       TeamRepository.cs
    ExternalSystems/   adapters for systems the module calls
  Notifications/
    MemberRemoved/            a reaction that only sends this notification
  ReadModels/
    MemberAccounts/           a read model of another module's published facts
```

**Checked by:** `litespec model check` reports `code-placement-drift` for a folder outside its closed set or naming no declared record, for a file in the wrong layer folder of its use case, and for a reaction or job outside the folder of what it runs, and `mark-folder-drift` for a marked element outside the folder its mark names.

### Keep Business Values in the Kernel

**ID:** `litespec/backend/dotnet/architecture#keep-business-values-in-the-kernel`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The Kernel project MUST hold only the business values every module uses, with no reference outside the base class library.

**Rationale:** Only the values every module speaks live here: money, typed identifiers, dates, the aggregate root, the domain exception, and closed sets every module uses. The Kernel takes no package reference and no project reference, so a type a domain model may use is separated from a type only a host touches by the project boundary itself. A technical capability in the Kernel would drag its libraries into every module.

**Example:**

```text
{Product}.Kernel/
  Money.cs  TypedId.cs  AggregateRoot.cs  DomainException.cs  BusinessDate.cs
```

**Reviewed for:** Does the Kernel project hold only shared business values, with no package reference and no project reference? Look at the Kernel project file and its files.

### Keep Technical Capabilities in the Platform

**ID:** `litespec/backend/dotnet/architecture#keep-technical-capabilities-in-the-platform`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The Platform project MUST hold only technical capabilities that name no module, referencing only the Kernel, its libraries, and the base class library.

**Rationale:** Persistence setup, messaging, authorization, auditing, HTTP conventions, country pack loading, and module composition are built once and stay generic. A module plugs in by contributing data, never by copying platform code. A business word in the Platform names a module the Platform never knows, and a module reference fails the build because the Platform project references no module project.

**Example:**

```text
{Product}.Platform/
  Persistence/    store setup, serializer, Money and typed id converters, discriminator registry
  Messaging/      mediators, outbox, in-process dispatch, recurring jobs
  Access/         sessions, authorization guards, composable policies
                  (Permission, RecentSignIn, Self, System)
  Auditing/       audit sink, pseudonym lookup port
  Http/           endpoint discovery, problem details, refusal mapping, JSON
  CountryPacks/   pack loader and dated lookups
  Modules/        IModule contract and AddModules(...)
```

**Reviewed for:** Does any Platform file name a module, its aggregates, or its use cases, or does the Platform project reference a module project? Look at the Platform sources and its project references.

### Register Each Module Through Its Entry Point

**ID:** `litespec/backend/dotnet/architecture#register-each-module-through-its-entry-point`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each module MUST expose one public entry type implementing `IModule` that registers everything the module holds.

**Rationale:** The entry type is the module's only public surface beside `Contracts/`: it registers the module's adapters, contributes its stored types, discriminators, policies, and routes, and gives hosts and scanners the assembly reference. Each host adds the modules that run in it through `AddModules`, so two hosts share wiring through the platform instead of through a folder beside the modules that reaches into module internals. Settled by [decision/0107-kernel-platform-and-one-project-per-module](decision/0107-kernel-platform-and-one-project-per-module).

**Example:** `SpendingModule` implements `IModule`, and each host program calls `AddPlatform()` and then `AddModules(...)` with the modules that run in that host.

**Reviewed for:** Does every module expose one public entry type implementing `IModule`, and does every host add its modules through `AddModules`? Look at each module project and each `Program.cs`.

### Plug Modules into the Platform Through Contributions

**ID:** `litespec/backend/dotnet/architecture#plug-modules-into-the-platform-through-contributions`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A module MUST plug into a platform capability by contributing data to the platform registry instead of copying platform code.

**Rationale:** Each module entry type contributes its stored types and closed-set discriminators to the persistence registry, its policies to access, and its routes to HTTP discovery; the platform builds the one serializer, the one guard evaluation, the one converter set, and the one endpoint map. No module writes serializer, store, guard, or converter code, and the platform never names a module type. A copy drifts with the next platform change, while a contribution composes.

**Example:** The spending module contributes its stored types and closed-set discriminators, and the platform builds the one serializer from every module's contribution.

**Reviewed for:** Does any module hold serializer, store, guard, converter, or endpoint-discovery code the platform provides? Look at the module projects beside the platform registries.

### Hold Each Use Case Layer in Its Own Folder

**ID:** `litespec/backend/dotnet/architecture#hold-each-use-case-layer-in-its-own-folder`

**Strength:** required

**Applies to:** artifact: use-case

**Statement:** A use case folder MUST hold one folder per layer it has, and only the layers it has.

**Rationale:** The folders make each layer visible at a glance, keep each layer's rules tied to a place, and give new kinds of slice content an obvious home. A flat slice with the layer told by each file's suffix hides the layer in every file name and leaves new content with no home. A project adds a layer by giving it a row in the layer table with its allowed references and its analyzer rule, so the new folder carries the same contract as the first three. Settled by [decision/0107-kernel-platform-and-one-project-per-module](decision/0107-kernel-platform-and-one-project-per-module).

**Example:**

| Folder | Holds | Present when |
|---|---|---|
| `Application/` | the message, handler, validator, one definition class per concern, the result, the reaction or job that runs the use case, and ports only this use case calls | always |
| `WebApi/` | the endpoint, its request and response models | the use case is reached over HTTP |
| `Infrastructure/` | adapters that implement this use case's own ports (an archive writer, a renderer, a file parser only this use case uses) | the use case has its own port |

A project might later add `Mcp/` for a use case reached through an MCP entry point, `Notifications/` for a message only this use case sends, or `ReadModels/` for a projection only this use case reads, each as a layer with its own row, references, and rule.

**Reviewed for:** Does every use case folder hold only the layer folders it has, with each file in its layer's folder? Look at the use case folders.

### Keep Application Types in Their Layer Folders

**ID:** `litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An application MUST place each use case's messages, results, handlers, validators, definitions, and triggers in the use case's `Application/` folder, shared ports in the module `Infrastructure/` folder, and cross-module contracts in the sorted kind folders of its Contracts folder.

**Rationale:** One use case keeps its command, query, result, handler, validator, definitions, and triggers in its `Application/` folder, its endpoint and transport models in its `WebApi/` folder, and the adapters for its own ports in its `Infrastructure/` folder. A use case's own models never move up: two use cases with one shape each declare it, under `litespec/backend/architecture#give-each-use-case-its-own-models`.

Rules used by two or more use cases of one aggregate sit in the aggregate `Domain/{Aggregate}/` folder. Ports and adapters used by two or more use cases of one module sit in the module `Infrastructure/` folder beside the implementation. A module publishes its typed identifiers in `Contracts/Identifiers/`, its integration events in `Contracts/IntegrationEvents/`, its module events in `Contracts/ModuleEvents/`, and its read-only cross-module ports in `Contracts/Ports/`, so the public surface of a module stays visible in its Contracts folder. It publishes no command and no refusal.

**Reviewed for:** Does every message, result, handler, validator, definition, and trigger sit in its own use case `Application/` folder, every shared port in the module `Infrastructure/` folder, and every cross-module contract in its Contracts kind folder? Check the use case, Infrastructure, and Contracts folders.

### Place Each Read Model in Its Module Read Models Folder

**ID:** `litespec/backend/dotnet/architecture#place-each-read-model-in-its-module-read-models-folder`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each read model MUST live in a folder named for the read model in the plural inside its module `ReadModels/` folder, holding the read-model document, one projection per source event named `{ReadModel}On{Fact}Projection`, the reader interface `I{ReadModel}ReadModel` and its implementation, each carrying the read model's mark.

**Rationale:** A read model's projections answer module events and integration events like reactions, but they write only their read model and run no use case. Placed with a use case, they would look like reactions; placed with the read model, one folder shows every event that fills it and the one reader its module's use cases call.

The mark links each class to the record that names its sources. A projection that wrote anything but its read model would be an unrecorded reaction. This provision binds `litespec/backend/application#read-another-modules-state-through-a-read-model` for .NET.

**Example:**

```text
Modules/Claimwick.Reporting/ReadModels/ReimbursedExpenses/
  ReimbursedExpense.cs                                        the read-model document, keyed by ExpenseId
  ReimbursedExpenseOnExpenseReimbursedProjection.cs           one projection per source event
  ReimbursedExpenseOnExpensePersonalDataErasedProjection.cs
  IReimbursedExpenseReadModel.cs                              what Reporting use cases read
  ReimbursedExpenseReadModel.cs                               its store implementation
```

**Checked by:** `litespec model check` reports `code-placement-drift` for a class carrying a read-model mark outside its read-model folder, and for a folder under `ReadModels/` that names no declared read model.

### Keep Implementation Types Internal

**ID:** `litespec/backend/dotnet/architecture#keep-implementation-types-internal`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler, validator, reaction, repository, endpoint

**Statement:** A handler, validator, definition, persistence implementation, or endpoint implementation MUST be declared `internal sealed`.

**Rationale:** A public type is visible to every assembly, so an implementation left public can be referenced by callers it was never written for. Messages, results, and ports turn public only inside `Contracts/`, while handlers, validators, and implementations stay `internal sealed`. Assembly scanning reads internal types through the module entry type, so scanning never justifies widening visibility. This provision binds `litespec/backend/architecture#keep-implementations-hidden-and-closed` for .NET.

**Checked by:** `dotnet build` reports `LS2001` for an implementation type that is not `internal sealed`.

### Expose Only Contracts and the Entry Point

**ID:** `litespec/backend/dotnet/architecture#expose-only-contracts-and-the-entry-point`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every type in a module project outside its `Contracts/` folder and its module entry type MUST be internal.

**Rationale:** With one project per module, visibility is the module boundary: public `Contracts/` and one public entry type, everything else internal, so no module names another module's internals and the compiler refuses the reference. Endpoints stay internal while the platform discovers them in every module assembly the host adds. A project that keeps one modules project holding every module keeps the same visibility inside each module folder, with `LS2020` as the boundary.

**Reviewed for:** Is any type outside `Contracts/` and the entry type public? Look at the visibility modifiers in each module project.

### Follow Layer Direction in Every File

**ID:** `litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A type MUST reference only the layers its own layer allows under the layer table of [Backend Architecture](../architecture.md#the-four-layers).

**Rationale:** Layers are roles played by folders and use case files, whatever project holds them: a file takes its layer from its folder and, inside a use case folder, from its role suffix. Global usings import namespaces for every file, so the rule covers them too: a global using hides no coupling. The dependency rules below bind the layer table to projects and folders. This provision binds `litespec/backend/architecture#point-dependencies-inward` for .NET.

| From | May reference | Enforced by |
|---|---|---|
| Kernel | the base class library only | project references |
| Platform | Kernel and its libraries; never a module | project references |
| A module | Kernel, Platform, and other modules' `Contracts/` | project references plus visibility: only `Contracts/` and `{Module}Module` are `public`, everything else `internal` |
| `Domain/` | Kernel and the module's own `Domain/` | analyzer (folder) |
| `UseCases/*/Application/` | `Domain/`, its own slice, the module's `Contracts/` and `Infrastructure/` ports, Platform abstractions, other modules' `Contracts/`; never another slice | analyzer (folder) |
| `UseCases/*/WebApi/` | its own `Application/`, Kernel values, typed identifiers; never `Domain/` types, repositories or another slice | analyzer (folder) |
| `UseCases/*/Infrastructure/` | its own `Application/` ports, Platform, libraries | analyzer (folder) |
| `Infrastructure/` (module) | `Domain/`, Platform, libraries; implements the module's ports | analyzer (folder) |
| Hosts | Platform and each module's entry type | project references |

**Example:** Domain references no Application, Infrastructure, or WebApi type, and none of the persistence, web, mediator, logging, and injection namespaces. Application references no Infrastructure or WebApi type outside the module's `Infrastructure/` ports, and no ASP.NET Core type. WebApi references no Domain type outside typed identifiers and Kernel values, and no repository.

**Checked by:** `dotnet build` reports `LS1001` for a Domain type naming an outer-layer type and `LS2018` for an Application or WebApi type naming a type outside its layer.

### Keep Use Case Folders Isolated

**ID:** `litespec/backend/dotnet/architecture#keep-use-case-folders-isolated`

**Strength:** required

**Applies to:** artifact: use-case, command-handler, query-handler, validator, endpoint, reaction

**Statement:** A use case folder MUST reference no type in another use case folder.

**Rationale:** A use case folder holds one use case, so a reference between two such folders couples two slices that change for different reasons. A reaction or job that runs a use case sits in that use case's own `Application/` folder, so it references the message from inside, and no handler sends another use case's message.

A shape two use cases need is declared in each, under `litespec/backend/architecture#give-each-use-case-its-own-models`. Behavior they share belongs in the module or product folder that holds every consumer, under `litespec/backend/architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers`. The rule applies to files and to global usings alike.

Kernel values are referenced directly, never copied: money, typed identifiers, dates, and closed sets the Kernel owns are referenced from any layer the layer table allows, and WebApi models carry them on the wire. Copying a Kernel value into a use case folder breaks the rule.

**Checked by:** `dotnet build` reports `LS2019` for a use case folder naming a type in another use case folder.

### Reach Another Module Only Through Contracts

**ID:** `litespec/backend/dotnet/architecture#reach-another-module-only-through-contracts`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A module MUST reach another module only through types its Contracts folder publishes.

**Rationale:** The Contracts folder is the public surface of a module, sorted by kind: typed identifiers in `Identifiers/`, integration events in `IntegrationEvents/`, module events in `ModuleEvents/`, and read-only ports other modules call in `Ports/`. Domain may reference the typed identifiers; ports, module events, and integration events stay outside Domain.

With one project per module, project references and visibility enforce the boundary: only `Contracts/` and the entry type are public, so a module cannot name another module's internals and the compiler refuses the reference. A project that keeps one modules project holding every module enforces the same boundary through `LS2020`.

A module publishes no command and no refusal: another module triggers its acts through a module event its reaction answers, and each module refuses with its own refusals. A module that needs another module's state keeps its own read model built from the owner's module events, or asks one question through a decision port in the owner's `Contracts/Ports/` folder. It loads no foreign aggregate or repository. A reaction to another module's fact subscribes to the published module event, never to the domain event.

**Checked by:** `dotnet build` reports `LS2020` for a module reference naming another module type outside its Contracts folder.

### Follow Declared Module Dependencies

**ID:** `litespec/backend/dotnet/architecture#follow-declared-module-dependencies`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A module MUST reference only modules its record names in dependsOn.

**Rationale:** The `dependsOn` list states which modules a module may reference. An undeclared edge crosses a boundary the design does not allow. The rule covers code references and record links alike, and the graph stays acyclic.

**Reviewed for:** Does every cross-module reference name a module its owner declares in `dependsOn`, with no dependency cycle? Check the cross-module references beside the module records.

### Keep Host Endpoints in Hosts

**ID:** `litespec/backend/dotnet/architecture#keep-host-endpoints-in-hosts`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** Each host endpoint without a handler MUST live in its host project.

**Rationale:** Protocol endpoints, static assets, and development-only tooling belong to no use case folder, so the host keeps them. Each still carries its mark, and the placement check accepts a host endpoint in the host.

**Reviewed for:** Does every host endpoint without a handler live in its host project and carry its mark? Check the host endpoints beside the use case folders.

## Conventions

### Separate the Kernel, the Platform, and the Modules

**ID:** `litespec/backend/dotnet/architecture#separate-the-kernel-the-platform-and-the-modules`

**Strength:** default

**Applies to:** artifact: any

**Default:** Use a `{Product}.Kernel` project for the business values every module speaks, a `{Product}.Platform` project for the technical capabilities, one `{Product}.{Module}` project per module grouped under `Modules/`, and WebApi and optional Worker hosts grouped under `Hosts/`, at every size.

**Replacement:** A project records its own project layout in its host documentation.

**Rationale:** Each module project then holds one complete business capability: a reader opening it sees the module's domain, use cases, infrastructure, and entry points, and one use case stays in one tree instead of spreading across layer projects. The compiler enforces the boundaries: the Kernel references only the base class library, the Platform references no module, and each module exposes only its `Contracts/` and its entry type. Hosts stay separate projects, because two hosts can run the same modules.

A project that keeps one modules project holding every module is an allowed deviation; `LS2020` then enforces the `Contracts/` boundary that project references otherwise enforce. Settled by [decision/0107-kernel-platform-and-one-project-per-module](decision/0107-kernel-platform-and-one-project-per-module).

**Example:**

```text
apps/api/src/{Product}.Kernel/
apps/api/src/{Product}.Platform/
apps/api/src/Modules/{Product}.{Module}/     one project per module
apps/api/src/Hosts/{Product}.WebApi/
apps/api/src/Hosts/{Product}.Worker/     only when background work needs its own process
```

**Reviewed for:** Does the solution hold only the Kernel, the Platform, one project per module, and their hosts, with the reference direction the dependency rules state? Check the solution file and the project references.

### Keep Composition in Hosts

**ID:** `litespec/backend/dotnet/architecture#keep-composition-in-hosts`

**Strength:** default

**Applies to:** artifact: any

**Default:** Compose each host in `Program.cs` and host registration modules: register transport services there and call `AddPlatform` and `AddModules`.

**Replacement:** A project names its registration layout in its host documentation.

**Rationale:** Domain and Application declare abstractions for the hosts to implement. A registration call inside either layer makes the abstraction depend on the thing it abstracts, which inverts the dependency direction and drags Infrastructure into layers that must not name it. Each module registers its own types through its entry type, and `Program.cs` calls `AddPlatform()` and `AddModules(...)` in one visible order, so two hosts share wiring through the platform instead of through a folder beside the modules. This provision binds `litespec/backend/architecture#keep-service-registration-in-hosts` and `litespec/backend/architecture#compose-each-process-explicitly` for .NET.

**Example:** The platform call, the modules call, and the endpoint map, in order:

```text
AddPlatform(...)     persistence, messaging, access, auditing, HTTP, country packs
AddModules(...)      one entry type per module that runs in this host
MapEndpoints(...)    the endpoints the platform discovered
```

`Program.cs` keeps their call order visible. The platform owns endpoint discovery, Problem Details, refusal mapping, and JSON; the host owns transport security, authentication, authorization, and OpenAPI.

**Reviewed for:** Does every host compose its process in `Program.cs` and host registration modules through `AddPlatform` and `AddModules`? Check each `Program.cs`.

### Expose One Public Entry Type per Module

**ID:** `litespec/backend/dotnet/architecture#expose-one-public-entry-type-per-module`

**Strength:** default

**Applies to:** artifact: any

**Default:** Expose the module entry type as the one public marker of each module project for host composition, endpoint discovery, and scanning.

**Replacement:** A project names its scanner assembly reference in its host documentation.

**Rationale:** Scanners and the host need an assembly reference, and without a marker developers make handlers public to supply one, widening visibility for tooling convenience. Each module project's entry type gives the host and the scanners their reference while every handler stays internal. The platform discovers the internal endpoints in every module assembly the host adds.

**Reviewed for:** Does each module project expose exactly its entry type as its one public marker, with no unneeded marker elsewhere? Check each project for its marker.

### Compile Each Contracts Folder as Its Own Project

**ID:** `litespec/backend/dotnet/architecture#compile-each-contracts-folder-as-its-own-project`

**Strength:** default

**Applies to:** artifact: any

**Default:** Compile each module's `Contracts/` folder as its own `{Product}.{Module}.Contracts` project inside the module folder, referencing only the Kernel, the Platform, and the Contracts projects it names, and let each module project reference the Contracts projects of the modules it reaches instead of their module projects.

**Replacement:** A project whose module dependencies run one way only references module projects directly and records that choice in its host documentation.

**Rationale:** Project references cannot form a cycle, but module dependencies through contracts often run both ways: a module reacts to a module event of a module that depends on it, or one module's port names another module's identifier. With one project per module and module projects referencing each other, the first such edge fails the build. A Contracts project depends on no module project, so any module can reference any contracts, and the module projects never reference each other. The Contracts projects must not form a cycle among themselves, so an identifier that two modules' contracts both name, such as the workspace, business, or person identifier every module carries, belongs in the Kernel, which `litespec/backend/dotnet/architecture#keep-business-values-in-the-kernel` already allows for values every module speaks. A Contracts project holds no domain type: a module event that a domain event produces is mapped inside the publishing module, never by a factory in `Contracts/` that names the domain event.

**Example:**

```text
Modules/Claimwick.Spending/
  Claimwick.Spending.csproj                 references Kernel, Platform, Claimwick.Spending.Contracts,
                                            Claimwick.Reporting.Contracts
  SpendingModule.cs
  Contracts/
    Claimwick.Spending.Contracts.csproj     references Kernel and Platform only
    Identifiers/  ModuleEvents/  Ports/
  Domain/  UseCases/  Infrastructure/
```

The module project excludes `Contracts/**` from its own compile items so each contract compiles once, in its Contracts project.

**Reviewed for:** Does any module project reference another module project, does any Contracts project reference a module project or name a domain type, and do the Contracts projects reference each other without a cycle? Check the project references of each module and Contracts project.
