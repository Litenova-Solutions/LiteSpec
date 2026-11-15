# Domain Layer

**Stack:** any
**Concern:** domain

The Domain layer holds a product's business rules in code that depends on nothing but itself, so the rules are tested on their own.

An aggregate is one unit of business state that stays consistent and changes in one step. This page states how to model aggregates, their lifecycles, closed sets of values, and the domain repositories that load them. It also states the boundary that keeps technical concerns out of business rules.

## Concepts

### Aggregates and Their Roots

In code, the [aggregate root](../reference/glossary.md) is the single type through which outside code changes the aggregate. It guards every change, holds the lifecycle state, and records domain events. Child entities live inside the boundary the root guards and expose no mutable state outside the root.

One aggregate references another by typed identifier rather than by object reference. One domain repository per aggregate loads and stages it; the repository is a contract the Domain declares and Infrastructure implements.

A `store` aggregate is the exception: it has no root in code and reaches code through a port, as `litespec/backend/application#expose-store-state-through-ports` states.

### Lifecycles as State Types

Domain models the lifecycle as state types: one state property on the root, backed by one closed hierarchy with one type per state. Each state is a named type carrying the facts that exist in that state, and the root's methods own the transitions between them.

Model the states from the first implementation, even when the aggregate has only one state today. A one-state hierarchy gives later states and their facts a defined home, so adding a state extends the hierarchy instead of replacing an enumeration, a status string, or a flag.

In the Claimwick product, an expense is `Submitted`, `Approved`, `Refused`, or `Reimbursed`. The approved case carries the approver and the decided time, and the reimbursed case carries the reimbursed time. An enumeration, a status string, or a flag carries no per-state facts, so none of them models a lifecycle.

### Closed Sets of Values

A closed set is a fixed menu of values where the type system knows every case, such as a refund outcome or an expense state. Each case is a named type that a switch can name in its own arm, and a case with no arm reaches the switch fallback at run time.

Domain models each closed set as one type hierarchy with one type per case. A case that carries data holds it on the type, so no parallel nullable field grows beside the set.

Each outer layer mirrors the same set in its own shape: a result union in Application, a transport model in WebApi, and a stable string discriminator in storage. A label arriving from outside converts to its case through a named lookup before it reaches a Domain method, and an unrecognized label fails validation instead of becoming a default case.

### Acts, Marks, and Domain Events

Each use case names one business action, and the aggregate performs it. That action is called an act: submitting an expense, approving it, reimbursing it. The act lives in a root method named for the business action, and the method carries a mark naming the use case it serves. A mark is the link from code to record: it says which record the code exists for, so a reader moves from the use case to the method.

When the act runs, it raises domain events: immutable facts about what happened, such as `domain-event/expense.approved`. A domain event carries the business data its reactions need and nothing else: no aggregate, no session, no service, no mutable collection, no thrown error. Reactions and integration events build on those facts, so each event carries every field the integration events publish from it.

When the act rejects a business rule, it ends in a refusal: a dedicated outcome naming the failed rule and carrying a stable code that travels to the caller. One refusal per rule lets each boundary map the outcome without parsing messages.

### Values, Services, and Repositories

Not every business concept is an aggregate. A value object is an immutable value that compares by its complete contents: a post title, a money amount, a set of tags. It validates every value at creation, so an invalid value never exists. Money gets one shared value object carrying an exact decimal amount and an ISO 4217 currency code, because exact arithmetic and an explicit currency belong on every monetary value.

Some rules span concepts with no natural owning aggregate, such as pricing an order from its lines. A domain service holds that calculation: a stateless type that accepts and returns Domain values and calls no persistence, messaging, logging, clock, authorization, or provider. Application loads the aggregates, calls the service, and passes the result into aggregate behavior.

A domain repository is the contract that loads and stages one aggregate. It serves exactly one aggregate root, speaks only aggregate and Domain types, and offers no composable query, session, or generic CRUD surface. Infrastructure implements it, and the command pipeline commits the staged aggregates.

The smaller types that make up an aggregate, such as its identifier, state types, closed sets, child entities, value objects, and repository interface, are called part types. They sit in the aggregate's `Domain/` folder, and the folder says which aggregate they belong to, so they carry no mark of their own.

### The Project Glossary

The project glossary is the record that defines each business term: its meaning, its labels per language, its rejected words, and its broader and narrower links. Domain takes its names from the glossary, so code and records use the same words. Records cite the terms they use, following `litespec/core/domain-model#define-every-behavior-term-in-the-glossary`.

## Standards

### Keep Domain Independent of Outer Layers

**ID:** `litespec/backend/domain#keep-domain-independent-of-outer-layers`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The Domain code MUST reference only the language standard library and project-owned Domain types.

**Rationale:** Domain that references only the language standard library and its own types compiles and tests with no infrastructure at all. Each banned reference would drag one outer concern into the business model: persistence metadata couples a rule to a store format, and dispatch plumbing couples it to delivery. Infrastructure registers a persisted discriminator instead, so Domain carries no serialization metadata.

**Reviewed for:** Does any Domain type reference code outside the language standard library and project-owned Domain types? Look at the references in the Domain code.

### Use One Ubiquitous Language

**ID:** `litespec/backend/domain#use-one-ubiquitous-language`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A Domain name MUST match a term in the project glossary.

**Rationale:** When code and records use different names for one action, every reader translates between them. `SetStatus` in code hides which business action ran. With one shared name from the project glossary on both sides, the record and the code describe the same action in the same words.

**Example:** The glossary names the business action `Publish`, so the root method is `Publish`, not `SetStatus`.

**Reviewed for:** Does every Domain name match a term in the project glossary? Compare the Domain names with `glossary.yaml`.

### Treat Aggregates as Consistency Boundaries

**ID:** `litespec/backend/domain#treat-aggregates-as-consistency-boundaries`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** An aggregate root MUST be the only public mutation entry point for every invariant its transaction protects.

**Rationale:** If outside code could set properties or call child mutations directly, it would change data around the root's checks. With the root as the only public mutation entry point, every change passes the invariant its transaction protects. A command with `consistency: atomic` changes every aggregate its `writes` names in one transaction, citing the rule in `boundBy` when it changes more than one.

**Reviewed for:** Can any invariant be changed through a public mutation outside its aggregate root? Look at the public mutation entry points of each aggregate.

### Reference Other Aggregates by ID

**ID:** `litespec/backend/domain#reference-other-aggregates-by-id`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** An aggregate MUST store another aggregate's typed identifier rather than an object reference.

**Rationale:** An object reference between aggregates loads one aggregate's data through another, which couples their consistency boundaries and their storage. A typed identifier keeps each aggregate loadable on its own, and Application loads each side through its own domain repository when a command coordinates several aggregates, with `consistency: atomic` citing the rule in `boundBy`.

**Reviewed for:** Does any aggregate field hold another aggregate as an object reference instead of a typed identifier? Look at the fields of each aggregate.

### Take Identity and Events from the Shared Root Contract

**ID:** `litespec/backend/domain#take-identity-and-events-from-the-shared-root-contract`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** An aggregate root MUST take its identity and event mechanics from the project-owned root contract.

**Rationale:** One shared root contract gives every aggregate the same identity and event mechanics, so Infrastructure collects pending events from any aggregate the same way. The contract stays small on purpose: it owns typed identity, pending events, recording, and clearing, plus the aggregate version as persistence metadata that Domain never reads or sets. A timestamp, audit, tenant, lifecycle, or other persistence behavior would couple every aggregate to a concern only some need.

**Reviewed for:** Does any aggregate root take identity or event mechanics from outside the project-owned root contract? Look at each aggregate root declaration.

### Model Every Aggregate Lifecycle with State Types

**ID:** `litespec/backend/domain#model-every-aggregate-lifecycle-with-state-types`

**Strength:** required

**Applies to:** artifact: state-record

**Statement:** An aggregate MUST expose its lifecycle only through one state property backed by one closed hierarchy of per-state types.

**Rationale:** Each lifecycle state can carry different facts: a published post carries its publication time, and a draft has none. An enumeration, a string discriminator, a boolean flag, a parallel nullable field, or a computed state carries no state-specific data. State types hold each state's data on the state itself, and aggregate methods own the transitions between them. A switch over the state types names each case in its own arm where the language requires a fallback, and a case with no arm reaches that fallback at run time.

**Example:** A post defines one abstract post state with draft and published cases, and the published case carries its publication time. An aggregate with one current state still defines the hierarchy, so the second state has a home.

In the Claimwick product, `aggregate/spending.expense` records the `Submitted`, `Approved`, `Refused`, and `Reimbursed` states with their transitions: Submit from `use-case/spending.submit-expense` creates Submitted, Approve from `use-case/spending.approve-expense` moves Submitted to Approved, Refuse from `use-case/spending.refuse-expense` moves Submitted to Refused, and Reimburse from `use-case/spending.reimburse-expense` moves Approved to Reimbursed. The approved case carries the approver and the decided time, and the reimbursed case carries the reimbursed time.

![Submit from submit-expense creates a Submitted expense, Approve from approve-expense moves it to Approved carrying the approver and the decided time, Refuse from refuse-expense moves it to Refused carrying the reason, and Reimburse from reimburse-expense moves an Approved expense to Reimbursed carrying the reimbursed time.](../assets/diagrams/backend-aggregate-lifecycle.svg)

**Reviewed for:** Is any lifecycle exposed outside one state property and one closed hierarchy of per-state types? Look at the lifecycle declarations in each aggregate.

### Model Every Closed Set as a Type Hierarchy

**ID:** `litespec/backend/domain#model-every-closed-set-as-a-type-hierarchy`

**Strength:** required

**Applies to:** artifact: value-object

**Statement:** Domain code MUST model a closed set as one type hierarchy whose cases the type system can enumerate.

**Rationale:** A closed set with case data needs a type per case. An enumeration carries no case data, admits undefined values, and forces parallel nullable fields the moment one case gains data. A closed hierarchy gives each case a named type that a switch can name in its own arm, and a case with no arm reaches the switch fallback at run time. A label arriving from outside converts to its case through a named lookup rather than through a string conversion, and an unrecognized label fails validation rather than landing on a default case.

**Example:** A refund outcome defines pending, succeeded, failed, and reversed cases, and each case carries its own data.

This provision owns the Domain form. Application mirrors the set in its result under `litespec/backend/application#mirror-a-domain-closed-set-in-the-result`, the wire mirrors it as a transport model of the same shape under `litespec/backend/api#mirror-each-closed-set-on-the-wire`, and storage mirrors it as a stable string discriminator under `litespec/backend/persistence#keep-serialization-behavior-out-of-domain`. Label-only sets cross the boundary as plain enumerations under `litespec/backend/api#publish-label-only-sets-as-string-enums`. A transport label converts to its type case before it reaches a Domain method.

**Reviewed for:** Is any closed set escaping one type hierarchy with named label lookups? Look at each closed set and its boundary lookups.

### Create Valid Aggregates Through Named Factories

**ID:** `litespec/backend/domain#create-valid-aggregates-through-named-factories`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** An aggregate root MUST create instances only through a factory named for its business action.

**Rationale:** A constructor carries no business name, so creation through constructors spreads creation rules across call sites. A factory named for its business action accepts typed identifiers, rejects empty identity and violated creation rules, selects the initial state, records the required events, and returns a complete aggregate.

A root created with its children composes them inside the same factory call. The factory builds the children itself from typed values the caller supplies, because a caller holding an already built child could change it after the invariant was checked.

**Example:** In the Claimwick product, `use-case/spending.submit-expense` creates its expense through a factory named for submitting. The factory takes the expense and employee identifiers, rejects an empty identity, selects the submitted state, and records the submitted event.

**Reviewed for:** Can any aggregate root be created through a constructor or a factory that is not named for its business action? Look at the creation paths of each root.

### Express Transitions Through Business Methods

**ID:** `litespec/backend/domain#express-transitions-through-business-methods`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** A public aggregate mutation method MUST check current state and protect its invariants, replacing state and recording its transition event when it moves the lifecycle.

**Rationale:** A business-named method lets the handler call the publish behavior without testing the state first: the method checks the current state and protects its invariants, then replaces state and records the transition event when it moves the lifecycle. A generic `Set`, `Update`, `Process`, or `Handle` name hides which business action ran. A mutation that changes no lifecycle state, such as `ChangeTitle` on a draft, checks state and assigns without recording a transition. Checks run before replacement, and replacement runs before recording: a method that replaces state before checking an invariant leaves a rejected aggregate holding the new state.

**Example:** In the Claimwick product, `use-case/spending.reimburse-expense` ends with `result/reimburse-expense.reimbursed` when the expense is approved, and with `result/reimburse-expense.not-approved` when it is not. The handler calls one business method, and the method checks the state, replaces it, and records `domain-event/expense.reimbursed`.

An unapproved expense keeps its state, and the refusal carries the stable code `spending.not-approved`.

**Reviewed for:** Does any public mutation skip its state check or invariant, or any lifecycle move skip replacement or its transition event? Look at the public mutation methods of each aggregate.

### Change Each Aggregate Through Its Marked Act

**ID:** `litespec/backend/domain#change-each-aggregate-through-its-marked-act`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** A command handler changing an entity aggregate MUST change it by calling a root method marked with the handler's use case.

**Rationale:** The use case names an act, and the aggregate performs it. The mark on the root method says which act the method is. A handler that changes its aggregate only through methods marked for other use cases performs an act its record never named, and a reader has no way to tell from the use case which method to open. The use-case verb, the method name, and the raised event's tense agree under `litespec/backend/domain#raise-the-past-tense-of-a-methods-own-verb`. This provision states the marked form of `litespec/backend/application#keep-command-handlers-to-load-call-and-stage`.

A command that changes only stores, or that only runs other commands and lists nothing in `writes`, performs no act on a root, so it needs no marked call.

**Example:** The handler for `use-case/spending.reimburse-expense` calls `Reimburse` on the expense root, and `Reimburse` carries the mark `use-case/spending.reimburse-expense`.

**Checked by:** `litespec model check` reports `use-case-act-missing` (level S) for a command handler changing an entity aggregate that calls no root method marked with its use case. The check runs in the CLI only, with no analyzer rule: the handler and the root method sit in different projects, and one compilation sees one project.

### Give Each Use Case Its Own Act

**ID:** `litespec/backend/domain#give-each-use-case-its-own-act`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Each aggregate root method that changes state MUST perform the act of exactly one use case.

**Rationale:** Each use case is its own path, with its own caller, setup, refusals, events, and scenarios. A method two use cases share couples them: a change made for one silently changes the other, and neither scenario set describes the method. One method per act keeps each path readable on its own; logic the acts share moves into a private method of the root that each act calls. A method that only reads or computes performs no act and carries no mark.

**Example:** In the Claimwick product, a manager approving an expense and a delegate approving it for an absent manager both record an approval. The expense root offers `Approve` marked `use-case/spending.approve-expense` and `ApproveOnBehalf` marked `use-case/spending.approve-expense-on-behalf`: each raises its own event, and both call a private `RecordApproval`.

**Checked by:** `litespec model check` reports `mark-count-invalid` (level S) for a root method carrying more than one use-case mark, and `mark-unknown` (level S) for a mark on a method that changes no state.

### Raise the Declared Events from the Marked Act

**ID:** `litespec/backend/domain#raise-the-declared-events-from-the-marked-act`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** The root methods marked with a use case MUST together raise exactly the events that use case lists in `raises`.

**Rationale:** The use case record states the facts its act leaves behind, and reactions subscribe to those facts. A marked method that raises an event the record does not list starts a consequence nobody reviewed, and a listed event that no marked method raises is a promise the code does not keep. Each raised event carries every field its integration events publish, because each integration event is built from its domain event alone under `litespec/backend/application#emit-declared-integration-events-after-commit`.

**Example:** `use-case/spending.submit-expense` lists `domain-event/expense.submitted` in `raises`, and the `Submit` method marked with it raises `ExpenseSubmittedEvent` and nothing else.

**Checked by:** `litespec model check` reports `act-event-undeclared` (level S) for a marked method raising an event its use case does not list, and `act-event-unimplemented` (level S) for a listed event no marked method raises.

### Raise the Past Tense of a Method's Own Verb

**ID:** `litespec/backend/domain#raise-the-past-tense-of-a-methods-own-verb`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Each aggregate method that changes state MUST raise a domain event named for the past tense of its verb.

**Rationale:** The command raises nothing; the aggregate root raises its domain event inside its own method. An event named for another verb tells the reader the act finished under a name the code never spoke, and reactions subscribe to a vocabulary the aggregate does not own. The event name takes the past tense of the act's verb, as `litespec/core/domain-model#name-every-event-in-the-past-tense` requires for every event. The name may place the past tense as a whole word followed by a complement that opens with a preposition or particle. A method that sets a state names that state in its event, and a method may raise further events for the consequences of its act, as long as one of its events names the act itself.

**Example:** In the Claimwick product, `Approve` on an expense raises `domain-event/expense.approved` and passes. `Approve` raising `domain-event/expense.signed-off` fails review, because that name belongs to another act.

**Reviewed for:** Does any state-changing aggregate method raise an event named for a different verb than its own? Look at the events each changed aggregate method raises.

### Raise Immutable Facts as Domain Events

**ID:** `litespec/backend/domain#raise-immutable-facts-as-domain-events`

**Strength:** required

**Applies to:** artifact: domain-event

**Statement:** A domain event MUST be immutable and carry business data only.

**Rationale:** A domain event records a completed fact for reactions to consume. The event carries business data only: typed identifiers and the values its reactions need, with no aggregate, session, service, mutable collection, or thrown error, because any of those would couple reactions to the raising aggregate. Envelope identity, such as message identity and causation identity, lives on the published integration event that carries the fact outside the process. That integration event names the domain event in `translates`. The event name states the aggregate and the completed fact, following `litespec/workspace/naming#anchor-aggregate-owned-types-on-the-aggregate-root` and `litespec/workspace/naming#use-architectural-suffixes`, so a reader knows which aggregate raised the fact without opening the file.

**Reviewed for:** Is any domain event mutable, named for the wrong aggregate or fact, or carrying more than business data? Look at each domain event.

### Keep Child Entities Inside the Aggregate Boundary

**ID:** `litespec/backend/domain#keep-child-entities-inside-the-aggregate-boundary`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** A child entity MUST expose no mutable state outside its aggregate root, surfacing its collection as a read-only collection.

**Rationale:** A child that exposed mutable state would let outside code change aggregate data without passing the root's checks. The root creates, finds, and changes children through business methods, and collections surface as read-only collections. A child entity and the values only it holds sit in the part folder its data group names, under `litespec/backend/domain#organize-a-module-by-aggregate-and-concept`. A child with lifecycle states uses the same state types and no enumeration.

**Reviewed for:** Does any child entity expose mutable state outside its root, including through a writable collection? Look at the child entities beside their root, including their collections.

### Identify Each Aggregate with a Typed Version 7 Identifier

**ID:** `litespec/backend/domain#identify-each-aggregate-with-a-typed-version-7-identifier`

**Strength:** required

**Applies to:** artifact: aggregate, value-object

**Statement:** An aggregate identity MUST be a dedicated type wrapping one version 7 UUID, with no conversion, alias, or creation path that bypasses its validation.

**Rationale:** A bare identifier type lets one aggregate's identifier pass where another's is expected: an expense id lands in a parameter typed for an employee id, and the compiler stays silent. A dedicated identity type wrapping one version 7 UUID makes that a compile error. With no conversion, alias, or creation path around its validation, there is no quiet route back to a bare value. The type rejects the empty value, and each factory also rejects its default. New values arrive from the caller: a factory takes its identifier as an argument, and a method that needs several identifiers takes a generator. Domain reads no clock and calls no static source of time or randomness, so every Domain call stays a pure function of its inputs. Version 7 values come from the caller-supplied generator, so Domain observes no business time and decides nothing about clocks. Persistence rejects a duplicate key and the write fails, which follows the same path as any other write conflict, with the violation mapped to the use case conflict refusal under `litespec/backend/persistence#map-a-constraint-violation-to-its-refusal`.

**Example:** A post identifier wraps one version 7 UUID, rejects the empty value, and parses from text through a named method.

**Reviewed for:** Does any aggregate identity escape the dedicated version 7 type, or does any conversion, alias, or creation path bypass its validation? Look at each identity type and its creation paths.

### Use Immutable Value Objects for Domain Concepts

**ID:** `litespec/backend/domain#use-immutable-value-objects-for-domain-concepts`

**Strength:** required

**Applies to:** artifact: value-object

**Statement:** A value object MUST be immutable, compare by its complete value, and validate every value at creation.

**Rationale:** Immutability with comparison by complete value lets two instances with the same data stand in for each other. The value exposes no mutation and no implicit conversion, because a primitive conversion hides validation and a value conversion erases the domain type. Reading uses the wrapped value or its text form, and creation through a named creation path keeps every value under the validation the type declares.

**Example:** A post title accepts only non-blank text up to 200 characters: blank or longer input fails creation, and two titles with the same text compare as equal.

**Reviewed for:** Is any value object mutable, comparing by less than its complete value, or creatable without validation? Look at each value object and its creation paths.

### Compare Collection Values by Their Content

**ID:** `litespec/backend/domain#compare-collection-values-by-their-content`

**Strength:** required

**Applies to:** artifact: value-object

**Statement:** A value object containing a collection MUST implement content equality and a matching hash for its declared ordering rule.

**Rationale:** Default structural equality compares collection references rather than contents, so two value objects holding equal lists compare as different. Content equality with a matching hash compares what the value means. Creation copies incoming mutable collections, and members return read-only views, so later changes by the caller leave the value unchanged. The module terms state whether order is part of the value.

**Example:** A post-tags value holding an ordered list compares element by element with a matching hash. Where order is not part of the value, creation normalizes to the documented comparison order before storing values.

**Reviewed for:** Is any collection value compared by reference instead of content, or missing a matching hash for its declared ordering rule? Look at each collection value beside the module terms.

### Model Money with Exact Decimal Values

**ID:** `litespec/backend/domain#model-money-with-exact-decimal-values`

**Strength:** required

**Applies to:** artifact: value-object

**Statement:** A monetary amount MUST use an exact decimal type inside one shared `Money` value object carrying an ISO 4217 currency code and serializing as `{ "amount": "12.50", "currency": "EUR" }`.

**Rationale:** Money needs exact arithmetic and an explicit currency on every value, and one shared `Money` object gives both. The owning module record defines supported currencies, scale, rounding, sign rules, and cross-currency arithmetic, and Infrastructure maps that precision explicitly. A use case fixed to one currency accepts an amount alone and states that currency in its record.

**Example:**

```json
{ "amount": "12.50", "currency": "EUR" }
```

The amount serializes as a string, so no reader parses it as binary floating point.

**Reviewed for:** Is any monetary amount held outside the shared `Money` value, missing its currency, or serialized in another shape? Look at each monetary amount in Domain.

### Keep Binary Floating Point Out of Exact Quantities

**ID:** `litespec/backend/domain#keep-binary-floating-point-out-of-exact-quantities`

**Strength:** required

**Applies to:** artifact: value-object

**Statement:** A Domain value MUST NOT use binary floating point for a monetary amount, a rate applied to one, or a quantity compared for exact equality.

**Rationale:** Binary floating point represents most decimal fractions only approximately, so two amounts a person reads as equal compare as different, and totals drift by fractions of a cent. Exact decimal types keep equality and arithmetic exact.

**Example:** A tax rate multiplied into a `Money` amount uses an exact decimal type. A measured latency in a diagnostic log is not a Domain quantity and sits outside this rule.

**Reviewed for:** Is any monetary amount, rate applied to one, or exactly-compared quantity held in a binary floating-point type? Look at the field, parameter, and return types in Domain.

### Use Stateless Domain Services for Ownerless Rules

**ID:** `litespec/backend/domain#use-stateless-domain-services-for-ownerless-rules`

**Strength:** required

**Applies to:** artifact: value-object

**Statement:** A domain service MUST be stateless, accept and return Domain values, and perform no persistence, messaging, logging, clock, authorization, or provider call.

**Rationale:** Some calculations span concepts with no natural aggregate owner. Forcing one into an aggregate couples that aggregate to another's data. A stateless service holds the calculation instead, accepting and returning Domain values with no persistence, messaging, logging, clock, authorization, or provider call. Application loads the aggregates, calls the service, and passes its result into aggregate behavior. A domain service takes no port and holds no state, so its tests are Domain tests: they sit beside the aggregate tests in the unit tests mirroring the module tree, with no harness, no substitute, and no fixture.

**Example:** An order-pricing service takes order lines and a currency and returns their total as `Money`, with no domain repository, clock, logger, or provider client. Application supplies the lines and passes the returned total into aggregate behavior.

**Reviewed for:** Is any domain service stateful, taking or returning non-Domain values, or calling persistence, messaging, logging, clock, authorization, or a provider? Look at each domain service and its calls.

### Limit Each Repository to One Aggregate

**ID:** `litespec/backend/domain#limit-each-repository-to-one-aggregate`

**Strength:** required

**Applies to:** artifact: repository

**Statement:** A domain repository MUST serve exactly one aggregate root, exposing only aggregate and Domain types, with no composable query, session, or generic CRUD surface.

**Rationale:** One domain repository per aggregate keeps the write side shaped by the aggregates it loads. A required load looks the aggregate up by identity and reports a missing aggregate through the missing-resource refusal, so a handler repeats no absence check. An optional lookup stays reserved for cases where absence is normal. A composable query, session, or generic CRUD surface would expose store mechanics to every handler. This boundary is the write side only: a query uses a read model owned by Application and shaped by Infrastructure, with no Domain contract on that path. A repository that gained a projection method would put query shapes in Domain, which is the coupling this layer exists to prevent. A flow that finds a record or creates it is two use cases that the boundary owning the flow chooses between, following `litespec/backend/application#implement-one-outcome-path-per-command`.

**Example:** A post repository loads a post by identity, reports a missing post through the missing-resource refusal, and stages the loaded aggregate through a staging call. The Infrastructure implementation owns the refusal, the command pipeline owns the commit, and the staging call stages the aggregate through the selected provider.

**Reviewed for:** Is any domain repository serving more than one aggregate, exposing non-Domain types, or offering a composable query, session, or generic CRUD surface? Look at each repository interface.

### Place Every Part Type in Its Aggregate's Folder

**ID:** `litespec/backend/domain#place-every-part-type-in-its-aggregates-folder`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A Domain part type, meaning an identifier, a state type, a closed set, a child entity, a value object, or a repository interface, MUST sit in the `Domain/` folder of the aggregate it belongs to, or in the product `Domain/` folder, and a row or projection a port returns is no part.

**Rationale:** A part serves no record of its own: it belongs to its aggregate, whose record already states the states, transitions, and data the part holds. A mark on each part would write that fact a second time, and the two copies would drift. The folder already says which aggregate a part belongs to, so the builder derives the link from it. A type in a folder that holds no aggregate root, and that is not the product `Domain/` folder, is the one place the derivation fails. A domain service that holds one rule calculation is not a part: it carries its `rule/` mark. A row or projection a read port returns is not a part either: it is the port's answer, shaped for a reader, and it sits with its port in Application.

**Example:** In the Claimwick product, `ExpenseState` and `IExpenseRepository` sit beside the `Expense` root in `Spending/Expenses/Domain/` and carry no mark; the builder lists them as parts of `aggregate/spending.expense`. `Money` sits in the product `Domain/` folder and belongs to no aggregate.

**Checked by:** `litespec model check` reports `part-parent-missing` (level S) for an unmarked Domain type whose folder holds no aggregate root and is not the product `Domain/` folder, and the model output lists each part under its aggregate.

### Declare Cross-Record Keys on the Aggregate

**ID:** `litespec/backend/domain#declare-cross-record-keys-on-the-aggregate`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** An aggregate needing a cross-record uniqueness key MUST expose its current keys through `UniquenessKeys`.

**Rationale:** `UniquenessKeys` is the only uniqueness code in Domain. The aggregate declares its current keys through it, and persistence claims and releases them in the commit that changes the aggregate, following `litespec/backend/persistence#claim-a-cross-record-key-at-commit`. The aggregate computes keys from its own state and validates nothing about other records, because only the store sees every concurrent write and arbitrates the race.

**Example:** In the library product, a book holding a list of copy barcodes returns each barcode from `UniquenessKeys`, so adding a copy claims the new barcode at commit. In the Claimwick product, a payout carrying a reference that is unique across payouts and adjustments returns that reference, so a duplicate reference fails the commit on either record.

**Reviewed for:** Is any cross-record uniqueness rule missing its `UniquenessKeys` declaration, or enforced by other code in Domain? Look at each aggregate with such a rule.

### Reject Business Violations with Dedicated Refusals

**ID:** `litespec/backend/domain#reject-business-violations-with-dedicated-refusals`

**Strength:** required

**Applies to:** artifact: aggregate, value-object

**Statement:** A rejected business rule MUST end its use case in a dedicated refusal carrying its stable code.

**Rationale:** One refusal per rejected rule lets each boundary map outcomes without parsing messages: the refusal names the failed rule, and the stable code travels to the caller. The name leads with the aggregate root. Domain raises no standard library, validation, transport, or provider failure for a business rejection, and carries no transport status: the host maps refusals to transport errors through the mapping the API page documents under `litespec/backend/api#defer-error-mapping-to-the-global-handler`. Refusal names follow `litespec/workspace/naming#name-exceptions-by-failed-rule`; this provision owns the one-refusal-per-rule shape and the code each refusal carries.

**Example:** A refund allocation rule ends its use case in its own refusal, rather than sharing one refusal with a code passed at the call site.

Two rules that share a caller-visible refusal code because a boundary maps them to one response still get two refusals. The shared code lives in the two refusals, not in a value passed by the aggregate. One refusal serves more than one input only when one rule fails from more than one input and the differing values travel as creation parameters.

Application validators handle malformed caller input through validity findings. Aggregate and value-object refusals stay the last defense when direct Domain use violates a rule. Command handlers do not catch expected Domain refusals.

**Reviewed for:** Is any rejected business rule ending outside a dedicated refusal with its stable code, or carrying transport status? Look at each rejection beside its refusal.

### Pass Nondeterministic Values into Domain

**ID:** `litespec/backend/domain#pass-nondeterministic-values-into-domain`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Domain behavior MUST receive time, randomness, and new identifiers as arguments rather than read them, and read no clock.

**Rationale:** Domain behavior that reads a clock or generates identifiers returns different results for the same call, which makes tests nondeterministic: the same test passes and fails with no code change. Receiving time, randomness, and new identifiers as arguments keeps every Domain call a pure function of its inputs: a test supplies explicit values and stays deterministic. Application obtains each value through an owned port and passes it in. A method that needs more than one value receives a clock or a generator through its arguments, and version 7 identifiers come from the caller-supplied generator.

**Reviewed for:** Is any clock read, random source, or new identifier reached from inside Domain instead of passed in as an argument? Look at the calls Domain code makes.

### Document Every Public Domain Contract

**ID:** `litespec/backend/domain#document-every-public-domain-contract`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every public Domain type and member MUST carry documentation stating the business constraint, result, or refusal it represents.

**Rationale:** A reader meeting a public Domain member needs the business rule behind it, not just its signature. Documentation states the business constraint, result, or refusal the member represents. A mutation method names its source states, resulting state, protected invariant, and recorded event. A summary that restates the member name adds nothing.

**Reviewed for:** Is any public Domain type or member missing documentation of its business constraint, result, or refusal, or documented with a restated name? Look at the documentation on each public member.

## Conventions

### Organize a Module by Aggregate and Concept

**ID:** `litespec/backend/domain#organize-a-module-by-aggregate-and-concept`

**Strength:** default

**Applies to:** artifact: any

**Default:** Give each aggregate a `Domain/` folder named with its plural root name, holding its own states, events, refusals, concepts, and one folder per part its record declares.

**Replacement:** A project names its own module layout in its documentation.

**Rationale:** Each aggregate `Domain/` folder holds everything one aggregate owns: its states, events, refusals, and concepts. A reader finds the full aggregate in one place instead of hunting shared folders. A single aggregate stays flat only when its plural root name equals the module name; separate aggregates keep separate lifecycle, event, and refusal folders.

A part is a data group the aggregate record declares with dotted field names, such as `parcel.*`. The part's child entity and the values only that child holds sit in a folder named for the part, such as `Parcels/`, while the part's events and refusals stay in the aggregate's `Events/` and `Refusals/` folders. A type that belongs to no part stays at the root.

**Example:**

```text
{Product}/
  Domain/                         the shared kernel: the root, event, identity, and refusal contracts
  Posts/                          one aggregate whose name matches the module: kept flat in the module folder
    Domain/
      Post
      PostId
      PostTitle
      PostRepository
      States/
        PostState
        PostDraftState
        PostPublishedState
        PostArchivedState
      Events/
        PostCreatedEvent
        PostPublishedEvent
      Refusals/
        PostIdentityRequired
        PostAlreadyPublished
  Shipping/                       one aggregate whose name differs from the module, with a domain union in a concept folder
    Shipments/
      Domain/
        Shipment
        ShipmentId
        ShipmentRepository
        Outcomes/
          ShipmentOutcome
          ShipmentDeliveredOutcome
          ShipmentReturnedOutcome
          ShipmentLostOutcome
        States/
          ShipmentState
          ShipmentPendingState
          ShipmentDispatchedState
          ShipmentDeliveredState
        Events/
          ShipmentOutcomeRecordedEvent
  Customers/                      more than one aggregate: one plural folder per aggregate
    CustomerAccounts/
      Domain/
        CustomerAccount
        CustomerAccountId
        CustomerAccountRepository
        States/
          CustomerAccountState
          CustomerAccountClaimedState
        Events/
          CustomerAccountRestrictedEvent
    Consents/
      Domain/
        Consent
        ConsentId
        ConsentRepository
        States/
          ConsentState
          ConsentGrantedState
        Events/
          ConsentGrantedEvent
```

Each aggregate's domain repository stays with the aggregate it loads. The module, aggregate, and use case order follows `litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case`. Each stack page states the file names and extensions that carry this layout.

**Checked by:** `litespec model check` reports `code-placement-drift` (level S) for a domain event outside its aggregate's `Events` folder.

### Name Domain Types from Their Aggregate Root

**ID:** `litespec/backend/domain#name-domain-types-from-their-aggregate-root`

**Strength:** default

**Applies to:** artifact: any

**Default:** Derive each Domain type name from the aggregate root it belongs to, following the workspace naming provisions cited below.

**Replacement:** A project names its own Domain naming rules in its documentation.

**Rationale:** Aggregate-prefixed names keep each type readable at its call site without its namespace. Aggregate-owned names follow `litespec/workspace/naming#anchor-aggregate-owned-types-on-the-aggregate-root`, architectural suffixes follow `litespec/workspace/naming#use-architectural-suffixes`, and refusal names follow `litespec/workspace/naming#name-exceptions-by-failed-rule`. A project names every state case in one form and keeps it.

**Example:** A post title reads as `PostTitle` at its call site, and a published post state as `PostPublishedState`. The stack page states the declaration form each name takes.

**Reviewed for:** Is any Domain name breaking the cited workspace naming provisions, or any state case breaking the one kept form? Look at each Domain name.

### Reuse the Shared Domain Contracts Across Modules

**ID:** `litespec/backend/domain#reuse-the-shared-domain-contracts-across-modules`

**Strength:** default

**Applies to:** artifact: any

**Default:** Declare the shared Domain contracts once in the product `Domain/` folder and reuse them across every module.

**Replacement:** A project names its own shared-contract layout in its documentation.

**Rationale:** One declaration keeps the root, event, identity, and refusal mechanics identical in every module. A second declaration drifts from the first with the next fix.

**Example:** The shared contracts are the aggregate root, the domain event, the typed identity, and the refusal.

Event clearing is delivery mechanics, not a business mutation. Infrastructure calls it only after delivery accepts the pending events.

**Reviewed for:** Is the root, event, identity, or refusal contract declared more than once across modules? Look at those contracts in each module.

### Align ID Representations at Every Boundary

**ID:** `litespec/backend/domain#align-id-representations-at-every-boundary`

**Strength:** default

**Applies to:** artifact: aggregate, state-record, value-object, domain-event

**Default:** Represent an identifier the same way in Domain, persistence, transport, and generated clients.

**Replacement:** A project names its own identifier-mapping rules in its documentation.

**Rationale:** One identifier takes a different form at each boundary, and each form is correct in its own place. Alignment means every form still reads as the same value, so a reader tracing one identifier follows it across boundaries without translation.

**Example:** Domain and Application hold the typed identifier; the [HTTP API](api.md) states the route, query, JSON, and contract forms, and [Persistence](persistence.md) states the stored form. Alignment means a reader recognizes one identifier in every form.

**Reviewed for:** Is any identifier unreadable as the same value across Domain, persistence, transport, or generated clients? Look at one identifier in each boundary form.
