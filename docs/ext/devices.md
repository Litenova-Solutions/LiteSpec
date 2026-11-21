# Connected Devices

**Stack:** any

**Concern:** frontend-ui

Entry points that serve connected devices carry a `protocol` block. It describes the messages the two sides exchange, the versions they speak, the rules devices apply on their own, and how offline uploads work.

This extension applies to entry points that serve scanners, kiosks, or other offline-capable clients with stored state, local rules, or offline sync.

## Activation

Activation scope: `local`.

Applicable specification kinds: `entry-point`.

Select `devices` for an entry point when devices reach it with stored state, local rules, or offline sync. Leave it unselected on a browser or API entry point with no device client.

## Baseline Relationship

This extension replaces no baseline provision and switches on no record type. It adds the `protocol` block to entry-point records. The block holds the messages exchanged, the version rule devices follow, the client rules with their scenarios, the nonce that makes retries idempotent, and the `force` and `simulate` flags on offline uploads.

## Standards

### Describe Device Messages as a Channel Fragment

**ID:** `litespec/ext/devices#describe-device-messages-as-a-channel-fragment`

**Strength:** required

**Applies to:** artifact: record:entry-point; layer: behavior

**Statement:** A device protocol MUST describe its messages as an AsyncAPI channel fragment with named operations and payloads.

**Rationale:** Devices and servers change on different schedules: a scanner can run last year's firmware against this week's server. A channel fragment names each direction, each message, and each payload in one shape both sides read. A mismatch then shows in the record before it affects deployed devices.

**Example:**

```yaml
protocol:
  channel: scanner/check-in
  publish:
    message: check-in.recorded
    payload: { code, nonce, scannedAt }
  subscribe:
    message: check-in.denied
    payload: { code, reason }
```

**Reviewed for:** Does every device entry point describe each operation with its payload in the channel shape? Read the protocol blocks on each device entry point.

### Version the Device Protocol

**ID:** `litespec/ext/devices#version-the-device-protocol`

**Strength:** required

**Applies to:** artifact: record:entry-point; layer: behavior

**Statement:** A device protocol MUST state its version rule with numbering, overlap, and the upgrade path.

**Rationale:** Old firmware cannot be recalled from the field, so several protocol versions stay live at once. Numbering tells a device which version it speaks, overlap tells the server how long to answer old versions, and the upgrade path tells the device how to move forward. Without the three, a server upgrade leaves every device on an old version unable to communicate.

**Reviewed for:** Does every protocol block state numbering, overlap, and the upgrade path? Read each protocol block.

### State Client Rules with Scenarios

**ID:** `litespec/ext/devices#state-client-rules-with-scenarios`

**Strength:** required

**Applies to:** artifact: record:entry-point; layer: behavior

**Statement:** Each client rule in a device protocol MUST carry the scenarios that prove it.

**Rationale:** Client rules run where the server cannot see them: validation a scanner applies offline, storage limits a kiosk enforces, and queueing a phone does without a signal. A rule without scenarios leaves that behavior unproven. Scenarios tie each rule to observable device behavior the product tests against.

**Example:** The rule `a scanner queues scans while offline` carries the scenario `a scan queued offline uploads on reconnect`.

**Reviewed for:** Does every client rule in the protocol blocks carry the scenarios that prove it? Read the protocol blocks beside the scenarios.

### Identify Retries with a Nonce

**ID:** `litespec/ext/devices#identify-retries-with-a-nonce`

**Strength:** required

**Applies to:** artifact: record:entry-point; layer: behavior

**Statement:** Each mutating device message MUST carry a client-generated nonce as its idempotency key.

**Rationale:** A device that loses signal mid-send retries a request the server already applied. The nonce gives the retry a stable identity scoped to the device, so the server answers the repeat from the first attempt instead of lending, paying, or shipping twice.

**Example:** A loan scan carries `nonce: 7f3a`, and the second delivery of the same nonce returns the first attempt's result.

**Reviewed for:** Does every mutating message in the protocol blocks carry a client-generated nonce scoped to the device? Read each message in the protocol blocks.

### Simulate Offline Uploads Before Applying

**ID:** `litespec/ext/devices#simulate-offline-uploads-before-applying`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** An offline upload with `simulate` MUST return its outcome without writing.

**Rationale:** A device back online after a week holds uploads that refer to state that changed while it was offline: prices changed, holds lapsed, windows closed. Simulating the queue shows what would happen before anything is written, so the operator reconciles first and applies second.

**Example:** A scanner uploads three hundred queued scans with `simulate`, reviews the two the server would refuse, and then uploads the queue to apply it.

**Reviewed for:** Does any simulated upload in the device tests write behind the outcome it returns? Read the device tests for simulated uploads.

### Apply Forced Uploads over Newer State

**ID:** `litespec/ext/devices#apply-forced-uploads-over-newer-state`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** An offline upload with `force` MUST apply over newer server state and record the overwrite.

**Rationale:** Sometimes the device holds the correct value and the server holds a stale one: a supervisor corrected the count on the floor while the server held the old one. Force lets the operator say so explicitly, and the recorded overwrite keeps the decision visible instead of hiding it in a silent overwrite.

**Example:** A corrected count uploads with `force`, overwrites the server value, and leaves an overwrite record naming the operator.

**Reviewed for:** Does every forced upload in the device tests overwrite newer server values and leave an overwrite record? Read the device tests for forced uploads.

## Conventions

### Version Protocols by Date

**ID:** `litespec/ext/devices#version-protocols-by-date`

**Strength:** default

**Applies to:** artifact: record:entry-point; layer: behavior

**Default:** Protocol versions use calendar numbering of the form `YYYY.N`.

**Replacement:** A project names its protocol numbering scheme in its device documentation.

**Rationale:** A calendar number tells a support engineer how old a device is at a glance. Any scheme works as long as every device and the server share it.

**Reviewed for:** Does every protocol version use calendar numbering, or does the device documentation name another scheme? Read the protocol version values beside that documentation.

### Replay Uploads in Queue Order

**ID:** `litespec/ext/devices#replay-uploads-in-queue-order`

**Strength:** default

**Applies to:** artifact: command-handler

**Default:** The server applies an offline queue in the order the device queued it.

**Replacement:** A project names the order its server applies offline queues.

**Rationale:** Queue order matches what the operator saw on the device: first scanned, first applied. Another order applies uploads in a different order than the operator saw, which makes the log hard to follow.

**Reviewed for:** Does every upload test apply offline queues in device order, or has the project named another order? Read the upload tests beside the project's documented order.

## Dependencies

Packages: None.

Related extensions: `concurrency` for the nonce idempotency keys and overlapping-write handling.

Guides: None.
