# Overview

**Stack:** react, svelte
**Concern:** frontend-ui

The web pages state what the React and Svelte frontends do the same way: one TypeScript toolchain, one generated API client, and one shared UI package across structure, rendering, components, UI, data, and testing. When you add a route, a component, or data code to either stack, find the shared rule here and the stack-specific rule on the stack page. No project selects this folder directly; selecting the [React Stack](react/stack.md) or the [Svelte Stack](svelte/stack.md) applies it through that stack's folder chain.

## How the Pages Fit Together

Each shared page pairs with one frontend principle page. The principle page says what holds for every frontend, the shared page states the rule once for what React and Svelte do the same way, and each stack page names the binding for its stack where the two differ. When you add a web screen, read in that order: the principle, then the shared page here, then the stack page where the stacks differ.

## Shared Web Pages

- [Web Structure](structure.md): the application tree, the frame, use-case marks, and the generated records file.
- [Web Rendering and Routes](rendering.md): the route metadata rule both stacks share.
- [TypeScript Web Components](components.md): the registry source, the closed element list, and the shared route checks.
- [Web UI](ui.md): the registry build, suite marks, CSS decisions, source rules, and pattern implementation for shared UI.
- [Web Data and State](data.md): the generated client, failure mapping, cache updates, and state ownership.
- [Web Testing](testing.md): Vitest beside the module and Playwright against a running stack.

## Stacks

- [React Stack](react/stack.md): the rendering mode choice and the names, files, and libraries React alone requires.
- [Svelte Stack](svelte/stack.md): the rendering mode choice and the names, files, and libraries Svelte alone requires.
