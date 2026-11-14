# Document Templates

## Intent

These files are starting points for consumer records. `litespec init` copies the project setup files to their targets and writes the local tool manifest; `litespec new` scaffolds a record file or a document from its template. Copy only the record files the current work needs, replace every example value with the project's own before committing, and remove any optional line left unused. A section whose starter text opens with `Optional.` may be removed when it has nothing to say; every other section is required. `litespec init` fills `__PROJECT__` in `.litespec/project.yaml` and the `agent-protocol` region in `AGENTS.md`, and lists the placeholders it leaves, and `litespec model check` refuses a project file that still holds one. The other templates carry working example values; adjust them to the project.

A record file carries its record in YAML, with the entries in `records`. A named record sits in its own folder named for it, as `<name>/<name>.yaml`, and its explanation file `<name>.md` sits in that same folder beside the record file. Every record type with a fixed-section explanation has its own `.md` template, listed below. A document holds no record. Each requirement is a named record in its own folder inside its feature's folder. Rules and events sit on an aggregate file, results sit on a use-case file, and scenarios sit on their use-case, reaction, or job file. Permissions sit on their module's permissions file, and settings on a setting-set file. Each entry point is one record in its own folder, with its `screens/` folder beside the entry-point file. Retention rows sit on the retention schedule. Terms live in the glossary at `.litespec/glossary.yaml` instead of on a file.

## Project Setup

| Template | Target | Purpose |
|:---|:---|:---|
| `project.yaml` | `.litespec/project.yaml` | Selects the size, shape, stacks, reviewed LiteSpec release, paths, and selected extensions. |
| `project-agents.md` | `AGENTS.md` | Provides the short consumer agent entry point with the generated `agent-protocol` region above the project-owned `## Project Commands` section. |
| `glossary.yaml` | `.litespec/glossary.yaml` | Holds the Term entries prose is checked against, with layers, aliases, rejected words, and mannered terms. |
| `example-cast.md` | `.litespec/example-cast.md` | Fixes the people, place, dates, and amounts every `Example` section draws from. |

## Page Records

| Template | Target | Purpose |
|:---|:---|:---|
| `product.yaml` | `.litespec/intent/product.yaml` | Defines the product boundary and the actors it addresses. |
| `product.md` | `.litespec/intent/product.md` | Explains the product beside its record file. |
| `goal.yaml` | `.litespec/intent/goals/<name>/<name>.yaml` | States one measurable outcome the product serves. |
| `goal.md` | `.litespec/intent/goals/<name>/<name>.md` | Explains one goal beside its record file. |
| `vision.yaml` | `.litespec/intent/visions/<name>/<name>.yaml` | States where the product is heading beyond what it promises now. |
| `vision.md` | `.litespec/intent/visions/<name>/<name>.md` | Explains one vision beside its record file. |
| `constraint.yaml` | `.litespec/intent/constraints/<name>/<name>.yaml` | Records one outside rule and whether it applies. |
| `constraint.md` | `.litespec/intent/constraints/<name>/<name>.md` | Explains one constraint beside its record file. |
| `research.yaml` | `.litespec/intent/research/<name>/<name>.yaml` | Records one outside fact that informs a choice. |
| `research.md` | `.litespec/intent/research/<name>/<name>.md` | Explains one research record beside its record file. |
| `decision.yaml` | `.litespec/intent/decisions/<number>-<name>/<number>-<name>.yaml` | Records one dated choice with its options and evidence. A technical decision lives under `.litespec/implementation/decisions/` instead. |
| `decision.md` | `.litespec/intent/decisions/<number>-<name>/<number>-<name>.md` | Explains one decision beside its record file. |
| `problem.yaml` | `.litespec/intent/problems/<name>/<name>.yaml` | Records one pain someone has today. |
| `problem.md` | `.litespec/intent/problems/<name>/<name>.md` | Explains one problem beside its record file. |
| `feature.yaml` | `.litespec/intent/problems/<problem>/features/<name>/<name>.yaml` | Records the offer against the problem whose folder holds it. Its requirements sit beside it in their own folders. |
| `feature.md` | `.litespec/intent/problems/<problem>/features/<name>/<name>.md` | Explains one feature beside its record file. |
| `requirement.yaml` | `.litespec/intent/problems/<problem>/features/<feature>/requirements/<name>/<name>.yaml` | Records one commitment the feature makes, with its kind and statement. |
| `requirement.md` | `.litespec/intent/problems/<problem>/features/<feature>/requirements/<name>/<name>.md` | Explains one requirement beside its record file, when its statement needs prose. |
| `actor.yaml` | `.litespec/intent/actors/<name>/<name>.yaml` | Records one person, organization, or system. |
| `actor.md` | `.litespec/intent/actors/<name>/<name>.md` | Explains one actor beside its record file. |
| `role.yaml` | `.litespec/intent/roles/<name>/<name>.yaml` | Records one named set of permissions at a scope. |
| `role.md` | `.litespec/intent/roles/<name>/<name>.md` | Explains one role beside its record file. |
| `external-system.yaml` | `.litespec/behavior/external-systems/<name>/<name>.yaml` | Records one outside system, its operations, what crosses its boundary, and its failure and erasure behavior. |
| `external-system.md` | `.litespec/behavior/external-systems/<name>/<name>.md` | Explains one external system beside its record file. |
| `journey.yaml` | `.litespec/intent/journeys/<name>/<name>.yaml` | Records the ordered steps an actor takes toward one goal. |
| `journey.md` | `.litespec/intent/journeys/<name>/<name>.md` | Explains one journey beside its record file. |
| `module.yaml` | `.litespec/behavior/modules/<module>/module.yaml` | Declares one product area; Behavior folders follow it. |
| `module.md` | `.litespec/behavior/modules/<module>/module.md` | Explains one module beside its module file. |
| `aggregate.yaml` | `.litespec/behavior/modules/<module>/aggregates/<aggregate>/aggregate.yaml` | Defines one aggregate, with its kind, states, data classes, and its rule and event entries. |
| `aggregate.md` | `.litespec/behavior/modules/<module>/aggregates/<aggregate>/aggregate.md` | Explains one aggregate beside its aggregate file. |
| `use-case.yaml` | `.litespec/behavior/modules/<module>/aggregates/<aggregate>/use-cases/<use-case>/<use-case>.yaml` | Defines one command or query, with its result and scenario entries. |
| `use-case.md` | `.litespec/behavior/modules/<module>/aggregates/<aggregate>/use-cases/<use-case>/<use-case>.md` | Explains one use case beside its record file. |
| `reaction.yaml` | `.litespec/behavior/modules/<module>/reactions/<name>/<name>.yaml` | Defines one reaction an event triggers. |
| `reaction.md` | `.litespec/behavior/modules/<module>/reactions/<name>/<name>.md` | Explains one reaction beside its record file. |
| `job.yaml` | `.litespec/behavior/modules/<module>/jobs/<name>/<name>.yaml` | Defines one scheduled, one-off, or manual job. |
| `job.md` | `.litespec/behavior/modules/<module>/jobs/<name>/<name>.md` | Explains one job beside its record file. |
| `notification.yaml` | `.litespec/behavior/modules/<module>/notifications/<name>/<name>.yaml` | Defines one person-facing message and its claims. |
| `notification.md` | `.litespec/behavior/modules/<module>/notifications/<name>/<name>.md` | Explains one notification beside its record file. |
| `integration-event.yaml` | `.litespec/behavior/modules/<module>/integration-events/<name>/<name>.yaml` | Defines one published or consumed integration event. |
| `integration-event.md` | `.litespec/behavior/modules/<module>/integration-events/<name>/<name>.md` | Explains one integration event beside its record file. |
| `entry-point.yaml` | `.litespec/behavior/entry-points/<entry-point>/entry-point.yaml` | Records one door into the system: its kind and auth. |
| `entry-point.md` | `.litespec/behavior/entry-points/<entry-point>/entry-point.md` | Explains one entry point beside its entry-point file. |
| `screen.yaml` | `.litespec/behavior/entry-points/<entry-point>/screens/<home>.<name>/<home>.<name>.yaml` | Records one page a person sees through an entry point, what it offers, and the steps it serves. |
| `screen.md` | `.litespec/behavior/entry-points/<entry-point>/screens/<home>.<name>/<home>.<name>.md` | Explains one screen beside its record file. |
| `release-record.md` | `.litespec/releases/<YYYY-MM-DD>.md` | Records one release: what it ships and the evidence it was gated on. |
| `runbook.yaml` | `.litespec/implementation/runbooks/<name>/<name>.yaml` | Gives one repeatable recovery or operating procedure. |
| `runbook.md` | `.litespec/implementation/runbooks/<name>/<name>.md` | Explains one runbook beside its record file. |
| `departure.yaml` | `.litespec/governance/departures/<name>/<name>.yaml` | Records one departure from the provisions, pages or finding codes it names, with the reason. A gap the team means to close also names its expiry date and exit. |
| `departure.md` | `.litespec/governance/departures/<name>/<name>.md` | Explains one departure beside its record file. |

## Entry Lists and Page Styles

| Template | Target | Purpose |
|:---|:---|:---|
| `permission-list.yaml` | `.litespec/behavior/modules/<module>/permissions.yaml` | Holds the permission entries of one module that roles allow. |
| `permission-list.md` | `.litespec/behavior/modules/<module>/permissions.md` | Explains the permission list beside its file. |
| `setting-set.yaml` | `.litespec/behavior/modules/<module>/settings/<name>.yaml` | Holds the setting entries a module runs with. |
| `setting-set.md` | `.litespec/behavior/modules/<module>/settings/<name>.md` | Explains the setting set beside its file. |
| `retention-schedule.yaml` | `.litespec/behavior/retention-schedule.yaml` | Holds the retention rows for every data class. |
| `retention-schedule.md` | `.litespec/behavior/retention-schedule.md` | Explains the retention schedule beside its file. |
| `rule-set.yaml` | `.litespec/behavior/modules/<module>/rules/<name>.yaml` | Holds rule entries that sit together outside one aggregate. |
| `rule-set.md` | `.litespec/behavior/modules/<module>/rules/<name>.md` | Explains the rule set beside its file. |
| `convention.md` | `.litespec/governance/conventions/<name>.md` | States one project convention in provision shape, with ID, strength, and scope. |
| `glossary.md` | `.litespec/behavior/<name>.md` | Renders the Term entries of `.litespec/glossary.yaml`; `litespec new glossary <name>` names the page. |
| `index.md` | `.litespec/<section>/README.md` | Lists what one directory holds and what lives elsewhere. |
| `tutorial.md` | `.litespec/guide/<tutorial>.md` | Gives a newcomer one path to a first working result. |
| `how-to.md` | `.litespec/guide/<how-to>.md` | Solves one stated goal for a reader who is already running. |
| `command.md` | `.litespec/tools/<command>.md` | Documents one command the repository ships. |
| `reference.md` | `.litespec/reference/<name>.md` | Publishes exact values a reader looks up. |
| `configuration.md` | `.litespec/tools/configuration.md` | Documents settings a reader can change, with defaults and precedence. |

## Explanation Files

An explanation file sits in the record's folder beside its record file, with the same stem. It opens with the record title as its one H1, then an opening paragraph between the H1 and the first H2: what the record is in plain words, who it concerns and why it matters. After that come the fixed H2 sections of its record type, in the order of the table below. Each section holds one line of guidance in its template; replace the guidance with the record's own prose and remove a section whose guidance opens with `Optional.` when it has nothing to say.

| Record | Sections, in order |
|:---|:---|
| product | Scope, Users, Out of Scope, Direction |
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
| journey | Example, Branches (optional) |
| aggregate | Lifecycle, Data and Retention, Example (optional), Limits (optional) |
| use-case | Flow, Example (optional), Edge Cases (optional) |
| rule-set | Rationale, Example (optional) |
| reaction | Trigger and Effect, Failure Handling, Example (optional) |
| job | Schedule, Failure Handling, Example (optional) |
| notification | Content, Delivery, Example |
| integration-event | Consumers, Compatibility, Example |
| external-system | Use, Data Shared, Failure Handling |
| entry-point | Audience, Authentication, Limits (optional) |
| screen | Task, States, Example |
| permission-list | Risk, Boundaries (optional) |
| setting-set | Defaults, Changes (optional) |
| retention-schedule | Legal Basis, Conflicts, Deletion |
| runbook | Prerequisites, Steps, Verification, Recovery |
| departure | Provision, Reason, Risk, Expiry |

For an article type (research, decision) and for a type on a layer the project lists in `explanations.layers`, `litespec new` writes the explanation file from the template for its type, with the title filled in. The same setting decides which explanations `litespec model check` requires: an article type always needs one, and every other record needs one on a layer the project lists.

## Interface

| Template | Target | Purpose |
|:---|:---|:---|
| `design-contract.md` | `packages/ui/DESIGN.md` | States the brand, tokens, composites, and voice every frontend composes from. |
| `ui-source-lock.yaml` | `packages/ui/ui-source-lock.yaml` | Records the installed registry source, digests, and dependencies. |
| `ui-pattern-catalog.yaml` | `.litespec/implementation/patterns/catalog.yaml` | Records each recurring interface decision with its options and choice. |

The model builder reads each record and computes its state in memory, and the book renders that state.
