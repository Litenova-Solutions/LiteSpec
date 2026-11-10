# Dialog and Overlay Patterns

**Stack:** any
**Concern:** frontend-ui

## Intent

A dialog, drawer, popover, menu, toast, context menu, shortcut overlay, bottom sheet, or persistent bar floats above or beside the page to confirm, edit, preview, or announce without a full navigation. This page shows when each overlay fits and how it opens, holds focus, and closes, as drawn options with a named default to build. The product owner chooses among the options when the team adds a confirmation, a side preview, or any floating layer. Right-sized overlays keep staff oriented: the page stays visible behind short tasks, and irreversible commands get the full attention they need.

## Concepts

### How to Read a Pattern

Each pattern answers one recurring question about floating layers, such as when a short task belongs in a dialog. It states the question, the default option a project builds, how the default behaves on a narrow screen, and why the default won. Each option then shows one wireframe drawing with its own rationale, cost, and optional reference. A project records its choice in its pattern catalog and builds the chosen option with its own components. The [UI patterns](../patterns.md) page states the catalog contract and the wireframe language.

### The Running Example

The drawings on this page use the Claimwick payout desk: the March payout run with 1,204 payouts, expenses such as EXP-1042 for Ava, and the reversal and note commands staff run on them. On this page, the reader is the staff member using the screen. The Standards section states the two rules every option keeps: a confirmation dialog labels its confirm action with the command it runs, and an overlay returns focus to the control that opened it.

## Standards

### Name the Command on a Confirm Action

**ID:** `litespec/frontend/patterns/dialogs-and-overlays#name-the-command-on-a-confirm-action`

**Strength:** rule

**Statement:** A confirmation dialog MUST label its confirm action with the command it runs.

**Rationale:** Staff often scan only the buttons before clicking, so the button text is the last chance to state what happens. A button labeled `OK` or `Yes` forces the reader to reread the dialog title to know what they confirm, and under time pressure they click without rereading and run the wrong command.

**Reviewed for:** Which built confirmation labels its commit button with a bare Yes or OK? Open the confirm dialogs on built screens.

### Return Focus When an Overlay Closes

**ID:** `litespec/frontend/patterns/dialogs-and-overlays#return-focus-when-an-overlay-closes`

**Strength:** rule

**Statement:** An overlay MUST return focus to the control that opened it when it closes.

**Rationale:** Keyboard and screen-reader users navigate by focus position, so closing an overlay must put them back where they started. Focus that falls back to the top of the page strands them far from their row in a long list, and they must tab through the whole page to continue.

**Reviewed for:** Which built overlay leaves focus lost after it closes? Open and dismiss each dialog, drawer, popover, menu, and toast on built screens with a keyboard.

## Patterns

### Confirming a Command That Cannot Be Taken Back

**ID:** `litespec/frontend/patterns/dialogs-and-overlays#confirming-a-command-that-cannot-be-taken-back`

**Strength:** default

**Question:** When does a command deserve a confirmation step? A payout-run cancellation cannot be taken back. A confirmation used on routine commands becomes a reflex click that protects nothing.

**Default:** `name-the-object-state-the-consequence-offer-the-alternative`

**Compact:** The recommended option reflows: the dialog becomes a bottom sheet with the title naming the object, the consequence in the body, and the actions stacked full width.

**Rationale:** Keep the dialog that names the object, states the consequence in numbers, and offers the alternative in the same surface. The runner-up is the dialog with the consequence as numbers. It is the better choice wherever money moves, because the reader sees the number before confirming.

Never ship the undo toast for an irreversible cancellation. An undo there is a second command rather than a reversal.

**Reviewed for:** Which built irreversible step departs from the wireframe its catalog entry chose? Open the built confirmation dialogs beside the catalog entry recording the chosen option and its reason.

#### `name-the-object-state-the-consequence-offer-the-alternative`: Name the Object, State the Consequence, Offer the Alternative

```wireframe
page
  breadcrumb "Home" "Payouts" "March payout run"
  header
    heading "March payout run"
    text "Pays Sat 14 Mar 2026. 1,204 payouts."
    action "Move the run"
  dialog "Cancel the March payout run?"
    text "1,204 spenders await payout and will be told the run is off."
    fact "Held" "EUR 54,180.00 back in the March budget"
    text "Two people on the payout rota will be told to stand down."
    message "This cannot be undone. A canceled run cannot be scheduled again." critical
    link "Move the run instead of canceling it"
    action "Keep the run"
    action destructive "Cancel the run"
```

```wireframe compact
page
  breadcrumb "Home" "Payouts" "March payout run"
  header
    heading "March payout run"
    text "Pays Sat 14 Mar 2026. 1,204 payouts."
    action "Move the run"
  drawer bottom
    heading "Cancel the March payout run?"
    text "1,204 spenders await payout and will be told the run is off."
    fact "Held" "EUR 54,180.00 back in the March budget"
    message "This cannot be undone. A canceled run cannot be scheduled again." critical
    footer
      action "Keep the run"
      action destructive "Cancel the run"
```

**Rationale:** The title asks the question, the body states what the object is and what happens, and the safe way out sits in the same dialog. The commit button repeats the verb, so the last thing the reader sees says what the click does.

**Cost:** Three pieces of copy per destructive command, in two languages, all of which have to stay true as the domain changes. The words are a second copy of the domain rules.

**Reference:** MOJ, Carbon

#### `a-dialog-with-the-consequence-as-numbers`: A Dialog with the Consequence as Numbers

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava of Harbor and Pine. Train fare, 2 fares."
    action "Reverse"
  region "What was bought"
    fact "Two fares" "EUR 90.00"
    fact "Booking fee" "EUR 5.00"
    fact "Paid to the spender" "EUR 125.00"
  dialog "Reverse EXP-1042 in full?"
    fact "Fares and extras" "EUR 125.00"
    fact "Returns from Ava" "EUR 125.00"
    message "The two fares go back to drafts and claim again. Personal item is not reversed." neutral
    action "Reverse part of it"
    action "Cancel"
    action destructive "Reverse in full"
```

**Rationale:** Where a command changes money, the dialog shows the money. The reader sees the number before committing to it.

**Cost:** The dialog reads the number at the moment of the click, which means a read between the button and the command. On a slow connection the dialog waits.

#### `no-dialog-at-all-act-then-offer-an-undo`: No Dialog at All: Act, Then Offer an Undo

```wireframe
page
  breadcrumb "Home" "Payouts" "March payout run"
  header
    heading "March payout run"
    status "Canceled" neutral
    text "Canceled at 14:22. 1,204 spenders have been told."
  region "Budget"
    fact "Returned" "EUR 54,180.00 to the March budget"
    fact "Messages" "1,204 sent at 14:22"
  toast "Run canceled at 14:22. Undo stays until dismissed." critical
```

**Rationale:** The command runs and a toast names what happened with an Undo that stays until it is dismissed.

**Cost:** On money this trains the reflex the confirmation exists to prevent. An Undo on a reversal is a second reversal command rather than a reversal of the first.

**Reference:** Carbon

### Short Task in a Dialog

**ID:** `litespec/frontend/patterns/dialogs-and-overlays#short-task-in-a-dialog`

**Strength:** default

**Question:** A short task with one to four inputs and nothing to consult fits a dialog. More than four inputs, sections, or steps belong on a full page. The decision is which surface each short command takes, and what to do when a dialog would need a second dialog.

**Default:** `two-or-three-inputs-one-dialog`

**Compact:** The recommended option reflows: the dialog becomes a full-screen sheet with the fields stacked and the actions pinned at the foot.

**Rationale:** Keep one dialog shape for commands with one to four inputs: title, fields, one verb, no navigation. The runner-up is the task on the page. It is the better choice for commands that repeat all day, where leaving the page costs more than the dialog saves.

Never ship a dialog over a dialog. A helper that supports the nesting does not make it readable.

**Reviewed for:** Which built two-input task departs from the wireframe its catalog entry chose? Open the built small dialogs beside the catalog entry recording the chosen option and its reason.

#### `two-or-three-inputs-one-dialog`: Two or Three Inputs, One Dialog

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "3 lines for Harbor and Pine. Submitted 8 Oct 2026, 09:32."
  region "Notes"
    list 1
      item: text "Harbor and Pine approved the Friday trip" · text muted "Cara, 2 Oct 2026"
  dialog "Add a note"
    field "Note" textarea
    field "About" select = "The spender"
    action "Cancel"
    action primary "Add the note"
```

```wireframe compact
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "3 lines for Harbor and Pine. Submitted 8 Oct 2026, 09:32."
  region "Notes"
    list 1
      item: text "Harbor and Pine approved the Friday trip" · text muted "Cara, 2 Oct 2026"
  drawer bottom
    heading "Add a note"
    field "Note" textarea
    field "About" select = "The spender"
    footer
      action "Cancel"
      action primary "Add the note"
```

**Rationale:** One dialog shape for short commands: title, two fields, Cancel and the commit verb. This is where most short commands belong.

**Cost:** A dialog is a separate surface from the page. A refusal has to be worded in the dialog rather than on the page it came from.

#### `the-task-on-the-page-not-in-a-dialog`: The Task on the Page, Not in a Dialog

```wireframe
page
  breadcrumb "Home" "Expenses" "Reversals"
  header
    heading "Reversals"
    text "31 expenses to decide. Ade Okafor spent EUR 45.00."
  toolbar
    field "Search reversals" search
    field "Window" select = "Any window"
  table 4 "Expense" "Spender" "Amount" "Command"
    item: field "Reversal amount for EXP-1035" text = "45.00" · field "Route" select = "Deduction from the next payout" · action "Cancel" · action primary "Apply deduction"
  region "The rest of the queue"
    list 4
      item: text "EXP-1037" · text muted "Elin Lindqvist" · status "No route" critical · text "EUR 45.00"
```

**Rationale:** Where the command repeats, the task is completable on the main page rather than in a modal. An inline row becomes an editing row, which is where a repeatable task belongs.

**Cost:** Inline editing works only on a narrow table, and a row must look visibly different or people edit it by accident.

**Reference:** NN/g

#### `a-dialog-whose-second-step-is-another-dialog`: A Dialog Whose Second Step Is Another Dialog

```wireframe
page
  breadcrumb "Home" "Settings" "Portal"
  header
    heading "Portal"
    text "The logo spenders see."
  region "Logo"
    field "Drop a logo here or choose a file" file
    text muted "A square image of at most 2 MB."
  dialog "That file is not an image"
    text "harbor-pine-logo.psd is a design file. The portal takes JPEG, PNG or WebP."
    action "Try another file"
    action "Cancel"
```

**Rationale:** Refusing a file needs its own question, and the second dialog opens over the first.

**Cost:** A dialog that triggers another dialog, and a modal over a panel, are both refused: the nesting is inaccessible and confusing. This is the shape the project forbids.

**Reference:** Atlassian

### Drawer Beside the List

**ID:** `litespec/frontend/patterns/dialogs-and-overlays#drawer-beside-the-list`

**Strength:** default

**Question:** A list action opens a drawer so a reader can see a record without leaving the list. The drawer holds read-only context that supports the task. Below a narrow width the drawer takes the whole screen with a Back control.

**Default:** `read-only-record-beside-the-list`

**Compact:** The recommended option reflows: at phone width there is no beside, so the drawer takes the screen and Back returns to the list at the same scroll position with its filters intact.

**Rationale:** Keep the read-only record beside the list. The list keeps its scroll and its filters while the record reads beside it, and nothing in the drawer is editable.

Prefer the tabbed drawer when support compares reversals across facts, history, and messages. Never ship the filter drawer. Filters belong in the toolbar, and a drawer over the results hides the list the filters describe.

**Reviewed for:** Which built side preview departs from the wireframe its catalog entry chose? Open the built drawer variants over and next to the list, with the catalog entry recording the chosen option and its reason.

#### `read-only-record-beside-the-list`: Read-Only Record Beside the List

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses. Select a row to read it here."
  toolbar
    field "Search expenses" search
    field "State" select = "Any state"
  table 3 "Expense" "Spender" "Paid"
    item: text "EXP-1042" · text "Ava" · text "EUR 125.00"
  drawer right "EXP-1042"
    status "Paid" positive
    fact "Spender" "Ava"
    fact "Submitted" "8 Oct 2026, 09:32"
    fact "Paid" "EUR 125.00"
    fact "Lines" "3, none approved"
    action "Resend the receipt"
    link "Open the full record"
```

```wireframe compact
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava of Harbor and Pine. Submitted 8 Oct 2026, 09:32."
    action "Back to all expenses"
  region "The spender"
    fact "Name" "Ava"
    fact "Email" "ava@harbor-pine.example"
  region "What was bought"
    fact "Train fare, 2 fares" "EUR 90.00"
    fact "Booking fee" "EUR 5.00"
    fact "Paid" "EUR 125.00"
  footer sticky
    action primary "Message the spender"
```

**Rationale:** The recommended shape. The list is untouched and narrowed, the record holds facts, and the way out is back to the full record. Nothing in the drawer is editable, so it cannot become a task.

**Cost:** A 420-pixel drawer on a 1440 screen leaves the list about 900 wide, which fits five columns and not eight. The screen then holds two navigations, the list and the drawer, side by side.

**Reference:** Atlassian

#### `a-tabbed-drawer-the-record-its-history-its-messages`: A Tabbed Drawer: The Record, Its History, Its Messages

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses. Select a row to read it here."
  toolbar
    field "Search expenses" search
  table 3 "Expense" "Spender" "Paid"
    item: text "EXP-1042" · text "Ava" · text "EUR 125.00"
  drawer right "EXP-1042"
    status "Paid" positive
    text "Ava of Harbor and Pine."
    tabs "Record" "History" "Messages"
      region "History"
        timeline 3
          item: text "Receipt resent" · text muted "8 Oct 2026, 11:02" · text muted "Eli"
    action "Resend the receipt"
    action primary "Open the record"
```

**Rationale:** The row stays selected behind while tabs divide facts from history. Three views in one drawer beat three navigations when support compares reversals.

**Cost:** Tabs inside a drawer are two navigations deep on one screen. Readers lose track of which tab held the fact they half-read, and the Back control has two jobs.

**Reference:** Linear

#### `the-drawer-as-a-filter-list-results-behind`: The Drawer as a Filter, List Results Behind

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "12 expenses match. 2 filters applied."
  toolbar
    field "Search expenses" search
    action "Filters"
  table 3 "Expense" "Spender" "Paid"
    item: text "EXP-1042" · text "Ava" · text "EUR 125.00"
  drawer right "Filters"
    text "2 applied. 12 expenses match."
    field "Search" text = "fare"
    field "Line" select = "Train fare"
    field "State" select = "Paid"
    field "Date" text = "01/10/2026"
    action quiet "Clear both"
    action primary "Show the results"
```

**Rationale:** A drawer holds the filters rather than a record. The list updates behind it as each control changes, so the reader never leaves the results.

**Cost:** A drawer is not a menu, but it hides several filter categories the same way a menu does. It covers the toolbar the reader would use to search.

**Reference:** Carbon

### Toast at the Edge of the Screen

**ID:** `litespec/frontend/patterns/dialogs-and-overlays#toast-at-the-edge-of-the-screen`

**Strength:** default

**Question:** A command ends with a short message at the edge of the screen. One message carries at most one action. Nothing critical belongs in something that disappears on a timer.

**Default:** `a-confirmation-that-closes-by-itself`

**Compact:** The recommended option reflows: the toast spans the phone width above the home indicator, with one action at most.

**Rationale:** Keep the self-closing confirmation for success, where the message leaves on its own and nothing goes with it. The runner-up is the toast that links to the result. It suits commands whose result lives elsewhere, because the link going away costs nothing.

Never ship two actions on one toast. Two buttons turn a transient message into a small panel with a timer. Never ship a toast as the only route to a result.

**Reviewed for:** Which built edge notice departs from the wireframe its catalog entry chose? Open the built confirmations and linked results beside the catalog entry recording the chosen option and its reason.

#### `a-confirmation-that-closes-by-itself`: A Confirmation That Closes by Itself

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses. 3 receipts resent."
  toolbar
    field "Search expenses" search
    field "State" select = "Any state"
  table 4 "Expense" "Spender" "State" "Paid"
    item: text "EXP-1042" · text "Ava" · status "Paid" positive · text "EUR 125.00"
  toast "Receipts resent to 3 spenders." positive
```

**Rationale:** It carries no action, so it can leave on its own. The default timeout is 5,000 milliseconds, and nothing is lost by it going.

**Cost:** A reader can miss an error that faded after five seconds and wait five minutes. That is fine for a success and wrong for anything else.

**Reference:** Base UI, NN/g

#### `a-toast-that-links-to-the-result`: A Toast That Links to the Result

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses."
    action "Export"
  region "Latest exports"
    list 2
      item: text "expenses-2026-10-08.csv" · text muted "268 rows" · action "Download"
  toast "Your export is ready. expenses-2026-10-08.csv waits under Latest exports." positive
```

**Rationale:** The message links to the result, and the result also waits on the page. The toast going away costs nothing because the file does not go with it.

**Cost:** Two routes to one result need the same name in both places. Where the names drift, the reader stops trusting the link and opens the page instead.

**Reference:** Vercel

#### `a-toast-carrying-two-actions`: A Toast Carrying Two Actions

```wireframe
page
  breadcrumb "Home" "Budgets" "Travel budget"
  header
    heading "Travel budget"
    text "9,412 lines claimed and all paid."
  region "The budget"
    fact "Claimed" "9,412 of 10,000"
    fact "Paid" "9,380"
    fact "Unpaid" "32"
  toast "The budget was closed. Undo and Dismiss are both on this message." caution
```

**Rationale:** The toast offers Undo and Dismiss together, which is the shape most people reach for first.

**Cost:** One action per snackbar, and the action may not be Dismiss. Two buttons turn a transient message into a small panel with a timer.

**Reference:** Material Design

### Menu of a Row's Commands

**ID:** `litespec/frontend/patterns/dialogs-and-overlays#menu-of-a-rows-commands`

**Strength:** default

**Question:** A row with several commands shows the first inline and keeps the rest behind a menu. The decision is how that menu groups its items, explains disabled ones, and handles search and submenus.

**Default:** `a-menu-with-groups-and-a-destructive-item-last`

**Compact:** The recommended option reflows: the menu becomes a bottom sheet with thumb-sized rows, the title naming the row, and one full-width dismissal.

**Rationale:** Keep the menu with groups and the destructive item last after a separator, with nothing disabled without a reason. The runner-up is the menu that states why an item is disabled. It is the better choice where a command is often disabled, because the reason in place beats a hover.

Never ship a confirmation inside a menu. A confirmation there is a dialog over a menu, the same nesting the project forbids for dialogs.

**Reviewed for:** Which built row menu departs from the wireframe its catalog entry chose? Open the built grouped menus beside the catalog entry recording the chosen option and its reason.

#### `a-menu-with-groups-and-a-destructive-item-last`: A Menu with Groups and a Destructive Item Last

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses."
  toolbar
    field "Search expenses" search
  table 4 "Expense" "Spender" "State" "Paid"
    item: text "EXP-1042" · text "Ava" · status "Paid" positive · text "EUR 125.00" · action "Resend"
  menu "Row commands"
    action "Open the record"
    action "Resend the receipt"
    action "Report a problem"
    action disabled "Void the receipt code"
    action quiet "Copy the expense number"
    action destructive "Reverse"
```

```wireframe compact
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses."
  region "Expenses"
    field "Expense number, name or email" search
    table 2 "Expense" "Spender" "Paid"
      item: text "EXP-1042" · text "Ava" · text "EUR 125.00"
  drawer bottom
    heading "Expense EXP-1042"
    text "Ava · Harbor and Pine · EUR 125.00 paid."
    action "Open the record"
    action "Resend the receipt"
    action disabled "Void the receipt code"
    action destructive "Reverse"
    footer
      action "Cancel"
```

**Rationale:** The recommended shape groups the items under small headings and puts the destructive one at the end, after a separator, in the destructive tone. Nothing is disabled without a reason.

**Cost:** A menu item that is disabled gives no reason in the menu itself. The reader has to hover or press it to learn why.

#### `a-menu-that-says-why-an-item-is-disabled`: A Menu That Says Why an Item Is Disabled

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses."
  toolbar
    field "Search expenses" search
  table 4 "Expense" "Spender" "State" "Paid"
    item: text "EXP-1042" · text "Ava" · status "Paid" positive · text "EUR 125.00"
  menu "Row commands"
    action "Open the record"
    action "Resend the receipt"
    action "Reverse"
    action disabled "Void the receipt code"
  message "Void the receipt code is unavailable: both receipts were attached at 22:41." neutral
```

**Rationale:** The disabled item carries its reason in place, so the reader learns it without pressing it. This is the fix for a menu whose commands are conditionally absent.

**Cost:** A reason inside a menu is a paragraph among one-word items, so the menu becomes taller than the row that opened it.

#### `a-menu-with-a-confirmation-inside-it`: A Menu with a Confirmation Inside It

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses."
  toolbar
    field "Search expenses" search
  table 4 "Expense" "Spender" "State" "Paid"
    item: text "EXP-1034" · text "Ade Okafor" · status "Disputed" critical · text "EUR 45.00"
  menu "Row commands"
    action destructive "Withdraw the dispute"
  popover "Withdraw the dispute"
    text "The bank is told the company accepts the payout."
    message "This cannot be taken back." critical
    action "Keep it open"
    action destructive "Withdraw"
```

**Rationale:** A destructive item opens its confirmation from inside the menu, so the menu stays and the question appears beside the row it applies to.

**Cost:** The confirmation opens over the menu, which stacks two floating layers. A dialog that triggers another dialog is refused, and the same reasoning refuses a dialog over a menu.

**Reference:** Atlassian

### Short Panel That Opens Beside a Control

**ID:** `litespec/frontend/patterns/dialogs-and-overlays#short-panel-that-opens-beside-a-control`

**Strength:** default

**Question:** A popover is a small panel anchored to a control: not a dialog and not a toast. It closes when the reader looks away. The decision is what earns such a panel: term explanations, row previews, pickers, calendars, copy confirmations, hover cards, or coach marks.

**Default:** `a-popover-that-explains-what-a-term-means`

**Compact:** The recommended option reflows: the panel becomes a bottom sheet with the same content and one dismissal.

**Rationale:** Keep the popover that explains what a term means. A defined term carries its glossary entry where it is read, and only for terms the application has defined. The runner-up is the row preview popover. It suits dense lists where a hover card saves a navigation, and it must open on focus as well as hover.

Never ship a menu and a popover for the same thing. The pair teaches that floating layers stack, and stacked layers confuse readers about which one to dismiss.

**Reviewed for:** Which built hover card or hint departs from the wireframe its catalog entry chose? Open the built popovers and previews beside the catalog entry recording the chosen option and its reason.

#### `a-popover-that-explains-what-a-term-means`: A Popover That Explains What a Term Means

```wireframe
page
  breadcrumb "Home" "Billing" "Costs"
  header
    heading "What the plan costs"
    text "Every charge on the account, and what it is for."
  region "September charges"
    fact "Business plan" "EUR 49.00 a month"
    fact "Expense fee" "EUR 270.90"
    fact "Per-line fee" "EUR 236.00"
    fact "Total for September" "EUR 555.90"
  popover "Expense fee"
    text "0.5 percent of every expense total. It pays for payout handling."
```

**Rationale:** A word somebody may not know carries its glossary entry beside it. It answers the question where it is asked.

**Cost:** This is only for a term the application has defined. A hint on every unfamiliar word becomes noise, and the glossary has to stay in step with the hint.

#### `a-row-preview-popover-on-hover-and-focus`: A Row Preview Popover on Hover and Focus

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses."
  toolbar
    field "Search expenses" search
  table 3 "Expense" "Spender" "Receipt"
    item: text "EXP-1042" · text "Ava" · text "RC-0C893968A2"
  popover "RC-0C893968A2"
    text "Train fare, 2 fares in expense EXP-1042."
    fact "For" "Ava"
    fact "Packed" "8 Oct 2026, 09:34"
    fact "Attached" "Not attached"
```

**Rationale:** Hovering a receipt code shows the receipt's facts. It gives context without leaving the list and costs one read.

**Cost:** A hover affordance is invisible on touch and hard to discover on desktop. It needs focus as well as hover, which means two ways to render every row.

#### `a-menu-and-a-popover-used-for-the-same-thing`: A Menu and a Popover Used for the Same Thing

```wireframe
page
  breadcrumb "Home" "Expenses" "Reversals"
  header
    heading "Reversals"
    text "31 expenses. 12 outside the recall window."
  toolbar
    field "Search reversals" search
  table 3 "Expense" "Spender" "Asked"
    item: text "EXP-1037" · text "Elin Lindqvist" · text muted "214 days ago"
  menu "Row commands"
    action "Apply deduction"
    action "Record a bank transfer"
    action "Ask the spender"
  popover "Recall window"
    text "This payout route reverses only within 180 days."
```

**Rationale:** The row menu holds commands and a popover holds context, side by side. A reviewer can see the two floating shapes next to each other.

**Cost:** A page with both holds three floating layers counting the dialog, and a reader must learn which is which. This is why a page should show one overlay at a time.

### Bar That Stays While the Reader Works

**ID:** `litespec/frontend/patterns/dialogs-and-overlays#bar-that-stays-while-the-reader-works`

**Strength:** default

**Question:** A persistent bar is a band the page keeps, worded as a fact about the page, the deployment, or the record. A tone may not paint a full-width band on a page about other work. The decision is which facts earn a bar and where each bar sits.

**Default:** `a-neutral-band-for-what-this-page-is`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Ship the neutral band stating a fact about the deployment, the same on every page. The bar on the record page is the runner-up for facts true of one record only, drawn as a notice on that record.

Never ship the bar naming a deadline: a full-width caution band paints the whole page, and the tone covers work the page is not about.

**Reviewed for:** Which built persistent band departs from the wireframe its catalog entry chose? Open the built deployment and deadline bands beside the catalog entry recording the chosen option and its reason.

#### `a-neutral-band-for-what-this-page-is`: A Neutral Band for What This Page Is

```wireframe
page
  message info "This is a sandbox. Nothing here is paid and nothing reaches a bank."
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense, newest first."
  region "Expenses"
    field "Expense number, name or email" search
    table 3 "Expense" "Spender" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · status "Paid" positive · text "EUR 125.00"
```

**Rationale:** A band across the back office that holds on every page, such as which deployment this is. It stays neutral, never a tone, because it is about the page rather than about work.

**Cost:** A band on every page is chrome the reader cannot remove, and it competes with the heading for the top of the screen. It has to earn its height.

#### `a-bar-on-the-record-page-not-the-console`: A Bar on the Record Page, Not the Console

```wireframe
page
  breadcrumb "Home" "Policies" "Train fare"
  message caution "This policy is not active. Its ceilings open Tue 13 Jan 2026 at 10:00."
  header
    heading "Train fare"
    status "Draft" neutral
    text "TRAVEL-01 · 412 claimed."
    action primary "Publish the policy"
  region "What is active"
    fact "Claims" "412 claimed, none paid"
    fact "Ceiling" "EUR 45.00"
```

**Rationale:** The band is scoped to one record rather than the whole console. It appears only where the fact is true and disappears everywhere else.

**Cost:** The reader learns the band on one page and then has to learn that another page does not have one. A scoped band is not a deployment marker. It is a per-record notice.

#### `a-bar-naming-a-deadline-that-is-closing`: A Bar Naming a Deadline That Is Closing

```wireframe
page
  message caution "Answering closes on 21 October. 5 disputes recall on 21 Oct if nothing is sent."
  breadcrumb "Home" "Payouts" "Disputes"
  header
    heading "Disputes"
    text "5 open. The nearest closes in 19 days."
  region "Cases"
    table 4 "Case" "Expense" "Amount" "Closes"
      item: text "D-0182" · text "EXP-1034" · text "EUR 45.00" · status "19 days" caution
```

**Rationale:** A band naming the date the bank recalls, so the disputes work under it has a clock the band states.

**Cost:** A full-width caution band on a page about disputes is exactly the case the tone rule forbids. The tone paints the whole page rather than the thing in that state.

### Dialog with a Long Body and a Sticky Footer

**ID:** `litespec/frontend/patterns/dialogs-and-overlays#dialog-with-a-long-body-and-a-sticky-footer`

**Strength:** default

**Question:** Some commands need more words than a dialog holds: terms, line items, per-audience consequences. The footer must stay where the thumb is while the body scrolls, or the reader agrees to what they cannot see.

**Default:** `a-scrolling-body-with-a-pinned-footer`

**Compact:** The recommended option reflows: the dialog becomes a full-screen sheet with the body scrolling under a footer pinned above the home indicator.

**Rationale:** Ship the scrolling body with a pinned footer: one scrolling body, one pinned footer, nothing else. The summary pinned above the footer is the runner-up wherever money moves, because the reader sees the totals above the footer before confirming.

Never ship the dialog with no pinned footer: buttons that scroll away ask the reader to agree first and find how after.

**Reviewed for:** Which built scrolling dialog departs from the wireframe its catalog entry chose? Open the built pinned footers beside the catalog entry recording the chosen option and its reason.

#### `a-scrolling-body-with-a-pinned-footer`: A Scrolling Body with a Pinned Footer

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    text "Ava · Harbor and Pine · EUR 125.00 paid."
  region "What was bought"
    fact "Train fare, 2 fares" "EUR 90.00"
    fact "Paid to the spender" "EUR 125.00"
  dialog
    heading "Reverse EXP-1042 in full?"
    text "The two fares go back to drafts and claim again. The personal item sits outside the policy and is not reversed."
    text "The reversal returns to the Harbor and Pine account. It arrives within five working days."
    text "This payout is 31 days old, inside the 180-day window. Past it the fallbacks are a deduction or a repayment the back office only records."
    footer
      action "Cancel"
      action destructive "Reverse EUR 125.00"
```

```wireframe compact
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    text "Ava · Harbor and Pine · EUR 125.00 paid."
  region "What was bought"
    fact "Train fare, 2 fares" "EUR 90.00"
    fact "Paid to the spender" "EUR 125.00"
  drawer bottom
    heading "Reverse EXP-1042 in full?"
    text "The two fares go back to drafts and claim again. The personal item sits outside the policy and is not reversed."
    text "The reversal returns to the Harbor and Pine account. It arrives within five working days."
    text "This payout is 31 days old, inside the 180-day window. Past it the fallbacks are a deduction or a repayment the back office only records."
    footer
      action "Cancel"
      action destructive "Reverse EUR 125.00"
```

**Rationale:** The body scrolls under a footer that never moves. The commit stays in reach and the terms stay above it.

**Cost:** A scrolling region inside a dialog is two scrolls on one screen. Keyboard readers must land in the body rather than behind it, or the terms are decoration.

**Reference:** Stripe

#### `a-summary-pinned-above-the-footer`: A Summary Pinned Above the Footer

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    text "Ava · Harbor and Pine · EUR 125.00 paid."
  region "What was bought"
    fact "Train fare, 2 fares" "EUR 90.00"
    fact "Paid to the spender" "EUR 125.00"
  dialog
    heading "Reverse EXP-1042 in full?"
    text "The two fares go back to drafts and claim again. The personal item sits outside the policy and is not reversed."
    text "The reversal returns to the Harbor and Pine account and arrives within five working days."
    fact "From Ava" "EUR 125.00"
    footer
      action "Cancel"
      action destructive "Reverse EUR 125.00"
```

**Rationale:** The total sits above the commit action: the number the click commits to stays visible while the terms scroll. The reader signs the number, not the scroll position.

**Cost:** The pinned summary duplicates the body. Where the two disagree the reader trusts the pin, and the body becomes decoration nobody checks.

**Reference:** Stripe

#### `no-pinned-footer-the-buttons-scroll-away`: No Pinned Footer: The Buttons Scroll Away

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    text "Ava · Harbor and Pine · EUR 125.00 paid."
  region "What was bought"
    fact "Train fare, 2 fares" "EUR 90.00"
    fact "Paid to the spender" "EUR 125.00"
  dialog
    heading "Reverse EXP-1042 in full?"
    text "The two fares go back to drafts and claim again. The personal item sits outside the policy and is not reversed."
    text "The reversal returns to the Harbor and Pine account. It arrives within five working days."
    text "This payout is 31 days old, inside the 180-day window, so the bank accepts it."
    message caution "The footer with Cancel and Reversal sits below this body. Scroll to reach it."
    action "Cancel"
    action destructive "Reverse EUR 125.00"
```

**Rationale:** Included to refuse it: without a scroll cap the dialog grows past the viewport and the footer sits below the fold. The reader agrees first and finds how after.

**Cost:** On a phone the footer is two screens down, past terms nobody scrolled to. This is the shape every other option in this pattern exists to replace.

### Keyboard Shortcut Help Overlay

**ID:** `litespec/frontend/patterns/dialogs-and-overlays#keyboard-shortcut-help-overlay`

**Strength:** default

**Question:** Staff who live in lists learn keys faster than clicks. The application answers `?` with an overlay, grouped by where the keys work, closable with the same key.

**Default:** `a-grouped-grid-of-shortcuts`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Ship the grouped grid of shortcuts: three groups, one grid, every key the application honors, closed by the same key. The this-page-only list is the runner-up on dense pages, where it answers the only keys that work there.

Never ship tooltips alone: they hide the catalog from the reader who would learn it fastest.

**Reviewed for:** Which built shortcut list departs from the wireframe its catalog entry chose? Open the built grouped grids beside the catalog entry recording the chosen option and its reason.

#### `a-grouped-grid-of-shortcuts`: A Grouped Grid of Shortcuts

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses."
  region "Expenses"
    field "Expense number, name or email" search
    table 2 "Expense" "Spender" "Paid"
      item: text "EXP-1042" · text "Ava" · text "EUR 125.00"
  dialog
    heading "Keyboard shortcuts"
    text "Press ? again to close."
    region "Everywhere"
      list 3
        item: text "Open this help" · text "?"
    region "Expenses list"
      list 3
        item: text "Search the list" · text "/"
    region "Record"
      list 2
        item: text "Message the spender" · text "M"
    footer
      action "Close"
```

**Rationale:** Groups by where the keys work, each key in its own cap, and the same key closes. Staff learn the catalog in one reading.

**Cost:** A catalog dialog lists keys the reader has no reason to press yet. Past twenty keys it is reference rather than learning, and reference belongs on a page.

**Reference:** Linear

#### `shortcuts-for-this-page-only`: Shortcuts for This Page Only

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses."
  region "Expenses"
    field "Expense number, name or email" search
    table 2 "Expense" "Spender" "Paid"
      item: text "EXP-1042" · text "Ava" · text "EUR 125.00"
  dialog
    heading "Shortcuts on Expenses"
    text "Keys that work nowhere else are not listed here."
    list 3
      item: text "Search the list" · text "/"
    link "All shortcuts"
    footer
      action "Close"
```

**Rationale:** The panel scopes to the tool at hand, showing only the keys that work here. The list stays short enough to learn. The global keys live one link away.

**Cost:** Page-scoped lists repeat the global keys on every page or omit them. Either way the reader meets two lists where one catalog would do.

**Reference:** Figma

#### `no-overlay-hints-only-on-tooltips`: No Overlay: Hints Only on Tooltips

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses."
    action "Export"
  region "Expenses"
    field "Expense number, name or email" search
    table 2 "Expense" "Spender" "Paid"
      item: text "EXP-1042" · text "Ava" · text "EUR 125.00"
  popover
    text "Export these expenses"
    text "E"
```

**Rationale:** Included to refuse it: the catalog hides one hover at a time, and touch readers never meet it at all. A reader who would learn ten keys learns none.

**Cost:** Each hint is accurate and each is undiscoverable. The tooltip teaches the key for a control already found, which is the one key that reader needed least.

### Context Menu Where the Pointer Is

**ID:** `litespec/frontend/patterns/dialogs-and-overlays#context-menu-where-the-pointer-is`

**Strength:** default

**Question:** A right click asks what this thing can do. The application answers at the pointer with the same commands the row menu holds. The menu is a shortcut rather than a second set.

**Default:** `a-right-click-menu-on-a-row`

**Compact:** Touch has no right click: a long press opens the same menu at the row, with the same commands in the same order.

**Rationale:** Ship the right-click menu on the row: the row's own commands at the pointer, nothing added, nothing missing, closed by clicking elsewhere. The bulk-selection menu is the runner-up for bulk work, where the menu rewrites itself for the selection count.

Never ship silence: it hands the gesture to the browser, whose commands act on the page rather than the record.

**Reviewed for:** Which built right-click menu departs from the wireframe its catalog entry chose? Open the built row and bulk menus beside the catalog entry recording the chosen option and its reason.

#### `a-right-click-menu-on-a-row`: A Right-Click Menu on a Row

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses."
  region "Expenses"
    field "Expense number, name or email" search
    table 3 "Expense" "Spender" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · status "Paid" positive · text "EUR 125.00"
  popover
    menu "Expense EXP-1042"
      action "Open the record"
      action "Resend the receipt"
      action "Reverse"
      action quiet "Copy the expense number"
```

**Rationale:** The row's own commands at the pointer, nothing added, nothing missing. The menu is a shortcut to the row menu rather than a second set.

**Cost:** Right click is invisible. Staff who never try it keep the row menu, and both menus must hold the same commands or one of them lies.

**Reference:** Linear

#### `a-context-menu-for-a-bulk-selection`: A Context Menu for a Bulk Selection

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses. 3 selected."
  region "Expenses"
    field "Expense number, name or email" search
    table 3 "Expense" "Spender" "Paid"
      item: text "EXP-1042" · text "Ava" · text "EUR 125.00"
  popover
    menu "3 selected expenses"
      action "Resend to 3 spenders"
      action "Export 3 expenses"
      action quiet "Copy 3 expense numbers (Ctrl C)"
```

**Rationale:** The menu rewrites for a multi-selection: the count in every label, and the single-record commands are gone. The reader cannot ask three rows to open one record.

**Cost:** The menu rewrites per count, so muscle memory for one row breaks at three. The count must be exact at the click, not at the render.

**Reference:** Figma

#### `no-menu-the-browser-default-answers`: No Menu: The Browser Default Answers

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses."
  region "Expenses"
    field "Expense number, name or email" search
    table 3 "Expense" "Spender" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · status "Paid" positive · text "EUR 125.00"
  popover
    text "The browser answers, because the back office does not."
    menu "Browser"
      action "Back"
      action "Reload"
      action "Save as"
      action quiet "Inspect element"
```

**Rationale:** Included to refuse it: silence hands the gesture to the browser, whose commands act on the page rather than the record. Staff learn the gesture does nothing for them and stop trying.

**Cost:** It puts Inspect element beside Reload on a row about money. The absence is not neutral. It teaches that right click is not part of the application.
