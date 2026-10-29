# LiteSpec

## Intent

LiteSpec is a system for building software from approved written records: the records state the required behavior, and people and agents build, verify, and operate against them. This page states what that system is and which parts it combines, while the intent, behavior, access, layers, and model pages state the records and the rules between them. A team starting a project reads it first, so every later choice lands in one shared model joining intent to code.

## Concepts

### Company Context

Litenova Solutions authors LiteSpec and uses it as the engineering system for its own software. Another technical team can adopt the same system: the pages state the rules, and this context explains the defaults behind them.

One technical founder currently operates Litenova Solutions, so LiteSpec assumes technical contributors rather than separate departments. One person can hold product, domain, architecture, implementation, testing, release, and operations responsibilities at the same time. The system names each responsibility instead: a decision owner, an implementation owner, and an operations owner identify who answers for what, and one person can hold all three.

Agents perform a substantial part of the engineering work. An agent can inspect repositories, propose designs, edit authorized artifacts, run tools, and collect evidence. An agent never owns unresolved product policy, legal judgment, financial decisions, or risk acceptance; those stay with accountable people.

Because agents do so much execution, the system defines its technical concepts exactly. An agent tells a module from an aggregate, a journey from a reaction, and an approved record from a claim the implementation makes about itself.

### Why the System Exists

Two capable contributors can answer one request with different boundaries, names, transaction behavior, and verification. Each result can compile while the workspace loses a coherent model. The same happens with two agents, or with one agent on two different days.

The system addresses five recurring problems:

- Product intent is lost between a request and its implementation.
- An agent invents a missing rule when a record leaves a decision implicit.
- Standard technical concepts are renamed until developers no longer recognize the established pattern.
- A use case ships one part while persistence, public entry points, tests, or operating impact stay incomplete.
- Documentation claims behavior that code and automated evidence do not support.

The system makes the record durable and the agent replaceable: a different person, model, or tool continues from the same approved records and reaches the same required boundaries.

### Connected Layers

The name has two parts. `Lite` means a consumer adopts one bounded context, one size, and its selected stacks, and nothing more. `Spec` means approved written records and provisions define the work, and agents and people build and verify against them.

The system holds one delivery approach and one operating model:

| Layer | Meaning | Concrete Example |
|:---|:---|:---|
| LiteSpec | The complete system of standards, records, implementation, verification, operations, and release control. | The repository structure, provision IDs, templates, tests, runbooks, and release records. |
| Record-Driven Delivery | Approved records select work and define completion. | `use-case/spending.submit-expense` defines behavior before its handler, endpoint, and tests are accepted. |
| Agent-Driven Engineering | Agents perform substantial engineering execution within approved scope and report decisions that need an accountable person. | An agent loads the submission record, implements it, runs its checks, and reports an unknown approval policy. |

`Driven` means engineering work starts from an approved record, and completion is judged against that record. A record outlives one agent session in the workspace, which is what makes it more than a prompt.

`Agent-driven` describes who performs the execution. It never transfers business decision authority to the agent.

### What the System Combines

The system combines product intent, domain behavior, access control, operations, and project upkeep into one specification that joins intent to code. The [intent page](intent.md) states goals, problems, features, constraints, research, decisions, requirements, and journeys. The [behavior page](behavior.md) states the bounded context, the glossary, modules, aggregates, rules, events, use cases, results, scenarios, reactions, jobs, notifications, and settings. The [access page](access.md) states actors, roles, permissions, entry points, and external systems. The [layers page](layers.md) states how each record layer is written from the one above it, and the [model page](model.md) states records, identifiers, links, authority, authoring order, verification, and the builder that computes how intent connects to code.

Each page owns its tables and rules, and this page cites them without copying. A reader who needs a record type, a link field, or the authority order opens the owning page.

## Standards

### Keep Decision Authority with Accountable People

**ID:** `litespec/core/system#keep-decision-authority-with-accountable-people`

**Statement:** An agent MUST stop affected work and record the question in a draft decision when an unknown fact changes behavior, money, data, access, or recovery.

**Rationale:** Agents meet open questions in the middle of a task: an unknown approval policy, an unclear access rule, a missing recovery target. When the answer changes behavior, money, data, access, or recovery, only an accountable person can give it. Stopping and recording the question in a draft decision, with a decider and a decide-by date, keeps the choice visible and owned, instead of buried in code nobody approved. Decision owners approve outcomes, terms, policies, criteria, commitments, and risk, and an agent contribution stays a proposal until one of them accepts it.

**Reviewed for:** The draft decision pages under `docs/intent/decisions/` naming the open question, the decider, and the decide-by date.

### Drive Work from Approved Records

**ID:** `litespec/core/system#drive-work-from-approved-records`

**Statement:** An agent MUST start behavior work from the approved Intent records and the reviewed Behavior records that own the behavior.

**Rationale:** A prompt, ticket, chat message, or code comment can start work, but none of them holds the approved behavior. Starting from the owning records keeps the agent building what reviewers approved, and keeps the records as the lasting statement of what the code should do. Without this rule, each task reinterprets intent from its starting message, and the records drift from the code until nobody trusts either.

**Reviewed for:** The change report listing the approved Intent and reviewed Behavior records the work started from.

### Point Code and Tests up to Records

**ID:** `litespec/core/system#point-code-and-tests-up-to-records`

**Statement:** Every code element the marks table on the [model page](model.md) names MUST carry a mark naming the record it serves, with each handler marking exactly one record.

**Rationale:** The mark is the one link from Implementation to Behavior and from Proof to the scenario it proves. A folder or a type name only resembles the record, and a resemblance drifts with the next rename; a mark holds the identifier, so the checks, the analyzers and the editor follow the same link without guessing. One record per handler keeps each file answerable to one use case. Nothing above Implementation is computed from code, so the link points up and the records stay the authority.

**Example:** The reimburse-expense handler carries a mark naming `use-case/spending.reimburse-expense`, the `Reimburse` method on the expense root carries the same mark, and the reminder event handler carries a mark naming `reaction/spending.send-approver-reminders-on-expense-submitted`.

**Checked by:** `litespec model check` reports `mark-missing`, `mark-unresolved` and `mark-count` (tier S) for a code element with no mark, a mark naming no record of the type its element takes, or a handler marking other than one record.

### Point Technical Code up to the Choice Behind It

**ID:** `litespec/core/system#point-technical-code-up-to-the-choice-behind-it`

**Statement:** Every technical class the marks table on the [model page](model.md) names MUST carry at least one `follows` mark naming the LiteSpec provision, decision, or deviation that shapes it.

**Rationale:** Code that serves no Behavior record still exists for a reason: a provision requires it, a technical decision chose it, or a deviation allows it. A `follows` mark names that reason by identifier, so a reader asking why the command pipeline commits once, or why the repository stores documents, follows the link instead of guessing, and a check finds the code that still follows a retired decision. Code that serves a record names it in `implements` and code built for a technical reason names that reason in `follows`, so every class in the outer layers answers what it serves or why it is built as it is.

**Example:** The Claimwick command pipeline's commit step carries `/// <follows>litespec/backend/persistence#commit-each-command-pipeline-once</follows>`, and the expense repository carries `/// <follows>decision/0002-store-expenses-as-documents</follows>`.

**Checked by:** `litespec model check` reports `mark-missing` and `mark-unresolved` (tier S), and `dotnet build` reports `LS5001` and `LS5002`, for a technical class with no `follows` mark, or a `follows` mark naming no provision of the carried release, no decision or deviation, a retired record, or a provision on a page the project's stacks do not apply.

### Declare Every Edge Between Records

**ID:** `litespec/core/system#declare-every-edge-between-records`

**Statement:** Every edge between use cases, reactions, jobs, notifications, integration events, and external systems MUST be declared in a record and checked.

**Rationale:** The declared edges form the effect graph the checks verify: an undeclared edge is the defect, not the mechanism that carries it. The graph holds no cycles except through domain events with a stated end condition. An inline edge states whether it shares the caller's transaction, and atomic consistency spans no async edge. The [behavior page](behavior.md) states the edge fields, and the [layers page](layers.md) states how Implementation is checked against them.

**Checked by:** `litespec model check` emits no `effect-undeclared` or `effect-loop` finding across use cases, reactions, jobs, notifications, and integration events, and reports `effect-unimplemented` (tier A) for a declared edge no code takes.

### Deliver One Complete Use Case

**ID:** `litespec/core/system#deliver-one-complete-use-case`

**Statement:** A use case MUST complete with behavior, persistence, entry points, evidence, and operating impact.

**Rationale:** A use case is done only when a caller can run it and operations can support it: behavior, storage, entry points, automated evidence, and operating impact together. Accepting a thinner slice leaves the missing parts for later, and later rarely itemizes what shipped half-built. Placeholder work leaves the use case unimplemented. Multi-step product outcomes belong to a journey rather than stretching one use case across several goals.

**Example:** A use case without its tests and operating records is not complete.

**Reviewed for:** The use case page with its behavior, persistence, entry points, tests, and operating records present.

### Update Records with Behavior

**ID:** `litespec/core/system#update-records-with-behavior`

**Statement:** An observable behavior change MUST update its use case page, implementation, tests, product records, decision, journey, and operating records together.

**Rationale:** Approved records map to current names and paths only when each behavior change carries them along. A change that moves code but leaves the use case page, the tests, or the journey behind splits the project into approved fiction and unreviewed fact. Updating the records in the same change keeps the review of behavior and the review of its description in one place.

**Reviewed for:** The behavior change with its use case page, tests, product records, decision, journey, and operating records changed together.

### Keep Computed Facts in Proof

**ID:** `litespec/core/system#keep-computed-facts-in-proof`

**Statement:** A fact computed from code MUST live in Proof and not in an Intent or Behavior record.

**Rationale:** Implemented, verified, stale, reachable, and the effective status are computed from code and tests, so they appear in the book and the model output only. Writing one into an Intent or Behavior record freezes a figure from commit day that the next unrelated change silently falsifies. The [model page](model.md) states what the builder computes.

**Example:** The book marks a chapter verified when every scenario passes, and stale when a scenario's semantic hash changes after its test last passed.

**Reviewed for:** Which Intent or Behavior records restate a fact computed from code? Look at the statuses and counts the change adds, beside the book and model output.

## Conventions

### Use This Consumer Documentation Layout

**ID:** `litespec/core/system#use-this-consumer-documentation-layout`

**Default:** Use the consumer documentation layout in this section, creating a directory only when its first real artifact exists.

**Replacement:** A consumer names its own documentation layout in its project documentation.

**Example:**

```text
docs/
  product/
    README.md
    brief.md
    business/
      pricing.md
    markets/
      expansion.md
    roadmap/
      milestones.md
    decisions/
      0001-receipt-threshold.md
    problems/
      expenses-wait-weeks.md
    features/
      approval-reminders.md
    goals/
      decided-in-days.md
    journeys/
      submit-and-reimburse.md
    constraints/
      tax-receipt-retention.md
  behavior/
    README.md
    actors/
      employee.md
    roles/
      approver.md
    permissions.md
    entry-points.md
    systems/
      payout-bank.md
    rules/
      spending-rules.md
    modules/
      README.md
      spending/
        README.md
        expenses/
          README.md
          submit-expense.md
  implementation/
    decisions/
      0002-store-expenses-as-documents.md
  research/
    approval-time-benchmarks.md
  operations/
    limits.md
    secrets.json
    log/
  runbooks/
    restore-a-backup.md
  deviations/
    temporary-direct-database-read.md
  known-gaps/
    export-expenses-unimplemented.md
  ui/
    README.md
  glossary.json
```

`product/` holds the brief, the journeys, and the business intent: one topic page per area with its requirements, and the product decisions, goals, problems, features, and constraints behind them. `behavior/` holds the actors, roles, permissions, entry points, external systems, rules, and modules.

`operations/` holds the limits, the secret inventory, and the operations log. `runbooks/`, `deviations/`, and `known-gaps/` hold their records. `ui/` holds the first UI decision or guide. `glossary.json` holds the term records.

Create an optional directory only when its first real artifact exists. Never create empty directories or placeholder records during inception.

**Reviewed for:** The `docs/` tree matching the layout, or the project documentation naming the replacement layout.

### Group Module Use Case Files by Aggregate

**ID:** `litespec/core/system#group-module-use-case-files-by-aggregate`

**Default:** Group module use case files under a plural kebab-case aggregate subdirectory, each holding that aggregate's page, including a module with one aggregate.

**Replacement:** A consumer names its own module file grouping in its documentation.

**Rationale:** Each aggregate needs its own directory for its page and use cases, so grouping use case files under a plural kebab-case aggregate subdirectory gives every aggregate one home, including a module with one aggregate. The subdirectory costs one repeated path segment where the names coincide, as in `modules/products/products/`. The use case identifier stays `use-case/<module>.<name>`; only the directory changes.

**Reviewed for:** Each use case file under its module directory or its aggregate subdirectory.

### Keep Operational and Security References Under Operations

**ID:** `litespec/core/system#keep-operational-and-security-references-under-operations`

**Default:** Keep operating and security reference prose under `docs/behavior/operations/`.

**Replacement:** A consumer names its own home for operating and security reference prose in its documentation.

**Rationale:** One home for operating and security reference prose means an on-call reader looks in `docs/behavior/operations/` first and finds the runbooks, limits, and inventories there. Scattering that prose across product and domain pages buries it where an incident never looks. Enforceable security and privacy rules still belong to their owning use case sections and rules, since those are what the code and tests prove.

**Reviewed for:** Operating and security reference prose under `docs/behavior/operations/`, or the project documentation naming the replacement home.

### Use Established Technical Terms

**ID:** `litespec/core/system#use-established-technical-terms`

**Default:** Use aggregate, command, query, reaction, repository, projection, outbox, idempotency, and transaction with their accepted technical meanings.

**Replacement:** A consumer names its own technical vocabulary in its documentation.

**Rationale:** Names such as aggregate, command, query, reaction, repository, projection, outbox, idempotency, and transaction carry established meanings that developers and agents already share. Replacing one with a softer synonym hides the pattern and forces every reader to guess what the new word covers. When a term is unfamiliar, explain it and give an example instead of renaming it.

**Reviewed for:** Prose using aggregate, command, query, reaction, repository, projection, outbox, idempotency, and transaction with their accepted meanings.

### Use Ordinary Capitalization in Prose

**ID:** `litespec/core/system#use-ordinary-capitalization-in-prose`

**Default:** Write module, aggregate, use case, journey, rule, event, and decision as ordinary nouns in prose.

**Replacement:** A consumer names its own capitalization rule in its documentation.

**Rationale:** Lowercase module, aggregate, use case, journey, rule, event, and decision read as ordinary nouns, while exact record types and code types keep their declared capitalization. The contrast tells a reader whether a word names the concept or the artifact. Capitalizing both blurs that distinction on every page.

**Reviewed for:** Prose writing module, aggregate, use case, journey, rule, event, and decision in lowercase outside names and identifiers.

## Reference Example

This informative example demonstrates `litespec/core/system#deliver-one-complete-use-case`, `litespec/core/system#update-records-with-behavior`, `litespec/core/system#point-code-and-tests-up-to-records`, `litespec/core/system#declare-every-edge-between-records`, and `litespec/core/system#keep-computed-facts-in-proof`.

The `use-case/spending.submit-expense` use case is complete only with its behavior, persistence, entry points, tests, and operating records. A change to the receipt threshold updates the use case page, the tests, the requirement, the decision, the journey, and the operating records in the same change. The handler mark points up to the use case record, every edge the change adds is declared on its record, and the book marks the chapter verified only after the tests pass.
