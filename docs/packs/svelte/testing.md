# Web Testing

**Stack:** svelte
**Concern:** tests

A Svelte frontend proves logic close to the code and proves navigation against the running system. This page is for developers who place Vitest and Playwright tests in a Svelte frontend. A test in the wrong runner proves the wrong thing, and a suite without assertions raises coverage while proving nothing.

## Concepts

### Where Each Test Runs

Each kind of test runs in one place on the Svelte stack. The table below names each kind with its runner:

| Kind | Where it runs |
|:---|:---|
| Logic | Vitest beside the module |
| Structure | Vitest beside the suite |
| Route contract | Playwright against a running stack |
| Evidence | Playwright against a running stack |

Pure logic sits beside its module in Vitest. Navigation and integration run against the running system in Playwright, and the route contract visits every address. The Claimwick expense page test runs with the real query library and replaces only the network call.

### How Suites Stay Honest

Suites that touch HTTP answer through `msw` handlers generated from the published OpenAPI document, in the browser worker and the Node server alike. The workspace keeps one mock-generation command and regenerates the handlers from the contract. The module mirror check runs as a Vitest structure test over the backend module tree.

```text
pnpm --filter @claimwick/web exec playwright test --project=routes --grep "expenses list keeps the route contract"
```

```typescript
test("every module folder names a backend use case, and no folder is named for nothing", () => {
  expect(checkModuleStructure(appRoot, applicationRoot)).toEqual([]);
});
```

```typescript
await expect(refusalFor(page, "expenses.not-found")).toBeVisible({ timeout: AFTER_ACTION });
```

The browser command above runs one route contract by name. The structure test above resolves each folder against the backend tree. The refusal helper above asserts the declared result code for a refused action.

Changed TypeScript lines run under StrykerJS in incremental mode. Calculation properties run on fast-check inside Vitest with seed cases from the examples table. Element queries follow role, label, name, or visible text first, scoped through `data-floorplan`.

## Standards

### Refuse a Test Without an Assertion

**ID:** `litespec/packs/svelte/testing#refuse-a-test-without-an-assertion`

**Strength:** required

**Applies to:** artifact: test

**Statement:** Lint MUST fail a Vitest or Playwright test that contains no `expect` assertion, through the `expect-expect` rule.

**Rationale:** A test without an assertion passes unless something throws. It raises coverage while proving nothing about the code under test. The `expect-expect` rule of the Vitest ESLint plugin reports each such test in the lint output.

**Checked by:** `pnpm eslint` reports `vitest/expect-expect` for each test that contains no assertion.

### Keep Web Test Mocks to the Stack Libraries

**ID:** `litespec/packs/svelte/testing#keep-web-test-mocks-to-the-stack-libraries`

**Strength:** required

**Applies to:** artifact: test

**Statement:** A test MUST NOT replace Svelte, SvelteKit, the query library, or the form library.

**Rationale:** Those libraries form the stack behavior the screen depends on. A stand-in for one of them tests the stand-in instead of the screen.

**Example:** The Claimwick expense page test runs against the real query library, replacing only the network call. The test proves the screen, not a copy of the stack.

**Reviewed for:** Does any test replace a library the stack keeps? Check the mocks in each test file.

## Conventions

None.
