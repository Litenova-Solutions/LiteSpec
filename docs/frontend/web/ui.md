# Web UI

**Stack:** react, svelte
**Concern:** frontend-ui

## Intent

One shared UI package carries the pieces every route composes: the registry of page references, the token stylesheet, and the baseline source it installs once. This page states the registry build, the suite marks, the CSS decisions, the browser support target, the color schemes, motion, and the source rules one web frontend requires, following the UI rules in [Controlled UI Governance](../ui.md). A team that applies it while writing shared UI keeps every registry item buildable, every route readable in every scheme and browser it targets, and every primitive traceable to its source.

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
| Reflow | At 320 CSS pixels the page has no horizontal overflow; a data table may scroll inside its own region, which carries `tabindex="0"` and an accessible name |
| Text spacing | The reflow check still passes under the WCAG 1.4.12 text-spacing override |
| Keyboard | Every interactive element is reachable, and the focused element's computed outline is not `none` and reaches 3:1 against the background next to it |
| Target size | Every pointer target meets the minimum size or spacing |
| Route change | After a client-side navigation through a link, `document.activeElement` is the route's `h1` |
| Color scheme | The root's computed `color-scheme` names each scheme the design contract declares, and axe reports zero violations with each one emulated |
| Motion | With `prefers-reduced-motion: reduce` emulated, `document.getAnimations()` holds no running transform, scale, or infinitely repeating animation once the route settles |
| Announcements | One `role="status"` region and one `role="alert"` region exist when the route loads, and asynchronous status changes announce through them |

### The Browser Support Target

The design contract's Browser Support section names one Baseline tier, the set of web platform features that every major browser supports:

| Tier | A feature belongs to it when |
|:---|:---|
| Baseline widely available | Every major browser has supported it for at least 30 months |
| Baseline newly available | Every major browser supports it |

The Default is Baseline widely available. A team decides each newer feature in this order, and the first rule that matches wins:

1. The feature belongs to the declared tier: use it with no fallback.
2. The tier is Baseline newly available and the feature belongs to it: detect the feature and let an unsupported browser show its default behavior.
3. A fallback fits in the same file, behind `@supports` in CSS or a feature check in script: write it there.
4. The fallback needs a package: record the decision that admits the package, as `litespec/frontend/ui#govern-behavior-packages` states.
5. No fallback fits: change the design so it needs no such feature.

Tailwind CSS v4 itself needs a recent browser, so a project that targets older browsers than the styling engine supports records the gap in its design contract.

### Pattern Implementation

A product records its UI pattern choices in one catalog, and each chosen pattern needs one implementation per web platform. The catalog names the source path for each platform, for example `packages/ui/src/table/sort-header.tsx` under `react` and `packages/ui/src/table/sort-header.svelte` under `svelte`, so every route composes that implementation, per `litespec/frontend/patterns#implement-a-chosen-pattern-once`. LiteSpec options are reviewed on the pattern's page on the LiteSpec site at the catalog breakpoints, and product-added options are drawn in a product registry at the catalog breakpoints, per `litespec/frontend/patterns#keep-a-visual-registry`.

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
- Result proof: `litespec/core/edges#show-every-refusal-a-screen-can-reach`
- Change evidence: `litespec/frontend/ui#prove-ui-behavior-and-appearance`

## Standards

### Publish One Registry Item per Floorplan

**ID:** `litespec/frontend/web/ui#publish-one-registry-item-per-floorplan`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The shared package MUST publish a registry whose build exits zero and which lists one item per floorplan the design contract declares.

**Rationale:** Without an installable page for each floorplan, an author invents a page from primitives and the next author invents another. One registry item per floorplan the design contract declares gives every page type a reference the author installs before writing. A registry build that exits zero proves every item still builds.

**Checked by:** `shadcn build` on React or `shadcn-svelte build` on Svelte exits zero, and `registry.json` lists one item per declared floorplan.

### Mark the Route Suite and Goldens with What They Prove

**ID:** `litespec/frontend/web/ui#mark-the-route-suite-and-goldens-with-what-they-prove`

**Strength:** required

**Applies to:** artifact: test

**Statement:** The route suite and every visual golden MUST carry a `proves` annotation for each UI provision, pattern provision, or quality requirement they enforce.

**Rationale:** The route suite is the one place accessibility is enforced for every page, and a visual golden is the one place a pattern's look is held, yet neither proves anything a tool can read while it names nothing. Marking them turns the suite into the evidence for the UI provisions it checks, and each golden into the evidence for the pattern it protects, so a passing run marks those records tested. Neither proves a scenario: a route suite asserts every page, not one use case. Each frontend names its route suite in `routeSuite` and its goldens in `visualGoldens` in its `frontends[]` entry, so the check has a named subject instead of a convention.

**Example:** Claimwick's route suite, named in its web frontend's `routeSuite`, carries `proves("litespec/frontend/ui#check-every-route")`.

**Checked by:** `litespec model check` reports `mark-missing` (level S) for a test in a frontend's `routeSuite` or `visualGoldens` with no `proves` annotation, and `mark-unknown` (level S) for one naming a scenario.

### Restrict CSS Decisions

**ID:** `litespec/frontend/web/ui#restrict-css-decisions`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** A Tailwind class MUST use a semantic token rather than an arbitrary value, a raw palette value, or an important modifier.

**Rationale:** Semantic tokens keep every class inside the governed palette, spacing, and type scales. An arbitrary value, a raw palette value, or an important modifier steps outside them: the value answers to no token and survives every restyle. The approved CSS surface is the shared package's stylesheet, holding the Tailwind import, the tokens, the fonts, and the documented resets. The source scan skips build output and reports authored feature CSS. This realizes litespec/frontend/ui#restrict-styling-to-semantic-tokens.

**Checked by:** `litespec model check` reports `ui-class-invalid` for each arbitrary value, raw palette value, and important modifier, and `ui-stylesheet-invalid` for each inline style, with its file.

### Build Within the Browser Support Target

**ID:** `litespec/frontend/web/ui#build-within-the-browser-support-target`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** The shared package and the routes MUST use only web platform features in the tier the design contract's Browser Support section names, or carry a fallback beside each other feature.

**Rationale:** A feature outside the declared tier breaks silently for the staff member whose browser lacks it: a dialog that never opens, a layout that collapses, a control that does nothing. Naming one tier in the design contract lets every author and agent judge a feature against the same line, and the decision order in the Concepts section says what to write when a feature falls outside it.

**Example:** A design contract that declares Baseline widely available lets the expense record header switch its layout with a size container query, which belongs to that tier, with no fallback. A tooltip placed with CSS anchor positioning, which belongs to neither tier yet, keeps the script positioning of the baseline popover component instead.

**Reviewed for:** Which platform feature in the shared package or a route falls outside the declared tier with no fallback beside it? Look in the stylesheets and components against the design contract's Browser Support section.

### Render Each Declared Color Scheme

**ID:** `litespec/frontend/web/ui#render-each-declared-color-scheme`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A web frontend MUST declare on its root element the color schemes its tokens define and pass the route suite in each of them.

**Rationale:** A token set with a dark scheme that no check renders ships unreadable text to every staff member whose device is set to dark. Declaring `color-scheme` also lets the browser draw scrollbars, form controls, and the page before styles load in the matching scheme, so nothing flashes white. A product that defines only a light scheme declares `color-scheme: light` and is checked in that scheme only.

**Reviewed for:** Which route fails the route suite in a declared color scheme, or which root omits `color-scheme`? Look in the route suite output and the shared stylesheet.

### Honor Reduced Motion

**ID:** `litespec/frontend/web/ui#honor-reduced-motion`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A route MUST run no spatial, scaling, or looping animation while the reader's system asks for reduced motion.

**Rationale:** WCAG 2.3.3 Animation from Interactions: a drawer that slides or a card that zooms can bring on nausea or dizziness for a reader with a vestibular condition. Under reduced motion, a transition becomes a short fade or nothing, and every animation still ends in its final state, so the screen shows the same result without the movement. The design contract's Motion section names what each duration becomes. The mobile stacks hold the same rule in `litespec/frontend/mobile/screens#honor-reduced-motion`.

**Reviewed for:** Which route still slides, scales, or loops an animation with reduced motion emulated? Look in the route suite output.

### Give Moving Content a Pause Control

**ID:** `litespec/frontend/web/ui#give-moving-content-a-pause-control`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** Content that moves, scrolls, or updates on its own for more than five seconds MUST offer a control that pauses, stops, or hides it.

**Rationale:** WCAG 2.2.2 Pause, Stop, Hide: a ticker of incoming expenses or a rotating banner pulls the eye away from the form a staff member is filling in, and a screen-reader user hears it interrupt. A pause control gives the reader the page back.

**Reviewed for:** Which moving or self-updating content runs longer than five seconds with no pause, stop, or hide control? Look in the routes that show live feeds, carousels, or animated status.

### Set Text Sizes in Relative Units

**ID:** `litespec/frontend/web/ui#set-text-sizes-in-relative-units`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A font-size token MUST use `rem` rather than `px`.

**Rationale:** WCAG 1.4.4 Resize Text: a reader who sets a larger default text size in the browser or the system expects every page to follow it, and a size fixed in pixels ignores that setting. A `rem` token scales with the reader's setting, and the reflow and text-spacing checks then prove that the layout still holds.

**Reviewed for:** Which font-size token is written in `px`? Look in the shared package's token stylesheet.

### Track Source Changes

**ID:** `litespec/frontend/web/ui#track-source-changes`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The shared package's source lock MUST record the pinned CLI, the registry address, the paths, the normalized digest, and the dependencies for each primitive.

**Rationale:** The source lock records the pinned CLI, the registry address, the paths, the normalized digest, and the dependencies for each primitive. Normalizing source before hashing lets two workspaces compute the same digest for identical source. One lock beside one copy is what makes a changed primitive report a fork instead of passing unreported. `litespec model render` writes the digests, the preset and the CLI pin, so a person reviews a changed digest in the pull request diff and never computes one. Settled by [decision/0024-keep-the-ui-source-lock](decision/0024-keep-the-ui-source-lock).

**Checked by:** `litespec model check` recomputes each digest and reports `ui-sourcelock-invalid` for changed primitive source.

## Conventions

### Use the Source Update Procedure

**ID:** `litespec/frontend/web/ui#use-the-source-update-procedure`

**Strength:** default

**Applies to:** artifact: any

**Default:** Update the primitive source on a branch that pins the CLI, starts clean, and reviews the regenerated files and the local composites separately.

**Replacement:** A consumer can replace this default by naming its source-update branch procedure in its documentation.

**Rationale:** Running `view`, `--dry-run`, and `--diff` first shows what the update changes before anything is written. The regenerated files and the local composites change for different reasons, upstream source versus product adaptation, so one review cannot cover both. The branch pins the CLI and starts clean, so the run is repeatable and the diff shows only the update.

**Reviewed for:** Which update branches miss the pinned CLI, the dry-run output, or the separate review of regenerated files and local composites? Look in the update branches.
