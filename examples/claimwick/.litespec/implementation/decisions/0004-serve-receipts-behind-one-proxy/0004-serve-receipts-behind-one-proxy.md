# Serve Receipts Behind One Proxy

Claimwick serves receipt files through one Caddy proxy. Receipts are the proof the tax file needs, so every client that fetches one reaches it through the same entry point. This decision chose that single entry point over direct links to each file, and it accepted one more service to run.

## Context

Receipts need one hardened entry point before the pilot. The team needs that entry point in place before the pilot starts.

The choice rests on `requirement/approval-reminders.month-end-submission`, and the team tested the proxy against a month-end load.

## Options

Two options were weighed on the same points: one Caddy proxy, and direct object links.

| Option | Advantage | Drawback |
|:---|:---|:---|
| One Caddy proxy | Receipts share one hardened entry point. | The proxy is one more service to run. |
| Direct object links | Nothing new to run. | Each client fetches receipts its own way. |

Direct links avoid one more service, but each client then fetches receipts in its own way. The proxy adds a service to run, and in return all receipt traffic passes through one entry point.

## Decision

Claimwick serves receipts behind one Caddy proxy.

The proxy is the single entry point for receipt traffic. The team hardens one place instead of hardening each client.

## Consequences

Receipt traffic shares one entry point and one set of security headers. The team takes on one more service to run.

## Evidence

The team ran the proxy against the month-end load harness in November 2026. The harness held acknowledgements within 2 seconds.

The record does not give the load size the harness used, and it gives no separate confidence level for this test.

## Revisit Conditions

The choice reopens when receipt traffic outgrows one proxy. At that point one proxy no longer carries all receipt traffic, so the choice needs a new look.
