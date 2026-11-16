# Read Another Module Through a Read Model

A module often needs to know something another module owns, such as what an order cost when a refund decides how much to return. This decision adds the read-model record. The record is a module's own copy of what it needs, filled from the integration events the owner publishes. Read models, screens that call each owner's query, and decision ports replace cross-module readers that return the owner's current state for any caller.

## Context

In 6.0.0 a module read another module's data through a read port in the owner's `Contracts/Ports/` folder. One large consumer had 86 such readers named in 741 files of other modules. The busiest reader had 18 methods, several named for their one caller. No record said which fields crossed the boundary. A personal field could travel to any reader.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| A read-model record each consumer owns, built from published integration events | Each field names its source, and a personal field names its erasure. | Projections to run, a lag up to the declared freshness, and events kept for rebuilds. |
| Keep read ports in the owner's Contracts | Always the current value. | Readers grow per caller, and every reader couples to the owner. |
| Share the owner's tables through database views | No events and no projections. | The owner's schema becomes a contract. |

## Decision

A `read-model` is a Behavior record at `behavior/modules/<module>/read-models/<name>/`, with the identifier `read-model/<module>.<name>`. It names the other module's aggregate in `keyedBy` and keeps one row per aggregate.

Each field names its source as `integration-event/<name>#<field>`. It may accumulate with `sum` or `count`. A personal or sensitive field names the event that erases it in `erasedBy`. `removedBy` names the events that remove a row. `freshness` states how stale a reader may see it. A use case names the read model in `reads`.

Three other answers cover what a read model does not. A screen that only shows another module's data calls each owner's own query and joins the answers. A command whose decision needs another module's current value asks one question through a decision port. Repositories, outside-system ports, and rule calculators stay ports.

## Consequences

`model check` reports `reference-unknown`, `read-model-erasure-missing`, `read-model-unused`, and `read-model-key-invalid`.

Code sits in a module `ReadModels/` folder, one folder per read model. Each folder holds the read-model document, one projection per source event, and the read model's reader.

The book shows each read model under its module with the events that fill it. Claimwick's reporting module carries a read model of reimbursed expenses.

With the outbox extension, every integration event a read model names is kept for as long as a read model may rebuild from it. A new read model fills from history instead of from the owner's tables.

## Evidence

[research/cross-module-coupling-practice](research/cross-module-coupling-practice) holds the event-carried state transfer and ports-and-adapters sources and the consumer counts.

## Revisit Conditions

A consumer shows a read model whose freshness no decision can accept and no decision port can answer. Or a consumer shows a rebuild that cannot replay the kept events.
