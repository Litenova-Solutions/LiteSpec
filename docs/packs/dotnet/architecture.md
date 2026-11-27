# .NET Architecture

**Stack:** dotnet
**Concern:** application

This page states how a .NET backend fills the Kernel, the Platform, modules, knowledge models, and hosts. It is for developers who place types, projects, and folders in a LiteSpec backend. A type in the wrong project breaks the build, so each project has one job and the compiler holds the line.

## Concepts

A module is one business capability in one project: its domain, its use cases, its adapters, and its endpoints live together. The Kernel carries the business values every module shares, and the Platform carries the technical services every module reuses. A host picks the modules that run in one process and wires them in a visible order.

```text
apps/api/src/
  {Product}.Kernel/               business values shared by every module
  {Product}.Platform/             technical services shared by every module
  Modules/
    {Product}.Spending/           one project per module
      SpendingModule.cs           the entry type that registers the module
      Domain/Expenses/            the Expense aggregate
      Application/Expenses/SubmitExpense/
      Ports/ReceiptPdf/           IReceiptPdf with its signature types in Models/
      Infrastructure/ReceiptPdf/  the adapter with its wire types in Models/
      WebApi/Expenses/SubmitExpense/
    {Product}.Spending.Contracts/ public data of the module, grouped by aggregate
      Expenses/                   ExpenseId, OnExpenseReimbursed/, ReadExpense/
  Knowledge/
    {Product}.Travel/             one model project per knowledge area
    {Product}.Travel.Germany/     one set project per variant
  Hosts/
    {Product}.WebApi/Program.cs   AddPlatform, AddModules, endpoint map
    {Product}.Worker/Program.cs   the same modules with jobs and the outbox
```

The Spending module shows the shape: the Expense aggregate lives in `Domain/Expenses`, the SubmitExpense use case fills `Application/Expenses/SubmitExpense` and `WebApi/Expenses/SubmitExpense`, and other modules read it through `Claimwick.Spending.Contracts/Expenses`.

```csharp
// Hosts/Claimwick.WebApi/Program.cs
builder.Services.AddPlatform();
builder.Services.AddModules(new SpendingModule(), new ReportingModule());
app.MapEndpoints();
```

### What Each Project Holds

The Kernel holds the shared business vocabulary: money, typed identifiers, the aggregate root, domain events, domain exceptions, and the audit vocabulary. It points at the base class library and groups its files into `Domain`, `Values`, and `Auditing` with no files at its root. A technical file in the Kernel pulls its libraries into every module, so such files live in the Platform instead.

The Platform holds the technical base in one place: store setup with its serializer and converters, messaging with its outbox and dispatch, access with its guards and composable policies, HTTP with its endpoint discovery and refusal mapping, the country pack loader, and the module contract. It names tenants and actors, while each business identifier stays with the module that owns its aggregate. A module adds its stored types, discriminators, policies, and routes to the platform registries, and the platform builds one serializer, one guard check, one converter set, and one endpoint map from those additions.

### How a Module Fills Its Folders

Each module project centers on its entry type, a public class that implements `IModule` and registers what the module holds. Hosts and scanners use that entry as their assembly marker, so handlers, endpoints, and adapters stay internal. The sibling Contracts project carries the public data beside the module, grouped by aggregate.

Namespaces track folders from the product name down, with each project name as its own root namespace. Aggregate folders use the plural, so `Domain/Expenses` holds the `Expense` type and `Application/Expenses/SubmitExpense` holds its command, handler, and result without shadowing the type. The record tree maps to the code tree by dropping typed container folders and turning names into PascalCase, while module-level reads drop the aggregate segment in each layer.

Layer folders show the role of each file: `Domain` for aggregates, `Application` for use cases, `Ports` for driven ports, `Infrastructure` for adapters and store code, `WebApi` for endpoints. Reactions and jobs sit with the use case they run, or with the notification or external system they serve when they run none. Read models sit together in `Application/ReadModels`, one plural folder per model with its document, its projections, and its reader.

The module record lists the modules it builds on in `dependsOn`, covering Contracts references, `queries` and `commands` edges, and reactions to published facts. The graph of those edges stays acyclic: a cycle points to a concept split across two modules, and the fix places the concept in one of them. Contracts references follow the same direction, with module projects pointing at Contracts projects instead of each other.

### How Hosts and Knowledge Sets Fit

Each `Program.cs` wires one process: transport setup, then `AddPlatform`, then `AddModules` with the modules of that host, then the endpoint map. Two hosts share wiring through the platform instead of a folder beside the modules. Endpoints without a use case folder, such as protocol endpoints, static assets, and development-only tooling, live in the host project with their marks.

Each knowledge area compiles as one model project with one set project per variant under `Knowledge`, with subject folders mirrored between model and sets. Claimwick compiles `Claimwick.Travel` with `Germany` and `Netherlands` sets, each repeating the `PerDiem` and `Mileage` folders of the model. Modules point at the model, sets point at their model and the Kernel, and hosts point at the sets they register at startup.

```csharp
services.AddTravel(x => x.Add<Germany>().Add<Netherlands>());
```

### Where the Names Come From

Kernel, Platform, and Port shorten earlier patterns to what each project holds, and [Lineage](../../learn/lineage.md#kernel-platform-and-port) states the sources and the names set aside.

## Standards

### Keep Closed Folder Sets at Every Level

**ID:** `litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each level of the Kernel, Platform, and module projects MUST hold only the folders the closed set table in this section allows.

**Rationale:** A closed set of folders keeps each level predictable: a reader finds the same folders in every module, and review turns away the rest. Product-wide values sit in the Kernel, product-wide services sit in the Platform, and cross-module data sits in each Contracts project. Module folders track record homes, with one plural aggregate folder and one folder per use case inside each serving layer, plus one folder per module-level read.

Reactions and jobs sit with the use case, notification, or external system they run, send, or call, and each read model sits in `Application/ReadModels`. A record at `modules/{module}/aggregates/{aggregates}/use-cases/{use-case}` maps to `Application/{Aggregates}/{UseCase}/` and `WebApi/{Aggregates}/{UseCase}/`, with typed container folders dropped and names in PascalCase. A project with one modules project maps each module to one folder of that project instead.

**Example:**

| Level | Allowed folders |
|:---|:---|
| Kernel project root | `Domain/`, `Values/`, `Auditing/`; no files |
| Platform project root | `Persistence/`, `Messaging/`, `Access/`, `Http/`, `CountryPacks/`, `Modules/`; no files; inside a capability past 20 files, one folder per sub-area. `CountryPacks/` keeps its loader and registry at its root and groups pack types by pack area: `Vat/`, `Charts/`, `Banking/`, `Filing/`, `Documents/`, `Country/` and `Retention/` |
| `Modules/` folder | one project folder per declared module, and one Contracts project folder per declared module |
| `Knowledge/` folder | one model project folder per knowledge area and one set project folder per variant of a model the folder holds; no files |
| Model or set project root | one folder per subject, mirrored in each set of the model |
| Module project root | `Domain/`, `Application/`, `Ports/`, `Infrastructure/`, `WebApi/`, and the entry type file |
| One modules project root, when the project keeps it | one folder per declared module |
| Contracts project root | one folder per aggregate with anything public, in the plural, and one folder per public module-level query; no files |
| Contracts `Contracts/{Aggregates}/` | the aggregate identifier when another module carries it, the types facts or messages of more than one aggregate carry when the aggregate owns the concept, one `On{Fact}/` folder per fact about the aggregate another module consumes, and one folder per public query or public command of that aggregate; no other files |
| Contracts `Contracts/{Aggregates}/On{Fact}/` | the fact type with the types only it carries; no other files |
| Contracts `Contracts/{Aggregates}/{UseCase}/` and each public module-level query folder | the message, its result, and the result parts; no other files |
| Module `Contracts/`, in the one modules project only | one folder per aggregate with anything public, in the plural, and one folder per public module-level query; no files |
| Module `Domain/` | one folder per declared aggregate, in the plural |
| Module `Domain/{Aggregates}/` | the root's main file, the repository interface as the aggregate's store interface, the root's partial files for a concern no single act owns (`{Root}.{Concern}.cs`), `{Act}/` per root action, `Parts/`, `Facts/`, `Events/`, `Exceptions/` |
| Module `Domain/{Aggregates}/{Act}/` | the partial file `{Root}.{Act}.cs`, the act's inputs, its private rules, and the steps only it calls |
| Module `Domain/{Aggregates}/Parts/` | the aggregate's entities, value objects, state types, and the stateless rules several acts share |
| Module `Domain/{Aggregates}/Facts/` | values passed in from other modules or the country pack |
| Module `Application/` | one folder per declared aggregate that has use cases, one folder per declared module-level read, `Notifications/`, `ExternalSystems/`, `ReadModels/`, `Settings/` |
| Module `Application/{Aggregates}/`, `WebApi/{Aggregates}/` | one folder per use case declared under that aggregate; no files |
| Module `Application/Settings/` | the module options class; no other files |
| Module `Ports/` | one folder per driven port of the module; no files |
| Module `Ports/{Port}/` | the interface `I{Port}` and `Models/` |
| Module `Ports/{Port}/Models/` | every type the interface's signatures name |
| Module `Infrastructure/` | `Persistence/`, `Messaging/`, `Pipeline/`, one folder per port the module implements |
| Module `Infrastructure/Messaging/` | the module's message pipeline pieces: the domain-to-module event mapping, audit writers, and the resolvers and mappers the module registers into the message pipeline; files only |
| Module `Infrastructure/Pipeline/` | the module's HTTP pipeline pieces serving no single use case: middleware, authorization conditions, authentication handlers; files only, past 20 files one folder per protocol holding that protocol's wire types beside its pieces. A protocol endpoint that serves one use case sits in that use case's `WebApi/{Aggregates}/{UseCase}/` folder |
| Module `Infrastructure/{Port}/` | the adapter class, its options class `{Adapter}Options.cs`, `Models/`, and `Parts/` when the adapter calls helpers; several adapters of one port sit side by side |
| Module `Infrastructure/{Port}/Models/` | the external system's wire types: requests, responses, file records |
| Module `Infrastructure/{Port}/Parts/` | the helpers the adapter calls |
| Module `Infrastructure/Persistence/` | the module store contribution, and one folder per aggregate with a repository; no other files |
| Module `Infrastructure/Persistence/{Aggregates}/` | the aggregate's repository implementation, stored documents, and mapping |
| Module `WebApi/` | the aggregate use case folders and module-level read folders of use cases reached over HTTP |
| Module `Application/Notifications/` | one per notification a reaction or job of the module sends |
| Module `Application/ExternalSystems/` | one per external system a reaction or job of the module calls |
| Module `Application/ReadModels/` | one per declared read model of the module, in the plural |
| Each use case folder and each folder under `Notifications/`, `ExternalSystems/`, `ReadModels/`, and `Settings/` | Files only |

```text
{Product}.Kernel/
  Domain/      the aggregate base, the domain event marker, the domain exceptions
  Values/      money, the typed id marker
  Auditing/    the audit vocabulary
{Product}.Platform/
  Persistence/  Messaging/  Access/  Http/  CountryPacks/  Modules/
Modules/{Product}.Spending/
  SpendingModule.cs
  Domain/
    Expenses/            Expense.cs  IExpenseRepository.cs  Submit/  Parts/  Facts/  Events/  Exceptions/
  Application/
    Expenses/
      SubmitExpense/     command, handler, validator, definitions, result
    ReadExpenseDirectory/  a module-level read with no aggregate folder
    Notifications/
      MemberRemoved/       a reaction that sends this notification
    ReadModels/
      ReimbursedExpenses/  a read model of published facts
  Ports/
    ReceiptPdf/          IReceiptPdf with its signature types in Models/
  Infrastructure/
    Persistence/Expenses/  the repository with its stored documents and mapping
    Messaging/           the domain-to-module event mapping, audit writers
    ReceiptPdf/          the adapter with its wire types in Models/ and helpers in Parts/
  WebApi/
    Expenses/
      SubmitExpense/     endpoint with request and response models
Modules/{Product}.Spending.Contracts/
  Expenses/
    ExpenseId.cs  OnExpenseReimbursed/ExpenseReimbursedModuleEvent.cs  ReadExpense/
  ReadExpenseDirectory/
```

**Checked by:** `litespec model check` reports `code-placement-drift` for a folder outside its closed set or naming no declared record, for a file directly at the Kernel or Platform root, for a file in the wrong layer folder of its use case, for a reaction or job outside the folder of what it runs, for an `{Aggregates}/` folder naming no declared aggregate, for a use case folder naming no use case of that aggregate, for a file directly in an `{Aggregates}/` folder, for a singular `Domain/{Aggregate}/` folder, for a Domain type directly in `Domain/` or a use case directly in `Application/` or `WebApi/` that belongs to an aggregate, for an exception file outside `Exceptions/` and a domain event file outside `Events/`, for a state file beside its aggregate root, for a second store interface of one aggregate, for a file directly in `Ports/`, for a port folder with no adapter folder in `Infrastructure/`, for an adapter folder with no port folder, for a type beside the adapter class outside `Models/` and `Parts/`, for a `Models/` or `Parts/` type no adapter file names, for a port or adapter folder naming a declared module, for a repository implementation outside its aggregate's `Persistence/` folder, for a use-case model or adapter file in `Persistence/`, for any other file directly in `Persistence/` outside the module store contribution, for an interface in a Contracts project, for a fact in a Contracts project outside its `{Aggregates}/On{Fact}/` folder, for an event file in an `On{Fact}/` folder that names no declared event or another fact, for a fact in an `On{Fact}/` folder outside the aggregate of the domain event it translates, for a command in a Contracts project outside the `{Aggregates}/{UseCase}/` folder of a public command, for a public use case message or result outside its `Contracts/{Aggregates}/{UseCase}/` path, for a test folder naming no declared record, and for a type whose namespace does not match its folder path through its layer segment, for a folder under `Knowledge/` that is neither a model nor a set of a model the folder holds, for a file directly in the Knowledge root, `folder-ungrouped` for a folder past 20 files that holds none of its allowed groupings, and `mark-folder-drift` for a marked element outside the folder its mark names.

### Name No Business Identifier in the Platform

**ID:** `litespec/packs/dotnet/architecture#name-no-business-identifier-in-the-platform`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The Platform MUST name no business identifier.

**Rationale:** The Platform carries the tenant and the actor across modules: the execution identity tenant and the principal subject. A business identifier in the Platform ties every module to a copy owned elsewhere, so each module converts at its own edge. The workspace module turns the tenant string into `WorkspaceId` and back, while the Platform tracks the tenant.

**Example:** The workspace module converts the tenant string into `WorkspaceId` at its edge and back when it answers; the Platform names only the tenant.

**Reviewed for:** Does any Platform file name a module identifier? Look at the Platform sources beside the module Contracts projects.

### Plug Modules into the Platform Through Contributions

**ID:** `litespec/packs/dotnet/architecture#plug-modules-into-the-platform-through-contributions`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A module MUST plug into a platform capability by contributing data to the platform registry instead of copying platform code.

**Rationale:** Shared code built once stays consistent: the platform owns the serializer, the guard check, the converters, and endpoint discovery. Each module adds its stored types, closed-set discriminators, policies, and routes to those registries. A copy of platform code inside a module drifts at the next platform change, while an addition composes.

**Example:** The spending module adds its stored types and closed-set discriminators, and the platform builds the one serializer from every module addition.

**Reviewed for:** Does any module hold serializer, store, guard, converter, or endpoint-discovery code the platform provides? Look at the module projects beside the platform registries.

### Keep Application Types in Their Layer Folders

**ID:** `litespec/packs/dotnet/architecture#keep-application-types-in-their-layer-folders`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An application MUST place each use case's messages, results, handlers, validators, definitions, and triggers in the use case's folder inside `Application/`, every port of the module in its own `Ports/{Port}/` folder holding the interface `I{Port}` with its signature types in `Models/` and no files directly in `Ports/`, each port's adapter in its `Infrastructure/{Port}/` folder with the same folder name, holding the adapter class with its options class `{Adapter}Options.cs`, its wire types in `Models/`, and the collaborators it needs in `Parts/`, and cross-module contracts grouped by aggregate in its Contracts project.

**Rationale:** One use case fills one folder per serving layer: its command, query, result, handler, validator, definitions, and triggers in `Application/{Aggregates}/{UseCase}/`, with its endpoint and transport models in `WebApi/{Aggregates}/{UseCase}/`. A module-level read drops the aggregate segment in each. A shape shared by two use cases appears in each, as `litespec/core/backend-architecture#give-each-use-case-its-own-models` states.

Rules shared by two or more use cases of one aggregate live in `Domain/{Aggregates}/`. Each port of the module fills its own `Ports/{Port}/` folder, so `Ports/` shows the full list of what the module needs from outside: the interface `I{Port}` with the types its signatures name in `Models/`, with no files directly in `Ports/`. Each adapter fills `Infrastructure/{Port}/` under the same folder name, with the adapter class, its options class, its wire types in `Models/`, and its helpers in `Parts/`. Whether an interface counts as a port stays a reviewer judgment on the owning module. A module publishes data only through its Contracts project, grouped by aggregate, with no refusal and no interface in the published surface.

**Checked by:** `litespec model check` reports `code-placement-drift` for a port folder holding no interface or more than one, for a port folder whose name is not its interface name without the `I`, for a type in a port folder outside `Models/` that the interface signatures do not name, for a type beside the adapter class outside `Models/` and `Parts/`, and for a `Models/` or `Parts/` type no adapter file names, and `dotnet build` reports `LS2018` for a `Ports/` file naming an `Application/`, `Infrastructure/` or `WebApi/` type.

### Place Each Read Model in Its Application Read Models Folder

**ID:** `litespec/packs/dotnet/architecture#place-each-read-model-in-its-application-read-models-folder`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each read model MUST live in a folder named for the read model in the plural inside its `Application/ReadModels/` folder, holding the read-model document, one projection per source event named `{ReadModel}On{Fact}Projection`, the reader interface `I{ReadModel}ReadModel` and its implementation, each carrying the read model's mark.

**Rationale:** A read model gathers answers from module events and integration events and serves the reads of its module. Beside a use case it reads as a reaction, so it sits with the other read models instead: one folder per model shows every event that fills it and the one reader its use cases call. The mark on each class points to the record that names its sources. A projection that writes past its own read model is an unrecorded reaction.

**Example:**

```text
Modules/Claimwick.Reporting/Application/ReadModels/ReimbursedExpenses/
  ReimbursedExpense.cs                                        the read-model document, keyed by ExpenseId
  ReimbursedExpenseOnExpenseReimbursedProjection.cs           one projection per source event
  ReimbursedExpenseOnExpensePersonalDataErasedProjection.cs
  IReimbursedExpenseReadModel.cs                              what Reporting use cases read
  ReimbursedExpenseReadModel.cs                               its store implementation
```

**Checked by:** `litespec model check` reports `code-placement-drift` for a class carrying a read-model mark outside its read-model folder, and for a folder under `ReadModels/` that names no declared read model.

### Keep Implementation Types Internal

**ID:** `litespec/packs/dotnet/architecture#keep-implementation-types-internal`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler, validator, reaction, repository, endpoint

**Statement:** A handler, validator, definition, persistence implementation, or endpoint implementation MUST be declared `internal sealed`.

**Rationale:** A public type is visible to every assembly, so a public handler invites calls outside its design. Messages, results, and ports turn public only inside the Contracts project, while handlers, validators, and implementations stay `internal sealed`. Assembly scanning reads internal types through the module entry type, so scanning gives no reason to widen visibility. This provision binds [Backend Architecture](../../core/backend-architecture.md) for .NET.

**Checked by:** `dotnet build` reports `LS2001` for an implementation type that is not `internal sealed`.

### Expose Only Contracts and the Entry Point

**ID:** `litespec/packs/dotnet/architecture#expose-only-contracts-and-the-entry-point`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every type in a module project outside its module entry type MUST be internal.

**Rationale:** With one project per module, visibility forms the boundary: a public Contracts project and one public entry type, with all else internal. The compiler turns away direct use of another module internals, and the platform still discovers internal endpoints in each added module assembly. A project with one modules project holds the same visibility inside each module folder, with `LS2020` as the boundary.

**Reviewed for:** Is any type in a module project outside the entry type public? Look at the visibility modifiers in each module project.

### Follow Layer Direction in Every File

**ID:** `litespec/packs/dotnet/architecture#follow-layer-direction-in-every-file`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A type MUST reference only the layers its own layer allows under the layer table of [Backend Architecture](../../core/backend-architecture.md).

**Rationale:** A layer is a role played by a folder: a file takes its layer from its top folder in the module. Global usings import namespaces for every file, so the rule covers them too. The project references plus the folder checks join the layer table into one boundary. This provision binds `litespec/core/backend-architecture#point-dependencies-inward` for .NET.

| From | May reference | Enforced by |
|---|---|---|
| Kernel | the base class library only | project references |
| Platform | Kernel and its libraries; never a module | project references |
| A module | Kernel, Platform, and other modules' Contracts projects | project references plus visibility: only the Contracts project and `{Module}Module` are `public`, everything else `internal` |
| A knowledge model | Kernel and its libraries | project references |
| A knowledge set | Its model and the Kernel | project references |
| `Domain/` | Kernel and the module's own `Domain/`, and the identifiers in other modules' Contracts projects | analyzer (folder) |
| `Ports/` | Kernel, the module's `Domain/` types, the identifiers in other modules' Contracts projects, Platform abstractions; never `Application/`, `Infrastructure/` or `WebApi/` | analyzer (folder) |
| `Application/{Aggregates}/{UseCase}/` | `Domain/`, its own use case folder, `Ports/`, `Application/ReadModels/`, the identifiers in other modules' Contracts projects, the facts its reactions answer, another module's public queries and query results with a declared `queries` edge, Platform abstractions; never another use case folder, never `Infrastructure/` or `WebApi/`, never another module's public command | analyzer (folder) |
| `Infrastructure/` | `Domain/`, `Ports/` (to implement them), Platform, libraries | analyzer (folder) |
| `WebApi/{Aggregates}/{UseCase}/` | its own `Application/{Aggregates}/{UseCase}/` (message and result), Kernel values, typed identifiers, Platform `Http/` and `Access/`; never `Ports/`, `Domain/` types, repositories, `Infrastructure/`, another module's public query, public command or result, or another use case | analyzer (folder) |
| Hosts | Platform, each module's entry type, and the knowledge sets it registers | project references |

**Example:** Domain names Kernel types, its own `Domain` types, and identifiers from other Contracts projects. `Ports` adds the module `Domain` types it serves and Platform abstractions, with no `Application`, `Infrastructure`, or `WebApi` types.

Application names `Domain`, its own use case folder, `Ports`, `ReadModels`, identifiers from other Contracts, facts its reactions answer, and public queries with a declared `queries` edge. Infrastructure names `Domain`, the `Ports` it implements, Platform, and libraries. WebApi names its own Application message and result, Kernel values, typed identifiers, and Platform `Http` and `Access`.

Handlers, reactions, and jobs name another module public query or query result with a declared `queries` edge, and reactions and jobs name public commands with a declared `commands` edge. An adapter in `Infrastructure/{Port}/` names Kernel values, typed identifiers, and the types of the port interfaces it implements, with no WebApi type. A knowledge set points at its model and the Kernel, and a module points at the knowledge model it reads.

**Checked by:** `dotnet build` reports `LS1001` for a Domain type naming an outer-layer type, `LS2018` for an Application, Ports, or WebApi type naming a type outside its layer, `LS2025` for an adapter in `Infrastructure/{Port}/` naming a Domain type outside those or a WebApi type, and `LS2029` for another module's public query or query result named outside a handler, reaction, or job, and for another module's public command or command result named outside a reaction or job.

### Keep Use Case Folders Isolated

**ID:** `litespec/packs/dotnet/architecture#keep-use-case-folders-isolated`

**Strength:** required

**Applies to:** artifact: use-case, command-handler, query-handler, validator, endpoint, reaction

**Statement:** The folders of one use case MUST reference no type in the folders of another use case.

**Rationale:** The folders of one use case hold one behavior, so a link between two use case folders joins behaviors that change for different reasons. A reaction or job that runs a use case sits in that use case `Application` folder and uses the message from inside, and no handler sends a command: another module public query is the one message a handler sends, with a declared `queries` edge.

A shape shared by two use cases appears in each, as `litespec/core/backend-architecture#give-each-use-case-its-own-models` states. Behavior shared more widely lives in the module or product folder that holds every consumer, as `litespec/core/backend-architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers` states. The rule covers files and global usings alike.

Kernel values stay referenced directly: money, typed identifiers, dates, and closed sets the Kernel owns come from any layer the layer table allows, and WebApi models carry them on the wire. The shared `Application` folders `Notifications`, `ExternalSystems`, `ReadModels`, and `Settings` belong to no use case: a use case folder names those types freely, and the check reports no link either way.

**Checked by:** `dotnet build` reports `LS2019` for a use case folder naming a type in another use case folder.

### Reach Another Module Only Through Contracts

**ID:** `litespec/packs/dotnet/architecture#reach-another-module-only-through-contracts`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A module MUST reach another module only through types its Contracts project publishes.

**Rationale:** The Contracts project is the public surface of a module, grouped by aggregate with identifiers, shared concept types, one `On{Fact}` folder per consumed fact, and one folder per public query or command. Each layer names only what it needs from another Contracts: identifiers across `Domain`, `Ports`, `Application`, and `WebApi`; facts in `Application` reactions; public queries and results in handlers, reactions, and jobs with a declared `queries` edge; public commands and results in reactions and jobs with a declared `commands` edge.

With one project per module, project references plus visibility form the boundary: only the Contracts project and the entry type are public, so other internals stay out of reach. A project with one modules project draws the same line through `LS2020`. A module publishes no refusal and no interface: it answers upstream facts through public commands from its reactions and jobs, asks public queries for current values, and reshapes kept data through its own read models.

**Checked by:** `dotnet build` reports `LS2020` for a module reference naming another module type outside its Contracts.

### Publish Only Data from Each Contracts Project

**ID:** `litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each Contracts project MUST publish only data grouped by aggregate, with no interface anywhere in the project.

**Rationale:** Data crosses the boundary; a call shape couples every consumer to provider code. The root holds one folder per aggregate with anything public, in the plural, and one folder per public module-level query, with no files. Each `Contracts/{Aggregates}/` folder holds the identifier another module carries, the types shared across facts or messages of that aggregate, one `On{Fact}/` folder per fact another module consumes, and one folder per public query or command with its message and result at the provider `Application` path.

Each fact sits in its own `On{Fact}/` folder in the aggregate of the domain event it translates, beside the public query and command folders of that aggregate, with the namespace `{Product}.{Module}.Contracts.{Aggregates}.On{Fact}`. A module-level public query sits in its own folder directly in `Contracts`. Names stay business names: `ReadBusinessQuery` with `BusinessResult`, with no transport shapes, routes, or HTTP verbs.

**Example:**

```text
Modules/Claimwick.Spending.Contracts/
  Expenses/
    ExpenseId.cs
    OnExpenseReimbursed/
      ExpenseReimbursedModuleEvent.cs
    ReadExpense/
      ReadExpenseQuery.cs
      ReadExpenseResult.cs
    CloseExpenses/
      CloseExpensesCommand.cs
      CloseExpensesResult.cs
  ReadExpenseDirectory/
    ReadExpenseDirectoryQuery.cs
    ReadExpenseDirectoryResult.cs
```

**Checked by:** `litespec model check` reports `code-placement-drift` for an interface in a Contracts project, for a fact in a Contracts project outside its `{Aggregates}/On{Fact}/` folder, for an event file in an `On{Fact}/` folder that names no declared event or another fact, for a fact in an `On{Fact}/` folder outside the aggregate of the domain event it translates, for a command in a Contracts project outside the `{Aggregates}/{UseCase}/` folder of a public command, for a public use case message or result outside its `Contracts/{Aggregates}/{UseCase}/` path, for a module event or integration event record with no `On{Fact}/` folder, and for a fact type naming no declared module or integration event.

### Keep Each Identifier with Its Owner

**ID:** `litespec/packs/dotnet/architecture#keep-each-identifier-with-its-owner`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A business identifier MUST live in the project of the module that owns its aggregate, in `Contracts/{Aggregates}/` when another module carries it and in `Domain/{Aggregates}/` otherwise, except in the Kernel with a decision record naming the Contracts cycle it breaks.

**Rationale:** An identifier points at its aggregate, so it lives where the aggregate lives: in the owner Contracts project when another module carries it, beside the `On{Fact}` folders and the public queries and commands of that aggregate, and in `Domain/{Aggregates}/` when no other module carries it. A copy placed by convenience drifts into three copies across consumer, Kernel, and owner. The Kernel holds an identifier only to break a Contracts cycle, and only with a decision record naming that cycle. The marker generic code uses for converters lives in the Kernel.

**Example:** `ExpenseId` belongs to `aggregate/spending.expense`. The reporting module carries it, so it lives in `Claimwick.Spending.Contracts/Expenses/ExpenseId.cs`. An identifier no other module carries stays in `Claimwick.Spending/Domain/Expenses/`.

**Reviewed for:** Does every business identifier live in the Contracts or Domain folder of the module owning its aggregate, and does every Kernel identifier have a decision record naming the Contracts cycle it breaks? Look at each identifier beside its aggregate record and the decision records.

### Compile Each Knowledge Area as a Model Project with Its Set Projects

**ID:** `litespec/packs/dotnet/architecture#compile-each-knowledge-area-as-a-model-project-with-its-set-projects`

**Strength:** required

**Applies to:** artifact: knowledge-model, knowledge-set

**Statement:** Each knowledge area MUST compile as one `<Product>.<Area>/` model project with one `<Product>.<Area>.<Variant>/` set project per variant under `src/Knowledge/`, with slice folders by subject mirrored in each set and host composition through `services.Add<Area>(x => x.Add<Variant>())`.

**Rationale:** One model project gives every module the same types and lookups for an area, and one set project per variant keeps each country or branch values apart without copying types into each set. Slice folders by subject, mirrored in each set, put the same subject in the same folder in the model and in every set. The host registers the sets it runs at startup, so a new variant touches the host and adds a set project while each module keeps pointing at the model.

**Example:** Claimwick travel area compiles as:

```text
src/Knowledge/Claimwick.Travel/                the model: row types, roles, keys, lookups, the registry, composition, validation
  PerDiem/  Mileage/                           one folder per subject
src/Knowledge/Claimwick.Travel.Germany/        one set per variant
  PerDiem/  Mileage/                           the same subjects
src/Knowledge/Claimwick.Travel.Netherlands/
```

Each host registers the sets it runs:

```csharp
services.AddTravel(x => x.Add<Germany>().Add<Netherlands>());
```

**Checked by:** `litespec model check` reports `code-placement-drift` for a folder under `src/Knowledge/` that is neither a `<Product>.<Area>/` model project nor a `<Product>.<Area>.<Variant>/` set project of a model the folder holds, and for a file directly in the Knowledge root. Review each host `Program.cs` for the `Add<Area>` call registering the sets the host runs, and each set for slice folders mirroring its model.

### Reference Knowledge Models from Modules and Sets from Hosts

**ID:** `litespec/packs/dotnet/architecture#reference-knowledge-models-from-modules-and-sets-from-hosts`

**Strength:** required

**Applies to:** artifact: knowledge-model, knowledge-set, command-handler, query-handler, reaction, job

**Statement:** Project references MUST run from modules to knowledge models, from knowledge sets to their model and the Kernel only, and from hosts to the sets they register.

**Rationale:** A module that points at a set ties itself to one country or branch, so modules point at the model and leave each set to the hosts that register it. A set that points past its model and the Kernel pulls operational code into per-variant values. Host registration is composition, so hosts point at the sets they run.

**Example:** Claimwick spending points at `Claimwick.Travel`, with country projects left to the hosts. `Claimwick.Travel.Germany` points at `Claimwick.Travel` and `Claimwick.Kernel`. The API host points at both country sets to register them.

**Checked by:** `litespec model check` reports `knowledge-reference-invalid` for a module project referencing a knowledge set project, for a knowledge set project referencing anything but its model and the Kernel, and for a knowledge model project referencing a module.

### Mirror Modules, Aggregates, and Use Cases in One Namespace Tree

**ID:** `litespec/packs/dotnet/architecture#mirror-modules-aggregates-and-use-cases-in-one-namespace-tree`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every type in the Kernel, Platform, and module projects MUST live in the namespace tree that starts with the product name and follows the folders below the project root.

**Rationale:** When the namespace matches the folder path, `Claimwick.Spending.Application.Expenses.SubmitExpense` states the product, module, layer, aggregate, and use case without a map. A reader moves from a Domain aggregate to its handler, its repository, and its endpoint by name alone, following folders under [Naming and Code Style](../../workspace/naming.md). Each project takes its own name as its root namespace, so every namespace still starts with the product name. The aggregate folder is plural, so the namespace never hides the aggregate type the way `...Application.Expense.SubmitExpense` hides `Expense`. This provision binds `litespec/core/backend-architecture#organize-code-by-module-aggregate-and-use-case` for .NET.

**Example:** The Expense aggregate of Claimwick's Spending module fills one branch:

```text
Modules/Claimwick.Spending/Domain/Expenses/Expense.cs
Modules/Claimwick.Spending/Application/Expenses/SubmitExpense/SubmitExpenseCommandHandler.cs
```

The handler in the tree above has namespace `Claimwick.Spending.Application.Expenses.SubmitExpense`.

**Reviewed for:** Does every type in the Kernel, Platform, and module projects carry the namespace its folder path implies? Compare the namespaces with the folders.

### Keep Host Endpoints in Hosts

**ID:** `litespec/packs/dotnet/architecture#keep-host-endpoints-in-hosts`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** Each host endpoint without a handler MUST live in its host project.

**Rationale:** Protocol endpoints, static assets, and development-only tooling belong to no use case folder, so the host keeps them. Each still carries its mark, and the placement check accepts a host endpoint in the host.

**Reviewed for:** Does every host endpoint without a handler live in its host project and carry its mark? Check the host endpoints beside the use case folders.

### Keep Business Values in the Kernel

**ID:** `litespec/packs/dotnet/architecture#keep-business-values-in-the-kernel`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The Kernel project MUST hold only the business values and the refusal exceptions every module uses, with no reference outside the base class library, grouped into `Domain/`, `Values/`, and `Auditing/` with no files at its root.

**Rationale:** Only the values every module speaks live here: money, typed identifiers, dates, the aggregate root, the domain exception, the refusal exceptions of product rules, and closed sets every module uses. The Kernel takes no package reference and no project reference, so a type a domain model may use is separated from a type only a host touches by the project boundary itself. A technical capability in the Kernel would drag its libraries into every module. `Domain/` holds the aggregate base, the domain event marker, and the domain exceptions; `Values/` holds money and the typed id marker; `Auditing/` holds the audit vocabulary.

**Example:**

```text
{Product}.Kernel/
  Domain/      AggregateRoot.cs  IDomainEvent.cs  DomainException.cs
  Values/      Money.cs  TypedId.cs
  Auditing/    the audit vocabulary
```

**Reviewed for:** Does the Kernel project hold only shared business values and the refusal exceptions of product rules, with no package reference and no project reference? Look at the Kernel project file and its files.

### Keep Technical Capabilities in the Platform

**ID:** `litespec/packs/dotnet/architecture#keep-technical-capabilities-in-the-platform`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The Platform project MUST hold only technical capabilities that name no module, referencing only the Kernel, its libraries, and the base class library.

**Rationale:** Persistence setup, messaging, authorization, HTTP conventions, country pack loading, and module composition are built once and stay generic. A module plugs in by contributing data, never by copying platform code. A business word in the Platform names a module the Platform never knows, and a module reference fails the build because the Platform project references no module project. Inside a capability past 20 files, one folder per sub-area groups it further, with each pack file beside the facts it reads.

**Example:**

```text
{Product}.Platform/
  Persistence/    store setup, serializer, Money and typed id converters, discriminator registry
  Messaging/      mediators, outbox, in-process dispatch, recurring jobs
  Access/         sessions, authorization guards, composable policies
                  (Permission, RecentSignIn, Self, System)
  Http/           endpoint discovery, problem details, refusal mapping, JSON
  CountryPacks/   pack loader and dated lookups
  Modules/        IModule contract and AddModules(...)
```

**Reviewed for:** Does any Platform file name a module, its aggregates, or its use cases, or does the Platform project reference a module project? Look at the Platform sources and its project references.

### Register Each Module Through Its Entry Point

**ID:** `litespec/packs/dotnet/architecture#register-each-module-through-its-entry-point`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each module MUST expose one public entry type implementing `IModule` that registers everything the module holds.

**Rationale:** The entry type is the module's only public surface beside its Contracts project: it registers the module's adapters, contributes its stored types, discriminators, policies, and routes, and gives hosts and scanners the assembly reference. Each host adds the modules that run in it through `AddModules`, so two hosts share wiring through the platform instead of through a folder beside the modules that reaches into module internals. Settled by [decision/0107-kernel-platform-and-one-project-per-module](decision/0107-kernel-platform-and-one-project-per-module).

**Example:** `SpendingModule` implements `IModule`, and each host program calls `AddPlatform()` and then `AddModules(...)` with the modules that run in that host.

**Reviewed for:** Does every module expose one public entry type implementing `IModule`, and does every host add its modules through `AddModules`? Look at each module project and each `Program.cs`.

### Hold Each Layer in Its Own Folder

**ID:** `litespec/packs/dotnet/architecture#hold-each-layer-in-its-own-folder`

**Strength:** required

**Applies to:** artifact: use-case

**Statement:** Each module MUST hold its Domain, Application, Ports, Infrastructure, and WebApi code in those folders, with one plural aggregate folder holding one folder per use case inside each layer that serves the use case, and one folder per module-level read directly in each layer that serves it.

**Rationale:** The folders make each layer visible at a glance: a reader opening a module sees its domain, its use cases, its ports, its adapters, and its endpoints in the shape every .NET backend uses. A use case spreads over one folder per layer that serves it, inside the plural folder of its aggregate, and every port of the module lives in its own `Ports/{Port}/` folder with its adapter in `Infrastructure/{Port}/`. The code mirrors the records, so a reader moves between a record folder and its code folder by the same names, and the plural keeps the aggregate type visible to every handler instead of hiding it behind a singular namespace that fails with CS0118. Settled by [decision/0107-kernel-platform-and-one-project-per-module](decision/0107-kernel-platform-and-one-project-per-module).

**Example:**

| Folder | Holds | Present when |
|---|---|---|
| `Domain/{Aggregates}/` | the aggregate with its act, part, fact, event, and refusal groupings | the module declares an aggregate |
| `Application/{Aggregates}/{UseCase}/` | the message, handler, validator, one definition class per concern, the result, the reaction or job that runs the use case | always |
| `Application/Settings/` | the module options class | the module binds a setting |
| `Ports/{Port}/` | the interface `I{Port}` with its signature types in `Models/` | the module declares a port |
| `WebApi/{Aggregates}/{UseCase}/` | the endpoint, its request and response models | the use case is reached over HTTP |
| `Infrastructure/Messaging/` | the module's message pipeline pieces | the module registers message pipeline pieces |
| `Infrastructure/Pipeline/` | the module's HTTP pipeline pieces serving no single use case | the module holds HTTP pipeline pieces |
| `Infrastructure/{Port}/` | the adapter implementing the module's port, with its options class, its wire types in `Models/`, and the collaborators it needs in `Parts/` | the module declares a port |

A module-level read drops the aggregate segment: `Application/{UseCase}/` and `WebApi/{UseCase}/`.

**Reviewed for:** Does every module hold its Domain, Application, Ports, Infrastructure, and WebApi code in those folders, with one plural aggregate folder holding one folder per use case inside each layer that serves it, one folder per module-level read directly in each layer that serves it, every port in its own `Ports/{Port}/` folder, and every adapter in its `Infrastructure/{Port}/` folder? Look at the module folders.

## Conventions

### Separate the Kernel, the Platform, and the Modules

**ID:** `litespec/packs/dotnet/architecture#separate-the-kernel-the-platform-and-the-modules`

**Strength:** default

**Applies to:** artifact: any

**Default:** Use a `{Product}.Kernel` project for the business values every module speaks, a `{Product}.Platform` project for the technical capabilities, one `{Product}.{Module}` project per module grouped under `Modules/`, one `{Product}.{Area}` knowledge model project per knowledge area with one `{Product}.{Area}.{Variant}` set project per variant grouped under `Knowledge/`, and WebApi and optional Worker hosts grouped under `Hosts/`, at every size.

**Replacement:** A project records its own project layout in its host documentation.

**Rationale:** Each module project then holds one full business capability: domain, use cases, ports, adapters, and endpoints in the module layer folders. The compiler draws the lines: the Kernel points at the base class library, the Platform skips module projects, and each module exposes its Contracts project and its entry type. Each knowledge model exposes its lookups to reading modules, with sets pointed at only by registering hosts. A project that groups every module into one modules project draws the Contracts line through `LS2020` instead. Settled by [decision/0107-kernel-platform-and-one-project-per-module](decision/0107-kernel-platform-and-one-project-per-module).

The names shorten earlier patterns to what each project holds; [Lineage](../../learn/lineage.md#kernel-platform-and-port) states the sources and the names set aside.

**Example:**

```text
apps/api/src/{Product}.Kernel/
apps/api/src/{Product}.Platform/
apps/api/src/Modules/{Product}.{Module}/              one project per module
apps/api/src/Modules/{Product}.{Module}.Contracts/    its sibling Contracts project
apps/api/src/Knowledge/{Product}.{Area}/              one model project per knowledge area
apps/api/src/Knowledge/{Product}.{Area}.{Variant}/    one set project per variant
apps/api/src/Hosts/{Product}.WebApi/
apps/api/src/Hosts/{Product}.Worker/     only when background work needs its own process
```

**Reviewed for:** Does the solution hold only the Kernel, the Platform, one project per module, the knowledge models with their sets, and their hosts, with the reference direction the dependency rules state? Check the solution file and the project references.

### Keep Composition in Hosts

**ID:** `litespec/packs/dotnet/architecture#keep-composition-in-hosts`

**Strength:** default

**Applies to:** artifact: any

**Default:** Compose each host in `Program.cs` and host registration modules: register transport services there and call `AddPlatform` and `AddModules`.

**Replacement:** A project names its registration layout in its host documentation.

**Rationale:** Domain and Application state abstractions for hosts to wire. A registration call inside either layer flips the dependency direction and drags Infrastructure into layers that stay clear of it. Each module registers its own types through its entry type, and `Program.cs` calls `AddPlatform()` and `AddModules(...)` in one visible order, so two hosts share wiring through the platform. This provision binds [Backend Architecture](../../core/backend-architecture.md) for .NET.

**Example:** The platform call, the modules call, and the endpoint map, in order:

```text
AddPlatform(...)     persistence, messaging, access, auditing, HTTP, country packs
AddModules(...)      one entry type per module that runs in this host
MapEndpoints(...)    the endpoints the platform discovered
```

`Program.cs` shows the call order. The platform owns endpoint discovery, Problem Details, refusal mapping, and JSON; the host owns transport security, authentication, authorization, and OpenAPI.

**Reviewed for:** Does every host compose its process in `Program.cs` and host registration modules through `AddPlatform` and `AddModules`? Check each `Program.cs`.

### Compile Module Contracts as Sibling Projects

**ID:** `litespec/packs/dotnet/architecture#compile-each-contracts-folder-as-its-own-project`

**Strength:** default

**Applies to:** artifact: any

**Default:** Compile each module's contracts as a sibling `{Product}.{Module}.Contracts` project beside the module project under `Modules/`, referencing only the Kernel and the Contracts projects whose identifiers its public types name, and let each module project reference the Contracts projects of the modules it reaches instead of their module projects.

**Replacement:** A project whose module dependencies run one way only references module projects directly and records that choice in its host documentation.

**Rationale:** Project references can't form a cycle, and neither can module dependencies. Contracts references, `queries` edges, `commands` edges, and reactions to facts form a directed acyclic graph, and a cycle means a concept is split across two modules. If module projects referenced each other, the first cross edge would fail the build.

A Contracts project depends on no module project, so any module can reference the Contracts of the modules below it, and module projects never reference each other. An identifier that two modules carry lives in the Contracts project of the module that owns its aggregate. It moves to the Kernel only with a decision record naming the Contracts cycle it breaks, under `litespec/packs/dotnet/architecture#keep-each-identifier-with-its-owner`.

A Contracts project holds no domain type. The publishing module maps a domain event to its fact itself, so no factory in the Contracts project names the domain event.

**Example:**

```text
Modules/Claimwick.Spending/
  Claimwick.Spending.csproj                 references Kernel, Platform, Claimwick.Spending.Contracts,
                                            Claimwick.Reporting.Contracts
  SpendingModule.cs
  Domain/  Application/  Ports/  Infrastructure/  WebApi/
Modules/Claimwick.Spending.Contracts/
  Claimwick.Spending.Contracts.csproj       references Kernel and the Contracts projects its public types name
  Expenses/
    ExpenseId.cs  OnExpenseReimbursed/ExpenseReimbursedModuleEvent.cs  ReadExpense/
```

The Contracts project sits beside the module project: a nested project would need the outer project to exclude its files item by item, would put its build output inside the module tree, and shows awkwardly in IDEs.

**Reviewed for:** Does any module project reference another module project, does any Contracts project reference a module project or name a domain type, and do the Contracts projects reference each other without a cycle? Check the project references of each module and Contracts project.
