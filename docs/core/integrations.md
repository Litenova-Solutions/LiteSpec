# External Integrations

**Stack:** any

**Concern:** infrastructure

External providers sit outside the product: banks, payout rails, and spreadsheet imports the team reaches only through a contract. This page is for developers who connect a use case to one of those providers. A provider fails, changes shape, or delivers twice, so each boundary carries explicit checks for leaving calls, entering messages, and imported values.

## Concepts

### Where Providers Meet the Product

Each provider is recorded as an external system with its operations and its erasure path. A use case reaches the outside through a `calls` effect that names one operation the system declares. The module owns a port named for the business action, such as sending a payout, while Infrastructure holds the client, the SDK, the authentication, and the transport models.

One adapter folder groups the options class, the client, the transport models, the mappings, and the registration module. A provider change then lands in one place. The Claimwick payout provider groups its options, client, transport models, and mappings in its adapter folder.

### How Calls Leave the Product

A provider call runs from an event handler or a job after the outbox delivers the fact, with one delivery per handler. A command records its intent first and answers; the handler calls the provider after the commit, so a provider failure retries with the accepted change intact. A query changes nothing, so an inline call that fails fails that read alone.

Each client binds its base address, credentials, timeout, and policy through options validated at startup. HTTP integrations build on `HttpClientFactory` with service discovery, with no client constructed per request. Retries cover transient failures with a bound and jitter, spreading repeated calls over time.

```yaml
onUnknown: dedupe-key  # the retry reuses the provider deduplication key
```

Each provider error maps to a rejected, accepted, or unknown outcome with a project-owned result and stable diagnostics from the project catalog. Each unknown case names its `onUnknown` handling: `dedupe-key` reuses the provider key on retry, `reconcile` settles the effect out of band, and `retry-safe` repeats a call the receiver tolerates twice. API responses carry the mapped outcome, with provider bodies, credentials, headers, exception types, and account identifiers left out.

### How Messages Enter the Product

Each inbound integration event declares its trust: trigger-only wakes a read of the source, while payload is fit to act on, with the correlation that matches it. The record names the arrival path in `arrivesThrough` and the system in `through`.

A webhook arrival carries a per-message signature over the raw body, checked first, before parsing or any other check. A subscription arrival authenticates at the subscription, so the handler checks event identity, schema, size, and replay protection on each message.

Spreadsheet imports treat formula-leading values at the boundary, as the provision below states. Integration tests run against WireMock.Net or a protocol-specific local endpoint. The suite covers timeout, connection failure, transient and permanent responses, malformed payloads, rate limits, duplicate delivery, and unexpected shapes, with the timeout case exercising the unknown outcome and its `onUnknown` handling.

## Standards

### Protect Non-Idempotent Calls

**ID:** `litespec/core/integrations#protect-non-idempotent-calls`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An external client MUST NOT retry a non-idempotent provider call without provider idempotency and documented replay behavior.

**Rationale:** A payout, a shipment, or any irreversible call performed twice charges twice or ships twice. Provider idempotency plus documented replay behavior turns the retry into a repeat the provider absorbs.

**Example:** The Claimwick payout retry reuses the provider deduplication key, and the replay notes state that a repeated send-batch settles once.

**Reviewed for:** Does every retried non-idempotent provider call rest on provider idempotency and documented replay behavior? Check the retry paths.

### Verify Inbound Provider Messages

**ID:** `litespec/core/integrations#verify-inbound-provider-messages`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** An inbound provider handler MUST verify signature, timestamp tolerance, replay protection, content type, size, schema, and event identity before processing on a webhook arrival, and event identity, schema, size, and replay protection on a subscribed broker or stream arrival.

**Rationale:** Inbound bytes arrive from the network, where any sender reaches the endpoint. Each check guards one part of that boundary: the signature proves the sender, the timestamp and replay protection stop old copies, and content type, size, schema, and event identity stop malformed or oversized bodies. The signature runs over the raw body first, since parsing first hands an unauthenticated caller the parser.

**Example:** A Claimwick payout-bank webhook with a failed signature returns `401` with no body.

**Reviewed for:** Does every webhook inbound handler run the checks its arrival requires, and does every subscription inbound handler verify event identity, schema, size, and replay protection? Check the inbound handlers.

### Process Duplicate Messages Safely

**ID:** `litespec/core/integrations#process-duplicate-messages-safely`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** An inbound provider handler MUST store or process duplicate events idempotently.

**Rationale:** Providers repeat delivery after a timeout or a lost acknowledgement, so the same event arrives twice. Storage or processing that absorbs the repeat leaves one accepted effect, while a handler without that protection pays or notifies twice.

**Example:** Claimwick stores the processed event identity for each payout-bank notification and drops the second copy.

**Reviewed for:** Does every inbound provider handler leave exactly one accepted effect for a duplicate event? Check the duplicate-delivery paths.

### Reject Formula-Leading Import Values

**ID:** `litespec/core/integrations#reject-formula-leading-import-values`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A CSV or spreadsheet import MUST reject or neutralize a value that begins with a formula-leading character.

**Rationale:** The formula-leading characters are the equals sign, the plus sign, the minus sign, the at sign, the tab, and the carriage return. A value that opens with one evaluates as a formula in the next tool that opens it, and the value travels through the store into later exports. [CSV injection](https://owasp.org/www-community/attacks/CSV_Injection) stops at the boundary that admits the value.

**Example:** A Claimwick member import rejects the row and names the row and the field. An import that neutralizes the value records what it changed.

**Reviewed for:** Does every CSV or spreadsheet import reject or neutralize each formula-leading value? Check the import boundaries.

## Conventions

None.
