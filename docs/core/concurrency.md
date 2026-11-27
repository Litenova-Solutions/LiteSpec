# Concurrency and Idempotency

**Stack:** any

**Concern:** application

Overlapping writes overwrite each other silently, and a retried request runs twice. This page states how a project documents conflicting writes, coordinates them with versions, and makes retried operations return one result through client keys and fingerprints. It applies when overlap or retry corrupts behavior, so its callers get one predictable outcome.

## Concepts

### How Conflicting Writes Settle

A command that conflicts with another accepted write states `conflicts` with the subject naming the operations and the invalid result, the winner naming `first-write`, `last-write`, or `merge`, and the refusal naming the id the loser gets. Claimwick's approve-expense command names a second approval of the same expense, keeps the first write, and refuses the late one with its declared refusal id. Every stored aggregate already carries a version the store compares at commit, while caller-supplied versions appear only beside a `conflicts` entry.

```yaml
# use-case/spending.approve-expense
conflicts:
  subject: two approvals of the same expense
  winner: first-write
  refusal: expense-already-approved
```

The caller reads the current state with its strong persistence-derived `ETag` and sends it back in `If-Match` on the write. The server compares what the caller saw with what the store holds, inside the same transaction as the change. A missing `If-Match` answers 428 `precondition_required`, and a stale version answers 409 `version_conflict` with its stable Problem Details code.

A conflicting write that replaces the accepted change destroys committed work while the loser learns nothing. The conflict answer carries the facts the caller reconciles with, and no version value leaks into another contract field past the `ETag`.

### How Retried Operations Return One Result

Idempotency runs as a pipeline stage after authorization and validation, before the handler. A replayed key returns the stored response without running the handler again, while a denied or invalid attempt consumes no client key. The client sends `Idempotency-Key` up to 255 characters of visible ASCII, and each operation declares the header where its contracts expose it so generated API contracts show it to every client author.

Each key scopes to tenant, authenticated actor, and operation, so one caller's key replays nobody else's operation. A project without tenancy uses one fixed tenant value. The fingerprint derives from canonical mapped command input, route identity, and the actor identifier, with credentials and the client key outside it. A cryptographic hash stands in where support needs no original sensitive content, and the store keeps original content only where support reads it.

The fingerprint, state, response status, response body, and expiry commit with the business change in one provider transaction, leaving neither a completed record without its change nor a change without its replay record. The unique constraint on tenant, actor, operation, and client key admits one winner among concurrent same-key requests. A losing request with a matching fingerprint reloads the winning record and replays it, while a reuse of one key with a different fingerprint meets the documented rejection. A failed pre-commit request leaves neither the business change nor a completed replay record, so a later retry performs the operation for the first time.

A completed retry returns the original response status and body, so the caller sees one execution however often the network retries. Each use case states whether a concurrent request waits, learns the work is in progress, or receives a stable conflict. A provider call with no undo runs after the business transaction commits, and each outside effect travels through the outbox extension or a documented provider idempotency contract. Idempotency persistence and transaction handling live in Infrastructure with the provider mechanics, and replay response mapping lives at the API boundary with the transport shapes.

### How Keys Expire and What Replays Hold

Each key lives past the caller retry window plus the business risk of a late duplicate, recorded as a setting record. A key that expires inside the promised window turns a legitimate retry into duplicate business work. A bounded maintenance process removes expired records within fixed limits on rate and batch, keeping the store small without starving the application.

A completed retry reproduces the stored statuses and headers instead of the live ones, from the declared list on the use case. The store holds only safe response fields that reproduce the accepted result. Authentication failures, transient server failures, `Set-Cookie`, caching headers, hop-by-hop headers, tokens, and secrets stay out of replays, since each describes the exchange that produced it rather than the accepted result. The replayed response computes its own freshness values.

Outside effects that travel past a process failure use [Durable Delivery](outbox.md).

## Standards

### Document Write Conflict Behavior

**ID:** `litespec/core/concurrency#document-write-conflict-behavior`

**Strength:** required

**Applies to:** artifact: record:use-case; layer: behavior

**Statement:** A command that can conflict with another accepted write MUST state `conflicts` naming what conflicts in `subject`, which write wins in `winner`, and the refusal id the loser gets in `refusal`.

**Rationale:** The subject names the operations and the invalid result they leave, the winner names first-write, last-write, or merge, and the refusal gives the loser a stable code. Without the entry, each conflict invents its own answer and callers handle all of them.

**Example:** Claimwick's approve-expense command states `conflicts` with `subject` naming a second approval of the same expense, `winner: first-write`, and `refusal` naming the refusal id the late approval gets.

**Checked by:** `litespec model check` reports `field-extension-unselected` for a `conflicts` entry written when `concurrency` is not selected, and `record-invalid` for a `conflicts` entry that names no `subject`, no `winner`, or no `refusal`, or a `winner` outside `first-write`, `last-write`, and `merge`.

### Compare Caller and Stored Versions in One Transaction

**ID:** `litespec/core/concurrency#compare-caller-and-stored-versions-in-one-transaction`

**Strength:** required

**Applies to:** artifact: command-handler; when: concurrency

**Statement:** An implementation MUST compare the caller-supplied version with the stored version in the same transaction as the change.

**Rationale:** The caller version marks the state the caller saw, and the stored version marks what the database holds. Comparing the two inside the same transaction as the change catches a competing accepted write that lands between the read and the write.

**Reviewed for:** Does every caller-version comparison run inside the transaction carrying its change? Check the conflicting-write paths.

### Reject Silent Overwrites

**ID:** `litespec/core/concurrency#reject-silent-overwrites`

**Strength:** required

**Applies to:** artifact: command-handler; when: concurrency

**Statement:** An implementation MUST NOT silently overwrite a conflicting accepted change.

**Rationale:** A conflicting write that replaces the accepted change destroys work another caller committed, and the losing caller loses its change without learning it. Refusing hands the caller the facts it reconciles with.

**Reviewed for:** Does any conflicting write silently replace the accepted change? Check the conflict outcomes.

### Store Idempotency State Atomically

**ID:** `litespec/core/concurrency#store-idempotency-state-atomically`

**Strength:** required

**Applies to:** artifact: command-handler; when: concurrency

**Statement:** An idempotent operation MUST store its fingerprint, state, response status, response body, and expiry with its business change in one provider transaction.

**Rationale:** The fingerprint, state, response, and expiry describe the accepted change, so they commit with it in one provider transaction. One transaction leaves neither a completed record without its change nor a change without its replay record.

**Reviewed for:** Does every idempotent operation store its fingerprint, state, response, and expiry with its business change in one transaction? Check the keyed write paths.

### Reject Conflicting Key Reuse

**ID:** `litespec/core/concurrency#reject-conflicting-key-reuse`

**Strength:** required

**Applies to:** artifact: command-handler; when: concurrency

**Statement:** An idempotent operation MUST reject reuse of one key with a different request fingerprint.

**Rationale:** One key names one intent. A different fingerprint under the same key names a different intent, and replaying the first result answers a request nobody made.

**Reviewed for:** Does every reuse of one key with a different fingerprint meet the documented rejection? Check the key-reuse paths.

### Replay Completed Results

**ID:** `litespec/core/concurrency#replay-completed-results`

**Strength:** required

**Applies to:** artifact: command-handler; when: concurrency

**Statement:** A completed idempotent retry MUST return the original response status and body.

**Rationale:** The first attempt did the work, and the retry repeats the answer instead of the work. The caller sees one execution however often the network retries.

**Example:** Claimwick's submit-expense retry returns the accepted result with its original status after the first attempt commits.

**Reviewed for:** Does every completed idempotent retry return the original response status and body? Check the replay paths.

### Delay Irreversible Provider Calls

**ID:** `litespec/core/concurrency#delay-irreversible-provider-calls`

**Strength:** required

**Applies to:** artifact: command-handler; when: concurrency

**Statement:** An idempotent operation MUST NOT perform an irreversible provider call before its business transaction commits.

**Rationale:** Some provider calls have no undo. A call placed before commit fires even when the transaction then rolls back, so the business change vanishes while its outside effect stands. The commit comes first, and a rolled-back change leaves no outside trace.

**Reviewed for:** Does any irreversible provider call run before its business transaction commits? Check the idempotent operation paths.

## Conventions

None.
