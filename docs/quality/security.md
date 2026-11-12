# Security

**Stack:** any
**Concern:** application

## Intent

Security answers 3 questions for every operation: who acts, what it touches, and what it keeps. This page states how a LiteSpec product answers them through authentication, actor resolution, authorization, trust boundaries, secrets, browser defenses, personal data, abuse limits, audit events, and release security checks, for anyone who builds or reviews an operation, an endpoint, a frontend, or a release. A product that follows it proves every operation runs for the right actor, touches only what it may, and keeps sensitive data safe.

## Concepts

### Actors, Roles, and Permissions

Every protected operation answers one question: who may do what to which target. An actor is whoever acts: a person, a company, a provider, a system, or a guest. An actor holds a role at a scope, such as one organization; a role allows permissions from the shared closed list; and each use case declares the permissions it needs. The application guard authorizes every protected operation from those declarations, so two operations about the same record ask the same question.

### Trust Boundaries

A trust boundary is any point where data crosses from one party to another: a request arriving at the API, a file upload, a provider response, database content rendered into a new output, or generated text shown to a user. Each boundary validates what crosses it before use, because data that was safe in its old context may be unsafe in the new one. Application validators then check message shape, while Domain checks business invariants.

### Personal Data and Its Stores

Personal data spreads further than the database: logs, search indexes, outboxes, snapshots, and backups each hold their own copy. Each store that holds personal data keeps its own inventory entry naming the fields, the purposes, and the retention, so every export, erasure, and breach answer starts from the same map.

## Standards

### Keep Backend Authentication Provider-Neutral

**ID:** `litespec/quality/security#keep-backend-authentication-provider-neutral`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A backend MUST validate issuer, audience, signature, lifetime, and required claims through OIDC and JWT Bearer.

**Rationale:** The backend accepts one thing: a token whose issuer, audience, signature, lifetime, and required claims validate through OIDC and JWT Bearer. Provider-specific login and session behavior belongs to a frontend or an extension, because the API cannot recheck a login flow it cannot see. An unsigned token, an unvalidated decode, or a client-supplied identity header is never accepted: each one lets a caller mint its own identity.

**Reviewed for:** Does the backend authentication setup validate issuer, audience, signature, lifetime, and required claims, with no identity header accepted? Look at the authentication configuration in WebApi.

### Derive the Actor from Claims

**ID:** `litespec/quality/security#derive-the-actor-from-claims`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler, endpoint

**Statement:** Actor resolution MUST follow `litespec/quality/security#use-one-current-actor-abstraction`.

**Rationale:** Actor resolution has one home: the WebApi boundary, which maps verified claims to identity, roles, and scopes through the one current-actor accessor. Request data never supplies the current actor, because anything the caller sends can name anyone. One resolution path keeps every operation answering "who acts" from the same verified source.

**Reviewed for:** Does WebApi map verified claims through the one current-actor accessor, with no request data supplying the actor? Look at the actor resolution in WebApi.

### Authorize Each Target Resource

**ID:** `litespec/quality/security#authorize-each-target-resource`

**Strength:** required

**Applies to:** artifact: use-case, command-handler, query-handler

**Statement:** The application guard MUST authorize every protected operation from its use case's declared permissions, as `litespec/backend/application#enforce-target-authorization-in-the-pipeline` states, with endpoints adding only coarse policies.

**Rationale:** One decision from one declaration stops two operations about the same record from asking different questions. The use case declares the permissions it needs, the guard resolves the actor's roles at the target's scope, and the decision follows. A message whose target must be the caller carries no target (`litespec/core/access#remove-a-target-that-must-be-the-caller`). A handler never compares the caller with a target and never throws the forbidden refusal itself; the guard decides. Endpoints add only coarse policies, because a policy that cannot see business data cannot decide about an aggregate.

**Reviewed for:** Does each protected operation pass the application guard from its use case's declared permissions? Look at the guard decision for each protected operation in the authorization tests and the pipeline.

### Restrict a Protected Collection in the Database

**ID:** `litespec/quality/security#restrict-a-protected-collection-in-the-database`

**Strength:** required

**Applies to:** artifact: query-handler

**Statement:** A protected collection query MUST apply its authorization scope as a database predicate, before paging, rather than filter a materialized result.

**Rationale:** A collection has no single target, so the check that `litespec/quality/security#authorize-each-target-resource` states has nothing to resolve against: the scope must become part of the database query itself. Reading the unrestricted set already exposes the rows to the process, and a page assembled from them leaks the total count even when every row is dropped. Filtering after the load is the shape [OWASP lists as broken object level authorization](https://owasp.org/www-project-top-ten/2021/A01_2021-Broken_Access_Control/).

**Example:** A query for an organization's orders adds the organization predicate to the database query. It does not load every order and drop the ones the caller may not read.

**Reviewed for:** Does each protected collection query apply its authorization scope as a database predicate before paging? Look at each query handler that returns a collection.

### Validate at Trust Boundaries

**ID:** `litespec/quality/security#validate-at-trust-boundaries`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** A trust boundary MUST validate transport shape and file metadata before use, leaving message checks to application validators as `litespec/backend/application#separate-input-validation-from-invariants` states.

**Rationale:** Message shape, required values, length, range, format, and pagination bounds stay with the application validators that `litespec/backend/application#separate-input-validation-from-invariants` names, and business invariants stay with Domain. Each layer then answers only the questions it can see. Database content, provider responses, file contents, and generated text stay untrusted when they cross into a new output context, because validation for one context proves nothing about the next.

**Reviewed for:** Does each trust boundary validate transport shape and file metadata before use? Look at the endpoints, the upload handlers, and the provider response readers.

### Keep Secrets Out of Tracked and Observable Data

**ID:** `litespec/quality/security#keep-secrets-out-of-tracked-and-observable-data`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A secret MUST NOT appear in source, logs, traces, exception messages, generated files, URLs, snapshots, container layers, or browser storage.

**Rationale:** Every place a secret lands is a place it leaks from: source reaches every clone, logs and traces reach every dashboard reader, URLs reach every proxy log, snapshots and container layers outlive the process, and browser storage reaches the user. Platform secret stores hold the values instead, and diagnostics redact sensitive configuration before it is emitted, so the value never enters the observable stream.

**Reviewed for:** Does any source, log, trace, exception message, generated file, URL, snapshot, container layer, or browser storage hold a secret? Search the tree and a sample of log and trace output for key and token values.

### Parameterize Database Input

**ID:** `litespec/quality/security#parameterize-database-input`

**Strength:** required

**Applies to:** artifact: repository

**Statement:** An external value reaching the database MUST travel through a provider query API or a parameterized SQL parameter.

**Rationale:** String concatenation or interpolation into raw command text makes the value part of the statement, so a crafted value rewrites the query. A provider query API or a parameterized SQL parameter keeps the value as data. The provider query API is Marten's under the baseline, and it parameterizes. The reporting extension owns the reviewed raw SQL patterns.

**Reviewed for:** Does each external value reach the database through a query API call or a SQL parameter? Look at the repository code and each raw SQL file.

### Limit Public Error Detail

**ID:** `litespec/quality/security#limit-public-error-detail`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** A public error MUST use the error contract `litespec/backend/api#return-stable-problem-details` defines.

**Rationale:** The error contract has one home: the HTTP API rules. Every public error then carries the same stable shape with nothing extra. A stack trace, SQL, connection detail, provider body, uncontracted internal identifier, or authorization reason that reveals resource existence gives an attacker the system's structure with every error.

**Reviewed for:** Does each public error carry the HTTP API error contract shape, with no stack trace, SQL, or provider body? Look at the error responses the endpoint tests assert.

### Protect Browser Boundaries

**ID:** `litespec/quality/security#protect-browser-boundaries`

**Strength:** required

**Applies to:** artifact: component, route, form

**Statement:** A frontend MUST apply framework escaping, a documented Content Security Policy, secure headers, redirect validation, forgery protection, and sanitization of allowed rich content.

**Rationale:** The browser is hostile ground: injected scripts, forged requests, malicious redirects, and hostile markup all arrive as ordinary page content. Framework escaping, a documented Content Security Policy, secure headers, redirect validation, forgery protection, and sanitization of allowed rich content each blocks one of those attacks. A route guard or hidden control is presentation, so it never replaces API authorization.

**Reviewed for:** Does each frontend ship framework escaping, a Content Security Policy, secure headers, redirect validation, forgery protection, and rich-content sanitization? Look at the frontend host configuration and the response headers it sends.

### Minimize Sensitive Data

**ID:** `litespec/quality/security#minimize-sensitive-data`

**Strength:** required

**Applies to:** artifact: use-case, command-handler, query-handler

**Statement:** A use case MUST collect, return, log, export, and retain only the fields its input and results declare.

**Rationale:** Data the use case never collects cannot leak from it. Collecting, returning, logging, exporting, and retaining only the declared fields keeps each use case's exposure exactly what its page promises. A use case handling sensitive data states its classification, access, retention, deletion, and audit behavior on its page, so the promise is written where the fields are.

**Reviewed for:** Does each use case collect, return, log, export, and retain only the fields its input and results declare? Look at each use case's input and result types and its log calls.

### Keep a Personal-Data Inventory per Store

**ID:** `litespec/quality/security#keep-a-personal-data-inventory-per-store`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A platform holding personal data MUST keep a personal-data inventory per store naming the fields, purposes, and retention of each store.

**Rationale:** Export, erasure, and breach answers each ask the same opening question: which stores hold this person's data, which fields, for which purposes, and for how long. An inventory the team can read answers it in one place. Without it, each request starts with a search across logs, indexes, and outboxes, and the store nobody remembers stays out of the answer.

**Reviewed for:** Does the inventory name the fields, purposes, and retention of each store that holds personal data? Look at the inventory document beside the stores it lists.

### Apply Erasure and Export to Every Store

**ID:** `litespec/quality/security#apply-erasure-and-export-to-every-store`

**Strength:** required

**Applies to:** artifact: use-case; when: erasure

**Statement:** Erasure and export MUST reach every store holding personal data, with backups expiring inside the retention bound the retention schedule states.

**Rationale:** Personal data lives in logs, search indexes, and outboxes beside the database. A request honored in the database but left in a log export answers half the duty and breaks the promise to the person. A backup cannot answer field by field, so it carries a retention bound instead, and the data ages out with the backup.

**Reviewed for:** Do erasure and export reach every store on the inventory, with backups expiring inside their retention bound? Look at the erasure and export jobs and the backup retention setting.

### Keep Personal Data Out of Logs

**ID:** `litespec/quality/security#keep-personal-data-out-of-logs`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A log, trace, or diagnostic record MUST NOT carry personal data in clear.

**Rationale:** Logs flow to stores the product does not control: collectors, dashboards, long-term archives. Redaction at the sink fails once, in one pipeline, and the cleartext is already downstream. A field never emitted fails nowhere.

**Reviewed for:** Do logs, traces, and diagnostic records carry personal data in clear? Search the log sinks and a sample of recent log output for personal fields.

### Treat an Unsalted Digest as Personal Data

**ID:** `litespec/quality/security#treat-an-unsalted-digest-as-personal-data`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An unsalted digest of personal data MUST receive the same handling as the data itself.

**Rationale:** A digest without a salt reverses by table lookup: anyone holding a table of common values recovers the original. It hides nothing, so it gets none of the relaxed handling a truly one-way value earns. The same handling as the data itself keeps the digest from leaking what the data would.

**Reviewed for:** Does each unsalted digest of personal data receive the handling its source data receives? Look at each hashing call on a personal field and the storage class of its result.

### Report a Breach Within Its Deadline

**ID:** `litespec/quality/security#report-a-breach-within-its-deadline`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A breach-notification procedure MUST name the person who acts and a backup person and meet the deadline the applicable constraint record states.

**Rationale:** A breach deadline starts running at discovery. The applicable constraint record states the deadline the outside rule sets. A deadline with nobody named to meet it slips past the team, and one named person fails on the first absence. Naming both before anything happens keeps the procedure starting on time.

**Reviewed for:** Does the breach procedure name the person who acts, a backup person, and the deadline the constraint record states? Look at the breach runbook and the constraint record it cites.

### Protect Live Data at Rest

**ID:** `litespec/quality/security#protect-live-data-at-rest`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Live data at rest MUST carry encryption or a departure covering this provision.

**Rationale:** Live data at rest outlives the process: disks, snapshots, and replicas each hold a copy outside the application's control. Encryption keeps each copy unreadable without its key. Where encryption does not apply yet, the departure covering this provision keeps the gap visible with its reason, exit, and expiry date instead of silent.

**Reviewed for:** Does each live store carry encryption, or does a departure cover this provision? Look at each store's encryption setting and the departure records.

### Bound Abuse at Exposed Endpoints

**ID:** `litespec/quality/security#bound-abuse-at-exposed-endpoints`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** A public, authentication, webhook, search, or upload endpoint MUST declare its limit as a setting record with key and rejection response.

**Rationale:** Public, authentication, webhook, search, and upload endpoints invite abuse: credential stuffing, scraping, flooding. Declaring the bound as a setting record names the key that isolates one caller and the rejection response the caller sees. The rejection is `429` with a stable code, so legitimate callers can back off and retry.

**Reviewed for:** Does each public, authentication, webhook, search, and upload endpoint declare its setting with key and rejection response? Look at the setting records beside each endpoint.

### Keep One Limit Store Across Replicas

**ID:** `litespec/quality/security#keep-one-limit-store-across-replicas`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A deployment running more than one replica MUST hold its rate and concurrency counters in one store that every replica reads.

**Rationale:** A per-process counter multiplies the declared limit by the replica count, so the limit the specification states is not the limit the system applies. The multiplier also changes whenever the deployment scales, which makes the effective limit unstated and unreviewable. One store every replica reads keeps the applied limit equal to the declared one.

**Reviewed for:** Do all replicas read their rate and concurrency counters from one store? Look at the counter configuration of each deployment that runs more than one replica.

### Restrict Cross-Origin Access

**ID:** `litespec/quality/security#restrict-cross-origin-access`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A cross-origin policy MUST list its allowed origins, methods, headers, and credential mode without using a wildcard origin.

**Rationale:** A wildcard origin lets any site call the API with the user's credentials, which turns every malicious page into an API client. Listing allowed origins, methods, headers, and credential mode keeps each cross-origin caller explicit. Validated configuration owns the allowlist, so an unlisted origin cannot arrive by mistake. Deployment-owned origins are allowed when the allowlist shape is validated at startup.

**Reviewed for:** Does the cross-origin policy list its origins, methods, headers, and credential mode, with no wildcard origin? Look at the cross-origin configuration in each host.

### Record Security Audit Events

**ID:** `litespec/quality/security#record-security-audit-events`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A security audit event MUST record actor, action, target, outcome, timestamp, trace identifier, and available reason.

**Rationale:** An incident review reconstructs who did what to which target, with what outcome and why. Without actor, action, target, outcome, timestamp, trace identifier, and reason on each security event, the reconstruction is guesswork. Audit records follow a documented retention and access policy, exclude secrets, and stay available for incident review. A consumer that activates the audit extension records the event through that extension instead, which states a wider field set, a classification rule, and a tamper-evidence rule.

**Reviewed for:** Does each security audit event record actor, action, target, outcome, timestamp, trace identifier, and reason? Look at the audit writes and a sample of audit records.

### Rotate Production Secrets

**ID:** `litespec/quality/security#rotate-production-secrets`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A production secret MUST have a secret-inventory entry naming its storage location and next rotation date, with revocation and recovery in a runbook.

**Rationale:** Every secret leaks eventually: through a departing teammate, a copied file, an old backup. A secret-inventory entry with storage location and next rotation date keeps each secret's lifetime bounded, and rotation allows overlap while clients switch. Procedures, logs, artifacts, and audit records carry no secret value, as `litespec/quality/security#keep-secrets-out-of-tracked-and-observable-data` states, and revocation and recovery live in a runbook.

**Reviewed for:** Does each production secret carry an inventory entry with its location and next rotation date, beside its revocation runbook? Look at `.litespec/implementation/operations/secrets.json` and the runbooks.

### Set an ASVS Level as the Security Target

**ID:** `litespec/quality/security#set-an-asvs-level-as-the-security-target`

**Strength:** required

**Applies to:** artifact: record:requirement; layer: intent

**Statement:** A consumer MUST set an ASVS 5.0 level as its security target.

**Rationale:** ASVS 5.0 defines three levels, L1, L2, and L3, with each requirement verifiable to pass or fail, and the organization picks one by risk. The level lives on a quality requirement with its measure, as `litespec/core/intent#state-the-measure-on-a-counted-requirement` states, so the target is checked rather than declared. The level choice and any accepted finding are each decided once by a person.

**Reviewed for:** Does the security quality requirement name its ASVS 5.0 level, with a passing measure? Look at the requirement record and its measure.

### Scan Every Release with the ZAP Baseline

**ID:** `litespec/quality/security#scan-every-release-with-the-zap-baseline`

**Strength:** required

**Applies to:** artifact: any

**Statement:** CI MUST scan the running API with the ZAP baseline on every pull request.

**Rationale:** The ZAP baseline runs the spider with passive scanning and no active attacks, so it is safe on every pull request, with a WARN, FAIL, and IGNORE config file. Its JSON report feeds the model with the other measured results, and the scan passes with no medium or high alert. ZAP is free and maintained under Apache-2.0.

**Reviewed for:** Does the ZAP result file under `TestResults/` show no medium or high alert for the pull request? Look at the ZAP report.

### Scan Every Pull Request with a Free Static Analyzer

**ID:** `litespec/quality/security#scan-every-pull-request-with-a-free-static-analyzer`

**Strength:** required

**Applies to:** artifact: any

**Statement:** CI MUST scan every pull request for security findings with a free static analyzer.

**Rationale:** CodeQL is free only on public repositories, where no paid plan is required; on a private repository the project uses a free alternative and records the choice in its CI documentation. An accepted finding lives as a departure covering the finding, with its reason, exit, and expiry date, so each acceptance stays visible and dated.

**Reviewed for:** Does the static analysis report for the pull request show a departure for each accepted finding? Look at the analyzer report and the departure records.

## Conventions

### Use One Current Actor Abstraction

**ID:** `litespec/quality/security#use-one-current-actor-abstraction`

**Strength:** default

**Applies to:** artifact: any

**Default:** Expose one narrow current-actor accessor in WebApi that maps verified claims to typed identity, roles, and scopes.

**Replacement:** A consumer records a current-actor convention naming its accessor and mapping.

**Rationale:** One narrow accessor in WebApi keeps claims mapping in one place: typed identity, roles, and scopes, mapped once. Application receives the actor through input that WebApi creates, or through a narrow trusted port when behavior must stay host-independent. A second accessor invites a second interpretation of the same claims.

**Reviewed for:** Does WebApi expose one current-actor accessor, or does the convention name another arrangement? Look at WebApi's identity code and the convention.

### Keep Secure Headers in Host Configuration

**ID:** `litespec/quality/security#keep-secure-headers-in-host-configuration`

**Strength:** default

**Applies to:** artifact: any

**Default:** Define Content Security Policy, frame restrictions, content-type protection, referrer policy, and transport security in one host location.

**Replacement:** A consumer records a secure-headers convention naming where its headers live.

**Rationale:** Content Security Policy, frame restrictions, content-type protection, referrer policy, and transport security form one set: the browser reads them together. Spread across middleware registrations, one of them is always the header nobody remembers. One host location keeps the set reviewable as a whole.

**Reviewed for:** Do the secure headers sit in one host location, or does the convention name another arrangement? Look at the host configuration.

### Use Deny-by-Default Policies

**ID:** `litespec/quality/security#use-deny-by-default-policies`

**Strength:** default

**Applies to:** artifact: endpoint; layer: behavior

**Default:** Set a fallback policy requiring an authenticated user, and mark each public endpoint with `AllowAnonymous` intentionally.

**Replacement:** A consumer records an endpoint-protection convention naming how each endpoint is protected.

**Rationale:** A new endpoint nobody secured is a public endpoint by accident. A fallback policy requiring an authenticated user keeps each new endpoint protected until somebody marks it public on purpose with `AllowAnonymous`. Named role or scope policies add coarse checks while the guard retains resource authorization.

**Reviewed for:** Does each new endpoint require authentication unless it carries `AllowAnonymous` on purpose? Look at the fallback policy and each endpoint marked `AllowAnonymous`.

### Test the Resource Authorization Matrix

**ID:** `litespec/quality/security#test-the-resource-authorization-matrix`

**Strength:** default

**Applies to:** artifact: test

**Default:** Cover anonymous, invalid-token, unrelated-actor, owner, and each privileged grant for every protected operation.

**Replacement:** A consumer records an authorization-matrix convention naming the caller cases each operation passes.

**Rationale:** Authorization fails one caller at a time: the anonymous caller who gets in, the unrelated actor who reads another tenant's row. Covering anonymous, invalid-token, unrelated-actor, owner, and each privileged grant for every protected operation proves each caller separately. The matrix also covers disclosure behavior and the collection query predicate. A UI redirect or hidden control is not evidence.

**Reviewed for:** Does each protected operation pass every caller in the matrix, or does the convention name another set? Look at the authorization tests beside each protected operation.

## Reference Example

This informative example demonstrates `litespec/quality/security#authorize-each-target-resource` and `litespec/quality/security#limit-public-error-detail`.

`GET /api/posts/{id}` for a private draft filters by both post ID and the author ID derived from claims. A different author receives the documented not-found or forbidden response without learning protected fields.
