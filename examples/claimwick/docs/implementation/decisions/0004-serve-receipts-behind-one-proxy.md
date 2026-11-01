# Serve Receipts Behind One Proxy

## Context

Receipts need one hardened entry point before the pilot.

## Decision

Claimwick serves receipts behind one Caddy proxy.

## Evidence with Confidence

The team ran the proxy against the month-end load harness in November 2026 and held acknowledgements within two seconds.
