# Backend Architecture

**Stack:** any
**Concern:** application

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

A [module](../reference/glossary.md) is one business area organizing language, records, use cases, and ownership. A [use case](../reference/glossary.md) is one command or query with its access, results, and scenarios. A [reaction](../reference/glossary.md) answers an event without a request, and a [job](../reference/glossary.md) runs work on a schedule or by hand. Every module organizes its code as the module tree: one folder per declared aggregate, one `UseCases/` folder per aggregate and module with one folder per declared use case inside it, one `On{Fact}/` folder per reaction inside the module `UseCases/` folder, and the layer folders each level allows, so a reader moves from a Domain aggregate to its handler, its domain repository, and its endpoint without a map. A module publishes its public surface in its `Contracts/` folder sorted by kind: `Identifiers/`, `Exceptions/`, `IntegrationEvents/`, `Commands/` and `Ports/`.

### The Folder Tree

Every node (product root, module, aggregate) may hold the same layer folders. Each level has a closed set of folder names, so a check refuses anything else. Inside a layer folder, subfolders are free (`Application/Pipeline/`, `Infrastructure/Development/`), because the folder already states the layer and the lowest-folder rule already placed the type.

```text
{Product}/
  Domain/  Application/  Infrastructure/  WebApi/     shared kernel, pipeline positions, store setup and outbox
  Organizations/                                      module
    Contracts/
      Identifiers/  Exceptions/  IntegrationEvents/  Commands/  Ports/
    Domain/  Application/  Infrastructure/            types used by two or more aggregates; module registration
    WebApi/                                           transport types used by use cases of two or more aggregates
    Teams/                                            aggregate
      Domain/          Team, TeamState, ITeamRepository, Events/, Refusals/
      Application/     types used by two or more Team use cases
      Infrastructure/  TeamRepository
      WebApi/          transport types used by two or more Team use cases (headers, links, partial models)
      UseCases/
        CreateTeam/    use case, flat: Command, Result, Handler, Validator, Endpoint, RequestModel, ResponseModel
    UseCases/
      ReadTeamDirectory/      a use case no single aggregate owns
      OnTeamMemberRemoved/    a reaction
    Jobs/
```

| Level | Allowed folders |
|:---|:---|
| Product root | `Domain/`, `Application/`, `Infrastructure/`, `WebApi/`, one per declared module |
| Module | `Contracts/`, `Domain/`, `Application/`, `Infrastructure/`, `WebApi/`, `UseCases/`, `Jobs/`, one per declared aggregate |
| Module `Contracts/` | `Identifiers/`, `Exceptions/`, `IntegrationEvents/`, `Commands/`, `Ports/` |
| Module `UseCases/` | one per declared module use case, one `On{Fact}/` per reaction |
| Aggregate | `Domain/`, `Application/`, `Infrastructure/`, `WebApi/`, `UseCases/` |
| Aggregate `UseCases/` | one per declared use case |
| Use case, `On{Fact}/` | Files only |

A typed identifier another module names lives in its owner's `Contracts/Identifiers/` folder, so the one rule "another module only through `Contracts/`" covers identifiers too. Each module `Contracts/` folder sorts its public surface by kind: `Identifiers/`, `Exceptions/`, `IntegrationEvents/`, `Commands/` and `Ports/`. Shared-kernel values stay in the product `Domain/`. Product-wide HTTP plumbing (Problem Details, authentication, OpenAPI, rate limiting) stays in the host. The product `WebApi/` folder holds only what endpoints of two or more modules compile against, such as the endpoint contract and the current actor. `litespec/backend/architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers` decides between a use case folder and the aggregate, module and product layer folders; across modules a type is never shared, and the owning module publishes it in `Contracts/`. A use case file's layer comes from its role suffix in `litespec/workspace/naming#use-architectural-suffixes`: `Endpoint`, `RequestModel` and `ResponseModel` are WebApi, while `Command`, `Query`, `Result`, `Handler` and `Validator` are Application. An aggregate keeps its use cases in its `UseCases/` folder, one folder per use case, and a module keeps the use cases no single aggregate owns in its own `UseCases/` folder, beside one `On{Fact}/` folder per reaction. A module with one aggregate of its own name stays flat. The models stay separate and the flow is one way: request model, command, aggregate, result, response model. The endpoint maps in and out, and the handler returns a result, never the aggregate.

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

A module that needs another module's data reads it through a read port in the owner's `Contracts/Ports/` folder that returns contract results keyed by typed id, or answers with its own query; it loads the other module's aggregate or repository in no case. A command changes only its own module's aggregates; an act on another module's aggregate is that module's use case, dispatched or triggered by an integration event. A reaction to another module's fact subscribes to the published integration event, never to the domain event.

### From Records to Code

Behavior records map onto code one way: each code element carries a mark naming the record it serves, under `litespec/core/system#point-code-and-tests-up-to-records`, and the folders mirror the records so a reader finds the code. Reactions, jobs, notifications, settings, and integration events map onto the code shapes the layer pages state. Nothing above Implementation is computed from code: code points up to the record it serves, and an undeclared edge or outcome is the defect.

## Standards

### Point Dependencies Inward

**ID:** `litespec/backend/architecture#point-dependencies-inward`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A reference MUST point inward, so Domain references no outer layer and WebApi composes the process.

**Rationale:** Inner code names no outer code, so business rules compile and test without adapters or hosting. An outward reference would drag technical concerns into Domain or coordination into Infrastructure. Application coordinates Domain, Infrastructure implements the boundaries both declare, and the reference matrix in [Dependencies](../workspace/dependencies.md) applies exactly.

**Example:** Every reference points from an outer layer to an inner one, and Domain references none of the others.

![The four layers and their references: WebApi references Application and WebApi, and from Domain only typed identifiers and shared-kernel values; Infrastructure references Domain, Application, and Infrastructure; Application references Domain and Application; Domain references only Domain.](../assets/diagrams/backend-dependencies.svg)

**Reviewed for:** Which references point outward, away from Domain? Look at the reference graph in the code.

### Own Each Layer's Contract Types

**ID:** `litespec/backend/architecture#own-each-layers-contract-types`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A layer MUST define its own outward contracts rather than reuse an inner layer's type, except for shared-kernel values and aggregate typed identifiers.

**Rationale:** When each layer defines its own outward contracts, a change to an inner shape stops at the boundary instead of rippling through every outer layer. Shared-kernel values cross because the kernel is shared by definition, and aggregate typed identifiers cross because messages need to name their targets.

**Reviewed for:** Which outward contracts reuse an inner layer's type outside shared-kernel values and aggregate typed identifiers? Look at the contracts in each layer's folders.

### Organize Code by Module, Aggregate and Use Case

**ID:** `litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every module MUST organize its code as the module tree with one folder per declared aggregate, one `UseCases/` folder per aggregate and module, one folder per declared use case inside its `UseCases/` folder, and one `On{Fact}/` folder per reaction inside the module `UseCases/` folder, using only the folder names its level allows.

**Rationale:** One folder per declared aggregate and one folder per declared use case inside its `UseCases/` folder let a reader move from a Domain aggregate to its handler, its repository, and its endpoint without a map. The closed folder names at each level keep that reading exact: a folder outside the allowed set is refused, so no second organization grows beside the tree. A module publishes what other modules may name in its `Contracts/` folder sorted by kind, use cases no single aggregate owns sit in the module `UseCases/` folder, and each reaction sits in one `On{Fact}/` folder beside them. An aggregate stays flat only when its plural name equals the single module name.

A folder named for a technical grouping rather than a module defeats the rule, because the next unrelated type fits it. `Shared`, `Common`, `Util`, `Helpers`, `Core`, and `Misc` inside a layer are the names that do this.

**Example:**

```text
# single-aggregate module whose aggregate name matches the module: the aggregate stays flat
Organizations/
  Contracts/
    Identifiers/  Exceptions/  IntegrationEvents/  Commands/  Ports/
  Domain/  Application/  Infrastructure/  WebApi/
  UseCases/
    CreateOrganization/      use case, flat: Command, Result, Handler, Validator, Endpoint, RequestModel, ResponseModel

# multi-aggregate module: module, then aggregate folder, then UseCases, then use case
Spending/
  Contracts/
    Identifiers/  Exceptions/  IntegrationEvents/  Commands/  Ports/
  Expenses/
    Domain/  Application/  Infrastructure/  WebApi/
    UseCases/
      SubmitExpense/
      ApproveExpense/
  Policies/
    Domain/  Application/  Infrastructure/  WebApi/
    UseCases/
      DefinePolicy/

# module-level work beside the aggregates
Spending/
  UseCases/
    ReadExpenseDirectory/     a use case no single aggregate owns
    OnExpenseReimbursed/      a reaction
  Jobs/
```

The placement check asserts the folder names directly, because a rule only a reviewer checks stops holding at the first missed file.

```text
fail when a type path contains Shared, Common, Util, Helpers, Core, or Misc
```

**Reviewed for:** Which module, aggregate, or use case folders resolve to no declared record, use a folder name their level forbids, or carry a technical-grouping name? Look at the folder paths in each module.

### Place a Type at the Lowest Folder That Holds Its Consumers

**ID:** `litespec/backend/architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A type MUST live at the lowest folder containing every consumer that names it.

**Rationale:** Counting consumers gives each type exactly one folder, so placement needs no judgment call. The lowest folder is the use case folder when one use case names the type, the aggregate layer folder when several use cases of one aggregate name it, the module layer folder when several aggregates of one module name it, and the product layer folder when several modules name it with no module between them. A type another module names is published by its owning module in `Contracts/`; across modules a type is never shared. A use case file's layer comes from its role suffix, and a type only Infrastructure names is an Infrastructure type.

Two kinds of type travel with something else rather than by their own consumer count. A type that exists only to shape one contract's input or output stays with that contract, and a closed enumerated set stays whole.

The rule is mechanical, so placement is counted rather than judged. The product layer folders hold what remains after the count rather than a destination anyone chooses. A folder named for the absence of a reason (`Shared`, `Common`, `Util`, `Helpers`, `Core`, `Misc`) can refuse nothing. Such a folder accumulates until a tenth of the layer sits outside the module tree, unreviewed, and named for no aggregate.

Domain's shared kernel is the one product `Domain/` folder a solution keeps. Its membership is closed by `litespec/backend/architecture#own-each-layers-contract-types` rather than open to whatever needs a home. A service that coordinates several aggregates lives in the module that owns the aggregates it reads and writes, not with whichever module calls it first.

**Reviewed for:** Which types sit above the lowest folder holding every consumer that names them? Look at each type beside its consumers, watching for Shared, Common, Util, Helpers, Core, or Misc folders.

### Keep Implementation Types Hidden

**ID:** `litespec/backend/architecture#keep-implementation-types-hidden`

**Strength:** required

**Applies to:** artifact: command-handler, endpoint, query-handler, reaction, repository, validator

**Statement:** An application MUST keep each handler, validator, definition, persistence implementation, and endpoint implementation inside its own layer and closed to extension.

**Rationale:** Hidden implementations keep each layer's surface to the contracts other layers actually use. A public handler invites direct construction that bypasses the pipeline, and an inheritable one invites overrides that split behavior across subclasses. Messages, results, and ports stay public only when another layer uses them, and automatic discovery is not a reason to widen visibility.

**Reviewed for:** Which handlers, validators, definitions, persistence implementations, or endpoint implementations escape their own layer or stay open to extension? Look at the implementations in each layer.

### Keep Business Invariants in Domain

**ID:** `litespec/backend/architecture#keep-business-invariants-in-domain`

**Strength:** required

**Applies to:** artifact: aggregate, command-handler, job, query-handler, reaction, value-object

**Statement:** An aggregate or value object MUST be the only place that enforces a state transition or invariant.

**Rationale:** The aggregate or value object that owns data is the only code that sees every change to it. A rule duplicated in a handler drifts from the aggregate that owns it, and then the same action is allowed through one path and refused through another.

**Reviewed for:** Which state transitions or invariants run outside their aggregate or value object, or repeat in a handler? Look at the handlers beside the aggregates they call.

### Separate Command and Query Behavior

**ID:** `litespec/backend/architecture#separate-command-and-query-behavior`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler

**Statement:** A command MUST mutate one aggregate through its domain repository while a query projects read results without loading an aggregate or writing.

**Rationale:** Reads that load aggregates pay for invariant enforcement no read needs, and writes that read from projections enforce rules against stale shapes. Commands mutate one aggregate through its domain repository, following `litespec/backend/application#keep-command-handlers-to-load-call-and-stage`, with a use case declaring `consistency: atomic` over several aggregates as the declared exception. Queries project read results without loading an aggregate and write nothing, following `litespec/backend/application#keep-query-handlers-free-of-writes`. An enabled persistence extension may replace the read boundary for named aggregate paths, but a command still enforces no invariant from a read projection.

**Reviewed for:** Which commands mutate outside one aggregate or skip the domain repository, or which queries load an aggregate or write? Look at the command and query handlers in Application.

### Read Another Module Only Through Its Contracts

**ID:** `litespec/backend/architecture#read-another-module-only-through-its-contracts`

**Strength:** required

**Applies to:** artifact: command-handler, port, query-handler

**Statement:** A module needing another module's data MUST read it only through a read port in the owner's `Contracts/Ports/` folder that returns contract results keyed by typed id, or through its own query.

**Rationale:** A read port in the owner's `Contracts/Ports/` folder keeps the owning module in charge of its data: the port returns contract results keyed by typed id, so the caller names targets without loading them. A module that loaded another module's aggregate or repository would couple its reads to the owner's consistency boundary and storage. Where no shared read fits, the module answers with its own query over data it already holds.

**Example:** In the Claimwick product, the reporting module reads expense totals through the spending module's read port in `Contracts/Ports/`, which returns contract results keyed by expense id; it loads no spending aggregate and no spending repository.

**Reviewed for:** Which cross-module reads bypass the owner's `Contracts/Ports/` read port or load another module's aggregate or repository? Look at the cross-module reads beside the `Contracts/` folders.

### Restrict Commands to Their Own Module Aggregates

**ID:** `litespec/backend/architecture#restrict-commands-to-their-own-module-aggregates`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** A command MUST change only the aggregates of its own module, so an act on another module's aggregate runs as that module's use case, dispatched or triggered by an integration event.

**Rationale:** Each module guards its own consistency boundaries, so a command from another module that wrote its aggregates would bypass the owning module's rules. An act on another module's aggregate is that module's use case: the caller dispatches it or triggers it through a published integration event, and the owning module enforces its own invariants.

**Example:** In the Claimwick product, a command that needs a policy change dispatches the policy module's use case or raises the integration event that triggers it; it writes no policy aggregate itself.

**Reviewed for:** Which commands change an aggregate outside their own module instead of dispatching that module's use case? Look at each command handler beside its module.

### Subscribe Across Modules Only to Integration Events

**ID:** `litespec/backend/architecture#subscribe-across-modules-only-to-integration-events`

**Strength:** required

**Applies to:** artifact: reaction

**Statement:** A reaction to another module's fact MUST subscribe only to the published integration event.

**Rationale:** A domain event stays inside the module that raised it, while the published integration event carries the fact outside. A reaction that subscribed to another module's domain event would couple to the owner's internals and break when they change. The integration event is the stable cross-module fact, so cross-module reactions answer it.

**Example:** In the Claimwick product, a reaction in the reporting module answers the published expense reimbursed integration event, never the spending domain event behind it.

**Reviewed for:** Which reactions subscribe to another module's domain event instead of its published integration event? Look at each reaction trigger beside the integration events.

### Declare the Execution Host for Work That Outlives a Request

**ID:** `litespec/backend/architecture#declare-the-execution-host-for-work-that-outlives-a-request`

**Strength:** required

**Applies to:** artifact: record:decision

**Statement:** An application MUST declare the execution host that runs its durable dispatch, queue consumption, and scheduled work.

**Rationale:** The declared host is either the WebApi process or a Worker project. A rule that only restricts when a Worker exists leaves unsaid where the work runs without one, so a project can host background services inside WebApi while breaking no named rule and recording no decision. Naming the host makes the choice visible whichever way it goes.

The declaration lives in a Decision record following `litespec/core/model#record-each-technical-choice-as-a-decision`. Durable dispatch and queue consumption follow the outbox extension, and scheduled work follows the jobs extension.

The host is a composition decision, not a code-placement decision. The hosted operation classes live in Worker or Infrastructure, because they hold provider and scheduling concerns: in Worker when the project exists, otherwise in Infrastructure. The host registers them.

**Example:** A Decision naming WebApi as the execution host states that durable dispatch shares the request process, and states what an unavailable process costs.

**Reviewed for:** Which execution host runs durable dispatch, queue consumption, or scheduled work without a naming decision? Look for the decision record under `.litespec/intent/decisions/`.

### Create a Worker Project When Background Work Needs Its Own Process

**ID:** `litespec/backend/architecture#create-a-worker-project-when-background-work-needs-its-own-process`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An application MUST create a Worker project when background work needs independent scaling, independent deployment, or isolation from request-path resource limits.

**Rationale:** A shared process couples scaling, deployment, and resource limits: a restart to deploy an endpoint change interrupts a dispatch loop, and request load and dispatch load compete for one thread pool where neither scales alone. When background work needs independent scaling, deployment, or isolation, it gets its own process. A product with one deployable unit and modest background load accepts that coupling deliberately, which is why the choice is declared rather than assumed.

**Example:** An application with an outbox dispatching a few messages each minute can run it in WebApi. An application dispatching continuously, or one whose dispatch needs to survive an endpoint deployment, creates the Worker project.

**Reviewed for:** Which background work needing its own process runs outside a Worker project? Look at the execution-host decision beside the solution project list.

### Test Structural Boundaries

**ID:** `litespec/backend/architecture#test-structural-boundaries`

**Strength:** required

**Applies to:** artifact: test

**Statement:** A structural boundary that no LiteSpec analyzer or `model check` finding covers MUST have an architecture test.

**Rationale:** Layer references, package boundaries, visibility, module folders, and aggregate inheritance are invisible at runtime: the application runs the same with or without them, so each one needs mechanical proof on every run. The analyzers and `model check` already give that proof for the boundaries in the table below. `LS1001`, for one, reports an outer-layer call inside a method body, which a test over compiled signatures misses. An architecture test that repeats a covered boundary holds a second copy of a rule the build or the check already enforces, and the copy drifts while the analyzer or the check keeps reporting the breach. Architecture tests hold the rest: a boundary the table covers only in part, and a structural decision the project itself made that no LiteSpec tool names.

| Structural boundary | Covered by |
|:---|:---|
| Domain names an outer-layer type or namespace, including a persistence, web, mediator, logging, or dependency injection namespace | `LS1001` from `dotnet build` |
| Application or WebApi names a type outside its layer | `LS2018` from `dotnet build` |
| A module names another module's type outside its `Contracts/` folder | `LS2020` from `dotnet build` |
| A use case folder names a type declared in another use case folder | `LS2019` from `dotnet build` |
| A handler, validator, definition, persistence implementation, or endpoint implementation is not `internal sealed` | `LS2001` from `dotnet build` |
| A folder sits outside the closed set its level allows, or a module folder names no declared module | `folder-invalid` and `module-undeclared` from `litespec model check` |
| A marked code element, test, or folder sits outside the folder its record names, or a folder names no declared record | `mark-folder-drift` and `code-placement-drift` from `litespec model check` |
| A folder or type carries a technical grouping name such as `Shared`, `Common`, or `Helpers` outside the declared shared kernel | `name-reason-missing` from `litespec model check` |
| An aggregate exposes no `State` property typed as an abstract record, or a state case is not sealed | `LS1003` and `LS1004` from `dotnet build` |
| Domain declares an enum for a closed set | `LS1002` from `dotnet build` |
| A command handler holds or calls through a document session or database context | `LS2004` from `dotnet build` |
| A handler throws a forbidden refusal or takes the authorization guard | `LS2012` from `dotnet build` |

**Example:** In the Claimwick product, no architecture test repeats `LS1001`, `LS1002`, `LS2004`, or `LS2012`: the build reports a Domain file that names a Marten type, an enum declared in Domain, a command handler that holds a document session, and a handler that throws a forbidden refusal or takes the authorization guard. The architecture suite keeps a test that no module reaches another module outside its `dependsOn`, which `LS2020` allows when the named type sits in the other module's `Contracts/` folder.

**Reviewed for:** Which structural boundaries have no LiteSpec analyzer or `model check` finding in the table and no architecture test, or which architecture tests repeat a boundary the table covers? Look at the architecture tests beside the analyzer and `model check` runs.

### Compose Each Process Explicitly

**ID:** `litespec/backend/architecture#compose-each-process-explicitly`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A host MUST register its own transport services and call the Infrastructure entry point without building a second provider.

**Rationale:** Each host registers its own services and calls the shared Infrastructure entry point, so the full composition of a process is visible in one place. A second provider built mid-registration would resolve a different object graph than the host runs. WebApi and Worker are the only composition roots: Domain contains no registration code, and Application exposes contracts without resolving services.

**Reviewed for:** Which host entry points build a second provider or skip the Infrastructure entry point? Look at each host entry point.

## Conventions

### Keep Service Registration in Hosts

**ID:** `litespec/backend/architecture#keep-service-registration-in-hosts`

**Strength:** default

**Applies to:** artifact: any

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
