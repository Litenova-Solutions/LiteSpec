# Svelte Web Components

**Stack:** svelte
**Concern:** frontend-ui

Every Svelte screen draws with composites from one shared UI package. This page is for developers who write route and module code in a Svelte frontend. A hand-built element beside the composites splits the product into two looks, and review loses the single list of replacements.

## Concepts

### How Page Code Meets the Shared Package

The registry CLI writes the primitives into one folder that stays untouched by hand. The composites sit beside the primitives in the same shared package and import them. Page code declares layout and interaction through the composites, and each refusal in the table below names the composite to use.

| Refused | Use instead |
|:---|:---|
| `div`, `span` | `Stack`, `Inline`, `Columns`, `Section`, or a floorplan slot |
| `button` | `Button` from the shared package |
| `a` | `TextLink` or `ButtonLink` |
| `input`, `select`, `textarea` | a field from the shared form library |
| `form` | the shared form component |
| `table` | `DataTable` |
| `img` | the shared image component |

The table above lists every refused element with its replacement. Prose elements stay available for sentences: `p`, `h2`, `h3`, `em`, `strong`, `ul`, `ol`, and `li` carry sentences, not layout or interaction. The floorplan owns the `h1`, and a drawing outside the composites lives in a visual file instead of a new element.

### How Style and Raw Content Stay Reviewable

Class names merge through the one merge helper the shared package exports. Every merge or variant helper import lives inside that helper. The Claimwick expense status chip toggles its overdue tone with `class:overdue`.

Raw HTML passes through `{@html}`, the one template tag a search finds. Stored rich content renders under its recorded sanitization decision. A visible card, panel, or section title renders as a heading at its level, and the route suite resolves each as a heading.

The route suite checks keyboard reach, visible focus, target size, and status announcements on every route. Visual files draw with semantic theme tokens. Trusted Types in report-only mode comes first, with enforcement once every reported sink passes through the named policy.

## Standards

### Refuse Page Code Violations with ESLint

**ID:** `litespec/packs/svelte/components#refuse-page-code-violations-with-eslint`

**Strength:** required

**Applies to:** artifact: component

**Statement:** Route and module code MUST pass the ESLint rules that refuse raw layout and interactive elements, class and style bindings, and layout utilities outside visual files, with each refusal naming the composite to use.

**Rationale:** A refusal without a replacement leaves the author searching the shared package. A refusal that names the composite tells the author the replacement inside the error itself. Visual files stay allowlisted because a drawing outside the composites has no composite to name.

**Reviewed for:** Does any refused element, binding, or layout utility show in the consumer's lint output outside visual files in route and module code? Check the lint output the consumer's configuration produces in CI.

### Keep Component Boundaries Typed

**ID:** `litespec/packs/svelte/components#keep-component-boundaries-typed`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A component MUST type its props, its events, and its snippets under strict mode.

**Rationale:** An untyped prop fails at the call site the next author writes, after the code that uses it. Strict mode turns that failure into a build error at the author's own call. Typed events and snippets do the same for what the component reports and what callers fill in.

**Example:** A Svelte component types its props and derives its total. The snippet below shows typed props with derived state for an order line.

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

**Rationale:** A recorded sanitizer protects only the calls that remember it. Trusted Types turns the browser into a second check: a raw string outside the named policy fails in testing instead of in a reader browser. A team ships report-only first, reads the reports, and enforces the policy once every reported sink passes through the named policy.

**Reviewed for:** Does every raw markup sink run inside the named policy, with the frontend sending the Trusted Types directive? Check the Content Security Policy header and the violation reports.

### Use the Stack Class and Style Bindings

**ID:** `litespec/packs/svelte/components#use-the-stack-class-and-style-bindings`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A class or style binding MUST use `class`, `class:`, `style`, or `style:`.

**Rationale:** Fixed forms keep styling in one vocabulary. The lint and a reviewer read the same four bindings on every component.

**Example:** The Claimwick expense status chip toggles its overdue tone with `class:overdue`. The binding reads as a class toggle beside the chip markup.

**Reviewed for:** Does every class or style binding use the stated form? Check the component markup.

### Name Visual Files with the Stack Suffix

**ID:** `litespec/packs/svelte/components#name-visual-files-with-the-stack-suffix`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A file that holds a drawing no composite makes MUST end with `.visual.svelte`.

**Rationale:** The suffix marks the one place outside the composites. A search over the suffix finds every custom drawing in one pass.

**Example:** The Claimwick receipt sparkline lives in `ExpenseSparkline.visual.svelte`. The suffix tells review that the file draws outside the composites.

**Reviewed for:** Does every file holding a custom drawing end with the stated suffix? Check the component folders.

### Render Raw HTML Through the Stack Sink

**ID:** `litespec/packs/svelte/components#render-raw-html-through-the-stack-sink`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A component MUST pass raw HTML to the page only through `{@html}`.

**Rationale:** One named sink gives every raw HTML write a name a search finds, so review checks each use. Stored rich content renders under its recorded sanitization decision.

**Example:** The Claimwick expense note passes its sanitized HTML through `{@html}`, matching its recorded sanitization decision. Review finds the write by searching for the sink name.

**Reviewed for:** Does every raw HTML write go through the stated sink? Check for raw markup in the component files.

## Conventions

None.
