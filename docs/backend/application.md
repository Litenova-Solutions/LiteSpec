# Application Layer

## Intent

The Application layer coordinates use cases, checking the shape of each incoming message, authorizing it, running the handler, and returning a result that carries no transport details. A use case is one command, which changes business state, or one query, which reads without changing state; an automation runs work without a request, on an event, on a schedule, or by hand. This page states how use cases and automations are organized, what each stage of the command pipeline owns, and how ports and declared effects are wired.

## Concepts

### The Use-Case Folder

Every folder in Application is a module, an aggregate, a use case, or an event handler folder. One use-case folder holds everything a reviewer needs to judge a use case: its message, result, validator, handler, mapping, and definition of pipeline-read values. An event handler folder holds its event handler classes with the marks naming their automations, named under `litespec/workspace/naming#group-event-handlers-by-the-fact-they-handle`. Contracts that more than one module names live in the shared application contracts, and the pipeline machinery lives in one folder named for the pipeline.

### The Command Pipeline

A command travels fixed stages in order: validation, authorization, idempotency, audit, timestamp, holds, the handler, commit, and fault classification, with exactly one commit post-handler. The handler loads or creates the one aggregate its use case changes, calls one domain method on it, stages it through its domain repository, and returns a result. Validation, authorization, idempotency, audit, the timestamp, holds, the commit, and fault classification each belong to their own pipeline stage, so none appears in the handler. A query travels validation and the guard, then projects its result directly without loading an aggregate. An event handler answers its trigger outside the command pipeline, running the declared `runs`, `sends`, and `calls` effects after the commit, while an `emits` effect leaves the commit as a domain event staged in the outbox. A scheduled or manual automation runs its commands once per item in its selection.

### Ports

A port is a narrow interface Application declares for work it does not do itself, such as archiving posts or reading the clock. Each port names one business capability with project-owned types, and Infrastructure implements it. A port is declared beside the aggregate its consumers share, so it is reviewed with that aggregate's specification. An external-system port carries the operations one `calls` effect names, a messenger port carries the notifications `sends` effects name, and a store port carries state the owning system holds.

### Declared Effects

Every edge between use cases, automations, notifications, integration events, and external systems is declared on a record and checked in code. A use case declares its edges in `effects`: `runs` names a command, `sends` names a notification, `emits` names an integration event, and `calls` names an external-system operation. Each entry carries its timing: `inline` shares the caller's call, `after-commit` runs after the commit succeeds, and `async` runs outside the caller's transaction. An edge the records do not declare is the defect the effect checks report.

## Standards

### Organize Application by Use Case

**ID:** `litespec/backend/application#organize-application-by-use-case`

**Statement:** Each command or query MUST own one use-case folder under its module holding its message, result, validator, handler, mapping, and definition of pipeline-read values.

**Rationale:** One use-case folder holds everything a reviewer needs to judge a use case: its message, result, validator, handler, mapping, and pipeline-read values. Grouping by technical type scatters one use case across the project, so a change touches six folders and review misses one. Type names share the use-case prefix and follow the suffix table under `litespec/workspace/naming#use-architectural-suffixes`. Pipeline-read values are declared beside the message under `litespec/backend/application#declare-every-pipeline-position-beside-the-message`.

**Reviewed for:** Each command or query owns one use-case folder under its module holding its message, result, validator, handler, mapping, and pipeline-read values.

### Dispatch Commands and Queries Through Separate Entry Points

**ID:** `litespec/backend/application#dispatch-commands-and-queries-through-separate-entry-points`

**Statement:** An application MUST dispatch a command and a query through two separate Application-facing entry points rather than one message bus abstraction.

**Rationale:** Two entry points keep the write and read paths distinguishable at every call site and let the pipeline compose different stages for each. A unified bus hides that difference behind one method, so a reader sees no difference between a dispatch that changes state and one that reads. This backend's whole command and query split depends on that difference.

The rule states the property rather than the package. Each stack binding pins one library that supplies it. A consumer replacing that library records an exception against the binding convention rather than touching this rule.

**Reviewed for:** Each dispatch site calls the command entry point or the query entry point, with no unified bus.

### Separate Input Validation from Invariants

**ID:** `litespec/backend/application#separate-input-validation-from-invariants`

**Statement:** A validator MUST limit its checks to structural input, leaving aggregate state decisions to Domain.

**Rationale:** Structural input and aggregate state fail for different reasons and at different times: malformed input is rejected before Domain runs, while a state refusal needs the loaded aggregate. A validator that checked state would need the aggregate, which pulls persistence into validation. A validator rejects an empty title, a malformed identifier, or an invalid page size, and Domain decides whether the expense above the threshold carries a receipt. Every validator for a message reports validity findings under `litespec/backend/application#report-input-problems-as-validity-findings`, so the caller learns the whole form in one answer.

**Example:** In the Claimwick product, `use-case/spending.submit-expense` takes a command carrying an expense identifier and an employee identifier with the amount, currency, and receipt. The validator rejects an empty expense identifier or an empty employee identifier, and accepts every well-formed pair.

Whether the expense above the threshold carries a receipt is an aggregate state decision, so the validator leaves it to Domain. Domain rejects an expense above the threshold that carries no receipt with `result/submit-expense.receipt-missing`, which carries the stable code `spending.receipt-missing`.

**Reviewed for:** Each validator checks structural input only, leaving aggregate state decisions to Domain.

### Report Input Problems as Validity Findings

**ID:** `litespec/backend/application#report-input-problems-as-validity-findings`

**Statement:** A validator MUST report its findings as validity findings, with each finding carrying a member, a code, and a message, and no transport status.

**Rationale:** Running every validator and merging findings tells the caller everything wrong with the form in one answer; stopping at the first finding turns one form into repeated round trips. Each finding carries a member naming the input member, a code naming the stable reason, and a message explaining the problem in caller-safe language, with no transport status because Application answers no transport.

**Example:** A submit request with an empty expense identifier and an empty employee identifier reports two findings, one per member, with no transport status:

```json
[
  { "member": "ExpenseId", "code": "required", "message": "An expense is required." },
  { "member": "EmployeeId", "code": "required", "message": "An employee is required." }
]
```

The caller learns the whole form in one answer, and the pipeline refuses the request before Domain runs.

**Reviewed for:** Each validator reports validity findings with member, code, and message per finding, and no transport status.

### Model Expected Use-Case Refusals Explicitly

**ID:** `litespec/backend/application#model-expected-use-case-refusals-explicitly`

**Statement:** An expected use-case refusal MUST carry its result code and a caller-safe message, with no transport status.

**Rationale:** An expected refusal is part of the use case's contract: a missing resource, a forbidden act, or a conflicting write, carrying the refusal result's stable code and a caller-safe message. The refusal carries no transport result, provider detail, or stack detail, so the same refusal maps correctly at any transport, and handlers do not catch it.

The use case lists every result it returns, and each refusal result carries its code. Generic refusals are derived, never written: a use case whose grant is a permission derives its `forbidden` result, and a use case with rules on its input derives its `invalid-input` result. A conflict refuses the same input the same way until the caller rereads the resource and retries with current state. A refusal that a blind retry can pass is transient and belongs to the [integrations extension](../ext/integrations.md).

**Reviewed for:** Each expected refusal carries its result code and a caller-safe message, with no transport status.

### Enforce Target Authorization in the Pipeline

**ID:** `litespec/backend/application#enforce-target-authorization-in-the-pipeline`

**Statement:** The authorization guard, not the handler, MUST authorize every message from the `access` its use-case record declares.

**Rationale:** Authorization decided in each handler is decided differently in each handler. The guard decides once from what the record declares: the `auth` kinds the caller presents, the `grant` naming a permission, a `self` scope over one, `anonymous`, or `system`, and the `tenant` scope naming the tenants the call reaches. Ownership, tenant, role, state, or delegated access resolves against the target data through a scope owner lookup for the record the message names. A handler never compares the caller with a target and never throws the forbidden refusal itself. Code names no permitted action outside that grant, so the record stays the one list of who may run a use case; where the stack generates code, it generates one constant per `permission/` record from the records.

Declaring `access` beside the message also lets composition refuse to start when a message states no position at all. The guard reports a refusal as a denial rather than raising it, so a refused attempt reaches the audit trail beside the ones that succeeded. The audit extension records the denial under `litespec/ext/audit#record-refused-authorization`.

A collection query has no single target record, so a scope owner lookup has nothing to resolve. Its authorized scope is a database predicate under `litespec/quality/security#restrict-a-protected-collection-in-the-database`, applied before paging. Filtering a loaded collection in memory satisfies neither rule. It matches the shape [OWASP names as broken object level authorization](https://owasp.org/www-project-top-ten/2021/A01_2021-Broken_Access_Control/).

WebApi enforces coarse authenticated, role, or scope policies first under `litespec/backend/api#apply-only-coarse-policies-at-the-endpoint`, which replaces no decision that depends on business data.

**Checked by:** `dotnet build` reports `LS2012` for a handler that throws a forbidden refusal; the guard authorizes every message from its record's `access`.

### Keep Command Handlers to Load, Call and Stage

**ID:** `litespec/backend/application#keep-command-handlers-to-load-call-and-stage`

**Statement:** A command handler MUST load or create the aggregate its use case changes, call one domain method on it, stage it through its repository, and return the result, and do nothing else.

**Rationale:** Everything else has an owner. Validation, authorization, idempotency, audit, the timestamp, holds, and the commit are pipeline stages; decisions and invariants are the aggregate's; uniqueness is persistence's under `litespec/backend/persistence#enforce-uniqueness-in-the-store`; messages, provider calls, and changes to other aggregates are consequences that event handlers run after the commit under `litespec/backend/application#dispatch-only-declared-effect-edges`. A handler that does more widens the unit of work, so one failing step rolls back work that had no reason to fail with it, and agents copy the widened shape. The commit post-handler owns the transaction under `litespec/backend/persistence#commit-each-command-pipeline-once`, WebApi owns error mapping, the audit completion handler owns the trail, and the guard owns authorization. A use case whose record declares `consistency: atomic` over several aggregates is the one exception: the handler loads, calls, and stages each aggregate its `changes` names, one method each, and the record states why the change cannot be eventual. The handler takes its repositories under `litespec/backend/application#take-repositories-from-declared-changes-and-reads`. It returns only the results and refusals the record declares, under `litespec/backend/application#return-only-declared-results-and-refusals`.

**Example:** In the Claimwick product, `use-case/spending.approve-expense` runs through the command pipeline: the endpoint maps the request to the approve command, the command entry point dispatches it, validation and the authorization guard run, the handler loads the expense, calls Approve on it, stages the expense, and returns the result. The handler body reads as three steps: it loads the expense through its domain repository, it calls one domain method on it, and it stages the expense. The handler commits nothing, and exactly one post-handler commits after it. The diagram shows each stage from the HTTP request to that single commit.

![An HTTP request reaches the endpoint, which maps it to a command and dispatches it through the command mediator. Validation checks structural input, then the authorization guard checks the permission and the target. The handler loads or creates one aggregate through its repository, calls one domain method, and stages it. The repository stages the aggregate, and exactly one commit post-handler commits the session once.](../assets/diagrams/backend-command-pipeline.svg)

The handler loads or creates, calls once, stages, and returns. Validation, authorization, idempotency, audit, the timestamp, holds, commit, and fault classification belong to their owners, so none appears in the handler.

**Reviewed for:** Each command handler loads or creates one aggregate, calls one method, stages it, and returns, with pipeline stages owning the rest.

### Take Repositories from Declared Changes and Reads

**ID:** `litespec/backend/application#take-repositories-from-declared-changes-and-reads`

**Statement:** A command handler MUST write only the aggregates its record's `changes` names, through one domain repository per aggregate.

**Rationale:** A write outside `changes` is behavior the record hides, so the stack reader fails `write-undeclared` on it. Under `litespec/backend/application#keep-command-handlers-to-load-call-and-stage` the handler normally takes one domain repository for the one aggregate it changes and stages only that aggregate. A use case whose record declares `consistency: atomic` over several aggregates takes one repository per named aggregate and stages each one. A query handler likewise projects only over the aggregates its record's `reads` names, through the read session.

**Example:** The `approve-expense` handler takes the expense repository and no other; staging a second aggregate fails the check unless the record declares `consistency: atomic` over both.

**Reviewed for:** Each command handler takes one repository per aggregate its `changes` names, with no write outside `changes`.

### Implement One Outcome Path per Command

**ID:** `litespec/backend/application#implement-one-outcome-path-per-command`

**Statement:** Each command MUST implement one use case with one success path, so a flow that finds a record or else creates it is two use cases that the boundary owning the flow chooses between.

**Rationale:** A get-or-create command hides two use cases with different access, audit, and refusals behind one name. The boundary that owns the flow, an endpoint, an event handler, or an automation, runs a query and then sends the command that fits. When the create loses a race, it refuses with its conflict under `litespec/backend/application#refuse-a-lost-race-as-a-conflict`, and the owner sends the other command. Where the record is usually present, the owner may instead send the command first and, on the record's not-found refusal, send the create and then the command again; one shared helper holds that sequence, so no endpoint repeats it.

**Example:** Signing in to the library with an unknown reader card runs as a query and then one of two commands: the endpoint queries `query/loans.find-reader-by-card`, then sends either `use-case/loans.register-reader` or `use-case/loans.record-reader-sign-in`.

**Checked by:** `dotnet build` reports `LS2014` for a command handler that calls a `Find` repository method and then creates the found type in the same method.

### Return Only Declared Results and Refusals

**ID:** `litespec/backend/application#return-only-declared-results-and-refusals`

**Statement:** A handler MUST return only the results, refusals, and codes its use-case record declares.

**Rationale:** An observable outcome no result declares is behavior the specification hides, so the stack reader fails `result-undeclared` on it. Refusals return in the precedence order the record lists, first match wins, and a success carrying `codes` returns only those codes. Generic refusals derive from `access` and `input` and need no declaration.

**Example:** A handler that returns a code its record never lists fails the check, even when the code is spelled correctly.

**Reviewed for:** Each handler returns only declared results, refusals, and codes.

### Project Queries Directly

**ID:** `litespec/backend/application#project-queries-directly`

**Statement:** A query handler MUST project directly to its result without loading an aggregate.

**Rationale:** A read path acts on no invariant, so loading an aggregate for presentation pays for enforcement no read needs and couples the read shape to the write model. Projecting directly to the result keeps reads on the read boundary the persistence standards state. A query handler takes no domain repository. The result type carries the `include` groups the record's `output` declares, under `litespec/backend/application#shape-query-results-from-declared-output`. The command and query split lives in `litespec/backend/architecture#separate-command-and-query-behavior`.

**Checked by:** `dotnet build` reports `LS2011` for a query handler taking a repository; each query handler projects directly without loading an aggregate.

### Keep Query Handlers Free of Writes

**ID:** `litespec/backend/application#keep-query-handlers-free-of-writes`

**Statement:** A query handler MUST NOT stage, store, delete or dispatch anything.

**Rationale:** A read that writes makes retries, caching, and authorization of reads unsafe. An audited read is the pipeline's, not the handler's, under `litespec/ext/audit#declare-audit-selection-on-every-command`.

**Example:** The list-expenses query projects its rows and stages nothing.

**Reviewed for:** Each query handler stages, stores, deletes, or dispatches nothing.

### Shape Query Results from Declared Output

**ID:** `litespec/backend/application#shape-query-results-from-declared-output`

**Statement:** A query result type MUST carry the `include` groups its record's `output` declares, with their fields, freshness, and visibility.

**Rationale:** The record's `output` names each group with its fields, their classification, the aggregates it reads, and the freshness the caller can rely on: `immediate`, `snapshot`, `bounded`, or `advisory`. The result type carries one member per group, so the caller reads what the record promises and nothing more. A group with `visibleWhen` appears only when its condition holds, and a group wider than its `reads` names the approved requirement widening it in `cites`. A query with `builtAsynchronously` carries a `bounded` or `advisory` freshness with its lag scenario.

**Example:** The expense-list result carries its `rows` group with the declared fields, freshness, and visibility.

**Reviewed for:** Each query result type carries the declared `include` groups with their fields, freshness, and visibility.

### Mirror a Domain Closed Set in the Result

**ID:** `litespec/backend/application#mirror-a-domain-closed-set-in-the-result`

**Statement:** An Application result carrying a Domain closed set MUST model that set with the same shape as its own union or code value.

**Rationale:** Each case of a closed set can carry different data, and that data needs to survive to the caller. Mirroring the Domain shape in the result keeps the layers aligned; flattening case data into an enumeration with nullable fields loses which fields belong to which case. Application exposes no Domain union type. Narrowing follows `litespec/backend/application#record-a-decision-for-closed-set-narrowing`, and the set itself follows `litespec/backend/domain#model-every-closed-set-of-domain-values-as-a-closed-type-hierarchy`. The wire carries the same shape under `litespec/backend/api#mirror-a-domain-closed-set-as-a-transport-model-of-the-same-shape`.

**Example:** A refund outcome result carries one case per Domain case with its data: pending, succeeded, failed, and reversed.

**Reviewed for:** Each result carrying a Domain closed set models it with the same shape and cases.

### Record a Decision for Closed-Set Narrowing

**ID:** `litespec/backend/application#record-a-decision-for-closed-set-narrowing`

**Statement:** A layer MUST NOT narrow a Domain closed set without a recorded decision and an updated specification.

**Rationale:** Narrowing silently drops cases the Domain can produce, so a caller meeting a dropped case gets a shape the contract left undescribed. A recorded decision with an updated specification makes the dropped cases visible and owned. The decision and the specification follow `litespec/core/model#keep-one-owner-and-status-on-every-record`.

**Reviewed for:** Each narrowed closed set names its recorded decision and updated specification.

### Define Narrow External Ports

**ID:** `litespec/backend/application#define-narrow-external-ports`

**Statement:** Application MUST declare each external port as a public interface with a noun or verb name, one method or one purpose, and project-owned types.

**Rationale:** A port is the seam where a provider change stops: one method or one purpose, named as a noun for the capability (`PostArchive`) or a verb for the act (`ArchivePosts`), typed with project-owned types. If provider names or transport models entered the interface, replacing the provider would rewrite Application. Keeping them in Infrastructure confines the change to the adapter.

**Reviewed for:** Each external port is a public interface with a noun or verb name, one method or one purpose, and project-owned types.

### Declare a Port Where Its Consumers Meet

**ID:** `litespec/backend/application#declare-a-port-where-its-consumers-meet`

**Statement:** A port MUST be declared at the lowest folder holding every consumer that names it, following `litespec/backend/architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers`.

**Rationale:** A port reviewed beside the aggregate it serves is reviewed with that aggregate's specification; a port in a project-wide folder is reviewed by nobody. A port whose consumers sit in one module is declared in that module beside the aggregate it serves, and only a port that more than one module names belongs in the shared application contracts. Collecting every port in one folder because they share the word "port" is grouping by technical type. `litespec/backend/application#organize-application-by-use-case` rejects that grouping one level down for the same reason.

You review a port declared beside its aggregate with that aggregate's specification. Nobody reviews a port in a project-wide folder. Where a port serves aggregates in one module but is called from another, the declaring module is the one that owns the aggregates. That module is the one whose rules change when the port changes.

**Reviewed for:** Each port sits at the lowest folder holding every consumer that names it.

### Call External Systems Through Their Ports

**ID:** `litespec/backend/application#call-external-systems-through-their-ports`

**Statement:** Each `calls` effect MUST run in an event handler or automation through the port operation its record names, after the commit of the command that raised its trigger.

**Rationale:** A command handler changes only its aggregate under `litespec/backend/application#dispatch-only-declared-effect-edges`, so a provider call runs in an event handler or automation after the commit. The `calls` effect names one operation on one external system, and the operations entry states its timeout, its timeout behavior, its retries, its response, and its cache. The port carries that operation with project-owned types and carries a mark naming its system on the port interface, and Infrastructure implements it, so a provider change stops at the adapter. The stack reader counts a call as a `calls` edge only through such a marked port; any other injected interface is internal work. A call on a marked port that no `calls` entry declares fails `effect-undeclared`. A provider call whose result the command needs before it can answer is a departure: the use case records the intent first, for example a pending charge, and an event handler calls the provider. A project that keeps an inline call records an exception naming the compensation that undoes it.

**Example:** An event handler that calls an expense payout operation names it through the record's `calls` entry and the port method; a direct provider call fails the check.

**Reviewed for:** Each `calls` effect runs in an event handler or automation through its port, after the commit.

### Send Notifications Through the Messenger Port

**ID:** `litespec/backend/application#send-notifications-through-the-messenger-port`

**Statement:** Each `sends` effect MUST run in an event handler through the messenger port, after the commit of the command that raised its trigger.

**Rationale:** A command handler changes only its aggregate under `litespec/backend/application#dispatch-only-declared-effect-edges`, so a notification send runs in an event handler after the commit. The notification record owns the template, the addresses, the channel, the consent rule, the frequency, the dedup key, and the claims each message keeps. The `sends` edge names the notification, and the notification record carries its suppression in `suppressedWhen`. The messenger port carries the send with project-owned types. A send no edge declares fails `effect-undeclared`, and a second send of a `once` notification with the same dedup key sends nothing.

**Example:** The due-notice event handler sends the loan-due notification through the messenger port; the template and consent rule stay on the notification record.

**Reviewed for:** Each `sends` effect runs in an event handler through the messenger port, after the commit.

### Emit Declared Integration Events After Commit

**ID:** `litespec/backend/application#emit-declared-integration-events-after-commit`

**Statement:** Each `emits` effect MUST leave the commit as a domain event the aggregate raised, staged in the outbox with the business work.

**Rationale:** A command handler changes only its aggregate under `litespec/backend/application#dispatch-only-declared-effect-edges`, so an `emits` effect is a domain event the aggregate raises, which persistence stages in the outbox in the same commit. The `emits` entry names the integration event with its timing, and the outbox extension stages the message with the business work so both commit together and delivery follows. An emit no entry declares fails `effect-undeclared`. Emitting outside the outbox delivers facts the commit did not store.

**Example:** The `reimburse-expense` aggregate raises its reimbursed fact, and persistence stages it in the outbox with the business work; the record's `emits` entry names the event.

**Reviewed for:** Each `emits` effect leaves the commit as a domain event staged in the outbox.

### Expose Store State Through Ports

**ID:** `litespec/backend/application#expose-store-state-through-ports`

**Statement:** A `store` aggregate MUST reach code only through a port naming its owner, with no aggregate root.

**Rationale:** A `store` aggregate is state no invariant guards: a library-owned table, a suppression list, or a delivery log. With no invariant, there is no root, no state record, and no domain repository; the port names the owning system and Infrastructure implements it. Staff actions on that state stay ordinary use cases calling the port.

**Example:** The suppression list reaches code through a suppression port owned by the communications module, with no aggregate root.

**Reviewed for:** Each `store` aggregate reaches code through an owner-naming port, with no aggregate root.

### Read Settings Through Typed Options

**ID:** `litespec/backend/application#read-settings-through-typed-options`

**Statement:** A handler MUST read a setting record through a typed options value, or through stored tenant data where the record's `setBy` is `tenant`.

**Rationale:** A setting record owns its type, its default, its range, its unit, and what happens at the bound. Decider-set and operator-set values bind to typed options the host validates at startup, so a bad value fails before traffic arrives. Tenant-set values are data nobody reviews: they live in stored tenant state the handler reads per tenant. A raw configuration lookup bypasses the type, the range, and the bound, so no handler uses one.

**Example:** The receipt threshold binds to a spending options value; the handler reads the bound and refuses past it with `atBound: refuse`.

**Reviewed for:** Each setting reaches its handler through a typed options value or stored tenant data, with no raw lookup.

### Declare Every Pipeline Position Beside the Message

**ID:** `litespec/backend/application#declare-every-pipeline-position-beside-the-message`

**Statement:** A message MUST declare a value or an exemption for every pipeline position the baseline and the selected extensions require.

**Rationale:** A pipeline stage that guesses a message's position guesses differently per message. Declaring a value or an exemption beside the message makes each position explicit, and composition refuses to start when a message states neither. The baseline holds one position: authorization, declared from the record's `access` on the message itself. The audit extension adds the audit selection position under `litespec/ext/audit#declare-audit-selection-on-every-command`.

**Reviewed for:** Each message declares a value or an exemption for every pipeline position the baseline and selected extensions require.

### Keep Event Automation Implementations Explicit

**ID:** `litespec/backend/application#keep-event-automation-implementations-explicit`

**Statement:** An event automation implementation MUST answer the trigger its automation record names, in one class carrying one mark naming that record.

**Rationale:** An automation without an explicit implementation leaves its trigger, its target, and its failure handling to whoever reads the code. One event handler class per automation record answers the trigger the record names in `triggeredBy`, whether a domain event or an integration event, and dispatches only through the record's `effects` entries. The class never writes an aggregate directly: an event handler that needs a write runs a command. It honors the record's `reads`, its idempotency, and its `onFailure`, and the mark is the link the stack reader follows. The class lives in the `On{Fact}` folder for the fact it handles, named under `litespec/workspace/naming#group-event-handlers-by-the-fact-they-handle`. The [automation page](../core/automation.md) states the trigger fields. An entry with `after-commit` timing uses durable delivery through the outbox extension.

**Example:** The class `SendDueNoticeOnLoanBorrowedHandler` in `OnLoanBorrowed/` carries one mark naming `automation/lending.send-due-notice-on-loan-borrowed`, answers `event/loan.loan-borrowed`, and sends `notification/lending.loan-due`.

**Reviewed for:** Each event handler answers its automation trigger in one class carrying its record mark, in its `On{Fact}` folder.

### Run Each Automation Within Its Declared Selection

**ID:** `litespec/backend/application#run-each-automation-within-its-declared-selection`

**Statement:** A scheduled or manual automation MUST run its declared commands once per item in its record's `selection`, honoring its batch size, lease, and cursor.

**Rationale:** The automation record owns what each run selects, how many items each batch takes, the lease that keeps two runs apart, and the cursor where a run resumes. The automation runs its `effects` entries once per item, records its evidence where the record states, and follows its `onFailure` when an item fails. A `manual` automation carries no aggregate: it states its `runsWhere` and its guard, and runs as interactive platform work. One execution host runs the automation under `litespec/backend/architecture#declare-the-execution-host-for-work-that-outlives-a-request`.

**Example:** The erase-expired-data automation selects decided expenses past retention in batches of 200 under its lease, runs one erase command per expense, and resumes from its cursor.

**Reviewed for:** Each scheduled or manual automation runs its commands once per item in its selection, honoring batch size, lease, and cursor.

### Issue Per-Record Work from an Automation

**ID:** `litespec/backend/application#issue-per-record-work-from-an-automation`

**Statement:** Work over many records MUST run as an automation that runs one command per record, each in its own unit of work, never as a command handler that changes records in a loop.

**Rationale:** One failing record otherwise rolls back the batch, and the batch holds locks for its whole length. The automation runs each command within its declared selection under `litespec/backend/application#run-each-automation-within-its-declared-selection`, and persistence commits each record alone under `litespec/backend/persistence#commit-each-command-pipeline-once`.

**Example:** The erase-expired-data automation runs one erase command per expense, each in its own unit of work, instead of a command looping over expenses.

**Checked by:** `dotnet build` reports `LS2015` for a command handler with a repository `Store`, `Add`, `Update` or `Delete` call inside a loop body.

### Dispatch Only Declared Effect Edges

**ID:** `litespec/backend/application#dispatch-only-declared-effect-edges`

**Statement:** A command handler MUST change only its aggregate, leaving every declared `runs`, `sends`, `calls`, and `emits` edge to an event handler or automation after the commit.

**Rationale:** An undeclared edge is the defect: an event handler or automation reaches a use case, notification, integration event, or external system the records never named, so review sees behavior the specification hides. Each declared entry names exactly one verb, its timing, and its failure handling, and the stack reader fails `effect-undeclared` on any dispatch the record does not declare. An `emits` effect is a domain event the aggregate raises, staged in the outbox in the same commit under `litespec/backend/application#emit-declared-integration-events-after-commit`; `sends`, `calls`, and `runs` effects run in an event handler or automation after the commit. A `sends` or `calls` effect is `after-commit` or `async`, never `inline`. An `inline` `runs` entry states whether it shares the caller's transaction, and `consistency: atomic` never spans an `async` edge. The effect graph holds no cycles except through domain events with a stated end condition. A saga carried by a workflow engine maps each engine step to exactly one use case with its compensation through the `workflow-engine` extension.

**Example:** The `reimburse-expense` aggregate raises its reimbursed fact, and persistence stages `integration-event/expense.reimbursed` in the outbox because its record declares that `emits` entry; a second emit the record does not declare fails the check.

**Reviewed for:** A command handler changes only its aggregate, and each event handler or automation dispatch names a declared effect entry.

### Declare Audit Values Beside the Message

**ID:** `litespec/backend/application#declare-audit-values-beside-the-message`

**Statement:** Each audited command MUST declare beside the message the audit target and reason that the message carries, leaving the handler to add only the values it learns while running.

**Rationale:** A value read from the message is known before the handler runs, so the pipeline records it on every outcome, including a denial. Declaring the target and the reason beside the message evaluates both before the handler runs. The handler adds only a generated identity or a reason composed from loaded state.

**Example:** The approve-expense command declares its expense id as the audit target with the reason the message carries, and the handler adds only the approval reference it learns while running.

**Checked by:** `dotnet build` reports `LS2013` for a `WithTarget` or `WithReason` call on `IAuditScope` inside a command handler whose argument reads only the message.

### Take One Timestamp per Message

**ID:** `litespec/backend/application#take-one-timestamp-per-message`

**Statement:** Each handler MUST read the message timestamp from the scoped clock the pipeline stamps once per message.

**Rationale:** A handler that reads the clock two or three times in one decision records different times for one act, sometimes around an outside call. One timestamp per message fixes the moment the message starts, and every decision in the handling answers to the same time. The shared clock stays for infrastructure only.

**Example:** The approve handler stamps its approval with the handled clock, not with a fresh clock read.

**Reviewed for:** Each handler reads time from the scoped clock stamped once per message.

### Declare Exclusive Holds Beside the Message

**ID:** `litespec/backend/application#declare-exclusive-holds-beside-the-message`

**Statement:** Each command needing exclusive access MUST declare its hold beside the message, with its key and its patience.

**Rationale:** A lock copied into each handler carries its own key, patience, and refusal at every site, and each copy drifts. Declaring the hold beside the message takes it in a pipeline step after validation and releases it on completion, refusing with the conflict code when the wait runs out.

**Example:** The refund command declares its per-order hold with fifteen seconds of patience.

**Reviewed for:** Each command needing exclusive access declares its hold key and patience beside the message.

### Refuse a Lost Race as a Conflict

**ID:** `litespec/backend/application#refuse-a-lost-race-as-a-conflict`

**Statement:** Each command that loses a race at commit MUST end with its use case's conflict refusal.

**Rationale:** A duplicate key or a stale version reaches the commit as a store fault, and the pipeline classifies it as the use case's named conflict refusal, so the caller learns what happened and may retry. No reservation, re-read, or retry lives in the handler. The store enforces uniqueness under `litespec/backend/persistence#enforce-uniqueness-in-the-store` and versions every aggregate under `litespec/backend/persistence#version-every-aggregate`.

**Example:** Two clerks register the same book barcode at once; the second commit violates the barcode unique index, and the command refuses with `book-barcode-taken`.

**Reviewed for:** Each command that loses a race at commit ends with its use case conflict refusal.

### Load a Required Record or Refuse It

**ID:** `litespec/backend/application#load-a-required-record-or-refuse-it`

**Statement:** Each handler needing a record MUST load it through the getter that throws the aggregate not-found refusal, and use the finder only for optional records.

**Rationale:** A find followed by a thrown not-found repeats the aggregate decision in every handler, and each copy names its own failure. The required getter throws the aggregate own not-found refusal, so the handler states that the record is required by the call it makes.

**Example:** The approve handler loads its expense with `GetByIdAsync`, while the optional coupon lookup uses `FindByIdAsync`.

**Reviewed for:** Each required record loads through the throwing getter, with the finder kept for optional records.

### Parse Input in the Validator

**ID:** `litespec/backend/application#parse-input-in-the-validator`

**Statement:** Each validator MUST parse every value object in the input, so the handler construction cannot fail on input.

**Rationale:** A validator that checks presence only leaves parsing to the handler, so one input is judged in two layers and the handler throws on caller data. Parsing in the validator keeps business refusals in the domain and input failures in validation, where each belongs.

**Example:** The invite validator parses the email address with `TryCreate`, and the handler creates it without a check.

**Reviewed for:** Each validator parses every value object in the input.

### Validate Query Input

**ID:** `litespec/backend/application#validate-query-input`

**Statement:** Each query carrying paging, filters, or ranges MUST declare a query validator run by the pipeline.

**Rationale:** An unvalidated page size or range reaches the store unchecked, and each query handler clamps its own input or none does. A query validator runs in the same pipeline step as command validators, so reads and writes answer to the same input rule.

**Example:** The list-expenses query validates its page size through its query validator.

**Reviewed for:** Each query with paging, filters, or ranges declares a query validator.

### Send Messages After Commit

**ID:** `litespec/backend/application#send-messages-after-commit`

**Statement:** Each command MUST send its messages from an event handler after the commit, never from the command handler.

**Rationale:** A message that leaves before its transaction commits promises work even when the transaction then fails. The aggregate raises its fact, and an event handler for that fact sends, so no message leaves for a write that never landed. A `sends` effect is `after-commit` or `async`, never `inline`.

**Example:** The submit-expense aggregate raises its submitted fact, and an event handler sends the approver notice after the commit.

**Checked by:** `litespec model check` reports `effect-timing-invalid` (level S) for a command use case declaring a `sends` or `calls` effect with `inline` timing.

### Run Heavy Work as a Request Then a Build

**ID:** `litespec/backend/application#run-heavy-work-as-a-request-then-a-build`

**Statement:** Each command assembling a large artifact MUST record the request and let an automation build the artifact.

**Rationale:** A command that reads many repositories to assemble a large response holds the caller waiting and times out under load. Recording the request answers at once, the build runs in the background, and the requester reads the state of its own request.

**Example:** The export-expenses command records its request, and an automation builds the file the requester then downloads.

**Reviewed for:** Each heavy command records its request, with an automation building the artifact.

### Build Collaborators Through the Container

**ID:** `litespec/backend/application#build-collaborators-through-the-container`

**Statement:** Each handler needing a collaborator MUST take it through the constructor from the container, never build it with `new`.

**Rationale:** A step class built with `new` hides its dependencies, and a handler that looks small is really hundreds of lines behind its helpers. Injected collaborators keep their dependencies visible in the constructor.

**Example:** The reimburse handler takes its expense repository through its constructor, while a step built with `new` that takes a repository is reported.

**Checked by:** `litespec model check` reports `collaborator-repository-invalid` (level A) for a class built with `new` whose constructor takes a repository, and `dependency-count-invalid` (level A) for a handler or automation taking more than ten constructor dependencies.

### Read No Repository Inside a Loop

**ID:** `litespec/backend/application#read-no-repository-inside-a-loop`

**Statement:** Each handler and automation MUST read no repository inside a loop body.

**Rationale:** A read inside a loop turns one handling into as many round trips as there are items, and the slowdown hides behind small test data. A batch read or a scoped query loads every item at once, so the handling costs one trip whatever the data holds.

**Example:** The reminder automation loads due loans with one scoped query instead of reading each loan inside its loop.

**Checked by:** `litespec model check` reports `repository-read-invalid` (level A) for a repository read inside a loop body.

## Conventions

### Use This Use-Case Layout

**ID:** `litespec/backend/application#use-this-use-case-layout`

**Default:** Place each use case in a folder named for it under its module and aggregate.

**Replacement:** A project names its own Application folder layout in its documentation.

**Rationale:** One folder per use case holds everything a reviewer needs to judge it in one place, and the same module and aggregate order in every layer lets a reader move from a handler to its endpoint without a map.

**Example:** The layout reads as follows:

```text
{ProjectName}.Application.Abstractions/   contracts more than one module names
  Clock/                                  the clock port
  Refusals/                               the transport-neutral refusal carriers
  Validation/                             the validity-findings shape
{ProjectName}.Application/
  Pipeline/                               the pipeline stages, and nothing else
  Posts/                                  single aggregate whose name matches the module: use cases sit directly under the module
    PostArchive                           a port several Posts use cases name
    CreateDraft/                          one folder per use case: message, result, validator, handler, mapping
    GetPost/
    ListPosts/
      ListPostsQueryResultItem            a result part named after its result
    OnPostPublished/                     an event handler folder: one folder per fact, holding every handler of that fact
      NotifySubscribersOnPostPublishedHandler   one event handler class with its mark
      PostPublicationNotifier             a port one event handler names
  Customers/                              two aggregates: use cases nest under each aggregate
    CustomerAccounts/
      CustomerAccountScopeLookup          aggregate-owned, so it sits with the aggregate
      RestrictAccount/
    Consents/
      GrantConsent/
```

Module and aggregate order, including the single-aggregate flat form, follow `litespec/backend/architecture#organize-every-layer-by-module-and-use-case`.

Only a use case or an event handler folder gets a folder. Any other type sits flat in the folder that owns it, chosen by `litespec/backend/architecture#place-a-type-at-the-lowest-folder-that-holds-its-consumers`, so every folder name below a module names one use case or one fact. An event handler lives in the `On{Fact}` folder for the fact it handles, named under `litespec/workspace/naming#group-event-handlers-by-the-fact-they-handle`, with its mark under `litespec/backend/application#keep-event-automation-implementations-explicit`. A folder named for a service, a technical kind, or a caller breaks that reading; it is drift rather than a second convention.

**Reviewed for:** Each use-case path matches its module, aggregate, and use case, each event handler sits in its `On{Fact}` folder, and each result part is named after its result.

### Keep Messages Immutable

**ID:** `litespec/backend/application#keep-messages-immutable`

**Default:** Declare commands, queries, results, and validation errors as immutable values.

**Replacement:** A project names its own message-shape rules in its documentation.

**Rationale:** Immutable values give commands, queries, results, and validation errors a shape no handler mutates mid-pipeline. Typed identifiers and shared-kernel value objects cross the boundary; an aggregate-owned value object does not, so the message carries a primitive and the handler reconstructs it.

**Reviewed for:** Each command, query, result, and validation error is an immutable value.

### Return Use-Case Results

**ID:** `litespec/backend/application#return-use-case-results`

**Default:** Return only the values the caller needs from an Application result.

**Replacement:** A project names its own result-shape rules in its documentation.

**Rationale:** A result carries only what the caller needs. An aggregate, session, provider response, or transport result leaving Application makes an inner concern part of the outward contract, and every change to that concern breaks callers.

**Reviewed for:** Each Application result returns only the values the caller needs.

### Keep Mappings at the Owning Boundary

**ID:** `litespec/backend/application#keep-mappings-at-the-owning-boundary`

**Default:** Own domain-to-result projection in Application, transport mapping in WebApi, and view models in the frontend.

**Replacement:** A project names its own mapping homes in its documentation.

**Rationale:** A mapping changes when its output shape changes, so it lives with the boundary that defines that shape: domain-to-result projection in Application, transport mapping in WebApi, view models in the frontend. A mapping kept elsewhere breaks when the owning boundary evolves.

**Reviewed for:** Each conversion lives in the layer owning its output shape.
