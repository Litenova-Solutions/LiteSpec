# Web UI Patterns

**Stack:** any

**Concern:** frontend-ui

## Intent

A product records its UI pattern choices in one catalog, and each chosen pattern needs one implementation per web platform. The pattern rules live in [UI Patterns](../patterns.md); this page holds the web mechanism behind them for the [React Stack](react/stack.md) and [Svelte Stack](svelte/stack.md): one implementation per platform at the source paths the catalog names, and the registries that draw each option for review. A team that applies it while building shared UI keeps every route on the same behavior for each pattern.

## Concepts

### Web Mechanism

The catalog names `packages/ui/src/table/sort-header.tsx` under `react` and `packages/ui/src/table/sort-header.svelte` under `svelte`, so every invoice route composes one of them, per `litespec/frontend/patterns#implement-a-chosen-pattern-once`. LiteSpec options are reviewed at 390 and 1200 CSS pixels on the pattern's page on the LiteSpec site, and product-added options are drawn in a product registry at 390 and 1280, per `litespec/frontend/patterns#keep-a-visual-registry`. The catalog `registry` field holds the product registry command or path.

## Standards

None.

## Conventions

None.
