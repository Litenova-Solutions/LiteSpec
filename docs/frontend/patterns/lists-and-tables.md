# List and Table Patterns

**Stack:** any

**Concern:** frontend-ui

An expenses list shows many records of one kind so a reader can find one expense, compare rows, and act on them. Every list in a product asks the same questions: which columns it shows, how it sorts, filters, and pages, and how it handles row density, selection, row actions, table states, status, money, export, search, grouping, expansion, pinning, and the layout of a short list. These patterns settle each question once, with drawings to choose from. A team building a list screen starts from them, the owner picks an option, and the team records the choice in its pattern catalog, the file holding the project's interface decisions. One shared answer per question keeps every list behaving the same way, so a reader who learns one list knows them all.

## Concepts

### How to Read a Pattern

Each pattern answers one recurring question about lists, such as which columns a table shows. It names the question and the situation, the default option a project builds unless its catalog records another, how the default behaves on a narrow screen, and why the default won over its rivals. Below that, each option shows one wireframe drawing with its own rationale, its cost, and the outside product it learns from, where one exists. A project records its choice in its pattern catalog and builds the chosen option with its own components. The [UI patterns](../patterns.md) page states the catalog contract and the wireframe language.

### The Running Example

Every drawing on this page uses one example: the Claimwick expenses list for Harbor and Pine. It holds 268 expenses across many spenders and trips, and expense EXP-1042 for Ava stands in as one row. Filters, sorting, and paging narrow the list, and the money column shows amounts such as EUR 125.00. Five rules in the Standards section hold for every option on the page: the list view lives in the address, money columns align right with their currency, the sorted column carries its mark, the result count is announced, and a scrolling table sits in a named region.

## Standards

### Keep a List's View in Its Address

**ID:** `litespec/frontend/patterns/lists-and-tables#keep-a-lists-view-in-its-address`

**Strength:** required

**Statement:** A list MUST keep its filters, sort order, and page in the URL, and its expanded groups where it groups rows.

**Rationale:** Readers pass list views around: they open one expense and come back, they refresh, they paste a link to a colleague. When the filters, sort order, page, and expanded groups live in the URL, all three return to the same rows. Without that, opening one record wipes out the filters the reader set, and a pasted link opens the unfiltered first page instead of the rows under discussion.

**Reviewed for:** Does every built list keep its filters, sort, page, and expanded groups after a reload? Change the view on each built list and reload the address.

### Align Money to the Right with Its Currency

**ID:** `litespec/frontend/patterns/lists-and-tables#align-money-to-the-right-with-its-currency`

**Strength:** required

**Statement:** A table MUST right-align a money column and show the currency of every amount in it.

**Rationale:** Right alignment lines up place value down the column, so a reader compares hundreds against thousands at a glance instead of reading each figure digit by digit. Showing the currency on every amount keeps EUR 125.00 distinct from a bare 125.00, so an export or a screenshot never leaves finance guessing which money it shows.

**Reviewed for:** Does every built money column right-align its figures and show a currency on each? Open the money columns on built tables.

### Mark the Sorted Column

**ID:** `litespec/frontend/patterns/lists-and-tables#mark-the-sorted-column`

**Strength:** required

**Statement:** A sortable column header MUST be a button inside its header cell, with `aria-sort` and its direction on the sorted column only.

**Rationale:** A screen-reader user needs to hear which column orders the list and to change it like any other button. A header drawn as plain text hides the sorting, and a sorted column with no mark keeps the order silent even though the rows follow it. The button shape plus `aria-sort` with its direction on the sorted column gives both. This is WCAG 1.3.1 Info and Relationships.

**Reviewed for:** Is every built sortable header a button, and does only the sorted column carry the direction mark? Open built tables and read each sortable header and the sorted column.

### Announce a List's Result Count

**ID:** `litespec/frontend/patterns/lists-and-tables#announce-a-lists-result-count`

**Strength:** required

**Statement:** A list MUST announce its result count after a filter or search and its selection total after a selection changes, once per change through the page's polite status region.

**Rationale:** A filter narrows 268 expenses to 58, and a screen-reader user sitting on that list hears nothing: the rows changed under them. A bulk selection of "3 selected, EUR 412.00" stays silent too, so the reader acts without knowing the scope. Announcing the result count after each filter or search, and the selection total after each selection change, through the page's polite status region, keeps both in the open. Announce once per change, through the region in `litespec/frontend/patterns/feedback-and-states#announce-each-status-by-its-urgency`. This is WCAG 4.1.3 Status Messages.

**Reviewed for:** Does every built list announce its result count after a filter or search, and its selection total after a selection change, once per change? Change the view and the selection on built lists and listen for the announcement.

### Make a Scrolling Table a Named Region

**ID:** `litespec/frontend/patterns/lists-and-tables#make-a-scrolling-table-a-named-region`

**Strength:** required

**Statement:** A table that scrolls sideways MUST sit in a named region that takes keyboard focus and shows a cue at each edge it can still scroll.

**Rationale:** A sideways scroll without a focus stop is pointer-only, so a keyboard user on a 390 px screen has no other way to reach the columns past the edge, and without a cue at the edge a sighted user has no sign that more columns exist. A named region gives the table a stop in the tab order and a name for assistive technology, and the edge cue shows there is more to scroll to. This is WCAG 2.1.1 Keyboard.

**Reviewed for:** Does every built sideways-scrolling table take keyboard focus, carry a name, and show a cue at each edge it can still scroll? Open built wide tables at 390 px and tab to each table edge.

## Conventions

### Keep the Header Row in View

**ID:** `litespec/frontend/patterns/lists-and-tables#keep-the-header-row-in-view`

**Strength:** default

**Default:** Keep the header row of a table taller than the viewport visible while it scrolls, with no focused row hidden under it.

**Replacement:** A project that replaces the default records the lists it keeps short enough to fit.

**Rationale:** On a long expense list the header scrolls out of sight, and the reader scrolls back up to relearn which column is which. A header fixed in place keeps the names visible, but a focused row sliding under it looks lost even though focus sits on it. Keeping the header visible while scrolling, with no focused row hidden under it, answers both. This is WCAG 2.4.11 Focus Not Obscured.

**Reviewed for:** Does every built tall table keep its header in view while scrolling, with no focused row hidden under it? Scroll built long lists with the keyboard.

### Shape a List by Its Container

**ID:** `litespec/frontend/patterns/lists-and-tables#shape-a-list-by-its-container`

**Strength:** default

**Default:** Switch a list between its table and its cards by the width of the region that holds it, not the width of the screen.

**Replacement:** A project that replaces the default records the breakpoint it uses and why.

**Rationale:** A list does not always get the full screen. The expense table inside a record's side panel has a phone's width on a desktop screen, and a screen-wide breakpoint would keep it a table where cards read better. Measuring the region that holds the list switches each list at the width it actually has, so one list on a page stays a table while its neighbor turns to cards.

**Reviewed for:** Does every built list switch between table and cards by the width of its holding region? Open built lists in narrow panels on a wide screen.

## Patterns

### Choosing the Columns

**ID:** `litespec/frontend/patterns/lists-and-tables#choosing-the-columns`

**Strength:** default

**Question:** Which columns does the expenses table show? Readers tell rows apart by expense, spender, date, state, and money, and the trip name sits under the expense code as a second line.

**Default:** `five-columns-nothing-removable`

**Compact:** Below 640 pixels the table turns into two-line rows with the amount on the right, and a Sort control in the toolbar takes over from the headers. The compact option is the compact form of `five-columns-nothing-removable`.

**Rationale:** Five fixed columns cover the five questions a reader asks of a row: which expense, who spent, when, what state it is in, and how much. That is what fits at 1440 pixels without scrolling, so it stays the default. The runner-up is the Columns popover: add it once more than one team asks for a sixth fact, such as method or cost centre.

Saved views come later, when finance and the auditors want different tables of their own. Never ship the question-shaped headers: they cost width and read as a form.

**Reviewed for:** Does the built column set match the wireframe its catalog entry chose? Open the built choosers and saved views beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** Five fixed columns fit at 1440 pixels without scrolling and answer which expense, who, when, what state, and what money. On a phone the rows turn into two lines with the amount on the right, and a Sort control takes over from the headers.

**Cost:** Nothing can be dropped or added: a team that wants the trip as its own column, or that hides a column, has no way to get there. On a phone there are no headers, so sorting and comparison down a column move to a control or to the desktop.

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

**Rationale:** A Columns control opens a popover listing every column with a checkbox each, plus Reset to default. The expense column stays always shown and says so. A view-options menu or a display panel works the same way.

**Cost:** The popover covers the table while it is open. Drag-to-reorder inside it would be a second feature, and one most readers would never find.

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

**Rationale:** Each saved view carries its own columns: All expenses keeps the default five, Finance swaps in method, paid, and reversed, and Payout run shows spender, trip, and lines. Each role opens the table it needs, and a support reply can name the view.

**Cost:** Saved views are a feature with their own lifecycle: who creates one, whether views are shared, and what happens when a column retires. That is too much for a first release.

**Reference:** HubSpot, Linear

### Sorting

**ID:** `litespec/frontend/patterns/lists-and-tables#sorting`

**Strength:** default

**Question:** How does a reader reorder the expenses table? It starts newest first, each sortable header offers its own order, and the sort lives in the URL so a link carries it.

**Default:** `every-sortable-header-carries-an-arrow`

**Compact:** Below 640 pixels the table turns into two-line rows and the headers leave with it, so a Sort control in the toolbar carries the same sort. The compact option is `headers-on-a-desktop-a-sort-control-on-a-phone`.

**Rationale:** Every sortable header carries its own arrow, which states that headers sort on touch screens too. The runner-up is headers on a desktop with a Sort control on the phone list: it becomes necessary the moment the table becomes rows below 640 pixels.

Never ship multi-column sort: no finance question needs two keys.

**Reviewed for:** Do the built sort controls match the wireframe the catalog entry chose? Open the built arrows and phone sort controls beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** Each sortable header is a ghost button with a faint up-down arrow, and the sorted header gets a solid arrow. The common table examples and the dashboard tables work the same way.

**Cost:** Five arrows add ink to the header row, and the Spender header looks inert beside its sorting neighbors.

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

**Rationale:** On a wide screen the reader sorts from the headers, and on the phone list a Sort control takes over, because rows have no headers to click. One URL parameter drives both.

**Cost:** Two controls hold one state, which pays off only once the table becomes a list on a phone.

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

**Rationale:** Sorting lives in one named control, Sort newest first, instead of several header affordances. It works on a touch screen and states itself in words.

**Cost:** The reader has to find the control to change the order, and on a desktop table sorting belongs in the headers.

**Reference:** Carbon

### Filters

**ID:** `litespec/frontend/patterns/lists-and-tables#filters`

**Strength:** default

**Question:** How does a reader narrow the expenses table? Search alone answers one question, so each filter category gets its own control, a closed control shows how many values it applies, and several categories never share one dropdown.

**Default:** `one-filter-per-category-visible`

**Compact:** The default reflows: the search box keeps the top row, the four category controls wrap beneath it, and the expenses table scrolls sideways inside its region.

**Rationale:** One visible control per category shows what can narrow the table without opening anything, and each category keeps its own control. The runner-up is faceted buttons with counts: prefer it once a category holds more than a few values and the reader needs the split before choosing.

Never ship every category inside one dropdown: it hides the applied state and breaks the stated rule.

**Reviewed for:** Do the built filter controls match the wireframe the catalog entry chose? Open the built controls, panels, and facet buttons beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** Trip, state, date, and cost centre each get their own control in one row, so a reader sees what is available without opening anything and no menu holds more than one category.

**Cost:** Four controls plus a search box make a wide toolbar: below about 700 pixels they have to wrap or collapse, which is where the complexity moves.

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

**Rationale:** Each category is a button opening a checklist with a count per value, as in the tasks example. The reader sees the split before choosing, and several values in one category combine without one select per value.

**Cost:** A count per value costs a read per category, and the popover covers the table while it is open. Worth it only where a category holds more than a few values.

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

**Rationale:** Filters sit in a panel down the left side with every category open at once and nothing hidden. That shape works once there are more than three categories.

**Cost:** A permanent panel takes about 190 pixels from the table on every visit, while most visits apply no filter at all: a cost paid by everyone for the benefit of a few.

**Reference:** Carbon

### Row Density

**ID:** `litespec/frontend/patterns/lists-and-tables#row-density`

**Strength:** default

**Question:** How tall is a row in the expenses table? Five row heights from 24 to 64 pixels are common, the header, toolbar, and pagination match the row size, and the default table runs near 47 pixels per row.

**Default:** `40-pixels-the-default`

**Compact:** Below 640 pixels the table turns into two-line record rows with the amount on the right, and sorting and comparison down a column move to the desktop. The compact option is `a-record-list-instead-of-a-table`.

**Rationale:** Stay at 40 pixels: two lines fit, the row scans, and it matches the default size. The runner-up is 32 pixels, for a working queue where every row is read once and acted on.

Never ship a density control the reader sets: most readers set it once, and a screenshot suite then holds three versions of every page. The phone answer is a record list.

**Reviewed for:** Does the built row height match the wireframe the catalog entry chose? Open the built 24, 32, and 40 pixel rows beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The medium default height: two lines of text fit and the row scans.

**Cost:** Twenty-five rows take 1000 pixels, so 268 rows fill eleven screens: fine for browsing, slow for counting.

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

**Rationale:** The extra-small height puts one line per row, so the eye travels straight down a column. It fits a queue a finance reader works through rather than reads.

**Cost:** One line leaves nowhere for the trip name and the spender email: both have to be dropped or opened from the row.

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

**Rationale:** Each record gets a card-width block with two lines and its own commands on the right: the shape the phone layout wants.

**Cost:** No columns means no sorting and no comparison down a column: every table capability traded for reading one record well.

### Bulk Action Bar

**ID:** `litespec/frontend/patterns/lists-and-tables#bulk-action-bar`

**Strength:** default

**Question:** What appears once expense rows are selected? The bar names the count and the money, then offers the commands: a reversal on the wrong scope is the expensive mistake this pattern guards against.

**Default:** `a-bar-above-the-table-naming-the-count`

**Compact:** The default reflows: the selection count and money stack above the table, with the reverse commands wrapping under them.

**Rationale:** A bar above the table names the count and the money, and on a reversals page that total is the thing a reader checks before acting. The runner-up is a floating pill for reversible bulk work, such as resending receipts or tagging, where the pill leaves the table in place.

Never ship the bar at the foot: on a long table the action sits off screen while the checkboxes that enabled it do not.

**Reviewed for:** Does the built selection bar match the wireframe the catalog entry chose? Open the built count bars, pills, and dialogs beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The bar states how many rows are selected and what that total is worth, then offers the commands. The count is what a reader checks before acting on money.

**Cost:** The toolbar stays visible above the bar, so two rows of controls stack: either the toolbar goes or the page gets taller.

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

**Rationale:** The bar floats over the content instead of pushing the table down, holding the count, the one or two common commands, and a menu for the rest. The table never shifts when the first box is ticked.

**Cost:** The pill covers the bottom rows while it is up, and it has no room to state a money total. Keep it for reversible work.

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

**Rationale:** The primary button opens a dialog naming the count and the money. A bulk reversal is rare and irreversible, which is the kind of action that needs an interruption.

**Cost:** Two steps for a batch the reader spent a minute selecting: acceptable on money, wrong for anything reversible such as a bulk tag.

**Reference:** NN/g

### Pagination

**ID:** `litespec/frontend/patterns/lists-and-tables#pagination`

**Strength:** default

**Question:** How does a reader move through 268 expenses? The range and two chevrons move one page at a time, numbered pages add a jump, and the count still says where the reader is.

**Default:** `count-left-numbered-pages-right`

**Compact:** Below 640 pixels the numbered pages leave, and the pager keeps the range with two chevrons. The compact option is `a-compact-pager-range-plus-two-chevrons`.

**Rationale:** The count with numbered pages lets the reader jump while the range still says where they are. The runner-up is the compact pager, for tight widths where nobody jumps further than one page at a time.

Never ship the jump field: typing a page number and pressing Enter is slower than clicking for almost every reader.

**Reviewed for:** Does the built page control match the wireframe the catalog entry chose? Open the built chevrons, load-more buttons, and compact pagers beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The default pager with numbered pages added and no redundant page label: the reader can jump, and the count still says where they are.

**Cost:** Seven page numbers plus two chevrons make a wide control that has to collapse on a phone, and the count and the numbers still overlap slightly.

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

**Rationale:** The range and two chevrons, nothing else: it fits a phone without collapsing, and on a 268-row list most readers move one page at a time anyway.

**Cost:** No jumping, so reaching page 9 costs eight clicks. Only right where the reader rarely leaves the first pages.

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

**Rationale:** Infinite scroll breaks keyboard users, so loading stays a button the reader presses, and the count stays visible so the total is never a surprise.

**Cost:** A link to a specific page no longer exists, and that link is the main reason admin lists land on pagination.

**Reference:** GOV.UK, Carbon

### Row Actions

**ID:** `litespec/frontend/patterns/lists-and-tables#row-actions`

**Strength:** default

**Question:** How many commands sit on each expense row, and where do the rest live? Each row may carry its own commands, but a row has room for few.

**Default:** `one-inline-action-the-rest-in-a-row-menu`

**Compact:** Below 640 pixels the list turns into one card per expense with its commands on the card. The compact option is the compact form of `one-inline-action-the-rest-in-a-row-menu`.

**Rationale:** One inline command keeps the common job one click away while the row stays narrow, and the rest live in the row menu. Prefer the side preview where acting needs the record context, such as the lines before a resend. Never ship hover-revealed actions: hover fails on touch, and a second focusable render doubles the table for no gain.

**Reviewed for:** Do the built row commands match the wireframe the catalog entry chose? Open the built inline actions, row menus, and side previews beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The first row command sits inline and everything else hides behind the row's overflow menu. On a phone the list turns into cards with the commands on each card, because the phone is for acting on one expense.

**Cost:** The second command stays invisible, and the overflow is a 32-pixel target to find in every row. Cards run tall: three expenses fill the screen where rows fit six.

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

**Rationale:** A row opens in a side panel carrying the record's facts and its commands together. Resending Ava's receipt happens beside the expense lines, so the reader checks what is sent before sending it.

**Cost:** A panel over the list hides the rows it came from, and on a phone it fills the screen, which makes it a page by another name.

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

**Rationale:** No commands on the row at all. Clicking the expense number opens the record and every command lives there: the table is for finding, the record is for acting.

**Cost:** Bulk work becomes one row at a time, so this shape fits finding, not acting.

### Table's Own States

**ID:** `litespec/frontend/patterns/lists-and-tables#tables-own-states`

**Strength:** default

**Question:** What shows for each state of the table itself? The expenses table is sometimes empty, still loading, or failed to load, and each case needs its own words and layout.

**Default:** `no-data-yet-table-replaced`

**Compact:** The default reflows: the message and its New trip command stack in one column under the header.

**Rationale:** Each state gets its own sentence for its own fact: no data yet replaces the table, a failed refresh keeps the chrome and names how old the rows are, and a slow read names what is loading. Never ship a bare spinner over the table body: rows that shift when the data lands move the row the reader already chose.

**Reviewed for:** Do the built table states match the wireframe the catalog entry chose? Open the built no-data, failed-read, and slow-read states beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** Nothing has ever existed, so the table and its headers are gone, and one positive sentence offers the command that creates the first record.

**Cost:** The reader cannot see what a row will look like: the columns stay undiscoverable until there is data.

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

**Rationale:** The table rendered before and a refresh failed, so the headers stay and the error shows in place: the reader sees the rows are stale rather than current.

**Cost:** Old rows under an error can be acted on by mistake, which is the opposite of what the message wants.

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

**Rationale:** A slow read names itself while it runs: what is loading, how long it has taken, and a way to stop waiting. A reader who knows the list holds 268 expenses waits, while a reader watching a blank box leaves.

**Cost:** A progress note needs the total before the rows arrive, which costs a second read. It also teaches the reader that some lists are slow, which invites the question why.

**Reference:** Vercel

### Status in a Column

**ID:** `litespec/frontend/patterns/lists-and-tables#status-in-a-column`

**Strength:** default

**Question:** How does the State column tell apart the states that need a decision from those that need none? Each expense row carries a state such as Paid or Disputed.

**Default:** `tone-only-on-the-states-that-block`

**Compact:** The default reflows: the State column keeps its toned badges while the table scrolls sideways, so Disputed and Awaiting payment still read at a glance.

**Rationale:** Only the states that block get a tone: Disputed and Awaiting payment take a badge because somebody must act, while Paid and Completed stay plain words. Prefer the reason on a second line for a disputes queue, where the word alone never answers why. Never ship progress marks: four blocks per row on a 268-row list cannot serve a lifecycle that branches.

**Reviewed for:** Do the built state badges match the wireframe the catalog entry chose? Open the built tones, badges, and reason lines beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** States that need a decision get a tone and the rest stay plain words, which keeps the table readable.

**Cost:** A reader has to learn which states carry a tone, and a new state added later has to be classified before it is used.

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

**Rationale:** The state pairs with its reason: Disputed with why Ben asked, Awaiting payment with how old the wait is. The word says what, the line says why, and the reader never opens a row to learn the reason.

**Cost:** Two lines double the row height for every row, including paid rows whose reason is nothing. Only right on a queue where most rows need a decision.

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

**Rationale:** Every state becomes a badge mapping a code to a word and a tone. The word is the signal and the tone repeats it.

**Cost:** More than five or six status indicators overwhelm the eye, and a state needing no action reads better as plain text. A badge on every row is the opposite of that.

**Reference:** Carbon

### Money Column

**ID:** `litespec/frontend/patterns/lists-and-tables#money-column`

**Strength:** default

**Question:** What does the money cell say beyond the number? Each expense row shows what the spender was paid, and each cell shows the currency with the figure.

**Default:** `number-only-right-aligned`

**Compact:** The default reflows: the Paid column keeps its right-aligned figures while the table scrolls sideways, with each cell still showing the currency.

**Rationale:** Keep the number only. Each cell shows the currency with the figure, so an export or a screenshot never leaves finance guessing which money it shows. Prefer the reversed part under the charged amount where reversals are the work rather than the exception. Never ship money as a share of the total: a derived percentage column answers a question no expenses list is asked.

**Reviewed for:** Does the built amount column match the wireframe the catalog entry chose? Open the built aligned figures and reversal columns beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The default shape. Each cell shows the currency with the figure, so an export or a screenshot never leaves finance guessing which money it shows.

**Cost:** EUR 0.00 reads as an error rather than a fact, so a zero row needs words saying why it is zero.

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

**Rationale:** One money column keeps the charged amount with what went back on its second line: the charge, then what returned from the spender. The column stays scannable and the reversal stays on the row.

**Cost:** Two lines double the row height, and a part reversal needs a third fact, the net, which has nowhere to go in this cell.

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

**Rationale:** Where reversals are common the net matters more than the gross, and a second column turns a negative into a fact instead of something the reader reconstructs.

**Cost:** Two money columns on a five-column table are a lot of numbers, and most rows read identical in both: the reader scans two equal columns to learn nothing.

### Exporting the List

**ID:** `litespec/frontend/patterns/lists-and-tables#exporting-the-list`

**Strength:** default

**Question:** Which export does the list offer? A download serves one spreadsheet now, while a schedule serves a repeated question.

**Default:** `one-export-button-that-downloads-csv`

**Compact:** The default reflows: the Export CSV command sits beside the search above the table, which scrolls sideways inside its region.

**Rationale:** One Export button downloads the filtered rows as CSV, so the accountant's job costs one click and no decisions. Prefer a background export past a few thousand rows, where a direct download would time out. Never ship copy as the only export: copying the visible page silently drops every row past the pager.

**Reviewed for:** Does the built download flow match the wireframe the catalog entry chose? Open the built CSV buttons, dialogs, and background exports beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** One button in the toolbar downloads exactly the rows in the current filter as CSV. The accountant gets the Rotterdam client visit expenses with one click and no decisions.

**Cost:** No choice of columns or format: a reader who wants only expense number and paid, or an Excel file, trims or converts it elsewhere.

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

**Rationale:** A large file prepares in the background while the reader keeps working, then email delivers a download link. Past a few thousand rows a direct download times out, and a progress note beats a failed one.

**Cost:** The file arrives later, so the reader cannot reconcile against it right away, and the export needs its own failure state for when the job dies overnight.

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

**Rationale:** The team sets a rhythm once and finance receives the expenses without opening the product: a Monday 07:00 email answers the weekly question before anyone asks it.

**Cost:** A schedule is invisible state that nobody sees on the expenses page, so a stale recipient keeps receiving figures until someone remembers the schedule exists.

### Searching with Highlighted Matches

**ID:** `litespec/frontend/patterns/lists-and-tables#searching-with-highlighted-matches`

**Strength:** default

**Question:** How does the list show why each row matched? A search box filters the expenses table, but a match on an email looks the same as a match on a name.

**Default:** `matches-highlighted-in-the-cells`

**Compact:** The default reflows: the match count stays under the search box, and the table with its highlighted cells scrolls sideways.

**Rationale:** Highlight the matches in the cells, so the reader sees why each row matched without opening it. Prefer a scope control once readers search across trips and must say which one. Never ship a match column on its own: it spends a column stating what the highlight already shows.

**Reviewed for:** Does the built list search match the wireframe the catalog entry chose? Open the built highlighted cells and scoped searches beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The matched text marks itself where it sits. The term stays visible in the box and every match reads clear in its cell, so the reader trusts the filter.

**Cost:** Highlighting needs the match offsets from the server, not the rows alone: a client-side guess highlights the wrong occurrence on long text.

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

**Rationale:** One control beside the box scopes the search to the current view or the whole company. A reader inside the Rotterdam client visit searches it first and widens only when nothing matches.

**Cost:** A scope turns invisible once set: a reader who narrowed to one trip last week searches it again today and wonders where the rest went.

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

**Rationale:** The list says when a search is still running instead of showing yesterday's rows as if they matched. The reader waits instead of acting on a stale list.

**Cost:** A visible search state needs debouncing and canceling: without them every keystroke queues a request and the indicator flickers.

**Reference:** Stripe

### Grouped Rows

**ID:** `litespec/frontend/patterns/lists-and-tables#grouped-rows`

**Strength:** default

**Question:** Some questions are asked per group, not per row: what did today pay, what does each trip hold? A flat table answers them only after an export, while a group header with its own subtotal answers them on the page.

**Default:** `grouped-by-day-with-subtotals`

**Compact:** The default reflows: each day keeps its subtotal above its table, and the day groups stack with their tables scrolling sideways.

**Rationale:** Grouping by day with subtotals answers what today paid without a chart and without leaving the list. The runner-up is grouping by trip with collapse toggles: prefer it once trips grow long enough that one day of rows stops fitting. Collapsed rows stay findable, because the browser's find opens the group that holds a match. Never ship two group levels: month-then-day nesting turns the list into a tree that paging cannot cross.

**Reviewed for:** Do the built group headers match the wireframe the catalog entry chose? Open the built day, trip, and flat groupings beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** Like bank statements and payout lists, money groups by day with a subtotal per group. The reader sees the shape of the spending and still opens any row.

**Cost:** Without a flat row order, pagination and select-all both get harder to reason about across group edges.

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

**Rationale:** A toggle on each group header lets a reader following one trip collapse the rest. The header carries the count and the subtotal even while collapsed.

**Cost:** Collapsed groups hide rows from a reader who forgot they collapsed anything, though the browser's find still opens the group that holds a match. The toggle state has to survive a reload or it annoys.

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

**Rationale:** The table stays flat and the reader sorts by the would-be group field instead. Nothing hides, paging stays simple, and the eye groups adjacent equal values on its own.

**Cost:** No subtotals, so a reader who wants today's takings sorts by date and adds the rows up by hand. Sorting is not grouping; it only looks like it.

**Reference:** Carbon

### Inline Row Expansion

**ID:** `litespec/frontend/patterns/lists-and-tables#inline-row-expansion`

**Strength:** default

**Question:** How much detail opens in place under the row? Some rows hold a second level: an expense holds its lines, its receipts, its history. Opening the record for each one costs a page load per row, while an expansion row under the row itself answers the question where it was asked.

**Default:** `expand-to-the-order-lines`

**Compact:** The default reflows: the expense lines card stacks under its table, with the Collapse command first.

**Rationale:** Expansion shows the expense lines: what the spender claimed, which is the question asked from the list. The runner-up adds decision state to the lines; prefer it on the decision list, where handover is the work. Never ship expand-all: twenty expanded rows make a page nobody can scan, and one click fires a read per row.

**Reviewed for:** Do the built expandable rows match the wireframe the catalog entry chose? Open the built expense lines and decision states beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** A row expands to its lines: what was claimed, at what price, adding to the row total. Support answers what was paid for without leaving the list.

**Cost:** The detail row breaks the column rhythm, and sorting reorders parents with children following, which confuses a reader who sorted by money.

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

**Rationale:** An expense on the decision list expands to its lines with what is decided and by whom. Ben works the list between meetings instead of opening forty records.

**Cost:** Decision state changes by the minute at busy times, so an expanded row goes stale while open. It needs the same refresh the decision screen has.

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

**Rationale:** The expense number is a link and everything lives on the record page: one pattern for detail everywhere, and the list stays a list.

**Cost:** Every question costs a page load and a back button: checking three expenses means six page loads where expansion needs none.

### Pinned and Resizing Columns

**ID:** `litespec/frontend/patterns/lists-and-tables#pinned-and-resizing-columns`

**Strength:** default

**Question:** How does a wide table keep its bearings while it scrolls? The expense code leaves as the money arrives. Pinning the identity column keeps the row named, and resizing lets the reader spend width where their own data needs it.

**Default:** `the-order-column-pinned-while-the-rest-scroll`

**Compact:** The default reflows: the expense column stays pinned while the other columns scroll under it.

**Rationale:** The expense column stays pinned while the rest scroll: the row stays named at any scroll position, and it costs no control. The runner-up pins the actions too; prefer it once rows carry inline commands that must stay reachable. Never ship draggable widths in a first release: per-user widths are stored state that support cannot see and screenshots cannot hold.

**Reviewed for:** Do the built fixed columns match the wireframe the catalog entry chose? Open the built frozen first columns beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The expense column stays pinned while the other columns scroll under it. Nothing folds, so every fact keeps its own scannable column.

**Cost:** On a phone the reader sees the expense code and one other column at a time: pinning keeps the bearings, not the overview.

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

**Rationale:** The record name pins on the left and the row commands on the right, so both survive any scroll position and acting never requires scrolling back.

**Cost:** Two pinned columns on a phone leave no room for the middle at all: the pattern needs a wide screen to earn its keep.

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

**Rationale:** The table scrolls as one piece and every column leaves together: no sticky edges, no shadows, no special cases in the layout.

**Cost:** Halfway across, the money column arrives and the expense code is gone, so the reader scrolls back to check which row the amount belongs to.

**Reference:** Carbon

### The Layout of a Short List

**ID:** `litespec/frontend/patterns/lists-and-tables#the-layout-of-a-short-list`

**Strength:** default

**Question:** How should a short, dated list read: as table rows or as records? A list such as trips holds a handful of records and asks how each one is doing. A table answers with rows of numbers, while a card or a date-led list answers with the record itself: its dates, its place, and its progress.

**Default:** `trip-cards-with-a-budget-bar`

**Compact:** The default reflows: the trip cards stack in one column, each keeping its dates, state, and claimed figure.

**Rationale:** Cards with a progress bar: a handful of trips reads better as cards than as rows, and the bar answers the only question the list exists for. The runner-up is a date-led list; prefer it once the year holds dozens of trips and cards stop fitting. Never ship a month calendar for this question: it answers when, which the list is never asked.

**Reviewed for:** Does the built short-list layout match the wireframe the catalog entry chose? Open the built cards, table rows, and date-led lists beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** Each trip is a card with its dates, its client site, and how much of its budget is claimed. Four trips a month read as four trips, not four rows of a table.

**Cost:** Cards stop scanning past a dozen trips, with no column to sort and no state to scan down. The pattern fits a handful, not a hundred.

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

**Rationale:** The table stays and each row gains a budget bar, as in a product list. The trips stay sortable and comparable, and the bar still answers how much of the budget each one has claimed.

**Cost:** A bar in a table row is a chart without an axis: 80 percent of EUR 400.00 and 53 percent of EUR 600.00 read as two bars of different lengths for different totals.

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

**Rationale:** A schedule view leads with the date and groups by month. A planner reads October, then December, then March, each trip under its own day.

**Cost:** Dates lead and states follow, so finding the one draft means reading every month: the view answers when, not what needs work.

**Reference:** Google Calendar
