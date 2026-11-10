# Web UI

**Stack:** any

**Concern:** frontend-ui

## Intent

One shared UI package carries the pieces every route composes: the registry of page references, the token stylesheet, and the baseline source it installs once. This page states the registry build, the suite marks, the CSS decisions, and the source rules one web frontend requires, following the UI rules in [Controlled UI Governance](../ui.md). A team that applies it while writing shared UI keeps every registry item buildable, every suite marked with what it proves, and every primitive traceable to its source.

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

### Rules Kept on the Base Page

The UI rules for the baseline, floorplans, layout, page references, tones, tokens, the base color, styling, route checks, result proof, and change evidence live on [Controlled UI Governance](../ui.md). The web stacks add the registry, suite marks, CSS decisions, and source rules this page states.

- Baseline pin: `litespec/frontend/ui#pin-the-baseline-in-the-shared-package`
- Floorplan composition: `litespec/frontend/ui#compose-each-route-from-one-floorplan`
- Layout ownership: `litespec/frontend/ui#keep-layout-inside-floorplans`
- Page reference: `litespec/frontend/ui#derive-each-page-type-from-a-named-reference`
- State tones: `litespec/frontend/ui#select-a-tone-from-the-closed-set`
- Token definition: `litespec/frontend/ui#keep-tokens-in-one-definition`
- Base color: `litespec/frontend/ui#hold-the-declared-base-color`
- Styling gate: `litespec/frontend/ui#restrict-styling-to-semantic-tokens`
- Route checks: `litespec/frontend/ui#check-every-route`
- Result proof: `litespec/frontend/ui#prove-every-result-a-route-calls`
- Change evidence: `litespec/frontend/ui#prove-ui-behavior-and-appearance`

## Standards

### Publish One Registry Item per Floorplan

**ID:** `litespec/frontend/web/ui#publish-one-registry-item-per-floorplan`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** The shared package MUST publish a registry whose build exits zero and which lists one item per floorplan the design contract declares.

**Rationale:** Without an installable page for each floorplan, an author invents a page from primitives and the next author invents another. One registry item per floorplan the design contract declares gives every page type a reference the author installs before writing. A registry build that exits zero proves every item still builds.

**Checked by:** `shadcn build` on React or `shadcn-svelte build` on Svelte exits zero, and `registry.json` lists one item per declared floorplan.

### Mark the Route Suite and Goldens with What They Prove

**ID:** `litespec/frontend/web/ui#mark-the-route-suite-and-goldens-with-what-they-prove`

**Strength:** rule

**Applies to:** artifact: test

**Statement:** The route suite and every visual golden MUST carry a `proves` annotation for each UI provision, pattern provision, or quality requirement they enforce.

**Rationale:** The route suite is the one place accessibility is enforced for every page, and a visual golden is the one place a pattern's look is held, yet neither proves anything a tool can read while it names nothing. Marking them turns the suite into the evidence for the UI provisions it checks, and each golden into the evidence for the pattern it protects, so a passing run marks those records tested. Neither proves a scenario: a route suite asserts every page, not one use case. Each frontend names its route suite in `routeSuite` and its goldens in `visualGoldens` in its `frontends[]` entry, so the check has a named subject instead of a convention.

**Example:** Claimwick's route suite, named in its web frontend's `routeSuite`, carries `proves("litespec/frontend/ui#check-every-route")`.

**Checked by:** `litespec model check` reports `mark-missing` (level S) for a test in a frontend's `routeSuite` or `visualGoldens` with no `proves` annotation, and `mark-unknown` (level S) for one naming a scenario.

### Restrict CSS Decisions

**ID:** `litespec/frontend/web/ui#restrict-css-decisions`

**Strength:** rule

**Applies to:** artifact: component, route

**Statement:** A Tailwind class MUST use a semantic token rather than an arbitrary value, a raw palette value, or an important modifier.

**Rationale:** Semantic tokens keep every class inside the governed palette, spacing, and type scales. An arbitrary value, a raw palette value, or an important modifier steps outside them: the value answers to no token and survives every restyle. The approved CSS surface is the shared package's stylesheet, holding the Tailwind import, the tokens, the fonts, and the documented resets. The source scan skips build output and reports authored feature CSS. This realizes litespec/frontend/ui#restrict-styling-to-semantic-tokens.

**Checked by:** `litespec model check` reports each arbitrary value, raw palette value, important modifier, and inline style with its file.

### Track Source Changes

**ID:** `litespec/frontend/web/ui#track-source-changes`

**Strength:** rule

**Applies to:** artifact: any

**Statement:** The shared package's source lock MUST record the pinned CLI, the registry address, the paths, the normalized digest, and the dependencies for each primitive.

**Rationale:** The source lock records the pinned CLI, the registry address, the paths, the normalized digest, and the dependencies for each primitive. Normalizing source before hashing lets two workspaces compute the same digest for identical source. One lock beside one copy is what makes a changed primitive report a fork instead of passing unreported.

**Checked by:** `litespec model check` recomputes each digest and reports changed primitive source.

## Conventions

### Use the Source Update Procedure

**ID:** `litespec/frontend/web/ui#use-the-source-update-procedure`

**Strength:** default

**Applies to:** artifact: any

**Default:** Update the primitive source on a branch that pins the CLI, starts clean, and reviews the regenerated files and the local composites separately.

**Replacement:** A consumer can replace this default by naming its source-update branch procedure in its documentation.

**Rationale:** Running `view`, `--dry-run`, and `--diff` first shows what the update changes before anything is written. The regenerated files and the local composites change for different reasons, upstream source versus product adaptation, so one review cannot cover both. The branch pins the CLI and starts clean, so the run is repeatable and the diff shows only the update.

**Reviewed for:** Which update branches miss the pinned CLI, the dry-run output, or the separate review of regenerated files and local composites? Look in the update branches.
