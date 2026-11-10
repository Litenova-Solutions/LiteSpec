# Live Delivery

**Stack:** any

**Concern:** infrastructure

## Intent

Live updates reach open screens in seconds through a realtime transport the browser keeps open. This page states when the product replaces polling with realtime transport, which transport carries each kind of live update, and how connections, subscriptions, and clients stay safe under load and failure. A team selects it after it measures polling against an update target and accepts the cost of disconnects and missed messages.

## Activation

Activation scope: `local`.

Applicable specification kinds: `use-case`, `journey`.

A use case or journey selects this extension for live updates after the team measures polling against the update target: the polling interval, the visible delay, the request cost, and the required update speed. The selection also names the accepted cost of disconnects and missed messages: how the client recovers when the connection drops or a notification never arrives.

## Baseline Relationship

This extension replaces no baseline provision.

This extension switches on no optional record type. Each pushed update is a notification record, and each resource bound lives in a setting record.

One-way notifications travel over server-sent events, which need no added package because the platform ships them.

## Standards

### Record Realtime Need

**ID:** `litespec/ext/realtime#record-realtime-need`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** A realtime use case MUST document polling behavior, measured cost, update latency, expected connections, and recovery behavior.

**Rationale:** Open connections cost memory, CPU, and operating attention on every host, so the team writes down what polling costs, what the update target requires, and how many connections to expect before choosing transport. The use case page carries that record, so a later reviewer sees which measurements justified the choice.

**Reviewed for:** Which realtime use case pages lack the polling record, measured cost, update target, expected connections, or recovery behavior? Look at each realtime use case page.

### Reject Cosmetic Realtime Adoption

**ID:** `litespec/ext/realtime#reject-cosmetic-realtime-adoption`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** A product MUST NOT activate realtime only to make an interface feel immediate.

**Rationale:** Realtime transport that serves only a feeling of immediacy spends connection capacity and operating attention without moving any target. The extension asks for a measured update requirement first, so polling stays where polling suffices.

**Reviewed for:** Which realtime decisions cite no measured update requirement? Look at the use case page beside each realtime transport.

### Use Server-Sent Events for Notifications

**ID:** `litespec/ext/realtime#use-server-sent-events-for-notifications`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** A one-way server notification use case MUST use server-sent events.

**Rationale:** A notification that flows one way, from server to browser, needs no bidirectional channel with groups and fallback. Server-sent events carry it over plain HTTP, which passes through proxies and firewalls without extra negotiation.

**Reviewed for:** Which one-way notifications travel over a boundary other than server-sent events? Look at the notification endpoints in code.

### Use SignalR for Interactive Messaging

**ID:** `litespec/ext/realtime#use-signalr-for-interactive-messaging`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** A use case requiring bidirectional messages, server groups, or transport fallback MUST use SignalR.

**Rationale:** Two-way messages, server-managed groups, and transport fallback need a channel built for interaction, which one-way events cannot provide. SignalR supplies all three, so the use case that needs them takes SignalR instead of stretching events past their shape.

**Reviewed for:** Which bidirectional use cases run without SignalR groups, messages, or fallback? Look at each use case's hub code.

### Record Transport Support

**ID:** `litespec/ext/realtime#record-transport-support`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** A realtime use case MUST record its transport choice and expected hosted-platform support.

**Rationale:** Hosted platforms limit connection duration, idle timeouts, and transport upgrades in ways that differ per host. Recording the transport choice with its expected platform support surfaces the mismatch before deployment instead of during the first long-lived connection.

**Reviewed for:** Which realtime use case pages name no transport choice or expected hosted-platform support? Look at each use case page.

### Declare Realtime Freshness

**ID:** `litespec/ext/realtime#declare-realtime-freshness`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** A query served over realtime MUST declare a `freshness` object with `mode` of `immediate` or `advisory` on its output.

**Rationale:** A realtime channel delivers an update seconds after the change, so the served query promises `{"mode": "immediate"}`; where the pushed value still awaits a later decision, it promises `{"mode": "advisory", "authority": "<record id>"}` naming the record that holds the authoritative value. Without the declared freshness, a client cannot tell whether the live value is final or provisional.

**Reviewed for:** Which realtime-served queries lack an immediate or advisory freshness object on their output? Look at the served query beside the realtime use case.

### Authenticate and Authorize Subscriptions

**ID:** `litespec/ext/realtime#authenticate-and-authorize-subscriptions`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** A realtime server MUST authenticate each connection and authorize every user-, tenant-, or resource-scoped subscription.

**Rationale:** Connecting proves who the caller is; it says nothing about which user's, tenant's, or resource's updates the caller may hear. Each user-, tenant-, or resource-scoped subscription checks its own authorization, so one valid connection cannot listen in on another tenant's data.

**Reviewed for:** Which connection tests accept an unauthenticated connection, or a scoped subscription with no authorization check? Look at the connection and subscription tests.

### Revalidate Long-Lived Access

**ID:** `litespec/ext/realtime#revalidate-long-lived-access`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** A realtime server MUST revalidate long-lived access according to token and session lifetime.

**Rationale:** A connection can outlive the grant that opened it: tokens expire, roles change, sessions end. Revalidating access on token and session lifetime closes the connection whose grant died instead of streaming updates to a caller who no longer qualifies.

**Reviewed for:** Which connections keep streaming after their token or session expires? Look at the revalidation tests.

### Reconnect and Refresh Authoritative State

**ID:** `litespec/ext/realtime#reconnect-and-refresh-authoritative-state`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** A realtime client MUST reconnect with bounded backoff and refresh authoritative state after a connection gap.

**Rationale:** Connections drop, and a notification sent during the gap never arrives. Bounded backoff reconnects without hammering the server, and refreshing authoritative state after the gap replaces whatever the missed notifications would have changed.

**Reviewed for:** Which clients reconnect without bounded backoff, or skip the authoritative refresh after a gap? Look at the recovery tests.

### Tolerate Notification Ordering Differences

**ID:** `litespec/ext/realtime#tolerate-notification-ordering-differences`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** A realtime client MUST safely handle duplicate and out-of-order notifications.

**Rationale:** The transport can deliver a notification twice or deliver two of them out of order. A client that applies each notification as an absolute update corrupts its state on the first duplicate; a client built for repeats and reordering converges on the truth either way.

**Reviewed for:** Which duplicate or reordered notifications leave client state wrong? Look at the client state tests.

### Bound Realtime Resources

**ID:** `litespec/ext/realtime#bound-realtime-resources`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** A realtime deployment MUST set four setting-record bounds: concurrent connections per host, receive message size, parallel invocations per connection, and client timeout.

**Rationale:** Each open connection holds memory, each message costs CPU, and a flood of either starves the host. The deployment sets four bounds as setting records - concurrent connections per host, receive message size, parallel invocations per connection, and client timeout - with each value drawn from the deployment's own connection count and message size and the observation that produced it. [The platform's realtime security guidance](https://learn.microsoft.com/en-us/aspnet/core/signalr/security) names the same settings.

**Reviewed for:** Which of the four deployment bounds lack a setting record beside the deployment? Look at the setting records and the deployment configuration.

### Reject Unbounded Slow-Client Buffering

**ID:** `litespec/ext/realtime#reject-unbounded-slow-client-buffering`

**Strength:** required

**Applies to:** artifact: record:notification

**Statement:** A realtime server MUST NOT buffer an unbounded stream for a slow client.

**Rationale:** A client that reads slower than the server writes grows its buffer without end, consuming host memory meant for every subscriber. Bounded buffering with backpressure disconnects or slows the laggard instead of letting one slow reader starve the rest.

**Reviewed for:** Which server code paths buffer a slow client's stream without a bound or backpressure? Look at the connection and send code.

## Conventions

### Place Realtime Code at Outer Boundaries

**ID:** `litespec/ext/realtime#place-realtime-code-at-outer-boundaries`

**Strength:** default

**Applies to:** artifact: record:notification

**Default:** Keep hubs or endpoints in WebApi, publication adapters in Infrastructure, and browser connections under `lib/realtime/`.

**Replacement:** A consumer records a realtime-placement convention naming where its transport code lives.

**Rationale:** Hubs, endpoints, publication adapters, and browser connections deal in transport and provider detail, not business rules. Keeping them at the outer boundaries leaves business features owning only the narrow functions they call.

**Reviewed for:** Which realtime transport code sits outside the documented boundaries, with no realtime-placement convention? Look at the WebApi, Infrastructure, and `lib/realtime/` folders.

### Subscribe Through Narrow Functions

**ID:** `litespec/ext/realtime#subscribe-through-narrow-functions`

**Strength:** default

**Applies to:** artifact: record:notification

**Default:** Let feature modules subscribe through narrow use-case-specific functions.

**Replacement:** A consumer records a realtime-subscription convention naming how its features subscribe.

**Rationale:** A feature that touches the raw connection spreads subscription strings and payload shapes through business code. Narrow use-case-specific functions show each subscription's inputs and effects beside the feature that owns them.

**Reviewed for:** Which features subscribe to the raw connection instead of a use-case-specific function? Look at each feature module's realtime calls.

## Dependencies

Packages: Selecting SignalR adds `@microsoft/signalr`. The project records a decision for the package and pins it in the manifest.

Related extensions: `outbox`.

Guides: None.
