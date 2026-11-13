# Remove Only the Timing Variable

## Context

The plan asked the owner to decide three items after a usage check: the `LITESPEC_TIMING` variable, the `book --editor` option and the www redirect Worker. Nothing in CI or the docs sets the variable, and no test covers it.

## Decision

The variable is removed. The editor option and the redirect Worker stay as they are.
