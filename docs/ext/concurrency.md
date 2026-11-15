# Concurrency and Idempotency

**Stack:** any

**Concern:** application

Concurrency and idempotency rules stop overlapping writes from silently overwriting each other and stop a retried request from executing twice. This page states how a project documents conflicting writes, coordinates them with versions, and makes retried operations return one result through client keys and fingerprints. A team applies it when overlap or retry can corrupt behavior, so its callers get one predictable outcome instead of a silent overwrite or a duplicated effect.

## Activation

Activation scope: `local`.

Applicable specification kinds: `module`, `aggregate`, `use-case`.

Enable `concurrency` when accepted writes can conflict with each other, or when a retry can duplicate an irreversible or externally visible effect.

## Baseline Relationship

This extension replaces no baseline provision. `litespec/ext/concurrency#return-precondition-outcomes` adds the 428 outcome for a missing precondition to the outcome table that `litespec/backend/api#use-consistent-status-codes` owns.

It switches on `setting`: key retention is a setting record under `litespec/ext/concurrency#set-key-retention`.

Its client-key standards apply to operations whose `idempotency.mode` is `key`. Every stored aggregate already carries a version the store compares at commit, under `litespec/backend/persistence#version-every-aggregate`. The version standards on this page govern what the caller sees for a conflict.

## Standards

### Document Write Conflict Behavior

**ID:** `litespec/ext/concurrency#document-write-conflict-behavior`

**Strength:** required

**Applies to:** artifact: record:use-case; layer: behavior

**Statement:** A command that can conflict with another accepted write MUST state `conflicts` naming what conflicts in `on`, which write wins in `winner`, and the refusal code the loser gets in `refusal`.

**Rationale:** Version coordination exists to protect one invariant. The `on` entry names the conflicting operations and the invalid combined result they would leave, so an implementation tells a conflict it rejects from contention it absorbs. The `winner` entry names which write wins: `first-write` keeps the accepted change and refuses the late write, `last-write` lets the late write replace the accepted change, and `merge` combines both writes. The `refusal` entry names the refusal code the loser gets from the module's refusal set, so the caller learns the loss as a declared refusal with its stable code.

**Example:** Claimwick's approve-expense command states `conflicts` with `on` naming a second approval of the same expense, `winner: first-write`, and `refusal` naming the approve conflict code the late approval gets.

**Checked by:** `litespec model check` reports `record-invalid` for a `conflicts` entry that names no `on`, no `winner`, or no `refusal`, or a `winner` outside `first-write`, `last-write`, and `merge`.

### Require Caller Versions Only for Declared Conflicts

**ID:** `litespec/ext/concurrency#require-caller-versions-only-for-declared-conflicts`

**Strength:** required

**Applies to:** artifact: command-handler, endpoint

**Statement:** An implementation MUST NOT require a caller-supplied version without a `conflicts` entry on its command.

**Rationale:** Every stored aggregate already carries a baseline version that the store compares at commit, so the version itself needs no record. Requiring the caller to supply a version adds a rejection path the caller has to handle: a missing or stale caller version refuses an otherwise valid write. The `conflicts` entry stays the gate for that caller burden: no entry, no caller-version requirement.

**Reviewed for:** Does every caller-version requirement have a `conflicts` entry on its command? Check the versioned operations beside the use case records.

### Compare Caller and Stored Versions in One Transaction

**ID:** `litespec/ext/concurrency#compare-caller-and-stored-versions-in-one-transaction`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** An implementation MUST compare the caller-supplied version with the stored version in the same transaction as the change.

**Rationale:** The caller-supplied version marks which write the caller saw, and the stored version marks what the database holds. Comparing the two in the same transaction as the change detects a competing accepted write against the same state, through the selected persistence model. A comparison outside that transaction races the commit it claims to guard.

**Reviewed for:** Does every caller-version comparison run inside the transaction carrying its change? Check the conflicting-write paths.

### Return 409 for Failed Version Checks

**ID:** `litespec/ext/concurrency#return-409-for-failed-version-checks`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** An HTTP API MUST map a failed expected version to status 409 Problem Details with a stable code.

**Rationale:** A caller that loses a version race retries by reading again, applying again, and sending again. Status 409 with a stable Problem Details code names the loss as a conflict, distinct from validation and authorization failures. Without that stable code, the caller guesses which failure it met.

**Reviewed for:** Does every failed expected version return 409 Problem Details with its stable code? Check the conflict responses.

### Reject Silent Overwrites

**ID:** `litespec/ext/concurrency#reject-silent-overwrites`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** An implementation MUST NOT silently overwrite a conflicting accepted change.

**Rationale:** A conflicting write that silently replaces the accepted change destroys work another caller committed, and the losing caller never learns its change is gone. Rejecting the conflict instead hands the caller the facts it needs to reconcile.

**Reviewed for:** Does any conflicting write silently replace the accepted change? Check the conflict outcomes.

### Apply HTTP Version Preconditions

**ID:** `litespec/ext/concurrency#apply-http-version-preconditions`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** An HTTP operation requiring a caller version MUST return a strong persistence-derived `ETag` and require `If-Match` on writes.

**Rationale:** The caller reads the current state with its strong persistence-derived `ETag` and sends it back in `If-Match` on the write. The server compares what the caller saw with what is stored. The round trip carries the version through HTTP headers instead of a second contract field.

**Example:** A current `ETag` permits the write; a changed entity version rejects it.

**Reviewed for:** Does every versioned HTTP operation return a strong persistence-derived `ETag` and require `If-Match` on writes? Check the versioned endpoints.

### Return Precondition Outcomes

**ID:** `litespec/ext/concurrency#return-precondition-outcomes`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** An HTTP API MUST return 428 `precondition_required` for missing `If-Match` and 409 `version_conflict` for stale versions.

**Rationale:** A missing `If-Match` and a stale version are different caller mistakes with different fixes: send the version, or read again and retry. Status 428 `precondition_required` names the omission and 409 `version_conflict` names the stale version. One code for both would leave the caller guessing which fix applies.

**Reviewed for:** Does every versioned operation return 428 `precondition_required` for a missing `If-Match` and 409 `version_conflict` for a stale version? Check the precondition responses.

### Hide Provider Version Values

**ID:** `litespec/ext/concurrency#hide-provider-version-values`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** An HTTP API MUST NOT expose a provider-specific version value in another contract field.

**Rationale:** The `ETag` is the one contract field that carries the persistence version. A provider-specific version leaked into another field gives callers a second version to depend on. Hiding it keeps the coordination behind one boundary the project can change.

**Reviewed for:** Does any transport model expose a provider-specific version value outside the `ETag`? Check the public transport models.

### Run Idempotency in the Pipeline

**ID:** `litespec/ext/concurrency#run-idempotency-in-the-pipeline`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** Idempotency MUST run as a pipeline stage after authorization and validation, before the handler.

**Rationale:** Authorization and validation run first, so a denied or invalid attempt neither consumes a client key nor replays a stored result. The handler runs after, so a replayed key returns the stored response without running the handler again. Any other order either spends keys on attempts the pipeline would refuse, or runs work the store already accepted.

**Reviewed for:** Does every idempotent operation run idempotency in a pipeline stage after authorization and validation, before the handler? Check the pipeline stage order.

### Scope Client Keys

**ID:** `litespec/ext/concurrency#scope-client-keys`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** An idempotent operation MUST scope its client key to the tenant, authenticated actor, and operation.

**Rationale:** A client key alone proves nothing about who sent it: two callers can pick the same key. Scoping the key to tenant, authenticated actor, and operation stops one caller's key from replaying another caller's operation. A project without the tenancy extension uses one fixed tenant value.

**Reviewed for:** Does every idempotent operation scope its client key to tenant, authenticated actor, and operation? Check the keyed operations.

### Store Idempotency State Atomically

**ID:** `litespec/ext/concurrency#store-idempotency-state-atomically`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** An idempotent operation MUST store its fingerprint, state, response status, response body, and expiry with its business change in one provider transaction.

**Rationale:** The fingerprint, state, response status, response body, and expiry describe the accepted change, so they commit with it in one provider transaction. One transaction leaves neither a completed record without its change, nor a change without its replay record. A retry then replays exactly what the first attempt accepted.

**Reviewed for:** Does every idempotent operation store its fingerprint, state, response, and expiry with its business change in one transaction? Check the keyed write paths.

### Reject Conflicting Key Reuse

**ID:** `litespec/ext/concurrency#reject-conflicting-key-reuse`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** An idempotent operation MUST reject reuse of one key with a different request fingerprint.

**Rationale:** A key reused with a different request fingerprint names two different intents under one key. Accepting both would replay the first intent's result for the second intent's request. Rejecting the reuse keeps one key bound to one intent.

**Reviewed for:** Does every reuse of one key with a different fingerprint meet the documented rejection? Check the key-reuse paths.

### Enforce Key Uniqueness

**ID:** `litespec/ext/concurrency#enforce-key-uniqueness`

**Strength:** required

**Applies to:** artifact: repository

**Statement:** An idempotency store MUST enforce a unique constraint on tenant, actor, operation, and client key.

**Rationale:** Two concurrent requests can arrive with the same key before either commits. The unique constraint on tenant, actor, operation, and client key admits one winner and rejects the other at the store. Application code alone cannot order two requests that arrive together. A project without the tenancy extension uses one fixed tenant value.

**Reviewed for:** Does every idempotency store enforce the unique constraint on tenant, actor, operation, and client key? Check the store schema.

### Build Fingerprints from Canonical Request Data

**ID:** `litespec/ext/concurrency#build-fingerprints-from-canonical-request-data`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** An idempotency implementation MUST derive its fingerprint from canonical mapped command input, route identity, and the authenticated actor identifier, excluding credentials and the client key.

**Rationale:** The fingerprint decides whether two requests under one key carry the same intent. Canonical mapped command input, route identity, and the authenticated actor identifier make equivalent requests produce one value. The actor identifier is in the fingerprint because two actors can present the same client key, and a fingerprint without it would replay one actor's stored response to another. The credential that proved the actor stays out, since a cookie and a signature each change between two requests this rule treats as equal.

**Reviewed for:** Does every fingerprint derive from canonical input, route identity, and actor identifier, with no credentials and no client key? Check the fingerprint derivation.

### Hash Sensitive Fingerprint Data

**ID:** `litespec/ext/concurrency#hash-sensitive-fingerprint-data`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** An idempotency implementation MUST store a cryptographic hash when support does not require original sensitive request content.

**Rationale:** The fingerprint has to compare requests, not to show what they contained. A cryptographic hash preserves the comparison while the original sensitive content stays out of the store. Support keeps original content only where it needs the original to help.

**Reviewed for:** Does any idempotency store keep original sensitive content that support never needs? Check the fingerprint store.

### Replay Completed Results

**ID:** `litespec/ext/concurrency#replay-completed-results`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** A completed idempotent retry MUST return the original response status and body.

**Rationale:** The first attempt performed the state change, and the retry performs it no second time. Returning the original response status and body gives the retry the accepted result. The caller sees one execution no matter how many times the network retried.

**Reviewed for:** Does every completed idempotent retry return the original response status and body? Check the replay paths.

### Define Concurrent Retry Behavior

**ID:** `litespec/ext/concurrency#define-concurrent-retry-behavior`

**Strength:** required

**Applies to:** artifact: record:use-case; layer: behavior

**Statement:** An idempotent use case MUST define whether a concurrent request waits, reports in-progress, or returns a stable conflict.

**Rationale:** An identical request can arrive while the first is still running. The use case states whether that concurrent request waits, learns the work is in progress, or receives a stable conflict. Without one documented result, each implementation invents its own and callers handle all of them.

**Reviewed for:** Does every idempotent use case define whether a concurrent request waits, reports in-progress, or returns a stable conflict? Check the idempotent use case pages.

### Replay the Winner for Losing Concurrent Requests

**ID:** `litespec/ext/concurrency#replay-the-winner-for-losing-concurrent-requests`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** A losing concurrent idempotency request MUST reload and replay the winning record when its fingerprint matches.

**Rationale:** The unique constraint admits one winner among concurrent same-key requests. A losing request whose fingerprint matches reloads the winning record and replays its result. The loser then returns the accepted outcome instead of an error for work that succeeded.

**Reviewed for:** Does every losing concurrent request with a matching fingerprint reload and replay the winning record? Check the concurrent retry paths.

### Discard Failed Replay State

**ID:** `litespec/ext/concurrency#discard-failed-replay-state`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** A failed pre-commit idempotent request MUST leave neither the business change nor a completed replay record.

**Rationale:** A request that fails before commit changed nothing, so it leaves neither the business change nor a completed replay record. A leftover completed record would replay success for work that never happened. Clean state lets a later retry perform the operation for the first time.

**Reviewed for:** Does every failed pre-commit request leave neither a business change nor a completed replay record? Check the failure paths.

### Delay Irreversible Provider Calls

**ID:** `litespec/ext/concurrency#delay-irreversible-provider-calls`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** An idempotent operation MUST NOT perform an irreversible provider call before its business transaction commits.

**Rationale:** A provider call that cannot be undone follows the commit. A call made before commit fires even when the transaction then rolls back. The business change would vanish while its external effect stands.

**Reviewed for:** Does any irreversible provider call run before its business transaction commits? Check the idempotent operation paths.

### Route Required External Effects Durably

**ID:** `litespec/ext/concurrency#route-required-external-effects-durably`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** An idempotent operation MUST use the outbox extension or a documented provider idempotency contract for required external effects.

**Rationale:** A required external effect has to survive retries without duplicating. The outbox extension delivers it durably after commit, or a documented provider idempotency contract lets the provider absorb the replay. An effect with neither path fires once per retry.

**Reviewed for:** Does every required external effect use the outbox or a documented provider idempotency contract? Check the idempotent operation paths.

### Set Key Retention

**ID:** `litespec/ext/concurrency#set-key-retention`

**Strength:** required

**Applies to:** artifact: record:setting; layer: behavior

**Statement:** An idempotency owner MUST record key retention as a setting record set from the caller retry window and business risk.

**Rationale:** A key that expires too early turns a legitimate retry into duplicate business work. Retention therefore spans the caller retry window plus the business risk of a late duplicate. The setting record states that span where reviewers and operators read it.

**Reviewed for:** Does every key retention cover the caller retry window and the business risk? Check the setting record beside the idempotent use case.

### Remove Expired Keys Within Fixed Bounds

**ID:** `litespec/ext/concurrency#remove-expired-keys-within-fixed-bounds`

**Strength:** required

**Applies to:** artifact: job

**Statement:** An idempotency owner MUST remove expired records through a bounded maintenance process.

**Rationale:** Expired keys accumulate without cleanup, and cleanup without bounds steals capacity from live work. A bounded maintenance process removes expired records within fixed limits on rate and batch. The store stays small without starving the application.

**Reviewed for:** Does every expired-record cleanup run as a bounded maintenance process? Check the cleanup jobs.

### Keep Keys Until Callers Stop Retrying

**ID:** `litespec/ext/concurrency#keep-keys-until-callers-stop-retrying`

**Strength:** required

**Applies to:** artifact: record:setting; layer: behavior

**Statement:** An idempotency owner MUST NOT expire a key before callers can safely retry.

**Rationale:** Callers retry inside a documented window after an uncertain response. A key that expires inside that window is gone when the retry arrives, so the retry runs as new work. Retention covers the whole window the project promises.

**Reviewed for:** Does any key expire before the documented safe retry time? Check the retention settings beside the idempotent use cases.

### List Which Statuses and Headers Replay

**ID:** `litespec/ext/concurrency#list-which-statuses-and-headers-replay`

**Strength:** required

**Applies to:** artifact: record:use-case; layer: behavior

**Statement:** An idempotent use case MUST list the statuses and headers stored for replay.

**Rationale:** A completed retry reproduces the stored statuses and headers instead of the live ones. The declared list states which statuses and headers the store holds. A reviewer reads that list to learn exactly what a retry can return.

**Reviewed for:** Does every idempotent use case list the statuses and headers stored for replay? Check the idempotent use case pages.

### Store Only Fields That Replay Needs

**ID:** `litespec/ext/concurrency#store-only-fields-that-replay-needs`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** An idempotent implementation MUST store only safe response fields required to reproduce an accepted result.

**Rationale:** The store holds what a retry needs to reproduce the accepted result, and nothing more. Each extra field widens the sensitive data held under the key's retention. A field that serves no replay need stays out.

**Reviewed for:** Does every stored replay field serve a result callers reproduce? Check the replay store.

### Exclude Failures and Secrets from Replays

**ID:** `litespec/ext/concurrency#exclude-failures-and-secrets-from-replays`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** An idempotent implementation MUST NOT replay authentication failures, transient server failures, `Set-Cookie`, `Cache-Control`, `Vary`, `Expires`, `Age`, hop-by-hop headers, tokens, or secrets.

**Rationale:** Authentication failures and transient server failures describe one exchange rather than the accepted result, so replaying them answers a new request with an old failure. Tokens, secrets, cookies, and hop-by-hop headers each belong to the exchange that produced them. The caching headers are computed for the response that produced them, so replaying them hands a later caller freshness instructions from an earlier moment. [RFC 9111](https://datatracker.ietf.org/doc/html/rfc9111) makes each one a property of the exchange rather than of the result, so the replayed response computes its own.

**Reviewed for:** Does any replay return an authentication failure, a transient failure, a cookie, a hop-by-hop header, a token, or a secret? Check the replayed responses.

## Conventions

### Use the Standard Idempotency Header

**ID:** `litespec/ext/concurrency#use-the-standard-idempotency-header`

**Strength:** default

**Applies to:** artifact: endpoint

**Default:** Use `Idempotency-Key` for HTTP commands that accept client keys, bounded to 255 characters of visible ASCII.

**Replacement:** A consumer records a client-key convention naming the header and value bounds its commands accept.

**Rationale:** The `Idempotency-Key` header makes client-key behavior recognizable at the HTTP boundary. [The IETF draft](https://datatracker.ietf.org/doc/draft-ietf-httpapi-idempotency-key-header/) leaves the value opaque, so a server stores whatever a client sends. The 255-character visible-ASCII bound stops an unbounded key from becoming an unbounded store entry, and it keeps control characters out of log lines.

**Reviewed for:** Does every applicable operation accept `Idempotency-Key` within its bounds, or a recorded client-key convention naming its header and value bounds? Check the operations beside that convention.

### Keep Idempotency Persistence in Infrastructure

**ID:** `litespec/ext/concurrency#keep-idempotency-persistence-in-infrastructure`

**Strength:** default

**Applies to:** artifact: repository

**Default:** Keep idempotency persistence and transaction handling in Infrastructure.

**Replacement:** A consumer records an idempotency-store convention naming where its idempotency state lives.

**Rationale:** Idempotency state lives in provider transactions guarded by store constraints. That machinery belongs in Infrastructure, where provider details live. Application and Domain stay free of the store mechanics behind their operations.

**Reviewed for:** Does every idempotency persistence and transaction handling sit in Infrastructure, or under a recorded idempotency-store convention naming where its state lives? Check the idempotency store code beside that convention.

### Map Replay Responses at the API Boundary

**ID:** `litespec/ext/concurrency#map-replay-responses-at-the-api-boundary`

**Strength:** default

**Applies to:** artifact: endpoint

**Default:** Keep replay response mapping at the API boundary.

**Replacement:** A consumer records a replay-mapping convention naming where its replay mapping lives.

**Rationale:** Replaying a stored result means rebuilding its statuses and headers. Those are transport concerns, so the mapping lives at the API boundary. Application code then deals in operation results, never in wire shapes.

**Reviewed for:** Does every replay response mapping sit at the API boundary, or under a recorded replay-mapping convention naming where it lives? Check the replay mapping code beside that convention.

### Declare Applicable Headers

**ID:** `litespec/ext/concurrency#declare-applicable-headers`

**Strength:** default

**Applies to:** artifact: endpoint

**Default:** Declare the idempotency header as required on each applicable HTTP operation.

**Replacement:** A consumer records a header-declaration convention naming how its contracts expose the idempotency header.

**Rationale:** Each HTTP operation that accepts a client key declares the header as required. Generated API contracts then expose the requirement to every client author, under `litespec/backend/api#publish-precise-complete-schemas`. An undeclared header reaches only the callers who read the implementation.

**Reviewed for:** Does every applicable operation expose the idempotency header in the generated OpenAPI, or a recorded header-declaration convention naming how contracts expose it? Check the generated OpenAPI document beside that convention.

## Dependencies

Packages: None.

Related extensions: `outbox` for durable required external effects.

Guides: None.
