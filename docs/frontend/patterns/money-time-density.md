# Money, Time and Density Patterns

**Stack:** any

**Concern:** frontend-ui

This page holds the money, time, and density patterns: how amounts, statements, moments, periods, counts, charts, payouts, invoices, VAT, and identifiers read in tables, on statements and charts, in exports, and at phone width.

A team building a finance or reporting screen starts from these patterns. The owner chooses an option from the wireframe drawings, and the team records the choice in its pattern catalog, the file holding the project's interface decisions. One shared form keeps screens, statements, and exports in agreement, so finance can cite a figure without checking where it was read.

## Concepts

### How to Read a Pattern

Each pattern answers one recurring question about money, time, or density, such as how an amount reads in a table. It names the question and the situation, the default option a project builds unless its catalog records another, how the default behaves on a narrow screen, and why the default was chosen over the other options.

Each option shows one wireframe drawing with its own rationale, its cost, and the outside product it learns from, where one exists. A project records its choice in its pattern catalog and builds the chosen option with its own components. The [UI patterns](../patterns.md) page states the catalog contract and the wireframe language.

### The Running Example

Every drawing on this page comes from one month: the Claimwick September 2026 statement. It covers expense EXP-1042 for Ava at EUR 125.00, the payout run holding it, and the autumn travel season. Amounts use currency codes with the decimals each currency defines, moments use the Europe/Amsterdam zone, and counts name what they count.

Three rules hold for every option, whichever one a project picks, and the Standards section states them: every amount shows its currency, a moment shared across zones states its zone, and figures in a column line up by place.

## Standards

### Show the Currency of Every Amount

**ID:** `litespec/frontend/patterns/money-time-density#show-the-currency-of-every-amount`

**Strength:** required

**Statement:** Every amount MUST show its currency.

**Rationale:** A bare 125.00 reads clearly until a second currency arrives. Then each bare figure is a guess, and a pasted number carries nothing to settle it. A currency on every amount keeps EUR 125.00 distinct from GBP 125.00 on every screen and export.

**Reviewed for:** Does every built figure show its currency? Open the amounts on built tables, statements, and charts.

### State the Time Zone of a Shared Moment

**ID:** `litespec/frontend/patterns/money-time-density#state-the-time-zone-of-a-shared-moment`

**Strength:** required

**Statement:** A moment shown to readers in different time zones MUST state its time zone.

**Rationale:** Without a stated zone, a reader in London reads 09:32 in Amsterdam as 09:32 local time. Two readers then plan the same payout window an hour apart, and finance meets only one of them. A stated zone keeps the moment the same for every reader.

**Reviewed for:** Does every built shared moment state its zone? Open the dates and times on built screens read across zones.

### Align Figures in a Column

**ID:** `litespec/frontend/patterns/money-time-density#align-figures-in-a-column`

**Strength:** required

**Statement:** An amount, count, or quantity shown in a column MUST use tabular figures, so digits of equal place sit under each other.

**Rationale:** Proportional figures give each digit its own width, so digits in equal places stop lining up down a column. A misaligned reversal column is misread at a glance, and the wrong row gets paid. Tabular figures give every digit one width, so hundreds sit under hundreds down the whole column.

**Reviewed for:** Does every built figure column line up by place? Open built amount, count, and quantity columns and read down each one.

## Conventions

### Mark a Moment with Its Machine Value

**ID:** `litespec/frontend/patterns/money-time-density#mark-a-moment-with-its-machine-value`

**Strength:** default

**Default:** Render a date or moment in a time element that carries its ISO 8601 value.

**Replacement:** A project that replaces the default records where its dates render as plain text.

**Rationale:** People write one moment many ways: 8 Oct, October 8, next Tuesday. Assistive technology and the browser read the exact value from the machine form, not from the words the reader sees. The ISO 8601 value in the time element keeps the cited moment exact even where the words stay short.

**Reviewed for:** Does every built date or moment carry its machine value? Open built dates and read each time element for its ISO value.

## Patterns

### Writing an Amount

**ID:** `litespec/frontend/patterns/money-time-density#writing-an-amount`

**Strength:** default

**Question:** A reader meets amounts in tables, statements, and exports. One table writes EUR 45.00 while a statement writes the euro sign before 45.00. The question is which form the product keeps, and it also asks how a negative reads and which separators the product uses.

**Default:** `code-and-the-currency-s-own-decimals`

**Compact:** On a narrow screen the amount column keeps the code with each figure and the true minus on reversals, while the table scrolls sideways.

**Rationale:** Ship the code format in the reader's language with a true minus. One format settles decimals, negatives, and separators at once: each currency shows the decimals it defines. The runner-up is the plain code table, which shows the same choice without reversal rows. The entry point names the option its screens render in `formats.money`.

Choose the symbol with the code in the header only when the finance screens and the spender screens show the same form. Never ship the no-currency option: it loses the currency in exports. Never ship mixed decimals: they break column alignment.

**Reviewed for:** Does the built money text match the wireframe its catalog entry chose? Open the built codes, symbols, and red negatives beside the catalog entry recording the chosen option and its reason.

#### `code-and-decimals-with-the-column-totals-at-the-foot`: Code and Decimals, with the Column Totals at the Foot

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses from 96 spenders."
    action "Download"
  field "Expense number, name or email" search
  region "Expenses"
    table 4 "Expense" "Spender" "Submitted" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 2026, 09:32" · status "Paid" positive · text "EUR 125.00"
  region "Totals"
    fact "Total on this page" "EUR 3,112.50"
    fact "Total of every match" "EUR 33,480.00"
```

**Rationale:** This option keeps the code convention and adds two totals: one for the page and one for every match. Both totals follow the filters, so the reader checks the column without exporting it.

**Cost:** The two totals disagree by design, so their labels must stay clear at a glance. The all-match total adds a second read on every filter change.

#### `code-and-the-currency-s-own-decimals`: Code and the Currency's Own Decimals, in the Reader's Language, with a True Minus

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses from 96 spenders."
    action "Download"
  field "Language" select = "English"
  region "Expenses"
    table 5 "Expense" "Spender" "Submitted" "State" "Amount"
      item: text "EXP-1030" · text "Tom Becker" · text "8 Oct 2026, 09:12" · status "Reversed" neutral · text "−EUR 50.00"
  region "Note"
    text muted "A Dutch page writes EUR 125,00. A reversal uses a true minus."
```

**Rationale:** Stripe dashboard and Shopify admin keep one amount format, and this option follows them. The reader's language decides the separators: English shows EUR 1,203.65 where Dutch shows EUR 1.203,65. Each currency shows the decimals it defines, none for JPY and three for KWD. A reversal uses a true minus before the code, which lines up with digits in a tabular column.

**Cost:** Two colleagues on the same account see different separators, so a screenshot in a support case has to be read in its own language. The minus is the only signal of money out, so the State column has to say Reversed as well.

**Reference:** Stripe dashboard, Shopify admin

#### `money-out-in-red-with-a-true-minus`: Money Out in Red with a True Minus

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses from 96 spenders."
    action "Download"
  field "Expense number, name or email" search
  region "Expenses"
    table 4 "Expense" "Spender" "Submitted" "State" "Amount"
      item: text "EXP-1030" · text "Tom Becker" · text "8 Oct 2026, 09:12" · status "Reversed" neutral · status "−EUR 50.00" critical
```

**Rationale:** Stripe dashboard writes a refund in red with its minus, and this option follows it. Money out is visible at a glance, and the minus keeps the meaning for a reader who cannot see the color.

**Cost:** Red is also the error tone, so a statement of reversals reads as a page of problems. The State column still says Reversed, because color alone never carries the meaning.

**Reference:** Stripe dashboard

### Statement That Adds Up

**ID:** `litespec/frontend/patterns/money-time-density#statement-that-adds-up`

**Strength:** default

**Question:** A statement has one purpose: money adds up from top to bottom. Each kind forms a group with a subtotal, a grand total closes the statement at the foot, and a count under each line helps a reader spot a missing line.

**Default:** `groups-subtotals-and-a-grand-total`

**Compact:** On a narrow screen the default option stacks the groups, with each subtotal above the grand total, and every figure keeps its currency.

**Rationale:** Keep groups, subtotals, and a grand total, with a count under each line. The count is the only completeness check. The runner-up ends in the payouts the statement became, which fits finance checking the statement against bank lines.

Add links on every line later, to whichever option the project picks. Never ship the one-figure option as the statement: it hides how the figure was reached.

**Reviewed for:** Does the built totals page match the wireframe its catalog entry chose? Open the built groups, subtotals, and payout endings beside the catalog entry recording the chosen option and its reason.

#### `groups-subtotals-and-a-grand-total`: Groups, Subtotals and a Grand Total

```wireframe
page
  breadcrumb "Home" "Reports" "Statements" "September 2026"
  header
    heading "September 2026"
    text "Every euro of the month under the kind it is."
    action "Download"
  region "Spending"
    fact "Travel, 1,204 lines" "EUR 54,180.00"
    fact "Meals, 1,204 lines" "EUR 6,020.00"
    fact "Lodging, 126 nights" "EUR 3,150.00"
    fact "Total claimed" "EUR 63,350.00"
  region "Reversals and fees"
    fact "Reversals, 31 lines" "−EUR 1,550.00"
    fact "Booking fee" "−EUR 332.92"
    fact "Net paid out" "EUR 61,346.68"
```

**Rationale:** Each group holds its lines and a subtotal, and a rule separates the grand total at the foot. The count under each line shows a missing line.

**Cost:** The count repeats 1,204 twice, and a reader still opens the expenses list to find which expenses a line holds.

#### `a-statement-with-no-count-on-any-line`: A Statement with No Count on Any Line

```wireframe
page
  breadcrumb "Home" "Reports" "Statements" "September 2026"
  header
    heading "September 2026"
    text "Every euro of the month under the kind it is."
    action "Download"
  region "Spending"
    fact "Travel" "EUR 54,180.00"
    fact "Meals" "EUR 6,020.00"
    fact "Lodging" "EUR 3,150.00"
    fact "Total claimed" "EUR 63,350.00"
  region "Reversals and fees"
    fact "Reversals" "−EUR 1,550.00"
    fact "Booking fee" "−EUR 332.92"
    fact "Net paid out" "EUR 61,346.68"
```

**Rationale:** This option drops the count under each line, the way the Shopify finance summary reads. Each line is one row and the page is shorter.

**Cost:** A reader cannot check for a missing payment, because the count is the only completeness check.

**Reference:** Shopify

#### `a-statement-that-ends-in-the-payouts-it-became`: A Statement That Ends in the Payouts It Became

```wireframe
page
  breadcrumb "Home" "Reports" "Statements" "September 2026"
  header
    heading "September 2026"
    text "What the month spent, and the payouts sent for it."
    action "Download"
  region "Net for the month"
    fact "Spending" "EUR 63,350.00"
    fact "Reversals" "−EUR 1,550.00"
    fact "Fees taken at each payment" "−EUR 453.32"
    fact "Net for the month" "EUR 61,346.68"
  region "Paid out to NL91 ABNA 0417 1643"
    fact "Mon 7 Sep 2026" "EUR 11,420.36"
    fact "Mon 14 Sep 2026" "EUR 16,284.10"
    fact "Total paid out" "EUR 61,346.68"
    status "Reconciled" positive
    fact "Difference" "EUR 0.00"
```

**Rationale:** The net closes into the payouts sent to the bank. Finance checks each payout against a bank line, and the difference reads zero for a closed month.

**Cost:** A payout can span two months, so the difference is zero only for a closed month. The page explains that edge in one line.

**Reference:** Stripe

### Writing a Moment

**ID:** `litespec/frontend/patterns/money-time-density#writing-a-moment`

**Strength:** default

**Question:** Every date uses the zone the screen works in, and each table keeps one form: the expenses table writes 8 Oct 2026, 09:32. A messages table that mixes Sent 6 hours ago with For 2 Jan 2030, 10:00 in one column breaks that rule.

**Default:** `one-form-per-table-with-the-zone-stated-once`

**Compact:** On a narrow screen the default option keeps the Submitted column in its single form, with the zone still named once in the header.

**Rationale:** Keep one form per table and state the zone once in the header, so a column reads as a column. The entry point names the option its screens render in `formats.moment`.

The runner-up puts absolute over relative, which fits dispute and payout surfaces where recency leads. Never ship relative-only dates on a reconciled page: a relative date gives the reader nothing exact to cite.

**Reviewed for:** Does the built date text match the wireframe its catalog entry chose? Open the built absolute, relative, and weekday forms beside the catalog entry recording the chosen option and its reason.

#### `one-form-per-table-with-the-zone-stated-once`: One Form per Table, with the Zone Stated Once

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses from 96 spenders."
    action "Download"
  field "Expense number, name or email" search
  region "Expenses"
    table 4 "Expense" "Spender" "Submitted Europe/Amsterdam" "Paid"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 2026, 09:32" · text "EUR 125.00"
```

**Rationale:** Every row keeps the same shape and the header names the zone once, so a column reads as a column.

**Cost:** Minutes add noise once the week has passed. A reader cannot tell which zone an expense was filed in.

#### `grouped-by-day-so-the-column-is-the-day`: Grouped by Day, so the Column Is the Day

```wireframe
page
  breadcrumb "Home" "Expenses" "Timeline"
  header
    heading "Everything that happened"
    text "Newest first. All times Europe/Amsterdam."
  region "Today, 8 October 2026"
    list 4
      item: text "EXP-1042" · text "Ava filed an expense" · text "09:32" · text "EUR 125.00"
  region "Yesterday, 7 October 2026"
    list 2
      item: text "EXP-1031" · text "Ade Okafor filed an expense" · text "19:02" · text "EUR 45.00"
```

**Rationale:** A list of events takes the day as its shape: the date becomes a heading instead of a column, and each row keeps what matters.

**Cost:** It is no longer a table, so it is no longer sortable as a column, and sorting moves to the toolbar. Each group heading adds another row to read.

#### `a-time-with-the-zone-on-every-value`: A Time with the Zone on Every Value

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses from 3 offices in 3 time zones."
    action "Download"
  field "Expense number, name or email" search
  region "Expenses"
    table 4 "Expense" "Spender" "Submitted" "Paid"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 2026, 09:32 CEST" · text "EUR 125.00"
  region "Note"
    text muted "Each trip uses its own zone. Your zone is Europe/Amsterdam."
```

**Rationale:** Every moment carries its own zone. Expenses on one page come from offices in different zones, and a trip in another zone is a real case.

**Cost:** A column of repeated zones is hard to read. Stating the zone once instead reads wrong for rows from another zone.

### Choosing a Period

**ID:** `litespec/frontend/patterns/money-time-density#choosing-a-period`

**Strength:** default

**Question:** A statements page can put the period in the heading. Two plain buttons beside it walk months, but nothing says which is previous and which is next, and the period cannot be typed. The question is how the reader walks, jumps, and types a period.

**Default:** `chevrons-either-side-of-the-period-as-the-heading`

**Compact:** On a narrow screen the default option keeps the chevrons on the period heading with the month control under it, and the spending facts stack below.

**Rationale:** Ship chevrons beside the heading with a month dropdown: walking a month is one click, and jumping avoids twelve clicks. The runner-up uses presets with typeable bounds, which fits charts and exports where the period is not a month. Never ship the period as a filter chip: the period is the subject, not a refinement.

**Reviewed for:** Does the built period control match the wireframe its catalog entry chose? Open the built chevrons, preset ranges, and filter chips beside the catalog entry recording the chosen option and its reason.

#### `chevrons-either-side-of-the-period-as-the-heading`: Chevrons Either Side of the Period as the Heading

```wireframe
page
  breadcrumb "Home" "Reports" "Statements"
  header
    heading "September 2026"
    text "Every euro of the month under the kind it is."
    action quiet "‹ Previous"
    action quiet "Next ›"
  field "Month" select = "September 2026"
  region "Spending"
    fact "Travel and lodging" "EUR 57,330.00"
    fact "Meals" "EUR 6,020.00"
    fact "Total claimed" "EUR 63,350.00"
  region "Fees"
    fact "Booking fee" "−EUR 332.92"
    fact "Net paid out" "EUR 62,896.68"
```

**Rationale:** The period is the heading, with a chevron on each side to walk months. A chevron cannot read as a tab.

**Cost:** Walking a quarter still takes four clicks, so a dropdown to jump stays beside the chevrons.

#### `a-range-with-presets-and-typeable-bounds`: A Range with Presets and Typeable Bounds

```wireframe
page
  breadcrumb "Home" "Reports" "Spending"
  header
    heading "Spending"
    text "Every euro that moved, by the day it moved."
  region "Period"
    action "Last 7 days"
    action "Last 30 days"
    action "Custom range"
    field "From" = "09/10/2026"
    field "To" = "08/11/2026"
    action "Apply"
    text muted "Showing 9 October to 8 November 2026. All 30 days."
  region "Daily"
    image "Spending per day, line chart"
    text muted "Peak 13 Oct, EUR 12,400.00."
```

**Rationale:** Where the period is not a month, a range control fits: it offers presets and two typeable dates. Carbon asks for a simple way to type. Nothing sits behind the calendar.

**Cost:** Presets plus bounds are two mechanisms for one period. The used preset needs displaying afterwards, or a reader loses track of which period is showing.

**Reference:** Carbon

#### `the-period-as-a-filter-chip-in-the-toolbar`: The Period as a Filter Chip, in the Toolbar

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses."
  field "Expense number, name or email" search
  region "Filters"
    status "Submitted in September 2026" neutral
    status "Paid" neutral
    action quiet "Clear both"
    text "1,148 expenses submitted in September 2026 and paid."
  region "Expenses"
    table 3 "Expense" "Spender" "Submitted" "Paid"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 2026, 09:32" · text "EUR 125.00"
```

**Rationale:** The period sits as one filter among several, with the other narrowing choices, and removes like any of them.

**Cost:** The period is the page subject, not a refinement, so a filter row demotes the one thing the report covers.

### How Much Fits Before It Breaks

**ID:** `litespec/frontend/patterns/money-time-density#how-much-fits-before-it-breaks`

**Strength:** default

**Question:** A table can run at 47 pixels a row with three columns holding two lines each. At 390 pixels a five-column table cannot keep every column: it either drops one, truncates names to Rotterdam with an ellipsis, or changes shape. The question is what the page does at that width.

**Default:** `the-full-width-with-nothing-truncated`

**Compact:** Under 640 pixels the table becomes a record list: one block per expense holds spender, trip, and amount, newest first, and sorting by column goes with the table. The compact option is `at-390-pixels-a-record-list-rather-than-a-table`.

**Rationale:** Keep the table at desktop width with two-line cells and no truncation, and switch to the record list under 640 pixels. That pair fits both widths.

The runner-up wraps rather than truncates, which fits where one column holds names of any length. Never ship shortened figures near a statement: a rounded money figure is not a money figure.

**Reviewed for:** Does the built dense table match the wireframe its catalog entry chose? Open the built 390-pixel lists and two-column tables beside the catalog entry recording the chosen option and its reason.

#### `the-full-width-with-nothing-truncated`: The Full Width, with Nothing Truncated

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses from 96 spenders."
    action "Download"
  field "Expense number, name or email" search
  region "Expenses"
    table 4 "Expense" "Spender" "Submitted" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 2026, 09:32" · status "Paid" positive · text "EUR 125.00"
```

```wireframe compact
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses, newest first."
    action "Download"
  field "Expense or spender" search
  region "Expenses"
    list 4
      item: text "EXP-1042" · text "Ava" · text "Rotterdam client visit" · text "8 Oct, 09:32" · status "Paid" positive · text "EUR 125.00"
```

**Rationale:** At 1440 pixels every column has room: the trip is the second line of the expense cell, so nothing needs an ellipsis.

**Cost:** Five columns reach the ceiling at this width: a seventh column forces a truncation, and the second line is where it goes.

#### `at-390-pixels-a-record-list-rather-than-a-table`: At 390 Pixels, a Record List Rather Than a Table

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses, newest first."
    action "Download"
  field "Expense or spender" search
  region "Expenses"
    list 4
      item: text "EXP-1042" · text "Ava" · text "Rotterdam client visit" · text "8 Oct, 09:32" · status "Paid" positive · text "EUR 125.00"
```

**Rationale:** A phone has no room for columns, so one block per expense holds everything.

**Cost:** There is no sorting by column and no comparison across a column. On a phone a reader seeks one known expense, and the list serves that case.

#### `at-390-pixels-a-table-with-two-columns-and-no-truncation`: At 390 Pixels, a Table with Two Columns and No Truncation

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses."
    action "Download"
  field "Expense or spender" search
  region "Expenses"
    table 6 "Expense" "Paid"
      item: text "EXP-1042" · text "EUR 125.00"
  region "Note"
    text muted "The spender and trip are on the record. Open one to read it."
```

**Rationale:** The table keeps two facts per row: the expense and what it cost. The trip and the spender stay on the record.

**Cost:** Rows of expense numbers and amounts cannot show spenders, so this works only when the reader arrives with an expense number.

### Counting Things

**ID:** `litespec/frontend/patterns/money-time-density#counting-things`

**Strength:** default

**Question:** One rule governs every count: read a number the system can count, and never type it. A hand-typed figure is never updated, and a reviewer trusts it anyway.

**Default:** `counts-in-the-sidebar-on-the-rows-that-hold-work`

**Compact:** On a narrow screen the default option stacks the sidebar counts above the work list, with each row keeping its count.

**Rationale:** Keep counts where the reader would otherwise fetch them: on the sidebar rows that hold work, and on the tabs above a list. The runner-up is the stat tiles with their measure stated.

Tiles are the better choice for the home page, where the question is how large the month is. Never ship counts with the report that produces them: the report name is system language on a number page.

**Reviewed for:** Does the built tally match the wireframe its catalog entry chose? Open the built sidebar counts, tiles, and report-backed counts beside the catalog entry recording the chosen option and its reason.

#### `counts-in-the-sidebar-on-the-rows-that-hold-work`: Counts in the Sidebar, on the Rows That Hold Work

```wireframe
page
  breadcrumb "Home" "Expenses" "Reversals"
  header
    heading "Reversals"
    status "Open" neutral
    text "10 open. 2 are outside the reversal window."
  split 1:3
    nav side
      link "Expenses"
      link "Late Payouts 31"
      link current "Reversals 10"
      link "Exceptions 2"
    region "Reversals"
      list 5
        item: text "EXP-1029" · text "Elin Lindqvist" · status "No budget" critical · text "EUR 45.00" · action "Deal with it"
```

**Rationale:** Reversals 10 in the sidebar, one thing to do on the home page. A count appears where a reader would otherwise have to go and get it.

**Cost:** Every count is a read on every page load. A count of 0 beside an area makes it look finished rather than unused.

#### `counts-as-stat-tiles-with-the-measure-they-describe`: Counts as Stat Tiles, with the Measure They Describe

```wireframe
page
  header
    heading "Good afternoon, Alex"
    text "Two things need you today."
    action primary "New expense"
  region "Claimed this month"
    grid 3
      card "Claimed this month"
        figure "9,412"
        text "Lines across every trip"
  region "Needs attention"
    list 2
      item: text "10 reversals need a decision" · text muted "2 are outside the reversal window" · action "Deal with them"
```

**Rationale:** The home page shape is a label, a figure, and one line saying what the figure counts. Carbon describes a KPI as a number with a single-word description, and 3 to 5 tiles make a reasonable row.

**Cost:** A tile figure is one moment, and the next read is a different moment, so two colleagues looking at the same page can see different numbers with no explanation.

**Reference:** Carbon

#### `counts-with-the-report-that-produces-them`: Counts with the Report That Produces Them

```wireframe
page
  breadcrumb "Home" "Reports" "Census"
  header
    heading "The census"
    text "Every figure on this page is read by a report."
  region "What this company holds"
    table 7 "What" "Count" "Report"
      item: text "Paid expenses" · text "214" · link "Expenses report"
```

**Rationale:** Each figure names the report that reads it, so a reader who wants the number elsewhere can get it. A figure typed into a page is wrong from the next commit; these figures are read on demand.

**Cost:** A report name beside every figure is system language on a page where the reader only wants the number. It belongs in documentation rather than in the product, unless the reader can open the report from it.

### Spending over Time

**ID:** `litespec/frontend/patterns/money-time-density#spending-over-time`

**Strength:** default

**Question:** How the product shows spending over time. A dashboard can answer how the quarter is going with a number, which says where the budget stands, but only a chart says whether spending is speeding up or slowing down.

**Default:** `daily-columns-with-the-total-above`

**Compact:** On a narrow screen the total stays above the daily columns, with the peak still labeled in words.

**Rationale:** Ship the daily columns with the total above and the peak labeled: they answer how much and when in one glance. The runner-up is the cumulative line against the previous quarter, and it fits once a previous quarter exists to compare against. Never ship the chart without its table: a figure nobody can read exactly is a figure nobody can cite.

**Reviewed for:** Does every built trend chart match the wireframe its catalog entry chose? Open the built columns, areas, and sparklines beside the catalog entry recording the chosen option and its reason.

#### `daily-columns-with-the-total-above`: Daily Columns with the Total Above

```wireframe
page
  breadcrumb "Home" "Trips" "Rotterdam client visit" "Spending"
  header
    heading "Rotterdam client visit, spending"
    text "The 30 days to 8 Oct 2026, Europe/Amsterdam."
    action "Download"
  region "Lines per day"
    figure "8,176 lines"
    text "30 days. Peak 8 Oct, 488 lines."
    image "Daily lines claimed, 9 September to 8 October 2026, bar chart"
    text muted "Counts are lines claimed, before reversals."
```

**Rationale:** Shopify sales reports open with the total and draw the days as columns, so the headline and the shape sit together. The peak day is labeled in words, which is the figure the company quotes in the review.

**Cost:** Thirty columns at phone width are a few pixels each: the shape survives but the single day does not, so the table behind the chart carries the exact day, not the chart.

**Reference:** Shopify

#### `cumulative-spending-against-the-previous-quarter`: Cumulative Spending Against the Previous Quarter

```wireframe
page
  breadcrumb "Home" "Trips" "Rotterdam client visit" "Spending"
  header
    heading "Rotterdam client visit, spending"
    text "Claimed so far, against the previous quarter."
    action "Download"
  region "Cumulative lines claimed"
    figure "9,412 lines"
    text "1,042 ahead of the previous quarter."
    text muted "Solid line is this quarter. Dashed line is the previous quarter."
    image "Cumulative lines claimed against the previous quarter, line chart"
    text muted "Both lines count the same weeks after the quarter start."
```

**Rationale:** Vercel analytics draw the previous period as a dashed line behind the current one, so the comparison needs no second chart. A claimed line above the dashed line shows a growing quarter.

**Cost:** A first-year budget has no previous quarter, so the page says so instead of drawing one line and calling it a comparison. Readers may also mistake a calendar shift for a real gap.

**Reference:** Vercel

#### `weekly-columns-with-the-average-marked`: Weekly Columns with the Average Marked

```wireframe
page
  breadcrumb "Home" "Trips" "Rotterdam client visit" "Spending"
  header
    heading "Rotterdam client visit, spending"
    text "The 30 days to 8 Oct 2026, in weeks."
    action "Download"
  region "Lines per week"
    figure "8,176 lines"
    fact "Weekly average" "1,831, full weeks only"
    image "Lines claimed per week with the weekly average marked, bar chart"
    text muted "The last column holds two days, 7 and 8 October."
```

**Rationale:** Plausible dashboards mark the average across the bars, so a glance shows which weeks ran above average. Weekly columns suit a company that plans payout runs by week rather than by day.

**Cost:** Weeks cut across the quarter's phases of kickoff, steady filing, and year-end close: a week holding two of them averages them together, and the final two-day column is not a week at all.

**Reference:** Plausible

### Spending by Expense Kind

**ID:** `litespec/frontend/patterns/money-time-density#spending-by-expense-kind`

**Strength:** default

**Question:** How the product shows spending by kind. The Rotterdam trip claims three expense kinds plus a fee, and the company asks which kind carries the quarter, which is a question of share rather than total.

**Default:** `horizontal-bars-with-the-figure-on-each-row`

**Compact:** On a narrow screen each kind keeps its bar with its figure and count on one stacked row.

**Rationale:** Ship the horizontal bars with the figure on each row: each kind shows its count and its amount in one glance. The runner-up is the table with a bar in each row, and it fits once kinds gain states such as disputed or reversed. Never ship the donut alone: an angle nobody can read exactly is decoration, not a figure.

**Reviewed for:** Does every built mix chart match the wireframe its catalog entry chose? Open the built bars, tiles, and donuts beside the catalog entry recording the chosen option and its reason.

#### `horizontal-bars-with-the-figure-on-each-row`: Horizontal Bars with the Figure on Each Row

```wireframe
page
  breadcrumb "Home" "Trips" "Rotterdam client visit" "Spending"
  header
    heading "Rotterdam client visit, by kind"
    text "9,412 lines and 126 nights, at receipt amounts."
    action "Download"
  region "Lines"
    list 3
      item: text "Travel" · text muted "EUR 45.00, 6,204 lines" · text "EUR 279,180.00" · image "Share bar, 65.9 percent"
  region "Lodging"
    fact "Hotel nights" "EUR 3,150.00"
    text muted "EUR 25.00, 126 nights. Nights keep their own scale."
```

**Rationale:** Shopify product performance lists each variant with its bar and its money, so the ranking and the figures share one row. Every kind carries its amount, its count, and its claimed total: the whole question in one glance.

**Cost:** Bars share one scale, so the lodging EUR 3,150.00 is a sliver beside travel. A second trip that matters needs its own scale, which this option does not give it.

**Reference:** Shopify

#### `a-table-with-a-bar-in-each-row`: A Table with a Bar in Each Row

```wireframe
page
  breadcrumb "Home" "Trips" "Rotterdam client visit" "Spending"
  header
    heading "Rotterdam client visit, by kind"
    text "Every kind with its amount, its count and its share."
    action "Download"
  region "By kind"
    table 4 "Kind" "State" "Amount" "Claimed" "Share" "Total"
      item: text "Travel" · status "Active" positive · text "EUR 45.00" · text "6,204" · image "Share bar, 65.9 percent" · text "EUR 279,180.00"
    text muted "The taxi kind is paused. Its claimed lines stand."
```

**Rationale:** Notion progress columns put a bar inside the table cell, so the figure and its share never separate. The table also has room for the state each kind holds, which bars alone cannot carry.

**Cost:** Five columns need a wide page, so the table scrolls sideways on a phone, and the bar in a scrolling table is the first thing cut off, which is where the share lives.

**Reference:** Notion

#### `a-tile-per-type-with-its-share`: A Tile per Type with Its Share

```wireframe
page
  breadcrumb "Home" "Trips" "Rotterdam client visit" "Spending"
  header
    heading "Rotterdam client visit, by kind"
    text "Three kinds, one quarter."
    action "Download"
  region "By kind"
    grid 3
      card "Travel"
        figure "EUR 279,180.00"
        text "EUR 45.00, 6,204 lines, 65.9 percent"
```

**Rationale:** A per-kind summary gives each kind its own tile with claimed and share, so three kinds read as three facts rather than one ranking. Tiles suit a home page where the by-kind question is asked in passing.

**Cost:** Tiles cannot be sorted or compared at a glance the way bars can, and a fourth kind wraps the row. The amount of text each tile needs makes every tile taller than the figure it carries.

### Spend Against the Budget

**ID:** `litespec/frontend/patterns/money-time-density#spend-against-the-budget`

**Strength:** default

**Question:** How the product shows spend against the budget. The quarter budget holds EUR 10,000 with EUR 9,412 claimed, and the distance between the two is the decision: move money, hold claims, or call the budget spent.

**Default:** `a-bar-with-claimed-held-and-left-in-words`

**Compact:** On a narrow screen the spend bar keeps the claimed, held, and left figures in words beneath it.

**Rationale:** Ship the progress bar with claimed, held, and left in words: one glance shows how much of the budget is spent and what can still move. The runner-up is the per-kind spend table, and it fits when one kind is spent and the others are not. Never ship the countdown alone: EUR 468 left without the budget size hides whether the budget is nearly gone or barely touched.

**Reviewed for:** Does every built fill-rate view match the wireframe its catalog entry chose? Open the built bars, countdowns, and capacity tables beside the catalog entry recording the chosen option and its reason.

#### `a-bar-with-claimed-held-and-left-in-words`: A Bar with Claimed, Held and Left in Words

```wireframe
page
  breadcrumb "Home" "Finance" "Budgets" "TRIP10"
  header
    heading "TRIP10, spend"
    text "The quarter budget holds EUR 10,000."
    action "Open budget"
  region "How far the budget has burned"
    figure "94.1 percent spent"
    text "EUR 9,412 of EUR 10,000. EUR 468 left."
    image "Spend bar, 94 percent spent"
    fact "Claimed" "EUR 9,412"
    fact "Held" "EUR 120"
    fact "Left to spend" "EUR 468"
    text muted "Held money is reserved for open disputes and advances."
```

**Rationale:** A budget app draws spending as a bar against the budget with the figures in words, so the glance and the figures agree. Claimed, held, and left are three different decisions, which is why the bar names all three.

**Cost:** One bar hides which kind is spent, so a spent taxi kind behind a half-empty budget reads as room to spare. The per-kind rows answer that question, not this bar.

#### `spend-per-kind`: Spend per Kind

```wireframe
page
  breadcrumb "Home" "Finance" "Budgets" "TRIP10"
  header
    heading "TRIP10, spend"
    text "EUR 9,412 of EUR 10,000 spent."
    action "Open budget"
  region "By kind"
    list 3
      item: text "Travel" · text muted "EUR 6,204 of EUR 6,500" · text "95.4 percent" · image "Spend bar, 95 percent"
    text muted "The taxi kind is paused. Its EUR 200 unclaimed cannot be spent."
```

**Rationale:** Shopify inventory by variant draws each variant against its own stock, because one empty kind and two full ones are not the same as a third gone everywhere. Each row states what refilling that kind would take.

**Cost:** Three bars share no scale unless the budgets are close, and here they are not, so the budget total has to sit above the rows or the budget reads as three budgets.

**Reference:** Shopify

#### `capacity-claimed-held-and-left-as-a-table`: Capacity, Claimed, Held and Left as a Table

```wireframe
page
  breadcrumb "Home" "Finance" "Budgets" "TRIP10"
  header
    heading "TRIP10, spend"
    text "Every euro accounted for, by kind."
    action "Open budget"
  region "By kind"
    table 4 "Kind" "Budget" "Claimed" "Held" "Left"
      item: text "Travel" · text "EUR 6,500" · text "EUR 6,204" · text "EUR 60" · text "EUR 236"
    text muted "Held money is reserved for open disputes and advances."
```

**Rationale:** Shopify inventory by location is a table because four figures per row need columns rather than bars. Every row adds up across and every column adds down, so finance can check the arithmetic both ways.

**Cost:** A table of four money-less counts reads as a ledger, which is what it is: it answers the audit and bores the glance, so the bar belongs above it on the page that holds both.

**Reference:** Shopify

### Figure Against an Earlier Period

**ID:** `litespec/frontend/patterns/money-time-density#figure-against-an-earlier-period`

**Strength:** default

**Question:** How the product compares a figure against an earlier period. A tile saying EUR 63,350.00 answers how much; only the tile adding August EUR 48,340.00 beside it answers whether the month was good.

**Default:** `tiles-with-a-delta-pill-and-the-previous-figure`

**Compact:** On a narrow screen the comparison tiles stack, each keeping its delta pill with the previous figure in words.

**Rationale:** Ship the tiles with a delta pill and the previous figure in words: the comparison is visible and the basis is stated. The runner-up is the tiles with sparklines, and it fits the home page where the shape of the month matters more than one earlier figure. Never ship a bare percentage with no basis: up 12 percent without the basis is a claim, not a comparison.

**Reviewed for:** Does every built comparison tile match the wireframe its catalog entry chose? Open the built deltas, sparklines, and KPI tables beside the catalog entry recording the chosen option and its reason.

#### `tiles-with-a-delta-pill-and-the-previous-figure`: Tiles with a Delta Pill and the Previous Figure

```wireframe
page
  breadcrumb "Home" "Reports" "Spending"
  header
    heading "September 2026"
    text "Against August 2026, same 30 days."
    action "Download"
  region "September against August"
    grid 3
      card "Spending"
        figure "EUR 63,350.00"
        status "Up 31.1 percent" positive
        text muted "August EUR 48,340.00"
        text "1,148 expenses"
```

**Rationale:** Stripe home tiles compare each figure against the previous period with the basis stated under the delta, so no reader asks compared to what. The pill carries direction and size, and the words carry the basis.

**Cost:** Three tiles with a pill, a basis, and a note each are three small paragraphs, which is heavy for a home page. A fourth KPI wraps the row and takes its comparison to the next row.

**Reference:** Stripe

#### `tiles-with-sparklines`: Tiles with Sparklines

```wireframe
page
  breadcrumb "Home" "Reports" "Spending"
  header
    heading "September 2026"
    text "Against August 2026, same 30 days."
    action "Download"
  region "September against August"
    grid 3
      card "Spending"
        figure "EUR 63,350.00"
        status "Up 31.1 percent" positive
        image "Spending by day, sparkline"
        text muted "August EUR 48,340.00"
```

**Rationale:** Vercel usage tiles pair each meter with its sparkline, so the month reads as a shape rather than two endpoints. A steady climb and a last-week spike end at the same figure and mean different things.

**Cost:** A sparkline shares no axis with its neighbors, so the reversal line can look calmer than the spending line while rising faster. The delta pill beside each one stays required.

**Reference:** Vercel

#### `no-comparison-only-the-figure-with-its-date`: No Comparison, Only the Figure with Its Date

```wireframe
page
  breadcrumb "Home" "Reports" "Spending"
  header
    heading "September 2026"
    text "The first full month. There is no earlier period to compare."
    action "Download"
  region "September"
    grid 3
      card "Spending"
        figure "EUR 63,350.00"
        text muted "As at 30 September 2026, 1,148 expenses"
```

**Rationale:** GOV.UK statistics releases state the period with no comparison until the next release exists, because a comparison without a fair basis misleads. A company in its first month has no August to compare against, so the tile says when instead of versus.

**Cost:** A figure with no comparison answers how much and never whether it was good. This option suits the first month; every month after it needs the delta back.

**Reference:** GOV.UK

### When the Money Arrives

**ID:** `litespec/frontend/patterns/money-time-density#when-the-money-arrives`

**Strength:** default

**Question:** How the product shows when the money arrives. Finance holds approved expenses and pays them out on payout days, and spenders plan around those days, so the schedule is a planning surface rather than a history.

**Default:** `upcoming-payouts-with-the-expenses-each-one-holds`

**Compact:** On a narrow screen each payout keeps its arrival date, expenses, and amount in one stacked row.

**Rationale:** Ship the list of upcoming payouts with the expenses each one holds: the company sees what goes out and when in one place. The runner-up is the past payouts table with bank references, and it fits finance reconciling against the bank. Never ship the single next-payout tile as the page: one arrival without the ones after it cannot plan a month.

**Reviewed for:** Does every built payout view match the wireframe its catalog entry chose? Open the built upcoming, weekly, and past views beside the catalog entry recording the chosen option and its reason.

#### `upcoming-payouts-with-the-expenses-each-one-holds`: Upcoming Payouts with the Expenses Each One Holds

```wireframe
page
  breadcrumb "Home" "Reports" "Payouts"
  header
    heading "Payouts"
    text "What runs from NL91 ABNA 0417 1643, and when."
    action "Download"
  region "On the way and building up"
    table 4 "Arrives" "Holds" "Amount" "State"
      item: text "Mon 12 Oct 2026" · text "Expenses of 5 to 11 October, 418 expenses" · text "EUR 8,412.55" · status "On its way" info
```

**Rationale:** The Stripe payouts list pairs each payout with its arrival date and the expenses it holds, because the company plans around arrivals rather than filings. Each row states its state, since a payout building up is not a payout sent.

**Cost:** A building payout moves with every expense, so its amount is stale the moment the page sits open. The page must date each figure, which the list does and a tile cannot.

**Reference:** Stripe

#### `payouts-grouped-by-week-with-totals`: Payouts Grouped by Week with Totals

```wireframe
page
  breadcrumb "Home" "Reports" "Payouts"
  header
    heading "Payouts"
    text "What runs from NL91 ABNA 0417 1643, and when."
    action "Download"
  region "Week of 12 October, EUR 8,412.55"
    table 4 "Arrives" "Holds" "Amount" "State"
      item: text "Mon 12 Oct" · text "Expenses of 5 to 11 October, 418 expenses" · text "EUR 8,412.55" · status "On its way" info
  region "Week of 5 October, EUR 6,117.46"
    table 4 "Arrives" "Holds" "Amount" "State"
      item: text "Mon 5 Oct" · text "Expenses of 28 to 30 September, 214 expenses" · text "EUR 6,117.46" · status "Paid out" positive
  region "Week of 28 September, EUR 12,618.04"
    table 4 "Arrives" "Holds" "Amount" "State"
      item: text "Mon 28 Sep" · text "Expenses of 21 to 27 September, 302 expenses" · text "EUR 12,618.04" · status "Paid out" positive
```

**Rationale:** QuickBooks groups deposits by week with a total per group, because payouts are planned by week. That group total is the figure the company carries into planning for the week.

**Cost:** A week holding two payouts, as happens around holidays, gets a total covering two arrivals, and the group then needs its dates restated, which the flat list never needs.

**Reference:** QuickBooks

#### `past-payouts-with-their-bank-references`: Past Payouts with Their Bank References

```wireframe
page
  breadcrumb "Home" "Reports" "Payouts"
  header
    heading "Payouts"
    text "What went out, with the reference each bank line carries."
    action "Download"
  region "Past payouts"
    table 4 "Paid out" "Covers" "Amount" "Bank reference"
      item: text "5 Oct 2026" · text "28 to 30 Sep" · text "EUR 6,117.46" · text "SH-PO-44182"
    action quiet "Copy the bank reference"
    fact "September total" "Five payouts, EUR 61,346.68 net"
```

**Rationale:** The Revolut Business payout history puts the bank reference on each row, because finance checks each payout against a bank line. The reference is copyable, which is the whole job of this table.

**Cost:** A history answers reconciliation and nothing else: it says what went out, never what is coming, so the upcoming list has to sit above it or the page plans nothing.

**Reference:** Revolut Business

### Vendor's Invoice to the Company

**ID:** `litespec/frontend/patterns/money-time-density#vendors-invoice-to-the-company`

**Strength:** default

**Question:** How the product shows the vendor's invoice to the company. Billing holds what the company owes its software vendor, the Business subscription plus anything metered, and the invoice is the one document where the vendor is the seller: it reads as the sale of the vendor rather than the sale of the company.

**Default:** `the-invoice-as-a-document-with-the-usage-behind-each-line`

**Compact:** On a narrow screen the seller facts stack above the lines, and the totals keep their order down to the amount due.

**Rationale:** Ship the document-style invoice with the usage behind each line: finance sees what the fee paid for without opening another page. The runner-up is the paid invoice with its mandate reference, which is the better state of the same document once the money has moved. Never ship the invoice without its VAT lines: a business invoice without VAT is not an invoice.

**Reviewed for:** Does every built supplier bill match the wireframe its catalog entry chose? Open the built documents, states, and usage lines beside the catalog entry recording the chosen option and its reason.

#### `the-invoice-as-a-document-with-the-usage-behind-each-line`: The Invoice as a Document, with the Usage Behind Each Line

```wireframe
page
  breadcrumb "Home" "Invoices" "2026-1841"
  header
    heading "Invoice 2026-1841"
    status "Unpaid" caution
    text "September 2026. Due 15 October 2026."
    action "Download PDF"
  region "From Claimwick"
    fact "Seller" "Claimwick B.V., Herengracht 420, Amsterdam"
    fact "VAT" "NL987654321B01"
    fact "To" "Harbor and Pine"
  region "Lines"
    table 4 "What" "Quantity" "Rate" "Amount"
      item: text "Business subscription, September 2026" · text "1 month" · text "EUR 49.00" · text "EUR 49.00"
  region "Totals"
    fact "Subtotal, excluding VAT" "EUR 169.40"
    fact "VAT, 21%" "EUR 35.57"
    fact "Due" "EUR 204.97"
    text muted "Collected by mandate ACM-2024-0117 on 15 October 2026."
```

**Rationale:** The Xero invoice preview shows the document the customer receives, with each line carrying the quantity behind it. The company reads what the vendor charged and why in one place, which is the whole point of an invoice.

**Cost:** A document is a page of its own, so the invoice list needs the state of each invoice without opening it. The list carries the states; this page carries the lines.

**Reference:** Xero

#### `paid-with-the-mandate-reference-and-the-date`: Paid, with the Mandate Reference and the Date

```wireframe
page
  breadcrumb "Home" "Invoices" "2026-1702"
  header
    heading "Invoice 2026-1702"
    status "Paid" positive
    text "August 2026. Paid 15 August 2026 by mandate ACM-2024-0117."
    action "Download PDF"
  region "From Claimwick"
    fact "Seller" "Claimwick B.V., Herengracht 420, Amsterdam"
    fact "Paid" "15 August 2026, EUR 190.45"
  region "Lines"
    table 4 "What" "Quantity" "Rate" "Amount"
      item: text "Business subscription, August 2026" · text "1 month" · text "EUR 49.00" · text "EUR 49.00"
  region "Totals"
    fact "Subtotal, excluding VAT" "EUR 157.40"
    fact "VAT, 21%" "EUR 33.05"
    fact "Paid 15 August 2026" "EUR 190.45"
```

**Rationale:** The Wise paid receipts carry the reference and the date above the lines, so a filed invoice answers when and how without being read. The stamp states the state in the words of the document rather than the words of the dashboard.

**Cost:** A stamp is final in a way a badge is not, so a payment that later fails leaves a stamped invoice that lies. The state must follow the mandate rather than the page the reader printed.

**Reference:** Wise

#### `every-invoice-with-its-state`: Every Invoice with Its State

```wireframe
page
  breadcrumb "Home" "Invoices"
  header
    heading "Invoices"
    text "What Harbor and Pine owes Claimwick, newest first."
  region "Every invoice"
    table 5 "Invoice" "Period" "State" "Due" "Total"
      item: text "2026-1841" · text "September 2026" · status "Unpaid" caution · text "15 Oct 2026" · text "EUR 204.97"
    text muted "Subscription and usage fee, including VAT. Open an invoice for its lines."
```

**Rationale:** The Stripe invoices table lists each invoice with its period, its amount, and its state, so the list answers whether anything is owed before any invoice is opened.

**Cost:** A list of invoices says nothing about what any invoice holds, so a surprising total needs the document. Each row opens the invoice, which must then carry the usage this list omits.

**Reference:** Stripe

### Two VAT Rates on One Page

**ID:** `litespec/frontend/patterns/money-time-density#two-vat-rates-on-one-page`

**Strength:** default

**Question:** How the product shows two VAT rates on one page. Books carry 9% and a hotel night carries 21%, the spender sees one total, and the accounts show both rates on the page the tax return is built from.

**Default:** `the-vat-split-under-the-expense-total`

**Compact:** On a narrow screen the claimed facts stack above the VAT split, with each rate keeping its base and tax.

**Rationale:** Ship the VAT split under the expense total: the spender total stays one figure and the books get both rates. The runner-up is the per-rate report with taxable bases, and it fits quarter end when finance files the return. Never ship amounts with no VAT note at all: a lawful amount states what the tax is.

**Reviewed for:** Does every built mixed-rate view match the wireframe its catalog entry chose? Open the built splits, badges, and per-rate summaries beside the catalog entry recording the chosen option and its reason.

#### `the-vat-split-under-the-expense-total`: The VAT Split Under the Expense Total

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava. Amounts include VAT."
  region "What she claimed"
    fact "Hotel night, 2 at EUR 45.00" "EUR 90.00"
    fact "Booking fee" "EUR 10.00"
    fact "Design book" "EUR 25.00"
    fact "Paid to the spender" "EUR 125.00"
  region "VAT in that total"
    fact "9% on books, base EUR 22.94" "EUR 2.06"
    fact "21% on night and fee, base EUR 82.64" "EUR 17.36"
    fact "Total VAT" "EUR 19.42"
    text muted "Books are taxed at 9%. Nights and fees at 21%."
```

**Rationale:** The Coolblue receipts split VAT per rate under the total: one spender-facing figure breaks down into both rates, and each line states its base and its tax, which is what the return needs per rate.

**Cost:** Three more rows under every total is heavy on a receipt for two nights, so the split belongs on the record and the export rather than on every row of the expenses list.

**Reference:** Coolblue

#### `the-rate-on-each-invoice-line`: The Rate on Each Invoice Line

```wireframe
page
  breadcrumb "Home" "Invoices" "2026-1841"
  header
    heading "Invoice 2026-1841"
    status "Unpaid" caution
    text "September 2026. Due 15 October 2026."
    action "Download PDF"
  region "Lines with their rate"
    table 3 "What" "VAT" "Amount, excluding VAT"
      item: text "Business subscription, September 2026" · status "21%" neutral · text "EUR 49.00"
  region "Totals"
    fact "Subtotal, excluding VAT" "EUR 169.40"
    fact "VAT, 21%" "EUR 35.57"
    fact "Due" "EUR 204.97"
```

**Rationale:** The Moneybird invoice lines carry their own rate, so a mixed invoice is right on every line instead of in a summary the reader takes on trust. The rate badge sits where the eye checks the amount.

**Cost:** A rate column narrows every other column on a document that already holds four of them. On a single-rate invoice the same badge repeats on every line, which says nothing after the first.

**Reference:** Moneybird

#### `a-vat-report-with-taxable-bases`: A VAT Report with Taxable Bases

```wireframe
page
  breadcrumb "Home" "Reports" "VAT"
  header
    heading "Third quarter 2026"
    text "VAT collected, per rate, with the base each rate taxed."
    action "Download"
  region "Per rate"
    table 4 "Rate" "Base" "VAT" "Gross"
      item: text "21%, nights and fees" · text "EUR 133,520.66" · text "EUR 28,039.34" · text "EUR 161,560.00"
    text muted "Gross is what spenders claimed, including VAT."
```

**Rationale:** The Exact VAT return overview lists each rate with its base and its tax, because the return is filed per rate. Its two quarter lines are the two lines finance copies into the filing.

**Cost:** A report per rate says nothing about which kinds produced it, so a surprising base sends finance to the kind spending behind it. The report opens that spending instead of only stating the base.

**Reference:** Exact

### Numbers at Phone Width

**ID:** `litespec/frontend/patterns/money-time-density#numbers-at-phone-width`

**Strength:** default

**Question:** How numbers read at phone width. Alex Morgan checks the payout of the day from the office floor on a phone, where at 390 pixels a money column, a date column, and a state badge do not fit side by side. The page chooses what the number needs beside it.

**Default:** `kpis-in-pairs-with-the-expenses-under-them`

**Compact:** At 390 pixels the figures pair two across with the latest records under them, tables become record lists with one right-aligned amount per row, statements keep their alignment with short labels, and charts halve their axis labels. The compact option is `kpis-in-pairs-with-the-expenses-under-them`.

**Rationale:** Ship the two-column KPI pair with the record list under it: the figures of the day stay visible and every expense keeps its amount right-aligned. The runner-up is the aligned statement, and it fits finance pages where the adding up is the point. Never ship the desktop table unchanged at 390 pixels: a table that scrolls sideways for its money column hides the answer.

**Reviewed for:** Does every built narrow figure match the wireframe its catalog entry chose? Open the built pairs, per-row figures, and halved charts at 390 pixels beside the catalog entry recording the chosen option and its reason.

#### `kpis-in-pairs-with-the-expenses-under-them`: KPIs in Pairs with the Expenses Under Them

```wireframe
page
  header
    heading "Saturday payout"
    text "Today, 08:00 to 18:00. Rotterdam office."
  grid 4
    card "Paid"
      figure "9,412"
      text muted "of 10,000"
  region "Latest expenses"
    list 3
      item: text "EXP-1042" · text muted "Ava, 09:32" · text "EUR 125.00"
```

**Rationale:** The Shopify mobile dashboard pairs its KPIs two across, so four figures fit above the fold of a phone without shrinking past reading. The expenses under them keep one amount each, right-aligned, which is the column the list is about.

**Cost:** Two across at 390 pixels is 169 pixels a tile, so a long figure wraps or clips. Figures over ten characters need the single-column list rather than the pair.

**Reference:** Shopify

#### `expenses-as-records-with-amounts-right-aligned`: Expenses as Records with Amounts Right-Aligned

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses, newest first."
  toolbar
    field "Expense or spender" search
  region "Expenses"
    list 4
      item: text "EXP-1042, Ava" · text muted "Rotterdam client visit, 8 Oct 09:32, Paid" · text "EUR 125.00"
```

**Rationale:** The ING mobile activity list keeps one right-aligned amount per row in tabular figures, so the money column survives without a table. The state sits under the name, where it is read second.

**Cost:** There are no columns, so there is no sorting by column and no comparison across one. A reader looking for one expense they were told about is served; a reader auditing the day is not.

**Reference:** ING

#### `a-statement-that-keeps-its-alignment`: A Statement That Keeps Its Alignment

```wireframe
page
  breadcrumb "Home" "Reports"
  header
    heading "September 2026"
    text "Every euro of the month."
  region "Spending"
    fact "Travel" "EUR 54,180.00"
    fact "Meals" "EUR 6,020.00"
    fact "Lodging" "EUR 3,150.00"
    fact "Spending" "EUR 63,350.00"
  region "Out"
    fact "Reversals" "EUR 1,550.00"
    fact "Fees" "EUR 453.32"
    fact "Out" "EUR 2,003.32"
  region "Net"
    fact "Net" "EUR 61,346.68"
```

**Rationale:** The Xero mobile profit and loss keeps the statement shape at phone width, because a statement must add up at every width. Short labels and tabular figures hold the alignment of the wide option.

**Cost:** Labels short enough for 390 pixels lose the counts the full lines carry: the statement adds up but no longer says what it adds, which the full labels do.

**Reference:** Xero

### Showing an Identifier

**ID:** `litespec/frontend/patterns/money-time-density#showing-an-identifier`

**Strength:** default

**Question:** How the product shows an identifier. One design rule governs identifiers: an identifier on screen is labeled and copyable, and a raw code never reaches the screen. A payout page showing SH-88213 as plain text breaks it, and so does a messages list showing identifiers where a subject should be.

**Default:** `a-code-monospaced-labeled-and-copyable`

**Compact:** On a narrow screen each code card stacks with its label and copy control beside the value.

**Rationale:** Ship the labeled, monospaced, copyable code on the record: it is the shape of the rule and the one thing a support case needs quoted. The runner-up is the identifier as the record name, and it fits records with no human name, such as a payout reference. Never ship the page with no identifier: a record nobody can quote is a record support cannot help with.

**Reviewed for:** Does every built code display match the wireframe its catalog entry chose? Open the built monospaced, labeled, and copyable codes beside the catalog entry recording the chosen option and its reason.

#### `a-code-monospaced-labeled-and-copyable`: A Code, Monospaced, Labeled and Copyable

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava, Harbor and Pine."
    action primary "Message spender"
  region "Codes to quote"
    card "Expense number"
      text "EXP-1042"
      text muted "Quote it when you call about the expense."
      action quiet "Copy"
    card "Bank reference"
      text "SH-88213"
      text muted "For a support case with the bank."
      action quiet "Copy"
    card "Receipt codes"
      text "RC-0C893968A2"
      text muted "2 receipts, neither matched."
      action quiet "Copy"
```

**Rationale:** The shared code shape is monospaced, never wrapped, with a copy control. A code a person reads aloud on a support call is functional, not decorative.

**Cost:** A copy control on every row of a list is a control per row, and a code in a monospaced font looks visibly different from a name, which is correct and slightly more prominent than the page around it.

#### `the-identifier-labeled-by-what-it-is-not-by-its-form`: The Identifier Labeled by What It Is, Not by Its Form

```wireframe
page
  breadcrumb "Home" "Reports" "Disputes" "D-0182"
  header
    heading "Dispute D-0182"
    status "Disputed" critical
    text "EUR 45.00 on expense EXP-1031. Opened 2 Oct 2026 by the bank."
    action primary "Answer the bank"
  region "What each code is"
    list 3
      item: text "Case number" · text muted "What the bank calls it" · text "D-0182" · action quiet "Copy"
```

**Rationale:** Labels name what the code is, so Expense number is used instead of Reference, which could mean anything. A reader who knows what a thing is can use it, and a reader who does not can ask.

**Cost:** Some identifiers have no natural name and end up labeled by their kind, which tells a reader nothing about where to use them.

#### `the-identifier-as-the-record-s-name`: The Identifier as the Record's Name

```wireframe
page
  breadcrumb "Home" "Expenses" "Receipts" "RC-0C893968A2"
  header
    heading "RC-0C893968A2"
    status "Unmatched" neutral
    text "One hotel bill, filed on expense EXP-1042."
    action "Match to a line"
  region "The receipt code"
    fact "Covers" "One bill"
    fact "Spender" "Ava"
    fact "Expense" "EXP-1042"
    fact "Issued" "8 Oct 2026, 09:34"
    fact "Matched" "Never"
  region "How it was used"
    timeline 2
      item: text "Attached at filing" · text muted "8 Oct 2026, 09:34"
```

**Rationale:** Where a record has no human name the identifier is the heading: the code is what the record is called, and it is never repeated as a field.

**Cost:** A code as a heading cannot be read aloud. It is the right shape where the code is all there is, and the wrong shape where the record has a human name.
