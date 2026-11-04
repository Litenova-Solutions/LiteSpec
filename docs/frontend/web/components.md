# TypeScript Web Components

**Stack:** any

**Layer:** frontend-ui

## Intent

Every screen in a TypeScript web frontend draws with composites, shared building blocks for layout and interaction, from one shared UI package, instead of each application drawing its own. The component principles live in [Frontend Components](../components.md); this page states the TypeScript mechanism behind them for the [React Stack](react/stack.md) and [Svelte Stack](svelte/stack.md): the registry source holding the primitives every composite builds on, the closed list of refused raw elements, visual files for drawings no composite makes, the raw HTML sinks kept away from untrusted content, the typed component boundaries, and the route checks both stacks share. A team that applies it while writing pages keeps layout and interaction inside governed composites instead of scattered raw elements.

## Concepts

### TypeScript Mechanism

The registry CLI writes the primitives into one folder nobody edits by hand, and the composites sit beside them and import them, per `litespec/frontend/components#keep-shared-ui-in-one-package`. Page code declares no layout or interactive element, and each refusal names the composite to use, per `litespec/frontend/components#use-composites-for-layout-and-interaction-in-page-code`. The refused elements and their replacements are one closed list.

| Refused | Use instead |
|:---|:---|
| `div`, `span` | `Stack`, `Inline`, `Columns`, `Section`, or a floorplan slot |
| `button` | `Button` from the shared package |
| `a` | `TextLink` or `ButtonLink` |
| `input`, `select`, `textarea` | a field from the shared form library |
| `form` | the shared form component |
| `table` | `DataTable` |
| `img` | the shared image component, which `litespec/frontend/components#use-a-composite-for-a-content-image` owns |

Text elements stay available, because a sentence is not a component decision: `p`, `h2`, `h3`, `em`, `strong`, `ul`, `ol`, and `li` carry prose. The floorplan owns the `h1`. Any other element follows the same rule: page code declares no other element, and the author adds a composite or a visual file instead.

Class names merge through the one merge helper the shared package exports, with every merge or variant helper import inside it, per `litespec/frontend/components#write-no-class-name-in-page-code`. A drawing no composite makes lives in a visual file, which draws with semantic theme tokens only, per `litespec/frontend/components#place-a-drawing-no-composite-makes-in-an-exception-file`. Each stack page names the visual file pattern, the class and style bindings, and the raw HTML sink for its stack.

A visible card, panel, or section title renders as a heading at its correct level, per `litespec/frontend/components#expose-card-and-section-titles-as-headings`, and the route suite resolves each as a heading. The route suite checks keyboard reach, visible focus, target size, and status announcements on every route, per `litespec/frontend/components#support-keyboard-focus-labels-and-announcements`. Stored rich content renders only under its recorded sanitization decision, per `litespec/frontend/components#sanitize-untrusted-rich-content`.

## Standards

### Refuse Page Code Violations with ESLint

**ID:** `litespec/frontend/web/components#refuse-page-code-violations-with-eslint`

**Strength:** rule

**Applies to:** artifact: component

**Statement:** Route and module code MUST pass the ESLint rules that refuse raw layout and interactive elements, class and style bindings, and layout utilities outside visual files, with each refusal naming the composite to use.

**Rationale:** A lint gate that only refuses leaves the author searching the shared package for the replacement; a refusal that names the composite answers with the error. The refusal-to-composite map is approved once per stack, and each stack page names the mechanism that enforces it. Visual files stay allowlisted, because a drawing no composite makes has no composite to name. This gates litespec/frontend/components#use-composites-for-layout-and-interaction-in-page-code, litespec/frontend/components#write-no-class-name-in-page-code, and litespec/frontend/ui#keep-layout-inside-floorplans.

**Checked by:** `pnpm eslint` reports no refused element, binding, or layout utility outside visual files in route and module code.

### Keep Component Boundaries Typed

**ID:** `litespec/frontend/web/components#keep-component-boundaries-typed`

**Strength:** rule

**Applies to:** artifact: component

**Statement:** A component MUST type its props, its events, and its snippets under strict mode.

**Rationale:** An untyped prop fails at the call site the next author writes, which is the most expensive place to learn a shape: after writing the code that uses it. Strict mode turns that failure into a build error at the author's own call. Typed events and snippets do the same for what the component reports and what it lets callers fill in. React components carry props and events; Svelte components add snippets.

**Example:** A Svelte component types its props and derives its total; a React component types its props.

```svelte
<script lang="ts">
  let { orderId }: { orderId: string } = $props();
  let quantity = $state(1);
  let unitPrice = $state(0);
  let total = $derived(quantity * unitPrice);
</script>
```

```tsx
function OrderTotal({ orderId }: { orderId: string }) {
  const [quantity, setQuantity] = useState(1);
  const unitPrice = useUnitPrice(orderId);
  const total = quantity * unitPrice;
  return <Price value={total} />;
}
```

**Checked by:** `svelte-check` fails on an untyped Svelte prop, event, or snippet, and `pnpm type-check` fails on an untyped React prop or event.

## Conventions

None.
