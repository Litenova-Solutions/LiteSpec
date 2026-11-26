# Web Structure

**Stack:** svelte
**Concern:** frontend-ui

The Svelte frontend holds one folder layout: a frame holding the values shared across routes, one route per address, and modules that mirror the backend's use cases, so each business area and user task is found in the same place in the frontend and the backend.

This page states the Svelte tree: the application tree, the frame folder, the use-case marks, the generated records file, and the route tree.

## Concepts

### Rules the Svelte Stack Follows

Every address keeps one thin route entry, imports point from routes toward shared packages, shared packages expose explicit public entry points, and tests sit at their ownership boundary. The Svelte stack adds the tree, frame, mark, records file, and route tree rules this page states.

### The Grouping Folders of the Web Frame

The frame folder groups its cross-route values by capability: `session/` holds the session read and permissions, `i18n/` holds the language choice, `api/` holds client configuration and API access, and `ui/` holds formatting and shell data. Each grouping names the capability its values serve, so a reader finds a value by what it is for, and an agent never invents another folder. The generated records file sits beside these folders, as `litespec/packs/svelte/structure#generate-the-records-file-into-the-frame` states.

## Standards

### Use the Frontend Application Tree

**ID:** `litespec/packs/svelte/structure#use-the-frontend-application-tree`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A frontend MUST use the frame, routes, and modules layout its stack page states.

**Rationale:** The frame, routes, and modules layout gives every file one predictable home: cross-route values in the frame, one address per route file or folder, and use-case code under the module that owns it. A reader moves from a use case to its code by path alone.

**Example:**

```text
apps/{frontend}/
  src/
    routes/    one folder per address, with a page per folder
    lib/
      app/       the frame: client configuration, session, language, permissions, formatting, shell data
      modules/   one folder per backend module and use case
  tests/
    unit/      logic tests
    routes/    the route contract suite
    e2e/       evidence specifications
  vite.config.ts
  package.json
```

The generated route artifacts, build output, and dependency folders stay untracked. SvelteKit configuration lives in `vite.config.ts`: SvelteKit 3 reads no `svelte.config.js`.

**Reviewed for:** Does every frontend tree follow the frame, routes, and modules layout its stack page states? Check the frontend tree.

### Mark Each Use-Case Module with Its Use Case

**ID:** `litespec/packs/svelte/structure#mark-each-use-case-module-with-its-use-case`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A frontend use-case folder MUST export one `litespec` constant whose `implements` names its use case, typed against the generated union of use-case identifiers.

**Rationale:** The folder path mirrors the backend use case, which shows a reader where to look, but a path alone gives a tool nothing to check. A typed constant is the frontend mark in both mark styles: a mistyped identifier fails the consumer's type check against the generated union, and `litespec model check` reports it as `mark-unknown` where no type check ran. The route files import the module instead of restating the identifier. The calls a screen makes through the generated client remain the computed `calls` link; the mark adds the written link from the screen to the use case it serves.

**Example:** The list-expenses module in the Claimwick web frontend exports its mark.

```ts
// src/lib/modules/spending/expenses/list-expenses/litespec.ts
import type { LiteSpecMark } from "#lib/app/litespec-records.ts";

export const litespec = { implements: "use-case/spending.list-expenses" } as const satisfies LiteSpecMark;
```

**Checked by:** `litespec model check` reports `mark-missing` (level S) for a use-case folder exporting no `litespec` constant, `mark-unknown` (level S) for one naming no written use case, and `code-placement-drift` (level S) for a folder whose path does not mirror its marked use case.

### Generate the Records File into the Frame

**ID:** `litespec/packs/svelte/structure#generate-the-records-file-into-the-frame`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A frontend's generated `litespec-records.ts` MUST sit in its frame folder and hold only the use cases and screens of that frontend's own entry points.

**Rationale:** The records file is a cross-route value every module and route imports, which is what the frame holds. It lives in the frame folder at `src/lib/app/`. A union holding every browser use case of the project would let one frontend mark a use case only another frontend reaches, and the type check would pass; a union per frontend makes that mistake a type error. The file exports the `UseCaseId` and `ScreenId` unions, the `LiteSpecMark` type, and the `proves` helper that browser tests annotate with. The render reads the screens and use cases of the frontend's entry points, not the code marks, so it writes the file before any screen exists: run `litespec model render` once when the web app is created. A frontend holding no marked route yet reads every browser entry point.

**Example:** `litespec model render` writes `apps/web/src/lib/app/litespec-records.ts` for the Claimwick web frontend, whose `UseCaseId` holds the use cases its entry point reaches and nothing else.

**Checked by:** `litespec model render` writes each frontend's file into its frame from its own entry points, and `litespec model check` reports `mark-unknown` (level S) for a mark naming a use case or screen outside its frontend's entry points.

### Refuse Stack-Specific Folder Names

**ID:** `litespec/packs/svelte/structure#refuse-stack-specific-folder-names`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A Svelte folder under `src/lib` MUST NOT be named `stores`.

**Rationale:** A `stores` folder would gather shared state in one place, while the rules put shared state in the frame or beside its readers. Refusing the name keeps each value in the one place the rules give it.

**Reviewed for:** Does any Svelte folder under `src/lib` take the name `stores`? Check the frontend tree.

### Locate the Frame in the App Folder

**ID:** `litespec/packs/svelte/structure#locate-the-frame-in-the-app-folder`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A frontend MUST place cross-route values in the frame folder its stack page states: client configuration, session read, language choice, permissions, formatting, and shell data.

**Rationale:** Client configuration, session read, language choice, permissions, formatting, and shell data cross every route. Placed in the frame folder, each has one home; placed in a route, each would exist once per route, with copies to keep equal. A browser API reached from a use-case module belongs here too, because a module renders and reads data rather than calling such APIs directly.

**Reviewed for:** Do all cross-route values and browser uses sit in the frame folder their stack page names? Check the frame folder and the route and module sources.

### Group the Web Frame by Capability

**ID:** `litespec/packs/svelte/structure#group-the-web-frame-by-capability`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A web frontend MUST group its frame folder into `session/`, `i18n/`, `api/`, and `ui/`, with no other folder.

**Rationale:** One flat frame folder reads as one long listing: a reader finds a value only by search, and review misses what the frame holds. Each grouping names the capability its values serve, so a reader finds a value by what it is for. An agent never invents another grouping: a folder outside the allowed set is refused, and a folder past 20 files that holds none of its allowed groupings is a finding.

**Example:** The Claimwick web frontend holds its session read and permissions in `session/`, its language dictionaries in `i18n/`, its API client setup in `api/`, and its formatting and shell data in `ui/`.

**Checked by:** `litespec model check` reports `folder-ungrouped` for a frame folder past 20 files that holds none of its allowed groupings.

### Use the Stack File Extension

**ID:** `litespec/packs/svelte/structure#use-the-stack-file-extension`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A shared file MUST use the file extension `.svelte`.

**Rationale:** One extension lets the compiler, the lint, and a search treat every shared file the same way. No file needs a second guess.

**Example:** The Claimwick console holds its expense total as `ExpenseTotal.svelte`.

**Reviewed for:** Does every shared file use the stated extension? Check the `src` tree of the web frontend.

### Mark Each Web Route Entry with Its Screen

**ID:** `litespec/packs/svelte/structure#mark-each-web-route-entry-with-its-screen`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route entry MUST export its screen mark as the same typed constant a use-case module exports, from the `<script module>` of `+page.svelte`.

**Rationale:** A mark where the address is declared ties the address to its use case in one link, so the builder and the check read one place. A wrong identifier fails the consumer type check against the generated union.

**Example:** The Claimwick expense list exports its mark from the `<script module>` of its `+page.svelte`.

**Reviewed for:** Does every route entry export its screen mark from the stated place? Check each route entry.

### Keep Web Layouts, Error Surfaces, and Route Groups Unmarked

**ID:** `litespec/packs/svelte/structure#keep-web-layouts-error-surfaces-and-route-groups-unmarked`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A layout, an error surface, or a route group MUST NOT carry a screen mark.

**Rationale:** A layout, an error surface, or a group draws no screen of its own, so a mark on it ties an address to a screen it does not show.

**Example:** The Claimwick `+layout.svelte` carries no screen mark.

**Reviewed for:** Do any layouts, error surfaces, or route groups carry a screen mark? Check the non-entry files under `src/routes`.

## Conventions

### Use the Generated Route Tree

**ID:** `litespec/packs/svelte/structure#use-the-generated-route-tree`

**Strength:** default

**Applies to:** artifact: route

**Default:** Generate the typed route artifacts from the files under `src/routes` with the router's own command, and keep them untracked.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** The typed route artifacts derive from the files under `src/routes`. Generating them with the router's own command and leaving them untracked keeps one source of truth. Committed artifacts add a second copy that goes stale when a route file is renamed.

**Reviewed for:** Does every build and type-check script run the router's generate command, with no generated artifact staying tracked? Check the package scripts and the version control status.
