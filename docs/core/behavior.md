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
| aggregate, saga | one coordinator whose steps run use cases on other aggregates, each with its compensation |
| aggregate, store | one state holder with a named owner and no guarded invariant |
| aggregate `states` | the lifecycle of the aggregate |
| (none) | child entity, changed only through the root |
| (none) | value object, compared by value |
| rule, invariant | an invariant checked on every change |
| rule, precondition | a guard the behavior passes before it runs |
| rule, calculation | a pure computation on the aggregate or its values |
| rule, cross-aggregate | a stored constraint, or a reaction or job reaching eventual consistency |
| rule, precondition spanning aggregates | a synchronous read of the guarding aggregate named in `reads`, with `consistency: atomic` and the guarding rule in `cites`; use a stored constraint only when the guard is a single stored value, and a reaction or job only when eventual consistency is acceptable |
| rule, atomicity | one atomic write group across the aggregates it binds |
| rule, reservation | a held claim with its release |
| rule, commercial-data | a frozen commercial value with its history |
| rule, default | a default value the model holds |
| event | a domain event raised through the root |
| integration event | a published fact in CloudEvents form |
| use case, command | one handler behind the use case |
| use case, query | one read behind the use case |
| reaction | one reaction answering its events |
| job, scheduled | one job running on its schedule |
| job, one-off | one job running once |
| job, manual | one job a person runs |
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

**Reviewed for:** Which record pages declare a child entity or value object as a record type? Look at the record types beside the aggregate pages.

## Conventions

### Illustrate Pages with Examples from the Cast

**ID:** `litespec/core/behavior#illustrate-pages-with-examples-from-the-cast`

**Default:** Each module, aggregate, and use-case page carries an Example section drawn from the example cast, removed where no walk adds to the records.

**Replacement:** A project names the pages that carry an Example section in its documentation.

**Rationale:** Every other section states a rule, a state, or a mapping; none says when the behavior happens or who is under pressure while it does. An occasion drawn from the one shared cast lets a reviewer judge whether the behavior fits the world it serves, and lets readers carry the same people and figures from page to page.

**Reviewed for:** Which module, aggregate, and use-case pages lack an Example section from the cast where a walk would add to the records, without a recorded project convention? Look at the Example sections beside the module, aggregate, and use-case pages.

### Group Cross-Aggregate Rules by Theme

**ID:** `litespec/core/behavior#group-cross-aggregate-rules-by-theme`

**Default:** Each rule page holds the cross-aggregate rules of one theme, named in its Summary.

**Replacement:** A project names its rule-page grouping in its documentation.

**Rationale:** Cross-aggregate rules share no aggregate to gather them, so an ungrouped rule page becomes a list of unrelated checks. One theme per page, stated in its Summary, keeps each page reviewable as one decision about what holds across the domain.

**Reviewed for:** Which rule pages state no theme in Summary without a recorded project convention? Look at the Summary of each rule page.

## Reference Example

This informative example demonstrates `litespec/core/use-cases#give-each-use-case-a-command-or-query-kind`, `litespec/core/use-cases#connect-each-use-case-to-intent`, `litespec/core/use-cases#grant-each-use-case-scoped-access`, `litespec/core/use-cases#name-the-entry-points-that-reach-each-use-case`, `litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification`, `litespec/core/use-cases#bound-every-caller-supplied-input-value`, `litespec/core/use-cases#state-the-consistency-of-every-use-case`, `litespec/core/use-cases#declare-every-effect-with-one-verb-and-its-timing`, `litespec/core/use-cases#name-the-idempotency-of-every-use-case`, `litespec/core/use-cases#state-the-audit-position-of-every-command`, `litespec/core/use-cases#end-every-use-case-in-named-results`, `litespec/core/use-cases#order-every-refusal-list-by-precedence`, `litespec/core/use-cases#name-the-result-each-scenario-ends-in`, `litespec/core/use-cases#prove-every-written-result-with-a-scenario`, `litespec/core/use-cases#write-every-scenario-in-given-when-then`, `litespec/core/use-cases#exercise-rules-from-scenarios`, `litespec/core/use-cases#share-scenario-setup-in-background`, `litespec/core/use-cases#record-attempts-behind-recorded-refusals`, `litespec/core/use-cases#list-recorded-outcomes-inside-success-codes`, `litespec/core/use-cases#retire-refusal-codes-with-their-replacements`, `litespec/core/access#declare-access-on-every-use-case`, `litespec/core/access#authenticate-every-caller-through-a-declared-auth-kind`, `litespec/core/access#grant-every-use-case-through-a-declared-grant`, `litespec/core/access#scope-every-grant-to-its-tenant-reach`, `litespec/core/edges#describe-every-integration-event-for-its-direction`, `litespec/core/edges#publish-every-outbound-integration-event-from-effects`, `litespec/core/edges#give-each-entry-point-a-kind`, `litespec/core/edges#give-every-screen-an-address-or-a-host`, and `litespec/core/edges#match-a-screens-offers-to-its-calls`.

**Hold queue.** A reader who finds the copy lent places a hold. The place-hold command serves the hold journey step and fulfills the pickup-window requirement. It declares the place-hold permission with own tenant scope in `access`, and the library browser door reaching it carries session auth. Its caller-supplied pickup code declares its type with the code-shape rule bounding it. The write commits atomically over the hold aggregate, and the hold-placed integration event, declared with its typed payload and trust, is emitted after commit. The derived forbidden and invalid-input refusals come first, then the written refusals in precedence order, ahead of the hold-placed result. Each scenario states its given, when, and then from the shared background, names the rules it exercises, and ends in a named result, so every written result meets its scenario. The command audits at required level with idempotency keyed on the request.

**Self-checkout scan.** A kiosk records each scan attempt. The record-self-checkout-scan command arrives through the device door with a system grant, writes each attempt on its own, and keys idempotency on `{nonce}` scoped to the login. Its recorded refusals for unknown, retired, and already-lent barcodes store each attempt with its reason, while malformed scans store nothing, and its checked-out result lists `borrow` and `renew` codes. A retired barcode refusal names its replacement and is never reused. The kiosk screen carries its address and offers the scan use case.
