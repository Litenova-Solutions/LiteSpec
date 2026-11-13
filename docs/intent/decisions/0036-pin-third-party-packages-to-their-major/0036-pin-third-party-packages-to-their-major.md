# Pin Third-Party Packages to Their Major

## Context

The manifest pinned each third-party package to an exact version, so a consumer waited for a LiteSpec release to take a patch, although its lockfile already held the exact version.

## Decision

The manifest states the major of each third-party package as `N.x`. Exact pins stay only where a check depends on exact output: the analyzer package, the UI baseline CLIs, and the Node and pnpm toolchain entries.
