# Use Cases

**Stack:** any
**Concern:** records

Use Cases states what the system does and what it answers: the commands and queries, their verbs, inputs, effects, endings, scenarios, and the settings they read. Agents write these records from Intent, and code carries them. The [Behavior overview](behavior.md) maps the five Behavior pages, and [Record Language](language.md) states how each field, value, and record name is written.

## Concepts

### Use Cases

A use case is one goal that a single test proves through the public boundary: a page beside its aggregate with one kind. A command is a use case that changes the product. A query is a use case that reads without changing it.

Each use case links back to the Intent it carries: the flow steps it serves in `serves`, and the requirements it carries in `fulfills`. It states who may act in `access`, and the entry points reaching it in `reachedThrough`. It names the aggregates and read models it touches in `writes` and `reads`, the events it raises in `raises`, and how it ends in its results.

| Field | Meaning |
|:---|:---|
| `access` | `{ grant, tenant }`. The grant is `permission/<name>`, `self(permission/<name>)`, `signed-in`, `anonymous`, or `system`. The tenant scope is `own`, `cross`, `public`, or `none`. The entry point the call arrives through declares `auth`, as [Access](access.md) states. |
| `input` | Entries of `{ name, meaning, type, of, choices, many, required, rules, origin, verifiedBy, classification, contentTypes, maxSize }`. The `type` is `text`, `integer`, `decimal`, `money`, `boolean`, `date`, `date-time`, `duration`, `email`, `choice`, `file`, `list`, or `id`. The `of` field names the record an `id` refers to, or the row shape of a `list`. The `choices` field names the allowed values of a `choice`. A `file` field carries `contentTypes` with its media types and `maxSize` with its limit. A `list` field carries in `of` the row shape with each row field's name, type, required flag, and meaning, with `minItems` and the `unique` key fields. The `many` flag is true when the field holds a list of values of its type. The `required` flag states whether the caller sends the field. The `rules` field names the `rule/` identifiers bounding the value. The origin is `caller`, `claim`, `verified-claim`, or `derived`. A `verified-claim` input names the entry point or external system that validated it in `verifiedBy`. |
| `writes`, `reads` | The ids of aggregates written, and the ids of aggregates, read models, and settings read, including `store` aggregates. A read model is the module's own copy of another module's state, as [Automation](automation.md) states. |
| `consistency` | `{ mode, boundBy }`, on a command writing two or more aggregates only. The mode is `atomic` or `per-record`. An atomic write across aggregates cites its `atomicity` rule in `boundBy`. A query carries no `consistency`. |
| `effects` | Entries of `{ sends / calls, timing, onFailure, onExhausted, before, forEach }` with exactly one verb. The timing is `inline`, `after-commit`, or `async`, and only a query's `calls` may be `inline`. A use case runs no other use case and declares no integration event. A reaction runs what follows, and an outbound integration event names the event it translates. |
| `idempotency` | `{ mode, key, scope, reason, replay, recognizedBy }`, on a command only. The mode is `key`, `state`, or `none`. The `key` templates `{field}` placeholders over the input field names. The `scope` is `tenant`, `account`, `login`, or `global`. The `replay` is `returns-first-result`, `refuses`, or `acts-again`. Mode `state` also carries `recognizedBy` naming the state checked. A query carries no `idempotency`. |
| `audit` | `{ level, target }` with level `required`, `optional`, or `none`, or `{ exemptBecause }` with its justification. A level of `none` is not audited. Async work audits at request time. |
| `background` | The optional Given shared by every scenario of the use case. A use case carrying `background` holds at least two scenarios. |

Each consistency mode defines its promise for the writes of one use case:

| Mode | Means |
|:---|:---|
| `atomic` | Every write in `writes` commits as one group |
| `per-record` | Each write commits on its own; a failure leaves earlier writes in place |

No mode reaches past the use case. Every edge to another record runs after the commit. A command writing one aggregate is atomic by definition, so it carries no `consistency`.

Each effect entry carries the one `onFailure` shape: `{ mode, retry, ifExhausted, deadLetter, surfacesIn, runbook }`. The `mode` is `retry`, `skip-and-log`, or `discard`. The `retry` and `ifExhausted` fields accompany `mode: retry`, while `deadLetter`, `surfacesIn`, and `runbook` stay optional. The `onExhausted` field names the act when retries run out. The `before` field orders the entry ahead of another named target with its reason, and `forEach` names the items the entry repeats over.

The `summary` field states what the use case is, in one paragraph. Each use case carries `summary`. A command describes its steps under Flow in its explanation file, as [Summarize Every Use Case](#summarize-every-use-case) states. The `exposure` field states in plain words who reaches the use case, through which entry points, under which permission, and what is at stake if it goes wrong. Example and Notes live in the explanation file beside the record file.

### Queries

A query reads without changing, and declares what its readers receive. Its output groups fields into named `include` groups. Each group names its fields, its classification, the aggregates it reads, and its freshness.

Freshness is `{ mode, within, decidedBy }`. The `mode` is `immediate`, `snapshot`, or `eventual`. The `within` field is the ISO 8601 duration a lagging read stays behind its source, present when the mode allows lag. The `decidedBy` field names the command use case or aggregate that decides the figure at write time. It appears only with `snapshot` or `eventual` freshness, when a command re-decides the figure.

A query whose aggregates lag reads from a read model: data shaped for the query, served without loading an aggregate. A command may declare its output as a list of `{ name, meaning, type }`.

| Field | Meaning |
|:---|:---|
| `output.include` | Named groups of `{ fields, classification, reads, freshness, visibleWhen, widenedBy }`. Each field carries `name`, `meaning`, and `type`. |
| `visibleWhen`, `widenedBy` | The optional visibility condition, and the requirement widening visibility beyond the aggregates in `reads`. |
| `paging` | `{ "mode": "pages", order, cursor | offset, defaultLimit, maxLimit }`, or `{ "mode": "none", "reason": "..." }`. |

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

The baseline vocabulary retires these synonyms: `get`, `fetch`, `lookup`, and `count` become `read`; `browse` becomes `list`; `find` becomes `search`; `update`, `edit`, `modify`, `revise`, `set`, `adjust`, `configure`, and `rename` become `change`; `define`, `compose`, `save`, and `new` become `create`; `delete` becomes `remove`; `notify`, `remind`, and `email` become `send`; `suggest` and `ask` become `answer`; `mark`, `note`, and `log` become `record`; `end` becomes `close`; `finish` becomes `complete`; `abandon` becomes `cancel`; `lapse` becomes `expire`; `clone` and `copy` become `duplicate`; `replay` becomes `retry`; `recheck` becomes `verify`; `archive` becomes `retire`; `reject` and `deny` become `refuse`; `unpublish` and `hide` become `withdraw`.

A verb term names the verb in `term`, the kind it opens in `verbKind`, and the act in `meaning`, in one sentence, as any term does.

```yaml
- id: term/borrow
  term: borrow
  meaning: The loan of one copy to one reader.
  layer: behavior
  verbKind: command
```

A reaction takes its name from its trigger, and a job from the use case it runs, as [Automation](automation.md) states.

### Results and Refusals

Each use case ends in named results. A result is a child record with an `outcome` of `success` or `refusal`, a `code` on a refusal, and a `meaning`.

Each module declares its refusal codes once in `behavior/modules/<m>/refusals.yaml`, as [Declare Each Module's Refusals Once](#declare-each-modules-refusals-once) states. A result's `code` names a code from its module's set. A rule's `refusal` names a code from the same set, and every result carrying that code is the rule's refusal in that use case.

Refusals stand in precedence order with the derived refusals first, as [Order Every Refusal List by Precedence](#order-every-refusal-list-by-precedence) states. A refusal is a rejection or error in industry words; the record word stays refusal. A code in use is retired with its replacement named and never reused, as [Retire Refusal Codes with Their Replacements](#retire-refusal-codes-with-their-replacements) states.

| Field | Meaning |
|:---|:---|
| `code`, `meaning` | The stable refusal code from the module's refusal set and what it means, such as `spending.receipt-missing`. |
| `recorded` | Set where the attempt and its reason are stored. A validating refusal stores nothing. |
| `codes` | The closed list of recorded outcomes inside a success result. |

The generic refusals are derived and never written as records. The `forbidden` refusal comes from the grant in `access`, `invalid-input` from the rules in `input`, `not-found` from the named target, `conflict` from the state the call met, `unauthenticated` from the missing caller, and `sign-in-too-old` from the recent-sign-in window [Access](access.md) states. A derived generic result takes the form `result/<use-case>.<name>`, and scenarios may point at derived results.

### Scenarios

A scenario proves one ending of the behavior it exercises. It is an entry in the file of its use case, and that use case is its parent. A reaction or job scenario is an entry in the file of its reaction or job, as [Automation](automation.md) states.

The scenario carries `given`, `when`, `then`, an optional `examples` table, and the `result` it ends in. One test proves each scenario through the public boundary. A proof goes stale when either of its proof hashes changes. The book groups a use case's scenarios under their result.

| Field | Meaning |
|:---|:---|
| `given`, `when`, `then` | The situation, the action, and the assertion, all on the scenario's own operation. |
| `examples` | The worked example rows, all with one shared set of keys. |
| `result` | The written or derived result the scenario ends in. |
| `exercises` | The optional rules the scenario shows. |

### Settings

A setting is one tunable value with its type, range, unit, setter, and bound behavior.

A default or figure that is commercial, legal, or differs by tenant, market, or date fulfills a requirement. A model invariant is a rule instead. A product-wide setting lives at product level.

The `setting-set` page style holds many settings. A setting set carries `summary`, and its explanation file holds Notes. A use case, reaction, or job names the settings it reads in `reads`.

| Field | Meaning |
|:---|:---|
| `valueType` | One of `integer`, `duration`, `money`, `boolean`, `text`, or `choice`. A money setting holds an amount with an ISO 4217 currency, as the [Record Language](language.md) states. |
| `default`, `range`, `unit` | The default in the setting's `valueType`, the min-max or choices, and the unit. A money default holds an amount with its currency, a duration default holds an ISO 8601 duration, and a choice default names one of its `choices`. |
| `dated` | `true` when each value applies from a date and never changes a posted record. |
| `setBy` | `product`, `operator`, or `tenant`. |
| `key` | The configuration path an operator setting binds from, such as `Spending:ApprovalReminderDelay`. Operator settings name it, as [Name the Configuration Key of Every Operator Setting](#name-the-configuration-key-of-every-operator-setting) states. |
| `atBound` | `refuse`, `truncate`, or `degrade`. |
| `secret` | Whether the value is secret. |
| `fulfills` | The requirements the setting carries. |

## Standards

### Give Each Use Case a Command or Query Kind

**ID:** `litespec/core/use-cases#give-each-use-case-a-command-or-query-kind`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use case MUST declare one kind: command or query.

**Rationale:** A use case with no kind leaves each reader to allow it everything, and the checks with no shape to confirm. The kind decides what the use case may touch: a command changes, a query reads. When-X-do-Y behavior and work without a request are reaction and job records, as [Automation](automation.md) states.

**Checked by:** `litespec model check` reports `record-invalid` for a use-case page without `kind` or with an unknown kind.

### Start Every Use Case with a Declared Verb

**ID:** `litespec/core/use-cases#start-every-use-case-with-a-declared-verb`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use case MUST start with a verb from the baseline vocabulary or a verb term the project declares in its glossary.

**Rationale:** Without one verb per meaning, `read` versus `get` and `change` versus `update` compete in every listing, and related behavior scatters. Starting from one closed baseline vocabulary and adding only declared verbs keeps use cases findable. A shared first word groups related behavior, and a retired synonym points at its replacement instead of spreading silently. When the act verb is in neither list, the author first declares it as a glossary verb term in the same change, then scaffolds the use case. The `litespec new` command scaffolds a use-case page without checking its verb, so the verb term lands before the scaffold rather than after a refusal from the scaffold command. A project never redefines a baseline verb. Proposed by [decision/0062-declare-project-verbs-in-the-glossary](decision/0062-declare-project-verbs-in-the-glossary).

**Example:** The project declares `borrow` and `check-in` as glossary verb terms. The names `use-case/lending.borrow-copy` and `use-case/lending.check-in-copy` pass, `use-case/lending.list-loans` passes on the baseline vocabulary, and `use-case/lending.update-copy` is told to use `change`.

**Checked by:** `litespec model check` reports `use-case-verb-unknown` for a use-case name opening with a verb neither list declares, and `term-verb-duplicate` for a glossary verb term restating a baseline verb.

### Keep One Kind per Verb

**ID:** `litespec/core/use-cases#keep-one-kind-per-verb`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use case MUST open with a verb of its own kind, or with an either verb.

**Rationale:** A query named `change-...` or a command named `read-...` makes every listing lie about what the use case may touch. The first word is the first thing a reader trusts, so a lying first word teaches readers to trust no name. The baseline vocabulary gives each verb a kind of query, command, or either, and an either verb opens either kind. A project declares the kind of each of its verbs in its glossary verb term. Proposed by [decision/0062-declare-project-verbs-in-the-glossary](decision/0062-declare-project-verbs-in-the-glossary).

**Example:** `use-case/lending.list-loans` is a query and passes. The name `use-case/lending.answer-inquiry` opens either kind on an either verb and passes. The name `use-case/lending.read-overdue-count` declared as a command is reported, and so is `use-case/lending.change-copy` declared as a query.

**Checked by:** `litespec model check` reports `use-case-verb-invalid` for a use case opening with a verb of the other kind.

### Name What a Use Case Acts on from the Model

**ID:** `litespec/core/use-cases#name-what-a-use-case-acts-on-from-the-model`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use case MUST name, after its verb, an aggregate, a declared part, or a glossary term.

**Rationale:** A free synonym after the verb hides which business record the use case changes, and breeds a second word for the concept. The noun after the verb groups behavior by the record it changes.

**Example:** `use-case/lending.borrow-copy` names the copy aggregate, and `use-case/sales.add-line` names the declared line part. The name `use-case/lending.borrow-volume` names no model word and is reported.

**Reviewed for:** Does every use-case name carry an aggregate, a declared part, or a glossary term after its verb? Look at the words after the verb in each use-case name.

### Name a Use Case for the Act Its Aggregate Performs

**ID:** `litespec/core/use-cases#name-a-use-case-for-the-act-its-aggregate-performs`

**Strength:** required

**Applies to:** artifact: record:use-case; when: command

**Statement:** Each command use case MUST share its verb with the aggregate method its handler calls.

**Rationale:** A different word in the use-case name and in the act means two meanings have merged, or the use case is misnamed. Each reader then guesses which word tells the truth. The use case names one aggregate act, so the two share their verb. A handler calling no aggregate method answers to no act, so queries and ports stay out of this rule. A command that records a request for work an event handler finishes after the commit, as `litespec/backend/application#call-external-systems-through-their-ports` requires, performs the request, so its method starts with `Request` and passes.

**Example:** `use-case/orders.void-order` whose handler calls `Abandon` is reported. The project renames the use case to the domain word, or the method and its event to `Void` and `OrderVoidedEvent`.

**Reviewed for:** Does every aggregate method a command handler calls start with the use-case verb or `Request`? Look at the methods each command handler calls.

### Connect Each Use Case to Intent

**ID:** `litespec/core/use-cases#connect-each-use-case-to-intent`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use case MUST name a flow step in `serves` or a requirement in `fulfills`.

**Rationale:** A use case serving nothing and fulfilling nothing is behavior nobody asked for: it ships without a promise behind it and stays when the promise changes. Naming a flow step or a requirement ties each behavior to the intent it carries. The `record-link-missing` check reports a use case with neither field, and `step-use-case-missing` reports a flow step no use case serves.

**Example:** The reimburse-expense command serves the flow step `flow/submit-and-reimburse#s4` and fulfills `requirement/approval.expense-reimbursed-at-most-once`.

**Checked by:** `litespec model check` reports `record-link-missing` for a use case with neither `serves` nor `fulfills`.

### Grant Each Use Case Scoped Access

**ID:** `litespec/core/use-cases#grant-each-use-case-scoped-access`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use case MUST declare its grant and its tenant scope in `access`.

**Rationale:** Access split across prose and code is widened silently: a new caller arrives, the grant stretches, and no record shows it. Declaring the grant beside the behavior states who may act and how far the call reaches. The grant is `permission/<name>`, `self(permission/<name>)`, `signed-in`, `anonymous`, or `system`, with the tenant scope `own`, `cross`, `public`, or `none`. [Access](access.md) states the grant forms and the tenant meanings, and the entry point the call arrives through states how the caller proved identity. The authorization guard enforces the grant (`litespec/backend/application#enforce-target-authorization-in-the-pipeline`), and `LS2012` reports a handler that authorizes itself.

**Example:** The reimburse-expense command declares the reimburse-expense permission with own tenant scope, reached through a session entry point. The self-checkout scan declares a system grant through a device entry point.

**Checked by:** `litespec model check` reports `record-invalid` for a use-case page without `access` carrying grant and tenant scope.

### Name the Entry Points That Reach Each Use Case

**ID:** `litespec/core/use-cases#name-the-entry-points-that-reach-each-use-case`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use case a caller reaches from outside MUST name the entry points that reach it in `reachedThrough`.

**Rationale:** A use case with no named entry point is reachable from anywhere or nowhere. Readers cannot tell a public operation from an internal one. Naming the entry points shows how callers arrive at each behavior. A use case that only a reaction or a job runs has no entry point of its own, and the `runs` edge of that reaction or job already shows how it is reached.

**Checked by:** `litespec model check` reports `record-invalid` for a live use case that names no entry point and that nothing runs, and `reference-unknown` for a `reachedThrough` entry naming no entry point.

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

**Rationale:** An input without an origin is trusted on faith: caller-asserted values pass as verified claims, and money moves on an unchecked figure. Declaring the name, the meaning, the type, the origin, and the classification states what arrives, what it is, and how far to trust it. The type comes from `text`, `integer`, `decimal`, `money`, `boolean`, `date`, `date-time`, `duration`, `email`, `choice`, `file`, `list`, and `id`. The origin is `caller`, `claim`, `verified-claim`, or `derived`. An `id` field names the record it refers to in `of`, a `choice` field names its allowed values in `choices`, a `file` field names its media types in `contentTypes` with its limit in `maxSize`, and a `list` field names its row shape in `of`. Other types carry no `of` and no `choices`. A `verified-claim` input names the entry point or external system that validated it in `verifiedBy`, as [Mark Every Provider-Verified Input](#mark-every-provider-verified-input) states. The `rules` field names the `rule/` identifiers bounding the value, and `required` states whether the caller sends the field. A money field carries an amount with an ISO 4217 currency, as [Record Language](language.md) states. A type on each field lets the builder compare the records with the generated contract and lets agents generate the right code first time. Settled by [decision/0016-typed-contracts-and-privacy](decision/0016-typed-contracts-and-privacy) on the evidence in [research/lower-layers-practice](research/lower-layers-practice).

**Example:**

```json
"input": [
  { "name": "expenseId", "type": "id", "of": "aggregate/spending.expense", "origin": "caller" },
  { "name": "amount", "type": "money", "rules": ["rule/spending.positive-amount"], "origin": "caller" },
  { "name": "lines", "type": "list", "of": { "fields": [{ "name": "amount", "type": "money", "required": true, "meaning": "The line amount." }], "minItems": 1 }, "origin": "caller" }
]
```

**Checked by:** `litespec model check` reports `record-invalid` for a use-case input without name, meaning, or type, without `of` on an `id` or `list` field, without `choices` on a `choice` field, or with `of` or `choices` on another type.

### Bound Every Caller-Supplied Input Value

**ID:** `litespec/core/use-cases#bound-every-caller-supplied-input-value`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each input value with `origin: caller` on a money or access path MUST cite the rule bounding it.

**Rationale:** A caller-asserted figure on a money or access path arrives unchecked: a replacement amount arrives negative, a quantity arrives absurd, and the code obliges. Citing the rule bounding it ties each such value to the guard holding it.

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

**Rationale:** A write no record names is invisible: the handler touches a second aggregate, the second aggregate changes, and no diff shows it. Naming every touched aggregate, including `store` aggregates, states the read and write footprint beside the behavior. The `write-undeclared` check reports a write outside `writes`.

**Checked by:** `litespec model check` reports `write-undeclared` for a write outside `writes` and `reference-unknown` for an unknown aggregate.

### Declare Consistency on Multi-Aggregate Commands

**ID:** `litespec/core/use-cases#declare-consistency-on-multi-aggregate-commands`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each command writing two or more aggregates MUST declare its write consistency in `consistency`.

**Rationale:** Consistency left unsaid is read as atomic until a multi-aggregate write fails halfway. Declaring the mode states the promise: `atomic` commits every write in `writes` as one group, and `per-record` commits each write on its own. An atomic write across aggregates cites its `atomicity` rule in `boundBy`, and `writes` holds exactly the aggregates the rule binds. A group above 4 aggregates is reported. Atomic consistency never spans an `async` edge. A command writing one aggregate is atomic by definition and carries no `consistency`, and a query carries none. [Domain Model](domain-model.md) states the `atomicity` rule. The `consistency-cites-missing` check reports an atomic multi-aggregate write with no `boundBy`, `atomicity-drift` reports `writes` differing from the bound group, and `atomicity-invalid` reports a group above 4 aggregates. Settled by [decision/0076-remove-shapes-nothing-needs](decision/0076-remove-shapes-nothing-needs).

**Example:** The reimburse-expense command declares atomic consistency citing the reimbursed-once rule. The self-checkout scan declares per-record consistency across its attempts.

**Checked by:** `litespec model check` reports `consistency-cites-missing` for an atomic multi-aggregate write with no `boundBy`, `atomicity-drift` for `writes` differing from the bound group, `atomicity-invalid` (level A) for a group above 4 aggregates, and `atomicity-duplicate` (level A) for a command saving more than one atomic write group; `litespec model check` reports `record-invalid` for a query carrying `consistency`.

### Declare Every Effect with One Verb and Its Timing

**ID:** `litespec/core/use-cases#declare-every-effect-with-one-verb-and-its-timing`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each effect entry MUST carry exactly one verb and its timing.

**Rationale:** An edge no record declares stays hidden: calls between use cases, reactions, jobs, notifications, integration events, and external systems that no reader can see. Declaring each edge with exactly one verb, its timing, and its failure handling turns every coupling into data. The verb is `sends` or `calls`. The timing is `inline`, `after-commit`, or `async`, and timing is required on every use-case effect. On a command, a `sends` or `calls` effect carries `after-commit` or `async` timing, and an event handler runs the consequence after the commit. A use case declares no `runs` and no `emits`. A reaction to its raised fact runs what follows, and the integration events it publishes follow from `raises` and the `translates` field of each outbound integration event. The `effect-undeclared` check reports an edge the records never declared.

**Example:** The reimburse-expense command declares no effect. It raises its reimbursed fact, the expense-reimbursed integration event translates that fact, and the reporting module's read model answers the published event.

**Checked by:** `litespec model check` reports `effect-undeclared` for an edge the records never declared and `effect-timing-invalid` for a command `sends` or `calls` effect with `inline` timing; `litespec model check` reports `record-invalid` for an effect entry without exactly one verb or its timing.

### Run Follow-On Work Only from Reactions and Jobs

**ID:** `litespec/core/use-cases#run-follow-on-work-only-from-reactions-and-jobs`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A use case MUST NOT run another use case, so each follow-on act runs from a reaction to the fact the use case raised, or from a job.

**Rationale:** A use case that runs another use case nests one command inside another. The inner command rides on the outer commit. Its failure rolls back work that had no reason to fail, and its record no longer states what it commits. A reaction to the raised fact runs the follow-on act in its own transaction, with its own failure handling and retry, so each record states one unit of work. The caller learns the outcome from the later fact instead of from the call. Settled by [decision/0064-raise-facts-and-send-nothing-from-handlers](decision/0064-raise-facts-and-send-nothing-from-handlers).

**Example:** In the library product, borrowing a copy raises the loan-borrowed fact. The send-due-notice reaction answers it in its own transaction, instead of the borrow command sending the notice.

**Checked by:** `litespec model check` reports `record-invalid` for a use-case effect that names `runs` or `emits`.

### Keep the Effect Graph Free of Cycles

**ID:** `litespec/core/use-cases#keep-the-effect-graph-free-of-cycles`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The effect graph MUST hold no cycle except through a domain event carrying its end condition.

**Rationale:** A cycle without an end condition runs until something breaks: a reaction runs a use case raising the event that triggers the reaction. Allowing cycles only through a domain event with a stated end condition keeps every loop a loop with an exit. The `effect-loop-invalid` check reports a cycle with no stated end.

**Checked by:** `litespec model check` reports `effect-loop-invalid` for a cycle with no stated end condition.

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

**Statement:** Each command MUST declare its idempotency mode with the key, scope, or reason that mode needs.

**Rationale:** A retried call without a stated idempotency runs twice: a double payout, a double loan, a double notice. Declaring the mode states how repeats collapse. The mode is `key`, `state`, or `none`, with its key template and scope. Mode `none` carries the reason repeats cannot happen. The `key` templates `{field}` placeholders over the record's `input` field names, and the `scope` is `tenant`, `account`, `login`, or `global`. A query carries no `idempotency`, since a read is idempotent by kind. A reaction or job carries its own `idempotency`, as [Automation](automation.md) states. The self-checkout scan keys on `{nonce}` scoped to the login.

**Checked by:** `litespec model check` reports `record-invalid` for a command without an idempotency mode or without the key, scope, or reason its mode needs; `litespec model check` reports `record-invalid` for a key with no `{field}` placeholder, a placeholder naming no input field, or a scope outside `tenant`, `account`, `login`, and `global`; `litespec model check` reports `record-invalid` for a query carrying `idempotency`.

### State What a Replay Does

**ID:** `litespec/core/use-cases#state-what-a-replay-does`

**Strength:** required

**Applies to:** artifact: record:use-case; when: command

**Statement:** Each command carrying `idempotency` MUST state what a recognized replay does in `replay` and how the command recognizes it.

**Rationale:** A repeat the command recognizes but handles nowhere either runs twice or fails without telling why: a double payout, or a refusal that hides the first success. Stating `returns-first-result`, `refuses`, or `acts-again` tells the caller what the repeat brings. A keyed command recognizes the repeat by the key the caller sends. A state command names the state it checks in `recognizedBy`, such as the posted entry the second booking meets.

**Example:** The reimburse-expense command replays with `returns-first-result`. A retried payout returns the first reimbursement instead of posting again.

**Checked by:** `litespec model check` reports `idempotency-replay-missing` for a command `idempotency` without `replay`, or mode `state` without `recognizedBy`.

### State the Audit Position of Every Command

**ID:** `litespec/core/use-cases#state-the-audit-position-of-every-command`

**Strength:** required

**Applies to:** artifact: record:use-case; when: command

**Statement:** Each command MUST declare its audit level and target, or its exemption with its justification in `exemptBecause`.

**Rationale:** An unaudited change with no exemption is a gap nobody admitted: money moves, holds lapse, and no record says who asked. Declaring the level with its target, or the exemption with its justification, states the audit beside the change. The level is `required`, `optional`, or `none`, and the exemption carries its justification in `exemptBecause`. A level of `none` is not audited, and async work audits at request time.

**Checked by:** `litespec model check` reports `record-invalid` for a command without an audit level and target or an exemption with its justification in `exemptBecause`.

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

**Rationale:** A read showing more than the aggregates it reads widens quietly: roles see figures their own grants never allowed, and no requirement states the view. Citing the requirement in `widenedBy` keeps the widening a decided choice. The `visibleWhen` field states the condition where one narrows it. The `widening-cites-missing` check reports a widened group with no cited requirement.

**Checked by:** `litespec model check` reports `widening-cites-missing` for a widened include group with no cited requirement.

### Declare Paging on Every Collection Query

**ID:** `litespec/core/use-cases#declare-paging-on-every-collection-query`

**Strength:** required

**Applies to:** artifact: record:use-case; when: query

**Statement:** Each collection query MUST declare its paging with `mode` of `pages`, or `{ "mode": "none", "reason": "..." }` with its reason.

**Rationale:** A collection without paging is a promise the data outgrows: the first hundred rows return, and the next ten thousand time out. Declaring the order, the cursor or offset, and the default and maximum limits with `mode` of `pages` states the paging beside the read. The value `{ "mode": "none", "reason": "..." }` with its reason states why a query returns one object. The `paging-undeclared` check reports a collection query with no paging.

**Checked by:** `litespec model check` reports `paging-undeclared` for a collection query with no paging.

### Type Every Output Field

**ID:** `litespec/core/use-cases#type-every-output-field`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each output field MUST carry its value type.

**Rationale:** An output field without a type returns whatever its author fetched. Readers and screens guess the shape from code. Carrying the value type from the input types on every field of an `include` group and of a command output states the shape beside the promise, so the builder compares the records with the generated contract. A list of rows takes `type: list` with its row shape in `of`, as the input types state. Settled by [decision/0073-type-every-output-and-declare-refusal-sets](decision/0073-type-every-output-and-declare-refusal-sets).

**Example:** The list-expenses query types its amount field `money`. It types its lines field `list` with the row shape in `of`.

**Checked by:** `litespec model check` reports `record-invalid` for an output field without `type`.

### Declare Each Module's Refusals Once

**ID:** `litespec/core/use-cases#declare-each-modules-refusals-once`

**Strength:** required

**Applies to:** artifact: record:refusal-set

**Statement:** Each module MUST declare its refusal codes once in `behavior/modules/<m>/refusals.yaml` with each code's meaning and problem type.

**Rationale:** Refusal codes declared per use case drift: one use case retires a code while its neighbor keeps it, and the same breach reports two codes. Declaring each code once per module, with its meaning and its RFC 9457 problem type, keeps one code behind every result citing it. The code reads `<module>.<name>`. The problem type is a URI or a short type name. A result's `code` names a code from its module's set, and a rule's `refusal` names the same code, so every result carrying it is that rule's refusal in its use case. Settled by [decision/0073-type-every-output-and-declare-refusal-sets](decision/0073-type-every-output-and-declare-refusal-sets).

**Example:** The spending module declares `spending.receipt-missing` with its meaning and its problem type. The submit-expense refusal cites that code.

**Checked by:** `litespec model check` reports `refusal-code-unknown` for a result code missing from its module's refusal set.

### End Every Use Case in Named Results

**ID:** `litespec/core/use-cases#end-every-use-case-in-named-results`

**Strength:** required

**Applies to:** artifact: record:use-case, record:result

**Statement:** Each use case MUST declare its success result and every refusal with its code.

**Rationale:** Callers branch on named endings: success, or one refusal with its stable code. Without a named success, readers guess what good looks like. Without a code on a refusal, each architecture layer reports the same breach another way. The `use-case-success-missing` check reports a use case with no success result, and `result-undeclared` reports a code the records never declared.

**Example:** The reimburse-expense command declares `reimbursed`, with the refusals `spending.not-approved` and `spending.already-reimbursed`.

**Checked by:** `litespec model check` reports `use-case-success-missing` for a use case with no success result; each refusal entry on the use-case page carries its code.

### Name Each Refusal Code for the State That Refuses

**ID:** `litespec/core/use-cases#name-each-refusal-code-for-the-state-that-refuses`

**Strength:** required

**Applies to:** artifact: record:result

**Statement:** Each refusal code MUST read `<module>.<noun>-<state>` and name the state, not the failure.

**Rationale:** The code is a public contract. The words `invalid`, `failed`, or `error` tell an integrator nothing to do. A code naming the state tells what holds and what to change. The prefix names the module that owns the refused state, which may differ from the module of the refusing use case. A code built at run time from a key, such as the conflict for a lost uniqueness key, declares its template in `refusals.composed`.

**Example:** `spending.receipt-missing` names the missing receipt. The code `spending.receipt-invalid` hides the state behind a vague word and is reported.

**Reviewed for:** Does every refusal code read `<module>.<noun>-<state>` and name its state? Look at the refusal codes on each use-case page.

### Retire Refusal Codes with Their Replacements

**ID:** `litespec/core/use-cases#retire-refusal-codes-with-their-replacements`

**Strength:** required

**Applies to:** artifact: record:result

**Statement:** A refusal code in use MUST be retired with its replacement named, never renamed or reused.

**Rationale:** The code is a public contract, and clients branch on it. Renaming a code in use silently breaks them. Reusing a retired code for another state sends old clients down the new branch. Retiring the code with its replacement named keeps every past refusal readable and every new client on the new code.

**Example:** The retired `spending.receipt-missing` result names `spending.receipt-absent` as its replacement. No later refusal reuses the old code.

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

**Rationale:** Some denials are forensics: a loan denial, a limit refusal, a fraud block. Without a stored attempt, the business has no evidence to answer with. Storing the attempt with its reason keeps that evidence. A validating refusal, such as a malformed code, stores nothing.

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

### Name the Result Each Scenario Ends In

**ID:** `litespec/core/use-cases#name-the-result-each-scenario-ends-in`

**Strength:** required

**Applies to:** artifact: record:scenario

**Statement:** Each scenario MUST name the result it ends in, written or derived.

**Rationale:** A scenario that names no result reads well and proves nothing: it illustrates behavior without asserting an ending. Naming the result ties each example to the ending it walks toward, so every test knows what to assert. A reaction or job scenario names a result or an outcome instead, as [Automation](automation.md) states. The `scenario-result-unknown` check reports a scenario ending in an unknown result.

**Checked by:** `litespec model check` reports `record-invalid` for a scenario entry without `result`, and `scenario-result-unknown` for a scenario ending in an unknown result.

### Prove Every Written Result with a Scenario

**ID:** `litespec/core/use-cases#prove-every-written-result-with-a-scenario`

**Strength:** required

**Applies to:** artifact: record:result

**Statement:** Each written result MUST be shown by at least one scenario of its use case.

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

**Rationale:** A rule proved only through a test mark hides which example walks it: readers open the rule and find no story. Naming the shown rules in `exercises` ties each example to the rules it walks, and every rule meets its stories. A refusal scenario names the rule behind its result. The scenarios of a use case together walk every rule its inputs cite.

**Example:** In the library product, a scenario walking a second hold on one copy names `rule/lending.one-hold-per-copy` in `exercises`.

**Checked by:** `litespec model check` reports `scenario-rule-unexercised` for a refusal scenario whose result's rule is not in `exercises`, or an input rule no scenario of the use case exercises.

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

**Rationale:** When every scenario repeats the same setup, the step that differs gets buried, and readers reread one Given on each story. Holding the shared Given once in `background` leaves each scenario its own situation, action, and assertion. Company narrative belongs in the explanation file, not in `background`.

**Example:** `background` holds that the employee has a session and one submitted expense awaits approval. Each approval scenario starts there.

**Checked by:** `litespec model check` reports `background-scenarios-missing` for a use case with `background` and fewer than two scenarios.

### Summarize Every Use Case

**ID:** `litespec/core/use-cases#summarize-every-use-case`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use case MUST carry `summary`.

**Rationale:** A reader landing cold needs the act before its fields. One paragraph states what the use case is. The steps live under Flow in the explanation file beside a command, where they stay readable without a shape a check must parse.

**Example:** The submit-expense summary states that an employee files a work expense for approval. Its Flow section lists validation, storage, and the approver notice in order.

**Checked by:** `litespec model check` reports `prose-missing` for a use case without `summary`.

### Tie Every Setting Default to a Requirement

**ID:** `litespec/core/use-cases#tie-every-setting-default-to-a-requirement`

**Strength:** required

**Applies to:** artifact: record:setting

**Statement:** Each setting MUST declare its value type, range, unit, setter, and bound behavior, with the requirement behind a commercial, legal, or varying default.

**Rationale:** A default no requirement holds is a figure nobody stated: a price, a fee, a term, or a limit nobody decided. Declaring the value type, the range, the unit, the setter, and the bound behavior states the tunable beside its promise. The setter is product, operator, or tenant. The bound behavior is `refuse`, `truncate`, or `degrade`. A commercial, legal, or tenant-, market-, or date-varying default cites its requirement. The default takes the setting's `valueType`: a duration default holds an ISO 8601 duration, a money default holds an amount with its currency, and a choice default names one of its `choices`. A setting whose values apply from a date carries `dated: true`, and no value change touches a posted record. The `setting-requirement-missing` check reports such a default with no cited requirement.

**Example:** The receipt threshold declares money with a product-set default of EUR 25. It refuses an expense above the line with no receipt, behind its requirement.

**Checked by:** `litespec model check` reports `setting-requirement-missing` for a commercial, legal, or varying default with no cited requirement.

### Name the Settings Each Use Case Reads

**ID:** `litespec/core/use-cases#name-the-settings-each-use-case-reads`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use case MUST name the settings it reads in `reads`.

**Rationale:** A setting a use case reads but no record names is a coupling readers cannot see: the tunable moves, the reader breaks, and no diff shows the pair. Naming the settings in `reads` beside the use case states the coupling where the reader lives, so each change to the range or default meets its readers. The setting's readers are computed as the inverse, never written. A setting no use case, reaction, or job reads is a finding. Settled by [decision/0012-use-cases-read-their-settings](decision/0012-use-cases-read-their-settings) on the evidence in [research/behavior-containment-evidence](research/behavior-containment-evidence) and [research/field-naming-evidence](research/field-naming-evidence).

**Example:** The submit-expense use case names `setting/spending.receipt-threshold` in `reads`. A move of the threshold meets the submission story.

**Checked by:** `litespec model check` reports `setting-unused` for a setting no record `reads`.

### Name the Configuration Key of Every Operator Setting

**ID:** `litespec/core/use-cases#name-the-configuration-key-of-every-operator-setting`

**Strength:** required

**Applies to:** artifact: record:setting

**Statement:** A setting the operator sets MUST name its configuration path in `key`.

**Rationale:** An operator setting reaches the product through configuration. The record and the configuration meet at one path: the record states the default and the bounds, and the configuration names the value. Without the key, a renamed section silently falls back to a default nobody chose, and no reader can tell which configuration value a promise rests on. A setting a tenant or the product sets is held as data by an aggregate instead, and names no key.

**Example:** Claimwick's `setting/spending.approval-reminder-delay` is set by the operator. It carries `key` `Spending:ApprovalReminderDelay`.

**Checked by:** `litespec model check` reports `setting-key-missing` (level G) for a setting with `setBy: operator` and no `key`.

### Bind Every Keyed Setting in One Marked Options Class

**ID:** `litespec/core/use-cases#bind-every-keyed-setting-in-one-marked-options-class`

**Strength:** required

**Applies to:** artifact: record:setting

**Statement:** A setting with a `key` MUST be bound by an options class that carries an `implements` mark naming the setting and whose section and property spell the key.

**Rationale:** The options class is where configuration becomes a typed value the code reads. It is the code element that implements the setting, so marking it names the promise the class holds. Comparing its section name and property with the record's key finds the renamed section or the typo before the product starts with a default.

**Example:** Claimwick's `SpendingOptions` class declares `SectionName = "Spending"`, an `ApprovalReminderDelay` property, and `/// <implements>setting/spending.approval-reminder-delay</implements>`.

**Checked by:** `litespec model check` reports `setting-unimplemented` (level S) for a keyed setting no options class marks, and `setting-key-drift` (level S) for a marked options class whose `SectionName` constant and property do not spell the setting's `key`.

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
