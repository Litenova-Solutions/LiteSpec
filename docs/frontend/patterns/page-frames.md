# Page Frame Patterns

**Stack:** any
**Concern:** frontend-ui

A page-frame pattern fixes the layout for one kind of page: which parts sit where, in what order, and which action is primary. This page defines the frame for each kind: home, list, queue, record, form, report, settings, editor, outcome, operations, schedule, setup, entry, and error. Each pattern names the question it answers, the default option a project builds, and how that default behaves on a narrow screen, and shows every option as a wireframe. An owner picks an option from the wireframes, and the team records the choice in its pattern catalog, the file holding the project's interface decisions. One frame per kind keeps every screen predictable: the reader knows where the heading, actions, and content sit before reading. This page belongs to the Page Frames category. Its patterns are tested layouts a team chooses from, not required rules.

## Concepts

### How to Read a Pattern

A pattern answers one recurring interface question, here a layout question such as how a list page arranges its search, filters, table, and paging. One shared frame per kind means every screen of a kind arranges those parts the same way instead of each route defining its own. Each pattern below names its question, the default option a project builds unless its catalog records another, how that default behaves on a narrow screen, and why that option is the default.

Each option shows one wireframe drawing, a plain-text sketch of the parts and their order, with its rationale, its cost, and, for some options, the outside product it learns from. Your project records its choice in its pattern catalog and builds the chosen option with its own components and design system. The [catalog contract and wireframe language](../patterns.md) state what each catalog entry holds and how to read the drawings. The slug in each pattern ID, such as `record-page`, is the key a screen's `pageFrame` field and the design contract's Page Frames section cite.

### The Running Example

The drawings follow the Claimwick expense product for Harbor and Pine: expense EXP-1042 for Ava, the Rotterdam client visit, and the Amsterdam sales cost centre. Each kind of page uses one frame, so each screen shows one heading. Each page carries exactly one visible `h1` under `litespec/frontend/ui#pass-every-route-suite-check`, which the route suite checks on every route. The [primary-action default](record-detail.md#show-one-primary-action-in-a-record-header) is stated on the record detail page, and each header on these frames follows it.

## Patterns

### Home Page

**ID:** `litespec/frontend/patterns/page-frames#home-page`

**Strength:** default

**Question:** What the first page after sign-in shows. It tells the reader what needs doing now, most urgent first, before any figure. A home page typically holds a greeting, stat tiles, a setup section, a needs-attention list, and a coming-up list. The options disagree on how much of that sits above the fold, and on whether the tiles lead or the work leads.

**Default:** `work-first-tiles-second`

**Compact:** On a narrow screen the needs-attention and coming-up lists stack in one column above the figures, which stack one per row.

**Rationale:** The default shows the work first and the figures second: the manager reads the queue and stops, and a reader who wants the numbers scrolls for them. The my-work split is the runner-up: choose it once assignment is a real command and two people share one queue. The tiles-first layout is the other runner-up, for a reader who opens the page to check a number. Never ship the tiles-first layout as the default: three stat tiles push the queue below the fold, and the page reads as a report instead of a task list.

**Reviewed for:** Does the built start screen match the wireframe its catalog entry chose? Open the built queues, tiles, and next actions beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The page shows what needs doing first and the figures underneath. A returning manager reads the two-item queue and stops, and a reader who wants the numbers scrolls for them: 9,412 filed this month, EUR 57,506 paid out, 2 active now.

**Cost:** The tiles lose prominence, and a reader who came to check a number must scroll past the work first. The work-first order costs one screen of vertical space.

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

**Rationale:** The figures give the reader a sense of scale at a glance: 9,412 filed, 2 coming up, none paying out now. The setup checklist and the queue below read as a drill-down from those numbers.

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

**Rationale:** This is the triage shape. What is assigned to Alex sits above what belongs to Harbor and Pine as a whole, so two people never decide the reversal on EXP-1042 twice. The shared queue stays visible below, so one person's queue never hides the shared state.

**Cost:** Assignment must be a real command with a real owner, or every row shows Alex and the split adds nothing. The split also costs the tiles their place on short screens.

**Reference:** Linear

### List Page

**ID:** `litespec/frontend/patterns/page-frames#list-page`

**Strength:** default

**Question:** How to find one record among many. The frame is fixed: header, toolbar, one table, and pagination. The decision is which controls earn their place in the toolbar, because a search box on its own is not enough across 268 expenses.

**Default:** `search-and-a-state-filter-only`

**Compact:** On a narrow screen the search box and state filter stack above the table, and the table scrolls sideways inside its own region.

**Rationale:** The default builds search plus one state filter: two controls answer the two questions readers ask, who is this and what state is it in, and a bare list needs only the filter added. The row of views is the runner-up for the states a reader returns to. The dense filter bar is the other runner-up past a few hundred rows across many spenders, where the query is restated in words. A list page holds one table: a second table belongs on a second route. Views can replace the state filter, but they never replace the search box.

**Reviewed for:** Does the built collection screen match the wireframe its catalog entry chose? Open the built searches, views, and filter bars beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** Two controls answer the two questions people ask: who is this, and what state is it in. The search box takes an expense number, a name, or an email, and the state filter narrows by state; everything else is a view. This is the smallest honest addition to a bare list.

**Cost:** A reader with 268 expenses from 40 spenders still cannot narrow to one spender. That stays acceptable while the search box already searches the spender name, so typing Ava answers the same question.

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

**Rationale:** A segmented control across the top reads All, Paid, Awaiting, Reversed, Disputed, each with its count. One control replaces five, and the current choice stays visible rather than hiding in a list.

**Cost:** Five segments plus a search box crowd one toolbar.

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

**Rationale:** One filter bar states the query in words, such as state:paid, a count says 214 of 268 expenses matched, and a dense table sits under it. The reader sees what narrowed the list without opening anything, and can save the view for next time.

**Cost:** A query syntax must be learned, and typing a query is slower than pressing a segment for the two daily states. It fits lists past a few hundred rows, and adds noise below that size.

**Reference:** GitHub

### Queue

**ID:** `litespec/frontend/patterns/page-frames#queue`

**Strength:** default

**Question:** How to process items in batches. The queue is the one list page where selection is the interface: the reader selects rows and acts on them together. The page must answer what the header checkbox selects, and what happens to a thousand of them.

**Default:** `select-all-on-the-page-banner-for-the-rest`

**Compact:** On a narrow screen the banner and the sticky footer keep their place, and the table scrolls sideways inside its own region.

**Rationale:** The default builds select-all on the page, then the rest from a banner. It is the clearest answer to the select-all problem: the header checkbox selects the 25 rows on screen, and the banner offers all 31 as an explicit second act. Acting on money is worth the second click. The play mode is the runner-up: choose it when each decision needs different information and batch work is rare. A checkbox on every row is the other runner-up for a queue whose bulk job is small and capped. Keep the search box and the filter in place while rows are selected: a bulk bar that replaces the toolbar shifts the layout mid-decision and hides the controls the reader uses to refine it.

**Reviewed for:** Does the built batch-selection screen match the wireframe its catalog entry chose? Open the built select-all banners, play modes, and per-row checkboxes beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The header checkbox selects the 25 rows on screen. A banner then says all 25 on this page are selected, and offers all 31, while the sticky footer totals the batch at EUR 1,155.00. The larger set becomes an explicit second act rather than a silent expansion.

**Cost:** Acting on everything takes two clicks, and the banner is one more thing on the page. It gives the clearest available answer to the select-all problem.

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

**Rationale:** Selection is per expense and never global, which is honest for money. The reader acts on up to 20 at a time, and the page never implies it can act on all 31 at once.

**Cost:** The most common bulk job, reversing everything inside the window, becomes 31 clicks. A cap that protects money also takes away the batch work the queue exists for.

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

**Rationale:** The queue shows one record at a time, with reversal, credit, and Skip beside it and the next expenses already named below. Each reversal gets full attention: the reader sees the whole record before deciding.

**Cost:** One at a time is slow for thirty identical reversals, and the batch these queues exist for disappears. This shape fits decisions that each need different information, never uniform work.

**Reference:** Zendesk

### Record Page

**ID:** `litespec/frontend/patterns/page-frames#record-page`

**Strength:** default

**Question:** How one record page shows the record and everything done to it. The page carries a summary and the record's facets, and it decides where history and editable properties sit. The decision is how to divide the page: tabs against stacked sections, and what earns the aside slot.

**Default:** `summary-sections-aside-for-history`

**Compact:** On a narrow screen the sections stack in one column and the history aside follows them, with the header actions under the title.

**Rationale:** The default builds the summary with sections and history in the aside. What defines the record runs down the main column, while history supports it from the side. The properties rail is the runner-up: choose it when the aside must carry editable values rather than history. The tabs layout is the other runner-up for a record with more facets than three sections allow. Never put the record's facts in the header: a dense header pushes every command below the fold on a phone and leaves no room for the breadcrumb trail.

**Reviewed for:** Does the built single-record screen match the wireframe its catalog entry chose? Open the built summaries, tabs, and asides beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The main column carries what defines the record in at most three sections, and the narrower aside carries the history that supports but does not define it. EXP-1042 shows it: the expense in the column, the payout trail in the aside. The shape is proven: a summary at the top, at most three sections, and history in the aside.

**Cost:** Below roughly 1000 pixels the aside stacks under the sections, so the history lands after the actions. The record still reads acceptably in either order.

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

**Rationale:** When a record has more facets than three sections allow, tabs divide it. The Rotterdam trip splits into expenses, budget, reversals, and details, and the active tab sits in the URL, so a link carries the facet with it.

**Cost:** The reader cannot see two facets at once, and the tab count grows with every new facet the back office adds. Guidance caps tabs at about six for this reason.

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

**Rationale:** This is the issue shape. The record shows down the main column while every editable property sits in the right rail, changed where it is shown: owner, follow-up, and spender note save from the rail itself. There is no Edit mode and no second page for one value.

**Cost:** Every property needs its own save and its own failure state, so five quick edits are five round trips. This shape fails where a change must be reviewed whole before anything is written.

**Reference:** Linear

### Form Page

**ID:** `litespec/frontend/patterns/page-frames#form-page`

**Strength:** default

**Question:** How to take one command's input. A new-expense page can put every field in one section, stack several labeled blocks, or ask one question per screen. The decision is how much structure one command earns.

**Default:** `one-section-one-column-of-fields-cancel-beside-save`

**Compact:** On a narrow screen the single column of fields keeps its order, and Cancel sits beside Create expense at the end.

**Rationale:** The default builds one section, one column, Cancel beside the submit action. A single command reads as a list of questions with one place to submit and one place to leave. The stacked blocks are the runner-up when the form carries several labeled groups, such as the expense, the amounts, and the budget. The one-question-per-screen flow is the other runner-up when each answer changes the next question and nothing may be skipped. A stepped flow for work that fits one page adds steps with no benefit.

**Reviewed for:** Does the built input screen match the wireframe its catalog entry chose? Open the built sections, labeled blocks, and one-question flows beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The whole command fits one column: one place to submit, and one to leave. The section is the only box on the page, and the field labels sit above their controls, so name, description, category, amount, and code read as a list of questions.

**Cost:** A long form makes a long page, and every field carries the same weight, so a reader who came for the budget group cannot skip to it.

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

**Rationale:** The form splits into several labeled blocks, each with its title above its fields. The page stays scannable when one command carries groups that mean different things: the expense, the amounts, and the budget each explain themselves in a line.

**Cost:** The page grows longer, and the block titles add weight above fields that would otherwise read as one list.

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

**Rationale:** Each answer gets the whole screen in the one-thing-per-page shape. The name question opens first with its own Continue and Back, each answer validates before the next opens, and Back returns to the question before it. Nothing half-filled is lost to a section the reader skipped.

**Cost:** Five screens for a form that fits one page feels slow to a returning reader. The answers cannot be reviewed together until the final check, so reserve this shape for flows where each answer changes the next question.

**Reference:** GOV.UK

### Report Page

**ID:** `litespec/frontend/patterns/page-frames#report-page`

**Strength:** default

**Question:** How to read figures over one period. The page names the period, lets the reader step to another, and keeps the statement accountable, so every euro is shown. The decision is how the period changes and what leads: the statement, a trend, or a typed range.

**Default:** `chevron-pair-with-the-period-as-the-heading`

**Compact:** On a narrow screen the period heading keeps its chevrons, with the month select and download beside it, and the statement regions stack in one column.

**Rationale:** The default builds the chevron pair with the period as the heading. A reader cannot mistake a chevron for a tab, and the month select still allows a jump across months. The cards with sparklines are the runner-up: choose them mid-month when the question is trend rather than settlement. The typed range is the other runner-up for a report over an arbitrary span. Label each month control with its direction: a pair of unlabeled buttons leaves the reader guessing which one goes back.

**Reviewed for:** Does the built figures screen match the wireframe its catalog entry chose? Open the built period pickers, range controls, and trend cards beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The period is the page heading, with an unambiguous chevron on each side to step through consecutive months and a select to jump across them. September 2026 reads as the heading with Previous month and Next month beside it, so a reader cannot mistake a chevron for a tab.

**Cost:** Walking months one at a time is slow when comparing a quarter, which is why the month select stays beside the chevrons for the longer jump.

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

**Rationale:** A range control offers Last 30 days, This month, Last month, and Custom, beside a pair of typeable date fields for 9 Sep to 8 Oct. Nothing hides behind the calendar: the peak of 26 Sep reads under the chart.

**Cost:** Presets plus a range are two mechanisms on one control. The used preset must stay visible afterwards, or the reader loses track of which days the payout bars cover.

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

**Rationale:** Three cards with sparklines name the trend before the statement names the total: the analytics shape. The reader notices Expenses paid rising 12 percent on August before reading why, and the statement underneath keeps every euro accountable.

**Cost:** Sparklines invite trend questions the monthly statement cannot answer, and three cards push the accountable total down. This shape fits mid-month reading, and the plain statement fits settlement.

**Reference:** Shopify

### Settings Home

**ID:** `litespec/frontend/patterns/page-frames#settings-home`

**Strength:** default

**Question:** How to find one setting among many areas. A settings home can carry a whole tree: thirty routes under `/settings`, and the home page must hold all of them.

**Default:** `grouped-links-one-line-each`

**Compact:** On a narrow screen the grouped links stack in one column, each link keeping its line of explanation.

**Rationale:** The default builds grouped links. Each area names what it holds, the page never grows a form, and thirty routes fit in a handful of sections. The left navigation is the runner-up: choose it once areas gain enough depth to deserve their own tree. The searchable list is the other runner-up for a reader who knows the name of the setting. Group by the thing the setting controls, never by how often it changes: readers look for a thing, not a frequency.

**Reviewed for:** Does the built setup index match the wireframe its catalog entry chose? Open the built grouped links, settings trees, and search indexes beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** Each area is a link with a line saying what it holds, grouped into a handful of sections such as the company, filing, and people. The team row even says 5 people with 2 invitations outstanding, so the reader knows the area before clicking, and the page never grows a form.

**Cost:** Nothing states which areas are incomplete, so a new starter must open each one to check. The Harbor and Pine home page already covers this with its setup section.

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

**Rationale:** The areas sit in a left navigation and the chosen area opens beside it: the settings-tree shape. Thirty routes become one tree the reader scans without leaving the page, General stays marked as current, and company name, contact email, and time zone edit in place.

**Cost:** An area's incompleteness stays invisible until it is opened. A phone has no room for the tree, so the navigation must collapse into the grouped links of the default.

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

**Rationale:** One flat list holds every setting with a search box over it. Thirty routes become one searchable index, and the reader types what they are looking for. Each row shows its trail, such as Settings, Cost centres, Budgets and spenders, so the result still says where the setting sits.

**Cost:** The grouping is lost, so a reader who knows the area must hunt through a flat list for it. The search must know every setting name a reader might type.

### Editor Page

**ID:** `litespec/frontend/patterns/page-frames#editor-page`

**Strength:** default

**Question:** How to edit what a spender sees, with a live preview beside the form. An expense form editor holds the look in a form, such as the logo, cover, accent color, and typeface, and shows the result in a preview. The decision is where the preview sits and how much of the page it takes, because the reader must judge a change without saving it.

**Default:** `form-left-live-preview-right-one-save`

**Compact:** On a narrow screen the form stays first and the live preview follows it in one column, with the one Save at the end of the form.

**Rationale:** The default builds the form left with a live preview right. Every control writes straight into the preview, and nobody saves to find out what a change looks like. The floating panel is the runner-up: choose it when the visual dominates, such as a cover image judged at real size. The preview drawer is the other runner-up when the form needs the full page. Keep the preview beside the form or over it: a preview above the form scrolls away with every edit, which defeats a live preview.

**Reviewed for:** Does the built preview screen match the wireframe its catalog entry chose? Open the built form-plus-preview splits, drawers, and floating panels beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** Every control writes straight into the preview, so the reader never saves to find out what a change looks like. The dropped logo, the typeface, and the accent color all show live beside the form.

**Cost:** This layout has no width toggle, so the preview cannot be judged at phone width. Two thirds of a 1440 screen is also a lot of preview for four small fields.

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

**Rationale:** The form takes the whole page, and the preview opens over it as a drawer on request. The reader checks the result as a spender sees it at either width, then closes the drawer without losing their place in the form.

**Cost:** The preview stays out of sight by default. A drawer adds a layer over the form for a preview whose purpose is reassurance.

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

**Rationale:** The preview takes the whole page and the settings float over it as a panel: the full-bleed shape. A cover image is judged at real size with the logo and Submit expense in place, and closing the panel shows the whole result.

**Cost:** The panel covers the preview it edits, and dragging it aside is awkward on a laptop trackpad. This shape fits a visual judged whole, such as a cover, while detailed fields want side-by-side.

**Reference:** Webflow

### Outcome Page

**ID:** `litespec/frontend/patterns/page-frames#outcome-page`

**Strength:** default

**Question:** Where a flow ends: an expense confirmed, a payment that failed, a link that expired. The ending carries one state, one statement of what happened, and one next step. Flows often end in a toast instead, so this frame states what an ending page holds.

**Default:** `one-statement-one-next-step`

**Compact:** On a narrow screen the statement, the facts, and the next step stack in one column in the same order.

**Rationale:** The default builds one statement with one next step. A reader who came to learn whether it worked leaves in one glance. The refusal outcome is the runner-up, for the same frame with the fix in the primary place. The confirmation band is the other runner-up when the flow leaves a reference the reader must keep. An outcome the reader may need to return to belongs on a page, not in a toast.

**Reviewed for:** Does the built result screen match the wireframe its catalog entry chose? Open the built statements, references, and refusal outcomes beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** What happened sits in the heading, what it means in one line, and exactly one primary action follows. EUR 45.00 on its way back to Ava reads with the one-to-three-days line under it, so a reader who came to find out whether it worked leaves in one glance.

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

**Rationale:** The same frame carries the refusal tone. The reversal window has closed at 214 days, and the primary action is the one that resolves it: not Back, but Offer credit instead.

**Cost:** Every refusal needs a differently worded fix, and one that has none must fall back to explaining itself.

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

**Rationale:** A green band carries the outcome and the reference together: the confirmation panel. Reversal sent with EXP-1042 is the one thing to keep and the one thing that stands out, and everything below it is supporting detail.

**Cost:** The band works for success and only success. A refusal in the same shape would read as approval, so reserve it for flows that leave a reference behind, and never use it as a generic end screen.

**Reference:** GOV.UK

### Live Operations Page

**ID:** `litespec/frontend/patterns/page-frames#live-operations-page`

**Strength:** default

**Question:** What a live operations page shows while payout run day is happening. The cost centres are open, scanners are reading, and the finance lead needs the count at a glance. The decision is what earns that glance: the total, the per-cost-centre split, or only the exceptions.

**Default:** `the-count-the-cost-centres-the-exceptions`

**Compact:** On a narrow screen the count stays first, and the cost centre cards stack above the exceptions in one column.

**Rationale:** The default builds the count with cost centres and exceptions. The finance lead reads one number first, then the per-cost-centre split, then only what needs a person. The exceptions-only view is the runner-up: choose it on the wall display once the day is steady and the count is already known. The full-screen count is the other runner-up for a display read from across the room. A page that lists every payout is a log, not a live view, so keep the count in the header and the exceptions above the fold.

**Reviewed for:** Does the built live view match the wireframe its catalog entry chose? Open the built counts, cost centres, and exception views beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** One number first, then the per-cost-centre split, then only what needs a person. The finance lead reads 554 of 840 in one glance and reaches the double-scanned EXP-1042 without scrolling past the day's log.

**Cost:** Three structures share one page, and the exceptions sit below the cost centres on a phone. The page still reads correctly, with the count first in either order.

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

**Rationale:** The count sits in the header, and below it only what needs a person appears. A finance lead who trusts the scanners reads 554 paid out of 840 and puts the phone away, with the resolved scanner outage kept under Earlier today for the record.

**Cost:** When nothing needs anybody the page is nearly empty, and an empty page on a wall display reads as broken. The count in the header is what proves it is alive.

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

**Rationale:** A wall display carries one number with the exceptions count beside it: 554 paid, 1 needs a person. There is no navigation, and one action opens the exceptions if needed. Nothing else on the screen can be misread from three meters away.

**Cost:** A display is not a workplace for decisions. It answers how many, and most work needs a phone.

### Calendar or Schedule Page

**ID:** `litespec/frontend/patterns/page-frames#calendar-or-schedule-page`

**Strength:** default

**Question:** What shape answers when, not what, for ninety-three trips ahead. The decision is density: a grid that shows the month against a list that shows the next trip.

**Default:** `month-grid-with-trips-marked`

**Compact:** Seven columns do not fit a phone, so the agenda list is the phone shape: each day becomes a date chip beside its trips. The compact option is `agenda-grouped-by-day`.

**Rationale:** The default builds the month grid. Ninety-three trips ahead need density, and the grid shows the month the list cannot. The agenda list is the runner-up: choose it on a phone, where seven columns become unreadable whichever way they are drawn. The week lanes are the other runner-up for a company with few cost centres and a clash to spot. Never ship the week lanes as the default: two cost centres fit, and the third cost centre breaks the shape.

**Reviewed for:** Does the built date view match the wireframe its catalog entry chose? Open the built month grids, agendas, and week lanes beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The grid shows the month at a glance, with each trip sitting on its date, such as the Rotterdam trip on the 14th. It is the only shape where a clash between two trips is visible without opening either.

**Cost:** Seven columns are hard to read on a phone, where a date holding three trips shows three clipped pills. For the phone, the agenda is the honest shape.

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

**Rationale:** Each day becomes a date chip with its trips beside it, soonest first. The reader scrolls from the Rotterdam visit on the 14th through Utrecht and Amsterdam rather than paging through months, which suits a phone.

**Cost:** Empty days still take a row or the rhythm breaks, so a quiet month is mostly scrolling past blank days. The grid shows the quiet month better at a glance.

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

**Rationale:** Each cost centre gets a lane and each trip a block on its days: Amsterdam sales with 32 of 40 expenses beside Rotterdam sales with 12 of 40. A clash at one cost centre shows as a visible overlap, not a second page.

**Cost:** Two cost centres fit. The third cost centre breaks the shape, and a phone has no room for lanes at all. Only a company with few cost centres can use this shape honestly.

**Reference:** Float

### Onboarding or Setup Page

**ID:** `litespec/frontend/patterns/page-frames#onboarding-or-setup-page`

**Strength:** default

**Question:** A new company has an account and nothing else. The decision is how much the page does. A checklist waits, a wizard leads, and a template names the first trip before anything else.

**Default:** `checklist-with-a-progress-bar`

**Compact:** On a narrow screen the progress line and the steps stack in one column, each step keeping its status and action.

**Rationale:** The default builds the checklist with a progress bar. It states the three steps, shows which is done, and waits without leading. The stepper wizard is the runner-up: choose it when a step cannot be skipped and the next depends on the last. Starting from a template is the other runner-up when the first trip is a known shape. Three saves in a first run are three chances to leave the company half set up.

**Reviewed for:** Does the built first-run guide match the wireframe its catalog entry chose? Open the built checklists, wizards, and templates beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** Three steps carry done, current, and open markers, with 1 of 3 steps done saying how far along the company is. The company step shows done, payments show current with their Connect action, and the expense form waits open. The page waits, and each step opens its own command.

**Cost:** A checklist can be ignored, and a reader who skips the payment step owns a back office that cannot take money. The home page's setup section must repeat the warning.

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

**Rationale:** One step stays open at a time with a stepper above it. Step 2 of 3 asks for the business number and payout account with its own Back, each step validates before the next opens, and nothing half-filled is ever left behind.

**Cost:** A wizard leads, which means it also blocks: the reader cannot add a trip until the wizard is done. When company, payments, and form stand alone, that block serves no purpose.

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

**Rationale:** Starting from a template names what the first trip is before anything else, and the template sets the lines to match. The client visit template brings hotel and train lines for one scheduled trip, Skip stays beside Continue, and everything stays editable after the choice.

**Cost:** A template guesses what the trip is, and a wrong guess sets lines nobody reads. Templates must stay visibly editable, never silently applied behind the Continue button.

### Entry Page

**ID:** `litespec/frontend/patterns/page-frames#entry-page`

**Strength:** default

**Question:** What a page outside the application shell shows: sign-in, workspace choice, invitation acceptance, or session end. It shows no work and no record, and it holds no command with inputs; it holds one card with a choice or a notice. The decision is how much product surrounds the card: nothing, a panel beside it, or the product behind it.

**Default:** `single-centered-card`

**Compact:** On a narrow screen the card takes the full width, and the product panel drops away on the options that have one.

**Rationale:** The default builds the single centered card. A reader who came to sign in meets the methods and nothing else, and the note on what the product stores answers the one question such a page raises. The split with a product panel is the runner-up: choose it where the product story earns half the screen, such as a first sign-in after an invitation. The floating card over a full panel is the other runner-up for a page that leads with the product. An entry page shows no navigation shell: there is no session to navigate with.

**Reviewed for:** Does the built entry screen match the wireframe its catalog entry chose? Open the built cards, splits, and panels beside the catalog entry recording the chosen option and its reason.

#### `single-centered-card`: Single Centered Card

```wireframe
page
  card
    text muted "Harbor and Pine"
    heading "Sign in to Claimwick"
    text "Expenses, payouts, and budgets for your company."
    action primary "Continue with Google"
    action "Continue with email"
    text muted "Claimwick stores your name and work email. No password: sign-in runs through your provider."
```

**Rationale:** The page is one card: the product name, the title, one sentence on what the product does, the method buttons, and the note on what the product stores. A reader who came to sign in meets the methods and nothing else.

**Cost:** The card says nothing about the product beyond one sentence, so a reader arriving from an invitation learns nothing before signing in. The split option exists for that reader.

#### `split-with-a-product-panel`: Split with a Product Panel

```wireframe
page
  split 1:1
    card
      image "Harbor and Pine paying out expenses, product panel"
      heading small "Every expense, paid and accounted"
      text "268 expenses this month, EUR 57,506 paid out."
    card
      text muted "Harbor and Pine"
      heading "Accept the invitation"
      text "Priya Shah invited you to file expenses for Amsterdam sales."
      action primary "Accept and sign in"
      action quiet "Decline"
      text muted "Claimwick stores your name and work email."
```

**Rationale:** The product panel carries the story beside the card: what the product does, with its figures. The invitation reader learns the product before accepting, and the card keeps the choice itself small.

**Cost:** Half the screen serves a reader who already knows the product. On a narrow screen the panel drops away, so the story must never carry the only statement of what accepting means.

#### `card-floating-over-a-full-panel`: Card Floating over a Full Panel

```wireframe
page
  image "Expense payouts across Harbor and Pine, full product panel"
  card
    text muted "Harbor and Pine"
    heading "Sign in to Claimwick"
    text "Expenses, payouts, and budgets for your company."
    action primary "Continue with Google"
    action "Continue with email"
```

**Rationale:** The product panel fills the page behind a floating card: the marketing shape. A first-time reader meets the product at full size with the sign-in beside it.

**Cost:** Text over imagery risks contrast, and the panel competes with the action it surrounds. Reserve it for a page that leads with the product; a returning reader meets the single card.

### Error or Not-Found Page

**ID:** `litespec/frontend/patterns/page-frames#error-or-not-found-page`

**Strength:** default

**Question:** What to show when something is missing, broken, or refused. The decision is how much the page owns: a dead end with links back, or a working page that names the fix and offers it.

**Default:** `centered-not-found-with-links-back`

**Compact:** On a narrow screen the statement and the links back stack centered in one column.

**Rationale:** The default builds the centered not-found with links back. It says what happened, says where to go, and does both in one glance. The permission refusal is the runner-up: choose it whenever the page knows who can resolve it, because Back is not a fix. The full-page outage is the other runner-up when a provider failure blocks the page. A retry button on a page that will never exist misleads the reader, so name the state and the next step instead.

**Reviewed for:** Does the built dead end match the wireframe its catalog entry chose? Open the built not-found, outage, and refused views beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** What happened and where to go show in one glance: the 404 code, one line saying the address may be wrong, and the two places the reader most likely wanted. The trip and its expenses show as untouched. A plain 404 shape, with no joke.

**Cost:** A dead end still ends the reader's task: they lose whatever they were doing. It must at least keep the navigation, so Back to home stays one click away.

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

**Rationale:** A refusal names its permission, with the one command that resolves it beside it. The page knows Jordan cannot issue a reversal and knows Priya Shah can grant it, so it says both, with a link to what the role may do for the full picture.

**Cost:** Every refusal needs its own fix, and one with no fix falls back to explaining itself. The fix must be a command, never a longer paragraph.

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

**Rationale:** When a provider is down, the page says which one, what still works, and what does not. The payment provider shows as down since 11:04 with Try again beside it. The status-page shape: the failure is named, so the reader stops guessing.

**Cost:** A full page for a partial failure hides what still works. Only a failure that truly blocks the page earns the full-page shape.

**Reference:** Stripe
