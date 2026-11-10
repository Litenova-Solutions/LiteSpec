# Access

**Stack:** any
**Concern:** records

## Intent

Who may act in the product and how they reach it: actors, roles, permissions, the access each use case declares, entry points, and external systems. Actors and roles live in Intent as product decisions users see; permissions, entry points, and external systems live in Behavior as access design. The [use-cases page](use-cases.md) states the use cases that carry access, and the [model page](model.md) states the shared record rules.

## Concepts

### The Permission Chain

Access follows one chain from actor to use case: an actor holds a role at a scope, the role allows permissions, and the use case declares the grant it checks. The chain answers every access question in one place: which actor held which role at which scope, which permission the role allowed, and which use case declared it. The chain is role-based access control: permissions attach to roles, never to actors directly, and scopes bound each hold to one reach. The role lives in Intent and the permission lives in Behavior, so `allows` is a declared down-link from Intent to Behavior.

### Actors, Roles, and Scopes

An actor is one party that acts in the product. Its kind is person for a human acting directly, or organization for a company or group acting as one party. Software is no actor: an external system stands alone, and internal machinery is never an actor. Each actor states in `belongsToTeam` whether it belongs to the team building and running the product.

A role is one named set of permissions granted together. Its scope is a kebab-case level naming the reach of the grant, such as platform, tenant, or self. An actor records each grant in `holds` as the role with the scope at which it holds it, so the same role grants different reach in different places.

### Permissions

A permission is one allowed action. Each module keeps its permissions in `modules/<m>/permissions.yaml`, and each permission states in `meaning` what it allows in one sentence. Roles allow permissions from those lists, and use cases cite them in their grants, so the lists are the one vocabulary both sides share.

Each permission names in `scope` the reach at which a role grants it, using the same levels a role scope uses, such as `platform`, `tenant`, or `self`, or levels the project names itself. An optional `group` orders the permission beside its peers in role editors. The `risk` field marks a sensitive permission; it is the only risk field records carry. Whether a permission only reads follows from the kind of its leading verb in the use-case verb table on the [use-cases page](use-cases.md). A permission name may also open with `manage`, which allows every changing use case on what it names, so it never only reads.

### The Access Field

Each use case declares `access` with two parts: what grant the call checks and which tenant reach the call has. The door the call arrives through declares `auth`: how the caller proves identity. The door authenticates and the use case authorizes. The `auth` list on the door names one or more of `session`, `token`, `device`, `link`, `app`, or `none`. The `grant` on the use case is a permission such as `permission/manage-copies`, a self grant such as `self(permission/read-loans)`, or the open grants `anonymous` and `system`. A self grant allows the permission only on records the caller owns. The `tenant` scope is `own` for work inside the caller tenant, `cross` for work reaching across tenants, `public` for work with no tenant boundary, or `none` for work that touches no tenant data. The [use-cases page](use-cases.md) states the use case fields around access.

### Generic Refusals

Refusals that follow from access and input need no written result. A call without the grant ends in the derived forbidden result, and a call with input that breaks its rules ends in the derived invalid input result. [Derive Generic Refusals Instead of Writing Them](use-cases.md#derive-generic-refusals-instead-of-writing-them) states the rule, and scenarios point at the derived results without anyone writing them.

### Tenant Scope and Cross Tenant Access

`own` keeps the call inside the caller tenant, `public` opens it beyond tenant boundaries, `none` states the call touches no tenant data, and `cross` reaches across tenants. A call that reads or writes another tenant carries the widest reach, and the pull request that changes it is its review.

### Entitlements

An entitlement is a grant that depends on what the caller bought or configured, such as a plan limit or a policy setting. The use case cites the setting or plan behind the grant, as [Cite the Setting or Plan Behind Every Entitlement](#cite-the-setting-or-plan-behind-every-entitlement) states, so a reviewer sees why one tenant is allowed what another is refused. The cited setting states its range and setter, as the [use-cases page](use-cases.md) states for settings.

### Hierarchies, Derived Trust, and Delegation

Hierarchies, derived trust, and delegation are patterns built from the same access parts, not extra grant forms. A hierarchy nests roles so a senior role allows what its junior roles allow, derived trust lets one checked fact stand for another within one call, and delegation lets one actor act for another. Each pattern proves its allowed and refused calls with scenarios, as [Prove Hierarchies Derived Trust and Delegation with Scenarios](#prove-hierarchies-derived-trust-and-delegation-with-scenarios) states. Grants that cross tenants through delegation use the tenancy extension, which states the declared grant each side carries.

### Entry Points

An entry point is one door from outside: a way people or systems reach the product. [Give Each Entry Point a Kind](edges.md#give-each-entry-point-a-kind) states the kinds, and each door declares its `auth`. A `served-content` entry point carries `path`, `contentType`, `cacheControl`, `versioning`, and `fulfills`, so a static script or a legal text is served content rather than a use case. Use cases name the entry points that reach them in `reachedThrough`, so every operation shows how callers arrive. The protocol detail for devices lives in the devices extension.

### External Systems

An external system is one system outside the bounded context that the product exchanges messages with, such as a payment provider or a text gateway. It stands alone and names no actor. An integration that calls the product is an external system calling through an API entry point. It declares each operation the product may call in `operations`, with its direction, timeout, response, and cache behavior, as [Describe Every External System Operation](edges.md#describe-every-external-system-operation) states, so every outside call is a declared edge. It declares in `erasure` how personal data it holds is erased, or why erasure there is impossible with the Intent decision that accepts the limit.

### Homes and Summaries

Each access record lives in one home, and the [model page](model.md) states the shared identifier and ownership rules.

| Type | Stored as | Home |
|:---|:---|:---|
| `actor` | page | The Intent actors home |
| `role` | page | The Intent roles home |
| `permission` | entry | The module permissions file |
| `entry-point` | page | The Behavior entry points home |
| `external-system` | page | The Behavior systems home, at product or module level |

An actor, role, or external system page carries a `summary` field stating what the record is for. Each module permissions file holds entries only.

## Standards

### Keep Access Records in Their Homes

**ID:** `litespec/core/access#keep-access-records-in-their-homes`

**Strength:** required

**Applies to:** artifact: record:actor, record:role, record:permission, record:entry-point, record:external-system

**Statement:** An access record MUST live in the home its type names.

**Rationale:** One home per type tells a reader where to open a record and tells the book where to render it. A record kept anywhere else looks written while readers open the home and find nothing there.

**Reviewed for:** Which access records sit outside the homes their type names? Look at the actor, role, permission, entry point, and external system homes.

### Name Every Actor with Its Kind

**ID:** `litespec/core/access#name-every-actor-with-its-kind`

**Strength:** required

**Applies to:** artifact: record:actor

**Statement:** An actor MUST name its kind in `kind`.

**Rationale:** The kind tells a reviewer whether a human or an organization stands behind the actor, which decides who answers for what it does. Without the kind, a reviewer assigns responsibility without knowing who stands behind the actor. Software stands behind no actor.

**Example:** An employee actor carries `person`, and the library as a lending party carries `organization`.

**Checked by:** `litespec model check` reports `record-invalid` for an actor without `kind` and `field-invalid` for a kind outside person and organization.

### Record Every Role Hold with Its Scope

**ID:** `litespec/core/access#record-every-role-hold-with-its-scope`

**Strength:** required

**Applies to:** artifact: record:actor

**Statement:** An actor that acts through a role MUST record the hold in `holds`.

**Rationale:** The hold is the grant: without it, the actor and the role sit on separate pages with no stated connection, and no reviewer can tell what the actor is allowed to do. The scope bounds each grant, so one role grants platform reach to one actor and self reach to another.

**Example:**

```json
"holds": [{ "role": "role/employee", "scope": "self" }]
```

**Checked by:** `litespec model check` reports `role-orphaned` for a role held by no actor.

### Scope Every Role

**ID:** `litespec/core/access#scope-every-role`

**Strength:** required

**Applies to:** artifact: record:role

**Statement:** A role MUST name its scope in `scope`.

**Rationale:** The scope bounds what the role reaches: the same permission allows platform-wide action in one grant and own-record action in another. Without a named scope, every grant reads as product-wide, and the narrowest grant gets the widest reach.

**Example:** A scope of `team` bounds the role to one team, while `platform` bounds it to the whole product.

**Checked by:** `litespec model check` reports `record-invalid` for a role without `scope` and `field-invalid` for a scope outside kebab-case form.

### Grant Permissions Through Roles

**ID:** `litespec/core/access#grant-permissions-through-roles`

**Strength:** required

**Applies to:** artifact: record:role

**Statement:** A role MUST name the permissions it allows in `allows`.

**Rationale:** The allows list is the middle of the chain: it joins the actors that hold the role to the use cases that cite its permissions in their grants. The role lives in Intent and the permission lives in Behavior, so `allows` is a declared down-link. A role that names no permission grants nothing, so every use case its holders run breaks the chain at the middle link.

**Example:** `role/employee` allows `permission/submit-expense`, which the submit use case cites in its grant.

**Checked by:** `litespec model check` reports `record-invalid` for a role with no `allows` entry, and `permission-orphan` for a permission allowed by no role.

### State Every Permission with Its Meaning

**ID:** `litespec/core/access#state-every-permission-with-its-meaning`

**Strength:** required

**Applies to:** artifact: record:permission

**Statement:** A permission MUST state what it allows in `meaning`.

**Rationale:** The meaning is the one sentence a reviewer reads to decide whether the grant fits the role: a name alone carries no boundary. Two permissions with the same name pattern and no meanings drift into overlapping grants that no audit can separate.

**Example:** `permission/submit-expense` carries "Submit an expense for reimbursement."

**Checked by:** `litespec model check` reports `record-invalid` for a permission without `meaning`.

### State the Scope of Every Permission

**ID:** `litespec/core/access#state-the-scope-of-every-permission`

**Strength:** required

**Applies to:** artifact: record:permission

**Statement:** Each permission MUST state the scope at which a role grants it.

**Rationale:** A grant without a named reach is decided in code: each check looks up the reach beside the permission list, and code then holds a second list of permissions.

**Example:** `permission/manage-copies` carries `scope: platform`, the same level as the librarian role that grants it.

**Checked by:** `litespec model check` reports `record-invalid` for a permission entry without `scope`.

### Declare Access on Every Use Case

**ID:** `litespec/core/access#declare-access-on-every-use-case`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use case MUST declare its grant and its tenant scope in `access`.

**Rationale:** The two parts answer the access questions together: what grant the call checks and which tenant reach the call has. A use case without one part leaves that question to code, and two implementations of the same page enforce different boundaries. The door the call arrives through declares how the caller proved identity.

**Example:**

```json
"access": { "grant": "permission/manage-copies", "tenant": "own" }
```

**Checked by:** `litespec model check` reports `record-invalid` for a use case without `access`, `grant`, or `tenant`.

### Authenticate Every Caller Through a Declared Auth Kind

**ID:** `litespec/core/access#authenticate-every-caller-through-a-declared-auth-kind`

**Strength:** required

**Applies to:** artifact: record:entry-point

**Statement:** Each entry point MUST name its auth kinds from session, token, device, link, app, and none.

**Rationale:** The auth kind tells a reviewer how the caller proves identity before the grant is checked: a session for a signed-in person, a device for a managed scanner, a link for a single-purpose link, an app for a calling application, a token for a bearer token, and none for open calls. Without a declared kind, open calls and authenticated calls look alike, and a reviewer cannot tell which identity the grant was checked against.

**Example:** An expense submission names `session`, a self-checkout kiosk names `device`, and a public catalog names `none`.

**Checked by:** `litespec model check` reports `field-invalid` for an `auth` kind outside session, token, device, link, app, and none.

### Grant Every Use Case Through a Declared Grant

**ID:** `litespec/core/access#grant-every-use-case-through-a-declared-grant`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each access declaration MUST name its grant as a permission, a self grant, anonymous, or system.

**Rationale:** The grant is the check the call passes: a permission for role-held action, a self grant for action on records the caller owns, anonymous for open calls, and system for work with no caller. A grant outside these four forms gives the checker no stated check to enforce, and each caller invents its own test.

**Example:** `permission/manage-copies` for staff work, `self(permission/read-loans)` for a reader reading their own loans, `anonymous` for a public catalog, and `system` for a self-checkout handler acting for the device.

**Checked by:** `litespec model check` reports `field-invalid` for a grant outside permission, self, anonymous, and system, and `reference-unknown` for a permission no record defines.

### Remove a Target That Must Be the Caller

**ID:** `litespec/core/access#remove-a-target-that-must-be-the-caller`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A message whose target must be the caller MUST carry no target and act on the caller, or declare a self grant that the authorization guard enforces.

**Rationale:** A target that must equal the caller is a value the caller could forge: each handler would compare it with the verified identity, and each comparison is authorization logic in a handler. Carrying no target leaves nothing to forge, and the call acts on the caller from verified claims. Where the target must stay on the message, a self grant states the scope beside the use case, and the guard enforces it.

**Example:** "My loans" lists the signed-in reader's loans and takes no reader id.

**Reviewed for:** Which use cases with a self grant name a caller-owned target in their inputs without a self declaration the guard enforces, or compare the caller with a target in a handler? Look at the inputs and handlers beside the self grants.

### Scope Every Grant to Its Tenant Reach

**ID:** `litespec/core/access#scope-every-grant-to-its-tenant-reach`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each access declaration MUST name its tenant scope as own, cross, public, or none.

**Rationale:** The tenant scope bounds which tenant data the call may touch: own stays inside the caller tenant, cross reaches across tenants, public works beyond tenant boundaries, and none touches no tenant data. Without the scope, a read inside one tenant and a read across all tenants look the same, and the widest reach hides in the narrowest grant.

**Example:** An expense report names `own`, a staff search across tenants names `cross`, a public catalog names `public`, and a health check names `none`.

**Checked by:** `litespec model check` reports `field-invalid` for a tenant scope outside own, cross, public, and none.

### Cite the Setting or Plan Behind Every Entitlement

**ID:** `litespec/core/access#cite-the-setting-or-plan-behind-every-entitlement`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A grant that depends on a setting or a plan MUST cite that setting or plan.

**Rationale:** An entitlement allows one tenant what another is refused, so the grant alone reads as unequal treatment. Citing the setting or plan shows the commercial or configured reason behind the difference, and gives the reviewer the range and setter to judge it against.

**Example:** A submit command cites `setting/spending.receipt-threshold`, so an expense above the line with no receipt is refused by the policy setting rather than by role.

**Reviewed for:** Which entitlement grants lack a cited setting or plan? Look at the grant beside the settings each use case cites.

### Prove Hierarchies Derived Trust and Delegation with Scenarios

**ID:** `litespec/core/access#prove-hierarchies-derived-trust-and-delegation-with-scenarios`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A hierarchy, derived trust, or delegation pattern MUST prove its allowed and refused calls with scenarios.

**Rationale:** Patterns compose the same access parts into wider reach, so prose alone cannot show where the wider grant stops. Scenarios that walk an allowed call and a refused call pin the boundary as checked facts, and the checker confirms each one through the public boundary.

**Example:** A delegation story shows an assistant acting for a manager succeeding, and the same assistant acting without delegation ending in the derived forbidden result.

**Reviewed for:** Which hierarchy, trust, or delegation patterns lack an allowed and a refused scenario? Look at the scenarios on the use cases that carry the pattern.

### Record a Delegated Grant Inside One Tenant as a Role Assignment

**ID:** `litespec/core/access#record-a-delegated-grant-inside-one-tenant-as-a-role-assignment`

**Strength:** required

**Applies to:** artifact: record:use-case, record:aggregate

**Statement:** A delegated or temporary grant inside one tenant MUST be recorded as a role assignment with a validity window in `validFrom` and `validUntil`, held on an aggregate the access model reads and granted and revoked by its own use cases.

**Rationale:** Inside one tenant, delegation changes who holds a role for a time, not which grant forms exist. Recording the assignment with its window on an aggregate the access model reads gives the authorization guard one stated source for the deputy, the role, and the dates, and the granting and revoking use cases make the start and the end reviewable acts. A use case acting under the assignment declares its permission as usual, so no new access form appears. The tenancy extension adds only the cross-tenant form of delegation; it adds no form inside one tenant.

**Example:** In Claimwick, a manager delegating team management to a deputy for 2026-12-20 to 2027-01-05 is recorded as an assignment of the manager role to the deputy with `validFrom` of 2026-12-20 and `validUntil` of 2027-01-05, granted by its own grant use case and ended by its own revoke use case. The manage use case acting under it still declares `permission/manage-team`.

**Reviewed for:** Which delegated or temporary grants inside one tenant lack a role assignment with `validFrom` and `validUntil` on an aggregate the access model reads, or lack their own granting and revoking use cases? Look at the assignments beside the actor holds and the use cases that act under them.

### Grant Across Tenants Only Through Declared Delegation

**ID:** `litespec/core/access#grant-across-tenants-only-through-declared-delegation`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A grant that acts across tenants through delegation MUST declare its delegation grant on both sides.

**Rationale:** Delegation lets one actor act for another, so a grant on one side alone leaves the other side unreviewed. Declaring the grant on both sides shows who may delegate and who may be acted for, and the tenancy extension states the fields each side carries.

**Example:** An assistant acts for a manager only where the manager delegation grant and the assistant acting grant both name the pairing.

**Reviewed for:** Which cross tenant delegation grants lack a declared grant on both sides? Look at the delegation grants the tenancy extension declares.

### Describe Every Served Content Entry Point with Its Serving Fields

**ID:** `litespec/core/access#describe-every-served-content-entry-point-with-its-serving-fields`

**Strength:** required

**Applies to:** artifact: record:entry-point

**Statement:** A served content entry point MUST carry path, contentType, cacheControl, versioning, and fulfills.

**Rationale:** Served content is a file the product hands out, not behavior it runs, so the serving fields are the whole contract: where it lives, what it is, how long it caches, how it versions, and which requirement it carries. Without them, a legal text or a static script ships as an unreviewed file with no stated requirement or freshness.

**Example:** A terms text carries its path, `text/html`, its cache window, its version rule, and the requirement it fulfills.

**Checked by:** `litespec model check` reports `record-invalid` for a served content entry point without path, contentType, cacheControl, versioning, or fulfills.

### Declare Erasure for Every External System

**ID:** `litespec/core/access#declare-erasure-for-every-external-system`

**Strength:** required

**Applies to:** artifact: record:external-system

**Statement:** An external system MUST declare how personal data it holds is erased in `erasure`.

**Rationale:** Personal data sent outside stays outside unless some operation or event removes it, so the erasure declaration states the call, the integration event, or why removal is impossible. An impossible erasure cites the Intent decision that accepts the limit, so the gap reads as an accepted decision rather than a missed edge.

**Example:** A payout archive declares `{"mode": "impossible", "reason": "settled payouts belong to the bank"}` with the decision that accepts the limit, plus the sentence employees see.

**Checked by:** `litespec model check` reports `record-invalid` for an external system without `erasure` and `reference-unknown` for an impossible erasure without an Intent decision.

### Write a Summary on Every Actor and Role Page

**ID:** `litespec/core/access#write-a-summary-on-every-actor-and-role-page`

**Strength:** required

**Applies to:** artifact: record:actor, record:role

**Statement:** An actor or role page MUST carry a `summary` field.

**Rationale:** The `summary` states in plain sentences who the party is and what the grant is for, which the fields alone do not say. A reviewer meets the story first and the fields after, so the grant reads as a decision rather than a row.

**Checked by:** `litespec model check` reports `prose-missing` for an actor or role page without `summary`.

### Write a Summary on Every External System Page

**ID:** `litespec/core/access#write-a-summary-on-every-external-system-page`

**Strength:** required

**Applies to:** artifact: record:external-system

**Statement:** An external system page MUST carry a `summary` field.

**Rationale:** The `summary` states in plain sentences what the outside system does for the product, which the message lists alone do not say. A reviewer meets the relationship first and the messages after, so the dependency reads as a decision rather than a row.

**Checked by:** `litespec model check` reports `prose-missing` for an external system page without `summary`.

### Keep Permission Lists to Entries

**ID:** `litespec/core/access#keep-permission-lists-to-entries`

**Strength:** required

**Applies to:** artifact: record:permission-list

**Statement:** A module permissions file MUST hold entries only.

**Rationale:** The file holds entries only: each permission is one row with its fields, and prose around them would restate the meanings the entries already carry. A file with entries only stays readable as a list even as entries grow.

**Reviewed for:** Which module permissions files carry an explanation file? Look beside each permissions file for a `.md` file with its name.

## Conventions

### Use Three Conventional Scope Levels

**ID:** `litespec/core/access#use-three-conventional-scope-levels`

**Strength:** default

**Applies to:** artifact: record:role

**Default:** Use `platform`, `tenant`, and `self` as role scopes.

**Replacement:** A project names its own scope levels in its role records.

**Rationale:** Three levels cover the grants most products need: product-wide administration, tenant-wide operation, and action on one's own records. A project with different boundaries names its own levels, and every role still carries exactly one scope.

**Reviewed for:** Which role scopes fall outside platform, tenant, and self without a recorded project convention? Look at `scope` on each role page.
