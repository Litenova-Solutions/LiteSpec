# Multitenancy

**Stack:** any

**Concern:** application

One deployment serves independent customer organizations, and each organization keeps its own data and policy isolated from the others. A tenant is a separate customer, not a separate login.

This page states how the product resolves tenant identity from trusted context, isolates each tenant's data and policy across authorization, storage, caches, reactions, jobs, exports, diagnostics, and recovery, and records the operations that cross tenant lines. These rules cover every deployment where separate customers require isolated data or policy in one deployment, because a tenant column without these obligations leaks one customer's data to another.

## Concepts

### When These Rules Apply

These rules apply when independent customer organizations share one deployment and need isolated data or policy. One organization with many users does not trigger them: tenants are separate customers, not separate logins.

A project whose messages carry the tenant identifier names the glossary term for the tenant in the `tenant` field of the project record, such as `tenant: term/workspace`. When the field is set and a use case declares `access.tenant` of `own`, the message property carrying the tenant is implied: `Workspace` or `WorkspaceId` for `term/workspace`. The use case does not restate the property in `input`, and `litespec model check` reports `reference-unknown` for a `tenant` value that no record holds. Without the field, no tenant property is implied.

### How This Page Builds on the Baseline

No baseline provision changes under these rules. This page adds the tenant obligations the baseline does not state: identity, isolation, tenant scope, and operations across every boundary a tenant touches. For delegation, this page adds only the cross-tenant form; a delegated or temporary grant inside one tenant follows `litespec/core/access#record-a-delegated-grant-inside-one-tenant-as-a-role-assignment`.

### Related Reading

Tamper-resistant evidence for an all-tenant run follows [Audit Trail](audit.md). Entitlements for tenants on different tiers follow the `plans` extension. Tenancy support for Marten ships in the baseline persistence package; a project that isolates tenants with another provider records a decision and pins that provider in the manifest.

This page adds no record type: tenant scope applies to records every project requires, such as decisions, audit records, and exports. Every use case declares its tenant scope under `litespec/core/access#scope-every-grant-to-its-tenant-reach`. A cross-tenant delegation declares both its grants under `litespec/core/access#grant-across-tenants-only-through-declared-delegation`, and proves an allowed call and a refused call with scenarios under `litespec/core/access#prove-hierarchies-derived-trust-and-delegation-with-scenarios`.

## Standards

### Record the Tenant Adoption Decision

**ID:** `litespec/core/tenancy#record-the-tenant-adoption-decision`

**Strength:** required

**Applies to:** artifact: record:decision

**Statement:** A consumer MUST record a decision naming tenant source, storage model, isolation guarantee, administrative access, evolution path, and enumeration bound.

**Rationale:** Tenant source, storage model, isolation guarantee, administrative access, and evolution path interact across every boundary, so one decision records them together before code assumes any of them. The enumeration bound caps how many tenant identifiers an all-tenant record lists.

**Reviewed for:** Does the tenant adoption decision name the source, storage model, isolation guarantee, administrative access, evolution path, and enumeration bound? Check the decision under `.litespec/intent/decisions/`.

### Define Tenant Isolation

**ID:** `litespec/core/tenancy#define-tenant-isolation`

**Strength:** required

**Applies to:** artifact: record:decision

**Statement:** A multi-tenant decision MUST define shared-table, schema, or database isolation and state its guarantee and operating trade-off.

**Rationale:** Shared tables, separate schemas, and separate databases isolate differently and operate differently, and each choice moves work between queries, migrations, backups, and scaling. Naming the model with its guarantee and its operating trade-off tells every later choice which isolation it can rely on.

**Reviewed for:** Does the adoption decision name its isolation model with its guarantee and operating trade-off? Check the adoption decision.

### Avoid Unplanned Isolation Models

**ID:** `litespec/core/tenancy#avoid-unplanned-isolation-models`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A multi-tenant system MUST NOT mix isolation models without a decision and data evolution plan.

**Rationale:** One part of the product on shared tables and another on separate databases decide which rows each tenant sees in two different ways. Without a decision and a data evolution plan for the mix, backup, restore, and incident response each guess which guarantee covers which data.

**Reviewed for:** Does every mixed isolation model trace to a decision with a data evolution plan? Check each mixed model beside its decision.

### Resolve Tenant Identity from Trusted Context

**ID:** `litespec/core/tenancy#resolve-tenant-identity-from-trusted-context`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A trusted host boundary MUST resolve tenant identity from verified claims, host mapping, or another authenticated source.

**Rationale:** The tenant decides which data the request may touch, so only a trusted host boundary resolves it: verified claims, host mapping, or another authenticated source. A tenant identifier the caller supplies in request data is caller-chosen, not authenticated.

**Reviewed for:** Does every trusted host boundary resolve the tenant from an authenticated source? Check each boundary's tenant resolution.

### Reject Unrestricted Tenant Input

**ID:** `litespec/core/tenancy#reject-unrestricted-tenant-input`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A tenant resolver MUST NOT accept an unrestricted tenant ID from request data.

**Rationale:** Any caller can name any tenant when the tenant ID comes directly from request data. Rejecting unrestricted input keeps the request from choosing a data boundary outside its authenticated context.

**Reviewed for:** Does any tenant resolver path accept a tenant identifier from request data? Check the tenant resolver code.

### Verify Actor and Target Tenancy

**ID:** `litespec/core/tenancy#verify-actor-and-target-tenancy`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler

**Statement:** A protected multi-tenant operation MUST confirm actor and target tenancy, with the needed permission held at the resolved scope and the target belonging to it.

**Rationale:** Checking the caller alone leaves the target unchecked. An authorized caller can still name a resource from another tenant. The operation confirms both sides through one chain: the actor holds a role for the resolved tenant, the permission names the role in `grantedTo`, the use case declares that permission in its grant, and the target belongs to the same scope.

**Reviewed for:** Does every protected operation refuse when the actor and target tenants mismatch? Check the authorization tests.

### Isolate Administrative Cross-Tenant Access

**ID:** `litespec/core/tenancy#isolate-administrative-cross-tenant-access`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler; when: cross-tenant

**Statement:** An administrative cross-tenant operation MUST require a permission the actor holds through a role whose scope is platform, writing an audit record.

**Rationale:** Support and operations sometimes reach across tenants, and that reach needs exceptional control: a permission held through a role whose scope is `platform`, with an audit record for every use. Without both, one support login gains unrestricted access to every customer's data, with no record of what it touched. Grants that let one tenant act for another follow `litespec/core/access#grant-across-tenants-only-through-declared-delegation` instead.

**Reviewed for:** Does every administrative cross-tenant use case require a platform-scope permission, with an audit record for every run? Check the use case permissions beside the audit trail.

### Scope Persistence Sessions to the Tenant

**ID:** `litespec/core/tenancy#scope-persistence-sessions-to-the-tenant`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A multi-tenant persistence implementation MUST resolve its tenant at session creation through the selected provider's own tenancy support.

**Rationale:** A session opened without a tenant serves its next query without scope. Resolving the tenant at session creation, through the selected provider's own tenancy support, scopes every query the session runs, so no query needs its own check. Filtering per query instead means any query that omits the filter reads across tenants. The baseline provider resolves the tenant from the registered tenancy at session creation; an Entity Framework Core implementation resolves it when the context is created and applies it through a query filter convention.

**Reviewed for:** Does every session-creation path resolve the tenant through its provider's own tenancy support? Check each provider's session creation.

### Scope Tenant-Owned Records

**ID:** `litespec/core/tenancy#scope-tenant-owned-records`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every tenant-owned document, query, unique constraint, cache key, outbox record, reaction, job, export, and object-storage path MUST include tenant scope.

**Rationale:** Documents, queries, unique constraints, cache keys, outbox records, reactions, jobs, exports, and object-storage paths can each carry one tenant's data into another tenant's reach. Leaving tenant scope out of any one of them discloses or mixes customer data through exactly that gap.

**Reviewed for:** Does every tenant-owned document, query, constraint, cache key, outbox record, reaction, job, export, and storage path in code carry tenant scope? Check the code for each kind.

### Apply Cross-Tenant Disclosure Policy

**ID:** `litespec/core/tenancy#apply-cross-tenant-disclosure-policy`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler; when: cross-tenant

**Statement:** A cross-tenant resource response MUST use the documented not-found or forbidden policy.

**Rationale:** A caller that names another tenant's resource forces an answer. The product either pretends the resource does not exist or admits it exists and refuses. It documents one policy and applies it to every cross-tenant response, so callers cannot probe for resources through inconsistent answers.

**Reviewed for:** Does every cross-tenant response follow the documented not-found or forbidden policy? Check the API tests.

### Protect Tenant Diagnostics

**ID:** `litespec/core/tenancy#protect-tenant-diagnostics`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Errors, logs, metrics, and traces MUST NOT reveal another tenant's business identifiers or data.

**Rationale:** Errors, logs, metrics, and traces travel to people the API never authorized, on support screens, dashboards, and log stores shared across tenants. Scrubbing other tenants' identifiers and data from diagnostics keeps the disclosure policy whole past the API boundary.

**Reviewed for:** Is every error, log, metric, and trace output free of another tenant's identifiers and data? Check the output samples in the tests.

### Scope Tenant Operations

**ID:** `litespec/core/tenancy#scope-tenant-operations`

**Strength:** required

**Applies to:** artifact: record:runbook

**Statement:** Backup, restore, replay, deletion, export, support access, and incident investigation MUST document one-tenant or all-tenant scope.

**Rationale:** Each of backup, restore, replay, deletion, export, support access, and incident investigation touches more data than one normal request. Documenting each as one-tenant or all-tenant tells the operator how much data the run touches before it starts.

**Reviewed for:** Does every backup, restore, replay, deletion, export, support, and investigation procedure document one-tenant or all-tenant scope? Check each procedure.

### Record an All-Tenant Operation

**ID:** `litespec/core/tenancy#record-an-all-tenant-operation`

**Strength:** required

**Applies to:** artifact: command-handler, job; when: cross-tenant

**Statement:** An all-tenant operation MUST record actor, operation, reason, tenant count, and reached tenant identifiers up to the adoption decision bound.

**Rationale:** A documented scope states what an operation may touch. It does not state what one run touched, and the run is what a tenant asks about after an incident. An all-tenant run is also the one case where no single tenant's audit trail holds the whole event. [The OWASP logging guidance](https://owasp.org/www-project-cheat-sheets/cheatsheets/Logging_Cheat_Sheet) treats an administrative action over many subjects as one event to record: actor, operation, reason, tenant count, and reached tenant identifiers up to the adoption decision bound. When the `audit` extension is selected, the record is an audit record; otherwise it stays a standalone record.

**Example:** A support export covering every tenant records the operator, the export, the tenant count, and the incident it was run for.

**Reviewed for:** Does every all-tenant run record the actor, operation, reason, tenant count, and reached tenant identifiers up to the adoption bound? Check the run records.

### Scope Delegated Grants to Named Permissions

**ID:** `litespec/core/tenancy#scope-delegated-grants-to-named-permissions`

**Strength:** required

**Applies to:** artifact: record:role

**Statement:** A delegation MUST name the permissions it delegates, never all permissions.

**Rationale:** A delegation open to every permission hands the whole tenant to the acting party. One pairing meant for reading reports also moves money. Naming each delegated permission bounds the pairing to what the delegator intended. Each new permission then needs its own review in the pull request before the acting party holds it.

**Reviewed for:** Does every delegation name its delegated permissions, with none opened to all permissions? Check each delegation's permission list.

### Limit Every Delegation in Time

**ID:** `litespec/core/tenancy#limit-every-delegation-in-time`

**Strength:** required

**Applies to:** artifact: record:role

**Statement:** A delegation MUST carry an expiry or a revocation path.

**Rationale:** Without an end, a grant outlives its reason: the assistant keeps acting after the engagement ends, and no date prompts anyone to reopen the pairing. An expiry ends the pairing on its date, and a revocation path ends it on demand. Either way, the grant states its own end, so the pairing ends on its terms instead of running until an incident exposes it.

**Reviewed for:** Does every delegation carry an expiry or a revocation path? Check each delegation record.

### Audit Actions Taken Under Delegation

**ID:** `litespec/core/tenancy#audit-actions-taken-under-delegation`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler; when: cross-tenant

**Statement:** An action that an actor takes under delegation MUST record the delegation grant used.

**Rationale:** An acted-for action without a marker reads exactly like the delegator's own action, and a reviewer then cannot tell who actually acted. Recording the delegation grant used puts the acting party on the record beside the delegator. `litespec/core/audit#mark-actions-taken-under-a-support-grant` states the same marker for administrative grants.

**Reviewed for:** Does every action taken under delegation record the delegation grant used? Check the audit records of delegated actions.

## Conventions

### Use Typed Tenant Identities

**ID:** `litespec/core/tenancy#use-typed-tenant-identities`

**Strength:** default

**Applies to:** artifact: any

**Default:** Represent tenant identity with a strongly typed `TenantId`.

**Replacement:** A consumer records a tenant-identity convention naming the type its tenant references carry.

**Rationale:** A bare string tenant ID passes anywhere a string passes. It can land in an actor field, an aggregate key, or a log line. A strongly typed `TenantId` turns each of those mistakes into a compilation failure instead of a production failure.

**Reviewed for:** Does every tenant reference carry the `TenantId` type, or another type the tenant-identity convention names? Check the tenant references in code.

### Use One Tenant Accessor

**ID:** `litespec/core/tenancy#use-one-tenant-accessor`

**Strength:** default

**Applies to:** artifact: any

**Default:** Use one current-tenant accessor at the trusted host boundary.

**Replacement:** A consumer records a tenant-access convention naming where its readers find the current tenant.

**Rationale:** Every boundary reads the current tenant. Two accessors can disagree about which tenant is current, so one accessor at the trusted host boundary gives every reader the same answer from the same trusted source.

**Reviewed for:** Does every trusted host boundary read the current tenant through one accessor, or through the place the tenant-access convention names? Check the tenant accessors in code.

### Pass Tenant Scope Explicitly

**ID:** `litespec/core/tenancy#pass-tenant-scope-explicitly`

**Strength:** default

**Applies to:** artifact: use-case, port

**Default:** Pass tenant scope through Application messages or trusted ports rather than ambient Domain request state.

**Replacement:** A consumer records a tenant-scope convention naming how its Domain receives tenant scope.

**Rationale:** Domain logic that reads the tenant from ambient request state depends on the host to run at all, which complicates tests and reuse. Passing the scope through Application messages or trusted ports keeps the tenant visible in the call and the Domain free of host state.

**Reviewed for:** Is the Domain free of ambient tenant state reads, or does the tenant-scope convention name how the Domain receives scope? Check the Domain code.

### Record Delegation Grants with Roles

**ID:** `litespec/core/tenancy#record-delegation-grants-with-roles`

**Strength:** default

**Applies to:** artifact: record:role

**Default:** Record each delegation grant on the role page that holds it.

**Replacement:** A consumer records a delegation-records convention naming where its delegation grants live.

**Rationale:** Each grant qualifies its role: the delegator role names who may be acted for, and the acting role names who may act. Recording the grant on its role page keeps the pairing beside the permission it extends, so a review reads both at once.

**Reviewed for:** Does every delegation grant sit on its role page, or where the delegation-records convention names? Check the role pages beside that convention.
