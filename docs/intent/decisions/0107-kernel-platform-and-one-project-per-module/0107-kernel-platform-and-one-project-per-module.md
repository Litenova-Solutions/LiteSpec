# Kernel, Platform, and One Project per Module

A module is a business capability, and it is complete in one project. This decision separates the business values every module speaks into the Kernel, the technical capabilities into the Platform, and each module into its own project, with the four onion layer folders at the top of each module.

## Context

The two-project layout held business values and technical wiring in one shared kernel: money and typed identifiers beside mediator wiring, authorization guards, the audit sink, and JSON converters. Nothing separated a type a domain model may use from a type only a host touches.

Module registration lived in a `Composition/` folder inside the modules project, because two hosts needed the same wiring. The folder sat among the business modules and reached module internals across the same assembly.

Infrastructure had two homes and a use case had none. A use case held `Application/` and `WebApi/` only, so an adapter belonging to one use case landed in `Application/`. Technical setup repeated per module.

Module boundaries held as analyzer rules only, because one assembly held every module. Use cases sat five levels deep under one aggregate, and a use case changing two aggregates had to pick one parent. Aggregate folders shared their name with the aggregate type, so sibling files resolved the bare name to the namespace.

The slice layout that replaced the two-project layout put one use case in one folder with its own `Application/`, `WebApi/`, and `Infrastructure/` folders. It read as unfamiliar to its own owner: a .NET reader opening a module expects the four layer folders at the top, and a layout readers recognize is worth more than keeping one use case in one folder.

## Options

LiteSpec compared 5 options for the backend layout. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Separate the Kernel, the Platform, and one project per module with onion layer folders | Module boundaries hold by project reference and visibility. The four layer folders read at first glance to anyone who knows .NET, and one style holds across products. | A solution holds more projects, one use case spreads over up to three folders, and small products carry the same shape as large ones. |
| Slice folders with layers in each use case | One folder shows a use case whole, with its layers beside its handler. | The layout reads as unfamiliar to .NET readers, and a second organization grows beside the layer checks. |
| Aggregate-first folders | Each aggregate holds its domain and its use cases in one place. | A use case touching two aggregates needs a second parent, and a use case no aggregate owns fits nowhere. |
| Onion projects per module | The compiler enforces layer purity inside each module through project references. | Four projects per module multiply the solution, and every use case crosses project boundaries. |
| Ports and adapters folders | Hexagonal names state the seams between the application and its drivers. | The vocabulary is new to the checks, and the ports-and-adapters split differs from the four layers the standard enforces. |

## Decision

The Kernel holds the business values every module speaks with no dependency on any library. The Platform holds the technical capabilities with no business words and references no module.

Each module is one project with its entry type, `Contracts/`, `Domain/`, `Application/`, `Infrastructure/`, and `WebApi/`. The four layer folders stand at the top of the module, and each holds one folder per use case it serves: `Application/{UseCase}/` always, `WebApi/{UseCase}/` when the use case is reached over HTTP, and `Infrastructure/{UseCase}/` when the use case has its own adapter. Aggregates live under `Domain/` with one folder each in the singular.

Only `Contracts/` and the entry type are public, so the compiler refuses any other cross-module reference. Hosts compose modules through `AddModules`, and modules plug into platform capabilities by contributing data instead of copying platform code.

The record tree stays as it is. A use case record under `aggregates/{aggregate}/use-cases/{use-case}` or under `use-cases/{use-case}` maps to code at `Application/{UseCase}/` and, when reached over HTTP, `WebApi/{UseCase}/`, plus `Infrastructure/{UseCase}/` when it has its own adapter, and an aggregate record maps to code at `Domain/{Aggregate}/`. A module whose single aggregate has the module's name stays flat. A single modules project holding every module stays allowed with `LS2020` as its boundary. LiteSpec supports one architecture style: the only project choice is one project per module or one modules project.

## Consequences

The Kernel holds the business values every module speaks with no dependency on any library. The Platform holds the technical capabilities with no business words and references no module. Each module is one project with its entry type, Contracts, Domain, Application, Infrastructure, and WebApi.

Each layer folder holds one folder per use case it serves. Only Contracts and the entry type are public, and hosts compose modules through AddModules.

The record tree stays, with use case records mapping to their layer folders and aggregate records to Domain. A single modules project stays allowed with LS2020 as its boundary.

The names come from two established patterns. The Kernel takes its name from the Shared Kernel pattern in Eric Evans's *Domain-Driven Design* (2003), a small shared part of the model that changes only by agreement, without the word Shared, because everything in it is shared and one product is one bounded context. The Platform follows the Microservice Chassis pattern Chris Richardson describes on microservices.io, the technical base that handles cross-cutting concerns so each service holds only business code; it is named Platform because modules contribute to it and hosts call `AddPlatform`. The [.NET Architecture](../../../backend/dotnet/architecture.md) page lists the other names considered.

The new provisions are `litespec/backend/dotnet/architecture#keep-business-values-in-the-kernel`, `litespec/backend/dotnet/architecture#keep-technical-capabilities-in-the-platform`, `litespec/backend/dotnet/architecture#register-each-module-through-its-entry-point`, `litespec/backend/dotnet/architecture#plug-modules-into-the-platform-through-contributions`, `litespec/backend/dotnet/architecture#hold-each-layer-in-its-own-folder`, `litespec/backend/dotnet/architecture#expose-only-contracts-and-the-entry-point`, `litespec/backend/architecture#produce-file-formats-in-infrastructure`, `litespec/backend/application#keep-use-case-folders-small`, and `litespec/backend/dotnet/api#convert-money-with-the-platform-converter`.

## Revisit Conditions

This decision reopens when a module outgrows the layout, when project-per-module costs outweigh compiler-enforced boundaries, or when the layer folders stop matching the shape .NET readers expect.

Any of those cases means the layout no longer fits the products built on it. The team then decides again how backend code is organized.
