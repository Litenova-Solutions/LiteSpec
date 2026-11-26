# Project Model

**Stack:** any

**Concern:** records

A LiteSpec project describes itself with records: written statements of fixed types, joined by fixed links. People write the Intent records that state what the product promises, agents write the Behavior records from Intent to state how the system keeps those promises, and the model builder computes how code and tests connect to the records.

This page defines that record system for projects of every size, shape, and stack: the layers, types, files, links, identifiers, marks, authority order, and the states the builder computes. The Concepts explain each part, and the Standards make each rule testable.

## Concepts

### The Four Record Layers

Each layer answers one question, each layer is written from the layer above it as `litespec/core/layers#write-each-layer-from-the-layer-above-it` states, and each record belongs to the layer that owns it.

| Record layer | Question | Writes | Records |
|:--|:--|:--|:--|
| Intent | Why build it, and what do we promise? | people and agents | goal, direction, problem, feature, requirement, constraint, decision, research, flow, actor, role, term (`layer: intent`) |
| Behavior | How does the system behave to keep the promises? | agents | module, knowledge-area, aggregate, rule, domain-event, module-event, integration-event, use-case, result, scenario, reaction, job, read-model, notification, setting, reference-data, permission, refusal-set, refusal, entry-point, screen, external-system, term (`layer: behavior`) |
| Implementation | How is it built and operated? | agents | code, technical decisions, runbooks, operating procedures |
| Proof | How do we know it holds? | the builder | tests, check results, computed completion, the rendered book |

Governance sits beside the four layers instead of forming a fifth: the `waiver` record attaches to any layer, and sets a rule aside for one project with its reason.

Three translation rules join the layers:

- **Specify** moves Intent to Behavior. It adds the states, invariants, refusals, consistency, data classes with their erasure positions, freshness, and access with tenant scope that the Intent record implies but leaves unstated. It sends every price, fee, rate, term, market, payer, retention period for personal data, observable freshness, and default or figure that differs by tenant, market, or date to the owner, as `litespec/core/layers#raise-open-needs-as-owner-questions-or-backlog-items` states. A figure the business changes while the system runs is aggregate data: its range is a rule with a refusal, and its change is a use case with access and audit. A setting is operator configuration the running system reads, such as a provider timeout or a batch size.
- **Implement** moves Behavior to Implementation. It adds the code shapes the stack pages fix, and nothing else: no observable outcome that no result declares, no edge that no record declares, no write outside `writes`, and no access wider than the grant.
- **Verify** moves Implementation to Proof. One test proves each scenario through the public boundary. A proof is fresh when its run commit is HEAD, or when every path that changed between the run commit and the working tree is a record or documentation path. Otherwise the proof is stale and reads "last passed at <short commit>, <N> commits ago". A run records its commit.

### Record Types and Page Styles

A record type is one kind of statement a project can write, and each type answers one question. The type fixes what the record holds, where it lives, and what it links to.

| Type | Record layer | Question |
|:--|:--|:--|
| `product` | root | What is built, and for whom? |
| `goal` | intent | Which measurable outcome does the product serve? |
| `direction` | intent | Which way might the product grow beyond what it promises now? |
| `problem` | intent | Which pain does someone have today? |
| `feature` | intent | What is offered against a problem? |
| `constraint` | intent | Which outside rules hold? |
| `research` | intent | Which outside facts inform a choice? |
| `decision` | intent, implementation | Which choice holds, and what changes it? |
| `requirement` | intent | What does the product do or guarantee? |
| `flow` | intent | Which ordered steps reach the record it exists for? |
| `step` | intent | Which act of its performer moves the flow forward? |
| `actor` | intent | Who or what touches the product? |
| `role` | intent | Which permission sets exist? |
| `module` | behavior | Which product areas exist? |
| `knowledge-area` | behavior | Which knowledge areas exist? |
| `aggregate` | behavior | Which things stay consistent? |
| `rule` | behavior | What holds? |
| `use-case` | behavior | Which use cases does the product offer? |
| `result` | behavior | How does a use case end? |
| `scenario` | behavior | Which example proves the result? |
| `domain-event` | behavior | What happened? |
| `module-event` | behavior | Which facts pass between modules? |
| `integration-event` | behavior | Which facts cross the boundary? |
| `reaction` | behavior | Which work runs when an event arrives? |
| `job` | behavior | Which work runs on a schedule, once, or by hand? |
| `read-model` | behavior | What does a module keep of another module's published facts? |
| `notification` | behavior | Which person-facing messages go out? |
| `setting` | behavior | Which operator configuration does the running system read? |
| `reference-data` | behavior | Which dated values does the running system look up? |
| `permission` | behavior | Which single actions are allowed? |
| `refusal-set` | behavior | Which refusal codes does each aggregate and the module declare? |
| `refusal` | behavior | What does one refusal mean? |
| `entry-point` | behavior | How do people reach the product? |
| `screen` | behavior | Which page does a person see through an entry point, and what does it offer? |
| `external-system` | behavior | Which outside system exchanges what? |
| `runbook` | implementation | How is it run or recovered? |
| `waiver` | governance | Which provision, page or finding code is set aside, why, and until when? |
| `term` | intent, behavior | Which word means what? |
| `cast` | root | Which people, parties, occasions, and amounts do the examples share? |
| `party` | root | Which organization appears in the examples? |
| `person` | root | Which person in the examples plays which actor? |
| `occasion` | root | Which occasion do the examples share? |
| `figure` | root | Which typed value do the examples share? |

The [intent page](intent.md) states the Intent records, the [behavior page](behavior.md) states the Behavior records, and the [access page](access.md) states the access records.

Each record file holds the fields the record schema names for its type. List records hold many entries: a permission set per module holds permissions, and a refusal set per aggregate holds the codes that aggregate's rules raise, with the module's set holding only the codes no aggregate raises.

The book renders the retention schedule from the data classes.

Every record states something the software does, holds, refuses at runtime, or runs on, and translates into code, results, tests, or infrastructure. The [intent page](intent.md) states the test each Intent type meets.

### Size and Stacks

Each project declares its size, its shape, and its stacks in `.litespec/project.yaml`. Size decides which record types the checks require, so a small project is not held to records only a large one needs. Each frontend declares one stack and the backend declares its own; each stack brings the technology pages and checks that apply to it. A selected extension names the baseline content it replaces. The [scope page](scope.md) states how a project selects its size, shape, and stacks.

### Placement Principles

Placement is the choice of where a record lives. Ten principles place every record, from the product root down to a test.

- **One tree from the product.** The product is the root record, above the record layers. Every record has exactly one parent, and only the product has none. Every other link is a connection. An inverse link stays computed. When a reader needs it on the record, the record states it and a check holds it to its source.
- **The container is the parent.** An entry's parent is the record file that holds it. A record file's parent is its scope node: the product, a module, a knowledge area, an aggregate, or an entry point. No record writes an up-link that its location already states.
- **Five kinds of scope node, and only five.** Product, module, knowledge area, aggregate, and entry point are the scope nodes a folder can stand for. Every other folder name is a fixed type folder from a closed set per layer and level.
- **The lowest scope node that holds every user.** A record sits under the lowest scope node that holds every record linking to it within its layer. The author places it, and the tool validates it.
- **Locality.** Within one layer, a record under a module is linked from another module only along a declared module dependency, through a module event, through an integration event, through a queries edge to a public query that lists the consuming module, or through a commands edge to a public command that lists the sending module. Links from one layer down to the next may cross modules, because that is how one capability spans modules.
- **One identifier.** The identifier is short, unique in its record type, and what every link and citation uses. Placement shows as the folder path and the `module` field, never as a second name.
- **One name per module in every layer and in code.** `behavior/modules/finance/`, the backend source trees, the test projects, and the frontend modules all use the declared module name.
- **Code and tests follow the identifier.** The folder and namespace of code and tests that mark a record match that record's identifier and module.
- **Computed stays computed.** Proof and the generated folders hold no authored records.
- **The folder holds the content, the link names the reason.** A record nests under another record only when its whole content lies within that record, as a requirement lies within its feature. A record whose content spans several records sits at the lowest scope node that holds all of it. It names the record it exists for in a link field.

### The Placement Table

The Placement Table gives each type its home: its layer, the scope levels it may sit under, the storage folder that holds it, its containment parent, and its identifier. The table is frozen, and a record type gains a home only by changing this table.

Four words keep the placement facts apart:

- **Scope** is the node a record sits under: product, module, aggregate, or entry point.
- **Containment** is the record's parent.
- **Storage** is the file and typed folder that hold it.
- **Book grouping** is presentation, which follows containment except where this page says otherwise.

Folders are relative to `.litespec/`. A named record sits in its own folder named for it, as `<name>/<name>.yaml`, with its explanation file `<name>.md` beside the record file. The fixed-name files (`product.yaml`, `module.yaml`, `knowledge-area.yaml`, `aggregate.yaml`, `entry-point.yaml`, `permissions.yaml`, `refusals.yaml`, `glossary.yaml` and `cast.yaml`), the list files, and the entries (results and scenarios) keep the places the table gives.

Each parent with child records holds them in a folder named for the child type in the plural, inside the parent's own folder. A child stored as its own file sits in its own folder inside that typed folder, as `<typed folder>/<name>/<name>.yaml`. A child stored as an entry, such as a result, scenario, permission, refusal code, flow step, or source, stays in its parent's file. The table gives each child type's folder. Proposed by [decision/0060-put-child-records-in-typed-folders](decision/0060-put-child-records-in-typed-folders).

| Type | Layer | Scope | Storage | Containment | Identifier |
|:--|:--|:--|:--|:--|:--|
| `product` | Root | product | `product.yaml` | none | `product/<name>` |
| `goal` | Intent | product | `intent/goals/<name>/<name>.yaml` | product | `goal/<name>` |
| `direction` | Intent | product | `intent/directions/<name>/<name>.yaml` | product | `direction/<name>` |
| `problem` | Intent | product | `intent/problems/<name>/<name>.yaml` | product | `problem/<name>` |
| `feature` | Intent | product | `intent/problems/<problem>/features/<name>/<name>.yaml` | problem | `feature/<name>` |
| `requirement` | Intent | product | `intent/problems/<problem>/features/<feature>/requirements/<name>/<name>.yaml` or `intent/requirements/<name>/<name>.yaml` | feature or product | `requirement/<feature>.<name>` or `requirement/<name>` |
| `constraint` | Intent | product | `intent/constraints/<name>/<name>.yaml` | product | `constraint/<name>` |
| `decision` | Intent, Implementation | product, module | `intent/decisions/<number>-<name>/<number>-<name>.yaml`, `implementation/decisions/<number>-<name>/<number>-<name>.yaml`, `implementation/modules/<m>/decisions/<number>-<name>/<number>-<name>.yaml` | product or module | `decision/<number>-<name>` |
| `research` | Intent | product | `intent/research/<name>/<name>.yaml` | product | `research/<name>` |
| `flow` | Intent | product | `intent/flows/<name>/<name>.yaml` | product | `flow/<name>` |
| `step` | Intent | entry | in its flow file | flow | `step/<flow>.<act>` |
| `actor` | Intent | product | `intent/actors/<name>/<name>.yaml` | product | `actor/<name>` |
| `role` | Intent | product | `intent/roles/<name>/<name>.yaml` | product | `role/<name>` |
| `module` | Behavior | module | `behavior/modules/<m>/module.yaml` | product | `module/<name>` |
| `knowledge-area` | Behavior | product | `behavior/knowledge/<area>/knowledge-area.yaml` | product | `knowledge-area/<name>` |
| `term` | Intent, Behavior | product | `glossary.yaml` | product | `term/<name>` |
| `cast` | Root | product | `cast.yaml`, with `cast.md` beside it | product | `cast/<name>` |
| `party` | Root | product | `cast.yaml`, in the cast's `parties` list | cast | `party/<name>` |
| `person` | Root | product | `cast.yaml`, in the cast's `people` list | cast | `person/<name>` |
| `occasion` | Root | product | `cast.yaml`, in the cast's `occasions` list | cast | `occasion/<name>` |
| `figure` | Root | product | `cast.yaml`, in the cast's `figures` list | cast | `figure/<name>` |
| `aggregate` | Behavior | module | `behavior/modules/<m>/aggregates/<aggregates>/aggregate.yaml` | module | `aggregate/<module>.<name>` |
| `use-case` | Behavior | aggregate, module | `behavior/modules/<m>/aggregates/<aggregates>/use-cases/<use-case>/<use-case>.yaml`, `behavior/modules/<m>/use-cases/<use-case>/<use-case>.yaml` | aggregate or module | `use-case/<module>.<name>` |
| `result` | Behavior | entry | in its use case file | use case | `result/<use-case>.<name>` |
| `scenario` | Behavior | entry | in the file of its use case, reaction or job | that record | `scenario/<parent>.<name>` |
| `domain-event` | Behavior | aggregate | `behavior/modules/<m>/aggregates/<aggregates>/domain-events/<name>/<name>.yaml` | aggregate | `domain-event/<aggregate>.<name>` |
| `rule` | Behavior | aggregate, module, product | `behavior/modules/<m>/aggregates/<aggregates>/rules/<name>/<name>.yaml`, `behavior/modules/<m>/rules/<name>/<name>.yaml`, `behavior/rules/<name>/<name>.yaml` | its scope node | `rule/<home>.<name>` |
| `reaction` | Behavior | module | `behavior/modules/<m>/reactions/<name>/<name>.yaml` | module | `reaction/<module>.<name>` |
| `job` | Behavior | module | `behavior/modules/<m>/jobs/<name>/<name>.yaml` | module | `job/<module>.<name>` |
| `read-model` | Behavior | module | `behavior/modules/<m>/read-models/<name>/<name>.yaml` | module | `read-model/<module>.<name>` |
| `notification` | Behavior | module | `behavior/modules/<m>/notifications/<name>/<name>.yaml` | module | `notification/<module>.<name>` |
| `setting` | Behavior | module | `behavior/modules/<m>/settings/<name>/<name>.yaml` | module | `setting/<module>.<name>` |
| `reference-data` | Behavior | module, knowledge area | `behavior/modules/<m>/reference-data/<name>/<name>.yaml`, `behavior/knowledge/<area>/reference-data/<name>/<name>.yaml` | module or knowledge area | `reference-data/<home>.<name>` |
| `module-event` | Behavior | aggregate | `behavior/modules/<m>/aggregates/<aggregates>/module-events/<name>/<name>.yaml` | aggregate | `module-event/<module>.<name>` |
| `integration-event` | Behavior | aggregate | `behavior/modules/<m>/aggregates/<aggregates>/integration-events/<name>/<name>.yaml` | aggregate | `integration-event/<name>` |
| `permission` | Behavior | module | `behavior/modules/<m>/permissions.yaml` | module | `permission/<name>` |
| `refusal-set` | Behavior | aggregate, module | `behavior/modules/<m>/aggregates/<aggregates>/refusals.yaml`, `behavior/modules/<m>/refusals.yaml` | aggregate or module | `refusal/<module>.<name>` (each entry's id; the code is the id's name) |
| `entry-point` | Behavior | product | `behavior/entry-points/<entry-point>/entry-point.yaml` | product | `entry-point/<name>` |
| `screen` | Behavior | entry point | `behavior/entry-points/<entry-point>/screens/<home>.<name>/<home>.<name>.yaml` | entry point | `screen/<entry-point>.<name>` |
| `external-system` | Behavior | product, module | `behavior/external-systems/<name>/<name>.yaml`, `behavior/modules/<m>/external-systems/<name>/<name>.yaml` | product or module | `external-system/<name>` |
| `runbook` | Implementation | product, module | `implementation/runbooks/<name>/<name>.yaml`, `implementation/modules/<m>/runbooks/<name>/<name>.yaml` | product or module | `runbook/<name>` |
| `waiver` | Governance | product, module | `governance/waivers/<name>/<name>.yaml`, `governance/modules/<m>/waivers/<name>/<name>.yaml` | product or module | `waiver/<name>` |

Inside `behavior/modules/<m>/`, the subfolders are the typed folders from the closed set: `aggregates/`, `use-cases/`, `rules/`, `reactions/`, `jobs/`, `read-models/`, `notifications/`, `settings/`, `reference-data/` and `external-systems/`. Inside `behavior/knowledge/<area>/`, the only subfolder is `reference-data/`. Each aggregate sits in `aggregates/<aggregates>/`, where `<aggregates>` is the plural of the aggregate name (`expenses` for `aggregate/spending.expense`), and holds its `use-cases/`, `domain-events/`, `module-events/`, `integration-events/` and `rules/` folders with its `refusals.yaml` beside `aggregate.yaml`. The record id stays singular. Any other subfolder of a module is a `record-placement-invalid` finding.

A module is declared by its `behavior/modules/<m>/` folder, and a folder under `behavior/modules/` that names no declared module gets a `record-placement-invalid` finding. A knowledge area is declared by its `behavior/knowledge/<area>/` folder, and a folder under `behavior/knowledge/` that names no declared knowledge area gets a `record-placement-invalid` finding. A record of a type kept in the wrong folder, a record type outside its allowed scope, and a child record outside its typed folder, such as an aggregate directly in its module folder, are `record-placement-invalid` findings. A command use case at module level is a `record-placement-invalid` finding: every command belongs to the aggregate it changes, and module level keeps only module-wide reads. A named record whose folder name differs from its file stem is a `record-placement-invalid` finding.

A module, a knowledge area, an aggregate, and an entry point are scope nodes, and the folder path of each record under one names that node. Storage is the folder path and the home, never a second name. A feature answers the problem whose folder holds it, and a product-level requirement cites at least one constraint, goal, or problem in `cites`.

A rule home is an aggregate name or a module name. A reference-data home is a module name or a knowledge-area name. A rule identifier keeps its home segment when the rule moves; only identifiers whose first segment names a module change with a module move.

Policy rules are Intent requirements or constraints; an invariant sits under its aggregate or module, and an invariant across modules sits at product level, as the one stated exception. Module-level rules live in their own folders under `behavior/modules/<m>/rules/`; `module.yaml` holds no rules, and a rule in `module.yaml` is a `record-placement-invalid` finding.

An integration event has exactly one publishing module: the module of the use cases raising the domain event it translates. A second publishing module is an `event-publisher-invalid` finding. An integration event sits under the aggregate whose domain event it translates, in its publishing module, or under the aggregate of its receiving module whose use case it runs when inbound. A module event has exactly one publishing module, its own module. A second publishing module is an `event-publisher-invalid` finding. A module event sits under the aggregate whose domain event it translates. A module-level `module-events/` folder is a `record-placement-invalid` finding.

List records (the permission set per module and the refusal sets per aggregate and module) are files whose entries take the file's scope node as parent. Each entry point is one record in its own folder, with `screens/` beside it.

An external system sits at the lowest scope node holding every record that calls it. A product-level external system called from one module only is an `external-system-placement-wide` finding.

The book groups a use case's scenarios by result, and it shows each feature under the problem whose folder holds it. The first grouping is presentation; the second follows containment.

`fulfilledBy` does not exist: a use case keeps `fulfills` pointing up to the requirement, and the book computes the inverse. Every link points from the lower or later record to the higher or earlier one: each permission names the roles that grant it in `grantedTo`, each settled record names its decision in `basedOn`, and each external system names the actor it represents in `represents`. The reverse views are gathered for `model query` and the book, never stored.

Module dependencies form a directed acyclic graph over Contracts references, `queries` edges, `commands` edges, and reactions to facts, beside the `dependsOn` links; a `module-dependency-invalid` finding reports a cycle.

Decision numbers come from one sequence across every home, allocated by `litespec new decision`, which takes the next free number. A repeated number is a `record-key-duplicate` finding.

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

### Record Parents

A parent is the one record a record sits under. Every record except the product has exactly one parent, so book grouping places each record once: under its parent, or at the top of its own layer when the product parents it. A use case's scenarios group by result instead.

Every other link on the record is a connection: it renders with the reads-as phrase the Links table states, never as a second placement. An inverse link stays computed unless a reader needs it stated, in which case a check holds the copy.

Containment follows storage. An entry's parent is the record file that holds it: a scenario's parent is its use case, reaction, or job. A record file's parent is its scope node, or the record whose folder holds it when it nests inside another record: a feature's parent is its problem, and a feature requirement's parent is its feature. List records are files whose entries take the file's scope node as parent.

Types at product level take the product as parent with no field written: a project has exactly one product, so the field would repeat one value on every record.

The product has no record parent; its parent is the project that declares it, and it carries the layer key `root`. Intent, Behavior, Implementation, Proof, and Governance are transparent group nodes on the path from product to record: a group node is never a parent and never satisfies the parent rule.

The Placement Table above names each type's scope, storage, and parent.

### The Docs Layout

A consumer keeps its documentation in one folder per record layer, so the folder a page sits in says who writes it and what it may contain.

| Folder | Record layer | Holds |
|:---|:---|:---|
| `.litespec/intent/` | Intent | Goals, directions, problems with their features and requirements, product-level requirements, constraints, decisions, roles, flows, actors, research, and the product documents the business publishes, such as legal texts |
| `.litespec/behavior/` | Behavior | Modules with their aggregates, use cases, rules, domain events, reactions, jobs, read models, notifications, settings, module events, integration events, permissions and refusal sets; knowledge areas with their reference data; entry points with their screens; external systems |
| `.litespec/implementation/` | Implementation | Technical decisions under `decisions/` at product or module level; runbooks and operating procedures; the UI catalog, captures and patterns per entry point; the shared pattern catalog under `patterns/`; product copy rules under `writing/`; the API contract under `api/`; and guides for people working in the code. The guides describe code and hold no records |
| `.litespec/proof/` | Proof | Evidence the tools compute or a run leaves behind: generated registers, gate files, test runs, and operating logs. No person writes a fact here that a tool can compute |
| `.litespec/governance/` | Governance | Waivers at product level or under modules, and project conventions under `conventions/` |

A named record lives in its record file `<name>.yaml` inside its own folder `<name>/`, and an explanation file `<name>.md` may sit in that folder beside it. A document holds no record. The folder name equals the file stem, so the folder and the file form one pair that the tools find together.

The product record (`.litespec/product.yaml`) stands at the top of `.litespec/`, above the record layers. The glossary (`.litespec/glossary.yaml`) and the cast (`.litespec/cast.yaml`, with its optional explanation `.litespec/cast.md`) stay at the top of `.litespec/` beside it, because every record layer uses them. The cast holds the people, parties, occasions, and amounts every example draws from; [The Cast](#the-cast) states its shape.

Proof holds no authored records. Tests are code, placed by the rule the Where Code and Tests Sit section states, and the model computes completion from their marks and results.

### Pages and Entries

A page record is one record file `<name>.yaml` holding one YAML mapping: the page record with its entries in `records`. An entry record is an object in the `records` sequence of that mapping.

A requirement is a page record in its own folder, and it holds no entries. Terms are the exception: each term is an entry in the glossary. The Placement Table above names each type's home.

A record with reasoning of its own gets its own folder and explanation: a rationale, sources, an example, or limits give a record enough to say on its own. Rules, settings, and domain events take this form. Only closed lists whose entries carry no reasoning of their own stay as entries in a set or their parent's file: permissions, refusals, entry points, results, scenarios, and cast entries.

Container pages (`permission-set`, `entry-point-set`, `refusal-set`) are record files: they hold entries.

The decision type spans two layers: a product decision lives under Intent, and a technical decision lives under Implementation. Both share one number sequence, so a number still names one decision wherever it sits. A technical decision carries `kind`, one of the six kinds the [decision records page](../reference/decision-records.md) names; a product decision takes none.

### The Cast

The cast is the one set of people, parties, occasions, and amounts that the project's examples and every `Example` section draw from. It is one record, `.litespec/cast.yaml`, with the identifier `cast/<name>`, and it is optional: a project with no cast has no cast finding. Its explanation, `.litespec/cast.md`, tells the cast's story for people and adds no fact.

The cast page holds four optional lists, and each entry states the type its list names:

- `parties` holds party entries. A party has a `name`, a `uses` of `product` (the organization the product serves) or `appears-in-data` (an organization that appears only in data), a `description`, and optional `facts`, each naming a glossary term and the value it states.
- `people` holds person entries. A person has a `name`, the `actor` it plays (required), an optional `partyOf` party it belongs to, an optional `email` on a reserved test domain, and what it `does`.
- `occasions` holds occasion entries. An occasion has a `title`, a `scene`, and optional `date` (an ISO date), `place`, and `involves` (the people and parties it involves).
- `figures` holds figure entries. A figure has a `title` and exactly one typed `value`: `money` (a decimal `amount` and a three-letter ISO `currency`), `quantity` (a decimal `amount` and a `unit`), `percentage` (a decimal string), `date`, `count` (a non-negative whole number), or `text`. An optional `term` names the glossary term the figure states.

The cast page may also hold `today`, the ISO date every example shares as now, and `timeZone`, the IANA time zone the cast's dates belong to.

Each identifier takes the grammar `party/<name>`, `person/<name>`, `occasion/<name>`, `figure/<name>`, or `cast/<name>`, where the name is lowercase words joined by hyphens. The links are `actor`, `partyOf`, `involves`, and `term`, and the Links table reads each one's phrase from the record schema.

The model checks the cast in four ways. A malformed cast is a `record-invalid` finding. A person's `email` on a domain that no reserved test domain covers is a `cast-identity-unreserved` finding, at level G. A cast entry that no record field, explanation file, or seeds mark cites, by its identifier or, for a person or party, by its exact name, is a `cast-entry-unused` finding, at level R reported by `litespec model review`. A seeds mark that names no cast entry is a `mark-unknown` finding. The [book](#the-book) lists each entry with the records, explanation files, and code marks that cite it, computed from those citations.

### Record Files

A record file is one file `<name>.yaml` holding one YAML mapping: the page record, or a container page, with its entries in `records`. The mapping holds exactly the fields the record schema defines for its type. A record file holds no Markdown and no frontmatter fences.

Every `.yaml` file under a record root is a record file, except the glossary file; a file that does not parse, or whose top level names no record type or container page style in `type`, is a `record-invalid` finding. `.yml` files are not read. Multi-line text uses a `|` block scalar or a plain string, never a folded `>` block, which the reader refuses.

```yaml
summary: Finance reimburses the approved expense.
rationale: |
  Approvers batch decisions monthly.
  The wait costs more than the money held.
```

### Explanation Files

An explanation file is the `<name>.md` beside its record file, with the same name in the record's own folder. It is plain Markdown with no frontmatter: no field joins the two files, so the shared name is the link, and a rename moves both. It opens with `# <record title>`, then one paragraph of prose, then H2 sections in the order of the explanation-section table in [the writing standard](writing.md).

It restates the record in the same specification for people, elaborating each field in plain words with context and an example. One explanation file serves one page; entries have no file of their own, and the page explanation cites them by identifier.

Inside Intent and Behavior records, a generated region derives from other records only. A record file on a layer the project lists in `explanations.layers` in [the project file](../reference/project-file.md) carries its explanation file; on any other layer the file stays optional. The layers a project can list are `intent`, `behavior` and `governance`.

### Documents

A document is a `.md` file under a record root with no `.yaml` of the same name beside it. It is Markdown with an optional YAML frontmatter block between `---` lines, holding only a document page style in `type` and its common fields. The document page styles are `index`, `tutorial`, `how-to`, `reference`, `command`, `configuration`, and `glossary`. A document holds no record type and no entries.

A `.md` file with no frontmatter and no record file beside it is an orphaned explanation, except a file named `README.md`, which is a document. Document pages may sit at a layer root.

### What Goes in the Record File

Anything a check reads is a field in the record file. Anything that only helps someone understand goes in the explanation file. The split keeps checked facts where the checks read them, and prose where people read it.

Every record except a direction carries one `summary`, and a record that makes a testable claim also carries one `statement`. A direction states `statement` and takes `summary` only when the owner wants one. The opening paragraph and the fixed sections of each record type live in the explanation file, and so does any project-specific section under its own H2 heading.

### Research and Decision Articles

Two record types read as prose articles: `research` and `decision`. Their explanation file, when present, carries the required H2 sections in the order the writing standard gives: Findings for research, and Context, Options, Decision, Consequences and Revisit Conditions for decision.

### How Record Files Are Read

A record file holds one YAML mapping, read by YamlDotNet. The reader accepts the YAML YamlDotNet reads, except it refuses the following, each reported with the line it sits on:

- An empty document.
- A document that does not start at the first column.
- `---` and `...` markers.
- Tabs in indentation.
- Folded `>` blocks.
- Duplicate keys.

Scalars resolve by the YAML 1.2 core schema; a number the core schema reads that JSON cannot hold (`0x1F`, `.inf`, `+1`) is refused. A scalar that would resolve to a non-string where a string is meant is quoted, such as `"0001"`.

### Canonical Form

Canonical form is the one layout every record file shares, owned by the tool. `litespec model format` rewrites every record file into it: block mappings and sequences with two-space indentation, sequences indented under their key, keys in schema order, multi-line text in `|` block scalars, plain scalars where they read back as the same string and double quotes otherwise, and one line end at the end of the file. One layout keeps diffs on content: a change shows the moved fact, never reflowed prose or reordered keys.

Keys open with `type`, `id`, and `title`, continue with the type's own fields in schema order, then `terms` where the type carries it, and close with `records`; any other key keeps its written order after these. Only a permission carries `risk`. An object without a record `type` keeps its written key order.

`litespec model format --check` lists each file that differs without writing; the [model command page](../tools/model.md) lists the exit codes. A record file that differs from its canonical text is a `format-drift` finding.

### Identifiers

An identifier is the one name every link and citation uses for a record. It names its record type first, then the record name, with the owning home in between when the type has one. The name carries one to six lowercase hyphenated words, chosen by a person; a decision counts its number as a word. Names follow the grammar the [record language page](language.md) states.

| Type | Form | Example |
|:---|:---|:---|
| `product` | `product/<name>` | `product/claimwick` |
| `goal` | `goal/<name>` | `goal/faster-reimbursement` |
| `direction` | `direction/<name>` | `direction/contractor-expenses` |
| `problem` | `problem/<name>` | `problem/expenses-wait-weeks` |
| `feature` | `feature/<name>` | `feature/approval-reminders` |
| `constraint` | `constraint/<name>` | `constraint/tax-receipt-retention` |
| `research` | `research/<name>` | `research/approval-time-benchmarks` |
| `decision` | `decision/<number>-<name>` | `decision/0001-receipt-threshold` |
| `requirement` | `requirement/<feature>.<name>` or `requirement/<name>` | `requirement/approval-reminders.receipt-over-threshold` |
| `flow` | `flow/<name>` | `flow/submit-and-reimburse` |
| `step` | `step/<flow>.<act>` | `step/submit-and-reimburse.submit-expense` |
| `module` | `module/<name>` | `module/spending` |
| `knowledge-area` | `knowledge-area/<name>` | `knowledge-area/travel` |
| `aggregate` | `aggregate/<module>.<name>` | `aggregate/spending.expense` |
| `rule` | `rule/<home>.<name>` | `rule/expense.approver-is-not-submitter` |
| `use-case` | `use-case/<module>.<name>` | `use-case/spending.submit-expense` |
| `result` | `result/<use-case-name>.<name>` | `result/submit-expense.receipt-missing` |
| `scenario` | `scenario/<parent>.<name>` | `scenario/submit-expense.submitting-with-a-receipt-succeeds` |
| `domain-event` | `domain-event/<aggregate-name>.<past-tense>` | `domain-event/expense.submitted` |
| `module-event` | `module-event/<module>.<name>` | `module-event/spending.expense-approved` |
| `integration-event` | `integration-event/<name>` | `integration-event/expense-submitted` |
| `reaction` | `reaction/<module>.<name>` | `reaction/spending.send-approver-reminder-on-expense-submitted` |
| `job` | `job/<module>.<name>` | `job/spending.expire-lapsed-holds` |
| `read-model` | `read-model/<module>.<name>` | `read-model/reporting.reimbursed-expense` |
| `notification` | `notification/<module>.<name>` | `notification/spending.approval-reminder` |
| `setting` | `setting/<module>.<name>` | `setting/spending.max-open-expenses` |
| `reference-data` | `reference-data/<home>.<name>` | `reference-data/spending.decision-target` |
| `actor` | `actor/<name>` | `actor/employee` |
| `role` | `role/<name>` | `role/approver` |
| `permission` | `permission/<name>` | `permission/approve-expense` |
| `refusal` | `refusal/<module>.<name>` | `refusal/spending.receipt-missing` |
| `entry-point` | `entry-point/<name>` | `entry-point/claimwick-app` |
| `screen` | `screen/<entry-point>.<name>` | `screen/claimwick-app.expense-list` |
| `external-system` | `external-system/<name>` | `external-system/payout-bank` |
| `runbook` | `runbook/<name>` | `runbook/clear-stuck-reimbursements` |
| `waiver` | `waiver/<name>` | `waiver/export-expenses-unimplemented` |
| `term` | `term/<name>` | `term/expense` |
| `cast` | `cast/<name>` | `cast/claimwick` |
| `party` | `party/<name>` | `party/harbor-and-pine` |
| `person` | `person/<name>` | `person/ava` |
| `occasion` | `occasion/<name>` | `occasion/client-visit` |
| `figure` | `figure/<name>` | `figure/train-fare` |

The home segment stays short because names are unique where it matters. A screen's `<name>` follows the same one-to-six-word rule as every other name; its `<entry-point>` is the name of the entry point it belongs to, and agrees with its folder. A rule home is an aggregate name or a module name, and a reference-data home is a module name or a knowledge-area name.

A use case name is unique among use cases, so a result cites only that name. Use case, reaction, and job names share one namespace, so a scenario cites only its parent name. An aggregate name is unique among aggregates. A flow name scopes its steps, so each step id carries its flow.

A feature requirement's feature segment names its feature, and the feature's folder holds it. A product-level requirement carries no feature segment. A rule name keeps the negation its statement makes. Every identifier citation resolves to a written record.

The folder decides. A record that moves to another module changes the module segment of its identifier and every citation in the same change. An identifier whose home segment disagrees with its folder is an `record-placement-invalid` finding.

Each name stays unique in its record type, and a `record-key-duplicate` finding reports a duplicate. Folders do not narrow uniqueness within a type. A page record takes its identifier name from its title, with the name words appearing in the title words in the same order.

A flow step id is `step/<flow>.<act>`, where `<flow>` is the flow's name and `<act>` is the step's act as a verb phrase in kebab case. List order is the main path; no position lives in an id. An act that only numbers the step, such as `s3` or `step-2`, is reported, and so is a `<flow>` segment naming another flow.

A derived generic result is `result/<use-case>.forbidden`, `result/<use-case>.invalid-input`, `result/<use-case>.not-found`, `result/<use-case>.conflict`, `result/<use-case>.unauthenticated`, `result/<use-case>.sign-in-too-old` or `result/<use-case>.rule-not-stated`. Scenarios can point at derived results. Derived results are never written as records.

### Common Fields

Every written record carries the shared fields in this table, in its record file. They are the fields every join needs. Field names follow `litespec/core/language#name-each-field-by-its-word-class`.

| Field | When written | Meaning |
|:---|:---|:---|
| `type` | always | The record type in kebab case |
| `id` | always | The identifier |
| `title` | always | The display title, free to change |
| `terms` | when used | The term identifiers the record uses |
| `records` | on a page with entries | The entries the page holds |

`risk` lives on `permission` entries only: the analyzers generate the permission table with its risks into code, so it is a product fact there.

A field that records the history of the record is gone, because git holds it. A field that states a fact about the product or the world, including a future date, stays: `expiresOn` on a waiver and `readOn` beside an outside source.

### Citations in Prose

A Markdown link whose target is a record identifier cites that record. A flow step is cited by its `step/<flow>.<act>` identifier like any record. Link checks resolve the identifier against the model's identifiers, citation checks count it, and the book links it to the record. Prose cites a record by its identifier, never by its file path.

### Links

A link is a typed reference from one record to another, and each written link lives as a field on the record it starts from: a permission carries `grantedTo`, a rule carries `fulfills`, and so on. Each link property carries one meaning, so every verb reads the same everywhere.

The table below is generated from the schema's `x-reads-as` and `x-read-back-as` annotations. Each link states how it reads in both directions, and the book takes its connection labels from those two phrases.

<!-- generated:links -->
| Field | Written on | Points at | Reads as | Read back as |
|:---|:---|:---|:---|:---|
| `terms` | any record | term | uses | is used by |
| `grantedTo` | permission | role | is granted to | grants |
| `refusal` | result, rule | refusal | is refused as | is the refusal for |
| `result` | scenario | result, derived result | ends in | is shown by |
| `exercises` | scenario | rule | exercises | is exercised by |
| `competesWith` | scenario | use case | competes with | is competed with by |
| `audience` | entry point | actor | is used by | uses |
| `servedContent.fulfills` | entry point | requirement | fulfills | is fulfilled by |
| `users` | product | actor | is used by | uses |
| `basedOn` | constraint | research | is based on | is a basis for |
| `basedOn` | decision | constraint, research, direction, requirement | is based on | is a basis for |
| `supersedes` | decision | decision | supersedes | is superseded by |
| `affects` | problem | actor | affects | is affected by |
| `evidence.source` | problem | research | cites as evidence | is cited as evidence by |
| `advances` | feature | goal | advances | is advanced by |
| `cites` | requirement | goal, direction, problem, feature, constraint, research, decision | cites | is cited by |
| `basedOn` | requirement, rule | constraint, research, decision | is based on | is a basis for |
| `except.fallback` | requirement | requirement | falls back to | is the fallback for |
| `holds` | actor | role | holds | is held by |
| `actor` | flow | actor | is taken by | takes |
| `serves` | flow | goal | serves | is served by |
| `answers` | flow | problem, constraint | answers | is answered by |
| `steps.actor` | flow | actor | is performed by | performs |
| `steps.carries` | flow | requirement | carries | is carried by |
| `steps.branches.then` | flow | flow step | continues at | is continued from |
| `steps.branches.carries` | flow | requirement | carries | is carried by |
| `dependsOn` | module | module | depends on | is depended on by |
| `transitions.movedBy` | aggregate | use case, job | is moved by | moves |
| `dataClasses.basis` | aggregate | constraint, requirement, decision | rests on | is the basis for |
| `dataClasses.periodStartsAt` | aggregate | domain event, module event, integration event, aggregate field | starts at | starts the period of |
| `dataClasses.enforcedBy` | aggregate | job, use case | is enforced by | enforces |
| `access.recentSignIn` | use case | setting | is limited by | limits |
| `access.preconditions` | use case | rule | is authorized under | authorizes |
| `serves` | use case, reaction, job, screen | flow step | serves | is served by |
| `fulfills` | use case, reaction, job, notification, screen, setting, rule | requirement | fulfills | is fulfilled by |
| `computedBy` | use case | rule | is computed by | computes |
| `reachedThrough` | use case | entry point, module | is reached through | reaches |
| `raises` | use case | domain event | raises | is raised by |
| `writes` | use case | aggregate | writes | is written by |
| `reads` | use case, reaction, job | aggregate, setting, read model, reference data | reads | is read by |
| `consistency.boundBy` | use case | rule | is kept by | keeps |
| `consistency.guards` | use case | aggregate | holds the guard of | is guarded by |
| `conflicts.refusal` | use case | refusal | is refused as | is the refusal for |
| `effects.sends` | use case, reaction, job | notification | sends | is sent by |
| `effects.calls` | use case, reaction, job | external operation | calls | is called by |
| `effects.queries` | use case, reaction, job | use case | queries | is queried by |
| `effects.onFailure.surfacesIn` | use case | entry point | surfaces in | surfaces |
| `effects.onFailure.runbook` | use case | runbook | is answered by | answers |
| `consentRule` | use case, notification | rule | needs consent under | sets consent for |
| `steps.cites` | use case | rule, result, setting, domain event, read model, external system, aggregate, reference data | applies | is applied in |
| `triggeredBy` | reaction | domain event, module event, integration event | is triggered by | triggers |
| `compensates` | reaction | use case | compensates | is compensated by |
| `effects.runs` | reaction, job | use case | runs | is run by |
| `effects.commands` | reaction, job | use case | commands | is commanded by |
| `onFailure.surfacesIn` | reaction, job | entry point | surfaces in | surfaces |
| `onFailure.runbook` | reaction, job | runbook | is answered by | answers |
| `settles.aggregate` | reaction, job | aggregate | settles | is settled by |
| `schedule.timezone.setting` | job | setting | takes its time zone from | is the time zone of |
| `schedule.timezone.field` | job | aggregate field | takes its time zone from | is the time zone of |
| `runbook` | job | runbook | is answered by | answers |
| `audience` | notification | actor | is sent to | is the audience of |
| `deliveredThrough` | notification | external system | is delivered through | delivers |
| `claims.requirement` | notification | requirement | claims | is claimed by |
| `represents` | external system | actor | represents | is represented by |
| `processes.aggregate` | external system | aggregate | processes | is processed by |
| `erasure.operation` | external system | external operation | is erased through | erases |
| `erasure.event` | external system | published event | is erased through | carries the erasure of |
| `basedOn` | external system | decision, requirement | is based on | is a basis for |
| `payload.identifies` | integration event | identifies | identifies | is identified by |
| `through` | integration event | external system | travels through | carries |
| `arrivesThrough` | integration event | entry point, external system | arrives through | receives |
| `requires` | integration event | permission | requires | is required by |
| `translates` | integration event, module event | domain event | translates | is translated by |
| `keyedBy` | read model | aggregate | is keyed by | keys |
| `removedBy` | read model | published event | is removed by | removes rows of |
| `freshness.decidedBy` | read model | use case, aggregate | is decided by | decides |
| `waives` | waiver | standards provision, standards page | waives | is waived by |
| `appliesTo` | waiver | generic record, flow step | applies to | is applied to |
| `closedBy` | waiver | generic record, flow step | is closed by | closes |
| `host` | screen | screen | opens on | hosts |
| `offers` | screen | use case | offers | is offered by |
| `primaryAction` | screen | use case | runs as its primary action | is the primary action of |
| `fieldSources.from` | screen | use case | shows fields from | provides fields to |
| `leadsTo` | screen | screen | leads to | is reached from |
| `governs` | rule | aggregate, use case | governs | is governed by |
| `binds` | rule | aggregate | binds | is bound by |
| `cites` | rule | constraint, research | cites | is cited by |
| `releasedBy` | rule | use case | is released by | releases |
| `carries.identifies` | domain event | identifies | identifies | is identified by |
| `rows.refersTo` | reference data | reference data row | refers to | is referred to by |
| `rows.source` | reference data | constraint, research, decision | rests on | is the source for |
| `records.terms` | refusal set | term | uses | is used by |
| `facts.term` | party | term | instantiates | is instantiated by |
| `actor` | person | actor | plays | is played by |
| `partyOf` | person | party | belongs to | has member |
| `involves` | occasion | person, party | involves | takes part in |
| `term` | figure | term | instantiates | is instantiated by |
<!-- /generated:links -->

Every link points up: from the lower or later record to the higher or earlier one. Intent links point up: `serves`, `answers`, `fulfills`, `cites`, `advances`, `grantedTo`, `basedOn` and `represents` connect Behavior to Intent or Intent to the promise above it. A flow names no use case, and a requirement names no rule.

Effect and structure links follow the effect graph within Behavior: `runs`, `sends`, `calls`, `queries`, `commands`, `triggeredBy`, `translates`, `raises`, `offers`, `host`, `refusal`, `movedBy` and the read-model sources.

`holds` stays: actor and role are both Intent, so an actor naming its roles is not a layer down-link. The reverse views are gathered, never stored: `model query` and the book read a role's permissions from the permissions naming it in `grantedTo`, a decision's settled records from the records naming it in `basedOn`, and a system actor's outside system from the external system naming it in `represents`.

A read model carries no `fulfills`: the query that reads the read model fulfills the requirement. Each `servedContent` entry on a `browser` entry point carries `fulfills` naming the requirement its content meets.

Containment needs no link field: it follows storage. The scope node holds the parent, so `serves` on a flow and `answers` on a flow remain connections that carry meaning without deciding placement. A feature answers the one problem whose folder holds it, so the table carries no feature-to-problem link: the folder states it.

The builder computes the remaining links from code, routes, and tests:

- `implements`, from a code mark to the record it names.
- `follows`, from a code mark to the provision, decision, waiver, or project convention that shapes the code.
- `calls`, from a screen to a use case.
- `proves`, from a test mark to a scenario or rule.
- `publishes`, from a use case to each outbound integration event that translates a domain event the use case raises.

Code and tests point up to the record they serve through their marks, with each handler implementing exactly one record. The Placement Table above names each record parent.

### Computed Links

Some facts a reader needs sit on the record even though the model could derive them, and some stay derived. The table below lists the links and facts the model derives from other records, code, routes, or tests, and states for each whether the derivation stays computed or is stated on the record with the check that holds it.

| Link | Stated or Computed | Reason or Check |
|:--|:--|:--|
| Inverse of a written link | Kept computed | The book renders it from the reads-as phrases |
| Flow citations from step carries | Kept computed | The steps sit in the same file |
| Integration event publishers | Kept computed | Storage states the publishing module |
| Reactions listed on a domain event | Kept computed | Inverse of `triggeredBy`, shown in the book |
| Retention schedule from data classes | Kept computed | Judgement rendered in the book |
| `implements`, `follows`, `proves`, `calls`, `publishes` | Kept computed | Derived from marks, routes and tests |
| In-scope flows and features for completion | Kept computed | Judgement, never typed |
| `governs` on a rule | Stated in `governs` | `rule-governs-drift` |
| `exposure` on a use case | Stated in `exposure` | `exposure-reference-drift`, `exposure-missing` |
| `classification` on a field | Stated in `classification` | `classification-drift` |
| `fields` on a data class | Stated in `fields` | `data-class-fields-drift` |
| `serves` on a screen | Stated in `serves` | `litespec/core/edges#limit-each-screen-to-steps-its-use-cases-serve` |

### Marks

A mark is a written link from a code element to the record it serves. The link runs from code up to the record. A mark holds the record identifier, and every tool reads it the same way: the model builder and the analyzers.

It comes in one of two kinds: `implements` names the record served, and `follows` names the provision, decision, waiver, or project convention that shapes the code.

This page is the one home of the mark rules: the marks table below states the mark each code element carries, and the [Write Code for a Use Case](../guide/write-code-for-a-use-case.md) page states where each element goes and what checks it. A folder path stays the map a reader follows through the code, and it is never the link, except for a Domain part type, which takes no mark and belongs to the aggregate whose folder holds it (`litespec/backend/domain#place-every-part-type-in-its-aggregates-folder`).

| Record layer the code serves | Code element | Mark names |
|:--|:--|:--|
| Domain | Aggregate root | `aggregate/<module>.<name>` |
| Domain | Aggregate root method that changes state | `use-case/<module>.<name>`, exactly one: the use case whose act it performs |
| Domain | Domain event | `domain-event/<aggregate>.<fact>` |
| Domain | Refusal type for one rejected rule | `rule/<home>.<name>` |
| Domain | Domain service holding one rule calculation | `rule/<home>.<name>` |
| Domain | Part type: identifier, state type, closed set, child entity, value object, repository interface | No mark: the builder derives its aggregate from its folder |
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
| Tests | Test | The lowest record it verifies: `scenario/<parent>.<name>` or `rule/<home>.<name>` for behavior; `requirement/<feature>.<name>` or `requirement/<name>` of kind quality; `decision/<number>-<name>` for a technical choice; `external-system/<name>` or `integration-event/<name>` for an outside agreement; `litespec/<page>#<slug>` for a provision the code must keep; `<project>/conventions#<slug>` for a project convention the code must keep |
| Application, Infrastructure or API | Technical class: a top-level class that declares a method, or an options class that binds a setting, and takes no other mark. Nested types, records, interfaces, generated code, migrations, and model snapshots are never technical classes | One or more: a Behavior record it serves in an `implements` mark, or `litespec/<page>#<slug>`, `decision/<number>-<name>`, `waiver/<name>`, or `<project>/conventions#<slug>` in a `follows` mark |

A code element serving a record names it in an `implements` mark. A technical class names the record it serves in an `implements` mark, as an input validator names its use case and a repository names its aggregate.

Where it serves no record, it names the reason it is built the way it is in a `follows` mark: the LiteSpec provision that requires its shape, the technical decision that chose it, the waiver that allows it, or the project convention that states it. Any other code element may carry a `follows` mark as well.

The tools recognize code elements through the semantic model and then expect their mark: the generated mark attribute by its full name, and a handler by the interface it implements, wherever that interface comes from. A port and a domain service are recognized by their mark, and a job host by its hosted-service shape or by its mark.

A public method on an aggregate root that changes state needs its use-case mark even when it raises no domain event, so an unmarked port is found only from the top, through its external system.

Seven provisions divide the marks between them:

- `litespec/core/system#point-code-and-tests-up-to-records` requires the mark.
- `litespec/core/system#point-every-technical-class-up-to-its-record-or-choice` covers technical classes.
- `litespec/core/model#declare-one-mark-style` fixes the style.
- `litespec/core/model#match-each-backend-mark-to-its-folder` matches each backend mark to its folder.
- `litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark` checks each mapped record from the top.
- `litespec/core/model#name-the-lowest-record-each-test-proves` sets what a test names.
- `litespec/quality/testing#prove-each-scenario-through-the-public-boundary` places the test mark.

Mark style belongs to the language. C# marks use the one style the project declares as `marks` in `.litespec/project.yaml`: `xml` for a documentation tag or `attribute` for the generated attribute. TypeScript marks use the typed constant, and marks in YAML and other operations files use the comment.

One shared parser reads all three forms, each reader records the style it saw, and `mark-style-invalid` compares styles within one language only. A browser test carries its `proves` mark as a test annotation built by the `proves` helper the generated records file exports.

A runtime value a record already states, such as an event classification or a refusal code, is generated from the mark and the record where the stack generates code, and checked against the record where a person writes it.

A development seed or fixture names each cast entry it uses with a `seeds` mark: `<seeds>person/ava</seeds>` in a C# documentation tag, or `// litespec: seeds person/ava` in a TypeScript line comment. A seeds mark stands on no code element of a kind, so the element readers leave it out and the cast checks read it. `mark-unknown` reports a seeds mark that names no party, person, occasion, or figure. The [writing page](writing.md) states where cast names may appear.

### Where Code and Tests Sit

Backend code follows the module layout: one `Contracts/` folder holding data only grouped by aggregate, one `Domain/` folder holding one folder per aggregate in the plural, and the `Application/`, `Infrastructure/`, and `WebApi/` layer folders holding one plural aggregate folder with one folder per use case inside it plus one folder per module-level read, in one tree. The layout mirrors the records, so the folder of a file already says which record it serves. The namespace and folder of a type that marks a record match the record's identifier and module. A handler marking `use-case/finance.request-payout` sits in `{Product}/Finance/Application/Payouts/RequestPayout/`.

Tests mirror the code they prove, with the same check: a test marking a scenario sits under the folder path of the scenario's use case, so acceptance tests mirror `{Module}/{Aggregates}/{UseCase}/`.

Frontend code follows [Organize a Frontend into Frame, Routes, and Modules](../frontend/structure.md#organize-a-frontend-into-frame-routes-and-modules): module folders take the declared module names, and routes group by entry point.

One check owns this comparison, so each drift is reported once.

### Authority

Authority is the order that settles conflicts between records. Git is the decision record: a record in the repository is decided, and a record no longer wanted is deleted.

Intent outranks Behavior. A lower record cannot override a higher one: constraint over product, product over requirement, requirement over rule, rule over use case, use case over result, and result over scenario. A contradiction is reported and resolved by updating or removing the inconsistent records. Inside one record, a must beats a should. Several decisions may decide one record: a retention decision and a rounding decision both decide `aggregate/spending.expense`. A decision that replaces another names it in `supersedes`; a superseded decision stays in the repository as context, and the book shows it as superseded, computed from the inverse link. Every other link is trace: it explains why a record exists and never decides a conflict.

A decision explains a choice. Goals, problems, features, flows, actors, research, and prose carry no authority, so they never override another record.

Only an explicit product `excludes` overrides a requirement: the product page states exclusions in `excludes`, and its summary is never read as one.

A waiver outranks the LiteSpec provision its `waives` names for the project that records it: a gap waiver until its `expiresOn` date, and a lasting waiver for as long as its reason holds. [Waivers](waivers.md) states the record.

When code and the specification disagree, the specification wins where Intent has spoken. Where the specification is silent, an agent builds nothing: something that seems missing becomes a question to the owner or a backlog item, never code.

An accepted gap becomes a gap waiver with an `expiresOn` date, an `exitWhen` and `closedBy`: the findings its `waives` names are covered, and do not fail the run, until that date.

Module order states dependencies and grants no authority.

### Authoring Order

Records are authored top-down, each slice from Intent down: each slice writes its Intent before its Behavior, and its Behavior before its code. Each record lands with the Intent above it already written, so no Behavior record rests on a promise nobody stated yet. Independent slices may run at the same time. Actors, roles, permissions, and the glossary grow beside Intent and Behavior. A branch may be incomplete; what merges is complete.

Adding new intent to a running project follows the one sequence under New Intent in a Running Project on the [agent page](agent.md); this page states no second order.

### Reachability

Storage fixes where a record sits. Reachability fixes that it leads to software: every promise must trace down to behavior that keeps it, or it is unfinished work.

Each Intent type reaches Behavior in its own way:

- A goal reaches a flow serving it or a quality requirement.
- A flow serves one goal or answers one problem or constraint, and each step carries the requirements it names.
- A problem reaches a feature, a requirement, or a flow that answers it.
- A feature or requirement reaches a use case, screen, flow, or quality scenario in Behavior.
- A constraint is answered by a requirement or by a flow.
- Research is cited by a record that reaches Behavior.
- A decision is named in `basedOn` by a record that reaches Behavior.
- A direction reaches nothing by design: decisions and requirements cite it, and no reach check reads it, as the [intent page](intent.md#directions) states.

A record that reaches no Behavior gets an `intent-unreached` finding: it is either unfinished work, which belongs on the backlog, or not software.

Intent that is written but not yet served is planned, and planned Intent is a gap the project records. The planned-Intent findings are `intent-unreached`, `requirement-fulfillment-missing`, `step-use-case-missing`, `goal-mover-missing` and `constraint-unreached`.

Each one blocks in every run, local or in the pull-request check, whenever the project claims Behavior, because a link from Intent to the Behavior that serves it always blocks. A gap waiver with `expiresOn` and `closedBy` parks any of them for its appliesTo or paths until it expires. A project that claims Intent alone sees Intent coverage in the book and no planned-Intent finding.

Intent coverage states, for each Intent requirement and each flow step, whether some Behavior record reaches it, and the share reached. A requirement is reached when a Behavior record fulfills it, and a step is reached when a use case, reaction, or job serves it.

The builder computes the share from the records alone, so the book shows Intent coverage in every claim, first on the Intent part of the book.

### Completion Stages

Completion is how far a record has traveled from a promise to proven software. The builder computes one completion stage for every requirement, use case, flow, feature, and goal, and writes it as `completion` with `stage`, `proof`, `heldBy`, `parkedBy` and `counts`. The stages run from least to most complete: `unfulfilled`, `specified`, `coded` and `verified`. Nobody types a stage: the builder counts it from fulfillment, code, and proof on every run, so the figure never goes stale.

A parent is only as far as its weakest in-scope child, and `heldBy` names the in-scope children at that weakest stage, at most ten, with a `held by` count in `counts` naming the full number when the list is capped. `counts` holds the labeled done and of pairs behind the stage, with `inScope`, `parked` and `total`. `parkedBy` lists parked records with the waiver that parks each and its `expiresOn`.

`proof` reads `known` when the model holds a test run and `unknown` when it holds none; with no test run, a coded record stays `coded` with proof `unknown`, and proof unknown never reads as a failure. The book leads with completion.

Each type takes its stage from its children:

- A requirement is `unfulfilled` when no Behavior record fulfills it, `specified` when a fulfiller has no code, `coded` when every fulfiller has code and not every fulfiller is verified, and `verified` when every fulfiller is verified and its measure passes.
- A use case is `unfulfilled` when no scenario specifies it, `specified` when a scenario specifies it and no code implements it, `coded` when code implements it and not every scenario is verified, and `verified` when every scenario has a passing proof that is not stale.
- A flow takes the stage of its weakest step: an unserved step is `unfulfilled`, a served step with no code is `specified`, a step with code and no passing proof exercising it is `coded`, and an exercised step is `verified`.
- A feature takes the stage of its weakest in-scope requirement and flow. Its in-scope requirements are the requirements on the feature page, and its in-scope flows are the flows whose steps carry those requirements.
- A goal takes the stage of its weakest in-scope feature, flow, and requirement. Its in-scope flows are the flows serving it.
- A goal's completion shows delivery only: its outcome reads `not measured` until the goal records `observations` with date, measured value, and source, and a goal is never shown as reached from delivery alone.

A requirement a waiver parks stays out of the stage and counts as parked: the stage is computed over in-scope records, `heldBy` lists only in-scope records holding the stage back, and `parkedBy` lists parked records with the waiver that parks each and its `expiresOn`. The book shows "verified, 12 of 14; 2 parked", with a parked list naming each waiver and expiry.

The builder derives the proof words from code, screens, tests, and test runs on every run:

| State | Applies to | When |
|:--|:--|:--|
| coded | use case, rule, domain-event, aggregate state | the stack builder finds code for it |
| verified | use case, requirement | every scenario has a passing proof through the public boundary that is not stale; a requirement is verified when every fulfiller is verified and its measure's check passes |
| tested | quality requirement, technical decision, external system, integration event | a passing test that asserts something links to it; tested never makes a use case verified |
| stale | scenario proof | the run commit is behind HEAD with a changed path outside the record and documentation paths, or the run commit is unknown |
| flaky | scenario proof | the test both passed and failed within the runs read |
| reachable | aggregate state | a transition leads into it |
| exercised | flow step | a passing proof of a use case, reaction or job serving the step |

### Screen Captures

A capture is a screenshot of a built screen, filed where the builder can match it to the screen record. The project file names the screenshot galleries in `screenshots`: `galleries` holds one or more repository-relative folders, and `pattern` holds the file-name pattern with `{route}`, `{theme}` and `{width}`.

The builder records each screen's captures as `captures` with `path`, `theme`, `width` and `capturedOn`, and its `captureState` as `captured`, `route-not-captured` or `no-route`.

The route slug drops the leading slash of the screen route, joins its segments with a hyphen, reduces a bracketed, braced, colon or dollar parameter to its name, and names the root route `home`, so `/events/[id]` becomes `events-id` and the default pattern names its desktop light capture `events-id--light--1440.png`. `capturedOn` is the capture file's last-write date in UTC. A screen with a route and no matching capture is `route-not-captured`; a screen with no route and no route carrying it is `no-route`.

A missing gallery folder captures nothing and fails nothing. A gallery inside a declared frontend folder serves only the screens of that frontend's entry point, so two frontends that both serve the root route never share a capture.

### The Model Builder

The model builder is the tool that turns records, code, and test runs into one model. It reads record files, explanation files, documents, code, routes, tests, and test runs. It holds the model in memory, runs the model checks, and renders generated regions. The [model command page](../tools/model.md) lists the subcommands and finding codes.

Generated regions sit inside fences that only the builder writes and prose checks skip, in explanation files and documents. Handwritten prose and computed views stay in one file without mixing: a region that renders facts about its record finds the record through the explanation file name, and each project runs the builder in its own gate.

The `decision-index` region renders one table of the decisions beside it, grouped by kind, then by the computed concern of each decision's followers. The `waiver-index` region renders the waivers beside it, gaps by `expiresOn` with their `closedBy`, then lasting waivers with their consequences. The [decision records page](../reference/decision-records.md) states the columns of the decision table.

A finding that a live waiver covers is reported as covered and exits zero. A waiver is live when it has no `expiresOn` date or its date has not passed. [The waivers page](waivers.md) states how a waiver covers a finding, what no waiver covers, and when one expires. A waiver past its `expiresOn` date is reported as `waiver-expired`.

Proof state lives in `.litespec/proof/proofs.json`, committed, holding for each scenario the run commit and the outcome at the last passing run. A proof is fresh when its run commit is HEAD, or when every path that changed between the run commit and the working tree, committed and uncommitted, is a record or documentation path: a path under the project's LiteSpec folder (`paths.docs`, default `.litespec/`) or a Markdown file. Otherwise the proof is `stale` and reads "last passed at <short commit>, <N> commits ago". When git is unavailable or the run commit is unknown to the repository, the proof is `stale` with the reason. Only `litespec model check --record-proof` writes `proofs.json`; CI runs it on the pull request head, so a merged pull request carries fresh proofs.

### Check Levels

Every check has a level, and the level states what a finding rests on: records only, code read through a stack reader, or prose quality a person weighs.

| Level | Meaning |
|:--|:--|
| G | Rests on records only. Every missing required link is G. |
| S | Rests on code read through a stack reader. |
| R | Rests on prose quality a person weighs. Reported only by `litespec model review`; never blocks. |

Whether a finding blocks follows its level: a G finding blocks, an S finding blocks where the reader that produced it is qualified, and an R finding never blocks. A waiver parks a blocking finding; it never changes a level. There is no advisory level: `litespec model check` prints only findings that block.

Qualification is per reader, never global: a finding takes the qualification of the reader that produced its facts. A reader qualifies when it ran and its handshake matched. A finding no reader produced, a record finding, is qualified. A level S finding from an unqualified reader reports without blocking even when another reader in the same project is qualified.

A missing or failing reader produces `reader-missing` (level S) for that frontend, and that frontend's level S findings report instead of blocking. The same finding reports when:

- A .NET reader cannot read or parse a backend, test, or screen file.
- A declared stack root has no folder.
- A `server` project has no backend reader.
- A configured bill of materials cannot be read or parsed, is not SPDX or CycloneDX, or yields no packages.

Each names the file or path, and the model is built from what could be read.

`litespec model check` prints a last line when any reader was missing: `inspection incomplete: reader for <frontend> missing, <N> findings not blocking`, with one clause per frontend. It exits with code 3 when no finding blocks but the inspection was incomplete. Exit codes are 0 for pass, 1 for a blocking finding or a missing record, 2 for a usage error, and 3 for a pass with incomplete inspection.

`litespec model check` prints the level beside each finding.

`litespec model check` runs only the findings of the layers the project claims in `layers`, as the [scope page](scope.md#layer-claims) states. With only `intent` and `behavior` claimed, no stack reader runs, and the check runs the record findings and the Intent coverage the builder computes from records alone. Claiming `implementation` or `proof` adds the stack readers and the mark and proof findings. Claiming `implementation: litespec` adds the layout group, `code-placement-drift` and `mark-folder-drift`, the findings about LiteSpec's own code layout, with the stack and analyzer findings.

The [model command page](../tools/model.md) lists every finding code with its level, its layer, its trace, and its meaning.

`litespec model check` blocks on the planned-Intent findings in every run whenever the project claims Behavior, as the Reachability section states. A record that must stay unreached carries a gap waiver with `expiresOn` and `closedBy`, which then parks the finding for that record until it expires.

Two flags tune blocking without changing the levels:

- `litespec model check --release` makes `reader-missing` block for a claimed layer, unless a gap waiver with `expiresOn` and `closedBy` parks it.
- `litespec model check --since <ref>` checks the tree at `<ref>` and the working tree, and blocks every finding present now and absent at `<ref>`, so an adopting project clears new findings first. Findings match by code, subject identifier and message, never by line. When the base cannot be built or checked, every finding blocks and the output says why.

### The Book

The book is the model made readable: it renders the in-memory model for people, and judges nothing itself. It lives in the Proof record layer.

The book leads with completion, and it says `claimed` for a link and `verified` for proof: a `fulfills` link reads as a claim until its scenarios pass.

The book shows each flow as record detail:

- Its `startedWhen`.
- The steps with their `title`, their `statement`, their `failure`, and their branches indented under them, each branch with its condition and where the path continues or ends.
- Its `finishedWhen`.
- For each step, the requirements the step carries, with their statements and their `except` and fallback inline.
- For each step, the serving behavior in one line.
- For each step, its trace: the screens and use cases serving it, the requirements it carries with the use cases fulfilling them, and the code files and tests behind them.
- The Then clauses of its scenarios, as context.

Field tables render as tables. A use case's scenarios group under their result. Field names never render literally in flow detail. The book renders each record's explanation file under its fields, anchored at the record.

## Standards

### Place Child Records in Typed Folders

**ID:** `litespec/core/model#place-child-records-in-typed-folders`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A project MUST hold each child record stored as its own file in a folder named for its type in the plural, inside its parent's folder, and each child stored as an entry in its parent's file.

**Rationale:** A reader who opens a parent folder sees its own files with each kind of child kept apart, without opening each file. One rule for every parent replaces a placement sentence per type and gives every module one layout. A child stored as an entry has no file of its own, so a folder for it would hold nothing. Proposed by [decision/0060-put-child-records-in-typed-folders](decision/0060-put-child-records-in-typed-folders).

**Example:** A feature's requirements sit in `features/approval/requirements/`, and an aggregate's use cases sit in `aggregates/expenses/use-cases/`, while a result stays in its use case file.

**Checked by:** `litespec model check` reports `record-placement-invalid` for a child record outside its typed folder, and for a module subfolder that names no typed folder.

### Write Every Record in Its Home

**ID:** `litespec/core/model#write-every-record-in-its-home`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A project MUST keep every record in the home its type names.

**Rationale:** A fixed home tells a reader where to open a record and tells the builder where to read it. A record kept anywhere else misses the checks that scan its home, so it looks sound while nothing validates it. Settled by [decision/0042-record-named-folders](../intent/decisions/0042-record-named-folders/0042-record-named-folders.md), which puts each named record in its own folder beside its explanation.

**Example:** A decision lives at `.litespec/intent/decisions/0001-receipt-threshold/0001-receipt-threshold.yaml`.

**Checked by:** `litespec model check` reports `record-placement-invalid` for a folder outside the closed set of its layer and level, `record-placement-invalid` for a `modules` folder naming no declared module, and `record-placement-invalid` for a record outside its home.

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

**Rationale:** One file per page tells a reader where to open a record and tells the builder where to read it. The mapping holds exactly the fields the schema defines for its type, with entries in `records`, so every record has one shape and the checks read every record the same way. A record kept anywhere else misses the checks that scan its home. Settled by [decision/0020-records-in-yaml](decision/0020-records-in-yaml) on the evidence in [research/record-format-evidence](research/record-format-evidence).

**Example:** Claimwick keeps `use-case/spending.submit-expense` in `.litespec/behavior/modules/spending/aggregates/expenses/use-cases/submit-expense/submit-expense.yaml`, with its results and scenarios in `records`.

**Checked by:** `litespec model check` reports `record-invalid` for a record file that does not parse or whose top level names no record type or container page style.

### Explain Each Record Beside Its Record File

**ID:** `litespec/core/model#explain-each-record-beside-its-record-file`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An explanation file MUST sit beside its record file in the record's own folder, with the same name, and hold plain Markdown with no frontmatter.

**Rationale:** The shared file name is the link: no field joins the two files, so a rename moves both and nothing else. Plain Markdown with no frontmatter stays readable in any reader and tells every check to read fields from the record file only. The file opens with the record title and uses H2 sections, so each explanation has one shape.

**Example:** `submit-expense.yaml` explains its Example and Notes in `submit-expense.md` beside it.

**Reviewed for:** Does every Markdown file under a record root sit beside its record file or carry frontmatter? Look at the Markdown files beside the record files.

### Split Record Fields from Explanation

**ID:** `litespec/core/model#split-record-fields-from-explanation`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A fact a check reads MUST live as a field in the record file, the page's only metadata carrier.

**Rationale:** Two metadata carriers on one page disagree sooner or later: source links, paths, and evidence drift between the record file and the explanation file. One record file keeps every field a check reads in one place: fields are checked and prose is not, so a check-read fact sitting in prose is invisible to its check. The page explanation cites entries by identifier, so entries need no file of their own.

A generated region that `model render` writes from the record file is not a second carrier, because no person edits it and `model check` reports any drift from the record. The source-links region of a research or constraint explanation is one such region.

**Example:** The submit-expense summary lives in the `summary` field of `submit-expense.yaml`, while why the flow reads well lives under Notes in `submit-expense.md`.

**Reviewed for:** Does any explanation file state a fact a check reads, or any page carry field values outside the record file? Compare each explanation file with its record file.

### Require Explanation Files on the Layers a Project Names

**ID:** `litespec/core/model#require-explanation-files-on-the-layers-a-project-names`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A record page on a layer the project lists in `explanations.layers` MUST carry its explanation file with the required H2 sections.

**Rationale:** On a complex product, every record needs explaining for people who did not write it. Listing a layer in `explanations.layers` makes the explanation a gate instead of a habit. The layers a project can list are `intent`, `behavior` and `governance`, and every record file on a listed layer carries its explanation file, requirements and waivers included. On a layer the project does not list, the explanation file is optional for every record type, including research and decision records.

**Example:** Claimwick lists `intent` and `behavior` in `explanations.layers`, so its problem `problem/expenses-wait-weeks` carries `expenses-wait-weeks.md` with the sections Affected Users, Failure Cases, Current Workaround, Cost, Evidence and Resolution Criteria. A problem without that file, or without one of those sections, fails the check. When Claimwick lists `governance`, its waiver `waiver/export-expenses-unimplemented` carries its explanation file with Provision, Reason, Risk and Expiry.

**Reviewed for:** Does every record page carry its explanation file with the required H2 sections in order? Look at the record files beside their explanation files.

### Write Every Record File in Canonical Form

**ID:** `litespec/core/model#write-every-record-file-in-canonical-form`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every record file MUST match the canonical text the record writer produces for it.

**Rationale:** One layout for every record keeps diffs small and readers on content: a change shows the moved fact, never reflowed prose or reordered keys. The tool owns the layout, so `litespec model format` rewrites every record file into canonical form and the check reports any file that differs.

**Example:** A use-case file opens with `type`, `id`, and `title`, holds its own fields in schema order, and closes with `records`.

**Checked by:** `litespec model check` reports `format-drift` for a record file whose text differs from its canonical form.

### Keep Shape Rules in the Schema

**ID:** `litespec/core/model#keep-shape-rules-in-the-schema`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each rule on the shape of one record MUST be written in the record schema, and each rule on the graph between records stays in the model check.

**Rationale:** An editor validates a record against the schema alone, so a shape rule the schema lacks lets a record pass in the editor and fail in the check. Graph rules, such as reference resolution, ownership, and consistency across records, need every record at once, which a schema cannot read. The schema holds required properties, minimum counts, conditional requirements, and allowed values. A list of objects does not use `uniqueItems`, since that compares whole objects, so the model check reports each key that two entries of one record share. Proposed by [decision/0058-keep-shape-rules-in-the-schema](../intent/decisions/0058-keep-shape-rules-in-the-schema/0058-keep-shape-rules-in-the-schema.md) on the disagreements between the schema and the check.

**Example:** A Claimwick research record with an empty `sources` list fails the schema in an editor and the model check alike, and a research record that names source name `benchmark-report` twice is reported as `record-key-duplicate`.

**Checked by:** `litespec model check` reports `record-invalid` for a record that breaks a shape rule and `record-key-duplicate` for two entries of one record that share a key, and the parity test `RecordSchemaParityTests` fails when the schema and the model check disagree on a rule both carry.

### Give Every Record One Parent

**ID:** `litespec/core/model#give-every-record-one-parent`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every record except the product MUST have exactly one parent from the Placement Table on this page.

**Rationale:** A record with no parent has no place in the book, and a record with two parents appears twice. One parent per record places every record exactly once: the container is the parent, so an entry sits under the record file that holds it, a scenario sits under its use case, reaction, or job, and a record file sits under its scope node.

A feature requirement sits under its feature in its own folder, and types at product level, including a product-level requirement, sit under the product with no field written. The product is the root of the tree and stands above every record layer, settled by [decision/0091-place-the-product-above-layers](decision/0091-place-the-product-above-layers). Settled by [decision/0002-parent-for-every-record](../intent/decisions/0002-parent-for-every-record/0002-parent-for-every-record.md) on the evidence in [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence/behavior-containment-evidence.md), [research/intent-hierarchy-evidence](../intent/research/intent-hierarchy-evidence/intent-hierarchy-evidence.md), [research/book-outline-evidence](../intent/research/book-outline-evidence/book-outline-evidence.md), and [research/established-traceability-practice](../intent/research/established-traceability-practice/established-traceability-practice.md).

**Example:** In the Claimwick product, `requirement/approval-reminders.receipt-over-threshold` sits under `feature/approval-reminders` in `intent/problems/expenses-wait-weeks/features/approval-reminders/requirements/receipt-over-threshold/`; `use-case/spending.submit-expense` sits beside its aggregate; and `scenario/submit-expense.submitting-with-a-receipt-succeeds` sits in the file of `use-case/spending.submit-expense`.

**Checked by:** `litespec model check` reports `record-placement-invalid` (level G) for a record whose required parent is missing.

### Place a Record by Its Content and Link It by Its Reason

**ID:** `litespec/core/model#place-a-record-by-its-content-and-link-it-by-its-reason`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A record MUST nest under another record only when its whole content lies within that record, and otherwise sit at the lowest scope node that holds all of its content and name the record it exists for in a link field.

**Rationale:** A folder states that everything inside it belongs to the record above it. Content is what a record states and what it changes; its links are its reasons, not its content. A record filed under one of several records its content spans claims a scope it does not hold, and a reader looking for every record of its type must search the folder of every possible parent. The reason a record exists can change while its content stays the same, so a link field keeps that change to one line where a folder would move the record. Settled by [decision/0078-place-each-record-by-its-content](decision/0078-place-each-record-by-its-content).

**Example:** In the Claimwick product, `flow/submit-and-reimburse` carries requirements of both `feature/approval` and `feature/approval-reminders`, so it sits in `intent/flows/` under the product and names its goal in `serves`. A reaction answering another module's integration event sits with the module whose records it changes, and names the event in `triggeredBy`.

**Reviewed for:** Does any record sit under a record that holds only part of its content, and does any record type in the Placement Table take a parent whose scope is narrower than its content? Compare each record's folder with the records it cites and the records that serve it.

### Move Each Identifier with Its Module

**ID:** `litespec/core/model#move-each-identifier-with-its-module`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A record that moves to another module MUST change the module segment of its identifier and every citation in the same change.

**Rationale:** The folder decides: an identifier whose home segment disagrees with its folder names a home the record left. Only identifiers whose first segment names a module change with a module move. A rule identifier keeps its home segment, because the segment names the aggregate or module the rule is about rather than the folder that holds it. A move between product and module level changes no identifier, so no citation changes.

**Example:** Claimwick moves `use-case/spending.submit-expense` from the spending module to the finance module, so the use case becomes `use-case/finance.submit-expense` and every citation changes in the same change.

**Checked by:** `litespec model check` reports `record-placement-invalid` for an identifier whose home segment disagrees with its folder.

### Keep Each Name Unique in Its Record Type

**ID:** `litespec/core/model#keep-each-name-unique-in-its-record-type`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each name MUST stay unique in its record type.

**Rationale:** Short identifiers resolve with no lookup table only when each name means one record of its type. Two records of one type with one name collide everywhere the identifier is read, while two records of different types with one name coexist: `actor/owner` and `role/owner` are different records. Folders do not narrow uniqueness within a type. Use case, reaction, and job names share one namespace, so a scenario cites its parent without ambiguity.

**Example:** Claimwick names one feature `approval-reminders`; a second feature with that name is reported even though the folders differ. An actor named `owner` and a role named `owner` coexist.

**Checked by:** `litespec model check` reports `record-key-duplicate` for a name that occurs twice in one record type, and for a use case, reaction or job name that occurs in another of the three types.

### Take Decision Numbers from One Sequence

**ID:** `litespec/core/model#take-decision-numbers-from-one-sequence`

**Strength:** required

**Applies to:** artifact: record:decision

**Statement:** A decision number MUST come from one sequence across every home, with each decision folder named `<four-digit number>-<name>/` holding its record file.

**Rationale:** Product and technical decisions share one identifier space, so a number identifies one decision wherever it sits. `litespec new decision` takes the next free number, and no two decisions share it. Each decision folder is named `<four-digit number>-<name>/` and holds its record file `<four-digit number>-<name>.yaml`, with `decision/<four-digit number>-<name>` as its identifier. Numbers order decisions by when each choice was proposed, giving every record a short stable name a citation can carry. The number counts as a word, so digits never stand alone in the identifier.

**Example:** `decision/0005-render-receipts-as-pdf` is the fifth decision, whether it sits under Intent or under Implementation.

**Checked by:** `litespec model check` reports `record-key-duplicate` for two decisions sharing one number.

### Name Each Identifier from Its Title

**ID:** `litespec/core/model#name-each-identifier-from-its-title`

**Strength:** required

**Applies to:** artifact: record:product, record:goal, record:direction, record:problem, record:feature, record:requirement, record:constraint, record:decision, record:research, record:flow, record:step, record:actor, record:role, record:module, record:aggregate, record:use-case, record:reaction, record:job, record:notification, record:reference-data, record:integration-event, record:entry-point, record:screen, record:external-system, record:runbook, record:waiver

**Statement:** Every page record and every flow step MUST take its name from the words of its title in title order: a page record its identifier name, and so its folder and file names, and a flow step the act in its identifier.

**Rationale:** Readers browse the folders before they open a record, so the folder name must say what the record says. A retitle that keeps the identifier leaves the folder and file saying the old name while the record says the new one, and no listing shows the gap. Taking the name from the title's words keeps the identifier, the file, and the folder on what the record says. A step lives in its flow's file, so only its act faces its title. Proposed by [decision/0061-name-each-identifier-from-its-title](../intent/decisions/0061-name-each-identifier-from-its-title/0061-name-each-identifier-from-its-title.md).

**Example:** Claimwick titles a requirement "Nobody Approves Their Own Expense", so the requirement is `requirement/approval.nobody-approves-own-expense` in `nobody-approves-own-expense/nobody-approves-own-expense.yaml`. The name `no-self-approval` holds words the title does not hold, so it is reported. Claimwick titles a step "Submit Expense", so the step is `step/submit-and-reimburse.submit-expense`.

**Checked by:** `litespec model check` reports `identifier-title-invalid` for a page record whose identifier name is no word subsequence of its title, or whose record file is named for neither its identifier name nor the fixed file name of its type, and for a flow step whose act is no word subsequence of its title.

### State What a Reader Needs on the Record and Check Each Copy

**ID:** `litespec/core/model#state-what-a-reader-needs-on-the-record-and-check-each-copy`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A record MUST state the links and facts a reader needs to read it alone, with every stated fact the model can also derive held by a check that reports a difference.

**Rationale:** An isolated file reads whole by eye and by tool, without opening the files it links to. The copy stays honest because a check compares it with its source on every run: `governs` against the links naming the rule, `exposure` against the entry points and permissions the use case declares, a field `classification` against its data class, a data class `fields` list against the fields naming the class, and a screen `serves` against the steps its use cases serve. Judgments stay computed and are never typed: completion, coverage, proof status, counts, and censuses. Settled by [decision/0070-keep-one-source-for-each-fact](decision/0070-keep-one-source-for-each-fact).

**Example:** The Claimwick submit-expense use case states its `exposure` in plain words, and `exposure-reference-drift` reports an entry point it names that `reachedThrough` lacks.

**Checked by:** `litespec model check` reports `rule-governs-drift`, `exposure-reference-drift`, `exposure-missing`, `classification-drift` and `data-class-fields-drift` for a stated copy that differs from its source.

### Keep the Whole Specification in the Records

**ID:** `litespec/core/model#keep-the-whole-specification-in-the-records`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every fact an agent needs to build the product MUST stand in a record field, and no fact MAY stand only in an explanation file.

**Rationale:** An agent builds from the records, and a check reads only fields. A fact written only in prose reaches neither: the agent builds without it, and no finding notices when it changes. Keeping every fact in a field makes the YAML a complete specification on its own, and leaves the explanation file free to restate and elaborate it for people. Settled by [decision/0095-keep-the-whole-specification-in-the-records](decision/0095-keep-the-whole-specification-in-the-records).

**Example:** Claimwick's `problem/expenses-wait-weeks` states its failure cases in `failureCases` and the condition that ends the problem in `resolutionCriteria`. Its explanation's Failure Cases section restates those cases and walks through one with the cast.

**Reviewed for:** Does every explanation section rest on filled backing fields, with no fact beyond the record and the records it cites? Compare each explanation file with its record file.

### Write Links on the Record That Points

**ID:** `litespec/core/model#write-links-on-the-record-that-points`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A link MUST live as a field on the record it starts from, with an inverse stated only where a reader needs it and held by an agreement check.

**Rationale:** One writing site keeps a link and its source in one edit: changing the source updates the link in the same change. Links written on the target instead would scatter each relationship across every page it touches. A record states the inverse only when a reader needs it there, and the agreement check holds the copy to its source, as `litespec/core/model#state-what-a-reader-needs-on-the-record-and-check-each-copy` states. The Links table states how each link reads in both directions. Settled by [decision/0002-parent-for-every-record](../intent/decisions/0002-parent-for-every-record/0002-parent-for-every-record.md) on the evidence in [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence/behavior-containment-evidence.md), [research/intent-hierarchy-evidence](../intent/research/intent-hierarchy-evidence/intent-hierarchy-evidence.md), [research/book-outline-evidence](../intent/research/book-outline-evidence/book-outline-evidence.md), and [research/established-traceability-practice](../intent/research/established-traceability-practice/established-traceability-practice.md).

**Checked by:** `litespec model check` reports `reference-unknown` for a link whose target no record carries.

### Point Every Link Up

**ID:** `litespec/core/model#point-every-link-up`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every link MUST point from the lower or later record to the higher or earlier one, with no exceptions.

**Rationale:** A link from a higher layer to a lower one makes the layers depend both ways: an Intent record cannot be written or read before the Behavior records it names. Each permission names the roles that grant it in `grantedTo`, each settled record names its decision in `basedOn`, and each external system names the actor it represents in `represents`, so every record is written before the records above it read it. The reverse views are gathered for `model query` and the book, never stored. `holds` stays: actor and role are both Intent, so it is not a layer down-link.

**Example:** `permission/request-expense` names `role/employee` in `grantedTo`, and the book gathers the employee role's permissions from the permissions naming it.

**Checked by:** `litespec model check` reports `link-direction-invalid` (level G) for a link from a higher layer to a lower one.

### Connect Every Behavior Record to Intent

**ID:** `litespec/core/model#connect-every-behavior-record-to-intent`

**Strength:** required

**Applies to:** artifact: record:use-case, record:reaction, record:job, record:notification, record:setting

**Statement:** A use case, reaction, job, notification, or setting MUST carry `serves`, `fulfills`, or both.

**Rationale:** A behavior record with no served flow step and no fulfilled requirement answers to nothing above it: no promise explains why it exists. Pointing up with `serves` or `fulfills` ties every behavior to the Intent that asks for it. A use case is background-only when a reaction or job runs it, so `use-case-flow-missing` skips it.

**Checked by:** `litespec model check` reports `record-link-missing` for a use case, reaction, job, notification, or setting with neither `serves` nor `fulfills`, and `use-case-flow-missing` (level G) at large size for a use case that no reaction, job or other use case runs and that serves no flow step.

### Serve Every Flow Step

**ID:** `litespec/core/model#serve-every-flow-step`

**Strength:** required

**Applies to:** artifact: record:flow

**Statement:** Every flow step MUST have a serving use case, reaction, or job.

**Rationale:** A step no use case, reaction, or job serves promises a moment the product never builds. Use cases, reactions, and jobs point up with `serves`, so each step id collects the behaviors that carry the actor through it.

Three findings cover the three gaps: `step-use-case-missing` names a step no use case, reaction, or job serves, `step-screen-missing` names a step a browser or device use case serves that no screen serves, and `step-implementation-missing` names a step whose serving use cases have no implementing code. Reworded by [decision/0049-literal-flow-words-concrete-consequences](decision/0049-literal-flow-words-concrete-consequences).

**Checked by:** `litespec model check` reports `step-use-case-missing` for a flow step with no serving use case, reaction, or job, beside `step-screen-missing` for a step a browser or device use case serves that no screen serves, and beside `step-implementation-missing` (level S) for a step whose serving use cases have no implementing code.

### Connect Across Modules Only Through Declared Dependencies

**ID:** `litespec/core/model#connect-across-modules-only-through-declared-dependencies`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Within one layer, a record under a module MUST be linked from another module only along a declared module dependency, through a module event, through an integration event, through a queries edge to a public query that lists the consuming module in `reachedThrough`, or through a commands edge to a public command that lists the sending module in `reachedThrough`.

**Rationale:** A record linked from another module with no declared tie is not local: it belongs at product level. The declared dependency, the module event, or the integration event names the tie, so the seam where a monolith would split stays visible. A queries edge to a public query that lists the consuming module names its own tie: the consuming module's `queries` edge names the query, and the query's `reachedThrough` names the consuming module back. A commands edge to a public command that lists the sending module names its own tie the same way. Links from one layer down to the next may cross modules, because that is how one capability spans modules. This provision is the one rule for links across modules.

**Example:** A spending rule cited from the finance module with no `dependsOn` tie and no module or integration event between them is reported; the rule moves to product level or the modules declare the tie. Reporting's reimbursement command declares a `queries` edge to spending's read-expense query, which lists `module/reporting` in `reachedThrough`, so the edge passes with no `dependsOn` tie. Reporting's close-period command declares a `commands` edge to spending's close-expenses command, which lists `module/reporting` in `reachedThrough`, so that edge passes the same way.

**Checked by:** `litespec model check` reports `link-locality-invalid` for a record under a module that is linked from another module with no declared dependency, module event, or integration event. A queries edge to a public query that lists the consuming module passes, and so does a commands edge to a public command that lists the sending module; a queries edge to a query use case whose `reachedThrough` does not name the consuming module is reported as `query-target-invalid`, and a commands edge to a command use case whose `reachedThrough` does not name the sending module is reported as `command-target-invalid`.

### Declare Cross-Module References in DependsOn

**ID:** `litespec/core/model#declare-cross-module-references-in-depends-on`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** An `input`, `output`, or `steps` entry of a use case MUST name a record of another module only when the use case's module lists that module in `dependsOn` or when the entry is an input or output field stating `held: opaque` for an identifier the module stores and passes without loading, querying, or reasoning about the named aggregate.

**Rationale:** The link checks read a use case's top-level links, but an entry inside `input`, `output`, or `steps` can name another module's record without appearing in any of them. Such a reference is a dependency the record graph does not declare, so the module seam stays invisible to a reader and to a check. Naming the dependency in `dependsOn` states the tie where module ties belong; a module event, an integration event, or a public edge states the crossing when the two modules should not depend on each other. A module that only stores and passes an identifier needs no tie to the owning module: the value arrives through an event, a public query result, or the caller, and the module never follows it into the aggregate. Declaring `dependsOn` for such a carried identifier would tie modules that never call each other and can close a dependency cycle. The field states `held: opaque` to record the carrying, and it still states how the value arrives: an input keeps its `origin`, and a query output group keeps its `reads`. The exemption ends where use begins: a use case that also reads or writes the named aggregate, or whose step cites it, still declares the dependency.

**Example:** In Claimwick, an input of `use-case/spending.reimburse-expense` names `aggregate/reporting.reimbursed-expense` in `identifies`. Module `spending` does not list `module/reporting` in `dependsOn`, so the entry is reported; the modules either declare the tie or the use case reads the value through a public query. When a reporting use case takes the expense identifier its row came from as an input it only stores and passes, the input states `held: opaque` and needs no `dependsOn` tie to spending. When the same use case also reads the expense aggregate or a step cites it, the input is reported like any other cross-module reference.

**Checked by:** `litespec model check` reports `module-reference-undeclared` (level G) for an `input`, `output`, or `steps` entry that names another module's record with no declared dependency. A field stating `held: opaque` passes unless the use case also reads or writes the named record or a step cites it.

### Keep Module Dependencies Free of Cycles

**ID:** `litespec/core/model#keep-module-dependencies-free-of-cycles`

**Strength:** required

**Applies to:** artifact: record:module

**Statement:** Module dependencies MUST NOT form a cycle.

**Rationale:** A dependency cycle says each module builds on the other, so neither has a seam to split along. The graph runs over Contracts project references, `queries` edges, `commands` edges, and reactions to another module's facts, beside the `dependsOn` links. A cycle means a concept is split across two modules, and the fix moves the concept into one module, never through a shared identifier. Settled by [decision/0110-module-dependencies-form-a-dag](decision/0110-module-dependencies-form-a-dag).

**Example:** Spending depends on finance and finance depends on spending, so the cycle is reported naming both modules and the edges that close it.

**Checked by:** `litespec model check` reports `module-dependency-invalid` for a module dependency cycle.

### Publish Each Integration Event from One Module

**ID:** `litespec/core/model#publish-each-integration-event-from-one-module`

**Strength:** required

**Applies to:** artifact: record:integration-event

**Statement:** An integration event MUST have exactly one publishing module.

**Rationale:** An event with two publishers has two writers of one fact: readers cannot tell which module states it. One publishing module gives the fact one home, and every other module reads it through the event. The publishers follow from the records: the use cases whose `raises` holds the domain event the integration event `translates`.

**Example:** `integration-event/expense-submitted` translates `domain-event/expense.submitted`, which only spending use cases raise, so the spending module publishes it.

**Checked by:** `litespec model check` reports `event-publisher-invalid` for an integration event with a second publishing module.

### Hold Module Rules in Rule Folders

**ID:** `litespec/core/model#hold-module-rules-in-rule-folders`

**Strength:** required

**Applies to:** artifact: record:rule

**Statement:** A module-level rule MUST live in its own folder under `behavior/modules/<m>/rules/<name>/`, never in `module.yaml`.

**Rationale:** The module record declares the module; rules are behavior and live with Behavior. A rule written into `module.yaml` hides behavior in a declaration no rule check scans.

**Example:** A spending rule lives in its own folder under `behavior/modules/spending/rules/`, as `reimbursed-once/reimbursed-once.yaml`; the same rule written into `module.yaml` is reported.

**Checked by:** `litespec model check` reports `record-placement-invalid` for a rule in `module.yaml`.

### Follow the Authority Order

**ID:** `litespec/core/model#follow-the-authority-order`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A conflict MUST resolve toward the higher authority.

**Rationale:** Intent records outrank Behavior records: constraint over product, product over requirement, requirement over rule, rule over use case, use case over result, and result over scenario. A use case asking for behavior its rule forbids loses to the rule, and the contradiction is reported and resolved by updating the inconsistent records. Where Intent has spoken, the specification wins over code; where the specification is silent, an agent builds nothing, as the Authority section states. Goals, problems, features, flows, actors, research, and prose carry no authority, so none outranks the ladder. Only an explicit product `excludes` overrides a requirement.

**Reviewed for:** When records conflict, does the decision name the higher record that won? Look at the conflict decisions under `.litespec/intent/decisions/`, in the order this page states.

### Name What Every Decision Decides

**ID:** `litespec/core/model#name-what-every-decision-decides`

**Strength:** required

**Applies to:** artifact: record:decision

**Statement:** Every product decision MUST be named in `basedOn` by at least one record it settles.

**Rationale:** A decision that no settled record names governs nothing. Every link points from the lower or later record to the higher or earlier one, so the settled record names its decision in `basedOn` instead of the decision naming the record. A technical decision governs the code that follows it instead.

**Checked by:** `litespec model check` reports `decision-decides-missing` for a product decision that no settled record names in `basedOn`.

### Record Each Technical Choice as a Decision

**ID:** `litespec/core/model#record-each-technical-choice-as-a-decision`

**Strength:** required

**Applies to:** artifact: record:decision

**Statement:** A technical choice of one of the six kinds that code follows and no LiteSpec provision makes MUST be a decision under `.litespec/implementation/decisions/` that at least one code element follows.

**Rationale:** Why a pattern, a library, or a configuration value was chosen is the first thing a maintainer asks before changing it. Written in a comment, the answer drifts and no tool finds it. Written as a decision, it carries its options, what speaks for and against each, and what would reopen it, and every code element shaped by it names it in a `follows` mark.

A choice a LiteSpec provision already makes needs no decision, because the provision is the record. The six kinds are provider, package, format, boundary, topology, and replacement, as the [decision records page](../reference/decision-records.md) states: a choice of no kind is not a decision. A choice of no kind states its reason in the class documentation, and the mark on the class names the record the class serves.

Technical decisions sit under Implementation because they name technologies and code, which Intent never holds, and they share the numbered sequence of product decisions, so all decisions share one identifier space. Settled by [decision/0040-decisions-only-for-six-kinds-of-choice](decision/0040-decisions-only-for-six-kinds-of-choice).

**Example:** Claimwick renders each receipt as a PDF file. `decision/0005-render-receipts-as-pdf` weighs a PDF library against an HTML-to-PDF browser, chooses the PDF library, and names what reopens the choice: receipts no longer needing a downloadable file. The decision carries `kind: package`. The receipt PDF renderer carries `/// <follows>decision/0005-render-receipts-as-pdf</follows>`.

**Checked by:** `litespec model check` reports `decision-unimplemented` (level S) for a technical decision that no code element follows. It reports `decision-kind-missing` (level G) for a technical decision with no `kind`, `decision-scope-invalid` (level S) for a technical decision whose followers all sit in one module and each already carry an `implements` mark, and `decision-home-drift` (level S) for a technical decision kept at a level its followers do not match. The check runs in the CLI only, with no analyzer rule: whether any code follows a decision is known only across every project, and one compilation sees one project.

### Keep Rules and Targets Out of Technical Decisions

**ID:** `litespec/core/model#keep-rules-and-targets-out-of-technical-decisions`

**Strength:** required

**Applies to:** artifact: record:decision

**Statement:** A technical decision MUST leave each rule the product obeys whatever its design to a constraint and each measurable target to a quality requirement, and name the ones it serves in `basedOn`.

**Rationale:** A technical decision chooses one way among alternatives, and a better alternative may replace it tomorrow. A rule such as keeping personal data in the European Union, or a target such as answering checkout within two seconds, does not change when the library does: overriding it needs a business, legal, or organizational change. Written inside a decision, the rule disappears the day the decision is replaced, and nobody holds it as the promise it is.

Kept in Intent, the rule and the target stand as the promises they are, the decision points up to them, and the chain runs unbroken from code through the decision to the constraint or requirement and the goal above it. A decision with no basis blocks at level G, like a product decision that no settled record names. A technical choice among equal options may serve no rule or target yet and still govern the code that follows it, but a product decision that settles no record governs nothing.

**Example:** Claimwick keeps each receipt for the tax file because `constraint/tax-receipt-retention` requires it, and answers submissions quickly because `requirement/approval-reminders.month-end-submission` sets the target. `decision/0005-render-receipts-as-pdf` names the constraint in `basedOn`, and `decision/0004-serve-receipts-behind-one-proxy` names the requirement in `basedOn`; neither the seven years nor the two seconds is written in a decision.

**Checked by:** `litespec model check` reports `decision-basis-missing` (level G) for a technical decision whose `basedOn` names nothing, and counts a quality requirement as fulfilled when a technical decision names it in `basedOn`.

### Resolve Every Page Link

**ID:** `litespec/core/model#resolve-every-page-link`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every Markdown link outside code in a record field, an explanation file, or a document MUST resolve to an existing file or to a written record identifier.

**Rationale:** A link that points at a moved file or an unknown identifier reads as navigation and leads nowhere. A link whose target is a record identifier cites that record: link checks resolve it against the model's identifiers, citation checks count it, and the book links it to the record. Prose cites a record by its identifier, never by its file path.

**Example:** The Claimwick receipt rule is cited as `[the receipt rule](rule/expense.approver-is-not-submitter)`.

**Checked by:** `litespec model check` reports `link-unknown` for a link that resolves to no file and no written record identifier.

### Name Records in Prose by Their Identifiers

**ID:** `litespec/core/model#name-records-in-prose-by-their-identifiers`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Prose in a record field, an explanation file, a document, or the `AGENTS.md` file at the consumer root MUST name every record by its identifier in backticks and every provision by the published identifier the carried release ships.

**Rationale:** A name in plain words, such as a decision number without its identifier, cannot be resolved: nothing tells whether the record still exists after a rename or a move. The identifier in backticks is the one form every check reads, so a stale name fails the check at the line that names it instead of sending the reader after a record nobody wrote.

**Example:** A waiver `exitWhen` reads "The gap closes when `decision/0005-render-receipts-as-pdf` lands." rather than "The gap closes when Decision 5 lands."

**Checked by:** `litespec model check` reports `reference-unknown` for a backticked identifier that names no written record and for a backticked provision the carried release does not ship.

### Declare Size and Stacks

**ID:** `litespec/core/model#declare-size-and-stacks`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A project MUST declare its size and stacks.

**Rationale:** Size decides which record types and test projects the checks require, while stacks decide which technology pages and checks apply. Without the declaration, the builder cannot tell a missing record from one the project never needed.

**Example:** The running Claimwick example declares large with a server shape and two stacks.

**Checked by:** `litespec model check` reports `size-type-missing` and `record-invalid` for a project missing its declaration or a required type.

### Compute Proof Statuses in the Builder

**ID:** `litespec/core/model#compute-proof-statuses-in-the-builder`

**Strength:** required

**Applies to:** artifact: any; layer: implementation; trace: yes

**Statement:** Coded, verified, tested, exercised, stale, flaky, and reachable MUST come from the builder, never from handwriting.

**Rationale:** Handwritten state drifts: a record marked coded stays marked after its handler is deleted. Computed state cannot drift, because the builder re-derives it from handlers, routes, scenarios, and test runs on every run.

A use case reads coded when a handler, reaction, job, or endpoint marked with it carries it out, verified when every scenario has a passing proof through the public boundary that is not stale, and stale when its run commit is behind HEAD with a changed path outside the record and documentation paths, or when its run commit is unknown. A run records its commit. A requirement reads verified when everything fulfilling it is verified and its measure's check passes. A record reads tested when a passing test that asserts something links to it. A flow step reads exercised when a passing proof of a use case, reaction, or job serves it. A proof reads flaky when its test both passed and failed within the runs read.

Nothing above Implementation is computed from code: derivation from code into any other record is forbidden, and code-computed facts live in the book and the model output. Settled by [decision/0017-proof-that-holds](decision/0017-proof-that-holds) on the evidence in [research/lower-layers-practice](research/lower-layers-practice).

**Example:** A use case reads coded when its handler exists.

**Checked by:** `litespec model check` reports `use-case-unimplemented`, `proves-unreached`, and `refusal-unimplemented` for a use case whose handler, proof, or refusal code is missing, and `proof-invalid` (level S) for a test that both passed and failed within the runs read.

### Report Unverified Use Cases

**ID:** `litespec/core/model#report-unverified-use-cases`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A use case whose scenarios are not all proved MUST be reported as `use-case-unverified`.

**Rationale:** An unverified use case is a promise with no proof, so the finding blocks until every scenario has a passing proof or a waiver parks the use case. The waiver keeps daily work loose where proof cannot land yet, and the release stays honest because no unverified use case passes the check unparked.

**Example:** Claimwick adds `use-case/spending.export-expenses` with two scenarios and one passing proof: `litespec model check` reports `use-case-unverified` (level S) for it and exits 1, until a passing proof or a waiver covers it.

**Checked by:** `litespec model check` reports `use-case-unverified` (level S) for a use case whose scenarios are not all proved.

### Compute Completion in the Builder

**ID:** `litespec/core/model#compute-completion-in-the-builder`

**Strength:** required

**Applies to:** artifact: record:use-case, record:requirement, record:flow, record:feature, record:goal; layer: implementation; trace: yes

**Statement:** The builder MUST compute one completion stage for every requirement, use case, flow, feature and goal, and write it as `completion` with `stage`, `proof`, `heldBy`, `parkedBy` and `counts`, with a parent only as far as its weakest in-scope child, `heldBy` naming at most ten in-scope children at that stage, and `counts` holding `inScope`, `parked` and `total`.

**Rationale:** Completion is a fact the builder can count from fulfillment, code, and proof, so no record carries a shorthand saying how far it is. One ladder reads the same everywhere: `unfulfilled` when no Behavior record fulfills a record, `specified` when it is fulfilled and no code is found, `coded` when code is found for every in-scope child, and `verified` when every in-scope child is verified with fresh proof. When the model holds no test run, a coded record stays `coded` with proof `unknown`, so proof unknown never reads as a failure. A requirement a waiver parks stays out of the stage and counts as parked: `heldBy` lists only in-scope records holding the stage back, and `parkedBy` lists parked records with the waiver that parks each and its `expiresOn`. The book shows "verified, 12 of 14; 2 parked", with a parked list naming each waiver and expiry. A goal's completion shows delivery only; its outcome reads `not measured` until the goal records `observations`.

**Example:** In Claimwick, `feature/expense-submission` shows `coded` with proof `unknown` before any test run, held by the one requirement with no fulfiller; after a passing run proves every fulfiller, the same feature shows `verified` with no held-by entries.

**Reviewed for:** Which features read below `verified`, and which children hold each one back? Look at `completion` on each feature in the model.

### Record Each Screen's Captures from the Gallery

**ID:** `litespec/core/model#record-each-screens-captures-from-the-gallery`

**Strength:** required

**Applies to:** artifact: record:screen

**Statement:** The builder MUST record each screen's captures from the gallery folders and file-name pattern the project file names, as `captures` with `path`, `theme`, `width` and `capturedOn`, and its `captureState` as `captured`, `route-not-captured` or `no-route`.

**Rationale:** A reader judging a screen needs to see it, and a capture named by its route, theme, and width is the copy the built screen makes. The route slug names the file the same way everywhere: the leading slash dropped, segments joined with a hyphen, a parameter reduced to its name, and the root route named `home`. A missing gallery folder captures nothing and fails nothing, so a project with no photography yet still builds its model.

**Example:** The Claimwick web gallery holds `expenses--light--1440.png`, so `screen/expenses.list` at `/expenses` shows that capture at theme `light` and width 1440 with `captureState` `captured`; a screen with no route shows `no-route`.

**Reviewed for:** Which screens with a route have no capture? Look at `captureState` `route-not-captured` on screen records in the model.

### Mark Every Check with Its Level

**ID:** `litespec/core/model#mark-every-check-with-its-level`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every check MUST carry one of the levels G, S or R, where `litespec model check` blocks on G findings and on S findings whose reader is qualified, and only `litespec model review` reports R findings, without blocking.

**Rationale:** The level tells a reader what a finding rests on: G rests on records only, S rests on code read through a stack reader, and R rests on prose quality a person weighs. `litespec model check` prints the level beside each finding, so a reader tells what each finding rests on at a glance. A missing required link is level G.

**Checked by:** `litespec model check` reports `reader-missing` (level S) for a frontend whose reader is missing or failing, prints the level beside each finding, prints `inspection incomplete: reader for <frontend> missing, <N> findings not blocking` as its last line when any reader was missing with one clause per frontend, and exits with code 3 when no finding blocks but the inspection was incomplete. `litespec model check --release` blocks on `reader-missing` for a claimed layer. `litespec model review` reports the level R findings and exits zero.

### Block Every Finding That Predicts a Runtime Failure

**ID:** `litespec/core/model#block-every-finding-that-predicts-a-runtime-failure`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A check MUST block when ignoring its finding fails at runtime; prose quality only a person can weigh is reported by `litespec model review` at level R and never blocks.

**Rationale:** A finding nobody must act on teaches its readers to ignore findings. When a finding predicts a refused call, a crash, or a wrong result, moving on ships the failure: a command whose idempotency states no replay ignores its report, and a repeat then fails instead of returning the first result. Blocking the finding holds the run until the record or the code answers what the repeat brings. Level R stays for prose quality no caller meets at runtime, which a person weighs through `litespec model review`.

**Example:** Claimwick's reimburse-expense command states `replay: refuses` with the state it checks in `recognizedBy`. A command stating no replay is reported and blocks until the replay is stated.

**Reviewed for:** Does every check block when ignoring its finding fails at runtime, and does level R hold only prose quality? Read each finding code's meaning in `FindingCodes.cs` against the runtime-failure test: a refused call, a crash, or a wrong result.

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

**Checked by:** `litespec model check` reports `mark-folder-drift` (level S) for a handler, endpoint, event handler, job host or aggregate root whose mark names a record outside its module and folder, and `code-placement-drift` (level S) for a code element, test, or folder outside the code folder its record's placement names.

### Name the Lowest Record Each Test Proves

**ID:** `litespec/core/model#name-the-lowest-record-each-test-proves`

**Strength:** required

**Applies to:** artifact: test

**Statement:** Every test MUST name in a `proves` mark the lowest record it verifies, from the kinds the marks table on this page lists for a test.

**Rationale:** A test outside the record tree is either proof of something the specification forgot to state or a test that verifies nothing anyone promised. Naming the lowest record it verifies places each test: a scenario or rule for behavior, a quality requirement for how well the product behaves, a technical decision for a technical choice, an external system or integration event for an agreement with the outside, a provision for a structural rule the code must keep, and a project convention for a standing rule the code must keep.

A test that verifies a choice no record states shows a missing record, which is written at the level where the choice lives, as safety-critical standards resolve derived requirements; a test with no record above it is a finding to resolve by writing the missing record or reporting the gap. It is deleted only when a reviewer establishes that the behavior it protects is obsolete, redundant or wrong, and the pull request says so. Proof stays layered: only scenario and rule proofs verify a use case, and a passing test that asserts something links to a quality requirement, a technical decision, an external system, an integration event, or a provision marks that record tested, so "the adapter is right" never reads as "the promise is kept". A background use case is verified through the passing proofs of the use cases it serves. A browser success proof asserts the result sentence or the scenario Then text.

**Example:** In Claimwick, the submit-expense acceptance test proves `scenario/submit-expense.submitting-with-a-receipt-succeeds`; a load test of month-end submissions proves `requirement/approval-reminders.month-end-submission`; a receipt-rendering test proves `decision/0005-render-receipts-as-pdf`; and the layering test that Domain references no persistence package proves `litespec/backend/architecture#point-dependencies-inward`.

**Checked by:** `litespec model check` reports `mark-missing` and `mark-unknown` (level S) for a test that names no record or a record of a kind a test does not take.

### Implement Each Mapped Behavior Record Behind a Mark

**ID:** `litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark`

**Strength:** required

**Applies to:** artifact: record:aggregate, record:reaction, record:job, record:external-system, record:use-case; layer: implementation; trace: yes

**Statement:** Every entity aggregate, every reaction, job, and external system, and every use case reached through a `browser` or `api` entry point, MUST be named by a mark on the code element the marks table gives it.

**Rationale:** A mark points up, so a check reading marks alone finds code nobody recorded but misses a record nobody built. Checking each record that the marks table maps to code from the top closes the loop in the other direction: an entity aggregate with no root, a reaction with no event handler, a job with no host, an external system with no port, and a use case people reach with no endpoint. A use case with no screen calling it blocks at level S: the link between a use case and the screens that call it always blocks.

**Example:** `reaction/spending.send-approver-reminder-on-expense-submitted` has no event handler carrying its mark, so the check reports `reaction-unimplemented` and blocks.

**Checked by:** `litespec model check` reports `aggregate-unimplemented`, `reaction-unimplemented`, `job-unimplemented`, `external-system-unimplemented`, and `use-case-endpoint-missing` (level S) for a record no mark names, where `aggregate-unimplemented` fires only for an entity aggregate, and `use-case-unreached` (level S) for a browser use case no screen calls; a request pipeline handler carrying the use case mark serves as its endpoint and needs no screen call.

### Organize the Book by the Record Layers

**ID:** `litespec/core/model#organize-the-book-by-the-record-layers`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The book MUST outline a project by the four record layers, with the governance records beside them, and place each record under the record its folder and links name, or at the top of its own layer when the product parents it.

**Rationale:** The book is how people read the model, so it shows the model's own hierarchy. A reader who learns Intent, Behavior, Implementation, and Proof in the book reads the same structure in the docs folder, in the model output, and in every other LiteSpec project. An outline of the book's own invention is a second hierarchy to learn, and it drifts from the one the records keep. Settled by [decision/0002-parent-for-every-record](../intent/decisions/0002-parent-for-every-record/0002-parent-for-every-record.md) on the evidence in [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence/behavior-containment-evidence.md), [research/intent-hierarchy-evidence](../intent/research/intent-hierarchy-evidence/intent-hierarchy-evidence.md), [research/book-outline-evidence](../intent/research/book-outline-evidence/book-outline-evidence.md), and [research/established-traceability-practice](../intent/research/established-traceability-practice/established-traceability-practice.md). Settled by [decision/0008-book-follows-record-layers](../intent/decisions/0008-book-follows-record-layers/0008-book-follows-record-layers.md) on the evidence in [research/book-outline-evidence](../intent/research/book-outline-evidence/book-outline-evidence.md), [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence/behavior-containment-evidence.md), and [research/check-coverage-evidence](../intent/research/check-coverage-evidence/check-coverage-evidence.md). Settled by [decision/0088-serve-the-book-live-and-export-it](decision/0088-serve-the-book-live-and-export-it).

**Example:** In the Claimwick product, `use-case/spending.submit-expense` sits under Behavior, its module and its aggregate; its results under it; each scenario under the result it shows; and the handler that carries its mark under Implementation.

**Reviewed for:** Which book headings or groupings are not a record layer, a record type, or a record the identifier names? Look at the book's outline beside this page's record type table.

### Name Book Labels with LiteSpec Terms

**ID:** `litespec/core/model#name-book-labels-with-litespec-terms`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every label in the book MUST be a record type, a field name, a computed state, a glossary term, or the reads-as phrase the Links table states for a connection.

**Rationale:** LiteSpec already names every concept once, for people and agents alike. A friendlier synonym in the book, such as promise for requirement, is a second word for one concept: the reader learns it, then meets the real word in the records and the checks, and has to translate. Settled by [decision/0008-book-follows-record-layers](../intent/decisions/0008-book-follows-record-layers/0008-book-follows-record-layers.md) on the evidence in [research/book-outline-evidence](../intent/research/book-outline-evidence/book-outline-evidence.md), [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence/behavior-containment-evidence.md), and [research/check-coverage-evidence](../intent/research/check-coverage-evidence/check-coverage-evidence.md).

**Example:** A use case page heads its results "Results" and its scenarios "Scenarios", never "Endings" and "Examples".

**Reviewed for:** Does every book label use a LiteSpec term? Look at the book beside the glossary and the Links table: each label is a record type, a field name, a computed state, a glossary term, or a reads-as phrase.

### Explain the Book from LiteSpec Definitions

**ID:** `litespec/core/model#explain-the-book-from-litespec-definitions`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every explanation the book shows MUST come from the glossary definition of the term or from the question its record type answers on this page.

**Rationale:** The book explains itself so nobody needs a learning curve, and the explanation stays true only when it has one source. An explanation written for the book drifts from the definition the records follow. Settled by [decision/0008-book-follows-record-layers](../intent/decisions/0008-book-follows-record-layers/0008-book-follows-record-layers.md) on the evidence in [research/book-outline-evidence](../intent/research/book-outline-evidence/book-outline-evidence.md), [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence/behavior-containment-evidence.md), and [research/check-coverage-evidence](../intent/research/check-coverage-evidence/check-coverage-evidence.md). Settled by [decision/0090-make-the-book-an-explorer](decision/0090-make-the-book-an-explorer): the book states a definition on hover and in the lexicon, and shows no teaching text beside each use.

**Example:** The book explains a waiver with the glossary definition of Waiver.

**Reviewed for:** Does every book explanation come from a LiteSpec definition? Look at the book's explanations beside the glossary and the record type table.

### Show Only Computed Judgments in the Book

**ID:** `litespec/core/model#show-only-computed-judgments-in-the-book`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every status, count, order and finding the book shows MUST be one the builder or a check computes.

**Rationale:** The book is a view of the model, not a second authority. A judgment only the book makes, such as its own idea of what to do next, cannot be checked, cannot fail a build, and cannot be found by an agent reading the model output. Settled by [decision/0008-book-follows-record-layers](../intent/decisions/0008-book-follows-record-layers/0008-book-follows-record-layers.md) on the evidence in [research/book-outline-evidence](../intent/research/book-outline-evidence/book-outline-evidence.md), [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence/behavior-containment-evidence.md), and [research/check-coverage-evidence](../intent/research/check-coverage-evidence/check-coverage-evidence.md).

**Example:** The book orders features by the completion the builder computes.

**Reviewed for:** Does every status, count, order, and finding in the book come from the builder or a check? Look at the book beside the model output.

### Add a Missing Fact to LiteSpec Before the Book Shows It

**ID:** `litespec/core/model#add-a-missing-fact-to-litespec-before-the-book-shows-it`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A fact the book needs that LiteSpec does not define MUST become a record field, a computed status, or a check in LiteSpec before the book shows it.

**Rationale:** Rendering the book is a probe: what it cannot show is a gap in the specification. Closing the gap in LiteSpec gives every project the fact, checked; closing it in the book gives one view an unchecked fact. Settled by [decision/0008-book-follows-record-layers](../intent/decisions/0008-book-follows-record-layers/0008-book-follows-record-layers.md) on the evidence in [research/book-outline-evidence](../intent/research/book-outline-evidence/book-outline-evidence.md), [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence/behavior-containment-evidence.md), and [research/check-coverage-evidence](../intent/research/check-coverage-evidence/check-coverage-evidence.md).

**Example:** The book needs to say that a refusal has no scenario, so LiteSpec gains a check for it, and the book shows that check's findings.

**Reviewed for:** Does every fact the book shows come from LiteSpec? Look at the book beside the model output and the finding codes: each fact is a record field, a computed status, or a check.

### Point Every Book Entry to Its Source

**ID:** `litespec/core/model#point-every-book-entry-to-its-source`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every record, code element, test and finding the book shows MUST link to the file and line it comes from.

**Rationale:** The book is where a reader notices what to change, and the source is where the change is made. A link to the file and line makes the next step one click, and it shows that every entry comes from a written record or from code, not from the book. Settled by [decision/0008-book-follows-record-layers](../intent/decisions/0008-book-follows-record-layers/0008-book-follows-record-layers.md) on the evidence in [research/book-outline-evidence](../intent/research/book-outline-evidence/book-outline-evidence.md), [research/behavior-containment-evidence](../intent/research/behavior-containment-evidence/behavior-containment-evidence.md), and [research/check-coverage-evidence](../intent/research/check-coverage-evidence/check-coverage-evidence.md).

**Example:** The submit-expense use case page links to `.litespec/behavior/modules/spending/aggregates/expenses/use-cases/submit-expense/submit-expense.yaml` at its first line, and its handler to the handler file at the class.

**Reviewed for:** Does every book entry link to its file and line? Look at the book's record, code, test, and finding entries.

### Hold Every Record File and Frontmatter Block to the Schema

**ID:** `litespec/core/model#hold-every-record-file-and-frontmatter-block-to-the-schema`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every record file and every document frontmatter block MUST satisfy the record schema for its declared type.

**Rationale:** The record schema owns the field list of every record type and page style. A record file is one YAML mapping with its entries in `records`, holding no Markdown and no frontmatter fences. A document frontmatter block is YAML between two lines that hold only `---`, holding only the page style and its common fields. This page states the record types and their homes.

**Example:** A use-case record file opens with its identity fields.

```yaml
type: use-case
id: use-case/spending.submit-expense
title: Submit Expense
```

**Checked by:** `litespec model check` reports `record-invalid` for each record file and document frontmatter block that fails the record schema.

### Report One Shape in Schema Errors

**ID:** `litespec/core/model#report-one-shape-in-schema-errors`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A schema error on a value matching no allowed shape MUST report only the errors of the one shape its discriminating field selects, falling back to the closest shape by error count, with the full list behind the `--verbose` flag.

**Rationale:** A record declares its shape in `type`, so listing every shape the value is not wastes the reader's time: the cause sits in one shape's errors, buried under missing-property notes from shapes that never applied. Reporting only the selected shape's errors shows the cause first. The discriminating field is the `type` field, or the field the value carries that only one shape declares. The closest shape by error count is the best guess when neither selects one, and `--verbose` keeps the full list for the case the guess is wrong.

**Example:** A rule record with a misspelled field reports that field under its rule shape, not under every other shape in the schema.

**Checked by:** `litespec model check` reports `record-invalid` naming only the errors of the selected shape, and the full shape list with `--verbose`.

### Name the Field to Use Instead in Schema Errors

**ID:** `litespec/core/model#name-the-field-to-use-instead-in-schema-errors`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A schema error on a rejected field or a link in the wrong field MUST name the field to use instead.

**Rationale:** Some fields read naturally but belong elsewhere: `heldBy` on a permission, `required` on an output field, a notification under a step's `carries`. The record schema lists each rejected field with the field to use instead, and the link checks name the field for a misplaced link, so the error moves the author forward instead of leaving them to guess which of the many shapes they broke.

**Example:** `heldBy` on a permission reports that the permission names its roles in `grantedTo`.

**Checked by:** `litespec model check` reports `record-invalid` naming the field to use instead.

### Classify Every Specification File

**ID:** `litespec/core/model#classify-every-specification-file`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every file under the consumer documentation root MUST be a record file, an explanation file beside its record file, or a document, or sit under a declared unstructured path.

**Rationale:** A file the validator leaves unclassified is skipped, and a skipped file looks sound: its errors stay hidden. Requiring every file to be a record file, an explanation, or a document, or to sit under a declared unstructured path, leaves no page unchecked. A directory index that owns no record uses the index style, and a `README.md` with no frontmatter is a document.

**Reviewed for:** a file under the documentation root that is neither a record file, an explanation beside its record file, nor a document, and sits under no declared unstructured path.

### Start Each Explanation File with Its Title

**ID:** `litespec/core/model#start-each-explanation-file-with-its-title`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An explanation file MUST open with one H1 title and section its content with H2 sections.

**Rationale:** One shape puts the title and each section in the same place on every explanation. The H1 carries the record title, and each H2 carries one section from the table on this page or a free section; Notes take free `###` headings under their H2.

**Reviewed for:** Does every explanation file open with its H1 title and section content under H2? Look at the explanation files beside the changed record files.

### Derive a Computed Fact Instead of Restating It

**ID:** `litespec/core/model#derive-a-computed-fact-instead-of-restating-it`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A specification MUST name the source or the command that produces any count, census, or membership list derived between records.

**Rationale:** A restated number is right until the next unrelated edit changes the records it counts, and nothing reports when it goes stale. Naming the source or the command gives every reader the current figure instead of the figure from commit day. Derivation between records is allowed: generic refusals derive from `access`, `input`, and the knowledge the use case reads, and computed completion appears in the book and the model output only.

**Example:** A chapter states where its numbers come from rather than copying them.

```markdown
The figures below render from the retention schedule at build time.
```

**Reviewed for:** Does every count, census, and membership list name its source or command? Look at the numbers the change adds.

### Keep Code Out of Intent and Behavior Fields

**ID:** `litespec/core/model#keep-code-out-of-intent-and-behavior-fields`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A project MUST fill no field on an Intent or Behavior record from code.

**Rationale:** Code implements Behavior, so a field filled from an implementation restates what already ships instead of stating the promise under review. A fact learned from the world enters Intent as research or a constraint, cited and dated, and a need the upper record layer does not state becomes a backlog item outside the repository. Derivation between records stays allowed.

**Example:** A positivity guard found in a handler becomes a backlog item for a rule, while the code stays untouched until the rule lands.

**Reviewed for:** Does any Intent or Behavior field copy a value from code? Look at the fields the change fills, beside the implementation.

### Scaffold Pages and Reserve Identifiers

**ID:** `litespec/core/model#scaffold-pages-and-reserve-identifiers`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An author MUST scaffold a new page with `litespec new <type> <id>`, which reserves its identifier.

**Rationale:** Two agents working in parallel mint the same identifier when each writes a file by hand, and the collision surfaces only at merge time. Scaffolding reserves the identifier before any prose exists, so the second author learns the clash at creation and picks another name. For an article type the command also writes the explanation file, and for a document style it writes the document.

**Example:** `litespec new reaction spending.send-receipt-reminder-on-expense-submitted` creates the reaction record file with its reserved identifier.

**Reviewed for:** Does every new page carry a reserved identifier? Look for pages created without `litespec new`.

### Rewrite Every Link on a Rename

**ID:** `litespec/core/model#rewrite-every-link-on-a-rename`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An author MUST rename a record identifier with `litespec rename <old> <new>`, which rewrites every link and keeps the record at its level and in its owner's folder unless `--move` names another owner.

**Rationale:** An identifier rename touches the record file, its explanation file, and every record that points at it. Rewriting every link in the same command leaves no citation pointing at the old name, so the repository stays resolvable at every commit. When the file name follows the identifier, both files move together. A plain rename changes no owner, so a record never changes level by accident, and a move to another owner is an explicit choice. Proposed by [decision/0060-put-child-records-in-typed-folders](../intent/decisions/0060-put-child-records-in-typed-folders/0060-put-child-records-in-typed-folders.md).

**Example:** `litespec rename use-case/spending.close-run use-case/spending.complete-run` updates the page and every citation together.

**Checked by:** `litespec rename` leaves no citation of the old identifier, and `litespec-maintain validate` emits no `ID_UNKNOWN_REFERENCE` diagnostic.

### Validate a Single Page Before Committing

**ID:** `litespec/core/model#validate-a-single-page-before-committing`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An author MUST validate a single page before committing it.

**Rationale:** Full validation walks the whole repository, which grows slow exactly when many authors commit often. A single-page check reports shape and link errors for the page at hand in seconds, so each commit arrives already sound.

**Example:** The author validates the touched page, fixes the reported diagnostics, and commits only when the page passes.

**Reviewed for:** Does the change report show each touched page passing single-page validation before commit?

### Generate Record-Determined Code from the Records

**ID:** `litespec/core/model#generate-record-determined-code-from-the-records`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Code that the records fully determine MUST come from `litespec generate`, written again in the same change as the record and never typed by hand.

**Rationale:** A typed copy of a record fact drifts the first time the record changes and the copy does not: a refusal code typed into an exception and into the web, a permission name typed into a policy, an event decision typed into startup code. A generator removes the typing, and the drift check removes the drift. The records stay the one source, and the generated files carry no fact of their own.

**Example:** The spending refusal codes live in a generated class, with one constant per refusal holding its code. When the receipt rule gains a refusal, the same change runs `litespec generate` and the exceptions read the new constant.

**Checked by:** `litespec model check` reports `generated-drift` for each file `litespec generate` writes that is missing or differs from what the records render now.

### State What Happens in Every Behavior Statement

**ID:** `litespec/core/model#state-what-happens-in-every-behavior-statement`

**Strength:** advisory

**Applies to:** artifact: any

**Statement:** A rule statement, scenario step, result meaning, and result sentence SHOULD avoid words that hide behavior.

**Deviation:** A quoted name, an interface label, or a cited source keeps its fixed wording.

**Rationale:** The vague words are handle, handles, handled, process, processes, manage, manages, various, etc., appropriate, appropriately, properly, as needed, and so on: each says that something happens without saying what. A reader cannot check a statement that hides its behavior, so each statement names the act in words a test could assert.

**Example:** A scenario step reads "The member borrows the copy" rather than "The member handles the copy".

**Reviewed for:** Does any rule statement, scenario step, result meaning, or result sentence hide its behavior behind a vague word? Look at the statements the change adds.

### Cite Reference Data Instead of Literal Values

**ID:** `litespec/core/model#cite-reference-data-instead-of-literal-values`

**Strength:** advisory

**Applies to:** artifact: record:rule

**Statement:** A rule SHOULD cite the reference-data table holding a value instead of stating the value in its statement.

**Deviation:** A value held by no table stays literal until a table holds it.

**Rationale:** A value written into prose drifts: each statement holding it needs its own edit when the value changes, and no check sees which statements hold it. One table with dated rows keeps each value once with its source, and the model checks that every cited table exists. Settled by [decision/0098-reference-data-tables](decision/0098-reference-data-tables).

**Example:** The approval rule cites `reference-data/spending.decision-target` instead of stating five business days.

**Reviewed for:** Does any rule statement state a value a reference-data table holds? Check the rule statements beside the reference-data tables.

## Conventions

### Render Generated Regions from the Model

**ID:** `litespec/core/model#render-generated-regions-from-the-model`

**Strength:** default

**Applies to:** artifact: any

**Default:** A generated region renders only facts derived from records, and a `decision-index` or `waiver-index` region also renders the computed follower facts its rows name.

**Replacement:** Any other code-computed fact lives in the book and the model output.

**Rationale:** A generated region sits inside a record page, so everything in it reads with record authority. Facts computed from code, such as coded and verified counts, describe the world rather than the promise, and rendering them beside records would lend them authority they do not hold. The book and the model output carry code-computed facts where readers meet them as computed. An index region is the exception: it lists the records beside it, so each row names the computed followers, concerns, and flags the reader needs to read the list. Settled by [decision/0072-generate-the-links-table-from-the-schema](decision/0072-generate-the-links-table-from-the-schema).

**Reviewed for:** Does any generated region outside an index region render code-computed facts? Look inside the generated fences beside the book and the model output.
