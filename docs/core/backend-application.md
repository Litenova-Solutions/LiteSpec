# Application Layer

**Stack:** any
**Concern:** application

Application code turns each incoming message into one run of one use case. This page tells backend developers where that run lives: the folders, the pipeline, the ports, and the background work around it. Each behavior sits in one folder, so a reviewer sees the message, the checks, the handling, and the mapping together.

## Concepts

An application turns each message into one use case run: a command changes one aggregate and returns one result, while a query projects stored data and read models into its result. The folders below show where each part of that run lives, and the pipeline behind them runs the same stages in the same order for every message.

```text
Spending/
  Application/
    Expenses/
      ApproveExpense/      command, result, validator, handler, mapping, definitions
    ReadExpenseDirectory/  a module-level read with no aggregate folder
  WebApi/
    Expenses/
      ApproveExpense/      endpoint with request and response models
  Ports/
    ReceiptPdf/            IReceiptPdf with its signature types in Models/
```

### How Use Cases Fill Their Folders

Each command or query lives in one folder inside the plural aggregate folder of its aggregate, with its message, result, validator, handler, mapping, and pipeline-read definitions together. A module-level read drops the aggregate segment and sits directly in Application. The matching WebApi folder holds the endpoint with its request and response models when the use case answers over HTTP, and a use case with no WebApi folder answers through events or jobs instead.

Commands, queries, results, and validation errors travel as immutable values. Primitives cross the boundary while the handler rebuilds each value object, and domain-to-result projection stays in Application with transport mapping in WebApi and view models in the frontend. A command that starts a large artifact records the request and answers at once, while a job builds the file and the requester reads the finished file back.

### How Commands Move Through the Pipeline

Commands and queries travel through two separate Application-facing entry points, one for the write path and one for the read path, with each stack pinning the library behind its entry points. Each message carries a value or an exemption for every pipeline position the baseline and the selected extensions need, starting with authorization from the access on the message, with the audit extension adding its own position. A command needing exclusive access carries its hold beside the message with its key and its patience, taken after validation and released on completion, with a timeout ending as a conflict.

A command passes validation, authorization, idempotency, audit, timestamp, holds, the handler, commit, and fault classification in a fixed order, with one commit after the handler. The handler loads or creates one aggregate, calls one domain method, stages it through its repository, and returns the result. Each domain event the act raises leaves the commit as a module event or an outbound integration event through the one staging step, built from the domain event alone.

Each setting reaches handlers through a typed options value the host validates at startup, carrying the type, default, range, unit, and bound behavior from its record, while tenant-set values live in stored tenant state. Handlers use the bound options value instead of reading configuration directly. Each handler reads the message timestamp from the scoped clock the pipeline stamps once per message, so every decision in one handling answers to the same time.

### How Input Checks Share the Work

Validators parse each value object in the input, so handler construction meets values instead of raw strings. The Claimwick invite-member validator parses the mail address into its value object, and the handler takes that value directly. Queries carrying paging, filters, or ranges carry a query validator running in the same pipeline step as command validators.

Each validator reports structural problems as validity findings, with each finding carrying a member, a code, and a caller-safe message. The pipeline runs every validator and merges the findings, so the caller learns the whole form in one answer. Findings carry no transport status, because Application answers no transport.

```json
[
  { "member": "ExpenseId", "code": "required", "message": "An expense is required." },
  { "member": "EmployeeId", "code": "required", "message": "An employee is required." }
]
```

A handler loads needed records through a load that throws the aggregate not-found refusal when the record is absent. The Claimwick approve handler loads its expense as a needed record, while the optional coupon lookup returns nothing when the coupon is absent. Optional records use a lookup that returns an empty result.

### How Refusals and Results Read to Callers

Each expected refusal carries its result code with a caller-safe message and no transport status, so the same refusal maps at any transport. Generic refusals derive from the record: a permission grant derives the forbidden result and input rules derive the invalid-input result. Refusals stay result codes on the record, with no published refusal type; a conflict refusal ends a lost race, and the caller then rereads the resource and retries with current state. Transient failures belong to the integrations extension.

Each query result carries one member per include group from its record output, with declared fields, freshness, and visibility. Freshness is immediate, snapshot, or eventual with a within bound, and an asynchronous query carries a lag scenario. A group with visibleWhen shows its members when its condition holds, a group wider than its reads names the approved requirement in widenedBy, and a group with a re-decided figure names the deciding command in decidedBy.

A result carrying a Domain closed set repeats the Domain shape case by case, so the data of each case reaches the caller. The Claimwick refund outcome result carries one case per Domain case, pending, succeeded, failed, and reversed, with cases kept separate rather than flattened to one enum with nullable fields, and no Domain union type appears in Application. A narrowed set ships with a recorded decision and an updated specification. Each result carries the values the caller needs and nothing else: no aggregate, session, provider response, or transport result crosses the Application boundary.

### Where Outside Work Lives

Each external port is a public interface with a noun or verb name, one method or one purpose, and project-owned types. Provider names and transport models stay in Infrastructure, so a provider change stops at the adapter. In Claimwick, Spending declares IReceiptPdf in Ports/ReceiptPdf and implements it in Infrastructure/ReceiptPdf, because writing the receipt file reaches outside the process.

Each calls effect runs in an event handler or job after the commit through the port operation its record names, counted through a marked port. A provider answer the command needs before answering stays a waiver: the use case records its intent first, for example a pending charge, and an event handler calls the provider after the commit. Each sends effect runs in an event handler or job through the messenger port after the commit.

The notification record owns the template, the audience, the channel, the consent rule, the frequency, the dedup key, and the claims each message keeps. A reaction or job sending a single notification lives in the Application/Notifications folder named for it, and a second send of a once notification with the same dedup key sends nothing. SuppressedWhen silences a send whose condition holds.

### How Reactions and Jobs Carry Background Work

One event handler class answers each reaction record, carrying one mark naming that record. The class answers the trigger in triggeredBy, whether a Domain event of its own module, a module event, or an inbound integration event, and dispatches through the record effects entries with at most one command. The class writes no aggregate directly: work needing a write runs a command, and the class honors the record reads, idempotency, and onFailure, with a conflict retrying the reaction and a final refusal ending it.

Each scheduled or one-off job runs its declared commands once per item in its record selection, honoring batch size, lease, and cursor. The job records its evidence where the record states and follows its onFailure when an item fails. The Claimwick erase-expired-data job selects decided expenses past retention in batches of 200 under its lease, runs one erase command per expense, and resumes from its cursor, with after-commit work using the outbox and each job running on one execution host. Each job class sits with the use case it runs.

Work over many records runs as a job that selects the records and sends one command per record, each in its own unit of work. One command looping over many records rolls back the batch on one failing record and holds locks for its whole length, so a job keeps each record in its own unit of work, with the batch condition moved into the erase act so a changed record refuses itself. Reactions and the job running a use case sit in its Application folder, or with the notification or external system they serve when they run none, and each carries the mark naming its reaction.

## Standards

### Organize Application by Use Case

**ID:** `litespec/core/backend-application#organize-application-by-use-case`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each command or query MUST own one folder named for its use case inside the plural aggregate folder of its aggregate in the `Application/` folder of its module, or directly in the `Application/` folder when its record is a module-level read, holding its message, result, validator, handler, mapping, and definition of pipeline-read values.

**Rationale:** Scattered behavior turns one change into edits across many folders, and review misses one of them. One folder per use case keeps the message, the checks, the handling, and the mapping together, so a reviewer judges the whole behavior from that folder.

**Example:** In Claimwick, `use-case/spending.approve-expense` keeps its command, result, validator, and handler in `Application/Expenses/ApproveExpense/`, with its endpoint in `WebApi/Expenses/ApproveExpense/`.

**Reviewed for:** Does every command and query own one folder named for its use case inside the plural aggregate folder of its aggregate in the `Application/` folder of its module, or directly in the `Application/` folder when its record is a module-level read, with its message, result, validator, handler, mapping, and pipeline-read values? Look at the folders under each module.

### Keep Use Case Folders Small

**ID:** `litespec/core/backend-application#keep-use-case-folders-small`

**Strength:** advisory

**Applies to:** artifact: use-case

**Statement:** A use case folder SHOULD hold its message, handler, validator, definitions, and result with at most a few helpers, and no file over 200 lines.

**Deviation:** Only when a helper serves only the use case and splitting it would scatter one act across folders.

**Rationale:** A small folder reads as one act: a reader opening it sees the whole act at once. Many helpers or a long file point to work that belongs behind a port, such as file rendering, parsing, or production of a file format.

**Reviewed for:** Does any use case folder hold more than its message, handler, validator, definitions, result, and a few helpers, or a file over 200 lines? Look at the file counts and sizes in each use case folder.

### Separate Input Validation from Invariants

**ID:** `litespec/core/backend-application#separate-input-validation-from-invariants`

**Strength:** required

**Applies to:** artifact: validator

**Statement:** A validator MUST limit its checks to structural input, leaving aggregate state decisions to Domain.

**Rationale:** Shape problems fail fast, before Domain loads anything, while state refusals need the loaded aggregate. A validator that checks state pulls persistence into validation and judges one input in two layers.

**Example:** The Claimwick submit-expense validator rejects an empty expense identifier or an empty employee identifier and accepts every well-formed pair. Domain decides the receipt rule and rejects a receipt-less expense above the threshold with `result/submit-expense.receipt-missing`, carrying the stable code `spending.receipt-missing`.

**Reviewed for:** Does every validator check structural input only, leaving aggregate state to Domain? Look at each validator body.

### Enforce Target Authorization in the Pipeline

**ID:** `litespec/core/backend-application#enforce-target-authorization-in-the-pipeline`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The authorization guard, not the handler, MUST authorize every message from the `access` its use-case record declares.

**Rationale:** Per-handler authorization drifts, while one guard decides from one record for every message. The guard reads the presented kinds, the grant, the scope, and the tenant reach from the record, resolves ownership and delegation against the target through a scope owner lookup, and evaluates one composable policy per cited condition in order. Coarse authenticated, role, or scope checks stay at the endpoint and replace no decision that depends on business data. This follows decision/0108-composable-access-policies, which holds each shared condition in one factory the guard evaluates.

**Example:** The Claimwick approve-expense message carries its `access` with the spending permission, and the guard authorizes the caller before the handler loads the expense. The handler compares no caller with any target.

**Reviewed for:** Does every message reach the guard with the `access` its record declares, with no handler throwing a forbidden refusal or taking the authorization guard? Look at each handler body and the `access` on each message.

### Declare Shared Access Conditions as Composable Policies

**ID:** `litespec/core/backend-application#declare-shared-access-conditions-as-composable-policies`

**Strength:** required

**Applies to:** artifact: use-case

**Statement:** An access condition shared by several use cases MUST be one composable authorization policy declared in each use case's authorization definition and evaluated by the one authorization guard.

**Rationale:** A security rule copied per use case drifts: one copy checks before the permission and one after, and a new sensitive use case silently gets none. One policy factory beside the permission policy holds the condition, each citing use case declares it in its authorization definition, and the one guard evaluates it for all of them. This follows decision/0108-composable-access-policies, which holds shared conditions in one factory instead of copies.

**Example:** Sensitive Claimwick spending use cases such as reimburse-expense declare `RecentSignIn(permission)` in their authorization definitions. The guard checks the sign-in presence, then its age against the window from the identity contract, then the permission.

**Reviewed for:** Does every use case whose record cites a rule governing an access condition declare the matching policy, with no use case guard of its own for it? Look at each authorization definition beside the guard implementations.

### Keep Command Handlers to Load, Call and Stage

**ID:** `litespec/core/backend-application#keep-command-handlers-to-load-call-and-stage`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** A command handler MUST load or create the aggregate its use case changes, call one domain method on it, stage it through its repository, and return the result, and send no command or event.

**Rationale:** Validation, authorization, idempotency, audit, the timestamp, holds, the commit, and fault classification each have an owner in the pipeline. A handler doing more widens the unit of work, so one failing step rolls back work with no reason to fail with it. A handler sending a command nests one use case inside another, with the inner commit surviving an outer failure, and a handler publishing an event announces state its act left undescribed.

**Example:** In Claimwick, the approve-expense command flows from the endpoint through validation and the guard to a handler loading the expense, calling Approve, staging it, and returning. The pipeline commits once after the handler.

![An HTTP request reaches the endpoint, which maps it to a command and dispatches it through the command mediator. Validation checks structural input, then the authorization guard checks the permission and the target. The handler loads or creates one aggregate through its repository, calls one domain method, and stages it. The repository stages the aggregate, and exactly one commit post-handler commits the session once.](../assets/diagrams/backend-command-pipeline.svg)

**Checked by:** `dotnet build` reports `LS2021` for a command or event mediator a handler, validator, step or domain type holds, and `LS2022` for an event published outside the platform.

### Write Only Aggregates the Record Names

**ID:** `litespec/core/backend-application#write-only-aggregates-the-record-names`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** A command handler MUST write only the aggregates its record's `writes` names, through one domain repository per aggregate.

**Rationale:** A write outside `writes` hides behavior the record left undescribed, so the stack reader fails it as `write-undeclared`. One repository per named aggregate keeps the handler inside its declared boundary, with the atomic multi-aggregate case named with its invariant by a decision record.

**Example:** The Claimwick approve-expense handler takes the expense repository and no other. Staging a second aggregate fails the check without a record declaring `consistency: atomic` over both with its decision.

**Checked by:** `litespec model check` reports `write-undeclared` (level S) for a handler that takes a repository whose aggregate sits outside the aggregates its record names in `writes` and `reads`; one domain repository serves each named aggregate.

### Implement One Outcome Path per Command

**ID:** `litespec/core/backend-application#implement-one-outcome-path-per-command`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** Each command MUST implement one use case with one success path, so a flow that finds a record or else creates it is two use cases that the boundary owning the flow chooses between.

**Rationale:** A get-or-create command hides two use cases with different access, audit, and refusals behind one name. The boundary owning the flow runs a query first and sends the command fitting the answer, with one shared helper holding the sequence.

**Example:** Signing in to the library with an unknown reader card runs as a query and then one of two commands: the endpoint queries `query/loans.find-reader-by-card`, then sends either `use-case/loans.register-reader` or `use-case/loans.record-reader-sign-in`.

**Reviewed for:** Does any command handler look up a record and then create one in the same method? Look at each command handler body for a lookup followed by a create.

### Return Only Declared Results and Refusals

**ID:** `litespec/core/backend-application#return-only-declared-results-and-refusals`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler

**Statement:** A handler MUST return only the results, refusals, and codes its use-case record declares.

**Rationale:** An outcome no result declares hides behavior the specification left undescribed, so the stack reader fails it as `result-undeclared`. Refusals return in the precedence order the record lists with first match winning, and a success carrying codes returns those codes alone.

**Example:** A Claimwick handler returning a code its record lists nowhere fails the check, even with correct spelling. Every result, refusal, and code on the wire traces to the use-case record.

**Checked by:** `litespec model check` reports `result-undeclared` (level S) for a code a handler produces that no declared result names; a handler returns only the results and codes its record lists.

### Project Queries Directly

**ID:** `litespec/core/backend-application#project-queries-directly`

**Strength:** required

**Applies to:** artifact: query-handler

**Statement:** A query handler MUST project directly to its result from its own module's store and read models, without loading an aggregate.

**Rationale:** A read path enforces no invariant, so loading an aggregate for presentation pays for enforcement no read needs and ties the read shape to the write model. Projecting directly keeps reads on the read boundary, with the command and query split behind it.

**Example:** The Claimwick list-expenses query projects its rows from the store and read models and takes no domain repository. Its shape follows the result record, not the aggregate.

**Reviewed for:** Does any query handler take a domain repository, load an aggregate, or send another module's public query without a declared `queries` edge? Look at each query handler's constructor and body.

### Keep Query Handlers Free of Writes

**ID:** `litespec/core/backend-application#keep-query-handlers-free-of-writes`

**Strength:** required

**Applies to:** artifact: query-handler

**Statement:** A query handler MUST NOT stage, store, delete or dispatch anything.

**Rationale:** A read writing state makes retries, caching, and read authorization unsafe. Audited reads belong to the pipeline, so the handler stays a pure projection.

**Example:** The Claimwick list-expenses query projects its rows and stages nothing. A retry of the query repeats the same read with no new state.

**Reviewed for:** Does any query handler stage, store, delete, or dispatch? Look at each query handler body.

### Declare Every Port in the Module Ports Folder

**ID:** `litespec/core/backend-application#declare-every-port-in-the-module-ports-folder`

**Strength:** required

**Applies to:** artifact: port

**Statement:** Every port of a module MUST live in its own `Ports/{Port}/` folder holding the interface `I{Port}` with the types its signatures name in `Models/`, with its adapter in its `Infrastructure/{Port}/` folder holding its wire types in `Models/`, spelling for an external system the record name in PascalCase.

**Rationale:** One folder per port shows every outside dependency in one listing, instead of scattered interfaces across use case folders. The folder name spells the external-system record name in PascalCase, and each port pairs with its adapter in Infrastructure.

**Example:** Claimwick Spending declares `IReceiptPdf` in `Ports/ReceiptPdf/` with its adapter in `Infrastructure/ReceiptPdf/`.

**Checked by:** `litespec model check` reports `code-placement-drift` for a port folder that does not spell its external-system record name, and for a port or adapter folder naming a declared module.

### Declare a Port Only for What the Module Does Not Compute

**ID:** `litespec/core/backend-application#declare-a-port-only-for-what-the-module-does-not-compute`

**Strength:** required

**Applies to:** artifact: port

**Statement:** A module MUST declare a port in its `Ports/` folder only as an interface Infrastructure implements for work the module does not compute in memory: an external system, a file format it reads or writes, a platform service, or the clock.

**Rationale:** A `Ports/` folder accepting in-process helpers turns into the place for whatever fits nowhere else, and a reader stops telling what the module depends on. Each port earns its folder by reaching outside the process: an external system, a file format, a platform service, or the clock.

**Example:** In Claimwick, the receipt PDF renderer is a port: Spending declares `IReceiptPdf` in `Ports/ReceiptPdf/` and implements it in `Infrastructure/ReceiptPdf/`. The mapping from an `Expense` to an export row runs in memory behind one implementation, so it stays in the use case folder.

**Reviewed for:** Does every interface in the module `Ports/` folders reach outside the process, with no in-process logic, store read, duplicate platform interface, permission constant, domain exception, or enum among them? Look at each port beside its callers and its Infrastructure implementation.

### Read Another Module's State Through a Read Model

**ID:** `litespec/core/backend-application#read-another-modules-state-through-a-read-model`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler, reaction

**Statement:** A use case that needs another module's data reshaped and kept locally MUST read it through a read model its own module keeps, named in its `reads`.

**Rationale:** A read model is the reading module's own copy, with each field traced to the published field it comes from and projections keeping it current from the owner events. Reads through the model lag by up to the declared freshness, which fits rows the consumer shows and figures it accumulates.

**Example:** The Claimwick reporting list-reimbursed-expenses query reads `read-model/reporting.reimbursed-expense`, filled from the spending expense-reimbursed module event. It calls no spending reader.

**Reviewed for:** Does every read model hold data its module reshaped and keeps locally, and does every read model have a use case naming it in `reads`? Look at each read model beside the use cases that read it.

### Ask Another Module One Question Through a Decision Port

**ID:** `litespec/core/backend-application#ask-another-module-one-question-through-a-decision-port`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler; when: cross-module

**Statement:** A handler whose decision needs another module's current value MUST ask for it through the owning module's public query, declaring the edge in `effects.queries`.

**Rationale:** Some decisions reject a lagging copy: money still moving, or capacity still selling. The provider public query answers with the owner current value and nothing else, so the question stays narrow. The edge appears on both sides: the consumer `queries` edge names the public use case, and the provider record lists the consumer in `reachedThrough`.

**Example:** A Claimwick reimbursement command asks spending for the current expense through spending public read-expense query, instead of reading a whole event profile through a general reader.

**Checked by:** `litespec model check` reports `effect-undeclared` (level S) for another module's public query sent with no declared `queries` edge, `effect-unimplemented` (level S) for a declared `queries` edge no send takes, and `query-target-invalid` (level G) for a `queries` edge naming the calling module's own query.

### Send Cross-Module Queries and Commands Through the Mediator

**ID:** `litespec/core/backend-application#send-cross-module-queries-and-commands-through-the-mediator`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler, reaction, job; when: cross-module

**Statement:** A use case, reaction, or job MUST reach another module's public query or public command only through the mediator with a declared edge: a handler, reaction, or job sends another module's public query with a `queries` edge, and only a reaction or job sends another module's public command with a `commands` edge.

**Rationale:** Provider result types appear in Application code, while Domain takes plain values and identifiers, so no provider shape leaks into invariants. Handlers read their own module through the repository or the read session, endpoints send their own use case command alone, and each cross-module send travels through the mediator with its declared edge.

**Example:** A Claimwick reimbursement command sends spending public read-expense query through the query mediator with a declared `queries` edge, and a reporting reaction sends spending public command through the command mediator with a declared `commands` edge.

**Checked by:** `litespec model check` reports `effect-undeclared` (level S) for another module's public query or public command sent with no declared edge, and `effect-unimplemented` (level S) for a declared `queries` or `commands` edge no send takes, and `dotnet build` reports `LS2029` for another module's public query or query result named outside a handler, reaction, or job, and for another module's public command or command result named outside a reaction or job.

### Declare Every Commands Edge on the Sender

**ID:** `litespec/core/backend-application#declare-every-commands-edge-on-the-sender`

**Strength:** required

**Applies to:** artifact: reaction, job; when: cross-module

**Statement:** A reaction or job that sends another module's public command MUST declare the edge as `effects.commands` naming the owner's public command use case.

**Rationale:** A send no record declares hides a cross-boundary pair no diff shows. The `effects.commands` entry names the owner public command beside the behavior sending it, with the owner record listing the sender in `reachedThrough`.

**Example:** Claimwick reporting close-period job declares `effects.commands` naming `use-case/spending.close-expenses`, and the spending close-expenses command lists `module/reporting` in `reachedThrough`.

**Checked by:** `litespec model check` reports `effect-undeclared` (level S) for another module's public command sent with no declared `commands` edge, `effect-unimplemented` (level S) for a declared `commands` edge no send takes, `command-edge-invalid` (level G) for a `commands` edge a use case declares, and `command-target-invalid` (level G) for a `commands` edge naming the sending module's own command.

### Send Public Commands in Process

**ID:** `litespec/core/backend-application#send-public-commands-in-process`

**Strength:** required

**Applies to:** artifact: reaction, job; when: cross-module

**Statement:** A reaction or job MUST send another module's public command in process and synchronously through the command entry point.

**Rationale:** The owner runs its ordinary handler through its own pipeline, with authorization, audit, idempotency, and validation intact. Each command keeps its own commit, since no handler sends a command.

**Example:** Claimwick reporting close-period reaction sends spending close-expenses command through the command entry point, and the spending handler runs the change through spending own pipeline.

**Checked by:** `dotnet build` reports `LS2029` for another module's public command or command result named outside a reaction or job.

### Confirm Downstream Completion Through an Upstream Command

**ID:** `litespec/core/backend-application#confirm-downstream-completion-through-an-upstream-command`

**Strength:** required

**Applies to:** artifact: command-handler, reaction; when: cross-module

**Statement:** A module that needs to know downstream work finished MUST learn it through its own public command that the downstream module calls, never by reacting to a downstream fact.

**Rationale:** An upstream module reacting to a downstream fact depends on the downstream module, breaking the one direction each module relationship keeps. Facts flow downstream alone, while completion confirmations travel upstream as commands through the upstream own pipeline.

**Example:** When a Claimwick erasure spanning modules finishes its spending part, spending calls the owning module public confirm command naming the spending module, instead of the owner reacting to a spending fact.

**Reviewed for:** Does any module react to a downstream fact instead of learning completion through its own public command? Look at each reaction to another module's fact beside the upstream public commands.

### Stage Declared Module and Integration Events in the Commit

**ID:** `litespec/core/backend-application#stage-declared-module-and-integration-events-in-the-commit`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Each module event and each outbound integration event MUST leave the commit only through the one pipeline step that stages raised domain events, built from the domain event its record `translates`, so no user code publishes an event.

**Rationale:** User code publishing events produces two payloads for one fact and emits state the act left unannounced. One global step before the commit stages each raised Domain event for its own module reactions, each module event translating it, and each outbound integration event translating it, all committing with the business work.

**Example:** The Claimwick expense raises its reimbursed fact, and the staging step stages it, `module-event/spending.expense-reimbursed`, and the outbound `integration-event/expense-reimbursed`, each built from that fact, in the same commit. The reimburse handler publishes nothing.

**Checked by:** `dotnet build` reports `LS2022` for an event published outside the platform, and `litespec model check` reports `effect-undeclared` for an event a handler, event handler or job publishes itself.

### Issue Per-Record Work from a Job

**ID:** `litespec/core/backend-application#issue-per-record-work-from-a-job`

**Strength:** required

**Applies to:** artifact: job

**Statement:** Work over many records MUST run as a job that selects the records and sends one command per record, each in its own unit of work, never as a command handler that changes records or sends commands in a loop.

**Rationale:** One command looping over many records rolls back the batch on one failing record and holds locks for its whole length. A job sending one command per record keeps each record in its own unit of work, with the batch condition moved into the erase act so a changed record refuses itself.

**Example:** The Claimwick erase-expired-data job selects the expired expenses and sends one erase command per expense, each in its own unit of work, instead of a batch command looping over expenses.

**Checked by:** `dotnet build` reports `LS2015` for a command handler that changes records inside a loop, except when its use case record states `consistency: per-record`.

### Dispatch Only Declared Effect Edges

**ID:** `litespec/core/backend-application#dispatch-only-declared-effect-edges`

**Strength:** required

**Applies to:** artifact: command-handler, job, reaction

**Statement:** A command handler MUST change only its aggregate, leaving every declared `runs`, `sends` and `calls` edge to an event handler or job after the commit.

**Rationale:** An undeclared edge hides behavior review leaves unseen. Each declared entry names one verb with its timing and failure handling, and the pipeline refuses timing outside its bounds: `sends` and `calls` on a command run `after-commit` or `async`, with the hosted sign-in token exchange as the one `inline` `calls` edge.

**Example:** The Claimwick send-approver-reminders reaction sends the approval reminder through the messenger port after the submit commit. A send its record leaves undeclared fails the check.

**Checked by:** `litespec model check` reports `effect-undeclared` (level S) for a dispatch a handler, event handler, or job takes that its record declares no edge for, `effect-unimplemented` (level S) for a declared edge no dispatch takes, and `effect-timing-invalid` (level S) for a command use case declaring a `sends` or `calls` effect with `inline` timing, and `dotnet build` reports `LS2021` for a command or event mediator held outside an endpoint, event handler or job.

### Declare the Audit Target and Reason Beside the Message

**ID:** `litespec/core/backend-application#declare-the-audit-target-and-reason-beside-the-message`

**Strength:** required

**Applies to:** artifact: use-case; when: command

**Statement:** Each audited command MUST declare beside the message the audit target and reason that the message carries, leaving the handler to add only the values it learns while running.

**Rationale:** Values from the message exist before the handler runs, so the pipeline records them on every outcome, including a denial. The handler adds a generated identity or a reason composed from loaded state alone.

**Example:** The Claimwick approve-expense command declares its expense id as the audit target with the reason the message carries, and the handler adds the approval reference it learns while running.

**Reviewed for:** Does any command handler add an audit target or reason that the message already carries? Look at each audit call in the command handlers.

### Read Time from the Scoped Clock

**ID:** `litespec/core/backend-application#read-time-from-the-scoped-clock`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler

**Statement:** Each handler MUST read the message timestamp from the scoped clock the pipeline stamps once per message.

**Rationale:** A handler reading the clock two or three times in one decision records different times for one act, sometimes with an outside call in between. One timestamp per message fixes the moment the message starts, and every decision in the handling answers to the same time.

**Example:** The Claimwick approve handler stamps its approval with the handled clock, instead of a fresh clock read.

**Reviewed for:** Which handlers read the system clock directly, or take more than one timestamp for one message? Look at the clock reads in each handler.

### Refuse a Lost Race as a Conflict

**ID:** `litespec/core/backend-application#refuse-a-lost-race-as-a-conflict`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** Each command that loses a race at commit MUST end with its use case's conflict refusal.

**Rationale:** A duplicate key or a stale version reaches the commit as a store fault, and the pipeline classifies it as the use case named conflict refusal. The caller learns what happened and rereads and retries with current state, with no reservation or retry inside the handler.

**Example:** Two library clerks register the same book barcode at once. The second commit violates the barcode unique index, and the command refuses with `book-barcode-taken`.

**Reviewed for:** Does every command that loses a race at commit end with its use case conflict refusal? Look at the race outcomes beside the use case refusals.

### Validate Query Input

**ID:** `litespec/core/backend-application#validate-query-input`

**Strength:** required

**Applies to:** artifact: use-case; when: query

**Statement:** Each query carrying paging, filters, or ranges MUST declare a query validator run by the pipeline.

**Rationale:** An unvalidated page size or range reaches the store unchecked, with each handler clamping its own input or none doing it. One query validator per query answers to the same input rule as commands.

**Example:** The Claimwick list-expenses query validates its page size through its query validator.

**Reviewed for:** Does every query with paging, filters, or ranges declare a query validator? Look at each such query beside the pipeline validators.

### Build Collaborators Through the Container

**ID:** `litespec/core/backend-application#build-collaborators-through-the-container`

**Strength:** required

**Applies to:** artifact: command-handler, job, query-handler, reaction

**Statement:** Each handler needing a collaborator MUST take it through the constructor from the container, never create it inside its own body.

**Rationale:** A step created inside a handler hides its dependencies behind a small-looking handler. Injected collaborators keep their dependencies visible in the constructor.

**Example:** The Claimwick reimburse handler takes its expense repository through its constructor, while a step created inside the handler taking a repository is reported.

**Reviewed for:** Does any handler, job, or step class create collaborators inside its body, or take more than ten constructor dependencies? Look at each constructor the change adds.

### Keep Repository Reads Out of Loops

**ID:** `litespec/core/backend-application#keep-repository-reads-out-of-loops`

**Strength:** required

**Applies to:** artifact: command-handler, job, query-handler

**Statement:** Each handler and job MUST read no repository inside a loop body.

**Rationale:** A read inside a loop turns one handling into as many round trips as there are items, with the slowdown hidden behind small test data. A batch read or a scoped query loads every item at once, costing one trip whatever the data holds.

**Example:** The library reminder job loads due loans with one scoped query instead of reading each loan inside its loop.

**Checked by:** `litespec model check` reports `repository-read-invalid` (level S) for a repository read inside a loop body.

## Conventions

### Use This Use-Case Layout

**ID:** `litespec/core/backend-application#use-this-use-case-layout`

**Strength:** default

**Applies to:** artifact: any

**Default:** Place each use case in one folder per layer that serves it, inside the plural aggregate folder of its aggregate, with a module-level read dropping the aggregate segment in each: `Application/{Aggregates}/{UseCase}/` with its message, result, handler, validator, pipeline definitions, own steps and triggers, and `WebApi/{Aggregates}/{UseCase}/` with its endpoint and transport models when it has an endpoint; place every port of the module in its own `Ports/{Port}/` folder with its adapter in `Infrastructure/{Port}/`.

**Replacement:** A project names its own Application folder layout in its documentation.

**Rationale:** One folder per use case inside the plural aggregate folder in each serving layer holds everything a reviewer needs in one set of folders. The module tree moves a reader from a handler to its endpoint without a map, and the `Ports/` folder lists every outside dependency with its adapter. Reactions and the job running the use case sit in its `Application/` folder, so the folder shows every way the use case starts.

**Example:** The layout reads as follows:

```text
{Product}/
  Application/                            product layer folder: pipeline positions and pipeline stages, and nothing else
  Posts/                                  one aggregate: every layer holds the plural aggregate folder
    Contracts/
      Posts/          PostId, OnPostPublished/PostPublishedModuleEvent, ReadPost/ (query and result)
    Domain/
      Posts/                              Post, PostState, and the post repository contract
    Application/
      Posts/
        CreateDraft/                      Command, Result, Handler, Validator, mapping
        GetPost/
        ListPosts/
          ListPostsQueryResultItem        a result part named after its result
        ArchiveStalePost/                 no WebApi folder: no endpoint reaches this use case
          ArchiveStalePostsJob            the job that runs this use case once per stale post
          ArchiveStalePostOnPostReportedHandler   a reaction that runs this use case
        ExportPosts/                      query, handler, definitions, result
      Notifications/
        NewPost/
          NotifySubscribersOnPostPublishedHandler   a reaction that only sends the new-post notification
    Ports/
      PostsArchive/                       IPostsArchive with its signature types in Models/
    Infrastructure/
      Persistence/                        module-wide store setup, with one folder per aggregate
        Posts/                            PostRepository with its stored documents and mapping
      PostsArchive/                       PostsArchiveWriter, one writer per section
    WebApi/
      Posts/
        CreateDraft/                      Endpoint, RequestModel, ResponseModel
        GetPost/
        ListPosts/
        ExportPosts/                      endpoint
  Customers/                              two aggregates: Domain holds one plural folder per aggregate
    Contracts/
      CustomerAccounts/  CustomerAccountId, one On{Fact}/ folder per fact, and the public queries and public commands of the account aggregate
      Consents/          ConsentId, one On{Fact}/ folder per fact, and the public queries and public commands of the consent aggregate
    Domain/
      CustomerAccounts/
      Consents/
    Application/
      CustomerAccounts/
        RestrictAccount/
      Consents/
        GrantConsent/
      ReadModels/
        OrderHistories/                   a read model of another module's published facts
    WebApi/
      CustomerAccounts/
        RestrictAccount/
      Consents/
        GrantConsent/
```

Module, aggregate, and use case order follows `litespec/core/backend-architecture#organize-code-by-module-aggregate-and-use-case`. A use case own models stay in its folder, and any other type sits in the layer folder owning it, as `litespec/core/backend-architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers` states. An event handler or job lives with what it runs and carries the mark naming its reaction.

**Reviewed for:** Does every use-case path, layer folder, trigger folder, and result part name follow the layout? Look at each path under the modules.
