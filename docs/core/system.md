# LiteSpec

**Stack:** any

**Concern:** records

## Intent

LiteSpec is a system for building software from written records: the records state the required behavior, and people and agents build, verify, and operate against them. This page states what that system is and which parts it combines, while the intent, behavior, access, layers, and model pages state the records and the rules between them. A team starting a project reads it first, so every later choice lands in one shared model joining intent to code.

## Concepts

### Product Scope

LiteSpec specifies one bounded-context business application: the behavior the application performs, the data it holds, and the limits it enforces. An Intent or Behavior record states only behavior the application performs, data it holds, or a limit it enforces. Running the company that builds the application sits outside the standard: a price as an offer, a market, a hiring or sales action, a launch calendar, a company goal and a founder risk enter a consumer's records only as a constraint, a limit the application enforces or an authority imposes, or as dated research. Plan entitlements the software enforces stay as application behavior in settings and permissions; the price list as a company offer does not.

LiteSpec names responsibilities instead of departments: product, domain, architecture, implementation, testing, release, and operations responsibilities, and one person can hold all of them at the same time.

Agents perform a substantial part of the engineering work. An agent can inspect repositories, propose designs, edit authorized artifacts, run tools, and collect evidence. An agent never decides unresolved product policy, legal judgment, or financial decisions; those stay with accountable people.

Because agents do so much execution, the system defines its technical concepts exactly. An agent tells a module from an aggregate, a journey from a reaction or job, and a record from a claim the implementation makes about itself.

### Why the System Exists

Two capable contributors can answer one request with different boundaries, names, transaction behavior, and verification. Each result can compile while the workspace loses a coherent model. The same happens with two agents, or with one agent on two different days.

The system addresses five recurring problems, from product intent lost between a request and its implementation to documentation no evidence supports, which [Why LiteSpec Exists](../learn/why-litespec.md#the-problem) lists in full.

The system makes the record durable and the agent replaceable: a different person, model, or tool continues from the same records and reaches the same required boundaries.

### Connected Layers

The name has two parts. `Lite` means a consumer adopts one bounded context, one size, and its selected stacks, and nothing more. `Spec` means written records and provisions define the work, and agents and people build and verify against them.

The system holds one delivery approach and one operating model:

| Layer | Meaning | Concrete Example |
|:---|:---|:---|
| LiteSpec | The complete product specification: standards, records, implementation, verification, and the operating behavior the application performs. | The repository structure, provision IDs, templates, tests, runbooks, and release records. |
| Record-Driven Delivery | Records select work and define completion. | `use-case/spending.submit-expense` defines behavior before its handler, endpoint, and tests are accepted. |
| Agent-Driven Engineering | Agents perform substantial engineering execution within stated scope and report decisions that need an accountable person. | An agent loads the submission record, implements it, runs its checks, and reports an unknown access rule. |

`Driven` means engineering work starts from a record, and completion is judged against that record. A record outlives one agent session in the workspace, which is what makes it more than a prompt.

`Agent-driven` describes who performs the execution. It never transfers business decision authority to the agent.

### What the System Combines

The system combines product intent, domain behavior, access control, and the operating behavior the application itself performs into one specification that joins intent to code. The [intent page](intent.md) states goals, problems, features, constraints, research, decisions, requirements, journeys, modules, and roles. The [behavior page](behavior.md) states the bounded context, the glossary, aggregates, rules, events, use cases, results, scenarios, reactions, jobs, notifications, and settings. The [access page](access.md) states actors, permissions, entry points, and external systems. The [layers page](layers.md) states how each record layer is written from the one above it, and the [model page](model.md) states records, identifiers, links, authority, authoring order, verification, and the builder that computes how intent connects to code. What the builder computes lives in `litespec/core/model#compute-proof-statuses-in-the-builder`. The mark rules keep their one home on the [model page](model.md), and the [Write Code for a Use Case](../guide/write-code-for-a-use-case.md) page states where each code element goes, the mark it carries, and what checks it.

Each page owns its tables and rules, and this page cites them without copying. A reader who needs a record type, a link field, or the authority order opens the owning page.

## Standards

### Keep Decision Authority with Accountable People

**ID:** `litespec/core/system#keep-decision-authority-with-accountable-people`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST stop affected work and take to an accountable person an unknown fact that changes behavior, money, data, access, or recovery.

**Rationale:** Agents meet unknowns in the middle of a task: an unknown access rule, an unclear retention limit, a missing recovery target. When the answer changes behavior, money, data, access, or recovery, only an accountable person can give it. Stopping and taking the question to that person keeps the choice visible instead of buried in code nobody stated. Accountable people decide outcomes, terms, policies, and criteria, and an agent contribution stays a proposal until one of them accepts it.

**Reviewed for:** The change report naming the unknown fact, the person asked, and the record carrying the answer.

### Drive Work from Records

**ID:** `litespec/core/system#drive-work-from-records`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An agent MUST start behavior work from the Intent records and the Behavior records that own the behavior.

**Rationale:** A prompt, ticket, chat message, or code comment can start work, but none of them holds the stated behavior. Starting from the owning records keeps the agent building what the records state, and keeps the records as the lasting statement of what the code does. Without this rule, each task reinterprets intent from its starting message, and the records drift from the code until nobody trusts either.

**Reviewed for:** The change report listing the Intent and Behavior records the work started from.

### Point Code and Tests up to Records

**ID:** `litespec/core/system#point-code-and-tests-up-to-records`

**Strength:** required

**Applies to:** artifact: any; layer: implementation; trace: yes

**Statement:** Every code element the marks table on the [model page](model.md) assigns a mark MUST carry one naming the record it serves, with each handler marking exactly one record.

**Rationale:** The mark is the link from Implementation to Behavior and from Proof to the scenario it proves, in the two kinds the [model page](model.md) states: `implements` names the record served and `follows` names the choice followed. A folder or a type name only resembles the record, and a resemblance drifts with the next rename; a mark holds the identifier, so the checks, the analyzers and the editor follow the same link without guessing. One record per handler keeps each file answerable to one use case. Nothing above Implementation is computed from code, so the link points up and the records stay the authority.

**Example:** The reimburse-expense handler carries a mark naming `use-case/spending.reimburse-expense`, the `Reimburse` method on the expense root carries the same mark, and the reminder event handler carries a mark naming `reaction/spending.send-approver-reminder-on-expense-submitted`.

**Checked by:** `litespec model check` reports `mark-missing`, `mark-unknown` and `mark-count-invalid` (check level S) for a code element with no mark, a mark naming no record of the type its element takes, or a handler marking other than one record.

### Point Every Technical Class up to Its Record or Choice

**ID:** `litespec/core/system#point-every-technical-class-up-to-its-record-or-choice`

**Strength:** required

**Applies to:** artifact: any; layer: implementation; trace: yes

**Statement:** Every technical class the marks table on the [model page](model.md) names MUST carry at least one mark: an `implements` mark naming a Behavior record it serves, or a `follows` mark naming the LiteSpec provision, decision, or departure that shapes it.

**Rationale:** Every class in the outer layers exists for a record or for a reason. An input validator or a read model serves one use case, and a repository serves one aggregate, so each names that record in `implements` like the handler beside it. Code that serves no record still exists for a reason: a provision requires it, a technical decision chose it, or a departure allows it, and a `follows` mark names that reason by identifier. The named target is the reason for the shape the class carries, so review judges a `follows` mark by whether its target requires that shape. A reader asking why the command pipeline commits once, or why the repository stores documents, follows the link instead of guessing, and a check finds the code that still follows a deleted decision. A class may carry both kinds when it serves a record in a chosen way.

**Example:** The Claimwick submit-expense validator carries `/// <implements>use-case/spending.submit-expense</implements>`. The receipt PDF renderer carries `/// <follows>decision/0005-render-receipts-as-pdf</follows>`, and the command pipeline's commit step carries `/// <follows>litespec/backend/persistence#commit-each-command-pipeline-once</follows>`.

**Checked by:** `litespec model check` reports `mark-missing` and `mark-unknown` (check level S), and `dotnet build` reports `LS5001` and `LS5002`, for a technical class with no mark, an `implements` mark naming no Behavior record, or a `follows` mark naming no provision of the carried release, no decision or departure, or a provision on a page the project's stacks do not apply.

### Declare Every Edge Between Records

**ID:** `litespec/core/system#declare-every-edge-between-records`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every edge between use cases, reactions, jobs, notifications, integration events, and external systems MUST be declared in a record and checked.

**Rationale:** The declared edges form the effect graph the checks verify: an undeclared edge is the defect, not the mechanism that carries it. The graph holds no cycles except through domain events with a stated end condition. An inline edge states whether it shares the caller's transaction, and atomic consistency spans no async edge. The [behavior page](behavior.md) states the edge fields, and the [layers page](layers.md) states how Implementation is checked against them.

**Checked by:** `litespec model check` emits no `effect-undeclared` or `effect-loop-invalid` finding across use cases, reactions, jobs, notifications, and integration events, and reports `effect-unimplemented` (check level A) for a declared edge no code takes.

### Deliver One Complete Use Case

**ID:** `litespec/core/system#deliver-one-complete-use-case`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A use case MUST complete with behavior, persistence, entry points, evidence, and operating impact.

**Rationale:** A use case is done only when a caller can run it and operations can support it: behavior, storage, entry points, automated evidence, and operating impact together. Accepting a thinner slice leaves the missing parts for later, and later rarely itemizes what shipped half-built. Placeholder work leaves the use case unimplemented. Multi-step product outcomes belong to a journey rather than stretching one use case across several goals.

**Example:** A use case without its tests and operating records is not complete.

**Reviewed for:** The use case page with its behavior, persistence, entry points, tests, and operating records present.

### Update Records with Behavior

**ID:** `litespec/core/system#update-records-with-behavior`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An observable behavior change MUST update its use case page, implementation, tests, product records, decision, journey, and operating records together.

**Rationale:** Records map to current names and paths only when each behavior change carries them along. A change that moves code but leaves the use case page, the tests, or the journey behind splits the project into stated fiction and unstated fact. Updating the records in the same change keeps the change to behavior and the change to its description in one place.

**Reviewed for:** The behavior change with its use case page, tests, product records, decision, journey, and operating records changed together.

## Conventions

### Use the Docs Layout on the Model Page

**ID:** `litespec/core/system#use-the-docs-layout-on-the-model-page`

**Strength:** default

**Applies to:** artifact: any

**Default:** Use the docs layout the [model page](model.md) states under The Docs Layout, creating a directory only when its first real artifact exists.

**Replacement:** A consumer names its own documentation layout in its project documentation.

**Rationale:** One home for the docs layout means the folders and the record layers stay in one table. A second tree beside it drifts with the next reorganization, and readers follow the stale one.

**Reviewed for:** The `docs/` tree matching the layout the model page states, or the project documentation naming the replacement layout.

### Group Module Use Case Files by Aggregate

**ID:** `litespec/core/system#group-module-use-case-files-by-aggregate`

**Strength:** default

**Applies to:** artifact: record:use-case

**Default:** Group module use case files under a plural kebab-case aggregate subdirectory, each holding that aggregate's page; a module with one aggregate of the module's own name may stay flat.

**Replacement:** A consumer names its own module file grouping in its documentation.

**Rationale:** Each aggregate needs its own directory for its page and use cases, so grouping use case files under a plural kebab-case aggregate subdirectory gives every aggregate one home. A module with one aggregate of the module's own name may stay flat, avoiding a repeated segment such as `modules/products/products/`. The use case identifier stays `use-case/<module>.<name>`; only the directory changes.

**Reviewed for:** Each use case file under its module directory or its aggregate subdirectory.

### Keep Operational and Security References Under Operations

**ID:** `litespec/core/system#keep-operational-and-security-references-under-operations`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep operating and security reference prose under `.litespec/implementation/operations/`.

**Replacement:** A consumer names its own home for operating and security reference prose in its documentation.

**Rationale:** One home for operating and security reference prose means an on-call reader looks in `.litespec/implementation/operations/` first and finds the runbooks, limits, and inventories there. Scattering that prose across product and domain pages buries it where an incident never looks. Enforceable security and privacy rules still belong to their owning use case fields and rules, since those are what the code and tests prove.

**Reviewed for:** Operating and security reference prose under `.litespec/implementation/operations/`, or the project documentation naming the replacement home.

### Use Established Technical Terms

**ID:** `litespec/core/system#use-established-technical-terms`

**Strength:** default

**Applies to:** artifact: any

**Default:** Use aggregate, command, query, reaction, job, repository, projection, outbox, idempotency, and transaction with their accepted technical meanings.

**Replacement:** A consumer names its own technical vocabulary in its documentation.

**Rationale:** Names such as aggregate, command, query, reaction, job, repository, projection, outbox, idempotency, and transaction carry established meanings that developers and agents already share. Replacing one with a softer synonym hides the pattern and forces every reader to guess what the new word covers. When a term is unfamiliar, explain it and give an example instead of renaming it.

**Reviewed for:** Prose using aggregate, command, query, reaction, job, repository, projection, outbox, idempotency, and transaction with their accepted meanings.

### Use Ordinary Capitalization in Prose

**ID:** `litespec/core/system#use-ordinary-capitalization-in-prose`

**Strength:** default

**Applies to:** artifact: any

**Default:** Write module, aggregate, use case, journey, rule, event, and decision as ordinary nouns in prose.

**Replacement:** A consumer names its own capitalization rule in its documentation.

**Rationale:** Lowercase module, aggregate, use case, journey, rule, event, and decision read as ordinary nouns, while exact record types and code types keep their declared capitalization. The contrast tells a reader whether a word names the concept or the artifact. Capitalizing both blurs that distinction on every page.

**Reviewed for:** Prose writing module, aggregate, use case, journey, rule, event, and decision in lowercase outside names and identifiers.

## Reference Example

This informative example demonstrates `litespec/core/system#deliver-one-complete-use-case`, `litespec/core/system#update-records-with-behavior`, `litespec/core/system#point-code-and-tests-up-to-records`, `litespec/core/system#declare-every-edge-between-records`, and `litespec/core/model#compute-proof-statuses-in-the-builder`.

The `use-case/spending.submit-expense` use case is complete only with its behavior, persistence, entry points, tests, and operating records. A change to the receipt threshold updates the use case page, the tests, the requirement, the decision, the journey, and the operating records in the same change. The handler mark points up to the use case record, every edge the change adds is declared on its record, and the book marks the chapter verified only after the tests pass.
