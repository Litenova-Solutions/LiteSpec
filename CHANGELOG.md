# Changelog

## v7.0.0

LiteSpec 7.0.0 reshapes the record model around explicit records held by checks. Intent gains flows (the record that was the journey), which serve a goal or answer a problem or a constraint and cite requirements step by step, standalone features that name the problems they solve, and product-level requirements. Behavior gains read models, module events delivered in process through the outbox, guards that hold at commit, refusal sets, typed outputs and row inputs, file values, verified claims, replay rules, automation outcomes, timed transitions and settings that are read and dated, and every link has one meaning and reads from a Links table generated from the schema. Each use case owns its folder, its models and everything that starts it; a handler sends no command, query or event; and a module reads another module's state through a read model, a decision port or a composed screen. Every link between records holds: a finding about a missing, unknown or unused link blocks in every run, a reaction names the step it compensates, a rule names every aggregate it spans, a stale figure names the command that decides it, and an identifier written in prose must resolve. The record that sets a rule aside is the waiver, which states whether it is a gap with the records that close it or a lasting choice with its consequences. Findings block by the strength of the provision they report, proof is bound to the code it proves, the command line finds records by text and reports the links between modules, and `litespec book` serves a live book by default, groups Intent by product area, and writes a book safe to host with `--out`. It is a major release: record types, fields, folders, finding codes and provision identifiers change, as the parts below state.


### Part A: Each Use Case Owns Its Folder

- Each use case owns one folder in each layer that serves it, inside the plural folder of its aggregate: `Application/{Aggregates}/{UseCase}/` with its message, result, result parts, handler, validator, pipeline definitions, own steps and triggers, and, when the use case has an endpoint, `WebApi/{Aggregates}/{UseCase}/` with its endpoint and transport models. A module-level use case drops the aggregate segment in each. A use case with no `WebApi/` folder of its own has no endpoint.
- A reaction's event handler and a job class live with what they run: in the `Application/` folder of the use case their record runs; otherwise in the `Application/Notifications/<Notification>/` folder of the notification they send; otherwise in the `Application/ExternalSystems/<System>/` folder of the system they call. The `On{Fact}/` folders and the module `Jobs/` folder are removed.
- The module project closed set is `Domain/`, `Application/`, `Infrastructure/`, `WebApi/` and the entry type. `Domain/` holds one folder per aggregate in the plural; `Application/` holds one folder per aggregate that has use cases, one folder per module-level use case, plus `Ports/`, `Notifications/`, `ExternalSystems/` and `ReadModels/`; `Infrastructure/` holds `Persistence/`, `ExternalSystems/` and the aggregate and module-level use case folders of use cases with their own adapter; `WebApi/` holds the aggregate and module-level use case folders of use cases reached over HTTP. A module's Contracts project holds `Identifiers/`, `IntegrationEvents/`, `ModuleEvents/` and `Ports/`.
- A message, result, result part, transport model or mapping belongs to exactly one use case. Two use cases that need the same shape each declare it, and the lowest-folder rule places only behavior such as ports, rule calculations and adapters. A step two use cases share lives once in Domain when it decides something, and each use case holds its own copy when it only coordinates.
- `code-placement-drift` reads a trigger's folder from its record's effects, reports a file in the wrong layer folder of its use case, and reports a folder under `Application/Notifications/`, `Application/ExternalSystems/` or `Application/ReadModels/` that names no declared record. LS2019 reports any type a use case folder names in another use case folder, with no exception for messages and results.
- Every folder name is a product noun, a business act, one of the four layers, or a record kind the product speaks; no folder groups by mechanism or by caller.

The new rules are `litespec/backend/architecture#give-each-use-case-its-own-models` and `litespec/backend/architecture#repeat-coordination-never-a-rule`. Decision 0063, Place Each Trigger with What It Runs, records why.

### Part B: Handlers Raise Facts and Send Nothing

- A command handler loads, calls one act and stages; it sends no command, query or event. Only an endpoint, a reaction's event handler and a job send a message, one each. LS2021 reports a mediator held by a type that is none of these.
- A use case's `effects` hold only `sends` and `calls`. `runs` and `emits` leave the use-case effect entry, `emits` and `sharesTransaction` leave every effect entry, and an old entry is `record-invalid`. A reaction or job holds at most one `runs` entry.
- No record declares publishing. An outbound integration event names in `translates` the domain event it carries, and the use cases whose `raises` holds that event publish it. One pipeline step stages each raised domain event, each module event and each integration event built from it, in the same commit. LS2022 reports an event published by user code, and `effect-undeclared` reports a publish call a handler, event handler or job makes. `event-publisher-invalid` derives the publishing module from `raises` and `translates`.
- No edge shares a transaction. Atomic consistency covers the writes of one use case, and a rule across use cases is held by a state guard. Every command commits in its own pipeline.
- A module publishes no refusal: a use case refuses only with refusals its own module declares. Another module asks it to act only through a public command (Part AN).
- A conflict refusal from the command a reaction runs is transient and retried under the reaction's `onFailure`; a declared final refusal ends the reaction.
- A job selects its items and sends one command per item; a condition a batch command checked moves into the per-item act.

The new rule is `litespec/core/automation#run-at-most-one-use-case-from-a-reaction-or-job`. Decision 0064, Raise Facts and Send Nothing from Handlers, records why.

### Part C: The Read Model Record

- A `read-model` is a Behavior record at `.litespec/behavior/modules/<module>/read-models/<name>/<name>.yaml` with the identifier `read-model/<module>.<name>`: one module's own copy of what it needs about another module's aggregate. It names that aggregate in `keyedBy`, each field's source as `module-event/<module>.<name>#<field>` or `integration-event/<name>#<field>` in `from`, an optional `accumulate` of `latest`, `sum` or `count`, the events that remove a row in `removedBy`, and how stale a reader may see a row in `freshness`. A `personal` or `sensitive` field names its erasing event in `erasedBy`.
- A use case names a read model in `reads`, and a query output group may read one. `litespec new read-model <module>.<name>` scaffolds one from `templates/consumer/read-model.yaml`, and `litespec rename` moves one.
- `model check` reports `reference-unknown`, `read-model-erasure-missing`, `read-model-key-invalid` and `read-model-unused`. A read model's explanation file carries Sources, Freshness and an optional Example.
- In .NET a read model lives in its `Application/ReadModels/` folder, one plural folder per read model, with its document, one `{ReadModel}On{Fact}Projection` per source event, the reader `I{ReadModel}ReadModel` and its implementation, each carrying the read model's mark. LS2007 does not hold a projection to the reaction naming rule.
- A command whose decision needs another module's current value asks one question through a decision port with one read-only method. A screen that only shows another module's data calls each owner's own query, and a query handler reads only its own module's store and read models. Repositories, outside-system ports and rule calculators stay ports.
- With the outbox extension, each module event or integration event a read model names is kept for as long as a read model may rebuild from it, apart from the delivery records, with erased personal fields removed.
- The book lists read models under their module, shows each field with the published field it copies and its erasing event, and lists on each integration event the use cases that publish it.
- Claimwick gains the reporting module with `read-model/reporting.reimbursed-expense`, built from the new `module-event/spending.expense-reimbursed` and erased by `module-event/spending.expense-personal-data-erased`, and the `use-case/reporting.list-reimbursed-expenses` query that reads it.

The new rules are `litespec/core/automation#declare-each-read-model-field-from-a-published-field`, `litespec/core/automation#erase-a-read-model-personal-field-with-its-owner`, `litespec/backend/application#read-another-modules-state-through-a-read-model`, `litespec/backend/application#ask-another-module-one-question-through-a-decision-port`, `litespec/backend/dotnet/architecture#place-each-read-model-in-its-application-read-models-folder`, `litespec/ext/outbox#keep-published-events-available-for-read-model-rebuilds` and `litespec/frontend/data#compose-a-screen-from-each-owners-query`. Decision 0065, Read Another Module Through a Read Model, records why.

### Part D: Domain Parts, Read Shapes and Module Edges

- An aggregate's `Domain/` folder holds one folder per part its record declares as a dotted data group, such as `Parcels/` for `parcel.*`, with the part's child entity and the values only it holds. The part's events and refusals stay in the aggregate's `Events/` and `Exceptions/` folders.
- A row or projection a read port returns is the port's answer, not a Domain constituent, and sits with its port.
- A use case maps another module's port answer at its edge and returns it in no result or transport model of its own; only a typed identifier crosses.
- A domain event carries every field its integration events publish.
- A static class of string constants is a closed set and becomes a record hierarchy, like any closed set in Domain.

### Part E: Branded Identifiers in the Frontend

- A generated client gives each contract identifier its own branded type from the identifier's named schema component, and a route loader parses each identifier parameter with the generated schema, under the new default `litespec/frontend/data#brand-every-identifier-from-the-contract`.
- A form schema or view model stays in its use-case folder, and a second use case that needs the same shape declares its own.

### Part F: The Analyzers

- LS2019 drops its exemption for types ending in `Command`, `Query`, `CommandResult` or `QueryResult`.
- LS2021, Message Sent Outside an Entry Point, reports an `ICommandMediator`, `IQueryMediator` or `IEventMediator` held by a type in the modules project that is not an endpoint, an event handler or a job.
- LS2022, Event Published by User Code, reports an `IEventMediator` or a type named `*EventPublisher` named anywhere in the modules project.
- LS2007 ignores an event handler named `*Projection` or placed in a module `ReadModels/` folder.

### Part G: Records in the Book and the Command Line

- The record schema, `litespec new`, `litespec rename`, the folder checks, the parent rules and the placement table carry the `read-model` type and its `read-models/` typed folder.
- The book links `read-model/...` identifiers in prose, labels the read-model fields, and drops the `emits` edge label.

### Part H: Flows

- The Intent record `journey` is now `flow`: identifiers `flow/<name>`, the folder `intent/flows/`, and the template `templates/consumer/flow.yaml`.
- Each step has a named identifier `step/<flow>.<act>`, the act a verb phrase in kebab case (`step/submit-and-reimburse.approve-expense`), and a `title` in Title Case whose words the act repeats. List order is the main path; no position lives in an id. A step id is a real identifier: `model query`, search and every link check resolve it, and a screen or use case serves `step/<flow>.<act>`.
- Branches live on the step they leave, in its `branches` list: each has one `when` condition (two conditions are two branches), exactly one of `then` (a step of the same flow) or `ends` (the outcome sentence), and an optional `instead` with its own `carries`. The flow-level `branches` list is removed.
- `model check` reports a step no path reaches (`step-unreached`), a branch with both or neither of `then` and `ends` or a `when` joining two conditions with "or" (`record-invalid`), a `then` naming no step (`reference-unknown`), an act that is no word subsequence of its title (`identifier-title-invalid`), and a step whose serving use cases have no implementing code (`step-implementation-missing`). `model query step/<flow>.<act>` traces the step to the screens and use cases that serve it, the requirements it carries, and the code and tests behind them; the book shows each step with its title, its branches under it, and its trace.
- A flow carries exactly one reason: `serves` names one goal, or `answers` names one problem or constraint. Only `serves` moves a goal.
- Each step names the requirements it carries in its own `cites`; the flow's citations are the union of its steps'. A step's text states what the person does and sees, and a failure states what the person sees and can do next.
- A step names a person in `actor` or an external system in `system`; jobs and reactions serve a step upward and never appear in one.

### Part I: Features, Product Requirements and Goal Outcomes

- A feature answers exactly one problem and sits in that problem's folder: `intent/problems/<problem>/features/<feature>/`, with its requirements under it. There is no `solves` field; a feature that answers two pains is split, or the two pains are one problem. The 7.0.0 draft that made features stand alone with a `solves` list is withdrawn.
- A permanent commitment with no offer is a product-level requirement at `intent/requirements/<name>/`, identifier `requirement/<name>`, citing the constraint, goal or problem it comes from.
- The product states its exclusions in `excludes`; only an exclusion overrides a requirement.
- A goal records measured outcomes in `observations`; its completion shows delivery, and its outcome reads "not measured" until it has one.

### Part J: Explicit Records and One Source per Fact

- A record states every fact and link a reader needs to read it alone, and a check holds each copy that the model can also derive to its source. `governs`, `exposure`, field classification and data-class membership stay on the record under agreement checks.
- Retention lives on the aggregate data class: `period`, `periodStartsAt`, `basis`, `purpose`, `enforcedBy` and `exceptions`. The retention schedule is rendered by the book, and an external system names the data it holds in `processes`.
- `exposure` states who reaches a use case, through which entry points, under which permission, and what is at stake. `background` is the Given shared by every scenario of its use case.
- Reactions and jobs require `onFailure`, `idempotency` and `outcomes`. Queries carry neither idempotency nor consistency, and `consistency` is required only on a command that writes several aggregates.
- The records state the whole specification under `litespec/core/model#keep-the-whole-specification-in-the-records`, and each explanation file restates the same specification under `litespec/core/writing#explain-the-same-specification-the-records-state`, with `litespec/core/writing#update-a-record-and-its-explanation-together` keeping both in step in one commit. Intent records gain `failureCases`, `workaround`, `cost` and `resolutionCriteria` on a problem, `scope`, `obligations`, `productEffect` and `openPoints` on a constraint, `findings` and `openQuestions` on research with `note` on each source, `successCriteria` and `excludes` on a feature, `responsibilities` and `knowledge` on an actor, `limits` on a role, and `context` on a decision. Behavior records gain `responsibilities` and `excludes` on a module, `limits` on an aggregate, `steps` and `edgeCases` on a use case, `rationale` on a rule, `compatibility` on module and integration events, `audience`, `session`, `reauthenticationTriggers` and `limits` on an entry point, `task` and `states` on a screen, and `excludes` on a permission set. The section table pairs each explanation section with its backing fields, an explanation leaves out a fixed section whose backing fields are empty, research and decision records no longer always need an explanation file, and `explanation-section-unbacked`, `constraint-obligation-source-unknown` and `research-finding-source-unknown` are new findings. Decision 0095, Keep the Whole Specification in the Records, records why.
- A field name reads in the context of its record type under `litespec/core/language#read-each-field-name-in-its-record-type`: one concept keeps one name across types, and `litespec-maintain validate` keeps three guards, a record-type name holding links, a link verb holding links, and a date name holding a date, with the same-shape check removed. Decision 0096, Read Field Names in Their Record Type, records why.

### Part K: Fields and Checks for Stated Rules

- A reaction triggers only on its own module's domain event or on an integration event.
- Every link is a typed identifier that the link and locality checks read, including consistency bindings, output-group widening, input rules and failure targets.
- A domain event's `carries` is typed, and an integration event publishes only fields its domain event carries.
- New findings: `reaction-trigger-foreign`, `scenario-rule-unexercised`, `event-field-uncarried`, `refusal-generic-written`, `reference-unknown`, `statement-word-vague`, `example-keys-drift`, `external-system-placement-wide`, `rule-governs-drift`, `exposure-reference-drift`, `exposure-missing`, `classification-drift`, `data-class-fields-drift`, `setting-unused`, `background-scenarios-missing`, `input-claim-unverified` and `idempotency-replay-missing`.

### Part L: One Links Table and One Meaning per Verb

- Each link property in the record schema carries its reads-as phrases in `x-reads-as` and `x-read-back-as`; `litespec-maintain provisions` generates the Links table from them, and the book takes its connection labels from them.
- Intent links point up a layer, effect and structure links follow the effect graph, and only `decides`, `allows` and `holds` point down.
- Notification `audience`, integration-event `requires`, consistency `boundBy` and output-group `widenedBy` give each verb one meaning; `triggeredBy` reads "is triggered by".
- Read models fulfill no requirement; the query that reads one does. A served-content entry point keeps `fulfills`.

### Part M: New Behavior Concepts

- Files: a `file` value type with `contentTypes` and `maxSize`. Every output field is typed, a list of rows takes `type: list` with an `identifies` shape, and choices take `choices`.
- Refusal sets: each module declares its refusal codes once in `behavior/modules/<m>/refusals.yaml`, mapped to a problem type. `forbidden`, `invalid-input`, `not-found`, `conflict`, `unauthenticated` and `sign-in-too-old` are derived, never written. A rule names its refusal code.
- Access: `grant: signed-in`, `recentSignInLimit` naming the window setting, and the `device` entry-point authentication.
- Inputs a provider verified carry `origin: verified-claim` with `verifiedBy`; a command's `idempotency` states what a replay does in `replay`.
- Settings are read through `reads`, can be `dated`, and take typed defaults and `choices`. A transition may be moved by time with `after` and a job in `movedBy`.
- An integration event names the external system it travels `through`, and an inbound one names how it arrives in `arrivesThrough`: a webhook entry point or a subscribed external system.
- An outside call separates rejected, accepted and unknown outcomes, with `onUnknown` for the unknown case.
- Notifications are messages to people, delivered `deliveredThrough` an external system; consent is recorded data a rule requires through `consentRule`. Under the concurrency extension a command states its `conflicts`.
- Reaction and job scenarios end in a run use case's result or in one of the record's outcomes. A scenario belongs to the use case, reaction or job it exercises.
- An entry point declares the `languages` it serves and its money and moment `formats`, each an option of the money and time patterns; the screens render them from the locale data of the reader's language and the currency and country the record carries, and `sentence-language-missing` reports a refusal with no sentence in a declared language.

### Part N: Removals and Names

- The product is the root record above the record layers. It is stored at `.litespec/product.yaml` with its explanation file `.litespec/product.md`, its layer key is `root`, and every layer hangs under it: the book opens the outline with the product and starts the Intent layer at goals, problems, features and flows. Decision 0091, Place the Product Above the Layers, records why.
- Removed: the `compensated` consistency mode, the `manual` job kind, the `default` and `cross-aggregate` rule kinds, the `not-pending` idempotency mode, `builtAsynchronously`, the external-system prose fields `sends`, `receives` and `failure`, `writes` on reactions and jobs, the screen `floorplan` link (the design contract maps screens to floorplans), and the retention-schedule record type.
- Renamed: the integration-event and external-system operation field `direction` (`out`, `in`) to `action` (`send`, `receive`), `view` to `read-model`, `event` entries to `domain-event` (`domain-event/<aggregate>.<name>`, folder `domain-events/`), `changes` to `writes`, `transitions.by` to `movedBy`, `runsWhere` to `runsWhen`, `onFailure.then` to `ifExhausted`, audit `exempt` to `exemptBecause`, rule kind `commercial-data` to `frozen-value`, `permission-list` and `entry-point-list` to `permission-set` and `entry-point-set`, product `addresses` to `users`, screen `address` to `route`, flow-step `cites` to `carries`, `of` to `identifies`, `auth` to `authentication`, `dedupKey` to `deduplicationKey`, `params` to `parameters`, `requiresConsent` to `consentRule`, `recentSignIn` to `recentSignInLimit`, `atBound` to `outOfRange`, and waiver `exit` to `exitWhen`; external-operation `timeout` becomes an ISO 8601 duration.
- The Intent record `vision` is now `direction`: identifiers `direction/<name>`, the folder `intent/directions/`, and the template `templates/consumer/direction.yaml`. Only `statement` is required, `summary` is optional, and the explanation file is free-form, with no fixed sections. The product explanation loses its optional `Direction` section. The four rules are `litespec/core/intent#state-each-direction-in-its-statement`, `litespec/core/intent#keep-dates-and-priorities-out-of-directions`, `litespec/core/intent#build-nothing-from-a-direction`, and `litespec/core/intent#cite-the-direction-a-decision-keeps-open`. A consumer holding vision records moves each record to `intent/directions/`, renames its identifier, and rewrites its explanation file as free prose. Decision 0094, Record Product Directions as Light Records, records why.
- Finding codes renamed: `atomic-group-*` to `atomicity-*`, `constituent-parent-missing` to `part-parent-missing`, `notification-sender-missing` to `notification-effect-missing`, `permission-orphaned` and `role-orphaned` to `permission-unused` and `role-unused`, `use-case-journey-missing` to `use-case-flow-missing`, and the `view-*` codes to `read-model-*`.

### Part O: A Consistent Standard

- A record's content is what it states and changes; its links are its reasons. External systems sit at the lowest node that holds their callers.
- Strength, detection and blocking are separate: a finding blocks by the strength of the provision it reports, `SHOULD` is advisory and needs no waiver, and prose and naming checks are advisory.
- `select-task-context` is the one context rule; the intent layout is fixed; the model page owns the cross-module rule; scope, containment, storage and book grouping are four words.
- Contradictions between records are reported and resolved by updating records; behavior with no record above it is recorded or reported, never removed.
- Access is evaluated in one stated order, and a permission allows exactly the use cases that name it.
- Each effect of a reaction or job runs on its own; a read model deduplicates by event identity, orders by source version and keeps erasure tombstones, and never guards a decision that needs current truth.
- Proof is fresh only for the commit its run read, or when nothing but records and Markdown changed since; the book says "claimed" for a link and "verified" for proof; a release never passes on a missing reader.
- Names are unique per record type. Each glossary entry names its home page, and `docs/reference/concepts.md` lists every concept with the page that owns it.

### Part P: The Live Book

- `litespec book` serves the book from memory on localhost and rebuilds it when the records, code or test results change; `litespec book --out <folder>` writes a static book that opens from disk or any static host.
- `--editor` is now `--links <template>`, with `{root}`, `{path}`, `{line}` and `{commit}`; a written book has no source links unless a template is given, carries no local paths, and records the release and the build time.
- The book takes a look that follows IBM Carbon, in IBM Plex type and in the reader's light or dark scheme: a dark top bar, the outline on a gray panel and the record on the white page.
- Every record type shows as a short code badge in a color of its own, such as F for a feature on blue and Pb for a problem on red; hovering it explains the type. A new About this book page lists every code with its record count, and holds the build time, the test run and the completion that every page used to repeat.
- The rail lists the record's Trace to the other record layers only, its Completion and an On this page list. The keyboard shortcuts dialog is removed, and explanations open only on hover or focus.
- A flow's branches show inside the steps they leave from, and the step a branch continues at names where else it is reached from.
- The record page is centered between the outline on the left and the rail on the right edge, so a wide screen keeps it in the middle.
- `litespec context` writes the project's records and explanation files as one compact text file for an AI chat, filtered by `--layers`, `--modules`, `--types` and `--include records|explanations|both`, compacted unless `--full` is given. It defaults to `--include records`, so it writes the records that hold the whole specification, and explanations only when asked. Each file stands between `=== BEGIN FILE: <path> ===` and `=== END FILE: <path> ===`, and the file ends with `=== END OF CONTEXT: <n> files ===`, so a reader can tell a complete context from a truncated one. It is read-only.

### Part Q: Waivers

- The record that sets a required provision, page or finding code aside is the waiver: `type: waiver`, identifiers `waiver/<name>`, the folders `.litespec/governance/waivers/` and `.litespec/governance/modules/<module>/waivers/`, the page `docs/core/waivers.md`, the template `templates/consumer/waiver.yaml`, and the codes `waiver-expired`, `waiver-invalid` and `waiver-duplicate`. Decision 0014, One Waiver Record, records why.
- The advisory exit inside a provision is the `**Deviation:**` part again, so one word no longer names both the exit that needs no record and the record that sets a required rule aside. `litespec/core/waivers#deviate-from-advice-without-a-record` and `litespec/core/scopes#set-a-rule-aside-only-through-a-recorded-waiver` state the two.
- Every waiver states its kind: `gap` for work the team means to close, `lasting` for a choice the project keeps. A gap carries `expiresOn`, `exitWhen` and `closedBy`, the records whose change closes the gap; a lasting waiver carries `consequences` and no end date.
- `model check` reports `waiver-duplicate` when two live waivers cover the same finding code, provision or page on the same appliesTo record or the same path, and `reference-unknown` for a `closedBy` entry that names no written record.
- The `waiver-index` region lists every waiver of the product in the README of the product `governance/waivers/` folder, with a Module column; gaps by `expiresOn` with their `closedBy`, then lasting waivers with their consequences.
- The book waiver page shows the kind, the closing records and the consequences, and Claimwick's waivers state their kinds and closing records.
- The new rules are `litespec/core/waivers#state-the-kind-of-every-waiver`, `litespec/core/waivers#name-the-records-that-close-a-gap` and `litespec/core/waivers#cover-each-finding-with-one-waiver`.

### Part R: Links That Hold

- A reaction that undoes a committed step of a multi-step process names the step's use case in `compensates`, runs another use case, and answers the event that reports the later failure. `model check` reports `compensation-target-invalid` for a `compensates` naming a use case that is not a command or the same use case the reaction runs, and `compensation-trigger-invalid` for a compensating reaction answering an event the compensated use case raises. Decision 0015, Automations and Sagas, restates compensation in these terms, and decision 0076 keeps the `compensated` consistency mode removed with `compensates` as the field its undo lacked. The new rule is `litespec/core/automation#name-the-step-a-compensating-reaction-undoes`.
- A rule whose `governs` names more than one aggregate names every governed aggregate in `binds`, or `model check` reports `rule-binds-missing`, under `litespec/core/domain-model#name-every-spanned-aggregate-in-binds`.
- A query output group and a read model whose figure a command re-decides name the deciding command use case or its aggregate in `freshness.decidedBy`, allowed only with `snapshot` or `eventual` freshness. `model check` reports `freshness-decider-invalid` for a `decidedBy` naming a query or a decider outside the module that owns the figure's source, under `litespec/core/use-cases#name-the-use-case-that-decides-a-stale-figure`.
- The .NET reader records each event handler's subscribed event type. `model check` reports `reaction-subscription-drift` for a marked handler subscribed to an event outside its reaction's `triggeredBy`, and `reaction-trigger-unhandled` for a declared trigger no marked handler answers, under `litespec/backend/dotnet/application#subscribe-each-reaction-handler-to-its-declared-triggers`.
- A refusal code is carried only from its own module and by one exception type: `model check` reports `refusal-module-foreign` and `refusal-code-duplicate`, under `litespec/backend/dotnet/domain#throw-only-refusal-codes-the-module-owns`. Two rules a boundary maps to one response keep two codes.
- `model check` reports `convention-unreached` for a project convention whose `Applies to` matches no file, folder or record, running the matcher `rules <path>` runs, under `litespec/core/agent#scope-every-convention-to-files-it-governs`.
- Prose names a record by its identifier in backticks, and `model check` reports `reference-unknown` for a backticked record identifier that names no written record, or a backticked provision identifier the carried release does not ship, in any record field, explanation file, document or the root `AGENTS.md`, skipping fenced code and generated regions, under `litespec/core/model#name-records-in-prose-by-their-identifiers`.
- Every finding about a missing, unknown or unused link between records, or between a record and code, blocks: `read-model-unused`, `actor-unused`, `runbook-unused`, `setting-unused`, `source-unused`, `reference-unknown`, `screen-unreachable`, `screen-dead-end`, `requirement-cites-missing`, `decision-basis-missing`, `exposure-missing`, `scenario-rule-unexercised`, `explanation-orphaned`, `result-scenario-missing` and `example-keys-drift` move to level G, and `use-case-unreached`, `effect-unimplemented` and `part-parent-missing` to level S. The planned-Intent findings block in every run, not only in the pull-request check; a record that must stay unreached carries a gap waiver.

### Part S: Tools, Analyzers and Text

- `litespec model query --search <text>` lists the records whose identifier, title, summary or statement holds the text, an exact identifier first; `litespec model query --list <type> [--module <module>]` lists every record of a type; `litespec model check --module <module>` prints only that module's findings and exits on them alone; `litespec model edges [--module <module>]` prints each module's links to other modules, by link kind and by module, and every pair of modules linking each other both ways.
- The book offers a product-area grouping of the Intent layer: one group per Behavior module, listing the features whose requirements its use cases, reactions, jobs or screens fulfill and the flows whose steps they serve, with a Not yet fulfilled group; the book writes it as `data/product-areas.js`. Decision 0051 names this as Intent's navigation by product area.
- LS3002 reports a `CancellationToken` parameter that is not last, or second to last before a `params` array, or not named `cancellationToken`; LS3003 reports a C# file not named for its one primary type. The new rule is `litespec/backend/dotnet/naming#put-the-cancellation-token-last`, and Claimwick's state cases sit one per file.
- `token-limit-failed` is `design-token-limit-failed`. Decisions 0005, 0014, 0048 and 0049 take the slugs `0005-flows-serve-a-goal`, `0014-one-waiver-record`, `0048-state-the-start-and-branches-of-each-flow` and `0049-literal-flow-words-concrete-consequences`, and decisions 0004, 0005 and 0051 state the 7.0.0 record model.
- Text that still named removed shapes states the 7.0.0 ones: the read-model finding meanings, `record-placement-invalid`, the Behavior overview and workspace tree, the cache and realtime freshness modes, the testing page's rule spanning aggregates, manual work as a command whose entry point and access name its starter, and the scope of a project-wide `bdd` selection.

### Part T: Plainer Pages

- LiteSpec's own pages, examples, templates, READMEs and tool messages follow the writing and tone guidelines in `WRITING.md`: plainspoken technical writing for a capable reader new to LiteSpec, claims at the strength the source gives them, and the meaning of every provision kept exactly while its prose is rewritten for clarity. Consumer records keep following the writing standard on `docs/core/writing.md`.
- A page opens with a lead paragraph under its title instead of an `Intent` or `Purpose` section: `litespec-maintain validate` reports `PAGE_LEAD_MISSING` for a page with none, under `litespec/core/authoring#open-each-page-with-a-lead`, which replaces the three-sentence Intent rule. The site shows the lead as the page introduction.
- Every learn page, guide, standards page, extension, pattern page, tool page, analyzer page and hand-written reference page is rewritten to these guidelines, and the finding meanings and fixes the tools print say what happened and how to fix it. Provisions whose titles hid their act are retitled; the generated list below names each rename, and every citation moves with it.

### Part V: Module Events and Business State in Aggregates

- State the business cares about is an aggregate with its invariants; state that exists only because of a technical mechanism, such as outbox rows, idempotency keys, caches or provider delivery logs, is infrastructure and no Behavior record, under `litespec/core/domain-model#keep-technical-state-out-of-behavior`. An aggregate with no lifecycle of its own declares the single state `Active`, which leaves room for a later soft delete. Aggregates carry no `kind` and no `owner`. Claimwick's suppression list is `aggregate/spending.reminder-preference`, which only the manager or an admin changes.
- A `module-event` is a fact one module publishes for the other modules of the same product: `module-event/<module>.<name>` at `.litespec/behavior/modules/<module>/module-events/<name>/<name>.yaml`, naming the domain event it `translates` and selecting the `fields` of that event it publishes, so each field keeps one definition. An integration event stays the fact that crosses the bounded context, with its own typed `payload` and the external system it travels `through`. A reaction answers its own module's domain event, a module event of any module of the product, or an inbound integration event; read models copy module events; erasure and `removedBy` name either kind. `integration-event-field-uncarried` is now `event-field-uncarried`, and `litespec/backend/architecture#subscribe-across-modules-only-to-integration-events` is now `litespec/backend/architecture#subscribe-across-modules-only-to-module-events`. Claimwick's reporting module reads `module-event/spending.expense-reimbursed`.
- Decision 0092 records why.

### Part W: Guards That Hold at Commit

- A command that reads an aggregate whose rule guards its write names that aggregate in `consistency.guards`; `consistency` is allowed on such a command as well as on a command writing two or more aggregates. `model check` reports `guard-undeclared` for a `boundBy` rule governing a read aggregate that `guards` leaves out.
- At large size, a guarded command has a scenario naming the competing command in `competesWith`, or `model check` reports `guard-race-scenario-missing`.
- On .NET, the repository reads the guarding document with a `FOR SHARE` query through the Marten session inside the transaction LiteBus opens through `IUnitOfWorkBoundary`, so a competing change waits for the commit. Claimwick's submit-expense guards the spending policy against a concurrent threshold change.

### Part X: Completion, Proof and the Model Check Command

- A proof is fresh when its run commit is HEAD, or when every path changed since is under the project's LiteSpec folder or a Markdown file; otherwise it is stale and reads "last passed at <commit>, <N> commits ago". `proofs.json` holds each scenario's run commit, outcome and run; only `litespec model check --record-proof` writes it, and CI runs that on the pull request head, so a plain `model check` changes no file.
- Completion carries `inScope`, `parked` and `total` counts. `heldBy` lists only in-scope records, and `parkedBy` lists each parked record with its waiver and expiry; the book shows "verified, 12 of 14; 2 parked".
- `litespec model check --since <ref>` checks the tree at the ref and the working tree, and blocks every finding present now and absent at the ref, matched by code, subject and message.
- `litespec model check` exits 3 when no finding blocks but a reader was missing, and prints which frontend went uninspected and how many findings it left unblocked.
- A decision may decide a record another decision also decides; `decision-decides-duplicate` is removed, and a decision that replaces another names it in `supersedes`.

### Part Y: Pages That Match Their Checks

- Specify adds the detail an Intent record implies: a detail is implied when every faithful fulfillment needs it and no actor meets it as an outcome, limit or figure the promise does not name. Anything else goes up as a question to the owner or a backlog item, and every figure the owner sets goes to the owner.
- `result-scenario-missing` runs at large size or with the `bdd` extension, as its provision now says; the planned-Intent findings run when a project claims Behavior; a quality requirement may be fulfilled by a technical decision naming it, its check, or a test that proves it.
- `litespec-maintain validate` reports `PROVISION_CHECK_SCOPE` for a provision whose Statement leaves out the size or extension condition of the finding codes it cites.
- `litespec/core/model#author-one-slice-at-a-time` is now `litespec/core/layers#write-each-layer-from-the-layer-above-it`: each slice writes its Intent before its Behavior and its Behavior before its code, and independent slices run at the same time.
- `litespec/core/use-cases#bind-every-keyed-setting-in-one-marked-options-class` is now `litespec/core/use-cases#bind-every-keyed-setting-in-one-marked-element`, independent of the stack; the options class rule moves to `litespec/backend/dotnet/config#bind-every-keyed-setting-in-one-options-class`.
- A test that proves no record is reported and resolved by writing the record or reporting the gap, never deleted for lacking one.
- A project may declare a retired synonym such as `archive` as its own verb when the term's definition states how it differs from the baseline verb; `verb-distinction-missing` reports one that does not.
- `implementation: own` keeps the team's code layout and analyzer architecture rules, while the Behavior model and its units of work still apply.
- Smaller corrections: `triggeredBy` reads "is triggered by", a flow step's `system` is a declared down-link, a setting several modules read is owned by one module, a product-level rule governs records in any module, follow-on state changes run after the commit while reads and inline calls run inside it, and a refusal is a declared business outcome rather than a technical failure.

### Part Z: In-Process Delivery with LiteBus 7.2

- Module events and domain-event reactions are delivered by the LiteBus outbox in process against the product's database: `StageCollectedEvents`, `UseInProcessDispatch`, `EnablePerHandlerDelivery` and `EnableOutboxProcessor`. No broker is needed inside the bounded context; an outbound integration event uses a transport dispatcher toward the external system it names.
- Every outbox subscriber carries `[SubscriberName("<module>.<handler>")]`, so envelopes reach a renamed handler.
- A projection the caller reads when the command returns carries `[DispatchImmediately]` with `EnableImmediateDispatch`: LiteBus dispatches it after the commit and before `SendAsync` returns, and the processor retries what fails. The deployment reports `litebus.outbox.immediate_dispatch.count` and `litebus.outbox.immediate_dispatch.duration` beside the backlog indicators.
- Acceptance tests drain the outbox with `LiteBus.Testing.DurableMessaging`. The manifest pins `LiteBus.Testing.DurableMessaging`, `LiteBus.Outbox.Extensions.OpenTelemetry`, `LiteBus.Outbox.Storage.EntityFrameworkCore`, `LiteBus.Outbox.Dispatch.Amqp` and `LiteBus.Outbox.Dispatch.Kafka` at `7.x`, and these provisions need LiteBus 7.2.0 or later.

### Part AA: Records with One Home for Each Fact

- A value the business changes while the system runs, such as a threshold a manager sets, is aggregate data with its rule, refusal, access and audit; a `setting` is operator configuration the running system reads. Claimwick's receipt threshold lives on the policy aggregate.
- A refusal code and its meaning live once, in the module refusal set; a refusal result names its `code` and keeps the `sentence` a person reads. A refusal set entry carries no `problemType`: the project may declare `problemTypeBase` in `.litespec/project.yaml`, and a code's problem type is that base followed by the code's part after its module, or that part alone as a short type name.
- A `screen` may fulfill a requirement about the interface, such as what a chart shows.
- Served content is the `servedContent` list of a browser entry point, each entry with `path`, `contentType`, `cacheControl`, `versioning` and the requirement it `fulfills`; the `served-content` entry-point kind is removed.
- A field an extension owns is required only when the project selects that extension: a command's `audit` with the `audit` extension, reported as `audit-missing`; a field written while its extension is not selected reports `field-extension-unselected`. `idempotency` on commands and `onFailure` on reactions stay required.
- A constraint's explanation keeps its Effect on the Product as free prose; the book lists the requirements citing the constraint from their links, and `constraint-effect-unlisted` and `constraint-effect-uncited` are removed.

### Part AB: One Owner for Each Rule, Code and Check

- A finding reports at the level `FindingCodes` gives it; a check no longer raises or lowers a level per finding.
- Overlapping finding codes merge into one code per author action: `field-invalid` into `record-invalid`; `record-parent-missing`, `folder-invalid`, `module-undeclared`, `record-folder-drift` and `identifier-folder-drift` into `record-placement-invalid`; `name-duplicate` and `decision-number-duplicate` into `record-key-duplicate`; `prose-reference-unknown`, `transition-unknown`, `reaction-trigger-unknown`, `read-model-source-unknown`, `refusal-code-unknown`, `source-key-unknown` and `scenario-result-unknown` into `reference-unknown`, with `link-unknown` kept for file-system link targets and wireframe references reported as `wireframe-invalid`. A numbered statement without a measure reports `statement-measure-missing`.
- Heading case, example length, rule naming and generated-output drift findings are advisory, and the maintainer's sentence, modal, casing and vocabulary diagnostics are warnings.
- Each traceability finding has one implementation: the analyzers own what one compilation shows, the CLI what needs the records.
- Duplicate provisions merge into one owner provision, which other pages cite, keeping the stronger strength; the generated list below names each removed provision with its survivor.
- The `stage` extension is removed; its deferrals are gap waivers. The `jobs` extension keeps scheduled jobs and reports, and human work queues move to the new `queues` extension. `plans` and `tenancy` cite the core provisions they restated.
- The UI patterns are grouped into categories and are tested layouts a team chooses from: their provisions are defaults with a Replacement for the project's own choice, and the administration and finance categories are marked as examples from a finance product.
- Templates are checked against the record schema rather than copied from example records, and the generated `docs/reference/concepts.md` is removed.
- The book opens in light mode, and its Outline groups records by type in reading order: Intent with directions, goals, problems with the features that solve them and their requirements, features, flows, constraints, research and decisions; Behavior by module; Implementation with technical decisions; Governance with waivers.

### Part AC: Kernel, Platform, and One Project per Module

- The .NET backend separates the business values every module speaks into `{Product}.Kernel`, with no dependency on any library, and the technical capabilities into `{Product}.Platform`, which names no module: persistence, messaging, access, auditing, HTTP, country packs, and module composition.
- Each module is one project with its entry type, `Domain/`, `Application/`, `Ports/`, `Infrastructure/`, and `WebApi/`, beside a sibling `{Product}.{Module}.Contracts` project. Aggregates live under `Domain/` with one folder each in the plural, and each layer folder holds one plural aggregate folder with one folder per use case inside it: `Application/{Aggregates}/{UseCase}/` always, `WebApi/{Aggregates}/{UseCase}/` when the use case is reached over HTTP. A module-level use case drops the aggregate segment in each. Only the Contracts project and the entry type are public, and hosts compose modules through `AddModules`. LiteSpec supports one architecture style: the only project choice is one project per module or one modules project, with `LS2020` as the boundary of the latter.
- A module plugs into a platform capability by contributing data instead of copying platform code: stored types and discriminators, policies, converters, and routes.
- Each module's contracts compile as the sibling project `Modules/{Product}.{Module}.Contracts/`, never a project nested inside the module folder, so module projects reference contracts projects and never each other, and two modules may depend on each other's contracts without a project cycle. Identifiers every module carries, such as the workspace, business, and person identifiers, live in the Kernel, and a Contracts project names no domain type: the publishing module maps each domain event to its module event.
- The Kernel takes its name from the Shared Kernel pattern, and the Platform follows the microservice chassis pattern.
- The record tree stays, with aggregate folders in the plural and record ids in the singular: a use case record under `aggregates/{aggregates}/use-cases/{use-case}` maps to `Application/{Aggregates}/{UseCase}/` and, when reached over HTTP, `WebApi/{Aggregates}/{UseCase}/`, and an aggregate record maps to `Domain/{Aggregates}/`. A use case record under `use-cases/{use-case}` drops the aggregate segment in every layer.
- Production of a file format, rendering, and parsing live in Infrastructure behind a module port, even when the code uses only the base class library. A use case folder holds its message, handler, validator, definitions, and result with at most a few helpers and no file over 200 lines.
- Kernel values are referenced directly from any layer the layer table allows; copying one into a use case folder breaks the rule. WebApi converts `Money` through the platform converter, which writes the amount as an invariant decimal string, reads only a string amount with a three-letter code, and registers once.
- An access condition shared by several use cases is one composable authorization policy, `Permission`, `RecentSignIn`, `Self`, or `System`, declared in each authorization definition and evaluated by the one guard in the order signed-in, condition, permission.
- A condition only a module can decide, such as owning the record a message names or the workspace being open, is a condition the module contributes to the platform: the policy names it with `When(condition, inner)`, the one guard evaluates it after the sign-in checks and before the permission, and a condition that holds for every message runs without a policy change.
- A record sentence describing a possible future is not a specification: code builds no interface, provider list, or hook for a feature no record specifies. A record sentence saying a country pack may add a national audit file specifies no feature, and the national audit file arrives as its own specified feature.
- The use-case code placement guide, the learn pages, the domain tutorial, and the Claimwick code example describe the Kernel, Platform, and one-project-per-module layout: aggregates under module `Domain/{Aggregates}/` in the plural, one plural aggregate folder with one folder per use case inside each layer that serves it, every port in its own `Ports/{Port}/` folder with its adapter in `Infrastructure/{Port}/`, repositories implemented in module `Infrastructure/Persistence/`, and one message-definition class per concern.
- A web use-case module sits at `lib/modules/{module}/{aggregates}/{use-case}/`, mirroring the backend `Application/{Aggregates}/{UseCase}/` folder; a module-level use case drops the aggregate segment in both trees.
- The slice layout and the `layer-projects` extension are removed: every module holds `Domain/`, `Application/`, `Ports/`, `Infrastructure/`, and `WebApi/` at its top, and the analyzers take a file's layer from its top folder.
- `model check` places backend code by the one module layout: a file's layer is its top folder in the module project, a use case is the first folder below `Application/` or `WebApi/` that holds files, tests mirror `{Module}/{Aggregates}/{UseCase}/`, and reactions, jobs, notifications, external systems and read models sit in their `Application/` folders. The `layer-projects` project-file keys are gone; the one-modules-project shape stays, and there a module's contracts stay a `Contracts/` folder inside the module folder.
- The model check, `rules`, and the analyzers attribute a file in a sibling Contracts project to its owner module; a nested `Contracts/` folder inside a module project reports `code-placement-drift`.
- LS2017 and the `LiteSpecLayer` MSBuild property are removed with the `layer-projects` extension; `None` no longer switches the package off for a project. LS2019 reports a use case folder naming a type of another use case folder across `Application/` and `WebApi/`; LS2025 reports an `Infrastructure/{Port}/` adapter naming a Domain type outside typed identifiers and shared-kernel values, or a WebApi type. LS2028 reports a public type in a module project outside its entry type and stays quiet in Contracts projects.
- The generator always emits the registry operation lookup in a module project and writes permissions and roles there; a host and the Kernel generate none of their own.
- Everything that belongs to one aggregate sits in a folder named for the aggregate in the plural, in every layer: records at `aggregates/{aggregates}/`, code at `Domain/{Aggregates}/`, `Application/{Aggregates}/{UseCase}/` and `WebApi/{Aggregates}/{UseCase}/`, ports at `Ports/{Port}/` with adapters at `Infrastructure/{Port}/`, acceptance tests at `{Module}/{Aggregates}/{UseCase}/`, and web use-case modules at `lib/modules/{module}/{aggregates}/{use-case}/`. A module-level use case drops the aggregate segment in every layer, and record ids stay singular.
- A singular folder makes a namespace such as `...Application.Expense.SubmitExpense` in which the C# name lookup finds the namespace before the `Expense` type, so every handler naming its aggregate fails with CS0118; the same smell exists in a singular `Domain/Expense/` holding the type `Expense`. The plural is computed from the name part of the aggregate id, in kebab case for records and the frontend and in PascalCase for .NET.
- `code-placement-drift` reports a use case file, test or web module outside its plural aggregate folder, a file directly in an aggregate folder, a use case folder that names no use case of its aggregate, a singular `Domain/` aggregate folder, and a namespace that names another path than the plural folder; each finding names the expected folder. `record-placement-invalid` reports an aggregate record folder that is not the plural of its id.
- `litespec new aggregate` writes the plural record folder, and `litespec rename` moves a renamed aggregate folder whole to the plural of the new name.
- LS2019, LS2025 and LS2026 take the use case from the first folder below `Application/` or `WebApi/` that holds files, so two use cases of one aggregate stay isolated from each other. LS1014 and LS1015 find the aggregate root by its `AggregateRoot` base type instead of by the folder name, so the plural `Domain/Expenses/` folder holding the root type `Expense` is accepted.
- The Claimwick example moves to the plural layout: `Domain/Expenses/` holding the `Expense` root, `Application/Expenses/SubmitExpense/`, `WebApi/Expenses/SubmitExpense/`, acceptance tests under `Spending/Expenses/SubmitExpense/`, and web modules under `lib/modules/spending/expenses/`.
- Every port of a module lives in its own `Ports/{Port}/` folder at the top of the module project, whether one use case or many call it, holding the interface `I{Port}` and only the types its signatures name, with no files directly in `Ports/`; each port has exactly one adapter folder in `Infrastructure/` with the same name, and `Infrastructure/` holds `Persistence/` and one folder per port it implements, nothing else. `Application/Ports/`, `Infrastructure/ExternalSystems/`, and the use-case adapter folders are gone.
- A port is an interface the module declares for something it does not compute in memory: an external system, a file format it reads or writes, a platform service the Platform does not already expose as an abstraction, or the clock. In-process logic, store reads, wrappers over Platform abstractions, duplicate interfaces over one service, permission constants, and domain exceptions and enums are not ports.
- `code-placement-drift` reports a file directly in `Ports/`, a port folder holding no interface or more than one, a port folder whose name is not its interface name without the `I`, a type in a port folder that the interface signatures do not name, a port interface or adapter outside its folder, a port with no adapter folder, and an adapter folder with no port. The `rules` command reports concern `ports` for files under `Ports/`.
- The analyzers read `Ports/` as a layer: LS2018 reports a Ports file naming an Application, Infrastructure or WebApi type, an ASP.NET Core type or `HttpContext`, and a WebApi file naming a Ports type; LS1001 reports a Domain file naming a Ports type. LS2025 runs on the adapter folders and reads "Adapter names a type outside its layer"; LS2019 treats ports and adapters as shared.
- "Port" comes from Alistair Cockburn's "Hexagonal Architecture" (2005), also called Ports and Adapters: the application talks to the outside only through purpose-named ports, and adapters convert between a port and a technology. Decision 0107 records the name, its sources and the alternatives weighed.
- Claimwick moves its Spending ports to `Ports/ReceiptPdf/`, `Ports/EmailMessenger/`, `Ports/EventBus/` and `Ports/PayoutBank/` with one adapter folder each, and replaces the `IExpiredExpenseSelector` store read with the `IExpenseRepository.SelectExpiredAsync` repository query.
- The Claimwick backend moves to the onion layout: use-case code in `Application/{Aggregates}/{UseCase}/` with endpoints in `WebApi/{Aggregates}/{UseCase}/`, the approval reminder in `Application/Notifications/ApprovalReminder/`, the reimbursed-expenses read model in `Application/ReadModels/ReimbursedExpenses/`, the receipt PDF renderer in `Infrastructure/ReceiptPdf/` with `IReceiptPdf` in `Ports/ReceiptPdf/`, the Spending contracts in `Claimwick.Spending.Contracts`, and Expense, Policy and ReminderPreference repositories in `Infrastructure/Persistence/`.

The new rules are `litespec/backend/dotnet/architecture#keep-business-values-in-the-kernel`, `litespec/backend/dotnet/architecture#keep-technical-capabilities-in-the-platform`, `litespec/backend/dotnet/architecture#register-each-module-through-its-entry-point`, `litespec/backend/dotnet/architecture#plug-modules-into-the-platform-through-contributions`, `litespec/backend/dotnet/architecture#hold-each-layer-in-its-own-folder`, `litespec/backend/dotnet/architecture#expose-only-contracts-and-the-entry-point`, `litespec/backend/architecture#produce-file-formats-in-infrastructure`, `litespec/backend/application#keep-use-case-folders-small`, `litespec/backend/application#declare-shared-access-conditions-as-composable-policies`, and `litespec/backend/dotnet/api#convert-money-with-the-platform-converter`, and `litespec/backend/application#declare-a-port-only-for-what-the-module-does-not-compute`. Decisions 0107, Kernel, Platform, and One Project per Module, and 0108, Composable Policies for Shared Access Conditions, record why.

### Part AD: The Aggregate Root as the Only Entry Point

- Code outside an aggregate reaches it only through its root's named factories and methods, passing plain inputs; a file outside the aggregate's `Domain/` folder constructs nothing from it except the root's input records and typed identifiers. LS1014 reports the construction.
- A child entity carries no public setter, mutating method, or constructor; LS1015 reports the surface. An aggregate references another only through its typed identifier; LS1016 reports a property typed as another aggregate's root or entity.
- A domain service holds only a calculation that spans aggregates with no owning aggregate; the root calls it inside its own method, or Application passes its plain result in as an input.
- A root past 500 lines splits into one partial file per business action (`{Root}.{Action}.cs`), with its rules in private members or private nested types.

The new rules are `litespec/backend/domain#reach-an-aggregate-only-through-its-root`, `litespec/backend/dotnet/domain#construct-aggregate-parts-only-inside-the-root`, `litespec/backend/dotnet/domain#reference-other-aggregates-by-typed-identifier` and `litespec/backend/dotnet/domain#split-a-large-root-into-one-file-per-action`. Decision 0104, Reach Each Aggregate Only Through Its Root, records why.

### Part AE: Each Aggregate Root Stored as Its Own Document

- Marten stores each aggregate root as its own document; a project writes no parallel document type for an aggregate. The Infrastructure store setup carries the alias, the typed-identifier mapping, the serializer with its converters and discriminator registrations, and optimistic concurrency, while Domain carries no serialization attribute. Query handlers query the aggregate type through `IQuerySession`.
- The shared-kernel `DomainException` takes only the message in its constructor and declares `Code` abstract, so the generator writes each refusal code as an override; LS5013 reports a base that declares a non-virtual `Code` instead of emitting hiding code.
- LS2018 names the fix when the Infrastructure type it reports is a Marten document of an aggregate: query the aggregate type through `IQuerySession`.

The new rule is `litespec/backend/dotnet/persistence#store-each-aggregate-root-as-its-own-document`. Decision 0105, Store Each Aggregate Root as Its Own Document, records why.

### Part AF: Class Design Rules and No Primary Constructors

- The workspace gains the Class Design page: one responsibility per class, one capability per set of interfaces, one class per pipeline concern, and composition over helper base classes. Each pipeline declaration class is named `{Message}{Concern}Definition`. LS2023 reports a class implementing two LiteBus message-definition contracts.
- Classes and structs avoid primary constructors and use explicit fields with an explicit constructor; positional records keep their form. Every .NET example is rewritten to the advised shape, and LS3005 reports a class or struct primary constructor at info level.

The new rules are `litespec/workspace/design#give-each-class-one-responsibility`, `litespec/workspace/design#implement-one-capability-per-class`, `litespec/workspace/design#declare-each-pipeline-concern-in-its-own-class`, `litespec/workspace/design#take-shared-behavior-through-composition`, `litespec/workspace/design#name-each-definition-after-its-message-and-concern` and `litespec/workspace/naming#avoid-primary-constructors-on-classes-and-structs`. Decision 0106, State Class Design Rules, records why.

### Part AG: Analyzer Rules for the Module Layout

- New Domain rules: LS1014 reports a file outside an aggregate's `Domain/` folder that constructs a type from that folder, the root's input records and typed identifiers aside; LS1015 reports a public setter, constructor, or mutable collection on a non-root aggregate type; LS1016 reports a property of one aggregate typed as another aggregate's root or entity.
- New layer rules: LS2023 reports a class implementing two LiteBus message definition contracts and names the split; LS2024 reports store, serializer, guard, or converter code inside a module that the platform provides; LS2025 reports a use case `Infrastructure/` folder naming a Domain or WebApi type; LS2026 reports a record declared with the same members in two use case folders; LS2027 reports archive, serialization, or document work in Application; LS2028 reports a public module type outside `Contracts/` and the entry type. LS3005 reports a class or struct primary constructor at info severity.
- The generator reports LS5013 and writes no part when a marked exception inherits a non-virtual `Code`; a base declaring an abstract or virtual `Code` keeps the generated override.
- The layer map recognizes both module shapes: one project per module (the project root is its own module) and one modules project. Slices accept `Infrastructure/` and later layer folders; a Kernel project reads as Domain, a Platform project as Infrastructure, and an `Api` host as WebApi.
- LS2018 allows Platform types and Infrastructure ports from Application and names the `IQuerySession` fix for stored aggregate documents; LS2020 stays as the backstop for one-modules-project solutions while visibility holds the boundary with one project per module; LS2021 and LS2022 exempt the Platform; message definition classes take no mark.
- LS1013 counts an arm that names a sealed case by type alone, such as `EntryKind.Asset => ...`, as covering that case, in switch expressions and switch statements. Cases grouped with `or` are covered the same way. A discard arm after every sealed case is handled reports nothing; the finding names only cases with no arm of their own.

### Part AH: The .NET Stack Binding, the Agent Protocol, and Hosted Sign-In

- The .NET pipeline binding names the LiteBus 7.2 APIs for every position: a Marten project stores outbox rows in its own session through `LiteBus.Outbox.Storage.Marten` (`outbox.UsePostgreSqlStorage` with `UseMartenTransactionalOutbox`), in-process dispatch runs through `outbox.UseInProcessDispatch()` with `outbox.EnableOutboxProcessor()`, delivery runs per handler, events stage through `outbox.StageCollectedEvents()`, and immediate dispatch runs through `outbox.EnableImmediateDispatch(...)`.
- Each message declares its pipeline positions in its use case `Application/` folder in `{Message}AuthorizationDefinition`, `{Message}AuditDefinition`, and `{Message}IdempotencyDefinition`, each in its own file and implementing exactly one of `IAuthorizationDefinition<TMessage>`, `IAuditDefinition<TCommand>`, and `IIdempotencyDefinition<TMessage>`; a definition carries no LiteSpec mark, and a project uses no attribute or custom guard for what a definition declares. An idempotency definition keys a command of mode `key` and exempts a command of mode `state` or `none`; `definition-drift` reports a keyed state command, whose stored key would outlive the state it mirrors.
- The manifest pins every LiteBus package at `7.2.0` and adds `LiteBus.Outbox.Storage.Marten`.
- The agent protocol points at the local docs of the carried release through `__LITESPEC_DOCS__` and tells the agent to read this release only, never substituting another version. The path is written relative to the project when the docs sit in the same repository, or under `~/` when they sit in the home folder, so a committed `AGENTS.md` matches on every machine.
- A marked external-system interface in a module `Infrastructure/` folder reads as a port, so module-shared ports keep their adapters and edges.
- `LS2018` lets a WebApi file name platform types, such as the endpoint contract, the session principal and the mediation settings, and `LS1014` reads an input record the root takes inside a list or from a sub-namespace of the aggregate.
- A hosted sign-in is modeled as use cases that start and complete the sign-in, with the provider as an external system declaring its authorize, token, and logout operations.
- An unreleased analyzer build is consumed through a local feed, never through a project reference.
- The complete-sign-in command of a hosted sign-in is the one command that declares a `calls` edge with `inline` timing, for the token exchange whose result the session needs; `effect-timing-invalid` exempts that edge.

The new rule is `litespec/core/access#model-a-hosted-sign-in-as-use-cases`. Decision 0103, Settle the .NET Stack Binding, Protocol, and Sign-In, records why.

### Part AI: Reference Data, Rule Sources, and Retention Holds

- Each rule states `source` as `law`, `contract`, or `product-choice`, and a rule with `source: law` names the restated constraint or research record in `cites`.
- A data class carries `holds` beside its period, each with its reason, its `heldOn` date, and its `liftedOn` date once lifted, and deletion pauses while a hold stands.
- `periodStartsAt` takes any declared event or aggregate date field, or an event or field with `rounding` to `financial-year-end` or `calendar-year-end`.
- A schedule `timezone` is a literal IANA zone, `{ setting: setting/<module>.<name> }`, or `{ field: aggregate/<module>.<aggregate>#<field> }` naming where the zone is stored.
- A schema error on a value matching no allowed shape reports only the selected shape's errors: the shape the value's `type` names, else the shape a value field names that exactly one shape declares, else the shape with the fewest errors. `litespec model check --verbose` keeps the full shape list.
- A schema error on a rejected field names the field to use instead, from the schema's `x-rejected-fields`.
- `model check` reports `reference-data-period-invalid` for rows of one reference-data table whose validity periods overlap, or whose `effectiveTo` is no later than `effectiveFrom`. (This line replaces the ls-records line that names `reference-data-overlap`; the shipped code is `reference-data-period-invalid` per `litespec/core/domain-model#keep-reference-data-rows-from-overlapping`.)
- The book lists reference-data tables under their module and shows each table's row count.

The new rules are `litespec/core/model#cite-reference-data-instead-of-literal-values`, `litespec/core/domain-model#state-each-rules-source`, `litespec/core/domain-model#name-the-record-each-law-rule-restates`, `litespec/core/privacy#pause-deletion-while-a-hold-stands`, `litespec/core/privacy#round-an-event-start-to-a-calendar-end`, `litespec/core/model#report-one-shape-in-schema-errors`, `litespec/core/model#name-the-field-to-use-instead-in-schema-errors`, and `litespec/ext/jobs#name-a-varying-time-zone-by-its-source`. Decisions 0098, Reference Data Tables, 0099, Rule Sources, and 0100, Retention Holds and Rounded Starts, record why.

### Part AJ: Rule Citation Checks Across Commands and Scenarios

- `model check` reports `rule-sibling-uncited` when a command writes an aggregate without citing an invariant most commands writing that aggregate cite, so one form can no longer skip the checks its siblings apply.
- `model check` reports `exercises-uncited` (level A) when a scenario exercises a rule its use case cites nowhere in its steps, inputs, or results.
- `model check` reports `refusal-next-step-unknown` (level A) when a refusal sentence names a use case, screen, or setting no record holds, or promises another way out with `instead`, `alternatively`, `you can also`, or `or you can`.
- `model check` reports `guard-module-foreign` when a guard reaches an aggregate of another module, whose rule cannot hold at commit; the guard provision now scopes guards to the command's own module.
- A rule governing another module's records without a declared dependency reports `link-locality-invalid` with a message naming the fix: the reached module states the check as its own rule at its boundary and names the governing rule in prose, while the owning module checks again when it acts.
- Edges and Screens documents how two modules keep one fact consistent (one owner, a module event, a read model, an agreement check) with a worked timestamp example, and states the boundary-rule pattern each module applies at its own edge.
- Claimwick's attach-receipt step cites the receipt-filed-once invariant its success scenario exercises.

The new rules are `litespec/core/use-cases#cite-the-invariants-most-writers-cite`, `litespec/core/use-cases#name-only-a-provided-way-out-in-each-refusal-sentence`, `litespec/core/edges#resolve-late-arrivals-against-owner-written-facts` and `litespec/core/edges#state-every-boundary-check-as-a-rule-of-its-module`. Decision 0101, Check Rule Citations Across Sibling Commands and Scenarios, records why.

### Part AK: Audit Chains a Checkpoint Can Prove and Retention Can Delete

- An audit checkpoint proves only the records it covers: verification detects any change to those records, while records written after it are covered from the next checkpoint, so the checkpoint interval bounds the window in which a rewrite goes unseen. Each checkpoint carries the chain head and its record count, and the adoption decision states the checkpoint interval as the detection window.
- An audit store holds one hash chain segment per audited category and calendar year, each with its own genesis record and checkpoints. A segment is closed with a final checkpoint and deleted as a whole after its category's retention ends, while the remaining segments still verify from their own anchors, and each deleted segment is recorded in the following segment of the same category.
- Retention deletion and tenant data erasure run through a separate audited operator process under its own account holding DELETE; the application account keeps only INSERT and SELECT, and the append-only and trail-store rules now name the operator account as the exception.

The new rules are `litespec/ext/audit#delete-expired-records-through-a-separate-operator-account`, `litespec/ext/audit#segment-the-audit-chain-by-category-and-calendar-year`, `litespec/ext/audit#record-each-segment-deletion-in-the-following-segment` and `litespec/ext/audit#state-the-checkpoint-interval-as-the-detection-window`. Decision 0097, One Audit Chain Segment per Category and Year, records why.

### Part AL: Command Line Messages That Name the Fix

- `model check` prints the `release-drift` finding before every other finding, naming the reviewed release, the installed release, and the command that installs the reviewed one, because every finding after it may not apply.
- A record file that fails to parse on a plain value holding `': '` now reports the file, the line, and the field, and says to quote the value or rephrase it without the colon. The glossary reports the same shape.
- `rules --since` orders its report skeleton by how many changed files each judgment provision governs, most first, and prints the count on each line; duplicates stay removed.
- `model render` writes `readonly offers?: readonly UseCaseId[]` on `LiteSpecMark`, and it writes nothing, lists each invalid file, and exits 2 when a record file does not parse, so a rendered identifier list is never quietly short.

### Part AM: Frontend Data, States, and Entry Pages

- The Claimwick web frontend wires one screen through the query cache end to end: the root layout builds the query client per request and provides it, the expense list prefetches in `+page.ts` and reads back the same key in the page, and the new-expense submit invalidates the list query.
- The Claimwick web frontend ships its `package.json`, `vite.config.ts`, and `tsconfig.json`, showing the import-resolution settings agreeing: the `imports` field as the one alias declaration, bundler resolution with extension imports, and the SvelteKit configuration in `vite.config.ts`. SvelteKit 3 reads no `svelte.config.js`, and the Svelte application tree no longer lists one.
- Two route state patterns join the feedback and states page: `route-loading-frame` renders skeletons within the floorplan, and `route-error-frame` renders the error state with retry and trace, with the bare status outside the floorplan kept as the recorded freestyle exception.
- Dialog, popover, and details composites build on their native elements; every other composite comes from the pinned baseline. Decision 0102, Draw Platform Composites with Native Elements, records why.
- The records-file rule states the bootstrap: `litespec model render` writes the file from the entry point's screens and use cases before any screen exists, reading every browser entry point for a frontend with no marked route yet.
- The page frames gain `entry-page`: a centered card with title, lead, note, and actions for sign-in and similar pages outside the app shell, with split and floating-card options.
- Every monetary amount on a frontend travels in the frame Money type with a decimal string amount and scaled-integer arithmetic, and the Claimwick frame holds it in `src/lib/app/money.ts`.

The new rules are `litespec/frontend/data#hold-every-monetary-amount-in-the-frame-money-type` and `litespec/frontend/web/ui#draw-platform-composites-with-native-elements`, and the new patterns are `litespec/frontend/patterns/page-frames#entry-page`, `litespec/frontend/patterns/feedback-and-states#route-loading-frame`, and `litespec/frontend/patterns/feedback-and-states#route-error-frame`. Decision 0102, Draw Platform Composites with Native Elements, records why.

### Part AN: Module Contracts, Public Queries and Commands, and One Direction per Relationship

- A module's Contracts project publishes data, never interfaces: one folder per aggregate holding the aggregate's identifier when another module carries it, its facts, and its public queries and commands at `{Aggregates}/{UseCase}/`, plus module-level public use cases at `{UseCase}/`. No `Identifiers/`, `ModuleEvents/` or `Ports/` folder and no interface remain in a Contracts project.
- A public query or public command is a use case whose record lists the modules that may send it in `reachedThrough`. The sender declares the edge as `effects.queries` or `effects.commands`; the owner handles it with its ordinary handler through its own pipeline. For now a public command travels in process and synchronously, like a public query.
- A consumer reaches another module through one port per provider, `Ports/{Provider}/I{Provider}.cs`, in its own types; its adapter in `Infrastructure/{Provider}/` is the only code that names the provider's queries, commands and results (LS2029).
- An identifier lives in `Contracts/{Aggregates}/` of the module that owns its aggregate when another module carries it, else in `Domain/{Aggregates}/`; the Kernel holds one only with a decision naming the Contracts cycle it breaks, and the Platform names no business identifier.
- Every relationship between two modules has one direction: commands and queries flow upstream, facts flow downstream, and an upstream module never queries, commands or reacts to a downstream module. A process spanning modules runs from its most downstream participant through the participants' public commands, and a downstream module reports finished work to an upstream module through that module's public command.
- `model check` reports `module-dependency-invalid` for a cycle counting Contracts references, `dependsOn`, `queries` and `commands` edges and reactions to facts; `query-target-invalid` and `command-target-invalid` for an edge to a use case not reached through the sender; `query-consumer-missing` and `command-consumer-missing` for a public use case no module sends; and `code-placement-drift` for a command or query in a Contracts project that no public use case owns.
- Decisions 0109, Module Contracts Publish Data, and 0110, Module Dependencies Form a DAG, record why, with the sources: Enterprise Integration Patterns, NServiceBus on commands and events, message ownership, orchestrated sagas, and Spring Modulith's dependency verification.
- An event type ends in the suffix of its kind: `DomainEvent` for a domain event in `Domain/`, `ModuleEvent` for a module event in Contracts, and `IntegrationEvent` for an integration event in Contracts. A bare `Event` suffix or no suffix is reported by the new analyzer rule LS2031, and reaction handlers keep `{Act}On{Fact}Handler` with the fact stem only.

### Part AO: Explanations on Every Listed Layer

- A layer listed in `explanations.layers` requires an explanation file for every record on it, requirements included; the exemption for requirements is removed, and `litespec new requirement` writes the explanation from `templates/consumer/requirement.md`.
- `governance` is a valid entry in `explanations.layers`, so a project can require an explanation for each decision, convention and waiver.

### Part AP: LiteBus 8.0.0-preview.3 Binding

- Every `LiteBus.*` package is pinned at `8.0.0-preview.3`. The manifest adds `LiteBus.Auditing`, `LiteBus.Auditing.Abstractions`, `LiteBus.Auditing.Storage.Marten` and `LiteBus.Extensions.AspNetCore`, which carry the audit chain and the ASP.NET Core problem details.
- Domain events reach the outbox by harvesting: `outbox.HarvestDomainEvents(...)` names the aggregate root base, the Marten harvester is registered on the storage builder, and each domain event type takes one decision (`TranslateDomainEvent`, `IgnoreDomainEvent`) with `RequireDomainEventDecision()`. Hand collection stays for a deletion by predicate or by id, a carrier type that is never stored, and an event raised outside an aggregate.
- The authorization guard derives from `AuthorizationGuard<TMessage>`, the audit chain is configured through `AddAuditing` with `UseMartenStore` and `UseHashChain`, and problem responses map through `AddLiteBusExceptionHandler` and `IRefusalProblemCatalog`.
- The nested-command rule turns on with `litebus_forbid_nested_commands`, which enables `LB1023`.
- The audit trail guide uses the preview.3 audit definitions, the `MessageErrorContext` error handler signature and the preview.3 documentation link.

### Part AQ: Reactions Filed with the Sending Module

- A reaction or job that changes another module only by sending that module's public command lives with the sending module. `litespec/core/automation#file-every-reaction-and-job-with-the-module-it-changes` names both places a reaction or job may sit.

### Part AR: Provider Calls Inline Only from Queries

- `litespec/ext/integrations#run-provider-calls-from-event-handlers-or-jobs` allows a query to call a provider inline, and states that a command never calls a provider before its commit.

### Part AS: Keys, Module Events, Enforcing Jobs and Module Boundaries

- `module-reference-undeclared` (level G) reports an `input`, `output`, or `steps` entry of a use case that names another module's record without a declared dependency, under `litespec/core/model#declare-cross-module-references-in-depends-on`.
- `tenant-setting-change-missing` (level G) reports a `setBy: tenant` setting that no `change-*` use case changes, under `litespec/core/use-cases#give-every-tenant-setting-a-change-use-case`; the use-cases prose now states what the schema admits for `setBy`.
- `on-failure-placement-invalid` (level G) reports failure handling stated in a reaction's or job's effect entry; the record's `onFailure` is its one home under `litespec/core/automation#keep-failure-handling-on-the-reaction-or-job`.
- `reference-data-pack-missing` (level G) reports a reference-data table naming an external pack file that does not exist, under `litespec/core/domain-model#bind-a-reference-data-table-to-an-external-pack`.
- `term-missing` no longer reads phrases built from a reaction title, because the title is assembled from the trigger and use case names.
- `computedBy` now counts as a calculation link in the rule reach, so a calculation rule cited only by a use case's `computedBy` governs that use case without `rule-governs-drift`.
- `idempotency-key-uncarried` (level G) reports a key placeholder that no input of a use case, field of a reaction's triggering event, or input of a job's run use case carries, under `litespec/core/use-cases#carry-every-key-placeholder`.
- `module-event-unused` (level G) reports a module event named by no reaction, read model projection or other consumer, under `litespec/core/automation#subscribe-to-every-module-event`.
- `enforcement-job-unreached` (level G) reports a data class naming an enforcement job that runs or sends no use case writing the aggregate holding the class, under `litespec/core/privacy#enforce-a-data-class-through-its-aggregate`.
- `scenario-result-unknown` (level G) reports a scenario ending in a result id its use case declares neither as a written result nor as a derived refusal, under `litespec/core/use-cases#declare-the-result-a-scenario-ends-in`.
- The frontend reader skips the client generator's bundled runtime under `client/` and `core/` when discovering operations, so a screen that builds its client with `createClient` or calls `buildUrl` is no longer reported as an unrecorded operation.

### Part AT: The AppHost in the Deploy Folder

- An AppHost lives at `deploy/{Product}.AppHost/` at the workspace root, whatever it runs. `apps/` holds only the applications that ship, and the AppHost, which models how they run together locally and in the deployment files it publishes, sits beside the rest of the deployment description.
- The workspace tree gains `deploy/` and loses `apps/apphost/`.

The new rule is `litespec/backend/dotnet/workspace#keep-the-apphost-in-the-deploy-folder`, which replaces `litespec/backend/dotnet/workspace#place-a-workspace-apphost-beside-the-applications-it-runs`.

### Part AU: The Cast as a Record

- The example cast is a record: `.litespec/cast.yaml` (`type: cast`) with typed `parties`, `people`, `occasions` and `figures`, and an optional `cast.md`. A figure's value is one of `money`, `quantity`, `percentage`, `date`, `count` or `text`. People name the actor they play, and facts name glossary terms; both links are checked.
- The `example-cast` document style, `example-cast.md`, and its page-sections check are removed. `litespec init` writes `cast.yaml`.
- `cast-identity-unreserved` (level G) reports a cast email outside the reserved test domains; `cast-entry-unused` (level A) reports an entry nothing cites.
- Seed and fixture code marks the cast entries it uses with `<seeds>` (C#) or `// litespec: seeds` (TypeScript); an unknown id is `mark-unknown`.
- Cast names and values may appear in development seed data, demo data, fixtures and tests, never in production code paths or production data.

The new rule is `litespec/core/writing#keep-the-cast-out-of-production-code`. Decision 0112, Cast as a Record, records why.

### Part AV: Local Development and Development Shortcuts

- A new page, Local Development (`docs/quality/development.md`), holds `start-the-stack-with-one-local-command`, moved from Operations.
- A development shortcut (a sign-in shortcut, a fake external system, a seed or reset command, a relaxed guard) is excluded from release builds at compile time, refuses to run outside development, and carries the development-shortcut mark. On .NET it lives in a project referenced only in Debug, registers behind `#if DEBUG`, and carries `<development-shortcut>`; analyzer LS5014 reports one compiled without `DEBUG`.
- Five defaults: authenticate as a cast person through the product's own sign-in use case; seed from the cast through public use cases; provide seed and reset from the local orchestrator; keep the real identity provider one launch profile away; give each external system a stand-in that answers its declared operations.

The new rules are `litespec/quality/development#keep-development-shortcuts-out-of-release-builds` and `litespec/backend/dotnet/workspace#exclude-development-shortcuts-from-release-builds`. Decision 0113, Shortcuts Stay Out of Release Builds, records why.

### Part AW: Schema and Stored-Document Changes on Marten

- Outside development, schema objects change only in a migration step that runs before the hosts start and applies the difference with `CreateOrUpdate`; `AutoCreate.All`, which drops and recreates tables, never runs against a database holding data, and the hosts run with `AutoCreate.None`.
- By default the migration step prints the planned SQL, refuses destructive statements without an explicit allowance, records each run in a ledger with the build's schema fingerprint, and readiness requires that fingerprint.
- By default each stored document carries its contract version and upgrade steps run on the JSON when it is read, so an append-only or hash-chained record is never rewritten on disk.

The new rules are `litespec/backend/dotnet/persistence#apply-schema-changes-in-a-migration-step`, `litespec/backend/dotnet/persistence#plan-each-schema-change-before-applying-it` and `litespec/backend/dotnet/persistence#evolve-stored-documents-when-they-are-read`.

### Part AX: Consumers Stay Out of LiteSpec

- LiteSpec names no consumer project and carries no consumer's domain vocabulary, records, flows, figures, people or products in any page, provision, example, fixture, template, decision, changelog entry or tool message; examples come from Claimwick or the library product.
- A change prompted by one consumer states the general need behind it and is designed and checked against products unlike that consumer; a need that holds for one consumer alone stays in that consumer as a project convention or waiver.
- `AGENTS.md` gains a Serving Many Consumers section stating both.

The new rules are `litespec/core/maintaining#keep-consumers-out-of-litespec` and `litespec/core/maintaining#derive-a-general-rule-from-a-consumer-need`.

### Part AY: One Condition per Rule and Record Field Order

- Each rule states one condition. Its first sentence states the condition, and further sentences only narrow it. A case with its own refusal, derived value, dates, or scenario is a rule of its own.
- A rule statement stays within 600 characters. `litespec model check` reports `rule-statement-wide` at level A past that length, without blocking.
- A rule cites the reference-data table that holds a rate, threshold, or deadline that varies by date, jurisdiction, or tenant, rather than writing the value into its statement.
- A decision's `context` comes right after `kind`, before `options`, so `litespec model format` writes decisions in the order they are read.
- A cast occasion's date is `date`, and a conflict's subject is `subject`; the field `on`, which YAML 1.1 readers take for a boolean, is gone from both.

The new rules are `litespec/core/domain-model#state-one-condition-per-rule`, `litespec/core/domain-model#keep-each-rule-statement-short` and `litespec/core/domain-model#cite-varying-values-from-reference-data`. Decision 0114, State One Condition per Rule, records why.

### Part AZ: One Folder per Rule, Setting and Domain Event, and the Design Rules

- Each rule is a record file in its own folder: `rules/<name>/<name>.yaml` under its aggregate, under its module, or under `behavior/` for a product invariant, with its explanation `<name>.md` beside it. The `rule-set` page style is gone.
- Each setting is a record file at `behavior/modules/<m>/settings/<name>/<name>.yaml`. The `setting-set` page style is gone.
- Each domain event is a record file at `behavior/modules/<m>/aggregates/<aggregates>/domain-events/<name>/<name>.yaml`. An aggregate file holds no domain events.
- A rule, a setting and a domain event carry no `summary`: a rule opens with its statement, the others with their meaning.
- A rule's explanation holds Rationale, then an optional Example and an optional Limits. A setting's holds Defaults and an optional Changes. A domain event's holds an optional Example.
- `litespec new rule`, `litespec new setting` and `litespec new domain-event` write the folder and its files. `litespec new entry` takes permissions only.
- `litespec/core/model#hold-module-rules-in-rule-sets` is now `litespec/core/model#hold-module-rules-in-rule-folders`.
- The page Designing the Standard states the design rules for LiteSpec itself, and the agent protocol summarizes them. The page lists the design provisions other pages own. Its new provisions are:
  - `litespec/core/designing#hold-one-thing-in-each-record`
  - `litespec/core/designing#give-a-record-with-its-own-reasoning-its-own-folder`
  - `litespec/core/designing#shape-each-record-type-for-its-next-field`
  - `litespec/core/designing#give-every-type-field-and-value-a-reader`
  - `litespec/core/designing#deliver-each-record-type-complete`
  - `litespec/core/designing#change-every-instance-of-a-shape-in-one-change`
  - `litespec/core/designing#state-each-deviation-from-a-design-rule`
  - `litespec/core/designing#update-a-design-rule-that-keeps-bending`

Upgrade: move each rule, setting and domain event into its own folder.
1. For each entry under `records:` in a rule-set or setting-set file, and each domain-event entry in an `aggregate.yaml`, write the entry's fields at the top level of `<name>/<name>.yaml`. The name is the last part of the identifier.
2. Move each rule's section of the old set explanation into that rule's `<name>.md`, under `## Rationale`.
3. Delete the set files.

Decision 0115, Give Reasoned Records Their Own Folder, and decision 0116, Write the Design Rules Down, record why.

### Part BA: Screens Composed from Checked Structure

- `docs/frontend/ui.md` gains fifteen required composition standards, each applying to artifact `route`: no surface inside another, every field inside a section, dialog or side sheet, division past the section limit, tabs for peer views only, grouped long forms, one primary action per view named in `primaryAction`, hub frames for screens past the offer limit, `pageFrame` on every screen with a route, widths by value kind, one size per action group, one composite per role, names never codes, end-aligned numbers, alike destructive commands, and a review at phone, laptop and wide widths before done.
- The screen record requires `pageFrame` with a route and gains optional `primaryAction` naming an offered use case, and `.litespec/project.yaml` gains optional `ui.limits` with `sections` (default 3), `formFields` (default 6) and `offers` (default 4). The screen blueprint check reports `wireframe-nesting-invalid`, `wireframe-surface-missing`, `wireframe-tabs-missing`, `wireframe-group-missing`, `screen-primary-action-unknown`, `screen-pageframe-invalid` and `screen-pageframe-missing`, and the whole view holds at most one primary action.
- The token roles `card` and `card-edge` are renamed to `layer-1` and `layer-edge` with no alias, joined by `layer-2` and `layer-accent`: `layer-1` sits on `page`, `layer-2` sits on `layer-1`, and `layer-accent` fills a table header, a field and a selected row on whichever layer holds them. Each layer differs from the surface it sits on by at least 1.06:1 relative luminance in every declared scheme.
- `litespec ui audit` opens each route in a browser at the given widths and schemes and reports `surface-nesting-invalid`, `surface-contrast-failed`, `field-surface-missing`, `field-width-invalid`, `action-group-invalid`, `primary-action-duplicate`, `column-alignment-invalid`, `text-code-invalid`, `tab-label-invalid` and `table-clip-invalid`. The web route-suite section points at the audit and lists its codes.
- The rendered-page audit also reports `page-request-failed`, when a fetch or XHR request the page makes to its own origin answers 5xx or fails at the network level, and `page-error-shown`, when a visible alert holds text once the page settles. The finding names the request method and the path without its query string.
- The pattern pages draw every toolbar inside its section, one primary action per view, and no card inside a region, so the taught shapes agree with the rules.

The new rules are `litespec/frontend/ui#place-no-surface-inside-another`, `litespec/frontend/ui#draw-every-field-on-a-surface`, `litespec/frontend/ui#limit-a-page-to-three-stacked-sections`, `litespec/frontend/ui#use-tabs-only-for-peer-views`, `litespec/frontend/ui#group-a-long-form`, `litespec/frontend/ui#show-one-primary-action-per-view`, `litespec/frontend/ui#route-a-busy-screen-through-a-hub-frame`, `litespec/frontend/ui#name-the-page-frame-on-every-screen`, `litespec/frontend/ui#match-each-field-width-to-its-value`, `litespec/frontend/ui#keep-one-action-group-to-one-size`, `litespec/frontend/ui#draw-each-role-with-one-composite`, `litespec/frontend/ui#show-names-never-codes`, `litespec/frontend/ui#align-numbers-to-the-end`, `litespec/frontend/ui#mark-every-destructive-command-alike` and `litespec/frontend/ui#review-each-screen-at-three-widths`. Decisions 0117, Compose Screens from Checked Structure, 0118, Layer Surfaces by Role, and 0119, Audit the Rendered Page, record why.

### Part BB: Seeding from Scenarios on the Cast Day

- The local stack runs on the cast's `today`: the orchestrator sets a development clock, every host reads it through the scoped clock, and the frontend marks the displayed date as a development date. With no `today` in the cast, the real clock runs.
- The seed is split into named scenarios: each names the cast occasion it brings about, the scenarios it depends on, and how it tells it already ran, and skips when it has. A baseline scenario sets up the parties, and the orchestrator offers each scenario and all of them.
- A seed step that needs a value the cast lacks adds the fact to the cast and reads it from there; the seed invents no name, amount, or date.
- Volume data for paging, search, and speed comes from a fixed random seed, lives in its own scenario, and is marked as generated, so it reads as load data rather than as an example the specification states.
- The screen review and the rendered-page audit run on a stack seeded with every scenario, and every screen in the model is reached with data by at least one scenario.
- Seed and reset commands refuse to run in production; production reference data (charts, rates, country data) arrives only through migrations or startup data that is part of the release.

The new rule is `litespec/quality/development#seed-only-development-and-test-environments`.

### Part BC: Checks for What Only Fails at Runtime

Two parts that are each correct alone and wrong together now meet in a check before anything runs.

- A domain event nothing answers states `ignored: true`. `domain-event-outcome-missing` reports an event with no reaction, no translation and no `ignored`, and `domain-event-outcome-drift` an ignored event something consumes. With the outbox extension, `outbox-decision-missing` reports a domain event type with no outbox decision in code, and `outbox-decision-drift` a code decision that contradicts the records.
- A use case that a reaction or job runs admits the system identity: its grant is `system` or `anonymous`, or its access states `runsAs: system` beside the person grant. `system-access-missing` reports one that does not.
- `caller-identifier-unreachable` reports a required caller-supplied identifier that no query through the same entry point returns, and `module-identifier-unreachable` advises the same for a use case no entry point reaches.
- A screen names each field it shows in `fieldSources`, with the offered use case it comes from. `screen-shows-unknown` reports a field no offered use case outputs or accepts, and `screen-shows-drift` one that matches only by name.
- A reaction or job that runs a use case writing an aggregate names the pending, done and refused states in `settles`. `async-settles-missing` advises on a run that names no `settles`, and `async-pending-missing` and `async-refused-unrecorded` report a `settles` whose states a reader cannot tell apart.
- `idempotency-replay-drift` reports a keyed command whose idempotency declaration replays differently from its record; a keyed declaration that omits the flag replays nothing.
- A finding whose neglect fails at runtime blocks. `idempotency-replay-missing`, `refusal-sentence-missing`, `sentence-language-missing`, `setting-unit-missing`, `template-value-unchanged` and `field-extension-unselected` move from level A to level G.
- Reference data is domain. A table lists the closed values of a meaning column in `choices`, the roles its rules read in `requiredRoles`, and per row a `key`, a `role` and a `refersTo` cross-reference. `reference-data-value-invalid`, `reference-data-key-unknown`, `reference-data-role-missing` and `reference-data-coverage-missing` report a value outside its set, a dangling reference, a role no row plays and dates no row covers. An amount field states its `sign`, and `amount-sign-drift` advises when two use cases computing one value disagree.
- Seven analyzer rules report what a build cannot see and a running store refuses: LS2032 a typed-identifier collection in a queryable `Contains`, LS2033 `LoadManyAsync` on a document keyed by a typed identifier, LS2037 a document's own `Id.Value` read inside a query, LS2034 a mediator call in an event handler without explicit mediation settings, LS2035 a generic exception thrown after a load finds nothing, LS2036 the system clock read outside Domain, and LS1017 a validated value type with no non-throwing parse.
- `litespec api explore` walks a running API from its OpenAPI document with a fixed seed, reuses the identifiers earlier answers return, and reports every 5xx, every refusal that is not a problem detail, and every timeout. It refuses a host that is not loopback unless `--allow-remote` is given.
- Backend pages gain one serializer configuration for every persisted payload, startup checks for durable contracts, outbox decisions and reference data, one exception-to-status map, cross-module references as contract types, and stored states the reader sees. Development pages gain seed read-back against the cast, the API walker after every boundary change, and failure causes shown in development. The agent page requires exercising each changed use case through the running stack before reporting done, and writing only rules the agent has seen hold.

The new rules are `litespec/ext/outbox#decide-every-domain-event-outcome`, `litespec/core/access#admit-the-system-identity-on-every-system-run-use-case`, `litespec/core/use-cases#return-every-caller-supplied-identifier-from-a-reachable-query`, `litespec/frontend/ui#show-only-fields-an-offered-use-case-outputs-or-accepts`, `litespec/frontend/ui#cover-each-screen-scope-with-its-read`, `litespec/core/automation#settle-every-async-outcome-in-visible-states`, `litespec/core/model#block-every-finding-that-predicts-a-runtime-failure`, the reference-data rules on `litespec/core/domain-model` and `litespec/backend/knowledge`, `litespec/backend/dotnet/domain#offer-a-non-throwing-parse-on-validated-value-types`, and the backend, development and agent rules named above.

### Part BD: The Knowledge Layer

Rates, limits, deadlines, templates and treatments that vary by a dimension form a knowledge layer beside the modules: Fowler's knowledge level and Evans' Knowledge Level.

- A knowledge area has one knowledge model (rows, closed sets, roles, typed keys, policy methods, the set contract, the runtime that answers lookups by role and date, startup validation) and one knowledge set per variant, holding values only. An area that varies by nothing keeps its one table in the model.
- Knowledge lives in the backend language when only the backend reads it and it ships with the release; a data format is the exception, for editors who are not developers, changes without a release, or several readers.
- Every row is dated and sourced, released rows are append-only, a set carries a version, and operational records keep the key, value and version they used. A missing rule refuses with a reason.
- Templates seed operational records once; per-tenant overrides are settings on an operational aggregate, and catalogs of outside systems are adapter configuration.
- In .NET, `src/Knowledge/<Product>.<Area>/` holds the model and `src/Knowledge/<Product>.<Area>.<Variant>/` each set, with slice folders by subject. Modules reference the model, a set references only its model and the Kernel, and only hosts reference sets. `knowledge-reference-invalid` reports a project reference that crosses this, and `code-placement-drift` a folder under `src/Knowledge/` that is neither.
- LS2038 reports a module naming a knowledge set type, LS2039 logic inside a knowledge set, and LS2040 a knowledge key built from a string literal outside a set.
- A knowledge area's records live at `.litespec/behavior/knowledge/<area>/` with a `knowledge-area` record naming its `variants`; its reference-data tables state their `variation` dimension and each row its `variant`. `knowledge-variation-undeclared` reports a table with variant rows and no dimension, and `knowledge-set-missing` a variant with no rows.
- A knowledge lookup that finds no rule refuses with the derived generic result `rule-not-stated` (409), beside `not-found` and `conflict`: a use case gets it when an input takes the product's rule-not-stated rule or a rule it takes cites a knowledge-area table.
- Claimwick gains the `travel` knowledge area: per-diem and mileage rates for Germany and the Netherlands, in records and in code.
- Four reference-data provisions move from `litespec/core/domain-model` to `litespec/backend/knowledge`: model reference data as domain values, name a role not a row, validate reference data before serving, and use an aggregate for edited reference data only.

The new pages are `docs/backend/knowledge.md` and the knowledge sections of `docs/backend/dotnet/architecture.md`.

### Part BE: Code That Follows the Records Exactly

Checks now compare code with the record field by field, generated code replaces typed copies of record facts, and every finding in `model check` blocks.

- `input-field-drift` and `output-field-drift` (level S) compare each use case's message and result type with its `input` and `output` fields. `refusal-undeclared` (level S) reports a refusal code a handler, a root method it calls or a one-hop helper throws while its use case declares no result for it.
- `logic-duplicate` (level S) reports a method body of at least 6 statements or 80 tokens carried in two or more folders, under the new `litespec/core/domain-model#keep-one-home-for-each-rule-calculation-and-projection` and `litespec/backend/dotnet/application#call-shared-logic-instead-of-copying-it`.
- `litespec generate` writes refusal codes, permission ids, domain event decisions and closed value sets from the records, configured by a new `generate:` list in `.litespec/project.yaml`. `generated-drift` (level S) reports a generated file that is missing or stale (`litespec/core/model#generate-record-determined-code-from-the-records`). Claimwick's exceptions and authorization definitions read the generated constants.
- A frontend may name its generated API client with `client: { contract, generated }`; the generator writes `contract.sha256`, and `client-drift` (level S) reports a client generated from another contract (`litespec/frontend/data#regenerate-the-client-with-the-contract`).
- The advisory level is gone. Every former level A code blocks at G or S, except the prose and tidiness codes, which move to the new level R that only `litespec model review` reports, never blocking. `async-settles-missing` is removed; a declared `settles` that is wrong still blocks.
- `litespec rules` prints the contract card of the use case, reaction or job a file implements, the judgement provisions, and one line counting the checked ones; `--all` lists every checked provision.
- `toolchain-drift` (level G) reports a consumer whose tool manifest, analyzer reference and running CLI name different LiteSpec builds (`litespec/core/release#run-one-litespec-build`). `litespec-maintain publish-local` packs the checkout's CLI and analyzers into a consumer's local feed and moves both pins to that build.
- The analyzers treat a knowledge model as its own layer: it may reference dependency injection, logging and options for its composition and startup validation, and LS1005 does not apply there, while the domain value rules still do. Only an assembly under `Knowledge/` that references its model counts as a knowledge set, so LS2038 no longer reports third-party packages.
- New agent provisions: `litespec/core/agent#raise-a-spec-gap-instead-of-bending-the-code`, `litespec/core/agent#check-your-own-change-before-handing-it-back` and `litespec/core/agent#follow-the-record-not-the-neighbour`, and for maintainers `litespec/core/maintaining#model-a-reported-spec-gap-before-more-code`.

### Part BF: Records That Determine the Code

Records now hold every fact the structure of a slice depends on, and `litespec generate` writes that structure, so hand-written code is left to logic, adapters and screens.

- Field facts: each input and output field maps to one code type per stack (typed identifier from `identifies`, closed set through `set`, money with its currency, decimal `scale`, text `maxLength`, list rows, nullability from `required`), with the mapping tables on the .NET stack page and the web data page, and carries machine-readable constraints (`minLength`, `maxLength`, `range`, `pattern`, `set`). The field-drift checks compare exact types when a field states them.
- Access facts: `access` states `recentSignIn`, `scope: self` with its `identityInput`, and `preconditions`, each checked.
- Audit and HTTP facts: `audit.category` and `audit.target`; an `http` block with method, route and a `binding` per input for use cases reached through an api or browser entry point; a `status` on each refusal cited over HTTP; eight checks hold them.
- New generator kinds: `messages` and `results` (partial records from `input` and `output`), `declarations` (one partial declaration catalog per module for authorization, audit and idempotency), `domain-events` and `module-events`, `exceptions` (one partial exception per refusal), `validators` (rules from field constraints with a hook for hand-written checks), `endpoints` (one partial route map per module, with an opt-out for special endpoints), `knowledge-sets` (set tables from reference-data rows, so the record stays the one source of each value), and `closed-sets` as full types (abstract record, sealed cases, `Code`, `FromCode`, `TryFromCode`, `All`). Every generated type is partial; hand-written members live in a sibling file. Claimwick adopts each kind.
- Generator safety: every renderer ships with golden-file tests, each generated header names the LiteSpec version, and a target inside a frontend's client output folder or two targets writing one file is a configuration error.
- The .NET backend reader builds a Roslyn compilation when the consumer has build output (semantic mode) and reads constants, typed identifiers and calls through symbols; it falls back to syntax-only reading otherwise, and `model check` names the mode that ran. The native tool keeps semantic mode.
- Each green release-branch build publishes the CLI and analyzers to GitHub Packages as `<version>-dev.<run>`; `litespec-maintain publish-local --from-ci <run>` pins a consumer to one, so its CI restores the build its developers use.
- `audit.target` also takes a list of inputs forming a composite target with its `kind`, or `from: output` naming the output field that carries a target the handler determines.
- An input bound from the route may name its route parameter (`routeParameter`) when the URL names it apart from the input.
- A refusal reporting an upstream system that refuses or is unreachable answers 503; 500 stays out of refusal statuses, since a server fault is never a refusal.
- The `declarations` generator resolves each message type with its namespace and members from the backend source; a use case whose message, key placeholder or audit target resolves to nothing is skipped with the reason, and idempotency keys render culture-invariant with dates round-trip.
- Knowledge-set rows build through the row constructor unless the table names a factory, and reference-data columns render `decimal` values, nested closed-set cases (`case`) and nested objects from sub-columns fed by extra row fields.

### Part U: Provision Changes Since v6.0.0

Provisions whose rule changed take the identifier their heading derives:

- `litespec/core/principles#state-transaction-sharing-on-every-inline-edge` is now `litespec/core/principles#share-no-transaction-across-an-edge`.
- `litespec/core/principles#confine-atomic-consistency-to-synchronous-edges` is now `litespec/core/principles#confine-atomic-consistency-to-one-use-case`.
- `litespec/core/use-cases#state-the-transaction-sharing-of-every-inline-call` is now `litespec/core/use-cases#run-follow-on-work-only-from-reactions-and-jobs`.
- `litespec/workspace/naming#group-event-handlers-by-the-fact-they-handle` is now `litespec/workspace/naming#place-each-trigger-with-what-it-runs`.
- `litespec/core/edges#publish-every-outbound-integration-event-from-effects` is now `litespec/core/edges#publish-every-outbound-integration-event-from-the-event-it-translates`.
- `litespec/backend/domain#place-every-constituent-type-in-its-aggregates-folder` is now `litespec/backend/domain#place-every-part-type-in-its-aggregates-folder`.
- `litespec/core/edges#describe-every-integration-event-for-its-direction` is now `litespec/core/edges#describe-every-integration-event-the-product-sends-or-receives`.
- `litespec/core/intent#state-each-vision-in-its-fields` is now `litespec/core/intent#state-each-direction-in-its-statement`, and the other three vision provisions name directions: `litespec/core/intent#keep-dates-and-priorities-out-of-directions`, `litespec/core/intent#build-nothing-from-a-direction` and `litespec/core/intent#cite-the-direction-a-decision-keeps-open`.
- Every provision naming journeys now names flows, and every provision naming the view record now names read models, as the generated list below shows.
- Every provision of `litespec/core/departures` moves to `litespec/core/waivers` with "departure" read as "waiver", `litespec/core/scopes#depart-from-a-rule-only-through-a-recorded-departure` is now `litespec/core/scopes#set-a-rule-aside-only-through-a-recorded-waiver`, and `litespec/core/departures#depart-from-advice-without-a-record` is now `litespec/core/waivers#deviate-from-advice-without-a-record`.
- `litespec/core/writing#interpret-fields-in-explanations-never-restate-them` is now `litespec/core/writing#explain-the-same-specification-the-records-state`.
- `litespec/core/access#authenticate-every-caller-through-a-declared-auth-kind` is now `litespec/core/access#authenticate-every-caller-through-a-declared-authentication-kind`.
- `litespec/core/access#require-a-recent-sign-in-by-setting` is now `litespec/core/access#limit-the-age-of-a-sign-in-by-a-setting`.
- `litespec/core/edges#give-every-screen-an-address-or-a-host` is now `litespec/core/edges#give-every-screen-a-route-or-a-host`.

<!-- generated:provision-diff -->
### Provision Changes Since v6.0.0

Added (356):

- `litespec/backend/api#map-every-failure-to-its-declared-status`
- `litespec/backend/application#ask-another-module-one-question-through-a-decision-port`
- `litespec/backend/application#confirm-downstream-completion-through-an-upstream-command`
- `litespec/backend/application#declare-a-port-only-for-what-the-module-does-not-compute`
- `litespec/backend/application#declare-every-commands-edge-on-the-sender`
- `litespec/backend/application#declare-every-port-in-the-module-ports-folder`
- `litespec/backend/application#declare-shared-access-conditions-as-composable-policies`
- `litespec/backend/application#keep-use-case-folders-small`
- `litespec/backend/application#read-another-modules-state-through-a-read-model`
- `litespec/backend/application#send-cross-module-queries-and-commands-through-the-mediator`
- `litespec/backend/application#send-public-commands-in-process`
- `litespec/backend/application#stage-declared-module-and-integration-events-in-the-commit`
- `litespec/backend/application#write-only-aggregates-the-record-names`
- `litespec/backend/architecture#carry-cross-module-text-references-as-contract-types`
- `litespec/backend/architecture#give-each-use-case-its-own-models`
- `litespec/backend/architecture#give-every-module-relationship-one-direction`
- `litespec/backend/architecture#group-a-folder-that-holds-past-twenty-files`
- `litespec/backend/architecture#merge-modules-that-need-each-others-model`
- `litespec/backend/architecture#produce-file-formats-in-infrastructure`
- `litespec/backend/architecture#repeat-coordination-never-a-rule`
- `litespec/backend/architecture#run-every-spanning-process-from-its-most-downstream-participant`
- `litespec/backend/architecture#send-commands-and-queries-upstream`
- `litespec/backend/architecture#subscribe-across-modules-only-to-module-events`
- `litespec/backend/domain#change-each-aggregate-through-its-marked-act`
- `litespec/backend/domain#name-domain-types-from-their-aggregate-root`
- `litespec/backend/domain#place-every-part-type-in-its-aggregates-folder`
- `litespec/backend/domain#reach-an-aggregate-only-through-its-root`
- `litespec/backend/domain#store-each-state-the-reader-sees`
- `litespec/backend/dotnet/api#compose-the-host-in-programcs`
- `litespec/backend/dotnet/api#convert-money-with-the-platform-converter`
- `litespec/backend/dotnet/api#map-failures-to-statuses-in-the-global-exception-handler`
- `litespec/backend/dotnet/application#call-shared-logic-instead-of-copying-it`
- `litespec/backend/dotnet/application#dispatch-read-model-projections-immediately`
- `litespec/backend/dotnet/application#keep-query-handlers-free-of-repositories-and-writes`
- `litespec/backend/dotnet/application#name-every-outbox-subscriber`
- `litespec/backend/dotnet/application#pass-explicit-mediation-settings-to-nested-use-cases`
- `litespec/backend/dotnet/application#report-immediate-dispatch-beside-the-backlog`
- `litespec/backend/dotnet/application#subscribe-each-reaction-handler-to-its-declared-triggers`
- `litespec/backend/dotnet/application#verify-durable-contracts-event-decisions-and-reference-data-at-startup`
- `litespec/backend/dotnet/architecture#compile-each-contracts-folder-as-its-own-project`
- `litespec/backend/dotnet/architecture#compile-each-knowledge-area-as-a-model-project-with-its-set-projects`
- `litespec/backend/dotnet/architecture#expose-one-public-entry-type-per-module`
- `litespec/backend/dotnet/architecture#expose-only-contracts-and-the-entry-point`
- `litespec/backend/dotnet/architecture#hold-each-layer-in-its-own-folder`
- `litespec/backend/dotnet/architecture#keep-business-values-in-the-kernel`
- `litespec/backend/dotnet/architecture#keep-each-identifier-with-its-owner`
- `litespec/backend/dotnet/architecture#keep-technical-capabilities-in-the-platform`
- `litespec/backend/dotnet/architecture#name-no-business-identifier-in-the-platform`
- `litespec/backend/dotnet/architecture#place-each-read-model-in-its-application-read-models-folder`
- `litespec/backend/dotnet/architecture#plug-modules-into-the-platform-through-contributions`
- `litespec/backend/dotnet/architecture#publish-only-data-from-each-contracts-project`
- `litespec/backend/dotnet/architecture#reference-knowledge-models-from-modules-and-sets-from-hosts`
- `litespec/backend/dotnet/architecture#register-each-module-through-its-entry-point`
- `litespec/backend/dotnet/architecture#separate-the-kernel-the-platform-and-the-modules`
- `litespec/backend/dotnet/config#bind-every-keyed-setting-in-one-options-class`
- `litespec/backend/dotnet/domain#construct-aggregate-parts-only-inside-the-root`
- `litespec/backend/dotnet/domain#offer-a-non-throwing-parse-on-validated-value-types`
- `litespec/backend/dotnet/domain#reference-other-aggregates-by-typed-identifier`
- `litespec/backend/dotnet/domain#split-a-large-root-into-one-file-per-action`
- `litespec/backend/dotnet/domain#throw-only-refusal-codes-the-module-owns`
- `litespec/backend/dotnet/naming#name-every-event-type-by-its-kind`
- `litespec/backend/dotnet/naming#put-the-cancellation-token-last`
- `litespec/backend/dotnet/persistence#apply-schema-changes-in-a-migration-step`
- `litespec/backend/dotnet/persistence#compare-typed-identifiers-by-their-underlying-values-in-queries`
- `litespec/backend/dotnet/persistence#evolve-stored-documents-when-they-are-read`
- `litespec/backend/dotnet/persistence#implement-the-unit-of-work-boundary`
- `litespec/backend/dotnet/persistence#load-documents-keyed-by-typed-identifiers-one-at-a-time`
- `litespec/backend/dotnet/persistence#plan-each-schema-change-before-applying-it`
- `litespec/backend/dotnet/persistence#read-each-guarding-aggregate-under-a-shared-lock`
- `litespec/backend/dotnet/persistence#share-one-serializer-configuration-across-marten-and-the-outbox`
- `litespec/backend/dotnet/persistence#store-each-aggregate-root-as-its-own-document`
- `litespec/backend/dotnet/testing#drain-the-outbox-in-acceptance-tests`
- `litespec/backend/dotnet/workspace#exclude-development-shortcuts-from-release-builds`
- `litespec/backend/dotnet/workspace#keep-the-apphost-in-the-deploy-folder`
- `litespec/backend/knowledge#check-shape-at-compile-time-and-dates-at-startup`
- `litespec/backend/knowledge#give-each-knowledge-area-one-model`
- `litespec/backend/knowledge#keep-knowledge-in-the-backend-language`
- `litespec/backend/knowledge#keep-knowledge-sets-free-of-logic`
- `litespec/backend/knowledge#keep-overrides-and-integration-catalogs-out-of-knowledge`
- `litespec/backend/knowledge#model-reference-data-as-domain-values`
- `litespec/backend/knowledge#name-a-role-not-a-row`
- `litespec/backend/knowledge#read-knowledge-by-role-and-date`
- `litespec/backend/knowledge#read-knowledge-through-its-model`
- `litespec/backend/knowledge#refuse-a-missing-rule-with-a-reason`
- `litespec/backend/knowledge#seed-operational-records-from-templates`
- `litespec/backend/knowledge#use-an-aggregate-for-edited-reference-data-only`
- `litespec/backend/knowledge#validate-reference-data-before-serving`
- `litespec/backend/knowledge#version-each-set-and-freeze-what-records-used`
- `litespec/backend/persistence#serialize-every-persisted-payload-with-one-configuration`
- `litespec/core/access#admit-the-system-identity-on-every-system-run-use-case`
- `litespec/core/access#authenticate-every-caller-through-a-declared-authentication-kind`
- `litespec/core/access#authorize-every-use-case-through-a-declared-grant`
- `litespec/core/access#describe-every-served-content-entry-with-its-serving-fields`
- `litespec/core/access#evaluate-access-in-one-order`
- `litespec/core/access#keep-permission-sets-to-entries`
- `litespec/core/access#limit-the-age-of-a-sign-in`
- `litespec/core/access#model-a-hosted-sign-in-as-use-cases`
- `litespec/core/access#name-every-authorization-precondition`
- `litespec/core/access#name-the-caller-identity-of-a-self-scope`
- `litespec/core/agent#check-your-own-change-before-handing-it-back`
- `litespec/core/agent#exercise-changed-use-cases-through-the-running-stack`
- `litespec/core/agent#follow-the-record-not-the-neighbour`
- `litespec/core/agent#raise-a-spec-gap-instead-of-bending-the-code`
- `litespec/core/agent#scope-every-convention-to-files-it-governs`
- `litespec/core/agent#verify-money-calculations-and-refusals-against-held-back-examples`
- `litespec/core/agent#write-only-rules-you-have-seen-hold`
- `litespec/core/authoring#open-each-page-with-a-lead`
- `litespec/core/authoring#validate-each-template-against-the-record-schema`
- `litespec/core/automation#decide-current-truth-in-the-owning-module`
- `litespec/core/automation#declare-each-read-model-field-from-a-published-field`
- `litespec/core/automation#declare-the-outcomes-of-each-reaction-and-job`
- `litespec/core/automation#end-reaction-and-job-scenarios-in-the-run-result`
- `litespec/core/automation#erase-a-read-model-personal-field-with-its-owner`
- `litespec/core/automation#keep-failure-handling-on-the-reaction-or-job`
- `litespec/core/automation#keep-validity-apart-from-storage`
- `litespec/core/automation#move-a-state-by-time-with-a-timed-transition`
- `litespec/core/automation#name-the-step-a-compensating-reaction-undoes`
- `litespec/core/automation#publish-every-module-event-from-the-event-it-translates`
- `litespec/core/automation#raise-completion-only-when-every-module-is-done`
- `litespec/core/automation#run-at-most-one-use-case-from-a-reaction-or-job`
- `litespec/core/automation#run-each-effect-on-its-own`
- `litespec/core/automation#settle-every-async-outcome-in-visible-states`
- `litespec/core/automation#subscribe-to-every-module-event`
- `litespec/core/designing#change-every-instance-of-a-shape-in-one-change`
- `litespec/core/designing#deliver-each-record-type-complete`
- `litespec/core/designing#give-a-record-with-its-own-reasoning-its-own-folder`
- `litespec/core/designing#give-every-type-field-and-value-a-reader`
- `litespec/core/designing#hold-one-thing-in-each-record`
- `litespec/core/designing#shape-each-record-type-for-its-next-field`
- `litespec/core/designing#state-each-deviation-from-a-design-rule`
- `litespec/core/designing#update-a-design-rule-that-keeps-bending`
- `litespec/core/domain-model#append-released-reference-rows`
- `litespec/core/domain-model#bind-a-reference-data-table-to-an-external-pack`
- `litespec/core/domain-model#cite-varying-values-from-reference-data`
- `litespec/core/domain-model#close-the-value-set-of-every-meaning-column`
- `litespec/core/domain-model#cover-every-date-in-use`
- `litespec/core/domain-model#date-every-reference-data-row`
- `litespec/core/domain-model#keep-each-rule-statement-short`
- `litespec/core/domain-model#keep-one-home-for-each-rule-calculation-and-projection`
- `litespec/core/domain-model#keep-reference-data-rows-from-overlapping`
- `litespec/core/domain-model#keep-technical-state-out-of-behavior`
- `litespec/core/domain-model#name-every-spanned-aggregate-in-binds`
- `litespec/core/domain-model#name-the-record-each-law-rule-restates`
- `litespec/core/domain-model#record-when-each-frozen-value-freezes-and-where-it-lives`
- `litespec/core/domain-model#state-each-rules-source`
- `litespec/core/domain-model#state-one-condition-per-rule`
- `litespec/core/domain-model#state-the-sign-of-every-amount`
- `litespec/core/domain-model#state-what-each-rule-governs`
- `litespec/core/edges#describe-every-integration-event-the-product-sends-or-receives`
- `litespec/core/edges#give-every-screen-a-route-or-a-host`
- `litespec/core/edges#limit-each-screen-to-steps-its-use-cases-serve`
- `litespec/core/edges#name-each-external-system-for-its-role`
- `litespec/core/edges#name-how-every-inbound-integration-event-arrives`
- `litespec/core/edges#name-the-actor-each-external-system-represents`
- `litespec/core/edges#publish-every-outbound-integration-event-from-the-event-it-translates`
- `litespec/core/edges#resolve-late-arrivals-against-owner-written-facts`
- `litespec/core/edges#serve-static-files-as-served-content`
- `litespec/core/edges#state-every-boundary-check-as-a-rule-of-its-module`
- `litespec/core/edges#state-the-density-and-voice-of-each-entry-point`
- `litespec/core/edges#state-the-outcome-of-every-outside-call`
- `litespec/core/edges#state-the-vendor-of-each-external-system`
- `litespec/core/edges#write-the-sentence-a-person-reads-on-each-refusal`
- `litespec/core/intent#branch-a-flow-only-between-its-steps`
- `litespec/core/intent#build-nothing-from-a-direction`
- `litespec/core/intent#cite-the-direction-a-decision-keeps-open`
- `litespec/core/intent#keep-actors-to-people-organizations-and-systems`
- `litespec/core/intent#keep-dates-and-priorities-out-of-directions`
- `litespec/core/intent#lead-each-flow-toward-the-record-it-exists-for`
- `litespec/core/intent#move-every-goal-with-a-flow-or-a-quality-requirement`
- `litespec/core/intent#name-every-actor-from-a-flow-a-problem-or-an-access-position`
- `litespec/core/intent#perform-each-flow-step-with-an-actor`
- `litespec/core/intent#place-product-wide-commitments-as-product-requirements`
- `litespec/core/intent#reach-every-flow-step-by-a-path`
- `litespec/core/intent#record-goal-outcomes-as-observations`
- `litespec/core/intent#record-research-with-its-question-conclusion-and-sources`
- `litespec/core/intent#state-each-decision-with-context-and-choice`
- `litespec/core/intent#state-each-direction-in-its-statement`
- `litespec/core/intent#state-each-flow-blocker-as-a-step-failure`
- `litespec/core/intent#state-the-brand-on-the-product`
- `litespec/core/intent#state-when-each-flow-finishes`
- `litespec/core/intent#state-when-each-flow-starts`
- `litespec/core/intent#summarize-every-flow-before-its-steps`
- `litespec/core/intent#write-flow-steps-as-sentences-with-ids`
- `litespec/core/language#read-each-field-name-in-its-record-type`
- `litespec/core/maintaining#derive-a-general-rule-from-a-consumer-need`
- `litespec/core/maintaining#keep-consumers-out-of-litespec`
- `litespec/core/maintaining#model-a-reported-spec-gap-before-more-code`
- `litespec/core/model#block-every-finding-that-predicts-a-runtime-failure`
- `litespec/core/model#cite-reference-data-instead-of-literal-values`
- `litespec/core/model#declare-cross-module-references-in-depends-on`
- `litespec/core/model#generate-record-determined-code-from-the-records`
- `litespec/core/model#hold-module-rules-in-rule-folders`
- `litespec/core/model#keep-each-name-unique-in-its-record-type`
- `litespec/core/model#keep-generate-targets-outside-other-tools-output-folders`
- `litespec/core/model#keep-the-whole-specification-in-the-records`
- `litespec/core/model#name-records-in-prose-by-their-identifiers`
- `litespec/core/model#name-the-field-to-use-instead-in-schema-errors`
- `litespec/core/model#place-a-record-by-its-content-and-link-it-by-its-reason`
- `litespec/core/model#point-every-link-up`
- `litespec/core/model#prove-every-generator-with-golden-tests`
- `litespec/core/model#report-one-shape-in-schema-errors`
- `litespec/core/model#serve-every-flow-step`
- `litespec/core/model#state-what-a-reader-needs-on-the-record-and-check-each-copy`
- `litespec/core/principles#add-new-machinery-only-for-a-current-requirement`
- `litespec/core/principles#confine-atomic-consistency-to-one-use-case`
- `litespec/core/principles#share-no-transaction-across-an-edge`
- `litespec/core/privacy#enforce-a-data-class-through-its-aggregate`
- `litespec/core/privacy#pause-deletion-while-a-hold-stands`
- `litespec/core/privacy#round-an-event-start-to-a-calendar-end`
- `litespec/core/release#run-one-litespec-build`
- `litespec/core/release#verify-connected-flows`
- `litespec/core/scopes#set-a-rule-aside-only-through-a-recorded-waiver`
- `litespec/core/stacks#name-every-provision-a-replacement-replaces`
- `litespec/core/use-cases#bind-every-keyed-setting-in-one-marked-element`
- `litespec/core/use-cases#carry-every-key-placeholder`
- `litespec/core/use-cases#change-one-aggregate-in-each-command`
- `litespec/core/use-cases#cite-the-invariants-most-writers-cite`
- `litespec/core/use-cases#declare-consistency-on-multi-aggregate-commands`
- `litespec/core/use-cases#declare-each-aggregates-refusals-once`
- `litespec/core/use-cases#declare-each-modules-refusals-once`
- `litespec/core/use-cases#declare-the-binding-of-every-input-of-an-http-reached-use-case`
- `litespec/core/use-cases#declare-the-created-location-of-every-command-that-creates-a-resource`
- `litespec/core/use-cases#declare-the-http-surface-of-every-http-reached-use-case`
- `litespec/core/use-cases#declare-the-result-a-scenario-ends-in`
- `litespec/core/use-cases#declare-the-status-of-every-refusal-an-http-reached-use-case-cites`
- `litespec/core/use-cases#give-each-rule-boundary-its-own-scenario`
- `litespec/core/use-cases#give-every-tenant-setting-a-change-use-case`
- `litespec/core/use-cases#hold-every-cross-aggregate-guard-until-commit`
- `litespec/core/use-cases#keep-each-scenario-inside-its-operation`
- `litespec/core/use-cases#list-the-consumer-modules-of-a-public-query`
- `litespec/core/use-cases#list-the-sending-modules-of-a-public-command`
- `litespec/core/use-cases#mark-every-provider-verified-input`
- `litespec/core/use-cases#name-every-aggregate-a-use-case-writes-or-reads`
- `litespec/core/use-cases#name-only-a-provided-way-out-in-each-refusal-sentence`
- `litespec/core/use-cases#name-the-idempotency-of-every-command`
- `litespec/core/use-cases#name-the-use-case-that-decides-a-stale-figure`
- `litespec/core/use-cases#prove-every-guard-against-a-competing-command`
- `litespec/core/use-cases#return-every-caller-supplied-identifier-from-a-reachable-query`
- `litespec/core/use-cases#run-follow-on-work-only-from-reactions-and-jobs`
- `litespec/core/use-cases#state-the-exposure-of-every-use-case`
- `litespec/core/use-cases#state-what-a-replay-does`
- `litespec/core/use-cases#summarize-every-use-case`
- `litespec/core/use-cases#tie-every-setting-default-to-a-requirement`
- `litespec/core/use-cases#type-every-output-field`
- `litespec/core/use-cases#write-special-endpoints-by-hand`
- `litespec/core/waivers#cover-each-finding-with-one-waiver`
- `litespec/core/waivers#deviate-from-advice-without-a-record`
- `litespec/core/waivers#name-the-records-that-close-a-gap`
- `litespec/core/waivers#record-a-project-wide-lowering-as-a-waiver`
- `litespec/core/waivers#record-accepted-gaps-as-waivers`
- `litespec/core/waivers#state-the-kind-of-every-waiver`
- `litespec/core/writing#explain-the-same-specification-the-records-state`
- `litespec/core/writing#keep-the-cast-out-of-production-code`
- `litespec/core/writing#update-a-record-and-its-explanation-together`
- `litespec/core/writing#use-one-mechanical-form-for-each-fact`
- `litespec/ext/audit#chain-records-to-expose-tampering`
- `litespec/ext/audit#declare-which-commands-are-audited`
- `litespec/ext/audit#delete-expired-records-through-a-separate-operator-account`
- `litespec/ext/audit#delete-records-when-their-retention-ends`
- `litespec/ext/audit#record-each-segment-deletion-in-the-following-segment`
- `litespec/ext/audit#segment-the-audit-chain-by-category-and-calendar-year`
- `litespec/ext/audit#state-the-checkpoint-interval-as-the-detection-window`
- `litespec/ext/cache#state-the-staleness-each-cached-query-accepts`
- `litespec/ext/concurrency#require-caller-versions-only-for-declared-conflicts`
- `litespec/ext/delivery#close-incident-waivers-within-seven-days`
- `litespec/ext/delivery#expose-release-health-and-diagnostics`
- `litespec/ext/delivery#keep-deployment-limits-and-runbooks-ready`
- `litespec/ext/delivery#link-each-operations-file-to-its-decision`
- `litespec/ext/delivery#link-every-runbook-to-its-caller`
- `litespec/ext/jobs#keep-worker-orchestration-separate-from-report-rules`
- `litespec/ext/jobs#name-a-varying-time-zone-by-its-source`
- `litespec/ext/jobs#record-the-schedule-on-the-job-record`
- `litespec/ext/mcp#keep-secrets-and-personal-data-out-of-descriptions-and-logs`
- `litespec/ext/outbox#decide-every-domain-event-outcome`
- `litespec/ext/outbox#deliver-module-facts-in-process-without-a-broker`
- `litespec/ext/outbox#dispatch-flow-step-commands-in-a-new-scope`
- `litespec/ext/outbox#dispatch-projections-immediately-after-commit`
- `litespec/ext/outbox#keep-published-events-available-for-read-model-rebuilds`
- `litespec/ext/queues#confirm-bulk-money-actions-out-of-band`
- `litespec/ext/queues#order-queue-items-by-score`
- `litespec/ext/queues#order-unscored-queues-by-age`
- `litespec/ext/queues#release-claims-past-their-lease`
- `litespec/ext/queues#show-a-preview-before-bulk-money-actions`
- `litespec/ext/queues#show-the-score-beside-each-item`
- `litespec/ext/queues#state-the-claim-mode`
- `litespec/ext/realtime#measure-polling-before-choosing-realtime`
- `litespec/frontend/components#group-the-shared-ui-package-by-role`
- `litespec/frontend/data#brand-every-identifier-from-the-contract`
- `litespec/frontend/data#compose-a-screen-from-each-owners-query`
- `litespec/frontend/data#hold-every-monetary-amount-in-the-frame-money-type`
- `litespec/frontend/data#regenerate-the-client-with-the-contract`
- `litespec/frontend/mobile/screens#route-app-links-to-flow-entry-screens`
- `litespec/frontend/patterns#draw-each-screen-as-a-wireframe-before-it-is-built`
- `litespec/frontend/patterns/feedback-and-states#route-error-frame`
- `litespec/frontend/patterns/feedback-and-states#route-loading-frame`
- `litespec/frontend/patterns/page-frames#entry-page`
- `litespec/frontend/testing#name-the-flow-a-flow-test-covers`
- `litespec/frontend/ui#align-numbers-to-the-end`
- `litespec/frontend/ui#cover-each-screen-scope-with-its-read`
- `litespec/frontend/ui#draw-each-role-with-one-composite`
- `litespec/frontend/ui#draw-every-field-on-a-surface`
- `litespec/frontend/ui#group-a-long-form`
- `litespec/frontend/ui#keep-one-action-group-to-one-size`
- `litespec/frontend/ui#keep-token-values-in-one-token-file`
- `litespec/frontend/ui#limit-a-page-to-three-stacked-sections`
- `litespec/frontend/ui#link-each-screen-to-its-next-screens`
- `litespec/frontend/ui#map-each-page-frame-to-its-floorplan`
- `litespec/frontend/ui#mark-every-destructive-command-alike`
- `litespec/frontend/ui#match-each-field-width-to-its-value`
- `litespec/frontend/ui#meet-the-limits-of-every-token-role`
- `litespec/frontend/ui#name-money-and-moment-formats-on-each-entry-point`
- `litespec/frontend/ui#name-the-page-frame-on-every-screen`
- `litespec/frontend/ui#place-no-surface-inside-another`
- `litespec/frontend/ui#review-each-screen-at-three-widths`
- `litespec/frontend/ui#route-a-busy-screen-through-a-hub-frame`
- `litespec/frontend/ui#show-names-never-codes`
- `litespec/frontend/ui#show-one-primary-action-per-view`
- `litespec/frontend/ui#show-only-fields-an-offered-use-case-outputs-or-accepts`
- `litespec/frontend/ui#use-tabs-only-for-peer-views`
- `litespec/frontend/web/components#name-visual-files-with-the-stack-suffix`
- `litespec/frontend/web/components#render-raw-html-through-the-stack-sink`
- `litespec/frontend/web/components#use-the-stack-class-and-style-bindings`
- `litespec/frontend/web/data#add-the-stack-query-plugin-to-the-client-generator`
- `litespec/frontend/web/data#build-a-web-form-on-the-shared-form-entry-point`
- `litespec/frontend/web/data#resolve-copy-through-the-stack-dictionary-library`
- `litespec/frontend/web/rendering#show-route-failure-on-the-error-surface`
- `litespec/frontend/web/structure#group-the-web-frame-by-capability`
- `litespec/frontend/web/structure#keep-web-layouts-error-surfaces-and-route-groups-unmarked`
- `litespec/frontend/web/structure#mark-each-web-route-entry-with-its-screen`
- `litespec/frontend/web/structure#use-the-stack-file-extension`
- `litespec/frontend/web/testing#keep-web-test-mocks-to-the-stack-libraries`
- `litespec/frontend/web/ui#draw-platform-composites-with-native-elements`
- `litespec/frontend/web/ui#keep-the-tone-table-in-the-stack-states-file`
- `litespec/quality/development#add-a-missing-example-fact-to-the-cast-not-to-the-seed`
- `litespec/quality/development#authenticate-as-a-cast-person`
- `litespec/quality/development#check-seeded-figures-against-the-cast`
- `litespec/quality/development#generate-volume-data-apart-from-the-cast`
- `litespec/quality/development#give-each-external-system-a-stand-in-that-answers-its-declared-operations`
- `litespec/quality/development#keep-development-shortcuts-out-of-release-builds`
- `litespec/quality/development#keep-the-real-identity-provider-one-launch-profile-away`
- `litespec/quality/development#provide-seed-and-reset-from-the-local-orchestrator`
- `litespec/quality/development#review-screens-with-seeded-data`
- `litespec/quality/development#run-the-api-walker-after-api-boundary-changes`
- `litespec/quality/development#run-the-local-stack-on-the-cast-day`
- `litespec/quality/development#seed-from-the-cast-through-public-use-cases`
- `litespec/quality/development#seed-only-development-and-test-environments`
- `litespec/quality/development#show-failure-causes-in-development`
- `litespec/quality/development#split-the-seed-into-repeatable-scenarios`
- `litespec/quality/development#start-the-stack-with-one-local-command`
- `litespec/workspace/design#declare-each-pipeline-concern-in-its-own-class`
- `litespec/workspace/design#give-each-class-one-responsibility`
- `litespec/workspace/design#implement-one-capability-per-class`
- `litespec/workspace/design#name-each-definition-after-its-message-and-concern`
- `litespec/workspace/design#take-shared-behavior-through-composition`
- `litespec/workspace/naming#avoid-primary-constructors-on-classes-and-structs`
- `litespec/workspace/naming#place-each-trigger-with-what-it-runs`

Removed (221):

- `litespec/backend/application#declare-a-port-where-its-consumers-meet`
- `litespec/backend/application#emit-declared-integration-events-after-commit`
- `litespec/backend/application#expose-store-state-through-ports`
- `litespec/backend/application#send-messages-after-commit`
- `litespec/backend/application#take-repositories-from-declared-changes-and-reads`
- `litespec/backend/architecture#subscribe-across-modules-only-to-integration-events`
- `litespec/backend/domain#call-the-marked-act-from-each-command-handler`
- `litespec/backend/domain#place-every-constituent-type-in-its-aggregates-folder`
- `litespec/backend/domain#use-these-domain-names`
- `litespec/backend/dotnet/api#keep-programcs-as-composition`
- `litespec/backend/dotnet/api#mark-polymorphic-transport-models-with-discriminators`
- `litespec/backend/dotnet/api#place-endpoints-in-their-use-case-folders`
- `litespec/backend/dotnet/application#keep-query-handlers-free-of-repositories`
- `litespec/backend/dotnet/application#use-the-use-case-folder-layout`
- `litespec/backend/dotnet/architecture#separate-the-shared-kernel-from-the-modules`
- `litespec/backend/dotnet/architecture#use-one-public-assembly-marker`
- `litespec/backend/dotnet/domain#document-every-public-domain-contract`
- `litespec/backend/dotnet/domain#use-the-domain-layer-folder-layout`
- `litespec/backend/dotnet/ext/layer-projects#keep-application-dependencies-narrow`
- `litespec/backend/dotnet/ext/layer-projects#keep-domain-package-free`
- `litespec/backend/dotnet/ext/layer-projects#keep-net-production-and-test-projects-separate`
- `litespec/backend/dotnet/ext/layer-projects#keep-one-application-assembly`
- `litespec/backend/dotnet/ext/layer-projects#keep-the-test-projects-the-size-requires`
- `litespec/backend/dotnet/ext/layer-projects#mirror-modules-in-folders-and-namespaces`
- `litespec/backend/dotnet/ext/layer-projects#run-litespecanalyzers-in-every-application-project`
- `litespec/backend/dotnet/ext/layer-projects#use-five-application-projects`
- `litespec/backend/dotnet/ext/layer-projects#use-the-domain-module-layout`
- `litespec/backend/dotnet/ext/layer-projects#use-the-endpoint-folder-layout`
- `litespec/backend/dotnet/ext/layer-projects#use-the-operation-folder-layout`
- `litespec/backend/dotnet/ext/layer-projects#use-the-project-reference-graph`
- `litespec/backend/dotnet/ext/layer-projects#use-this-baseline-package-ownership`
- `litespec/backend/dotnet/ext/layer-projects#use-this-infrastructure-layout`
- `litespec/backend/dotnet/naming#check-architectural-role-suffixes`
- `litespec/backend/dotnet/naming#check-repository-variable-names`
- `litespec/backend/dotnet/naming#check-result-part-names`
- `litespec/backend/dotnet/persistence#use-the-persistence-layer-folder-layout`
- `litespec/backend/dotnet/workspace#place-a-workspace-apphost-beside-the-applications-it-runs`
- `litespec/core/access#authenticate-every-caller-through-a-declared-auth-kind`
- `litespec/core/access#describe-every-served-content-entry-point-with-its-serving-fields`
- `litespec/core/access#grant-every-use-case-through-a-declared-grant`
- `litespec/core/access#keep-access-records-in-their-homes`
- `litespec/core/access#keep-permission-lists-to-entries`
- `litespec/core/access#write-a-summary-on-every-actor-and-role-page`
- `litespec/core/access#write-a-summary-on-every-external-system-page`
- `litespec/core/agent#escalate-decisions-that-specify-forbids`
- `litespec/core/agent#follow-the-local-pattern-before-the-model-query`
- `litespec/core/agent#raise-open-figures-to-the-owner`
- `litespec/core/agent#verify-refusals-and-calculations-against-held-back-examples`
- `litespec/core/agent#write-pages-below-decided-intent`
- `litespec/core/authoring#copy-each-template-from-the-example`
- `litespec/core/authoring#limit-each-intent-to-three-sentences`
- `litespec/core/automation#guard-every-manual-job`
- `litespec/core/automation#point-reaction-and-job-scenarios-at-the-run-result`
- `litespec/core/behavior#group-cross-aggregate-rules-by-theme`
- `litespec/core/departures#depart-from-advice-without-a-record`
- `litespec/core/departures#record-a-project-wide-lowering-as-a-departure`
- `litespec/core/departures#record-accepted-gaps-as-departures`
- `litespec/core/domain-model#declare-the-kind-of-every-aggregate`
- `litespec/core/domain-model#freeze-every-commercial-data-value`
- `litespec/core/domain-model#keep-cross-aggregate-rules-on-a-rule-page`
- `litespec/core/domain-model#leave-states-off-every-store`
- `litespec/core/domain-model#name-the-owner-of-every-store`
- `litespec/core/domain-model#state-every-module-dependency`
- `litespec/core/edges#describe-every-integration-event-for-its-direction`
- `litespec/core/edges#give-every-screen-an-address-or-a-host`
- `litespec/core/edges#publish-every-outbound-integration-event-from-effects`
- `litespec/core/edges#reach-every-inbound-integration-event-through-a-webhook`
- `litespec/core/edges#serve-only-the-steps-a-screens-use-cases-serve`
- `litespec/core/edges#serve-static-content-through-served-content`
- `litespec/core/edges#show-every-refusal-a-screen-can-reach`
- `litespec/core/intent#add-no-behavior-in-an-explanation-file`
- `litespec/core/intent#branch-a-journey-only-between-its-steps`
- `litespec/core/intent#build-nothing-from-a-vision`
- `litespec/core/intent#cite-each-applying-constraint-from-a-requirement`
- `litespec/core/intent#cite-the-vision-a-decision-keeps-open`
- `litespec/core/intent#describe-every-journey`
- `litespec/core/intent#explain-each-requirement-in-one-line`
- `litespec/core/intent#group-product-records-by-problem`
- `litespec/core/intent#keep-actors-to-people-and-organizations`
- `litespec/core/intent#keep-company-operation-out-of-intent`
- `litespec/core/intent#keep-dates-and-priorities-out-of-visions`
- `litespec/core/intent#keep-intent-in-its-homes`
- `litespec/core/intent#move-every-goal-with-a-journey-or-a-quality-requirement`
- `litespec/core/intent#name-every-actor-from-a-journey-a-problem-or-an-access-position`
- `litespec/core/intent#name-product-decisions-in-one-numbered-sequence`
- `litespec/core/intent#order-each-journey-toward-one-goal`
- `litespec/core/intent#perform-each-journey-step-with-an-actor-or-a-system`
- `litespec/core/intent#record-research-with-read-sources`
- `litespec/core/intent#resolve-every-failure-case-with-an-outcome`
- `litespec/core/intent#shape-each-decision-page`
- `litespec/core/intent#state-each-journey-blocker-as-a-step-failure`
- `litespec/core/intent#state-each-problem-with-its-evidence`
- `litespec/core/intent#state-each-vision-in-its-fields`
- `litespec/core/intent#state-when-each-journey-finishes`
- `litespec/core/intent#state-when-each-journey-starts`
- `litespec/core/intent#write-journey-steps-as-sentences-with-ids`
- `litespec/core/language#follow-the-identifier-form-of-each-record-type`
- `litespec/core/language#name-each-record-by-its-types-name-form`
- `litespec/core/language#name-records-with-one-to-six-words`
- `litespec/core/layers#record-changed-provider-behavior-as-observed-research`
- `litespec/core/layers#send-commercial-and-observable-figures-to-the-owner`
- `litespec/core/model#author-one-slice-at-a-time`
- `litespec/core/model#hold-module-rules-in-rule-sets`
- `litespec/core/model#keep-each-name-unique-in-the-product`
- `litespec/core/model#require-an-explanation-file-for-article-types`
- `litespec/core/model#serve-every-journey-step`
- `litespec/core/model#use-one-metadata-carrier`
- `litespec/core/principles#confine-atomic-consistency-to-synchronous-edges`
- `litespec/core/principles#require-current-complexity-activation`
- `litespec/core/principles#state-transaction-sharing-on-every-inline-edge`
- `litespec/core/privacy#schedule-every-data-class-for-retention`
- `litespec/core/release#record-accepted-debt-as-a-departure`
- `litespec/core/release#run-backend-release-checks`
- `litespec/core/release#run-extension-checks`
- `litespec/core/release#run-frontend-release-checks`
- `litespec/core/release#run-journey-checks`
- `litespec/core/release#verify-connected-journeys`
- `litespec/core/scopes#depart-from-a-rule-only-through-a-recorded-departure`
- `litespec/core/stacks#declare-replacements`
- `litespec/core/stacks#use-manifest-version-pins`
- `litespec/core/system#drive-work-from-records`
- `litespec/core/use-cases#bind-every-keyed-setting-in-one-marked-options-class`
- `litespec/core/use-cases#connect-each-use-case-to-intent`
- `litespec/core/use-cases#grant-each-use-case-scoped-access`
- `litespec/core/use-cases#hold-every-setting-default-to-a-requirement`
- `litespec/core/use-cases#keep-the-effect-graph-free-of-cycles`
- `litespec/core/use-cases#mark-every-lagging-query-as-built-asynchronously`
- `litespec/core/use-cases#name-every-aggregate-a-use-case-changes-or-reads`
- `litespec/core/use-cases#name-the-idempotency-of-every-use-case`
- `litespec/core/use-cases#state-the-consistency-of-every-use-case`
- `litespec/core/use-cases#state-the-transaction-sharing-of-every-inline-call`
- `litespec/core/use-cases#write-prose-on-every-use-case`
- `litespec/core/writing#apply-plain-mechanics`
- `litespec/core/writing#compare-real-options-in-each-decision-record`
- `litespec/core/writing#open-each-summary-with-who-and-what-is-at-stake`
- `litespec/ext/audit#declare-audit-selection-on-every-command`
- `litespec/ext/audit#provide-tamper-evidence`
- `litespec/ext/audit#retain-each-category-for-its-declared-period`
- `litespec/ext/cache#record-accepted-cache-staleness`
- `litespec/ext/concurrency#limit-version-checks`
- `litespec/ext/delivery#close-incident-departures-within-seven-days`
- `litespec/ext/delivery#name-every-runbook-from-an-alert-job-reaction-or-failure-policy`
- `litespec/ext/delivery#point-each-operations-file-at-its-decision`
- `litespec/ext/delivery#provide-release-delivery-controls`
- `litespec/ext/delivery#provide-release-operating-visibility`
- `litespec/ext/integrations#group-provider-code`
- `litespec/ext/integrations#keep-ports-near-use-cases`
- `litespec/ext/jobs#carry-the-schedule-on-the-job-record`
- `litespec/ext/jobs#confirm-bulk-money-actions-out-of-band`
- `litespec/ext/jobs#order-queue-items-by-score`
- `litespec/ext/jobs#order-unscored-queues-by-age`
- `litespec/ext/jobs#release-claims-past-their-lease`
- `litespec/ext/jobs#run-manual-jobs-under-their-guard`
- `litespec/ext/jobs#separate-worker-orchestration`
- `litespec/ext/jobs#show-a-preview-before-bulk-money-actions`
- `litespec/ext/jobs#show-the-score-beside-each-item`
- `litespec/ext/jobs#state-the-claim-mode`
- `litespec/ext/mcp#exclude-secrets-and-personal-data`
- `litespec/ext/outbox#dispatch-journey-step-commands-in-a-new-scope`
- `litespec/ext/plans#bound-plan-amounts-in-settings`
- `litespec/ext/plans#gate-behavior-through-permissions`
- `litespec/ext/plans#refuse-runs-beyond-entitlement`
- `litespec/ext/realtime#record-realtime-need`
- `litespec/ext/stage#declare-the-release-stage`
- `litespec/ext/stage#defer-named-provisions-until-promotion`
- `litespec/ext/stage#promote-through-a-recorded-decision`
- `litespec/ext/tenancy#declare-delegation-grants-on-both-sides`
- `litespec/ext/tenancy#prove-delegated-calls-with-scenarios`
- `litespec/ext/tenancy#scope-every-use-case-to-its-tenant-reach`
- `litespec/frontend/mobile/screens#route-app-links-to-journey-entry-screens`
- `litespec/frontend/patterns/create-edit-remove#refuse-a-second-send-while-one-is-in-flight`
- `litespec/frontend/patterns/lists-and-tables#align-money-to-the-right-with-its-currency`
- `litespec/frontend/patterns/lists-and-tables#keep-a-lists-view-in-its-address`
- `litespec/frontend/patterns/navigation#give-every-page-its-own-address`
- `litespec/frontend/patterns/page-frames#name-the-page-in-one-heading`
- `litespec/frontend/testing#name-the-journey-a-journey-test-covers`
- `litespec/frontend/ui#declare-a-closed-floorplan-set`
- `litespec/frontend/ui#hold-the-declared-base-color`
- `litespec/frontend/ui#keep-separators-at-the-subtle-step`
- `litespec/frontend/web/react/stack#add-the-tanstack-query-plugin-to-the-client-generator`
- `litespec/frontend/web/react/stack#apply-the-page-code-lint-to-route-and-module-code`
- `litespec/frontend/web/react/stack#declare-route-search-parameters-in-validatesearch`
- `litespec/frontend/web/react/stack#hold-a-routes-parts-in-its-route-file`
- `litespec/frontend/web/react/stack#keep-layouts-error-components-and-route-groups-unmarked`
- `litespec/frontend/web/react/stack#keep-react-test-mocks-to-the-owned-libraries`
- `litespec/frontend/web/react/stack#keep-the-react-frame-routes-and-modules`
- `litespec/frontend/web/react/stack#keep-the-tone-table-in-one-states-file`
- `litespec/frontend/web/react/stack#load-intent-targets-before-navigation`
- `litespec/frontend/web/react/stack#mark-each-react-route-entry-with-its-screen`
- `litespec/frontend/web/react/stack#meet-the-locale-dictionary-contract-with-the-dictionary-library`
- `litespec/frontend/web/react/stack#name-drawings-with-the-visual-suffix`
- `litespec/frontend/web/react/stack#prefetch-route-reads-in-the-loader`
- `litespec/frontend/web/react/stack#read-route-data-with-usesuspensequery`
- `litespec/frontend/web/react/stack#render-raw-html-only-through-dangerouslysetinnerhtml`
- `litespec/frontend/web/react/stack#run-the-registry-build-command`
- `litespec/frontend/web/react/stack#show-route-failure-in-the-route-error-component`
- `litespec/frontend/web/react/stack#use-named-props-for-class-and-style-bindings`
- `litespec/frontend/web/react/stack#use-the-shared-package-form-hook`
- `litespec/frontend/web/react/stack#use-the-tsx-extension-on-shared-files`
- `litespec/frontend/web/svelte/stack#add-the-tanstack-query-plugin-to-the-client-generator`
- `litespec/frontend/web/svelte/stack#apply-the-page-code-lint-to-route-and-module-code`
- `litespec/frontend/web/svelte/stack#declare-route-parameters-in-a-schema`
- `litespec/frontend/web/svelte/stack#hold-a-routes-parts-in-its-folder`
- `litespec/frontend/web/svelte/stack#keep-layout-files-error-pages-and-route-groups-unmarked`
- `litespec/frontend/web/svelte/stack#keep-svelte-test-mocks-to-the-owned-libraries`
- `litespec/frontend/web/svelte/stack#keep-the-svelte-frame-routes-and-modules`
- `litespec/frontend/web/svelte/stack#keep-the-tone-table-in-one-states-file`
- `litespec/frontend/web/svelte/stack#load-intent-targets-before-navigation`
- `litespec/frontend/web/svelte/stack#mark-svelte-route-entries`
- `litespec/frontend/web/svelte/stack#meet-the-locale-dictionary-contract-with-the-dictionary-library`
- `litespec/frontend/web/svelte/stack#name-drawings-with-the-visual-suffix`
- `litespec/frontend/web/svelte/stack#prefetch-route-reads-in-the-load`
- `litespec/frontend/web/svelte/stack#read-route-data-with-a-query-creator`
- `litespec/frontend/web/svelte/stack#render-raw-html-only-through-one-template-tag`
- `litespec/frontend/web/svelte/stack#run-the-registry-build-command`
- `litespec/frontend/web/svelte/stack#show-route-failure-in-the-route-error-page`
- `litespec/frontend/web/svelte/stack#use-named-attributes-for-class-and-style-bindings`
- `litespec/frontend/web/svelte/stack#use-the-shared-package-form-creator`
- `litespec/frontend/web/svelte/stack#use-the-svelte-extension-on-shared-files`
- `litespec/quality/operations#start-the-stack-with-one-local-command`
- `litespec/workspace/naming#group-event-handlers-by-the-fact-they-handle`

Renamed (161):

- `litespec/backend/api#keep-endpoint-dependencies-transport-focused` to `litespec/backend/api#keep-business-dependencies-out-of-endpoints`
- `litespec/backend/api#mirror-a-domain-closed-set-as-a-transport-model-of-the-same-shape` to `litespec/backend/api#mirror-each-closed-set-on-the-wire`
- `litespec/backend/application#declare-audit-values-beside-the-message` to `litespec/backend/application#declare-the-audit-target-and-reason-beside-the-message`
- `litespec/backend/application#keep-reaction-implementations-explicit` to `litespec/backend/application#answer-each-reaction-in-one-marked-class`
- `litespec/backend/application#read-no-repository-inside-a-loop` to `litespec/backend/application#keep-repository-reads-out-of-loops`
- `litespec/backend/application#return-use-case-results` to `litespec/backend/application#return-only-what-the-caller-needs`
- `litespec/backend/application#take-one-timestamp-per-message` to `litespec/backend/application#read-time-from-the-scoped-clock`
- `litespec/backend/architecture#declare-the-execution-host-for-work-that-outlives-a-request` to `litespec/backend/architecture#name-the-host-for-work-that-outlives-a-request`
- `litespec/backend/architecture#keep-implementation-types-hidden` to `litespec/backend/architecture#keep-implementations-hidden-and-closed`
- `litespec/backend/domain#define-collection-value-semantics-explicitly` to `litespec/backend/domain#compare-collection-values-by-their-content`
- `litespec/backend/domain#describe-every-public-domain-contract-in-documentation` to `litespec/backend/domain#document-every-public-domain-contract`
- `litespec/backend/domain#expose-repositories-for-one-aggregate-only` to `litespec/backend/domain#limit-each-repository-to-one-aggregate`
- `litespec/backend/domain#identify-aggregates-with-strongly-typed-version-7-values` to `litespec/backend/domain#identify-each-aggregate-with-a-typed-version-7-identifier`
- `litespec/backend/domain#model-every-closed-set-of-domain-values-as-a-closed-type-hierarchy` to `litespec/backend/domain#model-every-closed-set-as-a-type-hierarchy`
- `litespec/backend/domain#take-identity-and-events-from-the-project-root-contract` to `litespec/backend/domain#take-identity-and-events-from-the-shared-root-contract`
- `litespec/backend/dotnet/domain#make-money-and-decimal-rules-explicit` to `litespec/backend/dotnet/domain#hold-every-monetary-amount-in-the-shared-money-object`
- `litespec/backend/dotnet/domain#mark-each-changing-root-method-with-one-use-case` to `litespec/backend/dotnet/domain#mark-each-state-changing-root-method-with-its-use-case`
- `litespec/backend/dotnet/ext/bdd#drive-public-behavior` to `litespec/backend/dotnet/ext/bdd#call-public-boundaries-from-bdd-steps`
- `litespec/backend/dotnet/ext/bdd#group-feature-files-by-module` to `litespec/backend/dotnet/ext/bdd#group-feature-classes-by-module`
- `litespec/backend/dotnet/ext/bdd#prove-a-scenario-from-every-bdd-scenario` to `litespec/backend/dotnet/ext/bdd#trace-every-bdd-scenario-to-its-scenario`
- `litespec/backend/dotnet/ext/bdd#translate-business-phrases` to `litespec/backend/dotnet/ext/bdd#translate-business-phrases-into-test-driver-calls`
- `litespec/backend/dotnet/naming#name-no-folder-or-type-for-the-absence-of-a-reason` to `litespec/backend/dotnet/naming#name-every-folder-and-type-for-its-reason`
- `litespec/backend/dotnet/workspace#keep-one-workspace-solution-beside-the-api-solution` to `litespec/backend/dotnet/workspace#list-every-deployable-unit-in-the-root-solution`
- `litespec/backend/persistence#bound-aggregate-document-growth` to `litespec/backend/persistence#keep-unbounded-collections-out-of-aggregate-documents`
- `litespec/backend/persistence#read-collection-queries-in-the-store-with-paging-and-filters` to `litespec/backend/persistence#page-and-filter-collection-queries-in-the-store`
- `litespec/core/access#scope-every-role` to `litespec/core/access#scope-every-role-to-its-one-reach`
- `litespec/core/authoring#give-one-observable-fact-one-mark-across-pages` to `litespec/core/authoring#use-one-mark-for-one-fact-across-pages`
- `litespec/core/authoring#identify-actionable-conventions` to `litespec/core/authoring#write-each-convention-with-a-default-and-a-replacement`
- `litespec/core/authoring#map-provisions-to-evidence` to `litespec/core/authoring#end-each-provision-with-one-verification-line`
- `litespec/core/authoring#mark-every-provision-checkable-or-guidance` to `litespec/core/authoring#mark-each-provision-as-checked-or-reviewed`
- `litespec/core/authoring#name-the-escape-from-every-abstraction` to `litespec/core/authoring#state-what-the-command-runs-underneath`
- `litespec/core/authoring#name-the-tool-rather-than-the-mechanism` to `litespec/core/authoring#name-the-command-that-performs-each-step`
- `litespec/core/authoring#record-a-setting-as-reference` to `litespec/core/authoring#record-each-setting-on-a-configuration-page`
- `litespec/core/authoring#write-atomic-standards-provisions` to `litespec/core/authoring#keep-each-standard-to-one-obligation`
- `litespec/core/edges#claim-every-notification-promise-from-a-requirement` to `litespec/core/edges#cite-the-requirement-behind-every-notification-claim`
- `litespec/core/edges#keep-notification-templates-and-the-template-store-one-set` to `litespec/core/edges#match-notification-templates-to-the-template-store`
- `litespec/core/intent#state-requirement-kind-and-statement` to `litespec/core/intent#state-each-requirement-with-kind-and-statement`
- `litespec/core/language#exclude-the-compound-that-names-another-concept` to `litespec/core/language#list-the-compounds-a-rejection-does-not-cover`
- `litespec/core/language#write-each-kind-of-text-in-its-form` to `litespec/core/language#write-each-text-kind-in-its-own-form`
- `litespec/core/model#declare-structured-specification-metadata` to `litespec/core/model#hold-every-record-file-and-frontmatter-block-to-the-schema`
- `litespec/core/model#show-only-computed-judgements-in-the-book` to `litespec/core/model#show-only-computed-judgments-in-the-book`
- `litespec/core/principles#follow-intent-where-it-has-spoken` to `litespec/core/principles#follow-intent-where-it-states-the-promise`
- `litespec/core/principles#name-boundary-intent` to `litespec/core/principles#name-boundaries-for-their-business-action`
- `litespec/core/release#qualify-the-reader-with-known-good-and-known-bad-fixtures` to `litespec/core/release#qualify-each-stack-reader-with-fixtures`
- `litespec/core/release#run-consistency-checks` to `litespec/core/release#check-intent-and-behavior-changes-with-the-book`
- `litespec/core/writing#state-each-statement-without-modal-words` to `litespec/core/writing#write-each-statement-as-a-fact-without-modal-words`
- `litespec/ext/ai#assert-structure-and-never-wording` to `litespec/ext/ai#assert-output-structure-instead-of-wording`
- `litespec/ext/audit#add-a-correcting-record` to `litespec/ext/audit#correct-mistakes-with-a-new-record`
- `litespec/ext/audit#cover-the-required-audit-categories` to `litespec/ext/audit#audit-every-required-category`
- `litespec/ext/audit#declare-audit-selection-beside-the-command` to `litespec/ext/audit#declare-audit-details-on-the-command-type`
- `litespec/ext/audit#emit-audit-records-at-the-mediation-boundary` to `litespec/ext/audit#write-audit-records-in-the-mediation-pipeline`
- `litespec/ext/audit#exclude-indiscriminate-auditing` to `litespec/ext/audit#audit-only-selected-commands`
- `litespec/ext/audit#exclude-personal-data-beyond-actor-identity` to `litespec/ext/audit#limit-personal-data-to-actor-and-target-identifiers`
- `litespec/ext/audit#exclude-secrets-and-payment-data` to `litespec/ext/audit#keep-secrets-and-payment-data-from-the-trail`
- `litespec/ext/audit#exclude-state-snapshots` to `litespec/ext/audit#keep-state-copies-from-audit-records`
- `litespec/ext/audit#give-the-trail-a-read-path` to `litespec/ext/audit#provide-a-use-case-that-reads-the-trail`
- `litespec/ext/audit#record-a-bulk-read-inside-its-own-boundary` to `litespec/ext/audit#audit-bulk-reads-inside-one-boundary`
- `litespec/ext/audit#record-bulk-export` to `litespec/ext/audit#audit-every-bulk-export`
- `litespec/ext/audit#record-delegated-administrative-access` to `litespec/ext/audit#mark-actions-taken-under-a-support-grant`
- `litespec/ext/audit#record-the-operation-trace-identity` to `litespec/ext/audit#link-each-record-to-its-operation-trace`
- `litespec/ext/audit#reject-an-undeclared-command` to `litespec/ext/audit#fail-the-build-for-commands-without-an-audit-declaration`
- `litespec/ext/audit#state-the-action-as-use-case-identity` to `litespec/ext/audit#name-each-action-after-its-use-case`
- `litespec/ext/cache#emit-a-signal-for-each-degraded-cache-path` to `litespec/ext/cache#distinguish-cache-misses-errors-and-degraded-responses`
- `litespec/ext/cache#record-a-manual-cache-invalidation` to `litespec/ext/cache#record-each-manual-cache-invalidation`
- `litespec/ext/compat#classify-independent-consumer-changes` to `litespec/ext/compat#classify-changes-that-reach-independent-consumers`
- `litespec/ext/compat#diff-release-contracts` to `litespec/ext/compat#compare-release-contracts-against-baselines`
- `litespec/ext/compat#exercise-generated-consumers` to `litespec/ext/compat#test-generated-clients-against-changed-contracts`
- `litespec/ext/compat#gate-new-error-outcomes` to `litespec/ext/compat#add-error-outcomes-only-when-old-clients-cope`
- `litespec/ext/compat#publish-deprecation-context` to `litespec/ext/compat#announce-planned-removals-in-the-changelog`
- `litespec/ext/compat#record-version-retirement` to `litespec/ext/compat#record-how-each-version-retires`
- `litespec/ext/compat#reject-breaking-diffs-without-authorization` to `litespec/ext/compat#block-breaking-changes-without-authorization`
- `litespec/ext/compat#resolve-uncertain-classifications-safely` to `litespec/ext/compat#classify-unclear-changes-as-breaking`
- `litespec/ext/compat#review-exhaustive-consumer-changes` to `litespec/ext/compat#review-new-enum-values-and-subtypes-with-consumers`
- `litespec/ext/compat#treat-removed-behavior-fields-as-breaking` to `litespec/ext/compat#treat-removed-behavior-fields-as-breaking-changes`
- `litespec/ext/concurrency#bound-expired-record-cleanup` to `litespec/ext/concurrency#remove-expired-keys-within-fixed-bounds`
- `litespec/ext/concurrency#canonicalize-request-fingerprints` to `litespec/ext/concurrency#build-fingerprints-from-canonical-request-data`
- `litespec/ext/concurrency#compare-expected-aggregate-versions` to `litespec/ext/concurrency#compare-caller-and-stored-versions-in-one-transaction`
- `litespec/ext/concurrency#declare-replayed-transport-outcomes` to `litespec/ext/concurrency#list-which-statuses-and-headers-replay`
- `litespec/ext/concurrency#exclude-unsafe-replay-outcomes` to `litespec/ext/concurrency#exclude-failures-and-secrets-from-replays`
- `litespec/ext/concurrency#map-failed-expected-versions` to `litespec/ext/concurrency#return-409-for-failed-version-checks`
- `litespec/ext/concurrency#protect-fingerprint-source-data` to `litespec/ext/concurrency#hash-sensitive-fingerprint-data`
- `litespec/ext/concurrency#reload-concurrent-winners` to `litespec/ext/concurrency#replay-the-winner-for-losing-concurrent-requests`
- `litespec/ext/concurrency#restrict-replay-data` to `litespec/ext/concurrency#store-only-fields-that-replay-needs`
- `litespec/ext/concurrency#retain-safe-retry-keys` to `litespec/ext/concurrency#keep-keys-until-callers-stop-retrying`
- `litespec/ext/delivery#apply-the-documented-defaults` to `litespec/ext/delivery#give-each-jobs-table-job-its-own-workflow`
- `litespec/ext/delivery#exclude-insecure-frontend-secret-carriers` to `litespec/ext/delivery#keep-frontend-secrets-out-of-arguments-files-and-layers`
- `litespec/ext/delivery#exercise-a-restore-before-the-first-production-data` to `litespec/ext/delivery#rehearse-a-restore-before-the-first-production-data`
- `litespec/ext/delivery#exercise-every-runbook-before-release` to `litespec/ext/delivery#rehearse-every-runbook-before-release`
- `litespec/ext/delivery#hold-back-example-rows-from-the-implementing-worker` to `litespec/ext/delivery#check-implementations-against-held-back-example-rows`
- `litespec/ext/delivery#keep-each-alert-threshold-to-its-requirements-measure` to `litespec/ext/delivery#tie-alert-thresholds-to-requirement-measures`
- `litespec/ext/delivery#keep-mutable-data-external` to `litespec/ext/delivery#keep-mutable-data-outside-the-container`
- `litespec/ext/delivery#keep-rollback-executable` to `litespec/ext/delivery#keep-rollback-ready-to-run`
- `litespec/ext/delivery#place-dockerfiles-beside-deployables` to `litespec/ext/delivery#place-dockerfiles-beside-their-applications`
- `litespec/ext/delivery#provide-release-data-recovery` to `litespec/ext/delivery#keep-release-data-recoverable`
- `litespec/ext/delivery#separate-frontend-configuration-classes` to `litespec/ext/delivery#separate-public-configuration-from-runtime-secrets`
- `litespec/ext/delivery#set-sufficient-shutdown-time` to `litespec/ext/delivery#allow-time-for-graceful-shutdown`
- `litespec/ext/delivery#test-released-traffic` to `litespec/ext/delivery#run-the-end-to-end-suite-after-traffic-shifts`
- `litespec/ext/delivery#use-a-repeatable-deployment` to `litespec/ext/delivery#repeat-every-deployment-from-named-inputs`
- `litespec/ext/delivery#use-protected-frontend-secret-delivery` to `litespec/ext/delivery#pass-frontend-secrets-by-mount-or-runtime-injection`
- `litespec/ext/integrations#bind-client-configuration` to `litespec/ext/integrations#configure-external-clients-through-validated-options`
- `litespec/ext/integrations#bound-transient-retries` to `litespec/ext/integrations#retry-transient-failures-with-bounded-attempts`
- `litespec/ext/integrations#exclude-provider-internals` to `litespec/ext/integrations#keep-provider-internals-from-api-responses`
- `litespec/ext/integrations#trust-inbound-integration-events-by-declared-trust` to `litespec/ext/integrations#declare-trust-for-inbound-integration-events`
- `litespec/ext/jobs#bound-job-duration` to `litespec/ext/jobs#limit-job-duration`
- `litespec/ext/jobs#bound-job-retries` to `litespec/ext/jobs#limit-job-retries-with-backoff-and-a-poison-state`
- `litespec/ext/jobs#define-business-local-clock-behavior` to `litespec/ext/jobs#state-clock-behavior-for-business-local-schedules`
- `litespec/ext/jobs#define-misfire-recovery` to `litespec/ext/jobs#state-how-missed-occurrences-recover`
- `litespec/ext/jobs#dispatch-application-behavior` to `litespec/ext/jobs#dispatch-jobs-through-application-commands-or-ports`
- `litespec/ext/jobs#exclude-direct-storage-and-http-execution` to `litespec/ext/jobs#keep-jobs-off-direct-storage-and-web-endpoints`
- `litespec/ext/jobs#provide-operational-recovery-procedures` to `litespec/ext/jobs#write-runbooks-for-inspection-replay-and-disable`
- `litespec/ext/jobs#record-reporting-need` to `litespec/ext/jobs#record-why-each-report-needs-its-infrastructure`
- `litespec/ext/jobs#restrict-completion-by-fencing-value` to `litespec/ext/jobs#allow-completion-only-with-a-current-fencing-value`
- `litespec/ext/jobs#resume-driven-work-through-the-cursor` to `litespec/ext/jobs#resume-batch-work-from-its-cursor`
- `litespec/ext/jobs#run-budget-exceeding-exports-in-worker` to `litespec/ext/jobs#run-over-budget-exports-in-the-worker`
- `litespec/ext/jobs#run-one-command-per-item-in-selection` to `litespec/ext/jobs#run-one-command-per-selected-item`
- `litespec/ext/jobs#separate-schedule-definitions` to `litespec/ext/jobs#keep-schedule-definitions-separate-from-commands`
- `litespec/ext/jobs#use-execution-scopes` to `litespec/ext/jobs#resolve-dependencies-inside-an-execution-scope`
- `litespec/ext/lifecycle#bound-purge-and-archive-work` to `litespec/ext/lifecycle#keep-purge-and-archive-work-bounded-and-observable`
- `litespec/ext/lifecycle#define-storage-lifecycle-behavior` to `litespec/ext/lifecycle#define-document-lifecycle-behavior`
- `litespec/ext/mcp#confirm-every-writing-tool` to `litespec/ext/mcp#refuse-each-write-until-the-person-confirms`
- `litespec/ext/outbox#await-target-acceptance` to `litespec/ext/outbox#wait-for-target-acceptance-before-completing`
- `litespec/ext/outbox#back-off-unavailable-store-polling` to `litespec/ext/outbox#reduce-polling-when-the-store-is-unavailable`
- `litespec/ext/outbox#bound-delivery-retries` to `litespec/ext/outbox#retry-delivery-with-bounded-backoff-and-jitter`
- `litespec/ext/outbox#classify-permanent-delivery-failure` to `litespec/ext/outbox#stop-retrying-permanent-failures-at-the-bound`
- `litespec/ext/outbox#deliver-each-event-to-each-handler-separately` to `litespec/ext/outbox#give-each-handler-its-own-delivery`
- `litespec/ext/outbox#distinguish-dependency-outage` to `litespec/ext/outbox#report-store-outages-separately-from-delivery-failures`
- `litespec/ext/outbox#exclude-manually-repeatable-reactions-and-jobs` to `litespec/ext/outbox#skip-the-outbox-for-manually-repeatable-work`
- `litespec/ext/outbox#gate-dispatch-on-readiness` to `litespec/ext/outbox#stop-dispatch-when-the-host-is-not-ready`
- `litespec/ext/outbox#plan-undispatched-rollback-records` to `litespec/ext/outbox#cover-undispatched-records-in-the-rollback-runbook`
- `litespec/ext/outbox#record-durable-delivery-behavior` to `litespec/ext/outbox#document-delivery-guarantees-before-sending`
- `litespec/ext/outbox#support-coexisting-message-producers` to `litespec/ext/outbox#read-records-from-every-coexisting-version`
- `litespec/ext/realtime#record-transport-support` to `litespec/ext/realtime#record-the-transport-and-its-platform-support`
- `litespec/ext/tenancy#bound-every-delegation-in-time` to `litespec/ext/tenancy#limit-every-delegation-in-time`
- `litespec/ext/tenancy#use-selected-tenant-storage-support` to `litespec/ext/tenancy#scope-persistence-sessions-to-the-tenant`
- `litespec/frontend/mobile/data#make-refusals-explicit` to `litespec/frontend/mobile/data#return-every-refusal-in-a-typed-result`
- `litespec/frontend/mobile/device#prefer-permissionless-system-pickers` to `litespec/frontend/mobile/device#pick-media-through-permissionless-system-pickers`
- `litespec/frontend/mobile/structure#hold-the-domain-in-the-app-only-without-a-server` to `litespec/frontend/mobile/structure#match-core-projects-to-the-product-shape`
- `litespec/frontend/mobile/testing#run-the-release-build-before-a-release` to `litespec/frontend/mobile/testing#try-the-release-build-on-hardware-before-publishing`
- `litespec/frontend/rendering#keep-address-parameters-validated-by-a-schema` to `litespec/frontend/rendering#validate-address-parameters-with-a-schema`
- `litespec/frontend/rendering#represent-route-states` to `litespec/frontend/rendering#define-every-route-state`
- `litespec/frontend/structure#keep-shared-packages-non-application-specific` to `litespec/frontend/structure#keep-shared-packages-free-of-application-code`
- `litespec/frontend/structure#locate-cross-route-values-in-the-frame` to `litespec/frontend/structure#place-cross-route-values-in-the-frame`
- `litespec/frontend/testing#mark-the-scenario-a-browser-test-proves` to `litespec/frontend/testing#mark-each-browser-test-with-the-scenario-it-proves`
- `litespec/frontend/ui#check-every-route` to `litespec/frontend/ui#pass-every-route-suite-check`
- `litespec/frontend/ui#derive-each-page-type-from-a-named-reference` to `litespec/frontend/ui#name-the-reference-behind-each-page-type`
- `litespec/frontend/ui#govern-behavior-packages` to `litespec/frontend/ui#keep-each-behavior-package-justified-and-on-baseline`
- `litespec/frontend/ui#prove-ui-behavior-and-appearance` to `litespec/frontend/ui#name-the-evidence-for-each-ui-change`
- `litespec/frontend/ui#record-a-freestyle-route` to `litespec/frontend/ui#justify-each-freestyle-route-with-a-decision-record`
- `litespec/frontend/ui#record-a-frontend-outside-the-controlled-contract` to `litespec/frontend/ui#declare-each-frontend-outside-the-contract`
- `litespec/frontend/ui#select-one-visual-authority` to `litespec/frontend/ui#record-one-shared-ui-package`
- `litespec/frontend/ui#use-the-product-profiles` to `litespec/frontend/ui#select-a-density-profile-for-each-frontend`
- `litespec/frontend/web/rendering#define-route-metadata-deliberately` to `litespec/frontend/web/rendering#declare-titles-and-indexing-policy-on-every-route`
- `litespec/frontend/web/svelte/stack#name-the-module-extension-on-each-import` to `litespec/frontend/web/svelte/stack#write-the-file-extension-in-each-import`
- `litespec/frontend/web/ui#restrict-css-decisions` to `litespec/frontend/web/ui#write-tailwind-classes-with-semantic-tokens-only`
- `litespec/frontend/web/ui#track-source-changes` to `litespec/frontend/web/ui#record-primitive-sources-in-the-source-lock`
- `litespec/quality/operations#keep-runbooks-near-project-documentation` to `litespec/quality/operations#place-each-runbook-at-its-scope`
- `litespec/quality/security#minimize-sensitive-data` to `litespec/quality/security#limit-each-use-case-to-its-declared-fields`
- `litespec/quality/testing#use-evidence-rather-than-one-coverage-target` to `litespec/quality/testing#refuse-one-coverage-percentage-as-sufficient-testing`
- `litespec/workspace/dependencies#keep-the-approved-web-ui-dependency-boundary` to `litespec/workspace/dependencies#take-web-ui-packages-from-the-approved-baseline`
- `litespec/workspace/naming#name-asynchronous-operations-completely` to `litespec/workspace/naming#reveal-asynchrony-and-cancellation-in-operation-names`
- `litespec/workspace/structure#keep-consumer-documentation-at-the-root` to `litespec/workspace/structure#keep-project-records-under-the-root-litespec-directory`

Extensions added (1):

- `queues`

Extensions removed (2):

- `layer-projects`
- `stage`

<!-- /generated:provision-diff -->

## v6.0.0

LiteSpec 6.0.0 gives constraints their sources, scope and rules, and gives journeys a start, steps and branches. It also moves each feature into the problem it answers, declares modules only in Behavior, puts each child record in a typed folder, ties each research finding to its source, and publishes release candidates from release branches. It adds the vision record, puts each named record and each requirement in its own folder, gives every explanation file plain sections behind an opening paragraph, states each problem by its failure cases, and has each feature state its boundaries for the people who build and test it. It is a major release: it changes the record layout, renames and retires the provision IDs that Part H lists, and removes fields, so a consumer moves its records, rewrites its explanation files and follows Part I. It also adds three guides: Write a Constraint, Write a Journey and Write a Research Record.

### Part A: The Vision Record

- A `vision` record lives at `.litespec/intent/visions/<name>/<name>.yaml`, at product level only, with `summary` and `statement` and no date, priority or status. `litespec new vision <name>` scaffolds it. When the project lists `intent` in `explanations.layers`, its explanation file carries the sections Direction, Effect on Current Design and Trigger.
- A decision names a vision in `basedOn`, and a requirement names one in `cites`. Nothing else links to a vision, and a vision links to nothing.
- A vision reaches no Behavior by design: no reach check reads it, so it raises no `intent-unreached` or other planned-Intent finding. `litespec/core/intent#reach-every-intent-record-with-behavior` now excepts it.
- `litespec/core/principles#require-current-complexity-activation` states that a vision never justifies a package, project, wrapper, process, cache, queue or distributed pattern on its own. A vision may justify the shape of a record or model.
- The book lists visions beside goals in the Why group, under the product. `prose-missing` covers a vision with no `statement`.
- Claimwick carries `vision/contractor-expenses`.

The four rules are `litespec/core/intent#state-each-vision-in-its-fields`, `litespec/core/intent#keep-dates-and-priorities-out-of-visions`, `litespec/core/intent#build-nothing-from-a-vision`, and `litespec/core/intent#cite-the-vision-a-decision-keeps-open`. Decision 0041, Record Product Vision as a Vision Record, records why.

### Part B: The Book on Narrow Screens

- Below 1024 pixels the navigation becomes a drawer: a menu button in the top bar opens it, and choosing a record, the backdrop or Escape closes it and returns focus to the button. The page behind an open drawer is inert. Above 1024 pixels the columns stand side by side as before.
- The top bar drops the test run and completion widgets below 1280 pixels, and the button labels, the book label and the shortcut hint below 768 pixels. The search box placeholder reads "Find a record".
- Record pages fit a 320-pixel screen with no sideways scroll: long identifiers, paths and refusal codes wrap, label and value grids stack, and wide tables scroll inside their own frame.
- The Intent tree and the trace rail show visions under the product, each with the decisions based on it and the requirements citing it.

### Part C: Named Records in Their Own Folders

- Each named record lives in its own folder named for it, `<container>/<name>/<name>.yaml`, and its explanation file `<name>.md` sits in that folder beside the record file. The folder name equals the file stem. `litespec/core/model#write-every-record-in-its-home` and `litespec/core/model#explain-each-record-beside-its-record-file` state the rule.
- A named record placed flat in its type's folder is a `record-placement-invalid` finding. A named record whose folder name differs from its file stem is a `record-folder-drift` finding, a level G finding.
- Fixed-name files keep their places: `product.yaml`, `module.yaml`, `aggregate.yaml`, `entry-point.yaml`, `permissions.yaml`, `glossary.yaml`, rule sets, settings, events and the list files. A result or scenario sits in its use-case file, and a requirement sits in its own folder inside its feature's folder, as Part F states.
- `litespec new` writes a named record in the folder layout, and `litespec rename` moves a record with its folder.
- No migration tool ships. Part I lists the move each consumer makes.

Decision 0042, Keep Each Named Record in Its Own Folder, records why.

### Part D: Explanation Files with Plain Sections

- Each record type has one fixed list of plain noun sections, in the order the section table on the writing page gives. The table covers every type with an explanation file, including the container pages: permission lists, setting sets, rule sets, retention schedules, runbooks and departures. A section is named by one or two plain nouns, such as Cost, Evidence or Revisit Conditions.
- Each explanation file opens with one paragraph of prose between its H1 and its first H2. `explanation-opening-missing` reports an explanation with no such paragraph, at level A.
- `explanation-missing` reports a missing fixed section, and it now also reports sections that come out of order. Its message names the section.
- The old headings, such as What It Means, Who Has It, Answer and Known Limits, are invalid in 6.0.0. The generic explanation template is removed. `litespec new` writes each type's own explanation template, with the title filled in.
- The book shows the opening paragraph above the sections, and it sets Limits apart as risks, Example as an example and Open Points as questions. It no longer moves a Why section out of the body.

Decision 0043, Use Plain Noun Sections and an Opening Paragraph, records why.

### Part E: Problems Stated by Failure Case

- A problem explanation carries Affected Users, Failure Cases, Example (optional), Current Workaround, Cost, Evidence and Resolution Criteria, in that order. `explanation-missing` reports a problem explanation that still carries Resolution, or that has no Failure Cases.
- The opening paragraph states what the sufferer cannot reliably do, the gap that causes it, and what they lose or risk. Failure Cases gives each failure that can happen on its own its own entry, with how the sufferer finds it and what follows when they do not.
- Each claim stays within its evidence, and an estimate is named as an estimate. An evidence entry shows that the sufferer has the pain; a file format, an API or another product only shows that a solution can be built, and it belongs in research that a feature cites.
- Resolution Criteria gives one observable outcome for each failure case and names no feature or implementation. The feature that delivers it sits in the folder of the problem it answers.
- Current Workaround names the burden of the workaround, and Cost keeps time, money and risk apart.
- A problem title names the failure or its consequence, never a theme or a feature.
- `litespec new problem` writes the new sections, and the Claimwick problem follows them.
- The new how-to [Write a Problem](docs/guide/write-a-problem.md) walks through each section with a weak and a strong Claimwick example.

The five new rules are `litespec/core/intent#open-each-problem-with-its-gap-and-consequence`, `litespec/core/intent#state-each-failure-case-on-its-own`, `litespec/core/intent#claim-no-more-than-the-evidence-shows`, `litespec/core/intent#keep-feasibility-out-of-problem-evidence` and `litespec/core/intent#resolve-every-failure-case-with-an-outcome`. `litespec/core/intent#explain-every-problem-in-four-sections` becomes `litespec/core/intent#explain-every-problem-in-its-fixed-sections`, under the heading Explain Every Problem in Its Fixed Sections. Decision 0044, State Each Problem by Its Failure Cases, records why.

### Part F: Requirements in Their Own Folders

- Each requirement lives in its own folder inside its feature's folder: `.litespec/intent/modules/<module>/features/<feature>/<name>/<name>.yaml`. Its identifier stays `requirement/<feature>.<name>`, and its feature segment names the feature whose folder holds it; `identifier-invalid` reports one that differs.
- A feature record file holds no requirement entries. A requirement entry left in a feature file is a `record-invalid` finding.
- A requirement's explanation file `<name>.md` is optional on every layer, even when the project lists `intent` in `explanations.layers`. When it exists, it opens with a paragraph and may carry an Example; acceptance stays in the Behavior scenarios that fulfill the requirement.
- `litespec new requirement <feature>.<name>` writes the requirement's folder inside the feature's folder, with no explanation file. `litespec new entry` no longer takes a requirement. `litespec rename` moves a requirement's folder, and renaming a feature moves its folder with the requirement folders inside.
- The book brings a feature's requirements together on the feature's page, as before.
- Claimwick, the templates and the test fixtures hold each requirement in its own folder.

Decision 0045, Keep Each Requirement in Its Own Folder, records why.

### Part G: Features Written for the Builder and the Tester

- A feature explanation carries Capabilities, Success Criteria, Boundaries and an optional Example. Boundaries is required: what the feature leaves to other features, the features it depends on, and each open question its owner has not decided. `explanation-missing` reports a feature explanation without it, or one that still carries Limits.
- Success Criteria lists only outcomes the feature brings about on its own and a tester can check.
- A feature or requirement explanation describes only behavior that the feature's requirements state.
- A requirement statement states one behavior a tester can pass or fail on its own, with each condition, date and amount its result depends on. A rationale states why the requirement exists, adds no behavior, and claims no more than its reason supports. A requirement title names the specific behavior or fact it requires.
- Every record names the operation it describes instead of a loose verb such as handles, holds, touches or lands, and states the observable behavior or the measured target instead of an intensifier such as seamlessly, instantly or at once.
- The feature template and the Claimwick approval feature follow, and [Write a Feature Under a Problem](docs/guide/write-a-feature-under-a-problem.md) walks through the requirement and explanation steps with a weak and a strong Claimwick example.

The six new rules are `litespec/core/intent#state-the-boundaries-of-every-feature`, `litespec/core/intent#state-success-criteria-the-feature-controls`, `litespec/core/intent#add-no-behavior-in-an-explanation-file`, `litespec/core/intent#state-one-testable-behavior-in-each-requirement`, `litespec/core/intent#keep-each-rationale-to-its-reason` and `litespec/core/writing#name-observable-behavior-instead-of-intensifiers`. `litespec/core/writing#use-literal-words` also refuses loose verbs. Decision 0046, Write Features for the Builder and the Tester, records why.

### Part H: Provision Changes Since v5.2.0

The generated list below compares the provision index at v5.2.0 with this release. The comparison matches a provision by its page and statement, so a renamed or moved identifier appears as one removal and one addition. These identifiers changed in 6.0.0:

- `litespec/core/access#record-every-role-hold-with-its-scope` is renamed to `litespec/core/access#record-every-role-an-actor-holds`.
- `litespec/core/access#state-the-scope-of-every-permission` is retired. The role holds the one scope, so no permission states one.
- `litespec/core/intent#explain-every-problem-in-four-sections` is renamed to `litespec/core/intent#explain-every-problem-in-its-fixed-sections`.
- `litespec/core/intent#group-product-records-by-module` is replaced by `litespec/core/intent#group-product-records-by-problem`.
- `litespec/core/intent#name-each-sibling-problem-or-merge-it` is retired, and `litespec/core/intent#answer-one-problem-with-each-feature` replaces it.
- `litespec/core/intent#shape-each-module-page` moves to `litespec/core/behavior#shape-each-module-page`.
- `litespec/frontend/testing#name-the-journey-a-journey-test-walks` is renamed to `litespec/frontend/testing#name-the-journey-a-journey-test-covers`.

Six finding codes are new in the schema or the model check: `constraint-effect-uncited`, `constraint-effect-unlisted`, `finding-source-missing`, `permission-verb-unknown`, `scope-term-missing` and `template-value-unchanged`. The rest of the new codes are listed below.

- New: `constraint-effect-uncited`, `constraint-effect-unlisted`, `explanation-opening-missing`, `explanation-section-unknown`, `finding-source-missing`, `identifier-title-invalid`, `permission-verb-unknown`, `record-folder-drift`, `record-key-duplicate`, `scope-term-missing`, `source-links-drift`, `source-links-missing`, `template-value-unchanged`, `term-meaning-invalid`, `term-verb-duplicate`, `use-case-verb-invalid` and `use-case-verb-unknown`.
- New provision `litespec/core/model#name-each-identifier-from-its-title`, checked as `identifier-title-invalid`: every page record takes its identifier name from the words of its title in title order, and names its record file for its identifier name or for the fixed file name of its type. Decision 0061, Name Each Identifier from Its Title, records why.

<!-- generated:provision-diff -->
### Provision Changes Since v5.2.0

Added (41):

- `litespec/core/access#define-each-scope-level-in-the-glossary`
- `litespec/core/access#name-each-permission-by-a-use-case-verb`
- `litespec/core/access#record-every-role-an-actor-holds`
- `litespec/core/authoring#copy-each-template-from-the-example`
- `litespec/core/behavior#shape-each-module-page`
- `litespec/core/intent#add-no-behavior-in-an-explanation-file`
- `litespec/core/intent#answer-one-problem-with-each-feature`
- `litespec/core/intent#branch-a-journey-only-between-its-steps`
- `litespec/core/intent#build-nothing-from-a-vision`
- `litespec/core/intent#cite-a-source-for-each-constraint-rule`
- `litespec/core/intent#cite-the-vision-a-decision-keeps-open`
- `litespec/core/intent#claim-no-more-than-the-evidence-shows`
- `litespec/core/intent#explain-every-problem-in-its-fixed-sections`
- `litespec/core/intent#group-product-records-by-problem`
- `litespec/core/intent#keep-dates-and-priorities-out-of-visions`
- `litespec/core/intent#keep-each-rationale-to-its-reason`
- `litespec/core/intent#keep-feasibility-out-of-problem-evidence`
- `litespec/core/intent#open-each-problem-with-its-gap-and-consequence`
- `litespec/core/intent#reach-each-finish-condition-through-a-step`
- `litespec/core/intent#resolve-every-failure-case-with-an-outcome`
- `litespec/core/intent#split-records-whose-parts-change-separately`
- `litespec/core/intent#state-each-constraint-rule-with-its-condition`
- `litespec/core/intent#state-each-failure-case-on-its-own`
- `litespec/core/intent#state-each-journey-blocker-as-a-step-failure`
- `litespec/core/intent#state-each-research-finding-with-its-source`
- `litespec/core/intent#state-each-vision-in-its-fields`
- `litespec/core/intent#state-one-testable-behavior-in-each-requirement`
- `litespec/core/intent#state-success-criteria-the-feature-controls`
- `litespec/core/intent#state-the-boundaries-of-every-feature`
- `litespec/core/intent#state-what-the-product-assumes-each-actor-knows`
- `litespec/core/intent#state-when-each-journey-starts`
- `litespec/core/maintaining#publish-each-release-candidate-from-its-release-branch`
- `litespec/core/model#keep-shape-rules-in-the-schema`
- `litespec/core/model#name-each-identifier-from-its-title`
- `litespec/core/model#place-child-records-in-typed-folders`
- `litespec/core/writing#keep-release-phases-and-plans-out-of-records`
- `litespec/core/writing#keep-the-conditions-when-shortening`
- `litespec/core/writing#name-observable-behavior-instead-of-intensifiers`
- `litespec/core/writing#open-each-explanation-file-with-a-paragraph`
- `litespec/core/writing#take-each-example-detail-from-the-cast-or-a-cited-record`
- `litespec/frontend/testing#name-the-journey-a-journey-test-covers`

Removed (7):

- `litespec/core/access#record-every-role-hold-with-its-scope`
- `litespec/core/access#state-the-scope-of-every-permission`
- `litespec/core/intent#explain-every-problem-in-four-sections`
- `litespec/core/intent#group-product-records-by-module`
- `litespec/core/intent#name-each-sibling-problem-or-merge-it`
- `litespec/core/intent#shape-each-module-page`
- `litespec/frontend/testing#name-the-journey-a-journey-test-walks`

Renamed (0):

None.

Extensions added (0):

None.

Extensions removed (0):

None.

<!-- /generated:provision-diff -->

### Part I: Upgrading a Consumer

LiteSpec does no migration. Each step below is a manual edit, in the order given, because later steps depend on earlier moves.

1. Install `LiteSpec.Cli` 6.0.0 (`dotnet tool update LiteSpec.Cli --version 6.0.0`) or rerun the install script, and set `reviewedLiteSpecVersion: 6.0.0` in `.litespec/project.yaml`. To test first, install the candidate `6.0.0-rc.<n>` and set the same field to it.
2. Move each module from `.litespec/intent/modules/<module>/` to `.litespec/behavior/modules/<module>/`: move its `module.yaml` and its explanation with it. Move each goal, constraint, decision and research record that sat under an Intent module to product level, under `.litespec/intent/`.
3. Move each feature folder into the folder of the one problem it answers, `.litespec/intent/problems/<problem>/features/<feature>/`, and remove its `solves` field. A feature that answers two problems becomes two features, or its two problems merge into one. A feature that answers no problem gets the problem its evidence supports, or its requirements move into the features that need them.
4. Move each requirement entry out of its feature file into its own folder under the feature's `requirements/` folder: `requirement/approval.nobody-approves-own-expense` in `features/approval/approval.yaml` becomes `features/approval/requirements/nobody-approves-own-expense/nobody-approves-own-expense.yaml`, with `type`, `id` and `title` first and the entry's other fields after them. Remove the moved entries from the feature file.
5. Move each aggregate folder under its module's `aggregates/` folder, each use case under its aggregate's `use-cases/` folder, and each aggregate's event and rule files under the aggregate's `events/` and `rules/` folders. A use case that belongs to no aggregate moves to `behavior/modules/<module>/use-cases/<use-case>/`. No tool moves them, so move each one by hand.
6. Move each named record and its explanation file into the record's own folder: `<container>/<name>/<name>.yaml` and `<container>/<name>/<name>.md`. For example, `.litespec/intent/goals/decided-in-days.yaml` moves to `.litespec/intent/goals/decided-in-days/decided-in-days.yaml`.
7. Give each constraint a `sources` list, one entry per source with its `key`, `url` and `readOn`, and remove the record-level `readOn`. Add `effectiveFrom` where the rule starts on a date, and `basedOn` where the rule rests on research. Rewrite its explanation to Scope, Rules, Effect on the Product and Open Points: state each Rules paragraph's condition and cite its source key, and name in Effect on the Product each requirement that cites the constraint. Remove the Source section.
8. Give each journey `startedWhen`, and replace `betweenSteps` with `branches`, one entry for each path that leaves the order of the steps, with its `step`, `condition` and `nextStep`. Move any action that sat in `betweenSteps` into the step list. Remove a step that serves another journey, and split a journey that takes two actors or two goals. Rewrite the explanation to Example and Branches: rename Variations to Branches, and move Preconditions into the Example. Make each clause of `finishedWhen` reached by a step, and remove a clause that no step reaches.
9. Give each research record's Findings paragraph a source key as a reference link, and say in the sentence whether the source states the claim or the writer infers it. Remove the high, medium and low confidence labels. Move each point the sources do not establish from Source Notes into Open Questions, each as a question. Shorten the `conclusion` to what the sources establish, with its conditions, and remove what follows for the product. Add `publishedOn` where the source states its publication date.
10. Give each actor explanation a Knowledge section in place of Access. Move the product rules out of the actor, into the requirements they belong to. Remove the Access section.
11. Remove `scope` from each `holds` entry and from each permission entry. A role that one actor holds at two levels becomes one role per level. Add a glossary term titled with each role's scope level and "Scope", stating what the level covers in the product. Rename each permission whose leading verb is not in the use-case verb table of the Use Cases page, a glossary verb of kind command or either, or `manage`, and update each `allows` list, each `grant` and each code mark that names it. In each permission-list explanation, replace Future Roles with Boundaries.
12. Move each project use-case verb from its convention page into the glossary as a verb term: the term names the verb, `verbKind` names the kind it opens (`command`, `query`, or `either`), and `meaning` states the act in one sentence. Then delete the convention page. A verb term that restates a baseline verb is removed instead, since a project never redefines one.
13. Rewrite each explanation file to the fixed sections of its record type, as Parts D, E, G, J, K, P and R and the section table on the writing page state. Keep the H1, write one opening paragraph, and keep each free section after the fixed sections. The old headings are invalid, and no tool converts them. `litespec new` shows the template for each type. In a problem, give each failure its own entry under Failure Cases, give each failure case one outcome under Resolution Criteria, and move an evidence entry that only shows a solution can be built into research. In a feature, replace Limits with Boundaries, and add the features it depends on and its open questions.
14. Remove each release phase and each future plan from every explanation: a destination becomes a vision, an open choice becomes an owner question or a backlog item, and a limit of the current product stays a stated limit.
15. Replace each citation of a retired or renamed provision with its new identifier, including `follows` and `proves` marks in code and tests: `litespec/core/intent#explain-every-problem-in-four-sections` becomes `litespec/core/intent#explain-every-problem-in-its-fixed-sections`, `litespec/core/intent#name-each-sibling-problem-or-merge-it` becomes `litespec/core/intent#answer-one-problem-with-each-feature`, `litespec/core/access#record-every-role-hold-with-its-scope` becomes `litespec/core/access#record-every-role-an-actor-holds`, `litespec/core/intent#group-product-records-by-module` becomes `litespec/core/intent#group-product-records-by-problem`, `litespec/core/intent#shape-each-module-page` becomes `litespec/core/behavior#shape-each-module-page`, and `litespec/frontend/testing#name-the-journey-a-journey-test-walks` becomes `litespec/frontend/testing#name-the-journey-a-journey-test-covers`. Drop each citation of `litespec/core/access#state-the-scope-of-every-permission`, which is retired.
16. To record where the product is heading, run `litespec new vision <name>` and name the vision in `basedOn` on each decision made to keep it open.
17. Run `litespec model render`, which writes each source-links region and the consumer's agent protocol, and then `litespec model check`. Fix each finding, including `record-placement-invalid`, `record-parent-missing`, `record-folder-drift`, `record-invalid`, `field-invalid`, `identifier-invalid`, `identifier-title-invalid`, `record-key-duplicate`, `explanation-missing`, `explanation-opening-missing`, `explanation-section-unknown`, `finding-source-missing`, `source-links-missing`, `source-links-drift`, `constraint-effect-unlisted`, `constraint-effect-uncited`, `scope-term-missing`, `permission-verb-unknown`, `template-value-unchanged`, `use-case-verb-unknown`, `use-case-verb-invalid` and `term-verb-duplicate`. A value a template copied and you kept is a finding, so write the value you decided.
18. Rename each record the new `identifier-title-invalid` finding reports with `litespec rename`, or retitle it when the title is the wrong one.

### Part J: Constraints with Sources, Scope and Rules

- A constraint carries `sources`, one entry per source with its `key`, `url` and `readOn`, in the shape research uses. The record-level `readOn` is removed. Each source may carry `publishedOn`, the date the source states it was published.
- A constraint carries `effectiveFrom`, the date its rule starts to bind, and `basedOn`, the research the rule rests on. `authority` names the issuing body in a few words, and `summary` states the rule, its condition and whom it binds.
- A constraint explanation holds Scope, Rules, Effect on the Product and Open Points (optional), in that order. Source is removed. Each Rules paragraph keeps its condition and cites a source key as a reference link `[text][key]`.
- Effect on the Product names by identifier each requirement that holds the rule's effect. `model check` reports `constraint-effect-unlisted` for a requirement that cites the constraint but is not named there, and `constraint-effect-uncited` for a requirement named there that does not cite it.
- `model render` writes a `source-links` region at the end of each research and constraint explanation, one link definition per source with its read date. `model check` reports `source-links-missing` and `source-links-drift`, so each citation resolves in a plain Markdown preview.
- The source checks `source-key-unknown`, `source-link-bare` and `source-unused` read constraint explanations as well as research. `record-invalid` reports a constraint with no source, and `field-invalid` reports a `basedOn` that names no research record.
- New provisions `litespec/core/intent#cite-a-source-for-each-constraint-rule` and `litespec/core/intent#state-each-constraint-rule-with-its-condition`. `litespec/core/intent#answer-every-constraint`, `litespec/core/layers#enter-world-facts-as-cited-research-or-constraints` and `litespec/core/layers#build-ahead-of-a-regulation-only-under-its-constraint` are reworded for the fields. Decision 0047, Cite a Source for Each Constraint Rule, records why.
- Claimwick's constraint `tax-receipt-retention` states its source with a key, a URL and its read date, and its Effect on the Product names the two requirements that hold it.

### Part K: Journeys with a Start, Steps and Branches

- A journey carries `startedWhen`, the observable state in which it starts, in one sentence. The Preconditions section is removed, and any detail of one occasion moves to the Example.
- `betweenSteps` is removed. A journey carries `branches`, a list where each entry names the `step` it leaves, the `condition` under which it leaves, and the `nextStep` it continues at, or `end`. A branch states no action of its own, so an action on a path is a step.
- A blocker is the `failure` of the step it blocks, with the requirement that holds it cited. A step names one act of its performer, may name the product's observable response after that act, and a second act of a person is a second step.
- `record-invalid` reports a journey without `startedWhen`, a journey still holding `betweenSteps`, or a branch missing `step`, `condition` or `nextStep`. `field-invalid` reports a branch whose `step` or `nextStep` names no step of its journey. `record-key-duplicate` reports two branches that repeat a step and next step.
- The journey explanation sections are Example and Branches. Variations is renamed to Branches, which explains when each branch happens and why, with an occasion.
- The book renders `startedWhen` before the steps, and each branch as a sentence after them.
- New provisions `litespec/core/intent#state-when-each-journey-starts`, `litespec/core/intent#branch-a-journey-only-between-its-steps`, `litespec/core/intent#reach-each-finish-condition-through-a-step` and `litespec/core/intent#state-each-journey-blocker-as-a-step-failure`. Decision 0048, State the Start and Branches of Each Journey, records why.
- Claimwick's reference journey is split by actor and goal, as the plan's review asked. `journey/submit-and-reimburse` keeps the employee's path from submission to reimbursement, with handoffs to the manager and finance. The threshold step moves to `journey/set-receipt-threshold`, taken by the manager, and the export step moves to `journey/export-approved-expenses`, taken by finance. The erasure step leaves every journey, and its use case fulfills its requirement without serving a step, as a scheduled promise does.

### Part L: Literal Journey Words and Concrete Consequences

- An actor takes a journey, and nothing walks. The Links table reads "is taken by" and "takes", and the journey sense of "walk" is gone from the records, templates, glossary and standards pages.
- `litespec/frontend/testing#name-the-journey-a-journey-test-walks` is renamed to `litespec/frontend/testing#name-the-journey-a-journey-test-covers`, and every citation and `follows` mark moves with it.
- Record openings state the consequence at stake and for whom, not "why it matters" followed by a general reason. The opening prompts of the actor, goal, notification, product and README templates change, and `explanation-opening-missing` names the consequence in its fix text. Documentation pages keep their Intent sections, since those describe LiteSpec pages rather than records.
- `litespec/core/writing#use-literal-words` gains a weak and strong example: "It matters because the figures must be correct" against "A wrong box amount means a correction return, and a late return means a fine." Decision 0049, Use Literal Journey Words and Concrete Consequences, records why.

### Part M: Features Inside the Problem They Answer

- A feature sits in the folder of the one problem it answers: `.litespec/intent/problems/<problem>/features/<feature>/<feature>.yaml`, with its explanation beside it. The `solves` field is removed, because the folder names the problem.
- Each requirement sits in the `requirements/` folder of its feature, as Part X states. Identifiers do not change, because `feature/<name>` and `requirement/<feature>.<name>` name no problem, so moving a feature to another problem moves its folder and changes no citation.
- `advances` stays a list, since one feature can move several goals.
- `record-invalid` reports a feature that still carries `solves`. `record-placement-invalid` reports a feature outside a problem's `features/` folder, and `record-parent-missing` reports a feature in the folder of no problem record.
- Retired: `litespec/core/intent#name-each-sibling-problem-or-merge-it`. New: `litespec/core/intent#answer-one-problem-with-each-feature`. Two pains that one offer answers are one problem, and an offer that answers two pains is two features.
- The Links table reads "answers" and "is answered by" for the folder parent. The book's Intent tree shows each problem with its features, and `litespec new feature` takes the problem the feature answers.
- The guide `docs/guide/write-a-feature-under-a-module.md` is renamed to `docs/guide/write-a-feature-under-a-problem.md`. Decision 0050, Put Each Feature Inside the Problem It Answers, records why.

### Part N: Modules Declared Only in Behavior

- A module is declared in Behavior: `.litespec/behavior/modules/<module>/module.yaml`, with its explanation beside it. Its fields are unchanged. The module sections are Responsibilities, Boundaries and Example, and they sit under Behavior.
- `.litespec/intent/modules/` is removed. Goals, problems, constraints, decisions, research and features sit at product level, under their problems where they belong. `record-placement-invalid` reports any file under `intent/modules/`.
- The Intent tree groups by problem. The Behavior tree keeps grouping aggregates by module. `litespec new module` writes into `behavior/modules/<module>/`.
- The provision `litespec/core/intent#shape-each-module-page` moves to `litespec/core/behavior#shape-each-module-page`, and `litespec/core/intent#group-product-records-by-module` is replaced by `litespec/core/intent#group-product-records-by-problem`.
- Decision 0051, Declare Modules Only in Behavior, records why.

### Part O: Research Findings Tied to Their Sources

- `readOn` states when someone read a source. The date a rule takes effect and the period a figure covers are claims a finding states with its source. `publishedOn` carries the date the source states, and the book shows it beside `readOn`.
- Each paragraph under Findings states one claim with its conditions, cites its source key as a reference link, and says whether the source states the claim or the writer infers it. The high, medium and low confidence labels are removed.
- `model check` reports `finding-source-missing` for a Findings paragraph with no source link. The existing source checks cover the keys.
- The research sections are Findings, Open Questions (optional) and Source Notes (optional). Open Questions holds what the sources do not establish, each as a question that names what to investigate. Source Notes holds what each source covers and where sources disagree.
- The `conclusion` states in one sentence what the sources establish, with the conditions the findings carry, and leaves out what follows for the product.
- Findings answer only the record's `question`. The shared split rule in Part Q applies, so a record that answers two questions becomes two records.
- New provision `litespec/core/intent#state-each-research-finding-with-its-source`. Decision 0052, Tie Each Research Finding to Its Source, records why.

### Part P: Actors by Responsibility and Knowledge

- The actor sections are Responsibilities, Knowledge and Example (optional). Access is removed. The actor's record names its role in `holds`, and the role's own files state the permissions and why that scope fits.
- Responsibilities states the tasks and decisions the actor answers for, and no product rule. Knowledge states what the product assumes the actor knows, as an assumption of the product and not a fact about every user.
- New provision `litespec/core/intent#state-what-the-product-assumes-each-actor-knows`. Decision 0053, Describe Actors by Responsibility and Knowledge, records why.

### Part Q: Rules Shared by Every Record Type

- `litespec/core/intent#add-no-behavior-in-an-explanation-file` applies to every Intent explanation file, not only features and requirements. An explanation describes only what its record's fields and the records it cites state.
- `litespec/core/writing#keep-the-conditions-when-shortening` (new): a summary, conclusion, statement or opening paragraph keeps each condition of the claim it shortens.
- `litespec/core/writing#take-each-example-detail-from-the-cast-or-a-cited-record` (new): each name, date and number in an Example comes from the example cast or from a record the example's own record cites.
- `litespec/core/intent#split-records-whose-parts-change-separately` (new): a constraint or research record holds claims that share one question or source, one scope and one rate of change, so a claim verified or changed separately sits in its own record.
- `litespec/core/writing#keep-release-phases-and-plans-out-of-records` (new): a record states the product as it stands. A destination is a vision, an open choice is an owner question or a backlog item, and a limit stays a limit.
- The Example prompt of each template becomes one sentence in the same form, and no prompt opens with "Walk" or asks for numbers "worked through". Decision 0054, Write Each Shared Record Rule Once, records why.

### Part R: Access Records with One Scope and Literal Permissions

- A role's `scope` is the one scope of its grant. `scope` is removed from each `holds` entry and from each permission, so `holds` is a list of role identifiers. Which tenant or team a person holds a role for is runtime data held by role assignments. A permission that two roles allow at different scopes takes its reach from the role the caller holds.
- Each scope level a role names needs a glossary term titled with the level and "Scope", stating what the level covers in this product. `model check` reports `scope-term-missing` for a role scope with no such term. A role held at two levels becomes one role per level.
- Each permission name opens with a verb from the use-case verb table or with `manage`. `model check` reports `permission-verb-unknown` for any other. Claimwick's `submit-expense` is now `request-expense`, `reimburse-expense` is `post-reimbursement`, and `erase-personal-data` is `remove-personal-data`.
- The permission-list explanation sections are Risk and Boundaries (optional). Boundaries states what a reader might assume a permission allows but it does not. Future Roles is removed.
- `litespec/core/access#record-every-role-hold-with-its-scope` is renamed to `litespec/core/access#record-every-role-an-actor-holds`, and `litespec/core/access#state-the-scope-of-every-permission` is retired. `litespec/core/access#define-each-scope-level-in-the-glossary` and `litespec/core/access#name-each-permission-by-a-use-case-verb` are new.
- The Access page states once that a permission allows a call and the call still meets every rule, so holding a permission never bypasses a refusal. Decision 0055, State Each Access Scope Once, records why.

### Part S: Templates Copied from Claimwick

- Each YAML template is a copy of one Claimwick record. `litespec-maintain validate` reports a template that differs from its source (`TEMPLATE_SOURCE_DIFFERS`) or names none (`TEMPLATE_SOURCE_MISSING`), so every link in a template resolves in one coherent product. Claimwick gains the records the templates copy: an approver reminder requirement, a retention schedule and an expense-submitted integration event.
- `litespec new` writes a marker line that names its template. `model check` reports `template-value-unchanged` for a scaffolded record that still keeps a template's text or number, so a copied value never passes for a decision.
- The use-case template splits into a command template and a query template. `litespec new use-case <id> --kind command|query` chooses one. The command template keys its idempotency on a request identifier declared in its input, so two expenses of one amount do not collide.
- Explanation prompts follow the record's kind. Aggregate Lifecycle covers states for an entity and creation, change and removal for a store. The job section becomes Invocation, covering scheduled, one-off and manual runs. The retention schedule's Legal Basis becomes Basis, covering statute, business and engineering. Entry point Authentication asks how callers prove identity, and setting set Defaults asks for the default, range, setter and when a change applies.
- The product's Direction section is optional and points to vision records. The goal prompt asks for the population, the start and end events, the measurement window and the exclusions. The decision template drops the date, which Git holds. The README's actor row drops "or system".
- The design contract states the accessible rule: buttons for actions, links for navigation, native controls for input, and `role=alert` only for failures that need attention. Motion timings are defaults, not a rule that every state change takes 0 ms. Decision 0056, Copy Each Template from Claimwick, records why.

### Part T: CLI and Check Fixes

- `litespec new` writes one Title Case title to the YAML `title` and to the explanation H1 for every type. A test runs `new` for every listed type.
- `new aggregate` has a scaffold home, and `new journey` has a template. `new use-case` takes `--kind`.
- `litespec rename` keeps a record at its level and in its owner's folder. A new identifier that names another owner is refused unless `--move` is given. A record folder moves whole with its supporting files, and every destination is checked before anything moves.
- The explanation checks read the Markdown block structure. A comment or a code fence no longer counts as the opening paragraph, a heading inside a fence is not a section, and section findings point at the explanation file. `explanation-section-unknown` reports an H2 outside the type's section table ahead of a fixed section, and a free section after the fixed sections passes.
- Claimwick renames five requirements to take their names from their titles: `requirement/approval.erase-after-retention` becomes `requirement/approval.erased-after-retention`, `requirement/approval.no-self-approval` becomes `requirement/approval.nobody-approves-own-expense`, `requirement/approval.receipt-over-threshold` becomes `requirement/approval.above-threshold-receipt`, `requirement/approval.single-reimbursement` becomes `requirement/approval.expense-reimbursed-at-most-once`, and `requirement/approval-reminders.month-end-submissions` becomes `requirement/approval-reminders.month-end-submission`.
- The LiteSpec workflow checks pushes to `release/*`, the branch a release is prepared on.
- In the book, choosing a record in the narrow-screen drawer closes it, including the record already selected, and focus returns to the menu button.
- `docs/core/maintaining.md` no longer promises that a pinned consumer keeps resolving old schema `$id` URLs. The site serves one release, and the CLI carries its schemas offline.

### Part U: Writing Rules Narrowed to Where They Improve Meaning

- `litespec/core/writing#put-the-actor-first` applies to a sentence that describes an action. A sentence that states a fact, a state or a calculation may take its object or its condition as the subject. Passive voice that hides who acts is still a finding.
- `litespec/core/writing#write-for-the-reader-who-arrives-cold` applies to the opening paragraph of a record, an explanation file or a page, and each section answers its own question without repeating it.
- `litespec/core/writing#keep-each-record-sentence-within-twenty-five-words` is advisory. A longer sentence in a `statement` or `summary` that keeps a condition is not a defect. A glossary meaning longer than the bound reports `term-meaning-invalid` at the same strength, where it used to report `field-invalid`.
- The rationales of the twenty-five-word and eight-word bounds state them as house limits, not as facts about readers or screens. The eight-word title bound stays required, and so does the one-sentence rule for a `statement`.
- `use-literal-words` states that a word is loose when it leaves the act unclear, so a domain term used in its defined sense is not loose. Decision 0057, Narrow the Writing Rules to Meaning, records why.

### Part V: Schema and Model Check Parity

- Shape rules live in `schemas/record.schema.yaml`: required properties, minimum counts, conditional requirements and allowed values. Rules about the record graph stay in the model check. New provision `litespec/core/model#keep-shape-rules-in-the-schema` states the split.
- Five local requirements now appear in the schema as well as the check: role `allows`, decision `choice`, research `sources`, module `dependsOn` and policy `values`. An editor that validates against the schema refuses the same records the CLI does.
- `uniqueItems` is removed from every array of objects, because it compares whole objects. The model check reports `record-key-duplicate` for a repeated source key, journey step id, external operation name or field name in one record.
- A parity test runs both validators over invalid records and fails where they disagree on a rule both carry. Decision 0058, Keep Shape Rules in the Schema, records why.

### Part W: Release Candidates from Release Branches

- A tag `v<version>-rc.<n>` on `release/<version>` publishes a release candidate: `LiteSpec.Cli` and `LiteSpec.Analyzers` at `<version>-rc.<n>` as prerelease packages on nuget.org, the same standalone archives, checksums and attestations, and a GitHub release marked prerelease, with the changelog section for the version as its notes.
- A plain tag `v<version>` still publishes only from `main`. The workflow refuses a candidate whose base version differs from the manifest, whose number is not digits, or whose commit is not on the matching release branch.
- The committed files keep the plain version. The workflow stamps the suffix at build time, and the tool reads the full version from its assembly, so `init` and `model render` write `6.0.0-rc.1` into a consumer's `reviewedLiteSpecVersion`, tool manifest and analyzer pin. Links the tool writes keep the base version, because a candidate publishes no site. The reviewed-release comparison uses the base version.
- `reviewedLiteSpecVersion` accepts a `-rc.<n>` suffix. `docs/guide/install-the-cli.md` shows how to install a candidate, and `docs/reference/published-urls.md` says a candidate publishes no site.
- New provision `litespec/core/maintaining#publish-each-release-candidate-from-its-release-branch`, and `litespec/core/maintaining#publish-a-litespec-release-from-a-tag` states both tag forms. Decision 0059, Publish Release Candidates from Release Branches, records why.

### Part X: Child Records in Typed Folders

- A child record stored as its own file sits in its own folder, inside a folder named for its type in the plural, inside its parent's folder. A child stored as an entry stays in its parent's file. `litespec/core/model#place-child-records-in-typed-folders` states the rule.
- The homes are `intent/problems/<problem>/features/<feature>/requirements/<requirement>/`, `behavior/modules/<module>/aggregates/<aggregate>/` with `use-cases/<use-case>/`, `events/` and `rules/` folders inside it, and `intent/requirements/<requirement>/` for a product-level quality requirement.
- A use case that belongs to no aggregate keeps the module-level home `behavior/modules/<module>/use-cases/<use-case>/`.
- The flat module homes are removed, so every module has one layout. An unknown subfolder of a module is a `record-placement-invalid` finding, not an aggregate. Identifiers do not change, because none names a type folder.
- `new`, `rename`, the folder and locality checks, the book outline and the folder reference page follow the table, and the reference page is generated from the same table the checks read. Decision 0060, Put Child Records in Typed Folders, records why.

### Part Y: Project Verbs in the Glossary

- A project declares each use-case verb beyond the baseline vocabulary as a verb term in its glossary: the term names the verb, the new `verbKind` field names the kind it opens (`command`, `query`, or `either`), and `meaning` states the act in one sentence. The glossary schema carries the field, and the consumer glossary template shows it.
- `model check` reports `use-case-verb-unknown` for a use-case name opening with a verb neither the table nor the glossary declares, `use-case-verb-invalid` for a use case opening with a verb of the other kind, and `term-verb-duplicate` for a verb term restating a baseline verb. `permission-verb-unknown` now also accepts a glossary verb of kind `command` or `either`.
- Reworded: `litespec/core/use-cases#start-every-use-case-with-a-declared-verb` and `litespec/core/use-cases#keep-one-kind-per-verb` are checked instead of reviewed, and `litespec/core/access#name-each-permission-by-a-use-case-verb` accepts glossary verbs. A convention states a judgment rule, never a list of words.
- Claimwick declares `submit`, `attach`, `reimburse` and `erase` as glossary verb terms. Decision 0062, Declare Project Verbs in the Glossary, records why.

## v5.2.0

LiteSpec 5.2.0 generates the agent instructions in a consumer's `AGENTS.md` from one template and reports when they drift, narrows `rules` to the provisions that apply to the edited file or the change set, ships a LiteSpec skill for Claude Code and OpenCode, and requires a technical decision only for a choice of one of six named kinds. It is a minor release: no provision ID is removed or renamed. A consumer runs `model render`, moves provision-shaped conventions into the conventions folder, and may give each technical decision its kind, as Part H describes.

### Part A: Generated Agent Protocol in AGENTS.md

- `templates/consumer/project-agents.md` is the only source of the consumer protocol. Its LiteSpec part sits inside a generated region, `<!-- generated:agent-protocol -->` ... `<!-- /generated:agent-protocol -->`, opening with the precedence sentence: when the file disagrees with tool output or a record, the tool output and the record win. The `## Project Commands` part stays outside the region, written by the project.
- `init` writes the region into a fresh `AGENTS.md`, and inserts it into an existing one that holds none, after the first heading or at the end. It never touches text outside the region, and it never rewrites a region that is already there.
- `model render` rewrites the region from the carried release and the project file: release, size, shape, stacks, layers, extensions, selection key, and command form.
- `model check` reports `agent-protocol-drift` when the region differs from what `render` would write, and `agent-protocol-missing` when `AGENTS.md` holds no region. Both block.
- The region holds 40 lines or fewer: commands and the rules an agent cannot guess, and no description of LiteSpec. It states one order: query the model for the records, run `rules <path>` for each file to edit, read the pages `rules` cites, then read neighboring code. The phrase "generated agent context" is gone; the query output is the task context. The `rules` lines cite `load-applicable-provisions-before-editing`.
- The site selection page renders the same template, filled for the chosen selection, through one shared generated module. `/llms.txt` embeds the filled template under "Agent Protocol" instead of this repository's `AGENTS.md`.

The rule is `litespec/core/agent#keep-the-agent-protocol-current`. Decision 0037, Generate the Agent Protocol, records why.

### Part B: Narrower Rules Output, Change Sets and Lookup by ID

- When the target is code, provisions matched only through a marked record print as one line per record: `use-case/spending.approve-expense: <n> provisions about the record; model check checks <m>; run rules use-case/spending.approve-expense for the rest`. A record path or record ID still prints them in full.
- `rules --since <ref>` takes every file changed since `<ref>`, returns the union with duplicates removed, leaves out the tool-checked provisions, and prints a report skeleton: one line per judgment provision with its Reviewed for question and an empty `How judged:` line.
- `rules <provision-id>` prints that provision in full: strength, statement, rationale, example, Reviewed for or Checked by, and page. `rules <page-id>` prints the page's provision list. Both read the carried `provisions.json`, so they work offline.
- The duty is reworded: `litespec/core/agent#load-applicable-provisions-before-editing` runs `rules <path>` before editing a file, and answers each judgment provision `rules --since <base>` lists before reporting done. The stale provision count is gone from its rationale.
- A provision that holds only in some situations states a `when` key on its `**Applies to:**` line: `query`, `command`, `upload`, `document-number`, `erasure`, `project`, or `audit-adoption`. `rules` returns it on a code path only when the condition holds for the record the file marks; a record path prints it with its condition shown.
- When the target is code, a `Decided:` group prints first: each decision the file follows with its `choice` and `reopenedWhen`, then each departure covering a provision in the result with its `reason` and, for a gap, its `expiresOn`.
- The stage provision carries `when: project`, so it applies once to the project and never per record. The audit adoption provision carries `when: audit-adoption`, so it applies only to the decision the `audit` extension selection names in its new `decision` field in `.litespec/project.yaml`.
- The site serves `/5.2.0/provisions.json`, one object per provision, outside search results. The [published URLs](docs/reference/published-urls.md) page states the ID-to-URL rule: `litespec/<page>#<slug>` is `https://litespec.dev/<release>/<page>/#<slug>` as HTML and `<page>.md` as Markdown.

Decision 0038, Narrow Rules Output per File and Change Set, records why.

### Part C: The LiteSpec Skill

- `templates/skill/litespec/` holds `SKILL.md` with `references/commands.md` and `references/report.md`. The frontmatter names `litespec`, with a description that triggers on work in a repository holding `.litespec/project.yaml`. The body stays under 200 lines and carries the procedure, not the provisions: read the protocol region and the project file, query the model, run `rules` per file, stop on unresolved conflict, check the change set, and write the completion report.
- `litespec skill add [--agent claude|opencode|agents]... [--global]` installs one generated copy per target; `litespec skill remove` deletes them. Without `--agent`, `add` installs for `agents` and `claude`, which together reach Claude Code and OpenCode. Each target gets its own copy, never a link, so it works in git and on Windows without Developer Mode.
- Every copy carries `metadata.generated-by: litespec`. A project-scope copy is also stamped with the release; a global copy names no release, so one global skill serves projects on different releases. `add` replaces a copy it wrote and refuses a folder without its marker, with exit 1; `remove` deletes only a folder carrying the marker. Global scope writes under the user's home and needs no project file.
- `init --skill <agent>[,<agent>]` installs the skill at project scope during setup, and `--skill-global` installs it at global scope instead. Without `--skill`, `init` prints the `skill add` command as its last line.
- `model render` rewrites project-scope copies that carry the marker. `model check` reports `skill-drift` when a project copy differs from what the carried release generates. Global copies live outside the project and stay outside the check.

The rule is `litespec/core/agent#keep-the-agent-protocol-current`, which also names the skill. The [skill page](docs/tools/skill.md) states the full syntax. Decision 0039, Ship a LiteSpec Skill, records why.

### Part D: Decision Kinds, Convention Marks and Decision Indexes

- A technical decision carries an optional `kind`: `provider`, `package`, `format`, `boundary`, `topology`, or `replacement`. A product decision takes none. `litespec/core/model#record-each-technical-choice-as-a-decision` requires a decision only for a choice of one of the six kinds; otherwise the class documentation states the reason, and the mark names the record the class serves. Claimwick's `0005-render-receipts-as-pdf` reads `package`.
- `model check` reports `decision-kind-missing` for a technical decision with no `kind`, and `decision-scope-invalid` for a technical decision whose followers all sit in one module and each already carry an `implements` mark. Both are advisory.
- The default `litespec/core/agent#search-the-provisions-before-recording-a-decision` queries `rules` for the decision's subject first: where a provision makes the choice, the code follows that provision instead. `litespec new decision <name>` prints the provisions whose statement matches the name's words before it writes the files.
- A `follows` or `proves` mark may name a project convention as `<project>/conventions#<slug>`. An unknown convention ID reports `mark-unknown`, as an unknown decision does. Claimwick gains one convention that a class follows.
- A decision chooses between options at one moment, and its `reopenedWhen` names what would reopen it. A convention is a standing rule every change follows, and its `Reviewed for` question names how a reviewer finds a breach. `model check` reports `convention-folder-invalid` for a convention-shaped file under `.litespec/` outside `governance/conventions/`, where `rules` never reads it. A convention's `Applies to` takes a path glob or the scope keys provisions use, such as `stack: dotnet; artifact: command-handler`, and anything neither reports `convention-scope-invalid`. Both are advisory. `model check` accepts `.litespec/governance/conventions/` and the convention files in it, which the folder and page checks refused although `rules` reads them there.
- Folders carry ownership and layer only. A decision's concern is computed from its followers' paths, so it cannot drift. `model render` writes a `decision-index` region into the README of each decisions folder that carries the fence, grouped by `kind`, then by concern, with the choice, follower count and modules, `reopenedWhen`, and markers for `decision-unimplemented`, `decision-basis-missing` and `decision-scope-invalid`. It writes a `departure-index` region grouped by gap or permanent, ordered by `expiresOn`, with the provisions each departure covers.
- The book's decision chapter groups by kind, then by concern; each decision page lists its followers by module; and an external-system page shows the decision that chooses its provider. `model check` reports `decision-home-drift` for a module-level decision followed from more than one module, or a product-level decision followed only from one module. It is advisory.

Decision 0040, Require Decisions Only for Six Kinds of Choice, records why.

### Part E: Strength and Modal Word Agree

- The rule is `litespec/core/authoring#match-strength-to-the-modal-word`: a `required` statement holds MUST or MUST NOT, an `advisory` statement holds neither, and a permission on its own joins the provision it relaxes or becomes an obligation. `litespec-maintain validate` enforces it as `STANDARD_STRENGTH_MODAL`, and reports `TEMPLATE_CITATION_UNKNOWN` for a `litespec/...` citation in `templates/` that resolves to no provision.
- Nine provisions are reworded, each keeping its ID:
  - Required, now obliging: `core/agent#follow-the-local-pattern-before-the-model-query`, `core/agent#replace-only-declared-conventions`, `core/authoring#keep-a-pinned-release-for-as-long-as-it-serves`, `core/use-cases#exercise-rules-from-scenarios`, `core/use-cases#share-scenario-setup-in-background`, `frontend/components#place-a-drawing-no-composite-makes-in-an-exception-file`.
  - Advisory, now suggesting with a Deviation: `frontend/testing#test-observable-states`, `workspace/naming#use-current-language-features`, `workspace/naming#use-intent-revealing-boolean-names`.
- For `follow-the-local-pattern-before-the-model-query`, the statement reads "An agent MUST run the model query before a change that touches a permission carrying `risk`, and MAY rely on neighboring code otherwise." Its heading, and its rename, wait for 6.0.

### Part F: The Documentation and litespec.dev

- Selection indexes describe each page with its Intent's first sentence, with links rewritten the way page bodies are. They leave out the maintainer-only `core/maintaining` page and list the `rules`, `model`, `new` and `skill` tool pages with the Published URLs page. A "By Artifact" section names, for each artifact kind, the pages of the selection that carry provisions for it. Each index stays under 25 KB.
- `/<version>/llms-full.txt` is removed: Cloudflare analytics recorded no request for it in the 29 days before this release, while the indexes kept their readers. The layer bundles stay, and each section heading of a selection index links the bundle that carries the same text.
- The "Use with Agents" page shows the full generated block and a third step, "Install the Skill", with the `skill add` command.
- The guide [Run Rules After Every Edit](docs/guide/run-rules-after-every-edit.md) runs `rules` after every file edit: a Claude Code `PostToolUse` hook on `Edit|Write` that returns the short `rules` output as added context, a `Stop` hook running `model check --since <base>`, and an OpenCode plugin doing the same after each edit. `skill add` writes Markdown files only and installs no hooks.
- [Work with agents](docs/learn/working-with-agents.md) names the entry file each agent reads: Claude Code reads `CLAUDE.md` else the nearest `AGENTS.md`, and the rest read the nearest `AGENTS.md` except Gemini CLI with `GEMINI.md`. `init` prints a line when a `CLAUDE.md` does not import `AGENTS.md`, and `model check` reports `agent-protocol-unreached` as advisory for the same gap.

### Part G: Provision Changes Since v5.1.0

<!-- generated:provision-diff -->
### Provision Changes Since v5.1.0

Added (3):

- `litespec/core/agent#keep-the-agent-protocol-current`
- `litespec/core/agent#search-the-provisions-before-recording-a-decision`
- `litespec/core/authoring#match-strength-to-the-modal-word`

Removed (0):

None.

Renamed (0):

None.

Extensions added (0):

None.

Extensions removed (0):

None.

<!-- /generated:provision-diff -->

### Part H: Upgrading a Consumer

1. Install `LiteSpec.Cli` 5.2.0 (`dotnet tool update LiteSpec.Cli --version 5.2.0`) or rerun the install script, and set `reviewedLiteSpecVersion: 5.2.0` in `.litespec/project.yaml`.
2. Run `litespec model render`. It inserts or rewrites the `agent-protocol` region in `AGENTS.md` and rewrites each project-scope skill copy. Without it, `model check` reports `agent-protocol-missing`, `agent-protocol-drift`, or `skill-drift`. `render` leaves text outside the region alone, so delete the `## LiteSpec` section an earlier `init` wrote into `AGENTS.md`: the region replaces it.
3. Move each provision-shaped convention to `.litespec/governance/conventions/`, where `rules` reads it. A convention-shaped file anywhere else under `.litespec/` reports `convention-folder-invalid`.
4. Add the `decision-index` fence to the README of each decisions folder to index, and the `departure-index` fence to the README of each departures folder, then run `litespec model render` to fill them.
5. A project that selects the `audit` extension names its audit adoption decision in the selection's `decision` field. A project holding a `CLAUDE.md` adds the line `@AGENTS.md`, so Claude Code reads the protocol.
6. A technical decision may take its `kind`: `provider`, `package`, `format`, `boundary`, `topology`, or `replacement`. A decision with none reports `decision-kind-missing` as advisory.
7. A project that references `LiteSpec.Analyzers` updates it to 5.2.0.
8. Run `litespec model check` and fix each finding.

## v5.1.0

LiteSpec 5.1.0 lets a project require an explanation file for every record on the layers it names, and adds behavior Standards for keyboards, screen readers, reduced motion, color schemes and older browsers. It pins third-party packages to their major version, so a consumer takes patch and minor releases without waiting for LiteSpec. `model check` is several times faster on large projects. It is a minor release: no provision ID is removed or renamed. A consumer with a shared UI package adds a Browser Support section to its design contract, as Part H describes.

### Part A: Explanation Files a Project Requires

- `explanations.layers` in `.litespec/project.yaml` names the layers whose records must each carry an explanation file. It takes `intent`, `behavior` or both. Every page record on a named layer then needs its `<name>.md` beside its record file, with the sections its record type defines. Projects that leave the key out are unaffected.
- `model check` reports `explanation-missing` for a missing file or a missing required section, and `project-invalid` when `explanations.layers` names a layer the project does not claim in `layers`.
- `litespec new` writes the explanation file from `templates/consumer/<type>.md` when the new record's layer is listed, and from `templates/consumer/explanation.md` for a type with no template of its own.
- `litespec new feature <module>.<name>` writes the record `feature/<name>` in the module's folder. In 5.0.0 it refused every name: without a module it asked for one, and with one it rejected the identifier.

The rule is `litespec/core/model#require-explanation-files-on-the-layers-a-project-names`, and the [project file reference](docs/reference/project-file.md) lists the field.

### Part B: How a Screen Behaves

- Controlled UI Governance names three layers of a screen: product, behavior and parts. The parts never lower the behavior: a baseline component that closes a modal on an outside press, or traps Tab in a menu, is overridden.
- The pattern and foundation pages gain behavior Standards drawn from WCAG 2.2. They cover overlay dismissal and initial focus, live regions by urgency, when a field error appears and how assistive technology reads it, one send in flight, sortable and scrolling tables, aligned figures, calendar dates without a time zone, sibling pages as links, 3:1 focus indicators and input boundaries, color schemes, reduced motion, pause controls, text sizes in `rem`, and focus on a route change.
- The route suite checks focus after a route change, every declared color scheme and reduced motion.
- The pattern pages state what they contradicted before:
  - Amounts show the decimals each currency defines, and money fields are text inputs.
  - The session warning no longer expires.
  - The theme follows the device by default.
  - Every Compact line states its pattern's narrow behavior.

### Part C: Packages Pinned to Their Major

- `litespec.manifest.yaml` pins each third-party package to its major, written `N.x`, such as `Marten: 9.x`. The consumer's central version file or lockfile holds the exact version, so a consumer takes a patch or minor release of a baseline package without waiting for a LiteSpec release.
- Three packages stay exact: `LiteSpec.Analyzers`, which matches the CLI release, and the `shadcn` and `shadcn-svelte` CLIs, whose exact version the UI source lock records. The `node` and `pnpm` entries under `stack` stay exact too.
- `dependency-version-drift` reports a direct package outside its pin: another major, or a prerelease inside the pinned major. Its message reads `... is outside its manifest pin '9.x'.` for a major pin and keeps `... differs from its manifest pin ...` for an exact pin.

Decision 0036, Pin Third-Party Packages to Their Major, records why.

### Part D: The Book

- A use case shows its Kind once, and a scenario shows Given, When, Then and Result in their own sections only.
- The `norun` proof state reads "Not run".
- A page record no longer lists its own entries as links. In 5.0.0 they appeared under raw keys such as `use-case.records.id`.
- The top bar carries the Litenova family mark.

### Part E: Faster Model Check

On a project of 3,142 records and 19,334 files, `litespec model check` took 195 s in 5.0.0 and takes about 18 s in 5.1.0, with the same output. The checks run in parallel, the schema evaluator stops at the first failing shape, and record lookups use indexes built once per run. A run also reads the release's provision pages and schemas once, where 5.0.0 read the provision pages three times and could parse a schema once per parallel worker.

### Part F: The Documentation and litespec.dev

- `learn/README.md` is the starting page: what LiteSpec is, a five-minute picture, then five pages in reading order. Each concept is explained on one page, and the others link to it.
- `learn/reading-tracks.md` and `guide/folder-reference.md` are removed. Their content is in `learn/how-litespec-works.md` and the [folder reference](docs/reference/folders.md).
- On litespec.dev:
  - The sidebar is one tree in five groups, in reading order: Learn, Tutorial, Guides, Standard and Reference. Every area and folder opens and closes, starts open when it holds the current page, and lists its overview first.
  - Breadcrumbs show every level, and every breadcrumb entry in the page's structured data links its page.
  - A folder's overview page is titled by its folder, such as "CLI Commands Overview".
  - The Standard has an index page of linked cards, and "On this page" opens the section in view.
  - A pattern lists its facts with each label beside its value, and its Default links to the option it names.
  - The landing page is the opening title and the release badge, which links to this changelog.
  - Screens 1600px and wider get a wider column and larger type, and muted text reads at 6:1 or more in light mode.
  - Pages animate only when the reader's system allows motion.
  - On a phone, the page browser has a filter.

### Part G: Provision Changes Since v5.0.0

<!-- generated:provision-diff -->
### Provision Changes Since v5.0.0

Added (41):

- `litespec/core/model#require-explanation-files-on-the-layers-a-project-names`
- `litespec/frontend/components#make-touch-targets-fit-a-finger`
- `litespec/frontend/components#show-a-focus-indicator-that-stands-out`
- `litespec/frontend/data#keep-a-calendar-date-free-of-a-time-zone`
- `litespec/frontend/patterns/create-edit-remove#refuse-a-second-send-while-one-is-in-flight`
- `litespec/frontend/patterns/dialogs-and-overlays#hold-a-toast-while-a-modal-is-open`
- `litespec/frontend/patterns/dialogs-and-overlays#keep-tab-free-to-leave-a-menu-or-popover`
- `litespec/frontend/patterns/dialogs-and-overlays#keep-the-page-behind-a-modal-still`
- `litespec/frontend/patterns/dialogs-and-overlays#keep-unsaved-input-through-a-dismissal`
- `litespec/frontend/patterns/dialogs-and-overlays#pause-a-timed-toast-while-it-is-in-use`
- `litespec/frontend/patterns/dialogs-and-overlays#place-initial-focus-on-the-safe-action`
- `litespec/frontend/patterns/feedback-and-states#announce-each-status-by-its-urgency`
- `litespec/frontend/patterns/feedback-and-states#keep-unsent-input-when-the-connection-drops`
- `litespec/frontend/patterns/feedback-and-states#mark-a-loading-region-busy`
- `litespec/frontend/patterns/feedback-and-states#move-focus-to-a-refusal`
- `litespec/frontend/patterns/forms-and-fields#expose-a-field-error-to-assistive-technology`
- `litespec/frontend/patterns/forms-and-fields#keep-submit-enabled-to-reveal-errors`
- `litespec/frontend/patterns/forms-and-fields#mark-every-required-field`
- `litespec/frontend/patterns/forms-and-fields#match-the-keyboard-to-the-value`
- `litespec/frontend/patterns/forms-and-fields#refuse-enter-while-text-is-being-composed`
- `litespec/frontend/patterns/forms-and-fields#show-a-field-error-after-the-field-is-left`
- `litespec/frontend/patterns/forms-and-fields#state-each-fields-purpose`
- `litespec/frontend/patterns/forms-and-fields#take-a-name-in-one-field-of-any-script`
- `litespec/frontend/patterns/lists-and-tables#announce-a-lists-result-count`
- `litespec/frontend/patterns/lists-and-tables#keep-the-header-row-in-view`
- `litespec/frontend/patterns/lists-and-tables#make-a-scrolling-table-a-named-region`
- `litespec/frontend/patterns/lists-and-tables#mark-the-sorted-column`
- `litespec/frontend/patterns/lists-and-tables#shape-a-list-by-its-container`
- `litespec/frontend/patterns/money-time-density#align-figures-in-a-column`
- `litespec/frontend/patterns/money-time-density#mark-a-moment-with-its-machine-value`
- `litespec/frontend/patterns/navigation#draw-sibling-pages-as-links-not-tabs`
- `litespec/frontend/rendering#move-focus-on-a-route-change`
- `litespec/frontend/rendering#split-long-client-work`
- `litespec/frontend/ui#give-every-input-a-visible-boundary`
- `litespec/frontend/web/components#enforce-trusted-types-on-raw-markup`
- `litespec/frontend/web/ui#build-within-the-browser-support-target`
- `litespec/frontend/web/ui#give-moving-content-a-pause-control`
- `litespec/frontend/web/ui#honor-reduced-motion`
- `litespec/frontend/web/ui#render-each-declared-color-scheme`
- `litespec/frontend/web/ui#set-text-sizes-in-relative-units`
- `litespec/quality/ci#assert-loading-stability-and-blocking-time-per-route`

Removed (0):

None.

Renamed (0):

None.

Extensions added (0):

None.

Extensions removed (0):

None.

<!-- /generated:provision-diff -->

### Part H: Upgrading a Consumer

1. Install `LiteSpec.Cli` 5.1.0 (`dotnet tool update LiteSpec.Cli --version 5.1.0`) or rerun the install script, and set `reviewedLiteSpecVersion: 5.1.0` in `.litespec/project.yaml`.
2. A project with a shared UI package adds a `## Browser Support` section to its design contract, naming the browser target, as `templates/consumer/design-contract.md` shows. Without it, `model check` reports `ui-design-contract-invalid` with `missing required section 'Browser Support'`.
3. A project that references `LiteSpec.Analyzers` updates it to 5.1.0.
4. Run `litespec model check` and fix each finding.


## v5.0.0

LiteSpec 5.0.0 lets a project claim only the record layers it uses, writes every file a person edits in YAML, replaces exceptions with departures, and ships the `litespec` command line as a native executable for Windows, Linux and macOS. It is a major release: a consumer upgrades by following the steps in Part H, then running `litespec model check` until it is clean.

### Part A: Claim the Record Layers the Project Uses

- `layers` in the project file replaces the conformance level. A project names each record layer it claims, `intent`, `behavior`, `implementation` and `proof`, leaving layers out only from the bottom; `implementation` and `proof` each take `litespec`, LiteSpec's practice for the layer, or `own`, which keeps the team's practice and checks only that code and tests mark their records.
- Every provision and every finding code carries the record layer it governs and whether it ties that layer to the records (`trace`). `model check`, `rules`, the book and the analyzers report only what the claim covers, and the book shows an unclaimed layer as not claimed.
- A page's Scope block names its `Concern`; a provision's Applies to line may name its `layer` and `trace`.
- The operations alert and runbook checks belong to the `delivery` extension.
- The analyzers read the claim: the traceability rules run when Implementation is claimed, the architecture rules when it follows `litespec`, and LS4001 when Proof is claimed.

### Part B: People Write YAML, Programs Exchange JSON

- `.litespec/project.yaml`, `litespec.manifest.yaml`, the schemas (`schemas/<name>.schema.yaml`), the pattern catalog (`.litespec/implementation/patterns/catalog.yaml`), the UI source lock (`ui-source-lock.yaml`) and the presets are YAML that states its `schemaVersion` and names its schema in a first-line `# yaml-language-server: $schema=<url>` comment. Check results, `--format json` output, the book data and the provision index stay JSON.
- The project file holds `frontends` and `ui.package` at the top level, `stackConfig.dotnet.webapi` and `worker` as lists, and no `conformance`, `$schema`, `unstructuredDocs`, `paths.records`, `surfaces`, `sites`, `apiSolution` or `refusals.prefixes`.
- The schemas publish at `https://litespec.dev/5.0.0/schemas/<name>.schema.yaml`, including the new `check-result.schema.yaml`; a harness result that fails it reports `proof-invalid`.
- `litespec model render` writes the UI source lock: each component's digest, the preset and the CLI pin.

### Part C: One Departure Record

- The departure record replaces the exception record. It covers provisions, pages or finding codes on its `subject` or `paths`, with a reason; a gap the team means to close carries `expiresOn` and `exit`, and a lasting choice carries neither. It lives in `.litespec/governance/departures/`.
- `departure-expired` and `departure-invalid` replace the exception codes. No departure covers the findings the model needs to load.

### Part D: One Name per Concept

- A finding's `outcome` is `blocking`, `reported` or `covered`; `severity` leaves the JSON, and `model check` ends with `N findings, M blocking, K covered.`
- Completion stages read `unfulfilled`, `specified`, `coded` and `verified`.
- The qualified identifier is gone; a record shows its folder path and its module.
- Provision strengths read `required`, `default` and `advisory`.

### Part E: A Smaller, Bound Standard

- Near-duplicate provisions keep one home, and every citation follows it. Departures, testing and formats each have one page; how-to guides live under `docs/guide/`.
- Each UI pattern offers its Default and at most two other options.
- The `jobs` extension is cut to what a small team follows; `realtime` and `customer-rules` are extensions; tenant delegation is part of `tenancy`; sagas and workflow engines leave the standard.
- `litespec-maintain validate` reports a cited finding code, template or example record that does not exist, a `Checked by` line for the model check that names no code, a page the index does not reach, and a pattern with more than three options. The finding-code, folder and pin tables are generated.
- `quality/testing.md` holds one mock rule for every stack: a test substitutes only what the code under test does not own and what is not deterministic.
- An agent never weakens a test that proves a scenario, and `model check --since` reports `proof-hash-drift` for a proving test that changed while its scenario did not.

### Part F: A Native Command Line on Every Platform

- The CLI compiles ahead of time with .NET Native AOT for `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `linux-musl-x64`, `linux-musl-arm64`, `osx-x64` and `osx-arm64`, and needs no .NET runtime.
- `LiteSpec.Cli` on NuGet is a per-platform tool package, so `dotnet tool install`, `dotnet tool restore` and `dotnet litespec` work unchanged; every GitHub release carries a standalone archive per platform, `SHA256SUMS`, `install.sh` and `install.ps1`, each with a build provenance attestation.
- `litespec init --install standalone` writes a project that runs the CLI as `litespec`; in a consumer root, the install scripts install the release that `.litespec/project.yaml` reviews.
- The Linux glibc binaries run on glibc 2.28 and later and the musl binaries on Alpine 3.21 and later; the CLI computes SHA-256 itself.
- The CLI and the maintainer tool check every schema with LiteSpec's own JSON Schema evaluator, draft 2020-12 without dynamic references, anchors, `unevaluatedProperties`, `unevaluatedItems` and `contentSchema`.
- Every archive and package ships `THIRD-PARTY-NOTICES.md`.

### Part G: Provision Changes Since v4.0.0

<!-- generated:provision-diff -->
### Provision Changes Since v4.0.0

Added (162):

- `litespec/backend/domain#raise-the-past-tense-of-a-methods-own-verb`
- `litespec/backend/dotnet/domain#keep-clock-reads-and-random-values-out-of-domain`
- `litespec/backend/dotnet/domain#keep-double-and-float-out-of-exact-quantities`
- `litespec/backend/dotnet/domain#mark-each-changing-root-method-with-one-use-case`
- `litespec/backend/dotnet/naming#check-architectural-role-suffixes`
- `litespec/backend/dotnet/naming#check-repository-variable-names`
- `litespec/backend/dotnet/naming#check-result-part-names`
- `litespec/backend/dotnet/persistence#keep-pipeline-owned-collaborators-out-of-handlers`
- `litespec/backend/dotnet/stack#identify-an-aggregate-root-by-its-mark-and-state-record`
- `litespec/backend/dotnet/stack#pin-the-test-packages-in-the-manifest`
- `litespec/backend/dotnet/stack#run-mutation-testing-with-stryker-net`
- `litespec/backend/dotnet/stack#start-the-integration-database-in-a-container`
- `litespec/backend/dotnet/stack#test-with-xunit-v3-on-microsoft-testing-platform`
- `litespec/backend/persistence#test-persistence-against-the-pinned-database`
- `litespec/core/agent#protect-proofs-from-implementation-edits`
- `litespec/core/departures#depart-from-advice-without-a-record`
- `litespec/core/departures#record-a-project-wide-lowering-as-a-departure`
- `litespec/core/departures#record-accepted-gaps-as-departures`
- `litespec/core/language#cite-shared-provisions-in-their-published-form`
- `litespec/core/language#exclude-declared-content-paths-from-the-ascii-check`
- `litespec/core/language#exclude-the-compound-that-names-another-concept`
- `litespec/core/language#follow-the-identifier-form-of-each-record-type`
- `litespec/core/language#keep-wire-codes-stable`
- `litespec/core/language#name-records-with-one-to-six-words`
- `litespec/core/language#record-the-project-vocabulary-as-data`
- `litespec/core/language#reject-a-recorded-synonym-inside-its-scope`
- `litespec/core/language#resolve-every-identifier-citation`
- `litespec/core/language#review-each-term-for-its-lookalikes`
- `litespec/core/language#state-mannered-terms-literally`
- `litespec/core/language#use-every-identifier-once`
- `litespec/core/language#use-the-declared-record-identifier-grammar`
- `litespec/core/model#classify-every-specification-file`
- `litespec/core/model#declare-structured-specification-metadata`
- `litespec/core/model#derive-a-computed-fact-instead-of-restating-it`
- `litespec/core/model#keep-code-out-of-intent-and-behavior-fields`
- `litespec/core/model#rewrite-every-link-on-a-rename`
- `litespec/core/model#scaffold-pages-and-reserve-identifiers`
- `litespec/core/model#start-each-explanation-file-with-its-title`
- `litespec/core/model#state-what-happens-in-every-behavior-statement`
- `litespec/core/model#use-one-metadata-carrier`
- `litespec/core/model#validate-a-single-page-before-committing`
- `litespec/core/release#record-accepted-debt-as-a-departure`
- `litespec/core/scope#claim-the-record-layers-the-project-uses`
- `litespec/core/scope#write-every-hand-written-litespec-file-in-yaml`
- `litespec/core/scopes#depart-from-a-rule-only-through-a-recorded-departure`
- `litespec/core/scopes#give-every-provision-its-layer-and-trace`
- `litespec/ext/customer-rules#bound-tenant-rule-runs-per-period`
- `litespec/ext/customer-rules#isolate-hook-failures-from-the-trigger`
- `litespec/ext/customer-rules#record-the-customer-rules-catalog`
- `litespec/ext/customer-rules#refuse-unknown-catalog-entries`
- `litespec/ext/customer-rules#state-what-a-hook-may-not-assume`
- `litespec/ext/customer-rules#state-what-a-hook-may-rely-on`
- `litespec/ext/customer-rules#treat-tenant-rules-as-data`
- `litespec/ext/customer-rules#version-catalog-entries`
- `litespec/ext/delivery#ask-only-unanswered-questions`
- `litespec/ext/delivery#close-incident-departures-within-seven-days`
- `litespec/ext/delivery#cover-every-quality-requirement-with-an-alert-or-a-check`
- `litespec/ext/delivery#hold-back-example-rows-from-the-implementing-worker`
- `litespec/ext/delivery#name-every-runbook-from-an-alert-job-reaction-or-failure-policy`
- `litespec/ext/delivery#name-the-requirement-and-runbook-on-every-alert`
- `litespec/ext/delivery#point-each-operations-file-at-its-decision`
- `litespec/ext/delivery#restrict-a-worker-to-its-paths`
- `litespec/ext/delivery#verify-before-accepting-worker-output`
- `litespec/ext/delivery#write-a-brief-before-handing-off`
- `litespec/ext/realtime#authenticate-and-authorize-subscriptions`
- `litespec/ext/realtime#bound-realtime-resources`
- `litespec/ext/realtime#declare-realtime-freshness`
- `litespec/ext/realtime#place-realtime-code-at-outer-boundaries`
- `litespec/ext/realtime#reconnect-and-refresh-authoritative-state`
- `litespec/ext/realtime#record-realtime-need`
- `litespec/ext/realtime#record-transport-support`
- `litespec/ext/realtime#reject-cosmetic-realtime-adoption`
- `litespec/ext/realtime#reject-unbounded-slow-client-buffering`
- `litespec/ext/realtime#revalidate-long-lived-access`
- `litespec/ext/realtime#subscribe-through-narrow-functions`
- `litespec/ext/realtime#tolerate-notification-ordering-differences`
- `litespec/ext/realtime#use-server-sent-events-for-notifications`
- `litespec/ext/realtime#use-signalr-for-interactive-messaging`
- `litespec/frontend/blazor/stack#bind-blazor-routes-to-their-address`
- `litespec/frontend/blazor/stack#build-blazor-forms-on-the-shared-form-entry-point`
- `litespec/frontend/blazor/stack#declare-blazor-outside-the-controlled-ui-contract`
- `litespec/frontend/blazor/stack#keep-blazor-test-mocks-to-the-owned-boundaries`
- `litespec/frontend/blazor/stack#keep-class-and-style-bindings-out-of-blazor-page-code`
- `litespec/frontend/blazor/stack#keep-layouts-and-error-components-unmarked`
- `litespec/frontend/blazor/stack#keep-the-blazor-frame-routes-and-modules`
- `litespec/frontend/blazor/stack#load-blazor-route-data-before-its-view-renders`
- `litespec/frontend/blazor/stack#localize-blazor-copy-through-a-pinned-dictionary-library`
- `litespec/frontend/blazor/stack#mark-each-blazor-route-entry-with-its-screen`
- `litespec/frontend/blazor/stack#name-blazor-drawings-with-the-visual-suffix`
- `litespec/frontend/blazor/stack#render-raw-html-only-through-markupstring`
- `litespec/frontend/blazor/stack#run-blazor-logic-tests-on-xunitv3`
- `litespec/frontend/blazor/stack#show-route-failure-in-the-frames-route-error-component`
- `litespec/frontend/blazor/stack#use-a-layout-for-the-blazor-shell`
- `litespec/frontend/blazor/stack#validate-blazor-route-parameters-before-loading`
- `litespec/frontend/mobile/android/stack#accept-app-links-in-the-host-activity`
- `litespec/frontend/mobile/android/stack#apply-system-insets-to-each-screen`
- `litespec/frontend/mobile/android/stack#declare-android-outside-the-controlled-ui-contract`
- `litespec/frontend/mobile/android/stack#hold-one-navigation-controller-in-the-platform-project`
- `litespec/frontend/mobile/android/stack#keep-android-core-test-mocks-to-the-owned-boundaries`
- `litespec/frontend/mobile/android/stack#keep-hosts-and-failure-views-unmarked`
- `litespec/frontend/mobile/android/stack#keep-screen-styles-in-resources`
- `litespec/frontend/mobile/android/stack#keep-the-android-frame-routes-and-modules`
- `litespec/frontend/mobile/android/stack#mark-each-android-screen-with-its-screen-record`
- `litespec/frontend/mobile/android/stack#read-android-copy-through-the-generated-resource-class`
- `litespec/frontend/mobile/android/stack#request-audio-focus-before-playing-sound`
- `litespec/frontend/mobile/android/stack#request-permissions-at-the-point-of-use`
- `litespec/frontend/mobile/android/stack#route-the-back-gesture-through-the-activity-dispatcher`
- `litespec/frontend/mobile/android/stack#run-android-core-tests-on-xunitv3`
- `litespec/frontend/mobile/android/stack#store-navigation-state-in-the-activity-bundle`
- `litespec/frontend/mobile/android/stack#store-persisted-state-in-app-private-storage`
- `litespec/frontend/mobile/android/stack#use-the-system-pickers-for-media`
- `litespec/frontend/web/react/stack#add-the-tanstack-query-plugin-to-the-client-generator`
- `litespec/frontend/web/react/stack#apply-the-page-code-lint-to-route-and-module-code`
- `litespec/frontend/web/react/stack#declare-route-search-parameters-in-validatesearch`
- `litespec/frontend/web/react/stack#hold-a-routes-parts-in-its-route-file`
- `litespec/frontend/web/react/stack#keep-layouts-error-components-and-route-groups-unmarked`
- `litespec/frontend/web/react/stack#keep-react-test-mocks-to-the-owned-libraries`
- `litespec/frontend/web/react/stack#keep-the-react-frame-routes-and-modules`
- `litespec/frontend/web/react/stack#keep-the-tone-table-in-one-states-file`
- `litespec/frontend/web/react/stack#load-intent-targets-before-navigation`
- `litespec/frontend/web/react/stack#mark-each-react-route-entry-with-its-screen`
- `litespec/frontend/web/react/stack#meet-the-locale-dictionary-contract-with-the-dictionary-library`
- `litespec/frontend/web/react/stack#name-drawings-with-the-visual-suffix`
- `litespec/frontend/web/react/stack#prefetch-route-reads-in-the-loader`
- `litespec/frontend/web/react/stack#read-route-data-with-usesuspensequery`
- `litespec/frontend/web/react/stack#render-raw-html-only-through-dangerouslysetinnerhtml`
- `litespec/frontend/web/react/stack#run-the-registry-build-command`
- `litespec/frontend/web/react/stack#show-route-failure-in-the-route-error-component`
- `litespec/frontend/web/react/stack#use-named-props-for-class-and-style-bindings`
- `litespec/frontend/web/react/stack#use-the-shared-package-form-hook`
- `litespec/frontend/web/react/stack#use-the-tsx-extension-on-shared-files`
- `litespec/frontend/web/svelte/stack#add-the-tanstack-query-plugin-to-the-client-generator`
- `litespec/frontend/web/svelte/stack#apply-the-page-code-lint-to-route-and-module-code`
- `litespec/frontend/web/svelte/stack#declare-route-parameters-in-a-schema`
- `litespec/frontend/web/svelte/stack#hold-a-routes-parts-in-its-folder`
- `litespec/frontend/web/svelte/stack#keep-layout-files-error-pages-and-route-groups-unmarked`
- `litespec/frontend/web/svelte/stack#keep-route-group-names-in-router-syntax`
- `litespec/frontend/web/svelte/stack#keep-svelte-test-mocks-to-the-owned-libraries`
- `litespec/frontend/web/svelte/stack#keep-the-svelte-frame-routes-and-modules`
- `litespec/frontend/web/svelte/stack#keep-the-tone-table-in-one-states-file`
- `litespec/frontend/web/svelte/stack#load-intent-targets-before-navigation`
- `litespec/frontend/web/svelte/stack#mark-svelte-route-entries`
- `litespec/frontend/web/svelte/stack#meet-the-locale-dictionary-contract-with-the-dictionary-library`
- `litespec/frontend/web/svelte/stack#name-drawings-with-the-visual-suffix`
- `litespec/frontend/web/svelte/stack#name-the-module-extension-on-each-import`
- `litespec/frontend/web/svelte/stack#prefetch-route-reads-in-the-load`
- `litespec/frontend/web/svelte/stack#read-route-data-with-a-query-creator`
- `litespec/frontend/web/svelte/stack#render-raw-html-only-through-one-template-tag`
- `litespec/frontend/web/svelte/stack#resolve-imports-through-the-lib-alias`
- `litespec/frontend/web/svelte/stack#run-the-registry-build-command`
- `litespec/frontend/web/svelte/stack#show-route-failure-in-the-route-error-page`
- `litespec/frontend/web/svelte/stack#use-named-attributes-for-class-and-style-bindings`
- `litespec/frontend/web/svelte/stack#use-the-shared-package-form-creator`
- `litespec/frontend/web/svelte/stack#use-the-svelte-extension-on-shared-files`
- `litespec/frontend/web/svelte/stack#write-components-in-runes-mode`
- `litespec/quality/testing#derive-expected-values-from-the-scenario`
- `litespec/quality/testing#keep-the-test-projects-the-size-requires`
- `litespec/quality/testing#prove-each-rule-through-its-scenarios`
- `litespec/quality/testing#prove-each-scenario-through-the-public-boundary`
- `litespec/quality/testing#refuse-surviving-mutants-on-changed-lines`
- `litespec/quality/testing#use-evidence-rather-than-one-coverage-target`
- `litespec/quality/testing#use-one-mock-rule-for-every-stack`

Removed (114):

- `litespec/backend/dotnet/domain#expose-cross-record-keys-from-the-aggregate`
- `litespec/backend/dotnet/domain#raise-the-past-tense-of-a-methods-own-verb`
- `litespec/backend/dotnet/stack#identify-an-aggregate-root-by-its-mark-and-state-record-at-traced`
- `litespec/backend/identifiers#cite-shared-provisions-in-their-published-form`
- `litespec/backend/identifiers#follow-the-identifier-form-of-each-record-type`
- `litespec/backend/identifiers#keep-wire-codes-stable`
- `litespec/backend/identifiers#name-records-with-one-to-six-words`
- `litespec/backend/identifiers#resolve-every-identifier-citation`
- `litespec/backend/identifiers#use-every-identifier-once`
- `litespec/backend/persistence#test-persistence-against-postgresql`
- `litespec/backend/testing#derive-expected-values-from-the-scenario`
- `litespec/backend/testing#mark-the-test-that-proves-each-scenario`
- `litespec/backend/testing#prove-each-rule-through-its-scenarios`
- `litespec/backend/testing#refuse-surviving-mutants-on-changed-lines`
- `litespec/backend/testing#test-application-coordination`
- `litespec/backend/testing#use-evidence-rather-than-one-coverage-target`
- `litespec/core/access#declare-every-operation-on-every-external-system`
- `litespec/core/access#name-every-entry-point-with-its-kind`
- `litespec/core/authoring#classify-every-specification-file`
- `litespec/core/authoring#declare-structured-specification-metadata`
- `litespec/core/authoring#derive-a-computed-fact-instead-of-restating-it`
- `litespec/core/authoring#exclude-declared-content-paths-from-the-ascii-check`
- `litespec/core/authoring#exclude-the-compound-that-names-another-concept`
- `litespec/core/authoring#keep-code-out-of-intent-and-behavior-fields`
- `litespec/core/authoring#record-the-project-vocabulary-as-data`
- `litespec/core/authoring#reject-a-recorded-synonym-inside-its-scope`
- `litespec/core/authoring#review-each-term-for-its-lookalikes`
- `litespec/core/authoring#rewrite-every-link-on-a-rename`
- `litespec/core/authoring#scaffold-pages-and-reserve-identifiers`
- `litespec/core/authoring#start-each-explanation-file-with-its-title`
- `litespec/core/authoring#state-mannered-terms-literally`
- `litespec/core/authoring#state-what-happens-in-every-behavior-statement`
- `litespec/core/authoring#use-one-metadata-carrier`
- `litespec/core/authoring#use-the-declared-record-identifier-grammar`
- `litespec/core/authoring#validate-a-single-page-before-committing`
- `litespec/core/layers#close-incident-exceptions-within-seven-days`
- `litespec/core/model#give-every-use-case-its-results`
- `litespec/core/model#reach-behavior-from-every-intent-record`
- `litespec/core/model#record-a-project-wide-lowering-as-an-exception`
- `litespec/core/model#record-accepted-gaps-as-exceptions`
- `litespec/core/principles#build-only-what-the-specification-states`
- `litespec/core/principles#declare-every-edge`
- `litespec/core/principles#enter-facts-from-the-world-as-research-or-constraints`
- `litespec/core/principles#map-each-engine-step-to-one-use-case`
- `litespec/core/principles#prove-scenarios-with-tests`
- `litespec/core/principles#raise-what-a-lower-layer-needs-to-the-owner`
- `litespec/core/principles#write-each-layer-from-the-layer-above`
- `litespec/core/release#record-accepted-debt-as-an-exception`
- `litespec/core/scope#declare-the-conformance-level`
- `litespec/core/scope#keep-every-record-about-the-software`
- `litespec/core/scopes#depart-from-a-rule-only-through-a-recorded-exception`
- `litespec/core/scopes#give-every-provision-a-conformance-level`
- `litespec/core/writing#keep-company-facts-out-of-intent-prose`
- `litespec/core/writing#name-each-title-in-its-record-type-form`
- `litespec/ext/automation#bound-tenant-rule-runs-per-period`
- `litespec/ext/automation#isolate-hook-failures-from-the-trigger`
- `litespec/ext/automation#record-the-automation-catalog`
- `litespec/ext/automation#refuse-unknown-catalog-entries`
- `litespec/ext/automation#state-what-a-hook-may-not-assume`
- `litespec/ext/automation#state-what-a-hook-may-rely-on`
- `litespec/ext/automation#treat-tenant-rules-as-data`
- `litespec/ext/automation#version-catalog-entries`
- `litespec/ext/compat#declare-published-facts-as-integration-events`
- `litespec/ext/delegation#ask-only-unanswered-questions`
- `litespec/ext/delegation#hold-back-example-rows-from-the-implementing-worker`
- `litespec/ext/delegation#restrict-a-worker-to-its-paths`
- `litespec/ext/delegation#verify-before-accepting-worker-output`
- `litespec/ext/delegation#write-a-brief-before-delegating`
- `litespec/ext/delivery#apply-schema-work-before-traffic`
- `litespec/ext/delivery#keep-schema-work-out-of-startup`
- `litespec/ext/jobs#bound-the-ingest-buffer`
- `litespec/ext/jobs#declare-ingest-steps-in-order`
- `litespec/ext/jobs#declare-the-engine-carrier`
- `litespec/ext/jobs#define-the-closed-drop-reason-set`
- `litespec/ext/jobs#discard-poison-records-after-three-attempts`
- `litespec/ext/jobs#expose-drop-counts-per-reason`
- `litespec/ext/jobs#keep-engine-retry-configuration-in-implementation`
- `litespec/ext/jobs#map-each-engine-step-to-one-use-case`
- `litespec/ext/jobs#name-engine-steps-after-use-cases`
- `litespec/ext/jobs#read-enrichment-data-from-declared-sources`
- `litespec/ext/jobs#record-one-drop-reason-per-dropped-record`
- `litespec/ext/jobs#reject-unmapped-engine-steps`
- `litespec/ext/jobs#state-each-step-compensation`
- `litespec/ext/jobs#state-the-freshness-consequence`
- `litespec/ext/outbox#authenticate-and-authorize-subscriptions`
- `litespec/ext/outbox#bound-realtime-resources`
- `litespec/ext/outbox#declare-realtime-freshness`
- `litespec/ext/outbox#place-realtime-code-at-outer-boundaries`
- `litespec/ext/outbox#reconnect-and-refresh-authoritative-state`
- `litespec/ext/outbox#record-realtime-need`
- `litespec/ext/outbox#record-transport-support`
- `litespec/ext/outbox#reject-cosmetic-realtime-adoption`
- `litespec/ext/outbox#reject-unbounded-slow-client-buffering`
- `litespec/ext/outbox#revalidate-long-lived-access`
- `litespec/ext/outbox#subscribe-through-narrow-functions`
- `litespec/ext/outbox#tolerate-notification-ordering-differences`
- `litespec/ext/outbox#use-server-sent-events-for-notifications`
- `litespec/ext/outbox#use-signalr-for-interactive-messaging`
- `litespec/frontend/patterns/dialogs-and-overlays#bottom-sheet-on-a-phone`
- `litespec/frontend/patterns/forms-and-fields#keep-entered-values-after-a-refused-submit`
- `litespec/frontend/patterns/lists-and-tables#list-on-a-phone`
- `litespec/frontend/patterns/navigation#phone-navigation`
- `litespec/frontend/patterns/page-frames#record-drawer`
- `litespec/frontend/patterns/page-frames#show-one-primary-action-in-a-page-frame`
- `litespec/frontend/patterns/record-detail#hold-built-record-headers-to-one-primary-action`
- `litespec/frontend/patterns/record-detail#record-header-on-a-phone`
- `litespec/frontend/testing#keep-mocks-at-owned-boundaries`
- `litespec/frontend/ui#prove-every-result-a-route-calls`
- `litespec/frontend/web/svelte/components#write-components-in-runes-mode`
- `litespec/quality/operations#apply-schema-changes-outside-request-startup`
- `litespec/quality/operations#cover-every-quality-requirement-with-an-alert-or-a-check`
- `litespec/quality/operations#name-every-runbook-from-an-alert-job-reaction-or-failure-policy`
- `litespec/quality/operations#name-the-requirement-and-runbook-on-every-alert`
- `litespec/quality/operations#point-each-operations-file-at-its-decision`

Renamed (13):

- `litespec/backend/dotnet/application#keep-command-handlers-to-load-call-and-stage` to `litespec/backend/dotnet/application#bind-command-handlers-to-load-call-and-stage`
- `litespec/backend/dotnet/application#keep-query-handlers-free-of-writes` to `litespec/backend/dotnet/application#keep-query-handlers-free-of-repositories`
- `litespec/core/authoring#state-one-layer-per-page` to `litespec/core/authoring#state-one-tier-per-page`
- `litespec/core/release#qualify-the-checker-with-known-good-and-known-bad-fixtures` to `litespec/core/release#qualify-the-reader-with-known-good-and-known-bad-fixtures`
- `litespec/frontend/mobile/screens#restore-the-visitors-place` to `litespec/frontend/mobile/screens#restore-the-users-place`
- `litespec/frontend/patterns/admin-screens#bringing-data-from-spreadsheets` to `litespec/frontend/patterns/admin-screens#importing-data-from-a-file`
- `litespec/frontend/patterns/admin-screens#budget-codes-and-advances` to `litespec/frontend/patterns/admin-screens#budget-limits-and-advances`
- `litespec/frontend/patterns/admin-screens#supplier-invoice` to `litespec/frontend/patterns/admin-screens#subscription-invoices`
- `litespec/frontend/patterns/create-edit-remove#publishing-and-unpublishing-a-policy` to `litespec/frontend/patterns/create-edit-remove#publishing-and-unpublishing-a-draft`
- `litespec/frontend/patterns/forms-and-fields#color-field-for-the-mail-accent` to `litespec/frontend/patterns/forms-and-fields#color-field`
- `litespec/frontend/patterns/forms-and-fields#repeatable-group-for-budget-codes` to `litespec/frontend/patterns/forms-and-fields#repeatable-group`
- `litespec/frontend/patterns/lists-and-tables#trips-as-a-grid` to `litespec/frontend/patterns/lists-and-tables#the-layout-of-a-short-list`
- `litespec/frontend/patterns/record-detail#expense-record` to `litespec/frontend/patterns/record-detail#whole-record-screen`

Extensions added (2):

- `customer-rules`
- `realtime`

Extensions removed (2):

- `automation`
- `delegation`

<!-- /generated:provision-diff -->

### Part H: Upgrading a Consumer

1. Install `LiteSpec.Cli` 5.0.0 (`dotnet tool update LiteSpec.Cli --version 5.0.0`) or rerun the install script.
2. Rename `.litespec/project.json` to `.litespec/project.yaml` and write it as YAML: add the schema comment and `schemaVersion: 1`, set `reviewedLiteSpecVersion: 5.0.0`, and replace `conformance` with `layers`: `records` becomes `intent` and `behavior`; `traced` becomes all four with `implementation: own` and `proof: own`; `full` becomes all four with `litespec`, plus the `delivery` extension when the project runs alerts and runbooks.
3. Move `paths.frontends` to `frontends` and `paths.uiPackage` to `ui.package`, make `webapi` and `worker` lists, and delete the removed fields Part B names.
4. Convert the pattern catalog and the UI source lock to YAML at their 5.0.0 paths, drop `kind` and `shell` from the `DESIGN.md` metadata, and run `litespec model render` to rewrite the lock.
5. Rewrite each exception record as a departure under `.litespec/governance/departures/`: `kind: gap` keeps `expiresOn` and `exit`; `kind: departure` drops `expiresOn` when the choice is lasting; `covers` becomes a list.
6. Rename the code citations and marks of every provision the Part G list renames, and read `--format json` output for `outcome` instead of `severity` and `known`.
7. Run `litespec model check` and fix each finding.

## v4.0.0

LiteSpec 4.0.0 is a standard for specification-driven development of one bounded-context business application by people and coding agents. It defines a strict specification model of linked records beside graded engineering provisions, with git as the decision record. A consumer pins this release in `reviewedLiteSpecVersion`. The release takes effect as the `v4.0.0` tag.

### Part A: The Nature of LiteSpec

- The specification model is strict: a record follows its schema, links resolve, and code and tests mark the records they serve; a violation is a defect.
- Each provision states its strength, rule, default or advice, and where it applies; a check exists where one is cheap and sure.
- Git is the decision record: a record in the repository is decided, and the pull request that changes it is its review.
- The specification is closed: an agent builds what the specification states and takes anything missing to the owner or the backlog board.
- Every record is about the software: what the application does, holds, refuses at runtime or runs on.

### Part B: Git Is the Decision Record

- Records carry no history of their own; git carries who changed what and when.
- Completion is computed from code, tests and passing scenarios, and the book shows it first.
- An exception carries reason, exit and expiry; an expired exception is a blocking finding.
- A date about the outside world sits with its source as `readOn`.

### Part C: Eight Commands

- The CLI has eight commands: `init`, `new`, `rename`, `model check`, `model format`, `model query`, `book` and `rules`.
- `model check` is the one command CI runs: schema, folders, names, links, locality, reachability, marks and exceptions.
- `model render` has three regions: `alert-inventory`, `runbook-index` and `operations-log`.
- An additional command, finding code, analyzer rule or generated file needs an owner decision that records it.

### Part D: Provisions by Situation

- Each page declares its stack and layer; a provision adds `Applies to` only when narrower than its page.
- Every provision states `Strength`: rule, default or advice.
- The provision index carries scope and strength; the site groups Standard, Reference, Learn and LiteSpec's own records.

### Part E: Rules for the File at Hand

- `litespec rules <path or record id>` lists the applying provisions grouped by strength, with why each was retrieved; `--full` adds rationale and example.
- Project conventions and departures merge into the result; a departure shows in place of the provision it replaces.
- A retrieval evaluation with a precision target proves the command.

### Part F: The Consumer Uses the Same Structure

- Layer and artifact derive from the stack structure and the records a file marks.
- Project conventions live in `.litespec/governance/conventions/` in provision shape.
- A departure names the provision it replaces and a scope.
- Records use the same layer and artifact terms, so one query answers file, records and provisions together.

### Part G: Surfaces That State the Nature

- The README, AGENTS.md, the core pages, the command pages, the templates and this changelog state the nature above.
- The docs index groups pages with generated counts; the site navigation follows the same groups.

### Part H: Consumers Adopt the Whole Contract

- A consumer pins one release and adopts a later release by accepting its whole contract.
- A consumer declares the conformance level it claims in `.litespec/project.json`: `records`, `traced` or `full`. The levels nest, and `model check`, the book, `rules` and the analyzers report only the part of the contract the level claims.
- At `records` a project keeps its own code layout and needs no stack; at `traced` its code and tests mark their records and its test runs prove scenarios, in its own layout; at `full` it takes the selected stacks, the code layout, the analyzers and the operations provisions.
- Provision identifiers stay stable within a major version; the changelog of each release lists the provision changes since the previous tag, generated by `litespec-maintain changelog`.

### Part I: Every Record Is About the Software

- Intent reads module, then feature, then requirement; Behavior reads module, then aggregate, then use case.
- Features sit under `intent/modules/<module>/features/` with their requirements as entries; a requirement identifier has the form `requirement/<feature>.<name>`.
- A folder under `intent/modules/` that names no declared module is a blocking finding.
- Reachability blocks: a goal reaches a journey or a quality requirement, a problem reaches a feature or requirement, a feature or requirement reaches Behavior, a constraint is answered, research is cited, and a decision decides a record that reaches Behavior.
- Identifiers are short and unique in the product; the qualified identifier is computed from identifier segments plus placement, never written.

### Part J: One Placement Model for Every Layer

- Every record has one parent; the container is the parent; product, module, aggregate and entry point are the only folder nodes.
- Behavior, Implementation and Governance place each type per the placement table in the model page; Proof holds no authored records.
- Code and tests match the qualified identifier of the record they mark in folder and namespace, including frontend module folders.
- Module `dependsOn` holds no cycle; an integration event has one publishing module.

### Part K: Fields That State the Product

- Every record carries type, id, title, terms and records plus its type fields; every record carries one `summary`, and a record that makes a testable claim carries one `statement`.
- The project file carries project, size, shape, stacks, stackConfig, paths, marks, proof, dependencies, screenshots, refusals, selectedExtensions and `reviewedLiteSpecVersion`.
- `risk` sits on permission only; `setBy` uses product, operator or tenant; a rule names where retained facts live in `retainedIn`; a use case states its field read and write lifecycle.

### Part L: Complete Work Before Merging

- A branch may be incomplete; what merges is complete. `model check` reports reachability locally and blocks in the pull-request check; an exception with `expiresOn` parks one record.
- The folder wins: an identifier whose home segment disagrees with its folder is a blocking finding; each name segment is unique in the product.
- Tiers: G reads records and always blocks, S reads code and blocks where qualified, A reports and never blocks.
- An exception is live when present and unexpired.

### Part M: The Module Layout as the Default Architecture

- Layers are roles: Domain, Application, Infrastructure and WebApi with the reference table the architecture page states.
- One application project holds every module; hosts hold composition and HTTP plumbing.
- The folder tree uses closed folder sets per level; use case folders are flat; cross-module access runs only through `Contracts/` along `dependsOn`.
- The `layer-projects` extension holds the five-project layout for consumers that select it.

### Part N: Gaps Closed in the Standard

- A deviation is an exception of kind departure whose `covers` names the provision it replaces.
- Product patterns live in `implementation/patterns/`; product copy rules live in `implementation/writing/`; document pages may sit at a layer root.
- Module rules live in `behavior/modules/<module>/rules/`; adapter tests sit under the adapter's `Infrastructure/` path.
- Guidance precedence lives in one provision on the agent page.

### Part O: Checks a Partial Adopter Can Read

- Findings about Intent that no Behavior reaches yet report locally and block in the pull-request check, so Intent can run ahead of Behavior.
- The .NET reader reads several web and worker hosts, reports a default project root that matches nothing as `reader-missing`, and at `traced` finds an aggregate root by its mark and its state record.
- A JUnit test case proves a Behavior scenario through a `litespec.proves` property, so a runner whose steps carry no mark, or a stack with no code reader, still proves its scenarios.
- `model check` lists level A findings as a summary by code unless `--all` is given, and its JSON output maps G, S and A to error, warning and note.
- `init` writes a project file the schema accepts for every preset, including the `records` preset, and no size requires an exception record.

### Part P: Analyzers as Proof and a Smaller Rules Output

- A structural boundary needs an architecture test only where no analyzer or `model check` finding covers it; `analyzer-lowering-unrecorded` reports an analyzer rule lowered or switched off without an exception, so an analyzer can stand as the proof.
- LS1013 reports a switch over a state or closed-set record hierarchy that leaves a sealed case to its discard arm; the tests, not the compiler, catch a new case.
- LS2012 also reports a handler that takes the authorization guard.
- `litespec rules` prints statements only for the provisions that need judgment, the checked provisions as one line with their codes, and the layer-wide provisions by identifier; every provision on a page that names kinds of file names its own.

<!-- generated:provision-diff -->
### Provision Changes Since v3.0.0

Added (238):

- `litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case`
- `litespec/backend/architecture#read-another-module-only-through-its-contracts`
- `litespec/backend/architecture#restrict-commands-to-their-own-module-aggregates`
- `litespec/backend/architecture#subscribe-across-modules-only-to-integration-events`
- `litespec/backend/dotnet/api#place-endpoints-in-their-use-case-folders`
- `litespec/backend/dotnet/application#use-the-use-case-folder-layout`
- `litespec/backend/dotnet/architecture#follow-declared-module-dependencies`
- `litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file`
- `litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders`
- `litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level`
- `litespec/backend/dotnet/architecture#keep-host-endpoints-in-hosts`
- `litespec/backend/dotnet/architecture#keep-use-case-folders-isolated`
- `litespec/backend/dotnet/architecture#mirror-modules-aggregates-and-use-cases-in-one-namespace-tree`
- `litespec/backend/dotnet/architecture#reach-another-module-only-through-contracts`
- `litespec/backend/dotnet/architecture#separate-the-shared-kernel-from-the-modules`
- `litespec/backend/dotnet/architecture#use-one-public-assembly-marker`
- `litespec/backend/dotnet/domain#use-the-domain-layer-folder-layout`
- `litespec/backend/dotnet/ext/layer-projects#keep-application-dependencies-narrow`
- `litespec/backend/dotnet/ext/layer-projects#keep-domain-package-free`
- `litespec/backend/dotnet/ext/layer-projects#keep-net-production-and-test-projects-separate`
- `litespec/backend/dotnet/ext/layer-projects#keep-one-application-assembly`
- `litespec/backend/dotnet/ext/layer-projects#keep-the-test-projects-the-size-requires`
- `litespec/backend/dotnet/ext/layer-projects#mirror-modules-in-folders-and-namespaces`
- `litespec/backend/dotnet/ext/layer-projects#run-litespecanalyzers-in-every-application-project`
- `litespec/backend/dotnet/ext/layer-projects#use-five-application-projects`
- `litespec/backend/dotnet/ext/layer-projects#use-the-domain-module-layout`
- `litespec/backend/dotnet/ext/layer-projects#use-the-endpoint-folder-layout`
- `litespec/backend/dotnet/ext/layer-projects#use-the-operation-folder-layout`
- `litespec/backend/dotnet/ext/layer-projects#use-the-project-reference-graph`
- `litespec/backend/dotnet/ext/layer-projects#use-this-baseline-package-ownership`
- `litespec/backend/dotnet/ext/layer-projects#use-this-infrastructure-layout`
- `litespec/backend/dotnet/packages#run-litespec-analyzers-in-the-business-projects-and-hosts`
- `litespec/backend/dotnet/persistence#use-the-persistence-layer-folder-layout`
- `litespec/backend/dotnet/stack#identify-an-aggregate-root-by-its-mark-and-state-record-at-traced`
- `litespec/backend/dotnet/stack#read-every-declared-web-and-worker-host`
- `litespec/backend/dotnet/stack#report-a-stack-root-that-matches-no-folder`
- `litespec/core/access#keep-permission-lists-to-entries`
- `litespec/core/agent#load-applicable-provisions-before-editing`
- `litespec/core/agent#name-judged-provisions-in-the-completion-report`
- `litespec/core/agent#raise-open-figures-to-the-owner`
- `litespec/core/authoring#generate-the-provision-diff-for-each-release`
- `litespec/core/domain-model#name-each-module-after-its-capability`
- `litespec/core/intent#admit-only-outside-pains-as-problems`
- `litespec/core/intent#define-each-recurring-capitalized-phrase`
- `litespec/core/intent#describe-every-journey`
- `litespec/core/intent#explain-every-problem-in-four-sections`
- `litespec/core/intent#grade-problem-evidence-by-kind`
- `litespec/core/intent#group-product-records-by-module`
- `litespec/core/intent#keep-actors-to-people-and-organizations`
- `litespec/core/intent#keep-company-operation-out-of-intent`
- `litespec/core/intent#move-every-goal-with-a-journey-or-a-quality-requirement`
- `litespec/core/intent#name-each-sibling-problem-or-merge-it`
- `litespec/core/intent#name-every-actor-from-a-journey-a-problem-or-an-access-position`
- `litespec/core/intent#name-no-solution-in-a-problem-statement`
- `litespec/core/intent#perform-each-journey-step-with-an-actor-or-a-system`
- `litespec/core/intent#reach-every-intent-record-with-behavior`
- `litespec/core/intent#shape-each-module-page`
- `litespec/core/intent#state-each-feature-with-its-offering`
- `litespec/core/intent#state-the-cost-and-workaround-of-every-problem`
- `litespec/core/layers#raise-open-needs-as-owner-questions-or-backlog-items`
- `litespec/core/layers#send-commercial-and-observable-figures-to-the-owner`
- `litespec/core/maintaining#keep-provision-identifiers-stable-within-a-major-version`
- `litespec/core/model#compute-completion-in-the-builder`
- `litespec/core/model#connect-across-modules-only-through-declared-dependencies`
- `litespec/core/model#declare-size-and-stacks`
- `litespec/core/model#hold-module-rules-in-rule-sets`
- `litespec/core/model#keep-each-name-unique-in-the-product`
- `litespec/core/model#keep-module-dependencies-free-of-cycles`
- `litespec/core/model#move-each-identifier-with-its-module`
- `litespec/core/model#publish-each-integration-event-from-one-module`
- `litespec/core/model#reach-behavior-from-every-intent-record`
- `litespec/core/model#record-each-screens-captures-from-the-gallery`
- `litespec/core/model#take-decision-numbers-from-one-sequence`
- `litespec/core/principles#build-only-what-the-specification-states`
- `litespec/core/principles#complete-work-before-merging`
- `litespec/core/principles#compute-completion-from-code-and-tests`
- `litespec/core/principles#keep-git-as-the-decision-record`
- `litespec/core/principles#keep-tools-small`
- `litespec/core/principles#raise-what-a-lower-layer-needs-to-the-owner`
- `litespec/core/principles#state-the-strength-of-every-provision`
- `litespec/core/principles#treat-code-beyond-silent-intent-as-evidence-for-an-owner-question`
- `litespec/core/principles#write-every-record-about-the-software`
- `litespec/core/privacy#complete-the-subject-export-before-erasure`
- `litespec/core/privacy#discard-rows-that-leave-no-trace`
- `litespec/core/privacy#keep-a-tombstone-where-deletion-stays-visible`
- `litespec/core/privacy#keep-tombstone-fields-minimal`
- `litespec/core/privacy#move-data-to-its-next-controller-instead-of-erasing`
- `litespec/core/privacy#purge-erased-data-from-restored-backups`
- `litespec/core/privacy#record-which-duty-wins-each-conflict`
- `litespec/core/privacy#remove-child-rows-through-their-parent-only`
- `litespec/core/privacy#require-a-fresh-export-after-changes`
- `litespec/core/privacy#run-one-sweep-job-per-data-class`
- `litespec/core/privacy#scope-erasure-to-what-the-subject-requested`
- `litespec/core/privacy#state-the-erasure-sentence-in-intent`
- `litespec/core/scope#declare-the-conformance-level`
- `litespec/core/scope#keep-every-litespec-file-under-one-folder`
- `litespec/core/scope#keep-every-record-about-the-software`
- `litespec/core/scopes#declare-the-page-scope`
- `litespec/core/scopes#depart-from-a-rule-only-through-a-recorded-exception`
- `litespec/core/scopes#give-every-provision-a-conformance-level`
- `litespec/core/scopes#name-the-artifact-of-every-provision-on-a-page-that-names-one`
- `litespec/core/scopes#record-each-replaced-default`
- `litespec/core/scopes#state-applies-to-only-when-narrower`
- `litespec/core/scopes#use-only-declared-scope-values`
- `litespec/core/system#drive-work-from-records`
- `litespec/core/use-cases#write-prose-on-every-use-case`
- `litespec/core/writing#apply-plain-mechanics`
- `litespec/core/writing#comment-on-why-not-what`
- `litespec/core/writing#compare-real-options-in-each-decision-record`
- `litespec/core/writing#give-each-explanation-file-its-fixed-sections`
- `litespec/core/writing#give-each-paragraph-one-idea`
- `litespec/core/writing#keep-company-facts-out-of-intent-prose`
- `litespec/core/writing#keep-each-record-sentence-within-twenty-five-words`
- `litespec/core/writing#name-each-title-in-its-record-type-form`
- `litespec/core/writing#open-each-summary-with-who-and-what-is-at-stake`
- `litespec/core/writing#put-the-actor-first`
- `litespec/core/writing#state-each-statement-without-modal-words`
- `litespec/core/writing#use-literal-words`
- `litespec/core/writing#write-each-changelog-as-a-release-comparison`
- `litespec/core/writing#write-each-documentation-page-as-one-kind`
- `litespec/core/writing#write-every-record-title-in-title-case`
- `litespec/core/writing#write-for-the-reader-who-arrives-cold`
- `litespec/ext/automation#record-the-automation-catalog`
- `litespec/ext/compat#record-breaking-behavior-changes-as-decisions`
- `litespec/ext/compat#reject-breaking-diffs-without-authorization`
- `litespec/ext/delivery#accept-every-shipped-package-license-explicitly`
- `litespec/ext/delivery#apply-oci-release-labels`
- `litespec/ext/delivery#apply-schema-work-before-traffic`
- `litespec/ext/delivery#apply-the-documented-defaults`
- `litespec/ext/delivery#attach-dashboard-links-to-production-objectives`
- `litespec/ext/delivery#build-from-the-workspace-root`
- `litespec/ext/delivery#build-release-images-in-stages`
- `litespec/ext/delivery#copy-only-runtime-files`
- `litespec/ext/delivery#cover-the-baseline-alert-set`
- `litespec/ext/delivery#declare-writable-paths-and-limits`
- `litespec/ext/delivery#define-actionable-baseline-alerts`
- `litespec/ext/delivery#define-backup-and-restore-behavior`
- `litespec/ext/delivery#enforce-the-supply-chain-gate`
- `litespec/ext/delivery#exclude-build-material-from-runtime-images`
- `litespec/ext/delivery#exclude-build-timestamps-from-contracts`
- `litespec/ext/delivery#exclude-environment-configuration-from-images`
- `litespec/ext/delivery#exclude-insecure-frontend-secret-carriers`
- `litespec/ext/delivery#exercise-a-restore-before-the-first-production-data`
- `litespec/ext/delivery#exercise-every-runbook-before-release`
- `litespec/ext/delivery#keep-a-canonical-job-graph`
- `litespec/ext/delivery#keep-a-first-release-readiness-record`
- `litespec/ext/delivery#keep-deployment-descriptors-together`
- `litespec/ext/delivery#keep-each-alert-threshold-to-its-requirements-measure`
- `litespec/ext/delivery#keep-mutable-data-external`
- `litespec/ext/delivery#keep-rollback-executable`
- `litespec/ext/delivery#keep-schema-work-out-of-startup`
- `litespec/ext/delivery#keep-scripts-at-the-root`
- `litespec/ext/delivery#limit-container-privileges`
- `litespec/ext/delivery#match-every-shipped-package-to-its-pin`
- `litespec/ext/delivery#meet-release-readiness-gates`
- `litespec/ext/delivery#place-dockerfiles-beside-deployables`
- `litespec/ext/delivery#promote-verified-artifacts`
- `litespec/ext/delivery#protect-the-default-branch`
- `litespec/ext/delivery#provide-release-data-recovery`
- `litespec/ext/delivery#provide-release-delivery-controls`
- `litespec/ext/delivery#provide-release-operating-visibility`
- `litespec/ext/delivery#publish-a-book-preview-for-every-change`
- `litespec/ext/delivery#record-each-drill-run-as-a-check-result`
- `litespec/ext/delivery#record-every-branch-protection-bypass`
- `litespec/ext/delivery#record-image-digests`
- `litespec/ext/delivery#record-operating-readiness`
- `litespec/ext/delivery#record-release-evidence`
- `litespec/ext/delivery#report-liveness-from-every-recurring-job`
- `litespec/ext/delivery#review-schema-artifacts`
- `litespec/ext/delivery#route-each-severity-to-a-declared-destination`
- `litespec/ext/delivery#run-images-as-non-root`
- `litespec/ext/delivery#run-the-application-as-pid-1`
- `litespec/ext/delivery#separate-frontend-configuration-classes`
- `litespec/ext/delivery#set-sufficient-shutdown-time`
- `litespec/ext/delivery#state-availability-objectives-as-openslo-files`
- `litespec/ext/delivery#supply-runtime-configuration-externally`
- `litespec/ext/delivery#test-released-traffic`
- `litespec/ext/delivery#treat-check-warnings-as-failures`
- `litespec/ext/delivery#use-a-repeatable-deployment`
- `litespec/ext/delivery#use-protected-frontend-secret-delivery`
- `litespec/ext/delivery#use-stable-service-names`
- `litespec/ext/delivery#wait-for-replica-readiness`
- `litespec/ext/delivery#write-runtime-logs-to-streams`
- `litespec/ext/jobs#apply-item-read-authorization`
- `litespec/ext/jobs#audit-export-reads`
- `litespec/ext/jobs#avoid-speculative-reporting-systems`
- `litespec/ext/jobs#bound-the-ingest-buffer`
- `litespec/ext/jobs#confirm-bulk-money-actions-out-of-band`
- `litespec/ext/jobs#declare-export-paging`
- `litespec/ext/jobs#declare-ingest-steps-in-order`
- `litespec/ext/jobs#declare-the-engine-carrier`
- `litespec/ext/jobs#define-export-encoding-and-columns`
- `litespec/ext/jobs#define-report-limits`
- `litespec/ext/jobs#define-the-closed-drop-reason-set`
- `litespec/ext/jobs#discard-poison-records-after-three-attempts`
- `litespec/ext/jobs#escape-formula-leading-export-values`
- `litespec/ext/jobs#expose-drop-counts-per-reason`
- `litespec/ext/jobs#keep-engine-retry-configuration-in-implementation`
- `litespec/ext/jobs#keep-raw-sql-in-infrastructure`
- `litespec/ext/jobs#keep-report-definitions-in-application`
- `litespec/ext/jobs#keep-report-providers-in-infrastructure`
- `litespec/ext/jobs#map-each-engine-step-to-one-use-case`
- `litespec/ext/jobs#name-engine-steps-after-use-cases`
- `litespec/ext/jobs#order-queue-items-by-score`
- `litespec/ext/jobs#order-unscored-queues-by-age`
- `litespec/ext/jobs#parameterize-report-sql`
- `litespec/ext/jobs#protect-export-output`
- `litespec/ext/jobs#read-enrichment-data-from-declared-sources`
- `litespec/ext/jobs#record-one-drop-reason-per-dropped-record`
- `litespec/ext/jobs#record-reporting-need`
- `litespec/ext/jobs#reject-unmapped-engine-steps`
- `litespec/ext/jobs#release-claims-past-their-lease`
- `litespec/ext/jobs#review-report-plans`
- `litespec/ext/jobs#run-budget-exceeding-exports-in-worker`
- `litespec/ext/jobs#scope-export-output-to-the-tenant`
- `litespec/ext/jobs#separate-worker-orchestration`
- `litespec/ext/jobs#show-a-preview-before-bulk-money-actions`
- `litespec/ext/jobs#show-the-score-beside-each-item`
- `litespec/ext/jobs#state-each-step-compensation`
- `litespec/ext/jobs#state-the-claim-mode`
- `litespec/ext/jobs#state-the-freshness-consequence`
- `litespec/ext/jobs#support-report-cancellation`
- `litespec/ext/outbox#authenticate-and-authorize-subscriptions`
- `litespec/ext/outbox#bound-realtime-resources`
- `litespec/ext/outbox#declare-realtime-freshness`
- `litespec/ext/outbox#place-realtime-code-at-outer-boundaries`
- `litespec/ext/outbox#reconnect-and-refresh-authoritative-state`
- `litespec/ext/outbox#record-realtime-need`
- `litespec/ext/outbox#record-transport-support`
- `litespec/ext/outbox#reject-cosmetic-realtime-adoption`
- `litespec/ext/outbox#reject-unbounded-slow-client-buffering`
- `litespec/ext/outbox#revalidate-long-lived-access`
- `litespec/ext/outbox#subscribe-through-narrow-functions`
- `litespec/ext/outbox#tolerate-notification-ordering-differences`
- `litespec/ext/outbox#use-server-sent-events-for-notifications`
- `litespec/ext/outbox#use-signalr-for-interactive-messaging`
- `litespec/frontend/web/components#keep-component-boundaries-typed`
- `litespec/quality/operations#name-every-runbook-from-an-alert-job-reaction-or-failure-policy`

Removed (439):

- `litespec/backend/api#state-an-unexpressible-transition-as-a-custom-method`
- `litespec/backend/api#use-put-for-a-whole-replacement`
- `litespec/backend/architecture#keep-one-application-part`
- `litespec/backend/architecture#organize-every-layer-by-module-and-use-case`
- `litespec/backend/architecture#use-five-application-parts`
- `litespec/backend/dotnet/api#use-the-endpoint-folder-layout`
- `litespec/backend/dotnet/application#use-the-operation-folder-layout`
- `litespec/backend/dotnet/architecture#keep-one-application-assembly`
- `litespec/backend/dotnet/architecture#mirror-modules-in-folders-and-namespaces`
- `litespec/backend/dotnet/architecture#use-five-application-projects`
- `litespec/backend/dotnet/architecture#use-one-public-assembly-marker-per-scanned-project`
- `litespec/backend/dotnet/config#bind-setting-records-to-options`
- `litespec/backend/dotnet/config#validate-backend-options-at-startup`
- `litespec/backend/dotnet/domain#use-the-domain-module-layout`
- `litespec/backend/dotnet/ext/efcore#apply-migrations-as-release-work`
- `litespec/backend/dotnet/ext/efcore#avoid-per-aggregate-read-stores`
- `litespec/backend/dotnet/ext/efcore#commit-ef-core-command-work-once`
- `litespec/backend/dotnet/ext/efcore#configure-ef-core-mappings-in-infrastructure`
- `litespec/backend/dotnet/ext/efcore#configure-optimistic-tokens`
- `litespec/backend/dotnet/ext/efcore#configure-relational-details-explicitly`
- `litespec/backend/dotnet/ext/efcore#define-ef-core-query-roots`
- `litespec/backend/dotnet/ext/efcore#exclude-direct-handler-commits`
- `litespec/backend/dotnet/ext/efcore#generate-migrations-for-schema-changes`
- `litespec/backend/dotnet/ext/efcore#implement-query-roots-in-infrastructure`
- `litespec/backend/dotnet/ext/efcore#keep-migration-files-together`
- `litespec/backend/dotnet/ext/efcore#map-ef-core-concurrency-conflict`
- `litespec/backend/dotnet/ext/efcore#map-stable-relational-state-shape`
- `litespec/backend/dotnet/ext/efcore#map-unique-constraint-violations-to-their-refusals`
- `litespec/backend/dotnet/ext/efcore#map-unsupported-state-shapes-in-infrastructure`
- `litespec/backend/dotnet/ext/efcore#process-changed-aggregate-events`
- `litespec/backend/dotnet/ext/efcore#project-ef-core-queries-directly`
- `litespec/backend/dotnet/ext/efcore#record-ef-core-replacement-scope`
- `litespec/backend/dotnet/ext/efcore#record-provider-selection`
- `litespec/backend/dotnet/ext/efcore#register-one-command-post-handler`
- `litespec/backend/dotnet/ext/efcore#reject-automatic-command-retry`
- `litespec/backend/dotnet/ext/efcore#reject-cross-provider-outbox-storage`
- `litespec/backend/dotnet/ext/efcore#reject-mixed-provider-writes`
- `litespec/backend/dotnet/ext/efcore#reject-startup-schema-mutation`
- `litespec/backend/dotnet/ext/efcore#resolve-cross-provider-invariants`
- `litespec/backend/dotnet/ext/efcore#resolve-the-tenant-at-context-creation`
- `litespec/backend/dotnet/ext/efcore#retain-domain-state-values`
- `litespec/backend/dotnet/ext/efcore#retain-marten-query-boundary`
- `litespec/backend/dotnet/ext/efcore#retain-unaffected-baseline-persistence`
- `litespec/backend/dotnet/ext/efcore#review-migration-effects`
- `litespec/backend/dotnet/ext/efcore#review-new-persisted-states`
- `litespec/backend/dotnet/ext/efcore#stage-aggregate-writes-in-repositories`
- `litespec/backend/dotnet/ext/efcore#stage-ef-core-outbox-records-together`
- `litespec/backend/dotnet/ext/efcore#test-migration-starting-states`
- `litespec/backend/dotnet/ext/efcore#use-direct-state-mapping-only-when-supported`
- `litespec/backend/dotnet/ext/efcore#use-durable-outbox-delivery`
- `litespec/backend/dotnet/ext/efcore#use-one-write-provider`
- `litespec/backend/dotnet/ext/efcore#use-snake-case-relational-names`
- `litespec/backend/dotnet/ext/efcore#use-the-ef-core-infrastructure-layout`
- `litespec/backend/dotnet/naming#group-event-handlers-by-the-fact-they-handle`
- `litespec/backend/dotnet/naming#name-asynchronous-methods-completely`
- `litespec/backend/dotnet/naming#name-repository-variables-after-their-interface`
- `litespec/backend/dotnet/naming#name-result-parts-after-their-result`
- `litespec/backend/dotnet/packages#keep-application-dependencies-narrow`
- `litespec/backend/dotnet/packages#keep-domain-package-free`
- `litespec/backend/dotnet/packages#pin-every-dependency-centrally`
- `litespec/backend/dotnet/packages#run-litespecanalyzers-in-every-application-project`
- `litespec/backend/dotnet/packages#use-the-project-reference-graph`
- `litespec/backend/dotnet/packages#use-this-baseline-package-ownership`
- `litespec/backend/dotnet/persistence#use-this-infrastructure-layout`
- `litespec/backend/dotnet/testing#enforce-architecture-rules`
- `litespec/backend/dotnet/testing#gate-changed-lines-with-mutation-testing`
- `litespec/backend/dotnet/testing#generate-model-tests-from-declared-transitions`
- `litespec/backend/dotnet/testing#keep-the-test-projects-the-size-requires`
- `litespec/backend/dotnet/testing#prove-each-scenario-with-a-test-that-calls-its-use-case`
- `litespec/backend/dotnet/testing#seed-property-tests-from-example-tables`
- `litespec/backend/dotnet/testing#test-http-integration-through-the-real-host`
- `litespec/backend/dotnet/testing#use-one-production-faithful-integration-harness`
- `litespec/backend/dotnet/workspace#declare-size-and-stacks-in-project-config`
- `litespec/backend/dotnet/workspace#keep-net-production-and-test-projects-separate`
- `litespec/backend/identifiers#keep-aggregate-names-unique-across-the-project`
- `litespec/backend/identifiers#keep-use-case-names-unique-across-the-project`
- `litespec/backend/testing#assert-architecture-boundaries-with-tests`
- `litespec/core/access#keep-permission-and-entry-point-lists-to-entries`
- `litespec/core/access#name-the-actor-for-every-external-system`
- `litespec/core/access#route-cross-tenant-access-through-the-security-queue`
- `litespec/core/agent#mark-transcribed-records-during-adoption`
- `litespec/core/agent#raise-questions-in-ballots-with-recommendations`
- `litespec/core/authoring#check-the-vocabulary-on-every-surface-a-reader-meets`
- `litespec/core/domain-model#close-every-saga-with-named-exits-and-a-driver`
- `litespec/core/domain-model#prove-every-saga-guard-and-exit-with-a-scenario`
- `litespec/core/domain-model#state-each-saga-step-with-its-compensation`
- `litespec/core/intent#group-product-records-by-area`
- `litespec/core/intent#group-questions-into-ballots`
- `litespec/core/intent#leave-business-choices-to-the-decider`
- `litespec/core/intent#name-the-person-who-approved-a-decision`
- `litespec/core/intent#replace-a-decision-with-a-new-record`
- `litespec/core/intent#shape-each-topic-page`
- `litespec/core/intent#state-each-feature-with-its-lifecycle`
- `litespec/core/language#name-each-change-code-with-a-past-participle`
- `litespec/core/layers#flag-records-when-a-parent-record-changes`
- `litespec/core/layers#raise-commercial-and-observable-figures-as-questions`
- `litespec/core/layers#raise-open-needs-as-draft-questions`
- `litespec/core/model#answer-each-question-on-its-record`
- `litespec/core/model#apply-guidance-precedence`
- `litespec/core/model#approve-tier-1-records-by-a-person`
- `litespec/core/model#declare-size-stacks-and-optional-types`
- `litespec/core/model#derive-generic-refusals`
- `litespec/core/model#end-transcription-at-the-named-decision`
- `litespec/core/model#flag-every-record-whose-parent-changed`
- `litespec/core/model#hold-approval-behind-draft-decisions`
- `litespec/core/model#hold-approval-behind-open-questions`
- `litespec/core/model#hold-each-finding-to-its-own-status`
- `litespec/core/model#hold-transcribed-records-in-draft`
- `litespec/core/model#keep-one-owner-and-status-on-every-record`
- `litespec/core/model#record-approval-as-data`
- `litespec/core/model#resolve-ties-by-the-later-decision`
- `litespec/core/model#retire-records-with-replaces`
- `litespec/core/model#use-the-layer-status-vocabulary`
- `litespec/core/principles#classify-record-authority`
- `litespec/core/principles#locate-record-status-in-frontmatter`
- `litespec/core/principles#raise-what-a-lower-layer-needs-as-a-question`
- `litespec/core/principles#refuse-approval-behind-a-draft-decision`
- `litespec/core/principles#render-consequences-as-context-outside-approval`
- `litespec/core/principles#retire-public-behavior-deliberately`
- `litespec/core/principles#separate-authority-from-implementation`
- `litespec/core/principles#treat-code-beyond-silent-intent-as-evidence-for-a-question`
- `litespec/core/privacy#review-every-erasure-exemption-before-its-date`
- `litespec/core/release#advise-without-gating-as-an-agent`
- `litespec/core/release#approve-drafts-in-bounded-batches`
- `litespec/core/release#compute-the-review-queue-from-the-model`
- `litespec/core/release#draw-each-tier-3-sample-from-the-semantic-hash`
- `litespec/core/release#hold-only-the-blocked-records-of-a-type-batch`
- `litespec/core/release#keep-a-first-release-readiness-record`
- `litespec/core/release#keep-unfinished-behavior-in-draft`
- `litespec/core/release#meet-release-readiness-gates`
- `litespec/core/release#name-the-security-reviewer-in-the-project-file`
- `litespec/core/release#provide-release-data-recovery`
- `litespec/core/release#provide-release-delivery-controls`
- `litespec/core/release#provide-release-operating-visibility`
- `litespec/core/release#record-operating-readiness`
- `litespec/core/release#record-release-evidence`
- `litespec/core/release#refuse-a-batch-holding-an-open-question`
- `litespec/core/release#refuse-a-batch-without-a-named-approver`
- `litespec/core/release#refuse-a-record-whose-effective-status-stays-draft`
- `litespec/core/release#refuse-self-review-alone-for-tier-1-behavior`
- `litespec/core/release#require-passing-scenarios-and-advisory-review-for-tier-1-behavior`
- `litespec/core/release#review-behavior-changes-through-the-behavior-diff`
- `litespec/core/release#review-intent-changes-through-the-intent-diff-and-ballots`
- `litespec/core/release#review-the-security-queue-separately-from-reviewed`
- `litespec/core/release#sample-tier-3-behavior-reviews`
- `litespec/core/release#treat-check-warnings-as-failures`
- `litespec/core/release#write-a-manifest-for-every-batch-approval`
- `litespec/core/scope#record-the-review-configuration`
- `litespec/core/scope#record-the-transcription-endpoint`
- `litespec/core/system#drive-work-from-approved-records`
- `litespec/core/use-cases#write-prose-on-significant-use-cases`
- `litespec/ext/automation#review-the-automation-catalog`
- `litespec/ext/compat#raise-breaking-behavior-changes-as-intent-questions`
- `litespec/ext/compat#reject-unapproved-breaking-diffs`
- `litespec/ext/containers#apply-oci-release-labels`
- `litespec/ext/containers#apply-schema-work-before-traffic`
- `litespec/ext/containers#build-from-the-workspace-root`
- `litespec/ext/containers#build-release-images-in-stages`
- `litespec/ext/containers#copy-only-runtime-files`
- `litespec/ext/containers#declare-writable-paths-and-limits`
- `litespec/ext/containers#exclude-build-material-from-runtime-images`
- `litespec/ext/containers#exclude-build-timestamps-from-contracts`
- `litespec/ext/containers#exclude-environment-configuration-from-images`
- `litespec/ext/containers#exclude-insecure-frontend-secret-carriers`
- `litespec/ext/containers#keep-mutable-data-external`
- `litespec/ext/containers#keep-schema-work-out-of-startup`
- `litespec/ext/containers#limit-container-privileges`
- `litespec/ext/containers#place-dockerfiles-beside-deployables`
- `litespec/ext/containers#record-image-digests`
- `litespec/ext/containers#retain-rollback-material`
- `litespec/ext/containers#run-images-as-non-root`
- `litespec/ext/containers#run-the-application-as-pid-1`
- `litespec/ext/containers#separate-frontend-configuration-classes`
- `litespec/ext/containers#set-sufficient-shutdown-time`
- `litespec/ext/containers#supply-runtime-configuration-externally`
- `litespec/ext/containers#test-released-traffic`
- `litespec/ext/containers#use-protected-frontend-secret-delivery`
- `litespec/ext/containers#wait-for-replica-readiness`
- `litespec/ext/containers#write-runtime-logs-to-streams`
- `litespec/ext/pipeline#bound-the-ingest-buffer`
- `litespec/ext/pipeline#declare-ingest-steps-in-order`
- `litespec/ext/pipeline#define-the-closed-drop-reason-set`
- `litespec/ext/pipeline#discard-poison-records-after-three-attempts`
- `litespec/ext/pipeline#expose-drop-counts-per-reason`
- `litespec/ext/pipeline#read-enrichment-data-from-declared-sources`
- `litespec/ext/pipeline#record-one-drop-reason-per-dropped-record`
- `litespec/ext/pipeline#state-the-freshness-consequence`
- `litespec/ext/plans#record-plan-prices-as-business-requirement-values`
- `litespec/ext/privacy#approve-the-erasure-sentence-in-intent`
- `litespec/ext/privacy#complete-the-subject-export-before-erasure`
- `litespec/ext/privacy#discard-rows-that-leave-no-trace`
- `litespec/ext/privacy#keep-a-tombstone-where-deletion-stays-visible`
- `litespec/ext/privacy#keep-tombstone-fields-minimal`
- `litespec/ext/privacy#move-data-to-its-next-controller-instead-of-erasing`
- `litespec/ext/privacy#purge-erased-data-from-restored-backups`
- `litespec/ext/privacy#record-which-duty-wins-each-conflict`
- `litespec/ext/privacy#remove-child-rows-through-their-parent-only`
- `litespec/ext/privacy#require-a-fresh-export-after-changes`
- `litespec/ext/privacy#run-one-sweep-job-per-data-class`
- `litespec/ext/privacy#scope-erasure-to-what-the-subject-requested`
- `litespec/ext/queues#confirm-bulk-money-actions-out-of-band`
- `litespec/ext/queues#order-queue-items-by-score`
- `litespec/ext/queues#order-unscored-queues-by-age`
- `litespec/ext/queues#release-claims-past-their-lease`
- `litespec/ext/queues#show-a-preview-before-bulk-money-actions`
- `litespec/ext/queues#show-the-score-beside-each-item`
- `litespec/ext/queues#state-the-claim-mode`
- `litespec/ext/realtime#authenticate-and-authorize-subscriptions`
- `litespec/ext/realtime#bound-realtime-resources`
- `litespec/ext/realtime#declare-realtime-freshness`
- `litespec/ext/realtime#place-realtime-code-at-outer-boundaries`
- `litespec/ext/realtime#reconnect-and-refresh-authoritative-state`
- `litespec/ext/realtime#record-realtime-need`
- `litespec/ext/realtime#record-transport-support`
- `litespec/ext/realtime#reject-cosmetic-realtime-adoption`
- `litespec/ext/realtime#reject-unbounded-slow-client-buffering`
- `litespec/ext/realtime#revalidate-long-lived-access`
- `litespec/ext/realtime#subscribe-through-narrow-functions`
- `litespec/ext/realtime#tolerate-notification-ordering-differences`
- `litespec/ext/realtime#use-server-sent-events-for-notifications`
- `litespec/ext/realtime#use-signalr-for-interactive-messaging`
- `litespec/ext/report#apply-item-read-authorization`
- `litespec/ext/report#audit-export-reads`
- `litespec/ext/report#avoid-speculative-reporting-systems`
- `litespec/ext/report#declare-export-paging`
- `litespec/ext/report#define-export-encoding-and-columns`
- `litespec/ext/report#define-report-limits`
- `litespec/ext/report#escape-formula-leading-export-values`
- `litespec/ext/report#keep-raw-sql-in-infrastructure`
- `litespec/ext/report#keep-report-definitions-in-application`
- `litespec/ext/report#keep-report-providers-in-infrastructure`
- `litespec/ext/report#parameterize-report-sql`
- `litespec/ext/report#protect-export-output`
- `litespec/ext/report#record-reporting-need`
- `litespec/ext/report#review-report-plans`
- `litespec/ext/report#run-budget-exceeding-exports-in-worker`
- `litespec/ext/report#scope-export-output-to-the-tenant`
- `litespec/ext/report#separate-worker-orchestration`
- `litespec/ext/report#support-report-cancellation`
- `litespec/ext/site#avoid-client-scripts-without-a-named-interactive-need`
- `litespec/ext/site#declare-each-static-site-in-the-project-record`
- `litespec/ext/site#default-to-a-light-scheme-with-a-reader-chosen-dark-scheme`
- `litespec/ext/site#generate-a-sitemap-covering-every-page`
- `litespec/ext/site#generate-the-site-with-astro`
- `litespec/ext/site#give-every-page-one-main-heading-and-landmark-elements`
- `litespec/ext/site#keep-each-repeated-fact-in-one-data-source`
- `litespec/ext/site#list-every-page-for-language-models`
- `litespec/ext/site#load-every-script-from-the-site-origin`
- `litespec/ext/site#load-nothing-from-a-third-party-origin`
- `litespec/ext/site#mark-pages-with-json-ld-structured-data`
- `litespec/ext/site#mark-unfinished-facts-with-visible-placeholders`
- `litespec/ext/site#name-every-page-with-a-title-and-a-description`
- `litespec/ext/site#point-crawler-rules-at-the-sitemap`
- `litespec/ext/site#point-every-page-at-its-absolute-canonical-address`
- `litespec/ext/site#publish-a-markdown-copy-beside-every-page`
- `litespec/ext/site#publish-full-page-text-for-language-models`
- `litespec/ext/site#read-prices-and-claims-from-brief-and-business-requirements`
- `litespec/ext/site#read-site-prices-and-claims-from-consumer-records`
- `litespec/ext/site#reference-language-alternates-from-every-translated-page`
- `litespec/ext/site#reject-a-release-build-carrying-a-placeholder`
- `litespec/ext/site#render-components-on-the-server-with-astro-or-svelte`
- `litespec/ext/site#render-every-language-from-one-page-structure`
- `litespec/ext/site#render-every-page-to-static-files`
- `litespec/ext/site#state-allowed-sources-in-a-content-security-policy`
- `litespec/ext/site#use-tailwind-for-site-styles`
- `litespec/ext/stage#end-transcription-at-the-named-decision`
- `litespec/ext/stage#hold-transcribed-records-in-draft`
- `litespec/ext/stage#record-rested-on-draft-intent`
- `litespec/ext/stage#resolve-rested-on-draft-intent-at-promotion`
- `litespec/ext/tenancy#review-delegated-cross-tenant-grants-through-the-security-queue`
- `litespec/ext/workflow-engine#declare-the-engine-carrier`
- `litespec/ext/workflow-engine#keep-engine-retry-configuration-in-implementation`
- `litespec/ext/workflow-engine#map-each-engine-step-to-one-use-case`
- `litespec/ext/workflow-engine#name-engine-steps-after-use-cases`
- `litespec/ext/workflow-engine#reject-unmapped-engine-steps`
- `litespec/ext/workflow-engine#state-each-step-compensation`
- `litespec/frontend/blazor/browser#bound-what-is-stored`
- `litespec/frontend/blazor/browser#keep-a-stable-serialization-contract`
- `litespec/frontend/blazor/browser#own-storage-in-infrastructure`
- `litespec/frontend/blazor/browser#provide-a-migration-for-every-changed-shape`
- `litespec/frontend/blazor/browser#provide-export-and-import`
- `litespec/frontend/blazor/browser#record-the-classification`
- `litespec/frontend/blazor/browser#select-the-storage-mechanism-per-shape`
- `litespec/frontend/blazor/browser#treat-reads-as-fallible`
- `litespec/frontend/blazor/browser#version-and-scope-every-storage-key`
- `litespec/frontend/blazor/components#apply-one-styling-system`
- `litespec/frontend/blazor/components#declare-every-parameter-type`
- `litespec/frontend/blazor/components#keep-accessibility-in-the-component`
- `litespec/frontend/blazor/components#keep-components-presentational`
- `litespec/frontend/blazor/components#name-for-the-boundary-role`
- `litespec/frontend/blazor/components#own-the-layer-contract`
- `litespec/frontend/blazor/components#place-one-component-per-file`
- `litespec/frontend/blazor/components#raise-events-rather-than-mutating`
- `litespec/frontend/blazor/components#resolve-browser-behavior-through-an-interface`
- `litespec/frontend/blazor/data#classify-state`
- `litespec/frontend/blazor/data#keep-derived-values-single-sourced`
- `litespec/frontend/blazor/data#keep-forms-typed`
- `litespec/frontend/blazor/data#load-persisted-state-once`
- `litespec/frontend/blazor/data#make-refusals-explicit`
- `litespec/frontend/blazor/data#prefer-explicit-refresh`
- `litespec/frontend/blazor/data#route-use-cases-through-application`
- `litespec/frontend/blazor/data#validate-at-the-boundary`
- `litespec/frontend/blazor/rendering#declare-routes-on-pages`
- `litespec/frontend/blazor/rendering#give-every-route-explicit-states`
- `litespec/frontend/blazor/rendering#keep-navigation-state-in-the-url`
- `litespec/frontend/blazor/rendering#keep-the-acquisition-surface-static`
- `litespec/frontend/blazor/rendering#name-routes-for-the-reader`
- `litespec/frontend/blazor/rendering#operate-offline-after-first-load`
- `litespec/frontend/blazor/rendering#render-on-the-client-only`
- `litespec/frontend/blazor/stack#keep-blazor-outside-the-ui-contract`
- `litespec/frontend/blazor/stack#publish-static-webassembly-output`
- `litespec/frontend/blazor/stack#record-a-first-load-budget`
- `litespec/frontend/blazor/stack#verify-with-the-net-toolchain`
- `litespec/frontend/blazor/structure#isolate-module-internals`
- `litespec/frontend/blazor/structure#keep-domain-free-of-browser-concerns`
- `litespec/frontend/blazor/structure#keep-imports-directional`
- `litespec/frontend/blazor/structure#keep-static-content-out-of-the-assembly`
- `litespec/frontend/blazor/structure#organize-features-by-module-and-use-case`
- `litespec/frontend/blazor/structure#place-interop-adapters-together`
- `litespec/frontend/blazor/structure#use-the-client-solution-tree`
- `litespec/frontend/blazor/testing#enforce-the-first-load-budget`
- `litespec/frontend/blazor/testing#keep-browser-capability-tests-honest`
- `litespec/frontend/blazor/testing#keep-end-to-end-tests-few`
- `litespec/frontend/blazor/testing#prove-each-scenario-with-a-test`
- `litespec/frontend/blazor/testing#replace-interop-in-component-tests`
- `litespec/frontend/blazor/testing#test-the-core-without-a-browser`
- `litespec/frontend/blazor/testing#use-the-four-client-test-projects`
- `litespec/frontend/blazor/testing#verify-real-browser-behavior-end-to-end`
- `litespec/frontend/mobile/android/stack#build-with-net-for-android`
- `litespec/frontend/mobile/android/stack#keep-android-outside-the-ui-contract`
- `litespec/frontend/mobile/android/stack#pin-every-android-binding-package`
- `litespec/frontend/mobile/android/stack#record-a-package-size-budget`
- `litespec/frontend/mobile/android/stack#record-the-minimum-android-version`
- `litespec/frontend/mobile/android/stack#store-streamed-media-uncompressed`
- `litespec/frontend/mobile/android/stack#verify-with-the-net-toolchain`
- `litespec/frontend/mobile/android/views#apply-insets-from-windowinsets`
- `litespec/frontend/mobile/android/views#avoid-platform-names-that-collide-with-the-app-namespace`
- `litespec/frontend/mobile/android/views#build-views-in-code-or-layout-resources-not-both`
- `litespec/frontend/mobile/android/views#deliver-app-links-to-the-controller`
- `litespec/frontend/mobile/android/views#host-screens-in-one-activity`
- `litespec/frontend/mobile/android/views#keep-text-colors-and-icons-in-resources`
- `litespec/frontend/mobile/android/views#persist-navigation-state-in-the-activity-bundle`
- `litespec/frontend/mobile/android/views#use-the-android-back-dispatcher`
- `litespec/frontend/mobile/data#classify-state`
- `litespec/frontend/mobile/data#route-use-cases-through-application`
- `litespec/frontend/mobile/data#update-the-store-from-command-results`
- `litespec/frontend/mobile/structure#keep-imports-directional`
- `litespec/frontend/testing#query-by-accessible-behavior`
- `litespec/frontend/web/components#compose-class-names-in-the-shared-package`
- `litespec/frontend/web/components#give-card-and-section-titles-heading-semantics`
- `litespec/frontend/web/components#keep-primitives-and-composites-in-one-shared-package`
- `litespec/frontend/web/components#meet-accessibility-requirements`
- `litespec/frontend/web/components#place-a-drawing-no-composite-makes-in-a-visual-file`
- `litespec/frontend/web/components#protect-rich-content-boundaries`
- `litespec/frontend/web/components#use-no-raw-element-in-page-code`
- `litespec/frontend/web/data#assign-state-to-the-narrowest-owner`
- `litespec/frontend/web/data#build-a-form-on-the-shared-form-library`
- `litespec/frontend/web/data#generate-the-client-from-the-published-contract`
- `litespec/frontend/web/data#keep-the-client-boundary-in-the-frame`
- `litespec/frontend/web/data#localize-copy-through-locale-dictionaries`
- `litespec/frontend/web/data#map-every-failure-to-one-problem-type`
- `litespec/frontend/web/data#place-a-refusal-beside-what-was-refused`
- `litespec/frontend/web/data#regenerate-the-client-rather-than-editing-it`
- `litespec/frontend/web/data#update-the-cache-a-command-changed`
- `litespec/frontend/web/data#validate-a-form-with-the-generated-schema`
- `litespec/frontend/web/patterns#implement-a-chosen-pattern-once`
- `litespec/frontend/web/patterns#keep-a-visual-registry`
- `litespec/frontend/web/react/components#keep-component-boundaries-typed`
- `litespec/frontend/web/react/components#write-components-as-functions`
- `litespec/frontend/web/react/stack#build-a-device-client-as-one-static-page`
- `litespec/frontend/web/react/stack#select-a-rendering-mode-by-audience`
- `litespec/frontend/web/rendering#build-the-server-query-cache-per-request`
- `litespec/frontend/web/rendering#keep-authenticated-caching-explicit`
- `litespec/frontend/web/rendering#keep-proxy-behavior-at-the-edge`
- `litespec/frontend/web/rendering#keep-search-parameters-validated-by-a-schema`
- `litespec/frontend/web/rendering#load-a-route-on-intent`
- `litespec/frontend/web/rendering#load-route-data-in-the-loader`
- `litespec/frontend/web/rendering#read-route-data-with-a-suspense-query`
- `litespec/frontend/web/rendering#split-one-chunk-per-route-and-per-language`
- `litespec/frontend/web/rendering#use-a-pathless-layout-route-for-a-shell`
- `litespec/frontend/web/structure#keep-a-route-file-thin`
- `litespec/frontend/web/structure#keep-imports-directional`
- `litespec/frontend/web/structure#keep-tests-near-their-ownership-boundary`
- `litespec/frontend/web/structure#use-explicit-public-entry-points-for-workspace-packages`
- `litespec/frontend/web/svelte/components#keep-component-boundaries-typed`
- `litespec/frontend/web/svelte/stack#build-a-device-client-as-one-static-page`
- `litespec/frontend/web/svelte/stack#select-a-rendering-mode-by-audience`
- `litespec/frontend/web/testing#assert-each-declared-refusal-by-its-code`
- `litespec/frontend/web/testing#gate-changed-lines-with-mutation-testing`
- `litespec/frontend/web/testing#keep-mocks-at-owned-boundaries`
- `litespec/frontend/web/testing#match-test-level-to-risk`
- `litespec/frontend/web/testing#prove-the-module-mirror-in-a-structure-test`
- `litespec/frontend/web/testing#query-by-accessible-behavior`
- `litespec/frontend/web/testing#run-browser-checks-through-the-playwright-cli`
- `litespec/frontend/web/testing#seed-property-tests-from-example-tables`
- `litespec/frontend/web/testing#use-network-mocks-for-component-tests`
- `litespec/frontend/web/ui#check-every-route-in-a-browser`
- `litespec/frontend/web/ui#compose-each-route-from-one-floorplan`
- `litespec/frontend/web/ui#derive-each-page-type-from-a-named-reference`
- `litespec/frontend/web/ui#hold-the-declared-base-color`
- `litespec/frontend/web/ui#keep-layout-inside-floorplans`
- `litespec/frontend/web/ui#keep-tokens-in-one-stylesheet`
- `litespec/frontend/web/ui#pin-the-baseline-in-the-shared-package`
- `litespec/frontend/web/ui#prove-every-result-a-route-calls`
- `litespec/frontend/web/ui#prove-ui-behavior-and-appearance`
- `litespec/frontend/web/ui#refuse-token-violations-with-stylelint`
- `litespec/frontend/web/ui#select-a-tone-from-the-closed-set`
- `litespec/quality/ci#apply-the-documented-defaults`
- `litespec/quality/ci#approve-breaking-changes-by-the-highest-record-owner`
- `litespec/quality/ci#enforce-the-supply-chain-gate`
- `litespec/quality/ci#keep-a-canonical-job-graph`
- `litespec/quality/ci#map-record-areas-to-owners`
- `litespec/quality/ci#promote-verified-artifacts`
- `litespec/quality/ci#protect-the-default-branch`
- `litespec/quality/ci#publish-a-book-preview-for-every-change`
- `litespec/quality/ci#publish-the-intent-diff-on-every-pull-request`
- `litespec/quality/ci#record-every-branch-protection-bypass`
- `litespec/quality/ci#require-zero-surviving-mutants-on-changed-lines`
- `litespec/quality/ci#review-schema-artifacts`
- `litespec/quality/ci#show-intent-counts-in-the-job-summary`
- `litespec/quality/operations#attach-dashboard-links-to-production-objectives`
- `litespec/quality/operations#cover-the-baseline-alert-set`
- `litespec/quality/operations#define-actionable-baseline-alerts`
- `litespec/quality/operations#define-backup-and-restore-behavior`
- `litespec/quality/operations#exercise-a-restore-before-the-first-production-data`
- `litespec/quality/operations#exercise-every-runbook-before-release`
- `litespec/quality/operations#keep-deployment-descriptors-together`
- `litespec/quality/operations#keep-each-alert-threshold-to-its-requirements-measure`
- `litespec/quality/operations#keep-rollback-executable`
- `litespec/quality/operations#record-each-drill-run-as-a-check-result`
- `litespec/quality/operations#report-liveness-from-every-recurring-job`
- `litespec/quality/operations#route-each-severity-to-a-declared-destination`
- `litespec/quality/operations#state-availability-objectives-as-openslo-files`
- `litespec/quality/operations#use-a-repeatable-deployment`
- `litespec/quality/operations#use-stable-service-names`
- `litespec/quality/security#route-tier-1-access-and-data-changes-through-the-security-reviewer`
- `litespec/workspace/dependencies#accept-every-shipped-package-license-explicitly`
- `litespec/workspace/dependencies#match-every-shipped-package-to-its-pin`
- `litespec/workspace/structure#keep-scripts-at-the-root`

Renamed (2):

- `litespec/core/model#connect-every-use-case-to-intent` to `litespec/core/model#connect-every-behavior-record-to-intent`
- `litespec/core/system#use-this-consumer-documentation-layout` to `litespec/core/system#use-the-docs-layout-on-the-model-page`

Extensions added (2):

- `delivery`
- `layer-projects`

Extensions removed (8):

- `containers`
- `pipeline`
- `privacy`
- `queues`
- `realtime`
- `report`
- `site`
- `workflow-engine`

<!-- /generated:provision-diff -->

## v3.0.0

LiteSpec 3.0.0 is a top-down specification standard for one bounded-context business application, written by agents and judged by machines, so people review only what is theirs to decide. A consumer installs this release as the `LiteSpec.Cli` local .NET tool at the release version and records it in `reviewedLiteSpecVersion`. The release takes effect as the `v3.0.0` tag.

### Layers

- Four layers: Intent (why and what we promise; product people review it), Behavior (how the system behaves; developers review it), Implementation (code) and Proof (tests, checks, computed statuses and the book).
- Each layer is written from the one above. Nothing above Implementation is computed from code; code and tests point up. Facts from the world enter Intent as research or constraints, and what a lower layer needs travels up as a question with a decider.
- The translation rules Specify, Implement and Verify state what each boundary receives, must add, may not decide, and checks.
- Consumer docs sit in one folder per layer: `docs/intent`, `docs/behavior`, `docs/implementation`, `docs/proof` and `docs/governance`.

### Records

- Each record page is a YAML record file holding one mapping with its entries in `records`; an optional explanation file beside it holds Markdown under H2 sections.
- A module page is `module.yaml` in its module folder and an aggregate page is `aggregate.yaml` in its aggregate folder; the glossary is `docs/glossary.yaml`.
- Anything a check reads or a reviewer approves is a record field; the explanation file only helps a reader understand. Product, constraint, rule set, actor, role and external system hold `summary`; goal holds `whyItMatters`; problem holds `situation`; feature holds `offering`; decision holds `consequences`; topic holds `summary`, `position` and `refusals`; journey holds `betweenSteps`.
- Use cases hold `purpose`, `whatHappens` and `exposure`; reactions, jobs, notifications and setting sets hold `purpose` and `whatHappens`; screens hold `summary`; integration events hold `summary` and `delivery`; runbooks hold `impact`, `prerequisites`, `procedure`, `verification`, `recovery` and `escalation`; exceptions hold `exit` and `consequences`; research sources carry a `key`.
- Prose cites a record by its identifier, such as `[the refund rule](rule/refund.within-window)`; research cites a source as `[text][key]`.
- A Markdown page that holds no record is a document with an optional frontmatter block holding only its page style and common fields.
- Journeys are product sentences with step ids, `finishedWhen` and a per-step `failure`; use cases point up with `serves`.
- Requirements state exceptions with `except` and a `fallback`, and carry `risk` and `setBy`.
- Use cases are commands or queries with `access` (`grant` and `tenant`; `auth` sits on the entry point), `changes`, `reads`, `consistency`, `effects` (`runs`, `sends`, `emits`, `calls`), `idempotency`, `audit`, typed query output in `include` groups, freshness and paging. Generic refusals are derived.
- Record types include reaction, job, notification, setting and integration event. Aggregates have `entity`, `saga` and `store` kinds, and data classes with erasure positions. A retention schedule page holds retention rows.
- Reactions and jobs declare edges in `effects` with the same verb grammar as use cases; a reaction may omit `timing`, which reads as `async`, and a top-level verb beside `effects` is refused.
- An idempotency `key` templates `{field}` placeholders over the record's `input` fields, and `scope` is `tenant`, `account`, `login` or `global`; reaction and job placeholders name triggering event or run window fields and are not checked.
- Reaction and job `onFailure` is a structured policy: `mode` is `retry`, `skip-and-log` or `discard`; a `retry` policy holds `maxAttempts`, `initialDelay`, `maxDelay` and `backoff`; `then` is `dead-letter`, with `deadLetter`, `surfacesIn` and `runbook`, with ISO 8601 durations.
- Approval is data: `approvedBy`, `approvedOn` and a semantic `approvedHash`, written by `litespec model approve`.

### Review and checks

- Product people read journey chapters and ballots; developers review Behavior diffs; a named security reviewer sees tier 1 access and data changes. Tier 3 questions auto-approve after a veto window.
- Every check has a level: G (record graph, blocking), S (stack reader, blocking where qualified) and A (advisory).
- Every edge between use cases, reactions, jobs, notifications, integration events and external systems is declared and checked. A calls edge counts only through a port anchored to an external-system record.
- A refusal proof confirms from the asserted code, a client code `refusals.aliases` translates, or the HTTP status `refusals.statuses` maps to the code.
- An integration fact that drives a use case or rule confirms its scenario or rule claim from inside the host.
- An `effect-undeclared` finding names a one-hop helper as `via 'Type.Method'`.
- A term's `labels` sit outside the approval hash, so adding a language reopens no approval.
- A project declares its use-case verb vocabulary in `vocabulary`: `verbs`, `phrasal` verbs counted as one, and `retired` synonyms with their replacement, checked by `use-case-verb-unknown`.
- `effect-timing-invalid` (level S) reports a command whose `sends` or `calls` effect runs inline; consequences run from an event handler or a job after the commit.
- `event-tense-invalid` accepts a past tense followed by a prepositional complement and a mark method naming its state, and `term-clash` skips a hyphenated pair opening with a function word.
- `state-unreached` (level G) reports every state no transition enters. An aggregate writes its creation as a transition from `-` in its `transitions` field, so its first state is reached too.
- The approval hash covers every written record field except the identity, workflow and approval fields the guide lists, and compares numbers by value, so `50.00` and `50` hash alike. For research, decision and topic pages it also covers the explanation file.
- `prose-missing` reports a significant use case with no `purpose` field and a significant command with no `whatHappens` field.
- `explanation-orphaned`, `explanation-missing`, `format-drift`, `source-link-bare`, `source-key-unknown` and `source-unused` (level A) report an orphaned explanation, a missing article explanation, a record outside canonical form, and a research source linked directly, cited by an unknown key, or never cited.
- `record-invalid` reports a record file that does not parse, with the parser message and line.
- `reader-missing` (level S) also reports a .NET file that cannot be read or parsed, a declared stack root that resolves to no folder, a `server` project with no backend reader, and a configured bill of materials that cannot be read, is not SPDX or CycloneDX, or yields no packages.
- A test proves a scenario across machines: the proof ledger hashes each test file by its path relative to the project root and its text with normalized line endings.

### Application Layer

- A command handler loads or creates one aggregate, calls one domain method, stages it and returns; validation, authorization, idempotency, audit, the timestamp, holds, the commit and fault classification are pipeline stages, and a change to another aggregate is a consequence. A record declaring `consistency: atomic` is the one exception.
- Each command implements one use case with one success path; a find-or-create flow is two use cases the owning boundary chooses between, and a lost race ends with the use case's conflict refusal at commit.
- Uniqueness is enforced by the store: a unique index, an identity derived from the owner, aggregate state for a once-only rule, or a key the aggregate declares and persistence claims at commit. Each constraint maps by name to its conflict refusal, and no handler reserves or checks a key.
- A refusal code built at run time from a key, such as the conflict for a lost uniqueness key, declares its template in the project file `refusals.composed`, and `refusal-unimplemented` reads the code as raised where its key is a literal in the code.
- Every aggregate carries a version the store compares at commit.
- A message whose target must be the caller carries no target or declares a self grant the guard enforces.
- Audit targets and reasons the message carries are declared beside it; the handler adds only values it learns while running.
- Messages, provider calls and changes to other aggregates run in event handlers after the commit; the outbox delivers each event to each handler separately.
- Query handlers take no repository and never write. Work over many records is a job issuing one command per record, each in its own unit of work.
- Result parts are named `{UseCase}QueryResult{Part}` or `{UseCase}CommandResult{Part}`, event handlers are `{Action}On{Fact}Handler` in an `On{Fact}` folder of the subscriber's aggregate, and repository variables are named after their interface.

### Mobile

- An `android` stack builds a native Android app with .NET for Android and plain Android views: no MAUI, no WebView screens. It sits at `docs/frontend/mobile/android/` under shared mobile pages for structure, screens and navigation, data and state, device capabilities, and testing, so a later mobile stack reuses them.
- The mobile solution lives at `apps/mobile/` with platform-free core projects beside one app project per platform; a `client-only` app holds Domain, Application and Infrastructure, a `server` app holds the generated API client.
- The `android-client` preset starts a client-only Android consumer, and the manifest pins the Android API level in `stack.android`.

### Tools

- `litespec model diff` has `--intent` and `--behavior` views with a security queue, and `litespec book` renders journey chapters and a ballots page.
- `litespec new` scaffolds a record file with its explanation file for an article type, `litespec rename` rewrites an id in record files, explanation files and documents, and `litespec model approve` records approval in the record file.
- `litespec new result` and `litespec new scenario` append one entry to a use-case record file in house order: successes, refusals, then scenarios.
- `litespec model format` rewrites every record file and the glossary file into canonical form; `--check` writes nothing, lists each file that would change, and exits 1 when any would. It exits 2 when a file does not parse.
- `litespec append` adds an entry to a record file; `litespec init` writes `docs/glossary.yaml`.
- Consumer templates ship as `<type>.yaml` record files, with `<type>.md` explanation files for article types and `<type>.md` documents for document styles.
- `litespec model check --staged` reports only staged records, their direct links, and the records staged code implements.
- `litespec model approve --dry-run` prints which records would be stamped and which would be reopened, without writing.
- The `litespec-maintain` tool checks the standard's own pages, schemas, manifest, and provision index from `apps/maintain/` with `validate` and `provisions`; the `litespec` tool serves consumers only.
- Extensions added: privacy, devices, automation, workflow-engine, pipeline, queues and ai. LS2002 is retired.
- `litespec model check` prints each finding once, the blocking ones first, then a count per finding code and level, and ends with the number of findings, blocking findings and known findings. `--level` limits the findings and counts to the named levels, such as `--level G,S`.
- `litespec model check --since` and `--staged` leave out files under `.litespec/model/`, which the tool writes itself.
- `litespec model approve --all --dry-run` lists exactly the records `litespec model approve --all` stamps.
- Every command reports an unexpected failure as one line and exit code 1; with `--format json` the line is a JSON object with an `error` field. A repeated option, an empty value, and an unknown option given with `--help` are usage errors.
- The book links only `http`, `https`, `mailto`, in-page fragments, relative paths, record identifiers and the configured editor scheme, and renders any other link target as text. `litespec book` exits with code 2 when the carried release is missing.
- A project that opts out of the analyzers with `LiteSpecLayer=None` gets no diagnostics and no generated code from the analyzer package.
- The navigation and page-frame patterns draw their wireframes on the Claimwick cast. Their option ids follow the option names: `cost-centre-sections-as-tabs-on-the-cost-centre-page`, `no-switcher-back-to-the-cost-centre-list`, `the-count-the-cost-centres-the-exceptions`, `per-cost-centre-cards-with-a-rate`, `payouts-as-a-timeline`, `split-payouts-left-incidents-right`, `month-grid-with-trips-marked`, `week-lanes-per-cost-centre` and `receipt-scanner-is-offline`.
- Provisions cover file uploads and the `429` status on the API, and offline commands, saved navigation state, app links and touch and text accessibility on Android. The shared frontend data rules live on the frontend data page for every stack, and the `jobs` extension runs scheduled jobs in a Worker. FsCheck is pinned for property-based Domain tests.
- Record files use a strict YAML subset in canonical key order with two-space indentation; `litespec model format` owns the layout.
- The YAML block at the top of a document is called frontmatter throughout the standard.

### Record Hierarchy

- Every record has exactly one parent: by identifier containment, by one required written up-link, or, for a type with no record above it, its layer list under the product. A record whose required parent is missing is an error at level G, reported even when the record is a draft. Every inverse link is computed, never written.
- A feature names at least one problem in `solves`; its parent is the first entry. Every non-refused feature names at least one goal in `advances`. A refused feature names `refusedBecause` and the problem it declines in `solves`, and carries no `advances`.
- Every journey names its goal record in a required single link `serves`, which is its parent. A use case, reaction, job or screen serves journey steps; a journey serves one goal, and a use case never serves a goal directly.
- Every requirement names its parent: a carrying feature, a constraint or research in `basedOn`, or a decision naming it in `decides`, in that precedence.
- Written result and scenario identifiers use the `result/<use-case>.<name>` and `scenario/<use-case>.<name>` form, and derived generic results take the same form. A written result no scenario shows is reported: level G for a success result, level A for a refusal, on large projects.
- Structural parents are level G for all records; other required links are level A for tier 3 records and level G for tier 1 records.
- Field names follow one grammar: a classifier is `kind`; a link is a present-tense verb, plural when it holds a list; a deadline is `<verb>By`, a recorded date `<verb>On`; a reason is `reason` or `<verb>Because`. Feature `lifecycle`, goal and requirement `dueBy`, feature `plannedFor`, decision `approvedOn`, and constraint `authority` and `kind` follow it, checked by `litespec-maintain validate`. Glossary terms carry `layer` and `aliases`.
- Use cases list the settings they read in `reads`; a setting's readers are computed, never written. The Links table carries a "reads as" phrase in both directions for every link, and the book takes its connection labels from it. A follows edge marks export beside implements in the built model.
- Research states its `conclusion`, and LiteSpec keeps its own research and decision records under `docs/intent/`.
- `litespec book` is one offline shell with sharded data, generated from the model, with the outline from the parent table and the labels from the Links table and the glossary.
- Approval batches a journey with the goal it serves, keeps each entry a page stamp did not hold at the page's former status, refuses a decision naming no chosen option, and keeps a transcribed record serving a draft journey in draft. Screens are Behavior, and reaction and job scenarios carry their own stamp.
