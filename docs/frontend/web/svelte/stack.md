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

The layout follows `litespec/frontend/web/structure#use-the-frontend-application-tree`, and shared files use the extension in `litespec/frontend/web/structure#use-the-stack-file-extension`.

### How a Route Loads Its Data

Svelte routes are folders under `src/routes`, each holding a page. A shell is a layout file that holds the frame every child page shares. Server-only code lives in a server file or under the server library.

A route loads its data before it draws. In Svelte terms, the page is a route file, the load is a loader, and the query creator is a suspense query: the load prefetches the route's reads into the query cache, and the page reads back the key the load filled, so it renders the data the load fetched.

For example, the Claimwick expense list folder holds its page number schema, its load, and its page component: the load fetches the expense page for the checked page number, and the page reads it back with a query creator over the same key. Intent preloading extends the same idea to links: hovering or focusing a link fills the target's cache key before the click, so the target draws from data it already holds.

Routes hold their parts together under `litespec/frontend/structure#keep-a-route-entry-thin`, declare parameters under `litespec/frontend/rendering#validate-address-parameters-with-a-schema`, prefetch under `litespec/frontend/rendering#load-route-data-before-rendering`, read under `litespec/frontend/rendering#read-route-data-from-the-route-cache`, and preload intent under `litespec/frontend/rendering#prefetch-a-route-on-intent`.

### Which Files Declare an Address

A route entry is the folder under `src/routes` that holds a `+page.svelte`. The entry exports a screen mark, the same typed constant a use-case module exports, from the `<script module>` of its `+page.svelte`. The mark lives in the page because SvelteKit refuses any export from a `+page.ts` other than its own options and underscore-prefixed names.

Layout files (`+layout.svelte`, `+layout.ts`), error pages (`+error.svelte`), and route groups are not route entries. They draw no screen of their own, so they hold no screen mark: a mark on one would tie an address to a screen it does not show.

Route entries carry marks under `litespec/frontend/web/structure#mark-each-web-route-entry-with-its-screen`, and layouts stay unmarked under `litespec/frontend/web/structure#keep-web-layouts-error-surfaces-and-route-groups-unmarked`.

### How Components Draw and Fail

Component authoring follows [Frontend Components](../../components.md) and [TypeScript Web Components](../components.md). A class or style binding uses `class`, `class:`, `style`, or `style:`.

Two cases sit outside the composites. A drawing that no composite makes lives in a visual file. Raw HTML passes through the `{@html}` tag: the one template tag a search finds, so a review can check each use.

When a route fails, the failure renders in the route error page, which draws the shared problem state. The page-code lint enforces the refusals of [TypeScript Web Components](../components.md) through the Svelte ESLint processor in route and module folders.

Bindings, visual files, raw HTML, failure, and lint follow `litespec/frontend/web/components#use-the-stack-class-and-style-bindings`, `litespec/frontend/web/components#name-visual-files-with-the-stack-suffix`, `litespec/frontend/web/components#render-raw-html-through-the-stack-sink`, `litespec/frontend/web/rendering#show-route-failure-on-the-error-surface`, and `litespec/frontend/web/components#refuse-page-code-violations-with-eslint`.

### Where Data and Sentences Live

Route data lives in the query cache, filled by loads and read back by pages. A component's own state is a rune. Generated reads take their cache keys from the client generator's `@tanstack/svelte-query` plugin, so entries from every route share one shape.

Forms build on the form creator of the shared package: one entry point gives every form the same field setup and the same display for a refusal on its field. Sentences live in locale dictionaries, one dictionary per language, under the contract stated in [Frontend Data and State](../../data.md): the same fill, plural, and fallback behavior in every language the product ships.

The query plugin, dictionary library, and form entry point follow `litespec/frontend/web/data#add-the-stack-query-plugin-to-the-client-generator`, `litespec/frontend/web/data#resolve-copy-through-the-stack-dictionary-library`, and `litespec/frontend/web/data#build-a-web-form-on-the-shared-form-entry-point`.

### What Tests Leave in Place

The mocks rule names the libraries a test leaves in place: Svelte, SvelteKit, the query library, and the form library, because a stand-in for any of them tests the stand-in instead of the screen. For example, the Claimwick expense page test renders with the real query library and replaces only the network call.

Mocks follow `litespec/frontend/web/testing#keep-web-test-mocks-to-the-stack-libraries`.

### How Shared UI Is Built

The registry is built with the `shadcn-svelte` CLI, pinned in the manifest as `shadcn-svelte`. One registry command builds the UI artifacts the same way in every Svelte frontend.

The state-to-tone table maps each application state to one tone. One table keeps every state in the same tone on every route: the Claimwick console maps its overdue state to its warning tone in `src/lib/app/states.ts`.

The registry follows `litespec/frontend/web/ui#publish-one-registry-item-per-floorplan`, and the tone table follows `litespec/frontend/web/ui#keep-the-tone-table-in-the-stack-states-file`.

### Which Versions Are Pinned

The `stack.node` key in `litespec.manifest.yaml` pins the Node version, the `stack.pnpm` key pins pnpm, and the `packages.npm` keys pin every npm package version. The keys name what is pinned; the manifest holds the pinned values.

## Standards

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

## Conventions

None.
