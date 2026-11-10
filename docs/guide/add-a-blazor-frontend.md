# Add a Blazor Frontend

## Purpose

A product that serves its interface from Blazor runs as a client with no server of its own, with the domain model, handlers, and pages in one Blazor WebAssembly application. Blazor stays outside the controlled UI contract, so no floorplan or baseline step applies. This guide declares that frontend for a `client-only` project: the stack, the project shape, and the feature layout. Each step names the standard it satisfies, and the guide adds no rule of its own.

## Prerequisites

Start with:

- A consumer project with the `client-only` shape, or an empty folder where you run `litespec init --preset blazor-client --size small` as the [initialize command](../tools/init.md) describes.
- The .NET SDK release pinned under `stack.dotnet` in `litespec.manifest.yaml`.

Read the [Blazor stack](../frontend/blazor/stack.md), the [supported scope](../core/scope.md), and the [frontend structure](../frontend/structure.md) pages before this guide: they state the obligations, and this guide shows one compliant way to meet them.

## Procedure

### Declare the Blazor Stack

Set `shape` to `client-only` and list `blazor` as the only stack in `.litespec/project.yaml`, matching the `blazor-client` preset on the [initialize command](../tools/init.md) page. A `client-only` project names no backend stack. This satisfies `litespec/core/scope#select-a-size-a-shape-and-stacks` and `litespec/core/scope#declare-a-consumer-that-has-no-backend`.

### Declare the Client Application

Keep the domain model, the application handlers, the browser adapters, and the pages in the one Blazor WebAssembly application that the [Blazor stack](../frontend/blazor/stack.md) describes. Organize the application into a frame, routes, and use-case modules. This satisfies `litespec/frontend/structure#organize-a-frontend-into-frame-routes-and-modules`.

### Organize Features by Module and Use Case

Place each feature in the module and use case it serves, mirroring the backend use case path in kebab case where the product keeps that path. This satisfies `litespec/frontend/structure#mirror-the-backend-use-case-path-in-kebab-case`.

## Check

Confirm the result by build and by model check:

- The client application builds with no error and no analyzer finding outside a recorded departure.
- `dotnet litespec model check` links each screen to its use case with no finding.
