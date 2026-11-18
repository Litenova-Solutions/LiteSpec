# Module Contracts Publish Data

A module's Contracts project publishes data only, grouped by aggregate, and each consumer reaches another module through its own port to that module. This decision replaces reader interfaces in Contracts with public queries, public commands, facts, and carried identifiers as data, and makes each cross-module edge a declared record link.

## Context

A module published reader interfaces in its Contracts folder for every other module to call. One large consumer named 86 such readers in 741 files of other modules. The busiest reader had 18 methods, several named for their one caller. No record said which fields crossed the boundary, and a personal field could travel to any reader.

The word port named two directions at once. A module declares driven ports in its `Ports/` folder for work it does not compute in memory: an external system, a file format, a platform service, or the clock. The same word named the reader interfaces a module published in `Contracts/Ports/` for other modules to call. One word for what a module needs and for what it serves hid which direction each interface faces.

Two modules of the owner's product referenced each other's identifiers, so neither Contracts project could state what it owned. Each module carried identifiers the other owned, and no folder said which module owned which aggregate.

A microservice backend whose internal contracts project published request and response models per operation showed the shape that works. Its contracts project published only data, grouped by aggregate and operation, while each consuming service declared its own client port. LiteSpec keeps that shape and removes its weaknesses: transport details in the contract, provider types leaking into the consumer, and undeclared edges.

Letting only queries cross modules then showed its own cost. A module could never ask another to act, so a process spanning modules could only be choreographed, each module reacting to the other's facts, and choreography makes two modules depend on each other. One product showed ten such two-way pairs: a workspace erasure process with the requester reacting to every module's erased fact, and business collaborations with a ledger module and the banking, vat and invoicing modules reacting to each other's facts.

Outside practice resolves this with commands owned by their handler. [Enterprise Integration Patterns](https://www.enterpriseintegrationpatterns.com/patterns/messaging/CommandMessage.html) defines the command message. [NServiceBus](https://docs.particular.net/nservicebus/messaging/messages-events-commands) sends a command to its one logical owner and publishes an event to any subscriber. A command's contract belongs to the service that handles it, while an event's belongs to the service that publishes it, as [Jimmy Bogard's messaging semantics](https://lostechies.com/jimmybogard/2012/12/20/messaging-semantics-ownership/) states. [Chris Richardson's saga pattern](https://microservices.io/patterns/data/saga.html) runs a spanning process as an orchestrator that drives participants through their own command APIs, with participants never knowing the coordinator.

## Options

LiteSpec compared 6 options for module contracts. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Contracts publish public queries and public commands as data grouped by aggregate; each consumer declares one port per provider | The provider states its public queries, public commands, facts, and carried identifiers as data beside the use cases that produce them. Each consumer port states what one consumer needs in its own types. Each cross-module call has one direction, and no interface crosses a module boundary. | Each consumer writes a port and an adapter per provider module it calls. A public query or public command needs its record, its message, and its result in Contracts. |
| Consumers call the provider's public query or public command directly | No consumer port and no adapter: the handler sends the provider message itself. | Provider types leak into the consumer's application layer, and no port states what the consumer needs. Replacing the provider rewrites every calling handler. |
| Only queries cross modules; changes travel through facts and reactions | No synchronous cross-module write: the caller never commits with the provider. | A module can never ask another to act, so spanning processes choreograph and modules depend on each other both ways. One product showed ten such two-way pairs. |
| Facts stay in a ModuleEvents folder apart from their aggregate | One folder lists every fact the module publishes. | The fact sits apart from the aggregate and the use cases that produce it. A reader cannot tell what one aggregate publishes without searching every kind folder. |
| Identifiers shared modules carry live in the Kernel | Every module references one shared identifier without a Contracts reference. | The Kernel fills with business identifiers owned by no module. A shared identifier hides which module owns the aggregate it names. |
| Modules carry foreign identifiers as plain Guids in the Platform | No Contracts reference is needed to carry another module's identifier. | The type no longer says which aggregate the value names. A caller passes any Guid where an ExpenseId belongs, and the compiler stays silent. |

## Decision

A Contracts project holds data only: no interface anywhere in the project. Its root holds one folder per aggregate with anything public, in the plural, and one folder per public module-level use case. Each aggregate folder holds the aggregate identifier when another module carries it, an `Events/` folder with the facts about the aggregate another module consumes, and one folder per public query or public command of that aggregate with its message and result at the same path the use case has in the provider's `Application/` folder. Each fact sits in the `Events/` folder of the aggregate whose domain event it translates, as its module or integration event record's `translates` names.

Commands and queries flow upstream, and facts flow downstream. A downstream module sends an upstream module's public queries and public commands and reacts to its facts, while an upstream module never queries, commands, or reacts to a downstream module. A public command is a command use case whose record lists the sending modules in `reachedThrough`, exactly like a public query. Its message and result sit in the owner's Contracts, and the owner handles it with its ordinary command handler, through its own pipeline with authorization, audit, idempotency, and validation.

A consumer reaches another module only through its port to that module: one port per provider module, named for the provider, in `Ports/{Provider}/`, with one adapter in `Infrastructure/{Provider}/` that sends the provider's public queries and public commands and maps their results. The port's signatures name the consumer's own types, never the provider's messages or results. The calling use case, reaction, or job declares the edge as `effects.queries` or `effects.commands` naming the provider's public use case. For now each public command is sent in process and synchronously through the adapter, like a public query. When an upstream module must know downstream work finished, the downstream module calls the upstream module's public command.

Facts are module events or integration events; both sit in the `Events/` folder of their aggregate, told apart by type and record, not by folder. A process spanning modules runs from its most downstream participant, or from a process module depending on every participant, through the participants' public commands.

An identifier another module carries lives directly in `Contracts/{Aggregates}/` of the module that owns the aggregate; one no other module carries lives in `Domain/{Aggregates}/`. The Kernel holds an identifier only with a decision record naming the Contracts cycle it breaks. The Platform names no business identifier: it speaks the tenant and the actor, and the owner of each identifier converts at the module edge.

## Consequences

The provider's public surface reads as data beside the use cases that produce it. The consumer's `Ports/` folder lists every module it calls beside every outside system, file format, and platform service it needs, each with its adapter in `Infrastructure/`. No interface crosses a module boundary, so replacing a provider rewrites one adapter per consumer.

A read model stays the answer when a consumer needs another module's data reshaped and kept locally, fed by the provider's facts; it is the exception now, not the rule.

The new and widened provisions are `litespec/backend/architecture#give-every-module-relationship-one-direction`, `litespec/backend/architecture#send-commands-and-queries-upstream`, `litespec/backend/application#declare-every-commands-edge-on-the-sender`, `litespec/backend/application#send-public-commands-in-process`, `litespec/backend/application#confirm-downstream-completion-through-an-upstream-command`, `litespec/backend/application#reach-another-module-through-one-port-per-provider`, `litespec/backend/dotnet/architecture#publish-only-data-from-each-contracts-project`, `litespec/core/use-cases#list-the-consumer-modules-of-a-public-query`, and `litespec/core/use-cases#list-the-sending-modules-of-a-public-command`.

## Evidence

[research/cross-module-coupling-practice](research/cross-module-coupling-practice) holds the reader counts and the coupling they measured. The command ownership rule follows [Enterprise Integration Patterns](https://www.enterpriseintegrationpatterns.com/patterns/messaging/CommandMessage.html), [NServiceBus messages, events and commands](https://docs.particular.net/nservicebus/messaging/messages-events-commands), [Jimmy Bogard's messaging semantics](https://lostechies.com/jimmybogard/2012/12/20/messaging-semantics-ownership/), and [Chris Richardson's saga pattern](https://microservices.io/patterns/data/saga.html).

## Revisit Conditions

This decision reopens when a product shows a cross-module act no public command can carry, or when a module shows an identifier two modules must own at once.

Either case means data-only contracts no longer cover what modules share. The team then decides again what a Contracts project publishes.
