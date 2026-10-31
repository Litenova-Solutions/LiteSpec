# Behavior Records

## Intent

Behavior states how the system acts to keep the promises Intent records: the words it uses, the areas it divides into, the things it keeps consistent, the rules that hold, and the behavior it defines with its endings and examples. Agents write these records from Intent, and developers review them before code carries them. The [model page](model.md) states the shared record rules, the [intent page](intent.md) states the promises Behavior answers to, and the [record language page](language.md) states how each field, value and record name is written.

## Concepts

### Bounded Context and Module

The product speaks one business language inside one deployment boundary, as the [scope page](scope.md) states. Modules organize that language into areas; they stay areas of one language and one deployment, never languages or deployments of their own. A module is one business area: a page under the behavior docs carrying a `summary` and the `dependsOn` links to the modules it relies on, in the home the [model page](model.md) names.

| Field | Meaning |
|:---|:---|
| `summary` | What the area covers, in one paragraph. |
| `dependsOn` | The modules this module relies on. |

### Glossary

The glossary is the project ubiquitous language: the one word or phrase the project uses for each concept, with its meaning. Each term carries `layer`: intent for business words, behavior for design words such as aggregate, refusal, and reaction. Each term lists in `aliases` the established synonyms its readers know, so industry readers find LiteSpec own words: a refusal is a rejection or error, a reaction is a policy or automation, a compensation is an undo. The `rejected` list still bars the synonyms the project refuses, as the [authoring page](authoring.md) states.

| Field | Meaning |
|:---|:---|
| `term`, `meaning` | The one word and what it means. |
| `layer` | `intent` or `behavior`. |
| `aliases` | Established synonyms readers know. |
| `rejected` | Synonyms barred in their scope. |
| `broader`, `narrower`, `values` | The wider and narrower terms, and the closed values. |
| `labels` | The term in each interface language beside English, keyed by its two-letter language code. |
| `owner`, `status`, `decider`, `decideBy`, `approvedBy`, `approvedOn`, `approvedHash` | The common fields every record carries. |

### Aggregates

An aggregate is one thing the product keeps consistent and changes in one step, through its root. Each aggregate declares one kind, defaulting to entity when the page names none. The aggregate identifier carries its module, so containment follows the identifier and needs no link field.

| Kind | Meaning |
|:---|:---|
| `entity` | One thing kept consistent through its root, with states, transitions, and data. |
| `process` | A multi-step aggregate with steps, exits, `finishedWhen`, and a driver. |
| `store` | State no aggregate invariant guards, such as a library-owned table, with a named owner. |

An aggregate lists in `parts` the kebab-case names of the child entities and value objects inside it that use cases act on, such as `line`, `hold`, and `note`. The use-case noun check reads each part as a known word.

The body keeps the Transitions and Data tables from the aggregate page style. The Transitions table lists every allowed change with the use case moving it, naming the state left and the state entered. The Data table lists the fields the aggregate keeps with name, meaning, classification, and data class. Each data class declares how its fields erase:

| Field | Meaning |
|:---|:---|
| `name`, `classification`, `fields` | The class, its `public`, `internal`, `personal`, or `sensitive` handling, and its fields. |
| `onErasure` | `erase`, `pseudonymise`, `retain`, or `exempt`. |
| `basis` | `statute`, `business`, or `engineering`. |
| `period` | The retention period as an ISO duration, or `none (on-demand)`. |
| `enforcedBy` | `job/...`, `record-expiry`, `store-ttl`, or `query-filter`. |
| `reach` | `own-tenant`, `subject-wide`, or `platform-wide`. |
| `exceptions` | Per-field erasure positions differing from the class. |
| `reason`, `reviewBy`, `acceptedBy` | The reason and review date of an exemption, and the Intent record accepting retained or exempt personal data. |

### Process Aggregates

A process is an aggregate with a multi-step life: a hold queue, an erasure request, a hiring pipeline. Its steps name the work in order; each step carries an optional `when` guard and a compensation, the step undo: the use case reversing the step, or `none` with the reason no undo applies. Its exits name the ways out, its `finishedWhen` names the observable end, its `driver` names what moves it, and its `cursor` names where resume state lives when a job drives it.

| Field | Meaning |
|:---|:---|
| `steps` | Ordered `{ name, when, compensation }` entries; `compensation` is `use-case/...` or `none (reason)`. |
| `exits` | `{ name, trigger, then }` entries; a trigger is a condition, a `decision by <actor>`, or a `due <duration>`. |
| `finishedWhen` | The observable end, in one sentence. |
| `driver` | `reaction/...`, `job/...`, or `operator: <who, via runbook/..., due <duration>>` entries. |
| `cursor` | Where resume state lives for a job-driven process. |

### Rules

A rule states what holds, with its `kind` and its `statement`. A rule names what it constrains in `governs`, the refusal its breach produces in `refusal`, the requirements it carries in `fulfills`, and the constraints and research behind it in `basedOn`. A cross-aggregate rule spans aggregates and lives on a rule page; each other rule lives as an entry on its aggregate page.

| Kind | Meaning |
|:---|:---|
| `invariant` | Holds after every change. |
| `precondition` | Guards the behavior before it runs. |
| `calculation` | Derives a value, proven by an examples table. |
| `cross-aggregate` | Spans aggregates and lives on a rule page. |
| `atomicity` | Names one atomic write group. |
| `reservation` | Holds a claim until a use case releases it, named in `releasedBy`, or `never` with its cost. |
| `commercial-data` | Freezes a commercial value, with `frozenWhen` and `historyLink`. |
| `default` | States a model default. |

A query and a command showing and recording one value both cite the same calculation rule in `computedBy`. Code carries a mark naming the rule, and never the reverse.

### Domain Events

A domain event is an immutable statement that a business fact completed, named in the past tense, with its `meaning` and the facts it `carries`. Each event is raised through the root of its aggregate, and each event stays inside the product. Facts crossing the boundary are integration events.

| Field | Meaning |
|:---|:---|
| `meaning` | What happened, in one sentence. |
| `carries` | The facts arriving with the event. |

### Integration Events

An integration event is a published fact crossing the bounded context in CloudEvents form. Its direction is `out` for facts the product emits and `in` for facts it accepts. Readers place one of two trusts in it: `trigger-only`, where the event wakes a read of the source, or `payload`, where the fields are fit to act on.

| Field | Meaning |
|:---|:---|
| `direction` | `out` or `in`. |
| `eventType`, `source`, `subject` | The CloudEvents type, source, and what the subject identifies. |
| `payload` | `{ name, meaning, classification }` entries. |
| `trust` | `trigger-only` or `payload`. |
| `correlation` | How an inbound event is matched, inbound only. |
| `translates` | The domain event it carries outward, outbound only. |
| `addresses` | The permission reading it, outbound only, optional. |

### Use Cases

A use case is one independently testable goal: a page beside its aggregate with one kind. A command changes the product; a query reads without changing. Each use case names the journey steps it serves in `serves`, the requirements it carries in `fulfills`, who calls it in `access`, the entry points reaching it in `reachedThrough`, the aggregates it touches in `changes` and `reads`, the events it raises in `raises`, and how it ends in its results. A use case may carry an optional `risk` from money, physical-access, personal-data, and access, as the [model page](model.md) states.

| Field | Meaning |
|:---|:---|
| `access` | `{ auth, grant, tenant }`: auth of `session`, `token`, `device`, `link`, `app`, `anonymous`, or `system`; grant of `permission/<name>`, `self(permission/<name>)`, `anonymous`, or `system`; tenant of `own`, `cross`, `public`, or `none`. |
| `input` | `{ name, meaning, rules, source, classification }` entries; source is `caller`, `claim`, or `derived`. |
| `changes`, `reads` | Aggregate ids touched, including `store` aggregates. |
| `consistency` | `{ mode, because }`: mode of `atomic`, `per-record`, `compensated`, or `best-effort`. |
| `effects` | `{ issues / sends / publishes / calls, timing, onFailure, before, forEach }` entries with exactly one verb; timing is `inline`, `after-commit`, or `async`. On a command, a `sends` or `calls` effect uses `after-commit` or `async` timing. An inline `issues` entry states `sharesTransaction`. |
| `idempotency` | `{ mode, key, scope, reason }`: mode of `key`, `state`, `not-pending`, or `none`; `key` templates `{field}` placeholders over the input field names; `scope` is `tenant`, `account`, `login`, or `global`. |
| `audit` | `{ reason, target }` with reason of `required`, `optional`, or `none`, or `{ exempt }` with its justification. Commands are audited by default with reason `none`, and async work audits at request time. |

Goal and What Happens are required only when the use case changes more than one aggregate, refuses beyond the generic refusals, carries risk, or issues inline effects. Otherwise frontmatter plus Goal is enough.

### Queries

A query reads without changing and declares what its readers receive. Its output groups fields into named `include` groups, each with its fields, classification, source aggregates, and freshness. Freshness is `immediate`, `snapshot`, `bounded` with a duration, or `advisory` naming the authority deciding later and exposing the gap as a typed output field. A command may declare its output as a list of `{ name, meaning }`.

| Field | Meaning |
|:---|:---|
| `output.include` | Named groups of `{ fields, classification, source, freshness, visibleWhen, wideningBecause }`; each field carries `name` and `meaning`. |
| `visibleWhen`, `wideningBecause` | The optional visibility condition and the requirement widening visibility beyond the sources. |
| `paging` | `{ order, cursor | offset, defaultLimit, maxLimit }`, or `none (reason)`. |
| `builtAsynchronously` | Set where the source lags, with a bounded or advisory freshness and a lag scenario. |

### Use-Case Verbs

A use-case name starts with a verb, and each verb keeps one meaning and one kind across the project. LiteSpec ships the baseline vocabulary below; a project adds its domain verbs in `litespec.project.json` `vocabulary` and retires more synonyms there, and never redefines a verb from the baseline vocabulary.

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
| `expire` | command | What time ended; issued by jobs. |
| `record` | command | Something that happened outside the system: a provider answer, a delivery result, a scan. |
| `send` | command | A message to a person. |
| `post`, `reverse` | command | A ledger transaction and its correction. |
| `retry`, `verify` | command | Work run again, and a check run again. |

The baseline vocabulary retires these synonyms: `get`, `fetch`, `lookup` and `count` become `read`; `browse` becomes `list`; `find` becomes `search`; `update`, `edit`, `modify`, `revise`, `set`, `adjust`, `configure` and `rename` become `change`; `define`, `compose`, `save` and `new` become `create`; `delete` becomes `remove`; `notify`, `remind` and `email` become `send`; `suggest` and `ask` become `answer`; `mark`, `note` and `log` become `record`; `end` becomes `close`; `finish` becomes `complete`; `abandon` becomes `cancel`; `lapse` becomes `expire`; `clone` and `copy` become `duplicate`; `replay` becomes `retry`; `recheck` becomes `verify`; `archive` becomes `retire`; `reject` and `deny` become `refuse`; `unpublish` and `hide` become `withdraw`.

A reaction is named for what it does and the fact it answers, `<verb>-<what>-on-<fact>`, such as `send-due-notice-on-loan-borrowed`. A job carries the verb of the command it issues, so `expire-lapsed-holds` issues `expire-holds`.

### Results and Refusals

Each use case ends in named results: child records with an `outcome` of `success` or `refusal`, a `code` on a refusal, and a `meaning`. Refusals stand in precedence order: the first match wins. A refusal is a rejection or error in industry words; the record word stays refusal.

| Field | Meaning |
|:---|:---|
| `code`, `meaning` | The stable refusal code, such as `spending.receipt-missing`, and what it means. |
| `recorded` | Set where the attempt and its reason are stored; a validating refusal stores nothing. |
| `codes` | The closed list of recorded outcomes inside a success. |

Generic refusals for forbidden access and invalid input are derived from `access` and `input` and never written as records. A derived generic result is `result/<use-case>.forbidden` or `result/<use-case>.invalid-input`, and scenarios may point at derived results.

### Scenarios

A scenario proves one ending: a child record with `given`, `when`, `then`, an optional `examples` table, and the `result` it ends in. One test proves each scenario through the public boundary, and a proof goes stale when its scenario semantic hash changes. The book groups scenarios under their result.

| Field | Meaning |
|:---|:---|
| `given`, `when`, `then` | The situation, the action, and the assertion. |
| `examples` | The debated rows, required behind a calculation rule. |
| `result` | The written or derived result the scenario ends in. |

### Reactions

A reaction is a when-X-do-Y automation: on a domain or integration event, it runs the edges its `effects` declare. A reaction changes nothing directly; a reaction needing a change issues a small command. It is filed with the module whose records it changes, or with the notification module when it only sends.

| Field | Meaning |
|:---|:---|
| `on` | The domain or integration events answered. |
| `if` | The skip condition in domain words, optional. |
| `effects` | `{ issues / sends / publishes / calls, timing }` entries with exactly one verb, the same entries a use case uses; a reaction may omit `timing`, which reads as `async`. There are no top-level `issues` or `sends`. |
| `forEach`, `reads` | The optional scope and the aggregates read. |
| `fulfills`, `serves` | The requirements carried and the journey steps served. |
| `idempotency` | The idempotency mode, usually `state`; a `key` templates `{field}` placeholders over the triggering event or run window fields, which the record does not list. |
| `onFailure` | `{ retry, deadLetter, surfacesIn, runbook }`: `retry` is `none`, `next-run`, or `{ maxAttempts, initialDelay, maxDelay, backoff }` with `exponential` or `fixed` backoff; `deadLetter` is `{ after, action }`; `surfacesIn` names an `entry-point/...` and `runbook` a `runbook/...`. Delays and `after` are ISO 8601 durations. |

A reaction may hold scenario child records ending in a result of a use case its effects issue.

### Jobs

A job is scheduled, one-off, or operator work running without a request. A scheduled job runs on its structured schedule; a one-off job runs once; an operator job is interactive platform work with no aggregate, such as a rehearsal harness. Its `effects` run once per item in its `scope`.

| Field | Meaning |
|:---|:---|
| `kind` | `scheduled`, `one-off`, or `operator`. |
| `schedule` | `{ every | cron, timezone }` in structured form. |
| `scope`, `batchSize`, `lease` | What each run selects, how many items per batch, and the lock with its duration. |
| `effects`, `reads` | The `{ issues / sends / publishes / calls, timing }` entries run per item and the aggregates read. There is no top-level `issues`. |
| `serviceLevel`, `evidence` | The promise citing its requirement, and where proof of a run lands. |
| `runbook`, `runsWhere` | The runbook answering the job and where it runs. |
| `fulfills`, `serves`, `onFailure`, `idempotency` | As on a use case and a reaction. |
| `guard` | The guard an operator job runs under. |
| `cursor` | Where resume state lives on a driven process. |

### Notifications

A notification is one person-facing message with its template, addresses, and channel. Each `sends` edge carries its own `if` suppression, and suppression scenarios saying the message sends nothing are first class.

| Field | Meaning |
|:---|:---|
| `template`, `params`, `locale` | The template key, its parameters, and where the language comes from. |
| `addresses`, `channel` | The actors addressed and the channels: `email`, `sms`, `push`, `webhook`. |
| `consent` | The consent rule, such as SMS only with consent. |
| `frequency`, `dedupKey` | `once` or `per-attempt`, and the dedup template over the trigger payload. |
| `suppressedWhen` | The suppression condition, optional. |
| `claims` | `{ claim, requirement }` entries for what the message says. |
| `fulfills` | The requirements carried. |

### Settings

A setting is one tunable value with its type, range, unit, setter, and bound behavior. A default or figure that is commercial, legal, or differs by tenant, market, or date fulfills a requirement; a model invariant is a rule instead. The `setting-set` page style holds many settings.

| Field | Meaning |
|:---|:---|
| `valueType` | `integer`, `duration`, `money`, `boolean`, `text`, or `choice`. |
| `default`, `range`, `unit` | The value, the min-max or choices, and the unit. |
| `setBy` | `decider`, `operator`, or `tenant`. |
| `key` | The configuration path an operator setting binds from, such as `Spending:ApprovalReminderDelay`; required when `setBy` is `operator`. |
| `atBound` | `refuse`, `truncate`, or `degrade`. |
| `secret` | Whether the value is secret. |
| `fulfills` | The requirements carried. |
| `readBy` | The use cases that read the setting. |

### Entry Points

An entry point is one way callers reach the product. Each entry point declares its kind and its `auth`. Served content is a static script or legal text with its path, content type, cache control, and versioning: content served, not a use case run.

| Kind | Meaning |
|:---|:---|
| `browser`, `api`, `webhook`, `device`, `cli`, `mcp` | A browser app, a public interface, an inbound hook, a device, a command line, or a model context channel. |
| `served-content` | Static content with `path`, `contentType`, `cacheControl`, `versioning`, and `fulfills`. |
| `reaction`, `job` | Behavior an event causes and background work without a request. |

The optional `protocol` block belongs to the `devices` extension. The [access page](access.md) states actors, roles, and permissions.

### Screens

A screen is one page a person sees through a `browser` or `device` entry point. Its record lives at `docs/behavior/screens/<entry-point>.<name>.md` with the identifier `screen/<entry-point>.<name>`, such as `screen/claimwick-app.expense-list`. It names its `entryPoint`, its `address` (the route template its stack uses: a web path, a Blazor `@page` template, or an Android destination), the use cases it `offers`, the journey steps it `serves`, and optionally the `floorplan` it is composed from. An overlay with no address of its own names the screen it opens on in `host` instead; a screen carries exactly one of `address` and `host`. A navigation screen offers no use case and says so in its summary.

A screen states what a person can do there; what they see follows from it. The results a screen shows come from the use cases it offers, its outgoing links come from its route, and the sentence shown for a refusal is joined by the refusal's `code`, so none of those are written on the screen. Intent never names a screen: a journey step says what a person achieves, and the book adds the screen that carries the step as context outside the approval hash.

The route entry that renders a screen carries its mark, in the form its stack page states, naming `screen/...`. The builder computes `calls` from the route entry: it follows imports within the route's own folder and the modules folder, so a module the route imports and every module that module composes count, collecting generated-client operation calls (a query helper the client generator emits for an operation, such as its options, query key or mutation, counts as a call to that operation), and maps each operation through the endpoint's mark to its use case. It follows neither the frame nor shared packages, so a call a shared component makes is not the screen's offer, while a call a composed use-case module makes is. An operation that resolves to two use cases is a reader diagnostic rather than a silent pick. The builder compares the computed `calls` with `offers`. A journey step is exercised when a passing test proved a scenario of a use case that serves the step.

### External Systems

An external system is one system outside the bounded context the product exchanges messages with. It acts inside the product as one actor, and each operation declares its direction, timeout, retries, response, and cache. Its erasure position says how personal data leaves it: a call, an integration event, or `impossible` with its reason and acceptance.

| Field | Meaning |
|:---|:---|
| `actor` | The actor the system acts as. |
| `operations` | `{ name, direction, timeout, onTimeout, retries, response, cache }` entries; direction is `out` or `in`. |
| `erasure` | `call <operation>`, `via integration-event/...`, or `impossible (reason)` with `acceptedBy`. |

### Retention Schedule

The retention schedule is one page listing every data class with its basis, period, enforcement, and the requirement behind personal or sensitive keeping. Its columns follow GDPR article 30 order; the deleting job and the evidence are LiteSpec extensions to that record. Processors and conflicts sit on the row: where the data forwards, and which demand wins when keeping for audit and erasing for the subject collide, with who records the exception.

| Field | Meaning |
|:---|:---|
| `dataClass`, `aggregates` | The class and the aggregates holding it. |
| `basis`, `period`, `enforcedBy` | The statute, business, or engineering basis, the period, and the enforcement. |
| `intent` | The requirement behind personal or sensitive keeping. |
| `processors` | `{ system, forwarding, acceptedBy }` entries. |
| `conflicts` | Which demand wins on a keeping-versus-erasing collision and who records it. |

### Spec to Design Mapping

The specification names only what carries business meaning. Child entities and value objects are design choices inside an aggregate: they appear in code and the glossary, never as records. Each record maps to one design pattern; the stack pages state the code form of each pattern.

| Spec record | Design pattern |
|:---|:---|
| module | one module in the bounded context |
| aggregate, entity | one aggregate, changed through its root |
| aggregate, process | one multi-step aggregate with steps, exits, and a driver |
| aggregate, store | one state holder with a named owner and no guarded invariant |
| aggregate `states` | the lifecycle of the aggregate |
| (none) | child entity, changed only through the root |
| (none) | value object, compared by value |
| rule, invariant | an invariant checked on every change |
| rule, precondition | a guard the behavior passes before it runs |
| rule, calculation | a pure computation on the aggregate or its values |
| rule, cross-aggregate | a stored constraint, or a reaction reaching eventual consistency |
| rule, precondition spanning aggregates | a synchronous read of the guarding aggregate named in `reads`, with `consistency: atomic` and the guarding rule in `because`; use a stored constraint only when the guard is a single stored value, and a reaction only when eventual consistency is acceptable |
| rule, atomicity | one atomic write group across the named aggregates |
| rule, reservation | a held claim with its release |
| rule, commercial-data | a frozen commercial value with its history |
| rule, default | a default value the model holds |
| event | a domain event raised through the root |
| integration event | a published fact in CloudEvents form |
| use case, command | one handler behind the use case |
| use case, query | one read behind the use case |
| reaction | one automation answering its trigger |
| job | one scheduled, one-off, or operator run |
| notification | one rendered message to the actors it addresses |
| setting | one tunable value with its range and setter |
| result | one ending per result, each refusal with its code |
| scenario | an executable example proving the scenario |

## Standards

### Define Every Behavior Term in the Glossary

**ID:** `litespec/core/behavior#define-every-behavior-term-in-the-glossary`

**Statement:** Each term a Behavior record names MUST have a glossary entry stating its meaning.

**Rationale:** Records and code share one language only when each word is defined once: a term used without an entry means whatever each reader guesses. The entry fixes the meaning, the rejected synonyms, and the wider and narrower terms, so reviewers and checks read the same word the same way.

**Example:** The term `receipt` carries the meaning "proof of payment accepted for an expense" in the glossary, so every record naming a receipt means that proof.

**Reviewed for:** Each term the Behavior records name appears with its meaning in the project glossary.

### Mark Each Term with Its Layer

**ID:** `litespec/core/behavior#mark-each-term-with-its-layer`

**Statement:** Each glossary term MUST carry its record layer, intent or behavior, with the established synonyms its readers know in `aliases`.

**Rationale:** Business words and design words age on different clocks: a business term moves when the market moves, while a design term moves when the model moves. The record layer tells each reader which clock the word follows, and the aliases let industry readers find LiteSpec own words without learning a second vocabulary first.

**Example:** The term `refusal` carries `layer: behavior` with `aliases: ["rejection", "error"]`.

**Checked by:** `litespec consumer` fails a glossary term without `layer`, and each behavior term lists the synonyms its readers know in `aliases`.

### State Every Module Dependency

**ID:** `litespec/core/behavior#state-every-module-dependency`

**Statement:** Each module MUST name every module it depends on in `dependsOn`.

**Rationale:** A module relies on the language and behavior of others, and each reliance shapes change: editing a module ripples into everything depending on it. Naming every reliance in `dependsOn` shows that ripple as data instead of leaving it for readers to reconstruct from prose.

**Reviewed for:** Each module page names in `dependsOn` the modules its text and records rely on.

### Declare the Kind of Every Aggregate

**ID:** `litespec/core/behavior#declare-the-kind-of-every-aggregate`

**Statement:** Each aggregate MUST declare its kind: entity, process, or store.

**Rationale:** The kind decides what the page carries: an entity carries a lifecycle, a process carries steps with exits and a driver, and a store carries an owner with no guarded invariant. Without the kind, a reviewer cannot tell a thing kept consistent from a table the product only holds.

**Example:** The hold queue aggregate declares `kind: process`, and the suppression list declares `kind: store`.

**Checked by:** `litespec consumer` fails an aggregate page without `kind` or with an unknown kind.

### Name the Owner of Every Store

**ID:** `litespec/core/behavior#name-the-owner-of-every-store`

**Statement:** Each store MUST name its owning system or module in `owner`.

**Rationale:** A store holds state no aggregate invariant guards, so nothing in the model explains who answers for its shape: a library-owned table, a suppression list, or a delivery log. Naming the owner puts one party behind it, and keeps staff actions on it as ordinary use cases instead of unwritten operations.

**Checked by:** `litespec consumer` fails a store aggregate page without `owner`.

### Model Each Aggregate Lifecycle with States

**ID:** `litespec/core/behavior#model-each-aggregate-lifecycle-with-states`

**Statement:** Each entity and each process MUST declare its lifecycle as `states` entries with a name and a meaning.

**Rationale:** A bare state name tells a reviewer nothing: `rejected` without its meaning hides what rejection costs and allows. A name with a meaning makes each lifecycle step reviewable on the page, and gives the Transitions table states worth connecting. A store carries no lifecycle, so it carries no states.

**Example:** The hold queue process declares `Requested`, `Ready`, `Collected`, `Cancelled`, and `Lapsed`, each with one sentence saying what the hold awaits or has received.

**Checked by:** `litespec consumer` fails an entity or process page without `states` entries carrying a name and a meaning.

### Connect Every State Through Transitions

**ID:** `litespec/core/behavior#connect-every-state-through-transitions`

**Statement:** Each entity and each process MUST list every allowed state change with its use case in its Transitions table.

**Rationale:** States without transitions are a list of names with no path between them: a reviewer sees where an aggregate rests but never how it moves. Listing every allowed change with the use case that moves it shows the whole lifecycle as debated rows, and leaves no state reachable only by a path no record names. The `state-unreachable` and `transition-unresolved` checks report a state no transition reaches and a transition naming a missing state or use case.

**Checked by:** `litespec model check` reports `state-unreachable` (tier G) for a state reached by no transition and `transition-unresolved` (tier G) for a transition naming a missing state or use case.

### Classify Every Aggregate Data Field

**ID:** `litespec/core/behavior#classify-every-aggregate-data-field`

**Statement:** Each aggregate MUST list its data fields with their meaning, classification, and data class in its Data table.

**Rationale:** Data without a classification is handled by guess: personal facts sit beside public ones with nothing marking which is which. A meaning with a classification of public, internal, personal, or sensitive tells every later reader how each field is handled, and the data class ties the field to its erasure position and its retention row.

**Reviewed for:** The Data table on each aggregate page pairs each field with a meaning, a classification, and a data class.

### Declare Every Data Class with Its Erasure Position

**ID:** `litespec/core/behavior#declare-every-data-class-with-its-erasure-position`

**Statement:** Each data class MUST declare its classification, its fields, its erasure position, its basis, its retention period, and the enforcement that carries it out.

**Rationale:** Erasure declared per field drifts: one field keeps a new position while its twin keeps the old one. Declaring the position once per class, with per-field exceptions beside it, keeps one decision in one place. The position is `erase`, `pseudonymise`, `retain`, or `exempt`; the basis is `statute`, `business`, or `engineering`; the period is an ISO duration or `none (on-demand)`; the enforcement is `job/...`, `record-expiry`, `store-ttl`, or `query-filter`. The `erasure-undeclared` check reports a personal or sensitive field with no declared position, and `retention-without-enforcement` reports a class nothing enforces.

**Example:** The submitter-contact class declares `personal`, `pseudonymise`, and its approving requirement, while the expense-ledger class declares `internal`, `retain`, a statutory seven years, and the finance expiry job.

**Checked by:** `litespec model check` reports `erasure-undeclared` for a personal or sensitive field with no declared position and `retention-without-enforcement` for a class nothing enforces.

### Accept Every Retained Personal Data Class in Intent

**ID:** `litespec/core/behavior#accept-every-retained-personal-data-class-in-intent`

**Statement:** Each personal or sensitive data class kept with `retain` or `exempt` MUST name the Intent requirement or decision accepting it in `acceptedBy`.

**Rationale:** Keeping personal data past erasure is a business choice, not a technical default: the statute or the business accepts it, with the reason on record. Naming that requirement or decision in `acceptedBy` keeps the keeping reviewable where the promise lives. The `risk-without-acceptance` check reports a retained or exempt personal or sensitive class with no Intent acceptance.

**Checked by:** `litespec model check` reports `risk-without-acceptance` for a retained or exempt personal or sensitive class with no Intent acceptance.

### Review Every Erasure Exemption Before Its Date

**ID:** `litespec/core/behavior#review-every-erasure-exemption-before-its-date`

**Statement:** Each exempt data class MUST carry its reason and a future `reviewBy` date.

**Rationale:** An exemption without a date is a permanent exception wearing temporary clothes: nobody reopens it, so it holds forever. A reason with a future review date makes the exemption a dated choice the owner rejudges on time. The `exemption-expired` check reports an exemption past its date.

**Checked by:** `litespec model check` reports `exemption-expired` for an exemption past its `reviewBy` date.

### State Each Process Step with Its Compensation

**ID:** `litespec/core/behavior#state-each-process-step-with-its-compensation`

**Statement:** Each process step MUST name its compensation: the use case undoing it, or `none` with the reason it needs no undo.

**Rationale:** A step without a named undo leaves failure handling to the implementer: each handler invents its own reversal, and some invent none. Naming the compensating use case states the reversal beside the step, and `none` with its reason states why no reversal applies, such as money already moved. The step may also carry a `when` guard stating when it runs.

**Example:** The hold-place step compensates with the release-hold use case, while the settle step carries `none (money has moved)`.

**Checked by:** `litespec consumer` fails a process step without a compensation use case or `none` with its reason.

### Close Every Process with Named Exits and a Driver

**ID:** `litespec/core/behavior#close-every-process-with-named-exits-and-a-driver`

**Statement:** Each process MUST name its exits with their triggers, its `finishedWhen`, and its `driver`.

**Rationale:** A process with no named end runs until someone notices: a hold waits forever, an erasure sits half done. Exits with triggers name every way out, `finishedWhen` names the observable end in one sentence, and the driver names what moves the process: a reaction, a job, or an operator with a runbook and a due duration. A job-driven process also names its `cursor`, where resume state lives.

**Example:** The hold queue process exits as cancelled on reader decision and as lapsed at the pickup window, finishes when collected, cancelled, or lapsed, and is driven by a completion reaction, an expiry job, and the librarian.

**Checked by:** `litespec consumer` fails a process page without exits carrying triggers, `finishedWhen`, and `driver`.

### Prove Every Process Guard and Exit with a Scenario

**ID:** `litespec/core/behavior#prove-every-process-guard-and-exit-with-a-scenario`

**Statement:** Each `when` guard and each process exit MUST have a scenario proving it.

**Rationale:** A guard no scenario exercises is a condition nobody checked: it holds in prose and fails in code. A scenario per guard and per exit pins each branch as a checked fact, including the branches where the process leaves early.

**Reviewed for:** A scenario proving each `when` guard and each exit sits on the process page or its use case pages.

### State Each Rule with Its Kind

**ID:** `litespec/core/behavior#state-each-rule-with-its-kind`

**Statement:** Each rule MUST state its kind and its statement.

**Rationale:** The kind decides how the rule is proved: an invariant on every change, a precondition before the behavior, a calculation through its examples, a cross-aggregate rule where aggregates meet, an atomicity rule per write group, a reservation through its release, a commercial-data rule through its frozen value, a default through the value the model holds. A rule without a kind leaves each reader to prove it a different way, and the checks no place to confirm it.

**Example:** `rule/expense.reimbursed-once` is an invariant stating "an expense is reimbursed at most once", checked on every reimbursement.

**Checked by:** `litespec consumer` fails a rule entry without `kind` or `statement`.

### Keep the Negation Word in the Rule Name

**ID:** `litespec/core/behavior#keep-the-negation-word-in-the-rule-name`

**Statement:** A rule whose statement says `not`, `no`, `never`, or `cannot` MUST keep a restricting word in its name: a negation such as `no`, `never`, `not`, `cannot`, `none`, `without`, `unless`, `refused`, or `forbidden`, a limit such as `only`, or a uniqueness `one-...-per-...`.

**Rationale:** A name that drops the negation reads as its own opposite: a rule named `cancel-after-shipment` whose statement says "never" approves what it forbids. A restricting word keeps the name a true sentence a reviewer trusts without opening the statement, and `one-grant-per-account` already says what "an account never holds two grants" says. The `rule-name-negation` check reports a rule whose statement negates while its name keeps no restricting word or uniqueness.

**Example:** The rule reads `rule/order.cancel-never-after-shipment`.

**Checked by:** `litespec model check` reports `rule-name-negation` for a rule whose statement says `not`, `no`, `never`, or `cannot` while its name keeps no restricting word or uniqueness.

### Keep Cross-Aggregate Rules on a Rule Page

**ID:** `litespec/core/behavior#keep-cross-aggregate-rules-on-a-rule-page`

**Statement:** A rule spanning aggregates MUST live as an entry on a rule page.

**Rationale:** An aggregate page holds what one aggregate guarantees; a rule spanning aggregates guarantees what no single aggregate owns. Giving such rules their own page keeps each aggregate page a true account of its aggregate, and gathers the shared rules where a reviewer judges them together.

**Reviewed for:** Each cross-aggregate rule sits as an entry on a rule page; other rules sit on their aggregate page.

### Name the Refusal a Broken Rule Produces

**ID:** `litespec/core/behavior#name-the-refusal-a-broken-rule-produces`

**Statement:** Each rule whose breach refuses a use case MUST name the refusal in `refusal`.

**Rationale:** A guarded rule without a named refusal leaves the ending unnamed: callers branch on a refusal the records never predicted. Naming the refusal ties the rule to the result the use case declares, so the breach and the ending stay one fact.

**Reviewed for:** Each guarded rule names its refusal in `refusal` beside the rule entry.

### Release Every Reservation by a Named Use Case

**ID:** `litespec/core/behavior#release-every-reservation-by-a-named-use-case`

**Statement:** Each reservation rule MUST name the use case releasing it in `releasedBy`, or `never` with its cost.

**Rationale:** A held claim with no release is a leak: places, funds, or names stay locked past their need, and nobody owns freeing them. Naming the releasing use case states the end of the hold beside its start, and `never` with its cost admits a hold that truly lasts, with the price on record. Persistence claims the added keys and releases the removed ones in the commit that changes the aggregate (`litespec/backend/persistence#claim-a-cross-record-key-at-commit`). The `reservation-without-release` check reports a reservation with no release.

**Checked by:** `litespec model check` reports `reservation-without-release` for a reservation rule without `releasedBy` or `never` with its cost.

### Freeze Every Commercial Data Value

**ID:** `litespec/core/behavior#freeze-every-commercial-data-value`

**Statement:** Each commercial-data rule MUST name when the value freezes in `frozenWhen` and where its history lives in `historyLink`.

**Rationale:** A price, fee, or rate read live changes under settled business: a refund follows today price instead of the price at sale. Naming when the value freezes pins each reading to its moment, and the history link keeps the earlier values reachable for audit and dispute.

**Checked by:** `litespec consumer` fails a commercial-data rule without `frozenWhen` or `historyLink`.

### Compute Shared Values from One Calculation Rule

**ID:** `litespec/core/behavior#compute-shared-values-from-one-calculation-rule`

**Statement:** A value shown by a query and recorded by a command MUST cite the same calculation rule in `computedBy` on both use cases.

**Rationale:** Two computations of one value drift: the screen shows one figure while the ledger records another, and each side cites its own code. Citing the same calculation rule on both use cases keeps one computation behind both faces. Code carries a mark naming the rule, and never the reverse.

**Reviewed for:** The showing query and the recording command cite the same calculation rule in `computedBy`.

### Tabulate Every Calculation with Examples

**ID:** `litespec/core/behavior#tabulate-every-calculation-with-examples`

**Statement:** Each calculation rule MUST be proven by a scenario carrying an examples table.

**Rationale:** A calculation stated only as a sentence hides its edges: reviewers agree on the words while each pictures other numbers. An examples table shows the debated rows, and the scenario proving the rule pins them as checked facts. The `calculation-without-table` check reports a calculation rule with no scenario examples table.

**Checked by:** `litespec model check` reports `calculation-without-table` for a calculation rule with no scenario examples table.

### State Each Event with Meaning and Carries

**ID:** `litespec/core/behavior#state-each-event-with-meaning-and-carries`

**Statement:** Each event MUST state its meaning and the facts it carries.

**Rationale:** An event without a meaning is a name every reaction interprets alone; facts without a carries list are reconstructed from code at each reaction. Stating both on the entry lets each reaction trust what happened and what arrived with it.

**Checked by:** `litespec consumer` fails an event entry without `meaning` or `carries`.

### Raise Every Event Through Its Aggregate Root

**ID:** `litespec/core/behavior#raise-every-event-through-its-aggregate-root`

**Statement:** Each event MUST be raised through its aggregate root.

**Rationale:** The root is the one type through which outside behavior changes the aggregate, so it sees every change worth announcing. Raising each event there keeps the fact and its announcement in one step, and leaves no event announced for a change the aggregate never made.

**Reviewed for:** Each event is raised from the root of the aggregate that owns it.

### Describe Every Integration Event for Its Direction

**ID:** `litespec/core/behavior#describe-every-integration-event-for-its-direction`

**Statement:** Each integration event MUST declare its direction, its CloudEvents identity, its payload with classifications, and the trust its readers place in it.

**Rationale:** A published fact without a direction is read both ways: an outbound notice is answered as an order, an inbound claim is trusted as law. The direction, the CloudEvents type, source, and subject, the payload with classifications, and the trust of `trigger-only` or `payload` state what the fact is and how far readers follow it. An inbound event also declares its `correlation`; an outbound event declares its `translates` domain event and its optional `addresses`.

**Checked by:** `litespec consumer` fails an integration event without direction, CloudEvents identity, payload, or trust.

### Publish Every Outbound Integration Event from Effects

**ID:** `litespec/core/behavior#publish-every-outbound-integration-event-from-effects`

**Statement:** Each use case or reaction emitting an integration event MUST declare it in its effects as `publishes`.

**Rationale:** An emitted fact no edge declares is a hidden coupling: downstream readers depend on a promise the records never made, and the promise breaks without a diff. Declaring each emission as a `publishes` effect states the coupling beside the behavior causing it. The `effect-undeclared` check reports an emitted event no record declares.

**Checked by:** `litespec model check` reports `effect-undeclared` for an emitted integration event no record declares in effects.

### Name Published Events in the Domain's Words

**ID:** `litespec/core/behavior#name-published-events-in-the-domains-words`

**Statement:** Each outbound integration event SHOULD be named for the business fact, without internal words.

**Deviation:** An event kept for an internal outbox fact its readers already consume keeps its name until its readers move.

**Rationale:** An integrator reads the published name as the business fact; `staged`, `dispatch` or `outbox` describe the machinery that delivered it. A name carrying the fact travels across the boundary unchanged.

**Example:** `expense-reimbursement-requested` names the requested reimbursement; `expense-reimbursement-staged` names the staging step and is reported.

**Checked by:** `litespec model check` reports `integration-event-wording` (tier G) for an outbound event named with an internal word.

### Reach Every Inbound Integration Event Through a Webhook

**ID:** `litespec/core/behavior#reach-every-inbound-integration-event-through-a-webhook`

**Statement:** Each inbound integration event MUST arrive through an entry point of kind `webhook`.

**Rationale:** An inbound fact with no door is accepted anywhere: each handler invents its own intake, and trust scatters across the code. Arriving through a webhook entry point gives each inbound fact one door with one auth, where trust and correlation hold.

**Reviewed for:** Each inbound integration event names a webhook entry point as its door.

### Give Each Use Case a Command or Query Kind

**ID:** `litespec/core/behavior#give-each-use-case-a-command-or-query-kind`

**Statement:** Each use case MUST declare one kind: command or query.

**Rationale:** The kind decides what the use case is allowed to touch: a command changes, a query reads. A use case with no kind leaves each reader to allow it everything, and the checks no shape to confirm. When-X-do-Y behavior is a reaction record, and work without a request is a job record.

**Checked by:** `litespec consumer` fails a use-case page without `kind` or with an unknown kind.

### Start Every Use Case with a Declared Verb

**ID:** `litespec/core/behavior#start-every-use-case-with-a-declared-verb`

**Statement:** Each use case MUST start with a verb from the baseline vocabulary or the domain verbs the project declares in `litespec.project.json`.

**Rationale:** One verb per meaning keeps use cases findable: `read` versus `get` and `change` versus `update` stop competing when every project starts from one closed baseline vocabulary and adds only its domain verbs. A shared first word groups related behavior in every listing, and a retired synonym points at its replacement instead of spreading silently. When the act verb is in neither list, the author first adds it to `vocabulary.verbs` in `litespec.project.json` in the same change, or to `vocabulary.queryVerbs` for a reading verb, and then scaffolds the use case. `litespec new` scaffolds a use-case page without checking its verb, so the vocabulary addition lands before the scaffold rather than after a refusal from the scaffold command. The `use-case-verb-unknown` check reports a use-case name starting outside the baseline vocabulary and the project verbs, and names the replacement of a retired synonym.

**Example:** The vocabulary declares `verbs: ["borrow"]` and `phrasal: ["check-in"]`, so `use-case/lending.borrow-copy` and `use-case/lending.check-in-copy` pass, `use-case/lending.list-loans` passes on the baseline vocabulary, and `use-case/lending.update-copy` is told to use `change`.

**Checked by:** `litespec model check` reports `use-case-verb-unknown` for a use-case name starting outside the baseline vocabulary and the project verbs.

### Keep One Kind per Verb

**ID:** `litespec/core/behavior#keep-one-kind-per-verb`

**Statement:** Each use case MUST open with a verb of its own kind: a reading verb for a query, and a changing verb for a command.

**Rationale:** The first word is the first thing a reader trusts. A query named `change-...` or a command named `read-...` makes every listing lie about what the use case may touch, and a reviewer stops reading names at all. The baseline vocabulary gives each verb a kind, a project names its own reading verbs in `queryVerbs`, and its other domain verbs open either kind. The `use-case-verb-kind` check reports a use case whose verb names the other kind.

**Example:** `use-case/lending.list-loans` is a query and passes; `use-case/lending.read-overdue-count` declared as a command is reported, and so is `use-case/lending.change-copy` declared as a query.

**Checked by:** `litespec model check` reports `use-case-verb-kind` for a query opening with a changing verb or a command opening with a reading verb.

### Name What a Use Case Acts on from the Model

**ID:** `litespec/core/behavior#name-what-a-use-case-acts-on-from-the-model`

**Statement:** Each use case MUST name, after its verb, an aggregate, a declared part, or a glossary term.

**Rationale:** The noun groups behavior by the business record it changes; a free synonym hides which record that is and breeds a second word for the concept.

**Example:** `use-case/lending.borrow-copy` names the copy aggregate, and `use-case/sales.add-line` names the declared line part; `use-case/lending.borrow-volume` names no model word and is reported.

**Checked by:** `litespec model check` reports `use-case-noun-unknown` (tier A) for a use case naming no aggregate, declared part, or glossary term after its verb.

### Name a Use Case for the Act Its Aggregate Performs

**ID:** `litespec/core/behavior#name-a-use-case-for-the-act-its-aggregate-performs`

**Statement:** Each command use case MUST share its verb with the aggregate method its handler calls.

**Rationale:** The use case is the doorway into one aggregate act. A different word at the door and at the act means two meanings have merged, or the use case is misnamed, and each reader guesses which word tells the truth. A handler calling no aggregate method answers to no act, so queries and ports stay out of this rule. A command that records a request for work an event handler finishes after the commit, as `litespec/backend/application#call-external-systems-through-their-ports` requires, performs the request: its method starts with `Request` and passes.

**Example:** `use-case/orders.void-order` whose handler calls `Abandon` is reported; the project renames the use case to the domain word, or the method and its event to `Void` and `OrderVoidedEvent`.

**Checked by:** `litespec model check` reports `use-case-verb-method` (tier A) for a command handler calling aggregate methods, none starting with the use-case verb or with `Request`.

### Connect Each Use Case to Intent

**ID:** `litespec/core/behavior#connect-each-use-case-to-intent`

**Statement:** Each use case MUST name a journey step in `serves` or a requirement in `fulfills`.

**Rationale:** A use case serving nothing and fulfilling nothing is behavior nobody asked for: it ships without a promise behind it and stays when the promise changes. Naming a journey step or a requirement ties each behavior to the intent it carries. The `record-orphaned` check reports a use case with neither field, and `step-unserved` reports a journey step no use case serves.

**Example:** The reimburse-expense command serves `journey/submit-and-reimburse#s4` and fulfills `requirement/approval.single-reimbursement`.

**Checked by:** `litespec model check` reports `record-orphaned` for a use case with neither `serves` nor `fulfills`.

### Grant Each Use Case Scoped Access

**ID:** `litespec/core/behavior#grant-each-use-case-scoped-access`

**Statement:** Each use case MUST declare its `auth`, its grant, and its tenant scope in `access`.

**Rationale:** Access split across prose and code is widened silently: a new caller arrives, the grant stretches, and no record shows it. Declaring the auth kind, the grant of `permission/<name>`, `self(permission/<name>)`, `anonymous`, or `system`, and the tenant scope of `own`, `cross`, `public`, or `none` states who calls and how far the call reaches in one place. Tenant scope `cross` routes the use case to the security queue of the behavior diff. The authorization guard enforces the grant (`litespec/backend/application#enforce-target-authorization-in-the-pipeline`), and `LS2012` reports a handler that authorizes itself.

**Example:** The reimburse-expense command declares session auth, the reimburse-expense permission, and own tenant scope, while the self-checkout scan declares device auth with a system grant.

**Checked by:** `litespec consumer` fails a use-case page without `access` carrying `auth`, grant, and tenant scope.

### Name the Entry Points That Reach Each Use Case

**ID:** `litespec/core/behavior#name-the-entry-points-that-reach-each-use-case`

**Statement:** Each use case MUST name the entry points that reach it in `reachedThrough`.

**Rationale:** A use case with no named door is reachable from anywhere or nowhere: reviewers cannot tell a public operation from an internal one. Naming the entry points shows how callers arrive at each behavior.

**Checked by:** `litespec model check` reports `reference-unresolved` for a `reachedThrough` entry naming no entry point.

### Describe Every Use Case Input with Source and Classification

**ID:** `litespec/core/behavior#describe-every-use-case-input-with-source-and-classification`

**Statement:** Each use-case input MUST declare its name, meaning, source, and classification.

**Rationale:** An input without a source is trusted on faith: caller-asserted values pass as verified claims, and money moves on an unchecked figure. Declaring the name, the meaning, the source of `caller`, `claim`, or `derived`, and the classification states what arrives and how far to trust it.

**Checked by:** `litespec consumer` fails a use-case input without name, meaning, source, or classification.

### Bound Every Caller-Supplied Input Value

**ID:** `litespec/core/behavior#bound-every-caller-supplied-input-value`

**Statement:** Each input value with `source: caller` on a money or access path MUST cite the rule bounding it.

**Rationale:** A caller-asserted figure on a money or access path is an open till: a replacement amount arrives negative, a quantity arrives absurd, and the code obliges. Citing the rule bounding it ties each such value to the guard holding it.

**Example:** The replacement net amount cites the positivity rule bounding it.

**Reviewed for:** Each caller-sourced money or access input cites its bounding rule beside the input.

### Name Every Aggregate a Use Case Changes or Reads

**ID:** `litespec/core/behavior#name-every-aggregate-a-use-case-changes-or-reads`

**Statement:** Each use case MUST name every aggregate it changes in `changes` and every aggregate it reads in `reads`.

**Rationale:** A write no record names is invisible to review: the handler touches a second aggregate, the second aggregate changes under its reviewers, and no diff shows it. Naming every touched aggregate, including `store` aggregates, states the read and write footprint beside the behavior. The `write-undeclared` check reports a write outside `changes`.

**Checked by:** `litespec model check` reports `write-undeclared` for a write outside `changes` and `reference-unresolved` for an unknown aggregate.

### State the Consistency of Every Use Case

**ID:** `litespec/core/behavior#state-the-consistency-of-every-use-case`

**Statement:** Each use case MUST declare its write consistency in `consistency`.

**Rationale:** Consistency left unsaid defaults to hope: a multi-aggregate write is read as atomic until it fails halfway. Declaring the mode states the promise: `atomic` with its `because` rules when `changes` lists more than one aggregate, `per-record` with a receipt per target on best effort, `compensated` with every mutating step naming its compensation. Atomic consistency never spans an `async` edge. The `consistency-unexplained` check reports an atomic multi-aggregate write with no `because`, and `many-aggregates` flags more than one atomic write group as a smell to consider.

**Example:** The reimburse-expense command declares atomic consistency because of the reimbursed-once rule, while the self-checkout scan declares per-record consistency across its attempts.

**Checked by:** `litespec model check` reports `consistency-unexplained` for an atomic multi-aggregate write with no `because`, and `many-aggregates` (tier A) for a command saving more than one atomic write group.

### Declare Every Effect with One Verb and Its Timing

**ID:** `litespec/core/behavior#declare-every-effect-with-one-verb-and-its-timing`

**Statement:** Each effect entry MUST carry exactly one verb and its timing.

**Rationale:** An edge no record declares is the defect: hidden calls between use cases, reactions, jobs, notifications, integration events, and external systems. Declaring each edge with exactly one verb of `issues`, `sends`, `publishes`, or `calls`, with its timing of `inline`, `after-commit`, or `async`, its failure handling, and its `before` and `forEach` where they apply, states every coupling as data. On a command, a `sends` or `calls` effect carries `after-commit` or `async` timing, and an event handler runs the consequence after the commit. The `effect-undeclared` check reports an edge the records never declared.

**Example:** The reimburse-expense command publishes the reimbursed integration event after commit.

**Checked by:** `litespec model check` reports `effect-undeclared` for an edge the records never declared and `effect-before-commit` for a command `sends` or `calls` effect with `inline` timing; `litespec consumer` fails an effect entry without exactly one verb or its timing.

### State the Transaction Sharing of Every Inline Call

**ID:** `litespec/core/behavior#state-the-transaction-sharing-of-every-inline-call`

**Statement:** Each inline `issues` effect MUST state whether it shares the caller transaction in `sharesTransaction`.

**Rationale:** An inline call without a stated transaction is a coin toss at failure: the caller rolls back while the issued work stands, or the reverse. Stating whether the call shares the caller transaction makes the failure shape reviewable beside the edge.

**Checked by:** `litespec consumer` fails an inline `issues` effect without `sharesTransaction`.

### Keep the Effect Graph Free of Cycles

**ID:** `litespec/core/behavior#keep-the-effect-graph-free-of-cycles`

**Statement:** The effect graph MUST hold no cycle except through a domain event carrying its end condition.

**Rationale:** A cycle without an end condition runs until something breaks: a reaction issues a command raising the event triggering the reaction. Forbidding cycles except through a domain event with a stated end condition keeps every loop a loop with an exit. The `effect-loop` check reports a cycle with no stated end.

**Checked by:** `litespec model check` reports `effect-loop` for a cycle with no stated end condition.

### Name the Events Each Use Case Raises

**ID:** `litespec/core/behavior#name-the-events-each-use-case-raises`

**Statement:** Each use case MUST name the domain events it raises in `raises`.

**Rationale:** An event raised but unnamed leaves reactions hanging: downstream behavior answers a fact no behavior admits raising. Naming the raised events ties each fact to the behavior announcing it.

**Reviewed for:** Each use case names its raised domain events in `raises` beside the use-case page.

### Name the Idempotency of Every Use Case

**ID:** `litespec/core/behavior#name-the-idempotency-of-every-use-case`

**Statement:** Each use case MUST declare its idempotency mode with the key, scope, or reason that mode needs.

**Rationale:** A retried call without a stated idempotency runs twice: a double payout, a double loan, a double notice. Declaring the mode of `key`, `state`, or `not-pending`, with its key template and scope, states how repeats collapse; `none` carries the reason repeats cannot happen. The `key` templates `{field}` placeholders over the record's `input` field names, and the `scope` is `tenant`, `account`, `login`, or `global`. A reaction or job key names fields of the triggering event or run window instead, which the record does not list, so its placeholders are not checked. The self-checkout scan keys on `{nonce}` scoped to the login.

**Checked by:** `litespec consumer` fails a use case without an idempotency mode or without the key, scope, or reason its mode needs; `litespec model check` reports `record-shape` for a key with no `{field}` placeholder, a placeholder naming no input field, or a scope outside `tenant`, `account`, `login`, and `global`.

### State the Audit Position of Every Command

**ID:** `litespec/core/behavior#state-the-audit-position-of-every-command`

**Statement:** Each command MUST declare its audit reason and target, or its exemption with its justification.

**Rationale:** An unaudited change with no exemption is a gap nobody admitted: money moves, holds lapse, and no record says who asked. Declaring the reason of `required`, `optional`, or `none` with its target, or the exemption with its justification, states the audit beside the change. Commands are audited by default with reason `none`, and async work audits at request time.

**Checked by:** `litespec consumer` fails a command without an audit reason and target or an exemption with its justification.

### Declare Every Query Output

**ID:** `litespec/core/behavior#declare-every-query-output`

**Statement:** Each query MUST declare its output in named `include` groups.

**Rationale:** A query without a declared output returns whatever its author fetched: readers and screens guess the shape from code. Declaring the output in named groups with fields, classifications, sources, and freshness states what readers receive, so the shape is reviewable before anything implements it. The `query-output-missing` check reports a query with no declared output.

**Example:** The list-expenses query declares open and decided groups with immediate freshness.

**Checked by:** `litespec model check` reports `query-output-missing` for a query with no declared output.

### Justify Every Wider Visibility

**ID:** `litespec/core/behavior#justify-every-wider-visibility`

**Statement:** Each include group visible to more roles than its sources MUST cite the approved requirement widening it in `wideningBecause`.

**Rationale:** A read showing more than its sources is a quiet widening: roles see figures their own grants never allowed, and no requirement approved the view. Citing the approved requirement in `wideningBecause` keeps the widening a decided choice, and `visibleWhen` states the condition where one narrows it. The `widening-unjustified` check reports a widened group with no cited requirement.

**Checked by:** `litespec model check` reports `widening-unjustified` for a widened include group with no cited requirement.

### Declare Paging on Every Collection Query

**ID:** `litespec/core/behavior#declare-paging-on-every-collection-query`

**Statement:** Each collection query MUST declare its paging or `none` with its reason.

**Rationale:** A collection without paging is a promise the data outgrows: the first hundred rows return, the next ten thousand time out. Declaring the order, the cursor or offset, and the default and maximum limits states the paging beside the read, and `none` with its reason states why a query returns one object. The `paging-undeclared` check reports a collection query with no paging.

**Checked by:** `litespec model check` reports `paging-undeclared` for a collection query with no paging.

### Mark Every Lagging Query as Built Asynchronously

**ID:** `litespec/core/behavior#mark-every-lagging-query-as-built-asynchronously`

**Statement:** Each query whose source lags MUST set `builtAsynchronously` with a bounded or advisory freshness and a lag scenario.

**Rationale:** A read built asynchronously without a mark is trusted as fresh: screens show yesterday figures as now, and nobody warned them. Marking the query, with a bounded or advisory freshness and one scenario covering the lag, states the staleness beside the promise.

**Reviewed for:** Each lagging query sets `builtAsynchronously` with a bounded or advisory freshness and a lag scenario.

### End Every Use Case in Named Results

**ID:** `litespec/core/behavior#end-every-use-case-in-named-results`

**Statement:** Each use case MUST declare its success result and every refusal with its code.

**Rationale:** Callers branch on named endings: success, or one refusal with its stable code. A use case with no named success leaves readers guessing what good looks like, and a refusal without a code leaves each architecture layer reporting the same breach another way. The `use-case-without-success` check reports a use case with no success result, and `result-undeclared` reports a code the records never declared.

**Example:** The reimburse-expense command declares `reimbursed` with `spending.not-approved` and `spending.already-reimbursed`.

**Checked by:** `litespec model check` reports `use-case-without-success` for a use case with no success result; each refusal entry on the use-case page carries its code.

### Name Each Refusal Code for the State That Refuses

**ID:** `litespec/core/behavior#name-each-refusal-code-for-the-state-that-refuses`

**Statement:** Each refusal code MUST read `<module>.<noun>-<state>` and name the state, not the failure.

**Rationale:** The code is a public contract; `invalid`, `failed` or `error` tell an integrator nothing to do. A code naming the state tells what holds and what to change. The prefix names the module that owns the refused state, which may differ from the module of the refusing use case; a shared kernel declares its prefix in `refusals.prefixes`, and a code built at run time from a key, such as the conflict for a lost uniqueness key, declares its template in `refusals.composed`.

**Example:** `spending.receipt-missing` names the missing receipt; `spending.receipt-invalid` hides the state behind a vague word and is reported.

**Checked by:** `litespec model check` reports `refusal-code-form` (tier A) for a refusal code outside its module, outside kebab-case, or carrying a vague word.

### Order Every Refusal List by Precedence

**ID:** `litespec/core/behavior#order-every-refusal-list-by-precedence`

**Statement:** Each use case MUST list its refusals so the first match wins.

**Rationale:** Two refusals matching one call need an order: without precedence, each architecture layer picks its own refusal, and callers branch on a different code per stack. Listing refusals in precedence order makes the first match the one every architecture layer reports.

**Reviewed for:** The refusal entries on each use-case page stand in precedence order with first match winning.

### Record Attempts Behind Recorded Refusals

**ID:** `litespec/core/behavior#record-attempts-behind-recorded-refusals`

**Statement:** Each refusal carrying `recorded: true` MUST store the attempt with its reason.

**Rationale:** Some denials are forensics: a loan denial, a limit refusal, a fraud block. Storing the attempt with its reason keeps the evidence the business answers with, while a validating refusal such as a malformed code stores nothing.

**Example:** The self-checkout scan records unknown, retired, and already-lent barcodes with their reasons, while a malformed scan stores nothing.

**Reviewed for:** Each refusal carrying `recorded: true` stores the attempt with its reason; validating refusals store nothing.

### List Recorded Outcomes Inside Success Codes

**ID:** `litespec/core/behavior#list-recorded-outcomes-inside-success-codes`

**Statement:** Each success result with recorded outcomes MUST list them in `codes`.

**Rationale:** A success hiding several recorded outcomes forces callers to guess: borrow and renew both succeed, but the due date and the analytics differ. Listing the closed codes inside the success states each recorded outcome beside the ending.

**Example:** The checked-out result lists `borrow` and `renew`.

**Reviewed for:** Each success result with recorded outcomes lists them in `codes` beside the result.

### Derive Generic Refusals Instead of Writing Them

**ID:** `litespec/core/behavior#derive-generic-refusals-instead-of-writing-them`

**Statement:** A use case MUST NOT write a generic forbidden or invalid-input refusal as a record.

**Rationale:** Hand-written generic refusals multiply: hundreds of pages each declare their own forbidden, and each drifts in wording and code. Deriving them from `access` and `input` keeps one forbidden and one invalid input per use case, current with the grant. Scenarios point at the derived results without anyone writing them.

**Reviewed for:** No use-case page writes a generic forbidden or invalid-input refusal as a record.

### Name the Result Each Scenario Ends In

**ID:** `litespec/core/behavior#name-the-result-each-scenario-ends-in`

**Statement:** Each scenario MUST name the result it ends in, written or derived.

**Rationale:** A scenario that names no result illustrates behavior without asserting an ending: it reads well and proves nothing. Naming the result ties each example to the ending it walks toward, so every test knows what to assert. The `scenario-result-unresolved` check reports a scenario ending in an unknown result.

**Checked by:** `litespec consumer` fails a scenario entry without `result`, and `litespec model check` reports `scenario-result-unresolved` for a scenario ending in an unknown result.

### Prove Every Written Result with a Scenario

**ID:** `litespec/core/behavior#prove-every-written-result-with-a-scenario`

**Statement:** Each written result MUST be shown by at least one scenario of its use case.

**Rationale:** A written result no scenario shows is an ending nobody proved: it reads on the page and is asserted in no test, so it drifts from the code with the next change. One scenario per result pins each ending as a checked fact, including the refusals a caller can meet.

**Example:** The reimburse-expense command declares the `already-reimbursed` refusal, and a scenario walks an approved expense claimed twice to that refusal.

**Checked by:** `litespec model check` reports `result-without-scenario`, tier G for a success result and tier A for a refusal.

### Write Every Scenario in Given When Then

**ID:** `litespec/core/behavior#write-every-scenario-in-given-when-then`

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

### Write Prose on Significant Use Cases

**ID:** `litespec/core/behavior#write-prose-on-significant-use-cases`

**Statement:** Each use case changing more than one aggregate, refusing beyond the generic refusals, carrying risk, or issuing inline effects MUST carry Goal and What Happens.

**Rationale:** Prose on every use case buries the significant ones: a simple read carries two sections saying what the frontmatter already says. Requiring Goal and What Happens only on significant use cases keeps prose where judgment lives, and frontmatter plus Goal suffice elsewhere. The `prose-missing` check reports a significant use case without its prose as advisory.

**Checked by:** `litespec model check` reports `prose-missing` for a significant use case without Goal and What Happens.

### Trigger Every Reaction from a Declared Event

**ID:** `litespec/core/behavior#trigger-every-reaction-from-a-declared-event`

**Statement:** Each reaction MUST name the domain or integration events it answers in `on`.

**Rationale:** A reaction without a trigger never runs or runs whenever its author remembered: reviewers see behavior with no moment attached. Naming the answered events states when the behavior fires, and an `if` condition states when it skips. The `reaction-trigger-unresolved` check reports a trigger naming an unknown event.

**Example:** The send-due-notice reaction answers the loan loan-borrowed domain event and issues the send-due-notice command.

**Checked by:** `litespec model check` reports `reaction-trigger-unresolved` for a trigger naming an unknown event.

### Issue or Send from Every Reaction

**ID:** `litespec/core/behavior#issue-or-send-from-every-reaction`

**Statement:** Each reaction MUST declare its edges in `effects`, with exactly one verb per entry.

**Rationale:** A reaction with no declared edge observes and never acts, while an edge outside `effects` hides behavior the checks never see. Declaring each edge in `effects` with exactly one verb of `issues`, `sends`, `publishes`, or `calls` keeps every consequence of the automation beside its trigger, under one grammar for every record that has edges. The `record-shape` check reports a reaction with no `effects` and a reaction holding top-level `issues` or `sends`.

**Checked by:** `litespec model check` reports `record-shape` for a reaction with no `effects` and for top-level `issues` or `sends`.

### Write No Aggregate Directly from a Reaction

**ID:** `litespec/core/behavior#write-no-aggregate-directly-from-a-reaction`

**Statement:** A reaction MUST NOT write an aggregate except through the use case it issues.

**Rationale:** A reaction writing directly splits the write path: the same aggregate changes through a reviewed command and through an unreviewed automation, each with its own guards. Issuing a small command keeps every write behind a use case with its access, consistency, results, and scenarios.

**Reviewed for:** Each reaction changes aggregates only through the use case it issues.

### File Every Reaction with the Module It Changes

**ID:** `litespec/core/behavior#file-every-reaction-with-the-module-it-changes`

**Statement:** Each reaction MUST live with the module whose records it changes, or with the notification module when it only sends.

**Rationale:** Reactions filed by trigger scatter: one module behavior hides on a dozen trigger pages, and nobody sees the module whole. Filing each reaction with the module it changes keeps each module behavior in one place. Its event handler lives in that module's code under the `On{Fact}` folder (`litespec/workspace/naming#group-event-handlers-by-the-fact-they-handle`). The trigger page shows reactions to its event as a view derived from records.

**Reviewed for:** Each reaction page sits with the module it changes, or with the notification module when it only sends, and its event handler lives in that module's code under the `On{Fact}` folder.

### Name Each Reaction for Its Act and Its Trigger

**ID:** `litespec/core/behavior#name-each-reaction-for-its-act-and-its-trigger`

**Statement:** Each reaction MUST be named `<verb>-<what>-on-<fact>`, with a declared verb.

**Rationale:** A reaction runs because something happened, and a name without its trigger hides why it runs: `send-due-notice` could fire on a borrow, a renewal or a reminder job. Naming the act and the fact lets a reader find every consequence of a fact by its name, and keeps two reactions to one fact from colliding. The `reaction-name-form` check reports a reaction name without a declared verb or without `-on-`.

**Example:** `reaction/lending.send-due-notice-on-loan-borrowed` answers `event/loan.loan-borrowed` and issues the notice; `reaction/lending.notify-borrower` is reported.

**Checked by:** `litespec model check` reports `reaction-name-form` for a reaction name without a declared verb or without `-on-`.

### State the Failure Handling of Every Reaction

**ID:** `litespec/core/behavior#state-the-failure-handling-of-every-reaction`

**Statement:** Each reaction MUST declare its structured `onFailure` policy and its idempotency.

**Rationale:** A failed automation without handling retries forever or drops silently: notices double-send, holds never clear. Declaring the `onFailure` policy states the failure shape beside the trigger: `retry` is `none`, `next-run`, or a retry object with `maxAttempts`, `initialDelay`, `maxDelay`, and `backoff` of `exponential` or `fixed`; `deadLetter` carries `after` with the action a person takes; `surfacesIn` names the entry point showing the failure and `runbook` the recovery procedure. Delays and `after` are ISO 8601 durations, and prose handling is refused. The same shape serves a job. The idempotency mode, usually `state`, states how repeats collapse.

**Checked by:** `litespec consumer` fails a reaction with `onFailure` outside the retry and dead-letter shape; `litespec model check` reports `record-shape` for prose handling, a missing or malformed retry, and a malformed duration.

### Point Reaction Scenarios at the Issued Result

**ID:** `litespec/core/behavior#point-reaction-scenarios-at-the-issued-result`

**Statement:** Each scenario on a reaction MUST end in a result of a use case its effects issue.

**Rationale:** A reaction scenario ending anywhere else proves another behavior story: it walks the trigger and asserts an ending no issued use case declares. Ending in an issued use case result keeps the scenario a proof of the automation consequence.

**Checked by:** `litespec model check` reports `scenario-result-unresolved` for a reaction scenario ending outside the issued use case results.

### Declare Every Job with Kind and Structured Schedule

**ID:** `litespec/core/behavior#declare-every-job-with-kind-and-structured-schedule`

**Statement:** Each job MUST declare its kind, scope, effects, reads, service level, evidence, and runbook, with a structured schedule when it runs on time.

**Rationale:** A job without a declared scope and promise is a blind run: nobody states what each run selects, what it runs per item, what it guarantees, or where proof lands. Declaring the kind of `scheduled`, `one-off`, or `operator`, the scope with its batch size and lease, the `effects` entries with the aggregates read, the service level citing its requirement, the evidence, and the runbook states the run beside its promise. The schedule is structured with its timezone, never prose. The `schedule-unstructured` check reports a schedule no tool can read, and `record-shape` reports a job holding top-level `issues`.

**Example:** The expire-lapsed-holds job runs every six hours over open holds past their window, issues one expiry per item, and lands its evidence in the run log.

**Checked by:** `litespec model check` reports `schedule-unstructured` for a schedule no tool can read and `record-shape` for top-level `issues`; `litespec consumer` fails a job without kind, scope, effects, reads, service level, evidence, or runbook.

### Name Each Job for the Command It Issues

**ID:** `litespec/core/behavior#name-each-job-for-the-command-it-issues`

**Statement:** Each job MUST start with the verb of a use case it issues.

**Rationale:** A job decides nothing about an item; it selects the next items and issues one command for each. Naming it for that command lets a reader go from the schedule to the behavior in one step, and stops a job named `sweep-` or `process-` from hiding what it changes. The `job-name-form` check reports a job whose first word is not the verb of a use case it issues.

**Example:** `job/lending.expire-lapsed-holds` issues `use-case/lending.expire-holds` and passes; `job/lending.sweep-holds` issuing the same use case is reported.

**Checked by:** `litespec model check` reports `job-name-form` for a job whose first word is not the verb of a use case it issues.

### Guard Every Operator Job

**ID:** `litespec/core/behavior#guard-every-operator-job`

**Statement:** Each operator job MUST state where it runs in `runsWhere` and the guard it runs under.

**Rationale:** Interactive platform work without a guard runs anywhere by anyone: a rehearsal harness touches production, a manual sweep runs twice. Stating where it runs and the guard it runs under keeps operator work inside its fence. The `operator-job-without-guard` check reports an operator job with no guard.

**Checked by:** `litespec model check` reports `operator-job-without-guard` for an operator job without `runsWhere` or guard.

### Describe Every Notification with Audience Channel and Consent

**ID:** `litespec/core/behavior#describe-every-notification-with-audience-channel-and-consent`

**Statement:** Each notification MUST declare its template, addresses, channel, consent, frequency, dedup key, parameters, and locale.

**Rationale:** A message without declared addresses and consent is sent on hope: the wrong actors receive it, the channel ignores consent, repeats annoy. Declaring the template with its parameters and locale, the addresses, the channel, the consent rule, the frequency of `once` or `per-attempt`, and the dedup key states the message beside its promise. Suppression lives on each `sends` edge and in suppression scenarios. The `notification-without-sender` check reports a notification no use case or reaction sends.

**Checked by:** `litespec model check` reports `notification-without-sender` for a notification no use case or reaction sends; `litespec consumer` fails a notification without its declared fields.

### Keep Notification Templates and the Template Store One Set

**ID:** `litespec/core/behavior#keep-notification-templates-and-the-template-store-one-set`

**Statement:** The `template` keys the notification records declare and the keys the project's template store holds MUST be the same set.

**Rationale:** The template key is the one join between a notification's promise and the text a person receives. A declared key the store lacks fails at send time, and a stored template no notification declares is text nobody reviewed. The store is the stack's own file, so the check reads it where it is instead of copying the keys into a record. A notification whose body the sender composes at send time declares `template: none` and is left out.

**Example:** Claimwick's `notification/spending.approval-reminder` declares `template: approval-reminder-manager`, and the template store holds `approval-reminder-manager`.

**Checked by:** `litespec model check` reports `template-unimplemented` (tier S) for a declared key the store the stack page names lacks, and `template-unrecorded` (tier S) for a stored key no notification declares, in a project whose stack configuration names a template store.

### Use Every Declared Template Parameter

**ID:** `litespec/core/behavior#use-every-declared-template-parameter`

**Statement:** Every parameter a notification declares in `params` MUST appear as a placeholder in at least one of its template's texts.

**Rationale:** A declared parameter is part of the message's promise: the order number in a confirmation, the name in a reminder. A parameter the template never uses is a promise the message does not keep, and it usually means the template was rewritten without the record.

**Example:** `notification/spending.approval-reminder` declares the parameter `submitterName`, and the `approval-reminder-manager` template uses `{{SubmitterName}}`.

**Checked by:** `litespec model check` reports `template-param-drift` (tier S) for a declared parameter whose placeholder, in the case the stack page states, appears in none of its template's texts.

### Name Each Template for Its Subject, Event and Audience

**ID:** `litespec/core/behavior#name-each-template-for-its-subject-event-and-audience`

**Statement:** Each notification template key MUST read `<subject>-<event>-<audience>`, with the audience name the record declares in `addresses`.

**Rationale:** A key naming its subject, its event and its audience tells each reader what the message is about and who receives it; a key naming none of the three hides both.

**Example:** `approval-reminder-manager` names the approval, the reminder and the manager audience; `email-change-confirmation` names no audience and is reported.

**Checked by:** `litespec model check` reports `template-key-form` (tier A) for a template key ending in no name the notification declares in `addresses`.

### Claim Every Notification Promise from a Requirement

**ID:** `litespec/core/behavior#claim-every-notification-promise-from-a-requirement`

**Statement:** Each claim a notification makes MUST cite its requirement.

**Rationale:** A message promising what no requirement holds is a promise nobody approved: the text assures a refund the policy never states. Citing the requirement behind each claim keeps every assured sentence a carried promise.

**Reviewed for:** Each notification claim cites its requirement beside the claim.

### Hold Every Setting Default to a Requirement

**ID:** `litespec/core/behavior#hold-every-setting-default-to-a-requirement`

**Statement:** Each setting MUST declare its value type, range, unit, setter, and bound behavior, with the requirement behind a commercial, legal, or varying default.

**Rationale:** A default no requirement holds is a figure nobody approved: a price, a fee, a term, or a limit nobody decided. Declaring the value type, the range, the unit, the setter of decider, operator, or tenant, and the bound behavior of `refuse`, `truncate`, or `degrade` states the tunable beside its promise, and a commercial, legal, or tenant-, market-, or date-varying default cites its requirement. The `setting-default-without-requirement` check reports such a default with no cited requirement.

**Example:** The receipt threshold declares money with a decider-set default of EUR 25, refusing an expense above the line with no receipt, behind its requirement.

**Checked by:** `litespec model check` reports `setting-default-without-requirement` for a commercial, legal, or varying default with no cited requirement.

### Name the Use Cases That Read Each Setting

**ID:** `litespec/core/behavior#name-the-use-cases-that-read-each-setting`

**Statement:** Each setting MUST name the use cases that read it in `readBy`.

**Rationale:** A setting read by a use case no record names is a coupling reviewers cannot see: the tunable moves, the reader breaks, and no diff shows the pair. Naming the readers in `readBy` states the coupling beside the setting, so each change to the range or default meets its readers. Settled by [decision/0007-adopt-setting-readby](../intent/decisions/0007-adopt-setting-readby.md) on the evidence in [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence.md) and [research/field-naming-evidence](../intent/research/field-naming-evidence.md).

**Example:** The receipt threshold names `use-case/spending.submit-expense` in `readBy`, so a move of the threshold meets the submission story.

**Reviewed for:** Which settings are read by a use case that `readBy` does not name? Look at the setting entries beside the use cases that read them.

### Name the Configuration Key of Every Operator Setting

**ID:** `litespec/core/behavior#name-the-configuration-key-of-every-operator-setting`

**Statement:** A setting the operator sets MUST name its configuration path in `key`.

**Rationale:** An operator setting reaches the product through configuration, so the record and the configuration meet at one path: the record states the default and the bounds, and the configuration names the value. Without the key, a renamed section silently falls back to a default nobody chose, and no reader can tell which configuration value a promise rests on. A setting a tenant or the decider sets is held as data by an aggregate instead, and names no key.

**Example:** Claimwick's `setting/spending.approval-reminder-delay` is set by the operator and carries `key` `Spending:ApprovalReminderDelay`.

**Checked by:** `litespec model check` reports `setting-key-missing` (tier G) for a setting with `setBy: operator` and no `key`.

### Bind Every Keyed Setting in One Marked Options Class

**ID:** `litespec/core/behavior#bind-every-keyed-setting-in-one-marked-options-class`

**Statement:** A setting with a `key` MUST be bound by an options class that carries an `implements` mark naming the setting and whose section and property spell the key.

**Rationale:** The options class is where configuration becomes a typed value the code reads, so it is the code element that implements the setting. Marking it names the promise the class holds, and comparing its section name and property with the record's key finds the renamed section or the typo before the product starts with a default.

**Example:** Claimwick's `SpendingOptions` declares `SectionName = "Spending"`, an `ApprovalReminderDelay` property, and `/// <implements>setting/spending.approval-reminder-delay</implements>`.

**Checked by:** `litespec model check` reports `setting-unimplemented` (tier S) for a keyed setting no options class marks, and `setting-key-drift` (tier S) for a marked options class whose `SectionName` constant and property do not spell the setting's `key`.

### State the Unit of Every Quantity Setting

**ID:** `litespec/core/behavior#state-the-unit-of-every-quantity-setting`

**Statement:** Each setting whose value is a number or a duration MUST declare its `unit`.

**Rationale:** A number without a unit is a guess: minutes read as seconds, a count reads as money. Declaring the unit states the measure beside the value.

**Example:** The retry backoff declares `unit: seconds`; a timeout with `valueType: duration` and no unit is reported.

**Checked by:** `litespec model check` reports `setting-unit-missing` (tier A) for a numeric or duration setting declaring no `unit`.

### Keep Model Invariants in Rules

**ID:** `litespec/core/behavior#keep-model-invariants-in-rules`

**Statement:** A model invariant MUST live as a rule, never as a setting.

**Rationale:** A setting tunes; a rule holds. An invariant filed as a setting reads as tunable, and a later operator tunes away what the model guaranteed. Keeping invariants in rules keeps what holds apart from what varies.

**Reviewed for:** No setting page holds a model invariant; each invariant lives as a rule.

### Give Each Entry Point a Kind

**ID:** `litespec/core/behavior#give-each-entry-point-a-kind`

**Statement:** Each entry point MUST declare its kind and its `auth`.

**Rationale:** A door with no kind is entered any way: a webhook is called as a page, a device as a browser. Declaring the kind of `browser`, `api`, `webhook`, `device`, `cli`, `mcp`, `served-content`, `reaction`, or `job` with its auth states how callers arrive and prove themselves.

**Checked by:** `litespec consumer` fails an entry point without `kind` or `auth`.

### Give Every Page Its Screen Record

**ID:** `litespec/core/behavior#give-every-page-its-screen-record`

**Statement:** Every route a browser or device entry point serves MUST be the implementation of exactly one screen record.

**Rationale:** A page is where a person meets the product, and an unrecorded page offers use cases nobody reviewed as a page: what it lets someone start, which journey step it carries, which refusal it shows. One record per page, with one mark on the route, makes the page a reviewable promise and lets every other link (calls, results, journey coverage) be computed from it.

**Example:** Claimwick's web route `/expenses` carries `implements: "screen/claimwick-app.expense-list"`, and `docs/behavior/screens/claimwick-app.expense-list.md` offers `use-case/spending.list-expenses`.

**Checked by:** `litespec model check` reports `screen-unimplemented` (tier S) for a screen no route marks, and `behavior-unrecorded` (tier S) for a route with no mark or a mark naming no screen.

### Match a Screen's Offers to Its Calls

**ID:** `litespec/core/behavior#match-a-screens-offers-to-its-calls`

**Statement:** A screen's `offers` MUST equal the use cases its route reaches through the generated client.

**Rationale:** `offers` is the reviewed promise of what a person can start on the page, and the calls are what the page really does. When the two differ, either the page grew a capability nobody approved or the record promises one the page dropped. Comparing them on every run keeps the record honest without anyone re-reading the page.

**Example:** The expense list offers `use-case/spending.list-expenses`, and its route calls `ListExpenses` through the generated client.

**Checked by:** `litespec model check` reports `screen-calls-mismatch` (tier S) for a screen whose `offers` and computed calls differ, naming each use case on one side only.

### Serve Only the Steps a Screen's Use Cases Serve

**ID:** `litespec/core/behavior#serve-only-the-steps-a-screens-use-cases-serve`

**Statement:** A screen's `serves` MUST name only journey steps that a use case it offers serves through a browser or device entry point.

**Rationale:** A journey step is achieved through use cases, and a person reaches those use cases on a screen. Naming the step on the screen lets the book say where each step happens, and checking it against the offered use cases finds a screen claiming a step its use cases do not carry.

**Example:** The expense form serves `journey/submit-and-reimburse#s1` because it offers `use-case/spending.submit-expense`, which serves that step.

**Checked by:** `litespec model check` reports `screen-serves-mismatch` (tier G) for a served step no offered use case serves.

### Give Every Browser Step a Screen

**ID:** `litespec/core/behavior#give-every-browser-step-a-screen`

**Statement:** Every journey step a use case serves through a browser or device entry point MUST be served by at least one screen.

**Rationale:** A step a person achieves in a browser happens on some page. A step no screen serves is either carried by a page nobody recorded or not reachable at all, and the book cannot tell the reader where it happens. The pair sits side by side: `step-unserved` names a step no use case serves, and `step-unscreened` names a step a browser or device use case serves that no screen serves.

**Example:** `journey/submit-and-reimburse#s1` is served by the expense form.

**Checked by:** `litespec model check` reports `step-unscreened` (tier G) for a step a browser or device use case serves that no screen serves.

### Show Every Refusal a Screen Can Reach

**ID:** `litespec/core/behavior#show-every-refusal-a-screen-can-reach`

**Statement:** Every refusal a use case a screen offers can return MUST have a sentence in the refusal dictionary its frontend names in `refusalDictionary` or a browser test that asserts its code.

**Rationale:** A refusal a person can trigger on a page is part of what the page says to them. Without a sentence, the person sees a code or a generic error; without a test, nobody knows what they see. Requiring one of the two only where a screen can reach the refusal avoids demanding sentences for refusals no person meets. Each frontend names its dictionary in its `paths.frontends[]` entry, so the check reads that frontend's words rather than a convention.

**Example:** The expense form offers `use-case/spending.submit-expense`, whose `spending.receipt-missing` refusal has a sentence in the dictionary the Claimwick web frontend names in `refusalDictionary`.

**Checked by:** `litespec model check` reports `result-unshown` (tier A) for a refusal a screen can reach with neither a sentence in its frontend's `refusalDictionary` nor a browser test asserting its code.

### Serve Static Content Through Served Content

**ID:** `litespec/core/behavior#serve-static-content-through-served-content`

**Statement:** A static script or legal text MUST be served content with its path, content type, cache control, and versioning, never a use case.

**Rationale:** A static file modeled as a use case invents behavior where none exists: results, scenarios, and handlers for bytes served unchanged. Serving it as content with its path, content type, cache control, versioning, and fulfilled requirements keeps content apart from behavior.

**Reviewed for:** Each static script or legal text sits as served content with path, content type, cache control, and versioning.

### Describe Every External System Operation

**ID:** `litespec/core/behavior#describe-every-external-system-operation`

**Statement:** Each external system MUST describe its operations with direction, timeout, timeout handling, retries, response, and cache.

**Rationale:** An outside call without a declared shape fails by surprise: timeouts hang, retries double-charge, responses unparse. Describing each operation with its direction of `out` or `in`, its timeout with its timeout handling, its retries, its response, and its cache states the coupling beside the system. The system acts inside the product as one named actor.

**Checked by:** `litespec consumer` fails an external system operation without direction, timeout, timeout handling, retries, response, or cache.

### Route Every External Erasure Through an Operation or Event

**ID:** `litespec/core/behavior#route-every-external-erasure-through-an-operation-or-event`

**Statement:** Each external system MUST declare its erasure as a call, an integration event, or `impossible` with its reason and acceptance.

**Rationale:** Personal data forwarded to a processor without an erasure route stays there past erasure: the product forgets while the processor remembers. Declaring a call to an operation, a forwarding integration event, or `impossible` with its reason and its Intent acceptance states the route beside the system.

**Reviewed for:** Each external system declares its erasure call, forwarding event, or impossible reason with acceptance.

### Schedule Every Data Class for Retention

**ID:** `litespec/core/behavior#schedule-every-data-class-for-retention`

**Statement:** Each data class MUST appear on the retention schedule with its basis, period, enforcement, and intent link where personal or sensitive.

**Rationale:** Retention decided per aggregate scatters: one module erases in days what another keeps for years, and no page shows the whole. Listing every data class on one schedule with its basis, period, enforcement, processors with their forwarding and acceptance, and the conflict rule where keeping and erasing collide states the whole as debated rows. Personal or sensitive keeping cites its Intent requirement. The `retention-without-enforcement` check reports a row nothing enforces.

**Checked by:** `litespec model check` reports `retention-without-enforcement` for a data class nothing enforces.

### Keep Design Choices Out of the Records

**ID:** `litespec/core/behavior#keep-design-choices-out-of-the-records`

**Statement:** Child entities and value objects MUST appear only in code and the glossary, never as records.

**Rationale:** The records name what carries business meaning; the design inside an aggregate changes without changing that meaning. Recording each child and value as its own record would triple the tree while adding no decision a reviewer approves. Code and the glossary carry them, where builders and readers meet them.

**Example:** Order lines live as a child of the order aggregate, and money as a value with its glossary entry; neither is a record.

**Reviewed for:** No record page declares a child entity or value object as a record type.

## Conventions

### Illustrate Pages with Examples from the Cast

**ID:** `litespec/core/behavior#illustrate-pages-with-examples-from-the-cast`

**Default:** Each module, aggregate, and use case page carries an Example section drawn from the example cast.

**Replacement:** A project names the pages that carry an Example section in its documentation.

**Rationale:** Every other section states a rule, a state, or a mapping; none says when the behavior happens or who is under pressure while it does. An occasion drawn from the one shared cast lets a reviewer judge whether the behavior fits the world it serves, and lets readers carry the same people and figures from page to page.

**Reviewed for:** Each module, aggregate, and use case page carries an Example section from the cast, or the project documentation names the pages that do.

### Group Cross-Aggregate Rules by Theme

**ID:** `litespec/core/behavior#group-cross-aggregate-rules-by-theme`

**Default:** Each rule page holds the cross-aggregate rules of one theme, named in its Summary.

**Replacement:** A project names its rule-page grouping in its documentation.

**Rationale:** Cross-aggregate rules share no aggregate to gather them, so an ungrouped rule page becomes a list of unrelated checks. One theme per page, stated in its Summary, keeps each page reviewable as one decision about what holds across the domain.

**Reviewed for:** Each rule page states its theme in Summary, or the project documentation names another grouping.

## Reference Example

This informative example demonstrates `litespec/core/behavior#declare-the-kind-of-every-aggregate`, `litespec/core/behavior#model-each-aggregate-lifecycle-with-states`, `litespec/core/behavior#state-each-process-step-with-its-compensation`, `litespec/core/behavior#close-every-process-with-named-exits-and-a-driver`, `litespec/core/behavior#state-each-rule-with-its-kind`, `litespec/core/behavior#give-each-use-case-a-command-or-query-kind`, `litespec/core/behavior#connect-each-use-case-to-intent`, `litespec/core/behavior#grant-each-use-case-scoped-access`, `litespec/core/behavior#bound-every-caller-supplied-input-value`, `litespec/core/behavior#state-the-consistency-of-every-use-case`, `litespec/core/behavior#declare-every-effect-with-one-verb-and-its-timing`, `litespec/core/behavior#end-every-use-case-in-named-results`, `litespec/core/behavior#order-every-refusal-list-by-precedence`, `litespec/core/behavior#trigger-every-reaction-from-a-declared-event`, `litespec/core/behavior#issue-or-send-from-every-reaction`, `litespec/core/behavior#file-every-reaction-with-the-module-it-changes`, `litespec/core/behavior#state-the-failure-handling-of-every-reaction`, `litespec/core/behavior#tabulate-every-calculation-with-examples`, `litespec/core/behavior#record-attempts-behind-recorded-refusals`, `litespec/core/behavior#list-recorded-outcomes-inside-success-codes`, and `litespec/core/behavior#name-the-idempotency-of-every-use-case`.

**Hold queue.** A reader who finds the copy lent places a hold. The hold queue aggregate is a process with states `Requested`, `Ready`, `Collected`, `Cancelled`, and `Lapsed`, steps from hold-place to collect with compensations, exits for cancellation and lapse, and a driver of a completion reaction, an expiry job, and the librarian. The collect-ready-hold command serves the hold journey step, fulfills the pickup-window requirement, declares session auth with the place-hold permission, bounds the caller-supplied pickup code with the code-shape rule, writes atomically because of the one-ready-hold rule, and publishes the collected integration event after commit. Its refusals stand in precedence order ahead of the hold-collected result. The advance-queue reaction answers the collected domain event, issues the advance-hold-queue command from the lending module through its effects, and retries five times with exponential backoff before dead-lettering. The fine-per-day rule is proven by a scenario with an examples table: a copy three days overdue accrues EUR 1.50 at EUR 0.50 a day, and a copy returned on time accrues nothing.

**Self-checkout scan.** A kiosk records each scan attempt. The record-self-checkout-scan command declares device auth with a system grant, per-record consistency, and idempotency keyed on `{nonce}` scoped to the login. Its recorded refusals for unknown, retired, and already-lent barcodes store each attempt with its reason, while malformed scans store nothing, and its checked-out result lists `borrow` and `renew` codes.
