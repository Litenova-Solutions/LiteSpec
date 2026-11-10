# Project Model

**Stack:** any

**Concern:** records

## Intent

A LiteSpec project describes itself with records, written statements of fixed types joined by fixed links. This page states the four record layers, the record types with their placement and identifiers, the record files, the explanation files, the documents, the common fields, the links, the marks, the authority order, the authoring order, the completion stages, the screen captures, the model builder, the check levels, and the book. People write Intent, agents write Behavior from it, and the model builder computes how code and tests connect to the records.

## Concepts

### The Four Record Layers

Each record layer answers one question, is written from the record layer above it, and owns its records.

| Record layer | Question | Writes | Records |
|:--|:--|:--|:--|
| Intent | Why build it, and what do we promise? | people and agents | product, goal, problem, feature, requirement, constraint, decision, research, journey, actor, role, module, term (`layer: intent`) |
| Behavior | How does the system behave to keep the promises? | agents | aggregate, rule, event, integration-event, use-case, result, scenario, reaction, job, notification, setting, permission, entry-point, screen, external-system, term (`layer: behavior`) |
| Implementation | How is it built and operated? | agents | code, technical decisions, runbooks, operating procedures |
| Proof | How do we know it holds? | the builder | tests, check results, computed completion, the rendered book |

Governance is a group of records beside the four record layers, not a fifth layer: the `exception` record attaches to any record layer.

Three translation rules join the record layers. Specify moves Intent to Behavior: it adds states, invariants, refusals, consistency, data classes with their erasure positions, freshness, and access with tenant scope, and it states every price, fee, rate, term, market, payer, retention period for personal data, observable freshness, and default or figure that differs by tenant, market, or date. Implement moves Behavior to Implementation: it adds the code shapes the stack pages fix, and adds no observable outcome no result declares, no edge no record declares, no write outside `changes`, and no access wider than the grant. Verify moves Implementation to Proof: one test proves each scenario through the public boundary, and a proof goes stale when its scenario hash or its test-body hash changes.

### Record Types and Page Styles

Each type answers one question.

| Type | Record layer | Question |
|:--|:--|:--|
| `product` | intent | What is built, and for whom? |
| `goal` | intent | Which measurable outcome does the product serve? |
| `problem` | intent | Which pain does someone have today? |
| `feature` | intent | What is offered against a problem? |
| `constraint` | intent | Which outside rules hold? |
| `research` | intent | Which outside facts inform a choice? |
| `decision` | intent, implementation | Which choice holds, and what changes it? |
| `requirement` | intent | What does the product do or guarantee? |
| `journey` | intent | Which ordered steps reach one goal? |
| `actor` | intent | Who or what touches the product? |
| `role` | intent | Which permission sets exist? |
| `module` | intent | Which product areas exist? |
| `aggregate` | behavior | Which things stay consistent? |
| `rule` | behavior | What holds? |
| `use-case` | behavior | Which use cases does the product offer? |
| `result` | behavior | How does a use case end? |
| `scenario` | behavior | Which example proves the result? |
| `event` | behavior | What happened? |
| `integration-event` | behavior | Which facts cross the boundary? |
| `reaction` | behavior | Which work runs when an event arrives? |
| `job` | behavior | Which work runs on a schedule, once, or by hand? |
| `notification` | behavior | Which person-facing messages go out? |
| `setting` | behavior | Which adjustable values hold, and who sets them? |
| `permission` | behavior | Which single actions are allowed? |
| `entry-point` | behavior | How do people reach the product? |
| `screen` | behavior | Which page does a person see through an entry point, and what does it offer? |
| `external-system` | behavior | Which outside system exchanges what? |
| `runbook` | implementation | How is it run or recovered? |
| `exception` | governance | Which rule is set aside or which check stays red, and until when? |
| `term` | intent, behavior | Which word means what? |

The [intent page](intent.md) states the Intent records, the [behavior page](behavior.md) states the Behavior records, and the [access page](access.md) states the access records.

Each record file holds the fields the [Authoring Standard](authoring.md) names for its type. List records hold many entries: a rule-set page holds rules, a setting-set page holds settings, and the permission list per module holds permissions. The retention-schedule page holds the retention rows at `.litespec/behavior/retention-schedule.yaml`. The retention schedule is a container page style, not a record type.

Every record states something the software does, holds, refuses at runtime or runs on, and translates into code, results, tests or infrastructure. The [intent page](intent.md) states the test each Intent type meets.

### Placement Principles

The placement of every record follows nine principles, from the product root down to a test.

1. **One tree from the product.** Every record has exactly one parent, and only the product has none. Every other link is a connection, and every inverse link is computed.
2. **The container is the parent.** An entry's parent is the record file that holds it, except a scenario, whose parent is its result. A record file's parent is its folder node: the product, a module, an aggregate or an entry point. No record writes an up-link that its location already states.
3. **Four kinds of node, and only four.** Product, module, aggregate and entry point are the nodes a folder can stand for. Every other folder name is a fixed type folder from a closed set per layer and level.
4. **The lowest node that holds every user.** A record sits under the lowest node that holds every record linking to it within its layer. The author places it, and the tool validates it.
5. **Locality.** Within one layer, a record under a module is linked from another module only along a declared module dependency or through an integration event. Links from one layer down to the next may cross modules, because that is how one capability spans modules.
6. **Two identifiers.** The identifier is short, unique in the product, and what every link and citation uses. The qualified identifier is computed from the identifier segments plus placement, never written, shown in the book and the CLI, and accepted by `model query`.
7. **One name per module in every layer and in code.** `intent/modules/finance/`, `behavior/modules/finance/`, the backend source trees, the test projects and the frontend modules all use the declared module name.
8. **Code and tests follow the qualified identifier.** The folder and namespace of code and tests that mark a record match that record's qualified identifier.
9. **Computed stays computed.** Proof and the generated folders hold no authored records.

### The Placement Table

The table is frozen: it gives each type its layer, levels, folder, parent, identifier and qualified form, and a record type gains a home only by changing this table. Folders are relative to `.litespec/`.

| Type | Layer | Levels | Folder | Parent | Identifier | Qualified Form |
|:--|:--|:--|:--|:--|:--|:--|
| `product` | Intent | product | `intent/product.yaml` | none | `product/<name>` | `product/<name>` |
| `goal` | Intent | product, module | `intent/goals/`, `intent/modules/<m>/goals/` | product or module | `goal/<name>` | `goal/<name>`, `<module>/goal/<name>` |
| `problem` | Intent | product, module | `intent/problems/`, `intent/modules/<m>/problems/` | product or module | `problem/<name>` | `problem/<name>`, `<module>/problem/<name>` |
| `feature` | Intent | module | `intent/modules/<m>/features/<feature>.yaml` | module | `feature/<name>` | `<module>/feature/<name>` |
| `requirement` | Intent | entry | in its feature file | feature | `requirement/<feature>.<name>` | `<module>/feature/<feature>/requirement/<name>` |
| `constraint` | Intent | product, module | `intent/constraints/`, `intent/modules/<m>/constraints/` | product or module | `constraint/<name>` | `constraint/<name>`, `<module>/constraint/<name>` |
| `decision` | Intent, Implementation | product, module | `intent/decisions/`, `intent/modules/<m>/decisions/`, `implementation/decisions/`, `implementation/modules/<m>/decisions/` | product or module | `decision/<number>-<name>` | `decision/<number>-<name>`, `<module>/decision/<number>-<name>` |
| `research` | Intent | product, module | `intent/research/`, `intent/modules/<m>/research/` | product or module | `research/<name>` | `research/<name>`, `<module>/research/<name>` |
| `journey` | Intent | product | `intent/journeys/` | product | `journey/<name>` | `journey/<name>` |
| `actor` | Intent | product | `intent/actors/` | product | `actor/<name>` | `actor/<name>` |
| `role` | Intent | product | `intent/roles/` | product | `role/<name>` | `role/<name>` |
| `module` | Intent | module | `intent/modules/<m>/module.yaml` | product | `module/<name>` | `<module>` |
| `term` | Intent, Behavior | product | `glossary.yaml` | product | `term/<name>` | `term/<name>` |
| `aggregate` | Behavior | module | `behavior/modules/<m>/<aggregate>/aggregate.yaml` | module | `aggregate/<module>.<name>` | `<module>/<aggregate>` |
| `use-case` | Behavior | aggregate, module | `behavior/modules/<m>/<aggregate>/<use-case>.yaml`, `behavior/modules/<m>/use-cases/<use-case>.yaml` | aggregate or module | `use-case/<module>.<name>` | `<module>/<aggregate>/use-case/<name>`, `<module>/use-case/<name>` |
| `result` | Behavior | entry | in its use case file | use case | `result/<use-case>.<name>` | `<use-case-qualified>/result/<name>` |
| `scenario` | Behavior | entry | in its use case file | result | `scenario/<use-case>.<name>` | `<use-case-qualified>/scenario/<name>` |
| `event` | Behavior | aggregate | in `aggregate.yaml` or in `<aggregate>/events/` | aggregate | `event/<aggregate>.<name>` | `<module>/<aggregate>/event/<name>` |
| `rule` | Behavior | aggregate, module, product | `behavior/modules/<m>/<aggregate>/rules/`, `behavior/modules/<m>/rules/`, `behavior/rules/` | its folder node | `rule/<home>.<name>` | `<module>/<aggregate>/rule/<name>`, `<module>/rule/<name>`, `rule/<name>` |
| `reaction` | Behavior | module | `behavior/modules/<m>/reactions/` | module | `reaction/<module>.<name>` | `<module>/reaction/<name>` |
| `job` | Behavior | module | `behavior/modules/<m>/jobs/` | module | `job/<module>.<name>` | `<module>/job/<name>` |
| `notification` | Behavior | module | `behavior/modules/<m>/notifications/` | module | `notification/<module>.<name>` | `<module>/notification/<name>` |
| `setting` | Behavior | module | setting-set pages under `behavior/modules/<m>/settings/` | module | `setting/<module>.<name>` | `<module>/setting/<name>` |
| `integration-event` | Behavior | module | `behavior/modules/<m>/integration-events/` | module | `integration-event/<name>` | `<module>/integration-event/<name>` |
| `permission` | Behavior | module | `behavior/modules/<m>/permissions.yaml` | module | `permission/<name>` | `<module>/permission/<name>` |
| `entry-point` | Behavior | product | `behavior/entry-points/<entry-point>/entry-point.yaml` | product | `entry-point/<name>` | `<entry-point>` |
| `screen` | Behavior | entry point | `behavior/entry-points/<entry-point>/screens/` | entry point | `screen/<entry-point>.<name>` | `<entry-point>/screen/<name>` |
| `external-system` | Behavior | product, module | `behavior/external-systems/`, `behavior/modules/<m>/external-systems/` | its folder node | `external-system/<name>` | `external-system/<name>`, `<module>/external-system/<name>` |
| `runbook` | Implementation | product, module | `implementation/runbooks/`, `implementation/modules/<m>/runbooks/` | product or module | `runbook/<name>` | `runbook/<name>`, `<module>/runbook/<name>` |
| `departure` | Governance | product, module | `governance/departures/`, `governance/modules/<m>/departures/` | product or module | `departure/<name>` | `departure/<name>`, `<module>/departure/<name>` |

Inside `behavior/modules/<m>/`, a subfolder is either a fixed type folder from the closed set or an aggregate holding `aggregate.yaml`. Anything else is a `folder-invalid` finding. A module with one aggregate of the module's own name may stay flat.

`intent/modules/<m>/` and `behavior/modules/<m>/` name the same declared modules. A folder under either that names no declared module, and a module folder without its module, get a `module-undeclared` finding. A record of a type in the wrong folder and a record type outside its allowed levels are `record-placement-invalid` findings.

A module, an aggregate, and an entry point are their own qualified form: the qualified form of `module/finance` is `finance`, and the qualified form of any record under them builds from that node name. The qualified form builds from the identifier segments plus placement, never from raw folder names.

A rule home is an aggregate name or a module name. A rule identifier keeps its home segment when the rule moves; only identifiers whose first segment names a module change with a module move. Policy rules are Intent requirements or constraints; an invariant sits under its aggregate or module, and an invariant across modules sits at product level, as the one stated exception. Module-level rules live in `behavior/modules/<m>/rules/` rule-set files; `module.yaml` holds no rules, and a rule in `module.yaml` is a `record-placement-invalid` finding.

An integration event has exactly one publishing module; a second publisher is an `event-publisher-invalid` finding. List records (rule sets, setting sets, the permission list per module) are files whose entries take the file's folder node as parent. Each entry point is one record in its own folder, with `screens/` beside it.

`fulfilledBy` does not exist: a use case keeps `fulfills` pointing up to the requirement, and the book computes the inverse. `role.allows` is a declared down-link, like `decides`. Module `dependsOn` may not form a cycle; a `module-dependency-invalid` finding reports one.

Decision numbers come from one sequence across every home, allocated by `litespec new decision`, which takes the next free number. A repeated number is a `decision-number-duplicate` finding.

Implementation content that is not a record, and Proof content left by a run, sit in fixed folders:

| Content | Folder |
|:--|:--|
| UI catalog, captures and patterns per entry point | `implementation/entry-points/<entry-point>/` |
| Shared pattern catalog | `implementation/patterns/` |
| Product copy rules | `implementation/writing/` |
| API contract | `implementation/api/` |
| Guides | `implementation/guides/` |
| Operating documents | `implementation/operations/` |
| Generated registers, gate files, operating logs | `proof/` |

### The Docs Layout

A consumer keeps its documentation in one folder per record layer, so the folder a page sits in says who writes it and what it may contain.

| Folder | Record layer | Holds |
|:---|:---|:---|
| `.litespec/intent/` | Intent | The product, goals, problems, features with their requirements under modules, constraints, decisions, roles, journeys, actors, research, and the product documents the business publishes, such as legal texts |
| `.litespec/behavior/` | Behavior | Modules with their aggregates, use cases, rules, events, reactions, jobs, notifications, settings, integration events and permissions; entry points with their screens; external systems; the retention schedule |
| `.litespec/implementation/` | Implementation | Technical decisions under `decisions/` at product or module level; runbooks and operating procedures; the UI catalog, captures and patterns per entry point; the shared pattern catalog under `patterns/`; product copy rules under `writing/`; the API contract under `api/`; and guides for people working in the code. The guides describe code and hold no records |
| `.litespec/proof/` | Proof | Evidence the tools compute or a run leaves behind: generated registers, gate files, and operating logs. No person writes a fact here that a tool can compute |
| `.litespec/governance/` | Governance | Departures at product level or under modules |

A record lives in its record file, an explanation file may sit beside it, and a document holds no record. The file name joins a record file to its explanation file: `<name>.yaml` with `<name>.md` in the same folder.

The glossary (`.litespec/glossary.yaml`) and the example cast stay at the top of `.litespec/`, because every record layer uses them.

Proof holds no authored records. Tests are code, placed by the rule the Code and Tests section states, and the model computes completion from their marks and results.

### Pages and Entries

A page record is one record file `<name>.yaml` holding one YAML mapping: the page record with its entries in `records`. An entry record is an object in the `records` sequence of that mapping. Terms are the exception: each term is an entry in the glossary. The Placement Table above names each type's home.

Container pages (`permission-list`, `setting-set`, `rule-set`, `retention-schedule`) are record files: they hold entries.

The decision type has two layers: a product decision lives under Intent, and a technical decision lives under Implementation. Both share one number sequence.

### Record Files

A record file is one file `<name>.yaml` holding one YAML mapping: the page record, or a container page, with its entries in `records`. The mapping holds exactly the fields the record schema defines for its type. A record file holds no Markdown and no frontmatter fences. Every `.yaml` file under a record root is a record file, except the glossary file; a file that does not parse, or whose top level names no record type or container page style in `type`, is a `record-invalid` finding. `.yml` files are not read.

### Explanation Files

An explanation file is the optional `<name>.md` beside its record file, with the same name in the same folder. It is plain Markdown with no frontmatter. It opens with `# <record title>` and uses H2 sections. It explains the record for people and agents, and it states no field value a check reads. One explanation file serves one page; entries have no file of their own, and the page explanation cites them by identifier.

### Documents

A document is a `.md` file under a record root with no `.yaml` of the same name beside it. It is Markdown with an optional YAML frontmatter block between `---` lines, holding only a document page style in `type` and its common fields. The document page styles are `index`, `tutorial`, `how-to`, `reference`, `command`, `configuration`, `glossary`, and `example-cast`. A document holds no record type and no entries. A `.md` file with no frontmatter and no record file beside it is an orphaned explanation, except a file named `README.md`, which is a document. Document pages may sit at a layer root.

### The Split Rule

Anything a check reads is a field in the record file. Anything that only helps someone understand goes in the explanation file. Every record carries one `summary`, and a record that makes a testable claim also carries one `statement`. Example, Notes, Known Limits, Evidence, Context, Decision, Answer, Notes on Sources, and any project-specific section live in the explanation file under the same H2 heading.

### Article Types

The article types are `research` and `decision`. Their explanation file is required, and it carries the required H2 sections: Answer for research, and Context, Options Considered, Decision, and What Would Reopen It for decision.

### Canonical Form

Every record file uses one canonical form: block mappings and sequences with two-space indentation, key order from the record schema, multi-line text in `|` block scalars, and quoting only where a scalar would resolve to a non-string. The [Authoring Standard](authoring.md) states the YAML reader. `litespec model format` rewrites every record file into canonical form, and a record file that differs from its canonical text is a `format-drift` finding.

### Citations in Prose

A Markdown link whose target is a record identifier cites that record, with an optional `#<step-id>` for a journey step. Link checks resolve the identifier against the model's identifiers, citation checks count it, and the book links it to the record. Prose cites a record by its identifier, never by its file path.

### Identifiers

An identifier names its record type first, then the record name, with the owning home in between when the type has one. The name carries one to six lowercase hyphenated words, chosen by a person; a decision counts its number as a word. Names follow the grammar the [record language page](language.md) states.

| Type | Form | Example |
|:---|:---|:---|
| `product` | `product/<name>` | `product/claimwick` |
| `goal` | `goal/<name>` | `goal/faster-reimbursement` |
| `problem` | `problem/<name>` | `problem/expenses-wait-weeks` |
| `feature` | `feature/<name>` | `feature/approval-reminders` |
| `constraint` | `constraint/<name>` | `constraint/tax-receipt-retention` |
| `research` | `research/<name>` | `research/approval-time-benchmarks` |
| `decision` | `decision/<number>-<name>` | `decision/0001-receipt-threshold` |
| `requirement` | `requirement/<feature>.<name>` | `requirement/approval-reminders.receipt-over-threshold` |
| `journey` | `journey/<name>` | `journey/submit-and-reimburse` |
| `module` | `module/<name>` | `module/spending` |
| `aggregate` | `aggregate/<module>.<name>` | `aggregate/spending.expense` |
| `rule` | `rule/<home>.<name>` | `rule/expense.approver-is-not-submitter` |
| `use-case` | `use-case/<module>.<name>` | `use-case/spending.submit-expense` |
| `result` | `result/<use-case-name>.<name>` | `result/submit-expense.receipt-missing` |
| `scenario` | `scenario/<use-case-name>.<name>` | `scenario/submit-expense.submitting-with-a-receipt-succeeds` |
| `event` | `event/<aggregate-name>.<past-tense>` | `event/expense.submitted` |
| `integration-event` | `integration-event/<name>` | `integration-event/expense-submitted` |
| `reaction` | `reaction/<module>.<name>` | `reaction/spending.send-approver-reminder-on-expense-submitted` |
| `job` | `job/<module>.<name>` | `job/spending.expire-lapsed-holds` |
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

A screen's `<name>` follows the same one-to-six-word rule as every other name; its `<entry-point>` is the name of the entry point it belongs to, and agrees with its folder. A rule home is an aggregate name or a module name. A use case name is unique across the project, so a result or a scenario cites only that name. An aggregate name is unique across the project. A requirement's feature segment names its feature, and the feature file holds it. A rule name keeps the negation its statement makes. Every identifier citation resolves to a written record.

The folder wins. A record that moves to another module changes the module segment of its identifier and every citation in the same change. An identifier whose home segment disagrees with its folder is an `identifier-folder-drift` finding. Each name segment stays unique in the product, and a `name-duplicate` finding reports a duplicate. Folders do not narrow uniqueness.

A journey step id is `journey/<name>#<step-id>`, with step ids `s1`, `s2`, and so on. A derived generic result is `result/<use-case>.forbidden` or `result/<use-case>.invalid-input`. Scenarios can point at derived results. Derived results are never written as records.

### Qualified Identifiers

The qualified identifier is computed from the identifier segments plus placement, and never written, like a fully qualified type name. Links never use it. The book and the CLI show both identifiers, and `litespec model query` accepts either. One grammar per node kind:

| Placement | Qualified identifier |
|:---|:---|
| Product level | `<type>/<name>`, the identifier itself |
| Module level | `<module>/<type>/<name>` |
| Under an aggregate | `<module>/<aggregate>/<type>/<name>` |
| An entry | the qualified identifier of its file, then `/<type>/<name>` |
| Under an entry point | `<entry-point>/<type>/<name>` |

### Common Fields

Every written record carries the shared fields in this table, in its record file. Field names follow `litespec/core/language#name-each-field-by-its-word-class`.

| Field | Written | Meaning |
|:---|:---|:---|
| `type` | always | The record type in kebab case |
| `id` | always | The identifier |
| `title` | always | The display title, free to change |
| `terms` | when used | The term identifiers the record uses |
| `records` | on a page with entries | The entries the page holds |

`risk` lives on `permission` entries only: the analyzers generate the permission table with its risks into code, so it is a product fact there.

A field that records the history of the record is gone, because git holds it. A field that states a fact about the product or the world, including a future date, stays: `expiresOn` on an exception and `readOn` beside an outside source.

### Links

Each written link lives as a field on the record it starts from: a decision carries `decides`, a rule carries `fulfills`, and so on. The table names every written link field, the record that carries it, and what it points at. Each link states how it reads in both directions, and the book takes its connection labels from those two phrases.

| Field | Written on | Points at | Reads as | Read back as |
|:---|:---|:---|:---|:---|
| `decides` | decision | Requirements, features, rules, constraints, and the permissions, roles, actors, exceptions, entry points, settings, or decisions an engineering choice settles | decides | is decided by |
| `basedOn` | decision, rule, data class, retention processor, external system | Constraints, research, and on a technical decision the requirements it serves | is based on | is a basis for |
| `addresses` | product | Actors | addresses | is addressed by |
| `affects` | problem | Actors | affects | is affected by |
| `solves` | feature | Problems | solves | is solved by |
| `advances` | feature | Goals | advances | is advanced by |
| `serves` | use case, reaction, job | Journey steps | serves | is served by |
| `serves` | journey | The goal it walks toward | serves | is served by |
| `cites` | requirement | Goals, problems, features, constraints, research, and decisions | cites | is cited by |
| `cites` | journey | Requirements it shows | cites | is cited by |
| `fulfills` | rule, use case, reaction, job, notification, setting | Requirements | fulfills | is fulfilled by |
| `governs` | rule | Aggregates, use cases | governs | is governed by |
| `transitions.by` | aggregate | The use case that moves the transition | is moved by | moves |
| `refusal` | rule | The refusal result carrying its code | is refused as | is the refusal for |
| `result` | scenario | The written or derived result it ends in | ends in | is shown by |
| `raises` | use case | Events | raises | is raised by |
| `access.grant` | use case | A permission, a self scope, anonymous, or system | is granted through | grants |
| `reachedThrough` | use case | Entry points | is reached through | reaches |
| `address` | screen | The route template it serves; a screen carries `address` or `host`, never both | is addressed at | is the address of |
| `host` | screen | The screen an overlay with no address of its own opens on; a screen carries `address` or `host`, never both | opens on | hosts |
| `offers` | screen | The use cases a person can start from it | offers | is offered by |
| `serves` | screen | The journey steps it carries a person through | serves | is served by |
| `floorplan` | screen | The floorplan it composes | is composed from | is composed into |
| `changes` | use case, reaction, job | Aggregates | changes | is changed by |
| `reads` | use case, reaction, job | Aggregates, and on a use case the settings it reads | reads | is read by |
| `effects` | use case, reaction, job | Use cases, notifications, integration events, external system operations | effects | is effected by |
| `triggeredBy` | reaction | Domain events or integration events | answers | is answered by |
| `binds` | rule | Aggregates | binds | is bound by |
| `exercises` | scenario | Rules | exercises | is exercised by |
| `freshness.authority` | use case | The record holding the authoritative value | is advised by | advises |
| `erasure.operation` | aggregate | The external system operation erasing the data class | is erased through | erases |
| `erasure.event` | aggregate | The integration event carrying the erasure | is erased through | carries the erasure of |
| `holds` | actor | Roles at a scope | holds | is held by |
| `allows` | role | Permissions | allows | is allowed by |
| `actor` | journey, external system | The actor walking the journey; the actor the system acts as | is walked by / acts as | walks / is acted as by |
| `translates` | integration event | Domain events | translates | is translated by |
| `dependsOn` | module | Modules | depends on | is depended on by |
| `runbook` | job | The runbook answering the job | is answered by | answers |
| `covers` | departure | The provisions, pages or finding codes the departure sets aside | covers | is covered by |
| `subject` | departure | The records the departure covers, including journey steps (`journey/<name>#s<n>`) for the step findings | applies to | is applied to |
| `terms` | any record | Terms | uses | is used by |

Behavior records point up: `serves` on a use case, reaction, job, or screen points to journey steps, `serves` on a journey points to its goal, `advances` on a feature points to goals, and `fulfills` points to a requirement. Records never point down, except `decides`, which names the records a decision settles, `allows`, which names the permissions a role grants, and `holds`, which names the roles an actor holds at a scope: a journey names no use case, and a requirement names no rule.

Placement needs no link field: it follows the folder. The folder node is the parent, and `solves` on a feature and `serves` on a journey remain connections that carry meaning without deciding placement. The builder computes the remaining links from code, routes, and tests: `implements` from a code mark to the record it names, `follows` from a code mark to the provision, decision, or exception that shapes the code, `calls` from a screen to a use case, and `proves` from a test mark to a scenario or rule. Code and tests point up to the record they serve through their marks, with each handler implementing exactly one record. The Placement Table above names each record parent.

### Parents

Every record except the product has exactly one parent. The book places each record once, under its parent. Every other link on the record is a connection: it renders with the reads-as phrase the Links table states, never as a second placement. Every inverse link is computed, never written.

The container is the parent. An entry's parent is the record file that holds it, except a scenario, whose parent is its result. A record file's parent is its folder node: the product, a module, an aggregate or an entry point. List records are files whose entries take the file's folder node as parent. Types at product level take the product as parent with no field written: a project has exactly one product, so the field would repeat one value on every record.

The product has no record parent; its parent is the project that declares it. Intent, Behavior, Implementation, Proof, and Governance are transparent group nodes on the path from product to record: a group node is never a parent and never satisfies the parent rule.

The Placement Table above names each type's parent.

### Marks

The mark rules keep their one home on this page: the marks table below states the mark each code element carries, and the [Write Code for a Use Case](../backend/write-code-for-a-use-case.md) page states where each element goes and what checks it.

A mark is a written link from a code element to the record it serves, in one of two kinds: `implements` names the record served and `follows` names the provision, decision, or exception that shapes the code. It holds the record identifier, and every tool reads it the same way: the model builder and the analyzers. A folder path stays the map a reader follows through the code, and it is never the link, except for a Domain constituent type, which takes no mark and belongs to the aggregate whose folder holds it (`litespec/backend/domain#place-every-constituent-type-in-its-aggregates-folder`).

| Record layer the code serves | Code element | Mark names |
|:--|:--|:--|
| Domain | Aggregate root | `aggregate/<module>.<name>` |
| Domain | Aggregate root method that changes state | `use-case/<module>.<name>`, exactly one: the use case whose act it performs |
| Domain | Domain event | `event/<aggregate>.<fact>` |
| Domain | Refusal type for one rejected rule | `rule/<home>.<name>` |
| Domain | Domain service holding one rule calculation | `rule/<home>.<name>` |
| Domain | Constituent type: identifier, state type, closed set, child entity, value object, repository interface | No mark: the builder derives its aggregate from its folder |
| Application | Command or query handler | `use-case/<module>.<name>`, exactly one |
| Application | Event handler | `reaction/<module>.<name>` |
| Application or Infrastructure | Job host | `job/<module>.<name>` |
| Application | Port for an external system | `external-system/<name>` |
| Infrastructure | Adapter implementing that port | `external-system/<name>` |
| API | Endpoint | `use-case/<module>.<name>` |
| Frontend | Use-case module | `use-case/<module>.<name>` |
| Frontend | Route entry: a web route file, a Blazor `@page` component, or an Android destination activity or fragment class | `screen/<entry-point>.<name>`, exactly one |
| Operations | Deployment, proxy, host, or CI workflow file listed in `paths.operations` | One or more `follows` marks in the file's `#` comments, such as `# <follows>decision/<number>-<name></follows>` |
| Operations | Alert rule in a file listed in `paths.alerts` | A `requirement` label naming a quality requirement and a `runbook` label naming a runbook |
| Tests | Test | The lowest record it verifies: `scenario/<use-case>.<name>` or `rule/<home>.<name>` for behavior; `requirement/<feature>.<name>` of kind quality; `decision/<number>-<name>` for a technical choice; `external-system/<name>` or `integration-event/<name>` for an outside agreement; `litespec/<page>#<slug>` for a provision the code must keep |
| Application, Infrastructure or API | Technical class: a top-level class that declares a method, or an options class that binds a setting, and takes no other mark. Nested types, records, interfaces, generated code, migrations, and model snapshots are never technical classes | One or more: a Behavior record it serves in an `implements` mark, or `litespec/<page>#<slug>`, `decision/<number>-<name>` or `exception/<name>` in a `follows` mark |

A code element serving a record names it in an `implements` mark. A technical class names the record it serves in an `implements` mark, as an input validator names its use case and a repository its aggregate. Where it serves no record, it names the reason it is built the way it is in a `follows` mark: the LiteSpec provision that requires its shape, the technical decision that chose it, or the exception that allows it. Any other code element may carry a `follows` mark as well.

The tools recognize code elements through the semantic model and then expect their mark: the generated mark attribute by its full name, and a handler by the interface it implements, wherever that interface comes from. A port and a domain service are recognized by their mark, and a job host by its hosted-service shape or by its mark. A public method on an aggregate root that changes state needs its use-case mark even when it raises no event, so an unmarked port is found only from the top, through its external system.

Seven provisions divide the marks between them: `litespec/core/system#point-code-and-tests-up-to-records` requires the mark, `litespec/core/system#point-every-technical-class-up-to-its-record-or-choice` covers technical classes, `litespec/core/model#declare-one-mark-style` fixes the style, `litespec/core/model#match-each-backend-mark-to-its-folder` matches each backend mark to its folder, `litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark` checks each mapped record from the top, `litespec/core/model#name-the-lowest-record-each-test-proves` sets what a test names, and `litespec/backend/testing#mark-the-test-that-proves-each-scenario` places the test mark.

Mark style belongs to the language. C# marks use the one style the project declares as `marks` in `.litespec/project.yaml`: `xml` for a documentation tag or `attribute` for the generated attribute. TypeScript marks use the typed constant, and marks in YAML and other operations files use the comment. One shared parser reads all three forms, each reader records the style it saw, and `mark-style-invalid` compares styles within one language only. A browser test carries its `proves` mark as a test annotation built by the `proves` helper the generated records file exports. A runtime value a record already states, such as an event classification or a refusal code, is generated from the mark and the record where the stack generates code, and checked against the record where a person writes it.

### Code and Tests

Backend code follows the module layout (module, then aggregate, then use case, in one tree), or the five project trees under the `layer-projects` extension. The namespace and folder of a type that marks a record match the record's qualified identifier. A handler marking `finance/payout/use-case/request-payout` sits in `{Product}/Finance/Payout/RequestPayout/`, or in `{Product}.Application/Finance/Payout/RequestPayout/` under `layer-projects`.

Tests mirror the code they prove, with the same check: a test marking a scenario sits under the qualified path of the scenario's use case.

Frontend code follows "Organize a Frontend into Frame, Routes, and Modules", with module folders named after declared modules and routes grouped by entry point.

The check lives in one place and is reported once.

### Authority

Git is the decision record. A record in the repository is decided, and a record no longer wanted is deleted.

Intent outranks Behavior. When two records disagree, the higher one wins: constraint over product, product over requirement, requirement over rule, rule over use case, use case over result, and result over scenario. Inside one record, a must beats a should. Two decisions deciding the same record are a `decision-decides-duplicate` finding, not a tie to resolve. Every other link is trace: it explains why a record exists and never decides a conflict.

A decision explains a choice. Goals, problems, features, journeys, actors, research, and prose carry no authority, so they never override another record. An exception of kind `departure` outranks the LiteSpec provision its `covers` names for the project that records it, until its `expiresOn` date. That exception is the departure; there is no separate type or folder.

When code and the specification disagree, the specification wins where Intent has spoken. Where the specification is silent, an agent builds nothing: something that seems missing becomes a question to the owner or a backlog item, never code.

An accepted gap becomes an exception of kind `gap`, and the findings its `covers` names report without failing until the `expiresOn` date. Module order states dependencies and grants no authority.

### Authoring Order

Records are authored top-down, one slice at a time: one feature from problem to scenario, then the next. Actors, roles, permissions, and the glossary grow beside Intent and Behavior. A branch may be incomplete; what merges is complete. Adding new intent to a running project follows the one sequence under New Intent in a Running Project on the [agent page](agent.md); this page states no second order.

### Reachability

The folder fixes where a record sits. Reachability fixes that it leads to software.

A goal reaches a journey or a quality requirement. A problem reaches a feature or requirement that addresses it. A feature or requirement reaches a use case, journey or quality scenario in Behavior. A constraint is answered by a requirement. Research is cited by a record that reaches Behavior. A decision decides a record that reaches Behavior.

A record that reaches no Behavior gets an `intent-unreached` finding: it is either unfinished work, which belongs on the backlog, or not software.

Intent that is written but not yet served is planned, not blocked. The planned-Intent findings are `intent-unreached`, `requirement-fulfillment-missing`, `step-use-case-missing`, `goal-mover-missing` and `constraint-unreached`. Each one reports on a local run and blocks only in the pull-request check, the run whose environment marks it continuous integration. An exception with `expiresOn` parks any of them for its subject or paths until it expires. The rule holds in every claim: a planned record is not a failure of the layers the project claims.

Intent coverage states, for each Intent requirement and each journey step, whether some Behavior record reaches it, and the share reached. A requirement is reached when a Behavior record fulfills it, and a step is reached when a use case, reaction or job serves it. The builder computes the share from the records alone, so the book shows Intent coverage in every claim, first on the Intent part of the book.

### Completion Stages

The builder computes one completion stage for every requirement, use case, journey, feature and goal, and writes it as `completion` with `stage`, `proof`, `heldBy` and `counts`. The stages run from least to most complete: `unfulfilled`, `specified`, `implemented` and `proven`. A parent is only as far as its weakest in-scope child, and `heldBy` names the children at that weakest stage, at most ten, with a `held by` count in `counts` naming the full number when the list is capped. `counts` holds the labelled done and of pairs behind the stage. `proof` reads `known` when the model holds a test run and `unknown` when it holds none; with no test run, an implemented record stays `implemented` with proof `unknown`, and proof unknown never reads as a failure. The book leads with completion.

A requirement is `unfulfilled` when no Behavior record fulfills it, `specified` when a fulfiller has no code, `implemented` when every fulfiller has code and not every fulfiller is proven, and `proven` when every fulfiller is verified and its measure passes. A use case is `unfulfilled` when no scenario specifies it, `specified` when a scenario specifies it and no code implements it, `implemented` when code implements it and not every scenario is proven, and `proven` when every scenario has a passing proof that is not stale. A journey takes the stage of its weakest step: an unserved step is `unfulfilled`, a served step with no code is `specified`, a step with code and no passing proof exercising it is `implemented`, and an exercised step is `proven`. A feature takes the stage of its weakest in-scope requirement and journey. Its in-scope requirements are the requirements on the feature page, and its in-scope journeys are the journeys citing those requirements. A goal takes the stage of its weakest in-scope feature, journey and requirement.

A requirement an exception parks stays out of a feature's stage and counts and names itself in the feature's `heldBy`, so deferred work stays visible without holding the stage back.

The builder derives the proof words from code, screens, tests, and test runs on every run:

| State | Of | When |
|:--|:--|:--|
| implemented | use case, rule, event, aggregate state | the stack builder finds code for it |
| verified | use case | every scenario has a passing proof through the public boundary that is not stale |
| tested | quality requirement, technical decision, external system, integration event | a passing test that asserts something links to it; tested never makes a use case verified |
| met | requirement | everything fulfilling it is verified, and its measure's check passes |
| stale | scenario proof | the scenario hash or the test-body hash differs from the hashes `.litespec/proof/proofs.json` recorded at the last passing run |
| flaky | scenario proof | the test both passed and failed within the runs read |
| reachable | aggregate state | a transition leads into it |
| exercised | journey step | a passing proof of a use case serving the step |

### Screen Captures

The project file names the screenshot galleries in `screenshots`: `galleries` holds one or more repository-relative folders, and `pattern` holds the file-name pattern with `{route}`, `{theme}` and `{width}`. The builder records each screen's captures as `captures` with `path`, `theme`, `width` and `capturedOn`, and its `captureState` as `captured`, `route-not-captured` or `no-route`. The route slug drops the leading slash of the screen address, joins its segments with a hyphen, reduces a bracketed, braced, colon or dollar parameter to its name, and names the root route `home`, so `/events/[id]` becomes `events-id` and the default pattern names its desktop light capture `events-id--light--1440.png`. `capturedOn` is the capture file's last-write date in UTC. A screen with an address and no matching capture is `route-not-captured`; a screen with no address and no route carrying it is `no-route`. A missing gallery folder captures nothing and fails nothing. A gallery inside a declared frontend folder serves only the screens of that frontend's entry point, so two frontends that both serve the root route never share a capture.

### Size and Stacks

Each project declares its size, its shape, and its stacks in `.litespec/project.yaml`. Each frontend declares one stack, and the backend declares its own. Extensions name their replacements. The [scope page](scope.md) states how a project selects its size, shape, and stacks.

### The Model Builder

The model builder reads record files, explanation files, documents, code, routes, tests, and test runs into one model. It holds the model in memory, runs the model checks, and renders generated regions. The [model command page](../tools/model.md) lists the subcommands and finding codes.

Generated regions sit inside fences that only the builder writes and prose checks skip, in explanation files and documents. A region that renders facts about its record finds the record through the explanation file name. Each project runs the builder in its own gate, so handwritten prose and computed views stay in one file without mixing.

A finding that a live exception covers is reported as excepted and exits zero. An exception is live when it is present and unexpired. A gap covers a finding when its `covers` lists the finding code; a departure covers the finding codes its provision's Checked by line names. The exception covers a finding only on its `subject` records or its `paths` files, and only while it is live. An exception names files no record describes in `paths`, as globs from the repository root, and records in `subject`; one of the two is required. An exception on a use case also covers the findings about the scenarios and results it holds. An exception past its `expiresOn` date is reported as `exception-expired`. Proof state lives in `.litespec/proof/proofs.json`, committed, holding for each scenario the hash of the scenario and of its test body at the last passing run; a proof is stale when either hash changes.

### Check Levels

Every check has a level.

| Level | Meaning |
|:--|:--|
| G | Reads records only. Always blocks. Every missing required link is G. |
| S | Reads code through a stack reader. Blocks where the reader is qualified, reports otherwise. |
| A | Advisory. Reports by default. `litespec model check --release` makes `use-case-unverified` block. |

Qualification is per reader, never global: a finding takes the qualification of the reader that produced its facts. A reader qualifies when it ran and its handshake matched. A finding no reader produced, a record finding, is qualified. A level S finding from an unqualified reader reports without blocking even when another reader in the same project is qualified. A missing or failing reader produces `reader-missing` (level S) for that frontend, and that frontend's level S findings report instead of blocking. A .NET reader that cannot read or parse a backend, test or screen file, a declared stack root with no folder, a `server` project with no backend reader, and a configured bill of materials that cannot be read or parsed, is not SPDX or CycloneDX, or yields no packages each report `reader-missing` (level S) naming the file or path, and the model is built from what could be read.

`litespec model check` prints the level beside each finding.

`litespec model check` runs only the findings of the layers the project claims. With only `intent` and `behavior` claimed, no stack reader runs, and the check runs the record findings and the Intent coverage the builder computes from records alone. Claiming `implementation` or `proof` adds the stack readers and the mark and proof findings. Claiming `implementation: litespec` adds the layout group, `code-placement-drift` and `mark-folder-drift`, the findings about LiteSpec's own code layout, with the stack and analyzer findings. The [model command page](../tools/model.md) lists every finding code with its level, its layer, its trace and its meaning.

`litespec model check` reports the planned-Intent findings locally and blocks on them in the pull-request check. The planned-Intent findings are `intent-unreached`, `requirement-fulfillment-missing`, `step-use-case-missing`, `goal-mover-missing` and `constraint-unreached`. A record that must stay unreached past a merge carries an exception with `expiresOn`, which then parks the finding for that record until it expires. The rule holds in every claim.

Two flags tune blocking without changing the levels. `litespec model check --release` makes the release-readiness findings block. `litespec model check --since <ref>` blocks only on findings about records and code changed since `<ref>` and reports the rest, so an adopting project clears new findings first.

### The Book

The book lives in the Proof record layer. The book shows each journey as record detail: its `finishedWhen`, the steps with their `statement` and `failure`; the requirements it cites, with their statements and their `except` and fallback inline; for each step, the serving use case `goal` in one line; and the Then clauses of its scenarios, as context.

Field tables render as tables. Scenarios group under their result. Field names never render literally in journey detail. The book renders each record's explanation file under its fields, anchored at the record. The book leads with completion.

The book renders the in-memory model only and judges nothing itself.

## Standards

### Write Every Record in Its Home

**ID:** `litespec/core/model#write-every-record-in-its-home`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A project MUST keep every record in the home its type names.

**Rationale:** One home per type tells a reader where to open a record and tells the builder where to read it. A record kept anywhere else is invisible to the checks that scan its home, so it looks sound while nothing validates it.

**Example:** A decision lives at `.litespec/intent/decisions/0001-receipt-threshold.yaml`.

**Checked by:** `litespec model check` reports `folder-invalid` for a folder outside the closed set of its layer and level, `module-undeclared` for a `modules` folder naming no declared module, and `record-placement-invalid` for a record outside its home.

### State the Common Fields on Every Record

**ID:** `litespec/core/model#state-the-common-fields-on-every-record`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every written record MUST carry its type, identifier, and title.

**Rationale:** Type, identifier, and title are the fields every join needs. Without them on every record, the builder cannot connect decisions to requirements, scenarios to results, or records to terms, and the model falls apart into loose pages. A record adds `terms` when it uses glossary terms and `records` when it holds entries.

**Checked by:** `litespec model check` reports `record-invalid` for a record missing a common field.

### Write Every Record in Its Record File

**ID:** `litespec/core/model#write-every-record-in-its-record-file`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every record page MUST live in one record file holding one YAML mapping with its entries in `records`.

**Rationale:** One file per page tells a reader where to open a record and tells the builder where to read it. The mapping holds exactly the fields the schema defines for its type, with entries in `records`, so every record has one shape and the checks read every record the same way. A record kept anywhere else is invisible to the checks that scan its home. Settled by [decision/0020-records-in-yaml](decision/0020-records-in-yaml) on the evidence in [research/record-format-evidence](research/record-format-evidence).

**Example:** Claimwick keeps `use-case/spending.submit-expense` in `.litespec/behavior/modules/spending/expense/submit-expense.yaml`, with its results and scenarios in `records`.

**Checked by:** `litespec model check` reports `record-invalid` for a record file that does not parse or whose top level names no record type or container page style.

### Explain Each Record Beside Its Record File

**ID:** `litespec/core/model#explain-each-record-beside-its-record-file`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An explanation file MUST sit beside its record file with the same name and hold plain Markdown with no frontmatter.

**Rationale:** The file name is the link: no field joins the two files, so a rename moves both and nothing else. Plain Markdown with no frontmatter keeps the explanation readable in any reader and tells every check to read fields from the record file only. The file opens with the record title and uses H2 sections, so each explanation has one shape.

**Example:** `submit-expense.yaml` explains its Example and Notes in `submit-expense.md` beside it.

**Checked by:** `litespec model check` reports `explanation-orphaned` for a Markdown file under a record root with no frontmatter and no record file beside it.

### Split Record Fields from Explanation

**ID:** `litespec/core/model#split-record-fields-from-explanation`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A fact a check reads MUST live as a field in the record file rather than in the explanation file.

**Rationale:** Fields are checked; explanation prose is not. A fact a check reads that sits in prose is invisible to the check. The page explanation cites entries by identifier, so entries need no file of their own.

**Example:** The submit-expense summary lives in the `summary` field of `submit-expense.yaml`, while why the flow reads well lives under Notes in `submit-expense.md`.

**Reviewed for:** Which explanation files state a fact a check reads? Look at each explanation file beside its record file.

### Require an Explanation File for Article Types

**ID:** `litespec/core/model#require-an-explanation-file-for-article-types`

**Strength:** required

**Applies to:** artifact: record:research, record:decision

**Statement:** A research or decision page MUST carry its explanation file with the required H2 sections.

**Rationale:** An article is read as prose: the Answer of a research and the Context of a decision are what the reader comes for. The required sections keep each article complete: Answer for research, and Context, Options Considered, Decision, and What Would Reopen It for decision.

**Example:** `research/approval-time-benchmarks` carries its Answer in `approval-time-benchmarks.md`.

**Checked by:** `litespec model check` reports `explanation-missing` for an article type with no explanation file or with a required H2 section missing.

### Write Every Record File in Canonical Form

**ID:** `litespec/core/model#write-every-record-file-in-canonical-form`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every record file MUST match the canonical text the record writer produces for it.

**Rationale:** One layout for every record keeps diffs small and readers on content: a change shows the moved fact, never reflowed prose or reordered keys. The tool owns the layout, so `litespec model format` rewrites every record file into canonical form and the check reports any file that differs.

**Example:** A use-case file opens with `type`, `id`, and `title`, holds its own fields in schema order, and closes with `records`.

**Checked by:** `litespec model check` reports `format-drift` for a record file whose text differs from its canonical form.

### Give Every Record One Parent

**ID:** `litespec/core/model#give-every-record-one-parent`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every record except the product MUST have exactly one parent from the Placement Table on this page.

**Rationale:** A record with no parent has no place in the book, and a record with two parents appears twice. One parent per record puts every record exactly once: the container is the parent, so an entry sits under the record file that holds it, a scenario sits under its result, and a record file sits under its folder node. A requirement sits on its feature page, and types at product level sit under the product with no field written. Settled by [decision/0002-parent-for-every-record](../intent/decisions/0002-parent-for-every-record.md) on the evidence in [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence.md), [research/intent-hierarchy-evidence](../intent/research/intent-hierarchy-evidence.md), [research/book-outline-evidence](../intent/research/book-outline-evidence.md), and [research/established-traceability-practice](../intent/research/established-traceability-practice.md).

**Example:** In the Claimwick product, `requirement/approval-reminders.receipt-over-threshold` sits on the `feature/approval-reminders` page under `intent/modules/spending/features/`; `use-case/spending.submit-expense` sits beside its aggregate; and `scenario/submit-expense.submitting-with-a-receipt-succeeds` sits under `result/submit-expense.submitted`.

**Checked by:** `litespec model check` reports `record-parent-missing` (level G) for a record whose required parent is missing.

### Move Each Identifier with Its Module

**ID:** `litespec/core/model#move-each-identifier-with-its-module`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A record that moves to another module MUST change the module segment of its identifier and every citation in the same change.

**Rationale:** The folder wins: an identifier whose home segment disagrees with its folder names a home the record left. Only identifiers whose first segment names a module change with a module move. A rule identifier keeps its home segment, because the segment names the aggregate or module the rule is about rather than the folder that holds it. A move between product and module level changes no identifier, so no citation changes.

**Example:** Claimwick moves `use-case/spending.submit-expense` from the spending module to the finance module, so the use case becomes `use-case/finance.submit-expense` and every citation changes in the same change.

**Checked by:** `litespec model check` reports `identifier-folder-drift` for an identifier whose home segment disagrees with its folder.

### Keep Each Name Unique in the Product

**ID:** `litespec/core/model#keep-each-name-unique-in-the-product`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each name segment MUST stay unique in the product.

**Rationale:** Short identifiers resolve with no lookup table only when each name means one record. Folders do not narrow uniqueness: two features with one name in two modules still collide everywhere the identifier is read.

**Example:** Claimwick names one feature `approval-reminders`; a second feature with that name in another module is reported even though the folders differ.

**Checked by:** `litespec model check` reports `name-duplicate` for a name segment that occurs twice in the product.

### Take Decision Numbers from One Sequence

**ID:** `litespec/core/model#take-decision-numbers-from-one-sequence`

**Strength:** required

**Applies to:** artifact: record:decision

**Statement:** A decision number MUST come from one sequence across every home.

**Rationale:** Product and technical decisions share one identifier space, so a number identifies one decision wherever it sits. `litespec new decision` takes the next free number, and no two decisions share it.

**Example:** `decision/0005-render-receipts-as-pdf` is the fifth decision, whether it sits under Intent or under Implementation.

**Checked by:** `litespec model check` reports `decision-number-duplicate` for two decisions sharing one number.

### Write Links on the Record That Points

**ID:** `litespec/core/model#write-links-on-the-record-that-points`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A link MUST live as a field on the record it starts from, with the Links table stating how it reads in both directions.

**Rationale:** One writing site keeps a link and its source in one edit: changing the source updates the link in the same change. Links written on the target instead would scatter each relationship across every page it touches. Settled by [decision/0002-parent-for-every-record](../intent/decisions/0002-parent-for-every-record.md) on the evidence in [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence.md), [research/intent-hierarchy-evidence](../intent/research/intent-hierarchy-evidence.md), [research/book-outline-evidence](../intent/research/book-outline-evidence.md), and [research/established-traceability-practice](../intent/research/established-traceability-practice.md).

**Checked by:** `litespec model check` reports `reference-unknown` for a link whose target no record carries.

### Connect Every Behavior Record to Intent

**ID:** `litespec/core/model#connect-every-behavior-record-to-intent`

**Strength:** required

**Applies to:** artifact: record:use-case, record:reaction, record:job, record:notification, record:setting

**Statement:** A use case, reaction, job, notification, or setting MUST carry serves, fulfills, or both.

**Rationale:** A use case, reaction, job, notification, or setting with no served journey step and no fulfilled requirement answers to nothing above it: no promise explains why it exists. Pointing up with serves or fulfills ties every behavior to the Intent that asks for it. `record-link-missing` fails a use case, reaction, job, notification, or setting carrying neither. A use case is background-only when a reaction or job runs it, so `use-case-journey-missing` skips it.

**Checked by:** `litespec model check` reports `record-link-missing` for a use case, reaction, job, notification, or setting with neither `serves` nor `fulfills`, and `use-case-journey-missing` (level G) at large size for a use case that no reaction, job or other use case runs and that serves no journey step.

### Reach Behavior from Every Intent Record

**ID:** `litespec/core/model#reach-behavior-from-every-intent-record`

**Strength:** required

**Applies to:** artifact: record:goal, record:problem, record:feature, record:requirement, record:constraint, record:research, record:decision

**Statement:** Every goal, problem, feature, requirement, constraint, research, and decision MUST reach Behavior through the links the Reachability section states.

**Rationale:** The folder fixes where a record sits, and reachability fixes that it leads to software. A record that reaches no Behavior is either unfinished work, which belongs on the backlog, or not software. A branch may be incomplete, so a local run reports unreached records while the pull-request check blocks on them. The same planned-Intent rule covers `requirement-fulfillment-missing`, `step-use-case-missing`, `goal-mover-missing` and `constraint-unreached`: each reports locally and blocks only in the pull-request check. A record that must stay unreached past a merge carries an exception with `expiresOn`, which parks reachability for that record until it expires.

**Example:** In Claimwick, `problem/expenses-wait-weeks` reaches Behavior through `feature/approval-reminders` and its requirements; a problem no feature or requirement addresses is reported.

**Checked by:** `litespec model check` reports `intent-unreached` for a record that reaches no Behavior; a local run reports it and the pull-request check blocks on it. The planned-Intent findings `requirement-fulfillment-missing`, `step-use-case-missing`, `goal-mover-missing` and `constraint-unreached` take the same outcome.

### Serve Every Journey Step

**ID:** `litespec/core/model#serve-every-journey-step`

**Strength:** required

**Applies to:** artifact: record:journey

**Statement:** Every journey step MUST have a serving use case, reaction, or job.

**Rationale:** A step no use case, reaction, or job serves promises a moment the product never builds. Use cases, reactions, and jobs point up with serves, so each step id collects the behaviors that carry the walker through it. `step-use-case-missing` fails a step with no serving use case, reaction, or job. The pair sits side by side: `step-use-case-missing` names a step no use case, reaction, or job serves, and `step-screen-missing` names a step a browser or device use case serves that no screen serves.

**Checked by:** `litespec model check` reports `step-use-case-missing` for a journey step with no serving use case, reaction, or job, beside `step-screen-missing` for a step a browser or device use case serves that no screen serves.

### Connect Across Modules Only Through Declared Dependencies

**ID:** `litespec/core/model#connect-across-modules-only-through-declared-dependencies`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Within one layer, a record under a module MUST be linked from another module only along a declared module dependency or through an integration event.

**Rationale:** A record linked from another module with no declared tie is not local: it belongs at product level. The declared dependency or the integration event names the tie, so the seam where a monolith would split stays visible. Links from one layer down to the next may cross modules, because that is how one capability spans modules.

**Example:** A spending rule cited from the finance module with no `dependsOn` tie and no integration event between them is reported; the rule moves to product level or the modules declare the tie.

**Checked by:** `litespec model check` reports `link-locality-invalid` for a record under a module that is linked from another module with no declared dependency or integration event.

### Keep Module Dependencies Free of Cycles

**ID:** `litespec/core/model#keep-module-dependencies-free-of-cycles`

**Strength:** required

**Applies to:** artifact: record:module

**Statement:** Module `dependsOn` links MUST NOT form a cycle.

**Rationale:** A dependency cycle says each module builds on the other, so neither has a seam to split along. The module order stays a directed graph with no cycle, like the effect edges.

**Example:** Spending depends on finance and finance depends on spending, so the cycle is reported.

**Checked by:** `litespec model check` reports `module-dependency-invalid` for a module dependency cycle.

### Publish Each Integration Event from One Module

**ID:** `litespec/core/model#publish-each-integration-event-from-one-module`

**Strength:** required

**Applies to:** artifact: record:integration-event

**Statement:** An integration event MUST have exactly one publishing module.

**Rationale:** An event with two publishers has two writers of one fact: readers cannot tell which module states it. One publishing module gives the fact one home, and every other module reads it through the event.

**Example:** `integration-event/expense-submitted` is published from the spending module; a second publisher in the finance module is reported.

**Checked by:** `litespec model check` reports `event-publisher-invalid` for an integration event with a second publishing module.

### Hold Module Rules in Rule Sets

**ID:** `litespec/core/model#hold-module-rules-in-rule-sets`

**Strength:** required

**Applies to:** artifact: record:rule

**Statement:** A module-level rule MUST live in a rule-set file under `behavior/modules/<m>/rules/`, never in `module.yaml`.

**Rationale:** The module record declares the module; rules are behavior and live with Behavior. A rule written into `module.yaml` hides behavior in a declaration no rule check scans.

**Example:** A spending rule lives in a rule-set file under `behavior/modules/spending/rules/`; the same rule written into `module.yaml` is reported.

**Checked by:** `litespec model check` reports `record-placement-invalid` for a rule in `module.yaml`.

### Follow the Authority Order

**ID:** `litespec/core/model#follow-the-authority-order`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A conflict MUST resolve toward the higher authority.

**Rationale:** Intent records outrank Behavior records: constraint over product, product over requirement, requirement over rule, rule over use case, use case over result, and result over scenario. The order settles a conflict without a meeting: a use case asking for behavior its rule forbids loses to the rule. Where Intent has spoken, the specification wins over code; where the specification is silent, an agent builds nothing, as the Authority section states. Goals, problems, features, journeys, actors, research, and prose carry no authority, so none outranks the ladder.

**Reviewed for:** Conflict decisions under `.litespec/intent/decisions/` name the higher record that won, in the order this page states.

### Record Accepted Gaps as Exceptions

**ID:** `litespec/core/model#record-accepted-gaps-as-exceptions`

**Strength:** required

**Applies to:** artifact: record:exception

**Statement:** An accepted gap MUST live as an exception of kind `gap` with an expiry date.

**Rationale:** Missing work with no date is invisible: nothing reports it. An exception of kind `gap` with an `expiresOn` date turns the same missing work into a visible promise: the findings its `covers` names report until the date, and `exception-expired` reports the exception after it. Settled by [decision/0014-one-exception-record](decision/0014-one-exception-record) on the evidence in [research/lower-layers-practice](research/lower-layers-practice).

**Example:** The running Claimwick example accepts that exporting expenses has no handler yet, so the gap lives as a departure with an expiry date in `.litespec/governance/departures/export-expenses-unimplemented.yaml`.

```yaml
type: exception
id: exception/export-expenses-unimplemented
title: Export expenses unimplemented
kind: gap
covers:
  - use-case-unimplemented
subject:
  - use-case/spending.export-expenses
reason: Export waits for the finance file format.
expiresOn: 2027-06-01
```

The `use-case-unimplemented` finding reports without failing until 2027-06-01, which satisfies this provision.

**Checked by:** `litespec model check` reports `departure-expired` (level G) for a departure past its `expiresOn` date, and `departure-invalid` (level G) for a departure naming a finding code no check uses or a code the model needs in order to load.

### Name What Every Decision Decides

**ID:** `litespec/core/model#name-what-every-decision-decides`

**Strength:** required

**Applies to:** artifact: record:decision

**Statement:** Every product decision MUST name at least one record it settles in `decides`.

**Rationale:** A decision that names nothing governs nothing. Naming at least one settled record gives the decision something to govern. A technical decision governs the code that follows it instead.

**Checked by:** `litespec model check` reports `decision-decides-missing` for a product decision with an empty `decides` field, and `decision-decides-duplicate` for two decisions deciding one record.

### Record Each Technical Choice as a Decision

**ID:** `litespec/core/model#record-each-technical-choice-as-a-decision`

**Strength:** required

**Applies to:** artifact: record:decision

**Statement:** A technical choice that code follows and no LiteSpec provision makes MUST be a decision under `.litespec/implementation/decisions/` that at least one code element follows.

**Rationale:** Why a pattern, a library, or a configuration value was chosen is the first thing a maintainer asks before changing it. Written in a comment, the answer drifts and no tool finds it. Written as a decision, it carries its options, what speaks for and against each, and what would reopen it, and every code element shaped by it names it in a `follows` mark. A choice a LiteSpec provision already makes needs no decision, because the provision is the record. Technical decisions sit under Implementation because they name technologies and code, which Intent never holds, and they share the numbered sequence of product decisions, so all decisions share one identifier space.

**Example:** Claimwick renders each receipt as a PDF file. `decision/0005-render-receipts-as-pdf` weighs a PDF library against an HTML-to-PDF browser, chooses the PDF library, and names what reopens the choice: receipts no longer needing a downloadable file. The receipt PDF renderer carries `/// <follows>decision/0005-render-receipts-as-pdf</follows>`.

**Checked by:** `litespec model check` reports `decision-unimplemented` (level S) for a technical decision that no code element follows. The check runs in the CLI only, with no analyzer rule: whether any code follows a decision is known only across every project, and one compilation sees one project.

### Keep Rules and Targets Out of Technical Decisions

**ID:** `litespec/core/model#keep-rules-and-targets-out-of-technical-decisions`

**Strength:** required

**Applies to:** artifact: record:decision

**Statement:** A technical decision MUST leave each rule the product obeys whatever its design to a constraint and each measurable target to a quality requirement, and name the ones it serves in `basedOn`.

**Rationale:** A technical decision chooses one way among alternatives, and a better alternative may replace it tomorrow. A rule such as keeping personal data in the European Union, or a target such as answering checkout within two seconds, does not change when the library does: overriding it needs a business, legal, or organizational change. Written inside a decision, the rule disappears the day the decision is replaced, and nobody holds it as the promise it is. Kept in Intent, the rule and the target stand as the promises they are, the decision points up to them, and the chain runs unbroken from code through the decision to the constraint or requirement and the goal above it. A decision with no basis is advice, level A, while a product decision that decides nothing blocks at level G: a technical choice among equal options may serve no rule or target yet and still govern the code that follows it, but a product decision that settles no record governs nothing.

**Example:** Claimwick keeps each receipt for the tax file because `constraint/tax-receipt-retention` requires it, and answers submissions quickly because `requirement/expense-submission.month-end-load` sets the target. `decision/0005-render-receipts-as-pdf` names the constraint in `basedOn`, and `decision/0004-serve-receipts-behind-one-proxy` names the requirement in `basedOn`; neither the seven years nor the two seconds is written in a decision.

**Checked by:** `litespec model check` reports `decision-basis-missing` (level A) for a technical decision whose `basedOn` names nothing, and counts a quality requirement as fulfilled when a technical decision names it in `basedOn`.

### Resolve Every Page Link

**ID:** `litespec/core/model#resolve-every-page-link`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every Markdown link outside code in a record field, an explanation file, or a document MUST resolve to an existing file or to a written record identifier.

**Rationale:** A link that points at a moved file or an unknown identifier reads as navigation and leads nowhere. A link whose target is a record identifier, with an optional `#<step-id>`, cites that record: link checks resolve it against the model's identifiers, citation checks count it, and the book links it to the record. Prose cites a record by its identifier, never by its file path.

**Example:** The Claimwick receipt rule is cited as `[the receipt rule](rule/expense.approver-is-not-submitter)`.

**Checked by:** `litespec model check` reports `link-unknown` for a link that resolves to no file and no written record identifier.

### Declare Size and Stacks

**ID:** `litespec/core/model#declare-size-and-stacks`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A project MUST declare its size and stacks.

**Rationale:** Size decides which record types and test projects the checks require, while stacks decide which technology pages and checks apply. Without the declaration, the builder cannot tell a missing record from one the project never needed.

**Example:** The running Claimwick example declares large with a server shape and two stacks.

**Checked by:** `litespec model check` reports `size-type-missing` and `record-invalid` for a project missing its declaration or a required type.

### Give Every Use Case Its Results

**ID:** `litespec/core/model#give-every-use-case-its-results`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A use case MUST end in a success result and name its refusals.

**Rationale:** Callers branch on named endings: success or a specific refusal with its code. Refusals stand in precedence order, so the first match wins. A refusal that stores its attempt carries recorded, and a success with recorded outcomes inside it carries codes. Generic refusals are derived, never written, so they need no entry. A use case with no named success leaves callers guessing what good looks like, and scenarios prove one ending each, so unnamed endings stay unproven.

**Example:** A submit use case names `submitted` and `receipt-missing`.

The running Claimwick example carries this in `use-case/spending.submit-expense`, which names `result/submit-expense.submitted` and `result/submit-expense.receipt-missing` with `scenario/submit-expense.submitting-with-a-receipt-succeeds`:

```yaml
records:
  - type: result
    id: result/submit-expense.submitted
    title: Submitted
    outcome: success
    meaning: The expense is submitted.
  - type: result
    id: result/submit-expense.receipt-missing
    title: Receipt missing
    outcome: refusal
    code: spending.receipt-missing
    meaning: The expense is above the receipt threshold and carries no receipt.
  - type: scenario
    id: scenario/submit-expense.submitting-with-a-receipt-succeeds
    title: Submitting with a receipt succeeds
    given: An employee paid for a train ticket and kept the receipt.
    when: The employee submits the expense with the receipt.
    then: The expense is submitted.
    result: result/submit-expense.submitted
```

Success names the submitted ending, the refusal carries the code `spending.receipt-missing`, and the scenario pairs one given and when with the success result, which satisfies this provision.

**Checked by:** `litespec model check` reports `use-case-success-missing` and `scenario-result-unknown` for a use case missing its success or a scenario naming no result.

### Author One Slice at a Time

**ID:** `litespec/core/model#author-one-slice-at-a-time`

**Strength:** required

**Applies to:** artifact: record:feature

**Statement:** Authoring MUST complete one feature from problem to scenario before starting the next.

**Rationale:** A feature worked from problem to scenario stays complete at every level: each record lands with the Intent above it already written. Parallel slices leave Behavior records resting on Intent nobody wrote yet.

**Reviewed for:** Behavior records show Intent above them through their `fulfills` and `decides` links.

### Compute Proof Statuses in the Builder

**ID:** `litespec/core/model#compute-proof-statuses-in-the-builder`

**Strength:** required

**Applies to:** artifact: any; layer: implementation; trace: yes

**Statement:** Implemented, verified, met, tested, exercised, stale, flaky, and reachable MUST come from the builder, never from handwriting.

**Rationale:** Handwritten state drifts: a record marked implemented stays marked after its handler is deleted. Computed state cannot drift, because the builder re-derives it from handlers, routes, scenarios, and test runs on every run. A use case reads implemented when a handler, reaction, job, or endpoint marked with it carries it out, verified when every scenario has a passing proof through the public boundary that is not stale, and stale when the scenario hash or the test-body hash differs from `.litespec/proof/proofs.json`. A requirement reads met when everything fulfilling it is verified and its measure's check passes. A record reads tested when a passing test that asserts something links to it. A journey step reads exercised when a passing proof of a use case serves it. A proof reads flaky when its test both passed and failed within the runs read. Nothing above Implementation is computed from code: derivation from code into any other record is forbidden, and code-computed facts live in the book and the model output. Settled by [decision/0017-proof-that-holds](decision/0017-proof-that-holds) on the evidence in [research/lower-layers-practice](research/lower-layers-practice).

**Example:** A use case reads implemented when its handler exists.

**Checked by:** `litespec model check` reports `use-case-unimplemented`, `proves-unreached`, and `refusal-unimplemented` for a use case whose handler, proof, or refusal code is missing, and `proof-invalid` (level A) for a test that both passed and failed within the runs read.

### Report Unverified Use Cases

**ID:** `litespec/core/model#report-unverified-use-cases`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A use case whose scenarios are not all proved MUST be reported as `use-case-unverified`.

**Rationale:** Nothing blocks on an unverified scenario day to day, so the release gate would depend on someone reading the book. Reporting the use case keeps daily work loose, and `litespec model check --release` makes the finding block, so the release stays honest without slowing ordinary work.

**Example:** Claimwick adds `use-case/spending.export-expenses` with two scenarios and one passing proof: `litespec model check` reports `use-case-unverified` for it and exits zero, and `litespec model check --release` blocks on it.

**Checked by:** `litespec model check` reports `use-case-unverified` (level A) for a use case whose scenarios are not all proved, and `litespec model check --release` blocks on it.

### Compute Completion in the Builder

**ID:** `litespec/core/model#compute-completion-in-the-builder`

**Strength:** required

**Applies to:** artifact: record:use-case, record:requirement, record:journey, record:feature, record:goal; layer: implementation; trace: yes

**Statement:** The builder MUST compute one completion stage for every requirement, use case, journey, feature and goal, and write it as `completion` with `stage`, `proof`, `heldBy` and `counts`, with a parent only as far as its weakest in-scope child and `heldBy` naming at most ten children at that stage.

**Rationale:** Completion is a fact the builder can count from fulfilment, code and proof, so no record carries a shorthand saying how far it is. One ladder reads the same everywhere: `unfulfilled` when no Behavior record fulfills a record, `specified` when it is fulfilled and no code is found, `implemented` when code is found for every in-scope child, and `proven` when every in-scope child is verified or met with fresh proof. When the model holds no test run, an implemented record stays `implemented` with proof `unknown`, so proof unknown never reads as a failure. A requirement an exception parks stays out of the stage and the counts and names itself in `heldBy`, so deferred work stays visible.

**Example:** In Claimwick, `feature/expense-submission` shows `implemented` with proof `unknown` before any test run, held by the one requirement with no fulfiller; after a passing run proves every fulfiller, the same feature shows `proven` with no held-by entries.

**Reviewed for:** Which features read below `proven`, and which children hold each one back? Look at `completion` on each feature in the model.

### Record Each Screen's Captures from the Gallery

**ID:** `litespec/core/model#record-each-screens-captures-from-the-gallery`

**Strength:** required

**Applies to:** artifact: record:screen

**Statement:** The builder MUST record each screen's captures from the gallery folders and file-name pattern the project file names, as `captures` with `path`, `theme`, `width` and `capturedOn`, and its `captureState` as `captured`, `route-not-captured` or `no-route`.

**Rationale:** A reader judging a screen needs to see it, and a capture named by its route, theme and width is the copy the built screen makes. The route slug names the file the same way everywhere: the leading slash dropped, segments joined with a hyphen, a parameter reduced to its name, and the root route named `home`. A missing gallery folder captures nothing and fails nothing, so a project with no photography yet still builds its model.

**Example:** The Claimwick web gallery holds `expenses--light--1440.png`, so `screen/expenses.list` at `/expenses` shows that capture at theme `light` and width 1440 with `captureState` `captured`; a screen with no address shows `no-route`.

**Reviewed for:** Which screens with a route have no capture? Look at `captureState` `route-not-captured` on screen records in the model.

### Mark Every Check with Its Level

**ID:** `litespec/core/model#mark-every-check-with-its-level`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every check MUST carry its level and block or report as that level states.

**Rationale:** The level tells a reader what a finding means: G reads records only and always blocks, S reads code through a stack reader and blocks only where the reader is qualified, and A reports, blocking only for the release-readiness findings under `--release`. `litespec model check` prints the level beside each finding, so a reader tells a blocking defect from advice at a glance. A missing required link is level G.

**Checked by:** `litespec model check` reports `reader-missing` (level S) for a frontend whose reader is missing or failing, and prints the level beside each finding.

### Declare One Mark Style

**ID:** `litespec/core/model#declare-one-mark-style`

**Strength:** required

**Applies to:** artifact: any; layer: implementation; trace: yes

**Statement:** A project MUST write each C# mark in the one style that `marks` in `.litespec/project.yaml` declares, each TypeScript mark as a typed constant, and each operations mark as a comment.

**Rationale:** Two styles in one language are two ways to do one job: a reader learns both, a check covers both, and a search for a record finds half its code. The C# style is a project choice made once, so every tool reads it from the project file and refuses the other style within C#. TypeScript and operations files have one form each, so a typed constant beside a C# documentation tag is two languages, not two styles. A code fix converts a mark to the form its language takes.

**Example:** The Claimwick product declares `"marks": "xml"`, so its submit-expense handler carries a documentation tag naming `use-case/spending.submit-expense`, and an attribute naming the same use case is reported. Its use-case module carries the same identifier as a typed constant, and its deployment file carries its `follows` mark as a comment.

**Checked by:** `litespec model check` reports `mark-style-invalid` (level S) for a mark written in a form its language does not take.

### Match Each Backend Mark to Its Folder

**ID:** `litespec/core/model#match-each-backend-mark-to-its-folder`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler, endpoint, reaction, job, aggregate; layer: implementation

**Statement:** A backend handler, endpoint, event handler, job host or aggregate root MUST carry a mark naming a use case, reaction, job, or aggregate in the module and folder it sits in.

**Rationale:** Copying a handler and forgetting its mark is the most common agent mistake, and the folder already says what the mark should be. A handler that sits in one use-case folder while naming another sends readers and checks to the wrong record. An aggregate method and a technical class carry the mark of the use case that calls them, which may sit in another module, so their folder says nothing about their mark. The frontend mirror already compares each mark with its module; the backend check applies the same comparison to handlers, endpoints, and aggregates.

**Example:** A handler in `Booking/Holds/HoldRoom/` marked `use-case/booking.release-hold` is reported, because the folder holds `use-case/booking.hold-room`.

**Checked by:** `litespec model check` reports `mark-folder-drift` (level S) for a handler, endpoint, event handler, job host or aggregate root whose mark names a record outside its module and folder, and `code-placement-drift` (level S) for a code element, test, or folder outside its record's qualified path.

### Name the Lowest Record Each Test Proves

**ID:** `litespec/core/model#name-the-lowest-record-each-test-proves`

**Strength:** required

**Applies to:** artifact: test

**Statement:** Every test MUST name in a `proves` mark the lowest record it verifies, from the kinds the marks table on this page lists for a test.

**Rationale:** A test outside the record tree is either proof of something the specification forgot to state or a test that verifies nothing anyone promised. Naming the lowest record it verifies places each test: a scenario or rule for behavior, a quality requirement for how well the product behaves, a technical decision for a technical choice, an external system or integration event for an agreement with the outside, and a provision for a structural rule the code must keep. A test that verifies a choice no record states shows a missing record, which is written at the level where the choice lives, as safety-critical standards resolve derived requirements; a test that verifies nothing a record should state is deleted. Proof stays layered: only scenario and rule proofs verify a use case, and a passing test that asserts something links to a quality requirement, a technical decision, an external system, an integration event, or a provision marks that record tested, so "the adapter is right" never reads as "the promise is kept". A background use case is verified through the passing proofs of the use cases it serves. A browser success proof asserts the result sentence or the scenario Then text.

**Example:** In Claimwick, the submit-expense acceptance test proves `scenario/submit-expense.submitting-with-a-receipt-succeeds`; a load test of month-end submissions proves `requirement/expense-submission.month-end-load`; a receipt-rendering test proves `decision/0005-render-receipts-as-pdf`; and the layering test that Domain references no persistence package proves `litespec/backend/architecture#point-dependencies-inward`.

**Checked by:** `litespec model check` reports `mark-missing` and `mark-unknown` (level S) for a test that names no record or a record of a kind a test does not take.

### Implement Each Mapped Behavior Record Behind a Mark

**ID:** `litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark`

**Strength:** required

**Applies to:** artifact: record:aggregate, record:reaction, record:job, record:external-system, record:use-case; layer: implementation; trace: yes

**Statement:** Every entity aggregate, every reaction, job, and external system, and every use case reached through a `browser` or `api` entry point, MUST be named by a mark on the code element the marks table gives it.

**Rationale:** A mark points up, so a check reading marks alone finds code nobody recorded but misses a record nobody built. Checking each record that the marks table maps to code from the top closes the loop in the other direction: an entity aggregate with no root, a reaction with no event handler, a job with no host, an external system with no port, and a use case people reach with no endpoint. A store has no root, so it needs no mark. A use case with no screen calling it is reported as advice, because whether a browser reaches it depends on the deployment.

**Example:** `reaction/spending.send-approver-reminder-on-expense-submitted` has no event handler carrying its mark, so the check reports `reaction-unimplemented` and blocks.

**Checked by:** `litespec model check` reports `aggregate-unimplemented`, `reaction-unimplemented`, `job-unimplemented`, `external-system-unimplemented`, and `use-case-endpoint-missing` (level S) for a record no mark names, where `aggregate-unimplemented` fires only for an entity aggregate, and `use-case-unreached` (level A) for a browser use case no screen calls.

### Record a Project-Wide Lowering as an Exception

**ID:** `litespec/core/model#record-a-project-wide-lowering-as-an-exception`

**Strength:** required

**Applies to:** artifact: record:exception

**Statement:** A project MUST record an exception for every analyzer rule it lowers or switches off in an `.editorconfig` that applies beyond one file.

**Rationale:** A project-wide lowering in `.editorconfig` is a departure from the standard rather than a setting. An exception of kind `departure` names the provision, the reason and the expiry date, so the lowering is dated and ends instead of staying silent.

**Example:** A project that lowers LS5001 to a suggestion in its root `.editorconfig` records an exception of kind `departure` citing `litespec/core/system#point-code-and-tests-up-to-records`.

**Checked by:** `litespec model check` reports `analyzer-lowering-unrecorded` (level S) for a project-wide lowering in `.editorconfig`, `.globalconfig`, `<NoWarn>`, `#pragma warning disable` or `[SuppressMessage]` that no live exception covers.

### Organize the Book by the Record Layers

**ID:** `litespec/core/model#organize-the-book-by-the-record-layers`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The book MUST outline a project by the four record layers, with the governance records beside them, and place each record under the record its folder and links name.

**Rationale:** The book is how people read the model, so it shows the model's own hierarchy. A reader who learns Intent, Behavior, Implementation and Proof in the book reads the same structure in the docs folder, in the model output and in every other LiteSpec project. An outline of the book's own invention is a second hierarchy to learn, and it drifts from the one the records keep. Settled by [decision/0002-parent-for-every-record](../intent/decisions/0002-parent-for-every-record.md) on the evidence in [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence.md), [research/intent-hierarchy-evidence](../intent/research/intent-hierarchy-evidence.md), [research/book-outline-evidence](../intent/research/book-outline-evidence.md), and [research/established-traceability-practice](../intent/research/established-traceability-practice.md). Settled by [decision/0008-book-follows-record-layers](../intent/decisions/0008-book-follows-record-layers.md) on the evidence in [research/book-outline-evidence](../intent/research/book-outline-evidence.md), [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence.md), and [research/check-coverage-evidence](../intent/research/check-coverage-evidence.md).

**Example:** In the Claimwick product, `use-case/spending.submit-expense` sits under Behavior, its module and its aggregate; its results under it; each scenario under the result it shows; and the handler that carries its mark under Implementation.

**Reviewed for:** Which book headings or groupings are not a record layer, a record type, or a record the identifier names? Look at the book's outline beside this page's record type table.

### Name Book Labels with LiteSpec Terms

**ID:** `litespec/core/model#name-book-labels-with-litespec-terms`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every label in the book MUST be a record type, a field name, a computed state, a glossary term, or the reads-as phrase the Links table states for a connection.

**Rationale:** LiteSpec already names every concept once, for people and agents alike. A friendlier synonym in the book, such as promise for requirement, is a second word for one concept: the reader learns it, then meets the real word in the records and the checks, and has to translate. Settled by [decision/0008-book-follows-record-layers](../intent/decisions/0008-book-follows-record-layers.md) on the evidence in [research/book-outline-evidence](../intent/research/book-outline-evidence.md), [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence.md), and [research/check-coverage-evidence](../intent/research/check-coverage-evidence.md).

**Example:** A use case page heads its results "Results" and its scenarios "Scenarios", never "Endings" and "Examples".

**Reviewed for:** Which book labels are neither a record type, a field name, a computed state, a glossary term, nor the reads-as phrase the Links table states for a connection? Look at the book beside the glossary and the Links table.

### Explain the Book from LiteSpec Definitions

**ID:** `litespec/core/model#explain-the-book-from-litespec-definitions`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every explanation the book shows MUST come from the glossary definition of the term or from the question its record type answers on this page.

**Rationale:** The book explains itself so nobody needs a learning curve, and the explanation stays true only when it has one source. An explanation written for the book drifts from the definition the records follow. Settled by [decision/0008-book-follows-record-layers](../intent/decisions/0008-book-follows-record-layers.md) on the evidence in [research/book-outline-evidence](../intent/research/book-outline-evidence.md), [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence.md), and [research/check-coverage-evidence](../intent/research/check-coverage-evidence.md).

**Example:** The book explains an exception with the glossary definition of Exception.

**Reviewed for:** Which explanations in the book are not a glossary definition or a record type question? Look at the book's explanations beside the glossary and the record type table.

### Show Only Computed Judgements in the Book

**ID:** `litespec/core/model#show-only-computed-judgements-in-the-book`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every status, count, order and finding the book shows MUST be one the builder or a check computes.

**Rationale:** The book is a view of the model, not a second authority. A judgement only the book makes, such as its own idea of what to do next, cannot be checked, cannot fail a build, and cannot be found by an agent reading the model output. Settled by [decision/0008-book-follows-record-layers](../intent/decisions/0008-book-follows-record-layers.md) on the evidence in [research/book-outline-evidence](../intent/research/book-outline-evidence.md), [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence.md), and [research/check-coverage-evidence](../intent/research/check-coverage-evidence.md).

**Example:** The book orders features by the completion the builder computes.

**Reviewed for:** Which statuses, counts, orders or findings in the book does no builder output or check produce? Look at the book beside the model output.

### Add a Missing Fact to LiteSpec Before the Book Shows It

**ID:** `litespec/core/model#add-a-missing-fact-to-litespec-before-the-book-shows-it`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A fact the book needs that LiteSpec does not define MUST become a record field, a computed status, or a check in LiteSpec before the book shows it.

**Rationale:** Rendering the book is a probe: what it cannot show is a gap in the specification. Closing the gap in LiteSpec gives every project the fact, checked; closing it in the book gives one view an unchecked fact. Settled by [decision/0008-book-follows-record-layers](../intent/decisions/0008-book-follows-record-layers.md) on the evidence in [research/book-outline-evidence](../intent/research/book-outline-evidence.md), [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence.md), and [research/check-coverage-evidence](../intent/research/check-coverage-evidence.md).

**Example:** The book needs to say that a refusal has no scenario, so LiteSpec gains a check for it, and the book shows that check's findings.

**Reviewed for:** Which facts does the book show that no record field, computed status or check defines? Look at the book beside the model output and the finding codes.

### Point Every Book Entry to Its Source

**ID:** `litespec/core/model#point-every-book-entry-to-its-source`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every record, code element, test and finding the book shows MUST link to the file and line it comes from.

**Rationale:** The book is where a reader notices what to change, and the source is where the change is made. A link to the file and line makes the next step one click, and it shows that every entry comes from a written record or from code, not from the book. Settled by [decision/0008-book-follows-record-layers](../intent/decisions/0008-book-follows-record-layers.md) on the evidence in [research/book-outline-evidence](../intent/research/book-outline-evidence.md), [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence.md), and [research/check-coverage-evidence](../intent/research/check-coverage-evidence.md).

**Example:** The submit-expense use case page links to `.litespec/behavior/modules/spending/expense/submit-expense.yaml` at its first line, and its handler to the handler file at the class.

**Reviewed for:** Which book entries lack a link to their file and line? Look at the book's record, code, test and finding entries.

## Conventions

### Render Generated Regions from the Model

**ID:** `litespec/core/model#render-generated-regions-from-the-model`

**Strength:** default

**Applies to:** artifact: any

**Default:** A generated region renders only facts derived from records.

**Replacement:** Code-computed facts live in the book and the model output.

**Rationale:** A generated region sits inside a record page, so everything in it reads with record authority. Facts computed from code, such as implemented and verified counts, describe the world rather than the promise, and rendering them beside records would lend them authority they do not hold. The book and the model output carry code-computed facts where readers meet them as computed.

**Reviewed for:** Which generated regions render code-computed facts? Look inside the generated fences beside the book and the model output.
