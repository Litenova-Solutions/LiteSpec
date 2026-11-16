# Web Testing

**Stack:** any

**Concern:** tests

Web frontends prove pure logic in Vitest beside the module under test, and navigation and integration in Playwright against the running system.

[Frontend Testing](../testing.md) states what each test kind proves. This page states where each kind runs on the web stacks: the runners, the libraries, the commands, and the lint rule that rejects a test with no assertion.

## Concepts

### Test Kinds

Each kind of test runs in one place on the web stacks:

| Kind | Where it runs |
|:---|:---|
| Logic | Vitest beside the module |
| Structure | Vitest beside the suite |
| Route contract | Playwright against a running stack |
| Evidence | Playwright against a running stack |

What each kind proves is stated on [Frontend Testing](../testing.md).

### Runners and Libraries

Pure logic runs in Vitest beside its module. Navigation and integration run in Playwright against a running stack, and the route contract visits every address, per `litespec/frontend/testing#match-test-level-to-risk`. Browser suites run through the Playwright CLI, addressable per route and per file, per `litespec/frontend/testing#run-browser-checks-through-the-command-line`:

```text
pnpm --filter @claimwick/web exec playwright test --project=routes --grep "expenses list keeps the route contract"
```

Component and logic tests that need HTTP answer through `msw` handlers generated from the published OpenAPI document, running in the browser worker and the Node server alike, per `litespec/frontend/testing#use-network-mocks-for-component-tests`. The workspace keeps one mock-generation command that regenerates them, and each stack page names the libraries tests keep, per `litespec/quality/testing#use-one-mock-rule-for-every-stack`.

The module mirror check runs as a Vitest structure test resolving each folder against the backend module tree, per `litespec/frontend/testing#prove-the-module-mirror-in-a-structure-test`:

```typescript
test("every module folder names a backend use case, and no folder is named for nothing", () => {
  expect(checkModuleStructure(appRoot, applicationRoot)).toEqual([]);
});
```

A declared refusal is reached and asserted by its result code through the refusal helper, per `litespec/frontend/testing#assert-each-declared-refusal-by-its-code`:

```typescript
await expect(refusalFor(page, "expenses.not-found")).toBeVisible({ timeout: AFTER_ACTION });
```

Changed TypeScript lines run under StrykerJS in incremental mode, per `litespec/quality/testing#refuse-surviving-mutants-on-changed-lines`. Calculation properties run on fast-check inside Vitest with seed cases from the examples table, per `litespec/backend/testing#prove-every-calculation-with-property-tests`.

Element queries follow the [Testing Library guiding principles](https://testing-library.com/docs/guiding-principles): role, label, name, or visible text first, scoped to one floorplan through its `data-floorplan` attribute.

## Standards

### Refuse a Test Without an Assertion

**ID:** `litespec/frontend/web/testing#refuse-a-test-without-an-assertion`

**Strength:** required

**Statement:** Lint MUST fail a Vitest or Playwright test that contains no `expect` assertion, through the `expect-expect` rule.

**Rationale:** A test that calls code and asserts nothing passes unless something throws: it raises coverage while proving nothing. The `expect-expect` rule of the Vitest ESLint plugin reports each such test before it runs, at no measurable cost. The rule is free and maintained under MIT.

**Checked by:** `pnpm eslint` reports `vitest/expect-expect` for each test that contains no assertion.

## Conventions

None.
