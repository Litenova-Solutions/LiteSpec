# Keep the UI Source Lock

The UI source lock is a file that records the digest of each component a project copies into its code. A digest is a fingerprint of the component's source, so a changed component changes its digest. This decision says who writes that file and how a change reaches review.

## Context

The UI source lock records the digest of each copied component, so a changed component shows in review. Teams compute those digests by hand, and no command writes them, although the check needs them to be exact.

## Options

LiteSpec compared 3 options for keeping the lock. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Keep the lock, written by `litespec model render` | A person acknowledges each digest change in the pull request diff, and the tool computes every digest the way the check does. | `litespec model render` writes one more file, and it reads the manifest baseline and the CLI pin to do so. |
| Keep the lock, copied by hand | No command changes, and the lock keeps its shape. | A person computes each digest, and a copy error passes until the check runs against it. |
| Remove the lock | No file to keep in step with the source. | A changed primitive passes unreported, so a forked component reads as unchanged. |

## Decision

`litespec model render` writes the lock. A person acknowledges a changed digest by committing it, and no person computes a digest.

## Consequences

`litespec model render` writes each component's digest from its paths. It writes the preset from the manifest baseline and the CLI version from the manifest pin. It keeps the names, addresses, statuses and dependencies that the team chose.

`litespec model check` remains the guard, and a changed digest shows in the pull request diff. The lock is `ui-source-lock.yaml`, in the folder that it held before.

## Revisit Conditions

This decision reopens when source changes arrive that a render cannot digest from the declared paths, or when the team stops copying primitives into one shared package.

A render cannot digest a change that the declared paths do not cover. The lock then keeps a digest that no longer matches the source, which is the case that would change the choice.
