# Overview

## Intent

The extensions pages hold conditional provision bundles a project adds only when a specific need appears, from caching and audit trails to multitenancy and delivery, so a project carries no provision it does not need. Each bundle stays inactive until a current need meets the criterion its page states, and the project records each choice with its criterion. A team facing a need the default provisions do not cover reads the candidate page and selects only the extensions whose criteria hold today.

## How to Select an Extension

Each extension below states its own activation criterion and whether it applies to the whole project or to named records. A project selects every extension whose criteria hold and no extension whose criteria do not, and lists each selection in `selectedExtensions` in `.litespec/project.yaml` with the criterion it met. The manifest repeats each activation scope, replacement list, and switched-on record type where checks read it. Most extensions add provisions without replacing any; the audit and outbox pages each name the one baseline provision they replace.

## Project Extensions

Selected once for the whole project:

- [AI Assistance](ai.md): closed tool sets, structure-only scenarios, and pinned stubs for queries a model mediates.
- [API Compatibility](compat.md): versioning and retirement for APIs with public, partner, mobile, or independently deployed consumers.
- [Audit Trail](audit.md): tamper-resistant evidence of who attempted what in data another party owns.
- [Customer Rules and Hooks](customer-rules.md): customer-composed rules and hooks for a product whose tenants automate their own work.
- [Delivery](delivery.md): release readiness gates, image build and rollout, deployment and rollback, backup and restore, alerting and the supply-chain gate for a product operated in production.
- [Localization](locale.md): locales, catalogs, and formatting for a product that ships in more than one locale.
- [Model Context Protocol](mcp.md): the server a product ships so an LLM client can call its public API.
- [Multitenancy](tenancy.md): tenant isolation across authorization, storage, jobs, and recovery for one shared deployment.
- [Plans and Entitlements](plans.md): plans, entitlements, and feature gating for a product that sells tiers.
- [Release Stage](stage.md): the pre-customer-data stage, what it defers, and the promotion that ends it.

## Local Extensions

Selected per record, for the use cases, entry points, jobs, journeys, modules, rules, or aggregates the criterion names:

- [Caching](cache.md): staleness contracts and recovery for a repeated expensive read that traces or load tests name.
- [Concurrency and Idempotency](concurrency.md): versions, client keys, and fingerprints where writes can overlap or retries can duplicate an effect.
- [Connected Devices](devices.md): protocol blocks with messages, version rules, and offline uploads for entry points that devices reach.
- [Data Lifecycle](lifecycle.md): retention, deletion, archival, and restore behavior for records with a lifetime duty.
- [External Integrations](integrations.md): provider isolation, failure mapping, and boundary tests for use cases that call outside systems.
- [Durable Delivery](outbox.md): one-transaction commits with dispatched messages where delivery must survive process failure.
- [Jobs and Queues](jobs.md): recurring, delayed, and heavy work, and human-cleared queues, with leases that survive restarts.
- [Live Delivery](realtime.md): live transport for updates a measured target requires faster than polling.

## Stack Extensions

Extensions that bind to one stack live beside that stack:

- [.NET extensions overview](../backend/dotnet/ext/README.md): the BDD scenarios and the layer projects only a .NET backend offers.
