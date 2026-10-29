# Project Model

## Intent

A LiteSpec project describes itself with records, written statements of fixed types joined by fixed links. This page states the four record layers, the record types with their identifiers and homes, the common fields, the links, the marks, the authority order, the authoring order, the statuses, the approval data, the risk tiers, the questions, the transcription mode, the model builder, the check tiers, and the book and the diffs. People write and approve Intent, agents write Behavior from it, and the model builder computes how code and tests connect to the records.

## Concepts

### The Four Record Layers

Each record layer answers one question, is written from the record layer above it, and owns its records. Nothing above Implementation is computed from code.

| Record layer | Question | Writes | Reviews | Records |
|:--|:--|:--|:--|:--|
| Intent | Why build it, and what do we promise? | agents draft, the founder decides | the founder and product people | product, goal, problem, feature, requirement, constraint, decision, research, journey, actor, term (`layer: intent`) |
| Behavior | How does the system behave to keep the promises? | agents | developers; the security reviewer for access, outside calls and personal-data flows | module, aggregate, rule, event, integration-event, use-case, result, scenario, reaction, job, notification, setting, role, permission, entry-point, external-system, runbook, term (`layer: behavior`) |
| Implementation | How is it built? | agents | S-tier checks; developers sample | code only |
| Proof | How do we know it holds? | the builder | G-tier and S-tier checks | tests, check results, computed statuses, the rendered book |

Governance records attach to any record layer: `deviation` and `known-gap`.

Three translation rules join the record layers. Specify moves Intent to Behavior: it adds states, invariants, refusals, consistency, data classes with their erasure positions, freshness, and access with tenant scope, and it leaves open any price, fee, rate, term, market, payer, retention period for personal data, observable freshness, or default or figure that differs by tenant, market, or date, each as a draft question. Implement moves Behavior to Implementation: it adds the code shapes the stack pages fix, and adds no observable outcome no result declares, no edge no record declares, no write outside `changes`, and no access wider than the grant. Verify moves Implementation to Proof: one test proves each scenario through the public boundary, and a proof goes stale when its scenario semantic hash changes.

### Record Types and Page Styles

Each type answers one question.

| Type | Record layer | Question |
|:--|:--|:--|
| `product` | intent | What is built, for whom, and what is excluded? |
| `goal` | intent | Which measurable outcome does the product serve? |
| `problem` | intent | Which pain does someone have today? |
| `feature` | intent | What is offered against a problem? |
| `constraint` | intent | Which outside rules hold? |
| `research` | intent | Which outside facts inform a choice? |
| `decision` | intent, implementation | Which choice holds, and what changes it? |
| `requirement` | intent | What does the product do or guarantee? |
| `journey` | intent | Which ordered steps reach one goal? |
| `actor` | intent | Who or what touches the product? |
| `module` | behavior | Which business areas exist? |
| `aggregate` | behavior | Which things stay consistent? |
| `rule` | behavior | What holds? |
| `use-case` | behavior | Which use cases does the product offer? |
| `result` | behavior | How does a use case end? |
| `scenario` | behavior | Which example proves the result? |
| `event` | behavior | What happened? |
| `integration-event` | behavior | Which facts cross the boundary? |
| `reaction` | behavior | When something happens, what follows? |
| `job` | behavior | Which work runs on a schedule or by hand? |
| `notification` | behavior | Which person-facing messages go out? |
| `setting` | behavior | Which adjustable values hold, and who sets them? |
| `role` | behavior | Which permission sets exist? |
| `permission` | behavior | Which single actions are allowed? |
| `entry-point` | behavior | How do people reach the product? |
| `external-system` | behavior | Which outside system exchanges what? |
| `runbook` | behavior | How is it run or recovered? |
| `deviation` | any | Where does it depart, and until when? |
| `known-gap` | any | What is missing, who owns it, and until when? |
| `term` | intent, behavior | Which word means what? |

The [intent page](intent.md) states the Intent records, the [behavior page](behavior.md) states the Behavior records, and the [access page](access.md) states the access records.

Each record page uses the page style the [Authoring Standard](authoring.md) names for its type. A setting-set page holds many settings, and the retention-schedule page holds the retention rows at `docs/behavior/retention-schedule.md`. The retention schedule is a page style, not a record type.

### The Docs Layout

A consumer keeps its documentation in one folder per record layer, so the folder a page sits in says who writes it, who reviews it, and what it may contain.

| Folder | Record layer | Holds |
|:---|:---|:---|
| `docs/intent/` | Intent | The brief, goals, problems, features, requirement pages by area, constraints, decisions, journeys, actors, research, and the product documents the business publishes, such as legal texts and the writing voice |
| `docs/behavior/` | Behavior | Modules with their aggregates, use cases, reactions, jobs, notifications and settings; rule pages; access (roles, permissions, entry points); external systems; integration events; the retention schedule; runbooks and operating procedures |
| `docs/implementation/` | Implementation | The technical decisions under `decisions/`, and guides for people working in the code: frontend architecture, the command reference, the published API guide, and setup guides. The guides describe code and hold no records |
| `docs/proof/` | Proof | Evidence the tools compute or a run leaves behind: generated registers, gate files, and operating logs. No person writes a fact here that a tool can compute |
| `docs/governance/` | Governance | Deviations and known gaps, which can attach to any record layer |

The glossary (`docs/glossary.json`) and the example cast stay at the top of `docs/`, because every record layer uses them.

### Pages and Entries

A page record is a Markdown file whose opening block names a record type. An entry record is an object in the `records` array of that block. Terms are the exception: each term is an entry in the glossary. Each type has one home.

| Type | Stored as | Home |
|:---|:---|:---|
| `product` | page, exactly one | `docs/intent/brief.md` |
| `goal` | page | `docs/intent/goals/<name>.md` |
| `problem` | page | `docs/intent/problems/<name>.md` |
| `feature` | page | `docs/intent/features/<name>.md` |
| `constraint` | page | `docs/intent/constraints/<name>.md` |
| `research` | page | `docs/intent/research/<name>.md` |
| `decision` | page | `docs/intent/decisions/<number>-<name>.md`; a technical decision `docs/implementation/decisions/<number>-<name>.md` |
| `requirement` | entry | `docs/intent/<area>/<topic>.md` |
| `journey` | page | `docs/intent/journeys/<name>.md` |
| `actor` | page | `docs/intent/actors/<name>.md` |
| `module` | page | `docs/behavior/modules/<module>/README.md` |
| `aggregate` | page | `docs/behavior/modules/<module>/<aggregate>/README.md` |
| `rule` | entry | On its aggregate page; a cross-aggregate rule on a rule page under `docs/behavior/rules/` |
| `use-case` | page | Beside its aggregate: `docs/behavior/modules/<module>/<aggregate>/<use-case>.md`; a query describing no one aggregate under `reads/` beside its module |
| `result` | entry | In its use case page |
| `scenario` | entry | In its use case page, or in its reaction page |
| `event` | entry | On its aggregate page |
| `integration-event` | page | `docs/behavior/integration-events/<name>.md` |
| `reaction` | page | `docs/behavior/modules/<module>/reactions/<reaction>.md` |
| `job` | page | `docs/behavior/modules/<module>/jobs/<job>.md` |
| `notification` | page | `docs/behavior/modules/<module>/notifications/<name>.md` |
| `setting` | entry | On a setting-set page: `docs/behavior/modules/<module>/settings.md`, or `docs/behavior/settings/<name>.md` for platform-wide settings |
| `role` | page | `docs/behavior/access/roles/<name>.md` |
| `permission` | entry | `docs/behavior/access/permissions.md` |
| `entry-point` | entry | `docs/behavior/access/entry-points.md` |
| `external-system` | page | `docs/behavior/external-systems/<name>.md` |
| `runbook` | page | `docs/behavior/runbooks/<name>.md` |
| `deviation` | page | `docs/governance/deviations/<name>.md` |
| `known-gap` | page | `docs/governance/known-gaps/<name>.md` |
| `term` | entry in the glossary | `docs/glossary.json` |

### Identifiers

An identifier names its record type first, then the record name, with the owning home in between when the type has one. The name carries one to six lowercase hyphenated words, chosen by a person; a decision counts its number as a word.

| Type | Form | Example |
|:---|:---|:---|
| `product` | `product/<name>` | `product/claimwick` |
| `goal` | `goal/<name>` | `goal/faster-reimbursement` |
| `problem` | `problem/<name>` | `problem/expenses-wait-weeks` |
| `feature` | `feature/<name>` | `feature/approval-reminders` |
| `constraint` | `constraint/<name>` | `constraint/tax-receipt-retention` |
| `research` | `research/<name>` | `research/approval-time-benchmarks` |
| `decision` | `decision/<number>-<name>` | `decision/0001-receipt-threshold` |
| `requirement` | `requirement/<topic>.<name>` | `requirement/approval.receipt-over-threshold` |
| `journey` | `journey/<name>` | `journey/submit-and-reimburse` |
| `module` | `module/<name>` | `module/spending` |
| `aggregate` | `aggregate/<module>.<name>` | `aggregate/spending.expense` |
| `rule` | `rule/<home>.<name>` | `rule/expense.approver-is-not-submitter` |
| `use-case` | `use-case/<module>.<name>` | `use-case/spending.submit-expense` |
| `result` | `result/<use-case-name>.<name>` | `result/submit-expense.receipt-missing` |
| `scenario` | `scenario/<use-case-name>.<name>` | `scenario/submit-expense.submitting-with-a-receipt-succeeds` |
| `event` | `event/<aggregate-name>.<past-tense>` | `event/expense.submitted` |
| `integration-event` | `integration-event/<name>` | `integration-event/expense-submitted` |
| `reaction` | `reaction/<module>.<name>` | `reaction/spending.remind-approver` |
| `job` | `job/<module>.<name>` | `job/spending.clear-stuck-reimbursements` |
| `notification` | `notification/<module>.<name>` | `notification/spending.approval-reminder` |
| `setting` | `setting/<module>.<name>` | `setting/spending.max-open-expenses` |
| `actor` | `actor/<name>` | `actor/employee` |
| `role` | `role/<name>` | `role/approver` |
| `permission` | `permission/<name>` | `permission/approve-expense` |
| `entry-point` | `entry-point/<name>` | `entry-point/claimwick-app` |
| `external-system` | `external-system/<name>` | `external-system/payout-bank` |
| `runbook` | `runbook/<name>` | `runbook/clear-stuck-reimbursements` |
| `deviation` | `deviation/<name>` | `deviation/export-review-bypass` |
| `known-gap` | `known-gap/<name>` | `known-gap/export-expenses-unimplemented` |
| `term` | `term/<name>` | `term/expense` |

A rule home is an aggregate name or a module name. A use case name is unique across the project, so a result cites only that name. An aggregate name is unique across the project, so a rule cites only that name. A reaction, job, notification, or setting is filed with its module, and an integration event belongs to the published set. A rule name keeps the negation its statement makes. Every identifier citation resolves to a written record.

A journey step id is `journey/<name>#<step-id>`, with step ids `s1`, `s2`, and so on. A derived generic result is `result/<module>.<use-case>.forbidden` or `result/<module>.<use-case>.invalid-input`. Scenarios can point at derived results. Derived results are never written as records.

### Common Fields

Every written record carries the shared fields in this table. Entries inherit `owner` and `status` from their page unless they set their own.

| Field | Written | Meaning |
|:---|:---|:---|
| `type` | always | The record type in kebab case |
| `id` | always | The identifier |
| `title` | always | The display title, free to change |
| `owner` | pages; entries inherit | One name from the closed owner list |
| `status` | pages; entries inherit | Draft, approved or reviewed, or retired, in the record-layer vocabulary |
| `decider` | on drafts | The owner deciding by the date |
| `decideBy` | on drafts | The decision date in ISO form |
| `replaces` | decision, when replacing | The decision this decision replaces |
| `terms` | when used | The term identifiers the record uses |
| `questions` | when open | Open questions, each with `question`, `decider`, `decideBy`, `recommended`, `ballot`, and `tier`; an open question blocks approval |
| `approvedBy` | on approved and reviewed | The approver: a person owner name, or `auto (veto window)` |
| `approvedOn` | on approved and reviewed | The approval date in ISO form |
| `approvedHash` | on approved and reviewed | The builder hash over the semantic fields |
| `risk` | on requirements and Behavior records, when present | Any of `money`, `door`, `personal-data`, `access` |
| `source` | during adoption, when transcribed | `transcribed` for a Behavior record written from code |

### Links

Each written link lives as a field on the record it starts from: a decision carries `decides`, a rule carries `fulfills`, and so on. The table names every written link field, the record that carries it, and what it points at.

| Field | Written on | Points at |
|:---|:---|:---|
| `decides` | decision | Requirements, features, rules, constraints, and the permissions, roles, actors, deviations, entry points, settings, or decisions an engineering choice settles |
| `basedOn` | decision, requirement, rule | Constraints, research, and on a technical decision the requirements it serves |
| `replaces` | decision | The replaced decision |
| `audience` | product | Actors |
| `affects` | problem | Actors |
| `solves` | feature | Problems |
| `serves` | feature | Goals |
| `serves` | use case, reaction, job | Journey steps or goals |
| `feature` | requirement | The planned, later, or refused feature that carries it |
| `fulfills` | rule, use case, reaction, job, notification, setting, journey | Requirements |
| `governs` | rule | Aggregates, use cases |
| `refusal` | rule | The refusal result carrying its code |
| `raises` | use case | Events |
| `access.grant` | use case | A permission, a self scope, anonymous, or system |
| `reachedThrough` | use case | Entry points |
| `changes` | use case | Aggregates |
| `reads` | use case, reaction, job | Aggregates |
| `effects` | use case, reaction, job | Use cases, notifications, integration events, external system operations |
| `on` | reaction | Domain events or integration events |
| `holds` | actor | Roles at a scope |
| `allows` | role | Permissions |
| `actor` | journey, external system | The actor walking the journey; the actor the system acts as |
| `translates` | integration event | Domain events |
| `dependsOn` | module | Modules |
| `runbook` | job | The runbook answering the job |
| `provision` | deviation | The LiteSpec provision it departs from |
| `subject` | known gap, deviation | The records the gap or deviation covers |
| `terms` | any record | Terms |

Behavior records point up: `serves` points to a goal or a journey step, and `fulfills` points to a requirement. Records never point down: a journey names no use case, and a requirement names no rule.

Containment needs no link field: it follows the identifier. An aggregate belongs to the module its identifier names, a reaction, job, notification, or setting belongs to the module its identifier names, and a result or a scenario belongs to the use case its identifier names. An integration event belongs to the published set. The builder computes the remaining links from code, routes, and tests: `implements` from a code mark to the record it names, `follows` from a code mark to the provision, decision, or deviation that shapes the code, `calls` from a screen to a use case, and `proves` from a test mark to a scenario or rule. Code and tests point up to the record they serve through their marks, with each handler implementing exactly one record.

### Marks

A mark is the one written link from a code element to the record it serves. It holds the record identifier, and every tool reads it the same way: the model builder and the analyzers. A folder path stays the map a reader follows through the code, and it is never the link.

| Record layer the code serves | Code element | Mark names |
|:--|:--|:--|
| Domain | Aggregate root | `aggregate/<module>.<name>` |
| Domain | Aggregate root method that changes state | `use-case/<module>.<name>`, exactly one: the use case whose act it performs |
| Domain | Domain event | `event/<aggregate>.<fact>` |
| Domain | Refusal type for one rejected rule | `rule/<home>.<name>` |
| Domain | Domain service holding one rule calculation | `rule/<home>.<name>` |
| Application | Command or query handler | `use-case/<module>.<name>`, exactly one |
| Application | Event handler | `reaction/<module>.<name>` |
| Application or Infrastructure | Job host | `job/<module>.<name>` |
| Application | Port for an external system | `external-system/<name>` |
| Infrastructure | Adapter implementing that port | `external-system/<name>` |
| API | Endpoint | `use-case/<module>.<name>` |
| Frontend | Use-case module | `use-case/<module>.<name>` |
| Tests | Test | The lowest record it verifies: `scenario/<use-case>.<name>` or `rule/<home>.<name>` for behavior; `requirement/<topic>.<name>` of kind quality; `decision/<number>-<name>` for a technical choice; `external-system/<name>` or `integration-event/<name>` for an outside agreement; `litespec/<page>#<slug>` for a provision the code must keep |
| Application, Infrastructure or API | Technical class: a class that declares a method and takes no other mark | One or more: a Behavior record it serves in an `implements` mark, or `litespec/<page>#<slug>`, `decision/<number>-<name>` or `deviation/<name>` in a `follows` mark |

A code element serving a record names it in an `implements` mark. A technical class names the record it serves in an `implements` mark, as an input validator names its use case and a repository its aggregate. Where it serves no record, it names the reason it is built the way it is in a `follows` mark: the LiteSpec provision that requires its shape, the technical decision that chose it, or the deviation that allows it. Any other code element may carry a `follows` mark as well.

The tools recognize most code elements from the code itself and then expect their mark. A port and a domain service are recognized by their mark, a job host by its hosted-service shape or by its mark, and an aggregate root method by the event it raises or by its mark, so an unmarked port is found only from the top, through its external system, and an unmarked method that changes state without raising an event is not recognized at all.

Six provisions divide the marks between them: `litespec/core/system#point-code-and-tests-up-to-records` requires the mark, `litespec/core/system#point-every-technical-class-up-to-its-record-or-choice` covers technical classes, `litespec/core/model#declare-one-mark-style` fixes the style, `litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark` checks each mapped record from the top, `litespec/core/model#name-the-lowest-record-each-test-proves` sets what a test names, and `litespec/backend/testing#mark-the-test-that-proves-each-scenario` places the test mark.

A project writes its marks in one mark style, declared as `marks` in `litespec.project.json`. Each stack page states the forms the style takes: the .NET stack writes an `xml` mark as a documentation tag and an `attribute` mark as a generated attribute, and a frontend writes a typed constant in either style. A runtime value a record already states, such as an event classification or a refusal code, is generated from the mark and the record where the stack generates code, and checked against the record where a person writes it.

### Authority

Intent outranks Behavior. When two records disagree, the higher one wins: constraint over product, product over requirement, requirement over rule, rule over use case, use case over result, and result over scenario. Inside one record, a must beats a should. Between peers, the later approved decision wins, and its `replaces` link shows what it overturned. Every other link is trace: it explains why a record exists and never decides a conflict.

A decision explains a choice and is replaced, never edited. Research, examples, and prose carry no authority, so they never override an approved record. A deviation outranks the LiteSpec provision it names for the project that records it, until its review date.

When code and the specification disagree, the specification wins where Intent has spoken. Where Intent is silent, working code is evidence for a question, never a defect to delete and never a source to transcribe silently.

A record stays unapproved while a decision behind it stays draft. An accepted gap becomes a known gap, and its check reports without failing until the end date. Module order states dependencies and grants no authority.

### Authoring Order

Records are authored top-down, one slice at a time:

1. Product intent: brief, goals, problems, features, constraints, decisions, requirements.
2. Then Behavior: modules, aggregates with states, transitions and data, rules, use cases, results, scenarios, reactions, jobs, notifications, settings.
3. Actors, roles, permissions and the glossary grow beside both.
4. One slice at a time: one feature from problem to scenario, then the next.
5. A question found while modeling goes up as a draft question with a decider and a date; the Behavior record stays draft until it is answered.

### Statuses

Intent and governance records use draft, approved, and retired. Behavior records use draft, reviewed, and retired. Draft means the record awaits its decider, approved or reviewed means it holds, and retired means a replacement holds. A draft names its `decider` and its `decideBy` date. A decision is replaced, never edited: the retired decision stands beside its replacement so the reasoning stays readable.

Implemented, verified, stale, and reachable are computed, never handwritten. The builder derives them from code, screens, tests, and test runs on every run:

| State | Of | When |
|:--|:--|:--|
| implemented | use case, rule, event, aggregate state | the stack builder finds code for it |
| verified | use case | every scenario has a confirmed, passing proof through the public boundary that is not stale |
| tested | quality requirement, technical decision, external system, integration event | a test proving it passes; tested never makes a use case verified |
| verified | requirement | everything fulfilling it is verified, and a measured requirement measure has a passing check |
| stale | scenario proof | the scenario semantic hash differs from the hash the builder recorded when it first read a passing run of the proof |
| reachable | aggregate state | a transition leads into it |

The builder also writes each record effective status: draft when the record, a decision deciding it, or a requirement it fulfills is a draft. A result or a scenario takes its use case effective status.

### Approval as Data

An approved or reviewed record carries its approval as data: `approvedBy` names a person owner or `auto (veto window)`, `approvedOn` names the date, and `approvedHash` is the builder hash over the semantic fields, written by `litespec model approve`. The semantic fields are the statements, results, output names, rules, and steps. Prose bodies and a term's translation labels sit outside the hash, so rewording prose or adding a language reopens no approval. `approval-reopened` fires when the semantic hash differs from `approvedHash`. [The approval hash guide](../guide/approval-hash.md) lists which fields feed the hash per record type.

### Risk Tiers

`risk` is optional on requirements and Behavior records, and holds any of `money`, `door`, `personal-data`, and `access`. Any value makes the record tier 1. A record with no risk is tier 3. Tier 1 needs explicit approval or review by a named person. A tier-3 draft question carrying `recommended` auto-approves after its `decideBy` plus the project veto window (`review.vetoWindowDays` in `litespec.project.json`, default 7), recorded as `approvedBy: auto (veto window)`.

### Questions and Ballots

`questions` carries `question`, `decider`, `decideBy`, `recommended`, `ballot`, and `tier`. `ballot` groups questions in the inbox, by a decision id or a short topic slug. An open question blocks approval. A draft past its `decideBy` that is not auto-approvable is reported as a known gap with its owner, and never silently blocks. Each ballot carries a consequences summary rendered from Behavior, marked as context and outside the approval hash, so Intent stays enough to approve. `litespec book` writes a ballots page, listing questions grouped by `ballot`, ordered tier 1 first, with each question recommendation and auto-approval date. The [release page](release.md) states the review flow.

### Transcription Mode

During adoption, a Behavior record written from code carries `source: transcribed`. Such a record can stand as draft, but never as reviewed, until its `fulfills` or `serves` parent is approved. `litespec.project.json` `adoption.transcriptionEndsWith` names the decision that ends the mode. After that decision is approved, `transcribed-after-adoption` fails on any remaining transcribed record.

### Size and Stacks

Each project declares its size, its stacks, and its optional types in `litespec.project.json`. Each frontend declares one stack, and the backend declares its own. Every project answers whether constraints apply, possibly with none. Owners form a closed list, and extensions name their replacements and switched-on types. The [scope page](scope.md) states how a project selects its size, shape, and stacks.

### The Model Builder

The model builder reads records, code, routes, tests, and test runs into one model. It writes the model file, runs the model checks, and renders generated regions. The [model command page](../tools/model.md) lists the subcommands and finding codes.

Generated regions sit inside fences that only the builder writes and prose checks skip. Each project runs the builder in its own gate, so handwritten prose and computed views stay in one page without mixing.

A finding that an open known gap covers is reported as known and exits zero. The gap covers the finding when its `check` equals the finding code and its `subject` lists the finding identifier. An expired known gap fails the check.

`litespec model diff <base>` prints the records a change adds, changes and removes, in the book words, for review.

### Check Tiers

Every check has a tier.

| Tier | Meaning |
|:--|:--|
| G | Reads records only. Always blocks. |
| S | Reads code through a stack reader. Blocks where the stack reader is qualified (.NET today), and reports otherwise. |
| A | Advisory. Reports and never blocks. |

`litespec model check` prints the tier beside each finding.

An S finding about a record follows the record effective status: it blocks when the record is approved or reviewed, and reports without blocking while the record is a draft, so code written ahead of a draft never fails the build and never goes unreported. `check.phase` in `litespec.project.json` names the project phase: `working`, the default, or `frozen`, when the project holds its tests still while the code moves, so findings about tests report without blocking. An analyzer reports a blocking finding as an error and a reported one as a warning, and a finding that a known gap or a deviation covers stays silent.

The [model command page](../tools/model.md) lists every finding code with its tier and meaning.

### The Book and the Diffs

The book and the diffs live in the Proof record layer. The book renders each journey as one chapter: the goal, `finishedWhen`, the steps as sentences, `ifItGoesWrong`; the requirements it fulfills, with their statements and their `except` and fallback inline; for each step, the serving use case goal in one line; the Then clauses of its scenarios, as context; a consequences box, as context, naming new refusals in product words, data kept and for how long, and money effects; and the open questions.

Field tables render as tables. Scenarios group under their result. Field names never render literally in Intent chapters.

`litespec model diff <base> --intent` shows Intent changes in product words. `--behavior` shows Behavior changes and the security queue. Without a flag, it shows both. The security queue lists tenant scope `cross` access, new `calls` effects, and new personal or sensitive data classes for the security reviewer named in `review.securityReviewer`.

## Standards

### Write Every Record in Its Home

**ID:** `litespec/core/model#write-every-record-in-its-home`

**Statement:** A project MUST keep every record in the home its type names.

**Rationale:** One home per type tells a reader where to open a record and tells the builder where to read it. A record kept anywhere else is invisible to the checks that scan its home, so it looks sound while nothing validates it.

**Example:** A decision lives at `docs/intent/decisions/0001-receipt-threshold.md`.

**Checked by:** `litespec model check` reports `record-shape` for a record outside its home.

### State the Common Fields on Every Record

**ID:** `litespec/core/model#state-the-common-fields-on-every-record`

**Statement:** Every written record MUST carry its type, identifier, title, owner, and status.

**Rationale:** Type, identifier, title, owner, and status are the fields every join needs. Without them on every record, the builder cannot connect decisions to requirements, scenarios to results, or records to owners, and the model falls apart into loose pages.

**Checked by:** `litespec model check` reports `record-shape` for a record missing a common field.

### Use the Layer Status Vocabulary

**ID:** `litespec/core/model#use-the-layer-status-vocabulary`

**Statement:** Every written record MUST carry the status vocabulary of its record layer.

**Rationale:** Intent and governance records use draft, approved, and retired, while Behavior records use draft, reviewed, and retired. One vocabulary per record layer tells a reader whether a person approved the promise or reviewed the behavior. A Behavior record marked approved claims a founder decision nobody made.

**Checked by:** `litespec model check` reports `invalid-field` for a status outside the record layer vocabulary.

### Hold Approval Behind Open Questions

**ID:** `litespec/core/model#hold-approval-behind-open-questions`

**Statement:** A record MUST NOT stand approved or reviewed while it carries an open question.

**Rationale:** An open question names a fact the record still needs, with the decider and the date that will settle it. Approving the record first would claim settled intent while the missing fact can still move it. Answering the question or retiring the record clears the gate. A question that auto-approves after the veto window is closed, not open, so the gate still holds.

**Checked by:** `litespec model check` reports `record-shape` for an approved or reviewed record carrying an open question.

### Record Approval as Data

**ID:** `litespec/core/model#record-approval-as-data`

**Statement:** An approved or reviewed record MUST carry approvedBy, approvedOn, and approvedHash over its semantic fields.

**Rationale:** Approval as data tells every reader who decided, when, and over which content. The hash covers statements, results, output names, rules, and steps, and never prose bodies, so a typo fix reopens no approval while a moved promise does. `approval-reopened` fires when the semantic hash differs from the stored hash.

**Example:** An approved requirement carries its approver, its date, and its hash.

```json
{
  "status": "approved",
  "approvedBy": "ava-reyes",
  "approvedOn": "2026-10-01",
  "approvedHash": "9f2c41ab"
}
```

**Checked by:** `litespec model check` reports `approval-reopened` for a record whose semantic fields changed after approval.

### Approve Tier 1 Records by a Person

**ID:** `litespec/core/model#approve-tier-1-records-by-a-person`

**Statement:** A tier 1 record MUST carry explicit approval or review by a named person.

**Rationale:** A record carrying money, door, personal-data, or access risk decides what money moves, who passes, whose data is kept, or who may act. Only a named person weighs those consequences; the veto window auto-approval never decides them. Tier 3 records carry no risk, so their draft questions can auto-approve after the window.

**Reviewed for:** Which tier 1 records lack a named personal approver? Look at `approvedBy` on records carrying risk.

### Write Links on the Record That Points

**ID:** `litespec/core/model#write-links-on-the-record-that-points`

**Statement:** A link MUST live as fields on the record it starts from.

**Rationale:** One writing site keeps a link and its source in one edit: changing the source updates the link in the same change. Links written on the target instead would scatter each relationship across every page it touches.

**Checked by:** `litespec model check` reports `reference-unresolved` for a link whose target no record carries.

### Connect Every Use Case to Intent

**ID:** `litespec/core/model#connect-every-use-case-to-intent`

**Statement:** A use case MUST carry serves, fulfills, or both.

**Rationale:** A use case with no served journey step or goal and no fulfilled requirement answers to nothing above it: no promise explains why it exists. Pointing up with serves or fulfills ties every behavior to the Intent that asks for it. `record-orphaned` fails a use case carrying neither.

**Checked by:** `litespec model check` reports `record-orphaned` for a use case with neither `serves` nor `fulfills`.

### Serve Every Journey Step

**ID:** `litespec/core/model#serve-every-journey-step`

**Statement:** Every journey step MUST have a serving use case.

**Rationale:** A step no use case serves promises a moment the product never builds. Use cases point up with serves, so each step id collects the behaviors that carry the walker through it. `step-unserved` fails a step with no serving use case.

**Checked by:** `litespec model check` reports `step-unserved` for a journey step with no serving use case.

### Follow the Authority Order

**ID:** `litespec/core/model#follow-the-authority-order`

**Statement:** A conflict MUST resolve toward the higher authority.

**Rationale:** Intent records outrank Behavior records: constraint over product, product over requirement, requirement over rule, rule over use case, use case over result, and result over scenario. The order settles a conflict without a meeting: a use case asking for behavior its rule forbids loses to the rule. Where Intent has spoken, the specification wins over code; where Intent is silent, working code is evidence for a question. Decisions explain choices and research informs them, but neither outranks the ladder, so prose never silently overrides an approved record.

**Reviewed for:** Conflict decisions under `docs/intent/decisions/` name the higher record that won, in the order this page states.

### Resolve Ties by the Later Decision

**ID:** `litespec/core/model#resolve-ties-by-the-later-decision`

**Statement:** A tie MUST resolve toward the later approved decision.

**Rationale:** Two records at one level can disagree with no higher record to settle them. The later approved decision wins because it decided with knowledge of the earlier one. When no later decision separates them, their shared owner decides, so every tie still has exactly one judge.

**Reviewed for:** Each tie-breaking decision carries a later approval date than the peer records it settles.

### Hold Approval Behind Draft Decisions

**ID:** `litespec/core/model#hold-approval-behind-draft-decisions`

**Statement:** A record MUST NOT stand approved or reviewed behind a draft decision.

**Rationale:** Approval means the reasoning behind the record already holds. A record approved behind a draft decision claims settled intent while its foundation can still move. The builder also writes each record effective status: draft when the record, a decision deciding it, or a requirement it fulfills is a draft. A result or a scenario takes its use case effective status.

**Checked by:** `litespec model check` reports `draft-gate` for an approved or reviewed record behind a draft decision.

### Record Accepted Gaps as Known Gaps

**ID:** `litespec/core/model#record-accepted-gaps-as-known-gaps`

**Statement:** An accepted gap MUST live as a known gap with owner and end date.

**Rationale:** Missing work with no owner and no date is invisible: nobody answers for it and nothing reports it. A known gap with an owner and an end date turns the same missing work into a visible promise that the checks report until the date and fail after it.

**Example:** The running Claimwick example accepts that exporting expenses has no handler yet, so the gap lives as a known gap with an owner and an end date in `docs/governance/known-gaps/export-expenses-unimplemented.md`.

```json
{
  "type": "known-gap",
  "id": "known-gap/export-expenses-unimplemented",
  "title": "Export expenses unimplemented",
  "owner": "claimwick-team",
  "status": "approved",
  "check": "use-case-unimplemented",
  "subject": ["use-case/spending.export-expenses"],
  "endsBy": "2027-06-01",
  "reason": "Export waits for the finance file format."
}
```

The `use-case-unimplemented` check reports without failing until 2027-06-01, and the expenses team answers for closing the gap, which satisfies this provision.

**Checked by:** `litespec model check` reports `record-shape` and `known-gap-expired` for a gap missing its owner, date, or renewal.

### Apply Guidance Precedence

**ID:** `litespec/core/model#apply-guidance-precedence`

**Statement:** A conflict MUST resolve toward the project deviation, the extension, the deeper page, or the shallower page, in that order.

**Rationale:** A project requirement can never quietly override a shared standard; without this order it would, one page at a time. The conflict becomes a deviation record instead: dated, reasoned, and reviewable. The order puts the project deviation first, then the extension, then the deeper page, then the shallower page, so the most local approved decision wins.

**Example:** The running Claimwick example runs its weekend export with no reviewer on shift, so it records a project deviation that outranks the shared standard in `docs/governance/deviations/export-review-bypass.md`.

```json
{
  "type": "deviation",
  "id": "deviation/export-review-bypass",
  "title": "Export Review Bypass",
  "owner": "claimwick-team",
  "status": "approved",
  "provision": "litespec/backend/application#enforce-target-authorization-in-the-pipeline",
  "reason": "The weekend export runs with no reviewer on shift.",
  "reviewBy": "2027-01-01",
  "removeWhen": "A reviewer covers weekends."
}
```

The record names the provision it departs from, the reason, and the review date, so the departure stays visible and dated, which satisfies this provision.

A deviation that governs code no record describes names those files in `code`, as paths from the repository root. A deviation that governs records names them in `subject`, as a known gap does. While the deviation is approved and its `reviewBy` date has not passed, `model check` reports the findings on those files and records, and on the results and scenarios a named use case holds, as known instead of failing on them; after the review date they fail again.

**Reviewed for:** Every departure from a shared provision resolves to a deviation record under `docs/governance/deviations/` with reason and review date.

### Keep One Owner and Status on Every Record

**ID:** `litespec/core/model#keep-one-owner-and-status-on-every-record`

**Statement:** Every page MUST name one owner and one status.

**Rationale:** One owner means one person answers for the page. One status means the page either awaits decision, holds, or stands replaced. Entries inherit both from their page, so a page of entries stays consistent without repeating the fields on every object. The owner is the readable name the project owners list holds, and the project file maps that name to its CODEOWNERS handles.

**Example:** A draft names its `decider` and `decideBy` date. The three statuses and what each allows: a draft awaits its decider, approved or reviewed holds, and retired means a replacement holds.

**Checked by:** `litespec model check` reports `owner-unknown` and `draft-without-decider` for a page missing its owner or draft decider.

### Retire Records with Replaces

**ID:** `litespec/core/model#retire-records-with-replaces`

**Statement:** A replacing decision MUST name the decision it replaces, and no other record type carries `replaces`.

**Rationale:** Only decisions keep the past: the retired decision stands beside its replacement so the reasoning stays readable. Every other record changes in place, because retired copies of rules or use cases would fork the intent readers must follow. A rename rewrites every citation in the same change, so no dangling reference survives.

**Checked by:** `litespec model check` reports `invalid-field` for `replaces` outside a decision and `reference-unresolved` for an unknown target.

### Name What Every Decision Decides

**ID:** `litespec/core/model#name-what-every-decision-decides`

**Statement:** Every product decision that is not retired MUST name at least one record it settles in `decides`.

**Rationale:** The draft gate and the computed effective status both follow `decides`. A decision that names nothing holds back nothing, so its approval state constrains no record and its draft gate checks nothing. Naming at least one settled record gives the decision something to govern. A technical decision governs the code that follows it instead.

**Checked by:** `litespec model check` reports `decision-decides-nothing` for a product decision with an empty `decides` field.

### Record Each Technical Choice as a Decision

**ID:** `litespec/core/model#record-each-technical-choice-as-a-decision`

**Statement:** A technical choice that code follows and no LiteSpec provision makes MUST be a decision under `docs/implementation/decisions/` that at least one code element follows.

**Rationale:** Why a pattern, a library, or a configuration value was chosen is the first question a maintainer asks before changing it. Written in a comment, the answer drifts and no tool finds it. Written as a decision, it carries its options, what speaks for and against each, and what would reopen it, and every code element shaped by it names it in a `follows` mark. A choice a LiteSpec provision already makes needs no decision, because the provision is the record. Technical decisions sit under Implementation because they name technologies and code, which Intent never holds, and they share the numbered sequence of product decisions, so all decisions share one identifier space.

**Example:** Claimwick stores each expense as one document. `decision/0002-store-expenses-as-documents` weighs a document store against relational tables, chooses documents, and names what reopens the choice: a report that must join expenses across employees. The expense repository carries `/// <follows>decision/0002-store-expenses-as-documents</follows>`.

**Checked by:** `litespec model check` reports `decision-unfollowed` (tier S) for a technical decision that no code element follows. The check runs in the CLI only, with no analyzer rule: whether any code follows a decision is known only across every project, and one compilation sees one project.

### Keep Rules and Targets Out of Technical Decisions

**ID:** `litespec/core/model#keep-rules-and-targets-out-of-technical-decisions`

**Statement:** A technical decision MUST leave each rule the product obeys whatever its design to a constraint and each measurable target to a quality requirement, and name the ones it serves in `basedOn`.

**Rationale:** A technical decision chooses one way among alternatives, and a better alternative may replace it tomorrow. A rule such as keeping personal data in the European Union, or a target such as answering checkout within two seconds, does not change when the library does: overriding it needs a business, legal, or organizational change. Written inside a decision, the rule disappears the day the decision is replaced, and nobody approves it as the business promise it is. Kept in Intent, the rule and the target are approved by the person who owns them, the decision points up to them, and the chain runs unbroken from code through the decision to the constraint or requirement and the goal above it. A decision with no basis is advice, tier A, while a product decision that decides nothing blocks at tier G: a technical choice among equal options may serve no rule or target yet and still govern the code that follows it, but a product decision that settles no record governs nothing.

**Example:** Claimwick stores receipts in a European region because `constraint/receipts-stay-in-the-eu` requires it, and answers submissions quickly because `requirement/spending.month-end-submissions` sets the target. `decision/0002-store-expenses-as-documents` names both in `basedOn`; neither the region nor the two seconds is written in the decision.

**Checked by:** `litespec model check` reports `decision-without-basis` (tier A) for a technical decision whose `basedOn` names nothing, and counts a quality requirement as fulfilled when a technical decision names it in `basedOn`.

### Resolve Every Page Link

**ID:** `litespec/core/model#resolve-every-page-link`

**Statement:** Every Markdown link outside code on a page under the record roots MUST point at an existing file, by its relative path.

**Rationale:** A link that points at a moved file or a bare identifier reads as navigation and leads nowhere. Requiring every link to name an existing file by its relative path keeps the documentation clickable at every commit. Identifiers are cited in backticks instead, so the two never confuse.

**Checked by:** `litespec model check` reports `link-unresolved` for a page link naming no file.

### Declare Size Stacks and Optional Types

**ID:** `litespec/core/model#declare-size-stacks-and-optional-types`

**Statement:** A project MUST declare its size, stacks, and optional types.

**Rationale:** Size decides which record types and test projects the checks require, while stacks decide which technology pages and checks apply. Without the declaration, the builder cannot tell a missing record from one the project never needed. Optional types switch on per project, so the declaration also names which ones this project carries.

**Example:** The running Claimwick example declares large with a server shape and two stacks.

**Checked by:** `litespec model check` reports `size-type-missing` and `record-shape` for a project missing its declaration or a required type.

### Give Every Use Case Its Results

**ID:** `litespec/core/model#give-every-use-case-its-results`

**Statement:** A use case MUST end in a success result and name its refusals.

**Rationale:** Callers branch on named endings: success or a specific refusal with its code. Refusals stand in precedence order, so the first match wins. A refusal that stores its attempt carries recorded, and a success with recorded outcomes inside it carries codes. Generic refusals are derived, never written, so they need no entry. A use case with no named success leaves callers guessing what good looks like, and scenarios prove one ending each, so unnamed endings stay unproven.

**Example:** A submit use case names `submitted` and `receipt-missing`.

The running Claimwick example carries this in `use-case/spending.submit-expense`, which names `result/submit-expense.submitted` and `result/submit-expense.receipt-missing` with `scenario/submit-expense.submitting-with-a-receipt-succeeds`:

```json
"records": [
  {
    "type": "result",
    "id": "result/submit-expense.submitted",
    "title": "Submitted",
    "owner": "claimwick-team",
    "outcome": "success",
    "meaning": "The expense is submitted."
  },
  {
    "type": "result",
    "id": "result/submit-expense.receipt-missing",
    "title": "Receipt missing",
    "owner": "claimwick-team",
    "outcome": "refusal",
    "code": "spending.receipt-missing",
    "meaning": "The expense is above the receipt threshold and carries no receipt."
  },
  {
    "type": "scenario",
    "id": "scenario/submit-expense.submitting-with-a-receipt-succeeds",
    "title": "Submitting with a receipt succeeds",
    "owner": "claimwick-team",
    "status": "approved",
    "given": "An employee paid for a train ticket and kept the receipt.",
    "when": "The employee submits the expense with the receipt.",
    "then": "The expense is submitted.",
    "result": "result/submit-expense.submitted"
  }
]
```

Success names the submitted ending, the refusal carries the code `spending.receipt-missing`, and the scenario pairs one given and when with the success result, which satisfies this provision.

**Checked by:** `litespec model check` reports `use-case-without-success` and `scenario-result-unresolved` for a use case missing its success or a scenario naming no result.

### Derive Generic Refusals

**ID:** `litespec/core/model#derive-generic-refusals`

**Statement:** Generic refusals MUST be derived from access and input, never written as records.

**Rationale:** Forbidden and invalid input follow from the access grant and the input rules on every use case, so writing them by hand repeats one decision hundreds of times and drifts. The builder derives `result/<module>.<use-case>.forbidden` and `result/<module>.<use-case>.invalid-input`, and scenarios point at those ids. A written record with a generic id claims a decision the access grant already made.

**Example:** A scenario for `use-case/spending.submit-expense` points at `result/spending.submit-expense.forbidden` with no result record of that id on the page.

**Reviewed for:** Which result records carry a generic refusal id? Look for ids ending in `.forbidden` or `.invalid-input` beside the use case pages.

### Author One Slice at a Time

**ID:** `litespec/core/model#author-one-slice-at-a-time`

**Statement:** Authoring MUST complete one feature from problem to scenario before starting the next.

**Rationale:** A feature worked from problem to scenario stays reviewable at every level: each record lands with the Intent above it already written. Parallel slices leave Behavior records resting on Intent nobody wrote yet, and reviewers cannot tell a settled choice from a guess.

**Reviewed for:** Approved Behavior records show approved Intent above them through their `fulfills` and `decides` links.

### Raise Modeling Questions with a Decider and a Date

**ID:** `litespec/core/model#raise-modeling-questions-with-a-decider-and-a-date`

**Statement:** A question found while modeling MUST travel up as a draft question carrying its decider and its decision date.

**Rationale:** A Behavior record written around an open question bakes the guess into states, rules, and scenarios. Sending the question up with its decider, its date, its recommendation, its ballot group, and its tier keeps it owned and dated, while the Behavior record stays draft until the answer lands. A draft past its date that cannot auto-approve becomes a known gap with its owner, so it never silently blocks.

**Reviewed for:** Open modeling questions appear as draft questions carrying a decider and a date.

### Hold Transcribed Records in Draft

**ID:** `litespec/core/model#hold-transcribed-records-in-draft`

**Statement:** A record with source transcribed MUST NOT stand reviewed while its fulfills or serves parent is unapproved.

**Rationale:** Transcription copies working code into a record during adoption, so the record describes what exists rather than what Intent asked for. Draft holds it visibly unfinished until its fulfills or serves parent is approved. `transcribed-reviewed` fails a transcribed record standing reviewed.

**Checked by:** `litespec model check` reports `transcribed-reviewed` for a transcribed record standing reviewed.

### End Transcription at the Named Decision

**ID:** `litespec/core/model#end-transcription-at-the-named-decision`

**Statement:** Every transcribed record MUST clear before the adoption decision is approved.

**Rationale:** Transcription mode exists to adopt working code, not to write Behavior from code forever. `adoption.transcriptionEndsWith` names the decision that ends the mode, and approving it closes the door: `transcribed-after-adoption` fails on any record still carrying source transcribed. Clearing means the record earns its Intent parent and drops the transcribed mark, or retires with the code it copied.

**Checked by:** `litespec model check` reports `transcribed-after-adoption` for a transcribed record after the adoption decision is approved.

### Compute Proof Statuses in the Builder

**ID:** `litespec/core/model#compute-proof-statuses-in-the-builder`

**Statement:** Implemented, verified, stale, reachable, and effective status MUST come from the builder, never from handwriting.

**Rationale:** Handwritten status drifts: a record marked implemented stays marked after its handler is deleted. Computed status cannot drift, because the builder re-derives it from handlers, routes, scenarios, and test runs on every run. A use case reads implemented when a handler, reaction, job, or endpoint marked with it carries it out, verified when every scenario passes its proof, and stale when a scenario changed after its proof, and a requirement reads verified when everything fulfilling it is verified. Nothing above Implementation is computed from code: derivation from code into a record is forbidden, and code-computed facts live in the book and the model output.

**Example:** A use case reads implemented when its handler exists.

**Checked by:** `litespec model check` reports `use-case-unimplemented`, `proves-unconfirmed`, and `refusal-not-thrown` for a use case whose handler, proof, or refusal code is missing.

### Mark Every Check with Its Tier

**ID:** `litespec/core/model#mark-every-check-with-its-tier`

**Statement:** Every check MUST carry its tier and block or report as that tier states.

**Rationale:** The tier tells a reader what a finding means: G reads records only and always blocks, S reads code through a stack reader and blocks only where the reader is qualified, and A reports and never blocks. `litespec model check` prints the tier beside each finding, so a review tells a blocking defect from advice at a glance.

**Reviewed for:** Which findings lack a tier letter, or block outside their tier? Look at the output of `litespec model check`.

### Declare One Mark Style

**ID:** `litespec/core/model#declare-one-mark-style`

**Statement:** A project MUST write every mark in the one style that `marks` in `litespec.project.json` declares.

**Rationale:** Two styles in one project are two ways to do one job: a reader learns both, a review checks both, and a search for a record finds half its code. The style is a project choice made once, so every tool reads it from the project file and refuses the other style. A code fix converts a mark to the declared style.

**Example:** The Claimwick product declares `"marks": "xml"`, so its submit-expense handler carries a documentation tag naming `use-case/spending.submit-expense`, and an attribute naming the same use case is reported.

**Checked by:** `litespec model check` reports `mark-style-mixed` (tier S) for a mark written in the style the project did not declare.

### Follow Effective Status in Code Findings

**ID:** `litespec/core/model#follow-effective-status-in-code-findings`

**Statement:** An S finding about a record MUST follow the record effective status: block when it is approved or reviewed, report without blocking when it is draft, and stay silent when it is retired.

**Rationale:** A draft record states work in progress, and code written ahead of it is the normal order of a slice: failing the build on it trains people to suppress findings, while hiding it loses the signal. Reporting without blocking keeps the finding visible until the record is reviewed, and then it blocks. A retired record states the past, so code still marked with it is reported through the mark, not through the record. A finding about no record, such as an unmarked type or a mark naming no record, follows its tier.

**Example:** `use-case/spending.export-expenses` is a draft with no handler marked for it, so `use-case-unimplemented` prints with the draft marker and the check exits zero; once the use case is reviewed, the same finding blocks.

**Checked by:** `litespec model check` prints `draft` beside a non-blocking S finding whose subject is a draft, and exits zero when only such findings remain.

### Hold Proof Findings While Tests Are Frozen

**ID:** `litespec/core/model#hold-proof-findings-while-tests-are-frozen`

**Statement:** When `check.phase` is `frozen`, `proves-unconfirmed`, `use-case-without-scenario`, and every mark finding on a test MUST report without blocking.

**Rationale:** Before a first release a project can hold its tests still while the code moves faster than tests can follow. Naming that phase in the project file lets every tool respect it the same way, instead of each agent remembering a note: proof findings stay visible, and the build does not fail on tests nobody may change yet. Setting the phase back to `working` makes them block again.

**Example:** With `"check": { "phase": "frozen" }`, `proves-unconfirmed` and `use-case-without-scenario` print with the frozen marker and the check exits zero.

**Checked by:** `litespec model check` prints `frozen` beside `proves-unconfirmed`, `use-case-without-scenario`, and every mark finding on a test while `check.phase` is `frozen`, and none of them blocks; `dotnet build` reports `LS4001` and every LS5xxx rule on a test as a warning in the same phase.

### Name the Lowest Record Each Test Proves

**ID:** `litespec/core/model#name-the-lowest-record-each-test-proves`

**Statement:** Every test MUST name in a `proves` mark the lowest record it verifies, from the kinds the marks table on this page lists for a test.

**Rationale:** A test outside the record tree is either proof of something the specification forgot to state or a test that verifies nothing anyone promised. Naming the lowest record it verifies places each test: a scenario or rule for behavior, a quality requirement for how well the product behaves, a technical decision for a technical choice, an external system or integration event for an agreement with the outside, and a provision for a structural rule the code must keep. A test that verifies a choice no record states shows a missing record, which is written at the level where the choice lives, as safety-critical standards resolve derived requirements; a test that verifies nothing a record should state is deleted. Proof stays layered: only scenario and rule proofs verify a use case, and a passing test of a requirement, decision, external system or provision marks that record tested, so "the adapter is right" never reads as "the promise is kept".

**Example:** In Claimwick, the submit-expense acceptance test proves `scenario/submit-expense.submitting-with-a-receipt-succeeds`; a load test of month-end submissions proves `requirement/spending.month-end-submissions`; a round-trip test of the expense document proves `decision/0002-store-expenses-as-documents`; and the layering test that Domain references no persistence package proves `litespec/backend/architecture#point-dependencies-inward`.

**Checked by:** `litespec model check` reports `mark-missing` and `mark-unresolved` (tier S) for a test that names no record or a record of a kind a test does not take.

### Implement Each Mapped Behavior Record Behind a Mark

**ID:** `litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark`

**Statement:** Every aggregate, reaction, job, and external system, and every use case reached through a `browser` or `api` entry point, MUST be named by a mark on the code element the marks table gives it.

**Rationale:** A mark points up, so a check reading marks alone finds code nobody recorded but misses a record nobody built. Checking each record that the marks table maps to code from the top closes the loop in the other direction: an aggregate with no root, a reaction with no event handler, a job with no host, an external system with no port, and a use case people reach with no endpoint. A use case with no screen calling it is reported as advice, because whether a browser reaches it depends on the deployment.

**Example:** `reaction/spending.send-approver-reminders-on-expense-submitted` is reviewed and no event handler carries its mark, so the check reports `reaction-unimplemented` and blocks.

**Checked by:** `litespec model check` reports `aggregate-unimplemented`, `reaction-unimplemented`, `job-unimplemented`, `external-system-unimplemented`, and `use-case-unexposed` (tier S) for a record no mark names, and `use-case-unreached` (tier A) for a browser use case no screen calls.

### Record a Project-Wide Lowering as a Deviation

**ID:** `litespec/core/model#record-a-project-wide-lowering-as-a-deviation`

**Statement:** A project MUST record a deviation for every analyzer rule it lowers or switches off in an `.editorconfig` that applies beyond one file.

**Rationale:** Severity follows record status and project phase on its own, so a project-wide lowering in `.editorconfig` is a departure from the standard rather than a setting. A deviation names the provision, the reason and the review date, so the lowering is reviewed and ends instead of staying silent.

**Example:** A project that lowers LS5001 to a suggestion in its root `.editorconfig` records a deviation citing `litespec/core/system#point-code-and-tests-up-to-records`.

**Reviewed for:** Which project-wide severity lowerings in `.editorconfig` lack a deviation record? Look at every `.editorconfig` in the repository beside the deviation records.

## Conventions

### Render Generated Regions from the Model

**ID:** `litespec/core/model#render-generated-regions-from-the-model`

**Default:** A generated region renders only facts derived from records.

**Replacement:** Code-computed facts live in the book and the model output.

**Rationale:** A generated region sits inside a record page, so everything in it reads with record authority. Facts computed from code, such as implemented and verified counts, describe the world rather than the promise, and rendering them beside records would lend them authority they do not hold. The book and the model output carry code-computed facts where readers meet them as computed.

**Reviewed for:** Which generated regions render code-computed facts? Look inside the generated fences beside the book and the model output.
