# Blazor Stack

## Intent

The Blazor stack is the technology binding for a product that has no server of its own: a .NET domain model, browser-local persistence, and a Blazor WebAssembly application compiled to static files. A team chooses it when the product runs entirely on the user's device, under the `client-only` shape, the deployment boundary with no server, declared in [Supported Scope](../../core/scope.md). This page states what the stack itself requires; exact framework and package versions live only in `litespec.manifest.json`.

## Concepts

### Route Entry Bindings

A route entry is a component that declares an `@page` template. Its screen mark sits on that component in the .NET mark style the project declares in `litespec.project.json`, read by the .NET reader. Layout components, components with no `@page` template, and error-handling components are not route entries and carry no screen mark.

## Standards

### Keep Blazor Outside the UI Contract

**ID:** `litespec/frontend/blazor/stack#keep-blazor-outside-the-ui-contract`

**Statement:** A consumer selecting this stack MUST NOT apply the floorplan, token, and route-suite contract of the controlled UI pages.

**Rationale:** That contract's floorplan, token, and route-suite checks read TypeScript sources, so they cannot read a `.razor` component. Applying them to this stack would fail every check for lack of readable input. The Blazor pages state the styling, accessibility, and state rules that apply instead, so nothing is left ungoverned. Screen records and route marks still apply on this stack: the .NET reader reads the `@page` component marks, and the screen, `calls`, and `refusal-sentence-missing` checks run from those marks.

**Reviewed for:** Which stack selections or CI jobs apply a floorplan, token, or route-suite check, and which `@page` components lack a screen mark the .NET reader reads? Look in the stack selection, the CI jobs, and the `@page` components.

### Publish Static WebAssembly Output

**ID:** `litespec/frontend/blazor/stack#publish-static-webassembly-output`

**Statement:** A consumer MUST publish a standalone Blazor WebAssembly application to static files.

**Rationale:** Standalone WebAssembly compiles to static files, which is what lets the product run with no server and offline. Blazor Server, interactive server rendering, and prerendering each reintroduce a server dependency, which stops the product working offline and contradicts the `client-only` shape.

**Reviewed for:** Which published output holds a server assembly or a server render mode? Look in the published output.

## Conventions

### Verify with the .NET Toolchain

**ID:** `litespec/frontend/blazor/stack#verify-with-the-net-toolchain`

**Default:** Run client verification through `dotnet` rather than a Node package manager.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** The client has no Node build step, so `dotnet` builds and tests the whole application. A Node toolchain would add downloads and configuration for a step that does not exist. A consumer holding a separate content or asset workspace keeps the manifest-pinned Node and pnpm toolchain for that workspace only.

**Example:**

```bash
dotnet build apps/web/{ProjectName}.slnx --configuration Release
dotnet test apps/web/{ProjectName}.slnx --configuration Release --no-build
```

**Reviewed for:** Which CI client jobs skip `dotnet build` or `dotnet test`, or add a Node build step? Look in the CI client job.

### Record a First-Load Budget

**ID:** `litespec/frontend/blazor/stack#record-a-first-load-budget`

**Default:** Record the compressed transfer size of the framework payload as a limit entry in `docs/behavior/operations/limits.md`.

**Replacement:** A consumer can replace this default with an explicit local convention.

**Rationale:** A WebAssembly client downloads a runtime before it renders anything, so first load is the slowest moment of the product. A recorded budget on compressed transfer size makes that wait a reviewed number instead of a surprise. Directory size does not describe the wait, because the visitor downloads compressed bytes over the network.

**Reviewed for:** Which first-load budgets miss their limit entry in `docs/behavior/operations/limits.md`? Look in the limits file beside the publish job.
