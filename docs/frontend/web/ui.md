# Web UI

**Stack:** react, svelte
**Concern:** frontend-ui

One shared UI package carries what every route on a web frontend composes: the registry of page references, the token stylesheet, and the baseline source it installs once.

[Controlled UI Governance](../ui.md) states the UI rules shared by every stack. This page states how the React and Svelte web stacks apply them: what the shared package publishes, what the route suite and visual goldens carry, which CSS and platform features routes may use, and how each primitive traces to its pinned source.

## Concepts

### The Web Baseline

The baseline is the pinned UI source the shared package installs once. The manifest names it in the `uiBaseline` entry on React and the `uiBaselineSvelte` entry on Svelte. Each stack page names the command that builds the registry.

### The Browser Route Suite

The browser suite discovers its routes from the generated route tree rather than from a handwritten list, so a new route is checked without anyone writing a specification for it. A dynamic segment resolves to a record the deployment holds, or to an absent record when it holds none.

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
| Color scheme | The root's computed `color-scheme` names each scheme the project declares, and axe reports zero violations with each one emulated |
| Motion | With `prefers-reduced-motion: reduce` emulated, `document.getAnimations()` holds no running transform, scale, or infinitely repeating animation once the route settles |
| Announcements | One `role="status"` region and one `role="alert"` region exist when the route loads, and asynchronous status changes announce through them |

The judgments a page-level assertion cannot make live in `litespec ui audit`, which opens each route in a browser and reads the rendered page. It reports eleven codes: `surface-nesting-invalid`, `surface-contrast-failed`, `field-surface-missing`, `field-width-invalid`, `action-group-invalid`, `primary-action-duplicate`, `column-alignment-invalid`, `text-code-invalid`, `tab-label-invalid`, `table-clip-invalid`, and `page-exception-unhandled`. The [UI audit command](../../tools/ui-audit.md) states how to run it.

### The Browser Support Target

The design contract's Browser Support section names one Baseline tier, the set of web platform features that every major browser supports:

| Tier | A feature belongs to it when |
|:---|:---|
| Baseline widely available | Every major browser has supported it for at least 30 months |
| Baseline newly available | Every major browser supports it |

The default is Baseline widely available. A team decides each newer feature in this order, and the first rule that matches wins:

1. The feature belongs to the declared tier: use it with no fallback.
2. The feature belongs to Baseline newly available but not to the declared tier: detect the feature and let an unsupported browser show its default behavior.
3. A fallback fits in the same file, behind `@supports` in CSS or a feature check in script: write it there.
4. The fallback needs a package: record the decision that admits the package, as `litespec/frontend/ui#keep-each-behavior-package-justified-and-on-baseline` states.
5. No fallback fits: change the design so it needs no such feature.

Tailwind CSS v4 itself needs a recent browser, so a project that targets older browsers than the styling engine supports records the gap in its design contract.

### Pattern Implementation

A product records its UI pattern choices in one catalog, and each chosen pattern needs one implementation per web platform. The catalog names the source path for each platform, for example `packages/ui/src/table/sort-header.tsx` under `react` and `packages/ui/src/table/sort-header.svelte` under `svelte`, so every route composes that implementation, per `litespec/frontend/patterns#implement-a-chosen-pattern-once`.

LiteSpec options are reviewed on the pattern's page on the LiteSpec site at the catalog breakpoints. Product-added options are drawn in a product registry at the same breakpoints, per `litespec/frontend/patterns#keep-a-visual-registry`.

### Rules Kept on the Base Page

The shared UI rules live on [Controlled UI Governance](../ui.md): the baseline, floorplans, layout, page references, tones, tokens, token limits, screen navigation, styling, route checks, result proof, and change evidence. The web stacks add the registry, the suite marks, the CSS rules, and the source rules this page states.

- Baseline pin: `litespec/frontend/ui#pin-the-baseline-in-the-shared-package`
- Floorplan composition: `litespec/frontend/ui#compose-each-route-from-one-floorplan`
- Layout ownership: `litespec/frontend/ui#keep-layout-inside-floorplans`
- Page reference: `litespec/frontend/ui#name-the-reference-behind-each-page-type`
- State tones: `litespec/frontend/ui#select-a-tone-from-the-closed-set`
- Token definition: `litespec/frontend/ui#keep-tokens-in-one-definition`
- Token limits: `litespec/frontend/ui#meet-the-limits-of-every-token-role`
- Token file: `litespec/frontend/ui#keep-token-values-in-one-token-file`
- Screen navigation: `litespec/frontend/ui#link-each-screen-to-its-next-screens`
- Styling gate: `litespec/frontend/ui#restrict-styling-to-semantic-tokens`
- Route checks: `litespec/frontend/ui#pass-every-route-suite-check`
- Result proof: `litespec/core/edges#write-the-sentence-a-person-reads-on-each-refusal`
- Change evidence: `litespec/frontend/ui#name-the-evidence-for-each-ui-change`

## Standards

### Publish One Registry Item per Floorplan

**ID:** `litespec/frontend/web/ui#publish-one-registry-item-per-floorplan`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The shared package MUST publish a registry whose build exits zero and which lists one item per floorplan the design contract declares.

**Rationale:** Without an installable page for each floorplan, an author invents a page from primitives, and the next author invents another. One registry item per floorplan the design contract declares gives every page type a reference the author installs before writing. A registry build that exits zero proves every item still builds.

**Checked by:** `shadcn build` on React or `shadcn-svelte build` on Svelte exits zero, and `registry.json` lists one item per declared floorplan.

### Draw Platform Composites with Native Elements

**ID:** `litespec/frontend/web/ui#draw-platform-composites-with-native-elements`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A dialog, popover, or details composite MUST build on the platform dialog, popover, or details element.

**Rationale:** The platform dialog carries focus handling, dismissal, and layering; the popover carries anchoring and light dismissal; details carries disclosure. A composite rebuilding any of them on generic elements reimplements that behavior, usually worse, and each page then tunes the copy for itself. Composites the platform lacks, such as a combobox and a date picker, stay on the pinned baseline, which exists for what the platform does not provide. Each native element still meets the declared browser support tier under `litespec/frontend/web/ui#build-within-the-browser-support-target`. Settled by [decision/0102-draw-platform-composites-with-native-elements](decision/0102-draw-platform-composites-with-native-elements).

**Example:** The Claimwick shared package builds its confirmation dialog on the platform dialog element, and takes its expense-category combobox from the pinned baseline.

**Reviewed for:** Does every dialog, popover, and details composite build on its native element? Look in the shared package composites.

### Mark the Route Suite and Goldens with What They Prove

**ID:** `litespec/frontend/web/ui#mark-the-route-suite-and-goldens-with-what-they-prove`

**Strength:** required

**Applies to:** artifact: test

**Statement:** The route suite and every visual golden MUST carry a `proves` annotation for each UI provision, pattern provision, or quality requirement they enforce.

**Rationale:** The route suite is the one place accessibility is enforced for every page, and a visual golden is the one place a pattern's look is held. Until each names what it enforces, no tool can read that evidence from it. The marks turn the suite into the evidence for the UI provisions it checks and each golden into the evidence for the pattern it protects, so a passing run marks those records tested. Neither proves a scenario: a route suite asserts every page, not one use case. Each frontend names its route suite in `routeSuite` and its goldens in `visualGoldens` in its `frontends[]` entry, so the check reads a named subject instead of a convention.

**Example:** Claimwick's route suite, named in its web frontend's `routeSuite`, carries `proves("litespec/frontend/ui#pass-every-route-suite-check")`.

**Checked by:** `litespec model check` reports `mark-missing` (level S) for a test in a frontend's `routeSuite` or `visualGoldens` with no `proves` annotation, and `mark-unknown` (level S) for one naming a scenario.

### Write Tailwind Classes with Semantic Tokens Only

**ID:** `litespec/frontend/web/ui#write-tailwind-classes-with-semantic-tokens-only`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** A Tailwind class MUST use a semantic token rather than an arbitrary value, a raw palette value, or an important modifier.

**Rationale:** Semantic tokens keep every class inside the governed palette, spacing, and type scales. An arbitrary value, a raw palette value, or an important modifier steps outside them: the value belongs to no token and survives every restyle. The approved CSS surface is the shared package's stylesheet, holding the Tailwind import, the tokens, the fonts, and the documented resets. The source scan skips build output and reports authored feature CSS. This realizes litespec/frontend/ui#restrict-styling-to-semantic-tokens.

**Checked by:** `litespec model check` reports `ui-class-invalid` for each arbitrary value, raw palette value, and important modifier, and `ui-stylesheet-invalid` for each inline style, with its file.

### Build Within the Browser Support Target

**ID:** `litespec/frontend/web/ui#build-within-the-browser-support-target`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** The shared package and the routes MUST use only web platform features in the tier the design contract's Browser Support section names, or carry a fallback beside each other feature.

**Rationale:** A feature outside the declared tier breaks silently for the staff member whose browser lacks it: a dialog that never opens, a layout that collapses, a control that does nothing. Naming one tier in the design contract lets every author and agent judge a feature against the same line. The decision order in the Concepts section says what to write when a feature falls outside it.

**Example:** A design contract that declares Baseline widely available lets the expense record header switch its layout with a size container query, which belongs to that tier, with no fallback. A tooltip placed with CSS anchor positioning, which belongs to neither tier yet, keeps the script positioning of the baseline popover component instead.

**Reviewed for:** Does every platform feature in the shared package or a route sit in the declared tier or carry a fallback beside it? Check the stylesheets and components against the design contract's Browser Support section.

### Render Each Declared Color Scheme

**ID:** `litespec/frontend/web/ui#render-each-declared-color-scheme`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A web frontend MUST declare on its root element the color schemes its tokens define and pass the route suite in each of them.

**Rationale:** A token set with a dark scheme that no check renders ships unreadable text to every staff member whose device is set to dark. Declaring `color-scheme` also lets the browser draw scrollbars, form controls, and the page before styles load in the matching scheme, so nothing flashes white. A product that defines only a light scheme declares `color-scheme: light` and is checked in that scheme only.

**Reviewed for:** Does every route pass the route suite in each declared color scheme, with the root declaring `color-scheme`? Check the route suite output and the shared stylesheet.

### Honor Reduced Motion

**ID:** `litespec/frontend/web/ui#honor-reduced-motion`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A route MUST run no spatial, scaling, or looping animation while the reader's system asks for reduced motion.

**Rationale:** WCAG 2.3.3 Animation from Interactions: a drawer that slides or a card that zooms can bring on nausea or dizziness for a reader with a vestibular condition. Under reduced motion, a transition becomes a short fade or nothing, and every animation still ends in its final state, so the screen shows the same result without the movement. The design contract's Motion section names what each duration becomes. The mobile stacks hold the same rule in `litespec/frontend/mobile/screens#honor-reduced-motion`.

**Reviewed for:** Does any route still slide, scale, or loop an animation with reduced motion emulated? Check the route suite output.

### Give Moving Content a Pause Control

**ID:** `litespec/frontend/web/ui#give-moving-content-a-pause-control`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** Content that moves, scrolls, or updates on its own for more than five seconds MUST offer a control that pauses, stops, or hides it.

**Rationale:** WCAG 2.2.2 Pause, Stop, Hide: a ticker of incoming expenses or a rotating banner pulls the eye away from the form a staff member is filling in, and a screen-reader user hears it interrupt. A pause control gives the reader the page back.

**Reviewed for:** Does any moving or self-updating content run longer than five seconds with no pause, stop, or hide control? Check the routes that show live feeds, carousels, or animated status.

### Set Text Sizes in Relative Units

**ID:** `litespec/frontend/web/ui#set-text-sizes-in-relative-units`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A font-size token MUST use `rem` rather than `px`.

**Rationale:** WCAG 1.4.4 Resize Text: a reader who sets a larger default text size in the browser or the system expects every page to follow it. A size fixed in pixels ignores that setting. A `rem` token scales with the reader's setting, and the reflow and text-spacing checks then prove that the layout still holds.

**Reviewed for:** Is any font-size token written in `px`? Check the shared package's token stylesheet.

### Record Primitive Sources in the Source Lock

**ID:** `litespec/frontend/web/ui#record-primitive-sources-in-the-source-lock`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The shared package's source lock MUST record the pinned CLI, the registry address, the paths, the normalized digest, and the dependencies for each primitive.

**Rationale:** A primitive whose source drifts from the registry is a fork, and without a lock the fork passes unreported. One lock beside one copy makes a changed primitive report a fork instead. Normalizing source before hashing lets two workspaces compute the same digest for identical source. `litespec model render` writes the digests, the preset and the CLI pin, so a person reviews a changed digest in the pull request diff and never computes one. Settled by [decision/0024-keep-the-ui-source-lock](decision/0024-keep-the-ui-source-lock).

**Checked by:** `litespec model check` recomputes each digest and reports `ui-sourcelock-invalid` for changed primitive source.

### Keep the Tone Table in the Stack States File

**ID:** `litespec/frontend/web/ui#keep-the-tone-table-in-the-stack-states-file`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The state-to-tone table MUST live in the states file its stack states: `src/app/ui/states.tsx` on React and `src/lib/app/ui/states.ts` on Svelte.

**Rationale:** One file per stack holds every state tone, so a state shows the same tone on every route. This realizes litespec/frontend/ui#select-a-tone-from-the-closed-set.

**Example:** The library product maps its overdue state to its warning tone in `src/app/ui/states.tsx` on React, and the Claimwick console maps its overdue state in `src/lib/app/ui/states.ts` on Svelte.

**Reviewed for:** Does every state-to-tone mapping live in the states file its stack states? Search the `src` tree for tone maps.

## Conventions

### Use the Source Update Procedure

**ID:** `litespec/frontend/web/ui#use-the-source-update-procedure`

**Strength:** default

**Applies to:** artifact: any

**Default:** Update the primitive source on a branch that pins the CLI, starts clean, and reviews the regenerated files and the local composites separately.

**Replacement:** A consumer can replace this default by naming its source-update branch procedure in its documentation.

**Rationale:** Running `view`, `--dry-run`, and `--diff` first shows what the update changes before anything is written. The regenerated files and the local composites change for different reasons, upstream source versus product adaptation, so one review cannot cover both. The branch pins the CLI and starts clean, so the run is repeatable and the diff shows only the update.

**Reviewed for:** Does every update branch pin the CLI, show the dry-run output, and review the regenerated files and the local composites separately? Check the update branches.
