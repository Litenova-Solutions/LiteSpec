# Realtime Updates

## Intent

Realtime updates push a change to open screens seconds after it happens, faster than polling delivers at an acceptable cost. This page states when a team replaces polling with realtime transport, which transport fits which need, and which protections each connection carries: authentication, revalidation, recovery, and resource bounds. A team selects it for each use case or journey after measuring polling against the update target, so the product pays for open connections only where a measured target requires live data.

## Activation

Activation scope: `local`.

Applicable specification kinds: `use-case`, `journey`.

A use case or journey selects this extension after the team measures polling against the update target: the polling interval, the visible delay, the request cost, and the required update speed. The selection also names the accepted cost of disconnects and missed messages: how the client recovers when the connection drops or a notification never arrives.

## Baseline Relationship

This extension replaces no baseline provision. One-way notifications travel over server-sent events, which need no added package because the platform ships them.

Selecting `realtime` switches on the notification and setting record types: each pushed update is a notification record, and each resource bound lives in a setting record.

## Standards

### Record Realtime Need

**ID:** `litespec/ext/realtime#record-realtime-need`

**Statement:** A realtime use case MUST document polling behavior, measured cost, update latency, expected connections, and recovery behavior.

**Rationale:** Open connections cost memory, CPU, and operating attention on every host, so the team writes down what polling costs, what the update target requires, and how many connections to expect before choosing transport. The use case page carries that record with its owner and status, so a later reader sees which measurements justified the choice.

**Reviewed for:** The use case page holds the polling record, measured cost, update target, expected connections, and recovery behavior.

### Reject Cosmetic Realtime Adoption

**ID:** `litespec/ext/realtime#reject-cosmetic-realtime-adoption`

**Statement:** A product MUST NOT activate realtime only to make an interface feel immediate.

**Rationale:** Realtime transport that serves only a feeling of immediacy spends connection capacity and operating attention without moving any target. The extension asks for a measured update requirement first, so polling stays where polling suffices.

**Reviewed for:** The realtime decision cites a measured update requirement.

### Use Server-Sent Events for Notifications

**ID:** `litespec/ext/realtime#use-server-sent-events-for-notifications`

**Statement:** A one-way server notification use case MUST use server-sent events.

**Rationale:** A notification that flows one way, from server to browser, needs no bidirectional channel with groups and fallback. Server-sent events carry it over plain HTTP, which passes through proxies and firewalls without extra negotiation.

**Reviewed for:** Each one-way notification travels over the server-sent events boundary in code.

### Use SignalR for Interactive Messaging

**ID:** `litespec/ext/realtime#use-signalr-for-interactive-messaging`

**Statement:** A use case requiring bidirectional messages, server groups, or transport fallback MUST use SignalR.

**Rationale:** Two-way messages, server-managed groups, and transport fallback need a channel built for interaction, which one-way events cannot provide. SignalR supplies all three, so the use case that needs them takes SignalR instead of stretching events past their shape.

**Reviewed for:** Each bidirectional use case runs on SignalR groups, messages, or fallback in code.

### Record Transport Support

**ID:** `litespec/ext/realtime#record-transport-support`

**Statement:** A realtime use case MUST record its transport choice and expected hosted-platform support.

**Rationale:** Hosted platforms limit connection duration, idle timeouts, and transport upgrades in ways that differ per host. Recording the transport choice with its expected platform support surfaces the mismatch before deployment instead of during the first long-lived connection.

**Reviewed for:** The use case page names its transport choice with the expected hosted-platform support.

### Declare Realtime Freshness

**ID:** `litespec/ext/realtime#declare-realtime-freshness`

**Statement:** A query served over realtime MUST declare a `freshness` object with `mode` of `immediate` or `advisory` on its output.

**Rationale:** A realtime channel delivers an update seconds after the change, so the served query promises `{"mode": "immediate"}`; where the pushed value still awaits a later decision, it promises `{"mode": "advisory", "authority": "<record id>"}` naming the record that holds the authoritative value. Without the declared freshness, a reader cannot tell whether the live value is final or provisional.

**Reviewed for:** Which realtime-served queries lack an immediate or advisory freshness object on their output? Look at the served query beside the realtime use case.

### Authenticate and Authorize Subscriptions

**ID:** `litespec/ext/realtime#authenticate-and-authorize-subscriptions`

**Statement:** A realtime server MUST authenticate each connection and authorize every user-, tenant-, or resource-scoped subscription.

**Rationale:** Connecting proves who the caller is; it says nothing about which user's, tenant's, or resource's updates the caller may hear. Each user-, tenant-, or resource-scoped subscription checks its own authorization, so one valid connection cannot listen in on another tenant's data.

**Reviewed for:** The connection tests show authenticated connections only, with each scoped subscription authorized.

### Revalidate Long-Lived Access

**ID:** `litespec/ext/realtime#revalidate-long-lived-access`

**Statement:** A realtime server MUST revalidate long-lived access according to token and session lifetime.

**Rationale:** A connection can outlive the grant that opened it: tokens expire, roles change, sessions end. Revalidating access on token and session lifetime closes the connection whose grant died instead of streaming updates to a caller who no longer qualifies.

**Reviewed for:** A connection whose token or session expired loses access in the revalidation tests.

### Reconnect and Refresh Authoritative State

**ID:** `litespec/ext/realtime#reconnect-and-refresh-authoritative-state`

**Statement:** A realtime client MUST reconnect with bounded backoff and refresh authoritative state after a connection gap.

**Rationale:** Connections drop, and a notification sent during the gap never arrives. Bounded backoff reconnects without hammering the server, and refreshing authoritative state after the gap replaces whatever the missed notifications would have changed.

**Reviewed for:** The client reconnects with bounded backoff and refreshes authoritative state after a gap in the recovery tests.

### Tolerate Notification Ordering Differences

**ID:** `litespec/ext/realtime#tolerate-notification-ordering-differences`

**Statement:** A realtime client MUST safely handle duplicate and out-of-order notifications.

**Rationale:** The transport can deliver a notification twice or deliver two of them out of order. A client that applies each notification as an absolute update corrupts its state on the first duplicate; a client built for repeats and reordering converges on the truth either way.

**Reviewed for:** Duplicate and reordered notifications leave client state correct in the client tests.

### Bound Realtime Resources

**ID:** `litespec/ext/realtime#bound-realtime-resources`

**Statement:** A realtime deployment MUST set four setting-record bounds: concurrent connections per host, receive message size, parallel invocations per connection, and client timeout.

**Rationale:** Each open connection holds memory, each message costs CPU, and a flood of either starves the host. The deployment sets four bounds as setting records - concurrent connections per host, receive message size, parallel invocations per connection, and client timeout - with each value drawn from the deployment's own connection count and message size and the observation that produced it. [The platform's realtime security guidance](https://learn.microsoft.com/en-us/aspnet/core/signalr/security) names the same settings.

**Reviewed for:** The four deployment bounds live as setting records with their values beside the deployment.

### Reject Unbounded Slow-Client Buffering

**ID:** `litespec/ext/realtime#reject-unbounded-slow-client-buffering`

**Statement:** A realtime server MUST NOT buffer an unbounded stream for a slow client.

**Rationale:** A client that reads slower than the server writes grows its buffer without end, consuming host memory meant for every subscriber. Bounded buffering with backpressure disconnects or slows the laggard instead of letting one slow reader starve the rest.

**Reviewed for:** Slow-client buffering in the server code is bounded with backpressure.

## Conventions

### Place Realtime Code at Outer Boundaries

**ID:** `litespec/ext/realtime#place-realtime-code-at-outer-boundaries`

**Default:** Keep hubs or endpoints in WebApi, publication adapters in Infrastructure, and browser connections under `lib/realtime/`.

**Replacement:** A consumer records a realtime-placement convention naming where its transport code lives.

**Rationale:** Hubs, endpoints, publication adapters, and browser connections deal in transport and provider detail, not business rules. Keeping them at the outer boundaries leaves business features owning only the narrow functions they call.

**Reviewed for:** Realtime transport code sits at the documented boundaries, or the realtime-placement convention names where it lives.

### Subscribe Through Narrow Functions

**ID:** `litespec/ext/realtime#subscribe-through-narrow-functions`

**Default:** Let feature modules subscribe through narrow use-case-specific functions.

**Replacement:** A consumer records a realtime-subscription convention naming how its features subscribe.

**Rationale:** A feature that touches the raw connection spreads subscription strings and payload shapes through business code. Narrow use-case-specific functions show each subscription's inputs and effects beside the feature that owns them.

**Reviewed for:** Features subscribe through use-case-specific functions, or the realtime-subscription convention names how they subscribe.

## Dependencies

Packages: Selecting SignalR adds `@microsoft/signalr`. The project records a decision for the package and pins it in the manifest.

Related extensions: None.

Guides: None.
