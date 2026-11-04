# Backend Architecture

**Stack:** any
**Layer:** application

## Intent

The backend is a modular monolith of four layers: Domain holds business rules, Application coordinates use cases, Infrastructure holds technical adapters, and WebApi holds HTTP transport. A use case is one command or query with its access, results, and scenarios, a reaction answers an event by running a command or sending a notification, and a job runs work on a schedule or by hand. This page states those layers, the direction their references point, and the module tree that organizes every layer; a team building the backend reads it before the layer pages, which state what each layer owns.

## Concepts

### The Four Layers

A layer is the role a type plays, whatever packaging holds it. The four layers form one inward-pointing chain: each layer references only layers closer to Domain.

| Layer | Holds | May reference |
|:---|:---|:---|
| Domain | Aggregates, state records, value objects, domain events, domain exceptions, repository contracts | Domain |
| Application | Commands, queries, results, handlers, validators, ports, pipeline positions | Domain, Application |
| Infrastructure | Repositories, adapters, mappings, module registration | Domain, Application, Infrastructure |
| WebApi | Endpoints, request and response models, transport mappings | Application, WebApi, and from Domain only typed identifiers and shared-kernel values |

Layer boundaries are not deployment boundaries: the layers deploy together as one modular monolith.

### Modules and Use Cases

A [module](../reference/glossary.md) is one business area organizing language, records, use cases, and ownership. A [use case](../reference/glossary.md) is one command or query with its access, results, and scenarios. A [reaction](../reference/glossary.md) answers an event without a request, and a [job](../reference/glossary.md) runs work on a schedule or by hand. Every module organizes its code as the module tree: one folder per declared aggregate, one folder per declared use case, and the layer folders each level allows, so a reader moves from a Domain aggregate to its handler, its domain repository, and its endpoint without a map. A module publishes its public surface in `Contracts/`: typed identifiers other modules name, integration events it publishes, and ports other modules call.

### The Folder Tree

Every node (product root, module, aggregate) may hold the same layer folders. Each level has a closed set of folder names, so a check refuses anything else. Inside a layer folder, subfolders are free (`Application/Pipeline/`, `Infrastructure/Development/`), because the folder already states the layer and the lowest-folder rule already placed the type.

```text
{Product}/
  Domain/  Application/  Infrastructure/     shared kernel, pipeline positions, store setup and outbox
  Organizations/                             module
    Contracts/                               public surface: typed ids, integration events, ports other modules call
    Domain/  Application/  Infrastructure/   types used by two or more aggregates; module registration
    WebApi/                                  transport types used by use cases of two or more aggregates
    Teams/                                   aggregate
      Domain/          Team, TeamId, TeamState, ITeamRepository, Events/, Refusals/
      Application/     types used by two or more Team use cases
      Infrastructure/  TeamRepository
      WebApi/          transport types used by two or more Team use cases (headers, links, partial models)
      CreateTeam/      use case, flat: Command, Result, Handler, Validator, Endpoint, RequestModel, ResponseModel
    UseCases/          use cases no single aggregate owns
    Reactions/
    Jobs/
```

| Level | Allowed folders |
|:---|:---|
| Product root | `Domain/`, `Application/`, `Infrastructure/`, one per declared module |
| Module | `Contracts/`, `Domain/`, `Application/`, `Infrastructure/`, `WebApi/`, `UseCases/`, `Reactions/`, `Jobs/`, one per declared aggregate |
| Aggregate | `Domain/`, `Application/`, `Infrastructure/`, `WebApi/`, one per declared use case |
| Use case | Files only |

A typed identifier another module names lives in its owner's `Contracts/`, so the one rule "another module only through `Contracts/`" covers identifiers too. Shared-kernel values stay in the product `Domain/`. Product-wide HTTP plumbing (Problem Details, authentication, OpenAPI, rate limiting) stays in the host. The product `WebApi/` folder holds only what endpoints of two or more modules compile against, such as the endpoint contract and the current actor. `litespec/backend/architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers` decides between a use case folder and the aggregate, module and product layer folders; across modules a type is never shared, and the owning module publishes it in `Contracts/`. A use case file's layer comes from its role suffix in `litespec/workspace/naming#use-architectural-suffixes`: `Endpoint`, `RequestModel` and `ResponseModel` are WebApi, while `Command`, `Query`, `Result`, `Handler` and `Validator` are Application. Module-level folders mirror the record homes: `UseCases/` holds the use cases no single aggregate owns, `Reactions/` the reactions, and `Jobs/` the jobs. A module with one aggregate of its own name stays flat. The models stay separate and the flow is one way: request model, command, aggregate, result, response model. The endpoint maps in and out, and the handler returns a result, never the aggregate.

### Commands and Queries

CQRS separates the command and query models inside the one Application layer: a command mutates one aggregate through its domain repository, while a query projects read results without loading an aggregate and without writing. A use case whose record declares `consistency: atomic` over several aggregates is the declared exception, naming each aggregate it changes. A reaction answers its trigger by running a command or sending a notification, and a scheduled or manual job runs one command per item in its selection; neither travels the command pipeline itself. Publishing a post, for example, runs a `PublishPostCommand` through the handler and the domain repository, while reading a post runs a query that projects the stored data directly to its result.

### What Each Layer May Touch

| From | May not reference |
|:---|:---|
| Domain | Application, Infrastructure or WebApi types; persistence, web, mediator, logging and dependency injection namespaces |
| Application | Infrastructure or WebApi types; host transport types |
| WebApi | Domain types other than typed identifiers and shared-kernel values; repositories |
| A use case folder | Any type in another use case folder |
| A module | Another module's types outside its `Contracts/`, and any module outside its `dependsOn` |

A module that needs another module's data reads it through a read port in the owner's `Contracts/` that returns contract results keyed by typed id, or answers with its own query; it loads the other module's aggregate or repository in no case. A command changes only its own module's aggregates; an act on another module's aggregate is that module's use case, dispatched or triggered by an integration event. A reaction to another module's fact subscribes to the published integration event, never to the domain event.

### From Records to Code

Behavior records map onto code one way: each code element carries a mark naming the record it serves, under `litespec/core/system#point-code-and-tests-up-to-records`, and the folders mirror the records so a reader finds the code. Reactions, jobs, notifications, settings, and integration events map onto the code shapes the layer pages state. Nothing above Implementation is computed from code: code points up to the record it serves, and an undeclared edge or outcome is the defect.

## Standards

### Point Dependencies Inward

**ID:** `litespec/backend/architecture#point-dependencies-inward`

**Strength:** rule

**Statement:** A reference MUST point inward, so Domain references no outer layer and WebApi composes the process.

**Rationale:** Inner code names no outer code, so business rules compile and test without adapters or hosting. An outward reference would drag technical concerns into Domain or coordination into Infrastructure. Application coordinates Domain, Infrastructure implements the boundaries both declare, and the reference matrix in [Dependencies](../workspace/dependencies.md) applies exactly.

**Example:** Every reference points from an outer layer to an inner one, and Domain references none of the others.

![The four layers and their references: WebApi references Application and WebApi, and from Domain only typed identifiers and shared-kernel values; Infrastructure references Domain, Application, and Infrastructure; Application references Domain and Application; Domain references only Domain.](../assets/diagrams/backend-dependencies.svg)

**Reviewed for:** Which references point outward, away from Domain? Look at the reference graph in the code.

### Own Each Layer's Contract Types

**ID:** `litespec/backend/architecture#own-each-layers-contract-types`

**Strength:** rule

**Statement:** A layer MUST define its own outward contracts rather than reuse an inner layer's type, except for shared-kernel values and aggregate typed identifiers.

**Rationale:** When each layer defines its own outward contracts, a change to an inner shape stops at the boundary instead of rippling through every outer layer. Shared-kernel values cross because the kernel is shared by definition, and aggregate typed identifiers cross because messages need to name their targets.

**Reviewed for:** Which outward contracts reuse an inner layer's type outside shared-kernel values and aggregate typed identifiers? Look at the contracts in each layer's folders.

### Organize Code by Module, Aggregate and Use Case

**ID:** `litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case`

**Strength:** rule

**Statement:** Every module MUST organize its code as the module tree with one folder per declared aggregate and one folder per declared use case, using only the folder names its level allows.

**Rationale:** One folder per declared aggregate and one folder per declared use case let a reader move from a Domain aggregate to its handler, its repository, and its endpoint without a map. The closed folder names at each level keep that reading exact: a folder outside the allowed set is refused, so no second organization grows beside the tree. A module publishes what other modules may name in `Contracts/`, and use cases no single aggregate owns sit in `UseCases/`. An aggregate stays flat only when its plural name equals the single module name.

A folder named for a technical grouping rather than a module defeats the rule, because the next unrelated type fits it. `Shared`, `Common`, `Util`, `Helpers`, `Core`, and `Misc` inside a layer are the names that do this.

**Example:**

```text
# single-aggregate module whose aggregate name matches the module: the aggregate stays flat
Organizations/
  Contracts/
  Domain/  Application/  Infrastructure/  WebApi/
  CreateOrganization/      use case, flat: Command, Result, Handler, Validator, Endpoint, RequestModel, ResponseModel

# multi-aggregate module: module, then aggregate folder, then use case
Spending/
  Contracts/
  Expenses/
    Domain/  Application/  Infrastructure/  WebApi/
    SubmitExpense/
    ApproveExpense/
  Policies/
    Domain/  Application/  Infrastructure/  WebApi/
    DefinePolicy/

# module-level work beside the aggregates
Spending/
  UseCases/
  Reactions/
  Jobs/
```

The placement check asserts the folder names directly, because a rule only a reviewer checks stops holding at the first missed file.

```text
fail when a type path contains Shared, Common, Util, Helpers, Core, or Misc
```

**Reviewed for:** Which module, aggregate, or use case folders resolve to no declared record, use a folder name their level forbids, or carry a technical-grouping name? Look at the folder paths in each module.

### Place a Type at the Lowest Folder That Holds Its Consumers

**ID:** `litespec/backend/architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers`

**Strength:** rule

**Statement:** A type MUST live at the lowest folder containing every consumer that names it.

**Rationale:** Counting consumers gives each type exactly one folder, so placement needs no judgment call. The lowest folder is the use case folder when one use case names the type, the aggregate layer folder when several use cases of one aggregate name it, the module layer folder when several aggregates of one module name it, and the product layer folder when several modules name it with no module between them. A type another module names is published by its owning module in `Contracts/`; across modules a type is never shared. A use case file's layer comes from its role suffix, and a type only Infrastructure names is an Infrastructure type.

Two kinds of type travel with something else rather than by their own consumer count. A type that exists only to shape one contract's input or output stays with that contract, and a closed enumerated set stays whole.

The rule is mechanical, so placement is counted rather than judged. The product layer folders hold what remains after the count rather than a destination anyone chooses. A folder named for the absence of a reason (`Shared`, `Common`, `Util`, `Helpers`, `Core`, `Misc`) can refuse nothing. Such a folder accumulates until a tenth of the layer sits outside the module tree, unreviewed, and named for no aggregate.

Domain's shared kernel is the one product `Domain/` folder a solution keeps. Its membership is closed by `litespec/backend/architecture#own-each-layers-contract-types` rather than open to whatever needs a home. A service that coordinates several aggregates lives in the module that owns the aggregates it reads and writes, not with whichever module calls it first.

**Reviewed for:** Which types sit above the lowest folder holding every consumer that names them? Look at each type beside its consumers, watching for Shared, Common, Util, Helpers, Core, or Misc folders.

### Keep Implementation Types Hidden

**ID:** `litespec/backend/architecture#keep-implementation-types-hidden`

**Strength:** rule

**Statement:** An application MUST keep each handler, validator, definition, persistence implementation, and endpoint implementation inside its own layer and closed to extension.

**Rationale:** Hidden implementations keep each layer's surface to the contracts other layers actually use. A public handler invites direct construction that bypasses the pipeline, and an inheritable one invites overrides that split behavior across subclasses. Messages, results, and ports stay public only when another layer uses them, and automatic discovery is not a reason to widen visibility.

**Reviewed for:** Which handlers, validators, definitions, persistence implementations, or endpoint implementations escape their own layer or stay open to extension? Look at the implementations in each layer.

### Keep Business Invariants in Domain

**ID:** `litespec/backend/architecture#keep-business-invariants-in-domain`

**Strength:** rule

**Statement:** An aggregate or value object MUST be the only place that enforces a state transition or invariant.

**Rationale:** The aggregate or value object that owns data is the only code that sees every change to it. A rule duplicated in a handler drifts from the aggregate that owns it, and then the same action is allowed through one path and refused through another.

**Reviewed for:** Which state transitions or invariants run outside their aggregate or value object, or repeat in a handler? Look at the handlers beside the aggregates they call.

### Separate Command and Query Behavior

**ID:** `litespec/backend/architecture#separate-command-and-query-behavior`

**Strength:** rule

**Statement:** A command MUST mutate one aggregate through its domain repository while a query projects read results without loading an aggregate or writing.

**Rationale:** Reads that load aggregates pay for invariant enforcement no read needs, and writes that read from projections enforce rules against stale shapes. Commands mutate one aggregate through its domain repository, following `litespec/backend/application#keep-command-handlers-to-load-call-and-stage`, with a use case declaring `consistency: atomic` over several aggregates as the declared exception. Queries project read results without loading an aggregate and write nothing, following `litespec/backend/application#keep-query-handlers-free-of-writes`. An enabled persistence extension may replace the read boundary for named aggregate paths, but a command still enforces no invariant from a read projection.

**Reviewed for:** Which commands mutate outside one aggregate or skip the domain repository, or which queries load an aggregate or write? Look at the command and query handlers in Application.

### Read Another Module Only Through Its Contracts

**ID:** `litespec/backend/architecture#read-another-module-only-through-its-contracts`

**Strength:** rule

**Statement:** A module needing another module's data MUST read it only through a read port in the owner's `Contracts/` that returns contract results keyed by typed id, or through its own query.

**Rationale:** A read port in the owner's `Contracts/` keeps the owning module in charge of its data: the port returns contract results keyed by typed id, so the caller names targets without loading them. A module that loaded another module's aggregate or repository would couple its reads to the owner's consistency boundary and storage. Where no shared read fits, the module answers with its own query over data it already holds.

**Example:** In the Claimwick product, the reporting module reads expense totals through the spending module's read port in `Contracts/`, which returns contract results keyed by expense id; it loads no spending aggregate and no spending repository.

**Reviewed for:** Which cross-module reads bypass the owner's `Contracts/` read port or load another module's aggregate or repository? Look at the cross-module reads beside the `Contracts/` folders.

### Restrict Commands to Their Own Module Aggregates

**ID:** `litespec/backend/architecture#restrict-commands-to-their-own-module-aggregates`

**Strength:** rule

**Statement:** A command MUST change only the aggregates of its own module, so an act on another module's aggregate runs as that module's use case, dispatched or triggered by an integration event.

**Rationale:** Each module guards its own consistency boundaries, so a command from another module that wrote its aggregates would bypass the owning module's rules. An act on another module's aggregate is that module's use case: the caller dispatches it or triggers it through a published integration event, and the owning module enforces its own invariants.

**Example:** In the Claimwick product, a command that needs a policy change dispatches the policy module's use case or raises the integration event that triggers it; it writes no policy aggregate itself.

**Reviewed for:** Which commands change an aggregate outside their own module instead of dispatching that module's use case? Look at each command handler beside its module.

### Subscribe Across Modules Only to Integration Events

**ID:** `litespec/backend/architecture#subscribe-across-modules-only-to-integration-events`

**Strength:** rule

**Statement:** A reaction to another module's fact MUST subscribe only to the published integration event.

**Rationale:** A domain event stays inside the module that raised it, while the published integration event carries the fact outside. A reaction that subscribed to another module's domain event would couple to the owner's internals and break when they change. The integration event is the stable cross-module fact, so cross-module reactions answer it.

**Example:** In the Claimwick product, a reaction in the reporting module answers the published expense reimbursed integration event, never the spending domain event behind it.

**Reviewed for:** Which reactions subscribe to another module's domain event instead of its published integration event? Look at each reaction trigger beside the integration events.

### Declare the Execution Host for Work That Outlives a Request

**ID:** `litespec/backend/architecture#declare-the-execution-host-for-work-that-outlives-a-request`

**Strength:** rule

**Statement:** An application MUST declare the execution host that runs its durable dispatch, queue consumption, and scheduled work.

**Rationale:** The declared host is either the WebApi process or a Worker project. A rule that only restricts when a Worker exists leaves unsaid where the work runs without one, so a project can host background services inside WebApi while breaking no named rule and recording no decision. Naming the host makes the choice visible whichever way it goes.

The declaration lives in a Decision record following `litespec/core/model#record-each-technical-choice-as-a-decision`. Durable dispatch and queue consumption follow the outbox extension, and scheduled work follows the jobs extension.

The host is a composition decision, not a code-placement decision. The hosted operation classes live in Worker or Infrastructure, because they hold provider and scheduling concerns: in Worker when the project exists, otherwise in Infrastructure. The host registers them.

**Example:** A Decision naming WebApi as the execution host states that durable dispatch shares the request process, and states what an unavailable process costs.

**Reviewed for:** Which execution host runs durable dispatch, queue consumption, or scheduled work without a naming decision? Look for the decision record under `.litespec/intent/decisions/`.

### Create a Worker Project When Background Work Needs Its Own Process

**ID:** `litespec/backend/architecture#create-a-worker-project-when-background-work-needs-its-own-process`

**Strength:** rule

**Statement:** An application MUST create a Worker project when background work needs independent scaling, independent deployment, or isolation from request-path resource limits.

**Rationale:** A shared process couples scaling, deployment, and resource limits: a restart to deploy an endpoint change interrupts a dispatch loop, and request load and dispatch load compete for one thread pool where neither scales alone. When background work needs independent scaling, deployment, or isolation, it gets its own process. A product with one deployable unit and modest background load accepts that coupling deliberately, which is why the choice is declared rather than assumed.

**Example:** An application with an outbox dispatching a few messages each minute can run it in WebApi. An application dispatching continuously, or one whose dispatch needs to survive an endpoint deployment, creates the Worker project.

**Reviewed for:** Which background work needing its own process runs outside a Worker project? Look at the execution-host decision beside the solution project list.

### Test Structural Boundaries

**ID:** `litespec/backend/architecture#test-structural-boundaries`

**Strength:** rule

**Applies to:** artifact: test

**Statement:** An application MUST prove layer references, package boundaries, visibility, module folders, and aggregate inheritance with architecture tests.

**Rationale:** Layer references, package boundaries, visibility, module folders, and aggregate inheritance are invisible at runtime: the application runs the same with or without them. A structural rule that only a reviewer checks stops holding as soon as review misses one file, so architecture tests prove each one on every run.

**Reviewed for:** Which structural boundaries lack an architecture test, or fail the latest run? Look at the architecture tests beside the latest test run.

### Compose Each Process Explicitly

**ID:** `litespec/backend/architecture#compose-each-process-explicitly`

**Strength:** rule

**Statement:** A host MUST register its own transport services and call the Infrastructure entry point without building a second provider.

**Rationale:** Each host registers its own services and calls the shared Infrastructure entry point, so the full composition of a process is visible in one place. A second provider built mid-registration would resolve a different object graph than the host runs. WebApi and Worker are the only composition roots: Domain contains no registration code, and Application exposes contracts without resolving services.

**Reviewed for:** Which host entry points build a second provider or skip the Infrastructure entry point? Look at each host entry point.

## Conventions

### Keep Service Registration in Hosts

**ID:** `litespec/backend/architecture#keep-service-registration-in-hosts`

**Strength:** default

**Default:** Keep service registration in host entry points and host registration modules.

**Replacement:** A project names its own registration home in its documentation.

**Rationale:** Registration lives where the process is composed, so hosts and their registration modules hold every registration call. Domain and Application declare abstractions, so a registration call inside either inverts the dependency direction.

**Example:** One registration entry point per outer layer keeps the call order visible.

```text
infrastructure: register providers and options
webapi: register transport, authentication, and documentation
```

The Infrastructure entry point receives configuration and host environment values needed to bind and validate provider options. The WebApi entry point owns error details, authentication, authorization, endpoint discovery, and API documentation.

**Reviewed for:** Which service registration calls sit outside host entry points and host registration modules? Look at the registration calls in each layer.

## Reference Example

This informative example demonstrates `litespec/backend/architecture#point-dependencies-inward` and `litespec/backend/architecture#separate-command-and-query-behavior`.

Publishing a post follows this direction:

1. WebApi maps the HTTP request to `PublishPostCommand`.
2. Application loads the post through its domain repository.
3. Domain publishing enforces publication rules and raises the post-published event.
4. Infrastructure stages and commits the post through the command pipeline.
5. One event handler class answers the event through the reaction its record declares under `litespec/backend/application#keep-reaction-implementations-explicit`, carrying one mark naming that record.
