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
      "for": "Receipts share one hardened entry point.",
      "against": "The proxy is one more service to run."
    },
    {
      "name": "Direct object links",
      "for": "Nothing new to run.",
      "against": "Each client fetches receipts its own way."
    }
  ],
  "changeWhen": "Receipt traffic outgrows one proxy.",
  "basedOn": [
    "requirement/spending.month-end-submissions"
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
