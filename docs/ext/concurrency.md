# Concurrency and Idempotency

## Intent

Concurrency and idempotency rules stop overlapping writes from silently overwriting each other and stop a retried request from executing twice. This page states how a project documents conflicting writes, coordinates them with versions, and makes retried operations return one result through client keys and fingerprints. A team applies it when overlap or retry can corrupt behavior, so its callers get one predictable outcome instead of a silent overwrite or a duplicated effect.

## Activation

Activation scope: `local`.

Applicable specification kinds: `module`, `aggregate`, `use-case`.

The consumer enables `concurrency` when accepted writes can conflict with each other, or when a retry can duplicate an irreversible or externally visible effect.

## Baseline Relationship

This extension replaces no baseline provision. `litespec/ext/concurrency#return-precondition-outcomes` adds the 428 outcome for a missing precondition to the outcome table that `litespec/backend/api#use-consistent-status-codes` owns.

This extension switches on `setting`: key retention is a setting record under `litespec/ext/concurrency#set-key-retention`.

Its client-key standards apply to operations whose `idempotency.mode` is `key`. Every stored aggregate already carries a version that the store compares at commit under `litespec/backend/persistence#version-every-aggregate`; the version standards on this page govern what the caller sees for a conflict.

## Standards

### Document Write Conflict Behavior

**ID:** `litespec/ext/concurrency#document-write-conflict-behavior`

**Statement:** A conflicting-write use case MUST document conflicting operations, invalid combined result, expected winner behavior, and user-visible conflict response.

**Rationale:** Version coordination exists to protect one invariant. Naming the conflicting operations, the invalid combined result, the expected winner, and the caller-visible response states which invariant that is. An implementation without that document cannot tell a conflict it has to reject from contention it has to absorb.

**Reviewed for:** Which conflicting-write use cases lack conflicts, invalid state, winner behavior, or caller response? Look at the use case pages.

### Limit Version Checks

**ID:** `litespec/ext/concurrency#limit-version-checks`

**Statement:** An implementation MUST NOT require a caller-supplied version without a documented conflicting-write case.

**Rationale:** Every stored aggregate already carries a baseline version that the store compares at commit, so the version itself needs no document. Requiring the caller to supply a version adds a rejection path the caller has to handle: a missing or stale caller version refuses an otherwise valid write. The conflicting-write document stays the gate for that caller burden: no case, no caller-version requirement.

**Checked by:** `ConcurrencyVersionTests` link every caller-version requirement to a documented conflicting-write case.

### Compare Expected Aggregate Versions

**ID:** `litespec/ext/concurrency#compare-expected-aggregate-versions`

**Statement:** An implementation MUST compare the caller-supplied version with the stored version in the same transaction as the change.

**Rationale:** The caller-supplied version marks which write the caller saw, and the stored version marks what the database holds. Comparing the two in the same transaction as the change detects a competing accepted write against the same state, through the selected persistence model. A comparison outside the transaction races the commit it claims to guard.

**Checked by:** `ConcurrencyVersionTests` race conflicting aggregate writes with caller-supplied expected versions.

### Map Failed Expected Versions

**ID:** `litespec/ext/concurrency#map-failed-expected-versions`

**Statement:** An HTTP API MUST map a failed expected version to status 409 Problem Details with a stable code.

**Rationale:** A caller that loses a version race retries: re-read, re-apply, re-send. Status 409 with a stable Problem Details code names the loss as a conflict, distinct from validation and authorization failures. Without the stable code, the caller guesses which failure it met.

**Checked by:** `ConcurrencyVersionTests` assert a failed expected version returns 409 Problem Details with its stable code.

### Reject Silent Overwrites

**ID:** `litespec/ext/concurrency#reject-silent-overwrites`

**Statement:** An implementation MUST NOT silently overwrite a conflicting accepted change.

**Rationale:** A conflicting write that silently replaces the accepted change destroys work another caller committed. The losing caller never learns its change is gone. Rejecting the conflict instead hands the caller the facts it needs to reconcile.

**Checked by:** `ConcurrencyVersionTests` assert conflicting accepted writes preserve the winner without silent replacement.

### Apply HTTP Version Preconditions

**ID:** `litespec/ext/concurrency#apply-http-version-preconditions`

**Statement:** An HTTP operation requiring a caller version MUST return a strong persistence-derived `ETag` and require `If-Match` on writes.

**Rationale:** The caller reads the current state with its strong persistence-derived `ETag` and sends it back in `If-Match` on the write. The server compares what the caller saw with what is stored. The round trip carries the version through HTTP headers instead of a second contract field.

**Example:** A current `ETag` permits the write; a changed entity version rejects it.

**Checked by:** `ConcurrencyVersionTests` require and validate a strong persistence-derived ETag.

### Return Precondition Outcomes

**ID:** `litespec/ext/concurrency#return-precondition-outcomes`

**Statement:** An HTTP API MUST return 428 `precondition_required` for missing `If-Match` and 409 `version_conflict` for stale versions.

**Rationale:** A missing `If-Match` and a stale version are different caller mistakes with different fixes: send the version, or re-read and retry. Status 428 `precondition_required` names the omission and 409 `version_conflict` names the stale version. One code for both would leave the caller guessing which fix applies.

**Checked by:** `ConcurrencyVersionTests` return 428 for a missing `If-Match` and 409 for a stale version.

### Hide Provider Version Values

**ID:** `litespec/ext/concurrency#hide-provider-version-values`

**Statement:** An HTTP API MUST NOT expose a provider-specific version value in another contract field.

**Rationale:** The `ETag` is the one contract field that carries the persistence version. A provider-specific version leaked into another field gives callers a second version to depend on. Hiding it keeps the coordination behind one boundary the project can change.

**Checked by:** `ConcurrencyVersionTests` assert public transport models expose no provider-specific version field.

### Run Idempotency in the Pipeline

**ID:** `litespec/ext/concurrency#run-idempotency-in-the-pipeline`

**Statement:** Idempotency MUST run as a pipeline stage after authorization and validation, before the handler.

**Rationale:** Authorization and validation run first, so a denied or invalid attempt neither consumes a client key nor replays a stored result. The handler runs after, so a replayed key returns the stored response without executing the handler again. Any other order either spends keys on attempts the pipeline would refuse or executes work the store already accepted.

**Reviewed for:** Which idempotent operations run idempotency outside a pipeline stage after authorization and validation, before the handler? Look at the pipeline stage order.

### Scope Client Keys

**ID:** `litespec/ext/concurrency#scope-client-keys`

**Statement:** An idempotent operation MUST scope its client key to the tenant, authenticated actor, and operation.

**Rationale:** A client key alone proves nothing about who sent it: two callers can pick the same key. Scoping the key to tenant, authenticated actor, and operation stops one caller's key from replaying another caller's operation. A project without the tenancy extension uses one fixed tenant value.

**Checked by:** `IdempotencyKeyTests` assert keys from different tenants, actors, or operations do not replay each other.

### Store Idempotency State Atomically

**ID:** `litespec/ext/concurrency#store-idempotency-state-atomically`

**Statement:** An idempotent operation MUST store its fingerprint, state, response status, response body, and expiry with its business change in one provider transaction.

**Rationale:** The fingerprint, state, response status, response body, and expiry describe the accepted change, so they commit with it in one provider transaction. One transaction leaves neither a completed record without its change nor a change without its replay record. A retry then replays exactly what the first attempt accepted.

**Checked by:** `IdempotencyKeyTests` commit fingerprint, state, response, and expiry with the accepted business change.

### Reject Conflicting Key Reuse

**ID:** `litespec/ext/concurrency#reject-conflicting-key-reuse`

**Statement:** An idempotent operation MUST reject reuse of one key with a different request fingerprint.

**Rationale:** A key reused with a different request fingerprint names two different intents under one key. Accepting both would replay the first intent's result for the second intent's request. Rejecting the reuse keeps one key bound to one intent.

**Checked by:** `IdempotencyKeyTests` assert reused keys with changed input return the documented rejection.

### Enforce Key Uniqueness

**ID:** `litespec/ext/concurrency#enforce-key-uniqueness`

**Statement:** An idempotency store MUST enforce a unique constraint on tenant, actor, operation, and client key.

**Rationale:** Two concurrent requests can arrive with the same key before either commits. The unique constraint on tenant, actor, operation, and client key admits one winner and rejects the other at the store. Application code alone cannot order two requests that arrive together. A project without the tenancy extension uses one fixed tenant value.

**Checked by:** `IdempotencyKeyTests` assert concurrent inserts prove the tenant-actor-operation-key uniqueness constraint.

### Canonicalize Request Fingerprints

**ID:** `litespec/ext/concurrency#canonicalize-request-fingerprints`

**Statement:** An idempotency implementation MUST derive its fingerprint from canonical mapped command input, route identity, and the authenticated actor identifier, excluding credentials and the client key.

**Rationale:** The fingerprint decides whether two requests under one key carry the same intent. Canonical mapped command input, route identity, and the authenticated actor identifier make equivalent requests produce one value. The actor identifier is in the fingerprint because two actors can present the same client key, and a fingerprint without it would replay one actor's stored response to another. The credential that proved the actor stays out: a cookie and a signature each change between two requests the rule treats as equal.

**Checked by:** `IdempotencyKeyTests` assert equivalent mapped requests produce one fingerprint without credentials or client key.

### Protect Fingerprint Source Data

**ID:** `litespec/ext/concurrency#protect-fingerprint-source-data`

**Statement:** An idempotency implementation MUST store a cryptographic hash when support does not require original sensitive request content.

**Rationale:** The fingerprint needs to compare requests, not to show what they contained. A cryptographic hash preserves the comparison while the original sensitive content stays out of the store. Support keeps original content only where it needs the original to help.

**Reviewed for:** Which idempotency stores keep original sensitive request content that support never needs? Look at the fingerprint store.

### Replay Completed Results

**ID:** `litespec/ext/concurrency#replay-completed-results`

**Statement:** A completed idempotent retry MUST return the original response status and body.

**Rationale:** The first attempt performed the state change, and the retry performs it no second time. Returning the original response status and body gives the retry the accepted result. The caller sees one execution no matter how many times the network retried.

**Checked by:** `IdempotencyReplayTests` return the original accepted status and body.

### Define Concurrent Retry Behavior

**ID:** `litespec/ext/concurrency#define-concurrent-retry-behavior`

**Statement:** An idempotent use case MUST define whether a concurrent request waits, reports in-progress, or returns a stable conflict.

**Rationale:** An identical request can arrive while the first is still running. The use case states whether the concurrent request waits, learns the work is in progress, or receives a stable conflict. Without one documented result, each implementation invents its own and callers handle all of them.

**Checked by:** `IdempotencyReplayTests` prove the documented wait, in-progress, or conflict behavior.

### Reload Concurrent Winners

**ID:** `litespec/ext/concurrency#reload-concurrent-winners`

**Statement:** A losing concurrent idempotency request MUST reload and replay the winning record when its fingerprint matches.

**Rationale:** The unique constraint admits one winner among concurrent same-key requests. A losing request whose fingerprint matches reloads the winning record and replays its result. The loser then returns the accepted outcome instead of an error for work that succeeded.

**Checked by:** `IdempotencyReplayTests` reload and replay the matching winner record.

### Discard Failed Replay State

**ID:** `litespec/ext/concurrency#discard-failed-replay-state`

**Statement:** A failed pre-commit idempotent request MUST leave neither the business change nor a completed replay record.

**Rationale:** A request that fails before commit changed nothing, so it leaves neither the business change nor a completed replay record. A leftover completed record would replay success for work that never happened. Clean state lets a later retry perform the operation for the first time.

**Checked by:** `IdempotencyReplayTests` leave no business change or completed replay state.

### Delay Irreversible Provider Calls

**ID:** `litespec/ext/concurrency#delay-irreversible-provider-calls`

**Statement:** An idempotent operation MUST NOT perform an irreversible provider call before its business transaction commits.

**Rationale:** A provider call that cannot be undone follows the commit. A call made before commit fires even when the transaction then rolls back. The business change would vanish while its external effect stands.

**Reviewed for:** Which irreversible provider calls run before their business transaction commits? Look at the idempotent operation paths.

### Route Required External Effects Durably

**ID:** `litespec/ext/concurrency#route-required-external-effects-durably`

**Statement:** An idempotent operation MUST use the outbox extension or a documented provider idempotency contract for required external effects.

**Rationale:** A required external effect has to survive retries without duplicating. The outbox extension delivers it durably after commit, or a documented provider idempotency contract lets the provider absorb the replay. An effect with neither path fires once per retry.

**Reviewed for:** Which required external effects use neither the outbox nor a documented provider idempotency contract? Look at the idempotent operation paths.

### Set Key Retention

**ID:** `litespec/ext/concurrency#set-key-retention`

**Statement:** An idempotency owner MUST record key retention as a setting record set from the caller retry window and business risk.

**Rationale:** A key that expires too early turns a legitimate retry into duplicate business work. Retention therefore spans the caller retry window plus the business risk of a late duplicate. The setting record states that span where reviewers and operators read it.

**Reviewed for:** Which key retentions miss the caller retry window or the business risk? Look at the setting record beside the idempotent use case.

### Bound Expired-Record Cleanup

**ID:** `litespec/ext/concurrency#bound-expired-record-cleanup`

**Statement:** An idempotency owner MUST remove expired records through a bounded maintenance process.

**Rationale:** Expired keys accumulate without cleanup, and cleanup without bounds steals capacity from live work. A bounded maintenance process removes expired records within fixed limits on rate and batch. The store stays small without starving the application.

**Checked by:** `IdempotencyRetentionTests` show bounded deletion of expired idempotency records.

### Retain Safe Retry Keys

**ID:** `litespec/ext/concurrency#retain-safe-retry-keys`

**Statement:** An idempotency owner MUST NOT expire a key before callers can safely retry.

**Rationale:** Callers retry inside a documented window after an uncertain response. A key expired inside that window is gone when the retry arrives, so the retry executes as new work. Retention covers the whole window the project promises.

**Checked by:** `IdempotencyRetentionTests` reject expiration before the documented safe retry time.

### Declare Replayed Transport Outcomes

**ID:** `litespec/ext/concurrency#declare-replayed-transport-outcomes`

**Statement:** An idempotent use case MUST list the statuses and headers stored for replay.

**Rationale:** A completed retry reproduces the stored statuses and headers instead of the live ones. The declared list states which statuses and headers the store holds. A reviewer reads the list to learn exactly what a retry can return.

**Checked by:** `IdempotencyOutcomeTests` match replayed statuses and headers against the declared list.

### Restrict Replay Data

**ID:** `litespec/ext/concurrency#restrict-replay-data`

**Statement:** An idempotent implementation MUST store only safe response fields required to reproduce an accepted result.

**Rationale:** The store holds what a retry needs to reproduce the accepted result and nothing more. Each extra field widens the sensitive data held under the key's retention. A field that serves no replay need stays out.

**Reviewed for:** Which stored replay fields serve no accepted result callers reproduce? Look at the replay store.

### Exclude Unsafe Replay Outcomes

**ID:** `litespec/ext/concurrency#exclude-unsafe-replay-outcomes`

**Statement:** An idempotent implementation MUST NOT replay authentication failures, transient server failures, `Set-Cookie`, `Cache-Control`, `Vary`, `Expires`, `Age`, hop-by-hop headers, tokens, or secrets.

**Rationale:** Authentication failures and transient server failures describe one exchange rather than the accepted result, so replaying them answers a new request with an old failure. Tokens, secrets, cookies, and hop-by-hop headers each belong to the exchange that produced them. The caching headers are computed for the response that produced them: replaying them hands a later caller freshness instructions from an earlier moment. [RFC 9111](https://datatracker.ietf.org/doc/html/rfc9111) makes each one a property of the exchange rather than of the result, so the replayed response computes its own.

**Checked by:** `IdempotencyOutcomeTests` exclude authentication, transient failures, cookies, hop-by-hop headers, tokens, and secrets.

## Conventions

### Use the Standard Idempotency Header

**ID:** `litespec/ext/concurrency#use-the-standard-idempotency-header`

**Default:** Use `Idempotency-Key` for HTTP commands that accept client keys, bounded to 255 characters of visible ASCII.

**Replacement:** A consumer records a client-key convention naming the header and value bounds its commands accept.

**Rationale:** The `Idempotency-Key` header makes client-key behavior recognizable at the HTTP boundary. [The IETF draft](https://datatracker.ietf.org/doc/draft-ietf-httpapi-idempotency-key-header/) leaves the value opaque, so a server stores whatever a client sends. The 255-character visible-ASCII bound stops an unbounded key from becoming an unbounded store entry and keeps control characters out of log lines.

**Checked by:** `ConcurrencyTests` assert applicable operations accept `Idempotency-Key` or follow the recorded local convention.

### Keep Idempotency Persistence in Infrastructure

**ID:** `litespec/ext/concurrency#keep-idempotency-persistence-in-infrastructure`

**Default:** Keep idempotency persistence and transaction handling in Infrastructure.

**Replacement:** A consumer records an idempotency-store convention naming where its idempotency state lives.

**Rationale:** Idempotency state lives in provider transactions guarded by store constraints. That machinery belongs in Infrastructure, where provider details live. Application and Domain stay free of the store mechanics behind their operations.

**Checked by:** `ConcurrencyTests` assert idempotency persistence and transaction handling live in Infrastructure.

### Map Replay Responses at the API Boundary

**ID:** `litespec/ext/concurrency#map-replay-responses-at-the-api-boundary`

**Default:** Keep replay response mapping at the API boundary.

**Replacement:** A consumer records a replay-mapping convention naming where its replay mapping lives.

**Rationale:** Replaying a stored result means rebuilding its statuses and headers. Those are transport concerns, so the mapping lives at the API boundary. Application code then deals in operation results, never in wire shapes.

**Checked by:** `ConcurrencyTests` assert replay response mapping lives at the API boundary.

### Declare Applicable Headers

**ID:** `litespec/ext/concurrency#declare-applicable-headers`

**Default:** Declare the idempotency header as required on each applicable HTTP operation.

**Replacement:** A consumer records a header-declaration convention naming how its contracts expose the idempotency header.

**Rationale:** Each HTTP operation that accepts a client key declares the header as required. Generated API contracts then expose the requirement to every client author under `litespec/backend/api#publish-precise-complete-schemas`. An undeclared header reaches only the callers who read the implementation.

**Checked by:** `ConcurrencyTests` asserts generated OpenAPI declares the header for every applicable operation.

## Dependencies

Packages: None.

Related extensions: `outbox` for durable required external effects.

Guides: None.
