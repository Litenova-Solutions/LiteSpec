# Remove Only the Timing Variable

LiteSpec's tools have three items that a usage check flagged: the `LITESPEC_TIMING` variable, the `book --editor` option and the www redirect Worker. This decision removes the variable, because nothing uses it, and it keeps the other two items. A person who set the variable for local timing loses that switch.

## Context

A usage check leaves three items for the owner to decide: the `LITESPEC_TIMING` variable, the `book --editor` option and the www redirect Worker. Nothing in CI or the docs sets the variable, and no test covers it.

## Options

LiteSpec compared 3 options for the three items. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Remove `LITESPEC_TIMING`, and keep `book --editor` and the www redirect Worker | Nothing in CI or the docs sets the variable, and no test covers it. | A person who set the variable for local timing loses the switch. |
| Keep all three | No change for any caller. | A switch with no test and no user reads as a feature that the tools honor. |
| Remove all three | The smallest surface. | It reverses the decision the owner accepted for the editor option and the redirect Worker. |

## Decision

The variable is removed. The editor option and the redirect Worker stay as they are.

## Consequences

`LITESPEC_TIMING` leaves the code and the docs. `book --editor` stays. The www redirect Worker stays as it is, and any change to its deploy waits for the owner.

## Revisit Conditions

This decision reopens when a person needs the timing output and no other tool gives it, or when the owner asks for the redirect Worker to be removed.

A person who needs the timing output has no switch for it after this change. The owner's request would reopen the Worker item on its own.
