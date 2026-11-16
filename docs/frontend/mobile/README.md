# Overview

A native mobile app in LiteSpec is one .NET solution: core projects carry the domain model, and one platform project per platform carries the screens.

No project selects this folder directly; selecting the [Android Stack](android/stack.md) applies these shared pages through that stack's folder chain. A developer or agent adding a screen, a device feature, or data code to a mobile app finds the shared rule here and the stack-specific rule on the stack page.

## How the Pages Fit Together

Each page here follows one frontend principle page and states what every mobile stack does the same way. Where platforms differ, the shared page states the rule once and each stack page names the binding for its platform. A team that adds a mobile screen reads the principle page, then the shared page here, then the stack page where the platforms differ.

A native app builds each UI pattern it uses from the pattern's Default, with the platform's own views and the project's own components, and records the option it chose in the pattern catalog. [Mobile Screens and Navigation](screens.md) states how those views render, navigate, and respond to the system back gesture.

## Shared Mobile Pages

- [Mobile Structure](structure.md): the solution tree, the core projects each shape holds, the screen folders, and the import direction between them.
- [Mobile Screens and Navigation](screens.md): native views, one navigation controller, back behavior, insets, motion, media release, and accessibility.
- [Mobile Data and State](data.md): Application handlers, classified state, the typed store, app-private storage, and the generated client.
- [Mobile Device Capabilities](device.md): permissionless pickers, point-of-use requests, audio focus, background behavior, and permission declarations.
- [Mobile Testing](testing.md): plain .NET core tests, device tests for every screen, and the release-device check.

## Stacks

- [Android Stack](android/stack.md): the .NET for Android project, its pinned versions, and the rules only Android needs.
