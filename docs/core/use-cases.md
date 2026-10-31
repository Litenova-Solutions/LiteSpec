# Use Cases

## Intent

Use Cases states what the system does and what it answers: the commands and queries, their verbs, inputs, effects, endings, scenarios, and the settings they read. Agents write these records from Intent, and developers review them before code carries them. The [Behavior overview](behavior.md) maps the five Behavior pages, and the [record language page](language.md) states how each field, value and record name is written.

## Concepts

### Use Cases

A use case is one goal a single test proves through the public boundary: a page beside its aggregate with one kind. A command changes the product; a query reads without changing. Each use case names the journey steps it serves in `serves`, the requirements it carries in `fulfills`, who may act in `access`, the entry points reaching it in `reachedThrough`, the aggregates it touches in `changes` and `reads`, the events it raises in `raises`, and how it ends in its results. A use case may carry an optional `risk` from money, physical-access, personal-data, and access, as the [model page](model.md) states.

| Field | Meaning |
|:---|:---|
| `access` | `{ grant, tenant }`: grant of `permission/<name>`, `self(permission/<name>)`, `anonymous`, or `system`; tenant of `own`, `cross`, `public`, or `none`. The door the call arrives through declares `auth`, as the [Access page](access.md) states. |
| `input` | `{ name, meaning, type, of, many, required, rules, origin, classification }` entries; `type` is `text`, `integer`, `decimal`, `money`, `boolean`, `date`, `date-time`, `duration`, `email`, `choice`, or `id`; `of` names the record an `id` refers to or the choices of a `choice`; `many` is true when the field holds a list of values of its type; `required` states whether the caller sends the field; origin is `caller`, `claim`, or `derived`. |
| `changes`, `reads` | Aggregate ids changed, and aggregate and setting ids read, including `store` aggregates. |
| `consistency` | `{ mode, cites }`: mode of `atomic`, `per-record`, or `compensated`. An atomic write across aggregates cites its `atomicity` rule in `cites`. |
| `effects` | `{ runs / sends / emits / calls, timing, onFailure, onExhausted, before, forEach }` entries with exactly one verb; timing is `inline`, `after-commit`, or `async`. |
| `idempotency` | `{ mode, key, scope, reason }`: mode of `key`, `state`, `not-pending`, or `none`; `key` templates `{field}` placeholders over the input field names; `scope` is `tenant`, `account`, `login`, or `global`. |
| `audit` | `{ level, target }` with level of `required`, `optional`, or `none`, or `{ exempt }` with its justification. A level of `none` is not audited; async work audits at request time. |
| `background` | The shared Given every scenario starts from, optional. |

Each consistency mode defines its promise and the effect timings it permits:

| Mode | Means | Effects allowed |
|:---|:---|:---|
| `atomic` | Every write in `changes` commits as one group | any timing |
| `per-record` | Each write commits on its own; a failure leaves earlier writes in place | `after-commit`, `async` |
| `compensated` | Each write names the undo that runs when a later write fails | `after-commit`, `async` |

Each effect entry carries the one `onFailure` shape: `{ mode, retry, then, deadLetter, surfacesIn, runbook }` with `mode` of `retry`, `skip-and-log`, or `discard`; `retry` and `then` accompany `mode: retry`, and `deadLetter`, `surfacesIn`, and `runbook` stay optional. `onExhausted` names the act when retries run out. `before` orders the entry ahead of another named target with its reason, and `forEach` names the items the entry repeats over.

Goal states the intent and What Happens walks the steps. Significant use cases carry both, as [Write Prose on Significant Use Cases](#write-prose-on-significant-use-cases) states; frontmatter plus Goal suffices elsewhere.

### Queries

A query reads without changing and declares what its readers receive. Its output groups fields into named `include` groups, each with its fields, classification, the aggregates it reads, and freshness. Freshness is an object whose `mode` names the case: `{ "mode": "immediate" }`, `{ "mode": "snapshot" }`, `{ "mode": "bounded", "within": "PT5M" }`, or `{ "mode": "advisory", "authority": "<record id>" }` naming the record holding the authoritative value and exposing the gap as a typed output field. A query whose aggregates lag reads from a read model: data shaped for the query, served without loading an aggregate. A command may declare its output as a list of `{ name, meaning }`.

| Field | Meaning |
|:---|:---|
| `output.include` | Named groups of `{ fields, classification, reads, freshness, visibleWhen, cites }`; each field carries `name` and `meaning`. |
| `visibleWhen`, `cites` | The optional visibility condition and the requirement widening visibility beyond the aggregates in `reads`. |
| `paging` | `{ "mode": "pages", order, cursor | offset, defaultLimit, maxLimit }`, or `{ "mode": "none", "reason": "..." }`. |
| `builtAsynchronously` | Present on a query whose aggregates lag, with a `{ "mode": "bounded", "within": "..." }` or `{ "mode": "advisory", "authority": "..." }` freshness and a lag scenario. |

### Use-Case Verbs

A use-case name starts with a verb, and each verb keeps one meaning and one kind across the project. LiteSpec ships the baseline vocabulary below; a project adds its domain verbs in `litespec.project.json` `vocabulary`, as [Start Every Use Case with a Declared Verb](#start-every-use-case-with-a-declared-verb) states.

| Verb | Kind | Means |
|:---|:---|:---|
| `read` | query | One record, or one composed view, by its identity. |
| `list` | query | A page of records matching a filter. |
| `search` | query | A page of records matching free text. |
| `export` | query | A generated file of many records. |
| `download` | query | The stored or rendered document of one record. |
| `answer`, `draft`, `summarize`, `translate` | either | Text a model writes for a person to review. |
| `create` | command | A new aggregate root. |
| `add`, `remove` | command | A part inside an existing record. |
| `change` | command | Data of an existing record. |
| `duplicate` | command | A new record copied from an existing one. |
| `open`, `close` | command | A period, case or shift that runs between the two. |
| `start`, `complete` | command | Work that runs to an end. |
| `publish`, `withdraw` | command | What readers or the public can see. |
| `pause`, `resume` | command | A temporary stop the owner chooses. |
| `suspend`, `reinstate` | command | A stop an operator imposes. |
| `enable`, `disable` | command | A switch. |
| `grant`, `revoke` | command | Access. |
| `request`, `approve`, `refuse` | command | A request someone else decides. |
| `issue`, `void` | command | A document or credential. |
| `retire`, `restore` | command | The end of a record's life, and its return inside the restore window. |
| `cancel` | command | An intention or booking that will not happen. |
| `expire` | command | What time ended; started by an automation. |
| `record` | command | Something that happened outside the system: a provider answer, a delivery result, a scan. |
| `send` | command | A message to a person. |
| `post`, `reverse` | command | A ledger transaction and its correction. |
| `retry`, `verify` | command | Work run again, and a check run again. |

The baseline vocabulary retires these synonyms: `get`, `fetch`, `lookup` and `count` become `read`; `browse` becomes `list`; `find` becomes `search`; `update`, `edit`, `modify`, `revise`, `set`, `adjust`, `configure` and `rename` become `change`; `define`, `compose`, `save` and `new` become `create`; `delete` becomes `remove`; `notify`, `remind` and `email` become `send`; `suggest` and `ask` become `answer`; `mark`, `note` and `log` become `record`; `end` becomes `close`; `finish` becomes `complete`; `abandon` becomes `cancel`; `lapse` becomes `expire`; `clone` and `copy` become `duplicate`; `replay` becomes `retry`; `recheck` becomes `verify`; `archive` becomes `retire`; `reject` and `deny` become `refuse`; `unpublish` and `hide` become `withdraw`.

An automation is named for its trigger, as the [Automation page](automation.md) states.

### Results and Refusals

Each use case ends in named results: child records with an `outcome` of `success` or `refusal`, a `code` on a refusal, and a `meaning`. Refusals stand in precedence order with the derived refusals first, as [Order Every Refusal List by Precedence](#order-every-refusal-list-by-precedence) states. A refusal is a rejection or error in industry words; the record word stays refusal. A code in use is retired with its replacement named and never reused, as [Retire Refusal Codes with Their Replacements](#retire-refusal-codes-with-their-replacements) states.

| Field | Meaning |
|:---|:---|
| `code`, `meaning` | The stable refusal code, such as `spending.receipt-missing`, and what it means. |
| `recorded` | Set where the attempt and its reason are stored; a validating refusal stores nothing. |
| `codes` | The closed list of recorded outcomes inside a success. |

Generic refusals for forbidden access and invalid input are derived from `access` and `input` and never written as records. A derived generic result is `result/<use-case>.forbidden` or `result/<use-case>.invalid-input`, and scenarios may point at derived results.

### Scenarios

A scenario proves one ending: a child record with `given`, `when`, `then`, an optional `examples` table, and the `result` it ends in. One test proves each scenario through the public boundary, and a proof goes stale when either of its proof hashes changes. The book groups scenarios under their result.

| Field | Meaning |
|:---|:---|
| `given`, `when`, `then` | The situation, the action, and the assertion. |
| `examples` | The worked rows reviewers agreed. |
| `result` | The written or derived result the scenario ends in. |
| `exercises` | The rules the scenario shows, optional. |

### Settings

A setting is one tunable value with its type, range, unit, setter, and bound behavior. A default or figure that is commercial, legal, or differs by tenant, market, or date fulfills a requirement; a model invariant is a rule instead. A platform-wide setting lives in the `platform` module. The `setting-set` page style holds many settings.

| Field | Meaning |
|:---|:---|
| `valueType` | `integer`, `duration`, `money`, `boolean`, `text`, or `choice`. A money setting holds an amount with an ISO 4217 currency, as the [Record Language](language.md) states. |
| `default`, `range`, `unit` | The value, the min-max or choices, and the unit. |
| `setBy` | `decider`, `operator`, or `tenant`. |
| `key` | The configuration path an operator setting binds from, such as `Spending:ApprovalReminderDelay`. Operator settings name it, as [Name the Configuration Key of Every Operator Setting](#name-the-configuration-key-of-every-operator-setting) states. |
| `atBound` | `refuse`, `truncate`, or `degrade`. |
| `secret` | Whether the value is secret. |
| `fulfills` | The requirements carried. |

## Standards

### Give Each Use Case a Command or Query Kind

**ID:** `litespec/core/use-cases#give-each-use-case-a-command-or-query-kind`

**Statement:** Each use case MUST declare one kind: command or query.

**Rationale:** The kind decides what the use case is allowed to touch: a command changes, a query reads. A use case with no kind leaves each reader to allow it everything, and the checks no shape to confirm. When-X-do-Y behavior and work without a request are automation records, as the [Automation page](automation.md) states.

**Checked by:** `litespec consumer` fails a use-case page without `kind` or with an unknown kind.

### Start Every Use Case with a Declared Verb

**ID:** `litespec/core/use-cases#start-every-use-case-with-a-declared-verb`

**Statement:** Each use case MUST start with a verb from the baseline vocabulary or the domain verbs the project declares in `litespec.project.json`.

**Rationale:** One verb per meaning keeps use cases findable: `read` versus `get` and `change` versus `update` stop competing when every project starts from one closed baseline vocabulary and adds only its domain verbs. A shared first word groups related behavior in every listing, and a retired synonym points at its replacement instead of spreading silently. When the act verb is in neither list, the author first adds it to `vocabulary.verbs` in `litespec.project.json` in the same change, or to `vocabulary.queryVerbs` for a reading verb, and then scaffolds the use case. `litespec new` scaffolds a use-case page without checking its verb, so the vocabulary addition lands before the scaffold rather than after a refusal from the scaffold command. A project never redefines a baseline verb. The `use-case-verb-unknown` check reports a use-case name starting outside the baseline vocabulary and the project verbs, and names the replacement of a retired synonym.

**Example:** The vocabulary declares `verbs: ["borrow"]` and `phrasal: ["check-in"]`, so `use-case/lending.borrow-copy` and `use-case/lending.check-in-copy` pass, `use-case/lending.list-loans` passes on the baseline vocabulary, and `use-case/lending.update-copy` is told to use `change`.

**Checked by:** `litespec model check` reports `use-case-verb-unknown` for a use-case name starting outside the baseline vocabulary and the project verbs.

### Keep One Kind per Verb

**ID:** `litespec/core/use-cases#keep-one-kind-per-verb`

**Statement:** Each use case MUST open with a verb of its own kind, or with an either verb.

**Rationale:** The first word is the first thing a reader trusts. A query named `change-...` or a command named `read-...` makes every listing lie about what the use case may touch, and a reviewer stops reading names at all. The baseline vocabulary gives each verb a kind of query, command, or either; an either verb opens either kind. A project names its own reading verbs in `queryVerbs`, and its other domain verbs open either kind. The `use-case-verb-invalid` check reports a use case whose verb names the other kind.

**Example:** `use-case/lending.list-loans` is a query and passes; `use-case/lending.answer-inquiry` opens either kind on an either verb and passes; `use-case/lending.read-overdue-count` declared as a command is reported, and so is `use-case/lending.change-copy` declared as a query.

**Checked by:** `litespec model check` reports `use-case-verb-invalid` for a query opening with a changing verb or a command opening with a reading verb.

### Name What a Use Case Acts on from the Model

**ID:** `litespec/core/use-cases#name-what-a-use-case-acts-on-from-the-model`

**Statement:** Each use case MUST name, after its verb, an aggregate, a declared part, or a glossary term.

**Rationale:** The noun groups behavior by the business record it changes; a free synonym hides which record that is and breeds a second word for the concept.

**Example:** `use-case/lending.borrow-copy` names the copy aggregate, and `use-case/sales.add-line` names the declared line part; `use-case/lending.borrow-volume` names no model word and is reported.

**Checked by:** `litespec model check` reports `use-case-noun-unknown` (level A) for a use case naming no aggregate, declared part, or glossary term after its verb.

### Name a Use Case for the Act Its Aggregate Performs

**ID:** `litespec/core/use-cases#name-a-use-case-for-the-act-its-aggregate-performs`

**Statement:** Each command use case MUST share its verb with the aggregate method its handler calls.

**Rationale:** The use case is the doorway into one aggregate act. A different word at the door and at the act means two meanings have merged, or the use case is misnamed, and each reader guesses which word tells the truth. A handler calling no aggregate method answers to no act, so queries and ports stay out of this rule. A command that records a request for work an event handler finishes after the commit, as `litespec/backend/application#call-external-systems-through-their-ports` requires, performs the request: its method starts with `Request` and passes.

**Example:** `use-case/orders.void-order` whose handler calls `Abandon` is reported; the project renames the use case to the domain word, or the method and its event to `Void` and `OrderVoidedEvent`.

**Checked by:** `litespec model check` reports `use-case-method-drift` (level A) for a command handler calling aggregate methods, none starting with the use-case verb or with `Request`.

### Connect Each Use Case to Intent

**ID:** `litespec/core/use-cases#connect-each-use-case-to-intent`

**Statement:** Each use case MUST name a journey step in `serves` or a requirement in `fulfills`.

**Rationale:** A use case serving nothing and fulfilling nothing is behavior nobody asked for: it ships without a promise behind it and stays when the promise changes. Naming a journey step or a requirement ties each behavior to the intent it carries. The `record-link-missing` check reports a use case with neither field, and `step-use-case-missing` reports a journey step no use case serves.

**Example:** The reimburse-expense command serves `journey/submit-and-reimburse#s4` and fulfills `requirement/approval.single-reimbursement`.

**Checked by:** `litespec model check` reports `record-link-missing` for a use case with neither `serves` nor `fulfills`.

### Grant Each Use Case Scoped Access

**ID:** `litespec/core/use-cases#grant-each-use-case-scoped-access`

**Statement:** Each use case MUST declare its grant and its tenant scope in `access`.

**Rationale:** Access split across prose and code is widened silently: a new caller arrives, the grant stretches, and no record shows it. Declaring the grant of `permission/<name>`, `self(permission/<name>)`, `anonymous`, or `system`, and the tenant scope of `own`, `cross`, `public`, or `none` states who may act and how far the call reaches beside the behavior. The [Access page](access.md) states the grant forms and the tenant meanings, and the door the call arrives through states how the caller proved identity. Tenant scope `cross` routes the use case to the security queue, as `litespec/core/access#route-cross-tenant-access-through-the-security-queue` states. The authorization guard enforces the grant (`litespec/backend/application#enforce-target-authorization-in-the-pipeline`), and `LS2012` reports a handler that authorizes itself.

**Example:** The reimburse-expense command declares the reimburse-expense permission with own tenant scope, reached through a session door, while the self-checkout scan declares a system grant through a device door.

**Checked by:** `litespec consumer` fails a use-case page without `access` carrying grant and tenant scope.

### Name the Entry Points That Reach Each Use Case

**ID:** `litespec/core/use-cases#name-the-entry-points-that-reach-each-use-case`

**Statement:** Each use case MUST name the entry points that reach it in `reachedThrough`.

**Rationale:** A use case with no named door is reachable from anywhere or nowhere: reviewers cannot tell a public operation from an internal one. Naming the entry points shows how callers arrive at each behavior.

**Checked by:** `litespec model check` reports `reference-unknown` for a `reachedThrough` entry naming no entry point.

### Describe Every Use Case Input with Origin and Classification

**ID:** `litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification`

**Statement:** Each use-case input MUST declare its name, meaning, type, origin, and classification.

**Rationale:** An input without an origin is trusted on faith: caller-asserted values pass as verified claims, and money moves on an unchecked figure. Declaring the name, the meaning, the type from `text`, `integer`, `decimal`, `money`, `boolean`, `date`, `date-time`, `duration`, `email`, `choice`, and `id`, the origin of `caller`, `claim`, or `derived`, and the classification states what arrives, what it is, and how far to trust it. An `id` field names the record it refers to in `of`, and a `choice` field names its choices there; other types carry no `of`. `required` states whether the caller sends the field, and a money field carries an amount with an ISO 4217 currency, as the [Record Language](language.md) states. A type on each field lets the builder compare the records with the generated contract and lets agents generate the right code first time.

**Example:**

```json
"input": [
  { "name": "expenseId", "type": "id", "of": "aggregate/spending.expense", "origin": "caller" },
  { "name": "amount", "type": "money", "rules": ["the amount is positive"], "origin": "caller" }
]
```

**Checked by:** `litespec consumer` fails a use-case input without name, meaning, or type, without `of` on an `id` or `choice` field, or with `of` on another type.

### Bound Every Caller-Supplied Input Value

**ID:** `litespec/core/use-cases#bound-every-caller-supplied-input-value`

**Statement:** Each input value with `origin: caller` on a money or access path MUST cite the rule bounding it.

**Rationale:** A caller-asserted figure on a money or access path is an open till: a replacement amount arrives negative, a quantity arrives absurd, and the code obliges. Citing the rule bounding it ties each such value to the guard holding it.

**Example:** The replacement net amount cites the positivity rule bounding it.

**Reviewed for:** Which caller-supplied money or access inputs lack a cited bounding rule? Look beside each input with `origin: caller` on a money or access path.

### Name Every Aggregate a Use Case Changes or Reads

**ID:** `litespec/core/use-cases#name-every-aggregate-a-use-case-changes-or-reads`

**Statement:** Each use case MUST name every aggregate it changes in `changes` and every aggregate it reads in `reads`.

**Rationale:** A write no record names is invisible to review: the handler touches a second aggregate, the second aggregate changes under its reviewers, and no diff shows it. Naming every touched aggregate, including `store` aggregates, states the read and write footprint beside the behavior. The `write-undeclared` check reports a write outside `changes`.

**Checked by:** `litespec model check` reports `write-undeclared` for a write outside `changes` and `reference-unknown` for an unknown aggregate.

### State the Consistency of Every Use Case

**ID:** `litespec/core/use-cases#state-the-consistency-of-every-use-case`

**Statement:** Each use case MUST declare its write consistency in `consistency`.

**Rationale:** Consistency left unsaid defaults to hope: a multi-aggregate write is read as atomic until it fails halfway. Declaring the mode states the promise: `atomic` commits every write in `changes` as one group, `per-record` commits each write on its own, and `compensated` names the undo of each write. An atomic write across aggregates cites its `atomicity` rule in `cites`, and `changes` holds exactly the aggregates the rule binds; a group above 4 aggregates is a saga candidate. Atomic consistency never spans an `async` edge. The [Domain Model page](domain-model.md) states the `atomicity` rule. The `consistency-cites-missing` check reports an atomic multi-aggregate write with no `cites`, `atomic-group-drift` reports `changes` differing from the bound group, and `atomic-group-invalid` reports a group above 4 aggregates.

**Example:** The reimburse-expense command declares atomic consistency citing the reimbursed-once rule, while the self-checkout scan declares per-record consistency across its attempts.

**Checked by:** `litespec model check` reports `consistency-cites-missing` for an atomic multi-aggregate write with no `cites`, `atomic-group-drift` for `changes` differing from the bound group, `atomic-group-invalid` (level A) for a group above 4 aggregates, and `atomic-group-duplicate` (level A) for a command saving more than one atomic write group.

### Declare Every Effect with One Verb and Its Timing

**ID:** `litespec/core/use-cases#declare-every-effect-with-one-verb-and-its-timing`

**Statement:** Each effect entry MUST carry exactly one verb and its timing.

**Rationale:** An edge no record declares is the defect: hidden calls between use cases, automations, notifications, integration events, and external systems. Declaring each edge with exactly one verb of `runs`, `sends`, `emits`, or `calls`, with its timing of `inline`, `after-commit`, or `async`, and its failure handling states every coupling as data. Timing is required on every use-case effect. On a command, a `sends` or `calls` effect carries `after-commit` or `async` timing, and an event handler runs the consequence after the commit. The `effect-undeclared` check reports an edge the records never declared.

**Example:** The reimburse-expense command emits the reimbursed integration event after commit.

**Checked by:** `litespec model check` reports `effect-undeclared` for an edge the records never declared and `effect-timing-invalid` for a command `sends` or `calls` effect with `inline` timing; `litespec consumer` fails an effect entry without exactly one verb or its timing.

### State the Transaction Sharing of Every Inline Call

**ID:** `litespec/core/use-cases#state-the-transaction-sharing-of-every-inline-call`

**Statement:** Each inline `runs` effect MUST state whether it shares the caller transaction in `sharesTransaction`.

**Rationale:** An inline call without a stated transaction is a coin toss at failure: the caller rolls back while the issued work stands, or the reverse. Stating whether the call shares the caller transaction makes the failure shape reviewable beside the edge.

**Checked by:** `litespec consumer` fails an inline `runs` effect without `sharesTransaction`.

### Keep the Effect Graph Free of Cycles

**ID:** `litespec/core/use-cases#keep-the-effect-graph-free-of-cycles`

**Statement:** The effect graph MUST hold no cycle except through a domain event carrying its end condition.

**Rationale:** A cycle without an end condition runs until something breaks: an automation runs a use case raising the event triggering the automation. Forbidding cycles except through a domain event with a stated end condition keeps every loop a loop with an exit. The `effect-loop-invalid` check reports a cycle with no stated end.

**Checked by:** `litespec model check` reports `effect-loop-invalid` for a cycle with no stated end condition.

### Name the Events Each Use Case Raises

**ID:** `litespec/core/use-cases#name-the-events-each-use-case-raises`

**Statement:** Each use case MUST name the domain events it raises in `raises`.

**Rationale:** An event raised but unnamed leaves automations waiting: downstream behavior answers a fact no behavior admits raising. Naming the raised events ties each fact to the behavior announcing it.

**Reviewed for:** Which use cases raise domain events that `raises` does not name? Look at `raises` beside each use-case page.

### Name the Idempotency of Every Use Case

**ID:** `litespec/core/use-cases#name-the-idempotency-of-every-use-case`

**Statement:** Each use case MUST declare its idempotency mode with the key, scope, or reason that mode needs.

**Rationale:** A retried call without a stated idempotency runs twice: a double payout, a double loan, a double notice. Declaring the mode of `key`, `state`, or `not-pending`, with its key template and scope, states how repeats collapse; `none` carries the reason repeats cannot happen. The `key` templates `{field}` placeholders over the record's `input` field names, and the `scope` is `tenant`, `account`, `login`, or `global`. An automation key names fields of the triggering event or run window instead, which the record does not list, so its placeholders are not checked. The self-checkout scan keys on `{nonce}` scoped to the login.

**Checked by:** `litespec consumer` fails a use case without an idempotency mode or without the key, scope, or reason its mode needs; `litespec model check` reports `record-invalid` for a key with no `{field}` placeholder, a placeholder naming no input field, or a scope outside `tenant`, `account`, `login`, and `global`.

### State the Audit Position of Every Command

**ID:** `litespec/core/use-cases#state-the-audit-position-of-every-command`

**Statement:** Each command MUST declare its audit level and target, or its exemption with its justification.

**Rationale:** An unaudited change with no exemption is a gap nobody admitted: money moves, holds lapse, and no record says who asked. Declaring the level of `required`, `optional`, or `none` with its target, or the exemption with its justification, states the audit beside the change. A level of `none` is not audited; async work audits at request time.

**Checked by:** `litespec consumer` fails a command without an audit level and target or an exemption with its justification.

### Declare Every Query Output

**ID:** `litespec/core/use-cases#declare-every-query-output`

**Statement:** Each query MUST declare its output in named `include` groups.

**Rationale:** A query without a declared output returns whatever its author fetched: readers and screens guess the shape from code. Declaring the output in named groups with fields, classifications, the aggregates each group reads, and freshness states what readers receive, so the shape is reviewable before anything implements it. The `query-output-missing` check reports a query with no declared output.

**Example:** The list-expenses query declares open and decided groups with `{ "mode": "immediate" }` freshness.

**Checked by:** `litespec model check` reports `query-output-missing` for a query with no declared output.

### Justify Every Wider Visibility

**ID:** `litespec/core/use-cases#justify-every-wider-visibility`

**Statement:** Each include group visible to more roles than the aggregates it reads MUST cite the approved requirement widening it in `cites`.

**Rationale:** A read showing more than the aggregates it reads is a quiet widening: roles see figures their own grants never allowed, and no requirement approved the view. Citing the approved requirement in `cites` keeps the widening a decided choice, and `visibleWhen` states the condition where one narrows it. The `widening-cites-missing` check reports a widened group with no cited requirement.

**Checked by:** `litespec model check` reports `widening-cites-missing` for a widened include group with no cited requirement.

### Declare Paging on Every Collection Query

**ID:** `litespec/core/use-cases#declare-paging-on-every-collection-query`

**Statement:** Each collection query MUST declare its paging with `mode` of `pages`, or `{ "mode": "none", "reason": "..." }` with its reason.

**Rationale:** A collection without paging is a promise the data outgrows: the first hundred rows return, the next ten thousand time out. Declaring the order, the cursor or offset, and the default and maximum limits with `mode` of `pages` states the paging beside the read, and `{ "mode": "none", "reason": "..." }` with its reason states why a query returns one object. The `paging-undeclared` check reports a collection query with no paging.

**Checked by:** `litespec model check` reports `paging-undeclared` for a collection query with no paging.

### Mark Every Lagging Query as Built Asynchronously

**ID:** `litespec/core/use-cases#mark-every-lagging-query-as-built-asynchronously`

**Statement:** Each query reading lagging aggregates MUST set `builtAsynchronously` with a `{ "mode": "bounded", "within": "..." }` or `{ "mode": "advisory", "authority": "..." }` freshness and a lag scenario.

**Rationale:** A read built asynchronously without a mark is trusted as fresh: screens show yesterday figures as now, and nobody warned them. Marking the query, with a `{ "mode": "bounded", "within": "..." }` or `{ "mode": "advisory", "authority": "..." }` freshness and one scenario covering the lag, states the staleness beside the promise.

**Reviewed for:** Which lagging queries lack `builtAsynchronously` with a bounded or advisory freshness and a lag scenario? Look at the queries beside the aggregates they read.

### End Every Use Case in Named Results

**ID:** `litespec/core/use-cases#end-every-use-case-in-named-results`

**Statement:** Each use case MUST declare its success result and every refusal with its code.

**Rationale:** Callers branch on named endings: success, or one refusal with its stable code. A use case with no named success leaves readers guessing what good looks like, and a refusal without a code leaves each architecture layer reporting the same breach another way. The `use-case-success-missing` check reports a use case with no success result, and `result-undeclared` reports a code the records never declared.

**Example:** The reimburse-expense command declares `reimbursed` with `spending.not-approved` and `spending.already-reimbursed`.

**Checked by:** `litespec model check` reports `use-case-success-missing` for a use case with no success result; each refusal entry on the use-case page carries its code.

### Name Each Refusal Code for the State That Refuses

**ID:** `litespec/core/use-cases#name-each-refusal-code-for-the-state-that-refuses`

**Statement:** Each refusal code MUST read `<module>.<noun>-<state>` and name the state, not the failure.

**Rationale:** The code is a public contract; `invalid`, `failed` or `error` tell an integrator nothing to do. A code naming the state tells what holds and what to change. The prefix names the module that owns the refused state, which may differ from the module of the refusing use case; a shared kernel declares its prefix in `refusals.prefixes`, and a code built at run time from a key, such as the conflict for a lost uniqueness key, declares its template in `refusals.composed`.

**Example:** `spending.receipt-missing` names the missing receipt; `spending.receipt-invalid` hides the state behind a vague word and is reported.

**Checked by:** `litespec model check` reports `refusal-code-invalid` (level A) for a refusal code outside its module, outside kebab-case, or carrying a vague word.

### Retire Refusal Codes with Their Replacements

**ID:** `litespec/core/use-cases#retire-refusal-codes-with-their-replacements`

**Statement:** A refusal code in use MUST be retired with its replacement named, never renamed or reused.

**Rationale:** The code is a public contract; clients branch on it. Renaming a code in use silently breaks them, and reusing a retired code for another state sends old clients down the new branch. Retiring the code with its replacement named keeps every past refusal readable and every new client on the new code.

**Example:** The retired `spending.receipt-missing` result names `spending.receipt-absent` as its replacement, and no later refusal reuses the old code.

**Reviewed for:** Which refusal codes in use were renamed or reused instead of retired with a named replacement? Look at the refusal codes beside their results.

### Order Every Refusal List by Precedence

**ID:** `litespec/core/use-cases#order-every-refusal-list-by-precedence`

**Statement:** Each use case MUST list its refusals so the first match wins.

**Rationale:** Two refusals matching one call need an order: without precedence, each architecture layer picks its own refusal, and callers branch on a different code per stack. The derived refusals come first, forbidden then invalid input, followed by the written refusals in the order the page lists them, so the first match is the one every architecture layer reports.

**Reviewed for:** Which refusal lists stand out of precedence order, or place a written refusal ahead of a derived one? Look at the refusal entries on each use-case page.

### Record Attempts Behind Recorded Refusals

**ID:** `litespec/core/use-cases#record-attempts-behind-recorded-refusals`

**Statement:** Each refusal carrying `recorded: true` MUST store the attempt with its reason.

**Rationale:** Some denials are forensics: a loan denial, a limit refusal, a fraud block. Storing the attempt with its reason keeps the evidence the business answers with, while a validating refusal such as a malformed code stores nothing.

**Example:** The self-checkout scan records unknown, retired, and already-lent barcodes with their reasons, while a malformed scan stores nothing.

**Reviewed for:** Which refusals carrying `recorded: true` store no attempt with its reason? Look at the recorded refusals beside their use-case pages.

### List Recorded Outcomes Inside Success Codes

**ID:** `litespec/core/use-cases#list-recorded-outcomes-inside-success-codes`

**Statement:** Each success result with recorded outcomes MUST list them in `codes`.

**Rationale:** A success hiding several recorded outcomes forces callers to guess: borrow and renew both succeed, but the due date and the analytics differ. Listing the closed codes inside the success states each recorded outcome beside the ending.

**Example:** The checked-out result lists `borrow` and `renew`.

**Reviewed for:** Which success results with recorded outcomes list no `codes`? Look at the success results beside their use-case pages.

### Derive Generic Refusals Instead of Writing Them

**ID:** `litespec/core/use-cases#derive-generic-refusals-instead-of-writing-them`

**Statement:** A use case MUST NOT write a generic forbidden or invalid-input refusal as a record.

**Rationale:** Hand-written generic refusals multiply: hundreds of pages each declare their own forbidden, and each drifts in wording and code. Deriving them from the grant in `access` and the rules in `input` keeps one forbidden and one invalid input per use case, current with the grant: the builder derives `result/<use-case>.forbidden` and `result/<use-case>.invalid-input`. Scenarios point at the derived results without anyone writing them.

**Reviewed for:** Which use-case pages write a generic forbidden or invalid-input refusal as a record? Look at the results on each use-case page beside the derived names.

### Name the Result Each Scenario Ends In

**ID:** `litespec/core/use-cases#name-the-result-each-scenario-ends-in`

**Statement:** Each scenario MUST name the result it ends in, written or derived.

**Rationale:** A scenario that names no result illustrates behavior without asserting an ending: it reads well and proves nothing. Naming the result ties each example to the ending it walks toward, so every test knows what to assert. The `scenario-result-unknown` check reports a scenario ending in an unknown result.

**Checked by:** `litespec consumer` fails a scenario entry without `result`, and `litespec model check` reports `scenario-result-unknown` for a scenario ending in an unknown result.

### Prove Every Written Result with a Scenario

**ID:** `litespec/core/use-cases#prove-every-written-result-with-a-scenario`

**Statement:** Each written result MUST be shown by at least one scenario of its use case.

**Rationale:** A written result no scenario shows is an ending nobody proved: it reads on the page and is asserted in no test, so it drifts from the code with the next change. One scenario per result pins each ending as a checked fact, including the refusals a caller can meet.

**Example:** The reimburse-expense command declares the `already-reimbursed` refusal, and a scenario walks an approved expense claimed twice to that refusal.

**Checked by:** `litespec model check` reports `result-scenario-missing`, level G for a success result and level A for a refusal.

### Write Every Scenario in Given When Then

**ID:** `litespec/core/use-cases#write-every-scenario-in-given-when-then`

**Statement:** Each scenario MUST state its `given`, `when`, and `then`.

**Rationale:** The three steps separate the situation from the action and the action from the assertion: collapsing them blurs what was true, what moved, and what the test checks. Stating all three keeps each example readable by a person and provable by a test.

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

**Checked by:** `litespec consumer` fails a scenario entry without `given`, `when`, or `then`.

### Exercise Rules from Scenarios

**ID:** `litespec/core/use-cases#exercise-rules-from-scenarios`

**Statement:** A scenario MAY name the rules it shows in `exercises`.

**Rationale:** A rule proved only through a test mark hides which example walks it: readers open the rule and find no story. Naming the shown rules in `exercises` ties each example to the rules it walks, so every rule meets its stories.

**Example:** A scenario walking a second hold on one copy names `rule/lending.one-hold-per-copy` in `exercises`.

**Reviewed for:** Which scenarios show rules without naming them in `exercises`? Look at the scenarios beside the rules they walk.

### Share Scenario Setup in Background

**ID:** `litespec/core/use-cases#share-scenario-setup-in-background`

**Statement:** A use case MAY hold the shared Given of its scenarios in `background`.

**Rationale:** Every scenario repeating the same setup buries the step that differs: readers reread one Given on each story. Holding the shared Given once in `background` leaves each scenario its own situation, action, and assertion.

**Example:** `background` holds that the employee has a session and one submitted expense awaits approval, and each approval scenario starts there.

**Reviewed for:** Which use cases repeat shared setup across scenarios instead of holding it in `background`? Look at the givens beside each use-case page.

### Write Prose on Significant Use Cases

**ID:** `litespec/core/use-cases#write-prose-on-significant-use-cases`

**Statement:** Each use case changing more than one aggregate, refusing beyond the generic refusals, carrying risk, or running effects inline MUST carry Goal and What Happens.

**Rationale:** Prose on every use case buries the significant ones: a simple read carries two sections saying what the frontmatter already says. Requiring Goal and What Happens only on significant use cases keeps prose where judgment lives, and frontmatter plus Goal suffice elsewhere. The `prose-missing` check reports a significant use case without its prose as advisory.

**Checked by:** `litespec model check` reports `prose-missing` for a significant use case without Goal and What Happens.

### Hold Every Setting Default to a Requirement

**ID:** `litespec/core/use-cases#hold-every-setting-default-to-a-requirement`

**Statement:** Each setting MUST declare its value type, range, unit, setter, and bound behavior, with the requirement behind a commercial, legal, or varying default.

**Rationale:** A default no requirement holds is a figure nobody approved: a price, a fee, a term, or a limit nobody decided. Declaring the value type, the range, the unit, the setter of decider, operator, or tenant, and the bound behavior of `refuse`, `truncate`, or `degrade` states the tunable beside its promise, and a commercial, legal, or tenant-, market-, or date-varying default cites its requirement. The `setting-requirement-missing` check reports such a default with no cited requirement.

**Example:** The receipt threshold declares money with a decider-set default of EUR 25, refusing an expense above the line with no receipt, behind its requirement.

**Checked by:** `litespec model check` reports `setting-requirement-missing` for a commercial, legal, or varying default with no cited requirement.

### Name the Settings Each Use Case Reads

**ID:** `litespec/core/use-cases#name-the-settings-each-use-case-reads`

**Statement:** Each use case MUST name the settings it reads in `reads`.

**Rationale:** A setting read by a use case no record names is a coupling reviewers cannot see: the tunable moves, the reader breaks, and no diff shows the pair. Naming the settings in `reads` beside the use case states the coupling where the reader lives, so each change to the range or default meets its readers. The setting's readers are computed as the inverse, never written. Settled by [decision/0012-use-cases-read-their-settings](../intent/decisions/0012-use-cases-read-their-settings.md) on the evidence in [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence.md) and [research/field-naming-evidence](../intent/research/field-naming-evidence.md).

**Example:** The submit-expense use case names `setting/spending.receipt-threshold` in `reads`, so a move of the threshold meets the submission story.

**Reviewed for:** Which use cases read a setting that `reads` does not name? Look at the use cases beside the settings they read.

### Name the Configuration Key of Every Operator Setting

**ID:** `litespec/core/use-cases#name-the-configuration-key-of-every-operator-setting`

**Statement:** A setting the operator sets MUST name its configuration path in `key`.

**Rationale:** An operator setting reaches the product through configuration, so the record and the configuration meet at one path: the record states the default and the bounds, and the configuration names the value. Without the key, a renamed section silently falls back to a default nobody chose, and no reader can tell which configuration value a promise rests on. A setting a tenant or the decider sets is held as data by an aggregate instead, and names no key.

**Example:** Claimwick's `setting/spending.approval-reminder-delay` is set by the operator and carries `key` `Spending:ApprovalReminderDelay`.

**Checked by:** `litespec model check` reports `setting-key-missing` (level G) for a setting with `setBy: operator` and no `key`.

### Bind Every Keyed Setting in One Marked Options Class

**ID:** `litespec/core/use-cases#bind-every-keyed-setting-in-one-marked-options-class`

**Statement:** A setting with a `key` MUST be bound by an options class that carries an `implements` mark naming the setting and whose section and property spell the key.

**Rationale:** The options class is where configuration becomes a typed value the code reads, so it is the code element that implements the setting. Marking it names the promise the class holds, and comparing its section name and property with the record's key finds the renamed section or the typo before the product starts with a default.

**Example:** Claimwick's `SpendingOptions` declares `SectionName = "Spending"`, an `ApprovalReminderDelay` property, and `/// <implements>setting/spending.approval-reminder-delay</implements>`.

**Checked by:** `litespec model check` reports `setting-unimplemented` (level S) for a keyed setting no options class marks, and `setting-key-drift` (level S) for a marked options class whose `SectionName` constant and property do not spell the setting's `key`.

### State the Unit of Every Quantity Setting

**ID:** `litespec/core/use-cases#state-the-unit-of-every-quantity-setting`

**Statement:** Each setting whose value is a number or a duration MUST declare its `unit`.

**Rationale:** A number without a unit is a guess: minutes read as seconds, a count reads as money. Declaring the unit states the measure beside the value.

**Example:** The retry backoff declares `unit: seconds`; a timeout with `valueType: duration` and no unit is reported.

**Checked by:** `litespec model check` reports `setting-unit-missing` (level A) for a numeric or duration setting declaring no `unit`.

## Conventions

None.
