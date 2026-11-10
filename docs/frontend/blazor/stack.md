# Blazor Stack

**Stack:** blazor
**Concern:** frontend-ui

## Intent

The Blazor stack is the technology binding for a product that has no server of its own: a .NET domain model, browser-local persistence, and a Blazor WebAssembly application compiled to static files. A team chooses it when the product runs entirely on the user's device, under the `client-only` shape declared in [Supported Scope](../../core/scope.md). Exact framework and package versions live only in `litespec.manifest.yaml`.

## Concepts

### What the Stack Holds

The stack is one Blazor WebAssembly application holding the whole product on the user's device: the domain model, the application handlers, the browser adapters, and the pages and features. It publishes to static files, so the product starts with no server and keeps working offline after the first load. The .NET toolchain builds, tests, and publishes the whole client. The shared frontend pages in the [Frontend overview](../README.md) state the rules the stack builds on.

### Version Pins

`litespec.manifest.yaml` pins the .NET version the stack builds with under `stack.dotnet`, and every NuGet package version under `packages.nuget`. These keys name the pins; the manifest holds the values.

## Standards

None.

## Conventions

None.
