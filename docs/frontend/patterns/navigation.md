# Navigation Patterns

**Stack:** any
**Concern:** frontend-ui

## Intent

This page holds the patterns for the product frame around every screen, which lets a reader move between areas and know where they are: sidebar, breadcrumb, header bar, tabs, icons, language and theme, command palette, account menu, notifications, help, and workspace switching. A team building the product frame starts from these patterns, an owner picks an option from the drawings, and the team records the choice in its pattern catalog, the file holding the project's interface decisions. One shared product frame keeps every page reachable by address and marked in place, so a reader never loses where they are.

## Concepts

### How to Read a Pattern

Each pattern answers one recurring question about moving through the console, such as how the sidebar shows seven areas. It states the question, the default option a project builds, how the default behaves on a narrow screen, and why the default won. Each option then shows one wireframe drawing with its own rationale, cost, and optional reference. A project records its choice in its pattern catalog and builds the chosen option with its own components. The [UI patterns](../patterns.md) page states the catalog contract and the wireframe language.

### The Running Example

The drawings on this page use the Claimwick expense console for Harbor and Pine: seven areas from Home to Settings, expense EXP-1042 for Ava, and the Amsterdam sales cost centre. The frame marks the current area, and every page carries its own address. The Standards section states the two rules every option keeps: the navigation marks the section holding the current page, and every page opens directly from its own URL.

## Standards

### Mark the Current Section

**ID:** `litespec/frontend/patterns/navigation#mark-the-current-section`

**Strength:** required

**Statement:** The navigation MUST mark the section that holds the current page.

**Rationale:** A marked section tells a reader who arrived from a link, a notification, or a search which area holds the open page. Without it, every page looks like the start, and the reader reopens the sidebar to learn where they already are.

**Reviewed for:** Which built screen fails to highlight the area holding its open page? Open three built screens reached by link, notification, and search.

### Give Every Page Its Own Address

**ID:** `litespec/frontend/patterns/navigation#give-every-page-its-own-address`

**Strength:** required

**Statement:** Every page MUST have its own URL that opens it directly.

**Rationale:** A direct address lets a reader share, bookmark, and reopen a page, and lets a notification or a record link to it. Without it, support cannot send the page under discussion, and a refresh drops the reader back to the start.

**Reviewed for:** Which built screen cannot be opened directly from its copied link? Paste three copied addresses into a fresh tab.


## Patterns

### Sidebar

**ID:** `litespec/frontend/patterns/navigation#sidebar`

**Strength:** default

**Question:** Seven top-level areas organize the console. Settings alone holds thirty routes. Groups expand to two levels. The current item stays marked wherever it sits.

**Default:** `grouped-areas-with-expandable-sub-items`

**Compact:** Below the md breakpoint the sidebar becomes an off-canvas sheet: the same entries slide in over a scrim and close with a control.

**Rationale:** The grouped areas with expandable sub-items keep seven areas in two levels. The counts beside groups are the runner-up for areas that hold a queue, such as reversals. Recent pages above the areas fit a reader who resumes the same few records.

Never ship the flat list of every page. Over a hundred entries is an index, not a map, and it belongs in the command palette.

**Reviewed for:** Which built area list breaks from the wireframe its catalog entry chose? Open the built console frame, with its grouped areas and marked item, beside the catalog entry recording the chosen option and its reason.

#### `grouped-areas-with-expandable-sub-items`: Grouped Areas with Expandable Sub-Items

```wireframe
page
  breadcrumb "Home" "Trips"
  header
    heading "Trips"
    text "93 in the expense list."
    action primary "New expense"
  split 1:4
    nav side
      link "Home"
      link "Expenses"
      link "Spenders"
      link current "Trips"
      link "Receipts"
      link "Payouts"
      link "Reports"
      link "Settings"
    region "Trips"
      toolbar
        field "Search trips" search
        action "Active 81"
        action "Draft 9"
      table 4 "Trip" "Budget" "State" "Amount"
        item: text "Rotterdam client visit" · text "412 filed" · status "Active" positive · text "EUR 45.00"
```

```wireframe compact
page
  header
    heading "Expenses"
    text "268 expenses."
    action "Open the menu"
  region "Expenses"
    toolbar
      field "Search expenses" search
    list 2
      item: text "EXP-1042 · Ava" · text muted "8 Oct 2026, 09:32 · Rotterdam client visit" · status "Paid" positive · text "EUR 125.00"
  drawer left "Menu"
    nav side
      link "Home"
      link current "Expenses"
      link "Reversals"
      link "Exceptions"
      link "Spenders"
      link "Trips"
```

**Rationale:** Seven areas expand, with the current item marked and the sub-item indented under its area. The account sits at the foot.

**Cost:** Thirty settings routes in one group means a group longer than the screen. Settings stays open on every settings page, so it never collapses.



#### `two-levels-with-the-group-showing-a-live-count`: Two Levels, with the Group Showing a Live Count

```wireframe
page
  breadcrumb "Home" "Expenses" "Reversals"
  header
    heading "Reversals"
    text "10 open. The oldest waited 214 days."
    action "Export"
  split 1:4
    nav side
      link "Home"
      link "Expenses"
      link "Awaiting approval 31"
      link current "Reversals 10"
      link "Exceptions 2"
      link "Spenders"
      link "Trips"
      link "Payouts"
      link "Reports"
      link "Settings"
    region "Reversals"
      toolbar
        field "Search reversals" search
      table 4 "Expense" "Spender" "Asked" "State" "Amount"
        item: text "EXP-1042" · text "Ava" · text muted "Asked 6 days ago" · status "Open" neutral · text "EUR 125.00"
```

**Rationale:** The groups carry the number a reader would otherwise have to go and get: 10 under Reversals, 3 team invitations, 5 disputes. The count is the group's whole job.

**Cost:** A count beside every area is a claim on every read. An area with a zero count looks finished rather than empty.


#### `recent-pages-pinned-above-the-areas`: Recent Pages Pinned Above the Areas

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses from 142 spenders."
  split 1:4
    nav side
      link "EXP-1042 · Ava"
      link "Rotterdam client visit"
      link "September statement"
      link "Home"
      link current "Expenses"
      link "Spenders"
      link "Trips"
      link "Payouts"
    region "Expenses"
      toolbar
        field "Search expenses" search
      table 4 "Expense" "Spender" "State" "Paid"
        item: text "EXP-1042" · text "Ava" · status "Paid" positive · text "EUR 125.00"
```

**Rationale:** The three most recent pages sit above the seven areas, so resuming work is one click instead of re-walking the tree. None of the other options remembers where the reader was: this one navigates by recency rather than by structure.

**Cost:** Recents are per person, so a shared screen shows another person's trail. A recent that was archived or renamed also needs a rule for what its row says.


### Breadcrumb

**ID:** `litespec/frontend/patterns/navigation#breadcrumb`

**Strength:** default

**Question:** Trails run four levels deep on an expense record. A trail never wraps and may scroll sideways. The last item carries aria-current. Dutch strings sometimes appear in an English frame.

**Default:** `the-trail-as-the-parent-chain`

**Compact:** Below the md breakpoint the trail collapses to a single back link to the parent, above the heading.

**Rationale:** The parent chain with the last item marked current states where a record sits in the tree. The left-truncating trail with an overflow menu is the runner-up for four-level chains, because it keeps the current page visible. The current title as a sibling menu fits records whose siblings are opened together.

Never ship no trail at all: on a phone the sidebar is closed, so the trail is the only statement of where a record sits.

**Reviewed for:** Which built parent chain differs from the wireframe its catalog entry chose? Open a built four-level trail, with its truncation and parent links, beside the catalog entry recording the chosen option and its reason.

#### `the-trail-as-the-parent-chain`: The Trail as the Parent Chain

```wireframe
page
  breadcrumb "Home" "Trips" "EXP-1042" "Standard"
  header
    heading "Standard"
    status "Active" positive
    text "What this expense is, and the receipt rule its lines carry."
    action "Pause payouts"
  region "Details"
    fact "Lines per expense" "1"
    fact "Budget" "Whole trip"
    fact "Filed" "9,412 of 10,000"
```

```wireframe compact
page
  link "‹ EXP-1042"
  header
    heading "Standard"
    status "Active" positive
    text "What this expense is, and the receipt rule its lines carry."
    action "Pause payouts"
  region "Details"
    fact "Lines per expense" "1"
    fact "Filed" "9,412 of 10,000"
```

**Rationale:** Every level is a link except the last, which carries the current item. It is the only place the record's place in the tree is stated.

**Cost:** Four levels on an expense, and Fluent's no-wrap rule pushes the last item off a phone unless the trail scrolls.



#### `a-trail-that-truncates-from-the-left`: A Trail That Truncates from the Left

```wireframe
page
  breadcrumb "Home" "Standard"
  menu "Levels above"
    action "Trips"
    action "EXP-1042, Rotterdam client visit"
  header
    heading "Standard"
    status "Active" positive
    text "What this expense is, and the receipt rule its lines carry."
    action "Pause payouts"
  region "Details"
    fact "Lines per expense" "1"
    fact "Filed" "9,412 of 10,000"
```

**Rationale:** The trail truncates after a fixed length with a tooltip. The overflow collapses into a menu from the second item. A long trip name never pushes the current page off.

**Cost:** A menu in the trail is a third kind of menu on one page. The page already has row menus and header menus. The reader opens it to see where they are.

**Reference:** Fluent UI


#### `the-current-title-as-a-sibling-menu`: The Current Title as a Sibling Menu

```wireframe
page
  breadcrumb "Home" "Trips" "EXP-1042" "Standard"
  header
    heading "Standard"
    status "Active" positive
    text "What this expense is, and the receipt rule its lines carry."
    action quiet "Standard ▾"
    action "Pause payouts"
  menu "Expenses"
    action "Standard"
    action "Economy"
    action "Bundle"
    action "Add an expense"
  region "Details"
    fact "Lines per expense" "1"
    fact "Filed" "9,412 of 10,000"
```

**Rationale:** The current page title opens a menu of its siblings: opening it lists the other expenses beside Standard. Moving sideways costs one click instead of up and back down.

**Cost:** A heading that opens a menu is a heading that does not behave like one. The menu repeats the sidebar section the reader left. It fits only where siblings are visited together.

**Reference:** Notion


### Header Bar

**ID:** `litespec/frontend/patterns/navigation#header-bar`

**Strength:** default

**Question:** The bar carries the page title, a sidebar toggle, a help control, and the theme. It does not carry the company name. It does not say who is acting. It says nothing about location beyond the page title.

**Default:** `the-bar-with-the-company-name`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** The bar with the company name and the acting person states the two facts a support conversation needs before anything else. The bar as the breadcrumb is the runner-up when the trail would otherwise repeat the page title. The deployment identity band fits a sandbox deployment, where naming the deployment avoids confusion with production.

Never ship no bar at all: burying the theme and language in the sidebar foot turns a visible control into three taps.

**Reviewed for:** Which built top band breaks from the wireframe its catalog entry chose? Open the built trip header, with its company name and deployment identity, beside the catalog entry recording the chosen option and its reason.

#### `the-bar-with-the-company-name`: The Bar with the Company Name

```wireframe
page
  toolbar
    action "Toggle the sidebar"
    text "Harbor and Pine"
    text "Acting as Alex Morgan"
    action "Help"
    action "Switch the theme"
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses from 142 spenders, newest first."
  region "Expenses"
    table 3 "Expense" "Spender" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · status "Paid" positive · text "EUR 125.00"
```

**Rationale:** The page title on the left, then the things that are true everywhere: which company, who is acting, help, and theme. The sidebar toggle stays because a phone needs it.

**Cost:** Four things in a bar above the content on every page, and a brand mark that repeats what the sidebar already says.


#### `the-bar-as-the-breadcrumb-replacing-the-trail`: The Bar as the Breadcrumb, Replacing the Trail

```wireframe
page
  toolbar
    action "Toggle the sidebar"
    text "Trips › EXP-1042 › Standard"
    action "Help"
    action "Switch the theme"
  header
    heading "Standard"
    status "Active" positive
    text "What this expense is, and the receipt rule its lines carry."
    action "Pause payouts"
  region "Details"
    fact "Lines per expense" "1"
    fact "Filed" "9,412 of 10,000"
```

**Rationale:** The chain moves into the bar, where the page title already is. One place states where the reader is rather than two.

**Cost:** The bar is present on every page including the list pages where there is no chain, so the bar has two different shapes. A breadcrumb is a page-level element below the header in many systems, so a bar that replaces it takes that job.


#### `the-bar-with-the-deployment-identity`: The Bar with the Deployment Identity

```wireframe
page
  toolbar
    text "Expenses"
    status "Sandbox" info
    action "Help"
    action "Switch the theme"
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Nothing here is charged and nothing reaches a bank."
  region "Expenses"
    table 2 "Expense" "Spender" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · status "Paid" positive · text "EUR 125.00"
```

**Rationale:** Where a deployment is a sandbox, the bar says so on every page. Naming the deployment is the one job of an identity band.

**Cost:** A deployment identity in the bar competes with the page title for space. A reader who has seen it fifty times stops seeing it.


### Tabs Within a Page

**ID:** `litespec/frontend/patterns/navigation#tabs-within-a-page`

**Strength:** default

**Question:** Sibling pages of one area sit in a sub-page navigation. A second tab row uses a subordinate level. Tab sets stay small, at about six. The active tab belongs in the URL.

**Default:** `the-shipped-tab-row-with-the-tab-in-the-url`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** The tab row with the active tab in the URL carries the facet in a pasted link. Tabs as real links are the runner-up and the most defensible alternative, because middle click and the back button behave as expected. Headed sections on one page fit a record with a handful of facets that all fit.

Never ship the segmented control for facets: three segments is its ceiling and it cannot carry counts or overflow.

**Reviewed for:** Which built tab row differs from the wireframe its catalog entry chose? Open the built in-page tabs, with the active tab in the URL, beside the catalog entry recording the chosen option and its reason.

#### `the-shipped-tab-row-with-the-tab-in-the-url`: The Shipped Tab Row, with the Tab in the URL

```wireframe
page
  breadcrumb "Home" "Trips" "EXP-1042" "Standard"
  header
    heading "Standard"
    status "Active" positive
    text "What this expense is, and the receipt rule its lines carry."
    action "Pause payouts"
  tabs "Amounts" "Receipts" "Reversals" "Details"
    region "Details"
      fact "Lines per expense" "1"
      fact "Budget" "Whole trip"
```


**Rationale:** The tab row under the header on an expense, with the active tab as a URL parameter, so a pasted link carries it.

**Cost:** A reader cannot compare two facets, and every new backend facet adds a tab. Tabs stay small, at about six, for this reason.

**Reference:** Carbon

**Reference:** GOV.UK

#### `tabs-as-links-so-a-middle-click-works`: Tabs as Links, so a Middle Click Works

```wireframe
page
  breadcrumb "Home" "Trips" "EXP-1042"
  header
    heading "EXP-1042"
    status "Active" positive
    text "EXP-1042 · 412 filed."
    action primary "Add a receipt"
  tabs "Details" "Amounts" "Budget" "Money" "Operations"
    region "Amounts"
      fact "Standard" "EUR 45.00"
      text muted "9,412 filed of 10,000"
```

**Rationale:** The tab labels are real links to sibling routes rather than buttons that swap a panel. Middle click, open in a new tab, and the back button all behave the way a reader expects.


**Rationale:** The tab labels are real links to sibling routes rather than buttons that swap a panel. Middle click, open in a new tab, and the back button all behave the way a reader expects.

**Cost:** Anchor links with a URL fragment make each tab a real address. This is the most defensible option available rather than an alternative.

**Reference:** GOV.UK

#### `no-tabs-because-the-facets-are-a-checklist`: No Tabs, Because the Facets Are a Checklist

```wireframe
page
  breadcrumb "Home" "Settings" "Cost centres" "Amsterdam sales"
  header
    heading "Amsterdam sales"
    status "Active" positive
    text "40 expenses, general storage."
    action primary "Add a cost centre"
  region "What it is"
    fact "Name" "Amsterdam sales"
    fact "Capacity" "40 expenses"
    fact "Company" "Harbor and Pine"
  region "Cost centres"
    list 2
      item: text "Cost centre A" · text muted "4 cost centres · open from 08:00" · status "Active" positive · action "Edit"
  region "Which trips use it"
    list 1
      item: text "Rotterdam client visit" · text muted "Sat 14 Mar 2026 · 40 expenses" · action "Open"
```

**Rationale:** Where a record has a handful of facets and they all fit, keep headed sections on one page. Headed sections on one page are the first-class alternative to tabs and the right answer for most records.

**Cost:** A long page. It stays honest only while the sections stay few, and it is why a record with eight facets carries tabs rather than eight sections.

**Reference:** GOV.UK

### Icons in the Frame

**ID:** `litespec/frontend/patterns/navigation#icons-in-the-frame`

**Strength:** default

**Question:** Seven area icons, a sidebar toggle, a help mark, and the theme. Every one of these is a named component with an accessible label, never a raw element.

**Default:** `an-icon-beside-every-label`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** An icon beside every label reinforces the label rather than replacing it, which makes seven areas easy to read at a glance. Icons only for the actions, with words for the areas, are the runner-up where icon budget is short. Words only fit a small set of areas with no icon set to keep.

Never ship state-carrying icons in the frame: theme and language as bare glyphs force the reader to hover both to tell them apart.

**Reviewed for:** Which built label or action carries an icon outside the recorded set? Open the built frame, with its labeled actions and marked current item, beside the catalog entry recording the chosen option and its reason.

#### `an-icon-beside-every-label`: An Icon Beside Every Label

```wireframe
page
  breadcrumb "Home"
  header
    heading "Good afternoon, Alex"
    text "Two things need you today."
    action primary "New expense"
  split 1:4
    region "Areas"
      list 7
        item: icon "Home" · link current "Home"
    region "Needs attention"
      status "2 to do" caution
      list 1
        item: text "Close out the March payout run" · text muted "The count ended. Nothing left to settle." · action "Close out"
```


**Rationale:** Every area carries an icon and its name, so the icon is reinforcement rather than the only label. It is what makes seven areas easy to read at a glance.

**Cost:** Seven icons is seven drawings to keep consistent and to give a name to. Icons in tab labels are refused for the same reason.

**Reference:** Carbon

#### `icons-only-for-the-actions-words-only-for-the-areas`: Icons Only for the Actions, Words Only for the Areas

```wireframe
page
  toolbar
    text "Expenses"
    action "Search expenses"
    action "Export as CSV"
    action "Light or dark"
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses from 142 spenders, newest first."
  split 1:4
    nav side
      link "Home"
      link current "Expenses"
      link "Spenders"
      link "Trips"
      link "Payouts"
      link "Reports"
      link "Settings"
    region "Expenses"
      table 3 "Expense" "Spender" "State" "Paid"
        item: text "EXP-1042" · text "Ava" · status "Paid" positive · text "EUR 125.00" · action "More actions for expense EXP-1042"
```


**Rationale:** The split the evidence supports: an icon reinforces a labeled area, but an icon alone is never the label for a place. An action with one unmistakable meaning can be an icon with a tooltip and an accessible name.

**Cost:** An action that has to be learned has to be hovered. One action per notification and no Dismiss is the same economy applied to a button.

**Reference:** Material

#### `words-only-no-icons`: Words Only, No Icons

```wireframe
page
  breadcrumb "Home"
  header
    heading "Good afternoon, Alex"
    text "Two things need you today."
    action primary "New expense"
  split 1:4
    nav side
      link current "Home"
      link "Expenses"
      link "Spenders"
      link "Trips"
      link "Payouts"
      link "Reports"
      link "Settings"
    region "Needs attention"
      status "2 to do" caution
      list 1
        item: text "Close out the March payout run" · text muted "The count ended. Nothing left to settle." · action "Close out"
```

**Rationale:** The labels are the navigation. Nothing to draw, nothing to name, and nothing that fails when a reader cannot distinguish the icons.

**Cost:** Seven areas of the same weight in the same style is a wall of text, and the eye has nothing to catch on. This is the shape that needs a bold current item to work.


### Language and Theme

**ID:** `litespec/frontend/patterns/navigation#language-and-theme`

**Strength:** default

**Question:** Two switches change nothing about the data. They are the most error-prone controls on the page. An explicit choice is preferred to the account default. A missing translation falls back to the other language.

**Default:** `both-switches-in-the-header-each-showing-its-value`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Both switches in the header, each showing its value, let the reader see what is set without opening anything. The account menu is the runner-up for language, which rarely changes and belongs with the account. The theme following the system with an override fits a reader whose device already states the preference.

Never ship no switch at all. One language is a commercial decision about which companies Claimwick serves. Claimwick already serves companies in two languages.

**Reviewed for:** Which built switch differs from the wireframe its catalog entry chose? Open the built locale and appearance controls, in the header and account menu, beside the catalog entry recording the chosen option and its reason.

#### `both-switches-in-the-header-each-showing-its-value`: Both Switches in the Header, Each Showing Its Value

```wireframe
page
  toolbar
    text "Expenses"
    field "Language" select = "English"
    field "Theme" select = "Light"
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses."
  region "Expenses"
    table 3 "Expense" "Spender" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · status "Paid" positive · text "EUR 125.00"
```

**Rationale:** Each control carries its current value rather than an icon, so the reader can see what is set without opening it.

**Cost:** Two text buttons in a header of text, and a reader who wants to change the language twice a day reads 'EN' as decoration.


#### `both-switches-in-the-account-menu`: Both Switches in the Account Menu

```wireframe
page
  toolbar
    text "Expenses"
    action "Account: Alex Morgan"
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses."
  menu "Alex Morgan · Harbor and Pine"
    action "Your account"
    action "Sign out"
    action "English"
    action "Nederlands"
    action "Light"
    action "Dark"
  region "Expenses"
    table 2 "Expense" "Spender" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · status "Paid" positive · text "EUR 125.00"
```

**Rationale:** Everything about who you are and how the trip looks sits in one place, and the header is left for the page title. Account, language, theme, and sign-out are one decision.

**Cost:** Two taps for a reader who only wants dark at the end of a shift. The theme is not something a reader opens an account menu to find.


#### `the-theme-following-the-system-with-an-override`: The Theme Following the System, with an Override

```wireframe
page
  toolbar
    text "Expenses"
    status "System theme" neutral
    field "Theme" select = "Match my device"
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses."
  region "Expenses"
    table 4 "Expense" "Spender" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · status "Paid" positive · text "EUR 125.00"
```

**Rationale:** The theme defaults to the operating system's and the reader overrides it once. The preference is kept, and the switch shows the override rather than the system's value.

**Cost:** A reader whose system is dark gets a dark console without asking. That is the correct behavior. A company filming a screen for a spender then gets a dark screenshot.


### Command Palette

**ID:** `litespec/frontend/patterns/navigation#command-palette`

**Strength:** default

**Question:** An operations manager working a list often needs one record or one command and then the list again. The palette jumps straight to the expense for Rotterdam client visit. It runs the next step from the keyboard, so the reader never loses the page they were working.

**Default:** `cmd-k-dialog-with-grouped-results`

**Compact:** On a phone the palette opens as a bottom sheet where the thumb reaches the input, with recent records above the fold.

**Rationale:** The grouped Cmd+K dialog puts pages above records, so a typed query answers both where to go and what to open. Recent items first are the runner-up for cost centre and support work that moves between a few records. Search scoped to the current area fits a reader who is already in the right list.

Never ship command verbs with a preview as the default palette. Every verb becomes a second path to a use case.

**Reviewed for:** Which built jump-to dialog breaks from the wireframe its catalog entry chose? Open the built palette, with its grouped results and recent items, beside the catalog entry recording the chosen option and its reason.

#### `cmd-k-dialog-with-grouped-results`: Cmd+K Dialog with Grouped Results

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense, newest first."
  dialog "Jump to"
    field "Search pages, expenses, trips" search
    region "Go to"
      list 2
        item: text "Expenses" · text muted "Pages"
    region "Expenses"
      list 2
        item: text "EXP-1042" · text "Ava" · status "Paid" positive
    text muted "Move with Up and Down. Open with Enter."
```

```wireframe compact
page
  header
    heading "Expenses"
    text "268 expenses."
  region "Expenses"
    list 1
      item: text "EXP-1042" · text "Ava" · status "Paid" positive
  drawer bottom "Search"
    field "Search pages, expenses, trips" search
    list 2
      item: text "EXP-1042" · text "Ava" · status "Paid" positive
```

**Rationale:** One dialog over the current page, with pages above records, so a typed query answers both where to go and what to open. The groups keep the list short enough to read while the query is still short.

**Cost:** Grouped results compete for the same short list, so a page the reader wants can sit below records they do not.



#### `recent-items-first`: Recent Items First

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense, newest first."
  dialog "Jump to"
    field "Search pages, expenses, trips" search
    region "Recent"
      list 2
        item: text "EXP-1042" · text "Ava" · status "Paid" positive
    region "Go to"
      list 2
        item: text "Expenses" · text muted "Pages"
    text muted "Open with Enter."
```

**Rationale:** Recent records sit above the groups before anything is typed, so returning to the expense left open is one keypress. Recency matches how cost centre and support work moves between a few records.

**Cost:** Recency is personal state on a shared machine, so the next person at the keyboard sees what the last person opened.


#### `search-scoped-to-current-area`: Search Scoped to Current Area

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense, newest first."
  dialog "Jump to"
    field "Scope" select = "Expenses"
    field "Search expenses" search
    region "Expenses"
      list 2
        item: text "EXP-1042" · text "Ava" · status "Paid" positive
    text muted "Scope with Tab. Open with Enter."
```

**Rationale:** The scope starts on the area the reader is already in, so typing an expense number on the expenses page searches expenses first. Widening to everything is one explicit step.

**Cost:** A scoped default hides matches elsewhere, so a reader who mistakes the scope concludes a record is missing.


### Account Menu

**ID:** `litespec/frontend/patterns/navigation#account-menu`

**Strength:** default

**Question:** The frame names who is acting, for which company, with what access. The account sits at the foot of the sidebar with a way to switch identity beside it on a development deployment.

**Default:** `an-avatar-menu-in-the-header`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** The avatar menu in the header holds the account page, the access statement, and sign-out in one control. The sidebar foot is the runner-up, but it is far from the top of a long page. The account as a page reached from the menu fits a console where account, email, sign-in, and close are separate pages.

Never ship the bare sign-out link. An account with no page turns language, sessions, and closing into support messages.

**Reviewed for:** Which built sign-in menu differs from the wireframe its catalog entry chose? Open the built account controls, in the sidebar foot or header, beside the catalog entry recording the chosen option and its reason.

#### `an-avatar-menu-in-the-header`: An Avatar Menu in the Header

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses."
    action "Alex Morgan"
  region "Expenses"
    table 2 "Expense" "Spender" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · status "Paid" positive · text "EUR 125.00"
  popover "Alex Morgan · Operations manager"
    action "Your account"
    action "Your access"
    action "Language: English"
    action "Theme: Light"
    action "Sign out"
```


**Rationale:** One avatar in the header holds the account page, the preferences, and sign-out. The acting person is visible on every page without scrolling to the foot.

**Cost:** The header gains a control on every page, and the menu repeats the sidebar foot it replaces during the transition. The foot still needs a sign-out for readers who look there first.

**Reference:** Linear

**Reference:** Vercel

#### `the-account-in-the-sidebar-foot`: The Account in the Sidebar Foot

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses from 142 spenders, newest first."
  split 1:3
    nav side
      link "Home"
      link current "Expenses"
      link "Spenders"
      link "Trips"
      link "Payouts"
      link "Settings"
      link "Help"
    region "Expenses"
      table 2 "Expense" "Spender" "State" "Paid"
        item: text "EXP-1042" · text "Ava" · status "Paid" positive · text "EUR 125.00"
  footer
    avatar "AM"
    text "Alex Morgan · Operations manager"
```

**Rationale:** The foot names the acting person and role on every page, and opening it switches identity on a development deployment.

**Cost:** The foot of a long sidebar is far from the top of the page. On a phone the whole account hides inside the sheet. The bar says nothing about who is acting.


#### `the-account-as-a-page-reached-from-the-menu`: The Account as a Page, Reached from the Menu

```wireframe
page
  breadcrumb "Home" "Account"
  header
    heading "Your account"
    text "Alex Morgan · Harbor and Pine."
  region "Details"
    fact "Name" "Alex Morgan"
    fact "Email" "alex@harbor-pine.example"
    fact "Company" "Harbor and Pine"
    fact "Role" "Operations manager"
  region "Manage"
    list 3
      item: text "Email address" · text muted "Where sign-in messages go" · action "Edit"
```

**Rationale:** The navigation already lists account, email, sign-in, and close as pages, so the menu only has to open one of them. Each page does one job and says what changes.

**Cost:** Four pages for one account is a tree of its own, and closing an account sits two levels from anywhere. The menu has to name the right page, not the account alone.


### Notification Inbox

**ID:** `litespec/frontend/patterns/navigation#notification-inbox`

**Strength:** default

**Question:** Reversals, disputes, invitations, and cost centre incidents all need somebody who was looking elsewhere. The console lists them under Needs attention on Home and nowhere else.

**Default:** `a-bell-in-the-header-with-a-dropdown-panel`

**Compact:** On a phone notifications open as a bottom sheet, unread first, where the thumb reaches them.

**Rationale:** A bell with a dropdown panel keeps the count visible on every page and answers what changed without leaving the list being worked. The inbox page is the runner-up for support days with dozens of items, where triage needs filters. Needs attention on the home page fits a console where the first page is the only queue.

Never ship no notifications. Polling seven areas by hand is how a dispute deadline is missed.

**Reviewed for:** Which built attention list breaks from the wireframe its catalog entry chose? Open the built inbox and bell, with triage and unread states, beside the catalog entry recording the chosen option and its reason.

#### `a-bell-in-the-header-with-a-dropdown-panel`: A Bell in the Header with a Dropdown Panel

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses."
    action "Notifications · 3 unread"
  region "Expenses"
    table 2 "Expense" "Spender" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · status "Paid" positive · text "EUR 125.00"
  popover "Notifications"
    list 3
      item: text "A dispute was opened on expense EXP-1041" · text muted "Tom Becker · 2 hours ago" · status "Disputed" critical
    action quiet "Mark all read"
    action quiet "Open inbox"
```

```wireframe compact
page
  header
    heading "Expenses"
    text "268 expenses."
  region "Expenses"
    list 1
      item: text "EXP-1042" · text "Ava" · status "Paid" positive
  drawer bottom "3 unread"
    list 3
      item: text "A dispute was opened on expense EXP-1041" · text muted "Tom Becker · 2 hours ago" · status "Disputed" critical
    action quiet "Mark all read"
```

**Rationale:** The count sits where the eye already is and the list is one click away, without leaving the page being worked. The panel answers what changed. The inbox page behind it answers the rest.

**Cost:** Three unread is a glance. Thirty is a dropdown that needs its own filters. The panel stays a summary and hands over to the inbox page past a handful.

**Reference:** GitHub


#### `an-inbox-page-with-triage`: An Inbox Page with Triage

```wireframe
page
  breadcrumb "Home" "Inbox"
  header
    heading "Inbox"
    text "4 unread across reversals, disputes and the team."
    action "Mark all read"
  tabs "All 12" "Unread 4"
    region "Unread"
      list 4
        item: text "A dispute was opened on expense EXP-1041" · text muted "Tom Becker · answers in 5 days" · status "Disputed" critical · action "Answer"
```


**Rationale:** Notifications become a queue with its own page: filter by unread, act, mark read. A support day with dozens of items needs filters rather than a dropdown.

**Cost:** A second queue beside Needs attention splits what needs doing across two pages. One of them has to own the ordering or they disagree.

**Reference:** Linear

#### `needs-attention-on-the-home-page`: Needs Attention on the Home Page

```wireframe
page
  header
    heading "Good afternoon, Alex"
    text "Three things need you today."
    action primary "New expense"
  region "Needs attention"
    status "3 to do" caution
    list 3
      item: text "A dispute was opened on expense EXP-1041" · text muted "Tom Becker · answers in 5 days" · action "Answer"
```

**Rationale:** One list on the first page after sign-in, sorted by what needs doing, with each row carrying the step that deals with it.

**Cost:** Home is one page among many, so a reader working a list all day never sees it. Anything urgent waits for a visit that may not come.


### Help and Support

**ID:** `litespec/frontend/patterns/navigation#help-and-support`

**Strength:** default

**Question:** The navigation names a help topic per page, so help can open the guide for the page on screen. The console links a help page from the sidebar.

**Default:** `contextual-help-beside-the-page-it-explains`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** The guide for the current page in a side panel answers the question without leaving the record. A help page from the sidebar is the runner-up index for a reader who knows what to ask. Help search inside the command palette fits a console where navigation queries already go to one input.

Never ship no help entry: a finance lead stuck at the office with a queue has nowhere else to turn.

**Reviewed for:** Which built guidance entry differs from the wireframe its catalog entry chose? Open the built help surfaces, from sidebar entry to contextual hints, beside the catalog entry recording the chosen option and its reason.

#### `contextual-help-beside-the-page-it-explains`: Contextual Help Beside the Page It Explains

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · Rotterdam client visit · EUR 125.00."
    action primary "Reversal expense"
  split 2:1
    region "The expense at a glance"
      fact "Paid by the spender" "EUR 125.00"
      fact "Paid" "8 Oct 2026, 09:34"
    aside
      card "Reversing this expense"
        text "Check the payment date: this method reversals only within 180 days."
        text "Past the window, offer credit or record an out-of-band payment."
        action "Open the full guide"
```

**Rationale:** The guide for the current page opens in a side panel, so the reader never leaves the record. The navigation already carries a topic per page, so this is a lookup rather than a search.

**Cost:** A panel beside the content halves the content width, and on a phone it stacks below the fold it explains. Every page also needs its topic written before the panel has anything to show.

**Reference:** Stripe

#### `a-help-page-from-the-sidebar`: A Help Page from the Sidebar

```wireframe
page
  breadcrumb "Home" "Help"
  header
    heading "Help"
    text "Guides for the expenses you hold, and the people who answer the rest."
  toolbar
    field "Search the guides" search
  region "Guides"
    list 4
      item: text "Filing expenses" · text muted "Trips, receipts, amounts and the expense list" · action "Open"
  region "Talk to support"
    list 1
      item: text "Contact support" · text muted "The support team answers on business days" · action "Write"
```

**Rationale:** One index of guides with search, reached from the sidebar on every page, plus a support row for what no guide answers.

**Cost:** An index answers the reader who knows what to ask. A reader stuck on a record has to translate the record into a search query first.

#### `help-search-inside-the-command-palette`: Help Search Inside the Command Palette

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense, newest first."
  dialog "Jump to"
    field "Search pages, expenses, trips" search = "reversal"
    region "Go to"
      list 2
        item: text "Reversals" · text muted "Pages"
    region "Guides"
      list 3
        item: text "Reversing an expense" · text muted "Guide · Reversals and receipts"
```

**Rationale:** Help queries go where navigation queries already go: one input searches pages, records, and guides together. A reader who learned the palette shortcut needs no second shortcut.

**Cost:** Guides compete with pages and records for the same short list, so a guide the reader wants can sit below records they do not. Help also inherits the palette discoverability problem.

**Reference:** Linear

### Workspace Switcher

**ID:** `litespec/frontend/patterns/navigation#workspace-switcher`

**Strength:** default

**Question:** People who run several cost centres think in cost centres: one cost centre at a time, switched between often. The console carries a cost centre's sections as tabs on the cost centre page.

**Default:** `a-workspace-switcher-at-the-top-of-the-sidebar`

**Compact:** On a phone the workspaces list as a bottom sheet with the state beside each name.

**Rationale:** A workspace switcher at the top of the sidebar moves the whole console without visiting the list. The trail sibling menu is the runner-up on record pages four levels deep, where the sidebar context is far away. Recent workspaces first in the palette fit a reader who switches by keyboard.

Never ship back-to-list as the only path. It costs the most to exactly the companies with the most cost centres.

**Reviewed for:** Which built cost centre picker breaks from the wireframe its catalog entry chose? Open the built switcher, with recent workspaces first, beside the catalog entry recording the chosen option and its reason.

#### `a-workspace-switcher-at-the-top-of-the-sidebar`: A Workspace Switcher at the Top of the Sidebar

```wireframe
page
  breadcrumb "Home" "Settings" "Cost centres" "Amsterdam sales"
  header
    heading "Amsterdam sales"
    status "Active" positive
    text "Amsterdam office · 4 cost centres."
    action "Switch cost centre"
    action primary "Add budget"
  region "Budget"
    fact "Cost centre" "Amsterdam sales"
    fact "On hand" "412 of 500 filed"
  popover "Switch cost centre"
    field "Search cost centres" search
    list 3
      item: text "Amsterdam sales" · text muted "Amsterdam office · 4 cost centres" · status "Active" positive
```

```wireframe compact
page
  header
    heading "Amsterdam sales"
    status "Active" positive
    text "Amsterdam office · 4 cost centres."
  drawer bottom "Switch cost centre"
    list 3
      item: text "Amsterdam sales" · text muted "Amsterdam office · 4 cost centres" · status "Active" positive
```


**Rationale:** The switcher sits at the top of the sidebar, above the navigation the switch changes. One control moves the whole console from the Amsterdam sales to Rotterdam.

**Cost:** The switcher changes what every entry below it means. It names the cost centre prominently enough that no one reads the wrong cost centre with confidence.

**Reference:** Vercel


#### `the-workspace-level-of-the-trail-as-a-sibling-menu`: The Workspace Level of the Trail as a Sibling Menu

```wireframe
page
  breadcrumb "Home" "Settings" "Amsterdam sales" "Budget"
  header
    heading "Budget"
    text "3 trip lines · 812 units filed."
    action primary "Add budget"
  region "Budget"
    table 2 "Item" "On hand" "Amount"
      item: text "EXP-1042" · text "412 of 500" · text "EUR 45.00"
  popover "Amsterdam sales"
    action "Amsterdam sales"
    action "Rotterdam sales"
    action "Utrecht sales"
```

**Rationale:** Each trail level can open a menu of its siblings, so the cost centre level lists the other cost centres. Switching sits exactly where the current cost centre is named.

**Cost:** A trail level that opens a menu is a trail that does not behave like one. Dozens of siblings need a search the trail has no room for. It fits companies with a handful of cost centres.

**Reference:** Notion

#### `recent-workspaces-first-in-the-palette`: Recent Workspaces First in the Palette

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense, newest first."
  dialog "Jump to"
    field "Search pages, expenses, trips" search
    region "Recent cost centres"
      list 2
        item: text "Amsterdam sales" · text muted "Amsterdam office · 4 cost centres" · status "Active" positive
    region "Go to"
      list 2
        item: text "Expenses" · text muted "Pages"
    text muted "Open with Enter."
```

**Rationale:** Recent records can appear before anything is typed, so returning to the cost centre left open is one keypress. Recency matches how cost centre and support work moves between a few cost centres.

**Cost:** Recency is personal state on a shared machine, so the next person at the keyboard sees what the last person opened. A renamed cost centre also needs a rule for what its row says.

**Reference:** Linear
