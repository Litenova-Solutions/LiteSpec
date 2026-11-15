# Web Rendering and Routes

**Stack:** react, svelte
**Concern:** frontend-ui

A route in a TypeScript web frontend carries metadata deciding how links and search meet it. This page states the metadata rule the [React Stack](react/stack.md) and [Svelte Stack](svelte/stack.md) add to the rendering rules in [Frontend Rendering and Routes](../rendering.md). A team that applies it while writing routes keeps every public route linkable and every private route out of search.

## Concepts

### Rules Kept on the Base Page

The rendering rules for loaders, suspense reads, search schemas, chunk splits, intent preloading, authenticated caching, proxies, the server cache, and shells live on [Frontend Rendering and Routes](../rendering.md). The web stacks add only the metadata rule this page states.

- Loader prefetch: `litespec/frontend/rendering#load-route-data-before-rendering`
- Suspense read: `litespec/frontend/rendering#read-route-data-from-the-route-cache`
- Search schema: `litespec/frontend/rendering#validate-address-parameters-with-a-schema`
- Chunk split: `litespec/frontend/rendering#load-each-route-and-each-language-separately`
- Intent preload: `litespec/frontend/rendering#prefetch-a-route-on-intent`
- Authenticated caching: `litespec/frontend/rendering#keep-authenticated-caching-explicit`
- Proxy behavior: `litespec/frontend/rendering#keep-proxy-behavior-at-the-edge`
- Server cache: `litespec/frontend/rendering#build-the-server-data-cache-per-request`
- Shell layout: `litespec/frontend/rendering#use-a-shared-layout-for-a-shell`

## Standards

### Declare Titles and Indexing Policy on Every Route

**ID:** `litespec/frontend/web/rendering#declare-titles-and-indexing-policy-on-every-route`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A public route MUST define its title, its description, its canonical address, and its indexing policy, while a private route refuses indexing.

**Rationale:** A public route's title, description, canonical address, and indexing policy are what a search result and a shared link show. Without them the page arrives as a bare address. A private route refuses indexing, because an indexed console address exposes to search what a signed-in user believes is private.

**Example:** A channel route states `og:*` tags and one alternate per language. A console route ships a `noindex` meta tag in its document.

**Reviewed for:** Does every public route define its title, description, canonical address, and indexing policy, with every private route carrying a `noindex` marker? Check the route metadata.

## Conventions

None.
