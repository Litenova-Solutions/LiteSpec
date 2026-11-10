# Add an Android Frontend

## Purpose

A product that serves its interface from Android declares a mobile solution tree instead of the web application tree, with screens organized by module and hosted in one activity. Android stays outside the controlled UI contract, so no floorplan or baseline step applies. This guide declares that frontend: the stack entry, the mobile tree, and the screen layout. Each step names the standard it satisfies, and the guide adds no rule of its own.

## Prerequisites

Start with:

- A consumer from [Start a Consumer Repository](../tutorial/getting-started.md), with its backend use cases implemented.
- The `android` stack beside the tutorial pair in the consumer's `.litespec/project.yaml`.
- The .NET SDK and Android API level pinned in `litespec.manifest.yaml` (`stack.dotnet`, `stack.android`) at the LiteSpec root, with the `android` workload installed through `dotnet workload install android`.

Read the [Mobile Structure page](../frontend/mobile/structure.md) and the [Android Stack page](../frontend/mobile/android/stack.md) before this guide: they state the obligations, and this guide shows one compliant way to meet them.

## Procedure

### Declare the Android Stack

Add the `android` stack to `.litespec/project.yaml`, following the stack entries of the `android-client` preset on the [initialize command page](../tools/init.md). A consumer without a server starts from that preset. This satisfies `litespec/core/scope#select-a-size-a-shape-and-stacks`.

### Declare the Mobile Solution Tree

Declare the mobile solution tree. This satisfies `litespec/frontend/mobile/structure#use-the-mobile-solution-tree`.

### Organize Screens by Module

Organize screens by module. This satisfies `litespec/frontend/mobile/structure#organize-screens-by-module`.

### Host Screens in One Activity

Host screens in one activity with one navigation controller. This satisfies `litespec/frontend/mobile/screens#own-navigation-in-one-controller`.

### Record the Minimum Android Version

Record the minimum Android version from the manifest pin. This satisfies `litespec/core/stacks#use-manifest-version-pins`.

## Check

- The mobile solution builds with no error and no analyzer finding outside a recorded exception.
- `dotnet publish apps/mobile/src/{ProjectName}.Android -c Release` produces an AAB.
