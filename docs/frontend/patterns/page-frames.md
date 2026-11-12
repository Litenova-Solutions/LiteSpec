# Page Frame Patterns

**Stack:** any
**Concern:** frontend-ui

## Intent

This page holds the layout pattern for each kind of page: home, list, queue, record, form, report, settings, editor, outcome, operations, schedule, setup, and error. A team building a new screen starts from these patterns, an owner picks an option from the drawings, and the team records the choice in its pattern catalog, the file holding the project's interface decisions. One layout per kind keeps every screen predictable, so a reader knows where the heading, actions, and content sit before reading.

## Concepts

### How to Read a Pattern

Each pattern answers one layout question, such as how a list page arranges its search, filters, table, and paging. It states the question, the default option a project builds, how the default behaves on a narrow screen, and why the default won. Each option then shows one wireframe drawing with its own rationale, cost, and optional reference. A project records its choice in its pattern catalog and builds the chosen option with its own components. The [UI patterns](../patterns.md) page states the catalog contract and the wireframe language.

### The Running Example

The drawings on this page use the Claimwick expense product for Harbor and Pine: expense EXP-1042 for Ava, the Rotterdam client visit, and the Amsterdam sales cost centre. Each kind of page repeats the same frame, so a reader meets one heading and at most one primary action on every screen. The Standards section states the rule every option keeps: each page shows one top-level heading. The [record detail](record-detail.md#show-one-primary-action-in-a-record-header) page states the primary-action rule the frames follow.

## Standards

### Name the Page in One Heading

**ID:** `litespec/frontend/patterns/page-frames#name-the-page-in-one-heading`

**Strength:** required

**Statement:** Each page MUST show one top-level heading that names the record, list, or task it holds.

**Rationale:** One heading names the open record, list, or task, so a reader who arrived from a link or a search knows where they landed. Without it, two headings compete for that job, and the reader guesses which line to trust while assistive tech announces the wrong one.

**Reviewed for:** Which built screen splits its title across headings or omits it? Open every built page and read its top-level heading.

## Patterns

### Home Page

**ID:** `litespec/frontend/patterns/page-frames#home-page`

**Strength:** default

**Question:** The first page after sign-in tells the reader what needs doing now, most urgent first, before any figure. A home page typically holds a greeting, stat tiles, a setup section, a needs-attention list, and a coming-up list. The disagreement is how much of that sits above the fold, and whether the tiles lead or the work leads.

**Default:** `work-first-tiles-second`

**Compact:** The recommended option reflows: the needs-attention and coming-up lists stack in one column above the figures, which stack one per row.

**Rationale:** Work first, tiles second stays the pick. The manager reads the queue and stops, and the figures wait below for the curious reader. The my-work split is the runner-up. Prefer it once assignment is a real command and two people share one queue. The tiles-first layout is the other runner-up for a reader who opens the page to check a number.

Never ship the tiles-first layout as the default. Three stat tiles push the queue below the fold and the page reads as a report instead of a task list.

**Reviewed for:** Which built start screen breaks from the wireframe its catalog entry chose? Open the built queues, tiles, and next actions beside the catalog entry recording the chosen option and its reason.

#### `work-first-tiles-second`: Work First, Tiles Second

```wireframe
page
  header
    heading "Good afternoon, Alex"
    text "Two things need you today. The Rotterdam client visit starts Saturday."
    action primary "New trip"
  region "Needs attention"
    status "2 to do" caution
    list 2
      item: text "Close out the Rotterdam trip" · text muted "The trip is complete. Nothing left to settle." · action "Close out"
  region "Coming up"
    list 2
      item: text "Rotterdam client visit" · text muted "Sat 14 Mar 2026, 08:00 to 12:00" · status "Active" positive · action "Open trip"
  row
    figure "Filed this month" "9,412"
    figure "Paid out by the provider" "EUR 57,506"
    figure "Active now" "2"
```

**Rationale:** The page opens on what needs doing and shows the figures underneath. A returning manager reads the queue and stops. A curious one scrolls for the numbers.

**Cost:** The tiles lose prominence, and a reader who came to check a number has to scroll. It costs one screen of vertical space.

#### `tiles-first-work-second`: Tiles First, Work Second

```wireframe
page
  header
    heading "Good afternoon, Alex"
    text "What is waiting for you, and the coming trips."
    action primary "New trip"
  row
    figure "Filed" "9,412"
    figure "Coming up" "2"
    figure "Paying out now" "0"
  region "Finish your setup"
    list 1
      item: text "Connect a payment account" · text muted "Money cannot be taken until the provider is connected." · action "Connect"
  region "Needs attention"
    status "1 thing to do" caution
    list 1
      item: text "Close out the Rotterdam trip" · text muted "Nothing left to settle." · action "Close out"
  region "Coming up"
    list 2
      item: text "Rotterdam client visit" · text muted "Sat 14 Mar 2026, 08:00 to 12:00" · status "Active" positive · action "Open trip"
```

**Rationale:** Figures give the reader a sense of scale in the first second, and the queue reads as a drill-down from them.

**Cost:** Three large tiles push the needs-attention list below the fold, so the page reads as a report rather than a task list.

#### `my-work-first-shared-queue-second`: My Work First, Shared Queue Second

```wireframe
page
  header
    heading "Good afternoon, Alex"
    text "Yours first, then what belongs to everybody."
    action primary "New trip"
  region "Assigned to you"
    status "2 yours" caution
    list 2
      item: text "Decide the reversal on expense EXP-1042" · text muted "Ava paid EUR 125.00." · action "Decide"
  region "The shared queue"
    text muted "Work nobody owns yet, and work somebody else owns."
    list 2
      item: text "Close out the Rotterdam trip" · text muted "The trip is complete." · status "Unassigned" neutral · action "Take it"
  row
    figure "Filed this month" "9,412"
    figure "Paid out by the provider" "EUR 57,506"
    figure "Active now" "2"
```

**Rationale:** The triage shape. What is assigned to Alex sits above what belongs to Harbor and Pine as a whole, so two people never decide the same reversal twice. The shared queue stays visible, which keeps one morning from hiding the shared state.

**Cost:** Assignment must be a real command with a real owner, or every row reads Alex and the split is decoration. It costs the tiles their place on short screens.

**Reference:** Linear

### List Page

**ID:** `litespec/frontend/patterns/page-frames#list-page`

**Strength:** default

**Question:** Find one record among many. The frame is fixed: header, toolbar, one table, and pagination. The decision is which controls earn their place, because a search box on its own is not enough across 268 rows.

**Default:** `search-and-a-state-filter-only`

**Compact:** The recommended option reflows: the search box and state filter stack above the table, and the table scrolls sideways inside its own region.

**Rationale:** Search plus one state filter stays the pick. Two controls answer the two questions readers ask, and a bare list needs only the filter added. The row of views is the runner-up for the states a reader returns to. The dense filter bar is the other runner-up past a few hundred rows across many spenders, where the query is restated in words.

A list page holds one table, and a second table is a second route wearing a disguise. Views can replace the state filter, but they never replace the search box.

**Reviewed for:** Which built collection screen breaks from the wireframe its catalog entry chose? Open the built searches, views, and filter bars beside the catalog entry recording the chosen option and its reason.

#### `search-and-a-state-filter-only`: Search and a State Filter Only

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every expense across your spenders, newest first."
  toolbar
    field "Expense number, name or email" search
    field "State" select = "Any state"
    action "Export"
  region "Expenses"
    table 6 "Expense" "Spender" "Filed" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · text muted "8 Oct 2026, 09:32" · status "Paid" positive · text "EUR 125.00"
    text muted "1 to 25 of 268"
```

**Rationale:** Two controls for the two questions people ask: who is this, and what state is it in. Everything else is a view. This is the smallest honest addition to a bare list.

**Cost:** A reader with 268 expenses from 40 spenders still cannot narrow to one spender. It stays acceptable while the search box already searches the spender name.

#### `views-instead-of-filters`: Views Instead of Filters

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every expense across your spenders, newest first."
  toolbar
    field "Expense number, name or email" search
  tabs "All 268" "Paid 214" "Awaiting 31" "Reversed 18" "Disputed 5"
    region "Expenses"
      table 6 "Expense" "Spender" "Filed" "State" "Paid"
        item: text "EXP-1042" · text "Ava" · text muted "8 Oct 2026, 09:32" · status "Paid" positive · text "EUR 125.00"
      text muted "1 to 25 of 268"
```

**Rationale:** A segmented control across the top reads All, Paid, Awaiting, Reversed, Disputed. One control replaces five, and the current choice stays visible rather than hiding in a list.

**Cost:** Five segments plus a search box crowd one toolbar, and segments have no room for a count per state. The reader cannot see where the work is before choosing.

#### `dense-filter-bar-with-a-result-count`: Dense Filter Bar with a Result Count

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every expense across your spenders, newest first."
  toolbar
    field "Filter by state, spender or trip" search = "state:paid"
    action "Export"
  region "Expenses"
    text muted "214 of 268 expenses"
    action quiet "Save this view"
    table 6 "Expense" "Spender" "Filed" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · text muted "8 Oct 2026, 09:32" · status "Paid" positive · text "EUR 125.00"
    text muted "1 to 25 of 214"
```

**Rationale:** One filter bar states the query in words, a count says what matched, and a dense table sits under it. The reader sees what narrowed the list without opening anything.

**Cost:** A query syntax has to be learned, and typing a query is slower than pressing a segment for the two daily states. It fits lists past a few hundred rows and adds noise below that size.

**Reference:** GitHub

### Queue

**ID:** `litespec/frontend/patterns/page-frames#queue`

**Strength:** default

**Question:** Process items in batches. The queue is the one list page where selection is the interface. The whole page has to answer what the checkbox selects, and what happens to a thousand of them.

**Default:** `select-all-on-the-page-banner-for-the-rest`

**Compact:** The recommended option reflows: the banner and the sticky footer keep their place, and the table scrolls sideways inside its own region.

**Rationale:** Select all on the page, then the rest from a banner, stays the pick. It is the clearest answer to the select-all problem, and money deserves the second click. The play mode is the runner-up. Prefer it when each decision needs different information and batch work is rare. A checkbox on every row is the other runner-up for a queue whose bulk job is small and capped.

Keep the search box and the filter in place while rows are selected. A bulk bar that replaces the toolbar makes the layout jump mid-decision and hides the controls the reader wants to refine it.

**Reviewed for:** Which built batch-selection screen breaks from the wireframe its catalog entry chose? Open the built select-all banners, play modes, and per-row checkboxes beside the catalog entry recording the chosen option and its reason.

#### `select-all-on-the-page-banner-for-the-rest`: Select All on the Page, Banner for the Rest

```wireframe
page
  breadcrumb "Home" "Expenses" "Reversals"
  header
    heading "Reversals"
    text "31 expenses waiting for a decision."
  toolbar
    field "Search by expense or spender" search
    field "Age" select = "Any age"
    action "Columns"
  region "Reversals"
    message "All 25 reversals on this page are selected. 6 more are on the next page." info
    action "Select all 31"
    action quiet "Clear"
    table 5 "Select" "Expense" "Spender" "Asked" "Amount" "Window"
      item: field "Select EXP-1042" checkbox · text "EXP-1042" · text "Ava" · text muted "6 days ago" · text "EUR 45.00" · status "174 days left" info
  footer sticky
    text "25 of 31 selected, EUR 1,155.00 in total"
    action "Offer credit"
    action "Partial reversal"
    action primary "Reversal in full"
```

**Rationale:** The header checkbox selects the 25 rows on screen. A banner then says all 25 on this page are selected. It offers all 31, which makes the larger set an explicit second act rather than an invisible jump.

**Cost:** Two clicks to act on everything, and the banner is one more thing on the page. It gives the clearest available answer to the select-all problem.

**Reference:** Gmail

#### `a-checkbox-on-every-row-no-header-checkbox`: A Checkbox on Every Row, No Header Checkbox

```wireframe
page
  breadcrumb "Home" "Expenses" "Reversals"
  header
    heading "Reversals"
    text "31 expenses waiting for a decision. Choose up to 20 at a time."
  region "Reversals"
    table 4 "Select" "Expense" "Spender" "Asked" "Amount" "Reversal"
      item: field "Select EXP-1042" checkbox · text "EXP-1042" · text "Ava" · text muted "6 days ago" · text "EUR 45.00" · action "Reversal"
  footer sticky
    action primary "Reversal 1 expense"
```

**Rationale:** Selection is per expense and never global, which is honest for money. The reader acts on 20 at a time and the page never implies it can act on all of them.

**Cost:** The most common bulk job, reversing everything inside the window, becomes 31 clicks. A cap that protects money also removes the batch work the queue exists for.

#### `play-mode-one-decision-per-screen`: Play Mode, One Decision per Screen

```wireframe
page
  breadcrumb "Home" "Expenses" "Reversals"
  header
    heading "Reversals, 4 of 31"
    text "One decision at a time. Skip what needs another person."
  region "Expense EXP-1042"
    status "Open" info
    fact "Spender" "Ava, ava@harbor-pine.example"
    fact "Company" "Rotterdam client visit"
    fact "Asked" "6 days ago"
    fact "Amount" "EUR 45.00"
    fact "Reversal window" "174 days left"
    action primary "Reversal EUR 45.00"
    action "Offer credit"
    action quiet "Skip"
  region "Next"
    text muted "The two behind this one, soonest window first."
    list 2
      item: text "EXP-1041" · text muted "Tom Becker, EUR 45.00"
```

**Rationale:** The play mode. The queue becomes one record at a time with Decide and Skip beside it and the next one already named. Each reversal gets full attention, and the reader cannot half-read a row.

**Cost:** One at a time is slow for thirty identical reversals, and the batch these queues exist for disappears. Prefer it when each decision needs different information, never for uniform work.

**Reference:** Zendesk

### Record Page

**ID:** `litespec/frontend/patterns/page-frames#record-page`

**Strength:** default

**Question:** One record, and everything done to it. The page carries a summary and the record's facets, and it decides where history and editable properties sit. The decision is how to divide the page: tabs against stacked sections, and what earns the aside.

**Default:** `summary-sections-aside-for-history`

**Compact:** The recommended option reflows: the sections stack in one column and the history aside follows them, with the header actions under the title.

**Rationale:** Summary with sections and history in the aside stays the pick. What defines the record runs down the main column while history supports it from the side. The properties rail is the runner-up. Prefer it when the aside must carry editable values rather than history. The tabs layout is the other runner-up for a record with more facets than three sections allow.

Never put the record's facts in the header. A dense header pushes every command below the fold on a phone and fights the trail for space.

**Reviewed for:** Which built single-record screen breaks from the wireframe its catalog entry chose? Open the built summaries, tabs, and asides beside the catalog entry recording the chosen option and its reason.

#### `summary-sections-aside-for-history`: Summary, Sections, Aside for History

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Filed 8 Oct 2026, 09:32 for the Rotterdam client visit."
    action "Resend the confirmation"
    action "Reversal"
    action primary "Message the spender"
  split 2:1
    region "The expense"
      fact "Spender" "Ava"
      fact "Email" "ava@harbor-pine.example"
      fact "Two fares" "EUR 90.00"
      fact "Paid by the spender" "EUR 125.00"
    aside
      region "What happened"
        timeline 3
          item: text "Paid" · text muted "8 Oct 2026, 09:34" · text muted "Card"
```

**Rationale:** What defines the record runs down the main column in at most three sections. History, which supports but does not define, goes in a narrower aside. This is the proven shape: a summary at the top, at most three sections, and history in the aside.

**Cost:** Below roughly 1000 pixels the aside stacks under the sections, so the history lands after the actions. The record is acceptable either way.

**Reference:** Shopify

#### `four-tabs-over-the-same-sections`: Four Tabs over the Same Sections

```wireframe
page
  breadcrumb "Home" "Trips" "Rotterdam client visit"
  header
    heading "Rotterdam client visit"
    status "Active" positive
    text "32 of 40 expenses filed, in Amsterdam sales."
    action quiet "Pause payouts"
    action destructive "Cancel the trip"
    action primary "Add an expense"
  tabs "Expenses" "Budget" "Reversals" "Details"
    region "Details"
      fact "Dates" "Sat 14 Mar 2026, 08:00 to 12:00"
      fact "Cost centre" "Amsterdam sales"
      fact "Expenses" "32 of 40"
      action quiet "Copy"
      field "Name" text = "Rotterdam client visit"
      action primary "Save"
```

**Rationale:** When a record has more facets than three sections allow, tabs divide it. The active tab is in the URL, so a link carries it.

**Cost:** The reader cannot see two facets at once, and the tab count grows with every new thing the back office adds. Guidance caps tabs at about six for this reason.

**Reference:** GOV.UK, Carbon

#### `editable-properties-in-the-rail`: Editable Properties in the Rail

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Filed 8 Oct 2026, 09:32 for the Rotterdam client visit."
    action "Resend the confirmation"
    action "Reversal"
  split 2:1
    region "The spender"
      fact "Name" "Ava"
      fact "Email" "ava@harbor-pine.example"
      fact "Country" "Netherlands"
    aside
      region "Properties"
        field "Owner" select = "Priya Shah"
        field "Follow-up" select = "Calling back"
        field "Spender note" text = "Called 8 Oct, no answer"
        action primary "Save properties"
```

**Rationale:** The issue shape. The record reads down the main column while every editable property sits in the right rail, changed where it is shown. No Edit mode and no second page for one value.

**Cost:** Every property needs its own save and its own failure state, so five quick edits are five round trips. It fails where a change must be reviewed whole before anything is written.

**Reference:** Linear

### Form Page

**ID:** `litespec/frontend/patterns/page-frames#form-page`

**Strength:** default

**Question:** Take one command's input. A new-record page can put every field in one section, stack several labeled blocks, or ask one question per screen. The decision is how much structure one command earns.

**Default:** `one-section-one-column-of-fields-cancel-beside-save`

**Compact:** The recommended option reflows: the single column of fields keeps its order, and Cancel sits beside Save at the end.

**Rationale:** One section, one column, Cancel beside Save stays the pick. A single command reads as a list of questions with one place to submit and one to leave. The stacked blocks are the runner-up when the form carries several labeled groups. The one-question-per-screen flow is the other runner-up when each answer changes the next question and nothing may be skipped.

Three steps for work that fits one page is one step too many.

**Reviewed for:** Which built input screen breaks from the wireframe its catalog entry chose? Open the built sections, labeled blocks, and one-question flows beside the catalog entry recording the chosen option and its reason.

#### `one-section-one-column-of-fields-cancel-beside-save`: One Section, One Column of Fields, Cancel Beside Save

```wireframe
page
  breadcrumb "Home" "Expenses" "New expense"
  header
    heading "New expense"
    text "Start with a name. Everything else can wait."
  region "New expense"
    field "Name" text
    field "Description" textarea
    field "Category" select = "Choose a category"
    field "Amount" text = "45.00"
    field "Expense code" text = "EXP-1042"
    action "Cancel"
    action primary "Create expense"
```

**Rationale:** One command, one column, one place to submit, and one to leave. The section is the only box, and the field labels sit above their controls so the column reads as a list of questions.

**Cost:** A long form makes a long page, and every field sits at the same weight, so a reader has no groups to skip.

#### `stacked-blocks-with-the-title-above`: Stacked Blocks with the Title Above

```wireframe
page
  breadcrumb "Home" "Expenses" "New expense"
  header
    heading "New expense"
    text "Start with a name."
  region "The expense"
    text muted "The name shows on the expense list."
    field "Name" text
  region "Amounts"
    text muted "What the payout sends, and the currency it sends."
    field "Amount" text = "45.00"
    field "Currency" select = "EUR"
  region "Budget"
    text muted "Which cost centre holds it."
    field "Cost centre" select = "Amsterdam sales"
  region "Submit"
    action "Cancel"
    action primary "Create expense"
```

**Rationale:** Several labeled blocks, each with its title above the fields. The page stays scannable when one command carries groups that mean different things: the name, the amounts, and the budget.

**Cost:** The page grows longer, and the block titles add weight above fields that would read as one list.

#### `one-question-per-screen`: One Question per Screen

```wireframe
page
  breadcrumb "Home" "Expenses" "New expense"
  header
    heading "What is the expense called"
    text "Step 1 of 5."
  region "Name"
    field "Name" text
    text muted "Spenders see this on their expense list and in the payout."
    action primary "Continue"
    action quiet "Back"
```

**Rationale:** The one-thing-per-page shape. Each answer gets the whole screen, validates before the next opens, and Back returns to the question before it. Nothing half-filled is lost to a section the reader skipped.

**Cost:** Five screens for a form that fits one page feels slow to a returning reader. The answers cannot be reviewed together until the final check. Reserve it for flows where each answer changes the next question.

**Reference:** GOV.UK

### Report Page

**ID:** `litespec/frontend/patterns/page-frames#report-page`

**Strength:** default

**Question:** Read figures over one period. The page names the period, lets the reader step to another, and keeps the statement accountable. The decision is how the period changes and what leads: the statement, a trend, or a typed range.

**Default:** `chevron-pair-with-the-period-as-the-heading`

**Compact:** The recommended option reflows: the period heading keeps its chevrons with the month select and download beside it, and the statement regions stack in one column.

**Rationale:** The chevron pair with the period as the heading stays the pick. A reader cannot mistake a chevron for a tab, and the list still allows a jump across months. The cards with sparklines are the runner-up. Prefer them mid-month when the question is trend rather than settlement. The typed range is the other runner-up for a report over an arbitrary span.

Label each month control with its direction. A pair of unlabeled buttons leaves the reader guessing which one goes back.

**Reviewed for:** Which built figures screen breaks from the wireframe its catalog entry chose? Open the built period pickers, range controls, and trend cards beside the catalog entry recording the chosen option and its reason.

#### `chevron-pair-with-the-period-as-the-heading`: Chevron Pair with the Period as the Heading

```wireframe
page
  breadcrumb "Home" "Reports" "Statements"
  header
    heading "September 2026"
    text "Every euro of the month, by expense kind."
    action quiet "Previous month"
    action quiet "Next month"
    field "Month" select = "September 2026"
    action "Download"
  region "Paid out"
    fact "Expenses paid" "EUR 54,180.00"
    fact "Booking fees" "EUR 6,020.00"
    fact "Total paid out" "EUR 60,200.00"
    fact "Provider fee" "EUR -2,693.80"
  region "Net for Harbor and Pine, September 2026"
    figure "EUR 57,506.20"
  region "By cost centre"
    table 2 "Cost centre" "Net"
      item: text "Amsterdam sales" · text "EUR 49,686.20"
```

**Rationale:** The period is the page heading, with an unambiguous chevron on each side to step through consecutive months and a select to jump across them. A reader cannot mistake a chevron for a tab.

**Cost:** Walking months one at a time is slow when comparing a quarter, which is why the month select stays.

#### `dropdown-with-presets-beside-a-typeable-range`: Dropdown with Presets Beside a Typeable Range

```wireframe
page
  breadcrumb "Home" "Reports" "Payouts"
  header
    heading "Payouts"
    text "Every euro that moved, by the day it moved."
  region "Period"
    field "Range" select = "Custom"
    row
      field "From" date = "09/09/2026"
      field "To" date = "08/10/2026"
      action primary "Apply"
  region "Daily"
    image "Payouts per day, bar chart for 9 Sep to 8 Oct"
    text muted "Peak 26 Sep, EUR 12,400.00"
```

**Rationale:** A range control offering Last 30 days, This month, Last month, Custom, and a pair of typeable date fields. Nothing is hidden behind the calendar.

**Cost:** Presets plus a range are two mechanisms on one control. The used preset must stay visible afterwards, or the reader loses track of the period.

#### `cards-with-sparklines-above-the-statement`: Cards with Sparklines Above the Statement

```wireframe
page
  breadcrumb "Home" "Reports" "Payouts"
  header
    heading "Payouts, September 2026"
    text "The trend first, then every euro by expense kind."
  grid 3
    card "Expenses paid"
      figure "EUR 54,180"
      text muted "Up 12 percent on August"
      image "Expenses paid rising through September, sparkline"
  region "Statement for September 2026"
    fact "Expenses paid" "EUR 54,180.00"
    fact "Booking fees" "EUR 6,020.00"
    fact "Total paid out" "EUR 60,200.00"
    fact "Provider fee" "EUR -2,693.80"
    fact "Net for Harbor and Pine" "EUR 57,506.20"
```

**Rationale:** The analytics shape. Three cards with sparklines name the trend before the statement names the total, so a dip is noticed before it is explained. The statement underneath keeps every euro accountable.

**Cost:** Sparklines invite trend questions the monthly statement cannot answer, and three cards push the accountable total down. Prefer it mid-month. Prefer the plain statement at settlement.

**Reference:** Shopify

### Settings Home

**ID:** `litespec/frontend/patterns/page-frames#settings-home`

**Strength:** default

**Question:** Find one setting among many areas. A settings home can carry a whole tree: thirty routes under `/settings`, and the home page has to hold all of them.

**Default:** `grouped-links-one-line-each`

**Compact:** The recommended option reflows: the grouped links stack in one column, each link keeping its line of explanation.

**Rationale:** Grouped links stay the pick. Each area names what it holds, the page never grows a form, and thirty routes fit in a handful of sections. The left navigation is the runner-up. Prefer it once areas gain enough depth to deserve their own tree. The searchable list is the other runner-up for a reader who knows the name of the setting.

Group by the thing, never by how often it changes: readers look for a thing, not for how often they change it.

**Reviewed for:** Which built setup index breaks from the wireframe its catalog entry chose? Open the built grouped links, settings trees, and search indexes beside the catalog entry recording the chosen option and its reason.

#### `grouped-links-one-line-each`: Grouped Links, One Line Each

```wireframe
page
  breadcrumb "Home" "Settings"
  header
    heading "Settings"
    text "What Harbor and Pine runs on, and who can change it."
  region "The company"
    list 4
      item: text "General" · text muted "Name, address, contact details and time zone." · action "Open"
  region "Filing"
    list 3
      item: text "Expense form templates" · text muted "What a spender fills in when they file an expense." · action "Open"
  region "People and access"
    list 5
      item: text "Team" · text muted "5 people, 2 invitations outstanding." · action "Open"
```

**Rationale:** Each area is a link with a line saying what it holds, grouped into a handful of sections. The reader knows the area before they click and the page never grows a form.

**Cost:** Nothing states which areas are incomplete, so a new starter has to open each one. The Harbor and Pine home page already covers this with its setup section.

#### `left-navigation-with-the-area-on-the-right`: Left Navigation with the Area on the Right

```wireframe
page
  breadcrumb "Home" "Settings" "General"
  header
    heading "General"
    text "Name, address, contact details and the time zone the whole back office works in."
    action primary "Save"
  split 1:3
    nav side
      link current "General"
      link "Locations"
      link "Cost centres"
      link "Payments"
    region "General"
      field "Company name" = "Harbor and Pine"
      field "Contact email" = "hello@harbor-pine.example"
      field "Time zone" select = "Europe/Amsterdam"
```

**Rationale:** The settings-tree shape. The areas live in a left navigation and the chosen area opens beside it. Thirty routes become one tree the reader scans without leaving the page. The current area is always marked.

**Cost:** An area's incompleteness stays invisible until it is opened. A phone has no room for the tree. The navigation must collapse into the grouped links of the pick.

**Reference:** Stripe

#### `a-list-with-a-search-box`: A List with a Search Box

```wireframe
page
  breadcrumb "Home" "Settings"
  header
    heading "Settings"
    text "Every setting Harbor and Pine holds. Search by what it is called."
  toolbar
    field "Search settings" search
    text muted "30 settings"
  region "All settings"
    list 10
      item: text "Cost centres" · text muted "Settings, Cost centres, Budgets and spenders." · action "Open"
```

**Rationale:** One flat list of every setting with a search box over it. Thirty routes become one searchable index, and the reader types what they are looking for.

**Cost:** The grouping is lost, so a reader who knows the area must hunt for it. The search has to know every setting name a reader might use.

### Editor Page

**ID:** `litespec/frontend/patterns/page-frames#editor-page`

**Strength:** default

**Question:** Edit what a spender sees, with a live preview beside the form. An expense form editor holds the look in a form and shows the result in a preview. The decision is where the preview sits and how much of the page it takes, because the reader has to judge a change without saving it.

**Default:** `form-left-live-preview-right-one-save`

**Compact:** The recommended option reflows: the form stays first and the live preview follows it in one column, with the one Save at the end of the form.

**Rationale:** Form left with a live preview right stays the pick. Every control writes straight into the preview, and nobody saves to find out what a change looks like. The floating panel is the runner-up. Prefer it when the visual dominates, such as a cover image judged at real size. The preview drawer is the other runner-up when the form needs the full page.

Keep the preview beside the form or over it. A preview above the form scrolls away with every edit, which defeats a live preview.

**Reviewed for:** Which built preview screen breaks from the wireframe its catalog entry chose? Open the built form-plus-preview splits, drawers, and floating panels beside the catalog entry recording the chosen option and its reason.

#### `form-left-live-preview-right-one-save`: Form Left, Live Preview Right, One Save

```wireframe
page
  breadcrumb "Home" "Settings" "Expense form"
  header
    heading "Expense form"
    text "The logo, cover, accent color, and typeface spenders see."
  tabs "Expense form" "Domains"
    region "Expense form"
      split 1:1
        region "The look"
          field "Drop a logo here or choose a file" file
          field "Logo description" text
          field "Typeface" select = "System"
          field "Accent color" = "#1F2937"
          action "Cancel"
          action primary "Save"
        card "Preview"
          image "Expense form preview with cover image and logo"
          heading small "EXP-1042"
          text "Hotel night for the Rotterdam client visit, ready to pay."
          action quiet "Submit expense"
          text muted "Harbor and Pine, Amsterdam office, Amsterdam"
```

**Rationale:** Every control writes straight into the preview, so the reader never saves to find out what a change looks like.

**Cost:** A form preview earns trust at phone width too, and this layout has no width toggle. Two thirds of a 1440 screen is a lot of preview for a few fields.

#### `preview-as-a-drawer-pulled-over-the-form`: Preview as a Drawer Pulled over the Form

```wireframe
page
  breadcrumb "Home" "Settings" "Expense form"
  header
    heading "Expense form"
    text "The logo, cover, accent color, and typeface spenders see."
    action "Preview"
  region "The look"
    field "Drop a logo here or choose a file" file
    field "Accent color" = "#1F2937"
    field "Typeface" select = "System"
    action "Cancel"
    action primary "Save"
  drawer right
    heading small "Preview"
    text muted "As a spender sees it."
    image "Expense form preview with cover image and logo"
    heading small "EXP-1042"
    text "Hotel night for the Rotterdam client visit."
    action quiet "Close"
```

**Rationale:** The form takes the whole page and the preview opens over it as a drawer. The reader can check the result at either width without losing their place.

**Cost:** The preview is out of sight by default. A drawer over a form is a layered context for something meant to reassure.

#### `floating-settings-panel-over-a-full-bleed-preview`: Floating Settings Panel over a Full-Bleed Preview

```wireframe
page
  breadcrumb "Home" "Settings" "Expense form"
  header
    heading "Expense form"
    text "The logo, cover, accent color, and typeface spenders see."
    action quiet "Hide panel"
  region "The look"
    field "Accent color" = "#1F2937"
    field "Typeface" select = "System"
    action "Cancel"
    action primary "Save"
  card "Preview"
    image "Cover image judged at real size with logo and Submit expense"
    heading small "EXP-1042"
    text "Hotel night for the Rotterdam client visit, ready to pay."
    action quiet "Submit expense"
    text muted "Harbor and Pine, Amsterdam office, Amsterdam"
```

**Rationale:** The full-bleed shape. The preview takes the whole page and the settings float over it as a panel. A cover image is judged at real size. Closing the panel is the same as stepping back from the work.

**Cost:** The panel covers the preview it edits, and dragging it aside is awkward on a laptop trackpad. Prefer it when the visual dominates, such as a cover. Prefer side-by-side for detailed fields.

**Reference:** Webflow

### Outcome Page

**ID:** `litespec/frontend/patterns/page-frames#outcome-page`

**Strength:** default

**Question:** Where a flow ends: an expense confirmed, a payment that failed, a link that expired. One state, one statement of what happened, one next step. Flows often end in a toast instead, so this frame states what an ending page holds.

**Default:** `one-statement-one-next-step`

**Compact:** The recommended option reflows: the statement, the facts, and the next step stack in one column in the same order.

**Rationale:** One statement with one next step stays the pick. A reader who came to learn whether it worked leaves in one glance. The refusal outcome is the runner-up, for the same frame with the fix in the primary place. The confirmation band is the other runner-up when the flow leaves a reference the reader must keep.

A moment is not a place. An outcome the reader may need to return to belongs on a page, not in a toast.

**Reviewed for:** Which built result screen breaks from the wireframe its catalog entry chose? Open the built statements, references, and refusal outcomes beside the catalog entry recording the chosen option and its reason.

#### `one-statement-one-next-step`: One Statement, One Next Step

```wireframe
page
  header
    status "Sent" positive
    heading "EUR 45.00 is on its way back to Ava"
    text "Reversals reach the bank in one to three working days. The expense EXP-1042 returns to the budget."
    action quiet "Back to expenses"
    action "Email the spender"
    action primary "Open expense EXP-1042"
  region "What happened"
    fact "Expense" "EXP-1042"
    fact "Spender" "Ava, ava@harbor-pine.example"
    fact "Amount" "EUR 45.00"
    fact "Reversed by" "Priya Shah, 8 Oct 2026 at 11:04"
    fact "Method" "Back to the account it came from"
```

**Rationale:** What happened in the heading, what it means in one line, and exactly one primary action. A reader who came to find out whether it worked leaves in one glance.

**Cost:** Saying nothing about what comes next makes the page a dead end unless the one action is right. Nothing else is offered, so a wrong guess is expensive.

#### `refusal-outcome-that-names-the-fix`: Refusal Outcome That Names the Fix

```wireframe
page
  header
    status "Refused" critical
    heading "The reversal window has closed"
    text "This payment method allows reversals only within 180 days of the payment. This one is 214 days old."
    action quiet "Back to expense EXP-1042"
    action primary "Offer credit instead"
  region "What you can do"
    list 2
      item: text "Offer the spender credit" · text muted "A credit against a future expense, paid for by Harbor and Pine." · action "Offer credit"
```

**Rationale:** The same frame carries the refusal tone. The primary action is the one that resolves it: not Back, but the refused command.

**Cost:** Every refusal needs a differently-worded fix, and one that does not have one has to fall back to explaining itself.

#### `confirmation-band-with-the-reference-inside`: Confirmation Band with the Reference Inside

```wireframe
page
  card "Reversal sent"
    text "Expense EXP-1042"
    heading small "EUR 45.00 is on its way back to Ava"
  region "What happened"
    fact "Amount" "EUR 45.00"
    fact "Spender" "Ava, ava@harbor-pine.example"
    fact "Reversed by" "Priya Shah, 8 Oct 2026 at 11:04"
    fact "Method" "Back to the account it came from"
  footer
    action quiet "Back to expenses"
    action primary "Open expense EXP-1042"
```

**Rationale:** The confirmation panel. A green band carries the outcome and the reference together, so the one thing to keep is the one thing that stands out. Everything below it is supporting detail.

**Cost:** The band works for success and only success. A refusal in the same shape would read as approval. Reserve it for flows that leave a reference behind. Never use it as a generic end screen.

**Reference:** GOV.UK

### Live Operations Page

**ID:** `litespec/frontend/patterns/page-frames#live-operations-page`

**Strength:** default

**Question:** Payout run day is happening: the cost centres are open, scanners are reading, and the finance lead needs the count at a glance. The decision is what earns the glance: the total, the per-cost-centre split, or only the exceptions.

**Default:** `the-count-the-cost-centres-the-exceptions`

**Compact:** The recommended option reflows: the count stays first, and the cost centre cards stack above the exceptions in one column.

**Rationale:** The count with cost centres and exceptions stays the pick. The finance lead reads one number first, then the per-cost-centre split, then only what needs a person. The exceptions-only view is the runner-up. Prefer it on the wall display once the day is steady and the count is already known. The full-screen count is the other runner-up for a display read from across the room.

A page that lists every payout is a log, not a live view. Keep the count in the header and the exceptions above the fold.

**Reviewed for:** Which built wall display breaks from the wireframe its catalog entry chose? Open the built counts, cost centres, and exception views beside the catalog entry recording the chosen option and its reason.

#### `the-count-the-cost-centres-the-exceptions`: The Count, the Cost Centres, the Exceptions

```wireframe
page
  breadcrumb "Home" "Trips" "Rotterdam client visit" "Payout run"
  header
    heading "Rotterdam trip payouts, live"
    status "Live" positive
    text "Sat 14 Mar 2026, 08:00 to 12:00, Amsterdam sales."
    action quiet "End payout run"
  region "Paid out"
    figure "554 of 840 expenses"
    text muted "66 percent paid out"
  grid 4
    card "Amsterdam and Rotterdam"
      figure "210"
      text muted "Expenses, Ade Okafor"
  region "Needs a person"
    list 2
      item: text "One expense scanned at two cost centres" · text muted "EXP-1042, read at 09:40 and 09:41." · status "Check it" caution · action "Open"
```

**Rationale:** One number first, then the per-cost-centre split, then only what needs a person. The finance lead reads the count in one glance and reaches the exceptions without scrolling past the day's log.

**Cost:** Three structures on one page, and the exceptions sit below the cost centres on a phone. That expense still reads correctly.

#### `exceptions-only-quiet-otherwise`: Exceptions Only, Quiet Otherwise

```wireframe
page
  breadcrumb "Home" "Trips" "Rotterdam client visit" "Payout run"
  header
    heading "Rotterdam trip payouts, live"
    status "Live" positive
    text "554 paid out of 840 scheduled. Nothing needs a person."
  region "The cost centres are clear"
    text "554 paid out across 4 cost centres. The next exception appears here."
  region "Earlier today"
    list 1
      item: text "Receipt scanner was offline" · text muted "10:10 to 10:40, 41 scans queued, all synced." · status "Resolved" positive
```

**Rationale:** The page promises silence: the count in the header, and below it only what needs a person. A finance lead who trusts the scanners reads one line and puts the phone away.

**Cost:** When nothing needs anybody the page is nearly empty, and an empty page on a wall display reads as broken. It needs the count to prove it is alive.

#### `full-screen-count-for-the-wall-display`: Full-Screen Count for the Wall Display

```wireframe
page
  header
    status "Live" positive
    heading "554"
    text "Expenses of 840 paid out."
    text muted "Rotterdam trip, Sat 14 Mar 2026, Amsterdam sales."
    status "1 needs a person" caution
    action primary "Open the exceptions"
```

**Rationale:** A wall display carries one number and the exceptions count beside it. No navigation, and one action to open exceptions if needed. Nothing else misreads from three metres away.

**Cost:** A display is not a workplace for decisions. It answers how many, and most work needs a phone.

### Calendar or Schedule Page

**ID:** `litespec/frontend/patterns/page-frames#calendar-or-schedule-page`

**Strength:** default

**Question:** Ninety-three trips ahead need a shape that answers when, not what. The decision is density: a grid that shows the month against a list that shows the next trip.

**Default:** `month-grid-with-trips-marked`

**Compact:** Seven columns do not fit a phone. The agenda list is the phone shape, with each day as a date chip beside its trips. The compact option is `agenda-grouped-by-day`.

**Rationale:** The month grid stays the pick: ninety-three trips ahead need density, and the grid shows the month the list cannot. The agenda list is the runner-up. Prefer it on a phone, where seven columns become unreadable whichever way they are drawn. The week lanes are the other runner-up for a company with few cost centres and a clash to spot.

Never ship the week lanes as the default. Two cost centres fit, and the third cost centre breaks the shape.

**Reviewed for:** Which built date view breaks from the wireframe its catalog entry chose? Open the built month grids, agendas, and week lanes beside the catalog entry recording the chosen option and its reason.

#### `month-grid-with-trips-marked`: Month Grid with Trips Marked

```wireframe
page
  breadcrumb "Home" "Trips" "Schedule"
  header
    heading "March 2026"
    text "3 trips across 2 cost centres."
    action quiet "‹ Previous month"
    action quiet "Next month ›"
  region "March 2026"
    text muted "Sun Mon Tue Wed Thu Fri Sat"
    grid 35
      item: text "14" · status "Rotterdam trip" positive
```

**Rationale:** The month at a glance, with each trip sitting on its date. It is the only shape where a clash between two trips is visible without opening either.

**Cost:** Seven columns are hard to read on a phone, and a date with three trips shows three clipped pills. The agenda is the honest phone shape.

**Reference:** Google Calendar

#### `agenda-grouped-by-day`: Agenda Grouped by Day

```wireframe
page
  breadcrumb "Home" "Trips" "Schedule"
  header
    heading "Schedule"
    text "The next trips, soonest first."
    action primary "New trip"
  region "Sat 14 Mar 2026"
    list 1
      item: text "Rotterdam client visit" · text muted "08:00 to 12:00 · Amsterdam sales" · status "Active" positive · action "Open"
  region "Sat 21 Mar 2026"
    list 1
      item: text "Utrecht client visit" · text muted "08:00 to 12:00 · Rotterdam sales" · status "Scheduled" info · action "Open"
  region "Sat 28 Mar 2026"
    list 1
      item: text "Amsterdam client visit" · text muted "08:00 to 12:00 · Rotterdam sales" · status "Scheduled" info · action "Open"
```

**Rationale:** Each day is a date chip with its trips beside it. The reader scrolls time itself rather than paging through it.

**Cost:** Empty days still take a row or the rhythm breaks, so a quiet month is mostly scrolling. The grid shows the quiet month better.

**Reference:** Apple Calendar

#### `week-lanes-per-cost-centre`: Week Lanes per Cost Centre

```wireframe
page
  breadcrumb "Home" "Trips" "Schedule"
  header
    heading "Week 11 · 9 to 15 Mar"
    text "One lane per cost centre. A block is a trip."
    action quiet "‹ Previous week"
    action quiet "Next week ›"
  region "Amsterdam sales"
    text muted "Mon Tue Wed Thu Fri Sat Sun"
    text "Rotterdam trip · Sat · 32 of 40 expenses"
  region "Rotterdam sales"
    text muted "Mon Tue Wed Thu Fri Sat Sun"
    text "Rotterdam trip · Sat · 12 of 40 expenses"
```

**Rationale:** Each cost centre gets a lane and each trip a block on its days. A clash at one cost centre is a visible overlap, not a second page.

**Cost:** Two cost centres fit. The third cost centre breaks the shape, and a phone has no room for lanes at all. Only honest for a company with few cost centres.

**Reference:** Float

### Onboarding or Setup Page

**ID:** `litespec/frontend/patterns/page-frames#onboarding-or-setup-page`

**Strength:** default

**Question:** A new company has an account and nothing else. The decision is how much the page does. A checklist waits, a wizard leads, and a single page states the three steps and gets out of the way.

**Default:** `checklist-with-a-progress-bar`

**Compact:** The recommended option reflows: the progress line and the steps stack in one column, each step keeping its status and action.

**Rationale:** The checklist with a progress bar stays the pick: it states the three steps, shows which is done, and waits without leading. The stepper wizard is the runner-up. Prefer it when a step cannot be skipped and the next depends on the last. Starting from a template is the other runner-up when the first trip is a known shape.

Three saves for a first run is three chances to leave half set up.

**Reviewed for:** Which built first-run guide breaks from the wireframe its catalog entry chose? Open the built checklists, wizards, and templates beside the catalog entry recording the chosen option and its reason.

#### `checklist-with-a-progress-bar`: Checklist with a Progress Bar

```wireframe
page
  breadcrumb "Home" "Set up"
  header
    heading "Set up Harbor and Pine"
    text "Three steps between a company and its first payout."
  region "Before you can take money"
    text muted "1 of 3 steps done."
  region "Create a company"
    status "Done" positive
    text "Harbor and Pine, Amsterdam."
  region "Connect a payment account"
    status "Current" info
    text "The provider holds the money for you."
    action primary "Connect"
  region "Publish the expense form"
    status "Open" neutral
    text "Your own address, so spenders file expenses where you send them."
    action "Set up"
```

**Rationale:** Three steps with a done, current, and open marker, and a bar that says how far along the company is. The page waits, and each step opens its own command.

**Cost:** A checklist can be ignored, and a reader who skips the payment step owns a back office that cannot take money. The home page's setup section has to repeat the warning.

**Reference:** Shopify

#### `stepper-wizard-one-step-open`: Stepper Wizard, One Step Open

```wireframe
page
  breadcrumb "Home" "Set up"
  header
    heading "Set up Harbor and Pine"
    text "Step 2 of 3. Two minutes, and you can stop after this one."
  region "Steps"
    text muted "1 The company done · 2 Payments current · 3 Expense form open"
  region "Connect a payment account"
    field "Business number" text
    field "Payout account" text
    action primary "Connect the provider"
    action "Back"
```

**Rationale:** One step open at a time with a stepper above it. Each step validates before the next opens, so nothing half-filled is ever left behind.

**Cost:** A wizard leads, which means it also blocks: the reader cannot add a trip until the wizard is done. Wrong when the steps are independent.

**Reference:** Stripe

#### `start-from-a-template`: Start from a Template

```wireframe
page
  breadcrumb "Home" "Set up"
  header
    heading "What is the first trip?"
    text "The template sets the lines. Everything stays editable."
  region "Templates"
    list 3
      item: text "Client visit" · text muted "Hotel and train lines, one trip, scheduled" · action "Choose"
  row
    action quiet "Skip"
    action primary "Continue"
```

**Rationale:** Starting from a template names what the first trip is before anything else, and the template sets the lines to match. Everything stays editable after the choice.

**Cost:** A template is a guess about the trip, and a wrong guess sets lines nobody reads. Templates must stay visibly editable, never silently applied.

### Error or Not-Found Page

**ID:** `litespec/frontend/patterns/page-frames#error-or-not-found-page`

**Strength:** default

**Question:** Something is missing, broken or refused. The decision is how much the page owns: a dead end with links back, or a working page that names the fix and offers it.

**Default:** `centered-not-found-with-links-back`

**Compact:** The recommended option reflows: the statement and the links back stack centered in one column.

**Rationale:** The centered not-found with links back stays the pick: it says what happened, says where to go, and does both in one glance. The permission refusal is the runner-up. Prefer it whenever the page knows who can resolve it, because Back is not a fix. The full-page outage is the other runner-up when a provider failure blocks the page.

A retry button on a page that will never exist is a promise that cannot be kept. Name the state and the next step instead.

**Reviewed for:** Which built dead end breaks from the wireframe its catalog entry chose? Open the built not-found, outage, and refused views beside the catalog entry recording the chosen option and its reason.

#### `centered-not-found-with-links-back`: Centered Not-Found with Links Back

```wireframe
page
  breadcrumb "Home" "Trips"
  header
    heading "This page is not here"
    text muted "404 · /trips/rotterdam-client-visit/schedule"
    text "It may have been moved, or the address may be wrong. The trip and its expenses are untouched."
  region "Where to go"
    action primary "Back to home"
    action "Open expenses"
```

**Rationale:** What happened, where to go, in one glance: the code, one line, and the two places the reader most likely wanted. The 404 shape, minus the joke.

**Cost:** A dead end is still a dead end: the reader loses whatever they were doing. It must at least keep the navigation, so the way back is one click.

**Reference:** GitHub

#### `permission-refusal-with-the-fix`: Permission Refusal with the Fix

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "Your role cannot do this"
    text "Reversals sit with Finance. Nothing was changed."
  message "Support can read expenses and message spenders. Only Finance can issue a reversal." neutral
  region "What you can do"
    list 2
      item: text "Ask Priya Shah to grant Finance" · text muted "They own Harbor and Pine" · action "Ask"
    link "Read what your role may do"
  footer
    action quiet "Back to expense EXP-1042"
```

**Rationale:** A refusal that names its permission, with the one command that resolves it beside it. The page knows Jordan cannot issue a reversal and knows Priya Shah can grant it, so it says both.

**Cost:** Every refusal needs its own fix, and one without a fix falls back to explaining itself. The fix must be a command, never a longer paragraph.

#### `full-page-outage-with-the-state-named`: Full-Page Outage with the State Named

```wireframe
page
  header
    heading "Payments cannot be taken right now"
    text "Payment provider · since 11:04."
  region "What still works"
    text "Filed expenses stay valid, and receipts keep arriving."
    action "Read the status"
  region "What to do"
    action primary "Try again"
```

**Rationale:** When a provider is down, the page says which one, what still works and what does not. The status-page shape: the failure is named, so the reader stops guessing.

**Cost:** A full page for a partial failure hides what still works. Only honest when the failure truly blocks the page.

**Reference:** Stripe
