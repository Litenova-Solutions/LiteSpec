# Module Events, Business State in Aggregates, and Proof Freshness from the Run Commit

LiteSpec sent facts between two modules of one product through an external system, filed state with no lifecycle as an aggregate with a store flag, and called a proof fresh until a marked file changed. This decision gives facts between modules their own record, keeps every piece of business state in an aggregate, and judges proof freshness from the run commit.

## Context

Two modules of one product could share facts only through an external system: a reaction on another module's domain event was refused, and every integration event had to name the external system it travels through. Claimwick's reporting module reading the spending module's reimbursements needed an invented message bus, which the book showed as a dependency.

State with no lifecycle was an aggregate with `kind: store`. A review proposed a separate store type for state no invariant guards. Its examples were a manager's notification preference and a financial audit trail. Both are business state with rules: one preference per manager, changed only by its owner, kept as personal data; an append-only, chain-linked trail kept for its retention. A separate type for "stored but business-free" state invites filing business data without its rules.

A proof went stale only when the scenario, its test body, or a marked file changed after the run. A change to an unmarked value object left every proof of its callers verified.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| Module events, business state only in aggregates, freshness from the run commit | No invented bus; every piece of business state carries its rules; stale proofs show. | One more record type; a value with few rules still needs an aggregate. |
| A store type for state no invariant guards | A light record for a table with no rules. | Business state filed without its rules. |
| Keep in-product integration events and mark-based staleness | No new type, no commit tracking. | Invented bus stays; proofs outlive code. |

## Decision

A fact one module publishes for the other modules of the product is a `module-event`. It names the domain event it `translates` and selects the `fields` of that event it publishes, so each field keeps one definition. It travels in process through the transactional outbox, with no broker inside the bounded context. An integration event stays for a fact that crosses the bounded context and names the external system it travels through.

State the business cares about is an aggregate with its invariants. State that exists only because of a technical mechanism, such as outbox rows, idempotency keys, caches or provider delivery logs, is infrastructure: the stack and extension pages and technical decisions govern it, and it is no Behavior record. An aggregate with no lifecycle of its own declares the single state `Active`, which also leaves room for a later soft delete.

A scenario's proof is fresh when its run commit is HEAD, or when every path changed since is a record or Markdown file. Otherwise it is stale and reads `last passed at <short commit>, <N> commits ago`.

## Consequences

Claimwick's suppression list is the reminder-preference aggregate, with the rule that only the manager or an admin changes it. An audit trail is an aggregate whose rules guard it. The `store` record type and its checks do not exist.

A reaction answers its own module's domain event, a module event of any module of the product, or an inbound integration event. `event-field-uncarried` reports a selected field the domain event does not carry.

`proofs.json` keeps the run commit and outcome per scenario, written by `litespec model check --record-proof`, which CI runs on the pull request head.

## Evidence

The external 7.0.0 review reported the store shape (F-08, F-37), the module-to-module gap (F-51) and proof freshness (F-32). The owner rejected the store type on 2026-11-16 because its examples were business state with invariants.

## Revisit Conditions

This decision reopens when modules need a broker for facts inside the bounded context, when a kind of business state cannot hold its rules in an aggregate, or when proof needs a signal the run commit cannot carry.
