# Web Structure

**Stack:** any

**Layer:** frontend-ui

## Intent

React and Svelte frontends share one folder layout: a frame holding the values shared across routes, one route per address, and modules that mirror the backend's use cases so each business area and user task sits in the same place on both sides. This page states the tree, the frame folder, the use-case marks, the generated records file, and the route tree the [React Stack](react/stack.md) and [Svelte Stack](svelte/stack.md) share, following the structure rules in [Frontend Structure](../structure.md). A team that applies it while adding files keeps every use case findable by path and every mark checkable by type.

## Concepts

### Rules Kept on the Base Page

The structure rules for thin route entries, import direction, public entry points, and test placement live on [Frontend Structure](../structure.md). The web stacks add the tree, frame, mark, records file, and route tree rules this page states.

- Thin route file: `litespec/frontend/structure#keep-a-route-entry-thin`
- Import direction: `litespec/frontend/structure#order-imports-from-routes-to-shared-packages`
- Public entry points: `litespec/frontend/structure#use-explicit-public-entry-points-for-shared-packages`
- Test placement: `litespec/frontend/structure#place-tests-at-their-ownership-boundary`

## Standards

### Use the Frontend Application Tree

**ID:** `litespec/frontend/web/structure#use-the-frontend-application-tree`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A frontend MUST use the frame, routes, and modules layout its stack page states.

**Rationale:** The frame, routes, and modules layout gives every file one predictable home: cross-route values in the frame, one address per route file or folder, and use-case code under the module that owns it. A reader moves from a use case to its code by path alone, on either stack. This realizes litespec/frontend/structure#organize-a-frontend-into-frame-routes-and-modules.

**Example:**

React:

```text
apps/{frontend}/
  src/
    app/       the frame: client configuration, session, language, permissions, formatting, shell data
    routes/    one file per address, compiled into a typed route tree
    modules/   one folder per backend module, aggregate, and use case
  tests/
    unit/      logic tests
    routes/    the route contract suite
    e2e/       evidence specifications
  package.json
  vite.config.ts
```

Svelte:

```text
apps/{frontend}/
  src/
    routes/    one folder per address, with a page per folder
    lib/
      app/       the frame: client configuration, session, language, permissions, formatting, shell data
      modules/   one folder per backend module, aggregate, and use case
  tests/
    unit/      logic tests
    routes/    the route contract suite
    e2e/       evidence specifications
  svelte.config.js
  vite.config.ts
  package.json
```

The generated route artifacts, build output, and dependency folders stay untracked.

**Reviewed for:** Which frontend trees break from the frame, routes, and modules layout their stack page states? Look in the frontend tree.

### Mark Each Use-Case Module with Its Use Case

**ID:** `litespec/frontend/web/structure#mark-each-use-case-module-with-its-use-case`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A frontend use-case folder MUST export one `litespec` constant whose `implements` names its use case, typed against the generated union of use-case identifiers.

**Rationale:** The folder path mirrors the backend use case, which tells a reader where to look but tells a tool only a resemblance. A typed constant is the frontend mark in both mark styles: a mistyped identifier fails the consumer's type check against the generated union, and `litespec model check` reports it as `mark-unknown` where no type check ran, and the route files import the module instead of restating the identifier. The calls a screen makes through the generated client remain the computed `calls` link; the mark adds the written link from the screen to the use case it serves. A query folder at `{module}/use-cases/{use-case}/` mirrors a use case with no one aggregate, so its expected mark still names `use-case/<module>.<use-case>`, from the module and use-case segments alone.

**Example:** The list-expenses module in the Claimwick web frontend exports its mark.

Svelte:

```ts
// src/lib/modules/spending/expenses/list-expenses/litespec.ts
import type { LiteSpecMark } from "#lib/app/litespec-records.ts";

export const litespec = { implements: "use-case/spending.list-expenses" } as const satisfies LiteSpecMark;
```

React:

```ts
// src/modules/spending/expenses/list-expenses/litespec.ts
import type { LiteSpecMark } from "src/app/litespec-records";

export const litespec = { implements: "use-case/spending.list-expenses" } as const satisfies LiteSpecMark;
```

**Checked by:** `litespec model check` reports `mark-missing` (level S) for a use-case folder exporting no `litespec` constant, and `mark-unknown` (level S) for one naming a use case its path does not mirror; for a folder at `{module}/use-cases/{use-case}/` the mirrored use case is the one its module and use-case segments name, with no aggregate segment.

### Generate the Records File into the Frame

**ID:** `litespec/frontend/web/structure#generate-the-records-file-into-the-frame`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A frontend's generated `litespec-records.ts` MUST sit in its frame folder and hold only the use cases and screens of that frontend's own entry points.

**Rationale:** The records file is a cross-route value every module and route imports, which is what the frame holds on both stacks: `src/app/` on React and `src/lib/app/` on Svelte, so no stack-specific folder is needed. A union holding every browser use case of the project would let one frontend mark a use case only another frontend reaches, and the type check would pass; a union per frontend makes that mistake a type error. The file exports the `UseCaseId` and `ScreenId` unions, the `LiteSpecMark` type, and the `proves` helper browser tests annotate with.

**Example:** `litespec model render` writes `apps/web/src/lib/app/litespec-records.ts` for the Claimwick web frontend, whose `UseCaseId` holds the use cases its entry point reaches and nothing else.

**Checked by:** `litespec model render` writes each frontend's file into its frame from its own entry points, and `litespec model check` reports `mark-unknown` (level S) for a mark naming a use case or screen outside its frontend's entry points.

### Refuse Stack-Specific Folder Names

**ID:** `litespec/frontend/web/structure#refuse-stack-specific-folder-names`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A React folder MUST NOT be named `lib`, nor a Svelte folder under `src/lib` be named `stores`.

**Rationale:** A `lib` folder on React would sit beside `src/app` as a second frame, and `src/app` already holds the frame. A `stores` folder on Svelte would gather shared state in one place, while the rules put shared state in the frame or beside its readers. Refusing both names stops the second home before anything moves into it.

**Reviewed for:** Which React folders take the name `lib`, and which Svelte folders under `src/lib` take the name `stores`? Look in the frontend tree.

### Locate the Frame in the App Folder

**ID:** `litespec/frontend/web/structure#locate-the-frame-in-the-app-folder`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** A frontend MUST place cross-route values in the frame folder its stack page states: client configuration, session read, language choice, permissions, formatting, and shell data.

**Rationale:** Client configuration, session read, language choice, permissions, formatting, and shell data cross every route. Filed in the frame folder, each has one home; filed in a route, each exists once per route with copies to keep equal. A browser API reached from a use-case module belongs here too, because a module renders and reads data rather than touching the device. This realizes litespec/frontend/structure#locate-cross-route-values-in-the-frame.

**Reviewed for:** Which cross-route values or browser uses sit outside the frame folder their stack page names? Look in the frame folder and the route and module sources.

## Conventions

### Use the Generated Route Tree

**ID:** `litespec/frontend/web/structure#use-the-generated-route-tree`

**Strength:** default

**Applies to:** artifact: route

**Default:** Generate the typed route artifacts from the files under `src/routes` with the router's own command, and keep them untracked.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** The typed route artifacts derive from the files under `src/routes`, so generating them with the router's own command and leaving them untracked keeps one source of truth. Committed artifacts add a second copy that a renamed route file leaves behind.

**Reviewed for:** Which build or type-check scripts skip the router's generate command, and which generated artifacts stay tracked? Look in the package scripts and the version control status.
