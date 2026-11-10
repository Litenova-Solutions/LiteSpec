# List and Table Patterns

**Stack:** any

**Layer:** frontend-ui

## Intent

This page holds the patterns for the expenses list, which shows many records of one kind so a reader can find one record, compare rows, and act on them: columns, sorting, filters, row density, selection, paging, row actions, table states, status, money, export, search, grouping, expansion, pinning, and the layout of a short list. A team building a list screen starts from these patterns, an owner picks an option from the drawings, and the team records the choice in its pattern catalog, the file holding the project's interface decisions. One shared set of answers keeps every list in the product behaving the same way, so a reader who learns one list knows them all.

## Concepts

### How to Read a Pattern

Each pattern answers one recurring question about lists, such as which columns a table shows. It states the question, the default option a project builds, how the default behaves on a narrow screen, and why the default won. Each option then shows one wireframe drawing with its own rationale, cost, and optional reference. A project records its choice in its pattern catalog and builds the chosen option with its own components. The [UI patterns](../patterns.md) page states the catalog contract and the wireframe language.

### The Running Example

The drawings on this page use the Claimwick expenses list for Harbor and Pine: 268 expenses across many spenders and trips, with expense EXP-1042 for Ava as one row. Filters, sorting, and paging narrow that list, and the money column shows amounts such as EUR 125.00. The Standards section states the two rules every option keeps: the list view lives in the address, and money columns align right with their currency.

## Standards

### Keep a List's View in Its Address

**ID:** `litespec/frontend/patterns/lists-and-tables#keep-a-lists-view-in-its-address`

**Strength:** rule

**Statement:** A list MUST keep its filters, sort order, and page in the URL.

**Rationale:** A view kept in the address survives the back button, a refresh, and a pasted link, so all three return to the same rows. Without it, opening one record loses the filters the reader set, and a link sent to a colleague opens the unfiltered first page instead of the rows under discussion.

**Reviewed for:** Which built list loses its filters, sort, or page on refresh? Open built lists, change the view, and reload the address.

### Align Money to the Right with Its Currency

**ID:** `litespec/frontend/patterns/lists-and-tables#align-money-to-the-right-with-its-currency`

**Strength:** rule

**Statement:** A table MUST right-align a money column and show the currency of every amount in it.

**Rationale:** Right alignment lines up place value down the column, so a reader compares hundreds against thousands without reading each figure digit by digit. A currency on every amount keeps EUR 125.00 distinct from a bare 125.00, so an export or a screenshot never leaves finance guessing which money it shows.

**Reviewed for:** Which built money column centers its figures or hides a currency? Open the money columns on built tables.

## Patterns

### Choosing the Columns

**ID:** `litespec/frontend/patterns/lists-and-tables#choosing-the-columns`

**Strength:** default

**Question:** Which columns does the expenses table show. The reader picks rows by expense, spender, date, state, and money. The trip name sits under the expense code as a second line.

**Default:** `five-columns-nothing-removable`

**Compact:** Below 640 pixels the table becomes two-line rows with the amount on the right and a Sort control in the toolbar. The compact option is the compact form of `five-columns-nothing-removable`.

**Rationale:** Keep the default five columns. They answer which expense, who, when, what state, and how much. The runner-up is the Columns popover. Add it once more than one team asks for a sixth fact such as method or cost centre.

Saved views are the later answer, when finance and the auditors want different tables. Never ship the question-shaped headers. They cost width and read as a form.

**Reviewed for:** Which built column set breaks from the wireframe its catalog entry chose? Open the built choosers and saved views beside the catalog entry recording the chosen option and its reason.

#### `five-columns-nothing-removable`: Five Columns, Nothing Removable

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense across every trip, newest first."
  toolbar
    field "Search expenses" search
  region "Expenses"
    table 5 "Expense" "Spender" "Submitted" "State" "Paid"
      item: field "Select EXP-1042" checkbox · text "EXP-1042" · text muted "Rotterdam client visit" · text "Ava" · text "8 Oct 09:32" · status "Paid" positive · text "EUR 125.00"
  footer
    text "1 - 25 of 268"
    action quiet "< Previous"
    action quiet "Next >"
```

```wireframe compact
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense, newest first."
  toolbar
    field "Expense number, name or email" search
    field "Sort" select = "Newest first"
  region "Expenses"
    list 6
      item: link "EXP-1042" · text "EUR 125.00" · text muted "Ava, 8 Oct 09:32" · status "Paid" positive
```

**Rationale:** The default set. Five columns is what fits 1440 without scrolling and covers the four questions: which expense, who, when, what state, and what money. On a phone the rows become two lines with the amount on the right and a Sort control replaces the headers.

**Cost:** Nothing can be dropped or added. A team that wants the trip as its own column, or that hides the email, has no way to get there. On a phone there are no headers, so sorting and comparison down a column move to a control or to the desktop.

#### `a-columns-popover-listing-every-column`: A Columns Popover Listing Every Column

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense across every trip, newest first."
  toolbar
    field "Search expenses" search
    action "Columns"
  region "Expenses"
    table 5 "Expense" "Spender" "Submitted" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 09:32" · status "Paid" positive · text "EUR 125.00"
  footer
    text "1 - 25 of 268"
    action quiet "< Previous"
    action quiet "Next >"
  popover "Columns"
    field "Expense · Always shown" checkbox
    field "Spender" checkbox
    field "Submitted" checkbox
    field "State" checkbox
    field "Paid" checkbox
    field "Trip" checkbox
    field "Method" checkbox
    field "Cost centre" checkbox
    action quiet "Reset to default"
```

**Rationale:** The Columns control opens a popover with every column, a checkbox each, and Reset to default. The expense column is always shown and says so. The view-options menu and the display panel work the same way.

**Cost:** The popover covers the table while it is open, and drag-to-reorder inside it is a second feature that most readers would never find.

**Reference:** shadcn/ui, Linear

#### `saved-views-carry-their-own-columns`: Saved Views Carry Their Own Columns

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Finance view: what was paid, how, and what went back."
  toolbar
    field "Search expenses" search
    field "Date" select = "This month"
  tabs "All expenses" "Finance" "Payout run"
    region "Finance"
      table 5 "Expense" "Submitted" "Method" "Paid" "Reversed"
        item: text "EXP-1042" · text "8 Oct 09:32" · text "Bank transfer" · text "EUR 125.00" · text muted "No reversal"
  footer
    text "1 - 25 of 58"
    action quiet "< Previous"
    action quiet "Next >"
```

**Rationale:** Saved views tie a column set to a view. All expenses keep the default five, Finance swaps in method, paid, and reversed, and Payout run shows spender, trip, and lines. Each role opens the table it needs, and a support reply can name the view.

**Cost:** Views are a feature with their own lifecycle: who creates one, whether shared, and what follows when a column retires. Too much for a first release.

**Reference:** HubSpot, Linear

### Sorting

**ID:** `litespec/frontend/patterns/lists-and-tables#sorting`

**Strength:** default

**Question:** How does a reader reorder the expenses table. The table starts newest first. Each sortable header offers its own order, and the sort lives in the URL so a link carries it.

**Default:** `every-sortable-header-carries-an-arrow`

**Compact:** Below 640 pixels the table becomes two-line rows and the headers leave with it. A Sort control carries the same sort from the toolbar. The compact option is `headers-on-a-desktop-a-sort-control-on-a-phone`.

**Rationale:** Give every sortable header its own arrow. The arrow states that headers sort on touch screens too. The runner-up is headers on a desktop with a Sort control on the phone list. It becomes necessary the moment the table turns into rows below 640 pixels.

Never ship multi-column sort. No finance question needs two keys.

**Reviewed for:** Which built sort control breaks from the wireframe its catalog entry chose? Open the built arrows and phone sort controls beside the catalog entry recording the chosen option and its reason.

#### `every-sortable-header-carries-an-arrow`: Every Sortable Header Carries an Arrow

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense across every trip."
  toolbar
    field "Search expenses" search
  region "Expenses"
    table 5 "Expense ^v" "Spender" "Submitted v" "State ^v" "Paid ^v"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 09:32" · status "Paid" positive · text "EUR 125.00"
  footer
    text "1 - 25 of 268"
    action quiet "< Previous"
    action quiet "Next >"
```

**Rationale:** Each sortable header is a ghost button with a faint up-down arrow, and the sorted one has a solid arrow. The common table examples and the dashboard tables do the same.

**Cost:** Five arrows is more ink in the header row, and the Spender header looks inert beside its neighbors.

**Reference:** shadcn/ui, TanStack, Stripe

#### `headers-on-a-desktop-a-sort-control-on-a-phone`: Headers on a Desktop, a Sort Control on a Phone

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense across every trip."
  toolbar
    field "Search expenses" search
    field "Sort" select = "Newest first"
  region "Expenses"
    table 5 "Expense ^v" "Spender" "Submitted v" "State ^v" "Paid ^v"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 09:32" · status "Paid" positive · text "EUR 125.00"
  footer
    text "1 - 25 of 268"
    action quiet "< Previous"
    action quiet "Next >"
```

```wireframe compact
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense across every trip."
  toolbar
    field "Search expenses" search
    field "Sort" select = "Newest first"
  region "Expenses"
    list 6
      item: text "EXP-1042 · Rotterdam client visit" · text "EUR 125.00" · text muted "Ava · 8 Oct 09:32" · status "Paid" positive
```

**Rationale:** A dashboard sorts from the headers on a wide screen. Its mobile list carries a Sort control instead, because rows have no headers to click. The same URL parameter drives both.

**Cost:** Two controls for one state, and only worth building once the table becomes a list on a phone.

**Reference:** Stripe

#### `sort-as-a-control-above-the-table`: Sort as a Control Above the Table

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense across every trip."
  toolbar
    field "Search expenses" search
    field "State" select = "Any state"
    field "Sort" select = "Newest first"
  region "Expenses"
    table 5 "Expense" "Spender" "Submitted" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 09:32" · status "Paid" positive · text "EUR 125.00"
  footer
    text "1 - 25 of 268"
    action quiet "< Previous"
    action quiet "Next >"
```

**Rationale:** The sort is one named control rather than several header affordances: Sort newest first. It works on a touch screen and states itself in words.

**Cost:** The reader has to find the control to change the order, and sorting belongs in the headers on a desktop table.

**Reference:** Carbon

### Filters

**ID:** `litespec/frontend/patterns/lists-and-tables#filters`

**Strength:** default

**Question:** How does a reader narrow the expenses table. Search alone answers one question. Each closed filter control shows how many values it applies, and several categories never share one dropdown.

**Default:** `one-filter-per-category-visible`

**Compact:** The recommended option reflows below `md`. One column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Show one visible control per category. A reader sees what can narrow the table without opening anything, and each category keeps its own control. The runner-up is faceted buttons with counts. Prefer it once a category holds more than a few values and the reader needs the split before choosing.

Never ship every category inside one dropdown. It hides the applied state and contradicts the stated rule.

**Reviewed for:** Which built filter control breaks from the wireframe its catalog entry chose? Open the built controls, panels, and facet buttons beside the catalog entry recording the chosen option and its reason.

#### `one-filter-per-category-visible`: One Filter per Category, Visible

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses across 40 trips."
  toolbar
    field "Search expenses" search
    field "Trip" select = "Any trip"
    field "State" select = "Any state"
    field "Date" select = "Any date"
    field "Cost centre" select = "Any cost centre"
  region "Expenses"
    table 5 "Expense" "Spender" "Submitted" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 09:32" · status "Paid" positive · text "EUR 125.00"
  footer
    text "1 - 25 of 268"
    action quiet "< Previous"
    action quiet "Next >"
```

**Rationale:** Trip, state, date, and cost centre each get their own control in one row. No menu holds more than one category, so a reader can see what is available without opening anything.

**Cost:** Four controls plus a search box is a wide toolbar. Below about 700 pixels they have to wrap or collapse, which is where the complexity moves.

#### `faceted-filter-buttons-with-counts`: Faceted Filter Buttons with Counts

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "58 of 268 expenses match."
  toolbar
    field "Search expenses" search
    action "State: Paid, Awaiting payment"
    action "Trip: Rotterdam client visit"
    action "Cost centre"
    action quiet "Clear 3"
  region "Expenses"
    table 5 "Expense" "Spender" "Submitted" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 09:32" · status "Paid" positive · text "EUR 125.00"
  footer
    text "1 - 25 of 58"
    action quiet "< Previous"
    action quiet "Next >"
  popover "Trip"
    field "Rotterdam client visit (41)" checkbox
    field "Utrecht audit (12)" checkbox
    field "Maastricht workshop (5)" checkbox
    field "Office supplies (0)" checkbox
    action quiet "Clear this filter"
```

**Rationale:** The tasks example draws each category as a button that opens a checklist with a count per value. The reader sees the split before choosing, and several values in one category combine without one select per value.

**Cost:** Counts per value are a read per category, and the popover covers the table while it is open. Worth it only where a category holds more than a few values.

**Reference:** shadcn/ui

#### `a-filter-panel-beside-the-table`: A Filter Panel Beside the Table

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses across 40 trips."
  split 1:3
    aside "Filter"
      field "Search" text
      field "Trip" select = "Any trip"
      field "State" select = "Any state"
      field "Cost centre" select = "Any cost centre"
      field "Only reversals" checkbox
      action primary "Apply"
      action quiet "Clear"
    region "Expenses"
      table 5 "Expense" "Spender" "Submitted" "State" "Paid"
        item: text "EXP-1042" · text "Ava" · text "8 Oct 09:32" · status "Paid" positive · text "EUR 125.00"
  footer
    text "1 - 25 of 268"
    action quiet "< Previous"
    action quiet "Next >"
```

**Rationale:** Filters in a panel down the left side, with every category open at once and nothing hidden. This works when there are more than three categories.

**Cost:** A permanent panel takes about 190 pixels from the table on every page, and most visits apply no filter at all. The panel is a cost paid by everyone for the benefit of a few.

**Reference:** Carbon

### Row Density

**ID:** `litespec/frontend/patterns/lists-and-tables#row-density`

**Strength:** default

**Question:** How tall is a row in the expenses table. Five heights from 24 to 64 pixels are common, and the header, toolbar, and pagination match the row size. The default table runs near 47 pixels per row.

**Default:** `40-pixels-the-default`

**Compact:** Below 640 pixels the table becomes two-line record rows with the amount right. Sorting and comparison down a column move to the desktop. The compact option is `a-record-list-instead-of-a-table`.

**Rationale:** Keep 40 pixels. Two lines fit, the row scans, and it matches the default size. The runner-up is 32 pixels, for a working queue where every row is read once and acted on.

Never ship a density control the reader sets. Most readers set it once, and a screenshot suite then holds three versions of every page. The phone answer is a record list.

**Reviewed for:** Which built row height breaks from the wireframe its catalog entry chose? Open the built 24, 32, and 40 pixel rows beside the catalog entry recording the chosen option and its reason.

#### `40-pixels-the-default`: 40 Pixels, the Default

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense across every trip, newest first."
  toolbar
    field "Search expenses" search
  region "Expenses"
    table 5 "Expense" "Spender" "Submitted" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 09:32" · status "Paid" positive · text "EUR 125.00"
  footer
    text "1 - 25 of 268"
    action quiet "< Previous"
    action quiet "Next >"
```

**Rationale:** The medium default height. Two lines of text fit and the row scans.

**Cost:** Twenty-five rows is 1000 pixels, so a 268-row table is eleven screens. Fine for browsing, slow for counting.

**Reference:** Carbon

#### `24-pixels-scannable`: 24 Pixels, Scannable

```wireframe
page
  breadcrumb "Home" "Expenses" "Reversals"
  header
    heading "Reversals"
    text "31 payouts to decide. Sorted by how long you have left."
  toolbar
    field "Search reversals" search
    field "Window" select = "Any window"
  region "Reversals"
    table 5 "Expense ^" "Spender" "State" "Amount" "Time left"
      item: text "EXP-1036" · text "Nina" · status "Window closed" critical · text "EUR 50.00" · text "Closed 34 days ago"
  footer
    text "1 - 25 of 31"
    action quiet "< Previous"
    action quiet "Next >"
```

**Rationale:** The extra-small height. One line per row, so the eye can travel straight down a column. The right choice for a queue a finance reader works through rather than reads.

**Cost:** One line means the trip name and the spender email have nowhere to go. Both have to be dropped or opened from the row.

**Reference:** Carbon

#### `a-record-list-instead-of-a-table`: A Record List Instead of a Table

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense across every trip, newest first."
  toolbar
    field "Search expenses" search
    field "State" select = "Any state"
  region "Expenses"
    list 5
      item: text "EXP-1042 · Rotterdam client visit" · text "EUR 125.00" · text muted "Ava · ava@harbor-pine.example" · status "Paid" positive · action "Open"
  footer
    text "1 - 25 of 268"
    action quiet "< Previous"
    action quiet "Next >"
```

**Rationale:** One card-width block per record with two lines and its own commands on the right. It is what the phone layout wants.

**Cost:** No columns, so no sorting and no comparison across a column. It trades every table capability for reading one record well.

### Bulk Action Bar

**ID:** `litespec/frontend/patterns/lists-and-tables#bulk-action-bar`

**Strength:** default

**Question:** What appears once expense rows are selected. The bar names the count and the money, then offers the commands. A reversal on the wrong scope is the expensive mistake this pattern guards against.

**Default:** `a-bar-above-the-table-naming-the-count`

**Compact:** The recommended option reflows below `md`. One column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep a bar above the table naming the count and the money. On a reversals page the total is the thing a reader checks before acting. The runner-up is a floating pill, for reversible bulk work such as resending receipts or tagging, where the bar leaves the table in place.

Never ship the bar at the foot. On a long table the action sits off screen while the checkboxes that enabled it do not.

**Reviewed for:** Which built selection bar breaks from the wireframe its catalog entry chose? Open the built count bars, pills, and dialogs beside the catalog entry recording the chosen option and its reason.

#### `a-bar-above-the-table-naming-the-count`: A Bar Above the Table Naming the Count

```wireframe
page
  breadcrumb "Home" "Expenses" "Reversals"
  header
    heading "Reversals"
    text "31 payouts to decide."
  toolbar
    field "Search reversals" search
    field "Window" select = "Any window"
  region "Selected"
    text "3 selected"
    text muted "EUR 200.00 of payout money"
    action "Reverse in full"
    action "Reverse part"
    action primary "Reverse these 3"
    action quiet "Clear"
  region "Reversals"
    table 4 "Expense" "Spender" "State" "Paid"
      item: field "Select EXP-1042" checkbox · text "EXP-1042" · text "Ava" · status "Paid" positive · text "EUR 125.00"
```

**Rationale:** The bar states how many are selected and what that total is worth, then offers the commands. The count is the thing a reader checks before acting on money.

**Cost:** The toolbar is still visible above the bar, so two rows of controls stack. Either the toolbar goes or the page gets taller.

#### `a-floating-pill-with-the-count-and-one-menu`: A Floating Pill with the Count and One Menu

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense across every trip, newest first."
  toolbar
    field "Search expenses" search
    field "State" select = "Any state"
  region "Expenses"
    table 5 "Expense" "Spender" "Submitted" "State" "Paid"
      item: field "Select EXP-1042" checkbox · text "EXP-1042" · text "Ava" · text "8 Oct 09:32" · status "Paid" positive · text "EUR 125.00"
  footer
    text "1 - 25 of 268"
    action quiet "< Previous"
    action quiet "Next >"
  popover "Selected"
    text "3 selected"
    action "Resend receipt"
    action "Add tag"
    action quiet "More actions"
```

**Rationale:** The selection bar can float over the content instead of pushing the table down. It holds the count, the one or two common commands, and a menu for the rest. The table never shifts when the first box is ticked.

**Cost:** The pill covers the bottom rows while it is up, and money wants its total stated, which a pill has no room for. Keep it for reversible work.

**Reference:** Notion, Linear

#### `bar-with-a-confirm-before-it-commits`: Bar with a Confirm Before It Commits

```wireframe
page
  breadcrumb "Home" "Expenses" "Reversals"
  header
    heading "Reversals"
    text "31 payouts to decide."
  toolbar
    field "Search reversals" search
    field "Window" select = "Any window"
  region "Selected"
    text "3 selected"
    text muted "EUR 200.00"
    action quiet "Clear"
    action "Reverse part"
    action primary "Reverse these 3"
  region "Reversals"
    table 4 "Expense" "Spender" "State" "Paid"
      item: field "Select EXP-1042" checkbox · text "EXP-1042" · text "Ava" · status "Paid" positive · text "EUR 125.00"
  dialog "Reverse 3 payouts in full?"
    text "EUR 200.00 comes back from 3 spenders."
    fact "Ava · EXP-1042" "EUR 125.00"
    fact "Eli · EXP-1033" "EUR 50.00"
    fact "Ava · EXP-1035" "EUR 25.00"
    message "One of these payouts is already 214 days old. The bank will not take that one back." caution
    action "Cancel"
    action destructive "Reverse 3 payouts"
```

**Rationale:** The primary button opens a dialog naming the count and the money. A bulk reversal is the kind of action that is rare and irreversible and so needs an interruption.

**Cost:** Two steps for a batch the reader has spent a minute selecting. Acceptable on money, wrong for anything reversible such as a bulk tag.

**Reference:** NN/g

### Pagination

**ID:** `litespec/frontend/patterns/lists-and-tables#pagination`

**Strength:** default

**Question:** How does a reader move through 268 expenses. The range and two chevrons move one page at a time. Numbered pages add a jump, and the count still says where the reader is.

**Default:** `count-left-numbered-pages-right`

**Compact:** Below 640 pixels the numbered pages leave and the pager keeps the range with two chevrons. The compact option is `a-compact-pager-range-plus-two-chevrons`.

**Rationale:** Keep the count with numbered pages. The reader can jump, and the range still says where they are. The runner-up is the compact pager, where width is tight and nobody jumps further than one page at a time.

Never ship the jump field. Typing a page number then Enter is slower than clicking for almost every reader.

**Reviewed for:** Which built page control breaks from the wireframe its catalog entry chose? Open the built chevrons, load-more buttons, and compact pagers beside the catalog entry recording the chosen option and its reason.

#### `count-left-numbered-pages-right`: Count Left, Numbered Pages Right

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense across every trip, newest first."
  toolbar
    field "Search expenses" search
  region "Expenses"
    table 5 "Expense" "Spender" "Submitted" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 09:32" · status "Paid" positive · text "EUR 125.00"
  footer
    text "1 - 25 of 268"
    action quiet "1"
    action quiet "2"
    action quiet "3"
    action quiet "4"
    action quiet "5"
    action quiet "< Previous"
    action quiet "Next >"
```

**Rationale:** The default shape with numbered pages added and no redundant page label. The reader can jump, and the count still says where they are.

**Cost:** Seven page numbers plus two chevrons is a wide control, and it has to collapse on a phone. The count and the numbers still overlap slightly.

#### `a-compact-pager-range-plus-two-chevrons`: A Compact Pager: Range Plus Two Chevrons

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense across every trip, newest first."
  toolbar
    field "Search expenses" search
  region "Expenses"
    table 5 "Expense" "Spender" "Submitted" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 09:32" · status "Paid" positive · text "EUR 125.00"
  footer
    text "1 - 25 of 268"
    action quiet "< Previous"
    action quiet "Next >"
```

**Rationale:** The pager is the range and two chevrons, nothing else. It fits a phone without collapsing, and on a 268-row list most readers move one page at a time anyway.

**Cost:** No jumping: reaching page 9 costs eight clicks. Only right where the reader rarely leaves the first pages.

**Reference:** Stripe

#### `load-more-as-a-button-at-the-foot`: Load More as a Button at the Foot

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense across every trip, newest first."
  toolbar
    field "Search expenses" search
  region "Expenses"
    table 5 "Expense" "Spender" "Submitted" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 09:32" · status "Paid" positive · text "EUR 125.00"
  footer
    action "Load 25 more"
    text muted "25 of 268 shown"
```

**Rationale:** Infinite scroll breaks keyboard users, so loading is a button a reader presses. The count stays visible, so the total is never a surprise.

**Cost:** A link to a specific page is no longer available. That link is the main reason admin lists land on pagination.

**Reference:** GOV.UK, Carbon

### Row Actions

**ID:** `litespec/frontend/patterns/lists-and-tables#row-actions`

**Strength:** default

**Question:** The expenses table shows rows of expenses. Each row may carry its own commands. How many commands sit on the row, and where do the rest live.

**Default:** `one-inline-action-the-rest-in-a-row-menu`

**Compact:** Below 640 pixels the list becomes one card per expense with its commands on the card. The compact option is the compact form of `one-inline-action-the-rest-in-a-row-menu`.

**Rationale:** Keep one inline command with the rest in the row menu. The common job stays one click away and the row stays narrow. Prefer the side preview where acting needs the record context, such as the lines before a resend. Never ship hover-revealed actions. Hover fails on touch, and a second focusable render doubles the table for no gain.

**Reviewed for:** Which built row command breaks from the wireframe its catalog entry chose? Open the built inline actions, row menus, and side previews beside the catalog entry recording the chosen option and its reason.

#### `one-inline-action-the-rest-in-a-row-menu`: One Inline Action, the Rest in a Row Menu

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense across every trip, newest first."
    action "Export CSV"
  toolbar
    field "Expense number, name or email" search
  region "Expenses"
    table 5 "Expense" "Spender" "Submitted" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 09:32" · status "Paid" positive · text "EUR 125.00" · action "Resend" · menu "More"
```

```wireframe compact
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses, one card each."
  toolbar
    field "Expense number, name or email" search
  region "Expenses"
    grid 4
      card "EXP-1042, EUR 125.00"
        text "Ava, Rotterdam client visit"
        status "Paid" positive
        action "Open"
        action "Resend"
```

**Rationale:** The first row command is drawn inline and everything else sits behind the row's overflow menu. The row's other commands belong in its menu. On a phone the list becomes cards with the commands on each card, because the phone is for acting on one expense.

**Cost:** The second command is invisible, and the overflow is a 32-pixel target that has to be found in every row. Cards are tall: three expenses fill the screen where rows fit six.

**Reference:** Shopify Polaris

#### `open-in-a-side-preview-act-from-there`: Open in a Side Preview, Act from There

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense across every trip, newest first."
  toolbar
    field "Expense number, name or email" search
  region "Expenses"
    table 5 "Expense" "Spender" "Submitted" "State" "Paid"
      item: link "EXP-1042" · text "Ava" · text "8 Oct 09:32" · status "Paid" positive · text "EUR 125.00"
  drawer right
    heading "EXP-1042"
    text "Ava · Rotterdam client visit"
    fact "Lines" "2 train fares and 1 hotel night"
    fact "Total" "EUR 125.00"
    action primary "Resend receipt"
    action "Copy the expense link"
    action destructive "Void the expense"
```

**Rationale:** A row can open in a side panel that carries the record's facts and its commands together. Resending Ava's receipt happens beside the expense lines, so the reader checks what is sent before sending it.

**Cost:** A panel over the list hides the rows it came from. On a phone it is a full screen, which makes it a page by another name.

**Reference:** Shopify, Stripe

#### `the-row-is-the-only-action`: The Row Is the Only Action

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense across every trip, newest first. Click an expense to open it."
  toolbar
    field "Expense number, name or email" search
  region "Expenses"
    table 4 "Expense" "Spender" "Submitted" "State" "Paid"
      item: link "EXP-1042" · text "Ava" · text "8 Oct 09:32" · text "Paid" · text "EUR 125.00"
```

**Rationale:** No commands on the row at all: clicking the expense number opens the record, and every command lives there. The table is for finding, the record is for acting.

**Cost:** A bulk job becomes one row at a time, so this shape fits finding, not acting.

### Table's Own States

**ID:** `litespec/frontend/patterns/lists-and-tables#tables-own-states`

**Strength:** default

**Question:** The expenses table is sometimes empty, still loading, or failed to load. Each case needs its own words and layout. What shows for each state of the table itself.

**Default:** `no-data-yet-table-replaced`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep each state as its own sentence for its own fact. No data yet replaces the table. A failed refresh keeps the chrome and names how old the rows are. A slow read names what is loading. Never ship a bare spinner over the table body. Rows that shift when the data lands move the row the reader already chose.

**Reviewed for:** Which built table state breaks from the wireframe its catalog entry chose? Open the built no-data, failed-read, and slow-read states beside the catalog entry recording the chosen option and its reason.

#### `no-data-yet-table-replaced`: No Data yet, Table Replaced

```wireframe
page
  breadcrumb "Home" "Trips"
  header
    heading "Trips"
    text "Six trips a month, from a EUR 2,000 client visit to a single fare."
    action primary "New trip"
  region "Trips"
    message positive "No trips yet. A trip holds the dates, the client site, the budget and what is on it."
    action primary "New trip"
```

**Rationale:** Nothing has ever existed, so the table and its headers are gone. One positive sentence offers the command that creates the first record.

**Cost:** The reader cannot see what a row will look like, so the columns are undiscoverable until there is data.

#### `a-failed-read-keeps-the-chrome`: A Failed Read Keeps the Chrome

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense across every trip, newest first."
  toolbar
    field "Expense number, name or email" search
  region "Expenses"
    message critical "These expenses could not be refreshed. What you see is from 2 minutes ago."
    action "Try again"
    table 4 "Expense" "Spender" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · status "Paid" positive · text "EUR 125.00"
```

**Rationale:** The table rendered before, so a refresh failed. Keeping the headers and showing the error in place tells the reader the data they can see is stale rather than current.

**Cost:** Old rows under an error can be acted on by mistake, which is the opposite of what the message wants to prevent.

#### `a-slow-read-that-says-what-it-is-doing`: A Slow Read That Says What It Is Doing

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense across every trip, newest first."
  toolbar
    field "Expense number, name or email" search
    action quiet "Stop"
  region "Expenses"
    message info "Reading 268 expenses. 6 s so far."
    table 4 "Expense" "Spender" "State" "Paid"
      item: text muted "Expense" · text muted "Spender" · text muted "State" · text muted "Paid"
```

**Rationale:** A slow read is named while it runs: what is loading, how long it has taken, and a way to stop waiting. A reader who knows the list holds 268 expenses waits. A reader watching a blank box leaves.

**Cost:** A progress note needs the total before the rows arrive, which is a second read. It also teaches the reader that some lists are slow, which invites the question why.

**Reference:** Vercel

### Status in a Column

**ID:** `litespec/frontend/patterns/lists-and-tables#status-in-a-column`

**Strength:** default

**Question:** Each expense row carries a state such as Paid or Disputed. Some states need a decision and some need none. How does the State column tell them apart.

**Default:** `tone-only-on-the-states-that-block`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Give a tone only to the states that block. Disputed and Awaiting payment take a badge because somebody must act. Paid and Completed stay plain words. Prefer the reason on a second line for a disputes queue where the word alone never answers why. Never ship progress marks. Four blocks per row on a 268-row list cannot serve a lifecycle that branches.

**Reviewed for:** Which built state badge breaks from the wireframe its catalog entry chose? Open the built tones, badges, and reason lines beside the catalog entry recording the chosen option and its reason.

#### `tone-only-on-the-states-that-block`: Tone Only on the States That Block

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense across every trip, newest first."
  toolbar
    field "Expense number, name or email" search
  region "Expenses"
    text muted "Disputed and Awaiting payment are toned. Paid is not."
    table 5 "Expense" "Spender" "Submitted" "State" "Paid"
      item: text "EXP-1039" · text "Eli" · text "8 Oct 06:22" · status "Awaiting payment" caution · text "EUR 45.00"
```

**Rationale:** States that need a decision get a tone. The rest are plain words. This keeps states that need no action as plain text and keeps the table readable.

**Cost:** The reader has to learn which states are toned, and a new state added later has to be classified before it is used.

#### `status-with-the-reason-on-a-second-line`: Status with the Reason on a Second Line

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense across every trip, newest first."
  toolbar
    field "Expense number, name or email" search
    field "Scope" select = "Needs attention"
  region "Expenses"
    table 4 "Expense" "Spender" "State" "Paid"
      item: text "EXP-1037" · text "Eli" · text "7 Oct 19:02" · status "Disputed" critical · text muted "Ben asked for evidence" · text "EUR 45.00"
```

**Rationale:** The payouts list can pair the state with its reason: Disputed with why Ben asked, Awaiting payment with how old the wait is. The word says what, the line says why, and the reader never opens a row to learn the reason.

**Cost:** Two lines doubles the row height for every row, including the paid ones whose reason is nothing. Only right on a queue where most rows need a decision.

**Reference:** Stripe

#### `a-badge-for-every-state`: A Badge for Every State

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense across every trip, newest first."
  toolbar
    field "Expense number, name or email" search
  region "Expenses"
    table 5 "Expense" "Spender" "Submitted" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 09:32" · status "Paid" positive · text "EUR 125.00"
```

**Rationale:** Every state is a badge mapping a code to a word and a tone. The word is the signal. The tone repeats it.

**Cost:** More than five or six status indicators overwhelm the eye. A status needing no action works better as plain text. A badge on every row is the opposite of that.

**Reference:** Carbon

### Money Column

**ID:** `litespec/frontend/patterns/lists-and-tables#money-column`

**Strength:** default

**Question:** Each expense row shows what the spender was paid. The column header names the currency. What does the money cell say beyond the number.

**Default:** `number-only-right-aligned`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the number only. The header names the currency and repeating EUR on every row is noise. Prefer the reversed part under the charged amount where reversals are the work rather than the exception. Never ship money as a share of the total. A derived percentage column answers a question no expenses list is asked.

**Reviewed for:** Which built amount column breaks from the wireframe its catalog entry chose? Open the built aligned figures and reversal columns beside the catalog entry recording the chosen option and its reason.

#### `number-only-right-aligned`: Number Only, Right-Aligned

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense across every trip, newest first."
  toolbar
    field "Expense number, name or email" search
  region "Expenses"
    table 5 "Expense" "Spender" "Submitted" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 09:32" · status "Paid" positive · text "EUR 125.00"
```

**Rationale:** The default shape. A column headed Paid is unambiguous about the currency, so repeating EUR on every row is noise.

**Cost:** EUR 0.00 reads as an error rather than as a fact. A zero row needs words that say why it is zero.

#### `refunded-shown-under-the-charged-amount`: Reversed Shown Under the Charged Amount

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense across every trip, newest first."
  toolbar
    field "Expense number, name or email" search
    field "Scope" select = "Any state"
  region "Expenses"
    table 5 "Expense" "Spender" "Submitted" "State" "Paid"
      item: text "EXP-1037" · text "Eli" · text "7 Oct 19:02" · status "Disputed" critical · text "EUR 45.00" · text muted "EUR 45.00 back from the spender"
```

**Rationale:** Invoice lines can keep one money column and put what went back on its second line: the charged amount, then what returned from the spender. One column stays scannable and the reversal is still on the row.

**Cost:** Two lines doubles the row height, and a part reversal needs a third fact, the net, which has nowhere to go in this cell.

**Reference:** Stripe

#### `two-money-columns-charged-and-refunded`: Two Money Columns: Charged and Reversed

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense across every trip, newest first."
  toolbar
    field "Expense number, name or email" search
    field "Scope" select = "Any state"
  region "Expenses"
    text muted "Reversed is the total that came back from the spender."
    table 6 "Expense" "Spender" "Submitted" "State" "Charged" "Reversed"
      item: text "EXP-1036" · text "Nina" · text "7 Oct 22:14" · text "Reversed" · text "EUR 50.00" · text "EUR 50.00"
```

**Rationale:** Where reversals are common, the net matters more than the gross. A second column turns a negative into a fact rather than something the reader has to reconstruct.

**Cost:** Two money columns on a five-column table is a lot of numbers. Most rows are identical in both, so the reader scans two equal columns to learn nothing.

### Exporting the List

**ID:** `litespec/frontend/patterns/lists-and-tables#exporting-the-list`

**Strength:** default

**Question:** A reader takes the expenses list elsewhere. Download serves one spreadsheet now. A schedule serves a repeated question. Which export does the list offer.

**Default:** `one-export-button-that-downloads-csv`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep one Export button that downloads the filtered rows as CSV. The accountant job costs one click and no decisions. Prefer a background export past a few thousand rows where a download would time out. Never ship copy as the only export. Copying the visible page silently drops every row past the pager.

**Reviewed for:** Which built download flow breaks from the wireframe its catalog entry chose? Open the built CSV buttons, dialogs, and background exports beside the catalog entry recording the chosen option and its reason.

#### `one-export-button-that-downloads-csv`: One Export Button That Downloads CSV

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense for the Rotterdam client visit, newest first."
  toolbar
    field "Expense number, name or email" search
    action "Export CSV"
  region "Expenses"
    table 5 "Expense" "Spender" "Submitted" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 09:32" · status "Paid" positive · text "EUR 125.00"
```

**Rationale:** The default behavior. One button in the toolbar downloads exactly the rows in the current filter as CSV. The accountant gets the Rotterdam client visit expenses with one click and no decisions.

**Cost:** There is no choice of columns or format. A reader who wants only expense number and paid, or an Excel file, trims or converts it elsewhere.

#### `a-large-export-that-runs-in-the-background`: A Large Export That Runs in the Background

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense for the Rotterdam client visit, newest first."
  toolbar
    field "Expense number, name or email" search
    action disabled "Exporting"
  region "Expenses"
    message info "Preparing 9,412 expenses as CSV. The file arrives by email to finance@harbor-pine.example."
    text muted "34 percent done."
    table 5 "Expense" "Spender" "Submitted" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 09:32" · status "Paid" positive · text "EUR 125.00"
```

**Rationale:** The bulk export prepares a large file while the reader keeps working, then email sends a download link. Past a few thousand rows a direct download times out, and a progress note beats a failed one.

**Cost:** The file arrives later, so the reader cannot reconcile against it right away. The export also needs its own failure state when the job dies overnight.

**Reference:** Shopify

#### `scheduled-email-export`: Scheduled Email Export

```wireframe
page
  breadcrumb "Home" "Expenses" "Scheduled exports"
  header
    heading "Scheduled exports"
    text "Harbor and Pine receives its expenses by email, on a rhythm it sets once."
    action "New schedule"
  region "Monday figures"
    text "One schedule per recipient, each with its own rhythm and scope."
    field "Every" select = "Monday 07:00"
    field "Send to" text = "finance@harbor-pine.example"
    field "Scope" select = "Rotterdam client visit"
    message info "The next export runs Monday at 07:00 and covers the expenses submitted since the last one."
```

**Rationale:** The team sets a rhythm once and finance receives the expenses without opening the product. A Monday 07:00 email answers the weekly question before anyone asks it.

**Cost:** A schedule is invisible state. Nobody sees it on the expenses page. A stale recipient keeps receiving figures until someone remembers the schedule exists.

### Searching with Highlighted Matches

**ID:** `litespec/frontend/patterns/lists-and-tables#searching-with-highlighted-matches`

**Strength:** default

**Question:** A search box filters the expenses table. A match on an email looks the same as a match on a name. How does the list show why each row matched.

**Default:** `matches-highlighted-in-the-cells`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Highlight the matches in the cells. The reader sees why each row matched without opening it. Prefer a scope control once readers search across trips and must say which one. Never ship a match column on its own. It spends a column stating what the highlight already shows.

**Reviewed for:** Which built list search breaks from the wireframe its catalog entry chose? Open the built highlighted cells and scoped searches beside the catalog entry recording the chosen option and its reason.

#### `matches-highlighted-in-the-cells`: Matches Highlighted in the Cells

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "2 of 268 expenses match ava."
  toolbar
    field "Expense number, name or email" search = "ava"
  region "Expenses"
    table 4 "Expense" "Spender" "Submitted" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 09:32" · text "Paid" · text "EUR 125.00"
```

**Rationale:** The search marks the matched text where it sits. The term stays visible in the box and every match is clear in its cell, so the reader trusts the filter.

**Cost:** Highlighting needs the match offsets from the server, not the rows alone. A client-side guess highlights the wrong occurrence on long text.

**Reference:** GitHub

#### `scoped-search-this-delivery-or-all-deliveries`: Scoped Search: This Trip or All Trips

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Searching Rotterdam client visit for ava."
  toolbar
    field "Expense number, name or email" search = "ava"
    field "Scope" select = "Rotterdam client visit"
  region "Expenses"
    text "1 match in Rotterdam client visit. 1 more in all trips."
    table 4 "Expense" "Spender" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · text "Paid" · text "EUR 125.00"
```

**Rationale:** The search is scoped to the current view or the whole company from one control beside the box. A reader inside the Rotterdam client visit searches it first and widens only when nothing matches.

**Cost:** A scope is invisible once set. A reader who narrowed to one trip last week searches it again today and wonders where the rest went.

**Reference:** Shopify

#### `a-searching-indicator-while-the-server-answers`: A Searching Indicator While the Server Answers

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Searching for ava rotterdam visit."
  toolbar
    field "Expense number, name or email" search = "ava rotterdam visit"
    action quiet "Stop"
  region "Expenses"
    message info "Searching 268 expenses for ava rotterdam visit."
```

**Rationale:** The dashboard says when a search is still running rather than showing yesterday's rows as if they matched. The reader waits instead of acting on a stale list.

**Cost:** A visible search state needs debouncing and canceling, or every keystroke queues a request and the indicator flickers.

**Reference:** Stripe

### Grouped Rows

**ID:** `litespec/frontend/patterns/lists-and-tables#grouped-rows`

**Strength:** default

**Question:** Some questions are asked per group, not per row: what did today pay, what does each trip hold. A flat table answers them only after an export, while a group header with its own subtotal answers them on the page.

**Default:** `grouped-by-day-with-subtotals`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep grouped by day with subtotals. It answers what today paid without a chart and without leaving the list. The runner-up is grouped by trip with collapse toggles. Prefer it once trips grow so one day of rows stops fitting. Never ship two group levels: month-then-day nesting turns the list into a tree that paging cannot cross.

**Reviewed for:** Which built group header breaks from the wireframe its catalog entry chose? Open the built day, trip, and flat groupings beside the catalog entry recording the chosen option and its reason.

#### `grouped-by-day-with-subtotals`: Grouped by Day with Subtotals

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Newest first, grouped by day."
  toolbar
    field "Expense number, name or email" search
    field "Any state" select = "Any state"
  region "Today, 8 Oct 2026, 4 expenses"
    fact "Subtotal" "EUR 270.00"
    table 4 "Expense" "Spender" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · status "Paid" positive · text "EUR 125.00"
  region "Yesterday, 7 Oct 2026, 2 expenses"
    fact "Subtotal" "EUR 95.00"
    table 4 "Expense" "Spender" "State" "Paid"
      item: text "EXP-1037" · text "Eli" · status "Disputed" critical · text "EUR 45.00"
```

**Rationale:** Bank statements and payout lists group money by day with a subtotal per group. The reader sees the shape of the spending and still opens any row.

**Cost:** Grouping removes the flat row order, so pagination and select-all both get harder to reason about across group edges.

**Reference:** Stripe

#### `grouped-by-delivery-with-collapse-toggles`: Grouped by Trip with Collapse Toggles

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Grouped by trip. Utrecht audit and Maastricht workshop are collapsed."
  toolbar
    field "Expense number, name or email" search
    field "Group" select = "Trip"
  region "Rotterdam client visit, 3 expenses"
    fact "Subtotal" "EUR 225.00"
    action quiet "Collapse"
    table 4 "Expense" "Spender" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · status "Paid" positive · text "EUR 125.00"
  region "Utrecht audit, 2 expenses"
    fact "Subtotal" "EUR 90.00"
    action quiet "Expand"
  region "Maastricht workshop, 1 expense"
    fact "Subtotal" "EUR 45.00"
    action quiet "Expand"
```

**Rationale:** Grouping by trip can put a toggle on each group header, so a reader following one trip collapses the rest. The header carries the count and the subtotal even while collapsed.

**Cost:** Collapsed groups hide rows from find-in-page and from a reader who forgot they collapsed anything. The toggle state needs to survive a reload or it annoys.

**Reference:** Linear

#### `no-grouping-a-group-column-that-sorts`: No Grouping: A Group Column That Sorts

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Sorted by trip, newest first inside each trip."
  toolbar
    field "Expense number, name or email" search
    field "Sort" select = "Trip"
  table 5 "Expense" "Trip" "Spender" "State" "Paid"
    item: text "EXP-1042" · text "Rotterdam client visit" · text "Ava" · status "Paid" positive · text "EUR 125.00"
```

**Rationale:** The table can stay flat and let the reader sort by the would-be group field instead. Nothing hides, paging stays simple, and the eye groups adjacent equal values on its own.

**Cost:** No subtotals: the reader who wants today's takings sorts by date and adds the rows up by hand. Sorting is not grouping, it only looks like it.

**Reference:** Carbon

### Inline Row Expansion

**ID:** `litespec/frontend/patterns/lists-and-tables#inline-row-expansion`

**Strength:** default

**Question:** Some rows hold a second level: an expense holds its lines, its receipts, its history. Opening the record for each one costs a page load per row. An expansion row under the row itself answers the question where it was asked.

**Default:** `expand-to-the-order-lines`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep expansion to the expense lines. It answers what the spender claimed, which is the question asked from the list. The runner-up is expansion to lines with decision state. Prefer it on the decision list where handover is the work. Never ship expand-all: twenty expanded rows is a page nobody can scan, and one click fires a read per row.

**Reviewed for:** Which built expandable row breaks from the wireframe its catalog entry chose? Open the built expense lines and decision states beside the catalog entry recording the chosen option and its reason.

#### `expand-to-the-order-lines`: Expand to the Expense Lines

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense, newest first."
  toolbar
    field "Expense number, name or email" search
  table 4 "Expense" "Spender" "State" "Paid"
    item: text "EXP-1042" · text "Ava" · status "Paid" positive · text "EUR 125.00"
  card "EXP-1042 lines"
    action quiet "Collapse"
    fact "Train fare, 2" "EUR 90.00"
    fact "Hotel night, 1" "EUR 35.00"
```

**Rationale:** An invoice list can expand a row to its lines: what was claimed, at what price, adding to the row total. Support answers "what did I pay for" without leaving the list.

**Cost:** The detail row breaks the column rhythm, and sorting reorders parents while children follow, which confuses a reader who sorted by money.

**Reference:** Stripe

#### `expand-to-units-and-pickup-state`: Expand to Lines and Decision State

```wireframe
page
  breadcrumb "Home" "Expenses" "Rotterdam client visit" "Decision list"
  header
    heading "Decision list"
    text "Rotterdam client visit, today. What is decided and what is not."
  toolbar
    field "Spender name or expense number" search
  table 3 "Expense" "Spender" "Decided"
    item: text "EXP-1038" · text "Nina" · text "1 of 2"
  card "EXP-1038 lines"
    action quiet "Collapse"
    fact "Train fare L-1041, Ben" "09:41"
    fact "Hotel night L-1042" "Not yet"
```

**Rationale:** A decision list expands an expense to its lines with what is decided and by whom. Ben works the list between meetings instead of opening forty records.

**Cost:** Decision state changes by the minute at busy times, so an expanded row goes stale while it is open. It needs the same refresh the decision screen has.

#### `no-expansion-the-row-opens-the-record`: No Expansion: The Row Opens the Record

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "The expense number opens the record. Nothing expands in place."
  toolbar
    field "Expense number, name or email" search
  table 4 "Expense" "Spender" "State" "Paid"
    item: link "EXP-1042" · text "Ava" · status "Paid" positive · text "EUR 125.00"
```

**Rationale:** The expense number is a link and everything lives on the record page. One pattern for detail everywhere, and the list stays a list.

**Cost:** Every question costs a page load and a back button. Checking three expenses means six page loads where expansion needs none.

### Pinned and Resizing Columns

**ID:** `litespec/frontend/patterns/lists-and-tables#pinned-and-resizing-columns`

**Strength:** default

**Question:** A wide table scrolls, and a scrolling table loses its bearings. The expense code leaves as the money arrives. Pinning the identity column keeps the row named, and resizing lets the reader spend width where their own data needs it.

**Default:** `the-order-column-pinned-while-the-rest-scroll`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the expense column pinned while the rest scroll. The row stays named at any scroll position, and it costs no control. The runner-up pins the actions too. Prefer it once rows carry inline commands that must stay reachable. Never ship draggable widths in a first release: per-user widths are stored state that support cannot see and screenshots cannot hold.

**Reviewed for:** Which built fixed column breaks from the wireframe its catalog entry chose? Open the built frozen first columns beside the catalog entry recording the chosen option and its reason.

#### `the-order-column-pinned-while-the-rest-scroll`: The Expense Column Pinned While the Rest Scroll

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "The expense column stays while the rest scroll under it."
  toolbar
    field "Expense number, name or email" search
  table 8 "Expense" "Trip" "Submitted" "Spender" "Cost centre" "State" "Method" "Paid"
    item: text "EXP-1042" · text "Rotterdam client visit" · text "8 Oct 2026, 09:32" · text "Ava" · text "Travel" · status "Paid" positive · text "Bank transfer" · text "EUR 125.00"
```

**Rationale:** An orders index can pin the order column and let the other columns scroll under it. Nothing is folded, so every fact keeps its own scannable column.

**Cost:** On a phone the reader sees the expense code and one other column at a time. Pinning keeps the bearings, not the overview.

**Reference:** Shopify

#### `first-and-last-pinned-actions-always-visible`: First and Last Pinned, Actions Always Visible

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Expense stays left, Resend stays right."
  toolbar
    field "Expense number, name or email" search
  table 7 "Expense" "Trip" "Spender" "Cost centre" "State" "Paid" "Action"
    item: text "EXP-1042" · text "Rotterdam client visit" · text "Ava" · text "Travel" · status "Paid" positive · text "EUR 125.00" · action "Resend"
```

**Rationale:** The record name can be pinned on the left and the row commands on the right, so both survive any scroll position. Acting never requires scrolling back.

**Cost:** Two pinned columns on a phone leave no room for the middle at all. The pattern needs a wide screen to earn its keep.

**Reference:** Airtable

#### `no-pinning-the-table-scrolls-as-one`: No Pinning: The Table Scrolls as One

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every column scrolls together. Nothing stays."
  toolbar
    field "Expense number, name or email" search
  table 7 "Expense" "Trip" "Spender" "Cost centre" "State" "Method" "Paid"
    item: text "EXP-1042" · text "Rotterdam client visit" · text "Ava" · text "Travel" · status "Paid" positive · text "Bank transfer" · text "EUR 125.00"
```

**Rationale:** The table scrolls as one piece and every column leaves together. No sticky edges, no shadows, no special cases in the layout.

**Cost:** Halfway across, the money column arrives and the expense code is gone. The reader scrolls back to check which row the amount belongs to.

**Reference:** Carbon

### The Layout of a Short List

**ID:** `litespec/frontend/patterns/lists-and-tables#the-layout-of-a-short-list`

**Strength:** default

**Question:** A small, dated list such as trips has a handful of records and asks how each one is doing. A table answers with rows of numbers, while a card or a date-led list can answer with the record itself: its dates, its place, and its progress.

**Default:** `trip-cards-with-a-budget-bar`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep cards with a progress bar. A handful of trips reads better as cards than as rows. The bar answers the only question the list exists for. The runner-up is a date-led list. Prefer it once the year holds dozens of trips and cards stop fitting. Never ship a month calendar for this question: it answers when, which the list is never asked.

**Reviewed for:** Which built short-list layout breaks from the wireframe its catalog entry chose? Open the built cards, table rows, and date-led lists beside the catalog entry recording the chosen option and its reason.

#### `trip-cards-with-a-budget-bar`: Trip Cards with a Budget Bar

```wireframe
page
  breadcrumb "Home" "Trips"
  header
    heading "Trips"
    text "Four trips, three with a date still ahead."
    action primary "New trip"
  toolbar
    field "Search trips" search
  grid 4
    card "Rotterdam client visit"
      image "Trip photo"
      text "Fri 9 Oct 2026, Rotterdam client site"
      status "Scheduled" info
      fact "Claimed" "EUR 125.00 of EUR 400.00"
      action "Open"
```

**Rationale:** Each trip is a card with its dates, its client site, and how much of its budget is claimed. Four trips a month read as four trips, not as four rows of a table.

**Cost:** Cards stop scanning past a dozen trips: no column to sort, no state to scan down. The pattern fits a handful, not a hundred.

#### `a-table-with-a-budget-progress-column`: A Table with a Budget Progress Column

```wireframe
page
  breadcrumb "Home" "Trips"
  header
    heading "Trips"
    text "Four trips, three with a date still ahead."
    action primary "New trip"
  toolbar
    field "Search trips" search
  table 5 "Trip" "Date" "State" "Claimed" "Action"
    item: text "Rotterdam client visit" · text "9 Oct 2026" · status "Scheduled" info · text "EUR 125.00 of EUR 400.00" · action "Open"
```

**Rationale:** A product list can keep the table and add an inventory bar per row. The trips stay sortable and comparable, and the bar still answers how much of the budget each one has claimed.

**Cost:** A bar in a table row is a chart without an axis. 80 percent of EUR 400.00 and 53 percent of EUR 600.00 read as two bars of different lengths for different totals.

**Reference:** Shopify

#### `a-date-led-list-with-month-dividers`: A Date-Led List with Month Dividers

```wireframe
page
  breadcrumb "Home" "Trips"
  header
    heading "Trips"
    text "Four trips, three with a date still ahead."
    action primary "New trip"
  toolbar
    field "Search trips" search
  region "October 2026"
    list 2
      item: text "Rotterdam client visit" · text muted "Rotterdam client site, 08:00" · status "Scheduled" info · text "EUR 125.00 of EUR 400.00"
  region "December 2026"
    list 1
      item: text "Utrecht audit" · text muted "Utrecht client site, 08:00" · status "Draft" neutral · text "Not scheduled yet"
```

**Rationale:** A schedule view can lead with the date and group by month. A planner reads October, then December, then March, each trip under its own day.

**Cost:** Dates lead and states follow, so finding the one draft means reading every month. The view answers when, not what needs work.

**Reference:** Google Calendar
