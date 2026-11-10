# Svelte Stack

**Stack:** svelte

**Layer:** frontend-ui

## Intent

A Svelte frontend takes one of three rendering modes, chosen by who reads it: a static SvelteKit application for an authenticated console, server rendering for a public surface readers reach by link or search, and one static page with no router for a device client. A team opening a Svelte frontend chooses the mode under [Web Rendering](../rendering.md) before writing routes, and finds on this page the names, files, and libraries the shared web pages leave to this stack. The shared stack rules in [Technology Stacks](../../../core/stacks.md) apply to every stack.

## Concepts

### Structure Bindings

The shared web pages state each rule once; this stack names the values that differ per stack. Svelte frontends hold one folder per address in `src/routes`, and the frame and use-case code in `src/lib/app` and `src/lib/modules`. The toolkit generates the route types from the folders under `src/routes`. A page holds its parameters, its load, and its component. Imports resolve through the `#lib` alias, which the frontend declares in the `imports` field of its `package.json` as `"#lib/*": "./src/lib/*"`, and each import names its module's file extension (`#lib/app/words.ts`), as SvelteKit 3 requires; shared files use the `.svelte` extension. Route groups keep their parenthesized names, because those names are router syntax.

### Rendering Bindings

Svelte routes are folders under `src/routes`, each holding a page, and the toolkit generates the route types from them. Each page declares its parameters in a schema and prefetches its reads in its load. Pages read route data with a query creator over the key the load filled, and intent preloading fills the query cache before navigation. A shell is a layout file holding the frame every child page shares. Server-only code lives in a server file or under the server library.

In Svelte terms: a page is a route file, a load is a loader, and a query creator is a suspense query.

### Component Bindings

A class or style binding spells `class`, `class:`, `style`, or `style:`. A file holding a drawing no composite makes ends with `*.visual.svelte`. Raw HTML reaches the page only through `{@html}`. The route error surface is the route error page, which draws the shared problem state. Component authoring follows [Svelte Components](components.md). The page-code lint applies the same restrictions through the Svelte ESLint processor, with visual files allowlisted.

### Route Entry Bindings

A route entry is the folder under `src/routes` that holds a `+page.svelte`. Its screen mark sits in the `<script module>` of that `+page.svelte`, as the same typed constant a use-case module exports, because SvelteKit refuses any export from a `+page.ts` other than its own options and `_`-prefixed names. Layout files (`+layout.svelte`, `+layout.ts`), error pages (`+error.svelte`), and route groups (parenthesized folders) are not route entries and carry no screen mark.

### Data Bindings

The frame root, holding client configuration and cross-route values, is `src/lib/app`. The client generator adds the `@tanstack/svelte-query` plugin. The component's own state is a rune. The dictionary library is `svelte-i18n`, or machinery meeting the same contract. The shared package's form entry point is its form creator.

### Testing Bindings

The mocks rule forbids replacing Svelte, SvelteKit, the query library, and the form library.

### UI Bindings

The registry build command is `shadcn-svelte build`. The state-to-tone table lives in `src/lib/app/states.ts`.

## Standards

None.

## Conventions

None.
