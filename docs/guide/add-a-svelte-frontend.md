# Add a Svelte Frontend

Add a Svelte frontend to a consumer whose backend use cases already run. You add the stack entry, the application tree with its mirrored paths, the floorplan-composed routes, and the pinned baseline. The frontend keeps the same shape as the frontend in the tutorial: one web application tree, with routes that mirror the backend use case paths they serve. Each step names the standard it satisfies, and this guide adds no rule of its own.

## Prerequisites

Start with:

- A consumer from [Start a Consumer Repository](../tutorial/getting-started.md), with its backend use cases coded.
- The Svelte toolchain pins in `litespec.manifest.yaml`.

Read the [web structure](../packs/svelte/structure.md) and the [web UI](../packs/svelte/ui-primitives.md) pages before this guide. They state the obligations, and this guide shows one compliant way to meet them.

## Procedure

1. List the `svelte` stack in `.litespec/project.yaml`, matching the stack entries of the `dotnet-svelte` preset on the [initialize command](../tools/init.md) page. For example, a Claimwick consumer whose backend already runs adds `svelte` and keeps the backend entry untouched. This satisfies `litespec/core/scope#select-a-size-a-shape-and-stacks`.
2. Declare the web application tree and mirror the backend use case path for each route as the [web structure](../packs/svelte/structure.md) page states. Check that each route lives at the mirrored path. For example, the route that serves the expense submit use case lives at the web path that mirrors the submit use case path. This satisfies `litespec/packs/svelte/structure#use-the-frontend-application-tree`.
3. Compose each route from one floorplan, as the [UI](../core/ui-ia.md) page states. For example, the expense list route composes the list floorplan and fills only its declared slots. This satisfies `litespec/core/ui-ia#compose-each-route-from-one-locked-archetype`.
4. Pin the shadcn-svelte baseline in the shared package as the [web UI](../packs/svelte/ui-primitives.md) page states. For example, the shared package records the pinned baseline version, and the expense form builds from its controls. This satisfies `litespec/packs/svelte/ui-primitives#keep-shadcn-svelte-close-to-upstream`.

## Check

Confirm the result by model check:

- `dotnet litespec model check` exits with no open blocking finding, and each new screen links to its use case.
