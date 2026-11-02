# Overview

## Intent

The Quality pages state what every release proves beyond its behavior: who may act and what happens to sensitive data, how the product runs, and which checks each pull request runs. Every project applies them, from the first pull request to production operation. A project that operates in production adds the [Delivery extension](../ext/delivery.md) for its release gate, deployment, recovery and alerting regime.

## How the Pages Fit Together

[Security](security.md) answers who acts, what each operation touches, and what it keeps; [Operations](operations.md) states how the product runs after release; [Continuous Integration](ci.md) states the gates each pull request runs for its changed areas. Read them in that order when setting up a new consumer: the product boundary first, then its operation, then the checks that guard both.

## Pages

- [Security](security.md): identity, permissions, data classification, and trust boundaries.
- [Operations](operations.md): observability, diagnostics, and support by one maintainer.
- [Continuous Integration](ci.md): the gates each pull request runs for its changed areas.
- [Delivery](../ext/delivery.md): the opt-in production regime: release readiness, deployment and rollback, backup and restore, alerting and the supply-chain gate.
