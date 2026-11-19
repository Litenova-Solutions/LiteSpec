# Domain Model

**Stack:** any
**Concern:** domain

The Domain Model page states the Behavior records that hold the product's business language inside the bounded context: the modules that group it, the glossary that defines it, the aggregates with their lifecycles, the rules that hold, and the domain events that announce change. Agents write these records from Intent, and code carries what they declare. The [Behavior overview](behavior.md) maps the five Behavior pages, and the [record language page](language.md) states how each field, value, and record name is written.

## Concepts

### Bounded Context and Module

Inside one deployment boundary, the product speaks one business language, as the [scope page](scope.md) states. Modules organize that language into areas. A module stays an area of the one language and the one deployment, never a language or a deployment of its own.

A module is one business area. Its record sits at `behavior/modules/<m>/module.yaml` and carries a `summary` and the `dependsOn` links to the modules it relies on, as `litespec/core/behavior#shape-each-module-page` states, in the home the [model page](model.md) names. Intent holds no module: each module is declared only in Behavior, beside the aggregates it groups.

Each child record sits in a typed folder inside its module, such as `aggregates/<aggregates>/`, `aggregates/<aggregates>/integration-events/<name>/`, or `rules/`, as the [model page](model.md) places each type. The aggregate folder name is the plural of the aggregate name, while the record id stays singular. In Claimwick, the spending module groups the expense aggregate and the rules that govern it. The explanation file beside the record restates Covers from `covers` and Boundaries from `excludes` and carries Example.

| Field | Meaning |
|:---|:---|
| `summary` | What the area covers, in one paragraph. |
| `dependsOn` | The modules this module relies on. |
| `covers` | What the area covers, one sentence per covered area. |
| `excludes` | What the area does not cover, one sentence per exclusion. |

### Glossary

The glossary holds the project's ubiquitous language: the one word or phrase the project uses for each concept, with its meaning. Each term carries `layer`, which says whose vocabulary the word belongs to: `intent` for business words, `behavior` for design words such as aggregate, refusal, reaction, and job, and `implementation` for design words that live in code, such as a read model.

Each term lists in `aliases` the established synonyms its readers know, so industry readers find LiteSpec's own words without learning a second vocabulary first: a refusal is a rejection or error, a reaction is a policy, and a compensation is an undo. The `rejected` list bars the synonyms the project refuses, as the [authoring page](authoring.md) states.

A term that declares a project use-case verb carries `verbKind` with the kind it opens: `command`, `query`, or `either`. For example, Claimwick defines `receipt` as proof of payment accepted for an expense.

| Field | Meaning |
|:---|:---|
| `term`, `meaning` | The one word and what it means. |
| `layer` | `intent`, `behavior`, or `implementation`. |
| `verbKind` | The use-case kind a verb term opens, where the term declares a verb. |
| `aliases` | Established synonyms readers know. |
| `rejected` | Synonyms barred in their scope. |
| `broader`, `narrower`, `values` | The wider and narrower terms, and the closed values. |
| `labels` | The term in each interface language beside English, keyed by its two-letter language code. |

### Aggregates

An aggregate is the product's consistency boundary: the state that stays valid together, guarded by its invariants and changed in one step through its root. Its identifier carries its module, so containment follows the identifier and needs no link field. Claimwick's expense aggregate carries states such as Approved and Reimbursed. An aggregate with no lifecycle declares the single state `Active`.

An aggregate lists in `parts` the kebab-case names of the child entities and value objects inside it that use cases act on, such as `line`, `hold`, and `note`. The use-case noun check reads each part as a known word. The optional `excludes` field states what the aggregate does not do, one sentence per exclusion. The explanation file beside the record restates Excludes from `excludes` and carries Lifecycle, Data and Retention, and Example.

`transitions` lists every allowed change with the use case or job moving it. Each entry names the state left in `from`, the state entered in `to`, and the moving use case or job in `movedBy`. A creation, the change that brings an instance into being, writes `-` as the state left. A transition moved by time carries `after` with the ISO 8601 duration counted from entering its source state, and `movedBy` names the job.

`data` lists the fields the aggregate keeps, each with its name, meaning, classification, and data class. Each data class declares how its fields erase and how long they are kept:

| Field | Meaning |
|:---|:---|
| `name`, `classification`, `fields` | The class, its `public`, `internal`, `personal`, or `sensitive` handling, and its fields. |
| `purpose` | Why the data is kept. |
| `onErasure` | `erase`, `pseudonymize`, `anonymize`, `retain`, or `exempt`. |
| `basis` | The constraint, requirement, and decision identifiers behind the retention. |
| `period`, `periodStartsAt` | The retention period as an ISO 8601 duration, or `{ "mode": "none", "reason": "..." }`, with the date it counts from: a `domain-event/`, `module-event/`, or `integration-event/` identifier, an aggregate date field as `aggregate/<module>.<aggregate>#<field>`, a state transition written `<state> to <state>`, `financial-year-end` or `calendar-year-end`, or `{ "event": ..., "rounding": ... }` counting from an event or field rounded up to a calendar end. |
| `holds` | Pauses to deletion, each with its `reason`, the `heldOn` date the hold starts, and the `liftedOn` date the hold ends, left out while the hold stands. |
| `enforcedBy` | The `job/` or `use-case/` identifiers carrying out the erasure. |
| `reach` | `own-tenant`, `subject-wide`, or `platform-wide`. |
| `exceptions` | Per-field erasure positions differing from the class, and the retention conflicts with the winning duty and its basis. |
| `reason`, `basedOn` | The reason of an exemption, and the Intent records accepting retained or exempt personal data. |

### Rules

A rule states what holds, with its `kind` and its `statement`. It names what it constrains in `governs`, the refusal its breach produces in `refusal` by the refusal's id, the requirements it carries in `fulfills`, and the constraints, research, and decisions behind it in `basedOn`. It states whether it restates the law, a contract, or a product choice in `source`, and a rule restating the law names the restated constraint or research record in `cites`. Its optional `rationale` states why the rule holds. A rule an organization chooses, such as rounding, consent, or seller responsibility, is a requirement or a constraint in Intent, not a Behavior rule.

A rule's scope comes from its home. An invariant an aggregate enforces lives under its aggregate or its module, and a module-level rule over several aggregates names them in `binds`. A module rule lives in `behavior/modules/<m>/rules/`, while `module.yaml` holds no rules.

A rule page carries a `summary` stating why its rules sit together, and its explanation file holds Notes. No rule page restates another record type. A rule identifier keeps its segment when the rule moves; only identifiers whose first segment names a module change with a module move.

State one rule per condition: one rule states one condition and names its refusal from its aggregate's refusal set, and every use case that can break it lists the refusal result citing that refusal. For example, Claimwick's reimbursed-once rule states one condition with its refusal, and every reimbursement lists the refusal result citing that refusal.

| Kind | Meaning |
|:---|:---|
| `invariant` | Holds after every change. |
| `precondition` | Guards the behavior before it runs. |
| `calculation` | Derives a value, proven by an examples table. |
| `atomicity` | Names one atomic write group in `binds`. |
| `reservation` | Holds a claim until a use case releases it, named in `releasedBy` as a use-case id, or `{ "mode": "never", "reason": "..." }` with its cost. |
| `frozen-value` | Freezes a commercial value, with `frozenWhen` and `retainedIn`. |

When a query shows a value and a command records it, both cite the same calculation rule in `computedBy`. Code carries a mark naming the rule; the rule never names code.

### Reference Data

Reference data is a table of dated business values the running system looks up: rates, thresholds, deadlines, and time zones with the dates each row takes effect. It carries the identifier `reference-data/<module>.<name>` and lives at `.litespec/behavior/modules/<module>/reference-data/<name>/<name>.yaml`, under the module that owns the values. A setting holds one operator configuration value, aggregate data holds what the business changes while the system runs, and reference data holds the dated tables rules look up.

Rules cite the table instead of stating its values: a use case names it in `reads` and a step cites it in `cites`, and a rule statement names it by its identifier. The model checks that every cited table exists and that no two rows overlap in time.

| Field | Meaning |
|:---|:---|
| `summary`, `unit` | What values the table holds and who reads them, and the unit of each value, such as EUR. |
| `rows` | One entry per dated value, each with its `value`, the `effectiveFrom` date it takes effect, the `effectiveTo` date it stops applying left out while the row stands, and the `source` naming the constraint, research, or decision behind the value. |

### Domain Events

A domain event is an immutable statement that a business fact completed. It is named in the past tense, with its `meaning` and the facts it `carries`. Each event is raised through the root of its aggregate, and each event stays inside the product: a fact crossing the boundary is an integration event, not a domain event. Claimwick's expense aggregate raises `domain-event/expense.submitted` when a submission completes.

| Field | Meaning |
|:---|:---|
| `meaning` | What happened, in one sentence. |
| `carries` | The typed fields arriving with the event, each with `name` and `type` from the input value types. An integration event publishes only fields its translated domain event carries. |

## Standards

### Define Every Behavior Term in the Glossary

**ID:** `litespec/core/domain-model#define-every-behavior-term-in-the-glossary`

**Strength:** required

**Applies to:** artifact: record:term

**Statement:** Each term a Behavior record names MUST have a glossary entry stating its meaning.

**Rationale:** Records and code share one language only when each word is defined once. A term used without an entry means whatever each reader guesses, while the entry fixes the meaning, the rejected synonyms, and the wider and narrower terms, so reviewers and checks read the same word the same way.

**Example:** Claimwick's glossary defines `receipt` as "proof of payment accepted for an expense", so every record naming a receipt means that proof.

**Reviewed for:** Does every term the Behavior records name have a glossary entry stating its meaning? Check the records beside the project glossary.

### Mark Each Term with Its Layer

**ID:** `litespec/core/domain-model#mark-each-term-with-its-layer`

**Strength:** required

**Applies to:** artifact: record:term

**Statement:** Each glossary term MUST carry its record layer, intent, behavior, or implementation, with the established synonyms its readers know in `aliases`.

**Rationale:** Each layer's words change for their own reasons: a business term changes when the market changes, a design term when the model changes, and an implementation term when the code changes. The record layer tells each reader what kind of change moves the word, and the aliases let industry readers find LiteSpec's own words without learning a second vocabulary first.

**Example:** The term `refusal` carries `layer: behavior` with `aliases: ["rejection", "error"]`, so readers who know rejections and errors find it.

**Checked by:** `litespec model check` reports `glossary-invalid` for a glossary term without `layer`, and each behavior term lists the synonyms its readers know in `aliases`.

### Keep Technical State Out of Behavior

**ID:** `litespec/core/domain-model#keep-technical-state-out-of-behavior`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** State that exists only because of a technical mechanism MUST NOT be written as a Behavior record.

**Rationale:** State the business cares about is an aggregate with its invariants. State that exists only because of a technical mechanism, such as outbox rows, idempotency keys, caches, sessions, leases, or provider delivery logs, is infrastructure: a Behavior record for it promises business meaning nothing guards, and it drifts with every mechanism change. The stack and extension pages and technical decisions govern such state instead. When such state holds personal data, its retention is stated where the mechanism is stated. Settled by [decision/0092-module-events-and-business-state](decision/0092-module-events-and-business-state).

**Example:** Claimwick records each manager's reminder choice as `aggregate/spending.reminder-preference`, one per manager with the single state `Active`: a rule states that only the manager or an admin holding the named permission changes it, a use case changes it with its refusals, access, and audit, and its data classes carry an erasure position. The outbox rows carrying the reminder facts are mechanism state, so no Behavior record states them and the outbox extension governs them.

**Reviewed for:** Does any Behavior record state technical mechanism state, such as outbox rows, idempotency keys, caches, sessions, leases, or provider delivery logs? Check the aggregates beside the stack and extension pages and the technical decisions.

### Model Each Aggregate Lifecycle with States

**ID:** `litespec/core/domain-model#model-each-aggregate-lifecycle-with-states`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Each aggregate MUST declare its lifecycle as `states` entries with a name and a meaning, and an aggregate with no lifecycle declares the single state `Active`.

**Rationale:** A bare state name tells a reviewer nothing: `rejected` without its meaning hides what rejection costs and allows. A name with a meaning makes each lifecycle step reviewable on the page and gives `transitions` states to connect. An aggregate with no lifecycle declares the single state `Active`, so every aggregate carries states.

**Example:** The library's hold queue aggregate declares `Requested`, `Ready`, `Collected`, `Cancelled`, and `Lapsed`, each with one sentence saying what the hold awaits or has received. Claimwick's reminder-preference aggregate declares the single state `Active`.

**Checked by:** `litespec model check` reports `record-invalid` for an aggregate page without `states` entries carrying a name and a meaning.

### Connect Every State Through Transitions

**ID:** `litespec/core/domain-model#connect-every-state-through-transitions`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Each aggregate MUST list every allowed state change with the use case or job moving it in `transitions`, including the creation that enters its first state.

**Rationale:** States without transitions list names with no path between them: a reviewer sees where an aggregate can stand but never how it moves. Listing every allowed change with the use case or job moving it in `movedBy` shows the whole lifecycle beside the states, and leaves no state reachable only by a path no record names. A transition moved by time names the job in `movedBy` with the duration from entering its source state in `after`. The `state-unreached` and `reference-unknown` checks report a state no transition reaches and a transition naming a missing state, use case, or job.

**Checked by:** `litespec model check` reports `state-unreached` (level G) for a state reached by no transition, a creation included, and `reference-unknown` (level G) for a transition naming a missing state, or a `movedBy` naming no use case or job.

### State Each Rule with Its Kind

**ID:** `litespec/core/domain-model#state-each-rule-with-its-kind`

**Strength:** required

**Applies to:** artifact: record:rule

**Statement:** Each rule MUST state its kind and its statement.

**Rationale:** The kind decides how the rule is proved: an invariant on every change, a precondition before the behavior, a calculation through its examples, an atomicity rule through its bound group, a reservation through its release, and a frozen-value rule through its frozen value. A rule without a kind leaves each reader proving it a different way, and the checks with no place to confirm it.

**Example:** Claimwick's invariant `rule/expense.reimbursed-once` states "an expense is reimbursed at most once", and every reimbursement checks it.

**Checked by:** `litespec model check` reports `record-invalid` for a rule entry without `kind` or `statement`.

### State What Each Rule Governs

**ID:** `litespec/core/domain-model#state-what-each-rule-governs`

**Strength:** required

**Applies to:** artifact: record:rule

**Statement:** Each rule MUST name what it constrains in `governs`.

**Rationale:** A rule that names no governed record constrains nothing a reader can find. Naming the constrained aggregates and use cases in `governs` pins each rule to its records. The governed records match the links that name the rule: input `rules`, consistency bindings, calculation links, scenario `exercises`, and result refusals. A module rule governs records of its own module; a product-level rule governs records in any module and names each aggregate it governs.

**Example:** The reimbursed-once rule governs the expense aggregate and the reimburse-expense command, matching the refusal results that cite its refusal.

**Checked by:** `litespec model check` reports `rule-governs-drift` for `governs` disagreeing with the links that name the rule.

### Bind Every Atomic Write Group in Its Rule

**ID:** `litespec/core/domain-model#bind-every-atomic-write-group-in-its-rule`

**Strength:** required

**Applies to:** artifact: record:rule

**Statement:** Each atomicity rule MUST name the aggregates it binds in `binds` and the decision behind the group in `basedOn`.

**Rationale:** An atomic write across aggregates stays allowed only through its rule and its decision. The rule states the group and why, such as an order without its hold overselling, names in `basedOn` the decision recording why the invariant needs one transaction, and the use case commits exactly that group, as the [use-cases page](use-cases.md) states. A group above 4 aggregates is a coordination candidate, and the check reports it at level A.

**Example:** The library's hold rule binds the hold and the copy aggregates, so a hold without its copy never stands.

**Checked by:** `litespec model check` reports `atomicity-drift` (level G) for an atomic use case whose `writes` differs from its rule's `binds`, and `atomicity-invalid` (level A) for a group binding more than 4 aggregates.

### Name Every Spanned Aggregate in Binds

**ID:** `litespec/core/domain-model#name-every-spanned-aggregate-in-binds`

**Strength:** required

**Applies to:** artifact: record:rule

**Statement:** Each rule whose `governs` names more than one aggregate MUST name every governed aggregate in `binds`.

**Rationale:** No rule across aggregates is held by one aggregate alone: a state guard or an atomicity rule holds it. Naming every reached aggregate in `binds` shows the rule's reach beside the rule, so a reader sees which aggregates change together.

**Example:** `rule/spending.receipt-filed-once` states that a receipt file supports one expense across spending, and names `aggregate/spending.expense` and `aggregate/spending.receipt` in `binds`.

**Checked by:** `litespec model check` reports `rule-binds-missing` (level G) for a rule whose `governs` names more than one aggregate while `binds` names none or not all of them.

### Keep the Negation Word in the Rule Name

**ID:** `litespec/core/domain-model#keep-the-negation-word-in-the-rule-name`

**Strength:** required

**Applies to:** artifact: record:rule

**Statement:** A rule whose statement says `not`, `no`, `never`, or `cannot` MUST keep a restricting word in its name: `no`, `never`, `not`, `cannot`, `none`, `nothing`, `without`, `unless`, `non`, `refused`, `refuses`, `refusal`, `forbidden`, `blocked`, or `rejected`, a limit `only`, or a uniqueness `one-...-per-...`.

**Rationale:** A name that drops the negation reads as its own opposite: a rule named `cancel-after-shipment` whose statement says "never" allows what it forbids. A restricting word keeps the name a true sentence, which a reviewer trusts without opening the statement. A uniqueness name such as `one-grant-per-account` already says what "an account never holds two grants" says.

**Example:** The rule is named `rule/order.cancel-never-after-shipment`.

**Checked by:** `litespec model check` reports `rule-name-invalid` for a rule whose statement says `not`, `no`, `never`, or `cannot` while its name keeps no restricting word or uniqueness.

### Name the Refusal a Broken Rule Produces

**ID:** `litespec/core/domain-model#name-the-refusal-a-broken-rule-produces`

**Strength:** required

**Applies to:** artifact: record:rule

**Statement:** Each rule whose breach refuses a use case MUST name the refusal in `refusal` by the refusal's id.

**Rationale:** A guarded rule without a named refusal leaves the ending unnamed, and callers branch on a refusal the records never predicted. Naming the refusal from its aggregate's refusal set ties the rule to every result citing that refusal, so the breach and the ending stay one fact. The [Use Cases page](use-cases.md) states the refusal set.

**Reviewed for:** Does every guarded rule name its refusal in `refusal`? Check the rule entries beside the results citing their refusal.

### State Each Rule's Source

**ID:** `litespec/core/domain-model#state-each-rules-source`

**Strength:** advisory

**Applies to:** artifact: record:rule

**Statement:** Each rule SHOULD state in `source` whether it restates the law, a contract, or a product choice.

**Deviation:** A rule whose statement already states its origin keeps no `source`.

**Rationale:** A product simplification later gets mistaken for law: a future maintainer reads a rule that claims legal backing and will not change it, even when the product owner could, or changes a real legal rule believing it is a product choice. Stating the source keeps each kind changeable on its own terms: the law changes with the statute, and a product choice changes with the owner. Settled by [decision/0099-rule-sources](decision/0099-rule-sources).

**Example:** Claimwick's erased-after-retention rule states `law` and cites the tax constraint. A rule making a product choice states `product-choice` with no `cites`.

**Reviewed for:** Does each rule that restates an outside rule or makes a product choice state its source? Check `source` on the rule entries.

### Name the Record Each Law Rule Restates

**ID:** `litespec/core/domain-model#name-the-record-each-law-rule-restates`

**Strength:** required

**Applies to:** artifact: record:rule

**Statement:** A rule with `source: law` MUST name the restated constraint or research record in `cites`.

**Rationale:** A law claim without its record cannot be checked: no reader can tell which outside rule the statement restates, so a change to the outside rule never reaches the rule. Naming the restated record ties each law rule to the constraint or research it carries, and the model checks the link resolves. Settled by [decision/0099-rule-sources](decision/0099-rule-sources).

**Example:** Claimwick's erased-after-retention rule cites `constraint/tax-receipt-retention`, the tax rule its seven years restate.

**Checked by:** `litespec model check` reports `record-invalid` for a rule with `source: law` and no `cites`.

### Release Every Reservation by a Named Use Case

**ID:** `litespec/core/domain-model#release-every-reservation-by-a-named-use-case`

**Strength:** required

**Applies to:** artifact: record:rule

**Statement:** Each reservation rule MUST name the use case releasing it in `releasedBy`, or `{ "mode": "never", "reason": "..." }` with its cost.

**Rationale:** A held claim with no release keeps places, funds, or names locked past their need, and nobody owns freeing them. Naming the releasing use case states the end of the hold beside its start. A hold that truly lasts is admitted with `{ "mode": "never", "reason": "..." }` and its cost on record. Persistence claims the added keys and releases the removed ones in the commit that changes the aggregate (`litespec/backend/persistence#claim-a-cross-record-key-at-commit`).

**Checked by:** `litespec model check` reports `reservation-release-missing` for a reservation rule whose `releasedBy` names neither a use case nor `{ "mode": "never", "reason": "..." }` with its cost.

### Record When Each Frozen Value Freezes and Where It Lives

**ID:** `litespec/core/domain-model#record-when-each-frozen-value-freezes-and-where-it-lives`

**Strength:** required

**Applies to:** artifact: record:rule

**Statement:** Each frozen-value rule MUST name when the value freezes in `frozenWhen` and where its retained facts live in `retainedIn`.

**Rationale:** A price, fee, or rate read live changes under settled business: a refund follows today's price instead of the price at sale. Naming when the value freezes pins each reading to its moment, and the retained link keeps the earlier values reachable for audit and dispute.

**Checked by:** `litespec model check` reports `record-invalid` for a frozen-value rule without `frozenWhen` or `retainedIn`.

### Compute Shared Values from One Calculation Rule

**ID:** `litespec/core/domain-model#compute-shared-values-from-one-calculation-rule`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A value shown by a query and recorded by a command MUST cite the same calculation rule in `computedBy` on both use cases.

**Rationale:** Two computations of one value drift: the screen shows one figure while the ledger records another, and each side cites its own code. Citing the same calculation rule on both use cases keeps one computation behind the shown and the recorded value. Code carries a mark naming the rule; the rule never names code.

**Reviewed for:** Do the query and the command behind one value cite the same calculation rule in `computedBy`? Check the use cases beside the calculation rule.

### Tabulate Every Calculation with Examples

**ID:** `litespec/core/domain-model#tabulate-every-calculation-with-examples`

**Strength:** required

**Applies to:** artifact: record:rule

**Statement:** Each calculation rule MUST be proven by a scenario carrying an `examples` table.

**Rationale:** A calculation stated only as a sentence hides its edge cases, so reviewers agree on the words while each pictures other numbers. An `examples` table shows those edge cases, and the scenario proving the rule pins them as checked facts.

**Checked by:** `litespec model check` reports `calculation-table-missing` for a calculation rule with no scenario examples table.

### State Each Event with Meaning and Carries

**ID:** `litespec/core/domain-model#state-each-event-with-meaning-and-carries`

**Strength:** required

**Applies to:** artifact: domain-event

**Statement:** Each event MUST state its meaning and the typed fields it carries.

**Rationale:** An event without a meaning is a name every reaction interprets alone, while facts without a carries list are reconstructed from code at each reaction. Stating the meaning with the typed `carries` fields lets each reaction trust what happened and what arrived with it. An integration event publishes only fields its translated domain event carries.

**Checked by:** `litespec model check` reports `record-invalid` for an event entry without `meaning` or `carries`, and `event-field-uncarried` for an integration-event payload field its translated domain event does not carry.

### Name Every Event in the Past Tense

**ID:** `litespec/core/domain-model#name-every-event-in-the-past-tense`

**Strength:** required

**Applies to:** artifact: domain-event

**Statement:** Each domain event MUST be named in the past tense.

**Rationale:** An event names a fact that completed, so the past tense tells a reader the business already moved: `submitted` reports, while `submit` orders. The past tense also pairs each event with the act raising it.

**Example:** Claimwick's expense aggregate raises `domain-event/expense.submitted` for a completed submission.

**Reviewed for:** Does every event name read as a completed fact rather than an order? Check the event entries beside their raising acts.

### Raise Every Event Through Its Aggregate Root

**ID:** `litespec/core/domain-model#raise-every-event-through-its-aggregate-root`

**Strength:** required

**Applies to:** artifact: domain-event

**Statement:** Each event MUST be raised through its aggregate root.

**Rationale:** The root is the one type through which outside behavior changes the aggregate, so it sees every change worth announcing. Raising each event there keeps the fact and its announcement in one step, and no event is announced for a change the aggregate never made.

**Reviewed for:** Is every event raised through the root of its aggregate? Check the aggregate code beside the event entries.

### Keep Model Invariants in Rules

**ID:** `litespec/core/domain-model#keep-model-invariants-in-rules`

**Strength:** required

**Applies to:** artifact: record:rule

**Statement:** A model invariant MUST live as a rule, never as a setting.

**Rationale:** A setting tunes and a rule holds. An invariant filed as a setting reads as tunable, and a later operator tunes away what the model guaranteed. Keeping invariants in rules keeps what holds apart from what varies.

**Reviewed for:** Does any setting page hold a model invariant? Check the settings beside the rules.

### Date Every Reference-Data Row

**ID:** `litespec/core/domain-model#date-every-reference-data-row`

**Strength:** required

**Applies to:** artifact: record:reference-data

**Statement:** Each reference-data row MUST state the date its value takes effect in `effectiveFrom` and the record behind its value in `source`.

**Rationale:** A value without its date cannot be looked up for a past moment, and a value without its source cannot be defended or updated when the outside rule moves. The table keeps each value once with both, so every lookup answers with the value in force and the record behind it.

**Example:** Claimwick's decision-target row states five business days from 2026-02-10 with `research/approval-time-benchmarks` as its source.

**Checked by:** `litespec model check` reports `record-invalid` for a reference-data row without `effectiveFrom` or `source`.

### Keep Reference-Data Rows from Overlapping

**ID:** `litespec/core/domain-model#keep-reference-data-rows-from-overlapping`

**Strength:** required

**Applies to:** artifact: record:reference-data

**Statement:** The validity periods of the rows of one reference-data table MUST NOT overlap.

**Rationale:** Two rows in force on one date give two answers to one lookup, and the reader guesses which holds. One row in force per date keeps every lookup single-valued. A row without `effectiveTo` stays in force, so it overlaps any later row until it gains an end date.

**Example:** A second decision-target row starting 2027-01-01 needs the first row to end with `effectiveTo: 2026-12-31`.

**Checked by:** `litespec model check` reports `reference-data-period-invalid` for rows whose validity periods overlap, or whose `effectiveTo` is no later than `effectiveFrom`.

## Conventions

### Name Each Module After Its Capability

**ID:** `litespec/core/domain-model#name-each-module-after-its-capability`

**Strength:** default

**Applies to:** artifact: record:module

**Default:** Name each module after one capability the software offers, not after a company function or a catch-all, and split a module whose aggregates serve more than one capability into sibling modules.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** Modules mark the areas of the bounded context: where a monolith would later split, and how a reader finds a use case by module, then aggregate, then use case. A capability name keeps one module cohesive. A company function or a catch-all such as `platform`, `sales`, `operations`, or `misc` gathers unrelated aggregates that change for unrelated reasons. Splitting such a module into siblings keeps each module one capability with aggregates that serve it.

**Example:** A finance module holds payout and ledger aggregates serving finance, while a misc module holding lending and catalog aggregates splits into lending and catalog siblings.

**Reviewed for:** Does every aggregate in this module serve the capability the module name states? Check the aggregates beside the module name.
