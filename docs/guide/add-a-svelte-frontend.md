# Add a Svelte Frontend

## Purpose

A product that serves its web interface from Svelte declares the same web application tree as the React frontend in the tutorial, against the same backend use cases. This guide declares that frontend: the stack, the application tree with mirrored use case paths, routes composed from one floorplan, and the pinned baseline. Each step names the standard it satisfies, and the guide adds no rule of its own.

## Prerequisites

Start with:

- A consumer from [Start a Consumer Repository](../tutorial/getting-started.md), with its backend use cases coded.
- The `svelte` stack added to the consumer's `.litespec/project.yaml`.
- The Svelte toolchain release pinned in `litespec.manifest.yaml`.

Read the [web structure](../frontend/web/structure.md) and the [web UI](../frontend/web/ui.md) pages before this guide: they state the obligations, and this guide shows one compliant way to meet them.

## Procedure

### Declare the Svelte Stack

Add the `svelte` stack to `.litespec/project.yaml`, matching the stack entries of the `dotnet-svelte` preset on the [initialize command](../tools/init.md) page. This satisfies `litespec/core/scope#select-a-size-a-shape-and-stacks`.

### Declare the Application Tree

Declare the web application tree and mirror the backend use case path for each route as the [web structure](../frontend/web/structure.md) page states. Check that each route lives at the mirrored path. This satisfies `litespec/frontend/web/structure#use-the-frontend-application-tree`.

### Compose Each Route from One Floorplan

Compose each route from one floorplan. This satisfies `litespec/frontend/ui#compose-each-route-from-one-floorplan`.

### Pin the Baseline in the Shared Package

Pin the shadcn-svelte baseline in the shared package as the [web UI](../frontend/web/ui.md) page states. This satisfies `litespec/frontend/ui#pin-the-baseline-in-the-shared-package`.

## Check

Confirm the result by model check:

- `dotnet litespec model check` exits with no open blocking finding and links each new screen to its use case.
