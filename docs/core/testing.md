# Backend Testing

**Stack:** any

**Concern:** tests

Backend tests prove each boundary in its own suite: Domain logic with no dependencies, use-case coordination through the project's own handlers, persistence and HTTP behavior through the production host, and structural rules as checks over compiled code. This page states which suites each project size keeps and what each suite runs with. It is for developers placing tests, and for reviewers judging whether a boundary has proof. A failure points at the layer breaking, and fast suites stay fast by running no heavier harness than their boundary needs.

## Concepts

### Test Boundaries

A test belongs to the suite whose boundary it proves. Each suite pairs that boundary with one harness. The [cross-stack proof rules](../quality/testing.md) state how tests prove scenarios:

| Suite | Proves | Runs With |
|:---|:---|:---|
| Domain | Factories, lifecycles, invariants, and value behavior | Explicit inputs alone, with no database, host, container, clock, or mock |
| Application | Handler coordination: what was loaded, called, staged, and returned | The project's own handlers, with outside providers and the clock alone substituted |
| Integration | Real persistence and HTTP behavior through the production host | The pinned PostgreSQL and the real application host |
| Architecture | Structural boundaries no analyzer or `model check` finding covers | Static checks over the compiled code |
| Acceptance | End-to-end behavior for large projects | The executable BDD extension and its host |

A small codebase keeps the Architecture and Integration suites. The Acceptance suite joins at large, running on the executable BDD extension and its host. That host reuses the integration fixture's container and reset helper instead of starting another database. Unit suites stay optional at both sizes, and acceptance stays optional at small. An acceptance suite without the executable BDD extension is an unnamed baseline suite: the project either selects the extension or names the suite for what it actually is.

Domain behavior is deterministic: the same explicit inputs produce the same new state or the same refusal. A database, host, container, clock, or mock in a Domain test hides nondeterminism behind the call, so the same test passes and fails with no code change. The suite covers factories, every lifecycle state, allowed and rejected transitions, invariants, value equality, collections, money rules, exceptions, and raised events.

The real host is where routing, serialization, authentication, and authorization are wired together. A test bypassing the host proves the handler a second time and misses the boundary where status codes, Problem Details shapes, and policy denials actually come from. An HTTP integration test covers Problem Details, authentication, and resource authorization through the real host with the real configuration.

Two tests sharing mutable business state pass or fail depending on which runs first, and the failure disappears when either test runs alone. One documented reset strategy removes that coupling, so order stops mattering. The default strategy is one database per test class, cloned from a migrated template. The harness applies the schema once per run to a template database.

Each test class then receives its own database cloned from that template and drops it when the class finishes. A clone takes milliseconds, so every class starts from an empty migrated schema without a second container. A project resetting tables between tests instead records that strategy in its testing documentation. Container and host fixtures stay shared for cost, since mutable business state alone leaks between tests.

Result files name each test in full: its namespace, its containing types joined with dots, and its method, without data-row arguments. A renamed test then matches nothing instead of matching another test's same-named method, so the rename fails loudly instead of lingering as an awaited run. Matching by short name hides the rename behind a passing name the test no longer owns.

One fixture, one host factory, and one reset helper give every integration test the same database and configuration. A container per test group multiplies run time by the group count and buys no isolation a cloned database lacks. The fixture starts the pinned PostgreSQL once per test run and supplies each test group its connection string before the host builds.

### Proving Rules and Aggregates

An examples table holds the debated rows, and property tests range beyond them: each generated input checks the same stated property the rows illustrate. Seeding from the table ties the properties to reviewed values instead of invented ones. At large, each calculation rule earns property tests seeded from its scenario examples; at small, property tests stay optional. The table itself lives on a scenario proving the rule.

The transitions table declares every allowed move between states. A model-based test walks generated sequences through that model, checking allowed moves succeeding and rejected moves refused. Generating from the declared table keeps the model and the test in agreement by construction. At large, each aggregate earns a model-based test walking its declared transitions; at small, model-based tests stay optional. An expense test generates walks across submitted, approved, refused, and reimbursed, checking each move against the declared transitions. A walk attempting reimbursement from submitted ends in the refusal the transitions table allows.

### Naming Tests and Data

The failing test name is often the first thing anyone reads in a broken build. A name stating operation, condition, and expected result identifies the broken behavior without opening the file. A name such as `Test1` or `HappyPath` describes the author's intent instead of the behavior breaking, so the reader opens the file to learn what broke. A test class carries the name of the production type it covers.

A builder starting from valid state keeps each test focused on the one value it changes. Building state by bypassing Domain methods is reserved for a persistence compatibility test, since that test alone needs state the Domain refuses.

A focused assertion fails when the behavior it covers breaks. A broad object snapshot fails on an unrelated field addition, which trains readers to update it without reading it.

## Standards

None.

## Conventions

### Mirror Production Module Names

**ID:** `litespec/core/testing#mirror-production-module-names`

**Strength:** default

**Default:** Mirror the production module, aggregate, and use case folder names inside each test suite, so acceptance tests mirror `{Module}/{Aggregates}/{UseCase}/` with no layer segment.

**Replacement:** A project names its test tree shape in its testing documentation.

**Rationale:** When test folders mirror production folders, a reader finds the tests for a module without searching.

Harness and flat files stay exempt: a `Support` folder and flat Architecture test files need no production counterpart. Adapter tests sit under the adapter's `Infrastructure/` path and are exempt by kind, since they prove a scenario through an adapter owning no use case folder.

**Example:** Each test tree mirrors the production module folders, with no layer segment:

```text
Unit tests/
  Posts/
    Posts/
      CreateDraft/
      GetPost/
Integration tests/
  Posts/
    Posts/
      CreateDraft/
  Support/
Architecture tests/
```

The same module names appear in every test tree, apart from the exempt harness files and adapter tests.

**Checked by:** `litespec model check` reports `code-placement-drift` for a test folder under `{Module}/{Aggregates}/` naming no declared aggregate or use case, and `use-case-unimplemented` for a use case with scenarios and no `Application/` folder.

## Reference Example

This informative example demonstrates `litespec/core/testing#mirror-production-module-names`.

A publishing product mirrors its Posts module in every suite: `Posts/Posts/CreateDraft/` holds the unit, application, and integration tests for that use case, with no layer segment in the path. The Architecture suite holds flat files, and the `Support/` folder holds the shared fixtures, both exempt from mirroring.
