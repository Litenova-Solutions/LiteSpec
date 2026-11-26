# Svelte UI Primitives

**Stack:** svelte
**Concern:** frontend-ui

A Svelte frontend draws every route from one shared UI package. Routes compose locked archetypes and typed layout primitives. No route invents its own spacing, control heights, or surface nesting.

This page states the Svelte visual contract: the single visual reference, the shadcn-svelte baseline, the locked primitives, the spacing and height tokens, and the lint bans that hold routes to them.

## Concepts

### One Visual Reference Family

Each product declares one visual reference that sets layout density, settings navigation, tables, and visual hierarchy. One reference keeps every route calm and consistent: dense tables read the same on every list, and settings read the same on every sub-route. A second reference would split the product into two looks, so the product record names exactly one.

The Claimwick console declares one calm business reference for density, tables, and hierarchy. Every list, record, and settings route follows it, so a reader who learns one screen has learned them all.

### The `shadcn-svelte` Baseline

The shared package builds on shadcn-svelte near its upstream defaults. Staying near upstream keeps upgrades cheap: a new release merges cleanly because local code changed little. A product that rebuilds dozens of wrappers with drifting padding and heights pays for each one on every upgrade, and review cannot tell which drift is intentional.

The manifest pins `shadcn-svelte` and its companion libraries. The registry command builds the same artifacts in every frontend from those pins.

### Locked Layout Primitives

Four typed primitives carry all page geometry. Each primitive owns one job, so authors compose instead of styling.

| Primitive | One job |
|:---|:---|
| `FieldGrid` | Aligned 12-column rows for form fields, at most 6 fields per group |
| `ActionBar` | Matching button sizes with primary, secondary, and danger order |
| `PropertyList` | Aligned key and value columns with tabular numerals |
| `MetricGrid` | Symmetric desktop columns with no orphan trailing tile |

A fifth piece, `Subsection`, is the only allowed nested divider. It marks two labeled sub-groups inside one card that truly holds two groups. Anything deeper nests surfaces, which the ban below refuses.

Routes fill archetype slots with these primitives and feature components. A slot takes primitives, never raw elements or ad-hoc wrappers. The table names each archetype with its fixed slots.

| Archetype | Fixed slots |
|:---|:---|
| Home | header, work list, metrics row |
| List | header, filter bar, table or empty state, pagination |
| Work | header, context pane, decision pane |
| Record | header, optional tabs, main, optional aside |
| Settings | section header, sub nav, panel |
| Form | header, sections, footer action bar |

### Spacing, Height, and Type

Spacing runs on a 4px and 8px scale. Buttons and control groups use 8px gaps. Sections and neighboring fields use 16px gaps. Card padding uses 20px or 24px. Major layout regions use 24px gaps. One scale keeps rhythm steady across routes: two cards from different authors still line up.

Control height is uniform. Text inputs, selects, date pickers, money inputs, and standard buttons stand 36px tall. Compact table and inline actions stand 32px tall. Uniform height lines up every row and bar without per-case tuning.

Type uses tabular numerals for amounts and aligned columns for properties. Numbers that share a column share a width, so totals scan down instead of jumping.

### Route Composition Discipline

A route file stays thin. It names its archetype, fills each slot with primitives and feature components, and holds no layout logic of its own. Anything the route computes lives in its feature module. Thin routes keep reviews short: the reader checks slot filling, not geometry.

## Standards

### Declare One Visual Reference

**ID:** `litespec/packs/svelte/ui-primitives#declare-one-visual-reference`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A product MUST declare exactly one visual reference in its product record.

**Rationale:** One reference gives every route the same density, navigation, tables, and hierarchy. Two references split the product into two looks, and no check can tell which route follows which.

**Example:** The Claimwick console declares one calm business reference, and every list and settings route follows it.

**Reviewed for:** Does the product record name exactly one visual reference? Check the product record.

### Keep `shadcn-svelte` Close to Upstream

**ID:** `litespec/packs/svelte/ui-primitives#keep-shadcn-svelte-close-to-upstream`

**Strength:** required

**Applies to:** artifact: component

**Statement:** The shared package MUST build on shadcn-svelte near its upstream defaults, at the manifest pin, with no rebuilt wrapper that drifts in padding or height.

**Rationale:** Near-upstream code merges cleanly on each release. Rebuilt wrappers with drifting padding and heights multiply upgrade cost and hide intent: review cannot tell drift from design. Settled by [decision/0024-keep-the-ui-source-lock](decision/0024-keep-the-ui-source-lock), which keeps `litespec model render` writing each copied component's digest into the source lock. Settled by [decision/0102-draw-platform-composites-with-native-elements](decision/0102-draw-platform-composites-with-native-elements), which draws dialog, popover, and details composites from native elements and takes the rest from the baseline.

**Example:** The Claimwick console uses the registry output as built, pinning `shadcn-svelte` from the manifest.

**Reviewed for:** Does any shared component rebuild an upstream primitive with different padding or height? Check the shared package beside the registry output.

### Build Pages from Locked Primitives

**ID:** `litespec/packs/svelte/ui-primitives#build-pages-from-locked-primitives`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** Page geometry MUST come from `FieldGrid`, `ActionBar`, `PropertyList`, and `MetricGrid`, each used for its single job.

**Rationale:** One primitive per job keeps geometry consistent without per-route tuning. A hand-built grid beside `FieldGrid` reimplements its alignment and drifts on the next edit.

**Example:** The Claimwick expense form lays its fields on `FieldGrid`, and its submit row uses `ActionBar` with primary and cancel in order.

**Reviewed for:** Does any route or feature component build grid, bar, property, or metric geometry outside the four primitives? Check the route and module sources.

### Keep Spacing on the 4px and 8px Scale

**ID:** `litespec/packs/svelte/ui-primitives#keep-spacing-on-the-4px-and-8px-scale`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** Gaps and padding MUST use the scale: 8px inside button and control groups, 16px inside sections and between fields, 20px or 24px card padding, and 24px between major regions.

**Rationale:** One scale keeps rhythm steady: cards from different authors still line up. Ad-hoc values break alignment one route at a time.

**Example:** The Claimwick record card pads 24px, its inner sections gap 16px, and its action row gaps 8px.

**Reviewed for:** Does every gap and padding value sit on the stated scale? Check the shared primitives and the route sources.

### Keep Uniform Control Height

**ID:** `litespec/packs/svelte/ui-primitives#keep-uniform-control-height`

**Strength:** required

**Applies to:** artifact: component

**Statement:** Text inputs, selects, date pickers, money inputs, and standard buttons MUST stand 36px tall, with compact table and inline actions at 32px.

**Rationale:** Uniform height lines up every row and bar. Mixed heights force per-case tuning and show as ragged edges on every form.

**Example:** The Claimwick entry form holds every input at 36px, and its table row actions at 32px.

**Reviewed for:** Does every control stand at its stated height? Check the shared field and button components.

### Compose One Archetype per Route

**ID:** `litespec/packs/svelte/ui-primitives#compose-one-archetype-per-route`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route MUST compose exactly one page archetype and fill only its declared slots with primitives and feature components.

**Rationale:** One archetype per route gives each address a single job and a fixed shape. A route that mixes two archetypes serves two jobs, and a slot filled with raw elements bypasses the primitives that keep geometry steady.

**Example:** The Claimwick expense list composes the list archetype: header, filter bar, table, and pagination slots, each filled with primitives.

**Reviewed for:** Does every route compose one archetype with only declared slots filled? Check each route entry.

### Group Form Fields in FieldGrid Sections

**ID:** `litespec/packs/svelte/ui-primitives#group-form-fields-in-fieldgrid-sections`

**Strength:** required

**Applies to:** artifact: form

**Statement:** Form fields MUST sit in `FieldGrid` groups of at most 6 fields on 12 aligned columns.

**Rationale:** Small aligned groups scan as one task: the reader finishes a group before meeting the next. Long ungrouped forms blur tasks together, and unaligned columns make labels jump. Twelve columns divide evenly for halves, thirds, and quarters, so related fields share rows without custom widths.

**Example:** The Claimwick entry form groups payer, date, and amount in one `FieldGrid` group, and lines in the next.

**Reviewed for:** Does every form group hold at most 6 fields on aligned 12-column rows? Check each form.

### Order ActionBar Buttons Primary First

**ID:** `litespec/packs/svelte/ui-primitives#order-actionbar-buttons-primary-first`

**Strength:** required

**Applies to:** artifact: component

**Statement:** An `ActionBar` MUST order its buttons primary, then secondary, then danger, with one primary per bar and matching sizes.

**Rationale:** One order puts the safe forward action where every reader looks first. Two primaries, or danger beside primary, force the reader to weigh buttons instead of reading them. Matching sizes keep the order visible instead of size-led.

**Example:** The Claimwick approval bar shows Approve, then Cancel, with Approve as the single primary.

**Reviewed for:** Does every action bar carry one primary first with matching sizes? Check each action bar.

### Align Property Columns in PropertyList

**ID:** `litespec/packs/svelte/ui-primitives#align-property-columns-in-propertylist`

**Strength:** required

**Applies to:** artifact: component

**Statement:** Key and value columns MUST use `PropertyList` with aligned columns and tabular numerals for amounts.

**Rationale:** Aligned columns let values scan down: the reader compares down a column instead of hunting across rows. Proportional numerals shift totals by digit width, so tabular numerals hold every amount in place.

**Example:** The Claimwick expense summary aligns payer, date, and total in `PropertyList` columns with tabular totals.

**Reviewed for:** Do all property columns align with tabular numerals for amounts? Check each property list.

### Refuse Raw Layout Utilities in Routes

**ID:** `litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes`

**Strength:** required

**Applies to:** artifact: route

**Statement:** Route files MUST NOT use raw layout or spacing utilities directly: `m-*`, `mt-*`, `mb-*`, `mx-*`, `my-*`, `p-*`, `px-*`, `py-*`, `gap-*`, `space-*`, `flex`, `flex-*`, `grid`, `grid-*`, `w-*`, and `h-*`.

**Rationale:** Raw utilities in routes rebuild geometry per address, so two routes drift apart one class at a time. Primitives and feature components already carry the approved spacing and sizes. The ban forces authors to compose instead of styling.

**Example:** A Claimwick route lays its filters through the list archetype filter slot, not through `flex` and `gap-4` classes in `+page.svelte`.

**Checked by:** The route layout-class ESLint rule fails on any banned utility inside `apps/web/src/routes/**/+page.svelte`.

### Refuse Nested Surfaces

**ID:** `litespec/packs/svelte/ui-primitives#refuse-nested-surfaces`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A component MUST NOT render a card, panel, drawer, or dialog inside another card, panel, drawer, or dialog.

**Rationale:** Nested surfaces stack shadows and borders until hierarchy flattens: everything looks equally raised, so nothing reads as primary. One level of surface per region keeps the primary action obvious. Settled by [decision/0118-layer-surfaces-by-role](decision/0118-layer-surfaces-by-role), which layers surfaces by role so a card inside a card reads as two levels instead of one.

**Example:** The Claimwick record page holds one card per region instead of a card inside a card.

**Reviewed for:** Does any card, panel, drawer, or dialog render inside another card, panel, drawer, or dialog? Check the component markup.

### Keep Focus Visible on Every Control

**ID:** `litespec/packs/svelte/ui-primitives#keep-focus-visible-on-every-control`

**Strength:** required

**Applies to:** artifact: component

**Statement:** Every interactive control MUST show a visible focus indicator with at least 3:1 contrast against its background.

**Rationale:** Keyboard readers tab through controls without a pointer, so a control that hides focus strands them: they act without knowing where they are. A visible indicator at 3:1 contrast shows position on every theme.

**Example:** The Claimwick entry form outlines the focused input with the focus ring from the shared package.

**Reviewed for:** Does every control show visible focus at 3:1 contrast? Check each route with keyboard navigation.

### Keep One H1 per Route

**ID:** `litespec/packs/svelte/ui-primitives#keep-one-h1-per-route`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route MUST render exactly one `h1` naming its job, with section titles as lower-level headings.

**Rationale:** One `h1` names the route for readers and assistive tools. Two `h1` headings split the page into two subjects, and none leaves the route unnamed. Lower-level headings structure the sections beneath it.

**Example:** The Claimwick expense list renders one `h1` with the list name, and its filter groups use lower headings.

**Reviewed for:** Does every route render exactly one `h1`? Check each route entry.

### Use One Icon Set

**ID:** `litespec/packs/svelte/ui-primitives#use-one-icon-set`

**Strength:** required

**Applies to:** artifact: component

**Statement:** Icons MUST come from the single icon set the manifest pins, at consistent sizes per use.

**Rationale:** One set keeps stroke weight and style steady: mixed sets show thick icons beside thin ones. Consistent sizes per use keep actions aligned in bars and rows.

**Example:** The Claimwick console draws every action icon from the pinned set at the bar and row sizes the shared package states.

**Reviewed for:** Does any icon come from outside the pinned set or at an ad-hoc size? Check the icon uses in each route.

### Use Subsection for Grouped Content

**ID:** `litespec/packs/svelte/ui-primitives#use-subsection-for-grouped-content`

**Strength:** required

**Applies to:** artifact: component

**Statement:** A card that truly holds two labeled sub-groups MUST divide them with the lightweight `Subsection` divider, never with a nested surface.

**Rationale:** Two labeled groups inside one card need a visual break without a second elevation. `Subsection` draws that break: a label and a rule, no shadow. A nested card would add elevation where none is earned.

**Example:** The Claimwick settings panel divides its notification groups with `Subsection` dividers inside one card.

**Reviewed for:** Does every grouped card divide its groups with `Subsection`, with no nested surface? Check the card components.

## Conventions

### Keep Metric Grids Symmetric

**ID:** `litespec/packs/svelte/ui-primitives#keep-metric-grids-symmetric`

**Strength:** default

**Applies to:** artifact: component

**Default:** Lay metric tiles in balanced desktop rows with no orphan trailing tile.

**Replacement:** A product with a metric set that never divides evenly names its trailing-tile rule in its product record.

**Rationale:** An orphan tile stretches or shrinks against empty space and draws the eye for no reason. Balanced rows keep every metric equal weight. A set that never divides evenly needs one recorded rule, not per-route improvisation.

**Example:** The Claimwick period summary shows four KPI tiles in one symmetric row, never three plus one.

**Reviewed for:** Does every metric grid show balanced rows with no orphan tile? Check each metric grid at desktop width.

### Keep Form Content Narrow

**ID:** `litespec/packs/svelte/ui-primitives#keep-form-content-narrow`

**Strength:** default

**Applies to:** artifact: form

**Default:** Cap form content at the narrow width with full-width inputs never stretching across the desktop.

**Replacement:** A product whose forms need a context aside names the wider cap and its aside rule in its product record.

**Rationale:** Narrow forms scan in one column: labels, inputs, and errors stay in one vertical rhythm. Full-width inputs stretch across the desktop and break that rhythm, so the eye travels instead of reading down.

**Example:** The Claimwick entry form caps its sections narrow with inputs at full section width, never at full desktop width.

**Reviewed for:** Does any form stretch inputs across the full desktop width? Check each form container width.
