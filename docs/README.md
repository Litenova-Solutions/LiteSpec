# LiteSpec Documentation

## Intent

LiteSpec is a standard for building one business application from written rules that tools check, and this index lists every page of that standard in four groups. Developers and agents open it at the start of a task to find the page that states the rules for that task. A project applies only the folders named by its selection in `.litespec/project.yaml` (its size, shape, technology stacks, and extensions), so most readers need only a few groups.

## Which Page Do You Want

An **explanation** describes what LiteSpec is and how its parts fit, for a reader who is learning. Start with the [Learn overview](learn/README.md) when LiteSpec is new to you.
A **tutorial** takes you to a first working result, one step at a time.
A **how-to** solves one stated problem for a reader whose project already runs.
A **reference** lists exact values, options, and defaults for lookup.
A **command** page documents one command: its options, its exit codes, and what it runs underneath.
A standards page states the provisions a reader cites for one concern.

A project applies only some folders below. [Read the Standard](learn/reading-the-standard.md) explains which folders a selection applies and how a provision ID cites one rule.

## Standard

The Standard group holds the `core`, `backend`, `frontend`, `quality`, `workspace`, and `ext` folders.

### Core

- [Core overview](core/README.md): the system all projects share, and how its pages fit together.
- [LiteSpec](core/system.md): what LiteSpec coordinates and the parts it combines.
- [Supported Scope](core/scope.md): the size, shape, and stacks each project selects, the extensions and constraints it declares, the record layers it claims in `layers`, and what claiming conformance means.
- [Technology Stacks](core/stacks.md): what selecting a stack applies, and what each stack folder holds.
- [Provision Scopes](core/scopes.md): the stack, layer, artifact, and strength each provision carries, and the scope block each page declares.
- [Engineering Principles](core/principles.md): the rules of thumb for choices no narrower rule covers.
- [Project Model](core/model.md): the four layers, the record types, their links, checks, and the book and diffs.
- [Departures](core/departures.md): the one record that sets a provision, a page or a finding code aside, with its reason and its expiry.
- [Record Layers](core/layers.md): the four record layers and the translation rules between them.
- [Intent Layer](core/intent.md): where a consumer states why it exists and what it promises, and what Intent never holds.
- [Behavior Records](core/behavior.md): the overview of the five Behavior pages and the mapping from records to design.
- [Domain Model](core/domain-model.md): the modules, glossary, aggregates, rules, and domain events inside the bounded context.
- [Use Cases](core/use-cases.md): the commands, queries, results, scenarios, and settings.
- [Automation](core/automation.md): the reactions and jobs that run without a request.
- [Edges and Screens](core/edges.md): the integration events, notifications, entry points, screens, and external systems.
- [Privacy and Retention](core/privacy.md): the data classes and the retention schedule.
- [Access](core/access.md): the actors, roles, permissions, access each use case declares, entry points, and external systems.
- [Agent Operating Protocol](core/agent.md): how agents select context, write below decided Intent, raise questions, and report completion.
- [Authoring Standard](core/authoring.md): page styles, provision contracts, identifiers, and prose style; what `litespec-maintain validate` checks.
- [Record Language](core/language.md): the grammar for everything a project writes: field names, values, record names, finding codes, and tool names.
- [Writing](core/writing.md): the prose standard for every record value, title, explanation file, documentation page, comment, changelog, and decision record a project writes.
- [Maintaining the Standards](core/maintaining.md): the maintainer manual for provision lifecycle, renames, citations, and releases.
- [Product Release](core/release.md): which checks each boundary runs, completion computed from proof, and what the release record and verification report list.

### Workspace

- [Workspace overview](workspace/README.md): the shared repository shape, and how its pages fit together.
- [Workspace Structure](workspace/structure.md): the canonical monorepo tree for applications, packages, docs, and tests.
- [Naming and Code Style](workspace/naming.md): names, suffixes, and file conventions that expose intent and role.
- [Dependencies](workspace/dependencies.md): layer dependencies, package ownership, and approval of new packages.
- [Configuration](workspace/config.md): version pins, compiler rules, environment access, and secret handling.

### Backend

These principles hold for every backend. A backend stack folder states how its technology realizes them.

- [Backend overview](backend/README.md): the principles every backend shares and the stacks that realize them.
- [Backend Architecture](backend/architecture.md): the module layout as the default across four layers, the direction references point, and the modules, aggregates, and use cases inside each layer.
- [Domain Layer](backend/domain.md): aggregates, lifecycles, closed value sets, and repositories, and what Domain never references.
- [Application Layer](backend/application.md): use-case coordination, pipeline stages, ports, and reactions.
- [Persistence](backend/persistence.md): repositories, the read session, the one commit per command, and the obligations on stored data.
- [HTTP API](backend/api.md): endpoints, routes, errors, collection paging, and the generated OpenAPI contract.
- [Backend Testing](backend/testing.md): the test suites each project size keeps and what each suite may use.

### .NET Stack

- [.NET Stack](backend/dotnet/stack.md): what the .NET backend binding covers and what the builder reads.
- [.NET Architecture](backend/dotnet/architecture.md): the shared kernel and modules projects with their hosts, the namespace tree, and host-only endpoints.
- [.NET Domain Layer](backend/dotnet/domain.md): the C# types, references, and tooling for the Domain project.
- [.NET Application Layer](backend/dotnet/application.md): LiteBus dispatch and validity reporting.
- [.NET Persistence](backend/dotnet/persistence.md): the Marten baseline provider rules.
- [.NET HTTP API](backend/dotnet/api.md): endpoint classes and build-time OpenAPI generation.
- [.NET Workspace](backend/dotnet/workspace.md): solutions and project files in the workspace tree.
- [.NET Naming and Code Style](backend/dotnet/naming.md): C# file and primary-type match, implementation style, and reasoned names.
- [.NET Packages](backend/dotnet/packages.md): LiteBus module scope and the analyzer setup.
- [.NET Configuration](backend/dotnet/config.md): SDK, build, package, and tool pins in MSBuild files.
- [Executable BDD Acceptance Tests](backend/dotnet/ext/bdd.md): executable scenarios in Reqnroll or LightBDD; an extension for this stack.
- [Layer Projects](backend/dotnet/ext/layer-projects.md): the five-project packaging for backends that keep it; an extension for this stack.

### Frontend

These principles hold for every frontend, including the Blazor stack and a native client.

- [Frontend overview](frontend/README.md): the principles every frontend shares and the stacks that realize them.
- [Frontend Structure](frontend/structure.md): frame, routes, and use-case modules, and the module tree that mirrors the backend.
- [Frontend Rendering and Routes](frontend/rendering.md): route addresses, data loading, and how each route state renders.
- [Frontend Components](frontend/components.md): the shared UI package and how page code composes its parts.
- [Controlled UI Governance](frontend/ui.md): the visual authority, the design contract, the floorplans, and the route suite.
- [UI Patterns](frontend/patterns.md): the catalog that decides each recurring interface question once.
- [Frontend Data and State](frontend/data.md): the generated client, the query cache, command writes, and how failures reach the reader.
- [Frontend Testing](frontend/testing.md): unit tests, browser suites, and the contract check for every address.

### Web

The TypeScript web pages the React and Svelte stacks share. This folder is a group, not a stack: no project selects it directly.

- [Web Structure](frontend/web/structure.md): the application tree, the frame, use-case marks, and the generated records file.
- [Web Rendering and Routes](frontend/web/rendering.md): the route metadata rule both stacks share.
- [TypeScript Web Components](frontend/web/components.md): the registry source, the closed element list, and the shared route checks.
- [Web UI](frontend/web/ui.md): the registry build, suite marks, CSS decisions, and source rules for shared UI.
- [Web UI Patterns](frontend/web/patterns.md): one implementation per platform for each chosen pattern.
- [Web Data and State](frontend/web/data.md): the generated client, failure mapping, cache updates, and state ownership.
- [Web Testing](frontend/web/testing.md): Vitest beside the module and Playwright against a running stack.

### React Stack

- [React Stack](frontend/web/react/stack.md): the React binding and its three rendering modes.

### Svelte Stack

- [Svelte Stack](frontend/web/svelte/stack.md): the Svelte binding and its three rendering modes.

### Blazor Stack

The Blazor stack serves a product that has no server of its own.

- [Blazor Stack](frontend/blazor/stack.md): the client-side domain model, browser persistence, and WebAssembly application.

### Mobile Stacks

The mobile pages state what every native mobile app does the same way, and each mobile stack binds them to one platform.

- [Mobile overview](frontend/mobile/README.md): the shared mobile pages and the mobile stacks.
- [Mobile Structure](frontend/mobile/structure.md): the mobile solution, the platform-free core projects, and the screen folders.
- [Mobile Screens and Navigation](frontend/mobile/screens.md): native views, one navigation controller, the back gesture, insets, and motion.
- [Mobile Data and State](frontend/mobile/data.md): handlers, the typed store, app-private storage, and the generated client.
- [Mobile Device Capabilities](frontend/mobile/device.md): pickers, permissions, audio focus, and background behavior.
- [Mobile Testing](frontend/mobile/testing.md): plain .NET core tests and device tests for screens.
- [Android Stack](frontend/mobile/android/stack.md): the .NET for Android binding, its version pins, and its checks.

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
- [API Compatibility](ext/compat.md): versioning and retirement for APIs with public, partner, mobile, or independently deployed consumers.
- [Caching](ext/cache.md): staleness contracts and recovery for a repeated expensive read that traces or load tests name.
- [Concurrency and Idempotency](ext/concurrency.md): versions, client keys, and fingerprints where writes can overlap or retries can duplicate an effect.
- [Data Lifecycle](ext/lifecycle.md): retention, deletion, archival, and restore behavior for records with a lifetime duty.
- [Delivery](ext/delivery.md): release readiness gates, image build and rollout, deployment and rollback, backup and restore, alerting and the supply-chain gate for a product operated in production.
- [External Integrations](ext/integrations.md): provider isolation, failure mapping, and boundary tests for use cases that call outside systems.
- [Audit Trail](ext/audit.md): tamper-resistant evidence of who attempted what in data another party owns.
- [Localization](ext/locale.md): locales, catalogs, and formatting for a product that ships in more than one locale.
- [Model Context Protocol](ext/mcp.md): the server a product ships so an LLM client can call its public API.
- [Multitenancy](ext/tenancy.md): tenant isolation across authorization, storage, jobs, and recovery for one shared deployment.
- [Plans and Entitlements](ext/plans.md): plans, entitlements, and feature gating for a product that sells tiers.
- [Live Delivery](ext/realtime.md): server-sent events and interactive messaging where measured polling cannot meet the update target.
- [Durable Delivery](ext/outbox.md): one-transaction commits with dispatched messages where delivery must survive process failure.
- [Jobs and Queues](ext/jobs.md): recurring, delayed, and heavy work, and human-cleared queues, with leases that survive restarts.
- [Release Stage](ext/stage.md): the pre-customer-data stage, what it defers, and the promotion that ends it.
- [Devices](ext/devices.md): the entry-point protocol block: messages, version rule, client rules, nonce, and offline uploads.
- [Customer Rules and Hooks](ext/customer-rules.md): customer-composed rules and hooks with a may and may-not contract.
- [Model Assistance](ext/ai.md): model-mediated queries with a closed tools set, structure-only scenarios, and a pinned stub.

## Reference

The Reference group holds the `analyzers`, `reference`, `tools`, and `maintain` folders.

### CLI

- [CLI overview](tools/README.md): every shipped command with its arguments, options, and exit codes.
- [Initialize Consumer](tools/init.md): writes `.litespec/project.yaml` for a new consumer from a preset.
- [Model Builder](tools/model.md): checks the project model, formats record files, renders generated regions, and answers record queries.
- [Book](tools/book.md): builds the project book from the records: one offline shell with the Outline and Security views.

### Maintain

- [Maintain overview](maintain/README.md): the three commands that keep the standard itself consistent.
- [Validate Standards](maintain/validate.md): checks the standards pages, templates, and identifiers against the authoring contract.
- [Generate Provisions](maintain/provisions.md): rebuilds the provision index from the active standards.

### Analyzers

The Roslyn analyzers that report LiteSpec rule breaks as compiler errors on .NET code.

- [Analyzers overview](analyzers/README.md): what the analyzers check, how a rule runs, and where each rule is documented.
- [Check .NET Code with the Analyzers](analyzers/check-dotnet-code-with-the-analyzers.md): install the package once, read the first findings, and raise enforced rules to build errors.
- [Analyzer rules overview](analyzers/rules/README.md): the index of analyzer rules, grouped by layer; that index states how many rules ship.

### Reference

- [Reference overview](reference/README.md): the exact values to look up, and which page holds each.
- [Glossary](reference/glossary.md): the one term for each LiteSpec concept.
- [Provisions](reference/provisions.md): every provision ID resolved to its heading, page, and mark.
- [Audit Obligations](reference/audit-obligations.md): the external obligations the audit extension satisfies.
- [Decision Records](reference/decision-records.md): where a decision record lives, what it holds, and how it ends.
- [Project File](reference/project-file.md): every field of `.litespec/project.yaml` and what reads it.
- [Release Manifest](reference/manifest.md): every field of `litespec.manifest.yaml` and how to add a stack or extension.
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
- [Adopt LiteSpec](learn/adopting-litespec.md): what changes for a team, in the order teams usually do it.

### Example

One product worked end to end, from its first record to a passing check.

- [Example overview](example/README.md): the Claimwick product the example follows, and the reading order for its pages.
- [The Claimwick Product](example/the-product.md): the Intent records: brief, goal, problem, feature, requirements, research, decision, constraint, and journey.
- [Modules and Records](example/modules-and-records.md): the Spending module as Behavior records: aggregates, rules, use cases, entry point and screen, and the reaction, job, notification and settings around them.
- [A Use Case in Code](example/a-use-case-in-code.md): Submit expense from its record into .NET code and tests.
- [The Claimwick Screen](example/the-screen.md): the expenses screen, the generated client it reads, the shared UI package it composes, and the full model check.
- [The Model and Its Checks](example/model-and-checks.md): the findings that departures cover, a departure record, and a query of one record.

### Guides

Tutorials take a reader to a first working result; how-to guides solve one stated problem in a running project.

- [Guides overview](guide/README.md): tutorials for a first result, how-to guides for one goal in a running project.
- [Start a Consumer Repository](tutorial/getting-started.md): start a small consumer repository through one coded use case.
- [Model a Behavior Module](tutorial/model-domain.md): specify one spending module the builder reads.
- [Install the CLI](guide/install-the-cli.md): install the command line for a consumer repository or a machine, by every method a release ships.
- [Start with Records Only](guide/start-with-records-only.md): claim the Intent and Behavior records alone, and check them with the model check and the book.
- [Add a Svelte Frontend](guide/add-a-svelte-frontend.md): declare a Svelte frontend on the web stack.
- [Add a Blazor Frontend](guide/add-a-blazor-frontend.md): declare a Blazor frontend outside the controlled UI contract.
- [Add an Android Frontend](guide/add-an-android-frontend.md): declare a native Android app built with .NET for Android.
- [Write a Problem](guide/write-a-problem.md): write one problem with its failure cases, evidence and resolution criteria.
- [Write a Feature Under a Module](guide/write-a-feature-under-a-module.md): write one feature with its requirements under its module.
- [Write Code for a Use Case](guide/write-code-for-a-use-case.md): where each element goes, the mark it carries, and what checks it.
- [Build an Audit Trail](guide/audit-trail.md): the audit extension on the .NET LiteBus pipeline, step by step.

## LiteSpec's Own Records

This group holds the `intent` folder: the research and decisions behind this release.

### Research and Decisions

The reasoning behind the provisions, kept as consumer-shaped records. These records inform the provisions and settle nothing themselves.

- [Research and Decisions overview](intent/README.md): the research and decisions behind this release, and how provisions cite them.

Consumers pin a complete release. `CHANGELOG.md` describes the current release.
