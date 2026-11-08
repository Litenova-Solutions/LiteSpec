# Supported Scope

**Stack:** any

**Layer:** workspace

## Intent

LiteSpec is a standard for specification-driven development of one bounded-context business application by people and coding agents. This page states what each project choice decides, which extensions and outside constraints the project also declares, what claiming conformance means, and where the supported boundary ends. A team declares them once during inception, before writing anything else down, so agents and checks apply exactly the pages the project selected.

## Concepts

### The Three Project Choices

Three choices in `.litespec/project.json` decide everything the project must produce and every page that applies to it.

Size decides the record set and the test projects. A small project carries the core record types with Architecture and Integration test projects. A large project adds goals, problems, features, journeys, and scenarios with the Acceptance project. A project with one web frontend and no journeys fits small; the running Claimwick example, which needs journeys, selects large.

Shape decides the deployment boundary: `server` for a project with its own backend, `client-only` for one without. A server project names one backend stack. A client-only project names none, and records which backend surfaces stay uncovered.

Each stack decides which technology pages and checks apply. A project names one stack per frontend, choosing react, svelte, blazor, or android for each, and the stack applies its folder and every folder above it, as the [stacks page](stacks.md) states. A preset only fills in a common combination of these choices; the project record holds the result.

### One Folder for Every LiteSpec File

Every file LiteSpec creates, reads, or governs in a consumer sits under one folder, `.litespec/`, at the root of the consumer's repository. Inside it, the layer folders `intent/`, `behavior/`, `implementation/`, `proof/`, and `governance/` hold the records the layers own; `project.json`, `glossary.yaml`, and the example cast sit at its top level; the tool builds its model in memory, keeps its working files in `model/` and writes its book to `book/` under it, and a `.gitignore` there keeps those two generated folders out of source control. Nothing of LiteSpec lives anywhere else in the repository, and no LiteSpec material sits at the repository root. Application code, tests, and infrastructure keep the places their stack pages name. Proof holds no authored records.

### Extensions, Constraints, and the Boundary

An extension is a conditional standards bundle that stays inactive until a documented condition holds. The Behavior extensions are `privacy`, `devices`, `automation`, `workflow-engine`, `pipeline`, `queues`, and `ai`, beside the existing `tenancy`, `compat`, `jobs`, `outbox`, `audit`, and `stage` extensions. A project that operates the product in production with a formal release, deployment, rollback or alerting regime selects the `delivery` extension. A project selects an extension only when its activation criterion holds, and the [extension catalog](../ext/README.md) lists each extension with its criteria. Outside constraints work the same way: the project states in `.litespec/project.json` whether any constraint applies, and an empty answer records that none does.

LiteSpec specifies the product, not the company that builds it. An Intent or Behavior record states only behavior the application performs, data it holds, or a limit it enforces; company operation enters only as a constraint or as dated research.

Every record states something the software does, holds, refuses at runtime or runs on, and translates into code, results, tests or infrastructure:

| Type | Belongs when it is | Out when it is |
|:---|:---|:---|
| Goal | A software outcome: "Nights run without oversell or unreconciled money" | A business outcome: retention, revenue, market share |
| Problem | A user pain the software answers | A sales or market problem |
| Feature | A capability the software has | A marketing tactic or a launch plan |
| Requirement | A commitment Behavior fulfills | (always software when it reaches Behavior) |
| Constraint | Law, a provider or a limit that binds the software | A company duty with no software effect, such as incorporation |
| Research | Evidence a software record cites | Market sizing, viability, acquisition, pricing strategy |
| Module | A product area of the bounded context | A company function |

Each Intent area names a declared module under `intent/modules/<module>/`, so one name runs through Intent, Behavior, and code. Anything cross-cutting, such as accessibility or security, sits at product level. A folder under `intent/modules/` that names no declared module gets a blocking finding.

Some architectures fall outside what LiteSpec supports: microservices, multiple contexts, native clients on a platform with no mobile stack, other platforms, event sourcing, active-active regions, and large pipelines. A project that considers one records a separate project decision naming it. Projects outside the boundary can still use individual ideas without claiming conformance.

### What Claiming Conformance Means

A consumer claims conformance when it applies every page its size, shape, and stacks select, selects every extension whose criteria hold, records each exception it relies on with its reason and expiry date, and passes the checks its changed boundaries select. Governance is a group of records beside the four record layers, not a fifth layer. A project outside the supported boundary, or one that skips an applicable page or check, makes no conformance claim.

## Standards

### Select a Size, a Shape and Stacks

**ID:** `litespec/core/scope#select-a-size-a-shape-and-stacks`

**Strength:** rule

**Statement:** A consumer MUST select a size, a shape, one stack per frontend, and one backend stack unless the shape is client-only.

**Rationale:** One size and explicit stacks tell every check which records and pages apply. The size decides which record types the project carries and which test projects prove them; each stack decides which technology pages and checks apply. Without the declaration, each agent infers the architecture from the directory tree, and two agents infer two architectures.

**Example:** A project with one web frontend and no journeys selects small with the svelte stack; the running Claimwick example, with journeys, selects large.

The running Claimwick example declares large with a server shape and two stacks in `.litespec/project.json`:

```json
{
  "size": "large",
  "shape": "server",
  "stacks": ["dotnet", "svelte"]
}
```

The size selects the large record set, the shape selects a backend stack, and each stack selects its technology pages, which satisfies this provision.

The diagram shows the three project choices and what each decides.

![Three project choices and what each decides: size, small or large, decides the record types and the test projects, with large adding goals, problems, features, journeys, and scenarios; shape, server or client-only, decides whether the project names a backend stack; stacks, one backend stack and one stack per frontend, decide the technology pages and the checks that apply.](../assets/diagrams/core-scope-selection.svg)

**Checked by:** `litespec model check` refuses a shape whose backend stack count is wrong and a platform that names no frontend stack.

### Declare the Conformance Level

**ID:** `litespec/core/scope#declare-the-conformance-level`

**Strength:** rule

**Statement:** A consumer MUST declare in `.litespec/project.json` the conformance level it claims: `records`, `traced` or `full`.

**Rationale:** A project can take the record model without LiteSpec's code layout, or trace its code to its records in a layout of its own. The level tells every tool which part of the contract the project claims, so `model check`, the book, `rules` and the analyzers report that part and nothing else. The levels nest: `traced` claims everything `records` claims, and `full` claims everything `traced` claims.

**Example:** The running Claimwick example declares `"conformance": "full"`. A product that keeps its own folder layout and proves its records only through result files declares `"conformance": "traced"`.

**Checked by:** `litespec model check` reports `project-invalid` for a project file that names no level or a value other than `records`, `traced` or `full`.

### Keep One Bounded Context

**ID:** `litespec/core/scope#keep-one-bounded-context`

**Strength:** rule

**Statement:** A consumer MUST keep one business language and deployment boundary.

**Rationale:** One business language and one deployment boundary keep every module, record, and use case in a single consistent model. Modules organize related language, models, use cases, and code without becoming independent services or bounded contexts. A second language or boundary splits the project into systems that the records, links, and checks can no longer hold together.

**Example:** Posts, Authors, and Comments can be modules in one publishing context.

**Reviewed for:** Module pages and `.litespec/project.json` name one business language and one deployment boundary, with no second of either.

### Keep Every Record About the Software

**ID:** `litespec/core/scope#keep-every-record-about-the-software`

**Strength:** rule

**Statement:** Every record MUST state something the software does, holds, refuses at runtime, or runs on, and translate into code, results, tests, or infrastructure.

**Rationale:** A record about running the company states no behavior the checks confirm: retention, revenue, market sizing, sales tactics, and launch plans pass no software check. The software test in the Concepts section tells what belongs for each type, so a goal about nights without oversell stays while a goal about revenue leaves. A record that reaches no Behavior gets a blocking finding: it is either unfinished work for the backlog or not software.

**Example:** Claimwick keeps its expense areas under product names; a market fact that binds the product enters as dated research or as a constraint citing it, and a folder under `intent/modules/` naming no declared module is reported.

**Checked by:** `litespec model check` reports a blocking finding for a folder under `intent/modules/` naming no declared module and for a record reaching no Behavior.

### Select Conditional Extensions Explicitly

**ID:** `litespec/core/scope#select-conditional-extensions-explicitly`

**Strength:** rule

**Statement:** A consumer MUST select every extension whose activation criteria apply, and no extension whose criteria do not.

**Rationale:** Conditional capabilities stay inactive until a documented product or engineering condition applies, so no project carries caching, messaging, or tenancy machinery it never justified. Selecting exactly the extensions whose criteria hold keeps the active rule set minimal and reviewable. The manifest's extension list is the closed set a project selects from, so there is no informal extension.

**Example:** Caching, durable messaging, and multitenancy are extensions; the extension index lists each with its activation criteria.

**Reviewed for:** Each entry in `selectedExtensions` carries a criterion that holds, and no applicable extension from the extension catalog stays unselected.

### Record Selected Extensions

**ID:** `litespec/core/scope#record-selected-extensions`

**Strength:** rule

**Statement:** A consumer MUST list every selected extension in `selectedExtensions` in `.litespec/project.json`, each with its criterion.

**Rationale:** The project record determines which extension boundaries apply to the project's work. Writing each selection with its criterion lets a later reader check whether the condition still holds. Without the record, extensions drift in through dependencies and prose while the checks apply a different set.

**Example:**

```json
{ "id": "cache", "criterion": "expense reads miss the latency target" }
```

**Checked by:** `litespec model check` reports `project-invalid` for a selection without a criterion.

### Answer the Constraints Question

**ID:** `litespec/core/scope#answer-the-constraints-question`

**Strength:** rule

**Statement:** A consumer MUST state in `.litespec/project.json` whether any outside constraint applies to the project.

**Rationale:** An outside rule the project never names is a law the product can break unknowingly. Recording the answer, even when it is empty, proves the team asked the question: an empty answer states that no outside rule binds the project. A later reader can then tell a project with no constraints from one that never looked.

**Example:** A clinic booking system records that medical-device regulation does not apply to appointment scheduling.

**Reviewed for:** `.litespec/project.json` states which outside constraints apply, or records an empty answer.

### Declare a Consumer That Has No Backend

**ID:** `litespec/core/scope#declare-a-consumer-that-has-no-backend`

**Strength:** rule

**Statement:** A consumer with no server of its own MUST declare the `client-only` shape and record a decision naming the surfaces left without coverage.

**Rationale:** The shape decides which backend pages still apply. A `client-only` product keeps a domain model, so it applies the backend architecture, domain, application, and identifier pages that the manifest lists under `shapes`. Persistence, the HTTP API, and API testing have no target without a server, and the decision names those uncovered surfaces. Adding a server only to obtain conformance contradicts `litespec/core/principles#require-current-complexity-activation`, which permits new machinery only for a current documented requirement.

**Example:** `litespec/quality/ci#run-applicable-gates-on-every-pull-request` already permits skipping a gate whose surface does not exist, so a missing backend gate is a recorded consequence rather than an unexplained absence.

**Checked by:** `litespec model check` refuses a `client-only` project that names a backend stack.

### Record Unsupported Scope Decisions

**ID:** `litespec/core/scope#record-unsupported-scope-decisions`

**Strength:** rule

**Statement:** A consumer MUST record a separate project decision for microservices, multiple contexts, native clients on a platform with no mobile stack, other platforms, event sourcing, active-active regions, or large pipelines.

**Rationale:** Microservices, multiple contexts, native clients on a platform with no mobile stack, other platforms, event sourcing, active-active regions, and large pipelines each change the assumptions this page rests on: one language, one deployment, one API and database. A separate project decision records that the team saw the boundary and chose deliberately. Without it, the project drifts outside the supported scope while claiming its checks still apply.

**Reviewed for:** A project decision names each architecture outside the supported boundary the project adopts.

### Claim Conformance Only When Every Applicable Provision Holds

**ID:** `litespec/core/scope#claim-conformance-only-when-every-applicable-provision-holds`

**Strength:** rule

**Statement:** A consumer MUST meet every applicable provision before claiming conformance to LiteSpec.

**Rationale:** A conformance claim tells a reader that the whole selected contract holds, not only the parts the project chose to follow. A recorded exception whose expiry date has not passed outranks the provision or findings it covers, so a project that records its exceptions still meets this provision; an unrecorded departure breaks the claim.

**Example:** A consumer that selects the dotnet stack and the caching extension claims conformance when every page those selections apply holds and their checks pass, with each departure recorded as an exception.

**Reviewed for:** `.litespec/project.json` names the selected size, shape, stacks, and extensions; each departure from an applied page resolves to an exception record under `.litespec/governance/exceptions/`, and the selected checks pass.

### Point Behavior Docs at the Behavior Root

**ID:** `litespec/core/scope#point-behavior-docs-at-the-behavior-root`

**Strength:** rule

**Statement:** A consumer MUST set `paths.behaviorDocs` in `.litespec/project.json` to the folder that holds its Behavior records.

**Rationale:** The builder joins records, queries, diffs, and the book from one declared root. A Behavior tree the project record never names is one the tools never see, so its records skip every check. One declared folder keeps authors and tools on the same tree.

**Example:**

```json
{ "paths": { "behaviorDocs": ".litespec/behavior" } }
```

**Checked by:** `litespec model check` reports `project-invalid` for a project record without `paths.behaviorDocs`.

### Keep Every LiteSpec File Under One Folder

**ID:** `litespec/core/scope#keep-every-litespec-file-under-one-folder`

**Strength:** rule

**Statement:** A consumer MUST hold every file LiteSpec creates, reads, or governs under one folder, `.litespec/`, at the repository root.

**Rationale:** One folder with one name in every consumer makes the LiteSpec material identifiable at a glance: a reader, a tool, or an agent finds the whole contract in one place, moves or deletes it without hunting the tree, and keeps it apart from the application's code, tests, and infrastructure. Generated output inside it stays covered by its `.gitignore`, so a checkout never mixes committed records with built files. Every path in `.litespec/project.json` stays relative to the repository root, so moving the folder never rewrites the paths inside it, and a command finds the consumer by walking up from the working folder to the first folder that holds `.litespec/project.json`.

**Example:** Claimwick holds its project file, glossary, example cast, and the Intent, Behavior, Implementation, Proof, and Governance folders under `examples/claimwick/.litespec/` (in a real consumer, at its repository root), beside its application code and tests, which keep their own places. An agent asked to read the contract opens that one folder instead of collecting a root project file, a documentation tree, a model folder, and a book folder from four places.

**Reviewed for:** Which files created, read, or governed by LiteSpec sit outside `.litespec/`, or at the repository root? Look at the repository tree beside the project's `.litespec/` folder.

## Conventions

### Start with One API and Database

**ID:** `litespec/core/scope#start-with-one-api-and-database`

**Strength:** default

**Default:** Use one API deployable and one database.

**Replacement:** A consumer records a topology convention naming the deployables and stores it runs instead.

**Rationale:** One API deployable and one database keep the first architecture reviewable and operable by a small team. Each additional deployable or store adds deployment, data-consistency, and operations work that a project rarely needs at the start. A background job appears only when a process continues without an HTTP request.

**Reviewed for:** The topology holds one API deployable and one database, or a topology convention records what runs instead.

### Measure Capacity Before Expansion

**ID:** `litespec/core/scope#measure-capacity-before-expansion`

**Strength:** default

**Default:** Record current load, latency target, and observed constraint before adding caches, replicas, queues, or partitioning.

**Replacement:** A consumer records a capacity convention naming what justifies expansion without measurements.

**Rationale:** Caches, replicas, queues, and partitioning each answer a different limitation, and only measurements show which one the project hit. Recording current load, the latency target, and the observed constraint before expanding proves the added capacity answers the observed limit. Expanding first and measuring later adds machinery that can hide the real bottleneck.

**Reviewed for:** Each added cache, replica, queue, or partition follows recorded load, latency target, and observed constraint, or a capacity convention.

## Reference Example

This informative example demonstrates `litespec/core/scope#select-a-size-a-shape-and-stacks` and `litespec/core/scope#select-conditional-extensions-explicitly`.

A publishing product supports sign-in, draft creation, publication, and public reading inside one bounded context. Multi-tenant publication, live editing, and bulk analytics each fall inside a selected extension boundary with its own activation criterion.
