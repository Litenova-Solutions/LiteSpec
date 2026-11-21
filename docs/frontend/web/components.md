# TypeScript Web Components

**Stack:** react, svelte
**Concern:** frontend-ui

Every screen in a TypeScript web frontend draws with composites from one shared UI package: the shared building blocks for layout and interaction. No application draws its own.

[Frontend Components](../components.md) states the component principles; this page states the TypeScript mechanism behind them that the [React Stack](react/stack.md) and [Svelte Stack](svelte/stack.md) share: where the primitives come from, which raw elements page code refuses, where the allowed exceptions live, how component boundaries stay typed, and which route checks both stacks run.

## Concepts

### Primitives, Composites, and Page Code

The registry CLI writes the primitives into one folder nobody edits by hand. The composites sit beside them and import them, per `litespec/frontend/components#keep-shared-ui-in-one-package`.

Page code declares no layout or interactive element, and each refusal names the composite to use, per `litespec/frontend/components#use-composites-for-layout-and-interaction-in-page-code`. The table below is the whole list: every refused element with its replacement.

| Refused | Use instead |
|:---|:---|
| `div`, `span` | `Stack`, `Inline`, `Columns`, `Section`, or a floorplan slot |
| `button` | `Button` from the shared package |
| `a` | `TextLink` or `ButtonLink` |
| `input`, `select`, `textarea` | a field from the shared form library |
| `form` | the shared form component |
| `table` | `DataTable` |
| `img` | the shared image component, under `litespec/frontend/components#use-a-composite-for-a-content-image` |

Text elements stay available for prose: `p`, `h2`, `h3`, `em`, `strong`, `ul`, `ol`, and `li` carry sentences, not layout or interaction. The floorplan owns the `h1`. Anything else follows the same rule: page code declares no other element, and the author adds a composite or a visual file instead.

### Style, Checks, and Raw Content

Class names merge through the one merge helper the shared package exports, with every merge or variant helper import inside it, per `litespec/frontend/components#write-no-class-name-in-page-code`.

A drawing no composite makes lives in a visual file, which draws with semantic theme tokens only, per `litespec/frontend/components#place-a-drawing-no-composite-makes-in-an-exception-file`. The shared provisions name the visual file pattern, the class and style bindings, and the raw HTML sink for each stack: `litespec/frontend/web/components#name-visual-files-with-the-stack-suffix`, `litespec/frontend/web/components#use-the-stack-class-and-style-bindings`, and `litespec/frontend/web/components#render-raw-html-through-the-stack-sink`.

A visible card, panel, or section title renders as a heading at its correct level, per `litespec/frontend/components#expose-card-and-section-titles-as-headings`, and the route suite resolves each as a heading. The route suite checks keyboard reach, visible focus, target size, and status announcements on every route, per `litespec/frontend/components#support-keyboard-focus-labels-and-announcements`.

Stored rich content renders only under its recorded sanitization decision, per `litespec/frontend/components#sanitize-untrusted-rich-content`.

## Standards

### Refuse Page Code Violations with ESLint

**ID:** `litespec/frontend/web/components#refuse-page-code-violations-with-eslint`

**Strength:** required

**Applies to:** artifact: component

**Statement:** Route and module code MUST pass the ESLint rules that refuse raw layout and interactive elements, class and style bindings, and layout utilities outside visual files, with each refusal naming the composite to use.

**Rationale:** A lint gate that only refuses leaves the author searching the shared package for the replacement. A refusal that names the composite tells the author the replacement in the error itself. The refusal-to-composite map is approved once per stack, and each stack page names the mechanism that enforces it. Visual files stay allowlisted, because a drawing no composite makes has no composite to name. This gates litespec/frontend/components#use-composites-for-layout-and-interaction-in-page-code, litespec/frontend/components#write-no-class-name-in-page-code, and litespec/frontend/ui#keep-layout-inside-floorplans.

**Reviewed for:** Does any refused element, binding, or layout utility show in the consumer's lint output outside visual files in route and module code? Check the lint output the consumer's configuration produces in CI.

### Keep Component Boundaries Typed

**ID:** `litespec/frontend/web/components#keep-component-boundaries-typed`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A component MUST type its props, its events, and its snippets under strict mode.

**Rationale:** An untyped prop fails at the call site the next author writes, after writing the code that uses it. Strict mode turns that failure into a build error at the author's own call. Typed events and snippets do the same for what the component reports and what it lets callers fill in. React components carry props and events; Svelte components add snippets.

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

**Checked by:** `svelte-check` fails on an untyped Svelte prop, event, or snippet, and the consumer-supplied type-check command fails on an untyped React prop or event. The consumer's configuration provides the type-check command.

### Enforce Trusted Types on Raw Markup

**ID:** `litespec/frontend/web/components#enforce-trusted-types-on-raw-markup`

**Strength:** advisory

**Applies to:** artifact: component

**Statement:** A web frontend SHOULD send `require-trusted-types-for 'script'` in its Content Security Policy and pass every raw markup string through one named policy that calls the sanitizer `litespec/frontend/components#sanitize-untrusted-rich-content` records.

**Deviation:** A frontend whose framework runtime or a required library writes markup through a sink the named policy cannot cover keeps the directive in report-only mode and records the sink in its sanitization decision.

**Rationale:** A recorded sanitizer protects only the calls that remember to use it. Trusted Types makes the browser refuse any raw string written into `innerHTML` or a similar sink, so a component that forgets the sanitizer fails in testing instead of in a staff member's browser. Framework runtimes and libraries also write markup through those sinks. A team first sends the policy as `Content-Security-Policy-Report-Only`, reads the reports, and enforces it once every reported sink passes through the named policy.

**Reviewed for:** Does every raw markup sink run inside the named policy, with the frontend sending the Trusted Types directive? Check the Content Security Policy header and the violation reports.

### Use the Stack Class and Style Bindings

**ID:** `litespec/frontend/web/components#use-the-stack-class-and-style-bindings`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A class or style binding MUST use the form its stack states: `className` or `style` on React, and `class`, `class:`, `style`, or `style:` on Svelte.

**Rationale:** Fixed forms keep styling in one vocabulary per stack that the lint and a reviewer can read. Each stack names its forms, as litespec/frontend/components#write-no-class-name-in-page-code asks.

**Example:**

| Stack | Binding forms |
|:---|:---|
| React | `className`, `style` |
| Svelte | `class`, `class:`, `style`, `style:` |

The loan row sets its tone through `className` on React, and the Claimwick expense status chip toggles its overdue tone with `class:overdue` on Svelte.

**Reviewed for:** Does every class or style binding use the form its stack states? Check the component markup.

### Name Visual Files with the Stack Suffix

**ID:** `litespec/frontend/web/components#name-visual-files-with-the-stack-suffix`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A file that holds a drawing no composite makes MUST end with the suffix its stack states: `.visual.tsx` on React and `.visual.svelte` on Svelte.

**Rationale:** The suffix identifies the one place outside the composites, so a search finds every custom drawing in one pass. Each stack names its suffix, as litespec/frontend/components#place-a-drawing-no-composite-makes-in-an-exception-file asks.

**Example:** The library loan due-date sparkline lives in `LoanSparkline.visual.tsx` on React, and the Claimwick receipt sparkline lives in `ExpenseSparkline.visual.svelte` on Svelte.

**Reviewed for:** Does every file holding a custom drawing end with the suffix its stack states? Check the component folders.

### Render Raw HTML Through the Stack Sink

**ID:** `litespec/frontend/web/components#render-raw-html-through-the-stack-sink`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A component MUST pass raw HTML to the page only through the sink its stack states: `dangerouslySetInnerHTML` on React and `{@html}` on Svelte.

**Rationale:** One named sink per stack gives every raw HTML write a name a search finds, so a review can check each use. Stored rich content renders only under its recorded sanitization decision, under litespec/frontend/components#sanitize-untrusted-rich-content.

**Example:** The library loan note passes its sanitized HTML through `dangerouslySetInnerHTML` on React, and the Claimwick expense note passes its sanitized HTML through `{@html}` on Svelte, each matching its recorded sanitization decision.

**Reviewed for:** Does every raw HTML write go through the sink its stack states? Check for raw markup in the component files.

## Conventions

None.
