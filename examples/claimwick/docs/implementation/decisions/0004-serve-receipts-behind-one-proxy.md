---
{
  "type": "decision",
  "id": "decision/0004-serve-receipts-behind-one-proxy",
  "title": "Serve Receipts Behind One Proxy",
  "owner": "claimwick-team",
  "status": "draft",
  "decider": "claimwick-team",
  "decideBy": "2027-02-01",
  "options": [
    {
      "name": "One Caddy proxy",
      "pros": "Receipts share one hardened entry point.",
      "cons": "The proxy is one more service to run."
    },
    {
      "name": "Direct object links",
      "pros": "Nothing new to run.",
      "cons": "Each client fetches receipts its own way."
    }
  ],
  "reopenedWhen": "Receipt traffic outgrows one proxy.",
  "basedOn": [
    "requirement/approval.month-end-submissions"
  ]
}
---

# Serve Receipts Behind One Proxy

## Context

Receipts need one hardened entry point before the pilot.

## Decision

Claimwick serves receipts behind one Caddy proxy.

## Consequences

Receipt traffic shares one entry point with one set of security headers.

## Evidence with Confidence

The team ran the proxy against the month-end load harness in November 2026 and held acknowledgements within two seconds.
