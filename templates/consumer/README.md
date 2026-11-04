# Document Templates

## Intent

These files are starting points for consumer records. `litespec init` copies the project setup files to their targets and writes the local tool manifest; `litespec new` scaffolds a record file or a document from its template. Copy only the record files the current work needs, replace every example value with the project's own before committing, and remove any optional line left unused. A section whose starter text opens with `Optional.` may be removed when it has nothing to say; every other section is required. `litespec init` fills `__PROJECT__` in `.litespec/project.json` and the `## LiteSpec` block in `AGENTS.md`, and lists the placeholders it leaves, and `litespec consumer` refuses a project file that still holds one. The other JSON templates carry working example values; adjust them to the project.

A record file carries its record in YAML, with the entries in `records`. An article type (research, decision) also keeps an explanation file beside its record file. A document holds no record. Requirements sit on a feature file. Rules and events sit on an aggregate file, results sit on a use-case file, and scenarios sit on their use-case, reaction, or job file. Permissions sit on their module's permissions file, and settings on a setting-set file. Each entry point is one record with its screens beside it. Retention rows sit on the retention schedule. Terms live in the glossary at `.litespec/glossary.yaml` instead of on a file.

## Project Setup

| Template | Target | Purpose |
|:---|:---|:---|
| `project.json` | `.litespec/project.json` | Selects the size, shape, stacks, reviewed LiteSpec release, paths, and selected extensions. |
| `project-agents.md` | `AGENTS.md` | Provides the short consumer agent entry point with the `## LiteSpec` block. |
| `glossary.yaml` | `.litespec/glossary.yaml` | Holds the Term entries prose is checked against, with layers, aliases, rejected words, and mannered terms. |
| `example-cast.md` | `.litespec/behavior/example-cast.md` | Fixes the people, place, dates, and amounts every `Example` section draws from. |

## Page Records

| Template | Target | Purpose |
|:---|:---|:---|
| `product.yaml` | `.litespec/intent/product.yaml` | Defines the product boundary and the actors it addresses. |
| `goal.yaml` | `.litespec/intent/goals/<name>.yaml` | States one measurable outcome the product serves. |
| `constraint.yaml` | `.litespec/intent/constraints/<name>.yaml` | Records one outside rule and whether it applies. |
| `research.yaml` | `.litespec/intent/research/<name>.yaml` | Records one outside fact that informs a choice. |
| `research.md` | `.litespec/intent/research/<name>.md` | Explains one research record beside its record file. |
| `decision.yaml` | `.litespec/intent/decisions/<number>-<name>.yaml` | Records one dated choice with its options and evidence. A technical decision lives under `.litespec/implementation/decisions/` instead. |
| `decision.md` | `.litespec/intent/decisions/<number>-<name>.md` | Explains one decision beside its record file. |
| `problem.yaml` | `.litespec/intent/problems/<name>.yaml` | Records one pain someone has today. |
| `feature.yaml` | `.litespec/intent/modules/<module>/features/<name>.yaml` | Records what is offered against a problem, with its requirements as entries. |
| `actor.yaml` | `.litespec/intent/actors/<name>.yaml` | Records one person, organization, or system. |
| `role.yaml` | `.litespec/intent/roles/<name>.yaml` | Records one named set of permissions at a scope. |
| `external-system.yaml` | `.litespec/behavior/external-systems/<name>.yaml` | Records one outside system, its operations, what crosses its boundary, and its failure and erasure behavior. |
| `journey.yaml` | `.litespec/intent/journeys/<name>.yaml` | Records the ordered steps an actor takes toward one goal. |
| `module.yaml` | `.litespec/intent/modules/<module>/module.yaml` | Declares one product area; Behavior folders follow it. |
| `aggregate.yaml` | `.litespec/behavior/modules/<module>/<aggregate>/aggregate.yaml` | Defines one aggregate, with its kind, states, data classes, and its rule and event entries. |
| `use-case.yaml` | `.litespec/behavior/modules/<module>/<aggregate>/<use-case>.yaml` | Defines one command or query, with its result and scenario entries. |
| `reaction.yaml` | `.litespec/behavior/modules/<module>/reactions/<name>.yaml` | Defines one reaction an event triggers. |
| `job.yaml` | `.litespec/behavior/modules/<module>/jobs/<name>.yaml` | Defines one scheduled, one-off, or manual job. |
| `notification.yaml` | `.litespec/behavior/modules/<module>/notifications/<name>.yaml` | Defines one person-facing message and its claims. |
| `integration-event.yaml` | `.litespec/behavior/modules/<module>/integration-events/<name>.yaml` | Defines one published or consumed integration event. |
| `entry-point.yaml` | `.litespec/behavior/entry-points/<entry-point>/entry-point.yaml` | Records one door into the system: its kind and auth. |
| `screen.yaml` | `.litespec/behavior/entry-points/<entry-point>/screens/<name>.yaml` | Records one page a person sees through an entry point, what it offers, and the steps it serves. |
| `release-record.md` | `.litespec/releases/<YYYY-MM-DD>.md` | Records one release: what it ships and the evidence it was gated on. |
| `runbook.yaml` | `.litespec/implementation/runbooks/<name>.yaml` | Gives one repeatable recovery or operating procedure. |
| `exception.yaml` | `.litespec/governance/exceptions/<name>.yaml` | Records one departure from a provision or one gap whose check stays red, with reason and expiry date. |

## Entry Lists and Page Styles

| Template | Target | Purpose |
|:---|:---|:---|
| `permission-list.yaml` | `.litespec/behavior/modules/<module>/permissions.yaml` | Holds the permission entries of one module that roles allow. |
| `setting-set.yaml` | `.litespec/behavior/modules/<module>/settings.yaml` | Holds the setting entries a module runs with. |
| `retention-schedule.yaml` | `.litespec/behavior/retention-schedule.yaml` | Holds the retention rows for every data class. |
| `rule-set.yaml` | `.litespec/behavior/modules/<module>/rules/<name>.yaml` | Holds rule entries that sit together outside one aggregate. |
| `convention.md` | `.litespec/governance/conventions/<name>.md` | States one project convention in provision shape, with ID, strength, and scope. |
| `glossary.md` | `.litespec/behavior/glossary.md` | Renders the Term entries from the glossary. |
| `index.md` | `.litespec/<section>/README.md` | Lists what one directory holds and what lives elsewhere. |
| `tutorial.md` | `.litespec/guide/<tutorial>.md` | Gives a newcomer one path to a first working result. |
| `how-to.md` | `.litespec/guide/<how-to>.md` | Solves one stated goal for a reader who is already running. |
| `command.md` | `.litespec/tools/<command>.md` | Documents one command the repository ships. |
| `reference.md` | `.litespec/reference/<name>.md` | Publishes exact values a reader looks up. |
| `configuration.md` | `.litespec/tools/configuration.md` | Documents settings a reader can change, with defaults and precedence. |

## Explanation Files

An explanation file sits beside its record file with the same name. It opens with the record title as its one H1 and uses the fixed sections of its record type, in order. Each section holds one line of guidance in its template; replace the guidance with the record's own prose and remove a section whose guidance opens with `Optional.` when it has nothing to say.

| Record | Sections, in order |
|:---|:---|
| problem | Who Has It, What They Do Today, Evidence, What Would Retire It |
| journey | Who Walks and Why, Before It Starts (optional), Example, What It Connects To |
| feature | Who It Serves and Why, What It Offers, What Good Looks Like |
| goal | Why It Matters, What Counts as Done, What It Connects To |
| actor | Who They Are, What They Do, What They Can Reach |
| decision | Context, Options Considered, Decision, Evidence with Confidence (optional), What Would Reopen It |
| research | Answer, Notes on Sources (optional) |

Only research and decision scaffold an explanation file today. For the other records, create the `.md` file beside the record file with these sections when the record needs one.

## Interface

| Template | Target | Purpose |
|:---|:---|:---|
| `design-contract.md` | `packages/ui/DESIGN.md` | States the brand, tokens, composites, and voice every frontend composes from. |
| `ui-source-lock.json` | `packages/ui/ui-source-lock.json` | Records the installed registry source, digests, and dependencies. |
| `ui-pattern-catalog.json` | `.litespec/ui/pattern-catalog.json` | Records each recurring interface decision with its options and choice. |

The model builder reads each record and writes the computed state into `model.json`.
