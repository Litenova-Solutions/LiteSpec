# React Stack

**Stack:** react

**Concern:** frontend-ui

## Intent

A React frontend takes one of three rendering modes, chosen by who reads it: a static application for an authenticated console, server rendering for a public surface a user reaches by link or search, and one static page with no router for a device client. A team opening a React frontend chooses the mode under [Web Rendering](../rendering.md) before writing routes, and finds on this page the names, files, and libraries the shared web pages leave to this stack. The shared stack rules in [Technology Stacks](../../../core/stacks.md) apply to every stack.

## Concepts

### Version Pins

The `stack.node` key in `litespec.manifest.yaml` pins the Node version, the `stack.pnpm` key pins pnpm, and the `packages.npm` keys pin every npm package version. These keys name the pins; the manifest holds the values.

### Structure Bindings

React names the three frontend parts as folders: `src/app` for the frame, `src/routes` for the addresses, and `src/modules` for the use-case code. The router generates the typed route tree from the files under `src/routes`. Shared files use the `.tsx` extension.

### Rendering Bindings

A React route is a file under `src/routes` that uses the router file convention, and the router builds the typed route tree from it. A shell is a pathless layout route whose file name starts with an underscore. Server-only code lives in a file whose name and import show the server ownership.

### Component Bindings

A class or style binding spells the `className` or `style` prop. A drawing that no composite makes lives in a visual file. Raw HTML is the `dangerouslySetInnerHTML` prop. The route error surface is the route error component, which draws the shared problem state. Component authoring follows [Frontend Components](../../components.md) and [TypeScript Web Components](../components.md). The page-code lint enforces the refusals of [TypeScript Web Components](../components.md) in route and module folders.

### Route Entry Bindings

A route entry is one file under `src/routes` that declares a route for an address. Pathless layout routes, error components, and route groups are not route entries, so they hold no screen mark.

### Data Bindings

Route data lives in the query cache. A component's own state is local component state. The shared package's form entry point is its form hook, and the locale-dictionary contract is stated in [Web Data and State](../data.md).

### Testing Bindings

The mocks rule names the libraries a test must not replace.

### UI Bindings

The shadcn/ui registry is built with the `shadcn` CLI, pinned in the manifest as `shadcn`. The state-to-tone table maps each application state to one tone.

## Standards

### Keep the React Frame, Routes, and Modules

**ID:** `litespec/frontend/web/react/stack#keep-the-react-frame-routes-and-modules`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A React frontend MUST hold the frame in `src/app`, one file per address in `src/routes`, and the use-case code in `src/modules`.

**Rationale:** Three fixed homes let a reader and a tool find the frame, the routes, and the use-case code in the same place in every React frontend.

**Example:** In the library product, the frame sits in `src/app`, the loan list route is `src/routes/loans.tsx`, and the loan use-case code sits in `src/modules/loans`.

**Reviewed for:** Which files under `src` sit outside `src/app`, `src/routes`, and `src/modules`, or which address has two files under `src/routes`? Look at the `src` tree of each React frontend.

### Use the TSX Extension on Shared Files

**ID:** `litespec/frontend/web/react/stack#use-the-tsx-extension-on-shared-files`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A shared React file MUST use the `.tsx` extension.

**Rationale:** One extension lets the compiler, the lint, and a search treat every shared React file the same way.

**Example:** In the library product, the shared loan status helper is `src/app/loan-status.tsx`.

**Reviewed for:** Which shared React files use an extension other than `.tsx`? Look at the `src` tree of each React frontend.

### Hold a Route's Parts in Its Route File

**ID:** `litespec/frontend/web/react/stack#hold-a-routes-parts-in-its-route-file`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route file MUST hold its search parameters, its loader, and its component.

**Rationale:** One file for the three parts lets a reader see an address, its inputs, its data, and its screen together.

**Example:** The library loan list route file holds its page number, its loader, and its component.

**Reviewed for:** Which route files keep their search parameters, loader, or component in another file? Look at each file under `src/routes`.

### Declare Route Search Parameters in validateSearch

**ID:** `litespec/frontend/web/react/stack#declare-route-search-parameters-in-validatesearch`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route MUST declare its search parameters in `validateSearch`.

**Rationale:** A declared check runs before the route reads a parameter, so the route receives a typed value or a refusal.

**Example:** The library loan list route declares its page number in `validateSearch`.

**Reviewed for:** Which routes read a search parameter without declaring it in `validateSearch`? Look at each route file under `src/routes`.

### Prefetch Route Reads in the Loader

**ID:** `litespec/frontend/web/react/stack#prefetch-route-reads-in-the-loader`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route MUST prefetch its reads in its loader.

**Rationale:** A loader fills the query cache before the component draws, so the first render has its data.

**Example:** The library loan list loader fetches the loan page for the page number that `validateSearch` checked.

**Reviewed for:** Which routes read data in their component without a loader prefetch? Look at each loader under `src/routes`.

### Read Route Data with useSuspenseQuery

**ID:** `litespec/frontend/web/react/stack#read-route-data-with-usesuspensequery`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route file MUST read its route data with `useSuspenseQuery` over the key its loader filled.

**Rationale:** Reading the filled key keeps the component on the data the loader fetched.

**Example:** The library loan list component reads its page with `useSuspenseQuery` over the same key the loader filled.

**Reviewed for:** Which route data reads use another query hook or a key the loader does not fill? Look at each route file under `src/routes`.

### Load Intent Targets Before Navigation

**ID:** `litespec/frontend/web/react/stack#load-intent-targets-before-navigation`

**Strength:** required

**Applies to:** artifact: route

**Statement:** Intent preloading MUST fill the query cache before navigation.

**Rationale:** A preload on hover or focus fills the cache before the click, so the target route draws from data it already holds.

**Example:** Hovering the loan link fills the loan page key, so the click draws the loan list from the cache.

**Reviewed for:** Which links preload a route without filling the query cache? Look at the router link setup in the `src` tree.

### Use Named Props for Class and Style Bindings

**ID:** `litespec/frontend/web/react/stack#use-named-props-for-class-and-style-bindings`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A class or style binding in a React component MUST use `className` or `style`.

**Rationale:** Two fixed prop names keep styling in one vocabulary that the lint and a reviewer can read.

**Example:** The loan row in `src/app/components/LoanRow.tsx` sets its tone through `className` and sets no `style` prop.

**Reviewed for:** Which React components bind a class or style through another prop? Look at the JSX in the component files.

### Name Drawings with the Visual Suffix

**ID:** `litespec/frontend/web/react/stack#name-drawings-with-the-visual-suffix`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A file that holds a drawing no composite makes MUST end with `.visual.tsx`.

**Rationale:** The suffix marks the one place outside the composites, so a search finds every custom drawing in one pass.

**Example:** The library loan due-date sparkline is `LoanSparkline.visual.tsx`.

**Reviewed for:** Which custom drawings sit in `.tsx` files whose names lack the `.visual.tsx` suffix? Look in the component folders.

### Render Raw HTML Only Through dangerouslySetInnerHTML

**ID:** `litespec/frontend/web/react/stack#render-raw-html-only-through-dangerouslysetinnerhtml`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A React component MUST pass raw HTML to the page only through `dangerouslySetInnerHTML`.

**Rationale:** One named prop makes every raw HTML sink a name a search finds, and a review can check each use.

**Example:** The library loan note renders its sanitized HTML through `dangerouslySetInnerHTML` after its recorded sanitization decision.

**Reviewed for:** Which React components write raw HTML through another prop or API? Look for `innerHTML` and raw markup in the component files.

### Show Route Failure in the Route Error Component

**ID:** `litespec/frontend/web/react/stack#show-route-failure-in-the-route-error-component`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route failure MUST render in the route error component, which draws the shared problem state.

**Rationale:** One error component gives every route failure the same problem state and the same recovery.

**Example:** A library loan list that fails to load shows the shared problem state with its trace identifier.

**Reviewed for:** Which route failures render outside the route error component? Look at the error handling of each route under `src/routes`.

### Apply the Page-Code Lint to Route and Module Code

**ID:** `litespec/frontend/web/react/stack#apply-the-page-code-lint-to-route-and-module-code`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The page-code lint MUST apply import restrictions and JSX element restrictions in route and module folders, with visual files allowlisted.

**Rationale:** A lint refusal that names the composite to use lets the author replace a raw element when writing it, not in review.

**Example:** The lint refuses a raw `div` in `src/routes/loans.tsx` and names the `Stack` composite to use instead.

**Reviewed for:** Which route or module files use a refused import or JSX element without a lint finding? Run the page-code lint, then compare its findings with the files under `src/routes` and `src/modules`.

### Mark Each React Route Entry with Its Screen

**ID:** `litespec/frontend/web/react/stack#mark-each-react-route-entry-with-its-screen`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route entry MUST export its screen mark from its route file as the same typed constant a use-case module exports.

**Rationale:** A mark in the route file ties the address to its use case where the address is declared, so the builder and the check read one link.

**Example:** The library loan list route file exports the typed screen mark for the loan list screen.

**Reviewed for:** Which route entries export no screen mark from their route file? Look at each route file under `src/routes`.

### Keep Layouts, Error Components, and Route Groups Unmarked

**ID:** `litespec/frontend/web/react/stack#keep-layouts-error-components-and-route-groups-unmarked`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A pathless layout route, an error component, or a route group MUST NOT carry a screen mark.

**Rationale:** A layout, an error component, or a group draws no screen of its own, so a mark on it ties an address to a screen it does not show.

**Example:** The underscore layout route in the library product exports no screen mark.

**Reviewed for:** Which layout routes, error components, or route groups export a screen mark? Look at the non-entry files under `src/routes`.

### Add the TanStack Query Plugin to the Client Generator

**ID:** `litespec/frontend/web/react/stack#add-the-tanstack-query-plugin-to-the-client-generator`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The client generator MUST add the `@tanstack/react-query` plugin.

**Rationale:** The plugin gives every generated read a query key, so the cache entries of every route share one shape.

**Example:** The library product's client generator adds the `@tanstack/react-query` plugin, so each generated loan read has a query key.

**Reviewed for:** Which client generator configurations omit the `@tanstack/react-query` plugin? Look in the generator configuration of each React frontend.

### Meet the Locale-Dictionary Contract with the Dictionary Library

**ID:** `litespec/frontend/web/react/stack#meet-the-locale-dictionary-contract-with-the-dictionary-library`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The dictionary library MUST meet the locale-dictionary contract that the web data page states.

**Rationale:** One contract keeps every sentence in one dictionary per language, with the same fill, plural, and fallback behavior.

**Example:** The library product resolves its overdue-loan sentence through its dictionary library in every language it ships.

**Reviewed for:** Which copy bypasses the dictionary library, or misses fill, plural, typed keys, or a declared fallback? Look at the copy call sites under `src`.

### Use the Shared Package Form Hook

**ID:** `litespec/frontend/web/react/stack#use-the-shared-package-form-hook`

**Strength:** required

**Applies to:** artifact: form

**Statement:** A React form MUST build on the form hook of the shared package.

**Rationale:** One entry point gives every form the same field wiring and the same display for a refusal on its field.

**Example:** The library loan request form uses the shared form hook and no form library of its own.

**Reviewed for:** Which React forms bypass the shared package form hook? Look at the form files under `src`.

### Keep React Test Mocks to the Owned Libraries

**ID:** `litespec/frontend/web/react/stack#keep-react-test-mocks-to-the-owned-libraries`

**Strength:** required

**Applies to:** artifact: test

**Statement:** A test MUST NOT replace the rendering library, the router, or the query library.

**Rationale:** Those three are the stack's own behavior, and a stand-in for any of them tests the stand-in instead of the screen.

**Example:** The library loan list route test renders with the real router and query library and replaces only the network call.

**Reviewed for:** Which tests replace the rendering library, the router, or the query library? Look at the mocks in each test file.

### Run the Registry Build Command

**ID:** `litespec/frontend/web/react/stack#run-the-registry-build-command`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The registry build command MUST be `shadcn build`.

**Rationale:** One registry command builds UI artifacts the same way in every React frontend.

**Example:** The library product builds its registry with `shadcn build`.

**Reviewed for:** Which registry build scripts run a command other than `shadcn build`? Look at the scripts in each frontend's `package.json`.

### Keep the Tone Table in One States File

**ID:** `litespec/frontend/web/react/stack#keep-the-tone-table-in-one-states-file`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The state-to-tone table MUST live in `src/app/states.tsx`.

**Rationale:** One file holds every state's tone, so a state reads the same tone on every route.

**Example:** The library product maps the overdue state to its warning tone in `src/app/states.tsx`.

**Reviewed for:** Which state-to-tone mappings live outside `src/app/states.tsx`? Look for tone maps in the `src` tree.

## Conventions

None.
