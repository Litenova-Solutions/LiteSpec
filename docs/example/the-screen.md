# The Claimwick Screen

## Intent

The expenses screen is the one screen of Claimwick's web app that the example builds. It shows an employee their own expenses at the address `/expenses`. This page follows that screen from its route file, through the generated API client it reads, the shared UI package it composes, and the pattern catalog that records its interface choices, with the file of each step and the check the model runs over it.

## The Route

The screen is `apps/web/src/routes/expenses/+page.svelte`. The route folder names the address, not the use case, and it is the only route folder in the example. Its module script carries the screen's mark, which names the record it implements:

```svelte
<script module lang="ts">
  import type { LiteSpecMark } from "#lib/app/litespec-records.ts";

  export const litespec = { implements: "screen/claimwick-app.expense-list" } as const satisfies LiteSpecMark;
</script>
```

The screen record is `.litespec/behavior/entry-points/claimwick-app/screens/expense-list.yaml`, with the address `/expenses` and the floorplan `list-page`. The route file holds no reusable logic. It imports the client and the shared floorplan, and it renders the rows it gets back.

## The Use Case Code

The list itself is a module, `apps/web/src/lib/modules/spending/expenses/list-expenses/litespec.ts`. Its mark names `use-case/spending.list-expenses`, so the module folder mirrors the backend use case path in kebab case. The record it implements is [the List expenses use case](modules-and-records.md#the-use-cases).

## The Generated Client

The screen does not write its own request. The client in `packages/api/src/generated/sdk.gen.ts` is generated from the published contract, and the `listExpenses` operation in that file calls `GET /api/expenses`:

```ts
export const listExpenses = <ThrowOnError extends boolean = false>(options: Options<never, ThrowOnError> = {}) =>
  (options.client ?? client).get<ListExpensesResponse, unknown, ThrowOnError>({
    url: "/api/expenses",
    ...options,
  });
```

The response type `ListExpensesResponse` holds one array, `expenses`, of `ExpenseRow` values, and both live in `packages/api/src/generated/types.gen.ts`. A change to the contract regenerates this file, and the screen does not copy the contract.

The route calls `listExpenses` in its script and passes the result to the table through the `ListPage` floorplan. The example route has no separate load file, so it does not prefetch the list before rendering. A reader who compares it with the frontend rendering rules sees that gap: the rules ask for a load that the example does not yet write.

## The Shared UI Package

The screen composes `@claimwick/ui`, which lives in `packages/ui`. The route imports `ListPage` and `DataTable` from `@claimwick/ui/floorplans`. The floorplan file `packages/ui/src/floorplans/list-page.svelte` is exported through `packages/ui/src/floorplans/index.ts`, and the table is `packages/ui/src/components/data-table.svelte`.

The package pins its baseline in `packages/ui/ui-source-lock.yaml`. The lock names the generator `shadcn-svelte` at CLI version `1.7.0`, and it lists the components the package holds with their status. The design contract at `packages/ui/DESIGN.md` names the two patterns the package allows, `list-page` and `entity-page`, and the screen uses `list-page`.

## The Browser Proof

The browser test `apps/web/tests/expenses/list-expenses.spec.ts` proves the scenario the list use case declares. Its `proves` call names `scenario/list-expenses.listing-own-expenses-succeeds`, and the test opens `/expenses`, checks the table named "Your expenses, newest first" and waits for the text "The expenses are listed." The route suite `apps/web/tests/routes/every-route.spec.ts` is the frontend's route check, and the project file names it as `routeSuite`.

## The Pattern Catalog

The screen starts from patterns the project records, not from a choice made in a file. The catalog at `.litespec/implementation/patterns/catalog.yaml` holds two patterns:

- Orders list page asks how a reader finds one order among many. Its options are a filterable table and searchable cards. The decision is `filter-table` with the source `recommended`.
- Table sorting asks how a reader reorders the rows of a table. Its options are a sort control in each column header and a toolbar sort. The decision is `column-header-sort` with the source `recommended`.

A decision whose source is `recommended` stands until an owner chooses otherwise. Both decisions in the example are recommendations, so no owner has chosen an option yet.

## What the Model Checks

`litespec model check --root examples/claimwick` runs the controlled UI pass over the package, its source lock, its design contract and the one frontend, `web`, which declares `platform: svelte`. The full result against the example, copied from the run, is:

```text
.litespec/behavior/modules/spending/expenses/approve-expense.yaml:1 [covered A] [use-case-unreached] Use case 'use-case/spending.approve-expense' is reached through 'entry-point/claimwick-app', but no screen calls it.
.litespec/behavior/modules/spending/expenses/approve-expense.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/spending.approve-expense' has scenarios with no passing proof.
.litespec/behavior/modules/spending/expenses/erase-personal-data.yaml:1 [covered A] [use-case-unreached] Use case 'use-case/spending.erase-personal-data' is reached through 'entry-point/claimwick-app', but no screen calls it.
.litespec/behavior/modules/spending/expenses/erase-personal-data.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/spending.erase-personal-data' has scenarios with no passing proof.
.litespec/behavior/modules/spending/expenses/export-expenses.yaml:1 [covered S] [use-case-endpoint-missing] Use case 'use-case/spending.export-expenses' is reached through 'entry-point/claimwick-app', but no endpoint carries its mark.
.litespec/behavior/modules/spending/expenses/export-expenses.yaml:1 [covered S] [use-case-unimplemented] Use case 'use-case/spending.export-expenses' has no handler, reaction or job.
.litespec/behavior/modules/spending/expenses/export-expenses.yaml:1 [covered A] [use-case-unreached] Use case 'use-case/spending.export-expenses' is reached through 'entry-point/claimwick-app', but no screen calls it.
.litespec/behavior/modules/spending/expenses/export-expenses.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/spending.export-expenses' has scenarios with no passing proof.
.litespec/behavior/modules/spending/expenses/list-expenses.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/spending.list-expenses' has scenarios with no passing proof.
.litespec/behavior/modules/spending/expenses/refuse-expense.yaml:1 [covered A] [use-case-unreached] Use case 'use-case/spending.refuse-expense' is reached through 'entry-point/claimwick-app', but no screen calls it.
.litespec/behavior/modules/spending/expenses/refuse-expense.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/spending.refuse-expense' has scenarios with no passing proof.
.litespec/behavior/modules/spending/expenses/reimburse-expense.yaml:1 [covered A] [use-case-unreached] Use case 'use-case/spending.reimburse-expense' is reached through 'entry-point/claimwick-app', but no screen calls it.
.litespec/behavior/modules/spending/expenses/reimburse-expense.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/spending.reimburse-expense' has scenarios with no passing proof.
.litespec/behavior/modules/spending/expenses/submit-expense.yaml:1 [covered A] [use-case-unreached] Use case 'use-case/spending.submit-expense' is reached through 'entry-point/claimwick-app', but no screen calls it.
.litespec/behavior/modules/spending/expenses/submit-expense.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/spending.submit-expense' has scenarios with no passing proof.
.litespec/behavior/modules/spending/policies/change-receipt-threshold.yaml:1 [covered A] [use-case-unreached] Use case 'use-case/spending.change-receipt-threshold' is reached through 'entry-point/claimwick-app', but no screen calls it.
.litespec/behavior/modules/spending/policies/change-receipt-threshold.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/spending.change-receipt-threshold' has scenarios with no passing proof.
.litespec/behavior/modules/spending/receipts/attach-receipt.yaml:1 [covered A] [use-case-unreached] Use case 'use-case/spending.attach-receipt' is reached through 'entry-point/claimwick-app', but no screen calls it.
.litespec/behavior/modules/spending/receipts/attach-receipt.yaml:1 [covered A] [use-case-unverified] Use case 'use-case/spending.attach-receipt' has scenarios with no passing proof.
.litespec/intent/journeys/submit-and-reimburse.yaml:1 [covered G] [step-screen-missing] Journey step 'journey/submit-and-reimburse#s1' is served by a browser or device use case but by no screen.
.litespec/intent/journeys/submit-and-reimburse.yaml:1 [covered G] [step-screen-missing] Journey step 'journey/submit-and-reimburse#s2' is served by a browser or device use case but by no screen.
.litespec/intent/journeys/submit-and-reimburse.yaml:1 [covered G] [step-screen-missing] Journey step 'journey/submit-and-reimburse#s3' is served by a browser or device use case but by no screen.
.litespec/intent/journeys/submit-and-reimburse.yaml:1 [covered G] [step-screen-missing] Journey step 'journey/submit-and-reimburse#s4' is served by a browser or device use case but by no screen.
.litespec/intent/journeys/submit-and-reimburse.yaml:1 [covered G] [step-screen-missing] Journey step 'journey/submit-and-reimburse#s6' is served by a browser or device use case but by no screen.
.litespec/intent/journeys/submit-and-reimburse.yaml:1 [covered G] [step-screen-missing] Journey step 'journey/submit-and-reimburse#s7' is served by a browser or device use case but by no screen.
.litespec/intent/journeys/submit-and-reimburse.yaml:1 [covered G] [step-screen-missing] Journey step 'journey/submit-and-reimburse#s8' is served by a browser or device use case but by no screen.
0 findings, 0 blocking, 26 covered.
``` The check reports no finding on the screen itself. The use cases and journey steps that the screen does not reach are reported as covered by departures, as [The Model and Its Checks](model-and-checks.md) shows.
