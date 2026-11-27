# Svelte UI Primitives

**Stack:** svelte
**Concern:** frontend-ui

A Svelte frontend draws every route from one shared UI package. This page is for developers who compose routes from locked archetypes and typed primitives. A hand-built grid beside the primitives drifts on the next edit, and two visual references split the product into two looks.

## Concepts

### One Visual Reference and a Near-Upstream Baseline

Each product declares one visual reference for density, settings navigation, tables, and visual hierarchy. The Claimwick console declares one calm business reference, and every list, record, and settings route follows it. A reader who learns one screen has learned them all.

The shared package builds on shadcn-svelte near its upstream defaults. The manifest pins `shadcn-svelte` with its companion libraries, and the registry command builds the same artifacts in every frontend. Near-upstream code merges cleanly, while rebuilt wrappers with drifting padding hide intent from review.

### Locked Primitives and Archetypes

Four typed primitives carry page geometry, each with one job. The table below names each primitive with its job. Routes fill archetype slots with these primitives and feature components, and a slot takes primitives, not raw elements or ad-hoc wrappers.

| Primitive | One job |
|:---|:---|
| `FieldGrid` | Aligned 12-column rows for form fields, at most 6 fields per group |
| `ActionBar` | Matching button sizes with primary, secondary, and danger order |
| `PropertyList` | Aligned key and value columns with tabular numerals |
| `MetricGrid` | Symmetric desktop columns with no orphan trailing tile |

A fifth piece, `Subsection`, is the allowed nested divider. It marks two labeled sub-groups inside one card that truly holds two groups. Anything deeper nests surfaces, and the surface rule below keeps one level per region.

| Archetype | Fixed slots |
|:---|:---|
| Home | header, work list, metrics row |
| List | header, filter bar, table or empty state, pagination |
| Work | header, context pane, decision pane |
| Record | header, optional tabs, main, optional aside |
| Settings | section header, sub nav, panel |
| Form | header, sections, footer action bar |

The table above names each archetype with its fixed slots. The Claimwick expense list fills the list slots with header, filter bar, table, and pagination. The Claimwick expense form lays its fields on `FieldGrid` with its submit row in `ActionBar`.

### Spacing, Type, and Control Rhythm

Spacing runs on a 4px and 8px scale: 8px inside button and control groups, 16px inside sections and between fields, 20px or 24px card padding, and 24px between major regions. The Claimwick record card pads 24px, gaps its sections 16px, and gaps its action row 8px. Two cards from different authors still line up on that scale.

Text inputs, selects, date pickers, money inputs, and standard buttons stand 36px tall, with compact table and inline actions at 32px. Amounts use tabular numerals and property columns align, so totals scan down instead of jumping. Uniform height lines up rows and bars without per-case tuning.

Form fields sit in `FieldGrid` groups of at most 6 fields on 12 aligned columns. The Claimwick entry form groups payer, date, and amount in one group and lines in the next. Form content caps at the narrow width, with inputs at full section width.

An `ActionBar` orders primary, secondary, and danger with one primary per bar in matching sizes. The Claimwick approval bar shows Approve before Cancel with Approve as the single primary. Property keys and values align in `PropertyList` columns, and a card with two labeled groups divides them with a `Subsection` divider instead of a nested card.

Controls show a visible focus indicator with 3:1 contrast, and each route renders one `h1` naming its job with lower headings beneath. Icons come from the single manifest-pinned set at consistent sizes per use. Metric tiles sit in balanced desktop rows with no orphan trailing tile, as the Claimwick period summary shows with four tiles in one row.

## Standards

### Declare One Visual Reference

**ID:** `litespec/packs/svelte/ui-primitives#declare-one-visual-reference`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A product MUST declare exactly one visual reference in its product record.

**Rationale:** One reference gives every route the same density, navigation, tables, and hierarchy. Two references split the product into two looks. No check tells which route follows which once the product splits.

**Example:** The Claimwick console declares one calm business reference, and every list and settings route follows it. A reader meets the same tables and hierarchy on each route.

**Reviewed for:** Does the product record name exactly one visual reference? Check the product record.

### Keep `shadcn-svelte` Close to Upstream

**ID:** `litespec/packs/svelte/ui-primitives#keep-shadcn-svelte-close-to-upstream`

**Strength:** required

**Applies to:** artifact: component

**Statement:** The shared package MUST build on shadcn-svelte near its upstream defaults, at the manifest pin, with no rebuilt wrapper that drifts in padding or height.

**Rationale:** Near-upstream code merges cleanly on each release. Rebuilt wrappers with drifting padding and heights multiply upgrade cost and hide intent. Review tells drift from design only when local changes stay small. Settled by [decision/0024-keep-the-ui-source-lock](decision/0024-keep-the-ui-source-lock).

**Example:** The Claimwick console uses the registry output as built, pinning `shadcn-svelte` from the manifest. Its shared components track upstream instead of rebuilding it.

**Reviewed for:** Does any shared component rebuild an upstream primitive with different padding or height? Check the shared package beside the registry output.

### Build Pages from Locked Primitives

**ID:** `litespec/packs/svelte/ui-primitives#build-pages-from-locked-primitives`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** Page geometry MUST come from `FieldGrid`, `ActionBar`, `PropertyList`, and `MetricGrid`, each used for its single job.

**Rationale:** One primitive per job keeps geometry consistent without per-route tuning. A hand-built grid beside `FieldGrid` reimplements its alignment and drifts on the next edit.

**Example:** The Claimwick expense form lays its fields on `FieldGrid`, and its submit row uses `ActionBar` with primary and cancel in order. Geometry comes from the primitives on both parts.

**Reviewed for:** Does any route or feature component build grid, bar, property, or metric geometry outside the four primitives? Check the route and module sources.

### Keep Spacing on the 4px and 8px Scale

**ID:** `litespec/packs/svelte/ui-primitives#keep-spacing-on-the-4px-and-8px-scale`

**Strength:** required

**Applies to:** artifact: component, route

**Statement:** Gaps and padding MUST use the scale: 8px inside button and control groups, 16px inside sections and between fields, 20px or 24px card padding, and 24px between major regions.

**Rationale:** One scale keeps rhythm steady across routes. Cards from different authors still line up. Ad-hoc values break alignment one route at a time.

**Example:** The Claimwick record card pads 24px, its inner sections gap 16px, and its action row gaps 8px. Each value sits on the scale.

**Reviewed for:** Does every gap and padding value sit on the stated scale? Check the shared primitives and the route sources.

### Compose One Archetype per Route

**ID:** `litespec/packs/svelte/ui-primitives#compose-one-archetype-per-route`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route MUST compose exactly one page archetype and fill only its declared slots with primitives and feature components.

**Rationale:** One archetype per route gives each address a single job and a fixed shape. A route mixing two archetypes serves two jobs. A slot filled with raw elements bypasses the primitives that keep geometry steady.

**Example:** The Claimwick expense list composes the list archetype with header, filter bar, table, and pagination slots. Each slot holds primitives and feature components.

**Reviewed for:** Does every route compose one archetype with only declared slots filled? Check each route entry.

### Refuse Raw Layout Utilities in Routes

**ID:** `litespec/packs/svelte/ui-primitives#refuse-raw-layout-utilities-in-routes`

**Strength:** required

**Applies to:** artifact: route

**Statement:** Route files MUST NOT use raw layout or spacing utilities directly: `m-*`, `mt-*`, `mb-*`, `mx-*`, `my-*`, `p-*`, `px-*`, `py-*`, `gap-*`, `space-*`, `flex`, `flex-*`, `grid`, `grid-*`, `w-*`, and `h-*`.

**Rationale:** Raw utilities in routes rebuild geometry per address. Two routes drift apart one class at a time. Primitives and feature components already carry the approved spacing and sizes.

**Example:** A Claimwick route lays its filters through the list archetype filter slot, not through `flex` and `gap-4` classes in `+page.svelte`. Composition replaces styling in the route file.

**Checked by:** The route layout-class ESLint rule fails on any banned utility inside `apps/web/src/routes/**/+page.svelte`.

## Conventions

None.
