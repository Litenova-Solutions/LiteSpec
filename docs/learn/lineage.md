# Lineage

LiteSpec names its concepts after the established practices they come from, so a reader who knows the source already knows the word. Each section below names one practice, what LiteSpec takes from it, the words it uses, and what LiteSpec does differently. Read this page after [How LiteSpec Compares](how-litespec-compares.md); it is background, not rules.

## Domain-Driven Design

Developers use domain-driven design to keep code aligned with the business: one ubiquitous language shared by people and code, aggregates that guard consistency, and domain events that record completed facts.

**Takes:** the aggregate as the unit of consistency, the module inside one bounded context, the ubiquitous language shared by people and code, and the domain event as the record of something completed.

**Words:** aggregate, module, glossary, domain event.

**Differs:** LiteSpec fixes one bounded context per product, so there is no context map to maintain. Child entities and value objects live in code and the glossary, never as records: the specification names only what carries business meaning. Each aggregate declares its states with their meanings, its transitions, its data classes with their erasure positions, and its classified data, and the builder checks each declaration.

## Event Storming

Event storming finds behavior with one question: whenever something happens, what does the system do in response? The answer is a policy, and LiteSpec records each policy as its own record.

**Takes:** the policy: whenever something happens, do something in response.

**Words:** reaction, with `triggeredBy`, `runs`, `sends` or `calls` in `effects`, and `onFailure`; job, with `kind`, `schedule`, and `runs`, `sends` or `calls` in `effects`.

**Differs:** A reaction is its own record rather than a use case kind: it names the domain events, module events, and integration events it answers in `triggeredBy`, runs at most one use case through `effects`, and never writes an aggregate directly. A job carries scheduled work the same way, with its `kind` and `schedule`. Both are proven by scenarios like any other record, and the builder reads the fields that name their triggers.

## CQRS

Command Query Responsibility Segregation splits commands, which change state, from queries, which answer questions. The split matters because reads and writes have different needs: writes need consistency, while reads need typed output with freshness. LiteSpec applies the split to every use case, so each one declares what its kind requires.

**Takes:** the command-query split: commands change state, and queries answer questions.

**Words:** kind, with the values command and query.

**Differs:** Every use case is a command or a query. Commands declare what they change and their consistency. Queries declare their typed output in `include` groups with freshness, and their paging. Both kinds are proven the same way: a passing test through the public boundary for every scenario.

## Behavior-Driven Development

Behavior-driven development writes behavior as concrete examples in plain words, so developers, testers, and product people can agree on what the software does before it exists. LiteSpec takes the scenario as its unit of proof and ties each one to a result and a passing test.

**Takes:** the scenario in given, when, then, written with the Gherkin grammar; `examples` lists that try one scenario against many rows; and the Example section narrative, drawn from Example Mapping and Specification by Example, that shows behavior inside a real occasion.

**Words:** scenario, with given, when, then and an optional `examples` field; example, the narrative section in the explanation file.

**Differs:** Scenarios are grouped under their result: each one names exactly one result, and each is proven by a passing test through the public boundary. A test's expected values come only from the scenario's then, never from running the code. The book shows Then clauses as context in its flow detail, so product readers see the promised endings without opening Behavior.

## Requirements Engineering

Requirements engineering sorts what a product must do into kinds: what it does, how well it does it, and the policies it obeys. LiteSpec takes those kinds and the habit of measuring quality claims.

**Takes:** the requirement kinds functional, quality, and policy, drawn from BABOK and IREB; the measurable goal with its target, drawn from Volere and product practice; and the fit criterion: every quality requirement carries a measure that a check confirms.

**Words:** requirement, goal, measure.

**Differs:** LiteSpec writes each requirement as a plain statement with a one-line rationale, not in the fixed EARS sentence patterns. Goals stand as their own records instead of living inside requirements, so one goal serves many features. Each requirement states its exceptions with a fallback requirement, so the fallback is promised beside the rule instead of hidden in prose.

## Impact Mapping and Jobs to Be Done

Impact mapping reasons outward from a goal through actors and impacts to deliverables, and Jobs to Be Done frames a customer's job as progress in a situation, stable while solutions churn. LiteSpec takes the goal-first shape and fixes it as typed records and links.

**Takes:** the goal at the center, with actors, impacts, and deliverables reasoned outward from it; and the job as progress in a situation, stable while solutions churn.

**Words:** goal, problem, feature, flow.

**Differs:** LiteSpec fixes the map as typed records and links: a feature answers one problem from that problem's folder and advances goals, and a flow holds product sentences with ids that point toward the record the flow exists for. Use cases point up to the steps they serve; a step never names a use case. There is no map to draw: the record tree carries the links, and the builder checks them.

## Decision Records

Architecture decision records keep one record per significant decision: the context, the options considered, the choice, and its consequences. LiteSpec takes that shape so a later reader sees what was decided, why, and what would change it.

**Takes:** one record per significant decision, with its context, the options considered, the choice, and its consequences.

**Words:** decision, with options, choice, and consequences.

**Differs:** Options are entries with `pros` and `cons`, which keeps the choice structured instead of prose. Each settled record names its decision in `basedOn`, and the decision names what would change it. A later decision names the earlier one in `supersedes`, and the earlier record stays as context.

## Role-Based Access Control

Role-based access control grants permissions through roles, which forms a chain from the people and systems that act to what they may do. LiteSpec takes that chain into its records: each use case declares who may reach it, and the code derives the actor from verified claims.

**Takes:** permissions grouped into roles, granted to the people and systems that act.

**Words:** actor, role, permission.

**Differs:** An actor holds a role, and a use case declares its access as a grant and a tenant scope, which forms one chain from actor to use case. Deriving the actor from verified claims is Implementation: stack code that tools check. Generic refusals derive from access, input, and the knowledge the use case reads instead of being written on every use case.

## Design by Contract

Design by contract states what always holds and what must hold before a call: invariants and preconditions. LiteSpec takes both and adds the rule kinds a business application needs.

**Takes:** the invariant, always true, and the precondition, checked before.

**Words:** invariant, precondition.

**Differs:** LiteSpec adds calculation, atomicity, reservation, and frozen-value as rule kinds, so computed values, atomic writes, held capacity, and frozen commercial values each have a home. Each rule is proven by scenarios, each violation ends in a refusal with a stable code, and refusals are ordered so the first match wins.

## Kernel, Platform, and Port

A .NET backend groups code into a small shared Kernel, a technical Platform, and one project per module that talks outward through ports. The grouping keeps business words in one place and technical setup in another, so a reader finds each kind of code by project.

**Takes:** the shared kernel from Eric Evans's *Domain-Driven Design* (2003): a small part of the model that each part of the system shares and changes together; the microservice chassis from Chris Richardson (microservices.io): the technical base that handles the cross-cutting work each service repeats, such as persistence setup, messaging, and security; and the port from Alistair Cockburn's "Hexagonal Architecture" (2005) ([alistair.cockburn.us](https://alistair.cockburn.us/hexagonal-architecture/)): the application talks outward only through named ports, and adapters convert between a port and a technology.

**Words:** kernel, for the business values every module uses; platform, for the technical capabilities every module reuses; port, with `Ports/{Port}/` holding `I{Port}` and `Infrastructure/{Port}/` holding the adapters that implement it.

**Differs:** LiteSpec builds one bounded context per product, so the kernel drops Shared and holds vocabulary, not an agreement between contexts. The platform uses a plain word for the chassis, and modules plug into it by adding data to its registries instead of calling services. Ports cover external systems, file formats, and the clock alike, and the same idea appears as Fowler's Gateway ([martinfowler.com](https://martinfowler.com/eaaCatalog/gateway.html)), Palermo's Onion Architecture ([jeffreypalermo.com](https://jeffreypalermo.com/2008/07/the-onion-architecture-part-1/)), and Clean Architecture gateways ([blog.cleancoder.com](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)): the inner code owns the interface and the outer code implements it. No interface crosses between modules: a module reaches another module through published data and the mediator.

Names set aside when the three words were picked:

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

## Next

[Work with Agents](working-with-agents.md) explains how an agent uses the standard and the records together.
