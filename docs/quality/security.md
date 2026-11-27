# Security

**Stack:** any
**Concern:** application

Security covers who acts, what an operation touches, and what it keeps: authentication, actor resolution, authorization, trust boundaries, secrets, browser defenses, personal data, abuse limits, audit events, and release security checks. It applies to anyone who builds or reviews an operation, an endpoint, a frontend, or a release.

## Concepts

### Actors, Roles, and Permissions

Each protected operation answers one question: who acts on which target under which grant. The actor is whoever acts: a person, a company, a provider, a system, or a guest. An actor holds a role, and the role scope sets its reach, such as one organization.

Each permission names the roles that hold it in `grantedTo`, and each use case declares the permissions it needs. The application guard authorizes each protected operation from those declarations, so two operations about the same record ask the same question. The use case that approves an expense declares the permission it needs, and the guard resolves the caller roles at the scope of that expense.

### Trust Boundaries

A trust boundary is each point where data crosses from one party to another: a request arriving at the API, a file upload, a provider response, database content rendered into a new output, or generated text shown to a user. Each boundary validates what crosses it before use, because data that was safe in its old context turns unsafe in the new one. Application validators check message shape, while Domain checks business invariants. An expense receipt upload gets validated as a file at the boundary before each use case reads it.

### Personal Data and Its Stores

Personal data spreads further than the database: logs, search indexes, outboxes, snapshots, and backups each hold their own copy. Each store that holds personal data keeps its own inventory entry naming the fields, the purposes, and the retention. Each export, erasure, and breach answer then starts from the same map instead of a fresh search.

### Identity, Sessions, and Secrets

The backend accepts one thing: a token whose issuer, audience, signature, lifetime, and named claims validate through OIDC and JWT Bearer. Provider-specific login and session behavior belongs to a frontend or an extension, since the API rechecks no login flow it sees nowhere. An unsigned token, an unvalidated decode, or a client-supplied identity header mints an identity for anyone, so none gets accepted.

Claims mapping has one home: a narrow accessor in WebApi that maps verified claims once to typed identity, roles, and scopes. Application receives the actor through input that WebApi creates, or through a narrow trusted port when behavior stays host-independent. A second accessor invites a second interpretation of the same claims.

Platform secret stores hold the values, and diagnostics redact sensitive configuration before emitting it, so the value enters the observable stream nowhere. Each production secret carries an inventory entry naming its storage location and next rotation date, with revocation and recovery in a runbook. Production values live on the host, and the repository example file holds placeholders only.

### Data Duties

A use case collects, returns, logs, exports, and retains only the fields its input and results declare. A use case handling sensitive data states its classification, access, retention, deletion, and audit behavior on its page, so the promise sits beside the fields it covers. Erasure and export reach each store on the inventory, and a backup carries a retention bound instead, so the data ages out when the backup expires.

A digest without a salt reverses by table lookup, so it gets the same handling as the data itself. Live data at rest carries encryption, with a visible gap waiver where encryption is not in place yet. The breach procedure names the person who acts and a backup person, and meets the deadline the applicable constraint record states.

### Browser, Origin, and Release Checks

The cross-origin policy lists its allowed origins, methods, headers, and credential mode, with no wildcard origin. Deployments with more than one replica read their rate and concurrency counters from one shared store, so the applied limit equals the declared one. Each security audit event records actor, action, target, outcome, timestamp, trace identifier, and reason, and stays available for incident review under a documented retention and access policy.

One quality requirement carries the security target as an ASVS 5.0 level, with its measure. The ZAP baseline runs the spider with passive scanning on each pull request, with WARN, FAIL, and IGNORE config and a JSON report under `TestResults/` showing no medium or high alert. Each pull request gets a scan for security findings from a free static analyzer: CodeQL on public repositories, a free alternative with its choice recorded in CI documentation on private ones, with each accepted finding living as a gap waiver.

The browser reads the secure headers together: Content Security Policy, frame restrictions, content-type protection, referrer policy, and transport security in one host location. Authorization gets proved one caller at a time: anonymous, invalid-token, unrelated-actor, owner, and each privileged grant for each protected operation. A UI redirect or a hidden control counts as no evidence.

## Standards

### Derive the Actor from Claims

**ID:** `litespec/quality/security#derive-the-actor-from-claims`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler, endpoint

**Statement:** Actor resolution MUST map verified claims to identity, roles, and scopes through the one current-actor accessor in WebApi.

**Rationale:** The actor comes from verified claims, not from the request. The WebApi boundary maps those claims to identity, roles, and scopes through the one current-actor accessor, and each operation reads the actor from there. Request data names anyone the caller wants, so it supplies the current actor nowhere.

**Reviewed for:** Does WebApi map verified claims through the one current-actor accessor, with no request data supplying the actor? Look at the actor resolution in WebApi.

### Authorize Each Target Resource

**ID:** `litespec/quality/security#authorize-each-target-resource`

**Strength:** required

**Applies to:** artifact: use-case, command-handler, query-handler

**Statement:** The application guard MUST authorize every protected operation from its use case's declared permissions, as `litespec/core/backend-application#enforce-target-authorization-in-the-pipeline` states, with endpoints adding only coarse policies.

**Rationale:** Two operations about the same record ask the same authorization question. The use case declares the permissions it needs, the guard resolves the actor roles at the scope of the target, and the decision follows from that declaration. A message whose target is the caller itself carries no target, and the guard decides while endpoints add coarse policies only.

**Reviewed for:** Does each protected operation pass the application guard from its use case's declared permissions? Look at the guard decision for each protected operation in the authorization tests and the pipeline.

### Restrict a Protected Collection in the Database

**ID:** `litespec/quality/security#restrict-a-protected-collection-in-the-database`

**Strength:** required

**Applies to:** artifact: query-handler

**Statement:** A protected collection query MUST apply its authorization scope as a database predicate, before paging, rather than filter a materialized result.

**Rationale:** A collection has no single target, so the per-target check has nothing to resolve against. The scope becomes part of the database query itself. Reading the unrestricted set exposes the rows to the process, and a page assembled from them leaks the total count even when each row drops.

**Example:** A Claimwick query for the expenses of an organization adds the organization predicate to the database query. It loads no full set first.

**Reviewed for:** Does each protected collection query apply its authorization scope as a database predicate before paging? Look at each query handler that returns a collection.

### Validate at Trust Boundaries

**ID:** `litespec/quality/security#validate-at-trust-boundaries`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** A trust boundary MUST validate transport shape and file metadata before use, leaving message checks to application validators as `litespec/core/backend-application#separate-input-validation-from-invariants` states.

**Rationale:** Each layer validates what it sees: the boundary checks transport shape and file metadata, the application validators check message shape, values, length, range, format, and pagination bounds, and Domain checks business invariants. Database content, provider responses, file contents, and generated text stay untrusted when they cross into a new output context, because validation for one context proves nothing about the next.

**Reviewed for:** Does each trust boundary validate transport shape and file metadata before use? Look at the endpoints, the upload handlers, and the provider response readers.

### Keep Secrets Out of Tracked and Observable Data

**ID:** `litespec/quality/security#keep-secrets-out-of-tracked-and-observable-data`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A secret MUST NOT appear in source, logs, traces, exception messages, generated files, URLs, snapshots, container layers, or browser storage.

**Rationale:** Each place a secret lands is a place it leaks from: source reaches each clone, logs and traces reach each dashboard reader, URLs reach each proxy log, snapshots and container layers outlive the process, and browser storage reaches the user. Platform secret stores hold the values instead, and diagnostics redact sensitive configuration before emitting it, so the value enters the observable stream nowhere.

**Reviewed for:** Does any source, log, trace, exception message, generated file, URL, snapshot, container layer, or browser storage hold a secret? Search the tree and a sample of log and trace output for key and token values.

### Parameterize Database Input

**ID:** `litespec/quality/security#parameterize-database-input`

**Strength:** required

**Applies to:** artifact: repository

**Statement:** An external value reaching the database MUST travel through a provider query API or a parameterized SQL parameter.

**Rationale:** String concatenation or interpolation into raw command text makes the value part of the statement, so a crafted value rewrites the query. A provider query API or a parameterized SQL parameter keeps the value as data. Under the baseline, the provider query API is the Marten API, and it parameterizes.

**Reviewed for:** Does each external value reach the database through a query API call or a SQL parameter? Look at the repository code and each raw SQL file.

### Limit Public Error Detail

**ID:** `litespec/quality/security#limit-public-error-detail`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** A public error MUST use the error contract `litespec/core/backend-api#return-stable-problem-details` defines.

**Rationale:** The HTTP API rules own the error contract, so each public error carries the same stable shape with nothing extra. A stack trace, SQL, connection detail, provider body, uncontracted internal identifier, or authorization reason that reveals resource existence hands an attacker the structure of the system with each error.

**Reviewed for:** Does each public error carry the HTTP API error contract shape, with no stack trace, SQL, or provider body? Look at the error responses the endpoint tests assert.

### Protect Browser Boundaries

**ID:** `litespec/quality/security#protect-browser-boundaries`

**Strength:** required

**Applies to:** artifact: component, route, form

**Statement:** A frontend MUST apply framework escaping, a documented Content Security Policy, secure headers, redirect validation, forgery protection, and sanitization of allowed rich content.

**Rationale:** The browser renders each response as page content, so injected scripts, forged requests, malicious redirects, and hostile markup arrive as ordinary content. Each listed defense blocks one of those attacks. A route guard or hidden control only changes what the user sees, so it replaces API authorization at no point.

**Reviewed for:** Does each frontend ship framework escaping, a Content Security Policy, secure headers, redirect validation, forgery protection, and rich-content sanitization? Look at the frontend host configuration and the response headers it sends.

### Keep Personal Data Out of Logs

**ID:** `litespec/quality/security#keep-personal-data-out-of-logs`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A log, trace, or diagnostic record MUST NOT carry personal data in clear.

**Rationale:** Once a log line leaves the process, it flows through collectors, dashboards, and long-term archives the product controls nowhere. Redaction at the sink is one pipeline stage that fails once, and by then the cleartext is downstream. A field that leaves the process in no log line stays out of that pipeline.

**Reviewed for:** Do logs, traces, and diagnostic records carry personal data in clear? Search the log sinks and a sample of recent log output for personal fields.

### Bound Abuse at Exposed Endpoints

**ID:** `litespec/quality/security#bound-abuse-at-exposed-endpoints`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** A public, authentication, webhook, search, or upload endpoint MUST declare its limit as a setting record with key and rejection response.

**Rationale:** Public, authentication, webhook, search, and upload endpoints invite abuse: credential stuffing, scraping, flooding. Declaring the bound as a setting record names the key that isolates one caller from another and the rejection response the caller sees. The rejection is `429` with a stable code, so legitimate callers back off and retry.

**Reviewed for:** Does each public, authentication, webhook, search, and upload endpoint declare its setting with key and rejection response? Look at the setting records beside each endpoint.

## Conventions

### Use Deny-by-Default Policies

**ID:** `litespec/quality/security#use-deny-by-default-policies`

**Strength:** default

**Applies to:** artifact: endpoint; layer: behavior

**Default:** Set a fallback policy requiring an authenticated user, and mark each public endpoint with `AllowAnonymous` intentionally.

**Replacement:** A consumer records an endpoint-protection convention naming how each endpoint is protected.

**Rationale:** Without a fallback, a new endpoint nobody secured is public by accident. A fallback policy that asks for an authenticated user keeps each new endpoint protected until somebody marks it public on purpose with `AllowAnonymous`. Named role or scope policies add coarse checks while the guard keeps resource authorization.

**Reviewed for:** Does each new endpoint ask for authentication unless it carries `AllowAnonymous` on purpose? Look at the fallback policy and each endpoint marked `AllowAnonymous`.

## Reference Example

This informative example demonstrates `litespec/quality/security#authorize-each-target-resource` and `litespec/quality/security#limit-public-error-detail`.

A request for `GET /api/posts/{id}` on a private draft filters by the post ID and the author ID derived from claims together. A different author receives the documented not-found or forbidden response and learns none of the protected fields.
