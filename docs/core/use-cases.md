# Use Cases

**Stack:** any
**Concern:** records

This page states what a use case record holds: its kind, name, inputs, reads and writes, effects, outputs, results, scenarios, and settings. It is for people writing behavior records and for agents scaffolding code from them. A vague verb or an unnamed ending drifts through every layer, so each rule below pins one piece beside the record.

## Concepts

### Commands and Queries

A use case is one goal proved through the public boundary by one test. A command changes records, and a query reads without changing records. The kind decides what the record touches.

A command names one aggregate in `writes` and changes one instance under that aggregate. A command across independent instances carries `consistency: per-record`, with each write committing on its own. A command changing nothing declares `writes: []`, and follow-on work in the same module answers the domain event from a reaction with its own commit.

| Mode | Means |
|:---|:---|
| `atomic` | Every write in `writes` commits as one group |
| `per-record` | Each write commits on its own; a failure leaves earlier writes in place |

One mode covers one use case and stops there. A command writing one aggregate and reading no guarding aggregate is atomic by definition and carries no `consistency` field. A command writing more than one aggregate stays exceptional, with `boundBy` citing the `atomicity` rule holding the invariant in one transaction and `basedOn` naming the decision behind it.

A guarded command reads an aggregate its rule guards and names that aggregate in `guards`, so the guard holds at commit. A guard reaches no further than its own module. At large size, a scenario naming the competing command in `competesWith` walks the race, with `then` holding the refusal or the serialized outcome.

### Use-Case Verbs

A use-case name opens with a verb. Each verb keeps one meaning and one kind across the project: query, command, or either. The baseline vocabulary below gives each verb its kind, and each further project verb lives as a verb term in the project glossary.

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
| `open`, `close` | command | A period, case or shift running between the two. |
| `start`, `complete` | command | Work running to an end. |
| `publish`, `withdraw` | command | What readers or the public see. |
| `pause`, `resume` | command | A temporary stop a person chooses. |
| `suspend`, `reinstate` | command | A stop an operator imposes. |
| `enable`, `disable` | command | A switch. |
| `grant`, `revoke` | command | Access. |
| `request`, `approve`, `refuse` | command | A request someone else decides. |
| `issue`, `void` | command | A document or credential. |
| `retire`, `restore` | command | The end of a record life, and its return inside the restore window. |
| `cancel` | command | An intention or booking not happening. |
| `expire` | command | What time ended; started by a job. |
| `record` | command | Something happening outside the system: a provider answer, a delivery result, a scan. |
| `send` | command | A message to a person. |
| `post`, `reverse` | command | A ledger transaction and its correction. |
| `retry`, `verify` | command | Work run again, and a check run again. |

The baseline retires one synonym per meaning into the verb that keeps it:

| Retired synonyms | Become |
|:---|:---|
| `get`, `fetch`, `lookup`, `count` | `read` |
| `browse` | `list` |
| `find` | `search` |
| `update`, `edit`, `modify`, `revise`, `set`, `adjust`, `configure`, `rename` | `change` |
| `define`, `compose`, `save`, `new` | `create` |
| `delete` | `remove` |
| `notify`, `remind`, `email` | `send` |
| `suggest`, `ask` | `answer` |
| `mark`, `note`, `log` | `record` |
| `end` | `close` |
| `finish` | `complete` |
| `abandon` | `cancel` |
| `lapse` | `expire` |
| `clone`, `copy` | `duplicate` |
| `replay` | `retry` |
| `recheck` | `verify` |
| `archive` | `retire` |
| `reject`, `deny` | `refuse` |
| `unpublish`, `hide` | `withdraw` |

A project retakes a retired synonym as its own glossary verb term with a definition holding the difference from the baseline verb. The definition names the baseline verb it differs from. The retired synonym then points at its replacement in listings.

After the verb, the name carries an aggregate, a declared part, or a glossary term. The name `use-case/lending.borrow-copy` carries the copy aggregate, and `use-case/sales.add-line` carries the declared line part. The command name and the aggregate method share the verb, so the two stay one act under one word.

Query names follow the same noun shape beside their verb. A method opening with `Request` performs the request that an event handler finishes.

```yaml
- id: term/borrow
  term: borrow
  meaning: The loan of one copy to one reader.
  layer: behavior
  verbKind: command
```

A reaction takes its name from its trigger, and a job takes its name from the use case it runs. The name shows the source beside the work. Readers trace the trigger to the reaction without opening the edge.

### Inputs and Access

Each input declares its name, meaning, type, origin, and classification. The type comes from `text`, `integer`, `decimal`, `money`, `boolean`, `date`, `date-time`, `duration`, `email`, `choice`, `file`, `list`, or `id`. The origin is `caller`, `claim`, `verified-claim`, or `derived`, and it shows how far the value is trusted.

An `id` field names the record it refers to in `identifies`, or an identified part of an aggregate as `aggregate/<module>.<aggregate>#<part>`. A `choice` field names its allowed values in `choices`. A `file` field names its media types in `contentTypes` with its limit in `maxSize`.

A `list` field names its item shape in `items`, with each row field holding name, type, flag, and meaning, plus `minItems` and the unique key fields. Other types carry no `identifies`, `items`, or `choices`.

An input carrying a claim a provider validated declares `origin: verified-claim`, with `verifiedBy` naming the entry point or external system validating it. A caller-asserted figure on a money or access path cites the rule bounding it, tying each such value to the guard holding it. In Claimwick, the replacement net amount cites its positivity rule.

Each use case declares its grant and tenant scope in `access`: `permission/<name>`, `self(permission/<name>)`, `signed-in`, `anonymous`, or `system`, with tenant scope `own`, `cross`, `public`, or `none`. The `tenantReason` states why the call reaches no tenant or crosses tenants. The `scope: self` narrows the call to records identifying the caller, with `identityInput` naming the verified-claim input carrying the caller identity, and `preconditions` naming the precondition rules the authorization evaluates.

```json
"inputs": [
  { "name": "expenseId", "type": "id", "identifies": "aggregate/spending.expense", "origin": "caller" },
  { "name": "amount", "type": "money", "rules": ["rule/spending.positive-amount"], "origin": "caller" },
  { "name": "lines", "type": "list", "items": { "type": "object", "fields": [{ "name": "amount", "type": "money", "required": true, "meaning": "The line amount." }], "minItems": 1 }, "origin": "caller" }
]
```

A command carries only identifiers its caller holds. Each caller-supplied `id` input, apart from an idempotency key the caller mints, comes back as an `id` field identifying the same record, or the same identified part, through a query through the same entry point, or through a query of its module where no entry point reaches the use case. An optional input travels with no such read, because the caller calls without it. In Claimwick, the approve-expense command takes `expense` identifying `aggregate/spending.expense`, and the list-expenses query through the same entry point returns `expenseId` identifying the same aggregate, so the manager picks the expense from the list and approves it.

### Entry Points and Public Modules

Each use case a caller reaches from outside names the entry points reaching it in `via`. The entry point the call arrives through declares `authentication`. A use case only a reaction or a job runs carries no entry point of its own, and the `runs` edge of that reaction or job shows how it is reached.

A public query lists the consuming modules in `via`, and each consuming use case, reaction, or job declares the edge in `effects.queries`. A public command lists the sending modules in `via`, and each sending reaction or job declares the edge in `effects.commands`. A use case declares no `commands` edge.

In Claimwick, spending read-expense lists the reporting module in `via`, and reporting reimbursement declares `effects.queries` naming `use-case/spending.read-expense`. Spending close-expenses lists the reporting module in `via`, and the reporting close-period job declares `effects.commands` naming `use-case/spending.close-expenses`. A public query no module reads is published with no reader, and a public command no module sends stays with no caller.

### Effects and the Facts They Raise

Each effect entry carries exactly one verb, `sends`, `calls`, `queries`, or `commands`, with its timing: `inline`, `after-commit`, or `async`. A `queries` entry names the public query of another module read within the handling. A `commands` entry names the public command of another module sent within the handling.

A use case runs no other use case and declares no `runs`, `emits`, or `commands` edge. Each use case names the domain events it raises in `raises`, tying each fact to the behavior announcing it. A reaction to the raised fact runs the follow-on work, and the outbound integration events follow from `raises` with the `translates` field of each outbound integration event.

In Claimwick, the reimburse-expense command declares no effect. It raises its reimbursed fact, the expense-reimbursed integration event translates that fact, and the reporting read model answers the published event.

Each effect entry carries one `onFailure` shape with its mode, `retry`, `skip-and-log`, or `discard`, with `retry` and `ifExhausted` beside mode `retry`, and optional `deadLetter`, `surfacesIn`, and `runbook`. The `onExhausted` field names the act where retries run out. The `before` field orders the entry ahead of another named target with its reason, and `forEach` names the items the entry repeats over. The effect graph holds no cycle except through a domain event with a stated end condition.

### Idempotency and Audit

Each command declares its idempotency mode with what the mode needs. Key mode carries its key template and scope, state mode carries the state named in `recognizedBy`, and none mode carries the reason repeats are absent. The key templates `{field}` placeholders over the input names, and the scope is `tenant`, `account`, `login`, or `global`. A query carries no `idempotency`, while a reaction or job carries its own.

Each command with `idempotency` states what a recognized repeat brings: `returns-first-result`, `refuses`, or `acts-again`. A keyed command recognizes the repeat by the key the caller sends. A state command names the state it checks in `recognizedBy`, such as the posted entry the second booking meets, and recognizes the repeat there, with no stored key outliving an undone state. In Claimwick, the reimburse-expense command replays with `returns-first-result`, returning the first reimbursement on a retried payout.

Each command declares its audit position: an `audit` object with its level and target kind, or an exemption with its justification in `exemptBecause`. The `target` names one input, a list of inputs forming one composite target, or the `outputs` field carrying the target where the handler determines it. A refusal carrying `recorded: true` stores the attempt with its reason: the self-checkout scan records unknown, retired, and already-lent barcodes with their reasons. A validating refusal stores nothing.

### Queries and Their Output

A query reads without changing, and declares what its readers receive. Its `outputs` group fields into named `include` groups, with each group holding its fields, classification, the aggregates it reads, and its freshness. A query whose aggregates lag reads from a read model: data shaped for the query, served without loading an aggregate. A command declares its output as a list of name, meaning, and type entries, and the endpoint answers a stated output with 200, or with 201 and the created location, while a command with no output answers 204.

Freshness is mode, within, and decidedBy: `immediate`, `snapshot`, or `eventual`. A lagging read states in `within` how far behind its source it stays, and names in `decidedBy` the command use case or aggregate re-deciding the figure at write time. In Claimwick, the reimbursed-expenses list carries `snapshot` freshness naming `use-case/spending.reimburse-expense`, so a reader seeing a stale row follows the link to the deciding command.

An `include` group visible to more roles than the aggregates it reads cites the widening requirement in `widenedBy`, with `visibleWhen` holding the narrowing condition. Each collection query declares its paging: mode `pages` with the order, the cursor or offset, and the default and maximum limits, or mode `none` with its reason. Each output field carries its value type, with a list of rows taking type `list` and its item shape in `items`.

### Results and Refusals

Each use case ends in named `results`: a success carrying `meaning`, or a refusal naming its refusal by id with the message a person reads, plus one message per interface language beside it in `translations`. Each aggregate declares the refusals its rules raise once in its `refusals.yaml`, and the module `refusals.yaml` keeps only the refusals no aggregate raises. A result `refusal` names a refusal from its aggregate set, or from the module set where no aggregate raises it, and a rule `refusal` names the same refusal.

Each refusal id reads `refusal/<module>.<noun>-<state>`, naming the state meeting the caller rather than the failure: `refusal/spending.receipt-missing` names the missing receipt. The module segment names the module owning the refused state. A code built at run time from a key declares its template in `refusals.composed`.

A refusal code in use retires with its replacement named, and no retired code returns for another state. Refusals stand in precedence order with the first match winning: the derived refusals first, unauthenticated, forbidden, sign-in-too-old, invalid input, not found, rule-not-stated, then conflict, followed by the written refusals in the order the page lists them.

Generic refusals come from derivation rather than writing: `forbidden` from the grant in `access`, `invalid-input` from the rules in `inputs`, `not-found` from the named target, `conflict` from the state the call met, `unauthenticated` from the missing caller, `sign-in-too-old` from the sign-in window, and `rule-not-stated` from the knowledge the use case reads. A success with recorded outcomes lists them in `codes`: the checked-out result lists `borrow` and `renew`. Each refusal sentence names only a way out the product provides, a use case, screen, setting, or input that exists, or holds the refusal without one.

### Scenarios

A scenario proves one ending of the behavior it exercises, as an entry in the file of its use case. It carries `given`, `when`, `then`, an optional `examples` table with one shared set of keys, and the `result` it ends in. One test proves each scenario through the public boundary, and a proof turns stale where either of its proof hashes changes.

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

A scenario proving a guard against a competing command carries that command in `competesWith`, with `then` holding the refusal or the serialized outcome. A refusal scenario names the rule behind its result in `exercises`, and the scenarios of a use case together walk every rule its inputs cite. Each scenario asserts only its own operation in `then`: its results, state, and raised events, leaving later operations to the flow test.

Each rule boundary named in `exercises` carries its own scenario at the line: the hold expiry rule carries one scenario collecting on the last pickup day and one after the lapse. Each scenario carries its own `given`, `when`, and `then`. Shared narrative lives in the explanation file beside the record file, and the retired `background` name fails validation.

### Settings

A setting is one operator configuration value with its type, range, unit, setter, and bound behavior: a provider timeout, a batch size, or a delay the operator tunes, read by the running system. A business value a person changes through work is aggregate data instead: its range is a rule with a refusal, and its change is a use case with access and audit. A value a tenant administrator changes for the whole tenant is a tenant setting with `setBy: tenant`, changed by a `change-*` use case.

A setting is one record file in `behavior/modules/<m>/settings/<name>/<name>.yaml`, with its explanation file beside it. It opens with its `meaning` and carries no `summary`. A use case, reaction, or job names the settings it reads in `reads`, and the setting readers come back as the computed inverse. A setting several modules read is owned by one module, and the other modules read it across their declared dependency.

An operator setting binds from a configuration path such as `Spending:ApprovalReminderDelay`, while a tenant setting changes through its `change-*` use case and carries no path. Each number or duration setting declares its `unit`, and each value applying from a date carries `dated: true`, with no value change touching a posted record. Out-of-range values turn to `refuse`, `truncate`, or `degrade` per `outOfRange`.

### Summaries and Explanation Files

The `summary` field states what the use case is, in one paragraph. A command lists its steps in `steps` in order, each `statement` naming who acts with `cites` naming the rules, results, settings, events, read models, external systems, and aggregates the step applies. The explanation file restates the steps under Flow. Edge cases the command leaves outside the steps take one sentence per case in `edgeCases`.

An actor or role `summary` states in plain sentences who the party is and what the grant is for, which the fields alone leave unsaid, so the grant reads as a decision rather than a row. An external system `summary` states in plain sentences what the outside system does for the product, which the message lists alone leave unsaid, so the dependency reads as a decision rather than a row.

## Standards

### Give Each Use Case a Command or Query Kind

**ID:** `litespec/core/use-cases#give-each-use-case-a-command-or-query-kind`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use case MUST declare one kind: command or query.

**Rationale:** A use case with no kind reads as able to do anything, and the checks have no shape to confirm. The kind decides what the record touches: a command changes, and a query reads.

**Checked by:** `litespec model check` reports `record-invalid` for a use-case page without `kind` or with an unknown kind.

### Start Every Use Case with a Declared Verb

**ID:** `litespec/core/use-cases#start-every-use-case-with-a-declared-verb`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use case MUST start with a verb from the baseline vocabulary or a verb term the project declares in its glossary.

**Rationale:** Without one verb per meaning, `read` competes with `get` and `change` competes with `update` in every listing, and related behavior scatters. Starting from one closed baseline vocabulary and adding only declared verbs keeps use cases findable. Settled by decision/0062-declare-project-verbs-in-the-glossary.

**Example:** The project declares `borrow` and `check-in` as glossary verb terms. The names `use-case/lending.borrow-copy` and `use-case/lending.check-in-copy` pass, `use-case/lending.list-loans` passes on the baseline vocabulary, and `use-case/lending.update-copy` is told to use `change`. The library declares `archive` as its own verb for a copy kept readable and restorable, with a definition holding how it differs from `retire`.

**Checked by:** `litespec model check` reports `use-case-verb-unknown` for a use-case name opening with a verb neither list declares and `term-verb-duplicate` for a glossary verb term restating a baseline verb, and `litespec model review` reports `verb-distinction-missing` (level R) for a declared verb retaking a retired synonym whose definition does not name the baseline verb it differs from.

### Keep One Kind per Verb

**ID:** `litespec/core/use-cases#keep-one-kind-per-verb`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use case MUST open with a verb of its own kind, or with an either verb.

**Rationale:** A query named `change-...` or a command named `read-...` mislabels what the use case touches in every listing. The first word is the first thing a reader trusts, and a mismatched first word teaches readers to trust no name. Settled by decision/0062-declare-project-verbs-in-the-glossary.

**Example:** `use-case/lending.list-loans` is a query and passes. The name `use-case/lending.answer-inquiry` opens either kind on an either verb and passes. The name `use-case/lending.read-overdue-count` declared as a command is reported, and so is `use-case/lending.change-copy` declared as a query.

**Checked by:** `litespec model check` reports `use-case-verb-invalid` for a use case opening with a verb of the other kind.

### Name What a Use Case Acts on from the Model

**ID:** `litespec/core/use-cases#name-what-a-use-case-acts-on-from-the-model`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use case MUST name, after its verb, an aggregate, a declared part, or a glossary term.

**Rationale:** A free synonym after the verb hides which business record the use case acts on, and gives the concept a second word. The noun after the verb groups behavior by the record it acts on.

**Example:** `use-case/lending.borrow-copy` names the copy aggregate, and `use-case/sales.add-line` names the declared line part. The name `use-case/lending.borrow-volume` carries no model word and is reported.

**Reviewed for:** Does every use-case name carry an aggregate, a declared part, or a glossary term after its verb? Look at the words after the verb in each use-case name.

### Name the Entry Points and Modules That Reach Each Use Case

**ID:** `litespec/core/use-cases#name-the-entry-points-that-reach-each-use-case`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use case a caller reaches from outside MUST name the entry points that reach it in `via`.

**Rationale:** A use case with no named entry point reads as reachable from anywhere or nowhere. Naming the entry points shows how callers arrive at each behavior. A public query names the consuming modules beside or instead of entry points, and a public command names the sending modules.

**Example:** The library `complete-sign-in` use case names the library app entry point in `via`. The provider redirects the browser to the host callback, where the ticket handler in the `Infrastructure/Pipeline/` folder runs the use case under its mark. No endpoint maps the callback route and no screen calls the use case.

**Checked by:** `litespec model check` reports `record-invalid` for a live use case that names no entry point and that nothing runs, and `reference-unknown` for a `via` entry naming no entry point or module.

### List the Consumer Modules of a Public Query

**ID:** `litespec/core/use-cases#list-the-consumer-modules-of-a-public-query`

**Strength:** required

**Applies to:** artifact: record:use-case; when: query

**Statement:** Each public query MUST list every consuming module in `via`.

**Rationale:** A `queries` edge names the public query use case it reads, and the public query names the modules allowed to read it. Where the two lists disagree, the edge sits declared on one side only. A public query no module reads is published with no reader.

**Example:** Spending's read-expense query lists `module/reporting` in `via`, and reporting's reimbursement command declares `effects.queries` naming `use-case/spending.read-expense`. A second consumer joins by adding its module to the list.

**Checked by:** `litespec model check` reports `reference-unknown` for a `queries` edge naming no written use case, `query-target-invalid` for a `queries` edge to a use case that is not a query or whose `via` names no calling module, and `query-consumer-missing` for a public query no module queries.

### List the Sending Modules of a Public Command

**ID:** `litespec/core/use-cases#list-the-sending-modules-of-a-public-command`

**Strength:** required

**Applies to:** artifact: record:use-case; when: command

**Statement:** Each public command MUST list every sending module in `via`.

**Rationale:** A `commands` edge names the public command use case it sends, and the public command names the modules allowed to send it. Where the two lists disagree, the edge sits declared on one side only. A public command no module sends is published with no caller.

**Example:** Spending's close-expenses command lists `module/reporting` in `via`, and reporting's close-period job declares `effects.commands` naming `use-case/spending.close-expenses`. A second sender joins by adding its module to the list.

**Checked by:** `litespec model check` reports `reference-unknown` for a `commands` edge naming no written use case, `command-target-invalid` for a `commands` edge to a use case that is not a command or whose `via` names no sending module, and `command-consumer-missing` for a public command no module commands.

### Keep Retired Exposure Out of Use Cases

**ID:** `litespec/core/use-cases#state-the-exposure-of-every-use-case`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A use case MUST NOT carry `exposure`: who reaches the use case is declared in `via` and `access`, and the human judgment about what is at stake lives in the explanation file beside the record file.

**Rationale:** `exposure` repeated `via`, `access`, and `summary` in plain words, so the same fact lived in two places and drifted. Declaring who reaches the use case once in `via` and `access` keeps one source of truth. The explanation holds the judgment about what is at stake.

**Example:** The reimburse-expense record declares the approver, the approval screen in `via`, and the reimburse-expense permission in `access`. Its explanation states that a wrong payout moves money to the wrong account.

**Checked by:** `litespec model check` reports `record-invalid` for a use case carrying `exposure`.

### Describe Every Use Case Input with Origin and Classification

**ID:** `litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use-case input MUST declare its name, meaning, type, origin, and classification.

**Rationale:** An input without an origin is trusted on faith: caller-asserted values pass as verified claims, and money moves on an unchecked figure. Declaring the name, the meaning, the type, the origin, and the classification states what arrives, what it is, and how far to trust it. Settled by decision/0016-typed-contracts-and-privacy.

**Example:**

```json
"inputs": [
  { "name": "expenseId", "type": "id", "identifies": "aggregate/spending.expense", "origin": "caller" },
  { "name": "amount", "type": "money", "rules": ["rule/spending.positive-amount"], "origin": "caller" },
  { "name": "lines", "type": "list", "items": { "type": "object", "fields": [{ "name": "amount", "type": "money", "required": true, "meaning": "The line amount." }], "minItems": 1 }, "origin": "caller" }
]
```

**Checked by:** `litespec model check` reports `record-invalid` for a use-case input without name, meaning, or type, without `identifies` on an `id` field, without `items` on a `list` field, without `choices` on a `choice` field, or with `identifies`, `items`, or `choices` on another type, and `reference-unknown` for an `identifies` naming an aggregate part the aggregate does not declare with identity.

### Mark Every Provider-Verified Input

**ID:** `litespec/core/use-cases#mark-every-provider-verified-input`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each input carrying a claim a provider validated MUST declare `origin: verified-claim` with the entry point or external system that validated it in `verifiedBy`.

**Rationale:** A provider claim filed as caller input is trusted on faith: a forged subject or email passes as verified, and access follows. Declaring the claim with the entry point or external system checking its issuer, audience, signature, and expiry ties each trusted value to the boundary earning the trust. Settled by decision/0079-cover-the-behavior-reviewers-found-missing.

**Example:** The library sign-in command declares the provider subject with `origin: verified-claim`. Its `verifiedBy` names the provider sign-in entry point.

**Checked by:** `litespec model check` reports `input-claim-unverified` for a claim-shaped input with `origin: caller` on an entry point that authenticates through a provider.

### Return Every Caller-Supplied Identifier from a Reachable Query

**ID:** `litespec/core/use-cases#return-every-caller-supplied-identifier-from-a-reachable-query`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each required input with `origin: caller` and type `id` that names no idempotency key placeholder MUST be returned as an `id` field identifying the same record, or the same identified part, by a query through the same entry point, or by a query of its module when no entry point reaches the use case.

**Rationale:** A command needing an identifier no read hands to its caller fails when called: the screen offers the act, the caller holds no value to send, and the failure surfaces only at run time. Comparing the `identifies` links keeps the check exact, because names differ while the identified record stays the same.

**Example:** Claimwick's approve-expense command takes `expense` identifying `aggregate/spending.expense` from the caller. The list-expenses query through the same entry point returns `expenseId` identifying the same aggregate, so the manager picks the expense from the list and approves it. A command taking an identifier no query through its entry point returns is reported.

**Checked by:** `litespec model check` reports `caller-identifier-unreachable` for a required caller-supplied identifier on a use case an entry point reaches that no query through the same entry point returns, and `module-identifier-unreachable` (level G) for one on a use case no entry point reaches that no query of its module returns.

### Name Every Aggregate a Use Case Writes or Reads

**ID:** `litespec/core/use-cases#name-every-aggregate-a-use-case-writes-or-reads`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use case MUST name every aggregate it writes in `writes` and every aggregate it reads in `reads`.

**Rationale:** A write no record names stays invisible: the handler touches a second aggregate, the second aggregate changes, and no diff shows it. Naming each touched aggregate states the read and write footprint beside the behavior.

**Checked by:** `litespec model check` reports `write-undeclared` for a write outside `writes` and `reference-unknown` for an unknown aggregate.

### Change One Aggregate in Each Command

**ID:** `litespec/core/use-cases#change-one-aggregate-in-each-command`

**Strength:** required

**Applies to:** artifact: record:use-case; when: command

**Statement:** Each command MUST name one aggregate in `writes`, live under that aggregate, and change one instance, unless a rule that governs the use case is named in its consistency `boundBy` and names in `basedOn` the decision recording the invariant needing more than one in one transaction, its record states `consistency: per-record` for independent instances, or it changes no aggregate and declares `writes: []`.

**Rationale:** A command changing two aggregate types in one transaction couples their consistency boundaries: one failing part rolls back work with no reason to fail with it. One aggregate type per command keeps each transaction on one boundary. A command changing nothing, because it verifies or reports and raises a fact, declares `writes: []`.

**Example:** The reimburse-expense command names `aggregate/spending.expense` in `writes` and lives under the expenses aggregate. The reporting reaction to its reimbursed fact answers in its own transaction instead of joining the write. The attach-receipt command names two aggregates with its atomicity rule in `boundBy` naming the decision recording the invariant.

**Checked by:** `litespec model check` reports `use-case-writes-invalid` for a command whose `writes` names more than one aggregate without a `boundBy` rule that governs the use case and names a decision in `basedOn`, and `record-placement-invalid` for a command use case at module level.

### Cite the Invariants Most Writers Cite

**ID:** `litespec/core/use-cases#cite-the-invariants-most-writers-cite`

**Strength:** required

**Applies to:** artifact: record:use-case; when: command

**Statement:** Each command MUST cite every invariant rule that governs an aggregate it writes when most commands writing that aggregate cite the rule.

**Rationale:** A command writing an aggregate meets every invariant governing it, and the sibling commands writing the same aggregate show which invariants those writes meet. Where most of them cite an invariant, the outlier posts without the checks the others apply. Settled by decision/0101-check-rule-citations.

**Example:** Three library commands write the loan aggregate. Two cite the one-hold-per-copy invariant, and the third is reported.

**Checked by:** `litespec model check` reports `rule-sibling-uncited` for a command writing an aggregate without citing an invariant most commands writing that aggregate cite.

### Declare Consistency on Multi-Aggregate Commands

**ID:** `litespec/core/use-cases#declare-consistency-on-multi-aggregate-commands`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each command writing two or more aggregates, or reading an aggregate whose rule guards its write, MUST declare its write consistency in `consistency`.

**Rationale:** Consistency left unsaid reads as atomic until a multi-aggregate write fails halfway. Declaring the mode states the promise: `atomic` commits every write in `writes` as one group, and `per-record` commits each write on its own. A guarded command declares `atomic` with the guarding aggregates in `guards`.

**Example:** The reimburse-expense command declares atomic consistency citing the reimbursed-once rule. The self-checkout scan declares per-record consistency across its attempts. The submit-expense command writes the expense and reads the report, so it declares atomic consistency citing the open-report rule with the report in `guards`.

**Checked by:** `litespec model check` reports `consistency-cites-missing` for an atomic multi-aggregate write with no `boundBy`, `atomicity-drift` for `writes` differing from the bound group, `atomicity-invalid` (level G) for a group above 4 aggregates, and `atomicity-duplicate` (level G) for a command saving more than one atomic write group; `litespec model check` reports `record-invalid` for a query carrying `consistency`.

### Hold Every Cross-Aggregate Guard Until Commit

**ID:** `litespec/core/use-cases#hold-every-cross-aggregate-guard-until-commit`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each command whose `boundBy` rule governs an aggregate of its own module that the command reads but does not write MUST name that aggregate in `consistency.guards`.

**Rationale:** A guard read at the start of a command shifts before the command commits: submit-expense reads the report as open, close-report closes it and commits, and submit-expense then commits an expense on a closed report. Naming the guarding aggregate in `guards` states that the rule holds when the command commits.

**Example:** The submit-expense command reads the report and writes the expense, with the open-report rule in `boundBy`. It names the report in `guards`, so a racing close-report commit either waits or makes submit-expense refuse.

**Checked by:** `litespec model check` reports `guard-undeclared` (level G) for a command whose `boundBy` rule governs an aggregate the command reads but does not write, and whose `consistency.guards` does not name that aggregate, and `guard-module-foreign` for a guard naming an aggregate of another module, or a bound rule over one the command reads but does not write.

### Prove Every Guard Against a Competing Command

**ID:** `litespec/core/use-cases#prove-every-guard-against-a-competing-command`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** At large size, each command carrying `consistency.guards` MUST be shown by at least one scenario naming the competing command in `competesWith`.

**Rationale:** Each scenario running alone passes while the race stays unproven: each command meets the guard in its own state and commits. Running the guarded command against its competitor shows what the race brings. The scenario names the competing command in `competesWith`, and its `then` holds the refusal or the serialized outcome.

**Example:** A submit-expense scenario carries `competesWith: use-case/spending.close-report`. Its `then` states that the expense is refused because the report closed, or that one command commits first and the other meets the new state.

**Checked by:** `litespec model check` reports `guard-race-scenario-missing` (level G) for a command with `consistency.guards` and no scenario carrying `competesWith`.

### Declare Every Effect with One Verb and Its Timing

**ID:** `litespec/core/use-cases#declare-every-effect-with-one-verb-and-its-timing`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each effect entry MUST carry exactly one verb and its timing.

**Rationale:** An edge no record declares stays hidden: calls between use cases, reactions, jobs, notifications, integration events, and external systems that no reader sees. Declaring each edge with exactly one verb, its timing, and its failure handling turns each coupling into data. The timing is `inline`, `after-commit`, or `async`, with timing on each use-case effect.

**Example:** The reimburse-expense command declares no effect. It raises its reimbursed fact, the expense-reimbursed integration event translates that fact, and the reporting module's read model answers the published event.

**Checked by:** `litespec model check` reports `effect-undeclared` for an edge the records never declared and `effect-timing-invalid` for a command `sends` or `calls` effect with `inline` timing; `litespec model check` reports `record-invalid` for an effect entry without exactly one verb or its timing.

### Run Follow-On Work Only from Reactions and Jobs

**ID:** `litespec/core/use-cases#run-follow-on-work-only-from-reactions-and-jobs`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A use case MUST NOT run another use case, so each follow-on act runs from a reaction to the fact the use case raised, or from a job.

**Rationale:** A use case running another use case nests one command inside another. The inner command runs inside the outer commit. A reaction to the raised fact runs the follow-on act in its own transaction, with its own failure handling and retry, so each record states one unit of work.

**Example:** In the library product, borrowing a copy raises the loan-borrowed fact. The send-due-notice reaction answers it in its own transaction, instead of the borrow command sending the notice.

**Checked by:** `litespec model check` reports `record-invalid` for a use-case effect that names `runs`, `emits`, or `commands`.

### Name the Idempotency of Every Command

**ID:** `litespec/core/use-cases#name-the-idempotency-of-every-command`

**Strength:** required

**Applies to:** artifact: record:use-case; when: command

**Statement:** Each command MUST declare its idempotency mode with the key, key scope, or reason that mode needs.

**Rationale:** A retried call without a stated idempotency runs twice: a double payout, a double loan, a double notice. Declaring the mode states how repeats collapse. The mode is `key`, `state`, or `none`, with its key template and scope. Mode `none` carries the reason repeats are absent.

**Checked by:** `litespec model check` reports `record-invalid` for a command without an idempotency mode or without the key, scope, or reason its mode needs; `litespec model check` reports `record-invalid` for a key with no `{field}` placeholder or a scope outside `tenant`, `account`, `login`, and `global`; `litespec model check` reports `record-invalid` for a query carrying `idempotency`. `litespec model check` reports `idempotency-key-uncarried` for a key placeholder naming no carried field.

### Carry Every Key Placeholder

**ID:** `litespec/core/use-cases#carry-every-key-placeholder`

**Strength:** required

**Applies to:** artifact: record:use-case, record:reaction, record:job

**Statement:** Each idempotency or deduplication key MUST name only placeholders that the record carries: an input of a use case, a field of an event a reaction answers, or an input of a use case a job runs.

**Rationale:** A key template is evaluated where a repeat arrives. A placeholder without a value the record carries is left unfilled, so each call computes the same key or no key at all. Naming only carried fields keeps each key unique to the work it guards.

**Example:** In Claimwick, `use-case/spending.submit-expense` keys repeats on `{expenseId}`, an input it carries. A key on `{attemptId}` is reported, because no input or triggering event supplies an attempt identifier.

**Checked by:** `litespec model check` reports `idempotency-key-uncarried` (level G) for each key placeholder that no input of a use case, no field of a reaction's triggering event, and no input of a job's run use case carries.

### State What a Replay Does

**ID:** `litespec/core/use-cases#state-what-a-replay-does`

**Strength:** required

**Applies to:** artifact: record:use-case; when: command

**Statement:** Each command carrying `idempotency` MUST state what a recognized replay does in `replay` and how the command recognizes it.

**Rationale:** A repeat the command recognizes but handles nowhere either runs twice or fails without telling why: a double payout, or a refusal hiding the first success. Stating `returns-first-result`, `refuses`, or `acts-again` tells the caller what the repeat brings. A state command names the state it checks in `recognizedBy`. Settled by decision/0079-cover-the-behavior-reviewers-found-missing.

**Example:** The reimburse-expense command replays with `returns-first-result`. A retried payout returns the first reimbursement instead of posting again.

**Checked by:** `litespec model check` reports `idempotency-replay-missing` for a command `idempotency` without `replay`, or mode `state` without `recognizedBy`, and `idempotency-replay-drift` (level S) for a keyed command whose idempotency declaration replays the first result differently than its `replay` states.

### State the Audit Position of Every Command

**ID:** `litespec/core/use-cases#state-the-audit-position-of-every-command`

**Strength:** required

**Applies to:** artifact: record:use-case; when: command

**Statement:** Each command MUST declare its audit level and target kind, or its exemption with its justification in `exemptBecause`, when the project selects the `audit` extension.

**Rationale:** An unaudited change with no exemption is a gap nobody admitted: money moves, holds lapse, and no record says who asked. Declaring the level with its target kind, or the exemption with its justification, states the audit beside the change.

**Checked by:** `litespec model check` reports `audit-missing` for a command without an audit level and target kind or an exemption with its justification when the project selects the `audit` extension, and `field-extension-unselected` for `audit` written while the extension is not selected.

### Declare Every Query Output

**ID:** `litespec/core/use-cases#declare-every-query-output`

**Strength:** required

**Applies to:** artifact: record:use-case; when: query

**Statement:** Each query MUST declare its output in named `include` groups.

**Rationale:** A query without a declared output returns whatever its author fetched. Readers and screens guess the shape from code. Declaring the output in named groups with fields, classifications, the aggregates each group reads, and freshness states what readers receive.

**Example:** The list-expenses query declares open and decided groups, each with `{ "mode": "immediate" }` freshness.

**Checked by:** `litespec model check` reports `query-output-missing` for a query with no declared output.

### Name the Use Case That Decides a Stale Figure

**ID:** `litespec/core/use-cases#name-the-use-case-that-decides-a-stale-figure`

**Strength:** required

**Applies to:** artifact: record:use-case, record:read-model

**Statement:** Each query output group and each read model whose figure a command re-decides at write time MUST name in `decidedBy` that command use case or its aggregate, in the module that owns the figure's source.

**Rationale:** A stale figure without a named decider leaves the reader guessing who settles it: the page shows seat availability, and nothing says the hold command re-decides it at write time. Naming the deciding command use case, or its aggregate, states the link beside the figure, so the reader follows it to the current truth.

**Example:** The reimbursed-expenses list carries `snapshot` freshness and names `use-case/spending.reimburse-expense` in `decidedBy`. A reader seeing a stale row follows the link to the command deciding each reimbursement.

**Checked by:** `litespec model check` reports `freshness-decider-invalid` (level G) for a `decidedBy` naming a query or a use case or aggregate outside the module that owns the figure's source, and `record-invalid` for a `decidedBy` with `immediate` freshness.

### Justify Every Wider Visibility

**ID:** `litespec/core/use-cases#justify-every-wider-visibility`

**Strength:** required

**Applies to:** artifact: record:use-case; when: query

**Statement:** Each include group visible to more roles than the aggregates it reads MUST cite the requirement widening it in `widenedBy`.

**Rationale:** A read showing more than the aggregates it reads widens without a recorded choice: roles see figures their own grants disallow, and no requirement states the view. Citing the requirement in `widenedBy` keeps the widening a decided choice.

**Checked by:** `litespec model check` reports `widening-cites-missing` for a widened include group with no cited requirement.

### Declare Paging on Every Collection Query

**ID:** `litespec/core/use-cases#declare-paging-on-every-collection-query`

**Strength:** required

**Applies to:** artifact: record:use-case; when: query

**Statement:** Each collection query MUST declare its paging with `mode` of `pages`, or `{ "mode": "none", "reason": "..." }` with its reason.

**Rationale:** A collection without paging works until the data outgrows it: the first hundred rows return, and the next ten thousand time out. Declaring the order, the cursor or offset, and the default and maximum limits with mode `pages` states the paging beside the read.

**Checked by:** `litespec model check` reports `paging-undeclared` for a collection query with no paging.

### Type Every Output Field

**ID:** `litespec/core/use-cases#type-every-output-field`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each output field MUST carry its value type.

**Rationale:** An output field without a type returns whatever its author fetched. Readers and screens guess the shape from code. Carrying the value type from the input types on every field of an `include` group and of a command output states the shape beside the promise. Settled by decision/0073-type-every-output-and-declare-refusal-sets.

**Example:** The list-expenses query types its amount field `money`. It types its lines field `list` with the item shape in `items`.

**Checked by:** `litespec model check` reports `record-invalid` for an output field without `type`.

### Declare Each Aggregate's Refusals Once

**ID:** `litespec/core/use-cases#declare-each-aggregates-refusals-once`

**Strength:** required

**Applies to:** artifact: record:refusal-set

**Statement:** Each aggregate MUST declare the refusals its rules raise once in `behavior/modules/<m>/aggregates/<aggregates>/refusals.yaml`, with each entry carrying its `id` and `title`.

**Rationale:** Refusals declared per use case drift: one use case retires a code while its neighbor keeps it, and the same breach reports two refusals. Declaring each refusal once beside the aggregate whose rules raise it keeps one refusal and one meaning behind each result citing it. Settled by decision/0073-type-every-output-and-declare-refusal-sets.

**Example:** The expense aggregate declares `refusal/spending.receipt-missing` with its meaning. The submit-expense refusal cites that id.

**Checked by:** `litespec model check` reports `reference-unknown` for a result refusal missing from its aggregate's refusal set, `record-invalid` for a refusal entry without `id` or `title`, and `code-placement-drift` or `record-placement-invalid` for a refusal and its exception declared under different homes: a refusal declared under an aggregate whose exception lives under another aggregate or outside `Domain/`, or an aggregate's exception whose refusal is declared at module level.

### Declare Each Module's Refusals Once

**ID:** `litespec/core/use-cases#declare-each-modules-refusals-once`

**Strength:** required

**Applies to:** artifact: record:refusal-set

**Statement:** Each module MUST declare in `behavior/modules/<m>/refusals.yaml` only refusals no aggregate raises, with each code's meaning.

**Rationale:** A refusal an aggregate rule raises is declared in that aggregate set. The module set keeps only the refusals no aggregate raises: a file refused before any aggregate exists, or a module-wide refusal. Settled by decision/0073-type-every-output-and-declare-refusal-sets.

**Example:** The spending module declares a module-wide refusal with its meaning. With a `problemTypeBase` of `https://claimwick.example/problems/`, its problem type is the base followed by the code part after the module, and without a base it is the short name, the part after the module.

**Checked by:** `litespec model check` reports `reference-unknown` for a result refusal missing from its module's refusal set, `record-placement-invalid` for a module-level refusal raised by exactly one aggregate, and `code-placement-drift` for a module-level refusal carried outside `Domain/` or directly in `Domain/`.

### End Every Use Case in Named Results

**ID:** `litespec/core/use-cases#end-every-use-case-in-named-results`

**Strength:** required

**Applies to:** artifact: record:use-case, record:result

**Statement:** Each use case MUST declare its success result and, on every refusal result, its `refusal`.

**Rationale:** Callers branch on named endings: success, or one refusal result with its stable code. Without a named success, readers guess what good looks like. Without a refusal id on a refusal result, each architecture layer reports the same breach another way.

**Example:** The reimburse-expense command declares `reimbursed`, with the refusal results citing `refusal/spending.not-approved` and `refusal/spending.already-reimbursed`.

**Checked by:** `litespec model check` reports `use-case-success-missing` for a use case with no success result; each refusal result on the use-case page carries its `refusal`.

### Derive Generic Refusals Instead of Writing Them

**ID:** `litespec/core/use-cases#derive-generic-refusals-instead-of-writing-them`

**Strength:** required

**Applies to:** artifact: record:result

**Statement:** A use case MUST NOT write a generic refusal as a record.

**Rationale:** Hand-written generic refusals multiply: hundreds of pages each declare their own forbidden, and each drifts in wording and code. Deriving them keeps one of each per use case, current with the grant, the inputs, the target, the state, the knowledge, the caller, and the sign-in.

**Example:** The library takes `rule/library.unknown-copy` on the id input of each use case loading a copy. Each use case derives `result/<use-case>.not-found` carrying `refusal/library.unknown-copy`, so a handler throwing `library.unknown-copy` is declared with no written result.

**Checked by:** `litespec model check` reports `refusal-generic-written` for a use case writing a generic refusal as a record.

### Name Only a Provided Way Out in Each Refusal Sentence

**ID:** `litespec/core/use-cases#name-only-a-provided-way-out-in-each-refusal-sentence`

**Strength:** advisory

**Applies to:** artifact: record:result

**Statement:** Each refusal sentence that names a next step SHOULD name a use case, screen, setting, or input that exists.

**Deviation:** A way out the product cannot provide, such as visiting a bank branch, stays as prose naming the place.

**Rationale:** The refusal sentence is what the user reads. Where it promises a way out the product lacks, the user follows the advice into the same refusal. Naming only a use case, screen, setting, or input that exists keeps each promised step one the user takes.

**Example:** Claimwick's closed-report refusal carries "This expense falls in a closed report. Reopen the report and submit again." only where the reopen use case exists; otherwise it carries "This expense falls in a closed report, so it cannot be submitted."

**Checked by:** `litespec model check` reports `refusal-next-step-unknown` (level G) for a refusal sentence naming a `use-case/`, `screen/`, or `setting/` id no record holds, or carrying `instead`, `alternatively`, `you can also`, or `or you can`.

### Name the Result Each Scenario Ends In

**ID:** `litespec/core/use-cases#name-the-result-each-scenario-ends-in`

**Strength:** required

**Applies to:** artifact: record:scenario

**Statement:** Each scenario MUST name the result it ends in, written or derived.

**Rationale:** A scenario without a named result reads well and proves nothing: it illustrates behavior without asserting an ending. Naming the result ties each example to the ending it walks toward, so each test knows what to assert.

**Checked by:** `litespec model check` reports `record-invalid` for a scenario entry without `result`, and `reference-unknown` for a scenario ending in another use case's result. `litespec model check` reports `scenario-result-unknown` for a result id no record declares.

### Declare the Result a Scenario Ends In

**ID:** `litespec/core/use-cases#declare-the-result-a-scenario-ends-in`

**Strength:** required

**Applies to:** artifact: record:scenario

**Statement:** The result a scenario names MUST be declared by the use case: a written result, or one of the generic refusals the model derives.

**Rationale:** A scenario whose ending no result declares asserts an outcome the use case promises nowhere, so its test proves a story nobody wrote down. Naming a declared ending ties each scenario to an outcome the use case owns.

**Example:** A Claimwick scenario for `use-case/spending.reimburse-expense` ending in `result/reimburse-expense.already-reimbursed` needs a written result with that id. Ending in `result/reimburse-expense.not-found` needs none, because the model derives that refusal.

**Checked by:** `litespec model check` reports `scenario-result-unknown` (level G) for a scenario ending in a result id that no result record declares and that is not a derived generic refusal.

### Prove Every Written Result with a Scenario

**ID:** `litespec/core/use-cases#prove-every-written-result-with-a-scenario`

**Strength:** required

**Applies to:** artifact: record:result

**Statement:** At large size or with the `bdd` extension, each written result MUST be shown by at least one scenario of its use case.

**Rationale:** A written result no scenario shows is an ending nobody proved: it reads on the page and is asserted in no test, so it drifts from the code with the next change. One scenario per result pins each ending as a checked fact.

**Example:** The reimburse-expense command declares the `already-reimbursed` refusal. A scenario walks an approved expense claimed twice to that refusal.

**Checked by:** `litespec model check` reports `result-scenario-missing` (level G) for a written result no scenario shows.

### Write Every Scenario in Given When Then

**ID:** `litespec/core/use-cases#write-every-scenario-in-given-when-then`

**Strength:** required

**Applies to:** artifact: record:scenario

**Statement:** Each scenario MUST state its `given`, `when`, and `then`.

**Rationale:** Collapsing the three steps blurs what was true, what moved, and what the test checks. The three steps separate the situation from the action and the action from the assertion. Example rows of one scenario share one set of keys.

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

**Rationale:** A rule proved only through a test mark hides which example walks it: readers open the rule and find no scenario showing it. Naming the shown rules in `exercises` ties each example to the rules it walks, and each rule meets its scenarios. Settled by decision/0101-check-rule-citations.

**Example:** In the library product, a scenario walking a second hold on one copy names `rule/lending.one-hold-per-copy` in `exercises`.

**Checked by:** `litespec model check` reports `scenario-rule-unexercised` for a refusal scenario whose result's rule is not in `exercises`, or an input rule no scenario of the use case exercises, and `exercises-uncited` (level G) for a scenario exercising a rule its use case cites nowhere in its steps, inputs, or results.

### Keep Each Scenario Inside Its Operation

**ID:** `litespec/core/use-cases#keep-each-scenario-inside-its-operation`

**Strength:** required

**Applies to:** artifact: record:scenario

**Statement:** Each scenario MUST assert only its own operation's results, state, and raised events in `then`.

**Rationale:** A scenario asserting a later operation effect ties two operations in one test. Where the first operation changes, the second breaks, and the failure points at the wrong record. Asserting only the scenario's own results, state, and raised events keeps each test on its record. Settled by decision/0081-own-scenarios-and-declare-outcomes and decision/0079-cover-the-behavior-reviewers-found-missing.

**Example:** The submit-expense scenario asserts the stored expense and its submitted fact. It leaves the approver payout to the flow test walking both operations.

**Reviewed for:** Does every scenario assert only its own operation in `then`? Look at `then` beside the scenario's own operation.

### Give Each Rule Boundary Its Own Scenario

**ID:** `litespec/core/use-cases#give-each-rule-boundary-its-own-scenario`

**Strength:** required

**Applies to:** artifact: record:scenario

**Statement:** Each rule boundary a use case exercises MUST have its own scenario at the boundary.

**Rationale:** A boundary shown only from the safe side proves nothing about the edge. Expiry, attempt limits, and one-time use each fail exactly at the line. Giving each boundary named in `exercises` its own scenario at the line pins the edge as a checked fact. Settled by decision/0071-give-every-stated-rule-a-field and decision/0079-cover-the-behavior-reviewers-found-missing.

**Example:** The hold expiry rule carries one scenario collecting on the last pickup day, and one collecting after the lapse.

**Reviewed for:** Does every exercised rule boundary have its own scenario at the boundary? Look at `exercises` beside the scenarios of the use case.

### Keep Retired Background Out of Use Cases

**ID:** `litespec/core/use-cases#share-scenario-setup-in-background`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A use case MUST NOT carry `background`: each scenario carries its own `given`, `when`, and `then`, and shared narrative lives in the explanation file beside the record file.

**Rationale:** `background` repeated one Given on each scenario and hid the step that differs, while the same setup lived in two places. Each scenario carrying its own situation, action, and assertion keeps one source of truth.

**Example:** Each approval scenario states that the employee holds a session and one submitted expense awaits approval. No shared `background` holds it once.

**Checked by:** `litespec model check` reports `record-invalid` for a use case carrying `background`.

### Summarize Every Use Case, Actor, Role, and External System

**ID:** `litespec/core/use-cases#summarize-every-use-case`

**Strength:** required

**Applies to:** artifact: record:use-case, record:actor, record:role, record:external-system

**Statement:** Each use case, actor, role, and external system MUST carry `summary`.

**Rationale:** A reader landing cold needs the story before its fields. One paragraph states what the use case is; its steps sit in `steps` in order, and the explanation restates them under Flow. An actor or role `summary` states who the party is and what the grant is for.

**Example:** The submit-expense summary states that an employee files a work expense for approval. Its `steps` list validation, storage, and the approver notice in order, and its Flow section restates them.

**Checked by:** `litespec model review` reports `prose-missing` (level R) for a use case, actor, role, or external system without `summary`.

### Tie Every Setting Default to a Requirement

**ID:** `litespec/core/use-cases#tie-every-setting-default-to-a-requirement`

**Strength:** required

**Applies to:** artifact: record:setting

**Statement:** Each setting MUST declare its value type, range, unit, setter, and bound behavior, with the requirement behind its default.

**Rationale:** A default no requirement holds is a figure nobody decided: a delay, a timeout, or a batch size nobody chose. Declaring the value type, the range, the unit, the setter, and the bound behavior states the tunable beside its promise.

**Example:** The approval-reminder delay declares a duration with an operator-set default of `PT72H`, citing the requirement that each expense is decided within five days.

**Checked by:** `litespec model check` reports `setting-requirement-missing` for a setting default with no cited requirement.

### Name the Settings Each Use Case Reads

**ID:** `litespec/core/use-cases#name-the-settings-each-use-case-reads`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use case MUST name the settings it reads in `reads`.

**Rationale:** A setting a use case reads but no record names is a coupling nobody sees: the tunable moves, the use case reading it breaks, and no diff shows the pair. Naming the settings in `reads` beside the use case states the coupling beside the behavior reading it. Settled by decision/0012-use-cases-read-their-settings.

**Example:** The send-approver-reminders reaction names `setting/spending.approval-reminder-delay` in `reads`. A move of the delay then meets the reaction reading it.

**Checked by:** `litespec model check` reports `setting-unused` for a setting no record `reads`.

### Name the Configuration Key of Every Operator Setting

**ID:** `litespec/core/use-cases#name-the-configuration-key-of-every-operator-setting`

**Strength:** required

**Applies to:** artifact: record:setting

**Statement:** A setting the operator sets MUST name its configuration path in `configurationPath`.

**Rationale:** An operator setting reaches the product through configuration. The record and the configuration meet at one path: the record states the default and the bounds, and the configuration names the value. Without the path, a renamed section silently falls back to a default nobody chose.

**Example:** Claimwick's `setting/spending.approval-reminder-delay` is set by the operator. It carries `configurationPath` `Spending:ApprovalReminderDelay`.

**Checked by:** `litespec model check` reports `setting-key-missing` (level G) for a setting with `setBy: operator` and no `configurationPath`.

### Give Every Tenant Setting a Change Use Case

**ID:** `litespec/core/use-cases#give-every-tenant-setting-a-change-use-case`

**Strength:** required

**Applies to:** artifact: record:setting

**Statement:** A setting with `setBy: tenant` MUST be changed by a `change-*` use case that names it in `reads`.

**Rationale:** A tenant setting holds configuration the tenant administrator changes while the system runs. Without a use case changing it, the value is fixed and the default turns into a permanent figure. Naming the setting in `reads` of the `change-*` use case states who changes it and under which access.

**Example:** Claimwick's `setting/spending.claim-approval-limit` is set by the tenant, and `use-case/spending.change-claim-approval-limit` names it in `reads`. A tenant setting with no such use case is reported.

**Checked by:** `litespec model check` reports `tenant-setting-change-missing` (level G) for a setting with `setBy: tenant` that no `change-*` use case names in `reads`.

### Bind Every Keyed Setting in One Marked Element

**ID:** `litespec/core/use-cases#bind-every-keyed-setting-in-one-marked-element`

**Strength:** required

**Applies to:** artifact: record:setting; layer: implementation; trace: yes

**Statement:** A setting with a `configurationPath` MUST be bound in code by one element that carries an `implements` mark naming the setting and whose name spells the path.

**Rationale:** A setting reaches the running product through configuration, and the code reads it as a typed value. The code element holding that value is where the record and the configuration meet, so marking it with the setting it implements names the promise the element holds.

**Example:** Claimwick's approval-reminder-delay setting carries `configurationPath` `Spending:ApprovalReminderDelay`. One element carries an `implements` mark naming `setting/spending.approval-reminder-delay`, and its name spells the path.

**Checked by:** `litespec model check` reports `setting-unimplemented` (level S) for a keyed setting no marked element implements, and `setting-key-drift` (level S) for a marked element whose name does not spell the setting's `configurationPath`.

### State the Unit of Every Quantity Setting

**ID:** `litespec/core/use-cases#state-the-unit-of-every-quantity-setting`

**Strength:** required

**Applies to:** artifact: record:setting

**Statement:** Each setting whose value is a number or a duration MUST declare its `unit`.

**Rationale:** A number without a unit is a guess. Minutes read as seconds, and a count reads as money. Declaring the unit states the measure beside the value.

**Example:** The retry backoff declares `unit: seconds`. A timeout with `value.type: duration` and no unit is reported.

**Checked by:** `litespec model check` reports `setting-unit-missing` for a numeric or duration setting declaring no `unit`.

## Conventions

None.
