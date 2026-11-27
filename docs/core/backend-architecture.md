# Backend Architecture

**Stack:** any
**Concern:** application

The backend is a modular monolith of four layers with driven ports beside them: Domain holds business rules, Application coordinates use cases, Infrastructure holds technical adapters, and WebApi holds HTTP transport. This page is for developers who place types, folders, and module relationships in a LiteSpec backend. A type in the wrong layer or module still compiles, so the rules below fix every address and the direction of every reference.

## Concepts

### The Four Layers

A layer is the role a type plays, regardless of which folder or project holds it. The four layers form one inward-pointing chain: each layer references layers closer to Domain, so business rules compile and test without adapters or hosting.

| Layer | Holds | References |
|:---|:---|:---|
| Domain | Aggregates, state records, value objects, domain events, domain exceptions, repository contracts | Domain |
| Application | Commands, queries, results, handlers, validators, pipeline positions | Domain, Application, Ports |
| Infrastructure | Repositories, adapters, mappings, module registration | Domain, Application, Infrastructure, Ports |
| WebApi | Endpoints, request and response models, transport mappings | Application, WebApi, and from Domain: typed identifiers and Kernel values |

Each module also holds a `Ports/` folder beside the four layer folders with its driven ports. Application and Infrastructure reference it, while Domain and WebApi stay clear of it. The layers deploy together as one modular monolith.

| Code in | Stays clear of |
|:---|:---|
| Domain | Application, Infrastructure or WebApi types; persistence, web, mediator, logging and dependency injection concerns |
| Application | Infrastructure or WebApi types; host transport types |
| `Ports/` | Application, Infrastructure or WebApi types |
| WebApi | Domain types other than typed identifiers and Kernel values; repositories |
| A use case folder | Any type in another use case folder |
| A module | Another module's types outside its `Contracts/`, and any module outside its `dependsOn`; a knowledge set project |
| A knowledge model | A module project, and a knowledge set project |
| A knowledge set | A module project, and any project other than its model and the Kernel |

### The Knowledge Layer

A knowledge area is a backend concept beside the module layer. It holds the rules that govern how operational records behave: rates, limits, deadlines, and templates that vary by a dimension such as a country or a branch. An area has one knowledge model, which owns the types and the lookups, and one knowledge set per variant, which owns the values alone. An area that varies by nothing keeps its one table in the model.

Knowledge code lives in `src/Knowledge/` beside `src/Modules/`: one project per model named `<Product>.<Area>`, and one per set named `<Product>.<Area>.<Variant>`. Slice folders inside both stay by subject. The dependencies point from the Kernel to a knowledge model to the modules. A knowledge set references its model and the Kernel. Hosts reference sets to register them, while modules reach knowledge through the model.

### The Module Tree

Every module organizes its code as the module tree: one `Contracts/` folder for its public surface, one `Domain/` folder holding one folder per declared aggregate in the plural, one `Ports/` folder holding one folder per driven port of the module, the `Application/` and `WebApi/` layer folders holding one plural aggregate folder with one folder per use case inside it plus one folder per module-level read, and one `Infrastructure/` folder holding `Persistence/` nested by aggregate, `Messaging/`, `Pipeline/`, and one adapter folder per port. A command sits in the aggregate folder its record names: the record tree already groups use cases by aggregate, and the code mirrors the records.

A module publishes its public surface in its `Contracts/` folder: data grouped by aggregate. Each `Contracts/{Aggregates}/` folder holds the identifier another module carries, the types facts or messages of more than one aggregate carry with the aggregate owning the concept, one `On{Fact}/` folder per fact another module consumes, and one folder per public query or public command of that aggregate. Other modules reference nothing outside it.

The Claimwick spending module shows the tree:

```text
Spending/
  Contracts/
    Expenses/              ExpenseId, OnExpenseReimbursed/ExpenseReimbursedModuleEvent, ReadExpense/
    ReadExpenseDirectory/  a public module-level query
  Domain/
    Expenses/              Expense, ExpenseState, the expense repository contract, Events/, Exceptions/
  Application/
    Expenses/
      SubmitExpense/       command, result, handler, validator, and the reactions and jobs that run it
      ErasePersonalData/   the erase command, its handler, and the job that runs it
    ReadExpenseDirectory/  a module-level read: no aggregate folder
    Notifications/
      ApprovalReminder/    the reaction that sends the approval reminder
    ReadModels/
      ReimbursedExpenses/  a read model of another module's published facts
  Ports/
    ReceiptPdf/            IReceiptPdf with its signature types in Models/
  Infrastructure/
    Persistence/
      Expenses/            the expense repository with its stored documents and mapping
    Messaging/             the domain-to-module event mapping, audit writers
    Pipeline/              middleware, authorization conditions, authentication handlers
    ReceiptPdf/            the adapter with its wire types in Models/ and its collaborators in Parts/
  WebApi/
    Expenses/
      SubmitExpense/       endpoint, request model, response model
```

The tree fixes every type's address: a reader moves from a Domain aggregate to its handler, its domain repository, and its endpoint without a map. Every level defines a closed set of folder names, and the check reports a folder outside the allowed set. Every folder name is a product noun (a module or an aggregate), a business act (a use case), one of the four layers, a record kind the product speaks (`Ports`, `Notifications`, `ExternalSystems`, `ReadModels`), or a grouping from the closed set its place allows.

| Level | Allowed folders |
|:---|:---|
| Product root | `Domain/`, `Application/`, `Infrastructure/`, `WebApi/`, one per declared module |
| Module | `Contracts/`, `Domain/`, `Application/`, `Ports/`, `Infrastructure/`, `WebApi/` |
| Module `Contracts/` | one folder per aggregate with anything public, in the plural, and one folder per public module-level query; no files |
| Module `Contracts/{Aggregates}/` | the aggregate identifier when another module carries it, the types facts or messages of more than one aggregate carry when the aggregate owns the concept, one `On{Fact}/` folder per fact about the aggregate another module consumes, and one folder per public query or public command of that aggregate; no other files |
| Module `Contracts/{Aggregates}/On{Fact}/` | the fact type with the types only it carries; no other files |
| Module `Contracts/{Aggregates}/{UseCase}/` and each public module-level query folder | the message, its result, and the result parts; no other files |
| Module `Domain/` | one folder per declared aggregate, in the plural |
| Module `Domain/{Aggregates}/` | the root's main file, the repository interface as the aggregate's only store interface, the root's partial files for a concern no single act owns (`{Root}.{Concern}.cs`), `{Act}/` per root action, `Parts/`, `Facts/`, `Events/`, `Exceptions/` |
| Module `Domain/{Aggregates}/{Act}/` | the partial file `{Root}.{Act}.cs`, the act's inputs, its private rules, and the steps only it calls; the name is the use case's act |
| Module `Domain/{Aggregates}/Parts/` | the aggregate's entities, value objects, state types, and the stateless rules several acts share |
| Module `Domain/{Aggregates}/Facts/` | values passed in from other modules or the country pack |
| Module `Application/` | one folder per declared aggregate that has use cases, one folder per declared module-level read, `Notifications/`, `ExternalSystems/`, `ReadModels/`, `Settings/` |
| Module `Application/{Aggregates}/`, `WebApi/{Aggregates}/` | one folder per use case declared under that aggregate; no files |
| Module `Application/Settings/` | the module options class; no other files |
| Module `Ports/` | one folder per driven port of the module; no files |
| Module `Ports/{Port}/` | the interface `I{Port}` and `Models/` |
| Module `Ports/{Port}/Models/` | every type the interface's signatures name |
| Module `Infrastructure/` | `Persistence/`, `Messaging/`, `Pipeline/`, one folder per port the module implements |
| Module `Infrastructure/Messaging/` | the module's message pipeline pieces: the domain-to-module event mapping, audit writers, and the resolvers and mappers the module registers into the message pipeline; files only |
| Module `Infrastructure/Pipeline/` | the module's HTTP pipeline pieces serving no single use case: middleware, authorization conditions, authentication handlers; files only, past 20 files one folder per protocol holding that protocol's wire types beside its pieces |
| Module `Infrastructure/{Port}/` | the adapter class, its options class `{Adapter}Options.cs`, `Models/`, and `Parts/` when the adapter needs collaborators; several adapters of one port sit side by side |
| Module `Infrastructure/{Port}/Models/` | the external system's wire types: requests, responses, file records |
| Module `Infrastructure/{Port}/Parts/` | the collaborators the adapter calls |
| Module `Infrastructure/Persistence/` | module-wide store setup, and one folder per aggregate with a repository; no other files |
| Module `Infrastructure/Persistence/{Aggregates}/` | the aggregate's repository implementation, stored documents, and mapping |
| Module `WebApi/` | the aggregate use case folders and module-level read folders of use cases reached over HTTP |
| Module `Application/Notifications/` | one per notification a reaction or job of the module sends |
| Module `Application/ExternalSystems/` | one per external system a reaction or job of the module calls |
| Module `Application/ReadModels/` | one per declared read model of the module |
| Each use case folder and each folder under `Notifications/`, `ExternalSystems/`, `ReadModels/` and `Settings/` | Files only |

A `Domain/{Aggregates}/` folder groups into `{Act}/` per root action, `Parts/`, `Facts/`, `Events/`, and `Exceptions/`; a `Ports/{Port}/` folder groups its signature types into `Models/`; an `Infrastructure/{Port}/` folder groups its wire types into `Models/` and the collaborators its adapter calls into `Parts/`; an `Infrastructure/Pipeline/` folder past 20 files groups into one folder per protocol. No folder groups by caller, and no folder carries the name `Commands`, `Queries`, or `Handlers`.

A file takes its layer from its top folder in the module. The role suffix states the file's role inside that layer: `Endpoint`, `RequestModel`, and `ResponseModel` sit in `WebApi/{Aggregates}/{UseCase}/`, and `Command`, `Query`, `Result`, `Handler`, `Validator`, and the use case steps sit in `Application/{Aggregates}/{UseCase}/`. Port interfaces sit in `Ports/{Port}/`, with their adapters in `Infrastructure/{Port}/`. A module-level read drops the aggregate segment in each layer.

Each trigger sits with what it runs. The event handler of a reaction and the class of a job sit in the `Application/` folder of the use case they run or, with none run, in the `Application/Notifications/` folder of the notification they send or the `Application/ExternalSystems/` folder of the system they call. The `Application/ExternalSystems/` folder holds those trigger classes; the adapters they call live in the port adapter folders of `Infrastructure/`.

Inside a product layer folder, subfolders stay free (`Application/Pipeline/`, `Infrastructure/Development/`), because the folder states the layer and the lowest-folder rule already placed the type.

A module keeps one plural aggregate folder holding one folder per use case inside each layer that serves it; a module-level read sits directly in each layer that serves it. Every module keeps its plural aggregate folder in every layer, whatever its number of aggregates and even with the aggregate's name equaling the module's: one shape everywhere lets a reader and a tool find code from a record without knowing how many aggregates a module holds today, and a second aggregate added later moves nothing. Models stay separate, and data flows one way: request model, command, aggregate, result, response model. The endpoint maps the request in and the result out, and the handler returns a result, not the aggregate.

A typed identifier another module carries lives in the aggregate folder of its owner's `Contracts/` folder, so the one rule covers identifiers too; an identifier no other module carries lives in the aggregate's `Domain/` folder. A module publishes no refusal and no interface. To trigger an act on an upstream module's aggregate, a reaction or job of the module sends that module's public command through the mediator; to read an upstream module's current value, a handler, reaction, or job of the module sends that module's public query through the mediator. A downstream change arrives as a fact the downstream module's reaction answers. Each module answers with its own refusals.

Kernel values stay in the product `Domain/`. Product-wide technical capabilities stay in the product `Application/`, `Infrastructure/`, and `WebApi/` folders: persistence setup, messaging, authorization, auditing, and HTTP conventions. The product `WebApi/` folder holds what endpoints of two or more modules compile against: the endpoint contract and the current actor.

Two rules place each type inside the tree. The lowest-folder rule places behavior such as a rule calculation or an adapter in the use case, module, or product layer folder; every port of a module lives in its `Ports/` folder instead, whether one use case or many call it. Across modules a type stays unshared, and the owning module publishes it in `Contracts/`. A message, result, result part, transport model, or mapping belongs to one use case and sits in its folder. A type's private shapes stay private beside it instead of moving up the count.

The product layer folders hold what remains after the count; they are no destination anyone chooses. A folder named for no reason (`Shared`, `Common`, `Util`, `Helpers`, `Core`, `Misc`) accepts every type placed in it, until a tenth of the layer sits outside the module tree, unreviewed and belonging to no aggregate.

### Commands Change State and Queries Read It

CQRS separates the command and query models inside the one Application layer: commands change state, queries read it. A command changes one aggregate instance through its domain repository, while a query projects read results without loading an aggregate and without writing. The declared exception is a use case whose record declares `consistency: atomic` over several aggregates with a decision record naming the use case and the invariant that takes both in one transaction, and it names each aggregate it changes.

A reaction answers its trigger by running one command or sending a notification, and a scheduled or one-off job runs one command per item in its selection; neither travels the command pipeline itself. A handler sends no command and no event: an endpoint, an event handler, and a job send commands, while a handler, reaction, or job sends another module's public query with a declared `queries` edge. Submitting an expense runs a `SubmitExpense` command through the handler and the domain repository. Reading an expense runs a query that projects the stored data directly to its result.

A use case folder reads as the use case: its message, handler, validator, definitions, and result. Production of a file format, rendering, or parsing lives in Infrastructure behind a module port: the module declares the port in its `Ports/` folder and hands the gathered input to it. The export-expenses use case declares `IExpensesArchive` in `Ports/ExpensesArchive/` and hands it the gathered lines; the writer lives in `Infrastructure/ExpensesArchive/`, split into one writer per archive section.

Business rules live with the data they guard. An aggregate or value object is the place that enforces a state transition or invariant, so a rule duplicated in a handler drifts from the aggregate that owns it. A deciding step lives once: the receipt threshold check lives on the expense aggregate. A coordinating step, such as mapping a request or loading two records in order, is copied into each use case that performs it, since a shared coordinator couples the use cases that call it. Where a reaction runs each follow-on act, the act raises its fact and the other use case answers it.

### Modules Talk Through Contracts

A module is downstream of the modules it depends on, and upstream of the modules depending on it. A depends on B with A referencing B's Contracts, sending B's public queries or public commands, or reacting to B's facts. Every relationship between two modules has one direction, so the dependency graph over all four edge kinds holds no cycle. A cycle means a concept is split across two modules, and the fix moves the concept into one of them. Two modules that each take the other's model merge into one module, or the shared concept moves into the module that owns it.

Commands and queries flow upstream, and facts flow downstream. A downstream module sends an upstream module's public queries and public commands and reacts to its facts. An upstream module sends no query, command, or reaction to a downstream module: with downstream work finished, the downstream module calls its public command. In the Claimwick product, the reporting module sits downstream of the spending module. Reporting sends spending's public read-expense query and spending's public commands through the mediator, and follows spending's published facts. Spending sends nothing to reporting and answers no reporting fact.

Across modules, each read takes one of three paths. A module that takes another module's current value asks the provider's public query from its handler, reaction, or job, with a declared `queries` edge. A module that reshapes another module's data and keeps it locally holds its own read model of it, built from the owner's published module events. A screen that shows another module's data calls the owner's own query. A module loads the other module's aggregate or repository in no case.

A command changes the aggregates of its own module. An act on an upstream module's aggregate runs as that module's public command, sent by the caller's reaction or job through the mediator: the owning module runs its own handler through its own pipeline and enforces its own invariants, with the caller declaring the send as `effects.commands` naming the owner's public command use case. An act on a downstream module's aggregate runs as a reaction to the fact the command raised. A reaction to another module's fact subscribes to the published module event, not to the domain event behind it. A fact from outside the product arrives as an inbound integration event.

A process spanning modules takes one driver: the most downstream participant, which already depends on every other participant, or a process module that depends on all of them. The driver runs the process through the participants' public commands and follows their facts, so each edge points upstream and the graph holds no cycle. Choreography, with each participant answering the others' facts, stays allowed where it creates no cycle. In the Claimwick product, a period close spanning spending and reporting runs from the reporting module, which sends spending's public commands for each spending step and follows spending's facts. Spending answers no reporting fact.

A text reference that crosses modules as text travels as one contract type with one parser and one formatter in the publishing module's Contracts folder. In the Claimwick product, a payout reference travels from spending to reporting as text. Spending publishes the reference as a contract type with one parser and one formatter, and reporting builds and reads it through that type alone.

### Background Work Has a Named Host

Durable dispatch, queue consumption, and scheduled work run under a declared execution host: the WebApi process or a Worker project. The declaration lives in a Decision record, following the model rule on recording each technical choice as a decision. Durable dispatch and queue consumption follow the outbox extension, and scheduled work follows the jobs extension. The host is a composition decision, not a code-placement decision. A job class lives with the use case it runs. The outbox dispatcher and the scheduler that drive reactions and jobs live in Worker with that project present, otherwise in Infrastructure, and the host registers them.

Each host registers its own transport services and calls the shared composition entry points, so the full composition of a process is visible in one place. A second provider built mid-registration resolves a different object graph from the one the host runs. WebApi and Worker are the composition roots. Domain contains no registration code, and Application exposes contracts without resolving services. Registration lives in host entry points and host registration modules: Infrastructure registers providers and options, while WebApi registers transport, authentication, and documentation.

Background work that scales, deploys, or isolates on its own takes its own process. A restart to deploy an endpoint change interrupts a dispatch loop, and request load and dispatch load compete for one thread pool, so neither scales alone. An application whose outbox dispatches a few messages each minute runs it in WebApi. An application that dispatches continuously, or whose dispatch survives an endpoint deployment, creates the Worker project.

### Boundaries Stay Closed and Checked

Implementations stay inside their layer and closed to extension. A public handler allows direct construction that bypasses the pipeline, and an inheritable one splits behavior across subclasses. Messages, results, and ports turn public where another layer uses them, and automatic discovery gives no reason to widen visibility.

Layer references, package boundaries, visibility, module folders, and aggregate inheritance are invisible at runtime: the application runs the same with or without them. Without mechanical proof on every run, each one erodes unnoticed. The analyzers and `model check` already prove the boundaries a stack page names as checked, plus the rows in the table below. A check over method bodies reports an outer-layer call inside a method, which a test over compiled signatures misses.

An architecture test that repeats a covered boundary holds a second copy of a rule the build or the check already enforces, and the copy drifts while the analyzer or the check keeps reporting the breach. Architecture tests hold the rest: a boundary the checks cover in part, and a structural decision the project itself made that no LiteSpec tool names.

| Structural boundary | Covered by |
|:---|:---|
| A folder sits outside the closed set its level allows, or a module folder carries no declared module | `record-placement-invalid` from `litespec model check` |
| A marked code element, test, or folder sits outside the folder its record names, or a folder carries no declared record | `mark-folder-drift` and `code-placement-drift` from `litespec model check` |
| A folder or type carries a technical grouping name such as `Shared`, `Common`, or `Helpers` outside the declared Kernel | `name-reason-missing` from `litespec model review` |

In the Claimwick product, no architecture test repeats the checks that report an outer-layer type in Domain, an enum in Domain, a command handler that holds a write session, or a handler that authorizes itself. The architecture suite keeps a test that no module reaches another module outside its `dependsOn`, with a type in the other module's `Contracts/` folder staying allowed.

## Standards

### Point Dependencies Inward

**ID:** `litespec/core/backend-architecture#point-dependencies-inward`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A reference MUST point inward, so Domain references no outer layer and WebApi composes the process.

**Rationale:** Inner code references no outer code, so business rules compile and test without adapters or hosting. An outward reference places technical concerns in Domain or coordination in Infrastructure. The reference matrix in [Dependencies](../workspace/dependencies.md) applies as written.

**Example:** Every reference points from an outer layer to an inner one: Application references Domain, Infrastructure references both, and Domain references neither.

![The four layers and their references: WebApi references Application and WebApi, and from Domain only typed identifiers and Kernel values; Infrastructure references Domain, Application, and Infrastructure; Application references Domain and Application; Domain references only Domain.](../assets/diagrams/backend-dependencies.svg)

**Reviewed for:** Is any outer layer referenced from an inner one? Look at the reference graph in the code.

### Own Each Layer's Contract Types

**ID:** `litespec/core/backend-architecture#own-each-layers-contract-types`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A layer MUST define its own outward contracts rather than reuse an inner layer's type or another module's contract type, except for Kernel values and aggregate typed identifiers.

**Rationale:** With each layer defining its own outward contracts, a change to an inner shape stops at the boundary instead of forcing changes in every outer layer. Kernel values cross layer boundaries because the Kernel is shared by definition, and aggregate typed identifiers cross them because messages identify their targets. A provider's public query or public command answer stays inside the consumer's Application code, and a use case returns it in no result or transport model of its own. Domain takes plain values and identifiers, not a provider's result.

**Reviewed for:** Is any inner layer's type reused as an outward contract, or any other module's port answer returned as-is, outside Kernel values and aggregate typed identifiers? Look at the results and transport models in each layer's folders.

### Produce File Formats in Infrastructure

**ID:** `litespec/core/backend-architecture#produce-file-formats-in-infrastructure`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler, job

**Statement:** Production of a file format, rendering, and parsing MUST live in Infrastructure behind a module port, even when the code uses only the base class library.

**Rationale:** An archive, CSV, PDF, or audit-file writer that uses the base class library alone passes each layer check while doing infrastructure work. A writer sitting in the use case folder fills it with code the use case calls, and the folder reads as anything but the use case. Behind a module port, the folder holds the message, handler, validator, definitions, and result.

**Example:** The export-expenses use case declares `IExpensesArchive` in `Ports/ExpensesArchive/` and hands it the gathered lines; the writer lives in `Infrastructure/ExpensesArchive/`, split into one writer per archive section.

**Reviewed for:** Does any Application folder produce a file format, render, or parse, instead of calling an Infrastructure port? Look for archive, CSV, JSON-writer, PDF, and rendering libraries in the Application folders.

### Organize Code by Module, Aggregate and Use Case

**ID:** `litespec/core/backend-architecture#organize-code-by-module-aggregate-and-use-case`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every module MUST organize its code as the module tree with one `Contracts/` folder, one `Domain/` folder holding one folder per declared aggregate in the plural, one `Ports/` folder holding one folder per driven port of the module, the `Application/` and `WebApi/` layer folders holding one plural aggregate folder with one folder per use case inside it plus one folder per module-level read, and one `Infrastructure/` folder holding `Persistence/` nested by aggregate, `Messaging/`, `Pipeline/`, and one adapter folder per port, using only the folder names its level allows.

**Rationale:** One plural folder per declared aggregate under `Domain/`, one folder per driven port under `Ports/` with its adapter in `Infrastructure/`, `Messaging/` with the module's message pipeline pieces, one folder per aggregate under `Persistence/`, `Pipeline/` with the module's HTTP pipeline pieces, and one plural aggregate folder with one folder per use case inside each layer that serves it fix every type's address: a reader moves from a Domain aggregate to its handler, its repository, and its endpoint without a map. The closed folder names at each level keep that reading exact, because a folder outside the allowed set is reported and no second organization grows beside the tree.

A module publishes what other modules reference in its `Contracts/` folder grouped by aggregate, every use case sits in the aggregate folder its record names or directly in the layer folders with a module-level read, and each reaction and job sits in the folder of the use case it runs. Every module keeps its plural aggregate folder in every layer, whatever its number of aggregates and even when the aggregate's name equals the module's. Settled by [decision/0063-place-each-trigger-with-what-it-runs](decision/0063-place-each-trigger-with-what-it-runs).

The module tree shape is settled by [decision/0107-kernel-platform-and-one-project-per-module](decision/0107-kernel-platform-and-one-project-per-module).

A folder named for a technical grouping rather than a module defeats the rule: the next unrelated type fits it too. `Shared`, `Common`, `Util`, `Helpers`, `Core`, and `Misc` inside a layer are the names that do this.

**Example:**

```text
Spending/
  Contracts/
    Expenses/                 ExpenseId, OnExpenseReimbursed/ExpenseReimbursedModuleEvent, ReadExpense/ (query and result)
    Policies/                 the identifier, one On{Fact}/ folder per fact, and the public queries and public commands of the policy aggregate
  Domain/
    Expenses/                 Expense, the expense repository contract, and the act, part, fact, event, and refusal groupings
    Policies/
  Application/
    Expenses/
      SubmitExpense/
      ErasePersonalData/      the erase command, its handler, and the job that runs it
    ReadExpenseDirectory/     a module-level read: no aggregate folder
    Notifications/
      ApprovalReminder/       the reaction that only sends the approval reminder
  Ports/
    ReceiptPdf/               IReceiptPdf with its signature types in Models/
  Infrastructure/
    Persistence/              module-wide store setup, with one folder per aggregate
      Expenses/               the expense repository with its stored documents and mapping
    Messaging/                the domain-to-module event mapping, audit writers
    Pipeline/                 middleware, authorization conditions, authentication handlers
    ReceiptPdf/               the adapter implementing Ports/ReceiptPdf/, same folder name, with its wire types in Models/ and its collaborators in Parts/
  WebApi/
    Expenses/
      SubmitExpense/
```

The placement check asserts these folder names directly.

```text
fail when a type path contains Shared, Common, Util, Helpers, Core, or Misc
```

**Reviewed for:** Is any module, aggregate, use case, notification, external-system, or read-model folder without a declared record, with a forbidden folder name, or with a technical-grouping name? Look at the folder paths in each module.

### Group a Folder That Holds Past Twenty Files

**ID:** `litespec/core/backend-architecture#group-a-folder-that-holds-past-twenty-files`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A folder that holds past 20 files MUST hold the grouping folders the closed set of its place allows.

**Rationale:** A folder past 20 files with no groupings reads as one long listing: a reader finds a file by search alone, and review misses what the folder holds. Each kind of place has a short closed list of grouping folders. A use case folder holds everything flat with no groupings: it stays small with fan-out living in a reaction or a job.

**Example:** A `Domain/Expenses/` folder past 20 files groups each root action into its own `{Act}/` folder, so `Domain/Expenses/Submit/` mirrors `Application/Expenses/SubmitExpense/`.

**Checked by:** `litespec model check` reports `folder-ungrouped` for a folder past 20 files that holds none of its allowed groupings.

### Place a Type at the Lowest Folder That Holds Its Consumers

**ID:** `litespec/core/backend-architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A type that is not a use case's own model MUST live at the lowest folder containing every consumer that names it.

**Rationale:** Counting consumers gives each type exactly one folder, so placing a type takes no judgment. The lowest folder is the use case folder with one referencing use case, the module folder with several use cases of one module referencing it, and the product layer folder with several modules referencing it and no module between them. A domain rule several use cases of one aggregate reference lives in that aggregate's `Domain/{Aggregates}/` folder. Every port of a module lives in the module `Ports/` folder instead, whether one use case or many call it, with its adapter in `Infrastructure/`, so the consumer count places no port.

A type another module references is published by its owning module in `Contracts/`; across modules a type stays unshared. A use case file's layer comes from its top folder in the module, and a type referenced from Infrastructure alone is an Infrastructure type. A type that exists to shape one contract's input or output stays with that contract, and a closed enumerated set stays whole. A use case's own models stay out of the count: a message, result, result part, transport model, or mapping belongs to one use case, so the count places behavior such as ports, rule calculations, and adapters.

The Kernel values are the one product `Domain/` folder a solution keeps. Its membership is closed by `litespec/core/backend-architecture#own-each-layers-contract-types`; it is no place for types that fit nowhere else. A service that coordinates several aggregates lives in the module that owns the aggregates it reads and writes, not with whichever module calls it first.

**Reviewed for:** Is any type sitting above the lowest folder holding every consumer that references it? Look at each type beside its consumers, watching for Shared, Common, Util, Helpers, Core, or Misc folders.

### Give Each Use Case Its Own Models

**ID:** `litespec/core/backend-architecture#give-each-use-case-its-own-models`

**Strength:** required

**Applies to:** artifact: use-case, endpoint

**Statement:** A message, result, result part, transport model or mapping MUST belong to exactly one use case and sit in its folder, so two use cases that need the same shape each declare their own.

**Rationale:** A shared shape couples two use cases that change for different reasons: one use case adds a field, and the other starts returning it. Counting consumers for shapes moves messages and results out of their use cases into module folders that hold more models than behavior. A copied shape costs a few lines and decides nothing, so it drifts into no wrong answer. A rule is different: two copies of a rule disagree, so a rule lives once in Domain where every use case reads it.

Kernel values are the exception: money, typed identifiers, dates, and closed sets the Kernel owns come referenced directly from any layer the layer table allows, and copying one into a use case folder breaks the rule.

**Example:** The submit-expense and approve-expense use cases each return the expense they acted on. Each declares its own result with the fields its caller needs, and neither result lives in `Domain/Expenses/`.

**Checked by:** `dotnet build` reports `LS2019` for a use case folder referencing a type in another use case folder, and `litespec model check` reports `code-placement-drift` for a message, result or transport-model file outside a use-case folder.

### Read Another Module Only Through Its Contracts

**ID:** `litespec/core/backend-architecture#read-another-module-only-through-its-contracts`

**Strength:** required

**Applies to:** artifact: command-handler, port, query-handler; when: cross-module

**Statement:** A module needing another module's data MUST read it through the provider's public query sent with a declared `queries` edge, through its own read model built from the owner's module events, or, on a screen, through the owner's own query.

**Rationale:** A module that loads another module's aggregate or repository couples its reads to the owner's consistency boundary and storage. A general reader in the owner's `Contracts/` grows a method per caller, with no record saying which fields cross. The three allowed paths each state what crosses. The provider's public query answers with the owner's current value, sent with a declared `queries` edge, under `litespec/core/backend-application#send-cross-module-queries-and-commands-through-the-mediator`. A read model states each field it copies from a published event, under `litespec/core/backend-application#read-another-modules-state-through-a-read-model`. A screen joins each owner's query, under `litespec/core/ui-ia#compose-each-screen-from-its-owners-queries`.

**Example:** In the Claimwick product, the reporting module reads the current expense through spending's public read-expense query, sent with a declared `queries` edge. It keeps its own `read-model/reporting.reimbursed-expense`, filled from the spending module's expense-reimbursed module event, for the reshaped rows its lists show. It loads no spending aggregate, no spending repository, and no spending reader.

**Reviewed for:** Does any cross-module read load another module's aggregate or repository, or call a general reader instead of a public query sent with a declared edge, a read model, or a screen joining owner queries? Look at the cross-module reads beside the declared edges.

### Give Every Module Relationship One Direction

**ID:** `litespec/core/backend-architecture#give-every-module-relationship-one-direction`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every relationship between two modules MUST have one direction, so the module dependency graph over Contracts references, `queries` edges, `commands` edges, and reactions to facts holds no cycle.

**Rationale:** A relationship with two directions says each module builds on the other, so neither has a seam to split along. One direction per relationship keeps the graph acyclic: A depends on B with A referencing B's Contracts, sending B's public queries or public commands, or reacting to B's facts, and no chain of such edges returns to its start. A cycle means a concept is split across two modules, and the fix moves the concept into one module, under `litespec/core/backend-architecture#merge-modules-that-need-each-others-model`. Settled by [decision/0110-module-dependencies-form-a-dag](decision/0110-module-dependencies-form-a-dag).

**Example:** In the Claimwick product, reporting depends on spending through its declared edges to spending's public queries, and spending depends on nothing. A spending edge back to reporting closes a cycle and is reported naming both modules and the edges.

**Checked by:** `litespec model check` reports `module-dependency-invalid` for a cycle in the module dependency graph over Contracts references, `queries` edges, `commands` edges, and reactions to facts.

### Send Commands and Queries Upstream

**ID:** `litespec/core/backend-architecture#send-commands-and-queries-upstream`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler, reaction, job; when: cross-module

**Statement:** A module MUST send another module's public queries and public commands only upstream, follow only upstream facts, and direct no query, command, or reaction at a downstream module.

**Rationale:** The direction rule works in practice through this provision. A downstream module sends an upstream module's public queries and public commands and reacts to its facts. An upstream module queries, commands, or reacts to a downstream module in no case: choreography across the boundary makes two modules depend on each other, which the direction rule forbids. With upstream work finished downstream, the downstream module calls its public command, under `litespec/core/backend-application#confirm-downstream-completion-through-an-upstream-command`. Settled by [decision/0109-module-contracts-publish-data](decision/0109-module-contracts-publish-data).

**Example:** In the Claimwick product, the reporting module sends spending's public read-expense query and spending's public commands through the mediator, and follows spending's published facts. Spending sends nothing to reporting and answers no reporting fact.

**Reviewed for:** Does any module query, command, or react to a downstream module instead of sending upstream and following upstream facts? Look at the cross-module edges beside the module dependency graph.

### Run Every Spanning Process from Its Most Downstream Participant

**ID:** `litespec/core/backend-architecture#run-every-spanning-process-from-its-most-downstream-participant`

**Strength:** required

**Applies to:** artifact: command-handler, reaction, job

**Statement:** A process spanning modules MUST run from its most downstream participant, or from a process module depending on every participant, through the participants' public commands, with choreography only where it creates no cycle.

**Rationale:** A spanning process takes one driver: the most downstream participant, which already depends on every other participant, or a process module that depends on all of them. The driver runs the process through the participants' public commands and follows their facts, so each edge points upstream and the graph holds no cycle. Choreography, where each participant reacts to the others' facts, stays allowed where it creates no cycle: two participants answering each other's facts depend on each other both ways. Settled by [decision/0109-module-contracts-publish-data](decision/0109-module-contracts-publish-data).

**Example:** In the Claimwick product, a period close spanning spending and reporting runs from the reporting module, which sends spending's public commands for each spending step and follows spending's facts. Spending answers no reporting fact.

**Reviewed for:** Does every process spanning modules run from its most downstream participant or a process module through public commands, with choreography where it creates no cycle? Look at the spanning processes beside their participants' public commands.

### Merge Modules That Need Each Other's Model

**ID:** `litespec/core/backend-architecture#merge-modules-that-need-each-others-model`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Two modules that need each other's model MUST merge into one module or move the shared concept into the module that owns it.

**Rationale:** Two modules that each take the other's model have no seam between them: whatever the boundary, each side reaches across it. Merging them into one module, or moving the shared concept into the module that owns it, gives every concept one owner and every relationship one direction, under `litespec/core/backend-architecture#give-every-module-relationship-one-direction`. Sharing identifiers through the Kernel hides the cycle while the split concept stays split. Settled by [decision/0110-module-dependencies-form-a-dag](decision/0110-module-dependencies-form-a-dag).

**Example:** Two modules passing each other's identifiers both ways merge into one module, and the shared concept lives in one aggregate with one owner.

**Reviewed for:** Do any two modules take each other's model without merging or moving the shared concept? Look at the module pairs with edges both ways beside the module records.

## Conventions

None.
