# Svelte Components

**Stack:** svelte

**Layer:** frontend-ui

## Intent

A Svelte component holds its state in runes, with props from `$props` and derived values from `$derived`. This page states what Svelte alone adds: runes-mode components; the shared component principles live in [Frontend Components](../../components.md), and typed component boundaries live in [TypeScript Web Components](../components.md). A team that applies it while writing components keeps state, derivation, and boundaries compile-checked under strict mode.

## Standards

### Write Components in Runes Mode

**ID:** `litespec/frontend/web/svelte/components#write-components-in-runes-mode`

**Strength:** rule

**Applies to:** artifact: component

**Statement:** A component MUST hold its local state in runes, with props from `$props` and derived values from `$derived`.

**Rationale:** Runes are the reactivity model the compiler understands: `$state` for local state, `$props` for inputs, and `$derived` for computed values. Legacy reactive statements and untyped prop access bypass that model and behave differently under the compiler. An effect covers what derivation cannot express, so nothing needs the legacy forms.

**Example:**

```svelte
<script lang="ts">
  let { orderId }: { orderId: string } = $props();
  let quantity = $state(1);
  let unitPrice = $state(0);
  let total = $derived(quantity * unitPrice);
</script>
```

**Checked by:** `svelte-check` fails on a legacy reactive statement or an untyped `$props` call.

## Conventions

None.
