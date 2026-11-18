# Backend Architecture

**Stack:** any
**Concern:** application

The backend is a modular monolith of four layers, with its driven ports in a top-level `Ports/` folder beside them: Domain holds business rules, Application coordinates use cases, Infrastructure holds technical adapters, and WebApi holds HTTP transport. A use case is one command or query with its access, results, and scenarios; a reaction answers an event by running a command or sending a notification; and a job runs work on a schedule or by hand.

Start here when building the backend. The rules below cover the layers, the direction references point between them, and the module tree that organizes every layer; each layer page states what its layer owns.

## Concepts

### The Four Layers

A layer is the role a type plays, regardless of which folder or project holds it. The four layers form one inward-pointing chain: each layer references only layers closer to Domain, so business rules compile and test without adapters or hosting.

| Layer | Holds | May reference |
|:---|:---|:---|
| Domain | Aggregates, state records, value objects, domain events, domain exceptions, repository contracts | Domain |
| Application | Commands, queries, results, handlers, validators, pipeline positions | Domain, Application, Ports |
| Infrastructure | Repositories, adapters, mappings, module registration | Domain, Application, Infrastructure, Ports |
| WebApi | Endpoints, request and response models, transport mappings | Application, WebApi, and from Domain only typed identifiers and Kernel values |

Each module also holds a `Ports/` folder beside the four layer folders with its driven ports, as The Module Tree states: Application and Infrastructure reference it, and Domain and WebApi never do.

Layers are not deployment units: the layers deploy together as one modular monolith.

### Modules and Use Cases

A [module](../reference/glossary.md) is one business area: it organizes the language, records, use cases, and ownership of that area. A [use case](../reference/glossary.md) is one command or query with its access, results, and scenarios. A [reaction](../reference/glossary.md) answers an event without a request, and a [job](../reference/glossary.md) runs work on a schedule or by hand.

Every module organizes its code as the module tree: one `Contracts/` folder for its public surface, one `Domain/` folder holding one folder per declared aggregate in the plural, one `Ports/` folder holding one folder per port of the module, and the `Application/` and `WebApi/` layer folders holding one plural aggregate folder with one folder per use case inside it, plus one folder per module-level use case, with `Infrastructure/` holding `Persistence/` and one adapter folder per port. A use case that touches two aggregates sits in the aggregate folder its record names: the record tree already groups use cases by aggregate, and the code mirrors the records.

A module publishes its public surface in its `Contracts/` folder: data only, grouped by aggregate. Each `Contracts/{Aggregates}/` folder holds the identifier another module carries, the facts another module consumes, and one folder per public query with its message and result. Other modules reference nothing outside it.

### The Module Tree

The tree fixes every type's address, so a reader moves from a Domain aggregate to its handler, its domain repository, and its endpoint without a map. Every node of the tree, from the product root down to a use case folder, holds the folders its level allows. Each level defines a closed set of folder names, and a check enforces it.

Every folder name is a product noun (a module or an aggregate), a business act (a use case), one of the four layers, or a record kind the product speaks (`Ports`, `Notifications`, `ExternalSystems`, `ReadModels`, and inside Domain `Events` and `Exceptions`). No folder groups by mechanism, such as `Commands`, `Queries`, `Handlers`, or `Models`, or by caller.

Inside a product layer folder, subfolders stay free (`Application/Pipeline/`, `Infrastructure/Development/`), because the folder states the layer and the lowest-folder rule already placed the type.

```text
{Product}/
  Domain/  Application/  Infrastructure/  WebApi/     Kernel values, pipeline positions, store setup and outbox
  Organizations/                                      module
    Contracts/
      Teams/             TeamId, TeamMemberAdded, ReadTeam/ (query and result)
      ReadTeamDirectory/ a public module-level query
    Domain/
      Teams/             Team, TeamState, TeamRepository (contract), Events/, Exceptions/
    Application/
      Teams/
        CreateTeam/              Command, Result, Handler, Validator, and the reactions and jobs that run it
        ArchiveTeam/             ArchiveTeamOnOrganizationClosedHandler: a reaction that runs this use case
      ReadTeamDirectory/         a module-level use case: no aggregate folder
      Notifications/
        TeamWelcome/             a reaction that only sends this notification
      ExternalSystems/
        DirectorySync/           a reaction that only calls this external system
      ReadModels/
        MemberAccounts/          a read model of another module's published facts
    Ports/
      {Port}/                    I{Port} and only the types its signatures name
    Infrastructure/
      Persistence/               TeamRepository, store contributions, read-model readers
      {Port}/                    the adapter implementing Ports/{Port}/, same folder name
    WebApi/
      Teams/
        CreateTeam/              Endpoint, RequestModel, ResponseModel
```

| Level | Allowed folders |
|:---|:---|
| Product root | `Domain/`, `Application/`, `Infrastructure/`, `WebApi/`, one per declared module |
| Module | `Contracts/`, `Domain/`, `Application/`, `Ports/`, `Infrastructure/`, `WebApi/` |
| Module `Contracts/` | one folder per aggregate with anything public, in the plural, and one folder per public module-level query; no files |
| Module `Contracts/{Aggregates}/` | the aggregate identifier when another module carries it, the facts another module consumes, and one folder per public query of that aggregate |
| Module `Contracts/{Aggregates}/{UseCase}/` and each public module-level query folder | the query message, its result, and the result parts; no other files |
| Module `Domain/` | one folder per declared aggregate, in the plural |
| Module `Domain/{Aggregates}/` | `Events/`, `Exceptions/`, files |
| Module `Application/` | one folder per declared aggregate that has use cases, one folder per declared module-level use case, `Notifications/`, `ExternalSystems/`, `ReadModels/` |
| Module `Application/{Aggregates}/`, `WebApi/{Aggregates}/` | one folder per use case declared under that aggregate; no files |
| Module `Ports/` | one folder per port of the module; no files |
| Module `Ports/{Port}/` | the interface `I{Port}` and only the types its member signatures name |
| Module `Infrastructure/` | `Persistence/`, one folder per port the module implements |
| Module `WebApi/` | the aggregate and module-level use case folders of use cases reached over HTTP |
| Module `Application/Notifications/` | one per notification a reaction or job of the module only sends |
| Module `Application/ExternalSystems/` | one per external system a reaction or job of the module only calls |
| Module `Application/ReadModels/` | one per declared read model of the module |
| Each use case folder and each folder under `Notifications/`, `ExternalSystems/` and `ReadModels/` | Files only |

A module shares with other modules only through its `Contracts/` folder. A typed identifier another module carries lives in the aggregate folder of its owner's `Contracts/` folder, so the one rule covers identifiers too; an identifier no other module carries lives in the aggregate's `Domain/` folder.

A module publishes no command, no refusal, and no interface. To trigger an act on another module's aggregate, a module raises its own fact, and the owning module's reaction answers the published module event; each module refuses with its own refusals. To read another module's current value, a module asks through its port to that module, answered by the provider's public query.

Kernel values stay in the product `Domain/`. Product-wide technical capabilities stay in the product `Application/`, `Infrastructure/`, and `WebApi/` folders: persistence setup, messaging, authorization, auditing, and HTTP conventions. The product `WebApi/` folder holds only what endpoints of two or more modules compile against, such as the endpoint contract and the current actor.

Two rules place each type inside the tree. `litespec/backend/architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers` places behavior such as a rule calculation or an adapter in the use case, module, or product layer folder; every port of a module lives in its `Ports/` folder instead, whether one use case or many call it. Across modules a type is never shared, and the owning module publishes it in `Contracts/`. A message, result, result part, transport model, or mapping belongs to one use case and sits in its folder under `litespec/backend/architecture#give-each-use-case-its-own-models`.

A file takes its layer from its top folder in the module. The role suffix in `litespec/workspace/naming#use-architectural-suffixes` states the file's role inside that layer: `Endpoint`, `RequestModel`, and `ResponseModel` sit in `WebApi/{Aggregates}/{UseCase}/`, and `Command`, `Query`, `Result`, `Handler`, `Validator`, and the use case's own steps sit in `Application/{Aggregates}/{UseCase}/`. Port interfaces sit in `Ports/{Port}/`, with their adapters in `Infrastructure/{Port}/`. A module-level use case drops the aggregate segment in each.

Each trigger sits with what it runs. The event handler of a reaction and the class of a job sit in the `Application/` folder of the use case they run or, when they run none, in the `Application/Notifications/` folder of the notification they send or the `Application/ExternalSystems/` folder of the system they call. The `Application/ExternalSystems/` folder holds those trigger classes; the adapters they call live in the port adapter folders of `Infrastructure/`.

A module keeps one plural aggregate folder holding one folder per use case inside each layer that serves it; a module-level use case sits directly in each layer that serves it. A module with one aggregate of its own name stays flat: its `Domain/` folder holds the aggregate's types directly.

Models stay separate, and data flows one way: request model, command, aggregate, result, response model. The endpoint maps the request in and the result out, and the handler returns a result, not the aggregate.

### Commands and Queries

CQRS separates the command and query models inside the one Application layer: commands change state, queries read it. A command changes one aggregate through its domain repository, while a query projects read results without loading an aggregate and without writing.

The declared exception is a use case whose record declares `consistency: atomic` over several aggregates, and it names each aggregate it changes.

A reaction answers its trigger by running one command or sending a notification, and a scheduled or one-off job runs one command per item in its selection; neither travels the command pipeline itself. A handler sends no command, query, or event: only an endpoint, a reaction's event handler, and a job send a message.

Publishing a post runs a `PublishPostCommand` through the handler and the domain repository. Reading a post runs a query that projects the stored data directly to its result.

### Reference Limits Between Layers

| Code in | May not reference |
|:---|:---|
| Domain | Application, Infrastructure or WebApi types; persistence, web, mediator, logging and dependency injection concerns |
| Application | Infrastructure or WebApi types; host transport types |
| `Ports/` | Application, Infrastructure or WebApi types |
| WebApi | Domain types other than typed identifiers and Kernel values; repositories |
| A use case folder | Any type in another use case folder |
| A module | Another module's types outside its `Contracts/`, and any module outside its `dependsOn` |

Across modules, each read takes one of three paths:

- A module that needs another module's current value asks through its port to that module, answered by the provider's public query.
- A module that needs another module's data reshaped and kept locally keeps its own read model of it, built from the owner's published module events.
- A screen that only shows another module's data calls the owner's own query.

A module loads the other module's aggregate or repository in no case.

Writes stay inside the module too. A command changes only its own module's aggregates; an act on another module's aggregate is that module's use case, triggered by a module event its reaction answers. A reaction to another module's fact subscribes to the published module event, not to the domain event behind it.

### From Records to Code

Behavior records map onto code in one direction: each code element carries a mark pointing to the record it serves, under `litespec/core/system#point-code-and-tests-up-to-records`, and the folders mirror the records so a reader finds the code from the record. The code holds one plural aggregate folder with one folder per use case inside each layer that serves it, and one folder per module-level use case directly in each layer that serves it; each stack page states the mapping from a record path to its code folder. Reactions, jobs, notifications, settings, module events, and integration events map onto the code shapes the layer pages state.

Nothing above Implementation is computed from code: code points up to the record it serves, and an undeclared edge or outcome is the defect.

## Standards

### Point Dependencies Inward

**ID:** `litespec/backend/architecture#point-dependencies-inward`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A reference MUST point inward, so Domain references no outer layer and WebApi composes the process.

**Rationale:** Inner code references no outer code, so business rules compile and test without adapters or hosting. An outward reference would place technical concerns in Domain or coordination in Infrastructure. Application coordinates Domain, Infrastructure implements the boundaries both declare, and the reference matrix in [Dependencies](../workspace/dependencies.md) applies as written.

**Example:** Every reference points from an outer layer to an inner one: Application references Domain, Infrastructure references both, and Domain references neither.

![The four layers and their references: WebApi references Application and WebApi, and from Domain only typed identifiers and Kernel values; Infrastructure references Domain, Application, and Infrastructure; Application references Domain and Application; Domain references only Domain.](../assets/diagrams/backend-dependencies.svg)

**Reviewed for:** Is any outer layer referenced from an inner one? Look at the reference graph in the code.

### Own Each Layer's Contract Types

**ID:** `litespec/backend/architecture#own-each-layers-contract-types`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A layer MUST define its own outward contracts rather than reuse an inner layer's type or another module's contract type, except for Kernel values and aggregate typed identifiers.

**Rationale:** When each layer defines its own outward contracts, a change to an inner shape stops at the boundary instead of forcing changes in every outer layer. Kernel values cross layer boundaries because the Kernel is shared by definition, and aggregate typed identifiers cross them because messages need to identify their targets.

The module boundary works the same way: the adapter in `Infrastructure/{Provider}/` maps the provider's public query answer into the consumer's own types, and a use case returns it in no result or transport model of its own, so the owner can change its answer without changing the reader's contract.

**Reviewed for:** Is any inner layer's type reused as an outward contract, or any other module's port answer returned as-is, outside Kernel values and aggregate typed identifiers? Look at the results and transport models in each layer's folders.

### Produce File Formats in Infrastructure

**ID:** `litespec/backend/architecture#produce-file-formats-in-infrastructure`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler, job

**Statement:** Production of a file format, rendering, and parsing MUST live in Infrastructure behind a module port, even when the code uses only the base class library.

**Rationale:** An archive, CSV, PDF, or audit-file writer that only uses the base class library passes every layer check while doing infrastructure work, and the use case folder fills with code the use case only calls. The module declares the port in its `Ports/` folder and hands the gathered input to it; Infrastructure implements the writer, split by section, with the small shapes as private nested types. A use case folder then reads as the use case: its message, handler, validator, definitions, and result.

**Example:** The export-expenses use case declares `IExpensesArchive` in `Ports/ExpensesArchive/` and hands it the gathered lines; the writer lives in `Infrastructure/ExpensesArchive/`, split into one writer per archive section.

**Reviewed for:** Does any Application folder produce a file format, render, or parse, instead of calling an Infrastructure port? Look for archive, CSV, JSON-writer, PDF, and rendering libraries in the Application folders.

### Organize Code by Module, Aggregate and Use Case

**ID:** `litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every module MUST organize its code as the module tree with one `Contracts/` folder, one `Domain/` folder holding one folder per declared aggregate in the plural, one `Ports/` folder holding one folder per port of the module, the `Application/` and `WebApi/` layer folders holding one plural aggregate folder with one folder per use case inside it plus one folder per module-level use case, and one `Infrastructure/` folder holding `Persistence/` and one adapter folder per port, using only the folder names its level allows.

**Rationale:** One plural folder per declared aggregate under `Domain/`, one folder per port under `Ports/` with its adapter in `Infrastructure/`, and one plural aggregate folder with one folder per use case inside each layer that serves it fix every type's address: a reader moves from a Domain aggregate to its handler, its repository, and its endpoint without a map. The closed folder names at each level keep that reading exact, because a folder outside the allowed set is refused and no second organization grows beside the tree.

A module publishes what other modules may reference in its `Contracts/` folder grouped by aggregate, every use case sits in the aggregate folder its record names or directly in the layer folders when its record is module-level, and each reaction and job sits in the folder of the use case it runs. A folder named for a fact would reference the commands of other use cases and pull them out of their folders, and a fact's reactions span modules, so no fact folder could ever show them all.

An aggregate stays flat only when its plural name equals the single module name. Settled by [decision/0063-place-each-trigger-with-what-it-runs](decision/0063-place-each-trigger-with-what-it-runs).

The module tree shape is settled by [decision/0107-kernel-platform-and-one-project-per-module](decision/0107-kernel-platform-and-one-project-per-module).

A folder named for a technical grouping rather than a module defeats the rule: the next unrelated type fits it too. `Shared`, `Common`, `Util`, `Helpers`, `Core`, and `Misc` inside a layer are the names that do this.

**Example:**

```text
# single-aggregate module whose aggregate name matches the module: Domain stays flat, layer folders nest under the plural aggregate folder
Organizations/
  Contracts/
    Organizations/            the identifier, the facts, and the public queries of the organization aggregate
  Domain/                     the organization aggregate directly: Organization, OrganizationState
  Application/
    Organizations/
      CreateOrganization/     Command, Result, Handler, Validator
  WebApi/
    Organizations/
      CreateOrganization/     Endpoint, RequestModel, ResponseModel

# multi-aggregate module: Domain holds one plural folder per aggregate, each layer holds one plural aggregate folder with one folder per use case inside it
Spending/
  Contracts/
    Expenses/                 ExpenseId, ExpenseReimbursed, ReadExpense/ (query and result)
    Policies/                 the identifier, the facts, and the public queries of the policy aggregate
  Domain/
    Expenses/                 Expense, ExpenseState, the expense repository contract
    Policies/
  Application/
    Expenses/
      SubmitExpense/
      ErasePersonalData/      the erase command, its handler, and the job that runs it
    ReadExpenseDirectory/     a module-level use case: no aggregate folder
    Notifications/
      ApprovalReminder/       the reaction that only sends the approval reminder
  Ports/
    {Port}/                   I{Port} and only the types its signatures name
  Infrastructure/
    Persistence/              repositories, store contributions, read-model readers
    {Port}/                   the adapter implementing Ports/{Port}/, same folder name
  WebApi/
    Expenses/
      SubmitExpense/
```

The placement check asserts these folder names directly: a rule that only a reviewer checks stops holding at the first missed file.

```text
fail when a type path contains Shared, Common, Util, Helpers, Core, or Misc
```

**Reviewed for:** Is any module, aggregate, use case, notification, external-system, or read-model folder without a declared record, with a forbidden folder name, or with a technical-grouping name? Look at the folder paths in each module.

### Place a Type at the Lowest Folder That Holds Its Consumers

**ID:** `litespec/backend/architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A type that is not a use case's own model MUST live at the lowest folder containing every consumer that names it.

**Rationale:** Counting consumers gives each type exactly one folder, so placing a type needs no judgment. The lowest folder is the use case folder when one use case references the type, the module folder when several use cases of one module reference it, and the product layer folder when several modules reference it with no module between them.

A domain rule several use cases of one aggregate reference lives in that aggregate's `Domain/{Aggregates}/` folder. Every port of a module lives in the module `Ports/` folder instead, whether one use case or many call it, with its adapter in `Infrastructure/`, under `litespec/backend/application#declare-every-port-in-the-module-ports-folder`, so the consumer count never places a port. A type another module references is published by its owning module in `Contracts/`; across modules a type is never shared. A use case file's layer comes from its top folder in the module, and a type only Infrastructure references is an Infrastructure type.

Two kinds of type follow something else instead of their own consumer count. A type that exists only to shape one contract's input or output stays with that contract, and a closed enumerated set stays whole. A use case's own models are never counted: a message, result, result part, transport model, or mapping belongs to one use case under `litespec/backend/architecture#give-each-use-case-its-own-models`, so the count places behavior such as ports, rule calculations, and adapters. A type's private shapes stay private beside it instead of moving up the count.

The product layer folders hold what remains after the count; they are not a destination anyone chooses. A folder named for no reason (`Shared`, `Common`, `Util`, `Helpers`, `Core`, `Misc`) accepts every type placed in it, until a tenth of the layer sits outside the module tree, unreviewed and belonging to no aggregate.

The Kernel values are the one product `Domain/` folder a solution keeps. Its membership is closed by `litespec/backend/architecture#own-each-layers-contract-types`; it is not a place for types that fit nowhere else. A service that coordinates several aggregates lives in the module that owns the aggregates it reads and writes, not with whichever module calls it first.

**Reviewed for:** Is any type sitting above the lowest folder holding every consumer that references it? Look at each type beside its consumers, watching for Shared, Common, Util, Helpers, Core, or Misc folders.

### Give Each Use Case Its Own Models

**ID:** `litespec/backend/architecture#give-each-use-case-its-own-models`

**Strength:** required

**Applies to:** artifact: use-case, endpoint

**Statement:** A message, result, result part, transport model or mapping MUST belong to exactly one use case and sit in its folder, so two use cases that need the same shape each declare their own.

**Rationale:** A shared shape couples two use cases that change for different reasons: one use case adds a field, and the other starts returning it. Counting consumers for shapes moved messages and results out of their use cases into module folders as soon as a second caller referenced them, until those folders held more models than behavior.

A copied shape costs a few lines and cannot drift into a wrong answer, because a shape decides nothing. A rule is different: two copies of a rule disagree, so a rule lives in one place in Domain under `litespec/backend/architecture#repeat-coordination-never-a-rule`. Settled by [decision/0063-place-each-trigger-with-what-it-runs](decision/0063-place-each-trigger-with-what-it-runs).

Kernel values are the exception: money, typed identifiers, dates, and closed sets the Kernel owns are referenced directly from any layer the layer table allows, and copying one into a use case folder breaks the rule.

**Example:** The submit-expense and approve-expense use cases each return the expense they acted on. Each declares its own result with the fields its caller needs, and neither result lives in `Domain/Expenses/`.

**Checked by:** `dotnet build` reports `LS2019` for a use case folder referencing a type in another use case folder, and `litespec model check` reports `code-placement-drift` for a message, result or transport-model file outside a use-case folder.

### Keep Implementations Hidden and Closed

**ID:** `litespec/backend/architecture#keep-implementations-hidden-and-closed`

**Strength:** required

**Applies to:** artifact: command-handler, endpoint, query-handler, reaction, repository, validator

**Statement:** An application MUST keep each handler, validator, definition, persistence implementation, and endpoint implementation inside its own layer and closed to extension.

**Rationale:** Hidden implementations limit each layer's surface to the contracts other layers actually use. A public handler allows direct construction that bypasses the pipeline, and an inheritable one allows overrides that split behavior across subclasses. Messages, results, and ports stay public only when another layer uses them, and automatic discovery is not a reason to widen visibility.

**Reviewed for:** Is any handler, validator, definition, persistence implementation, or endpoint implementation visible outside its own layer, or inheritable? Look at the implementations in each layer.

### Keep Business Invariants in Domain

**ID:** `litespec/backend/architecture#keep-business-invariants-in-domain`

**Strength:** required

**Applies to:** artifact: aggregate, command-handler, job, query-handler, reaction, value-object

**Statement:** An aggregate or value object MUST be the only place that enforces a state transition or invariant.

**Rationale:** The aggregate or value object that owns data is the only code that sees every change to it. A rule duplicated in a handler drifts from the aggregate that owns it: one path allows what the other refuses.

**Reviewed for:** Is any state transition or invariant enforced outside its aggregate or value object, or repeated in a handler? Look at the handlers beside the aggregates they call.

### Repeat Coordination, Never a Rule

**ID:** `litespec/backend/architecture#repeat-coordination-never-a-rule`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler, reaction, job

**Statement:** A step two use cases share MUST live once in Domain, or behind the owning module's port, when it decides something, while each use case holds its own copy of a step that only coordinates.

**Rationale:** A step that decides, such as whether an expense needs a receipt, is a rule: two copies drift until one path allows what the other refuses, so the rule lives once on its aggregate, in a domain service marked with its `rule/`, or in the owning module behind its port.

A step that only coordinates, such as mapping a request or loading two records in order, decides nothing, and a shared coordinator couples the use cases that call it. Once a reaction runs each follow-on act, most shared coordinators disappear: the act raises its fact, and the other use case answers it.

**Example:** The receipt threshold check lives once on the expense aggregate, while submit-expense and attach-receipt each map their own request.

**Reviewed for:** Is any shared Application type only coordinating, or any use-case step repeating a decision made elsewhere? Look at the shared Application types beside the use cases that call them.

### Separate Command and Query Behavior

**ID:** `litespec/backend/architecture#separate-command-and-query-behavior`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler

**Statement:** A command MUST mutate one aggregate through its domain repository while a query projects read results without loading an aggregate or writing.

**Rationale:** Loading aggregates for a read enforces invariants the read does not need, and writing from projections enforces rules against stale shapes. Commands change one aggregate through its domain repository, following `litespec/backend/application#keep-command-handlers-to-load-call-and-stage`, with a use case declaring `consistency: atomic` over several aggregates as the declared exception.

Queries project read results without loading an aggregate and write nothing, following `litespec/backend/application#keep-query-handlers-free-of-writes`. An enabled persistence extension may replace the read boundary for named aggregate paths, but a command still enforces no invariant from a read projection.

**Reviewed for:** Is any command changing outside one aggregate or skipping the domain repository, or any query loading an aggregate or writing? Look at the command and query handlers in Application.

### Read Another Module Only Through Its Contracts

**ID:** `litespec/backend/architecture#read-another-module-only-through-its-contracts`

**Strength:** required

**Applies to:** artifact: command-handler, port, query-handler

**Statement:** A module needing another module's data MUST read it through its port to that module answered by the provider's public query, through its own read model built from the owner's module events, or, on a screen, through the owner's own query.

**Rationale:** A module that loaded another module's aggregate or repository would couple its reads to the owner's consistency boundary and storage. A general reader in the owner's `Contracts/` is the same coupling one step removed: it grows a method per caller, and no record says which fields cross.

The three allowed paths each state what crosses. The consumer port states what one consumer needs in its own types, and the provider's public query answers with the owner's current value, under `litespec/backend/application#reach-another-module-through-one-port-per-provider`. A read model states each field it copies from a published event, under `litespec/backend/application#read-another-modules-state-through-a-read-model`. A screen joins each owner's query, under `litespec/frontend/data#compose-a-screen-from-each-owners-query`.

**Example:** In the Claimwick product, the reporting module reads the current expense through its port to the spending module, answered by spending's public read-expense query. It keeps its own `read-model/reporting.reimbursed-expense`, filled from the spending module's expense-reimbursed module event, only for the reshaped rows its lists show. It loads no spending aggregate, no spending repository, and no spending reader.

**Reviewed for:** Does any cross-module read load another module's aggregate or repository, or call a general reader instead of a consumer port, a read model, or a screen joining owner queries? Look at the cross-module reads beside the consumer ports.

### Send Only Queries Across Modules

**ID:** `litespec/backend/architecture#send-only-queries-across-modules`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler, reaction, job

**Statement:** A module MUST send only public queries across a module boundary, so a command is never public and a change in one module reaches another through a fact and a reaction there.

**Rationale:** A synchronous cross-module write couples the caller to the provider's consistency boundary, storage, and uptime: the caller commits only when the provider commits, and the provider's rules run inside the caller's transaction. A public query only reads: the provider answers with its current value and nothing else, and its record lists the consuming modules in `reachedThrough`, so every synchronous edge is declared. A change the caller needs in the owning module is a fact that module's reaction answers, under `litespec/backend/architecture#restrict-commands-to-their-own-module-aggregates`. Settled by [decision/0109-module-contracts-publish-data](decision/0109-module-contracts-publish-data).

**Example:** In the Claimwick product, the reporting module sends spending's public read-expense query through its port to the spending module. When reporting needs an expense changed, spending's reaction to reporting's published fact runs the spending use case; reporting sends no spending command.

**Reviewed for:** Does any module send another module's command, or publish a command in its Contracts? Look at the cross-module sends beside the Contracts projects.

### Restrict Commands to Their Own Module Aggregates

**ID:** `litespec/backend/architecture#restrict-commands-to-their-own-module-aggregates`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** A command MUST change only the aggregates of its own module, so an act on another module's aggregate runs as that module's use case, triggered by a module event its reaction answers.

**Rationale:** Each module guards its own consistency boundaries, so a command from another module that wrote its aggregates would bypass the owning module's rules. An act on another module's aggregate is that module's use case instead: the caller raises its own fact, the owning module's reaction answers the published module event, and the owning module enforces its own invariants. A module publishes no command, so no caller can send one across the boundary.

**Example:** In the Claimwick product, a command that needs a policy change raises its own fact, and the policy module's reaction to the published module event runs the policy use case. The command writes no policy aggregate and sends no policy command.

**Reviewed for:** Is any command writing an aggregate outside its own module, or sending another module's command, instead of raising a fact that module's reaction answers? Look at each command handler beside its module.

### Subscribe Across Modules Only to Module Events

**ID:** `litespec/backend/architecture#subscribe-across-modules-only-to-module-events`

**Strength:** required

**Applies to:** artifact: reaction

**Statement:** A reaction to another module's fact MUST subscribe only to the published module event.

**Rationale:** A domain event stays inside the module that raised it, while the published module event carries the fact to the other modules of the product. A reaction that subscribed to another module's domain event would couple to the owner's internals and break when they change. The module event is the stable cross-module fact, so cross-module reactions answer it. A fact from outside the product still arrives as an inbound integration event.

**Example:** In the Claimwick product, a reaction in the reporting module answers the published expense reimbursed module event, not the spending domain event behind it.

**Reviewed for:** Is any reaction triggered by another module's domain event instead of its published module event? Look at each reaction trigger beside the module events.

### Name the Host for Work That Outlives a Request

**ID:** `litespec/backend/architecture#name-the-host-for-work-that-outlives-a-request`

**Strength:** required

**Applies to:** artifact: record:decision

**Statement:** An application MUST declare the execution host that runs its durable dispatch, queue consumption, and scheduled work.

**Rationale:** The declared host is either the WebApi process or a Worker project. A rule that only constrains a Worker, when one exists, leaves unsaid where the work runs without one, so a project can host background services inside WebApi while breaking no named rule and recording no decision. Naming the host makes the choice visible either way.

The declaration lives in a Decision record, following `litespec/core/model#record-each-technical-choice-as-a-decision`. Durable dispatch and queue consumption follow the outbox extension, and scheduled work follows the jobs extension.

The host is a composition decision, not a code-placement decision. A job class lives with the use case it runs. The outbox dispatcher and the scheduler that drive reactions and jobs live in Worker when that project exists, otherwise in Infrastructure, and the host registers them.

**Example:** A Decision naming WebApi as the execution host states that durable dispatch shares the request process and what that costs when the process is unavailable.

**Reviewed for:** Does any durable dispatch, queue consumption, or scheduled work run without a Decision naming its execution host? Look for the decision record under `.litespec/intent/decisions/`.

### Create a Worker Project When Background Work Needs Its Own Process

**ID:** `litespec/backend/architecture#create-a-worker-project-when-background-work-needs-its-own-process`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An application MUST create a Worker project when background work needs independent scaling, independent deployment, or isolation from request-path resource limits.

**Rationale:** A shared process couples scaling, deployment, and resource limits. A restart to deploy an endpoint change interrupts a dispatch loop, and request load and dispatch load compete for one thread pool, so neither scales alone.

When background work needs independent scaling, deployment, or isolation, it gets its own process. A product with one deployable unit and modest background load accepts that coupling deliberately, which is why the choice is declared rather than assumed.

**Example:** An application whose outbox dispatches a few messages each minute can run it in WebApi. An application that dispatches continuously, or whose dispatch needs to survive an endpoint deployment, creates the Worker project.

**Reviewed for:** Is any background work that needs independent scaling, deployment, or isolation running outside a Worker project? Look at the execution-host decision beside the solution project list.

### Test Structural Boundaries

**ID:** `litespec/backend/architecture#test-structural-boundaries`

**Strength:** required

**Applies to:** artifact: test

**Statement:** A structural boundary that no LiteSpec analyzer or `model check` finding covers MUST have an architecture test.

**Rationale:** Layer references, package boundaries, visibility, module folders, and aggregate inheritance are invisible at runtime: the application runs the same with or without them. Without mechanical proof on every run, each one erodes unnoticed.

The analyzers and `model check` already prove the boundaries a stack page names as checked, and the rows in the table below. A check over method bodies, for example, reports an outer-layer call inside a method, which a test over compiled signatures misses.

An architecture test that repeats a covered boundary holds a second copy of a rule the build or the check already enforces, and the copy drifts while the analyzer or the check keeps reporting the breach. Architecture tests hold the rest: a boundary the checks cover only in part, and a structural decision the project itself made that no LiteSpec tool names.

| Structural boundary | Covered by |
|:---|:---|
| A folder sits outside the closed set its level allows, or a module folder names no declared module | `record-placement-invalid` from `litespec model check` |
| A marked code element, test, or folder sits outside the folder its record names, or a folder names no declared record | `mark-folder-drift` and `code-placement-drift` from `litespec model check` |
| A folder or type carries a technical grouping name such as `Shared`, `Common`, or `Helpers` outside the declared Kernel | `name-reason-missing` from `litespec model check` |

**Example:** In the Claimwick product, no architecture test repeats the checks that report an outer-layer type in Domain, an enum in Domain, a command handler that holds a write session, or a handler that authorizes itself, because the stack's checks report each one. The architecture suite keeps a test that no module reaches another module outside its `dependsOn`, with a type in the other module's `Contracts/` folder staying allowed.

**Reviewed for:** Does any structural boundary lack both an analyzer or `model check` finding and an architecture test, or does any architecture test repeat a boundary a check covers? Look at the architecture tests beside the analyzer and `model check` runs.

### Compose Each Process Explicitly

**ID:** `litespec/backend/architecture#compose-each-process-explicitly`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A host MUST register its own transport services and call the shared composition entry points without building a second provider.

**Rationale:** Each host registers its own services and calls the shared entry points, so the full composition of a process is visible in one place. A second provider built mid-registration resolves a different object graph than the one the host runs.

WebApi and Worker are the only composition roots. Domain contains no registration code, and Application exposes contracts without resolving services.

**Reviewed for:** Is any host composing its process without calling the shared entry points, or building a second provider mid-registration? Look at each host entry point.

## Conventions

### Keep Service Registration in Hosts

**ID:** `litespec/backend/architecture#keep-service-registration-in-hosts`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep service registration in host entry points and host registration modules.

**Replacement:** A project names its own registration home in its documentation.

**Rationale:** Registration lives where the process is composed, so hosts and their registration modules hold every registration call. A registration call inside Domain or Application inverts the dependency direction, because those layers declare abstractions.

**Example:** Each outer layer keeps one registration entry point, so the call order stays visible.

```text
infrastructure: register providers and options
webapi: register transport, authentication, and documentation
```

The Infrastructure entry point receives the configuration and host environment values needed to bind and validate provider options. The WebApi entry point owns error details, authentication, authorization, endpoint discovery, and API documentation.

**Reviewed for:** Is any service registered outside host entry points and host registration modules? Look at the registration calls in each layer.

## Reference Example

This informative example demonstrates `litespec/backend/architecture#point-dependencies-inward` and `litespec/backend/architecture#separate-command-and-query-behavior`.

Publishing a post moves inward through the layers:

1. WebApi maps the HTTP request to `PublishPostCommand`.
2. Application loads the post through its domain repository.
3. Domain publishing enforces the publication rules and raises the post-published event.
4. Infrastructure stages and commits the post through the command pipeline.
5. One event handler answers the event for the reaction its record declares, following `litespec/backend/application#answer-each-reaction-in-one-marked-class`, carrying one mark naming that record.
