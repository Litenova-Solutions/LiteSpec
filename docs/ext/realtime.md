# Live Delivery

**Stack:** any

**Concern:** infrastructure

Live updates reach open screens in seconds through a realtime transport the browser keeps open. One-way notifications travel over server-sent events, and interactive messaging uses SignalR; connections, subscriptions, and clients stay within their bounds under load and failure.

These rules apply after the team measures polling against an update target and accepts the cost of disconnects and missed messages.

## Activation

Activation scope: `local`.

Applicable specification kinds: `use-case`, `flow`.

A use case or flow selects this extension for live updates after the team measures polling against the update target: the polling interval, the visible delay, the request cost, and the required update speed. The selection also names the accepted cost of disconnects and missed messages: how the client recovers when the connection drops or a notification never arrives.

## Baseline Relationship

This extension replaces no baseline provision. It switches on no optional record type: each pushed update is a notification record, and each resource bound is a setting record.

One-way notifications travel over server-sent events, which need no added package because the platform ships them.

## Standards

### Measure Polling Before Choosing Realtime

**ID:** `litespec/ext/realtime#measure-polling-before-choosing-realtime`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** A realtime use case MUST document polling behavior, measured cost, update latency, expected connections, and recovery behavior.

**Rationale:** Open connections cost memory, CPU, and operating attention on every host, so the team writes down what polling costs, what the update target requires, how many connections to expect, and how recovery works before choosing transport. The use case page holds that record, so a later reviewer sees which measurements justified the choice.

**Reviewed for:** Does every realtime use case page record polling behavior, measured cost, update target, expected connections, and recovery behavior? Read each realtime use case page.

### Reject Cosmetic Realtime Adoption

**ID:** `litespec/ext/realtime#reject-cosmetic-realtime-adoption`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** A product MUST NOT activate realtime only to make an interface feel immediate.

**Rationale:** Realtime transport that serves only a feeling of immediacy spends connection capacity and operating attention without meeting any update target. The extension asks for a measured update requirement first, so polling stays where polling suffices.

**Reviewed for:** Does every realtime decision cite a measured update requirement? Read the use case page beside each realtime transport.

### Use Server-Sent Events for Notifications

**ID:** `litespec/ext/realtime#use-server-sent-events-for-notifications`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** A one-way server notification use case MUST use server-sent events.

**Rationale:** A notification that flows one way, from server to browser, has no use for a bidirectional channel with groups and fallback. Server-sent events carry it over plain HTTP, which passes through proxies and firewalls without extra negotiation.

**Reviewed for:** Do all one-way notifications travel over server-sent events? Read the notification endpoints in code.

### Use SignalR for Interactive Messaging

**ID:** `litespec/ext/realtime#use-signalr-for-interactive-messaging`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** A use case requiring bidirectional messages, server groups, or transport fallback MUST use SignalR.

**Rationale:** Two-way messages, server-managed groups, and transport fallback need a channel built for interaction, which one-way events cannot provide. SignalR supplies all three, so the use case that needs them takes SignalR instead of forcing one-way events to carry interaction.

**Reviewed for:** Does every bidirectional use case run on SignalR? Read each use case's hub code.

### Record the Transport and Its Platform Support

**ID:** `litespec/ext/realtime#record-the-transport-and-its-platform-support`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** A realtime use case MUST record its transport choice and expected hosted-platform support.

**Rationale:** Hosted platforms cap connection duration, idle timeouts, and transport upgrades, and each host caps them differently. Recording the transport choice with its expected platform support reveals the mismatch before deployment instead of during the first long-lived connection.

**Reviewed for:** Does every realtime use case page name its transport choice and expected hosted-platform support? Read each use case page.

### Declare Realtime Freshness

**ID:** `litespec/ext/realtime#declare-realtime-freshness`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** A query served over realtime MUST declare a `freshness` object with `mode` of `immediate` on its output.

**Rationale:** A realtime channel delivers an update seconds after the change, so the served query declares that speed on its output as `{"mode": "immediate"}`. Without that declaration, a client reading the live value has no way to know how stale it may be.

**Reviewed for:** Does every realtime-served query declare immediate freshness on its output? Read the served query beside the realtime use case.

### Authenticate and Authorize Subscriptions

**ID:** `litespec/ext/realtime#authenticate-and-authorize-subscriptions`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** A realtime server MUST authenticate each connection and authorize every user-, tenant-, or resource-scoped subscription.

**Rationale:** A connection proves who the caller is; it says nothing about which user's, tenant's, or resource's updates the caller may hear. Each user-, tenant-, or resource-scoped subscription checks its own authorization, so one valid connection cannot receive another tenant's data.

**Reviewed for:** Does any connection test accept an unauthenticated connection, or a scoped subscription without an authorization check? Read the connection and subscription tests.

### Revalidate Long-Lived Access

**ID:** `litespec/ext/realtime#revalidate-long-lived-access`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** A realtime server MUST revalidate long-lived access according to token and session lifetime.

**Rationale:** A connection can outlive the grant that opened it: tokens expire, roles change, and sessions end. Revalidating access on token and session lifetime closes the connection whose grant ended instead of streaming updates to a caller who no longer qualifies.

**Reviewed for:** Does any connection keep streaming after its token or session expires? Read the revalidation tests.

### Reconnect and Refresh Authoritative State

**ID:** `litespec/ext/realtime#reconnect-and-refresh-authoritative-state`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** A realtime client MUST reconnect with bounded backoff and refresh authoritative state after a connection gap.

**Rationale:** Connections drop, and notifications sent during the gap are lost. Bounded backoff reconnects without overloading the server, and refreshing authoritative state after the gap replaces whatever the missed notifications would have changed.

**Reviewed for:** Does every client reconnect with bounded backoff and refresh authoritative state after a gap? Read the recovery tests.

### Tolerate Notification Ordering Differences

**ID:** `litespec/ext/realtime#tolerate-notification-ordering-differences`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** A realtime client MUST safely handle duplicate and out-of-order notifications.

**Rationale:** The transport can deliver a notification twice or deliver two of them out of order. A client that applies each notification as an absolute update corrupts its state on the first duplicate. A client built for repeats and reordering reaches the correct state either way.

**Reviewed for:** Does any duplicate or reordered notification leave client state wrong? Read the client state tests.

### Bound Realtime Resources

**ID:** `litespec/ext/realtime#bound-realtime-resources`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** A realtime deployment MUST set four setting-record bounds: concurrent connections per host, receive message size, parallel invocations per connection, and client timeout.

**Rationale:** Each open connection holds memory and each message costs CPU, so a flood of either starves the host. The deployment sets four bounds as setting records: concurrent connections per host, receive message size, parallel invocations per connection, and client timeout. Each value comes from the deployment's own connection count and message size and the observation behind it. [The platform's realtime security guidance](https://learn.microsoft.com/en-us/aspnet/core/signalr/security) names the same settings.

**Reviewed for:** Does each of the four deployment bounds have a setting record beside the deployment? Read the setting records and the deployment configuration.

### Reject Unbounded Slow-Client Buffering

**ID:** `litespec/ext/realtime#reject-unbounded-slow-client-buffering`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** A realtime server MUST NOT buffer an unbounded stream for a slow client.

**Rationale:** A client that reads slower than the server writes grows its buffer without end, consuming host memory meant for every subscriber. Bounded buffering with backpressure disconnects the slow client or slows delivery to it, so one slow reader does not starve the rest.

**Reviewed for:** Does any server code path buffer a slow client's stream without a bound or backpressure? Read the connection and send code.

## Conventions

### Place Realtime Code at Outer Boundaries

**ID:** `litespec/ext/realtime#place-realtime-code-at-outer-boundaries`

**Strength:** default

**Applies to:** artifact: record:notification

**Default:** Keep hubs or endpoints in WebApi, publication adapters in Infrastructure, and browser connections under `lib/realtime/`.

**Replacement:** A consumer records a realtime-placement convention naming where its transport code lives.

**Rationale:** Hubs, endpoints, publication adapters, and browser connections contain transport and provider detail, not business rules. Keeping them at the outer boundaries leaves each business feature owning only the narrow functions it calls.

**Reviewed for:** Does any realtime transport code sit outside the documented boundaries without a realtime-placement convention? Check the WebApi, Infrastructure, and `lib/realtime/` folders.

### Subscribe Through Narrow Functions

**ID:** `litespec/ext/realtime#subscribe-through-narrow-functions`

**Strength:** default

**Applies to:** artifact: record:notification

**Default:** Let feature modules subscribe through narrow use-case-specific functions.

**Replacement:** A consumer records a realtime-subscription convention naming how its features subscribe.

**Rationale:** A feature using the raw connection directly spreads subscription strings and payload shapes through business code. Narrow use-case-specific functions show each subscription's inputs and effects beside the feature that owns them.

**Reviewed for:** Does any feature subscribe to the raw connection instead of a use-case-specific function? Read each feature module's realtime calls.

## Dependencies

Packages: Selecting SignalR adds `@microsoft/signalr`. The project records a decision for the package and pins it in the manifest.

Related extensions: `outbox`.

Guides: None.
