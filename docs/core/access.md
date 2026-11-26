# Access

**Stack:** any
**Concern:** records

Access states who calls the product, which calls each caller may make, and how callers reach it. Actors name the parties who act, roles bundle permissions into grants with a stated reach, and each use case declares the grant and tenant reach its calls must satisfy. Entry points and external systems state how callers reach the product.

Actors and roles are product decisions, so they live in Intent where users can see them. Permissions, entry points, and external systems are access design, so they live in Behavior.

The [use cases](use-cases.md) page states the use cases that carry access, and the [project model](model.md) states the shared record rules.

## Concepts

### How Actor, Role, and Permission Connect

Access follows one chain from actor to use case. An actor holds a role, the role carries its one scope, each permission names the roles that grant it, and the use case declares the grant it checks. When someone asks why a call was allowed or refused, the chain gives the whole answer: which actor held which role, what reach the role's scope states, which permission named that role, and which use case declared it.

This is role-based access control: permissions attach to roles, never to actors directly, and the role's scope bounds how far each permission reaches. Every link points from the lower or later record to the higher or earlier one: the permission lives in Behavior and names its Intent roles in `grantedTo`, and the book gathers each role's permissions from the permissions naming it.

### Actors, Roles, and Scopes

An actor is one party that acts in the product: a person acting directly, an organization, a company or group acting as one party, or an outside system standing in a flow step. The `kind` field says which, so a reviewer knows who answers for what the actor does. Each actor also states in `belongsToTeam` whether it belongs to the team that builds and runs the product. The actor states its responsibilities in `responsibilities`, the acts the product holds it to, and what the product assumes it knows in `knowledge`.

An outside system that acts in a flow is an actor of kind `system`, and its external-system record names it in `represents`. Internal machinery gets no actor at all.

A role is one named set of permissions granted together, and it holds the one scope of its grant. The scope is a kebab-case level naming how far the grant reaches, such as `platform`, `tenant`, or `self`, and the role is the only place that reach is stated. The role states what the grant does not cover in `excludes`, the acts inside its scope that the grant does not cover. Each scope level a role names has a glossary term that states what the level covers in this product, such as every business in one installation.

An actor lists each role it holds in `holds`, by role identifier. A hold names only the role: the reach comes from the role's scope, and one actor's hold cannot widen or narrow it.

Which tenant or team a person holds a role for is runtime data, not record data. A role assignment binds a scope instance beside the level: the tenant or team the hold reaches, or the person for a `self` grant. A role reaches the same level wherever an actor holds it, so a permission that two roles grant at different levels takes its reach from the role the caller holds.

### Permissions

A permission is one allowed action, and each module lists its own in `modules/<m>/permissions.yaml`. Each entry states in `meaning`, in one sentence, what it allows, and names in `grantedTo` the roles that grant it. Roles are granted permissions from these lists, and use cases cite them in their grants, so the lists are the one vocabulary both sides share.

A permission name opens with a verb that says what the grant does: a verb from the use-case verb table on the [use cases](use-cases.md) page, a glossary verb of kind command or either, or `manage`. An optional `group` orders the permission beside its peers in role editors.

The `risk` field marks a sensitive permission, and it is the only risk field any record carries: a permission that guards a query or use case disclosing personal or financial data carries the `personal-data` or `access` risk, just like a permission for a write.

A permission allows exactly the use cases whose `access` grant names it. No permission implies another by its name, and `manage-` is a naming convention only. When roles must differ on a destructive use case, that use case takes its own permission, and the permission carries the use case's risk. A permission set may carry `excludes` stating what the set does not allow, one sentence per exclusion. The explanation restates Boundaries from `excludes`.

### What the Access Field Declares

Each use case declares `access` in three parts: the grant the call must satisfy, the tenant reach the call has, and, when the operation needs a fresh sign-in, the window in `recentSignInLimit`. A use case that system triggers run beside person callers states the identity it runs as in `runsAs`. The entry point the call arrives through declares `authentication`, how the caller proves identity. The entry point authenticates, and the use case authorizes.

The `authentication` list on an entry point names one or more of `session`, `token`, `device`, `link`, `app`, or `none`.

The `grant` on a use case takes one of five forms:

- A permission such as `permission/manage-copies`, for role-held action.
- A self grant such as `self(permission/read-loans)`, which allows the permission only on records the caller owns.
- `signed-in`, for an authenticated caller with no permission.
- `anonymous`, for an open call.
- `system`, for work an internal trigger runs with no outside caller.

The `recentSignInLimit` field names a `setting/` identifier holding the sign-in window; a call whose sign-in is older ends in the derived sign-in-too-old result. The `tenant` scope is `own` for work inside the caller tenant, `cross` for work reaching across tenants, `public` for work with no tenant boundary, or `none` for work that touches no tenant data. The [use cases](use-cases.md) page states the use-case fields around access.

A use case that person callers and system triggers both run states `runsAs: system` beside its person grant: the grant covers the people, and `runsAs` admits the system identity beside them. A use case granted `system` or `anonymous` already admits the system, so it states no `runsAs`.

### Refusals That Need No Written Result

Some refusals follow from access and input rules, so they need no written result. A call with no authentication where one is needed ends in the derived unauthenticated result. A call without the grant ends in the derived forbidden result. A call whose sign-in is older than the `recentSignInLimit` window ends in the derived sign-in-too-old result. A call with input that breaks its rules ends in the derived invalid input result.

[Derive Generic Refusals Instead of Writing Them](use-cases.md#derive-generic-refusals-instead-of-writing-them) states the rule, and scenarios point at the derived results without anyone writing them. A permission allows a call, and the call still meets every rule, so holding a permission never bypasses a rule refusal. This is stated once here, not on each role.

### Tenant Reach of a Call

`own` keeps the call inside the caller tenant. `cross` reaches across tenants. `public` opens it beyond tenant boundaries. `none` states the call touches no tenant data. A call that reads or writes another tenant's data names the widest reach it needs, and the pull request that changes it is its review.

### Entitlements

An entitlement is a grant that depends on what the caller bought or configured, such as a plan limit or a policy setting. One tenant may submit expenses above a receipt threshold while another is refused, and the grant alone would read as unequal treatment.

The use case cites the setting or plan behind the grant, as [Cite the Setting or Plan Behind Every Entitlement](#cite-the-setting-or-plan-behind-every-entitlement) states, so a reviewer sees the commercial or configured reason for the difference. The cited setting states its range and setter, as the [use cases](use-cases.md) page states for settings.

### Hierarchies, Derived Trust, and Delegation

Hierarchies, derived trust, and delegation are patterns built from the same access parts, not extra grant forms. A hierarchy nests roles so a senior role is granted what its junior roles are granted. Derived trust lets one checked fact stand for another within one call. Delegation lets one actor act for another.

Each pattern proves its allowed and refused calls with scenarios, as [Prove Hierarchies Derived Trust and Delegation with Scenarios](#prove-hierarchies-derived-trust-and-delegation-with-scenarios) states. Grants that cross tenants through delegation use the tenancy extension, which states the declared grant each side carries.

### Entry Points

An entry point is how people or systems reach the product from outside. [Give Each Entry Point a Kind](edges.md#give-each-entry-point-a-kind) lists the kinds, and each entry point declares its `authentication`. A device caller authenticates with its own authentication kind, `device`, and the protocol detail for devices lives in the devices extension.

A browser entry point lists in `servedContent` the static content it serves, such as a script or a legal text: entries of `path`, `contentType`, `cacheControl`, `versioning`, and `fulfills`, so served content is content rather than a use case. Use cases name the entry points that reach them in `reachedThrough`, so every operation shows how callers arrive. An entry point also names its `audience` in `actor/...` ids, and where sessions apply its `session` lifetime and idle timeout as ISO 8601 durations. Its `reauthenticationTriggers` states when a fresh sign-in is required, and its `excludes` states what the entry point does not do. The explanation restates Audience from `audience`, Authentication from `authentication`, `session`, and `reauthenticationTriggers`, and Excludes from `excludes`.

### External Systems

An external system is one system outside the bounded context that the product exchanges messages with, such as a payment provider or a text gateway. It names the actor it represents in `represents`: an outside system that acts in a flow is an actor of kind `system`, and its system record names that actor. An integration that calls the product is an external system calling through an API entry point.

It declares each operation the product may call in `operations`, with its action, timeout, response, and cache behavior, as [Describe Every External System Operation](edges.md#describe-every-external-system-operation) states, so every outside call is a declared edge. It declares in `erasure` how personal data it holds is erased, or why erasure there does not run, with the Intent decision that accepts the limit.

### Hosted Sign-In

A hosted sign-in moves authentication to an identity provider outside the product: the product redirects the browser to the provider, the provider authenticates the person, and the product exchanges the returned code for tokens through its backend for frontend. The protocol is OpenID Connect with PKCE, so the browser never holds provider tokens.

The sign-in is behavior like any other: one use case starts it and one completes it, and the provider is an external system with its operations. The start use case builds the redirect with its PKCE challenge. The complete use case exchanges the code, validates the tokens, finds or creates the person, and starts the session. Each declares the provider operations it calls, so the sign-in shows the same declared edges as any use case that calls an outside system.

### Where Access Records Live

Each access record lives in the home its type names, as `litespec/core/model#write-every-record-in-its-home` states. The [project model](model.md) states the shared identifier and ownership rules.

| Type | Stored as | Home |
|:---|:---|:---|
| `actor` | page | The Intent actors home |
| `role` | page | The Intent roles home |
| `permission` | entry | The module permissions file |
| `entry-point` | page | The Behavior entry points home |
| `external-system` | page | The external systems home in Behavior, at product or module level |

An actor, role, or external system page carries a `summary` field that states what the record is for, as `litespec/core/use-cases#summarize-every-use-case` states. Each module permissions file holds its permission entries, and its explanation file states the risks and boundaries of those entries.

## Standards

### Name Every Actor with Its Kind

**ID:** `litespec/core/access#name-every-actor-with-its-kind`

**Strength:** required

**Applies to:** artifact: record:actor

**Statement:** An actor MUST name its kind in `kind`.

**Rationale:** The kind says whether a human, an organization, or an outside system stands behind the actor, and that decides who answers for what the actor does. Without it, a reviewer assigns responsibility without knowing who stands behind the actor. A `system` actor stands for one outside system in flow steps, and that system's record names the actor in `represents`.

**Example:** An employee actor carries `person`, the library as a lending party carries `organization`, and the text gateway standing in a flow step carries `system`.

**Checked by:** `litespec model check` reports `record-invalid` for an actor without `kind` and `record-invalid` for a kind outside person, organization, and system.

### Record Every Role an Actor Holds

**ID:** `litespec/core/access#record-every-role-an-actor-holds`

**Strength:** required

**Applies to:** artifact: record:actor

**Statement:** An actor that acts through a role MUST list the role's identifier in `holds`.

**Rationale:** The hold is what connects an actor to its grant. Without it, the actor and the role sit on separate pages with no stated connection, and no reviewer can tell what the actor may do. The hold names only the role, because the role holds the scope of its grant, and one actor's hold cannot widen or narrow the reach the role states. Proposed by [decision/0055-state-each-access-scope-once](decision/0055-state-each-access-scope-once).

**Example:**

```json
"holds": ["role/employee"]
```

**Checked by:** `litespec model check` reports `role-unused` for a role held by no actor, and `record-invalid` for a hold that is not a role identifier, such as a hold carrying a scope.

### Scope Every Role to Its One Reach

**ID:** `litespec/core/access#scope-every-role-to-its-one-reach`

**Strength:** required

**Applies to:** artifact: record:role

**Statement:** A role MUST name its one scope in `scope`.

**Rationale:** The role is the only home of a grant's reach. The same permission allows platform-wide action in one grant and own-record action in another, so the scope decides what each grant actually reaches. Stating the reach on each hold or each permission as well would keep three copies of one value, and the copies would drift apart, so the role states it once and every permission reads its reach from the roles that grant it.

Without a named scope, every grant reads as product-wide, and the narrowest grant gets the widest reach. Proposed by [decision/0055-state-each-access-scope-once](decision/0055-state-each-access-scope-once).

**Example:** A scope of `team` bounds the role to one team, while `platform` bounds it to the whole product.

**Checked by:** `litespec model check` reports `record-invalid` for a role without `scope` and `record-invalid` for a scope outside kebab-case form. A `scope` on a permission entry or on a hold fails the schema as an unknown property.

### Grant Permissions Through Roles

**ID:** `litespec/core/access#grant-permissions-through-roles`

**Strength:** required

**Applies to:** artifact: record:role, record:permission

**Statement:** Each permission MUST name the roles that grant it in `grantedTo`, with each role's `excludes` stating what the grant does not cover.

**Rationale:** The grant is the middle of the chain: it joins the actors that hold the role to the use cases that cite its permissions in their grants. Every link points from the lower or later record to the higher or earlier one, so the permission in Behavior names its Intent roles in `grantedTo` instead of the role naming the permission. A role that no permission names grants nothing, so every use case its holders run breaks the chain at the middle link. The `excludes` state the acts inside the role's scope that the grant does not cover, so a reader sees the edge of the grant on the role itself.

**Example:** `permission/request-expense` names `role/employee` in `grantedTo`, and the submit use case cites that permission in its grant.

**Checked by:** `litespec model check` reports `record-invalid` for a permission with no `grantedTo` entry or a role with no `excludes`, `permission-unused` for a permission no role grants, and `role-unused` for a role no permission names.

### State Every Permission with Its Meaning

**ID:** `litespec/core/access#state-every-permission-with-its-meaning`

**Strength:** required

**Applies to:** artifact: record:permission

**Statement:** A permission MUST state what it allows in `meaning`.

**Rationale:** The `meaning` is the one sentence a reviewer reads to decide whether the grant fits the role. A name alone carries no boundary: two permissions with the same name pattern and no meanings drift into overlapping grants that no audit can separate.

**Example:** `permission/request-expense` carries "Submit an expense for reimbursement."

**Checked by:** `litespec model check` reports `record-invalid` for a permission without `meaning`.

### Declare Access on Every Use Case

**ID:** `litespec/core/access#declare-access-on-every-use-case`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use case MUST declare its grant and its tenant scope in `access`.

**Rationale:** The grant and the tenant scope answer the two access questions together: what check the call passes, and which tenant data it may touch. A use case missing one part leaves that question to code, and two implementations of the same record enforce different boundaries. The entry point the call arrives through declares how the caller proved identity.

**Example:**

```json
"access": { "grant": "permission/manage-copies", "tenant": "own" }
```

**Checked by:** `litespec model check` reports `record-invalid` for a use case without `access`, `grant`, or `tenant`.

### Authenticate Every Caller Through a Declared Authentication Kind

**ID:** `litespec/core/access#authenticate-every-caller-through-a-declared-authentication-kind`

**Strength:** required

**Applies to:** artifact: record:entry-point

**Statement:** Each entry point MUST name its authentication kinds from session, token, device, link, app, and none.

**Rationale:** The authentication kind says how the caller proves identity before the grant is checked: a session for a signed-in person, a device for a managed scanner, a link for a single-purpose link, an app for a calling application, a token for a bearer token, and none for open calls. Without a declared kind, open calls and authenticated calls look alike, and a reviewer cannot tell which identity the grant was checked against.

**Example:** An expense submission names `session`, a library self-checkout kiosk names `device`, and a public catalog names `none`.

**Checked by:** `litespec model check` reports `record-invalid` for an `authentication` kind outside session, token, device, link, app, and none.

### Authorize Every Use Case Through a Declared Grant

**ID:** `litespec/core/access#authorize-every-use-case-through-a-declared-grant`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each access declaration MUST name its grant as a permission, a self grant, signed-in, anonymous, or system.

**Rationale:** The grant is the check the call passes: a permission for role-held action, a self grant for action on records the caller owns, signed-in for an authenticated caller with no permission, anonymous for open calls, and system for work an internal trigger runs with no outside caller. A grant outside these five forms gives the checker no stated check to enforce, so each caller invents its own test.

System never authorizes an outside request, and a call with no authentication where one is needed ends in the derived unauthenticated result. Settled by [decision/0079-cover-the-behavior-reviewers-found-missing](decision/0079-cover-the-behavior-reviewers-found-missing) and [decision/0083-evaluate-access-in-one-order](decision/0083-evaluate-access-in-one-order).

**Example:** `permission/manage-copies` guards staff work, `self(permission/read-loans)` lets a reader read their own loans, `signed-in` opens a workspace to any signed-in person with no permission, `anonymous` opens a public catalog, and `system` covers an archive use case only its retention job runs.

**Checked by:** `litespec model check` reports `record-invalid` for a grant outside permission, self, signed-in, anonymous, and system, and `reference-unknown` for a permission no record defines.

### Admit the System Identity on Every System-Run Use Case

**ID:** `litespec/core/access#admit-the-system-identity-on-every-system-run-use-case`

**Strength:** required

**Applies to:** artifact: record:use-case, record:reaction, record:job

**Statement:** A use case that a reaction or a job runs, commands, or queries MUST admit the system identity: its grant is `system` or `anonymous`, or it states `runsAs: system` beside its grant for person callers.

**Rationale:** A reaction or a job runs with no person behind it, so the call it sends carries the system identity. The guard authorizes every call from the use case's access, and a permission, self, or signed-in grant admits only people, so every system run of such a use case is refused at runtime. Stating `runsAs: system` beside the person grant admits the system identity too, and a use case that only system triggers run takes the `system` grant with no `runsAs`.

**Example:** Claimwick's erase-personal-data use case grants `permission/remove-personal-data` to finance staff and states `runsAs: system`, so the nightly erase job runs it beside them. Without `runsAs`, every nightly run is refused.

**Checked by:** `litespec model check` reports `system-access-missing` for a reaction or job that runs, commands, or queries a use case whose access admits no system identity, and `record-invalid` for `runsAs` beside a `system` or `anonymous` grant.

### Limit the Age of a Sign-In by a Setting

**ID:** `litespec/core/access#limit-the-age-of-a-sign-in-by-a-setting`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A use case that requires a recent sign-in MUST name the window in `recentSignInLimit` with a `setting/` identifier.

**Rationale:** Some operations need a fresh sign-in: changing a sign-in method or approving a large payout on a week-old session is not the same as doing it just after signing in. The window is a product decision, so it lives in a setting that states its range and setter, and every use case that needs it names the same setting instead of typing its own duration.

A call whose sign-in is older than the window ends in the derived sign-in-too-old result. Settled by [decision/0074-declare-access-settings-and-deadlines](decision/0074-declare-access-settings-and-deadlines).

**Example:** Claimwick payout approval above the threshold names `setting/spending.sign-in-window` in `recentSignInLimit`; a session older than the window ends in the derived sign-in-too-old result.

**Checked by:** `litespec model check` reports `record-invalid` for a `recentSignInLimit` that names no `setting/` identifier and `reference-unknown` for a setting no record defines.

### Remove a Target That Must Be the Caller

**ID:** `litespec/core/access#remove-a-target-that-must-be-the-caller`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A message whose target must be the caller MUST carry no target and act on the caller, or declare a self grant that the authorization guard enforces.

**Rationale:** A target that must equal the caller is a value the caller could forge. Each handler would compare it with the verified identity, and each comparison is authorization logic sitting in a handler. Carrying no target leaves nothing to forge: the call acts on the caller from verified claims.

Where the target must stay on the message, a self grant states the scope beside the use case, and the guard enforces it.

**Example:** "My loans" lists the signed-in reader's loans and takes no reader id.

**Reviewed for:** Which use cases name a caller-owned target in their inputs without a self grant the guard enforces, or compare the caller with a target in a handler? Check their inputs and handlers beside the self grants.

### Scope Every Grant to Its Tenant Reach

**ID:** `litespec/core/access#scope-every-grant-to-its-tenant-reach`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each access declaration MUST name its tenant scope as own, cross, public, or none.

**Rationale:** The tenant scope bounds which tenant data the call may touch: `own` stays inside the caller tenant, `cross` reaches across tenants, `public` works beyond tenant boundaries, and `none` touches no tenant data. Without the scope, a read inside one tenant and a read across all tenants look the same, and the widest reach hides inside the narrowest grant.

**Example:** An expense report names `own`, a staff search across tenants names `cross`, a public catalog names `public`, and a health check names `none`.

**Checked by:** `litespec model check` reports `record-invalid` for a tenant scope outside own, cross, public, and none.

### Evaluate Access in One Order

**ID:** `litespec/core/access#evaluate-access-in-one-order`

**Strength:** required

**Applies to:** artifact: record:use-case, record:aggregate

**Statement:** Access MUST be evaluated in one order: the authenticated principal, then the role assignments whose bound scope instance matches the target, then the permission, then the use case tenant scope, then the self-ownership restriction, then delegation validity, with several assignments combining as a union.

**Rationale:** Each step narrows what the next step checks: identity before assignments, assignments before the permission they carry, the permission before the reach it is used at, and ownership and delegation last as restrictions on an otherwise allowed call. One order means two implementations allow and refuse the same calls.

A role assignment binds a scope instance as well as a level: the tenant the hold reaches, or the person for a self grant. Several assignments combine as a union: any assignment that allows the call allows it. Settled by [decision/0083-evaluate-access-in-one-order](decision/0083-evaluate-access-in-one-order).

**Example:** A Claimwick approver holds the approver role for one cost center. A call to approve an expense evaluates as the session principal, then the approver assignment whose cost center matches the expense, then the approve-expense permission, then the own tenant scope, then ownership and delegation. An assignment for another cost center does not match, so the call ends in the derived forbidden result.

**Reviewed for:** Does every call evaluate principal, assignments, permission, tenant scope, ownership, and delegation in that order? Walk one allowed and one refused call through the steps.

### Cite the Setting or Plan Behind Every Entitlement

**ID:** `litespec/core/access#cite-the-setting-or-plan-behind-every-entitlement`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A grant that depends on a setting or a plan MUST cite that setting or plan.

**Rationale:** An entitlement allows one tenant what another is refused, so the grant alone reads as unequal treatment. Citing the setting or plan shows the commercial or configured reason behind the difference, and gives the reviewer the range and setter to judge it against.

**Example:** An export command cites the caller's plan, so a tenant past its plan limit is refused by the plan rather than by role.

**Reviewed for:** Does every entitlement grant cite its setting or plan? Check each grant beside the settings its use case cites.

### Prove Hierarchies Derived Trust and Delegation with Scenarios

**ID:** `litespec/core/access#prove-hierarchies-derived-trust-and-delegation-with-scenarios`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A hierarchy, derived trust, or delegation pattern MUST prove its allowed and refused calls with scenarios.

**Rationale:** These patterns compose the same access parts into wider reach, so prose alone cannot show where the wider grant stops. Scenarios that walk an allowed call and a refused call pin the boundary as checked facts, and the checker confirms each one through the public boundary.

**Example:** A delegation story shows an assistant acting for a manager succeeding, and the same assistant acting without delegation ending in the derived forbidden result.

**Reviewed for:** Does every hierarchy, trust, or delegation pattern carry an allowed and a refused scenario? Check the scenarios on the use cases that carry the pattern.

### Record a Delegated Grant Inside One Tenant as a Role Assignment

**ID:** `litespec/core/access#record-a-delegated-grant-inside-one-tenant-as-a-role-assignment`

**Strength:** required

**Applies to:** artifact: record:use-case, record:aggregate

**Statement:** A delegated or temporary grant inside one tenant MUST be recorded as a role assignment with a validity window in `validFrom` and `validUntil` and the bound scope instance, held on an aggregate the access model reads and granted and revoked by its own use cases.

**Rationale:** Inside one tenant, delegation changes who holds a role for a time, not which grant forms exist. Recording the assignment with its window on an aggregate the access model reads gives the guard one stated source for the deputy, the role, and the dates, and the granting and revoking use cases make the start and the end reviewable acts. A use case acting under the assignment declares its permission as usual, so no new access form appears.

The assignment binds the scope instance the deputy reaches, the tenant or team, beside the dates. The tenancy extension adds only the cross-tenant form of delegation; it adds no form inside one tenant. Settled by [decision/0083-evaluate-access-in-one-order](decision/0083-evaluate-access-in-one-order).

**Example:** In Claimwick, a manager delegating team management to a deputy for 2026-12-20 to 2027-01-05 is recorded as an assignment of the manager role to the deputy, bound to the manager's team, with `validFrom` of 2026-12-20 and `validUntil` of 2027-01-05, granted by its own grant use case and ended by its own revoke use case. The manage use case acting under it still declares `permission/manage-team`.

**Reviewed for:** Does every delegated or temporary grant inside one tenant sit on a role assignment with `validFrom`, `validUntil`, and the bound scope instance on an aggregate the access model reads, with its own granting and revoking use cases? Check the assignments beside the actor holds and the use cases that act under them.

### Grant Across Tenants Only Through Declared Delegation

**ID:** `litespec/core/access#grant-across-tenants-only-through-declared-delegation`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A grant that acts across tenants through delegation MUST declare its delegation grant on both sides.

**Rationale:** Delegation lets one actor act for another, so a grant on one side alone leaves the other side unreviewed. Declaring the grant on both sides shows who may delegate and who may be acted for, and the tenancy extension states the fields each side carries.

**Example:** An assistant acts for a manager only where the manager delegation grant and the assistant acting grant both name the pairing.

**Reviewed for:** Does every cross-tenant delegation grant declare its grant on both sides? Check the delegation grants the tenancy extension declares.

### Describe Every Served Content Entry with Its Serving Fields

**ID:** `litespec/core/access#describe-every-served-content-entry-with-its-serving-fields`

**Strength:** required

**Applies to:** artifact: record:entry-point

**Statement:** Each served content entry on a browser entry point MUST carry path, contentType, cacheControl, versioning, and fulfills.

**Rationale:** Served content is a file the product hands out, not behavior it runs, so the serving fields are the whole contract: where it lives, what it is, how long it caches, how it versions, and which requirement it carries. Without them, a legal text or a static script ships as an unreviewed file with no stated requirement or freshness.

**Example:** A terms text carries its path, `text/html`, its cache window, its version rule, and the requirement it fulfills.

**Checked by:** `litespec model check` reports `record-invalid` for a served content entry without path, contentType, cacheControl, versioning, or fulfills.

### Declare Erasure for Every External System

**ID:** `litespec/core/access#declare-erasure-for-every-external-system`

**Strength:** required

**Applies to:** artifact: record:external-system

**Statement:** An external system MUST declare how personal data it holds is erased in `erasure`.

**Rationale:** Personal data sent outside stays outside unless some operation or event removes it. The erasure declaration states the call, the integration event, or why removal does not run. An unsupported, manual, or not-applicable erasure cites the Intent decision that accepts the limit, so the gap reads as an accepted decision rather than a missed edge.

**Example:** A payout archive declares `{"mode": "unsupported", "reason": "settled payouts belong to the bank"}` with the decision that accepts the limit, plus the sentence employees see.

**Checked by:** `litespec model check` reports `record-invalid` for an external system without `erasure` and `reference-unknown` for an unsupported, manual or not-applicable erasure without an Intent decision.

### Model a Hosted Sign-In as Use Cases

**ID:** `litespec/core/access#model-a-hosted-sign-in-as-use-cases`

**Strength:** required

**Applies to:** artifact: record:use-case, record:external-system

**Statement:** A sign-in through a hosted identity provider MUST be modeled as use cases that start and complete the sign-in, with the provider as an external system.

**Rationale:** A product that signs people in through a hosted identity provider runs OpenID Connect with PKCE through its backend for frontend: the browser never holds provider tokens. The start use case builds the provider redirect with its PKCE challenge, and the complete use case exchanges the returned code, validates the tokens, finds or creates the person, and starts the session. The provider is an external system declaring its authorize, token, and logout operations, and the two use cases declare `calls` edges to the operations they use, under `litespec/backend/application#dispatch-only-declared-effect-edges`. Both use cases carry the `anonymous` grant, since the caller is not signed in yet, and arrive through the browser entry point whose `authentication` declares the session. Settled by [decision/0103-dotnet-stack-binding](decision/0103-dotnet-stack-binding).

**Example:** Claimwick signs employees through its hosted provider: `use-case/identity.start-sign-in` builds the redirect, `use-case/identity.complete-sign-in` exchanges the code and starts the session, and `external-system/identity-provider` declares the authorize, token, and logout operations the two use cases call.

**Reviewed for:** Does every hosted sign-in run as start and complete use cases against the provider external system, with anonymous grants and declared provider calls? Check the sign-in use cases beside the provider system.

### Define Each Scope Level in the Glossary

**ID:** `litespec/core/access#define-each-scope-level-in-the-glossary`

**Strength:** required

**Applies to:** artifact: record:role

**Statement:** Each scope level a role names MUST have a glossary term titled with the level and Scope, stating what the level covers in this product.

**Rationale:** A bare level such as `platform` or `tenant` reads the same in every product, and each product means a different reach by it. The glossary term states that reach once, so a reviewer reads what `platform` covers in this product rather than guessing it from the word. Proposed by [decision/0055-state-each-access-scope-once](decision/0055-state-each-access-scope-once).

**Example:** Claimwick's `platform` scope has a glossary term, Platform Scope, that states it covers every expense in the Claimwick installation.

**Checked by:** `litespec model check` reports `scope-term-missing` for a role scope with no glossary term titled with the level and Scope.

### Name Each Permission by a Use-Case Verb

**ID:** `litespec/core/access#name-each-permission-by-a-use-case-verb`

**Strength:** advisory

**Applies to:** artifact: record:permission

**Statement:** Each permission name SHOULD open with a verb from the use-case verb table, a glossary verb of kind command or either, or with `manage`.

**Deviation:** A permission name an access editor already lists under another verb.

**Rationale:** The first word of a permission name tells a reader whether the grant reads or changes, and which kind of use case it covers. A name such as `handle-documents` says nothing about what it includes, so a reader cannot tell whether it reads, writes, or removes. The verb table gives each opening word one meaning across the product, and a project verb carries its kind in its glossary verb term, so a permission opens with a command or either verb from either list.

The opening verb is a naming convention only: no permission implies another by its name, and `manage-` grants nothing beyond the use cases that cite the permission. Proposed by [decision/0055-state-each-access-scope-once](decision/0055-state-each-access-scope-once), and glossary verbs by [decision/0062-declare-project-verbs-in-the-glossary](decision/0062-declare-project-verbs-in-the-glossary). Settled by [decision/0083-evaluate-access-in-one-order](decision/0083-evaluate-access-in-one-order) and [decision/0087-keep-prose-checks-advisory-and-names-per-type](decision/0087-keep-prose-checks-advisory-and-names-per-type).

**Example:** `permission/approve-expense` opens with `approve` and `permission/manage-policy` opens with `manage`, so both pass. `permission/submit-expense` opens with the glossary verb `submit` and passes. `permission/handle-documents` opens with a word neither list states, so it is reported.

**Checked by:** `litespec model check` reports `permission-verb-unknown` (level G) for a permission name whose opening verb is neither in the table, nor a glossary verb of kind command or either, nor `manage`.

### Keep Permission Sets to Entries

**ID:** `litespec/core/access#keep-permission-sets-to-entries`

**Strength:** required

**Applies to:** artifact: record:permission-set

**Statement:** A module permissions file MUST hold permission entries only, and its explanation states only the risks and boundaries of those entries in `excludes`, without restating a permission's meaning or adding a grant.

**Rationale:** Each permission is defined once, in its entry: its `meaning` is the one statement of what it allows, and prose that repeats it drifts from the entry. A boundary is what a reader might assume a permission allows but it does not, which the meaning does not say, so `excludes` states what the set does not allow and the explanation holds what the entries cannot.

A permission that guards a query or use case disclosing personal or financial data carries the `personal-data` or `access` risk, like a permission for a write. Proposed by [decision/0055-state-each-access-scope-once](decision/0055-state-each-access-scope-once). Settled by [decision/0079-cover-the-behavior-reviewers-found-missing](decision/0079-cover-the-behavior-reviewers-found-missing).

**Reviewed for:** Does the explanation beside each permissions file state only risks and boundaries, without restating a meaning, adding a grant, or naming a future role? Check the Risk and Boundaries sections beside each permissions file.

## Conventions

### Use Three Conventional Scope Levels

**ID:** `litespec/core/access#use-three-conventional-scope-levels`

**Strength:** default

**Applies to:** artifact: record:role

**Default:** Use `platform`, `tenant`, and `self` as role scopes.

**Replacement:** A project names its own scope levels in its role records.

**Rationale:** Three levels cover the grants most products need: product-wide administration, tenant-wide operation, and action on one's own records. A project with different boundaries names its own levels, and every role still carries exactly one scope.

**Reviewed for:** Does every role scope use `platform`, `tenant`, or `self`, or a recorded project convention? Check `scope` on each role page.
