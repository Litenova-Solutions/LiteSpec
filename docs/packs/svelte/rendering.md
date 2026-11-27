# Web Rendering and Routes

**Stack:** svelte
**Concern:** frontend-ui

A Svelte route meets the reader through its link text and its search result. This page is for developers who define route titles and indexing for a Svelte frontend. A public route without a title shows a bare address, and a private console address in search results exposes what readers treat as private.

## Concepts

### How Routes Describe Themselves

A public route carries a title, a description, a canonical address, and an indexing policy. Search results and shared links read those four values. A console route carries a `noindex` marker, so search tools skip the address.

A failing route draws the shared problem state on the error surface. The Claimwick expense list shows that state with its trace identifier when the list load fails. One surface keeps every failure in the same shape with the same recovery.

## Standards

### Declare Titles and Indexing Policy on Every Route

**ID:** `litespec/packs/svelte/rendering#declare-titles-and-indexing-policy-on-every-route`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A public route MUST define its title, its description, its canonical address, and its indexing policy, while a private route refuses indexing.

**Rationale:** Link text and search results read the title, description, canonical address, and indexing policy. A bare address tells the reader nothing about the page behind it. An indexed console address puts a signed-in view in public search, so private routes carry `noindex`.

**Example:** A channel route states `og:*` tags and one alternate per language. A console route carries a `noindex` meta tag in its document.

**Reviewed for:** Does every public route define its title, description, canonical address, and indexing policy, with every private route carrying a `noindex` marker? Check the route metadata.

### Show Route Failure on the Error Surface

**ID:** `litespec/packs/svelte/rendering#show-route-failure-on-the-error-surface`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route failure MUST render on `+error.svelte`, which draws the shared problem state.

**Rationale:** One error surface gives every route failure the same problem state and the same recovery. Readers learn one shape and follow one path back to working state.

**Example:** When the Claimwick expense list fails, `+error.svelte` shows the shared problem state with its trace identifier. The reader copies the identifier for support and follows the recovery action on the page.

**Reviewed for:** Does every route failure render on the stated error surface? Check the error handling of each route.

## Conventions

None.
