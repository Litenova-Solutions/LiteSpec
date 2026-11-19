# Retention Holds and Rounded Starts

Deletion pauses while a hold stands, and a retention period counts from an event rounded up to a calendar end. This decision adds `retentionHolds` to the data class and an event-with-rounding form to `periodStartsAt`.

## Context

Data classes declared a period with its start, but no hold. A tax audit, an objection, or a dispute had nowhere to pause deletion, so a product that deleted on schedule during an audit broke the law while following its records exactly. Every product with legal retention would reinvent the hold on its own.

The start had the second gap: a period could start only at a date-like event. The law starts counting when a record stops being current and rounds up to the end of the year, and the records showed no way to state both halves.

## Options

LiteSpec compared 2 options for retention holds and starts. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Hold deletion on the data class, and start a period from an event with rounding | The hold sits beside the retention it pauses with its reason and dates, and the start names its event and rounding in one field. | Enforcement reads the holds beside the period it carries out. |
| Hold deletion in a separate record | Holds stand apart from the classes they pause. | Each hold links back to its class, and readers check two places for one retention. |

## Decision

A data class carries `retentionHolds` beside its period. Each hold states its reason with the date it starts in `heldOn`, and deletion pauses until the hold lifts with its date in `liftedOn`.

`periodStartsAt` takes any declared event or aggregate date field, or an event or field with `rounding` to a calendar end. The event starts the count and the rounding shares one expiry across the year.

## Consequences

A data class carries holds beside its period, each with its reason, its `heldOn` date, and its `liftedOn` date once lifted, and deletion pauses while a hold stands. `periodStartsAt` takes any declared event or aggregate date field, or an event or field with rounding to a calendar end.

The new provisions are `litespec/core/privacy#pause-deletion-while-a-hold-stands` and `litespec/core/privacy#round-an-event-start-to-a-calendar-end`.

## Revisit Conditions

This decision reopens when a hold needs behavior a dated entry fails to cover, such as approval by a second person.

That case means the entry no longer holds what enforcement needs. The team then decides again how holds are declared.
