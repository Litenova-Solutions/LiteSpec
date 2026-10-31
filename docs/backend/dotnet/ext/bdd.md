# Executable BDD Acceptance Tests

## Intent

This extension adds executable BDD scenarios for cross-layer examples that need review in business language and execution through a public boundary such as the HTTP API. Test authors write them as Reqnroll Gherkin features or as LightBDD feature classes, and use them for the critical examples its rules select, so product, domain, engineering, and testing discussions share one executable example. Each BDD scenario proves one scenario record from the written specification, which stays the behavior source.

## Activation

Activation scope: `local`.

Applicable specification kinds: `use-case`, `journey`.

The consumer enables `bdd` when a critical example needs both business-language review and execution through a public system boundary; routine unit behavior stays in narrower tests.

The consumer selects one runner for the whole suite; every provision below applies to either runner.

| Runner | A scenario is | It proves a scenario with | Its business-readable text is |
|:---|:---|:---|:---|
| Reqnroll | A Gherkin scenario in a `.feature` file, bound to step definitions | A `proves:` line under the scenario title | The feature file |
| LightBDD | A C# method on a feature class, whose steps are calls to step methods | A proves mark on the scenario method | The scenario method, the step method names, and the generated report |

LightBDD binds steps at compile time instead of matching text at run time. A step that no longer exists, or whose parameters changed, fails the build rather than the run. A consumer that generates its API client from the published contract extends the same property to every request a step sends.

## Baseline Relationship

This extension replaces no baseline provision: it adds an acceptance test project and the manifest-pinned packages of the selected runner.

It switches on `scenario`, so its BDD scenarios prove scenario records.

## Standards

### Select Shared Critical Examples

**ID:** `litespec/backend/dotnet/ext/bdd#select-shared-critical-examples`

**Statement:** A test author MUST create a BDD scenario only when business-language form improves shared understanding of cross-layer behavior.

**Rationale:** A scenario costs maintenance on every behavior change, so the suite earns its keep only where business-language form improves shared understanding of cross-layer behavior. Each selected scenario gives product, domain, engineering, and testing discussions one executable example instead of four retellings.

**Reviewed for:** Which BDD scenarios trace to no cross-layer example the team discusses in business language? Look at each BDD scenario in the acceptance suite.

### Avoid Routine Scenario Duplication

**ID:** `litespec/backend/dotnet/ext/bdd#avoid-routine-scenario-duplication`

**Statement:** A test author MUST NOT duplicate every unit or validation test as a BDD scenario.

**Rationale:** A unit test fails at the exact rule and runs in milliseconds; the same test duplicated as a BDD scenario fails at a public boundary after a full setup. Duplication slows the suite and moves the failure away from its cause, so routine behavior stays in its narrower tests.

**Reviewed for:** Which unit or validation tests repeat as a duplicate BDD scenario? Look at the narrower suites beside the acceptance suite.

### Prove a Scenario from Every BDD Scenario

**ID:** `litespec/backend/dotnet/ext/bdd#prove-a-scenario-from-every-bdd-scenario`

**Statement:** Every BDD scenario MUST carry one `proves` claim naming the scenario it proves.

**Rationale:** A scenario pairs one `given` situation and `when` action with the `then` result they lead to, and the proves claim is what ties the BDD scenario to that record. The claim names it, the steps call the proved use case, and the assertion checks its result; without the claim, the model builder cannot tell which scenario the BDD scenario proves. A LightBDD scenario carries the claim as a proves mark on the scenario method, in the style the project declares, and a Reqnroll scenario as a `proves:` line under its title.

**Example:** `/// <proves>scenario/cancel-order.canceling-twice-is-refused</proves>` traces a cancellation BDD scenario to its scenario record.

**Reviewed for:** Which BDD scenarios lack one `proves` claim naming their scenario? Look at each BDD scenario in the acceptance suite.

### Drive Public Behavior

**ID:** `litespec/backend/dotnet/ext/bdd#drive-public-behavior`

**Statement:** A BDD step MUST call the HTTP API or another declared public system boundary.

**Rationale:** A step that calls the HTTP API or another declared public boundary proves what a caller can observe. A step that reaches past the boundary depends on implementation details, so a refactoring breaks the scenario without breaking the behavior.

**Reviewed for:** Which step definitions call outside the HTTP API and the declared public boundaries? Look at each step definition in the acceptance suite.

### Assert Observable Outcomes

**ID:** `litespec/backend/dotnet/ext/bdd#assert-observable-outcomes`

**Statement:** A BDD scenario MUST assert a response, later read, durable event, audit record, or other observable outcome.

**Rationale:** A scenario that runs requests without asserting proves only that nothing threw. Asserting a response, a later read, a durable event, an audit record, or another observable outcome shows the public boundary achieved the expected behavior.

**Reviewed for:** Which BDD scenarios assert no response, read, event, record, or other observable outcome? Look at each BDD scenario in the acceptance suite.

### Limit Database Assertions

**ID:** `litespec/backend/dotnet/ext/bdd#limit-database-assertions`

**Statement:** A BDD scenario MUST use direct database assertions only for a documented durable boundary.

**Rationale:** The database is an implementation detail, so asserting its rows couples the scenario to storage shapes. A direct assertion is allowed only for a documented durable boundary, such as an outbox record, where the stored row is itself the promised evidence.

**Reviewed for:** Which direct database assertions name no documented durable boundary? Look at each direct database assertion in the acceptance suite.

### Own Scenario Data

**ID:** `litespec/backend/dotnet/ext/bdd#own-scenario-data`

**Statement:** Each BDD scenario MUST own its identifiers, actor context, requests, responses, and expected state.

**Rationale:** A scenario that borrows identifiers or state prepared elsewhere breaks when that other setup changes. Owning its identifiers, actor context, requests, responses, and expected state keeps each scenario self-contained and runnable alone.

**Reviewed for:** Which BDD scenarios borrow identifiers, actor context, requests, responses, or expected state? Look at each BDD scenario in the acceptance suite.

### Isolate Scenario Execution

**ID:** `litespec/backend/dotnet/ext/bdd#isolate-scenario-execution`

**Statement:** The acceptance suite MUST reset database state between scenarios and avoid scenario-order dependencies.

**Rationale:** State left by one scenario becomes a hidden precondition of the next, and the suite passes only in one order. Resetting database state between scenarios removes that coupling, so any scenario runs alone or in any order.

**Reviewed for:** Which scenarios fail alone or in another order, or which runs skip the state reset? Look at the acceptance suite runs.

### Translate Business Phrases

**ID:** `litespec/backend/dotnet/ext/bdd#translate-business-phrases`

**Statement:** A BDD step definition MUST translate a business phrase into a typed test-driver call.

**Rationale:** HTTP calls, JSON shapes, and assertion mechanics in a step definition hide the business phrase the scenario promises. Translating each phrase into a typed test-driver call keeps the scenario readable while the driver owns the boundary mechanics.

**Reviewed for:** Which step definitions skip the typed test-driver call? Look at each step definition in the acceptance suite.

### Exclude Production Internals

**ID:** `litespec/backend/dotnet/ext/bdd#exclude-production-internals`

**Statement:** A BDD step definition MUST NOT resolve repositories, handlers, sessions, or the production service provider.

**Rationale:** Resolving a repository, handler, session, or the production service provider runs the behavior without the boundary the scenario claims to verify. The scenario then passes while the public path stays untested, so step definitions resolve none of them.

**Reviewed for:** Which step definitions resolve a repository, handler, session, or the production service provider? Look at each step definition in the acceptance suite.

### Preserve Scenario Failures

**ID:** `litespec/backend/dotnet/ext/bdd#preserve-scenario-failures`

**Statement:** A BDD hook MUST await asynchronous setup and cleanup without replacing the original scenario failure.

**Rationale:** Setup and cleanup are asynchronous, and an unawaited hook leaves the next scenario running against a half-reset database. A hook that throws during cleanup can also replace the scenario's own failure, hiding the real cause. Awaiting both without replacing the original failure keeps the evidence trustworthy.

**Reviewed for:** Which hooks skip awaiting setup or cleanup, or replace the original scenario failure? Look at each hook in the acceptance suite.

## Conventions

### Group Feature Files by Module

**ID:** `litespec/backend/dotnet/ext/bdd#group-feature-files-by-module`

**Default:** Group feature files or feature classes under module folders in `Acceptance.Tests`.

**Replacement:** A project names its own feature grouping in its documentation.

**Rationale:** A scenario filed by business module sits beside the vocabulary it exercises, so a reader finds every example of one module in one folder. A flat or technically grouped suite scatters them.

**Reviewed for:** Which feature files or feature classes sit outside module folders without a documented grouping? Look at `Acceptance.Tests` beside the project documentation.

### Keep Steps Vocabulary-Scoped

**ID:** `litespec/backend/dotnet/ext/bdd#keep-steps-vocabulary-scoped`

**Default:** Keep step definitions narrow and reusable only within one business vocabulary.

**Replacement:** A project names its own step-sharing scope in its documentation.

**Rationale:** A step reused across vocabularies, such as a generic success step, means something subtly different in each place and couples unrelated modules to one definition. Steps narrow to one business vocabulary keep their meaning exact.

**Reviewed for:** Which reusable step sets serve more than one business vocabulary without a documented scope? Look at each reusable step set beside the project documentation.

### Limit Scenario Context

**ID:** `litespec/backend/dotnet/ext/bdd#limit-scenario-context`

**Default:** Store values for one scenario in a scenario context object.

**Replacement:** A project names its own scenario-context use in its documentation.

**Rationale:** One context object holding the scenario's own values keeps shared state visible and small. Resolving services from it turns it into a service locator that hides what the scenario depends on.

**Reviewed for:** Which scenario context objects hold more than scenario state without a documented use? Look at the scenario context beside the project documentation.

### Mark Pull-Request Scenarios

**ID:** `litespec/backend/dotnet/ext/bdd#mark-pull-request-scenarios`

**Default:** Mark the small pull-request subset separately from the proves claim: a Reqnroll scenario with the `@critical` tag, a LightBDD scenario with a critical test trait.

**Replacement:** A project names its own release-gating mark in its documentation.

**Rationale:** The `@critical` tag, or the LightBDD critical trait, selects the small subset whose failure stops a release, for fast pull-request feedback. The marking stays separate from the proves claim, which records scenario proof rather than release gating. A marking that every scenario carries selects nothing, so the subset keeps a stated size bound and an owner who keeps it there.

**Reviewed for:** Which pull-request subsets grow beyond their stated bound or lose their owner? Look at the marked subset beside the proves claims.

## Dependencies

Packages: `Reqnroll` and `Reqnroll.xunit.v3` for Gherkin features, or `LightBDD.XUnit3` with `xunit.v3.mtp-v2` for C# feature classes, each pinned in the manifest. LightBDD 3.12 builds against xUnit v3 3.2. Its test project therefore runs the 3.2 line, and other test projects run the current one.

Related extensions: None.

Guides: None.
