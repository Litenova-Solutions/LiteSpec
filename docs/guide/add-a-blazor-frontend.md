# Add a Blazor Frontend

## Purpose

A product that serves its interface from Blazor declares a client solution tree instead of the web application tree, with features organized by module and use case. Blazor stays outside the controlled UI contract, so no floorplan or baseline step applies. This guide declares that frontend: the stack entry, the client tree, and the feature layout. Each step names the standard it satisfies, and the guide adds no rule of its own.

## Prerequisites

Start with:

- A consumer from [Start a Consumer Repository](../tutorial/getting-started.md), with its backend use cases implemented.
- The `blazor` stack beside the tutorial pair in the consumer's `.litespec/project.yaml`.
- The .NET SDK release pinned in `litespec.manifest.yaml` at the LiteSpec root.

Read the [Blazor Stack page](../frontend/blazor/stack.md) and the [Frontend Structure page](../frontend/structure.md) before this guide: they state the obligations, and this guide shows one compliant way to meet them.

## Procedure

### Declare the Blazor Stack

Add the `blazor` stack to `.litespec/project.yaml`, following the stack entries of the `blazor-client` preset on the [initialize command page](../tools/init.md); the consumer keeps its server shape. This satisfies `litespec/core/scope#select-a-size-a-shape-and-stacks`.

### Declare the Client Solution Tree

Declare the client solution tree with its frame, routes, and use-case modules. This satisfies `litespec/frontend/structure#organize-a-frontend-into-frame-routes-and-modules`.

### Organize Features by Module and Use Case

Organize features by module and use case. This satisfies `litespec/frontend/structure#mirror-the-backend-use-case-path-in-kebab-case`.

## Check

- The client solution builds with no error and no analyzer finding outside a recorded exception.
- `litespec model check` links each new screen to its use case with no finding.
