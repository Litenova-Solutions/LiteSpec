# Overview

## Intent

The Quality pages state what a release proves beyond its behavior: who may act and what happens to sensitive data, how the product runs, which checks each pull request runs, and how each scenario is proved. Each page applies to a project that claims the record layer its rules govern, as the layer table below states. A project that operates in production adds the [Delivery extension](../ext/delivery.md) for its release gate, deployment, recovery and alerting regime.

## How the Pages Fit Together

[Security](security.md) answers who acts, what each operation touches, and what it keeps; [Operations](operations.md) states how the product runs after release; [Continuous Integration](ci.md) states the gates each pull request runs for its changed areas; [Testing](testing.md) states the cross-stack rules that prove each scenario. Read them in that order when setting up a new consumer: the product boundary first, then its operation, then the checks and proofs that guard both.

## Layer Claims

Each Quality page gives its rules one record layer, and a rule applies when the project claims that layer, as `layers` in `.litespec/project.yaml` states:

| Page | Layer its rules govern | Claim that applies it |
|:---|:---|:---|
| [Security](security.md) | Implementation, with access rules as Behavior and the ASVS target as Intent | `implementation`, `behavior`, `intent` |
| [Operations](operations.md) | Implementation | `implementation` |
| [Continuous Integration](ci.md) | Implementation | `implementation` |
| [Testing](testing.md) | Proof | `proof` |

A layer claimed as `own` leaves its LiteSpec rules unapplied, except the rules that state `trace: yes`, as the [layer claims](../core/scope.md#layer-claims) section states.

## Pages

- [Security](security.md): identity, permissions, data classification, and trust boundaries.
- [Operations](operations.md): observability, diagnostics, and support by one maintainer.
- [Continuous Integration](ci.md): the gates each pull request runs for its changed areas.
- [Testing](testing.md): the cross-stack rules that prove each scenario through the public boundary, the one mock rule, and the test projects each size requires.
- [Delivery](../ext/delivery.md): the opt-in production regime: release readiness, deployment and rollback, backup and restore, alerting and the supply-chain gate.
