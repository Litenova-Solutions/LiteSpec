# Record Detail Patterns

## Intent

One screen shows one record of the product, with its state, facts, history, and the commands a reader may run, and this page holds the patterns for that screen: header, tabs, summary, aside, timeline, relations, refusals, change history, notes, attachments, identifiers, phone header, neighbor movement, and the worked expense example. A team building a record screen starts from these patterns, an owner picks an option from the drawings, and the team records the choice in its pattern catalog, the file holding the project's interface decisions. One shared record shape keeps every record readable in the same order, so support finds the state and the next step without hunting.

## Concepts

### How to Read a Pattern

Each pattern answers one recurring question about a record screen, such as what the header states. It states the question, the default option a project builds, how the default behaves on a narrow screen, and why the default won. Each option then shows one wireframe drawing with its own rationale, cost, and optional reference. A project records its choice in its pattern catalog and builds the chosen option with its own components. The [UI patterns](../patterns.md) page states the catalog contract and the wireframe language.

### The Running Example

The drawings on this page use expense EXP-1042 for Ava: paid, EUR 125.00, part of the Rotterdam client visit. The header states the expense and its state, the sections state the spender, lines, and money, and the aside carries the history. The Standards section states the two rules every option keeps: a record header shows at most one primary action, and a destructive command never takes the primary place or style.

## Standards

### Show One Primary Action in a Record Header

**ID:** `litespec/frontend/patterns/record-detail#show-one-primary-action-in-a-record-header`

**Statement:** A record header wireframe MUST show at most one primary action.

**Rationale:** One primary action names the step readers take most often next on the record, such as Message spender on expense EXP-1042. Without that single answer, two equal buttons force the reader to compare labels on every visit, and the hurried click lands on the wrong command.

**Checked by:** `litespec patterns check` refuses a wireframe header holding two primary actions.

### Hold Built Record Headers to One Primary Action

**ID:** `litespec/frontend/patterns/record-detail#hold-built-record-headers-to-one-primary-action`

**Statement:** A built record header MUST show at most one primary action.

**Rationale:** The wireframe check stops at the drawing: a built header can still ship two primary actions where the drawing showed one. One primary action on the built header keeps the next step a single answer on the screen readers use.

**Reviewed for:** Which built record header holds more than one primary action? Open the built record screens and count the primary actions.

### Keep Destructive Commands Out of the Primary Place

**ID:** `litespec/frontend/patterns/record-detail#keep-destructive-commands-out-of-the-primary-place`

**Statement:** A destructive command MUST NOT take the primary action's place or style in a record header.

**Rationale:** The primary place draws the hurried click, so it holds only the safe next step. A destructive command styled or placed as primary turns a routine visit into a retired policy or a voided expense, with no step between intent and loss.

**Reviewed for:** Which built record header hands a destructive command the primary style? Open the headers on built record screens and read each command's tone.

## Patterns

### Record Header

**ID:** `litespec/frontend/patterns/record-detail#record-header`

**Question:** One record, stated at the top. A record header always carries a title. It carries a breadcrumb when the record has a parent. It offers exactly one primary action, with everything else secondary beside it. A common failure puts a destructive action where the primary should be.

**Default:** `title-state-breadcrumb-one-primary-action`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the title, state, breadcrumb, and one primary action. The state beside the title answers the first question on every record. The runner-up is the overflow menu option. It is the better choice once a record carries four or more commands.

Prefer the state-driven primary on records with a clear next step, such as an expense without a receipt. Never ship the title-less option. It fails the check for exactly one `h1` per page.

**Reviewed for:** Which built title row differs from the wireframe its catalog entry chose? Open the built states, facts, and overflow menus beside the catalog entry recording the chosen option and its reason.

#### `title-state-breadcrumb-one-primary-action`: Title, State, Breadcrumb, One Primary Action

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Submitted 8 Oct 2026, 09:32 by Ava of Harbor and Pine."
    action "Resend the receipt"
    action "Reverse payout"
    action primary "Message spender"
  region "The spender"
    fact "Name" "Ava"
    fact "Email" "ava@harbor-pine.example"
    fact "Country" "Netherlands"
```

**Rationale:** This option follows the shape Polaris describes for a Claimwick expense. The state badge sits beside the title so the record's state is read before anything else.

**Cost:** Two actions of the same weight sit beside the primary. The destructive one shares a visual group with a harmless one unless the tone separates them.

**Reference:** Shopify Polaris

#### `title-and-state-only-no-description`: Title and State Only, No Description

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    action "Resend the receipt"
    action primary "Reverse payout"
  region "The expense"
    fact "Trip" "Rotterdam client visit"
    fact "Submitted" "8 Oct 2026, 09:32"
    fact "Paid" "EUR 125.00"
```

**Rationale:** The heading and its state stand with nothing between them and the content. The trail already says where the record is, so the description repeats it.

**Cost:** A record with a long name or a subtle state loses the one line. That line says which trip this expense belongs to, which is the most-asked question.

#### `state-as-a-badge-under-the-title`: State as a Badge Under the Title

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Submitted 8 Oct 2026, 09:32 by Ava."
    action primary "Message spender"
  region "The expense"
    fact "Trip" "Rotterdam client visit"
    fact "Paid" "EUR 125.00"
```

**Rationale:** The state sits on its own line under the heading rather than inline with it. A long record name wraps without dragging the badge down with it.

**Cost:** Two lines where one would do, and the badge is below the fold on a long title. Polaris puts the state in the title metadata slot, which is the inline arrangement.

**Reference:** Shopify Polaris

#### `destructive-action-in-its-own-place-separated`: Destructive Action in Its Own Place, Separated

```wireframe
page
  breadcrumb "Home" "Policies" "Travel spending"
  header
    heading "Travel spending"
    status "Active" positive
    text "What this policy allows, and the ceiling it sets."
    action "Pause policy"
    menu "More"
      action "Rename the policy"
      action "Duplicate the policy"
      action destructive "Retire"
  region "Details"
    fact "Open exceptions" "1"
    fact "Expenses" "412 under this policy"
    fact "Decided" "9,412 in total"
    fact "Ceiling" "EUR 45.00"
```

**Rationale:** This option puts Retire immediately beside Pause policy for a spending policy, both in the header. Separating the destructive one into a row menu or a lower group keeps the primary slot for the reversible command.

**Cost:** A destructive action behind a menu is three clicks from a misclick, which is the other failure. Tone plus position is the usual answer, and neither is complete alone.

#### `facts-in-the-header-commands-beside-them`: Facts in the Header, Commands Beside Them

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · Rotterdam client visit · 8 Oct 2026, 09:32"
    fact "Paid" "EUR 125.00"
    fact "Receipts" "2, both readable"
    fact "Method" "Bank transfer"
    fact "Reversals" "None"
    action primary "Message spender"
    action "Resend the receipt"
    action "Reverse payout"
  region "Receipts"
    list 2
      item: text "Train fare" · text muted "one receipt" · text muted "RC-0C893968A2" · status "Attached" neutral · action "Detach"
```

**Rationale:** The record's own values sit in the header as a definition list, so a reader never scrolls to learn what they are looking at. The commands stack beside it.

**Cost:** A tall header pushes the content below the fold on a phone. A four-fact list competes with the trail for the same vertical space.

#### `no-title-because-the-trail-is-the-title`: No Title, Because the Trail Is the Title

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  region "The spender"
    fact "Name" "Ava"
    fact "Email" "ava@harbor-pine.example"
    fact "Submitted" "8 Oct 2026, 09:32"
    fact "Paid" "EUR 125.00"
  region "Receipts"
    list 2
      item: text "Train fare" · text muted "one receipt" · status "Attached" neutral
```

**Rationale:** The breadcrumb names the record and the page opens straight into its sections. The largest possible use of the space goes to the content.

**Cost:** Fails WCAG 2.4.8 Location in spirit and reads as a bare panel. Every page carries exactly one `h1`, which automated checks enforce, so this fails that check.

#### `title-state-and-who-saved-it-last`: Title, State, and Who Saved It Last

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Submitted 8 Oct 2026, 09:32 by Ava of Harbor and Pine."
    text "Last saved by Cara at 09:34."
    action "Resend the receipt"
    action "Reverse payout"
    action primary "Message spender"
  region "The expense"
    fact "Spender" "Ava"
    fact "Trip" "Rotterdam client visit"
    fact "Submitted" "8 Oct 2026, 09:32"
    fact "Paid" "EUR 125.00"
```

**Rationale:** Fewer overwrite conflicts. Ben and Cara share an expense, so the header says who touched it last and when before anyone acts on it. A reader who sees Cara saved at 09:34 asks before saving over her.

**Cost:** One more line in the header on every record, and it is only honest if the data tracks the last writer. On records nobody shares it is noise.

#### `one-primary-the-rest-in-an-overflow-menu`: One Primary, the Rest in an Overflow Menu

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Submitted 8 Oct 2026, 09:32 by Ava of Harbor and Pine."
    action primary "Message spender"
    menu "More"
      action "Resend the receipt"
      action "Reverse to the original account"
      action "Download the receipt"
      action destructive "Void the expense"
  region "The expense"
    fact "Spender" "Ava"
    fact "Trip" "Rotterdam client visit"
    fact "Submitted" "8 Oct 2026, 09:32"
    fact "Paid" "EUR 125.00"
```

**Rationale:** Stripe draws one primary action on a charge and folds the rest behind an overflow menu. The header stays one line however many commands a record gains. Resend, reverse, and export all fit without a second row of buttons.

**Cost:** Secondary commands are a click further away, and a reader cannot tell what the menu holds without opening it. The menu also needs its items ordered by frequency, which is a judgment per record.

**Reference:** Stripe

#### `the-primary-follows-the-state`: The Primary Follows the State

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1031"
  header
    heading "EXP-1031"
    status "Awaiting receipt" caution
    text "Submitted 8 Oct 2026, 09:30 by Eli. The receipt is still missing."
    action "Message spender"
    action primary "Attach the receipt"
  region "The expense"
    fact "Spender" "Eli"
    fact "Submitted" "8 Oct 2026, 09:30"
    fact "Due" "EUR 40.45"
  region "What happens next"
    text "The receipt grace ends Friday at 18:00. After that the expense returns to draft and reads Expired."
```

**Rationale:** Shopify changes the primary action of a record with its state, so the next step is always the button. An expense without a receipt offers Attach the receipt; a reimbursed one offers Message spender; a disputed one offers Answer finance.

**Cost:** The primary moves meaning between visits, so a reader who learned its place has to read its label every time. Every state needs its own chosen next step, which is a decision per record type.

**Reference:** Shopify

### Record Tabs

**ID:** `litespec/frontend/patterns/record-detail#record-tabs`

**Question:** A record holds at most three sections, and further facets become tabs. A policy record carries four facets. Tab sets stay small, at six at most, and the active tab stays in the URL.

**Default:** `four-tabs-with-the-active-one-in-the-url`

**Compact:** Below the md breakpoint the tab row becomes a select naming the current facet. The panels stay the same. The compact option is `a-select-on-a-phone-tabs-on-a-wide-screen`.

**Rationale:** Keep the four tabs with the active one in the URL. Four stays inside every cited ceiling, and each panel repeats its tab label as a heading. The runner-up is the side rail option. It is the better choice past five facets or where one facet is a long form.

Prefer the counts option only where a facet can be empty and the count saves a wasted click. Never ship the icon tabs. Icons are forbidden in tab labels, and they add a second channel for one meaning.

**Reviewed for:** Which built facet row differs from the wireframe its catalog entry chose? Open the built counts, icons, and phone selects beside the catalog entry recording the chosen option and its reason.

#### `four-tabs-with-the-active-one-in-the-url`: Four Tabs with the Active One in the URL

```wireframe
page
  breadcrumb "Home" "Policies" "Travel spending"
  header
    heading "Travel spending"
    status "Active" positive
    text "What this policy allows, and the ceiling it sets."
    action "Pause policy"
    action destructive "Retire"
  tabs "Ceilings" "Scopes" "Appeals" "Details"
    region "Details"
      fact "Policy" "TRAVEL-01"
      fact "Scope" "Travel cost centre"
      fact "Ceiling" "EUR 45.00"
```

**Rationale:** Four facets in tabs, which is inside the six every cited system allows. GOV.UK also asks for a heading inside each panel repeating the tab label, which the Details tab does.

**Cost:** A reader cannot see two facets at once, and GOV.UK says tabs are the wrong choice when comparing across facets is the job.

**Reference:** GOV.UK

#### `three-tabs-the-fourth-becomes-a-page`: Three Tabs, the Fourth Becomes a Page

```wireframe
page
  breadcrumb "Home" "Policies" "Travel spending"
  header
    heading "Travel spending"
    status "Active" positive
    text "What this policy allows, and the ceiling it sets."
    action "Pause policy"
  tabs "Ceilings" "Scopes" "Appeals"
    region "Ceilings"
      fact "Standard ceiling" "EUR 45.00"
      fact "Receipt threshold" "EUR 25.00"
      action "Change the ceiling"
      link "Policy text and what each rule holds"
```

**Rationale:** Hold the ceiling at three facets in tabs and push the fourth to its own route. Every cited system treats six as the ceiling; holding to three is the same discipline applied earlier.

**Cost:** One more navigation step for the reader who wants details. The fourth page joins a system that already counts its routes in the hundreds.

#### `tabs-with-counts`: Tabs with Counts

```wireframe
page
  breadcrumb "Home" "Policies" "Travel spending"
  header
    heading "Travel spending"
    status "Active" positive
    action "Pause policy"
  tabs "Ceilings" "Exceptions 3" "Appeals 0" "Details"
    region "Ceilings"
      fact "Standard ceiling" "EUR 45.00"
      text muted "412 expenses under this policy"
```

**Rationale:** Each tab carries the number of things in it, so a reader knows whether a facet is worth opening before opening it. The example shows four tabs where one holds three and another holds none.

**Cost:** Counts on tabs are a claim that has to stay true. A count of 0 invites a click that leads to an empty state.

#### `tabs-with-icons-as-well-as-words`: Tabs with Icons as Well as Words

```wireframe
page
  breadcrumb "Home" "Policies" "Travel spending"
  header
    heading "Travel spending"
    status "Active" positive
    action "Pause policy"
  tabs "Ceilings" "Scopes" "Appeals" "Details"
    region "Ceilings"
      fact "Standard ceiling" "EUR 45.00"
```

**Rationale:** An icon per tab so the row is scannable without reading, with the words still present for anyone who does not recognize the icons.

**Cost:** Carbon's tab guidance is explicit that icons are not permitted in tab labels. Two channels for one thing, and a second set of icons to design and maintain.

#### `scrollable-tabs-with-a-fade-at-the-edge`: Scrollable Tabs with a Fade at the Edge

```wireframe
page
  breadcrumb "Home" "Expenses" "Rotterdam client visit"
  header
    heading "Rotterdam client visit"
    status "Scheduled" info
    action "Reschedule"
  tabs "Overview" "Expenses" "Schedule" "Travel" "Money" "Operations" "Receipts"
    region "Overview"
      fact "Date" "Sat 14 Mar 2026"
      fact "Place" "Rotterdam client site"
      fact "Budget" "EUR 2,000"
```

**Rationale:** Where a record genuinely needs six facets, the row scrolls horizontally with a fade at the edge so the reader knows there is more, which is useful on a phone, where four tabs wrap.

**Cost:** A hidden seventh tab is a hidden feature, and a horizontal scroll on a page hides content from anyone who never swipes.

#### `tabs-below-the-summary-not-below-the-header`: Tabs Below the Summary, Not Below the Header

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · Rotterdam client visit · 8 Oct 2026, 09:32 · EUR 125.00"
    action primary "Message spender"
  tabs "Lines 3" "Payment" "History"
    region "Lines"
      list 3
        item: text "Train fare" · text muted "one receipt" · text muted "RC-0C893968A2" · status "Attached" neutral
```

**Rationale:** The summary stays visible above the tab row. A reader always knows which record they are on and what state it is in, whichever facet is open.

**Cost:** The tab row moves down the page, so it is less prominent. The summary has to be short or the tabs fall below the fold.

#### `facets-in-a-side-rail-instead-of-tabs`: Facets in a Side Rail Instead of Tabs

```wireframe
page
  breadcrumb "Home" "Policies" "Travel spending"
  header
    heading "Travel spending"
    status "Active" positive
    text "What this policy allows, and the ceiling it sets."
    action "Pause policy"
  split 1:3
    nav side
      link current "Ceilings"
      link "Scopes"
      link "Appeals"
      link "Details"
      link "Messages"
      link "Audit"
    region "Ceilings"
      fact "Standard ceiling" "EUR 45.00"
      fact "Receipt threshold" "EUR 25.00"
      action "Change the ceiling"
```

**Rationale:** GitHub draws repository settings as a side rail of links rather than a tab row. The rail holds a dozen facets without scrolling and keeps the panel wide enough for a form. The current facet reads as selected in the rail, and each facet keeps its own URL.

**Cost:** The rail takes about 200 px from every facet at desktop width. On a phone it has to become a select or stack above the panel. That is a second arrangement to keep true.

**Reference:** GitHub

#### `a-select-on-a-phone-tabs-on-a-wide-screen`: A Select on a Phone, Tabs on a Wide Screen

```wireframe
page
  breadcrumb "Home" "Policies" "Travel spending"
  header
    heading "Travel spending"
    status "Active" positive
    action "Pause policy"
  tabs "Ceilings" "Scopes" "Appeals" "Details"
    region "Ceilings"
      fact "Standard ceiling" "EUR 45.00"
      text muted "412 expenses under this policy"
```

```wireframe compact
page
  breadcrumb "Home" "Policies" "Travel spending"
  header
    heading "Travel spending"
    status "Active" positive
    action "Pause policy"
  field "Facet" select = "Ceilings"
  region "Ceilings"
    fact "Standard ceiling" "EUR 45.00"
    text muted "412 expenses under this policy"
```

**Rationale:** Polaris collapses its tab row into a select on narrow screens, so four facets fit a 390 px viewport without scrolling or wrapping. The control names the current facet and opens the same panels the tabs open.

**Cost:** Two controls for one choice, so the tab state and the select state have to stay in agreement. A reader on a phone also cannot see the other facets without opening the control.

**Reference:** Shopify Polaris

### Record Summary

**ID:** `litespec/frontend/patterns/record-detail#record-summary`

**Question:** The section at the top of a record answers what the reader is looking at without scrolling. It states the record's identity. A summary that states supporting information answers the wrong question.

**Default:** `facts-about-the-record-itself`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the facts about the record itself. A trip summary states when, where, and what it may spend, which is the identity of the record rather than its support. The runner-up is the copyable identifiers option. It is the better choice on records a reader quotes elsewhere, such as an expense read out on the phone.

Prefer the progress bar summary only for the one figure the company watches daily. Never ship the heading-less value row. Values with no section break the rule that a section is the only box.

**Reviewed for:** Which built fact block differs from the wireframe its catalog entry chose? Open the built key values, figures, and tiles beside the catalog entry recording the chosen option and its reason.

#### `facts-about-the-record-itself`: Facts About the Record Itself

```wireframe
page
  breadcrumb "Home" "Expenses" "Rotterdam client visit"
  header
    heading "Rotterdam client visit"
    status "Scheduled" info
    text "Sat 14 Mar 2026, 08:00 to 12:00 · Rotterdam client site"
    action "Reschedule"
    action primary "Add an expense"
  region "The trip"
    fact "When" "Sat 14 Mar 2026, 08:00 to 12:00"
    fact "Where" "Rotterdam client site"
    fact "Budget" "EUR 2,000"
    fact "Company" "Harbor and Pine"
    fact "Claimed" "EUR 1,800 of EUR 2,000"
```

**Rationale:** The summary holds the facts that define this record: its date, its place, its state, and what it holds. Everything else is a section below it.

**Cost:** Repeats what the header already says, since the header carries the title and state. Saying a thing twice is what the design contract forbids.

#### `one-line-of-key-values-no-heading`: One Line of Key Values, No Heading

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    action primary "Message spender"
  row
    text "Spender Ava"
    text "Trip Rotterdam client visit"
    text "Submitted 8 Oct 2026, 09:32"
    text "Paid EUR 125.00"
  region "Receipts"
    list 1
      item: text "Train fare" · text muted "one receipt" · status "Attached" neutral
```

**Rationale:** A single row of label and value pairs stands with no section around it, because a summary is not a block of content. It restates the heading.

**Cost:** Breaks the rule that a section is the only box, because the values would float on the canvas with nothing holding them together visually.

#### `money-summary-as-a-statement`: Money Summary as a Statement

```wireframe
page
  breadcrumb "Home" "Reports" "Statements" "September 2026"
  header
    heading "September 2026"
    text "Every euro of the month under the kind it is."
    action "Download"
  region
    fact "Claimed expenses" "EUR 54,180.00"
    fact "Per-diem extras" "EUR 6,020.00"
    fact "Total claimed" "EUR 60,200.00"
    fact "The bank fee" "EUR -1,143.80"
    fact "Total fees" "EUR -1,143.80"
    fact "Paid to spenders" "EUR 59,056.20"
```

**Rationale:** Where the record is a settlement, the summary is a statement: what came in, what went out, and what is left. It adds up top to bottom, which is what the reader came for.

**Cost:** Wrong for a record with no money, and a statement is longer than a summary ought to be, so it pushes the sections down.

#### `summary-with-a-figure-and-a-sub-line-each`: Summary with a Figure and a Sub-Line Each

```wireframe
page
  breadcrumb "Home" "Expenses" "Rotterdam client visit"
  header
    heading "Rotterdam client visit"
    status "Scheduled" info
    action primary "Add an expense"
  region "The trip"
    fact "When" "Sat 14 Mar 2026, 08:00 to 12:00"
    text muted "In 5 days"
    fact "Where" "Rotterdam client site"
    text muted "EUR 2,000"
    fact "Claimed" "EUR 1,800"
    text muted "Across every expense"
    fact "Money so far" "EUR 60,200.00"
    text muted "Travel and meals"
```

**Rationale:** Each fact carries a figure and a quieter line under it, as a figure row with a sub-line: a count beside its unit and a money figure beside what it is.

**Cost:** A figure and a sub-line per fact need three columns or a wide row. The sub-lines repeat the label meaning in more words.

#### `summary-with-a-progress-bar-for-the-one-number-that-matters`: Summary with a Progress Bar for the One Number That Matters

```wireframe
page
  breadcrumb "Home" "Expenses" "Rotterdam client visit"
  header
    heading "Rotterdam client visit"
    status "Scheduled" info
    text "Sat 14 Mar 2026, 08:00 to 12:00 · Rotterdam client site"
    action primary "Add an expense"
  region "Budget"
    figure "Claimed of EUR 2,000" "EUR 1,800"
    text "EUR 200 left"
    action "Raise the budget"
    action "Add an expense"
  region "Details"
    fact "When" "Sat 14 Mar 2026"
    fact "Where" "Rotterdam client site"
    fact "Company" "Harbor and Pine"
```

**Rationale:** One number, `budget left`, carries a bar and a count. Everything else is plain text, because one figure is worth watching and four are not.

**Cost:** Choosing which number matters is a judgment the data has to support. A bar on a capacity figure implies a ratio that stops mattering once loading is over.

#### `no-summary-straight-to-sections`: No Summary, Straight to Sections

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · Rotterdam client visit · 8 Oct 2026, 09:32"
    action primary "Message spender"
  region "The spender"
    fact "Name" "Ava"
    fact "Email" "ava@harbor-pine.example"
    fact "Phone" "Not given"
    fact "Country" "Netherlands"
  region "What was claimed"
    fact "Train fare, 2 x EUR 45.00" "EUR 90.00"
    fact "Booking fee" "EUR 10.00"
    fact "Breakfast, paid at the desk" "EUR 25.00"
    fact "Paid" "EUR 125.00"
```

**Rationale:** The header already states the record and its state, so the summary is dropped. Every fact appears once, in the section it belongs to.

**Cost:** The reader scrolls to learn what the record is. For a long record that is a real cost, and the record-page shape reserves a summary slot for exactly this reason.

#### `facts-plus-copyable-identifiers`: Facts Plus Copyable Identifiers

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Submitted 8 Oct 2026, 09:32 by Ava of Harbor and Pine."
    action primary "Message spender"
  region "The expense"
    fact "Spender" "Ava"
    fact "Trip" "Rotterdam client visit"
    fact "Submitted" "8 Oct 2026, 09:32"
    fact "Paid" "EUR 125.00"
    fact "Expense" "EXP-1042"
    action quiet "Copy"
    fact "Payment" "SH-88213"
    action quiet "Copy"
    fact "Email" "ava@harbor-pine.example"
    action quiet "Copy"
```

**Rationale:** Stripe puts a copy button beside every identifier on a charge. Support reads a reference out on the phone or pastes it into a dispute. The human facts stay plain text; the expense number, payment reference, and spender email copy with one click.

**Cost:** A copy button per row is visual noise on a record nobody quotes. Each button needs its copied confirmation, which is a small behavior per row.

**Reference:** Stripe

#### `four-tiles-instead-of-a-section`: Four Tiles Instead of a Section

```wireframe
page
  breadcrumb "Home" "Expenses" "Rotterdam client visit"
  header
    heading "Rotterdam client visit"
    status "Scheduled" info
    text "Sat 14 Mar 2026, 08:00 to 12:00 · Rotterdam client site"
    action "Reschedule"
    action primary "Add an expense"
  row
    figure "Claimed of EUR 2,000 budget" "EUR 1,800"
    figure "Left, 10 percent of the budget" "EUR 200"
    figure "Money so far, travel and meals" "EUR 60,200.00"
    figure "Days to travel, booking ends 13 Mar 23:59" "5"
  region "The trip"
    fact "When" "Sat 14 Mar 2026, 08:00 to 12:00"
    fact "Where" "Rotterdam client site"
    fact "Company" "Harbor and Pine"
```

**Rationale:** Vercel opens a project on four stat tiles. Four figures side by side compare at a glance where a fact list reads line by line. Claimed, left, money so far, and days to travel are the four finance checks each morning.

**Cost:** Tiles carry figures, not sentences, so anything that needs a qualifier does not fit. A tile row also answers nothing about who or where, so the facts still need a home below it.

**Reference:** Vercel

### Record Aside

**ID:** `litespec/frontend/patterns/record-detail#record-aside`

**Question:** A record page has an aside slot for what supports the record rather than describing it. An expense record puts its history there, which is the right use. A record with no supporting content leaves the slot empty.

**Default:** `history-in-the-aside-sections-in-the-main-column`

**Compact:** Below the `md` breakpoint the aside stacks as sections under the header, with history first. The compact option is `aside-collapses-to-a-section-on-narrow-screens`.

**Rationale:** Give the pick to history in the aside. History supports the expense without describing it, which is exactly what the aside slot is for. The runner-up is the decision panel. It is the better choice on records that wait for someone, such as a dispute with a deadline.

Prefer the people and labels aside on records a team hands between shifts. Never ship the empty aside state by accident. An expense with nothing pending and no history reads as a rendering fault.

**Reviewed for:** Which built side column differs from the wireframe its catalog entry chose? Open the built history, decisions, and related asides beside the catalog entry recording the chosen option and its reason.

#### `history-in-the-aside-sections-in-the-main-column`: History in the Aside, Sections in the Main Column

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · Rotterdam client visit · 8 Oct 2026, 09:32"
    action primary "Message spender"
  split 2:1
    region "The spender"
      fact "Name" "Ava"
      fact "Email" "ava@harbor-pine.example"
    aside
      region "What happened"
        timeline 3
          item: text "Receipt attached" · text muted "14 Mar 2026, 10:41" · text muted "Ava"
```

**Rationale:** History supports the expense without describing it, and it reads top to bottom beside the facts. On a wide screen it is a narrow column; below that it stacks under the sections.

**Cost:** Below about 1000 pixels the aside stacks under everything, so the history a reader most wants lands after the sections they came to use.

#### `aside-for-what-needs-a-decision`: Aside for What Needs a Decision

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1027"
  header
    heading "EXP-1027"
    status "Recall refused" caution
    text "Eli · Rotterdam client visit · 7 Oct 2026, 22:14"
    action primary "Message spender"
  split 2:1
    region "Money"
      fact "Paid to Eli" "EUR 45.00"
      fact "Recall asked for" "EUR 45.00"
      fact "Returned so far" "EUR 0.00"
    aside
      region "Needs a decision"
        message "The recall window has closed. This bank reverses only within 5 days." caution
        action "Deduct from the next payout"
        action "Record a repayment you received"
      region "History"
        timeline 2
          item: text "Receipt attached" · text muted "14 Mar 2026, 10:41" · text muted "Ava"
```

**Rationale:** The aside holds the things that need the reader: a pending decision, a clock, and a mismatch. History moves into a tab, because a list of past activity is rarely the reason to open a record.

**Cost:** The aside becomes a work panel, which changes what the page is. An expense with nothing pending has an empty aside, which is its own awkward state.

#### `no-aside-sections-fill-the-width`: No Aside, Sections Fill the Width

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · Rotterdam client visit · 8 Oct 2026, 09:32"
    action primary "Message spender"
  row
    region "The spender"
      fact "Name" "Ava"
      fact "Email" "ava@harbor-pine.example"
      fact "Country" "Netherlands"
    region "The expense"
      fact "Trip" "Rotterdam client visit"
      fact "Submitted" "8 Oct 2026, 09:32"
      fact "Paid" "EUR 125.00"
  region "What happened"
    timeline 5
      item: text "Bank feed lost its connection" · text muted "14 Mar 2026, 10:10 to 10:40" · text muted "Claimwick"
```

**Rationale:** The main column takes everything, with sections side by side where there are two that fit.

**Cost:** A two-line fact list stretched across 1200 pixels is a long measure for a name and an email. The history loses its link to the record.

#### `aside-sticky-main-column-scrolls`: Aside Sticky, Main Column Scrolls

```wireframe
page
  breadcrumb "Home" "Expenses" "Disputes" "D-0182"
  header
    heading "Dispute D-0182"
    status "Disputed" critical
    text "EUR 45.00 on expense EXP-1019. Ben opened a case on 2 Oct 2026."
    action primary "Answer the approver"
  split 2:1
    region "What Ben said"
      text "Ben says the hotel breakfast was personal and should not be claimed."
      text muted "Received 2 Oct 2026, 14:22"
    aside
      region "Needs a decision"
        message "13 days to answer. The expense is rejected on 21 Oct if nothing is sent." critical
        action "Answer the approver"
```

**Rationale:** The aside stays in place while the sections scroll, so a decision in the aside is always reachable from anywhere on a long record.

**Cost:** A sticky aside on a phone is a stacked block that no longer sticks. The behavior has two shapes and one of them is untested in practice.

#### `aside-for-related-records`: Aside for Related Records

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · Rotterdam client visit"
    action primary "Message spender"
  split 2:1
    region "What was claimed"
      fact "Train fare, 2 x EUR 45.00" "EUR 90.00"
      fact "Booking fee" "EUR 10.00"
      fact "Breakfast, paid at the desk" "EUR 25.00"
      fact "Paid" "EUR 125.00"
    aside
      region "With this trip"
        list 3
          item: text "Rotterdam client visit" · text muted "Sat 14 Mar 2026" · text muted "Rotterdam client site" · action "Open"
```

**Rationale:** The aside holds what relates to this record but is not part of it. That is the trip it claims on, the other lines on it, and the messages it has had.

**Cost:** Related records are what a reader navigates to, so putting them in the narrowest column makes them the least likely to be found.

#### `aside-collapses-to-a-section-on-narrow-screens`: Aside Collapses to a Section on Narrow Screens

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · Rotterdam client visit"
    action primary "Message spender"
  region "What happened"
    timeline 3
      item: text "Receipt attached" · text muted "14 Mar 2026, 10:41" · text muted "Ava"
  region "The spender"
    fact "Name" "Ava"
    fact "Email" "ava@harbor-pine.example"
  region "What was claimed"
    fact "Train fare, 2 x EUR 45.00" "EUR 90.00"
    fact "Booking fee" "EUR 10.00"
    fact "Breakfast, paid at the desk" "EUR 25.00"
    fact "Paid" "EUR 125.00"
```

**Rationale:** The same content, ordered so the aside's sections come first in the DOM. A reader on a phone reads history before the lines list rather than after it.

**Cost:** Two different orders for one record, so a screenshot test holds two arrangements. The DOM order and the visual order disagree, which is also an accessibility question.

#### `people-and-labels-in-the-aside-history-below`: People and Labels in the Aside, History Below

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · Rotterdam client visit · 8 Oct 2026, 09:32"
    action primary "Message spender"
  split 2:1
    region "The spender"
      fact "Name" "Ava"
      fact "Email" "ava@harbor-pine.example"
    aside
      region "Ownership"
        avatar "B"
        fact "Waiting for" "Ben"
        fact "Labels" "receipt-check"
        fact "Follow up" "Mon 12 Oct 2026"
      region "What happened"
        timeline 3
          item: text "Receipt attached" · text muted "14 Mar 2026, 10:41" · text muted "Ava"
```

**Rationale:** Linear puts assignee, state, labels, and dates in the issue sidebar, because a record a team hands between shifts needs its ownership beside its facts. Cara sees at a glance that this expense waits for Ben under the label `receipt-check`, and the history still reads below the handover.

**Cost:** Ownership fields need data that tracks them, and on records one person owns end to end they repeat what the team already knows. The aside also grows long once labels, dates, and watchers all claim a row.

**Reference:** Linear

#### `related-records-and-history-share-the-aside`: Related Records and History Share the Aside

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · Rotterdam client visit · 8 Oct 2026, 09:32"
    action primary "Message spender"
  split 2:1
    region "What was claimed"
      fact "Train fare, 2 x EUR 45.00" "EUR 90.00"
      fact "Booking fee" "EUR 10.00"
      fact "Breakfast, paid at the desk" "EUR 25.00"
      fact "Paid" "EUR 125.00"
    aside
      region "Related"
        list 2
          item: text "Rotterdam client visit" · text muted "Sat 14 Mar 2026" · text muted "Rotterdam client site" · action "Open"
      region "What happened"
        timeline 2
          item: text "Receipt attached" · text muted "14 Mar 2026, 10:41" · text muted "Ava"
```

**Rationale:** HubSpot draws associated records and the activity timeline in one right rail. The aside answers both who this record touches and what happened to it. The trip, the other expense from this spender, and the messages sit above a short history with a link to the rest.

**Cost:** Two different jobs in one column, so neither gets the width it wants. Related records compete with history for the top of the aside, and the loser sits below the fold on a short viewport.

**Reference:** HubSpot

### Record Timeline

**ID:** `litespec/frontend/patterns/record-detail#record-timeline`

**Question:** What happened to one record. Entries read newest first. Every entry carries a timestamp and a byline, because the reader asks who did this and when.

**Default:** `newest-first-with-actor-and-timestamp`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the newest-first timeline with actor and timestamp. It answers who and when on every line. The runner-up is the filtered timeline. It is the better choice once a record carries dozens of entries across kinds.

Prefer the day-grouped option for trip records, where entries cluster on one morning. Never ship the actor-grouped timeline as the main history. It destroys the chronological order a timeline exists to give.

**Reviewed for:** Which built event feed differs from the wireframe its catalog entry chose? Open the built day groups, diffs, and note mixes beside the catalog entry recording the chosen option and its reason.

#### `newest-first-with-actor-and-timestamp`: Newest First with Actor and Timestamp

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · Rotterdam client visit"
  region "What happened"
    timeline 4
      item: text "Paid by bank transfer" · text muted "8 Oct 2026, 09:34" · text muted "Claimwick"
```

**Rationale:** Newest first, each entry a plain-verb title with a byline and a timestamp, one deep link at most: the shape Maersk and Paste both describe.

**Cost:** A long history has to be paginated or collapsed. Maersk collapses descriptions behind a toggle, which this does not.

**Reference:** Maersk, Paste

#### `a-table-instead-of-a-timeline`: A Table Instead of a Timeline

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava"
  region "History"
    table 5 "When" "What" "Who" "Amount"
      item: text "8 Oct, 09:34" · text "Paid by bank transfer" · text "Claimwick" · text "EUR 125.00"
```

**Rationale:** When the history is the record rather than context, a table is denser and sortable: when, who, and what, with an amount column when money moved.

**Cost:** It gives up the shape of a sequence for a grid of facts. A reader asking 'what happened in order' has to read down a table, which is slower than a timeline.

#### `grouped-by-day-with-a-date-label`: Grouped by Day with a Date Label

```wireframe
page
  breadcrumb "Home" "Expenses" "Rotterdam client visit" "Schedule"
  header
    heading "Ava"
    text "Receipt attached at 10:41 on Sat 14 Mar 2026."
  region "14 March 2026"
    timeline 3
      item: text "Receipt attached" · text muted "10:41" · text muted "Ava"
  region "9 January 2026"
    timeline 1
      item: text "Reminder sent" · text muted "10:04" · text muted "Claimwick"
```

**Rationale:** Where entries cluster, a date heading above each group saves repeating the date on every line and shows the shape of the day.

**Cost:** More markup for the same information, and a reader scanning for one entry has to look under the right date heading.

#### `collapsed-descriptions-behind-a-toggle`: Collapsed Descriptions Behind a Toggle

```wireframe
page
  breadcrumb "Home" "Expenses" "Disputes" "D-0182"
  header
    heading "Dispute D-0182"
    status "Disputed" critical
    text "EUR 45.00 on expense EXP-1019."
  region "History"
    timeline 5
      item: text "Evidence attached" · text muted "3 Oct 2026, 11:05" · text muted "Cara"
    action "Show the detail for each"
```

**Rationale:** Maersk's approach: title and byline always, description behind a show-details toggle. Keeps a fifty-entry history to fifty lines.

**Cost:** The description is where the evidence lives for a dispute, so hiding it by default hides what a support reader came for.

**Reference:** Maersk

#### `grouped-by-actor-for-an-audit-question`: Grouped by Actor, for an Audit Question

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    text "Ava. Seven things happened to this expense."
  region "Claimwick"
    list 3
      item: text "Paid by bank transfer" · text muted "8 Oct, 09:34"
  region "Eli"
    list 2
      item: text "Resent the receipt at Ava's request" · text muted "8 Oct, 11:20"
  region "Ava"
    list 1
      item: text "Corrected her email address" · text muted "8 Oct, 10:51"
```

**Rationale:** When the question is 'who touched this', the timeline is grouped by person. Each person carries their own list, newest first within it.

**Cost:** Destroys chronological order, which is the order a timeline exists to give. A person touching a record five times over a month reads as one entry.

#### `timeline-with-a-what-changed-diff-on-each-entry`: Timeline with a 'What Changed' Diff on Each Entry

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    text "Ava"
  region "History"
    list 3
      card "Payment settled"
        text muted "8 Oct 2026, 09:34 · Claimwick"
        fact "state" "Awaiting payout to Paid"
```

**Rationale:** Each entry names what it changed, so the history answers what the record looks like now as well as how it got here. This is what an audit trail carries.

**Cost:** A diff needs the before and after. It has to be reconstructed at read time rather than read from the entry that recorded the change.

#### `filtered-by-kind-and-actor`: Filtered by Kind and Actor

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · Rotterdam client visit"
  region "What happened"
    toolbar
      text "Payments · 2"
      text "Receipts · 2"
      text "Messages · 2"
      text "Edits · 1"
      action quiet "Show all 7"
    timeline 5
      item: text "Receipt resent at the spender request" · text muted "8 Oct 2026, 11:20" · text muted "Eli"
```

**Rationale:** GitHub lets a reader filter an issue timeline to comments, commits, or references, because a long record mixes kinds and the reader usually wants one. The filters sit above the timeline and the count beside each says what choosing it keeps.

**Cost:** A filtered timeline hides entries without saying which, so a reader can miss the one line that mattered. The filter state also needs a reset that is always visible, or the hiding looks like absence.

**Reference:** GitHub

#### `entries-and-internal-notes-in-one-feed`: Entries and Internal Notes in One Feed

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · Rotterdam client visit"
    action "Add a note"
  region "What happened"
    timeline 1
      item: text "Receipt resent at the spender request" · text muted "8 Oct 2026, 11:20" · text muted "Eli"
    card "Note from Eli"
      text "Ava wrote that the receipt never arrived. Her address was correct."
      text muted "8 Oct 2026, 11:18 · Internal"
    timeline 3
      item: text "Email corrected by the spender" · text muted "8 Oct 2026, 10:51" · text muted "Ava"
```

**Rationale:** Zendesk draws notes and entries in one expense feed, newest last, because support reads what the team said beside what the system did. A note Eli wrote at 11:18 sits between the resend it explains and the payment it followed.

**Cost:** Notes and entries compete for the same line height, so the feed grows long faster than either would alone. A reader who wants only the system's facts has to skip the team's words, and there is no filter on this option.

**Reference:** Zendesk

### Related Records

**ID:** `litespec/frontend/patterns/record-detail#related-records`

**Question:** An expense belongs to a trip, a spender, and a cost centre. A trip belongs to a client and a schedule. A common failure states the parent in prose in the description rather than linking to it.

**Default:** `each-relation-is-a-link-with-its-state`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Ship the link with state list. Each relation is a link carrying its own state, so a reader can jump and knows what they will find. The runner-up is the preview card option. It is the better choice where the parent facts decide the next step, such as a receipt read against its trip.

Prefer the filtered list counts on records whose children number in the hundreds. Never ship the bare cross-reference line. It cannot carry the parent state and it hides in the description.

**Reviewed for:** Which built relation link differs from the wireframe its catalog entry chose? Open the built chains, cards, and counts beside the catalog entry recording the chosen option and its reason.

#### `each-relation-is-a-link-with-its-state`: Each Relation Is a Link with Its State

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Submitted 8 Oct 2026, 09:32."
    action primary "Message spender"
  region "Belongs to"
    list 4
      item: text "Rotterdam client visit" · text muted "Sat 14 Mar 2026" · text muted "Rotterdam client site" · status "Scheduled" info · action "Open trip"
```

**Rationale:** The trip, the spender, and the cost centre appear as links carrying their own state, so a reader can jump and knows what they will find. Nothing here is prose.

**Cost:** Three links is a small nav of its own, and it competes with the record's own commands for the reader's attention.

#### `a-breadcrumb-as-the-relation-chain`: A Breadcrumb as the Relation Chain

```wireframe
page
  breadcrumb "Home" "Expenses" "Rotterdam client visit" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · 8 Oct 2026, 09:32 · EUR 125.00"
    action primary "Message spender"
  region "The spender"
    fact "Name" "Ava"
    fact "Email" "ava@harbor-pine.example"
  region "What was claimed"
    fact "Train fare, 2 x EUR 45.00" "EUR 90.00"
    fact "Booking fee" "EUR 10.00"
    fact "Breakfast, paid at the desk" "EUR 25.00"
    fact "Paid" "EUR 125.00"
```

**Rationale:** The trail itself is the relation chain: trip, then expense. Nothing repeats it, which is the design contract's strongest single rule.

**Cost:** The trail cannot carry state, so a reader cannot tell a scheduled trip from a finished one without opening it.

#### `a-section-of-other-orders-on-the-same-delivery`: A Section of Other Expenses on the Same Trip

```wireframe
page
  breadcrumb "Home" "Expenses" "Rotterdam client visit"
  header
    heading "Rotterdam client visit"
    status "Scheduled" info
    text "Sat 14 Mar 2026, 08:00 to 12:00 · Rotterdam client site"
    action primary "Add an expense"
  region "The trip"
    fact "Budget" "EUR 2,000"
    fact "Claimed" "EUR 1,800 of EUR 2,000"
    fact "Company" "Harbor and Pine"
  region "Other expenses on this trip"
    list 4
      item: text "EXP-1042" · text muted "Ava" · text muted "8 Oct, 09:32" · text "EUR 125.00" · action "Open"
    action "All 1,204"
```

**Rationale:** The related record is the other expenses, as a short list with the money beside each, which answers 'who else is on this trip' without leaving.

**Cost:** Anywhere between one and two hundred rows, which is a table with a limit rather than a section; the money page already splits it by trip for this reason.

#### `a-cross-reference-line-not-a-section`: A Cross-Reference Line, Not a Section

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Part of Rotterdam client visit, Sat 14 Mar 2026."
    link "Rotterdam client visit"
    action primary "Message spender"
  region "The spender"
    fact "Name" "Ava"
    fact "Email" "ava@harbor-pine.example"
  region "What was claimed"
    fact "Train fare, 2 x EUR 45.00" "EUR 90.00"
    fact "Booking fee" "EUR 10.00"
    fact "Breakfast, paid at the desk" "EUR 25.00"
    fact "Paid" "EUR 125.00"
```

**Rationale:** One line under the heading names the parent record as a link: the smallest possible way to say where this record belongs, and nothing more.

**Cost:** It stays invisible to anyone who does not read descriptions. It cannot carry the parent state, so the reader has to open it to learn whether it is still live.

#### `a-count-that-links-to-the-filtered-list`: A Count That Links to the Filtered List

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · Rotterdam client visit"
    action primary "Message spender"
  region "Records on this expense"
    link "Receipts 2"
    link "Payments 1"
    link "Messages 2"
    text muted "Reversals None"
```

**Rationale:** '2 receipts on this expense' links to the receipts list filtered to this expense. The relation is navigable in both directions and the count is never stale.

**Cost:** A link whose text is a number reads as a figure, not a destination, unless the hover and the visited color make it clear. WCAG 2.4.4 wants the purpose to be clear from the link text.

#### `the-parent-as-a-card-above-the-record-s-own-sections`: The Parent as a Card Above the Record's Own Sections

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "RC-0C893968A2"
  header
    heading "RC-0C893968A2"
    status "Attached" neutral
    text "One receipt on expense EXP-1042."
  card "The trip this receipt belongs to"
    text "Rotterdam client visit"
    text muted "Sat 14 Mar 2026, 08:00 to 12:00 · Rotterdam client site"
    status "Scheduled" info
    action "Open trip"
    text muted "The trip sets the budget this receipt claims."
  region "This receipt"
    fact "Contents" "Train fare"
    fact "Quantity" "1"
    fact "Spender" "Ava"
    fact "Attached" "8 Oct 2026, 09:34"
```

**Rationale:** The parent record opens in a card at the top with its own state and its commands. A reader acting on the child can act on the parent without navigating.

**Cost:** A box inside the page sits above the sections, which is the nesting the design contract forbids. It makes the child look secondary when it is the page.

#### `links-with-a-preview-of-the-parent`: Links with a Preview of the Parent

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "RC-0C893968A2"
  header
    heading "RC-0C893968A2"
    status "Attached" neutral
    text "One receipt on expense EXP-1042."
    action "Detach"
  region "Belongs to"
    list 1
      item: text "Rotterdam client visit" · text muted "Sat 14 Mar 2026" · text muted "Rotterdam client site" · status "Scheduled" info · action "Open trip"
    fact "Travel" "Sat 14 Mar 2026, 08:00 to 12:00"
    fact "Budget left" "EUR 200 of EUR 2,000"
    fact "Company" "Harbor and Pine"
    text muted "The preview carries the three facts a finance reader asks for."
  region "This receipt"
    fact "Contents" "Train fare"
    fact "Quantity" "1"
    fact "Spender" "Ava"
    fact "Attached" "8 Oct 2026, 09:34"
```

**Rationale:** Notion shows a preview of a linked page on hover, because the reader usually wants one fact from the parent rather than the parent itself. The trip row expands to its date, place, and state inline, so a reader checking a receipt learns the travel window without navigating.

**Cost:** A preview is a second rendering of the parent to keep true. Only one row can be open at a time before the section doubles in height. It also answers nothing about children, which still need the list.

**Reference:** Notion

#### `children-as-an-editable-table`: Children as an Editable Table

```wireframe
page
  breadcrumb "Home" "Policies" "Travel spending"
  header
    heading "Travel spending"
    status "Active" positive
    text "What this policy allows, and the ceiling it sets."
    action primary "Add an exception"
  region "Exceptions"
    table 3 "Exception" "State" "Ceiling" "Used"
      item: text "Client meals" · status "Active" positive · text "EUR 45.00" · text "412" · action "Open"
    text muted "Every exception to this policy. The ceilings are the company data."
```

**Rationale:** Airtable draws linked records as rows that can be added and removed in place, because the relation itself is the work. The exceptions to a policy gain a row each with their ceiling and use count. The section holds the Add control rather than sending the reader elsewhere.

**Cost:** Editing inside a section blurs reading and writing: every row needs its own validation and refusal states. It also fits only children with few columns; a wide child still wants its own page.

**Reference:** Airtable

### Record the Reader May Not Change

**ID:** `litespec/frontend/patterns/record-detail#record-the-reader-may-not-change`

**Question:** Some records refuse some roles. A page a role cannot use shows the refusal with the reason and the grant that helps. The question is whether the refusal covers the whole page or a part of one.

**Default:** `the-commands-are-refused-the-record-is-not`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the refused commands on a readable record. Auditors can read an expense and cannot reimburse it, and each refusal names the grant that would allow it. The runner-up is the ask for access option. It is the better choice where access is routinely granted, such as an intern joining finance.

Prefer the field-withheld option only where the values themselves are restricted, such as spender personal data under an auditor role. Never ship the role switcher on production. Acting as somebody is a development capability, and the button would be a security hole with a label on it.

**Reviewed for:** Which built locked record differs from the wireframe its catalog entry chose? Open the built refused commands, banners, and withheld fields beside the catalog entry recording the chosen option and its reason.

#### `the-commands-are-refused-the-record-is-not`: The Commands Are Refused, the Record Is Not

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · Rotterdam client visit"
    message "Your role can read expenses. Only Finance can resend the receipt." neutral
    action disabled "Resend the receipt"
  region "Reversals"
    text muted "EUR 125.00 is paid and none of it has been reversed."
    message "Reversals move money. The finance role can do this. Your role is auditor." neutral
    action disabled "Reverse this payout"
  region "The spender"
    fact "Name" "Ava"
    fact "Email" "ava@harbor-pine.example"
```

**Rationale:** Auditors can read an expense and cannot reverse it. The page shows everything and refuses only the commands, naming the grant that would allow each one. This is an explained action: a disabled button with its reason beside it.

**Cost:** A page full of refused commands is a page that looks broken to someone who does not know what their role is.

#### `the-whole-page-is-refused`: The Whole Page Is Refused

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
  region
    message "You cannot open this expense. Your role is auditor. Expenses are readable by Spenders, Finance and Owners." critical
    text muted "Ask an owner to change your role, or use the trip record for this receipt instead."
    action "Open the trip record instead"
```

**Rationale:** The record itself is out of the reader's reach, not only its commands. A refused state replaces the page body and names the grant.

**Cost:** Nothing is shown, so an auditor cannot read a spender's name to answer a question on the phone. That is a real cost on the surface where names matter most.

#### `a-banner-above-the-page-the-record-still-readable`: A Banner Above the Page, the Record Still Readable

```wireframe
page
  breadcrumb "Home" "Expenses" "Disputes"
  message "You can read disputes, not answer them. Answering a dispute decides EUR 45.00 and is the finance role." neutral
  header
    heading "Dispute D-0182"
    status "Disputed" critical
    text "EUR 45.00 on expense EXP-1019."
    message "Only Finance can answer." neutral
    action disabled "Answer the approver"
  region "What Ben said"
    text "Ben says the hotel breakfast was personal."
```

**Rationale:** A page-level notice says what this role may not do on this page, once, and the sections below stay readable. It tells the reader rather than blocking them.

**Cost:** A full-width band on a page about other work breaks the rule that tone marks content, not chrome. This has to be a neutral notice, not a caution one.

#### `the-command-is-absent-not-refused`: The Command Is Absent, Not Refused

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · Rotterdam client visit"
    action "Message spender"
  region "The spender"
    fact "Name" "Ava"
    fact "Email" "ava@harbor-pine.example"
  region "What was claimed"
    fact "Train fare, 2 x EUR 45.00" "EUR 90.00"
    fact "Booking fee" "EUR 10.00"
    fact "Breakfast, paid at the desk" "EUR 25.00"
    fact "Paid" "EUR 125.00"
```

**Rationale:** A command the role does not have is not drawn at all. The guidance is that a tab or link appears only for a viewer who may open its page, extended to buttons.

**Cost:** Nothing tells the reader the capability exists elsewhere, so they cannot find out who can do it. On a page of four commands, three of them missing looks like a broken deployment.

#### `personal-data-withheld-field-by-field`: Personal Data Withheld Field by Field

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    action "Ask an owner for access"
  region "The spender"
    text muted "Spenders, Finance and Owners can read who submitted an expense. Your role is Auditor."
    fact "Name" "Withheld for your role"
    fact "Email" "Withheld for your role"
    fact "Country" "Netherlands"
  region "What was claimed"
    fact "Train fare, 2 x EUR 45.00" "EUR 90.00"
    fact "Booking fee" "EUR 10.00"
    fact "Breakfast, paid at the desk" "EUR 25.00"
    fact "Paid" "EUR 125.00"
```

**Rationale:** The expense stays readable and only the spender's personal data is withheld, each field saying so where the value would be. The server never sends the value, so nothing is merely hidden. The header offers the one step that would change it.

**Cost:** Every personal field needs a withheld form, and a reader sees that a value exists without seeing it. A caution alert on top of that repeats what the fields already say.

#### `a-role-switcher-rather-than-a-refusal`: A Role Switcher Rather Than a Refusal

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · Rotterdam client visit"
    action primary "Open as Finance"
  region
    message "You are acting as Nina, auditor. Finance can reverse this payout and you cannot." info
    action "Stop acting"
  region "The expense"
    fact "Spender" "Ava"
    fact "Paid" "EUR 125.00"
    fact "Submitted" "8 Oct 2026, 09:32"
```

**Rationale:** The development persona menu already lets a staff member act as somebody else. A record the role cannot use offers the role that can, in one click, for a deployment being demonstrated.

**Cost:** Only honest where acting as somebody is a real capability. On a production deployment with one real account it is a security hole with a nice button on it.

#### `ask-for-access-from-the-refused-command`: Ask for Access from the Refused Command

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · Rotterdam client visit"
    action "Message spender"
  region "Reversals"
    text "EUR 125.00 is paid and none of it has been reversed. Reversals move money, so only the finance role can make one."
    action "Ask for access"
    text muted "Dana decides, usually within a day."
  region "The spender"
    fact "Name" "Ava"
    fact "Email" "ava@harbor-pine.example"
```

**Rationale:** Google Drive turns a refusal into a request: the reader asks, an owner decides, and the decision arrives as a message. An auditor who needs to resend the receipt asks from the refusal itself instead of finding an owner in a corridor.

**Cost:** Every request needs an owner who answers it, and a queue of access requests is a new surface to build and watch. On records where access is never granted it promises a decision that never comes.

**Reference:** Google Drive

#### `a-read-only-ribbon-naming-what-the-role-can-do`: A Read-Only Ribbon Naming What the Role Can Do

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  message "You are reading as Auditor. You can read expenses and export lists. Resending the receipt and reversals belong to other roles." neutral
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · Rotterdam client visit"
    action "Resend the receipt"
    action "Reverse this payout"
  region "The spender"
    fact "Name" "Ava"
    fact "Email" "ava@harbor-pine.example"
  region "What was claimed"
    fact "Train fare, 2 x EUR 45.00" "EUR 90.00"
    fact "Booking fee" "EUR 10.00"
    fact "Breakfast, paid at the desk" "EUR 25.00"
    fact "Paid" "EUR 125.00"
```

**Rationale:** Figma tells a view-only reader exactly that, in a ribbon above the canvas, and lists what the role can still do. The ribbon here says the auditor role reads expenses and exports lists, so the reader knows their reach before they reach past it.

**Cost:** A ribbon on every page a role cannot fully use is a ribbon on most pages for the narrowest roles. It also states the role's reach in words the data has to keep true per page.

**Reference:** Figma

### Record Change History

**ID:** `litespec/frontend/patterns/record-detail#record-change-history`

**Question:** Several people touch one expense before the week ends. The spender corrects an email. The approver answers. Finance adds a note.

When the spender disputes what happened, the company answers from the record itself. It names who did what and when. The audit trail is that answer. It reads beside the facts it explains.

**Default:** `history-as-a-tab-on-the-record`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the history tab on the record. It keeps one route. The tab state stays in the URL. A reader checking a claim opens History without losing the expense. The runner-up is the per-field history.

It fits disputes about one value such as the email that carried the receipt. Prefer the full-page log only for long-lived records whose trail outgrows a tab. Never ship the money-only trail as the audit. The edits that explain the money are missing there.

**Reviewed for:** Which built audit trail differs from the wireframe its catalog entry chose? Open the built tabs, panels, and per-field histories beside the catalog entry recording the chosen option and its reason.

#### `history-as-a-tab-on-the-record`: History as a Tab on the Record

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Submitted 8 Oct 2026, 09:32 by Ava of Harbor and Pine."
    action "Resend the receipt"
    action primary "Message spender"
  tabs "History" "Details" "Receipts" "Payment"
    region "History"
      text muted "Who did what to this expense, newest first."
      timeline 4
        item: text "Payout note added" · text muted "Cara · 8 Oct 2026, 09:34"
```

**Rationale:** The audit sits beside Details and Payment as one more facet. The record keeps one route and the tab state stays in the URL the way record tabs do. A reader checking a claim opens History without losing the expense.

**Cost:** The trail is hidden until its tab opens, and facts cannot be compared against the trail without switching back and forth.

#### `history-in-a-side-panel-beside-the-summary`: History in a Side Panel Beside the Summary

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Ava claimed for Harbor and Pine."
    action primary "Message spender"
  split 1:1
    region "The expense"
      fact "Spender" "Ava"
      fact "Trip" "Rotterdam client visit"
      fact "Submitted" "8 Oct 2026, 09:32"
      fact "Paid" "EUR 125.00"
    region "History"
      timeline 3
        item: text "Payout note added" · text muted "Cara · 8 Oct 2026, 09:34"
```

**Rationale:** Facts and history read side by side, so Cara sees the email and who corrected it in one glance. Nothing is behind a tab.

**Cost:** Each column is half the width, so long entries wrap, and on a narrow screen the panel stacks below the facts it explains.

#### `full-page-audit-log-with-filters`: Full-Page Audit Log with Filters

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "History"
  header
    heading "History of EXP-1042"
    text muted "Every change to this expense, newest first."
    action "Export"
  toolbar
    field "Search changes" search
    field "Actor" select = "Everyone"
    field "Kind" select = "All changes"
  region "Changes"
    timeline 6
      item: text "Payout note added" · text muted "Cara · 8 Oct 2026, 09:34"
    text muted "1 to 6 of 6"
```

**Rationale:** Every change on one scrolling page with actor and kind filters, which suits a long-lived trip whose trail outgrows a tab. Filters answer whose changes these are without reading them all.

**Cost:** The trail leaves the record it explains, so the reader holds the facts in memory. The filters need data support for actor and kind.

#### `last-change-line-with-an-expander`: Last Change Line with an Expander

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Submitted 8 Oct 2026, 09:32 by Ava of Harbor and Pine."
    action primary "Message spender"
  region "The expense"
    fact "Spender" "Ava"
    fact "Trip" "Rotterdam client visit"
    fact "Submitted" "8 Oct 2026, 09:32"
    fact "Paid" "EUR 125.00"
  region "History"
    timeline 1
      item: text "Payout note added" · text muted "Cara · 8 Oct 2026, 09:34"
    action quiet "Show all 4 changes"
```

**Rationale:** One line answers the common question, who touched this last, and the expander reveals the rest without a new route. The record stays short.

**Cost:** Older changes stay hidden behind a click, and the expander state is not shareable, so two people cannot point at the same open trail.

#### `money-movements-only`: Money Movements Only

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Submitted 8 Oct 2026, 09:32 by Ava of Harbor and Pine."
    action "Reverse payout"
  tabs "Payment" "Details" "Receipts" "History"
    region "Money"
      text muted "Only the movements of money. Nothing has been reversed."
      fact "Two fares" "EUR 90.00"
      fact "Booking fee" "EUR 10.00"
      fact "Breakfast" "EUR 25.00"
      fact "Claimed in total" "EUR 125.00"
```

**Rationale:** Finance sees only the movements of money, each tied to its amount, without the operational edits around them. The lines add up top to bottom.

**Cost:** The edits that explain the money are missing. A corrected email before a resend reads as a gap, and there are two histories to keep true.

#### `exportable-trail-for-disputes`: Exportable Trail for Disputes

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Submitted 8 Oct 2026, 09:32 by Ava of Harbor and Pine."
    action "Download the trail"
    action primary "Message spender"
  region "The record"
    fact "Expense" "EXP-1042"
    fact "Spender" "Ava"
    fact "Trip" "Rotterdam client visit"
    fact "Paid" "EUR 125.00"
  region "Trail"
    text muted "Newest first, with the actor on every line."
    timeline 4
      item: text "Spender message sent" · text muted "Cara · 8 Oct 2026, 09:33"
    action quiet "Download"
```

**Rationale:** The trail downloads exactly as it reads, with actor and time on every line, so Cara can attach it to a dispute reply. What the screen states and what the file carries are one thing.

**Cost:** The download freezes wording that a later correction invalidates, and the file format becomes a contract the data has to keep stable.

#### `per-field-history-beside-the-facts`: Per-Field History Beside the Facts

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Submitted 8 Oct 2026, 09:32 by Ava of Harbor and Pine."
    action primary "Message spender"
  region "The expense"
    text muted "What each value was, beside what it is."
    fact "Spender" "Ava"
    text muted "Unchanged since the expense was submitted."
    fact "Email" "ava@harbor-pine.example"
    text muted "Corrected by Ava, 8 Oct 2026, 10:51."
    fact "Trip" "Rotterdam client visit"
    text muted "Unchanged since the expense was submitted."
    fact "Paid" "EUR 125.00"
    text muted "Settled by bank transfer, 8 Oct 2026, 09:34."
```

**Rationale:** Google Sheets shows the edit history of one cell, because a dispute is usually about one value rather than the whole record. Each fact carries its own changed-by line, so the email row says Ava corrected it at 10:51 without opening a trail.

**Cost:** Every fact doubles in height, so a long summary becomes a long read. Fields that never change carry a line saying so, which is honest but heavy.

**Reference:** Google Sheets

#### `two-versions-side-by-side`: Two Versions Side by Side

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Submitted 8 Oct 2026, 09:32 by Ava of Harbor and Pine."
  region "Submitted against now"
    text muted "One value changed since the expense was submitted."
    action quiet "Choose moments"
    split 1:1
      card "Submitted, 09:32"
        fact "Email" "ava@harbor-pine.exampl"
        fact "Paid" "EUR 0.00"
        fact "Receipts" "None attached"
      card "Now"
        fact "Email" "ava@harbor-pine.example"
        fact "Paid" "EUR 125.00"
        fact "Receipts" "2 attached, both readable"
```

**Rationale:** Wikipedia compares two revisions column to column with the changed lines marked, because a dispute names a moment and asks what changed then. The expense as Ava submitted it sits beside the expense as it reads now, and only the email row is marked.

**Cost:** Two columns halve the width each version gets, so long values wrap or clip on a phone. Picking the two moments to compare is also a control of its own, with its own state.

**Reference:** Wikipedia

### Record Notes

**ID:** `litespec/frontend/patterns/record-detail#record-notes`

**Question:** Finance thinks on the record, not beside it. They record why the receipt was resent. They record what the spender was told. They record what still waits. A note is staff writing with a byline and a time. The reader must always know whether the spender can see it.

**Default:** `a-notes-section-with-the-composer-on-top`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the notes section with the composer on top. The newest note reads first. Internal scope is the default. It cannot leak. The runner-up is the internal-or-spender composer.

It fits records where staff write to the spender from the same box. Prefer resolved notes only once open notes grow hard to scan. Never ship notes without a scope marker. A reader who cannot tell internal from spender-visible will write the wrong one.

**Reviewed for:** Which built annotation feed differs from the wireframe its catalog entry chose? Open the built composers, mixes, and resolved piles beside the catalog entry recording the chosen option and its reason.

#### `a-notes-section-with-the-composer-on-top`: A Notes Section with the Composer on Top

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Ava · Rotterdam client visit · 8 Oct 2026, 09:32."
    action primary "Message spender"
  region "Notes"
    text muted "For the team only. Two notes, newest first."
    field "Write a note for the team" textarea
    action primary "Add note"
    list 2
      item: avatar "E" · text "Eli" · text muted "8 Oct 2026, 11:18" · text "The mail provider held the message for two hours."
```

**Rationale:** Shopify draws order notes as a timeline with the composer at the top, newest first. The writer sees what was already said before adding to it, and every note carries its writer and time.

**Cost:** The composer takes the top of the section even when nobody writes. A long note history pushes the record's own sections down the page.

**Reference:** Shopify

#### `notes-mixed-into-the-timeline`: Notes Mixed into the Timeline

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Ava · Rotterdam client visit."
    action "Add a note"
  region "What happened"
    timeline 1
      item: text "Receipt resent at the request" · text muted "Eli · 8 Oct 2026, 11:20"
    card "Eli · 8 Oct 2026, 11:18 · Internal"
      text "The mail provider held the message for two hours."
    timeline 2
      item: text "Email corrected by the spender" · text muted "Ava · 8 Oct 2026, 10:51"
```

**Rationale:** Zendesk draws agent notes inside the expense feed, because what the team wrote explains what the system did next. The resend at 11:20 reads directly under the note that caused it.

**Cost:** Notes and system entries compete for attention in one feed, so a reader who wants only the facts has to skip the team's words. Long notes also stretch a feed meant for short lines.

**Reference:** Zendesk

#### `internal-or-customer-visible-chosen-per-note`: Internal or Spender-Visible, Chosen per Note

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Ava · Rotterdam client visit."
  region "Notes"
    tabs "Internal note" "Message to the spender"
      field "Write for the team" textarea
    action primary "Add note"
    list 2
      item: avatar "E" · text "Eli" · text muted "8 Oct 2026, 11:25" · status "Spender can see" info
```

**Rationale:** Zendesk splits its composer into internal note and public reply. Staff write both from the same record and the wrong scope is a real failure. The scope toggle sits on the composer and every note keeps its marker.

**Cost:** Two scopes double the reading of every note, and a spender-visible note needs the message pipeline behind it rather than a plain save. The toggle is also one more control to misread in a hurry.

**Reference:** Zendesk

#### `resolved-notes-collapse-away`: Resolved Notes Collapse Away

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Ava · Rotterdam client visit."
  region "Notes"
    list 1
      item: avatar "C" · text "Cara" · text muted "8 Oct 2026, 12:02" · text "Keep the breakfast receipt aside for Ben."
    action quiet "1 resolved note"
    list 1
      item: avatar "E" · text "Eli" · text muted "8 Oct 2026, 11:18" · status "Resolved" neutral
    action "Add a note"
```

**Rationale:** Google Docs resolves a comment thread once its question is answered, because an open list grows until nobody reads it. The receipt question resolves once the resend lands, leaving only what still waits.

**Cost:** Resolving hides the reasoning a later dispute may need, so the resolved list has to stay one click away and stay readable. Somebody also has to do the resolving, which is a habit to build.

**Reference:** Google Docs

#### `notes-in-the-aside`: Notes in the Aside

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Ava · Rotterdam client visit · 8 Oct 2026, 09:32."
    action primary "Message spender"
  split 2:1
    region "The spender"
      fact "Name" "Ava"
      fact "Email" "ava@harbor-pine.example"
      fact "Paid" "EUR 125.00"
    aside
      region "Notes"
        action quiet "Add"
        list 2
          item: avatar "E" · text "Eli" · text muted "8 Oct, 11:18"
```

**Rationale:** Linear keeps comments in the issue's main column and properties in the rail; this option reverses it for records whose facts are the work. Notes sit beside the expense so they never push the money down, and the composer stays one line until it opens.

**Cost:** A 260 px column is narrow for a paragraph, so notes wrap early and long threads scroll inside a short box. Notes also lose the prominence they need on a support-heavy record.

**Reference:** Linear

#### `no-notes-yet`: No Notes Yet

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Ava · Rotterdam client visit · 8 Oct 2026, 09:32."
    action primary "Message spender"
  region "The spender"
    fact "Name" "Ava"
    fact "Email" "ava@harbor-pine.example"
  region "Notes"
    message "No notes on this expense. Notes are for the team." neutral
    action "Write the first note"
```

**Rationale:** Notion draws an empty discussion as an invitation rather than a blank box, because a record nobody has annotated is the common case. The empty state says what notes are for and offers the first one.

**Cost:** An empty section takes vertical space on every quiet record to say there is nothing to read. Hiding the section until the first note would save the space but hide the capability.

**Reference:** Notion

### Record Attachments

**ID:** `litespec/frontend/patterns/record-detail#record-attachments`

**Question:** A dispute needs its evidence, a trip needs its receipts, and a policy needs its versions. An attachment is a file the company put on the record, with who added it and when. The reader must know what each file is before opening it.

**Default:** `a-file-list-with-a-drop-zone`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the file list with a drop zone. Each row names the file and its kind. It names who added it and its size. The drop zone invites the next file. The runner-up is the evidence-grouped option.

It fits a dispute. Ben asks for kinds there rather than files. Prefer thumbnails only where images dominate, such as receipt photos. Never ship the drop-zone-only section. A record whose files hide behind an upload control reads as empty.

**Reviewed for:** Which built file section differs from the wireframe its catalog entry chose? Open the built lists, thumbnails, and drop zones beside the catalog entry recording the chosen option and its reason.

#### `a-file-list-with-a-drop-zone`: A File List with a Drop Zone

```wireframe
page
  breadcrumb "Home" "Expenses" "Disputes" "D-0182"
  header
    heading "Dispute D-0182"
    status "Disputed" critical
    text muted "EUR 45.00 on expense EXP-1019. Ben opened a case on 2 Oct 2026."
    action primary "Answer the approver"
  region "Files"
    text muted "What Ben sees if this file set is sent with the answer."
    list 3
      item: icon "file" · text "sale-terms-march-2026.pdf" · text muted "Cara · 3 Oct 2026 · 182 KB" · action quiet "Open"
    field "Add a file" file
```

**Rationale:** Linear draws issue attachments as rows with a drop zone beneath, because the list answers what is here and the zone answers how to add. Each row carries the file kind, who added it, when, and the size.

**Cost:** Rows give every file equal weight, so a ten-file record reads long. Large files also need an upload state per row, which this option does not draw.

**Reference:** Linear

#### `compact-rows-for-many-files`: Compact Rows for Many Files

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Photos of every receipt on this expense."
  region "Receipts"
    text muted "The payout run reads the first photo."
    action quiet "Add a file"
    list 4
      item: image "receipt-train-1080x1350.jpg" · text muted "2.1 MB" · action quiet "Use"
```

**Rationale:** GitHub lists release assets as compact rows, because a record with a dozen files has to stay scannable. Name, size, and a download action fit one line each; who added it moves to the file's own page.

**Cost:** Who and when leave the row, so the reader cannot tell a fresh upload from a stale one without opening it. Compact rows also hide the file kind behind the name alone.

**Reference:** GitHub

#### `evidence-grouped-by-what-it-proves`: Evidence Grouped by What It Proves

```wireframe
page
  breadcrumb "Home" "Expenses" "Disputes" "D-0182"
  header
    heading "Dispute D-0182"
    status "Disputed" critical
    text muted "EUR 45.00 on expense EXP-1019."
    action primary "Answer the approver"
  region "Evidence"
    text muted "Two of three groups are filled. Ben decides on 21 Oct."
    card "Receipt"
      fact "File" "expense-EXP-1019-receipt.pdf"
      fact "Added" "Ava · 28 Sep 2026"
    card "Terms"
      fact "File" "sale-terms-march-2026.pdf"
      fact "Added" "Cara · 3 Oct 2026"
    card "Proof the trip was business"
      field "Add proof of the trip" file
```

**Rationale:** Stripe asks for dispute evidence by category, because the approver checks kinds rather than files: the receipt, the terms, and the proof of the trip. Each group says whether it is filled, so Cara sees what is still missing before she answers.

**Cost:** The categories belong to expense disputes, so the shape fits nothing else without renaming. A file that proves two things also has to sit in two groups or one group has to stay empty.

**Reference:** Stripe

#### `the-drop-zone-is-the-section`: The Drop Zone Is the Section

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1031"
  header
    heading "EXP-1031"
    status "Awaiting receipt" caution
    text muted "Submitted by Eli, still waiting for receipts."
  region "Receipts"
    text muted "This expense has no receipts yet."
    field "Drop the receipts here, or choose a file" file
    text muted "JPG or PNG, 1920 px wide or more."
```

**Rationale:** Dropbox draws an empty folder as one large drop target, because adding is the only job and the target should be unmissable. The section states what belongs here and takes the file anywhere it lands.

**Cost:** Once files exist the large zone wastes the space the list wants, so the shape has to change after the first upload. It also says nothing about what is already there.

**Reference:** Dropbox

#### `images-as-thumbnails-documents-as-rows`: Images as Thumbnails, Documents as Rows

```wireframe
page
  breadcrumb "Home" "Expenses" "Rotterdam client visit"
  header
    heading "Rotterdam client visit"
    status "Scheduled" info
    text muted "Sat 14 Mar 2026, 08:00 to 12:00 · Rotterdam client site."
  region "Files"
    action quiet "Add a file"
    grid 3
      item: image "receipt-hotel.jpg"
    list 2
      item: icon "file" · text "trip-approval-2026.pdf" · text muted "Dana · 12 Dec 2025 · 1.1 MB" · action quiet "Open"
```

**Rationale:** Zendesk draws attached images as thumbnails and documents as rows, because a reader checks a photo by looking and a contract by opening. The thumbnails answer at a glance; the rows answer on click.

**Cost:** Two shapes in one section, so the section needs a rule for what is an image and what is not. Thumbnails also cost a render per file that rows never pay.

**Reference:** Zendesk

#### `no-files-and-what-belongs-here`: No Files, and What Belongs Here

```wireframe
page
  breadcrumb "Home" "Expenses" "Disputes" "D-0182"
  header
    heading "Dispute D-0182"
    status "Disputed" critical
    text muted "EUR 45.00 on expense EXP-1019."
    action primary "Answer the approver"
  region "Evidence"
    message "No evidence attached. Ben wants the receipt, the terms and proof of the trip." neutral
    action "Attach the first file"
```

**Rationale:** Figma tells an empty project what to put in it, because an empty box without guidance reads as a fault. The section names the kinds that belong on a dispute and offers the upload in the same breath.

**Cost:** Guidance text per record type is content to write and keep true. A generic empty state would fit everywhere and help nowhere, which is the worse trade.

**Reference:** Figma

### Property List with Copy

**ID:** `litespec/frontend/patterns/record-detail#property-list-with-copy`

**Question:** Support reads a reference out on the phone and pastes an identifier into a dispute. A property list is a set of labeled values where the identifiers copy with one click. The reader must know the copy landed.

**Default:** `every-identifier-copies`

**Compact:** Below the `sm` breakpoint each row stacks: the label above its value, the copy button beside the value. The compact option is `stacked-on-a-phone`.

**Rationale:** Keep the always-visible copy button on each identifier. Support reaches for it without hunting. The runner-up is the in-place confirmation option. It fits records where a miscopied reference causes real mistakes.

Prefer hover-only copy nowhere. Touch screens have no hover and the button would never appear. Never ship a property list whose long values clip without a way to expand them.

**Reviewed for:** Which built copyable fact differs from the wireframe its catalog entry chose? Open the built identifiers, hovers, and truncations beside the catalog entry recording the chosen option and its reason.

#### `every-identifier-copies`: Every Identifier Copies

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Ava · Rotterdam client visit · 8 Oct 2026, 09:32."
    action primary "Message spender"
  region "Identifiers"
    text muted "What finance quotes. The spender reads none of this."
    list 5
      item: text "Expense" · text "EXP-1042" · action quiet "Copy"
```

**Rationale:** Stripe puts a copy button beside every identifier on a charge, because support quotes references all day. Expense number, payment reference, spender email, and receipt codes each copy with one click; human facts stay plain.

**Cost:** A button per row is noise on records nobody quotes, and each button needs its copied confirmation to be honest. The list also grows long once every identifier claims a row.

**Reference:** Stripe

#### `copy-appears-on-hover-only`: Copy Appears on Hover Only

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Ava · Rotterdam client visit."
  region "Identifiers"
    text muted "The pointer rests on the expense row, so only that row offers Copy."
    text muted "Payment, email and receipts show no button until touched."
    list 4
      item: text "Expense" · text "EXP-1042" · action quiet "Copy"
```

**Rationale:** GitHub shows the copy button on a file path only on hover. A quiet row reads better and the control appears where the pointer already is. The list stays a clean two columns until the reader reaches for a value.

**Cost:** Touch screens have no hover, so on a phone the button never appears and the pattern fails where approvers work. Keyboard readers also have to tab into a row to find a control they cannot see.

**Reference:** GitHub

#### `identifiers-grouped-in-one-card`: Identifiers Grouped in One Card

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Ava · Rotterdam client visit · 8 Oct 2026, 09:32."
    action primary "Message spender"
  region "The expense"
    fact "Spender" "Ava"
    fact "Trip" "Rotterdam client visit"
    fact "Submitted" "8 Oct 2026, 09:32"
    fact "Paid" "EUR 125.00"
  region "Quote these"
    text muted "Copied as written, with no surrounding prose."
    list 3
      item: text "Expense" · text "EXP-1042" · action quiet "Copy"
```

**Rationale:** Vercel groups a deployment's identifiers in one card apart from its facts, because identifiers are quoted together. The id, the reference, and the address leave in one message. One card holds all three with their copy buttons.

**Cost:** The identifiers leave the facts they belong to. The reader jumps between the summary and the card to connect a value with its meaning. A second card is also a second heading for the same record.

**Reference:** Vercel

#### `copied-confirmation-in-place`: Copied Confirmation in Place

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Ava · Rotterdam client visit."
  region "Identifiers"
    list 3
      item: text "Expense" · text "EXP-1042" · status "Copied" positive
  toast "Expense number copied." positive
```

**Rationale:** Railway confirms a copy inside the row that was copied. A toast at the edge of the screen is easy to miss while reading a reference aloud. The button turns into a check beside the value for two seconds.

**Cost:** The row changes shape for two seconds, which shifts the rows below it unless the confirmation holds the button's width. It also confirms only the click, not that the paste landed where it should.

**Reference:** Railway

#### `long-values-truncate-with-expand`: Long Values Truncate with Expand

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Ava · Rotterdam client visit."
  region "Identifiers"
    text muted "Copy takes the whole value, not the shortened line."
    list 3
      item: text "Receipt URL" · text "https://claimwick.example/receipts/EXP-1042" · action quiet "Show" · action quiet "Copy"
```

**Rationale:** Sentry truncates long identifiers to one line with an expander. A receipt URL or a full address would otherwise push the copy button off the row. The row stays one line; the full value opens on demand and still copies whole.

**Cost:** A truncated value cannot be checked by eye until it expands, so a reader comparing two long codes opens both. The expander is also one more control per long row.

**Reference:** Sentry

#### `stacked-on-a-phone`: Stacked on a Phone

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
  region "Identifiers"
    text muted "Expense"
    row
      text "EXP-1042"
      action quiet "Copy"
    text muted "Payment"
    row
      text "SH-88213"
      action quiet "Copy"
    text muted "Spender email"
    row
      text "ava@harbor-pine.example"
      action quiet "Copy"
```

**Rationale:** Polaris stacks a description list on narrow screens, because three columns cannot fit 390 px. The label sits above its value and the copy button stays beside the value, so the thumb reaches it without scrolling sideways.

**Cost:** A five-row list becomes five blocks, so the section doubles in height on the smallest screen. Labels above values also read slower than labels beside them.

**Reference:** Shopify Polaris

### Record Header on a Phone

**ID:** `litespec/frontend/patterns/record-detail#record-header-on-a-phone`

**Question:** Approvers read expenses on a phone between meetings, 390 px wide, one hand free. The header must state which record, what state, and the one step that matters without scrolling sideways or hiding the title.

**Default:** `stacked-title-full-width-primary-the-rest-in-a-menu`

**Compact:** This item is the compact behavior: title and state first, a full-width primary under them, rarer commands in a menu. The compact option is `stacked-title-full-width-primary-the-rest-in-a-menu`.

**Rationale:** Keep the stacked title with a full-width primary and the rest in a menu. It keeps the desktop order of title then actions. The primary stays reachable by thumb. The runner-up is the sticky foot bar.

It fits records where the step is taken mid-scroll, such as approving between meetings. Prefer the scrolling action row only where every action is equally likely. Never ship actions that need sideways scrolling to be found. A hidden Reverse between meetings stops the queue.

**Reviewed for:** Which built narrow title row differs from the wireframe its catalog entry chose? Open the built stacked headers and foot action bars at 390 pixels beside the catalog entry recording the chosen option and its reason.

#### `stacked-title-full-width-primary-the-rest-in-a-menu`: Stacked Title, Full-Width Primary, the Rest in a Menu

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Ava · Rotterdam client visit."
    action primary "Message spender"
    action "Resend the receipt"
    action "Reverse payout"
  region "The spender"
    fact "Name" "Ava"
    fact "Email" "ava@harbor-pine.example"
```

**Rationale:** shadcn dashboard pages stack the header actions under the title below the `sm` breakpoint, with the primary first and full width. The title and state read first; the thumb finds one large button; rarer commands wait behind the menu.

**Cost:** The header grows tall on the smallest screen, so the content starts lower. Secondary commands also hide behind a tap that a hurried reader may not try.

**Reference:** shadcn

#### `a-sticky-action-bar-at-the-foot`: A Sticky Action Bar at the Foot

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Ava · Rotterdam client visit."
  region "The spender"
    fact "Name" "Ava"
    fact "Email" "ava@harbor-pine.example"
  region "What was claimed"
    fact "Two fares" "EUR 90.00"
    fact "Booking fee" "EUR 10.00"
    fact "Breakfast" "EUR 25.00"
  footer sticky
    action "Resend the receipt"
    action primary "Message spender"
```

**Rationale:** Shopify pins the expense actions to the foot of a phone screen, because the thumb is already there and the step stays reachable mid-scroll. Message and Resend stay pinned while the facts scroll above them.

**Cost:** The bar covers content at the foot of every scroll position, so the last section needs padding it would not otherwise need. It also pins two actions that may not be the two this record needs.

**Reference:** Shopify

#### `back-chevron-and-state-in-the-bar`: Back Chevron and State in the Bar

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    action quiet "‹ Expenses"
    status "Paid" positive
    heading "EXP-1042"
    text muted "Ava · Rotterdam client visit · 8 Oct 2026, 09:32."
    action primary "Message spender"
  region "What was claimed"
    fact "Two fares" "EUR 90.00"
    fact "Booking fee" "EUR 10.00"
    fact "Breakfast" "EUR 25.00"
```

**Rationale:** iOS puts the way back in the navigation bar, because a phone reader arrives from a list and returns to it. The bar carries the chevron and the expense number; the page opens straight into the state and the summary.

**Cost:** The bar is shared chrome, so a record-specific bar is a second bar to keep true. The breadcrumb also leaves the page, which breaks the trail contract every other page keeps.

**Reference:** iOS

#### `the-description-folds-behind-the-title`: The Description Folds Behind the Title

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    action quiet "Show spender and trip"
    action primary "Message spender"
  region "The spender"
    fact "Name" "Ava"
    fact "Email" "ava@harbor-pine.example"
```

**Rationale:** GitHub mobile folds long issue context behind the title, because the title and state are the wayfinding and the rest is detail. One tap opens spender, trip, and submitted; until then the header is two lines.

**Cost:** The trip name hides behind a tap on exactly the record type where which-trip is the most-asked question. A folded header also looks unfinished to a reader who never unfolds it.

**Reference:** GitHub

#### `one-scrolling-row-of-actions`: One Scrolling Row of Actions

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Ava · Rotterdam client visit."
  row
    action primary "Message spender"
    action "Resend the receipt"
    action "Reverse payout"
    action "Export"
  region "The spender"
    fact "Name" "Ava"
    fact "Email" "ava@harbor-pine.example"
```

**Rationale:** Material Design scrolls a row of action chips under the title, because every action stays visible as a word rather than hiding in a menu. Message, Resend, Reverse, and Export all fit one swipeable row.

**Cost:** A sideways scroll inside a vertical page hides anything past the edge until swiped. Equal chips also give the destructive action the same weight as a harmless one.

**Reference:** Material Design

#### `no-actions-in-the-header-at-all`: No Actions in the Header at All

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Ava · Rotterdam client visit · 8 Oct 2026, 09:32."
  region "Receipts"
    action quiet "Resend the receipt"
    list 2
      item: text "Train fare" · text muted "one receipt · RC-0C893968A2" · status "Attached" neutral
  region "Money"
    action quiet "Reverse payout"
    fact "Paid by bank transfer" "EUR 125.00"
    fact "Reversed" "EUR 0.00"
```

**Rationale:** GOV.UK keeps small screens to one purpose per page, so commands live in the sections they belong to rather than crowding the header. The header states the record; Resend sits with the receipts and Reverse sits with the money.

**Cost:** The most-used command is a scroll away instead of a tap away. A reader who learned the desktop header finds nothing where the buttons were. Every section also needs its own command row.

**Reference:** GOV.UK

### Previous and Next Record

**ID:** `litespec/frontend/patterns/record-detail#previous-and-next-record`

**Question:** Finance works a queue, not a record. The next expense in the list waits. The next dispute waits for an answer. Moving on must be one step that keeps the list order. The reader never returns to the list merely to open its neighbor.

**Default:** `chevrons-beside-the-breadcrumb`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the chevrons beside the breadcrumb. They keep the list order. They cost almost no space. The runner-up is the jump-by-search option.

It fits readers who know the next record number rather than its neighbor. Prefer the list drawer on triage queues where the reader skims many records. Never ship movement without a position. Chevrons that never say 4 of 268 leave the reader lost in the queue.

**Reviewed for:** Which built neighbor step differs from the wireframe its catalog entry chose? Open the built chevrons, drawers, and jump searches beside the catalog entry recording the chosen option and its reason.

#### `chevrons-beside-the-breadcrumb`: Chevrons Beside the Breadcrumb

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  row
    action quiet "‹ Previous"
    text muted "4 of 268"
    action quiet "Next ›"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Ava · Rotterdam client visit · 8 Oct 2026, 09:32."
    action primary "Message spender"
  region "The spender"
    fact "Name" "Ava"
    fact "Email" "ava@harbor-pine.example"
  region "What was claimed"
    fact "Two fares" "EUR 90.00"
    fact "Breakfast" "EUR 25.00"
```

**Rationale:** Gmail puts newer and older arrows above a message, because triage is neighbor to neighbor and the list order is the queue. The position reads between the chevrons, so the reader knows where they are.

**Cost:** Chevrons move one step at a time, so jumping ten records ahead is ten clicks. They also need the list order behind them, which fails when the record was opened from a search.

**Reference:** Gmail

#### `chevrons-in-the-header-with-keyboard-hints`: Chevrons in the Header with Keyboard Hints

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Ava · Rotterdam client visit · 8 Oct 2026, 09:32."
    action quiet "‹ Previous"
    text muted "4 of 268"
    action quiet "Next ›"
    action primary "Message spender"
  region "What was claimed"
    fact "Two fares" "EUR 90.00"
    fact "Breakfast" "EUR 25.00"
  footer
    text muted "Press j for the previous expense, k for the next."
```

**Rationale:** Gmail teaches `j` and `k` beside its arrows, because a triage reader moves faster without the pointer. The header holds previous and next with their keys, and the position reads beside them.

**Cost:** Keyboard hints help nobody on a phone, where approvers do this work. The header also gains two buttons that compete with the record's own commands.

**Reference:** Gmail

#### `back-to-the-list-then-in-again`: Back to the List, Then in Again

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    action quiet "‹ Back to expenses"
    text muted "Paid · newest first"
    heading "EXP-1042"
    status "Paid" positive
    text muted "Ava · Rotterdam client visit · 8 Oct 2026, 09:32."
    action primary "Message spender"
  region "The spender"
    fact "Name" "Ava"
    fact "Email" "ava@harbor-pine.example"
  region "What was claimed"
    fact "Two fares" "EUR 90.00"
    fact "Breakfast" "EUR 25.00"
```

**Rationale:** Polaris moves between products through the list, because the list holds the search and the filters that define the queue. The record carries one way back; the next record opens from the list with its context intact.

**Cost:** Two steps per record instead of one. The list has to hold its scroll and filters across the round trip or the reader loses their place. Triage of fifty expenses becomes a hundred navigations.

**Reference:** Shopify Polaris

#### `a-list-drawer-beside-the-record`: A List Drawer Beside the Record

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  split 1:2
    region "Queue"
      text muted "Paid and awaiting, newest first. 4 of 268."
      list 4
        item: text "EXP-1042" · text muted "Ava · EUR 125.00" · status "Paid" positive
    region "EXP-1042"
      header
        heading "EXP-1042"
        status "Paid" positive
        text muted "Ava · Rotterdam client visit."
        action primary "Message spender"
      region "What was claimed"
        fact "Two fares" "EUR 90.00"
        fact "Breakfast" "EUR 25.00"
```

**Rationale:** Intercom draws the queue beside the conversation, because triage reads the list and the record together. The drawer lists the queue's expenses with their state and money; the record reads to its right.

**Cost:** The drawer takes a third of the width at desktop and the whole width on a phone, where it has to become a sheet. It also duplicates the expenses list page, so two lists of expenses have to stay true.

**Reference:** Intercom

#### `jump-to-a-record-by-searching`: Jump to a Record by Searching

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Ava · Rotterdam client visit · 8 Oct 2026, 09:32."
    action "Jump to an expense"
    action primary "Message spender"
  region "What was claimed"
    fact "Two fares" "EUR 90.00"
    fact "Breakfast" "EUR 25.00"
  region "Expenses matching EXP-10"
    list 2
      item: text "EXP-1031" · text muted "Eli · EUR 40.45 · Awaiting"
```

**Rationale:** Linear jumps to any issue from its command menu, because a reader who knows the number should not walk the queue to reach it. The expense number, spender name, or email lands on the record in one step.

**Cost:** Search answers only the record the reader already knows, so it never helps triage an unknown queue. It also needs the menu everywhere, which is a frame behavior rather than a record pattern.

**Reference:** Linear

#### `no-movement-each-record-opens-from-the-list`: No Movement: Each Record Opens from the List

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Ava · Rotterdam client visit · 8 Oct 2026, 09:32."
    action "Close"
    action primary "Message spender"
  region "The spender"
    fact "Name" "Ava"
    fact "Email" "ava@harbor-pine.example"
  region "What was claimed"
    fact "Two fares" "EUR 90.00"
    fact "Breakfast" "EUR 25.00"
```

**Rationale:** Some products deliberately offer no neighbor step, because the list is the queue and the record is the work. The reader finishes an expense, closes it, and the list shows what is next. Nothing on the record can carry a stale position.

**Cost:** Every record costs a full round trip through the list, which is slow triage and loses the reader's scroll each time. It is honest but it is the slowest honest option.

### Expense Record

**ID:** `litespec/frontend/patterns/record-detail#expense-record`

**Question:** The expense is the record finance opens most. Ava submitted EXP-1042. It totals EUR 125.00 for two fares, a booking fee, and breakfast. It must state its lines, its payments, and its reversals in one reading. The money adds up. Every reversal reads against the payment it reverses.

**Default:** `summary-three-sections-history-in-the-aside`

**Compact:** On a phone the expense stacks in the order the questions are asked on the road: state and spender first, then receipts, then money. The compact option is `the-order-on-a-phone`.

**Rationale:** Keep the record-page shape with a summary, three sections, and history in the aside. Spender, lines, and money each keep their own section. The runner-up is the money-first layout. It fits finance readers who reconcile payments against reversals.

Prefer the tabbed layout only once an expense outgrows three sections. Never ship the phone layout as the desktop page. Stacked sections waste most of the width there.

**Reviewed for:** Which built worked expense differs from the wireframe its catalog entry chose? Open the built summary, tabs, and phone expense screens beside the catalog entry recording the chosen option and its reason.

#### `summary-three-sections-history-in-the-aside`: Summary, Three Sections, History in the Aside

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Submitted 8 Oct 2026, 09:32 by Ava of Harbor and Pine."
    action "Resend the receipt"
    action "Reverse payout"
    action primary "Message spender"
  split 2:1
    region "Expense"
      fact "Spender" "Ava"
      fact "Trip" "Rotterdam client visit"
      fact "Paid" "EUR 125.00"
      list 3
        item: text "Train fare" · text muted "one receipt · RC-0C893968A2" · status "Attached" neutral
      fact "Reversed" "EUR 0.00"
    aside
      region "What happened"
        timeline 3
          item: text "Paid by bank transfer" · text muted "Claimwick · 8 Oct 2026, 09:34"
```

**Rationale:** A summary of the expense with the spender, the lines, and the money beside a history aside. Each facet of EXP-1042 has one home and the money adds up top to bottom in its own section.

**Cost:** Three sections is a ceiling, so the fourth facet waits for a tab or a second page. The aside also stacks below everything under 900 px, which buries the history on a phone.

#### `one-facet-per-tab`: One Facet per Tab

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Ava · Rotterdam client visit · 8 Oct 2026, 09:32 · EUR 125.00."
    action primary "Message spender"
  tabs "Lines" "Payment" "Notes" "History"
    region "Lines"
      list 3
        item: text "Train fare" · text muted "one receipt · RC-0C893968A2" · status "Attached" neutral
```

**Rationale:** Shopify draws an order as tabs for timeline, lines, and payments, because an expense with messages, notes, and reversals outgrows three sections. The spender summary stays above the tabs so the record is always identified.

**Cost:** Lines and payments can no longer be read together, so checking a reversal against its line means switching tabs. The tab state also has to live in the URL to stay shareable.

**Reference:** Shopify

#### `one-long-page-everything-in-order`: One Long Page, Everything in Order

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Submitted 8 Oct 2026, 09:32 by Ava of Harbor and Pine."
    action primary "Message spender"
  region "The spender"
    fact "Name" "Ava"
    fact "Email" "ava@harbor-pine.example"
    fact "Country" "Netherlands"
  region "What was claimed"
    fact "Two fares" "EUR 90.00"
    fact "Booking fee" "EUR 10.00"
    fact "Breakfast" "EUR 25.00"
  region "Payments"
    fact "Bank transfer, reference SH-88213" "EUR 125.00"
    status "Settled" positive
  region "Reversals"
    text muted "Nothing has been reversed. A reversal goes back against the bank payment."
    action "Reverse to that payment"
  region "What happened"
    timeline 2
      item: text "Paid by bank transfer" · text muted "Claimwick · 8 Oct 2026, 09:34"
```

**Rationale:** Stripe draws a charge as one scrolling page, because support reads top to bottom and prints the page for the file. Spender, lines, payments, reversals, and history follow in the order the questions are asked.

**Cost:** A long page buries the later sections, so reversals sit below the fold on every expense. There is also no shortcut to one facet: the reader scrolls past everything above it.

**Reference:** Stripe

#### `money-first`: Money First

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Ava · Rotterdam client visit · 8 Oct 2026, 09:32."
    action "Reverse payout"
    action primary "Message spender"
  region "Money"
    text muted "Paid by bank transfer on 8 Oct 2026, 09:32, reference SH-88213."
    fact "Two fares" "EUR 90.00"
    fact "Booking fee" "EUR 10.00"
    fact "Breakfast" "EUR 25.00"
    fact "Claimed in total" "EUR 125.00"
    fact "Reversed in total" "EUR 0.00"
  region "The spender"
    fact "Name" "Ava"
    fact "Email" "ava@harbor-pine.example"
```

**Rationale:** Xero opens an invoice on its money, because finance asks what moved before asking who moved it. Payments and reversals sit directly under the header with the statement adding up, and the spender and lines follow below.

**Cost:** Support reads the spender first, so money-first puts their answer second on every call. The money section also repeats the lines it totals, which is the same figure stated twice.

**Reference:** Xero

#### `customer-first`: Spender First

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Rotterdam client visit · 8 Oct 2026, 09:32 · EUR 125.00."
    action primary "Message spender"
  region "The spender"
    row
      avatar "A"
      text "Ava"
    text muted "ava@harbor-pine.example · Netherlands."
    action quiet "All expenses by Ava"
    text muted "2 expenses with Harbor and Pine, both paid, none reversed."
  region "What was claimed"
    fact "Two fares" "EUR 90.00"
    fact "Booking fee" "EUR 10.00"
    fact "Breakfast" "EUR 25.00"
  region "Payments"
    fact "Bank transfer, reference SH-88213" "EUR 125.00"
    status "Settled" positive
```

**Rationale:** Zendesk opens a case on the requester, because support answers a person rather than a record. The spender card leads with name, contact, and expense history, and the lines and money read as that spender's story below it.

**Cost:** The record's own identity waits behind the spender's, so an expense-first reader scrolls past a card they did not open. Spender history on the expense also duplicates the spender page.

**Reference:** Zendesk

#### `the-order-on-a-phone`: The Expense on a Phone

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Ava · Rotterdam client visit."
    action primary "Message spender"
  region "Receipts"
    action quiet "Resend"
    list 2
      item: text "RC-0C893968A2" · text muted "Train fare · one receipt" · status "Attached" neutral
  region "Money"
    fact "Paid by bank transfer" "EUR 125.00"
    fact "Reversed" "EUR 0.00"
```

**Rationale:** Shopify stacks an order on mobile in the order the questions are asked on the road: state and spender first, then receipts, then money. One column, full-width primary, nothing beside anything else.

**Cost:** Stacking puts the money last, so a reversal on the road is a long scroll away. The history also drops to the foot, where nobody on a phone reads it.

**Reference:** Shopify
