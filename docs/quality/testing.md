# Testing

**Stack:** any
**Concern:** tests

Every stack proves its Behavior records the same way: each scenario through the public boundary, each rule through its scenarios, with expected values from the scenario's Then. These are the cross-stack Proof rules, with the test projects each size requires, the one mock rule, and the mutation gate. Each stack's testing page states how that stack runs them. The page applies to a project that claims the `proof` layer; a project that claims `proof` as `own` keeps its own test practice.

## Concepts

### Scenario Proof

A scenario states one behavior the product shows. It is written in `given`, `when`, `then`: the situation, the action, and the expected ending. A result names one way a use case ends: success, or a refusal carrying a stable code.

A test proves a scenario in three parts: it carries a proves mark naming the scenario, it calls the use case through the public boundary, and it asserts the result the scenario names. A scenario may point at a derived generic result. A library scenario for borrowing a book carries its proves mark, drives the borrow use case through its message, and asserts the loan it names.

A reaction or job scenario is proved the same way: the test fires the trigger and asserts what the effects declare, the use case result, the sent notification, or the outside call. A frontend test proves a scenario the same way: it carries the proves mark, drives the scenario through the boundary it calls, and asserts the result the scenario names.

A rule claim confirms only through a scenario the same test proves. The model builder confirms each claim from the test body, and the latest test run marks it passing or failing. Test runs match results to tests by fully qualified name, so a renamed test matches nothing.

### Protected Proofs

Proofs are protected from casual edits: an agent that changes code leaves each proving test unchanged unless its scenario changed in the same change. The agent rule lives on the [agent page](../core/agent.md) as `litespec/core/agent#protect-proofs-from-implementation-edits`, which carries the whole obligation.

## Standards

### Prove Each Scenario Through the Public Boundary

**ID:** `litespec/quality/testing#prove-each-scenario-through-the-public-boundary`

**Strength:** required

**Statement:** Each scenario MUST be proved by a test carrying its proves mark, calling its use case through the public boundary, and asserting the result the scenario names.

**Rationale:** A scenario states one behavior the product shows, and the proves mark is the test claim that it shows it. A test that drives the use case through its steps, its message, or its endpoint exercises the public boundary, while a test that constructs a handler, an aggregate, or a repository reaches past it and proves nothing. The assertion names the result: the success fields for a success, the refusal code inside an assertion for a refusal.

A reaction or job scenario is proved the same way: its test fires the trigger and asserts what the effects declare, the use case result, the sent notification, or the outside call. A scenario no test proves is recorded as a gap waiver with its reason, exit, closing records, and expiry date. The latest test run marks each confirmed claim passing or failing.

**Example:** An acceptance test for submitting an expense carries the mark for `scenario/submit-expense.submitting-with-a-receipt-succeeds`, drives the submit-expense steps, and asserts the submitted expense. An integration test for the same scenario builds the submit-expense message, sends it through the host, and asserts the same result. In both, the mark names the scenario, the call crosses the public boundary, and the model builder confirms the claim from the test body.

**Reviewed for:** Does every scenario have a proving test with its mark, boundary call, and passing run? Look at the book beside the test bodies.

### Use One Mock Rule for Every Stack

**ID:** `litespec/quality/testing#use-one-mock-rule-for-every-stack`

**Strength:** required

**Statement:** A test MUST substitute only what the code under test does not own and what is not deterministic, and never the code's own repositories, handlers, or modules.

**Rationale:** Every substitute replaces behavior the test does not run with its author's guess. A test that stubs its own repositories, handlers, or modules proves the guess instead of the code. A substitute belongs only where the code under test cannot reach the real thing: an outside provider it does not control, another deployable it calls over the network such as the API a frontend calls, or a value that changes between runs such as the clock or randomness. Settled by [decision/0032-one-mock-rule-for-every-stack](decision/0032-one-mock-rule-for-every-stack).

**Example:** To test payment, the test substitutes the card provider and a fixed clock, and runs the expense handler against the real expense repository. A Claimwick web component test answers its API calls with network mocks generated from the published API document.

**Reviewed for:** Does any test double replace the project's own repositories, handlers, modules, or generated contract types instead of an outside provider or a non-deterministic value? Look at each test double and the mocks each test file imports.

### Keep the Test Projects the Size Requires

**ID:** `litespec/quality/testing#keep-the-test-projects-the-size-requires`

**Strength:** required

**Statement:** A project MUST hold each test project its declared size requires, as the testing page of each selected stack names them.

**Rationale:** The declared size decides which test projects prove the records. A project short of one has records that no boundary proves, and a project with an unnamed extra project collects tests that no page placed. Each stack names its projects on its testing page, so the required set reads in one place per stack.

**Example:** A large backend holds the architecture, integration, and acceptance projects its backend testing page names, while a small backend holds the first two.

**Reviewed for:** Is any required test project missing for the declared size, or does any test project lack a page that names it? Look at the test folders beside `size` in `.litespec/project.yaml`.

### Prove Each Rule Through Its Scenarios

**ID:** `litespec/quality/testing#prove-each-rule-through-its-scenarios`

**Strength:** required

**Statement:** A rule claim MUST confirm only through a scenario the same test proves.

**Rationale:** Every rule holds at the point its kind names: an invariant inside the aggregate, a precondition at the guard, a calculation in its pure evaluation, and a rule whose `binds` names the aggregates it spans at the state guard or the atomic use case. A scenario that walks through that point exercises the rule, so the rule claim rides on the scenario proof instead of carrying its own enforcement-point mark. A calculation rule earns its proof from a scenario carrying the examples table that `litespec/core/domain-model#tabulate-every-calculation-with-examples` requires.

**Example:** A test proves the scenario for an expense above the threshold with no receipt, and claims the receipt rule alongside it. The scenario sends the command and asserts the `spending.receipt-missing` refusal, and the rule claim confirms because the same test proves the scenario. No separate mark names the validator or the store.

**Reviewed for:** Does any rule claim stand without a proved scenario claim on the same test? Look at each rule claim.

### Derive Expected Values from the Scenario

**ID:** `litespec/quality/testing#derive-expected-values-from-the-scenario`

**Strength:** required

**Statement:** A test MUST take its expected values only from the Then of the scenario it proves.

**Rationale:** Expected values copied from the code under test restate what the code does, so the test passes with the code whatever the specification says. The Then of the scenario holds the expected ending, and the test asserts exactly that ending. The agent obligation behind this rule lives in `litespec/core/agent#take-expected-values-from-the-scenario`, with held-back example rows checked after implementation under `litespec/core/agent#verify-refusals-and-calculations-against-held-back-examples`.

**Example:** When the Then says the expense is submitted, the test asserts the submitted state and the returned identifier from the scenario text. Its author reads no handler code to learn the expected values.

**Reviewed for:** Does any test assertion repeat values from outside the Then of its scenario? Look at each assertion beside its scenario text.

### Refuse One Coverage Percentage as Sufficient Testing

**ID:** `litespec/quality/testing#refuse-one-coverage-percentage-as-sufficient-testing`

**Strength:** required

**Statement:** A repository MUST NOT treat one coverage percentage as the definition of sufficient testing.

**Rationale:** One coverage percentage rewards tests that execute lines without asserting behavior, so a suite reaches the target while proving no scenario. Coverage stays useful as review input: the project collects line and branch numbers without treating any value as sufficient. The evidence is scenario proof, domain edge cases, integration boundaries, security behavior, and architecture rules, with mutation results on changed lines under `litespec/quality/testing#refuse-surviving-mutants-on-changed-lines`.

**Reviewed for:** Does any build gate name a coverage percentage as sufficient? Look at the build gates beside the coverage numbers.

## Conventions

### Refuse Surviving Mutants on Changed Lines

**ID:** `litespec/quality/testing#refuse-surviving-mutants-on-changed-lines`

**Strength:** default

**Default:** Gate each change on the project's recorded mutation threshold over its changed lines, starting at zero surviving mutants; each equivalent mutant is recorded as a lasting waiver covering the mutation finding.

**Replacement:** A project records its own mutation threshold and gate in its testing documentation.

**Rationale:** Mutation testing seeds small faults into the changed lines and runs the suite against them. Each killed mutant is a fault the tests catch, and each survivor is a fault they miss. The gate counts survivors on changed lines only, so every change arrives proven.

A project-wide percentage confounds suite size with strength, and a full nightly run serves as a trend rather than a gate. An equivalent mutant preserves behavior by construction, so no test can kill it, and the waiver records that judgment with its reason and consequences instead of blocking the change forever.

**Example:** A change touching the receipt check runs mutation over those lines with the scenario suite. Survivors at or below the recorded threshold let the change land, and the rest send the change back for a stronger assertion, or into a waiver when the mutant is equivalent.

**Reviewed for:** Did any change land with survivors above the recorded threshold and no covering waiver? Look at the mutation report beside the waiver records.
