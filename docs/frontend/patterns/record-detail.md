# Record Detail Patterns

**Stack:** any
**Layer:** frontend-ui

## Intent

One screen shows one record of the product, with its state, facts, history, and the commands a reader may run, and this page holds the patterns for that screen: header, tabs, summary, aside, timeline, relations, refusals, change history, notes, attachments, identifiers, neighbor movement, and the whole record screen assembled from them. A team building a record screen starts from these patterns, an owner picks an option from the drawings, and the team records the choice in its pattern catalog, the file holding the project's interface decisions. One shared record shape keeps every record readable in the same order, so support finds the state and the next step without hunting.

## Concepts

### How to Read a Pattern

Each pattern answers one recurring question about a record screen, such as what the header states. It states the question, the default option a project builds, how the default behaves on a narrow screen, and why the default won. Each option then shows one wireframe drawing with its own rationale, cost, and optional reference. A project records its choice in its pattern catalog and builds the chosen option with its own components. The [UI patterns](../patterns.md) page states the catalog contract and the wireframe language.

### The Running Example

The drawings on this page use expense EXP-1042 for Ava: paid, EUR 125.00, part of the Rotterdam client visit. The header states the expense and its state, the sections state the spender, lines, and money, and the aside carries the history. The Standards section states the two rules every option keeps: a record header shows at most one primary action, and a destructive command never takes the primary place or style.

## Standards

### Show One Primary Action in a Record Header

**ID:** `litespec/frontend/patterns/record-detail#show-one-primary-action-in-a-record-header`

**Strength:** rule

**Statement:** A record header wireframe MUST show at most one primary action.

**Rationale:** One primary action names the step readers take most often next on the record, such as Message spender on expense EXP-1042. Without that single answer, two equal buttons force the reader to compare labels on every visit, and the hurried click lands on the wrong command.

**Checked by:** `litespec-maintain validate` emits `PATTERN_INVALID` for a wireframe header holding two primary actions.

### Keep Destructive Commands Out of the Primary Place

**ID:** `litespec/frontend/patterns/record-detail#keep-destructive-commands-out-of-the-primary-place`

**Strength:** rule

**Statement:** A destructive command MUST NOT take the primary action's place or style in a record header.

**Rationale:** The primary place draws the hurried click, so it holds only the safe next step. A destructive command styled or placed as primary turns a routine visit into a retired policy or a voided expense, with no step between intent and loss.

**Reviewed for:** Which built record header hands a destructive command the primary style? Open the headers on built record screens and read each command's tone.

## Patterns

### Record Header

**ID:** `litespec/frontend/patterns/record-detail#record-header`

**Strength:** default

**Question:** One record, stated at the top. A record header always carries a title. It carries a breadcrumb when the record has a parent. It offers exactly one primary action, with everything else secondary beside it. A common failure puts a destructive action where the primary should be.

**Default:** `title-state-breadcrumb-one-primary-action`

**Compact:** Below the `sm` breakpoint the header stacks: the title and state read first, a full-width primary under them, and rarer commands wait behind a menu.

**Rationale:** Keep the title, state, breadcrumb, and one primary action. The state beside the title answers the first question on every record. The runner-up is the overflow menu option. It is the better choice once a record carries four or more commands. Prefer the facts option only where the record's own facts are the reason to open it: a tall header pushes the sections below the fold. Never ship a header with no title. Every page carries exactly one `h1`, which automated checks enforce.

**Reviewed for:** Which built title row differs from the wireframe its catalog entry chose? Open the built facts and overflow menus beside the catalog entry recording the chosen option and its reason.

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

```wireframe compact
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text muted "Ava · Rotterdam client visit."
    action primary "Message spender"
    menu "More"
      action "Resend the receipt"
      action "Reverse payout"
  region "The spender"
    fact "Name" "Ava"
    fact "Email" "ava@harbor-pine.example"
```

**Rationale:** This option follows the shape Polaris describes for a Claimwick expense. The state badge sits beside the title so the record's state is read before anything else.

**Cost:** Two actions of the same weight sit beside the primary. The destructive one shares a visual group with a harmless one unless the tone separates them.

**Reference:** Shopify Polaris

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

### Record Tabs

**ID:** `litespec/frontend/patterns/record-detail#record-tabs`

**Strength:** default

**Question:** A record holds at most three sections, and further facets become tabs. A policy record carries four facets. Tab sets stay small, at six at most, and the active tab stays in the URL.

**Default:** `four-tabs-with-the-active-one-in-the-url`

**Compact:** Below the `md` breakpoint the tab row becomes a select naming the current facet. The panels stay the same.

**Rationale:** Keep the four tabs with the active one in the URL. Four stays inside every cited ceiling, and each panel repeats its tab label as a heading. The runner-up is the side rail option. It is the better choice past five facets or where one facet is a long form. Prefer three tabs with the fourth as its own page only where the fourth facet is long enough to stand as a page. Never ship tabs with icons in their labels: icons are forbidden in tab labels, and they add a second channel for one meaning.

**Reviewed for:** Which built facet row differs from the wireframe its catalog entry chose? Open the built side rails and three-tab rows beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** Four facets in tabs, which is inside the six every cited system allows. GOV.UK also asks for a heading inside each panel repeating the tab label, which the Details tab does.

**Cost:** A reader cannot see two facets at once, and GOV.UK says tabs are the wrong choice when comparing across facets is the job.

**Reference:** GOV.UK

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

### Record Summary

**ID:** `litespec/frontend/patterns/record-detail#record-summary`

**Strength:** default

**Question:** The section at the top of a record answers what the reader is looking at without scrolling. It states the record's identity. A summary that states supporting information answers the wrong question.

**Default:** `facts-about-the-record-itself`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the facts about the record itself. A trip summary states when, where, and what it may spend, which is the identity of the record rather than its support. The runner-up is the copyable identifiers option. It is the better choice on records a reader quotes elsewhere, such as an expense read out on the phone. Prefer the tiles option only where the reader checks four figures at a glance rather than reading the facts line by line. Never ship values with no section: a row of values standing on the canvas breaks the rule that a section is the only box.

**Reviewed for:** Which built fact block differs from the wireframe its catalog entry chose? Open the built copy buttons and figure tiles beside the catalog entry recording the chosen option and its reason.

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

**Strength:** default

**Question:** A record page has an aside slot for what supports the record rather than describing it. An expense record puts its history there, which is the right use. A record with no supporting content leaves the slot empty.

**Default:** `history-in-the-aside-sections-in-the-main-column`

**Compact:** Below the `md` breakpoint the aside stacks as sections under the header, with history first.

**Rationale:** Give the pick to history in the aside. History supports the expense without describing it, which is exactly what the aside slot is for. The runner-up is the decision panel. It is the better choice on records that wait for someone, such as a dispute with a deadline. Prefer the people and labels aside on records a team hands between shifts. Never ship an empty aside by accident: an expense with nothing pending and no history reads as a rendering fault.

**Reviewed for:** Which built side column differs from the wireframe its catalog entry chose? Open the built decision panels and ownership rails beside the catalog entry recording the chosen option and its reason.

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

```wireframe compact
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

### Record Timeline

**ID:** `litespec/frontend/patterns/record-detail#record-timeline`

**Strength:** default

**Question:** What happened to one record. Entries read newest first. Every entry carries a timestamp and a byline, because the reader asks who did this and when.

**Default:** `newest-first-with-actor-and-timestamp`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the newest-first timeline with actor and timestamp. It answers who and when on every line. The runner-up is the filtered timeline. It is the better choice once a record carries dozens of entries across kinds. Prefer the table option only where the history is the record rather than context. Never group the main history by actor: it destroys the chronological order a timeline exists to give.

**Reviewed for:** Which built event feed differs from the wireframe its catalog entry chose? Open the built filters and history tables beside the catalog entry recording the chosen option and its reason.

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

### Related Records

**ID:** `litespec/frontend/patterns/record-detail#related-records`

**Strength:** default

**Question:** An expense belongs to a trip, a spender, and a cost centre. A trip belongs to a client and a schedule. A common failure states the parent in prose in the description rather than linking to it.

**Default:** `each-relation-is-a-link-with-its-state`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Ship the link with state list. Each relation is a link carrying its own state, so a reader can jump and knows what they will find. The runner-up is the preview card option. It is the better choice where the parent facts decide the next step, such as a receipt read against its trip. Prefer the editable table only where the relation itself is the work, such as the exceptions to a policy. Never ship a cross-reference line in the description: it cannot carry the parent state and it hides where a reader does not look.

**Reviewed for:** Which built relation link differs from the wireframe its catalog entry chose? Open the built previews and editable tables beside the catalog entry recording the chosen option and its reason.

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

**Strength:** default

**Question:** Some records refuse some roles. A page a role cannot use shows the refusal with the reason and the grant that helps. The question is whether the refusal covers the whole page or a part of one.

**Default:** `the-commands-are-refused-the-record-is-not`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the refused commands on a readable record. Auditors can read an expense and cannot reimburse it, and each refusal names the grant that would allow it. The runner-up is the ask for access option. It is the better choice where access is routinely granted, such as an intern joining finance. Prefer the whole-page refusal only where the record itself is out of the reader's reach: it names the grant, and it costs the reader every fact on the page. Never ship a role switcher on production: acting as somebody is a development capability, and the button would be a security hole with a label on it.

**Reviewed for:** Which built locked record differs from the wireframe its catalog entry chose? Open the built access requests and whole-page refusals beside the catalog entry recording the chosen option and its reason.

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

### Record Change History

**ID:** `litespec/frontend/patterns/record-detail#record-change-history`

**Strength:** default

**Question:** Several people touch one expense before the week ends. The spender corrects an email. The approver answers. Finance adds a note.

When the spender disputes what happened, the company answers from the record itself. It names who did what and when. The audit trail is that answer. It reads beside the facts it explains.

**Default:** `history-as-a-tab-on-the-record`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the history tab on the record. It keeps one route. The tab state stays in the URL. A reader checking a claim opens History without losing the expense. The runner-up is the per-field history. It fits disputes about one value such as the email that carried the receipt. Prefer the full-page log only for long-lived records whose trail outgrows a tab. Never ship the money-only trail as the audit: the edits that explain the money are missing there.

**Reviewed for:** Which built audit trail differs from the wireframe its catalog entry chose? Open the built per-field histories and full-page logs beside the catalog entry recording the chosen option and its reason.

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

### Record Notes

**ID:** `litespec/frontend/patterns/record-detail#record-notes`

**Strength:** default

**Question:** Finance thinks on the record, not beside it. They record why the receipt was resent. They record what the spender was told. They record what still waits. A note is staff writing with a byline and a time. The reader must always know whether the spender can see it.

**Default:** `a-notes-section-with-the-composer-on-top`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the notes section with the composer on top. The newest note reads first. Internal scope is the default. It cannot leak. The runner-up is the internal-or-spender composer. It fits records where staff write to the spender from the same box. Prefer notes mixed into the timeline only where what the team wrote explains what the system did next. Never ship notes without a scope marker: a reader who cannot tell internal from spender-visible will write the wrong one.

**Reviewed for:** Which built annotation feed differs from the wireframe its catalog entry chose? Open the built scope toggles and mixed feeds beside the catalog entry recording the chosen option and its reason.

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

### Record Attachments

**ID:** `litespec/frontend/patterns/record-detail#record-attachments`

**Strength:** default

**Question:** A dispute needs its evidence, a trip needs its receipts, and a policy needs its versions. An attachment is a file the company put on the record, with who added it and when. The reader must know what each file is before opening it.

**Default:** `a-file-list-with-a-drop-zone`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the file list with a drop zone. Each row names the file and its kind. It names who added it and its size. The drop zone invites the next file. The runner-up is the evidence-grouped option. It fits a dispute: Ben asks for kinds there rather than files. Prefer the thumbnail option only where images dominate, such as receipt photos. Never ship a section that is only a drop zone: a record whose files hide behind an upload control reads as empty.

**Reviewed for:** Which built file section differs from the wireframe its catalog entry chose? Open the built evidence groups and thumbnail grids beside the catalog entry recording the chosen option and its reason.

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

### Property List with Copy

**ID:** `litespec/frontend/patterns/record-detail#property-list-with-copy`

**Strength:** default

**Question:** Support reads a reference out on the phone and pastes an identifier into a dispute. A property list is a set of labeled values where the identifiers copy with one click. The reader must know the copy landed.

**Default:** `every-identifier-copies`

**Compact:** Below the `sm` breakpoint each row stacks: the label above its value, the copy button beside the value.

**Rationale:** Keep the always-visible copy button on each identifier. Support reaches for it without hunting. The runner-up is the in-place confirmation option. It fits records where a miscopied reference causes real mistakes. Prefer the grouped card only where identifiers are quoted together, such as a reference pasted into a dispute. Hover-only copy fits nowhere: touch screens have no hover and the button would never appear. Never ship a property list whose long values clip without a way to expand them.

**Reviewed for:** Which built copyable fact differs from the wireframe its catalog entry chose? Open the built confirmations and identifier cards beside the catalog entry recording the chosen option and its reason.

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

```wireframe compact
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

**Rationale:** Stripe puts a copy button beside every identifier on a charge, because support quotes references all day. Expense number, payment reference, spender email, and receipt codes each copy with one click; human facts stay plain.

**Cost:** A button per row is noise on records nobody quotes, and each button needs its copied confirmation to be honest. The list also grows long once every identifier claims a row.

**Reference:** Stripe

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

### Previous and Next Record

**ID:** `litespec/frontend/patterns/record-detail#previous-and-next-record`

**Strength:** default

**Question:** Finance works a queue, not a record. The next expense in the list waits. The next dispute waits for an answer. Moving on must be one step that keeps the list order. The reader never returns to the list merely to open its neighbor.

**Default:** `chevrons-beside-the-breadcrumb`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the chevrons beside the breadcrumb. They keep the list order. They cost almost no space. The runner-up is the jump-by-search option. It fits readers who know the next record number rather than its neighbor. Prefer the list drawer on triage queues where the reader skims many records. Never ship movement without a position: chevrons that never say 4 of 268 leave the reader lost in the queue.

**Reviewed for:** Which built neighbor step differs from the wireframe its catalog entry chose? Open the built jump searches and list drawers beside the catalog entry recording the chosen option and its reason.

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

### Whole Record Screen

**ID:** `litespec/frontend/patterns/record-detail#whole-record-screen`

**Strength:** default

**Question:** One record screen has to order what the other patterns on this page hold: the header, the summary, the sections, and the history. The worked example is the expense, the record finance opens most: Ava submitted EXP-1042, totaling EUR 125.00 for two fares, a booking fee, and breakfast. It must state its lines, its payments, and its reversals in one reading. The money adds up. Every reversal reads against the payment it reverses.

**Default:** `summary-three-sections-history-in-the-aside`

**Compact:** On a phone the record stacks in the order the questions are asked on the road: state and spender first, then receipts, then money.

**Rationale:** Keep the record-page shape with a summary, three sections, and history in the aside. Spender, lines, and money each keep their own section. The runner-up is the money-first layout. It fits finance readers who reconcile payments against reversals. Prefer the tabbed layout only once a record outgrows three sections. Never ship the phone layout as the desktop page: stacked sections waste most of the width there.

**Reviewed for:** Which built record page orders its parts differently from the wireframe its catalog entry chose? Open the built summary, money-first, and tabbed screens beside the catalog entry recording the chosen option and its reason.

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

```wireframe compact
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

**Rationale:** A summary of the expense with the spender, the lines, and the money beside a history aside. Each facet of EXP-1042 has one home and the money adds up top to bottom in its own section.

**Cost:** Three sections is a ceiling, so the fourth facet waits for a tab or a second page. The aside also stacks below everything under 900 px, which buries the history on a phone.

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
