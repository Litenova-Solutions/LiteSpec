# Frontend Testing

## Intent

Frontend tests prove behavior at the cheapest level that represents the risk: unit tests for pure logic, browser suites against a running deployment for navigation and integration, and one route check for every address. This page states which kind proves what, how a test proves a use case by calling it and asserting the result each scenario names, and where mocks may stand. Frontend developers and agents apply it whenever they add or change a test, so each risk is proved once at the right level instead of slowly everywhere or nowhere at all.

## Concepts

### Test Kinds

Each kind runs against a frontend and answers one question. A scenario is one worked example leading from a situation to one named result, written in `given`, `when`, `then`. Both are record types the [model page](../core/model.md) defines.

| Kind | Where it runs | What it proves |
|:---|:---|:---|
| Logic | Unit runner beside the module | A mapping, a computed window, a state decision |
| Structure | Unit runner beside the suite | The module tree mirrors the backend |
| Route contract | Browser suite against a running stack | Every route renders one floorplan and passes the browser checks |
| Evidence | Browser suite against a running stack | A use case's scenarios, with each declared refusal provoked by name |

### Suites Bring No Deployment

A browser suite needs a deployment, and nothing in the suite starts one, so the same suite runs unchanged against a developer's stack and against a review deployment.

### Reaching a Screen

A browser test reaches a screen when a `page.goto` literal or template in the test matches that screen's `address` template. A test confirms a scenario when, in the same test, an assertion holds the refusal `code` or the success result's declared text or code, and the reached screen offers the scenario's use case. Click-through navigation inference is out of scope: a journey test uses one `page.goto` per screen it proves on.

### Related Rules

Three related rules live on other pages:

- `litespec/core/release#run-frontend-release-checks` owns the gates a changed frontend runs.
- `litespec/frontend/structure#place-tests-at-their-ownership-boundary` owns the test layout.
- `litespec/frontend/ui#prove-ui-behavior-and-appearance` owns the evidence a UI change runs.

## Standards

### Match Test Level to Risk

**ID:** `litespec/frontend/testing#match-test-level-to-risk`

**Statement:** A frontend MUST use a logic test for pure logic, a browser specification for navigation and integration, and the route contract for every address.

**Rationale:** A browser run costs seconds and a deployment; a unit test costs milliseconds and nothing else. Proving pure logic through the browser makes the suite slow without making it stronger, while proving navigation with unit tests proves nothing about the running page. Logic tests take pure logic, browser specifications take navigation and integration, and the route contract covers every address, so each risk is proved at the cheapest boundary that represents it.

**Reviewed for:** Which pure logic lacks a logic test, which navigation or integration lacks a browser specification, and which address lacks the route contract? Look in the test files beside the modules and under the tests roots.

### Mark the Scenario a Browser Test Proves

**ID:** `litespec/frontend/testing#mark-the-scenario-a-browser-test-proves`

**Statement:** A browser test that proves a scenario MUST carry a `proves` annotation naming that scenario.

**Rationale:** The annotation, built by the `proves` helper the generated records file exports, names the lowest record the test verifies exactly as a backend test mark does, and a wrong identifier fails the consumer's type check against the generated union, and `litespec model check` reports it as `mark-unknown` where no type check ran. A title tag claims proof without anything reading it, so the written annotation is the link the builder counts.

**Example:** The browser specification at `apps/web/tests/expenses/list-expenses.spec.ts` in the Claimwick example carries `proves("scenario/list-expenses.listing-own-expenses-succeeds")`.

**Checked by:** `litespec model check` reports `mark-unknown` (level S) for a `proves` annotation naming no record a test may prove, or naming a journey.

### Reach a Screen That Offers the Scenario's Use Case

**ID:** `litespec/frontend/testing#reach-a-screen-that-offers-the-scenarios-use-case`

**Statement:** A browser test that proves a scenario MUST reach a screen that offers the scenario's use case by a `page.goto` literal or template matching that screen's `address`.

**Rationale:** A test proves a scenario when it drives the use case through the page a person uses, so proof follows exercised behavior rather than a claim. Matching the `page.goto` target against the screen `address` templates is the deterministic reach the reader computes; a test that only clicks through navigation names no screen it proved on.

**Example:** The evidence specification opens `/expenses`, the address of `screen/claimwick-app.expense-list`, which offers `use-case/spending.list-expenses`.

**Checked by:** `litespec model check` reports `proves-unreached` (level S) for a browser test whose `page.goto` targets match no screen offering the claimed scenario's use case.

### Assert the Result a Scenario Ends In

**ID:** `litespec/frontend/testing#assert-the-result-a-scenario-ends-in`

**Statement:** A browser test that proves a scenario MUST assert the result the scenario ends in by its refusal code or by the success result's declared text or code.

**Rationale:** Reaching the screen shows the page opened; the assertion shows the ending the result promises. Holding the refusal code or the success text in the same test ties the proof to the named result, and a money-moving use case that a test only reads leaves completion unshown, so the test submits it.

**Example:** The browser specification for `scenario/list-expenses.listing-own-expenses-succeeds` opens the expense list on `screen/claimwick-app.expense-list` and asserts the expenses table, the listed ending the success result `result/list-expenses.listed` promises.

**Checked by:** `litespec model check` reports `proves-unreached` (level S) for a browser test reaching the screen whose assertions in the same test hold neither the refusal code nor the success result's declared text or code.

### Assert Each Declared Refusal by Its Code

**ID:** `litespec/frontend/testing#assert-each-declared-refusal-by-its-code`

**Statement:** An evidence specification for a use case declaring a refusal MUST reach that refusal and assert it by its result's code.

**Rationale:** A refusal the test leaves unreached renders where no reader looks: it appears in a log, or nowhere at all. Reaching it and asserting it by its result's code proves two things at once: the page renders the refusal, and the code the page shows is the code the result declares. The scenario records the situation that reaches it, so the test documents how the refusal is provoked.

**Example:** The refusal alert carries the code, so the assertion names it.

**Reviewed for:** Which declared refusals do no evidence specifications reach and assert by result code? Look in the evidence specifications.

### Prove Each Scenario at One Boundary

**ID:** `litespec/frontend/testing#prove-each-scenario-at-one-boundary`

**Statement:** A scenario MUST carry one `proves` mark, on the test at the cheapest boundary that represents its risk.

**Rationale:** An API test proves a business outcome faster and more steadily than a browser test, and a browser test is the only proof that a page offers the use case and renders its result. When both assert one scenario, marking both makes the slower test a second witness nobody needs and doubles the work when the scenario changes. The scenario whose risk is the outcome is proved at the API, and the one whose risk is what a person sees is proved in the browser.

**Example:** `scenario/list-expenses.listing-own-expenses-succeeds` is proved by the browser specification at `apps/web/tests/expenses/list-expenses.spec.ts`, the cheapest boundary that shows what a person sees on the expense list. A scenario whose risk is a business outcome alone is proved at the API boundary instead.

**Checked by:** `litespec model check` reports `proves-duplicate` (level A) for a scenario with `proves` marks on more than one test.

### Name the Journey a Journey Test Walks

**ID:** `litespec/frontend/testing#name-the-journey-a-journey-test-walks`

**Statement:** A browser test that walks several screens MUST carry a `journey` annotation naming the journey and a `proves` mark for each scenario it drives and asserts.

**Rationale:** A journey is not a proof target: it is achieved through use cases, so a journey test proves the scenarios it drives, and journey coverage is computed from them. The `journey` annotation tells a reader which walk the test rehearses without claiming that the walk itself is proven.

**Example:** The submit-and-reimburse walk carries `journey: journey/submit-and-reimburse` and proves the scenarios it drives and asserts on each screen it reaches.

**Reviewed for:** Which browser tests that walk several screens carry a `journey` annotation naming the journey and a `proves` mark for each scenario they drive and assert? Look in the browser tests and their annotations.

### Test Observable States

**ID:** `litespec/frontend/testing#test-observable-states`

**Statement:** A frontend test MUST assert observable behavior in each state applicable to what the test covers: loading, empty, error, forbidden, not-found, pending, validation, and success.

**Rationale:** An assertion on internal state passes while the rendered page stays broken, because nothing ties the variable to what the reader sees. Asserting observable behavior in each state the test's situation reaches, from loading and empty through error, forbidden, not-found, pending, validation, and success, ties the test to the reader's view. The scenario states which states its situation reaches, so a logic test asserts only states its level reaches. The project records each state as a term with labels per language.

**Reviewed for:** Which tests assert internal state instead of observable behavior in the states their situation reaches? Look in the test assertions.

### Keep Mocks at Owned Boundaries

**ID:** `litespec/frontend/testing#keep-mocks-at-owned-boundaries`

**Statement:** A frontend test MUST NOT mock the generated contract types or an implementation-private function, nor a library its stack names.

**Rationale:** A mock is a claim about code the test no longer exercises. Mocking the generated contract types, an implementation-private function, or a library the stack names replaces the project's own contract or someone else's internals with the test author's guess of how they behave. The generated client boundary stays replaceable because it is an owned contract: the project wrote it, so the project can stand in for it.

**Reviewed for:** Which mocks replace generated contract types, private functions, or stack-named libraries? Look in the test doubles.

### Use Network Mocks for Component Tests

**ID:** `litespec/frontend/testing#use-network-mocks-for-component-tests`

**Statement:** A component or logic test that needs HTTP MUST serve its answers through network mocks generated from the published contract.

**Rationale:** A mocked client replaces the generated code with a guess of what it returns, so the test proves the mock rather than the component. A network mock generated from the published contract answers the same HTTP request the browser would send, so the generated client stays in the path it takes in production and the component is tested through the client the page actually uses.

**Example:** The workspace keeps one mock-generation command that regenerates them.

**Reviewed for:** Which component or logic tests needing HTTP answer without contract-generated network mocks, or with the generated client out of the path? Look in the component tests and the mock generation.

### Isolate Browser Tests

**ID:** `litespec/frontend/testing#isolate-browser-tests`

**Statement:** A browser test MUST create or identify its own data, its own authentication context, and its own expected state.

**Rationale:** A test that depends on execution order or on leftover data fails for reasons unrelated to the change under review, and passes only when the suite runs in one order. Creating its own data, its own authentication context, and its own expected state makes each test runnable alone and in any order. A test spends no seeded record; it creates the record it needs.

**Reviewed for:** Which browser tests depend on another test's data, authentication context, or expected state? Look in the browser specifications.

### Prove the Module Mirror in a Structure Test

**ID:** `litespec/frontend/testing#prove-the-module-mirror-in-a-structure-test`

**Statement:** A frontend calling the API MUST hold a test asserting that its module tree resolves to the backend use-case folders and names no refused folder.

**Rationale:** The module mirror is the claim that makes a specification findable from a folder path: translate three names, land on the code. Without a check, the backend adds or renames a use case and the frontend tree silently stops matching, so the translation lands nowhere. A test asserting the mirror on every build, and naming no refused folder, keeps the translation true.

**Example:** The structure check resolves each folder name against the backend's module tree.

**Reviewed for:** Which module folders does the structure test fail to resolve to backend use-case folders, and which refused folders does it name? Look in the structure test.

### Run Browser Checks Through the Command Line

**ID:** `litespec/frontend/testing#run-browser-checks-through-the-command-line`

**Statement:** A browser suite MUST run through the command line, addressable per route and per file.

**Rationale:** The command line is the interface every runner shares: the developer, the drill, and the integration job all invoke the same command. A check that runs only inside an editor plugin or a proxy runner leaves the next investigator with no way to repeat it. Addressable per route and per file, the suite also runs just the check under review instead of the whole suite.

**Reviewed for:** Which browser suites run outside the command line, or without per-route and per-file addressing? Look in the suite runner and the CI invocation.

## Conventions

### Query by Accessible Behavior

**ID:** `litespec/frontend/testing#query-by-accessible-behavior`

**Default:** Query elements by role, label, name, or visible text before reaching for a test identifier.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** A query by role, label, name, or visible text fails when the user-facing behavior breaks, which is exactly the behavior worth protecting. A test identifier survives an accessibility regression in silence, so it is the last resort rather than the first. A structural floorplan marker is not an identifier, so a query can use it to scope to one floorplan; slot content is addressed by its accessible role and name.

**Reviewed for:** Which element queries reach for a test identifier before role, label, name, or visible text? Look in the test locators.

### Keep Test Support Narrow

**ID:** `litespec/frontend/testing#keep-test-support-narrow`

**Default:** Limit test support to render helpers for required providers, to one API helper that every specification shares, and to the locator helpers.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** Test support that grows into render frameworks, data builders, and custom runners becomes a second application with its own untested surface. Limiting it to render helpers for required providers, one shared API helper, and locator helpers keeps it small: a change to the actor header or the idempotency key reaches the fixtures rather than every specification.

**Reviewed for:** Which test support files hold more than render helpers, one shared API helper, and locator helpers? Look in the test support files.

### Read the Deployment for Its Identifiers

**ID:** `litespec/frontend/testing#read-the-deployment-for-its-identifiers`

**Default:** Read a record identifier a browser test needs from the running deployment, and fall back to an absent identifier when the deployment holds none.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** A hard-coded record identifier passes against the deployment it was copied from and fails against the next, where that record does not exist. Reading the identifier from the running deployment keeps the test true wherever it runs. Falling back to an absent identifier still visits the route as a not-found page, which is a state a reader can reach anyway.

**Reviewed for:** Which browser tests hard-code a record identifier instead of reading it from the running deployment? Look in the browser specifications.

## Reference Example

This informative example demonstrates `litespec/frontend/testing#match-test-level-to-risk`, `litespec/frontend/testing#assert-each-declared-refusal-by-its-code`, and `litespec/frontend/testing#isolate-browser-tests`.

A withdrawal window is a pure function, so it takes a logic test beside it. A reader reaches a redelivery dialog only through a record, so it takes an evidence specification. That specification creates the order it needs and asserts the refusal by its code.
