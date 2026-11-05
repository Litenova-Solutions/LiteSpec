# .NET Architecture

**Stack:** dotnet

**Layer:** application

## Intent

A .NET backend holds every module at every layer in one application project, with hosts that compose and serve it. This page binds that shape for .NET: which projects exist, how modules aggregates and use cases fill one namespace tree, which folders each level allows, and where host-only endpoints live, so every type keeps one home and boundaries hold by folder and mark. The shared architecture rules live on [Backend Architecture](../architecture.md); this page holds only their .NET bindings.

## Concepts

### The Application Project and Its Hosts

The application project takes the product name and holds every module at every layer. The WebApi host holds Program composition and HTTP plumbing, and the Worker host exists only when background work needs its own process. Endpoints live in their use case folders and the host maps them through the one public assembly marker. The product `WebApi/` folder holds what endpoints of two or more modules compile against, such as `IEndpoint`, the current actor and shared transport values, while Problem Details, authentication, OpenAPI and rate limiting stay in the host.

The host also keeps the endpoints that have no handler to sit beside: protocol endpoints such as sign-out and device callbacks, static assets such as scripts it serves, and development-only tooling. Each such endpoint still carries its mark.

### The Module Tree and Its Layers

Every node of the tree may hold the same layer folders. Each level allows a closed set of folder names, so any other folder is refused. Inside a layer folder subfolders stay free, because the folder already states the layer and the lowest-folder rule already placed the type.

A use case file takes its layer from its role suffix: Endpoint RequestModel and ResponseModel are WebApi, while Command Query Result Handler and Validator are Application. A typed identifier that another module names lives in its owner Contracts folder, and shared-kernel values live in the product Domain folder. The product WebApi folder holds only what endpoints of two or more modules compile against, while product-wide HTTP plumbing stays in the host.

A module with one aggregate of its own name stays flat. Models stay separate and flow one way: request model, command, aggregate, result, response model. The endpoint maps in and out, and the handler returns a result and never the aggregate.

### Visibility and Composition

A type stays visible only to the assemblies that need it: implementations stay internal sealed, and a message port or marker turns public only when another assembly names it. Service registration lives in the hosts, with each module registering its own Infrastructure types in its module Infrastructure folder for the host to call in one visible order.

## Standards

### Mirror Modules Aggregates and Use Cases in One Namespace Tree

**ID:** `litespec/backend/dotnet/architecture#mirror-modules-aggregates-and-use-cases-in-one-namespace-tree`

**Strength:** rule

**Statement:** Every type in the application project MUST live in the namespace tree that starts with the product name and follows module aggregate and use case folders.

**Rationale:** The namespace reads like the qualified identifier, so `NightHall.Organizations.Teams.CreateTeam` names the product module aggregate and use case without a map. Namespaces follow folders under `litespec/workspace/naming#keep-namespaces-aligned-with-folders`, so a reader moves from a Domain aggregate to its handler its repository and its endpoint by name alone. This provision binds `litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case` for .NET.

**Example:** The Teams aggregate of the Organizations module fills one branch:

```text
NightHall/Organizations/Teams/Domain/Team.cs
NightHall/Organizations/Teams/CreateTeam/CreateTeamCommandHandler.cs
```

The handler above lives in namespace `NightHall.Organizations.Teams.CreateTeam`.

**Reviewed for:** Which types miss the product module aggregate use case namespace or name one outside its folder? Look at the namespaces beside the folders in the application project.

### Keep Closed Folder Sets at Every Level

**ID:** `litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level`

**Strength:** rule

**Statement:** Each level of the application project MUST hold only the folders the closed set table in this section allows.

**Rationale:** A closed set refuses the catch-all folder by construction: anything outside the table has no home and review refuses it. Product-wide plumbing lives in the product layer folders and cross-module contracts in each module Contracts folder, while `litespec/backend/architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers` decides between a use case folder and the aggregate module and product layer folders. Module folders mirror record homes, with UseCases Reactions and Jobs at module level.

**Example:**

| Level | Allowed folders |
|:---|:---|
| Product root | `Domain/`, `Application/`, `Infrastructure/`, `WebApi/`, one per declared module |
| Module | `Contracts/`, `Domain/`, `Application/`, `Infrastructure/`, `WebApi/`, `UseCases/`, `Reactions/`, `Jobs/`, one per declared aggregate |
| Aggregate | `Domain/`, `Application/`, `Infrastructure/`, `WebApi/`, one per declared use case |
| Use case | Files only |

```text
{Product}/
  Domain/  Application/  Infrastructure/  WebApi/
  Organizations/
    Contracts/
    Domain/  Application/  Infrastructure/
    WebApi/
    Teams/
      Domain/          Team.cs  TeamId.cs  TeamState.cs  ITeamRepository.cs  Events/  Exceptions/
      Application/     types used by two or more Team use cases
      Infrastructure/  TeamRepository.cs
      WebApi/          transport types used by two or more Team use cases
      CreateTeam/      use case, flat: Command, Result, Handler, Validator, Endpoint, RequestModel, ResponseModel
    UseCases/
    Reactions/
    Jobs/
```

**Checked by:** `litespec model check` reports `mark-folder-drift` for a folder outside its closed set or naming no declared record.

### Keep Application Types in Their Layer Folders

**ID:** `litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders`

**Strength:** rule

**Statement:** An application MUST place commands queries results handlers validators and module ports in Application folders and cross-module contracts in Contracts folders.

**Rationale:** One use case stays flat in its folder, with its command query result handler validator endpoint and transport models side by side. Types used by two or more use cases of one aggregate live in the aggregate Application folder, types used by two or more aggregates in the module Application folder, and product-wide plumbing in the product Application folder. A module publishes its typed identifiers integration events and cross-module ports in its Contracts folder, so the public surface of a module stays visible at the module.

**Reviewed for:** Which commands queries results handlers validators or ports sit outside their Application or Contracts folder? Look at the Application and Contracts folders beside the use case folders.

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

**Rationale:** A use case folder holds one use case flat, so a reference between two such folders couples two slices that change for different reasons. A type named by two use cases belongs in the aggregate module or product layer folder that holds every consumer naming it, under `litespec/backend/architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers`. The rule applies to files and to global usings alike.

**Checked by:** `dotnet build` reports `LS2019` for a use case folder naming a type in another use case folder.

### Reach Another Module Only Through Contracts

**ID:** `litespec/backend/dotnet/architecture#reach-another-module-only-through-contracts`

**Strength:** rule

**Statement:** A module MUST reach another module only through types its Contracts folder publishes.

**Rationale:** The Contracts folder is the public surface of a module: typed identifiers, integration events, the refusals of rules other modules enforce, and ports that other modules call. Domain may name the typed identifiers and refusals; ports and integration events stay outside Domain. A module that needs another module data reads it through a read port in the owner Contracts folder that returns contract results keyed by typed identifier, or answers with its own query; it loads no foreign aggregate or repository. A command changes only its own module aggregates, and an act on another module aggregate runs as that module use case, dispatched or triggered by an integration event. A reaction to another module fact subscribes to the published integration event and never to the domain event.

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

### Use One Application Project

**ID:** `litespec/backend/dotnet/architecture#use-one-application-project`

**Strength:** default

**Default:** Use one application project named for the product with WebApi and optional Worker hosts at every size.

**Replacement:** A project records its own project layout in its host documentation.

**Rationale:** One application project holds every module at every layer, so one use case stays in one tree instead of spreading across projects. Hosts stay separate projects, because two hosts can run the same modules.

**Example:**

```text
apps/api/src/{Product}/
apps/api/src/{Product}.WebApi/
apps/api/src/{Product}.Worker/     only when background work needs its own process
```

**Reviewed for:** Which projects sit outside the one application project and its hosts? Look at the solution file beside the declared project size.

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

**Default:** Expose one public static assembly marker in the application project for host endpoint discovery and scanning.

**Replacement:** A project names its scanner assembly reference in its host documentation.

**Rationale:** Scanners and the host need an assembly reference, and without a marker developers make handlers public to supply one. One public static marker in the application project gives the host and the scanners their reference while every handler stays internal.

**Reviewed for:** Does the application project expose one public static assembly marker, and does any other project expose one it needs not? Look at each project for its marker.
