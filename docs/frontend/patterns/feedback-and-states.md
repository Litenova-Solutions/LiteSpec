# Feedback and State Patterns

## Intent

This page states what each screen state shows and how results read, as drawn options with a named default to build: empty, loading, forbidden, offline, signed out, conflicted, missing, failed, rate-limited, and under maintenance. The product owner chooses among the options when the team builds a list, a form, or any command that can fail. Decided states keep staff informed instead of guessing: each screen says what happened, what it means, and what to do next.

## Concepts

### How to Read a Pattern

Each pattern answers one recurring question about screen states, such as what a list shows before its first records exist. It states the question, the default option a project builds, how the default behaves on a narrow screen, and why the default won. Each option then shows one wireframe drawing with its own rationale, cost, and optional reference. A project records its choice in its pattern catalog and builds the chosen option with its own components. The [UI patterns](../patterns.md) page states the catalog contract and the wireframe language.

### The Running Example

The drawings on this page use the Claimwick payout desk: 268 expenses including EXP-1042 for Ava, reversals such as the EUR 125.00 on that expense, and the trip, payout, and budget facts around them. On this page, the reader is the staff member using the screen. The Standards section states the two rules every option keeps: a refusal or error message states what failed and the next step, and a failed save keeps the reader's input on screen.

## Standards

### State What Failed and the Next Step

**ID:** `litespec/frontend/patterns/feedback-and-states#state-what-failed-and-the-next-step`

**Statement:** A refusal or error message MUST state what failed and the next step the reader can take.

**Rationale:** A message that only says something went wrong leaves the reader guessing whether to retry, fix input, or ask for help, and most readers retry blindly or give up. Naming what failed plus the one next step turns the dead end into a task the reader can complete.

**Reviewed for:** Which built failure message omits the cause or the way forward? Open the refused commands and failed reads on built screens.

### Keep the Reader's Input Through a Failure

**ID:** `litespec/frontend/patterns/feedback-and-states#keep-the-readers-input-through-a-failure`

**Statement:** A failed save MUST keep the reader's unsaved input on the screen.

**Rationale:** Retyping a whole form to fix one refused field wastes the reader's time and introduces new typos. Worse, readers who lose work to a failure stop trusting the form and copy long answers elsewhere before saving.

**Reviewed for:** Which built form drops typed values after a refused save? Open the failed submits on built forms and check every field.

## Patterns

### Nothing Has Ever Existed

**ID:** `litespec/frontend/patterns/feedback-and-states#nothing-has-ever-existed`

**Question:** No record of this kind exists yet. The page states that plainly, says what the first record is for, and offers the one command that creates it.

**Default:** `positive-title-one-action`

**Compact:** The recommended option reflows: one column with the header actions under the title.

**Rationale:** The pick stays on the positive title with one action. It is the smallest shape and the only option that asks nothing of a reader who has nothing yet. The runner-up is the setup guide on Home. Prefer it when the first expense needs steps across several pages. Never ship the table with a dashed sample row. It shows a record that does not exist, and opening it leads nowhere.

**Reviewed for:** Which built first-run screen departs from the wireframe its catalog entry chose? Open the built empty lists and setup guides beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The page shows one positive title, one line saying what a policy is for, and one button that creates the first one.

**Cost:** The reader cannot see what a row will look like, so every column is undiscoverable until there is data. Carbon accepts this.

**Reference:** Carbon

#### `the-empty-state-beside-the-thing-it-fills`: The Empty State Beside the Thing It Fills

```wireframe
page
  breadcrumb "Home" "Policies"
  header
    heading "Policies"
    text "Nothing here yet."
    action primary "New policy"
  split 2:1
    region "Policies"
      message "No policies yet. A policy holds a name, a code, a ceiling and claims." neutral
      action primary "New policy"
    aside "What a first policy needs"
      list 4
        item: text "A name" · text muted "Spenders see it in the portal"
```

**Rationale:** The list holds nothing, and beside it the page says what the first policy needs to be payable. The empty state is a checklist rather than a sentence.

**Cost:** A list page whose job is finding becomes a setup page while empty. A reader with no policies has no reason to want a list at all.

#### `empty-with-the-first-record-s-shape-shown`: Empty with the First Record's Shape Shown

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses, newest first."
    action "Export"
  toolbar
    field "Search expenses" search
    field "State" select = "Any state"
  region "Expenses"
    message "No expenses yet. An expense is filed by a spender, not by you." neutral
    action primary "Publish a policy"
    table 5 "Expense" "Spender" "Filed" "State" "Paid"
      item: text muted "EXP-1042" · text muted "Ava" · text muted "8 Oct 2026, 09:32" · status "Paid" neutral · text muted "EUR 125.00"
```

**Rationale:** The table keeps its headers and shows a single dashed row with what a row will contain, so the columns are known before there is data.

**Cost:** Carbon says an empty state should replace the table, headers included, because a screen reader otherwise reads six empty headers before the message. This deliberately breaks that.

**Reference:** Carbon

#### `empty-inside-a-section-beside-the-sections-that-do-have-thin`: Empty Inside a Section, Beside the Sections That Do Have Things

```wireframe
page
  breadcrumb "Home" "Statements"
  header
    heading "Statements"
    text "September 2026. Every euro of the month under the kind it is."
  region "Disputes"
    text muted "5 the month before."
    message "No open disputes. Nothing is being challenged by a bank this month." neutral
  region "Unmatched payouts"
    text muted "1 the month before."
    message "Nothing unmatched. Every euro the bank sent matched an expense." neutral
  region "Questions"
    message "No spender questions. Nobody has asked about an expense this month." neutral
```

**Rationale:** One section of a page is empty while its neighbors are not. The empty state is scoped to its section and takes no page-level action.

**Cost:** A section with an empty state is taller than a section with three rows, which breaks the rhythm of a page of sections. It also invites a per-section action bar.

#### `empty-with-a-reason-and-a-way-to-fix-it`: Empty with a Reason and a Way to Fix It

```wireframe
page
  breadcrumb "Home" "Statements"
  header
    heading "Statements"
    text "Nothing has settled yet."
  region "October"
    message "Nothing to show for October. The month has not ended. The first settlement lands on 4 November." neutral
    action "See what settled in September"
  region "Why October is empty"
    text muted "Figures so far. They become the October statement when the month closes."
    fact "Expenses filed in October" "1,284"
    fact "First settlement covers" "4 Nov"
    fact "Amount expected" "EUR 62,180.00"
```

**Rationale:** The page is empty for a reason the reader cannot guess: the month has not closed. The empty state names the reason and the date it changes, and shows the figures that do exist.

**Cost:** A page whose empty state depends on why it is empty needs a read that answers why. That answer is a second thing the API must be able to say.

#### `empty-because-a-permission-hides-everything`: Empty Because a Permission Hides Everything

```wireframe
page
  breadcrumb "Home" "Payouts" "Disputes"
  header
    heading "Disputes"
    text "5 open. The nearest closes in 19 days."
  region "Disputes"
    message "Disputes are not visible to your role. You are Eli, support staff." neutral
    text "Disputes are readable by Support, Finance and Owners, because a dispute concerns money."
    action "Ask an owner for the role"
```

**Rationale:** The list is not empty. It is withheld. Carbon's third empty state is permissions, and this one names the grant rather than saying nothing is there.

**Cost:** It confirms that records exist, which some deployments would rather not do. The header count tells the reader what the body withholds.

**Reference:** Carbon

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

**Cost:** Samples must stay out of every export, total, and spender surface. A sample left in place at go-live ships fake data as the company's own.

#### `one-primary-action-and-two-quieter-ways-to-start`: One Primary Action and Two Quieter Ways to Start

```wireframe
page
  breadcrumb "Home" "Policies"
  header
    heading "Policies"
    text "Every policy Harbor and Pine pays, newest first."
  region "Policies"
    message "Create your first policy. A policy holds a name, a code, a ceiling and claims." neutral
    action primary "New policy"
    action "Start from a template"
    link "What a policy needs before it can pay"
```

**Rationale:** Shopify Polaris uses an `EmptyState` with one primary action, one secondary action, and a link to learn more. The reader can start blank, copy the shape of the standard per diem, or read what a policy needs first.

**Cost:** Three ways in is one decision more than one way in. A template only helps if the templates are good, so this needs at least one template that is worth copying.

**Reference:** Shopify Polaris

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

**Rationale:** The steps to a first expense live once, on Home, with progress. Each list page keeps a plain one-line empty state. Setup is a journey across pages, so it gets one place rather than seven.

**Cost:** The guide has to know what is done, which is a read across several modules. It also needs a way to be dismissed, or it stays on Home after the company has paid 10,000 expenses.

**Reference:** Shopify, Stripe

### Nothing Matches What Was Asked For

**ID:** `litespec/frontend/patterns/feedback-and-states#nothing-matches-what-was-asked-for`

**Question:** A search found nothing, but records exist. The page must say so, name the term that failed, and offer a way back.

**Default:** `different-copy-and-it-names-the-term`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** The pick stays on the copy that names the term. It says what failed and what to change, with the count proving the data exists. The runner-up is the table that keeps its headers. Prefer it where readers filter all day and want the columns to stay put. Never ship the empty state with no actions. A search that names its term can always offer to clear it.

**Reviewed for:** Which built no-results screen departs from the wireframe its catalog entry chose? Open the built filtered empties and suggestion rows beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The title quotes the term that found nothing. The line says how many records exist, so the reader knows the search failed rather than the data. Two actions separate clearing the search from clearing the filters.

**Cost:** The pattern uses two actions, and Carbon says an empty state should offer one primary action. The secondary one is a ghost link, which is the compromise.

**Reference:** Carbon

#### `the-controls-stay-editable-above-the-empty-state`: The Controls Stay Editable Above the Empty State

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Nothing matches."
    action quiet "Clear all"
  toolbar
    field "Search expenses" search = "harbor pine paid"
    field "Spender" select = "Harbor and Pine"
    field "State" select = "Paid"
    field "Date" select = "This month"
  region "Expenses"
    message "No expenses match these four. 268 exist. Clear the search first, since it is the narrowest." neutral
    action "Clear the search only"
```

**Rationale:** The search box and the filters keep their values so the reader can adjust rather than start again. The empty state sits below them, not instead of them.

**Cost:** An empty state with live controls above it is taller than one alone, and the reader has to know which control to change.

#### `suggesting-what-does-match`: Suggesting What Does Match

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Nothing matches ava prins."
  toolbar
    field "Search expenses" search = "ava prins"
    field "Spender" select = "Any spender"
    field "State" select = "Any state"
  region "What comes close"
    message "No spender called Ava Prins. Two records come close." neutral
    list 2
      item: text "Ava" · text muted "ava@harbor-pine.example · 4 expenses" · action "See her expenses"
```

**Rationale:** The empty state offers the nearest thing the reader probably meant: a similar spender name, or the same expense without the filters. A search that helps is a search that proposes.

**Cost:** Every suggestion is a read the API must be able to answer. A wrong suggestion is worse than none, because it sends the reader further away.

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

**Cost:** The list is gone, so the reader cannot see how many records the unfiltered list holds unless the panel says so.

#### `the-same-empty-state-with-no-actions-at-all`: The Same Empty State with No Actions at All

```wireframe
page
  breadcrumb "Home" "Payouts" "Disputes"
  header
    heading "Disputes"
    text "Nothing in 2026."
  tabs "2026" "2025" "2024"
    region "2026"
      message "No disputes in 2026. The year has 3 months left. Fourteen were opened in 2025." neutral
```

**Rationale:** Where nothing can be done about it, the state says so and stops. An empty search with no results and nothing to adjust is a fact, not a failure.

**Cost:** Carbon says an empty state should offer the verb that fills it. A page that violates that is telling the reader they are stuck, which may be the truth.

**Reference:** Carbon

#### `an-empty-state-that-carries-the-count-it-should-have`: An Empty State That Carries the Count It Should Have

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "No expenses in March 2024."
  tabs "2026 · 268" "2025 · 1,204" "2024 · 0"
    region "2024"
      message "Nothing was claimed in March 2024. 2024 holds 0 expenses across all 12 months. 2025 holds 1,204." neutral
      action "Look at 2025"
```

**Rationale:** Where a filter is the only thing between the reader and the records, the empty state names the filter and the number behind it. The reader can go back one step.

**Cost:** The count needs a second read, and it puts a number on a page that otherwise shows none.

#### `one-line-in-place-of-the-rows-with-one-link-that-clears-ever`: One Line in Place of the Rows, with One Link That Clears Everything

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses, newest first."
    action "Export"
  toolbar
    field "Search expenses" search = "ava prins"
    field "Spender" select = "Harbor and Pine"
    field "State" select = "Paid"
  region "Expenses"
    message "No expenses match this search. Searching for ava prins in Harbor and Pine, Paid." neutral
    link "Clear the search and the filters"
```

**Rationale:** GitHub's issue list shows `No results matched your search` and a single link that clears the query, the filters, and the sort together. The toolbar keeps its values, so a reader who wants to change one thing still can.

**Cost:** It does not say how many records exist without the filters, so the reader cannot tell a narrow search from an empty list. Adding the count is the only change it needs.

**Reference:** GitHub

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

**Rationale:** Stripe's payments list keeps the column headers when a filter finds nothing. The filters name columns, and the reader is still reading the same table. The message sits in one full-width row.

**Cost:** Carbon replaces the table for a first-use empty state, and this differs from it on purpose. It is right for a filter result and wrong for first use, so the two states need two components.

**Reference:** Stripe

### Loading

**ID:** `litespec/frontend/patterns/feedback-and-states#loading`

**Question:** The page waits for its first read. The wait needs a shape that holds its place until the rows arrive.

**Default:** `skeletons-at-the-real-row-height`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** The pick stays on skeletons at the real row height. Nothing moves when the data lands, even on a slow phone read. The runner-up is the thin bar over the old rows. Prefer it for a re-read where the reader keeps their place, such as a changed filter. Never ship nothing at all. Any read slower than a blink reads as a failed page.

**Reviewed for:** Which built waiting state departs from the wireframe its catalog entry chose? Open the built skeletons, spinners, and progress bars beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** This is Carbon's answer for a first read. Each skeleton row is shaped like the row it will become, so nothing moves when the data lands.

**Cost:** Carbon also wants the header, toolbar, and pagination to match the row size. A skeleton page must get four heights right, or the page jumps.

**Reference:** Carbon

#### `a-spinner-in-the-control-that-is-waiting`: A Spinner in the Control That Is Waiting

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses. Searching."
  toolbar
    field "Search expenses" search = "ava"
    text muted "Searching"
  region "Expenses"
    table 5 "Expense" "Spender" "Filed" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 09:32" · status "Paid" positive · text "EUR 125.00"
  footer
    text "1 to 3 of 3"
    action quiet "‹ Previous"
    action quiet "Next ›"
```

**Rationale:** A control-level rule: a search that is running shows a spinner inside the search field, not over the page. The table stays as it was.

**Cost:** On the first load of a page there is no control to put it in. That is why Carbon asks for a skeleton there, and a spinner for a re-read.

**Reference:** Carbon

#### `a-spinner-in-the-submit-button-while-it-sends`: A Spinner in the Submit Button While It Sends

```wireframe
page
  breadcrumb "Home" "Policies" "New policy"
  header
    heading "New policy"
    text "Start with a name."
  region "New policy"
    field "Name" = "Travel policy"
    field "Office" select = "Rotterdam office"
    field "Available from" = "09/10/2026"
  footer
    action "Cancel"
    action primary disabled "Creating"
```

**Rationale:** A submit is disabled only while it sends. The spinner lives in the button, so the reader's eye is already there and nothing else on the page changes.

**Cost:** A button whose label is replaced by a spinner loses its width, so the layout shifts unless the button holds its size. It also says nothing about how long the wait will be.

#### `progress-for-work-that-takes-a-while`: Progress for Work That Takes a While

```wireframe
page
  breadcrumb "Home" "Statements" "September 2026"
  header
    heading "September 2026"
    text "Matching 1,204 expenses against the bank file."
  region "Reading the bank file"
    figure "Matched payouts" "842 of 1,204"
    text "70%. About 20 seconds left. Started 14:22."
  region "What is matched so far"
    text muted "Shown while it runs. Nothing here is final."
    fact "Matched" "842"
    fact "Unmatched" "11"
    fact "In the back office, not in the file" "351"
```

**Rationale:** Where the read is genuinely slow, the page shows a bar with a percentage and what is happening. This is the case Carbon's skeleton does not cover, because it is not a read.

**Cost:** A percentage is a claim. If the server cannot estimate the remaining work, the bar is theater, and a bar that stalls at 40% is worse than a spinner.

**Reference:** Carbon

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

**Rationale:** The page shows a scrim over the whole page with a spinner and a sentence. The message is unambiguous, and the page underneath is deliberately not readable.

**Cost:** A scrim over the page is neither a shared component nor a page slot. It hides the page a reader may want to scroll.

#### `nothing-at-all-briefly`: Nothing at All, Briefly

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every expense, newest first."
  toolbar
    field "Search expenses" search
  region "Expenses"
    table 5 "Expense" "Spender" "Filed" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 09:32" · status "Paid" positive · text "EUR 125.00"
  footer
    text "1 to 5 of 268"
    action quiet "‹ Previous"
    action quiet "Next ›"
```

**Rationale:** The page shows no placeholder. It renders as it will, and the content appears when it does. This option is correct only where the load is under about 200 milliseconds.

**Cost:** Anything slower reads as a page that failed. Every read needs a loading state, so this is the shape the rule refuses.

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

**Rationale:** Linear and Vercel keep what was on screen and show a thin bar while the next read runs. A reader changing a filter keeps their place, and nothing jumps when the rows arrive.

**Cost:** The rows on screen are briefly the old answer. A reader who acts on a row during that second acts on the old answer. Commands on a list that is reading must wait, or say so.

**Reference:** Linear, Vercel

#### `a-skeleton-that-admits-a-slow-read-after-a-few-seconds`: A Skeleton That Admits a Slow Read After a Few Seconds

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every expense, newest first."
  toolbar
    field "Search expenses" search
  region "Expenses"
    message "Still reading 1,204 expenses for September. This is taking longer than usual." neutral
    action quiet "Stop"
    image "Skeleton rows shaped like expense rows"
```

**Rationale:** Polaris and Material both time loading states: nothing for the first few hundred milliseconds, a skeleton after that, and words once the wait is long. After five seconds this one says what it is reading and offers to stop.

**Cost:** Three timed states per page is more to build and to test, and the five-second line only helps if the read can be canceled.

**Reference:** Shopify Polaris, Material

### Command the API Refused

**ID:** `litespec/frontend/patterns/feedback-and-states#command-the-api-refused`

**Question:** The API refused a command. The page must say what went wrong, why, and what the reader can do instead.

**Default:** `the-refusal-sentence-its-code-and-a-way-to-fix-it`

**Compact:** The recommended option reflows: one column with the header actions under the title.

**Rationale:** The pick stays on the refusal sentence with its code and fix. It meets WCAG 3.3.1 and 3.3.3 in one place. The runner-up is the refusal inside the dialog that sent it. Prefer it where closing would lose what the reader typed, such as a reversal amount. Never ship the refusal only on the record timeline. A reader who sent the command from elsewhere never returns to find it.

**Reviewed for:** Which built refusal notice departs from the wireframe its catalog entry chose? Open the built refused commands, with codes and fixes, beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The page uses one refusal shape: a title saying what is wrong, the detail saying why, and a `Why` link where the application can explain the refusal. The refusal sentence comes from the dictionary, not the component.

**Cost:** Each refusal code needs three pieces in two languages, and every new command that can be refused adds a code. The dictionary is the second copy of the domain rules.

#### `the-refusal-beside-the-button-that-caused-it`: The Refusal Beside the Button That Caused It

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "Expense lines"
    text "EXP-1042 · Ava · Harbor and Pine."
  region "Expense lines"
    field "Train fares" number = "20"
    message "Not enough budget. The March budget holds EUR 120. The expense claims EUR 200." critical
  footer
    action "Cancel"
    action primary "Save the lines"
```

**Rationale:** The page places the same refusal under the control that sent it. On a page with one form, the reader is already looking there, so the link between message and control is plain.

**Cost:** On a page with several forms, one refusal sits above the form and one sits under a button. The reader needs to learn which placement means what.

#### `the-refusal-with-a-code-for-support`: The Refusal with a Code, for Support

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · Harbor and Pine."
  message "The recall window has closed. This payout route reverses only within 180 days of the payout." critical
  region "Details, if you need to report this"
    text muted "Harbor and Pine sees this. The system records it against the expense."
    fact "Reference" "RF-EXP-1042-181"
    fact "When it was refused" "8 Oct 2026, 11:04"
    fact "Code" "payouts/reversal.window-closed"
    action quiet "Copy the code"
```

**Rationale:** The page shows the refusal plus the code and a copy control for the reader who needs to quote it. This is what a support conversation needs and what a normal reader does not.

**Cost:** A code on screen breaks the usual rule: do not show a raw code. Shown deliberately, it needs to be labeled and copyable rather than bare.

#### `a-refusal-that-changed-nothing-stated-plainly`: A Refusal That Changed Nothing, Stated Plainly

```wireframe
page
  breadcrumb "Home" "Policies" "Travel policy" "Status"
  header
    heading "Publish the policy"
    text "Travel policy is already published since 13 Jan 2026, 10:00."
  region "Status"
    message "Nothing to do. This publication went out on 13 Jan 2026 at 10:00, as scheduled." neutral
    action "See the publication"
```

**Rationale:** Not every refusal is a problem. Some commands are refused because nothing needed doing, and a neutral tone says so better than an error tone.

**Cost:** The reader cannot tell a no-op from a failure unless the tone differs, and the tone is the only signal. A refusal that was correct should not be red.

#### `a-refusal-with-nothing-lost-said-first`: A Refusal with Nothing Lost, Said First

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses. 3 selected."
  message "1 statement was resent. 2 were not. Ava and Elin Lindqvist opted out of messages. Nothing was lost." critical
  link "Why"
  toolbar
    field "Search expenses" search
  region "Expenses"
    table 4 "Expense" "Spender" "Result" "Paid"
      item: text "EXP-1042" · text "Ava" · status "Sent" positive · text "EUR 125.00"
```

**Rationale:** The refusal leads with what is intact, then what is wrong. NN/g's rule is that a message must not need reading twice. Naming the damage first is the difference between a refusal and a dead end.

**Cost:** The sentence order must be right per code. That is why the dictionary holds it, and a component does not.

**Reference:** NN/g

#### `the-refusal-on-the-record-it-happened-to-not-a-banner`: The Refusal on the Record It Happened to, Not a Banner

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Reversed" neutral
    text "Ava · Harbor and Pine."
    action primary "Message spender"
  region "The money"
    fact "Paid to the spender" "EUR 125.00"
    fact "Reversed" "EUR 45.00"
    fact "Still to settle" "EUR 80.00"
  region "What happened"
    timeline 3
      item: text "A second reversal was refused" · text muted "8 Oct 2026, 11:06" · text muted "Harbor and Pine"
```

**Rationale:** When one command on one record is refused, it goes into the record's own timeline, where the reader looks for what happened. It does not sit at the top of a page they are passing through.

**Cost:** The refusal is invisible elsewhere. A reader who sent the command from somewhere else will not come back to this page to find out why it did not happen.

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

**Rationale:** Stripe's refund dialog keeps a refusal inside the dialog, above the amount, and leaves the dialog open so the reader can change what was refused. Focus moves to the message, and the alternative is one button away.

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

**Rationale:** GitHub's merge box disables `Merge` and says which check blocks it. Where the page already knows the recall window has closed, it says so beside the button, and the refusal never needs to happen.

**Cost:** This option works only for refusals the page can predict from what it already read. The API still refuses, so the dictionary sentence and the button sentence must say the same thing.

**Reference:** GitHub

### Command That Worked

**ID:** `litespec/frontend/patterns/feedback-and-states#command-that-worked`

**Question:** A command worked. The page must show what changed, so the reader trusts the result without asking.

**Default:** `evidence-on-the-page-not-only-a-toast`

**Compact:** The recommended option reflows: one column with the header actions under the title.

**Rationale:** The pick stays on evidence on the page. State, money, and timeline all say what changed after a reversal. The runner-up is the toast with `Undo`. Prefer it for a reversible change such as hiding a policy from the portal. Never ship no message at all. It fails WCAG 4.1.3, since a screen reader user gets no signal.

**Reviewed for:** Which built success notice departs from the wireframe its catalog entry chose? Open the built confirmations, toasts, and quiet saves beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The record changes and the page says what changed: the state becomes `Reversed`, the money section shows the reversal, and the timeline gains an entry. That is evidence a toast cannot be.

**Cost:** The reader needs to find the evidence. A toast is faster and leaves nothing once it goes, so a toast alone is not evidence.

#### `a-toast-for-a-change-with-no-evidence-to-leave`: A Toast, for a Change with No Evidence to Leave

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    text "Ava · Harbor and Pine."
  region "References"
    text muted "Read them aloud on the call."
    list 2
      item: text "Expense number" · text "EXP-1042" · action quiet "Copy"
  toast "Bank reference copied." positive
```

**Rationale:** Where the command changed nothing visible, a toast is the whole answer. Copying a reference is the clearest case: the value was already on screen.

**Cost:** Material allows one action and no `Dismiss`, and Android warns the timer can take the action away. It is fine for a message with no action.

**Reference:** Material, Android

#### `a-success-notice-that-persists-for-a-change-in-progress`: A Success Notice That Persists, for a Change in Progress

```wireframe
page
  breadcrumb "Home" "Policies" "Travel policy"
  header
    heading "Travel policy"
    status "Active" positive
    text "Published 8 Oct 2026, 14:22. The portal shows it now."
    action "Edit"
  message "Reaching spenders now. The portal picked this up at 14:22. The 412 claimed lines are published." neutral
  region "What is published"
    list 1
      item: text "Travel policy" · text muted "EUR 45.00 · 412 claimed" · status "Active" positive
```

**Rationale:** Where the command succeeded and the effect is still arriving, the notice says so and stays until it does. A toast that goes would leave the reader unsure.

**Cost:** A success message that waits is a status, not an alert, so it must not take a tone. A notice that persists needs to resolve into something else.

#### `a-success-with-a-count-when-a-batch-partly-worked`: A Success with a Count, When a Batch Partly Worked

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "3 of 5 statements resent."
  message "2 were not sent. Those spenders opted out of messages already. Nothing was sent to them." caution
  region "Expenses"
    table 3 "Expense" "Spender" "Result"
      item: text "EXP-1042" · text "Ava" · status "Sent" positive
  footer
    action "Show the 2"
```

**Rationale:** A bulk command's success says what succeeded and what did not, in the same breath. '3 of 5 sent' is more useful than 'Sent'.

**Cost:** A count that is not expected raises a question the message then needs to answer. A partial success needs its own copy per command.

#### `no-message-because-the-record-says-it`: No Message, Because the Record Says It

```wireframe
page
  breadcrumb "Home" "Policies" "Travel policy"
  header
    heading "Travel policy"
    status "Paused" caution
    text "Paused 8 Oct 2026, 14:22. Nothing can be claimed at this ceiling."
    action primary "List it again"
  region "The product"
    fact "Claimed" "412 of 500"
    fact "Ceiling" "EUR 45.00"
    fact "State" "Paused"
    fact "Paused" "8 Oct 2026, 14:22, by Ben"
```

**Rationale:** The lightest answer: the command runs and the thing it changed is visibly different. Nothing announces anything, so there is nothing to read twice.

**Cost:** The pattern fails WCAG 4.1.3 unless the change is exposed as a status. A screen reader user gets no signal that the command worked.

**Reference:** WCAG

#### `a-success-and-a-next-step-where-the-flow-is-not-finished`: A Success and a Next Step, Where the Flow Is Not Finished

```wireframe
page
  breadcrumb "Home" "Policies" "Travel policy" "Publish"
  header
    heading "Travel policy is published"
    text "Published 8 Oct 2026, 14:22. The portal shows it now."
  region "Two things this product still needs"
    text muted "Published does not mean ready. Nothing can pay without a payout plan."
    list 2
      item: text "No payout plan" · text muted "Nothing can pay until there is a budget and one bank account" · action primary "Set payouts up"
  footer
    action primary "Open Travel policy"
```

**Rationale:** Where a command succeeded and the work is not done, the success names what is left rather than pretending it is finished. The published policy that still has no payout plan is the case.

**Cost:** It sits between a success and a checklist, so it needs its own place. A full outcome page already owns that place.

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

**Rationale:** Gmail and Linear confirm a reversible change with a toast that carries `Undo`. The list already shows the change. The toast adds the one thing the page cannot, a way back for a few seconds.

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

**Rationale:** Stripe's settings and Notion's pages say `Saved` beside the control, with the time, and nothing else moves. A settings form is the case: the values on screen are already the evidence.

**Cost:** The message is easy to miss, and a screen reader hears it only if it is a live status. It suits settings and not a command that changes money.

**Reference:** Stripe, Notion

### Fact Somebody Has to Read Before Acting

**ID:** `litespec/frontend/patterns/feedback-and-states#fact-somebody-has-to-read-before-acting`

**Question:** Some pages must state a fact before staff act, such as a low budget or a closing window. Only a command result is an alert; a static fact is a notice. A tone paints the object in that state, never a full band across other work.

**Default:** `a-neutral-notice-above-the-page`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep one neutral sentence above the page, with no tone and no band. It says why the page holds its state and stays out of the work. The runner-up is the side column, which suits an Harbor and Pine expense where the recall window changes the next step. Never ship a full-width toned band for one record.

A banner stays for facts on every page, such as `Sandbox`.

**Reviewed for:** Which built must-read notice departs from the wireframe its catalog entry chose? Open the built callouts, hints, and banners beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The page shows one neutral sentence about this page above the sections: not a band and not a tone. It says why the page is what it is.

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

**Rationale:** A fact about one section goes in that section, not at the top of the page. The reader only meets it where it changes what they do.

**Cost:** The reader needs to open the section to learn something that may change whether they should be in the section at all.

#### `a-callout-inside-a-form-about-what-the-form-will-do`: A Callout Inside a Form, About What the Form Will Do

```wireframe
page
  breadcrumb "Home" "Settings" "Late fee"
  header
    heading "Late fee"
    text "What the spender pays on top of the expense."
  region "The fee"
    field "Per expense, in euros" number = "6.00"
    message caution "At EUR 6.00 about 1,204 spenders claim EUR 7,224 a month."
    field "Waive the fee for members" checkbox
    text muted "Last month that meant EUR 310 across 62 expenses."
    row
      action "Cancel"
      action primary "Save"
```

**Rationale:** The page shows a callout inside the field group, in words, before the command runs. It states the consequence where the reader decides, without painting the page.

**Cost:** A callout inside a form is a paragraph among fields, and it is the least visible of the alert shapes. It is also the one a reader reads before committing rather than after.

#### `a-hint-under-the-field-with-no-tone-at-all`: A Hint Under the Field, with No Tone at All

```wireframe
page
  breadcrumb "Home" "Policies" "Travel policy" "Ceilings"
  header
    heading "Ceilings"
    text "What each line costs and what the spender pays on top."
  region "Ceilings"
    field "Standard ceiling, in euros" number = "45.00"
    text muted "45.00 with up to two decimal places."
    field "Late fee spenders pay, in euros" number = "5.00"
    text muted "Added at filing and kept by Harbor and Pine."
    text "A ceiling already claimed is kept. Only published lines take a new ceiling."
    row
      action "Cancel"
      action primary "Save"
```

**Rationale:** Some facts are not worth interrupting for. A hint under the field says the same thing in a quieter register, and the reader reads it because it is where they are looking.

**Cost:** A hint has no visual weight, so it is the first thing a reader skips. It works for a definition and not for a consequence.

#### `a-tool-tip-on-the-word-that-carries-the-fact`: A Tool Tip on the Word That Carries the Fact

```wireframe
page
  breadcrumb "Home" "Settings" "Plan"
  header
    heading "What the plan costs"
    text "Every charge on an account and what it is for."
  region "September 2026"
    fact "Business plan" "EUR 49.00"
    fact "Per-expense fee" "EUR 301.00"
    text muted "A per-expense fee is EUR 0.25 on 1,204 expenses. Spenders never pay it."
    fact "Billed to Harbor and Pine" "EUR 350.00"
    link "What the per-expense fee covers"
```

**Rationale:** The page shows a term hint beside a heading or a table note, where the term is the product's and the glossary entry explains it. Nothing is interrupted.

**Cost:** Use it only for a term that is genuinely the product's and genuinely unclear. On any other word it is noise, and every term needs a glossary entry to keep in step.

#### `the-fact-as-the-heading-instead-of-an-alert`: The Fact as the Heading, Instead of an Alert

```wireframe
page
  breadcrumb "Home" "Expenses" "Rotterdam client visit" "Cancel"
  header
    heading "Cancel Rotterdam client visit"
    text "Sat 14 Mar 2026, 08:00 to 12:00 at the Rotterdam client site."
  region "What cancellation does"
    text "This cannot be undone. No canceled payout ever returns."
    fact "Reversed" "EUR 54,180.00 to each account used"
    fact "Spenders" "Every spender learns the payout is off"
    fact "Staff" "Two people on the plan stand down"
    fact "Record" "The record stays readable in June"
    row
      action primary "Go back and keep it"
      action destructive "Yes, cancel it"
```

**Rationale:** Where the fact is what the page is for, it is the heading and description rather than an alert. There is no second thing to read, and the page title says the same thing.

**Cost:** The heading cannot carry a tone, so severity has nowhere to go. The description is one line, which is not much room for a consequence.

#### `the-shipped-banner-for-a-fact-about-the-whole-console`: The Shipped Banner, for a Fact About the Whole Console

```wireframe
page
  message info "Sandbox, nothing is paid."
  breadcrumb "Home" "Policies"
  header
    heading "Policies"
    text "Every policy Harbor and Pine pays, newest first."
    action primary "New policy"
  region "Policies"
    list 2
      item: text "Travel policy" · text muted "Code TRAVEL-01, EUR 45.00" · status "Active" positive · text "412 claimed"
```

**Rationale:** The page uses a banner in GitHub's and Stripe's test-mode shape. It shows one centered sentence across the top of every page for a fact that holds on all of them. `Sandbox` is the case it exists for, in the `info` tone.

**Cost:** A band is the loudest place on the screen. When used for a fact about one policy, it breaks the rule above, so it needs a short list of what may go there.

**Reference:** GitHub, Stripe

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

**Rationale:** Stripe's payment page puts risk and dispute facts in a right-hand column beside the payment, and GitHub does the same for a pull request. The fact is next to the record it is about, and the main column stays for the work.

**Cost:** A side column drops below the record on a phone, so the fact is read last there. It needs a heading that says why it is there.

**Reference:** Stripe, GitHub

### Reader May Not Do This

**ID:** `litespec/frontend/patterns/feedback-and-states#reader-may-not-do-this`

**Question:** Staff meet pages their role cannot use, so each such page must show the reason and the grant that would help. A back office with several roles meets this state on most pages, so it needs one shape.

**Default:** `the-commands-are-refused-each-with-its-grant-named`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the explained action. Each refused command names the role that grants it, and the record stays readable. On a call reading the record is the point. The runner-up is the read-only section with values as text, which suits a fee form where a form that fails on `Save` hurts more.

Never refuse the whole page to a support agent. Hiding a spender name needed on a call costs more than the secrecy gains.

**Reviewed for:** Which built forbidden view departs from the wireframe its catalog entry chose? Open the built refused pages, absent tabs, and read-only sections beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** This is the explained action: the button is refused and the reason is visible text beside it, naming the role that can. The record stays readable, which on a call is the point.

**Cost:** A page of refused commands looks broken to someone who does not know their role. The role's name in the reason is what makes it legible.

#### `the-page-itself-is-refused`: The Page Itself Is Refused

```wireframe
page
  breadcrumb "Home" "Statements"
  header
    heading "Statements"
    text "Settlements for every month."
  region "No access"
    heading small "Statements stay hidden from support staff"
    text "Eli is support staff. Settlements concern money, so Finance and Owners read them."
    action "Open the budget records instead"
```

**Rationale:** The whole page is out of reach. A not-permitted state replaces the body with the reason and the grant, and nothing of the record is shown.

**Cost:** Nothing at all is shown, so a support agent cannot read a spender name to answer a question on a call. That is a real cost on the surface where names matter.

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

**Cost:** The reader cannot find out the capability exists. On a surface where someone is guessing at what the product can do, that is a real cost.

#### `a-banner-naming-what-this-role-cannot-do-once`: A Banner Naming What This Role Cannot Do, Once

```wireframe
page
  message neutral "Read-only for support. Answering a bank case needs Finance."
  breadcrumb "Home" "Payouts" "Disputes"
  header
    heading "Disputes"
    text "5 open. The nearest closes in 19 days."
  region "Cases"
    table 3 "Case" "Expense" "Closes"
      item: text "D-0182" · text "EXP-1037" · text "19 days"
    action disabled "Answer"
    message neutral "Answering a bank case needs the Finance role."
```

**Rationale:** One neutral band on the area says what the role may not do there, rather than a refused button beside every command. It is stated once, in words, at the top.

**Cost:** It repeats on every page of the area, and the reader's eye skips it after the first. A tone may not paint a full-width band on a page about other work.

#### `the-names-are-hidden-the-figures-are-not`: The Names Are Hidden, the Figures Are Not

```wireframe
page
  breadcrumb "Home" "Statements" "September 2026"
  header
    heading "September 2026"
    text "Every euro of the month under the kind it is."
  message neutral "Spender names stay hidden for this role. Every amount stays visible."
  region "The month"
    fact "Claimed, 1,204 expenses" "EUR 54,180.00"
    fact "Late fees, 1,204 expenses" "EUR 6,020.00"
    fact "Meals, 126 lines" "EUR 3,150.00"
    fact "Total income" "EUR 63,350.00"
    fact "Bank fee on 1,204 payouts" "EUR -1,203.65"
    fact "Net for Harbor and Pine" "EUR 62,146.35"
```

**Rationale:** Where the record itself is sensitive but its shape is not, the spender is withheld and the money is shown. A finance reader gets the reconciliation without the personal data.

**Cost:** A screen reader reads whatever is in the `DOM`, so anything hidden this way needs to be absent rather than blurred. Blur alone is not a boundary.

#### `the-role-is-named-in-the-refusal-and-the-way-to-change-it`: The Role Is Named in the Refusal, and the Way to Change It

```wireframe
page
  breadcrumb "Home" "Expenses" "Reversals"
  header
    heading "Reversals"
    status "Read only" neutral
    text "31 expenses to decide. Eli is support staff."
    action "Ask an owner for the finance role"
  region "To decide"
    text muted "The Finance role lets a reader reverse. Dana grants it."
    list 3
      item: text "EXP-1040" · text muted "Elin Lindqvist asked 214 days ago" · status "No route" critical · text "EUR 45.00" · action disabled "Reverse"
```

**Rationale:** The refusal says which role the reader has, which role would be needed, and offers the command that asks for it. It tells everything a reader can be told without a support call.

**Cost:** A request that reaches an owner is a message someone reads, and a busy owner ignores it. It should not be the only route, so the page needs to stay useful read-only.

#### `the-page-refused-with-a-request-sent-to-a-named-person`: The Page Refused, with a Request Sent to a Named Person

```wireframe
page
  breadcrumb "Home" "Statements"
  header
    heading "Statements need the Finance role"
    text "Eli is support staff. Statements show money, so Finance and Owners open them."
  region "Ask Dana, the owner"
    field "Message" textarea = "Need the September statement for the office float."
    row
      action "Cancel"
      action primary "Send the request"
```

**Rationale:** This option follows Google Drive's `You need access` page. The reader asks for access in place, with a short message. The request goes to a named person rather than to an administrator.

**Cost:** A request is a message an owner answers, so it needs a place in the owner's queue. Without that it is a button that leads nowhere.

**Reference:** Google Drive

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

**Rationale:** Stripe shows settings as plain values to a role that reads but never changes them. One line at the top of the section names the role that edits. Nothing looks like a form that fails on `Save`.

**Cost:** The page looks different for different roles, so a support call about it needs to start with which role the caller has.

**Reference:** Stripe

### Connection Is Gone

**ID:** `litespec/frontend/patterns/feedback-and-states#connection-is-gone`

**Question:** Staff lose the connection while working, so the page on screen goes stale. The decision is what a stale page says: how fresh its data is, what still works, and when to retry.

**Default:** `a-banner-saying-what-is-stale-and-what-still-works`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep one band saying what is stale, when it was read, and what still works. A stale page the reader can read beats a page that shows nothing. The runner-up is the failed read state, which suits a page with nothing to show because the read itself failed.

Never ship a queued reversal as a quiet default. A reversal nobody confirmed is a liability, and the page says plainly that the money has not moved.

**Reviewed for:** Which built offline screen departs from the wireframe its catalog entry chose? Open the built stale banners and queued commands beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The page shows a neutral band naming when the page last read and which commands are refused while it is offline. The reader can still read, and knows it.

**Cost:** Every page needs a last-read time and a notion of which commands are safe offline. Commands that change money are almost never safe offline.

#### `read-only-with-every-command-refused-and-why`: Read-Only, with Every Command Refused and Why

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava of Harbor and Pine."
    action disabled "Message spender"
    message neutral "Needs the connection back."
  message critical "Nothing here reaches the server. The connection dropped 4 minutes ago."
  region "The spender"
    fact "Name" "Ava"
    fact "Email" "ava@harbor-pine.example"
    fact "Paid" "EUR 125.00"
```

**Rationale:** The page refuses every command with the same reason in words. The message is unambiguous, and a page of refusals that all say the same thing is noisy.

**Cost:** A reader on a train sees a page they cannot use. The refusals are truthful but frustrating, and the page cannot fix the connection.

#### `a-queued-command-so-the-reader-does-not-lose-the-work`: A Queued Command, so the Reader Does Not Lose the Work

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava of Harbor and Pine."
  message neutral "No connection. Two commands wait. Nothing reached the server and no money moved."
  region "Waiting to be sent"
    text muted "Held in this browser only. Clearing it loses them."
    list 2
      item: text "Reverse EUR 45.00" · text muted "Wanted at 11:02" · status "Waiting" neutral · action "Drop it"
  region "What is already true"
    fact "Paid to the spender" "EUR 125.00"
    fact "Reversed" "Nothing yet"
```

**Rationale:** A wanted command waits with a `Waiting` state, with what it does when the connection returns and a way to drop it. Nothing is lost, and nothing is claimed to have happened.

**Cost:** A queued reversal is a reversal nobody has confirmed, and a queue of them is a liability. The page needs to say plainly that the money has not moved.

#### `a-connection-control-in-the-header-always-present`: A Connection Control in the Header, Always Present

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    status "Offline" caution
    text "Read 4 minutes ago. The control stays on every page."
    action "Try again"
  region "Expenses"
    table 3 "Expense" "Spender" "Paid"
      item: text "EXP-1042" · text "Ava" · text "EUR 125.00"
```

**Rationale:** The connection is a fact about the session rather than about a page. It lives in the header and holds on every page. A refresh control already exists for the queue version of this.

**Cost:** A second status sits in a header that already carries the theme and the language. A header-level indicator is easy to learn and then easy to ignore.

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

**Cost:** A reader who needs to answer a question on a call gets nothing. The printed payout list exists precisely because a caller cannot be refused, which is why the two surfaces answer this differently.

#### `the-staleness-stated-on-the-values-themselves`: The Staleness Stated on the Values Themselves

```wireframe
page
  breadcrumb "Home" "Statements" "September 2026"
  header
    heading "September 2026"
    text "Every euro of the month under the kind it is."
  message neutral "Read 11 minutes ago. Each figure says when it was last true."
  region "The month"
    fact "Claimed, true at 14:22" "EUR 54,180.00"
    fact "Late fees, true at 14:22" "EUR 6,020.00"
    fact "Reversals, true at 14:18" "EUR -1,550.00"
    fact "Net for Harbor and Pine, mixed moments" "EUR 57,506.20"
```

**Rationale:** Each figure that moved carries when it was last true. A reader comparing two rows sees which is fresher.

**Cost:** A timestamp on every money cell makes a table unreadable. It works on a page with two or three figures and not on a list.

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

**Rationale:** The problem state for status `0`. Where a read fails because nothing answered, the section says the server could not be reached. Nothing claims to be current, because nothing is shown.

**Cost:** It only appears when a read is tried. A page that loaded before the connection dropped looks normal until the reader does something.

#### `a-small-bar-that-retries-by-itself-and-says-when`: A Small Bar That Retries by Itself and Says When

```wireframe
page
  message caution "Not connected. Trying again in 8 seconds. Read at 14:18."
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses, newest first."
    action "Try now"
  region "Expenses"
    table 3 "Expense" "Spender" "Paid"
      item: text "EXP-1042" · text "Ava" · text "EUR 125.00"
```

**Rationale:** Gmail shows one quiet retry line with a time and a `Try now` control. The page stays readable while the line says when it retries.

**Cost:** A countdown is movement on a page the reader is trying to read. It stops once it gives up, and it never counts down forever on a laptop that has gone to sleep.

**Reference:** Gmail

### Sign-In Ran Out

**ID:** `litespec/frontend/patterns/feedback-and-states#sign-in-ran-out`

**Question:** A session ends while the reader is working. Sign-in runs on the application's own origin, and a refused or ended session lands on `/signed-out`. The reader's unsent work is what this state must not lose.

**Default:** `a-dialog-over-the-work-which-stays-where-it-is`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the dialog over the work, which stays where it is. It keeps unsent work on screen and names the one thing to do. The runner-up is the warning before the end, which suits a reader still at the desk who never meets the expired state.

Never ship the toast alone. The message leaves in seconds and the typed work leaves with it.

**Reviewed for:** Which built expired session departs from the wireframe its catalog entry chose? Open the built sign-out pages and re-auth dialogs beside the catalog entry recording the chosen option and its reason.

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

**Cost:** The reader leaves their work to sign in again. Whether anything typed survives depends on the browser keeping the page, which is exactly what the other options make explicit.

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

**Rationale:** Stripe and Linear keep the work on screen behind a dialog. Signing in again returns to it, and nothing typed is lost.

**Cost:** A dialog over a form the reader cannot touch until they sign in. On a phone it fills the screen, which is acceptable because there is exactly one thing to do.

**Reference:** Stripe, Linear

#### `the-form-says-so-where-save-was`: The Form Says so Where Save Was

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "Trip"
  header
    heading "Trip"
    text "Sat 14 Mar 2026 at the Rotterdam client site."
  message neutral "The sign-in expired. Everything typed stays here. Sign in again and save."
  region "The trip"
    field "Name" text = "Rotterdam client visit"
    field "Arrival" text = "07:00"
    field "Notes" textarea = "Forty receipts for Harbor and Pine, batch A."
    text muted "Not saved yet."
    row
      action "Cancel"
      action primary "Sign in again to save"
```

**Rationale:** Notion keeps an unsent change in place and says what blocks it beside the control. The `Save` button becomes the way back, so the reader's eye is already where the answer is.

**Cost:** Only the form carries this message. A reader on a list page, with no `Save` button, meets the expiry somewhere else, so this needs a page-level sibling.

**Reference:** Notion

#### `warned-before-it-ends`: Warned Before It Ends

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses, newest first."
    action "Export"
  region "Expenses"
    table 3 "Expense" "Spender" "Paid"
      item: text "EXP-1042" · text "Ava" · text "EUR 125.00"
  toast caution "The sign-in ends in 5 minutes. Stay to keep working."
    action primary "Stay signed in"
```

**Rationale:** Dutch banks and DigiD warn before a session ends, with one button to stay. The reader who is still there never meets the expired state at all.

**Cost:** The warning is a countdown on a page the reader is trying to use. It appears only when the reader is idle enough to need it and active enough to answer it.

**Reference:** DigiD

#### `refused-as-a-command-worded-by-its-code`: Refused as a Command, Worded by Its Code

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "Trip"
  header
    heading "Trip"
    text "Sat 14 Mar 2026 at the Rotterdam client site."
  message critical "The sign-in expired. Sign in again."
  region "The trip"
    field "Name" text = "Rotterdam client visit"
    field "Arrival" text = "07:00"
    field "Notes" textarea = "Forty receipts for Harbor and Pine, batch A."
    row
      action "Cancel"
      action primary "Save"
```

**Rationale:** This option applies the refused-command answer to a session. The save is refused with the session sentence, in the same alert shape as every other refusal. It uses one component, one dictionary, and no special case.

**Cost:** The reader learns it only when they save. An expiry discovered at `Save` is later than one discovered at reading, and the typed work waits longer.

#### `a-toast-and-the-typed-work-goes-with-it`: A Toast, and the Typed Work Goes with It

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "Trip"
  header
    heading "Trip"
    text "Sat 14 Mar 2026 at the Rotterdam client site."
  region "The trip"
    field "Name" text = "Rotterdam client visit"
    field "Arrival" text = "07:00"
    field "Notes" textarea = "Forty receipts for Harbor and Pine, batch A."
    row
      action "Cancel"
      action disabled "Save"
  toast critical "The sign-in expired. What was typed is not saved."
```

**Rationale:** This is what happens with no design: the save fails, a toast says the session expired, and the toast goes in seconds. It is drawn here to be refused on purpose.

**Cost:** The message leaves and the typed notes leave with it. A reader who looked away loses work they cannot reconstruct.

### Somebody Else Saved First

**ID:** `litespec/frontend/patterns/feedback-and-states#somebody-else-saved-first`

**Question:** Two people edit one record. The API refuses the second save with `concurrency/record.changed-elsewhere`, worded as `Someone else changed this`. The reader's unsent edits are still on the page.

**Default:** `the-refusal-above-save-with-her-value-under-the-field`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the refusal above `Save`, with her value under the field. It refuses in the dictionary's words and fixes only what collided, with no dialog and no lost keystroke. The runner-up is the field-by-field choice, which suits a record where both people changed several fields.

Never ship `read-only-until-reload`. It deletes Ben's notes to resolve an arrival time.

**Reviewed for:** Which built conflict screen departs from the wireframe its catalog entry chose? Open the built field-by-field merges and presence cues beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** Linear refuses the save in words. The other person's value appears under the field it beats, with one button to take hers. The reader fixes exactly what collided.

**Cost:** This option works field by field. Where both people rewrote the same long notes, one button per field is not enough and the dialog option takes over.

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

**Rationale:** Contentful and Airtable resolve a conflict per field: each row shows both values and takes one. Nothing is reloaded wholesale, so nothing Ben typed is lost.

**Cost:** A row per field is a second form to build and to read. Past three fields it is longer than the form it argues about.

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

**Rationale:** Dropbox and Office ask on a conflict: keep mine, take theirs, or look at both. The dialog offers three buttons with no field surgery, and it says whose version is whose.

**Cost:** This is a whole-record choice. Ben's arrival time and his notes travel together, so he cannot keep one and take the other.

**Reference:** Dropbox, Office

#### `presence-first-so-the-conflict-never-happens`: Presence First, so the Conflict Never Happens

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "Trip"
  header
    heading "Trip"
    text "Sat 14 Mar 2026 at the Rotterdam client site."
  row
    avatar "PS"
    text "Cara edits this trip too."
  region "The trip"
    field "Name" text = "Rotterdam client visit"
    field "Arrival" text = "07:00"
    message info "Cara edits arrival now."
    field "Notes" textarea = "Forty receipts for Harbor and Pine, batch A."
    row
      action "Cancel"
      action primary "Save"
```

**Rationale:** Figma and Google Docs show who else is on the record before anyone saves. Ben sees Cara's avatar on the arrival field and waits or talks, and the refusal never needs to happen.

**Cost:** Presence is a live subscription per open record, and it only helps readers who look. The API still refuses, so this is early warning, not handling.

**Reference:** Figma, Google Docs

#### `merged-where-you-did-not-overlap`: Merged Where You Did Not Overlap

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "Trip"
  header
    heading "Trip"
    text "Sat 14 Mar 2026 at the Rotterdam client site."
  message neutral "One change of hers sits in this form. Cara fixed the batch at 09:34."
  region "The trip"
    field "Name" text = "Rotterdam client visit"
    field "Arrival" text = "07:00"
    message info "Cara saved 08:00 at 09:34."
    action quiet "Use hers"
    field "Notes" textarea = "Forty receipts for Harbor and Pine, batch A."
    row
      action "Cancel"
      action primary "Save"
```

**Rationale:** Git merges what does not overlap and asks only about the rest. The batch Cara fixed at 09:34 lands in Ben's form silently. Only the arrival time needs a choice.

**Cost:** The merge makes silent changes to a form the reader is editing. If Cara's batch fix surprises Ben at `Save`, the merge reads as tampering.

**Reference:** Git

#### `read-only-until-reload`: Read-Only Until Reload

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "Trip"
  header
    heading "Trip"
    text "Sat 14 Mar 2026 at the Rotterdam client site."
  message critical "Someone else changed this. Cara saved at 09:34. Reload her version."
  region "The trip"
    field "Name" text = "Rotterdam client visit"
    field "Arrival" text = "08:00"
    field "Notes" textarea = "Forty receipts for Harbor and Pine, batch A."
    action primary "Reload her version"
```

**Rationale:** This is the banking answer: the form locks, one button reloads Cara's version, and Ben's edits are dropped. It is drawn to be refused: nothing on this form is worth deleting notes over.

**Cost:** Ben's notes are gone. A lock that deletes work teaches the reader to type notes somewhere else first.

### Work That Runs Without You

**ID:** `litespec/frontend/patterns/feedback-and-states#work-that-runs-without-you`

**Question:** An import or a bulk reversal runs for minutes, longer than the reader waits on one page. The reader starts it and does other work. The job must stay visible while it runs and report its result when it is done.

**Default:** `an-activity-tray-like-an-upload-panel`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the activity tray, like an upload panel. It stays visible on every page, it never holds the reader, and it answers progress without being asked. The runner-up is the job page with the failures published.

It suits a bulk reversal where 2 of 31 need a person and the rest do not. Never ship the dialog that holds the reader. Minutes of a blocked page are the cost this pattern exists to remove.

**Reviewed for:** Which built background job departs from the wireframe its catalog entry chose? Open the built trays, job sections, and progress rows beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** Vercel, Linear, and Figma run uploads in a tray at the corner. It shows progress while the job runs, a line when it is done, and the page behind stays usable.

**Cost:** A corner tray holds two jobs well and five badly. Past that it needs the jobs section option, and the tray becomes its summary.

**Reference:** Vercel, Linear, Figma

#### `a-jobs-section-on-the-page-that-started-it`: A Jobs Section on the Page That Started It

```wireframe
page
  breadcrumb "Home" "Spenders" "Spender list"
  header
    heading "Spender list"
    text "850 records expected."
    action primary "Import records"
  region "Imports"
    text "spender-list.csv · 70%"
    text muted "Started 14:22 by Ben · 595 of 850 records"
    text "september-expenses.csv · Waiting"
    text muted "Starts when the import finishes"
    action "Stop"
    text muted "An import adds records. It never removes or changes the ones already here."
```

**Rationale:** Shopify's bulk operations and Stripe's dashboard keep jobs on the page that started them, each with its state and a way to stop it. The reader who started the import finds it where they stood.

**Cost:** The jobs live on one page. A reader who started an export and moved to reversals needs to go back to find it.

**Reference:** Shopify, Stripe

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

**Rationale:** GitHub Actions tells the reader when a run finishes. While it runs there is nothing to decide. A toast plus a timeline entry answers the only two questions: did it work, and where is the evidence.

**Cost:** The page is silent while the job runs. A reader who waits for 850 records with no progress bar waits less patiently than one who watches 70%.

**Reference:** GitHub Actions

#### `progress-on-the-rows-it-changes`: Progress on the Rows It Changes

```wireframe
page
  breadcrumb "Home" "Expenses" "Reversals"
  header
    heading "Reversals"
    text "Reversing 31 expenses · 12 of 31 done · about 2 minutes left."
  region
    table 4 "Expense" "Spender" "Result" "Reversed"
      item: text "EXP-1040" · text "Elin Lindqvist" · status "Sending" info · text "EUR 64.50"
```

**Rationale:** Mailchimp and Intercom show a bulk send on the rows it touches: each row gains its result as the job reaches it. The reader watches 31 reversals land one by one.

**Cost:** Thirty-one rows update under the reader's eye. The pattern needs the reader to stay on the page, which is exactly what background work should not need.

**Reference:** Mailchimp, Intercom

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

**Rationale:** Stripe's bulk runs and Shopify's imports end on a page that lists what failed and why, each with its retry. The 29 that worked need no attention. The 2 that did not get all of it.

**Cost:** A page per job is a place readers bookmark and support must explain. It earns its keep only for jobs that can partly fail.

**Reference:** Stripe, Shopify

#### `a-dialog-that-holds-the-reader-until-it-is-done`: A Dialog That Holds the Reader Until It Is Done

```wireframe
page
  breadcrumb "Home" "Spenders" "Spender list"
  header
    heading "Spender list"
    text "Importing."
  region
    list 2
      item: text "Ava" · text muted "ava@harbor-pine.example" · status "Imported" positive
  dialog
    heading "Importing 850 records"
    text "595 of 850 records · 70% · about 1 minute left"
    text muted "The page behind is waiting too."
    action "Stop the import"
```

**Rationale:** This is the old desktop answer: a progress dialog with nothing behind it clickable. It is drawn to be refused: 850 records take minutes, and minutes of a held reader are the cost this whole item exists to remove.

**Cost:** The reader cannot do other work while the job runs. `Stop` is the only choice, and it wastes the minutes already spent.

### This Record Does Not Exist

**ID:** `litespec/frontend/patterns/feedback-and-states#this-record-does-not-exist`

**Question:** A link names a record that is gone, mistyped, or at another company. A problem state words it as `Not found`. The reader needs a way back, not a code.

**Default:** `not-found-with-the-way-back`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep `Not found`, with the way back. It names the record, covers the three ways a record goes missing, and offers the list and the search. The runner-up is the record state.

It suits a section whose read failed rather than a page whose record is gone. Never ship the bare code. A code means nothing to a reader, and a dead end with a number on it is still a dead end.

**Reviewed for:** Which built missing-record screen departs from the wireframe its catalog entry chose? Open the built not-found and gone states beside the catalog entry recording the chosen option and its reason.

#### `the-shipped-record-state`: The Shipped Record State

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every expense, newest first."
  region
    heading "Not found"
    text "This record does not exist, or it belongs to another company."
```

**Rationale:** The problem state for a missing record: `Not found` and the line that covers gone, mistyped, and another company's. It offers no retry, because the read answered correctly. Nothing claims the record exists.

**Cost:** No retry and no way back: the record is not there, so there is nothing to retry. The way back in the recommended option is what the reader needs.

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

**Rationale:** Stripe's dashboard and Linear name the missing record, say what may have happened, and offer the list and the search. The reader typed one character wrong. The fix is one look away.

**Cost:** The suggestions are guesses. Where the record belongs to another company, no suggestion helps, and the copy must say so rather than keep guessing.

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

**Rationale:** GitHub shows a missing comment inside an intact thread. The expense exists. One line on it does not. Only the lines section says so.

**Cost:** Use it only for a child that is missing from a parent that exists. Where the whole record is gone, a surviving page is a lie.

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

**Rationale:** Linear and Notion name who removed a page and when, because `does not exist` for something the reader saw yesterday reads as a fault. The audit trail already knows. The page says it.

**Cost:** It confirms the record existed, which leaks across companies if the reader should not know. For another company's record this must fall back to the `not-found-with-the-way-back` option.

**Reference:** Linear, Notion

#### `search-from-the-dead-end`: Search from the Dead End

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1099"
  header
    heading "Expense EXP-1099 does not exist"
    text "Two expenses come close. Neither is called EXP-1099."
  region "Close matches"
    toolbar
      field "Search expenses" search = "EXP-1099"
    list 2
      item: text "EXP-1042 · Ava" · text muted "ava@harbor-pine.example" · action "Open"
```

**Rationale:** GitHub's `404` offers search where the reader stands. The reader who mistyped an expense number searches without going back to the list first.

**Cost:** A search box on an error page is a second search to keep in step with the list's. It earns its keep only where mistyped codes are common, which receipt codes are.

**Reference:** GitHub

#### `the-big-code`: The Big Code

```wireframe
page
  breadcrumb "Home"
  header
    icon "Not found"
    text muted "404"
    heading "This page does not exist"
    text "It may be typed wrong, removed, or at another company."
    action primary "Back to Home"
```

**Rationale:** Vercel and Next.js draw a bare `404`. It states the status and nothing else: not what is missing, why, or where to go.

**Cost:** A code means nothing to a reader. When left bare, it is a dead end with a number on it.

**Reference:** Vercel, Next.js

### Something Went Wrong on Our Side

**ID:** `litespec/frontend/patterns/feedback-and-states#something-went-wrong-on-our-side`

**Question:** The server fails a read or a command. A problem state words it as `That did not go through`, with a reference the reader can quote. The reader must know what did not happen.

**Default:** `the-shipped-failure-with-its-reference`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the failure with its reference. It words the failure once, says what did not move, and gives support something to read back. The runner-up is the outcome page.

It suits a failed command where the money sentence deserves a page of its own. Never ship the old data with its quiet line. Figures the server could not confirm are misinformation, and a quiet line does not make them honest.

**Reviewed for:** Which built server-failure screen departs from the wireframe its catalog entry chose? Open the built outage pages and retrying bars beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The problem state for a failed command: the title, the caller-safe detail, `Try again`, and a reference the reader can quote to support. The money sentence is the point: what did not move.

**Cost:** A reference is only useful if support can read it back. Without that lookup it is a number that comforts nobody.

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

**Rationale:** Stripe's dashboard failure page leads with what did not happen, because after a failed refund that is the reader's first question. The money sentence comes before the retry.

**Cost:** A page per failure is heavier than a state in place. It suits a failed command and not a failed read, where the inline state stays.

**Reference:** Stripe

#### `tried-again-by-itself-and-says-when`: Tried Again by Itself, and Says When

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  message "The statement was not resent. Trying again in 8 seconds." caution
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · Harbor and Pine"
    action "Try now"
  region "Documents"
    list 1
      item: text "Statement ST-2041" · text muted "ava@harbor-pine.example" · status "Paid" positive · action "Resend"
```

**Rationale:** Gmail retries a failed send and says when, with `Try now` for the impatient. A failure that clears on its own should not need a reader to clear it.

**Cost:** Retrying a reversal doubles it unless the command is idempotent. This suits reads and sends, and must never retry money blindly.

**Reference:** Gmail

#### `a-toast-for-a-background-command-that-failed`: A Toast for a Background Command That Failed

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
  toast critical "The statement for EXP-1042 was not resent. Retry from here."
```

**Rationale:** Gmail's failed send and Linear's failed sync follow this shape: the reader moved on, so the failure comes to them as a toast with its retry. The page behind is whatever they do now.

**Cost:** A toast goes. A failed reversal must also land in the record's timeline, or the evidence leaves with the message.

**Reference:** Gmail, Linear

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

**Rationale:** React error boundaries and Vercel's per-section recovery follow this shape: the money section fails into its retry while the spender section stays readable. One failed read does not take the page.

**Cost:** Two sections can fail two different ways on one page. Past one failure the page is a list of retries, and the outcome page takes over.

**Reference:** React, Vercel

#### `the-old-data-with-a-quiet-line`: The Old Data with a Quiet Line

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
    text muted "These figures may be out of date. The last read failed at 14:22."
    link "Try again"
```

**Rationale:** This is what happens when a failed re-read is swallowed: the old figures stay with a quiet line. It is drawn to be refused: a reversal total that may be wrong is worse than none.

**Cost:** The reader acts on figures the server could not confirm. For money this is not a degradation. It is misinformation.

### Back Office Is Down for a Short While

**ID:** `litespec/frontend/patterns/feedback-and-states#back-office-is-down-for-a-short-while`

**Question:** A planned window stops the back office. Finance keeps its printed payout lists, but the payout desk checks every receipt against the system, so payouts wait too. The reader needs the window, what still works, and when to come back.

**Default:** `told-ahead-of-time-on-every-page`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the advance notice on every page. A window nobody meets by surprise is a window finance can plan around, and the printed payout lists are the plan. The runner-up is the maintenance page itself.

It is what every page becomes while the window runs. Never ship the message alone as the only signal. A Thursday message tells, and only the back office reminds on Sunday at 02:00.

**Reviewed for:** Which built maintenance screen departs from the wireframe its catalog entry chose? Open the built countdowns and read-only windows beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** Stripe's status banner and Linear's scheduled maintenance follow this shape: one sentence on every page from days before, with the window and what still works. Nobody meets maintenance by surprise.

**Cost:** A band on every page for days is days of noise for a 30-minute window. It must appear late enough to matter and early enough to plan finance work around.

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

**Rationale:** GitHub and Vercel draw the window as a page: what is happening, when it ends, and what still works. During the window this is every page.

**Cost:** Nothing can be read behind it, including the payout lists. Anything finance needs must be printed before 02:00.

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

**Rationale:** Shopify and Notion degrade to reading when writing is unsafe. Expenses can be read on a call. Nothing that changes money is offered.

**Cost:** Read-only needs every command to know it is refused and why. A page that forgets one offers a reversal that fails at 02:15.

**Reference:** Shopify, Notion

#### `a-countdown-with-what-still-works`: A Countdown with What Still Works

```wireframe
page
  breadcrumb "Home"
  header
    heading "Back soon"
    text "The back office is down for maintenance until 02:30."
    text muted "Harbor and Pine"
  card
    row
      figure "Back in" "42 min"
      figure "Printed payout lists" "Valid"
      figure "Payouts" "Waiting"
    action primary "Check again"
```

**Rationale:** Cloudflare's waiting room counts down with the facts beside it. The reader watches one number fall instead of wondering whether to keep waiting.

**Cost:** A countdown that passes 02:30 with the system still down breaks trust faster than no countdown. It must switch to words the moment the window does.

**Reference:** Cloudflare

#### `the-window-as-a-calendar-fact-on-home`: The Window as a Calendar Fact on Home

```wireframe
page
  breadcrumb "Home"
  header
    heading "Good morning, Ben"
    text "Harbor and Pine · two days that need you."
  region "This week"
    fact "Rotterdam client visit" "Sat 14 Mar · 08:00 to 12:00"
    action "Open the payout plan"
    fact "Maintenance, Sun 02:00 to 02:30" "Print the payout lists before you leave on Saturday."
    action "What still works"
```

**Rationale:** Incident.io and Linear put a planned window on Home days ahead, next to the work it touches. The payout plan for the day carries the note, not a banner.

**Cost:** Home is one page. A reader who lives in Expenses never sees it, which is why this pairs with the banner option and never replaces it.

**Reference:** Incident.io, Linear

#### `a-message-beforehand-nothing-on-screen`: A Message Beforehand, Nothing on Screen

```wireframe
page
  breadcrumb "Home" "Settings" "Messages"
  header
    heading "Messages to owners"
    text "What the system sent, and what the back office showed that night."
  region
    list 1
      item: text "The back office is unavailable Sun 15 Mar, 02:00 to 02:30" · text muted "Sent Thu 12 Mar to Dana and Ben" · status "Sent" neutral
  region "On the back office that night"
    heading small "Nothing"
    text "No banner, no page, no countdown. Finance finds out at 02:00."
```

**Rationale:** This is what happens when maintenance is announced by message only: owners get the mail on Thursday and finance meets the window on Sunday. It is drawn to be refused as the only signal.

**Cost:** Nobody reads a Thursday message on Sunday at 02:00. A message tells. Only the back office can remind.

### Too Many Tries

**ID:** `litespec/frontend/patterns/feedback-and-states#too-many-tries`

**Question:** The reader hits a limit: too many exports, too many sign-in tries, or too many sends. The API answers `429` with when to try again. The page must state the wait and the reason, briefly.

**Default:** `the-button-waits-and-says-how-long`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the button that waits and says how long. It turns the limit into a 40-second wait with its reason attached, which is all a reader who hit 12 exports needs. The runner-up is the limit stated before it hits.

It suits a reader planning a month of exports rather than one too many. Never ship the spinner that never says why. A wait the reader cannot name reads as a page that broke, and reloading restarts it.

**Reviewed for:** Which built rate-limit screen departs from the wireframe its catalog entry chose? Open the built waits, queues, and 429 pages beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** GitHub and Stripe answer `429` with `Retry-After`, and the button counts it down. The reader waits 40 seconds knowing why, instead of clicking a button that keeps refusing.

**Cost:** A countdown on a button is a timer the page must keep honestly. If the reader reloads, the count must survive or start over truthfully.

**Reference:** GitHub, Stripe

#### `sign-in-tries-counted-down`: Sign-In Tries, Counted Down

```wireframe
page
  breadcrumb "Home" "Sign in"
  header
    heading "Sign in"
    text "Harbor and Pine · Ben, finance manager"
  card
    field "Email" text = "ben@harbor-pine.example"
    text muted "3 tries left. After that this sign-in waits 10 minutes."
    action primary "Continue"
```

**Rationale:** DigiD and Dutch banks count sign-in tries down and name the wait at zero. The reader who mistyped twice knows exactly where they stand.

**Cost:** Counting tries tells an attacker the same thing. The wait at zero must be real, and the count must not leak whether the account exists.

**Reference:** DigiD

#### `queued-behind-the-others`: Queued Behind the Others

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses, newest first."
  region "Exports"
    list 1
      item: text "september-expenses.csv" · text muted "3rd in line · about 6 minutes" · text "Queued" · action "Leave the queue"
    text muted "Your export runs when the 2 ahead of it finish. Leaving the queue cancels it."
  region
    table 3 "Expense" "Spender" "Filed" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 2026, 09:32" · status "Paid" positive · text "EUR 125.00"
```

**Rationale:** Cloudflare's waiting room queues rather than refuses: the export is 3rd in line with its wait stated. The reader's work is held, not lost.

**Cost:** A queue is a promise of order. If an export jumps it, or the wait restarts, the queue reads as a trick.

**Reference:** Cloudflare

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

**Rationale:** Stripe's API dashboard and Linear's limits page show usage against the ceiling before anything refuses. The reader who sees 9 of 12 plans the last 3.

**Cost:** Usage figures are a read of their own, and a stale one refuses a reader it told there was room. The figure and the enforcement must agree.

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

**Rationale:** Cloudflare answers a hard stop with a page: too many tries, when to come back, and a reference. There is no retry button, because retrying is what caused this.

**Cost:** The page offers nothing to do but wait. It suits a stop measured in minutes and insults one measured in seconds, which the button option handles.

**Reference:** Cloudflare

#### `a-spinner-that-never-says-why`: A Spinner That Never Says Why

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "268 expenses. Searching."
  toolbar
    field "Search expenses" search = "ava"
    text muted "Searching"
  region
    table 3 "Expense" "Spender" "Filed" "State" "Paid"
      item: text "EXP-1042" · text "Ava" · text "8 Oct 2026" · status "Paid" positive · text "EUR 125.00"
    text muted "1 to 25 of 268"
```

**Rationale:** This is what happens when the `429` is swallowed into a loading state: the search spins with no message. It is drawn to be refused: a wait the reader cannot name reads as a page that broke.

**Cost:** The spinner is indistinguishable from a hang. Every second past the first is a second the reader spends deciding whether to reload, which restarts the wait.