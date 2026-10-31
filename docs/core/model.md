# Project Model

## Intent

A LiteSpec project describes itself with records, written statements of fixed types joined by fixed links. This page states the four record layers, the record types with their identifiers and homes, the common fields, the links, the marks, the authority order, the authoring order, the statuses, the approval data, the risk tiers, the questions, the transcription mode, the model builder, the check levels, and the book and the diffs. People write and approve Intent, agents write Behavior from it, and the model builder computes how code and tests connect to the records. The [record language page](language.md) states the grammar of the names and values the records hold.

## Concepts

### The Four Record Layers

Each record layer answers one question, is written from the record layer above it, and owns its records. Nothing above Implementation is computed from code.

| Record layer | Question | Writes | Reviews | Records |
|:--|:--|:--|:--|:--|
| Intent | Why build it, and what do we promise? | agents draft, the decider decides | the decider and product people | product, goal, problem, feature, topic, requirement, constraint, decision, research, journey, actor, term (`layer: intent`) |
| Behavior | How does the system behave to keep the promises? | agents | developers; the security reviewer for access, outside calls and personal-data flows | module, aggregate, rule, event, integration-event, use-case, result, scenario, automation, notification, setting, role, permission, entry-point, external-system, runbook, term (`layer: behavior`) |
| Implementation | How is it built? | agents | level S checks; developers sample | code only |
| Proof | How do we know it holds? | the builder | level G and level S checks | tests, check results, computed statuses, the rendered book |

Governance is a group of records beside the four record layers, not a fifth layer: the `exception` record attaches to any record layer.

Three translation rules join the record layers. Specify moves Intent to Behavior: it adds states, invariants, refusals, consistency, data classes with their erasure positions, freshness, and access with tenant scope, and it leaves open any price, fee, rate, term, market, payer, retention period for personal data, observable freshness, or default or figure that differs by tenant, market, or date, each as a question the decider decides. Implement moves Behavior to Implementation: it adds the code shapes the stack pages fix, and adds no observable outcome no result declares, no edge no record declares, no write outside `changes`, and no access wider than the grant. Verify moves Implementation to Proof: one test proves each scenario through the public boundary, and a proof goes stale when its scenario hash or its test-body hash changes.

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
| `topic` | intent | Which requirements share one story? |
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
| `automation` | behavior | Which work runs without a request? |
| `notification` | behavior | Which person-facing messages go out? |
| `setting` | behavior | Which adjustable values hold, and who sets them? |
| `role` | behavior | Which permission sets exist? |
| `permission` | behavior | Which single actions are allowed? |
| `entry-point` | behavior | How do people reach the product? |
| `screen` | behavior | Which page does a person see through an entry point, and what does it offer? |
| `external-system` | behavior | Which outside system exchanges what? |
| `runbook` | behavior | How is it run or recovered? |
| `exception` | any | Which rule is set aside or which check stays red, and until when? |
| `term` | intent, behavior | Which word means what? |

The [intent page](intent.md) states the Intent records, the [behavior page](behavior.md) states the Behavior records, and the [access page](access.md) states the access records.

Each record page uses the page style the [Authoring Standard](authoring.md) names for its type. A setting-set page holds many settings, and the retention-schedule page holds the retention rows at `docs/behavior/retention-schedule.md`. The retention schedule is a page style, not a record type.

### The Docs Layout

A consumer keeps its documentation in one folder per record layer, so the folder a page sits in says who writes it, who reviews it, and what it may contain.

| Folder | Record layer | Holds |
|:---|:---|:---|
| `docs/intent/` | Intent | The brief, goals, problems, features, topics with their requirements by area, constraints, decisions, journeys, actors, research, and the product documents the business publishes, such as legal texts and the writing voice |
| `docs/behavior/` | Behavior | Modules with their aggregates, use cases, automations, notifications and settings; rule pages; access (roles, permissions, entry points); screens, one page per screen under `screens/`; external systems; integration events; the retention schedule; runbooks and operating procedures |
| `docs/implementation/` | Implementation | The technical decisions under `decisions/`, and guides for people working in the code: frontend architecture, the command reference, the published API guide, and setup guides. The guides describe code and hold no records |
| `docs/proof/` | Proof | Evidence the tools compute or a run leaves behind: generated registers, gate files, and operating logs. No person writes a fact here that a tool can compute |
| `docs/governance/` | Governance | Exceptions, which can attach to any record layer |

The glossary (`docs/glossary.json`) and the example cast stay at the top of `docs/`, because every record layer uses them.

### Pages and Entries

A page record is a Markdown file whose opening block names a record type. An entry record is an object in the `records` array of that block. Terms are the exception: each term is an entry in the glossary. Each type has one home. The area in `docs/intent/<area>/<topic>.md` is a folder only, never a record.

| Type | Stored as | Home |
|:---|:---|:---|
| `product` | page, exactly one | `docs/intent/brief.md` |
| `goal` | page | `docs/intent/goals/<name>.md` |
| `problem` | page | `docs/intent/problems/<name>.md` |
| `feature` | page | `docs/intent/features/<name>.md` |
| `constraint` | page | `docs/intent/constraints/<name>.md` |
| `research` | page | `docs/intent/research/<name>.md` |
| `decision` | page | `docs/intent/decisions/<number>-<name>.md`; a technical decision `docs/implementation/decisions/<number>-<name>.md` |
| `topic` | page | `docs/intent/<area>/<topic>.md` |
| `requirement` | entry | On its topic page |
| `journey` | page | `docs/intent/journeys/<name>.md` |
| `actor` | page | `docs/intent/actors/<name>.md` |
| `module` | page | `docs/behavior/modules/<module>/README.md` |
| `aggregate` | page | `docs/behavior/modules/<module>/<aggregate>/README.md` |
| `rule` | entry | On its aggregate page; a cross-aggregate rule on a rule page under `docs/behavior/rules/` |
| `use-case` | page | Beside its aggregate: `docs/behavior/modules/<module>/<aggregate>/<use-case>.md`; a query describing no one aggregate under `reads/` beside its module |
| `result` | entry | In its use case page |
| `scenario` | entry | In its use case page, or in its automation page |
| `event` | entry | On its aggregate page |
| `integration-event` | page | `docs/behavior/integration-events/<name>.md` |
| `automation` | page | `docs/behavior/modules/<module>/automations/<automation>.md` |
| `notification` | page | `docs/behavior/modules/<module>/notifications/<name>.md` |
| `setting` | entry | On a setting-set page: `docs/behavior/modules/<module>/settings.md`, or `docs/behavior/settings/<name>.md` for platform-wide settings |
| `role` | page | `docs/behavior/access/roles/<name>.md` |
| `permission` | entry | `docs/behavior/access/permissions.md` |
| `entry-point` | entry | `docs/behavior/access/entry-points.md` |
| `screen` | page | `docs/behavior/screens/<entry-point>.<name>.md` |
| `external-system` | page | `docs/behavior/external-systems/<name>.md` |
| `runbook` | page | `docs/behavior/runbooks/<name>.md` |
| `exception` | page | `docs/governance/exceptions/<name>.md` |
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
| `topic` | `topic/<name>` | `topic/approval` |
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
| `automation` | `automation/<module>.<name>` | `automation/spending.send-approver-reminder-on-expense-submitted` |
| `notification` | `notification/<module>.<name>` | `notification/spending.approval-reminder` |
| `setting` | `setting/<module>.<name>` | `setting/spending.max-open-expenses` |
| `actor` | `actor/<name>` | `actor/employee` |
| `role` | `role/<name>` | `role/approver` |
| `permission` | `permission/<name>` | `permission/approve-expense` |
| `entry-point` | `entry-point/<name>` | `entry-point/claimwick-app` |
| `screen` | `screen/<entry-point>.<name>` | `screen/claimwick-app.expense-list` |
| `external-system` | `external-system/<name>` | `external-system/payout-bank` |
| `runbook` | `runbook/<name>` | `runbook/clear-stuck-reimbursements` |
| `exception` | `exception/<name>` | `exception/payment-retries-later` |
| `term` | `term/<name>` | `term/expense` |

A screen's `<name>` follows the same one-to-six-word rule as every other name; its `<entry-point>` is the name of the entry point it belongs to. A rule home is an aggregate name or a module name. A use case name is unique across the project, so a result or a scenario cites only that name. An aggregate name is unique across the project, so a rule cites only that name. A requirement's topic segment names its topic, and the topic is its parent by identifier containment, as an aggregate holds its rules. A requirement whose topic segment differs from its page's name is an `identifier-invalid` finding. An automation, notification, or setting is filed with its module, and an integration event belongs to the published set. A rule name keeps the negation its statement makes. Every identifier citation resolves to a written record.

A journey step id is `journey/<name>#<step-id>`, with step ids `s1`, `s2`, and so on. A derived generic result is `result/<use-case>.forbidden` or `result/<use-case>.invalid-input`. Scenarios can point at derived results. Derived results are never written as records.

### Common Fields

Every written record carries the shared fields in this table. Entries inherit `owner` and `status` from their page unless they set their own. Field names follow `litespec/core/language#name-each-field-by-its-word-class`.

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
| `questions` | when open | Open questions, each with `question`, `decider`, `decideBy`, `ballot`, `recommendation`, and once answered `answer`, `answeredBy`, `answeredOn`; an open question blocks approval |
| `approvedBy` | on approved and reviewed | The approver: a person owner name |
| `approvedOn` | on approved and reviewed | The approval date in ISO form |
| `approvedHash` | on approved and reviewed | The builder hash over the semantic fields |
| `risk` | on requirements and Behavior records, when present | Any of `money`, `physical-access`, `personal-data`, `access` |
| `provenance` | during adoption, when transcribed | `transcribed` for a Behavior record written from code |

### Links

Each written link lives as a field on the record it starts from: a decision carries `decides`, a rule carries `fulfills`, and so on. The table names every written link field, the record that carries it, and what it points at. Each link states how it reads in both directions, and the book takes its connection labels from those two phrases.

| Field | Written on | Points at | Reads as | Read back as |
|:---|:---|:---|:---|:---|
| `decides` | decision | Requirements, features, rules, constraints, and the permissions, roles, actors, exceptions, entry points, settings, or decisions an engineering choice settles | decides | is decided by |
| `basedOn` | decision, rule | Constraints, research, and on a technical decision the requirements it serves | is based on | is a basis for |
| `replaces` | decision | The replaced decision | replaces | is replaced by |
| `addresses` | product | Actors | addresses | is addressed by |
| `affects` | problem | Actors | affects | is affected by |
| `solves` | feature | Problems; a refused feature names the problem it declines, read as declines and is declined by | solves | is solved by |
| `advances` | feature | Goals | advances | is advanced by |
| `serves` | use case, automation | Journey steps | serves | is served by |
| `serves` | journey | The goal it walks toward | serves | is served by |
| `cites` | requirement | Goals, problems, features, constraints, research, and decisions | cites | is cited by |
| `cites` | journey | Requirements it shows | cites | is cited by |
| `fulfills` | rule, use case, automation, notification, setting | Requirements | fulfills | is fulfilled by |
| `governs` | rule | Aggregates, use cases | governs | is governed by |
| `transitions.by` | aggregate | The use case that moves the transition | is moved by | moves |
| `refusal` | rule | The refusal result carrying its code | is refused as | is the refusal for |
| `result` | scenario | The written or derived result it ends in | ends in | is shown by |
| `raises` | use case | Events | raises | is raised by |
| `access.grant` | use case | A permission, a self scope, anonymous, or system | is granted through | grants |
| `reachedThrough` | use case | Entry points | is reached through | reaches |
| `entryPoint` | screen | The one browser or device entry point it belongs to | belongs to | is entered through |
| `address` | screen | The route template it serves; a screen carries `address` or `host`, never both | is addressed at | is the address of |
| `host` | screen | The screen an overlay with no address of its own opens on; a screen carries `address` or `host`, never both | opens on | hosts |
| `offers` | screen | The use cases a person can start from it | offers | is offered by |
| `serves` | screen | The journey steps it carries a person through | serves | is served by |
| `floorplan` | screen | The floorplan it composes | is composed from | is composed into |
| `changes` | use case, automation | Aggregates | changes | is changed by |
| `reads` | use case, automation | Aggregates, and on a use case the settings it reads | reads | is read by |
| `effects` | use case, automation | Use cases, notifications, integration events, external system operations | effects | is effected by |
| `triggeredBy` | automation | Domain events or integration events | answers | is answered by |
| `binds` | rule | Aggregates | binds | is bound by |
| `exercises` | scenario | Rules | exercises | is exercised by |
| `drivenBy` | aggregate | Automations, actors, or roles | is driven by | drives |
| `compensation.useCase` | aggregate | The use case reversing the saga step | is compensated by | compensates |
| `freshness.authority` | use case | The record holding the authoritative value | is advised by | advises |
| `erasure.operation` | aggregate | The external system operation erasing the data class | is erased through | erases |
| `erasure.event` | aggregate | The integration event carrying the erasure | is erased through | carries the erasure of |
| `holds` | actor | Roles at a scope | holds | is held by |
| `allows` | role | Permissions | allows | is allowed by |
| `actor` | journey, external system | The actor walking the journey; the actor the system acts as | is walked by / acts as | walks / is acted as by |
| `translates` | integration event | Domain events | translates | is translated by |
| `dependsOn` | module | Modules | depends on | is depended on by |
| `runbook` | automation | The runbook answering the automation | is answered by | answers |
| `covers` | exception | The provision a departure sets aside, or the finding codes a gap holds | covers | is covered by |
| `subject` | exception | The records the exception covers; a gap may also name journey steps (`journey/<name>#s<n>`) for the step findings | applies to | is applied to |
| `terms` | any record | Terms | uses | is used by |

Behavior records point up: `serves` on a use case, automation, or screen points to journey steps, `serves` on a journey points to its goal, `advances` on a feature points to goals, and `fulfills` points to a requirement. Records never point down: a journey names no use case, and a requirement names no rule.

Containment needs no link field: it follows the identifier. An aggregate belongs to the module its identifier names, a requirement belongs to the topic its identifier names, an automation, notification, or setting belongs to the module its identifier names, and a result or a scenario belongs to the use case its identifier names. An integration event belongs to the published set. The builder computes the remaining links from code, routes, and tests: `implements` from a code mark to the record it names, `follows` from a code mark to the provision, decision, or exception that shapes the code, `calls` from a screen to a use case, and `proves` from a test mark to a scenario or rule. Code and tests point up to the record they serve through their marks, with each handler implementing exactly one record. The Parents table below names each record parent.

### Parents

Every record except the product has exactly one parent. The book places each record once, under its parent. Every other link on the record is a connection: it renders with the reads-as phrase the Links table states, never as a second placement. Every inverse link is computed, never written.

Where the identifier home segment, or the page home for a page or entry record, names one record, that record is the parent and no parent field is written. A use case page beside its aggregate is page containment. Where no containment names a parent, the child carries one required up-link naming it, except the sixteen root types below, whose parent is the product with no field written: a project has exactly one product, so the field would repeat one value on every record.

One parent needs a stated precedence. A feature names its parent in `solves`, and the parent is its first entry; a refused feature names the problem it declines in the same field. Additional entries are connections. List order decides placement, so reordering a list moves the record.

The product has no record parent; its parent is the project that declares it. Intent, Behavior, Implementation, Proof, and Governance are transparent group nodes on the path from product to record: a group node is never a parent and never satisfies the parent rule. A draft parent is a present parent, so an author may write top down or bottom up. A retired record is exempt and stands beside its replacement under the same parent, and a retired parent still counts as present for a live child.

| Record type | Parent | Found through |
|:---|:---|:---|
| `product` | none | the project that declares it |
| `goal` | the product | the product |
| `problem` | the product | the product |
| `feature` | `problem` | `solves`, first entry |
| `constraint` | the product | the product |
| `research` | the product | the product |
| `decision` | the product | the product |
| `topic` | the product | the product |
| `requirement` | `topic` | identifier home |
| `journey` | `goal` | `serves` |
| `actor` | the product | the product |
| `module` | the product | the product |
| `aggregate` | `module` | identifier home |
| `rule` | `aggregate` or `module` | identifier home, an aggregate before a module |
| `use-case` | `aggregate` or `module` | the aggregate holding its page; else identifier home |
| `result` | `use-case` | identifier home |
| `scenario` | `result` | `result` |
| `event` | `aggregate` | identifier home |
| `integration-event` | the product | the product |
| `automation` | `module` | identifier home |
| `notification` | `module` | identifier home |
| `setting` | `module` | identifier home |
| `role` | the product | the product |
| `permission` | the product | the product |
| `entry-point` | the product | the product |
| `screen` | `entry-point` | `entryPoint`, agreeing with the identifier home |
| `external-system` | the product | the product |
| `runbook` | the product | the product |
| `exception` | the product | the product |
| `term` | the product | the product |

### Marks

A mark is the one written link from a code element to the record it serves. It holds the record identifier, and every tool reads it the same way: the model builder and the analyzers. A folder path stays the map a reader follows through the code, and it is never the link, except for a Domain constituent type, which takes no mark and belongs to the aggregate whose folder holds it (`litespec/backend/domain#place-every-constituent-type-in-its-aggregates-folder`).

| Record layer the code serves | Code element | Mark names |
|:--|:--|:--|
| Domain | Aggregate root | `aggregate/<module>.<name>` |
| Domain | Aggregate root method that changes state | `use-case/<module>.<name>`, exactly one: the use case whose act it performs |
| Domain | Domain event | `event/<aggregate>.<fact>` |
| Domain | Refusal type for one rejected rule | `rule/<home>.<name>` |
| Domain | Domain service holding one rule calculation | `rule/<home>.<name>` |
| Domain | Constituent type: identifier, state type, closed set, child entity, value object, repository interface | No mark: the builder derives its aggregate from its folder |
| Application | Command or query handler | `use-case/<module>.<name>`, exactly one |
| Application | Event handler | `automation/<module>.<name>` |
| Application or Infrastructure | Job host | `automation/<module>.<name>` |
| Application | Port for an external system | `external-system/<name>` |
| Infrastructure | Adapter implementing that port | `external-system/<name>` |
| API | Endpoint | `use-case/<module>.<name>` |
| Frontend | Use-case module | `use-case/<module>.<name>` |
| Frontend | Route entry: a web route file, a Blazor `@page` component, or an Android destination activity or fragment class | `screen/<entry-point>.<name>`, exactly one |
| Operations | Deployment, proxy, host, or CI workflow file listed in `paths.operations` | One or more `follows` marks in the file's `#` comments, such as `# <follows>decision/<number>-<name></follows>` |
| Operations | Alert rule in a file listed in `paths.alerts` | A `requirement` label naming a quality requirement and a `runbook` label naming a runbook |
| Tests | Test | The lowest record it verifies: `scenario/<use-case>.<name>` or `rule/<home>.<name>` for behavior; `requirement/<topic>.<name>` of kind quality; `decision/<number>-<name>` for a technical choice; `external-system/<name>` or `integration-event/<name>` for an outside agreement; `litespec/<page>#<slug>` for a provision the code must keep |
| Application, Infrastructure or API | Technical class: a class that declares a method, or an options class that binds a setting, and takes no other mark | One or more: a Behavior record it serves in an `implements` mark, or `litespec/<page>#<slug>`, `decision/<number>-<name>` or `exception/<name>` in a `follows` mark |

A code element serving a record names it in an `implements` mark. A technical class names the record it serves in an `implements` mark, as an input validator names its use case and a repository its aggregate. Where it serves no record, it names the reason it is built the way it is in a `follows` mark: the LiteSpec provision that requires its shape, the technical decision that chose it, or the exception that allows it. Any other code element may carry a `follows` mark as well.

The tools recognize most code elements from the code itself and then expect their mark. A port and a domain service are recognized by their mark, a job host by its hosted-service shape or by its mark, and an aggregate root method by the event it raises or by its mark, so an unmarked port is found only from the top, through its external system, and an unmarked method that changes state without raising an event is not recognized at all.

Six provisions divide the marks between them: `litespec/core/system#point-code-and-tests-up-to-records` requires the mark, `litespec/core/system#point-every-technical-class-up-to-its-record-or-choice` covers technical classes, `litespec/core/model#declare-one-mark-style` fixes the style, `litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark` checks each mapped record from the top, `litespec/core/model#name-the-lowest-record-each-test-proves` sets what a test names, and `litespec/backend/testing#mark-the-test-that-proves-each-scenario` places the test mark.

A project writes its marks in one mark style, declared as `marks` in `litespec.project.json`. Each stack page states the forms the style takes: the .NET stack writes an `xml` mark as a documentation tag and an `attribute` mark as a generated attribute, and a frontend writes a typed constant in either style. A browser test carries its `proves` mark as a test annotation built by the `proves` helper the generated records file exports. A runtime value a record already states, such as an event classification or a refusal code, is generated from the mark and the record where the stack generates code, and checked against the record where a person writes it.

### Authority

Intent outranks Behavior. When two records disagree, the higher one wins: constraint over product, product over requirement, requirement over rule, rule over use case, use case over result, and result over scenario. Inside one record, a must beats a should. Between peers, the later approved decision wins, and its `replaces` link shows what it overturned. Every other link is trace: it explains why a record exists and never decides a conflict.

A decision explains a choice and is replaced, never edited. Goals, problems, features, journeys, actors, research, and prose carry no authority, so they never override an approved record. An exception of kind `departure` outranks the LiteSpec provision its `covers` names for the project that records it, until its `expiresOn` date.

When code and the specification disagree, the specification wins where Intent has spoken. Where Intent is silent, working code is evidence for a question, never a defect to delete and never a source to transcribe silently.

A record stays unapproved while a decision behind it stays draft. An accepted gap becomes an exception of kind `gap`, and the findings its `covers` names report without failing until the `expiresOn` date. Module order states dependencies and grants no authority.

### Authoring Order

Records are authored top-down, one slice at a time: one feature from problem to scenario, then the next. Actors, roles, permissions, and the glossary grow beside Intent and Behavior. A question found while modeling goes up as an open question with a decider and a date; the Behavior record stays draft until it is answered. Adding new intent to a running project follows the one sequence under New Intent in a Running Project on the [agent page](agent.md); this page states no second order.

### Statuses

Intent and governance records use draft, approved, and retired. Behavior records use draft, reviewed, and retired. Draft means the record awaits its decider, approved or reviewed means it holds, and retired means a replacement holds. A draft names its `decider` and its `decideBy` date. A decision is replaced, never edited: the retired decision stands beside its replacement so the reasoning stays readable.

Implemented, verified, met, tested, exercised, stale, flaky, and reachable are computed, never handwritten. The builder derives them from code, screens, tests, and test runs on every run. The four proof words are verified, met, tested, and exercised, each with the one meaning the table states:

| State | Of | When |
|:--|:--|:--|
| implemented | use case, rule, event, aggregate state | the stack builder finds code for it |
| verified | use case | every scenario has a passing proof through the public boundary that is not stale |
| tested | quality requirement, technical decision, external system, integration event | a passing test that asserts something links to it; tested never makes a use case verified |
| met | requirement | everything fulfilling it is verified, and its measure's check passes |
| stale | scenario proof | the scenario hash or the test-body hash differs from the hashes `docs/proof/proofs.json` recorded at the last passing run |
| flaky | scenario proof | the test both passed and failed within the runs read |
| reachable | aggregate state | a transition leads into it |
| exercised | journey step | a passing proof of a use case serving the step |

The builder also writes each record effective status: draft when the record, a decision deciding it, or a requirement it fulfills is a draft. A result or a scenario takes its use case effective status. A screen is draft when its entry point, a use case it offers, or a journey it serves is draft. A finding about a record follows the record's own status; an inherited draft shows as a label beside the finding.

### Approval as Data

An approved or reviewed record carries its approval as data: `approvedBy` names a person owner, `approvedOn` names the date, and `approvedHash` is the builder hash over the semantic fields, written by `litespec model approve`. The semantic fields are the statements, results, output names, rules, and steps. Prose bodies and a term's translation labels sit outside the hash, so rewording prose or adding a language reopens no approval. `approval-reopened` fires when the semantic hash differs from `approvedHash`. The per-type table in [the approval hash guide](../guide/approval-hash.md) is the one definition of which fields feed the hash.

### Risk Tiers

`risk` is optional on requirements and Behavior records, and holds any of `money`, `physical-access`, `personal-data`, and `access`. Any value makes the record tier 1. A record with no risk is tier 3. Tier 1 needs explicit approval or review by a named person. A question takes the tier of the record that carries it. Approval stays loose: an agent reading counts as the tier 1 second reading, a project whose owners list holds one name needs no second reviewer, and `review.securityReviewer` is optional with the security queue a list to read.

### Questions and Ballots

`questions` carries `question`, `decider`, `decideBy`, `ballot`, and `recommendation`, and once answered `answer`, `answeredBy`, and `answeredOn`. `ballot` is required and groups questions in the inbox, by a decision id or a short topic slug. `recommendation` is required when the record carries no `risk`. A question carries no `tier` and no `risk`; its tier is the tier of the record that carries it. An unanswered question is open, and an open question blocks approval. `decider-missing` checks question entries as well as drafts. Only a question auto-approves, after its `decideBy` plus the project veto window, recorded as `answeredBy: auto (veto window)` on the question; no record carries `approvedBy: auto (veto window)`. `review.vetoWindowDays` in `litespec.project.json` is optional and defaults to 7. `question-expired` reports every unanswered question past its `decideBy` that cannot auto-approve, tier 1 included. Each ballot carries a consequences summary rendered from Behavior, marked as context and outside the approval hash, so Intent stays enough to approve. `litespec book` shows the Ballots view, grouping questions by `ballot`, ordered tier 1 first, with each question recommendation and auto-approval date. The [release page](release.md) states the review flow.

### Transcription Mode

During adoption, a Behavior record written from code carries `provenance: transcribed`. Such a record can stand as draft, but never as reviewed, until its `fulfills` or `serves` parent is approved. `litespec.project.json` `adoption.transcriptionEndsWith` names the decision that ends the mode. After that decision is approved, `transcription-expired` fails on any remaining transcribed record.

### Size and Stacks

Each project declares its size, its stacks, and its optional types in `litespec.project.json`. Each frontend declares one stack, and the backend declares its own. Every project answers whether constraints apply, possibly with none. Owners form a closed list, and extensions name their replacements and switched-on types. The [scope page](scope.md) states how a project selects its size, shape, and stacks.

### The Model Builder

The model builder reads records, code, routes, tests, and test runs into one model. It writes the model file, runs the model checks, and renders generated regions. The [model command page](../tools/model.md) lists the subcommands and finding codes.

Generated regions sit inside fences that only the builder writes and prose checks skip. Each project runs the builder in its own gate, so handwritten prose and computed views stay in one page without mixing.

A finding that an approved exception covers is reported as excepted and exits zero. The exception covers the finding when its `covers` names the finding code, or the provision governing it, and its `subject` lists the finding identifier or its `paths` match the finding path. An exception past its `expiresOn` date is reported as `exception-expired`. Proof state lives in `docs/proof/proofs.json`, committed, holding for each scenario the hash of the scenario and of its test body at the last passing run; a proof is stale when either hash changes.

`litespec model diff <base>` prints the records a change adds, changes and removes, in the book words, for review.

### Check Levels

Every check has a level.

| Level | Meaning |
|:--|:--|
| G | Reads records only. Always blocks. |
| S | Reads code through a stack reader. Blocks where the reader that produced the finding is qualified, and reports otherwise. |
| A | Advisory. Reports and never blocks. |

A structural parent is any link or containment the Parents table and the Identifiers table name as a record parent: topic containment for a requirement, feature `solves`, journey `serves`, scenario `result`, screen `entryPoint`, and identifier containment. A missing structural parent is level G for every record, drafts included. Every other required link is coverage: level A for a tier 3 record and level G for a tier 1 record.

Qualification is per reader, never global: a finding takes the qualification of the reader that produced its facts. A reader qualifies when its handshake matches; the golden corpus is a test contract for the reader bundle and does not gate a consumer run. The .NET (Roslyn) reader, including the Blazor and Android screens it reads, stays qualified. A finding no reader produced, a record finding, is qualified. A level S finding from an unqualified reader reports without blocking even when another reader in the same project is qualified. A missing or failing reader produces `reader-missing` (level S) for that frontend, and that frontend's level S findings report instead of blocking.

`litespec model check` prints the level beside each finding.

A level S finding about a record follows the record's own status: it blocks when the record itself is approved or reviewed, and reports without blocking while the record itself is a draft, so code written ahead of a draft never fails the build and never goes unreported. An inherited draft shows as a label beside the finding. An analyzer reports a blocking finding as an error and a reported one as a warning, and a finding that an approved exception covers stays silent.

Two flags tune blocking without changing the levels. `litespec model check --release` makes the release-readiness findings block: `use-case-unverified` and unanswered questions on tier 1 records. `litespec model check --since <ref>` blocks only on findings about records and code changed since `<ref>` and reports the rest, so an adopting project clears new findings first.

The [model command page](../tools/model.md) lists every finding code with its level and meaning.

### The Book and the Diffs

The book and the diffs live in the Proof record layer. The book shows each journey as record detail: its `finishedWhen`, the steps with their `statement` and `failure`; the requirements it cites, with their statements and their `except` and fallback inline; for each step, the serving use case `goal` in one line; the Then clauses of its scenarios, as context; a consequences box, as context, naming new refusals in product words, data kept and for how long, and money effects; and the open questions.

Field tables render as tables. Scenarios group under their result. Field names never render literally in journey detail.

`litespec model diff <base> --intent` shows Intent changes in product words. `--behavior` shows Behavior changes and the security queue. Without a flag, it shows both. The security queue lists tenant scope `cross` access, new `calls` effects, and new personal or sensitive data classes for the security reviewer where the project names one in `review.securityReviewer`. The book renders `model.json` only and judges nothing itself.

## Standards

### Write Every Record in Its Home

**ID:** `litespec/core/model#write-every-record-in-its-home`

**Statement:** A project MUST keep every record in the home its type names.

**Rationale:** One home per type tells a reader where to open a record and tells the builder where to read it. A record kept anywhere else is invisible to the checks that scan its home, so it looks sound while nothing validates it.

**Example:** A decision lives at `docs/intent/decisions/0001-receipt-threshold.md`.

**Checked by:** `litespec model check` reports `record-invalid` for a record outside its home.

### State the Common Fields on Every Record

**ID:** `litespec/core/model#state-the-common-fields-on-every-record`

**Statement:** Every written record MUST carry its type, identifier, title, owner, and status.

**Rationale:** Type, identifier, title, owner, and status are the fields every join needs. Without them on every record, the builder cannot connect decisions to requirements, scenarios to results, or records to owners, and the model falls apart into loose pages.

**Checked by:** `litespec model check` reports `record-invalid` for a record missing a common field.

### Give Every Record One Parent

**ID:** `litespec/core/model#give-every-record-one-parent`

**Statement:** Every record except the product MUST have exactly one parent from the Parents table on this page.

**Rationale:** A record with no parent has no place in the book, and a record with two parents appears twice. One parent per record puts every record exactly once: by identifier or page containment where one names it, by one required up-link otherwise, and under the product for the sixteen root types with no record above them. A requirement sits under the topic its identifier names, and a feature takes the first entry of `solves`. A draft parent is a present parent, so an author may write top down or bottom up. Settled by [decision/0002-parent-for-every-record](../intent/decisions/0002-parent-for-every-record.md) on the evidence in [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence.md), [research/intent-hierarchy-evidence](../intent/research/intent-hierarchy-evidence.md), [research/book-outline-evidence](../intent/research/book-outline-evidence.md), and [research/established-traceability-practice](../intent/research/established-traceability-practice.md).

**Example:** In the Claimwick product, `feature/approval-reminders` names `problem/expenses-wait-weeks` in `solves` as its parent; `requirement/approval.receipt-over-threshold` sits under `topic/approval` through its identifier home; `journey/submit-and-reimburse` names `goal/faster-reimbursement` in `serves`; and `result/submit-expense.submitted` sits under `use-case/spending.submit-expense` through its identifier home.

**Checked by:** `litespec model check` reports `record-parent-missing` (level G) for a record whose required parent is missing.

### Flag Every Record Whose Parent Changed

**ID:** `litespec/core/model#flag-every-record-whose-parent-changed`

**Statement:** Every record that cites, fulfills, or serves a record whose semantic hash changed MUST be flagged until it is updated.

**Rationale:** A promise that moves without marking its readers leaves Behavior resting on Intent nobody holds anymore. The flag follows the trace: when a record semantic hash changes, every record that cites, fulfills, or serves it is flagged until it is updated. The top-down story depends on the flag, so demoting the rule and recording a gap instead would hide the change the next author must make.

**Example:** The decider raises the receipt threshold from EUR 25 to EUR 50, so the requirement statement changes and the builder flags the policy setting and the receipt rule that cite it until an agent updates them.

**Checked by:** `litespec model check` reports `parent-reopened` (level A while draft, level G once approved) for a record that cites, fulfills, or serves a changed record and is not yet updated.

### Use the Layer Status Vocabulary

**ID:** `litespec/core/model#use-the-layer-status-vocabulary`

**Statement:** Every written record MUST carry the status vocabulary of its record layer.

**Rationale:** Intent and governance records use draft, approved, and retired, while Behavior records use draft, reviewed, and retired. One vocabulary per record layer tells a reader whether a person approved the promise or reviewed the behavior. A Behavior record marked approved claims a decider approval nobody gave.

**Checked by:** `litespec model check` reports `field-invalid` for a status outside the record layer vocabulary.

### Hold Approval Behind Open Questions

**ID:** `litespec/core/model#hold-approval-behind-open-questions`

**Statement:** A record MUST NOT stand approved or reviewed while it carries an open question.

**Rationale:** An open question names a fact the record still needs, with the decider and the date that will settle it. Approving the record first would claim settled intent while the missing fact can still move it. Answering the question or retiring the record clears the gate. A question that auto-approves after the veto window is answered, not open, so the gate still holds.

**Checked by:** `litespec model check` reports `record-invalid` for an approved or reviewed record carrying an open question.

### Answer Each Question on Its Record

**ID:** `litespec/core/model#answer-each-question-on-its-record`

**Statement:** Each question MUST record its answer on the record that carries it.

**Rationale:** A question answered anywhere but on its record leaves the record open while the answer sits elsewhere. The question carries `question`, `decider`, `decideBy`, `ballot`, and `recommendation`, and once answered `answer`, `answeredBy`, and `answeredOn`. `ballot` is required, and `recommendation` is required when the record carries no `risk`. Only a question auto-approves, after its `decideBy` plus the veto window, recorded as `answeredBy: auto (veto window)`; no record carries `approvedBy: auto (veto window)`.

**Example:** Claimwick asks which receipt threshold holds, with the decider, the date, the ballot, and a recommendation of EUR 50; the decider answers EUR 50 on the same record with the answerer and the date.

**Checked by:** `litespec model check` reports `decider-missing` for a question missing its decider or date and `question-expired` for every unanswered question past its `decideBy` that cannot auto-approve.

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

**Rationale:** A record carrying money, physical-access, personal-data, or access risk decides what money moves, who passes, whose data is kept, or who may act. Only a named person weighs those consequences; the veto window auto-approval never decides them. Tier 3 records carry no risk, so their questions can auto-approve after the window.

**Reviewed for:** Which tier 1 records lack a named personal approver? Look at `approvedBy` on records carrying risk.

### Write Links on the Record That Points

**ID:** `litespec/core/model#write-links-on-the-record-that-points`

**Statement:** A link MUST live as a field on the record it starts from, with the Links table stating how it reads in both directions.

**Rationale:** One writing site keeps a link and its source in one edit: changing the source updates the link in the same change. Links written on the target instead would scatter each relationship across every page it touches. Settled by [decision/0002-parent-for-every-record](../intent/decisions/0002-parent-for-every-record.md) on the evidence in [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence.md), [research/intent-hierarchy-evidence](../intent/research/intent-hierarchy-evidence.md), [research/book-outline-evidence](../intent/research/book-outline-evidence.md), and [research/established-traceability-practice](../intent/research/established-traceability-practice.md).

**Checked by:** `litespec model check` reports `reference-unknown` for a link whose target no record carries.

### Connect Every Use Case to Intent

**ID:** `litespec/core/model#connect-every-use-case-to-intent`

**Statement:** A use case MUST carry serves, fulfills, or both.

**Rationale:** A use case with no served journey step and no fulfilled requirement answers to nothing above it: no promise explains why it exists. Pointing up with serves or fulfills ties every behavior to the Intent that asks for it. `record-link-missing` fails a use case carrying neither.

**Checked by:** `litespec model check` reports `record-link-missing` for a use case with neither `serves` nor `fulfills`, and `use-case-journey-missing` (level G) at large size for a use case that is not background-only and serves no journey step.

### Serve Every Journey Step

**ID:** `litespec/core/model#serve-every-journey-step`

**Statement:** Every journey step MUST have a serving use case.

**Rationale:** A step no use case serves promises a moment the product never builds. Use cases point up with serves, so each step id collects the behaviors that carry the walker through it. `step-use-case-missing` fails a step with no serving use case. The pair sits side by side: `step-use-case-missing` names a step no use case serves, and `step-screen-missing` names a step a browser or device use case serves that no screen serves.

**Checked by:** `litespec model check` reports `step-use-case-missing` for a journey step with no serving use case, beside `step-screen-missing` for a step a browser or device use case serves that no screen serves.

### Follow the Authority Order

**ID:** `litespec/core/model#follow-the-authority-order`

**Statement:** A conflict MUST resolve toward the higher authority.

**Rationale:** Intent records outrank Behavior records: constraint over product, product over requirement, requirement over rule, rule over use case, use case over result, and result over scenario. The order settles a conflict without a meeting: a use case asking for behavior its rule forbids loses to the rule. Where Intent has spoken, the specification wins over code; where Intent is silent, working code is evidence for a question. Goals, problems, features, journeys, actors, research, and prose carry no authority, so none outranks the ladder.

**Reviewed for:** Conflict decisions under `docs/intent/decisions/` name the higher record that won, in the order this page states.

### Resolve Ties by the Later Decision

**ID:** `litespec/core/model#resolve-ties-by-the-later-decision`

**Statement:** A tie MUST resolve toward the later approved decision.

**Rationale:** Two records at one level can disagree with no higher record to settle them. The later approved decision wins because it decided with knowledge of the earlier one. When no later decision separates them, their shared owner decides, so every tie still has exactly one judge.

**Reviewed for:** Each tie-breaking decision carries a later approval date than the peer records it settles.

### Hold Approval Behind Draft Decisions

**ID:** `litespec/core/model#hold-approval-behind-draft-decisions`

**Statement:** A record MUST NOT stand approved or reviewed behind a draft decision.

**Rationale:** Approval means the reasoning behind the record already holds. A record approved behind a draft decision claims settled intent while its foundation can still move. The builder also writes each record effective status: draft when the record, a decision deciding it, or a requirement it fulfills is a draft. A result or a scenario takes its use case effective status. A screen is draft when its entry point, a use case it offers, or a journey it serves is draft.

**Checked by:** `litespec model check` reports `approval-invalid` for an approved or reviewed record behind a draft decision.

### Record Accepted Gaps as Exceptions

**ID:** `litespec/core/model#record-accepted-gaps-as-exceptions`

**Statement:** An accepted gap MUST live as an exception of kind `gap` with owner and expiry date.

**Rationale:** Missing work with no owner and no date is invisible: nobody answers for it and nothing reports it. An exception of kind `gap` with an owner and an `expiresOn` date turns the same missing work into a visible promise: the findings its `covers` names report until the date, and `exception-expired` reports the exception after it. One person may approve it.

**Example:** The running Claimwick example accepts that exporting expenses has no handler yet, so the gap lives as an exception with an owner and an expiry date in `docs/governance/exceptions/export-expenses-unimplemented.md`.

```json
{
  "type": "exception",
  "id": "exception/export-expenses-unimplemented",
  "title": "Export expenses unimplemented",
  "owner": "claimwick-team",
  "status": "approved",
  "approvedBy": "ava-reyes",
  "approvedOn": "2026-10-01",
  "approvedHash": "9f2c41ab",
  "kind": "gap",
  "covers": ["use-case-unimplemented"],
  "subject": ["use-case/spending.export-expenses"],
  "reason": "Export waits for the finance file format.",
  "expiresOn": "2027-06-01"
}
```

The `use-case-unimplemented` finding reports without failing until 2027-06-01, and the expenses team answers for closing the gap, which satisfies this provision.

**Checked by:** `litespec model check` reports `record-invalid` for an exception missing its owner, date, or renewal, `exception-expired` (level G) past its `expiresOn` date, and `exception-finding-unknown` (level G) for an exception naming a finding code no check uses.

### Apply Guidance Precedence

**ID:** `litespec/core/model#apply-guidance-precedence`

**Statement:** A conflict MUST resolve toward the project exception, the extension, the deeper page, or the shallower page, in that order.

**Rationale:** A project requirement can never quietly override a shared standard; without this order it would, one page at a time. The conflict becomes an exception record instead: dated, reasoned, and reviewable. The order puts the project exception first, then the extension, then the deeper page, then the shallower page, so the most local approved decision wins.

**Example:** The running Claimwick example runs its weekend export with no reviewer on shift, so it records a project exception that outranks the shared standard in `docs/governance/exceptions/export-review-bypass.md`.

```json
{
  "type": "exception",
  "id": "exception/export-review-bypass",
  "title": "Export Review Bypass",
  "owner": "claimwick-team",
  "status": "approved",
  "approvedBy": "ava-reyes",
  "approvedOn": "2026-10-01",
  "approvedHash": "9f2c41ab",
  "kind": "departure",
  "covers": "litespec/backend/application#enforce-target-authorization-in-the-pipeline",
  "subject": ["use-case/spending.export-expenses"],
  "reason": "The weekend export runs with no reviewer on shift.",
  "expiresOn": "2027-01-01"
}
```

The record names the provision it sets aside, the reason, and the expiry date, so the departure stays visible and dated, which satisfies this provision.

An exception that covers code no record describes names those files in `paths`, as globs from the repository root. An exception that covers records names them in `subject`. One of the two is required. While the exception is approved and its `expiresOn` date has not passed, `model check` reports the findings its `covers` names on those files and records, and on the results and scenarios a named use case holds, as excepted instead of failing on them; after the expiry date they fail again. A departure covers the finding codes its provision's Checked by line names.

**Reviewed for:** Every departure from a shared provision resolves to an exception record under `docs/governance/exceptions/` with reason and expiry date.

### Keep One Owner and Status on Every Record

**ID:** `litespec/core/model#keep-one-owner-and-status-on-every-record`

**Statement:** Every page MUST name one owner and one status.

**Rationale:** One owner means one person answers for the page. One status means the page either awaits decision, holds, or stands replaced. Entries inherit both from their page, so a page of entries stays consistent without repeating the fields on every object. The owner is the readable name the project owners list holds, and the project file maps that name to its CODEOWNERS handles.

**Example:** A draft names its `decider` and `decideBy` date. The three statuses and what each allows: a draft awaits its decider, approved or reviewed holds, and retired means a replacement holds.

**Checked by:** `litespec model check` reports `owner-unknown` and `decider-missing` for a page missing its owner or draft decider.

### Retire Records with Replaces

**ID:** `litespec/core/model#retire-records-with-replaces`

**Statement:** A replacing decision MUST name the decision it replaces, and no other record type carries `replaces`.

**Rationale:** Only decisions keep the past: the retired decision stands beside its replacement so the reasoning stays readable. Every other record changes in place, because retired copies of rules or use cases would fork the intent readers must follow. A rename rewrites every citation in the same change, so no dangling reference survives.

**Checked by:** `litespec model check` reports `field-invalid` for `replaces` outside a decision and `reference-unknown` for an unknown target.

### Name What Every Decision Decides

**ID:** `litespec/core/model#name-what-every-decision-decides`

**Statement:** Every product decision that is not retired MUST name at least one record it settles in `decides`.

**Rationale:** The draft gate and the computed effective status both follow `decides`. A decision that names nothing holds back nothing, so its approval state constrains no record and its draft gate checks nothing. Naming at least one settled record gives the decision something to govern. A technical decision governs the code that follows it instead.

**Checked by:** `litespec model check` reports `decision-decides-missing` for a product decision with an empty `decides` field.

### Record Each Technical Choice as a Decision

**ID:** `litespec/core/model#record-each-technical-choice-as-a-decision`

**Statement:** A technical choice that code follows and no LiteSpec provision makes MUST be a decision under `docs/implementation/decisions/` that at least one code element follows.

**Rationale:** Why a pattern, a library, or a configuration value was chosen is the first question a maintainer asks before changing it. Written in a comment, the answer drifts and no tool finds it. Written as a decision, it carries its options, what speaks for and against each, and what would reopen it, and every code element shaped by it names it in a `follows` mark. A choice a LiteSpec provision already makes needs no decision, because the provision is the record. Technical decisions sit under Implementation because they name technologies and code, which Intent never holds, and they share the numbered sequence of product decisions, so all decisions share one identifier space.

**Example:** Claimwick renders each receipt as a PDF file. `decision/0005-render-receipts-as-pdf` weighs a PDF library against an HTML-to-PDF browser, chooses the PDF library, and names what reopens the choice: receipts no longer needing a downloadable file. The receipt PDF renderer carries `/// <follows>decision/0005-render-receipts-as-pdf</follows>`.

**Checked by:** `litespec model check` reports `decision-unimplemented` (level S) for a technical decision that no code element follows. The check runs in the CLI only, with no analyzer rule: whether any code follows a decision is known only across every project, and one compilation sees one project.

### Keep Rules and Targets Out of Technical Decisions

**ID:** `litespec/core/model#keep-rules-and-targets-out-of-technical-decisions`

**Statement:** A technical decision MUST leave each rule the product obeys whatever its design to a constraint and each measurable target to a quality requirement, and name the ones it serves in `basedOn`.

**Rationale:** A technical decision chooses one way among alternatives, and a better alternative may replace it tomorrow. A rule such as keeping personal data in the European Union, or a target such as answering checkout within two seconds, does not change when the library does: overriding it needs a business, legal, or organizational change. Written inside a decision, the rule disappears the day the decision is replaced, and nobody approves it as the business promise it is. Kept in Intent, the rule and the target are approved by the person who owns them, the decision points up to them, and the chain runs unbroken from code through the decision to the constraint or requirement and the goal above it. A decision with no basis is advice, level A, while a product decision that decides nothing blocks at level G: a technical choice among equal options may serve no rule or target yet and still govern the code that follows it, but a product decision that settles no record governs nothing.

**Example:** Claimwick keeps each receipt for the tax file because `constraint/tax-receipt-retention` requires it, and answers submissions quickly because `requirement/spending.month-end-submissions` sets the target. `decision/0005-render-receipts-as-pdf` names the constraint in `basedOn`, and `decision/0004-serve-receipts-behind-one-proxy` names the requirement in `basedOn`; neither the seven years nor the two seconds is written in a decision.

**Checked by:** `litespec model check` reports `decision-basis-missing` (level A) for a technical decision whose `basedOn` names nothing, and counts a quality requirement as fulfilled when a technical decision names it in `basedOn`.

### Resolve Every Page Link

**ID:** `litespec/core/model#resolve-every-page-link`

**Statement:** Every Markdown link outside code on a page under the record roots MUST point at an existing file, by its relative path.

**Rationale:** A link that points at a moved file or a bare identifier reads as navigation and leads nowhere. Requiring every link to name an existing file by its relative path keeps the documentation clickable at every commit. Identifiers are cited in backticks instead, so the two never confuse.

**Checked by:** `litespec model check` reports `link-unknown` for a page link naming no file.

### Declare Size Stacks and Optional Types

**ID:** `litespec/core/model#declare-size-stacks-and-optional-types`

**Statement:** A project MUST declare its size, stacks, and optional types.

**Rationale:** Size decides which record types and test projects the checks require, while stacks decide which technology pages and checks apply. Without the declaration, the builder cannot tell a missing record from one the project never needed. Optional types switch on per project, so the declaration also names which ones this project carries.

**Example:** The running Claimwick example declares large with a server shape and two stacks.

**Checked by:** `litespec model check` reports `size-type-missing` and `record-invalid` for a project missing its declaration or a required type.

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

**Checked by:** `litespec model check` reports `use-case-success-missing` and `scenario-result-unknown` for a use case missing its success or a scenario naming no result.

### Derive Generic Refusals

**ID:** `litespec/core/model#derive-generic-refusals`

**Statement:** Generic refusals MUST be derived from access and input, never written as records.

**Rationale:** Forbidden and invalid input follow from the access grant and the input rules on every use case, so writing them by hand repeats one decision hundreds of times and drifts. The builder derives `result/<use-case>.forbidden` and `result/<use-case>.invalid-input`, and scenarios point at those ids. A written record with a generic id claims a decision the access grant already made.

**Example:** A scenario for `use-case/spending.submit-expense` points at `result/submit-expense.forbidden` with no result record of that id on the page.

**Reviewed for:** Which result records carry a generic refusal id? Look for ids ending in `.forbidden` or `.invalid-input` beside the use case pages.

### Author One Slice at a Time

**ID:** `litespec/core/model#author-one-slice-at-a-time`

**Statement:** Authoring MUST complete one feature from problem to scenario before starting the next.

**Rationale:** A feature worked from problem to scenario stays reviewable at every level: each record lands with the Intent above it already written. Parallel slices leave Behavior records resting on Intent nobody wrote yet, and reviewers cannot tell a settled choice from a guess.

**Reviewed for:** Approved Behavior records show approved Intent above them through their `fulfills` and `decides` links.

### Raise Modeling Questions with a Decider and a Date

**ID:** `litespec/core/model#raise-modeling-questions-with-a-decider-and-a-date`

**Statement:** A question found while modeling MUST travel up as a question carrying its decider and its decision date.

**Rationale:** A Behavior record written around an open question bakes the guess into states, rules, and scenarios. Sending the question up with its decider, its date, its recommendation, and its ballot keeps it owned and dated, while the Behavior record stays draft until the answer lands. A question past its date that cannot auto-approve is reported, so it never silently blocks.

**Checked by:** `litespec model check` reports `question-expired` (level A) for every unanswered question past its `decideBy` that cannot auto-approve.

### Hold Transcribed Records in Draft

**ID:** `litespec/core/model#hold-transcribed-records-in-draft`

**Statement:** A record with provenance transcribed MUST NOT stand reviewed while its fulfills or serves parent is unapproved.

**Rationale:** Transcription copies working code into a record during adoption, so the record describes what exists rather than what Intent asked for. Draft holds it visibly unfinished until its fulfills or serves parent is approved. `transcription-status-invalid` fails a transcribed record standing reviewed while its fulfills or serves parent is unapproved.

**Checked by:** `litespec model check` reports `transcription-status-invalid` for a transcribed record standing reviewed while its fulfills or serves parent is unapproved.

### End Transcription at the Named Decision

**ID:** `litespec/core/model#end-transcription-at-the-named-decision`

**Statement:** Every transcribed record MUST clear before the adoption decision is approved.

**Rationale:** Transcription mode exists to adopt working code, not to write Behavior from code forever. `adoption.transcriptionEndsWith` names the decision that ends the mode, and approving it closes the door: `transcription-expired` fails on any record still carrying provenance transcribed. Clearing means the record earns its Intent parent and drops the transcribed mark, or retires with the code it copied.

**Checked by:** `litespec model check` reports `transcription-expired` for a transcribed record after the adoption decision is approved.

### Compute Proof Statuses in the Builder

**ID:** `litespec/core/model#compute-proof-statuses-in-the-builder`

**Statement:** Implemented, verified, met, tested, exercised, stale, flaky, reachable, and effective status MUST come from the builder, never from handwriting.

**Rationale:** Handwritten status drifts: a record marked implemented stays marked after its handler is deleted. Computed status cannot drift, because the builder re-derives it from handlers, routes, scenarios, and test runs on every run. A use case reads implemented when a handler, automation, or endpoint marked with it carries it out, verified when every scenario has a passing proof through the public boundary that is not stale, and stale when the scenario hash or the test-body hash differs from `docs/proof/proofs.json`. A requirement reads met when everything fulfilling it is verified and its measure's check passes. A record reads tested when a passing test that asserts something links to it. A journey step reads exercised when a passing proof of a use case serves it. A proof reads flaky when its test both passed and failed within the runs read. Nothing above Implementation is computed from code: derivation from code into a record is forbidden, and code-computed facts live in the book and the model output.

**Example:** A use case reads implemented when its handler exists.

**Checked by:** `litespec model check` reports `use-case-unimplemented`, `proves-unreached`, and `refusal-unimplemented` for a use case whose handler, proof, or refusal code is missing, and `proof-invalid` (level A) for a test that both passed and failed within the runs read.

### Report Unverified Use Cases

**ID:** `litespec/core/model#report-unverified-use-cases`

**Statement:** A use case whose scenarios are not all proved MUST be reported as `use-case-unverified`.

**Rationale:** Nothing blocks on an unverified scenario day to day, so the release gate would depend on someone reading the book. Reporting the use case keeps daily work loose, and `litespec model check --release` makes the finding block, so the release stays honest without slowing ordinary work.

**Example:** Claimwick adds `use-case/spending.export-expenses` with two scenarios and one passing proof: `litespec model check` reports `use-case-unverified` for it and exits zero, and `litespec model check --release` blocks on it.

**Checked by:** `litespec model check` reports `use-case-unverified` (level A) for a use case whose scenarios are not all proved, and `litespec model check --release` blocks on it.

### Mark Every Check with Its Level

**ID:** `litespec/core/model#mark-every-check-with-its-level`

**Statement:** Every check MUST carry its level and block or report as that level states.

**Rationale:** The level tells a reader what a finding means: G reads records only and always blocks, S reads code through a stack reader and blocks only where the reader is qualified, and A reports and never blocks. `litespec model check` prints the level beside each finding, so a review tells a blocking defect from advice at a glance. A missing structural parent is level G for every record, drafts included; every other required link is coverage, level A for a tier 3 record and level G for a tier 1 record.

**Reviewed for:** Which findings lack a level letter, or block outside their level? Look at the output of `litespec model check`.

### Declare One Mark Style

**ID:** `litespec/core/model#declare-one-mark-style`

**Statement:** A project MUST write every mark in the one style that `marks` in `litespec.project.json` declares.

**Rationale:** Two styles in one project are two ways to do one job: a reader learns both, a review checks both, and a search for a record finds half its code. The style is a project choice made once, so every tool reads it from the project file and refuses the other style. A code fix converts a mark to the declared style.

**Example:** The Claimwick product declares `"marks": "xml"`, so its submit-expense handler carries a documentation tag naming `use-case/spending.submit-expense`, and an attribute naming the same use case is reported.

**Checked by:** `litespec model check` reports `mark-style-invalid` (level S) for a mark written in the style the project did not declare.

### Hold Each Finding to Its Own Status

**ID:** `litespec/core/model#hold-each-finding-to-its-own-status`

**Statement:** A level S finding about a record MUST block when the record itself is approved or reviewed, report without blocking when the record itself is draft, and stay silent when it is retired, with an inherited draft shown as a label.

**Rationale:** A draft record states work in progress, and code written ahead of it is the normal order of a slice: failing the build on it trains people to suppress findings, while hiding it loses the signal. Reporting without blocking keeps the finding visible until the record itself is approved or reviewed, and then it blocks. An inherited draft never silences the finding: it shows as a label beside it, so a new draft requirement marks the use cases that fulfil it without quieting their findings. A retired record states the past, so code still marked with it is reported through the mark, not through the record. A finding about no record, such as an unmarked type or a mark naming no record, follows its level.

**Example:** `use-case/spending.export-expenses` is a draft with no handler marked for it, so `use-case-unimplemented` prints with the draft marker and the check exits zero; once the use case is reviewed, the same finding blocks. An approved use case fulfilling a new draft requirement blocks on its findings, with the draft requirement shown as a label.

**Checked by:** `litespec model check` prints `draft` beside a non-blocking level S finding whose subject is itself a draft, shows an inherited draft as a label beside a finding about an approved or reviewed record, and exits zero when only non-blocking findings remain.

### Name the Lowest Record Each Test Proves

**ID:** `litespec/core/model#name-the-lowest-record-each-test-proves`

**Statement:** Every test MUST name in a `proves` mark the lowest record it verifies, from the kinds the marks table on this page lists for a test.

**Rationale:** A test outside the record tree is either proof of something the specification forgot to state or a test that verifies nothing anyone promised. Naming the lowest record it verifies places each test: a scenario or rule for behavior, a quality requirement for how well the product behaves, a technical decision for a technical choice, an external system or integration event for an agreement with the outside, and a provision for a structural rule the code must keep. A test that verifies a choice no record states shows a missing record, which is written at the level where the choice lives, as safety-critical standards resolve derived requirements; a test that verifies nothing a record should state is deleted. Proof stays layered: only scenario and rule proofs verify a use case, and a passing test that asserts something links to a quality requirement, a technical decision, an external system, an integration event, or a provision marks that record tested, so "the adapter is right" never reads as "the promise is kept". A background use case is verified through the passing proofs of the use cases it serves. A browser success proof asserts the result sentence or the scenario Then text.

**Example:** In Claimwick, the submit-expense acceptance test proves `scenario/submit-expense.submitting-with-a-receipt-succeeds`; a load test of month-end submissions proves `requirement/spending.month-end-submissions`; a receipt-rendering test proves `decision/0005-render-receipts-as-pdf`; and the layering test that Domain references no persistence package proves `litespec/backend/architecture#point-dependencies-inward`.

**Checked by:** `litespec model check` reports `mark-missing` and `mark-unknown` (level S) for a test that names no record or a record of a kind a test does not take.

### Implement Each Mapped Behavior Record Behind a Mark

**ID:** `litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark`

**Statement:** Every entity and saga aggregate, every automation and external system, and every use case reached through a `browser` or `api` entry point, MUST be named by a mark on the code element the marks table gives it.

**Rationale:** A mark points up, so a check reading marks alone finds code nobody recorded but misses a record nobody built. Checking each record that the marks table maps to code from the top closes the loop in the other direction: an entity or saga aggregate with no root, an automation with no event handler or host, an external system with no port, and a use case people reach with no endpoint. A store has no root, so it needs no mark. A use case with no screen calling it is reported as advice, because whether a browser reaches it depends on the deployment.

**Example:** `automation/spending.send-approver-reminder-on-expense-submitted` is reviewed and no event handler carries its mark, so the check reports `automation-unimplemented` and blocks.

**Checked by:** `litespec model check` reports `aggregate-unimplemented`, `automation-unimplemented`, `external-system-unimplemented`, and `use-case-endpoint-missing` (level S) for a record no mark names, where `aggregate-unimplemented` fires only for an entity or saga aggregate, and `use-case-unreached` (level A) for a browser use case no screen calls.

### Record a Project-Wide Lowering as an Exception

**ID:** `litespec/core/model#record-a-project-wide-lowering-as-an-exception`

**Statement:** A project MUST record an exception for every analyzer rule it lowers or switches off in an `.editorconfig` that applies beyond one file.

**Rationale:** Severity follows record status on its own, so a project-wide lowering in `.editorconfig` is a departure from the standard rather than a setting. An exception of kind `departure` names the provision, the reason and the expiry date, so the lowering is reviewed and ends instead of staying silent. One person may approve it.

**Example:** A project that lowers LS5001 to a suggestion in its root `.editorconfig` records an exception of kind `departure` citing `litespec/core/system#point-code-and-tests-up-to-records`.

**Reviewed for:** Which project-wide severity lowerings in `.editorconfig` lack an exception record? Look at every `.editorconfig` in the repository beside the exception records.

### Organize the Book by the Record Layers

**ID:** `litespec/core/model#organize-the-book-by-the-record-layers`

**Statement:** The book MUST outline a project by the four record layers, with the governance records beside them, and place each record under the record its identifier and links name.

**Rationale:** The book is how people read the model, so it shows the model's own hierarchy. A reader who learns Intent, Behavior, Implementation and Proof in the book reads the same structure in the docs folder, in the model output and in every other LiteSpec project. An outline of the book's own invention is a second hierarchy to learn, and it drifts from the one the records keep. Settled by [decision/0002-parent-for-every-record](../intent/decisions/0002-parent-for-every-record.md) on the evidence in [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence.md), [research/intent-hierarchy-evidence](../intent/research/intent-hierarchy-evidence.md), [research/book-outline-evidence](../intent/research/book-outline-evidence.md), and [research/established-traceability-practice](../intent/research/established-traceability-practice.md). Settled by [decision/0008-book-follows-record-layers](../intent/decisions/0008-book-follows-record-layers.md) on the evidence in [research/book-outline-evidence](../intent/research/book-outline-evidence.md), [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence.md), and [research/check-coverage-evidence](../intent/research/check-coverage-evidence.md).

**Example:** In the Claimwick product, `use-case/spending.submit-expense` sits under Behavior, its module and its aggregate; its results under it; each scenario under the result it shows; and the handler that carries its mark under Implementation.

**Reviewed for:** Which book headings or groupings are not a record layer, a record type, or a record the identifier names? Look at the book's outline beside this page's record type table.

### Name Book Labels with LiteSpec Terms

**ID:** `litespec/core/model#name-book-labels-with-litespec-terms`

**Statement:** Every label in the book MUST be a record type, a field name, a status, a glossary term, or the reads-as phrase the Links table states for a connection.

**Rationale:** LiteSpec already names every concept once, for people and agents alike. A friendlier synonym in the book, such as promise for requirement, is a second word for one concept: the reader learns it, then meets the real word in the records, the checks and the diffs, and has to translate. Settled by [decision/0008-book-follows-record-layers](../intent/decisions/0008-book-follows-record-layers.md) on the evidence in [research/book-outline-evidence](../intent/research/book-outline-evidence.md), [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence.md), and [research/check-coverage-evidence](../intent/research/check-coverage-evidence.md).

**Example:** A use case page heads its results "Results" and its scenarios "Scenarios", never "Endings" and "Examples".

**Reviewed for:** Which book labels are neither a record type, a field name, a status, a glossary term, nor the reads-as phrase the Links table states for a connection? Look at the book beside the glossary and the Links table.

### Explain the Book from LiteSpec Definitions

**ID:** `litespec/core/model#explain-the-book-from-litespec-definitions`

**Statement:** Every explanation the book shows MUST come from the glossary definition of the term or from the question its record type answers on this page.

**Rationale:** The book explains itself so nobody needs a learning curve, and the explanation stays true only when it has one source. An explanation written for the book drifts from the definition the records follow. Settled by [decision/0008-book-follows-record-layers](../intent/decisions/0008-book-follows-record-layers.md) on the evidence in [research/book-outline-evidence](../intent/research/book-outline-evidence.md), [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence.md), and [research/check-coverage-evidence](../intent/research/check-coverage-evidence.md).

**Example:** The book explains an exception with the glossary definition of Exception.

**Reviewed for:** Which explanations in the book are not a glossary definition or a record type question? Look at the book's explanations beside the glossary and the record type table.

### Show Only Computed Judgements in the Book

**ID:** `litespec/core/model#show-only-computed-judgements-in-the-book`

**Statement:** Every status, count, order and finding the book shows MUST be one the builder, a check, or the review queue computes.

**Rationale:** The book is a view of the model, not a second authority. A judgement only the book makes, such as its own idea of what to do next, cannot be checked, cannot fail a build, and cannot be found by an agent reading the model output. Settled by [decision/0008-book-follows-record-layers](../intent/decisions/0008-book-follows-record-layers.md) on the evidence in [research/book-outline-evidence](../intent/research/book-outline-evidence.md), [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence.md), and [research/check-coverage-evidence](../intent/research/check-coverage-evidence.md).

**Example:** The book lists approval batches in the order `litespec model review` gives them.

**Reviewed for:** Which statuses, counts, orders or findings in the book does no builder output, check or review queue produce? Look at the book beside the model output.

### Add a Missing Fact to LiteSpec Before the Book Shows It

**ID:** `litespec/core/model#add-a-missing-fact-to-litespec-before-the-book-shows-it`

**Statement:** A fact the book needs that LiteSpec does not define MUST become a record field, a computed status, or a check in LiteSpec before the book shows it.

**Rationale:** Rendering the book is a probe: what it cannot show is a gap in the specification. Closing the gap in LiteSpec gives every project the fact, checked; closing it in the book gives one view an unchecked fact. Settled by [decision/0008-book-follows-record-layers](../intent/decisions/0008-book-follows-record-layers.md) on the evidence in [research/book-outline-evidence](../intent/research/book-outline-evidence.md), [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence.md), and [research/check-coverage-evidence](../intent/research/check-coverage-evidence.md).

**Example:** The book needs to say that a refusal has no scenario, so LiteSpec gains a check for it, and the book shows that check's findings.

**Reviewed for:** Which facts does the book show that no record field, computed status or check defines? Look at the book beside the model output and the finding codes.

### Point Every Book Entry to Its Source

**ID:** `litespec/core/model#point-every-book-entry-to-its-source`

**Statement:** Every record, code element, test and finding the book shows MUST link to the file and line it comes from.

**Rationale:** The book is where a reader notices what to change, and the source is where the change is made. A link to the file and line makes the next step one click, and it shows that every entry comes from a written record or from code, not from the book. Settled by [decision/0008-book-follows-record-layers](../intent/decisions/0008-book-follows-record-layers.md) on the evidence in [research/book-outline-evidence](../intent/research/book-outline-evidence.md), [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence.md), and [research/check-coverage-evidence](../intent/research/check-coverage-evidence.md).

**Example:** The submit-expense use case page links to `docs/behavior/modules/spending/expenses/submit-expense.md` at its first line, and its handler to the handler file at the class.

**Reviewed for:** Which book entries lack a link to their file and line? Look at the book's record, code, test and finding entries.

## Conventions

### Render Generated Regions from the Model

**ID:** `litespec/core/model#render-generated-regions-from-the-model`

**Default:** A generated region renders only facts derived from records.

**Replacement:** Code-computed facts live in the book and the model output.

**Rationale:** A generated region sits inside a record page, so everything in it reads with record authority. Facts computed from code, such as implemented and verified counts, describe the world rather than the promise, and rendering them beside records would lend them authority they do not hold. The book and the model output carry code-computed facts where readers meet them as computed.

**Reviewed for:** Which generated regions render code-computed facts? Look inside the generated fences beside the book and the model output.
