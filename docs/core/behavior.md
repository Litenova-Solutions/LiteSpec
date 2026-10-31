# Behavior Records

## Intent

Behavior states how the system acts to keep the promises Intent records: the words it uses, the areas it divides into, the things it keeps consistent, the rules that hold, and the behavior it defines with its endings and examples. Agents write these records from Intent, and developers review them before code carries them. The [model page](model.md) states the shared record rules, the [intent page](intent.md) states the promises Behavior answers to, and the [record language page](language.md) states how each field, value and record name is written.

## Concepts

### The Behavior Pages

The five Behavior pages hold the records; this page holds the overview, the mapping from records to design, and the rules that span pages.

| Page | Holds |
|:---|:---|
| [Domain Model](domain-model.md) | Modules, glossary, aggregates, rules, and domain events. |
| [Use Cases](use-cases.md) | Commands, queries, results, scenarios, and settings. |
| [Automation](automation.md) | Reactions and jobs. |
| [Edges and Screens](edges.md) | Integration events, notifications, entry points, screens, and external systems. |
| [Privacy and Retention](privacy.md) | Data classes and the retention schedule. |

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
| rule, precondition spanning aggregates | a synchronous read of the guarding aggregate named in `reads`, with `consistency: atomic` and the guarding rule in `cites`; use a stored constraint only when the guard is a single stored value, and a reaction only when eventual consistency is acceptable |
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

This informative example demonstrates `litespec/core/domain-model#declare-the-kind-of-every-aggregate`, `litespec/core/domain-model#model-each-aggregate-lifecycle-with-states`, `litespec/core/domain-model#state-each-process-step-with-its-compensation`, `litespec/core/domain-model#close-every-process-with-named-exits-and-a-driver`, `litespec/core/domain-model#state-each-rule-with-its-kind`, `litespec/core/use-cases#give-each-use-case-a-command-or-query-kind`, `litespec/core/use-cases#connect-each-use-case-to-intent`, `litespec/core/use-cases#grant-each-use-case-scoped-access`, `litespec/core/use-cases#bound-every-caller-supplied-input-value`, `litespec/core/use-cases#state-the-consistency-of-every-use-case`, `litespec/core/use-cases#declare-every-effect-with-one-verb-and-its-timing`, `litespec/core/use-cases#end-every-use-case-in-named-results`, `litespec/core/use-cases#order-every-refusal-list-by-precedence`, `litespec/core/automation#trigger-every-reaction-from-a-declared-event`, `litespec/core/automation#issue-or-send-from-every-reaction`, `litespec/core/automation#file-every-reaction-with-the-module-it-changes`, `litespec/core/automation#state-the-failure-handling-of-every-reaction`, `litespec/core/domain-model#tabulate-every-calculation-with-examples`, `litespec/core/use-cases#record-attempts-behind-recorded-refusals`, `litespec/core/use-cases#list-recorded-outcomes-inside-success-codes`, and `litespec/core/use-cases#name-the-idempotency-of-every-use-case`.

**Hold queue.** A reader who finds the copy lent places a hold. The hold queue aggregate is a process with states `Requested`, `Ready`, `Collected`, `Cancelled`, and `Lapsed`, steps from hold-place to collect with compensations, exits for cancellation and lapse, and a driver of a completion reaction, an expiry job, and the librarian. The collect-ready-hold command serves the hold journey step, fulfills the pickup-window requirement, declares session auth with the place-hold permission, bounds the caller-supplied pickup code with the code-shape rule, writes atomically citing the one-ready-hold rule, and publishes the collected integration event after commit. Its refusals stand in precedence order ahead of the hold-collected result. The advance-queue reaction answers the collected domain event, issues the advance-hold-queue command from the lending module through its effects, and retries five times with exponential backoff before dead-lettering. The fine-per-day rule is proven by a scenario with an examples table: a copy three days overdue accrues EUR 1.50 at EUR 0.50 a day, and a copy returned on time accrues nothing.

**Self-checkout scan.** A kiosk records each scan attempt. The record-self-checkout-scan command declares device auth with a system grant, per-record consistency, and idempotency keyed on `{nonce}` scoped to the login. Its recorded refusals for unknown, retired, and already-lent barcodes store each attempt with its reason, while malformed scans store nothing, and its checked-out result lists `borrow` and `renew` codes.
