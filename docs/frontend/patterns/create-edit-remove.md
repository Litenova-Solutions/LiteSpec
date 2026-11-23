# Create, Edit and Remove Patterns

**Stack:** any

**Concern:** frontend-ui

Staff create records such as expenses, trips, and fares, change them, and remove one or several at a time.

Each pattern below covers one such task with a set of layout options and a named default to build. The product owner picks among the options when the team builds or revises a screen. One decided shape per task keeps destructive and far-reaching commands predictable, so staff know what a command will do before they run it. This page belongs to the Forms and Input category. Its patterns are tested layouts a team chooses from, not required rules.

## Concepts

### How to Read a Pattern

A pattern answers one recurring question about working with records, such as how many inputs keep a create in a dialog.

Each pattern offers possible layouts for the task as options, each drawn as a wireframe, a plain-text sketch of the layout with the copy the product would show. One option is the default, the layout the project builds unless its catalog records another.

Each pattern says how its default behaves on a narrow screen and why it is the default. Each option carries a rationale, what the option is good for, and a cost, what it gives up. Some options name an outside product they learn from as a reference.

The project records its choice for each pattern in its pattern catalog and builds the chosen option with its own components. The [catalog contract and the wireframe language](../patterns.md) state what each catalog entry holds and how to read a drawing.

### The Running Example

The drawings all use Harbor and Pine travel: trips such as Rotterdam client visit, expenses such as EXP-1042 for Ava, and fares such as the train fare at EUR 48.90. The reader of each screen is the staff member using it.

Two defaults hold on every option unless the project records another choice, and the Conventions section below states them: each removal offers undo or asks for confirmation, and a successful create shows the created record. A command in flight refuses a second send under `litespec/frontend/components#render-control-states`.

## Conventions

### Confirm or Undo Every Removal

**ID:** `litespec/frontend/patterns/create-edit-remove#confirm-or-undo-every-removal`

**Strength:** default

**Default:** Offer undo for every removal, or ask for confirmation before it runs.

**Replacement:** A project that replaces the default records which removals run with neither undo nor confirmation and why.

**Rationale:** Staff remove the wrong record by misreading a row or misclicking, and without a way back the record is gone. An undo toast restores a reversible removal in one click, while a confirmation dialog makes an irreversible removal a deliberate choice. Either way, the reader gets a moment to catch the mistake before it becomes permanent.

**Reviewed for:** Does every built removal ask first or offer a way back? Open the archive, delete, void, and suspend flows on built screens.

### Show the Created Record

**ID:** `litespec/frontend/patterns/create-edit-remove#show-the-created-record`

**Strength:** default

**Default:** Take the reader to the created record after a successful create, or show it in place.

**Replacement:** A project that replaces the default records where its creates end instead.

**Rationale:** After creating, the reader needs proof that the record exists with the values entered, and the created record itself is that proof. A create that ends on an empty form or a bare toast leaves the reader unsure whether it worked, so they create it again and produce a duplicate.

**Reviewed for:** Does every built create flow end at its new record? Open the create dialogs, pages, and bulk flows on built screens.

## Patterns

### Creating One Record

**ID:** `litespec/frontend/patterns/create-edit-remove#creating-one-record`

**Strength:** default

**Question:** Staff create records such as trips, reversals, and fares. A create with one to four inputs fits a dialog, and a longer one needs a page; a drawer never holds a create. The question is which shape each create takes: a dialog for two or three inputs, a full page for the ones that grow, or a file import for many at once.

**Default:** `a-dialog-for-two-or-three-inputs`

**Compact:** On a narrow screen the dialog opens as a full-width sheet under the header, and its fields stack in one column.

**Rationale:** One to four inputs belong in the dialog: the list stays visible behind it, and a single verb commits the create. Past four inputs, or when the form needs sections, the full page is the runner-up and the right choice. The file import is the other alternative, for the days when many records arrive at once and a dialog per record would take too long. Never ship a form inside a table row: it breaks the table structure and hides validation.

**Reviewed for:** Does every built creation form match the wireframe its catalog entry chose? Open the built dialog, page, and file-import starts beside the catalog entry recording the chosen option and its reason.

#### `a-dialog-for-two-or-three-inputs`: A Dialog for Two or Three Inputs

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "Trip" "Plan"
  header
    heading "Rotterdam client visit"
    text "Trip plan, 6 entries."
    action "Add to the plan"
  region "Trip plan"
    list 3
      item: text "Hotel booked" · text muted "08:00" · text muted "Eli"
  dialog "Add to the trip plan"
    field "What" text
    field "When" text = "06:30"
    field "Who" text
    action "Cancel"
    action primary "Add it"
```

**Rationale:** The dialog holds three inputs, a single verb on the commit button, and Cancel beside it. This is where most creates belong and where they usually end up.

**Cost:** Past four inputs the create outgrows the dialog, and moving it to a page either carries the typed values along or loses them.

#### `a-full-page-for-the-ones-that-grow`: A Full Page for the Ones That Grow

```wireframe
page
  breadcrumb "Home" "Expenses" "New trip"
  header
    heading "New trip"
    text "Start with a name. Everything else can wait."
  region "The trip"
    field "Name" text
    field "Description" textarea
  region "When"
    field "Outbound" text
    field "Return" text
  region "Where"
    field "Site" select = "Rotterdam client site"
    action "Cancel"
    action primary "Create trip"
```

**Rationale:** Forms with sections, steps, or more than four inputs get a page of their own, where the fields have room and the result gets its own link.

**Cost:** Each creation costs two navigations, away from the list and back. That cost is acceptable only when the form is long.

#### `create-many-at-once-from-a-file`: Create Many at Once from a File

```wireframe
page
  breadcrumb "Home" "Settings" "Data imports" "New"
  header
    heading "Import expenses"
    text "Bring in a list you already hold. Nothing is published until you check it."
  region "Your file"
    field "Drop a CSV here or choose a file" file
    text muted "Up to 5 MB, 2,000 rows. UTF-8, comma separated."
    link "Download a template"
  region "What will happen"
    text "One expense per row, as a draft. Nothing is paid and nothing is sent."
    action "Cancel"
    action primary "Check the file"
```

**Rationale:** A bulk create runs in three steps: the file, a check of what it made, and a report. That fits a trip of expenses or a list of spenders, where a dialog per record would take too long.

**Cost:** One import writes across many records at once, so a partial failure has to stay reportable and undoable, while nothing is paid and nothing is sent.

### Changing a Record That Already Exists

**ID:** `litespec/frontend/patterns/create-edit-remove#changing-a-record-that-already-exists`

**Strength:** default

**Question:** Staff change records that already exist, from renaming a trip to raising a budget ceiling. The hazard is the reader who edits one section and saves another, believing both are saved. The question is how the edit commits: one save for the whole page, the impact of a breaking change stated before the save, or an autosave with no save button at all.

**Default:** `one-save-per-page`

**Compact:** On a narrow screen the fields stack in one column, with the unsaved note and Save staying together under them.

**Rationale:** The default is one Save for the whole page: a reader who changed three things saves once, and an unsaved note says what is still pending. The impact statement is the runner-up, for changes that break something. Autosave is the other alternative, and it earns its place only for free text, beside the version history in the Changing Something That Has Already Happened pattern. Never ship the inline list-row edit on this wide table: it hides validation.

**Reviewed for:** Does every built edit flow match the wireframe its catalog entry chose? Open the built save, impact-first, and autosave edits beside the catalog entry recording the chosen option and its reason.

#### `one-save-per-page`: One Save per Page

```wireframe
page
  breadcrumb "Home" "Policies" "Travel" "Travel policy"
  header
    heading "Travel policy"
    status "Active" positive
    text "What this policy is, and the coding rule its lines carry."
  region "Edits"
    message "Two changes not saved. The portal still shows the old ones." info
    field "Name" text = "Travel policy"
    field "Description" textarea = "Train, hotel and meals inside the trip ceiling."
    field "Coding" radio = "Batch code"
    action "Discard both"
    action primary "Save 2 changes"
```

**Rationale:** The whole page is one form with one Save, so a reader who changed the name, the description, and the coding saves all three at once. Every part of the page shows the same pending changes.

**Cost:** A partial failure loses every change on the page, and there is no way to save the name while leaving the dates alone. The page has to list what changed while edits are unsaved.

#### `editing-with-the-impact-stated-before-the-save`: Editing with the Impact Stated Before the Save

```wireframe
page
  breadcrumb "Home" "Policies" "Travel policy" "Budget"
  header
    heading "Raise the budget ceiling"
    text "Amsterdam sales holds EUR 10,000. EUR 8,240 is committed."
  region "New target"
    field "Ceiling" number = "12000"
    message "Two things happen when you save this. EUR 3,760 more can be claimed at EUR 45.00 a line." caution
    text "Published on the portal within a minute. No way to take it back once claimed."
  region "Nothing here changes"
    fact "Committed" "EUR 8,240"
    fact "Expenses" "96 unchanged"
    fact "Claimed lines" "Stay at EUR 45.00"
    action "Cancel"
    action primary "Raise it and release EUR 3,760"
```

**Rationale:** Where a change breaks something, the page says what breaks before the reader commits: who is affected, what stops working, and what cannot be changed back. The reader raising the ceiling sees the EUR 3,760 it releases before pressing Save.

**Cost:** The page has to answer a hypothetical, what would change, which is neither the record as it stands nor a command result. That preview needs its own read.

#### `autosave-with-the-saved-state-always-visible`: Autosave, with the Saved State Always Visible

```wireframe
page
  breadcrumb "Home" "Policies" "Travel" "Travel policy"
  header
    heading "Travel policy"
    status "Active" positive
    text "Saved 14:22."
    action "History"
  region "Notice"
    message "Every change saves as you type. The portal reads the saved version within a minute." info
  region "The policy"
    field "Name" text = "Travel policy"
    field "Description" textarea = "Train, hotel and meals inside the trip ceiling."
    field "Coding" radio = "Batch code"
```

**Rationale:** There is no Save button: every keystroke saves, and the header always says when the last save happened. A description edited over an afternoon cannot be lost to a closed tab.

**Cost:** Two people typing at once overwrite each other without noticing, and every keystroke becomes an audit entry unless the audit trail groups them. Autosave fits free text, not ceilings or counts.

**Reference:** Notion

### Removing Something

**ID:** `litespec/frontend/patterns/create-edit-remove#removing-something`

**Strength:** default

**Question:** Staff remove records that should no longer be active. Archive is preferred over delete because it is reversible and keeps the audit trail. The question is which kind of removal each record gets: archive, a thirty-day trash, or a delete that cannot be undone.

**Default:** `archive-with-what-archiving-means`

**Compact:** On a narrow screen the scheduled and archived lists stack in one column, with the search above them and the row menu on each row.

**Rationale:** Archive is the default: it is reversible, keeps the audit trail, and the Archived view is the way back. The thirty-day trash is the runner-up where readers expect the word delete. The delete dialog is the other alternative for records that have to go, and it says what is kept. Never ship a bare delete for anything with history: the dialog must name what survives.

**Reviewed for:** Does every built single removal match the wireframe its catalog entry chose? Open the built archive, delete, and trash flows beside the catalog entry recording the chosen option and its reason.

#### `archive-with-what-archiving-means`: Archive, with What Archiving Means

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Trips"
    text "93 scheduled, 4 archived."
    action "New trip"
  region "Find"
    toolbar
      field "Search trips" search
  region "Scheduled"
    list 3
      item: text "Rotterdam client visit" · text muted "9 Oct 2026" · status "Scheduled" info · action "Open"
  region "Archived"
    list 1
      item: text "Okafor Office visit" · text muted "28 Feb 2026" · status "Archived" neutral · action "Restore"
  menu "More actions for Rotterdam client visit"
    action "Open"
    action "Duplicate this trip"
    action destructive "Archive this trip"
```

**Rationale:** Archiving hides the trip from the scheduled list without deleting it. The dialog says what stops being visible and what is kept, and the record keeps its place with its state set to archived.

**Cost:** Readers end up with two ways to hide something, archive and delete. A list that holds both needs a view or a filter to separate them.

#### `trash-with-thirty-days-to-change-your-mind`: Trash with Thirty Days to Change Your Mind

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Trips"
    text "93 scheduled, 3 in the trash."
    action destructive "Empty the trash"
  region "Find"
    toolbar
      field "Search trips" search
  region "Notice"
    message "The trash empties itself after 30 days. Filed expenses are never in it." info
  region "Trash"
    list 3
      item: text "Okafor Office visit" · text muted "Deletes 7 Nov 2026" · action "Restore" · action destructive "Delete now"
```

**Rationale:** Delete moves the record to a trash that empties itself after thirty days. The verb stays delete, the word readers expect, and the removal stays reversible until the trash empties.

**Cost:** There is still a day when the record is removed permanently. The trash needs its own view, its own emptying rule, and an answer for records that carry filed expenses.

**Reference:** Google Drive

#### `delete-where-nothing-may-be-archived`: Delete, Where Nothing May Be Archived

```wireframe
page
  breadcrumb "Home" "Expenses" "Rotterdam client visit" "Incidents" "INC-0207"
  header
    heading "Incident INC-0207"
    text "14 Mar 2026, front desk."
    action destructive "Delete"
  region "What was recorded"
    fact "Who" "A courier, no name given"
    fact "When" "14 Mar 2026, 08:41"
    fact "Who decided" "Eli"
    fact "Why" "The original could not be produced, so nothing was handed over"
  dialog "Delete this incident?"
    text "The incident record is removed. The payout log keeps the decision and the time."
    message "This cannot be undone. Nothing about the decision is lost." critical
    action "Keep it"
    action destructive "Delete the record"
```

**Rationale:** Some records have to go rather than be hidden, such as incident INC-0207. The dialog says the record is removed permanently, and it names what survives: the payout log still holds the decision Eli made and the time it was made.

**Cost:** Everything the company holds has to stay reconstructable for an audit, so `delete` removes the operational record while keeping the evidence. One verb covers two different things, and the dialog has to explain which is which.

### Removing Several at Once

**ID:** `litespec/frontend/patterns/create-edit-remove#removing-several-at-once`

**Strength:** default

**Question:** Staff remove or reverse several records at once. The count itself is the risk, so the reader checks how many records and how much money the selection holds before committing. The question is how the confirmation carries that check for small and large selections.

**Default:** `a-dialog-naming-the-count-and-the-money`

**Compact:** On a narrow screen the dialog opens as a full-width sheet, with the count and the money stacked above the commit.

**Rationale:** Up to about twenty-five rows, the default is the dialog that names the count and the money, because those two figures are what the reader checks. Past that size the background job is the runner-up, and a report arrives afterwards. The dry-run plan is the other alternative where rows can refuse, because a page shows every refusal before anything runs. Never ship a typed count for an ordinary bulk remove: typing 200 encourages copy and paste, which is the reflex the pattern exists to break.

**Reviewed for:** Does every built bulk removal match the wireframe its catalog entry chose? Open the built count-and-money dialogs, background jobs, and dry-run plans beside the catalog entry recording the chosen option and its reason.

#### `a-dialog-naming-the-count-and-the-money`: A Dialog Naming the Count and the Money

```wireframe
page
  breadcrumb "Home" "Expenses" "Reversals"
  header
    heading "Reversals"
    text "31 expenses. 3 selected."
    action primary "Reversal these 3"
  table 3 "Expense" "Spender" "Amount"
    item: text "EXP-1042" · text "Ava" · text "EUR 90.00"
  dialog "Reversal 3 expenses in full?"
    fact "Ava, EXP-1042" "EUR 90.00"
    fact "Tom Becker, EXP-1041" "EUR 40.45"
    fact "Elin Lindqvist, EXP-1040" "EUR 45.00"
    fact "Total going back" "EUR 175.45"
    action "Cancel"
    action destructive "Reversal 3 expenses"
```

**Rationale:** The confirmation lists each record and totals the money above the commit button. The count and the amount are the two things the reader must check.

**Cost:** Twenty-five rows in a dialog mean scrolling inside a dialog. Past about five rows the total carries the meaning, and few readers check each line one by one.

#### `a-background-job-with-a-report-for-the-large-ones`: A Background Job with a Report, for the Large Ones

```wireframe
page
  breadcrumb "Home" "Expenses" "Bulk jobs"
  header
    heading "Bulk jobs"
    text "One running. Reports land here and stay."
    action "New bulk reversal"
  region "Running now"
    status "Running" info
    text "Reverse 1,204 expenses in full. Started 8 Oct 2026, 14:22 by Cara."
    figure "Done" "746 of 1,204"
    action "Cancel the rest"
  region "Finished"
    list 2
      item: text "Archive 200 completed trips" · text muted "Finished 8 Oct 2026, 11:04" · status "200 archived" positive · action "Read the report"
```

**Rationale:** Past the size any dialog can carry, the selection runs as a background job and a report arrives afterwards. Reversing 1,204 expenses cannot be checked on screen, so the report carries the check instead.

**Cost:** The reader walks away and comes back, so the job needs progress, a cancel, and a report naming every row it refused. A job that fails halfway also needs a rule for what already ran.

**Reference:** Shopify

#### `a-dry-run-first-the-way-a-migration-previews`: A Dry Run First, the Way a Migration Previews

```wireframe
page
  breadcrumb "Home" "Expenses" "Reversals" "Plan"
  header
    heading "Plan: reversal 3 expenses"
    text "Nothing has run. This is what would happen."
  table 3 "Expense" "Spender" "What would happen"
    item: text "EXP-1042" · text "Ava" · status "Will reversal" positive
  region "Two expenses are outside the window"
    message "Deduct it or record a bank transfer for those two instead." caution
    fact "1 expense, on the original payout" "EUR 90.00"
    action "Back to the selection"
    action "Discard the plan"
    action destructive "Apply to the 1 that works"
```

**Rationale:** A page says what each row will do, so the refusals are read before anything runs rather than after. Three rows fit, and three hundred still fit, because it is a page rather than a dialog.

**Cost:** The plan can go stale between reading and applying: a payout can settle, and a window can close. The apply step must recheck every row and say which answers changed since the plan was drawn.

**Reference:** Terraform

### Creating a Record and Then Filling It In

**ID:** `litespec/frontend/patterns/create-edit-remove#creating-a-record-and-then-filling-it-in`

**Strength:** default

**Question:** A new record such as a trip needs a name, a date, a site, a priced fare, and a cost centre before it can file, but staff may not know all five at once. The question is how much a create requires up front and how the record gets finished: a name-only draft filled in later, a setup guide on the home page, or everything required before the record exists.

**Default:** `create-with-a-name-fill-the-rest-later`

**Compact:** On a narrow screen the notice and the missing-parts list stack in one column under the header.

**Rationale:** The default is to create with a name and fill the rest later. The record exists as a draft, and every later addition is its own audited command. The home-page setup guide is the runner-up and the required companion, because a draft nobody finishes sits unfinished on a list. Everything required before the record exists is the other alternative, for a record that has to be whole before anyone sees it. Leave out a create that writes nothing until the final publish step: if the browser loses the page, the whole form goes with it.

**Reviewed for:** Does every built draft-first flow match the wireframe its catalog entry chose? Open the built name-first creates, setup guides, and required-field creates beside the catalog entry recording the chosen option and its reason.

#### `create-with-a-name-fill-the-rest-later`: Create with a Name, Fill the Rest Later

```wireframe
page
  breadcrumb "Home" "Expenses" "Rotterdam client visit"
  header
    heading "Rotterdam client visit"
    status "Draft" neutral
    text "Nothing about it is scheduled yet."
    action primary "Finish it"
  region "Notice"
    message "A draft cannot take expenses. It needs a date, a site, one priced fare and a cost centre." info
  region "Four things this trip still needs"
    fact "Name" "Rotterdam client visit. Set on 8 Oct 2026."
    fact "When and where" "No date, no site."
    fact "A fare, priced" "Nothing to claim yet."
    action "Set the date and site"
```

**Rationale:** The name is the only required field, and the record exists as a draft from that moment. Every later addition, the date, the site, the fare, is its own audited and reconstructable command.

**Cost:** A draft trip is nearly useless on its own. If nothing prompts the reader to finish it, the draft sits unfinished on a list. The home-page setup guide is that prompt.

#### `a-setup-guide-on-the-home-page-with-progress`: A Setup Guide on the Home Page, with Progress

```wireframe
page
  breadcrumb "Home"
  header
    heading "Good afternoon, Dana"
    text "Two things need you today. Rotterdam client visit is scheduled."
    action primary "New trip"
  region "Finish the setup"
    figure "Done" "3 of 5"
    fact "Connect the bank payouts" "Connected 2 Oct by Dana."
    fact "Set the returns policy" "Still the default text."
    action "Set it"
    action quiet "Dismiss"
```

**Rationale:** The home page carries the setup as a guide with a progress bar, not as one more list. Dana finished three of five steps, so she sees the two that are left every time she signs in.

**Cost:** The guide sits at the top of the home page until it is done, and a guide nobody dismisses becomes a banner nobody reads. Once dismissed, it stays dismissed for the company, not just the person.

**Reference:** Stripe

#### `everything-required-before-the-record-exists`: Everything Required Before the Record Exists

```wireframe
page
  breadcrumb "Home" "Expenses" "New trip"
  header
    heading "New trip"
    text "Five things. A trip cannot exist without them."
  region "The trip"
    field "Name" text
    field "Description" textarea
  region "When and where"
    field "Boards" text
    field "Departs" text
    field "Site" select = "Rotterdam client site"
  region "What is active"
    field "Fare name" text = "Travel policy"
    field "Amount, in euros" number = "45.00"
    action "Cancel"
    action primary "Create the trip"
```

**Rationale:** The record is not created until it is whole: name, date, site, one fare, and one cost centre. Nothing half-made ever lands in a list.

**Cost:** It asks for a five-part form before anything exists. A reader who only wants to pencil in a date has to fill in all of it.

### Changing Something That Has Already Happened

**ID:** `litespec/frontend/patterns/create-edit-remove#changing-something-that-has-already-happened`

**Strength:** default

**Question:** A record of what is true now cannot always be edited: an expense already paid has to stay reconstructable for billing and audits. The question is which shape each after-the-fact change takes: the old value kept beside the new, a dated correction, or a refusal that names the newer way.

**Default:** `the-old-value-stays-on-the-record-beside-the-new`

**Compact:** On a narrow screen the timeline of names stacks in one column under the header.

**Rationale:** The default keeps the old value beside the new: an expense filed last month still resolves to the trip it was filed for. The dated correction is the runner-up where a fact was entered wrong. The refusal naming the newer way is the other alternative where no safe edit exists. Never ship a silent overwrite of anything priced or paid: the record must show what it was before.

**Reviewed for:** Does every built correction flow match the wireframe its catalog entry chose? Open the built past-change edits, dated corrections, and refusals beside the catalog entry recording the chosen option and its reason.

#### `the-old-value-stays-on-the-record-beside-the-new`: The Old Value Stays on the Record Beside the New

```wireframe
page
  breadcrumb "Home" "Expenses" "Rotterdam client visit"
  header
    heading "Rotterdam client visit"
    text "Sat 14 Mar 2026, Rotterdam client site. 1,204 expenses."
    action "Rename"
  region "The name over time"
    text "Expenses already filed carry the name they were filed under."
    timeline 3
      item: text "Rotterdam client visit" · text muted "8 Oct 2026, 14:22" · text muted "Ben"
```

**Rationale:** A rename shows what the record was called and when it changed. The record keeps the earlier name, because an expense filed last month still has to resolve to the trip it was filed for.

**Cost:** Every edit grows the record. A reader looking for what the trip is called now has to skip past what it used to be called.

#### `a-correction-dated-and-attributed`: A Correction, Dated and Attributed

```wireframe
page
  breadcrumb "Home" "Expenses" "Rotterdam client visit" "Incidents" "INC-0207"
  header
    heading "Incident INC-0207"
    status "Closed" neutral
    text "14 Mar 2026, front desk. Recorded 14 Mar, corrected 2 Oct."
  region "What happened"
    fact "Who" "A courier, no name given"
    fact "What" "Presented another spender's receipt"
    fact "Why" "The original receipt could not be produced"
    text muted "Corrected 2 Oct 2026 by Cara."
  region "The corrections"
    timeline 2
      item: text "Why corrected" · text muted "2 Oct 2026, 09:12" · text muted "Cara"
```

**Rationale:** Where a figure was entered wrong, the correction is a statement about the past. Incident INC-0207 carries it as its own dated fact, corrected 2 Oct by Cara, rather than overwriting the wrong entry.

**Cost:** Two figures for the same thing confuse any page that shows only one of them. Every read has to choose which to show, and that choice is a rule rather than a display decision.

#### `the-edit-is-refused-with-the-newer-way-named`: The Edit Is Refused, with the Newer Way Named

```wireframe
page
  breadcrumb "Home" "Policies" "Travel policy" "Ceilings"
  header
    heading "Ceilings"
    text "Travel policy went active 12 Jan 2026."
  region "Refused"
    message "That ceiling has already been claimed. 412 lines were claimed at EUR 45.00." critical
    text "Changing it would make the records disagree with the spender."
    action disabled "Change the ceiling everyone claimed"
    link "Raise the remaining budget instead"
  region "Still active"
    field "Ceiling for the 88 lines still active, in euros" number = "50.00"
    action primary "Save the new ceiling"
```

**Rationale:** Some edits cannot happen at all after the fact. The page does not disable the command silently: it says what cannot change, and names the command that does what the reader was trying to do.

**Cost:** A reader who wants a ceiling change on a claimed line is refused and offered the replacement command instead. That is the correct answer, but refusals still generate support cases.

### Duplicating a Record

**ID:** `litespec/frontend/patterns/create-edit-remove#duplicating-a-record`

**Strength:** default

**Question:** A duplicate is a create that starts from something. The copy carries the setup and never the history: ceilings and a trip plan travel, while expenses and money stay with the original. The question is how much say the reader gets over what the copy carries: a dialog that names the parts, a checklist to choose them, or one click with no questions.

**Default:** `a-dialog-that-says-what-the-copy-carries`

**Compact:** On a narrow screen the dialog opens as a full-width sheet, with the name field and the carried list stacked.

**Rationale:** The default is the dialog that says what the copy carries. A duplicate that silently drops the ceilings is worse than no duplicate. The carry checklist is the runner-up where the parts vary per company. One click from the row menu is the other alternative, and it earns its place for budget codes and plan entries, which carry no spenders. Never ship one-click duplication for a trip: a silent copy drops the ceilings a trip needs. An expense is never duplicated, because it is one spender's claim.

**Reviewed for:** Does every built copy flow match the wireframe its catalog entry chose? Open the built duplicate dialogs, carry checklists, and row-menu copies beside the catalog entry recording the chosen option and its reason.

#### `a-dialog-that-says-what-the-copy-carries`: A Dialog That Says What the Copy Carries

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Trips"
    text "93 scheduled."
    action "New trip"
  region "Trips"
    toolbar
      field "Search trips" search
    list 1
      item: text "Rotterdam client visit" · text muted "Sat 14 Mar 2026 · Rotterdam client site" · status "Draft" neutral · text "0 of 40 receipts" · action "Open"
  dialog
    heading "Duplicate Rotterdam client visit"
    field "Name of the copy" text = "Rotterdam client visit (copy)"
    text "The copy carries the trip plan, the fares and ceilings, and the payout plan."
    text "Expenses, committed lines and money stay with the original. The copy starts as a draft."
    action "Cancel"
    action primary "Duplicate it"
```

**Rationale:** The dialog names what the copy carries before the copy exists: the trip plan, the fares and ceilings, and the payout plan travel, while expenses and money stay behind. The copy starts as a draft, so nothing about it is active until the reader says so.

**Cost:** A long carried list makes a long dialog, and most of it is read once. Past about six parts the checklist option says it better.

**Reference:** Shopify

#### `choose-what-the-copy-carries`: Choose What the Copy Carries

```wireframe
page
  breadcrumb "Home" "Expenses" "Rotterdam client visit" "Duplicate"
  header
    heading "Duplicate Rotterdam client visit"
    text "Tick what the copy carries. The rest stays behind."
  region "What travels"
    field "Name of the copy" text = "Rotterdam client visit (copy)"
    field "Trip plan, 6 entries" checkbox
    field "Fares and ceilings, Train fare and Hotel night" checkbox
    field "Payout plan, 1 run and 2 batches" checkbox
    field "Team assignments, Eli and Ben" checkbox
    text "Expenses, committed lines and money never travel. The copy starts as a draft."
  footer
    action "Cancel"
    action primary "Duplicate it"
```

**Rationale:** Every part is a checkbox with its size beside it: the trip plan with 6 entries, the fares, the payout plan. A reader who wants the ceilings but not the team picks exactly that here.

**Cost:** Six checkboxes for a routine duplicate are five too many on the tenth use. The checklist fits the first duplicate of a quarter, not the weekly one.

**Reference:** Notion

#### `one-click-from-the-row-menu-then-a-toast`: One Click from the Row Menu, Then a Toast

```wireframe
page
  breadcrumb "Home" "Policies" "Budget codes" "Codes"
  header
    heading "Budget codes"
    text "4 codes. All of them reduce the expense, never the fee."
    action "New budget code"
  region "Codes"
    table 3 "Code" "Reduction" "Used"
      item: text "WELCOME10 copy" · text "EUR 10.00 off" · text "0 of 200" · menu "More"
    text "WELCOME10, CREW and one more code sit above. The copy lands as a draft."
  toast info "WELCOME10 copy was created as a draft."
```

**Rationale:** One menu item with no questions. The copy is created as a draft, confirmed by a toast. A budget code carries no spenders, so there is nothing to check first.

**Cost:** There is no moment to rename or choose what travels. The option fits records with no spenders and no money: a trip duplicated this way silently drops the ceilings it needs.

**Reference:** Mailchimp

### Putting a List in Order by Hand

**ID:** `litespec/frontend/patterns/create-edit-remove#putting-a-list-in-order-by-hand`

**Strength:** default

**Question:** Policies, plan entries, and portal sections all have an order the reader sets. The spender reads the order the reader left, so the control has to work for a mouse, a keyboard, and a finger. The question is which control each list gets: drag handles, menu moves, or typed position numbers.

**Default:** `drag-handles-in-the-list`

**Compact:** On a narrow screen the rows stack in one column, with the grip and the row menu on each row.

**Rationale:** Drag handles in the list are the default: direct, visible, and the order saves as it changes. Move up and Move down in the row menu are the runner-up and the required companion, because drag alone leaves keyboard and screen-reader users behind. Typed position numbers are the other alternative for a jump across a long list. Never ship typed position numbers alone: two rows numbered 3 leave the order undecided.

**Reviewed for:** Does every built manual ordering match the wireframe its catalog entry chose? Open the built drag handles, move commands, and typed numbers beside the catalog entry recording the chosen option and its reason.

#### `drag-handles-in-the-list`: Drag Handles in the List

```wireframe
page
  breadcrumb "Home" "Policies" "Travel"
  header
    heading "Policies"
    text "3 policies. Spenders read them in this order."
  message info "Drag a row to move it. The order saves on drop and the portal follows within a minute."
  region "Fares"
    list 3
      item: text "1" · text "Travel policy" · text muted "EUR 45.00 · 412 claimed" · menu "More"
    text "Spring rate at EUR 40.00 and Hotel night at EUR 120.00 follow below."
    text "The first policy is the one spenders see first."
```

**Rationale:** A grip sits on every row, the dragged row lifts, and a line shows where it will land. The order saves on drop, so there is nothing left to forget.

**Cost:** A grip cannot be used from a keyboard or announced by a screen reader, and on a phone it conflicts with scrolling. The menu move is not an alternative to drag: it is the other half of this option.

**Reference:** Notion

#### `move-up-and-move-down-in-the-row-menu`: Move up and Move Down in the Row Menu

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "Trip" "Plan"
  header
    heading "Rotterdam client visit"
    text "Trip plan · 6 entries, sorted by start time."
  region "Trip plan"
    list 3
      item: text "Labels printed" · text muted "Set 2 Oct · position 2 of 6" · text "07:00" · menu "More"
    text "Receipt check at 06:30 sits above. Meeting room booked at 08:00 sits below."
    text "Every move announces its new position. The order saves with each move."
  popover "More"
    action "Move up"
    action "Move down"
    action "Move to top"
    action "Move to bottom"
```

**Rationale:** The row menu offers the same move as a named command: Move up, Move down, Move to top, Move to bottom. A screen reader announces Moved to position 2, and nothing about it needs a pointer.

**Cost:** Moving one row past twenty others takes twenty menu opens. The menu move is the accessible half of drag, not a replacement for it on long lists.

**Reference:** Linear

#### `a-position-number-typed-on-each-row`: A Position Number Typed on Each Row

```wireframe
page
  breadcrumb "Home" "Policies" "Travel"
  header
    heading "Policies"
    text "3 policies. Numbers set the sequence spenders read."
  region "Fares"
    list 3
      item: text "Travel policy" · text muted "EUR 45.00 · 412 claimed" · field "Position of Travel policy" number = "1"
    text "Spring rate holds 2 and Hotel night holds 3. Two rows with the same number are refused."
  footer
    action "Reset"
    action primary "Save the expense"
```

**Rationale:** A number on each row, and one save for the whole list. Jumping a row from 14 to 1 is one keystroke where dragging would cross the whole list.

**Cost:** Two rows numbered 3 leave the order undecided, so duplicates are refused and the reader renumbers by hand. Renumbering fourteen rows that way is slower than dragging one, so numbers suit a jump across a long list and nothing else.

**Reference:** WordPress

### Restoring an Archived Record

**ID:** `litespec/frontend/patterns/create-edit-remove#restoring-an-archived-record`

**Strength:** default

**Question:** Archive is only safe if restore works. A restore returns the record to where it was, with its history, and says when something changed while it was away, such as a name taken since. The question is where restore lives: the archived view, the record page, or the undo toast.

**Default:** `restore-from-the-archived-view`

**Compact:** On a narrow screen the archived list stacks in one column with Restore on each row, and the toast spans the phone width.

**Rationale:** Restore from the archived view is the default: the view is the way back, and each row says where its record returns to. The record-page banner is the runner-up for anyone arriving by link. The Undo toast is the other alternative in the seconds after archiving, not as the way back days later. Never ship a restore that silently un-archives parents or overwrites a newer name: both need their own dialog.

**Reviewed for:** Does every built restore flow match the wireframe its catalog entry chose? Open the built archived views, record-page banners, and undo toasts beside the catalog entry recording the chosen option and its reason.

#### `restore-from-the-archived-view`: Restore from the Archived View

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Trips"
    text "93 scheduled, 4 archived."
    action "New trip"
  region "Archived"
    toolbar
      field "Search trips" search
    list 3
      item: text "Okafor Office visit" · text muted "28 Feb 2026 · archived 2 Oct" · text "Returns as Scheduled" · action "Restore"
    text "A restore returns the record to the state it held. Nothing goes back live by itself."
  toast positive "Okafor Office visit is back under Scheduled."
```

**Rationale:** The Archived view lists what is hidden, and each row carries its own Restore. Okafor Office visit returns to the Scheduled state it held, not back to a draft.

**Cost:** Nobody opens the Archived view unless they remember it exists. Anything restored from here has to say where it went, or the record vanishes from this list into another nobody is reading.

**Reference:** Shopify

#### `restore-from-the-record-page`: Restore from the Record Page

```wireframe
page
  breadcrumb "Home" "Expenses" "Okafor Office visit"
  header
    heading "Okafor Office visit"
    text "28 Feb 2026 · Rotterdam office. Archived 2 Oct."
    action "Open"
  message caution "This trip is archived. Archived 2 Oct 2026 by Ben. Spenders cannot see it and nothing about it is scheduled."
  region "Restore it"
    text "It returns as Scheduled, with its plan, its ceilings and its 0 committed lines. Nothing goes back live by itself."
    action primary "Restore this trip"
```

**Rationale:** The archived banner sits on the record itself, with Restore beside it. Anyone arriving by link or search restores without finding the archived view first.

**Cost:** The page has to work while archived: read-only where it matters, with every command saying it needs a restore first. A banner on a broken page is worse than no banner.

**Reference:** GitHub

#### `undo-in-the-seconds-after-archiving`: Undo in the Seconds After Archiving

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Trips"
    text "93 scheduled, 4 archived."
    action "New trip"
  region "Trips"
    toolbar
      field "Search trips" search
    list 2
      item: text "Rotterdam client visit" · text muted "9 Oct 2026 · Rotterdam client site" · status "Scheduled" info · text "1 of 40 receipts"
    text "Okafor Office visit left this list a moment ago. Undo is fastest where the archive happened."
  toast info "Okafor Office visit was archived. Undo."
```

**Rationale:** The toast is the way back in the seconds after archiving. Archiving the wrong trip is fixed where it happened, with no visit to another view.

**Cost:** Eight seconds and the toast is gone, so this is a correction for slips, not the way back. Anyone restoring a day later still needs the archived view.

**Reference:** Gmail

### Inviting a Team Member

**ID:** `litespec/frontend/patterns/create-edit-remove#inviting-a-team-member`

**Strength:** default

**Question:** Trip crew join per trip, and staff join per company. An invitation names the person, the role, and the scope before sending, because wrongly granted access has to be found and removed later. The question is how each invitation is sent and tracked: by email, by link, or on the pending list.

**Default:** `invite-by-email-with-the-role-chosen-first`

**Compact:** On a narrow screen the dialog opens as a full-width sheet, with the email, role, and scope stacked.

**Rationale:** The default is to invite by email with the role chosen first: person, role, and scope are all named on one dialog. The pending list with resend and revoke is the runner-up and the required companion, because an invitation waits far longer than sending takes. The invite link is the other alternative for trip crew on one trip, where forty people are not typed by hand. Never ship an open invite link for staff roles: anyone holding it joins. A request to join is refused: the company invites, and nobody applies.

**Reviewed for:** Does every built join flow match the wireframe its catalog entry chose? Open the built email invites, links, and pending lists beside the catalog entry recording the chosen option and its reason.

#### `invite-by-email-with-the-role-chosen-first`: Invite by Email, with the Role Chosen First

```wireframe
page
  breadcrumb "Home" "Settings" "Team"
  header
    heading "Team"
    text "6 people. 1 invitation waiting."
    action "Invite someone"
  region "Team"
    list 3
      item: text "Dana" · text muted "Owner · Harbor and Pine" · menu "More"
    text "Ben and Eli sit below. Eli serves Rotterdam client visit."
  dialog
    heading "Invite someone"
    field "Email" text = "noa.vermeer@example.com"
    field "Role" select = "Trip lead"
    field "Scope" select = "Rotterdam client visit"
    action "Cancel"
    action primary "Send the invitation"
```

**Rationale:** One dialog holds the email, the role with what it can do beside it, and the scope. The scope line says whether Noa joins the company or one trip, before the invitation exists.

**Cost:** It sends one invitation per person, typed by hand. Assigning forty temps for a quarter needs the link option alongside it.

**Reference:** Slack

#### `pending-invitations-with-resend-and-revoke`: Pending Invitations, with Resend and Revoke

```wireframe
page
  breadcrumb "Home" "Settings" "Team"
  header
    heading "Team"
    text "6 people. 2 invitations waiting."
    action "Invite someone"
  tabs "People 6" "Invitations 2" "Roles 6"
    region "Invitations"
      list 2
        item: text "noa.vermeer@example.com" · text muted "Temp · Rotterdam client visit · sent 6 Oct, expires 13 Oct" · status "Waiting" caution · action "Resend" · action "Revoke"
      text "An invitation expires 7 days after it is sent. lena.bakker@example.com expired 8 Oct and needs reissue."
```

**Rationale:** Every waiting invitation shows its age, its expiry, and its way out: Noa's invitation shows sent 6 Oct, expires 13 Oct, with Resend and Revoke beside it. An invitation is read far more often than it is sent, so the pending list is the part that matters.

**Cost:** A list of the waiting becomes a list of the neglected: half of it expires unnoticed. Expired rows must say so and offer reissue, or the list fills with rows that can no longer be used.

**Reference:** GitHub

#### `an-invite-link-anyone-with-it-can-use`: An Invite Link Anyone with It Can Use

```wireframe
page
  breadcrumb "Home" "Settings" "Team" "Invite link"
  header
    heading "Invite the trip crew"
    text "Rotterdam client visit · Sat 14 Mar 2026. The link joins as Temp and expires with the trip."
  region "The link"
    fact "Invite link" "harbor-pine.example/join/7K4Q-2M9X"
    action quiet "Copy"
    fact "Role" "Temp"
    fact "Scope" "Rotterdam client visit"
    fact "Uses" "12 of 60"
    fact "Expires" "15 Mar 2026, 18:00"
    message caution "Anyone with this link joins as Temp for Rotterdam client visit. It never grants finance, manager or owner access."
  footer
    action destructive "Revoke the link"
    action "Copy it again"
```

**Rationale:** A link with an expiry and a use count covers forty temps nobody wants to type by hand. The link joins one trip in one role, Temp on Rotterdam client visit, and nothing else.

**Cost:** Anyone with the link joins, including anyone it was forwarded to. The link fits trip crew on one trip, and is refused for owner, finance, and manager roles.

**Reference:** Discord

### Publishing and Unpublishing a Draft

**ID:** `litespec/frontend/patterns/create-edit-remove#publishing-and-unpublishing-a-draft`

**Strength:** default

**Question:** A draft is private until spenders can see it. Publishing says what spenders will find, and unpublishing says what happens to what they found, because filed expenses outlive either command. The question is how each side of that line is drawn.

**Default:** `publish-with-the-readiness-check-beside-it`

**Compact:** On a narrow screen the dialog opens as a full-width sheet, with the readiness facts stacked above the commit.

**Rationale:** The default is to publish with the readiness check beside it: the reader reads what spenders will find before they find it. Per-channel publish is the runner-up for companies that publish in more than one place. Unpublish with the spender consequences stated is the other alternative, because filed expenses outlive the command. Never ship a silent unpublish: the page must say that filed expenses stay valid.

**Reviewed for:** Does every built publish flow match the wireframe its catalog entry chose? Open the built publish, unpublish, and per-channel controls beside the catalog entry recording the chosen option and its reason.

#### `publish-with-the-readiness-check-beside-it`: Publish with the Readiness Check Beside It

```wireframe
page
  breadcrumb "Home" "Policies" "Travel policy"
  header
    heading "Travel policy"
    status "Draft" neutral
    text "EUR 45.00 · 500 lines."
    action "Publish"
  region "Budget codes"
    list 1
      item: text "Standard" · text muted "EUR 45.00 · 500 lines" · status "Priced" positive
  dialog
    heading "Publish Travel policy?"
    text "Spenders will find this. The portal lists it within a minute of publishing."
    fact "A name and a description" "Travel policy · train, hotel and meals"
    fact "A ceiling" "Standard at EUR 45.00, 500 lines"
    fact "A channel live" "Portal"
    action "Keep it a draft"
    action primary "Publish it"
```

**Rationale:** The dialog lists what is ready, the name, the ceiling, a live channel, and the button commits only when all of it holds. Publishing is the one create the reader cannot undo quietly, so the check sits in the dialog itself.

**Cost:** A check that reads five systems can disagree with the page behind it by the time the reader presses the button. The publish command must recheck, and refuse with the missing piece named.

**Reference:** Webflow

#### `publish-per-channel-one-at-a-time`: Publish per Channel, One at a Time

```wireframe
page
  breadcrumb "Home" "Policies" "Travel policy" "Channels"
  header
    heading "Travel policy"
    text "One policy, three channels, three states."
  region "Channels"
    list 3
      item: text "Portal" · text muted "expenses.harbor-pine.example · live since 12 Jan 2026" · status "Live" positive · action "Unpublish"
    text "Phone filings at Canal Street 12 are ready and never published. The partner office holds no budget."
    text "Each channel publishes on its own. Filed expenses stay valid on every channel either way."
```

**Rationale:** Each channel holds its own state, so phone filing can run while the portal waits. The page shows one publish button per channel, each with its own readiness check.

**Cost:** Three channels in three states are a matrix the reader must hold in mind. The policy page must show every channel at once, or one channel publishes what another keeps unpublished.

**Reference:** Shopify

#### `unpublish-with-the-customer-consequences-stated`: Unpublish with the Spender Consequences Stated

```wireframe
page
  breadcrumb "Home" "Policies" "Travel policy"
  header
    heading "Travel policy"
    status "Active" positive
    text "412 claimed. The portal shows it now."
    action destructive "Unpublish"
  region "Portal"
    list 1
      item: text "expenses.harbor-pine.example" · text muted "Published since 12 Jan 2026" · status "Live" positive
  dialog
    heading "Unpublish Travel policy?"
    text "Filing stops within a minute. The portal shows the policy as unavailable and phone filing cannot take it."
    message positive "The 412 expenses already filed stay valid and still pay."
    action destructive "Unpublish it"
    action "Keep it published"
```

**Rationale:** The dialog says what stops and what stays, because filed expenses outlive the policy. Unpublishing stops filing, and touches nothing claimed: the 412 expenses already filed stay valid and still pay.

**Cost:** The dialog carries two messages at once, and the second is the one spenders will ask about. The page behind it must keep answering for the claimed lines after the policy is gone.

**Reference:** Shopify

### Scheduling a Change for Later

**ID:** `litespec/frontend/patterns/create-edit-remove#scheduling-a-change-for-later`

**Strength:** default

**Question:** Ceilings, fees, and live windows change on fixed dates. A scheduled change names the value, the date, and the way to stop it, and the record shows both the current and the coming value until the date arrives. The question is how the coming value is shown and stopped.

**Default:** `a-change-with-a-date-listed-with-the-record`

**Compact:** On a narrow screen the now and next facts stack in one column, with the schedule field and its save under them.

**Rationale:** The default is a change with a date, listed with the record: current and coming values sit side by side with a cancel. The code timeline is the runner-up where ceilings change on fixed dates. The spender preview is the other alternative where spenders read the value, because a fee change is judged by the line they see. Never ship overlapping schedules on one record: one record cannot become two fees on the same date.

**Reviewed for:** Does every built dated change match the wireframe its catalog entry chose? Open the built scheduled changes, code timelines, and spender previews beside the catalog entry recording the chosen option and its reason.

#### `a-change-with-a-date-listed-with-the-record`: A Change with a Date, Listed with the Record

```wireframe
page
  breadcrumb "Home" "Policies" "Late fee"
  header
    heading "Late fee"
    text "What the spender pays on top of the expense. You keep all of it."
  region "The fee"
    fact "Now, since 1 Oct 2026" "EUR 5.00"
    fact "From 1 November 2026, scheduled" "EUR 6.00"
    message info "Expenses filed before 1 Nov keep EUR 5.00. Expenses after pay EUR 6.00."
    field "Change the scheduled fee, in euros" number = "6.00"
  footer
    action destructive "Cancel the schedule"
    action primary "Save the schedule"
```

**Rationale:** The coming change is one row with a date and a cancel. The late fee page shows now, EUR 5.00, and next, EUR 6.00 from 1 November, together.

**Cost:** Two values on one record make a rule every read must apply: expenses filed before the date keep the old fee. Any page that prints the fee must print its date too.

**Reference:** WordPress

#### `a-tier-ends-on-its-date`: A Code Ends on Its Date

```wireframe
page
  breadcrumb "Home" "Policies" "Travel policy" "Standard" "Ceilings"
  header
    heading "Standard"
    text "One policy, three ceilings, each with its dates."
    action "Add a code"
  region "Ceilings"
    timeline 3
      item: text "Last lines · EUR 55.00" · text muted "From 1 Mar · 0 claimed · scheduled" · status "Scheduled" info · action "Change" · action "Cancel"
    text "Spring rate at EUR 40.00 ended 1 Feb with 88 claimed. Standard at EUR 45.00 applies now, active until 1 Mar."
    text "A line claimed keeps the ceiling it was claimed at. The timeline never rewrites a claim."
```

**Rationale:** Each ceiling holds its own dates, and the timeline shows what applies when. Standard at three ceilings appears as three rows, the Spring rate that ended, the Standard that applies now, and the Last lines that are scheduled, not one ceiling edited twice.

**Cost:** Three rows for one policy mean three records to read on every expense and in every report. A spender who finds the old ceiling can ask for the difference back, and the page must not hide that.

#### `what-the-customer-sees-before-and-after`: What the Spender Sees, Before and After

```wireframe
page
  breadcrumb "Home" "Policies" "Late fee" "Schedule"
  header
    heading "Fee change, 1 November"
    text "What the spender reads on the expense line, now and after."
  split 1:1
    region "Now"
      text "Travel policy · EUR 45.00."
      text "Plus EUR 5.00 trip fee · total EUR 50.00."
    region "After 1 November"
      text "Travel policy · EUR 45.00."
      text "Plus EUR 6.00 trip fee · total EUR 51.00."
  message info "The policy stays the same. Only the fee line changes, on expenses filed from 1 Nov."
  region "The schedule"
    fact "Takes effect" "1 November 2026, 00:00"
    fact "Set by" "Cara · 8 Oct 2026"
  footer
    action destructive "Cancel the schedule"
    action primary "Keep it scheduled"
```

**Rationale:** The reader reads the spender page twice, once for now and once for after. A fee change is judged by its spender line, the EUR 50.00 total becoming EUR 51.00, not by its row on a settings page.

**Cost:** A preview is a second rendering of the portal and can drift from it. It has to render from the same policy read the spender gets, or it shows a page the spender never sees.

**Reference:** Squarespace
