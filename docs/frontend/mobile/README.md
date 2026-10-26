# Overview

## Intent

The shared mobile pages state what every native mobile app does the same way: one .NET solution tree, native screens with one navigation controller, handlers with classified state, least-access device use, and core and device tests across structure, screens, data, device, and testing. No project selects this folder directly; selecting the [Android Stack](android/stack.md) applies it through that stack's folder chain. A developer or agent adding a screen, a device feature, or data code to a mobile app finds the shared rule here and the stack-specific rule on the stack page.

## How the Pages Fit Together

Each page here follows one frontend principle page and states what every mobile stack does the same way: one solution tree, one navigation model, one data path. Where platforms differ, the shared page states the rule once and each stack page names the binding for its platform. A team that adds a mobile screen reads the principle page, then the shared page here, then the stack page where the platforms differ.

## Shared Mobile Pages

- [Mobile Structure](structure.md): the solution tree, core projects, screen folders, and import direction every mobile stack shares.
- [Mobile Screens and Navigation](screens.md): native views, one navigation controller, back behavior, insets, motion, and media release.
- [Mobile Data and State](data.md): application handlers, classified state, the typed store, app-private storage, and the generated client.
- [Mobile Device Capabilities](device.md): permissionless pickers, point-of-use requests, audio focus, background behavior, and permission declarations.
- [Mobile Testing](testing.md): plain .NET core tests, device tests for every screen, and the release-device walk.

## Stacks

- [Android Stack](android/stack.md): the .NET for Android project, the pinned versions, and the views rules Android alone requires.
