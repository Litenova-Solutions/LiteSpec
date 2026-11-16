# Document Templates

These files are starting points for consumer records. `litespec init` copies the project setup files to their targets, writes the six interface quality requirements from `requirements/` to `intent/requirements/`, and writes the local tool manifest, while `litespec new` scaffolds a record file or a document from its template. Copy only the record files the current work needs, replace every example value with the project's own before committing, and delete any optional line left unused. A section whose starter text opens with `Optional.` may be removed when it has nothing to say; every other section is required. `litespec init` fills `__PROJECT__` in `.litespec/project.yaml` and the `agent-protocol` region in `AGENTS.md`, and lists the placeholders it leaves behind, while `litespec model check` rejects a project file still holding one. Each record template copies one record from the Claimwick example, so it carries that record's values, and `litespec model check` reports any scaffold that keeps a copied text or number.

A record file holds its record in YAML, with the entries under `records`. A named record gets its own folder named for it, as `<name>/<name>.yaml`, and its explanation file `<name>.md` sits beside the record file in that folder. Every record type with a fixed-section explanation has its own `.md` template, listed below. A document holds no record. Each feature requirement is a named record in its own folder inside its feature's requirements folder, and each product requirement is a named record in its own folder under `intent/requirements/`. Rules and domain events live on an aggregate file, results live on a use-case file, and scenarios live on their use-case, reaction, or job file. Permissions live on their module's permissions file, refusals on their module's refusals file, and settings on a setting-set file. Each entry point is one record in its own folder, with its `screens/` folder beside the entry-point file. Terms live in the glossary at `.litespec/glossary.yaml` rather than on a file.

## Project Setup

| Template | Target | Purpose |
|:---|:---|:---|
| `project.yaml` | `.litespec/project.yaml` | Sets the size, shape, stacks, reviewed LiteSpec release, paths, and selected extensions. |
| `project-agents.md` | `AGENTS.md` | Provides the short consumer agent entry point: the generated `agent-protocol` region above the project-owned `## Project Commands` section. |
| `glossary.yaml` | `.litespec/glossary.yaml` | Holds the Term entries that prose is checked against, with layers, aliases, rejected words, and mannered terms. |
| `example-cast.md` | `.litespec/example-cast.md` | Sets the people, place, dates, and amounts every `Example` section draws from. |

## Page Records

| Template | Target | Purpose |
|:---|:---|:---|
| `product.yaml` | `.litespec/product.yaml` | Defines the product boundary: the actors it addresses, what it excludes, and the brand every screen keeps. |
| `product.md` | `.litespec/product.md` | Explains the product beside its record file. |
| `goal.yaml` | `.litespec/intent/goals/<name>/<name>.yaml` | States one measurable outcome the product serves, with the observations that show it. |
| `goal.md` | `.litespec/intent/goals/<name>/<name>.md` | Explains the goal beside its record file. |
| `vision.yaml` | `.litespec/intent/visions/<name>/<name>.yaml` | States where the product is heading beyond what it promises now. |
| `vision.md` | `.litespec/intent/visions/<name>/<name>.md` | Explains the vision beside its record file. |
| `constraint.yaml` | `.litespec/intent/constraints/<name>/<name>.yaml` | Records one outside rule and whether it applies. |
| `constraint.md` | `.litespec/intent/constraints/<name>/<name>.md` | Explains the constraint beside its record file. |
| `research.yaml` | `.litespec/intent/research/<name>/<name>.yaml` | Records one outside fact that informs a choice. |
| `research.md` | `.litespec/intent/research/<name>/<name>.md` | Explains the research record beside its record file. |
| `decision.yaml` | `.litespec/intent/decisions/<number>-<name>/<number>-<name>.yaml` | Records one dated choice with its options and evidence. A technical decision lives under `.litespec/implementation/decisions/` instead. |
| `decision.md` | `.litespec/intent/decisions/<number>-<name>/<number>-<name>.md` | Explains the decision beside its record file. |
| `problem.yaml` | `.litespec/intent/problems/<name>/<name>.yaml` | Records one pain someone has today. |
| `problem.md` | `.litespec/intent/problems/<name>/<name>.md` | Explains the problem beside its record file. |
| `feature.yaml` | `.litespec/intent/features/<name>/<name>.yaml` | Records one standalone offer with the problems it solves and the goals it advances. Its requirements sit in their own folders beneath it. |
| `feature.md` | `.litespec/intent/features/<name>/<name>.md` | Explains the feature beside its record file. |
| `requirement.yaml` | `.litespec/intent/features/<feature>/requirements/<name>/<name>.yaml` | Records one commitment a feature makes, with its kind and statement. |
| `requirement.yaml` | `.litespec/intent/requirements/<name>/<name>.yaml` | Records one product commitment, citing the constraint, goal or problem behind it. |
| `requirements` | `.litespec/intent/requirements/<name>/<name>.yaml` | Ships the six interface quality requirements `litespec init` writes: text contrast, control boundary, focus indicator, chart series, reflow, and target size. |
| `requirement.md` | `.litespec/intent/features/<feature>/requirements/<name>/<name>.md` | Explains the requirement beside its record file, when its statement needs prose. |
| `requirement.md` | `.litespec/intent/requirements/<name>/<name>.md` | Explains the product requirement beside its record file, when its statement needs prose. |
| `actor.yaml` | `.litespec/intent/actors/<name>/<name>.yaml` | Records one person or organization that takes part in the product. |
| `actor.md` | `.litespec/intent/actors/<name>/<name>.md` | Explains the actor beside its record file. |
| `role.yaml` | `.litespec/intent/roles/<name>/<name>.yaml` | Records one named set of permissions at a scope. |
| `role.md` | `.litespec/intent/roles/<name>/<name>.md` | Explains the role beside its record file. |
| `external-system.yaml` | `.litespec/behavior/modules/<module>/external-systems/<name>/<name>.yaml` | Records one outside system called from one module: its typed operations with their unknown-outcome handling, the data it processes, and its erasure behavior. |
| `external-system.yaml` | `.litespec/behavior/external-systems/<name>/<name>.yaml` | Records one outside system called from several modules: its typed operations with their unknown-outcome handling, the data it processes, and its erasure behavior. |
| `external-system.md` | `.litespec/behavior/modules/<module>/external-systems/<name>/<name>.md` | Explains the external system beside its record file. |
| `external-system.md` | `.litespec/behavior/external-systems/<name>/<name>.md` | Explains the external system beside its record file. |
| `flow.yaml` | `.litespec/intent/flows/<name>/<name>.yaml` | Records the ordered steps an actor takes toward the goal it serves or the problem or constraint it answers. |
| `flow.md` | `.litespec/intent/flows/<name>/<name>.md` | Explains the flow beside its record file. |
| `module.yaml` | `.litespec/behavior/modules/<module>/module.yaml` | Declares one product area; Behavior folders follow it. |
| `module.md` | `.litespec/behavior/modules/<module>/module.md` | Explains the module beside its module file. |
| `aggregate.yaml` | `.litespec/behavior/modules/<module>/aggregates/<aggregate>/aggregate.yaml` | Defines one aggregate, with its states, data classes, and its rule and domain-event entries. |
| `aggregate.md` | `.litespec/behavior/modules/<module>/aggregates/<aggregate>/aggregate.md` | Explains the aggregate beside its aggregate file. |
| `store.yaml` | `.litespec/behavior/modules/<module>/stores/<name>/<name>.yaml` | Defines one store, with its owner, data and data classes. |
| `store.md` | `.litespec/behavior/modules/<module>/stores/<name>/<name>.md` | Explains the store beside its record file. |
| `use-case-command.yaml` | `.litespec/behavior/modules/<module>/aggregates/<aggregate>/use-cases/<use-case>/<use-case>.yaml` | Defines one command that changes data, with its input, writes, consistency, idempotency, result and scenario entries. |
| `use-case-query.yaml` | `.litespec/behavior/modules/<module>/aggregates/<aggregate>/use-cases/<use-case>/<use-case>.yaml` | Defines one query that returns data, with its output, freshness, paging and scenario entries. |
| `use-case.md` | `.litespec/behavior/modules/<module>/aggregates/<aggregate>/use-cases/<use-case>/<use-case>.md` | Explains the use case beside its record file, a command or a query. |
| `reaction.yaml` | `.litespec/behavior/modules/<module>/reactions/<name>/<name>.yaml` | Defines one reaction a domain event, module event, or integration event triggers, with its outcomes, failure handling and idempotency. |
| `reaction.md` | `.litespec/behavior/modules/<module>/reactions/<name>/<name>.md` | Explains the reaction beside its record file. |
| `job.yaml` | `.litespec/behavior/modules/<module>/jobs/<name>/<name>.yaml` | Defines one scheduled or one-off job, with its outcomes, failure handling and idempotency. |
| `job.md` | `.litespec/behavior/modules/<module>/jobs/<name>/<name>.md` | Explains the job beside its record file. |
| `notification.yaml` | `.litespec/behavior/modules/<module>/notifications/<name>/<name>.yaml` | Defines one person-facing message, its audience, delivery, consent and claims. |
| `notification.md` | `.litespec/behavior/modules/<module>/notifications/<name>/<name>.md` | Explains the notification beside its record file. |
| `module-event.yaml` | `.litespec/behavior/modules/<module>/module-events/<name>/<name>.yaml` | Defines one fact a module publishes for the other modules of the product, with the domain event it translates and its payload. |
| `module-event.md` | `.litespec/behavior/modules/<module>/module-events/<name>/<name>.md` | Explains the module event beside its record file. |
| `integration-event.yaml` | `.litespec/behavior/modules/<module>/integration-events/<name>/<name>.yaml` | Defines one published or consumed integration event with the system it travels through and how an inbound one arrives. |
| `integration-event.md` | `.litespec/behavior/modules/<module>/integration-events/<name>/<name>.md` | Explains the integration event beside its record file. |
| `read-model.yaml` | `.litespec/behavior/modules/<module>/read-models/<name>/<name>.yaml` | Defines one read model a module keeps from the events another module publishes, with its key, fields and freshness. |
| `read-model.md` | `.litespec/behavior/modules/<module>/read-models/<name>/<name>.md` | Explains the read model beside its record file. |
| `entry-point.yaml` | `.litespec/behavior/entry-points/<entry-point>/entry-point.yaml` | Records one entry point into the system: its kind, auth, density, and voice. |
| `entry-point.md` | `.litespec/behavior/entry-points/<entry-point>/entry-point.md` | Explains the entry point beside its entry-point file. |
| `screen.yaml` | `.litespec/behavior/entry-points/<entry-point>/screens/<home>.<name>/<home>.<name>.yaml` | Records one page a person sees through an entry point: what it offers, the steps it serves, its page frame, and the screens it leads to. |
| `screen.md` | `.litespec/behavior/entry-points/<entry-point>/screens/<home>.<name>/<home>.<name>.md` | Explains the screen beside its record file. |
| `release-record.md` | `.litespec/releases/<YYYY-MM-DD>.md` | Records one release: what it ships and the evidence it was gated on. |
| `runbook.yaml` | `.litespec/implementation/runbooks/<name>/<name>.yaml` | Gives one repeatable recovery or operating procedure. |
| `runbook.md` | `.litespec/implementation/runbooks/<name>/<name>.md` | Explains the runbook beside its record file. |
| `waiver.yaml` | `.litespec/governance/waivers/<name>/<name>.yaml` | Records one waiver from the provisions, pages or finding codes it names, with its kind and reason. A gap the team means to close also names its expiry date, exit and closing records, while a lasting waiver states its consequences instead. |
| `waiver.md` | `.litespec/governance/waivers/<name>/<name>.md` | Explains the waiver beside its record file. |

## Entry Lists and Page Styles

| Template | Target | Purpose |
|:---|:---|:---|
| `permission-set.yaml` | `.litespec/behavior/modules/<module>/permissions.yaml` | Holds the permission entries of one module that roles allow. |
| `permission-set.md` | `.litespec/behavior/modules/<module>/permissions.md` | Explains the permission set beside its file. |
| `setting-set.yaml` | `.litespec/behavior/modules/<module>/settings/<name>.yaml` | Holds the setting entries a module runs with. |
| `setting-set.md` | `.litespec/behavior/modules/<module>/settings/<name>.md` | Explains the setting set beside its file. |
| `refusals.yaml` | `.litespec/behavior/modules/<module>/refusals.yaml` | Declares each refusal code of one module once, with its meaning and problem type. |
| `refusals.md` | `.litespec/behavior/modules/<module>/refusals.md` | Explains the refusal set beside its file. |
| `rule-set.yaml` | `.litespec/behavior/modules/<module>/rules/<name>.yaml` | Holds rule entries that sit together outside one aggregate. |
| `rule-set.md` | `.litespec/behavior/modules/<module>/rules/<name>.md` | Explains the rule set beside its file. |
| `convention.md` | `.litespec/governance/conventions/<name>.md` | States one project convention in provision shape, with ID, strength, and scope. |
| `glossary.md` | `.litespec/behavior/<name>.md` | Renders the Term entries of `.litespec/glossary.yaml`; `litespec new glossary <name>` names the page. |
| `index.md` | `.litespec/<section>/README.md` | Lists what one directory holds and what lives elsewhere. |
| `tutorial.md` | `.litespec/guide/<tutorial>.md` | Gives a newcomer one path to a first working result. |
| `how-to.md` | `.litespec/guide/<how-to>.md` | Solves one stated goal for a reader who is already running. |
| `command.md` | `.litespec/tools/<command>.md` | Documents one command the repository ships. |
| `reference.md` | `.litespec/reference/<name>.md` | Publishes the exact values a reader looks up. |
| `configuration.md` | `.litespec/tools/configuration.md` | Documents the settings a reader can change, with defaults and precedence. |

## Explanation Files

An explanation file lives in the record's folder beside its record file, sharing its stem. It opens with the record title as its one H1. Between the H1 and the first H2 sits an opening paragraph: what the record is in plain words, who it concerns, and the consequence at stake. Then come the fixed H2 sections of its record type, in the table order below. Each section carries one line of guidance in its template. Replace that guidance with the record's own prose, and drop any section whose guidance opens with `Optional.` when it has nothing to say.

| Record | Sections, in order |
|:---|:---|
| product | Scope, Users, Out of Scope, Direction (optional) |
| actor | Responsibilities, Knowledge, Example (optional) |
| role | Access, Limits |
| goal | Target, Measurement |
| vision | Direction, Effect on Current Design, Trigger |
| problem | Affected Users, Failure Cases, Example (optional), Current Workaround, Cost, Evidence, Resolution Criteria |
| constraint | Scope, Rules, Effect on the Product, Open Points (optional) |
| research | Findings, Open Questions (optional), Source Notes (optional) |
| decision | Context, Options, Decision, Consequences, Evidence (optional), Revisit Conditions |
| module | Responsibilities, Boundaries, Example (optional) |
| feature | Capabilities, Success Criteria, Boundaries, Example (optional) |
| requirement | Example (optional); the explanation file itself is optional |
| flow | Example, Branches (optional) |
| aggregate | Lifecycle, Data and Retention, Example (optional), Limits (optional) |
| store | Lifecycle, Data and Retention, Example (optional), Limits (optional) |
| use-case | Flow, Example (optional), Edge Cases (optional) |
| rule-set | Rationale, Example (optional) |
| reaction | Trigger and Effect, Failure Handling, Example (optional) |
| job | Invocation, Failure Handling, Example (optional) |
| notification | Content, Delivery, Example |
| module-event | Consumers, Compatibility |
| integration-event | Consumers, Compatibility, Example |
| read-model | Sources, Freshness, Example (optional) |
| external-system | Use, Data Shared, Failure Handling |
| entry-point | Audience, Authentication, Limits (optional) |
| screen | Task, States, Example |
| permission-set | Risk, Boundaries (optional) |
| setting-set | Defaults, Changes (optional) |
| refusal-set | Meaning, Mapping |
| runbook | Prerequisites, Steps, Verification, Recovery |
| waiver | Provision, Reason, Risk, Expiry |

For an article type (research, decision) and for a type on a layer the project lists in `explanations.layers`, `litespec new` writes the explanation file from the template for its type, with the title filled in. The same setting decides which explanations `litespec model check` requires. An article type always needs one, and every other record needs one on a layer the project lists.

## Interface

| Template | Target | Purpose |
|:---|:---|:---|
| `design-contract.md` | `packages/ui/DESIGN.md` | Binds each page frame to its floorplan component, and names the token file, baseline, motion, and browser target. |
| `ui-source-lock.yaml` | `packages/ui/ui-source-lock.yaml` | Records the installed registry source, digests, and dependencies. |
| `ui-pattern-catalog.yaml` | `.litespec/implementation/patterns/catalog.yaml` | Records each recurring interface decision with its options and choice. |

The model builder reads each record and computes its state in memory. The book renders that state.
