# Svelte Golden Slice

**Stack:** svelte

**Concern:** frontend-ui

A new Svelte frontend starts from one small working slice with two addresses and real data. This page is for teams who copy that slice for each new job instead of inventing a second layout. A slice outside the locked archetypes splits the product before it grows.

## Concepts

### What the Slice Contains

The slice shows the health entries of the backend golden slice through a list at `/health` and a record at `/health/[id]`. The list fills the list archetype slots with header, filter bar, table, and pagination. The record fills the record archetype slots with header, URL-backed tabs, main sections, and summary panel.

```text
src/lib/app/ui/states.ts
src/lib/modules/health/queries.ts
src/routes/health/+page.ts
src/routes/health/+page.svelte
src/routes/health/[id]/+page.ts
src/routes/health/[id]/+page.svelte
packages/api/src/litespec/health.ts
```

The file set above holds the frame states, the module queries, the two loaders and pages, and the contract source. Both routes read through the same query client the root layout provides. The stack page states the alias and rendering modes, and the primitives page states the archetype slots each route fills.

### How Data Flows Through the Slice

The list loader prefetches the health entries into the query cache before the page draws. The page reads back the key the loader filled. The record loader prefetches one entry by identifier, and its tabs share that cached entry instead of fetching again.

A submit on the record invalidates the list key, so the list shows the new state on return. Hovering a list row preloads the record cache, so the target draws from data it holds. The Claimwick expense list prefetches its entry page in `+page.ts` and reads it back over the same key in `+page.svelte`.

### How the Slice Is Proved

The slice keeps one route test with the real query library and a replaced network call. One probe opens the list and follows one row to its record. Playwright captures each changed route beside the locked archetype images, and the visual critic scores alignment, spacing rhythm, hierarchy, symmetry, and single-job focus.

A route file stays under the line budget, so review checks slot filling instead of layout code. The health list test renders with the real query client, and `litespec ui review` captures the list beside the locked list baseline. The information architecture page states the sitemap and the archetypes, and the verification page states the workflow and the critic.

## Standards

None.

## Conventions

None.
