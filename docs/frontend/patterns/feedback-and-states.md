# Feedback and State Patterns

**Stack:** any
**Concern:** frontend-ui

This page defines the patterns for the states a screen shows around its work. Each pattern answers one recurring question about those states, from what an empty list shows to how a failed command reports itself, and every decided state says what happened, what it means, and what the reader can do next.

Each pattern names a default option: the shape the project builds unless the owner records another choice in the project's pattern catalog, the file that holds the project's interface decisions.

## Concepts

### How to Read a Pattern

A pattern answers one recurring question about screen states, such as what a list shows before its first records exist. It states that question, then names its default option: the shape the project builds unless the owner records another choice. It then states how the default behaves on a narrow screen, the reason it is the default, which alternative fits which case, and which shape to leave out.

Each option is one `####` heading with a stable option ID, one wireframe, and short parts after it: its rationale, its cost, and sometimes a reference to the outside product it learns from.

A wireframe states the structure of the option and the copy it shows, and nothing about color, font, or components: the team builds the chosen option in its own stack and design system. For example, the loading pattern's default draws skeleton rows at the real row height, and an owner who needs a re-read indicator records the thin-bar option instead. The [UI patterns page](../patterns.md) states the catalog contract and the wireframe language.

### The Running Example

The reader is the staff member using the screen. The wireframes use the Claimwick payout desk: 268 expenses including EXP-1042 for Ava, reversals such as the EUR 125.00 on that expense, and the trip, payout, and budget facts around them.

Every wireframe follows the six rules in Standards below: a refusal or error message states what failed and the next step; a failed save keeps the reader's input on screen; each status is announced by its urgency; a loading region marks itself busy; focus moves to a refusal; and unsent input survives a dropped connection.

## Standards

### State What Failed and the Next Step

**ID:** `litespec/frontend/patterns/feedback-and-states#state-what-failed-and-the-next-step`

**Strength:** required

**Statement:** A refusal or error message MUST state what failed and the next step the reader can take.

**Rationale:** A message that only says something went wrong leaves the reader guessing whether to retry, fix input, or ask for help; most readers retry blindly or give up. Naming what failed and the one next step gives the reader a task to complete instead of a message to guess at.

**Reviewed for:** Does every built failure message name the cause and the way forward? Open the refused commands and failed reads on built screens.

### Keep the Reader's Input Through a Failure

**ID:** `litespec/frontend/patterns/feedback-and-states#keep-the-readers-input-through-a-failure`

**Strength:** required

**Statement:** A failed save MUST keep the reader's unsaved input on the screen.

**Rationale:** Retyping a whole form to fix one refused field wastes the reader's time and introduces new typos. Worse, readers who lose work to a failure stop trusting the form: they copy long answers elsewhere before saving.

**Reviewed for:** Does every built form keep its typed values after a refused save? Open the failed submits on built forms and check every field.

### Announce Each Status by Its Urgency

**ID:** `litespec/frontend/patterns/feedback-and-states#announce-each-status-by-its-urgency`

**Strength:** required

**Statement:** A page MUST announce each status through one of two live regions present when the page loads: a polite region for routine news and one assertive region for a session about to end, a lost connection holding unsent input, or a failed money command.

**Rationale:** A live region added at the moment of the message is often not read, so both regions exist from the page load. Assertive messages interrupt whatever the reader hears, so only blocking news uses the assertive region: a session about to end, a lost connection holding unsent input, or a failed money command. This is WCAG 4.1.3 Status Messages, which asks that status changes be announced without moving focus.

**Reviewed for:** Does every built page announce routine news politely and add its live regions at load? Trigger each status with a screen reader and note which region speaks.

### Mark a Loading Region Busy

**ID:** `litespec/frontend/patterns/feedback-and-states#mark-a-loading-region-busy`

**Strength:** required

**Statement:** A loading region MUST expose its state through markup, as busy on the region or as role and value on a progress bar, and not through a live region announcement.

**Rationale:** A `Loading` announcement on every read hides the messages that matter. A region marked busy with `aria-busy="true"` stays silent while assistive technology reports its state on request, and a progress bar with its role and value says how far the read has come. Announcements stay for the news that matters.

**Reviewed for:** Does every built loading region stay silent in live regions and expose its busy mark or progress role? Trigger each load with a screen reader and inspect the loading markup.

### Move Focus to a Refusal

**ID:** `litespec/frontend/patterns/feedback-and-states#move-focus-to-a-refusal`

**Strength:** required

**Statement:** After the API refuses a command, focus MUST move to the refusal or to the first field it names.

**Rationale:** A sighted reader sees the refusal where it appears. A keyboard or screen-reader user otherwise never meets it. Moving focus to the refusal, or to the first field it names, puts every reader at the same place. This is WCAG 3.3.1 Error Identification, which asks that each error be described in text.

**Reviewed for:** Does every built refusal move focus to itself or to the first field it names? Refuse each command with a keyboard and note where focus lands.

### Keep Unsent Input When the Connection Drops

**ID:** `litespec/frontend/patterns/feedback-and-states#keep-unsent-input-when-the-connection-drops`

**Strength:** required

**Statement:** A route that loses its connection MUST keep the user's unsent input and show the error state with a retry action.

**Rationale:** A long expense form lost to a train tunnel is typed again or abandoned. Keeping the unsent input on screen with a retry action lets the reader retry the read instead of retyping the form.

**Reviewed for:** Does every built route keep typed input when the connection drops and offer a retry? Drop the connection on each route holding unsent input and try the retry.

## Patterns

### Nothing Has Ever Existed

**ID:** `litespec/frontend/patterns/feedback-and-states#nothing-has-ever-existed`

**Strength:** default

**Question:** No record of this kind exists yet. The page says so plainly, says what the first record is for, and offers the one command that creates it.

**Default:** `positive-title-one-action`

**Compact:** Below the md breakpoint the default option reflows: the title, the message, and the single action stack in one column.

**Rationale:** The default is the positive title with one action. It is the smallest shape, and the only option that asks nothing of a reader who has nothing yet. The runner-up is the setup guide on Home; prefer it when the first expense needs steps across several pages. Sample rows are the other alternative for a page whose shape is learned by using it. A table with a dashed sample row stays out: it shows a record that does not exist and leads nowhere when opened.

**Reviewed for:** Does every built first-run screen match the wireframe its catalog entry chose? Open the built empty lists and setup guides beside the catalog entry recording the chosen option and its reason.

#### `positive-title-one-action`: Positive Title, One Action

```wireframe
page
  breadcrumb "Home" "Policies"
  header
    heading "Policies"
    text "Six kinds of claims, from a 12-line per diem to the train fare."
    action primary "New policy"
  region "Policies"
    message "No policies yet. A policy holds a name, a code, a ceiling and what is claimed." neutral
    action primary "New policy"
```

**Rationale:** The page shows one positive title, one line saying what a policy is for, and one button that creates the first one. A reader with nothing yet is asked for nothing.

**Cost:** The reader sees no row, so every column stays undiscoverable until there is data.

**Reference:** Carbon

#### `a-setup-guide-on-home-plain-empty-states-elsewhere`: A Setup Guide on Home, Plain Empty States Elsewhere

```wireframe
page
  breadcrumb "Home"
  header
    heading "Good morning, Ben"
    text "Harbor and Pine. Nothing published yet."
  region "Get ready to sell"
    text muted "2 of 5 done."
    list 5
      item: text "Add a policy" · text muted "A name, a code and its ceiling" · action primary "New policy"
```

**Rationale:** Setup spans pages, so the steps to a first expense live once, on Home, with progress shown, rather than on seven pages. Each list page keeps a plain one-line empty state.

**Cost:** The guide has to know what is done, which is a read across several modules. It also needs a way to be dismissed, or it stays on Home after the company has paid 10,000 expenses.

**Reference:** Shopify, Stripe

#### `a-sample-the-reader-can-open-keep-or-remove`: A Sample the Reader Can Open, Keep, or Remove

```wireframe
page
  breadcrumb "Home" "Policies"
  header
    heading "Policies"
    text "One sample below, so the page reads as it will."
    action primary "New policy"
  message "Travel policy is a sample. Open it, change it, or remove it before publishing it." info
  toolbar
    field "Search policies" search
  region "Policies"
    text muted "Samples are never paid and never appear in exports."
    list 2
      item: text "Travel policy" · text muted "Code TRAVEL-01 · EUR 45.00" · status "Sample" info · action "Open"
  footer
    action destructive "Remove the samples"
```

**Rationale:** The page shows real sample rows instead of an empty illustration, fenced with a `Sample` state and a notice. The reader learns the page by using it rather than by reading about it.

**Cost:** Samples stay out of every export, total, and spender surface. A sample left in place at go-live ships fake data as the company's own.

### Nothing Matches What Was Asked For

**ID:** `litespec/frontend/patterns/feedback-and-states#nothing-matches-what-was-asked-for`

**Strength:** default

**Question:** A search found nothing, but records exist. The page says so, names the term that failed, and offers a way back.

**Default:** `different-copy-and-it-names-the-term`

**Compact:** Below the md breakpoint the default option reflows: the toolbar stacks above the message, and a table keeps its first two columns.

**Rationale:** The default is the copy that names the term. It says what failed and what to change, with the count proving the data exists. The runner-up is the table that keeps its headers; prefer it where readers filter all day and want the columns to stay put. The filters panel is the other alternative for a page whose filters are the whole question. An empty state with no action at all stays out: a search that names its term can always offer to clear it.

**Reviewed for:** Does every built no-results screen match the wireframe its catalog entry chose? Open the built filtered empties and filter panels beside the catalog entry recording the chosen option and its reason.

#### `different-copy-and-it-names-the-term`: Different Copy, and It Names the Term

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "No expenses match this search."
  toolbar
    field "Search expenses" search = "ava prins"
    field "Spender" select = "Harbor and Pine"
    field "State" select = "Paid"
  region "Expenses"
    message "Nothing matches ava prins. 268 expenses exist. None match this term with Harbor and Pine and Paid applied." neutral
    action "Clear the search"
    action quiet "Clear the filters too"
```

**Rationale:** The title quotes the term that found nothing, and the line says how many records exist, so the reader knows the search failed rather than the data. Two actions separate clearing the search from clearing the filters.

**Cost:** The guidance is one primary action, and this pattern uses two. The secondary one is a quiet action, which is the compromise.

**Reference:** Carbon

#### `the-table-header-stays-with-the-message-as-its-only-row`: The Table Header Stays, with the Message as Its Only Row

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses, newest first."
    action "Export"
  toolbar
    field "Search expenses" search
    field "State" select = "Reversed"
    field "Channel" select = "Phone"
    action quiet "Clear filters"
  region "Expenses"
    table 5 "Expense" "Spender" "Filed" "State" "Paid"
      item: text "No reversed call expenses" · text muted "31 expenses were reversed, all of them filed by phone"
```

**Rationale:** The expense list keeps its column headers when a filter finds nothing. The filters name columns, and the reader is still reading the same table, with the message in one full-width row.

**Cost:** A first-use empty state replaces the table, headers included; this differs on purpose. It suits a filter result and not first use, so the two states need two shapes.

**Reference:** Stripe

#### `a-filters-panel-instead-of-an-empty-state`: A Filters Panel Instead of an Empty State

```wireframe
page
  breadcrumb "Home" "Expenses" "Reversals"
  header
    heading "Reversals"
    text "Nothing matches."
  split 1:3
    aside "Filter"
      field "Search" search = "cosmo"
      field "Window" select = "Open now"
      field "Channel" select = "Phone"
      message "0 of 31 reversals match. The window and the channel are the narrowest." critical
      action quiet "Clear"
    region "Reversals"
      message "Nothing matches. 31 reversals exist. Phone filing has none, and none are open." neutral
      action "Clear the channel"
```

**Rationale:** When a filter has narrowed the list to nothing, the panel itself becomes the message. The reader sees which of their own choices did it.

**Cost:** The list is gone, so the reader learns the unfiltered count only if the panel states it.

### Loading

**ID:** `litespec/frontend/patterns/feedback-and-states#loading`

**Strength:** default

**Question:** The page waits for its first read. The wait needs a shape that holds the rows' place until they arrive.

**Default:** `skeletons-at-the-real-row-height`

**Compact:** Below the md breakpoint the default option reflows: the skeleton rows keep the row height, and the toolbar stacks above them.

**Rationale:** The default is skeletons at the real row height. Nothing moves when the data lands, even on a slow phone read. The runner-up is the thin bar over the old rows; prefer it for a re-read where the reader keeps their place, such as a changed filter. A scrim is the other alternative for a wait the reader must not work through. Showing nothing at all stays out: any read slower than a blink reads as a failed page.

**Reviewed for:** Does every built waiting state match the wireframe its catalog entry chose? Open the built skeletons, thin bars, and scrims beside the catalog entry recording the chosen option and its reason.

#### `skeletons-at-the-real-row-height`: Skeletons at the Real Row Height

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every expense, newest first."
  toolbar
    field "Search expenses" search
  region "Expenses"
    image "Seven skeleton rows shaped like expense rows"
  footer
    text "Reading 268 expenses"
```

**Rationale:** Each skeleton row is shaped like the row it will become. Nothing moves when the data lands.

**Cost:** The header, toolbar, and pagination match the row size. A skeleton page gets four heights right, or the page jumps.

**Reference:** Carbon

#### `the-last-rows-stay-a-thin-bar-says-a-new-read-is-running`: The Last Rows Stay, a Thin Bar Says a New Read Is Running

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses, newest first."
    action "Export"
  toolbar
    field "Search expenses" search
    field "State" select = "Paid"
    text muted "Reading"
  region "Expenses"
    message "Reading the latest expenses. The rows below are the last answer." neutral
    table 5 "Expense" "Spender" "Filed" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 09:32" · status "Paid" positive · text "EUR 125.00"
  footer
    text "1 to 5 of 268"
    action quiet "‹ Previous"
    action quiet "Next ›"
```

**Rationale:** The page keeps what was on screen and shows a thin bar while the next read runs. A reader changing a filter keeps their place, and nothing jumps when the rows arrive.

**Cost:** The rows on screen are briefly the old answer. A reader who acts on a row during that second acts on the old answer, so commands on a list that is reading wait, or say so.

**Reference:** Linear, Vercel

#### `a-dimmed-page-with-the-spinner-centered`: A Dimmed Page with the Spinner Centered

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every expense, newest first."
  toolbar
    field "Search expenses" search
  region "Expenses"
    table 3 "Expense" "Spender" "Paid"
      item: text "EXP-1042" · text "Ava" · text "EUR 125.00"
  dialog "Reading 1,204 expenses"
    text "This takes a moment on a deployment this size."
```

**Rationale:** A scrim covers the whole page with a spinner and a sentence. The message is unambiguous, and the page underneath is deliberately not readable.

**Cost:** A scrim over the page is neither a shared component nor a page slot. It hides a page the reader may want to scroll.

### Command the API Refused

**ID:** `litespec/frontend/patterns/feedback-and-states#command-the-api-refused`

**Strength:** default

**Question:** The API refused a command. The page says what went wrong, why, and what the reader can do instead.

**Default:** `the-refusal-sentence-its-code-and-a-way-to-fix-it`

**Compact:** Below the md breakpoint the default option reflows: the refusal and its alternative stack under the title.

**Rationale:** The default is the refusal sentence with its code and its fix. It meets WCAG 3.3.1 and 3.3.3 in one place. The runner-up is the refusal inside the dialog that sent it; prefer it where closing would lose what the reader typed, such as a reversal amount. The disabled button with its reason is the other alternative for a refusal the page can predict. A refusal only on the record timeline stays out: a reader who sent the command from elsewhere never returns to find it.

**Reviewed for:** Does every built refusal notice match the wireframe its catalog entry chose? Open the built refused commands, with codes and fixes, beside the catalog entry recording the chosen option and its reason.

#### `the-refusal-sentence-its-code-and-a-way-to-fix-it`: The Refusal Sentence, Its Code and a Way to Fix It

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · Harbor and Pine."
    action destructive "Reverse"
  message "The recall window has closed. This payout route reverses only within 180 days of the payout. This one is 214 days old." critical
  link "Why"
  region "What can be done instead"
    text muted "The recall window is the bank's, not the company's."
    list 2
      item: text "Offer the spender a deduction" · text muted "A deduction from a future payout" · action primary "Apply deduction"
```

**Rationale:** The page uses one refusal shape: a title saying what is wrong, the detail saying why, and a `Why` link where the application can explain the refusal. The refusal sentence comes from the message dictionary, not the component.

**Cost:** Each refusal code needs three pieces in two languages, and every new command that can be refused adds a code. The dictionary is the second copy of the domain rules.

#### `the-refusal-inside-the-dialog-that-sent-it`: The Refusal Inside the Dialog That Sent It

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · Harbor and Pine."
    action destructive "Reverse"
  region "The money"
    fact "Paid to the spender" "EUR 125.00"
    fact "Reversed so far" "EUR 0.00"
  dialog "Reverse EXP-1042"
    message "The recall window has closed. This payout route reverses only within 180 days." critical
    field "Amount" = "100.00"
    action "Close"
    action primary "Deduct instead"
```

**Rationale:** The reversal dialog keeps the refusal inside the dialog, above the amount, and stays open so the reader can change what was refused. Focus moves to the message, and the alternative is one button away.

**Cost:** The dialog grows by the height of the message, and a long refusal pushes the buttons down. On a phone the dialog may need to scroll.

**Reference:** Stripe

#### `refused-before-it-is-sent-the-button-explains`: Refused Before It Is Sent: The Button Explains

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · Harbor and Pine. Paid 214 days ago."
  region "Reversals"
    text muted "Paid EUR 125.00 on 8 Mar 2026. Nothing reversed."
    action disabled "Reverse to the original account"
    message "This payout route takes a reversal only within 180 days of the payout. This one is 214 days old." neutral
    list 2
      item: text "Apply deduction" · text muted "Harbor and Pine pays it from its own account" · action primary "Apply deduction"
```

**Rationale:** Where the page already knows the recall window has closed, it says so beside the button, and the refusal never needs to happen.

**Cost:** This option works only for refusals the page can predict from what it already read. The API still refuses, so the dictionary sentence and the button sentence must say the same thing.

**Reference:** GitHub

### Command That Worked

**ID:** `litespec/frontend/patterns/feedback-and-states#command-that-worked`

**Strength:** default

**Question:** A command worked. The page shows what changed, so the reader trusts the result without asking.

**Default:** `evidence-on-the-page-not-only-a-toast`

**Compact:** Below the md breakpoint the default option reflows: the money facts stack under the header, and the timeline follows them.

**Rationale:** The default is evidence on the page. State, money, and timeline all say what changed after a reversal. The runner-up is the toast with `Undo`; prefer it for a reversible change such as hiding a policy from the portal. The quiet saved line is the other alternative for a settings form where the values are already the evidence. No message at all stays out: a screen reader user gets no signal, which fails WCAG 4.1.3.

**Reviewed for:** Does every built success notice match the wireframe its catalog entry chose? Open the built confirmations, toasts with undo, and quiet saves beside the catalog entry recording the chosen option and its reason.

#### `evidence-on-the-page-not-only-a-toast`: Evidence on the Page, Not Only a Toast

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Reversed" neutral
    text "Fully reversed 8 Oct 2026, 11:04, by Cara."
    action primary "Message spender"
  region "The money"
    fact "Paid to the spender" "EUR 125.00"
    fact "Reversed in full" "EUR 125.00"
    fact "Kept by Harbor and Pine" "EUR 0.00"
  region "What happened"
    timeline 4
      item: text "Reversed, EUR 125.00" · text muted "8 Oct 2026, 11:04" · text muted "Cara"
```

**Rationale:** The record changes and the page says what changed: the state becomes `Reversed`, the money section shows the reversal, and the timeline gains an entry. That is evidence, which a toast never is.

**Cost:** The reader has to find the evidence on the page. A toast is faster and leaves nothing once it goes, so a toast alone is not evidence.

#### `a-toast-with-undo-for-a-change-that-can-be-taken-back`: A Toast with Undo, for a Change That Can Be Taken Back

```wireframe
page
  breadcrumb "Home" "Policies"
  header
    heading "Policies"
    text "Travel policy and the per diem."
    action primary "New policy"
  region "Policies"
    list 2
      item: text "Per diem, standard" · text muted "EUR 30.00 · 120 claimed" · status "Hidden from the portal" neutral
  toast "Per diem, standard is hidden from the portal." positive
```

**Rationale:** A reversible change is confirmed with a toast that carries `Undo`. The list already shows the change. The toast adds the one thing the page cannot, a way back for a few seconds.

**Cost:** Never for money, access, or budgets, which cannot be taken back quietly. `Undo` also needs the command to be reversible on the server, not hidden on the client.

**Reference:** Gmail, Linear

#### `saved-said-quietly-beside-the-button`: Saved, Said Quietly Beside the Button

```wireframe
page
  breadcrumb "Home" "Settings" "Company"
  header
    heading "Company"
    text "What spenders and statements show."
  region "Company"
    field "Name" = "Harbor and Pine"
    field "Email for spenders" = "hello@harbor-pine.example"
    field "Phone" = "+31 20 555 0100"
  footer
    text "Saved at 14:22"
    action primary disabled "Save"
```

**Rationale:** The settings form says `Saved` beside the control, with the time, and nothing else moves. The values on screen are already the evidence.

**Cost:** The message is easy to miss, and a screen reader hears it only if it is announced as a live status. It suits settings, not a command that changes money.

**Reference:** Stripe, Notion

### Fact Somebody Has to Read Before Acting

**ID:** `litespec/frontend/patterns/feedback-and-states#fact-somebody-has-to-read-before-acting`

**Strength:** default

**Question:** Some pages state a fact before staff act, such as a low budget or a closing window. Only a command result is an alert; a static fact is a notice. A tone paints the object in that state, never a full band across other work.

**Default:** `a-neutral-notice-above-the-page`

**Compact:** Below the md breakpoint the default option reflows: the notice sits under the header and above the sections.

**Rationale:** The default is one neutral sentence above the page, with no tone and no band. It says why the page holds its state and stays out of the work. The runner-up is the notice inside the section it is about; prefer it where the fact changes what the reader does in that section only. The side column is the other alternative for a record where the fact changes the next step. A full-width toned band for one record stays out: it covers other work with an alarm meant for one record.

**Reviewed for:** Does every built must-read notice match the wireframe its catalog entry chose? Open the built page notices, section notices, and side columns beside the catalog entry recording the chosen option and its reason.

#### `a-neutral-notice-above-the-page`: A Neutral Notice Above the Page

```wireframe
page
  breadcrumb "Home" "Policies" "Travel policy"
  header
    heading "Travel policy"
    status "Active" positive
    text "Code TRAVEL-01 at the Rotterdam office."
    action "Pause filing"
    action primary "Add a ceiling"
  message neutral "Claims are nearly closed. 588 lines left of 10,000 pay in about four days."
  region "Claims"
    fact "Claimed" "9,412 of 10,000 lines"
    fact "Left" "588 lines"
    text muted "At the pace of the last seven days the rest sells in four days."
```

**Rationale:** The page shows one neutral sentence about itself above the sections: not a band and not a tone. It says why the page is what it is.

**Cost:** It sits above the sections, so it is the first thing and the last thing read. A long notice pushes the work down on every visit.

#### `the-notice-inside-the-section-it-is-about`: The Notice Inside the Section It Is About

```wireframe
page
  breadcrumb "Home" "Expenses" "Reversals"
  header
    heading "Reversals"
    text "31 expenses to decide."
  region "Outside the recall window"
    message critical "12 of these cannot be reversed. This route reverses only within 180 days."
    list 2
      item: text "EXP-1040" · text muted "Elin Lindqvist asked 214 days ago" · status "No route" critical · text "EUR 45.00" · action "Apply deduction"
  region "Inside the window"
    text muted "These 19 the bank will take."
    list 2
      item: text "EXP-1042" · text muted "Ava asked 6 days ago" · status "Open" neutral · text "EUR 45.00" · action "Reverse"
```

**Rationale:** A fact about one section goes in that section, not at the top of the page. The reader meets it exactly where it changes what they do.

**Cost:** The reader has to open the section to learn something that may change whether they should be in the section at all.

#### `the-fact-beside-the-record-in-its-side-column`: The Fact Beside the Record, in Its Side Column

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava of Harbor and Pine."
    action "Resend the statement"
    action "Reverse"
  split 2:1
    region "The money"
      fact "Two train fares" "EUR 90.00"
      fact "Late fee" "EUR 5.00"
      fact "Paid to the spender" "EUR 125.00"
      fact "Reversed so far" "EUR 0.00"
    aside
      card "Before you reverse"
        message caution "The recall window closes on 6 Apr 2027, 180 days after payout."
        text muted "After that Harbor and Pine can deduct it or take a bank transfer."
```

**Rationale:** The fact sits in a right-hand column beside the record it is about. The main column stays for the work.

**Cost:** A side column drops below the record on a phone, so the fact is read last there. It needs a heading that says why it is there.

**Reference:** Stripe, GitHub

### Reader May Not Do This

**ID:** `litespec/frontend/patterns/feedback-and-states#reader-may-not-do-this`

**Strength:** default

**Question:** Staff meet pages their role cannot use. Each such page shows the reason and the grant that would help. A back office with several roles meets this state on most pages, so it needs one shape.

**Default:** `the-commands-are-refused-each-with-its-grant-named`

**Compact:** Below the md breakpoint the default option reflows: each refused command and its reason stack under the control.

**Rationale:** The default is the explained action. Each refused command names the role that grants it, and the record stays readable; on a call, reading the record is the point. The runner-up is the read-only section with values as text; prefer it where a form that fails on `Save` hurts more. The absent tab is the other alternative for a capability the reader should never see offered. Refusing the whole page stays out: a support agent needs the spender name on a call, and the refusal hides more than it protects.

**Reviewed for:** Does every built forbidden view match the wireframe its catalog entry chose? Open the built refused commands, absent tabs, and read-only sections beside the catalog entry recording the chosen option and its reason.

#### `the-commands-are-refused-each-with-its-grant-named`: The Commands Are Refused, Each with Its Grant Named

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava of Harbor and Pine."
    action disabled "Message the spender"
    message neutral "Support and Owners message spenders. Eli is support staff."
  region "Reversals"
    text muted "Paid in full on 8 Oct 2026. Nothing reversed."
    action disabled "Reverse this expense"
    message neutral "A reversal moves EUR 125.00, so only Finance and Owners issue one."
  region "The spender"
    fact "Name" "Ava"
    fact "Email" "ava@harbor-pine.example"
    fact "Country" "Netherlands"
```

**Rationale:** This is the explained action: the button is refused and the reason is visible text beside it, naming the role that can act. The record stays readable, which on a call is the point.

**Cost:** A page of refused commands looks broken to someone who does not know their role. The role's name in the reason is what makes it legible.

#### `the-tab-is-absent-so-the-reader-never-sees-the-gap`: The Tab Is Absent, so the Reader Never Sees the Gap

```wireframe
page
  breadcrumb "Home" "Payout records"
  header
    heading "Payout records"
    text "What finance paid today, newest first."
  split 1:3
    nav side
      link "Today"
      link "Payout lists"
      link current "Payout records"
      link "My shifts"
    region "Today"
      text muted "Statements and Reports stay absent here. An owner grants them."
      list 3
        item: text "EXP-1042" · text muted "Scanned, batch A by Ben" · text "09:41"
```

**Rationale:** A tab, sidebar entry, or link appears only for a viewer who may open its page. Statements, Reports, and the money pages stay absent from a support agent's sidebar.

**Cost:** The reader never learns the capability exists. On a surface where someone is guessing at what the product can do, that is a real cost.

#### `a-read-only-section-values-as-text-one-line-saying-why`: A Read-Only Section: Values as Text, One Line Saying Why

```wireframe
page
  breadcrumb "Home" "Settings" "Late fee"
  header
    heading "Late fee"
    text "What the spender pays on top of the expense."
  region "The fee"
    text muted "Only Finance and Owners change the fee. Ben is finance manager."
    fact "Per expense" "EUR 5.00"
    fact "Charged on" "Portal expenses"
    fact "Last changed" "2 Sep 2026 by Cara"
```

**Rationale:** The settings show plain values to a role that reads but never changes them. One line at the top of the section names the role that edits. Nothing looks like a form that fails on `Save`.

**Cost:** The page looks different for different roles, so a support call about it starts with which role the caller has.

**Reference:** Stripe

### Connection Is Gone

**ID:** `litespec/frontend/patterns/feedback-and-states#connection-is-gone`

**Strength:** default

**Question:** Staff lose the connection while working, so the page on screen goes stale. The question is what a stale page says: how fresh its data is, what still works, and when to retry.

**Default:** `a-banner-saying-what-is-stale-and-what-still-works`

**Compact:** Below the md breakpoint the default option reflows: the band sits above the breadcrumb, and the table keeps its first two columns.

**Rationale:** The default is one band saying what is stale, when it was read, and what still works. A stale page the reader can still read is better than a page that shows nothing. The runner-up is the failed read state, which suits a page with nothing to show because the read itself failed. Showing nothing is the other alternative for a page where acting on stale money would move the wrong money. Whatever the reader typed stays on the page while the connection is gone, and every failed read offers its retry. Queuing a command that moves money stays out: a reversal nobody confirmed is a liability, and the page says plainly that the money has not moved.

**Reviewed for:** Does every built offline screen match the wireframe its catalog entry chose? Open the built stale banners, failed reads, and refused pages beside the catalog entry recording the chosen option and its reason.

#### `a-banner-saying-what-is-stale-and-what-still-works`: A Banner Saying What Is Stale and What Still Works

```wireframe
page
  message neutral "No connection to the server. Read 4 minutes ago. Reading and export work."
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses. Read 4 minutes ago."
    action "Export"
  toolbar
    field "Search" search = "EXP-1042"
  region "Expenses"
    table 3 "Expense" "Spender" "Paid"
      item: text "EXP-1042" · text "Ava" · text "EUR 125.00"
    action disabled "Open an expense"
    message neutral "Nothing done here reaches the server while offline."
```

**Rationale:** The page shows a neutral band naming when it last read and which commands are refused while it is offline. The reader can still read, and knows it.

**Cost:** Every page needs a last-read time and a notion of which commands are safe offline. Commands that change money are almost never safe offline.

#### `the-read-fails-in-place-the-server-could-not-be-reached`: The Read Fails in Place: The Server Could Not Be Reached

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every expense, newest first."
  toolbar
    field "Search" search = "EXP-1042"
  region "Expenses"
    heading small "The server could not be reached"
    text "Check the connection and try again."
    action primary "Try again"
```

**Rationale:** Where a read fails because nothing answered, the section says the server could not be reached and offers `Try again`. Nothing claims to be current, because nothing is shown.

**Cost:** It appears only when a read is tried. A page that loaded before the connection dropped looks normal until the reader does something.

#### `nothing-shown-because-a-stale-page-is-a-dangerous-one`: Nothing Shown, Because a Stale Page Is a Dangerous One

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
  region "No page"
    heading small "This page cannot be shown"
    text "Read 11 minutes ago. Acting on that amount could move the wrong money."
    action primary "Try again"
```

**Rationale:** Where the reader is about to act on money, a stale page is refused outright. The page says so and offers one thing: `Try again`.

**Cost:** A reader who must answer a question on a call gets nothing. The printed payout list exists precisely because a caller cannot be refused, which is why the two surfaces answer this differently.

### Sign-In Ran Out

**ID:** `litespec/frontend/patterns/feedback-and-states#sign-in-ran-out`

**Strength:** default

**Question:** A session ends while the reader is working. Sign-in runs on the application's own origin, and a refused or ended session lands on `/signed-out`. The reader's unsent work is what this state keeps.

**Default:** `a-dialog-over-the-work-which-stays-where-it-is`

**Compact:** Below the md breakpoint the default option reflows: the dialog fills the screen, and the form stays behind it.

**Rationale:** The default is the dialog over the work, which stays where it is. It keeps unsent work on screen and names the one thing to do. The runner-up is the warning before the end, which suits a reader still at the desk who never meets the expired state. The signed-out page is the other alternative for a reader with no page to keep. A toast alone stays out: the message leaves in seconds and the typed work leaves with it.

**Reviewed for:** Does every built expired session match the wireframe its catalog entry chose? Open the built sign-out pages, warnings, and re-authentication dialogs beside the catalog entry recording the chosen option and its reason.

#### `a-dialog-over-the-work-which-stays-where-it-is`: A Dialog over the Work, Which Stays Where It Is

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "Trip"
  header
    heading "Trip"
    text "Sat 14 Mar 2026 at the Rotterdam client site."
  region "The trip"
    field "Name" text = "Rotterdam client visit"
    field "Date" text = "14/03/2026"
    field "Arrival" text = "08:00"
    field "Notes" textarea = "Forty receipts for Harbor and Pine, batch A."
  dialog
    heading "The sign-in expired"
    text "Sign in again. What was typed stays on this page."
    action "Sign out"
    action primary "Sign in again"
```

**Rationale:** The work stays on screen behind a dialog. Signing in again returns to it, and nothing typed is lost.

**Cost:** A dialog covers a form the reader cannot touch until they sign in. On a phone it fills the screen, which is acceptable because there is exactly one thing to do.

**Reference:** Stripe, Linear

#### `warned-before-it-ends`: Warned Before It Ends

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses, newest first."
    action "Export"
  region "Sign-in warning"
    message caution "The sign-in ends in 5 minutes. Stay signed in to keep working."
    action primary "Stay signed in"
    action "Sign out"
  region "Expenses"
    table 3 "Expense" "Spender" "Paid"
      item: text "EXP-1042" · text "Ava" · text "EUR 125.00"
```

**Rationale:** The session warns before it ends, with a warning that stays until the reader extends the session or signs out. The warning is announced through the assertive region, so a reader who is still there never meets the expired state at all.

**Cost:** The warning sits on the page until it is answered, so it takes space from the work it guards. It appears only when the reader is idle enough to need it and active enough to answer it.

**Reference:** DigiD

#### `signed-out-on-its-own-page`: Signed Out, on Its Own Page

```wireframe
page
  header
    heading "Signed out"
    text "The sign-in expired. Sign in again."
    action primary "Sign in again"
  region "The account"
    fact "Last reader" "Ben, finance manager"
    fact "Company" "Harbor and Pine"
    text muted "No back-office frame shows without a session."
```

**Rationale:** A refused or ended session lands on `/signed-out`, worded by one sentence: `Your sign-in has expired. Sign in again.` There is no back-office frame, because there is no session to show it to.

**Cost:** The reader leaves their work to sign in again. Whether anything typed survives depends on the client keeping the page, which is exactly what the other options make explicit.

### Somebody Else Saved First

**ID:** `litespec/frontend/patterns/feedback-and-states#somebody-else-saved-first`

**Strength:** default

**Question:** Two people edit one record. The API refuses the second save with `concurrency/record.changed-elsewhere`, worded as `Someone else changed this`. The reader's unsent edits stay on the page.

**Default:** `the-refusal-above-save-with-her-value-under-the-field`

**Compact:** Below the md breakpoint the default option reflows: the refusal sits above the fields, and each other value sits under its field.

**Rationale:** The default is the refusal above `Save`, with her value under the field. It refuses in the dictionary's words and fixes only what collided, with no dialog and no lost keystroke. The runner-up is the field-by-field choice, which suits a record where both people changed several fields. The whole-record dialog is the other alternative for two people who rewrote the same long notes. A lock that reloads her whole version stays out: it deletes Ben's notes to resolve an arrival time.

**Reviewed for:** Does every built conflict screen match the wireframe its catalog entry chose? Open the built field-level refusals, field-by-field merges, and conflict dialogs beside the catalog entry recording the chosen option and its reason.

#### `the-refusal-above-save-with-her-value-under-the-field`: The Refusal Above Save, with Her Value Under the Field

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "Trip"
  header
    heading "Trip"
    text "Sat 14 Mar 2026 at the Rotterdam client site."
  message critical "Someone else changed this. Cara saved at 09:34. Look, then save again."
  region "The trip"
    field "Name" text = "Rotterdam client visit"
    field "Arrival" text = "07:00"
    message info "Cara saved 08:00 at 09:34."
    action quiet "Use hers"
    field "Notes" textarea = "Arrival at 07:00, unloading by 11:00."
    row
      action "Cancel"
      action primary "Save"
```

**Rationale:** The save is refused in words. The other person's value appears under the field it beats, with one button to take hers. The reader fixes exactly what collided and nothing else.

**Cost:** This option works field by field. Where both people rewrote the same long notes, one button per field is not enough, and the dialog option takes over.

**Reference:** Linear

#### `field-by-field-keep-mine-or-use-hers`: Field by Field: Keep Mine or Use Hers

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "Trip"
  header
    heading "Two fields differ"
    text "Cara saved at 09:34. Choose one value per row, then save."
  region "Arrival"
    fact "Yours, not saved" "07:00"
    fact "Cara, saved 09:34" "08:00"
    row
      action "Keep mine"
      action primary "Use hers"
  region "Notes"
    fact "Yours, not saved" "Arrival at 07:00, unloading by 11:00."
    fact "Cara, saved 09:34" "Arrival at 08:00, scanning by noon."
    row
      action "Keep mine"
      action primary "Use hers"
```

**Rationale:** A conflict resolves per field: each row shows both values and takes one. Nothing is reloaded wholesale, so nothing Ben typed is lost.

**Cost:** A row per field is a second form to build and to read. Past three fields it runs longer than the form it argues about.

**Reference:** Contentful, Airtable

#### `a-dialog-with-three-ways-out`: A Dialog with Three Ways Out

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "Trip"
  header
    heading "Trip"
    text "Sat 14 Mar 2026 at the Rotterdam client site."
  region "The trip"
    field "Name" text = "Rotterdam client visit"
    field "Arrival" text = "07:00"
    field "Notes" textarea = "Arrival at 07:00, unloading by 11:00."
  dialog
    heading "Someone else changed this"
    text "Cara saved at 09:34 after this page opened. Edits stay on the page."
    fact "She changed" "Arrival, notes"
    fact "You changed" "Arrival, notes"
    action "Use hers"
    action "Save mine anyway"
    action primary "Compare the two"
```

**Rationale:** On a conflict the dialog offers three buttons with no field surgery: keep mine, take hers, or look at both. It says whose version is whose.

**Cost:** This is a whole-record choice: Ben keeps both his arrival time and his notes, or takes both of hers.

**Reference:** Dropbox, Office

### Work That Runs Without You

**ID:** `litespec/frontend/patterns/feedback-and-states#work-that-runs-without-you`

**Strength:** default

**Question:** An import or a bulk reversal runs for minutes, longer than the reader waits on one page. The reader starts it and moves on to other work. The question is how the job stays visible while it runs, and how it reports its result when it is done.

**Default:** `an-activity-tray-like-an-upload-panel`

**Compact:** Below the md breakpoint the default option reflows: the tray becomes a sheet over the page, opened from one control.

**Rationale:** The default is the activity tray. It stays visible on every page, it never holds the reader, and it answers progress without being asked. The runner-up is the job page with the failures listed; prefer it for a bulk reversal where two of 31 need a person and the rest do not. The finished notice suits a job nobody needs to watch. The dialog that holds the reader stays out: minutes of a blocked page are the cost this pattern exists to remove.

**Reviewed for:** Does every built background job match the wireframe its catalog entry chose? Open the built trays, job pages, and finished notices beside the catalog entry recording the chosen option and its reason.

#### `an-activity-tray-like-an-upload-panel`: An Activity Tray, Like an Upload Panel

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses, newest first."
    action "Export"
  region
    table 4 "Expense" "Spender" "Filed" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 2026, 09:32" · status "Paid" positive · text "EUR 125.00"
  popover
    heading small "Background work"
    text muted "1 running, 1 waiting"
    card "Spender list import"
      text "70% · 595 of 850 records"
      text muted "Started 14:22 by Ben"
    card "September export"
      text "Waiting · 1,204 expenses"
      text muted "Starts when the import finishes"
```

**Rationale:** A tray in the corner runs uploads and imports. It shows progress while each job runs and a line when it is done, and the page behind stays usable throughout.

**Cost:** A corner tray holds two jobs well and five badly. Past that the jobs need a page of their own, and the tray becomes that page's summary.

**Reference:** Vercel, Linear, Figma

#### `a-job-page-with-the-failures-listed`: A Job Page with the Failures Listed

```wireframe
page
  breadcrumb "Home" "Expenses" "Reversals"
  header
    heading "Bulk reversal · 29 of 31 sent"
    text "Finished 8 Oct 2026, 11:04. Each reversal went back to the company account."
  message "2 were not sent. Their payouts are past 180 days." caution
  region "The 2 that need you"
    list 2
      item: text "EXP-1040 · Elin Lindqvist" · status "Not sent" critical · text "EUR 45.00" · action "Apply deduction"
```

**Rationale:** A bulk run ends on a page that lists what failed and why, each failure with its retry. The 29 reversals that worked need no attention; the two that did not get all of it.

**Cost:** A page per job is a place readers bookmark and support has to explain. It earns its keep only for jobs that can partly fail.

**Reference:** Stripe, Shopify

#### `told-when-it-is-done-not-while-it-runs`: Told When It Is Done, Not While It Runs

```wireframe
page
  breadcrumb "Home" "Spenders" "Spender list"
  header
    heading "Spender list"
    text "850 records, imported 14:31."
  region
    list 2
      item: text "Ava" · text muted "ava@harbor-pine.example" · status "Imported" positive
  region "What happened"
    timeline 2
      item: text "850 records imported" · text muted "8 Oct 2026, 14:31" · text muted "Ben"
  toast positive "850 records are on the spender list."
```

**Rationale:** The reader hears nothing until the run finishes, because while it runs there is nothing to decide. A toast plus a timeline entry then answers the only two questions: did it work, and where is the evidence.

**Cost:** The page stays silent while the job runs. A reader waiting for 850 records with no progress bar waits less patiently than one watching progress climb past 70%.

**Reference:** GitHub Actions

### This Record Does Not Exist

**ID:** `litespec/frontend/patterns/feedback-and-states#this-record-does-not-exist`

**Strength:** default

**Question:** A link names a record that is gone, mistyped, or at another company. A problem state words that as `Not found`. The decision is how the page gives the reader a way back instead of a bare code.

**Default:** `not-found-with-the-way-back`

**Compact:** Below the md breakpoint the default option reflows: the title, the explanation, and the two ways back stack in one column.

**Rationale:** The default is `Not found`, with the way back. It names the record, covers the three ways a record goes missing, and offers the list and the search. The runner-up is the section that says so while the page stays; prefer it for a child missing from a parent that exists. Naming who removed a record and when suits a record the reader saw yesterday. The bare code stays out: a code means nothing to a reader, and a page with no way back is no better for carrying a number.

**Reviewed for:** Does every built missing-record screen match the wireframe its catalog entry chose? Open the built not-found pages, section notices, and removal notices beside the catalog entry recording the chosen option and its reason.

#### `not-found-with-the-way-back`: Not Found, with the Way Back

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1099"
  header
    heading "Expense EXP-1099 does not exist"
    text "It may be typed wrong, removed, or at another company."
    text "Nothing was paid and nothing changed."
    action primary "Back to Expenses"
    action "Search expenses"
```

**Rationale:** The page names the missing record, says what may have happened, and offers the list and the search. A reader who typed one character wrong finds the record there.

**Cost:** The suggestions are guesses. Where the record belongs to another company, no suggestion helps, and the copy has to say so rather than keep guessing.

**Reference:** Stripe, Linear

#### `the-section-says-so-the-page-stays`: The Section Says so, the Page Stays

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · Harbor and Pine"
  region "The money"
    fact "Paid" "EUR 125.00"
    fact "Reversed so far" "EUR 0.00"
  region "Lines"
    heading small "Line DL-9918 is not on this expense"
    text "This expense holds 3 lines. The code may be typed wrong."
    action "See the 3 lines"
```

**Rationale:** The expense itself exists; only line DL-9918 on it does not, so only the lines section carries the notice. The rest of the record stays readable around it.

**Cost:** This shape fits only a child missing from a parent that exists. Where the whole record is gone, a surviving page is a lie.

**Reference:** GitHub

#### `gone-and-when`: Gone, and When

```wireframe
page
  breadcrumb "Home" "Policies" "Per diem, standard"
  header
    icon "Removed"
    heading "Per diem, standard was removed"
    text "Dana removed it on 2 Sep 2026, before anything was claimed."
    text "Its record stays in the audit trail."
    action primary "Back to Policies"
```

**Rationale:** The page names who removed the record and when. Saying `does not exist` about something the reader saw yesterday reads as a fault; the audit trail already knows what happened, so the page says it too.

**Cost:** Naming the removal confirms the record existed, which leaks across companies if the reader should not know. For another company's record the page has to fall back to the default option.

**Reference:** Linear, Notion

### Something Went Wrong on Our Side

**ID:** `litespec/frontend/patterns/feedback-and-states#something-went-wrong-on-our-side`

**Strength:** default

**Question:** The server fails a read or a command. A problem state words that failure as `That did not go through`, with a reference the reader can quote. The decision is how the page tells the reader what did not happen.

**Default:** `the-shipped-failure-with-its-reference`

**Compact:** Below the md breakpoint the default option reflows: the failure, its reference, and `Try again` stack in one column.

**Rationale:** The default is the failure with its reference. It words the failure once, says what did not move, and gives support something to read back. The runner-up is the outcome page; prefer it where the money sentence deserves a page of its own. The failed section suits a page whose other sections still read. Old data with a quiet line stays out: figures the server could not confirm are misinformation, and a quiet line does not make them honest.

**Reviewed for:** Does every built server-failure screen match the wireframe its catalog entry chose? Open the built failure states, outcome pages, and failed sections beside the catalog entry recording the chosen option and its reason.

#### `the-shipped-failure-with-its-reference`: The Shipped Failure with Its Reference

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · Harbor and Pine"
  region
    heading small "That did not go through"
    text "The reversal could not be sent. The money has not moved and the spender has not been told."
    fact "Reference" "RF-EXP-1042-181"
    action quiet "Copy"
    action "Try again"
```

**Rationale:** The failure carries a title, caller-safe detail, `Try again`, and a reference the reader can quote to support. The money sentence is the point of it all: it says what did not move.

**Cost:** A reference only helps if support can read it back. Without that lookup it is a number that comforts nobody.

#### `an-outcome-page-that-says-what-did-not-happen`: An Outcome Page That Says What Did Not Happen

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "The reversal was not sent"
    text "The money has not moved and Ava has not been told."
    text "Nothing changed on expense EXP-1042."
    fact "Reference" "RF-EXP-1042-181"
    action quiet "Copy"
    action primary "Try again"
    action "Back to the expense"
```

**Rationale:** The failure page leads with what did not happen, because after a failed reversal that is the reader's first question. The money sentence comes before the retry.

**Cost:** A page per failure weighs more than a state in place. It suits a failed command, not a failed read, where the inline state stays.

**Reference:** Stripe

#### `the-section-fails-the-page-stays`: The Section Fails, the Page Stays

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · Harbor and Pine"
  region "The money"
    heading small "That did not go through"
    text "The money could not be read. The spender below was read normally."
    action "Try again"
  region "The spender"
    fact "Name" "Ava"
    fact "Email" "ava@harbor-pine.example"
    fact "Country" "Netherlands"
```

**Rationale:** The money section fails into its retry while the spender section stays readable. One failed read does not take the page.

**Cost:** Two sections can fail two different ways on one page. Past one failure the page is a list of retries, and the outcome page takes over.

### Back Office Is Down for a Short While

**ID:** `litespec/frontend/patterns/feedback-and-states#back-office-is-down-for-a-short-while`

**Strength:** default

**Question:** A planned window stops the back office. Finance keeps its printed payout lists, but the payout desk checks every receipt against the system, so payouts wait too. The decision is how the reader learns the window, what still works, and when to come back.

**Default:** `told-ahead-of-time-on-every-page`

**Compact:** Below the md breakpoint the default option reflows: the notice sits under the header, and the facts stack in one column.

**Rationale:** The default is the advance notice on every page. A window nobody meets by surprise is a window finance can plan around, and the printed payout lists are the plan. The runner-up is the maintenance page itself, which is what every page becomes while the window runs. Read-only access suits a short window where the records still answer a call. A lone message stays out as the only signal: a Thursday message tells once, and only the back office itself reminds on Sunday at 02:00.

**Reviewed for:** Does every built maintenance screen match the wireframe its catalog entry chose? Open the built advance notices, maintenance pages, and read-only windows beside the catalog entry recording the chosen option and its reason.

#### `told-ahead-of-time-on-every-page`: Told Ahead of Time, on Every Page

```wireframe
page
  breadcrumb "Home"
  message "The back office is unavailable Sun 15 Mar, 02:00 to 02:30. Finance keeps its printed payout lists." info
  header
    heading "Good morning, Ben"
    text "Harbor and Pine · 40 receipts reach batch A at 08:00."
  region "What still works during the window"
    fact "Printed payout lists" "Valid all night"
    fact "Payout desk" "Waits until 02:30"
    fact "Portal" "Paused"
```

**Rationale:** One sentence sits on every page from days before, naming the window and what still works. Nobody meets maintenance by surprise, and finance prints its payout lists in time.

**Cost:** A band on every page for days is days of noise for a 30-minute window. It has to appear late enough to matter, and early enough that finance can plan its work around it.

**Reference:** Stripe, Linear

#### `the-maintenance-page-itself`: The Maintenance Page Itself

```wireframe
page
  breadcrumb "Home"
  header
    text muted "Harbor and Pine"
    heading "The back office is down for maintenance"
    text "Back by 02:30. Finance keeps its printed payout lists. Payouts wait until the system is back."
    text muted "Started 02:00 · 18 minutes left"
    action primary "Check again"
```

**Rationale:** The window gets a page of its own: what is happening, when it ends, and what still works. While the window runs, this is every page.

**Cost:** Nothing can be read behind it, including the payout lists. Anything finance needs during the window has to be printed before 02:00.

**Reference:** GitHub, Vercel

#### `read-only-while-it-lasts`: Read-Only While It Lasts

```wireframe
page
  breadcrumb "Home" "Expenses"
  message "The back office is in maintenance until 02:30. You can read. Nothing you do here reaches the server." neutral
  header
    heading "Expenses"
    text "268 expenses. Read 2 minutes ago."
  region
    table 4 "Expense" "Spender" "Filed" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 2026, 09:32" · status "Paid" positive · text "EUR 125.00"
  region "Reversals"
    action disabled "Reverse an expense"
    message "Maintenance ends at 02:30. Reversals wait until then." neutral
```

**Rationale:** The back office drops to reading while writing is unsafe. Staff can still read expenses on a call, and nothing that changes money is offered anywhere.

**Cost:** Read-only mode needs every command to know it is refused and why. A page that forgets one command offers a reversal that fails at 02:15.

**Reference:** Shopify, Notion

### Too Many Tries

**ID:** `litespec/frontend/patterns/feedback-and-states#too-many-tries`

**Strength:** default

**Question:** The reader hits a limit: too many exports, too many sign-in tries, or too many sends. The API answers `429` and says when to try again. The decision is how the page states the wait and the reason, briefly.

**Default:** `the-button-waits-and-says-how-long`

**Compact:** Below the md breakpoint the default option reflows: the refusal sits under the disabled button, and the table keeps its first two columns.

**Rationale:** The default is the button that waits and says how long. It turns the limit into a 40-second wait with its reason attached, which is all a reader who ran 12 exports needs. The runner-up states the limit before it hits; prefer it for a reader planning a month of exports rather than one too many. The stop page suits a limit measured in minutes. The spinner that never says why stays out: a wait the reader cannot name reads as a page that broke, and reloading restarts it.

**Reviewed for:** Does every built rate-limit screen match the wireframe its catalog entry chose? Open the built waits, usage figures, and stop pages beside the catalog entry recording the chosen option and its reason.

#### `the-button-waits-and-says-how-long`: The Button Waits, and Says How Long

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses, newest first."
    action disabled "Export (40 s)"
  message "12 exports in the last hour. The limit is 12 an hour. Try again in 40 seconds." neutral
  region
    table 4 "Expense" "Spender" "Filed" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 2026, 09:32" · status "Paid" positive · text "EUR 125.00"
```

**Rationale:** The button carries the wait the API sent back in `Retry-After`, counting down from 40 seconds, and the message beside it names the limit that was hit. The reader sees a wait with an end and a reason, instead of a button that refuses every click without saying why.

**Cost:** That countdown is a timer the page must keep honestly. If the reader reloads mid-wait, the count must survive the reload or start over from a true value, never from a stale one.

**Reference:** GitHub, Stripe

#### `the-limit-stated-before-it-hits`: The Limit Stated Before It Hits

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses, newest first."
    action "Export"
  region "Exports"
    fact "9 of 12 used this hour" "3 left"
    text "75% used. Your plan allows 12 an hour."
    text muted "The 13th export waits for the next hour."
  region
    table 3 "Expense" "Spender" "Filed" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 2026, 09:32" · status "Paid" positive · text "EUR 125.00"
```

**Rationale:** The page shows usage against the ceiling before anything refuses: nine of 12 exports used, with three left. A reader who sees that figure plans the last three exports.

**Cost:** Usage figures are a read of their own, and a stale figure refuses a reader it just told there was room. The figure on the page and the enforcement behind it must always agree.

**Reference:** Stripe, Linear

#### `the-429-page`: The 429 Page

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Too many tries"
    text "Try again in 10 minutes. Nothing was sent twice and nothing is lost."
    fact "Reference" "RL-EXP-1042-03"
    action quiet "Copy"
    action primary "Back to Expenses"
```

**Rationale:** A hard stop gets a whole page: what happened, when to come back, and a reference to quote. There is deliberately no retry button, because retrying is what caused this state.

**Cost:** The page offers nothing to do but wait, so it fits a stop measured in minutes. For a stop measured in seconds a whole page is too much, and the default option handles those.

**Reference:** Cloudflare
