# User Interface Verification

**Stack:** any

**Concern:** tests

This page states how a product proves its interface without asking an owner to judge spacing tokens or markup. It is for teams that ship browser or device frontends. A look that rests on taste drifts with each route, so a four-stage workflow carries every frontend from an approved sitemap to checked routes. The [information architecture page](ui-ia.md) states the sitemap, the decomposition table, the screen budgets, and the six locked archetypes this workflow approves and checks.

## Concepts

### The Four Stages

Every consumer interface passes four gates in order. Each gate produces an artifact the next gate consumes, so each route gains an approved job before it is built and checked geometry before it ships.

1. **Stage 1: Sitemap approval.** The agent produces one sitemap and jobs table from flows and use cases (`route`, `navGroup`, `section`, `archetype`, `job`, `commands` of 4 at most). The owner checks that every page carries one clear job and a sensible grouping: a short review of a readable table.
2. **Stage 2: Golden archetype calibration.** The agent builds five real browser-rendered interactive archetype screens: the app shell with navigation and workspace switcher, a list screen, a record detail with URL tabs, a settings screen with left sub-navigation, and a complex form or workbench. The owner opens these five live screens, gives plain visual feedback, and locks the rendered PNGs as the golden baseline.
3. **Stage 3: Constrained route implementation.** Workers build or migrate every route from the approved sitemap using locked archetypes and layout primitives alone, guarded by the route layout rule, the route line-count budget, and post-write checks.
4. **Stage 4: Geometry sensors and visual critic.** Deterministic Playwright sensors run on every route, and an independent vision-capable critic reviews each new or changed route against the golden baseline.

### Stage Gates

Each stage ends in an artifact the next stage consumes.

| Stage | Artifact | Approved By |
|:---|:---|:---|
| 1. Sitemap approval | Sitemap and jobs table with one job per page | Owner, in a short readable review |
| 2. Golden calibration | Five rendered screens with locked PNGs | Owner, in plain visual words |
| 3. Constrained implementation | Every route built from locked archetypes | Layout rule, line budget, and post-write checks |
| 4. Sensors and critic | Sensor passes and critic scores for every route | Suite tolerances and defect tickets |

### The Five Golden Screens

The five calibration screens form the visual contract for the whole product. The owner reviews them rendered in a browser at `1440px`, `1280px`, and `390px` widths, in light and dark modes, and iterates on those five screens until they read as sharp. The locked PNGs sit under `.agents/ui-baseline/`, and each later route is judged against them.

Claimwick locks its app shell with the workspace switcher, its expense list, its expense detail with URL tabs, its workspace settings with left sub-navigation, and its expense form. A later route that drifts from these five carries a defect in that route, with no sixth style behind it.

### Deterministic Geometry Sensors

The sensor suite runs on every route at `1440x900`, `1280x800`, and `390x844` viewports in light and dark modes. Each sensor states a numeric tolerance, so a run passes or fails without judgment. The suite reports each breach with its measured value, and the worker fixes the route or files a ticket.

| Sensor | Tolerance | Scope |
|:---|:---|:---|
| Column edge alignment | Top-level sections in a column share identical `left` and `right` bounding-box edges within 1px | Every route |
| Control uniformity | Buttons, inputs, and selects inside one `ActionBar` or `FieldRow` share identical height with 0 variance and vertical center alignment within 1px | Every route |
| Grid symmetry | Multi-column `MetricGrid` or card grids produce balanced desktop rows with no orphan trailing cell | Routes with grids |
| Form width cap | Main form containers on `FormPage` and `SettingsPage` stay within 720px | Form and settings routes |
| Vertical bloat cap | Non-list, non-report screens stay within 2.2 times the viewport height at `1440x900` | Non-list screens |
| Surface discipline | 3 top-level sections, cards, or drawers at most, and 0 nested surfaces | Every route |
| Baseline health | 0 console errors, 0 failed requests, 0 horizontal overflow, 4.5 to 1 text contrast, 3 to 1 control and focus contrast, exactly one `h1`, visible keyboard focus, and hidden unauthorized commands | Every route |

### Calibration in Plain Words

The owner gives plain visual feedback on the five golden screens ("too cramped", "buttons misaligned", "too noisy"), and the agent iterates those five until they read as sharp. The owner reviews readable artifacts through the workflow: the jobs table, five rendered screens, and defect tickets written in plain words.

## Standards

### Run Geometry Sensors on Every Route

**ID:** `litespec/core/ui-verification#run-geometry-sensors-on-every-route`

**Strength:** required

**Applies to:** artifact: route

**Statement:** Every route MUST pass the deterministic geometry and health sensor suite at `1440x900`, `1280x800`, and `390x844` in light and dark modes: column edges within 1px, control height with 0 variance and centers within 1px, balanced grids with no orphan cell, form containers within 720px, non-list screens within 2.2 viewport heights, at most 3 top-level sections with 0 nested surfaces, and the baseline health checks.

**Rationale:** Alignment, uniform controls, symmetric grids, capped widths, bounded length, and flat surfaces are visible and measurable. Numeric tolerances turn each into a pass or fail, and the health checks ride in the same pass.

**Example:** Claimwick's expense form passes with its `max-w-2xl` container, uniform 36px controls, and one `h1` at all three viewports in both modes.

**Checked by:** `litespec ui review` reports the failing sensor with its measured value for every route outside tolerance.

### Review Changed Routes with an Independent Visual Critic

**ID:** `litespec/core/ui-verification#review-changed-routes-with-an-independent-visual-critic`

**Strength:** required

**Applies to:** artifact: route

**Statement:** Every new or changed route MUST be reviewed by an isolated vision-capable critic context that never built the route, scoring alignment, spacing rhythm, visual hierarchy, symmetry, golden archetype consistency, and single-job focus, with `litespec ui review --since <base>` capturing fresh screenshots beside the locked golden archetype screenshots.

**Rationale:** The builder reads intent instead of pixels. An isolated critic sees the screenshots and the golden baseline, so it reports what a person sees. Six named dimensions keep the review on the product look. A failing score produces concrete defect tickets that agents fix before the owner reviews anything. Settled by [decision/0119-audit-the-rendered-page](decision/0119-audit-the-rendered-page).

**Example:** The critic scores Claimwick's new mileage claim route against the locked expense form baseline and files "primary and cancel buttons differ in height by 4px."

**Checked by:** `litespec ui review --since <base>` records the critic score per dimension for every changed route and files defect tickets for failing scores.

## Conventions

None.

## Reference Example

This informative example demonstrates `litespec/core/ui-verification#run-geometry-sensors-on-every-route` and `litespec/core/ui-verification#review-changed-routes-with-an-independent-visual-critic`.

Claimwick adds a mileage claim route from the sitemap row for "Claim mileage for one trip." The route composes the locked `FormPage`, passes the sensor suite at all three viewports in both modes, and the independent critic scores it against the locked expense form baseline. The critic files one ticket, "cancel action sits 8px below the primary action baseline," the worker fixes the `ActionBar`, and the route ships without owner review of its pixels.
