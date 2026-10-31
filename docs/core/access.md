# Access

## Intent

Who may act in the product and how they reach it: actors, roles, permissions, the access each use case declares, entry points, and external systems. Developers and agents write these records in Behavior, and reviewers read them in the Behavior diff with the security queue. The [use-cases page](use-cases.md) states the use cases that carry access, and the [model page](model.md) states the shared record rules.

## Concepts

### The Permission Chain

Access follows one chain from actor to use case: an actor holds a role at a scope, the role allows permissions, and the use case declares the grant it checks. The chain answers every access question in one place: which actor held which role at which scope, which permission the role allowed, and which use case declared it. The chain is role-based access control: permissions attach to roles, never to actors directly, and scopes bound each hold to one reach.

### Actors, Roles, and Scopes

An actor is one party that acts in the product. Its kind is person for a human acting directly, organization for a company or group acting as one party, or system for software acting on its own.

A role is one named set of permissions granted together. Its scope is a kebab-case level naming the reach of the grant, such as platform, tenant, or self. An actor records each grant in `holds` as the role with the scope at which it holds it, so the same role grants different reach in different places.

### Permissions

A permission is one allowed action. The project keeps every permission in one shared list, and each permission states in `meaning` what it allows in one sentence. Roles allow permissions from that list, and use cases cite them in their grants, so the list is the one vocabulary both sides share.

Each permission names in `scope` the reach at which a role grants it, using the same levels a role scope uses, such as `platform`, `tenant`, or `self`, or levels the project names itself. An optional `group` orders the permission beside its peers in role editors. The `risk` field marks a sensitive permission, and whether a permission only reads follows from the kind of its leading verb in the use-case verb table on the [use-cases page](use-cases.md). A permission name may also open with `manage`, which allows every changing use case on what it names, so it never only reads.

### The Access Field

Each use case declares `access` with two parts: what grant the call checks and which tenant reach the call has. The door the call arrives through declares `auth`: how the caller proves identity. The door authenticates and the use case authorizes. The `auth` list on the door names one or more of `session`, `token`, `device`, `link`, `app`, or `none`. The `grant` on the use case is a permission such as `permission/manage-copies`, a self grant such as `self(permission/read-loans)`, or the open grants `anonymous` and `system`. A self grant allows the permission only on records the caller owns. The `tenant` scope is `own` for work inside the caller tenant, `cross` for work reaching across tenants, `public` for work with no tenant boundary, or `none` for work that touches no tenant data. The [use-cases page](use-cases.md) states the use case fields around access.

### Generic Refusals

Refusals that follow from access and input need no written result. A call without the grant ends in the derived forbidden result, and a call with input that breaks its rules ends in the derived invalid input result. [Derive Generic Refusals Instead of Writing Them](use-cases.md#derive-generic-refusals-instead-of-writing-them) states the rule, and scenarios point at the derived results without anyone writing them.

### Tenant Scope and Cross Tenant Access

`own` keeps the call inside the caller tenant, `public` opens it beyond tenant boundaries, `none` states the call touches no tenant data, and `cross` reaches across tenants. A call that reads or writes another tenant carries the highest access risk, so the Behavior diff lists every `cross` change in the security queue, where the security reviewer reads it separately from reviewed status. Cross scope raises no tier: the queue carries the review, and the tier follows the risk values the record carries.

### Entitlements

An entitlement is a grant that depends on what the caller bought or configured, such as a plan limit or a policy setting. The use case cites the setting or plan behind the grant, as [Cite the Setting or Plan Behind Every Entitlement](#cite-the-setting-or-plan-behind-every-entitlement) states, so a reviewer sees why one tenant is allowed what another is refused. The cited setting states its range and setter, as the [use-cases page](use-cases.md) states for settings.

### Hierarchies, Derived Trust, and Delegation

Hierarchies, derived trust, and delegation are patterns built from the same access parts, not extra grant forms. A hierarchy nests roles so a senior role allows what its junior roles allow, derived trust lets one checked fact stand for another within one call, and delegation lets one actor act for another. Each pattern proves its allowed and refused calls with scenarios, as [Prove Hierarchies Derived Trust and Delegation with Scenarios](#prove-hierarchies-derived-trust-and-delegation-with-scenarios) states. Grants that cross tenants through delegation use the tenancy extension, which states the declared grant each side carries.

### Entry Points

An entry point is one door from outside: a way people or systems reach the product. [Give Each Entry Point a Kind](edges.md#give-each-entry-point-a-kind) states the kinds, and each door declares its `auth`. A `served-content` entry point carries `path`, `contentType`, `cacheControl`, `versioning`, and `fulfills`, so a static script or a legal text is served content rather than a use case. Use cases name the entry points that reach them in `reachedThrough`, so every operation shows how callers arrive. The protocol detail for devices lives in the devices extension.

### External Systems

An external system is one system outside the bounded context that the product exchanges messages with, such as a payment provider or a text gateway. It acts inside the product as one actor, named in `actor`, so everything it does stays attributable to a party the access records know. It declares each operation the product may call in `operations`, with its direction, timeout, response, and cache behavior, as [Describe Every External System Operation](edges.md#describe-every-external-system-operation) states, so every outside call is a declared edge. It declares in `erasure` how personal data it holds is erased, or why erasure there is impossible with the Intent decision that accepts the limit.

### Homes and Summaries

Each access record lives in one home, and the [model page](model.md) states the shared identifier and ownership rules.

| Type | Stored as | Home |
|:---|:---|:---|
| `actor` | page | The Intent actors home |
| `role` | page | The Behavior roles home |
| `permission` | entry | The Behavior permissions list |
| `entry-point` | entry | The Behavior entry points list |
| `external-system` | page | The Behavior systems home |

An actor, role, or external system page carries a Summary section stating what the record is for. The permission and entry point lists carry entries only, with no body section.

## Standards

### Keep Access Records in Their Homes

**ID:** `litespec/core/access#keep-access-records-in-their-homes`

**Statement:** An access record MUST live in the home its type names.

**Rationale:** One home per type tells a reader where to open a record and tells the book where to render it. A record kept anywhere else looks written while readers open the home and find nothing there.

**Reviewed for:** Which access records sit outside the homes their type names? Look at the actor, role, permission, entry point, and external system homes.

### Name Every Actor with Its Kind

**ID:** `litespec/core/access#name-every-actor-with-its-kind`

**Statement:** An actor MUST name its kind in `kind`.

**Rationale:** The kind tells a reviewer whether a human, an organization, or software stands behind the actor, which decides who answers for what it does. Without the kind, a reviewer assigns responsibility without knowing whether a person or software stands behind the actor.

**Example:** An employee actor carries `person`, and a payout service carries `system`.

**Checked by:** `litespec model check` reports `record-invalid` for an actor without `kind` and `field-invalid` for a kind outside person, organization, and system.

### Record Every Role Hold with Its Scope

**ID:** `litespec/core/access#record-every-role-hold-with-its-scope`

**Statement:** An actor that acts through a role MUST record the hold in `holds`.

**Rationale:** The hold is the grant: without it, the actor and the role sit on separate pages with no stated connection, and no reviewer can tell what the actor is allowed to do. The scope bounds each grant, so one role grants platform reach to one actor and self reach to another.

**Example:**

```json
"holds": [{ "role": "role/employee", "scope": "self" }]
```

**Reviewed for:** Which actor holds lack a role or a scope? Look at `holds` on each actor page.

### Scope Every Role

**ID:** `litespec/core/access#scope-every-role`

**Statement:** A role MUST name its scope in `scope`.

**Rationale:** The scope bounds what the role reaches: the same permission allows platform-wide action in one grant and own-record action in another. Without a named scope, every grant reads as product-wide, and the narrowest grant gets the widest reach.

**Example:** A scope of `team` bounds the role to one team, while `platform` bounds it to the whole product.

**Checked by:** `litespec model check` reports `record-invalid` for a role without `scope` and `field-invalid` for a scope outside kebab-case form.

### Grant Permissions Through Roles

**ID:** `litespec/core/access#grant-permissions-through-roles`

**Statement:** A role MUST name the permissions it allows in `allows`.

**Rationale:** The allows list is the middle of the chain: it joins the actors that hold the role to the use cases that cite its permissions in their grants. A role that names no permission grants nothing, so every use case its holders run breaks the chain at the middle link.

**Example:** `role/employee` allows `permission/submit-expense`, which the submit use case cites in its grant.

**Checked by:** `litespec model check` reports `record-invalid` for a role with no `allows` entry.

### State Every Permission with Its Meaning

**ID:** `litespec/core/access#state-every-permission-with-its-meaning`

**Statement:** A permission MUST state what it allows in `meaning`.

**Rationale:** The meaning is the one sentence a reviewer reads to decide whether the grant fits the role: a name alone carries no boundary. Two permissions with the same name pattern and no meanings drift into overlapping grants that no audit can separate.

**Example:** `permission/submit-expense` carries "Submit an expense for approval."

**Checked by:** `litespec model check` reports `record-invalid` for a permission without `meaning`.

### State the Scope of Every Permission

**ID:** `litespec/core/access#state-the-scope-of-every-permission`

**Statement:** Each permission MUST state the scope at which a role grants it.

**Rationale:** A grant without a named reach is decided in code: each check looks up the reach beside the permission list, and code then holds a second list of permissions.

**Example:** `permission/manage-copies` carries `scope: platform`, the same level as the librarian role that grants it.

**Checked by:** `litespec consumer` fails a permission entry without `scope`.

### Declare Access on Every Use Case

**ID:** `litespec/core/access#declare-access-on-every-use-case`

**Statement:** Each use case MUST declare its grant and its tenant scope in `access`.

**Rationale:** The two parts answer the access questions together: what grant the call checks and which tenant reach the call has. A use case without one part leaves that question to code, and two implementations of the same page enforce different boundaries. The door the call arrives through declares how the caller proved identity.

**Example:**

```json
"access": { "grant": "permission/manage-copies", "tenant": "own" }
```

**Checked by:** `litespec model check` reports `record-invalid` for a use case without `access`, `grant`, or `tenant`.

### Authenticate Every Caller Through a Declared Auth Kind

**ID:** `litespec/core/access#authenticate-every-caller-through-a-declared-auth-kind`

**Statement:** Each entry point MUST name its auth kinds from session, token, device, link, app, and none.

**Rationale:** The auth kind tells a reviewer how the caller proves identity before the grant is checked: a session for a signed-in person, a device for a managed scanner, a link for a single-purpose link, an app for a calling application, a token for a bearer token, and none for open calls. Without a declared kind, open calls and authenticated calls look alike, and a reviewer cannot tell which identity the grant was checked against.

**Example:** An expense approval names `session`, a self-checkout kiosk names `device`, and a public catalog names `none`.

**Checked by:** `litespec model check` reports `field-invalid` for an `auth` kind outside session, token, device, link, app, and none.

### Grant Every Use Case Through a Declared Grant

**ID:** `litespec/core/access#grant-every-use-case-through-a-declared-grant`

**Statement:** Each access declaration MUST name its grant as a permission, a self grant, anonymous, or system.

**Rationale:** The grant is the check the call passes: a permission for role-held action, a self grant for action on records the caller owns, anonymous for open calls, and system for work with no caller. A grant outside these four forms gives the checker no stated check to enforce, and each caller invents its own test.

**Example:** `permission/manage-copies` for staff work, `self(permission/read-loans)` for a reader reading their own loans, `anonymous` for a public catalog, and `system` for a self-checkout handler acting for the device.

**Checked by:** `litespec model check` reports `field-invalid` for a grant outside permission, self, anonymous, and system, and `reference-unknown` for a permission no record defines.

### Remove a Target That Must Be the Caller

**ID:** `litespec/core/access#remove-a-target-that-must-be-the-caller`

**Statement:** A message whose target must be the caller MUST carry no target and act on the caller, or declare a self grant that the authorization guard enforces.

**Rationale:** A target that must equal the caller is a value the caller could forge: each handler would compare it with the verified identity, and each comparison is authorization logic in a handler. Carrying no target leaves nothing to forge, and the call acts on the caller from verified claims. Where the target must stay on the message, a self grant states the scope beside the use case, and the guard enforces it.

**Example:** "My loans" lists the signed-in reader's loans and takes no reader id.

**Reviewed for:** Which use cases with a self grant name a caller-owned target in their inputs without a self declaration the guard enforces, or compare the caller with a target in a handler? Look at the inputs and handlers beside the self grants.

### Scope Every Grant to Its Tenant Reach

**ID:** `litespec/core/access#scope-every-grant-to-its-tenant-reach`

**Statement:** Each access declaration MUST name its tenant scope as own, cross, public, or none.

**Rationale:** The tenant scope bounds which tenant data the call may touch: own stays inside the caller tenant, cross reaches across tenants, public works beyond tenant boundaries, and none touches no tenant data. Without the scope, a read inside one tenant and a read across all tenants look the same, and the widest reach hides in the narrowest grant.

**Example:** An expense report names `own`, a staff search across tenants names `cross`, a public catalog names `public`, and a health check names `none`.

**Checked by:** `litespec model check` reports `field-invalid` for a tenant scope outside own, cross, public, and none.

### Route Cross Tenant Access Through the Security Queue

**ID:** `litespec/core/access#route-cross-tenant-access-through-the-security-queue`

**Statement:** A use case with cross tenant scope MUST pass through the security queue at its own tier.

**Rationale:** Work that reaches across tenants can read or write data that belongs to another tenant, which is the highest access risk a grant carries. The security queue puts that grant before the security reviewer separately from reviewed status, so no cross grant ships on a developer sample alone. Cross scope raises no tier: the tier follows the risk values the record carries, and the queue carries the review.

**Example:** A staff search with `tenant: cross` appears in the security queue of the Behavior diff until the security reviewer accepts it. It keeps the tier its risk values give it.

**Reviewed for:** Which cross grants shipped without security review, or gained a tier from cross scope alone? Look at the security queue in the Behavior diff.

### Cite the Setting or Plan Behind Every Entitlement

**ID:** `litespec/core/access#cite-the-setting-or-plan-behind-every-entitlement`

**Statement:** A grant that depends on a setting or a plan MUST cite that setting or plan.

**Rationale:** An entitlement allows one tenant what another is refused, so the grant alone reads as unequal treatment. Citing the setting or plan shows the commercial or configured reason behind the difference, and gives the reviewer the range and setter to judge it against.

**Example:** A submit command cites `setting/spending.receipt-threshold`, so an expense above the line with no receipt is refused by the policy setting rather than by role.

**Reviewed for:** Which entitlement grants lack a cited setting or plan? Look at the grant beside the settings each use case cites.

### Prove Hierarchies Derived Trust and Delegation with Scenarios

**ID:** `litespec/core/access#prove-hierarchies-derived-trust-and-delegation-with-scenarios`

**Statement:** A hierarchy, derived trust, or delegation pattern MUST prove its allowed and refused calls with scenarios.

**Rationale:** Patterns compose the same access parts into wider reach, so prose alone cannot show where the wider grant stops. Scenarios that walk an allowed call and a refused call pin the boundary as checked facts, and the checker confirms each one through the public boundary.

**Example:** A delegation story shows an assistant acting for a manager succeeding, and the same assistant acting without delegation ending in the derived forbidden result.

**Reviewed for:** Which hierarchy, trust, or delegation patterns lack an allowed and a refused scenario? Look at the scenarios on the use cases that carry the pattern.

### Record a Delegated Grant Inside One Tenant as a Role Assignment

**ID:** `litespec/core/access#record-a-delegated-grant-inside-one-tenant-as-a-role-assignment`

**Statement:** A delegated or temporary grant inside one tenant MUST be recorded as a role assignment with a validity window in `validFrom` and `validUntil`, held on an aggregate the access model reads and granted and revoked by its own use cases.

**Rationale:** Inside one tenant, delegation changes who holds a role for a time, not which grant forms exist. Recording the assignment with its window on an aggregate the access model reads gives the authorization guard one stated source for the deputy, the role, and the dates, and the granting and revoking use cases make the start and the end reviewable acts. A use case acting under the assignment declares its permission as usual, so no new access form appears. The tenancy extension adds only the cross-tenant form of delegation; it adds no form inside one tenant.

**Example:** In Claimwick, a manager delegating approval to a deputy for 2026-12-20 to 2027-01-05 is recorded as an assignment of the approver role to the deputy with `validFrom` of 2026-12-20 and `validUntil` of 2027-01-05, granted by its own grant use case and ended by its own revoke use case. The approve use case acting under it still declares `permission/approve-expense`.

**Reviewed for:** Which delegated or temporary grants inside one tenant lack a role assignment with `validFrom` and `validUntil` on an aggregate the access model reads, or lack their own granting and revoking use cases? Look at the assignments beside the actor holds and the use cases that act under them.

### Grant Across Tenants Only Through Declared Delegation

**ID:** `litespec/core/access#grant-across-tenants-only-through-declared-delegation`

**Statement:** A grant that acts across tenants through delegation MUST declare its delegation grant on both sides.

**Rationale:** Delegation lets one actor act for another, so a grant on one side alone leaves the other side unreviewed. Declaring the grant on both sides shows who may delegate and who may be acted for, and the tenancy extension states the fields each side carries.

**Example:** An assistant acts for a manager only where the manager delegation grant and the assistant acting grant both name the pairing.

**Reviewed for:** Which cross tenant delegation grants lack a declared grant on both sides? Look at the delegation grants the tenancy extension declares.

### Name Every Entry Point with Its Kind

**ID:** `litespec/core/access#name-every-entry-point-with-its-kind`

**Statement:** An entry point MUST name its kind in `kind`.

**Rationale:** The kind tells a reviewer how callers arrive: a browser serves people, an api serves callers over a boundary, a webhook receives inbound events, a device serves a managed scanner, a cli serves operators, an mcp channel serves a model, and served content serves a static file. Without the kind, a webhook and a page look alike.

**Example:** The library app carries `browser`, and a kiosk handshake carries `device`.

**Checked by:** `litespec model check` reports `record-invalid` for an entry point without `kind` and `field-invalid` for a kind outside browser, api, webhook, device, cli, mcp, and served-content.

### Describe Every Served Content Entry Point with Its Serving Fields

**ID:** `litespec/core/access#describe-every-served-content-entry-point-with-its-serving-fields`

**Statement:** A served content entry point MUST carry path, contentType, cacheControl, versioning, and fulfills.

**Rationale:** Served content is a file the product hands out, not behavior it runs, so the serving fields are the whole contract: where it lives, what it is, how long it caches, how it versions, and which requirement it carries. Without them, a legal text or a static script ships as an unreviewed file with no stated owner or freshness.

**Example:** A terms text carries its path, `text/html`, its cache window, its version rule, and the requirement it fulfills.

**Checked by:** `litespec model check` reports `record-invalid` for a served content entry point without path, contentType, cacheControl, versioning, or fulfills.

### Name the Actor for Every External System

**ID:** `litespec/core/access#name-the-actor-for-every-external-system`

**Statement:** An external system MUST name the actor it acts as in `actor`.

**Rationale:** The actor link attributes everything the system does to a party the access records know: journeys, holds, and permissions all start from actors. A system with no actor acts in the product as nobody, and its messages enter use cases with no stated party behind them.

**Example:** The text gateway names `actor/reader` as its actor.

**Checked by:** `litespec model check` reports `record-invalid` for an external system without `actor`.

### Declare Every Operation on Every External System

**ID:** `litespec/core/access#declare-every-operation-on-every-external-system`

**Statement:** An external system MUST declare every operation the product may call in `operations`.

**Rationale:** Each operation is one edge to outside behavior the product does not control. Without the declared list, a new outside call ships as an unreviewed dependency, and the security queue has no stated edge to review. Each operation carries its direction, timeout, response, and cache, as [Describe Every External System Operation](edges.md#describe-every-external-system-operation) states.

**Example:** A payout bank declares a `settle-batch` operation with its timeout, timeout handling, and response shape, which the payout effect cites as `calls`.

**Checked by:** `litespec model check` reports `record-invalid` for an external system without `operations` and `reference-unknown` for a `calls` edge naming no declared operation.

### Declare Erasure for Every External System

**ID:** `litespec/core/access#declare-erasure-for-every-external-system`

**Statement:** An external system MUST declare how personal data it holds is erased in `erasure`.

**Rationale:** Personal data sent outside stays outside unless some operation or event removes it, so the erasure declaration states the call, the integration event, or why removal is impossible. An impossible erasure cites the Intent decision that accepts the limit, so the gap reads as an accepted decision rather than a missed edge.

**Example:** A payout archive declares `{"mode": "impossible", "reason": "settled payouts belong to the bank"}` with the decision that accepts the limit, plus the sentence employees see.

**Checked by:** `litespec model check` reports `record-invalid` for an external system without `erasure` and `reference-unknown` for an impossible erasure without an Intent decision.

### Write a Summary on Every Actor and Role Page

**ID:** `litespec/core/access#write-a-summary-on-every-actor-and-role-page`

**Statement:** An actor or role page MUST carry a Summary section.

**Rationale:** The Summary states in plain sentences who the party is and what the grant is for, which the fields alone do not say. A reviewer meets the story first and the fields after, so the grant reads as a decision rather than a row.

**Checked by:** `litespec consumer` fails an actor or role page without an H2 `Summary`.

### Write a Summary on Every External System Page

**ID:** `litespec/core/access#write-a-summary-on-every-external-system-page`

**Statement:** An external system page MUST carry a Summary section.

**Rationale:** The Summary states in plain sentences what the outside system does for the product, which the message lists alone do not say. A reviewer meets the relationship first and the messages after, so the dependency reads as a decision rather than a row.

**Checked by:** `litespec consumer` fails an external system page without an H2 `Summary`.

### Keep Permission and Entry Point Lists to Entries

**ID:** `litespec/core/access#keep-permission-and-entry-point-lists-to-entries`

**Statement:** A permission list or entry point list page MUST NOT carry a body section.

**Rationale:** The lists hold entries only: each permission and entry point is one row with its fields, and prose around them would restate the meanings the entries already carry. A list with no body section stays readable as a list even as entries grow.

**Reviewed for:** Which permission or entry point lists carry a body section? Look at the permission and entry point lists for H2 sections.

## Conventions

### Use Three Conventional Scope Levels

**ID:** `litespec/core/access#use-three-conventional-scope-levels`

**Default:** Use `platform`, `tenant`, and `self` as role scopes.

**Replacement:** A project names its own scope levels in its role records.

**Rationale:** Three levels cover the grants most products need: product-wide administration, tenant-wide operation, and action on one's own records. A project with different boundaries names its own levels, and every role still carries exactly one scope.

**Reviewed for:** Which role scopes fall outside platform, tenant, and self without a recorded project convention? Look at `scope` on each role page.
