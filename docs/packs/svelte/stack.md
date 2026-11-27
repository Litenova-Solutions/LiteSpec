# Svelte Stack

**Stack:** svelte

**Concern:** frontend-ui

A Svelte frontend picks one of three rendering modes before routes are written. This page is for developers who place files, imports, and libraries in a Svelte frontend. A file in the wrong folder breaks the import map, and a second alias splits every import into two styles.

## Concepts

### Where Each File Lives

A Svelte frontend keeps its frame in `src/lib/app`, its addresses in `src/routes`, and its use-case code in `src/lib/modules`. The toolkit generates route types from the folders under `src/routes`. Imports resolve through `#lib`, the one alias the frontend declares in `package.json`.

| File | It carries |
|:---|:---|
| `package.json` | `"imports": { "#lib/*": "./src/lib/*" }`, the one alias declaration |
| `tsconfig.json` | Extends `./.svelte-kit/tsconfig.json`, with bundler resolution and extension imports allowed |
| `vite.config.ts` | The SvelteKit configuration through `sveltekit()`, with the adapter the rendering mode needs |

The table above names the three files that agree on imports. The TypeScript setup extends the SvelteKit base with bundler resolution and extension imports. The Vite setup holds the SvelteKit configuration with the adapter for the rendering mode, and SvelteKit 3 keeps no `svelte.config.js`.

Route groups stay in parentheses under `src/routes`, such as `(console)`. The parentheses group routes under one layout without adding an address. The Claimwick expense console keeps its frame in `src/lib/app`, its list page in `src/routes/expenses/+page.svelte`, and its use-case code in `src/lib/modules/spending/expenses`.

### How a Route Loads Its Data

Svelte routes are folders under `src/routes`, each holding a page, with a layout file holding the frame every child page shares. Server-only code lives in a server file or under the server library. In Svelte terms the page is the route file and the load is the loader, with the query creator reading back the prefetched key.

The Claimwick expense list wires end to end: the root layout builds the query client, `+page.ts` prefetches the list into the cache, the page reads back the filled key with a query creator, and a submit invalidates the list query. Hovering a link preloads the target key, so the target draws from data it holds. The layout builds its server cache per request.

### Which Files Declare an Address

A route entry is the folder under `src/routes` holding a `+page.svelte`. The entry exports a screen mark from the `<script module>` of its page, the same typed constant a use-case module exports. SvelteKit allows exports from `+page.ts` only for its own options and underscore-prefixed names, so the mark lives in the page.

Layout files, error pages, and route groups draw no screen of their own. They carry no screen mark, so an address stays tied to the screen it shows. The Claimwick `+layout.svelte` carries no mark.

### How Components, Data, and Tests Fit

Component authorship follows the components page, with class and style bindings in the four fixed forms. Drawings outside the composites live in visual files, and raw HTML passes through `{@html}`. Route failure draws the shared problem state on the error surface, and the page-code lint runs through the Svelte ESLint processor.

Route data lives in the query cache, filled by loads and read back by pages. Component state lives in runes, and generated reads take their cache keys from the TanStack Query plugin. Forms build on the shared form creator, and sentences live in locale dictionaries under the data page contract.

Tests replace only the network call, with the real query, Svelte, SvelteKit, and form libraries in place. The Claimwick expense page test follows that shape. The shared package builds through the `shadcn-svelte` registry command, and the state-to-tone table lives in the stack states file.

### Which Versions Are Pinned

The `stack.node` key in `litespec.manifest.yaml` pins Node and `stack.pnpm` pins pnpm. The `packages.npm` keys pin each npm package version. The keys name what is pinned and the manifest holds the pinned values.

## Standards

### Resolve Imports Through the Lib Alias

**ID:** `litespec/packs/svelte/stack#resolve-imports-through-the-lib-alias`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Imports MUST resolve through the `#lib` alias that `package.json` declares in its `imports` field as `"#lib/*": "./src/lib/*"`.

**Rationale:** One declared alias keeps every import of frame and module code readable. Relative steps break when folders move, while the alias stays stable.

**Example:** The Claimwick console imports frame code through `#lib/app/words.ts`. The import reads the same from any route or module file.

**Reviewed for:** Does every import into `src/lib` use the `#lib` alias? Check the import lines of each Svelte frontend.

### Write the File Extension in Each Import

**ID:** `litespec/packs/svelte/stack#write-the-file-extension-in-each-import`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each import MUST name its module file extension.

**Rationale:** SvelteKit 3 resolves each import to its exact file. The extension removes the guess between a source file and its neighbor.

**Example:** The Claimwick console imports `#lib/app/words.ts`, extension included. The path resolves to one file with no second guess.

**Reviewed for:** Does every import name its module file extension? Check the import lines in the `src` tree of each Svelte frontend.

### Keep Route Group Names in Router Syntax

**ID:** `litespec/packs/svelte/stack#keep-route-group-names-in-router-syntax`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A route group MUST keep its parenthesized folder name.

**Rationale:** Parentheses form the router syntax for a shared layout. A group folder without them becomes an address the router serves.

**Example:** The Claimwick console groups its expense pages under `(console)`, sharing one layout with no extra address. The address bar shows the page path without the group name.

**Reviewed for:** Does every folder under `src/routes` that shares a layout use a parenthesized name? Check the folder names under `src/routes`.

### Write Components in Runes Mode

**ID:** `litespec/packs/svelte/stack#write-components-in-runes-mode`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A component MUST hold its local state in runes, with props from `$props` and derived values from `$derived`.

**Rationale:** Runes form the reactivity model the compiler understands. Legacy reactive statements and untyped prop access behave differently under the compiler. An effect covers what derivation expresses differently.

**Example:** The snippet below declares typed props with `$props` and derives the total with `$derived`. Quantity and unit price stay in `$state`.

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
