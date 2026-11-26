# Overview

LiteSpec's baseline provisions cover every project. An extension is a further bundle of provisions that applies only when a specific need appears, from delivery and compatibility to plans, live updates, and model-assisted queries. Each bundle stays inactive until its criterion holds, so a project carries no provision it does not need.

When the baseline does not cover a need, read the candidate page and select only the extensions whose criteria hold today.

## How to Select an Extension

Each extension page states its activation criterion and whether it applies to the whole project or to named records. Select every extension whose criteria hold, and none whose criteria do not.

List each selection in `selectedExtensions` in `.litespec/project.yaml`, with the criterion it met. The manifest repeats each activation scope, replacement list, and switched-on record type where checks read it.

## Project Extensions

Selected once for the whole project:

- [AI and Model Context Protocol](ai-mcp.md): model-mediated queries with a closed tools set, structure-only scenarios, and a pinned stub, plus the server a product ships so an outside model can call its public API.
- [API Compatibility](compat.md): versioning and retirement for APIs with public, partner, mobile, or independently deployed consumers.
- [Delivery](delivery.md): release readiness gates, image build and rollout, deployment and rollback, backup and restore, alerting, and the supply-chain gate for a product operated in production.
- [Plans and Entitlements](plans.md): plans, entitlements, and feature gating for a product that sells tiers.

## Local Extensions

Selected per record, for the use cases, entry points, jobs, flows, modules, rules, or aggregates the criterion names:

- [Live Delivery](realtime.md): live transport for updates a measured target requires faster than polling.
