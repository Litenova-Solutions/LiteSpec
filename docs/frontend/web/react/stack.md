# React Stack

**Stack:** react

**Concern:** frontend-ui

A React frontend takes one of three rendering modes, depending on who reads it: a static application for an authenticated console, server rendering for a public surface a user reaches by link or search, and one static page with no router for a device client.

A team opening a React frontend chooses its mode before writing routes. This page gives the names, files, and libraries the shared web pages leave to this stack, and the shared stack rules in [Technology Stacks](../../../core/stacks.md) apply to every stack.

## Concepts

### Where Each File Lives

A React frontend holds its three parts in three folders: `src/app` holds the frame, `src/routes` holds one file per address, and `src/modules` holds the use-case code. The router generates the typed route tree from the files under `src/routes`. Shared files use the `.tsx` extension.

For example, the library product keeps its frame in `src/app`, its loan list route in `src/routes/loans.tsx`, and its loan use-case code in `src/modules/loans`.

The layout follows `litespec/frontend/web/structure#use-the-frontend-application-tree`, and shared files use the extension in `litespec/frontend/web/structure#use-the-stack-file-extension`.

### How a Route Loads Its Data

A React route is a file under `src/routes` that uses the router file convention, and the router builds the typed route tree from it. A shell is a pathless layout route whose file name starts with an underscore. Server-only code lives in a file whose name and import identify it as server-only.

A route loads its data before it draws. The loader prefetches the route's reads into the query cache, and the component reads back the key the loader filled with `useSuspenseQuery`, so it renders the data the loader fetched.

For example, the library loan list loader fetches the loan page for the page number `validateSearch` checked, and the component reads it back over the same key. Intent preloading extends the same idea to links: hovering or focusing a link fills the target's cache key before the click, so the target draws from data it already holds.

Routes hold their parts together under `litespec/frontend/structure#keep-a-route-entry-thin`, declare parameters under `litespec/frontend/rendering#validate-address-parameters-with-a-schema`, prefetch under `litespec/frontend/rendering#load-route-data-before-rendering`, read under `litespec/frontend/rendering#read-route-data-from-the-route-cache`, and preload intent under `litespec/frontend/rendering#prefetch-a-route-on-intent`.

### Which Files Declare an Address

A route entry is one file under `src/routes` that declares a route for an address. The entry exports a screen mark from its route file as the same typed constant a use-case module exports. The mark ties the address to its use case where the address is declared, and a wrong identifier fails the consumer's type check against the generated union.

Pathless layout routes, error components, and route groups are not route entries. They draw no screen of their own, so they hold no screen mark: a mark on one would tie an address to a screen it does not show.

Route entries carry marks under `litespec/frontend/web/structure#mark-each-web-route-entry-with-its-screen`, and layouts stay unmarked under `litespec/frontend/web/structure#keep-web-layouts-error-surfaces-and-route-groups-unmarked`.

### How Components Draw and Fail

Component authoring follows [Frontend Components](../../components.md) and [TypeScript Web Components](../components.md). A class or style binding uses the `className` or `style` prop.

Two cases sit outside the composites. A drawing that no composite makes lives in a visual file. Raw HTML passes through the `dangerouslySetInnerHTML` prop: the one name a search finds, so a review can check each use.

When a route fails, the failure renders in the route error component, which draws the shared problem state. The page-code lint enforces the refusals of [TypeScript Web Components](../components.md) in route and module folders.

Bindings, visual files, raw HTML, failure, and lint follow `litespec/frontend/web/components#use-the-stack-class-and-style-bindings`, `litespec/frontend/web/components#name-visual-files-with-the-stack-suffix`, `litespec/frontend/web/components#render-raw-html-through-the-stack-sink`, `litespec/frontend/web/rendering#show-route-failure-on-the-error-surface`, and `litespec/frontend/web/components#refuse-page-code-violations-with-eslint`.

### Where Data and Sentences Live

Route data lives in the query cache, filled by loaders and read back by components. A component's own state is local component state. Generated reads take their cache keys from the client generator's `@tanstack/react-query` plugin, so entries from every route share one shape.

Forms build on the form hook of the shared package: one entry point gives every form the same field setup and the same display for a refusal on its field. Sentences live in locale dictionaries, one dictionary per language, under the contract stated in [Frontend Data and State](../../data.md): the same fill, plural, and fallback behavior in every language the product ships.

The query plugin, dictionary library, and form entry point follow `litespec/frontend/web/data#add-the-stack-query-plugin-to-the-client-generator`, `litespec/frontend/web/data#resolve-copy-through-the-stack-dictionary-library`, and `litespec/frontend/web/data#build-a-web-form-on-the-shared-form-entry-point`.

### What Tests Leave in Place

The mocks rule names the libraries a test leaves in place: the rendering library, the router, and the query library, because a stand-in for any of them tests the stand-in instead of the screen. For example, the library loan list route test renders with the real router and query library and replaces only the network call.

Mocks follow `litespec/frontend/web/testing#keep-web-test-mocks-to-the-stack-libraries`.

### How Shared UI Is Built

The shadcn/ui registry is built with the `shadcn` CLI, pinned in the manifest as `shadcn`. One registry command builds the UI artifacts the same way in every React frontend.

The state-to-tone table maps each application state to one tone. One table keeps every state in the same tone on every route: the library product maps its overdue state to its warning tone in `src/app/states.tsx`.

The registry follows `litespec/frontend/web/ui#publish-one-registry-item-per-floorplan`, and the tone table follows `litespec/frontend/web/ui#keep-the-tone-table-in-the-stack-states-file`.

### Which Versions Are Pinned

The `stack.node` key in `litespec.manifest.yaml` pins the Node version, the `stack.pnpm` key pins pnpm, and the `packages.npm` keys pin every npm package version. The keys name what is pinned; the manifest holds the pinned values.

## Standards

None.

## Conventions

None.
