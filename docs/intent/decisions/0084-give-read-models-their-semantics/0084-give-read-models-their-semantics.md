# Give Read Models Their Semantics

A module that reads another module's state keeps a read model: its own copy for queries, filled from published integration events. This decision states the event semantics behind each copy: identity, ordering, corrections, rebuilds, erasure, and the decisions a copy never guards.

## Context

[decision/0065-read-another-module-through-a-read-model](decision/0065-read-another-module-through-a-read-model) added the record but left its event semantics open. Nothing said how redelivery counts, what latest compares, how corrections arrive, what a rebuild replays, or where staleness is measured from. Erasure had no tombstone, so a rebuild could restore erased fields. Nothing said whether a read model may guard a decision needing current truth.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| Identity, versions, tombstones, no guard role | Exact counts; erasure survives; truth stays owned. | Identity per event; tombstones per erased key. |
| At-least-once copies with arrival order | No identity work. | Double counts; rebuilds restore erased fields. |
| Let read models guard decisions | Fewer decision ports. | Revocations and locks decide on stale data. |

## Decision

Every source event carries an identity and redelivery never counts twice. `latest` compares source versions, not arrival. Corrections arrive as events. A rebuild replays retained events from the start. Staleness is measured from the source event's time.

Erasure survives replay. A read model keeps an erasure tombstone for an erased key and applies it on rebuild. A retained integration event has its erased personal fields removed, or its key shredded, so no rebuild restores them. This settles the rebuild half of the earlier read-model decision's revisit condition.

Read models never guard decisions that need current truth. Revoked permissions, available funds, reservations, and period locks are decided by the owning module, through a decision port the command calls and revalidates within its own commit, or by the owner's own invariant.

## Consequences

The source events behind a reporting read model carry identity, and the model keeps a tombstone path for erasure.

A command deciding on funds or permissions asks the owning module instead of reading the copy.

## Evidence

[research/cross-module-coupling-practice](research/cross-module-coupling-practice) holds the event-carried state transfer and ports-and-adapters sources behind the copy and the port. The open semantics were read from the automation page with high confidence.

## Revisit Conditions

This decision reopens when a source cannot carry identity. It also reopens when a decision needs fresher truth than the owning module answers.
