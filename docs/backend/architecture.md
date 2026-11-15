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

A [module](../reference/glossary.md) is one business area organizing language, records, use cases, and ownership. A [use case](../reference/glossary.md) is one command or query with its access, results, and scenarios. A [reaction](../reference/glossary.md) answers an event without a request, and a [job](../reference/glossary.md) runs work on a schedule or by hand. Every module organizes its code as the module tree: one folder per declared aggregate, one `UseCases/` folder per aggregate and module with one folder per declared use case inside it, and the layer folders each level allows, so a reader moves from a Domain aggregate to its handler, its domain repository, and its endpoint without a map. A use case folder holds the reactions and jobs that run it beside its handler. A module publishes its public surface in its `Contracts/` folder sorted by kind: `Identifiers/`, `IntegrationEvents/` and `Ports/`.

### The Folder Tree

Every node (product root, module, aggregate, use case) holds the layer folders its level allows. Each level has a closed set of folder names, so a check refuses anything else. Every folder name is a product noun (a module, an aggregate), a business act (a use case), one of the four layers, or a record kind the product speaks (`Notifications`, `ExternalSystems`, `ReadModels`, and inside Domain `Events` and `Exceptions`). No folder groups by mechanism, such as `Commands`, `Queries`, `Handlers` or `Models`, or by caller. Inside a layer folder, subfolders are free (`Application/Pipeline/`, `Infrastructure/Development/`), because the folder already states the layer and the lowest-folder rule already placed the type.

```text
{Product}/
  Domain/  Application/  Infrastructure/  WebApi/     shared kernel, pipeline positions, store setup and outbox
  Organizations/                                      module
    Contracts/
      Identifiers/  IntegrationEvents/  Ports/
    Domain/  Application/  Infrastructure/            behavior used by two or more aggregates; module registration
    Teams/                                            aggregate
      Domain/          Team, TeamState, TeamRepository (contract), Events/, Exceptions/
      Application/     ports and rules used by two or more Team use cases
      Infrastructure/  TeamRepository
      UseCases/
        CreateTeam/                use case
          Application/             Command, Result, Handler, Validator, and the reactions and jobs that run it
          WebApi/                  Endpoint, RequestModel, ResponseModel
        ArchiveTeam/
          Application/             ArchiveTeamOnOrganizationClosedHandler: a reaction that runs this use case
    UseCases/
      ReadTeamDirectory/      a use case no single aggregate owns
    Notifications/
      TeamWelcome/            a reaction that only sends this notification
    ExternalSystems/
      DirectorySync/          a reaction that only calls this external system
    ReadModels/
      MemberAccounts/         a read model of another module's published facts
```

| Level | Allowed folders |
|:---|:---|
| Product root | `Domain/`, `Application/`, `Infrastructure/`, `WebApi/`, one per declared module |
| Module | `Contracts/`, `Domain/`, `Application/`, `Infrastructure/`, `UseCases/`, `Notifications/`, `ExternalSystems/`, `ReadModels/`, one per declared aggregate |
| Module `Contracts/` | `Identifiers/`, `IntegrationEvents/`, `Ports/` |
| Module `UseCases/` | one per declared module use case |
| Module `Notifications/` | one per notification a reaction or job of the module only sends |
| Module `ExternalSystems/` | one per external system a reaction or job of the module only calls |
| Module `ReadModels/` | one per declared read model of the module |
| Aggregate | `Domain/`, `Application/`, `Infrastructure/`, `UseCases/` |
| Aggregate `UseCases/` | one per declared use case |
| Use case | `Application/`, and `WebApi/` when the use case has an endpoint |
| Use case `Application/` and `WebApi/`, and each folder under `Notifications/`, `ExternalSystems/` and `ReadModels/` | Files only |

A typed identifier another module names lives in its owner's `Contracts/Identifiers/` folder, so the one rule "another module only through `Contracts/`" covers identifiers too. Each module `Contracts/` folder sorts its public surface by kind: `Identifiers/`, `IntegrationEvents/` and `Ports/`. A module publishes no command and no refusal: another module triggers its acts through an integration event its reaction answers, and refuses with its own refusals. Shared-kernel values stay in the product `Domain/`. Product-wide HTTP plumbing (Problem Details, authentication, OpenAPI, rate limiting) stays in the host. The product `WebApi/` folder holds only what endpoints of two or more modules compile against, such as the endpoint contract and the current actor. `litespec/backend/architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers` decides between the aggregate, module and product layer folders for behavior such as a port, a rule calculation or an adapter; across modules a type is never shared, and the owning module publishes it in `Contracts/`. A message, result, result part, transport model or mapping belongs to one use case and sits in its folder under `litespec/backend/architecture#give-each-use-case-its-own-models`. Inside a use case, the file's role suffix in `litespec/workspace/naming#use-architectural-suffixes` picks its layer folder: `Endpoint`, `RequestModel` and `ResponseModel` sit in `WebApi/`, while `Command`, `Query`, `Result`, `Handler`, `Validator` and the use case's own steps sit in `Application/`. A trigger lives with what it runs: the event handler of a reaction and the class of a job sit in the `Application/` folder of the use case they run, or, when they run none, in the module `Notifications/` folder of the notification they send or the `ExternalSystems/` folder of the system they call. An aggregate keeps its use cases in its `UseCases/` folder, one folder per use case, and a module keeps the use cases no single aggregate owns in its own `UseCases/` folder. A module with one aggregate of its own name stays flat. The models stay separate and the flow is one way: request model, command, aggregate, result, response model. The endpoint maps in and out, and the handler returns a result, never the aggregate.

### Commands and Queries

CQRS separates the command and query models inside the one Application layer: a command mutates one aggregate through its domain repository, while a query projects read results without loading an aggregate and without writing. A use case whose record declares `consistency: atomic` over several aggregates is the declared exception, naming each aggregate it changes. A reaction answers its trigger by running one command or sending a notification, and a scheduled or one-off job runs one command per item in its selection; neither travels the command pipeline itself. A handler sends no command, query or event: only an endpoint, a reaction's event handler and a job send a message. Publishing a post, for example, runs a `PublishPostCommand` through the handler and the domain repository, while reading a post runs a query that projects the stored data directly to its result.

### What Each Layer May Touch

| From | May not reference |
|:---|:---|
| Domain | Application, Infrastructure or WebApi types; persistence, web, mediator, logging and dependency injection concerns |
| Application | Infrastructure or WebApi types; host transport types |
| WebApi | Domain types other than typed identifiers and shared-kernel values; repositories |
| A use case folder | Any type in another use case folder |
| A module | Another module's types outside its `Contracts/`, and any module outside its `dependsOn` |

A module that needs to know another module's state keeps its own read model of it, built from the owner's published integration events. A command whose decision needs the owner's current value asks one question through a decision port in the owner's `Contracts/Ports/` folder, and a screen that only shows another module's data calls the owner's own query. A module loads the other module's aggregate or repository in no case. A command changes only its own module's aggregates; an act on another module's aggregate is that module's use case, triggered by an integration event its reaction answers. A reaction to another module's fact subscribes to the published integration event, never to the domain event.

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

**Statement:** A layer MUST define its own outward contracts rather than reuse an inner layer's type or another module's contract type, except for shared-kernel values and aggregate typed identifiers.

**Rationale:** When each layer defines its own outward contracts, a change to an inner shape stops at the boundary instead of rippling through every outer layer. Shared-kernel values cross because the kernel is shared by definition, and aggregate typed identifiers cross because messages need to name their targets. The module boundary works the same way: a use case maps another module's port answer at its edge and returns it in no result or transport model of its own, so the owner can change its answer without changing the reader's contract.

**Reviewed for:** Which outward contracts reuse an inner layer's type, or return another module's port answer, outside shared-kernel values and aggregate typed identifiers? Look at the results and transport models in each layer's folders.

### Organize Code by Module, Aggregate and Use Case

**ID:** `litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every module MUST organize its code as the module tree with one folder per declared aggregate, one `UseCases/` folder per aggregate and module, and one folder per declared use case inside its `UseCases/` folder holding its layer folders, using only the folder names its level allows.

**Rationale:** One folder per declared aggregate and one folder per declared use case inside its `UseCases/` folder let a reader move from a Domain aggregate to its handler, its repository, and its endpoint without a map. The closed folder names at each level keep that reading exact: a folder outside the allowed set is refused, so no second organization grows beside the tree. A module publishes what other modules may name in its `Contracts/` folder sorted by kind, use cases no single aggregate owns sit in the module `UseCases/` folder, and each reaction and job sits in the folder of the use case it runs. A folder named for a fact would name the commands of other use cases and pull them out of their folders, and a fact's reactions span modules, so no fact folder ever showed them all. An aggregate stays flat only when its plural name equals the single module name. Settled by [decision/0063-place-each-trigger-with-what-it-runs](decision/0063-place-each-trigger-with-what-it-runs).

A folder named for a technical grouping rather than a module defeats the rule, because the next unrelated type fits it. `Shared`, `Common`, `Util`, `Helpers`, `Core`, and `Misc` inside a layer are the names that do this.

**Example:**

```text
# single-aggregate module whose aggregate name matches the module: the aggregate stays flat
Organizations/
  Contracts/
    Identifiers/  IntegrationEvents/  Ports/
  Domain/  Application/  Infrastructure/
  UseCases/
    CreateOrganization/
      Application/           Command, Result, Handler, Validator
      WebApi/                Endpoint, RequestModel, ResponseModel

# multi-aggregate module: module, then aggregate folder, then UseCases, then use case
Spending/
  Contracts/
    Identifiers/  IntegrationEvents/  Ports/
  Expenses/
    Domain/  Application/  Infrastructure/
    UseCases/
      SubmitExpense/
        Application/  WebApi/
      ErasePersonalData/
        Application/          the erase command, its handler, and the job that runs it
  Policies/
    Domain/  Application/  Infrastructure/
    UseCases/
      DefinePolicy/
        Application/  WebApi/

# module-level work beside the aggregates
Spending/
  UseCases/
    ReadExpenseDirectory/     a use case no single aggregate owns
  Notifications/
    ApprovalReminder/         the reaction that only sends the approval reminder
```

The placement check asserts the folder names directly, because a rule only a reviewer checks stops holding at the first missed file.

```text
fail when a type path contains Shared, Common, Util, Helpers, Core, or Misc
```

**Reviewed for:** Which module, aggregate, use case, notification, external-system or read-model folders resolve to no declared record, use a folder name their level forbids, or carry a technical-grouping name? Look at the folder paths in each module.

### Place a Type at the Lowest Folder That Holds Its Consumers

**ID:** `litespec/backend/architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A type that is not a use case's own model MUST live at the lowest folder containing every consumer that names it.

**Rationale:** Counting consumers gives each type exactly one folder, so placement needs no judgment call. The lowest folder is the use case folder when one use case names the type, the aggregate layer folder when several use cases of one aggregate name it, the module layer folder when several aggregates of one module name it, and the product layer folder when several modules name it with no module between them. A type another module names is published by its owning module in `Contracts/`; across modules a type is never shared. A use case file's layer comes from its role suffix, and a type only Infrastructure names is an Infrastructure type.

Two kinds of type travel with something else rather than by their own consumer count. A type that exists only to shape one contract's input or output stays with that contract, and a closed enumerated set stays whole. A use case's own models are never counted: a message, result, result part, transport model or mapping belongs to one use case under `litespec/backend/architecture#give-each-use-case-its-own-models`, so the count places behavior such as ports, rule calculations and adapters.

The rule is mechanical, so placement is counted rather than judged. The product layer folders hold what remains after the count rather than a destination anyone chooses. A folder named for the absence of a reason (`Shared`, `Common`, `Util`, `Helpers`, `Core`, `Misc`) can refuse nothing. Such a folder accumulates until a tenth of the layer sits outside the module tree, unreviewed, and named for no aggregate.

Domain's shared kernel is the one product `Domain/` folder a solution keeps. Its membership is closed by `litespec/backend/architecture#own-each-layers-contract-types` rather than open to whatever needs a home. A service that coordinates several aggregates lives in the module that owns the aggregates it reads and writes, not with whichever module calls it first.

**Reviewed for:** Which types sit above the lowest folder holding every consumer that names them? Look at each type beside its consumers, watching for Shared, Common, Util, Helpers, Core, or Misc folders.

### Give Each Use Case Its Own Models

**ID:** `litespec/backend/architecture#give-each-use-case-its-own-models`

**Strength:** required

**Applies to:** artifact: use-case, endpoint

**Statement:** A message, result, result part, transport model or mapping MUST belong to exactly one use case and sit in its folder, so two use cases that need the same shape each declare their own.

**Rationale:** A shared shape couples two slices that change for different reasons: one use case adds a field, and the other starts returning it. Counting consumers for shapes moved messages and results out of their use cases into aggregate and module folders as soon as a second caller named them, until those folders held more models than behavior. A copy costs a few lines that never drift into a wrong answer, because a shape decides nothing. A rule is different: two copies of a rule disagree, so a rule keeps one home in Domain under `litespec/backend/architecture#repeat-coordination-never-a-rule`. Settled by [decision/0063-place-each-trigger-with-what-it-runs](decision/0063-place-each-trigger-with-what-it-runs).

**Example:** The submit-expense and approve-expense use cases each return the expense they acted on; each declares its own result with the fields its caller needs, and neither result lives in `Expenses/Application/`.

**Checked by:** `dotnet build` reports `LS2019` for a use case folder naming a type in another use case folder, and `litespec model check` reports `code-placement-drift` for a message, result or transport-model file outside a use-case folder.

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

### Repeat Coordination, Never a Rule

**ID:** `litespec/backend/architecture#repeat-coordination-never-a-rule`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler, reaction, job

**Statement:** A step two use cases share MUST live once in Domain, or behind the owning module's port, when it decides something, while each use case holds its own copy of a step that only coordinates.

**Rationale:** A step that decides, such as whether an expense needs a receipt, is a rule: two copies drift until one path allows what the other refuses, so the rule lives once on its aggregate, in a domain service marked with its `rule/`, or in the owning module behind its port. A step that only coordinates, such as mapping a request or loading two records in order, decides nothing, and a shared coordinator couples the use cases that call it. Once each follow-on act runs from a reaction, most shared coordinators disappear: the act raises its fact, and the other use case answers it.

**Example:** The receipt threshold check lives once on the expense aggregate, while submit-expense and attach-receipt each map their own request.

**Reviewed for:** Which types in an aggregate or module Application folder coordinate rather than decide, and which use-case steps repeat a decision another step makes? Look at the shared Application types beside the use cases that call them.

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

**Statement:** A module needing another module's data MUST read it through its own read model built from the owner's integration events, through a decision port in the owner's `Contracts/Ports/` folder, or, on a screen, through the owner's own query.

**Rationale:** A module that loaded another module's aggregate or repository would couple its reads to the owner's consistency boundary and storage. A general reader in the owner's `Contracts/` is the same coupling one step removed: it grows a method per caller, and no record says which fields cross. A read model names each field it copies from a published event, under `litespec/backend/application#read-another-modules-state-through-a-read-model`. A decision port answers one current-value question under `litespec/backend/application#ask-another-module-one-question-through-a-decision-port`. A screen joins each owner's query under `litespec/frontend/data#compose-a-screen-from-each-owners-query`.

**Example:** In the Claimwick product, the reporting module reads reimbursed expenses through its own `read-model/reporting.reimbursed-expense`, filled from the spending module's expense-reimbursed integration event; it loads no spending aggregate, no spending repository and no spending reader.

**Reviewed for:** Which cross-module reads load another module's aggregate or repository, or call a reader that returns the owner's state for any caller instead of a read model, a decision port or a screen joining owner queries? Look at the cross-module reads beside the `Contracts/Ports/` folders.

### Restrict Commands to Their Own Module Aggregates

**ID:** `litespec/backend/architecture#restrict-commands-to-their-own-module-aggregates`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** A command MUST change only the aggregates of its own module, so an act on another module's aggregate runs as that module's use case, triggered by an integration event its reaction answers.

**Rationale:** Each module guards its own consistency boundaries, so a command from another module that wrote its aggregates would bypass the owning module's rules. An act on another module's aggregate is that module's use case: the caller raises its own fact, the owning module's reaction answers the published integration event, and the owning module enforces its own invariants. A module publishes no command, so no caller can send one across the boundary.

**Example:** In the Claimwick product, a command that needs a policy change raises its own fact, and the policy module's reaction to the published integration event runs the policy use case; the command writes no policy aggregate and sends no policy command.

**Reviewed for:** Which commands change an aggregate outside their own module, or send another module's command, instead of raising a fact that module's reaction answers? Look at each command handler beside its module.

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

The host is a composition decision, not a code-placement decision. A job class lives with the use case it runs, and the outbox dispatcher and the scheduler that drive reactions and jobs live in Worker when the project exists, otherwise in Infrastructure. The host registers them.

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

**Rationale:** Layer references, package boundaries, visibility, module folders, and aggregate inheritance are invisible at runtime: the application runs the same with or without them, so each one needs mechanical proof on every run. The analyzers and `model check` already give that proof for the boundaries that a stack page names as checked and for the rows in the table below. A check over method bodies, for one, reports an outer-layer call inside a method, which a test over compiled signatures misses. An architecture test that repeats a covered boundary holds a second copy of a rule the build or the check already enforces, and the copy drifts while the analyzer or the check keeps reporting the breach. Architecture tests hold the rest: a boundary the checks cover only in part, and a structural decision the project itself made that no LiteSpec tool names.

| Structural boundary | Covered by |
|:---|:---|
| A folder sits outside the closed set its level allows, or a module folder names no declared module | `folder-invalid` and `module-undeclared` from `litespec model check` |
| A marked code element, test, or folder sits outside the folder its record names, or a folder names no declared record | `mark-folder-drift` and `code-placement-drift` from `litespec model check` |
| A folder or type carries a technical grouping name such as `Shared`, `Common`, or `Helpers` outside the declared shared kernel | `name-reason-missing` from `litespec model check` |

**Example:** In the Claimwick product, no architecture test repeats the checks that report an outer-layer type in Domain, an enum in Domain, a command handler that holds a write session, or a handler that authorizes itself; the stack's checks report each one. The architecture suite keeps a test that no module reaches another module outside its `dependsOn`, and a type in the other module's `Contracts/` folder stays allowed.

**Reviewed for:** Which structural boundaries have no analyzer or `model check` finding and no architecture test, or which architecture tests repeat a boundary a check covers? Look at the architecture tests beside the analyzer and `model check` runs.

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
5. One event handler answers the event through the reaction its record declares under `litespec/backend/application#keep-reaction-implementations-explicit`, carrying one mark naming that record.
