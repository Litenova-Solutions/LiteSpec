# Record Language

## Intent

Record Language states one grammar and one tone for every name and every value a project writes or a tool prints. Agents and authors apply it to record fields, values, identifiers, finding codes, and tool names. The [Authoring Standard](authoring.md) states the prose of LiteSpec pages; this page states the language of project records.

## Concepts

### The Eight Word Classes

Every field name belongs to one of eight word classes. The class decides the name's form, and the reading test tells whether a name sits in its class.

| Class | Form | Reading test | Examples |
|:---|:---|:---|:---|
| 1. Identity | fixed words | none | `type`, `id`, `title`, `owner`, `status`, `terms`, `questions`, `records` |
| 2. Link to many records | active present-tense verb | "journey serves goal" | `serves`, `fulfills`, `solves`, `advances`, `cites`, `governs`, `raises`, `reads`, `changes`, `covers`, `addresses` |
| 3. Link to one record in a role | singular noun naming the role | "the journey's actor is traveller" | `actor`, `entryPoint`, `fallback`, `runbook`, `result`, `host`, `refusal` |
| 4. Link where the target acts on this record | `<participle>By` | "the data class is acceptedBy the requirement" | `approvedBy`, `acceptedBy`, `enforcedBy`, `releasedBy`, `computedBy`, `drivenBy`, `triggeredBy` |
| 5. Closed set of values | noun naming the dimension; `kind` only for the record's own subtype | "its mode is atomic" | `kind`, `mode`, `direction`, `trust`, `reach`, `lifecycle`, `level` |
| 6. Prose | singular noun for what the text is; plural noun for a list of sentences | "its statement is ..." | `statement`, `meaning`, `rationale`, `summary`, `conclusion`, `failure`, `exclusions` |
| 7. Condition | `<participle or adjective>When` for a named condition; `condition` inside an entry | "it is finishedWhen ..." | `finishedWhen`, `frozenWhen`, `visibleWhen`, `skippedWhen`, `reopenedWhen`, `removedWhen` |
| 8. Reason | `<participle>Because`, or `reason` alone | "it is refusedBecause ..." | `refusedBecause`, `appliesBecause` |

Dates and booleans follow the classes: a deadline is `<base verb>By` (`decideBy`, `reviewBy`, `endBy`; `dueBy` and `plannedFor` hold a date or a milestone); a recorded date is `<verb>edOn` (`approvedOn`, `answeredOn`, `readOn`); a validity start is `effectiveFrom`; a boolean is a present-tense predicate that is true about the record when `true` (`applies`, `secret`, `recorded`), with no `is` or `has` prefix.

### Kinds of Text

Every piece of text a project writes falls into one of these kinds. The kind decides its form; the layer does not.

| Kind of text | Form | Example |
|:---|:---|:---|
| What holds: requirement, rule, goal and problem statements, journey steps, result meanings | Present tense, active voice, third person, one complete sentence ending in a period, no modal words | "A room the traveller starts to book is held for them for 10 minutes." |
| What happened: event and integration-event meanings | Past tense, one sentence | "The buyer placed the order." |
| A state something is in: aggregate state names, saga exit names, proof words | Past participle or adjective | `Held`, `Paid`, `withdrawn`, `verified`, `stale` |
| What to do: runbook steps, procedure steps | Imperative, one action per step | "Restart the payment worker." |
| A condition: `*When`, `condition`, `skippedWhen` | A clause that completes "when ...", without the word | "the application links to no event" |
| A reason: `rationale`, `*Because`, exception `reason` | A clause that completes "because ...", without the word | "partners will not share the no-show risk" |
| A question | One question in product words, ending in "?" | "How long should a room stay held during checkout?" |
| A definition: `meaning` of a term, a field, a setting | A noun phrase starting with A, An or The, at most 25 words, no leading verb | "A confirmed reservation of one room for one stay." |
| A title | The record's name in Title Case, no period | `use-case/orders.place-order` is titled "Place Order" |
| A finding message | One present-tense sentence of fact | "Requirement 'booking.service-fee' is fulfilled by no Behavior record." |

Gherkin fields keep Gherkin practice: `given` is the starting state (present tense, or past tense for an earlier act), `when` is one act by a named actor (present tense), `then` is the observable outcome (present tense, without "should"). A result's `meaning` says what is now true (success) or what the caller is told (refusal).

Person: record values are third person. Intent chapter prose (The Situation, What We Do) may say "we" for the business. Nothing says "you". Capitalized modal words belong only to LiteSpec's own provisions; a project's record states a fact or a commitment as a fact.

Quantities: dates are ISO 8601 dates, durations ISO 8601 durations (`P7Y`, `PT10M`), money is `{ amount, currency }` with an ISO 4217 code, and prose writes numbers with their unit ("95 percent", "3 seconds").

### Value Style

Each value below takes the written form its row gives. The last column names the mechanical test the value meets.

| Field | Write it as | Test |
|:---|:---|:---|
| `statement` (requirement, rule) | One present-tense declarative sentence, third person, product words, ending in a period | Ends in a period; no modal word in lower case such as "should" |
| `statement` (goal) | The outcome as a present-tense fact about the world | Ends in a period |
| `statement` (problem) | The pain as it happens today, present tense | Ends in a period |
| step `statement` | Actor first, present tense: "The traveller pays for the booking." | Starts with "The" or an actor title; ends in a period |
| `meaning` | A noun phrase starting with "A" or "An", no leading verb, at most 25 words | Starts with A, An or The; word count |
| `rationale`, `*Because` | A clause completing "because ..." | Does not start with "because" |
| `*When`, `condition` | A clause completing "when ..." | Does not start with "when" or "if" |
| `failure` | What the person experiences, present tense, never a code | Contains no refusal-code pattern |
| `question` | One question in product words | Ends in "?" |
| `title` | Title Case noun phrase, no period | Title Case; no final period |

### Names for Records

Each record type takes the name form its row gives, in the word class of what the record is.

| Type | Name form | Example |
|:---|:---|:---|
| goal | outcome noun phrase | `more-completed-bookings` |
| problem | the pain as a short clause | `rooms-sell-out-at-checkout` |
| feature | the offer as a noun phrase | `room-hold` |
| topic | subject noun | `checkout` |
| requirement | the assertion | `room-held-during-checkout` |
| journey | the task as a verb phrase | `book-a-room` |
| constraint | the rule's subject | `total-price-display` |
| research | the question's subject | `partner-hold-limits` |
| decision | number and subject | `0001-room-hold-length` |
| use case | verb first, from the verb vocabulary | `hold-room` |
| module | plural noun of the business area | `orders` |
| aggregate | singular noun | `order`, `ticket-resale` |
| rule | the assertion, keeping its negation word | `order.checkout-commits-with-holds`, `expense.approver-is-not-submitter` |
| event | `<aggregate>.<past participle>` | `order.placed` |
| integration event | `<aggregate>-<past participle>` | `order-placed` |
| reaction | `<verb>-<what>-on-<fact>` | `issue-credit-note-on-refund-succeeded` |
| job | `<verb>-<what>`, with the verb of the use case it runs | `expire-lapsed-ticket-resales` |
| saga step | verb first | `hold-place`, `refund-buyer` |
| saga exit, aggregate state | past participle or adjective | `withdrawn`, `Listed` |
| notification | `<subject>-<event>` noun phrase | `order-confirmation` |
| setting | noun phrase naming the value | `checkout-hold-window` |
| data class | noun phrase naming the data | `buyer-contact` |
| entry point, external system | noun naming the door or system | `webshop`, `payout-bank` |
| external operation | verb first | `create-charge` |
| screen | `<entry-point>.<noun>` | `webshop.basket` |
| runbook | verb first, what the reader does | `clear-stuck-refunds` |
| exception | noun phrase of what it allows | `payment-retries-later` |
| term | the singular word itself | `booking` |

### Spelling and Fixed Names

American spelling is the project spelling for field names, enum values, and prose: `license` rather than `licence`, and `pseudonymize` rather than `pseudonymise`. Each British form reads as a rejected word beside its American spelling.

Names an outside standard fixes stay exactly as that standard spells them. Gherkin fixes `given`, `when`, and `then`, and CloudEvents fixes its attribute names; these are the only names outside the word classes.

### Finding Codes and Tool Names

A finding code reads as `<subject>-<problem>`: the subject is a record type or field as the glossary spells it, and the problem is one of twelve words, each with one meaning.

| Problem | Meaning | Example |
|:---|:---|:---|
| `missing` | a required thing is absent | `use-case-scenario-missing` |
| `unknown` | a name matches nothing declared | `owner-unknown` |
| `undeclared` | used without a declaration | `erasure-undeclared` |
| `unrecorded` | true in code but written in no record | `behavior-unrecorded` |
| `unimplemented` | declared but built nowhere | `use-case-unimplemented` |
| `unreached` | built but reached by no flow | `use-case-unreached` |
| `drift` | the built thing differs from its record | `setting-key-drift` |
| `invalid` | a value breaks its form | `field-invalid` |
| `duplicate` | the same thing is declared twice | `identifier-duplicate` |
| `clash` | two names collide | `aggregate-name-clash` |
| `expired` | the date passed | `exception-expired` |
| `reopened` | approved content changed after approval | `approval-reopened` |

A finding message states one present-tense fact: it opens with the subject title and its quoted identifier and says what is true, not what to do. What to do lives in the finding table on the [model page](../tools/model.md), in a column for it. Change codes in `litespec model diff` follow the same pattern with past participles (`refusal-added`, `rule-weakened`, `requirement-narrowed`). Analyzer diagnostics keep their `LS` numbers and follow the same message rule.

Subcommands are verbs (`build`, `check`, `render`, `query`, `diff`, `review`, `approve`). Flags are kebab-case: a mode flag names its mode (`--release`, `--staged`, `--dry-run`), and a value flag names its value (`--since <ref>`, `--by <owner>`). Exit codes stay 0 for clean, 1 for blocking, and 2 for bad arguments. Book labels come from the Links table's reads-as phrases and the glossary only.

## Standards

### Name Each Field by Its Word Class

**ID:** `litespec/core/language#name-each-field-by-its-word-class`

**Statement:** Every record field MUST use the name its word class gives it in the word-class table.

**Rationale:** One grammar with no exceptions list means a reader who learned `cites` on a journey reads `cites` on a requirement without a second look. Each class answers one question about the field: whether it identifies, links, classifies, narrates, conditions, or reasons. Only names an outside standard fixes stand outside this grammar.

**Example:** The Claimwick journey `submit-an-expense` cites its goal in `cites`, states each step in `statement`, and records approval in `approvedBy`.

**Reviewed for:** Which fields break their word class? Look at the field names the change adds, beside the word-class table.

### Name a Boolean as a Predicate

**ID:** `litespec/core/language#name-a-boolean-as-a-predicate`

**Statement:** Every boolean field MUST read as a present-tense predicate that is true about the record when the value is true.

**Rationale:** A boolean without a prefix reads as a fact about the record: `applies` says the constraint applies, and `recorded` says the entry is recorded. An `is` or `has` prefix adds a word that says nothing the value does not already say. The predicate form keeps every boolean readable in a sentence about its record.

**Example:** Claimwick marks an applying constraint with `applies: true` instead of `isApplicable: true`.

**Reviewed for:** Which boolean fields read as something other than a predicate, or carry an `is` or `has` prefix? Look at the boolean fields the change adds.

### Name Each Date by What It Marks

**ID:** `litespec/core/language#name-each-date-by-what-it-marks`

**Statement:** Every date field MUST end in `By` for a deadline, in `On` for a recorded date, or carry a name that states what the date does.

**Rationale:** A deadline and a recorded date answer different questions, so their names differ: `<base verb>By` says when something ends, and `<verb>edOn` says when something happened. `effectiveFrom` opens a validity span, while `dueBy` and `plannedFor` hold a date or a milestone where the plan names one. The shape tells a reader which dates block a decision and which record that it happened.

**Example:** A Claimwick question carries `decideBy: 2026-11-01` and, once answered, `answeredOn` with the answer date.

**Checked by:** `litespec-maintain validate` emits no `FIELD_NAME_SHAPE` diagnostic for a date field.

### Write Each Kind of Text in Its Form

**ID:** `litespec/core/language#write-each-kind-of-text-in-its-form`

**Statement:** Every project text MUST use the form the text-kind table gives its kind.

**Rationale:** Each kind of text has one reader and one job: a requirement promises, an event reports, a runbook step instructs, and a question asks. One form per kind means the tense and voice tell the reader what the text does before a word is weighed. Record values stay in the third person, Intent chapter prose says `we` for the business, and nothing says `you`.

**Example:** A Claimwick requirement states "An expense above the threshold waits for its approver."

**Reviewed for:** Which texts miss the form of their kind? Look at the statements, steps, meanings, and questions the change adds, beside the text-kind table.

### Write Each Value in the Value Style

**ID:** `litespec/core/language#write-each-value-in-the-value-style`

**Statement:** Every `statement`, `meaning`, `rationale`, `*Because`, `*When`, `condition`, `failure`, `question`, and `title` value MUST use the value style the value-style table states.

**Rationale:** Values are read far more than they are written, in records, in the book, and in tool output. One written form per value means a period, a question mark, or a capital letter is never a styling choice but part of the value. The value-style table ends each row with the mechanical test the value meets.

**Example:** A Claimwick journey step states "The traveller pays for the booking."

**Checked by:** `litespec model check` reports no `value-invalid` finding for a record value, and `litespec-maintain validate` emits no `VALUE_STYLE` diagnostic for a template or example value.

### Write Quantities in Standard Units

**ID:** `litespec/core/language#write-quantities-in-standard-units`

**Statement:** Every quantity in a record MUST use an ISO 8601 date, an ISO 8601 duration, an amount with an ISO 4217 currency, or a number with its unit.

**Rationale:** Free-text quantities hide behind local habits: one record writes `7 years`, another writes `seven years`, and a check matches neither. ISO dates, ISO durations, and ISO currency codes give every quantity one written form across records and tools. Prose keeps numbers readable by writing each with its unit.

**Example:** Claimwick holds a room for `PT10M` and writes "95 percent of checkouts complete" in prose.

**Reviewed for:** Which quantities use free text or miss their unit? Look at the dates, durations, money values, and numbers the change adds.

### Name Each Record by Its Type's Name Form

**ID:** `litespec/core/language#name-each-record-by-its-types-name-form`

**Statement:** Every record name MUST use the name form the record-name table gives its type.

**Rationale:** A name in the word class of what the record is tells a reader the type before the identifier is opened: a verb opens a use case, a noun phrase opens a goal, and a past participle opens an event. One name form per type keeps listings scannable and citations writable by hand. The record-name table holds the Intent forms beside the Behavior and governance forms.

**Example:** Claimwick names the journey `submit-an-expense`, the use case `submit-expense`, and the setting `receipt-threshold`.

**Reviewed for:** Which record names miss the name form of their type? Look at the identifiers the change adds, beside the record-name table.

### Write in American English

**ID:** `litespec/core/language#write-in-american-english`

**Statement:** Every field name, enum value, and prose word MUST use American spelling.

**Rationale:** One spelling per word keeps each term defined once and checked once: `license` in one record and `licence` in another split every search and every listing. American spelling is the project spelling for field names, enum values, and prose alike. Each British form reads as a rejected word beside its American spelling.

**Example:** Claimwick writes `license` and `pseudonymize` in field names, values, and prose.

**Reviewed for:** Which words use British spelling? Look at the field names, enum values, and prose the change adds.

### Keep Names an Outside Standard Fixes

**ID:** `litespec/core/language#keep-names-an-outside-standard-fixes`

**Statement:** Every name an outside standard fixes MUST stay exactly as that standard spells it.

**Rationale:** A name an outside standard fixes belongs to that standard: renaming `given` to fit the word classes breaks every Gherkin reader. The grammar stays complete by exempting exactly these names instead of growing an exceptions list. Gherkin and CloudEvents names are the only exemptions.

**Example:** A Claimwick scenario keeps `given`, `when`, and `then`, which the word-class table gives no class.

**Reviewed for:** Which fixed names were renamed to fit the grammar? Look at the Gherkin fields and CloudEvents attributes the change touches.

### Name Each Finding Code by Subject and Problem

**ID:** `litespec/core/language#name-each-finding-code-by-subject-and-problem`

**Statement:** Every finding code MUST read as `<subject>-<problem>` with the problem drawn from the twelve problem words.

**Rationale:** A code in subject-problem form tells a reader what the finding is about and what is wrong with it before the message is opened. Twelve problem words, each with one meaning, keep codes comparable across tools: one word for absent, one for expired, and no synonyms. The problem-word table holds each word with its meaning and an example.

**Example:** A use case with no scenario takes the code `use-case-scenario-missing`.

**Reviewed for:** Which finding codes miss the subject-problem form or use a problem word outside the twelve? Look at the finding codes the change adds, beside the problem-word table.

### Write Each Finding Message as a Fact

**ID:** `litespec/core/language#write-each-finding-message-as-a-fact`

**Statement:** Every finding message MUST state one present-tense fact that opens with the subject title and its quoted identifier.

**Rationale:** A message that states a fact survives every reader: the terminal, the book, and the review queue all show what is true. Advice belongs in the finding table on the [model page](../tools/model.md), where it is written once beside the code instead of repeated in every occurrence. Analyzer diagnostics keep their `LS` numbers and follow the same message rule.

**Example:** A journey step with no serving use case reports `Journey step 'book-a-room#s3' is served by no use case.`

**Reviewed for:** Which finding messages instruct instead of stating a fact, or open without the subject and identifier? Look at the messages the change adds.

### Name Each Change Code with a Past Participle

**ID:** `litespec/core/language#name-each-change-code-with-a-past-participle`

**Statement:** Every change code in `litespec model diff` MUST read as `<subject>-<past participle>`.

**Rationale:** A diff compares two runs, so its codes read as what changed: a past participle says what the newer run added, removed, or weakened. The subject-problem shape keeps change codes comparable with finding codes. `refusal-added`, `rule-weakened`, and `requirement-narrowed` show the form.

**Example:** A refusal added between two runs diffs as `refusal-added`.

**Reviewed for:** Which change codes miss the past-participle form? Look at the diff codes the change adds.

### Name Commands as Verbs and Flags by What They Set

**ID:** `litespec/core/language#name-commands-as-verbs-and-flags-by-what-they-set`

**Statement:** Every subcommand MUST read as a verb and every flag as the mode or value it sets.

**Rationale:** A verb names what the command does, so `check` reads as an action before its arguments are weighed. A flag that names its mode or value reads the same way at every call site, and kebab-case keeps multiword flags writable without quotes. Stable exit codes let scripts tell a clean run from a blocking finding from bad arguments.

**Example:** The release check runs as `litespec model check --release` and exits 1 on a blocking finding.

**Reviewed for:** Which subcommands read as nouns, or which flags name something other than their mode or value? Look at the commands and flags the change adds.

## Conventions

None.

## Reference Example

This informative example demonstrates `litespec/core/language#name-each-field-by-its-word-class`, `litespec/core/language#write-each-value-in-the-value-style`, `litespec/core/language#name-each-record-by-its-types-name-form`, and `litespec/core/language#write-each-finding-message-as-a-fact`.

Claimwick holds booked rooms with the use case `hold-room`, which reads the setting `checkout-hold-window`. The requirement `requirement/checkout.room-held-during-checkout` states "A room the traveller starts to book is held for them for 10 minutes." When the hold lapses with no use case behind the step, the check reports "Requirement 'checkout.room-held-during-checkout' is fulfilled by no Behavior record."
