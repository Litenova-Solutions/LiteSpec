# Backend Testing

## Intent

Backend tests prove behavior at four separate boundaries: domain logic with no dependencies, use-case coordination with test stand-ins, real persistence and HTTP behavior through the production host, and structural rules with architecture tests. This page states which suites each project size keeps, what each suite runs with, and how tests prove the scenarios and rules the specification records. Separate boundaries keep fast tests fast and point each failure at the layer that broke.

## Concepts

### Test Boundaries

Each suite proves one boundary and runs with one set of tools. A test belongs to the suite whose boundary it proves.

| Suite | Proves | Runs With |
|:---|:---|:---|
| Domain | Factories, lifecycles, invariants, and value behavior | Explicit inputs only, with no database, host, container, clock, or mock |
| Application | Handler coordination: what was loaded, called, staged, and returned | Substituted repositories, clocks, actor accessors, and ports |
| Integration | Real persistence and HTTP behavior through the production host | The pinned PostgreSQL and the real application host |
| Architecture | Project references, visibility, folders, and inheritance | Static checks over the compiled code |
| Acceptance | End-to-end behavior for large projects | The executable BDD extension and its host |

### Scenario Proof

A scenario states one behavior the product shows, written in `given`, `when`, `then`: the situation, the action, and the expected ending. A result names one way a use case ends: success, or a refusal carrying a stable code. A test proves a scenario by carrying a proves mark naming it, calling its use case through the public boundary, and asserting the result the scenario names. A scenario may point at a derived generic result, and an automation scenario is proved the same way: the test fires the trigger and asserts the result of the use case the automation runs. A rule claim confirms only through a scenario the same test proves. A frontend test proves a scenario the same way: it carries the proves mark, drives the scenario through the boundary it calls, and asserts the result the scenario names. Test runs match results to tests by fully qualified name, so a renamed test matches nothing. The model builder confirms each claim from the test body, and the latest test run marks it passing or failing.

## Standards

### Keep the Test Suites the Size Requires

**ID:** `litespec/backend/testing#keep-the-test-suites-the-size-requires`

**Statement:** A backend codebase MUST hold exactly the test suites its size requires: architecture and integration for small, adding acceptance for large.

**Rationale:** Each suite owns one boundary and one harness, so a project that keeps exactly its suites knows where each new test belongs. A missing suite pushes its tests into another suite's harness, where they run with the wrong fixtures, and an extra unnamed suite collects the tests nobody placed. Unit suites stay optional in both sizes, and acceptance stays optional for small. An acceptance suite without the executable BDD extension is an unnamed baseline suite: select the extension, or name the suite for what it actually is.

**Example:** A backend codebase holds these suite folders:

```text
architecture-tests/
integration-tests/
acceptance-tests/
```

A small codebase holds the first two. Acceptance appears when the executable BDD extension activates.

**Reviewed for:** Which test folders miss a required suite, or hold an extra unnamed one? Look at the test folders beside the declared project size.

### Test Domain in Isolation

**ID:** `litespec/backend/testing#test-domain-in-isolation`

**Statement:** A Domain test MUST use explicit inputs with no database, HTTP host, container, clock, or mock.

**Rationale:** Domain behavior is deterministic: explicit inputs in, new state or a refusal out. A database, host, container, clock, or mock in a Domain test hides nondeterminism behind the call, so the same test passes and fails without a code change. The suite covers factories, every lifecycle state, allowed and rejected transitions, invariants, value equality, collections, money rules, exceptions, and raised events.

**Reviewed for:** Which Domain tests take more than explicit inputs? Look at the Domain test files for database, host, container, clock, or mock use.

### Test Application Coordination

**ID:** `litespec/backend/testing#test-application-coordination`

**Statement:** An Application test MUST substitute repositories, clocks, actor accessors, and external ports, then assert the handler's coordination.

**Rationale:** A handler's job is coordination: load the right aggregates, call Domain behavior, stage the result. Substitutes for repositories, clocks, actor accessors, and ports isolate that coordination, so a failure names the handler rather than the database or the provider. The assertion covers which aggregate was loaded, which Domain behavior ran, what was staged, and what was returned, and validator tests cover each structural rule separately.

**Example:** A handler test for submitting an expense substitutes the expense and policy repositories, sends a command carrying an expense identifier, an employee identifier, an amount, and a receipt, and asserts the returned expense identifier alongside what was loaded and staged. A second test sends an expense above the threshold with no receipt and asserts the `spending.receipt-missing` refusal. Both tests run with no database connection, because the repositories are substitutes.

**Reviewed for:** Which Application tests skip a substitute, or miss a coordination assertion? Look at each Application test file.

### Test HTTP Integration Through the Real Application Host

**ID:** `litespec/backend/testing#test-http-integration-through-the-real-application-host`

**Statement:** An HTTP integration test MUST run through the real application host with the real configuration, covering Problem Details, authentication, and resource authorization.

**Rationale:** Only the real host wires the real routing, serialization, authentication, and authorization. A test that bypasses the host proves the handler again and misses the boundary where status codes, Problem Details shapes, and policy denials are actually produced. Persistence behavior is covered against PostgreSQL under `litespec/backend/persistence#test-persistence-against-postgresql`, and both run on the harness that `litespec/backend/testing#share-one-production-faithful-integration-harness` owns.

**Reviewed for:** Which HTTP integration tests bypass the real host factory or production configuration, or skip Problem Details, authentication, or authorization? Look at the HTTP integration tests.

### Isolate Integration State

**ID:** `litespec/backend/testing#isolate-integration-state`

**Statement:** An integration test MUST reset database state between tests through one documented strategy and depend on no test order.

**Rationale:** When two tests share mutable business state, each passes or fails depending on which ran first, and the failure disappears when one test runs alone. One documented reset strategy removes that coupling. Container and host fixtures stay shared for cost, because only mutable business state leaks between tests.

The default strategy is one database per test class, cloned from a migrated template. The harness applies the schema once per run to a template database. Each test class then receives its own database cloned from that template and drops that database when the class finishes. A clone takes milliseconds, so every class starts from an empty migrated schema without a second container. A project that resets tables between tests instead records that strategy in its testing documentation.

**Reviewed for:** Which integration tests share mutable business state across runs, or which reset strategies stay undocumented? Look at the tests beside the testing documentation.

### Assert Architecture Boundaries with Tests

**ID:** `litespec/backend/testing#assert-architecture-boundaries-with-tests`

**Statement:** An architecture test MUST assert the structural boundaries that `litespec/backend/architecture#test-structural-boundaries` names.

**Rationale:** The compiler does not enforce project references, visibility, or folder placement, so a review miss on any of them lands in the main branch silently. An architecture test turns each structural boundary into a failure the suite reports on the change that broke it.

**Reviewed for:** Which structural boundaries lack an architecture test? Look at the architecture test files beside the compiled code.

### Mark the Test That Proves Each Scenario

**ID:** `litespec/backend/testing#mark-the-test-that-proves-each-scenario`

**Statement:** Each scenario MUST be proved by a test carrying its proves mark, calling its use case through the public boundary, and asserting the result the scenario names.

**Rationale:** A scenario states one behavior the product shows, and the proves mark is the test's claim that it shows it. A test that drives the use case through its steps, its message, or its endpoint exercises the public boundary; a test that constructs a handler, an aggregate, or a repository reaches past it and proves nothing. The assertion names the result: the success fields for a success, the refusal code inside an assertion for a refusal. An automation scenario is proved the same way: its test fires the trigger and asserts the result of the use case the automation runs. A scenario no test proves is recorded as a gap exception with owner and expiry date. The latest test run marks each confirmed claim passing or failing.

**Example:** An acceptance scenario for submitting an expense carries the mark for `scenario/submit-expense.submitting-with-a-receipt-succeeds`, drives the submit-expense steps, and asserts the submitted expense. An integration test for the same scenario constructs the submit-expense message, sends it through the host, and asserts the same result. The mark names the scenario, the call crosses the public boundary, and the model builder confirms the claim from the test body.

**Reviewed for:** Which scenarios lack a proving test with its mark, boundary call, and passing run? Look at the book beside the test bodies.

### Prove Each Rule Through Its Scenarios

**ID:** `litespec/backend/testing#prove-each-rule-through-its-scenarios`

**Statement:** A rule claim MUST confirm only through a scenario the same test proves.

**Rationale:** Each rule holds where its kind says: an invariant inside the aggregate, a precondition at the guard, a calculation in its pure evaluation, a cross-aggregate rule at the store or the reacting use case. A scenario that walks through that point exercises the rule, so the rule claim rides on the scenario proof instead of carrying its own enforcement-point mark. A calculation rule earns its proof from a scenario carrying the examples table that `litespec/core/domain-model#tabulate-every-calculation-with-examples` requires.

**Example:** A test proving the scenario for an expense above the threshold with no receipt claims the receipt rule alongside the scenario. The scenario sends the command and asserts the `spending.receipt-missing` refusal, and the rule claim confirms because the same test proves the scenario. No separate mark names the validator or the store.

**Reviewed for:** Which rule claims stand without a proved scenario claim on the same test? Look at each rule claim.

### Match Tests by Fully Qualified Name

**ID:** `litespec/backend/testing#match-tests-by-fully-qualified-name`

**Statement:** A test run MUST match results to tests by fully qualified name.

**Rationale:** A result file names each test with its namespace, its containing types joined with dots, and its method, without data-row arguments. A renamed test then matches nothing instead of matching another test's same-named method, so the rename fails loudly instead of lingering as an awaited run. Short-name matching hides the rename behind a passing name it no longer owns.

**Example:** Renaming a submit-expense test leaves the old qualified name with no matching result, and the scenario shows unproven until the new name runs and passes.

**Reviewed for:** Which test results match outside fully qualified names, or carry data-row arguments? Look at the test result files.

### Derive Expected Values from the Scenario

**ID:** `litespec/backend/testing#derive-expected-values-from-the-scenario`

**Statement:** A test MUST take its expected values only from the Then of the scenario it proves.

**Rationale:** Expected values read from the code under test restate what the code does, so the test passes with the code whatever the specification says. The scenario's Then holds the expected ending, and the test asserts exactly that ending. The agent obligation behind this rule lives in `litespec/core/agent#take-expected-values-from-the-scenario`, with held-back example rows checked after implementation under `litespec/core/agent#verify-refusals-and-calculations-against-held-back-examples`.

**Example:** A scenario whose Then says the expense is submitted asserts the submitted state and the returned identifier from the scenario text. The test author reads no handler code to learn the expected values.

**Reviewed for:** Which test assertions repeat values outside the Then of their scenarios? Look at each assertion beside its scenario text.

### Use Evidence Rather Than One Coverage Target

**ID:** `litespec/backend/testing#use-evidence-rather-than-one-coverage-target`

**Statement:** A repository MUST NOT treat one coverage percentage as the definition of sufficient testing.

**Rationale:** A single coverage percentage rewards tests that execute lines without asserting behavior: a suite reaches the target while proving no scenario. Coverage stays useful as review input, so the project collects line and branch numbers without treating any value as sufficient. The evidence is scenario proof, domain edge cases, integration boundaries, security behavior, and architecture rules, with mutation results on changed lines under `litespec/backend/testing#refuse-surviving-mutants-on-changed-lines`.

**Reviewed for:** Which build gates name a coverage percentage as sufficient? Look at the build gates beside the coverage numbers.

### Share One Production-Faithful Integration Harness

**ID:** `litespec/backend/testing#share-one-production-faithful-integration-harness`

**Statement:** The integration suite MUST own one database container fixture, one application host factory, and one database isolation helper.

**Rationale:** One fixture, one host factory, and one reset helper give every integration test the same database and configuration. A container per test group multiplies the run time by the group count, and it buys no isolation that a cloned database lacks. The fixture starts the pinned PostgreSQL once per test run, and it supplies each test group its connection string before the host builds.

One harness binds the integration suite. An activated BDD extension brings its own host for the acceptance suite, because the two suites run different lifetimes. That second host reuses this fixture's container and reset helper rather than starting another database.

**Reviewed for:** Which integration suites miss the one container fixture, host factory, or isolation helper, or hold a second of one? Look at the integration suite.

## Conventions

### Refuse Surviving Mutants on Changed Lines

**ID:** `litespec/backend/testing#refuse-surviving-mutants-on-changed-lines`

**Default:** Gate each change on the project's recorded mutation threshold over its changed lines, starting at zero surviving mutants; each equivalent mutant is recorded in an exception of kind `gap` covering the mutation finding.

**Replacement:** A project records its own mutation threshold and gate in its testing documentation.

**Rationale:** Mutation testing seeds small faults into changed lines and runs the suite: each killed mutant is a fault the tests catch, and each survivor is a fault they miss. The gate counts survivors on changed lines only, so every change arrives proven. A project-wide percentage confounds suite size with strength, and a full nightly run serves as a trend rather than a gate. An equivalent mutant preserves behavior by construction, so no test kills it; the exception records that judgement with its owner and expiry instead of blocking the change forever.

**Example:** A change touching the receipt check runs mutation over those lines with the scenario suite. Survivors at or below the recorded threshold let the change land; the rest send the change back for a stronger assertion, or into an exception when the mutant is equivalent.

**Reviewed for:** Which changes landed with survivors above the recorded threshold and no covering exception? Look at the mutation report beside the exception records.

### Prove Every Calculation with Property Tests

**ID:** `litespec/backend/testing#prove-every-calculation-with-property-tests`

**Default:** Prove every calculation rule with property tests seeded from its scenario examples at size large; property tests stay optional at size small.

**Replacement:** A project records its own calculation-proof technique in its testing documentation.

**Rationale:** An examples table holds the debated rows, and property tests range beyond them: each generated input checks the same stated property the rows illustrate. Seeding from the table ties the properties to reviewed values instead of invented ones. The table itself lives on a scenario proving the rule, as `litespec/core/domain-model#tabulate-every-calculation-with-examples` requires.

**Example:** A rounding rule with three reviewed rows seeds a property test asserting the rounded total against the stated bound for each generated input.

**Reviewed for:** Which calculation rules at size large lack property tests seeded from their scenario examples? Look at the calculation rules beside their property test files and the declared project size.

### Prove Every Aggregate with a Model-Based Test

**ID:** `litespec/backend/testing#prove-every-aggregate-with-a-model-based-test`

**Default:** Prove every aggregate with a model-based test generated from its declared transitions at size large; model-based tests stay optional at size small.

**Replacement:** A project records its own lifecycle-proof technique in its testing documentation.

**Rationale:** The transitions table declares every allowed move between states, and a model-based test walks generated sequences through that model: each walk checks that allowed moves succeed and rejected moves refuse. Generating from the declared table keeps the model and the test in agreement by construction. The table lives on the aggregate page, as `litespec/core/domain-model#connect-every-state-through-transitions` requires.

**Example:** An expense test generates walks across submitted, approved, rejected, and reimbursed, checking each move against the declared transitions. A walk attempting reimbursement from submitted ends in the refusal the transitions table allows.

**Reviewed for:** Which aggregates at size large lack a model-based test walking their declared transitions? Look at the aggregates beside their model-based test files and the declared project size.

### Mirror Production Module Names

**ID:** `litespec/backend/testing#mirror-production-module-names`

**Default:** Mirror the production module and aggregate folder names inside each test project.

**Replacement:** A project names its test tree shape in its testing documentation.

**Rationale:** Mirrored folders let a reader find the tests for a module without searching. Test projects stay exempt for harness and flat files: a `Support` folder and flat Architecture test files need no production counterpart.

**Example:** The test trees mirror the production module folders.

```text
Domain tests/
  Posts/
Application tests/
  Posts/
    CreateDraft/
    GetPost/
Integration tests/
  Posts/
  Support/
Architecture tests/
```

Production modules keep the same folder names in every test tree, apart from the exempt harness files.

**Reviewed for:** Which test trees break the production module folders outside the exempt harness files? Look at each test tree beside production.

### Name Tests by Observable Behavior

**ID:** `litespec/backend/testing#name-tests-by-observable-behavior`

**Default:** Name a test `{MethodOrOperation}_{Condition}_{ExpectedResult}` and its class after the production type it covers.

**Replacement:** A project names its test naming shape in its testing documentation.

**Rationale:** A failing test name is often the first thing read in a broken build. A name that states operation, condition, and expected result identifies the broken behavior without opening the file, while `Test1` or `HappyPath` describes the author's intent rather than the behavior that failed.

**Reviewed for:** Which test names miss operation, condition, or expected result, or which classes miss their production type? Look at the test names.

### Use Builders for Valid Defaults

**ID:** `litespec/backend/testing#use-builders-for-valid-defaults`

**Default:** Create a test data builder that produces valid state by default and exposes business-named customization.

**Replacement:** A project names its test data shape in its testing documentation.

**Rationale:** Builders that start from valid state keep each test focused on the one value it changes. Constructing state by bypassing Domain methods is reserved for a persistence compatibility test, because only that test needs state the Domain would refuse.

**Reviewed for:** Which test data builders start outside valid state, or customize without business names? Look at each builder.

### Keep Assertions Focused

**ID:** `litespec/backend/testing#keep-assertions-focused`

**Default:** Assert only the state, event, call, response, or error that the test covers.

**Replacement:** A project names its assertion scope in its testing documentation.

**Rationale:** A focused assertion fails only when the behavior it covers breaks. A broad object snapshot fails for an unrelated field addition, which trains readers to update it without reading it.

**Reviewed for:** Which test bodies assert outside their named outcome, or snapshot whole objects? Look at each test body.

## Reference Example

This informative example demonstrates `litespec/backend/testing#test-domain-in-isolation`, `litespec/backend/testing#test-application-coordination`, and `litespec/backend/testing#test-http-integration-through-the-real-application-host`.

A Domain test for publishing a draft post uses explicit inputs with no substitutes. An Application test for creating a draft substitutes the post repository and the clock, then asserts what was loaded, staged, and returned. An integration test for the same use case runs through the real host against the pinned PostgreSQL.
