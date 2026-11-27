# Domain Layer

**Stack:** any
**Concern:** domain

This page states how business rules live in the Domain layer: aggregates guarded by their roots, one act per use case, and values that carry their own checks. It is for developers who write or review Domain code. A rule enforced outside its aggregate drifts the first time a second caller skips the check.

## Concepts

### Aggregates and Their Roots

An aggregate is one unit of business state that stays consistent and changes in one step. The aggregate root is the single type through which outside code changes the aggregate: it guards each change, holds the lifecycle state, and records domain events. Child entities live inside the boundary the root guards, and surface their collections as read-only views.

One aggregate carries another aggregate only by typed identifier, so each aggregate loads on its own. Domain code builds on the language standard library and project-owned Domain types alone, with persistence metadata and dispatch plumbing living in Infrastructure. Domain takes its names from the project glossary, so code and records use the same words for the same actions.

One shared root contract gives every aggregate the same identity and event mechanics: typed identity, pending events, recording, and clearing, plus the version the store compares without Domain reading or setting it. A timestamp, audit, tenant, lifecycle, or other persistence behavior shared by some aggregates alone stays out of that contract.

### Lifecycles as State Types

Domain models the lifecycle as state types: one state property on the root, backed by one closed hierarchy with one type per state. Each state is a named type carrying the facts that exist in that state, and the root's methods own the transitions between them.

The hierarchy starts with the first implementation, even with one state today. A one-state hierarchy gives later states and their facts a defined place, so adding a state extends the hierarchy instead of replacing an enumeration, a status string, or a flag.

In the Claimwick product, an expense is `Submitted`, `Approved`, `Refused`, or `Reimbursed`. The approved case carries the approver and the decided time, and the reimbursed case carries the reimbursed time. An enumeration, a status string, or a flag carries no per-state facts, so none of them models a lifecycle.

![Submit from submit-expense creates a Submitted expense, Approve from approve-expense moves it to Approved carrying the approver and the decided time, Refuse from refuse-expense moves it to Refused carrying the reason, and Reimburse from reimburse-expense moves an Approved expense to Reimbursed carrying the reimbursed time.](../assets/diagrams/backend-aggregate-lifecycle.svg)

### Closed Sets of Values

A closed set is a fixed menu of values where the type system knows each case, such as a refund outcome or an expense state. Domain models each closed set as one hierarchy with one type per case, and a case that carries data holds it on the type, so no parallel nullable field grows beside the set.

Each closed hierarchy gives each case a named type with its own arm in a switch, and a case with no arm reaches the switch fallback at run time. A label arriving from outside converts to its case through a named lookup before it reaches a Domain method, and an unrecognized label fails validation instead of landing on a default case.

Each outer layer mirrors the same set in its own shape: a result union in Application, a transport model in WebApi, and a stable string discriminator in storage. The Domain hierarchy carries no serialization metadata; Infrastructure owns member names, constructors, converters, and polymorphic registration, with each stored concrete type registered once.

### Acts, Marks, and Domain Events

Each use case declares one business action, and the aggregate performs it. That action is called an act: submitting an expense, approving it, reimbursing it. The act lives in a root method named for the business action, and the method carries a mark pointing to the use case it serves.

A mark is the link from code to record: it says which record the code exists for, so a reader moves from the use case to the method. A factory creating an aggregate carries the business action in its name, accepts typed identifiers, rejects empty identity and violated creation rules, selects the initial state, and records the events its use case lists. A root built with its children composes them inside the same factory call from typed values the caller supplies.

When the act runs, it raises domain events: immutable facts about what happened, such as `domain-event/expense.approved`. A domain event carries the business data its reactions need and nothing else: typed identifiers and the values reactions consume, with no aggregate, session, service, mutable collection, or thrown error. Envelope identity for the published integration event lives on that event, which points to the domain event in its `translates` field. Each raised event carries each field the module and integration events publish from it.

A business-named method checks the current state and protects its invariants, replacing state and recording the transition event when it moves the lifecycle. Checks run before replacement, and replacement runs before recording. When the act rejects a business rule, it ends in a refusal: a dedicated outcome naming the failed rule and carrying a stable code that travels to the caller, one refusal per rule, with the aggregate root leading the name. Each event reads as a completed fact in the past tense of its act's verb.

### Values, Services, and Child Entities

Not every business concept is an aggregate. A value object is an immutable value that compares by its complete contents: a post title, a money amount, a set of tags. It validates each value at creation, so an invalid value has no form the code holds. A collection value compares element by element with a matching hash, creation copies incoming mutable collections, and members return read-only views.

Money gets one shared value object carrying an exact decimal amount and an ISO 4217 currency code, serialized as `{ "amount": "12.50", "currency": "EUR" }`. Binary floating point represents most decimal fractions only approximately, so totals drift by fractions of a cent; exact decimal types keep equality and arithmetic exact. A measured latency in a diagnostic log is no Domain quantity and sits outside that shape.

Some rules span concepts with no natural owning aggregate, such as pricing an order from its lines. A domain service holds that calculation: a stateless type that accepts and returns Domain values and calls no persistence, messaging, logging, clock, authorization, or provider. The root calls the service inside its own method, or Application passes the service's plain result into a root method as an input. A result carrying part of an aggregate's state crosses no such boundary: the aggregate's parts are built inside the root.

A child entity and the values holding only its data sit in the part folder named for its data group, while the part's events and refusals stay in the aggregate's `Events/` and `Exceptions/` folders. A part serves no record of its own: it belongs to its aggregate, whose record states the states, transitions, and data the part holds. The folder shows which aggregate a part belongs to, so the builder derives the link from it. One shared declaration in the product `Domain/` folder serves every module for the root, event, identity, and refusal contracts. One identifier reads as the same value across Domain, persistence, transport, and generated clients.

### The Project Glossary

The project glossary is the record that defines each business term: its meaning, its labels per language, its rejected words, and its broader and narrower links. Domain takes its names from the glossary, so code and records use the same words. Records cite the terms they use, following the term rules in the [Domain Model](domain-model.md).

## Standards

### Treat Aggregates as Consistency Boundaries

**ID:** `litespec/core/backend-domain#treat-aggregates-as-consistency-boundaries`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** An aggregate root MUST be the only public mutation entry point for every invariant its transaction protects.

**Rationale:** Outside code that sets properties or calls child mutations directly changes data around the root's checks. With the root as the single public mutation entry point, each change passes the invariant its transaction protects. A command with `consistency: atomic` that changes several aggregates in one transaction cites the rule in `boundBy` and the decision in the rule's `basedOn`.

**Reviewed for:** Can any invariant be changed through a public mutation outside its aggregate root? Look at the public mutation entry points of each aggregate.

### Reach an Aggregate Only Through Its Root

**ID:** `litespec/core/backend-domain#reach-an-aggregate-only-through-its-root`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Code outside an aggregate MUST reach the aggregate only through its root's named factories and methods, passing plain inputs.

**Rationale:** The root alone decides how the aggregate's state changes. A caller building entities or value objects of the aggregate, or calling a service that produces part of the aggregate's state, splits the behavior across layers: the caller chooses which rule applies and in which order, and a later caller skips a step. Passing plain inputs, such as typed identifiers, values, and input records, keeps that knowledge inside the root. The related rules on this page cover typed-identifier references under `litespec/core/backend-domain#reference-other-aggregates-by-id` and part placement under `litespec/core/backend-domain#place-every-part-type-in-its-aggregates-folder`. Settled by [decision/0104-reach-each-aggregate-only-through-its-root](decision/0104-reach-each-aggregate-only-through-its-root).

**Example:** In the Claimwick product, the submit-expense handler calls `Expense.Submit` with the expense and employee identifiers, the lines, and the submitted time. The root computes the cost-center allocation inside its own method and records the submitted event. No handler builds an allocation line, and no service produces part of the expense.

**Reviewed for:** Does every caller reach the aggregate only through its root? Look at the callers of each aggregate beside its root's factories and methods.

### Reference Other Aggregates by ID

**ID:** `litespec/core/backend-domain#reference-other-aggregates-by-id`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** An aggregate MUST store another aggregate's typed identifier rather than an object reference.

**Rationale:** An object reference between aggregates loads one aggregate's data through another, which couples their consistency boundaries and their storage. A typed identifier keeps each aggregate loadable on its own. Application loads each side through its own domain repository when a command coordinates several aggregates, with `consistency: atomic` citing the rule in `boundBy` and the decision in the rule's `basedOn`.

**Reviewed for:** Does any aggregate field hold another aggregate as an object reference instead of a typed identifier? Look at the fields of each aggregate.

### Change Each Aggregate Through Its Marked Act

**ID:** `litespec/core/backend-domain#change-each-aggregate-through-its-marked-act`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** A command handler changing an aggregate MUST change it by calling a root method marked with the handler's use case.

**Rationale:** The use case declares an act, and the aggregate performs it. The mark on the root method says which act the method performs. A handler that changes its aggregate only through methods marked for other use cases performs an act its record describes nowhere, and a reader finds no path from the use case to the method. The use-case verb, the method name, and the raised event's tense agree with each other. This provision states the marked form of `litespec/core/backend-application#keep-command-handlers-to-load-call-and-stage`.

A command that only runs other commands and lists nothing in `writes` performs no act on a root, so it needs no marked call.

**Example:** The handler for `use-case/spending.reimburse-expense` calls `Reimburse` on the expense root, and `Reimburse` carries the mark `use-case/spending.reimburse-expense`.

**Checked by:** `litespec model check` reports `use-case-act-missing` (level S) for a command handler changing an aggregate that calls no root method marked with its use case. The check runs in the CLI only, with no analyzer rule: the handler and the root method sit in different projects, and one compilation sees one project.

### Give Each Use Case Its Own Act

**ID:** `litespec/core/backend-domain#give-each-use-case-its-own-act`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Each aggregate root method that changes state MUST perform the act of exactly one use case.

**Rationale:** Each use case is its own path, with its own caller, setup, refusals, events, and scenarios. A method shared by two use cases couples them: a change made for one silently changes the other, and neither scenario set describes the method. One method per act keeps each path readable on its own; logic the acts share moves into a private method of the root that each act calls. A method that only reads or computes performs no act and carries no mark.

**Example:** In the Claimwick product, a manager approving an expense and a delegate approving it for an absent manager both record an approval. The expense root offers `Approve` marked `use-case/spending.approve-expense` and `ApproveOnBehalf` marked `use-case/spending.approve-expense-on-behalf`: each raises its own event, and both call a private `RecordApproval`.

**Checked by:** `litespec model check` reports `mark-count-invalid` (level S) for a root method carrying more than one use-case mark, and `mark-unknown` (level S) for a mark on a method that changes no state.

### Raise the Declared Events from the Marked Act

**ID:** `litespec/core/backend-domain#raise-the-declared-events-from-the-marked-act`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** The root methods marked with a use case MUST together raise exactly the events that use case lists in `raises`.

**Rationale:** The use case record states the facts its act leaves behind, and reactions subscribe to those facts. A marked method that raises an event the record lists nowhere starts a consequence nobody reviewed, and a listed event that no marked method raises is a promise the code keeps nowhere. Each raised event carries each field its module and integration events publish, because each module and integration event is built from its domain event alone under `litespec/core/backend-application#stage-declared-module-and-integration-events-in-the-commit`.

**Example:** `use-case/spending.submit-expense` lists `domain-event/expense.submitted` in `raises`, and the `Submit` method marked with it raises `ExpenseSubmittedDomainEvent` and nothing else.

**Checked by:** `litespec model check` reports `act-event-undeclared` (level S) for a marked method raising an event its use case does not list, and `act-event-unimplemented` (level S) for a listed event no marked method raises.

### Model Money with Exact Decimal Values

**ID:** `litespec/core/backend-domain#model-money-with-exact-decimal-values`

**Strength:** required

**Applies to:** artifact: value-object

**Statement:** A monetary amount MUST use an exact decimal type inside one shared `Money` value object carrying an ISO 4217 currency code and serializing as `{ "amount": "12.50", "currency": "EUR" }`.

**Rationale:** Money needs exact arithmetic and an explicit currency on each value, and one shared `Money` object gives both. The owning module record defines supported currencies, scale, rounding, sign rules, and cross-currency arithmetic, and Infrastructure maps that precision explicitly. A use case fixed to one currency accepts an amount alone and states that currency in its record.

**Example:**

```json
{ "amount": "12.50", "currency": "EUR" }
```

The amount serializes as a string, so no reader parses it as binary floating point.

**Reviewed for:** Is any monetary amount held outside the shared `Money` value, missing its currency, or serialized in another shape? Look at each monetary amount in Domain.

### Keep Binary Floating Point Out of Exact Quantities

**ID:** `litespec/core/backend-domain#keep-binary-floating-point-out-of-exact-quantities`

**Strength:** required

**Applies to:** artifact: value-object

**Statement:** A Domain value MUST NOT use binary floating point for a monetary amount, a rate applied to one, or a quantity compared for exact equality.

**Rationale:** Binary floating point represents most decimal fractions only approximately, so two amounts a person reads as equal compare as different, and totals drift by fractions of a cent. Exact decimal types keep equality and arithmetic exact.

**Example:** A tax rate multiplied into a `Money` amount uses an exact decimal type. A measured latency in a diagnostic log is no Domain quantity and sits outside this rule.

**Reviewed for:** Is any monetary amount, rate applied to one, or exactly-compared quantity held in a binary floating-point type? Look at the field, parameter, and return types in Domain.

### Use Stateless Domain Services for Ownerless Rules

**ID:** `litespec/core/backend-domain#use-stateless-domain-services-for-ownerless-rules`

**Strength:** required

**Applies to:** artifact: value-object

**Statement:** A domain service MUST be stateless, hold only a calculation that spans aggregates with no owning aggregate, accept and return Domain values, and perform no persistence, messaging, logging, clock, authorization, or provider call.

**Rationale:** Some calculations span concepts with no natural aggregate owner. Forcing one into an aggregate couples that aggregate to another's data. A stateless service holds the calculation instead, accepting and returning Domain values with no outside call. A calculation one aggregate owns stays in that aggregate's root. The root calls the service inside its own method, or Application passes the service's plain result into a root method as an input.

A domain service takes no port and holds no state, so its tests are Domain tests: they sit beside the aggregate tests in the unit tests mirroring the module tree, with no harness, no substitute, and no fixture. Settled by [decision/0104-reach-each-aggregate-only-through-its-root](decision/0104-reach-each-aggregate-only-through-its-root).

**Example:** An order-pricing service takes order lines and a currency and returns their total as `Money`, with no domain repository, clock, logger, or provider client. The order root calls it inside `ConfirmPrice`, or Application supplies the lines and passes the returned total into `ConfirmPrice` as an input. The service returns no order line and no other part of the order.

**Reviewed for:** Is any domain service stateful, taking or returning non-Domain values, or calling persistence, messaging, logging, clock, authorization, or a provider? Look at each domain service and its calls.

### Place Every Part Type in Its Aggregate's Folder

**ID:** `litespec/core/backend-domain#place-every-part-type-in-its-aggregates-folder`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A Domain part type, meaning an identifier, a state type, a closed set, a child entity, a value object, or a repository interface, MUST sit in the `Domain/` folder of the aggregate it belongs to, or in the product `Domain/` folder, and a row or projection a port returns is no part.

**Rationale:** A part serves no record of its own: it belongs to its aggregate, whose record states the states, transitions, and data the part holds. A mark on each part writes that fact a second time, and the two copies drift. The folder shows which aggregate a part belongs to, so the builder derives the link from it.

A type in a folder that holds no aggregate root, and that is not the product `Domain/` folder, is the one place the derivation fails. A domain service that holds one rule calculation is no part: it carries its `rule/` mark. A row or projection a port returns is no part either: it is the port's answer, shaped for a reader, and it sits with its port.

**Example:** In the Claimwick product, `IExpenseRepository` sits beside the `Expense` root in `Spending/Domain/Expenses/`, `ExpenseState` sits in its `Parts/` grouping, and both carry no mark; the builder lists them as parts of `aggregate/spending.expense`. `Money` sits in the product `Domain/` folder and belongs to no aggregate.

**Checked by:** `litespec model check` reports `part-parent-missing` (level S) for an unmarked Domain type whose folder holds no aggregate root and is not the product `Domain/` folder, and the model output lists each part under its aggregate.

### Pass Nondeterministic Values into Domain

**ID:** `litespec/core/backend-domain#pass-nondeterministic-values-into-domain`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Domain behavior MUST receive time, randomness, and new identifiers as arguments rather than read them, and read no clock.

**Rationale:** Domain behavior that reads a clock or generates identifiers returns different results for the same call, which makes tests nondeterministic: the same test passes and fails with no code change. Receiving time, randomness, and new identifiers as arguments keeps each Domain call a pure function of its inputs: a test supplies explicit values and stays deterministic. Persistence rejects a duplicate key and the write fails, which follows the same path as any other write conflict, with the store arbitrating the race under `litespec/core/backend-persistence#enforce-uniqueness-in-the-store`.

Application obtains each value through an owned port and passes it in. A method that needs more than one value receives a clock or a generator through its arguments, and version 7 identifiers come from the caller-supplied generator.

**Reviewed for:** Is any clock read, random source, or new identifier reached from inside Domain instead of passed in as an argument? Look at the calls Domain code makes.

### Document Every Public Domain Contract

**ID:** `litespec/core/backend-domain#document-every-public-domain-contract`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every public Domain type and member MUST carry documentation stating the business constraint, result, or refusal it represents.

**Rationale:** A reader meeting a public Domain member needs the business rule behind it, not just its signature. Documentation states the business constraint, result, or refusal the member represents. A mutation method names its source states, resulting state, protected invariant, and recorded event. A summary that restates the member name adds nothing.

**Reviewed for:** Is any public Domain type or member missing documentation of its business constraint, result, or refusal, or documented with a restated name? Look at the documentation on each public member.

## Conventions

### Organize a Module by Aggregate and Concept

**ID:** `litespec/core/backend-domain#organize-a-module-by-aggregate-and-concept`

**Strength:** default

**Applies to:** artifact: any

**Default:** Give each aggregate a `Domain/` folder named with its plural root name, holding the root's main file, the repository interface as the aggregate's only store interface, and the root's partial files for a concern no single act owns at its top with the groupings `{Act}/`, `Parts/`, `Facts/`, `Events/`, and `Exceptions/`.

**Replacement:** A project names its own module layout in its documentation.

**Rationale:** Each aggregate `Domain/` folder holds everything one aggregate owns: its states, events, refusals, and concepts. A reader finds the full aggregate in one place instead of hunting shared folders. Every aggregate keeps its own folder, whatever the module's number of aggregates. Each root action groups into its own `{Act}/` folder with its partial file, its inputs, its private rules, and the steps only it calls; entities, value objects, state types, and the stateless rules several acts share sit in `Parts/`; values passed in from other modules or the country pack sit in `Facts/`; domain events sit in `Events/` and refusals in `Exceptions/`.

A part is a data group the aggregate record declares with dotted field names, such as `parcel.*`. The part's child entity and the values only that child holds sit in `Parts/`, while the part's events and refusals stay in the aggregate's `Events/` and `Exceptions/` folders.

**Example:**

```text
{Product}/
  Domain/                         the shared kernel: the root, event, identity, and refusal contracts
  Posts/                          one aggregate: every layer holds the plural aggregate folder
    Domain/
      Posts/
        Post                      the root's main file
        PostRepository            the repository interface
        CreateDraft/              the act's partial file, inputs, and private rules
        Publish/
        Parts/
          PostId
          PostTitle
          PostState
          PostDraftState
          PostPublishedState
          PostArchivedState
        Events/
          PostCreatedDomainEvent
          PostPublishedDomainEvent
        Exceptions/
          PostIdentityRequired
          PostAlreadyPublished
  Shipping/                       one aggregate whose name differs from the module
    Domain/
      Shipments/
        Shipment
        ShipmentRepository
        RecordOutcome/
        Parts/
          ShipmentId
          ShipmentOutcome
          ShipmentDeliveredOutcome
          ShipmentReturnedOutcome
          ShipmentLostOutcome
          ShipmentState
          ShipmentPendingState
          ShipmentDispatchedState
          ShipmentDeliveredState
        Events/
          ShipmentOutcomeRecordedDomainEvent
        Exceptions/
  Customers/                      more than one aggregate: one plural folder per aggregate
    Domain/
      CustomerAccounts/
        CustomerAccount
        CustomerAccountRepository
        Restrict/
        Parts/
          CustomerAccountId
          CustomerAccountState
          CustomerAccountClaimedState
        Events/
          CustomerAccountRestrictedDomainEvent
        Exceptions/
      Consents/
        Consent
        ConsentRepository
        Grant/
        Parts/
          ConsentId
          ConsentState
          ConsentGrantedState
        Events/
          ConsentGrantedDomainEvent
        Exceptions/
```

Each aggregate's domain repository stays with the aggregate it loads. The module, aggregate, and use case order follows `litespec/core/backend-architecture#organize-code-by-module-aggregate-and-use-case`. Each stack page states the file names and extensions that carry this layout.

**Checked by:** `litespec model check` reports `code-placement-drift` (level S) for a domain event outside its aggregate's `Events` folder, for a state beside its aggregate root, and for a second store interface of one aggregate, and `folder-ungrouped` for a folder past 20 files that holds none of its allowed groupings.
