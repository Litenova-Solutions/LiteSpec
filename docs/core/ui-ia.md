# User Interface Information Architecture

**Stack:** any

**Concern:** frontend-ui

This page states how the product organizes its pages around user jobs: the sitemap and jobs table every frontend is approved against, the decomposition table that assigns each job shape to one page structure, the budgets that keep every screen focused, and the six locked page archetypes each route composes. It covers browser and device frontends alike. A project applies it whenever people reach the product through screens, and the [verification page](ui-verification.md) states how each route is approved and checked.

Backend modules organize write boundaries. Routes and navigation organize user jobs, their cadence, and their focus. Folders that mirror backend modules hold API clients and data-fetching adapters only (`lib/modules/` or `packages/api/`), never the decision of how pages, tabs, or navigation trees are structured. A reader who has learned this page has learned the shape of every route review.

## Concepts

### One Job per Screen

Each screen serves one user job stated in one plain sentence in its `job` field. A screen that serves two jobs splits into two screens. The [Edges and Screens page](edges.md) states the screen record, its route or host, its offers, and the flow steps it serves. This page adds the structure that carries those fields: where the screen sits in navigation, which archetype it composes, and the budgets its route honors.

Claimwick's expense list serves "Find one expense among many and open it." Its expense form serves "Submit one expense with its receipt." Neither page also edits workspace settings or shows reports. When a second job appears on a page, the sitemap moves it to its own route.

On a phone the same discipline holds with tighter limits. Each Android screen serves one job with at most 2 to 3 actions. The desktop left sidebar becomes bottom navigation with 3 to 5 primary destinations, and a clean settings hub list pushes one focused drill-down screen per configuration facet instead of stacking several forms on one phone screen.

### The Sitemap and Jobs Table

Before any frontend code is written or refactored, the project records one sitemap and jobs table from its flows and use cases. Each row names `route`, `navGroup`, `section`, `archetype`, `job`, and `commands`. The owner approves this table, as the [verification page](ui-verification.md) states, and every route review checks the route against its row.

| Column | Meaning |
|:---|:---|
| `route` | The address the screen serves, such as `/expenses` or `/settings/retention`. |
| `navGroup` | The primary navigation group the route sits in: daily work, periodic work, or settings. |
| `section` | The optional sub-nav parent for multi-facet areas, such as `settings`. |
| `archetype` | One of the six locked archetypes below. |
| `job` | One plain sentence naming the single user job. |
| `commands` | The offered commands, at most 4 per screen. |

A browser entry point declares its `navigation`: named groups for daily work, periodic work, and settings, with `sections` for multi-facet areas such as settings with its ordered sub-nav items. The primary sidebar holds at most 3 groups and at most 10 items. Periodic work (month-end close, yearly reports) sits apart from daily work (expenses, approvals), so a person finds each job where its cadence puts it.

### The Decomposition Table

Each job shape maps to exactly one page structure. The agent assigns use cases to screens by this table, and a route that uses another structure for the same shape is an information architecture defect.

| User Job Shape | Required Structure | Example |
|:---|:---|:---|
| Independent setup or admin facets with different cadences | `SettingsPage` with left sub-navigation and sibling sub-routes | Business profile, invoicing defaults, receipt scanning, retention hold, members, audit trail, export, and close workspace each get one focused `/settings/*` sub-route |
| Workspace or business switching | Global app-shell switcher | One switcher in the sidebar or header; content pages carry no second switcher |
| Peer read and history views of one single record | `RecordPage` with URL-backed tabs | Expense detail (`/expenses/{id}`) with tabs: Overview, Payments and Reimbursements, History |
| One high-complexity decision with mutually exclusive branches | Focused `WorkPage` workbench with route-backed mode tabs or isolated mode components | Flagged-expense review keeps a compact pinned summary header plus isolated mode views (`Match`, `Split`, `Reject`), each under 250 lines |
| Quick 1 to 3 field edit or irreversible confirmation on the current item | Modal dialog or right side sheet | Set or lift a retention hold, invite a member, confirm a destructive action |
| Complex creation or editing workflow with 4 or more inputs or multi-line items | Dedicated `FormPage` with capped form column width (`max-w-2xl` or `max-w-3xl`) | New expense report, mileage claim, reimbursement |

### The Six Locked Archetypes

A route composes exactly one archetype and fills only its declared slots. The product package owns the archetype components, and routes compose archetypes, layout primitives, and feature components, nothing else.

| Archetype | Single Job | Max Content Width | Fixed Slots | Strict Guardrails |
|:---|:---|:---|:---|:---|
| `HomePage` | Show waiting work first and key period summary second | `max-w-7xl` | `header`, `workList`, `metricsRow` (symmetric 3 or 4-up grid) | At most 1 work queue card plus 1 symmetric KPI strip; no buried settings or forms |
| `ListPage` | Search, filter, and open one item among many | `max-w-7xl` | `header` (1 primary CTA), `filterBar`, `table` (or `emptyState`), `pagination` | Single table surface; no secondary forms stacked below the table |
| `WorkPage` | Clear waiting items or make one focused multi-branch decision | `max-w-7xl` | `header`, `contextPane` (document preview or compact summary), `decisionPane` (focused mode tabs or single decision form) | 2-column split on desktop; isolated per-mode sub-components; at most 1 active form at a time |
| `RecordPage` | Inspect one record summary, state, primary action, and peer views | `max-w-6xl` | `header` (title, state badge, 1 primary CTA, overflow menu), optional `tabs` (URL-backed), `main` (at most 2 sections), optional `aside` (1 summary or history panel) | Peer facets move to `tabs` or sub-routes; no stacked drawers |
| `SettingsPage` | Configure one domain or workspace area without vertical clutter | `max-w-5xl` | `sectionHeader`, `subNav` (sticky left nav on desktop, select or horizontal nav on mobile), `panel` (single focused `max-w-2xl` form or list plus optional preview `aside`) | One facet per sub-route; consistent inline-save or single-list interaction across all siblings |
| `FormPage` | Complete one multi-field command cleanly | `max-w-3xl` (or `max-w-5xl` with context aside) | `header`, `sections` (`FormSection` groups of at most 6 fields on a 12-column `FieldGrid`), `footer` (`ActionBar` with primary plus cancel) | Never stretches form inputs across full desktop width; uniform 36px control height |

Layout primitives stay strictly typed: `FieldGrid` aligns rows in 12 columns, `ActionBar` orders primary, secondary, and danger actions with matching sizes, `PropertyList` aligns key-value columns with tabular numerals, and `MetricGrid` enforces symmetric desktop columns with no orphan trailing tile. Spacing follows a 4px and 8px scale, and every text input, select, date picker, money input, and standard button keeps a uniform 36px height, with 32px for compact table and inline actions.

## Standards

### Organize Pages by User Job

**ID:** `litespec/core/ui-ia#organize-pages-by-user-job`

**Strength:** required

**Applies to:** artifact: route, record:entry-point, record:screen

**Statement:** A consumer MUST organize its routes and navigation by user job, cadence, and focus instead of deriving page, tab, or navigation structure from backend module folders.

**Rationale:** Backend modules bound writes, while pages serve people. A navigation tree copied from module folders forces a person to learn the code layout to find a job, and two jobs with different cadences land on one page. Organizing by job, cadence, and focus puts daily work, periodic work, and settings where each is looked for. Backend-mirrored folders keep their one role: API clients and data-fetching adapters.

**Example:** Claimwick groups daily expense work apart from periodic close and reports, and keeps its workspace settings under one settings section instead of beside the expense list.

**Reviewed for:** Does the navigation follow jobs, cadence, and focus, with backend-mirrored folders holding only clients and fetching adapters? Check the sitemap beside the module folders.

### Declare Navigation on Each Browser Entry Point

**ID:** `litespec/core/ui-ia#declare-navigation-on-each-browser-entry-point`

**Strength:** required

**Applies to:** artifact: record:entry-point

**Statement:** Each browser entry point MUST declare its `navigation` groups for daily work, periodic work, and settings, with `sections` for multi-facet areas carrying their ordered sub-nav items.

**Rationale:** Navigation declared on the entry point is one reviewed list instead of one invented sidebar per route. Named groups keep daily and periodic work apart, and `sections` give multi-facet areas such as settings one ordered sub-nav. Without the declaration, each route adds its own links and the product grows two navigations.

**Example:** The Claimwick employee app declares daily work (`Home`, `Expenses`, `Approvals`), periodic work (`Reports`, `Close`), and settings (`Settings`, opening `/settings/business` with its ordered sub-nav).

**Checked by:** `litespec model check` reports `record-invalid` for a browser entry point without `navigation`, and for a primary sidebar with more than 3 groups or more than 10 items.

### Declare Each Screen Job and Archetype

**ID:** `litespec/core/ui-ia#declare-each-screen-job-and-archetype`

**Strength:** required

**Applies to:** artifact: record:screen

**Statement:** Each screen MUST declare `route`, its optional sub-nav parent in `section`, its `archetype` from the six locked archetypes, its `job` as one plain sentence naming the single user job, its `offers`, its `primaryAction`, its `states`, and its `leadsTo`.

**Rationale:** A screen without a stated job and archetype is reviewed by feel: nobody can say whether it does one thing or which structure it should follow. Declaring the job in one sentence forces the one-job decision up front, and declaring the archetype fixes which slots and guardrails the route review checks. `primaryAction`, `states`, and `leadsTo` state what the person starts, what the page looks like while busy or empty, and where each path continues.

**Example:** Claimwick's expense list declares `route: /expenses`, `archetype: ListPage`, `job: "Find one expense among many and open it."`, `offers` with the list query and the new-expense command, `primaryAction: new-expense`, `states` for empty and loading, and `leadsTo` with the expense detail route.

**Checked by:** `litespec model check` reports `record-invalid` for a screen without `route`, `archetype`, `job`, `offers`, `primaryAction`, `states`, or `leadsTo`, and for an `archetype` outside the six locked archetypes.

### Limit Each Non-Hub Screen to Four Offered Commands

**ID:** `litespec/core/ui-ia#cap-each-non-hub-screen-at-four-offered-commands`

**Strength:** required

**Applies to:** artifact: record:screen

**Statement:** A screen MUST offer at most 4 commands in `offers`, not counting read-only lookup queries explicitly typed as queries.

**Rationale:** Each offered command is a job the page claims to serve. A fifth offer is a second job sharing the page, and the page then needs the decomposition table again. Counting every command except explicitly typed read-only lookups keeps the cap honest: an untyped read is an offer until proven otherwise.

**Example:** Claimwick's expense detail offers open, approve, request changes, and reimburse: 4 offers. A fifth offer, such as editing workspace settings, moves to its own settings sub-route.

**Reviewed for:** Does every non-hub screen offer at most 4 commands, with only explicitly typed read-only lookups excluded? Check each screen record beside its route.

### Limit Visual Sections to Three per Screen

**ID:** `litespec/core/ui-ia#cap-visual-sections-at-three-per-screen`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route MUST render at most 3 top-level visual sections across cards, panels, and drawers combined with no nested surface.

**Rationale:** Each added surface splits attention: a fourth card or a drawer inside a card asks the person to track two hierarchies at once. Three flat sections fit one job, and a job that needs more is two jobs. A nested surface hides the hierarchy instead of simplifying it, so the ban is absolute.

**Example:** Claimwick's expense detail renders its header, its main sections, and one history aside: 3 sections. A nested card inside the history panel becomes a labeled subsection divider or moves to a tab.

**Reviewed for:** Does every route render at most 3 top-level sections with no nested surfaces? Check each route at desktop and mobile widths.

### Keep Route Files Small

**ID:** `litespec/core/ui-ia#keep-route-files-small`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route file MUST stay under 350 lines with an explicit waiver decision covering any file above that limit.

**Rationale:** A route file of 500 to 1,500 lines is not a long page but a missing decomposition: several jobs, modes, or facets stacked in one file. The line count is the cheapest proxy for that defect. Keeping each route small forces modes into isolated sub-components and facets into sub-routes, where the archetype guardrails check them. Teams treat 250 lines as the practical target, with the 350-line limit as the defect boundary.

**Example:** Claimwick's flagged-expense review stays under 250 lines per mode view by isolating `Match`, `Split`, and `Reject` into their own components behind route-backed mode tabs.

**Reviewed for:** Does every route file stay under 350 lines, or does a waiver decision record the reason? Check the route file sizes beside the waiver records.

### Keep One Editing Mechanism per Section

**ID:** `litespec/core/ui-ia#keep-one-editing-mechanism-per-section`

**Strength:** required

**Applies to:** artifact: route

**Statement:** Peer facets of one entity or settings group MUST use the same editing mechanism: all inline section forms, all dedicated sub-routes, all modal dialogs, or all drawers.

**Rationale:** Mixed mechanisms teach a different gesture per sibling: one facet saves inline while its neighbor opens a dialog and a third navigates away. The person learns three interactions for one kind of change. One mechanism per section keeps every sibling predictable once the first is learned.

**Example:** Every Claimwick settings sub-route saves its own inline section form. No sibling mixes a dialog edit beside an inline edit.

**Reviewed for:** Do peer facets of one entity or settings group share one editing mechanism? Check the sibling routes beside each other.

### Map Every Screen to Its Job Table Structure

**ID:** `litespec/core/ui-ia#decompose-every-screen-by-the-job-table`

**Strength:** required

**Applies to:** artifact: record:screen

**Statement:** Each screen MUST match the Required Structure its User Job Shape maps to in the decomposition table.

**Rationale:** Without a fixed mapping, each route author re-derives the structure from taste, and the same job shape gets a dialog in one place and a sub-route in another. The table fixes each shape to one structure, so review checks the row instead of re-arguing the shape. A shape the table does not cover is a gap in this page, not a free choice.

**Example:** Claimwick's workspace configuration follows the first table row: one `SettingsPage` with left sub-navigation and one focused sub-route per facet. Its expense creation follows the last row: one `FormPage` capped at `max-w-2xl`.

**Reviewed for:** Does every screen use the structure its job shape maps to? Check each screen against the decomposition table.

### Compose Each Screen from Its Owners Queries

**ID:** `litespec/core/ui-ia#compose-each-screen-from-its-owners-queries`

**Strength:** required

**Applies to:** artifact: record:screen

**Statement:** A screen that shows data owned by several modules MUST join each owner's query, and a query that only shows another module's data belongs to that module.

**Rationale:** A screen that copies another module's data into its own query duplicates ownership: two queries answer the same question and drift apart. Joining each owner's query keeps one owner per fact while the screen shows them together. A query that only reshapes another module's data is filed in the wrong module, so the rule files it with its owner.

**Example:** Claimwick's expense detail joins the spending query for the expense with the payouts query for its reimbursements instead of copying payout fields into a spending query.

**Reviewed for:** Does every screen join each owner's query, with no query that only shows another module's data? Check each screen beside the owning modules.

### Compose Each Route from One Locked Archetype

**ID:** `litespec/core/ui-ia#compose-each-route-from-one-locked-archetype`

**Strength:** required

**Applies to:** artifact: route

**Statement:** A route MUST compose exactly one of the six locked archetypes using only its declared slots.

**Rationale:** Unconstrained composition drifts: a list page grows a second form, a record page stacks drawers, a settings page buries a table. One archetype per route fixes the slots up front, and filling only declared slots keeps each job in the shape the product approved. The two minimal utility frames, `AuthPage` and `OutcomePage`, cover sign-in and plain confirmations and carry no other job.

**Example:** Claimwick's expense list composes `ListPage` and fills `header`, `filterBar`, `table`, and `pagination`. A secondary form below the table is refused in review.

**Reviewed for:** Does every route compose exactly one archetype and fill only its declared slots? Check each route against its archetype row.

### Use Only Declared Slots with Matching Guardrails

**ID:** `litespec/core/ui-ia#fill-only-declared-slots-with-matching-guardrails`

**Strength:** required

**Applies to:** artifact: route

**Statement:** Each filled slot MUST honor its archetype guardrails: at most 1 work queue card plus 1 symmetric KPI strip on a home page, a single table surface with no stacked forms on a list page, a 2-column split with at most 1 active form on a work page, at most 2 main sections with URL-backed tabs for peer facets on a record page, one facet per sub-route on a settings page, and capped form width with uniform 36px control height on a form page.

**Rationale:** Slots without guardrails fill up: a second queue card appears on the home page, a form lands below the table, a record page stacks a third section. Each guardrail caps the failure its archetype invites, so the common drift of that shape is refused by rule instead of by taste.

**Example:** Claimwick's home page renders 1 waiting-expenses queue card and 1 symmetric 4-up KPI strip. A proposed second queue becomes a second route or waits for the sitemap review.

**Reviewed for:** Does every filled slot honor its archetype guardrails? Check each route against its row in the archetype table.

## Conventions

### Keep Backend-Mirrored Folders to Clients and Fetching Adapters

**ID:** `litespec/core/ui-ia#keep-backend-mirrored-folders-to-clients-and-fetching-adapters`

**Strength:** default

**Applies to:** artifact: any

**Default:** Backend-mirrored folders hold only API clients and data-fetching adapters (`lib/modules/` or `packages/api/`).

**Replacement:** A project records a frontend layout convention naming where its clients and adapters live instead.

**Rationale:** Mirrored folders tempt authors to mirror backend structure in pages too. Restricting them to clients and adapters keeps the fetching code findable by module while routing decisions stay in the sitemap.

**Reviewed for:** Do backend-mirrored folders hold only clients and fetching adapters, or does a layout convention name another home? Check the mirrored folders beside the sitemap.

## Reference Example

This informative example demonstrates `litespec/core/ui-ia#decompose-every-screen-by-the-job-table` and `litespec/core/ui-ia#compose-each-route-from-one-locked-archetype`.

Claimwick serves employees who submit expenses and managers who approve them. Its sitemap holds a `HomePage` at `/` showing the waiting approvals queue and one symmetric KPI strip, a `ListPage` at `/expenses` for finding one expense among many, a `RecordPage` at `/expenses/{id}` with URL-backed tabs for Overview, Payments and Reimbursements, and History, a `FormPage` at `/expenses/new` capped at `max-w-2xl` for submitting one expense with its receipt, and a `SettingsPage` at `/settings/*` with left sub-navigation and one focused sub-route per workspace facet. Each route composes one archetype, offers at most 4 commands, and renders at most 3 flat sections.
