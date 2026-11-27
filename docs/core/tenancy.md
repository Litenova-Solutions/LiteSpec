# Multitenancy

**Stack:** any

**Concern:** application

One deployment serves independent customer organizations, and each organization keeps its data and policy isolated from the others. A tenant is a separate customer rather than a separate login. This page states how the product resolves tenant identity from trusted context, isolates tenant data and policy across authorization, storage, caches, reactions, jobs, exports, diagnostics, and recovery, and records the operations crossing tenant lines.

## Concepts

### When These Rules Apply

These rules apply where independent customer organizations share one deployment with isolated data or policy. One organization with many users is outside them: tenants are separate customers rather than separate logins. A project serving many customers states it in the `tenancy` block of `.litespec/project.yaml`: the `criterion` saying why one deployment serves many separate customers, and the `tenant` naming the glossary term for one tenant, such as `tenant: term/workspace`.

Where the block names a tenant and a use case declares `access.tenant` of `own`, the message property carrying the tenant is implied: `Workspace` or `WorkspaceId` for `term/workspace`. The use case leaves the property out of `input`. A `tenant` value no record holds is reported as `reference-unknown`, and with the block absent no tenant property is implied.

### How Tenant Identity Is Resolved

The tenant decides which data each request touches. A trusted host boundary resolves it from verified claims, host mapping, or another authenticated source. A tenant identifier from request data is caller-chosen rather than authenticated, so the resolver takes no unrestricted tenant ID from request data.

### How the Product Isolates Tenants

The tenant adoption decision records source, storage model, isolation guarantee, administrative access, evolution path, and enumeration bound together before code assumes any of them. The decision defines shared-table, schema, or database isolation with its guarantee and operating trade-off. Each of those choices moves work between queries, migrations, backups, and scaling.

One part of the product on shared tables and another on separate databases decide which rows each tenant sees in two different ways. A mix without a decision and a data evolution plan leaves backup, restore, and incident response guessing which guarantee covers which data. The product therefore holds one isolation model, or records the mix with its evolution plan.

Persistence resolves the tenant at session creation through the selected provider tenancy support. The baseline provider resolves it from the registered tenancy at creation, and an Entity Framework Core implementation resolves it where the context is created and applies a query filter convention. Each query the session runs is thus scoped, with no per-query check carrying the boundary.

Each tenant-owned document, query, unique constraint, cache key, outbox record, reaction, job, export, and object-storage path includes tenant scope in code. Errors, logs, metrics, and traces travel past the API boundary to shared screens, dashboards, and log stores. Scrubbing other tenant identifiers and data from them keeps the disclosure policy whole past the API.

Cross-tenant responses follow one documented policy everywhere: the resource is pretended absent or admitted with refusal. Backup, restore, replay, deletion, export, support access, and incident investigation each document one-tenant or all-tenant scope. The document tells the operator how much data the run touches before it starts.

### Cross-Tenant Operations and Delegation

Support and operations reach across tenants under exceptional control: a permission held through a role of `platform` scope, with an audit record for each use. Grants letting one tenant act for another follow declared delegation instead. An all-tenant run records actor, operation, reason, tenant count, and the reached tenant identifiers up to the adoption bound, as an audit record where the audit extension applies and as a standalone record otherwise.

Delegation names the permissions it delegates, with no pairing opened to all permissions. Each new permission then passes its own review in the pull request before the acting party holds it. Each delegation carries an expiry or a revocation path, so the pairing ends on its terms instead of running until an incident exposes it.

An action under delegation records the delegation grant used, putting the acting party on the record beside the delegator. Inside one tenant, delegation lives on a role assignment with a validity window plus the bound scope instance, granted and revoked by its own use cases. Scenarios prove an allowed call and a refused call for each pairing.

## Standards

### Define Tenant Isolation

**ID:** `litespec/core/tenancy#define-tenant-isolation`

**Strength:** required

**Applies to:** artifact: record:decision

**Statement:** A multi-tenant decision MUST define shared-table, schema, or database isolation and state its guarantee and operating trade-off.

**Rationale:** Shared tables, separate schemas, and separate databases isolate differently and operate differently. Each choice moves work between queries, migrations, backups, and scaling. Naming the model with its guarantee and its operating trade-off tells each later choice which isolation it relies on.

**Reviewed for:** Does the adoption decision name its isolation model with its guarantee and operating trade-off? Check the adoption decision.

### Resolve Tenant Identity from Trusted Context

**ID:** `litespec/core/tenancy#resolve-tenant-identity-from-trusted-context`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A trusted host boundary MUST resolve tenant identity from verified claims, host mapping, or another authenticated source.

**Rationale:** The tenant decides which data the request touches. A trusted host boundary resolves it from verified claims, host mapping, or another authenticated source. A tenant identifier from request data is caller-chosen rather than authenticated.

**Reviewed for:** Does every trusted host boundary resolve the tenant from an authenticated source? Check each boundary's tenant resolution.

### Reject Unrestricted Tenant Input

**ID:** `litespec/core/tenancy#reject-unrestricted-tenant-input`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A tenant resolver MUST NOT accept an unrestricted tenant ID from request data.

**Rationale:** Any caller names any tenant where the tenant ID comes directly from request data. Rejecting unrestricted input keeps the request from choosing a data boundary outside its authenticated context.

**Reviewed for:** Does any tenant resolver path accept a tenant identifier from request data? Check the tenant resolver code.

### Verify Actor and Target Tenancy

**ID:** `litespec/core/tenancy#verify-actor-and-target-tenancy`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler

**Statement:** A protected multi-tenant operation MUST confirm actor and target tenancy, with the needed permission held at the resolved scope and the target belonging to it.

**Rationale:** Checking the caller alone leaves the target unchecked. An authorized caller still names a resource from another tenant. The operation confirms both sides through one chain: the actor holds a role for the resolved tenant, the permission names the role, the use case declares that permission, and the target belongs to the same scope.

**Reviewed for:** Does every protected operation refuse when the actor and target tenants mismatch? Check the authorization tests.

### Isolate Administrative Cross-Tenant Access

**ID:** `litespec/core/tenancy#isolate-administrative-cross-tenant-access`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler; when: cross-tenant

**Statement:** An administrative cross-tenant operation MUST require a permission the actor holds through a role whose scope is platform, writing an audit record.

**Rationale:** Support and operations sometimes reach across tenants. That reach takes exceptional control: a permission held through a role of `platform` scope, with an audit record for each use. Without both, one support login opens each customer data with no record of what it touched.

**Reviewed for:** Does every administrative cross-tenant use case require a platform-scope permission, with an audit record for every run? Check the use case permissions beside the audit trail.

### Scope Persistence Sessions to the Tenant

**ID:** `litespec/core/tenancy#scope-persistence-sessions-to-the-tenant`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A multi-tenant persistence implementation MUST resolve its tenant at session creation through the selected provider's own tenancy support.

**Rationale:** A session opened without a tenant serves its next query without scope. Resolving the tenant at session creation scopes each query the session runs, so no query needs its own check. Filtering per query instead means any query omitting the filter reads across tenants.

**Reviewed for:** Does every session-creation path resolve the tenant through its provider's own tenancy support? Check each provider's session creation.

### Scope Tenant-Owned Records

**ID:** `litespec/core/tenancy#scope-tenant-owned-records`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Every tenant-owned document, query, unique constraint, cache key, outbox record, reaction, job, export, and object-storage path MUST include tenant scope.

**Rationale:** Documents, queries, constraints, cache keys, outbox records, reactions, jobs, exports, and storage paths each carry one tenant data into reach of another where scope is absent. Leaving scope out of any one of them discloses or mixes customer data through exactly that gap. Each kind carries tenant scope in code.

**Reviewed for:** Does every tenant-owned document, query, constraint, cache key, outbox record, reaction, job, export, and storage path in code carry tenant scope? Check the code for each kind.

### Limit Every Delegation in Time

**ID:** `litespec/core/tenancy#limit-every-delegation-in-time`

**Strength:** required

**Applies to:** artifact: record:role

**Statement:** A delegation MUST carry an expiry or a revocation path.

**Rationale:** Without an end, a grant outlives its reason. The assistant keeps acting after the engagement ends, with no date prompting anyone to reopen the pairing. An expiry ends the pairing on its date, and a revocation path ends it on demand.

**Reviewed for:** Does every delegation carry an expiry or a revocation path? Check each delegation record.

## Conventions

None.
