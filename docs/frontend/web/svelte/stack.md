# Svelte Stack

**Stack:** svelte

**Concern:** frontend-ui

## Intent

A Svelte frontend takes one of three rendering modes, chosen by who reads it: a static SvelteKit application for an authenticated console, server rendering for a public surface a user reaches by link or search, and one static page with no router for a device client. A team opening a Svelte frontend chooses the mode under [Web Rendering](../rendering.md) before writing routes, and finds on this page the names, files, and libraries the shared web pages leave to this stack. The shared stack rules in [Technology Stacks](../../../core/stacks.md) apply to every stack.

## Concepts

### Version Pins

The `stack.node` key in `litespec.manifest.yaml` pins the Node version, the `stack.pnpm` key pins pnpm, and the `packages.npm` keys pin every npm package version. These keys name the pins; the manifest holds the values.

### Structure Bindings

Svelte names the frontend parts as folders: `src/routes` holds one folder per address, `src/lib/app` holds the frame, and `src/lib/modules` holds the use-case code. The toolkit generates the route types from the folders under `src/routes`. Imports resolve through the `#lib` alias, which the frontend declares in the `imports` field of its `package.json` as `"#lib/*": "./src/lib/*"`. Shared files use the `.svelte` extension, and route groups keep their parenthesized names, because those names are router syntax.

### Rendering Bindings

Svelte routes are folders under `src/routes`, each holding a page, and the toolkit generates the route types from them. A shell is a layout file that holds the frame every child page shares. Server-only code lives in a server file or under the server library.

In Svelte terms: a page is a route file, a load is a loader, and a query creator is a suspense query.

### Component Bindings

A class or style binding spells `class`, `class:`, `style`, or `style:`. A drawing that no composite makes lives in a visual file. Raw HTML is the `{@html}` tag. The route error surface is the route error page, which draws the shared problem state. Component authoring follows [Frontend Components](../../components.md) and [TypeScript Web Components](../components.md). The page-code lint enforces the refusals of [TypeScript Web Components](../components.md) through the Svelte ESLint processor in route and module folders.

### Route Entry Bindings

A route entry is the folder under `src/routes` that holds a `+page.svelte`. Layout files (`+layout.svelte`, `+layout.ts`), error pages (`+error.svelte`), and route groups are not route entries, so they hold no screen mark.

### Data Bindings

Route data lives in the query cache. A component's own state is a rune. The shared package's form entry point is its form creator, and the locale-dictionary contract is stated in [Web Data and State](../data.md).

### Testing Bindings

The mocks rule names the libraries a test must not replace.

### UI Bindings

The registry is built with the `shadcn-svelte` CLI, pinned in the manifest as `shadcn-svelte`. The state-to-tone table maps each application state to one tone.

## Standards

### Keep the Svelte Frame, Routes, and Modules

**ID:** `litespec/frontend/web/svelte/stack#keep-the-svelte-frame-routes-and-modules`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A Svelte frontend MUST hold the frame in `src/lib/app`, one folder per address in `src/routes`, and the use-case code in `src/lib/modules`.

**Rationale:** Three fixed homes let a reader and a tool find the frame, the routes, and the use-case code in the same place in every Svelte frontend.

**Example:** The Claimwick expense console keeps the frame in `src/lib/app`, the expense list page in `src/routes/expenses/+page.svelte`, and the expense use-case code in `src/lib/modules/spending/expenses`.

**Reviewed for:** Which files under `src` sit outside `src/lib/app`, `src/routes`, and `src/lib/modules`, or which address has two folders under `src/routes`? Look at the `src` tree of each Svelte frontend.

### Use the Svelte Extension on Shared Files

**ID:** `litespec/frontend/web/svelte/stack#use-the-svelte-extension-on-shared-files`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A shared Svelte file MUST use the `.svelte` extension.

**Rationale:** One extension lets the compiler, the lint, and a search treat every shared Svelte file the same way.

**Example:** The shared expense total is `ExpenseTotal.svelte` in the shared package.

**Reviewed for:** Which shared Svelte files use an extension other than `.svelte`? Look at the `src` tree and the shared package of each Svelte frontend.

### Resolve Imports Through the Lib Alias

**ID:** `litespec/frontend/web/svelte/stack#resolve-imports-through-the-lib-alias`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Imports MUST resolve through the `#lib` alias that `package.json` declares in its `imports` field as `"#lib/*": "./src/lib/*"`.

**Rationale:** One declared alias lets a reader and a tool see every import of frame and module code without counting relative steps.

**Example:** The Claimwick console imports frame code through `#lib/app/words.ts`.

**Reviewed for:** Which imports reach `src/lib` through a relative path instead of `#lib`? Look at the import lines of each Svelte frontend.

### Name the Module Extension on Each Import

**ID:** `litespec/frontend/web/svelte/stack#name-the-module-extension-on-each-import`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each import MUST name its module file extension.

**Rationale:** SvelteKit 3 requires the extension, because it resolves each import to its exact file.

**Example:** The Claimwick console's import of the words module ends in `.ts`, as in `#lib/app/words.ts`.

**Reviewed for:** Which imports omit the module file extension? Look at the import lines in the `src` tree of each Svelte frontend.

### Keep Route Group Names in Router Syntax

**ID:** `litespec/frontend/web/svelte/stack#keep-route-group-names-in-router-syntax`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A route group MUST keep its parenthesized folder name.

**Rationale:** The parentheses are router syntax: a group folder without them becomes an address the router serves.

**Example:** The Claimwick console keeps its `(console)` group, so its expense pages share one layout without adding an address.

**Reviewed for:** Which folders under `src/routes` hold a shared layout without parentheses? Look at the folder names under `src/routes`.

### Hold a Route's Parts in Its Folder

**ID:** `litespec/frontend/web/svelte/stack#hold-a-routes-parts-in-its-folder`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route folder MUST hold its parameters, its load, and its component.

**Rationale:** One folder for the three parts lets a reader see an address, its inputs, its data, and its screen together.

**Example:** The Claimwick expense list folder holds its page number schema, its load, and its page component.

**Reviewed for:** Which route folders keep a parameter schema, a load, or a component outside the folder? Look at each folder under `src/routes`.

### Declare Route Parameters in a Schema

**ID:** `litespec/frontend/web/svelte/stack#declare-route-parameters-in-a-schema`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route MUST declare its parameters in a schema.

**Rationale:** A declared schema checks each parameter before the route reads it, so the page receives a typed value or a refusal.

**Example:** The Claimwick expense list page declares its page number in a schema.

**Reviewed for:** Which routes read a parameter without declaring it in a schema? Look at each route folder under `src/routes`.

### Prefetch Route Reads in the Load

**ID:** `litespec/frontend/web/svelte/stack#prefetch-route-reads-in-the-load`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route MUST prefetch its reads in its load.

**Rationale:** A load fills the query cache before the page draws, so the first render has its data.

**Example:** The Claimwick expense list load fetches the expense page for the page number its schema checked.

**Reviewed for:** Which routes read data in their page without a load prefetch? Look at each load under `src/routes`.

### Read Route Data with a Query Creator

**ID:** `litespec/frontend/web/svelte/stack#read-route-data-with-a-query-creator`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route MUST read its route data with a query creator over the key its load filled.

**Rationale:** Reading the filled key keeps the page on the data the load fetched.

**Example:** The Claimwick expense list page reads its page with a query creator over the same key its load filled.

**Reviewed for:** Which route data reads use a key the load does not fill? Look at each page under `src/routes`.

### Load Intent Targets Before Navigation

**ID:** `litespec/frontend/web/svelte/stack#load-intent-targets-before-navigation`

**Strength:** required

**Applies to:** artifact: route

**Statement:** Intent preloading MUST fill the query cache before navigation.

**Rationale:** A preload on hover or focus fills the cache before the click, so the target page draws from data it already holds.

**Example:** Hovering the Claimwick expense link fills the expense page key, so the click draws the list from the cache.

**Reviewed for:** Which links preload a route without filling the query cache? Look at the link setup in the `src` tree.

### Write Components in Runes Mode

**ID:** `litespec/frontend/web/svelte/stack#write-components-in-runes-mode`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A component MUST hold its local state in runes, with props from `$props` and derived values from `$derived`.

**Rationale:** Runes are the reactivity model the compiler understands: `$state` for local state, `$props` for inputs, and `$derived` for computed values. Legacy reactive statements and untyped prop access bypass that model and behave differently under the compiler. An effect covers what derivation cannot express, so nothing needs the legacy forms.

**Example:**

```svelte
<script lang="ts">
  let { orderId }: { orderId: string } = $props();
  let quantity = $state(1);
  let unitPrice = $state(0);
  let total = $derived(quantity * unitPrice);
</script>
```

**Checked by:** `svelte-check` fails on a legacy reactive statement or an untyped `$props` call.

### Use Named Attributes for Class and Style Bindings

**ID:** `litespec/frontend/web/svelte/stack#use-named-attributes-for-class-and-style-bindings`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A class or style binding in a Svelte component MUST use `class`, `class:`, `style`, or `style:`.

**Rationale:** Four fixed attribute forms keep styling in one vocabulary that the lint and a reviewer can read.

**Example:** The Claimwick expense status chip toggles its overdue tone with `class:overdue` and sets no raw `style` attribute.

**Reviewed for:** Which Svelte components bind a class or style through another attribute? Look at the markup in the component files.

### Name Drawings with the Visual Suffix

**ID:** `litespec/frontend/web/svelte/stack#name-drawings-with-the-visual-suffix`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A file that holds a drawing no composite makes MUST end with `.visual.svelte`.

**Rationale:** The suffix marks the one place outside the composites, so a search finds every custom drawing in one pass.

**Example:** The Claimwick expense receipt sparkline is `ExpenseSparkline.visual.svelte`.

**Reviewed for:** Which custom drawings sit in `.svelte` files whose names lack the `.visual.svelte` suffix? Look in the component folders.

### Render Raw HTML Only Through One Template Tag

**ID:** `litespec/frontend/web/svelte/stack#render-raw-html-only-through-one-template-tag`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A Svelte component MUST pass raw HTML to the page only through `{@html}`.

**Rationale:** One template tag makes every raw HTML sink a name a search finds, and a review can check each use.

**Example:** The Claimwick expense note renders its sanitized HTML through `{@html}` after its recorded sanitization decision.

**Reviewed for:** Which Svelte components write raw HTML through another API? Look for `{@html}` and raw markup in the component files.

### Show Route Failure in the Route Error Page

**ID:** `litespec/frontend/web/svelte/stack#show-route-failure-in-the-route-error-page`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route failure MUST render in the route error page, which draws the shared problem state.

**Rationale:** One error page gives every route failure the same problem state and the same recovery.

**Example:** A Claimwick expense list that fails to load shows the shared problem state from `+error.svelte` with its trace identifier.

**Reviewed for:** Which route failures render outside `+error.svelte`? Look at the error handling of each route folder under `src/routes`.

### Apply the Page-Code Lint to Route and Module Code

**ID:** `litespec/frontend/web/svelte/stack#apply-the-page-code-lint-to-route-and-module-code`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The page-code lint MUST apply the import and element restrictions through the Svelte ESLint processor, with visual files allowlisted.

**Rationale:** A lint refusal that names the composite to use lets the author replace a raw element when writing it, not in review.

**Example:** The lint refuses a raw `div` in `src/routes/expenses/+page.svelte` and names the `Stack` composite to use instead.

**Reviewed for:** Which route or module files use a refused import or element without a lint finding? Run the page-code lint, then compare its findings with `src/routes` and `src/lib/modules`.

### Mark Svelte Route Entries

**ID:** `litespec/frontend/web/svelte/stack#mark-svelte-route-entries`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route entry MUST export its screen mark from the `<script module>` of its `+page.svelte` as the same typed constant a use-case module exports.

**Rationale:** A mark in the page ties the address to its use case where the address is declared. SvelteKit refuses any export from a `+page.ts` other than its own options and underscore-prefixed names, so the mark belongs in the page.

**Example:** The Claimwick expense list `+page.svelte` exports the typed screen mark in its `<script module>`.

**Reviewed for:** Which route entries export no screen mark from the `<script module>` of `+page.svelte`? Look at each `+page.svelte` under `src/routes`.

### Keep Layout Files, Error Pages, and Route Groups Unmarked

**ID:** `litespec/frontend/web/svelte/stack#keep-layout-files-error-pages-and-route-groups-unmarked`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A layout file, an error page, or a route group MUST NOT carry a screen mark.

**Rationale:** A layout, an error page, or a group draws no screen of its own, so a mark on it ties an address to a screen it does not show.

**Example:** The Claimwick `+layout.svelte` exports no screen mark.

**Reviewed for:** Which layout files, error pages, or route groups export a screen mark? Look at the files under `src/routes` other than `+page.svelte`.

### Add the TanStack Query Plugin to the Client Generator

**ID:** `litespec/frontend/web/svelte/stack#add-the-tanstack-query-plugin-to-the-client-generator`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The client generator MUST add the `@tanstack/svelte-query` plugin.

**Rationale:** The plugin gives every generated read a query key, so the cache entries of every route share one shape.

**Example:** The Claimwick client generator adds the `@tanstack/svelte-query` plugin, so each generated expense read has a query key.

**Reviewed for:** Which client generator configurations omit the `@tanstack/svelte-query` plugin? Look in the generator configuration of each Svelte frontend.

### Meet the Locale-Dictionary Contract with the Dictionary Library

**ID:** `litespec/frontend/web/svelte/stack#meet-the-locale-dictionary-contract-with-the-dictionary-library`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The dictionary library MUST be `svelte-i18n` or machinery that meets the locale-dictionary contract that the web data page states.

**Rationale:** One contract keeps every sentence in one dictionary per language, with the same fill, plural, and fallback behavior.

**Example:** The Claimwick console resolves its receipt-required sentence through `svelte-i18n` in every language it ships.

**Reviewed for:** Which copy bypasses the dictionary library, or misses fill, plural, typed keys, or a declared fallback? Look at the copy call sites under `src`.

### Use the Shared Package Form Creator

**ID:** `litespec/frontend/web/svelte/stack#use-the-shared-package-form-creator`

**Strength:** required

**Applies to:** artifact: form

**Statement:** A Svelte form MUST build on the form creator of the shared package.

**Rationale:** One entry point gives every form the same field wiring and the same display for a refusal on its field.

**Example:** The Claimwick expense form uses the shared form creator and no form library of its own.

**Reviewed for:** Which Svelte forms bypass the shared package form creator? Look at the form files under `src`.

### Keep Svelte Test Mocks to the Owned Libraries

**ID:** `litespec/frontend/web/svelte/stack#keep-svelte-test-mocks-to-the-owned-libraries`

**Strength:** required

**Applies to:** artifact: test

**Statement:** A test MUST NOT replace Svelte, SvelteKit, the query library, or the form library.

**Rationale:** Those four are the stack's own behavior, and a stand-in for any of them tests the stand-in instead of the screen.

**Example:** The Claimwick expense page test renders with the real query library and replaces only the network call.

**Reviewed for:** Which tests replace Svelte, SvelteKit, the query library, or the form library? Look at the mocks in each test file.

### Run the Registry Build Command

**ID:** `litespec/frontend/web/svelte/stack#run-the-registry-build-command`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The registry build command MUST be `shadcn-svelte build`.

**Rationale:** One registry command builds UI artifacts the same way in every Svelte frontend.

**Example:** The Claimwick console builds its registry with `shadcn-svelte build`.

**Reviewed for:** Which registry build scripts run a command other than `shadcn-svelte build`? Look at the scripts in each frontend's `package.json`.

### Keep the Tone Table in One States File

**ID:** `litespec/frontend/web/svelte/stack#keep-the-tone-table-in-one-states-file`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The state-to-tone table MUST live in `src/lib/app/states.ts`.

**Rationale:** One file holds every state's tone, so a state reads the same tone on every route.

**Example:** The Claimwick console maps the overdue state to its warning tone in `src/lib/app/states.ts`.

**Reviewed for:** Which state-to-tone mappings live outside `src/lib/app/states.ts`? Look for tone maps in the `src` tree.

## Conventions

None.
