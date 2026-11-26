# .NET Testing

**Stack:** dotnet

**Concern:** tests

Acceptance tests in a .NET backend prove reactions through the outbox they travel: the test drains the outbox in process and asserts the effect the scenario names. Critical cross-layer examples also run as executable BDD scenarios: LightBDD feature classes that reviewers read in business language and the runner executes through a public boundary such as the HTTP API. This page defines how the shared [Testing](../../core/testing.md) rules apply to .NET: draining the outbox in acceptance tests, and writing BDD scenarios through the LightBDD runner.

## Concepts

### Durable Messaging in Tests

An acceptance test asserts what a caller observes: the response, a later read, or a reaction's effect. A reaction's effect travels through the outbox, so the test drains the outbox before asserting it. The durable-messaging test package hosts a transactional outbox in the test and drains it on demand, so a scenario observes the reaction's effect without waiting for the processor's polling delay.

### Executable BDD Scenarios

A BDD scenario is a C# method on a LightBDD feature class. Its steps are calls to step methods. Its business-readable text is the scenario method, the step method names, and the generated report. LightBDD binds steps at compile time, so a step that no longer exists, or whose parameters changed, fails the build rather than the run. Each BDD scenario proves one scenario record from the written specification, which stays the behavior source.

A project uses BDD only where business-language form improves shared understanding of cross-layer behavior: product, domain, engineering, and testing discussions then share one executable example. Routine unit behavior stays in narrower tests. Each BDD scenario carries one `proves` claim naming the scenario it proves, calls the proved use case through a public boundary, and asserts an observable outcome. The model builder reads `proves` marks on C# methods, so a Gherkin feature file would not count as proof and would state each scenario twice beside its scenario record.

### Test Runners for BDD Scenarios

BDD scenarios run on the LightBDD runner with `xunit.v3.mtp-v2`, each pinned in the manifest. LightBDD 3.12 builds against the xUnit v3 3.2 line, so the acceptance test project runs the 3.2 line while other test projects run the current one.

## Standards

### Drain the Outbox in Acceptance Tests

**ID:** `litespec/packs/dotnet/testing#drain-the-outbox-in-acceptance-tests`

**Strength:** required

**Applies to:** artifact: test

**Statement:** An acceptance test that asserts a reaction's effect MUST drain the outbox through the durable-messaging test package before asserting it.

**Rationale:** The test hosts a transactional outbox through `AddTestTransactionalOutbox` and drains it through `ProcessorPassHelpers.DrainOutboxAsync`, both from `LiteBus.Testing.DurableMessaging` at its manifest pin. Draining runs each subscribed handler in process, so the scenario observes the reaction's effect without the processor's polling delay. The suite still runs through the real application host under `litespec/core/testing#share-one-production-faithful-integration-harness`.

**Example:** In Claimwick, the expense-approval acceptance test sends the approve command, drains the outbox, and then asserts the payout the schedule-payout reaction wrote. Without the drain, the test would assert before the processor delivers the approval.

**Reviewed for:** Does every acceptance test that asserts a reaction's effect drain the outbox before asserting it? Look at each acceptance test beside the outbox test package.

### Select Shared Critical Examples

**ID:** `litespec/packs/dotnet/testing#select-shared-critical-examples`

**Strength:** required

**Applies to:** artifact: test

**Statement:** A test author MUST create a BDD scenario only when business-language form improves shared understanding of cross-layer behavior.

**Rationale:** A scenario costs maintenance on every behavior change, so the suite earns that cost only where business-language form improves shared understanding of cross-layer behavior. Each selected scenario gives product, domain, engineering, and testing discussions one executable example instead of four retellings.

**Reviewed for:** Does every BDD scenario trace to a cross-layer example the team discusses in business language? Look at each BDD scenario in the acceptance suite.

### Avoid Routine Scenario Duplication

**ID:** `litespec/packs/dotnet/testing#avoid-routine-scenario-duplication`

**Strength:** required

**Applies to:** artifact: test

**Statement:** A test author MUST NOT duplicate every unit or validation test as a BDD scenario.

**Rationale:** A unit test fails at the exact rule and runs in milliseconds. The same test duplicated as a BDD scenario fails at a public boundary after a full setup. Duplication slows the suite and moves the failure away from its cause, so routine behavior stays in its narrower tests.

**Reviewed for:** Does any unit or validation test repeat as a duplicate BDD scenario? Look at the narrower suites beside the acceptance suite.

### Trace Every BDD Scenario to Its Scenario

**ID:** `litespec/packs/dotnet/testing#trace-every-bdd-scenario-to-its-scenario`

**Strength:** required

**Applies to:** artifact: test

**Statement:** Every BDD scenario MUST carry one `proves` claim naming the scenario it proves.

**Rationale:** A scenario pairs one `given` situation and `when` action with the `then` result they lead to. The proves claim ties the BDD scenario to that record. The claim names it, the steps call the proved use case, and the assertion checks its result. Without the claim, the model builder cannot tell which scenario the BDD scenario proves. A LightBDD scenario carries the claim as a proves mark on the scenario method, in the style the project declares.

**Example:** `/// <proves>scenario/cancel-order.canceling-twice-is-refused</proves>` traces a cancellation BDD scenario to its scenario record.

**Reviewed for:** Does every BDD scenario carry one `proves` claim naming its scenario? Look at each BDD scenario in the acceptance suite.

### Call Public Boundaries from BDD Steps

**ID:** `litespec/packs/dotnet/testing#call-public-boundaries-from-bdd-steps`

**Strength:** required

**Applies to:** artifact: test

**Statement:** A BDD step MUST call the HTTP API or another declared public system boundary.

**Rationale:** A step that calls the HTTP API or another declared public boundary proves what a caller can observe. A step that reaches past the boundary depends on implementation details, so a refactoring breaks the scenario without breaking the behavior.

**Reviewed for:** Does every step definition stay within the HTTP API and the declared public boundaries? Look at each step definition in the acceptance suite.

### Assert Observable Outcomes

**ID:** `litespec/packs/dotnet/testing#assert-observable-outcomes`

**Strength:** required

**Applies to:** artifact: test

**Statement:** A BDD scenario MUST assert a response, later read, durable event, audit record, or other observable outcome.

**Rationale:** A scenario that runs requests without asserting proves only that nothing threw. Asserting a response, a later read, a durable event, an audit record, or another observable outcome shows the public boundary achieved the expected behavior.

**Reviewed for:** Does every BDD scenario assert a response, read, event, record, or other observable outcome? Look at each BDD scenario in the acceptance suite.

### Limit Database Assertions

**ID:** `litespec/packs/dotnet/testing#limit-database-assertions`

**Strength:** required

**Applies to:** artifact: test

**Statement:** A BDD scenario MUST use direct database assertions only for a documented durable boundary.

**Rationale:** The database is an implementation detail, so asserting its rows couples the scenario to storage shapes. A direct assertion is allowed only for a documented durable boundary, such as an outbox record, where the stored row is itself the promised evidence.

**Reviewed for:** Does every direct database assertion name a documented durable boundary? Look at each direct database assertion in the acceptance suite.

### Own Scenario Data

**ID:** `litespec/packs/dotnet/testing#own-scenario-data`

**Strength:** required

**Applies to:** artifact: test

**Statement:** Each BDD scenario MUST own its identifiers, actor context, requests, responses, and expected state.

**Rationale:** A scenario that borrows identifiers or state prepared elsewhere breaks when that other setup changes. Owning its identifiers, actor context, requests, responses, and expected state keeps each scenario self-contained and runnable alone.

**Reviewed for:** Does every BDD scenario own its identifiers, actor context, requests, responses, and expected state? Look at each BDD scenario in the acceptance suite.

### Isolate Scenario Execution

**ID:** `litespec/packs/dotnet/testing#isolate-scenario-execution`

**Strength:** required

**Applies to:** artifact: test

**Statement:** The acceptance suite MUST reset database state between scenarios and avoid scenario-order dependencies.

**Rationale:** State left by one scenario becomes a hidden precondition of the next. The suite then passes only in one order. Resetting database state between scenarios removes that coupling, so any scenario runs alone or in any order.

**Reviewed for:** Does every scenario pass alone and in any order, with state reset between runs? Look at the acceptance suite runs.

### Translate Business Phrases into Test-Driver Calls

**ID:** `litespec/packs/dotnet/testing#translate-business-phrases-into-test-driver-calls`

**Strength:** required

**Applies to:** artifact: test

**Statement:** A BDD step definition MUST translate a business phrase into a typed test-driver call.

**Rationale:** HTTP calls, JSON shapes, and assertion mechanics in a step definition hide the business phrase the scenario promises. Translating each phrase into a typed test-driver call keeps the scenario readable while the driver owns the boundary mechanics.

**Reviewed for:** Does every step definition translate its phrase through a typed test-driver call? Look at each step definition in the acceptance suite.

### Exclude Production Internals

**ID:** `litespec/packs/dotnet/testing#exclude-production-internals`

**Strength:** required

**Applies to:** artifact: test

**Statement:** A BDD step definition MUST NOT resolve repositories, handlers, sessions, or the production service provider.

**Rationale:** Resolving a repository, handler, session, or the production service provider runs the behavior without the boundary the scenario claims to verify. The scenario then passes while the public path stays untested, so step definitions resolve none of them.

**Reviewed for:** Does any step definition resolve a repository, handler, session, or the production service provider? Look at each step definition in the acceptance suite.

### Preserve Scenario Failures

**ID:** `litespec/packs/dotnet/testing#preserve-scenario-failures`

**Strength:** required

**Applies to:** artifact: test

**Statement:** A BDD hook MUST await asynchronous setup and cleanup without replacing the original scenario failure.

**Rationale:** Setup and cleanup are asynchronous. An unawaited hook leaves the next scenario running against a half-reset database. A hook that throws during cleanup can also replace the scenario's own failure, hiding the real cause. Awaiting both without replacing the original failure keeps the evidence trustworthy.

**Reviewed for:** Does every hook await setup and cleanup without replacing the original scenario failure? Look at each hook in the acceptance suite.

## Conventions

### Group Feature Classes by Module

**ID:** `litespec/packs/dotnet/testing#group-feature-classes-by-module`

**Strength:** default

**Applies to:** artifact: test

**Default:** Group feature classes under `{Module}/{Aggregates}/{UseCase}/` folders in `Acceptance.Tests`, with a module-level read dropping the aggregate segment.

**Replacement:** A project names its own feature grouping in its documentation.

**Rationale:** A scenario filed by business module and aggregate sits beside the vocabulary it exercises, so a reader finds every example of one aggregate in one folder. A flat or technically grouped suite scatters those examples.

**Reviewed for:** Does every feature class sit under its `{Module}/{Aggregates}/{UseCase}/` folder, or under a documented grouping? Look at `Acceptance.Tests` beside the project documentation.

### Keep Steps Vocabulary-Scoped

**ID:** `litespec/packs/dotnet/testing#keep-steps-vocabulary-scoped`

**Strength:** default

**Applies to:** artifact: test

**Default:** Keep step definitions narrow and reusable only within one business vocabulary.

**Replacement:** A project names its own step-sharing scope in its documentation.

**Rationale:** A step reused across vocabularies, such as a generic success step, means something subtly different in each place. It also couples unrelated modules to one definition. Steps narrow to one business vocabulary keep their meaning exact.

**Reviewed for:** Does every reusable step set serve one business vocabulary, or a documented scope? Look at each reusable step set beside the project documentation.

### Limit Scenario Context

**ID:** `litespec/packs/dotnet/testing#limit-scenario-context`

**Strength:** default

**Applies to:** artifact: test

**Default:** Store values for one scenario in a scenario context object.

**Replacement:** A project names its own scenario-context use in its documentation.

**Rationale:** One context object holding the scenario's own values keeps shared state visible and small. Resolving services from it turns it into a service locator that hides what the scenario depends on.

**Reviewed for:** Does every scenario context object hold only scenario state, or a documented use? Look at the scenario context beside the project documentation.

### Mark Pull-Request Scenarios

**ID:** `litespec/packs/dotnet/testing#mark-pull-request-scenarios`

**Strength:** default

**Applies to:** artifact: test

**Default:** Mark the small pull-request subset separately from the proves claim, with a critical test trait on the LightBDD scenario.

**Replacement:** A project names its own release-gating mark in its documentation.

**Rationale:** The critical trait selects the small subset whose failure stops a release, for fast pull-request feedback. The marking stays separate from the proves claim, which records scenario proof rather than release gating. A marking that every scenario carries selects nothing, so the subset keeps a stated size bound and an owner who keeps it within that bound.

**Reviewed for:** Does the marked pull-request subset stay within its stated bound with its owner named? Look at the marked subset beside the proves claims.
