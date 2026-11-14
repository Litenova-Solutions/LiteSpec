# Record Language

**Stack:** any

**Concern:** records

## Intent

Record Language states one grammar and one tone for every name, every identifier and every value a project writes or a tool prints. Agents and authors apply it to record fields, values, identifiers, finding codes and tool names. The [Authoring Standard](authoring.md) states the prose of LiteSpec pages, and the [Writing Standard](writing.md) states the prose of project records, explanation files and documents; this page states the names, values, identifiers and codes of project records.

## Concepts

### The Eight Word Classes

Every field name belongs to one of eight word classes. The class decides the name's form, and the reading test tells whether a name sits in its class.

| Class | Form | Reading test | Examples |
|:---|:---|:---|:---|
| 1. Identity | fixed words | none | `type`, `id`, `title`, `terms`, `records` |
| 2. Link to many records | active present-tense verb | "journey serves goal" | `serves`, `fulfills`, `solves`, `advances`, `cites`, `governs`, `raises`, `reads`, `changes`, `covers`, `addresses` |
| 3. Link to one record in a role | singular noun naming the role | "the journey's actor is traveller" | `actor`, `fallback`, `runbook`, `result`, `host`, `refusal` |
| 4. Link where the target acts on this record | `<participle>By`, and `basedOn` | "the rule is enforcedBy the aggregate" | `basedOn`, `enforcedBy`, `releasedBy`, `computedBy`, `triggeredBy` |
| 5. Closed set of values | noun naming the dimension; `kind` only for the record's own subtype | "its mode is atomic" | `kind`, `mode`, `direction`, `trust`, `reach`, `lifecycle`, `level` |
| 6. Prose | singular noun for what the text is; plural noun for a list of sentences | "its statement is ..." | `statement`, `meaning`, `rationale`, `summary`, `conclusion`, `failure` |
| 7. Condition | `<participle or adjective>When` for a named condition; `condition` inside an entry | "it is frozenWhen ..." | `frozenWhen`, `visibleWhen`, `skippedWhen`, `reopenedWhen`, `removedWhen` |
| 8. Reason | `<participle>Because`, or `reason` alone | "it appliesBecause ..." | `appliesBecause`, `reason` |

Dates and booleans follow the classes: a recorded date ends in `On` (`readOn`, `expiresOn`); a validity start is `effectiveFrom`; a boolean is a present-tense predicate that is true about the record when `true` (`applies`, `secret`, `recorded`), with no `is` or `has` prefix.

### Kinds of Text

Every piece of text a project writes falls into one of these kinds. The kind decides its form; the layer does not.

| Kind of text | Form | Example |
|:---|:---|:---|
| What holds: requirement, rule, goal and problem statements, journey steps, result meanings | Present tense, active voice, third person, one complete sentence ending in a period, no modal words | "A room the traveller starts to book is held for them for 10 minutes." |
| What happened: event and integration-event meanings | Past tense, one sentence | "The buyer placed the order." |
| A state something is in: aggregate state names, proof words | Past participle or adjective | `Held`, `Paid`, `withdrawn`, `verified`, `stale` |
| What to do: runbook steps, procedure steps | Imperative, one action per step | "Restart the payment worker." |
| A condition: `*When`, `condition`, `skippedWhen` | A clause that completes "when ...", without the word | "the application links to no event" |
| A reason: `rationale`, `*Because`, departure `reason` | A clause that completes "because ...", without the word | "partners will not share the no-show risk" |
| A definition: `meaning` of a term, a field, a setting | A noun phrase starting with A, An or The, at most 25 words, no leading verb | "A confirmed reservation of one room for one stay." |
| A title | The record's name in Title Case, no period | `use-case/orders.place-order` is titled "Place Order" |
| A finding message | One present-tense sentence of fact | "Requirement 'booking.service-fee' is fulfilled by no Behavior record." |

Gherkin fields keep Gherkin practice: `given` is the starting state (present tense, or past tense for an earlier act), `when` is one act by a named actor (present tense), `then` is the observable outcome (present tense, without "should"). A result's `meaning` says what is now true (success) or what the caller is told (refusal).

Person: record values are third person. Intent chapter prose (The Situation, What We Do) may say "we" for the business. Nothing says "you". Capitalized modal words belong only to LiteSpec's own provisions; a project's record states a fact or a commitment as a fact. Voice, tense and person beyond this table follow the [Writing Standard](writing.md).

Quantities: dates are ISO 8601 dates, durations ISO 8601 durations (`P7Y`, `PT10M`), money is `{ amount, currency }` with an ISO 4217 code. How prose writes a number with its unit follows the [Writing Standard](writing.md).

### Value Style

Each value below takes the written form its row gives. The last column names the mechanical test the value meets.

| Field | Write it as | Test |
|:---|:---|:---|
| `statement` (requirement, rule) | One present-tense declarative sentence, third person, product words, ending in a period | Ends in a period; no modal word in lower case such as "should" |
| `statement` (goal) | The outcome as a present-tense fact about the world | Ends in a period |
| `statement` (vision) | Where the product heads, as a present-tense fact about the product once it is there | Ends in a period |
| `statement` (problem) | The pain as it happens today, present tense | Ends in a period |
| step `statement` | Actor first, present tense: "The traveller pays for the booking." | Starts with "The" or an actor title; ends in a period |
| `meaning` | A noun phrase starting with "A" or "An", no leading verb, at most 25 words | Starts with A, An or The; word count |
| `rationale`, `*Because` | A clause completing "because ..." | Does not start with "because" |
| `*When`, `condition` | A clause completing "when ..." | Does not start with "when" or "if" |
| `failure` | What the person experiences, present tense, never a code | Contains no refusal-code pattern |
| `title` | Title Case noun phrase, no period | Title Case; no final period |

### Names for Records

Each record type takes the name form its row gives, in the word class of what the record is.

| Type | Name form | Example |
|:---|:---|:---|
| goal | outcome noun phrase | `more-completed-bookings` |
| vision | the destination as a noun phrase | `group-bookings` |
| problem | the pain as a short clause | `rooms-sell-out-at-checkout` |
| feature | the offer as a noun phrase | `room-hold` |
| requirement | the assertion | `room-held-during-checkout` |
| journey | the task as a verb phrase | `book-a-room` |
| constraint | the rule's subject | `total-price-display` |
| research | the evidence subject | `partner-hold-limits` |
| decision | number and subject | `0001-room-hold-length` |
| use case | verb first | `hold-room` |
| module | plural noun of the business area | `orders` |
| aggregate | singular noun | `order`, `ticket-resale` |
| rule | the assertion, keeping its negation word | `order.checkout-commits-with-holds`, `expense.approver-is-not-submitter` |
| event | `<aggregate>.<past participle>` | `order.placed` |
| integration event | `<aggregate>-<past participle>` | `order-placed` |
| reaction | `<verb>-<what>-on-<fact>` | `issue-credit-note-on-refund-succeeded` |
| job | `<verb>-<what>`, with the verb of the use case it runs | `expire-lapsed-ticket-resales` |
| aggregate state | past participle or adjective | `withdrawn`, `Listed` |
| notification | `<subject>-<event>` noun phrase | `order-confirmation` |
| setting | noun phrase naming the value | `checkout-hold-window` |
| data class | noun phrase naming the data | `buyer-contact` |
| entry point, external system | noun naming the door or system | `webshop`, `payout-bank` |
| external operation | verb first | `create-charge` |
| screen | `<entry-point>.<noun>` | `webshop.basket` |
| runbook | verb first, what the reader does | `clear-stuck-refunds` |
| departure | noun phrase of what it sets aside | `retry-limit-for-payments` |
| term | the singular word itself | `booking` |

A record name is not its title. Every record title takes the title form of its type on the [Writing Standard](writing.md) page.

### Spelling and Fixed Names

American spelling is the project spelling for field names and enum values: `license` rather than `licence`, and `pseudonymize` rather than `pseudonymise`. Each British form reads as a rejected word beside its American spelling. Prose spelling follows the [Writing Standard](writing.md).

Names an outside standard fixes stay exactly as that standard spells them. Gherkin fixes `given`, `when`, and `then`, and CloudEvents fixes its attribute names; these are the only names outside the word classes.

### Identifier Forms

An identifier names its record type first, then the record name, with the owning home in between when the type has one. The [Project Model](model.md) states the form each type uses, with one row per type and one example per row. A rule home is an aggregate name or a module name. A result or scenario home is a use-case name, and an event home is an aggregate name. A use-case name is unique across the project, so a result cites only that name; an aggregate name is unique across the project, so a rule cites only that name. A reaction, job, notification and setting home is the module that owns it. An integration event has no module home. A rule name keeps the negation its statement makes. A journey step identifier is `journey/<name>#<step-id>`, and a derived generic result is `result/<use-case>.forbidden` or `result/<use-case>.invalid-input`; scenarios may point at derived results, which are never written as records.

### Name Rules

The name is the last segment of the identifier. Every name follows these rules:

- Write one to six lowercase words joined by hyphens.
- Place digits inside a word; a name of digits alone breaks the rules.
- Count the decision number as a word.
- Let a person choose the name; no tool builds one by stripping words from a sentence.

Use case and aggregate names stay unique across the project.

### Wire Codes

A wire code is a value a client reads and branches on, such as a refusal code or an audit action code. Wire codes are not identifiers: they name no record, they keep one shape and they stay unchanged once clients read them.

### Resolution

Resolution means every identifier citation names a record that exists. The model check fails an unresolved citation, so a citation points only at a record someone wrote. Shared provisions keep their published `litespec/<page>#<slug>` form.

### Finding Codes and Tool Names

A finding code reads as `<subject>-<problem>`: the subject is a record type or field as the glossary spells it, and the problem is one of the words in the problem-word table below, each with one meaning.

| Problem | Meaning | Example |
|:---|:---|:---|
| `missing` | a required thing is absent | `use-case-scenario-missing` |
| `unknown` | a name matches nothing declared | `link-unknown` |
| `undeclared` | used without a declaration | `erasure-undeclared` |
| `unrecorded` | true in code but written in no record | `behavior-unrecorded` |
| `unimplemented` | declared but built nowhere | `use-case-unimplemented` |
| `unreached` | built but reached by no flow | `use-case-unreached` |
| `unverified` | a use case holds scenarios with no passing proof | `use-case-unverified` |
| `drift` | the built thing differs from its record | `setting-key-drift` |
| `invalid` | a value breaks its form | `field-invalid` |
| `duplicate` | the same thing is declared twice | `identifier-duplicate` |
| `expired` | the date passed | `departure-expired` |
| `orphaned` | a file stands without its partner | `explanation-orphaned` |
| `bare` | a reference bypasses its declared key | `source-link-bare` |
| `unused` | a declared thing is cited nowhere | `source-unused` |
| `break` | a contract the base published no longer holds | `contract-break` |

A finding message states one present-tense fact: it opens with the subject title and its quoted identifier and says what is true, not what to do. What to do lives in the finding table on the [model page](../tools/model.md), in a column for it. Analyzer diagnostics keep their `LS` numbers and follow the same message rule.

Subcommands are verbs (`check`, `format`, `render`, `query`). Flags are kebab-case: a mode flag names its mode (`--release`, `--staged`, `--dry-run`), and a value flag names its value (`--since <ref>`). Exit codes stay 0 for clean, 1 for blocking, and 2 for bad arguments. Book labels come from the Links table's reads-as phrases and the glossary only.

## Standards

### Name Each Field by Its Word Class

**ID:** `litespec/core/language#name-each-field-by-its-word-class`

**Strength:** required

**Statement:** Every record field MUST use the name its word class gives it in the word-class table.

**Rationale:** One grammar with no exceptions list means a reader who learned `cites` on a journey reads `cites` on a requirement without a second look. Each class answers one question about the field: whether it identifies, links, classifies, narrates, conditions, or reasons. Only names an outside standard fixes stand outside this grammar. Settled by [decision/0006-one-field-naming-grammar](decision/0006-one-field-naming-grammar) on the evidence in [research/field-naming-evidence](research/field-naming-evidence) and [research/cross-layer-trace-evidence](research/cross-layer-trace-evidence).

**Example:** The Claimwick journey `submit-an-expense` cites its goal in `cites` and states each step in `statement`.

**Reviewed for:** Which fields break their word class? Look at the field names the change adds, beside the word-class table.

### Name a Boolean as a Predicate

**ID:** `litespec/core/language#name-a-boolean-as-a-predicate`

**Strength:** required

**Statement:** Every boolean field MUST read as a present-tense predicate that is true about the record when the value is true.

**Rationale:** A boolean without a prefix reads as a fact about the record: `applies` says the constraint applies, and `recorded` says the entry is recorded. An `is` or `has` prefix adds a word that says nothing the value does not already say. The predicate form keeps every boolean readable in a sentence about its record.

**Example:** Claimwick marks an applying constraint with `applies: true` instead of `isApplicable: true`.

**Reviewed for:** Which boolean fields read as something other than a predicate, or carry an `is` or `has` prefix? Look at the boolean fields the change adds.

### Name Each Date by What It Marks

**ID:** `litespec/core/language#name-each-date-by-what-it-marks`

**Strength:** required

**Statement:** Every date field MUST end in `On` for a recorded date or carry a name that states what the date does.

**Rationale:** A recorded date says when something happened or until when it holds: `readOn` says when an outside source was read, and `expiresOn` says when a departure ends. `effectiveFrom` opens a validity span. The shape tells a reader which dates record the world and which open a span.

**Example:** A Claimwick constraint carries `readOn` on each source with the date the source was read, and a departure carries `expiresOn` with the date it ends.

**Checked by:** `litespec-maintain validate` emits no `FIELD_NAME_SHAPE` diagnostic for a date field.

### Write Each Kind of Text in Its Form

**ID:** `litespec/core/language#write-each-kind-of-text-in-its-form`

**Strength:** required

**Statement:** Every project text MUST use the form the text-kind table gives its kind.

**Rationale:** Each kind of text has one reader and one job: a requirement promises, an event reports, and a runbook step instructs. One form per kind means the tense and voice tell the reader what the text does before a word is weighed. Record values stay in the third person, Intent chapter prose says `we` for the business, and nothing says `you`.

**Example:** A Claimwick requirement states "An expense above the threshold waits for its approver."

**Reviewed for:** Which texts miss the form of their kind? Look at the statements, steps, and meanings the change adds, beside the text-kind table.

### Write Each Value in the Value Style

**ID:** `litespec/core/language#write-each-value-in-the-value-style`

**Strength:** required

**Statement:** Every `statement`, `meaning`, `rationale`, `*Because`, `*When`, `condition`, `failure`, and `title` value MUST use the value style the value-style table states.

**Rationale:** Values are read far more than they are written, in records, in the book, and in tool output. One written form per value means a period or a capital letter is never a styling choice but part of the value. The value-style table ends each row with the mechanical test the value meets. Settled by [decision/0011-one-grammar-for-names-and-values](decision/0011-one-grammar-for-names-and-values) on the evidence in [research/naming-grammar-evidence](research/naming-grammar-evidence).

**Example:** A Claimwick journey step states "The traveller pays for the booking."

**Reviewed for:** Which statement, meaning, rationale, condition, failure, or title values miss the value style of their row? Look at the values the change adds, beside the value-style table.

### Write Quantities in Standard Units

**ID:** `litespec/core/language#write-quantities-in-standard-units`

**Strength:** required

**Statement:** Every quantity in a record MUST use an ISO 8601 date, an ISO 8601 duration, an amount with an ISO 4217 currency, or a number with its unit.

**Rationale:** Free-text quantities hide behind local habits: one record writes `7 years`, another writes `seven years`, and a check matches neither. ISO dates, ISO durations, and ISO currency codes give every quantity one written form across records and tools. Prose keeps numbers readable by writing each with its unit.

**Example:** Claimwick holds a room for `PT10M` and writes "95 percent of checkouts complete" in prose.

**Reviewed for:** Which quantities use free text or miss their unit? Look at the dates, durations, money values, and numbers the change adds.

### Name Each Record by Its Types Name Form

**ID:** `litespec/core/language#name-each-record-by-its-types-name-form`

**Strength:** required

**Statement:** Every record name MUST use the name form the record-name table gives its type.

**Rationale:** A name in the word class of what the record is tells a reader the type before the identifier is opened: a verb opens a use case, a noun phrase opens a goal, and a past participle opens an event. One name form per type keeps listings scannable and citations writable by hand. The record-name table holds the Intent forms beside the Behavior and governance forms.

**Example:** Claimwick names the journey `submit-an-expense`, the use case `submit-expense`, and the setting `receipt-threshold`.

**Reviewed for:** Which record names miss the name form of their type? Look at the identifiers the change adds, beside the record-name table.

### Write in American English

**ID:** `litespec/core/language#write-in-american-english`

**Strength:** required

**Statement:** Every field name and enum value MUST use American spelling.

**Rationale:** One spelling per word keeps each term defined once and checked once: `license` in one record and `licence` in another split every search and every listing. American spelling is the project spelling for field names and enum values; prose spelling follows the [Writing Standard](writing.md). Each British form reads as a rejected word beside its American spelling.

**Example:** Claimwick writes `license` and `pseudonymize` in field names, values, and prose.

**Reviewed for:** Which words use British spelling? Look at the field names, enum values, and prose the change adds.

### Keep Names an Outside Standard Fixes

**ID:** `litespec/core/language#keep-names-an-outside-standard-fixes`

**Strength:** required

**Statement:** Every name an outside standard fixes MUST stay exactly as that standard spells it.

**Rationale:** A name an outside standard fixes belongs to that standard: renaming `given` to fit the word classes breaks every Gherkin reader. The grammar stays complete by exempting exactly these names instead of growing an exceptions list. Gherkin and CloudEvents names are the only exemptions.

**Example:** A Claimwick scenario keeps `given`, `when`, and `then`, which the word-class table gives no class.

**Reviewed for:** Which fixed names were renamed to fit the grammar? Look at the Gherkin fields and CloudEvents attributes the change touches.

### Name Each Finding Code by Subject and Problem

**ID:** `litespec/core/language#name-each-finding-code-by-subject-and-problem`

**Strength:** required

**Statement:** Every finding code MUST read as `<subject>-<problem>` with the problem drawn from the problem-word table.

**Rationale:** A code in subject-problem form tells a reader what the finding is about and what is wrong with it before the message is opened. A closed set of problem words, each with one meaning, keeps codes comparable across tools: one word for absent, one for expired, and no synonyms. The problem-word table holds each word with its meaning and an example.

**Example:** A use case with no scenario takes the code `use-case-scenario-missing`.

**Reviewed for:** Which finding codes miss the subject-problem form or end in a word the problem-word table does not hold? Look at the finding codes the change adds, beside the problem-word table.

### Write Each Finding Message as a Fact

**ID:** `litespec/core/language#write-each-finding-message-as-a-fact`

**Strength:** required

**Statement:** Every finding message MUST state one present-tense fact that opens with the subject title and its quoted identifier.

**Rationale:** A message that states a fact survives every reader: the terminal and the book both show what is true. Advice belongs in the finding table on the [model page](../tools/model.md), where it is written once beside the code instead of repeated in every occurrence. Analyzer diagnostics keep their `LS` numbers and follow the same message rule.

**Example:** A journey step with no serving use case reports `Journey step 'book-a-room#s3' is served by no use case.`

**Reviewed for:** Which finding messages instruct instead of stating a fact, or open without the subject and identifier? Look at the messages the change adds.

### Name Commands as Verbs and Flags by What They Set

**ID:** `litespec/core/language#name-commands-as-verbs-and-flags-by-what-they-set`

**Strength:** required

**Statement:** Every subcommand MUST read as a verb and every flag as the mode or value it sets.

**Rationale:** A verb names what the command does, so `check` reads as an action before its arguments are weighed. A flag that names its mode or value reads the same way at every call site, and kebab-case keeps multiword flags writable without quotes. Stable exit codes let scripts tell a clean run from a blocking finding from bad arguments.

**Example:** The release check runs as `litespec model check --release` and exits 1 on a blocking finding.

**Reviewed for:** Which subcommands read as nouns, or which flags name something other than their mode or value? Look at the commands and flags the change adds.

### Follow the Identifier Form of Each Record Type

**ID:** `litespec/core/language#follow-the-identifier-form-of-each-record-type`

**Strength:** required

**Statement:** A record identifier MUST follow the form the model states for its record type.

**Rationale:** A reader who meets `use-case/spending.submit-expense` knows it is a use case in the spending module without opening it. If two types shared one form, a citation needs its surrounding page to make sense, and search returns the wrong records.

**Example:** `event/expense.submitted` names an event of the expense aggregate.

**Checked by:** `litespec model check` reports `identifier-invalid` for an identifier outside the form its type uses.

### Name Records with One to Six Words

**ID:** `litespec/core/language#name-records-with-one-to-six-words`

**Strength:** required

**Statement:** A name MUST carry one to six lowercase hyphenated words.

**Rationale:** A short hyphenated name reads aloud in review and stays stable when the wording of the record changes. A long name gets shortened differently at each citation, and the shortcuts drift into competing names for one record.

**Example:** `decision/0001-receipt-threshold` counts its number as one word.

**Checked by:** `litespec model check` reports `identifier-invalid` for a name outside one to six lowercase hyphenated words.

### Use Every Identifier Once

**ID:** `litespec/core/language#use-every-identifier-once`

**Strength:** required

**Statement:** A record identifier MUST name exactly one record.

**Rationale:** An identifier used twice names two records at once, so every citation of it points both ways. One use per identifier keeps each citation single-valued.

**Example:** Two records named `use-case/spending.submit-expense` leave the model check reporting the duplicate on the second record.

**Checked by:** `litespec model check` reports `identifier-duplicate` for an identifier used twice.

### Keep Wire Codes Stable

**ID:** `litespec/core/language#keep-wire-codes-stable`

**Strength:** required

**Statement:** A wire code MUST stay unchanged once clients read it.

**Rationale:** Wire codes travel to deployed clients, which branch on their exact values. Renaming a code that clients already read breaks every deployed reader that compares the old value, with no record the reader opens to learn the new one.

**Reviewed for:** Which wire codes in the contract break the values deployed clients branch on? Look at the wire codes beside the deployed clients.

### Resolve Every Identifier Citation

**ID:** `litespec/core/language#resolve-every-identifier-citation`

**Strength:** required

**Statement:** Every identifier citation MUST resolve to a written record.

**Rationale:** A citation is a promise that a record exists at the other end. A citation that names nothing sends the reader searching for a record nobody wrote, so the model check fails it.

**Checked by:** `litespec model check` reports `reference-unknown` for a citation naming no written record.

### Cite Shared Provisions in Their Published Form

**ID:** `litespec/core/language#cite-shared-provisions-in-their-published-form`

**Strength:** required

**Statement:** A shared provision citation MUST use its published identifier.

**Rationale:** A shared provision is cited across projects, so its identifier looks the same everywhere. The published `litespec/<page>#<slug>` form stays fixed, while project records use the type forms above.

**Checked by:** `litespec-maintain validate` emits no `ID_UNKNOWN_REFERENCE` diagnostic.

### Exclude Declared Content Paths from the ASCII Check

**ID:** `litespec/core/language#exclude-declared-content-paths-from-the-ascii-check`

**Strength:** required

**Statement:** An ASCII check MUST exclude only the natural-language content paths that the project record declares.

**Rationale:** A product whose subject is another language carries that language in its content files, where a diacritic is part of a word rather than a styling choice. Excluding only the declared content paths keeps the check strict everywhere else: documents about that content stay in scope, so styled prose filed beside content still meets the check.

**Reviewed for:** Which non-ASCII prose sits outside the paths the project record declares? Look at the content files beside the declaration.

### Record the Project Vocabulary as Data

**ID:** `litespec/core/language#record-the-project-vocabulary-as-data`

**Strength:** required

**Statement:** A consumer MUST record each term, its rejected synonyms, the scope of each rejection, and its mannered terms in the project glossary satisfying `schemas/glossary.schema.yaml`.

**Rationale:** A glossary column headed "Avoid" speaks to a reader and to nothing else: no tool can check it. A rule held as data reaches the reader and the validator alike, so the same rejection appears in prose guidance and in diagnostics.

**Example:** A rejection carries the scope that binds it, because a word that is right in one module is wrong in another.

```json
{ "id": "term/customer", "term": "customer", "meaning": "A person or company that buys.",
  "rejected": [{ "word": "client", "scope": "^domain/modules/orders/",
    "reason": "client names the API consumer" }] }
```

**Checked by:** `litespec model check` reports `glossary-invalid` for a glossary that fails the glossary schema.

### Reject a Recorded Synonym Inside Its Scope

**ID:** `litespec/core/language#reject-a-recorded-synonym-inside-its-scope`

**Strength:** required

**Statement:** Authored prose MUST NOT use a synonym that the project glossary rejects for the scope the page sits in.

**Rationale:** Term drift is invisible one page at a time and plain across a module: each page looks fine while the project slowly speaks two languages. The expensive case is a synonym that is a defined term elsewhere, because one word then names two things and every reader guesses which one.

**Reviewed for:** Which words on the page are synonyms the project glossary rejects for the scope the page sits in?

### State Mannered Terms Literally

**ID:** `litespec/core/language#state-mannered-terms-literally`

**Strength:** required

**Statement:** Authored prose MUST NOT use a mannered term the project glossary lists.

**Rationale:** A metaphor or stock phrase reads smoothly while hiding what the project means: each reader pictures a different act behind the same flourish. The glossary lists each mannered term with the literal phrase that carries its meaning, so the check points at the flourish and names the plain words to reach for.

**Example:** The glossary lists `{ "term": "leverage", "instead": "use" }`, so prose reads "use the retry queue" rather than "leverage the retry queue".

**Reviewed for:** Which mannered terms on the page stand where the glossary asks for the literal word?

### Exclude the Compound That Names Another Concept

**ID:** `litespec/core/language#exclude-the-compound-that-names-another-concept`

**Strength:** required

**Statement:** A glossary rejection MUST list, in `except`, every compound in which a rejected word names a concept other than the one the rejection is about.

**Rationale:** A rejection matches a word, and a word can belong to more than one term: Claimwick rejects `docket` in favor of `receipt` for payment proof while a docket still names the shipping list. Without the `except` list, the check would force the project to rename an unrelated concept or drop a sound rejection.

**Example:** The exemption covers the compound only, so the bare word stays rejected.

```json
{ "word": "docket", "reason": "one word for a payment receipt and a shipping list",
  "scope": "^.litespec/domain/modules/spending/", "except": ["shipping docket"] }
```

**Reviewed for:** Which compounds use a rejected word to name another concept without being listed in the rejection's `except`?

### Review Each Term for Its Lookalikes

**ID:** `litespec/core/language#review-each-term-for-its-lookalikes`

**Strength:** required

**Statement:** A project MUST resolve each pair of terms that differ only in hyphenation, spelling variant, plurality, or a compound split.

**Rationale:** Two spellings of one concept split every search and every listing, and a reader cannot tell whether they name one concept or two. One spelling per concept keeps each term defined once and checked once. A hyphenated pair whose first part is a function word is two words side by side, not one compound split in two, so the check skips it: `a`, `an`, `as`, `at`, `by`, `in`, `of`, `on`, `or`, `to`, and `with`.

**Example:** The notification `send-receiptreminder-confirmation` and the reaction `send-receipt-reminder-on-expense-submitted` name one concept with two spellings; the project picks one and renames the other. `send-on-line-notice` beside `send-online-notice` is not reported, since `on` opens no compound.

**Reviewed for:** Which terms differ from another term only in hyphenation, spelling, plurality, or a compound split? Look at the terms the change adds, beside the glossary.

### Use the Declared Record Identifier Grammar

**ID:** `litespec/core/language#use-the-declared-record-identifier-grammar`

**Strength:** required

**Statement:** A consumer record identifier MUST be `type/home.name` in kebab-case, using the form the Project Model states for its type.

**Rationale:** The type names the record kind and `home.name` names the owning module or set, so an identifier resolves with no lookup table. One grammar for every record kind keeps identifiers writable by hand and checkable by tool. The [Project Model](model.md) states the form each type uses, and the Identifier Forms section above states the name rules and checks.

**Example:** `reaction/spending.send-receipt-reminder-on-expense-submitted` is a reaction owned by the spending module, and `journey/submit-and-reimburse#s2` is the second step of a journey.

**Checked by:** `litespec model check` reports `identifier-invalid` for a record identifier outside the declared grammar.

## Conventions

None.

## Reference Example

This informative example demonstrates `litespec/core/language#name-each-field-by-its-word-class`, `litespec/core/language#write-each-value-in-the-value-style`, `litespec/core/language#name-each-record-by-its-types-name-form`, `litespec/core/language#write-each-finding-message-as-a-fact` and `litespec/core/language#follow-the-identifier-form-of-each-record-type`.

Claimwick holds booked rooms with the use case `use-case/booking.hold-room`, which reads the setting `setting/booking.checkout-hold-window`. The requirement `requirement/room-hold.room-held-during-checkout` states "A room the traveller starts to book is held for them for 10 minutes." When the hold lapses with no use case behind the step, the check reports "Requirement 'room-hold.room-held-during-checkout' is fulfilled by no Behavior record."
