# Standardization: Specification

**Created:** 2026-10-31, from the review and discussion of 2026-10-30 and 2026-10-31.
**Baseline:** LiteSpec `v3` at `c9b2725`. Finding ids (`I1` to `I49` Intent, `B1` to `B41` Behavior, `X1` to `X25` seams) refer to `inputs/session-findings.tsv`; their status on the baseline is in `inputs/findings-verified.tsv`.

Order: Part 0 decides, Part A reshapes the Intent hierarchy, Part B sets the language standard for every layer, Part C closes the Intent gaps, Part D the Behavior gaps, Part E the seams, Part F updates the teaching material, Part G migrates NightHall, Part H lists the records this release adds, and Parts I to L carry the design changes for Behavior, Implementation, Proof and Governance (`proposals-lower-layers.md` explains each with examples).

## Part 0: Owner Direction and Decisions

### Direction

On 2026-10-31 the owner approved the Intent simplification, the gap and inconsistency fixes, and a standard grammar for field names and values, and asked for the recommendations below to be part of the plan. Each decision stands unless the owner overrides it before wave 1.

### Decisions

| Id | Decision | Replaces or changes |
|:---|:---|:---|
| D1 | The requirement container becomes a record type, `topic`, identifier `topic/<name>`, page `docs/intent/<area>/<topic>.md`. The area is a folder only, never a record. A requirement's identifier `requirement/<topic>.<name>` names its topic, and the topic is its parent by identifier containment, as an aggregate holds its rules. A requirement whose topic segment differs from its page's name is an `identifier-form` finding. | `requirement-page` page style; requirement origin as parent (decision 0002, `litespec/core/model#give-every-record-one-parent`) |
| D2 | A requirement names its reasons in one list, `cites`, holding any of goal, problem, feature, constraint, research and decision, in no order of precedence. | `features`, `basedOn`, and a deciding decision as origin |
| D3 | A requirement that cites nothing is `requirement-uncited`: tier A, tier G when the requirement carries `risk`. | none (new check) |
| D4 | Only Behavior records fulfil a requirement: rule, use case, reaction, job, notification and setting. A journey names the requirements it shows in `cites`. | journey `fulfills`; `CheckFulfilment` counting journeys |
| D5 | A constraint's What It Requires section is a generated region listing the requirements that cite it. A constraint with `applies: true` that no requirement cites is `constraint-uncited`, tier G. | handwritten What It Requires |
| D6 | Requirement `kind: business` becomes `kind: policy`. Constraint `kind` gains `legal`. | `business` |
| D7 | One home for "no": the product's `exclusions` (was `neverDoes`) for product-wide limits, and a refused feature for a declined offer. The requirement page's Refusals section is removed. | `neverDoes`; Refusals section |
| D8 | Journeys walk a person: system journeys are removed and scheduled promises become requirements that jobs fulfil. A journey drops `aim`. A step's sentence is `statement` (was `says`) and its failure text is `failure` (was `ifItGoesWrong`). | system journeys; `aim`; `says`; `ifItGoesWrong` |
| D9 | One naming grammar of eight word classes (Part B) with no exceptions list. Only names an outside standard fixes are exempt: Gherkin `given`, `when`, `then`, and the CloudEvents attributes. | `litespec/core/model#name-each-field-for-one-concept` text; decision 0006 |
| D10 | `cites` is the one verb for a reason link everywhere: requirement, journey, consistency `because`, query `wideningBecause`, retention row `intent`. | `basedOn`, `because`, `wideningBecause`, `intent` |
| D11 | A boolean is a present-tense predicate that is true about the record when `true` (`applies`, `secret`, `recorded`); no `is` or `has` prefix. | none |
| D12 | String mini-languages become objects: `none (on-demand)`, `none (reason)`, `impossible (reason)`, `never (cost)`, `bounded (<duration>)`, `advisory`, `call <operation>`, `via integration-event/...`, `operator: <who, via runbook/..., due <duration>>`, `retry (n, backoff) then dead-letter`. | the string patterns in `record.schema.json` |
| D13 | A use case names the settings it reads in `reads`; the setting's `readBy` is removed and computed as an inverse. | setting `readBy`; decision 0007 |
| D14 | American spelling in field names, enum values and prose: `license`, `pseudonymize`. | `licence`, `pseudonymise` |
| D15 | `ballot` stays, defined in the glossary as "the questions a decider answers together". | none |
| D16 | A question carries `question`, `decider`, `decideBy`, `ballot`, `recommendation` (was `recommended`), and once answered `answer`, `answeredBy`, `answeredOn`. `ballot` is required; `recommendation` is required when the record carries no `risk`. The question's `tier` is removed and derived from the record's `risk`. An unanswered question is open. | `recommended`; question `tier`; no answer fields |
| D17 | `when` keeps only its Gherkin meaning. An exception entry is `{ condition, fallback }`; a process step's guard is `guard`. | `except.when`; process step `when` |
| D18 | The value style (Part B, Section 3) is a set of provisions, with its mechanical half checked. | none |
| D19 | Only a question auto-approves after its `decideBy` plus the veto window, recorded as `answeredBy: auto (veto window)` on the question. No record carries `approvedBy: auto (veto window)`. | `intent.md:50`, `model.md:176` wording |
| D20 | A decision records its approval in `approvedBy`, `approvedOn` and `approvedHash` only, and names its choice in `choice`. Options carry `pros` and `cons`. The reopening condition is `reopenedWhen`. The decision template carries the approval fields. | `decidedOn`, `chosen`, `for`, `against`, `changeWhen` |
| D21 | Explain before prescribing: `docs/core/intent.md` Concepts opens with a table of the questions Intent answers, with columns Question, Record, Write It When, Skip It When and Familiar As. The glossary defines decider, record layer, risk tier, check level and ballot. | none |
| D22 | "Founder" is retired from the core pages in favour of "decider", the owner named in `decider`; the founder is one decider a project may name. | "founder" as a role word |
| D23 | Check tiers G, S and A are renamed check levels; "tier" means only the risk tier (1 or 3). | "tier" for checks |
| D24 | Everything lands in 3.0.0, which is not yet tagged, with no aliases or migration code in LiteSpec. NightHall migrates in the last wave. | none |

### Decisions for the Other Layers (2026-10-31)

The owner approved the proposals for the Behavior, Implementation, Proof and Governance layers (`proposals-lower-layers.md`, with the revisions the largest consumer forced), with one standing direction: approval stays loose, because LiteSpec serves one-person teams.

| Id | Decision | Replaces or changes |
|:---|:---|:---|
| D25 | Approval stays loose. An agent's reading counts as the tier 1 second reading; a project whose owners list holds one name needs no second reviewer; `review.securityReviewer` is optional and the security queue is a list to read, not a gate. The pages say so. Two bugs are fixed: single-record approval promotes the record's status, and re-approval writes the new approver and date. | `release.md:397-405`, `:257`; `scope.md:164`; `project-file.md:117`; `Approver.cs:115-125` |
| D26 | One `exception` record replaces deviation, known gap, `deferredProvisions` and `check.phase: frozen`: `kind` (`departure` or `gap`), `covers` (a provision or finding codes), `subject` or `paths`, `reason`, `owner`, `status`, `expiresOn`. It covers only what it names, applies once approved (one person may approve), and `exception-expired` reports it after its date. | `deviation`, `known-gap`, two project settings |
| D27 | One `automation` record replaces reaction and job, with `triggeredBy` of `event`, `schedule` or `manual`. | `reaction`, `job` |
| D28 | The aggregate kind `process` becomes `saga`: a coordinator whose steps run use cases on other aggregates, each with its compensation. A record with only a lifecycle is an `entity`. "Process manager" is the glossary alias. | `kind: process` |
| D29 | An entry point is a door from outside only (`browser`, `api`, `webhook`, `device`, `cli`, `mcp`, `served-content`). The door states how a caller authenticates (`auth`); the use case states who may act (`grant`, `tenant`). | entry-point kinds `reaction`, `job` |
| D30 | An atomic write across aggregates stays allowed through an `atomicity` rule that `binds` the aggregates; the use case's `changes` must equal that group; a group above 4 aggregates reports at tier A as a saga candidate. `best-effort` is removed. | BP1 as first proposed; `best-effort` |
| D31 | Use-case inputs and integration-event payload fields carry `type` (`text`, `integer`, `decimal`, `money`, `boolean`, `date`, `date-time`, `duration`, `email`, `choice`, `id` with `of`) and `required`; an integration event may name `dataschema`. Money values are `{ amount, currency }`. | untyped inputs and payloads |
| D32 | Effect verbs are `runs`, `sends`, `emits`, `calls`. A manual job is `triggeredBy: manual`; a process driver run by a person is `person`; the auth kind `anonymous` is `none`. | `issues`, `publishes`; three meanings of operator; two of anonymous |
| D33 | `pseudonymize` keeps the class personal, with its basis, and counts in `risk-without-acceptance`; `anonymize` is added; each data class and retention row states `purpose`; `period` is an ISO 8601 duration; the retention schedule claims only the time limits of a record of processing. Counsel confirms the wording. | `pseudonymise`; free-text periods; the Article 30 claim |
| D34 | A refusal code is retired, never renamed. A scenario names the rules it shows in `exercises`; a use case may hold a shared `background`. | none |
| D35 | Proof state lives in `docs/proof/proofs.json`, committed, holding each scenario's hash and its test body's hash at the last passing run; stale when either changes. Browser outcomes are read. | `.model/proofs.json`; browser proofs never passing |
| D36 | A finding blocks or reports by its record's own status; an inherited draft shows as a label. | effective status silencing approved records |
| D37 | Loose daily, strict at release: `use-case-unverified` reports by default; `litespec model check --release` makes the release-readiness items block; `litespec model check --since <ref>` blocks only on what changed since `<ref>`. | release gate by reading only |
| D38 | Four proof words: verified (use case), met (requirement), tested (a passing test with an assertion), exercised (journey step). | two meanings of verified |
| D39 | Finding codes follow `<subject>-<problem>` with twelve problem words (Part B, B8); an unknown code fails the build; the book renders `model.json` and judges nothing itself. | synonym codes; book-side judgements |
| D40 | Release hygiene and failing loud: the analyzer pin matches the analyzer project and every cited LS code ships; an unknown layer name is reported; Worker is a layer; `reader-unavailable` blocks when `CI` is set. | silent fail-open paths |
| D41 | Mark style belongs to the language (C#: `xml` or `attribute`; TypeScript: typed constant; operations files: comment), read by one parser; a backend mark must match its folder; marks and handlers are recognized by symbol; a state-changing aggregate method needs a mark. | one style per project; name matching |
| D42 | The `**ID:**` line is a provision's identity; a heading may change without changing it. | ids derived from headings |
| D43 | Size-aware code rules: five projects, property tests, model-based tests and mutation thresholds become Defaults by size; every MUST names a real check or says Reviewed for; one page, Write Code for a Use Case, answers where code goes. | size-blind MUSTs |
| D44 | One language standard for every layer (Part B) lives on a new core page, `docs/core/language.md`; `authoring.md` keeps LiteSpec's own page prose and links to it. | `litespec/core/model#name-each-field-for-one-concept` |
| D45 | A journey walks one actor toward one goal; a step names another actor only at a handoff; a walk with several goals splits into several journeys. | journeys of many actors |
| D46 | The risk value `door` becomes `physical-access`; each risk value is defined in the glossary; "task authorizer" is defined; governance is a group of records beside the four layers; an agent skips the model query only when the change touches no record with `risk` or an approval. | `door`; undefined terms; `agent.md:71` |

## Part A: The Intent Hierarchy

### A1. Target structure

```text
product
  goal ... journey (serves)            journey cites requirements it shows
  problem ... feature (solves)         feature advances goals
  topic ... requirement (contained)    requirement cites its reasons
  constraint, research, decision, actor, term (product is parent)
```

The eleven record types stay; `topic` is added (D1). Each type answers one question:

| Record | Question | Write it when | Skip it when |
|:---|:---|:---|:---|
| product | What is the product, for whom, and what will it never do? | Always, once | Never |
| actor | Who or what touches the product? | Someone appears in a problem, a journey or the audience | The "actor" is a role; roles are Behavior |
| goal | Which number in the world do we want to move? | You can name what you count | The system controls it directly: that is a quality requirement |
| problem | What pain do people have today? | You can describe it without a solution, with evidence | Only one solution is imaginable: it is a feature |
| feature | What do we offer against a problem, and when? | Scope and timing need tracking | The commitment is permanent and no offer carries it |
| topic | Which requirements share one story? | Always, for requirements | Never |
| requirement | What exactly must hold? | Always | Never |
| journey | Can a person get from start to finish? | A task crosses several actions | It is one action |
| constraint | Which outside rule binds us? | An outside party can hold us to it | It is our own choice: a decision |
| research | Which outside fact did we rely on, and when was it true? | A choice rests on it and it could change | It binds us: a constraint |
| decision | Which option did we choose, and what reopens it? | There were real options | There was nothing to choose |
| term | Which word means what? | A word could mean two things | Never skipped for a domain word |

### A2. Work items

| Id | Change | Files |
|:---|:---|:---|
| A2.1 | Add the `topic` type: identifier, home, page sections (Summary, The Situation, What We Do, Why It Is Better, Example, Costs and Risks, How Sure We Are, Position), `reviewBy`, common fields; parent is the product | `schemas/record.schema.json`, `docs/core/model.md` (Record Types, Pages and Entries, Identifiers, Parents), `docs/core/intent.md`, `templates/consumer/requirement-page.md` renamed `topic.md`, `templates/README.md`, `templates/consumer/README.md` |
| A2.2 | Requirement parent by identifier containment; topic segment must equal the page name (`identifier-form`) | `apps/cli/src/LiteSpec.Cli/Model/*` parent resolution and identifier checks, tests |
| A2.3 | `cites` on requirement (D2), journey (D4), and the reason links of D10; remove `features` and requirement `basedOn` | schema, model.md Links table with reads-as phrases ("cites", "is cited by"), intent.md, templates, checker link extraction, book labels |
| A2.4 | `requirement-uncited` (D3) and `constraint-uncited` (D5), with tiers and tests; finding table in `docs/tools/model.md` and `Finding.cs` | checker, `apps/model/src/LiteSpec.Model.Core/Finding.cs`, `docs/tools/model.md` |
| A2.5 | Only Behavior fulfils (D4): `CheckFulfilment` counts rule, use case, reaction, job, notification, setting | `Checker.cs` `CheckFulfilment`, tests (finding I15, X6, B2) |
| A2.6 | Constraint What It Requires as a generated region (D5) | renderer, `constraint.md` template, intent.md |
| A2.7 | Kinds (D6), exclusions (D7), journeys (D8) | schema enums and fields, intent.md, model.md, templates, book |
| A2.8 | The questions table and write-when and skip-when notes (D21); glossary entries | intent.md Concepts, `docs/reference/glossary.md` |
| A2.9 | Rewrite decision 0002's consequence for requirements as a new decision that replaces it, with research citing `inputs/outside-review-intent-hierarchy.md` and the largest consumer's counts | `docs/intent/decisions/`, `docs/intent/research/` |

## Part B: The Language Standard

One grammar and one tone for every name and every value a project writes or a tool prints, in every layer. Sections B1 to B5 cover field names, renames, Intent values and Intent identifiers; B6 to B10 extend the same rules to Behavior, Implementation, Proof and Governance, to finding codes and tool output, and name the page that holds them.

### B1. The eight word classes

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

Dates and booleans follow the classes: a deadline is `<base verb>By` (`decideBy`, `reviewBy`, `endBy`; `dueBy` and `plannedFor` hold a date or a milestone); a recorded date is `<verb>edOn` (`approvedOn`, `answeredOn`, `readOn`); a validity start is `effectiveFrom`; a boolean follows D11.

### B2. Renames

Intent first, then Behavior and governance. Each rename moves the schema, templates, every page citing it, the checker, the builder, the book labels and the tests in one change.

| Today | New | Class | Reason |
|:---|:---|:---|:---|
| requirement `features`, `basedOn` | `cites` | 2 | D2, D10 |
| journey `fulfills` | `cites` | 2 | D4 |
| journey `aim` | removed | | D8 |
| step `says` | `statement` | 6 | D8 |
| step `ifItGoesWrong` | `failure` | 6 | D8 |
| product `neverDoes` | `exclusions` | 6 | D7 |
| product, notification `audience` | `addresses` | 2 | a list is a verb |
| requirement `kind: business` | `kind: policy` | 5 | D6 |
| `except[].when` | `except[].condition` | 7 | D17 |
| decision `changeWhen` | `reopenedWhen` | 7 | the state, not a command |
| decision `for`, `against` | `pros`, `cons` | 6 | D20 |
| decision `chosen` | `choice` | 6 | D20 |
| decision `decidedOn` | removed (`approvedOn`) | | D20 |
| question `recommended` | `recommendation` | 6 | D16 |
| question `tier` | removed (from `risk`) | | D16 |
| research source `read` | `readOn` | date | dates rule |
| reaction `if` | `skippedWhen` | 7 | today's name reads as the opposite of what it does |
| reaction `on` | `triggeredBy` | 4 | |
| query output group `source` | `reads` | 2 | `source` has four meanings |
| input `source` | `origin` | 5 | same |
| record `source: transcribed` | `provenance` | 5 | same |
| process step `when` | `guard` | 6 | D17; same concept as an operator job's `guard` |
| process `driver` | `drivenBy` | 4 | |
| consistency `because` | `cites` | 2 | D10 |
| query `wideningBecause` | `cites` | 2 | D10 |
| retention row `intent` | `cites` | 2 | D10; "intent" names a layer |
| retention row `aggregates` | `covers` | 2 | |
| known gap and deviation `subject` | `covers` | 2 | the Links table already reads it "covers" |
| deviation `code` (file list) | `paths` | 6 | `code` is the refusal code |
| known gap `check` | `finding` | 6 | `check` on a requirement is a test |
| `audit.reason` (enum) | `audit.level` | 5 | `reason` is prose everywhere else |
| job `scope` | `selection` | 6 | `scope` has five meanings |
| rule `historyLink` | `history` | 6 | |
| deviation `removeWhen` | `removedWhen` | 7 | |
| known gap `endsBy` | `endBy` | date | |
| `retries` (integer or string) | one shape | | one name, one shape |
| setting `readBy` | removed (use case `reads`) | | D13 |
| `licence`, `pseudonymise` | `license`, `pseudonymize` | | D14 |

### B3. Value style

| Field | Write it as | Checked |
|:---|:---|:---|
| `statement` (requirement, rule) | One present-tense declarative sentence, third person, product words, ending in a period | ends in a period; no modal word in lower case such as "should" |
| `statement` (goal) | The outcome as a present-tense fact about the world | ends in a period |
| `statement` (problem) | The pain as it happens today, present tense | ends in a period |
| step `statement` | Actor first, present tense: "The traveller pays for the booking." | starts with "The" or an actor title; ends in a period |
| `meaning` | A noun phrase starting with "A" or "An", no leading verb, at most 25 words | starts with A, An or The; word count |
| `rationale`, `*Because` | A clause completing "because ..." | does not start with "because" |
| `*When`, `condition` | A clause completing "when ..." | does not start with "when" or "if" |
| `failure` | What the person experiences, present tense, never a code | contains no refusal-code pattern |
| `question` | One question in product words | ends in "?" |
| `title` | Title Case noun phrase, no period | Title Case; no final period |

### B4. Identifier names per type

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
| use case | verb first (already a provision) | `hold-room` |

### B5. Work items

| Id | Change | Files |
|:---|:---|:---|
| B5.1 | Replace `litespec/core/model#name-each-field-for-one-concept` with the eight-class grammar and no exceptions list; a new decision replaces 0006 | `docs/core/model.md`, `docs/intent/decisions/` |
| B5.2 | Value-style provisions (B3) and identifier-name provisions (B4) | `docs/core/authoring.md` or `model.md`, glossary |
| B5.3 | Apply every rename in B2 | schema, templates, every page citing a field, checker, builder, book, tests |
| B5.4 | Mini-languages to objects (D12) | schema `$defs`, templates, `behavior.md`, checker parsers |
| B5.5 | `litespec-maintain validate`: one rule per word class beside `FIELD_NAME_SHAPE`, and the mechanical value-style checks on LiteSpec's own templates and examples; `litespec model check` runs the value-style checks on consumer records at tier A | `apps/maintain/src/LiteSpec.Maintain/StandardsValidator.Fields.cs`, checker |
| B5.6 | American spelling sweep, with a rejected-word entry in the glossary for each British form | all of `docs/`, schema, templates |

### B6. Tense, Voice and Person Across the Layers

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
| A finding message | One present-tense sentence of fact (B8) | "Requirement 'booking.service-fee' is fulfilled by no Behavior record." |

Gherkin fields keep Gherkin practice: `given` is the starting state (present tense, or past tense for an earlier act), `when` is one act by a named actor (present tense), `then` is the observable outcome (present tense, never "should"). A result's `meaning` says what is now true (success) or what the caller is told (refusal).

Person: record values are third person. Intent chapter prose (The Situation, What We Do) may say "we" for the business. Nothing says "you". Modal words (MUST, SHOULD, MAY) belong only to LiteSpec's own provisions; a project's record states a fact or a commitment as a fact.

Quantities: dates are ISO 8601 dates, durations ISO 8601 durations (`P7Y`, `PT10M`), money is `{ amount, currency }` with an ISO 4217 code, and prose writes numbers with their unit ("95 percent", "3 seconds").

### B7. Identifier Names for Every Record Type

B4 covers Intent. The Behavior and Governance types follow the same principle, a name in the word class of what the record is:

| Type | Name form | Example |
|:---|:---|:---|
| module | plural noun of the business area | `orders` |
| aggregate | singular noun | `order`, `ticket-resale` |
| rule | the assertion, keeping its negation word | `order.checkout-commits-with-holds`, `expense.approver-is-not-submitter` |
| event | `<aggregate>.<past participle>` | `order.placed` |
| integration event | `<aggregate>-<past participle>` | `order-placed` |
| use case | verb first, from the verb vocabulary | `place-order` |
| automation, event trigger | `<verb>-<what>-on-<fact>` | `issue-credit-note-on-refund-succeeded` |
| automation, schedule or manual | `<verb>-<what>`, with the verb of the use case it runs | `expire-lapsed-ticket-resales` |
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

### B8. Finding Codes, Messages and Change Codes

- **Code:** `<subject>-<problem>`. The subject is a record type or field as the glossary spells it; the problem is one of twelve words, each with one meaning: `missing`, `unknown`, `undeclared`, `unrecorded`, `unimplemented`, `unreached`, `drift`, `invalid`, `duplicate`, `clash`, `expired`, `reopened`. Example: `use-case-without-scenario` becomes `use-case-scenario-missing`.
- **Message:** one present-tense sentence of fact that starts with the subject's title and its quoted identifier and states what is true, never what to do: "Journey step 'book-a-room#s3' is served by no use case." What to do lives in the finding table on `docs/tools/model.md`, in a column for it.
- **Change codes** in `litespec model diff` use the same pattern with past participles: `refusal-added`, `rule-weakened`, `requirement-narrowed`.
- **Analyzer diagnostics** keep their `LS` numbers and follow the same message rule.

### B9. Tool Surface

Command-line subcommands are verbs (`build`, `check`, `render`, `query`, `diff`, `review`, `approve`). Flags are kebab-case: a flag that switches a mode names the mode (`--release`, `--staged`, `--dry-run`); a flag that takes a value names the value (`--since <ref>`, `--by <owner>`). Exit codes stay 0 clean, 1 blocking, 2 bad arguments. Book labels come from the Links table's reads-as phrases and the glossary only.

### B10. Where the Standard Lives

A new core page, `docs/core/language.md` ("Record Language"), holds B1 to B9 as provisions: the field grammar (replacing `litespec/core/model#name-each-field-for-one-concept`), the value style, identifier names, finding codes and messages, and tool names. `docs/core/authoring.md` keeps the prose of LiteSpec's own pages (Title Case headings, imperative provision headings, one-sentence Statements, Reviewed for as a question) and links to `language.md` for everything a project writes. Each rule lives on one of the two pages, never both.

| Id | Work item | Files |
|:---|:---|:---|
| B10.1 | Write `docs/core/language.md` with B1 to B9 as provisions, each Checked by or Reviewed for; move the field provision from `model.md`; link from `authoring.md`, `intent.md`, `behavior.md` and `model.md` | `docs/core/`, `docs/README.md`, `docs/reference/provisions.md` (regenerated) |
| B10.2 | Checks: `FIELD_NAME_SHAPE` per word class in `litespec-maintain validate`; value-style and identifier-name checks on consumer records at tier A in `litespec model check`; a test that every finding code matches B8 and every message starts with a subject and an identifier | `apps/maintain`, `apps/cli/src/LiteSpec.Cli/Model/`, `apps/model/src/LiteSpec.Model.Core/Finding.cs`, tests |
| B10.3 | Rename finding codes and diff change codes to B8, with every citation in the same change | `Finding.cs`, checkers, `Differ.cs`, `docs/tools/model.md`, every page citing a code |
| B10.4 | Rewrite every template's placeholders and example values in the B6 style, and the Lantern example with them | `templates/consumer/`, `inputs/deck-sources/lantern-gen.js` |

## Part C: Intent Gaps and Inconsistencies

All items below are `open` or `changed` on `c9b2725` (`inputs/findings-verified.tsv`). Wave 1 changes the pages and records; wave 3 changes the schema, templates and code; C1 is a correctness fix in wave 2.

| Id | Work item | Findings | Wave |
|:---|:---|:---|:---|
| C1 | **Fix the two defects first.** The builder reads `decider` and `decideBy` on glossary terms (`RecordReader.cs` term records), and the glossary template carries both fields; a test proves a draft term with both raises nothing. `CheckFulfilment` counts only Behavior records (A2.5). The topic segment of a requirement identifier must match its page (A2.2). | I14, I15, I53 | 2 |
| C2 | **One question lifecycle (D16, D19).** `questionEntry`: `ballot` required; `recommendation` required when the record carries no `risk`; `answer`, `answeredBy`, `answeredOn`; no `tier`, no `risk`. `draft-without-decider` also checks question entries. `question-aged` reports every unanswered question past its `decideBy` that cannot auto-approve, tier 1 included; the "becomes a known gap" wording goes. The rule lives once, in `model.md` Questions and Ballots, and `intent.md`, `layers.md`, `agent.md`, `release.md`, `scope.md` and the glossary cite it. "Unless vetoed" is removed; `review.vetoWindowDays` is optional with default 7 on both `scope.md` and `project-file.md`. | I2, I4, I5, I6, I7, I23, I24, I30, I51, X8, X15 | 1, 3 |
| C3 | **One approval record.** The approval-hash guide's per-type table is the one definition, and `intent.md` and `model.md` link to it. Decisions carry approval as D20 says. The owner approves decisions 0001 to 0009, or each citation says "Proposed by" until approved. | I3, I9, I17, I52, X11 | 1, 3 |
| C4 | **Requirements.** `requirement-unmeasured` is described as it works (a requirement carrying `measure` needs a `check` and a passing run); a tier A check reports a statement naming a number with no `measure`. The quality fields `target`, `conditions`, `check`, `runsOn`, `evidence` are documented with which are required. `rationale` is required and one sentence. `setBy` takes one value set, `founder`, `operator`, `tenant`, on requirement and setting. A requirement whose default the decider sets and whose value each tenant sets is stated as one rule: the decider approves the range and default; the tenant's value is data. | I10, I11, I13, I22, I25, I26, I35, I37 | 1, 3 |
| C5 | **Constraints.** D5 and D6; `intent.md` names `constraints.answered` in `litespec.project.json` as the "no outside rules" answer. | I36, I39, I48 | 1, 3 |
| C6 | **Journeys and features.** `intent.md` names the required body sections (journey: Example, Between the Steps, defined as branch order only; actor: Summary). Optional step fields are stated as optional. D8 removes `aim` and system journeys. The feature goal link is stated as coverage: `feature-without-goal` stays tier A, tier G for tier 1, and the provision Statement says so. | I27, I28, I40, I41, I50, X21 | 1, 3 |
| C7 | **Homes and words.** Actors live in `docs/intent/actors/` (`system.md` and the template index follow). The decider-set figures are one list in `model.md` (D22 renames the role). Tier means risk tier (D23). Glossary entries for Intent, Behavior, record layer, approval, decider, ballot, risk tier, check level; "Layer" as documentation depth is renamed. "Product decision" is either un-rejected or replaced on both pages. One never-list for Intent, in the Keep Behavior Out of Intent Statement. The authority order says goals, problems, features, journeys, actors, research and prose carry no authority, and decisions win between peers. | I8, I12, I18, I19, I20, I21, I31, I32, X14, X18 | 1 |
| C8 | **Page and template hygiene.** Bundled provisions split into one obligation each, after C1 to C7 so the split does not chase moving text. The questions table (D21). Decision number: four digits enforced by the schema. "Organization" for the actor kind. The decision template's `decides` placeholder names real types. Templates are validated against the schema by `litespec-maintain validate`, with placeholders in a form the validator skips; research sources use `path` for a repository path. Exclusions (D7). The research record that names a consumer project says "the largest consumer" and drops the path. The topic record carries owner and status (D1). `decision.basedOn` accepts requirements only on technical decisions. | I29, I33, I34, I38, I42, I43, I44, I45, I46, I47, I49 | 1, 3 |
| C10 | **One actor per journey (D45).** `order-each-journey-toward-one-goal` says a step names another actor only at a handoff, and a walk with several goals splits. Example from the largest consumer: a 21-step journey with five actors, from creating the organizer to closing the books, is four journeys (set up and sell an event; buy a ticket; admit the crowd; close the night). | I-new | 1 |
| C9 | **Parent-changed.** Implement `parent-changed`: when a record's semantic hash changes, every record that cites, fulfils or serves it is flagged until updated, tier A while draft and tier G once approved. The alternative, demoting the provision and recording a known gap, is rejected because the top-down story on `layers.md` depends on the flag. | I1, X3 | 3 |

## Part D: Behavior Gaps and Inconsistencies

Wave 5, after the Part B renames land in wave 4.

| Id | Work item | Findings |
|:---|:---|:---|
| D1 | **One link-target table drives the checks.** The Links table in `model.md` lists every written link with reads-as phrases; `RecordTypes.cs` is tested against it; `requirement-unfulfilled`, `step-unserved` and `record-orphaned` read their record types from it, and the pages name the same set. Integration-event `audience` (renamed `addresses`) accepts a permission. | B2, B3, B12, B13, B14, B22 |
| D2 | **The schema backs every "consumer fails" claim.** Required fields per job kind, audit and idempotency by mode, notification fields, external-system operation fields, aggregate `kind` required with no default, exemption `reason`, `period` as an ISO duration, a store carries no states, the `reachedThrough` minimum stated, `advisory` with its authority (an object, D12). Consumer section checks agree with the model check on Goal and What Happens; the dead `reaction` branch in the use-case section check goes; consumer findings carry a tier. | B4, B8, B18, B19, B28, B29, B30, B31, B32, B42, B43, B45 |
| D3 | **One home per structured fact.** Transitions and data live in frontmatter; the body tables are generated regions, and a handwritten table that disagrees is a finding. Notification suppression lives on the notification (`suppressedWhen`), and the "each `sends` edge carries its own `if`" sentence goes. The retention schedule is generated from the data classes. One structured `onFailure` for every edge (D12). | B1, B44, B5, B6, B9 |
| D4 | **Semantics stated once.** Where derived refusals sit in the precedence order (recommended: forbidden, then invalid input, then written refusals). `tenant: cross` routes to the security queue and raises no tier, and `access.md` says so. Glossary term status follows the layer (`reviewed` for behavior terms). Platform settings get a `platform` module. Audit wording: reason `none` is not audited. Effect timing is required on use cases and jobs and optional on reactions. The `either` verb kind and the permission word `manage` are documented. Event tense gets a Standard citing `event-tense-mismatch`. The negation word lists agree. The glossary gains an `implementation` layer for design words. | B7, B10, B11, B20, B21, B27, B34, B37, B38, B39, B41 |
| D5 | **Page quality.** Every Reviewed for line is a question naming where to look. Obligations move out of Concepts. The derived-refusal rule keeps one home. Undefined fields are defined or removed (`onExhausted`, `runsWhere`, "the notification module", `floorplan`, `before`, `forEach`). A Runbook concept and Standard. Template defects fixed (screen `address` and `host`, identifier placeholders, refusal code form, the reaction example name). The Example convention matches the templates. Vague words defined. `behavior.md` split by folder home into domain model, use cases, automation, edges and screens, and privacy and retention. | B15, B16, B17, B23, B24, B25, B26, B33, B35, B36, B40 |

## Part E: The Seams Between the Layers

Wave 9.

| Id | Work item | Findings |
|:---|:---|:---|
| E1 | **The direction rules state their exceptions.** "Records never point down" names `decides` (and `holds`, or `holds` moves onto the role) as the only downward links. "Nothing above Implementation is computed from code" names transcription as the one exception: draft only, ended by `adoption.transcriptionEndsWith`. | X1, X2 |
| E2 | **One home per provision across pages.** The docs layout lives on `model.md` only, and `system.md` links to it. Raise open needs, write from the layer above, enter world facts and compute proof statuses each keep one provision; the other pages cite it. | X4, X17 |
| E3 | **Checks and docs agree.** The structural-parent list is rechecked after D1. `docs/tools/model.md` describes `requirement-unfulfilled` with its exemptions. Known-gap and deviation coverage is one rule in `model.md`. | X5, X6, X10, X26 |
| E4 | **Words with two meanings.** Implementation holds code and technical decisions. Node is needed for web frontends. The semantic hash splits into approval hash, proof hash and sample hash, each in the glossary. "Parent" means placement only. A mark has two kinds, `implements` and `follows`. Background-only, background caller and the effective-status entry are defined; "renewal" goes. | X9, X12, X13, X22, X23, X24 |
| E5 | **Book and workflow.** Book labels use model statuses and glossary terms, or the terms are added. A slice starts at the problem and feature on `agent.md` too. Scenario proof rules state their size: large, or with the `bdd` extension. | X19, X20, X25 |

X7, X8, X11, X14, X15, X16, X18 and X21 are closed by Parts A to C.

## Part F: Teaching Material

Wave 10.

| Id | Work item |
|:---|:---|
| F1 | The Lantern example (`D:/docs/lantern-example/`): requirements renamed `requirement/checkout.*` to match their page; every rename from Part B; `cites` instead of `features` and `basedOn`; questions with `recommendation`; glossary terms with deciders. Re-run `litespec consumer` and `litespec model check` and record the output. |
| F2 | The three decks in `D:/docs/` rebuilt from `inputs/deck-sources/` (`build.js`, `review.js`, the slide parts and the finding lists) with the new names, the topic hierarchy, the new Lantern output, and review appendices reduced to what remains open. Overflow check and screenshots as before. |
| F3 | The book: labels and connection phrases from the renamed Links table; book tests updated. |
| F4 | The Behavior and full-picture decks gain the automation, saga, exception and typed-input changes, with before-and-after slides from the largest consumer's records (anonymized as "a large consumer"), and the Lantern example gains a Behavior slice written in the B6 style. |

## Part G: NightHall Migration

Wave 11, in `D:/Personal-Projects/Nighthall` on branch `v1`, one slice per module as in the hierarchy follow-ups.

| Id | Work item |
|:---|:---|
| G1 | Mechanical renames from Part B across every record. |
| G2 | Requirement pages become topic records; identifiers keep their topic segment or are renamed to match their page. |
| G3 | Requirement reasons move to `cites`; a requirement that cites nothing gets a reason or a draft question, judged by the agents and recorded. |
| G4 | Questions move to the new fields; answered questions record their answer. |
| G5 | Mini-language values become objects. |
| G7 | Reactions and jobs become automations; processes become sagas or entities; the `entry-point/reaction` and `entry-point/job` doors go. |
| G8 | Inputs and payloads gain types; data classes gain `purpose`, ISO periods and the pseudonymize rule; atomicity rules gain `binds`. |
| G9 | Deviations, known gaps, deferrals and the frozen phase become exceptions; finding-code citations follow the renames. |
| G10 | Journeys of several actors split by actor and goal (D45), judged by the agents and recorded. |
| G11 | `litespec consumer` and `litespec model check` pass; approval reruns with the fixed tool for changed batches. |

## Part H: Records This Release Adds

Written in wave 1 under `docs/intent/` in LiteSpec:

| Record | States |
|:---|:---|
| `research/intent-hierarchy-practice` | The outside comparison and the measured consumer counts behind D1 to D8 |
| `research/naming-grammar-evidence` | The 248-field inventory, the collisions and the word classes behind D9 to D18 |
| `decision/0010-topic-holds-its-requirements` | D1 to D5, replacing the requirement part of decision 0002 |
| `decision/0011-one-grammar-for-names-and-values` | D9 to D18, replacing decision 0006 |
| `decision/0012-use-cases-read-their-settings` | D13, replacing decision 0007 |
| `decision/0013-one-question-lifecycle` | D16 and D19 |
| `research/lower-layers-practice` | The outside review of 2026-10-31 (`inputs/outside-review-lower-layers.md`) and the largest consumer's counts behind D25 to D46 |
| `decision/0014-one-exception-record` | D26, replacing the deviation and known-gap types |
| `decision/0015-automations-and-sagas` | D27, D28, D29 and D30 |
| `decision/0016-typed-contracts-and-privacy` | D31 and D33 |
| `decision/0017-proof-that-holds` | D35 to D39 |
| `decision/0018-one-record-language` | D44, with D9 to D18, replacing 0011 if wave 1 writes them together |
| `decision/0019-loose-approval` | D25: approval is sized for one-person teams |

## Part I: Behavior Design

Wave 6. Detail and examples: `proposals-lower-layers.md` BP1 to BP13, with examples from the largest consumer in the session record of 2026-10-31 (`place-order` writes 7 aggregates atomically; `ticket-resale` is a process with states; 256 reactions and 73 jobs share one shape; 71 `if` values spell their own direction; 30 data classes pseudonymize).

| Id | Work item | Proposal | Findings |
|:---|:---|:---|:---|
| I1 | `atomicity` rules gain `binds`; a check that `changes` equals the bound group; tier A saga-candidate finding above 4 aggregates; one consistency table (`atomic`, `per-record`, `compensated`); `best-effort` removed | BP1, BP2, BP9 (revised) | BE1, BE11, BE12 |
| I2 | `kind: saga` with steps, compensations, exits and driver; lifecycles on entities; glossary alias "process manager" | BP3 | BE2 |
| I3 | `automation` record with `triggeredBy`; trigger-specific fields; one set of checks; reactions and jobs folders become `automations/` | BP4 | BE7 |
| I4 | Entry points as doors; `reaction` and `job` kinds removed; auth on the door, grant on the use case, stated once in `access.md` | BP5 | BE6 |
| I5 | `type`, `required` and `of` on inputs and payload fields; `dataschema` on integration events; money as an object; the builder compares inputs with the generated OpenAPI contract at tier S | BP6 | BE3, BE18 |
| I6 | Effect verbs `runs` and `emits`; `manual`, `person` and `none` for the overloaded words | BP7, BP8 | BE8, BE9, BE10 |
| I7 | Privacy: `pseudonymize` stays personal; `anonymize`; `purpose`; ISO periods; the retention schedule's claim narrowed | BP10 | BE4, BE5 |
| I8 | Refusal codes retired never renamed; `exercises` on scenarios; `background` on use cases; `parts` stated as names; "read model" named once | BP11 to BP13 | BE13, BE14, BE15, BE16, BE17 |

## Part J: Implementation

Wave 2 takes J1 and J2; wave 8 takes the rest.

| Id | Work item | Proposal | Findings |
|:---|:---|:---|:---|
| J1 | Analyzer pin 3.0.0; validator compares pin and analyzer project version and checks every cited LS code ships; orphaned pins removed; `cn` checked; NetArchTest replaced or dropped | IP1 | IM1, IM16, IM17 |
| J2 | Fail loud: unknown layer reported; Worker layer; `reader-unavailable` blocks when `CI` is set | IP2 | IM2, IM7, IM8 |
| J3 | Mark style per language, one parser, recorded as seen | IP3 | IM3, IM4, IM13 |
| J4 | Backend mark-to-folder check | IP4 | IM5 |
| J5 | Recognition by symbol; state-changing aggregate methods need a mark; technical-class exclusions stated | IP5 | IM6, IM9, IM14, IM15 |
| J6 | Provision identity from the `**ID:**` line; validator checks uniqueness and that no released id disappears unrecorded | IP6 | IM10 |
| J7 | Size-aware Defaults for project layout and test techniques; mutation threshold with an equivalent-mutant record; Android framework as a Default | IP7 | IM11, IM12, IM22 |
| J8 | Every Statement Checked by a real check or Reviewed for; validator checks named codes, commands and files exist | IP8 | IM18, IM19, IM20 |
| J9 | Write Code for a Use Case page; mark rules in one home; the decision type's two homes stated; an Implementation landing page | IP9 | IM21, IM23, IM24 |

## Part K: Proof

Wave 2 takes K1; wave 7 takes the rest.

| Id | Work item | Proposal | Findings |
|:---|:---|:---|:---|
| K1 | Browser outcomes read; blocking by the record's own status; unknown codes fail the build; `Finding.Blocks` deleted | PP1, PP8 | PR1, PR4, PR11, PR12 |
| K2 | `docs/proof/proofs.json` with scenario and test hashes; the ignored ledger removed | PP2 | PR2 |
| K3 | `use-case-unverified`; `--release`; `--since <ref>` | PP3, PP5 | PR3, PR9 |
| K4 | Proof words verified, met, tested, exercised in one table; tested needs an assertion; indirect proofs documented; browser success proofs assert the result's sentence | PP4, PP8 | PR6, PR7, PR8, PR18 |
| K5 | Finding-code grammar applied (Part B, B10.3); tier-varying codes split or stated; diff change codes aligned | PP6 | PR14, PR16, PR17 |
| K6 | The book renders `model.json` only; its judgement code moves to the builder | PP7 | PR10, PR19 |
| K7 | `exception-expired` on the date, with the tools page matching; a flaky state at tier A | PP8 | PR13, PR15 |

## Part L: Governance

Wave 2 takes L1; wave 7 takes the rest.

| Id | Work item | Proposal | Findings |
|:---|:---|:---|:---|
| L1 | Approval bugs: single-record approval promotes status; re-approval writes the new approver and date | GP1 | GO3, GO6 |
| L2 | Loose approval stated on every page (D25): agent reading counts, solo projects need no second reviewer, security reviewer and queue optional | GP1 | GO2, GO9, GO10 |
| L3 | The `exception` record (D26): schema, template, suppression by the codes it covers, one expiry finding; deviation, known gap, deferral and frozen phase removed | GP2 | GO5, GO7, GO13, GO17, PR11 |
| L4 | Words: `physical-access`; risk values and task authorizer in the glossary; governance as a group beside the layers | GP3 | GO16, GO19, GO20 |
| L5 | Agent query by risk; corrected examples | GP4, GP5 | GO18, GO21 |

Not adopted, by owner direction: GO1, GO4 beyond D16, GO8, GO11, GO12, GO14, GO15.
