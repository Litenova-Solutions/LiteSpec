# Kernel, Platform, and One Project per Module

A module is a business capability, and it is complete in one project. This decision separates the business values every module speaks into the Kernel, the technical capabilities into the Platform, and each module into its own project, with use cases directly under the module and every use case layer in its own folder.

## Context

The two-project layout held business values and technical wiring in one shared kernel: money and typed identifiers beside mediator wiring, authorization guards, the audit sink, and JSON converters. Nothing separated a type a domain model may use from a type only a host touches.

Module registration lived in a `Composition/` folder inside the modules project, because two hosts needed the same wiring. The folder sat among the business modules and reached module internals across the same assembly.

Infrastructure had two homes and a use case had none. A use case held `Application/` and `WebApi/` only, so an adapter belonging to one use case landed in `Application/`. Technical setup repeated per module.

Module boundaries held as analyzer rules only, because one assembly held every module. Use cases sat five levels deep under one aggregate, and a use case changing two aggregates had to pick one parent. Aggregate folders shared their name with the aggregate type, so sibling files resolved the bare name to the namespace.

## Options

LiteSpec compared 2 options for the backend layout. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Separate the Kernel, the Platform, and one project per module | Module boundaries hold by project reference and visibility. Each use case sits two levels under its module, and every use case layer gets its own folder. | A solution holds more projects, and small products carry the same shape as large ones. |
| Keep the shared kernel and modules projects and fix each symptom alone | No project moves files, and each fix stays small. | Module boundaries stay analyzer rules, use cases stay five levels deep, and each fix leaves the layout that caused it. |

## Decision

The Kernel holds the business values every module speaks with no dependency on any library. The Platform holds the technical capabilities with no business words and references no module.

Each module is one project with its entry type, `Contracts/`, `Domain/`, `UseCases/`, and `Infrastructure/`. Aggregates live under `Domain/` with one folder each in the singular, and use cases sit directly under the module with one folder per layer: `Application/` always, `WebApi/` when the use case is reached over HTTP, and `Infrastructure/` when the use case has its own port. A project adds a layer with a row in the layer table, its allowed references, and its analyzer rule.

Only `Contracts/` and the entry type are public, so the compiler refuses any other cross-module reference. Hosts compose modules through `AddModules`, and modules plug into platform capabilities by contributing data instead of copying platform code.

The record tree stays as it is. A use case record under `aggregates/{aggregate}/use-cases/{use-case}` or under `use-cases/{use-case}` maps to code at `UseCases/{UseCase}/`, and an aggregate record maps to code at `Domain/{Aggregate}/`. A module whose single aggregate has the module's name stays flat. A single modules project holding every module stays allowed with `LS2020` as its boundary.

## Consequences

The Kernel holds the business values every module speaks with no dependency on any library. The Platform holds the technical capabilities with no business words and references no module. Each module is one project with its entry type, Contracts, Domain, UseCases, and Infrastructure.

Aggregates live under Domain with one folder each, and use cases sit directly under the module with one folder per layer. Only Contracts and the entry type are public, and hosts compose modules through AddModules.

The record tree stays, with use case records mapping to UseCases and aggregate records to Domain. A single modules project stays allowed with LS2020 as its boundary.

The names come from two established patterns. The Kernel takes its name from the Shared Kernel pattern in Eric Evans's *Domain-Driven Design* (2003), a small shared part of the model that changes only by agreement, without the word Shared, because everything in it is shared and one product is one bounded context. The Platform follows the Microservice Chassis pattern Chris Richardson describes on microservices.io, the technical base that handles cross-cutting concerns so each service holds only business code; it is named Platform because modules contribute to it and hosts call `AddPlatform`. The [.NET Architecture](../../../backend/dotnet/architecture.md) page lists the other names considered.

The new provisions are `litespec/backend/dotnet/architecture#keep-business-values-in-the-kernel`, `litespec/backend/dotnet/architecture#keep-technical-capabilities-in-the-platform`, `litespec/backend/dotnet/architecture#register-each-module-through-its-entry-point`, `litespec/backend/dotnet/architecture#plug-modules-into-the-platform-through-contributions`, `litespec/backend/dotnet/architecture#hold-each-use-case-layer-in-its-own-folder`, `litespec/backend/dotnet/architecture#expose-only-contracts-and-the-entry-point`, `litespec/backend/architecture#produce-file-formats-in-infrastructure`, `litespec/backend/application#keep-use-case-folders-small`, and `litespec/backend/dotnet/api#convert-money-with-the-platform-converter`.

## Revisit Conditions

This decision reopens when a module outgrows the layout, or when project-per-module costs outweigh compiler-enforced boundaries.

Either case means the layout no longer fits the products built on it. The team then decides again how backend code is organized.
