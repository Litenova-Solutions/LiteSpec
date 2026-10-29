# Blazor Testing

## Intent

The Blazor client proves its behavior in 4 test projects, one per boundary: core tests without a browser, component tests with substituted browser calls, and a small browser suite for what only a real browser shows. This page states how it follows the testing principles in [Frontend Testing](../testing.md) and where each kind of test lives. A team that applies it while writing tests keeps fast tests fast and still proves storage loss, offline startup, and export with import.

## Concepts

### Scenarios, Results, and Known Gaps

A use case states its scenarios: executable examples leading from a given situation, through a when call of the use case, to a then asserting one named result. A test proves a scenario when its steps call the use case and assert the result. A scenario no test proves is recorded as a known gap with an owner and an end date, so the missing proof stays visible instead of silently absent.

## Standards

### Use the Four Client Test Projects

**ID:** `litespec/frontend/blazor/testing#use-the-four-client-test-projects`

**Statement:** A client solution MUST contain Domain, Application, Web, and end-to-end test projects and no other baseline test project.

**Rationale:** Each project matches one boundary of the client, so a failing test names the layer that broke. There is no API factory, no PostgreSQL container, and no database reset, because there is no server or database. A backend test project added here would pull server fixtures into a client that has nothing to run them against.

**Example:**

```text
tests/
  {ProjectName}.Domain.Tests/          invariants, closed sets, state transitions
  {ProjectName}.Application.Tests/     handlers, validators, result unions
  {ProjectName}.Web.Tests/             component rendering and interaction
  {ProjectName}.EndToEnd.Tests/        real browser flows
```

**Reviewed for:** The client solution holds Domain, Application, Web, and end-to-end test projects that run in CI.

### Prove Each Scenario with a Test

**ID:** `litespec/frontend/blazor/testing#prove-each-scenario-with-a-test`

**Statement:** Each scenario of a use case MUST be proved by an automated test whose steps call the use case and assert the result.

**Rationale:** A test that calls the use case and asserts the result proves the specification still holds after every change. The link between scenario and test is computed from the test, so it cannot claim coverage it lacks. A scenario no test proves becomes a known gap with an owner and an end date, which keeps the gap visible and assigned instead of forgotten. This realizes `litespec/frontend/testing#mark-the-scenario-a-browser-test-proves`, `litespec/frontend/testing#reach-a-screen-that-offers-the-scenarios-use-case`, and `litespec/frontend/testing#assert-the-result-a-scenario-ends-in`.

**Reviewed for:** Each scenario has a test whose steps call the use case and assert the result.

### Test the Core Without a Browser

**ID:** `litespec/frontend/blazor/testing#test-the-core-without-a-browser`

**Statement:** A Domain or Application test MUST run with no browser, JavaScript host, or storage.

**Rationale:** Domain and Application hold no browser types, so their tests need no browser, JavaScript host, or storage. A core test that needs an interop substitute proves the layering already broke: the production code reaches past its boundary. The fix belongs in the layering, not in the test setup. This realizes litespec/frontend/testing#match-test-level-to-risk.

**Reviewed for:** No Domain or Application test project references a browser or interop type.

### Replace Interop in Component Tests

**ID:** `litespec/frontend/blazor/testing#replace-interop-in-component-tests`

**Statement:** A component test MUST inject a test double for every Application interface, including interop.

**Rationale:** A component test substitutes every Application interface, including interop, so the component renders with no browser and the test asserts on user-visible output rather than markup structure. Coverage includes keyboard interaction and each declared route state, because those are the behaviors a component owns. Starting a real browser here would make the suite slow without proving more. This realizes litespec/frontend/testing#keep-mocks-at-owned-boundaries and litespec/frontend/testing#test-observable-states.

**Reviewed for:** Each component render substitutes every Application interface.

### Verify Real Browser Behavior End to End

**ID:** `litespec/frontend/blazor/testing#verify-real-browser-behavior-end-to-end`

**Statement:** A browser suite MUST cover the primary release flow, startup with storage unavailable, offline startup, and export followed by import.

**Rationale:** The primary release flow, startup with storage unavailable, offline startup, and export followed by import are the paths where a product with no server loses user work. None of them can run without a real browser: storage denial, offline caches, and file download are browser behaviors. A suite that skips them leaves the riskiest paths unproved. This realizes litespec/frontend/testing#match-test-level-to-risk.

**Reviewed for:** The browser suite covers the primary flow, storage loss, offline startup, and export with import.

### Enforce the First-Load Budget

**ID:** `litespec/frontend/blazor/testing#enforce-the-first-load-budget`

**Statement:** CI MUST measure the compressed transfer size of the published framework payload and fail above the recorded budget.

**Rationale:** The framework payload is what the visitor waits for, and only compressed transfer size measures that wait; directory size does not. The measurement names the tool that produced it and the compression it applied, because two tools reporting different numbers for one payload make a budget unreviewable. A published size-limit tool holding the budget in its configuration is the baseline. A project-owned script recording the same two facts is equally valid.

**Reviewed for:** The CI publish job fails when transfer size exceeds the recorded budget.

## Conventions

### Keep Browser-Capability Tests Honest

**ID:** `litespec/frontend/blazor/testing#keep-browser-capability-tests-honest`

**Default:** Test both the supported and unsupported paths of any feature depending on a browser capability.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** A feature that depends on a browser capability meets browsers that lack it. Testing both paths proves the supported behavior and the guidance the unsupported browser shows. The unsupported path asserts user-facing guidance, because a silent absence of behavior reads as a broken page.

**Reviewed for:** Each capability-dependent feature tests its supported and unsupported paths.

### Keep End-to-End Tests Few

**ID:** `litespec/frontend/blazor/testing#keep-end-to-end-tests-few`

**Default:** Limit the browser suite to the required flows and scenario-named interactions.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** Browser tests are slow and share one deployment, so each one costs the whole suite's runtime. The required flows and scenario-named interactions earn that cost; everything else runs faster and more precisely as a component or core test. This realizes litespec/frontend/testing#match-test-level-to-risk.

**Reviewed for:** Each browser test maps to a required flow or a scenario-named interaction.
