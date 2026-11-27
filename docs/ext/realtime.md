# Live Delivery

**Stack:** any

**Concern:** infrastructure

Live updates reach open screens in seconds over a realtime transport the browser keeps open. This page is for teams that measured polling against an update target and accepted the cost of disconnects and missed messages. Pick the transport by direction, bound the resources, and prove recovery before the first release.

## Activation

Activation scope: `local`.

Applicable specification kinds: `use-case`, `flow`.

A use case or flow selects this extension for live updates after the team measures polling against the update target: the polling interval, the visible delay, the request cost, and the required update speed. The selection also records the accepted cost of disconnects and missed messages: how the client recovers when the connection drops or a notification never arrives.

## Baseline Relationship

This extension replaces no baseline provision. It switches on no optional record type: each pushed update is a notification record, and each resource bound is a setting record.

One-way notifications travel over server-sent events, which need no added package because the platform ships them.

### Measure First, Then Pick the Transport

Each realtime use case documents the polling baseline beside the choice: polling behavior, measured cost, update target, expected connections, and recovery behavior. A decision with no measured update requirement behind it spends connection capacity and operating attention for the look of speed. Polling stays where polling meets the target.

One-way updates from server to browser travel over server-sent events over plain HTTP, which passes through proxies and firewalls without extra negotiation. Two-way messages, server-managed groups, and transport fallback call for SignalR, built for interaction that one-way events fail to carry. The use case page records the transport choice with its expected hosted-platform support, since hosts cap connection duration, idle timeouts, and transport upgrades in different ways.

### Freshness, Access, and Recovery

A query served over realtime declares its speed on its output as `{"mode": "immediate"}`. The declaration tells each client reading the live value how current the value is.

Each connection passes authentication, and every user-scoped, tenant-scoped, or resource-scoped subscription passes its own authorization check. One valid connection opens the door; the subscription check decides which rooms the caller enters, so one tenant hears nothing from another. Access passes revalidation on token and session lifetime, closing the connection whose grant ended instead of streaming to a caller who no longer qualifies.

Each client reconnects with bounded backoff and refreshes authoritative state after a gap, replacing whatever missed notifications carried. Each client also absorbs repeats and reordering, reaching the correct state whether a notification arrives twice or two of them swap places. A client applying each notification as an absolute overwrite corrupts its state on the first duplicate.

### Bounds and Placement

Each deployment records four setting bounds: concurrent connections per host, receive message size, parallel invocations per connection, and client timeout. Each value comes from the deployment's own connection count and message size with the observation behind it. No server buffers an unbounded stream for a slow client; bounded buffering with backpressure disconnects the slow reader or slows delivery to it, so one slow reader starves nothing else.

Transport code lives at the outer boundaries: hubs or endpoints in WebApi, publication adapters in Infrastructure, and browser connections under `lib/realtime/`. Feature code reaches the connection through narrow use-case-specific functions, keeping subscription strings and payload shapes beside the feature that owns them instead of spreading raw connection calls through business code.

## Standards

None.

## Conventions

None.

## Dependencies

Packages: Selecting SignalR adds `@microsoft/signalr`. The project records a decision for the package and pins it in the manifest.

Related extensions: `outbox`.

Guides: None.
