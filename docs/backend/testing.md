# Backend Testing

**Stack:** any

**Concern:** tests

Backend tests prove each boundary on its own: Domain logic runs with no dependencies, use-case coordination runs through the project's own handlers, real persistence and HTTP behavior run through the production host, and structural rules run as checks over the compiled code. Each boundary gets its own suite, so fast tests stay fast and each failure points at the layer that broke.

The rules below state which suites each project size keeps and what each suite runs with, while the [Testing](../quality/testing.md) quality page states the cross-stack proof rules.

## Concepts

### Test Boundaries

Each suite proves one boundary and runs with one set of tools, so a test belongs to the suite whose boundary it proves. The table names each suite, the boundary it proves, and what it runs with.

| Suite | Proves | Runs With |
|:---|:---|:---|
| Domain | Factories, lifecycles, invariants, and value behavior | Explicit inputs only, with no database, host, container, clock, or mock |
| Application | Handler coordination: what was loaded, called, staged, and returned | The project's own handlers, with only outside providers and the clock substituted |
| Integration | Real persistence and HTTP behavior through the production host | The pinned PostgreSQL and the real application host |
| Architecture | Structural boundaries that no analyzer or `model check` finding covers | Static checks over the compiled code |
| Acceptance | End-to-end behavior for large projects | The executable BDD extension and its host |

Each suite pairs one boundary with one harness, so a new test has one home.

## Standards

### Keep the Test Suites the Size Requires

**ID:** `litespec/backend/testing#keep-the-test-suites-the-size-requires`

**Strength:** required

**Statement:** A backend codebase MUST hold exactly the test suites its size requires: architecture and integration for small, adding acceptance for large.

**Rationale:** Each suite owns one boundary and one harness, so a project that keeps exactly its suites knows where each new test belongs. A missing suite pushes its tests into another suite's harness, where they run with the wrong fixtures: an integration test in a unit harness gets no database. An extra suite with no stated boundary collects the tests nobody placed, and nobody knows what those tests prove. Unit suites stay optional at both sizes, and acceptance stays optional at size small. An acceptance suite without the executable BDD extension is an unnamed baseline suite: the project either selects the extension or names the suite for what it actually is.

**Example:** These are the suite folders:

```text
architecture-tests/
integration-tests/
acceptance-tests/
```

A small codebase holds the first two. The acceptance folder appears when the executable BDD extension activates.

**Reviewed for:** Is any required suite missing a folder, or any extra folder holding an unnamed suite? Look at the test folders beside the declared project size.

### Test Domain in Isolation

**ID:** `litespec/backend/testing#test-domain-in-isolation`

**Strength:** required

**Statement:** A Domain test MUST use explicit inputs with no database, HTTP host, container, clock, or mock.

**Rationale:** Domain behavior is deterministic: the same explicit inputs produce the same new state or the same refusal. A database, host, container, clock, or mock in a Domain test hides nondeterminism behind the call, so the same test passes and fails without a code change. The suite covers factories, every lifecycle state, allowed and rejected transitions, invariants, value equality, collections, money rules, exceptions, and raised events.

**Reviewed for:** Is any Domain test using a database, HTTP host, container, clock, or mock instead of explicit inputs alone? Look at the Domain test files.

### Test HTTP Integration Through the Real Application Host

**ID:** `litespec/backend/testing#test-http-integration-through-the-real-application-host`

**Strength:** required

**Statement:** An HTTP integration test MUST run through the real application host with the real configuration, covering Problem Details, authentication, and resource authorization.

**Rationale:** The real host is the only place where routing, serialization, authentication, and authorization are wired together. A test that bypasses the host proves the handler a second time and misses the boundary where status codes, Problem Details shapes, and policy denials actually come from. Persistence behavior runs against PostgreSQL under `litespec/backend/persistence#test-persistence-against-the-pinned-database`, and both run on the harness that `litespec/backend/testing#share-one-production-faithful-integration-harness` owns.

**Reviewed for:** Is any HTTP integration test bypassing the real host factory or production configuration, or skipping Problem Details, authentication, or resource authorization? Look at the HTTP integration tests.

### Isolate Integration State

**ID:** `litespec/backend/testing#isolate-integration-state`

**Strength:** required

**Statement:** An integration test MUST reset database state between tests through one documented strategy and depend on no test order.

**Rationale:** Two tests that share mutable business state pass or fail depending on which runs first, and the failure disappears when either test runs alone. One documented reset strategy removes that coupling, so order stops mattering. Container and host fixtures stay shared for cost, because only mutable business state leaks between tests.

The default strategy is one database per test class, cloned from a migrated template. The harness applies the schema once per run to a template database. Each test class then receives its own database cloned from that template and drops it when the class finishes. A clone takes milliseconds, so every class starts from an empty migrated schema without a second container. A project that resets tables between tests instead records that strategy in its testing documentation.

**Reviewed for:** Is any mutable business state shared across tests, or any reset strategy missing from the testing documentation? Look at the tests beside the testing documentation.

### Match Tests by Fully Qualified Name

**ID:** `litespec/backend/testing#match-tests-by-fully-qualified-name`

**Strength:** required

**Statement:** A test run MUST match results to tests by fully qualified name.

**Rationale:** Result files name each test in full: its namespace, its containing types joined with dots, and its method, without data-row arguments. A renamed test then matches nothing instead of matching another test's same-named method, so the rename fails loudly instead of lingering as an awaited run. Matching by short name hides the rename behind a passing name the test no longer owns.

**Example:** A renamed submit-expense test leaves its old qualified name with no matching result, so the use case stays coded until the new name runs and passes.

**Reviewed for:** Is any test result matched by a short name, or carrying data-row arguments? Look at the test result files.

### Share One Production-Faithful Integration Harness

**ID:** `litespec/backend/testing#share-one-production-faithful-integration-harness`

**Strength:** required

**Statement:** The integration suite MUST own one database container fixture, one application host factory, and one database isolation helper.

**Rationale:** One fixture, one host factory, and one reset helper give every integration test the same database and configuration. A container per test group multiplies run time by the group count and buys no isolation that a cloned database lacks. The fixture starts the pinned PostgreSQL once per test run and supplies each test group its connection string before the host builds.

The integration suite still owns one harness. An activated BDD extension brings its own host for the acceptance suite, because the two suites run different lifetimes, and that host reuses this fixture's container and reset helper instead of starting another database.

**Reviewed for:** Is any integration suite missing its container fixture, host factory, or isolation helper, or holding a second of one? Look at the integration suite.

## Conventions

### Prove Every Calculation with Property Tests

**ID:** `litespec/backend/testing#prove-every-calculation-with-property-tests`

**Strength:** default

**Default:** Prove every calculation rule with property tests seeded from its scenario examples at size large; property tests stay optional at size small.

**Replacement:** A project records its own calculation-proof technique in its testing documentation.

**Rationale:** An examples table holds the debated rows, and property tests range beyond them: each generated input checks the same stated property the rows illustrate. Seeding from the table ties the properties to reviewed values instead of invented ones. The table itself lives on a scenario proving the rule, as `litespec/core/domain-model#tabulate-every-calculation-with-examples` requires.

**Example:** A rounding rule with three reviewed rows seeds a property test that asserts the rounded total against the stated bound for every generated input.

**Reviewed for:** Is any calculation rule at size large missing property tests seeded from its scenario examples? Look at the calculation rules beside their property test files and the declared project size.

### Prove Every Aggregate with a Model-Based Test

**ID:** `litespec/backend/testing#prove-every-aggregate-with-a-model-based-test`

**Strength:** default

**Default:** Prove every aggregate with a model-based test generated from its declared transitions at size large; model-based tests stay optional at size small.

**Replacement:** A project records its own lifecycle-proof technique in its testing documentation.

**Rationale:** The transitions table declares every allowed move between states. A model-based test walks generated sequences through that model, checking that allowed moves succeed and rejected moves refuse. Generating from the declared table keeps the model and the test in agreement by construction. The table lives on the aggregate page, as `litespec/core/domain-model#connect-every-state-through-transitions` requires.

**Example:** An expense test generates walks across submitted, approved, refused, and reimbursed, checking each move against the declared transitions. A walk attempting reimbursement from submitted ends in the refusal the transitions table allows.

**Reviewed for:** Is any aggregate at size large missing a model-based test walking its declared transitions? Look at the aggregates beside their model-based test files and the declared project size.

### Mirror Production Module Names

**ID:** `litespec/backend/testing#mirror-production-module-names`

**Strength:** default

**Default:** Mirror the production module, aggregate, `UseCases/`, and use case folder names inside each test suite.

**Replacement:** A project names its test tree shape in its testing documentation.

**Rationale:** When test folders mirror production folders, a reader finds the tests for a module without searching. Harness and flat files stay exempt: a `Support` folder and flat Architecture test files need no production counterpart. Adapter tests sit under the adapter's `Infrastructure/` path and are exempt by kind, because they prove a scenario through an adapter that owns no use case folder.

**Example:** Each test tree mirrors the production module folders, including the `UseCases/` level:

```text
Unit tests/
  Posts/
    UseCases/
      CreateDraft/
      GetPost/
Integration tests/
  Posts/
    UseCases/
      CreateDraft/
  Support/
Architecture tests/
```

The same module names appear in every test tree, apart from the exempt harness files and adapter tests.

**Reviewed for:** Is any test tree breaking the production module folders outside the exempt harness files and adapter tests? Look at each test tree beside production.

### Name Tests by Observable Behavior

**ID:** `litespec/backend/testing#name-tests-by-observable-behavior`

**Strength:** advisory

**Default:** Name a test `{MethodOrOperation}_{Condition}_{ExpectedResult}` and its class after the production type it covers.

**Replacement:** A project names its test naming shape in its testing documentation.

**Rationale:** The failing test name is often the first thing anyone reads in a broken build. A name that states operation, condition, and expected result identifies the broken behavior without opening the file. A name such as `Test1` or `HappyPath` describes the author's intent instead of the behavior that failed, so the reader opens the file to learn what broke.

**Reviewed for:** Is any test name missing operation, condition, or expected result, or any test class missing its production type? Look at the test names.

### Use Builders for Valid Defaults

**ID:** `litespec/backend/testing#use-builders-for-valid-defaults`

**Strength:** advisory

**Default:** Create a test data builder that produces valid state by default and exposes business-named customization.

**Replacement:** A project names its test data shape in its testing documentation.

**Rationale:** A builder that starts from valid state keeps each test focused on the one value it changes. Building state by bypassing Domain methods is reserved for a persistence compatibility test, because only that test needs state the Domain would refuse.

**Reviewed for:** Is any test data builder starting outside valid state, or customizing without business names? Look at each builder.

### Keep Assertions Focused

**ID:** `litespec/backend/testing#keep-assertions-focused`

**Strength:** advisory

**Default:** Assert only the state, event, call, response, or error that the test covers.

**Replacement:** A project names its assertion scope in its testing documentation.

**Rationale:** A focused assertion fails only when the behavior it covers breaks. A broad object snapshot fails on an unrelated field addition, which trains readers to update it without reading it.

**Reviewed for:** Is any test body asserting outside its named outcome, or snapshotting whole objects? Look at each test body.

## Reference Example

This informative example demonstrates `litespec/backend/testing#test-domain-in-isolation` and `litespec/backend/testing#test-http-integration-through-the-real-application-host`.

A Domain test for publishing a draft post uses explicit inputs with no substitutes. An Application test for creating a draft runs the handler with a fixed clock and asserts what was staged and returned. An integration test covers the same use case through the real host against the pinned PostgreSQL.
