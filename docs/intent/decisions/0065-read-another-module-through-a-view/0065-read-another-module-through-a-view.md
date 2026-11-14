# Read Another Module Through a View

A module often needs to know something another module owns, such as what an order cost when a refund decides how much to return. This decision adds the view record: a module's own copy of what it needs, filled from the integration events the owner publishes. Cross-module readers that return the owner's current state for any caller give way to views, screens that call each owner's query, and decision ports.

## Context

In 6.0.0 a module read another module's data through a read port in the owner's `Contracts/Ports/` folder. One large consumer had 86 such readers named in 741 files of other modules, and the busiest reader had 18 methods, several named for their one caller. No record said which fields crossed the boundary, and a personal field could travel to any reader.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| A view record each consumer owns, built from published integration events | Each field names its source, and a personal field names its erasure. | Projections to run, a lag up to the declared freshness, and events kept for rebuilds. |
| Keep read ports in the owner's Contracts | Always the current value. | Readers grow per caller, and every reader couples to the owner. |
| Share the owner's tables through database views | No events and no projections. | The owner's schema becomes a contract. |

## Decision

A `view` is a Behavior record at `behavior/modules/<module>/views/<name>/`, with the identifier `view/<module>.<name>`. It names the other module's aggregate it keeps one row per in `keyedBy`. Each field names its source as `integration-event/<name>#<field>`, and may accumulate with `sum` or `count`. A personal or sensitive field names the event that erases it in `erasedBy`. `removedBy` names the events that remove a row, and `freshness` states how stale a reader may see it. A use case names the view in `reads`.

Three other answers cover what a view does not. A screen that only shows another module's data calls each owner's own query and joins the answers. A command whose decision needs another module's current value asks one question through a decision port. Repositories, outside-system ports and rule calculators stay ports.

## Consequences

`model check` reports `view-source-unknown`, `view-erasure-missing`, `view-unused` and `view-key-invalid`. Code sits in a module `Views/` folder, one folder per view, holding the view document, one projection per source event and the view's reader. The book shows each view under its module with the events that feed it. Claimwick's reporting module carries a view of reimbursed expenses.

With the outbox extension, every integration event a view names is kept for as long as a view may rebuild from it, so a new view fills from history instead of from the owner's tables.

## Evidence

[research/cross-module-coupling-practice](research/cross-module-coupling-practice) holds the event-carried state transfer and ports-and-adapters sources and the consumer counts.

## Revisit Conditions

A consumer shows a view whose freshness no decision can accept and no decision port can answer. Or a consumer shows a rebuild that cannot replay the kept events.
