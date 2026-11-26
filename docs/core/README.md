# Overview

The Core pages state what every LiteSpec project shares: the principles, the record layers, the records that hold Intent and Behavior, and the rules for agents, writing, and releases. Read them in the order below when starting a project, then load only the pages each task touches.

## Reading Order

- [System](system.md): what LiteSpec coordinates and the parts it joins together.
- [Scope](scope.md): the one size, one shape, and stacks each project selects.
- [Stacks](stacks.md): what selecting a stack applies, and what each stack folder holds.
- [Provision Scopes](scopes.md): the stack, layer, artifact, and strength each provision carries, and the scope block each page declares.
- [Principles](principles.md): the founding rules that settle choices no narrower page covers.
- [Model](model.md): the record types, their links, and what the builder computes.
- [Waivers](waivers.md): the one record that sets a provision, a page, or a finding code aside, with its kind, its reason and, for a gap, its expiry and closing records.
- [Layers](layers.md): the four record layers and the translation rules between them.
- [Intent](intent.md): where and in what form a project states its business intent.
- [Behavior](behavior.md): the overview of the five Behavior pages and the mapping from records to design.
- [Domain Model](domain-model.md): the modules, glossary, aggregates, rules, and domain events inside the bounded context.
- [Use Cases](use-cases.md): the commands, queries, results, scenarios, and settings.
- [Automation](automation.md): the reactions and jobs that run without a request.
- [Edges and Screens](edges.md): the integration events, notifications, entry points, screens, and external systems.
- [Backend Architecture](backend-architecture.md): the module layout, the direction references point, and the modules, aggregates, and use cases inside each layer.
- [Backend Domain](backend-domain.md): aggregates, lifecycles, closed value sets, and repositories, and what Domain never references.
- [Backend Application](backend-application.md): use-case coordination, pipeline stages, ports, reactions, and jobs.
- [Backend Persistence](backend-persistence.md): repositories, the read session, the one commit per command, and the obligations on stored data.
- [HTTP API](backend-api.md): endpoints, routes, errors, collection paging, and the generated OpenAPI contract.
- [Knowledge](knowledge.md): the knowledge areas, their models, and the reference data each area reads through.
- [Backend Testing](testing.md): the test suites each project size keeps and what each suite may use.
- [Privacy and Retention](privacy.md): the data classes with their erasure positions and retention.
- [Access](access.md): the actors, roles, permissions, entry points, and external systems that govern who may do what and how callers reach the product.
- [Audit Trail](audit.md): tamper-resistant evidence of who attempted what in data another party owns.
- [Multitenancy](tenancy.md): tenant isolation across authorization, storage, jobs, and recovery for one shared deployment.
- [Concurrency and Idempotency](concurrency.md): conflicting writes, caller versions, client keys, and fingerprints.
- [External Integrations](integrations.md): provider isolation, failure mapping, and boundary tests for use cases that call outside systems.
- [Durable Delivery](outbox.md): one-transaction commits with dispatched messages where delivery must survive process failure.
- [Scheduled Jobs and Reports](jobs.md): recurring, delayed, and heavy work, and the reports too heavy for a request.
- [Data Lifecycle](lifecycle.md): retention, deletion, archival, and restore behavior for records with a lifetime duty.
- [Localization](locale.md): locales, catalogs, and formatting for a product that ships in more than one locale.
- [User Interface Information Architecture](ui-ia.md): the sitemap, the route decomposition table, the screen budgets, and the six locked page archetypes.
- [User Interface Verification](ui-verification.md): the four-stage workflow, the golden archetype screens, the geometry sensors, and the visual critic.
- [Agent](agent.md): how agents select context, resolve standards, and report completion.
- [Authoring](authoring.md): page styles, provision contracts, identifiers, and prose style; what `litespec-maintain validate` checks.
- [Record Language](language.md): the grammar for every field name, value, record name, finding code, and tool name a project writes or reads.
- [Writing](writing.md): the prose standard for every record value, title, explanation file, documentation page, comment, changelog, and decision record a project writes.
- [Maintaining](maintaining.md): the maintainer manual for provision lifecycle, renames, citations, and releases.
- [Designing](designing.md): the design rules for LiteSpec itself: one thing per record, a folder for a record with its own reasoning, a reader for every shape, and a shape changed everywhere at once.
- [Release](release.md): what a release ships and what its evidence shows.
