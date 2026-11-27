# Behavior Records

**Stack:** any
**Concern:** records

This page states how Behavior records describe the system that keeps the Intent promises. It is for agents who write Behavior from Intent and for reviewers who check each record. A missing Behavior detail leaves each implementer guessing, and two guesses about the same promise diverge.

## Concepts

### The Behavior Pages

Five pages carry the Behavior records, and each record lives on one of them. This page carries the overview, the mapping from records to design, and the rules that span pages.

| Page | Holds |
|:---|:---|
| [Domain Model](domain-model.md) | Modules, glossary, aggregates, rules, and domain events. |
| [Use Cases](use-cases.md) | Commands, queries, results, scenarios, and settings. |
| [Automation](automation.md) | Reactions, jobs, module events, and read models. |
| [Edges and Screens](edges.md) | Integration events, notifications, entry points, screens, and external systems. |
| [Privacy and Retention](privacy.md) | Data classes with their erasure positions and retention. |

### Modules as Product Areas

A module names one product area of the bounded context. It is declared in Behavior, at `.litespec/behavior/modules/<module>/module.yaml`, beside the aggregates it groups. Intent carries the product reasons for the area, and Behavior carries the code boundary with its dependencies.

One name runs through every layer:

```text
.litespec/intent/problems/<problem>/features/<name>/   features with their requirements
.litespec/behavior/modules/<module>/...                 module, aggregates and use cases
src/<Layer>/<Module>/<Aggregates>/<UseCase>              code
```

A reader finds a use case by module, then aggregate, then use case. Intent carries no module folder. Each feature sits in the `features/` folder of the one problem it answers, while the Behavior records fulfilling its requirements name the module.

The module is also the seam where a monolith later splits into services. Cross-cutting work, such as accessibility or security, sits at product level instead of inside a module that borrows it.

A folder under `behavior/modules/` with a name outside the declared modules gets a blocking finding, so a stray folder looks like an area under review. A record under a module links from another module along a declared module dependency, through a module event, or through an integration event, as `litespec/core/model#connect-across-modules-only-through-declared-dependencies` states. A product-level record links from one module or many.

### Spec to Design Mapping

The specification names what carries business meaning. Child entities and value objects are design choices inside an aggregate, so they live in code and the glossary, outside the records. A record for each of them triples the tree without adding one decision a reviewer judges.

Each record maps to one design pattern, and the stack pages state the code form of each pattern:

| Spec record | Design pattern |
|:---|:---|
| module | one module in the bounded context |
| aggregate, entity | one aggregate, changed through its root |
| aggregate `states` | the lifecycle of the aggregate |
| (none) | child entity, changed only through the root |
| (none) | value object, compared by value |
| rule, invariant | an invariant checked on every change |
| rule, precondition | a guard the behavior passes before it runs |
| rule, calculation | a pure computation on the aggregate or its values |
| rule, precondition spanning aggregates | a read of the guarding aggregate named in `reads`, with `consistency: { mode: atomic, boundBy: <rule>, guards: [<aggregate>] }`; a stored constraint only where the guard is one stored value, and a reaction or job only where eventual consistency fits |
| rule, atomicity | one atomic write group across the aggregates it governs |
| rule, reservation | a held claim with its release |
| rule, frozen-value | a commercial value with its history |
| domain event | a domain event raised through the root |
| integration event | a published fact in CloudEvents form |
| module event | a fact one module publishes for the other modules of the same product, naming the fields of the domain event it carries |
| use case, command | one handler behind the use case |
| use case, query | one read behind the use case |
| reaction | one reaction answering its events |
| job, scheduled | one job running on its schedule |
| job, one-off | one job running once |
| read model | one module copy of another module published facts, with one projection per source event |
| notification | one rendered message to its audience |
| setting | one operator configuration value with its range and setter |
| result | one ending per result, each refusal with its code |
| scenario | an executable example proving the scenario |

### Quotas and Rate Limits

A quota from a customer plan is a rule. It bounds what the customer does, refuses with a refusal, and differs by plan. A technical rate limit protects the platform, not the plan, so it is declared in the extensions instead of as a rule.

### Examples from the Cast

Fields state rules, states, and mappings, while an occasion drawn from the one shared cast lets a reviewer judge the behavior against the world it serves. Each module, aggregate, and use-case explanation file carries an Example section drawn from the cast. The section stays out where no case adds to the records. A project naming other records for the Example section states that shape in its own documentation.

## Standards

### Shape Each Module Page

**ID:** `litespec/core/behavior#shape-each-module-page`

**Strength:** required

**Applies to:** artifact: record:module

**Statement:** A module MUST state its `summary` and its `dependsOn` in fields.

**Rationale:** The module names one product area, so Intent, Behavior, and code share one name for one area. The `summary` states what the area covers in one place, and `dependsOn` names the modules it builds on. Readers learn where each area sits once and find its records under one folder. Settled by [decision/0051-declare-modules-only-in-behavior](decision/0051-declare-modules-only-in-behavior).

**Example:** The spending module covers expenses, receipts, and the policy, with an empty `dependsOn` where it stands alone.

**Checked by:** `litespec model check` reports `record-invalid` for a module record file missing a required field.

## Conventions

None.

## Reference Example

This informative example demonstrates `litespec/core/behavior#shape-each-module-page`, `litespec/core/use-cases#give-each-use-case-a-command-or-query-kind`, `litespec/core/access#declare-access-on-every-use-case`, `litespec/core/edges#describe-every-integration-event-the-product-sends-or-receives`, and `litespec/core/edges#give-every-screen-a-route-or-a-host`. It shows how one module page gathers the behavior its area promises.

**Hold queue.** A reader who finds the copy lent places a hold. The place-hold command serves the hold flow step and fulfills the pickup-window requirement. It declares the place-hold permission with tenant scope in `access`, and the library browser entry point reaching it carries session authentication.

**Self-checkout scan.** A kiosk records each scan attempt. The record-self-checkout-scan command arrives through the device entry point with a system grant. The hold-placed integration event, declared with its typed payload and trust, goes out after commit. The kiosk screen carries its route and offers the scan use case.
