# Web Rendering and Routes

**Stack:** svelte
**Concern:** frontend-ui

A route in the Svelte frontend carries metadata that decides what links and search show for it. This page adds the metadata rule for Svelte routes: every public route defines its title, description, canonical address, and indexing policy, and every private route refuses indexing.

## Concepts

### Rules the Svelte Stack Follows

Each route prefetches its data before it draws, reads it back from the route cache, validates its address parameters against a schema, ships each route and each language as its own chunk, preloads on intent, keeps authenticated caching explicit, holds proxy behavior at the edge, builds the server data cache per request, and shares one layout for a shell. The Svelte stack adds only the metadata rule this page states.

## Standards

### Declare Titles and Indexing Policy on Every Route

**ID:** `litespec/packs/svelte/rendering#declare-titles-and-indexing-policy-on-every-route`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A public route MUST define its title, its description, its canonical address, and its indexing policy, while a private route refuses indexing.

**Rationale:** A public route's title, description, canonical address, and indexing policy are what a search result and a shared link show. Without them, the result shows only a bare address. A private route refuses indexing, because an indexed console address exposes to search what a signed-in user believes is private.

**Example:** A channel route states `og:*` tags and one alternate per language. A console route carries a `noindex` meta tag in its document.

**Reviewed for:** Does every public route define its title, description, canonical address, and indexing policy, with every private route carrying a `noindex` marker? Check the route metadata.

### Show Route Failure on the Error Surface

**ID:** `litespec/packs/svelte/rendering#show-route-failure-on-the-error-surface`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route failure MUST render on `+error.svelte`, which draws the shared problem state.

**Rationale:** One error surface gives every route failure the same problem state and the same recovery.

**Example:** When the Claimwick expense list fails, `+error.svelte` shows the shared problem state with its trace identifier.

**Reviewed for:** Does every route failure render on the stated error surface? Check the error handling of each route.

## Conventions

None.
