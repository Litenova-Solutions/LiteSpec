# Add an Android Frontend

Add an Android frontend to a consumer: the stack entry, the mobile solution tree, and the screen layout. Screens live in a mobile solution organized by module, all hosted in one activity. Android stays outside the controlled UI contract, so no floorplan or baseline step applies. Each step names the standard it satisfies, and this guide adds no rule of its own.

## Prerequisites

Start with:

- A consumer project. A product with a server adds `android` beside its backend stack, and a product with no server starts from the `android-client` preset on the [initialize command](../tools/init.md) page.
- The .NET SDK and Android API level pinned in `litespec.manifest.yaml` under `stack.dotnet` and `stack.android`, with the Android workload installed through `dotnet workload install android`.

Read the [mobile structure](../packs/android/structure.md), the [mobile screens and navigation](../packs/android/screens.md), and the [Android stack](../packs/android/stack.md) pages before this guide. They state the obligations, and this guide shows one compliant way to meet them.

## Procedure

1. Add the `android` stack to `.litespec/project.yaml`. For a `client-only` project, match the stack and shape of the `android-client` preset on the [initialize command](../tools/init.md) page. For example, the Claimwick server product adds `android` beside `dotnet` and keeps its shape as `server`. This satisfies `litespec/core/scope#select-a-size-a-shape-and-stacks`.
2. Create the mobile solution and its core and platform projects under `apps/mobile/` as the [mobile structure](../packs/android/structure.md) page states. Check that the solution holds the projects required for the project shape. For example, the Claimwick mobile solution holds the core project with the shared code and the platform project with the Android host. This satisfies `litespec/packs/android/structure#use-the-mobile-solution-tree`.
3. Place each screen under `Screens/` in the platform project, in a folder for its module when the product has more than one module. For example, the Claimwick expense screens live in the spending folder under `Screens/`. This satisfies `litespec/packs/android/structure#organize-screens-by-module`.
4. Host every screen in one activity with one navigation controller. For example, the Claimwick app opens its one activity at startup, and the controller moves between the expense list and the expense detail. This satisfies `litespec/packs/android/screens#own-navigation-in-one-controller`.
5. Use the Android API level pinned in `litespec.manifest.yaml` for the minimum version the app declares. For example, when the manifest pins API 34, the app manifest declares 34 as its minimum. This satisfies `litespec/core/principles#locate-package-versions-in-the-manifest`.

## Check

Confirm the result by build and publish:

- The mobile solution builds with no error and no analyzer finding outside a recorded waiver.
- `dotnet publish apps/mobile/src/{ProjectName}.Android -c Release` produces an AAB.
