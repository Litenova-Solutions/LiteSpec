# React Stack

**Stack:** react

**Concern:** frontend-ui

A React frontend takes one of three rendering modes, depending on who reads it: a static application for an authenticated console, server rendering for a public surface a user reaches by link or search, and one static page with no router for a device client.

A team opening a React frontend chooses its mode on [Web Rendering and Routes](../rendering.md) before writing routes. This page gives the names, files, and libraries the shared web pages leave to this stack, and the shared stack rules in [Technology Stacks](../../../core/stacks.md) apply to every stack.

## Concepts

### Where Each File Lives

A React frontend holds its three parts in three folders: `src/app` holds the frame, `src/routes` holds one file per address, and `src/modules` holds the use-case code. The router generates the typed route tree from the files under `src/routes`. Shared files use the `.tsx` extension.

For example, the library product keeps its frame in `src/app`, its loan list route in `src/routes/loans.tsx`, and its loan use-case code in `src/modules/loans`.

### How a Route Loads Its Data

A React route is a file under `src/routes` that uses the router file convention, and the router builds the typed route tree from it. A shell is a pathless layout route whose file name starts with an underscore. Server-only code lives in a file whose name and import identify it as server-only.

A route loads its data before it draws. The loader prefetches the route's reads into the query cache, and the component reads back the key the loader filled with `useSuspenseQuery`, so it renders the data the loader fetched.

For example, the library loan list loader fetches the loan page for the page number `validateSearch` checked, and the component reads it back over the same key. Intent preloading extends the same idea to links: hovering or focusing a link fills the target's cache key before the click, so the target draws from data it already holds.

### Which Files Declare an Address

A route entry is one file under `src/routes` that declares a route for an address. The entry exports a screen mark from its route file as the same typed constant a use-case module exports. The mark ties the address to its use case where the address is declared, and a wrong identifier fails the consumer's type check against the generated union.

Pathless layout routes, error components, and route groups are not route entries. They draw no screen of their own, so they hold no screen mark: a mark on one would tie an address to a screen it does not show.

### How Components Draw and Fail

Component authoring follows [Frontend Components](../../components.md) and [TypeScript Web Components](../components.md). A class or style binding uses the `className` or `style` prop.

Two cases sit outside the composites. A drawing that no composite makes lives in a visual file. Raw HTML passes through the `dangerouslySetInnerHTML` prop: the one name a search finds, so a review can check each use.

When a route fails, the failure renders in the route error component, which draws the shared problem state. The page-code lint enforces the refusals of [TypeScript Web Components](../components.md) in route and module folders.

### Where Data and Sentences Live

Route data lives in the query cache, filled by loaders and read back by components. A component's own state is local component state. Generated reads take their cache keys from the client generator's `@tanstack/react-query` plugin, so entries from every route share one shape.

Forms build on the form hook of the shared package: one entry point gives every form the same field setup and the same display for a refusal on its field. Sentences live in locale dictionaries, one dictionary per language, under the contract stated in [Web Data and State](../data.md): the same fill, plural, and fallback behavior in every language the product ships.

### What Tests Leave in Place

The mocks rule names the libraries a test leaves in place: the rendering library, the router, and the query library, because a stand-in for any of them tests the stand-in instead of the screen. For example, the library loan list route test renders with the real router and query library and replaces only the network call.

### How Shared UI Is Built

The shadcn/ui registry is built with the `shadcn` CLI, pinned in the manifest as `shadcn`. One registry command builds the UI artifacts the same way in every React frontend.

The state-to-tone table maps each application state to one tone. One table keeps every state in the same tone on every route: the library product maps its overdue state to its warning tone in `src/app/states.tsx`.

### Which Versions Are Pinned

The `stack.node` key in `litespec.manifest.yaml` pins the Node version, the `stack.pnpm` key pins pnpm, and the `packages.npm` keys pin every npm package version. The keys name what is pinned; the manifest holds the pinned values.

## Standards

### Keep the React Frame, Routes, and Modules

**ID:** `litespec/frontend/web/react/stack#keep-the-react-frame-routes-and-modules`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A React frontend MUST hold the frame in `src/app`, one file per address in `src/routes`, and the use-case code in `src/modules`.

**Rationale:** Three fixed homes let a reader and a tool find the frame, the routes, and the use-case code in the same place in every React frontend.

**Example:** The library product keeps its frame in `src/app`, its loan list route in `src/routes/loans.tsx`, and its loan use-case code in `src/modules/loans`.

**Reviewed for:** Does every file under `src` sit in `src/app`, `src/routes`, or `src/modules`, with one file per address? Check the `src` tree of each React frontend.

### Use the TSX File Extension on Shared Files

**ID:** `litespec/frontend/web/react/stack#use-the-tsx-file-extension-on-shared-files`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A shared React file MUST use the `.tsx` extension.

**Rationale:** One extension lets the compiler, the lint, and a search treat every shared React file the same way.

**Example:** The library product holds its shared loan status helper as `src/app/loan-status.tsx`.

**Reviewed for:** Does every shared React file use the `.tsx` extension? Check the `src` tree of each React frontend.

### Hold a Route's Parts in Its Route File

**ID:** `litespec/frontend/web/react/stack#hold-a-routes-parts-in-its-route-file`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route file MUST hold its search parameters, its loader, and its component.

**Rationale:** One file for the three parts lets a reader see an address, its inputs, its data, and its screen together.

**Example:** The library loan list keeps its page number, its loader, and its component in its route file.

**Reviewed for:** Does every route file hold its search parameters, its loader, and its component? Check each file under `src/routes`.

### Declare Route Search Parameters in validateSearch

**ID:** `litespec/frontend/web/react/stack#declare-route-search-parameters-in-validatesearch`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route MUST declare its search parameters in `validateSearch`.

**Rationale:** A declared check runs before the route reads a parameter, so the route receives a typed value or a refusal.

**Example:** The library loan list declares its page number in `validateSearch`, so the loader reads a checked number.

**Reviewed for:** Does every route declare its search parameters in `validateSearch` before reading them? Check each route file under `src/routes`.

### Prefetch Route Reads in the Loader

**ID:** `litespec/frontend/web/react/stack#prefetch-route-reads-in-the-loader`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route MUST prefetch its reads in its loader.

**Rationale:** A loader fills the query cache before the component draws, so the first render has its data.

**Example:** The library loan list loader fetches the loan page for the checked page number before the component draws.

**Reviewed for:** Does every route prefetch its reads in its loader? Check each loader under `src/routes`.

### Read Route Data with useSuspenseQuery

**ID:** `litespec/frontend/web/react/stack#read-route-data-with-usesuspensequery`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route file MUST read its route data with `useSuspenseQuery` over the key its loader filled.

**Rationale:** The component reads the key its loader filled, so it renders the data the loader fetched.

**Example:** The library loan list component reads its loan page through `useSuspenseQuery` over the key its loader filled.

**Reviewed for:** Does every route data read use `useSuspenseQuery` over the key its loader filled? Check each route file under `src/routes`.

### Load Intent Targets Before Navigation

**ID:** `litespec/frontend/web/react/stack#load-intent-targets-before-navigation`

**Strength:** required

**Applies to:** artifact: route

**Statement:** Intent preloading MUST fill the query cache before navigation.

**Rationale:** A preload on hover or focus fills the cache before the click, so the target route draws from data it already holds.

**Example:** In the library product, hovering the loan link fills the loan page key, and the click draws the list from the cache.

**Reviewed for:** Does every preloaded link fill the query cache before navigation? Check the router link setup in the `src` tree.

### Use Named Props for Class and Style Bindings

**ID:** `litespec/frontend/web/react/stack#use-named-props-for-class-and-style-bindings`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A class or style binding in a React component MUST use `className` or `style`.

**Rationale:** Two fixed prop names keep styling in one vocabulary that the lint and a reviewer can read.

**Example:** The loan row in `src/app/components/LoanRow.tsx` sets its tone through `className` and uses no `style` prop.

**Reviewed for:** Does every class or style binding use `className` or `style`? Check the JSX in the component files.

### Name Drawings with the Visual Suffix

**ID:** `litespec/frontend/web/react/stack#name-drawings-with-the-visual-suffix`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A file that holds a drawing no composite makes MUST end with `.visual.tsx`.

**Rationale:** The suffix identifies the one place outside the composites, so a search finds every custom drawing in one pass.

**Example:** The library loan due-date sparkline lives in `LoanSparkline.visual.tsx`.

**Reviewed for:** Does every file holding a custom drawing end with `.visual.tsx`? Check the component folders.

### Render Raw HTML Only Through dangerouslySetInnerHTML

**ID:** `litespec/frontend/web/react/stack#render-raw-html-only-through-dangerouslysetinnerhtml`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A React component MUST pass raw HTML to the page only through `dangerouslySetInnerHTML`.

**Rationale:** One named prop gives every raw HTML sink a name a search finds, so a review can check each use.

**Example:** The library loan note passes its sanitized HTML through `dangerouslySetInnerHTML`, matching its recorded sanitization decision.

**Reviewed for:** Does every raw HTML write go through `dangerouslySetInnerHTML`? Check for `innerHTML` and raw markup in the component files.

### Show Route Failure in the Route Error Component

**ID:** `litespec/frontend/web/react/stack#show-route-failure-in-the-route-error-component`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route failure MUST render in the route error component, which draws the shared problem state.

**Rationale:** One error component gives every route failure the same problem state and the same recovery.

**Example:** When the library loan list fails to load, the route error component shows the shared problem state with its trace identifier.

**Reviewed for:** Does every route failure render in the route error component? Check the error handling of each route under `src/routes`.

### Apply the Page-Code Lint to Route and Module Code

**ID:** `litespec/frontend/web/react/stack#apply-the-page-code-lint-to-route-and-module-code`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The page-code lint MUST apply import restrictions and JSX element restrictions in route and module folders, with visual files allowlisted.

**Rationale:** A lint refusal that names the composite to use lets the author replace a raw element when writing it, not in review. Visual files stay allowlisted because a drawing no composite makes has no composite to name.

**Example:** In `src/routes/loans.tsx`, the lint refuses a raw `div` and names the `Stack` composite to use instead.

**Reviewed for:** Does any route or module file use a refused import or JSX element without a lint finding? Run the page-code lint, then compare its findings with the files under `src/routes` and `src/modules`.

### Mark Each React Route Entry with Its Screen

**ID:** `litespec/frontend/web/react/stack#mark-each-react-route-entry-with-its-screen`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route entry MUST export its screen mark from its route file as the same typed constant a use-case module exports.

**Rationale:** A mark in the route file ties the address to its use case where the address is declared, so the builder and the check read one link. A wrong identifier fails the consumer's type check against the generated union.

**Example:** The library loan list exports its typed screen mark from its route file for the loan list screen.

**Reviewed for:** Does every route entry export its screen mark from its route file? Check each route file under `src/routes`.

### Keep Layouts, Error Components, and Route Groups Unmarked

**ID:** `litespec/frontend/web/react/stack#keep-layouts-error-components-and-route-groups-unmarked`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A pathless layout route, an error component, or a route group MUST NOT carry a screen mark.

**Rationale:** A layout, an error component, or a group draws no screen of its own, so a mark on it ties an address to a screen it does not show.

**Example:** The library product's underscore layout route carries no screen mark.

**Reviewed for:** Do any layout routes, error components, or route groups carry a screen mark? Check the non-entry files under `src/routes`.

### Add the TanStack Query Plugin to the Client Generator

**ID:** `litespec/frontend/web/react/stack#add-the-tanstack-query-plugin-to-the-client-generator`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The client generator MUST add the `@tanstack/react-query` plugin.

**Rationale:** The plugin gives every generated read a query key, so the cache entries of every route share one shape.

**Example:** The library product's client generator adds `@tanstack/react-query`, giving each generated loan read a query key.

**Reviewed for:** Does every client generator configuration add the `@tanstack/react-query` plugin? Check the generator configuration of each React frontend.

### Resolve Copy Through the Dictionary Library

**ID:** `litespec/frontend/web/react/stack#resolve-copy-through-the-dictionary-library`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The dictionary library MUST meet the locale-dictionary contract that the web data page states.

**Rationale:** One contract keeps every sentence in one dictionary per language, with the same fill, plural, and fallback behavior.

**Example:** The library product resolves its overdue-loan sentence through its dictionary library in every language it ships.

**Reviewed for:** Does every copy call site use the dictionary library with fill, plural, typed keys, and a declared fallback? Check the copy call sites under `src`.

### Use the Shared Package Form Hook

**ID:** `litespec/frontend/web/react/stack#use-the-shared-package-form-hook`

**Strength:** required

**Applies to:** artifact: form

**Statement:** A React form MUST build on the form hook of the shared package.

**Rationale:** One entry point gives every form the same field setup and the same display for a refusal on its field.

**Example:** The library loan request form builds on the shared form hook, with no form library of its own.

**Reviewed for:** Does every React form build on the shared package form hook? Check the form files under `src`.

### Keep React Test Mocks to the Owned Libraries

**ID:** `litespec/frontend/web/react/stack#keep-react-test-mocks-to-the-owned-libraries`

**Strength:** required

**Applies to:** artifact: test

**Statement:** A test MUST NOT replace the rendering library, the router, or the query library.

**Rationale:** Those three are the stack's own behavior, and a stand-in for any of them tests the stand-in instead of the screen.

**Example:** The library loan list route test runs against the real router and query library, replacing only the network call.

**Reviewed for:** Does any test replace the rendering library, the router, or the query library? Check the mocks in each test file.

### Run the Registry Build Command

**ID:** `litespec/frontend/web/react/stack#run-the-registry-build-command`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The registry build command MUST be `shadcn build`.

**Rationale:** One registry command builds UI artifacts the same way in every React frontend.

**Example:** The library product builds its UI registry with `shadcn build`.

**Reviewed for:** Does every registry build script run `shadcn build`? Check the scripts in each frontend's `package.json`.

### Keep the Tone Table in One States File

**ID:** `litespec/frontend/web/react/stack#keep-the-tone-table-in-one-states-file`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The state-to-tone table MUST live in `src/app/states.tsx`.

**Rationale:** One file holds every state's tone, so a state shows the same tone on every route.

**Example:** The library product maps its overdue state to its warning tone in `src/app/states.tsx`.

**Reviewed for:** Does every state-to-tone mapping live in `src/app/states.tsx`? Search the `src` tree for tone maps.

## Conventions

None.
