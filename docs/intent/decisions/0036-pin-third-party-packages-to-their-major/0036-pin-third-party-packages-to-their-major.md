# Pin Third-Party Packages to Their Major

Third-party packages are libraries that other people publish and that LiteSpec projects depend on. A pin is the version a project declares for a package. This decision pins each package to its major version, written `N.x`, so a consumer takes patch releases without waiting for a LiteSpec release, while the lockfile still records the exact version.

## Context

The manifest pins each third-party package to an exact version. A consumer therefore waits for a LiteSpec release to take a patch, although its lockfile already holds the exact version.

## Options

LiteSpec compared 2 options for pinning packages. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Pin each major, and exact versions only where a check depends on them | A consumer takes a patch without a LiteSpec release, and the lockfile still records the exact version. | 2 consumers on one LiteSpec release can resolve different patch versions of one package. |
| Pin every package to an exact version | Every consumer on one LiteSpec release resolves identical versions. | A consumer waits for a LiteSpec release to take a patch, although its lockfile already holds the exact version. |

## Decision

The manifest states the major of each third-party package as `N.x`. An exact pin stays only where a check depends on exact output.

These exact pins stay: `LiteSpec.Analyzers`, the `shadcn` and `shadcn-svelte` CLIs, and the Node and pnpm toolchain entries. A check or the release reads their exact output.

## Consequences

`dependency-version-drift` reports a resolved version that falls outside the pinned major.

The consumer's central file or lockfile holds the exact version, under `litespec/workspace/dependencies#pin-every-dependency-version-centrally`.

## Revisit Conditions

This decision reopens when a patch inside a pinned major breaks a consumer that a LiteSpec check could have held back.

A patch of that kind reaches the consumer, because the pinned major allows it. A check that could hold it back would make the pin stricter for that package.
