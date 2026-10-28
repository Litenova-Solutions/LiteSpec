# Multitenancy

## Intent

Multitenancy lets independent customer organizations share one deployment while each keeps isolated data and policy. This page states how the product resolves tenant identity from trusted context, isolates each tenant's data and policy across authorization, storage, caches, jobs, exports, diagnostics, and recovery, and records the operations that cross tenant lines. A project selects it when separate customers require isolated data or policy in one deployment, because a tenant column without these obligations leaks one customer's data to another.

## Activation

Activation scope: `project`.

Applicable specification kinds: None.

The project selects `tenancy` only when independent customer organizations share one deployment and need isolated data or policy. One organization with many users does not select it: tenants are separate customers, not separate logins.

## Baseline Relationship

This extension replaces no baseline provision. It adds tenant obligations the baseline does not state: identity, isolation, scope, and operations across every boundary a tenant touches.

It switches on no record type: tenant scope applies to records every project requires, such as decisions, audit records, and exports.

## Standards

### Record the Tenant Adoption Decision

**ID:** `litespec/ext/tenancy#record-the-tenant-adoption-decision`

**Statement:** A consumer selecting `tenancy` MUST record a decision naming tenant source, storage model, isolation guarantee, administrative access, evolution path, and enumeration bound.

**Rationale:** Tenant source, storage model, isolation guarantee, administrative access, and evolution path interact across every boundary, so one decision records them together before code assumes any of them. Approval waits while that decision stays a draft, and the enumeration bound caps how many tenant identifiers an all-tenant record lists.

**Reviewed for:** The tenant adoption decision under `docs/intent/decisions/` names source, storage model, isolation guarantee, administrative access, evolution path, and enumeration bound.

### Define Tenant Isolation

**ID:** `litespec/ext/tenancy#define-tenant-isolation`

**Statement:** A multi-tenant decision MUST define shared-table, schema, or database isolation and state its guarantee and operating trade-off.

**Rationale:** Shared tables, separate schemas, and separate databases isolate differently and operate differently: each choice moves work between queries, migrations, backups, and scaling. Naming the model with its guarantee and its operating trade-off tells every later choice which isolation it can rely on.

**Reviewed for:** The adoption decision names shared-table, schema, or database isolation with its guarantee and operating trade-off.

### Avoid Unplanned Isolation Models

**ID:** `litespec/ext/tenancy#avoid-unplanned-isolation-models`

**Statement:** A multi-tenant system MUST NOT mix isolation models without a decision and data evolution plan.

**Rationale:** One part of the product on shared tables and another on separate databases answers which rows each tenant sees two ways. Each mixed model needs its own decision with a data evolution plan, or backup, restore, and incident response each guess which guarantee covers which data.

**Reviewed for:** Each mixed isolation model traces to a decision with a data evolution plan.

### Resolve Tenant Identity from Trusted Context

**ID:** `litespec/ext/tenancy#resolve-tenant-identity-from-trusted-context`

**Statement:** A trusted host boundary MUST resolve tenant identity from verified claims, host mapping, or another authenticated source.

**Rationale:** The tenant decides which data the request may touch, so only a trusted host boundary resolves it: verified claims, host mapping, or another authenticated source. An identity the caller supplies itself is a choice, not a fact.

**Reviewed for:** Each trusted host boundary resolves the tenant from verified claims, host mapping, or another authenticated source.

### Reject Unrestricted Tenant Input

**ID:** `litespec/ext/tenancy#reject-unrestricted-tenant-input`

**Statement:** A tenant resolver MUST NOT accept an unrestricted tenant ID from request data.

**Rationale:** A tenant ID taken straight from request data lets any caller name any tenant. Rejecting unrestricted input keeps the request from choosing a data boundary outside its authenticated context.

**Reviewed for:** The tenant resolver code accepts a tenant identifier only from an authenticated source, never from request data.

### Verify Actor and Target Tenancy

**ID:** `litespec/ext/tenancy#verify-actor-and-target-tenancy`

**Statement:** A protected multi-tenant operation MUST confirm actor and target tenancy, with the needed permission held at the resolved scope and the target belonging to it.

**Rationale:** Checking the caller alone leaves the target unchecked: an authorized caller can still name a resource from another tenant. The operation confirms both sides through one chain: the actor holds a role at the resolved scope, the role allows the permission, the use case declares that permission in its grant, and the target belongs to the same scope.

**Reviewed for:** The authorization tests fail a protected operation whose actor and target tenants mismatch.

### Scope Every Use Case to Its Tenant Reach

**ID:** `litespec/ext/tenancy#scope-every-use-case-to-its-tenant-reach`

**Statement:** Every use case in a multi-tenant product MUST declare its tenant scope as own, cross, public, or none in `access`.

**Rationale:** The tenant scope bounds which tenant data the call may touch: own stays inside the caller tenant, cross reaches across tenants, public works beyond tenant boundaries, and none touches no tenant data. Without the scope, a read inside one tenant and a read across all tenants look the same. `litespec/core/access#scope-every-grant-to-its-tenant-reach` states the values, and `litespec/core/access#review-cross-tenant-access-as-tier-1` routes cross scope to the security queue.

**Example:** An expense report declares `own`, a finance search across companies declares `cross`, a public catalog declares `public`, and a health check declares `none`.

**Reviewed for:** Each use case declares own, cross, public, or none in its access tenant scope, with cross grants in the security queue.

### Isolate Administrative Cross-Tenant Access

**ID:** `litespec/ext/tenancy#isolate-administrative-cross-tenant-access`

**Statement:** An administrative cross-tenant operation MUST require a permission the actor holds through a role at platform scope, writing an audit record.

**Rationale:** Support and operations sometimes reach across tenants, and that exceptional reach needs exceptional control: a permission held through a role at platform scope, with an audit record for every use. Without both, one support login becomes an unscoped key to every customer's data. Grants that let one tenant act for another follow the delegation extension instead.

**Reviewed for:** Each administrative cross-tenant use case names a platform-scope permission, and the audit trail holds a record for every cross-tenant run.

### Use Selected Tenant Storage Support

**ID:** `litespec/ext/tenancy#use-selected-tenant-storage-support`

**Statement:** A multi-tenant persistence implementation MUST resolve its tenant at session creation through the selected provider's own tenancy support.

**Rationale:** Session creation is the point that matters: a session opened without a tenant serves an unscoped next query. Resolving the tenant then, through the selected provider's own tenancy support, scopes every query the session runs, so no query carries the concern alone. Adding the filter per query instead leaves every new query one omission away from reading across tenants. The baseline provider resolves the tenant from the registered tenancy at session creation; an Entity Framework Core implementation resolves it when the context is created and applies it through a query filter convention.

**Reviewed for:** The session-creation code of each provider resolves the tenant through that provider's tenancy support.

### Scope Tenant-Owned Records

**ID:** `litespec/ext/tenancy#scope-tenant-owned-records`

**Statement:** Every tenant-owned document, query, unique constraint, cache key, outbox record, job, export, and object-storage path MUST include tenant scope.

**Rationale:** Documents, queries, unique constraints, cache keys, outbox records, jobs, exports, and object-storage paths each form a boundary where tenants meet. Leaving tenant scope out of any one of them discloses or mixes customer data through exactly that gap.

**Reviewed for:** The documents, queries, constraints, cache keys, outbox records, jobs, exports, and storage paths in code each carry tenant scope.

### Apply Cross-Tenant Disclosure Policy

**ID:** `litespec/ext/tenancy#apply-cross-tenant-disclosure-policy`

**Statement:** A cross-tenant resource response MUST use the documented not-found or forbidden policy.

**Rationale:** A caller that names another tenant's resource forces an answer: pretend it does not exist, or admit it exists and refuse. The product documents one policy and applies it to every cross-tenant response, so callers cannot probe for resources through inconsistent answers.

**Reviewed for:** Each cross-tenant response follows the documented not-found or forbidden policy, shown in the API tests.

### Protect Tenant Diagnostics

**ID:** `litespec/ext/tenancy#protect-tenant-diagnostics`

**Statement:** Errors, logs, metrics, and traces MUST NOT reveal another tenant's business identifiers or data.

**Rationale:** Errors, logs, metrics, and traces travel to readers the API never authorized: support screens, dashboards, and log stores shared across tenants. Scrubbing other tenants' identifiers and data from diagnostics keeps the disclosure policy whole past the API boundary.

**Reviewed for:** The error, log, metric, and trace outputs hold no other tenant's identifiers or data.

### Scope Tenant Operations

**ID:** `litespec/ext/tenancy#scope-tenant-operations`

**Statement:** Backup, restore, replay, deletion, export, support access, and incident investigation MUST document one-tenant or all-tenant scope.

**Rationale:** Backup, restore, replay, deletion, export, support access, and incident investigation each touch more data than one normal request. Documenting each as one-tenant or all-tenant tells the operator how much data the run touches before it starts.

**Reviewed for:** The backup, restore, replay, deletion, export, support, and investigation procedures each state one-tenant or all-tenant scope.

### Record an All-Tenant Operation

**ID:** `litespec/ext/tenancy#record-an-all-tenant-operation`

**Statement:** An all-tenant operation MUST record actor, operation, reason, tenant count, and reached tenant identifiers up to the adoption decision bound.

**Rationale:** A documented scope states what an operation may touch, not what one run touched, and the run is what a tenant asks about after an incident. An all-tenant run is also the one case where no single tenant's audit trail holds the whole event. [The OWASP logging guidance](https://owasp.org/www-project-cheat-sheets/cheatsheets/Logging_Cheat_Sheet) treats an administrative action over many subjects as one event to record: actor, operation, reason, tenant count, and reached tenant identifiers up to the adoption decision bound. When the audit extension is selected, the record is an audit record; otherwise it stays a standalone record.

**Example:** A support export covering every tenant records the operator, the export, the tenant count, and the incident it was run for.

**Reviewed for:** The record of each all-tenant run names actor, operation, reason, tenant count, and reached tenant identifiers up to the adoption bound.

### Declare Delegation Grants on Both Sides

**ID:** `litespec/ext/tenancy#declare-delegation-grants-on-both-sides`

**Statement:** A delegation MUST declare the delegator grant naming who may be acted for and the acting grant naming who may act.

**Rationale:** Delegation lets one actor act for another, so a grant on one side alone leaves the other side unreviewed. The delegator grant names the role at its scope that may be acted for and the permissions open to delegation; the acting grant names the role permitted to act and the pairing it acts under. `litespec/core/access#grant-across-tenants-only-through-declared-delegation` requires both declarations.

**Example:** An assistant acts for a manager only where the manager delegation grant and the assistant acting grant both name the pairing.

**Reviewed for:** Each delegation declares a delegator grant naming who may be acted for and an acting grant naming who may act.

### Scope Delegated Grants to Named Permissions

**ID:** `litespec/ext/tenancy#scope-delegated-grants-to-named-permissions`

**Statement:** A delegation MUST name the permissions it delegates, never all permissions.

**Rationale:** A delegation open to every permission hands the whole tenant to the acting party: one pairing meant for reading reports also moves money. Naming each delegated permission bounds the pairing to what the delegator intended, and each new permission needs its own review before the acting party holds it.

**Reviewed for:** Each delegation names its delegated permissions, with none open to all permissions.

### Bound Every Delegation in Time

**ID:** `litespec/ext/tenancy#bound-every-delegation-in-time`

**Statement:** A delegation MUST carry an expiry or a revocation path.

**Rationale:** A grant with no end outlives its reason: the assistant keeps acting after the engagement ends, and nobody reopens the pairing because no date asks for it. An expiry ends the pairing on its date, and a revocation path ends it on demand. Either way, the end is stated beside the grant instead of left for an incident to discover.

**Reviewed for:** Each delegation carries an expiry or a revocation path.

### Audit Actions Taken Under Delegation

**ID:** `litespec/ext/tenancy#audit-actions-taken-under-delegation`

**Statement:** An action that an actor takes under delegation MUST record the delegation grant used.

**Rationale:** An acted-for action without a marker reads exactly like the delegator's own action, so a reviewer cannot tell who actually acted. Recording the delegation grant used puts the acting party on the record beside the delegator. `litespec/ext/audit#record-delegated-administrative-access` states the same marker for administrative grants.

**Reviewed for:** Each action under delegation records the delegation grant used.

### Prove Delegated Calls with Scenarios

**ID:** `litespec/ext/tenancy#prove-delegated-calls-with-scenarios`

**Statement:** A delegation MUST prove an allowed call and a refused call with scenarios.

**Rationale:** A pairing described only in prose hides where its grant stops: the allowed assistant and the uninvited one read alike. A scenario walking an allowed call and a scenario ending a call without delegation in the derived forbidden result pin the boundary as checked facts. `litespec/core/access#prove-hierarchies-derived-trust-and-delegation-with-scenarios` requires both scenarios.

**Reviewed for:** Each delegation proves an allowed call and a refused call with scenarios.

### Review Delegated Cross-Tenant Grants as Tier 1

**ID:** `litespec/ext/tenancy#review-delegated-cross-tenant-grants-as-tier-1`

**Statement:** A use case acting under delegation across tenants MUST carry tier 1 review through the security queue.

**Rationale:** A delegated call reads or writes data that belongs to another tenant, which is the highest access risk a grant carries. Tier 1 review through the security queue puts that grant before the security reviewer separately from reviewed status, so no delegated cross-tenant act ships on a developer sample alone. `litespec/core/access#review-cross-tenant-access-as-tier-1` routes every cross-tenant use case there.

**Reviewed for:** Each use case acting under delegation across tenants carries tier 1 review through the security queue.

## Conventions

### Use Typed Tenant Identities

**ID:** `litespec/ext/tenancy#use-typed-tenant-identities`

**Default:** Represent tenant identity with a strongly typed `TenantId`.

**Replacement:** A consumer records a tenant-identity convention naming the type its tenant references carry.

**Rationale:** A bare string tenant ID passes anywhere a string passes: into an actor field, an aggregate key, or a log line. A strongly typed `TenantId` fails compilation at each of those mistakes instead of failing in production.

**Reviewed for:** Tenant references carry the `TenantId` type, or the tenant-identity convention names another type.

### Use One Tenant Accessor

**ID:** `litespec/ext/tenancy#use-one-tenant-accessor`

**Default:** Use one current-tenant accessor at the trusted host boundary.

**Replacement:** A consumer records a tenant-access convention naming where its readers find the current tenant.

**Rationale:** Every boundary reads the current tenant, and two accessors can disagree about which tenant is current. One accessor at the trusted host boundary gives every reader the same answer from the same trusted source.

**Reviewed for:** One current-tenant accessor serves the trusted host boundary, or the tenant-access convention names where readers find the tenant.

### Pass Tenant Scope Explicitly

**ID:** `litespec/ext/tenancy#pass-tenant-scope-explicitly`

**Default:** Pass tenant scope through Application messages or trusted ports rather than ambient Domain request state.

**Replacement:** A consumer records a tenant-scope convention naming how its Domain receives tenant scope.

**Rationale:** Domain logic that reads the tenant from ambient request state depends on the host to run at all, which complicates tests and reuse. Passing the scope through Application messages or trusted ports keeps the tenant visible in the call and the Domain free of host state.

**Reviewed for:** Domain code reads no ambient tenant state, or the tenant-scope convention names how the Domain receives scope.

### Record Delegation Grants with Roles

**ID:** `litespec/ext/tenancy#record-delegation-grants-with-roles`

**Default:** Record each delegation grant on the role page that holds it.

**Replacement:** A consumer records a delegation-records convention naming where its delegation grants live.

**Rationale:** The grant qualifies the role: the delegator role names who may be acted for, and the acting role names who may act. Recording each grant on its role page keeps the pairing beside the permission it extends, so a review reads both at once.

**Reviewed for:** Each delegation grant sits on its role page, or the delegation-records convention names where grants live.

## Dependencies

Packages: Tenancy support for Marten ships in the baseline persistence package. A project that isolates tenants with another provider records a decision and pins that provider in the manifest.

Related extensions: The `audit` extension owns tamper-resistant evidence when an all-tenant run needs an audit record. The `plans` extension owns entitlements when tenants buy different tiers. The `delegation` extension owns grants that let one tenant act for another.

Guides: None.
