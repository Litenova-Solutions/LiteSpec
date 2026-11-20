# Use Cases

**Stack:** any
**Concern:** records

Use Cases states the system's commands and queries: their verbs, inputs, effects, endings, scenarios, and the settings they read. Agents write these records from Intent, and code carries them out. The [Behavior overview](behavior.md) maps the five Behavior pages, and [Record Language](language.md) states how each field, value, and record name is written.

## Concepts

### Use Cases

A use case is one goal that a single test proves through the public boundary. It is a page beside its aggregate, with one kind: command or query. A command changes one aggregate type and one instance: its record's `writes` names one aggregate, and its handler loads, changes, and stages one root. A command stating `consistency: per-record` changes independent instances of one type, and a command changing no aggregate declares `writes: []`. Another aggregate of the same module follows through a reaction to the domain event, with its own commit. A query reads without changing it.

Each use case links back to the Intent it carries, as `litespec/core/model#connect-every-behavior-record-to-intent` states: the flow steps it serves in `serves`, and the requirements it carries in `fulfills`. It states who may act in `access`, and the entry points and modules reaching it in `reachedThrough`. It names the aggregates and read models it touches in `writes` and `reads`, and the events it raises in `raises`. Its results state how it ends. A public query lists its consuming modules in `reachedThrough`, and each consumer declares the edge in `effects.queries`. A public command lists its sending modules in `reachedThrough`, and each sending reaction or job declares the edge in `effects.commands`.

| Field | Meaning |
|:---|:---|
| `access` | `{ grant, tenant }`. The grant is `permission/<name>`, `self(permission/<name>)`, `signed-in`, `anonymous`, or `system`. The tenant scope is `own`, `cross`, `public`, or `none`. Each use case declares its grant and tenant scope in `access`, as `litespec/core/access#declare-access-on-every-use-case` states. The entry point the call arrives through declares `authentication`, as [Access](access.md) states. |
| `input` | Entries of `{ name, meaning, type, identifies, rowShape, choices, many, required, rules, origin, verifiedBy, classification, contentTypes, maxSize }`. The `type` is `text`, `integer`, `decimal`, `money`, `boolean`, `date`, `date-time`, `duration`, `email`, `choice`, `file`, `list`, or `id`. The `identifies` field names the record an `id` refers to, and `rowShape` names the row shape of a `list`. The `choices` field names the allowed values of a `choice`. A `file` field carries `contentTypes` with its media types and `maxSize` with its limit. A `list` field carries in `rowShape` the row shape with each row field's name, type, required flag, and meaning, with `minItems` and the `unique` key fields. The `many` flag is true when the field holds a list of values of its type. The `required` flag states whether the caller sends the field. The `rules` field names the `rule/` identifiers bounding the value. The origin is `caller`, `claim`, `verified-claim`, or `derived`. A `verified-claim` input names the entry point or external system that validated it in `verifiedBy`. |
| `writes`, `reads` | The id of the one aggregate written, and the ids of aggregates, read models, and settings read. A read model is the module's own copy of another module's state, as [Automation](automation.md) states. A command whose `writes` names more than one aggregate is allowed only when its consistency `boundBy` rule states the invariant needing both in one transaction and names its decision in `basedOn`. A command changing no aggregate declares `writes: []`. |
| `consistency` | `{ mode, boundBy, guards }`, on a command writing two or more aggregates or reading an aggregate whose rule guards its write. The mode is `atomic` or `per-record`. An atomic write across aggregates cites its `atomicity` rule in `boundBy`, and the rule names in `basedOn` the decision recording why the invariant needs one transaction. The `guards` field lists the aggregates the command reads, and does not write, whose rule named in `boundBy` must still hold when the command commits. Mode `atomic` with `guards` means the guard holds at commit. A query carries no `consistency`. |
| `effects` | Entries of `{ sends / calls / queries / commands, timing, onFailure, onExhausted, before, forEach }` with exactly one verb. The timing is `inline`, `after-commit`, or `async`, and only a query's `calls` may be `inline`. A `queries` entry names the public query use case of another module the behavior reads and carries `inline` timing: the read runs within the handling. A `commands` entry names the public command use case of another module the behavior sends and carries `inline` timing: the send runs within the handling. A use case runs no other use case, declares no `commands` edge, and declares no integration event. A reaction runs the follow-on work, and an outbound integration event names the event it translates. The effect graph holds no cycle except through a domain event with a stated end condition, as `litespec/core/principles#end-every-effect-cycle-in-a-domain-event` states. |
| `idempotency` | `{ mode, key, keyScope, reason, replay, recognizedBy }`, on a command only. The mode is `key`, `state`, or `none`. The `key` templates `{field}` placeholders over the input field names. The `keyScope` is `tenant`, `account`, `login`, or `global`. The `replay` is `returns-first-result`, `refuses`, or `acts-again`. Mode `state` also carries `recognizedBy` naming the state checked. A query carries no `idempotency`. |
| `audit` | `{ level, targetKind }` with level `required`, `optional`, or `none`, or `{ exemptBecause }` with its justification. The `audit` extension owns this field: a command declares it only when the project selects that extension. A level of `none` is not audited. Async work audits at request time. |
| `background` | The optional Given shared by every scenario of the use case. A use case carrying `background` holds at least two scenarios. |
| `steps` | The command steps in order, each `statement` naming who acts and `cites` naming the rules, results, settings, events, read models, external systems, and aggregates the step applies. |
| `edgeCases` | Cases the command does not follow the steps for, one sentence per case. |

Each consistency mode defines its promise for the writes of one use case:

| Mode | Means |
|:---|:---|
| `atomic` | Every write in `writes` commits as one group |
| `per-record` | Each write commits on its own; a failure leaves earlier writes in place |

No mode reaches past the use case. Every follow-on state change runs after the commit, from a reaction or a job; reads and inline `calls` run inside the use case. A command writing one aggregate and reading no guarding aggregate is atomic by definition, so it carries no `consistency`. A command writing more than one aggregate is usually a sign the aggregate boundary is drawn wrong; it stays allowed only with its `atomicity` rule in `boundBy` naming the decision behind it in `basedOn`.

Each effect entry carries one `onFailure` shape: `{ mode, retry, ifExhausted, deadLetter, surfacesIn, runbook }`. The `mode` is `retry`, `skip-and-log`, or `discard`. The `retry` and `ifExhausted` fields accompany `mode: retry`, while `deadLetter`, `surfacesIn`, and `runbook` are optional. The `onExhausted` field names the act when retries run out. The `before` field orders the entry ahead of another named target with its reason, and `forEach` names the items the entry repeats over.

The `summary` field states what the use case is, in one paragraph. Each use case carries `summary`. A command lists its steps in `steps` in order, and each step cites the rules, results, settings, events, read models, external systems, and aggregates the step applies. The explanation file restates the steps under Flow. Scenarios still prove the behavior, as [Summarize Every Use Case, Actor, Role, and External System](#summarize-every-use-case-actor-role-and-external-system) states.

The `exposure` field states in plain words who reaches the use case, through which entry points, under which permission, and what is at stake if it goes wrong. Example and Notes live in the explanation file beside the record file.

### Queries

A query reads without changing, and declares what its readers receive. Its output groups fields into named `include` groups. Each group names its fields, its classification, the aggregates it reads, and its freshness.

Freshness is `{ mode, within, decidedBy }`. The `mode` is `immediate`, `snapshot`, or `eventual`. The `within` field is the ISO 8601 duration a lagging read stays behind its source, present when the mode allows lag. The `decidedBy` field names the command use case or aggregate that decides the figure at write time. It appears only with `snapshot` or `eventual` freshness, when a command re-decides the figure.

A query whose aggregates lag reads from a read model: data shaped for the query, served without loading an aggregate.

A command may declare its output as a list of `{ name, meaning, type }`.

| Field | Meaning |
|:---|:---|
| `output.include` | Named groups of `{ fields, classification, reads, freshness, visibleWhen, widenedBy }`. Each field carries `name`, `meaning`, and `type`. |
| `visibleWhen`, `widenedBy` | The optional visibility condition, and the requirement widening visibility beyond the aggregates in `reads`. |
| `paging` | `{ "mode": "pages", order, cursor | offset, defaultLimit, maxLimit }`, or `{ "mode": "none", "reason": "..." }`. |

### Public Queries and Commands

A public query is a query use case another module reads. Its record lists the consuming modules in `reachedThrough` beside or instead of entry points, and each consuming use case, reaction, or job declares the edge in `effects.queries` naming the public query use case.

A public command is a command use case another module sends. Its record lists the sending modules in `reachedThrough` beside or instead of entry points, exactly like a public query, and each sending reaction or job declares the edge in `effects.commands` naming the public command use case. A use case declares no `commands` edge: only a reaction or job sends another module's public command.

The provider answers a public query with its current value and nothing else, and runs a public command through its ordinary command handler with its own pipeline. Its message and result sit in Contracts at the same `{Aggregates}/{UseCase}/` path the use case has in the provider's `Application/` folder. A `queries` edge to a use case whose `reachedThrough` does not name the calling module is undeclared on the provider side, and so is a `commands` edge to a use case whose `reachedThrough` does not name the sending module. A public query no module reads, or a public command no module sends, is published for no caller.

In Claimwick, spending's read-expense query lists the reporting module in `reachedThrough`, and reporting's reimbursement command declares `effects.queries` naming `use-case/spending.read-expense`. Spending's close-expenses command lists the reporting module in `reachedThrough`, and reporting's close-period job declares `effects.commands` naming `use-case/spending.close-expenses`.

### Use-Case Verbs

A use-case name starts with a verb. Each verb keeps one meaning and one kind across the project. LiteSpec ships the baseline vocabulary below. A project declares each verb beyond the baseline as a verb term in its glossary, as [Start Every Use Case with a Declared Verb](#start-every-use-case-with-a-declared-verb) states.

| Verb | Kind | Means |
|:---|:---|:---|
| `read` | query | One record, or one composed view, by its identity. |
| `list` | query | A page of records matching a filter. |
| `search` | query | A page of records matching free text. |
| `export` | query | A generated file of many records. |
| `download` | query | The stored or rendered document of one record. |
| `answer`, `draft`, `summarize`, `translate` | either | Text a model writes for a person to check. |
| `create` | command | A new aggregate root. |
| `add`, `remove` | command | A part inside an existing record. |
| `change` | command | Data of an existing record. |
| `duplicate` | command | A new record copied from an existing one. |
| `open`, `close` | command | A period, case or shift that runs between the two. |
| `start`, `complete` | command | Work that runs to an end. |
| `publish`, `withdraw` | command | What readers or the public can see. |
| `pause`, `resume` | command | A temporary stop a person chooses. |
| `suspend`, `reinstate` | command | A stop an operator imposes. |
| `enable`, `disable` | command | A switch. |
| `grant`, `revoke` | command | Access. |
| `request`, `approve`, `refuse` | command | A request someone else decides. |
| `issue`, `void` | command | A document or credential. |
| `retire`, `restore` | command | The end of a record's life, and its return inside the restore window. |
| `cancel` | command | An intention or booking that will not happen. |
| `expire` | command | What time ended; started by a job. |
| `record` | command | Something that happened outside the system: a provider answer, a delivery result, a scan. |
| `send` | command | A message to a person. |
| `post`, `reverse` | command | A ledger transaction and its correction. |
| `retry`, `verify` | command | Work run again, and a check run again. |

The baseline vocabulary retires these synonyms: `get`, `fetch`, `lookup`, and `count` become `read`; `browse` becomes `list`; `find` becomes `search`; `update`, `edit`, `modify`, `revise`, `set`, `adjust`, `configure`, and `rename` become `change`; `define`, `compose`, `save`, and `new` become `create`; `delete` becomes `remove`; `notify`, `remind`, and `email` become `send`; `suggest` and `ask` become `answer`; `mark`, `note`, and `log` become `record`; `end` becomes `close`; `finish` becomes `complete`; `abandon` becomes `cancel`; `lapse` becomes `expire`; `clone` and `copy` become `duplicate`; `replay` becomes `retry`; `recheck` becomes `verify`; `archive` becomes `retire`; `reject` and `deny` become `refuse`; `unpublish` and `hide` become `withdraw`. A project may declare a retired synonym as its own glossary verb term when the term's definition states how it differs from the baseline verb it was retired into.

A verb term names the verb in `term`, the kind it opens in `verbKind`, and the act in `meaning`, in one sentence, as any term does. A verb term retaking a retired synonym states in `meaning` how it differs from the baseline verb it was retired into, naming that verb.

```yaml
- id: term/borrow
  term: borrow
  meaning: The loan of one copy to one reader.
  layer: behavior
  verbKind: command
```

A reaction takes its name from its trigger, and a job from the use case it runs, as [Automation](automation.md) states.

### Results and Refusals

Each use case ends in named results. A result is a child record with an `outcome` of `success` or `refusal`. A success carries `meaning`. A refusal names its refusal in `refusal` by the refusal's id and keeps the `sentence` a person reads, with one sentence per interface language beside it in `sentences`.

Each aggregate declares the refusals its rules raise once in its own `refusals.yaml`, as [Declare Each Aggregate's Refusals Once](#declare-each-aggregates-refusals-once) states, and the module's `refusals.yaml` keeps only the refusals no aggregate raises, as [Declare Each Module's Refusals Once](#declare-each-modules-refusals-once) states. Each refusal entry carries its `id` and `title` like every other entry; the code is the id's name, so `refusal/spending.receipt-missing` carries the code `spending.receipt-missing`. The set owns each refusal and its meaning; the book joins the meaning onto each result citing the refusal. A result's `refusal` names a refusal from its aggregate's set, or from the module's set for a refusal no aggregate raises. A rule's `refusal` names the same refusal, and every result carrying it is the rule's refusal in that use case. The problem type of a code is the project's `problemTypeBase` followed by the code's part after its module, or that part alone as a short name where the project declares no base.

Refusals stand in precedence order with the derived refusals first, as [Order Every Refusal List by Precedence](#order-every-refusal-list-by-precedence) states. A code in use is retired with its replacement named and never reused, as [Retire Refusal Codes with Their Replacements](#retire-refusal-codes-with-their-replacements) states.

A refusal is a rejection or error in industry words; the record word stays refusal. A refusal is a declared business outcome where the use case did not perform its act because a rule, access, or input said no. A technical failure (a timeout, a provider error, a crash) is not a result: retry, the outbox, and operations handle it, and no use case declares it.

| Field | Meaning |
|:---|:---|
| `refusal` | The refusal id from the aggregate's or module's refusal set, on a refusal, such as `refusal/spending.receipt-missing`. The code is the id's name. |
| `meaning` | What the result means, on a success. A refusal carries no `meaning`: its refusal id names the set entry holding it. |
| `sentence`, `sentences` | The sentence a person reads, in the product's language, with one sentence per interface language beside it. |
| `recorded` | Whether the attempt and its reason are stored. A validating refusal stores nothing. |
| `codes` | The closed list of recorded outcomes inside a success result. |

The generic refusals are derived and never written as records. The `forbidden` refusal comes from the grant in `access`, `invalid-input` from the rules in `input`, `not-found` from the named target, `conflict` from the state the call met, `unauthenticated` from the missing caller, and `sign-in-too-old` from the recent-sign-in window [Access](access.md) states. A derived generic result takes the form `result/<use-case>.<name>`. Scenarios may point at derived results.

### Scenarios

A scenario proves one ending of the behavior it exercises. It is an entry in the file of its use case, and that use case is its parent. A reaction or job scenario is an entry in the file of its reaction or job, as [Automation](automation.md) states.

The scenario carries `given`, `when`, `then`, an optional `examples` table, and the `result` it ends in. A scenario proving a guard against a competing command also carries `competesWith`. One test proves each scenario through the public boundary.

A proof goes stale when either of its proof hashes changes. The book groups a use case's scenarios under their result.

| Field | Meaning |
|:---|:---|
| `given`, `when`, `then` | The situation, the action, and the assertion, all on the scenario's own operation. |
| `examples` | The worked example rows, all with one shared set of keys. |
| `result` | The written or derived result the scenario ends in. |
| `exercises` | The optional rules the scenario shows. |
| `competesWith` | The `use-case/...` identifier of the command running at the same moment, on a scenario proving a guarded command against a competing command. The scenario's `then` states the one refusal or the serialized outcome. |

### Settings

A setting is one operator configuration value with its type, range, unit, setter, and bound behavior: a provider timeout, a batch size, or a delay the operator tunes, read by the running system.

A business value a person changes through their work is aggregate data, not a setting: a threshold a manager sets on an expense, or a fee. Its range is a rule with a refusal, and its change is a use case with access and audit. A value a tenant administrator changes for the whole tenant is a setting with `setBy: tenant`, changed by a `change-*` use case, as [Give Every Tenant Setting a Change Use Case](#give-every-tenant-setting-a-change-use-case) states; it carries no configuration path. A value the product fixes or the operator tunes is a setting with `setBy: product` or `setBy: operator`. A model invariant is a rule instead. A setting several modules read is owned by one module, and the other modules read it across their declared dependency.

The `setting-set` page style holds many settings. A setting set carries `summary`, and its explanation file holds Notes.

A use case, reaction, or job names the settings it reads in `reads`.

| Field | Meaning |
|:---|:---|
| `valueType` | One of `integer`, `duration`, `money`, `boolean`, `text`, or `choice`. A money setting holds an amount with an ISO 4217 currency, as the [Record Language](language.md) states. |
| `default`, `range`, `unit` | The default in the setting's `valueType`, the min-max or choices, and the unit. A money default holds an amount with its currency, a duration default holds an ISO 8601 duration, and a choice default names one of its `choices`. |
| `dated` | `true` when each value applies from a date and never changes a posted record. |
| `setBy` | `product`, `operator`, or `tenant`. |
| `configurationPath` | The configuration path an operator setting binds from, such as `Spending:ApprovalReminderDelay`. Operator settings name it, as [Name the Configuration Key of Every Operator Setting](#name-the-configuration-key-of-every-operator-setting) states. |
| `outOfRange` | `refuse`, `truncate`, or `degrade`. |
| `secret` | Whether the value is secret. |
| `fulfills` | The requirements the setting carries. |

## Standards

### Give Each Use Case a Command or Query Kind

**ID:** `litespec/core/use-cases#give-each-use-case-a-command-or-query-kind`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use case MUST declare one kind: command or query.

**Rationale:** A use case with no kind reads as able to do anything, and the checks have no shape to confirm. The kind decides what the use case may touch: a command changes, a query reads. Work that runs when an event arrives, and work without a request, are reaction and job records, as [Automation](automation.md) states.

**Checked by:** `litespec model check` reports `record-invalid` for a use-case page without `kind` or with an unknown kind.

### Start Every Use Case with a Declared Verb

**ID:** `litespec/core/use-cases#start-every-use-case-with-a-declared-verb`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use case MUST start with a verb from the baseline vocabulary or a verb term the project declares in its glossary.

**Rationale:** Without one verb per meaning, `read` versus `get` and `change` versus `update` compete in every listing, and related behavior scatters. Starting from one closed baseline vocabulary and adding only declared verbs keeps use cases findable. A shared first word groups related behavior, and a retired synonym points at its replacement instead of spreading without notice. When the act verb is in neither list, the author first declares it as a glossary verb term in the same change, then scaffolds the use case. The `litespec new` command scaffolds a use-case page without checking its verb, so declaring the verb term first avoids scaffolding a use case that the verb check then reports. A project never redefines a baseline verb. A project may still declare a retired synonym as its own glossary verb term when the term's definition states how it differs from the baseline verb it was retired into, naming that verb. This adds a verb, it does not redefine one. Proposed by [decision/0062-declare-project-verbs-in-the-glossary](decision/0062-declare-project-verbs-in-the-glossary).

**Example:** The project declares `borrow` and `check-in` as glossary verb terms. The names `use-case/lending.borrow-copy` and `use-case/lending.check-in-copy` pass, `use-case/lending.list-loans` passes on the baseline vocabulary, and `use-case/lending.update-copy` is told to use `change`. The library declares `archive` as its own verb for a copy kept readable and restorable, with a definition stating how it differs from `retire`.

**Checked by:** `litespec model check` reports `use-case-verb-unknown` for a use-case name opening with a verb neither list declares, `term-verb-duplicate` for a glossary verb term restating a baseline verb, and `verb-distinction-missing` (level A) for a declared verb retaking a retired synonym whose definition does not name the baseline verb it differs from.

### Keep One Kind per Verb

**ID:** `litespec/core/use-cases#keep-one-kind-per-verb`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use case MUST open with a verb of its own kind, or with an either verb.

**Rationale:** A query named `change-...` or a command named `read-...` mislabels what the use case may touch in every listing. The first word is the first thing a reader trusts, so a mismatched first word teaches readers to trust no name. The baseline vocabulary gives each verb a kind of query, command, or either, and an either verb opens either kind. A project declares the kind of each of its verbs in its glossary verb term. Proposed by [decision/0062-declare-project-verbs-in-the-glossary](decision/0062-declare-project-verbs-in-the-glossary).

**Example:** `use-case/lending.list-loans` is a query and passes. The name `use-case/lending.answer-inquiry` opens either kind on an either verb and passes. The name `use-case/lending.read-overdue-count` declared as a command is reported, and so is `use-case/lending.change-copy` declared as a query.

**Checked by:** `litespec model check` reports `use-case-verb-invalid` for a use case opening with a verb of the other kind.

### Name What a Use Case Acts on from the Model

**ID:** `litespec/core/use-cases#name-what-a-use-case-acts-on-from-the-model`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use case MUST name, after its verb, an aggregate, a declared part, or a glossary term.

**Rationale:** A free synonym after the verb hides which business record the use case acts on, and creates a second word for the concept. The noun after the verb groups behavior by the record it acts on.

**Example:** `use-case/lending.borrow-copy` names the copy aggregate, and `use-case/sales.add-line` names the declared line part. The name `use-case/lending.borrow-volume` names no model word and is reported.

**Reviewed for:** Does every use-case name carry an aggregate, a declared part, or a glossary term after its verb? Look at the words after the verb in each use-case name.

### Name a Use Case for the Act Its Aggregate Performs

**ID:** `litespec/core/use-cases#name-a-use-case-for-the-act-its-aggregate-performs`

**Strength:** required

**Applies to:** artifact: record:use-case; when: command

**Statement:** Each command use case MUST share its verb with the aggregate method its handler calls.

**Rationale:** A different word in the use-case name and in the act means two meanings have merged, or the use case is misnamed. Each reader then guesses which word to trust. The use case names one aggregate act, so the two share their verb. A handler calling no aggregate method answers to no act, so queries and ports stay out of this rule. A command that records a request for work an event handler finishes after the commit, as `litespec/backend/application#call-external-systems-through-their-ports` requires, performs the request, so its method starts with `Request` and passes.

**Example:** `use-case/orders.void-order` whose handler calls `Abandon` is reported. The project renames the use case to the domain word, or the method and its event to `Void` and `OrderVoidedDomainEvent`.

**Reviewed for:** Does every aggregate method a command handler calls start with the use-case verb or `Request`? Look at the methods each command handler calls.

### Name the Entry Points and Modules That Reach Each Use Case

**ID:** `litespec/core/use-cases#name-the-entry-points-that-reach-each-use-case`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use case a caller reaches from outside MUST name the entry points that reach it in `reachedThrough`.

**Rationale:** A use case with no named entry point is reachable from anywhere or nowhere. Readers cannot tell a public operation from an internal one. Naming the entry points shows how callers arrive at each behavior. A public query names the consuming modules beside or instead of entry points, under `litespec/core/use-cases#list-the-consumer-modules-of-a-public-query`, and a public command names the sending modules, under `litespec/core/use-cases#list-the-sending-modules-of-a-public-command`, so the provider side of every synchronous cross-module edge is declared. A use case that only a reaction or a job runs has no entry point of its own, and the `runs` edge of that reaction or job already shows how it is reached. A use case a provider callback runs names the entry point the host serves: the provider redirects the browser to the host callback, and the pipeline handler in the module `Infrastructure/Pipeline/` folder runs the use case under its mark. That handler is the use case endpoint, so the use case needs no mapped endpoint route and no screen call.

**Example:** The library `complete-sign-in` use case names the library app entry point in `reachedThrough`. The provider redirects the browser to the host callback, where the ticket handler in the `Infrastructure/Pipeline/` folder runs the use case under its mark. No endpoint maps the callback route and no screen calls the use case.

**Checked by:** `litespec model check` reports `record-invalid` for a live use case that names no entry point and that nothing runs, and `reference-unknown` for a `reachedThrough` entry naming no entry point or module.

### List the Consumer Modules of a Public Query

**ID:** `litespec/core/use-cases#list-the-consumer-modules-of-a-public-query`

**Strength:** required

**Applies to:** artifact: record:use-case; when: query

**Statement:** Each public query MUST list every consuming module in `reachedThrough`.

**Rationale:** A `queries` edge names the public query use case it reads, and the public query names the modules allowed to read it. When the two lists disagree, the edge is declared on one side only: a `queries` edge to a use case whose `reachedThrough` does not name the calling module reads a query that names no such reader, and a public query no module reads is published for no reader. Settled by [decision/0109-module-contracts-publish-data](decision/0109-module-contracts-publish-data).

**Example:** Spending's read-expense query lists `module/reporting` in `reachedThrough`, and reporting's reimbursement command declares `effects.queries` naming `use-case/spending.read-expense`. A second consumer joins by adding its module to the list.

**Checked by:** `litespec model check` reports `reference-unknown` for a `queries` edge naming no written use case, `query-target-invalid` for a `queries` edge to a use case that is not a query or whose `reachedThrough` names no calling module, and `query-consumer-missing` for a public query no module queries.

### List the Sending Modules of a Public Command

**ID:** `litespec/core/use-cases#list-the-sending-modules-of-a-public-command`

**Strength:** required

**Applies to:** artifact: record:use-case; when: command

**Statement:** Each public command MUST list every sending module in `reachedThrough`.

**Rationale:** A `commands` edge names the public command use case it sends, and the public command names the modules allowed to send it. When the two lists disagree, the edge is declared on one side only: a `commands` edge to a use case whose `reachedThrough` does not name the sending module sends a command that names no such sender, and a public command no module sends is published for no caller. Settled by [decision/0109-module-contracts-publish-data](decision/0109-module-contracts-publish-data).

**Example:** Spending's close-expenses command lists `module/reporting` in `reachedThrough`, and reporting's close-period job declares `effects.commands` naming `use-case/spending.close-expenses`. A second sender joins by adding its module to the list.

**Checked by:** `litespec model check` reports `reference-unknown` for a `commands` edge naming no written use case, `command-target-invalid` for a `commands` edge to a use case that is not a command or whose `reachedThrough` names no sending module, and `command-consumer-missing` for a public command no module commands.

### State the Exposure of Every Use Case

**ID:** `litespec/core/use-cases#state-the-exposure-of-every-use-case`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use case reached through an entry point MUST state its exposure in plain words: who reaches it, through which entry points, under which permission, and what is at stake if it goes wrong.

**Rationale:** A use case with no exposure leaves each reader to judge the stake alone. An exposure that restates the fields tells a reader nothing the fields do not already say. Stating who reaches the use case, through which entry points, under which permission, and what is at stake keeps the human judgment beside the entry points and the grant it names. An entry point or permission the exposure names matches `reachedThrough` and `access`, so the words stay current with the declaration.

**Example:** The reimburse-expense exposure states that an approver reaches the command through the approval screen under the reimburse-expense permission. It adds that a wrong payout moves money to the wrong account.

**Checked by:** `litespec model check` reports `exposure-missing` for a use case with an entry point and no `exposure`, and `exposure-reference-drift` for an entry point or permission named in `exposure` that `reachedThrough` or `access` lacks.

### Describe Every Use Case Input with Origin and Classification

**ID:** `litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use-case input MUST declare its name, meaning, type, origin, and classification.

**Rationale:** An input without an origin is trusted on faith: caller-asserted values pass as verified claims, and money moves on an unchecked figure. Declaring the name, the meaning, the type, the origin, and the classification states what arrives, what it is, and how far to trust it.

The type comes from `text`, `integer`, `decimal`, `money`, `boolean`, `date`, `date-time`, `duration`, `email`, `choice`, `file`, `list`, and `id`. The origin is `caller`, `claim`, `verified-claim`, or `derived`. An `id` field names the record it refers to in `identifies`, a `choice` field names its allowed values in `choices`, a `file` field names its media types in `contentTypes` with its limit in `maxSize`, and a `list` field names its row shape in `rowShape`. Other types carry no `identifies`, no `rowShape`, and no `choices`. A `verified-claim` input names the entry point or external system that validated it in `verifiedBy`, as [Mark Every Provider-Verified Input](#mark-every-provider-verified-input) states. The `rules` field names the `rule/` identifiers bounding the value, and `required` states whether the caller sends the field. A money field carries an amount with an ISO 4217 currency, as [Record Language](language.md) states.

A type on each field lets the builder compare the records with the generated contract and lets agents generate the right code first time. Settled by [decision/0016-typed-contracts-and-privacy](decision/0016-typed-contracts-and-privacy) on the evidence in [research/lower-layers-practice](research/lower-layers-practice).

**Example:**

```json
"input": [
  { "name": "expenseId", "type": "id", "identifies": "aggregate/spending.expense", "origin": "caller" },
  { "name": "amount", "type": "money", "rules": ["rule/spending.positive-amount"], "origin": "caller" },
  { "name": "lines", "type": "list", "rowShape": { "fields": [{ "name": "amount", "type": "money", "required": true, "meaning": "The line amount." }], "minItems": 1 }, "origin": "caller" }
]
```

**Checked by:** `litespec model check` reports `record-invalid` for a use-case input without name, meaning, or type, without `identifies` on an `id` field, without `rowShape` on a `list` field, without `choices` on a `choice` field, or with `identifies`, `rowShape`, or `choices` on another type.

### Bound Every Caller-Supplied Input Value

**ID:** `litespec/core/use-cases#bound-every-caller-supplied-input-value`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each input value with `origin: caller` on a money or access path MUST cite the rule bounding it.

**Rationale:** A caller-asserted figure on a money or access path arrives unchecked: a replacement amount arrives negative, a quantity arrives absurd, and the code accepts them. Citing the rule bounding it ties each such value to the guard holding it.

**Example:** The replacement net amount cites the positivity rule bounding it.

**Reviewed for:** Does every caller-supplied money or access input cite its bounding rule? Look beside each input with `origin: caller` on a money or access path.

### Mark Every Provider-Verified Input

**ID:** `litespec/core/use-cases#mark-every-provider-verified-input`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each input carrying a claim a provider validated MUST declare `origin: verified-claim` with the entry point or external system that validated it in `verifiedBy`.

**Rationale:** A provider claim filed as caller input is trusted on faith: a forged subject or email passes as verified, and access follows. Declaring the claim with the entry point or external system that checked its issuer, audience, signature, and expiry ties each trusted value to the boundary that earned the trust. A caller never supplies such an input.

**Example:** The library sign-in command declares the provider subject with `origin: verified-claim`. Its `verifiedBy` names the provider sign-in entry point.

**Checked by:** `litespec model check` reports `input-claim-unverified` for a claim-shaped input with `origin: caller` on an entry point that authenticates through a provider.

### Name Every Aggregate a Use Case Writes or Reads

**ID:** `litespec/core/use-cases#name-every-aggregate-a-use-case-writes-or-reads`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use case MUST name every aggregate it writes in `writes` and every aggregate it reads in `reads`.

**Rationale:** A write no record names is invisible: the handler touches a second aggregate, the second aggregate changes, and no diff shows it. Naming every touched aggregate states the read and write footprint beside the behavior. The `write-undeclared` check reports a write outside `writes`.

**Checked by:** `litespec model check` reports `write-undeclared` for a write outside `writes` and `reference-unknown` for an unknown aggregate.

### Change One Aggregate in Each Command

**ID:** `litespec/core/use-cases#change-one-aggregate-in-each-command`

**Strength:** required

**Applies to:** artifact: record:use-case; when: command

**Statement:** Each command MUST name one aggregate in `writes`, live under that aggregate, and change one instance, unless a rule that governs the use case is named in its consistency `boundBy` and names in `basedOn` the decision recording the invariant needing more in one transaction, its record states `consistency: per-record` for independent instances, or it changes no aggregate and declares `writes: []`.

**Rationale:** A command that changes two aggregate types in one transaction couples their consistency boundaries: one failing part rolls back work that had no reason to fail with it, and the change cannot move to eventual consistency later. One aggregate type per command keeps each transaction on one boundary. The command changes one instance; when its record states `consistency: per-record`, each instance it changes is independent with no invariant spanning two, and it may stage several in one commit. A command that changes two instances under one invariant, such as an original and its reversal or two statements resolving one duplicate, is split by reaction like two aggregates, or the invariant moves into a unique constraint the record names. A command that changes no aggregate, because it verifies or reports and raises a fact, is allowed and declares `writes: []`. Another aggregate of the same module follows through a reaction to the domain event, with its own commit, and time-based work is a job that selects instances and sends one command per instance. A command writing more than one aggregate type is usually a sign the aggregate boundary is drawn wrong; it stays allowed only when a rule that governs the use case is named in its consistency `boundBy`, stating the invariant needing both in one transaction, and names its decision in `basedOn`. Every command belongs to the aggregate it changes, so a command at module level is a finding, and module level keeps only module-wide reads such as an export archive.

**Example:** The reimburse-expense command names `aggregate/spending.expense` in `writes` and lives under the expenses aggregate. The reporting reaction to its reimbursed fact answers in its own transaction instead of joining the write. The attach-receipt command names two aggregates with its atomicity rule in `boundBy` naming the decision that records the invariant.

**Checked by:** `litespec model check` reports `use-case-writes-invalid` for a command whose `writes` names more than one aggregate without a `boundBy` rule that governs the use case and names a decision in `basedOn`, and `record-placement-invalid` for a command use case at module level.

### Cite the Invariants Most Writers Cite

**ID:** `litespec/core/use-cases#cite-the-invariants-most-writers-cite`

**Strength:** required

**Applies to:** artifact: record:use-case; when: command

**Statement:** Each command MUST cite every invariant rule that governs an aggregate it writes when most commands writing that aggregate cite the rule.

**Rationale:** A command that writes an aggregate can break every invariant governing it, and the sibling commands writing the same aggregate show which invariants those writes meet. When most of them cite an invariant, the outlier posts without the checks the others apply. A command cites a rule in its steps' cites, its input rules, its consistency boundBy, or a result carrying the rule's refusal code. Only invariants qualify, since any other kind constrains single commands, while an invariant holds on every change. Settled by [decision/0101-check-rule-citations](decision/0101-check-rule-citations).

**Example:** Three library commands write the loan aggregate. Two cite the one-hold-per-copy invariant, and the third does not, so the third is reported.

**Checked by:** `litespec model check` reports `rule-sibling-uncited` for a command writing an aggregate without citing an invariant most commands writing that aggregate cite.

### Declare Consistency on Multi-Aggregate Commands

**ID:** `litespec/core/use-cases#declare-consistency-on-multi-aggregate-commands`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each command writing two or more aggregates, or reading an aggregate whose rule guards its write, MUST declare its write consistency in `consistency`.

**Rationale:** Consistency left unsaid is read as atomic until a multi-aggregate write fails halfway. Declaring the mode states the promise: `atomic` commits every write in `writes` as one group, and `per-record` commits each write on its own. A command that writes one aggregate and reads an aggregate whose rule guards its write declares `atomic` with the guarding aggregates in `guards`, so the guard still holds when the command commits.

An atomic write across aggregates cites its `atomicity` rule in `boundBy`, and `writes` holds exactly the aggregates the rule binds. The command also needs its decision, as `litespec/core/use-cases#change-one-aggregate-in-each-command` states. A guarded command cites the guarding rule in `boundBy` and names in `guards` the aggregates it reads, and does not write. A group above 4 aggregates is reported. Atomic consistency never spans an `async` edge. A command writing one aggregate and reading no guarding aggregate is atomic by definition and carries no `consistency`, and a query carries none. [Domain Model](domain-model.md) states the `atomicity` rule. The `consistency-cites-missing` check reports an atomic multi-aggregate write with no `boundBy`, `atomicity-drift` reports `writes` differing from the bound group, and `atomicity-invalid` reports a group above 4 aggregates. Settled by [decision/0076-remove-shapes-nothing-needs](decision/0076-remove-shapes-nothing-needs).

**Example:** The reimburse-expense command declares atomic consistency citing the reimbursed-once rule. The self-checkout scan declares per-record consistency across its attempts. The submit-expense command writes the expense and reads the report, so it declares atomic consistency citing the open-report rule with the report in `guards`.

**Checked by:** `litespec model check` reports `consistency-cites-missing` for an atomic multi-aggregate write with no `boundBy`, `atomicity-drift` for `writes` differing from the bound group, `atomicity-invalid` (level A) for a group above 4 aggregates, and `atomicity-duplicate` (level A) for a command saving more than one atomic write group; `litespec model check` reports `record-invalid` for a query carrying `consistency`.

### Hold Every Cross-Aggregate Guard Until Commit

**ID:** `litespec/core/use-cases#hold-every-cross-aggregate-guard-until-commit`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each command whose `boundBy` rule governs an aggregate of its own module that the command reads but does not write MUST name that aggregate in `consistency.guards`.

**Rationale:** A guard read at the start of a command can change before the command commits: submit-expense reads the report as open, close-report closes it and commits, and submit-expense then commits an expense on a closed report. Naming the guarding aggregate in `guards` states that the rule must still hold when the command commits. Mode `atomic` with `guards` means the guard holds at commit. The stack page states how the guarded read holds the guard, by a lock, a version check, or serializable isolation. No guard reaches into another module: the guard holds at commit, and no commit spans modules. A command bound by a rule over another module's aggregate is reported, and [Edges and Screens](edges.md) states how the two modules keep the fact consistent without a lock.

**Example:** The submit-expense command reads the report and writes the expense, with the open-report rule in `boundBy`. It names the report in `guards`, so a racing close-report commit either waits or makes submit-expense refuse.

**Checked by:** `litespec model check` reports `guard-undeclared` (level G) for a command whose `boundBy` rule governs an aggregate the command reads but does not write, and whose `consistency.guards` does not name that aggregate, and `guard-module-foreign` for a guard naming an aggregate of another module, or a bound rule over one the command reads but does not write.

### Prove Every Guard Against a Competing Command

**ID:** `litespec/core/use-cases#prove-every-guard-against-a-competing-command`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** At large size, each command carrying `consistency.guards` MUST be shown by at least one scenario naming the competing command in `competesWith`.

**Rationale:** Every scenario that runs alone passes while the race stays unproven: each command meets the guard in its own state and commits. Running the guarded command against its competitor shows what the race brings. The scenario names the competing command in `competesWith`, and its `then` states the one refusal or the serialized outcome.

**Example:** A submit-expense scenario carries `competesWith: use-case/spending.close-report`. Its `then` states that the expense is refused because the report closed, or that one command commits first and the other meets the new state.

**Checked by:** `litespec model check` reports `guard-race-scenario-missing` (level G) for a command with `consistency.guards` and no scenario carrying `competesWith`.

### Declare Every Effect with One Verb and Its Timing

**ID:** `litespec/core/use-cases#declare-every-effect-with-one-verb-and-its-timing`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each effect entry MUST carry exactly one verb and its timing.

**Rationale:** An edge no record declares stays hidden: calls between use cases, reactions, jobs, notifications, integration events, and external systems that no reader can see. Declaring each edge with exactly one verb, its timing, and its failure handling turns every coupling into data.

The verb is `sends`, `calls`, `queries`, or `commands`. The timing is `inline`, `after-commit`, or `async`, and timing is required on every use-case effect. On a command, a `sends` or `calls` effect carries `after-commit` or `async` timing, and an event handler runs the consequence after the commit. A `queries` entry names the public query use case of another module the behavior reads and carries `inline` timing: the read runs within the handling. A `commands` entry names the public command use case of another module the behavior sends and carries `inline` timing: the send runs within the handling. A use case declares no `runs`, no `emits`, and no `commands`. A reaction to its raised fact runs the follow-on work, and the integration events it publishes follow from `raises` and the `translates` field of each outbound integration event. The `effect-undeclared` check reports an edge the records never declared.

**Example:** The reimburse-expense command declares no effect. It raises its reimbursed fact, the expense-reimbursed integration event translates that fact, and the reporting module's read model answers the published event.

**Checked by:** `litespec model check` reports `effect-undeclared` for an edge the records never declared and `effect-timing-invalid` for a command `sends` or `calls` effect with `inline` timing; `litespec model check` reports `record-invalid` for an effect entry without exactly one verb or its timing.

### Run Follow-On Work Only from Reactions and Jobs

**ID:** `litespec/core/use-cases#run-follow-on-work-only-from-reactions-and-jobs`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A use case MUST NOT run another use case, so each follow-on act runs from a reaction to the fact the use case raised, or from a job.

**Rationale:** A use case that runs another use case nests one command inside another. The inner command runs inside the outer commit. Its failure rolls back work that had no reason to fail, and its record no longer states what it commits. A reaction to the raised fact runs the follow-on act in its own transaction, with its own failure handling and retry, so each record states one unit of work. The caller learns the outcome from the later fact instead of from the call. A `queries` edge reads another module's public query; it is not a `runs` edge. A use case declares no `commands` edge: only a reaction or job sends another module's public command. Settled by [decision/0064-raise-facts-and-send-nothing-from-handlers](decision/0064-raise-facts-and-send-nothing-from-handlers).

**Example:** In the library product, borrowing a copy raises the loan-borrowed fact. The send-due-notice reaction answers it in its own transaction, instead of the borrow command sending the notice.

**Checked by:** `litespec model check` reports `record-invalid` for a use-case effect that names `runs`, `emits`, or `commands`.

### Name the Events Each Use Case Raises

**ID:** `litespec/core/use-cases#name-the-events-each-use-case-raises`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use case MUST name the domain events it raises in `raises`.

**Rationale:** An event raised but unnamed leaves reactions waiting on a fact no behavior admits raising. Naming the raised events ties each fact to the behavior that announces it.

**Reviewed for:** Does every use case name the domain events it raises in `raises`? Look at `raises` beside each use-case page.

### Name the Idempotency of Every Command

**ID:** `litespec/core/use-cases#name-the-idempotency-of-every-command`

**Strength:** required

**Applies to:** artifact: record:use-case; when: command

**Statement:** Each command MUST declare its idempotency mode with the key, key scope, or reason that mode needs.

**Rationale:** A retried call without a stated idempotency runs twice: a double payout, a double loan, a double notice. Declaring the mode states how repeats collapse. The mode is `key`, `state`, or `none`, with its key template and key scope. Mode `none` carries the reason repeats cannot happen. The `key` templates `{field}` placeholders over the record's `input` field names, and the `keyScope` is `tenant`, `account`, `login`, or `global`. A query carries no `idempotency`, since a read is idempotent by kind. A reaction or job carries its own `idempotency`, as [Automation](automation.md) states. The self-checkout scan keys on `{nonce}` scoped to the login.

**Checked by:** `litespec model check` reports `record-invalid` for a command without an idempotency mode or without the key, key scope, or reason its mode needs; `litespec model check` reports `record-invalid` for a key with no `{field}` placeholder or a key scope outside `tenant`, `account`, `login`, and `global`; `litespec model check` reports `record-invalid` for a query carrying `idempotency`. `litespec model check` reports `idempotency-key-uncarried` for a key placeholder naming no carried field.

### Carry Every Key Placeholder

**ID:** `litespec/core/use-cases#carry-every-key-placeholder`

**Strength:** required

**Applies to:** artifact: record:use-case, record:reaction, record:job

**Statement:** Each idempotency or deduplication key MUST name only placeholders that the record carries: an input of a use case, a field of an event a reaction answers, or an input of a use case a job runs.

**Rationale:** A key template is evaluated when a repeat arrives. A placeholder naming no value the record carries is never filled, so every call computes the same key or no key at all: distinct expenses collapse onto one replay, and a reaction never deduplicates the fact it answers. A use case carries its input field names. A reaction carries the fields of the domain event, module event or integration event in `triggeredBy`. A job carries the input field names of the use cases it runs or sends. Naming only carried fields keeps each key unique to the work it guards.

**Example:** In Claimwick, `use-case/spending.submit-expense` keys repeats on `{expenseId}`, an input it carries. A key on `{attemptId}` would be reported, because no input or triggering event supplies an attempt identifier.

**Checked by:** `litespec model check` reports `idempotency-key-uncarried` (level G) for each key placeholder that no input of a use case, no field of a reaction's triggering event, and no input of a job's run use case carries.

### State What a Replay Does

**ID:** `litespec/core/use-cases#state-what-a-replay-does`

**Strength:** required

**Applies to:** artifact: record:use-case; when: command

**Statement:** Each command carrying `idempotency` MUST state what a recognized replay does in `replay` and how the command recognizes it.

**Rationale:** A repeat the command recognizes but handles nowhere either runs twice or fails without telling why: a double payout, or a refusal that hides the first success. Stating `returns-first-result`, `refuses`, or `acts-again` tells the caller what the repeat brings. A keyed command recognizes the repeat by the key the caller sends. A state command names the state it checks in `recognizedBy`, such as the posted entry the second booking meets, and the command recognizes the repeat there, not by a stored key, which would outlive an undone state.

**Example:** The reimburse-expense command replays with `returns-first-result`. A retried payout returns the first reimbursement instead of posting again.

**Checked by:** `litespec model check` reports `idempotency-replay-missing` for a command `idempotency` without `replay`, or mode `state` without `recognizedBy`.

### State the Audit Position of Every Command

**ID:** `litespec/core/use-cases#state-the-audit-position-of-every-command`

**Strength:** required

**Applies to:** artifact: record:use-case; when: command

**Statement:** Each command MUST declare its audit level and target kind, or its exemption with its justification in `exemptBecause`, when the project selects the `audit` extension.

**Rationale:** An unaudited change with no exemption is a gap nobody admitted: money moves, holds lapse, and no record says who asked. Declaring the level with its target kind, or the exemption with its justification, states the audit beside the change. The level is `required`, `optional`, or `none`, and the exemption carries its justification in `exemptBecause`. A level of `none` is not audited, and async work audits at request time. The `audit` extension owns the field, as `litespec/ext/audit#declare-which-commands-are-audited` states, so a command in a project without that extension declares no `audit`.

**Checked by:** `litespec model check` reports `audit-missing` for a command without an audit level and target kind or an exemption with its justification when the project selects the `audit` extension, and `field-extension-unselected` (level A) for `audit` written while the extension is not selected.

### Declare Every Query Output

**ID:** `litespec/core/use-cases#declare-every-query-output`

**Strength:** required

**Applies to:** artifact: record:use-case; when: query

**Statement:** Each query MUST declare its output in named `include` groups.

**Rationale:** A query without a declared output returns whatever its author fetched. Readers and screens guess the shape from code. Declaring the output in named groups with fields, classifications, the aggregates each group reads, and freshness states what readers receive, so the shape is reviewable before anything implements it. The `query-output-missing` check reports a query with no declared output.

**Example:** The list-expenses query declares open and decided groups, each with `{ "mode": "immediate" }` freshness.

**Checked by:** `litespec model check` reports `query-output-missing` for a query with no declared output.

### Name the Use Case That Decides a Stale Figure

**ID:** `litespec/core/use-cases#name-the-use-case-that-decides-a-stale-figure`

**Strength:** required

**Applies to:** artifact: record:use-case, record:read-model

**Statement:** Each query output group and each read model whose figure a command re-decides at write time MUST name in `decidedBy` that command use case or its aggregate, in the module that owns the figure's source.

**Rationale:** A stale figure without a named decider leaves the reader guessing who settles it: the page shows seat availability, and nothing says the hold command re-decides it at write time. Naming the deciding command use case, or its aggregate, states the link beside the figure, so the reader follows it to the current truth. The schema allows `decidedBy` only with `snapshot` or `eventual` freshness, since an `immediate` figure is current and names no decider. A query decides nothing, so a `decidedBy` naming one is reported. The decider sits in the module that owns the figure's source, as `litespec/core/automation#decide-current-truth-in-the-owning-module` states.

**Example:** The reimbursed-expenses list carries `snapshot` freshness and names `use-case/spending.reimburse-expense` in `decidedBy`. A reader seeing a stale row follows the link to the command that decides each reimbursement.

**Checked by:** `litespec model check` reports `freshness-decider-invalid` (level G) for a `decidedBy` naming a query or a use case or aggregate outside the module that owns the figure's source, and `record-invalid` for a `decidedBy` with `immediate` freshness.

### Justify Every Wider Visibility

**ID:** `litespec/core/use-cases#justify-every-wider-visibility`

**Strength:** required

**Applies to:** artifact: record:use-case; when: query

**Statement:** Each include group visible to more roles than the aggregates it reads MUST cite the requirement widening it in `widenedBy`.

**Rationale:** A read showing more than the aggregates it reads widens without a recorded choice: roles see figures their own grants never allowed, and no requirement states the view. Citing the requirement in `widenedBy` keeps the widening a decided choice. The `visibleWhen` field states the condition where one narrows it. The `widening-cites-missing` check reports a widened group with no cited requirement.

**Checked by:** `litespec model check` reports `widening-cites-missing` for a widened include group with no cited requirement.

### Declare Paging on Every Collection Query

**ID:** `litespec/core/use-cases#declare-paging-on-every-collection-query`

**Strength:** required

**Applies to:** artifact: record:use-case; when: query

**Statement:** Each collection query MUST declare its paging with `mode` of `pages`, or `{ "mode": "none", "reason": "..." }` with its reason.

**Rationale:** A collection without paging works until the data outgrows it: the first hundred rows return, and the next ten thousand time out. Declaring the order, the cursor or offset, and the default and maximum limits with `mode` of `pages` states the paging beside the read. The value `{ "mode": "none", "reason": "..." }` with its reason states why a query returns one object. The `paging-undeclared` check reports a collection query with no paging.

**Checked by:** `litespec model check` reports `paging-undeclared` for a collection query with no paging.

### Type Every Output Field

**ID:** `litespec/core/use-cases#type-every-output-field`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each output field MUST carry its value type.

**Rationale:** An output field without a type returns whatever its author fetched. Readers and screens guess the shape from code. Carrying the value type from the input types on every field of an `include` group and of a command output states the shape beside the promise, so the builder compares the records with the generated contract. A list of rows takes `type: list` with its row shape in `rowShape`, as the input types state. Settled by [decision/0073-type-every-output-and-declare-refusal-sets](decision/0073-type-every-output-and-declare-refusal-sets).

**Example:** The list-expenses query types its amount field `money`. It types its lines field `list` with the row shape in `rowShape`.

**Checked by:** `litespec model check` reports `record-invalid` for an output field without `type`.

### Declare Each Aggregate's Refusals Once

**ID:** `litespec/core/use-cases#declare-each-aggregates-refusals-once`

**Strength:** required

**Applies to:** artifact: record:refusal-set

**Statement:** Each aggregate MUST declare the refusals its rules raise once in `behavior/modules/<m>/aggregates/<aggregates>/refusals.yaml`, with each entry carrying its `id` and `title`.

**Rationale:** Refusals declared per use case drift: one use case retires a code while its neighbor keeps it, and the same breach reports two refusals. Declaring each refusal once beside the aggregate whose rules raise it, mirroring `Domain/{Aggregates}/Exceptions/`, keeps one refusal and one meaning behind every result citing it. Each entry carries its `id` as `refusal/<module>.<name>` and its `title` like every other entry; the code is the id's name, so the code `spending.receipt-missing` stays the API's problem code and the audit trail's stored value. A result's `refusal` names a refusal from its aggregate's set, and a rule's `refusal` names the same refusal, so every result carrying it is that rule's refusal in that use case. Settled by [decision/0073-type-every-output-and-declare-refusal-sets](decision/0073-type-every-output-and-declare-refusal-sets).

**Example:** The expense aggregate declares `refusal/spending.receipt-missing` with its meaning. The submit-expense refusal cites that id.

**Checked by:** `litespec model check` reports `reference-unknown` for a result refusal missing from its aggregate's refusal set, `record-invalid` for a refusal entry without `id` or `title`, and `code-placement-drift` or `record-placement-invalid` for a refusal and its exception declared under different homes: a refusal declared under an aggregate whose exception lives under another aggregate or outside `Domain/`, or an aggregate's exception whose refusal is declared at module level.

### Declare Each Module's Refusals Once

**ID:** `litespec/core/use-cases#declare-each-modules-refusals-once`

**Strength:** required

**Applies to:** artifact: record:refusal-set

**Statement:** Each module MUST declare in `behavior/modules/<m>/refusals.yaml` only refusals no aggregate raises, with each code's meaning.

**Rationale:** A refusal an aggregate's rules raise is declared in that aggregate's set, as `litespec/core/use-cases#declare-each-aggregates-refusals-once` states. The module's set keeps only the refusals no aggregate raises: a file refused before any aggregate exists, or a module-wide refusal. Each exception carrying a module-level refusal sits under `Domain/{Aggregates}/Exceptions/` like any other exception. The project declares `problemTypeBase` once in `.litespec/project.yaml`: the problem type of a code is the base followed by the code's part after its module, and the code's part after its module as a short RFC 9457 type name where no base is declared. The book joins the meaning onto each result citing the refusal. Settled by [decision/0073-type-every-output-and-declare-refusal-sets](decision/0073-type-every-output-and-declare-refusal-sets).

**Example:** The spending module declares a module-wide refusal with its meaning. With a `problemTypeBase` of `https://claimwick.example/problems/`, its problem type is the base followed by the code's part after the module, and without a base it is the short name, the part after the module.

**Checked by:** `litespec model check` reports `reference-unknown` for a result refusal missing from its module's refusal set, `record-placement-invalid` for a module-level refusal raised by exactly one aggregate, and `code-placement-drift` for a module-level refusal carried outside `Domain/` or directly in `Domain/`.

### End Every Use Case in Named Results

**ID:** `litespec/core/use-cases#end-every-use-case-in-named-results`

**Strength:** required

**Applies to:** artifact: record:use-case, record:result

**Statement:** Each use case MUST declare its success result and, on every refusal result, its `refusal`.

**Rationale:** Callers branch on named endings: success, or one refusal result with its stable code. Without a named success, readers guess what good looks like. Without a refusal id on a refusal result, each architecture layer reports the same breach another way. The `use-case-success-missing` check reports a use case with no success result, and `result-undeclared` reports a refusal the records never declared.

**Example:** The reimburse-expense command declares `reimbursed`, with the refusal results citing `refusal/spending.not-approved` and `refusal/spending.already-reimbursed`.

**Checked by:** `litespec model check` reports `use-case-success-missing` for a use case with no success result; each refusal result on the use-case page carries its `refusal`.

### Name Each Refusal Code for the State That Refuses

**ID:** `litespec/core/use-cases#name-each-refusal-code-for-the-state-that-refuses`

**Strength:** required

**Applies to:** artifact: record:result

**Statement:** Each refusal id MUST read `refusal/<module>.<noun>-<state>` and name the state, not the failure.

**Rationale:** The code is the id's name, and it is a public contract. The words `invalid`, `failed`, or `error` tell an integrator nothing to do. A code naming the state tells what holds and what to change. The module segment names the module that owns the refused state, which may differ from the module of the refusing use case. A code built at run time from a key, such as the conflict for a lost uniqueness key, declares its template in `refusals.composed`.

**Example:** `refusal/spending.receipt-missing` names the missing receipt. The id `refusal/spending.receipt-invalid` hides the state behind a vague word and is reported.

**Reviewed for:** Does every refusal id read `refusal/<module>.<noun>-<state>` and name its state? Look at the refusal ids on each use-case page.

### Retire Refusal Codes with Their Replacements

**ID:** `litespec/core/use-cases#retire-refusal-codes-with-their-replacements`

**Strength:** required

**Applies to:** artifact: record:result

**Statement:** A refusal code in use MUST be retired with its replacement named, never renamed or reused.

**Rationale:** The code is a public contract, and clients branch on it. Renaming a code in use silently breaks them. Reusing a retired code for another state sends old clients down the new branch. Retiring the code with its replacement named keeps every past refusal readable and every new client on the new code.

**Example:** The retired `refusal/spending.receipt-missing` names `refusal/spending.receipt-absent` as its replacement. No later refusal reuses the old code.

**Reviewed for:** Which refusal codes in use were renamed or reused instead of retired with a named replacement? Look at the refusal codes beside their results.

### Order Every Refusal List by Precedence

**ID:** `litespec/core/use-cases#order-every-refusal-list-by-precedence`

**Strength:** required

**Applies to:** artifact: record:result

**Statement:** Each use case MUST list its refusals so the first match wins.

**Rationale:** Two refusals matching one call need an order. Without precedence, each architecture layer picks its own refusal, and callers branch on a different code per stack. The derived refusals come first: unauthenticated, forbidden, sign-in-too-old, invalid input, not found, then conflict. The written refusals follow in the order the page lists them. The first match is the one every architecture layer reports.

**Reviewed for:** Does every refusal list stand in precedence order, with derived refusals first? Look at the refusal entries on each use-case page.

### Record Attempts Behind Recorded Refusals

**ID:** `litespec/core/use-cases#record-attempts-behind-recorded-refusals`

**Strength:** required

**Applies to:** artifact: record:result

**Statement:** Each refusal carrying `recorded: true` MUST store the attempt with its reason.

**Rationale:** Some denials need evidence behind them: a loan denial, a limit refusal, a fraud block. Without a stored attempt, the business has nothing to answer with. Storing the attempt with its reason keeps that evidence. A validating refusal, such as a malformed code, stores nothing.

**Example:** The self-checkout scan records unknown, retired, and already-lent barcodes with their reasons. A malformed scan stores nothing.

**Reviewed for:** Does every refusal carrying `recorded: true` store the attempt with its reason? Look at the recorded refusals beside their use-case pages.

### List Recorded Outcomes Inside Success Codes

**ID:** `litespec/core/use-cases#list-recorded-outcomes-inside-success-codes`

**Strength:** required

**Applies to:** artifact: record:result

**Statement:** Each success result with recorded outcomes MUST list them in `codes`.

**Rationale:** A success hiding several recorded outcomes forces callers to guess: borrow and renew both succeed, but the due date and the analytics differ. Listing the closed codes inside the success states each recorded outcome beside the ending.

**Example:** The checked-out result lists `borrow` and `renew` as its recorded outcomes.

**Reviewed for:** Does every success result with recorded outcomes list them in `codes`? Look at the success results beside their use-case pages.

### Derive Generic Refusals Instead of Writing Them

**ID:** `litespec/core/use-cases#derive-generic-refusals-instead-of-writing-them`

**Strength:** required

**Applies to:** artifact: record:result

**Statement:** A use case MUST NOT write a generic refusal as a record.

**Rationale:** Hand-written generic refusals multiply: hundreds of pages each declare their own forbidden, and each drifts in wording and code. Deriving them keeps one of each per use case, current with the grant, the inputs, the target, the state, the caller, and the sign-in. The builder derives `result/<use-case>.forbidden`, `result/<use-case>.invalid-input`, `result/<use-case>.not-found`, `result/<use-case>.conflict`, `result/<use-case>.unauthenticated`, and `result/<use-case>.sign-in-too-old`. Scenarios point at the derived results without anyone writing them.

**Checked by:** `litespec model check` reports `refusal-generic-written` for a use case writing a generic refusal as a record.

### Name Only a Provided Way Out in Each Refusal Sentence

**ID:** `litespec/core/use-cases#name-only-a-provided-way-out-in-each-refusal-sentence`

**Strength:** advisory

**Applies to:** artifact: record:result

**Statement:** Each refusal sentence that names a next step SHOULD name a use case, screen, setting, or input that exists.

**Deviation:** A way out the product cannot provide, such as visiting a bank branch, stays as prose naming the place.

**Rationale:** The refusal sentence is what the user reads. When it promises a way out the product lacks, the user follows the advice into the same refusal: the sentence says to submit the expense to the next open report instead, but the command takes no report. Naming only a use case, screen, setting, or input that exists keeps every promised step one the user can take.

**Example:** Claimwick's closed-report refusal carries "This expense falls in a closed report. Reopen the report and submit again." only when the reopen use case exists; otherwise it carries "This expense falls in a closed report, so it cannot be submitted."

**Checked by:** `litespec model check` reports `refusal-next-step-unknown` (level A) for a refusal sentence naming a `use-case/`, `screen/`, or `setting/` id no record holds, or carrying `instead`, `alternatively`, `you can also`, or `or you can`.

### Name the Result Each Scenario Ends In

**ID:** `litespec/core/use-cases#name-the-result-each-scenario-ends-in`

**Strength:** required

**Applies to:** artifact: record:scenario

**Statement:** Each scenario MUST name the result it ends in, written or derived.

**Rationale:** A scenario that names no result reads well and proves nothing: it illustrates behavior without asserting an ending. Naming the result ties each example to the ending it walks toward, so every test knows what to assert. A reaction or job scenario names a result or an outcome instead, as [Automation](automation.md) states. The `reference-unknown` check reports a scenario ending in an unknown result.

**Checked by:** `litespec model check` reports `record-invalid` for a scenario entry without `result`, and `reference-unknown` for a scenario ending in another use case's result. `litespec model check` reports `scenario-result-unknown` for a result id no record declares.

### Declare the Result a Scenario Ends In

**ID:** `litespec/core/use-cases#declare-the-result-a-scenario-ends-in`

**Strength:** required

**Applies to:** artifact: record:scenario

**Statement:** The result a scenario names MUST be declared by the use case: a written result, or one of the generic refusals the model derives.

**Rationale:** A scenario whose ending no result declares asserts an outcome the use case never promises, so its test proves a story nobody wrote down. A written result is a child record of the use case, and a derived generic refusal follows from the access and inputs. Pointing the scenario at `result/<use-case>.<name>` with no such result leaves the proof attached to nothing. Naming a declared ending ties each scenario to an outcome the use case owns.

**Example:** A Claimwick scenario for `use-case/spending.reimburse-expense` that ends in `result/reimburse-expense.already-reimbursed` needs a written result with that id. Ending in `result/reimburse-expense.not-found` needs none, because the model derives that refusal.

**Checked by:** `litespec model check` reports `scenario-result-unknown` (level G) for a scenario ending in a result id that no result record declares and is not a derived generic refusal.

### Prove Every Written Result with a Scenario

**ID:** `litespec/core/use-cases#prove-every-written-result-with-a-scenario`

**Strength:** required

**Applies to:** artifact: record:result

**Statement:** At large size or with the `bdd` extension, each written result MUST be shown by at least one scenario of its use case.

**Rationale:** A written result no scenario shows is an ending nobody proved: it reads on the page and is asserted in no test, so it drifts from the code with the next change. One scenario per result pins each ending as a checked fact, including the refusals a caller can meet.

**Example:** The reimburse-expense command declares the `already-reimbursed` refusal. A scenario walks an approved expense claimed twice to that refusal.

**Checked by:** `litespec model check` reports `result-scenario-missing` (level G) for a written result no scenario shows.

### Write Every Scenario in Given When Then

**ID:** `litespec/core/use-cases#write-every-scenario-in-given-when-then`

**Strength:** required

**Applies to:** artifact: record:scenario

**Statement:** Each scenario MUST state its `given`, `when`, and `then`.

**Rationale:** Collapsing the three steps blurs what was true, what moved, and what the test checks. The three steps separate the situation from the action and the action from the assertion. Stating all three keeps each example readable by a person and provable by a test. Example rows of one scenario share one set of keys.

**Example:**

```json
{
  "type": "scenario",
  "id": "scenario/submit-expense.submitting-with-a-receipt-succeeds",
  "title": "Submitting with a receipt succeeds",
  "given": "An employee paid for a train ticket and kept the receipt.",
  "when": "The employee submits the expense with the receipt.",
  "then": "The expense is submitted.",
  "result": "result/submit-expense.submitted"
}
```

**Checked by:** `litespec model check` reports `record-invalid` for a scenario entry without `given`, `when`, or `then`, and `example-keys-drift` for example rows of one scenario with differing keys.

### Exercise Rules from Scenarios

**ID:** `litespec/core/use-cases#exercise-rules-from-scenarios`

**Strength:** required

**Applies to:** artifact: record:scenario

**Statement:** A scenario MUST name the rules it shows in `exercises`.

**Rationale:** A rule proved only through a test mark hides which example walks it: readers open the rule and find no scenario showing it. Naming the shown rules in `exercises` ties each example to the rules it walks, and every rule meets its scenarios. A refusal scenario names the rule behind its result. The scenarios of a use case together walk every rule its inputs cite. An `exercises` entry naming a rule the use case never applies inflates coverage and sends a test author looking for behavior the scenario never shows. Settled by [decision/0101-check-rule-citations](decision/0101-check-rule-citations).

**Example:** In the library product, a scenario walking a second hold on one copy names `rule/lending.one-hold-per-copy` in `exercises`.

**Checked by:** `litespec model check` reports `scenario-rule-unexercised` for a refusal scenario whose result's rule is not in `exercises`, or an input rule no scenario of the use case exercises, and `exercises-uncited` (level A) for a scenario exercising a rule its use case cites nowhere in its steps, inputs, or results.

### Keep Each Scenario Inside Its Operation

**ID:** `litespec/core/use-cases#keep-each-scenario-inside-its-operation`

**Strength:** required

**Applies to:** artifact: record:scenario

**Statement:** Each scenario MUST assert only its own operation's results, state, and raised events in `then`.

**Rationale:** A scenario that asserts a later operation's effect ties two operations in one test. When the first operation changes, the second breaks, and the failure points at the wrong record. Asserting only the scenario's own results, state, and raised events keeps each test on its record. An effect of a later operation belongs to a flow test. Settled by [decision/0081-own-scenarios-and-declare-outcomes](decision/0081-own-scenarios-and-declare-outcomes).

**Example:** The submit-expense scenario asserts the stored expense and its submitted fact. It leaves the approver payout to the flow test walking both operations.

**Reviewed for:** Does every scenario assert only its own operation in `then`? Look at `then` beside the scenario's own operation.

### Give Each Rule Boundary Its Own Scenario

**ID:** `litespec/core/use-cases#give-each-rule-boundary-its-own-scenario`

**Strength:** required

**Applies to:** artifact: record:scenario

**Statement:** Each rule boundary a use case exercises MUST have its own scenario at the boundary.

**Rationale:** A boundary shown only from the safe side proves nothing about the edge. Expiry, attempt limits, and one-time use each fail exactly at the line. Giving each boundary named in `exercises` its own scenario at the line pins the edge as a checked fact. Settled by [decision/0071-give-every-stated-rule-a-field](decision/0071-give-every-stated-rule-a-field).

**Example:** The hold expiry rule carries one scenario collecting on the last pickup day, and one collecting after the lapse.

**Reviewed for:** Does every exercised rule boundary have its own scenario at the boundary? Look at `exercises` beside the scenarios of the use case.

### Share Scenario Setup in Background

**ID:** `litespec/core/use-cases#share-scenario-setup-in-background`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A use case MUST hold the shared Given of its scenarios in `background`.

**Rationale:** When every scenario repeats the same setup, the step that differs gets buried, and readers reread one Given on each scenario. Holding the shared Given once in `background` leaves each scenario its own situation, action, and assertion. Company narrative belongs in the explanation file, not in `background`.

**Example:** `background` holds that the employee has a session and one submitted expense awaits approval. Each approval scenario starts there.

**Checked by:** `litespec model check` reports `background-scenarios-missing` for a use case with `background` and fewer than two scenarios.

### Summarize Every Use Case, Actor, Role, and External System

**ID:** `litespec/core/use-cases#summarize-every-use-case`

**Strength:** required

**Applies to:** artifact: record:use-case, record:actor, record:role, record:external-system

**Statement:** Each use case, actor, role, and external system MUST carry `summary`.

**Rationale:** A reader landing cold needs the story before its fields. One paragraph states what the use case is; its steps live in `steps` in order, and the explanation restates them under Flow. An actor or role `summary` states in plain sentences who the party is and what the grant is for, which the fields alone do not say, so the grant reads as a decision rather than a row. An external system `summary` states in plain sentences what the outside system does for the product, which the message lists alone do not say, so the dependency reads as a decision rather than a row.

**Example:** The submit-expense summary states that an employee files a work expense for approval. Its `steps` list validation, storage, and the approver notice in order, and its Flow section restates them.

**Checked by:** `litespec model check` reports `prose-missing` for a use case, actor, role, or external system without `summary`.

### Tie Every Setting Default to a Requirement

**ID:** `litespec/core/use-cases#tie-every-setting-default-to-a-requirement`

**Strength:** required

**Applies to:** artifact: record:setting

**Statement:** Each setting MUST declare its value type, range, unit, setter, and bound behavior, with the requirement behind its default.

**Rationale:** A default no requirement holds is a figure nobody stated: a delay, a timeout, or a batch size nobody decided. Declaring the value type, the range, the unit, the setter, and the bound behavior states the tunable beside its promise.

A setting holds configuration read by the running system. A business value a person changes through their work is aggregate data instead: its range is a rule with a refusal, and its change is a use case with access and audit. The setter is product, operator, or tenant; a tenant setting is changed by a `change-*` use case. The bound behavior is `refuse`, `truncate`, or `degrade`. The default takes the setting's `valueType`: a duration default holds an ISO 8601 duration, a money default holds an amount with its currency, and a choice default names one of its `choices`. A setting whose values apply from a date carries `dated: true`, and no value change touches a posted record. The `setting-requirement-missing` check reports a default with no cited requirement.

**Example:** The approval reminder delay declares a duration with an operator-set default of `PT72H`, citing the requirement that each expense is decided within five days.

**Checked by:** `litespec model check` reports `setting-requirement-missing` for a setting default with no cited requirement.

### Name the Settings Each Use Case Reads

**ID:** `litespec/core/use-cases#name-the-settings-each-use-case-reads`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use case MUST name the settings it reads in `reads`.

**Rationale:** A setting a use case reads but no record names is a coupling no one can see: the tunable moves, the use case reading it breaks, and no diff shows the pair. Naming the settings in `reads` beside the use case states the coupling beside the behavior that reads it, so each change to the range or default meets the use cases that read it. The setting's readers are computed as the inverse, never written. A setting no use case, reaction, or job reads is a finding. Settled by [decision/0012-use-cases-read-their-settings](decision/0012-use-cases-read-their-settings) on the evidence in [research/behavior-containment-evidence](research/behavior-containment-evidence) and [research/field-naming-evidence](research/field-naming-evidence).

**Example:** The send-approver-reminders reaction names `setting/spending.approval-reminder-delay` in `reads`. A move of the delay then meets the reaction that reads it.

**Checked by:** `litespec model check` reports `setting-unused` for a setting no record `reads`.

### Name the Configuration Key of Every Operator Setting

**ID:** `litespec/core/use-cases#name-the-configuration-key-of-every-operator-setting`

**Strength:** required

**Applies to:** artifact: record:setting

**Statement:** A setting the operator sets MUST name its configuration path in `configurationPath`.

**Rationale:** An operator setting reaches the product through configuration. The record and the configuration meet at one path: the record states the default and the bounds, and the configuration names the value. Without the path, a renamed section silently falls back to a default nobody chose, and no reader can tell which configuration value a promise rests on. A setting a tenant or the product sets names no configuration path; a tenant setting is changed by a `change-*` use case, as [Give Every Tenant Setting a Change Use Case](#give-every-tenant-setting-a-change-use-case) states.

**Example:** Claimwick's `setting/spending.approval-reminder-delay` is set by the operator. It carries `configurationPath` `Spending:ApprovalReminderDelay`.

**Checked by:** `litespec model check` reports `setting-key-missing` (level G) for a setting with `setBy: operator` and no `configurationPath`.

### Give Every Tenant Setting a Change Use Case

**ID:** `litespec/core/use-cases#give-every-tenant-setting-a-change-use-case`

**Strength:** required

**Applies to:** artifact: record:setting

**Statement:** A setting with `setBy: tenant` MUST be changed by a `change-*` use case that names it in `reads`.

**Rationale:** A tenant setting holds configuration the tenant's administrator changes while the system runs. The schema admits `setBy: tenant` beside `product` and `operator`, so a tenant setting is a record, not aggregate data; without a use case that changes it, the value can never move and the default becomes a permanent figure. Naming the setting in `reads` of the `change-*` use case states who changes it and under which access, so the change has the audit and refusal handling every command carries. A product or operator setting names a configuration path instead, as [Name the Configuration Key of Every Operator Setting](#name-the-configuration-key-of-every-operator-setting) states for the operator.

**Example:** Claimwick's `setting/spending.claim-approval-limit` is set by the tenant, and `use-case/spending.change-claim-approval-limit` names it in `reads`. A tenant setting with no such use case is reported.

**Checked by:** `litespec model check` reports `tenant-setting-change-missing` (level G) for a setting with `setBy: tenant` that no `change-*` use case names in `reads`.

### Bind Every Keyed Setting in One Marked Element

**ID:** `litespec/core/use-cases#bind-every-keyed-setting-in-one-marked-element`

**Strength:** required

**Applies to:** artifact: record:setting; layer: implementation; trace: yes

**Statement:** A setting with a `configurationPath` MUST be bound in code by one element that carries an `implements` mark naming the setting and whose name spells the path.

**Rationale:** A setting reaches the running product through configuration, and the code reads it as a typed value. The code element holding that value is where the record and the configuration meet, so marking it with the setting it implements names the promise the element holds. Comparing the element name with the record path finds a renamed section or a typo before the product starts on a default nobody chose. Each stack states the shape of the element.

**Example:** Claimwick's approval-reminder-delay setting carries `configurationPath` `Spending:ApprovalReminderDelay`. One element carries an `implements` mark naming `setting/spending.approval-reminder-delay`, and its name spells the path.

**Checked by:** `litespec model check` reports `setting-unimplemented` (level S) for a keyed setting no marked element implements, and `setting-key-drift` (level S) for a marked element whose name does not spell the setting's `configurationPath`.

### State the Unit of Every Quantity Setting

**ID:** `litespec/core/use-cases#state-the-unit-of-every-quantity-setting`

**Strength:** required

**Applies to:** artifact: record:setting

**Statement:** Each setting whose value is a number or a duration MUST declare its `unit`.

**Rationale:** A number without a unit is a guess. Minutes read as seconds, and a count reads as money. Declaring the unit states the measure beside the value.

**Example:** The retry backoff declares `unit: seconds`. A timeout with `valueType: duration` and no unit is reported.

**Checked by:** `litespec model check` reports `setting-unit-missing` (level A) for a numeric or duration setting declaring no `unit`.

## Conventions

None.
