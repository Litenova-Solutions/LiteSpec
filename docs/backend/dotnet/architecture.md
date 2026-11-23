# .NET Architecture

**Stack:** dotnet
**Concern:** application

Business code in a .NET backend lives in the Kernel, the Platform, one project per module, and one knowledge model project per knowledge area with its set projects, with hosts that compose and serve them. LiteSpec supports one architecture style: the only project choice is one project per module or one modules project. This page defines how the shared architecture rules apply to .NET: which projects exist, how modules, aggregates, and use cases fill one namespace tree, which folders each level allows, and where host-only endpoints live, so every type keeps one home and project references, folders, and marks hold the boundaries. The shared rules are stated on the [Backend Architecture](../architecture.md) page.

## Concepts

### The Kernel, the Platform, the Modules, and Their Hosts

A module is a business capability, and it is complete: its domain, its use cases, its own infrastructure, and its entry points live inside it. Nothing about a module lives outside it except one registration call in the hosts. One technical platform serves every module, and one tiny business kernel holds the values every module speaks. The compiler enforces the boundaries between them; analyzers enforce the conventions inside them.

```text
apps/api/src/
  {Product}.Kernel/                    business values, no dependencies
    Domain/                           aggregate base, domain event marker, domain exceptions
    Values/                           money, typed id marker
    Auditing/                         audit vocabulary

  {Product}.Platform/                  technical capabilities, no business words
    Persistence/                       store setup, serializer, Money and typed id converters,
                                       the discriminator registry modules contribute to
    Messaging/                         mediators, outbox, in-process dispatch, recurring jobs
    Access/                            sessions, authorization guards, composable policies
                                       (Permission, RecentSignIn, Self, System)
    Http/                              endpoint discovery, problem details, refusal mapping, JSON
    CountryPacks/                      pack loader and dated lookups
    Modules/                           IModule contract and AddModules(...)

  Knowledge/
    {Product}.Travel/                  one knowledge model project per area
      PerDiem/  Mileage/               one folder per subject
    {Product}.Travel.Germany/          one knowledge set project per variant
    {Product}.Travel.Netherlands/

  Modules/
    {Product}.Spending/                one project per module
      SpendingModule.cs                the only public entry point: registers everything below
      Domain/
        Expenses/                      Expense.cs, IExpenseRepository.cs, Submit/, Approve/, Parts/, Facts/, Events/, Exceptions/
        Policies/  ...
      Application/
        Expenses/
          SubmitExpense/
            SubmitExpenseCommand.cs
            SubmitExpenseCommandHandler.cs
            SubmitExpenseCommandValidator.cs
            SubmitExpenseCommandAuthorizationDefinition.cs
            SubmitExpenseCommandAuditDefinition.cs
            SubmitExpenseCommandIdempotencyDefinition.cs
            SubmitExpenseResult.cs
          ExportExpenses/
            query, handler, definitions, result
      Ports/
        ReceiptPdf/                    IReceiptPdf.cs with its signature types in Models/
      Infrastructure/
        Persistence/                   module-wide store setup, with one folder per aggregate
          Expenses/                    the expense repository, its stored documents and mapping
        ReceiptPdf/                    ReceiptPdfRenderer.cs, the adapter for Ports/ReceiptPdf/
      WebApi/
        Expenses/
          SubmitExpense/
            SubmitExpenseEndpoint.cs
            SubmitExpenseRequestModel.cs
            SubmitExpenseResponseModel.cs
          ExportExpenses/
            ExportExpensesEndpoint.cs
    {Product}.Spending.Contracts/      the module public surface beside its project: data only, grouped by aggregate
      Expenses/
        ExpenseId.cs                   the identifier, when another module carries it
        OnExpenseReimbursed/
          ExpenseReimbursedModuleEvent.cs   a fact another module consumes
        ReadExpense/                   a public query: ReadExpenseQuery and ReadExpenseResult
      ReadExpenseDirectory/            a public module-level query
    {Product}.Reporting/  ...
    {Product}.Reporting.Contracts/  ...

  Hosts/
    {Product}.WebApi/Program.cs        AddPlatform(); AddModules(Spending, Reporting, ...); map endpoints
    {Product}.Worker/Program.cs        the same modules, jobs and outbox only
```

`{Product}.Kernel` holds the business values every module speaks and references only the base class library. `{Product}.Platform` holds the technical capabilities with no business words and references the Kernel and its libraries, never a module. Each module project references the Kernel, the Platform, and the Contracts projects of the modules it reaches, and only its Contracts project and its `{Module}Module` entry type are public, so the compiler refuses any other cross-module reference. Each project takes its own name as its root namespace. Each module's contracts live in a sibling `{Product}.{Module}.Contracts` project beside the module project under `Modules/`, so module projects reference contracts projects and never each other. A Contracts project references the Kernel and the Contracts projects of the modules whose identifiers its public types name, and Contracts references never form a cycle. A project that keeps one modules project holding every module is an allowed deviation, with each module's contracts in a `Contracts/` folder inside its module folder; `LS2020` then enforces the Contracts boundary that project references otherwise enforce. Each knowledge area compiles as one `{Product}.{Area}` model project with one `{Product}.{Area}.{Variant}` set project per variant under `Knowledge/`: the model references the Kernel and its libraries, a set references its model and the Kernel, each module references the models of the areas it reads, and each host references the sets it registers.

The WebApi host holds `Program.cs` composition, and the Worker host exists only when background work needs its own process. Each `Program.cs` decides which modules run in that host and in what order, and nothing else: it calls `AddPlatform()`, then `AddModules(...)` with the modules of that host, then maps the endpoints the platform discovered. Two hosts share wiring through the platform's `AddModules`, never through a folder beside the modules.

The host also keeps the endpoints that have no use case folder to sit in: protocol endpoints such as sign-out and device callbacks, static assets such as scripts it serves, and development-only tooling. Each such endpoint still carries its mark.

### Knowledge Models and Their Sets

A knowledge area compiles as one model project with one set project per variant under `Knowledge/`, beside `Modules/`. The [Knowledge Level](../knowledge.md) page states what each holds: the model defines the row types, roles, keys, lookups, and validation of its area, and each set holds the dated rows of one variant. Claimwick's travel area is the `Claimwick.Travel` model with the `Claimwick.Travel.Germany` and `Claimwick.Travel.Netherlands` sets; the library product's loan area follows the same shape with one set per branch.

Slice folders inside both are by subject, mirrored in each set, so the model's `PerDiem/` folder and each set's `PerDiem/` folder hold the same subject. Each host registers the sets it runs with the model at startup:

```csharp
services.AddTravel(x => x.Add<Germany>().Add<Netherlands>());
```

A module references the model of each area it reads and names no set project. A set references its model and the Kernel, and nothing else.

### Why the Names Kernel, Platform, and Port

The two shared projects take their names from established patterns, each trimmed to what the project holds.

**Kernel** comes from the Shared Kernel pattern in Eric Evans's *Domain-Driven Design* (2003): a small part of the domain model that several parts of a system agree to share and change only together. Here the shared part is the business vocabulary every module speaks, such as money, typed identifiers, dates, the aggregate root, and the domain exception, so the same rule applies: it stays small, it holds no technical code, and a change to it is a change to every module. The word "Shared" is dropped because everything in the project is shared by definition, and because Evans's pattern names an agreement between bounded contexts, while a LiteSpec product is one bounded context whose modules share one vocabulary.

**Platform** names what Chris Richardson calls a microservice chassis (microservices.io, the Microservice Chassis pattern): the technical base that handles the cross-cutting concerns every service needs, such as persistence setup, messaging, security, auditing, HTTP conventions, and health, so each service holds only its business code. The modules of one product play the part of those services, and they plug into the base by contributing data to it. "Platform" is used instead of "Chassis" because it reads plainly in that sentence (a module contributes to the platform, a host calls `AddPlatform`), and because it matches the platform-team sense of a base other teams build on.

**Port** comes from Alistair Cockburn's "Hexagonal Architecture" (2005), also called Ports and Adapters ([alistair.cockburn.us](https://alistair.cockburn.us/hexagonal-architecture/)): the application talks to the outside only through purpose-named ports, and adapters convert between a port and a technology. The same idea appears as Fowler's Gateway and Separated Interface ([martinfowler.com](https://martinfowler.com/eaaCatalog/gateway.html)), as the core-owned interfaces of Palermo's Onion Architecture ([jeffreypalermo.com](https://jeffreypalermo.com/2008/07/the-onion-architecture-part-1/)), and as Gateways in Martin's Clean Architecture ([blog.cleancoder.com](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)). All rest on dependency inversion: the inner code owns the interface, the outer code implements it. "Port" is used instead of "Gateway" because a gateway fits an external system but less a file format or the clock, and instead of "Interfaces" or "Abstractions" because neither says why these interfaces are grouped. No interface lives in a Contracts project: a module reaches another module through the provider's Contracts and the mediator, never through a port of its own.

Other names were set aside:

| Name | Why not |
|:---|:---|
| `SharedKernel` | The full pattern name; "Shared" adds nothing and the pattern describes an agreement between bounded contexts. |
| `Core`, `Common`, `Shared` | Too vague: each invites technical and business code into one project, the mix the split exists to prevent. |
| `BuildingBlocks` | Common in modular monolith samples, where it holds both business and technical pieces. |
| `Primitives` | Accurate for money and identifiers, but it undersells the aggregate root and the domain exception. |
| `Infrastructure` | Collides with the `Infrastructure/` layer folder inside every module. |
| `Framework` | Suggests code reused across products, while the platform serves one product. |
| `Hosting`, `Runtime` | `Hosting` collides with `Hosts/`; `Runtime` fits messaging but not persistence setup or authorization. |
| `Chassis` | Kept as the pattern the platform follows, not as the project name. |
| `Gateway` | Fits an external system, but less a file format or the clock. |
| `Interfaces`, `Abstractions` | Say nothing about why these interfaces are grouped. |

### The Module Tree and Its Layers

Every node of the tree allows a closed set of folder names, so any other folder is refused. The layer of a file is its top folder in the module: a file under `Application/` is Application whatever use case folder holds it, and a file under `Ports/` is read as the ports layer.

A module holds its entry type at its root, then `Domain/`, `Application/`, `Ports/`, `Infrastructure/`, and `WebApi/`. Its public surface lives in the sibling `{Product}.{Module}.Contracts` project beside it. `Domain/` holds one folder per declared aggregate in the plural, whatever the module's number of aggregates and even when the aggregate's name equals the module's. Aggregates live under `Domain/{Aggregates}/`, and the root type keeps its singular name, so `Domain/Expenses/` holds the type `Expense`. `Application/` holds one folder per declared aggregate that has use cases, with one folder per use case inside it, plus one folder per declared module-level read, and the module options class in `Application/Settings/`. `Ports/` holds one folder per driven port of the module: `Ports/{Port}/` holds the interface `I{Port}` with the types its signatures name in `Models/`, with no files directly in `Ports/`, spelling for an external system the record name in PascalCase. `Infrastructure/` holds `Persistence/` with the module store contribution and one folder per aggregate holding its repository implementation, stored documents, and mapping, `Messaging/` with the module's message pipeline pieces, `Pipeline/` with the module's HTTP pipeline pieces serving no single use case, and one adapter folder per port the module implements, each named like its port folder with the adapter class, its options class, its wire types in `Models/`, and the collaborators it needs in `Parts/`. `WebApi/` holds the aggregate use case folders and module-level read folders of use cases reached over HTTP, with the endpoint and its request and response models. `Domain/` is absent when the module declares no aggregate, `WebApi/` when no use case is reached over HTTP, and `Ports/` when the module declares no port.

Everything that belongs to one aggregate sits in a folder named for the aggregate in the plural, and every layer of the product uses the same folder, so a reader follows one name from the record to the screen. A singular folder makes a namespace such as `Claimwick.Spending.Application.Expense`, and inside `...Application.Expense.SubmitExpense` the C# name lookup finds that namespace before the `Expense` type, so every handler naming its aggregate fails with CS0118. The same smell exists in a singular `Domain/Expense/` holding the type `Expense`. The plural also reads as the collection of the code of that aggregate, the common .NET feature-folder convention, and the record folders of the Claimwick example are already plural. The plural is computed from the name part of the aggregate id, in kebab case for records and the frontend and in PascalCase for .NET.

| The type is | It goes in |
|---|---|
| A business value every module uses | `{Product}.Kernel` |
| A technical capability with no business words | `{Product}.Platform` |
| An aggregate, its entities, values, events and refusals | `Modules/{Product}.{Module}/Domain/{Aggregates}/`, grouped into `{Act}/`, `Parts/`, `Facts/`, `Events/`, and `Exceptions/` |
| A use case's message, handler and definitions | `Modules/{Product}.{Module}/Application/{Aggregates}/{UseCase}/` |
| A use case's endpoint and HTTP models | `Modules/{Product}.{Module}/WebApi/{Aggregates}/{UseCase}/` |
| A driven port of the module | `Modules/{Product}.{Module}/Ports/{Port}/` holding `I{Port}` with its signature types in `Models/`, named for its external-system record or capability |
| The adapter implementing a port | `Modules/{Product}.{Module}/Infrastructure/{Port}/`, the same folder name as its port, with the adapter class, its options class, its wire types in `Models/`, and the collaborators it needs in `Parts/` |
| The module's message pipeline pieces | `Modules/{Product}.{Module}/Infrastructure/Messaging/` |
| The module's HTTP pipeline pieces serving no single use case | `Modules/{Product}.{Module}/Infrastructure/Pipeline/` |
| An aggregate's repository, stored documents and mapping | `Modules/{Product}.{Module}/Infrastructure/Persistence/{Aggregates}/` |
| Module-wide store setup | `Modules/{Product}.{Module}/Infrastructure/Persistence/` |
| The module options class | `Modules/{Product}.{Module}/Application/Settings/{Module}Options.cs` |
| A read model with its projections and reader | `Modules/{Product}.{Module}/Application/ReadModels/{ReadModels}/` |
| A type other modules use | `Modules/{Product}.{Module}.Contracts/{Aggregates}/` beside its aggregate (`On{Fact}/` for a fact), or directly in `Contracts/` for a public module-level query |
| Wiring that decides which modules run | `Hosts/{Host}/Program.cs` |

A module-level read, whose record sits under `modules/{module}/use-cases/`, drops the aggregate segment in each: `Application/{UseCase}/` and `WebApi/{UseCase}/`. Every command lives under the aggregate it changes.

A reaction's event handler and a job class sit in the `Application/` folder of the use case they run. One that runs no use case sits in the `Application/Notifications/` folder of the notification it sends, or in the `Application/ExternalSystems/` folder of the system it calls. A module keeps each read model in its `Application/ReadModels/` folder, and its public surface grouped by aggregate in its sibling Contracts project: each aggregate folder holds the identifier another module carries, the types facts or messages of more than one aggregate carry when the aggregate owns the concept, one `On{Fact}/` folder per fact another module consumes, and the public queries and public commands with their messages and results, so a reader sees at a glance what another module may reference.

A use case file takes its layer from its top folder: endpoints with their request and response models sit in `WebApi/{Aggregates}/{UseCase}/`, and the message, result, handler, validator, definitions, reactions, and job sit in `Application/{Aggregates}/{UseCase}/`. Port interfaces sit in `Ports/{Port}/`, with their adapters in `Infrastructure/{Port}/`. A typed identifier that another module carries lives directly in the aggregate folder of its owner's Contracts project, a type that facts or messages of more than one aggregate carry lives directly in the aggregate folder of the aggregate owning the concept, an identifier no other module carries lives in the aggregate's `Domain/{Aggregates}/` folder, and shared values live in the Kernel project. The platform `Http/` folder holds what endpoints of two or more modules compile against, such as the endpoint contract and the current actor.

The code tree mirrors the record tree. A use case record at `modules/{module}/aggregates/{aggregates}/use-cases/{use-case}` maps to code at `Application/{Aggregates}/{UseCase}/` and, when reached over HTTP, `WebApi/{Aggregates}/{UseCase}/`, and an aggregate record at `modules/{module}/aggregates/{aggregates}` maps to code at `Modules/{Product}.{Module}/Domain/{Aggregates}/`. A module-level read record at `modules/{module}/use-cases/{use-case}` drops the aggregate segment in every layer. Code drops the typed container folders of the record tree (`aggregates/`, `use-cases/`, `rules/`, `domain-events/`, `module-events/`, `integration-events/`) and keeps the module, aggregate, and record names in PascalCase, plural for aggregate folders. `Domain/{Aggregates}/Events/` is the stated exception: it groups the domain event types of one aggregate, which stay inside the module. `Domain/{Aggregates}/` groups into `{Act}/`, `Parts/`, `Facts/`, `Events/`, and `Exceptions/`; `Ports/{Port}/` groups into `Models/`, and `Infrastructure/{Port}/` groups into `Models/` and `Parts/`.

Every layer of the product uses the same plural aggregate folder:

| Layer | Path for the Expense aggregate and its Submit Expense use case |
|---|---|
| Records | `.litespec/behavior/modules/spending/aggregates/expenses/aggregate.yaml`, `.../aggregates/expenses/use-cases/submit-expense/` |
| Domain | `Domain/Expenses/` (the root type stays `Expense`) |
| Application | `Application/Expenses/SubmitExpense/` |
| Ports | `Ports/{Port}/` (one folder per driven port, such as `Ports/ReceiptPdf/`) |
| Infrastructure | `Infrastructure/Persistence/{Aggregates}/` and `Infrastructure/{Port}/` (one adapter folder per port, such as `Infrastructure/ReceiptPdf/`) |
| WebApi | `WebApi/Expenses/SubmitExpense/` |
| Acceptance tests | `{Product}.Acceptance.Tests/Spending/Expenses/SubmitExpense/` |
| Frontend | `lib/modules/spending/expenses/submit-expense/` |

Record ids stay singular (`aggregate/spending.expense`): an id names one thing, a folder holds its collection. HTTP routes already name collections in the plural (`/api/expenses/{expenseId}/approve`) and are not folders.

Models stay separate and flow one way: request model, command, aggregate, result, response model. The endpoint maps the request in and the result out, and the handler returns a result, never the aggregate.

### Visibility and Composition

A type stays visible only to the assemblies that need it. Only the Contracts project and the `{Module}Module` entry type are public; everything else in a module project is internal. Implementations stay `internal sealed`, endpoints stay internal while the platform discovers them, and a message or result turns public only inside the Contracts project. A Contracts project holds data only, so no port turns public anywhere: a module reaches another module through the provider's Contracts and the mediator, never through a port of its own.

Service registration sits in the hosts: each module registers its own types through its entry type, and the host calls `AddPlatform()` and `AddModules(...)` in one visible order.

## Standards

### Mirror Modules, Aggregates, and Use Cases in One Namespace Tree

**ID:** `litespec/backend/dotnet/architecture#mirror-modules-aggregates-and-use-cases-in-one-namespace-tree`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every type in the Kernel, Platform, and module projects MUST live in the namespace tree that starts with the product name and follows the folders below the project root.

**Rationale:** When the namespace matches the folder path, `Claimwick.Spending.Application.Expenses.SubmitExpense` states the product, module, layer, aggregate, and use case without a map. A reader moves from a Domain aggregate to its handler, its repository, and its endpoint by name alone, following folders under `litespec/workspace/naming#keep-namespaces-aligned-with-folders`. Each project takes its own name as its root namespace, so every namespace still starts with the product name. The aggregate folder is plural, so the namespace never hides the aggregate type the way `...Application.Expense.SubmitExpense` hides `Expense`. This provision binds `litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case` for .NET.

**Example:** The Expense aggregate of Claimwick's Spending module fills one branch:

```text
Modules/Claimwick.Spending/Domain/Expenses/Expense.cs
Modules/Claimwick.Spending/Application/Expenses/SubmitExpense/SubmitExpenseCommandHandler.cs
```

The handler in the tree above has namespace `Claimwick.Spending.Application.Expenses.SubmitExpense`.

**Reviewed for:** Does every type in the Kernel, Platform, and module projects carry the namespace its folder path implies? Compare the namespaces with the folders.

### Keep Closed Folder Sets at Every Level

**ID:** `litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each level of the Kernel, Platform, and module projects MUST hold only the folders the closed set table in this section allows.

**Rationale:** Only the folders the table lists are allowed, so a catch-all folder has no home and review refuses it. Product-wide values sit in the Kernel, product-wide technical capabilities sit in the Platform, and cross-module contracts sit in each module's Contracts project.

`litespec/backend/architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers` decides between a use case folder and the module and product folders. Module folders mirror record homes: each layer folder holds one plural aggregate folder with one folder per use case inside it, plus one folder per module-level read, each reaction and job sits with the use case, notification, or external system it runs, sends, or calls, and each read model sits in the `Application/ReadModels/` folder. Settled by [decision/0063-place-each-trigger-with-what-it-runs](decision/0063-place-each-trigger-with-what-it-runs).

A use case record at `modules/{module}/aggregates/{aggregates}/use-cases/{use-case}` maps to code at `Application/{Aggregates}/{UseCase}/` and, when reached over HTTP, `WebApi/{Aggregates}/{UseCase}/`, and an aggregate record at `modules/{module}/aggregates/{aggregates}` maps to code at `Modules/{Product}.{Module}/Domain/{Aggregates}/`. A module-level read record at `modules/{module}/use-cases/{use-case}` drops the aggregate segment in every layer. Code drops the typed container folders of the record tree (`aggregates/`, `use-cases/`, `rules/`, `domain-events/`, `module-events/`, `integration-events/`) and keeps the module, aggregate, and record names in PascalCase, plural for aggregate folders. `Domain/{Aggregates}/Events/` is the stated exception: it groups the domain event types of one aggregate, which stay inside the module. A project that keeps one modules project holding every module maps each module to one folder of that project instead.

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
| Module `Domain/{Aggregates}/` | the root's main file, the repository interface as the aggregate's only store interface, the root's partial files for a concern no single act owns (`{Root}.{Concern}.cs`), `{Act}/` per root action, `Parts/`, `Facts/`, `Events/`, `Exceptions/` |
| Module `Domain/{Aggregates}/{Act}/` | the partial file `{Root}.{Act}.cs`, the act's inputs, its private rules, and the steps only it calls; the name is the use case's act |
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
| Module `Infrastructure/{Port}/` | the adapter class, its options class `{Adapter}Options.cs`, `Models/`, and `Parts/` when the adapter needs collaborators; several adapters of one port sit side by side |
| Module `Infrastructure/{Port}/Models/` | the external system's wire types: requests, responses, file records |
| Module `Infrastructure/{Port}/Parts/` | the collaborators the adapter calls |
| Module `Infrastructure/Persistence/` | the module store contribution, and one folder per aggregate with a repository; no other files |
| Module `Infrastructure/Persistence/{Aggregates}/` | the aggregate's repository implementation, stored documents, and mapping |
| Module `WebApi/` | the aggregate use case folders and module-level read folders of use cases reached over HTTP |
| Module `Application/Notifications/` | one per notification a reaction or job of the module only sends |
| Module `Application/ExternalSystems/` | one per external system a reaction or job of the module only calls |
| Module `Application/ReadModels/` | one per declared read model of the module, in the plural |
| Each use case folder and each folder under `Notifications/`, `ExternalSystems/`, `ReadModels/`, and `Settings/` | Files only |

```text
{Product}.Kernel/
  Domain/      the aggregate base, the domain event marker, the domain exceptions
  Values/      money, the typed id marker
  Auditing/    the audit vocabulary
{Product}.Platform/
  Persistence/  Messaging/  Access/  Http/  CountryPacks/  Modules/
Modules/{Product}.Organizations/
  OrganizationsModule.cs
  Domain/
    Teams/             Team.cs  ITeamRepository.cs  Team.Members.cs  CreateTeam/  RemoveMember/  Parts/  Facts/  Events/  Exceptions/
  Application/
    Teams/
      CreateTeam/      CreateTeamCommand.cs  CreateTeamCommandResult.cs  CreateTeamCommandHandler.cs  CreateTeamCommandValidator.cs  CreateTeamCommandAuthorizationDefinition.cs  CreateTeamCommandAuditDefinition.cs  CreateTeamCommandIdempotencyDefinition.cs
      RemoveMember/    RemoveMemberCommand.cs  RemoveMemberCommandHandler.cs  RemoveMemberOnAccountClosedHandler.cs
    ReadTeamDirectory/        a module-level read: no aggregate folder
    Notifications/
      MemberRemoved/          a reaction that only sends this notification
    ReadModels/
      MemberAccounts/         a read model of another module's published facts
  Ports/
    {Port}/             I{Port} with its signature types in Models/; no files directly in Ports/
  Infrastructure/
    Persistence/       module-wide store setup, with one folder per aggregate
      Teams/           TeamRepository.cs with its stored documents and mapping
    Messaging/         the domain-to-module event mapping, audit writers
    Pipeline/          middleware, authorization conditions, authentication handlers
    {Port}/             the adapter implementing Ports/{Port}/, same folder name, with its wire types in Models/ and its collaborators in Parts/
  WebApi/
    Teams/
      CreateTeam/      CreateTeamEndpoint.cs  CreateTeamRequestModel.cs  CreateTeamResponseModel.cs
Modules/{Product}.Organizations.Contracts/
  Teams/
    TeamId.cs  OnTeamMemberAdded/TeamMemberAddedModuleEvent.cs  ReadTeam/  (ReadTeamQuery and ReadTeamResult)
  ReadTeamDirectory/  (a public module-level query)
```

**Checked by:** `litespec model check` reports `code-placement-drift` for a folder outside its closed set or naming no declared record, for a file directly at the Kernel or Platform root, for a file in the wrong layer folder of its use case, for a reaction or job outside the folder of what it runs, for an `{Aggregates}/` folder naming no declared aggregate, for a use case folder naming no use case of that aggregate, for a file directly in an `{Aggregates}/` folder, for a singular `Domain/{Aggregate}/` folder, for a Domain type directly in `Domain/` or a use case directly in `Application/` or `WebApi/` that belongs to an aggregate, for an exception file outside `Exceptions/` and a domain event file outside `Events/`, for a state file beside its aggregate root, for a second store interface of one aggregate, for a file directly in `Ports/`, for a port folder with no adapter folder in `Infrastructure/`, for an adapter folder with no port folder, for a type beside the adapter class outside `Models/` and `Parts/`, for a `Models/` or `Parts/` type no adapter file names, for a port or adapter folder naming a declared module, for a repository implementation outside its aggregate's `Persistence/` folder, for a use-case model or adapter file in `Persistence/`, for any other file directly in `Persistence/` outside the module store contribution, for an interface in a Contracts project, for a fact in a Contracts project outside its `{Aggregates}/On{Fact}/` folder, for an event file in an `On{Fact}/` folder that names no declared event or another fact, for a fact in an `On{Fact}/` folder outside the aggregate of the domain event it translates, for a command in a Contracts project outside the `{Aggregates}/{UseCase}/` folder of a public command, for a public use case message or result outside its `Contracts/{Aggregates}/{UseCase}/` path, for a test folder naming no declared record, and for a type whose namespace does not match its folder path through its layer segment, for a folder under `Knowledge/` that is neither a model nor a set of a model the folder holds, for a file directly in the Knowledge root, `folder-ungrouped` for a folder past 20 files that holds none of its allowed groupings, and `mark-folder-drift` for a marked element outside the folder its mark names.

### Keep Business Values in the Kernel

**ID:** `litespec/backend/dotnet/architecture#keep-business-values-in-the-kernel`

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

**ID:** `litespec/backend/dotnet/architecture#keep-technical-capabilities-in-the-platform`

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

### Name No Business Identifier in the Platform

**ID:** `litespec/backend/dotnet/architecture#name-no-business-identifier-in-the-platform`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The Platform MUST name no business identifier.

**Rationale:** The Platform speaks the tenant and the actor: the execution identity tenant and the principal subject. A business identifier in the Platform names an aggregate of a module the Platform never knows, and every module then converts through the Platform's copy instead of the owner's. The owner of each identifier converts at the module edge, from the tenant or subject into its identifier and back, so the Platform stays free of business words. Settled by [decision/0109-module-contracts-publish-data](decision/0109-module-contracts-publish-data).

**Example:** The workspace module converts the tenant string into `WorkspaceId` at its edge and back when it answers; the Platform names only the tenant.

**Reviewed for:** Does any Platform file name a module identifier? Look at the Platform sources beside the module Contracts projects.

### Register Each Module Through Its Entry Point

**ID:** `litespec/backend/dotnet/architecture#register-each-module-through-its-entry-point`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each module MUST expose one public entry type implementing `IModule` that registers everything the module holds.

**Rationale:** The entry type is the module's only public surface beside its Contracts project: it registers the module's adapters, contributes its stored types, discriminators, policies, and routes, and gives hosts and scanners the assembly reference. Each host adds the modules that run in it through `AddModules`, so two hosts share wiring through the platform instead of through a folder beside the modules that reaches into module internals. Settled by [decision/0107-kernel-platform-and-one-project-per-module](decision/0107-kernel-platform-and-one-project-per-module).

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

### Hold Each Layer in Its Own Folder

**ID:** `litespec/backend/dotnet/architecture#hold-each-layer-in-its-own-folder`

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

### Keep Application Types in Their Layer Folders

**ID:** `litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An application MUST place each use case's messages, results, handlers, validators, definitions, and triggers in the use case's folder inside `Application/`, every port of the module in its own `Ports/{Port}/` folder holding the interface `I{Port}` with its signature types in `Models/` and no files directly in `Ports/`, each port's adapter in its `Infrastructure/{Port}/` folder with the same folder name, holding the adapter class with its options class `{Adapter}Options.cs`, its wire types in `Models/`, and the collaborators it needs in `Parts/`, and cross-module contracts grouped by aggregate in its Contracts project.

**Rationale:** One use case keeps its command, query, result, handler, validator, definitions, and triggers in its `Application/{Aggregates}/{UseCase}/` folder, and its endpoint and transport models in its `WebApi/{Aggregates}/{UseCase}/` folder; a module-level read drops the aggregate segment in each. A use case's own models never move up: two use cases with one shape each declare it, under `litespec/backend/architecture#give-each-use-case-its-own-models`.

Rules used by two or more use cases of one aggregate sit in the aggregate `Domain/{Aggregates}/` folder. Every port of the module sits in its own `Ports/{Port}/` folder, whether one use case or many call it, so `Ports/` is the complete list of what the module needs from outside: `Ports/{Port}/` holds the interface `I{Port}` with the types its member signatures name in `Models/`, with no files directly in `Ports/`, and each port's adapter sits in its `Infrastructure/{Port}/` folder with the same folder name, holding the adapter class with its options class, its wire types in `Models/`, and the collaborators it needs in `Parts/`; several adapters of one port sit side by side. Whether an interface is a port at all is a judgment the reviewer answers under `litespec/backend/application#declare-a-port-only-for-what-the-module-does-not-compute`. A module publishes data only in its Contracts project, grouped by aggregate: each `Contracts/{Aggregates}/` folder holds the identifier another module carries, one `On{Fact}/` folder per fact another module consumes, and one folder per public query or public command with its message and result, so the public surface of a module stays visible in its Contracts project. It publishes no refusal and no interface.

**Checked by:** `litespec model check` reports `code-placement-drift` for a port folder holding no interface or more than one, for a port folder whose name is not its interface name without the `I`, for a type in a port folder outside `Models/` that the interface signatures do not name, for a type beside the adapter class outside `Models/` and `Parts/`, and for a `Models/` or `Parts/` type no adapter file names, and `dotnet build` reports `LS2018` for a `Ports/` file naming an `Application/`, `Infrastructure/` or `WebApi/` type.

### Place Each Read Model in Its Application Read Models Folder

**ID:** `litespec/backend/dotnet/architecture#place-each-read-model-in-its-application-read-models-folder`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each read model MUST live in a folder named for the read model in the plural inside its `Application/ReadModels/` folder, holding the read-model document, one projection per source event named `{ReadModel}On{Fact}Projection`, the reader interface `I{ReadModel}ReadModel` and its implementation, each carrying the read model's mark.

**Rationale:** A read model's projections answer module events and integration events like reactions, but they write only their read model and run no use case. Placed with a use case, they would look like reactions; placed with the read model, one folder shows every event that fills it and the one reader its module's use cases call.

The mark links each class to the record that names its sources. A projection that wrote anything but its read model would be an unrecorded reaction. This provision binds `litespec/backend/application#read-another-modules-state-through-a-read-model` for .NET.

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

**ID:** `litespec/backend/dotnet/architecture#keep-implementation-types-internal`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler, validator, reaction, repository, endpoint

**Statement:** A handler, validator, definition, persistence implementation, or endpoint implementation MUST be declared `internal sealed`.

**Rationale:** A public type is visible to every assembly, so an implementation left public can be referenced by callers it was never written for. Messages, results, and ports turn public only inside the Contracts project, while handlers, validators, and implementations stay `internal sealed`. Assembly scanning reads internal types through the module entry type, so scanning never justifies widening visibility. This provision binds `litespec/backend/architecture#keep-implementations-hidden-and-closed` for .NET.

**Checked by:** `dotnet build` reports `LS2001` for an implementation type that is not `internal sealed`.

### Expose Only Contracts and the Entry Point

**ID:** `litespec/backend/dotnet/architecture#expose-only-contracts-and-the-entry-point`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every type in a module project outside its module entry type MUST be internal.

**Rationale:** With one project per module, visibility is the module boundary: a public Contracts project and one public entry type, everything else internal, so no module names another module's internals and the compiler refuses the reference. Endpoints stay internal while the platform discovers them in every module assembly the host adds. A project that keeps one modules project holding every module keeps the same visibility inside each module folder, with `LS2020` as the boundary.

**Reviewed for:** Is any type in a module project outside the entry type public? Look at the visibility modifiers in each module project.

### Follow Layer Direction in Every File

**ID:** `litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A type MUST reference only the layers its own layer allows under the layer table of [Backend Architecture](../architecture.md#the-four-layers).

**Rationale:** Layers are roles played by folders, whatever project holds them: a file takes its layer from its top folder in the module. Global usings import namespaces for every file, so the rule covers them too: a global using hides no coupling. The dependency rules below bind the layer table to projects and folders. This provision binds `litespec/backend/architecture#point-dependencies-inward` for .NET.

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

**Example:** Domain references no Application, Infrastructure, or WebApi type, and none of the persistence, web, mediator, logging, and injection namespaces. Ports references no Application, Infrastructure, or WebApi type. Application references no Infrastructure or WebApi type, no ASP.NET Core type, and no other module's public command. A handler, reaction, or job names another module's public query or query result only with a declared `queries` edge, and only a reaction or job names another module's public command or command result. WebApi references no Domain type outside typed identifiers and Kernel values, and no repository. An adapter in `Infrastructure/{Port}/` names no Domain type outside typed identifiers, shared-kernel values and the types the interfaces of the ports it implements name in their member signatures, and no WebApi type. A knowledge set references its model and the Kernel, and a module references the model of each knowledge area it reads.

**Checked by:** `dotnet build` reports `LS1001` for a Domain type naming an outer-layer type, `LS2018` for an Application, Ports, or WebApi type naming a type outside its layer, `LS2025` for an adapter in `Infrastructure/{Port}/` naming a Domain type outside those or a WebApi type, and `LS2029` for another module's public query or query result named outside a handler, reaction, or job, and for another module's public command or command result named outside a reaction or job.

### Keep Use Case Folders Isolated

**ID:** `litespec/backend/dotnet/architecture#keep-use-case-folders-isolated`

**Strength:** required

**Applies to:** artifact: use-case, command-handler, query-handler, validator, endpoint, reaction

**Statement:** The folders of one use case MUST reference no type in the folders of another use case.

**Rationale:** The folders of one use case hold that use case, so a reference between the folders of two use cases couples two behaviors that change for different reasons. A reaction or job that runs a use case sits in that use case's own `Application/` folder, so it references the message from inside, and no handler sends a command: another module's public query is the one message a handler sends, with a declared `queries` edge.

A shape two use cases need is declared in each, under `litespec/backend/architecture#give-each-use-case-its-own-models`. Behavior they share belongs in the module or product folder that holds every consumer, under `litespec/backend/architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers`. The rule applies to files and to global usings alike.

Kernel values are referenced directly, never copied: money, typed identifiers, dates, and closed sets the Kernel owns are referenced from any layer the layer table allows, and WebApi models carry them on the wire. Copying a Kernel value into a use case folder breaks the rule.

The module's shared `Application/` folders, `Notifications/`, `ExternalSystems/`, `ReadModels/`, and `Settings/`, belong to no use case: a use case folder names the types there freely, and the rule reports neither a name from a use case folder to one of them nor a name from one of them to a use case folder.

**Checked by:** `dotnet build` reports `LS2019` for a use case folder naming a type in another use case folder.

### Reach Another Module Only Through Contracts

**ID:** `litespec/backend/dotnet/architecture#reach-another-module-only-through-contracts`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A module MUST reach another module only through types its Contracts project publishes.

**Rationale:** The Contracts project is the public surface of a module, grouped by aggregate: each `Contracts/{Aggregates}/` folder holds the identifier another module carries, the types facts or messages of more than one aggregate carry when the aggregate owns the concept, one `On{Fact}/` folder per fact another module consumes, and one folder per public query or public command with its message and result. Each layer names only what it needs from another module's Contracts: identifiers in `Domain/`, `Ports/`, `Application/`, and `WebApi/`; facts an `Application/` reaction answers; a handler, reaction, or job names the provider's public queries and query results with a declared `queries` edge, and only a reaction or job names the provider's public commands and command results with a declared `commands` edge.

With one project per module, project references and visibility enforce the boundary: only the Contracts project and the entry type are public, so a module cannot name another module's internals and the compiler refuses the reference. A project that keeps one modules project holding every module enforces the same boundary through `LS2020`.

A module publishes no refusal and no interface: another module triggers its acts by sending its public commands upstream from its reactions and jobs, and each module refuses with its own refusals. A module that needs another module's current value asks the provider's public query from its handler, reaction, or job with a declared `queries` edge; a module that needs another module's data reshaped and kept locally keeps its own read model built from the owner's module events. It loads no foreign aggregate or repository. A reaction to another module's fact subscribes to the published module event, never to the domain event, and answers upstream facts only.

**Checked by:** `dotnet build` reports `LS2020` for a module reference naming another module type outside its Contracts.

### Publish Only Data from Each Contracts Project

**ID:** `litespec/backend/dotnet/architecture#publish-only-data-from-each-contracts-project`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each Contracts project MUST publish only data grouped by aggregate, with no interface anywhere in the project.

**Rationale:** The project is the module boundary: what sits in it is what other modules may name. Data states what crosses; an interface states a call shape the provider owns and every consumer couples to. The root holds one folder per aggregate with anything public, in the plural, and one folder per public module-level query, with no files. Each `Contracts/{Aggregates}/` folder holds the aggregate identifier when another module carries it, the types facts or messages of more than one aggregate carry when the aggregate owns the concept, one `On{Fact}/` folder per fact about the aggregate another module consumes, and one folder per public query or public command of that aggregate with its message and result at the same path the use case has in the provider's `Application/` folder. Each fact sits in its own `On{Fact}/` folder in the aggregate whose domain event it translates, beside the aggregate's public query and command folders, as its module or integration event record's `translates` names, with namespace `{Product}.{Module}.Contracts.{Aggregates}.On{Fact}`. `{Fact}` is the record name in PascalCase, so `module-event/spending.expense-reimbursed` gives `OnExpenseReimbursed`, and the record tree maps one to one: `aggregates/expenses/module-events/expense-reimbursed/` gives `Expenses/OnExpenseReimbursed/`. The folder holds the event type and the types only it carries. A module-level public query sits in its own folder directly in `Contracts/`. Names are business names: `ReadBusinessQuery` and `BusinessResult`, `CloseBusinessCommand` and its result, never request or response models, routes, or HTTP methods. Settled by [decision/0109-module-contracts-publish-data](decision/0109-module-contracts-publish-data).

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

**ID:** `litespec/backend/dotnet/architecture#keep-each-identifier-with-its-owner`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A business identifier MUST live in the project of the module that owns its aggregate, in `Contracts/{Aggregates}/` when another module carries it and in `Domain/{Aggregates}/` otherwise, except in the Kernel with a decision record naming the Contracts cycle it breaks.

**Rationale:** The identifier names its aggregate, so it lives where its aggregate lives: in the owner's Contracts project when another module carries it, beside the `On{Fact}/` folders and the public queries and public commands of its aggregate, and in the aggregate's `Domain/{Aggregates}/` folder when no other module carries it. An identifier placed by convenience instead drifts: a copy in the consumer, a shared copy in the Kernel, and the owner holding a third. The Kernel holds an identifier only to break a cycle between Contracts projects, and only with a decision record naming that cycle, because a shared identifier hides which module owns the aggregate it names. The marker generic technical code uses for converters lives in the Kernel. Settled by [decision/0109-module-contracts-publish-data](decision/0109-module-contracts-publish-data).

**Example:** `ExpenseId` belongs to `aggregate/spending.expense`. The reporting module carries it, so it lives in `Claimwick.Spending.Contracts/Expenses/ExpenseId.cs`. An identifier no other module carries stays in `Claimwick.Spending/Domain/Expenses/`.

**Reviewed for:** Does every business identifier live in the Contracts or Domain folder of the module owning its aggregate, and does every Kernel identifier have a decision record naming the Contracts cycle it breaks? Look at each identifier beside its aggregate record and the decision records.

### Follow Declared Module Dependencies

**ID:** `litespec/backend/dotnet/architecture#follow-declared-module-dependencies`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A module MUST reference only modules its record names in dependsOn.

**Rationale:** The `dependsOn` list states which modules a module may reference. An undeclared edge crosses a boundary the design does not allow. The rule covers code references and record links alike: Contracts project references, `queries` edges, `commands` edges, and reactions to another module's facts. The dependency graph stays acyclic: a cycle means a concept is split across two modules, and the fix moves the concept, under `litespec/core/model#keep-module-dependencies-free-of-cycles`. Settled by [decision/0110-module-dependencies-form-a-dag](decision/0110-module-dependencies-form-a-dag).

**Reviewed for:** Does every cross-module reference name a module its owner declares in `dependsOn`, with no dependency cycle? Check the cross-module references beside the module records.

### Compile Each Knowledge Area as a Model Project with Its Set Projects

**ID:** `litespec/backend/dotnet/architecture#compile-each-knowledge-area-as-a-model-project-with-its-set-projects`

**Strength:** required

**Applies to:** artifact: knowledge-model, knowledge-set

**Statement:** Each knowledge area MUST compile as one `<Product>.<Area>/` model project with one `<Product>.<Area>.<Variant>/` set project per variant under `src/Knowledge/`, with slice folders by subject mirrored in each set and host composition through `services.Add<Area>(x => x.Add<Variant>())`.

**Rationale:** One model project gives every module the same types and lookups for an area, and one set project per variant keeps the values of each country or branch apart without copying the types into each set. Slice folders by subject, mirrored in each set, put the same subject in the same folder in the model and in every set, so a reader moves from a row type to its rows by folder name. The host registers the sets it runs with the model at startup, so adding a variant touches the host and adds a set project, while each module keeps referencing the model. This provision binds `litespec/backend/knowledge#give-each-knowledge-area-one-model` for .NET.

**Example:** Claimwick's travel area compiles as:

```text
src/Knowledge/Claimwick.Travel/                the model: row types, roles, keys, lookups, validation
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

**ID:** `litespec/backend/dotnet/architecture#reference-knowledge-models-from-modules-and-sets-from-hosts`

**Strength:** required

**Applies to:** artifact: knowledge-model, knowledge-set, command-handler, query-handler, reaction, job

**Statement:** Project references MUST run from modules to knowledge models, from knowledge sets to their model and the Kernel only, and from hosts to the sets they register.

**Rationale:** A module that references a set binds itself to one country or branch, so a module references the model and leaves each set to the hosts that register it: the model gives modules one stable surface across every variant. A set that references anything but its model and the Kernel pulls operational or technical code into values that ship per variant. A host references the sets it runs because registration is composition, which lives in hosts. This provision binds `litespec/backend/knowledge#read-knowledge-through-its-model` for .NET.

**Example:** Claimwick's spending module references `Claimwick.Travel` and names no country project. `Claimwick.Travel.Germany` references `Claimwick.Travel` and `Claimwick.Kernel`. The API host references both country sets to register them.

**Checked by:** `litespec model check` reports `knowledge-reference-invalid` for a module project referencing a knowledge set project, for a knowledge set project referencing anything but its model and the Kernel, and for a knowledge model project referencing a module.

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

**Default:** Use a `{Product}.Kernel` project for the business values every module speaks, a `{Product}.Platform` project for the technical capabilities, one `{Product}.{Module}` project per module grouped under `Modules/`, one `{Product}.{Area}` knowledge model project per knowledge area with one `{Product}.{Area}.{Variant}` set project per variant grouped under `Knowledge/`, and WebApi and optional Worker hosts grouped under `Hosts/`, at every size.

**Replacement:** A project records its own project layout in its host documentation.

**Rationale:** Each module project then holds one complete business capability: a reader opening it sees the module's domain, use cases, ports, adapters, and endpoints in the module's layer folders. The compiler enforces the boundaries: the Kernel references only the base class library, the Platform references no module, and each module exposes only its Contracts project and its entry type. Each knowledge model exposes its lookups to the modules that read it, and each set is referenced only by the hosts that register it. Hosts stay separate projects, because two hosts can run the same modules. The names come from the Shared Kernel pattern of domain-driven design and the microservice chassis pattern, as "Why the Names Kernel and Platform" above explains.

A project that keeps one modules project holding every module is an allowed deviation; `LS2020` then enforces the Contracts boundary that project references otherwise enforce. Settled by [decision/0107-kernel-platform-and-one-project-per-module](decision/0107-kernel-platform-and-one-project-per-module).

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

### Compile Module Contracts as Sibling Projects

**ID:** `litespec/backend/dotnet/architecture#compile-each-contracts-folder-as-its-own-project`

**Strength:** default

**Applies to:** artifact: any

**Default:** Compile each module's contracts as a sibling `{Product}.{Module}.Contracts` project beside the module project under `Modules/`, referencing only the Kernel and the Contracts projects whose identifiers its public types name, and let each module project reference the Contracts projects of the modules it reaches instead of their module projects.

**Replacement:** A project whose module dependencies run one way only references module projects directly and records that choice in its host documentation.

**Rationale:** Project references cannot form a cycle, and neither can module dependencies: Contracts references, `queries` edges, `commands` edges, and reactions to facts form a directed acyclic graph, and a cycle means a concept is split across two modules. With one project per module and module projects referencing each other, the first cross edge fails the build. A Contracts project depends on no module project, so any module can reference the Contracts of the modules below it, and the module projects never reference each other. An identifier that two modules carry lives in the Contracts project of the module that owns its aggregate; it moves to the Kernel only with a decision record naming the Contracts cycle it breaks, under `litespec/backend/dotnet/architecture#keep-each-identifier-with-its-owner`. A Contracts project holds no domain type: a fact that a domain event produces is mapped inside the publishing module, never by a factory in the Contracts project that names the domain event.

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
