# Overview

A LiteSpec product reaches its users through frontends: web applications, client-only Blazor products, and native Android apps. These pages state how frontends are built: the frame, routes, and modules named for the backend use cases they call; the shared components and governed visual language; the interface pattern catalog; and how data, rendering, and testing work, with the bindings each stack applies. Follow them when you add a route, build a screen, or change shared UI or data code, so every screen answers the same question the same way.

## How the Pages Fit Together

The seven principle pages directly in this folder hold for every frontend, whether a web application, a client-only Blazor product, or a native Android app. They state what each part owns without naming a framework: structure, routes, components, UI governance, patterns, data, and testing. The controlled UI contract and its route suite cover the React and Svelte web stacks, as [Controlled UI Governance](ui.md) states. A frontend outside that contract follows its stack page, which states its route entry, its mark, and how it meets the principle pages.

React and Svelte share the web folder: the [Web overview](web/README.md) lists the pages stating the TypeScript bindings both apply. Native apps share the mobile folder: the [Mobile overview](mobile/README.md) lists the pages every native mobile stack applies. Each stack folder then states only what its own technology adds. Recurring interface questions are decided once in [UI Patterns](patterns.md) and its pattern pages, which apply alongside this folder.

## Frontend Principles

- [Frontend Structure](structure.md): frame, routes, and use-case modules, and the module tree that mirrors the backend.
- [Frontend Rendering and Routes](rendering.md): route addresses, data loading, and how each route state renders.
- [Frontend Components](components.md): the shared UI package and how page code composes its parts.
- [Controlled UI Governance](ui.md): the visual authority, the design contract, the floorplans, and the route suite.
- [UI Patterns](patterns.md): the catalog that decides each recurring interface question once.
- [Frontend Data and State](data.md): the generated client, the query cache, command writes, and how failures reach the user.
- [Frontend Testing](testing.md): unit tests, browser suites, and the contract check for every address.

## Shared Web Pages

- [Web overview](web/README.md): the TypeScript pages the React and Svelte stacks both apply, and the two stacks themselves.

## Shared Mobile Pages

- [Mobile overview](mobile/README.md): the pages every native mobile stack applies, and the mobile stacks themselves.

## Stacks

- [React Stack](web/react/stack.md): the React binding and its three rendering modes.
- [Svelte Stack](web/svelte/stack.md): the Svelte binding and its three rendering modes.
- [Blazor Stack](blazor/stack.md): the client-only domain model, browser persistence, and WebAssembly application.
- [Android Stack](mobile/android/stack.md): the native Android app built with .NET for Android, under the shared [mobile pages](mobile/README.md).
