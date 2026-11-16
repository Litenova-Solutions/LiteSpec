# Behavior Records

**Stack:** any

**Concern:** records

Behavior records state how the system acts to keep the promises Intent records: the words it uses, the areas it divides into, the things it keeps consistent, the rules that hold, and the behavior it defines with its endings and examples. Agents write Behavior from Intent, and code carries it out. The [model page](model.md) states the shared record rules, the [intent page](intent.md) states the promises Behavior answers to, and the [record language page](language.md) states how each field, value, and record name is written.

## Concepts

### The Behavior Pages

Five pages hold the Behavior records, each owning its records. This page holds the overview, the mapping from records to design, and the rules that span pages.

| Page | Holds |
|:---|:---|
| [Domain Model](domain-model.md) | Modules, glossary, aggregates, rules, and domain events. |
| [Use Cases](use-cases.md) | Commands, queries, results, scenarios, and settings. |
| [Automation](automation.md) | Reactions and jobs. |
| [Edges and Screens](edges.md) | Integration events, notifications, entry points, screens, and external systems. |
| [Privacy and Retention](privacy.md) | Data classes with their erasure positions and retention. |

### Modules as Product Areas

A module names one product area of the bounded context. It is declared in Behavior, at `.litespec/behavior/modules/<module>/module.yaml`, beside the aggregates it groups, because a module is a code boundary with dependencies. Intent holds the product reasons for the area, not the boundary itself.

One name runs through every layer:

```text
.litespec/intent/features/<name>/              features with their requirements
.litespec/behavior/modules/<module>/...        module, aggregates and use cases
src/<Layer>/<Module>/<Aggregate>/<UseCase>     code
```

A reader finds a use case by module, then aggregate, then use case. Intent holds no module folder: each feature stands in `intent/features/` and names the problems it answers in `solves`, while the Behavior records that fulfill its requirements name the module.

The module is also the seam where a monolith would later split into services. Anything cross-cutting, such as accessibility or security, sits at product level instead of inside a module that borrows it.

A folder under `behavior/modules/` that names no declared module gets a blocking finding, so a stray folder cannot pose as an area.

A record under a module is linked from another module only along a declared module dependency or through an integration event, as `litespec/core/model#connect-across-modules-only-through-declared-dependencies` states. A product-level record may be linked from one module or many.

### Spec to Design Mapping

The specification names only what carries business meaning. Child entities and value objects are design choices inside an aggregate, so they appear in code and the glossary, never as records.

Each record maps to one design pattern, and the stack pages state the code form of each pattern:

| Spec record | Design pattern |
|:---|:---|
| module | one module in the bounded context |
| aggregate, entity | one aggregate, changed through its root |
| aggregate, store | one state holder with a named owner and no guarded invariant |
| aggregate `states` | the lifecycle of the aggregate |
| (none) | child entity, changed only through the root |
| (none) | value object, compared by value |
| rule, invariant | an invariant checked on every change |
| rule, precondition | a guard the behavior passes before it runs |
| rule, calculation | a pure computation on the aggregate or its values |
| rule, precondition spanning aggregates | a synchronous read of the guarding aggregate |
| rule, atomicity | one atomic write group across the aggregates it binds |
| rule, reservation | a held claim with its release |
| rule, frozen-value | a frozen commercial value with its history |
| domain event | a domain event raised through the root |
| integration event | a published fact in CloudEvents form |
| use case, command | one handler behind the use case |
| use case, query | one read behind the use case |
| reaction | one reaction answering its events |
| job, scheduled | one job running on its schedule |
| job, one-off | one job running once |
| read model | one module's copy of another module's published facts, with one projection per source event |
| notification | one rendered message to its audience |
| setting | one tunable value with its range and setter |
| result | one ending per result, each refusal with its code |
| scenario | an executable example proving the scenario |

A precondition that spans aggregates reads the guarding aggregate synchronously: the use case names it in `reads`, with `consistency: atomic` and the guarding rule in `boundBy`. Use a stored constraint only when the guard is a single stored value, and a reaction or job only when eventual consistency is acceptable.

### Quotas and Rate Limits

A quota a customer's plan sets is a rule: it bounds what the customer may do, refuses with a refusal code, and differs by plan.

A technical rate limit protects the platform, not the plan, so it is declared in the extensions rather than as a rule.

## Standards

### Shape Each Module Page

**ID:** `litespec/core/behavior#shape-each-module-page`

**Strength:** required

**Applies to:** artifact: record:module

**Statement:** A module MUST state its `summary` and its `dependsOn` in fields.

**Rationale:** The module names one product area of the bounded context, so Intent, Behavior, and code share one name for one area. `summary` states what the area covers, and `dependsOn` names the modules it builds on, so a reader learns where each area sits and finds its records under one folder. Cross-cutting work sits at product level, never in a module that borrows it. The module is declared in Behavior, beside the aggregates it groups, because it is a code boundary. Proposed by [decision/0051-declare-modules-only-in-behavior](decision/0051-declare-modules-only-in-behavior).

**Example:** The spending module covers expenses, receipts, and the policy, with an empty `dependsOn` where it stands alone.

**Checked by:** `litespec model check` reports `record-invalid` for a module record file missing a required field.

### Keep Design Choices Out of the Records

**ID:** `litespec/core/behavior#keep-design-choices-out-of-the-records`

**Strength:** required

**Applies to:** artifact: aggregate, value-object, record:term

**Statement:** Child entities and value objects MUST appear only in code and the glossary, never as records.

**Rationale:** The design inside an aggregate changes without changing its business meaning, so a record for every child entity and value object would add pages without adding decisions a reviewer judges. Keeping those choices in code and the glossary puts them where builders and readers meet them, and leaves records for what carries business meaning.

**Example:** Order lines live as a child of the order aggregate, and money as a value with its glossary entry; neither is a record.

**Reviewed for:** Does any record page declare a child entity or value object as a record type? Check the record types against the aggregate pages.

## Conventions

### Illustrate Pages with Examples from the Cast

**ID:** `litespec/core/behavior#illustrate-pages-with-examples-from-the-cast`

**Strength:** default

**Applies to:** artifact: record:use-case, record:module, record:aggregate

**Default:** Each module, aggregate, and use-case explanation file carries an Example section drawn from the example cast, removed where no case adds to the records.

**Replacement:** A project names the records that carry an Example section in its documentation.

**Rationale:** Fields state rules, states, and mappings, but none says when the behavior happens or who is affected while it runs. An occasion drawn from the one shared cast lets a reviewer judge the behavior against the world it serves, and lets readers carry the same people and figures from page to page.

**Reviewed for:** Does every module, aggregate, and use-case explanation file carry an Example section from the cast where a case would add to the records? Check the Example sections against the record files, or against the recorded project convention.

### Group Module Rules by Theme

**ID:** `litespec/core/behavior#group-module-rules-by-theme`

**Strength:** default

**Applies to:** artifact: record:rule-set

**Default:** Each rule-set record file holds the module rules of one theme, named in its `summary`.

**Replacement:** A project names its rule-page grouping in its documentation.

**Rationale:** Module rules share no aggregate to gather them, so an ungrouped rule set becomes a list of unrelated checks. One theme per rule set, stated in its `summary`, keeps each rule set reviewable as one decision about what holds across the domain.

**Reviewed for:** Does every rule set state its theme in `summary`? Check the `summary` of each rule-set record file, or the recorded project convention.

## Reference Example

This informative example demonstrates `litespec/core/use-cases#give-each-use-case-a-command-or-query-kind`, `litespec/core/use-cases#connect-each-use-case-to-intent`, `litespec/core/use-cases#grant-each-use-case-scoped-access`, `litespec/core/use-cases#name-the-entry-points-that-reach-each-use-case`, `litespec/core/use-cases#describe-every-use-case-input-with-origin-and-classification`, `litespec/core/use-cases#bound-every-caller-supplied-input-value`, `litespec/core/use-cases#declare-consistency-on-multi-aggregate-commands`, `litespec/core/use-cases#declare-every-effect-with-one-verb-and-its-timing`, `litespec/core/use-cases#name-the-idempotency-of-every-command`, `litespec/core/use-cases#state-the-audit-position-of-every-command`, `litespec/core/use-cases#end-every-use-case-in-named-results`, `litespec/core/use-cases#order-every-refusal-list-by-precedence`, `litespec/core/use-cases#name-the-result-each-scenario-ends-in`, `litespec/core/use-cases#prove-every-written-result-with-a-scenario`, `litespec/core/use-cases#write-every-scenario-in-given-when-then`, `litespec/core/use-cases#exercise-rules-from-scenarios`, `litespec/core/use-cases#share-scenario-setup-in-background`, `litespec/core/use-cases#record-attempts-behind-recorded-refusals`, `litespec/core/use-cases#list-recorded-outcomes-inside-success-codes`, `litespec/core/use-cases#retire-refusal-codes-with-their-replacements`, `litespec/core/access#declare-access-on-every-use-case`, `litespec/core/access#authenticate-every-caller-through-a-declared-auth-kind`, `litespec/core/access#authorize-every-use-case-through-a-declared-grant`, `litespec/core/access#scope-every-grant-to-its-tenant-reach`, `litespec/core/edges#describe-every-integration-event-for-its-direction`, `litespec/core/edges#publish-every-outbound-integration-event-from-the-event-it-translates`, `litespec/core/edges#give-each-entry-point-a-kind`, `litespec/core/edges#give-every-screen-an-address-or-a-host`, and `litespec/core/edges#match-a-screens-offers-to-its-calls`.

**Hold queue.** A reader who finds the copy lent places a hold. The place-hold command serves the hold flow step and fulfills the pickup-window requirement. It declares the place-hold permission with own tenant scope in `access`, and the library browser entry point that reaches it carries session auth. Its caller-supplied pickup code declares its type, with the code-shape rule bounding it. The write commits atomically over the hold aggregate, and the hold-placed integration event, declared with its typed payload and trust, is emitted after commit. The derived refusals come first, then the written refusals in precedence order, ahead of the hold-placed result. Each scenario states its given, when, and then from the shared background, names the rules it exercises, and ends in a named result, so every written result meets its scenario. The command audits at required level, with idempotency keyed on the request.

**Self-checkout scan.** A kiosk records each scan attempt. The record-self-checkout-scan command arrives through the device entry point with a system grant. It writes each attempt on its own, and keys idempotency on `{nonce}` scoped to the login. Its recorded refusals for unknown, retired, and already-lent barcodes store each attempt with its reason, while malformed scans store nothing. Its checked-out result lists the `borrow` and `renew` codes. A retired barcode refusal names its replacement and is never reused. The kiosk screen carries its address and offers the scan use case.
