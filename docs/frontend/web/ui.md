# Web UI

## Intent

One shared UI package carries the pieces every route composes: the pinned baseline all applications share, the token stylesheet, the registry of page references, and the floorplans fixing each page's layout. This page states the toolchain, registry, tokens, and browser suites one web frontend requires, following the UI rules in [Controlled UI Governance](../ui.md). A team that applies it while writing shared UI and routes keeps every application looking like one product and every route proved in a browser.

## Concepts

### The Web Baseline

The baseline is the pinned UI source the shared package installs once. The manifest names it in the `uiBaseline` entry on React and the `uiBaselineSvelte` entry on Svelte. The registry build runs the command its stack page names.

### The Browser Route Suite

The browser suite discovers its routes from the generated route tree rather than from a handwritten list, so a new route is checked without anyone writing a specification for it. A dynamic segment resolves to a record the deployment holds, falling back to an absent record when it holds none.

| Check | The route passes when |
|:---|:---|
| Floorplan | Exactly one element carries `data-floorplan` |
| Reference | Each route names its registry item or is a recorded freestyle route |
| Accessibility | axe reports zero WCAG 2.2 AA violations |
| Heading | The page carries exactly one visible `h1` |
| Landmark | The page carries one `main` landmark |
| Reflow | At 320 CSS pixels the page has no horizontal overflow; a data table may scroll inside its own region |
| Text spacing | The reflow check still passes under the WCAG 1.4.12 text-spacing override |
| Keyboard | Every interactive element is reachable and shows a visible focus indicator |
| Target size | Every pointer target meets the minimum size or spacing |
| Announcements | Asynchronous status changes announce through a live region |

## Standards

### Pin the Baseline in the Shared Package

**ID:** `litespec/frontend/web/ui#pin-the-baseline-in-the-shared-package`

**Statement:** A workspace MUST install the pinned baseline from `litespec.manifest.json` once, into its shared UI package, using the `uiBaseline` or `uiBaselineSvelte` entry.

**Rationale:** Installing the pinned baseline once into the shared UI package keeps every application on the same source: the CLI's configuration, the registry source, the token stylesheet, and the source lock beside them. Installing per application made one copy each, and drift in any copy was invisible because every copy compiled. An additional registry is permitted only under an exception recorded against this provision, so the departure stays visible. This realizes litespec/frontend/ui#pin-the-baseline-in-the-shared-package.

**Checked by:** `litespec ui` compares the committed configuration against the manifest baseline fields and reports each mismatch.

### Publish One Registry Item per Floorplan

**ID:** `litespec/frontend/web/ui#publish-one-registry-item-per-floorplan`

**Statement:** The shared package MUST publish a registry whose build exits zero and which lists one item per floorplan the design contract declares.

**Rationale:** Without an installable page for each floorplan, an author invents a page from primitives and the next author invents another. One registry item per floorplan the design contract declares gives every page type a reference the author installs before writing. A registry build that exits zero proves every item still builds.

**Checked by:** `shadcn build` on React or `shadcn-svelte build` on Svelte exits zero, and `registry.json` lists one item per declared floorplan.

### Compose Each Route from One Floorplan

**ID:** `litespec/frontend/web/ui#compose-each-route-from-one-floorplan`

**Statement:** A route MUST render exactly one floorplan from the declared set, unless it is a recorded freestyle route.

**Rationale:** The floorplan fixes the layout, the heading position, and the slots, so a reviewer reads which floorplan a route composes and knows the page's shape. The slots take domain values: the aggregate state, the use cases that state permits, and the refusal results of the called use case. Exactly one floorplan per route keeps layout decisions out of route code; a recorded freestyle route is the visible departure. This realizes litespec/frontend/ui#compose-each-route-from-one-floorplan.

**Example:** An expense route composes the entity floorplan and fills each slot from the expense.

React:

```tsx
<EntityPage
  title={expense.number}
  status={<ExpenseStatus state={expense.state} />}
  actions={<ExpenseActions permitted={expense.permittedActions} />}
  facts={<ExpenseFacts expense={expense} />}
  related={<ExpensePayments expenseId={expense.id} />}
/>
```

Svelte:

```svelte
<EntityPage title={expense.number}>
  {#snippet status()}
    <ExpenseStatus state={expense.state} />
  {/snippet}
  {#snippet actions()}
    <ExpenseActions permitted={expense.permittedActions} />
  {/snippet}
  {#snippet facts()}
    <ExpenseFacts {expense} />
  {/snippet}
  {#snippet related()}
    <ExpensePayments expenseId={expense.id} />
  {/snippet}
</EntityPage>
```

**Checked by:** `pnpm playwright test tests/routes/every-route.spec.ts` asserts `page.locator("[data-floorplan]")` has count 1 on every route, and `litespec ui` reports each route missing its suite.

### Keep Layout Inside Floorplans

**ID:** `litespec/frontend/web/ui#keep-layout-inside-floorplans`

**Statement:** Route code MUST NOT carry a layout utility such as `grid`, `flex`, `gap-*`, `space-*`, or `col-span-*`.

**Rationale:** A layout utility in a route file is a second layout beside the floorplan's, and the next route copies it instead of the floorplan. With layout owned by floorplan and display composites, one change reaches every route composing them. Route code carries no `grid`, `flex`, `gap-*`, `space-*`, or `col-span-*`. This realizes litespec/frontend/ui#keep-layout-inside-floorplans.

**Checked by:** `pnpm eslint` refuses each layout utility in route code, naming the floorplan or composite to use.

### Derive Each Page Type from a Named Reference

**ID:** `litespec/frontend/web/ui#derive-each-page-type-from-a-named-reference`

**Statement:** Every route MUST carry a `data-registry-item` attribute naming its page type's registry item, with a freestyle route carrying no attribute and naming its decision record.

**Rationale:** An author with no named reference invents a page from primitives, and the next author invents another. The `data-registry-item` attribute naming the page type's registry item keeps every page of one type on one reference, and a test resolves it. A freestyle route carries no attribute and names its decision record instead, so the departure is explicit. This realizes litespec/frontend/ui#derive-each-page-type-from-a-named-reference.

**Checked by:** `pnpm playwright test tests/routes/every-route.spec.ts` asserts `data-registry-item` resolves to a published registry item on every non-freestyle route, and `litespec ui` reports each route missing its suite.

### Select a Tone from the Closed Set

**ID:** `litespec/frontend/web/ui#select-a-tone-from-the-closed-set`

**Statement:** A state indicator MUST take one tone from the closed set, each application declaring its state-to-tone mapping once as an exhaustive table of glossary terms.

**Rationale:** A page that picks its own color picks a meaning nobody agreed to, and the same state reads differently on every screen. One state-to-tone mapping per application, declared once as an exhaustive table of glossary terms, keeps every screen consistent and makes a new state fail to compile until its tone and its word exist. The table lives at the path its stack page names. This realizes litespec/frontend/ui#select-a-tone-from-the-closed-set.

**Checked by:** `pnpm type-check` fails on a state with no tone, and the state-to-tone table holds glossary terms only.

### Keep Tokens in One Stylesheet

**ID:** `litespec/frontend/web/ui#keep-tokens-in-one-stylesheet`

**Statement:** An application stylesheet MUST import the shared package's token stylesheet and declare no rule, with no literal color in any stylesheet the shared package ships.

**Rationale:** One token stylesheet makes two applications look like one product. Each application stylesheet imports it and declares no rule, so no application invents a value the tokens have no opinion about. A stylesheet the shared package ships beside the token sheet declares no literal color, so every color resolves through the tokens. This realizes litespec/frontend/ui#keep-tokens-in-one-definition.

**Checked by:** `litespec ui` reports each application stylesheet declaring a rule and each shared stylesheet declaring a literal color.

### Hold the Declared Base Color

**ID:** `litespec/frontend/web/ui#hold-the-declared-base-color`

**Statement:** A token stylesheet MUST carry the values the manifest baseline `baseColor` ramp publishes, and no value outside them except under a recorded exception.

**Rationale:** A token value outside the published `baseColor` ramp is a second palette beside the declared one, and the next author copies it. Holding the ramp keeps the whole product on one palette. A recorded exception keeps a deliberate departure visible in the design contract instead of hidden in the stylesheet. This realizes litespec/frontend/ui#hold-the-declared-base-color.

**Checked by:** `pnpm stylelint` reports each token value outside the declared base color ramp.

### Refuse Token Violations with Stylelint

**ID:** `litespec/frontend/web/ui#refuse-token-violations-with-stylelint`

**Statement:** Every stylesheet MUST pass the project's approved Stylelint configuration, which refuses color, spacing, and type values outside the shared tokens.

**Rationale:** A value outside the tokens answers to no token and survives every restyle, so the gate refuses it where it is written. The token file and the lint configuration are approved once: which properties are token-backed and which values are allowlisted. This gates litespec/frontend/ui#hold-the-declared-base-color, litespec/frontend/ui#keep-tokens-in-one-definition, and litespec/frontend/ui#restrict-styling-to-semantic-tokens.

**Checked by:** `pnpm stylelint` reports no color, spacing, or type value outside the shared tokens in application or shared stylesheets.

### Check Every Route in a Browser

**ID:** `litespec/frontend/web/ui#check-every-route-in-a-browser`

**Statement:** Every route MUST pass each route suite check in a browser, with axe reporting zero WCAG 2.2 AA violations.

**Rationale:** The Concepts section names the checks: floorplan, registry reference, axe WCAG 2.2 AA, heading, landmark, reflow, text spacing, keyboard use, target size, and status announcements. One suite visits every route the tree discovers, so a new route is checked without anyone writing a specification for it. A route outside the suite can ship any defect on the list with no error. Each frontend names that suite in `routeSuite` in its `paths.frontends[]` entry. This realizes litespec/frontend/ui#check-every-route.

**Checked by:** `pnpm playwright test tests/routes/every-route.spec.ts` runs axe and the Concepts checks on each discovered route, and `litespec ui` reports each route missing its suite. The Playwright run is authoritative for floorplan presence, while `litespec ui` owns suite existence.

### Mark the Route Suite and Goldens with What They Prove

**ID:** `litespec/frontend/web/ui#mark-the-route-suite-and-goldens-with-what-they-prove`

**Statement:** The route suite and every visual golden MUST carry a `proves` annotation for each UI provision, pattern provision, or quality requirement they enforce.

**Rationale:** The route suite is the one place accessibility is enforced for every page, and a visual golden is the one place a pattern's look is held, yet neither proves anything a tool can read while it names nothing. Marking them turns the suite into the evidence for the UI provisions it checks, and each golden into the evidence for the pattern it protects, so a passing run marks those records tested. Neither proves a scenario: a route suite asserts every page, not one use case. Each frontend names its route suite in `routeSuite` and its goldens in `visualGoldens` in its `paths.frontends[]` entry, so the check has a named subject instead of a convention.

**Example:** Claimwick's route suite, named in its web frontend's `routeSuite`, carries `proves("litespec/frontend/web/ui#check-every-route-in-a-browser")`.

**Checked by:** `litespec model check` reports `mark-missing` (level S) for a test in a frontend's `routeSuite` or `visualGoldens` with no `proves` annotation, and `mark-unknown` (level S) for one naming a scenario.

### Prove Every Result a Route Calls

**ID:** `litespec/frontend/web/ui#prove-every-result-a-route-calls`

**Statement:** Every refusal a web screen can reach through its computed `calls` MUST be shown as `litespec/core/edges#show-every-refusal-a-screen-can-reach` states.

**Rationale:** That Behavior provision is the one result obligation for a screen, on the web stacks as on every stack: a sentence in the frontend's refusal dictionary or a browser test asserting the code. The screen's computed `calls` name the refusals in scope, so no route-level restatement adds a second scope or verification. This realizes litespec/frontend/ui#prove-every-result-a-route-calls by pointing at the same obligation.

**Example:** A browser test on the expense form asserts the `spending.receipt-missing` refusal by its code.

**Reviewed for:** Which refusals a web screen reaches through its computed `calls` lack a dictionary sentence and a browser test asserting the code? Look at the `refusal-sentence-missing` findings for the frontend.

### Restrict CSS Decisions

**ID:** `litespec/frontend/web/ui#restrict-css-decisions`

**Statement:** A Tailwind class MUST use a semantic token rather than an arbitrary value, a raw palette value, or an important modifier.

**Rationale:** Semantic tokens keep every class inside the governed palette, spacing, and type scales. An arbitrary value, a raw palette value, or an important modifier steps outside them: the value answers to no token and survives every restyle. The approved CSS surface is the shared package's stylesheet, holding the Tailwind import, the tokens, the fonts, and the documented resets. The source scan skips build output and reports authored feature CSS. This realizes litespec/frontend/ui#restrict-styling-to-semantic-tokens.

**Checked by:** `litespec ui` reports each arbitrary value, raw palette value, important modifier, and inline style with its file.

### Track Source Changes

**ID:** `litespec/frontend/web/ui#track-source-changes`

**Statement:** The shared package's source lock MUST record the pinned CLI, the registry address, the paths, the normalized digest, and the dependencies for each primitive.

**Rationale:** The source lock records the pinned CLI, the registry address, the paths, the normalized digest, and the dependencies for each primitive. Normalizing source before hashing lets two workspaces compute the same digest for identical source. One lock beside one copy is what makes a changed primitive report a fork instead of passing unreported.

**Checked by:** `litespec ui` recomputes each digest and reports changed primitive source.

### Prove UI Behavior and Appearance

**ID:** `litespec/frontend/web/ui#prove-ui-behavior-and-appearance`

**Statement:** A primitive, composite, token, source, floorplan, or route change MUST name the evidence the governance matrix requires for its class.

**Rationale:** The governance matrix on [Controlled UI Governance](../ui.md) maps each change class to the narrowest evidence that covers it: no more than needed, no less. On the web stacks the fork record is the source lock entry, so a fork names the digest beside the component test. This realizes litespec/frontend/ui#prove-ui-behavior-and-appearance.

**Checked by:** `pnpm playwright test tests/routes/every-route.spec.ts` runs on the changed route, and review confirms the change names the governance matrix evidence for its class.

## Conventions

### Use the Source Update Procedure

**ID:** `litespec/frontend/web/ui#use-the-source-update-procedure`

**Default:** Update the primitive source on a branch that pins the CLI, starts clean, and reviews the regenerated files and the local composites separately.

**Replacement:** A consumer can replace this default by naming its source-update branch procedure in its documentation.

**Rationale:** Running `view`, `--dry-run`, and `--diff` first shows what the update changes before anything is written. The regenerated files and the local composites change for different reasons, upstream source versus product adaptation, so one review cannot cover both. The branch pins the CLI and starts clean, so the run is repeatable and the diff shows only the update.

**Reviewed for:** Which update branches miss the pinned CLI, the dry-run output, or the separate review of regenerated files and local composites? Look in the update branches.
