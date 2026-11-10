# Add an Android Frontend

## Purpose

A product that serves its interface from Android declares a mobile solution with screens organized by module and hosted in one activity. Android stays outside the controlled UI contract, so no floorplan or baseline step applies. This guide declares that frontend: the stack, the mobile solution tree, and the screen layout. Each step names the standard it satisfies, and the guide adds no rule of its own.

## Prerequisites

Start with:

- A consumer project that declares the `android` stack. A product with a server adds `android` beside its backend stack, and a product with no server starts from the `android-client` preset on the [initialize command](../tools/init.md) page.
- The .NET SDK and Android API level pinned in `litespec.manifest.yaml` under `stack.dotnet` and `stack.android`, with the Android workload installed through `dotnet workload install android`.

Read the [mobile structure](../frontend/mobile/structure.md), the [mobile screens and navigation](../frontend/mobile/screens.md), and the [Android stack](../frontend/mobile/android/stack.md) pages before this guide: they state the obligations, and this guide shows one compliant way to meet them.

## Procedure

### Declare the Android Stack

Add the `android` stack to `.litespec/project.yaml`. For a `client-only` project, match the stack and shape of the `android-client` preset on the [initialize command](../tools/init.md) page. This satisfies `litespec/core/scope#select-a-size-a-shape-and-stacks`.

### Declare the Mobile Solution Tree

Create the mobile solution and its core and platform projects under `apps/mobile/` as the [mobile structure](../frontend/mobile/structure.md) page states. Check that the solution holds the projects required for the project shape. This satisfies `litespec/frontend/mobile/structure#use-the-mobile-solution-tree`.

### Organize Screens by Module

Place each screen under `Screens/` in the platform project, in a folder for its module when the product has more than one module. This satisfies `litespec/frontend/mobile/structure#organize-screens-by-module`.

### Host Screens in One Activity

Host every screen in one activity with one navigation controller. This satisfies `litespec/frontend/mobile/screens#own-navigation-in-one-controller`.

### Record the Minimum Android Version

Use the Android API level pinned in `litespec.manifest.yaml` for the minimum version the app declares. This satisfies `litespec/core/stacks#use-manifest-version-pins`.

## Check

Confirm the result by build and publish:

- The mobile solution builds with no error and no analyzer finding outside a recorded departure.
- `dotnet publish apps/mobile/src/{ProjectName}.Android -c Release` produces an AAB.
