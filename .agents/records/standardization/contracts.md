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
