# Security

## Intent

Security answers 3 questions for every operation: who acts, what it touches, and what it keeps. This page states how a LiteSpec product answers them through authentication, actor resolution, authorization, trust boundaries, secrets, browser defenses, personal data, abuse limits, audit events, and release security checks, for anyone who builds or reviews an operation, an endpoint, a frontend, or a release. A product that follows it proves every operation runs for the right actor, touches only what it may, and keeps sensitive data safe.

## Concepts

### Actors, Roles, and Permissions

Every protected operation answers one question: who may do what to which target. An actor is whoever acts: a person, a company, a provider, a system, or a guest. An actor holds a role at a scope, such as one organization; a role allows permissions from the shared closed list; and each use case declares the permissions it needs. The application guard authorizes every protected operation from those declarations, so two operations about the same record ask the same question.

### Trust Boundaries

A trust boundary is any point where data crosses from one owner to another: a request arriving at the API, a file upload, a provider response, database content rendered into a new output, or generated text shown to a user. Each boundary validates what crosses it before use, because data that was safe in its old context may be unsafe in the new one. Application validators then check message shape, while Domain checks business invariants.

### Personal Data and Its Stores

Personal data spreads further than the database: logs, search indexes, outboxes, snapshots, and backups each hold their own copy. Each store that holds personal data keeps its own inventory entry naming the fields, the purposes, and the retention, so every export, erasure, and breach answer starts from the same map.

### Security Review

Three changes move trust or data outward: an access tenant scope widening to cross, a new calls effect on an outside operation, and a new personal or sensitive data class. The behavior diff lists them in the security queue, a list to read rather than a gate, for review separately from the developer review.

## Standards

### Keep Backend Authentication Provider-Neutral

**ID:** `litespec/quality/security#keep-backend-authentication-provider-neutral`

**Statement:** A backend MUST validate issuer, audience, signature, lifetime, and required claims through OIDC and JWT Bearer.

**Rationale:** The backend accepts one thing: a token whose issuer, audience, signature, lifetime, and required claims validate through OIDC and JWT Bearer. Provider-specific login and session behavior belongs to a frontend or an extension, because the API cannot recheck a login flow it cannot see. An unsigned token, an unvalidated decode, or a client-supplied identity header is never accepted: each one lets a caller mint its own identity.

**Reviewed for:** The backend authentication setup validates issuer, audience, signature, lifetime, and required claims, with no identity header accepted.

### Derive the Actor from Claims

**ID:** `litespec/quality/security#derive-the-actor-from-claims`

**Statement:** Actor resolution MUST follow `litespec/quality/security#use-one-current-actor-abstraction`.

**Rationale:** Actor resolution has one owner: the WebApi boundary, which maps verified claims to identity, roles, and scopes through the one current-actor accessor. Request data never supplies the current actor, because anything the caller sends can name anyone. One resolution path keeps every operation answering "who acts" from the same verified source.

**Reviewed for:** Actor resolution in WebApi maps verified claims through the one current-actor accessor.

### Authorize Each Target Resource

**ID:** `litespec/quality/security#authorize-each-target-resource`

**Statement:** The application guard MUST authorize every protected operation from its use case's declared permissions, as `litespec/backend/application#enforce-target-authorization-in-the-pipeline` states, with endpoints adding only coarse policies.

**Rationale:** One decision from one declaration stops two operations about the same record from asking different questions. The use case declares the permissions it needs, the guard resolves the actor's roles at the target's scope, and the decision follows. A message whose target must be the caller carries no target (`litespec/core/access#remove-a-target-that-must-be-the-caller`). A handler never compares the caller with a target and never throws the forbidden refusal itself; the guard decides. Endpoints add only coarse policies, because a policy that cannot see business data cannot decide about an aggregate.

**Reviewed for:** Each protected operation passes the application guard from its use case's declared permissions.

### Restrict a Protected Collection in the Database

**ID:** `litespec/quality/security#restrict-a-protected-collection-in-the-database`

**Statement:** A protected collection query MUST apply its authorization scope as a database predicate, before paging, rather than filter a materialized result.

**Rationale:** A collection has no single target, so the check that `litespec/quality/security#authorize-each-target-resource` states has nothing to resolve against: the scope must become part of the database query itself. Reading the unrestricted set already exposes the rows to the process, and a page assembled from them leaks the total count even when every row is dropped. Filtering after the load is the shape [OWASP lists as broken object level authorization](https://owasp.org/www-project-top-ten/2021/A01_2021-Broken_Access_Control/).

**Example:** A query for an organization's orders adds the organization predicate to the database query. It does not load every order and drop the ones the caller may not read.

**Reviewed for:** Each protected collection query carries its authorization scope as a database predicate before paging.

### Route Tier-1 Access and Data Changes Through the Security Reviewer

**ID:** `litespec/quality/security#route-tier-1-access-and-data-changes-through-the-security-reviewer`

**Statement:** A change that widens access tenant scope to cross, adds a calls effect, or adds a personal or sensitive data class MUST appear in the security queue for review separately from the developer review.

**Rationale:** A cross-tenant grant, a new outside call, and a new personal-data flow each move trust or data further than the developer review sees: the use case still reads correctly while the exposure grows. The security queue in the behavior diff lists each of the three, so one reviewer judges them together and the decision stays visible beside the change.

**Reviewed for:** Which cross-scope access grants, new calls effects, and new personal-data flows shipped without security review? Look at the security queue of the behavior diff.

### Validate at Trust Boundaries

**ID:** `litespec/quality/security#validate-at-trust-boundaries`

**Statement:** A trust boundary MUST validate transport shape and file metadata before use, leaving message checks to application validators as `litespec/backend/application#separate-input-validation-from-invariants` states.

**Rationale:** Message shape, required values, length, range, format, and pagination bounds stay with the application validators that `litespec/backend/application#separate-input-validation-from-invariants` names, and business invariants stay with Domain. Each layer then answers only the questions it can see. Database content, provider responses, file contents, and generated text stay untrusted when they cross into a new output context, because validation for one context proves nothing about the next.

**Reviewed for:** Each trust boundary validates transport shape and file metadata before use.

### Keep Secrets Out of Tracked and Observable Data

**ID:** `litespec/quality/security#keep-secrets-out-of-tracked-and-observable-data`

**Statement:** A secret MUST NOT appear in source, logs, traces, exception messages, generated files, URLs, snapshots, container layers, or browser storage.

**Rationale:** Every place a secret lands is a place it leaks from: source reaches every clone, logs and traces reach every dashboard reader, URLs reach every proxy log, snapshots and container layers outlive the process, and browser storage reaches the user. Platform secret stores hold the values instead, and diagnostics redact sensitive configuration before it is emitted, so the value never enters the observable stream.

**Reviewed for:** Source, logs, traces, messages, files, URLs, snapshots, layers, and browser storage hold no secret.

### Parameterize Database Input

**ID:** `litespec/quality/security#parameterize-database-input`

**Statement:** An external value reaching the database MUST travel through a provider query API or a parameterized SQL parameter.

**Rationale:** String concatenation or interpolation into raw command text makes the value part of the statement, so a crafted value rewrites the query. A provider query API or a parameterized SQL parameter keeps the value as data. The provider query API is Marten's under the baseline and EF Core's under its extension, and both parameterize. The reporting extension owns the reviewed raw SQL patterns.

**Reviewed for:** Each external value reaches the database through a query API call or a SQL parameter.

### Limit Public Error Detail

**ID:** `litespec/quality/security#limit-public-error-detail`

**Statement:** A public error MUST use the error contract `litespec/backend/api#return-stable-problem-details` defines.

**Rationale:** The error contract has one owner: the HTTP API rules. Every public error then carries the same stable shape with nothing extra. A stack trace, SQL, connection detail, provider body, uncontracted internal identifier, or authorization reason that reveals resource existence gives an attacker the system's structure with every error.

**Reviewed for:** Each public error carries the HTTP API error contract shape.

### Protect Browser Boundaries

**ID:** `litespec/quality/security#protect-browser-boundaries`

**Statement:** A frontend MUST apply framework escaping, a documented Content Security Policy, secure headers, redirect validation, forgery protection, and sanitization of approved rich content.

**Rationale:** The browser is hostile ground: injected scripts, forged requests, malicious redirects, and hostile markup all arrive as ordinary page content. Framework escaping, a documented Content Security Policy, secure headers, redirect validation, forgery protection, and sanitization of approved rich content each blocks one of those attacks. A route guard or hidden control is presentation, so it never replaces API authorization.

**Reviewed for:** Each frontend ships framework escaping, a Content Security Policy, secure headers, redirect validation, forgery protection, and rich-content sanitization.

### Minimize Sensitive Data

**ID:** `litespec/quality/security#minimize-sensitive-data`

**Statement:** A use case MUST collect, return, log, export, and retain only the fields its input and results declare.

**Rationale:** Data the use case never collects cannot leak from it. Collecting, returning, logging, exporting, and retaining only the declared fields keeps each use case's exposure exactly what its page promises. A use case handling sensitive data states its classification, access, retention, deletion, and audit behavior on its page, so the promise is written where the fields are.

**Reviewed for:** Each use case collects, returns, logs, exports, and retains only its declared fields.

### Keep a Personal-Data Inventory per Store

**ID:** `litespec/quality/security#keep-a-personal-data-inventory-per-store`

**Statement:** A platform holding personal data MUST keep a personal-data inventory per store naming the fields, purposes, and retention of each store.

**Rationale:** Export, erasure, and breach answers each ask the same opening question: which stores hold this person's data, which fields, for which purposes, and for how long. An inventory the team can read answers it in one place. Without it, each request starts with a search across logs, indexes, and outboxes, and the store nobody remembers stays out of the answer.

**Reviewed for:** The inventory names the fields, purposes, and retention of each store holding personal data.

### Apply Erasure and Export to Every Store

**ID:** `litespec/quality/security#apply-erasure-and-export-to-every-store`

**Statement:** Erasure and export MUST reach every store holding personal data, with backups expiring inside the retention bound the retention schedule states.

**Rationale:** Personal data lives in logs, search indexes, and outboxes beside the database. A request honored in the database but left in a log export answers half the duty and breaks the promise to the person. A backup cannot answer field by field, so it carries a retention bound instead, and the data ages out with the backup.

**Reviewed for:** Erasure and export reach every store on the inventory, with backups expiring inside their bound.

### Keep Personal Data Out of Logs

**ID:** `litespec/quality/security#keep-personal-data-out-of-logs`

**Statement:** A log, trace, or diagnostic record MUST NOT carry personal data in clear.

**Rationale:** Logs flow to stores the product does not control: collectors, dashboards, long-term archives. Redaction at the sink fails once, in one pipeline, and the cleartext is already downstream. A field never emitted fails nowhere.

**Reviewed for:** Logs, traces, and diagnostic records carry no personal data in clear.

### Treat an Unsalted Digest as Personal Data

**ID:** `litespec/quality/security#treat-an-unsalted-digest-as-personal-data`

**Statement:** An unsalted digest of personal data MUST receive the same handling as the data itself.

**Rationale:** A digest without a salt reverses by table lookup: anyone holding a table of common values recovers the original. It hides nothing, so it gets none of the relaxed handling a truly one-way value earns. The same handling as the data itself keeps the digest from leaking what the data would.

**Reviewed for:** Each unsalted digest of personal data receives the handling its source data receives.

### Report a Breach Within Its Deadline

**ID:** `litespec/quality/security#report-a-breach-within-its-deadline`

**Statement:** A breach-notification procedure MUST name an owner and a backup person and meet the deadline the applicable constraint record states.

**Rationale:** A breach deadline starts running at discovery. The applicable constraint record states the deadline the outside rule sets. A deadline nobody owns slips past the team, and an owner without a backup fails on the first absence. Naming both before anything happens keeps the procedure starting on time.

**Reviewed for:** The breach procedure names an owner, a backup person, and the deadline the constraint record states.

### Protect Live Data at Rest

**ID:** `litespec/quality/security#protect-live-data-at-rest`

**Statement:** Live data at rest MUST carry encryption or a departure exception covering this provision.

**Rationale:** Live data at rest outlives the process: disks, snapshots, and replicas each hold a copy outside the application's control. Encryption keeps each copy unreadable without its key. Where encryption does not apply yet, the exception covering this provision keeps the gap visible with its reason, owner, and expiry date instead of silent.

**Reviewed for:** Each live store carries encryption, or an exception record covers this provision.

### Bound Abuse at Exposed Endpoints

**ID:** `litespec/quality/security#bound-abuse-at-exposed-endpoints`

**Statement:** A public, authentication, webhook, search, or upload endpoint MUST declare its limit as a setting record with key, owner, and rejection response.

**Rationale:** Public, authentication, webhook, search, and upload endpoints invite abuse: credential stuffing, scraping, flooding. Declaring the bound as a setting record names the key that isolates one caller, the owner who monitors the limit, and the rejection response the caller sees. The rejection is `429` with a stable code, so legitimate callers can back off and retry.

**Reviewed for:** Each public, authentication, webhook, search, and upload endpoint declares its setting with key, owner, and rejection response.

### Keep One Limit Store Across Replicas

**ID:** `litespec/quality/security#keep-one-limit-store-across-replicas`

**Statement:** A deployment running more than one replica MUST hold its rate and concurrency counters in one store that every replica reads.

**Rationale:** A per-process counter multiplies the declared limit by the replica count, so the limit the specification states is not the limit the system applies. The multiplier also changes whenever the deployment scales, which makes the effective limit unstated and unreviewable. One store every replica reads keeps the applied limit equal to the declared one.

**Reviewed for:** The deployment reads rate and concurrency counters from one store on every replica.

### Restrict Cross-Origin Access

**ID:** `litespec/quality/security#restrict-cross-origin-access`

**Statement:** A cross-origin policy MUST list its allowed origins, methods, headers, and credential mode without using a wildcard origin.

**Rationale:** A wildcard origin lets any site call the API with the user's credentials, which turns every malicious page into an API client. Listing allowed origins, methods, headers, and credential mode keeps each cross-origin caller explicit. Validated configuration owns the allowlist, so an unlisted origin cannot arrive by mistake. Deployment-owned origins are allowed when the allowlist shape is validated at startup.

**Reviewed for:** The cross-origin policy lists origins, methods, headers, and credential mode with no wildcard origin.

### Record Security Audit Events

**ID:** `litespec/quality/security#record-security-audit-events`

**Statement:** A security audit event MUST record actor, action, target, outcome, timestamp, trace identifier, and available reason.

**Rationale:** An incident review reconstructs who did what to which target, with what outcome and why. Without actor, action, target, outcome, timestamp, trace identifier, and reason on each security event, the reconstruction is guesswork. Audit records follow a documented retention and access policy, exclude secrets, and stay available for incident review. A consumer that activates the audit extension records the event through that extension instead, which states a wider field set, a classification rule, and a tamper-evidence rule.

**Reviewed for:** Each security audit event records actor, action, target, outcome, timestamp, trace identifier, and reason.

### Rotate Production Secrets

**ID:** `litespec/quality/security#rotate-production-secrets`

**Statement:** A production secret MUST have a secret-inventory entry naming its owner, storage location, and next rotation date, with revocation and recovery in a runbook.

**Rationale:** Every secret leaks eventually: through a departing teammate, a copied file, an old backup. A secret-inventory entry with owner, storage location, and next rotation date keeps each secret's lifetime bounded, and rotation allows overlap while clients switch. Procedures, logs, artifacts, and audit records carry no secret value, as `litespec/quality/security#keep-secrets-out-of-tracked-and-observable-data` states, and revocation and recovery live in a runbook.

**Reviewed for:** Each production secret carries a secret-inventory entry with owner, location, and rotation date beside its revocation runbook.

### Set an ASVS Level as the Security Target

**ID:** `litespec/quality/security#set-an-asvs-level-as-the-security-target`

**Statement:** A consumer MUST set an ASVS 5.0 level as its security target.

**Rationale:** ASVS 5.0 defines three levels, L1, L2, and L3, with each requirement verifiable to pass or fail, and the organization picks one by risk. The level lives on a quality requirement with its measure, as `litespec/core/intent#state-the-measure-on-a-counted-requirement` states, so the target is checked rather than declared. The level choice and any accepted finding are each decided once by a person.

**Reviewed for:** The security quality requirement names its ASVS level with a passing check.

### Scan Every Release with the ZAP Baseline

**ID:** `litespec/quality/security#scan-every-release-with-the-zap-baseline`

**Statement:** CI MUST scan the running API with the ZAP baseline on every pull request.

**Rationale:** The ZAP baseline runs the spider with passive scanning and no active attacks, so it is safe on every pull request, with a WARN, FAIL, and IGNORE config file. Its JSON report feeds the model with the other measured results, and the scan passes with no medium or high alert. ZAP is free and maintained under Apache-2.0.

**Reviewed for:** The ZAP result file under `TestResults/` shows no medium or high alert for the pull request.

### Scan Every Pull Request with a Free Static Analyzer

**ID:** `litespec/quality/security#scan-every-pull-request-with-a-free-static-analyzer`

**Statement:** CI MUST scan every pull request for security findings with a free static analyzer.

**Rationale:** CodeQL is free only on public repositories, where no paid plan is required; on a private repository the project uses a free alternative and records the choice in its CI documentation. An accepted finding lives as a gap exception covering the finding, with its reason, owner, and expiry date, so each acceptance stays visible and dated.

**Reviewed for:** The static analysis report for the pull request shows its findings with a gap exception for each accepted one.

## Conventions

### Use One Current Actor Abstraction

**ID:** `litespec/quality/security#use-one-current-actor-abstraction`

**Default:** Expose one narrow current-actor accessor in WebApi that maps verified claims to typed identity, roles, and scopes.

**Replacement:** A consumer records a current-actor convention naming its accessor and mapping.

**Rationale:** One narrow accessor in WebApi keeps claims mapping in one place: typed identity, roles, and scopes, mapped once. Application receives the actor through input that WebApi creates, or through a narrow trusted port when behavior must stay host-independent. A second accessor invites a second interpretation of the same claims.

**Reviewed for:** WebApi exposes one current-actor accessor, or the current-actor convention names another arrangement.

### Keep Secure Headers in Host Configuration

**ID:** `litespec/quality/security#keep-secure-headers-in-host-configuration`

**Default:** Define Content Security Policy, frame restrictions, content-type protection, referrer policy, and transport security in one host location.

**Replacement:** A consumer records a secure-headers convention naming where its headers live.

**Rationale:** Content Security Policy, frame restrictions, content-type protection, referrer policy, and transport security form one set: the browser reads them together. Spread across middleware registrations, one of them is always the header nobody remembers. One host location keeps the set reviewable as a whole.

**Reviewed for:** The secure headers sit in one host location, or the secure-headers convention names another arrangement.

### Use Deny-by-Default Policies

**ID:** `litespec/quality/security#use-deny-by-default-policies`

**Default:** Set a fallback policy requiring an authenticated user, and mark each public endpoint with `AllowAnonymous` intentionally.

**Replacement:** A consumer records an endpoint-protection convention naming how each endpoint is protected.

**Rationale:** A new endpoint nobody secured is a public endpoint by accident. A fallback policy requiring an authenticated user keeps each new endpoint protected until somebody marks it public on purpose with `AllowAnonymous`. Named role or scope policies add coarse checks while the guard retains resource authorization.

**Reviewed for:** New endpoints require authentication by default, or the endpoint-protection convention names another arrangement.

### Test the Resource Authorization Matrix

**ID:** `litespec/quality/security#test-the-resource-authorization-matrix`

**Default:** Cover anonymous, invalid-token, unrelated-actor, owner, and each privileged grant for every protected operation.

**Replacement:** A consumer records an authorization-matrix convention naming the caller cases each operation passes.

**Rationale:** Authorization fails one caller at a time: the anonymous caller who gets in, the unrelated actor who reads another tenant's row. Covering anonymous, invalid-token, unrelated-actor, owner, and each privileged grant for every protected operation proves each caller separately. The matrix also covers disclosure behavior and the collection query predicate. A UI redirect or hidden control is not evidence.

**Reviewed for:** Each protected operation passes every caller in the matrix, or the authorization-matrix convention names another set.

## Reference Example

This informative example demonstrates `litespec/quality/security#authorize-each-target-resource` and `litespec/quality/security#limit-public-error-detail`.

`GET /api/posts/{id}` for a private draft filters by both post ID and the author ID derived from claims. A different author receives the documented not-found or forbidden response without learning protected fields.
