# Svelte Stack

**Stack:** svelte

**Concern:** frontend-ui

A Svelte frontend takes one of three rendering modes, depending on who reads it: a static SvelteKit application for an authenticated console, server rendering for a public surface a user reaches by link or search, and one static page with no router for a device client.

A team opening a Svelte frontend chooses its mode before writing routes. This page gives the names, files, and libraries the shared web pages leave to this stack, and the shared stack rules in [Technology Stacks](../../../core/stacks.md) apply to every stack.

## Concepts

### Where Each File Lives

A Svelte frontend holds its parts in folders: `src/lib/app` holds the frame, `src/routes` holds one folder per address, and `src/lib/modules` holds the use-case code. The toolkit generates the route types from the folders under `src/routes`.

Imports resolve through the `#lib` alias, which the frontend declares in the `imports` field of its `package.json` as `"#lib/*": "./src/lib/*"`. Shared files use the `.svelte` extension.

Route groups are folders under `src/routes` whose names stay in parentheses, such as `(console)`. The parentheses are router syntax: the folder groups routes under one layout without adding an address. For example, the Claimwick expense console keeps its frame in `src/lib/app`, its expense list page in `src/routes/expenses/+page.svelte`, and its expense use-case code in `src/lib/modules/spending/expenses`, importing frame code through `#lib/app/words.ts`.

### How a Route Loads Its Data

Svelte routes are folders under `src/routes`, each holding a page. A shell is a layout file that holds the frame every child page shares. Server-only code lives in a server file or under the server library.

A route loads its data before it draws. In Svelte terms, the page is a route file, the load is a loader, and the query creator is a suspense query: the load prefetches the route's reads into the query cache, and the page reads back the key the load filled, so it renders the data the load fetched.

For example, the Claimwick expense list folder holds its page number schema, its load, and its page component: the load fetches the expense page for the checked page number, and the page reads it back with a query creator over the same key. Intent preloading extends the same idea to links: hovering or focusing a link fills the target's cache key before the click, so the target draws from data it already holds.

### Which Files Declare an Address

A route entry is the folder under `src/routes` that holds a `+page.svelte`. The entry exports a screen mark, the same typed constant a use-case module exports, from the `<script module>` of its `+page.svelte`. The mark lives in the page because SvelteKit refuses any export from a `+page.ts` other than its own options and underscore-prefixed names.

Layout files (`+layout.svelte`, `+layout.ts`), error pages (`+error.svelte`), and route groups are not route entries. They draw no screen of their own, so they hold no screen mark: a mark on one would tie an address to a screen it does not show.

### How Components Draw and Fail

Component authoring follows [Frontend Components](../../components.md) and [TypeScript Web Components](../components.md). A class or style binding uses `class`, `class:`, `style`, or `style:`.

Two cases sit outside the composites. A drawing that no composite makes lives in a visual file. Raw HTML passes through the `{@html}` tag: the one template tag a search finds, so a review can check each use.

When a route fails, the failure renders in the route error page, which draws the shared problem state. The page-code lint enforces the refusals of [TypeScript Web Components](../components.md) through the Svelte ESLint processor in route and module folders.

### Where Data and Sentences Live

Route data lives in the query cache, filled by loads and read back by pages. A component's own state is a rune. Generated reads take their cache keys from the client generator's `@tanstack/svelte-query` plugin, so entries from every route share one shape.

Forms build on the form creator of the shared package: one entry point gives every form the same field setup and the same display for a refusal on its field. Sentences live in locale dictionaries, one dictionary per language, under the contract stated in [Frontend Data and State](../../data.md): the same fill, plural, and fallback behavior in every language the product ships.

### What Tests Leave in Place

The mocks rule names the libraries a test leaves in place: Svelte, SvelteKit, the query library, and the form library, because a stand-in for any of them tests the stand-in instead of the screen. For example, the Claimwick expense page test renders with the real query library and replaces only the network call.

### How Shared UI Is Built

The registry is built with the `shadcn-svelte` CLI, pinned in the manifest as `shadcn-svelte`. One registry command builds the UI artifacts the same way in every Svelte frontend.

The state-to-tone table maps each application state to one tone. One table keeps every state in the same tone on every route: the Claimwick console maps its overdue state to its warning tone in `src/lib/app/states.ts`.

### Which Versions Are Pinned

The `stack.node` key in `litespec.manifest.yaml` pins the Node version, the `stack.pnpm` key pins pnpm, and the `packages.npm` keys pin every npm package version. The keys name what is pinned; the manifest holds the pinned values.

## Standards

### Keep the Svelte Frame, Routes, and Modules

**ID:** `litespec/frontend/web/svelte/stack#keep-the-svelte-frame-routes-and-modules`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A Svelte frontend MUST hold the frame in `src/lib/app`, one folder per address in `src/routes`, and the use-case code in `src/lib/modules`.

**Rationale:** Three fixed homes let a reader and a tool find the frame, the routes, and the use-case code in the same place in every Svelte frontend.

**Example:** In the Claimwick expense console, the frame lives in `src/lib/app`, the expense list page in `src/routes/expenses/+page.svelte`, and the expense use-case code in `src/lib/modules/spending/expenses`.

**Reviewed for:** Does every file under `src` sit in `src/lib/app`, `src/routes`, or `src/lib/modules`, with one folder per address? Check the `src` tree of each Svelte frontend.

### Use the Svelte File Extension on Shared Files

**ID:** `litespec/frontend/web/svelte/stack#use-the-svelte-file-extension-on-shared-files`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A shared Svelte file MUST use the `.svelte` extension.

**Rationale:** One extension lets the compiler, the lint, and a search treat every shared Svelte file the same way.

**Example:** The Claimwick shared package holds its expense total as `ExpenseTotal.svelte`.

**Reviewed for:** Does every shared Svelte file use the `.svelte` extension? Check the `src` tree and the shared package of each Svelte frontend.

### Resolve Imports Through the Lib Alias

**ID:** `litespec/frontend/web/svelte/stack#resolve-imports-through-the-lib-alias`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Imports MUST resolve through the `#lib` alias that `package.json` declares in its `imports` field as `"#lib/*": "./src/lib/*"`.

**Rationale:** One declared alias lets a reader and a tool see every import of frame and module code without counting relative steps.

**Example:** The Claimwick console imports frame code through `#lib/app/words.ts`.

**Reviewed for:** Does every import into `src/lib` use the `#lib` alias? Check the import lines of each Svelte frontend.

### Write the File Extension in Each Import

**ID:** `litespec/frontend/web/svelte/stack#write-the-file-extension-in-each-import`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each import MUST name its module file extension.

**Rationale:** SvelteKit 3 requires the extension, because it resolves each import to its exact file.

**Example:** The Claimwick console imports `#lib/app/words.ts`, extension included.

**Reviewed for:** Does every import name its module file extension? Check the import lines in the `src` tree of each Svelte frontend.

### Keep Route Group Names in Router Syntax

**ID:** `litespec/frontend/web/svelte/stack#keep-route-group-names-in-router-syntax`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A route group MUST keep its parenthesized folder name.

**Rationale:** The parentheses are router syntax: a group folder without them becomes an address the router serves.

**Example:** The Claimwick console groups its expense pages under `(console)`, sharing one layout with no extra address.

**Reviewed for:** Does every folder under `src/routes` that shares a layout use a parenthesized name? Check the folder names under `src/routes`.

### Hold a Route's Parts in Its Folder

**ID:** `litespec/frontend/web/svelte/stack#hold-a-routes-parts-in-its-folder`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route folder MUST hold its parameters, its load, and its component.

**Rationale:** One folder for the three parts lets a reader see an address, its inputs, its data, and its screen together.

**Example:** The Claimwick expense list keeps its page number schema, its load, and its page component in its folder under `src/routes`.

**Reviewed for:** Does every route folder hold its parameter schema, its load, and its component? Check each folder under `src/routes`.

### Declare Route Parameters in a Schema

**ID:** `litespec/frontend/web/svelte/stack#declare-route-parameters-in-a-schema`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route MUST declare its parameters in a schema.

**Rationale:** A declared schema checks each parameter before the route reads it, so the page receives a typed value or a refusal.

**Example:** The Claimwick expense list declares its page number in a schema, so the page reads a checked number.

**Reviewed for:** Does every route declare its parameters in a schema before reading them? Check each route folder under `src/routes`.

### Prefetch Route Reads in the Load

**ID:** `litespec/frontend/web/svelte/stack#prefetch-route-reads-in-the-load`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route MUST prefetch its reads in its load.

**Rationale:** A load fills the query cache before the page draws, so the first render has its data.

**Example:** The Claimwick expense list load fetches the expense page for the checked page number before the page draws.

**Reviewed for:** Does every route prefetch its reads in its load? Check each load under `src/routes`.

### Read Route Data with a Query Creator

**ID:** `litespec/frontend/web/svelte/stack#read-route-data-with-a-query-creator`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route MUST read its route data with a query creator over the key its load filled.

**Rationale:** The page reads the key its load filled, so it renders the data the load fetched.

**Example:** The Claimwick expense list page reads its expense page through a query creator over the key its load filled.

**Reviewed for:** Does every route data read use the key its load filled? Check each page under `src/routes`.

### Load Intent Targets Before Navigation

**ID:** `litespec/frontend/web/svelte/stack#load-intent-targets-before-navigation`

**Strength:** required

**Applies to:** artifact: route

**Statement:** Intent preloading MUST fill the query cache before navigation.

**Rationale:** A preload on hover or focus fills the cache before the click, so the target page draws from data it already holds.

**Example:** In the Claimwick console, hovering the expense link fills the expense page key, and the click draws the list from the cache.

**Reviewed for:** Does every preloaded link fill the query cache before navigation? Check the link setup in the `src` tree.

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

**Reviewed for:** Does every class or style binding use `class`, `class:`, `style`, or `style:`? Check the markup in the component files.

### Name Drawings with the Visual Suffix

**ID:** `litespec/frontend/web/svelte/stack#name-drawings-with-the-visual-suffix`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A file that holds a drawing no composite makes MUST end with `.visual.svelte`.

**Rationale:** The suffix identifies the one place outside the composites, so a search finds every custom drawing in one pass.

**Example:** The Claimwick receipt sparkline lives in `ExpenseSparkline.visual.svelte`.

**Reviewed for:** Does every file holding a custom drawing end with `.visual.svelte`? Check the component folders.

### Render Raw HTML Only Through the Html Tag

**ID:** `litespec/frontend/web/svelte/stack#render-raw-html-only-through-the-html-tag`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A Svelte component MUST pass raw HTML to the page only through `{@html}`.

**Rationale:** One template tag gives every raw HTML sink a name a search finds, so a review can check each use.

**Example:** The Claimwick expense note passes its sanitized HTML through `{@html}`, matching its recorded sanitization decision.

**Reviewed for:** Does every raw HTML write go through `{@html}`? Check for `{@html}` and raw markup in the component files.

### Show Route Failure in the Route Error Page

**ID:** `litespec/frontend/web/svelte/stack#show-route-failure-in-the-route-error-page`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route failure MUST render in the route error page, which draws the shared problem state.

**Rationale:** One error page gives every route failure the same problem state and the same recovery.

**Example:** When the Claimwick expense list fails to load, `+error.svelte` shows the shared problem state with its trace identifier.

**Reviewed for:** Does every route failure render in `+error.svelte`? Check the error handling of each route folder under `src/routes`.

### Apply the Page-Code Lint to Route and Module Code

**ID:** `litespec/frontend/web/svelte/stack#apply-the-page-code-lint-to-route-and-module-code`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The page-code lint MUST apply the import and element restrictions through the Svelte ESLint processor, with visual files allowlisted.

**Rationale:** A lint refusal that names the composite to use lets the author replace a raw element when writing it, not in review. Visual files stay allowlisted because a drawing no composite makes has no composite to name.

**Example:** In `src/routes/expenses/+page.svelte`, the lint refuses a raw `div` and names the `Stack` composite to use instead.

**Reviewed for:** Does any route or module file use a refused import or element without a lint finding? Run the page-code lint, then compare its findings with `src/routes` and `src/lib/modules`.

### Mark Each Svelte Route Entry with Its Screen

**ID:** `litespec/frontend/web/svelte/stack#mark-each-svelte-route-entry-with-its-screen`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route entry MUST export its screen mark from the `<script module>` of its `+page.svelte` as the same typed constant a use-case module exports.

**Rationale:** A mark in the page ties the address to its use case where the address is declared. SvelteKit refuses any export from a `+page.ts` other than its own options and underscore-prefixed names, so the mark belongs in the page.

**Example:** The Claimwick expense list exports its typed screen mark from the `<script module>` of its `+page.svelte`.

**Reviewed for:** Does every route entry export its screen mark from the `<script module>` of `+page.svelte`? Check each `+page.svelte` under `src/routes`.

### Keep Layout Files, Error Pages, and Route Groups Unmarked

**ID:** `litespec/frontend/web/svelte/stack#keep-layout-files-error-pages-and-route-groups-unmarked`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A layout file, an error page, or a route group MUST NOT carry a screen mark.

**Rationale:** A layout, an error page, or a group draws no screen of its own, so a mark on it ties an address to a screen it does not show.

**Example:** The Claimwick console's `+layout.svelte` carries no screen mark.

**Reviewed for:** Do any layout files, error pages, or route groups carry a screen mark? Check the files under `src/routes` other than `+page.svelte`.

### Add the TanStack Query Plugin to the Client Generator

**ID:** `litespec/frontend/web/svelte/stack#add-the-tanstack-query-plugin-to-the-client-generator`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The client generator MUST add the `@tanstack/svelte-query` plugin.

**Rationale:** The plugin gives every generated read a query key, so the cache entries of every route share one shape.

**Example:** The Claimwick client generator adds `@tanstack/svelte-query`, giving each generated expense read a query key.

**Reviewed for:** Does every client generator configuration add the `@tanstack/svelte-query` plugin? Check the generator configuration of each Svelte frontend.

### Resolve Copy Through the Dictionary Library

**ID:** `litespec/frontend/web/svelte/stack#resolve-copy-through-the-dictionary-library`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The dictionary library MUST be `svelte-i18n` or machinery that meets the locale-dictionary contract that the frontend data page states.

**Rationale:** One contract keeps every sentence in one dictionary per language, with the same fill, plural, and fallback behavior.

**Example:** The Claimwick console renders its receipt-required sentence through `svelte-i18n` in every language it ships.

**Reviewed for:** Does every copy call site use the dictionary library with fill, plural, typed keys, and a declared fallback? Check the copy call sites under `src`.

### Use the Shared Package Form Creator

**ID:** `litespec/frontend/web/svelte/stack#use-the-shared-package-form-creator`

**Strength:** required

**Applies to:** artifact: form

**Statement:** A Svelte form MUST build on the form creator of the shared package.

**Rationale:** One entry point gives every form the same field setup and the same display for a refusal on its field.

**Example:** The Claimwick expense form builds on the shared form creator, with no form library of its own.

**Reviewed for:** Does every Svelte form build on the shared package form creator? Check the form files under `src`.

### Keep Svelte Test Mocks to the Owned Libraries

**ID:** `litespec/frontend/web/svelte/stack#keep-svelte-test-mocks-to-the-owned-libraries`

**Strength:** required

**Applies to:** artifact: test

**Statement:** A test MUST NOT replace Svelte, SvelteKit, the query library, or the form library.

**Rationale:** Those four are the stack's own behavior, and a stand-in for any of them tests the stand-in instead of the screen.

**Example:** The Claimwick expense page test runs against the real query library, replacing only the network call.

**Reviewed for:** Does any test replace Svelte, SvelteKit, the query library, or the form library? Check the mocks in each test file.

### Run the Registry Build Command

**ID:** `litespec/frontend/web/svelte/stack#run-the-registry-build-command`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The registry build command MUST be `shadcn-svelte build`.

**Rationale:** One registry command builds UI artifacts the same way in every Svelte frontend.

**Example:** The Claimwick console builds its UI registry with `shadcn-svelte build`.

**Reviewed for:** Does every registry build script run `shadcn-svelte build`? Check the scripts in each frontend's `package.json`.

### Keep the Tone Table in One States File

**ID:** `litespec/frontend/web/svelte/stack#keep-the-tone-table-in-one-states-file`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The state-to-tone table MUST live in `src/lib/app/states.ts`.

**Rationale:** One file holds every state's tone, so a state shows the same tone on every route.

**Example:** The Claimwick console maps its overdue state to its warning tone in `src/lib/app/states.ts`.

**Reviewed for:** Does every state-to-tone mapping live in `src/lib/app/states.ts`? Search the `src` tree for tone maps.

## Conventions

None.
