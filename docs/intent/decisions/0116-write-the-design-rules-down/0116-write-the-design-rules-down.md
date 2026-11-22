# Write the Design Rules Down

LiteSpec states its own design rules as provisions on the page `docs/core/designing.md`, and the agent protocol summarizes them. A maintainer or an agent changing the record model follows those rules, and states a departure as a departure.

## Context

LiteSpec's design rules lived only in its decisions. Each new record type or field decided again when a record gets its own file, what a type must ship with, and how a shape changes. The answers drifted: requirements moved into folders while rules, settings and domain events stayed in sets for the same reasons.

The agent protocol stated how the standard reads, not how its record model is designed. An agent changing LiteSpec had no rule to follow and no way to tell an exception from a pattern.

## Options

LiteSpec compared 3 options. The table sets each against what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Leave the design rules in the decisions | No new page, and each decision keeps its full reasoning. | A maintainer reads over a hundred decisions to find a rule, and nothing marks a departure. |
| List the design rules in the agent protocol only | Every agent reads them before every change. | A rule there has no identifier, strength or review question, so nothing can cite or check it. |
| State the design rules as provisions on one page, and summarize them in the protocol | Each rule is cited and reviewed like every other provision, and the page cites rules other pages own instead of copying them. | One more page to keep, and the protocol summary follows it. |

## Decision

The page `docs/core/designing.md` holds the design rules no other page owned. Each record holds one thing, and a record with its own reasoning gets its own folder. A shape is ready for its next field, every type, field and value has a reader, and a record type ships complete. A shape changes everywhere at once, a deviation is stated, and a rule that keeps bending is amended.

The page also lists the provisions other pages own that a designer follows, by subject.

## Consequences

The agent protocol gains a Designing LiteSpec section that summarizes the rules and cites them. A decision that departs from a design rule names the provision and the reason.

## Revisit Conditions

Reopen this when the page restates provisions other pages own, or when decisions keep setting its rules aside without amending them.
