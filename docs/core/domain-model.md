# Domain Model

**Stack:** any
**Concern:** domain

## Intent

Domain Model states the words and things inside the bounded context: the modules, the glossary, the aggregates with their lifecycles, the rules that hold, and the domain events. Agents write these records from Intent, and code carries them. The [Behavior overview](behavior.md) maps the five Behavior pages, and the [record language page](language.md) states how each field, value and record name is written.

## Concepts

### Bounded Context and Module

The product speaks one business language inside one deployment boundary, as the [scope page](scope.md) states. Modules organize that language into areas; they stay areas of one language and one deployment, never languages or deployments of their own. A module is one business area: a page at `behavior/modules/<m>/module.yaml` carrying a `summary` and the `dependsOn` links to the modules it relies on, in the home the [model page](model.md) names. Intent holds no module; each module is declared only in Behavior, beside the aggregates it groups. Each child record sits in a typed folder inside its module, such as `aggregates/<aggregate>/`, `integration-events/<name>/` or `rules/`, as the [model page](model.md) places each type. Responsibilities, Boundaries and Example live in the explanation file beside the record file.

| Field | Meaning |
|:---|:---|
| `summary` | What the area covers, in one paragraph. |
| `dependsOn` | The modules this module relies on. |

### Glossary

The glossary is the project ubiquitous language: the one word or phrase the project uses for each concept, with its meaning. Each term carries `layer`: intent for business words, behavior for design words such as aggregate, refusal, reaction, and job, and implementation for design words that live in code, such as a read model. Each term lists in `aliases` the established synonyms its readers know, so industry readers find LiteSpec own words: a refusal is a rejection or error, a reaction is a policy, and a compensation is an undo. The `rejected` list still bars the synonyms the project refuses, as the [authoring page](authoring.md) states. A term that declares a project use-case verb carries `verbKind` with the kind it opens: `command`, `query`, or `either`.

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

An aggregate is one thing the product keeps consistent and changes in one step, through its root. Each aggregate declares one kind. The aggregate identifier carries its module, so containment follows the identifier and needs no link field.

| Kind | Meaning |
|:---|:---|
| `entity` | One thing kept consistent through its root, with states, transitions, and data. |
| `store` | State no aggregate invariant guards, such as a library-owned table, with a named owner. |

An aggregate lists in `parts` the kebab-case names of the child entities and value objects inside it that use cases act on, such as `line`, `hold`, and `note`. The use-case noun check reads each part as a known word. Lifecycle, Data and Retention, Example and Limits live in the explanation file beside the record file.

`transitions` lists every allowed change with the use case or job moving it: each entry names the state left in `from`, the state entered in `to`, and the moving use case or job in `movedBy`. A creation, the change that brings an instance into being, writes `-` as the state left. A transition moved by time carries `after` with the ISO 8601 duration from entering its source state, and `movedBy` names the job. `data` lists the fields the aggregate keeps, each with name, meaning, classification, and data class. Each data class declares how its fields erase and how long they are kept:

| Field | Meaning |
|:---|:---|
| `name`, `classification`, `fields` | The class, its `public`, `internal`, `personal`, or `sensitive` handling, and its fields. |
| `purpose` | Why the data is kept. |
| `onErasure` | `erase`, `pseudonymize`, `anonymize`, `retain`, or `exempt`. |
| `basis` | The constraint, requirement, and decision identifiers behind the retention. |
| `period`, `periodStartsAt` | The retention period as an ISO 8601 duration, or `{ "mode": "none", "reason": "..." }`, with the date it counts from: a `domain-event/` identifier, a state transition written `<state> to <state>`, or `financial-year-end` or `calendar-year-end`. |
| `enforcedBy` | The `job/` or `use-case/` identifiers carrying out the erasure. |
| `reach` | `own-tenant`, `subject-wide`, or `platform-wide`. |
| `exceptions` | Per-field erasure positions differing from the class, and the retention conflicts with the winning duty and its basis. |
| `reason`, `basedOn` | The reason of an exemption, and the Intent records accepting retained or exempt personal data. |

### Rules

A rule states what holds, with its `kind` and its `statement`. A rule names what it constrains in `governs`, the refusal code its breach produces in `refusal`, the requirements it carries in `fulfills`, and the constraints and research behind it in `basedOn`. A rule an organization chooses, such as rounding, consent, or seller responsibility, is a requirement or a constraint in Intent, not a Behavior rule. A rule's scope comes from its home: an invariant an aggregate enforces lives under its aggregate or its module, and a module-level rule over several aggregates names them in `binds`. A module rule lives in `behavior/modules/<m>/rules/`; `module.yaml` holds no rules. A rule page carries a `summary` stating why its rules sit together, and its explanation file holds Notes. No rule page restates another record type. A rule identifier keeps its segment when the rule moves; only identifiers whose first segment names a module change with a module move. State one rule per condition: one rule states one condition and names its refusal code from the module's refusal set, and every use case that can break it lists the refusal result citing that code.

| Kind | Meaning |
|:---|:---|
| `invariant` | Holds after every change. |
| `precondition` | Guards the behavior before it runs. |
| `calculation` | Derives a value, proven by an examples table. |
| `atomicity` | Names one atomic write group in `binds`. |
| `reservation` | Holds a claim until a use case releases it, named in `releasedBy` as a use-case id, or `{ "mode": "never", "reason": "..." }` with its cost. |
| `frozen-value` | Freezes a commercial value, with `frozenWhen` and `retainedIn`. |

A query and a command showing and recording one value both cite the same calculation rule in `computedBy`. Code carries a mark naming the rule, and never the reverse.

### Domain Events

A domain event is an immutable statement that a business fact completed, named in the past tense, with its `meaning` and the facts it `carries`. Each event is raised through the root of its aggregate, and each event stays inside the product. Facts crossing the boundary are integration events.

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

**Rationale:** Records and code share one language only when each word is defined once: a term used without an entry means whatever each reader guesses. The entry fixes the meaning, the rejected synonyms, and the wider and narrower terms, so reviewers and checks read the same word the same way.

**Example:** The term `receipt` carries the meaning "proof of payment accepted for an expense" in the glossary, so every record naming a receipt means that proof.

**Reviewed for:** Which terms do the Behavior records name without a glossary entry stating their meaning? Look at the records beside the project glossary.

### Mark Each Term with Its Layer

**ID:** `litespec/core/domain-model#mark-each-term-with-its-layer`

**Strength:** required

**Applies to:** artifact: record:term

**Statement:** Each glossary term MUST carry its record layer, intent, behavior, or implementation, with the established synonyms its readers know in `aliases`.

**Rationale:** Business words, design words, and implementation words age on different clocks: a business term moves when the market moves, a design term moves when the model moves, and an implementation term moves when the code moves. The record layer tells each reader which clock the word follows, and the aliases let industry readers find LiteSpec own words without learning a second vocabulary first.

**Example:** The term `refusal` carries `layer: behavior` with `aliases: ["rejection", "error"]`.

**Checked by:** `litespec model check` reports `glossary-invalid` for a glossary term without `layer`, and each behavior term lists the synonyms its readers know in `aliases`.

### State Every Module Dependency

**ID:** `litespec/core/domain-model#state-every-module-dependency`

**Strength:** required

**Applies to:** artifact: record:module

**Statement:** Each module MUST name every module it depends on in `dependsOn`.

**Rationale:** A module relies on the language and behavior of others, and each reliance shapes change: editing a module ripples into everything depending on it. Naming every reliance in `dependsOn` shows that ripple as data instead of leaving it for readers to reconstruct from prose.

**Reviewed for:** Which modules rely on a module that `dependsOn` does not name? Look at the module pages beside the modules they rely on.

### Declare the Kind of Every Aggregate

**ID:** `litespec/core/domain-model#declare-the-kind-of-every-aggregate`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Each aggregate MUST declare its kind: entity or store.

**Rationale:** The kind decides what the page carries: an entity carries a lifecycle and a store carries an owner with no guarded invariant. Without the kind, a thing kept consistent reads the same as a table the product only holds.

**Example:** The expense declares `kind: entity`, and the suppression list declares `kind: store`.

**Checked by:** `litespec model check` reports `record-invalid` for an aggregate page without `kind` or with an unknown kind.

### Name the Owner of Every Store

**ID:** `litespec/core/domain-model#name-the-owner-of-every-store`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Each store MUST name its owning system or module in `owner`.

**Rationale:** A store holds state no aggregate invariant guards, so nothing in the model explains who answers for its shape: a library-owned table, a suppression list, or a delivery log. Naming the owner puts one party behind it, and keeps staff actions on it as ordinary use cases instead of unwritten operations.

**Checked by:** `litespec model check` reports `record-invalid` for a store aggregate page without `owner`.

### Model Each Aggregate Lifecycle with States

**ID:** `litespec/core/domain-model#model-each-aggregate-lifecycle-with-states`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Each entity MUST declare its lifecycle as `states` entries with a name and a meaning.

**Rationale:** A bare state name tells a reviewer nothing: `rejected` without its meaning hides what rejection costs and allows. A name with a meaning makes each lifecycle step reviewable on the page, and gives `transitions` states worth connecting. An entity tracks what the thing is. A store carries no lifecycle, so it carries no states.

**Example:** The hold queue entity declares `Requested`, `Ready`, `Collected`, `Cancelled`, and `Lapsed`, each with one sentence saying what the hold awaits or has received.

**Checked by:** `litespec model check` reports `record-invalid` for an entity page without `states` entries carrying a name and a meaning.

### Leave States off Every Store

**ID:** `litespec/core/domain-model#leave-states-off-every-store`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** A store MUST carry no states.

**Rationale:** A store holds state no aggregate invariant guards, so a lifecycle on it promises what nothing guards: states claim a reviewed path while any writer moves the rows. The store names its owner instead, as `litespec/core/domain-model#name-the-owner-of-every-store` states, and staff actions on it stay ordinary use cases.

**Example:** The suppression list declares `kind: store` with its owner and no states.

**Checked by:** `litespec model check` reports `record-invalid` for a store aggregate carrying `states`.

### Connect Every State Through Transitions

**ID:** `litespec/core/domain-model#connect-every-state-through-transitions`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Each entity MUST list every allowed state change with the use case or job moving it in `transitions`, including the creation that enters its first state.

**Rationale:** States without transitions are a list of names with no path between them: a reviewer sees where an aggregate rests but never how it moves. Listing every allowed change with the use case or job moving it in `movedBy` shows the whole lifecycle beside the states, and leaves no state reachable only by a path no record names. A transition moved by time names the job in `movedBy` with the duration from entering its source state in `after`. The `state-unreached` and `transition-unknown` checks report a state no transition reaches and a transition naming a missing state, use case, or job.

**Checked by:** `litespec model check` reports `state-unreached` (level G) for a state reached by no transition, a creation included, and `transition-unknown` (level G) for a transition naming a missing state or use case.

### State Each Rule with Its Kind

**ID:** `litespec/core/domain-model#state-each-rule-with-its-kind`

**Strength:** required

**Applies to:** artifact: record:rule

**Statement:** Each rule MUST state its kind and its statement.

**Rationale:** The kind decides how the rule is proved: an invariant on every change, a precondition before the behavior, a calculation through its examples, an atomicity rule naming its bound group, a reservation through its release, and a frozen-value rule through its frozen value. A rule without a kind leaves each reader to prove it a different way, and the checks no place to confirm it.

**Example:** `rule/expense.reimbursed-once` is an invariant stating "an expense is reimbursed at most once", checked on every reimbursement.

**Checked by:** `litespec model check` reports `record-invalid` for a rule entry without `kind` or `statement`.

### State What Each Rule Governs

**ID:** `litespec/core/domain-model#state-what-each-rule-governs`

**Strength:** required

**Applies to:** artifact: record:rule

**Statement:** Each rule MUST name what it constrains in `governs`.

**Rationale:** A rule that names no governed record constrains nothing a reader can find: the condition floats beside the records it should hold. Naming the constrained aggregates and use cases in `governs` pins each rule to its records. The governed records match the links naming the rule: input `rules`, consistency bindings, calculation links, scenario `exercises`, and result refusals. A rule governs records of its own module.

**Example:** The reimbursed-once rule governs the expense aggregate and the reimburse-expense command, matching the refusal results citing its code.

**Checked by:** `litespec model check` reports `rule-governs-drift` for `governs` disagreeing with the links that name the rule.

### Bind Every Atomic Write Group in Its Rule

**ID:** `litespec/core/domain-model#bind-every-atomic-write-group-in-its-rule`

**Strength:** required

**Applies to:** artifact: record:rule

**Statement:** Each atomicity rule MUST name the aggregates it binds in `binds`.

**Rationale:** An atomic write across aggregates stays allowed only through its rule: the rule states the group and why, such as an order without its hold overselling, and the use case commits exactly that group, as the [use-cases page](use-cases.md) states. A group above 4 aggregates is a coordination candidate, reported at level A.

**Example:** The hold rule binds the hold and the copy aggregates, so a hold without its copy never stands.

**Checked by:** `litespec model check` reports `atomicity-drift` (level G) for an atomic use case whose `writes` differs from its rule's `binds`, and `atomicity-invalid` (level A) for a group binding more than 4 aggregates.

### Name Every Aggregate a Rule Spans in Binds

**ID:** `litespec/core/domain-model#name-every-aggregate-a-rule-spans-in-binds`

**Strength:** required

**Applies to:** artifact: record:rule

**Statement:** Each rule whose `governs` names more than one aggregate MUST name every governed aggregate in `binds`.

**Rationale:** A rule across aggregates is held by a state guard or an atomicity rule, never by one aggregate alone. Naming every reached aggregate in `binds` shows the rule's reach beside the rule, so a reader sees which aggregates change together.

**Example:** The `rule/spending.receipt-filed-once` rule states that a receipt file supports one expense across spending, and names `aggregate/spending.expense` and `aggregate/spending.receipt` in `binds`.

**Checked by:** `litespec model check` reports `rule-binds-missing` (level G) for a rule whose `governs` names more than one aggregate while `binds` names none or not all of them.

### Keep the Negation Word in the Rule Name

**ID:** `litespec/core/domain-model#keep-the-negation-word-in-the-rule-name`

**Strength:** required

**Applies to:** artifact: record:rule

**Statement:** A rule whose statement says `not`, `no`, `never`, or `cannot` MUST keep a restricting word in its name: `no`, `never`, `not`, `cannot`, `none`, `nothing`, `without`, `unless`, `non`, `refused`, `refuses`, `refusal`, `forbidden`, `blocked`, or `rejected`, a limit `only`, or a uniqueness `one-...-per-...`.

**Rationale:** A name that drops the negation reads as its own opposite: a rule named `cancel-after-shipment` whose statement says "never" allows what it forbids. A restricting word keeps the name a true sentence a reviewer trusts without opening the statement, and `one-grant-per-account` already says what "an account never holds two grants" says. The `rule-name-invalid` check reports a rule whose statement negates while its name keeps no restricting word or uniqueness.

**Example:** The rule reads `rule/order.cancel-never-after-shipment`.

**Checked by:** `litespec model check` reports `rule-name-invalid` for a rule whose statement says `not`, `no`, `never`, or `cannot` while its name keeps no restricting word or uniqueness.

### Name the Refusal a Broken Rule Produces

**ID:** `litespec/core/domain-model#name-the-refusal-a-broken-rule-produces`

**Strength:** required

**Applies to:** artifact: record:rule

**Statement:** Each rule whose breach refuses a use case MUST name the refusal code in `refusal`.

**Rationale:** A guarded rule without a named refusal leaves the ending unnamed: callers branch on a refusal the records never predicted. Naming the refusal code from the module's refusal set ties the rule to every result citing that code, so the breach and the ending stay one fact. The [Use Cases page](use-cases.md) states the refusal set.

**Reviewed for:** Which guarded rules name no refusal code in `refusal`? Look at the rule entries beside the results citing their code.

### Release Every Reservation by a Named Use Case

**ID:** `litespec/core/domain-model#release-every-reservation-by-a-named-use-case`

**Strength:** required

**Applies to:** artifact: record:rule

**Statement:** Each reservation rule MUST name the use case releasing it in `releasedBy`, or `{ "mode": "never", "reason": "..." }` with its cost.

**Rationale:** A held claim with no release is a leak: places, funds, or names stay locked past their need, and nobody owns freeing them. Naming the releasing use case states the end of the hold beside its start, and `{ "mode": "never", "reason": "..." }` with its cost admits a hold that truly lasts, with the price on record. Persistence claims the added keys and releases the removed ones in the commit that changes the aggregate (`litespec/backend/persistence#claim-a-cross-record-key-at-commit`). The `reservation-release-missing` check reports a reservation with no release.

**Checked by:** `litespec model check` reports `reservation-release-missing` for a reservation rule whose `releasedBy` names neither a use case nor `{ "mode": "never", "reason": "..." }` with its cost.

### Freeze Every Frozen Value

**ID:** `litespec/core/domain-model#freeze-every-frozen-value`

**Strength:** required

**Applies to:** artifact: record:rule

**Statement:** Each frozen-value rule MUST name when the value freezes in `frozenWhen` and where its retained facts live in `retainedIn`.

**Rationale:** A price, fee, or rate read live changes under settled business: a refund follows today price instead of the price at sale. Naming when the value freezes pins each reading to its moment, and the retained link keeps the earlier values reachable for audit and dispute.

**Checked by:** `litespec model check` reports `record-invalid` for a frozen-value rule without `frozenWhen` or `retainedIn`.

### Compute Shared Values from One Calculation Rule

**ID:** `litespec/core/domain-model#compute-shared-values-from-one-calculation-rule`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A value shown by a query and recorded by a command MUST cite the same calculation rule in `computedBy` on both use cases.

**Rationale:** Two computations of one value drift: the screen shows one figure while the ledger records another, and each side cites its own code. Citing the same calculation rule on both use cases keeps one computation behind both faces. Code carries a mark naming the rule, and never the reverse.

**Reviewed for:** Which queries and commands computing one value cite different rules in `computedBy`? Look at the use cases beside the calculation rule.

### Tabulate Every Calculation with Examples

**ID:** `litespec/core/domain-model#tabulate-every-calculation-with-examples`

**Strength:** required

**Applies to:** artifact: record:rule

**Statement:** Each calculation rule MUST be proven by a scenario carrying an `examples` table.

**Rationale:** A calculation stated only as a sentence hides its edges: reviewers agree on the words while each pictures other numbers. An `examples` table shows those edges, and the scenario proving the rule pins them as checked facts. The `calculation-table-missing` check reports a calculation rule with no scenario `examples` table.

**Checked by:** `litespec model check` reports `calculation-table-missing` for a calculation rule with no scenario examples table.

### State Each Event with Meaning and Carries

**ID:** `litespec/core/domain-model#state-each-event-with-meaning-and-carries`

**Strength:** required

**Applies to:** artifact: domain-event

**Statement:** Each event MUST state its meaning and the typed fields it carries.

**Rationale:** An event without a meaning is a name every reaction interprets alone; facts without a carries list are reconstructed from code at each reaction. Stating the meaning with the typed `carries` fields lets each reaction trust what happened and what arrived with it. An integration event publishes only fields its translated domain event carries.

**Checked by:** `litespec model check` reports `record-invalid` for an event entry without `meaning` or `carries`, and `integration-event-field-uncarried` for an integration-event payload field its translated domain event does not carry.

### Name Every Event in the Past Tense

**ID:** `litespec/core/domain-model#name-every-event-in-the-past-tense`

**Strength:** required

**Applies to:** artifact: domain-event

**Statement:** Each domain event MUST be named in the past tense.

**Rationale:** An event names a fact that completed, so the past tense tells a reader the business already moved: `submitted` reports, while `submit` orders. The past tense also pairs each event with the act raising it.

**Example:** The expense aggregate raises `domain-event/expense.submitted` for a completed submission.

**Reviewed for:** Which event names read as an order rather than a completed fact? Look at the event entries beside their raising acts.

### Raise Every Event Through Its Aggregate Root

**ID:** `litespec/core/domain-model#raise-every-event-through-its-aggregate-root`

**Strength:** required

**Applies to:** artifact: domain-event

**Statement:** Each event MUST be raised through its aggregate root.

**Rationale:** The root is the one type through which outside behavior changes the aggregate, so it sees every change worth announcing. Raising each event there keeps the fact and its announcement in one step, and leaves no event announced for a change the aggregate never made.

**Reviewed for:** Which events leave from outside the root of their aggregate? Look at the aggregate code beside the event entries.

### Keep Model Invariants in Rules

**ID:** `litespec/core/domain-model#keep-model-invariants-in-rules`

**Strength:** required

**Applies to:** artifact: record:rule

**Statement:** A model invariant MUST live as a rule, never as a setting.

**Rationale:** A setting tunes; a rule holds. An invariant filed as a setting reads as tunable, and a later operator tunes away what the model guaranteed. Keeping invariants in rules keeps what holds apart from what varies.

**Reviewed for:** Which setting pages hold a model invariant? Look at the settings beside the rules.

## Conventions

### Name Each Module After Its Capability

**ID:** `litespec/core/domain-model#name-each-module-after-its-capability`

**Strength:** default

**Applies to:** artifact: record:module

**Default:** Name each module after one capability the software offers, not after a company function or a catch-all, and split a module whose aggregates serve more than one capability into sibling modules.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** Modules mark the areas of the bounded context, where a monolith would later split and how a reader finds a use case by module, then aggregate, then use case. A capability name keeps one module cohesive; a company function or a catch-all such as `platform`, `sales`, `operations`, or `misc` gathers unrelated aggregates that change for unrelated reasons. Splitting such a module into siblings keeps each module one capability with aggregates that serve it.

**Example:** The finance module holds payout and ledger aggregates serving finance, while a misc module holding lending and catalog aggregates splits into lending and catalog siblings.

**Reviewed for:** Does every aggregate in this module serve the capability the module name states? Look at the aggregates beside the module name.
