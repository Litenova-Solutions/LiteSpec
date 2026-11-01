# Create, Edit and Remove Patterns

## Intent

Staff create records such as expenses, trips, and fares, change them, and remove one or several, and this page shows the layout for each task as drawn options with a named default to build, including the special cases: draft-first creation, corrections to the past, duplicates, hand ordering, restoring archived work, invitations, publishing, and scheduled changes. The product owner chooses among the options when the team builds or revises a working screen. One decided shape per task keeps destructive and far-reaching commands predictable, so staff know what a command will do before they commit.

## Concepts

### How to Read a Pattern

Each pattern answers one recurring question about working with records, such as how many inputs keep a create in a dialog. It states the question, the default option a project builds, how the default behaves on a narrow screen, and why the default won. Each option then shows one wireframe drawing with its own rationale, cost, and optional reference. A project records its choice in its pattern catalog and builds the chosen option with its own components. The [UI patterns](../patterns.md) page states the catalog contract and the wireframe language.

### The Running Example

The drawings on this page use Harbor and Pine travel: trips such as Rotterdam client visit, expenses such as EXP-1042 for Ava, and fares such as the train fare at EUR 48.90. On this page, the reader is the staff member using the screen. The Standards section states the two rules every option keeps: every removal offers undo or asks for confirmation, and a successful create shows the created record.

## Standards

### Confirm or Undo Every Removal

**ID:** `litespec/frontend/patterns/create-edit-remove#confirm-or-undo-every-removal`

**Statement:** Every removal MUST either offer undo or ask for confirmation before it runs.

**Rationale:** Staff remove the wrong record by misreading a row or misclicking, and without a way back the record is gone. An undo toast restores a reversible removal in one click, while a confirmation dialog makes an irreversible removal a deliberate choice. Either way the reader gets a moment to catch the mistake before it becomes permanent.

**Reviewed for:** Which built removal runs without asking and offers no way back? Open the archive, delete, void, and suspend flows on built screens.

### Show the Created Record

**ID:** `litespec/frontend/patterns/create-edit-remove#show-the-created-record`

**Statement:** A successful create MUST take the reader to the created record or show it in place.

**Rationale:** After creating, the reader needs proof that the record exists with the values entered, and the created record itself is that proof. A create that ends on an empty form or a bare toast leaves the reader unsure whether it worked, so they create it again and produce a duplicate.

**Reviewed for:** Which built create flow ends away from its new record? Open the create dialogs, pages, and bulk flows on built screens.

## Patterns

### Creating One Record

**ID:** `litespec/frontend/patterns/create-edit-remove#creating-one-record`

**Question:** Staff create records such as trips, reversals, and fares. A create with one to four inputs fits a dialog, and a longer one needs a page; a drawer never holds a create. The decision is which shape each create takes: dialog, page, template, copy, inline row, file import, steps, or a shortcut from elsewhere.

**Default:** `a-dialog-for-two-or-three-inputs`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** The dialog stays the pick for one to four inputs: it keeps the list behind it and commits with one verb. The full page is the runner-up and the right one past four inputs or with sections. The inline table row is the one to refuse, because a form inside a table breaks the table structure and hides validation. Of the further options, the split button earns its place on the trips list, where three starting points already exist.

**Reviewed for:** Which built creation form differs from the wireframe its catalog entry chose? Open the built dialog, page, and inline starts beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The dialog holds three inputs, one verb on the commit button, and Cancel beside it. This is where most creates belong and where they usually end up.

**Cost:** A create that grows past four inputs has to leave the dialog. The typed values come with it or are lost.

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

**Rationale:** The form pages with sections, steps, or more than four inputs. A page gives the fields room and gives the result a link.

**Cost:** Leaving the list to create one record, and coming back to the list afterwards, is two navigations for every single creation. It only pays when the form is long.

#### `create-from-a-template-chosen-before-the-form`: Create from a Template, Chosen Before the Form

```wireframe
page
  breadcrumb "Home" "Expenses" "New trip"
  header
    heading "New trip"
    text "Start from last time, or from nothing."
  region "Start from"
    field "Start from" select = "A template"
    text muted "A template carries a spender, a site and a trip plan."
  region "The trip"
    field "Template" select = "Client visit, standard"
    field "Name" text = "Rotterdam client visit"
    field "When" text = "09/10/2026"
    action "Cancel"
    action primary "Create trip"
```

**Rationale:** One shape offers 'A blank trip' or 'A template' at the top of the form. Choosing first means the fields below load with the template's spender, site, and plan already filled.

**Cost:** The starter control sits inside the form but is not part of it. Once a template is chosen the control stays visible with no further use, because the form cannot remove a control mid-create.

#### `create-from-a-copy-of-an-existing-record`: Create from a Copy of an Existing Record

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Trips"
    text "93 scheduled."
    action "New trip"
  toolbar
    field "Search trips" search
  region "Scheduled"
    list 3
      item: text "Rotterdam client visit" · text muted "9 Oct 2026" · status "Scheduled" info · action "Open"
  menu "More actions for Rotterdam client visit"
    action "Open"
    action "Duplicate this trip"
    action "Copy the plan to another"
```

**Rationale:** Duplicate a trip that worked, and then change what is different. The copy carries fares and a plan, and it starts as a draft with nothing committed.

**Cost:** A duplicate of a completed trip is nearly meaningless: the expenses and the money all belong to the original. It only works for a template-like record with no history.

#### `create-inline-in-the-table-as-an-empty-first-row`: Create Inline in the Table, as an Empty First Row

```wireframe
page
  breadcrumb "Home" "Expenses" "Reversals"
  header
    heading "Reversals"
    text "31 expenses to decide. None of these is a reversal yet."
    action primary "Start a reversal"
  region "New reversal"
    field "Expense" text = "EXP-1042"
    field "Route" select = "Bank reversal"
    field "Amount" number = "45.00"
    action "Cancel"
    action primary "Add"
  table 4 "Expense" "Spender" "Asked" "Amount"
    item: text "EXP-1041" · text "Elin Lindqvist" · text "214 days ago" · text "EUR 45.00"
```

**Rationale:** The table grows a form row at the top. Nothing opens, nothing closes, and the reader sees what they are adding in the context of what is already there.

**Cost:** Carbon says a modal is for tasks that are not repeated, and this is the most repeated create in the back office. It also puts a form inside a table, where tables hold rows and no rule covers embedded forms.

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

**Rationale:** A bulk create runs in three steps: the file, a check of what it made, and a report. This is the shape for a trip of expenses or a list of spenders, where one dialog per record would take all day.

**Cost:** An import writes across many records at once. A partial failure has to be reportable and undoable while nothing is paid.

#### `one-question-per-step-kept-as-a-draft`: One Question per Step, Kept as a Draft

```wireframe
page
  breadcrumb "Home" "Expenses" "New trip"
  header
    heading "New trip"
    text "Step 2 of 3. Every answer is kept as a draft."
  region "When is it"
    field "Boards" text = "14/03/2026 06:30"
    field "Departs" text
  region "Already answered"
    fact "Name" "Rotterdam client visit"
    fact "Site" "Rotterdam client site"
  region "Draft"
    message "This draft was started 6 minutes ago. It stays a draft until step 3 is done." info
    action "Back"
    action primary "Continue"
```

**Rationale:** A long create split into one question per step with a progress bar and an already-answered recap. Nothing is lost between steps because every answer saves a draft.

**Cost:** Steps hide the whole form, so a reader who knows every answer types slower than on one page. Drafts also need a list, an expiry, and a rule for who can see them.

#### `a-split-button-that-names-the-starting-point`: A Split Button That Names the Starting Point

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Trips"
    text "93 scheduled."
    action primary "New trip"
    action quiet "Next ›"
  region "Scheduled"
    list 3
      item: text "Rotterdam client visit" · text muted "9 Oct 2026" · status "Scheduled" info · action "Open"
  menu "More ways to create a trip"
    action "Blank trip"
    action "From a template"
    action "Duplicate Rotterdam client visit"
```

**Rationale:** The way Shopify Polaris draws a split button: the main face creates a blank trip. The chevron names the other ways to start. Three starting points already exist on this list, and this is the control that holds them without a toggle inside the form.

**Cost:** The chevron hides the template and the duplicate from anyone who never opens it. A first-time reader still lands on the blank form and never learns the faster start exists.

**Reference:** Shopify Polaris

#### `a-command-menu-create-from-anywhere`: A Command Menu Create, from Anywhere

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Trips"
    text "93 scheduled. Press Ctrl K to create from anywhere."
    action "New trip"
  region "Scheduled"
    list 2
      item: text "Rotterdam client visit" · text muted "9 Oct 2026" · status "Scheduled" info
  dialog "Create something"
    field "What to create" search = "Create trip"
    action "Create trip"
    action "Create policy"
    action "Create budget code"
```

**Rationale:** The way Linear opens creation: one shortcut, one input, and the record type is chosen by typing. A manager who lives in the back office creates without traveling to the list first.

**Cost:** It stays invisible until learned, and it only starts the create. The form still opens afterwards. It needs the shortcut taught on the page, or nobody finds it.

**Reference:** Linear

### Changing a Record That Already Exists

**ID:** `litespec/frontend/patterns/create-edit-remove#changing-a-record-that-already-exists`

**Question:** Staff change records that already exist, from renaming a trip to raising a budget ceiling. The decision is how the edit commits: one save per page, one per section, inline in the list, in a dialog or side panel, with impact stated first, blocked with a way out, or saved automatically. The hazard to avoid is the reader who edits one section and saves another, believing both are saved.

**Default:** `one-save-per-page`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** One Save per page is the pick. A reader who changed three things saves once, and the dirty banner says what is pending. The impact statement is the runner-up for any change that breaks something. Refuse the inline list-row edit on this wide table. Autosave earns its place only for free text, beside the version history in the after-the-fact pattern.

**Reviewed for:** Which built edit flow differs from the wireframe its catalog entry chose? Open the built save, autosave, and blocked edits beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The whole page is one form and one Save. A reader who has changed three things saves once, and nothing on the page disagrees about what is pending.

**Cost:** A partial failure loses every change on the page, and a reader who wants to save the name but leave the dates cannot. The page has to list what changed while edits are unsaved.

#### `one-save-per-section-and-each-says-what-it-saves`: One Save per Section, and Each Says What It Saves

```wireframe
page
  breadcrumb "Home" "Policies" "Travel" "Travel policy"
  header
    heading "Travel policy"
    status "Active" positive
  region "The name"
    status "Not saved" caution
    field "Name" text = "Travel policy"
    field "Description" textarea = "Train, hotel and meals inside the trip ceiling."
    action primary "Save the name"
  region "Line coding"
    field "Coding" radio = "Batch code"
    action primary "Save the coding"
```

**Rationale:** Each section's button repeats its own verb: Save the name, Save the coding. Nothing is ambiguous about what each button commits.

**Cost:** It still makes two commits on one screen, so the reader must remember to press both buttons. It works only because each button names what it commits.

#### `editing-in-place-in-the-list-row`: Editing in Place in the List Row

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Trips"
    text "93 scheduled."
    action primary "New trip"
  toolbar
    field "Search trips" search
  table 4 "Trip" "Date" "State" "Committed"
    item: field "Name" text = "Rotterdam client visit" · field "Date" text = "09/10/2026" · status "Scheduled" info · action "Save" · action "Cancel"
```

**Rationale:** One cell becomes an input, the row looks different while it does, and Save and Cancel sit in the row. NN/g's rule: the row must visibly change or people edit by accident.

**Cost:** It works only on a narrow table. This table has five columns, which is past the width NN/g means by narrow.

**Reference:** NN/g

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

**Rationale:** Where a change breaks something, the page says what before the reader commits: who is affected, what stops working, and what cannot be changed back.

**Cost:** It needs a read that answers a hypothetical, what would change, which is neither the record as it stands nor a command result.

#### `editing-in-a-dialog-for-two-fields`: Editing in a Dialog, for Two Fields

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Trips"
    text "93 scheduled."
  region "Scheduled"
    list 3
      item: text "Rotterdam client visit" · text muted "9 Oct 2026" · status "Scheduled" info · action "Rename"
  dialog "Rename this trip"
    field "Name" text = "Rotterdam client visit"
    text muted "Spenders see this. 96 expenses carry the old name on their paperwork."
    action "Cancel"
    action primary "Rename it"
```

**Rationale:** Two fields do not need a page. A dialog holds them, the list stays behind, and the reader sees the change in context as soon as it lands.

**Cost:** Carbon says a modal is not for repeatable work. Renaming a trip happens a dozen times a quarter, which is repeatable enough to matter.

#### `editing-blocked-with-the-reason-and-the-way-out`: Editing Blocked, with the Reason and the Way Out

```wireframe
page
  breadcrumb "Home" "Policies" "Travel policy" "Ceilings"
  header
    heading "Ceilings"
    text "Travel policy went active 12 Jan 2026. EUR 45.00 is the ceiling the 412 claimed lines carry."
  region "Standard"
    field "Standard, in euros" number = "45.00"
    message "The ceiling the spenders who already claimed were paid. It cannot change." neutral
    action disabled "Change the ceiling everyone claimed"
    link "Raise the remaining budget instead"
  region "Still active"
    field "Ceiling for the 88 lines still active, in euros" number = "50.00"
    action primary "Save the new ceiling"
```

**Rationale:** This is a change that the record's own state forbids. The field is read-only, the reason is stated on the field, and the command that would unblock it is named.

**Cost:** The reader must leave to unblock and come back, and typed input survives only if the page holds it. Showing the field read-only with a link to the unblocking command is the middle ground.

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

**Rationale:** The way Notion saves: there is no Save button, every keystroke lands, and the header always says when. A description edited over an afternoon cannot be lost to a closed tab.

**Cost:** Two people typing at once overwrite each other without noticing, and every keystroke is an audit entry unless the trail groups them. It fits free text, not ceilings or counts.

**Reference:** Notion

#### `edit-in-a-side-panel-the-list-still-visible`: Edit in a Side Panel, the List Still Visible

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Trips"
    text "93 scheduled."
  region "Scheduled"
    list 3
      item: text "Rotterdam client visit" · text muted "9 Oct 2026" · status "Scheduled" info · action "Rename"
  drawer right "Rename this trip"
    field "Name" text = "Rotterdam client visit"
    field "Boards" text = "09/10/2026 06:30"
    action "Cancel"
    action primary "Save"
```

**Rationale:** The way HubSpot edits a record: the list stays on screen and the form opens beside it. Renaming twelve trips in a row means twelve saves without twelve page loads.

**Cost:** It breaks the rule that a form is a dialog or a page, never a drawer. Create and edit would then disagree about where a form lives. The panel is also too narrow for sections or steps.

**Reference:** HubSpot

### Removing Something

**ID:** `litespec/frontend/patterns/create-edit-remove#removing-something`

**Question:** Staff remove records that should no longer be active. The decision is which kind of removal each record gets: archive, delete, blocked removal with the blocker named, reversal for money, void for codes, suspend for running services, a thirty-day trash, or an undo toast. Archive is preferred over delete because it is reversible and keeps the audit trail.

**Default:** `archive-with-what-archiving-means`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Archive stays the pick. It is reversible, it keeps the audit trail, and the Archived view is the way back. The thirty-day trash is the runner-up where delete is the verb the reader reaches for. Never ship a bare delete for anything with history. The delete dialog is only for records that have to go, and it says what is kept. The Undo toast earns its place for small reversible removes such as a plan entry.

**Reviewed for:** Which built single removal differs from the wireframe its catalog entry chose? Open the built archive, delete, and trash flows beside the catalog entry recording the chosen option and its reason.

#### `archive-with-what-archiving-means`: Archive, with What Archiving Means

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Trips"
    text "93 scheduled, 4 archived."
    action "New trip"
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

**Rationale:** Archive is the default removal. The dialog says what stops being visible and what is kept, and the record keeps its place with a state that says it is archived.

**Cost:** The reader now has two ways to hide something, archive and delete. A list that shows both needs a view or a filter separating them.

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

**Rationale:** Some records must go rather than be hidden. The dialog says the record is gone for good, and it names what is kept, because the payout log still holds the decision and the time.

**Cost:** Everything the company holds has to stay reconstructable for an audit, which means 'delete' removes the operational record and keeps the evidence. Two different things under one verb.

#### `removal-blocked-naming-what-blocks-it`: Removal Blocked, Naming What Blocks It

```wireframe
page
  breadcrumb "Home" "Expenses" "Okafor Office visit"
  header
    heading "Okafor Office visit"
    text "3 Nov 2026, Rotterdam office. 1,204 expenses."
    action destructive "Archive"
  dialog "This trip cannot be archived yet"
    text "Two policy lines are still open. Open lines are promised to spenders."
    fact "Travel policy" "412 of 500 committed. Open."
    fact "Hotel night" "0 committed. Open."
    action "Close the line"
    action primary "Close both lines first"
```

**Rationale:** The command cannot run because something depends on the record. The dialog names the dependency and the command that deals with it, rather than refusing with a bare code.

**Cost:** No cited design system publishes this pattern. It is an inference from two existing cases: the Cloudscape disabled button and the explained refusal for a blocked edit.

#### `reversal-rather-than-removal-for-money`: Reversal Rather Than Removal, for Money

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Reversed" neutral
    text "Fully reversed 8 Oct 2026, 11:04, by Cara."
    action "Message spender"
  region "The money, before and after"
    fact "Paid to the spender" "EUR 125.00"
    fact "Reversed in full" "EUR 125.00"
    fact "Still to reversal" "EUR 0.00"
  region "What was claimed"
    list 2
      item: text "Travel policy, 2 lines" · text muted "Returned at 11:04" · status "Returned" neutral
```

**Rationale:** A reversal does not remove an expense. It reverses the expense and leaves the record. The spender is shown the expense before and after, so the reversal is visible as a fact rather than an absence.

**Cost:** The record grows rather than shrinks. A list of expenses has to show an expense that was fully reversed without it looking empty.

#### `void-where-the-record-must-stay-but-stop-counting`: Void, Where the Record Must Stay but Stop Counting

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "Codes"
  header
    heading "EXP-1042"
    text "2 codes, both unused."
    action destructive "Void a code"
  region "Budget codes"
    list 2
      item: text "EUR 10.00 off" · text muted "VC-0C893968A2" · status "Not used" neutral · action "Void it"
  dialog "Void this code"
    message "VC-0C893968A2 stops working at once. Ava needs a replacement." caution
    field "Send Ava the replacement now" checkbox
    field "Keep the reason on the record" checkbox
    action "Cancel"
    action destructive "Void and replace"
```

**Rationale:** This is a code that was issued and is now withdrawn. The code stops working, the code stays on the expense, and the record says why.

**Cost:** Void and reverse are different verbs for different effects and a reader has to know which is which. Voiding a code that was already used is refused outright, because the expense log holds that.

#### `suspend-for-something-that-must-keep-running`: Suspend, for Something That Must Keep Running

```wireframe
page
  breadcrumb "Home" "Policies" "Portal"
  header
    heading "Portal"
    status "Paused" caution
    text "Paused since 2 Oct 2026, 14:22."
    action primary "Take it off pause"
  region "Notice"
    message "Nothing can be claimed here. Expenses already filed still pay." caution
    action "See the message spenders get"
  region "What is still true"
    fact "Address" "expenses.harbor-pine.example"
    fact "Policies published" "48 active"
    fact "Expenses filed" "1,204, all unchanged"
  region "What a spender sees"
    text "This portal is not taking expenses at the moment. Expenses already filed still pay."
```

**Rationale:** Pause the record rather than removing it. The record keeps every property and stops accepting what it accepts. The page says what is still possible while it is paused.

**Cost:** A pause is invisible to a spender unless something tells them. The same page has to answer what happens to somebody who tries to file while this is paused.

#### `trash-with-thirty-days-to-change-your-mind`: Trash with Thirty Days to Change Your Mind

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Trips"
    text "93 scheduled, 3 in the trash."
    action destructive "Empty the trash"
  toolbar
    field "Search trips" search
  region "Notice"
    message "The trash empties itself after 30 days. Filed expenses are never in it." info
  region "Trash"
    list 3
      item: text "Okafor Office visit" · text muted "Deletes 7 Nov 2026" · action "Restore" · action destructive "Delete now"
```

**Rationale:** The way Google Drive removes: delete moves the record to a trash that empties itself after thirty days. The verb stays delete, which is what the reader reaches for, and the reversibility stays too.

**Cost:** There is still a day when the record goes for good. The trash needs its own view, its own emptying rule, and an answer for records with filed expenses.

**Reference:** Google Drive

#### `no-confirmation-one-undo-in-a-toast`: No Confirmation, One Undo in a Toast

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "Trip" "Plan"
  header
    heading "Rotterdam client visit"
    text "Trip plan, 5 entries."
    action "Add to the plan"
  region "Trip plan"
    list 3
      item: text "Meeting room booked" · text muted "08:00" · text muted "Eli"
  toast "Ben on spender messages was removed. Undo within 8 seconds." info
```

**Rationale:** The way Gmail removes mail: the command applies at once and the toast carries the way back for eight seconds. Removing a plan entry is too small for a dialog and exactly the size of an Undo.

**Cost:** It only fits what is cheap to reverse. Money, expenses, and anything the audit trail holds still need the dialog that says what happens. An Undo that expires is not a record.

**Reference:** Gmail

### Removing Several at Once

**ID:** `litespec/frontend/patterns/create-edit-remove#removing-several-at-once`

**Question:** Staff remove or reverse several records at once. The count itself is the risk, so the reader checks how many records and how much money are involved before committing. The decision is how the confirmation carries that check for small and large selections.

**Default:** `a-dialog-naming-the-count-and-the-money`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** The dialog naming the count and the money stays the pick up to about twenty-five rows. The two figures are what the reader checks. The background job is the runner-up past that size, where a report lands afterwards. Never ship the typed count for an ordinary bulk remove. The dry-run plan earns its place where rows can refuse.

**Reviewed for:** Which built bulk removal differs from the wireframe its catalog entry chose? Open the built count-and-money dialogs and background jobs beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** A bulk reversal confirmation, which lists the records and totals the money before the commit button. The count and the amount are the two things a reader must check.

**Cost:** Twenty-five rows in a dialog mean scrolling inside a dialog. Past about five rows the total carries the meaning, and few readers check each line.

#### `a-dialog-that-names-the-scope-it-is-about-to-use`: A Dialog That Names the Scope It Is About to Use

```wireframe
page
  breadcrumb "Home" "Expenses" "Reversals"
  header
    heading "Reversals"
    text "31 expenses. 25 on this page are selected."
  region "Scope"
    message "All 25 on this page are selected. 1,204 reversals match this filter in total." info
    action "Select all 1,204 instead"
  table 3 "Expense" "Spender" "Amount"
    item: text "EXP-1042" · text "Ava" · text "EUR 90.00"
  dialog "Reversal the 25 on this page?"
    text "Only these 25, out of the 1,204 reversals that match this filter."
    fact "Expense money going back" "EUR 1,204.50"
    fact "Taken off the Harbor and Pine balance" "EUR 1,204.50"
    message "6 of the 25 are older than 180 days. This method will not take those back." caution
    action "Cancel"
    action destructive "Reversal these 25"
```

**Rationale:** Where the selection could be page-scoped or filter-scoped, the dialog says which. Gmail's banner exists for exactly this and Shopify documents the same escalation.

**Cost:** Two sentences of scope in a dialog about money, and the reader has to check the number as well as the amount. Both are worth checking, and the dialog carries both.

**Reference:** Gmail

#### `bulk-removal-as-a-bulk-archive-then-a-dialog-for-the-record`: Bulk Removal as a Bulk Archive, Then a Dialog for the Record

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Trips"
    text "241 in all. 4 selected."
  toolbar
    field "Search trips" search
  region "Selection"
    text "4 selected, all completed, none scheduled."
    action "Clear"
    action "Restore"
    action primary "Archive these 4"
  region "Trips"
    list 3
      item: text "Okafor Office visit" · text muted "9 Oct 2026, completed" · action "Open"
```

**Rationale:** Archive is cheap and reversible, so a bulk archive needs only a count in the selection bar, not a dialog listing every record.

**Cost:** Archiving 200 trips hides 200 trips, and the reader has no way back except an Archived view. The safety net is the view, not the dialog.

#### `a-partial-bulk-refusal-some-rows-will-not-do-it`: A Partial Bulk Refusal: Some Rows Will Not Do It

```wireframe
page
  breadcrumb "Home" "Expenses" "Reversals"
  header
    heading "Reversals"
    text "31 expenses. 3 selected."
  table 3 "Expense" "Spender" "Amount"
    item: text "EXP-1042" · text "Ava" · text "EUR 90.00"
  dialog "Reversal 1 of the 3?"
    text "Two of these are outside the reversal window, so the bank will refuse them."
    message "EXP-1042, EUR 90.00. Ava, 6 days old. The bank will take this one." positive
    message "EXP-1040, EUR 45.00. Elin Lindqvist, 181 days old. Outside the window." critical
    message "EXP-1038, EUR 45.00. Ade Okafor, 214 days old. Outside the window." critical
    action "Cancel"
    action destructive "Reversal the one that works"
```

**Rationale:** When some of the selection cannot take the command, the dialog says which and why before anything runs, and offers to do the rest.

**Cost:** A partial result has to be reportable afterwards as well, so the reader ends up reading the same information twice. Better to refuse the whole batch and let them narrow it.

#### `a-typed-confirmation-for-a-bulk-removal`: A Typed Confirmation for a Bulk Removal

```wireframe
page
  breadcrumb "Home" "Settings" "Data imports" "IMP-4F82C1A9"
  header
    heading "Data imports"
    text "4 imports. 1 holds 1,988 expenses."
    action destructive "Undo this import"
  region "What was imported"
    fact "Expenses created" "1,988"
    fact "Expense lines created" "2,361"
    fact "Amount paid to anybody" "EUR 0.00"
  dialog "Undo this import?"
    text "This removes 1,988 expenses and 2,361 expense lines."
    field "Type 1,988 to confirm" text
    action "Keep the import"
    action disabled "Undo the import"
```

**Rationale:** Where a bulk removal is genuinely dangerous, type the count. The number is the thing being confirmed, not the name of one record.

**Cost:** Typing 200 trains copy and paste, which is the reflex the pattern exists to break. Cloudscape's own restriction is to the most dangerous object, and this is many objects.

#### `a-bulk-removal-that-is-a-cancellation-with-consequences-per`: A Bulk Removal That Is a Cancellation, with Consequences per Row

```wireframe
page
  breadcrumb "Home" "Expenses" "Plans" "March trip plan"
  header
    heading "March trip plan"
    text "Four trips, 214 lines committed between them."
    action destructive "Cancel the plan"
  table 3 "Trip" "Date" "Committed"
    item: text "Trip 8" · text "27 Oct 2026" · text "86"
  dialog "Cancel the March trip plan?"
    text "86 lines were paid on trip 8. That trip cannot be called back."
    text "128 lines on trips 9 and 10 would be reversed, EUR 5,760.00 in total."
    message "The 86 paid lines are with the spenders. Nothing here can undo that." critical
    action "Keep the plan"
    action destructive "Cancel the plan"
```

**Rationale:** Canceling a plan of trips is one command with several effects, and the dialog lists them per row rather than as a total. What the reader needs is not the sum but which trips have spenders.

**Cost:** A dialog that varies per row is a table in a dialog, which is a page. Past a handful of rows, the consequence list belongs on a page of its own.

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

**Rationale:** The way Shopify runs bulk operations: past a size no dialog can carry, the selection runs as a job, and a report lands afterwards. Reversing 1,204 expenses cannot be checked on screen, so it is checked in the report instead.

**Cost:** The reader walks away and comes back, so the job needs progress, a cancel, and a report that names every row it refused. A job that fails halfway also needs a rule for what already ran.

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

**Rationale:** This option works the way Terraform plans before it applies. A page says what each row will do, so the refusals are read before anything runs rather than after. Three rows fit, and three hundred still fit, because it is a page.

**Cost:** The plan can go stale between reading and applying: a payout can settle, and a window can close. The apply step must recheck every row and say which answer changed.

**Reference:** Terraform

### Creating a Record and Then Filling It In

**ID:** `litespec/frontend/patterns/create-edit-remove#creating-a-record-and-then-filling-it-in`

**Question:** A new record such as a trip needs a name, a date, a site, a priced fare, and a cost centre before it can file, but staff may not know all five at once. The decision is how much a create requires up front and how the record gets finished: name-only draft, everything required, series defaults, checklist, wizard, nothing until publish, home-page guide, or point-of-use nudges.

**Default:** `create-with-a-name-fill-the-rest-later`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Create with a name and fill the rest later stays the pick. The record exists as a draft and every later addition is its own audited command. The home-page setup guide is the runner-up and the required companion, because a draft nobody finishes is a dead record. Never ship the everything-required option or the nothing-until-publish option. The point-of-use nudge earns its place beside the checklist for readers who skip guides.

**Reviewed for:** Which built draft-first flow differs from the wireframe its catalog entry chose? Open the built name-first creates and setup guides beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The name is the only required field, and the record exists as a draft. Every later addition is its own audited and reconstructable command.

**Cost:** A draft trip is nearly useless on its own. If nothing prompts the reader to finish it, it sits unfinished on a list. The home-page setup guide is what prompts the reader to finish it.

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

**Rationale:** The record is not created until it is whole: name, date, site, one fare, and one cost centre. Nothing half-made is ever in a list.

**Cost:** It asks for a five-part form before anything exists, and a reader who only wants to pencil in a date has to do all of it.

#### `create-from-a-series-where-the-series-holds-the-parts`: Create from a Series, Where the Series Holds the Parts

```wireframe
page
  breadcrumb "Home" "Expenses" "Plans" "March trip plan" "New trip"
  header
    heading "New trip"
    text "The March trip plan runs four trips. This is the fifth."
  region "What this trip takes from the plan"
    fact "Site" "Rotterdam client site. Shared."
    fact "Policies" "Travel policy, Hotel night. Shared."
    fact "Trip plan" "6 entries. Shared."
    fact "Team" "Nobody assigned for this date. Not shared."
  region "What this trip sets"
    field "Date" text = "24/11/2026"
    field "Boards" text = "06:30"
    action "Cancel"
    action primary "Create the trip"
```

**Rationale:** The plan carries the site, the trip plan, and the fares, and each trip is created already holding them. One create makes many records with no repetition.

**Cost:** A trip created from a plan can be edited away from it, and then the two disagree. The plan has to state which fields are shared and what diverging means.

#### `a-checklist-that-closes-itself`: A Checklist That Closes Itself

```wireframe
page
  breadcrumb "Home" "Expenses" "Rotterdam client visit"
  header
    heading "Rotterdam client visit"
    status "Draft" neutral
    text "14 Mar 2026, boards 06:30, Rotterdam client site."
    action primary "Publish"
  region "Before this can file"
    field "Name and description" checkbox = "Done"
    field "Date, hours and site" checkbox = "Done"
    field "A fare, priced" checkbox = "Done"
    field "List it on the portal" checkbox
    action "List it"
    field "Write the payout plan" checkbox
    action "Set it up"
```

**Rationale:** After creation, the page is a checklist and each item is a command. Finishing the last item removes the checklist rather than leaving a ticked list.

**Cost:** A checklist on every draft is a page of work before anything is usable. It is one more place that has to know which commands are already done.

#### `create-inside-a-wizard-that-ends-at-publish`: Create Inside a Wizard That Ends at Publish

```wireframe
page
  breadcrumb "Home" "Expenses" "New trip"
  header
    heading "New trip"
    text "Step 2 of 4, the fares. Stopping now keeps a draft."
  region "The fares"
    status "Priced" positive
    field "Amount, in euros" number = "45.00"
    field "Lines" number = "500"
    action "Add a second fare"
  region "Steps"
    text "1 The trip, done. 2 The fares, now. 3 Who pays. 4 Publish."
    action "Save and close"
    action "Back"
    action primary "Continue"
```

**Rationale:** One flow from nothing to published: the trip, its fares, its cost centre, and the publish step. The reader never lands on a draft they have to finish by hand.

**Cost:** Carbon says simplify before a wizard, and a stepper is only for a linear flow of three or more steps. Publishing is linear, so this is the case a stepper exists for.

**Reference:** Carbon

#### `nothing-created-until-the-reader-says-publish`: Nothing Created Until the Reader Says Publish

```wireframe
page
  breadcrumb "Home" "Expenses" "New trip" "Publish"
  header
    heading "Publish Rotterdam client visit"
    text "Nothing has been saved yet. This is the last step."
  region "What will exist when you publish"
    fact "Trip" "Rotterdam client visit, 14 Mar 2026"
    fact "Place" "Rotterdam client site, 40 receipts"
    fact "Fare" "Train fare, EUR 45.00"
    fact "Count" "One fare, 500 lines"
  region "Where it will publish"
    fact "Portal" "expenses.harbor-pine.example, published"
    action "Back to the form"
    action primary "Publish it"
```

**Rationale:** The form holds everything in the browser and writes nothing until the last step. It makes one command, one audit entry, and one record per creation.

**Cost:** Losing the browser loses the work, and a five-step form held in memory is a lot to lose. The record's state during the form does not exist, so nothing else can read it.

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

**Rationale:** The way Stripe onboards: the home page carries the setup as a guide with a progress bar, not as one more list. A reader who has finished three of five sees two left every time they sign in.

**Cost:** It sits at the top of the home page until it is done, and a guide nobody dismisses becomes a banner nobody reads. It needs a dismiss that stays dismissed per company, not per person.

**Reference:** Stripe

#### `nudges-at-the-point-of-use-not-a-checklist`: Nudges at the Point of Use, Not a Checklist

```wireframe
page
  breadcrumb "Home" "Expenses" "Rotterdam client visit"
  header
    heading "Rotterdam client visit"
    status "Draft" neutral
    text "Sat 14 Mar 2026, boards 06:30, Rotterdam client site."
  region "Publish"
    action disabled "Publish"
    message "Two things are missing: a priced fare and a channel live." neutral
  region "Fares"
    text "No fare yet. A spender cannot claim what has no ceiling."
    action primary "Add a fare"
  region "Portal"
    text "Nothing is published, so the portal shows the trip as coming soon."
```

**Rationale:** This option works the way Webflow gates publish. No checklist page: the missing piece is named where it is missing. The publish button says what it needs, and the section that needs it says so too.

**Cost:** Five missing pieces mean five separate discoveries, and nothing tells the reader how much is left. It fits one gap, not a new company with five.

**Reference:** Webflow

### Changing Something That Has Already Happened

**ID:** `litespec/frontend/patterns/create-edit-remove#changing-something-that-has-already-happened`

**Question:** A record of what is true now cannot always be edited: an expense already paid must stay reconstructable for billing and audits. The decision is which shape each after-the-fact change takes: old value kept beside the new, refusal with the newer way named, dated correction, scheduled window, scoped change, reversal, version history, or two-person approval.

**Default:** `the-old-value-stays-on-the-record-beside-the-new`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keeping the old value beside the new stays the pick. An expense filed last month still resolves to the trip it was filed for. The dated correction is the runner-up where a fact was entered wrong, and the reversal is the only shape for money. Never ship a silent overwrite of anything priced or paid. Where no safe edit exists, the refusal with the newer way named is the option. The version history earns its place for names and text, with two-person approval for corrections to money.

**Reviewed for:** Which built correction flow differs from the wireframe its catalog entry chose? Open the built past-change edits, reverses, and version histories beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** A rename shows what it was and when. The record never loses the earlier name, because an expense filed last month must still resolve to the trip it was filed for.

**Cost:** Every edit grows the record, and a reader looking for 'what is this called' has to skip past what it used to be called.

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

**Rationale:** Some edits cannot happen at all after the fact. The command is not disabled silently: it says what cannot change, and names the command that achieves what the reader was trying to do.

**Cost:** A reader who wants a ceiling change on a claimed line is told no and offered the replacement command instead. That is the correct answer, but refusals still generate support cases.

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

**Rationale:** Where a figure was entered wrong, the correction is a statement about the past. The record carries the correction as its own dated fact rather than overwriting the wrong one.

**Cost:** Two figures for the same thing confuse any page that shows only one of them. Every read has to choose which to show, and the choice is a rule rather than a display decision.

#### `a-change-with-a-window-before-it-takes-effect`: A Change with a Window, Before It Takes Effect

```wireframe
page
  breadcrumb "Home" "Policies" "Late fee"
  header
    heading "Late fee"
    text "What the spender pays on top of the expense. You keep all of it."
  region "The fee"
    fact "Now, since 1 Oct 2026" "EUR 5.00"
    fact "From 1 November 2026, scheduled" "EUR 6.00"
    field "Change the scheduled fee, in euros" number = "6.00"
    text muted "Takes effect on 1 November 2026. The 1,204 expenses already filed keep EUR 5.00."
    action "Cancel"
    action primary "Schedule the change"
```

**Rationale:** A scheduled change names what it will be, when it takes effect, and how to stop it before then. The record shows both the current value and the one coming.

**Cost:** Two values on one record is a rule every read honors. What this is worth carries a date that a reader has to notice.

#### `a-change-that-only-reaches-what-has-not-shipped`: A Change That Only Reaches What Has Not Paid

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "Trip" "Plan"
  header
    heading "Trip plan"
    text "6 entries, sorted by start time."
    action "Add to the plan"
  region "Scope"
    message "This is this trip only. A change here does not reach any paid trip." info
  region "Entries"
    list 3
      item: text "Receipt check" · text muted "06:30" · text muted "Changed 8 Oct, 14:22" · action "More actions"
```

**Rationale:** The edit page says which population the change reaches, and which it cannot. Editing a trip plan changes this trip. Editing the office does not.

**Cost:** It needs a read that counts the affected population. That count is a fact the API answers rather than a number typed into the copy.

#### `a-reverse-keeping-both-figures-and-the-reason`: A Reverse, Keeping Both Figures and the Reason

```wireframe
page
  breadcrumb "Home" "Billing" "Mismatches" "M-0091"
  header
    heading "Mismatch M-0091"
    text "Expense EXP-1042, EUR 45.00 recorded twice."
    action destructive "Reverse the second"
  region "What the two records say"
    fact "Recorded by the portal" "EUR 45.00"
    fact "Recorded by the bank file" "EUR 45.00"
    fact "Counted so far" "EUR 90.00"
    fact "After the reversal" "EUR 45.00"
  region "Why"
    field "Reason" textarea = "The bank file imported the same payout twice."
    action "Cancel"
    action destructive "Reverse the second record"
```

**Rationale:** A charge that turns out to be wrong is reversed with a reason, and both figures stay on the record. This is the shape every financial correction takes.

**Cost:** The record holds two values for one thing, and every statement has to decide which one to print. That decision is a rule, not a display preference.

#### `a-version-history-with-restore`: A Version History with Restore

```wireframe
page
  breadcrumb "Home" "Expenses" "Rotterdam client visit" "Versions"
  header
    heading "Rotterdam client visit"
    text "Sat 14 Mar 2026, Rotterdam client site. 1,204 expenses."
  region "Notice"
    message "Restoring writes a new version. Expenses keep the name they were filed under." info
  region "Versions"
    list 3
      item: text "Rotterdam March trip" · text muted "Version 11, 12 Jan 2026, 09:04 by Ben" · action "Restore"
```

**Rationale:** This option works the way Notion keeps versions. Every save is a version with a who and a when. Restoring writes a new version rather than rewriting the old. A name changed in October reads in June as what it was then.

**Cost:** Versions multiply: a description edited fifty times has fifty rows, and only the last few matter. The list needs grouping by day, or it hides the one version anyone wants.

**Reference:** Notion

#### `two-people-for-a-change-to-the-past`: Two People for a Change to the Past

```wireframe
page
  breadcrumb "Home" "Billing" "Mismatches" "M-0091" "CR-0031"
  header
    heading "Correction CR-0031"
    text "Mismatch M-0091, expense EXP-1042. Waiting on one approval."
  region "What was asked"
    fact "Asked by" "Cara, 8 Oct 2026, 09:12"
    fact "Change" "Reverse the second EUR 45.00 record"
    fact "Reason" "The bank file imported the same payout twice."
  region "Rule"
    message "Cara asked for this correction, so Cara cannot approve it." caution
  region "Decide"
    field "Note for the record" textarea
    action "Refuse"
    action primary "Approve and reverse"
```

**Rationale:** The way GitHub guards a protected branch: a change to what is settled needs a second person. A correction to money that one person could type alone is the case this exists for.

**Cost:** Every correction waits on someone else, and a team of one cannot use it at all. It fits corrections to money and the payout log, not names and text.

**Reference:** GitHub

### Duplicating a Record

**ID:** `litespec/frontend/patterns/create-edit-remove#duplicating-a-record`

**Question:** A duplicate is a create that starts from something. The copy carries the setup and never the history: ceilings and a trip plan travel, and expenses and money stay with the original.

**Default:** `a-dialog-that-says-what-the-copy-carries`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** The dialog that says what the copy carries is the pick: a duplicate that silently drops the ceilings is worse than no duplicate. The carry checklist is the runner-up where the parts vary per company. Never ship one-click duplication for a trip. It earns its place for budget codes and plan entries, which carry no spenders.

An expense is never duplicated, because it is one spender's claim.

**Reviewed for:** Which built copy flow differs from the wireframe its catalog entry chose? Open the built duplicate dialogs and row-menu copies beside the catalog entry recording the chosen option and its reason.

#### `a-dialog-that-says-what-the-copy-carries`: A Dialog That Says What the Copy Carries

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Trips"
    text "93 scheduled."
    action "New trip"
  toolbar
    field "Search trips" search
  region "Trips"
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

**Rationale:** The way Shopify duplicates a product: the dialog names what the copy carries before it exists. The copy starts as a draft, so nothing about it is active until the reader says so.

**Cost:** A long carried list makes a long dialog, and most of it is read once. Past about six parts the checklist option carries the meaning better.

**Reference:** Shopify

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

**Rationale:** The way Mailchimp duplicates a campaign: one menu item with no questions, and the copy lands as a draft, confirmed by a toast. A budget code carries no spenders, so there is nothing to check first.

**Cost:** No moment to rename or choose what travels. It fits records with no spenders and no money. A trip duplicated this way would surprise everyone in June.

**Reference:** Mailchimp

#### `copy-of-renamed-where-it-lands`: Copy of, Renamed Where It Lands

```wireframe
page
  breadcrumb "Home" "Policies" "Travel policy" "Codes"
  header
    heading "Budget codes"
    text "2 codes. 412 claimed between them."
    action "New budget code"
  region "Codes"
    list 3
      item: field "Name of the copy" text = "Copy of Standard" · text muted "EUR 45.00 · 0 claimed · draft, not active" · action "Keep the name"
    text "Standard at EUR 45.00 with 412 claimed and Large at EUR 60.00 sit below."
```

**Rationale:** The way Figma duplicates a layer: the copy appears beside the original as Copy of, already selected for rename. Duplicating a budget code is renaming plus a ceiling check, and both happen in the list.

**Cost:** The copy exists before it is named, so a list can fill with Copy of Standard rows that nobody renamed. It needs the draft state to say the copy is not active yet.

**Reference:** Figma

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

**Rationale:** The way Notion duplicates a page with its subpages offered as toggles: every part is a checkbox with its size beside it. A reader who wants the ceilings but not the team says so here.

**Cost:** Six checkboxes for a routine duplicate is five too many on the tenth use. It fits the first duplicate of a quarter, not the weekly one.

**Reference:** Notion

#### `duplication-refused-where-there-is-nothing-to-carry`: Duplication Refused Where There Is Nothing to Carry

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    text "Ava · EUR 125.00 · paid 8 Oct 2026, 09:32."
  message critical "An expense cannot be duplicated. EXP-1042 is the claim Ava made. A copy would claim a payout that never happened."
  region "What to do instead"
    text "To claim the same lines for someone else, create a manual expense. It carries its own number and its own payout."
    action primary "Create a manual expense"
```

**Rationale:** The way Stripe treats a paid invoice as a fact: it can be credited but never copied. An expense is one spender claim for one set of lines, so duplicating it would invent a claim that never happened.

**Cost:** The reader who wants a second expense like this one must build it by hand. The manual expense is the way out. This page must name it, or the refusal leaves the reader with nowhere to go.

**Reference:** Stripe

#### `duplicate-a-session-inside-its-series`: Duplicate a Session Inside Its Series

```wireframe
page
  breadcrumb "Home" "Expenses" "Plans" "March trip plan" "Trip 9" "Duplicate"
  header
    heading "Duplicate trip 9"
    text "March trip plan · Tue 3 Nov 2026. The copy joins the same series."
  region "The copy takes from the series"
    fact "Place" "Rotterdam client site, shared"
    fact "Policies" "Travel policy, Hotel night, shared at EUR 45.00"
    fact "Trip plan" "Shared, 6 entries"
    field "Date of the copy" text = "24/11/2026"
    text "Trip 9 moved its boarding slot to 19:30. The copy takes the series 20:00, not the change."
  footer
    action "Cancel"
    action primary "Duplicate the trip"
```

**Rationale:** The series holds what every trip shares, so the duplicate only asks for the date. The shared parts are named, not chosen, because the series owns them.

**Cost:** Anything the trip changed away from the series raises the question of which version travels. The page must say the series version wins, or the copy silently keeps the divergence.

### Putting a List in Order by Hand

**ID:** `litespec/frontend/patterns/create-edit-remove#putting-a-list-in-order-by-hand`

**Question:** Policies, plan entries, and portal sections all have an order the reader sets. The spender reads the order the reader left, so the control must work for a mouse, a keyboard, and a finger.

**Default:** `drag-handles-in-the-list`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Drag handles in the list are the pick: direct, visible, and the order saves as it changes. Move up and Move down in the row menu are the required companion, because drag alone leaves keyboard and screen-reader users behind. Never ship typed position numbers alone. Two rows both numbered 3 are a question the page cannot answer.

The portal preview earns its place for sections, where the order is the design.

**Reviewed for:** Which built manual ordering differs from the wireframe its catalog entry chose? Open the built drag handles, move commands, and pinned rows beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The way Notion reorders blocks: a grip sits on every row, the dragged row lifts, and a line shows where it will land. The order saves on drop, so there is nothing to forget.

**Cost:** A grip cannot be used from a keyboard or announced by a screen reader, and on a phone it conflicts with scrolling. The menu move is not an alternative. It is the other half of this option.

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

**Rationale:** The way Linear moves an issue with the keyboard: the same move drag performs, as a named command. A screen reader announces Moved to position 2, and nothing about it needs a pointer.

**Cost:** Moving one row past twenty others is twenty menu opens. It is the accessible half of drag, not a replacement for it on long lists.

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

**Rationale:** The way WordPress expenses menu items: a number on each row, one save for the whole list. Jumping a row from 14 to 1 is one keystroke where dragging would cross the whole list.

**Cost:** Two rows numbered 3 are a question the page cannot answer, and renumbering fourteen rows by hand is slower than dragging one. Numbers suit a jump across a long list and nothing else.

**Reference:** WordPress

#### `automatic-order-with-pinned-rows-on-top`: Automatic Order, with Pinned Rows on Top

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Trips"
    text "93 scheduled. Sorted by date, 2 pinned."
  toolbar
    field "Search trips" search
    field "Sort" select = "Date"
  region "Trips"
    list 4
      item: text "Rotterdam client visit" · text muted "Sat 14 Mar 2026 · pinned by Ben" · status "Pinned" info · text "412 lines" · action "Unpin"
    text "Becker Bouw trip stays pinned below. Dated rows follow the sort."
    text "Pinned rows stay first in every sort. Unpinning returns a row to the rule."
```

**Rationale:** The way Shopify sorts products: the list follows a rule such as price or date, and pinned rows stay above it. Ninety-three trips need no hand ordering. The two that matter stay first by hand.

**Cost:** Two orders on one list is a rule every reader must learn: pinned first, then the rule. A reader who sorts by name and finds two rows out of place has to be told why.

**Reference:** Shopify

#### `order-the-storefront-sections-on-the-preview`: Order the Portal Sections on the Preview

```wireframe
page
  breadcrumb "Home" "Policies" "Portal"
  header
    heading "Portal"
    text "expenses.harbor-pine.example · what spenders read, in order."
  split 1:1
    region "Sections"
      list 3
        item: text "2" · text "Travel policy" · text muted "412 claimed · active"
      text "Drag a section. The preview follows."
    region "Spender preview"
      heading small "Spring rates"
      text "From EUR 40.00 · ends 1 Feb."
      heading small "Travel policy"
      text "EUR 45.00 · 412 claimed."
```

**Rationale:** The way Squarespace orders page sections: list and preview sit side by side, and moving a row moves the preview with it. The order is the design, so it is judged as one.

**Cost:** Two panes need a wide screen. Below 768 px the preview stacks under the list and the judgement breaks. It also fits sections, not policies, which spenders read as a list either way.

**Reference:** Squarespace

#### `move-to-top-and-move-to-bottom-for-long-lists`: Move to Top and Move to Bottom for Long Lists

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "Trip" "Plan"
  header
    heading "Rotterdam client visit"
    text "Trip plan · 6 entries."
  region "Trip plan"
    list 3
      item: text "Meeting room booked" · text muted "Moved to top by Ben · position 1 of 6" · text "08:00" · menu "More"
    text "Receipt check at 06:30 and four more entries sit below."
    text "Jumps save at once, like every other move."
  popover "More"
    action "Move up"
    action "Move down"
    action "Move to top"
    action "Move to bottom"
```

**Rationale:** The way Trello positions a card: a position menu with top and bottom jumps beside the step moves. On a plan of forty entries the jump is the only move anyone uses.

**Cost:** Top and bottom move without aim: everything important ends up first and the top becomes its own unordered list. It needs the step moves beside it for the ordering that follows.

**Reference:** Trello

### Restoring an Archived Record

**ID:** `litespec/frontend/patterns/create-edit-remove#restoring-an-archived-record`

**Question:** Archive is only safe if restore works. A restore returns the record to where it was, with its history, and says so when something changed while it was away, such as a name taken since.

**Default:** `restore-from-the-archived-view`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Restore from the archived view is the pick: the view is the way back, and the row says where the record returns to. The record-page banner is the runner-up for anyone arriving by link. Never ship a restore that silently un-archives parents or overwrites a newer name. Both need their own dialog.

The Undo toast earns its place in the seconds after archiving, not as the way back days later.

**Reviewed for:** Which built restore flow differs from the wireframe its catalog entry chose? Open the built archived views and undo toasts beside the catalog entry recording the chosen option and its reason.

#### `restore-from-the-archived-view`: Restore from the Archived View

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Trips"
    text "93 scheduled, 4 archived."
    action "New trip"
  toolbar
    field "Search trips" search
  region "Archived"
    list 3
      item: text "Okafor Office visit" · text muted "28 Feb 2026 · archived 2 Oct" · text "Returns as Scheduled" · action "Restore"
    text "A restore returns the record to the state it held. Nothing goes back live by itself."
  toast positive "Okafor Office visit is back under Scheduled."
```

**Rationale:** The way Shopify restores an archived product: the Archived view lists what is hidden, and each row carries its own Restore. The record returns to the state it held, not to a draft.

**Cost:** Nobody opens the Archived view unless they remember it exists. Anything restored from here must say where it went, or the record vanishes from this list into another nobody is reading.

**Reference:** Shopify

#### `undo-in-the-seconds-after-archiving`: Undo in the Seconds After Archiving

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Trips"
    text "93 scheduled, 4 archived."
    action "New trip"
  toolbar
    field "Search trips" search
  region "Trips"
    list 2
      item: text "Rotterdam client visit" · text muted "9 Oct 2026 · Rotterdam client site" · status "Scheduled" info · text "1 of 40 receipts"
    text "Okafor Office visit left this list a moment ago. Undo is fastest where the archive happened."
  toast info "Okafor Office visit was archived. Undo."
```

**Rationale:** The way Gmail keeps an Undo on every archive: the toast is the way back in the seconds after the selection. Archiving the wrong trip is fixed where it happened, without a visit to another view.

**Cost:** Eight seconds and it is gone, so this is a correction for slips, not the way back. Anyone restoring a day later still needs the archived view.

**Reference:** Gmail

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

**Rationale:** The way GitHub reopens a closed issue from the issue itself: the archived banner sits on the record, with Restore beside it. Anyone arriving by link or search restores without finding the archived view first.

**Cost:** The page must work while archived: read-only where it matters, with every command saying it needs a restore first. A banner on a broken page is worse than no banner.

**Reference:** GitHub

#### `restore-refused-the-parent-is-still-archived`: Restore Refused: The Parent Is Still Archived

```wireframe
page
  breadcrumb "Home" "Expenses" "Okafor Office visit" "Codes"
  header
    heading "Budget codes"
    text "Okafor Office visit · archived 2 Oct."
  region "Codes"
    list 1
      item: text "Standard" · text muted "EUR 45.00 · 0 committed · archived" · action "Restore"
  dialog
    heading "Restore Standard as well as its trip?"
    text "Standard belongs to Okafor Office visit, which is still archived. A budget code cannot return while its trip stays hidden."
    text "Restoring both returns the trip as Scheduled and the code with it. Neither goes back live by itself."
    action primary "Restore both"
    action "Restore the trip first"
    action "Cancel"
```

**Rationale:** The way Jira refuses to reopen a subtask whose parent is closed: the restore names the blocker and offers the way out. A budget code cannot return while its trip stays archived.

**Cost:** It asks for two dialogs for one restore, and the offer to restore both at once is a second command hiding inside the first. It must say both are restored, not only the row that was selected.

**Reference:** Jira

#### `restore-with-a-name-taken-since`: Restore with a Name Taken Since

```wireframe
page
  breadcrumb "Home" "Policies" "Travel policy" "Codes"
  header
    heading "Budget codes"
    text "2 codes active. 1 archived."
  region "Codes"
    list 2
      item: text "Standard" · text muted "EUR 45.00 · 0 claimed · archived 2 Oct" · action "Restore"
    text "Standard at EUR 45.00 with 412 claimed stays active above."
  dialog
    heading "Standard is already taken"
    text "Another code took the name while this one was archived. The restored code comes back renamed."
    field "Name on return" text = "Standard (restored)"
    action "Cancel"
    action primary "Restore it renamed"
```

**Rationale:** The way Notion renames a restored page when the title is taken: the restore keeps both records by renaming the restored record. Two codes called Standard would disagree on the expense, so only one keeps the name.

**Cost:** The restored record comes back under a name nobody chose, and renaming it again is a second command. The dialog must show the name it will hold, not only the clash.

**Reference:** Notion

#### `restore-several-at-once`: Restore Several at Once

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Trips"
    text "4 archived. 2 selected."
  toolbar
    field "Search trips" search
  region "Archived"
    list 3
      item: field "Select Okafor Office visit" checkbox · text "Okafor Office visit" · text muted "28 Feb 2026 · returns as Scheduled"
    text "Lindqvist Studio trip returns as Scheduled. Becker Bouw trip returns as Ended."
    action "Clear"
    action primary "Restore these 2"
  dialog
    heading "Restore 2 trips?"
    text "Both return as Scheduled, with their plans and ceilings. Neither goes back live by itself."
    action "Cancel"
    action primary "Restore both"
```

**Rationale:** The way Google Drive restores a selection: the count is the confirmation, and every record returns to where it was. Archiving a whole quarter by mistake is fixed by selecting the same records and restoring them together.

**Cost:** A bulk restore can return records to states the reader forgot, including one that clashes. The report afterwards must name each record and where it landed.

**Reference:** Google Drive

### Inviting a Team Member

**ID:** `litespec/frontend/patterns/create-edit-remove#inviting-a-team-member`

**Question:** Trip crew join per trip, and staff join per company. An invitation names the person, the role, and the scope before sending, because wrongly granted access must be found and removed later.

**Default:** `invite-by-email-with-the-role-chosen-first`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Invite by email with the role chosen first is the pick: person, role, and scope are all named on one dialog. The pending list with resend and revoke is the runner-up and the required companion, because an invitation waits far longer than sending takes. Never ship an open invite link for staff roles.

It earns its place only for trip crew on one trip. A request to join is refused: the company invites. Nobody applies.

**Reviewed for:** Which built join flow differs from the wireframe its catalog entry chose? Open the built email invites, links, and pending lists beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The way Slack invites: one dialog, the email, and the role with what it can do beside it. The scope line says whether Noa joins the company or one trip, before the invitation exists.

**Cost:** It sends one invitation per person, typed by hand. Assigning forty temps for a quarter needs the link option beside it.

**Reference:** Slack

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

**Rationale:** The way Discord invites: a link with an expiry and a use count, for forty temps nobody wants to type by hand. The link joins one trip in one role, and nothing else.

**Cost:** Anyone with the link joins, including anyone it was forwarded to. It fits trip crew on one trip and is refused for owner, finance, and manager roles.

**Reference:** Discord

#### `invite-to-one-delivery-not-the-company`: Invite to One Trip, Not the Company

```wireframe
page
  breadcrumb "Home" "Settings" "Team" "Invite"
  header
    heading "Invite Noa Vermeer"
    text "Front desk · Sat 14 Mar 2026. Temp for one trip."
  region "The grant"
    text "Noa Vermeer · noa.vermeer@example.com."
    field "Role on the trip" select = "Temp"
    field "Trip" select = "Rotterdam client visit"
    text "After 15 Mar 2026, 18:00 this grant ends by itself. Nothing about Noa stays except the audit trail."
  footer
    action "Cancel"
    action primary "Send the invitation"
```

**Rationale:** The way Linear invites to one team rather than the workspace: the scope is chosen up front and printed on the invitation. Noa works the front desk on 14 Mar and sees nothing else afterwards.

**Cost:** Scopes multiply: sixty trips a year is sixty small grants to read later. The team page must group by person as well as by trip, or nobody can answer what Noa can do.

**Reference:** Linear

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

**Rationale:** The way GitHub lists organization invitations: every waiting invitation shows its age, its expiry, and its way out. An invitation is read far more often than it is sent, so the list is the pattern.

**Cost:** A list of the waiting is a list of the neglected: half of it expired quietly. Expired rows must say they expired and offer reissue, or the list fills with rows that can no longer be used.

**Reference:** GitHub

#### `an-expired-invitation-reissued`: An Expired Invitation, Reissued

```wireframe
page
  breadcrumb "Home" "Settings" "Team"
  header
    heading "Team"
    text "6 people. 2 invitations waiting."
    action "Invite someone"
  tabs "People 6" "Invitations 2" "Roles 6"
    region "Invitations"
      list 1
        item: text "lena.bakker@example.com" · text muted "Support · Harbor and Pine · sent 1 Oct, expired 8 Oct" · status "Expired" neutral · action "Reissue"
  dialog
    heading "Reissue the invitation?"
    text "The link from 1 Oct stays dead. A new invitation goes to lena.bakker@example.com as Support for Harbor and Pine, expiring 15 Oct."
    field "Note with it" text = "The first one expired before you opened it."
    action "Cancel"
    action primary "Reissue it"
```

**Rationale:** The way Google Workspace reissues an expired invite: the old link stays dead and a new one goes out with fresh expiry. Reissuing is a new invitation with the old role, not the old link made live again.

**Cost:** It sends two emails for one join, and the first still reads as live to anyone who kept it. The expired link must land on a page that says it expired, not on a bare refusal.

**Reference:** Google Workspace

#### `a-request-to-join-approved-by-an-owner`: A Request to Join, Approved by an Owner

```wireframe
page
  breadcrumb "Home" "Settings" "Team"
  header
    heading "Team"
    text "6 people. 1 request waiting."
  tabs "People 6" "Invitations 2" "Requests 1"
    region "Requests"
      message caution "This back office has no join request. This queue would let strangers ask for access to money."
      list 1
        item: text "unknown@example.com" · text muted "Asked 8 Oct 2026 · wants Temp for Rotterdam client visit" · status "Waiting" caution · action "Approve" · action "Refuse"
      text "The refused shape: invite the team instead."
```

**Rationale:** The way Slack handles join requests: the asker waits in a queue and an owner approves with the role. It is drawn here to be refused, because the back office has no join request and adding one would invite strangers to ask.

**Cost:** It is a queue nobody asked for, and every request is a stranger asking for access to money. The company invites its team. Nobody applies. This option stays in the library as the refused shape.

**Reference:** Slack

### Publishing and Unpublishing a Policy

**ID:** `litespec/frontend/patterns/create-edit-remove#publishing-and-unpublishing-a-policy`

**Question:** A draft is private until spenders can see it. Publishing says what spenders will find, and unpublishing says what happens to what they found, because filed expenses outlive either command.

**Default:** `publish-with-the-readiness-check-beside-it`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Publish with the readiness check beside it is the pick: the reader reads what spenders will find before they find it. Per-channel publish is the runner-up for companies that publish in more than one place. Never ship a silent unpublish. Filed expenses stay valid and the page must say so.

Pause earns its place where the policy stays visible but nothing may be claimed.

**Reviewed for:** Which built policy-state flow differs from the wireframe its catalog entry chose? Open the built publish, pause, and per-channel controls beside the catalog entry recording the chosen option and its reason.

#### `publish-with-the-readiness-check-beside-it`: Publish with the Readiness Check Beside It

```wireframe
page
  breadcrumb "Home" "Policies" "Travel policy"
  header
    heading "Travel policy"
    status "Draft" neutral
    text "EUR 45.00 · 500 lines."
    action primary "Publish"
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

**Rationale:** The way Webflow publishes: the dialog lists what is ready and the button commits only when all of it is. Publishing is the one create the reader cannot undo quietly, so the check is the dialog.

**Cost:** A check that reads five systems can disagree with the page behind it by the time the reader presses the button. The publish command must recheck and refuse with the missing piece named.

**Reference:** Webflow

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

**Rationale:** The way Shopify unpublishes a sales channel: the dialog says what stops and what stays, because filed expenses outlive the policy. Unpublishing stops filing. It touches nothing claimed.

**Cost:** It states two truths on one dialog, and the second is the one spenders will ask about. The page behind it must keep answering for the claimed lines after the policy is gone.

**Reference:** Shopify

#### `publish-on-a-date-set-in-advance`: Publish on a Date, Set in Advance

```wireframe
page
  breadcrumb "Home" "Policies" "Travel policy" "Publish"
  header
    heading "Travel policy"
    status "Draft" neutral
    text "Nothing about it is published yet."
  region "Publish on a date"
    field "Date" text = "13/01/2026"
    field "Time" text = "10:00"
    message info "At 13 Jan 2026, 10:00 the policy publishes itself: Standard at EUR 45.00 on the portal. Cancel the schedule any time before then."
  footer
    action "Publish it now"
    action "Cancel"
    action primary "Schedule it"
```

**Rationale:** The way WordPress schedules a post: the date is set on the draft and the publish happens without anyone present. A policy going live at 10:00 on a Tuesday should not need Dana at the desk at 10:00.

**Cost:** A scheduled publish can fire after the draft changed: the ceiling moved, or the budget moved. The scheduler must recheck at the hour and hold the publish with the reason named.

**Reference:** WordPress

#### `publish-blocked-with-the-missing-pieces-listed`: Publish Blocked, with the Missing Pieces Listed

```wireframe
page
  breadcrumb "Home" "Policies" "Travel policy"
  header
    heading "Travel policy"
    status "Draft" neutral
    text "EUR 45.00 · 500 lines."
    action disabled "Publish"
  message info "Publish needs two more pieces. Nothing about this policy is published until both are set."
  region "What is missing"
    list 2
      item: text "A ceiling" · text muted "Nothing to claim yet. The portal cannot show a policy with no ceiling." · action "Add a ceiling"
    text "A live channel is the second piece. The portal is published but shows nothing for this policy."
```

**Rationale:** The way Squarespace blocks publish on an unfinished site: the button stays, and the missing pieces are listed with links to each field. A disabled button alone would read as broken.

**Cost:** Every missing piece is a link away, and fixing one can reveal the next. The list must be complete on first read or the reader learns of them one publish attempt at a time.

**Reference:** Squarespace

#### `pause-sales-without-unpublishing`: Pause Filing Without Unpublishing

```wireframe
page
  breadcrumb "Home" "Policies" "Travel policy"
  header
    heading "Travel policy"
    status "Paused" caution
    text "Paused 8 Oct 2026, 14:22 by Ben. The page stays up."
    action primary "Resume filing"
  message caution "Nothing can be claimed here. The 412 expenses already filed stay valid and still pay."
  region "What a spender sees"
    heading small "Travel policy"
    text "Filing is paused at the moment. Expenses already filed still pay."
    text "More lines go live when the returned budget arrives."
```

**Rationale:** The way Shopify pauses a store: the page stays up and says so, filing stops, and everything claimed stays valid. A policy that may reopen needs the pause, not the unpublish.

**Cost:** Paused reads as broken to a spender who does not read the message. The spender message must say when filing returns, or spenders stop trusting the pause.

**Reference:** Shopify

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

**Rationale:** The way Shopify publishes per sales channel: each channel holds its own state, so phone filing can run while the portal waits. It shows one publish button per channel, each with its own check.

**Cost:** Three channels in three states is a matrix the reader must hold in mind. The policy page must show every channel at once, or one publishes what another promised not to.

**Reference:** Shopify

### Scheduling a Change for Later

**ID:** `litespec/frontend/patterns/create-edit-remove#scheduling-a-change-for-later`

**Question:** Ceilings, fees, and live windows change on fixed dates. A scheduled change names the value, the date, and the way to stop it, and the record shows both the current and the coming value until the date arrives.

**Default:** `a-change-with-a-date-listed-with-the-record`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** A change with a date, listed with the record, is the pick: current and coming values sit side by side with a cancel. The code timeline is the runner-up where ceilings change on fixed dates. Never ship overlapping schedules on one record, and never a change without its spender preview where spenders read the value.

The live window earns its place at creation, where open and close are set together.

**Reviewed for:** Which built dated change differs from the wireframe its catalog entry chose? Open the built live windows and scheduled codes beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The way WordPress lists a scheduled post beside its drafts: the coming change is a row with a date and a cancel. The fee page shows now and next together.

**Cost:** Two values on one record is a rule every read must honor: expenses filed before the date keep the old fee. Any page that prints the fee must print its date too.

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

**Rationale:** Each ceiling holds its own dates, and the timeline shows what applies when. Standard at three ceilings is three rows, not one ceiling edited twice.

**Cost:** Three rows for one policy is three records to read on every expense and in every report. A spender who finds the old ceiling can ask for the difference back, and the page must not hide that.

#### `a-scheduled-change-canceled-before-it-runs`: A Scheduled Change Canceled Before It Runs

```wireframe
page
  breadcrumb "Home" "Policies" "Late fee"
  header
    heading "Late fee"
    text "What the spender pays on top of the expense. You keep all of it."
  region "The fee"
    fact "Now, since 1 Oct 2026" "EUR 5.00"
    fact "From 1 November 2026, scheduled" "EUR 6.00"
  dialog
    heading "Cancel the scheduled fee?"
    text "The fee stays EUR 5.00. Nothing about any expense changes, because the EUR 6.00 never took effect."
    text "The schedule stays in the history as canceled, with who canceled it and when."
    action "Keep the schedule"
    action destructive "Cancel it"
```

**Rationale:** The way Stripe voids a scheduled subscription change: canceling is one dialog that says what stays. The EUR 6.00 fee never happens, and the record says it was canceled rather than forgetting it.

**Cost:** A canceled schedule leaves a row that did nothing, and a year of those is noise. Canceled rows must collapse into the history, not sit beside the value.

**Reference:** Stripe

#### `two-changes-on-one-date-refused`: Two Changes on One Date, Refused

```wireframe
page
  breadcrumb "Home" "Policies" "Late fee"
  header
    heading "Late fee"
    text "What the spender pays on top of the expense. You keep all of it."
  message critical "1 November already holds a change. The fee becomes EUR 6.00 that morning, scheduled by Cara. One record cannot become two fees on the same date."
  region "Move one of them"
    field "Move the EUR 6.00 change to" text = "08/11/2026"
    text "Its expenses keep their date either way. Or cancel the EUR 6.00 change first, then schedule again."
  footer
    action "Cancel"
    action primary "Move it and schedule mine"
```

**Rationale:** The way Google Calendar refuses a second booking in a locked room: the clash names both changes and offers the move. One record cannot become two fees on the same morning.

**Cost:** The second change waits on the first being moved or canceled, which means leaving and coming back. It must offer the move inline, or the refusal sends the reader hunting.

**Reference:** Google Calendar

#### `the-sale-window-set-at-creation`: The Live Window, Set at Creation

```wireframe
page
  breadcrumb "Home" "Policies" "Travel policy" "Standard"
  header
    heading "Standard"
    status "Active" positive
    text "EUR 45.00 · 412 of 500 claimed."
  region "The live window"
    field "Opens" text = "12/01/2026 10:00"
    field "Closes" text = "14/03/2026 22:00"
    text "Before 12 Jan the policy reads Coming soon. After 14 Mar 22:00 it reads Ended, and the expense list is final."
  footer
    action primary "Save the window"
```

**Rationale:** Opens and closes are two dates on the record, and the state follows them. Standard applies from 12 Jan to 14 Mar without anyone present at either end.

**Cost:** A window set at creation is rarely rechecked: it opens on an unfinished policy or closes on a live one. The policy page must show the window beside the state, always.

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

**Rationale:** The way Squarespace previews a scheduled site change: the reader reads the spender page twice, now and after. A fee change is judged by its spender line, not by its row on a settings page.

**Cost:** A preview is a second rendering of the portal and can drift from it. It must render from the same policy read the spender gets, or it shows a page the spender never sees.

**Reference:** Squarespace