# Module Contracts Publish Data

A module's Contracts project publishes data only, grouped by aggregate, and each consumer reaches another module through its own port to that module. This decision replaces reader interfaces in Contracts with public queries, facts, and carried identifiers as data, and makes each cross-module edge a declared record link.

## Context

A module published reader interfaces in its Contracts folder for every other module to call. One large consumer named 86 such readers in 741 files of other modules. The busiest reader had 18 methods, several named for their one caller. No record said which fields crossed the boundary, and a personal field could travel to any reader.

The word port named two directions at once. A module declares driven ports in its `Ports/` folder for work it does not compute in memory: an external system, a file format, a platform service, or the clock. The same word named the reader interfaces a module published in `Contracts/Ports/` for other modules to call. One word for what a module needs and for what it serves hid which direction each interface faces.

Two modules of the owner's product referenced each other's identifiers, so neither Contracts project could state what it owned. Each module carried identifiers the other owned, and no folder said which module owned which aggregate.

A microservice backend whose internal contracts project published request and response models per operation showed the shape that works. Its contracts project published only data, grouped by aggregate and operation, while each consuming service declared its own client port. LiteSpec keeps that shape and removes its weaknesses: transport details in the contract, provider types leaking into the consumer, synchronous cross-service writes, and undeclared edges.

## Options

LiteSpec compared 6 options for module contracts. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Contracts publish data grouped by aggregate; each consumer declares one port per provider | The provider states its public queries, facts, and carried identifiers as data beside the use cases that produce them. Each consumer port states what one consumer needs in its own types. No interface crosses a module boundary. | Each consumer writes a port and an adapter per provider module it reads. A public query needs its record, its message, and its result in Contracts. |
| Consumers call the provider's public query directly | No consumer port and no adapter: the handler sends the provider query itself. | Provider types leak into the consumer's application layer, and no port states what the consumer needs. Replacing the provider rewrites every calling handler. |
| Contracts publish public commands beside public queries | A consumer triggers a provider act with one call and reads its result at once. | A synchronous cross-module write couples the caller to the provider's consistency boundary and uptime. A change in one module reaches another without a fact the records can follow. |
| Facts stay in a ModuleEvents folder apart from their aggregate | One folder lists every fact the module publishes. | The fact sits apart from the aggregate and the use cases that produce it. A reader cannot tell what one aggregate publishes without searching every kind folder. |
| Identifiers shared modules carry live in the Kernel | Every module references one shared identifier without a Contracts reference. | The Kernel fills with business identifiers owned by no module. A shared identifier hides which module owns the aggregate it names. |
| Modules carry foreign identifiers as plain Guids in the Platform | No Contracts reference is needed to carry another module's identifier. | The type no longer says which aggregate the value names. A caller passes any Guid where an ExpenseId belongs, and the compiler stays silent. |

## Decision

A Contracts project holds data only: no interface anywhere in the project. Its root holds one folder per aggregate with anything public, in the plural, and one folder per public module-level query. Each aggregate folder holds the aggregate identifier when another module carries it, the facts about the aggregate another module consumes, and one folder per public query of that aggregate with its message and result at the same path the use case has in the provider's `Application/` folder.

A public use case is a query use case whose record lists consuming modules in `reachedThrough`. Only queries cross modules: a command is never public, and a change in one module reaches another through a fact and a reaction there. Facts are module events or integration events; both sit in their aggregate folder, told apart by type and record, not by folder.

A consumer reaches another module only through its port to that module: one port per provider module, named for the provider, in `Ports/{Provider}/`, with one adapter in `Infrastructure/{Provider}/` that sends the provider's public queries and maps their results. The port's signatures name the consumer's own types, never the provider's queries or results. The consuming use case, reaction, or job declares the edge as `effects.queries` naming the provider's public use case.

An identifier another module carries lives in `Contracts/{Aggregates}/` of the module that owns the aggregate; one no other module carries lives in `Domain/{Aggregates}/`. The Kernel holds an identifier only with a decision record naming the Contracts cycle it breaks. The Platform names no business identifier: it speaks the tenant and the actor, and the owner of each identifier converts at the module edge.

## Consequences

The provider's public surface reads as data beside the use cases that produce it. The consumer's `Ports/` folder lists every module it reads beside every outside system, file format, and platform service it needs, each with its adapter in `Infrastructure/`. No interface crosses a module boundary, so replacing a provider rewrites one adapter per consumer.

A read model stays the answer when a consumer needs another module's data reshaped and kept locally, fed by the provider's facts; it is the exception now, not the rule.

The new provisions are `litespec/backend/dotnet/architecture#publish-only-data-from-each-contracts-project`, `litespec/backend/dotnet/architecture#keep-each-identifier-with-its-owner`, `litespec/backend/dotnet/architecture#name-no-business-identifier-in-the-platform`, `litespec/backend/architecture#send-only-queries-across-modules`, `litespec/backend/application#reach-another-module-through-one-port-per-provider`, and `litespec/core/use-cases#list-the-consumer-modules-of-a-public-query`.

## Evidence

[research/cross-module-coupling-practice](research/cross-module-coupling-practice) holds the reader counts and the coupling they measured.

## Revisit Conditions

This decision reopens when a consumer shows a current-value read no public query can answer, or when a module shows an identifier two modules must own at once.

Either case means data-only contracts no longer cover what modules share. The team then decides again what a Contracts project publishes.
