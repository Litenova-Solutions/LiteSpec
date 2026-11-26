# Svelte Web Components

**Stack:** svelte
**Concern:** frontend-ui

Every screen in a Svelte frontend draws with composites from one shared UI package: the shared building blocks for layout and interaction. No application draws its own.

This page states the Svelte mechanism for shared UI: where the primitives come from, which raw elements page code refuses, where the allowed exceptions live, how component boundaries stay typed, and which route checks the Svelte stack runs.

## Concepts

### Primitives, Composites, and Page Code

The registry CLI writes the primitives into one folder nobody edits by hand. The composites sit beside them in the same shared package and import them.

Page code declares no layout or interactive element, and each refusal names the composite to use. The table below is the whole list: every refused element with its replacement.

| Refused | Use instead |
|:---|:---|
| `div`, `span` | `Stack`, `Inline`, `Columns`, `Section`, or a floorplan slot |
| `button` | `Button` from the shared package |
| `a` | `TextLink` or `ButtonLink` |
| `input`, `select`, `textarea` | a field from the shared form library |
| `form` | the shared form component |
| `table` | `DataTable` |
| `img` | the shared image component |

Text elements stay available for prose: `p`, `h2`, `h3`, `em`, `strong`, `ul`, `ol`, and `li` carry sentences, not layout or interaction. The floorplan owns the `h1`. Anything else follows the same rule: page code declares no other element, and the author adds a composite or a visual file instead.

### Style, Checks, and Raw Content

Class names merge through the one merge helper the shared package exports, with every merge or variant helper import inside it.

A drawing no composite makes lives in a visual file, which draws with semantic theme tokens only.

A visible card, panel, or section title renders as a heading at its correct level, and the route suite resolves each as a heading. The route suite checks keyboard reach, visible focus, target size, and status announcements on every route.

Stored rich content renders only under its recorded sanitization decision.

## Standards

### Refuse Page Code Violations with ESLint

**ID:** `litespec/packs/svelte/components#refuse-page-code-violations-with-eslint`

**Strength:** required

**Applies to:** artifact: component

**Statement:** Route and module code MUST pass the ESLint rules that refuse raw layout and interactive elements, class and style bindings, and layout utilities outside visual files, with each refusal naming the composite to use.

**Rationale:** A lint gate that only refuses leaves the author searching the shared package for the replacement. A refusal that names the composite tells the author the replacement in the error itself. Visual files stay allowlisted, because a drawing no composite makes has no composite to name. This gates the refusals the table above states: no raw elements, no class names, and no layout utilities outside visual files.

**Reviewed for:** Does any refused element, binding, or layout utility show in the consumer's lint output outside visual files in route and module code? Check the lint output the consumer's configuration produces in CI.

### Keep Component Boundaries Typed

**ID:** `litespec/packs/svelte/components#keep-component-boundaries-typed`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A component MUST type its props, its events, and its snippets under strict mode.

**Rationale:** An untyped prop fails at the call site the next author writes, after writing the code that uses it. Strict mode turns that failure into a build error at the author's own call. Typed events and snippets do the same for what the component reports and what it lets callers fill in.

**Example:** A Svelte component types its props and derives its total.

```svelte
<script lang="ts">
  let { orderId }: { orderId: string } = $props();
  let quantity = $state(1);
  let unitPrice = $state(0);
  let total = $derived(quantity * unitPrice);
</script>
```

**Checked by:** `svelte-check` fails on an untyped Svelte prop, event, or snippet.

### Enforce Trusted Types on Raw Markup

**ID:** `litespec/packs/svelte/components#enforce-trusted-types-on-raw-markup`

**Strength:** advisory

**Applies to:** artifact: component

**Statement:** A web frontend SHOULD send `require-trusted-types-for 'script'` in its Content Security Policy and pass every raw markup string through one named policy that calls the recorded sanitizer.

**Deviation:** A frontend whose framework runtime or a required library writes markup through a sink the named policy cannot cover keeps the directive in report-only mode and records the sink in its sanitization decision.

**Rationale:** A recorded sanitizer protects only the calls that remember to use it. Trusted Types makes the browser refuse any raw string written into `innerHTML` or a similar sink, so a component that forgets the sanitizer fails in testing instead of in a staff member's browser. A team first sends the policy as `Content-Security-Policy-Report-Only`, reads the reports, and enforces it once every reported sink passes through the named policy.

**Reviewed for:** Does every raw markup sink run inside the named policy, with the frontend sending the Trusted Types directive? Check the Content Security Policy header and the violation reports.

### Use the Stack Class and Style Bindings

**ID:** `litespec/packs/svelte/components#use-the-stack-class-and-style-bindings`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A class or style binding MUST use `class`, `class:`, `style`, or `style:`.

**Rationale:** Fixed forms keep styling in one vocabulary that the lint and a reviewer can read.

**Example:** The Claimwick expense status chip toggles its overdue tone with `class:overdue`.

**Reviewed for:** Does every class or style binding use the stated form? Check the component markup.

### Name Visual Files with the Stack Suffix

**ID:** `litespec/packs/svelte/components#name-visual-files-with-the-stack-suffix`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A file that holds a drawing no composite makes MUST end with `.visual.svelte`.

**Rationale:** The suffix identifies the one place outside the composites, so a search finds every custom drawing in one pass.

**Example:** The Claimwick receipt sparkline lives in `ExpenseSparkline.visual.svelte`.

**Reviewed for:** Does every file holding a custom drawing end with the stated suffix? Check the component folders.

### Render Raw HTML Through the Stack Sink

**ID:** `litespec/packs/svelte/components#render-raw-html-through-the-stack-sink`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A component MUST pass raw HTML to the page only through `{@html}`.

**Rationale:** One named sink gives every raw HTML write a name a search finds, so a review can check each use. Stored rich content renders only under its recorded sanitization decision.

**Example:** The Claimwick expense note passes its sanitized HTML through `{@html}`, matching its recorded sanitization decision.

**Reviewed for:** Does every raw HTML write go through the stated sink? Check for raw markup in the component files.

## Conventions

None.
