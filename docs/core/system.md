# LiteSpec

**Stack:** any

**Concern:** records

LiteSpec is a system for building software from written records: the records state the behavior the project builds, and people and agents build, verify, and operate against them. It covers one bounded-context business application, the parts the system combines, and the rules joining intent to code. Read this page first when starting a project, so every later choice lands in one shared model.

## Concepts

### Product Scope

LiteSpec specifies one bounded-context business application: the behavior the application performs, the data it holds, and the limits it enforces. An Intent or Behavior record states behavior the application performs, data it holds, or a limit it enforces.

Running the company building the application sits outside the standard. A price as an offer, a market, a hiring or sales action, a launch calendar, a company goal, and a founder risk enter consumer records as a constraint, a limit the application enforces or an authority imposes, or as dated research. Plan entitlements the software enforces stay as application behavior in settings and permissions; the price list as a company offer stays out.

LiteSpec names responsibilities instead of departments: product, domain, architecture, implementation, testing, release, and operations responsibilities. One person holds all of them at the same time.

Agents perform a large part of the engineering work: an agent inspects repositories, proposes designs, edits authorized artifacts, runs tools, and collects evidence. An agent stops affected work on an unknown fact changing behavior, money, data, access, or recovery, and takes the question to an accountable person. Accountable people decide outcomes, terms, policies, and criteria, and an agent contribution stays a proposal until one of them accepts it.

Since agents do so much execution, the system defines its technical concepts exactly. An agent tells a module from an aggregate, a flow from a reaction or job, and a record from a claim the implementation makes about itself.

### Why the System Exists

Two capable contributors answer one request with different boundaries, names, transaction behavior, and verification. Each result compiles while the workspace loses a coherent model. The same happens with two agents, or with one agent on two different days.

The system answers five recurring problems, from product intent lost between a request and its implementation to documentation no evidence supports. [Why LiteSpec Exists](../learn/why-litespec.md#the-problem) lists them in full.

The system makes the record durable and the agent replaceable: a different person, model, or tool continues from the same records and reaches the same boundaries.

### Connected Layers

The name has two parts. `Lite` means a consumer adopts one bounded context, one size, and its selected stacks, and nothing more. `Spec` means written records and provisions define the work, and agents and people build and verify against them.

The system holds one delivery approach and one operating model:

| Part | Meaning | Concrete Example |
|:---|:---|:---|
| LiteSpec | The complete product specification: standards, records, implementation, verification, and the operating behavior the application performs. | The repository structure, provision IDs, templates, tests, runbooks, and release records. |
| Record-Driven Engineering | Records select work and define completion. | `use-case/spending.submit-expense` defines behavior before its handler, endpoint, and tests are accepted. |
| Agent-Driven Delivery | Agents perform large engineering execution inside stated scope and report decisions needing an accountable person. | An agent loads the submission record, implements it, runs its checks, and reports an unknown access rule. |

`Driven` means engineering work starts from a record, and completion is judged against that record. A record outlives one agent session in the workspace, which makes it more than a prompt. `Agent-driven` describes who performs the execution. It transfers no business decision authority to the agent.

### What the System Combines

The system combines product intent, domain behavior, access control, and the operating behavior the application itself performs into one specification joining intent to code:

- The [intent page](intent.md) states goals, problems, features, constraints, research, decisions, requirements, flows, and roles.
- The [behavior page](behavior.md) states the bounded context, modules, the glossary, aggregates, rules, domain events, use cases, results, scenarios, reactions, jobs, notifications, and settings.
- The [access page](access.md) states actors, permissions, entry points, and external systems.

The [layers page](layers.md) states how each record layer is written from the one above it. The [model page](model.md) states records, identifiers, links, authority, authoring order, verification, and the builder computing how intent connects to code.

The mark rules keep their one home on the [model page](model.md). The [Write Code for a Use Case](../guide/write-code-for-a-use-case.md) page states where each code element goes, the mark it carries, and what checks it. Each page owns its tables and rules, and this page cites them without copying. A reader needing a record type, a link field, or the authority order opens the owning page.

### Where Project Docs Live

Project docs follow the docs layout the model page states, creating a directory with its first real artifact. A project keeping its own documentation layout names it in its project documentation.

Each module use case file sits in its own folder under a plural kebab-case aggregate subdirectory, which holds the aggregate's `aggregate.yaml`. The use case identifier stays `use-case/<module>.<name>`; the directory changes. Each module keeps the same typed layout, so a module with one aggregate has the same folders as any other.

Operating and security reference prose sits under `.litespec/implementation/operations/`, where an on-call reader looks first for runbooks, limits, and inventories. Enforceable security and privacy rules belong to their owning use case fields and rules, since those are what the code and tests prove.

Prose uses aggregate, command, query, reaction, job, repository, projection, outbox, idempotency, and transaction with their accepted technical meanings, and writes module, aggregate, use case, flow, rule, domain event, and decision as ordinary nouns outside names and identifiers. Exact record types and code types keep their declared capitalization. The contrast tells a reader whether a word names the concept or the artifact.

## Standards

### Point Code and Tests up to Records

**ID:** `litespec/core/system#point-code-and-tests-up-to-records`

**Strength:** required

**Applies to:** artifact: any; layer: implementation; trace: yes

**Statement:** Every code element the marks table on the [model page](model.md) assigns a mark MUST carry one naming the record it serves, with each handler marking exactly one record.

**Rationale:** A folder or a type name resembles its record, and a resemblance drifts with the next rename. The mark is the link from Implementation to Behavior and from Proof to the scenario it proves, in the two kinds the [model page](model.md) states: `implements` names the record served and `follows` names the choice followed. The mark holds the identifier, so the checks, the analyzers, and the editor follow the same link without guessing. One record per handler keeps each file answerable to one use case. Nothing above Implementation is computed from code, so the link points up and the records stay the authority.

**Example:** The reimburse-expense handler carries a mark naming `use-case/spending.reimburse-expense`, the `Reimburse` method on the expense root carries the same mark, and the reminder event handler carries a mark naming `reaction/spending.send-approver-reminder-on-expense-submitted`.

**Checked by:** `litespec model check` reports `mark-missing`, `mark-unknown`, and `mark-count-invalid` (check level S) for a code element with no mark, a mark naming no record of the type its element takes, or a handler marking other than one record.

### Point Every Technical Class up to Its Record or Choice

**ID:** `litespec/core/system#point-every-technical-class-up-to-its-record-or-choice`

**Strength:** required

**Applies to:** artifact: any; layer: implementation; trace: yes

**Statement:** Every technical class the marks table on the [model page](model.md) names MUST carry at least one mark: an `implements` mark naming a Behavior record it serves, or a `follows` mark naming the LiteSpec provision, decision, waiver, or project convention that shapes it.

**Rationale:** Every class in the outer layers exists for a record or for a reason. An input validator or a read model serves one use case, and a repository serves one aggregate, so each names that record in `implements` like the handler beside it. Code serving no record still exists for a reason: a provision requires it, a technical decision chose it, a waiver allows it, or a project convention states it, and a `follows` mark names that reason by identifier.

The named target is the reason for the shape the class carries, so review judges a `follows` mark by whether its target requires that shape. A reader asking why the command pipeline commits once, or why the repository stores documents, follows the link instead of guessing, and a check finds the code still following a deleted decision. A class serves a record in a chosen way with both kinds.

**Example:** The Claimwick submit-expense validator carries `/// <implements>use-case/spending.submit-expense</implements>`. The receipt PDF renderer carries `/// <follows>decision/0005-render-receipts-as-pdf</follows>`, and the command pipeline's commit step carries `/// <follows>litespec/core/backend-persistence#commit-each-command-pipeline-once</follows>`. The payout bank port carries `/// <follows>claimwick/conventions#confirm-large-payouts</follows>`.

**Checked by:** `litespec model check` reports `mark-missing` and `mark-unknown` (check level S), and `dotnet build` reports `LS5001` and `LS5002`, for a technical class with no mark, an `implements` mark naming no Behavior record, or a `follows` mark naming no provision of the carried release, no decision, waiver, or project convention, or a provision on a page the project's stacks do not apply.

### Declare Every Edge Between Records

**ID:** `litespec/core/system#declare-every-edge-between-records`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every edge between use cases, reactions, jobs, notifications, integration events, and external systems MUST be declared in a record and checked.

**Rationale:** The declared edges form the effect graph the checks verify, and an undeclared edge is the defect, not the mechanism carrying it. Cycles in the graph end in a domain event with a stated end condition. No edge shares a transaction: a use case commits its own writes, a reaction or job alone runs a use case, and an outbound integration event follows from the domain event it translates. The [behavior page](behavior.md) states the edge fields, and the [layers page](layers.md) states how Implementation is checked against them.

**Checked by:** `litespec model check` emits no `effect-undeclared` or `effect-loop-invalid` finding across use cases, reactions, jobs, notifications, and integration events, and reports `effect-unimplemented` (check level S) for a declared edge no code takes.

### Deliver One Complete Use Case

**ID:** `litespec/core/system#deliver-one-complete-use-case`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A use case MUST complete with behavior, persistence, entry points, evidence, and operating impact.

**Rationale:** A use case is done when a caller runs it and operations support it: behavior, storage, entry points, automated evidence, and operating impact together. Accepting a thinner slice leaves the missing parts for later, and later rarely itemizes what shipped half-built. Placeholder work leaves the use case unimplemented. Multi-step product outcomes belong to a flow rather than stretching one use case across several goals.

**Example:** The Claimwick `use-case/spending.submit-expense` use case without its tests and operating records is not complete.

**Reviewed for:** Does every use case page show behavior, persistence, entry points, tests, and operating records? Check the use case page against its records.

### Update Records with Behavior

**ID:** `litespec/core/system#update-records-with-behavior`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An observable behavior change MUST update its use case page, implementation, tests, product records, decision, flow, and operating records together.

**Rationale:** Records map to current names and paths when each behavior change carries them along. A change moving code but leaving the use case page, the tests, or the flow behind splits the project into stated fiction and unstated fact. Updating the records in the same change keeps the change to behavior and the change to its description in one place.

**Reviewed for:** Does every behavior change update its use case page, tests, product records, decision, flow, and operating records? Check the behavior change against its records.

## Conventions

None.

## Reference Example

This informative example demonstrates `litespec/core/system#deliver-one-complete-use-case`, `litespec/core/system#update-records-with-behavior`, `litespec/core/system#point-code-and-tests-up-to-records`, `litespec/core/system#declare-every-edge-between-records`, and `litespec/core/model#compute-proof-statuses-in-the-builder`.

The `use-case/spending.submit-expense` use case is complete with its behavior, persistence, entry points, tests, and operating records. A change to the receipt threshold updates the use case page, the tests, the requirement, the decision, the flow, and the operating records in the same change. The handler mark points up to the use case record, every edge the change adds is declared on its record, and the book marks the chapter verified after the tests pass.
