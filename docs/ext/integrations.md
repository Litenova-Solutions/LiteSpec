# External Integrations

**Stack:** any

**Concern:** infrastructure

External integrations call providers the product does not control. A provider can fail, change its responses, or deliver one message twice.

This extension covers each use case that depends on a provider. A use case is one command or query the product offers. It states the business action the use case needs, where provider details live, how failures map to project-owned results with stable refusal codes, and how tests simulate the network boundary, so a provider change touches one folder instead of the whole codebase.

## Activation

Activation scope: `local`.

Applicable specification kinds: `use-case`, `rule`, `flow`.

Enable `integrations` on each use case that calls an external system, receives an inbound integration event, or uses a provider SDK.

## Baseline Relationship

No baseline provision changes under this extension.

This extension switches on no optional record type. Each provider is recorded as an external system. That core record names the provider's actor, operations, and erasure.

## Standards

### Record Each Provider as an External System

**ID:** `litespec/ext/integrations#record-each-provider-as-an-external-system`

**Strength:** required

**Applies to:** artifact: record:external-system

**Statement:** An integration MUST record its provider as an external system naming its operations and erasure.

**Rationale:** The external-system record draws the boundary around the dependency. Its summary names the party behind the provider, `litespec/core/edges#describe-every-external-system-operation` names every operation the product may call, and `litespec/core/access#declare-erasure-for-every-external-system` names how personal data leaves it. A provider with no record gains new messages and fallbacks without review.

**Reviewed for:** Does every provider have an external system page in the Behavior external systems naming its operations and erasure? Check the external system pages.

### Call Only Declared Operations

**ID:** `litespec/ext/integrations#call-only-declared-operations`

**Strength:** required

**Applies to:** artifact: record:use-case

**Statement:** A `calls` effect MUST name an operation the cited external system declares.

**Rationale:** The effect names the system and its operation, and the operation states the action, timeout, retry, response, and cache, so each call to the outside is a declared edge. A call to an undeclared operation ships as an unreviewed dependency. `litespec/core/use-cases#declare-every-effect-with-one-verb-and-its-timing` states the effect verbs, and the `effect-undeclared` check reports an edge the records never declared.

**Example:** The Claimwick payout effect cites `calls` on the payout bank `send-batch` operation with its timeout and retry count.

**Reviewed for:** Does every `calls` effect name an operation its cited external system declares? Check each effect beside its external system record.

### Run Provider Calls from Event Handlers or Jobs

**ID:** `litespec/ext/integrations#run-provider-calls-from-event-handlers-or-jobs`

**Strength:** advisory

**Applies to:** artifact: reaction, job

**Statement:** A provider call SHOULD run from an event handler or a job, never from a command handler.

**Deviation:** A project that keeps an inline provider call records a waiver naming the compensation that undoes it.

**Rationale:** A command handler changes its aggregate and nothing else, and each consequence runs after the commit, as `litespec/backend/application#call-external-systems-through-their-ports` requires. An event handler or a job calls the provider after the outbox delivers the fact, with one delivery per handler under `litespec/ext/outbox#give-each-handler-its-own-delivery`. A provider failure then retries without rolling back the accepted change. When the command needs the provider result before it can answer, it records the intent first and an event handler calls the provider.

**Example:** The Claimwick reimburse-expense command records a pending payout on the expense, and the event handler for the recorded fact calls the payout provider through the business-action port.

**Reviewed for:** Does every provider call run from an event handler or a job, or carry a waiver naming its compensation? Check the provider call sites beside the waiver records.

### Define Business-Action Ports

**ID:** `litespec/ext/integrations#define-business-action-ports`

**Strength:** required

**Applies to:** artifact: port

**Statement:** Application MUST own a public external-service interface named for its business action.

**Rationale:** What the use case needs is a behavior, such as sending a payout, not a provider. Naming a public interface for that business action states the need without naming a provider, so Application depends on the behavior while providers change behind it.

**Example:** `IPayoutSender` describes the business action without exposing a payout-bank client.

**Reviewed for:** Is every Application port named for its business action, with no provider in its name or signature? Check each Application port.

### Isolate Provider Implementations

**ID:** `litespec/ext/integrations#isolate-provider-implementations`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Infrastructure MUST own provider clients, SDKs, authentication, and transport models.

**Rationale:** Provider clients, SDKs, authentication, and transport models name provider things in provider terms, so they live in Infrastructure, outside the Application business boundary. A provider SDK upgrade then touches Infrastructure instead of the use case.

**Example:** `IPayoutProviderClient` is provider infrastructure rather than an Application port.

**Reviewed for:** Does any Application layer code reference a provider SDK or transport type? Check the Application layer folders.

### Configure External Clients Through Validated Options

**ID:** `litespec/ext/integrations#configure-external-clients-through-validated-options`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An external client MUST bind its base address, credentials, timeout, and policy through validated options.

**Rationale:** Base address, credentials, timeout, and policy decide where the client calls and how it behaves. Validated options check each value at startup. A missing or invalid value then fails fast instead of misdirecting the first live call.

**Reviewed for:** Does every external client bind its base address, credentials, timeout, and policy through validated options? Check the client registrations.

### Use Managed HTTP Clients

**ID:** `litespec/ext/integrations#use-managed-http-clients`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An HTTP integration MUST use `HttpClientFactory` and applicable service discovery.

**Rationale:** `HttpClientFactory` centralizes handler lifetime in one place, and service discovery resolves provider addresses in one place. Together they keep HTTP construction out of the use case, where hand-built clients would repeat both at every call site.

**Reviewed for:** Does every HTTP integration use `HttpClientFactory` and applicable service discovery? Check the HTTP registrations.

### Reject Per-Request HTTP Clients

**ID:** `litespec/ext/integrations#reject-per-request-http-clients`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An HTTP integration MUST NOT construct a new HTTP client for each request.

**Rationale:** Each per-request client skips the central handler configuration and opens sockets the process never reuses. Under load, those exhausted sockets fail calls the provider would have served. Managed clients reuse what per-request construction throws away.

**Reviewed for:** Does any HTTP integration construct a new HTTP client per request? Check the HTTP call sites.

### Retry Transient Failures with Bounded Attempts

**ID:** `litespec/ext/integrations#retry-transient-failures-with-bounded-attempts`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An external client MUST retry transient failures with bounded attempts and jitter.

**Rationale:** When a provider fails, every caller retries at once, and the synchronized retries hit the provider the moment it recovers. Bounded attempts cap that load, and jitter spreads it over time. Without a bound, the retries become a second outage.

**Reviewed for:** Which external clients retry without a cap on attempts or without jitter? Check each client's retry policy.

### Protect Non-Idempotent Calls

**ID:** `litespec/ext/integrations#protect-non-idempotent-calls`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An external client MUST NOT retry a non-idempotent provider call without provider idempotency and documented replay behavior.

**Rationale:** Retrying a payout, a shipment, or any irreversible provider call can perform it twice. The retry is safe only with provider idempotency behind it and documented replay behavior stating what a repeat does. Without both, one failed call becomes two completed effects.

**Reviewed for:** Does every retried non-idempotent provider call rest on provider idempotency and documented replay behavior? Check the retry paths.

### Translate Provider Failure Outcomes

**ID:** `litespec/ext/integrations#translate-provider-failure-outcomes`

**Strength:** required

**Applies to:** artifact: any

**Statement:** An integration MUST map each provider error to a rejected, accepted, or unknown outcome with its project-owned result and stable diagnostics, and name the handling of the unknown case in `onUnknown`.

**Rationale:** Each provider reports failure in its own codes and shapes. Mapping them to project-owned results with stable diagnostics keeps the project failure contract steady while providers change theirs. A rejected outcome names the provider refusal, an accepted outcome names what the provider took, and an unknown outcome names a call whose effect stayed uncertain, such as a timeout. The unknown case names its handling in `onUnknown`: `dedupe-key` reuses the provider deduplication key on retry, `reconcile` settles the effect out of band, and `retry-safe` repeats a call the receiver tolerates twice.

The stable codes live in the project error catalog, and `litespec/backend/api#return-stable-problem-details` returns a code from that catalog. An integration that invents one code per provider creates one code space per provider, and a client that branches on failure then learns every code space. The provider error taxonomy feeds the mapping; it is not the mapping's output.

**Example:** A rejected Claimwick payout file and an unsettled batch both map to the project `payout-refused` code, while the provider's own code travels in diagnostics. A payout call that times out maps to unknown with `onUnknown: dedupe-key`, so the retry reuses the provider key instead of paying twice.

**Reviewed for:** Does every provider error map to a rejected, accepted, or unknown outcome with a project-owned result and stable diagnostics, and does every unknown case name its `onUnknown` handling? Check the integration failure mappings.

### Keep Provider Internals from API Responses

**ID:** `litespec/ext/integrations#keep-provider-internals-from-api-responses`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** An API MUST NOT expose provider bodies, credentials, headers, exception types, or internal account identifiers.

**Rationale:** Secrets, implementation details, and sometimes another account's data arrive in provider bodies, headers, exception types, and account identifiers. An API that returns them exposes all three to its callers. The mapped project-owned outcome carries the meaning without the internals.

**Reviewed for:** Does any API response expose provider bodies, credentials, headers, exception types, or internal account identifiers? Check the API responses.

### Declare Trust for Inbound Integration Events

**ID:** `litespec/ext/integrations#declare-trust-for-inbound-integration-events`

**Strength:** required

**Applies to:** artifact: record:integration-event

**Statement:** An inbound integration event MUST declare `trust` as trigger-only or payload with its correlation.

**Rationale:** An inbound fact carries one of two trusts: trigger-only wakes a read of the source, while payload is fit to act on. The `trust` declaration states which one, and the correlation states how the event matches, so no handler guesses how far to trust the event's fields. `litespec/core/edges#describe-every-integration-event-the-product-sends-or-receives` states the record, and `litespec/core/edges#name-how-every-inbound-integration-event-arrives` states how each inbound fact arrives. The record names in `arrivesThrough` the webhook entry point the provider calls or the external system whose broker or stream the module subscribes to, and every integration event names in `through` the external system it travels through.

**Reviewed for:** Does every inbound integration event declare trigger-only or payload trust with its correlation? Check each inbound integration event record.

### Verify Inbound Provider Messages

**ID:** `litespec/ext/integrations#verify-inbound-provider-messages`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** An inbound provider handler MUST verify signature, timestamp tolerance, replay protection, content type, size, schema, and event identity before processing on a webhook arrival, and event identity, schema, size, and replay protection on a subscribed broker or stream arrival.

**Rationale:** An inbound message arrives from the network, where anyone can send bytes. Signature, timestamp tolerance, replay protection, content type, size, schema, and event identity each guard a different part of that trust boundary, and skipping any one leaves its attack open. A subscribed broker or stream arrival authenticates at the subscription, so each message carries no per-message signature, and the handler verifies event identity, schema, size, and replay protection on each message.

Order matters on a webhook arrival: verify the signature first, over the raw body, before parsing the body or running any other check. An unauthenticated caller can reach a parser that runs first. A deserialized body is not the bytes the signature covered.

**Example:** A failed signature returns `401` with no body. A message that names the failed check tells an unauthenticated caller how to pass it.

**Reviewed for:** Does every webhook inbound handler run the checks its arrival requires, and does every subscription inbound handler verify event identity, schema, size, and replay protection? Check the inbound handlers.

### Process Duplicate Messages Safely

**ID:** `litespec/ext/integrations#process-duplicate-messages-safely`

**Strength:** required

**Applies to:** artifact: endpoint

**Statement:** An inbound provider handler MUST store or process duplicate events idempotently.

**Rationale:** After a timeout or an acknowledgement failure, the provider repeats delivery and the same event arrives twice. Idempotent storage or processing leaves one accepted effect. A handler without that protection pays, ships, or notifies twice.

**Reviewed for:** Does every inbound provider handler leave exactly one accepted effect for a duplicate event? Check the duplicate-delivery paths.

### Reject Formula-Leading Import Values

**ID:** `litespec/ext/integrations#reject-formula-leading-import-values`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A CSV or spreadsheet import MUST reject or neutralize a value that begins with a formula-leading character.

**Rationale:** The formula-leading characters are the equals sign, the plus sign, the minus sign, the at sign, the tab, and the carriage return. Escaping on export protects the user of that export, but not the reader of the next one: a different report can re-export an unchanged imported value without escaping it. [CSV injection](https://owasp.org/www-community/attacks/CSV_Injection) travels through the store, so the boundary that admits the value is where it stops.

**Example:** An import that rejects the value reports the row and the field. An import that neutralizes it records what it changed. A silently altered value is a data defect nobody can trace.

**Reviewed for:** Does every CSV or spreadsheet import reject or neutralize each formula-leading value? Check the import boundaries.

### Simulate Integration Boundaries

**ID:** `litespec/ext/integrations#simulate-integration-boundaries`

**Strength:** required

**Applies to:** artifact: test

**Statement:** An integration test MUST use WireMock.Net or a protocol-specific local endpoint.

**Rationale:** A live provider account makes tests slow, flaky, and expensive, and some failure modes never occur on demand. WireMock.Net or a protocol-specific local endpoint simulates the network contract under test control, so the suite covers provider behavior without calling the provider.

**Reviewed for:** Does every integration test run against WireMock.Net or a protocol-specific local endpoint instead of a live provider? Check the integration test suite.

### Cover Provider Failure Modes

**ID:** `litespec/ext/integrations#cover-provider-failure-modes`

**Strength:** required

**Applies to:** artifact: test

**Statement:** An integration test MUST cover timeout, connection failure, transient response, permanent response, malformed payload, rate limit, duplicate delivery, and unexpected response shape.

**Rationale:** Timeout, connection failure, transient and permanent responses, malformed payloads, rate limits, duplicate delivery, and unexpected shapes each exercise a different path through retry, mapping, and idempotency. A mode without a test leaves its path unverified, so the suite covers all eight and each path proves itself. The timeout mode exercises the unknown outcome: the test shows which `onUnknown` handling the retry follows.

An unexpected response shape is the mode a provider produces without failing. A well-formed response can carry a field that moved, a type that changed, or a value no one planned for. The response parses, but the behavior is still wrong, while every other listed mode reports that response as an error. A provider that versions its API changes shape on a schedule, and a pinned version still changes on the day the provider raises it.

**Reviewed for:** Does the suite test every declared failure mode: timeout, connection failure, transient response, permanent response, malformed payload, rate limit, duplicate delivery, and unexpected response shape? Check the integration test suite.

## Conventions

### Group Provider Code

**ID:** `litespec/ext/integrations#group-provider-code`

**Strength:** default

**Applies to:** artifact: any

**Default:** Place provider code under `Infrastructure/Integrations/{Provider}/`.

**Replacement:** A project names another root for provider code in its documentation.

**Rationale:** A reader finds everything about a provider in one place: its code sits inside one Infrastructure boundary under `Infrastructure/Integrations/{Provider}/`.

**Reviewed for:** Does every provider's code sit under `Infrastructure/Integrations/{Provider}/`, or under another root the project documentation names? Check the Infrastructure tree.

### Keep Provider Components Together

**ID:** `litespec/ext/integrations#keep-provider-components-together`

**Strength:** default

**Applies to:** artifact: any

**Default:** Keep one options class, client, transport models, mappings, and registration module in each provider folder.

**Replacement:** A project names another grouping for provider components in its documentation.

**Rationale:** When the provider changes, its options, client, transport models, mappings, and registration module change together. Keeping them in the provider folder keeps each change in one place, while a registration module stored elsewhere is easy to miss when the provider changes.

**Reviewed for:** Does every provider folder hold its options, client, models, mappings, and registration, or follow another grouping the project documentation names? Check each provider folder.

### Keep Ports Near Use Cases

**ID:** `litespec/ext/integrations#keep-ports-near-use-cases`

**Strength:** default

**Applies to:** artifact: port

**Default:** Keep business-action ports with their Application use cases.

**Replacement:** A project keeps business-action ports in one shared ports folder.

**Rationale:** A business-action port exists because its use case needs that behavior. Keeping the port with its Application use cases states the need beside the use case that needs it, while a ports folder far from the use cases hides which behavior each port serves.

**Reviewed for:** Does every business-action port sit with its Application use cases, or in the shared port folder the project keeps? Check the Application folders.

## Dependencies

Packages: `Microsoft.Extensions.Http.Resilience`, `WireMock.Net` in integration tests.

Related extensions: `outbox`, which delivers each event to each handler separately.

Guides: None.
