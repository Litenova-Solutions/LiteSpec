# User Interface Information Architecture

**Stack:** any

**Concern:** frontend-ui

People find work through pages, while modules bound writes. This page is for teams that build screens, on browser or device frontends. A page that mixes jobs sends each reader to the wrong place, so every route carries one job, one of six locked archetypes, and a reviewed place in navigation. The [verification page](ui-verification.md) states how each route is approved and checked.

## Concepts

### One Job per Screen

Each screen serves one user job stated in one plain sentence in its `job` field. A screen that serves two jobs splits into two screens. The [Edges and Screens page](edges.md) states the screen record with its route or host, its offers, and the flow steps it serves; this page adds the structure that carries those fields.

Claimwick's expense list serves "Find one expense among many and open it." Its expense form serves "Submit one expense with its receipt." The list leaves workspace settings alone, and the form leaves reports alone.

On a phone the same discipline holds with tighter limits. Each Android screen serves one job with 2 to 3 actions. The desktop left sidebar becomes bottom navigation with 3 to 5 primary destinations, and a settings hub list opens one focused drill-down screen per configuration facet.

### The Sitemap and Jobs Table

The project records one sitemap and jobs table from its flows and use cases before frontend code is written or refactored. The owner approves the table, and every route review checks the route against its row.

| Column | Meaning |
|:---|:---|
| `route` | The address the screen serves, such as `/expenses` or `/settings/retention`. |
| `navGroup` | The primary navigation group: daily work, periodic work, or settings. |
| `section` | The optional sub-nav parent for multi-facet areas, such as `settings`. |
| `archetype` | One of the six locked archetypes below. |
| `job` | One plain sentence naming the single user job. |
| `commands` | The offered commands, 4 per screen at most. |

A browser entry point declares its `navigation`: named groups for daily work, periodic work, and settings, with `sections` for multi-facet areas such as settings with its ordered sub-nav items. The primary sidebar holds 3 groups and 10 items at most.

### The Decomposition Table

Each job shape maps to exactly one page structure. The agent assigns use cases to screens by this table, and a route that uses another structure for the same shape carries an information architecture defect.

| User Job Shape | Structure | Example |
|:---|:---|:---|
| Independent setup or admin facets with different cadences | `SettingsPage` with left sub-navigation and sibling sub-routes | Business profile, invoicing defaults, receipt scanning, retention hold, members, audit trail, export, and close workspace each get one focused `/settings/*` sub-route |
| Workspace or business switching | Global app-shell switcher | One switcher in the sidebar or header; content pages carry no second switcher |
| Peer read and history views of one single record | `RecordPage` with URL-backed tabs | Expense detail (`/expenses/{id}`) with tabs: Overview, Payments and Reimbursements, History |
| One high-complexity decision with mutually exclusive branches | Focused `WorkPage` workbench with route-backed mode tabs or isolated mode components | Flagged-expense review keeps a compact pinned summary header plus isolated mode views (`Match`, `Split`, `Reject`), each under 250 lines |
| Quick 1 to 3 field edit or irreversible confirmation on the current item | Modal dialog or right side sheet | Set or lift a retention hold, invite a member, confirm a destructive action |
| Complex creation or editing workflow with 4 or more inputs or multi-line items | Dedicated `FormPage` with capped form column width (`max-w-2xl` or `max-w-3xl`) | New expense report, mileage claim, reimbursement |

A shape the table misses is a gap in this page, with no free choice behind it.

### The Six Locked Archetypes

A route composes exactly one archetype and fills its declared slots. The product package owns the archetype components, and routes compose archetypes, layout primitives, and feature components.

| Archetype | Single Job | Max Content Width | Fixed Slots | Strict Guardrails |
|:---|:---|:---|:---|:---|
| `HomePage` | Show waiting work first and key period summary second | `max-w-7xl` | `header`, `workList`, `metricsRow` (symmetric 3 or 4-up grid) | 1 work queue card at most plus 1 symmetric KPI strip; no buried settings or forms |
| `ListPage` | Search, filter, and open one item among many | `max-w-7xl` | `header` (1 primary CTA), `filterBar`, `table` (or `emptyState`), `pagination` | Single table surface; no secondary forms stacked below the table |
| `WorkPage` | Clear waiting items or make one focused multi-branch decision | `max-w-7xl` | `header`, `contextPane` (document preview or compact summary), `decisionPane` (focused mode tabs or single decision form) | 2-column split on desktop; isolated per-mode sub-components; 1 active form at most at a time |
| `RecordPage` | Inspect one record summary, state, primary action, and peer views | `max-w-6xl` | `header` (title, state badge, 1 primary CTA, overflow menu), optional `tabs` (URL-backed), `main` (2 sections at most), optional `aside` (1 summary or history panel) | Peer facets move to `tabs` or sub-routes; no stacked drawers |
| `SettingsPage` | Configure one domain or workspace area without vertical clutter | `max-w-5xl` | `sectionHeader`, `subNav` (sticky left nav on desktop, select or horizontal nav on mobile), `panel` (single focused `max-w-2xl` form or list plus optional preview `aside`) | One facet per sub-route; consistent inline-save or single-list interaction across all siblings |
| `FormPage` | Complete one multi-field command cleanly | `max-w-3xl` (or `max-w-5xl` with context aside) | `header`, `sections` (`FormSection` groups of 6 fields at most on a 12-column `FieldGrid`), `footer` (`ActionBar` with primary plus cancel) | Form inputs stay clear of full desktop width; uniform 36px control height |

Layout primitives stay strictly typed: `FieldGrid` aligns rows in 12 columns, `ActionBar` orders primary, secondary, and danger actions with matching sizes, `PropertyList` aligns key-value columns with tabular numerals, and `MetricGrid` keeps symmetric desktop columns with no orphan trailing tile. Spacing follows a 4px and 8px scale, and text inputs, selects, date pickers, money inputs, and standard buttons keep a uniform 36px height, with 32px for compact table and inline actions.

Two minimal utility frames cover the edges: `AuthPage` for sign-in and `OutcomePage` for plain confirmations, carrying no other job. Peer facets share one editing mechanism across siblings: inline section forms, dedicated sub-routes, modal dialogs, or drawers throughout. Screens hold 3 flat top-level sections at most with no nested surfaces, and backend-mirrored folders hold API clients and data-fetching adapters alone.

## Standards

### Declare Navigation on Each Browser Entry Point

**ID:** `litespec/core/ui-ia#declare-navigation-on-each-browser-entry-point`

**Strength:** required

**Applies to:** artifact: record:entry-point

**Statement:** Each browser entry point MUST declare its `navigation` groups for daily work, periodic work, and settings, with `sections` for multi-facet areas carrying their ordered sub-nav items.

**Rationale:** One reviewed list replaces one invented sidebar per route. Named groups keep daily and periodic work apart, and `sections` give multi-facet areas one ordered sub-nav.

**Example:** The Claimwick employee app declares daily work (`Home`, `Expenses`, `Approvals`), periodic work (`Reports`, `Close`), and settings (`Settings`, opening `/settings/business` with its ordered sub-nav).

**Checked by:** `litespec model check` reports `record-invalid` for a browser entry point without `navigation`, and for a primary sidebar with more than 3 groups or more than 10 items.

### Declare Each Screen Job and Archetype

**ID:** `litespec/core/ui-ia#declare-each-screen-job-and-archetype`

**Strength:** required

**Applies to:** artifact: record:screen

**Statement:** Each screen MUST declare `route`, its optional sub-nav parent in `section`, its `archetype` from the six locked archetypes, its `job` as one plain sentence naming the single user job, its `offers`, its `primaryAction`, its `states`, and its `leadsTo`.

**Rationale:** A screen without a stated job and archetype is reviewed by feel. One sentence forces the one-job decision up front, and the archetype fixes which slots and guardrails the review checks. `primaryAction`, `states`, and `leadsTo` state what the person starts, what the page shows while busy or empty, and where each path continues. Organized jobs-first with six locked archetypes and rendered-page proof by [decision/0124-jobs-first-ui-verification](../intent/decisions/0124-jobs-first-ui-verification/0124-jobs-first-ui-verification.md).

**Example:** Claimwick's expense list declares `route: /expenses`, `archetype: ListPage`, `job: "Find one expense among many and open it."`, `offers` with the list query and the new-expense command, `primaryAction: new-expense`, `states` for empty and loading, and `leadsTo` with the expense detail route.

**Checked by:** `litespec model check` reports `record-invalid` for a screen without `route`, `archetype`, `job`, `offers`, `primaryAction`, `states`, or `leadsTo`, and for an `archetype` outside the six locked archetypes.

### Limit Each Non-Hub Screen to Four Offered Commands

**ID:** `litespec/core/ui-ia#cap-each-non-hub-screen-at-four-offered-commands`

**Strength:** required

**Applies to:** artifact: record:screen

**Statement:** A screen MUST offer at most 4 commands in `offers`, not counting read-only lookup queries explicitly typed as queries.

**Rationale:** Each offered command is a job the page claims. A fifth offer shares the page with a second job, and the decomposition table applies again. Untyped reads count as offers until proven otherwise.

**Example:** Claimwick's expense detail offers open, approve, request changes, and reimburse: 4 offers. A fifth offer, such as editing workspace settings, moves to its own settings sub-route.

**Reviewed for:** Does every non-hub screen offer at most 4 commands, with only explicitly typed read-only lookups excluded? Check each screen record beside its route.

### Keep Route Files Small

**ID:** `litespec/core/ui-ia#keep-route-files-small`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route file MUST stay under 350 lines with an explicit waiver decision covering any file above that limit.

**Rationale:** A route file of 500 to 1,500 lines holds several jobs, modes, or facets in one file. The count is the cheapest proxy for that defect, pushing modes into isolated parts and facets into sub-routes where the archetype guardrails check them. Teams treat 250 lines as the practical target, with the 350-line limit as the defect boundary.

**Example:** Claimwick's flagged-expense review keeps each mode view under 250 lines by isolating `Match`, `Split`, and `Reject` behind route-backed mode tabs.

**Reviewed for:** Does every route file stay under 350 lines, or does a waiver decision record the reason? Check the route file sizes beside the waiver records.

### Compose Each Screen from Its Owners Queries

**ID:** `litespec/core/ui-ia#compose-each-screen-from-its-owners-queries`

**Strength:** required

**Applies to:** artifact: record:screen

**Statement:** A screen that shows data owned by several modules MUST join each owner's query, and a query that only shows another module's data belongs to that module.

**Rationale:** A screen that copies another module's data into its own query answers one question twice, and the copies drift apart. Joining each owner's query keeps one owner per fact.

**Example:** Claimwick's expense detail joins the spending query for the expense with the payouts query for its reimbursements.

**Reviewed for:** Does every screen join each owner's query, with no query that only shows another module's data? Check each screen beside the owning modules.

### Compose Each Route from One Locked Archetype

**ID:** `litespec/core/ui-ia#compose-each-route-from-one-locked-archetype`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route MUST compose exactly one of the six locked archetypes using only its declared slots.

**Rationale:** Open composition drifts: a list page grows a second form, a record page stacks drawers, a settings page buries a table. One archetype per route fixes the slots up front, and the declared slots keep each job in its approved shape.

**Example:** Claimwick's expense list composes `ListPage` and fills `header`, `filterBar`, `table`, and `pagination`.

**Reviewed for:** Does every route compose exactly one archetype and fill only its declared slots? Check each route against its archetype row.

## Conventions

None.

## Reference Example

This informative example demonstrates `litespec/core/ui-ia#declare-each-screen-job-and-archetype` and `litespec/core/ui-ia#compose-each-route-from-one-locked-archetype`.

Claimwick serves employees who submit expenses and managers who approve them. Its sitemap holds a `HomePage` at `/` showing the waiting approvals queue and one symmetric KPI strip, a `ListPage` at `/expenses` for finding one expense among many, a `RecordPage` at `/expenses/{id}` with URL-backed tabs for Overview, Payments and Reimbursements, and History, a `FormPage` at `/expenses/new` capped at `max-w-2xl` for submitting one expense with its receipt, and a `SettingsPage` at `/settings/*` with left sub-navigation and one focused sub-route per workspace facet. Each route composes one archetype, offers 4 commands at most, and renders 3 flat sections at most.
