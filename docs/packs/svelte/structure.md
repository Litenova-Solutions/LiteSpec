# Web Structure

**Stack:** svelte
**Concern:** frontend-ui

A Svelte frontend keeps one folder layout across frame, routes, and modules. This page is for developers who place cross-route values, route entries, and use-case modules in that tree. A value outside its folder hides from readers, and a mark outside its file hides from the type check.

## Concepts

### How the Tree Fits Together

Each address keeps one thin route entry, imports point from routes toward shared packages, shared packages expose explicit entry points, and tests sit at their ownership boundary. The Svelte stack adds the tree, frame, mark, records file, and route tree rules this page states. The Claimwick web frontend follows the frame, routes, and modules shape with unit, route, and evidence tests beside it.

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

The tree above shows the frame with client configuration, session, language, permissions, formatting, and shell data. Modules mirror the backend use cases, and generated route artifacts, build output, and dependency folders stay untracked. SvelteKit configuration lives in `vite.config.ts`, with no `svelte.config.js`.

### How the Frame Groups Its Values

The frame groups cross-route values by capability: `session/` for the session read and permissions, `i18n/` for the language choice, `api/` for client configuration and API access, and `ui/` for formatting and shell data. Each grouping names the capability its values serve. A reader finds a value by what it is for, and the generated records file sits beside these folders.

A browser API reached from a use-case module lives in the frame as well. The module renders and reads data, while the frame holds the browser access beside the client configuration. The Claimwick web frontend holds its session read and permissions in `session/`, its dictionaries in `i18n/`, its client setup in `api/`, and its formatting and shell data in `ui/`.

### How Marks Tie Screens to Use Cases

A use-case folder mirrors its backend use case in path and carries a typed `litespec` constant with the use case it serves. The constant fails the type check when the identifier drifts from the generated union. Route files import the module instead of restating the identifier.

A route entry exports the same typed constant from the `<script module>` of its `+page.svelte`. The mark sits where the address is declared, so the builder and the check read one place. Layouts, error surfaces, and route groups draw no screen and carry no mark.

## Standards

### Use the Frontend Application Tree

**ID:** `litespec/packs/svelte/structure#use-the-frontend-application-tree`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A frontend MUST use the frame, routes, and modules layout its stack page states.

**Rationale:** The frame, routes, and modules layout gives every file one predictable home. Cross-route values sit in the frame, each address maps to one route file or folder, and use-case code sits under the module that owns it. A reader moves from a use case to its code by path alone.

**Example:** The Claimwick frontend follows the tree in the Concepts section above. Its frame holds shared values, its routes hold one page per address, and its modules mirror the backend use cases.

**Reviewed for:** Does every frontend tree follow the frame, routes, and modules layout its stack page states? Check the frontend tree.

### Mark Each Use-Case Module with Its Use Case

**ID:** `litespec/packs/svelte/structure#mark-each-use-case-module-with-its-use-case`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A frontend use-case folder MUST export one `litespec` constant whose `implements` names its use case, typed against the generated union of use-case identifiers.

**Rationale:** A folder path shows a reader where to look, while a tool reads nothing from a path. A typed constant gives the tool the same link the reader sees. A mistyped identifier fails the type check against the generated union, and `litespec model check` reports it where no type check ran.

**Example:** The list-expenses module in the Claimwick web frontend exports its mark. The snippet below shows the constant with the backend use case it serves.

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

**Rationale:** The records file is a cross-route value, so it sits with the other cross-route values in the frame. A union per frontend turns a cross-frontend mark into a type error. The render writes the file before any screen exists, so a new web app runs the render once at creation.

**Example:** `litespec model render` writes `apps/web/src/lib/app/litespec-records.ts` for the Claimwick web frontend. Its `UseCaseId` holds the use cases its entry point reaches and nothing else.

**Checked by:** `litespec model render` writes each frontend's file into its frame from its own entry points, and `litespec model check` reports `mark-unknown` (level S) for a mark naming a use case or screen outside its frontend's entry points.

### Refuse Stack-Specific Folder Names

**ID:** `litespec/packs/svelte/structure#refuse-stack-specific-folder-names`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A Svelte folder under `src/lib` MUST NOT be named `stores`.

**Rationale:** A `stores` folder gathers shared state in one place outside the frame. The rules place shared state in the frame or beside its readers. Refusing the name keeps each value in the one place the rules give it.

**Reviewed for:** Does any Svelte folder under `src/lib` take the name `stores`? Check the frontend tree.

### Locate the Frame in the App Folder

**ID:** `litespec/packs/svelte/structure#locate-the-frame-in-the-app-folder`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A frontend MUST place cross-route values in the frame folder its stack page states: client configuration, session read, language choice, permissions, formatting, and shell data.

**Rationale:** Client configuration, session read, language choice, permissions, formatting, and shell data cross every route. A copy per route multiplies copies to keep equal. One frame folder gives each value one home, including browser uses reached from use-case modules.

**Reviewed for:** Do all cross-route values and browser uses sit in the frame folder their stack page names? Check the frame folder and the route and module sources.

### Group the Web Frame by Capability

**ID:** `litespec/packs/svelte/structure#group-the-web-frame-by-capability`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A web frontend MUST group its frame folder into `session/`, `i18n/`, `api/`, and `ui/`, with no other folder.

**Rationale:** One flat frame folder reads as one long listing. A reader finds a value by search alone, and review misses what the frame holds. Each grouping names the capability its values serve, so a reader finds a value by what it is for.

**Example:** The Claimwick web frontend holds its session read and permissions in `session/`, its language dictionaries in `i18n/`, its API client setup in `api/`, and its formatting and shell data in `ui/`. Each value sits under the capability it serves.

**Checked by:** `litespec model check` reports `folder-ungrouped` for a frame folder past 20 files that holds none of its allowed groupings.

### Use the Stack File Extension

**ID:** `litespec/packs/svelte/structure#use-the-stack-file-extension`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A shared file MUST use the file extension `.svelte`.

**Rationale:** One extension lets the compiler, the lint, and a search treat every shared file the same way. A second extension forces every tool to guess twice.

**Example:** The Claimwick console holds its expense total as `ExpenseTotal.svelte`. The extension marks it as a shared file in the `src` tree.

**Reviewed for:** Does every shared file use the stated extension? Check the `src` tree of the web frontend.

### Mark Each Web Route Entry with Its Screen

**ID:** `litespec/packs/svelte/structure#mark-each-web-route-entry-with-its-screen`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route entry MUST export its screen mark as the same typed constant a use-case module exports, from the `<script module>` of `+page.svelte`.

**Rationale:** A mark where the address is declared ties the address to its use case in one link. The builder and the check read that one place. A wrong identifier fails the type check against the generated union.

**Example:** The Claimwick expense list exports its mark from the `<script module>` of its `+page.svelte`. The constant matches the shape a use-case module exports.

**Reviewed for:** Does every route entry export its screen mark from the stated place? Check each route entry.

### Keep Web Layouts, Error Surfaces, and Route Groups Unmarked

**ID:** `litespec/packs/svelte/structure#keep-web-layouts-error-surfaces-and-route-groups-unmarked`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A layout, an error surface, or a route group MUST NOT carry a screen mark.

**Rationale:** A layout, an error surface, or a group draws no screen of its own. A mark belongs on the entry that draws the screen, so these files carry none.

**Example:** The Claimwick `+layout.svelte` carries no screen mark. Its child route entries carry the marks for their screens.

**Reviewed for:** Do any layouts, error surfaces, or route groups carry a screen mark? Check the non-entry files under `src/routes`.

## Conventions

### Use the Generated Route Tree

**ID:** `litespec/packs/svelte/structure#use-the-generated-route-tree`

**Strength:** default

**Applies to:** artifact: route

**Default:** Generate the typed route artifacts from the files under `src/routes` with the router's own command, and keep them untracked.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** The typed route artifacts derive from the files under `src/routes`. Generating them with the router command and leaving them untracked keeps one source of truth. Committed artifacts form a second copy that goes stale on rename.

**Reviewed for:** Does every build and type-check script run the router's generate command, with no generated artifact staying tracked? Check the package scripts and the version control status.
