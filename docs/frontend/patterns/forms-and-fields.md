# Form and Field Patterns

**Stack:** any
**Concern:** frontend-ui

## Intent

This page holds the patterns for entering a value into a form, and for what the form reports around it: money, record pickers, dates, text, single choices, switches, uploads, refused submits, unsaved work, periods, slugs, capacity, colors, repeated groups, tags, addresses, and message templates. A team building a form starts from these patterns, an owner picks an option from the drawings, and the team records the choice in its pattern catalog, the file holding the project's interface decisions. One decided shape per field keeps entry fast and errors fixable, so staff type once and repair only what the message names.

## Concepts

### How to Read a Pattern

Each pattern answers one recurring question about a field kind, such as how a money field names its currency. It states the question, the default option a project builds, how the default behaves on a narrow screen, and why the default won. Each option then shows one wireframe drawing with its own rationale, cost, and optional reference. A project records its choice in its pattern catalog and builds the chosen option with its own components. The [UI patterns](../patterns.md) page states the catalog contract and the wireframe language.

### The Running Example

The drawings on this page use the Claimwick expense for Ava's Rotterdam client visit: the EUR 45.00 hotel night, its booking fee, the Amsterdam sales cost centre, and the trips, payouts, and approval policies around them. Every option keeps one rule: every field carries a visible label naming its value. The Standards section states that rule, and the [Feedback and State Patterns](feedback-and-states.md) page states the rule for keeping the reader's input through a failure ([keep the reader's input through a failure](feedback-and-states.md#keep-the-readers-input-through-a-failure)).

## Standards

### Name Every Field with a Visible Label

**ID:** `litespec/frontend/patterns/forms-and-fields#name-every-field-with-a-visible-label`

**Strength:** rule

**Statement:** Every field MUST carry a visible label that names its value.

**Rationale:** A placeholder disappears on the first keystroke, so a filled field with no label no longer says what it holds. Screen readers announce the label, and sighted staff returning to a filled form read it to check each value before saving. Without a visible label, both groups guess what each value means.

**Reviewed for:** Which built field lacks a visible label naming its value? Open every form on built screens and read each field's label.

## Patterns

### Money Field

**ID:** `litespec/frontend/patterns/forms-and-fields#money-field`

**Strength:** default

**Question:** The back office holds amounts, fees, reversals, and payout figures, and staff type every one of them; the software chooses none. The decision is how a money field takes its value: the input shape, how it names the currency, where the symbol sits, and what total it restates beside the field.

**Default:** `text-input-currency-in-the-label-symbol-inside`

**Compact:** Below the md breakpoint the amount and its help line stack in one column, with the field at full width.

**Rationale:** The text input with the currency in its label stays the pick. The label names the currency for a screen reader, and the field accepts any amount a reader types, including a comma or a point as the decimal separator. The restated payout total is the runner-up: it belongs on every amount an approver sees, because it stops the fee from surprising either side at payout. The preset amount chips are the other alternative for a round amount that repeats. Never ship the stepper: a continuous amount cannot be reached by stepping.

**Reviewed for:** Which built amount input breaks from the wireframe its catalog entry chose? Open the built amount, currency, and total fields beside the catalog entry recording the chosen option and its reason.

#### `text-input-currency-in-the-label-symbol-inside`: Text Input, Currency in the Label, Symbol Inside

```wireframe
page
  breadcrumb "Home" "Expenses" "New expense"
  header
    heading "Lines"
    text "What each night costs, and what the payout adds on top."
  region "Hotel night"
    field "Nightly amount, in euros" text = "45.00"
    text muted "45,00 or 45.00. Up to two decimal places."
    field "Booking fee added, in euros" text = "5.00"
    text muted "Added at payout. The budget pays this one."
```

**Rationale:** The label names the currency, and the symbol sits inside the field as decoration that assistive technology skips. The field stays a text field, so a reader types any amount; a stepper cannot land on a value between its steps.

**Cost:** A reader can type the symbol into the field, so validation has to accept it rather than refuse it. At most two decimal places are allowed.

**Reference:** HMRC

#### `price-with-the-customer-facing-total-restated`: Amount with the Payout Total Restated

```wireframe
page
  breadcrumb "Home" "Expenses" "New expense"
  header
    heading "Lines"
    text "What each night costs, and what the payout adds on top."
  region "Hotel night"
    field "Nightly amount, in euros" text = "45.00"
    text muted "45,00 or 45.00. Up to two decimal places."
    field "Booking fee added, in euros" text = "5.00"
    fact "Payout sends" "EUR 50.00"
```

**Rationale:** The amount and the payout total read on the same screen before saving. The EUR 50.00 total stops the fee from surprising either side at payout.

**Cost:** The restated total must update as the reader types, which a static wireframe cannot show. It adds one line under every amount field, even where the fee is zero.

#### `preset-price-chips-for-the-common-price-points`: Preset Amount Chips for the Common Amounts

```wireframe
page
  breadcrumb "Home" "Expenses" "New expense"
  header
    heading "Lines"
    text "What each night costs."
  region "Hotel night"
    row
      action "25.00"
      action "35.00"
      action primary "45.00"
      action "60.00"
      action quiet "Custom"
    field "Nightly amount, in euros" text = "45.00"
    fact "Payout sends with the booking fee" "EUR 50.00"
```

**Rationale:** Most lines land on a round amount, and the common amounts sit one tap away. The field stays for the odd amount.

**Cost:** The presets are company data that must be learned or configured, and a stale set pushes the reader toward last year's amounts. Five chips plus a field is wider than a field alone on a phone.

### Choosing a Record

**ID:** `litespec/frontend/patterns/forms-and-fields#choosing-a-record`

**Strength:** default

**Question:** A reader chooses a reference to another record from a picker showing names. Nobody types an identifier. The failure to avoid is a select reading "Choose a cost centre" beside a field holding "CC-0C893968A2" as if it were a value to type.

**Default:** `searchable-picker-showing-names`

**Compact:** Below the md breakpoint the picker opens as a full-width sheet, with the search field at the top and the results under it.

**Rationale:** The searchable picker over names stays the pick: nobody types an identifier, and search beats scrolling for every list longer than a screen. The picker with one distinguishing fact per row is the runner-up for records that share a name. The plain select is the other alternative for a short, fixed list. Never ship a bare identifier field: a code such as CC-0C893968A2 is not a value a reader can type or check.

**Reviewed for:** Which built record picker breaks from the wireframe its catalog entry chose? Open the built searchable pickers and recent lists beside the catalog entry recording the chosen option and its reason.

#### `searchable-picker-showing-names`: Searchable Picker Showing Names

```wireframe
page
  breadcrumb "Home" "Expenses" "New expense"
  header
    heading "New expense"
    text "Start with a name."
  region "Which budget pays"
    field "Cost centre" search = "amster"
    list 3
      item: text "Amsterdam sales" · text muted "EUR 10,000"
    text muted "A cost centre carries its budget and its spend."
    field "Name" text
```

**Rationale:** It searches over names and shows nothing else. Search beats scrolling every time.

**Cost:** A picker hides how many records exist. A reader who knows the cost centre is "Zaandam" still has to search rather than scan. It needs an empty-result state that offers creating one.

#### `picker-with-the-record-s-facts-beside-its-name`: Picker with the Record's Facts Beside Its Name

```wireframe
page
  breadcrumb "Home" "Settings" "Approval policies"
  header
    heading "Add an approval policy"
    text "An approval policy is what finance picks and what the payout follows."
  region "Copy from"
    field "Copy from an existing policy" search
    list 3
      item: text "Standard" · text muted "9,412 decided, active"
    field "Name" text = "Travel"
```

**Rationale:** Each option carries the one fact that tells two similar records apart. Budget suits a cost centre and date suits a trip. State suits an approval policy.

**Cost:** The list grows wider, and the extra column is only useful for records that share a name. For a record set whose names are already unique it is noise.

#### `a-select-of-names`: A Select of Names

```wireframe
page
  breadcrumb "Home" "Expenses" "New expense"
  header
    heading "New expense"
    text "Start with a name."
  region "Which budget pays"
    field "Cost centre" select = "Choose a cost centre"
    field "Name" text
```

**Rationale:** A plain select lists names. It has fewer moving parts than a picker, and it works from the keyboard with no extra setup.

**Cost:** A select hides every option until it opens, and a 40-cost-centre list becomes a long scroll. It cannot search, so a reader who knows the name still scrolls to it. A short, fixed list is the only case where it helps.

**Reference:** GOV.UK

### Date and Time Field

**ID:** `litespec/frontend/patterns/forms-and-fields#date-and-time-field`

**Strength:** default

**Question:** Staff enter dates and times for trips, payouts, and trip plans. The decision is which control takes the value: a field with a calendar, three text inputs, or a date and a time beside each other. The native browser date input is refused: it draws the browser chrome instead of the application control, and it renders differently in every browser.

**Default:** `one-text-field-with-a-calendar-that-opens-beside-it`

**Compact:** Below the md breakpoint the calendar opens as a full-width sheet under the field, one month at a time.

**Rationale:** One typed field with a calendar beside it stays the pick. There is always a way to type, and the calendar serves a reader who would rather click. The three text inputs are the runner-up where accessibility review demands them; they cost the slowest entry in the form. A date and a time on one row are the other alternative where a form always takes both. Never ship the native date input: it draws the browser chrome instead of the application control.

**Reviewed for:** Which built calendar input breaks from the wireframe its catalog entry chose? Open the built date, duration, and range fields beside the catalog entry recording the chosen option and its reason.

#### `one-text-field-with-a-calendar-that-opens-beside-it`: One Text Field with a Calendar That Opens Beside It

```wireframe
page
  breadcrumb "Home" "Trips" "New trip"
  header
    heading "New trip"
    text "Start with a name."
  region "When it runs"
    field "Trip starts" text = "14/03/2026 08:00"
    text "March 2026"
    grid 31
      item: text "14"
    text muted "Times are in the Europe/Amsterdam time zone."
    field "Trip ends" text = "14/03/2026 12:00"
```

**Rationale:** The reader types into one field, with a calendar beside it to click. The date picker pattern is exactly this, and the rule is that there must always be a way to type.

**Cost:** A calendar is a grid of 42 cells to build, translate, and test in two languages. It is the most code in the form package for one field.

**Reference:** APG

#### `three-text-inputs-with-a-format-hint`: Three Text Inputs with a Format Hint

```wireframe
page
  breadcrumb "Home" "Trips" "New trip"
  header
    heading "New trip"
    text "Start with a name."
  region "When it runs"
    row
      field "Day" text = "14"
      field "Month" text = "3"
      field "Year" text = "2026"
    text muted "For example, 14 3 2026"
    row
      field "Start hour" text = "08"
      field "Start minute" text = "00"
    text muted "In the Europe/Amsterdam time zone."
```

**Rationale:** The three-field date is the most accessible answer. It uses numeric input modes and autocomplete tokens, and every browser renders it the same.

**Cost:** Three fields hold one value, so 94 dates a year means 282 numbers typed. It is the most accessible answer and the slowest one.

**Reference:** GOV.UK

#### `date-and-time-on-one-row-the-time-zone-named-once`: Date and Time on One Row, the Time Zone Named Once

```wireframe
page
  breadcrumb "Home" "Trips" "New trip"
  header
    heading "New trip"
    text "Start with a name. All times are Europe/Amsterdam."
  region "When it runs"
    row
      field "Start date" date = "14/03/2026"
      field "Start time" text = "12:00"
    row
      field "End date" date = "14/03/2026"
      field "End time" text = "08:00"
    message critical "The end must be after the start."
```

**Rationale:** Date and time sit side by side as two fields, with the time zone stated once above them rather than repeated under each. That means fewer repetitions of the same fact.

**Cost:** On a narrow screen two fields plus the row wrap to three lines. The time zone is stated once in the header, not beside each field.

### Text Field and Its States

**ID:** `litespec/frontend/patterns/forms-and-fields#text-field-and-its-states`

**Strength:** default

**Question:** Every text field draws several states: empty, filled, focused, refused, disabled, and read-only. The label stays visible above the input in all of them. A placeholder is never the label, because it vanishes on the first keystroke.

**Default:** `label-above-help-under-six-states-on-one-page`

**Compact:** Below the md breakpoint the states stack in one column, each with its label and help line.

**Rationale:** The six-state reference page stays the pick, because it is the only option that pins every state one field must draw. Inline confirmation of a good value is the runner-up for create forms, where it saves a refused submit. Floating labels are the other alternative for a compact form. Never ship the placeholder as the label: it vanishes on the first keystroke, and its color misses contrast.

**Reviewed for:** Which built text input breaks from the wireframe its catalog entry chose? Open the built labels, counts, and growing areas beside the catalog entry recording the chosen option and its reason.

#### `label-above-help-under-six-states-on-one-page`: Label Above, Help Under, Six States on One Page

```wireframe
page
  breadcrumb "Home" "Settings" "Field states"
  header
    heading "Field states"
    text "Every state one text field has to draw."
  region "The states"
    field "Empty" text
    field "Filled" text = "Rotterdam client visit"
    text muted "Spenders see this on the expense and in the payout."
    field "Focused" text = "Rotterdam client visit"
    field "Refused" text = "Spring offsite"
    message critical "A trip is already scheduled as Spring offsite."
    field "Disabled" text = "Rotterdam client visit"
    text muted "The policy switched this field off."
    field "Read only" text = "EXP-1042-01"
    text muted "Issued by the system when the line was made."
```

**Rationale:** The page is the component reference for the field: empty, filled, focused, refused, disabled, and read-only. Showing the states together is how a team checks one control rather than six screenshots.

**Cost:** Six instances of one field is a style guide, not a screen. It never appears in the product. It exists so the states cannot drift apart.

#### `inline-confirmation-of-a-good-value`: Inline Confirmation of a Good Value

```wireframe
page
  breadcrumb "Home" "Settings" "Approval policies"
  header
    heading "Add an approval policy"
    text "The name spenders see, and what the policy includes."
  region "The policy"
    field "Name" text = "Standard"
    message positive "Standard is available."
    field "What it includes" textarea
  footer
    action "Cancel"
    action primary "Add the policy"
```

**Rationale:** A good value is confirmed where it was typed, with a quiet line instead of a wait for the submit. Confirming the wanted name early saves a round trip.

**Cost:** Live validation is a narrow exception, because it judges a field before it is finished. The check must wait for a pause in typing, or it interrupts.

**Reference:** Stripe

#### `floating-labels-that-shrink-when-filled`: Floating Labels That Shrink When Filled

```wireframe
page
  breadcrumb "Home" "Settings" "Approval policies"
  header
    heading "Add an approval policy"
  region "The policy"
    field "Name" text
    field "What it includes" textarea = "Standard limits for every trip."
  footer
    action "Cancel"
    action primary "Add the policy"
```

**Rationale:** A floating label answers the placeholder problem by moving the label above the value once the field fills, so the label never vanishes. It keeps the compact look the placeholder promised without breaking it.

**Cost:** The empty state still reads as a placeholder, with small text and low contrast. A floated label is smaller than every other label in the back office, so a reader with low vision reads it at a size the rest of the form never uses.

**Reference:** Material Design

### Choosing One of a Few Options

**ID:** `litespec/frontend/patterns/forms-and-fields#choosing-one-of-a-few-options`

**Strength:** default

**Question:** A reader chooses from a closed set, such as a naming rule or a plan. A select hides its options until it opens, so radios beat a select for a short set, and a segmented control fits two or three short values. Nothing is pre-selected in a question, though settings allow it. The decision is which control carries the choice: a radio list, a segmented control, or a select.

**Default:** `a-radio-list-with-a-description-per-option`

**Compact:** Below the md breakpoint the radio list stays in one column, with each description under its option.

**Rationale:** The radio list with a description per option stays the pick. Each choice carries the sentence that lets a reader choose between names that sound alike. The segmented control is the runner-up for two or three short values with no descriptions. The select is the other alternative for a long list in a settings page. Never ship the native multi-select: it needs a modifier key to choose a second value and hides which values are chosen, and a checkbox list replaces it.

**Reviewed for:** Which built single-choice control breaks from the wireframe its catalog entry chose? Open the built radios, segments, and plan cards beside the catalog entry recording the chosen option and its reason.

#### `a-radio-list-with-a-description-per-option`: A Radio List with a Description per Option

```wireframe
page
  breadcrumb "Home" "Expenses" "Receipts"
  header
    heading "Receipt naming"
    text "Finance reads the name on each file."
    action primary "Save"
  region "Naming rule"
    field "No name" radio
    text "A receipt is filed without a name on it."
    field "The spender name" radio
    text "The spender name is on the file."
    field "Every line named" radio
    text "The spender names each line before filing."
```

**Rationale:** Each option carries a sentence saying what it means for the spender. That sentence is the only way to choose between "The spender name" and "Every line named".

**Cost:** Three radio rows are a tall block, and they do not scale to twelve options, which a permission picker does need.

#### `a-segmented-control-for-two-or-three`: A Segmented Control for Two or Three

```wireframe
page
  breadcrumb "Home" "Expenses" "New expense"
  header
    heading "New expense"
    text "Start with a name, or start from a template."
  region "Start from"
    row
      action primary "A blank expense"
      action "A template"
    field "Name" text
    field "Cost centre" select = "Choose a cost centre"
```

**Rationale:** The choice is between two or three values that are all visible at once. A segmented control shows every option and hides none behind a menu.

**Cost:** Labels have to fit, so "A blank expense" and "A template" are both shortened. The control grows with its longest label.

#### `a-select-in-a-settings-page-where-nothing-is-pre-selected`: A Select, in a Settings Page Where Nothing Is Pre-Selected

```wireframe
page
  breadcrumb "Home" "Settings" "General"
  header
    heading "General"
    text "What Harbor and Pine is and where it works."
    action primary "Save"
  region "Locale"
    field "Time zone" select = "Choose a time zone"
    text muted "Every date and time on every page is written in this zone."
    field "Default currency" select = "Choose a currency"
    text muted "The currency amounts are entered in. Every amount uses it."
```

**Rationale:** A select with a real empty first option is the least-bad shape where the value has no default and the options are many.

**Cost:** The empty option is a placeholder, and placeholders stay out of components. A select whose first choice is a blank that must be replaced is two states, not one.

**Reference:** GOV.UK

### Switch or a Checkbox

**ID:** `litespec/frontend/patterns/forms-and-fields#switch-or-a-checkbox`

**Strength:** default

**Question:** The payout editor holds a toggle for the cost-centre line, and settings pages hold many more switches and checkboxes. This pattern decides which control each setting deserves, and what the label must say, since the label carries the difference between the two.

**Default:** `a-switch-with-the-label-and-its-consequence`

**Compact:** Below the md breakpoint each switch keeps its label and its consequence line in one column.

**Rationale:** The switch with its consequence stated stays the pick. The label says what is on, and the line under it says what turning it off does. Instant apply with an undo toast is the runner-up for low-risk settings a reader flips often. A checkbox is the other alternative where the off state is the default. Never ship a switch whose label names the control instead of the outcome.

**Reviewed for:** Which built toggle breaks from the wireframe its catalog entry chose? Open the built switches, checkboxes, and instant applies beside the catalog entry recording the chosen option and its reason.

#### `a-switch-with-the-label-and-its-consequence`: A Switch with the Label and Its Consequence

```wireframe
page
  breadcrumb "Home" "Settings" "Payouts"
  header
    heading "Payouts"
    text "What spenders see when the payout lands."
  region "Remittance"
    field "Show the cost centre line" toggle
    text "The line on the remittance that names the budget."
    field "Let spenders file without an account" toggle
    text "They can still sign in afterwards to reach their own expenses."
    field "Add a booking fee" toggle
    text "The budget pays this one. EUR 5.00 per expense."
```

**Rationale:** The label says what is on and the line under it says what turning it off does, so the choice is reversible and stated.

**Cost:** The label has to carry the whole difference between on and off, and a reader still has to look twice to see the current state. A switch draws more attention than a setting that rarely changes needs.

#### `instant-apply-with-an-undo-toast`: Instant Apply with an Undo Toast

```wireframe
page
  breadcrumb "Home" "Settings" "Payouts"
  header
    heading "Payouts"
    text "What spenders see when the payout lands. Changes apply at once."
  region "Remittance"
    field "Waive the fee for the Travel policy" toggle
    text "Travel policy only. Last month that would have been EUR 310.00 across 62 expenses."
    field "Let spenders file without an account" toggle
    text "They can still sign in afterwards to reach their own expenses."
    field "Show the cost centre line" toggle
    text "The line on the remittance that names the budget."
  toast positive "Booking fee waived for the Travel policy."
```

**Rationale:** A low-risk switch applies at once, with Undo in a toast. There is no Save to forget and no dialog to dismiss, and the toast names what changed.

**Cost:** Every flip is an audited command the moment it happens, with no review step before it. It fits display settings, not money, access, or anything a toast cannot undo.

**Reference:** Linear

#### `a-checkbox-where-the-off-state-is-the-default`: A Checkbox Whose Default State Is Off

```wireframe
page
  breadcrumb "Home" "Settings" "Email"
  header
    heading "Email"
    text "What Harbor and Pine sends, and what it promises."
    action primary "Save"
  region "Messages"
    field "Email a spender when their payout is sent" checkbox
    text "Carries the payout reference. This is the message a spender looks for."
    field "Email a spender the day before the payout" checkbox
    text "Sent at 18:00 in the office time zone."
    field "Put the company name on every message" checkbox
    text "Harbor and Pine, with its address and telephone number, as the law requires."
```

**Rationale:** A checkbox fits a setting whose default is off and whose action is to tick something. The label is phrased as what ticking does, not as what the box is.

**Cost:** A checkbox and a switch look nearly identical here, and readers conflate them. The phrasing of the label has to carry the difference.

**Reference:** GOV.UK

### Upload Field

**ID:** `litespec/frontend/patterns/forms-and-fields#upload-field`

**Strength:** default

**Question:** Claimwick records take files: a receipt photo or a staff spreadsheet. A receipt needs a stated type and size, and a staff import needs stated columns and row-level refusals. The decision is how the upload field states its contract and reports progress and refusals.

**Default:** `a-drop-zone-that-states-what-it-accepts`

**Compact:** Below the md breakpoint the drop zone takes a tap to choose a file, and its type and size line stays under it.

**Rationale:** The drop zone that states its types and size limit stays the pick. Nobody learns the limit by being refused. The per-file progress tray is the runner-up for imports of several files, where one bar per file beats one bar for all. A CSV upload that states its columns first is the other alternative where the file must match a fixed shape. Never ship a CSV upload with no stated columns: a file refused for a format nobody stated is a refusal the application caused.

**Reviewed for:** Which built file input breaks from the wireframe its catalog entry chose? Open the built drop zones, CSV reads, and progress rows beside the catalog entry recording the chosen option and its reason.

#### `a-drop-zone-that-states-what-it-accepts`: A Drop Zone That States What It Accepts

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "Receipts"
    text "The receipt photos finance reads."
  region "The receipt"
    field "Drop a receipt here or choose a file" file
    text "A photo of at most 5 MB. JPEG, PNG or PDF."
    field "Receipt description" text
    field "Drop the second page here or choose a file" file
    text "The back of the receipt, if the total is there."
    field "Back-page description" text
```

**Rationale:** The zone states the accepted types and the size limit in a sentence under it, so nobody discovers the limit by being refused.

**Cost:** A drop zone invites a click anywhere, and a file dropped outside it does nothing at all. Nothing on the page shows where the zone begins.

#### `one-progress-row-per-file`: One Progress Row per File

```wireframe
page
  breadcrumb "Home" "Settings" "Data imports"
  header
    heading "Import staff"
    text "Bring in a list already held. Nothing is sent anywhere until the result is checked."
  region "Files"
    list 2
      item: text "staff-2026-10.csv" · text muted "1,240 of 2,000 rows read" · action "Cancel"
    text "receipt-hotel.jpg is ready."
    field "Drop another file here or choose one" file
    text "CSV up to 5 MB, or an image up to 2 MB."
  footer
    action "Cancel"
    action primary "Check the files"
```

**Rationale:** Each file reports on its own row with its own progress and its own cancel, because two files never finish together. One bar for all would lie about both.

**Cost:** A row per file is a list the page must manage: cancel one, remove one, and retry one. For a single receipt it is heavier than the one zone it replaces.

**Reference:** Dropbox

#### `a-csv-upload-that-states-its-columns-first`: A CSV Upload That States Its Columns First

```wireframe
page
  breadcrumb "Home" "Settings" "Data imports"
  header
    heading "Import staff"
    text "Bring in a list already held. Nothing is sent anywhere until the result is checked."
  region "The file"
    text "The file needs four columns."
    text "email, first_name, last_name, cost_centre. The header row names them. Order does not matter."
    link "Download a template"
    field "Drop a CSV here or choose a file" file
    text "Up to 5 MB, 2,000 rows. UTF-8, comma separated."
  footer
    action "Cancel"
    action primary "Check the file"
```

**Rationale:** For an import, the field states the four column names and the template before anything is uploaded. A file cannot fail a format the page never stated.

**Cost:** Stating four columns above the zone is a paragraph of prose, and prose belongs on the page beside the field, not inside a component.

### Reporting a Refused Submit

**ID:** `litespec/frontend/patterns/forms-and-fields#reporting-a-refused-submit`

**Strength:** default

**Question:** A submit refused by the API must report what is wrong and where. WCAG 3.3.1 and 3.3.3 ask for a summary that takes focus and links to each field, plus a message on each field. The decision is how the form carries that report.

**Default:** `a-summary-above-the-form-plus-a-message-on-each-field`

**Compact:** Below the md breakpoint the summary sits above the form with its links, and each message stays under its field.

**Rationale:** Keep the summary with focus, plus per-field messages. It meets WCAG 3.3.1 and 3.3.3, and the links carry the reader to each field. Validation on leaving each field is the runner-up as a complement, never a replacement: it confirms good values early but cannot orient a reader facing three errors. Messages on the fields only are the other alternative for a short form. Never ship the dialog summary: it hides the form the reader must fix. Whatever the form reports, it keeps every entered value, as [keep the reader's input through a failure](feedback-and-states.md#keep-the-readers-input-through-a-failure) states.

**Reviewed for:** Which built submit-error report breaks from the wireframe its catalog entry chose? Open the built summaries and per-field messages beside the catalog entry recording the chosen option and its reason.

#### `a-summary-above-the-form-plus-a-message-on-each-field`: A Summary Above the Form, Plus a Message on Each Field

```wireframe
page
  breadcrumb "Home" "Trips" "New trip"
  header
    heading "New trip"
  region "There is a problem"
    link "Enter the name of the trip"
    link "Enter a date as well as a time"
  region "The trip"
    field "Name" text
    message critical "Enter the name of the trip"
    field "Cost centre" select = "Amsterdam sales"
    field "Trip starts" text = "08:00"
    message critical "Enter a date as well as a time."
  footer
    action "Cancel"
    action primary "Create trip"
```

**Rationale:** The summary comes first and receives focus, with each entry linking to its field. The field repeats the same wording so it reads the same out of context.

**Cost:** The wording exists in two places per error, so generate it once and use it twice. Hand-written copies drift, and the drift is invisible.

**Reference:** GOV.UK

#### `validation-on-leaving-each-field`: Validation on Leaving Each Field

```wireframe
page
  breadcrumb "Home" "Trips" "New trip"
  header
    heading "New trip"
    text "Each field is checked when it is left."
  region "The trip"
    field "Name" text = "Spring offsite"
    message positive "Spring offsite is available."
    field "Cost centre" select = "Amsterdam sales"
    field "Trip starts" text = "08:00"
    message critical "Enter a date as well as a time."
  footer
    action "Cancel"
    action primary "Create trip"
```

**Rationale:** A field is checked when it is left and marked good or wrong where it sits. The reader fixes one field and moves on, and by the submit nothing is left to refuse.

**Cost:** It is a complement to the summary, never a replacement: with three errors and no summary the reader still cannot see them all at once. A check on blur fires while the form is half-filled.

**Reference:** Shopify Polaris

#### `messages-on-the-fields-only`: Messages on the Fields Only

```wireframe
page
  breadcrumb "Home" "Trips" "New trip"
  header
    heading "New trip"
  region "The trip"
    field "Name" text
    message critical "Enter the name of the trip"
    field "Cost centre" select = "Choose a cost centre"
    message critical "Choose which budget pays"
    field "Trip starts" text = "08:00"
    message critical "Enter a date as well as a time"
  footer
    action "Cancel"
    action primary "Create trip"
```

**Rationale:** Each field carries its own message and nothing else. The reader sees the error where it has to fix it.

**Cost:** On a long form the first error may be below the fold. The reader submits, sees nothing change, and concludes the button is broken.

### Work That Is Not Saved

**ID:** `litespec/frontend/patterns/forms-and-fields#work-that-is-not-saved`

**Strength:** default

**Question:** A form must show work that is not saved. Nothing may read as saved before it is, and leaving must not drop typed work without a warning. The decision is how the form marks unsaved work and guards the way out.

**Default:** `a-visible-unsaved-marker-beside-the-save`

**Compact:** Below the md breakpoint the unsaved note and the Save button stay together at the foot of the form.

**Rationale:** Keep the visible unsaved marker beside Save. A reader never has to remember whether anything was typed. The save bar pinned to the top is the runner-up for long settings pages where the form actions scroll out of view. Autosave with a saved marker is the other alternative for a note written over several sittings. Never ship silent autosave on money or access: each save would be an audited command, and an audit entry per keystroke batch is not a trail a reader can follow.

**Reviewed for:** Which built unsaved marker breaks from the wireframe its catalog entry chose? Open the built dirty flags, drafts, and save bars beside the catalog entry recording the chosen option and its reason.

#### `a-visible-unsaved-marker-beside-the-save`: A Visible Unsaved Marker Beside the Save

```wireframe
page
  breadcrumb "Home" "Settings" "Remittance"
  header
    heading "Remittance"
    text "The accent color spenders see on the remittance."
  region "Appearance"
    field "Accent color" text = "#B45309"
    text "A #RRGGBB value. Contrast with white is 5.1 to 1."
    field "Typeface" select = "Inter"
  footer
    text "Two changes not saved. Spenders still see the old ones."
    action quiet "Discard both"
    action primary "Save two changes"
```

**Rationale:** The moment a field changes, the Save button carries the count and a note says what changed. The reader never has to remember whether anything was typed.

**Cost:** The count of changed fields is a derived fact, so it has to come from the form rather than the route. A Save that changes its own label moves under the pointer.

#### `a-save-bar-pinned-to-the-top-while-dirty`: A Save Bar Pinned to the Top While Dirty

```wireframe
page
  breadcrumb "Home" "Settings" "Remittance"
  header
    heading "Remittance"
    text "The accent color and typeface spenders see."
  toolbar sticky
    text "Two unsaved changes. Spenders still see the old ones."
    action "Discard"
    action primary "Save"
  region "Appearance"
    field "Accent color" text = "#B45309"
    field "Typeface" select = "Inter"
```

**Rationale:** A pinned bar keeps Save and Discard in reach on a long settings page. It appears with the first keystroke and leaves with the save.

**Cost:** A pinned bar covers content on a phone and follows the reader past sections it has nothing to do with. It needs the same dirty tracking as the marker, so it adds reach rather than information.

**Reference:** Shopify Polaris

#### `autosave-with-a-saved-marker`: Autosave, with a Saved Marker

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "Disputes"
  header
    heading "Record a dispute"
    text "Saved as it is typed. Nothing is lost when this browser closes."
  region "The dispute"
    field "What happened" textarea = "The hotel charged EUR 12.00 for breakfast that the rate already included."
    text muted "Saved four seconds ago"
```

**Rationale:** The form saves itself every few seconds, and a quiet marker says when. Nothing is lost and no dialog is ever needed.

**Cost:** Autosave makes each save a command against money and access, which means an audit entry per keystroke batch. A past decision must stay reconstructable, and a trail of keystroke batches is not one a reader can follow.

### Picking a Period

**ID:** `litespec/frontend/patterns/forms-and-fields#picking-a-period`

**Strength:** default

**Question:** Payouts and reporting run on periods. Two typed dates cover every window but ask for both edges, while presets cover the common windows in one tap. The decision is how the period control combines typing and presets.

**Default:** `two-date-inputs-with-presets`

**Compact:** Below the md breakpoint the two date fields stack, with the presets above them and the payout date under the second field.

**Rationale:** Keep two typed dates with presets. One tap covers the common windows, typing covers the rare ones, and both edges stay visible. The painted calendar is the runner-up for reporting screens where the window itself is the question. The month picker is the other alternative for a report that runs by calendar month. Never ship presets alone with no typed fallback: no preset reaches the filing-to-payout window the payout check wants.

**Reviewed for:** Which built range picker breaks from the wireframe its catalog entry chose? Open the built presets, month pickers, and painted calendars beside the catalog entry recording the chosen option and its reason.

#### `two-date-inputs-with-presets`: Two Date Inputs with Presets

```wireframe
page
  breadcrumb "Home" "Payouts" "March run"
  header
    heading "March run"
    text "What moved for Harbor and Pine."
  tabs "Last 7 days" "Last 30 days" "Last 90 days"
  region "Period"
    field "From" date = "14/02/2026"
    field "To" date = "14/03/2026"
    text muted "The payout itself: Sat 14 Mar 2026."
  footer
    action primary "Show the run"
```

**Rationale:** The presets fill both fields and either edge can then be adjusted by typing. The common windows cost one tap and the rare ones stay possible. The control that fills the fields and the fields that hold them are one unit.

**Cost:** Two controls describe one value. The preset shows Custom the moment either date is edited, or the row claims a window it no longer holds.

#### `a-calendar-with-the-range-painted`: A Calendar with the Range Painted

```wireframe
page
  breadcrumb "Home" "Payouts" "March run"
  header
    heading "March run"
    text "What moved for Harbor and Pine."
  tabs "Payout week" "Trip week" "Custom"
  region "Period"
    split 1:1
      grid 28
        item: text "20"
      grid 31
        item: text "7"
    text "07/03/2026 to 14/03/2026, both days counted in full."
  footer
    action primary "Show the run"
```

```wireframe compact
page
  breadcrumb "Home" "Payouts" "March run"
  header
    heading "March run"
    text "What moved for Harbor and Pine."
  tabs "Payout week" "Trip week" "Custom"
  region "Period"
    grid 28
      item: text "20"
    grid 31
      item: text "7"
    text "07/03/2026 to 14/03/2026, both days counted in full."
  footer
    action primary "Show the run"
```

**Rationale:** The chosen range is painted across the calendar, so the window is seen instead of read from two dates. The endpoints stay readable as dates under the control.

**Cost:** A painted month shows one month, and the payout window usually spans two. Two months side by side show the window truthfully, and they do not fit a phone.

**Reference:** Airbnb

#### `month-picker`: Month Picker

```wireframe
page
  breadcrumb "Home" "Payouts" "March run"
  header
    heading "March run"
    text "What moved for Harbor and Pine."
  region "Period"
    field "Month" select = "March"
    field "Year" number = "2026"
    text muted "March 2026, the month of Sat 14 Mar 2026."
  footer
    action primary "Show the run"
```

**Rationale:** Payout and tax reporting run by calendar month, and picking March 2026 in one control beats typing the first and last day of the month.

**Cost:** The control cannot express a window that crosses a month edge, like the fortnight around the payout. Whole months are the only shape it draws.

### Slug or URL Field with a Preview

**ID:** `litespec/frontend/patterns/forms-and-fields#slug-or-url-field-with-a-preview`

**Strength:** default

**Question:** A trip takes a slug, the short part typed in, and spenders read the full address: they link it and print it. The decision is how the field takes the slug and previews the address before saving.

**Default:** `slug-with-the-live-address-under-it`

**Compact:** Below the md breakpoint the live address wraps under the slug field, with the copy command beside it.

**Rationale:** Keep the slug with its live address under it. The reader types the short part and reads the full address before saving. Transliteration from the name is the runner-up for creation, where typing the same words twice is pure overhead. The address as a fact is the other alternative once it is shared. Never ship an address that cannot be read before it is saved.

**Reviewed for:** Which built address input breaks from the wireframe its catalog entry chose? Open the built slugs, transliterations, and availability checks beside the catalog entry recording the chosen option and its reason.

#### `slug-with-the-live-address-under-it`: Slug with the Live Address Under It

```wireframe
page
  breadcrumb "Home" "Trips" "Rotterdam client visit" "Address"
  header
    heading "Trip address"
    text "Where spenders file against this trip."
  region "The address"
    field "Name" text = "Rotterdam client visit"
    field "Slug" text = "rotterdam-client-visit"
    text muted "Lowercase letters, numbers and dashes. Changing it breaks links already shared."
    fact "Spenders reach" "claimwick.example/rotterdam-client-visit"
    action quiet "Copy"
  footer
    action "Cancel"
    action primary "Save address"
```

**Rationale:** The full address sits under the slug field and updates as the short value is typed. The short value is never saved unseen. The copy command beside it is how the address leaves the back office.

**Cost:** The address must update as the reader types, which a static wireframe cannot show. It also states the host the platform gives, which cannot be changed on this screen.

**Reference:** Shopify

#### `transliterated-from-the-name-with-an-edit-toggle`: Transliterated from the Name with an Edit Toggle

```wireframe
page
  breadcrumb "Home" "Trips" "New trip"
  header
    heading "New trip"
    text "Start with a name."
  region "The address"
    field "Name" text = "Rotterdam client visit"
    field "Generate the slug from the name" checkbox
    field "Slug" text = "rotterdam-client-visit"
    text muted "Spenders reach claimwick.example/rotterdam-client-visit."
  footer
    action "Cancel"
    action primary "Create trip"
```

**Rationale:** The address is built from the name and can then be edited, because the name and the address agree nine times in ten. Typing both by hand is the same words twice.

**Cost:** The generated value must transliterate Dutch diacritics the way the reader expects, and a renamed trip must not silently change its address. Generation runs once at creation, never again.

**Reference:** WordPress

#### `the-address-as-a-fact-with-a-change-command`: The Address as a Fact with a Change Command

```wireframe
page
  breadcrumb "Home" "Trips" "Rotterdam client visit" "Address"
  header
    heading "Trip address"
    text "Where spenders file against this trip."
  region "The address"
    fact "Address" "claimwick.example/rotterdam-client-visit"
    fact "First shared" "13 Jan 2026"
    fact "Used in" "214 expenses"
    message caution "Changing the address breaks the links already shared. The old address keeps working for 30 days."
  footer
    action primary "Change the address"
```

**Rationale:** An address in use is shown as a fact with a change command, because links already shared depend on it. The 30-day redirect is what makes the change survivable.

**Cost:** Changing a value takes two steps, and the redirect has to be a real behavior, not a sentence. A redirect nobody builds is a promise the page breaks.

**Reference:** GitHub

### Capacity Field

**ID:** `litespec/frontend/patterns/forms-and-fields#capacity-field`

**Strength:** default

**Question:** A budget code takes money from one cost-centre budget: Amsterdam sales holds EUR 10,000, and every code draws from the same budget. The decision is how the capacity field shows the ceiling, the split, and the refusal past it.

**Default:** `a-number-with-the-warehouse-ceiling-restated`

**Compact:** Below the md breakpoint the ceiling field sits above the restated budget facts in one column.

**Rationale:** Keep the number with the budget ceiling restated. One field, the budget it draws from, and what is left sit on the same screen. The split across codes is the runner-up for the budget page, where the question is the whole budget rather than one code. Claimed as a read-only fact beside an editable ceiling is the other alternative where claimed must never be typed. Never ship a capacity with no ceiling stated: a number without its budget is a number that overspends.

**Reviewed for:** Which built ceiling input breaks from the wireframe its catalog entry chose? Open the built splits, steppers, and overage messages beside the catalog entry recording the chosen option and its reason.

#### `a-number-with-the-warehouse-ceiling-restated`: A Number with the Budget Ceiling Restated

```wireframe
page
  breadcrumb "Home" "Finance" "Budgets" "TRIP10" "Capacity"
  header
    heading "Capacity"
    text "How much TRIP10 can spend."
  region "Money on the code"
    field "Ceiling, in euros" number = "9600"
    text muted "EUR 9,412 claimed. EUR 188 still to claim."
    text "The budget is 94 percent spent."
    fact "Amsterdam sales holds" "EUR 10,000"
    text muted "PARTNER holds EUR 400 of it."
  footer
    action "Cancel"
    action primary "Save capacity"
```

**Rationale:** The budget total sits beside every ceiling field, because a code allocation means nothing without the budget it draws from. The line under the field says how spent the budget is.

**Cost:** The restated ceiling must update when another code changes, or two screens state two budgets. It is a read of the whole cost centre behind one field.

#### `the-split-across-types-with-what-is-left`: The Split Across Types with What Is Left

```wireframe
page
  breadcrumb "Home" "Finance" "Budgets" "Capacity"
  header
    heading "Capacity"
    text "How the EUR 10,000 is divided."
  region "Every code"
    list 3
      item: text "TRIP10" · text muted "Active since 13 Jan 2026" · text "9,412 of 9,600" · action "Change"
  region "The budget"
    fact "Claimed across every code" "9,730"
    fact "Left in the budget" "EUR 270"
    text muted "Retired codes keep their claimed amounts but release nothing back."
```

**Rationale:** One row per code shows claimed against its ceiling, because the question is the budget, not the row. The leftover line is what the reader came to read.

**Cost:** A row per code carries the columns a table would, and at twelve codes it needs the table back. Editing still jumps to one code at a time.

#### `sold-as-a-read-only-fact-beside-an-editable-cap`: Claimed as a Read-Only Fact Beside an Editable Ceiling

```wireframe
page
  breadcrumb "Home" "Finance" "Budgets" "TRIP10" "Capacity"
  header
    heading "Capacity"
    text "How much TRIP10 can spend."
  region "Money on the code"
    fact "Claimed" "9,412"
    fact "Left" "188"
    message info "Claimed is a fact and cannot be edited. Lowering the ceiling below 9,412 is refused."
    field "Ceiling, in euros" number = "9600"
    text muted "Must stay at or above 9,412."
  footer
    action "Cancel"
    action primary "Save capacity"
```

**Rationale:** Claimed is shown as a fact beside the controls that move it: claimed is what happened, and the ceiling is what the reader decides. The two must never share a field.

**Cost:** Three figures plus a field for one decision, and the derived Left line must update as the ceiling is typed. It is the most screen per field in this item.

**Reference:** Stripe

### Color Field

**ID:** `litespec/frontend/patterns/forms-and-fields#color-field`

**Strength:** default

**Question:** A company brands its approval mails with one accent color: a `#RRGGBB` value dark enough for white button text to read on it. The field takes the value and proves the contrast before the page refuses it.

**Default:** `hex-field-with-a-swatch-and-a-contrast-readout`

**Compact:** Below the md breakpoint the swatch sits above the hex field, and the contrast readout stays under it.

**Rationale:** The hex field with a swatch and a contrast readout is the pick. It takes any color, shows it, and states the ratio the validation enforces. The approver-button preview is the runner-up, because the button shows the ratio rather than stating it. Preset swatches are the other alternative for a reader who does not know a hex value. Never ship a free color with no contrast check: a pale accent ships white text nobody can read.

**Reviewed for:** Which built accent picker breaks from the wireframe its catalog entry chose? Open the built swatches, wells, and contrast readouts beside the catalog entry recording the chosen option and its reason.

#### `hex-field-with-a-swatch-and-a-contrast-readout`: Hex Field with a Swatch and a Contrast Readout

```wireframe
page
  breadcrumb "Home" "Settings" "Mail"
  header
    heading "Mail style"
    text "The accent color approvers see on mail buttons."
  region "Accent"
    image "Accent #1F2937 swatch"
    field "Accent color" text = "#1F2937"
    text muted "A #RRGGBB value. Contrast with white is 14.7:1, so white button text is readable."
    fact "White button text" "Readable, needs 4.5:1, this accent reaches 14.7:1"
    status "Readable" positive
  footer
    action "Cancel"
    action primary "Save accent"
```

**Rationale:** A hex field is paired with a swatch and the rule it passes. A color value means nothing until it is seen, and the readout repeats the validation rule, so the rule is learned before the request rather than after it.

**Cost:** The ratio must recompute as the value is typed, and an unfinished value has no ratio to state. Six characters that are not yet a color need patience, not a refusal.

**Reference:** Shopify

#### `the-accent-previewed-on-a-customer-button`: The Accent Previewed on an Approver Button

```wireframe
page
  breadcrumb "Home" "Settings" "Mail"
  header
    heading "Mail style"
    text "The accent color approvers see on mail buttons."
  region "Accent"
    image "Accent #1F2937 swatch"
    field "Accent color" text = "#1F2937"
    text muted "A #RRGGBB value. Contrast with white is 14.7:1."
  card "Approver preview"
    heading small "Approval mail"
    text "EXP-1042 from Ava"
    text "EUR 125.00, Rotterdam client visit"
    text "Two lines, EUR 125.00"
    action "Approve EUR 125.00"
    text muted "Harbor and Pine, Canal Street 12, 1011 AB Amsterdam"
  footer
    action "Cancel"
    action primary "Save accent"
```

**Rationale:** The brand color is previewed on the button the approver presses, because the button shows the ratio rather than stating it. The reader sees white on the accent instead of reading a number.

**Cost:** A preview is a second surface that must match the typed value. It shows the typed value, not the saved one, and updates as the reader types. A stale preview shows a value the reader will not get.

**Reference:** Mailchimp

#### `preset-swatches-plus-a-custom-value`: Preset Swatches Plus a Custom Value

```wireframe
page
  breadcrumb "Home" "Settings" "Mail"
  header
    heading "Mail style"
    text "The accent color approvers see on mail buttons."
  region "Accent"
    text "Presets, every preset passes 4.5:1 against white."
    row
      image "Ink preset, chosen"
      image "Amber preset"
      image "Pine preset"
      image "Ocean preset"
      image "Plum preset"
      image "Rose preset"
    field "Or a custom color" text = "#1F2937"
    text muted "A #RRGGBB value. Checked against white before it saves."
  footer
    action "Cancel"
    action primary "Save accent"
```

**Rationale:** A row of presets all satisfy the rule, with a custom field for a reader who knows better. One tap covers the common case and cannot produce a failing color.

**Cost:** Six presets cannot hold a brand color, so the custom field carries the real work anyway, and a preset row that never changes starts to look like the whole choice.

**Reference:** Slack

### Repeatable Group

**ID:** `litespec/frontend/patterns/forms-and-fields#repeatable-group`

**Strength:** default

**Question:** A trip budget holds TRIP10, MEAL20, and TAXI05: three groups with the same fields, each with its own name, ceiling, and allocation. The group repeats, and the budget counts every repetition.

**Default:** `stacked-cards-one-per-type`

**Compact:** Below the md breakpoint the cards stack in one column, each with its code, its claimed count, its ceiling, and its actions.

**Rationale:** The stacked cards are the pick: one card per code with its ceiling and allocation readable at once, and one command to add another. The table with inline editing is the runner-up for reviewing many codes, where changing three ceilings should cost three fields and no navigation. One section per repetition is the other alternative for creation, where each code is new. Never ship a group whose repetitions cannot be removed: a code added by mistake must leave without taking its claimed amounts with it.

**Reviewed for:** Which built code group breaks from the wireframe its catalog entry chose? Open the built stacked cards and inline tables beside the catalog entry recording the chosen option and its reason.

#### `stacked-cards-one-per-type`: Stacked Cards, One per Type

```wireframe
page
  breadcrumb "Home" "Finance" "Budgets" "Budget codes"
  header
    heading "Budget codes"
    text "What a spender claims and what the budget pays."
  region "Three codes"
    list 3
      item: text "TRIP10" · text muted "412 of 500 claimed" · status "Active" positive · text "EUR 500 ceiling" · action "Edit"
  footer
    action "Add a budget code"
    action primary "Save order"
```

**Rationale:** One card per repetition with its ceiling readable at once, because repetitions are compared before one is edited. A code with claims stays visible but loses its Remove command.

**Cost:** A card shows the ceiling but hides the dates, the cost-centre rule, and the description, so editing still opens each code. The list answers what, never why.

**Reference:** Shopify

#### `a-table-with-inline-editing`: A Table with Inline Editing

```wireframe
page
  breadcrumb "Home" "Finance" "Budgets" "Budget codes"
  header
    heading "Budget codes"
    text "What a spender claims and what the budget pays."
  region "Three codes"
    table 3 "Code" "Ceiling" "Claimed" "Left"
      item: text "TRIP10" · field "Ceiling in euros" text = "500" · text "412" · text "88"
  footer
    action "Add a budget code"
    action primary "Save all"
```

```wireframe compact
page
  breadcrumb "Home" "Finance" "Budgets" "Budget codes"
  header
    heading "Budget codes"
    text "What a spender claims and what the budget pays."
  region "Three codes"
    table 3 "Code" "Ceiling" "Claimed" "Left"
      item: text "TRIP10" · field "Ceiling in euros" text = "500" · text "412" · text "88"
  footer
    action "Add a budget code"
    action primary "Save all"
```

**Rationale:** Values are edited where they sit, so changing three ceilings costs three fields and no navigation. One row in edit state at a time keeps the table readable while it is also a form.

**Cost:** A table that is also a form validates cell by cell, and a refused cell must hold its row open. At phone width the columns scroll sideways, which hides the ceiling behind a swipe.

**Reference:** Airtable

#### `one-section-per-type-with-add-another`: One Section per Type with Add Another

```wireframe
page
  breadcrumb "Home" "Finance" "Budgets" "Budget codes"
  header
    heading "Budget codes"
    text "What a spender claims and what the budget pays."
  region "Code 1 of 3"
    field "Name" text = "TRIP10"
    field "Ceiling, in euros" text = "500"
    text muted "412 claimed."
    action "Remove"
  region "Code 2 of 3"
    field "Name" text = "MEAL20"
    field "Ceiling, in euros" text = "400"
    text muted "318 claimed."
    action "Remove"
  region "Code 3 of 3"
    field "Name" text = "TAXI05"
    field "Ceiling, in euros" text = "300"
    text muted "Nothing claimed yet."
    action "Remove"
  footer
    action "Add another budget code"
    action primary "Save codes"
```

**Rationale:** The full group repeats once per entry with its own remove. Each repetition deserves the same labels, help, and errors as the first, and nothing about the second is abbreviated.

**Cost:** Three full groups make a long page, and the budget total has nowhere to sit. It fits creation, where each code is new. It drags for review, where each code is known.

**Reference:** GOV.UK

### Tag Input

**ID:** `litespec/frontend/patterns/forms-and-fields#tag-input`

**Strength:** default

**Question:** Expenses and trips carry short tags: travel, meals, client. Tags are typed freely but read everywhere, so the input must take new words and still keep old ones spelled one way.

**Default:** `chips-with-type-to-add-and-suggestions`

**Compact:** Below the md breakpoint the chips wrap above the field, and the suggestions open under it.

**Rationale:** The chips with type-to-add and suggestions are the pick: new words are allowed and old ones are offered, so `travel` is spelled once. A checkbox list in a popover is the runner-up where the tag set is closed and counted. Free text split on commas is the other alternative where a list is pasted in. Never ship free text with no suggestions: it grows `travel`, `Travel`, and `travel-meals` side by side.

**Reviewed for:** Which built tag chip breaks from the wireframe its catalog entry chose? Open the built chips, popover lists, and inherited tags beside the catalog entry recording the chosen option and its reason.

#### `chips-with-type-to-add-and-suggestions`: Chips with Type-to-Add and Suggestions

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "Tags"
  header
    heading "Tags"
    text "Short tags spenders and finance read."
  region "The tags"
    row
      status "travel" neutral
      status "meals" neutral
      status "client" neutral
    field "Tags" search = "tra"
    text muted "Lowercase letters, numbers and dashes. New words are allowed."
  footer
    action "Cancel"
    action primary "Save tags"
  popover
    text "Suggestions"
    text "travel-meals, 12 expenses"
    text "Use tra as a new tag"
```

**Rationale:** Existing tags are offered as the reader types. A new word is accepted on Enter, so the existing spelling stays without forbidding the new one. The count on each suggestion says how many expenses carry the tag.

**Cost:** Suggestions must search across every expense on every keystroke pause, and a slow search answers after the reader has already pressed Enter. The new word is accepted by timing, not by choice.

**Reference:** GitHub

#### `a-checkbox-list-in-a-popover-with-a-count`: A Checkbox List in a Popover with a Count

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "Tags"
  header
    heading "Tags"
    text "Short tags spenders and finance read."
  region "The tags"
    action "Three tags chosen"
    text muted "Tick what applies. New tags are made on the tag list."
  footer
    action "Cancel"
    action primary "Save tags"
  popover
    field "Search tags" search
    field "travel" checkbox
    field "meals" checkbox
    field "client" checkbox
    field "travel-meals" checkbox
    link "Manage the tag list"
  dialog
    heading "Manage the tag list"
    text "Renaming a tag renames it on every expense that carries it."
    list 4
      item: text "travel" · action "Rename"
    action "Done"
```

**Rationale:** A closed tag set is drawn as checkboxes with a count on the button. Ticking is faster than typing when the words already exist, and managing the list is a separate task with its own dialog.

**Cost:** A closed set cannot take a new word where it is needed, so the reader leaves the form to make one. It fits a governed vocabulary and fights a growing one.

**Reference:** Shopify

#### `free-text-split-on-commas`: Free Text Split on Commas

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "Tags"
  header
    heading "Tags"
    text "Short tags spenders and finance read."
  region "The tags"
    field "Tags" text = "travel, meals, client"
    text muted "Separate tags with commas. Spaces at the edges are removed."
    text "Read as"
    row
      status "travel" neutral
      status "meals" neutral
      status "client" neutral
  footer
    action "Cancel"
    action primary "Save tags"
```

**Rationale:** Tags are taken as one comma-separated line with the parsed chips read back below. Pasting a list from elsewhere works without retyping, and the readout shows exactly how the line was split.

**Cost:** No suggestions means no shared spelling: `travel` and `Travel` become two tags and nobody is told. The readout shows the split but cannot judge the words.

**Reference:** WordPress

### Address Field

**ID:** `litespec/frontend/patterns/forms-and-fields#address-field`

**Strength:** default

**Question:** Harbor and Pine answers for its expenses with a name, an address, and a telephone number on every spender message. The address field takes the street the law asks for and the postcode the mail needs.

**Default:** `postcode-lookup-that-fills-the-street`

**Compact:** Below the md breakpoint the postcode and number sit on one row, and the looked-up street follows under them.

**Rationale:** The postcode lookup is the pick: two short values the reader knows produce the street they would mistype. Country first is the runner-up for companies outside the lookup coverage. One search that fills the address is the other alternative for a reader who knows the street but not its spelling. Never ship a single free-text address box: one line cannot be split into street, postcode, and city again.

**Reviewed for:** Which built location input breaks from the wireframe its catalog entry chose? Open the built lookups, country shapes, and map pins beside the catalog entry recording the chosen option and its reason.

#### `postcode-lookup-that-fills-the-street`: Postcode Lookup That Fills the Street

```wireframe
page
  breadcrumb "Home" "Settings" "Organization"
  header
    heading "Organization"
    text "What Harbor and Pine is and where it works."
  region "The address"
    row
      field "Postcode" text = "1011 AB"
      field "Number" text = "12"
    action "Look up the street"
    field "Street" text = "Canal Street"
    field "City" text = "Amsterdam"
    field "Country" select = "Netherlands"
    text muted "Spenders read this address on their payouts and their mail."
  footer
    action "Cancel"
    action primary "Save address"
```

**Rationale:** Postcode plus number is asked for first, because both are known by heart. The lookup then fills the street the reader would mistype. Two short fields beat one long one.

**Cost:** The lookup covers the Netherlands and nowhere else, so every other country falls back to manual entry. Two paths through one field are two paths to test.

**Reference:** GOV.UK

#### `country-first-then-the-local-shape`: Country First, Then the Local Shape

```wireframe
page
  breadcrumb "Home" "Settings" "Organization"
  header
    heading "Organization"
    text "What Harbor and Pine is and where it works."
  region "The address"
    field "Country" select = "Netherlands"
    text muted "The fields below follow the country."
    field "Street and number" text = "Canal Street 12"
    row
      field "Postcode" text = "1011 AB"
      field "City" text = "Amsterdam"
  footer
    action "Cancel"
    action primary "Save address"
```

**Rationale:** The country comes first because the shape of an address follows it: a Dutch postcode is nothing like a German one. The labels and the validation change with the country.

**Cost:** Every supported country needs its own shape, its own labels, and its own validation. Three countries fit this pattern. Thirty need a library.

**Reference:** Stripe

#### `one-search-that-fills-the-address`: One Search That Fills the Address

```wireframe
page
  breadcrumb "Home" "Settings" "Organization"
  header
    heading "Organization"
    text "What Harbor and Pine is and where it works."
  region "The address"
    field "Find the address" search = "Canal"
    text muted "Start typing the street. The postcode and city follow."
  footer
    action "Cancel"
    action primary "Save address"
  popover
    text "Canal Street 12, 1011 AB Amsterdam"
    link "Type the address by hand instead"
```

**Rationale:** A half-typed street resolves to a full address, because the reader knows where they are but not how the database spells it. One field holds the whole search.

**Cost:** An address search reads a provider the back office pays per keystroke, and the first result may be chosen without reading. The manual fallback must stay one tap away.

**Reference:** Google Places

### Email Body Editor

**ID:** `litespec/frontend/patterns/forms-and-fields#email-body-editor`

**Strength:** default

**Question:** Claimwick mails every spender when a payout is sent, and the message carries the spender name, the expense number, and the arrival date. The editor takes the company words once and fills the spender facts per expense.

**Default:** `subject-plus-body-with-variable-chips`

**Compact:** Below the md breakpoint the subject and body stack, and the variable chips and the insert field sit under the body.

**Rationale:** The subject plus body with variable chips is the pick: the sentence is written and the facts are placed where they belong. The side-by-side spender preview is the runner-up, because a template is proven once it is seen filled. Plain text with a variable list is the other alternative where a mail client must read text. Never ship a template with no preview: variables are spelled in one place and filled in another, and only a filled copy shows that they meet.

**Reviewed for:** Which built message composer breaks from the wireframe its catalog entry chose? Open the built subject-plus-body forms and variable chips beside the catalog entry recording the chosen option and its reason.

#### `subject-plus-body-with-variable-chips`: Subject Plus Body with Variable Chips

```wireframe
page
  breadcrumb "Home" "Settings" "Email" "Payout sent"
  header
    heading "Payout sent"
    text "The message a spender looks for when a payout is sent."
  region "The message"
    field "Subject" text = "Your payout is on its way"
    field "Body" textarea = "Hi ((spender_name)), your payout left Harbor and Pine. It arrives ((arrival_date))."
    row
      status "spender_name" neutral
      status "expense_number" neutral
      status "arrival_date" neutral
    text muted "Variables fill per expense when the mail is sent."
    field "Insert a variable" select = "Choose a variable"
  footer
    action "Send a test"
    action primary "Save message"
```

**Rationale:** Template variables are listed as chips under the body, because a variable is a word with a value, not code. The body carries each variable where it belongs, and the chips name what the editor accepts.

**Cost:** The body holds brackets as plain text, so the editor must highlight each variable it recognizes and refuse a misspelled one. The chips and the insert select are two ways to do the same thing, and both must agree.

**Reference:** Mailchimp

#### `side-by-side-preview-of-the-customer-message`: Side-by-Side Preview of the Spender Message

```wireframe
page
  breadcrumb "Home" "Settings" "Email" "Payout sent"
  header
    heading "Payout sent"
    text "The message a spender looks for."
  split 1:1
    region "Edit"
      field "Subject" text = "Your payout is on its way"
      field "Body" textarea = "Hi ((spender_name)), your expense ((expense_number)) was paid. It arrives ((arrival_date))."
    card "Spender preview"
      heading small "Your payout is on its way"
      text "Hi Ava,"
      text "Your expense EXP-1042 was paid. It arrives Sat 14 Mar 2026."
      text muted "Harbor and Pine, Canal Street 12, 1011 AB Amsterdam"
  footer
    action "Send a test"
    action primary "Save message"
```

**Rationale:** A template is previewed beside the editor with a real record filled in, because a variable is only proven by its value. Ava's payout fills every variable this template holds.

**Cost:** Two columns collapse to one on a phone, so the preview sits below the editor and the reader scrolls between them. A long template makes a long round trip.

**Reference:** Postmark

#### `plain-text-with-a-variable-list-beside-it`: Plain Text with a Variable List Beside It

```wireframe
page
  breadcrumb "Home" "Settings" "Email" "Payout sent"
  header
    heading "Payout sent"
    text "The message a spender looks for."
  split 1:1
    region "The message"
      field "Subject" text = "Your payout is on its way"
      field "Body" textarea = "Hi ((spender_name)), your payout left Harbor and Pine. It arrives ((arrival_date))."
    region "Variables"
      fact "((spender_name))" "Ava"
      fact "((expense_number))" "EXP-1042"
      fact "((arrival_date))" "14 Mar 2026"
      text muted "Spelled with double brackets. The value fills when the mail is sent."
  footer
    action "Send a test"
    action primary "Save message"
```

**Rationale:** A template is written as plain text with double-bracket variables, and each one is listed beside the editor with an example value. Nothing about the message can break in a mail client that reads text.

**Cost:** Double brackets are learned, not guessed: one missing bracket fails silently or loudly depending on the parser. The editor must highlight what it recognizes as the reader types.

**Reference:** GOV.UK
