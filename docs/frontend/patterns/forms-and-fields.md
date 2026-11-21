# Form and Field Patterns

**Stack:** any
**Concern:** frontend-ui

These patterns decide how a form takes each value and what the form reports around it. Each field kind gets one shape, so entry stays fast and each error message names exactly what to fix.

An owner picks an option from the wireframe drawings for each field the form needs, and the team records each choice in its pattern catalog. Every option keeps the defaults in Conventions unless the project records another choice, whatever the choice. This page belongs to the Forms and Input category. Its patterns are tested layouts a team chooses from, not required rules.

## Concepts

### How to Read a Pattern

A pattern answers one recurring question about a field kind, such as how a money field names its currency. It opens with that question, then names its default option: the shape the project builds unless the owner records another choice. It then states how the default behaves on a narrow screen, why the default was chosen, which alternative fits which case, and which shape to leave out.

Each option is one `####` heading with a stable option ID, one wireframe drawing plus sometimes a compact drawing for a different phone layout, and three short parts: its rationale, its cost, and sometimes a reference to the outside product it learns from.

A wireframe states the structure of the option and the copy it shows, and nothing about color, font, or components. The team builds the chosen option in its own stack and design system. For example, the money field pattern draws a text input with the currency in its label as the default, and an owner who needs preset amounts records the chips option instead.

Each choice is recorded in the project's pattern catalog, the file that holds the project's interface decisions. The [UI patterns page](../patterns.md) states the catalog contract and the wireframe language.

### The Running Example

The drawings follow one Claimwick expense: Ava's EUR 45.00 hotel night for a Rotterdam client visit, its EUR 5.00 booking fee, the Amsterdam sales cost centre that pays, and the trips, payouts, and approval policies around it.

Every drawing keeps the eight defaults from the Conventions section below unless the project records another choice. Each field shows a visible label naming its value. An error appears only after the field is left, and clears as soon as the value turns valid. Each error reaches assistive technology.

Each required field carries a marker. Submit stays enabled so it can reveal errors. Each field states its purpose for autocomplete. Enter is ignored while an input method still composes text.

A person's name is one field accepting any script, and a refused submit keeps everything the reader typed, as [Keep the Reader's Input Through a Failure](feedback-and-states.md#keep-the-readers-input-through-a-failure) states.

## Conventions

### Name Every Field with a Visible Label

**ID:** `litespec/frontend/patterns/forms-and-fields#name-every-field-with-a-visible-label`

**Strength:** default

**Default:** Carry a visible label naming its value on every field.

**Replacement:** A project that replaces the default records how its fields name their values.

**Rationale:** A placeholder vanishes on the first keystroke, so a filled field with no label beside it no longer says what it holds. Screen readers announce the label aloud, and sighted staff read it when they return to a filled form to check each value before saving. Without a visible label, both groups have to guess what each value means.

**Reviewed for:** Does every built field show a visible label naming its value? Open each form on the built screens and read every field's label.

### Show a Field Error After the Field Is Left

**ID:** `litespec/frontend/patterns/forms-and-fields#show-a-field-error-after-the-field-is-left`

**Strength:** default

**Default:** Show a field error only once the user leaves the field with a changed value or submits the form, and clear it as soon as the value becomes valid.

**Replacement:** A project that replaces the default records when its field errors appear and clear.

**Rationale:** An error shown while the reader is still typing marks a value they have not finished entering, and one that lingers after the fix makes the reader doubt the repair. The message appears once the field is left or the form is submitted, and it clears as soon as the value turns valid.

**Reviewed for:** Does any built field show an error while it is still being typed in, or keep one after the value turns valid? Open the built forms, type in each field, leave it, fix it, and watch when each message appears and clears.

### Expose a Field Error to Assistive Technology

**ID:** `litespec/frontend/patterns/forms-and-fields#expose-a-field-error-to-assistive-technology`

**Strength:** default

**Default:** Set `aria-invalid="true"` on a field and point `aria-describedby` at its error only while the error shows.

**Replacement:** A project that replaces the default records how its fields expose errors to assistive technology.

**Rationale:** A visible error message alone reaches only sighted readers. Marking the field invalid and pointing at the error message tells assistive technology which field failed and what the message says, which is what WCAG 3.3.1 Error Identification and WCAG 4.1.2 Name, Role, Value ask for: an error described in text, and a field exposing its state. An untouched empty required field carries neither mark, so it is never announced as invalid before the reader reaches it.

**Reviewed for:** Does every built field expose its invalid state and point at its error while the error shows, and carry neither before it is touched? Open the built forms and read each field's attributes with its error shown and hidden.

### Mark Every Required Field

**ID:** `litespec/frontend/patterns/forms-and-fields#mark-every-required-field`

**Strength:** default

**Default:** Carry the `required` attribute and a visible marker on every required field, with the form explaining the marker once, above its first field.

**Replacement:** A project that replaces the default records how its forms mark required fields.

**Rationale:** WCAG 3.3.2 Labels or Instructions asks that a required field say so before it is submitted. A visible marker tells sighted readers which fields they must fill, and the `required` attribute tells the same to screen readers. Explaining the marker once, above the first field, teaches it for the whole form, so every field needs only the mark itself.

**Reviewed for:** Does every built required field show a visible marker and carry the `required` attribute, and does each form explain the marker above its first field? Open the built forms and read each required field plus the line above the first one.

### Keep Submit Enabled to Reveal Errors

**ID:** `litespec/frontend/patterns/forms-and-fields#keep-submit-enabled-to-reveal-errors`

**Strength:** default

**Default:** Keep a submit control enabled while the form holds errors, so pressing it moves focus to the summary or the first field in error.

**Replacement:** A project that replaces the default records how its submit controls treat a form holding errors.

**Rationale:** A grayed-out submit button gives no reason and no way forward. A submit that stays enabled can be pressed, and pressing it moves focus to the summary or to the first field in error, which names what to fix. The form still refuses the submit, and keeps every entered value.

**Reviewed for:** Does any built submit control disable itself while the form holds errors? Open the built forms with errors in them and press each submit.

### State Each Field's Purpose

**ID:** `litespec/frontend/patterns/forms-and-fields#state-each-fields-purpose`

**Strength:** default

**Default:** Carry the `autocomplete` token for its purpose on a field collecting information about the user.

**Replacement:** A project that replaces the default records how its fields state their purpose.

**Rationale:** Without the token, the browser cannot offer the stored bank account holder or email, so the reader retypes both on every claim. With the right token, the browser fills each value once, and the reader checks it instead of typing it. That is what WCAG 1.3.5 Identify Input Purpose asks for: the purpose of each such field must be programmatically determinable.

**Reviewed for:** Does every built field collecting information about the user carry the `autocomplete` token for its purpose? Open the built forms and read the token on each such field.

### Refuse Enter While Text Is Being Composed

**ID:** `litespec/frontend/patterns/forms-and-fields#refuse-enter-while-text-is-being-composed`

**Strength:** default

**Default:** Ignore the Enter key in a field that acts on it while an input method is composing text.

**Replacement:** A project that replaces the default records how its fields treat Enter during composition.

**Rationale:** Someone typing a vendor name in Japanese or Chinese presses Enter to confirm a candidate in the input method, not to submit anything. If a tag input or inline rename acts on that keystroke, it files half-composed text. Ignoring Enter until composition ends keeps the submitted value whole.

**Reviewed for:** Does any built field acting on Enter submit half-composed text from an input method? Open the built tag inputs and other fields acting on Enter, and confirm a candidate with Enter.

### Take a Name in One Field of Any Script

**ID:** `litespec/frontend/patterns/forms-and-fields#take-a-name-in-one-field-of-any-script`

**Strength:** default

**Default:** Take a person's name in one field that accepts letters of any script.

**Replacement:** A project that replaces the default records how its name entries split the name and which scripts they accept.

**Rationale:** Given and family names are not universal, so splitting them forces staff into parts their name lacks, and a Latin-only rule refuses real staff. One field accepting letters of any script holds every name staff enter.

**Reviewed for:** Does any built name entry split the name, or refuse letters outside Latin? Open the built staff and spender forms and enter a name in a non-Latin script.

### Match the Keyboard to the Value

**ID:** `litespec/frontend/patterns/forms-and-fields#match-the-keyboard-to-the-value`

**Strength:** default

**Default:** Set `inputmode` on a numeric or code field for its value, and turn off autocorrect and autocapitalize on an identifier field such as an IBAN or a reference.

**Replacement:** A project that replaces the default records which fields keep the default text keyboard and why.

**Rationale:** A reader entering an amount or a booking reference sees keys for the value instead of the full alphabet. On an identifier field, autocorrect stays off, so it never rewrites an IBAN or a booking reference.

**Reviewed for:** Does any built numeric, code, or identifier field keep the default text keyboard with no recorded reason? Open the built fields and read the project's recorded convention beside them.

## Patterns

### Money Field

**ID:** `litespec/frontend/patterns/forms-and-fields#money-field`

**Strength:** default

**Question:** Staff type amounts, fees, reversals, and payout figures by hand; the software chooses none of them. The question is how a money field takes its value: the shape of the input, how it names the currency, where the symbol sits, and which total it restates beside the field.

**Default:** `text-input-currency-in-the-label-symbol-inside`

**Compact:** Below the md breakpoint the field goes full width, with the amount and its help line stacked in one column.

**Rationale:** The default is the text input with the currency in its label: the label names the currency for a screen reader, and the field accepts any typed amount with a comma or a point as the decimal separator. The restated payout total is the runner-up: it belongs on every amount an approver sees, because it stops the fee from surprising either side at payout. Preset chips are the other alternative where one round amount repeats. The stepper stays out: a continuous amount cannot be reached by stepping.

**Reviewed for:** Does every built amount input match the wireframe its catalog entry chose? Open the built amount, currency, and total fields beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The currency lives in the label, where a screen reader announces it, and the symbol sits inside the field as decoration that assistive technology skips. Because the field stays a plain text input, the reader types any amount instead of stepping between fixed values.

**Cost:** The reader can type the symbol into the field, so validation accepts the symbol rather than refusing it. The field allows at most two decimal places.

**Reference:** HMRC

#### `amount-with-the-payout-total-restated`: Amount with the Payout Total Restated

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

**Rationale:** The reader sees the amount and the EUR 50.00 payout total on the same screen before saving, so the fee surprises neither side at payout.

**Cost:** The restated total follows the typing, which a static wireframe cannot show. It also adds one line under every amount field, even where the fee is zero.

#### `preset-amount-chips-for-the-common-amounts`: Preset Amount Chips for the Common Amounts

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

**Rationale:** Where most lines land on a round amount, the common amounts sit one tap away, and the field stays for the odd amount.

**Cost:** The presets are company data that someone must learn or configure, and a stale set pushes the reader toward last year's amounts. Five chips plus a field also run wider than a field alone on a phone.

### Choosing a Record

**ID:** `litespec/frontend/patterns/forms-and-fields#choosing-a-record`

**Strength:** default

**Question:** A reader picks a reference to another record from a picker showing names, and types no identifier. The failure to avoid is a select reading "Choose a cost centre" beside a field holding "CC-0C893968A2", as if the code were a value worth typing.

**Default:** `searchable-picker-showing-names`

**Compact:** Below the md breakpoint the picker opens as a full-width sheet, with the search field on top and the results under it.

**Rationale:** The default is the searchable picker over names: nobody types an identifier, and search beats scrolling for every list longer than a screen. The runner-up shows one distinguishing fact per row; prefer it where records share a name. The plain select is the other alternative for a short, fixed list. A bare identifier field stays out: a code such as CC-0C893968A2 is not a value a reader can type or check.

**Reviewed for:** Does every built record picker match the wireframe its catalog entry chose? Open the built searchable pickers and recent lists beside the catalog entry recording the chosen option and its reason.

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
      item: text "Amsterdam sales"
    text muted "A cost centre carries its budget and its spend."
    field "Name" text
```

**Rationale:** The picker searches over names and shows nothing else. Search beats scrolling every time the list runs longer than a screen.

**Cost:** A picker hides how many records exist, so a reader who knows the cost centre is "Zaandam" searches instead of scanning. It also needs an empty-result state that offers to create the missing record.

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

**Rationale:** Each row carries the one fact that tells two similar records apart: the budget for a cost centre, the date for a trip, the state for an approval policy.

**Cost:** The list grows wider for a column that pays off only where records share a name. Where names are already unique, it is noise.

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

**Rationale:** The select lists names with fewer moving parts than a picker, and it works from the keyboard with no extra setup.

**Cost:** Every option stays hidden until the select opens, so a 40-cost-centre list becomes a long scroll with no search. A reader who knows the name scrolls to it anyway. Only a short, fixed list suits the select.

**Reference:** GOV.UK

### Date and Time Field

**ID:** `litespec/frontend/patterns/forms-and-fields#date-and-time-field`

**Strength:** default

**Question:** Staff enter dates and times for trips, payouts, and trip plans. The question is which control takes the value: a field with a calendar, three text inputs, or a date and a time beside each other. The native browser date input stays out: it draws the browser chrome instead of the application control, and it renders differently in every browser.

**Default:** `one-text-field-with-a-calendar-that-opens-beside-it`

**Compact:** Below the md breakpoint the calendar opens as a full-width sheet under the field, showing one month at a time.

**Rationale:** The default is one typed field with a calendar beside it: there is always a way to type, and the calendar serves a reader who would rather click. The runner-up is three text inputs; prefer it where accessibility review demands it, at the cost of the slowest entry in the form. A date and a time on one row are the other alternative where a form always takes both. The native date input stays out: it draws the browser chrome instead of the application control.

**Reviewed for:** Does every built calendar input match the wireframe its catalog entry chose? Open the built date, duration, and range fields beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** One field takes typing and the calendar beside it takes clicking. A reader who knows the date types it and never opens the grid; a reader who would rather click never has to type.

**Cost:** A calendar is a grid of 42 cells to build, translate, and test in two languages. It is the most code in the form package for a single field.

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

**Rationale:** Day, month, and year each get a numeric field with input modes and autocomplete tokens, and every browser renders those fields the same. Together those make it the most accessible answer.

**Cost:** Three fields hold one value, so 94 dates a year cost 282 numbers typed. The most accessible answer is also the slowest one to fill in.

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

**Rationale:** Date and time sit side by side as two fields for a form that always takes both. The time zone is stated once above them instead of repeated under each one.

**Cost:** On a narrow screen the row wraps to three lines. The time zone reads once in the header, not beside each field.

### Text Field and Its States

**ID:** `litespec/frontend/patterns/forms-and-fields#text-field-and-its-states`

**Strength:** default

**Question:** Every text field draws several states: empty, filled, focused, refused, disabled, and read-only, with the label visible above the input in all of them. A placeholder never serves as the label, because it vanishes on the first keystroke. The question is how the field draws those states, and whether it confirms a good value before submit.

**Default:** `label-above-help-under-six-states-on-one-page`

**Compact:** Below the md breakpoint the states stack in one column, each with its label and help line.

**Rationale:** The default is the six-state reference page, the only option pinning every state one field draws. The runner-up confirms a good value inline; prefer it on create forms, where it saves a refused submit. Floating labels are the other alternative for a compact form. The placeholder-as-label stays out: it vanishes on the first keystroke, and its color misses contrast.

**Reviewed for:** Does every built text input match the wireframe its catalog entry chose? Open the built labels, counts, and growing areas beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The page is the component reference for the field. Empty, filled, focused, refused, disabled, and read-only stand together, so the team checks one control instead of six screenshots.

**Cost:** Nobody ships this page: six instances of one field make a style guide, not a screen. It exists so the states never drift apart.

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

**Rationale:** A quiet line confirms a good value where it was typed, instead of waiting for the submit. Confirming the wanted name early saves a refused submit.

**Cost:** Live validation checks a field before it is finished, so it stays a narrow exception. The check waits for a pause in typing, or it interrupts the reader.

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

**Rationale:** The label starts inside the field and moves above the value once the field fills, so it never vanishes. The field keeps the compact look the placeholder promised, without breaking the label rule.

**Cost:** The empty state still reads as a placeholder, with small text and low contrast. A floated label also runs smaller than every other label in the back office, so a reader with low vision reads it at a size the rest of the form never uses.

**Reference:** Material Design

### Choosing One of a Few Options

**ID:** `litespec/frontend/patterns/forms-and-fields#choosing-one-of-a-few-options`

**Strength:** default

**Question:** A reader picks one value from a closed set, such as a receipt naming rule or a plan. A select hides its options until it opens, so visible controls win for short sets: radios for two to five exclusive options, a segmented control for two or three short values, and a select or a searchable picker for six or more. A question preselects nothing, though a setting may carry a default. The question is which control carries the choice: a radio list, a segmented control, or a select.

**Default:** `a-radio-list-with-a-description-per-option`

**Compact:** Below the md breakpoint the radio list stays in one column, with each description under its option.

**Rationale:** The default is the radio list with a description per option, since each choice carries the sentence that lets a reader tell names apart that sound alike. The runner-up is the segmented control; prefer it for two or three short values with no descriptions. The select is the other alternative for a long list on a settings page. The native multi-select stays out: it needs a modifier key for a second value and hides which values are chosen, so a checkbox list replaces it.

**Reviewed for:** Does every built single-choice control match the wireframe its catalog entry chose? Open the built radios, segments, and plan cards beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** Each choice carries a sentence saying what it means for the spender. Without that sentence, "The spender name" and "Every line named" read as the same choice twice.

**Cost:** Three radio rows with descriptions make a tall block. It does not scale to twelve options, which a permission picker does need.

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

**Rationale:** Two or three values sit side by side, all visible at once. Nothing hides behind a menu.

**Cost:** Every label must fit the control, so "A blank expense" and "A template" both read shortened. The control grows with its longest label, which caps how many values it holds.

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

**Rationale:** Where the value has no default and the options are many, a select with a real empty first option is the least-bad shape: it asks the question without answering it.

**Cost:** The empty option is a placeholder, and placeholders stay out of components. A select whose first choice is a blank awaiting replacement holds two states, not one.

**Reference:** GOV.UK

### Switch or a Checkbox

**ID:** `litespec/frontend/patterns/forms-and-fields#switch-or-a-checkbox`

**Strength:** default

**Question:** Settings pages hold many switches and checkboxes, and the payout editor holds a toggle for the cost-centre line. The question is which control each setting gets and what its label says, since the label carries the difference between on and off.

**Default:** `a-switch-with-the-label-and-its-consequence`

**Compact:** Below the md breakpoint each switch keeps its label and its consequence line in one column.

**Rationale:** The default is the switch with its consequence stated: the label says what is on, and the line under it says what turning it off does. The runner-up applies instantly with an undo toast; prefer it for low-risk settings a reader flips often. A checkbox is the other alternative where the off state is the default. A switch whose label names the control instead of the outcome stays out.

**Reviewed for:** Does every built toggle match the wireframe its catalog entry chose? Open the built switches, checkboxes, and instant applies beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The label says what is on, and the line under it says what turning it off does. The reader sees both directions of the choice before touching it.

**Cost:** The label carries the whole difference between on and off, and a reader still looks twice to see the current state. A switch also draws more attention than a setting that rarely changes needs.

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

**Rationale:** A low-risk switch applies at once, and a toast names what changed with Undo beside it. There is no Save to forget and no dialog to dismiss. Instant apply fits only a setting applied on its own, never a form holding other unsaved edits.

**Cost:** Every flip is an audited command the moment it happens, with no review step before it. The shape fits display settings, not money, access, or anything a toast cannot undo.

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

**Rationale:** A checkbox fits a setting whose default is off and whose action is a tick. The label reads as what ticking does, not as a name for the box.

**Cost:** A checkbox and a switch look nearly identical here, and readers conflate them. Only the phrasing of the label tells the two apart.

**Reference:** GOV.UK

### Upload Field

**ID:** `litespec/frontend/patterns/forms-and-fields#upload-field`

**Strength:** default

**Question:** Claimwick records take files, from a receipt photo to a staff spreadsheet. A receipt needs a stated type and size, and a staff import needs stated columns and row-level refusals. The question is how the upload field states its contract up front, and how it reports progress and refusals while the file moves.

**Default:** `a-drop-zone-that-states-what-it-accepts`

**Compact:** Below the md breakpoint the drop zone takes a tap to choose a file, with its type and size line staying under it.

**Rationale:** The default is the drop zone stating its types and size limit: nobody learns the limit by being refused. The runner-up is one progress row per file; prefer it for imports of several files, where one bar per file beats one bar for all. A CSV upload stating its columns first is the other alternative where the file must match a fixed shape. A CSV upload with no stated columns stays out: a file refused for a format nobody stated is a refusal the application caused.

**Reviewed for:** Does every built file input match the wireframe its catalog entry chose? Open the built drop zones, CSV reads, and progress rows beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The zone states the accepted types and the size limit in a sentence under it. The reader knows the contract before choosing a file, instead of discovering the limit by being refused.

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

**Rationale:** Two files never finish together, so each file gets its own row with its own progress and its own cancel. One shared bar would misreport both.

**Cost:** A row per file is a list the page must manage, with a cancel, a remove, and a retry per row. For a single receipt that list is heavier than the one zone it replaces.

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

**Rationale:** For an import, the field states the four column names and offers a template before anything is uploaded. A file never fails a format the page did not state.

**Cost:** Stating four columns above the zone costs a paragraph of prose. That prose belongs on the page beside the field, not inside a component.

### Reporting a Refused Submit

**ID:** `litespec/frontend/patterns/forms-and-fields#reporting-a-refused-submit`

**Strength:** default

**Question:** When the API refuses a submit, the form reports what is wrong and where. WCAG 3.3.1 and 3.3.3 ask for a summary that takes focus and links to each field, plus a message on each field. The question is how the form carries that report.

**Default:** `a-summary-above-the-form-plus-a-message-on-each-field`

**Compact:** Below the md breakpoint the summary sits above the form with its links, and each message stays under its field.

**Rationale:** The default is the summary with focus, plus per-field messages. It meets WCAG 3.3.1 and 3.3.3, and the links carry the reader to each field. The runner-up validates on leaving each field; prefer it as a complement, never a replacement, since it confirms good values early but leaves a reader facing three errors with no overview. Messages on the fields only are the other alternative for a short form. The dialog summary stays out: it hides the form the reader must fix. Whatever the form reports, it keeps every entered value, as [Keep the Reader's Input Through a Failure](feedback-and-states.md#keep-the-readers-input-through-a-failure) states.

**Reviewed for:** Does every built submit-error report match the wireframe its catalog entry chose? Open the built summaries and per-field messages beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The summary comes first and takes focus, with each entry linking to its field. The field repeats the same wording, so the message reads the same in the summary and beside the field.

**Cost:** The wording exists in two places per error, so it is generated once and used twice. Hand-written copies drift apart, and the drift stays invisible.

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

**Rationale:** Each field is checked when left and marked good or wrong where it sits. The reader fixes one field and moves on, leaving nothing for the submit to refuse.

**Cost:** The shape complements the summary and never replaces it: with three errors and no summary the reader still cannot see them all at once. A check on blur fires while the form is half-filled.

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

**Rationale:** Each field carries its own message and nothing else. The reader sees each error exactly where the fix goes.

**Cost:** On a long form the first error may sit below the fold. The reader submits, sees nothing change, and concludes the button is broken.

### Work That Is Not Saved

**ID:** `litespec/frontend/patterns/forms-and-fields#work-that-is-not-saved`

**Strength:** default

**Question:** A form shows work that is not saved yet. Nothing reads as saved before it is, and leaving drops no typed work without a warning. The question is how the form marks unsaved work and guards the way out.

**Default:** `a-visible-unsaved-marker-beside-the-save`

**Compact:** Below the md breakpoint the unsaved note and the Save button stay together at the foot of the form.

**Rationale:** The default is the visible unsaved marker beside Save: a reader never has to remember whether anything was typed. The runner-up pins a save bar to the top; prefer it for long settings pages where the form actions scroll out of view. Autosave with a saved marker is the other alternative for a note written over several sittings. Silent autosave on money or access stays out: each save would be an audited command, and an audit entry per keystroke batch is not a trail a reader can follow.

**Reviewed for:** Does every built unsaved marker match the wireframe its catalog entry chose? Open the built dirty flags, drafts, and save bars beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The moment a field changes, the Save button shows the count and a note says what changed. The reader never has to remember whether anything was typed.

**Cost:** The count of changed fields is a derived fact, so the form computes it rather than the route. A Save button that changes its own label moves under the pointer.

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

**Rationale:** On a long settings page, a pinned bar keeps Save and Discard in reach. It appears with the first keystroke and leaves with the save.

**Cost:** A pinned bar covers content on a phone, and it follows the reader past sections it has nothing to do with. It needs the same dirty tracking as the marker, so it adds reach rather than information.

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

**Rationale:** The form saves itself every few seconds, and a quiet marker says when. Nothing is lost when the browser closes, and no dialog is ever needed.

**Cost:** On money or access, each autosave is a command, which means an audit entry per keystroke batch. A past decision must stay reconstructable, and a trail of keystroke batches is not one a reader can follow.

### Picking a Period

**ID:** `litespec/frontend/patterns/forms-and-fields#picking-a-period`

**Strength:** default

**Question:** Payouts and reporting run on periods. Two typed dates cover every window at the cost of asking for both edges, while presets cover the common windows in one tap. The question is how the period control combines typing and presets.

**Default:** `two-date-inputs-with-presets`

**Compact:** Below the md breakpoint the two date fields stack, with the presets above them and the payout date under the second field.

**Rationale:** The default is two typed dates with presets: one tap covers the common windows, typing covers the rare ones, and both edges stay visible. The runner-up is the painted calendar; prefer it on reporting screens where the window itself is the question. The month picker is the other alternative for a report running by calendar month. Presets alone with no typed fallback stay out: no preset reaches the filing-to-payout window the payout check needs.

**Reviewed for:** Does every built range picker match the wireframe its catalog entry chose? Open the built presets, month pickers, and painted calendars beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The presets fill both fields, and either edge can then be adjusted by typing. The common windows cost one tap and the rare ones stay possible. The presets and the fields behave as one control.

**Cost:** Two controls describe one value. The preset row shows Custom the moment either date is edited, or it names a window the dates no longer match.

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

**Rationale:** The chosen range is painted across the calendar, so the reader sees the window instead of reading it from two dates. The endpoints stay readable as dates under the control.

**Cost:** A painted month shows one month, and the payout window usually spans two. Two months side by side show the window truthfully, and they do not fit on a phone.

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

**Rationale:** Payout and tax reporting run by calendar month. Picking March 2026 in one control beats typing the first and last day of the month.

**Cost:** The control draws whole months only. It holds no window crossing a month edge, like the fortnight around the payout.

### Slug or URL Field with a Preview

**ID:** `litespec/frontend/patterns/forms-and-fields#slug-or-url-field-with-a-preview`

**Strength:** default

**Question:** A trip takes a slug, the short part typed in, while spenders read the full address: they link it and print it. The question is how the field takes the slug and previews the address before saving.

**Default:** `slug-with-the-live-address-under-it`

**Compact:** Below the md breakpoint the live address wraps under the slug field, with the copy command beside it.

**Rationale:** The default is the slug with its live address under it: the reader types the short part and reads the full address before saving. The runner-up transliterates from the name; prefer it at creation, where typing the same words twice is pure overhead. The address as a fact is the other alternative once it is shared. An address that stays unreadable until after it is saved stays out.

**Reviewed for:** Does every built address input match the wireframe its catalog entry chose? Open the built slugs, transliterations, and availability checks beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** The full address sits under the slug field and updates as the short value is typed, so the short value is never saved unseen. The copy command beside it is how the address leaves the back office.

**Cost:** The address updates as the reader types, which a static wireframe cannot show. It also states the host the platform gives, which no control on this screen changes.

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

**Rationale:** The name and the address agree nine times in ten, so the address is built from the name and stays editable after. Typing both by hand is the same words twice.

**Cost:** The generated value transliterates Dutch diacritics the way the reader expects. Generation runs once at creation, never again, so a renamed trip never silently changes its address.

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

**Rationale:** Links already shared depend on the address, so an address in use reads as a fact with a change command. The 30-day redirect is what makes the change survivable.

**Cost:** Changing a value takes two steps, and the redirect must be real behavior, not a sentence. The redirect has to be built: without it, the sentence promises behavior that does not exist.

**Reference:** GitHub

### Capacity Field

**ID:** `litespec/frontend/patterns/forms-and-fields#capacity-field`

**Strength:** default

**Question:** A budget code takes money from one cost-centre budget: Amsterdam sales holds EUR 10,000, and every code draws from the same budget. The question is how the capacity field shows the ceiling, the split, and the refusal past it.

**Default:** `a-number-with-the-budget-ceiling-restated`

**Compact:** Below the md breakpoint the ceiling field sits above the restated budget facts in one column.

**Rationale:** The default is the number with the budget ceiling restated. One field, the budget it draws from, and what is left sit on the same screen. The runner-up is the split across codes; prefer it on the budget page, where the question is the whole budget rather than one code. Claimed as a read-only fact beside an editable ceiling is the other alternative where claimed must never be typed. A capacity with no ceiling stated stays out: a ceiling stated without its budget lets the code overspend.

**Reviewed for:** Does every built ceiling input match the wireframe its catalog entry chose? Open the built splits, steppers, and overage messages beside the catalog entry recording the chosen option and its reason.

#### `a-number-with-the-budget-ceiling-restated`: A Number with the Budget Ceiling Restated

```wireframe
page
  breadcrumb "Home" "Finance" "Budgets" "TRIP10" "Capacity"
  header
    heading "Capacity"
    text "How much TRIP10 can spend."
  region "Money on the code"
    field "Ceiling, in euros" text = "9600"
    text muted "EUR 9,412 claimed. EUR 188 still to claim."
    text "The budget is 94 percent spent."
    fact "Amsterdam sales holds" "EUR 10,000"
    text muted "PARTNER holds EUR 400 of it."
  footer
    action "Cancel"
    action primary "Save capacity"
```

**Rationale:** Every ceiling field carries the budget total beside it: a code allocation means nothing without the budget it draws from. The line under the field says how spent the budget is.

**Cost:** The restated figures update when another code changes, or two screens state two budgets. One field depends on a read of the whole cost centre behind it.

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

**Rationale:** One row per code shows claimed against its ceiling. The question is the budget, not the row, and the leftover line is what the reader came to read.

**Cost:** A row per code carries the columns a table would. At twelve codes it needs a table again, and editing still jumps to one code at a time.

#### `claimed-as-a-read-only-fact-beside-an-editable-ceiling`: Claimed as a Read-Only Fact Beside an Editable Ceiling

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
    field "Ceiling, in euros" text = "9600"
    text muted "Must stay at or above 9,412."
  footer
    action "Cancel"
    action primary "Save capacity"
```

**Rationale:** Claimed is shown as a fact beside the controls that change it. Claimed is what happened, and the ceiling is what the reader decides, so the two never share a field.

**Cost:** Three figures plus a field for one decision. The derived Left line updates as the ceiling is typed, and it uses the most screen space per field in this pattern.

**Reference:** Stripe

### Color Field

**ID:** `litespec/frontend/patterns/forms-and-fields#color-field`

**Strength:** default

**Question:** A company brands its approval mails with one accent color: a `#RRGGBB` value dark enough for white button text to read on it. The question is how the field takes the value and proves the contrast before the value is refused.

**Default:** `hex-field-with-a-swatch-and-a-contrast-readout`

**Compact:** Below the md breakpoint the swatch sits above the hex field, and the contrast readout stays under it.

**Rationale:** The default is the hex field with a swatch and a contrast readout. It takes any color, shows it, and states the ratio the validation enforces. The runner-up is the approver-button preview, which shows the ratio rather than stating it. Preset swatches are the other alternative for a reader who does not know a hex value. A free color with no contrast check stays out: a pale accent ships white text nobody can read.

**Reviewed for:** Does every built accent picker match the wireframe its catalog entry chose? Open the built swatches, wells, and contrast readouts beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** A hex field is paired with a swatch and the contrast rule the value is checked against. A color value means nothing until it is seen, and the readout states the validation rule, so the reader learns the rule before saving rather than after a refusal.

**Cost:** The ratio recomputes as the value is typed, and an unfinished value has no ratio to state. An unfinished value has no ratio yet, so the field waits for a complete value instead of refusing it.

**Reference:** Shopify

#### `the-accent-previewed-on-an-approver-button`: The Accent Previewed on an Approver Button

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

**Rationale:** The brand color is previewed on the button the approver presses. The button shows the ratio rather than stating it: the reader sees white on the accent instead of reading a number.

**Cost:** A preview is a second surface that matches the typed value. It shows the typed value, not the saved one, and updates as the reader types. A stale preview shows a value the reader will not get.

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

**Rationale:** A row of presets all satisfy the rule, with a custom field for a reader who knows better. One tap covers the common case and never produces a failing color.

**Cost:** Six presets hold no brand color, so the custom field carries the real work anyway. A preset row that never changes starts to look like the whole choice.

**Reference:** Slack

### Repeatable Group

**ID:** `litespec/frontend/patterns/forms-and-fields#repeatable-group`

**Strength:** default

**Question:** A trip budget holds TRIP10, MEAL20, and TAXI05: three groups with the same fields, each with its own name, ceiling, and allocation. The group repeats, and the budget counts every repetition. The question is how the form draws the repetition: cards, a table, or one section per entry.

**Default:** `stacked-cards-one-per-type`

**Compact:** Below the md breakpoint the cards stack in one column, each with its code, its claimed count, its ceiling, and its actions.

**Rationale:** The default is stacked cards: one card per code with its ceiling and allocation readable at once, and one command to add another. The runner-up is the table with inline editing; prefer it for reviewing many codes, where changing three ceilings costs three fields and no navigation. One section per repetition is the other alternative for creation, where each code is new. A group whose repetitions cannot be removed stays out: a code added by mistake is removed while its claimed amounts stay.

**Reviewed for:** Does every built code group match the wireframe its catalog entry chose? Open the built stacked cards and inline tables beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** One card per repetition carries its ceiling readable at once. Repetitions are compared before one is edited, and a code with claims stays visible but offers no Remove command.

**Cost:** A card shows the ceiling but hides the dates, the cost-centre rule, and the description, so editing still opens each code. The list shows what each code holds, not why it was set that way.

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

**Cost:** A table that is also a form validates cell by cell, and a refused cell holds its row open. At phone width the columns scroll sideways, which hides the ceiling behind a swipe.

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

**Rationale:** The full group repeats once per entry with its own remove. Each repetition gets the same labels, help, and errors as the first, and nothing about the second is abbreviated.

**Cost:** Three full groups make a long page, and the budget total has nowhere to sit. The shape fits creation, where each code is new, and slows review, where each code is already known.

**Reference:** GOV.UK

### Tag Input

**ID:** `litespec/frontend/patterns/forms-and-fields#tag-input`

**Strength:** default

**Question:** Expenses and trips carry short tags: travel, meals, client. Tags are typed freely but read everywhere. The question is how the input takes new words while keeping old ones spelled one way.

**Default:** `chips-with-type-to-add-and-suggestions`

**Compact:** Below the md breakpoint the chips wrap above the field, and the suggestions open under it.

**Rationale:** The default is chips with type-to-add and suggestions. New words are allowed and old ones are offered, so `travel` is spelled once. The runner-up is a checkbox list in a popover; prefer it where the tag set is closed and counted. Free text split on commas is the other alternative where a list is pasted in. Free text with no suggestions stays out: it grows `travel`, `Travel`, and `travel-meals` side by side.

**Reviewed for:** Does every built tag chip match the wireframe its catalog entry chose? Open the built chips, popover lists, and inherited tags beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** Existing tags are offered as the reader types, and a new word is accepted on Enter. The existing spelling stays without forbidding the new one, and the count on each suggestion says how many expenses carry the tag.

**Cost:** Suggestions search across every expense on every pause in typing. A slow search answers after the reader has pressed Enter, so the new word is accepted by timing, not by choice.

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

**Rationale:** A closed tag set is drawn as checkboxes with a count on the button. Ticking beats typing when the words already exist, and managing the list is a separate task with its own dialog.

**Cost:** A closed set takes no new word where it is needed, so the reader leaves the form to make one. It fits a governed vocabulary, not a growing one.

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

**Cost:** No suggestions means no shared spelling: `travel` and `Travel` become two tags and nobody is told. The readout shows the split but never checks the spelling.

**Reference:** WordPress

### Address Field

**ID:** `litespec/frontend/patterns/forms-and-fields#address-field`

**Strength:** default

**Question:** Harbor and Pine answers for its expenses with a name, an address, and a telephone number on every spender message. The question is how the address field takes the street the law asks for and the postcode the mail needs.

**Default:** `postcode-lookup-that-fills-the-street`

**Compact:** Below the md breakpoint the postcode and number sit on one row, and the looked-up street follows under them.

**Rationale:** The default is the postcode lookup. Two short values the reader knows produce the street they would mistype. The runner-up asks country first; prefer it for companies outside the lookup coverage. One search that fills the address is the other alternative for a reader who knows the street but not its spelling. The postcode stays optional for a country that has none, and every lookup offers manual entry beside it. A single free-text address box stays out: one line never splits back into street, postcode, and city.

**Reviewed for:** Does every built location input match the wireframe its catalog entry chose? Open the built lookups, country shapes, and map pins beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** Postcode plus number are asked first, because the reader knows both by heart. The lookup then fills the street the reader would mistype. Two short fields are quicker to fill than one long one.

**Cost:** The lookup covers the Netherlands and nowhere else, so every other country falls back to manual entry. One field with two paths means two paths to test.

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

**Cost:** Every supported country needs its own shape, labels, and validation. Three countries fit this pattern; thirty need a library.

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

**Rationale:** A half-typed street resolves to a full address, for the reader who knows where they are but not how the database spells it. One field holds the whole search.

**Cost:** An address search reads a provider the back office pays per keystroke, and the first result may be chosen without reading. The manual fallback stays one tap away.

**Reference:** Google Places

### Email Body Editor

**ID:** `litespec/frontend/patterns/forms-and-fields#email-body-editor`

**Strength:** default

**Question:** Claimwick mails every spender when a payout is sent, and the message carries the spender name, the expense number, and the arrival date. The question is how the editor takes the company words once and fills the spender facts per expense.

**Default:** `subject-plus-body-with-variable-chips`

**Compact:** Below the md breakpoint the subject and body stack, and the variable chips and the insert field sit under the body.

**Rationale:** The default is subject plus body with variable chips. The sentence is written once and the facts are placed where they belong. The runner-up is the side-by-side spender preview; prefer it because a template is proven once it is seen filled. Plain text with a variable list is the other alternative where a mail client must read text. A template with no preview stays out: variables are spelled in one place and filled in another, and only a filled copy shows that they meet.

**Reviewed for:** Does every built message composer match the wireframe its catalog entry chose? Open the built subject-plus-body forms and variable chips beside the catalog entry recording the chosen option and its reason.

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

**Rationale:** Template variables are listed as chips under the body. A variable is a word with a value, not code: the body carries each one where it belongs, and the chips name what the editor accepts.

**Cost:** The body holds brackets as plain text, so the editor highlights each variable it recognizes and refuses a misspelled one. The chips and the insert select offer the same variables two ways.

**Reference:** Mailchimp

#### `side-by-side-preview-of-the-spender-message`: Side-by-Side Preview of the Spender Message

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

**Rationale:** A template is previewed beside the editor with a real record filled in. A variable is only proven by its value, and Ava's payout fills every variable this template holds.

**Cost:** Two columns collapse to one on a phone. The preview sits below the editor and the reader scrolls between them, so a long template means a long scroll between them.

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

**Rationale:** A template is written as plain text with double-bracket variables, each listed beside the editor with an example value. Nothing about the message breaks in a mail client that reads text.

**Cost:** Double brackets are learned, not guessed: one missing bracket fails silently or loudly depending on the parser. The editor highlights what it recognizes as the reader types.

**Reference:** GOV.UK
