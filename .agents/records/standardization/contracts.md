# Contracts Between Slices

Names that two slices of one wave must agree on. A slice that writes one of these uses it exactly; a slice that cites one uses it exactly. A provision ID is derived from its heading, so the heading below is the one to write.

## Wave 1: Provisions on the New Language Page

`docs/core/language.md`, title "Record Language", holds spec Part B as provisions. Headings and IDs:

| Heading | ID | Spec |
|:---|:---|:---|
| Name Each Field by Its Word Class | `litespec/core/language#name-each-field-by-its-word-class` | B1, D9 |
| Name a Boolean as a Predicate | `litespec/core/language#name-a-boolean-as-a-predicate` | D11 |
| Name Each Date by What It Marks | `litespec/core/language#name-each-date-by-what-it-marks` | B1 dates |
| Write Each Kind of Text in Its Form | `litespec/core/language#write-each-kind-of-text-in-its-form` | B6 |
| Write Each Value in the Value Style | `litespec/core/language#write-each-value-in-the-value-style` | B3, D18 |
| Write Quantities in Standard Units | `litespec/core/language#write-quantities-in-standard-units` | B6 quantities |
| Name Each Record by Its Type's Name Form | `litespec/core/language#name-each-record-by-its-types-name-form` | B4, B7 |
| Spell in American English | `litespec/core/language#spell-in-american-english` | D14 |
| Keep Names an Outside Standard Fixes | `litespec/core/language#keep-names-an-outside-standard-fixes` | D9 exemptions |
| Name Each Finding Code by Subject and Problem | `litespec/core/language#name-each-finding-code-by-subject-and-problem` | B8, D39 |
| Write Each Finding Message as a Fact | `litespec/core/language#write-each-finding-message-as-a-fact` | B8 |
| Name Each Change Code with a Past Participle | `litespec/core/language#name-each-change-code-with-a-past-participle` | B8 |
| Name Commands as Verbs and Flags by What They Set | `litespec/core/language#name-commands-as-verbs-and-flags-by-what-they-set` | B9 |

`litespec/core/model#name-each-field-for-one-concept` is removed; every citation of it moves to `litespec/core/language#name-each-field-by-its-word-class`.

If the validator refuses a heading's first word, keep the ID's meaning, choose the nearest accepted verb, and list the new heading and ID under Decisions in the done file.

## Wave 1: Renamed and New Provisions on Other Pages

| Old ID or none | New heading | New ID | Owner slice |
|:---|:---|:---|:---|
| `litespec/core/intent#shape-each-requirement-page` | Shape Each Topic Page | `litespec/core/intent#shape-each-topic-page` | w1.intent |
| none | Cite the Reasons of Every Requirement | `litespec/core/intent#cite-the-reasons-of-every-requirement` | w1.intent |
| none | Cite Each Applying Constraint from a Requirement | `litespec/core/intent#cite-each-applying-constraint-from-a-requirement` | w1.intent |
| none | Fulfill Requirements Only from Behavior | `litespec/core/intent#fulfill-requirements-only-from-behavior` | w1.intent |
| `litespec/core/intent#leave-business-choices-to-the-founder` | Leave Business Choices to the Decider | `litespec/core/intent#leave-business-choices-to-the-decider` | w1.intent |
| `litespec/core/intent#group-questions-into-ballots` | stays; its lifecycle text moves to model.md and it cites the provision below | unchanged | w1.intent |
| none | Answer Each Question on Its Record | `litespec/core/model#answer-each-question-on-its-record` | w1.model |
| `litespec/core/model#mark-every-check-with-its-tier` | Mark Every Check with Its Level | `litespec/core/model#mark-every-check-with-its-level` | w1.model |
| none | Flag Every Record Whose Parent Changed | `litespec/core/model#flag-every-record-whose-parent-changed` | w1.model (C9; the check lands in wave 3, so Checked by names `parent-changed`) |

## Wave 1: Words

- The requirement container is `topic` (D1), identifier `topic/<name>`, page `docs/intent/<area>/<topic>.md`. A requirement is `requirement/<topic>.<name>`.
- The link verb for a reason is `cites`; the Links table reads it "cites" and "is cited by".
- Check tiers G, S and A are "check levels" (D23); "tier" alone means the risk tier 1 or 3.
- The role word is "decider" (D22); "founder" appears only as one decider a project may name.
- Proof words (D38): verified (use case), met (requirement), tested (a passing test with an assertion), exercised (journey step).

## Wave 1: Records

Decision numbers and research names are fixed by spec Part H (`source/spec.md` lines 362 to 380). Decision 0018 absorbs 0011: write 0011 as the grammar decision and 0018 as the one-page decision that cites it, so both numbers exist and neither is empty.

## Wave 3: Intent Record Shapes

Every slice of wave 3 writes and reads exactly these names. Old names disappear everywhere in the same wave: schema, templates, checker, builder, differ, book, validator, authoring commands, fixtures, tests and docs.

| Record | Old | New |
|:---|:---|:---|
| page style | `requirement-page` | `topic` (template `templates/consumer/topic.md`) |
| topic page | none | `type: topic`, `id: topic/<name>`, required `type`, `id`, `title`, `owner`, `status`, `reviewBy`; optional `decider`, `decideBy`, `approvedBy`, `approvedOn`, `approvedHash`, `terms`, `questions`, `records` (the requirement entries). Sections: Summary, The Situation, What We Do, Why It Is Better, Example, Costs and Risks, How Sure We Are, Position. No Refusals section. Parent: the product. |
| requirement entry | `features`, `basedOn` | `cites`: ids of goal, problem, feature, constraint, research or decision, any order |
| requirement entry | `kind: business` | `kind: policy` |
| requirement entry | `except[].when` | `except[].condition` |
| requirement entry | `rationale` optional | `rationale` required |
| requirement entry | `setBy` values | `decider`, `operator`, `tenant` (also on settings) |
| requirement id | `requirement/<area>.<name>` | `requirement/<topic>.<name>`; the topic segment equals the page file name, else `identifier-form` |
| question entry | `recommended`, `tier` | `recommendation`; `tier` removed. Fields: `question`, `decider`, `decideBy`, `ballot` (required), `recommendation`, `answer`, `answeredBy`, `answeredOn`. `recommendation` is required by the checker when the holding record carries no `risk`. |
| decision page | `chosen`, `changeWhen`, `decidedOn`, option `for`, `against` | `choice`, `reopenedWhen` (required), no `decidedOn`; option `pros`, `cons`. Identifier number is four digits. |
| journey page | `aim`, `fulfills` | `aim` removed; `cites` names the requirements the journey shows |
| journey step | `says`, `ifItGoesWrong` | `statement`, `failure` |
| product page | `neverDoes`, `audience` | `exclusions`, `addresses` |
| notification, integration event | `audience` | `addresses` |
| constraint page | `kind` values | adds `legal`; What It Requires is a generated region listing the requirements that cite the constraint |
| research source | `read` | `readOn` |

Finding codes this wave adds (wave 4 renames every code to the B8 grammar; use these names until then):

| Code | Level | Meaning |
|:---|:---|:---|
| `requirement-uncited` | A, or G when the requirement carries `risk` | A requirement cites nothing. |
| `constraint-uncited` | G | A constraint with `applies: true` that no requirement cites. |
| `parent-changed` | A while the record is draft, G once approved | A record cites, fulfills or serves a record whose semantic hash changed after the record was last approved. |

`requirement-unfulfilled` counts only rule, use case, reaction, job, notification and setting (landed in wave 2).

## Wave 4: Finding Codes (B8)

The judge renames every code below across LiteSpec before the wave's workers start (foundation `w4.codes`). Workers use only the new codes. Codes not listed already follow B8 and keep their names. Codes of reactions, jobs, processes, known gaps and deviations are renamed again when waves 6 and 7 replace those records.

| Old | New |
|:---|:---|
| `act-event-unraised` | `act-event-unimplemented` |
| `admission-incomplete` | `admission-invalid` |
| `alert-unlinked` | `alert-link-missing` |
| `calculation-without-table` | `calculation-table-missing` |
| `collaborator-holds-repository` | `collaborator-repository-invalid` |
| `consistency-unexplained` | `consistency-cites-missing` |
| `constituent-orphaned` | `constituent-parent-missing` |
| `constraint-uncited` | `constraint-unreached` |
| `decision-decides-nothing` | `decision-decides-missing` |
| `decision-unfollowed` | `decision-unimplemented` |
| `decision-without-basis` | `decision-basis-missing` |
| `dependency-count` | `dependency-count-invalid` |
| `dependency-licence-unapproved` | `dependency-license-unknown` |
| `dependency-unadmitted` | `dependency-undeclared` |
| `draft-gate` | `approval-invalid` |
| `draft-without-decider` | `decider-missing` |
| `effect-before-commit` | `effect-timing-invalid` |
| `effect-loop` | `effect-loop-invalid` |
| `event-tense-mismatch` | `event-tense-invalid` |
| `except-without-fallback` | `except-fallback-missing` |
| `feature-without-goal` | `feature-goal-missing` |
| `gap-unknown-check` | `gap-finding-unknown` |
| `identifier-form` | `identifier-invalid` |
| `integration-event-wording` | `integration-event-name-invalid` |
| `invalid-field` | `field-invalid` |
| `job-name-form` | `job-name-invalid` |
| `journey-reach` | `use-case-journey-missing` |
| `link-unresolved` | `link-unknown` |
| `many-aggregates` | `atomic-group-duplicate` |
| `mark-count` | `mark-count-invalid` |
| `mark-style-mixed` | `mark-style-invalid` |
| `mark-unresolved` | `mark-unknown` |
| `name-drift-across-layers` | `name-drift` |
| `notification-without-sender` | `notification-sender-missing` |
| `operation-use-case-mismatch` | `operation-use-case-drift` |
| `operator-job-without-guard` | `job-guard-missing` |
| `parent-changed` | `parent-reopened` |
| `product-not-singleton` | `product-count-invalid` |
| `proves-duplicated` | `proves-duplicate` |
| `proves-unconfirmed` | `proves-unreached` |
| `quality-unalerted` | `quality-alert-missing` |
| `query-filters-in-memory` | `query-filter-invalid` |
| `question-aged` | `question-expired` |
| `reaction-name-form` | `reaction-name-invalid` |
| `reaction-trigger-unresolved` | `reaction-trigger-unknown` |
| `reader-unavailable` | `reader-missing` |
| `reasonless-name` | `name-reason-missing` |
| `record-orphaned` | `record-link-missing` |
| `record-shape` | `record-invalid` |
| `record-without-parent` | `record-parent-missing` |
| `reference-unresolved` | `reference-unknown` |
| `refusal-code-form` | `refusal-code-invalid` |
| `refusal-not-thrown` | `refusal-unimplemented` |
| `repository-read-in-loop` | `repository-read-invalid` |
| `requirement-uncited` | `requirement-cites-missing` |
| `requirement-unfulfilled` | `requirement-fulfillment-missing` |
| `requirement-unmeasured` | `requirement-measure-missing` |
| `reservation-without-release` | `reservation-release-missing` |
| `result-unshown` | `refusal-sentence-missing` |
| `result-without-scenario` | `result-scenario-missing` |
| `retention-without-enforcement` | `retention-enforcement-missing` |
| `risk-without-acceptance` | `risk-acceptance-missing` |
| `route-shape` | `route-invalid` |
| `rule-name-negation` | `rule-name-invalid` |
| `scenario-result-unresolved` | `scenario-result-unknown` |
| `schedule-unstructured` | `schedule-invalid` |
| `screen-calls-mismatch` | `screen-offers-drift` |
| `screen-serves-mismatch` | `screen-serves-drift` |
| `setting-default-without-requirement` | `setting-requirement-missing` |
| `state-unreachable` | `state-unreached` |
| `statement-vague-word` | `statement-word-invalid` |
| `step-unscreened` | `step-screen-missing` |
| `step-unserved` | `step-use-case-missing` |
| `template-key-form` | `template-key-invalid` |
| `term-lookalike` | `term-clash` |
| `transcribed-after-adoption` | `transcription-expired` |
| `transcribed-reviewed` | `transcription-status-invalid` |
| `transition-unresolved` | `transition-unknown` |
| `use-case-act-uncalled` | `use-case-act-missing` |
| `use-case-unexposed` | `use-case-endpoint-missing` |
| `use-case-verb-kind` | `use-case-verb-invalid` |
| `use-case-verb-method` | `use-case-method-drift` |
| `use-case-without-scenario` | `use-case-scenario-missing` |
| `use-case-without-success` | `use-case-success-missing` |
| `widening-unjustified` | `widening-cites-missing` |

A finding message is one present-tense sentence of fact that starts with the subject's title (the record type as the glossary spells it, capitalized: "Use case", "Requirement", "Journey step") and its quoted identifier, and says what is true, never what to do: "Use case 'orders.place-order' has no scenario." What to do moves to a new last column, "Fix", of the finding table on `docs/tools/model.md`.

## Wave 4: Field Renames (B2, outside Intent)

Reaction, job, process, deviation and known-gap fields are not renamed here: waves 6 and 7 replace those records.

| Record | Old | New |
|:---|:---|:---|
| query output group | `source` | `reads` |
| use-case input | `source` | `origin` |
| any record | `source: transcribed` | `provenance: transcribed` |
| consistency | `because` | `cites` |
| query | `wideningBecause` | `cites` |
| retention row | `intent`, `aggregates` | `cites`, `covers` |
| audit | `reason` (the enum) | `level` |
| job | `scope` | `selection` |
| rule | `historyLink` | `history` |
| setting | `readBy` | removed; a use case names the settings it reads in `reads` (D13) |
| any field or value | `licence`, `licences`, `pseudonymise` | `license`, `licenses`, `pseudonymize` |

## Wave 4: The Value Style Check

`litespec model check` reports the mechanical half of the value style (B3) on consumer records as one code, `value-invalid`, at level A. Its message names the record, the field and the rule the value breaks: "Requirement 'checkout.room-held' has a statement that does not end in a period."

## Wave 5: Mini-Languages Become Objects (D12)

Every value below was a string with its own small grammar; it becomes an object whose `mode` names the case. A `reason` completes "because ..." (B6). Durations are ISO 8601.

| Field | Old string | New object |
|:---|:---|:---|
| query `freshness` | `immediate`, `snapshot` | `{ "mode": "immediate" }`, `{ "mode": "snapshot" }` |
| query `freshness` | `bounded (<duration>)` | `{ "mode": "bounded", "within": "PT5M" }` |
| query `freshness` | `advisory` | `{ "mode": "advisory", "authority": "<record id that holds the authoritative value>" }` |
| query `paging` | `none (<reason>)` | `{ "mode": "none", "reason": "..." }`; a paged query keeps its object and adds `"mode": "pages"` |
| `compensation` | `none (<reason>)` | `{ "mode": "none", "reason": "..." }`; a compensating use case is `{ "mode": "use-case", "useCase": "use-case/..." }` |
| retention `period` | `none (on-demand)` | `{ "mode": "none", "reason": "..." }`; a duration stays a string |
| rule `releasedBy` | `never (<cost>)` | `{ "mode": "never", "reason": "..." }`; a releasing record stays an id |
| erasure `forwarding`, data class erasure | `call <operation>` | `{ "mode": "call", "operation": "external-system/<name>#<operation>" }` |
| erasure | `via integration-event/<name>` | `{ "mode": "event", "event": "integration-event/<name>" }` |
| erasure | `impossible (<reason>)` | `{ "mode": "impossible", "reason": "..." }` |
| `onFailure` | `retry (<n>, <backoff>) then dead-letter`, `skip-and-log`, `discard` | the structured object only: `{ "mode": "retry", "retry": { "maxAttempts": 5, "backoff": "PT1M" }, "then": "dead-letter" }`, `{ "mode": "skip-and-log" }`, `{ "mode": "discard" }` |
| `retries` | integer or string | removed; the retry count lives in `onFailure.retry.maxAttempts` |

The process `driver` (`operator: ...`) is reshaped in wave 6 with the saga (D28, D32).

## Wave 5: The Behavior Page Splits by Folder Home (D5)

`docs/core/behavior.md` keeps the Behavior overview: its Intent, the Spec to Design Mapping concept, Keep Design Choices Out of the Records, the Conventions and the Reference Example, and a table linking the five pages below. Every other concept and provision moves, unchanged in heading, to one page:

| Page | Title | Holds |
|:---|:---|:---|
| `docs/core/domain-model.md` | Domain Model | Bounded Context and Module, Glossary, Aggregates, Process Aggregates, Rules, Domain Events; the provisions from Define Every Behavior Term in the Glossary through Raise Every Event Through Its Aggregate Root, except the data-class and erasure ones; Keep Model Invariants in Rules |
| `docs/core/use-cases.md` | Use Cases | Use Cases, Queries, Use-Case Verbs, Results and Refusals, Scenarios, Settings; the provisions from Give Each Use Case a Command or Query Kind through Write Prose on Significant Use Cases, and the setting provisions |
| `docs/core/automation.md` | Automation | Reactions, Jobs; the reaction and job provisions |
| `docs/core/edges.md` | Edges and Screens | Integration Events, Notifications, Entry Points, Screens, External Systems; their provisions, except Route Every External Erasure Through an Operation or Event |
| `docs/core/privacy.md` | Privacy and Retention | Retention Schedule and the data classes; Classify Every Aggregate Data Field, Declare Every Data Class with Its Erasure Position, Accept Every Retained Personal Data Class in Intent, Review Every Erasure Exemption Before Its Date, Route Every External Erasure Through an Operation or Event, Schedule Every Data Class for Retention |

A moved provision keeps its heading, so its ID changes only in the page part: `litespec/core/behavior#<slug>` becomes `litespec/core/<page>#<slug>`. The judge renames every citation by script after the split.

## Wave 5: New Finding Code

| Code | Level | Meaning |
|:---|:---|:---|
| `table-drift` | A | A handwritten body table holds a structured fact (transitions, data fields, retention rows) that differs from the record's frontmatter; the generated region is the one home (D3). |

## Wave 6: Behavior Design (Part I)

### Automation (D27, BP4)

One record type `automation` replaces `reaction` and `job`. Identifier `automation/<module>.<name>`; home `docs/behavior/modules/<module>/automations/<name>.md` (the `reactions/` and `jobs/` folders go). Name form: `<verb>-<what>-on-<fact>` for an event trigger, `<verb>-<what>` with the verb of the use case it runs for a schedule or manual trigger.

| Field | Shape |
|:---|:---|
| `triggeredBy` | exactly one of `{ "event": "event/<...>" }` or `{ "event": "integration-event/<...>" }`, `{ "schedule": { "every": "PT1M" } }` or `{ "schedule": { "cron": "0 3 * * *" } }`, `{ "manual": true }` |
| `skippedWhen` | was reaction `if`: a clause completing "when ..." |
| `effects` | as before, with the effect verbs below |
| `runbook`, `runsWhere`, `guard`, `cursor`, `selection`, `lease`, `batchSize` | allowed only with a `schedule` or `manual` trigger; `guard` is required with `manual` |
| `fulfills`, `serves`, `onFailure`, `idempotency`, `reads`, `changes` | shared, as on reactions and jobs today |

A code element that implemented a reaction or job carries `implements automation/<module>.<name>`.

### Saga (D28, BP3)

The aggregate kind `process` becomes `saga`: `steps` (each `{ "id", "runs": "use-case/<...>", "compensation": <wave 5 object>, "guard": "<clause>" }`, `guard` was step `when`), `exits` (each named by a past participle or adjective), and `drivenBy` (was `driver`): `{ "automation": "automation/<...>" }` or `{ "person": "<actor or role id>", "runbook": "runbook/<...>", "dueWithin": "<ISO duration>" }`. A process that only has a lifecycle becomes `kind: entity` with its states and transitions. Glossary alias: process manager.

### Doors and Words (D29, D32, BP5, BP7, BP8)

- Entry-point kinds: `browser`, `api`, `webhook`, `device`, `cli`, `mcp`, `served-content`. The kinds `reaction` and `job` go. The door states `auth` (`session`, `token`, `device`, `link`, `app`, `none`); the use case states `grant` and `tenant`. The auth kind `anonymous` is `none`; the grant `anonymous` stays.
- Effect verbs: `runs` (was `issues`), `sends`, `emits` (was `publishes`), `calls`.

### Consistency (D30, BP1, BP2, BP9)

Modes `atomic`, `per-record`, `compensated`; `best-effort` goes. An `atomicity` rule names the aggregates it binds in `binds`. A use case with `consistency.mode: atomic` over several aggregates cites that rule in `consistency.cites`, and its `changes` equals the rule's `binds`.

### Typed Contracts (D31, BP6)

Each use-case input and integration-event payload field carries `type` (`text`, `integer`, `decimal`, `money`, `boolean`, `date`, `date-time`, `duration`, `email`, `choice`, `id`), `of` (the record an `id` refers to; the choices of a `choice`) and `required`. An integration event may name `dataschema` (a URL). A money value is `{ "amount", "currency" }` with an ISO 4217 code.

### Privacy (D33, BP10)

The erasure positions gain `anonymize`; `pseudonymize` keeps the class personal with its basis and counts toward `risk-acceptance-missing`. Each data class and retention row states `purpose`. `period` is an ISO 8601 duration or the wave 5 none object. The retention schedule claims only the time limits of a record of processing.

### Scenarios and Refusals (D34, BP11, BP12)

A scenario may name the rules it shows in `exercises`; a use case may hold `background`, the shared Given. A refusal code in use is retired with its replacement named, never renamed or reused.

### Finding Codes

| Old | New | Level |
|:---|:---|:---|
| `reaction-name-invalid`, `job-name-invalid` | `automation-name-invalid` | G |
| `reaction-trigger-unknown` | `automation-trigger-unknown` | G |
| `reaction-unimplemented`, `job-unimplemented` | `automation-unimplemented` | S |
| `job-guard-missing` | `automation-guard-missing` | G |
| none | `atomic-group-drift`: an atomic use case's `changes` differs from its rule's `binds` | G |
| none | `atomic-group-invalid`: an atomic group binds more than 4 aggregates (a saga candidate) | A |

### One onFailure (D3, D12)

`onFailure` has one shape on every record and effect: `{ "mode": "retry" | "skip-and-log" | "discard", "retry": { "maxAttempts", "initialDelay", "maxDelay", "backoff": "exponential" | "fixed" }, "then": "dead-letter", "deadLetter": { "after", "action" }, "surfacesIn": "entry-point/<...>", "runbook": "runbook/<...>" }`. `retry` and `then` are required with `mode: retry`; `deadLetter`, `surfacesIn` and `runbook` are optional. The record-level `structuredOnFailure` and the effect-level `onFailurePolicy` merge into this one definition.

### Wave 5 Carry-Overs

- The Links table on `docs/core/model.md` gains rows for the nested link fields `compensation.useCase`, `freshness.authority`, `erasure.operation` and `erasure.event`, and `RecordTypes.cs` lists them as link targets.
- The aggregate template wraps its Transitions and Data tables in `<!-- generated:transitions -->` and `<!-- generated:data -->` fences, and the retention schedule template carries a `<!-- generated:retention-schedule -->` fence.
- `docs/tools/consumer.md` states that every consumer finding carries its level.

## Wave 7: Proof and Governance (Parts K and L)

### Exception (D26, GP2)

One record type `exception` replaces `deviation`, `known-gap`, the project setting `deferredProvisions` and `check.phase: frozen`. Identifier `exception/<name>`, a noun phrase of what it allows; home `docs/governance/exceptions/<name>.md` (the `deviations/` and `known-gaps/` folders go).

| Field | Shape |
|:---|:---|
| `kind` | `departure` (the project follows another rule than a LiteSpec provision) or `gap` (a check stays red until a date) |
| `covers` | a provision ID (a departure) or a list of finding codes (a gap) |
| `subject` or `paths` | the record ids, or the path globs, the exception covers; one of the two is required |
| `reason` | a clause completing "because ..." |
| `owner`, `status`, `expiresOn` | the common owner and status; the date the exception lapses |

An exception suppresses only the findings it covers, on its subject or paths, and only once `status` is `approved`; one person may approve it. A departure covers the finding codes its provision's Checked by line names.

Finding codes: `known-gap-expired` becomes `exception-expired` (G, the day after `expiresOn`); `gap-finding-unknown` becomes `exception-finding-unknown` (G). `exemption-expired` (erasure exemptions) stays.

### Proof (D35 to D38, PP1 to PP8)

- `docs/proof/proofs.json` is committed: for each scenario, the hash of the scenario and of its test body at the last passing run. A proof is stale when either changes. `.model/proofs.json` goes.
- New code `use-case-unverified` (A by default): a use case whose scenarios are not all proved. `litespec model check --release` makes it, and every other release-readiness finding (an unanswered question on a tier 1 record), block. `litespec model check --since <ref>` blocks only on findings about records and code changed since `<ref>` and reports the rest.
- The four proof words (verified, met, tested, exercised) are defined in one table on `docs/core/model.md`; "tested" needs a passing test that asserts something.
- The book renders `model.json` only; any judgement it computes today moves to the builder.
- A test that both passed and failed within the runs read is `proof-invalid` (A).

### Governance (D25, D46, GP1, GP3, GP4, GP5)

- Approval stays loose, and every page says so: an agent's reading counts as the tier 1 second reading; a project whose owners list holds one name needs no second reviewer; `review.securityReviewer` is optional and the security queue is a list to read.
- The risk value `door` becomes `physical-access`; each risk value and "task authorizer" is defined in the glossary; governance is a group of records beside the four layers.
- An agent skips the model query only when the change touches no record with `risk` or an approval.

## Wave 8: Implementation (Part J)

- **Mark style per language (J3, IP3):** C# marks use the project's `xml` or `attribute` style; TypeScript uses the typed constant; YAML and other operations files use the comment. One shared parser reads all three, the reader records the style it saw, and `mark-style-invalid` compares styles within one language only.
- **Mark and folder (J4, IP4):** a backend element whose mark names a use case, automation or aggregate outside the module and folder it sits in is `mark-folder-drift` (S).
- **By symbol (J5, IP5):** marks are read through the semantic model (the generated mark attribute by its full name, a handler by the interface it implements); a public state-changing method on an aggregate root needs a `use-case` mark even when it raises no event (`mark-missing`).
- **Provision identity (J6, IP6, D42):** the `**ID:**` line is a provision's identity; the heading may change without changing it. `litespec-maintain validate` reports a duplicate ID, and an ID present at the highest release tag that is gone now and not listed in the `## v3.0.0` section of `CHANGELOG.md` under "Removed and Renamed Provisions" (old ID, then the new ID or "removed").
- **Size-aware Defaults (J7, IP7):** the five application projects, property tests, model-based tests and the mutation threshold become a Default and Replacement by size as IP7's table states; an equivalent mutant is recorded in an `exception` of kind `gap` covering the mutation finding.
- **Honest enforcement (J8, IP8):** every Statement carries Checked by naming an existing finding code, command or shipped file, or Reviewed for as a question; `litespec-maintain validate` reports a Checked by line naming a code, command or file that does not exist (`CHECKED_BY_UNKNOWN`).
- **One page (J9, IP9):** `docs/backend/write-code-for-a-use-case.md`, title "Write Code for a Use Case", holds the element table (where each element goes, the mark it carries, what checks it); the mark rules keep their one home on `docs/core/model.md`, and `system.md` and the stack pages link to both.

## Wave 6R: Reaction and Job Return (Owner Direction, 2026-10-31)

The owner reversed BP4 (D27): "reaction" and "job" are more explicit and clearer when reading the code. The `automation` record type goes; `reaction` and `job` return as two record types. Every other wave 6 change stays: sagas, doors, typed inputs and payloads, privacy, scenarios, the effect verbs `runs` and `emits`, and the unified `onFailure`.

| Record | Identifier and home | Fields |
|:---|:---|:---|
| reaction | `reaction/<module>.<name>`, `docs/behavior/modules/<module>/reactions/<name>.md`; name form `<verb>-<what>-on-<fact>` | `triggeredBy` (a list of `event/...` or `integration-event/...` ids; was `on`, B2), `skippedWhen` (was `if`, B2), `effects`, `reads`, `changes`, `fulfills`, `serves`, `idempotency`, `onFailure`, `risk` |
| job | `job/<module>.<name>`, `docs/behavior/modules/<module>/jobs/<name>.md`; name form `<verb>-<what>` with the verb of the use case it runs | `kind` (`scheduled`, `one-off`, `manual`; `manual` was `operator`, D32), `schedule` (required with `scheduled`), `selection`, `batchSize`, `lease`, `cursor`, `runbook`, `runsWhere`, `guard` (required with `manual`), `serviceLevel`, `evidence`, `effects`, `reads`, `changes`, `fulfills`, `serves`, `idempotency`, `onFailure`, `risk` |

Code marks: an event handler carries `implements reaction/<...>`, a job host `implements job/<...>`.

Finding codes (B8 form): `automation-name-invalid` splits back into `reaction-name-invalid` and `job-name-invalid`; `automation-trigger-unknown` becomes `reaction-trigger-unknown`; `automation-unimplemented` splits into `reaction-unimplemented` and `job-unimplemented`; `automation-guard-missing` becomes `job-guard-missing`.

Pages: `docs/core/automation.md` keeps its path and title "Automation" as the page for reactions and jobs together; "automation" is the page's subject, never a record type. Its provisions name reactions and jobs. A saga's `drivenBy` names `{ "job": "job/<...>" }` or `{ "reaction": "reaction/<...>" }` or a person. `litespec.project.json` `proof.automationTriggerSteps` becomes `proof.jobTriggerSteps`.
