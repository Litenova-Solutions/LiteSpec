# Money, Time and Density Patterns

**Stack:** any

**Concern:** frontend-ui

## Intent

This page holds the patterns for writing amounts, statements, moments, periods, counts, charts, payouts, invoices, VAT, and identifiers at every width, read across tables, statements, charts, and phone screens. A team building a finance or reporting screen starts from these patterns, an owner picks an option from the drawings, and the team records the choice in its pattern catalog, the file holding the project's interface decisions. One shared way of writing figures keeps exports, statements, and screens agreeing, so finance can cite a figure without checking where it was read.

## Concepts

### How to Read a Pattern

Each pattern answers one recurring question about money, time, or density, such as how an amount reads in a table. It states the question, the default option a project builds, how the default behaves on a narrow screen, and why the default won. Each option then shows one wireframe drawing with its own rationale, cost, and optional reference. A project records its choice in its pattern catalog and builds the chosen option with its own components. The [UI patterns](../patterns.md) page states the catalog contract and the wireframe language.

### The Running Example

The drawings on this page use the Claimwick September 2026 statement: expense EXP-1042 for Ava at EUR 125.00, the payout run holding it, and the autumn travel season. Amounts use currency codes with two decimals, moments use the Europe/Amsterdam zone, and counts name what they count. The Standards section states the two rules every option keeps: every amount shows its currency, and a moment shared across zones states its zone.

## Standards

### Show the Currency of Every Amount

**ID:** `litespec/frontend/patterns/money-time-density#show-the-currency-of-every-amount`

**Strength:** required

**Statement:** Every amount MUST show its currency.

**Rationale:** A currency on every amount keeps EUR 125.00 distinct from GBP 125.00 on every screen and export. Without it, a second currency turns each bare figure into a guess, and a pasted number carries no way to settle the guess.

**Reviewed for:** Which built figure hides its currency? Open the amounts on built tables, statements, and charts.

### State the Time Zone of a Shared Moment

**ID:** `litespec/frontend/patterns/money-time-density#state-the-time-zone-of-a-shared-moment`

**Strength:** required

**Statement:** A moment shown to readers in different time zones MUST state its time zone.

**Rationale:** A stated zone keeps 09:32 in Amsterdam from reading as 09:32 in London. Without it, two readers plan the same payout window an hour apart, and finance meets only one of them.

**Reviewed for:** Which built shared moment omits its zone? Open the dates and times on built screens read across zones.

## Patterns

### Writing an Amount

**ID:** `litespec/frontend/patterns/money-time-density#writing-an-amount`

**Strength:** default

**Question:** A reader meets amounts in tables, statements, and exports. One table writes EUR 45.00, and one statement writes the euro sign before 45.00. The question is which form the product keeps. It also asks how a negative reads and which separator the product uses.

**Default:** `code-and-two-decimals-in-the-reader-s-language-with-a-true-m`

**Compact:** The recommended option reflows. One column carries the content. Header actions sit under the title. A table scrolls inside its own region.

**Rationale:** Ship the code format in the reader's language with a true minus. It keeps one convention and answers negatives and separators. The runner-up is the plain code table. It shows the same choice without reversal rows.

Choose the symbol with code in the header only when the finance screens and the spender screens show the same form. Never ship the no currency option. It loses the currency in exports. Never ship mixed decimals. It breaks the alignment tables exist for.

**Reviewed for:** Which built money text differs from the wireframe its catalog entry chose? Open the built codes, symbols, and red negatives beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** This option keeps the code convention. It adds a page total and an all-match total. Both totals follow the filters. The reader checks the column without exporting it.

**Cost:** Two totals disagree by design. Labels need to survive a glance. The all-match total adds a second read on every filter change.

#### `code-and-two-decimals-in-the-reader-s-language-with-a-true-m`: Code and Two Decimals, in the Reader's Language, with a True Minus

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

**Rationale:** Stripe dashboard and Shopify admin keep one amount format. The reader's language decides the separators. English shows EUR 1,203.65, and Dutch shows EUR 1.203,65. A reversal uses a true minus before the code. The minus lines up with digits in a tabular column.

**Cost:** Two colleagues on the same account see different separators, so a screenshot in a support case has to be read in its language. The minus is the only signal of money out, so the State column has to say Reversed as well.

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

**Rationale:** Stripe dashboard writes a refund in red with its minus. Money out reads at a glance. The minus keeps the meaning for a reader who misses color.

**Cost:** Red is also the error tone. A statement of reversals reads as a page of problems. The State column still says Reversed. Color alone never carries the meaning.

**Reference:** Stripe dashboard

### Statement That Adds Up

**ID:** `litespec/frontend/patterns/money-time-density#statement-that-adds-up`

**Strength:** default

**Question:** A statement has one purpose. Money adds up top to bottom. Each kind forms a group with a subtotal. A grand total closes the statement at the foot. A count under each line helps a reader spot a missing line.

**Default:** `groups-subtotals-and-a-grand-total`

**Compact:** The recommended option reflows. One column carries the content. Header actions sit under the title. A table scrolls inside its own region.

**Rationale:** Keep groups, subtotals, and a grand total. Keep a count under each line. The count is the only completeness check. The runner-up ends in the payouts it became. It fits finance work against bank lines.

Add links on every line later as a behavior of the pick. Never ship the one figure option as the statement. It hides how the figure was reached.

**Reviewed for:** Which built totals page differs from the wireframe its catalog entry chose? Open the built groups, subtotals, and payout endings beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** Each group holds its lines and a subtotal. A rule separates the grand total. The count under each line shows a missing line.

**Cost:** The count repeats 1,204 twice. A reader opens the expenses list to find which expenses a line holds.

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

**Rationale:** This option drops the count under each line. Shopify finance summary reads this way. Each line is one row and the page is shorter.

**Cost:** A reader cannot check a missing payment. The count is the only completeness check.

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

**Rationale:** The net closes into payouts sent to the bank. Finance ticks each payout against a bank line. The difference reads zero for a closed month.

**Cost:** A payout can span two months. The difference is zero only for a closed month. The page explains that edge in one line.

**Reference:** Stripe

### Writing a Moment

**ID:** `litespec/frontend/patterns/money-time-density#writing-a-moment`

**Strength:** default

**Question:** Every date uses the zone the screen works in. Each table keeps one form. The expenses table writes 8 Oct 2026, 09:32. A messages table that mixes Sent 6 hours ago with For 2 Jan 2030, 10:00 in one column breaks that rule.

**Default:** `one-form-per-table-with-the-zone-stated-once`

**Compact:** The recommended option reflows. One column carries the content. Header actions sit under the title. A table scrolls inside its own region.

**Rationale:** Keep one form per table. State the zone once in the header. A column then reads as a column.

The runner-up puts absolute over relative. It fits dispute and payout surfaces where recency leads. Never ship relative only on a reconciled page. Nothing there can be cited.

**Reviewed for:** Which built date text differs from the wireframe its catalog entry chose? Open the built absolute, relative, and weekday forms beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** Every row keeps the same shape. The header names the zone once. A column reads as a column.

**Cost:** Minutes add noise a week later. A reader cannot tell which zone a message used.

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

**Rationale:** A list of events takes the day as its shape. The date becomes a heading, not a column. The row keeps what matters.

**Cost:** It is no longer sortable as a column. It is no longer a table. Sorting moves to the toolbar. A group heading adds another row to read.

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

**Rationale:** Every moment carries its own zone. Expenses on one page come from offices in different zones. A trip in another zone is a real case.

**Cost:** A column of repeated offsets is hard to read. The alternative states the zone once. That alternative reads wrong for another zone.

### Choosing a Period

**ID:** `litespec/frontend/patterns/money-time-density#choosing-a-period`

**Strength:** default

**Question:** A statements page can put the period in the heading. It offers two plain buttons beside it. Nothing says which is previous and which is next. The period cannot be typed.

**Default:** `chevrons-either-side-of-the-period-as-the-heading`

**Compact:** The recommended option reflows. One column carries the content. Header actions sit under the title. A table scrolls inside its own region.

**Rationale:** Ship chevrons beside the heading with a month dropdown. Walking is one click and jumping avoids twelve clicks. The runner-up uses presets with typeable bounds. It fits charts and exports where the period is not a month. Never ship the period as a filter chip. The period is the subject, not a refinement.

**Reviewed for:** Which built period control differs from the wireframe its catalog entry chose? Open the built chevrons, tabs, and painted calendars beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The period is the heading. A chevron on each side walks months. A chevron cannot read as a tab.

**Cost:** Walking a quarter takes four clicks. A dropdown to jump stays beside it.

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

**Rationale:** Where the period is not a month a range control fits. It offers presets and two typeable dates. Carbon asks for a simple way to type. Nothing sits behind the calendar.

**Cost:** Presets plus bounds are two mechanisms. The used preset needs display afterwards. A reader can lose track of the period.

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

**Rationale:** The period sits as one filter among several. It sits with other narrowing choices. It removes like any of them.

**Cost:** The period is the page subject, not a refinement. A filter row demotes the one thing the report covers.

### How Much Fits Before It Breaks

**ID:** `litespec/frontend/patterns/money-time-density#how-much-fits-before-it-breaks`

**Strength:** default

**Question:** A table can run at 47 pixels a row. Three columns hold two lines each. At 390 pixels a five-column table has to drop a column. It truncates names to Rotterdam with an ellipsis unless the page changes shape.

**Default:** `the-full-width-with-nothing-truncated`

**Compact:** Under 640 pixels the table becomes a record list. One block per expense holds spender, trip, and amount. Newest comes first. Sorting by column goes with the table. The compact option is `at-390-pixels-a-record-list-rather-than-a-table`.

**Rationale:** Keep the table at desktop width. Use two-line cells with no truncation. Switch to the record list under 640 pixels. That pair answers honestly at both widths.

The runner-up wraps rather than truncates. It fits where one column holds names of any length. Never ship shortened figures near a statement. A rounded money figure is not a money figure.

**Reviewed for:** Which built dense table differs from the wireframe its catalog entry chose? Open the built 390-pixel lists and pinned first columns beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** At 1440 pixels the columns all have room. The trip is the second line of the expense cell. The spender email is the second line of the spender cell. Nothing needs an ellipsis.

**Cost:** Five columns reach the ceiling at this width. A seventh column forces a truncation. The second line is where it goes.

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

**Rationale:** A phone has no room for columns. One block per expense holds everything.

**Cost:** No sorting by column and no comparison across a column. On a phone a reader seeks one known expense. The list serves that case.

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

**Rationale:** The table keeps two facts per row. It keeps the expense and what it cost. The trip and spender wait on the record.

**Cost:** Rows of expense numbers and amounts cannot show spenders. It works only when the reader arrives with an expense number.

### Counting Things

**ID:** `litespec/frontend/patterns/money-time-density#counting-things`

**Strength:** default

**Question:** One rule governs this whole category: read a number the system can count; never type it. A hand-typed figure will not be updated and a reviewer will trust it anyway.

**Default:** `counts-in-the-sidebar-on-the-rows-that-hold-work`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep counts where the reader would otherwise fetch them. The sidebar rows that hold work carry them, and the tabs above a list carry them. The runner-up is the stat tiles with their measure stated.

It is the better choice for the home page where the question is how large the month is. Never ship counts with the report that produces them, since the report name is system language on a number page.

**Reviewed for:** Which built tally differs from the wireframe its catalog entry chose? Open the built sidebar counts, tiles, and dated claims beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** Reversals 10 in the sidebar, 1 thing to do on the home page. A count appears where a reader would otherwise have to go and get it.

**Cost:** Every count is a read on every page load. A count of 0 beside an area is an area that looks finished rather than unused.

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

**Rationale:** The home page shape is label, figure, and one line saying what the figure counts. Carbon describes a KPI as a number with a single word description, and 3 to 5 tiles is a defensible row.

**Cost:** A tile figure is a moment, and the next read is a different moment. Two colleagues looking at the same page can see different numbers with no explanation.

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

**Rationale:** Each figure names the report that reads it, so a reader who wants a number elsewhere can get it. A figure typed into a page is wrong from the next commit. These figures are read on demand.

**Cost:** A report name beside every figure is system language on a page where the reader only wants the number. It belongs in documentation, not in the product, unless the reader can open the report from it.

### Spending over Time

**ID:** `litespec/frontend/patterns/money-time-density#spending-over-time`

**Strength:** default

**Question:** A dashboard can answer how the quarter is going with a number. A number says where the budget stands. Only a line says whether it is speeding up or slowing down.

**Default:** `daily-columns-with-the-total-above`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Ship the daily columns with the total above and the peak labeled. They answer how much and when in one glance. The runner-up is the cumulative line against the previous quarter.

It is the better choice once the budget has a quarter to beat. Never ship the chart without its table, since a figure nobody can read exactly is a figure nobody can cite.

**Reviewed for:** Which built trend chart differs from the wireframe its catalog entry chose? Open the built columns, areas, and sparklines beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** Shopify sales reports open with the total and draw the days as columns, so the headline and the shape sit together. The peak day is labeled in words, which is what the company quotes in the review.

**Cost:** Thirty columns at phone width are a few pixels each, so the shape survives but the single day does not. The table behind the chart carries the exact day, not the chart.

**Reference:** Shopify

#### `cumulative-sales-against-the-previous-season`: Cumulative Spending Against the Previous Quarter

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

**Rationale:** Vercel analytics draw the previous period as a dashed line behind the current one, so the comparison needs no second chart. A claimed line above a dashed line is the whole story of a growing quarter.

**Cost:** A first-year budget has no previous quarter, so the page says that rather than drawing one line and calling it a comparison. The dashed line also invites reading a gap that is a calendar shift.

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

**Rationale:** Plausible dashboards mark the average across the bars, so a glance says which weeks pulled their weight. Weekly columns suit a company that plans payout runs by week, not by day.

**Cost:** Weeks split the story the quarter tells: kickoff, steady filing, and year-end close. A week that holds two of them averages them away, and the final two-day column is not a week at all.

**Reference:** Plausible

### Spending by Expense Kind

**ID:** `litespec/frontend/patterns/money-time-density#spending-by-expense-kind`

**Strength:** default

**Question:** The Rotterdam trip claims three expense kinds and a fee. The company asks which kind carries the quarter, and the answer is a share, not a total.

**Default:** `horizontal-bars-with-the-figure-on-each-row`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Ship the horizontal bars with the figure on each row. Every kind reads with its count and its money in one glance. The runner-up is the table with a bar in each row.

It is the better choice once kinds gain states such as disputed or reversed. Never ship the donut alone, since an angle nobody can read exactly is decoration and not a figure.

**Reviewed for:** Which built mix chart differs from the wireframe its catalog entry chose? Open the built bars, tiles, and donuts beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** Shopify product performance lists each variant with its bar and its money, so the ranking and the figures share one row. Every kind carries its amount, its count, and its claimed total, which is the whole question in one glance.

**Cost:** Bars share one scale, so the lodging EUR 3,150.00 is a sliver beside the travel. A second trip that matters needs its own scale, which this option refuses to give it.

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

**Rationale:** Notion progress columns put a bar inside the table cell, so the figure and its share never separate. The table also has room for the state each kind is in, which bars alone cannot carry.

**Cost:** Five columns need a wide page, so the table scrolls sideways on a phone. The bar in a scrolling table is the first thing cut off, which is where the share lives.

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

**Rationale:** A per-kind summary gives each kind its own tile with claimed and share. Three kinds read as three facts rather than one ranking. Tiles suit a home page where the by-variant question is asked in passing.

**Cost:** Tiles cannot be sorted or compared at a glance the way bars can, and a fourth kind wraps the row. The amount each tile needs makes every tile taller than the figure it carries.

### Spend Against the Budget

**ID:** `litespec/frontend/patterns/money-time-density#spend-against-the-budget`

**Strength:** default

**Question:** The quarter budget holds EUR 10,000 and EUR 9,412 is claimed. The distance between the two is the decision to move money, hold claims, or call the budget spent.

**Default:** `a-bar-with-sold-held-and-left-in-words`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Ship the progress bar with claimed, held, and left in words. One glance says how far the budget has burned and what is still movable. The runner-up is the per-kind spend table.

It is the better choice when one kind is gone and the others are not. Never ship the countdown alone, since EUR 468 left without the budget size hides whether the budget is nearly gone or barely touched.

**Reviewed for:** Which built fill-rate view differs from the wireframe its catalog entry chose? Open the built bars, countdowns, and capacity tables beside the catalog entry recording the chosen option and its reason.

#### `a-bar-with-sold-held-and-left-in-words`: A Bar with Claimed, Held and Left in Words

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

**Rationale:** A budget app draws spending as a bar against the budget with the counts in words, so the glance and the figures agree. Claimed, held, and left are three different decisions, which is why all three are named.

**Cost:** One bar hides which kind is gone, so a spent taxi kind behind a half-empty budget reads as room to spare. The per-kind rows answer that, not this bar.

#### `sell-through-per-variant`: Spend per Kind

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

**Rationale:** Shopify inventory by variant draws each variant against its own stock. One empty kind and two full ones are not the same as a third gone everywhere. Each row states what refilling that kind would take.

**Cost:** Three bars share no scale unless the budgets are close, and here they are not. The budget total has to sit above the rows or the budget reads as three budgets.

**Reference:** Shopify

#### `capacity-sold-held-and-left-as-a-table`: Capacity, Claimed, Held and Left as a Table

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

**Rationale:** Shopify inventory by location is a table because four figures per row need columns, not bars. Every row adds up across, and every column adds down, so finance can check the arithmetic both ways.

**Cost:** A table of four money-less counts reads as a ledger, which is what it is. It answers the audit and bores the glance, so the bar belongs above it on the page that holds both.

**Reference:** Shopify

### Figure Against an Earlier Period

**ID:** `litespec/frontend/patterns/money-time-density#figure-against-an-earlier-period`

**Strength:** default

**Question:** A tile that says EUR 63,350.00 answers how much. Only the tile that adds August EUR 48,340.00 beside it answers whether the month was good.

**Default:** `tiles-with-a-delta-pill-and-the-previous-figure`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Ship the tiles with a delta pill and the previous figure in words. The comparison is visible and the basis is stated. The runner-up is the tiles with sparklines.

It is the better choice on the home page where the shape of the month matters more than one earlier figure. Never ship a bare percentage with no basis, since up 12 percent without the basis is a claim and not a comparison.

**Reviewed for:** Which built comparison tile differs from the wireframe its catalog entry chose? Open the built deltas, sparklines, and KPI tables beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** Stripe home tiles compare each figure against the previous period with the basis stated under the delta. No reader asks compared to what. The pill carries direction and size. The words carry the basis.

**Cost:** Three tiles each with a pill, a basis, and a note are three small paragraphs, which is heavy for a home page. A fourth KPI wraps the row and the comparison wraps with it.

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

**Rationale:** Vercel usage tiles pair each meter with its sparkline, so the month reads as a shape rather than as two endpoints. A steady climb and a last-week spike end at the same figure and mean different things.

**Cost:** A sparkline shares no axis with its neighbors, so the reversal line can look calmer than the spending line while rising faster. The delta pill beside each one is still required.

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

**Rationale:** GOV.UK statistics releases state the period with no comparison until the next release exists, because a comparison without a fair basis misleads. A new company in its first month has no August, so the tile says when instead of versus.

**Cost:** A figure with no comparison answers how much and never whether it was good. The month this option suits is the first one. Every month after it wants the delta back.

**Reference:** GOV.UK

### When the Money Arrives

**ID:** `litespec/frontend/patterns/money-time-density#when-the-money-arrives`

**Strength:** default

**Question:** Finance holds the approved expenses and pays them out on payout days. Spenders plan around those days, so the schedule is a planning surface, not a history.

**Default:** `upcoming-payouts-with-the-sales-each-one-holds`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Ship the list of upcoming payouts with the expenses each one holds: the company sees what goes out and when in one place. The runner-up is the past payouts table with bank references, which is the better choice for finance reconciling against the bank.

Never ship the single next-payout tile as the page: one arrival without the ones after it cannot plan a month.

**Reviewed for:** Which built payout view differs from the wireframe its catalog entry chose? Open the built upcoming, weekly, and journey views beside the catalog entry recording the chosen option and its reason.

#### `upcoming-payouts-with-the-sales-each-one-holds`: Upcoming Payouts with the Expenses Each One Holds

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

**Rationale:** The Stripe payouts list pairs each payout with its arrival date and the expenses it holds. The company plans around arrivals rather than around filings. Each row states its state, because a payout building up is not a payout sent.

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

**Rationale:** QuickBooks groups deposits by week with a total per group, because payouts are planned by week. The group total is the figure the company carries into planning for the week.

**Cost:** A week that holds two payouts, which happens around holidays, gets a total that covers two arrivals. The group then needs its dates restated, which the flat list never needs.

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

**Rationale:** The Revolut Business payout history carries the bank reference on each row, because finance ticks each payout against a bank line. The reference is copyable, which is the whole job of this table.

**Cost:** A history answers reconciliation and nothing else: it says what went out, never what is coming. The upcoming list has to sit above it or the page plans nothing.

**Reference:** Revolut Business

### Vendor's Invoice to the Company

**ID:** `litespec/frontend/patterns/money-time-density#vendors-invoice-to-the-company`

**Strength:** default

**Question:** Billing holds what the company owes its software vendor: the Business subscription and anything metered. The invoice is the one document where the vendor is the seller. It reads as the sale of the vendor, not as the sale of the company.

**Default:** `the-invoice-as-a-document-with-the-usage-behind-each-line`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Ship the document-style invoice with the usage behind each line: finance sees what the fee paid for without opening another page. The runner-up is the paid invoice with its mandate reference, which is the better state of the same document once the money has moved.

Never ship the invoice without its VAT lines: a business invoice without VAT is not an invoice.

**Reviewed for:** Which built supplier bill differs from the wireframe its catalog entry chose? Open the built documents, states, and usage lines beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The Xero invoice preview is the document the customer receives, with each line carrying the quantity behind it. The company reads what the vendor charged and why in one place, which is the whole point of an invoice.

**Cost:** A document is a page of its own, so the invoice list needs the state of each invoice without opening it. The list carries the states. This page carries the lines.

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

**Rationale:** The Wise paid receipts carry the reference and the date above the lines. A filed invoice answers "when and how" without being read. The stamp states the state in the words of the document, not in the words of the dashboard.

**Cost:** A stamp is final in a way a badge is not, so a payment that later fails leaves a stamped invoice that lies. The state must follow the mandate, not the page the reader printed.

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

**Rationale:** The Stripe invoices table lists each invoice with its period, its amount, and its state. The list answers "do I owe anything" before any invoice is opened.

**Cost:** A list of invoices says nothing about what any invoice holds, so a surprising total needs the document. Each row opens the invoice, which must then carry the usage this list omits.

**Reference:** Stripe

### Two VAT Rates on One Page

**ID:** `litespec/frontend/patterns/money-time-density#two-vat-rates-on-one-page`

**Strength:** default

**Question:** Books carry 9% and a hotel night carries 21%. The spender sees one total. The accounts show two rates on the page the tax return is built from.

**Default:** `the-vat-split-under-the-order-total`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Ship the VAT split under the expense total. The spender total stays one figure and the books get both rates. The runner-up is the per-rate report with taxable bases, which is the better choice at quarter end when finance files the return.

Never ship amounts with no VAT note at all: a lawful amount states what the tax is.

**Reviewed for:** Which built mixed-rate view differs from the wireframe its catalog entry chose? Open the built splits, badges, and per-rate summaries beside the catalog entry recording the chosen option and its reason.

#### `the-vat-split-under-the-order-total`: The VAT Split Under the Expense Total

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

**Rationale:** The Coolblue receipts split VAT per rate under the total. One spender-facing figure opens into both rates. Each line states its base and its tax, which is what the return needs per rate.

**Cost:** Three more rows under every total, which is heavy on a receipt for two nights. The split belongs on the record and the export, not on every row of the expenses list.

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

**Rationale:** The Moneybird invoice lines carry their own rate. A mixed invoice is right on every line rather than in a summary that must be trusted. The rate badge sits where the eye checks the amount.

**Cost:** A rate column narrows every other column on a document that already has four of them. An invoice with one rate repeats the same badge on every line, which says nothing after the first.

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

**Rationale:** The Exact VAT return overview lists each rate with its base and its tax, because the return is filed per rate. The two quarter lines are the two lines finance copies into the filing.

**Cost:** A report per rate says nothing about which kinds produced it, so a surprising base needs the kind spending behind it. The report opens that spending instead of only stating the base.

**Reference:** Exact

### Numbers at Phone Width

**ID:** `litespec/frontend/patterns/money-time-density#numbers-at-phone-width`

**Strength:** default

**Question:** Alex Morgan checks the payout of the day from the office floor on a phone. At 390 pixels a money column, a date column, and a state badge do not fit side by side. The page chooses what the number needs beside it.

**Default:** `kpis-in-pairs-with-the-orders-under-them`

**Compact:** At 390 pixels the figures pair two across with the latest records under them. Tables become record lists with one right-aligned amount per row. Statements keep their alignment with short labels, and charts halve their axis labels. The compact option is `kpis-in-pairs-with-the-orders-under-them`.

**Rationale:** Ship the two-column KPI pair with the record list under it. The figures of the day stay visible and every expense keeps its amount right-aligned. The runner-up is the aligned statement. It is the better choice for finance pages where the adding up is the point.

Never ship the desktop table unchanged at 390: a table that scrolls sideways for its money column hides the answer.

**Reviewed for:** Which built narrow figure differs from the wireframe its catalog entry chose? Open the built pairs, per-row figures, and halved charts at 390 pixels beside the catalog entry recording the chosen option and its reason.

#### `kpis-in-pairs-with-the-orders-under-them`: KPIs in Pairs with the Expenses Under Them

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

**Rationale:** The Shopify mobile dashboard pairs its KPIs two across. Four figures fit above the fold of a phone without shrinking to decoration. The expenses under them keep one amount each, right-aligned, which is the column the list is about.

**Cost:** Two across at 390 pixels is 169 pixels a tile, so a long figure wraps or clips. Figures over ten characters need the single-column list, not the pair.

**Reference:** Shopify

#### `orders-as-records-with-amounts-right-aligned`: Expenses as Records with Amounts Right-Aligned

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

**Rationale:** The ING mobile activity list keeps one amount per row, right-aligned in tabular figures, so the money column survives without a table. The state sits under the name, where it is read second.

**Cost:** No sorting by column and no comparison across a column, because there are no columns. A reader hunting one expense they were told about is served. A reader auditing the day is not.

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

**Rationale:** The Xero mobile profit and loss keeps the statement shape at phone width, because money that adds up has to add up everywhere. Short labels and tabular figures keep the alignment of the wide option.

**Cost:** Labels short enough for 390 pixels lose the counts the full lines carry. The statement adds up but no longer says what it adds, which the full labels do.

**Reference:** Xero

### Showing an Identifier

**ID:** `litespec/frontend/patterns/money-time-density#showing-an-identifier`

**Strength:** default

**Question:** One design rule governs identifiers: an identifier shown on screen is labeled and copyable, and a raw code never reaches the screen. A payout page that shows SH-88213 as plain text breaks it, and so does a messages list showing identifiers where a subject should be.

**Default:** `a-code-monospaced-labeled-and-copyable`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Ship the labeled, monospaced, copyable code on the record. It is the shape of the rule and the one thing a support case needs quoted. The runner-up is the identifier as the record name. It is the better choice for records with no human name, such as a payout reference.

Never ship the page with no identifier: a record nobody can quote is a record support cannot help with.

**Reviewed for:** Which built code display differs from the wireframe its catalog entry chose? Open the built monospaced, labeled, and copyable codes beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The shared code shape: monospaced, never wrapped, with a copy control. A code a person reads aloud on a support call is a fact, not decoration.

**Cost:** A copy control on every row of a list is a control per row. And a code in a monospaced font is visibly different from a name, which is correct and slightly louder than the page around it.

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

**Rationale:** The label says Expense number rather than Reference, because a reference could be anything. A reader who knows what a thing is can use it. A reader who does not can ask.

**Cost:** Some identifiers have no natural name and end up labeled by their kind, which tells a reader nothing about where to use it.

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

**Rationale:** Where a record has no human name, the identifier is the heading. The code is what the record is called and it is never repeated as a field.

**Cost:** A code as a heading is not readable aloud. It is the right shape for a system where the code is all there is. It is the wrong shape for one where a person named the thing.
