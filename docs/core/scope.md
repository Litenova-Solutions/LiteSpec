# Supported Scope

**Stack:** any

**Concern:** workspace

LiteSpec starts with four choices in `.litespec/project.yaml`: the size, shape, stacks, and record layers of one bounded-context business application. This page states those choices, the folder holding every LiteSpec file, and how extensions and outside limits fit. Set them during inception: they decide which records the project writes, which pages apply, and which checks run.

## Concepts

### The Four Project Choices

Claimwick declares its choices up front, and every check reads them from there:

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

Size is the record set the project carries. A small project carries the core record types, proved by the Architecture and Integration test projects. A large project adds goals, problems, features, flows, and scenarios, proved by the Acceptance project as well. A project with one web frontend and no flows fits small, while Claimwick has flows and selects large.

Shape is the deployment boundary. A server project names one backend stack. A client-only project names none and records a decision naming the surfaces left without coverage: persistence, the HTTP API, and API testing have no target without a server, while the domain model and the backend architecture, domain, and application pages the manifest lists under shapes still apply.

A stack is the technology choice for the backend or for one frontend. A project names one stack per frontend, such as svelte or android. Each stack decides which technology pages and checks apply, covering its folder and every folder above it, as the [stacks page](stacks.md) states. A preset fills in a common combination of these choices; the project record holds the result either way.

### Layer Claims

A project claims the record layers it uses in `layers` in `.litespec/project.yaml`, with the practice it follows in each:

- **Intent** holds the outcomes and problems the product answers.
- **Behavior** holds what the software does, holds, and enforces.
- **Implementation** is the code serving the Behavior records.
- **Proof** is the tests and runs proving the Behavior scenarios.

Each layer is written from the one above it and points up to it. A project leaves layers out from the bottom: `behavior` needs `intent`, while `implementation` and `proof` each need `behavior` and point at it directly, so either one stands alone beside the first two. A project claims one of five combinations:

- `intent` alone.
- `intent` and `behavior`.
- `intent`, `behavior`, and `implementation`.
- `intent`, `behavior`, and `proof`.
- All four layers.

| Layer | Values | With `litespec` | With `own` |
|:---|:---|:---|:---|
| `intent` | `litespec` | The Intent records follow the record model | Not offered |
| `behavior` | `litespec` | The Behavior records follow the record model | Not offered |
| `implementation` | `litespec` or `own` | Code marks its records, and the stack, workspace and code layout pages and the analyzers' architecture rules apply | Code marks its records; `own` drops LiteSpec's code layout, stack shapes, and analyzer architecture rules, and the code honors the Behavior units of work below |
| `proof` | `litespec` or `own` | Tests prove scenarios, and the testing pages and LiteSpec's test projects apply | Tests prove scenarios; the team keeps its own test practice |

With `implementation: own`, the Behavior model still describes the system in modules, aggregates, stores, use cases, reactions, and jobs, and the code honors those units of work:

- One command commits once.
- Follow-on work runs from a reaction or a job.

Every provision and every finding code governs one record layer, or `any`. Each one either ties its layer to the records (`trace`) or states LiteSpec's practice for the layer. A provision or code applies when its layer is `any` or claimed. For Implementation and Proof, it also needs to tie the layer to the records, or the layer needs to follow `litespec`. The `model check`, the book, `rules`, and the analyzers report exactly the claimed part. The book shows a layer left out as not claimed, not as missing.

A project claiming Implementation or Proof names its stacks, since the stack selects the code reader. A [waiver](waivers.md) covers one provision, a whole page, or finding codes left unfollowed.

### One Folder for Every LiteSpec File

Every LiteSpec record, document, explanation file, and generated file in a consumer sits under one folder, `.litespec/`, at the root of the consumer's repository. No record, document, explanation file, or generated file sits at the repository root. Application code, tests, contracts, and `AGENTS.md` keep their own homes: code, tests, and contracts keep the places their stack pages name, and `AGENTS.md` stays at the repository root.

Inside `.litespec/`, the layer folders `intent/`, `behavior/`, `implementation/`, `proof/`, and `governance/` hold the records the layers own. The files `project.yaml`, `glossary.yaml`, and `cast.yaml` sit at its top level, with the cast's explanation `cast.md` beside it. Proof holds no authored records. Structured LiteSpec data is YAML; explanation files are Markdown.

The tool builds its model in memory, keeps its working files in `model/`, and writes its book to `book/` under `.litespec/`. A `.gitignore` there keeps those two generated folders out of source control, so a checkout holds committed records apart from built files. A command finds the consumer by walking up from the working folder to the first folder holding `.litespec/project.yaml`.

### Extensions, Constraints, and the Boundary

An extension is a conditional standards bundle staying inactive until a documented condition holds. A project selects an extension when its activation criterion holds, and no extension whose criterion fails. The [extension catalog](../ext/README.md) lists each extension with its criteria. A product operating in production with a formal release, deployment, rollback, or alerting regime selects the `delivery` extension, which owns the operations checks and the alert and runbook checks. No layer claim bundles them.

Outside constraints work the same way. The project file states whether any constraint applies, and an empty answer records that none does. An empty answer tells a later reader the team asked the question and found no outside rule binding the project.

LiteSpec specifies the product, not the company building it. An Intent or Behavior record states behavior the application performs, data it holds, or a limit it enforces. Company operation enters as a constraint or as dated research.

| Type | Belongs in Records When | Stays Out When |
|:---|:---|:---|
| Goal | A software outcome: "Nights run without oversell or unreconciled money" | A business outcome: retention, revenue, market share |
| Problem | A user pain the software answers | A sales or market problem |
| Feature | A capability the software has | A marketing tactic or a launch plan |
| Requirement | A commitment Behavior fulfills | (software by the time it reaches Behavior) |
| Constraint | Law, a provider or a limit binding the software | A company duty with no software effect, such as incorporation |
| Research | Evidence a software record cites | Market sizing, viability, acquisition, pricing strategy |
| Module | A product area of the bounded context | A company function |

Each module is declared under `behavior/modules/<module>/`, so one name runs through Behavior and code. A folder under `behavior/modules/` naming no declared module gets a blocking finding. Each feature sits in the `features/` folder of the one problem it answers. Product-level requirements sit in `intent/requirements/`. Anything cross-cutting, such as accessibility or security, sits at product level.

Some architectures sit outside what LiteSpec supports: microservices, multiple contexts, native clients on a platform with no mobile stack, other platforms, event sourcing, active-active regions, and large pipelines. A project considering one records a separate project decision naming it. Projects outside the boundary still use individual ideas, but they make no conformance claim.

A conformance claim covers exactly the layers the project names in `layers` in `.litespec/project.yaml`. It meets every provision applying to them, selects every extension whose criteria hold, records each [waiver](waivers.md) it relies on with its reason, and passes the checks running for those layers. A claim of `intent` and `behavior` covers the record model. A claim of `implementation` adds the code marks and, with `litespec`, the stack, workspace, and code layout pages. A claim of `proof` adds the test pages and scenario proof.

### Starting Small and Growing

A new product starts with one API deployable and one database. Each extra deployable or store adds deployment, data-consistency, and operations work a small team rarely needs at the start. A background job appears when a process continues without an HTTP request. A project running more records its topology convention naming the deployables and stores it runs instead.

Caches, replicas, queues, and partitioning each answer a different limitation, and measurements show which one the project hit. The team records current load, the latency target, and the observed constraint before adding one, so the added capacity answers the observed limit. A project expanding without measurements records a capacity convention naming what justifies expansion instead.

## Standards

### Select a Size, a Shape and Stacks

**ID:** `litespec/core/scope#select-a-size-a-shape-and-stacks`

**Strength:** required

**Statement:** A consumer MUST select a size, a shape, one stack per frontend, and one backend stack unless the shape is client-only or the project claims neither `implementation` nor `proof` in `layers`.

**Rationale:** Undeclared size and stacks leave each agent inferring the architecture from the directory tree, and two agents infer two architectures. Declared once, they tell every check which records and pages apply: the size decides which record types the project carries and which test projects prove them, and each stack decides which technology pages and checks apply. A project with no Implementation or Proof claim needs no code reader, so it leaves stacks empty or names them for information. Settled by decision/0122-isolate-three-stack-packs.

**Example:** A project with one web frontend and no flows selects small with the svelte stack. The running Claimwick example has flows, so it selects large. Claimwick declares large with a server shape and two stacks in `.litespec/project.yaml`:

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

Size selects the large record set, shape selects a backend stack, and each stack selects its technology pages, which satisfies this provision.

The diagram below shows what each choice decides.

![Size, shape and stacks and what each decides: size, small or large, decides the record types and the test projects, with large adding goals, problems, features, flows, and scenarios; shape, server or client-only, decides whether the project names a backend stack; stacks, one backend stack and one stack per frontend, decide the technology pages and the checks that apply.](../assets/diagrams/core-scope-selection.svg)

**Checked by:** `litespec model check` reports `stack-invalid`, when the project claims Implementation or Proof, for a shape whose backend stack count is wrong and a platform that names no frontend stack.

### Claim the Record Layers the Project Uses

**ID:** `litespec/core/scope#claim-the-record-layers-the-project-uses`

**Strength:** required

**Statement:** A consumer MUST name each record layer it claims in `layers` in `.litespec/project.yaml`, with `litespec` or `own` for Implementation and Proof, leaving layers out only from the bottom.

**Rationale:** Teams adopt LiteSpec in parts: the record model alone, records with marked code, records with acceptance tests, or all of it, with their own architecture or test practice where they have one. Naming each layer lets every tool check exactly the claimed part, so a missing test project is no finding for a team claiming no Proof, and a team keeping its own code layout still gets its marks checked. Leaving layers out from the bottom keeps every remaining link whole, since each layer points up to the one above it. See also decision/0027-projects-claim-record-layers.

**Example:** The running Claimwick example claims all four layers with `litespec`. A product marking its code in its own layout and proving no scenarios yet claims `intent`, `behavior`, and `implementation: own`, and `model check` reports no `use-case-unverified` finding for it.

**Checked by:** `litespec model check` reports `project-invalid` for a project file without `layers`, with a value other than those the table allows, or with a layer whose parent is not claimed.

### Keep One Bounded Context

**ID:** `litespec/core/scope#keep-one-bounded-context`

**Strength:** required

**Statement:** A consumer MUST keep one business language and deployment boundary.

**Rationale:** One business language and one deployment boundary keep every module, record, and use case in a single consistent model. Modules organize related language, models, use cases, and code without becoming independent services or bounded contexts. A second language or boundary splits the project into systems the records, links, and checks no longer hold together.

**Example:** A publishing product keeps Posts, Authors, and Comments as modules in one context, with one shared language.

**Reviewed for:** Does the project keep one business language and one deployment boundary? Check the module pages and `.litespec/project.yaml`.

### Select Conditional Extensions Explicitly

**ID:** `litespec/core/scope#select-conditional-extensions-explicitly`

**Strength:** required

**Statement:** A consumer MUST select every extension whose activation criteria apply, and no extension whose criteria do not.

**Rationale:** Conditional capabilities stay inactive until a documented product or engineering condition applies, so each project carries machinery a recorded condition justifies. Selecting exactly the extensions whose criteria hold keeps the active rule set minimal and reviewable. The manifest's extension list is the closed set to select from, so no informal extension slips in. See also decision/0030-limit-the-jobs-extension and decision/0126-consolidate-everyday-capabilities-into-core.

**Example:** Caching, durable messaging, and multitenancy are extensions. The extension index lists each with its activation criteria.

**Reviewed for:** Does `selectedExtensions` list exactly the extensions whose criteria hold? Check each entry against the extension catalog.

### Record Selected Extensions

**ID:** `litespec/core/scope#record-selected-extensions`

**Strength:** required

**Statement:** A consumer MUST list every selected extension in `selectedExtensions` in `.litespec/project.yaml`, each with its criterion.

**Rationale:** Without a written selection, extensions drift in through dependencies and prose while the checks apply a different set. A later reader has no way to tell which condition justified each extension, or whether it still holds. Writing each selection with its criterion keeps the project record as the one place deciding which extension boundaries apply.

**Example:**

```json
{ "id": "realtime", "criterion": "approval updates miss the latency target" }
```

**Checked by:** `litespec model check` reports `project-invalid` for a selection without a criterion.

### Declare a Consumer That Has No Backend

**ID:** `litespec/core/scope#declare-a-consumer-that-has-no-backend`

**Strength:** required

**Statement:** A consumer with no server of its own MUST declare the `client-only` shape and record a decision naming the surfaces left without coverage.

**Rationale:** The shape decides which backend pages still apply. A `client-only` product keeps a domain model, so the backend architecture, domain, and application pages the manifest lists under `shapes` still apply to it. Persistence, the HTTP API, and API testing have no target without a server, and the decision names those uncovered surfaces.

**Example:** A library catalog client declares `client-only` and records a decision naming persistence and the HTTP API as uncovered, while its domain model still follows the backend domain and application pages.

**Checked by:** `litespec model check` reports `stack-invalid` for a `client-only` project that names a backend stack.

### Point Behavior Docs at the Behavior Root

**ID:** `litespec/core/scope#point-behavior-docs-at-the-behavior-root`

**Strength:** required

**Statement:** A consumer MUST set `paths.behaviorDocs` in `.litespec/project.yaml` to the folder that holds its Behavior records.

**Rationale:** Tools see no Behavior tree the project record fails to name, so its records skip every check. The builder joins records, queries, diffs, and the book from one declared root. One declared folder keeps authors and tools on the same tree.

**Example:**

```yaml
paths:
  behaviorDocs: .litespec/behavior
```

**Checked by:** `litespec model check` reports `project-invalid` for a project record without `paths.behaviorDocs`.

### Keep Every LiteSpec File Under One Folder

**ID:** `litespec/core/scope#keep-every-litespec-file-under-one-folder`

**Strength:** required

**Statement:** A consumer MUST hold every LiteSpec record, document, explanation file, and generated file under one folder, `.litespec/`, at the repository root.

**Rationale:** LiteSpec material scattered across the repository sends readers and tools hunting the tree for it. One folder with one name in every consumer makes the material identifiable at a glance: a reader, a tool, or an agent finds the whole contract in one place, moves or deletes it without hunting, and keeps it apart from application code, tests, contracts, and `AGENTS.md`, which keep their own homes. Generated output inside it stays covered by its `.gitignore`, so a checkout holds committed records apart from built files. Every path in `.litespec/project.yaml` stays relative to the repository root. A command finds the consumer by walking up from the working folder to the first folder holding `.litespec/project.yaml`.

**Example:** Claimwick holds its project file, glossary, cast, and its Intent, Behavior, Implementation, Proof, and Governance folders under `.litespec/`, beside its application code, tests, contracts, and `AGENTS.md`, which keep their own homes. An agent asked to read the contract opens that one folder, instead of collecting a root project file, a documentation tree, a model folder, and a book folder from four places.

**Reviewed for:** Does any LiteSpec record, document, explanation file, or generated file sit outside `.litespec/` or at the repository root? Look at the repository tree beside the project's `.litespec/` folder.

### Write Every Hand-Written LiteSpec File in YAML

**ID:** `litespec/core/scope#write-every-hand-written-litespec-file-in-yaml`

**Strength:** required

**Statement:** A consumer MUST write every structured LiteSpec file a person edits, the project file, the records, the glossary, and the pattern catalog, in YAML that states its `schemaVersion` and names its schema in a first-line `# yaml-language-server: $schema=<url>` comment.

**Rationale:** Without one syntax, readers learn a new format per file, and edits hide in brace noise. People write these files and review them in pull requests, so one syntax with comments keeps every edit readable, and one syntax across the folder means a reader learns it once. The schema comment gives an editor completion and validation without a key in the data, and `schemaVersion` tells a tool which shape the file holds. See also decision/0023-every-handwritten-file-in-yaml. Files programs exchange, such as check results, the model, and `--format json` output, stay JSON, which every tool reads without a parser of its own.

**Example:** Claimwick's `.litespec/project.yaml` starts with the comment naming `https://litespec.dev/<version>/schemas/litespec-project.schema.yaml`, then `schemaVersion: 1`, and lists its layers, size, shape, and stacks as YAML keys. Its explanation files beside the records are Markdown. Its check results under `.litespec/proof/checks/` stay JSON, written by its harness.

**Checked by:** `litespec model check` reports `project-invalid` for a project file that is not `.litespec/project.yaml` or lacks `schemaVersion`; scope: the project file.

## Conventions

None.

## Reference Example

This informative example demonstrates `litespec/core/scope#select-a-size-a-shape-and-stacks` and `litespec/core/scope#select-conditional-extensions-explicitly`.

A publishing product supports sign-in, draft creation, publication, and public reading inside one bounded context. It selects small with a server shape and the dotnet stack, since it ships one frontend and no flows. Multi-tenant publication, live editing, and bulk analytics each sit inside a selected extension boundary with its own activation criterion.
