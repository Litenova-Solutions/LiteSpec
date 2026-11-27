# LiteSpec Documentation

LiteSpec is a standard for building one business application from written rules that tools check. This index lists every page of that standard in four groups. Developers and agents open it at the start of a task to find the page that states the rules for that task. A project applies only the folders named by its selection in `.litespec/project.yaml` (its size, shape, technology stacks, and extensions), so most readers need only a few groups. For example, a small server project with the dotnet and svelte stacks reads the Core pages plus the two stack packs, and skips the rest.

## Which Page Do You Want

An **explanation** describes what LiteSpec is and how its parts fit, for a reader who is learning. Start with the [Learn overview](learn/README.md) when LiteSpec is new to you. A **tutorial** takes you to a first working result, one step at a time. A **how-to** solves one stated problem for a reader whose project already runs. A **reference** lists exact values, options, and defaults for lookup. A **command** page documents one command: its options, its exit codes, and what it runs underneath. A standards page states the provisions a reader cites for one concern.

[Read the Standard](learn/reading-the-standard.md) explains which folders a selection applies and how a provision ID cites one rule.

## Standard

The Standard group holds the `core`, `packs`, `quality`, `workspace`, and `ext` folders.

### Core

- [Core overview](core/README.md): the system all projects share, and how its pages fit together.
- [LiteSpec](core/system.md): what LiteSpec coordinates and the parts it combines.
- [Supported Scope](core/scope.md): the size, shape, and stacks each project selects, the extensions and constraints it declares, the record layers it claims in `layers`, and what claiming conformance means.
- [Technology Stacks](core/stacks.md): what selecting a stack applies, and what each stack folder holds.
- [Provision Scopes](core/scopes.md): the stack, layer, artifact, and strength each provision carries, and the scope block each page declares.
- [Engineering Principles](core/principles.md): the rules of thumb for choices no narrower rule covers.
- [Project Model](core/model.md): the four layers, the record types, their links, checks, and the book and diffs.
- [Waivers](core/waivers.md): the one record that sets a provision, a page or finding code aside, with its kind, its reason and, for a gap, its expiry and closing records.
- [Record Layers](core/layers.md): the four record layers and the translation rules between them.
- [Intent Layer](core/intent.md): where a consumer states why it exists and what it promises, and what Intent never holds.
- [Behavior Records](core/behavior.md): the overview of the five Behavior pages and the mapping from records to design.
- [Domain Model](core/domain-model.md): the modules, glossary, aggregates, rules, and domain events inside the bounded context.
- [Use Cases](core/use-cases.md): the commands, queries, results, scenarios, and settings.
- [Automation](core/automation.md): the reactions and jobs that run without a request.
- [Edges and Screens](core/edges.md): the integration events, notifications, entry points, screens, and external systems.
- [Backend Architecture](core/backend-architecture.md): the module layout, the direction references point, and the modules, aggregates, and use cases inside each layer.
- [Backend Domain](core/backend-domain.md): aggregates, lifecycles, closed value sets, and repositories, and what Domain never references.
- [Backend Application](core/backend-application.md): use-case coordination, pipeline stages, ports, reactions, and jobs.
- [Backend Persistence](core/backend-persistence.md): repositories, the read session, the one commit per command, and the obligations on stored data.
- [HTTP API](core/backend-api.md): endpoints, routes, errors, collection paging, and the generated OpenAPI contract.
- [Knowledge](core/knowledge.md): the knowledge areas, their models, and the reference data each area reads through.
- [Backend Testing](core/testing.md): the test suites each project size keeps and what each suite may use.
- [Privacy and Retention](core/privacy.md): the data classes and their retention.
- [Access](core/access.md): the actors, roles, permissions, access each use case declares, entry points, and external systems.
- [Audit Trail](core/audit.md): tamper-resistant evidence of who attempted what in data another party owns.
- [Multitenancy](core/tenancy.md): tenant isolation across authorization, storage, jobs, and recovery for one shared deployment.
- [Concurrency and Idempotency](core/concurrency.md): conflicting writes, caller versions, client keys, and fingerprints.
- [External Integrations](core/integrations.md): provider isolation, failure mapping, and boundary tests for use cases that call outside systems.
- [Durable Delivery](core/outbox.md): one-transaction commits with dispatched messages where delivery must survive process failure.
- [Scheduled Jobs and Reports](core/jobs.md): recurring, delayed, and heavy work, and the reports too heavy for a request.
- [Data Lifecycle](core/lifecycle.md): retention, deletion, archival, and restore behavior for records with a lifetime duty.
- [Localization](core/locale.md): locales, catalogs, and formatting for a product that ships in more than one locale.
- [User Interface Information Architecture](core/ui-ia.md): the sitemap, the route decomposition table, the screen budgets, and the six locked page archetypes.
- [User Interface Verification](core/ui-verification.md): the four-stage workflow, the golden archetype screens, the geometry sensors, and the visual critic.
- [Agent Operating Protocol](core/agent.md): how agents select context, write below decided Intent, raise questions, and report completion.
- [Authoring Standard](core/authoring.md): page styles, provision contracts, identifiers, and prose style; what `litespec-maintain validate` checks.
- [Record Language](core/language.md): the grammar for everything a project writes: field names, values, record names, finding codes, and tool names.
- [Writing](core/writing.md): the prose standard for every record value, title, explanation file, documentation page, comment, changelog, and decision record a project writes.
- [Maintaining the Standards](core/maintaining.md): the maintainer manual for provision lifecycle, renames, citations, and releases.
- [Designing the Standard](core/designing.md): the design rules for LiteSpec itself: one thing per record, a folder for a record with its own reasoning, a reader for every shape, and a shape changed everywhere at once.
- [Product Release](core/release.md): which checks each boundary runs, completion computed from proof, and what the release record and verification report list.

### Workspace

- [Workspace overview](workspace/README.md): the shared repository shape, and how its pages fit together.
- [Workspace Structure](workspace/structure.md): the canonical monorepo tree for applications, packages, docs, and tests.
- [Naming and Code Style](workspace/naming.md): names, suffixes, and file conventions that expose intent and role.
- [Dependencies](workspace/dependencies.md): layer dependencies, package ownership, and approval of new packages.
- [Configuration](workspace/config.md): version pins, compiler rules, environment access, and secret handling.

### Packs

Each pack binds one stack to its technology: the .NET pack for the backend, and the Svelte and Android packs for the frontends. A project applies only the packs its stacks select. See the [Packs overview](packs/README.md) for the list.

- [.NET Stack](packs/dotnet/stack.md): what the .NET backend binding covers and what the builder reads.
- [Svelte Stack](packs/svelte/stack.md): what the Svelte web binding covers and what the builder reads.
- [Android Stack](packs/android/stack.md): the .NET for Android binding, its version pins, and its checks.

### Implementation

Implementation holds code only, so the `.litespec/implementation/` folder with its technical decisions and guides exists only in consumers. A backend element goes where [Write Code for a Use Case](guide/write-code-for-a-use-case.md) states, carries the mark the [Project Model](core/model.md) states, and meets the checks the [Analyzers overview](analyzers/README.md) states.

### Quality

- [Quality overview](quality/README.md): what every release proves beyond behavior, and how its pages fit together.
- [Security](quality/security.md): identity, permissions, data classification, and trust transitions.
- [Operations](quality/operations.md): observability, diagnostics, and support by one maintainer.
- [Continuous Integration](quality/ci.md): the gates each pull request runs for its changed areas.
- [Testing](quality/testing.md): the cross-stack rules that prove each scenario through the public boundary.

### Extensions

This folder holds extensions any stack can select. An extension for one stack sits in that stack's `ext/` folder and is listed with the stack.

- [Extensions overview](ext/README.md): activation criteria and how a consumer selects an extension.
- [AI and Model Context Protocol](ext/ai-mcp.md): model-mediated queries with a closed tools set, structure-only scenarios, and a pinned stub, plus the server a product ships so an outside model can call its public API.
- [API Compatibility](ext/compat.md): versioning and retirement for APIs with public, partner, mobile, or independently deployed consumers.
- [Delivery](ext/delivery.md): release readiness gates, image build and rollout, deployment and rollback, backup and restore, alerting and the supply-chain gate for a product operated in production.
- [Plans and Entitlements](ext/plans.md): plans, entitlements, and feature gating for a product that sells tiers.
- [Live Delivery](ext/realtime.md): server-sent events and interactive messaging where measured polling cannot meet the update target.

## Reference

The Reference group holds the `analyzers`, `reference`, `tools`, and `maintain` folders.

### CLI

- [CLI overview](tools/README.md): every shipped command with its arguments, options, and exit codes.
- [Initialize Consumer](tools/init.md): writes `.litespec/project.yaml` for a new consumer from a preset.
- [Scaffold Record](tools/new.md): creates a record file and reserves its identifier, allocates the next decision number, or appends a result, scenario, or list entry to its owning file.
- [Rename Record](tools/rename.md): renames a record identifier and rewrites every link.
- [Model Builder](tools/model.md): checks the project model, formats record files, renders generated regions, answers record queries, and reports module edges.
- [Book](tools/book.md): serves the project book live on localhost, or writes the static book with `--out`.
- [Rules Query](tools/rules.md): lists the provisions that govern a path or record identifier, grouped by strength.
- [Skill](tools/skill.md): installs or removes the litespec skill.
- [Context](tools/context.md): writes the specification as one plain-text file for an AI chat.
- [UI Audit](tools/ui-audit.md): visits each route of a running application in a browser and reports what the rendered pages show wrong.

### Maintain

- [Maintain overview](maintain/README.md): the four maintainer commands.
- [Validate Standards](maintain/validate.md): checks the standards pages, templates, and identifiers against the authoring contract.
- [Generate Provisions](maintain/provisions.md): rebuilds the provision index from the active standards.
- [Generate Provision Changelog](maintain/changelog.md): writes the provision changes since a git ref into the changelog.
- [Publish a Local Toolchain](maintain/publish-local.md): packs the checkout's CLI and analyzers into a local feed and moves a consumer onto that build.

### Analyzers

The Roslyn analyzers report LiteSpec rule breaks as compiler errors on .NET code.

- [Analyzers overview](analyzers/README.md): what the analyzers check, how a rule runs, and where each rule is documented.
- [Check .NET Code with the Analyzers](analyzers/check-dotnet-code-with-the-analyzers.md): install the package once, read the first findings, and raise enforced rules to build errors.
- [Analyzer rules overview](analyzers/rules/README.md): the index of analyzer rules, grouped by layer; that index states how many rules ship.

### Reference

- [Reference overview](reference/README.md): the exact values to look up, and which page holds each.
- [Glossary](reference/glossary.md): the one term for each LiteSpec concept.
- [Provisions](reference/provisions.md): every provision ID resolved to its heading, page, and mark.
- [Finding Codes](reference/finding-codes.md): every finding code the tools emit, with its level, layer, trace, meaning, and fix.
- [Audit Obligations](reference/audit-obligations.md): the external obligations the audit extension satisfies.
- [Decision Records](reference/decision-records.md): where a decision record lives, what it holds, and how it ends.
- [Project File](reference/project-file.md): every field of `.litespec/project.yaml` and what reads it.
- [Release Manifest](reference/manifest.md): every field of `litespec.manifest.yaml` and how to add a stack or extension.
- [Record Folders](reference/folders.md): the folder and file each record type lives in.
- [Formats](reference/formats.md): every file format, who writes it, its envelope, and the schema and command that check it.
- [Published URLs](reference/published-urls.md): every URL the site serves for agents and people.
- [Document templates](../templates/consumer/README.md): the starting files for each consumer record type.

## Learn

The Learn group holds the `learn`, `guide`, `tutorial`, and `example` folders.

### Learn

Readers who are new to LiteSpec start with the [Learn overview](learn/README.md).

- [Learn overview](learn/README.md): the reading order for the explanation pages.
- [Why LiteSpec Exists](learn/why-litespec.md): the problem LiteSpec solves, for a team that is deciding whether LiteSpec fits.
- [How LiteSpec Works](learn/how-litespec-works.md): the standard, the records, the model, and the tools, traced through one Claimwick use case.
- [Read the Standard](learn/reading-the-standard.md): project selections, page arrangement, and provision IDs; how to find the rules that apply to a project.
- [How LiteSpec Compares](learn/how-litespec-compares.md): LiteSpec beside fourteen other approaches to spec-driven development, and when each fits.
- [Lineage](learn/lineage.md): the practices LiteSpec borrows from, the words it takes from each, and what it does differently.
- [Work with Agents](learn/working-with-agents.md): how an agent finds the rules, what it decides alone, and what it brings back.
- [Adopt LiteSpec in Steps](learn/adopting-litespec.md): which record layers a team claims first and how it adds the rest.

### Example

This group follows one product end to end, from its first record to a passing check.

- [Example overview](example/README.md): the Claimwick product the example follows, and the reading order for its pages.
- [The Claimwick Product](example/the-product.md): the Intent records: brief, goal, problem, feature, requirements, research, decision, constraint, and flow.
- [Modules and Records](example/modules-and-records.md): the Spending module as Behavior records: aggregates, rules, use cases, entry point and screen, and the reaction, job, notification and settings around them.
- [A Use Case in Code](example/a-use-case-in-code.md): submit expense from its record into .NET code and tests.
- [The Claimwick Screen](example/the-screen.md): the expenses screen, the generated client it reads, the shared UI package it composes, and the full model check.
- [The Model and Its Checks](example/model-and-checks.md): the findings that waivers cover, a waiver record, and a query of one record.

### Guides

Tutorials take you to a first working result; how-to guides solve one stated problem in a running project. The [Guides overview](guide/README.md) states the order to follow them in.

- [Guides overview](guide/README.md): tutorials for a first result, how-to guides for one goal in a running project.
- [Start a Consumer Repository](tutorial/getting-started.md): start a small consumer repository through one coded use case.
- [Model a Behavior Module](tutorial/model-domain.md): specify one spending module the builder reads.
- [Install the CLI](guide/install-the-cli.md): install the command line for a consumer repository or a machine, by every method a release ships.
- [Start with Records Only](guide/start-with-records-only.md): claim the Intent and Behavior records alone, and check them with the model check and the book.
- [Add a Svelte Frontend](guide/add-a-svelte-frontend.md): declare a Svelte frontend on the web stack.
- [Add an Android Frontend](guide/add-an-android-frontend.md): declare a native Android app built with .NET for Android.
- [Write a Problem](guide/write-a-problem.md): write one problem with its failure cases, evidence and resolution criteria.
- [Write a Feature](guide/write-a-feature.md): write one feature with its requirements and the problem it answers.
- [Write a Constraint](guide/write-a-constraint.md): write one outside rule with its sources, its scope, its rules and the requirements that hold its effect.
- [Write a Flow](guide/write-a-flow.md): write one actor's path toward one goal or one problem, from its start through its steps, blockers and branches.
- [Write a Research Record](guide/write-a-research-record.md): write one question's findings, each tied to its source and marked as stated or inferred.
- [Write Code for a Use Case](guide/write-code-for-a-use-case.md): where each element goes, the mark it carries, and what checks it.
- [Build an Audit Trail](guide/audit-trail.md): the audit extension on the .NET LiteBus pipeline, step by step.
- [Agent Hooks and MCP](guide/agent-hooks-and-mcp.md): run `rules` on each file after it is edited, and check the change set before the agent stops.
- [Add a Stack or an Extension](guide/add-a-stack-or-extension.md): add a stack folder or an extension page to the release manifest.

## LiteSpec's Own Records

This group holds the `intent` folder: the research and decisions behind this release.

### Research and Decisions

The reasoning behind the provisions is kept as consumer-shaped records. These records inform the provisions and settle nothing themselves.

- [Research and Decisions overview](intent/README.md): the research and decisions behind this release, and how provisions cite them.

Consumers pin a complete release. `CHANGELOG.md` describes the current release.
