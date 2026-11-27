# Access

**Stack:** any

**Concern:** records

Access states who calls the product, which calls each caller makes, and how callers reach it. Actors name the acting parties, roles bundle permissions into grants with a stated reach, and each use case declares the grant and tenant reach its calls satisfy. Entry points and external systems state how callers reach the product. This page is for product teams shaping Intent actors and roles and for developers declaring Behavior access.

## Concepts

### How Actor, Role, and Permission Connect

Access follows one chain from actor to use case. An actor holds a role, the role carries its one scope, each permission names the roles granting it, and the use case declares the grant it checks. When someone asks why a call was allowed or refused, the chain gives the whole answer: which actor held which role, what reach the role scope states, which permission named that role, and which use case declared it.

This is role-based access control: permissions attach to roles, with no actor holding a permission directly, and the role scope bounds how far each permission reaches. Each link points from the later record to the earlier one: the permission lives in Behavior and names its Intent roles in `grantedTo`. The book gathers each role set from the permissions naming it.

### Actors, Roles, and Scopes

An actor is one party acting in the product: a person acting directly, an organization, a company or group acting as one party, or an outside system standing in a flow step. The `kind` field says which, so a reviewer knows who answers for what the actor does. Each actor states its responsibilities in `responsibilities` with the acts the product holds it to, and what the product assumes it knows in `knowledge`.

An outside system acting in a flow is an actor of kind `system`, and its external-system record names it in `represents`. Internal machinery gets no actor at all. A role is one named set of permissions granted together, holding the one scope of its grant.

The scope is a kebab-case level naming how far the grant reaches, such as `platform`, `tenant`, or `self`, and the role is the single place that reach is stated. The role states what the grant leaves out in `excludes`, with the acts inside its scope the grant leaves uncovered. Each scope level a role names has a glossary term stating what the level covers in this product.

An actor lists each role it holds in `holds`, by role identifier. A hold names the role alone: the reach comes from the role scope, and one hold widens or narrows nothing. Which tenant or team a person holds a role for is runtime data: the assignment binds a scope instance beside the level, the tenant or team the hold reaches, or the person for a `self` grant.

### Permissions

A permission is one allowed action, and each module lists its own in its permissions file. Each entry states in `meaning`, in one sentence, what it allows, and names in `grantedTo` the roles granting it. Role editors order the permission beside its peers with an optional `group`.

A permission name opens with a verb from the use-case verb table, a glossary verb of kind command or either, or `manage`. The opening verb is a naming convention alone: no permission implies another by its name, and `manage-` grants nothing beyond the use cases citing the permission. Where roles differ on a destructive use case, that use case takes its own permission carrying the use case risk.

The `risk` field marks a sensitive permission, and it is the single risk field any record carries. A permission guarding a query or use case disclosing personal or financial data carries the `personal-data` or `access` risk, just like a permission for a write. The file holds permission entries alone, and its explanation states the risks and boundaries of those entries in `excludes`, without restating a meaning or adding a grant.

### What the Access Field Declares

Each use case declares `access` in three fixed parts with four conditional ones: the grant the call passes, the tenant reach of the call, the tenant reason where the reach is `none` or `cross` in `tenantReason`, and, where they apply, the sign-in window, the caller-owned scope with its identity input, the authorization preconditions, and the system identity. The grant takes one of five forms: a permission for role-held action, a self grant on caller-owned records, `signed-in`, `anonymous`, or `system`. The tenant reach is `own` inside the caller tenant, `cross` across tenants, `public` beyond tenant boundaries, or `none` touching no tenant data.

The `recentSignInWindow` field states the window as an ISO 8601 duration, or names a `setting/` identifier holding the window. A call with an older sign-in ends in the derived sign-in-too-old result. A use case acting on the record identifying the caller states `scope: self` with the verified-claim input carrying the caller identity in `identityInput`, so the guard compares the target with that input instead of each handler comparing it.

A use case with authorization evaluating behavior rules names them in `preconditions`, each a precondition rule the use case cites, with a failing condition refusing the call with the rule refusal before the behavior runs. A use case run by people and system triggers states `runsAs: system` beside its person grant, while a use case granted `system` or `anonymous` states no `runsAs`. Refusals from access and input rules need no written result: a call with no authentication where some is needed ends in the derived unauthenticated result, a call without the grant ends in the derived forbidden result, and a call with inputs breaking their rules ends in the derived invalid-input result.

The entry point declares `authentication`, how the caller proves identity, from `session`, `token`, `device`, `link`, `app`, or `none`. The entry point authenticates, and the use case authorizes. Holding a permission bypasses no rule refusal: the call still meets each rule.

### Hierarchies, Delegation, and Entitlements

Hierarchies, derived trust, and delegation are patterns built from the same access parts, with no extra grant form. A hierarchy nests roles so a senior role is granted what its junior roles are granted. Derived trust lets one checked fact stand for another within one call, and delegation lets one actor act for another.

Each pattern proves its edge with scenarios: an allowed call with a refused call beside it. Inside one tenant, delegation is recorded as a role assignment with a validity window in `validFrom` and `validUntil` plus the bound scope instance, held on an aggregate the access model reads and granted and revoked by its own use cases. Across tenants, each side declares its delegation grant, and the grant names the pairing on both sides.

An entitlement is a grant depending on what the caller bought or configured, such as a plan limit or a policy setting. One tenant submits expenses above a receipt threshold while another is refused, and the grant alone reads as unequal treatment. The use case cites the setting or plan behind the grant, with the cited setting stating its range and setter.

### Entry Points and External Systems

An entry point is how people or systems reach the product from outside. Each entry point declares its `authentication`, and the protocol detail for devices lives in the devices extension. A browser entry point lists its served static content with path, contentType, cacheControl, versioning, and fulfills, so served content is content rather than a use case.

Use cases name the entry points reaching them in `via`, so each operation shows how callers arrive. An entry point also names its `audience`, its `session` lifetime and idle timeout as ISO 8601 durations, its `reauthenticateWhen`, and what it leaves out in `excludes`. A device caller authenticates with the `device` kind.

An external system is one system outside the bounded context exchanging messages with the product, such as a payment provider or a text gateway. It names the actor it represents in `represents`, and declares each operation the product calls with action, timeout, response, and cache behavior. It declares in `erasure` how personal data it holds is erased, or why erasure there is absent, with the Intent decision accepting the limit.

A hosted sign-in moves authentication to an identity provider outside the product. The product redirects the browser to the provider, the provider authenticates the person, and the product exchanges the returned code for tokens through its backend for frontend. The protocol is OpenID Connect with PKCE, so the browser holds no provider tokens.

Sign-in is behavior like any other: one use case starts it and one completes it, with the provider as an external system declaring its authorize, token, and logout operations. The start use case builds the redirect with its PKCE challenge. The complete use case exchanges the code, validates the tokens, finds or creates the person, and starts the session.

### Where Access Records Live

Each access record lives in the home its type names, as `litespec/core/model#write-every-record-in-its-home` states. The project model states the shared identifier and ownership rules.

| Type | Stored as | Home |
|:---|:---|:---|
| `actor` | page | The Intent actors home |
| `role` | page | The Intent roles home |
| `permission` | entry | The module permissions file |
| `entry-point` | page | The Behavior entry points home |
| `external-system` | page | The external systems home in Behavior, at product or module level |

An actor, role, or external-system page carries a `summary` stating what the record is for. Each module permissions file holds its permission entries, and its explanation file states the risks and boundaries of those entries.

## Standards

### Name Every Actor with Its Kind

**ID:** `litespec/core/access#name-every-actor-with-its-kind`

**Strength:** required

**Applies to:** artifact: record:actor

**Statement:** An actor MUST name its kind in `kind`.

**Rationale:** The kind says who stands behind the actor: a person answering directly, an organization answering as one party, or an outside system standing in a flow step. A reviewer assigns responsibility from that statement. The system kind pairs with the external-system record naming the actor in `represents`.

**Example:** An employee actor carries `person`. The library as a lending party carries `organization`, and the text gateway standing in a flow step carries `system`.

**Checked by:** `litespec model check` reports `record-invalid` for an actor without `kind` and `record-invalid` for a kind outside person, organization, and system.

### Record Every Role an Actor Holds

**ID:** `litespec/core/access#record-every-role-an-actor-holds`

**Strength:** required

**Applies to:** artifact: record:actor

**Statement:** An actor that acts through a role MUST list the role's identifier in `holds`.

**Rationale:** The hold joins the actor to its grant. Without it, the actor and the role sit on separate pages with no stated connection, and no reviewer tells what the actor is allowed to do. The hold names the role alone, and the reach comes from the role scope. This follows decision/0055-state-each-access-scope-once and decision/0083-evaluate-access-in-one-order.

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

**Rationale:** The same permission allows product-wide action in one grant and own-record action in another. The role scope decides what each grant reaches. Stating the reach on each hold or each permission as well keeps three copies of one value, and the copies drift apart.

**Example:** A scope of `team` bounds the role to one team. A scope of `platform` bounds it to the whole product.

**Checked by:** `litespec model check` reports `record-invalid` for a role without `scope` and `record-invalid` for a scope outside kebab-case form. A `scope` on a permission entry or on a hold fails the schema as an unknown property.

### Grant Permissions Through Roles

**ID:** `litespec/core/access#grant-permissions-through-roles`

**Strength:** required

**Applies to:** artifact: record:role, record:permission

**Statement:** Each permission MUST name the roles that grant it in `grantedTo`, with each role's `excludes` stating what the grant does not cover.

**Rationale:** The grant is the middle of the chain. It joins the actors holding the role to the use cases citing its permissions in their grants. Each permission in Behavior names its Intent roles in `grantedTo`, so the book gathers each role set from the permissions naming it.

**Example:** `permission/request-expense` names `role/employee` in `grantedTo`. The submit use case cites that permission in its grant, closing the chain.

**Checked by:** `litespec model check` reports `record-invalid` for a permission with no `grantedTo` entry or a role with no `excludes`, `permission-unused` for a permission no role grants, and `role-unused` for a role no permission names.

### State Every Permission with Its Meaning

**ID:** `litespec/core/access#state-every-permission-with-its-meaning`

**Strength:** required

**Applies to:** artifact: record:permission

**Statement:** A permission MUST state what it allows in `meaning`.

**Rationale:** The `meaning` is the one sentence a reviewer reads to judge the fit of a grant. A name alone carries no boundary. Two grants with the same name pattern and no meanings drift into overlapping grants no audit separates.

**Example:** `permission/request-expense` carries the sentence on submitting an expense for reimbursement. The sentence draws the boundary the short name leaves open.

**Checked by:** `litespec model check` reports `record-invalid` for a permission without `meaning`.

### Declare Access on Every Use Case

**ID:** `litespec/core/access#declare-access-on-every-use-case`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each use case MUST declare its grant and its tenant scope in `access`.

**Rationale:** The grant and the tenant scope answer the two access questions together: what check the call passes, and which tenant data it touches. A use case missing one part leaves that question to code. The entry point the call arrives through declares how the caller proved identity.

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

**Rationale:** The authentication kind says how the caller proved identity before the grant check. A session marks a signed-in person, a device marks a managed scanner, and `none` marks open calls. Without a declared kind, open calls and authenticated calls look alike.

**Example:** An expense submission names `session`. A library self-checkout kiosk names `device`, and a public catalog names `none`.

**Checked by:** `litespec model check` reports `record-invalid` for an `authentication` kind outside session, token, device, link, app, and none.

### Authorize Every Use Case Through a Declared Grant

**ID:** `litespec/core/access#authorize-every-use-case-through-a-declared-grant`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each access declaration MUST name its grant as a permission, a self grant, signed-in, anonymous, or system.

**Rationale:** The grant is the check the call passes. Five forms cover the ground: a permission for role-held action, a self grant for action on owned records, signed-in for an authenticated caller with no permission, anonymous for open calls, and system for work an internal trigger runs. A grant outside these five leaves the checker with no stated check to enforce.

**Example:** `permission/manage-copies` guards staff work, and `self(permission/read-loans)` lets a reader read owned loans. `signed-in` opens a workspace to any signed-in person with no permission, while `anonymous` opens a public catalog and `system` covers an archive use case only its retention job runs.

**Checked by:** `litespec model check` reports `record-invalid` for a grant outside permission, self, signed-in, anonymous, and system, and `reference-unknown` for a permission no record defines.

### Admit the System Identity on Every System-Run Use Case

**ID:** `litespec/core/access#admit-the-system-identity-on-every-system-run-use-case`

**Strength:** required

**Applies to:** artifact: record:use-case, record:reaction, record:job

**Statement:** A use case that a reaction or a job runs, commands, or queries MUST admit the system identity: its grant is `system` or `anonymous`, or it states `runsAs: system` beside its grant for person callers.

**Rationale:** A reaction or a job runs with no person behind it. The call it sends carries the system identity, and a permission, self, or signed-in grant admits people alone. Each system run of such a use case is refused at runtime without `runsAs`, so stating it beside the person grant admits the system identity too.

**Example:** Claimwick erase-personal-data grants `permission/remove-personal-data` to finance staff and states `runsAs: system`. The nightly erase job runs it beside the staff, and without `runsAs` each nightly run is refused.

**Checked by:** `litespec model check` reports `system-access-missing` for a reaction or job that runs, commands, or queries a use case whose access admits no system identity, and `record-invalid` for `runsAs` beside a `system` or `anonymous` grant.

### Limit the Age of a Sign-In

**ID:** `litespec/core/access#limit-the-age-of-a-sign-in`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A use case that requires a recent sign-in MUST state the window in `recentSignInWindow` as an ISO 8601 duration or a `setting/` identifier.

**Rationale:** Some operations need a fresh sign-in. Approving a large payout on a week-old session differs from approving it just after signing in. A window one use case needs sits beside it as a duration, while a window several use cases share lives in a setting. Settled by decision/0074-declare-access-settings-and-deadlines.

**Example:** Claimwick reimbursement states `recentSignInWindow: PT15M`. A session older than fifteen minutes ends in the derived sign-in-too-old result, and a shared product window lives in a setting each use case names.

**Checked by:** `litespec model check` reports `record-invalid` for a `recentSignInWindow` that is neither an ISO 8601 duration nor a `setting/` identifier, or that names a setting of another value type, and `reference-unknown` for a setting no record defines.

### Scope Every Grant to Its Tenant Reach

**ID:** `litespec/core/access#scope-every-grant-to-its-tenant-reach`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each access declaration MUST name its tenant scope as own, cross, public, or none.

**Rationale:** The tenant scope bounds which tenant data the call touches. `own` stays inside the caller tenant, `cross` reaches across tenants, `public` works beyond tenant boundaries, and `none` touches no tenant data. Without the scope, a read inside one tenant and a read across all tenants look the same.

**Example:** An expense report names `own`, and a staff search across tenants names `cross`. A public catalog names `public`, and a health check names `none`.

**Checked by:** `litespec model check` reports `record-invalid` for a tenant scope outside own, cross, public, and none.

### Name the Caller Identity of a Self Scope

**ID:** `litespec/core/access#name-the-caller-identity-of-a-self-scope`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A use case declaring `scope: self` MUST name its caller-identity input in `identityInput`, with `origin: verified-claim` identifying an actor.

**Rationale:** A scope without its identity input leaves the guard guessing which value is the caller. Handlers then compare their own field with the verified identity, and each comparison is authorization logic sitting in a handler. Naming the verified input beside the scope gives the guard the one value to compare the target with.

**Example:** Claimwick list-expenses declares `scope: self` with `identityInput: employee`. The employee input carries `origin: verified-claim`, and a call lists that employee owned expenses alone.

**Checked by:** `litespec model check` reports `record-invalid` for `scope` without `identityInput` or `identityInput` without `scope`, for an identity input naming no input or carrying another origin, and for an identity input identifying no actor, and `reference-unknown` for an identity input whose `identifies` names no record.

### Name Every Authorization Precondition

**ID:** `litespec/core/access#name-every-authorization-precondition`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** Each `preconditions` entry MUST name a rule of kind `precondition` that the use case also cites.

**Rationale:** An authorization condition the behavior leaves unstated lives in prose beside the definition. Each definition restates it, and the copies drift from the behavior they guard. Naming a precondition rule the use case already cites keeps one rule with one refusal.

**Example:** Claimwick approve-expense names `rule/expense.approver-is-not-submitter` in `preconditions`. The rule is kind `precondition`, the use case cites it in its steps, and a refused call ends in the rule refusal.

**Checked by:** `litespec model check` reports `record-invalid` for a precondition of another rule kind or one the use case cites nowhere in its steps, inputs, or results, and `reference-unknown` for a precondition no rule holds.

### Evaluate Access in One Order

**ID:** `litespec/core/access#evaluate-access-in-one-order`

**Strength:** required

**Applies to:** artifact: record:use-case, record:aggregate

**Statement:** Access MUST be evaluated in one order: the authenticated principal, then the role assignments whose bound scope instance matches the target, then the permission, then the use case tenant scope, then the self-ownership restriction, then delegation validity, with several assignments combining as a union.

**Rationale:** Each step narrows what the next step checks. Identity comes before assignments, assignments before the permission they carry, the permission before the reach of its use, and ownership and delegation last as restrictions on an otherwise allowed call. One order gives two implementations the same allowed and refused calls. Settled by decision/0083-evaluate-access-in-one-order.

**Example:** A Claimwick approver holds the approver role for one cost center. A call evaluates the session principal, then the approver assignment matching the expense cost center, then the permission, then the own tenant scope, with an assignment for another cost center ending in the derived forbidden result.

**Reviewed for:** Does every call evaluate principal, assignments, permission, tenant scope, ownership, and delegation in that order? Walk one allowed and one refused call through the steps.

### Grant Across Tenants Only Through Declared Delegation

**ID:** `litespec/core/access#grant-across-tenants-only-through-declared-delegation`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A grant that acts across tenants through delegation MUST declare its delegation grant on both sides.

**Rationale:** Delegation lets one actor act for another. A grant on one side alone leaves the other side unreviewed. Declaring the grant on both sides shows who delegates and who is acted for, with the tenancy extension stating the fields each side carries.

**Example:** An assistant acts for a manager only where both delegation grants name the pairing. The manager side names who is acted for, and the assistant side names who acts.

**Reviewed for:** Does every cross-tenant delegation grant declare its grant on both sides? Check the delegation grants the tenancy extension declares.

### Describe Every Served Content Entry with Its Serving Fields

**ID:** `litespec/core/access#describe-every-served-content-entry-with-its-serving-fields`

**Strength:** required

**Applies to:** artifact: record:entry-point

**Statement:** Each served content entry on a browser entry point MUST carry path, contentType, cacheControl, versioning, and fulfills.

**Rationale:** Served content is a file the product hands out rather than behavior it runs. The five serving fields are the whole contract for it. Without them, a legal text or a static script ships as an unreviewed file with no stated requirement or freshness.

**Example:** A terms text carries its path with `text/html`. It states its cache window, its version rule, and the requirement it fulfills.

**Checked by:** `litespec model check` reports `record-invalid` for a served content entry without path, contentType, cacheControl, versioning, or fulfills.

### Declare Erasure for Every External System

**ID:** `litespec/core/access#declare-erasure-for-every-external-system`

**Strength:** required

**Applies to:** artifact: record:external-system

**Statement:** An external system MUST declare how personal data it holds is erased in `erasure`.

**Rationale:** Personal data sent outside stays outside unless some operation or event removes it. The erasure declaration states the call, the forwarding event, or the reason removal is absent. An unsupported, manual, or not-applicable erasure cites the Intent decision accepting the limit.

**Example:** A payout archive declares an unsupported erasure with the accepting decision. The declaration carries the reason settled payouts belong to the bank, plus the sentence employees see.

**Checked by:** `litespec model check` reports `record-invalid` for an external system without `erasure` and `reference-unknown` for an unsupported, manual or not-applicable erasure without an Intent decision.

### Model a Hosted Sign-In as Use Cases

**ID:** `litespec/core/access#model-a-hosted-sign-in-as-use-cases`

**Strength:** required

**Applies to:** artifact: record:use-case, record:external-system

**Statement:** A sign-in through a hosted identity provider MUST be modeled as use cases that start and complete the sign-in, with the provider as an external system.

**Rationale:** The product signs people in through a hosted provider with OpenID Connect and PKCE through its backend for frontend. The browser holds no provider tokens at any point. The start use case builds the redirect with its challenge, and the complete use case exchanges the code, validates the tokens, finds or creates the person, and starts the session. This follows decision/0103-dotnet-stack-binding.

**Example:** Claimwick signs employees in through its hosted provider. `use-case/identity.start-sign-in` builds the redirect, `use-case/identity.complete-sign-in` exchanges the code and starts the session, and `external-system/identity-provider` declares the authorize, token, and logout operations.

**Reviewed for:** Does every hosted sign-in run as start and complete use cases against the provider external system, with anonymous grants and declared provider calls? Check the sign-in use cases beside the provider system.

### Define Each Scope Level in the Glossary

**ID:** `litespec/core/access#define-each-scope-level-in-the-glossary`

**Strength:** required

**Applies to:** artifact: record:role

**Statement:** Each scope level a role names MUST have a glossary term titled with the level and Scope, stating what the level covers in this product.

**Rationale:** A bare level such as `platform` reads the same in each product, and each product means a different reach by it. The glossary term states that reach once for the product. A reviewer reads what `platform` covers here instead of guessing from the word. This follows decision/0055-state-each-access-scope-once.

**Example:** Claimwick `platform` scope has a glossary term, Platform Scope. The term states coverage of each expense in the Claimwick installation.

**Checked by:** `litespec model check` reports `scope-term-missing` for a role scope with no glossary term titled with the level and Scope.

### Name Each Permission by a Use-Case Verb

**ID:** `litespec/core/access#name-each-permission-by-a-use-case-verb`

**Strength:** advisory

**Applies to:** artifact: record:permission

**Statement:** Each permission name SHOULD open with a verb from the use-case verb table, a glossary verb of kind command or either, or with `manage`.

**Deviation:** A permission name an access editor already lists under another verb.

**Rationale:** The first word of a permission name tells a reader whether the grant reads or changes, and which kind of use case it covers. The verb table gives each opening word one meaning across the product. A name opening with an unlisted word says nothing about what it includes.

**Example:** `permission/approve-expense` opens with `approve` and `permission/manage-policy` opens with `manage`, so both pass. `permission/handle-documents` opens with an unlisted word, so it is reported.

**Checked by:** `litespec model check` reports `permission-verb-unknown` (level G) for a permission name whose opening verb is neither in the table, nor a glossary verb of kind command or either, nor `manage`.

## Conventions

None.
