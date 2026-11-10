# Testing

**Stack:** any
**Concern:** tests

## Intent

Every stack proves its records through the same cross-stack Proof rules: each scenario is proved through the public boundary, each rule through its scenarios, and expected values come from the scenario's Then. This page states those rules, the test projects each size requires, the one mock rule and the mutation gate, while each stack's testing page states how its stack runs them. A project that claims the `proof` layer applies this page, and a team that claims it as its own keeps its own test practice.

## Concepts

### Scenario Proof

A scenario states one behavior the product shows, written in `given`, `when`, `then`: the situation, the action, and the expected ending. A result names one way a use case ends: success, or a refusal carrying a stable code. A test proves a scenario by carrying a proves mark naming it, calling its use case through the public boundary, and asserting the result the scenario names. A scenario may point at a derived generic result, and a reaction or job scenario is proved the same way: the test fires the trigger and asserts the result of the use case the reaction or job runs. A rule claim confirms only through a scenario the same test proves. A frontend test proves a scenario the same way: it carries the proves mark, drives the scenario through the boundary it calls, and asserts the result the scenario names. Test runs match results to tests by fully qualified name, so a renamed test matches nothing. The model builder confirms each claim from the test body, and the latest test run marks it passing or failing.

### Protected Proofs

An agent that changes code leaves each proving test unchanged unless its scenario changed in the same change. The rule for agents lives on the [agent page](../core/agent.md), as `litespec/core/agent#protect-proofs-from-implementation-edits` states, and this page adds no obligation to it.

## Standards

### Prove Each Scenario Through the Public Boundary

**ID:** `litespec/quality/testing#prove-each-scenario-through-the-public-boundary`

**Strength:** required

**Statement:** Each scenario MUST be proved by a test carrying its proves mark, calling its use case through the public boundary, and asserting the result the scenario names.

**Rationale:** A scenario states one behavior the product shows, and the proves mark is the test's claim that it shows it. A test that drives the use case through its steps, its message, or its endpoint exercises the public boundary; a test that constructs a handler, an aggregate, or a repository reaches past it and proves nothing. The assertion names the result: the success fields for a success, the refusal code inside an assertion for a refusal. A reaction or job scenario is proved the same way: its test fires the trigger and asserts the result of the use case the reaction or job runs. A scenario no test proves is recorded as a departure with its reason and expiry date. The latest test run marks each confirmed claim passing or failing.

**Example:** An acceptance scenario for submitting an expense carries the mark for `scenario/submit-expense.submitting-with-a-receipt-succeeds`, drives the submit-expense steps, and asserts the submitted expense. An integration test for the same scenario constructs the submit-expense message, sends it through the host, and asserts the same result. The mark names the scenario, the call crosses the public boundary, and the model builder confirms the claim from the test body.

**Reviewed for:** Which scenarios lack a proving test with its mark, boundary call, and passing run? Look at the book beside the test bodies.

### Use One Mock Rule for Every Stack

**ID:** `litespec/quality/testing#use-one-mock-rule-for-every-stack`

**Strength:** required

**Statement:** A test MUST substitute only what the code under test does not own and what is not deterministic, and never the code's own repositories, handlers, or modules.

**Rationale:** A substitute stands in for code the test does not run, so each one replaces the project's own behavior with the author's guess of it. A test that stubs its own repositories, handlers, or modules proves the guess instead of the code. A substitute earns its place only where the code under test cannot reach the real thing: an outside provider it does not control, another deployable it calls over the network, such as the API a frontend calls, or a value that changes between runs, such as the clock or randomness.

**Example:** A payment test substitutes the card provider and a fixed clock, and runs the expense handler against the real expense repository; a Claimwick web component test answers its API calls with network mocks generated from the published API document.

**Reviewed for:** Which test doubles replace the project's own repositories, handlers, modules, or generated contract types, rather than an outside provider or a non-deterministic value? Look at each test double and the mocks each test file imports.

### Keep the Test Projects the Size Requires

**ID:** `litespec/quality/testing#keep-the-test-projects-the-size-requires`

**Strength:** required

**Statement:** A project MUST hold each test project its declared size requires, as the testing page of each selected stack names them.

**Rationale:** The size decides which test projects prove the records. A project short of one has records that no boundary proves, and a project with an unnamed extra project collects tests that no page placed. Each stack names its projects on its testing page, so the required set reads in one place per stack.

**Example:** A large backend holds the architecture, integration, and acceptance projects that its backend testing page names. A small backend holds the first two.

**Reviewed for:** Which required test project is missing for the declared size, or which test project no page names? Look at the test folders beside `size` in `.litespec/project.yaml`.

### Prove Each Rule Through Its Scenarios

**ID:** `litespec/quality/testing#prove-each-rule-through-its-scenarios`

**Strength:** required

**Statement:** A rule claim MUST confirm only through a scenario the same test proves.

**Rationale:** Each rule holds where its kind says: an invariant inside the aggregate, a precondition at the guard, a calculation in its pure evaluation, a cross-aggregate rule at the store or the reacting use case. A scenario that walks through that point exercises the rule, so the rule claim rides on the scenario proof instead of carrying its own enforcement-point mark. A calculation rule earns its proof from a scenario carrying the examples table that `litespec/core/domain-model#tabulate-every-calculation-with-examples` requires.

**Example:** A test proving the scenario for an expense above the threshold with no receipt claims the receipt rule alongside the scenario. The scenario sends the command and asserts the `spending.receipt-missing` refusal, and the rule claim confirms because the same test proves the scenario. No separate mark names the validator or the store.

**Reviewed for:** Which rule claims stand without a proved scenario claim on the same test? Look at each rule claim.

### Derive Expected Values from the Scenario

**ID:** `litespec/quality/testing#derive-expected-values-from-the-scenario`

**Strength:** required

**Statement:** A test MUST take its expected values only from the Then of the scenario it proves.

**Rationale:** Expected values read from the code under test restate what the code does, so the test passes with the code whatever the specification says. The scenario's Then holds the expected ending, and the test asserts exactly that ending. The agent obligation behind this rule lives in `litespec/core/agent#take-expected-values-from-the-scenario`, with held-back example rows checked after implementation under `litespec/core/agent#verify-refusals-and-calculations-against-held-back-examples`.

**Example:** A scenario whose Then says the expense is submitted asserts the submitted state and the returned identifier from the scenario text. The test author reads no handler code to learn the expected values.

**Reviewed for:** Which test assertions repeat values outside the Then of their scenarios? Look at each assertion beside its scenario text.

### Use Evidence Rather Than One Coverage Target

**ID:** `litespec/quality/testing#use-evidence-rather-than-one-coverage-target`

**Strength:** required

**Statement:** A repository MUST NOT treat one coverage percentage as the definition of sufficient testing.

**Rationale:** A single coverage percentage rewards tests that execute lines without asserting behavior: a suite reaches the target while proving no scenario. Coverage stays useful as review input, so the project collects line and branch numbers without treating any value as sufficient. The evidence is scenario proof, domain edge cases, integration boundaries, security behavior, and architecture rules, with mutation results on changed lines under `litespec/quality/testing#refuse-surviving-mutants-on-changed-lines`.

**Reviewed for:** Which build gates name a coverage percentage as sufficient? Look at the build gates beside the coverage numbers.

## Conventions

### Refuse Surviving Mutants on Changed Lines

**ID:** `litespec/quality/testing#refuse-surviving-mutants-on-changed-lines`

**Strength:** default

**Default:** Gate each change on the project's recorded mutation threshold over its changed lines, starting at zero surviving mutants; each equivalent mutant is recorded as a departure covering the mutation finding.

**Replacement:** A project records its own mutation threshold and gate in its testing documentation.

**Rationale:** Mutation testing seeds small faults into changed lines and runs the suite: each killed mutant is a fault the tests catch, and each survivor is a fault they miss. The gate counts survivors on changed lines only, so every change arrives proven. A project-wide percentage confounds suite size with strength, and a full nightly run serves as a trend rather than a gate. An equivalent mutant preserves behavior by construction, so no test kills it; the departure records that judgement with its reason and expiry instead of blocking the change forever.

**Example:** A change touching the receipt check runs mutation over those lines with the scenario suite. Survivors at or below the recorded threshold let the change land; the rest send the change back for a stronger assertion, or into an exception when the mutant is equivalent.

**Reviewed for:** Which changes landed with survivors above the recorded threshold and no covering exception? Look at the mutation report beside the departure records.
