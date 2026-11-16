# Frontend Testing

**Stack:** any
**Concern:** tests

Frontend tests prove behavior at the cheapest level that represents the risk. Pure logic takes unit tests, navigation and integration take browser suites against a running deployment, and every address takes the route check.

This page states the test kinds and the rules for proving scenarios and using mocks. Frontend developers and agents apply it whenever they add or change a test.

## Concepts

### Test Kinds

Each kind runs in one place and proves one thing. A scenario is one worked example that leads from a situation to one named result, written in `given`, `when`, `then`. The [Project Model](../core/model.md) defines both record types.

| Kind | Where it runs | What it proves |
|:---|:---|:---|
| Logic | Unit runner beside the module | A mapping, a computed window, a state decision |
| Structure | Unit runner beside the suite | The module tree mirrors the backend |
| Route contract | Browser suite against a running stack | Every route renders one floorplan and passes the browser checks |
| Evidence | Browser suite against a running stack | A use case's scenarios, with each declared refusal provoked by name |

### A Suite Assumes a Running Deployment

A browser suite needs a deployment, and nothing in the suite starts one, so the same suite runs unchanged against a developer's stack and against a review deployment.

### Reaching a Screen

A browser test reaches a screen when a `page.goto` literal or template in the test matches that screen's `address` template. Click-through navigation inference is out of scope.

In the same test, the test confirms a scenario when an assertion holds the refusal `code` or the success result's declared text or code, and the reached screen offers the scenario's use case. A flow test therefore uses one `page.goto` for each screen where it proves a scenario.

### Element Queries

Element queries follow the [Testing Library guiding principles](https://testing-library.com/docs/guiding-principles): query by role, label, name, or visible text before reaching for a test identifier.

### Related Rules

Four related rules live on other pages:

- `litespec/core/release#run-frontend-release-checks` owns the gates a changed frontend runs.
- `litespec/frontend/structure#place-tests-at-their-ownership-boundary` owns the test layout.
- `litespec/frontend/ui#name-the-evidence-for-each-ui-change` owns the evidence a UI change runs.
- `litespec/quality/testing#use-one-mock-rule-for-every-stack` owns the one mock rule every stack follows.

## Standards

### Match Test Level to Risk

**ID:** `litespec/frontend/testing#match-test-level-to-risk`

**Strength:** required

**Statement:** A frontend MUST use a logic test for pure logic, a browser specification for navigation and integration, and the route contract for every address.

**Rationale:** A browser run costs seconds and a deployment; a unit test costs milliseconds and nothing else. Proving pure logic through the browser slows the suite without strengthening it, while proving navigation with unit tests proves nothing about the running page. Matching the level to the risk proves each risk at the cheapest boundary that represents it.

**Reviewed for:** Does all pure logic take a logic test, all navigation and integration a browser specification, and every address the route contract? Check the test files beside the modules and under the tests roots.

### Mark Each Browser Test with the Scenario It Proves

**ID:** `litespec/frontend/testing#mark-each-browser-test-with-the-scenario-it-proves`

**Strength:** required

**Statement:** A browser test that proves a scenario MUST carry a `proves` annotation naming that scenario.

**Rationale:** A title tag claims proof without anything reading it, so the builder cannot count it. The written annotation is the link the builder counts: it names the lowest record the test verifies, exactly as a backend test mark does. The annotation is built by the `proves` helper the generated records file exports. A wrong identifier fails the consumer's type check against the generated union, and `litespec model check` reports it as `mark-unknown` where no type check ran.

**Example:** The browser specification at `apps/web/tests/expenses/list-expenses.spec.ts` in the Claimwick example carries `proves("scenario/list-expenses.listing-own-expenses-succeeds")`. That mark makes the specification the evidence for the listing scenario.

**Checked by:** `litespec model check` reports `mark-unknown` (level S) for a `proves` annotation naming no record a test may prove, or naming a flow.

### Reach a Screen That Offers the Scenario's Use Case

**ID:** `litespec/frontend/testing#reach-a-screen-that-offers-the-scenarios-use-case`

**Strength:** required

**Statement:** A browser test that proves a scenario MUST reach a screen that offers the scenario's use case by a `page.goto` literal or template matching that screen's `address`.

**Rationale:** A test that only clicks through navigation names no screen where it proved the scenario, so proof becomes a claim instead of exercised behavior. Driving the use case through the page a person uses keeps proof on the exercised path. Matching the `page.goto` target against the screen `address` templates produces the deterministic reach the reader computes.

**Example:** The evidence specification opens `/expenses`, the address of `screen/claimwick-app.expense-list`, which offers `use-case/spending.list-expenses`. That navigation is the reach the check reads.

**Checked by:** `litespec model check` reports `proves-unreached` (level S) for a browser test whose `page.goto` targets match no screen offering the claimed scenario's use case.

### Assert the Result a Scenario Ends In

**ID:** `litespec/frontend/testing#assert-the-result-a-scenario-ends-in`

**Strength:** required

**Statement:** A browser test that proves a scenario MUST assert the result the scenario ends in by its refusal code or by the success result's declared text or code.

**Rationale:** Reaching the screen shows only that the page opened; the assertion shows the ending the result promises. Holding the refusal code or the success text in the same test ties the proof to the named result. A test that only reads a money-moving use case does not show its completion, so the test submits it.

**Example:** The browser specification for `scenario/list-expenses.listing-own-expenses-succeeds` opens the expense list on `screen/claimwick-app.expense-list` and asserts the expenses table: the listed ending the success result `result/list-expenses.listed` promises.

**Checked by:** `litespec model check` reports `proves-unreached` (level S) for a browser test reaching the screen whose assertions in the same test hold neither the refusal code nor the success result's declared text or code.

### Assert Each Declared Refusal by Its Code

**ID:** `litespec/frontend/testing#assert-each-declared-refusal-by-its-code`

**Strength:** required

**Statement:** An evidence specification for a use case declaring a refusal MUST reach that refusal and assert it by its result's code.

**Rationale:** A refusal the test leaves unreached renders where no reader looks: in a log, or nowhere at all. Reaching it and asserting it by its result's code proves two things at once: the page renders the refusal, and the code the page shows is the code the result declares. The scenario records the situation that reaches it, so the test also documents how the refusal is provoked.

**Example:** The refusal alert carries the code, so the assertion names it.

**Reviewed for:** Does every declared refusal get reached and asserted by result code in some evidence specification? Check the evidence specifications.

### Prove Each Scenario at One Boundary

**ID:** `litespec/frontend/testing#prove-each-scenario-at-one-boundary`

**Strength:** required

**Statement:** A scenario MUST carry one `proves` mark, on the test at the cheapest boundary that represents its risk.

**Rationale:** Marking both boundaries on one scenario makes the slower test an unneeded duplicate, and doubles the work when the scenario changes. An API test proves a business outcome faster and more reliably than a browser test, and a browser test is the only proof that a page offers the use case and renders its result. A scenario whose risk is the outcome is proved at the API, and a scenario whose risk is what a person sees is proved in the browser.

**Example:** `scenario/list-expenses.listing-own-expenses-succeeds` is proved by the browser specification at `apps/web/tests/expenses/list-expenses.spec.ts`, the cheapest boundary that shows what a person sees on the expense list. A scenario whose risk is a business outcome alone is proved at the API boundary instead.

**Checked by:** `litespec model check` reports `proves-duplicate` (level A) for a scenario with `proves` marks on more than one test.

### Name the Flow a Flow Test Covers

**ID:** `litespec/frontend/testing#name-the-flow-a-flow-test-covers`

**Strength:** required

**Statement:** A browser test that drives several screens MUST carry a `flow` annotation naming the flow and a `proves` mark for each scenario it drives and asserts.

**Rationale:** A flow is not a proof target: it is achieved through use cases, so a flow test proves the scenarios it drives, and flow coverage is computed from them. The `flow` annotation tells a reader which flow the test covers without claiming that the flow itself is proven. Reworded by [decision/0049-literal-flow-words-concrete-consequences](decision/0049-literal-flow-words-concrete-consequences).

**Example:** The submit-and-reimburse flow test carries `flow: flow/submit-and-reimburse` and a `proves` mark for each scenario it drives and asserts on each screen it reaches.

**Reviewed for:** Does every browser test that drives several screens carry a `flow` annotation naming the flow and a `proves` mark for each scenario it drives and asserts? Check the browser tests and their annotations.

### Test Observable States

**ID:** `litespec/frontend/testing#test-observable-states`

**Strength:** advisory

**Statement:** A frontend test SHOULD assert observable behavior in each state applicable to what the test covers: loading, empty, error, forbidden, not-found, pending, validation, and success.

**Deviation:** A logic test asserts only the states its level reaches.

**Rationale:** An assertion on internal state passes while the rendered page stays broken, because nothing ties the variable to what the reader sees. Asserting observable behavior in each state the test's situation reaches ties the test to what the reader sees instead. The scenario states which states its situation reaches, from loading and empty through error, forbidden, not-found, pending, validation, and success. The project records each state as a term with labels per language.

**Reviewed for:** Does every test assert observable behavior, in each state its situation reaches, instead of internal state? Check the test assertions.

### Use Network Mocks for Component Tests

**ID:** `litespec/frontend/testing#use-network-mocks-for-component-tests`

**Strength:** required

**Statement:** A component or logic test that needs HTTP MUST serve its answers through network mocks generated from the published contract.

**Rationale:** A mocked client replaces the generated code with a guess at what it returns, so the test proves the mock rather than the component. A network mock generated from the published contract answers the same HTTP request the browser would send instead. The generated client stays in the path it takes in production, and the component is tested through the client the page actually uses.

**Example:** The workspace keeps one mock-generation command that regenerates them from the published OpenAPI document.

**Reviewed for:** Does every component and logic test that needs HTTP answer through contract-generated network mocks, with the generated client in the path? Check the component tests and the mock generation.

### Isolate Browser Tests

**ID:** `litespec/frontend/testing#isolate-browser-tests`

**Strength:** required

**Statement:** A browser test MUST create or identify its own data, its own authentication context, and its own expected state.

**Rationale:** A test that depends on execution order or on leftover data fails for reasons unrelated to the change under review, and passes only when the suite runs in one order. Creating its own data, its own authentication context, and its own expected state makes each test runnable alone and in any order. A test uses no seeded record; it creates the record it needs.

**Reviewed for:** Does every browser test create or identify its own data, authentication context, and expected state? Check the browser specifications.

### Prove the Module Mirror in a Structure Test

**ID:** `litespec/frontend/testing#prove-the-module-mirror-in-a-structure-test`

**Strength:** required

**Statement:** A frontend calling the API MUST hold a test asserting that its module tree resolves to the backend use-case folders and names no refused folder.

**Rationale:** Without a check, the backend adds or renames a use case and the frontend tree silently stops matching, so the translation finds nothing. The module mirror is the claim that makes a specification findable from a folder path: translating three names finds the code. A test that asserts the mirror on every build, and names no refused folder, keeps the translation true. The same test can also assert no import between use case folders or between modules, which no lint rule checks.

**Example:** The structure check resolves each folder name against the backend's module tree, as the Vitest structure test on [Web Testing](web/testing.md) shows.

**Reviewed for:** Does the structure test resolve every module folder to a backend use-case folder, and does it name no refused folder? Check the structure test.

### Run Browser Checks Through the Command Line

**ID:** `litespec/frontend/testing#run-browser-checks-through-the-command-line`

**Strength:** required

**Statement:** A browser suite MUST run through the command line, addressable per route and per file.

**Rationale:** A check that runs only inside an editor plugin or a proxy runner leaves the next investigator with no way to repeat it. The command line is the interface every runner shares: the developer, the drill, and the integration job all invoke the same command. Because the suite is addressable per route and per file, it also runs just the check under review instead of the whole suite.

**Reviewed for:** Does every browser suite run through the command line, addressable per route and per file? Check the suite runner and the CI invocation.

## Conventions

### Keep Test Support Narrow

**ID:** `litespec/frontend/testing#keep-test-support-narrow`

**Strength:** default

**Default:** Limit test support to render helpers for required providers, to one API helper that every specification shares, and to the locator helpers.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** Test support grows easily into render frameworks, data builders, and custom runners: a second application with its own untested surface. Limiting it to render helpers for required providers, one shared API helper, and locator helpers keeps it small. A change to the actor header or the idempotency key is then made in the fixtures rather than in every specification.

**Reviewed for:** Does every test support file hold only render helpers, one shared API helper, and locator helpers? Check the test support files.

### Read the Deployment for Its Identifiers

**ID:** `litespec/frontend/testing#read-the-deployment-for-its-identifiers`

**Strength:** default

**Default:** Read a record identifier a browser test needs from the running deployment, and fall back to an absent identifier when the deployment holds none.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** A hard-coded record identifier passes against the deployment it was copied from and fails against the next deployment, where that record does not exist. Reading the identifier from the running deployment keeps the test valid wherever it runs. Falling back to an absent identifier still visits the route as a not-found page, which is a state a reader can reach.

**Reviewed for:** Does every browser test read the record identifiers it needs from the running deployment instead of hard-coding them? Check the browser specifications.

## Reference Example

This informative example demonstrates `litespec/frontend/testing#match-test-level-to-risk`, `litespec/frontend/testing#assert-each-declared-refusal-by-its-code`, and `litespec/frontend/testing#isolate-browser-tests`.

A withdrawal window is a pure function, so a logic test beside it proves it. A reader reaches a redelivery dialog only through a record, so an evidence specification proves it. That specification creates the order it needs and asserts the refusal by its result code.
