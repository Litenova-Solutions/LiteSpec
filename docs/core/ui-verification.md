# User Interface Verification

**Stack:** any

**Concern:** tests

This page states how a product proves its user interface without asking an owner to judge spacing tokens or markup: the four-stage workflow that carries every frontend from sitemap to checked routes, the five golden archetype screens the owner locks, the Playwright geometry sensors that run on every route, and the independent visual critic that reviews changed routes. It covers browser and device frontends alike. The [information architecture page](ui-ia.md) states the sitemap, the decomposition table, the screen budgets, and the six locked archetypes this workflow approves and checks.

An owner without interface experience never authors spacing tokens and never reviews markup. The owner reviews only high-leverage readable or rendered artifacts: the jobs table, five rendered screens, and defect tickets written in plain words.

## Concepts

### The Four Stages

Every consumer interface passes four gates in order. Each gate produces an artifact the next gate consumes, so no route is built before its job is approved and no route ships before its geometry and rendering are checked.

1. **Stage 1: Sitemap approval.** The agent produces one sitemap and jobs table from flows and use cases (`route`, `navGroup`, `section`, `archetype`, `job`, `commands` of at most 4). The owner verifies that every page has one clear job and a sensible grouping. This is a short review of a readable table, not of code.
2. **Stage 2: Golden archetype calibration.** The agent builds five real browser-rendered interactive archetype screens: the app shell with navigation and workspace switcher, a list screen, a record detail with URL tabs, a settings screen with left sub-navigation, and a complex form or workbench. The owner opens these five live screens, gives plain visual feedback, and locks the rendered PNGs as the golden baseline.
3. **Stage 3: Constrained route implementation.** Workers build or migrate every route from the approved sitemap using only locked archetypes and layout primitives, guarded continuously by the route layout rule, the route line-count budget, and post-write checks.
4. **Stage 4: Geometry sensors and visual critic.** Deterministic Playwright sensors run on every route, and an independent vision-capable critic reviews every new or changed route against the golden baseline.

### Stage Gates

Each stage ends in an artifact the next stage consumes. No stage starts before the previous artifact is approved.

| Stage | Artifact | Approved By |
|:---|:---|:---|
| 1. Sitemap approval | Sitemap and jobs table with one job per page | Owner, in a short readable review |
| 2. Golden calibration | Five rendered screens with locked PNGs | Owner, in plain visual words |
| 3. Constrained implementation | Every route built from locked archetypes | Layout rule, line budget, and post-write checks |
| 4. Sensors and critic | Sensor passes and critic scores for every route | Suite tolerances and defect tickets |

### The Five Golden Screens

The five calibration screens are the visual contract for the whole product. The owner reviews them rendered in a browser at `1440px`, `1280px`, and `390px` widths, in light and dark modes, and iterates on just those five screens until they read as sharp. The locked PNGs sit under `.agents/ui-baseline/`, and every later route is judged against them.

Claimwick locks its app shell with the workspace switcher, its expense list, its expense detail with URL tabs, its workspace settings with left sub-navigation, and its expense form. A later route that drifts from these five is a defect in that route, not a sixth style.

### Deterministic Geometry Sensors

The sensor suite runs on every route at `1440x900`, `1280x800`, and `390x844` viewports in light and dark modes. Each sensor states a numeric tolerance, so a run passes or fails without judgment. The owner never reviews these numbers; the suite reports each breach with its measured value, and the worker fixes the route or files a ticket.

| Sensor | Tolerance | Scope |
|:---|:---|:---|
| Column edge alignment | Top-level sections in a column share identical `left` and `right` bounding-box edges within 1px | Every route |
| Control uniformity | Buttons, inputs, and selects inside one `ActionBar` or `FieldRow` share identical height with 0 variance and vertical center alignment within 1px | Every route |
| Grid symmetry | Multi-column `MetricGrid` or card grids produce balanced desktop rows with no orphan trailing cell | Routes with grids |
| Form width cap | Main form containers on `FormPage` and `SettingsPage` stay within 720px | Form and settings routes |
| Vertical bloat cap | Non-list, non-report screens stay within 2.2 times the viewport height at `1440x900` | Non-list screens |
| Surface discipline | At most 3 top-level sections, cards, or drawers, and 0 nested surfaces | Every route |
| Baseline health | 0 console errors, 0 failed requests, 0 horizontal overflow, 4.5 to 1 text contrast, 3 to 1 control and focus contrast, exactly one `h1`, visible keyboard focus, and hidden unauthorized commands | Every route |

## Standards

### Approve the Sitemap Before Any Frontend Code

**ID:** `litespec/core/ui-verification#approve-the-sitemap-before-any-frontend-code`

**Strength:** required

**Applies to:** artifact: record:screen

**Statement:** No frontend code MUST be written or refactored before the owner approves the sitemap and jobs table with one clear job per page and commands of at most 4.

**Rationale:** Code written before the sitemap bakes unreviewed jobs into routes: two jobs share a page, a page has no archetype, navigation grows a second copy. Approving the table first makes every later route a fill-in of an approved row. The review stays short because the table shows only the job, the grouping, and the command count.

**Example:** Claimwick's sitemap review approves `/expenses` as a `ListPage` serving "Find one expense among many and open it." with 2 commands before any route file exists.

**Reviewed for:** Was the sitemap approved with one job per page before the first route was written? Check the approval beside the route history.

### Pin Five Golden Archetype Screens

**ID:** `litespec/core/ui-verification#lock-five-golden-archetype-screens`

**Strength:** required

**Applies to:** artifact: route

**Statement:** The product MUST lock five real browser-rendered archetype screens (app shell with navigation and workspace switcher, list, record detail with URL tabs, settings with left sub-navigation, and complex form or workbench) at `1440px`, `1280px`, and `390px` in light and dark modes, with the rendered PNGs stored under `.agents/ui-baseline/`.

**Rationale:** Five locked screens fix the product look once: density, rhythm, control sizes, and navigation behavior. Every later route copies them instead of inventing its own spacing. Rendering them in a real browser at three widths in both modes proves the look holds where people see it, and the stored PNGs give every later comparison a fixed target.

**Example:** Claimwick locks its expense list at all three widths in both modes. Its later reimbursement screens copy the list density instead of re-deciding it.

**Reviewed for:** Do five locked PNG sets sit under `.agents/ui-baseline/`, one per archetype at three widths in both modes? Open the baseline folder.

### Build Routes Only from Locked Archetypes

**ID:** `litespec/core/ui-verification#build-routes-only-from-locked-archetypes`

**Strength:** required

**Applies to:** artifact: route

**Statement:** Every route MUST be built or migrated from the approved sitemap using only locked archetypes and layout primitives, guarded by the route layout rule, the 350-line route budget, and post-write checks.

**Rationale:** An approved sitemap without build guards drifts on the first busy week: a route adds a second form, a raw spacing utility, a nested surface. Building only from locked archetypes with continuous guards keeps each route a fill-in of its row. The layout rule, the line budget from `litespec/core/ui-ia#keep-route-files-small`, and the post-write checks catch the drift at write time.

**Example:** Claimwick's settings migration builds each `/settings/*` sub-route from the locked settings screen, one facet per sub-route, with no raw layout utilities inside route files.

**Reviewed for:** Is every route built from the sitemap with locked archetypes only, within the line budget? Check the routes beside the sitemap rows.

### Run Geometry Sensors on Every Route

**ID:** `litespec/core/ui-verification#run-geometry-sensors-on-every-route`

**Strength:** required

**Applies to:** artifact: route

**Statement:** Every route MUST pass the deterministic geometry and health sensor suite at `1440x900`, `1280x800`, and `390x844` in light and dark modes: column edges within 1px, control height with 0 variance and centers within 1px, balanced grids with no orphan cell, form containers within 720px, non-list screens within 2.2 viewport heights, at most 3 top-level sections with 0 nested surfaces, and the baseline health checks.

**Rationale:** Geometry a person sees is measurable: aligned edges, uniform controls, symmetric grids, capped widths, bounded length, flat surfaces. Numeric tolerances turn each into a pass or fail with no argument about taste. The baseline health checks (errors, overflow, contrast, heading order, focus, hidden commands) run in the same pass, so one suite proves both geometry and accessibility on every route.

**Example:** Claimwick's expense form passes with its `max-w-2xl` container, uniform 36px controls, and one `h1` at all three viewports in both modes.

**Checked by:** `litespec ui review` reports the failing sensor with its measured value for every route outside tolerance.

### Review Changed Routes with an Independent Visual Critic

**ID:** `litespec/core/ui-verification#review-changed-routes-with-an-independent-visual-critic`

**Strength:** required

**Applies to:** artifact: route

**Statement:** Every new or changed route MUST be reviewed by an isolated vision-capable critic context that never built the route, scoring alignment, spacing rhythm, visual hierarchy, symmetry, golden archetype consistency, and single-job focus, with `litespec ui review --since <base>` capturing fresh screenshots beside the locked golden archetype screenshots.

**Rationale:** The builder cannot judge its own rendering: it reads its intent instead of the pixels. An isolated critic sees only the screenshots and the golden baseline, so it reports what a person would see. Scoring the six named dimensions keeps the review on the product look instead of personal taste. A failing score produces concrete defect tickets that agents fix before the owner reviews anything. Settled by [decision/0119-audit-the-rendered-page](decision/0119-audit-the-rendered-page).

**Example:** The critic scores Claimwick's new mileage claim route against the locked expense form baseline and files "primary and cancel buttons differ in height by 4px" instead of asking the owner to look.

**Checked by:** `litespec ui review --since <base>` records the critic score per dimension for every changed route and files defect tickets for failing scores.

## Conventions

### Update Golden Screens in Plain Words

**ID:** `litespec/core/ui-verification#iterate-golden-screens-in-plain-words`

**Strength:** default

**Applies to:** artifact: route

**Default:** The owner gives plain visual feedback on the five golden screens ("too cramped", "buttons misaligned", "too noisy") and the agent iterates only those five until they read as sharp.

**Replacement:** A project records a calibration convention naming who approves the golden screens and in which words.

**Rationale:** Plain words keep calibration open to an owner with no interface vocabulary: nobody needs to name a token or a component. Iterating only the five screens bounds the work, so calibration ends with a locked baseline instead of a running redesign.

**Reviewed for:** Did calibration stay on the five screens with plain-word feedback, or does a convention name another approver? Check the baseline history.

## Reference Example

This informative example demonstrates `litespec/core/ui-verification#run-geometry-sensors-on-every-route` and `litespec/core/ui-verification#review-changed-routes-with-an-independent-visual-critic`.

Claimwick adds a mileage claim route from the sitemap row for "Claim mileage for one trip." The route composes the locked `FormPage`, passes the sensor suite at all three viewports in both modes, and the independent critic scores it against the locked expense form baseline. The critic files one ticket, "cancel action sits 8px below the primary action baseline," the worker fixes the `ActionBar`, and the route ships without owner review of its pixels.
