# .NET Architecture

**Stack:** dotnet

**Layer:** application

## Intent

A .NET backend holds its business code in two projects, the shared kernel and the modules, with hosts that compose and serve them. This page binds that shape for .NET: which projects exist, how modules aggregates and use cases fill one namespace tree, which folders each level allows, and where host-only endpoints live, so every type keeps one home and boundaries hold by folder and mark. The shared architecture rules live on [Backend Architecture](../architecture.md); this page holds only their .NET bindings.

## Concepts

### The Shared Kernel, the Modules and Their Hosts

The business code lives in two projects. `{Product}.Modules` holds the modules and nothing else: each module with its aggregates, use cases, reactions and jobs, plus a `Composition/` folder for what names several modules at once, such as the module registration and the published event catalog. `{Product}.SharedKernel` holds what every module builds on and names no module: shared domain values, the command pipeline positions, store and outbox plumbing, and what endpoints of two or more modules compile against, such as `IEndpoint` and the current actor. Modules references the shared kernel, and the shared kernel references no module. Both projects take the product name as their root namespace, so a module type reads `{Product}.Organizations.Teams.UseCases.CreateTeam` and a shared kernel type reads `{Product}.Domain`.

The WebApi host holds Program composition and HTTP plumbing, and the Worker host exists only when background work needs its own process. Endpoints live in their use case folders and the host maps them through the one public assembly marker. Problem Details, authentication, OpenAPI and rate limiting stay in the host.

The host also keeps the endpoints that have no handler to sit beside: protocol endpoints such as sign-out and device callbacks, static assets such as scripts it serves, and development-only tooling. Each such endpoint still carries its mark.

### The Module Tree and Its Layers

Every node of the tree may hold the same layer folders. Each level allows a closed set of folder names, so any other folder is refused. Inside a layer folder subfolders stay free, because the folder already states the layer and the lowest-folder rule already placed the type.

An aggregate keeps its use cases in its `UseCases/` folder, one folder per use case, and a module keeps the use cases no single aggregate owns in its own `UseCases/` folder, beside one `On{Fact}/` folder per reaction. A module `Contracts/` folder sorts its public surface by kind: `Identifiers/`, `Exceptions/`, `IntegrationEvents/`, `Commands/` and `Ports/`, so a reader sees at a glance what another module may name.

A use case file takes its layer from its role suffix: Endpoint RequestModel and ResponseModel are WebApi, while Command Query Result Handler and Validator are Application. A typed identifier that another module names lives in its owner `Contracts/Identifiers/` folder, and shared values live in the shared kernel `Domain/` folder. The shared kernel `WebApi/` folder holds only what endpoints of two or more modules compile against, while product-wide HTTP plumbing stays in the host.

A module with one aggregate of its own name stays flat. Models stay separate and flow one way: request model, command, aggregate, result, response model. The endpoint maps in and out, and the handler returns a result and never the aggregate.

### Visibility and Composition

A type stays visible only to the assemblies that need it: implementations stay internal sealed, and a message port or marker turns public only when another assembly names it. Service registration lives in the hosts, with each module registering its own Infrastructure types in its module Infrastructure folder for the host to call in one visible order.

## Standards

### Mirror Modules Aggregates and Use Cases in One Namespace Tree

**ID:** `litespec/backend/dotnet/architecture#mirror-modules-aggregates-and-use-cases-in-one-namespace-tree`

**Strength:** rule

**Statement:** Every type in the shared kernel and modules projects MUST live in the namespace tree that starts with the product name and follows the folders below the project root.

**Rationale:** The namespace reads like the qualified identifier, so `Claimwick.Spending.Expenses.UseCases.SubmitExpense` names the product module aggregate and use case without a map. Namespaces follow folders under `litespec/workspace/naming#keep-namespaces-aligned-with-folders`, so a reader moves from a Domain aggregate to its handler its repository and its endpoint by name alone. This provision binds `litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case` for .NET.

**Example:** The Expenses aggregate of the Spending module fills one branch:

```text
Claimwick.Modules/Spending/Expenses/Domain/Expense.cs
Claimwick.Modules/Spending/Expenses/UseCases/SubmitExpense/SubmitExpenseCommandHandler.cs
```

The handler above lives in namespace `Claimwick.Spending.Expenses.UseCases.SubmitExpense`.

**Reviewed for:** Which types miss the product module aggregate use case namespace or name one outside its folder? Look at the namespaces beside the folders in the shared kernel and modules projects.

### Keep Closed Folder Sets at Every Level

**ID:** `litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level`

**Strength:** rule

**Statement:** Each level of the shared kernel and modules projects MUST hold only the folders the closed set table in this section allows.

**Rationale:** A closed set refuses the catch-all folder by construction: anything outside the table has no home and review refuses it. Product-wide plumbing lives in the product layer folders and cross-module contracts in each module Contracts folder, while `litespec/backend/architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers` decides between a use case folder and the aggregate module and product layer folders. Module folders mirror record homes: use cases sit in `UseCases/` folders, reactions in `On{Fact}/` folders beside the module use cases, and jobs in the module `Jobs/` folder.

**Example:**

| Level | Allowed folders |
|:---|:---|
| Shared kernel project root | `Domain/`, `Application/`, `Infrastructure/`, `WebApi/` |
| Modules project root | `Composition/`, one per declared module |
| Module | `Contracts/`, `Domain/`, `Application/`, `Infrastructure/`, `WebApi/`, `UseCases/`, `Jobs/`, one per declared aggregate |
| Module `Contracts/` | `Identifiers/`, `Exceptions/`, `IntegrationEvents/`, `Commands/`, `Ports/` |
| Module `UseCases/` | one per declared module use case, one `On{Fact}/` per reaction |
| Aggregate | `Domain/`, `Application/`, `Infrastructure/`, `WebApi/`, `UseCases/` |
| Aggregate `UseCases/` | one per declared use case |
| Use case, `On{Fact}/` | Files only |

```text
{Product}.SharedKernel/
  Domain/  Application/  Infrastructure/  WebApi/
{Product}.Modules/
  Composition/
  Organizations/
    Contracts/
      Identifiers/  Exceptions/  IntegrationEvents/  Commands/  Ports/
    Domain/  Application/  Infrastructure/  WebApi/
    Teams/
      Domain/          Team.cs  TeamState.cs  ITeamRepository.cs  Events/  Exceptions/
      Application/     types used by two or more Team use cases
      Infrastructure/  TeamRepository.cs
      WebApi/          transport types used by two or more Team use cases
      UseCases/
        CreateTeam/    use case, flat: Command, Result, Handler, Validator, Endpoint, RequestModel, ResponseModel
    UseCases/
      ReadTeamDirectory/        a use case no single aggregate owns
      OnTeamMemberRemoved/      a reaction
    Jobs/
```

**Checked by:** `litespec model check` reports `mark-folder-drift` for a folder outside its closed set or naming no declared record.

### Keep Application Types in Their Layer Folders

**ID:** `litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders`

**Strength:** rule

**Statement:** An application MUST place commands queries results handlers validators and module ports in Application folders and cross-module contracts in the sorted kind folders of its Contracts folder.

**Rationale:** One use case stays flat in its folder, with its command query result handler validator endpoint and transport models side by side. Types used by two or more use cases of one aggregate live in the aggregate Application folder, types used by two or more aggregates in the module Application folder, and product-wide plumbing in the shared kernel Application folder. A module publishes its typed identifiers in `Contracts/Identifiers/`, its refusals in `Contracts/Exceptions/`, its integration events in `Contracts/IntegrationEvents/` and its cross-module ports in `Contracts/Ports/`, so the public surface of a module stays visible at the module.

**Reviewed for:** Which commands queries results handlers validators or ports sit outside their Application folder or their Contracts kind folder? Look at the Application and Contracts folders beside the use case folders.

### Keep Implementation Types Internal

**ID:** `litespec/backend/dotnet/architecture#keep-implementation-types-internal`

**Strength:** rule

**Statement:** A handler, validator, definition, persistence implementation, or endpoint implementation MUST be declared `internal sealed`.

**Rationale:** A public type is an offer to every assembly, so an implementation left public invites callers it was never written for. Messages, results, markers, and ports stay public only when another assembly uses them; handlers, validators, and implementations stay `internal sealed`. Assembly scanning reads internal types through the one public marker, so it never justifies widening visibility.

This provision binds `litespec/backend/architecture#keep-implementation-types-hidden` for .NET.

**Checked by:** `dotnet build` reports `LS2001` for an implementation type that is not `internal sealed`.

### Follow Layer Direction in Every File

**ID:** `litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file`

**Strength:** rule

**Statement:** A type MUST reference only the layers its own layer allows under the layer table in this section.

**Rationale:** Layers are roles played by folders and use case files, whatever project holds them. A file takes its layer from its folder and, inside a use case folder, from its role suffix. Global usings import namespaces for every file, so the rule reads them too and they hide coupling from nothing. This provision binds `litespec/backend/architecture#point-dependencies-inward` for .NET.

**Example:**

| Layer | Holds | May reference |
|:---|:---|:---|
| Domain | Aggregates, state records, value objects, domain events, domain exceptions, repository contracts | Domain |
| Application | Commands, queries, results, handlers, validators, ports, pipeline positions | Domain, Application |
| Infrastructure | Repositories, adapters, mappings, module registration | Domain, Application, Infrastructure |
| WebApi | Endpoints, request and response models, transport mappings | Application, WebApi, and from Domain only typed identifiers and shared-kernel values |

Domain touches no Application Infrastructure or WebApi type and none of the persistence web mediator logging and injection namespaces. Application touches no Infrastructure or WebApi type and no ASP.NET Core type. WebApi touches no Domain type outside typed identifiers and shared-kernel values and no repository.

**Checked by:** `dotnet build` reports `LS1001` for a Domain type naming an outer-layer type and `LS2018` for an Application or WebApi type naming a type outside its layer.

### Keep Use Case Folders Isolated

**ID:** `litespec/backend/dotnet/architecture#keep-use-case-folders-isolated`

**Strength:** rule

**Statement:** A use case folder MUST reference no type in another use case folder.

**Rationale:** A use case folder holds one use case flat, so a reference between two such folders couples two slices that change for different reasons. A type named by two use cases belongs in the aggregate module or shared kernel layer folder that holds every consumer naming it, under `litespec/backend/architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers`. The rule applies to files and to global usings alike.

**Checked by:** `dotnet build` reports `LS2019` for a use case folder naming a type in another use case folder.

### Reach Another Module Only Through Contracts

**ID:** `litespec/backend/dotnet/architecture#reach-another-module-only-through-contracts`

**Strength:** rule

**Statement:** A module MUST reach another module only through types its Contracts folder publishes.

**Rationale:** The Contracts folder is the public surface of a module, sorted by kind: typed identifiers in `Identifiers/`, the refusals of rules other modules enforce in `Exceptions/`, integration events in `IntegrationEvents/`, the commands and queries other modules send with their results in `Commands/`, and ports that other modules call in `Ports/`. Domain may name the typed identifiers and refusals; ports and integration events stay outside Domain. A module that needs another module data reads it through a read port in the owner `Contracts/Ports/` folder that returns contract results keyed by typed identifier, or answers with its own query; it loads no foreign aggregate or repository. A command changes only its own module aggregates, and an act on another module aggregate runs as that module use case, dispatched or triggered by an integration event. A reaction to another module fact subscribes to the published integration event and never to the domain event.

**Checked by:** `dotnet build` reports `LS2020` for a module reference naming another module type outside its Contracts folder.

### Follow Declared Module Dependencies

**ID:** `litespec/backend/dotnet/architecture#follow-declared-module-dependencies`

**Strength:** rule

**Statement:** A module MUST reference only modules its record names in dependsOn.

**Rationale:** The dependsOn list states which modules a module may know, so an undeclared edge is a boundary the design never allowed. The rule covers code references and record links alike, and the graph stays acyclic.

**Reviewed for:** Which module references name a module outside their owner dependsOn, or close a dependency cycle? Look at the cross-module references beside the module records.

### Keep Host Endpoints in Hosts

**ID:** `litespec/backend/dotnet/architecture#keep-host-endpoints-in-hosts`

**Strength:** rule

**Applies to:** artifact: endpoint

**Statement:** Each host endpoint without a handler MUST live in its host project.

**Rationale:** Protocol endpoints, static assets, and development-only tooling have no use case folder to sit beside, so the host keeps them. Each still carries its mark, and the placement check accepts a host endpoint in the host.

**Reviewed for:** Which host endpoints without a handler sit outside their host project, or lack their mark? Look at the host endpoints beside the use case folders.

## Conventions

### Separate the Shared Kernel from the Modules

**ID:** `litespec/backend/dotnet/architecture#separate-the-shared-kernel-from-the-modules`

**Strength:** default

**Default:** Use a `{Product}.SharedKernel` project for what every module builds on and a `{Product}.Modules` project for the modules, with WebApi and optional Worker hosts, at every size.

**Replacement:** A project records its own project layout in its host documentation.

**Rationale:** The modules project then holds only business code and module slices, so a reader opening it sees the modules of the product and nothing else, and one use case stays in one tree instead of spreading across layer projects. The shared kernel names no module, so a change to one module never touches it, and the build refuses a shared kernel type that reaches into a module because the shared kernel project does not reference the modules project. Hosts stay separate projects, because two hosts can run the same modules.

**Example:**

```text
apps/api/src/{Product}.SharedKernel/
apps/api/src/{Product}.Modules/
apps/api/src/{Product}.WebApi/
apps/api/src/{Product}.Worker/     only when background work needs its own process
```

**Reviewed for:** Which projects sit outside the shared kernel, the modules project and their hosts, and does the shared kernel reference the modules project? Look at the solution file and the project references.

### Keep Composition in Hosts

**ID:** `litespec/backend/dotnet/architecture#keep-composition-in-hosts`

**Strength:** default

**Default:** Keep service registration in `Program.cs` and host registration modules.

**Replacement:** A project names its registration layout in its host documentation.

**Rationale:** Domain and Application declare abstractions for the hosts to implement. A registration call inside either layer makes the abstraction depend on the thing it abstracts, inverting the dependency direction. Each module registers its own Infrastructure types in its module Infrastructure folder, and `Program.cs` calls each module entry point in one visible order.

This provision binds `litespec/backend/architecture#keep-service-registration-in-hosts` for .NET.

**Example:** One registration entry point per module plus product plumbing has this shape:

```text
AddOrganizationsInfrastructure(...)
AddTeamsInfrastructure(...)
AddWebApi(...)
```

`Program.cs` keeps their call order visible. The WebApi entry point owns Problem Details, authentication, authorization, endpoint discovery, and OpenAPI.

**Reviewed for:** Which service registration calls sit outside `Program.cs` and the host registration modules? Look at the registration calls.

### Use One Public Assembly Marker

**ID:** `litespec/backend/dotnet/architecture#use-one-public-assembly-marker`

**Strength:** default

**Default:** Expose one public static assembly marker in the modules project for host endpoint discovery and scanning.

**Replacement:** A project names its scanner assembly reference in its host documentation.

**Rationale:** Scanners and the host need an assembly reference, and without a marker developers make handlers public to supply one. One public static marker in the modules project gives the host and the scanners their reference while every handler stays internal.

**Reviewed for:** Does the modules project expose one public static assembly marker, and does any other project expose one it needs not? Look at each project for its marker.
