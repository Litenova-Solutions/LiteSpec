# Supported Scope

**Stack:** any

**Concern:** workspace

## Intent

LiteSpec is a standard for specification-driven development of one bounded-context business application by people and coding agents. This page states what each project choice decides, which extensions and outside constraints the project also declares, what claiming conformance means, and where the supported boundary ends. A team declares them once during inception, before writing anything else down, so agents and checks apply exactly the pages the project selected.

## Concepts

### The Four Project Choices

Four choices in `.litespec/project.yaml` decide everything the project must produce and every page that applies to it: size, shape, stacks and layers. The layer claims decide which part of the contract the project takes, as Layer Claims below states.

Size decides the record set and the test projects. A small project carries the core record types with Architecture and Integration test projects. A large project adds goals, problems, features, journeys, and scenarios with the Acceptance project. A project with one web frontend and no journeys fits small; the running Claimwick example, which needs journeys, selects large.

Shape decides the deployment boundary: `server` for a project with its own backend, `client-only` for one without. A server project names one backend stack. A client-only project names none, and records which backend surfaces stay uncovered.

Each stack decides which technology pages and checks apply. A project names one stack per frontend, choosing react, svelte, blazor, or android for each, and the stack applies its folder and every folder above it, as the [stacks page](stacks.md) states. A preset only fills in a common combination of these choices; the project record holds the result.

Layers decide which record layers the project claims and whose practice it follows in each, as Layer Claims below states. A project that claims Implementation or Proof names its stacks.

### Layer Claims

A project claims the record layers it uses in `layers` in `.litespec/project.yaml`, and the practice each claimed layer follows. Each layer is written from the one above it and points up to it, so a project leaves layers out from the bottom: `behavior` needs `intent`, and `implementation` and `proof` each need `behavior`. Implementation and Proof both point at Behavior, not at each other, so either can be left out on its own. A project claims one of five combinations of layers:

1. `intent` alone.
2. `intent` and `behavior`.
3. `intent`, `behavior` and `implementation`.
4. `intent`, `behavior` and `proof`.
5. All four layers.

| Layer | Values | With `litespec` | With `own` |
|:---|:---|:---|:---|
| `intent` | `litespec` | The Intent records follow the record model | Not offered |
| `behavior` | `litespec` | The Behavior records follow the record model | Not offered |
| `implementation` | `litespec` or `own` | Code marks its records, and the stack, workspace and code layout pages and the analyzers' architecture rules apply | Code marks its records; the team keeps its own architecture |
| `proof` | `litespec` or `own` | Tests prove scenarios, and the testing pages and LiteSpec's test projects apply | Tests prove scenarios; the team keeps its own test practice |

Every provision and every finding code governs one record layer, or `any`, and either ties the layer to the records (`trace`) or states LiteSpec's practice for it. A provision or code applies when its layer is `any` or claimed and, for Implementation and Proof, when it ties the layer to the records or the layer follows `litespec`. `model check`, the book, `rules` and the analyzers report only what applies, and the book shows a layer the project does not claim as not claimed, never as missing. Stacks are required when Implementation or Proof is claimed, because the stack selects the code reader. A [departure](departures.md) covers one provision, a whole page or finding codes that the project does not follow.

### One Folder for Every LiteSpec File

Every file LiteSpec creates, reads, or governs in a consumer sits under one folder, `.litespec/`, at the root of the consumer's repository. Inside it, the layer folders `intent/`, `behavior/`, `implementation/`, `proof/`, and `governance/` hold the records the layers own; `project.yaml`, `glossary.yaml`, and the example cast sit at its top level; the tool builds its model in memory, keeps its working files in `model/` and writes its book to `book/` under it, and a `.gitignore` there keeps those two generated folders out of source control. Nothing of LiteSpec lives anywhere else in the repository, and no LiteSpec material sits at the repository root. Application code, tests, and infrastructure keep the places their stack pages name. Proof holds no authored records.

### Extensions, Constraints, and the Boundary

An extension is a conditional standards bundle that stays inactive until a documented condition holds. A project that operates the product in production with a formal release, deployment, rollback or alerting regime selects the `delivery` extension. The `delivery` extension owns the operations checks, the alert and runbook checks, so no layer claim bundles them. A project selects an extension only when its activation criterion holds, and the [extension catalog](../ext/README.md) lists each extension with its criteria. Outside constraints work the same way: the project states in `.litespec/project.yaml` whether any constraint applies, and an empty answer records that none does.

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

A consumer claims conformance to the layers it names in `layers` in `.litespec/project.yaml`. It meets every provision that applies to those layers, selects every extension whose criteria hold, records each [departure](departures.md) it relies on with its reason, and passes the checks that run for those layers. A claim covers only the layers it names: a claim of `intent` and `behavior` covers the record model, a claim of `implementation` adds the code marks and, with `litespec`, the stack, workspace and code layout pages, and a claim of `proof` adds the test pages and scenario proof. Governance is a group of records beside the four record layers, not a fifth layer. A project outside the supported boundary, or one that skips a provision or a check that applies to a layer it claims, makes no conformance claim for that layer.

## Standards

### Select a Size, a Shape and Stacks

**ID:** `litespec/core/scope#select-a-size-a-shape-and-stacks`

**Strength:** required

**Statement:** A consumer MUST select a size, a shape, one stack per frontend, and one backend stack unless the shape is client-only or the project claims neither `implementation` nor `proof` in `layers`.

**Rationale:** One size and explicit stacks tell every check which records and pages apply. The size decides which record types the project carries and which test projects prove them; each stack decides which technology pages and checks apply. Without the declaration, each agent infers the architecture from the directory tree, and two agents infer two architectures. A project that claims neither Implementation nor Proof may leave the stacks empty or absent, or name them for information alone, since no code reader runs for it; a project that claims either names the stacks the code reader and the checks govern.

**Example:** A project with one web frontend and no journeys selects small with the svelte stack; the running Claimwick example, with journeys, selects large.

The running Claimwick example declares large with a server shape and two stacks in `.litespec/project.yaml`:

```yaml
size: large
shape: server
stacks:
- dotnet
- svelte
layers:
  intent: litespec
  behavior: litespec
  implementation: litespec
  proof: litespec
```

The size selects the large record set, the shape selects a backend stack, and each stack selects its technology pages, which satisfies this provision.

The diagram shows size, shape and stacks and what each decides.

![Size, shape and stacks and what each decides: size, small or large, decides the record types and the test projects, with large adding goals, problems, features, journeys, and scenarios; shape, server or client-only, decides whether the project names a backend stack; stacks, one backend stack and one stack per frontend, decide the technology pages and the checks that apply.](../assets/diagrams/core-scope-selection.svg)

**Checked by:** `litespec model check` reports `stack-invalid`, when the project claims Implementation or Proof, for a shape whose backend stack count is wrong and a platform that names no frontend stack.

### Claim the Record Layers the Project Uses

**ID:** `litespec/core/scope#claim-the-record-layers-the-project-uses`

**Strength:** required

**Statement:** A consumer MUST name in `layers` in `.litespec/project.yaml` each record layer it claims, with `litespec` or `own` for Implementation and Proof, leaving layers out only from the bottom.

**Rationale:** Teams adopt LiteSpec in parts: the record model alone, records with marked code, records with acceptance tests, or all of it, and with their own architecture or test practice where they have one. Naming each layer lets every tool check exactly the part the team uses, so a missing test project is not a finding for a team that claims no Proof, and a team that keeps its own code layout still gets its marks checked. Leaving layers out from the bottom keeps every remaining link whole, because each layer points up to the one above it. Settled by [decision/0027-projects-claim-record-layers](decision/0027-projects-claim-record-layers).

**Example:** The running Claimwick example claims all four layers with `litespec`. A product that marks its code in its own layout and proves no scenarios yet claims `intent`, `behavior` and `implementation: own`, and `model check` reports no `use-case-unverified` finding for it.

**Checked by:** `litespec model check` reports `project-invalid` for a project file without `layers`, with a value other than those the table allows, or with a layer whose parent is not claimed.

### Keep One Bounded Context

**ID:** `litespec/core/scope#keep-one-bounded-context`

**Strength:** required

**Statement:** A consumer MUST keep one business language and deployment boundary.

**Rationale:** One business language and one deployment boundary keep every module, record, and use case in a single consistent model. Modules organize related language, models, use cases, and code without becoming independent services or bounded contexts. A second language or boundary splits the project into systems that the records, links, and checks can no longer hold together.

**Example:** Posts, Authors, and Comments can be modules in one publishing context.

**Reviewed for:** Module pages and `.litespec/project.yaml` name one business language and one deployment boundary, with no second of either.

### Select Conditional Extensions Explicitly

**ID:** `litespec/core/scope#select-conditional-extensions-explicitly`

**Strength:** required

**Statement:** A consumer MUST select every extension whose activation criteria apply, and no extension whose criteria do not.

**Rationale:** Conditional capabilities stay inactive until a documented product or engineering condition applies, so no project carries caching, messaging, or tenancy machinery it never justified. Selecting exactly the extensions whose criteria hold keeps the active rule set minimal and reviewable. The manifest's extension list is the closed set a project selects from, so there is no informal extension. Settled by [decision/0030-limit-the-jobs-extension](decision/0030-limit-the-jobs-extension) for the jobs extension, which keeps the subjects a small team follows.

**Example:** Caching, durable messaging, and multitenancy are extensions; the extension index lists each with its activation criteria.

**Reviewed for:** Each entry in `selectedExtensions` carries a criterion that holds, and no applicable extension from the extension catalog stays unselected.

### Record Selected Extensions

**ID:** `litespec/core/scope#record-selected-extensions`

**Strength:** required

**Statement:** A consumer MUST list every selected extension in `selectedExtensions` in `.litespec/project.yaml`, each with its criterion.

**Rationale:** The project record determines which extension boundaries apply to the project's work. Writing each selection with its criterion lets a later reader check whether the condition still holds. Without the record, extensions drift in through dependencies and prose while the checks apply a different set.

**Example:**

```json
{ "id": "cache", "criterion": "expense reads miss the latency target" }
```

**Checked by:** `litespec model check` reports `project-invalid` for a selection without a criterion.

### Answer the Constraints Question

**ID:** `litespec/core/scope#answer-the-constraints-question`

**Strength:** required

**Statement:** A consumer MUST state in `.litespec/project.yaml` whether any outside constraint applies to the project.

**Rationale:** An outside rule the project never names is a law the product can break unknowingly. Recording the answer, even when it is empty, proves the team asked the question: an empty answer states that no outside rule binds the project. A later reader can then tell a project with no constraints from one that never looked.

**Example:** A clinic booking system records that medical-device regulation does not apply to appointment scheduling.

**Reviewed for:** `.litespec/project.yaml` states which outside constraints apply, or records an empty answer.

### Declare a Consumer That Has No Backend

**ID:** `litespec/core/scope#declare-a-consumer-that-has-no-backend`

**Strength:** required

**Statement:** A consumer with no server of its own MUST declare the `client-only` shape and record a decision naming the surfaces left without coverage.

**Rationale:** The shape decides which backend pages still apply. A `client-only` product keeps a domain model, so it applies the backend architecture, domain, and application pages that the manifest lists under `shapes`. Persistence, the HTTP API, and API testing have no target without a server, and the decision names those uncovered surfaces. Adding a server only to obtain conformance contradicts `litespec/core/principles#require-current-complexity-activation`, which permits new machinery only for a current documented requirement.

**Example:** `litespec/quality/ci#run-applicable-gates-on-every-pull-request` already permits skipping a gate whose surface does not exist, so a missing backend gate is a recorded consequence rather than an unexplained absence.

**Checked by:** `litespec model check` reports `stack-invalid` for a `client-only` project that names a backend stack.

### Record Unsupported Scope Decisions

**ID:** `litespec/core/scope#record-unsupported-scope-decisions`

**Strength:** required

**Statement:** A consumer MUST record a separate project decision for microservices, multiple contexts, native clients on a platform with no mobile stack, other platforms, event sourcing, active-active regions, or large pipelines.

**Rationale:** Microservices, multiple contexts, native clients on a platform with no mobile stack, other platforms, event sourcing, active-active regions, and large pipelines each change the assumptions this page rests on: one language, one deployment, one API and database. A separate project decision records that the team saw the boundary and chose deliberately. Without it, the project drifts outside the supported scope while claiming its checks still apply.

**Reviewed for:** A project decision names each architecture outside the supported boundary the project adopts.

### Claim Conformance Only When Every Applicable Provision Holds

**ID:** `litespec/core/scope#claim-conformance-only-when-every-applicable-provision-holds`

**Strength:** required

**Statement:** A consumer MUST meet every provision that applies to each layer it claims in `layers` before claiming conformance to LiteSpec for those layers.

**Rationale:** A conformance claim tells a reader that the whole part of the contract the project claimed holds, not only the parts the project chose to follow. A claim of `intent` and `behavior` takes in the provisions about the record model; a claim of `implementation` or `proof` also takes in the provisions that tie each layer to the records. A [departure](departures.md) outranks the provision or findings it covers while it stands, so a project that records each departure still meets this provision; a departure past its expiry date reports `departure-expired` and no longer outranks them. An unrecorded departure in a claimed layer breaks the claim for that layer and leaves any layer the project does meet untouched.

**Example:** A consumer that claims all four layers with `litespec`, selects the dotnet stack and the caching extension, claims conformance when every page those selections apply holds and their checks pass, with each departure recorded. A consumer that claims `intent` and `behavior` claims conformance when the provisions about the record model hold, while its own code layout and tooling stay outside the claim.

**Reviewed for:** `.litespec/project.yaml` names each claimed layer in `layers`, beside the selected size, shape, stacks, and extensions; each departure from a provision that applies to a claimed layer resolves to a departure record under `.litespec/governance/departures/`, and the checks that run for those layers pass.

### Point Behavior Docs at the Behavior Root

**ID:** `litespec/core/scope#point-behavior-docs-at-the-behavior-root`

**Strength:** required

**Statement:** A consumer MUST set `paths.behaviorDocs` in `.litespec/project.yaml` to the folder that holds its Behavior records.

**Rationale:** The builder joins records, queries, diffs, and the book from one declared root. A Behavior tree the project record never names is one the tools never see, so its records skip every check. One declared folder keeps authors and tools on the same tree.

**Example:**

```yaml
paths:
  behaviorDocs: .litespec/behavior
```

**Checked by:** `litespec model check` reports `project-invalid` for a project record without `paths.behaviorDocs`.

### Keep Every LiteSpec File Under One Folder

**ID:** `litespec/core/scope#keep-every-litespec-file-under-one-folder`

**Strength:** required

**Statement:** A consumer MUST hold every file LiteSpec creates, reads, or governs under one folder, `.litespec/`, at the repository root.

**Rationale:** One folder with one name in every consumer makes the LiteSpec material identifiable at a glance: a reader, a tool, or an agent finds the whole contract in one place, moves or deletes it without hunting the tree, and keeps it apart from the application's code, tests, and infrastructure. Generated output inside it stays covered by its `.gitignore`, so a checkout never mixes committed records with built files. Every path in `.litespec/project.yaml` stays relative to the repository root, so moving the folder never rewrites the paths inside it, and a command finds the consumer by walking up from the working folder to the first folder that holds `.litespec/project.yaml`.

**Example:** Claimwick holds its project file, glossary, example cast, and the Intent, Behavior, Implementation, Proof, and Governance folders under `examples/claimwick/.litespec/` (in a real consumer, at its repository root), beside its application code and tests, which keep their own places. An agent asked to read the contract opens that one folder instead of collecting a root project file, a documentation tree, a model folder, and a book folder from four places.

**Reviewed for:** Which files created, read, or governed by LiteSpec sit outside `.litespec/`, or at the repository root? Look at the repository tree beside the project's `.litespec/` folder.

### Write Every Hand-Written LiteSpec File in YAML

**ID:** `litespec/core/scope#write-every-hand-written-litespec-file-in-yaml`

**Strength:** required

**Statement:** A consumer MUST write every LiteSpec file a person edits, the project file, the records, the glossary and the pattern catalog, in YAML that states its `schemaVersion` and names its schema in a first-line `# yaml-language-server: $schema=<url>` comment, while files that programs exchange, such as check results, the model and `--format json` output, stay JSON.

**Rationale:** People write these files and review them in pull requests, so one syntax with comments and no brace noise keeps every edit readable, and one syntax across the folder means a reader learns it once. The schema comment gives an editor completion and validation without a key in the data, and `schemaVersion` tells a tool which shape the file holds. Programs gain nothing from YAML, so their output stays JSON, which every tool reads without a parser of its own. Settled by [decision/0023-every-handwritten-file-in-yaml](decision/0023-every-handwritten-file-in-yaml).

**Example:** Claimwick's `.litespec/project.yaml` starts with the comment naming `https://litespec.dev/<version>/schemas/litespec-project.schema.yaml`, then `schemaVersion: 1`, and lists its layers, size, shape and stacks as YAML keys; its check results under `.litespec/proof/checks/` stay JSON files written by its harness.

**Checked by:** `litespec model check` reports `project-invalid` for a project file that is not `.litespec/project.yaml` or lacks `schemaVersion`; scope: the project file.

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
