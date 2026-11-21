# Navigation Patterns

**Stack:** any
**Concern:** frontend-ui

The navigation patterns define the product frame around every screen: the sidebar, breadcrumb, header bar, tabs, icons, language and theme, command palette, account menu, notifications, help, and workspace switching. The frame moves a reader between areas and shows where the open page sits: every page opens directly from its own address, and the frame marks the area holding it.

A team building the product frame starts from these patterns. The owner picks an option from the drawings, and the team records the choice in its pattern catalog, which records the project's interface decisions. This page belongs to the Page Frames category. Its patterns are tested layouts a team chooses from, not required rules.

## Concepts

### How to Read a Pattern

Each pattern answers one recurring question about moving through the console, such as how the sidebar shows seven areas or how the trail behaves four levels deep. Each pattern names its question, the default option a project builds, how the default behaves on a narrow screen, and why it was chosen over the others.

Each option below it shows one wireframe drawing: a plain-text sketch of the parts on the screen, with no color, font, spacing, or component library. Each drawing carries its own rationale, its cost, and sometimes a reference to the outside product it learns from. The team builds the chosen option with its own components, in its own design system.

A pattern catalog is the project's record of these choices: one entry per pattern, naming the option picked and who picked it. [UI Patterns](../patterns.md) states what each catalog entry holds and the wireframe language these drawings share.

### The Running Example

The drawings on this page use the Claimwick expense console for Harbor and Pine, with seven areas from Home to Settings, expense EXP-1042 for Ava, and the Amsterdam sales cost centre.

The frame marks the current area, and every page has its own address. The Conventions section states the two defaults every option keeps unless the project records another choice: the navigation marks the section holding the current page, and a row of sibling pages is a navigation landmark of links rather than a tab list. Every page opens directly from its own URL under `litespec/frontend/structure#organize-a-frontend-into-frame-routes-and-modules`, which gives each address its route entry.

## Conventions

### Mark the Current Section

**ID:** `litespec/frontend/patterns/navigation#mark-the-current-section`

**Strength:** default

**Default:** Mark the section that holds the current page in the navigation.

**Replacement:** A project that replaces the default records how its navigation marks the current section.

**Rationale:** A marked section tells a reader who arrived from a link, a notification, or a search which area holds the open page. Without it, every page looks like the start, and the reader reopens the sidebar to learn where they already are.

**Example:** Ava opens expense EXP-1042 from a notification; the Expenses area stays highlighted in the sidebar, so she knows which area holds the record.

**Reviewed for:** Does every built screen highlight the area holding its open page? Open three screens reached by link, notification, and search.

### Draw Sibling Pages as Links, Not Tabs

**ID:** `litespec/frontend/patterns/navigation#draw-sibling-pages-as-links-not-tabs`

**Strength:** default

**Default:** Build a row of sibling pages as a navigation landmark of links with `aria-current="page"` on the current page, leaving the tab list for switching panels within one page.

**Replacement:** A project that replaces the default records how its sibling pages expose themselves.

**Rationale:** A screen reader announces a tab list as panels on one page, so a reader who opens Receipts from a tab expects the address to stay and the Back button to skip it. A navigation landmark of links matches the behavior: each page has its own address, and the Back button returns to the page before it. WCAG 2.2 criterion 1.3.1 Info and Relationships and criterion 4.1.2 Name, Role, Value require the announced role to match the behavior.

**Example:** On the Standard expense page, the Amounts, Receipts, Reversals, and Details row looks like tabs but is built as links: opening Receipts changes the address, and the Back button returns to the previous page.

**Reviewed for:** Is every built row of sibling pages a navigation landmark of links instead of a tab list? Open each built row, and check the address changes and the Back button returns.


## Patterns

### Sidebar

**ID:** `litespec/frontend/patterns/navigation#sidebar`

**Strength:** default

**Question:** How does the sidebar show seven top-level areas when Settings alone holds thirty routes? Groups expand to two levels, and the current item stays marked wherever it sits.

**Default:** `grouped-areas-with-expandable-sub-items`

**Compact:** Below the md breakpoint, the sidebar turns into an off-canvas sheet: the same entries slide in over a scrim, and a control closes them again.

**Rationale:** Grouped areas with expandable sub-items are the default: two levels hold all seven areas, each sub-item sits indented under its area, and the current item stays marked. Groups with live counts are the runner-up for areas that hold a queue, such as reversals, where the count saves a visit. Recent pages above the areas fit a reader who resumes the same few records.

Never ship a flat list of every page: past a hundred entries the list stops showing the structure, and that lookup belongs to the command palette.

**Reviewed for:** Does the built area list match the wireframe its catalog entry chose? Open the built console frame, with its grouped areas and marked item, beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** Two levels hold all seven areas: each sub-item sits indented under its area, the current item stays marked wherever it sits, and the account sits at the foot of the sidebar.

**Cost:** The group holding thirty settings routes runs longer than the screen. Settings stays open on every settings page, so it never collapses.



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

**Rationale:** Each group carries the number the reader would otherwise have to go and fetch: 10 under Reversals, 3 team invitations, 5 disputes. Showing that count is the group's whole job.

**Cost:** A count beside every area has to stay correct on every read, and an area with a zero count looks finished rather than empty.


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

**Rationale:** The three most recent pages sit above the seven areas, so resuming work costs one click instead of re-walking the tree. This is the only option that remembers where the reader was: it navigates by recency rather than by structure.

**Cost:** Recent pages belong to one person, so a shared screen shows someone else's trail. An archived or renamed recent also needs a rule for what its row says.


### Breadcrumb

**ID:** `litespec/frontend/patterns/navigation#breadcrumb`

**Strength:** default

**Question:** How does the trail behave four levels deep on an expense record? The trail never wraps and may scroll sideways, its last item carries aria-current, and Dutch strings sometimes appear inside an English frame.

**Default:** `the-trail-as-the-parent-chain`

**Compact:** Below the md breakpoint, the trail collapses to one back link to the parent, shown above the heading.

**Rationale:** The parent chain is the default because it states where a record sits in the tree, with the last item marked current. The trail that truncates from the left is the runner-up for four-level chains: its overflow menu keeps the current page visible. The current title as a sibling menu fits records whose siblings get opened together.

Never ship with no trail at all: on a phone the sidebar is closed, so the trail is the only statement of where a record sits.

**Reviewed for:** Does the built parent chain match the wireframe its catalog entry chose? Open a built four-level trail, with its truncation and parent links, beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** Every level is a link except the last, which carries the current item. The trail is the one place that states where the record sits in the tree.

**Cost:** Four levels on an expense plus the no-wrap rule borrowed from Fluent push the last item off a phone screen, unless the trail scrolls sideways.



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

**Rationale:** The trail truncates past a fixed length with a tooltip, and the levels from the second on collapse into an overflow menu. A long trip name never pushes the current page off the screen.

**Cost:** The trail menu is a third kind of menu on a page that already has row menus and header menus, and the reader has to open it just to see where they are.

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

**Rationale:** The current page title opens a menu of its siblings, so moving sideways costs one click instead of going up and back down. On the Standard expense, opening it lists Economy and Bundle beside Standard.

**Cost:** A heading that opens a menu doesn't behave like a heading, and the menu repeats the sidebar section the reader just left. This option fits only where siblings get visited together.

**Reference:** Notion


### Header Bar

**ID:** `litespec/frontend/patterns/navigation#header-bar`

**Strength:** default

**Question:** Which facts does the top bar carry, and when does the trail move into it? The candidates are the page title, the company name, who is acting, a sidebar toggle, a help control, and the theme.

**Default:** `the-bar-with-the-company-name`

**Compact:** The default reflows on a narrow screen: the bar keeps the page title, the sidebar toggle, and help on one line, while the company name and the acting person move into the account menu.

**Rationale:** The bar with the company name and the acting person is the default because it states the two facts a support conversation needs first: which company, and who is acting. The bar as the breadcrumb is the runner-up when the trail would otherwise repeat the page title. The deployment identity band fits a sandbox deployment, where naming the deployment keeps it from being confused with production.

Never ship with no bar at all: burying the theme and language in the sidebar foot turns a visible control into three taps.

**Reviewed for:** Does the built top band match the wireframe its catalog entry chose? Open the built trip header, with its company name and deployment identity, beside the catalog entry recording the chosen option and its reason.

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
    table 4 "Expense" "Spender" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · status "Paid" positive · text "EUR 125.00"
```

**Rationale:** The page title sits on the left, followed by the facts that hold on every page: which company, who is acting, help, and the theme. The sidebar toggle stays in the bar because a phone needs it.

**Cost:** Four items sit above the content on every page, and the brand mark repeats what the sidebar already says.


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

**Rationale:** The chain moves into the bar, where the page title already sits, so one place states where the reader is rather than two.

**Cost:** The bar appears on every page, including list pages with no chain, so it takes two different shapes. A breadcrumb sits below the header as a page-level element in many systems, and this bar takes over that job.


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

**Rationale:** On a sandbox deployment, the bar says so on every page, next to the reminder that nothing here is charged. Naming the deployment is the identity band's one job.

**Cost:** The deployment identity competes with the page title for space, and a reader who has seen the band fifty times stops seeing it.


### Tabs Within a Page

**ID:** `litespec/frontend/patterns/navigation#tabs-within-a-page`

**Strength:** default

**Question:** Sibling pages of one area sit in a sub-page navigation. A second tab row uses a subordinate level. Tab sets stay small, at about six tabs, and the active tab belongs in the URL.

**Default:** `the-shipped-tab-row-with-the-tab-in-the-url`

**Compact:** The default reflows on a narrow screen: the row scrolls on one line, keeps the current page in view, and never wraps.

**Rationale:** The tab row with the active tab in the URL is the default because a pasted link carries the facet. The row looks like tabs but is built as navigation links, one address per page, following Draw Sibling Pages as Links, Not Tabs. The current page is marked by more than color, with weight or an underline. Tabs drawn as real links are the runner-up: middle click and the Back button behave as a reader expects. Headed sections on one page fit a record with a handful of facets that all fit.

Never ship a segmented control for facets: it fits at most three segments, and it can't carry counts or overflow.

**Reviewed for:** Does the built tab row match the wireframe its catalog entry chose? Open the built in-page tabs, with the active tab in the URL, beside the catalog entry recording the chosen option and its reason.

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


**Rationale:** The tab row sits under the header on an expense record, with the active tab as a URL parameter, so a pasted link carries the facet.

**Cost:** A reader cannot compare two facets, and every new backend facet adds a tab. That is why tab sets stay small, at about six.

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

**Rationale:** The tab labels are real links to sibling routes, not buttons that swap a panel. Middle click, open in a new tab, and the Back button all behave the way a reader expects.

**Cost:** Each tab becomes a real address through an anchor link with a URL fragment. This is the most defensible option available rather than an alternative.

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

**Rationale:** Where a record has a handful of facets and they all fit, keep headed sections on one page: nothing hides behind a click. Headed sections on one page are the first-class alternative to tabs and the right answer for most records.

**Cost:** The page grows long, and it stays honest only while the sections stay few. That is why a record with eight facets carries tabs rather than eight stacked sections.

**Reference:** GOV.UK

### Icons in the Frame

**ID:** `litespec/frontend/patterns/navigation#icons-in-the-frame`

**Strength:** default

**Question:** Which parts of the frame get icons? Seven area icons, a sidebar toggle, a help mark, and the theme control. Each one is a named component with an accessible label, never a raw element.

**Default:** `an-icon-beside-every-label`

**Compact:** The default reflows on a narrow screen: the areas stack above the content in one column, and each icon stays beside its label.

**Rationale:** An icon beside every label is the default because the icon reinforces the label instead of replacing it, which makes seven areas easy to scan. Icons only for the actions, with words for the areas, are the runner-up where the project keeps a small icon set. Words alone fit a small set of areas with no icon set to keep.

Never ship icons that carry state in the frame: the theme and language as bare glyphs force the reader to hover over each to tell them apart.

**Reviewed for:** Which built label or action carries an icon outside the set the catalog entry chose? Open the built frame, with its labeled actions and marked current item, beside the catalog entry recording the chosen option and its reason.

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


**Rationale:** Every area carries an icon and its name, so the icon reinforces the label instead of standing as the only label. That pairing is what makes seven areas easy to scan.

**Cost:** Seven icons are seven drawings to keep consistent and to name, and icons in tab labels are refused for the same reason.

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


**Rationale:** This is the split the evidence supports. An icon reinforces a labeled area, but an icon alone is never the label for a place. An action with one unmistakable meaning can stand as an icon with a tooltip and an accessible name.

**Cost:** An action that has to be learned has to be hovered first. One action per notification and no Dismiss button apply the same economy to buttons.

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

**Rationale:** The labels are the navigation: nothing to draw, nothing to name, and nothing that fails when a reader can't tell icons apart.

**Cost:** Seven same-weight labels in the same style make a wall of text, and the eye has nothing to catch on. This shape only works with a bold current item.


### Language and Theme

**ID:** `litespec/frontend/patterns/navigation#language-and-theme`

**Strength:** default

**Question:** Where do the language and theme switches live? The two switches change nothing about the data, yet they are the most error-prone controls on the page. An explicit choice wins over the account default, and a missing translation falls back to the other language.

**Default:** `the-theme-following-the-system-with-an-override`

**Compact:** The default reflows on a narrow screen: the language switch stays in the header showing its value, and the theme choice stays in the account menu.

**Rationale:** The default splits the two switches. The theme follows the device, because few readers change it during a day and the device already states the preference. System, Light, and Dark stay in the account menu for the reader who overrides it. The language stays in the header showing its value, because a wrong language makes every other word unreadable. Both switches in the header, each showing its value, are the runner-up for a product whose staff switch theme during a shift.

Never ship with no language switch: one language is a commercial decision about which companies Claimwick serves, and Claimwick already serves companies in two languages.

**Reviewed for:** Do the built locale and appearance controls match the wireframe the catalog entry chose? Open the built controls, in the header and account menu, beside the catalog entry recording the chosen option and its reason.

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
    table 4 "Expense" "Spender" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · status "Paid" positive · text "EUR 125.00"
```

**Rationale:** Each control shows its current value as text rather than an icon, so the reader sees what is set without opening anything.

**Cost:** Two text buttons sit in a header of text, and a reader who wants to change the language twice a day reads 'EN' as decoration rather than a control.


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

**Rationale:** Everything about who the reader is and how the console looks sits in one place, leaving the header for the page title. The account, the language, the theme, and sign-out are one decision in one menu.

**Cost:** A reader who only wants dark mode at the end of a shift pays two taps. The theme is not the kind of thing a reader opens an account menu to find.


#### `the-theme-following-the-system-with-an-override`: The Theme Following the System, with an Override

```wireframe
page
  toolbar
    text "Expenses"
    field "Language" select = "English"
    action "Account: Alex Morgan"
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses."
  menu "Alex Morgan · Harbor and Pine"
    action "Your account"
    action "System"
    action "Light"
    action "Dark"
    action "Sign out"
  region "Expenses"
    table 4 "Expense" "Spender" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · status "Paid" positive · text "EUR 125.00"
```

**Rationale:** The theme follows the device until the reader overrides it with System, Light, or Dark in the account menu. The language stays in the header showing its value, so a wrong language shows without opening anything.

**Cost:** A reader whose system is dark gets a dark console without asking: that is the correct behavior. A company capturing a screen for a spender then gets a dark screenshot.


### Command Palette

**ID:** `litespec/frontend/patterns/navigation#command-palette`

**Strength:** default

**Question:** How does a reader get one record or run one command and return to the list without losing their place? An operations manager working a list often needs one record or one command and then the list again. The palette jumps straight to the expense for Rotterdam client visit. It runs the next step from the keyboard, so the reader never loses the page they were working on.

**Default:** `cmd-k-dialog-with-grouped-results`

**Compact:** On a phone, the palette opens as a bottom sheet, where the thumb reaches the input, with recent records above the fold.

**Rationale:** The grouped Cmd+K dialog is the default because it puts pages above records, so one typed query answers both where to go and what to open. Recent items first are the runner-up for cost centre and support work that moves between a few records. Search scoped to the current area fits a reader who is already in the right list.

Never ship command verbs with a preview as the default palette: every verb becomes a second path to a use case.

**Reviewed for:** Does the built jump-to dialog match the wireframe its catalog entry chose? Open the built palette, with its grouped results and recent items, beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** One dialog opens over the current page, with pages above records, so one typed query answers both where to go and what to open. The groups keep the list short enough to scan while the query is still short.

**Cost:** Pages and records compete for the same short list, so a wanted page can sit below unwanted records.



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

**Rationale:** Recent records sit above the groups before anything is typed, so returning to the expense left open costs one keypress. That matches how cost centre and support work moves between a few records.

**Cost:** Recency is personal state, so on a shared machine the next person at the keyboard sees what the last person opened.


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

**Rationale:** The scope starts on the area the reader is already in: typing an expense number on the expenses page searches expenses first. Widening the search to everything costs one explicit step.

**Cost:** A scoped default hides matches elsewhere, so a reader who misreads the scope concludes a record is missing.


### Account Menu

**ID:** `litespec/frontend/patterns/navigation#account-menu`

**Strength:** default

**Question:** How does the frame say who is acting, for which company, and with what access? The account sits at the foot of the sidebar, with a way to switch identity beside it on a development deployment.

**Default:** `an-avatar-menu-in-the-header`

**Compact:** The default reflows on a narrow screen: the avatar stays in the header, and the menu opens as a bottom sheet with the same entries.

**Rationale:** The avatar menu in the header is the default because it holds the account page, the access statement, and sign-out in one control. The sidebar foot is the runner-up, but it sits far from the top of a long page. The account as a page reached from the menu fits a console where account, email, sign-in, and close are separate pages.

Never ship a bare sign-out link: an account with no page turns the language, sessions, and closing the account into support messages.

**Reviewed for:** Does the built sign-in menu match the wireframe its catalog entry chose? Open the built account controls, in the sidebar foot or header, beside the catalog entry recording the chosen option and its reason.

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


**Rationale:** One avatar in the header holds the account page, the preferences, and sign-out. The acting person shows on every page, with no scroll to the foot.

**Cost:** The header gains a control on every page, and while both exist the menu repeats the sidebar foot it replaces. The foot still needs a sign-out for readers who look there first.

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

**Rationale:** The foot names the acting person and role on every page. Opening it switches identity on a development deployment.

**Cost:** The foot of a long sidebar sits far from the top of the page. On a phone the whole account hides inside the sheet, and the bar says nothing about who is acting.


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

**Rationale:** Account, email, sign-in, and close are already pages in the navigation, so the menu only opens one of them. Each page does one job and says what changes.

**Cost:** Four pages for one account make a tree of their own, and closing an account sits two levels from anywhere. The menu has to name the right page, not the account alone.


### Notification Inbox

**ID:** `litespec/frontend/patterns/navigation#notification-inbox`

**Strength:** default

**Question:** How does the console reach somebody who was looking elsewhere? Reversals, disputes, invitations, and cost centre incidents all need that person. The console lists them under Needs attention on Home, and nowhere else.

**Default:** `a-bell-in-the-header-with-a-dropdown-panel`

**Compact:** On a phone, notifications open as a bottom sheet where the thumb reaches them, unread first.

**Rationale:** A bell with a dropdown panel is the default because it keeps the count visible on every page and answers what changed without leaving the list being worked. The inbox page is the runner-up for support days with dozens of items, where triage needs filters. Needs attention on the home page fits a console where the first page is the only queue.

Never ship with no notifications: polling seven areas by hand is how a dispute deadline gets missed.

**Reviewed for:** Does the built attention list match the wireframe its catalog entry chose? Open the built inbox and bell, with triage and unread states, beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The count sits where the eye already is, and the list is one click away, without leaving the page being worked. The panel answers what changed, and the inbox page behind it answers the rest.

**Cost:** Three unread items are a glance; thirty are a dropdown that needs its own filters. The panel stays a summary, and hands over to the inbox page past a handful.

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


**Rationale:** Notifications become a queue with its own page, where the reader filters by unread, acts, and marks read. A support day with dozens of items needs filters rather than a dropdown.

**Cost:** A second queue beside Needs attention splits the work across two pages. One of them has to own the ordering, or the two disagree.

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

**Rationale:** One list on the first page after sign-in holds the work sorted by what needs doing. Each row carries the step that deals with it.

**Cost:** Home is one page among many, so a reader who works a list all day never sees it. Anything urgent waits for a visit that may never come.


### Help and Support

**ID:** `litespec/frontend/patterns/navigation#help-and-support`

**Strength:** default

**Question:** How does a reader open help for the page on screen? The navigation names a help topic per page, so help can open the guide for the page on screen. The console also links a help page from the sidebar.

**Default:** `contextual-help-beside-the-page-it-explains`

**Compact:** The default reflows on a narrow screen: the help panel stacks below the content it explains, with the same guide and the link to the full guide.

**Rationale:** The guide for the current page in a side panel is the default because it answers the question without leaving the record. A help page from the sidebar is the runner-up: an index for a reader who knows what to ask. Help search inside the command palette fits a console where navigation queries already go to one input.

Never ship with no help entry: a finance lead stuck at the office with a queue has nowhere else to turn.

**Reviewed for:** Does the built guidance entry match the wireframe its catalog entry chose? Open the built help surfaces, from sidebar entry to contextual hints, beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The guide for the current page opens in a side panel, so the reader never leaves the record. Because the navigation already carries a topic per page, opening the guide is a lookup rather than a search.

**Cost:** A panel beside the content halves the content width, and on a phone it stacks below the content it explains. Every page also needs its topic written first, or the panel has nothing to show.

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

**Rationale:** One index of guides with search is reached from the sidebar on every page. A support row covers what no guide answers.

**Cost:** An index answers the reader who knows what to ask. A reader stuck on a record first has to translate the record into a search query.

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

**Rationale:** Help queries go where navigation queries already go. One input searches pages, records, and guides together, so a reader who learned the palette shortcut needs no second one.

**Cost:** Guides compete with pages and records for the same short list, so a wanted guide can sit below unwanted records, and help inherits the palette's discoverability problem.

**Reference:** Linear

### Workspace Switcher

**ID:** `litespec/frontend/patterns/navigation#workspace-switcher`

**Strength:** default

**Question:** How does a reader move between cost centres? People who run several think in cost centres: one at a time, switched between often. The console carries a cost centre's sections as tabs on the cost centre page.

**Default:** `a-workspace-switcher-at-the-top-of-the-sidebar`

**Compact:** On a phone, the workspaces list as a bottom sheet, with the state beside each name.

**Rationale:** A workspace switcher at the top of the sidebar is the default because it moves the whole console without visiting the list. The trail sibling menu is the runner-up on record pages four levels deep, where the sidebar context is far away. Recent workspaces first in the palette fit a reader who switches by keyboard.

Never ship back-to-list as the only path: it burdens most the companies with the most cost centres.

**Reviewed for:** Does the built cost centre picker match the wireframe its catalog entry chose? Open the built switcher, with recent workspaces first, beside the catalog entry recording the chosen option and its reason.

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


**Rationale:** The switcher sits at the top of the sidebar, above the navigation the switch changes. One control moves the whole console from Amsterdam sales to Rotterdam sales.

**Cost:** The switcher changes what every entry below it means, so it has to name the cost centre prominently enough that nobody reads the wrong one with confidence.

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

**Rationale:** Each trail level can open a menu of its siblings, so the cost centre level lists the other cost centres. The switch sits exactly where the current cost centre is named.

**Cost:** A trail level that opens a menu doesn't behave like a trail. Dozens of siblings need a search that the trail has no room for, so this option fits companies with a handful of cost centres.

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

**Rationale:** Recent cost centres can appear before anything is typed, so returning to the cost centre left open costs one keypress. That matches how cost centre and support work moves between a few cost centres.

**Cost:** Recency is personal state, so on a shared machine the next person at the keyboard sees what the last person opened. A renamed cost centre also needs a rule for what its row says.

**Reference:** Linear
