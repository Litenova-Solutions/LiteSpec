# Product Evolution

**Stack:** any

**Concern:** operations

A live product changes after launch: fields are added and renamed, routes move, permissions tighten, and modules grow until they split. This page states how a team makes those four changes without breaking stored data, saved links, active roles, or running jobs. It applies from the first production release onward, alongside the release gate on the [release page](release.md). It is for the team planning a change to a product already serving users.

The product keeps no compatibility layer between its own releases, and a consumer keeps no compatibility layer between its own versions unless the `compat` extension states one. Each change below keeps old data readable and old addresses reachable by explicit steps the team reviews, not by hidden fallbacks.

## Concepts

### When This Page Applies

This page covers four changes to a product already serving users. It covers adding a field to an aggregate, renaming a field on an aggregate, moving or renaming a web route, adding a permission or tightening an existing one, and splitting one module into two. It leaves out adopting a new LiteSpec release itself, which the release record and the recorded `reviewedLiteSpecVersion` carry.

### How Stored Documents Stay Readable

Marten stores each aggregate root as the JSON the code wrote at the time, and a code change rewrites no rows already on disk. Schema objects and document JSON are two different things: the store migrates tables and indexes from the model, while old JSON stays as written until the code reads it. The [persistence page](backend-persistence.md) states the split, and the [.NET persistence page](../packs/dotnet/persistence.md) states how it binds for Marten.

A new optional field reads as missing on old rows, so the reader supplies its default. Claimwick adds an optional receipt note to its expense aggregate with a default old rows read as empty. A renamed field reads under two names for one release window, so a converter or an upcaster maps the old name to the new shape before deserialization finishes. Claimwick renames a flat amount field into a money value with its currency, and a converter reads both the old number and the new object during the release window.

A frozen JSON snapshot per aggregate root and outbox envelope, recorded at each consumer release, proves the mapping: the build deserializes every prior snapshot through the real serializer and fails when a change breaks a stored form without a registered step.

### How Routes Stay Stable

People save links, and other systems link to public addresses. A route move keeps the old address as a permanent redirect for at least one retention window, and the sitemap names the new address as the canonical one. The [information architecture page](ui-ia.md) states the sitemap and the jobs table each route is checked against.

A redirect names its source, its target, and its reason in one reviewed list, so a later cleanup removes the entries no traffic uses alone. The Claimwick expense detail keeps `/expenses/{id}` as its canonical address, and an older `/expense/{id}` address redirects to it with a permanent status until the access logs show no use.

### How Permissions Stay Safe

Permissions name what a role does, and use cases declare the one grant they need. Adding a permission starts in audit mode: the pipeline logs which callers the new rule refuses while still allowing them, so the team sees the affected roles before enforcement starts. Tightening an existing permission follows the same path: log first, then enforce, with a named owner for each affected role. Claimwick tightens the approve-expense grant to managers in one workspace, logs the employees the new rule refuses for one week, updates their role assignments, and then enforces the rule.

The [access page](access.md) states actors, roles, permissions, and entry points. A permission change lists each affected role, the use cases declaring its grant, and the migration for existing assignments, so no active employee or manager loses access on deploy.

### How Modules Split Cleanly

A module holds one business capability with its domain, its use cases, its infrastructure, and its entry points. A split moves whole aggregates with their use cases, reactions, jobs, read models, and contracts together, and leaves no shared table behind. The [backend architecture page](backend-architecture.md) states the module layout the split preserves. A split moving half an aggregate leaves shared tables and private calls across the new boundary, and the two modules then change together.

The split runs as a sequence the team reviews: declare the new module, move the records and code with their marks intact, point cross-module reads at the new public query or decision port, move the stored documents with a storage migration, and retire the old paths. Claimwick splits payouts out of spending by moving the payout aggregate with its issue and reimburse use cases, its contracts, and its jobs into the new module, while spending reads payout state through the new public query alone. Each step keeps `model check` green, so the split lands as no large unverified move.

### Related Reading

The [backend persistence page](backend-persistence.md) states repositories, the single commit, and the duties on stored data. The [backend application page](backend-application.md) states use-case coordination, decision ports, and read models for cross-module reads. The [access page](access.md) states the permission records a tightening changes. The [release page](release.md) states the gate each change passes before it ships.

## Standards

None.

## Conventions

None.
