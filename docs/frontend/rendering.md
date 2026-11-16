# Frontend Rendering and Routes

**Stack:** any
**Concern:** frontend-ui

A frontend route serves one web address with its data in place, inside one floorplan. A floorplan is the page component that fixes the page layout.

This page states the rules for loading route data and rendering each address. Frontend developers and agents apply it to every address the product serves.

## Concepts

### The Route, Its Cache, and Its View

A route is the entry for one address. It declares its parameters and loads each read it needs through one shared cache. Its view reads the same cache entries the route filled, so the page renders with its data instead of showing a spinner after it appears.

The cache is the only place that tracks freshness: neither the router nor the view keeps a second copy. The view renders inside one floorplan, as [Controlled UI Governance](ui.md) states.

### Addresses Carry Their Own Parameters

A route declares its address parameters, including filters and page, in a validated schema. The address then holds the filter values, so a filtered list becomes a link a reader can share, bookmark, and reopen with the same view. A filter kept in component state cannot be shared, and it is lost when the reader navigates away.

### Route States

A route can reach six states: loading, empty, error, forbidden, not-found, and ready. Defining each state before building the route gives every pending or failed request a designed rendering instead of a blank region.

## Standards

### Load Route Data Before Rendering

**ID:** `litespec/frontend/rendering#load-route-data-before-rendering`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route MUST load each read it needs through one shared cache before its view renders.

**Rationale:** A view that renders before its reads complete shows a spinner after it appears, then replaces the spinner with content: two drawings for one navigation, with layout shifting between them. Loading each read through one shared cache puts the data in place for the first drawing, and the router keeps no second copy that could disagree.

**Example:** An expense record route loads the expense and every read its sections need, before the view renders.

**Reviewed for:** Does every route load each read it needs through the one shared cache before its view renders? Check the route loaders and the cache definitions.

### Read Route Data from the Route Cache

**ID:** `litespec/frontend/rendering#read-route-data-from-the-route-cache`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A view MUST read route data through the same cache entry its route filled.

**Rationale:** A view that reads under another key issues a second request for data the route already loaded, and the two answers can disagree when the data changes between them. Reading through the same entry renders the data the route loaded. A failed read reaches the route's error boundary, which draws the shared problem state instead of a half-rendered view.

**Reviewed for:** Does every view read through the cache entry its route filled, and does every failed read reach the route error boundary? Check the view data reads and the route loaders.

### Validate Address Parameters with a Schema

**ID:** `litespec/frontend/rendering#validate-address-parameters-with-a-schema`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route MUST declare its address parameters in a validated schema, including its filters and its page.

**Rationale:** Without a schema, a malformed address renders a page from invalid values, and a filter kept outside the address is lost on the first share or navigation away. A validated schema makes the address the complete input: a shareable address restores the same filters and page, and validation refuses a malformed address at the boundary.

**Example:** The expense list schema declares its own text field and spreads the shared paging fields.

**Reviewed for:** Does every route declare its address parameters, filters, and page in a validated schema? Check the route parameter schemas.

### Load Each Route and Each Language Separately

**ID:** `litespec/frontend/rendering#load-each-route-and-each-language-separately`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A frontend MUST load each route and each language as its own download, with no icon set or language loaded whole.

**Rationale:** A user opens one address and reads one language, so downloading every route and every language at first load wastes time on code and copy outside that path. Each route and each language ships as its own download: a user in Dutch who stays on the dashboard downloads neither English nor the routes outside that path. For the same reason, no icon set or language loads whole.

**Example:** A dictionary loader returns one language, not the whole dictionary. Each route file or folder ships as its own download.

**Reviewed for:** Does every route and every language ship as its own download, with no icon set or language loaded whole? Check the route bundles and the dictionary loader.

### Prefetch a Route on Intent

**ID:** `litespec/frontend/rendering#prefetch-a-route-on-intent`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A frontend MUST prefetch the route the user signals intent toward, without treating that prefetch as a cache beyond the shared cache.

**Rationale:** When the user hovers a link or focuses a menu item, prefetching loads that route's data into the shared cache before the navigation starts, so most navigations render from data already in place. The prefetch fills the shared cache only, because a router keeping its own second cache would serve a stale answer beside the fresh one.

**Reviewed for:** Does every route prefetch land in the shared cache, with no second router cache? Check the router prefetch wiring.

### Define Every Route State

**ID:** `litespec/frontend/rendering#define-every-route-state`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A data-driven route MUST define its loading, empty, error, forbidden, not-found, and ready behavior.

**Rationale:** A blank region while a request is pending or failed gives the user no signal: a reader cannot tell a loading page from a broken one. Defining each state in advance gives each state a designed drawing, and the root not-found handling draws the shared outcome page for an absent address.

A route has these six states and no others. A control adds `disabled` and `pending` under `litespec/frontend/components#render-control-states`, because both describe a control rather than a route. A route adds `not-found`, which a control does not have, because a missing target resolves at the route.

**Reviewed for:** Does every data-driven route define its loading, empty, error, forbidden, not-found, and ready behavior, with an absent address on the shared outcome page? Check the route state definitions.

### Move Focus on a Route Change

**ID:** `litespec/frontend/rendering#move-focus-on-a-route-change`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route change inside the application MUST move keyboard focus to the new route's top-level heading.

**Rationale:** A full page load starts focus and the screen reader at the top of the new page. A client-side navigation instead replaces the content while focus stays on the link just used, or falls back to the page body. A keyboard user then tabs through the old position, and a screen-reader user hears nothing change. Moving focus to the heading reads the new page's title and starts the next Tab from there, as WCAG 2.4.3 Focus Order expects.

**Example:** Opening EXP-1042 from the expense list lands focus on the heading "EXP-1042, Ava, EUR 125.00".

**Reviewed for:** Does every route move focus to the new route's top-level heading after a navigation inside the application? Check the route suite output.

### Keep Authenticated Caching Explicit

**ID:** `litespec/frontend/rendering#keep-authenticated-caching-explicit`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** Actor-specific data MUST declare its cache key, partition, invalidation owner, and security review before caching, beside the read definition setting the key.

**Rationale:** A cache entry that ignores whose data it holds serves one actor's answer to another user. Declaring the cache key, the partition, the invalidation owner, and the security review beside the read definition forces each cached actor-specific read to name its boundary before it caches. An uncached read is the safe default, and a cache partitioned per request is the safe server shape.

**Reviewed for:** Does every cached actor-specific read declare its cache key, partition, invalidation owner, and security review beside the read definition? Check the read definitions setting cache keys.

### Keep Proxy Behavior at the Edge

**ID:** `litespec/frontend/rendering#keep-proxy-behavior-at-the-edge`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A development or edge proxy rule MUST route only, and never decide who the actor is or what the actor may read.

**Rationale:** A development or edge proxy runs before the request reaches the API, without access to the record or the permissions an authorization decision depends on. A proxy rule that decided who the actor is or what the actor may read would enforce a different boundary than the API, and the two would disagree. Leaving identity and authorization to the API keeps one boundary instead of two.

**Example:** A development proxy forwards `/api` and `/auth` to the API and changes nothing else: no header decides the actor, and no rule filters what the actor may read.

**Reviewed for:** Does every development and edge proxy rule route only, deciding neither identity nor authorization? Check the proxy rules.

### Build the Server Data Cache per Request

**ID:** `litespec/frontend/rendering#build-the-server-data-cache-per-request`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A server-rendered frontend MUST construct its data cache inside each request.

**Rationale:** A server renders for many users at once. A data cache shared across requests serves the next user the previous user's answer, leaking one actor's data to another with no error to report it. Constructing the cache inside each request gives every user a fresh cache that is discarded with the response.

**Reviewed for:** Does every server-rendered frontend construct its data cache inside each request, reusing none across requests? Check the server request setup.

## Conventions

### Use a Shared Layout for a Shell

**ID:** `litespec/frontend/rendering#use-a-shared-layout-for-a-shell`

**Strength:** default

**Applies to:** artifact: route

**Default:** Give a shell a layout holding the frame every child route shares.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** A shell is the frame every child route shares: navigation, header, and providers. A shared layout holds it without adding an address segment, so the shell adds nothing to links and every child route renders inside the same frame.

**Reviewed for:** Does every shell sit in a shared layout, adding no address segment? Check the layout definitions.

### Keep Layouts Stable

**ID:** `litespec/frontend/rendering#keep-layouts-stable`

**Strength:** default

**Applies to:** artifact: route

**Default:** Keep a layout to the frame and the providers every child route needs.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** A layout wraps every route beneath it. A feature-specific provider placed in a shared layout forces unrelated routes to carry its download, its state, and its failure modes. The layout therefore holds only what every child needs, so each route carries only the cost of what it uses.

**Reviewed for:** Does every layout hold only the frame and the providers every child route needs? Check the layout definitions.

### Split Long Client Work

**ID:** `litespec/frontend/rendering#split-long-client-work`

**Strength:** default

**Applies to:** artifact: route

**Default:** Split client work that runs longer than 50 milliseconds, such as an import preview or a report total, into chunks that give the browser a turn between them.

**Replacement:** A consumer can replace this default by moving the work to a worker thread or to the server, and naming where it runs.

**Rationale:** While script runs, the page answers no click or key press. A staff member previewing 1,204 imported payouts sees a frozen screen and clicks again. Giving the browser a turn between chunks keeps every input answered within one frame, and keeps the measured total blocking time low.

**Reviewed for:** Does every client task longer than 50 milliseconds yield between chunks? Check the browser performance traces of the heaviest routes.

### Keep Server-Only Code Identifiable

**ID:** `litespec/frontend/rendering#keep-server-only-code-identifiable`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep a module reading a request header, a cookie, or a server secret in a file whose name and place show the server ownership.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** A module that reads a request header, a cookie, or a server secret breaks when it runs in the browser, and it can include the secret in the client download. A file whose name and place show server ownership turns an accidental browser import into a build error, and shows the same fact to a reader browsing the tree.

**Reviewed for:** Does every module reading a request header, cookie, or server secret sit in a file named and placed for the server? Check the server file names and places.
