# Form and Field Patterns

## Intent

This page shows how each field kind takes its value to create or change records such as expenses, trips, and payouts, and how a form reports a refused submit or unsaved work: money, record pickers, dates, text, single choices, switches, uploads, periods, slugs, capacity, colors, repeated groups, tags, addresses, and message templates. Each answer is drawn as options with a named default to build, and the product owner chooses among the options when the team builds or revises a form. One decided shape per field keeps entry fast and errors fixable, so staff type once and repair only what the message names.

## Concepts

### How to Read a Pattern

Each pattern answers one recurring question about a field kind, such as how a money field names its currency. It states the question, the default option a project builds, how the default behaves on a narrow screen, and why the default won. Each option then shows one wireframe drawing with its own rationale, cost, and optional reference. A project records its choice in its pattern catalog and builds the chosen option with its own components. The [UI patterns](../patterns.md) page states the catalog contract and the wireframe language.

### The Running Example

The drawings on this page use the Claimwick expense Ava files for the Rotterdam client visit: the EUR 45.00 hotel night, its booking fee, the Amsterdam sales cost centre, and the trips, payouts, and approval policies around them. On this page, `you` means the staff member entering values. The Standards section states the two rules every option keeps: every field carries a visible label naming its value, and a form keeps every entered value when the API refuses a submit.

## Standards

### Name Every Field with a Visible Label

**ID:** `litespec/frontend/patterns/forms-and-fields#name-every-field-with-a-visible-label`

**Statement:** Every field MUST carry a visible label that names its value.

**Rationale:** A placeholder disappears on the first keystroke, so a filled field with no label no longer says what it holds. Screen readers announce the label, and sighted staff returning to a filled form read it to check each value before saving. Without a visible label, both groups guess what each value means.

**Reviewed for:** Which built input lacks a shown label naming its value? Open every form on built screens and read each field's label.

### Keep Entered Values After a Refused Submit

**ID:** `litespec/frontend/patterns/forms-and-fields#keep-entered-values-after-a-refused-submit`

**Statement:** A form MUST keep every entered value when the API refuses a submit.

**Rationale:** Clearing the form after a refusal forces staff to retype every value to fix one field, which wastes time and adds new typos. Keeping all values lets the reader repair only the field the message names and submit again.

**Reviewed for:** Which built form drops typed values after a refused submit? Open the refused submits on built forms and check every field.

## Patterns

### Money Field

**ID:** `litespec/frontend/patterns/forms-and-fields#money-field`

**Question:** The back office holds amounts, fees, reversals, and payout figures, and staff type every one of them; the software chooses none. The field is where that rule meets a keyboard. The decision is how a money field takes its value: text input shape, currency naming, symbol prefix, error wording, and totals restated beside it.

**Default:** `text-input-currency-in-the-label-symbol-inside`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** The HMRC prefix with the currency in the label stays the pick. The prefix renders in light and dark themes. It names the currency to a screen reader while hiding the symbol from it. It refuses nothing you can type.

The payout total restatement is the runner-up. It belongs on every amount the approver sees. It stops the fee from surprising either side at payout. Never ship the stepper. You cannot reach a continuous amount by stepping.

**Reviewed for:** Which built amount input breaks from the wireframe its catalog entry chose? Open the built amount, currency, and total fields beside the catalog entry recording the chosen option and its reason.

#### `text-input-currency-in-the-label-symbol-inside`: Text Input, Currency in the Label, Symbol Inside

```wireframe
page
  breadcrumb "Home" "Expenses" "New expense"
  header
    heading "Lines"
    text "What each night costs, and what the payout adds on top."
  region "Hotel night"
    field "Nightly amount, in euros" number = "45.00"
    text muted "45,00 or 45.00. Up to two decimal places."
    field "Booking fee added, in euros" number = "5.00"
    text muted "Added at payout. The budget pays this one."
```

**Rationale:** The HMRC pattern places an `aria-hidden` prefix inside the field. It names the currency in the label. A screen reader hears the currency even though it does not read the symbol. This is not a `number` input. GDS rejected the `number` type outright.

**Cost:** You can type the symbol into the field, so validation has to accept it rather than refuse it. Allow at most two decimal places, per the HMRC pattern.

**Reference:** HMRC

#### `currency-as-a-separate-select`: Currency as a Separate Select

```wireframe
page
  breadcrumb "Home" "Expenses" "New expense"
  header
    heading "Lines"
    text "What each night costs."
  region "Hotel night"
    row
      field "Nightly amount" number = "45.00"
      field "Currency" select = "EUR"
    text muted "45,00 or 45.00. Up to two decimal places."
    row
      field "Booking fee added" number = "5.00"
      field "Currency" select = "EUR"
```

**Rationale:** The amount and the currency are two inputs, so a company claiming in several currencies changes one select rather than retyping an amount.

**Cost:** No design system in this set publishes a currency select beside an amount, and the company pays out in one currency. A select for a closed set of one is a control that cannot do anything.

#### `amount-with-a-unit-suffix-for-a-count`: Amount with a Unit Suffix, for a Count

```wireframe
page
  breadcrumb "Home" "Expenses" "New expense"
  header
    heading "Lines"
    text "What each night costs."
  region "Hotel night"
    row
      field "Nightly amount" number = "45.00"
      text "per night"
    row
      field "Booking fee added" number = "5.00"
      text "per expense"
```

**Rationale:** The GOV.UK suffix pattern uses a word rather than a symbol. Per night, per expense, and per kilometre read better than a symbol where the unit is not money.

**Cost:** The suffix takes horizontal room in a narrow field, and it cannot carry a currency. Money and a unit would need two suffixes, which no pattern here supports.

**Reference:** GOV.UK

#### `money-with-an-error-that-says-what-to-do`: Money with an Error That Says What to Do

```wireframe
page
  breadcrumb "Home" "Expenses" "New expense"
  header
    heading "Lines"
    text "What each night costs."
  region "Hotel night"
    field "Nightly amount, in euros" number = "45.000"
    message critical "Use at most two decimal places, like 45.00"
    field "Booking fee added, in euros" number = "45.00"
    text muted "Whole euros are fine here."
```

**Rationale:** The same field appears in its refused state. The message follows the GOV.UK template. It reads as a specific instruction rather than a rejection.

**Cost:** You cannot see the three states at once, so the text states which one the wireframe shows. Each wireframe here is one state.

**Reference:** GOV.UK

#### `amount-with-a-stepper`: Amount with a Stepper

```wireframe
page
  breadcrumb "Home" "Expenses" "New expense"
  header
    heading "Lines"
    text "What each night costs."
  region "Hotel night"
    row
      action quiet "One euro less"
      field "Nightly amount, in euros" number = "45.00"
      action quiet "One euro more"
    text muted "Steps of one euro."
```

**Rationale:** A plus and a minus sit on either side of the value. They cover the case where one amount is the common one. Small deviations are the exception.

**Cost:** NN/g and Carbon both warn against a stepper for an amount. You cannot reach a continuous value by stepping. You cannot express EUR 1.50. This is the wrong control for this field.

#### `money-as-a-read-only-fact-with-an-edit-command`: Money as a Read-Only Fact with an Edit Command

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Paid" positive
    action primary "Message spender"
  region "What was claimed"
    fact "Hotel night, 2 nights" "EUR 90.00"
    fact "Booking fee per expense" "EUR 5.00"
    fact "Paid to the spender" "EUR 125.00"
    action "Change a priced line"
```

**Rationale:** Once a line has been priced, the page shows it as a figure with one command to change it. You never retype the value, so a mistyped amount cannot become a second record.

**Cost:** Editing moves to a second surface for a field you touch often. A line already priced is a versioned fact rather than a field.

#### `price-with-the-customer-facing-total-restated`: Amount with the Payout Total Restated

```wireframe
page
  breadcrumb "Home" "Expenses" "New expense"
  header
    heading "Lines"
    text "What each night costs, and what the payout adds on top."
  region "Hotel night"
    field "Nightly amount, in euros" number = "45.00"
    text muted "45,00 or 45.00. Up to two decimal places."
    field "Booking fee added, in euros" number = "5.00"
    fact "Payout sends" "EUR 50.00"
```

**Rationale:** The money reads more clearly. You type what the night costs and read what the payout sends on the same screen before saving. The EUR 50.00 total figure stops the fee from surprising either side at payout.

**Cost:** The restated total must update as you type, which a static wireframe cannot show. It costs one line under every amount field, even where the fee is zero.

#### `gross-price-with-the-net-and-tax-restated`: Gross Amount with the Net and Tax Restated

```wireframe
page
  breadcrumb "Home" "Expenses" "New expense"
  header
    heading "Lines"
    text "What each night costs, and what the payout adds on top."
  region "Hotel night"
    field "Nightly amount, in euros" number = "45.00"
    fact "Of which VAT at 21 percent" "EUR 7.81"
    fact "Net counts against the budget" "EUR 37.19"
  region "Booking fee"
    field "Booking fee added per expense, in euros" number = "5.00"
    fact "Of which VAT at 21 percent" "EUR 0.87"
```

**Rationale:** Stripe Dashboard restates every gross amount as net plus tax under the field. You claim the gross amount. You read what counts against the budget.

**Cost:** The restated lines update as you type. The rates come from configuration keyed by date. An amount from January restates with January rates. This costs two lines per field even where you never look twice.

**Reference:** Stripe

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
    field "Nightly amount, in euros" number = "45.00"
    fact "Payout sends with the booking fee" "EUR 50.00"
```

**Rationale:** Square and Shopify POS put the common amounts one tap away. Most lines land on a round amount, and typing 45.00 ninety times a year is ninety chances to type 54.00. The field stays for the odd amount.

**Cost:** The presets are company data that must be learned or configured, and a stale set pushes you toward last year's amounts. Five chips plus a field is also wider than a field alone on a phone.

### Choosing a Record

**ID:** `litespec/frontend/patterns/forms-and-fields#choosing-a-record`

**Question:** You choose a reference to another record from a picker showing names. Nobody types an identifier. The failure to avoid is a select reading "Choose a cost centre" beside a field holding "CC-0C893968A2" as if it were a value to type.

**Default:** `searchable-picker-showing-names`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** The searchable picker over names stays the pick: nobody types an identifier, and search beats scrolling for every list longer than a screen. The picker with one distinguishing fact per row is the runner-up for records that share a name. Never ship a bare identifier field: a code such as CC-0C893968A2 is not a value you can type or check.

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

**Rationale:** It searches over names and shows nothing else. Time zone research from NN/g is the reason it searches. In that research 44 percent of first searches were by city. Search beats scrolling every time.

**Cost:** A picker hides how many records exist. If you know the cost centre is "Zaandam", you still have to search rather than scan. It needs an empty-result state that offers creating one.

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

**Rationale:** Each option carries the one fact that tells two similar records apart. Budget suits a cost centre and date suits a trip. State suits an approval policy. You tell two cost centres both called Rotterdam apart by what they hold.

**Cost:** The list grows wider, and the extra column is only useful for records that share a name. For a record set where names are already unique it is noise.

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

**Rationale:** It is a plain select listing names. It has fewer moving parts than a picker, and it works with the keyboard with no extra setup.

**Cost:** The GOV.UK guidance says to reduce the option count before reaching for a select. Research finds selects hard to use. The guidance forbids pre-selecting an option. A 40-cost-centre select is the case that guidance is about.

**Reference:** GOV.UK

#### `a-picker-whose-options-are-typed`: A Picker Whose Options Are Typed

```wireframe
page
  breadcrumb "Home" "Expenses" "New expense"
  header
    heading "New expense"
    text "Start with a name."
  region "Which budget pays"
    field "Cost centre" text = "rotter"
    list 3
      item: text "Rotterdam sales"
    text muted "Choose from the list. Nothing is picked until you do."
    field "Name" text
```

**Rationale:** You type part of a name and the list narrows, but you must choose from the list. APG warns that list autocomplete with automatic selection silently adopts the first suggestion, which stores the wrong record.

**Cost:** The field must not commit on blur or on Enter alone, or a mistyped prefix charges the wrong cost centre. That is an implementation rule the wireframe cannot show.

**Reference:** APG

#### `a-read-only-reference-showing-what-is-linked`: A Read-Only Reference Showing What Is Linked

```wireframe
page
  breadcrumb "Home" "Settings" "Approval policies"
  header
    heading "Standard"
    status "Active" positive
    action "Retire policy"
  region "What this policy belongs to"
    fact "Trip" "Rotterdam client visit"
    fact "Cost centre" "Amsterdam sales"
    fact "Applies to" "All trips"
    action "Change what is linked"
```

**Rationale:** On a record rather than a form, the page shows the relation as a fact. It carries a command to change it. You see what is linked without opening a picker.

**Cost:** Changing a relation takes two steps, and on a page with several relations it takes several such facts. It belongs on a record page, not on a create form.

#### `a-picker-that-also-creates`: A Picker That Also Creates

```wireframe
page
  breadcrumb "Home" "Expenses" "New expense"
  header
    heading "New expense"
    text "Start with a name."
  region "Which budget pays"
    field "Cost centre" text = "Zaandam"
    message critical "There is no cost centre called Zaandam."
    action primary "Create a cost centre called Zaandam"
    action "Choose from 8"
    field "Name" text
```

**Rationale:** The list ends with the command to make the missing record. If you need a new cost centre, you are never sent away. You make one and come back to the form.

**Cost:** The field opens a second surface, and a dialog must never open another dialog. Creating a cost centre has to be a page, which means losing the contents of the form.

#### `recent-records-above-the-search`: Recent Records Above the Search

```wireframe
page
  breadcrumb "Home" "Expenses" "New expense"
  header
    heading "New expense"
    text "Start with a name."
  region "Which budget pays"
    field "Cost centre" search
    text "Recent"
    list 2
      item: text "Amsterdam sales" · text muted "EUR 10,000"
    text "All cost centres"
    list 2
      item: text "Utrecht sales" · text muted "EUR 1,100"
```

**Rationale:** Linear opens every picker on the records you touched last. The cost centre for the next expense is almost always the cost centre of the last one. Recent first turns the common case into one tap. It leaves search for the rest.

**Cost:** Recent is per person, so two people see two lists and a screenshot never matches twice. It also needs an empty state for the first expense, where nothing is recent yet.

**Reference:** Linear

#### `results-grouped-by-state-with-counts`: Results Grouped by State with Counts

```wireframe
page
  breadcrumb "Home" "Settings" "Approval policies"
  header
    heading "Add an approval policy"
    text "An approval policy is what finance picks and what the payout follows."
  region "Copy from"
    field "Copy from an existing policy" search
    text "Active (2)"
    list 2
      item: text "Standard" · text muted "9,412 decided"
    text "Retired (1)"
    list 1
      item: text "Travel" · text muted "0 decided"
```

**Rationale:** GitHub command palette groups results under a heading with a count, so a list of thirty reads as three lists of ten. Grouping policies by state keeps a retired policy from being copied by mistake.

**Cost:** The groups are one more opinion about the records. A group the picker invents has to stay in sync with the states the records hold. This costs a heading row per group on a phone.

**Reference:** GitHub

### Date and Time Field

**ID:** `litespec/frontend/patterns/forms-and-fields#date-and-time-field`

**Question:** Staff enter dates and times for trips, payouts, and trip plans. The decision is which control takes the value: three text inputs, one field with a calendar, date and time side by side, relative entry, or presets. The native browser date input is refused: it draws the browser chrome instead of the application control, and GDS rejected it for implementation problems across browsers.

**Default:** `one-text-field-with-a-calendar-that-opens-beside-it`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** One typed field with a calendar beside it stays the pick. There is always a way to type. The calendar serves you when you would rather click.

The three GDS inputs are the runner-up where accessibility review demands them. They cost the slowest entry in the form. Never ship the native date input. It draws the browser chrome instead of the application control. It renders differently everywhere.

**Reviewed for:** Which built calendar input breaks from the wireframe its catalog entry chose? Open the built date, duration, and range fields beside the catalog entry recording the chosen option and its reason.

#### `three-text-inputs-with-a-format-hint`: Three Text Inputs with a Format Hint

```wireframe
page
  breadcrumb "Home" "Trips" "New trip"
  header
    heading "New trip"
    text "Start with a name."
  region "When it runs"
    row
      field "Day" number = "14"
      field "Month" number = "3"
      field "Year" number = "2026"
    text muted "For example, 14 3 2026"
    row
      field "Start hour" number = "08"
      field "Start minute" number = "00"
    text muted "In the Europe/Amsterdam time zone."
```

**Rationale:** The GOV.UK answer to a date uses three fields. It uses numeric `inputmode` and `autocomplete` tokens. The hint reads "For example, 31 3 1980". Every browser renders it the same.

**Cost:** Three fields hold one value, and if you enter 94 dates a year you type 282 numbers. This is the most accessible answer and the slowest one.

**Reference:** GOV.UK

#### `one-text-field-with-a-calendar-that-opens-beside-it`: One Text Field with a Calendar That Opens Beside It

```wireframe
page
  breadcrumb "Home" "Trips" "New trip"
  header
    heading "New trip"
    text "Start with a name."
  region "When it runs"
    field "Trip starts" date = "14/03/2026 08:00"
    text "March 2026"
    grid 31
      item: text "14"
    text muted "Times are in the Europe/Amsterdam time zone."
    field "Trip ends" text = "14/03/2026 12:00"
```

**Rationale:** You type into one field, with a calendar beside it if you would rather click. The APG `combobox` date picker pattern is exactly this. The Carbon rule is that there must always be a way to type.

**Cost:** A calendar is a grid of 42 cells to build, translate, and test in two languages. It is the most code in the form package for one field.

**Reference:** APG

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
      field "Start time" text = "08:00"
    row
      field "End date" date = "14/03/2026"
      field "End time" text = "12:00"
    message critical "The end must be after the start."
```

**Rationale:** Date and time sit side by side as two fields, with the time zone stated once above them rather than repeated under each. That means fewer repetitions of the same fact.

**Cost:** On a narrow screen two fields plus the row wraps to three lines. You have to read a time zone that is stated once before the second field. It does not sit next to that field.

#### `relative-dates-for-the-near-future-absolute-for-the-far`: Relative Dates for the Near Future, Absolute for the Far

```wireframe
page
  breadcrumb "Home" "Trips" "Rotterdam client visit"
  header
    heading "Add to the trip plan"
    text "Entries are sorted by their start time."
  region "The entry"
    field "What" text = "Meet the client"
    field "When" select = "Relative to departure"
    field "How long before" number = "4"
    text "Departure is at 08:00 on Sat 14 Mar 2026, so this is 04:00."
```

**Rationale:** A trip plan is written in relative language: "departure at 08:00", "check-in 4 hours before". One field takes a duration and the page calculates the clock time from the trip date.

**Cost:** The page derives an absolute time and shows it, so you never hold it in your head. A relative field cannot express the anchor itself: departure at 08:00 must come from the trip date.

#### `a-duration-field-for-a-length-of-time`: A Duration Field for a Length of Time

```wireframe
page
  breadcrumb "Home" "Settings" "Policies"
  header
    heading "Receipt grace period"
    text "How long a spender has to file a receipt before the expense is refused."
  region "The hold"
    row
      field "Wait for the receipt" number = "5"
      field "Unit" select = "days"
    text muted "Past 5 days, more receipts never get filed."
    field "Remind the spender before it ends" select = "1 day before"
```

**Rationale:** For a grace period, a timeout, or a hold, you know five days, not a date. One field takes the number and a unit sits beside it. The NN/g rule says the step and the unit stay explicit.

**Cost:** Two fields do the work where one number would do, and they cannot express "2h30". A single field parsing both needs error wording for every way a duration can be wrong.

#### `the-date-refused-with-the-date-that-was-wrong`: The Date Refused, with the Date That Was Wrong

```wireframe
page
  breadcrumb "Home" "Trips" "New trip"
  header
    heading "New trip"
    text "Start with a name."
  region "When it runs"
    field "Trip starts" date = "14/03/2026 26:00"
    message critical "Trip starts must be a time between 00:00 and 23:59. You entered 26:00."
    field "Trip ends" date = "14/03/2026 08:00"
    message critical "The trip must end after it starts. You entered 08:00."
```

**Rationale:** The same field appears refused, with the GOV.UK error wording. It names the field and says what to do. It says what was entered and what is wrong. You do not have to remember what you typed.

**Cost:** The message repeats the value, which makes it longer than the field. The message fits above the input, not inside it.

**Reference:** GOV.UK

#### `preset-chips-for-the-usual-dispatch-times`: Preset Chips for the Usual Departure Times

```wireframe
page
  breadcrumb "Home" "Trips" "New trip"
  header
    heading "New trip"
    text "Start with a name. All times are Europe/Amsterdam."
  region "When it runs"
    row
      action "Fri 08:00"
      action primary "Sat 08:00"
      action quiet "Custom"
    field "Trip starts" date = "14/03/2026 08:00"
    field "Trip ends" text = "14/03/2026 12:00"
```

**Rationale:** Calendly puts the usual lengths above a custom field because most meetings are thirty minutes. Departure times cluster the same way around 08:00. Two presets plus Custom covers the week without typing.

**Cost:** The presets are company data that goes stale when the season changes. A preset that names a date needs a new value every week. A stale chip offers last month mornings.

**Reference:** Calendly

#### `start-and-end-as-one-range-with-a-length-readout`: Start and End as One Range with a Length Readout

```wireframe
page
  breadcrumb "Home" "Trips" "New trip"
  header
    heading "New trip"
    text "Start with a name. All times are Europe/Amsterdam."
  region "When it runs"
    row
      field "Trip start" date = "14/03/2026 22:00"
      field "Trip end" date = "15/03/2026 06:00"
    fact "Length of the trip" "8 hours"
```

**Rationale:** Airbnb and Linear ask for a range as one control with the length read back. A start without an end is not a value the page can use. The readout catches the overnight mistake before the submit does.

**Cost:** Two values in one field need one error message that names both, and a screen reader meets a composite where it expected a field. Wire the labels with care.

**Reference:** Airbnb

### Text Field and Its States

**ID:** `litespec/frontend/patterns/forms-and-fields#text-field-and-its-states`

**Question:** Every text field draws several states: empty, filled, focused, refused, disabled, and read-only. The label stays visible above the input in all of them. A placeholder is never the label, because it vanishes on the first keystroke.

**Default:** `label-above-help-under-six-states-on-one-page`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** The six-state reference page stays the pick, because it is the only option that pins every state one field must draw. Inline validation that confirms a good value is the runner-up for create forms, where it saves a refused submit. Never ship the placeholder as the label: it vanishes on first keystroke, and its color misses contrast.

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
    field "With help" text = "Rotterdam client visit"
    text muted "Spenders see this on the expense and in the payout."
    field "Refused" text = "Spring offsite"
    message critical "A trip is already scheduled as Spring offsite."
    field "Read only" text = "EXP-1042-01"
    text muted "Issued by the system when the line was made."
```

**Rationale:** The component reference page for the field: default, filled, focused, error, disabled, and readonly. Showing them together is how you check one control rather than six screenshots.

**Cost:** Six instances of one field is a style guide, not a screen. It never appears in the product. It exists so the states cannot drift apart.

#### `label-above-placeholder-as-a-hint`: Label Above, Placeholder as a Hint

```wireframe
page
  breadcrumb "Home" "Settings" "Approval policies"
  header
    heading "Add an approval policy"
  region "The policy"
    field "Name" text = "Travel"
    text muted "Spenders see this on the expense and in the payout."
    field "What it includes" textarea
    field "Internal note" textarea
    text muted "Not shown to spenders or to approvers."
```

**Rationale:** The label names the field and the placeholder shows an example. Both are present. That is the GOV.UK recommendation once a hint is genuinely needed.

**Cost:** A form field carries two sets of words, so a form with five fields carries ten lines before the first value. The hint has to earn its place every time.

**Reference:** GOV.UK

#### `placeholder-as-the-label`: Placeholder as the Label

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Every paid expense, newest first."
  toolbar
  table 3 "Expense" "Spender" "Paid"
    item: text "EXP-1042" · text "Ava" · text "EUR 125.00"
```

**Rationale:** The search field has no label. The only description of what the search covers is a placeholder reading "Expense number, name or email".

**Cost:** It fails the GOV.UK guidance on three counts at once. It vanishes on first keystroke. Not every screen reader reads it. Its default color often misses `1.4.3` contrast.

**Reference:** GOV.UK

#### `character-count-under-a-long-answer-field`: Character Count Under a Long-Answer Field

```wireframe
page
  breadcrumb "Home" "Settings" "Policies"
  header
    heading "Approval policy"
    text "Spenders read this before they file."
    action primary "Save"
  region "The policy"
    field "Policy" textarea = "Expenses above EUR 150 need a receipt."
    text muted "148 characters of 1,500"
```

**Rationale:** Where a field has a hard limit, the remaining count sits under it. GOV.UK says count characters only where its own research shows it helps.

**Cost:** GOV.UK permits live counting as a narrow exception to its no-live-validation rule, because it judges a field before it is finished. On a policy text the count is worth it. On a name it is not.

**Reference:** GOV.UK

#### `a-textarea-with-the-customer-s-words-preserved`: A Textarea with the Spender's Words Preserved

```wireframe
page
  breadcrumb "Home" "Settings" "Policies"
  header
    heading "Approval policy"
    text "Spenders read this before they file."
    action primary "Save"
  region "Preview for spenders"
    text "Expenses above EUR 150 need a receipt before approval."
    text "Without one, finance refuses the expense instead."
    text muted "This is how it appears to a spender, with the line breaks kept."
```

**Rationale:** The page shows long answers with the line breaks you entered. A policy pasted from a document keeps its paragraphs instead of reflowing into one block.

**Cost:** Uncontrolled line breaks in stored text are a rendering hazard on a spender page. The width there differs from the width of the editor. Normalize it somewhere.

#### `a-field-that-grows-as-the-answer-does`: A Field That Grows as the Answer Does

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "Record a dispute"
    text "What happened on the trip, in your words."
  region "The dispute"
    field "What happened" textarea
    field "Where it happened" select = "Rotterdam office"
    field "Who decided" text
  footer
    action "Cancel"
    action primary "Record it"
```

**Rationale:** The `textarea` expands with its content up to a limit, so a dispute note is not a 74-pixel window with a scrollbar inside it.

**Cost:** A growing field pushes everything below it down as you type, which moves the button you are about to press. Most implementations stop growing at about six lines.

#### `inline-confirmation-of-a-good-value`: Inline Confirmation of a Good Value

```wireframe
page
  breadcrumb "Home" "Settings" "Approval policies"
  header
    heading "Add an approval policy"
    text "Checked as you type, so a good value is confirmed before the submit."
  region "The policy"
    field "Name" text = "Standard"
    message positive "Standard is free to use."
    field "What it includes" textarea
  footer
    action "Cancel"
    action primary "Add the policy"
```

**Rationale:** Stripe Checkout and Vercel confirm a valid value where you typed it. A tick and a quiet line mark it, instead of waiting for the submit. Confirming Standard early saves you a round trip for the name you wanted.

**Cost:** GOV.UK permits live validation only as a narrow exception, because it judges a field before it is finished. The check must wait for a pause in typing, or it nags.

**Reference:** Stripe

#### `floating-labels-that-shrink-when-filled`: Floating Labels That Shrink When Filled

```wireframe
page
  breadcrumb "Home" "Settings" "Approval policies"
  header
    heading "Add an approval policy"
    text "Two fields drawn the Material way, for comparison."
  region "The policy"
    field "Name" text
    text muted "Empty: the label sits where the value will go."
    field "Name" text = "Standard"
    text muted "Filled: the label shrinks above the value and stays visible."
  footer
    action "Cancel"
    action primary "Add the policy"
```

**Rationale:** Material Design answers the placeholder problem by floating the label above the value once the field fills, so the label never vanishes. It keeps the compact look the placeholder promised without breaking it.

**Cost:** The empty state still reads as a placeholder, with its small text and low contrast. A floated label at 10.5 px is smaller than any label the back office otherwise draws. The GOV.UK guidance keeps the label above and full size for that reason.

**Reference:** Material Design

### Choosing One of a Few Options

**ID:** `litespec/frontend/patterns/forms-and-fields#choosing-one-of-a-few-options`

**Question:** You choose from a closed set, such as a naming rule or a plan. The GOV.UK rules say a select is a last resort and radios beat a select. Nothing is pre-selected in a question, though settings allow it. The decision is which control carries the choice: radio list, segmented control, cards, select, checkbox list, listbox, or plain buttons.

**Default:** `a-radio-list-with-a-description-per-option`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** The radio list with a description per option stays the pick. Each choice carries the sentence that lets you choose between names that sound alike. The segmented control is the runner-up for two or three short values with no descriptions.

Never ship the native `multi-select`. GOV.UK rules it out for its usability and assistive record. A checkbox list replaces it.

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

**Rationale:** Each option carries a sentence saying what it means for the spender. That sentence is the only way you can choose between "The spender name" and "Every line named".

**Cost:** Three options at 22 pixels each is a tall block, and it does not scale to twelve options, which a permission picker does need.

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

**Rationale:** The choice is between two or three values that are all visible at once. A segmented control shows every option. It hides none of them behind a menu.

**Cost:** Labels have to fit, so "A blank expense" and "A template" are both shortened. The control grows with its longest label.

#### `plan-cards-for-a-priced-choice`: Plan Cards for a Priced Choice

```wireframe
page
  breadcrumb "Home" "Settings" "Plan"
  header
    heading "What the plan costs"
    text "The company is on Business. Changing plan takes effect from the next month."
  region "The plans"
    card "Starter"
      figure "EUR 0 a month"
      text "Up to 100 expenses a month. No approval policies of your own."
    card "Business"
      figure "EUR 49 a month"
      text "Everything, including your own approval policies."
    card "Enterprise"
      figure "EUR 249 a month"
      text "Ten cost centres at once and four approvers each."
  footer
    action "Cancel"
    action primary "Change to this plan"
```

**Rationale:** For a choice between priced options, each card names the option, its price, and its period. The one the company holds says so in words. It does not rely only on a ring.

**Cost:** Sections are the only boxes and never nest, so three plan cards inside a section break that rule three times.

#### `cards-with-a-radio-inside-each`: Cards with a Radio Inside Each

```wireframe
page
  breadcrumb "Home" "Settings" "Plan"
  header
    heading "What the plan costs"
    text "The company is on Business."
  region "The plans"
    card "Starter"
      field "Starter" radio
      figure "EUR 0 a month"
      text "Up to 100 expenses a month."
    card "Business"
      field "Business" radio
      figure "EUR 49 a month"
      text "Everything, including your own approval policies."
    card "Enterprise"
      field "Enterprise" radio
      figure "EUR 249 a month"
      text "Ten cost centres at once and four approvers each."
  footer
    action "Cancel"
    action primary "Change plan"
```

**Rationale:** The plan cards carry the choice in a real radio. The tab order and the arrow keys work as a form control. They do not work as a set of buttons.

**Cost:** One card holds two nested controls. The whole card has to forward the click to the radio, and the radio still has to be reachable by Tab.

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

**Rationale:** GOV.UK allows a pre-selection in settings and nowhere else. Where the value has no default and the options are many, a select with a real empty first option is the least-bad shape.

**Cost:** The empty option is a placeholder, and placeholders stay out of components. A select whose first choice is a blank that must be replaced is two states, not one.

**Reference:** GOV.UK

#### `a-multi-select-as-a-list-of-checkboxes`: A Multi-Select as a List of Checkboxes

```wireframe
page
  breadcrumb "Home" "Settings" "Team"
  header
    heading "New role"
    text "What this role may do."
    action primary "Create the role"
  region "The role"
    field "Name" text = "Auditor"
    text "Expenses"
    field "Look at any expense" checkbox
    field "Reverse a payout" checkbox
    field "Approve an expense" checkbox
    field "Record a refusal" checkbox
    text muted "7 actions across 2 areas."
```

**Rationale:** GOV.UK states it directly: do not use `select multiple`, because of its history of poor usability and assistive support. Use checkboxes in a list, or a dedicated picker where the options are many.

**Cost:** Twenty checkboxes is a wall. Above about eight, the picker needs a search and a summary of what is chosen, which is a component rather than a field.

**Reference:** GOV.UK

#### `a-listbox-with-a-checkmark-and-descriptions`: A Listbox with a Checkmark and Descriptions

```wireframe
page
  breadcrumb "Home" "Expenses" "Receipts"
  header
    heading "Receipt naming"
    text "Finance reads the name on each file."
  region "Naming rule"
    field "Who is named on the file" select = "No name"
    text muted "Applies to receipts not yet filed."
  popover
    list 3
      item: text "No name" · text muted "A receipt is filed without a name on it."
```

**Rationale:** Apple HIG and Radix UI draw a choice with descriptions as a `listbox`. One button shows the current value. A checkmark marks it in the list. It includes full keyboard support. It carries the same sentences as the radio list in the height of one field.

**Cost:** The options hide behind a button, so you compare them only by opening the list. A radio list shows all three at once. This one shows one and promises two more.

**Reference:** Apple HIG

#### `a-binary-question-as-two-plain-buttons`: A Binary Question as Two Plain Buttons

```wireframe
page
  breadcrumb "Home" "Settings" "Filing"
  header
    heading "Filing"
    text "What spenders meet before they file."
  region "Spender accounts"
    row
      card "No, sign-in stays optional"
      card "Yes, account required"
    text muted "Filing without an account is the default."
  footer
    action primary "Save"
```

**Rationale:** Typeform asks a yes-or-no question as two large buttons with the consequence under each, because a binary choice deserves no menu and no dropdown. Both answers stay visible with their cost attached.

**Cost:** Two cards for one bit is the heaviest binary control on this page, and it answers only questions with two genuine answers. A third option or a conditional follow-up breaks the shape.

**Reference:** Typeform

### Switch or a Checkbox

**ID:** `litespec/frontend/patterns/forms-and-fields#switch-or-a-checkbox`

**Question:** The payout editor holds a toggle for the cost-centre line, and settings pages hold many more switches and checkboxes. This pattern decides which control each setting deserves: switch or checkbox. It also decides what the label must say, since the label carries the difference between the two.

**Default:** `a-switch-with-the-label-and-its-consequence`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** The switch with its consequence stated stays the pick. The label says what is on. The line under it says what turning it off does.

The instant-apply switch with an undo toast is the runner-up for low-risk settings you flip often. Never ship a switch whose label names the control instead of the outcome. You need no documentation to learn what on means.

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

**Cost:** A switch has no pressed state on first paint, so you cannot tell what is currently on without looking twice. It carries no visible text.

#### `a-checkbox-where-the-off-state-is-the-default`: A Checkbox Where the off State Is the Default

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

**Rationale:** Use a checkbox rather than a switch, because the default is off and the action is to tick something. The GOV.UK wording phrases it as what ticking does. It does not phrase it as what the box is.

**Cost:** A checkbox and a switch look nearly identical here, and readers conflate them. The phrasing of the label has to carry the difference.

**Reference:** GOV.UK

#### `a-checkbox-with-the-consequence-under-it-no-state-carried`: A Checkbox with the Consequence Under It, No State Carried

```wireframe
page
  breadcrumb "Home" "Settings" "Payouts"
  header
    heading "Payouts"
    text "How money reaches the spenders."
    action primary "Save"
  region "The connected account"
    fact "The payout account" "NL91 ABNA 0417 1643"
    divider
    field "Pay twice a month" checkbox
    text "On the 4th and the 19th, with the money from three days before."
    field "Pay once a month" checkbox
    text "On the 4th, with everything from the previous month."
```

**Rationale:** Every setting is a checkbox with a consequence line. One control for the whole page, so nothing about the shape depends on whether the value happens to be on.

**Cost:** You cannot see at a glance which are on without reading the boxes. On a settings page with eight such settings that matters.

#### `two-options-where-the-difference-is-the-money`: Two Options Where the Difference Is the Money

```wireframe
page
  breadcrumb "Home" "Payouts" "Schedule"
  header
    heading "How often the payouts run"
    text "The payouts run from NL91 ABNA 0417 1643."
  region "Frequency"
    card "Twice a month"
      field "Twice a month" radio
      figure "EUR 28,510 on average"
      text "On the 4th and the 19th, three days behind."
    card "Once a month"
      field "Once a month" radio
      figure "EUR 57,020 on average"
      text "On the 4th, everything from the previous month."
  footer
    action "Cancel"
    action primary "Change to monthly"
```

**Rationale:** Payout frequency is a choice between two money outcomes. It gets radio cards with the amount and the date on each. No switch hides the difference.

**Cost:** Cards for a two-way choice are heavy, and the amounts are derived figures that have to be computed per company to stay correct.

#### `a-switch-that-is-refused-with-the-reason`: A Switch That Is Refused, with the Reason

```wireframe
page
  breadcrumb "Home" "Settings" "Payouts"
  header
    heading "Payouts"
    text "How money reaches the spenders."
  region "The connected account"
    action disabled "Switch to a different bank account"
    message critical "The Owner role can do this. Your role is Approver."
  region "Where it pays"
    fact "The payout account" "NL91 ABNA 0417 1643"
```

**Rationale:** The setting is one the role cannot change: switched off and refused, with the reason in visible text. This is the explained-action pattern applied to a control rather than a button.

**Cost:** A disabled switch with no value is a dead pixel if you do not notice the reason. The reason has to sit directly under it, not at the foot of the section.

#### `a-switch-with-the-number-it-would-change`: A Switch with the Number It Would Change

```wireframe
page
  breadcrumb "Home" "Settings" "Payouts"
  header
    heading "Booking fee"
    text "What the payout adds on top of the lines. The budget pays all of it."
  region "The fee"
    field "Per expense, in euros" number = "5.00"
    divider
    field "Waive the fee for the Travel policy" toggle
    text "Travel policy only. Last month that would have been EUR 310.00 across 62 expenses."
    field "Show the fee as part of the amount" toggle
    text "Claim EUR 50.00 instead of EUR 45.00."
```

**Rationale:** Where flipping the setting changes an amount, the amount is on the same line. You read the consequence before flipping rather than after.

**Cost:** A derived figure beside a control has to be current, which means a read that the switch itself depends on. It also puts a number on a page that was otherwise free of them.

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

**Rationale:** Linear and Google Material apply a low-risk switch at once. They offer Undo in a toast, so there is no Save to forget and no dialog to dismiss. The toast names what changed, which a silent apply never does.

**Cost:** Every flip is an audited command the moment it happens, with no review step before it. It fits display settings. It does not fit money, access, or anything a toast cannot undo.

**Reference:** Linear

#### `a-three-way-choice-as-a-segmented-control`: A Three-Way Choice as a Segmented Control

```wireframe
page
  breadcrumb "Home" "Settings" "Email"
  header
    heading "Email"
    text "What Harbor and Pine sends, and what it promises."
  region "Reversal mail"
    row
      action primary "Every reversal"
      action "Only bank refusals"
      action "Never"
    field "Copy to" text = "Priya Shah"
    text muted "A person on the team. Not the spender."
  footer
    action primary "Save"
```

**Rationale:** Slack draws a three-way notification choice as a segmented control, because the values are short, ordered, and all worth seeing at once. No menu hides the middle option you want.

**Cost:** Three labels have to fit the control, so the wording shortens until it almost stops meaning anything. A fourth option or a per-option description breaks the shape back into radios.

**Reference:** Slack

### Upload Field

**ID:** `litespec/frontend/patterns/forms-and-fields#upload-field`

**Question:** Claimwick records take files: a receipt photo or a staff spreadsheet. A receipt needs a stated type and size, and a staff import needs stated columns and row-level refusals. The decision is how the upload field states its contract and reports progress and refusals.

**Default:** `a-drop-zone-that-states-what-it-accepts`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the drop zone that states its types and size limit. Nobody learns the limit by being refused. The per-file progress tray is the runner-up for imports of several files, where one bar per file beats one bar for all. Never ship a CSV upload with no stated columns: a file refused for a format nobody stated is a refusal the application caused.

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

**Cost:** A drop zone invites a click anywhere, and if you drop a file on the page outside the zone you get nothing at all. No affordance says where the zone is.

#### `the-file-already-chosen-with-what-it-will-do`: The File Already Chosen, with What It Will Do

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "Receipts"
    text "The receipt photos finance reads."
    action primary "Save"
  region "The receipt"
    fact "Receipt" "receipt-hotel.jpg"
    text "512 by 512. 84 KB. Chosen now."
    action "Replace"
    action quiet "Remove the receipt"
    field "Receipt description" text = "Hotel bill, 2 nights"
    message info "Saving replaces the receipt finance reads now. It reaches the approver within a minute."
```

**Rationale:** After the choice, the row carries the file, its size, and the consequence of saving. The old receipt is replaced and the approver sees the new one within a minute.

**Cost:** A file that has been uploaded but not saved is a resource that exists and is not referenced. It needs cleanup, or the storage fills with orphans.

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

**Rationale:** For an import, the field states what it needs before anything is uploaded: the four column names and a link to a template. The upload cannot be wrong about a format nobody told it.

**Cost:** Stating four columns above the zone is a paragraph of prose, and prose belongs on the page beside the field, not inside a component.

#### `the-upload-refused-naming-the-row-that-failed`: The Upload Refused, Naming the Row That Failed

```wireframe
page
  breadcrumb "Home" "Settings" "Data imports"
  header
    heading "Import staff"
  region "The file"
    message critical "1,988 of 2,000 rows can be imported. Row 412 has no email address."
    table 2 "Row" "What is wrong" "What was in the file"
      item: text "412" · text "No email address" · text muted "Ada Kowalski"
  footer
    action "Choose a different file"
    action primary "Import the 1,988 good rows"
```

**Rationale:** The file is refused for a reason you can act on: row 412 has no email. A refusal that names a row is worth more than one that says "invalid file".

**Cost:** Naming one row out of 2,000 needs a read of the whole file before answering. The upload is not instant, and you wait without knowing why.

#### `an-image-field-with-the-dimensions-stated`: An Image Field with the Dimensions Stated

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "Receipts"
  region "The receipt"
    field "Drop the second page here or choose a file" file
    text "At least 1200 pixels wide. At most 5 MB. JPEG, PNG or PDF."
    message caution "That photo is 800 by 600. Small text may not read."
    field "Keep it anyway" checkbox
    text muted "Stored as is. Retake it if finance asks."
  footer
    action "Cancel"
    action primary "Save receipt"
```

**Rationale:** For a photo that must stay readable, the resolution is part of the requirement, not a nicety. The field says 1200 pixels wide reads best and warns below it.

**Cost:** The stored image box follows the page layout, not the resolution of the image file. A field that warns on resolution re-decides that per field.

#### `an-import-that-reports-what-it-made`: An Import That Reports What It Made

```wireframe
page
  breadcrumb "Home" "Settings" "Data imports" "IMP-4F82C1A9"
  header
    heading "1,988 staff imported"
    text "Finished 8 Oct 2026, 14:22. Nothing has been charged to anybody."
  region "The import"
    fact "Rows in the file" "2,000"
    fact "Staff created" "1,988"
    fact "Welcome mails queued" "2,361"
    fact "Rows refused" "12"
    fact "Amount charged to anybody" "EUR 0.00"
  footer
    action "Undo this import"
    action primary "Open the list"
```

**Rationale:** After an import, the page says what happened: how many rows became what, how many were refused, and why. You can undo it while nothing has been paid out.

**Cost:** A real import is a command whose effect spans many records, which the architecture reserves for a named exception. The report has to be a real read, not a summary of the session.

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

**Rationale:** Dropbox and Google Drive report each file on its own row with its own bar and its own cancel, because two files never finish together. One bar for all would lie about both.

**Cost:** A row per file is a list the page must manage: cancel one, remove one, and retry one. For a single receipt it is heavier than the one zone it replaces.

**Reference:** Dropbox

#### `drop-anywhere-on-the-section`: Drop Anywhere on the Section

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042"
  header
    heading "Receipts"
    text "The receipt photos finance reads."
  region "Front page"
    field "Drop the front page anywhere in this box" file
    text "A photo of at most 5 MB. JPEG, PNG or PDF."
    field "Receipt description" text = "Hotel bill, 2 nights"
  region "The receipt"
    field "Drop the second page here or choose a file" file
    text "The back of the receipt, if the total is there."
```

**Rationale:** GitHub and Notion take a drop anywhere on the editing surface. The target highlights while the file hovers, so you never aim at a small box.

**Cost:** A page that accepts drops everywhere must also refuse them precisely: a CSV over the receipt box needs a refusal, not a silent nothing. Every surface needs its own accepted list.

**Reference:** GitHub

### Reporting a Refused Submit

**ID:** `litespec/frontend/patterns/forms-and-fields#reporting-a-refused-submit`

**Question:** A submit refused by the API must report what is wrong and where. GOV.UK asks for a summary with focus and links, plus a message on each field, meeting WCAG 3.3.1 and 3.3.3. The decision is how the form carries that report.

**Default:** `a-summary-above-the-form-plus-a-message-on-each-field`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the GOV.UK summary with focus, plus per-field messages. It meets WCAG 3.3.1 and 3.3.3, and the links carry you to each field. Inline validation on blur is the runner-up as a complement, never a replacement. It confirms good values early but cannot orient you facing three errors. Never ship the dialog summary: it hides the form you must fix.

**Reviewed for:** Which built submit-error report breaks from the wireframe its catalog entry chose? Open the built summaries and per-field messages beside the catalog entry recording the chosen option and its reason.

#### `a-summary-above-the-form-plus-a-message-on-each-field`: A Summary Above the Form, Plus a Message on Each Field

```wireframe
page
  breadcrumb "Home" "Trips" "New trip"
  header
    heading "New trip"
  region "There is a problem"
    link "Enter the name of the trip"
    link "Trip starts must be before the trip ends"
  region "The trip"
    field "Name" text
    message critical "Enter the name of the trip"
    field "Cost centre" select = "Amsterdam sales"
    field "Trip starts" text = "08:00"
    message critical "Trip starts must be before the trip ends. It is set to 08:00."
  footer
    action "Cancel"
    action primary "Create trip"
```

**Rationale:** This is the GOV.UK pattern in full. The summary comes first and receives focus, with each entry linking to its field. The field repeats the same wording so it reads the same out of context.

**Cost:** The wording exists in two places per error, so generate it once and use it twice. Hand-written copies drift, and the drift is invisible.

**Reference:** GOV.UK

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
    message critical "Trip starts must be before the trip ends"
  footer
    action "Cancel"
    action primary "Create trip"
```

**Rationale:** Each field carries its own message and nothing else. You see the error where you have to fix it.

**Cost:** On a long form the first error may be below the fold. You submit, see nothing change, and conclude the button is broken. G139 exists to jump to the errors.

#### `a-summary-in-a-dialog-above-the-form`: A Summary in a Dialog Above the Form

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
  dialog
    heading "There is a problem"
    text "3 things need fixing before this trip can be created."
    text "Enter the name of the trip. Choose which budget pays."
    action primary "Fix these"
```

**Rationale:** The summary arrives as a dialog over the form, which cannot be scrolled past and cannot be missed, so you cannot submit again unaware.

**Cost:** A dialog over a form breaks focus rules: a dialog never opens another layer, and a form is not a dialog. It also hides the form you have to fix.

#### `a-summary-as-a-banner-above-the-page-not-the-form`: A Summary as a Banner Above the Page, Not the Form

```wireframe
page
  breadcrumb "Home" "Trips" "New trip"
  region "There is a problem"
    link "Enter the name of the trip"
    link "Trip starts must be before the trip ends"
  header
    heading "New trip"
    text "Start with a name."
  region "The trip"
    field "Name" text
    message critical "Enter the name of the trip"
    field "Trip starts" text = "08:00"
    message critical "Trip starts must be before the trip ends"
  footer
    action primary "Create trip"
```

**Rationale:** The summary sits at the top of the page, above the heading, as GOV.UK specifies. It sits below the back link and breadcrumb and takes focus.

**Cost:** Above the heading it is easy to miss on a long page, and it pushes the heading below the fold on a phone. GOV.UK accepts that because the page title also gets an error prefix for screen readers.

**Reference:** GOV.UK

#### `the-refused-field-only-with-no-summary-at-all`: The Refused Field Only, with No Summary at All

```wireframe
page
  breadcrumb "Home" "Trips" "New trip"
  header
    heading "New trip"
    text "Start with a name."
  region "The trip"
    field "Name" text = "Spring offsite"
    field "Trip starts" text = "32/13/2026"
    message critical "Trip starts must be a time between 00:00 and 23:59."
    field "Trip ends" text = "14/03/2026 12:00"
  footer
    action "Cancel"
    action primary "Create trip"
```

**Rationale:** The one field the command refused carries a message and everything else is untouched. It is the smallest response to a single refusal, which is what most refusals are.

**Cost:** This common behavior meets WCAG 3.3.1 but not 3.3.3, because "Invalid date" names no field and says nothing about what to do.

#### `the-refused-command-with-what-to-do-instead`: The Refused Command, with What to Do Instead

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "Payout"
  header
    heading "Run the payout"
    text "The payout runs Sat 14 Mar 2026 at 08:00."
  region "What would be paid"
    message critical "That is more than the budget holds. Lower an amount first."
    table 3 "Cost centre" "Budget" "Claimed"
      item: text "Amsterdam sales" · text "EUR 10,000" · text "EUR 10,310"
  footer
    action "Change an amount"
    action disabled "Run the payout"
```

**Rationale:** When a refusal is about the command rather than a field, it goes above the form as an alert. The alert carries the code sentence. Where the application can help, it states why it was refused and what resolves it.

**Cost:** The refusal is not attached to anything you can click, so it explains rather than navigates. It suits a business rule, not a typo.

#### `validation-on-leaving-each-field`: Validation on Leaving Each Field

```wireframe
page
  breadcrumb "Home" "Trips" "New trip"
  header
    heading "New trip"
    text "Each field is checked when it is left."
  region "The trip"
    field "Name" text = "Spring offsite"
    message positive "Spring offsite is free."
    field "Cost centre" select = "Amsterdam sales"
    field "Trip starts" text = "08:00"
    message critical "Trip starts must be before the trip ends. It is set to 08:00."
  footer
    action "Cancel"
    action primary "Create trip"
```

**Rationale:** Shopify Polaris and Stripe check a field on blur and mark it good or wrong where it sits. You fix one field and move on. By the submit nothing is left to refuse.

**Cost:** It is a complement to the summary, never a replacement: with three errors and no summary you still cannot see them all at once. A check on blur fires while the form is half-filled.

**Reference:** Shopify Polaris

#### `one-question-per-screen-with-a-progress-marker`: One Question per Screen with a Progress Marker

```wireframe
page
  breadcrumb "Home" "Trips" "New trip"
  header
    heading "New trip"
    text "Question 2 of 4."
  region "Budget"
    text "Halfway through the four questions."
    field "Which budget pays" select = "Choose a cost centre"
    message critical "Choose which budget pays"
  footer
    action "Back"
    action primary "Continue"
```

**Rationale:** TurboTax and Typeform ask one question per screen with nowhere else for an error to hide. The refusal sits under the only field on the page. A guided flow cannot submit with a field unseen.

**Cost:** Four screens for four fields is the slowest form among these options, and if you know all four answers you resent the pacing. It fits a first trip. It does not fit the fortieth.

**Reference:** Typeform

### Work That Is Not Saved

**ID:** `litespec/frontend/patterns/forms-and-fields#work-that-is-not-saved`

**Question:** A form must show work that is not saved. Nothing may read as saved before it is, and leaving must not drop typed work without a warning. The decision is how the form marks unsaved work and guards the way out.

**Default:** `a-visible-unsaved-marker-beside-the-save`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the visible unsaved marker beside Save. You never have to remember whether anything was typed. The sticky save bar is the runner-up for long settings pages where the form actions scroll out of view. Never ship silent autosave on money or access. Each save would be an audited command. An audit entry per keystroke batch is not a trail you can read.

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
    message info "Two changes not saved. Spenders still see the old ones."
  footer
    action quiet "Discard both"
    action primary "Save 2 changes"
```

**Rationale:** The moment a field changes, the Save button carries the word Save changes and a note says what changed. You never have to remember whether you typed anything.

**Cost:** A note naming one changed field states a derived fact. It has to come from the form rather than the route. A Save that changes its own label moves under the pointer.

#### `leave-with-a-question-when-there-is-something-to-lose`: Leave-with-a-Question When There Is Something to Lose

```wireframe
page
  breadcrumb "Home" "Settings" "Remittance"
  header
    heading "Remittance"
  region "Appearance"
    field "Accent color" text = "#B45309"
    text muted "Contrast with white is 5.1 to 1."
  footer
    action "Cancel"
    action primary "Save"
  dialog
    heading "Leave without saving"
    text "Two changes will be lost. The accent color and the typeface."
    action "Stay here"
    action primary "Leave and lose both"
```

**Rationale:** The Cloudcape rule confirms only when real data is at risk. It stays consistent about it, so the same dialog appears for a Cancel, a breadcrumb, and a move away.

**Cost:** Every page with a form needs the same guard. The browser prompt cannot be worded, so the custom one and the native one both appear on a browser Back.

**Reference:** Cloudcape

#### `autosave-with-a-saved-marker`: Autosave, with a Saved Marker

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "Disputes"
  header
    heading "Record a dispute"
    text "Saved as it is typed. Nothing is lost when this browser closes."
  region "The dispute"
    field "What happened" textarea = "At 08:41 Ben refused the hotel breakfast as personal."
    text muted "Saved 4 seconds ago"
```

**Rationale:** The form saves itself every few seconds and a quiet marker says "Saved 4 seconds ago". Nothing is ever lost and no dialog is ever needed.

**Cost:** Autosave makes each save a command against money and access, which means an audit entry per keystroke batch. A past decision must stay reconstructable, and a trail of keystroke batches is not one you can read.

#### `a-draft-saved-and-left-on-the-server`: A Draft, Saved and Left on the Server

```wireframe
page
  breadcrumb "Home" "Settings" "Policies"
  header
    heading "Approval policy"
    text "A draft from 6 Oct is waiting. It has not been published."
  region "The policy"
    message info "A draft from 6 Oct 2026, 17:40 waits. The published policy has not changed since 2 Sep."
    field "Published policy" textarea = "Expenses above EUR 150 need a receipt."
  footer
    action "Carry on"
    action primary "Publish"
```

**Rationale:** The state of the form is a draft the server holds. Leaving the page leaves the draft, and returning to it offers "Carry on". GOV.UK asks for this where a long form may be completed over more than one sitting.

**Cost:** A draft is a second version of the record with its own name and its own rules about when it expires. It is a new aggregate, not a field on the form.

**Reference:** GOV.UK

#### `saved-silently-with-the-command-in-the-page-header`: Saved Silently, with the Command in the Page Header

```wireframe
page
  breadcrumb "Home" "Settings" "Standard"
  header
    heading "Standard"
    status "Active" positive
    action "Retire policy"
  region "The name"
    field "Name" text = "Standard"
    field "What it includes" textarea = "Standard limits for every trip."
    action primary "Save the name"
  region "Expense naming"
    field "Naming" select = "The spender name"
    action primary "Save the naming"
```

**Rationale:** For a settings page where each change is independent, each section saves itself on its own Save and the header carries nothing. If you change two areas you can save each.

**Cost:** Two Saves on one screen is the case where you edit one form and save the other. The rule has to be one Save per page or one per section, never a mix.

#### `a-read-only-form-because-the-state-forbids-it`: A Read-Only Form Because the State Forbids It

```wireframe
page
  breadcrumb "Home" "Payouts" "March run" "Review"
  header
    heading "Review"
    text "The March run is paying. 9,412 expenses carry EUR 45.00."
  region "Hotel night"
    field "Nightly amount, in euros" text = "45.00"
    text "The amount the paid expenses already carry. It cannot change."
    field "Amount for the expenses still open, in euros" text = "50.00"
    message caution "Paying the 588 differently is allowed. A spender who finds out can ask for the difference."
  footer
    action primary "Save the new amount"
```

**Rationale:** Once the run has started, the amount is a versioned fact and the field is read-only rather than a field with a save. Changing it is a different command with its own consequences.

**Cost:** A read-only field with no command leaves you stuck, unless the command to change it is stated. The explained-action pattern exists for exactly this case.

#### `a-save-bar-pinned-to-the-top-while-dirty`: A Save Bar Pinned to the Top While Dirty

```wireframe
page
  breadcrumb "Home" "Settings" "Remittance"
  header
    heading "Remittance"
    text "The accent color and typeface spenders see."
  toolbar sticky
    text "2 unsaved changes. Spenders still see the old ones."
    action "Discard"
    action primary "Save"
  region "Appearance"
    field "Accent color" text = "#B45309"
    field "Typeface" select = "Inter"
```

**Rationale:** Shopify Polaris pins a contextual save bar to the top of a dirty form. Save and Discard never scroll out of reach on a long settings page. The bar appears with the first keystroke and leaves with the save.

**Cost:** A pinned bar covers content on a phone and follows you past sections it has nothing to do with. It also needs the same dirty tracking as the marker, so it adds no new information, only reach.

**Reference:** Shopify Polaris

#### `per-field-revert-to-the-saved-value`: Per-Field Revert to the Saved Value

```wireframe
page
  breadcrumb "Home" "Settings" "Remittance"
  header
    heading "Remittance"
    text "The accent color and typeface spenders see."
  region "Appearance"
    field "Accent color" text = "#B45309"
    text muted "Was #1F2937."
    action quiet "Revert the color"
    field "Typeface" select = "Inter"
    text muted "Was System."
    action quiet "Revert the typeface"
  footer
    action quiet "Discard both"
    action primary "Save 2 changes"
```

**Rationale:** Webflow and Figma mark each changed field with a dot and offer Revert beside it. You keep one change and drop another without retyping. The old value sits under the field as the value you fall back to.

**Cost:** A dot plus a revert button per field adds two elements on every row of a long form. If you want to keep everything you still need the page-level Save. It pays off only where partial saves are common.

**Reference:** Figma

### Picking a Period

**ID:** `litespec/frontend/patterns/forms-and-fields#picking-a-period`

**Question:** Payouts and reporting run on periods. Two typed dates cover every window but ask for both edges, while presets cover the common windows in one tap. The decision is how the period control combines typing and presets.

**Default:** `two-date-inputs-with-presets`

**Compact:** Below the `md` breakpoint the painted calendar shows one month. The second month of the window follows below it. The compact option is `a-calendar-with-the-range-painted`.

**Rationale:** Keep two typed dates with presets. One tap covers the common windows, typing covers the rare ones, and both edges stay visible. The two-month painted calendar is the runner-up for reporting screens where the window itself is the question. Never ship presets alone with no typed fallback: no preset reaches the filing to payout window the payout check wants.

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

**Rationale:** The presets fill both fields and you adjust either edge by typing. The common windows cost one tap and the rare ones stay possible. The control that fills the fields and the fields that hold them are one unit.

**Cost:** Two controls describe one value. The preset shows Custom the moment either date is edited, or the row claims a window it no longer holds.

#### `preset-segmented-control-7-30-90-days`: Preset Segmented Control: 7, 30, 90 Days

```wireframe
page
  breadcrumb "Home" "Payouts" "March run"
  header
    heading "March run"
    text "What moved for Harbor and Pine."
  tabs "Last 7 days" "Last 30 days" "Last 90 days"
  region "Period"
    text "Counts back from Sat 14 Mar 2026: 14/02/2026 to 14/03/2026."
  footer
    action primary "Show the run"
```

**Rationale:** One tap covers the three windows a payout check almost always wants, with no typing. The resolved dates are shown back under the control so the preset is a promise you can read.

**Cost:** No preset reaches a trip-anchored window like the trip to the payout, so a custom range still needs a second control beside this one.

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

**Rationale:** Payout and tax reporting run by calendar month. Picking March 2026 in one control beats typing the first and the last day of the month by hand.

**Cost:** You cannot express a window that crosses a month edge, like the fortnight around the payout. Whole months are the only shape this control draws.

#### `open-start-rolling-window`: Open-Start Rolling Window

```wireframe
page
  breadcrumb "Home" "Payouts" "March run"
  header
    heading "March run"
    text "What moved for Harbor and Pine."
  region "Period"
    field "Window" select = "Since the quarter opened"
    field "Up to and including" date = "14/03/2026"
    text muted "From the quarter opening to Sat 14 Mar 2026."
  footer
    action primary "Show the run"
```

**Rationale:** Everything since the quarter opened needs an end date only. The start is the opening and never moves. One field replaces two where you cannot answer differently.

**Cost:** An open start silently includes old test expenses, so the resolved start must be shown back under the field. Without it you cannot tell what the window holds.

#### `refusal-when-the-end-precedes-the-start`: Refusal When the End Precedes the Start

```wireframe
page
  breadcrumb "Home" "Payouts" "March run"
  header
    heading "March run"
    text "What moved for Harbor and Pine."
  region "Period"
    message critical "The end is before the start. To must be on or after 14/03/2026."
    field "From" date = "14/03/2026"
    field "To" date = "07/03/2026"
    message critical "To must be on or after 14/03/2026."
  footer
    action primary "Show the run"
```

**Rationale:** The page refuses an end before the start where you typed it, in the GOV.UK template. The message names the field and says what to do. The summary above it says the same in one line.

**Cost:** It is one more state to draw and to test. The refusal waits until both fields hold a date. It never fires while you still type the second one.

**Reference:** GOV.UK

#### `timezone-safe-inclusive-day-boundaries`: Timezone-Safe Inclusive Day Boundaries

```wireframe
page
  breadcrumb "Home" "Payouts" "March run"
  header
    heading "March run"
    text "What moved for Harbor and Pine."
  region "Period"
    message info "Both days count in full. From 14/02/2026 to 14/03/2026, Europe/Amsterdam."
    field "From" date = "14/02/2026"
    text muted "Includes the whole day."
    field "To" date = "14/03/2026"
    text muted "Includes the whole day, to the end of Sat 14 Mar 2026."
  footer
    action primary "Show the run"
```

**Rationale:** Each edge is a whole day in Europe/Amsterdam. An expense filed at 22:41 on the payout day falls inside the window that ends on that day. The boundary sentence is part of the control, not documentation beside it.

**Cost:** The sentence must be read to work, and if you think in UTC you see different instants. Stating the zone once keeps it from being repeated under each field.

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

**Rationale:** Airbnb and Google Flights paint the chosen range across the calendar, so you see the window instead of reading two dates. The endpoints stay readable as dates under the control.

**Cost:** A painted month shows one month, and the payout window usually spans two. Two months side by side show the window truthfully, and they do not fit a phone.

**Reference:** Airbnb

#### `delivery-anchored-presets`: Payout-Anchored Presets

```wireframe
page
  breadcrumb "Home" "Payouts" "March run"
  header
    heading "March run"
    text "What moved for Harbor and Pine."
  tabs "Since filing" "Trip week" "Payout week"
  region "Period"
    text muted "Anchored to Tue 13 Jan 2026 and Sat 14 Mar 2026, not to today."
    field "From" date = "07/03/2026"
    field "To" date = "14/03/2026"
  footer
    action primary "Show the run"
```

**Rationale:** Mixpanel and Amplitude anchor ranges to the milestones the business runs on, because `last-30-days` drifts while the filing stays fixed. The preset resolves to typed dates you can still adjust.

**Cost:** Anchors multiply: filing, approval, close, and payout runs. Three anchors fit a control. Six need a picker of their own, and the window stops being one tap.

**Reference:** Mixpanel

### Slug or URL Field with a Preview

**ID:** `litespec/frontend/patterns/forms-and-fields#slug-or-url-field-with-a-preview`

**Question:** A trip takes a slug, the short part typed in, and spenders read the full address: they link it and print it. The decision is how the field takes the slug and previews the address before saving.

**Default:** `slug-with-the-live-address-under-it`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the slug with its live address under it. You type the short part and read the full address before saving. The transliteration from the name is the runner-up for creation, where typing the same words twice is pure overhead. Never ship an address that cannot be read before it is saved.

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

**Rationale:** Contentful and Shopify show the full address under the slug field and update it as you type. The short value is never saved unseen. The copy command beside it is how the address leaves the back office.

**Cost:** The address must update as you type, which a static wireframe cannot show. It also states the host the platform gives, which you cannot change on this screen.

**Reference:** Shopify

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

**Rationale:** GitHub shows a repository address as a fact and puts Rename behind a command, because an address in use is depended upon. The 30-day redirect is what makes the change survivable.

**Cost:** Changing a value takes two steps, and the redirect has to be a real behavior, not a sentence. A redirect nobody builds is a promise the page breaks.

**Reference:** GitHub

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

**Rationale:** WordPress builds the permalink from the title and lets you edit it, because the name and the slug agree nine times in ten. Typing both by hand is the same words twice.

**Cost:** The generated value must transliterate Dutch diacritics the way you expect, and a renamed trip must not silently re-slug. Generation runs once at creation, never again.

**Reference:** WordPress

#### `availability-check-with-alternatives-offered`: Availability Check with Alternatives Offered

```wireframe
page
  breadcrumb "Home" "Trips" "Rotterdam client visit" "Address"
  header
    heading "Trip address"
    text "Where spenders file against this trip."
  region "The address"
    field "Slug" text = "visit"
    message caution "visit is taken. Another company holds it. One of these is free."
    list 3
      item: text "client-visit" · action "Use"
  footer
    action "Cancel"
    action primary "Save address"
```

**Rationale:** Namecheap answers a taken domain with free alternatives, because a refusal without a next step sends you back to guessing. The alternatives keep the shape of what was wanted.

**Cost:** The page must check the alternatives live, one by one, or the list offers addresses that fail the same way. Three checks per keystroke pause is a read the page has to budget for.

**Reference:** Namecheap

#### `the-slug-refused-with-the-rule-that-refused-it`: The Slug Refused with the Rule That Refused It

```wireframe
page
  breadcrumb "Home" "Trips" "Rotterdam client visit" "Address"
  header
    heading "Trip address"
    text "Where spenders file against this trip."
  region "There is a problem"
    link "Use lowercase letters, numbers and dashes in the slug"
  region "The address"
    field "Slug" text = "Rotterdam Visit!"
    message critical "Use lowercase letters, numbers and dashes. You entered capitals, spaces and exclamation."
  footer
    action "Cancel"
    action primary "Save address"
```

**Rationale:** The GOV.UK error template applies to a slug. The summary comes first, then the field, with the message naming what was entered and what to do. The page states the rule once and enforces it the same way.

**Cost:** The message repeats the value, which makes it longer than the field. It also has to name every offending character class, not the first one found.

**Reference:** GOV.UK

#### `custom-domain-with-its-dns-instructions`: Custom Domain with Its DNS Instructions

```wireframe
page
  breadcrumb "Home" "Settings" "Workspace" "Domain"
  header
    heading "Workspace domain"
    text "File from your own address instead of ours."
  region "The domain"
    field "Domain" text = "expenses.harbor-pine.example"
    fact "Type" "CNAME"
    fact "Name" "expenses"
    fact "Value" "workspaces.claimwick.example"
    message info "Add this record where the domain lives, then verify. DNS changes take up to a day."
    status "Not verified" caution
  footer
    action primary "Verify"
```

**Rationale:** Vercel and Webflow pair a domain field with the exact DNS record it needs, because the field alone is half the task. The page states the record as values to copy, not as prose to interpret.

**Cost:** Verification is a read against DNS the back office does not control. The badge can only say what the last check found and when. A stale badge claims a state the world has left.

**Reference:** Vercel

### Capacity Field

**ID:** `litespec/frontend/patterns/forms-and-fields#capacity-field`

**Question:** A budget code takes money from one cost-centre budget: Amsterdam sales holds EUR 10,000, and every code draws from the same budget. The decision is how the capacity field shows the ceiling, the split, and the refusal past it.

**Default:** `a-number-with-the-warehouse-ceiling-restated`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** Keep the number with the budget ceiling restated. One field, the budget it draws from, and what is left sit on the same screen. The split across codes is the runner-up for the budget page, where the question is the whole budget rather than one code. Never ship a capacity with no ceiling stated: a number without its budget is a number that overspends.

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
    text "The budget is 98 percent spent."
    fact "Amsterdam sales holds" "EUR 10,000"
    text muted "PARTNER holds EUR 400 of it."
  footer
    action "Cancel"
    action primary "Save capacity"
```

**Rationale:** The budget total sits beside every ceiling field, because a code allocation means nothing without the budget it draws from. The bar shows how spent the budget is at a glance.

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

**Rationale:** Spending draws as one row per code with claimed against cap, because the question is the budget, not the row. The leftover line is what you came to read.

**Cost:** A row per code is a table wearing a list costume, and at twelve codes it needs the table back. Editing still jumps to one code at a time.

#### `a-stepper-for-a-small-count`: A Stepper for a Small Count

```wireframe
page
  breadcrumb "Home" "Finance" "Budgets" "Advances"
  header
    heading "Advances"
    text "Cash up front for trips, inside the TRIP10 ceiling."
  region "Money"
    row
      action "One euro less"
      field "Advance amount" number = "24"
      action "One euro more"
    text muted "Steps of one euro, up to 50. EUR 318 of EUR 400 is claimed."
  footer
    action "Cancel"
    action primary "Save advance"
```

**Rationale:** Apple HIG reserves the stepper for small counts adjusted by one, which a 24-euro advance is. Tapping plus twice beats selecting 24 and typing 26.

**Cost:** A stepper for 9,600 euros would be tapped 9,600 times, so it fits only counts under fifty. The field must still take a typed value for the jump from 4 to 40.

**Reference:** Apple HIG

#### `a-percentage-held-back-from-sale`: A Percentage Held Back in Reserve

```wireframe
page
  breadcrumb "Home" "Finance" "Budgets" "Capacity"
  header
    heading "Capacity"
    text "How much money stays held for the year-end close."
  region "The hold"
    field "Held back" number = "5"
    text muted "Percent of the budget. Released to the codes at the quarter start."
    fact "Held back" "EUR 500"
    fact "Left to spend" "EUR 9,500"
  footer
    action "Cancel"
    action primary "Save hold"
```

**Rationale:** Holds are a percentage of the budget kept for the close, restated as money so you read both. A percentage survives a budget change. A count does not.

**Cost:** Two numbers describe one hold, and they round differently: 5% of 10,000 is 500 until the budget becomes 9,999. The money wins and the percentage follows.

#### `the-capacity-refused-with-the-overage-named`: The Capacity Refused with the Overage Named

```wireframe
page
  breadcrumb "Home" "Finance" "Budgets" "TRIP10" "Capacity"
  header
    heading "Capacity"
    text "How much TRIP10 can spend."
  region "Money on the code"
    message critical "That is more than the budget holds. Lower an amount first."
    field "Ceiling, in euros" number = "10000"
    message critical "That is EUR 400 more than the budget holds. Lower it to 9,600."
    fact "Amsterdam sales holds" "EUR 10,000"
  footer
    action "Cancel"
    action primary "Save capacity"
```

**Rationale:** The refusal is worded on screen, with the field carrying the same message. GOV.UK names the field and says what to do, and the numbers supply the overage. You read the overage, not the rule alone.

**Cost:** The message must recompute the overage for every value, and a value that breaks two rules needs two messages. One refusal per field keeps it readable.

**Reference:** GOV.UK

#### `sold-as-a-read-only-fact-beside-an-editable-cap`: Claimed as a Read-Only Fact Beside an Editable Cap

```wireframe
page
  breadcrumb "Home" "Finance" "Budgets" "TRIP10" "Capacity"
  header
    heading "Capacity"
    text "How much TRIP10 can spend."
  region "Money on the code"
    fact "Claimed" "9,412"
    fact "Ceiling" "9,600"
    fact "Left" "188"
    message info "Claimed is a fact and cannot be edited. Lowering the cap below 9,412 is refused."
    field "Ceiling, in euros" number = "9600"
    text muted "Must stay at or above 9,412."
  footer
    action "Cancel"
    action primary "Save capacity"
```

**Rationale:** Stripe draws an available balance as a fact beside the controls that move it: claimed is what happened, the cap is what you decide. The two must never share a field.

**Cost:** Three figures plus a field for one decision, and the derived Left line must update as you type the cap. It is the most screen per field in this item.

**Reference:** Stripe

### Color Field for the Mail Accent

**ID:** `litespec/frontend/patterns/forms-and-fields#color-field-for-the-mail-accent`

**Question:** The company brands its approval mails with one accent color: a `#RRGGBB` value dark enough for white button text to read on it. The field takes the value and proves the contrast before the page refuses it.

**Default:** `hex-field-with-a-swatch-and-a-contrast-readout`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** The hex field with a swatch and a contrast readout is the pick. It takes any color, shows it, and states the ratio the validation enforces. The approver-button preview is the runner-up beside it, because a ratio is believed once it is seen. Never ship a free color with no contrast check: a pale accent ships white text nobody can read.

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

**Rationale:** Shopify theme editor pairs a hex field with a swatch and the rule it passes. A color value means nothing until it is seen. The readout repeats the validation rule, so you learn it before the request rather than after it.

**Cost:** The ratio must recompute as you type, and an unfinished value has no ratio to state. Six characters that are not yet a color need patience, not a refusal.

**Reference:** Shopify

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

**Rationale:** Slack and Notion offer a row of presets that all satisfy the rule, with a custom field for you when you know better. One tap covers the common case and cannot produce a failing color.

**Cost:** Six presets cannot hold a brand color, so the custom field carries the real work anyway, and a preset row that never changes starts to look like the whole choice.

**Reference:** Slack

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
    text "2 lines, EUR 125.00"
    action "Approve EUR 125.00"
    text muted "Harbor and Pine, Canal Street 12, 1011 AB Amsterdam"
  footer
    action "Cancel"
    action primary "Save accent"
```

**Rationale:** Mailchimp previews a brand color on the button the approver presses, because a ratio is believed once it is seen. You read white on `#1F2937` instead of reading `14.7:1`.

**Cost:** A preview is a second surface that must match the typed value. It shows the typed value, not the saved one, and updates as you type. A stale preview proves nothing.

**Reference:** Mailchimp

#### `the-browser-color-well-beside-the-hex`: The Browser Color Well Beside the Hex

```wireframe
page
  breadcrumb "Home" "Settings" "Mail"
  header
    heading "Mail style"
    text "The accent color approvers see on mail buttons."
  region "Accent"
    row
      field "Pick the accent color" text = "#1F2937"
      field "Accent color" text = "#1F2937"
    text muted "Pick a color or type a #RRGGBB value. Both hold the same accent."
    fact "White button text" "Readable, needs 4.5:1, this accent reaches 14.7:1"
    status "Readable" positive
  footer
    action "Cancel"
    action primary "Save accent"
```

**Rationale:** The browser native color well is the only picker that costs no code. One input holds the platform dialog and a hex you can still type. MDN documents it as the accessible default.

**Cost:** The well draws the browser chrome instead of the back-office control. It renders differently everywhere, which is the same charge the native date input faces. It also offers colors that fail `4.5:1` without saying so.

**Reference:** MDN

#### `a-pale-color-refused-with-the-ratio-that-failed`: A Pale Color Refused with the Ratio That Failed

```wireframe
page
  breadcrumb "Home" "Settings" "Mail"
  header
    heading "Mail style"
    text "The accent color approvers see on mail buttons."
  region "Accent"
    image "Accent #F59E0B swatch"
    field "Accent color" text = "#F59E0B"
    message "White button text needs 4.5:1 against the accent. #F59E0B reaches 2.1:1. Choose a darker color." critical
  footer
    action "Cancel"
    action primary "Save accent"
```

**Rationale:** The validation rule appears worded on screen in the GOV.UK template. The message names the value, states both ratios, and says what to do. You learn the rule from the refusal, not from documentation.

**Cost:** A refusal that only says darker leaves you guessing how much darker. It earns its place fully only beside presets that all pass.

**Reference:** GOV.UK

#### `an-inherited-accent-with-an-override-command`: An Inherited Accent with an Override Command

```wireframe
page
  breadcrumb "Home" "Trips" "Rotterdam client visit" "Style"
  header
    heading "Trip style"
    text "What approvers see for this trip."
  region "Accent"
    fact "Harbor and Pine accent" "#1F2937"
    fact "This trip" "Inherits the company accent"
    message "One accent across every trip keeps the brand steady. Override it only for a trip with its own branding." info
    action "Override for this trip"
```

**Rationale:** Figma variables inherit down the tree and override at one level. The source stays named, so a trip can differ without hiding where its value came from. The company default stays the decider.

**Cost:** An inherited value with an override is two values and a rule about which wins. If you never open this screen you cannot tell which trips differ.

**Reference:** Figma

### Repeatable Group for Budget Codes

**ID:** `litespec/frontend/patterns/forms-and-fields#repeatable-group-for-budget-codes`

**Question:** The trip budget holds TRIP10, MEAL20, and TAXI05: three groups with the same fields, each with its own name, ceiling, and allocation. The group repeats, and the budget counts every repetition.

**Default:** `stacked-cards-one-per-type`

**Compact:** At phone width the table keeps its columns and scrolls sideways. The code column stays first. The compact option is `a-table-with-inline-editing`.

**Rationale:** The stacked cards are the pick: one card per code with its ceiling and allocation readable at once, and one command to add another. The dated budget steps are the runner-up for quarters, where time decides the ceiling rather than the spender choice. Never ship a group whose repetitions cannot be removed: a code added by mistake must leave without taking its claimed amounts with it.

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
      item: text "TRIP10" · text muted "412 of 500 claimed" · status "Active" positive · text "EUR 500 cap" · action "Edit"
  footer
    action "Add a budget code"
    action primary "Save order"
```

**Rationale:** Shopify draws product variants as one row per variant with its price readable at once, because you compare repetitions before editing one. Claimed codes stay visible but lose their Remove.

**Cost:** A row shows the ceiling but hides the dates, the cost-centre rule, and the description, so editing still opens each code. The list answers what, never why.

**Reference:** Shopify

#### `a-table-with-inline-editing`: A Table with Inline Editing

```wireframe
page
  breadcrumb "Home" "Finance" "Budgets" "Budget codes"
  header
    heading "Budget codes"
    text "What a spender claims and what the budget pays."
  region "Three codes"
    table 3 "Code" "Ceiling" "Claims" "Claimed"
      item: text "TRIP10" · text "EUR 500" · text "500" · text "412"
    field "TRIP10 ceiling in euros" text = "500"
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
    table 3 "Code" "Ceiling" "Claims" "Claimed"
      item: text "TRIP10" · text "EUR 500" · text "500" · text "412"
  footer
    action "Add a budget code"
    action primary "Save all"
```

**Rationale:** Airtable edits values where they sit, so changing three ceilings costs three fields and no navigation. One row in edit state at a time keeps the table readable while it is also a form.

**Cost:** A table that is also a form validates cell by cell, and a refused cell must hold its row open. At phone width the columns scroll sideways, which hides the ceiling behind a swipe.

**Reference:** Airtable

#### `one-section-per-type-with-add-another`: One Section per Type with Add Another

```wireframe
page
  breadcrumb "Home" "Finance" "Budgets" "Budget codes"
  header
    heading "Budget codes"
    text "What a spender claims and what the budget pays."
  region "Code 1 of 2"
    field "Name" text = "TRIP10"
    field "Ceiling, in euros" text = "500"
    field "Maximum uses" text = "500"
    text muted "412 claimed."
    action "Remove"
  region "Code 2 of 2"
    field "Name" text = "MEAL20"
    field "Ceiling, in euros" text = "200"
    field "Maximum uses" text = "400"
    text muted "318 claimed."
    action "Remove"
  footer
    action "Add another budget code"
    action primary "Save codes"
```

**Rationale:** GOV.UK `add-another` repeats the full group once per entry with its own remove. Each repetition deserves the same labels, help, and errors as the first. Nothing about code 2 is abbreviated.

**Cost:** Three full groups make a long page, and the budget total has nowhere to sit. It fits creation, where each code is new. It drags for review, where each code is known.

**Reference:** GOV.UK

#### `dated-price-steps-for-the-launch`: Dated Budget Steps for the Quarter

```wireframe
page
  breadcrumb "Home" "Finance" "Budgets" "Budget codes"
  header
    heading "Budget codes"
    text "Time decides the ceiling. The spender claims whatever step is open."
  region "Two steps"
    text muted "Steps never overlap. The next step opens the moment the current one closes."
    list 2
      item: text "Q1, EUR 500" · text muted "1 Feb to 14 Mar 2026, 08:00, 412 of 500 claimed" · status "Active" positive · text "Open"
  footer
    action "Add a step"
    action primary "Save steps"
```

**Rationale:** A quarter steps its ceilings by date, because the budget resets each quarter. The dates are part of each step, so you answer the ceiling on any day by reading one row.

**Cost:** Dated steps plus per-code allocations divide one budget in two ways. They can disagree. A step can close with money left, or run out with days left. One of the two has to win.

#### `drag-to-reorder-with-the-customer-order-shown`: Drag to Reorder with the Spender Order Shown

```wireframe
page
  breadcrumb "Home" "Finance" "Budgets" "Budget codes"
  header
    heading "Budget codes"
    text "Spenders meet them in this order. Drag a row to move it."
  region "Three codes"
    text muted "The numbers are the spender order on the filing form."
    list 3
      item: icon "Reorder" · text "1, TRIP10" · text muted "EUR 500 cap, active" · action quiet "Move up"
  footer
    action primary "Save order"
```

**Rationale:** Linear and Trello order by dragging with button equivalents beside each row, because drag alone strands keyboard users. The spender order is numbered, so the finance order and the filing order cannot drift apart silently.

**Cost:** Order is one more field per code with its own conflicts: two people reordering at once, and a code added in the middle. It needs the same version check as every shared write.

**Reference:** Linear

#### `the-group-refused-naming-the-type-that-breaks-the-ceiling`: The Group Refused, Naming the Type That Breaks the Ceiling

```wireframe
page
  breadcrumb "Home" "Finance" "Budgets" "Budget codes"
  header
    heading "Budget codes"
    text "What a spender claims and what the budget pays."
  message "That is more than the budget holds. Amsterdam sales holds EUR 10,000." critical
  region "Three codes"
    text muted "EUR 10,400 on the codes. The budget holds EUR 10,000."
    list 2
      item: text "TRIP10, EUR 500 cap" · text muted "EUR 10,000 on the code. Lower it by EUR 400." · text "EUR 10,000" · action "Change"
  footer
    action primary "Save codes"
```

**Rationale:** The refusal sits above the group and names the offending row below it. GOV.UK puts the summary first and the message on the field. Here the field is a row. TRIP10 carries the overage because TRIP10 grew last.

**Cost:** Blaming one row for a budget total is a guess the page makes about intent: you may sooner cut MEAL20 than TRIP10. The message must offer both, not decide.

**Reference:** GOV.UK

### Tag Input

**ID:** `litespec/frontend/patterns/forms-and-fields#tag-input`

**Question:** Expenses and trips carry short labels: travel, meals, client. Tags are typed freely but read everywhere, so the input must take new words and still keep old ones spelled one way.

**Default:** `chips-with-type-to-add-and-suggestions`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** The chips with type-to-add and suggestions are the pick: new words are allowed and old ones are offered, so `travel` is spelled once. The checkbox list is the runner-up where the tag set is closed and counted. Never ship free text with no suggestions: it grows `travel`, `Travel`, and `travel-meals` side by side.

**Reviewed for:** Which built label chip breaks from the wireframe its catalog entry chose? Open the built chips, popover lists, and inherited tags beside the catalog entry recording the chosen option and its reason.

#### `chips-with-type-to-add-and-suggestions`: Chips with Type-to-Add and Suggestions

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "Tags"
  header
    heading "Tags"
    text "Short labels spenders and finance read."
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

**Rationale:** GitHub and Linear offer existing labels as you type. They accept a new word on Enter, so the old spelling wins without forbidding the new one. The count beside each suggestion says how shared it is.

**Cost:** Suggestions must search across every expense on every keystroke pause, and a slow search answers after you have already pressed Enter. The new word wins by timing, not by choice.

**Reference:** GitHub

#### `a-checkbox-list-in-a-popover-with-a-count`: A Checkbox List in a Popover with a Count

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "Tags"
  header
    heading "Tags"
    text "Short labels spenders and finance read."
  region "The tags"
    action "3 tags chosen"
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

**Rationale:** Shopify and Jira draw a closed tag set as checkboxes with a count on the button. Ticking is faster than typing when the words already exist. Managing the list is a separate task with its own dialog.

**Cost:** A closed set cannot take a new word where it is needed, so you leave the form to make one. It fits a governed vocabulary. It fights a growing one.

**Reference:** Shopify

#### `free-text-split-on-commas`: Free Text Split on Commas

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "Tags"
  header
    heading "Tags"
    text "Short labels spenders and finance read."
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

**Rationale:** Stack Overflow and WordPress take tags as one comma-separated line with the parsed chips read back below. Pasting a list from elsewhere works without retyping. The readout shows exactly how the line was split.

**Cost:** No suggestions means no shared spelling: `travel` and `Travel` become two tags and nobody is told. The readout shows the split but cannot judge the words.

**Reference:** WordPress

#### `color-coded-tags-for-audience-segments`: Color-Coded Tags for Spender Groups

```wireframe
page
  breadcrumb "Home" "Spenders" "Groups"
  header
    heading "Groups"
    text "Who gets the receipt reminder before the close."
  region "The groups"
    row
      status "Regulars" info
      status "Early filers" positive
      status "At risk" caution
    fact "Regulars" "1,204 spenders, filed twice or more"
    fact "Early filers" "2,000 spenders, filed in the first week"
    fact "At risk" "318 spenders, filed last quarter, nothing since"
    action "Add"
  footer
    action "Cancel"
    action primary "Save groups"
```

**Rationale:** Mailchimp and Klaviyo color groups by meaning, because a segment is read in lists where its word is small. The tone repeats the signal instead of decorating it.

**Cost:** Five tones cover five meanings and no more. A sixth group reuses a tone and the code breaks. Color is never the only signal, so the word stays anyway.

**Reference:** Mailchimp

#### `a-tag-refused-with-the-rule`: A Tag Refused with the Rule

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "Tags"
  header
    heading "Tags"
    text "Short labels spenders and finance read."
  region "The tags"
    row
      status "travel" neutral
      status "meals" neutral
    field "Tags" text = "Travel Meals!"
    message "Tags hold lowercase letters, numbers and dashes. You entered capitals, a space and !." critical
  footer
    action "Cancel"
    action primary "Save tags"
```

**Rationale:** The GOV.UK error template applies to the tag being added. The good chips stay and the bad word carries the message. The rule is stated with the value that broke it.

**Cost:** The message judges one word while the field holds a list, so you must tell which word failed. The chips above carry that by staying put.

**Reference:** GOV.UK

#### `inherited-tags-with-their-source-named`: Inherited Tags with Their Source Named

```wireframe
page
  breadcrumb "Home" "Settings" "Standard" "Tags"
  header
    heading "Standard tags"
    text "Labels on this approval policy."
  region "The tags"
    text "From every trip"
    row
      status "travel" neutral
      status "meals" neutral
    text muted "Every trip under the policy carries these. Change them on the policy."
    field "On Standard itself" text = "featured"
    field "Add a tag" text
  footer
    action "Cancel"
    action primary "Save tags"
```

**Rationale:** HubSpot shows inherited properties with their source beside the local ones. A policy page answers the whole label set without hiding where each word came from. The page refuses removing an inherited word by pointing at its source.

**Cost:** The page shows two lists where one was expected, and the inherited one cannot be edited here. If you want the policy untagged you must walk up the tree.

**Reference:** HubSpot

### Address Field

**ID:** `litespec/frontend/patterns/forms-and-fields#address-field`

**Question:** Harbor and Pine answers for its expenses with a name, an address, and a telephone number on every spender message. The address field takes the street the law asks for and the postcode the mail needs.

**Default:** `postcode-lookup-that-fills-the-street`

**Compact:** The recommended option reflows: one column, header actions under the title, and a table scrolls inside its own region.

**Rationale:** The postcode lookup is the pick: two short values you know produce the street you would mistype. The country-first manual entry is the runner-up for companies outside the lookup coverage. Never ship a single free-text address box: one line cannot be split into street, postcode, and city again.

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

**Rationale:** Postcode.nl and GOV.UK ask for postcode plus number first, because you know both by heart. The lookup then fills the street you would mistype. Two short fields beat one long one.

**Cost:** The lookup covers the Netherlands and nowhere else, so every other country falls back to manual entry. Two paths through one field are two paths to test.

**Reference:** GOV.UK

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

**Rationale:** Google Places resolves a half-typed street to a full address, because you know where you are but not how the database spells it. One field holds the whole search.

**Cost:** An address search reads a provider the back office pays per keystroke, and you may choose a wrong first result without reading. The manual fallback must stay one tap away.

**Reference:** Google Places

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

**Rationale:** Stripe Checkout asks for the country first because the shape of an address follows it: a Dutch postcode is nothing like a German one. The labels and the validation change with the country.

**Cost:** Every supported country needs its own shape, its own labels, and its own validation. Three countries fit this pattern. Thirty need a library.

**Reference:** Stripe

#### `warehouse-address-with-a-map-pin`: Office Address with a Map Pin

```wireframe
page
  breadcrumb "Home" "Settings" "Offices" "Rotterdam office"
  header
    heading "Rotterdam office"
    text "Where the finance team sits."
  region "The address"
    fact "Office" "Rotterdam office"
    fact "City" "Rotterdam"
    fact "Rooms" "Finance room, audit room"
    image "Map pin, Rotterdam office, Rotterdam, pin placed by Harbor and Pine"
  footer
    action "Move the pin"
    action primary "Save address"
```

**Rationale:** The office is pinned on a map beside its address, because auditors navigate to a pin, not to a street name. The company places the pin instead of guessing it from the postcode.

**Cost:** A map is a third-party surface with its own terms and its own load time. A static pin box proves the pattern. The live map is a dependency, not a field.

#### `the-postcode-refused-with-the-expected-shape`: The Postcode Refused with the Expected Shape

```wireframe
page
  breadcrumb "Home" "Settings" "Organization"
  header
    heading "Organization"
    text "What Harbor and Pine is and where it works."
  region "The address"
    field "Street and number" text = "Canal Street 12"
    row
      field "Postcode" text = "1011"
      field "City" text = "Amsterdam"
    message "A Dutch postcode holds four digits, a space and two letters, like 1011 AB." critical
  footer
    action "Cancel"
    action primary "Save address"
```

**Rationale:** The GOV.UK error template on a postcode states the shape and gives an example. You repair 1011 without opening a second page, following the example beside the field.

**Cost:** The shape differs per country, so the message must follow the country field. A Dutch message under a Belgian address refuses a value that was right.

**Reference:** GOV.UK

#### `a-verified-address-with-a-change-command`: A Verified Address with a Change Command

```wireframe
page
  breadcrumb "Home" "Settings" "Organization"
  header
    heading "Organization"
    text "What Harbor and Pine is and where it works."
  region "The address"
    fact "Street" "Canal Street 12"
    fact "Postcode and city" "1011 AB Amsterdam"
    fact "Country" "Netherlands"
    status "Verified" positive
    message "Verified against the chamber of commerce on 2 Sep 2026. Changing it verifies the account again." info
    action "Change the address"
```

**Rationale:** Banks and company registers show a verified address as a fact with a change command. Payouts and invoices depend on a verified value. The badge states what the last check found.

**Cost:** Verification is a check against a register the back office does not hold, so the badge only grows older from its check date. A move the company forgets to report leaves a true badge on a false address.

### Email Body Editor

**ID:** `litespec/frontend/patterns/forms-and-fields#email-body-editor`

**Question:** Claimwick mails every spender when a payout is sent, and the message carries the spender name, the expense number, and the arrival date. The editor takes the company words once and fills the spender facts per expense.

**Default:** `subject-plus-body-with-variable-chips`

**Compact:** The preview sits below the editor. You scroll between writing and reading. The compact option is `side-by-side-preview-of-the-customer-message`.

**Rationale:** The subject plus body with variable chips is the pick: you write the sentence and place the facts where they belong. The side-by-side spender preview is the runner-up beside it, because a template is believed once it is seen filled. Never ship a template with no preview: variables are spelled in one place and filled in another, and only the filled copy proves they meet.

**Reviewed for:** Which built message composer breaks from the wireframe its catalog entry chose? Open the built subject-plus-body forms and variable chips beside the catalog entry recording the chosen option and its reason.

#### `subject-plus-body-with-variable-chips`: Subject Plus Body with Variable Chips

```wireframe
page
  breadcrumb "Home" "Settings" "Email" "Payout sent"
  header
    heading "Payout sent"
    text "The message a spender looks for. Sent when a payout is sent."
  region "The message"
    field "Subject" text = "Your payout of EUR 125.00 is on its way"
    text "Hi spender, your payout left Harbor and Pine. It arrives soon."
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

**Rationale:** Klaviyo and Mailchimp draw template variables as chips inside the sentence, because a variable is a word with a value, not code. You read the message as the spender will, with the facts marked.

**Cost:** Chips inside an editable body are a small rich-text editor, not a `textarea`. Backspace across a chip, paste with chips, and undo all need answers. The plain-text option exists for that reason.

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
      field "Subject" text = "Your payout of EUR 125.00 is on its way"
      field "Body" textarea = "Hi spender, your payout left Harbor and Pine."
    card "Spender preview"
      heading small "Your payout of EUR 125.00 is on its way"
      text "Hi Ava,"
      text "Your expense EXP-1042 was paid. It arrives Sat 14 Mar 2026."
      text muted "Harbor and Pine, Canal Street 12, 1011 AB Amsterdam"
  footer
    action "Send a test"
    action primary "Save message"
```

**Rationale:** Postmark and SendGrid preview a template beside the editor with a real record filled in, because a variable is only proven by its value. The payout of Ava fills every variable this template holds.

**Cost:** Two columns collapse to one on a phone, so the preview sits below the editor and you scroll between them. A long template makes a long round trip.

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
      field "Subject" text = "Your payout of EUR 125.00 is on its way"
      field "Body" textarea = "Hi spender, your payout left Harbor and Pine."
    region "Variables"
      fact "((spender name))" "Ava"
      fact "((expense number))" "EXP-1042"
      fact "((arrival date))" "14 Mar 2026"
      text muted "Spelled with double brackets. The value fills when the mail is sent."
  footer
    action "Send a test"
    action primary "Save message"
```

**Rationale:** GOV.UK Notify writes templates as plain text with `((double bracket))` variables and lists each one beside the editor with an example value. Nothing about the message can break in a mail client that reads text.

**Cost:** Double brackets are learned, not guessed: `((arrival date))` with one bracket fails silently or loudly depending on the parser. The editor must highlight what it recognizes as you type.

**Reference:** GOV.UK

#### `one-tab-per-language`: One Tab per Language

```wireframe
page
  breadcrumb "Home" "Settings" "Email" "Payout sent"
  header
    heading "Payout sent"
    text "The message a spender looks for."
  tabs "Nederlands" "English"
    region "Nederlands"
      field "Subject" text = "Je uitbetaling is onderweg"
      field "Body" textarea = "Hi spender, je uitbetaling is onderweg."
      status "English is missing" caution
  footer
    action primary "Save message"
```

**Rationale:** Contentful and Shopify Translate keep one tab per locale with the missing ones badged. A spender reads exactly one language, and you know which ones exist. The badge names the gap instead of hiding it.

**Cost:** Two tabs are two templates to keep in step. A variable added to one is added to the other. The Dutch sentence grows while the English one waits. Tabs hide the drift they also cause.

**Reference:** Shopify

#### `the-send-refused-with-the-missing-variable-named`: The Send Refused with the Missing Variable Named

```wireframe
page
  breadcrumb "Home" "Settings" "Email" "Payout sent"
  header
    heading "Payout sent"
    text "The message a spender looks for."
  message "Expense number is missing. The body asks for ((expense number)) and this test expense holds none." critical
  region "The message"
    field "Subject" text = "Your payout of EUR 125.00 is on its way"
    field "Body" textarea = "Hi spender, your payout is on its way."
    message "((expense number)) has no value on this expense. Add a fallback." critical
  footer
    action "Send a test"
    action primary "Save message"
```

**Rationale:** GOV.UK Notify refuses a send whose personalization is missing and names the variable. A blank where the expense number belongs is worse than no mail. The message offers the fallback shape, not the complaint.

**Cost:** The refusal can only name what the test record lacks. A variable filled on the test expense but empty on a real one still sends blank. Every variable needs a fallback, not a test.

**Reference:** GOV.UK

#### `a-sent-copy-with-the-values-filled-in`: A Sent Copy with the Values Filled In

```wireframe
page
  breadcrumb "Home" "Expenses" "EXP-1042" "Mail"
  header
    heading "Payout sent"
    text "Sent for expense EXP-1042."
  region "The sending"
    fact "To" "ava@harbor-pine.example"
    fact "Sent" "8 Oct 2026, 09:33"
    fact "Expense" "EXP-1042"
    status "Delivered" positive
    text "Hi Ava,"
    text "Your expense EXP-1042 was paid. It arrives Sat 14 Mar 2026."
  footer
    action "Resend"
```

**Rationale:** Postmark activity keeps the sent copy with every variable filled. Reading what was sent settles a dispute about what the spender was told. The record is read-only: history does not take edits.

**Cost:** A copy per sending is storage that grows with every payout, and each copy holds personal data under retention rules. The record must expire with the payout, not live beside it.

**Reference:** Postmark
