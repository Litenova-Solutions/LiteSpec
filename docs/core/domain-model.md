# Domain Model

## Intent

Domain Model states the words and things inside the bounded context: the modules, the glossary, the aggregates with their lifecycles, the sagas coordinating them, the rules that hold, and the domain events. Agents write these records from Intent, and developers review them before code carries them. The [Behavior overview](behavior.md) maps the five Behavior pages, and the [record language page](language.md) states how each field, value and record name is written.

## Concepts

### Bounded Context and Module

The product speaks one business language inside one deployment boundary, as the [scope page](scope.md) states. Modules organize that language into areas; they stay areas of one language and one deployment, never languages or deployments of their own. A module is one business area: a page under the behavior docs carrying a `summary` and the `dependsOn` links to the modules it relies on, in the home the [model page](model.md) names.

| Field | Meaning |
|:---|:---|
| `summary` | What the area covers, in one paragraph. |
| `dependsOn` | The modules this module relies on. |

### Glossary

The glossary is the project ubiquitous language: the one word or phrase the project uses for each concept, with its meaning. Each term carries `layer`: intent for business words, behavior for design words such as aggregate, refusal, reaction, and job, and implementation for design words that live in code, such as a read model. Each term lists in `aliases` the established synonyms its readers know, so industry readers find LiteSpec own words: a refusal is a rejection or error, a reaction is a policy, a saga is a process manager, a compensation is an undo. The `rejected` list still bars the synonyms the project refuses, as the [authoring page](authoring.md) states. A term carries the status vocabulary of its record layer, as the [model page](model.md) states.

| Field | Meaning |
|:---|:---|
| `term`, `meaning` | The one word and what it means. |
| `layer` | `intent`, `behavior`, or `implementation`. |
| `aliases` | Established synonyms readers know. |
| `rejected` | Synonyms barred in their scope. |
| `broader`, `narrower`, `values` | The wider and narrower terms, and the closed values. |
| `labels` | The term in each interface language beside English, keyed by its two-letter language code. |
| `owner`, `status`, `decider`, `decideBy`, `approvedBy`, `approvedOn`, `approvedHash` | The common fields every record carries. |

### Aggregates

An aggregate is one thing the product keeps consistent and changes in one step, through its root. Each aggregate declares one kind. The aggregate identifier carries its module, so containment follows the identifier and needs no link field.

| Kind | Meaning |
|:---|:---|
| `entity` | One thing kept consistent through its root, with states, transitions, and data. |
| `saga` | A coordinator whose steps run use cases on other aggregates, each with its compensation. |
| `store` | State no aggregate invariant guards, such as a library-owned table, with a named owner. |

An aggregate lists in `parts` the kebab-case names of the child entities and value objects inside it that use cases act on, such as `line`, `hold`, and `note`. The use-case noun check reads each part as a known word.

Transitions and data live in frontmatter, in `transitions` and `data`. The Transitions table lists every allowed change with the use case moving it, naming the state left and the state entered (a creation, the change that brings an instance into being, writes `-` as the state left, or leaves the From cell empty), and the Data table lists the fields the aggregate keeps with name, meaning, classification, and data class; both tables render as generated regions from the frontmatter. Each data class declares how its fields erase:

| Field | Meaning |
|:---|:---|
| `name`, `classification`, `fields` | The class, its `public`, `internal`, `personal`, or `sensitive` handling, and its fields. |
| `purpose` | Why the data is kept. |
| `onErasure` | `erase`, `pseudonymize`, `anonymize`, `retain`, or `exempt`. |
| `basis` | `statute`, `business`, or `engineering`. |
| `period` | The retention period as an ISO duration, or `{ "mode": "none", "reason": "..." }`. |
| `enforcedBy` | `reaction/...`, `job/...`, `record-expiry`, `store-ttl`, or `query-filter`. |
| `reach` | `own-tenant`, `subject-wide`, or `platform-wide`. |
| `exceptions` | Per-field erasure positions differing from the class. |
| `reason`, `reviewBy`, `acceptedBy` | The reason and review date of an exemption, and the Intent record accepting retained or exempt personal data. |

### Sagas

A saga is an aggregate that coordinates work across aggregates: a refund, a resale, a hiring pipeline. Its steps run in order, each running one use case on another aggregate; each step carries an `id`, the use case in `runs`, its compensation, and an optional `guard` clause completing "when ...". Its exits name the ways out, each a past participle or adjective; its `finishedWhen` names the observable end; its `drivenBy` names what moves it; and its `cursor` names where resume state lives.

| Field | Meaning |
|:---|:---|
| `steps` | Ordered `{ id, runs, compensation, guard }` entries; `runs` names `use-case/...`; `compensation` is `{ "mode": "use-case", "useCase": "use-case/..." }` or `{ "mode": "none", "reason": "..." }`; `guard` is optional. |
| `exits` | `{ name, trigger, then }` entries; the name is a past participle or adjective; a trigger is a condition, a `decision by <actor>`, or a `due <duration>`. |
| `finishedWhen` | The observable end, in one sentence. |
| `drivenBy` | `{ "job": "job/<...>" }`, `{ "reaction": "reaction/<...>" }`, or `{ "person": "<actor or role id>", "runbook": "runbook/<...>", "dueWithin": "<ISO duration>" }` entries. |
| `cursor` | Where resume state lives. |

### Rules

A rule states what holds, with its `kind` and its `statement`. A rule names what it constrains in `governs`, the refusal its breach produces in `refusal`, the requirements it carries in `fulfills`, and the constraints and research behind it in `basedOn`. A cross-aggregate rule spans aggregates and lives on a rule page; each other rule lives as an entry on its aggregate page.

| Kind | Meaning |
|:---|:---|
| `invariant` | Holds after every change. |
| `precondition` | Guards the behavior before it runs. |
| `calculation` | Derives a value, proven by an examples table. |
| `cross-aggregate` | Spans aggregates and lives on a rule page. |
| `atomicity` | Names one atomic write group in `binds`. |
| `reservation` | Holds a claim until a use case releases it, named in `releasedBy` as a use-case id, or `{ "mode": "never", "reason": "..." }` with its cost. |
| `commercial-data` | Freezes a commercial value, with `frozenWhen` and `history`. |
| `default` | States a model default. |

A query and a command showing and recording one value both cite the same calculation rule in `computedBy`. Code carries a mark naming the rule, and never the reverse.

### Domain Events

A domain event is an immutable statement that a business fact completed, named in the past tense, with its `meaning` and the facts it `carries`. Each event is raised through the root of its aggregate, and each event stays inside the product. Facts crossing the boundary are integration events.

| Field | Meaning |
|:---|:---|
| `meaning` | What happened, in one sentence. |
| `carries` | The facts arriving with the event. |

## Standards

### Define Every Behavior Term in the Glossary

**ID:** `litespec/core/domain-model#define-every-behavior-term-in-the-glossary`

**Statement:** Each term a Behavior record names MUST have a glossary entry stating its meaning.

**Rationale:** Records and code share one language only when each word is defined once: a term used without an entry means whatever each reader guesses. The entry fixes the meaning, the rejected synonyms, and the wider and narrower terms, so reviewers and checks read the same word the same way.

**Example:** The term `receipt` carries the meaning "proof of payment accepted for an expense" in the glossary, so every record naming a receipt means that proof.

**Reviewed for:** Which terms do the Behavior records name without a glossary entry stating their meaning? Look at the records beside the project glossary.

### Mark Each Term with Its Layer

**ID:** `litespec/core/domain-model#mark-each-term-with-its-layer`

**Statement:** Each glossary term MUST carry its record layer, intent, behavior, or implementation, with the established synonyms its readers know in `aliases`.

**Rationale:** Business words, design words, and implementation words age on different clocks: a business term moves when the market moves, a design term moves when the model moves, and an implementation term moves when the code moves. The record layer tells each reader which clock the word follows, and the aliases let industry readers find LiteSpec own words without learning a second vocabulary first.

**Example:** The term `saga` carries `layer: behavior` with `aliases: ["process manager"]`.

**Checked by:** `litespec consumer` fails a glossary term without `layer`, and each behavior term lists the synonyms its readers know in `aliases`.

### State Every Module Dependency

**ID:** `litespec/core/domain-model#state-every-module-dependency`

**Statement:** Each module MUST name every module it depends on in `dependsOn`.

**Rationale:** A module relies on the language and behavior of others, and each reliance shapes change: editing a module ripples into everything depending on it. Naming every reliance in `dependsOn` shows that ripple as data instead of leaving it for readers to reconstruct from prose.

**Reviewed for:** Which modules rely on a module that `dependsOn` does not name? Look at the module pages beside the modules they rely on.

### Declare the Kind of Every Aggregate

**ID:** `litespec/core/domain-model#declare-the-kind-of-every-aggregate`

**Statement:** Each aggregate MUST declare its kind: entity, saga, or store.

**Rationale:** The kind decides what the page carries: an entity carries a lifecycle, a saga carries steps with compensations, exits, and drivenBy, and a store carries an owner with no guarded invariant. Without the kind, a thing kept consistent reads the same as a table the product only holds.

**Example:** The refund saga declares `kind: saga`, and the suppression list declares `kind: store`.

**Checked by:** `litespec consumer` fails an aggregate page without `kind` or with an unknown kind.

### Name the Owner of Every Store

**ID:** `litespec/core/domain-model#name-the-owner-of-every-store`

**Statement:** Each store MUST name its owning system or module in `owner`.

**Rationale:** A store holds state no aggregate invariant guards, so nothing in the model explains who answers for its shape: a library-owned table, a suppression list, or a delivery log. Naming the owner puts one party behind it, and keeps staff actions on it as ordinary use cases instead of unwritten operations.

**Checked by:** `litespec consumer` fails a store aggregate page without `owner`.

### Model Each Aggregate Lifecycle with States

**ID:** `litespec/core/domain-model#model-each-aggregate-lifecycle-with-states`

**Statement:** Each entity and each saga MUST declare its lifecycle as `states` entries with a name and a meaning.

**Rationale:** A bare state name tells a reviewer nothing: `rejected` without its meaning hides what rejection costs and allows. A name with a meaning makes each lifecycle step reviewable on the page, and gives the Transitions table states worth connecting. An entity tracks what the thing is, and a saga tracks where its coordination stands. A store carries no lifecycle, so it carries no states.

**Example:** The hold queue entity declares `Requested`, `Ready`, `Collected`, `Cancelled`, and `Lapsed`, each with one sentence saying what the hold awaits or has received.

**Checked by:** `litespec consumer` fails an entity or saga page without `states` entries carrying a name and a meaning.

### Leave States off Every Store

**ID:** `litespec/core/domain-model#leave-states-off-every-store`

**Statement:** A store MUST carry no states.

**Rationale:** A store holds state no aggregate invariant guards, so a lifecycle on it promises what nothing guards: states claim a reviewed path while any writer moves the rows. The store names its owner instead, as `litespec/core/domain-model#name-the-owner-of-every-store` states, and staff actions on it stay ordinary use cases.

**Example:** The suppression list declares `kind: store` with its owner and no states.

**Checked by:** `litespec consumer` fails a store aggregate carrying `states`.

### Connect Every State Through Transitions

**ID:** `litespec/core/domain-model#connect-every-state-through-transitions`

**Statement:** Each entity and each saga MUST list every allowed state change with its use case in `transitions`, including the creation that enters its first state.

**Rationale:** States without transitions are a list of names with no path between them: a reviewer sees where an aggregate rests but never how it moves. Listing every allowed change with the use case that moves it shows the whole lifecycle beside the states, and leaves no state reachable only by a path no record names. The Transitions table renders `transitions` as a generated region. The `state-unreached` and `transition-unknown` checks report a state no transition reaches and a transition naming a missing state or use case, and `table-drift` reports a handwritten table that differs from the frontmatter.

**Checked by:** `litespec model check` reports `state-unreached` (level G) for a state reached by no transition, a creation included, `transition-unknown` (level G) for a transition naming a missing state or use case, and `table-drift` (level A) for a handwritten Transitions table that differs from `transitions`.

### State Each Saga Step with Its Compensation

**ID:** `litespec/core/domain-model#state-each-saga-step-with-its-compensation`

**Statement:** Each saga step MUST carry an `id`, name the use case it runs in `runs`, and name its compensation as `{ "mode": "use-case", "useCase": "use-case/..." }` or `{ "mode": "none", "reason": "..." }` with its reason.

**Rationale:** A step without a named undo leaves failure handling to the implementer: each handler invents its own reversal, and some invent none. Naming the compensating use case states the reversal beside the step, and `{ "mode": "none", "reason": "..." }` with its reason states why no reversal applies, such as money already moved. The step runs its use case on another aggregate, and it may carry a `guard` clause stating when it runs.

**Example:** The hold-place step runs the place-hold use case and compensates with the release-hold use case, while the settle step carries `{ "mode": "none", "reason": "money has moved" }`.

**Checked by:** `litespec consumer` fails a saga step without `id`, `runs`, or `{ "mode": "use-case", "useCase": "use-case/..." }` or `{ "mode": "none", "reason": "..." }` with its reason.

### Close Every Saga with Named Exits and a Driver

**ID:** `litespec/core/domain-model#close-every-saga-with-named-exits-and-a-driver`

**Statement:** Each saga MUST name its exits with their triggers, its `finishedWhen`, and its `drivenBy`.

**Rationale:** A saga with no named end runs until someone notices: a hold waits forever, an erasure sits half done. Exits with triggers name every way out, each a past participle or adjective, as `litespec/core/language#name-each-record-by-its-types-name-form` states. `finishedWhen` names the observable end in one sentence, and `drivenBy` names what moves the saga: `{ "job": "job/<...>" }`, `{ "reaction": "reaction/<...>" }`, or `{ "person": "<actor or role id>", "runbook": "runbook/<...>", "dueWithin": "<ISO duration>" }`. A driven saga also names its `cursor`, where resume state lives.

**Example:** The return saga exits as cancelled on reader decision and as lapsed at the pickup window, finishes when collected, cancelled, or lapsed, and is driven by the completion job and the librarian.

**Checked by:** `litespec consumer` fails a saga page without exits carrying names and triggers, `finishedWhen`, and `drivenBy`.

### Prove Every Saga Guard and Exit with a Scenario

**ID:** `litespec/core/domain-model#prove-every-saga-guard-and-exit-with-a-scenario`

**Statement:** Each `guard` and each saga exit MUST have a scenario proving it.

**Rationale:** A guard no scenario exercises is a condition nobody checked: it holds in prose and fails in code. A scenario per guard and per exit pins each branch as a checked fact, including the branches where the saga leaves early.

**Reviewed for:** Which saga guards and exits lack a proving scenario? Look on the saga page and its use case pages.

### State Each Rule with Its Kind

**ID:** `litespec/core/domain-model#state-each-rule-with-its-kind`

**Statement:** Each rule MUST state its kind and its statement.

**Rationale:** The kind decides how the rule is proved: an invariant on every change, a precondition before the behavior, a calculation through its examples, a cross-aggregate rule where aggregates meet, an atomicity rule naming its bound group, a reservation through its release, a commercial-data rule through its frozen value, a default through the value the model holds. A rule without a kind leaves each reader to prove it a different way, and the checks no place to confirm it.

**Example:** `rule/expense.reimbursed-once` is an invariant stating "an expense is reimbursed at most once", checked on every reimbursement.

**Checked by:** `litespec consumer` fails a rule entry without `kind` or `statement`.

### Bind Every Atomic Write Group in Its Rule

**ID:** `litespec/core/domain-model#bind-every-atomic-write-group-in-its-rule`

**Statement:** Each atomicity rule MUST name the aggregates it binds in `binds`.

**Rationale:** An atomic write across aggregates stays allowed only through its rule: the rule states the group and why, such as an order without its hold overselling, and the use case commits exactly that group, as the [use-cases page](use-cases.md) states. A group above 4 aggregates is a saga candidate, reported at level A.

**Example:** The hold rule binds the hold and the copy aggregates, so a hold without its copy never stands.

**Checked by:** `litespec model check` reports `atomic-group-drift` (level G) for an atomic use case whose `changes` differs from its rule's `binds`, and `atomic-group-invalid` (level A) for a group binding more than 4 aggregates.

### Keep the Negation Word in the Rule Name

**ID:** `litespec/core/domain-model#keep-the-negation-word-in-the-rule-name`

**Statement:** A rule whose statement says `not`, `no`, `never`, or `cannot` MUST keep a restricting word in its name: `no`, `never`, `not`, `cannot`, `none`, `nothing`, `without`, `unless`, `non`, `refused`, `refuses`, `refusal`, `forbidden`, `blocked`, or `rejected`, a limit `only`, or a uniqueness `one-...-per-...`.

**Rationale:** A name that drops the negation reads as its own opposite: a rule named `cancel-after-shipment` whose statement says "never" approves what it forbids. A restricting word keeps the name a true sentence a reviewer trusts without opening the statement, and `one-grant-per-account` already says what "an account never holds two grants" says. The `rule-name-invalid` check reports a rule whose statement negates while its name keeps no restricting word or uniqueness.

**Example:** The rule reads `rule/order.cancel-never-after-shipment`.

**Checked by:** `litespec model check` reports `rule-name-invalid` for a rule whose statement says `not`, `no`, `never`, or `cannot` while its name keeps no restricting word or uniqueness.

### Keep Cross-Aggregate Rules on a Rule Page

**ID:** `litespec/core/domain-model#keep-cross-aggregate-rules-on-a-rule-page`

**Statement:** A rule spanning aggregates MUST live as an entry on a rule page.

**Rationale:** An aggregate page holds what one aggregate guarantees; a rule spanning aggregates guarantees what no single aggregate owns. Giving such rules their own page keeps each aggregate page a true account of its aggregate, and gathers the shared rules where a reviewer judges them together.

**Reviewed for:** Which cross-aggregate rules sit outside a rule page? Look at the rule entries beside their aggregate pages.

### Name the Refusal a Broken Rule Produces

**ID:** `litespec/core/domain-model#name-the-refusal-a-broken-rule-produces`

**Statement:** Each rule whose breach refuses a use case MUST name the refusal in `refusal`.

**Rationale:** A guarded rule without a named refusal leaves the ending unnamed: callers branch on a refusal the records never predicted. Naming the refusal ties the rule to the result the use case declares, so the breach and the ending stay one fact.

**Reviewed for:** Which guarded rules name no refusal in `refusal`? Look at the rule entries beside the results they refuse.

### Release Every Reservation by a Named Use Case

**ID:** `litespec/core/domain-model#release-every-reservation-by-a-named-use-case`

**Statement:** Each reservation rule MUST name the use case releasing it in `releasedBy`, or `{ "mode": "never", "reason": "..." }` with its cost.

**Rationale:** A held claim with no release is a leak: places, funds, or names stay locked past their need, and nobody owns freeing them. Naming the releasing use case states the end of the hold beside its start, and `{ "mode": "never", "reason": "..." }` with its cost admits a hold that truly lasts, with the price on record. Persistence claims the added keys and releases the removed ones in the commit that changes the aggregate (`litespec/backend/persistence#claim-a-cross-record-key-at-commit`). The `reservation-release-missing` check reports a reservation with no release.

**Checked by:** `litespec model check` reports `reservation-release-missing` for a reservation rule whose `releasedBy` names neither a use case nor `{ "mode": "never", "reason": "..." }` with its cost.

### Freeze Every Commercial Data Value

**ID:** `litespec/core/domain-model#freeze-every-commercial-data-value`

**Statement:** Each commercial-data rule MUST name when the value freezes in `frozenWhen` and where its history lives in `history`.

**Rationale:** A price, fee, or rate read live changes under settled business: a refund follows today price instead of the price at sale. Naming when the value freezes pins each reading to its moment, and the history link keeps the earlier values reachable for audit and dispute.

**Checked by:** `litespec consumer` fails a commercial-data rule without `frozenWhen` or `history`.

### Compute Shared Values from One Calculation Rule

**ID:** `litespec/core/domain-model#compute-shared-values-from-one-calculation-rule`

**Statement:** A value shown by a query and recorded by a command MUST cite the same calculation rule in `computedBy` on both use cases.

**Rationale:** Two computations of one value drift: the screen shows one figure while the ledger records another, and each side cites its own code. Citing the same calculation rule on both use cases keeps one computation behind both faces. Code carries a mark naming the rule, and never the reverse.

**Reviewed for:** Which queries and commands computing one value cite different rules in `computedBy`? Look at the use cases beside the calculation rule.

### Tabulate Every Calculation with Examples

**ID:** `litespec/core/domain-model#tabulate-every-calculation-with-examples`

**Statement:** Each calculation rule MUST be proven by a scenario carrying an examples table.

**Rationale:** A calculation stated only as a sentence hides its edges: reviewers agree on the words while each pictures other numbers. An examples table shows those edges, and the scenario proving the rule pins them as checked facts. The `calculation-table-missing` check reports a calculation rule with no scenario examples table.

**Checked by:** `litespec model check` reports `calculation-table-missing` for a calculation rule with no scenario examples table.

### State Each Event with Meaning and Carries

**ID:** `litespec/core/domain-model#state-each-event-with-meaning-and-carries`

**Statement:** Each event MUST state its meaning and the facts it carries.

**Rationale:** An event without a meaning is a name every reaction interprets alone; facts without a carries list are reconstructed from code at each reaction. Stating both on the entry lets each reaction trust what happened and what arrived with it.

**Checked by:** `litespec consumer` fails an event entry without `meaning` or `carries`.

### Name Every Event in the Past Tense

**ID:** `litespec/core/domain-model#name-every-event-in-the-past-tense`

**Statement:** Each domain event MUST be named in the past tense.

**Rationale:** An event names a fact that completed, so the past tense tells a reader the business already moved: `submitted` reports, while `submit` orders. The past tense also pairs each event with the act raising it.

**Example:** The expense aggregate raises `event/expense.submitted` for a completed submission.

**Checked by:** `litespec model check` reports `event-tense-invalid` (level A) for an event not ending with the past tense of its verb.

### Raise Every Event Through Its Aggregate Root

**ID:** `litespec/core/domain-model#raise-every-event-through-its-aggregate-root`

**Statement:** Each event MUST be raised through its aggregate root.

**Rationale:** The root is the one type through which outside behavior changes the aggregate, so it sees every change worth announcing. Raising each event there keeps the fact and its announcement in one step, and leaves no event announced for a change the aggregate never made.

**Reviewed for:** Which events leave from outside the root of their aggregate? Look at the aggregate code beside the event entries.

### Keep Model Invariants in Rules

**ID:** `litespec/core/domain-model#keep-model-invariants-in-rules`

**Statement:** A model invariant MUST live as a rule, never as a setting.

**Rationale:** A setting tunes; a rule holds. An invariant filed as a setting reads as tunable, and a later operator tunes away what the model guaranteed. Keeping invariants in rules keeps what holds apart from what varies.

**Reviewed for:** Which setting pages hold a model invariant? Look at the settings beside the rules.

## Conventions

None.
